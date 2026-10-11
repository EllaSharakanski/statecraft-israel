#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The units in normal play (docs/units.md, "Units in play"): one token per unit that the sourced unit data
    /// (units_1948.yaml) and the sim's state give now (<see cref="UnitRoster"/>), each on its hex (<see cref="UnitPlacer"/>),
    /// with the #139 models (and the authored-prefab hook): riflemen for a brigade, the squadron's aircraft flying a loop
    /// over its base or parked on it, a corvette at sea. Selecting a token draws the promo's white ring round its hex.
    /// Presentation only: it reads the state and never writes it (UnitLayerTests), so the goldens cannot move. Built under
    /// the map's terrain, so a map rebuild destroys it; its meshes, palette and material are owned by its own
    /// MapRuntimeAssets, which also unregisters its "units" visual toggle (the frame profile prices it).</summary>
    public sealed class UnitLayer : MonoBehaviour
    {
        public const string RootName = "Units", ToggleName = "units";
        /// <summary>A footprint's buildings and tinted ground reach 1.1 of its radius (SettlementKits.PaintGround).</summary>
        public const float FootprintMargin = 1.1f;
        /// <summary>Clearance (m) between a figure's centre and a railway's or road's corridor: about half a figure's width
        /// at <see cref="UnitShowcase.FigureScale"/> (0.6 m x 2000 = 1.2 km across) plus a margin.</summary>
        public const float FigureClearanceM = 700f;
        /// <summary>A parked aircraft's centre of gravity above the ground, in real metres (on its undercarriage).</summary>
        public const float ParkedHeightM = 1.4f;
        public const float ParkedHeadingDegrees = 200f;

        MapScene map = null!;
        IReadOnlyList<UnitEntry> units = Array.Empty<UnitEntry>();
        IReadOnlyList<MapHex> hexes = Array.Empty<MapHex>();
        Vector2[] hexXZ = Array.Empty<Vector2>();
        /// <summary>The route corridors with their bounds grown by the clearance, so a figure far from a route skips it.</summary>
        (List<Vector2> line, float half, Rect bounds)[] corridors = Array.Empty<(List<Vector2>, float, Rect)>();
        MapRuntimeAssets assets = null!;
        Material paint = null!, ringTemplate = null!;
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        List<UnitToken>? roster;
        List<PlacedToken> placed = new List<PlacedToken>();
        Transform tokensRoot = null!;
        GameObject? ring;
        bool still;

        /// <summary>The tokens drawn now, in the data's order.</summary>
        public IReadOnlyList<PlacedToken> Tokens => placed;
        /// <summary>The selected unit's id, or null.</summary>
        public string? Selected { get; private set; }

        /// <summary>Makes the (empty) layer under the map's terrain; <see cref="Show"/> fills it. <paramref name="still"/>
        /// holds a flying aircraft at <see cref="UnitShowcase.StillPhase"/> (screenshots repeat).</summary>
        public static UnitLayer Create(MapScene map, IReadOnlyList<UnitEntry> units, IReadOnlyList<MapHex> hexes, bool still)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("UnitLayer needs a built map");
            var terrain = map.Terrain;
            var old = terrain.transform.Find(RootName);
            if (old != null)
            {
                foreach (var a in old.GetComponentsInChildren<MapRuntimeAssets>(true)) a.RunTeardown();
                old.SetParent(null);
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }
            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterialAssets.Create)");
            var ringTemplate = Resources.Load<Material>(UnitMaterials.RingResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.RingResource}.mat is missing (run UnitMaterialAssets.Create)");
            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;   // children are placed in world space
            var layer = root.AddComponent<UnitLayer>();
            layer.map = map; layer.units = units; layer.hexes = hexes; layer.still = still;
            layer.hexXZ = hexes.Select(h => { var w = map.Frame.ToWorld(h.Lon, h.Lat, 0); return new Vector2(w.x, w.z); }).ToArray();
            layer.corridors = map.RouteCorridors.Where(c => c.line.Count > 0).Select(c =>
            {
                float minX = c.line.Min(p => p.x), maxX = c.line.Max(p => p.x), minY = c.line.Min(p => p.y), maxY = c.line.Max(p => p.y);
                float grow = c.halfWidth + FigureClearanceM;
                return (c.line, c.halfWidth, Rect.MinMaxRect(minX - grow, minY - grow, maxX + grow, maxY + grow));
            }).ToArray();
            layer.assets = root.AddComponent<MapRuntimeAssets>();
            var palette = layer.assets.Own(UnitPalette.MakeTexture(readable: false));
            layer.paint = layer.assets.Own(new Material(template) { name = "UnitMiniature (palette)" });
            layer.paint.SetTexture("_BaseMap", palette);
            layer.ringTemplate = ringTemplate;
            layer.tokensRoot = new GameObject("Tokens").transform;
            layer.tokensRoot.SetParent(root.transform, false);
            layer.assets.OnTeardown(VisualToggles.Register(ToggleName, on => { if (root != null) root.SetActive(on); }));
            return layer;
        }

        /// <summary>Draws the units for <paramref name="state"/> (read only). Rebuilds the tokens only when the roster
        /// changed; returns true when it did. A selected unit that is no longer drawn is deselected.</summary>
        public bool Show(Content content, GameState state)
        {
            var now = UnitRoster.Build(units, content, state);
            if (roster != null && roster.SequenceEqual(now)) return false;
            roster = now;
            placed = UnitPlacer.Place(now, hexes, (lon, lat) => { var w = map.Frame.ToWorld(lon, lat, 0); return (w.x, w.z); }, IsClear, Debug.LogWarning);
            Rebuild();
            if (Selected != null && !placed.Any(p => p.Token.Unit.Id == Selected)) Select(null); else Select(Selected);
            return true;
        }

        /// <summary>What a click on hex (q, r) selects: the unit standing there, unless it is already selected (a second
        /// click shows the hex, so a token never hides the hex's map actions); null means show the hex.</summary>
        public string? UnitToSelectAt(int q, int r)
        {
            var hit = At(q, r);
            return hit != null && hit.Value.Token.Unit.Id != Selected ? hit.Value.Token.Unit.Id : null;
        }

        /// <summary>The token on hex (q, r), or null.</summary>
        public PlacedToken? At(int q, int r)
        {
            foreach (var p in placed) if (p.Q == q && p.R == r) return p;
            return null;
        }

        /// <summary>Selects a drawn unit by id (the ring goes round its hex), or clears the selection (null).</summary>
        public void Select(string? id)
        {
            if (ring != null) { ring.transform.SetParent(null); if (Application.isPlaying) Destroy(ring); else DestroyImmediate(ring); ring = null; }
            var hit = placed.FirstOrDefault(p => p.Token.Unit.Id == id);
            Selected = id != null && hit.Token != null ? id : null;
            if (Selected == null) return;
            float hex = map.Hexes.hex_size_km * 1000f;
            ring = UnitShowcase.Ring(map, assets, ringTemplate, transform, HexCentre(hit.Q, hit.R), UnitShowcase.RingRadius * hex);
        }

        Vector3 HexCentre(int q, int r)
        {
            foreach (var h in hexes)
                if (h.Q == q && h.R == r) return UnitShowcase.Ground(map, map.Frame.ToWorld(h.Lon, h.Lat, 0));
            throw new ArgumentException($"no hex ({q}, {r})");
        }

        /// <summary>UnitPlacer's test: the token's ring stays off every settlement footprint, and its figures (an army
        /// token's full trio, a parked aircraft's centre; so the hex does not change with the call-up or the security level)
        /// stand on land and off the railways and roads. Ships need only a sea hex.</summary>
        internal bool IsClear(UnitToken t, MapHex h)
        {
            float hex = map.Hexes.hex_size_km * 1000f;
            var c = map.Frame.ToWorld(h.Lon, h.Lat, 0);
            var centre = new Vector2(c.x, c.z);
            float disc = UnitShowcase.RingRadius * hex;
            foreach (var (fc, fr) in map.SettlementFootprints)
                if ((fc - centre).magnitude < fr * FootprintMargin + disc) return false;
            if (t.Unit.Service == UnitData.Navy) return true;
            var points = t.Unit.Service == UnitData.Army
                ? UnitShowcase.FigureOffsets(3).Select(o => centre + o * UnitShowcase.TrioSpread * hex)
                : new[] { centre };
            foreach (var p in points)
            {
                if (!LandAt(p)) return false;
                foreach (var (line, half, bounds) in corridors)
                    if (bounds.Contains(p))
                        for (int i = 0; i + 1 < line.Count; i++)
                            if (DistanceToSegment(p, line[i], line[i + 1]) < half + FigureClearanceM) return false;
            }
            return true;
        }

        bool LandAt(Vector2 p)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < hexXZ.Length; i++)
            {
                float d = (hexXZ[i] - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best >= 0 && hexes[best].Land;
        }

        internal static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return (p - (a + t * ab)).magnitude;
        }

        Mesh MeshFor(string key, Func<Mesh> build)
        {
            if (!meshes.TryGetValue(key, out var mesh))
            {
                mesh = assets.Own(build());
                mesh.UploadMeshData(true);   // no CPU copy kept in players
                meshes[key] = mesh;
            }
            return mesh;
        }

        void Rebuild()
        {
            for (int i = tokensRoot.childCount - 1; i >= 0; i--)
            {
                var old = tokensRoot.GetChild(i).gameObject;
                old.transform.SetParent(null);
                if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }
            float hex = map.Hexes.hex_size_km * 1000f;
            foreach (var p in placed)
            {
                var t = p.Token;
                var go = new GameObject(t.Unit.Id);
                go.transform.SetParent(tokensRoot, false);
                var centre = HexCentre(p.Q, p.R);
                switch (t.Unit.Service)
                {
                    case UnitData.Army:
                        var offsets = UnitShowcase.FigureOffsets(t.Figures);
                        for (int i = 0; i < offsets.Length; i++)
                        {
                            var (type, hat, pose, faded, _, heading) = UnitShowcase.Trio[i];
                            var at = UnitShowcase.Ground(map, centre + new Vector3(offsets[i].x, 0, offsets[i].y) * UnitShowcase.TrioSpread * hex);
                            UnitShowcase.Piece(go.transform, $"Rifleman{i}", type, () => MeshFor(type, () => UnitModels.Rifleman(hat, pose, faded, type)),
                                paint, at, Quaternion.Euler(0, heading, 0), UnitShowcase.FigureScale, out _);
                        }
                        break;
                    case UnitData.Air:
                        BuildAircraft(go.transform, t, centre);
                        break;
                    case UnitData.Navy:
                        UnitShowcase.Piece(go.transform, "Corvette", t.ArtType, () => MeshFor(t.ArtType, () => UnitModels.Corvette(t.ArtType)),
                            paint, centre, Quaternion.Euler(0, UnitShowcase.ShipHeadingDegrees, 0), UnitShowcase.ShipScale, out _);
                        break;
                }
            }
        }

        void BuildAircraft(Transform parent, UnitToken t, Vector3 baseCentre)
        {
            var plane = new GameObject("Aircraft");
            plane.transform.SetParent(parent, false);
            bool spitfire = t.ArtType == UnitShowcase.SpitfireType;
            Func<Mesh> airframe = spitfire ? (Func<Mesh>)(() => UnitModels.Spitfire(t.ArtType))
                : () => UnitModels.AviaS199(t.ArtType == UnitShowcase.S199YoavType, t.ArtType);
            UnitShowcase.Piece(plane.transform, "Airframe", t.ArtType, () => MeshFor(t.ArtType, airframe), paint, Vector3.zero, Quaternion.identity, 1f, out bool authored);
            Transform? prop = null;
            if (!authored)
            {
                var propKey = spitfire ? "spitfire-propeller" : "avia-s199-propeller";
                var propMesh = MeshFor(propKey, spitfire ? (Func<Mesh>)(() => UnitModels.SpitfirePropeller(propKey)) : () => UnitModels.S199Propeller(propKey));
                prop = UnitShowcase.Piece(plane.transform, "Propeller", propMesh, paint, Vector3.zero, Quaternion.identity, 1f).transform;
                prop.localPosition = spitfire ? UnitModels.SpitfirePropellerHub : UnitModels.S199PropellerHub;
            }
            plane.transform.localScale = Vector3.one * UnitShowcase.AircraftScale;
            if (t.Flying)
            {
                var flight = plane.AddComponent<ShowcaseFlight>();
                flight.Configure(baseCentre, UnitShowcase.LoopRadiusM, UnitShowcase.LoopFloor(map, baseCentre, UnitShowcase.LoopRadiusM) + UnitShowcase.AltitudeM,
                    UnitShowcase.LoopSeconds, UnitShowcase.BankDegrees, prop, still ? UnitShowcase.StillPhase : (float?)null);
            }
            else
                plane.transform.SetPositionAndRotation(baseCentre + Vector3.up * ParkedHeightM * UnitShowcase.AircraftScale,
                    Quaternion.Euler(0, ParkedHeadingDegrees, 0));
        }
    }
}
