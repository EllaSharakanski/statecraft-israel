using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    public sealed class SourceRef { public string Url { get; set; } = ""; public string Checked { get; set; } = ""; }

    public sealed class Settlement
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "";
        public double Lon { get; set; } = double.NaN; public double Lat { get; set; } = double.NaN; // NaN = missing
        public int? Founded { get; set; }      // null for pre-modern cities (no verifiable founding year)
        public bool PreModern { get; set; }
        public List<SourceRef> Sources { get; set; } = new List<SourceRef>();
    }

    public sealed class SettlementFile { public List<Settlement> Settlements { get; set; } = new List<Settlement>(); }

    public static class SettlementPlacer
    {
        public const string DefaultFileName = "settlements_1948.yaml";

        // Marker colour and box edge (m); label cap height (m). Cap heights give >= 18 / 14 / 11 px at the top (farthest
        // edge, ~128 km) of a 1080p flythrough frame at the rig's 90 km distance and 35-degree FOV (K1).
        static readonly Dictionary<string, (Color c, float h, float cap)> Marker = new Dictionary<string, (Color, float, float)>
        {
            ["city"] = (new Color(0.93f, 0.92f, 0.88f), 1800f, 1500f), ["town"] = (new Color(0.88f, 0.84f, 0.76f), 1100f, 1150f),
            ["kibbutz"] = (new Color(0.80f, 0.35f, 0.25f), 600f, 900f), ["moshav"] = (new Color(0.78f, 0.45f, 0.30f), 600f, 900f),
            ["arab_town"] = (new Color(0.82f, 0.74f, 0.58f), 1100f, 1150f), ["village"] = (new Color(0.76f, 0.68f, 0.52f), 600f, 900f),
        };

        // Labels in the dense Tel Aviv - Lod cluster (and Safed / Rosh Pinna, 4 km apart) extend away from their neighbours
        // instead of centring on the marker.
        static readonly Dictionary<string, TextAnchor> LabelAnchor = new Dictionary<string, TextAnchor>
        {
            ["jaffa"] = TextAnchor.LowerRight, ["rishon_lezion"] = TextAnchor.LowerRight, ["ramla"] = TextAnchor.LowerRight,
            ["lod"] = TextAnchor.LowerLeft, ["petah_tikva"] = TextAnchor.LowerLeft,
            ["safed"] = TextAnchor.LowerRight, ["rosh_pinna"] = TextAnchor.LowerLeft,
        };

        public const int LabelFontSize = 64;          // glyph raster size; characterSize scales it to world metres
        public const float CapHeightPerUnit = 4.5f;   // cap height (m) per unit of characterSize at LabelFontSize
        public const float LabelLift = 0.75f;         // label base above the marker top, in box edges: clears the box's far top edge at the rig pitch
        static readonly Color LabelColour = new Color(0.98f, 0.97f, 0.93f), ShadowColour = new Color(0.06f, 0.05f, 0.04f, 0.9f);

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>Parses settlements_1948.yaml. Every malformed input throws InvalidDataException naming
        /// <paramref name="file"/>, and for content errors the settlement id and the field: YAML syntax, unknown keys or bad
        /// scalars; empty id or name; unknown type; duplicate id; missing or non-finite lon/lat (or outside
        /// <paramref name="bounds"/> when given); empty sources, or a source with an empty url or checked; founded null
        /// without pre_modern: true.</summary>
        public static List<Settlement> Parse(string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null,
            string file = DefaultFileName)
        {
            SettlementFile doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build().Deserialize<SettlementFile>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            if (doc?.Settlements == null || doc.Settlements.Count == 0) throw new InvalidDataException($"{file}: no settlements");
            var ids = new HashSet<string>();
            foreach (var s in doc.Settlements)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.Id)) throw new InvalidDataException($"{file}: a settlement has an empty id");
                string at = $"{file}: '{s.Id}'";
                if (!ids.Add(s.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (string.IsNullOrWhiteSpace(s.Name)) throw new InvalidDataException($"{at} has an empty name");
                if (s.Type == null || !Marker.ContainsKey(s.Type)) throw new InvalidDataException($"{at} has unknown type '{s.Type}'");
                CheckCoord(at, "lon", s.Lon, bounds?.lon0, bounds?.lon1);
                CheckCoord(at, "lat", s.Lat, bounds?.lat0, bounds?.lat1);
                if (s.Sources == null || s.Sources.Count == 0) throw new InvalidDataException($"{at} sources is empty");
                for (int i = 0; i < s.Sources.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(s.Sources[i]?.Url)) throw new InvalidDataException($"{at} sources[{i}].url is empty");
                    if (string.IsNullOrWhiteSpace(s.Sources[i].Checked)) throw new InvalidDataException($"{at} sources[{i}].checked is empty");
                }
                if (s.Founded == null && !s.PreModern) throw new InvalidDataException($"{at} founded is null without pre_modern: true");
            }
            return doc.Settlements;
        }

        static void CheckCoord(string at, string field, double v, double? min, double? max)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new InvalidDataException($"{at} {field} is missing or not finite ({v})");
            if (min.HasValue && (v < min.Value || v > max.Value))
                throw new InvalidDataException($"{at} {field} {v} outside the heightmap bounds {min.Value}..{max.Value}");
        }

        /// <summary>Placeholder markers (real per-era prefabs come in M3/M4): a box sized by type plus a name label, under a
        /// returned "Settlements" root. The box is a plain MeshFilter + MeshRenderer (no collider) using the editor-made
        /// <paramref name="markerMaterial"/> (no runtime Shader.Find, M0 correction 1); the per-type colour goes through a
        /// MaterialPropertyBlock. The label is light text with a dark drop shadow, above the box, facing the rig's pitch.</summary>
        public static GameObject Place(List<Settlement> list, GeoFrame frame, Terrain terrain, Material markerMaterial)
        {
            var root = new GameObject("Settlements"); var parent = root.transform;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var block = new MaterialPropertyBlock();
            foreach (var s in list)
            {
                var p = frame.ToWorld(s.Lon, s.Lat, 0); p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                var (c, h, cap) = Marker[s.Type];
                var box = new GameObject(s.Id); box.transform.SetParent(parent);
                box.AddComponent<MeshFilter>().sharedMesh = cube;
                var r = box.AddComponent<MeshRenderer>(); r.sharedMaterial = markerMaterial;
                block.SetColor(BaseColor, c); r.SetPropertyBlock(block);
                box.transform.position = p + Vector3.up * h / 2; box.transform.localScale = new Vector3(h, h, h);

                var anchor = LabelAnchor.TryGetValue(s.Id, out var a) ? a : TextAnchor.LowerCenter;
                var label = MakeText(s.Id + "_label", s.Name, font, cap, anchor, LabelColour);
                label.transform.SetPositionAndRotation(p + Vector3.up * h * (1f + LabelLift), Quaternion.Euler(CameraRig.Pitch, 0, 0));
                label.transform.SetParent(parent, true);
                var shadow = MakeText("shadow", s.Name, font, cap, anchor, ShadowColour);
                shadow.transform.SetParent(label.transform, false);
                shadow.transform.localPosition = new Vector3(cap * 0.07f, -cap * 0.07f, cap * 0.05f); // down-right, behind
            }
            return root;
        }

        static GameObject MakeText(string name, string text, Font font, float capM, TextAnchor anchor, Color colour)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshRenderer>().sharedMaterial = font.material;
            var t = go.AddComponent<TextMesh>();
            t.font = font; t.fontSize = LabelFontSize; t.characterSize = capM / CapHeightPerUnit;
            t.text = text; t.anchor = anchor; t.color = colour;
            return go;
        }
    }
}
