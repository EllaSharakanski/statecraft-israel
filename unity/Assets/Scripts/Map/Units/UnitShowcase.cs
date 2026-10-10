#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The unit art slice (owner, 2026-10-11: "make sure the units are like in the promo video"): a 1948 rifle
    /// trio with a selection ring, a Spitfire flying a loop with its shadow on the ground, and a corvette at sea, all at
    /// <see cref="PromoPose.Gameplay03"/>. A showcase only: it reads nothing from the sim and places no unit the game state
    /// knows about (where real units stand, move and fight is war mode's job, issue #104). It appears only when asked
    /// for ("units" in the WebGL page URL or -units on desktop), in the art checkpoint's screenshots ("shot") and in the
    /// editor's promo renders; normal play never shows it. Registers the "units" visual toggle, so the frame profile can
    /// price it (?vprofile&amp;units).</summary>
    public static class UnitShowcase
    {
        public const string RootName = "UnitShowcase", ToggleName = "units";

        /// <summary>Symbolic scale, measured on the promo (docs/units.md): the promo's riflemen stand about 0.38 of a hex's
        /// screen width tall at the gameplay zoom, which on our 5 km hexes at 60 km is about 5.5 km. So a man is drawn
        /// <see cref="FigureScale"/> times his real size, and the aircraft and the ship are sized against him, as Civ draws
        /// them, not against each other.</summary>
        public const float FigureScale = 2600f;
        public const float AircraftScale = 520f;        // span 11.2 m x 520 = 5.8 km: a little wider than a man is tall
        public const float ShipScale = 105f;            // 62.5 m x 105 = 6.6 km: about one hex, like the promo's boats
        /// <summary>Trio spacing and the ring, as fractions of the hex circumradius.</summary>
        public const float TrioSpread = 0.30f, RingRadius = 0.62f, RingWidth = 0.035f, RingClearanceM = 25f;
        /// <summary>The flight: a circle of <see cref="LoopRadiusM"/> about <see cref="LoopCentre"/>, at
        /// <see cref="AltitudeM"/> above the highest ground under it, once every <see cref="LoopSeconds"/>, banked inwards.</summary>
        public const float LoopRadiusM = 16000f, AltitudeM = 4200f, LoopSeconds = 48f, BankDegrees = 28f;
        /// <summary>The loop's phase (0..1) used for still images (screenshots, editor renders), so they are repeatable: the
        /// aircraft over the coast off Tel Aviv, inside the tilt-shift's sharp band, its shadow on the coastal plain (chosen
        /// from renders at 0.40-0.74, docs/units.md).</summary>
        public const float StillPhase = 0.52f;

        /// <summary>Where the slice stands, inside the gameplay03 frame: the trio on the coastal plain south-east of Jaffa
        /// (the promo puts its trio there), the loop over Tel Aviv and the plain, the corvette off Jaffa.</summary>
        public static readonly (double lon, double lat) TrioAt = (34.84, 31.93), LoopCentre = (34.86, 32.03), ShipAt = (34.63, 32.02);
        public const float ShipHeadingDegrees = 200f;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "units") || UrlFlags.Has(url, "shot") : args.Contains("-units") || args.Contains("-shot");

        /// <summary>True when the flight should hold its still phase (screenshots: the same image every run).</summary>
        public static bool StillRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "shot") : args.Contains("-shot");

        /// <summary>Builds the showcase under the map's terrain (so a map rebuild destroys it), its meshes, palette and
        /// materials owned by the terrain's MapRuntimeAssets, and registers the "units" toggle through its teardown.
        /// <paramref name="still"/> freezes the flight at <see cref="StillPhase"/>. Returns the root.</summary>
        public static GameObject Spawn(MapScene map, bool still)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("UnitShowcase needs a built map");
            var terrain = map.Terrain;
            var assets = terrain.GetComponent<MapRuntimeAssets>();
            var old = terrain.transform.Find(RootName);
            if (old != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject); else UnityEngine.Object.DestroyImmediate(old.gameObject); }

            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterials.Create)");
            var ringTemplate = Resources.Load<Material>(UnitMaterials.RingResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.RingResource}.mat is missing (run UnitMaterials.Create)");
            var palette = assets.Own(UnitPalette.MakeTexture());
            var paint = assets.Own(new Material(template) { name = "UnitMiniature (palette)" });
            paint.SetTexture("_BaseMap", palette);

            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;   // children are placed in world space
            float hex = map.Hexes.hex_size_km * 1000f;

            // The trio: three riflemen in a loose triangle facing the camera's side of south-west, on the hex's centre.
            var centre = Ground(map, map.Frame.ToWorld(TrioAt.lon, TrioAt.lat, 0));
            var looks = new[]
            {
                (UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, new Vector2(-0.62f, -0.30f), 200f),
                (UnitModels.Headgear.SteelHelmet, UnitModels.RiflePose.Grounded, true, new Vector2(0.05f, 0.05f), 185f),
                (UnitModels.Headgear.WoolCap, UnitModels.RiflePose.Slung, false, new Vector2(0.66f, -0.40f), 170f),
            };
            for (int i = 0; i < looks.Length; i++)
            {
                var (hat, pose, faded, offset, heading) = looks[i];
                var mesh = assets.Own(UnitModels.Rifleman(hat, pose, faded, $"Rifleman1948-{i}"));
                var p = Ground(map, centre + new Vector3(offset.x, 0, offset.y) * TrioSpread * hex);
                Piece(root.transform, $"Rifleman{i}", mesh, paint, p, Quaternion.Euler(0, heading, 0), FigureScale);
            }
            Ring(map, assets, ringTemplate, root.transform, centre, RingRadius * hex);

            // The aircraft and its propeller.
            var plane = new GameObject("Spitfire1948");
            plane.transform.SetParent(root.transform, false);
            var body = Piece(plane.transform, "Airframe", assets.Own(UnitModels.Spitfire("Spitfire1948")), paint, Vector3.zero, Quaternion.identity, 1f);
            body.transform.localPosition = Vector3.zero;
            var prop = Piece(plane.transform, "Propeller", assets.Own(UnitModels.SpitfirePropeller("SpitfirePropeller")), paint, Vector3.zero, Quaternion.identity, 1f);
            prop.transform.localPosition = UnitModels.SpitfirePropellerHub;
            plane.transform.localScale = Vector3.one * AircraftScale;
            var loopCentre = map.Frame.ToWorld(LoopCentre.lon, LoopCentre.lat, 0);
            var flight = plane.AddComponent<ShowcaseFlight>();
            flight.Configure(loopCentre, LoopRadiusM, LoopFloor(map, loopCentre, LoopRadiusM) + AltitudeM, LoopSeconds, BankDegrees, prop.transform, still ? StillPhase : (float?)null);

            // The corvette, at the sea surface.
            var ship = Ground(map, map.Frame.ToWorld(ShipAt.lon, ShipAt.lat, 0));
            Piece(root.transform, "Corvette1948", assets.Own(UnitModels.Corvette("CorvetteK20")), paint, ship, Quaternion.Euler(0, ShipHeadingDegrees, 0), ShipScale);

            assets.OnTeardown(VisualToggles.Register(ToggleName, on => { if (root != null) root.SetActive(on); }));
            return root;
        }

        static GameObject Piece(Transform parent, string name, Mesh mesh, Material mat, Vector3 position, Quaternion rotation, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>The point on the terrain under <paramref name="p"/> (world x, z).</summary>
        public static Vector3 Ground(MapScene map, Vector3 p)
        {
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y;
            return p;
        }

        /// <summary>The highest ground under the loop (sampled every 5 degrees and at its centre), so the aircraft never
        /// dips into a ridge; its altitude is constant, so its shadow keeps one offset.</summary>
        public static float LoopFloor(MapScene map, Vector3 centre, float radius)
        {
            float top = Ground(map, centre).y;
            for (int a = 0; a < 360; a += 5)
            {
                var p = centre + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)) * radius;
                top = Mathf.Max(top, Ground(map, p).y);
            }
            return top;
        }

        /// <summary>The selection ring: a thin flat band on the ground around the trio, drawn with the hex selection's
        /// unlit ivory and depth offset (the promo's white ring), each vertex lifted to the ground under it.</summary>
        static void Ring(MapScene map, MapRuntimeAssets assets, Material ringTemplate, Transform parent, Vector3 centre, float radius)
        {
            const int n = 96;
            var verts = new List<Vector3>(2 * n); var tris = new List<int>(6 * n);
            float w = RingWidth * radius / RingRadius;
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var outer = Ground(map, centre + d * (radius + w / 2)); var inner = Ground(map, centre + d * (radius - w / 2));
                float y = Mathf.Max(outer.y, inner.y) + RingClearanceM;
                outer.y = y; inner.y = y;
                verts.Add(outer); verts.Add(inner);
            }
            for (int i = 0; i < n; i++)
            {
                int a = 2 * i, b = 2 * ((i + 1) % n);
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });   // the ring shader draws both faces
            }
            var mesh = assets.Own(new Mesh { name = "UnitSelectionRing" });
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = ringTemplate;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
