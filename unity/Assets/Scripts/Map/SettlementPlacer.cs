using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    public sealed class SourceRef { public string Url { get; set; } = ""; public string Checked { get; set; } = ""; }

    public sealed class Settlement
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "";
        public double Lon { get; set; } public double Lat { get; set; }
        public int? Founded { get; set; }      // null for pre-modern cities (no verifiable founding year)
        public bool PreModern { get; set; }
        public List<SourceRef> Sources { get; set; } = new List<SourceRef>();
    }

    public sealed class SettlementFile { public List<Settlement> Settlements { get; set; } = new List<Settlement>(); }

    public static class SettlementPlacer
    {
        static readonly Dictionary<string, (Color c, float h)> Marker = new Dictionary<string, (Color, float)>
        {
            ["city"] = (new Color(0.93f, 0.92f, 0.88f), 1800f), ["town"] = (new Color(0.88f, 0.84f, 0.76f), 1100f),
            ["kibbutz"] = (new Color(0.80f, 0.35f, 0.25f), 600f), ["moshav"] = (new Color(0.78f, 0.45f, 0.30f), 600f),
            ["arab_town"] = (new Color(0.82f, 0.74f, 0.58f), 1100f), ["village"] = (new Color(0.76f, 0.68f, 0.52f), 600f),
        };

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>Parses settlements_1948.yaml. Content errors throw InvalidDataException naming the settlement id and the
        /// field: unknown type, duplicate id, non-finite lon/lat (or outside <paramref name="bounds"/> when given), empty
        /// sources, founded null without pre_modern: true.</summary>
        public static List<Settlement> Parse(string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null)
        {
            var list = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build().Deserialize<SettlementFile>(yaml).Settlements;
            var ids = new HashSet<string>();
            foreach (var s in list)
            {
                if (!Marker.ContainsKey(s.Type)) throw new InvalidDataException($"settlements: '{s.Id}' has unknown type '{s.Type}'");
                if (!ids.Add(s.Id)) throw new InvalidDataException($"settlements: '{s.Id}' duplicate id");
                CheckCoord(s.Id, "lon", s.Lon, bounds?.lon0, bounds?.lon1);
                CheckCoord(s.Id, "lat", s.Lat, bounds?.lat0, bounds?.lat1);
                if (s.Sources == null || s.Sources.Count == 0) throw new InvalidDataException($"settlements: '{s.Id}' sources is empty");
                if (s.Founded == null && !s.PreModern) throw new InvalidDataException($"settlements: '{s.Id}' founded is null without pre_modern: true");
            }
            return list;
        }

        static void CheckCoord(string id, string field, double v, double? min, double? max)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new InvalidDataException($"settlements: '{id}' {field} {v} is not finite");
            if (min.HasValue && (v < min.Value || v > max.Value))
                throw new InvalidDataException($"settlements: '{id}' {field} {v} outside the heightmap bounds {min.Value}..{max.Value}");
        }

        /// <summary>Placeholder markers (real per-era prefabs come in M3/M4): a box sized by type plus a name label, under a
        /// returned "Settlements" root. The box uses the editor-made <paramref name="markerMaterial"/> (no runtime
        /// Shader.Find, M0 correction 1); the per-type colour goes through a MaterialPropertyBlock.</summary>
        public static GameObject Place(List<Settlement> list, GeoFrame frame, Terrain terrain, Material markerMaterial)
        {
            var root = new GameObject("Settlements"); var parent = root.transform;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var block = new MaterialPropertyBlock();
            foreach (var s in list)
            {
                var p = frame.ToWorld(s.Lon, s.Lat, 0); p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                var (c, h) = Marker[s.Type];
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = s.Id; box.transform.SetParent(parent);
                box.transform.position = p + Vector3.up * h / 2; box.transform.localScale = new Vector3(h, h, h);
                var r = box.GetComponent<Renderer>(); r.sharedMaterial = markerMaterial;
                block.SetColor(BaseColor, c); r.SetPropertyBlock(block);

                var labelGo = new GameObject(s.Id + "_label");
                labelGo.AddComponent<MeshRenderer>().sharedMaterial = font.material;
                var label = labelGo.AddComponent<TextMesh>();
                label.font = font; label.text = s.Name; label.characterSize = 400; label.anchor = TextAnchor.LowerCenter; label.color = Color.black;
                labelGo.transform.position = p + Vector3.up * (h + 400); labelGo.transform.rotation = Quaternion.Euler(50, 0, 0);
                labelGo.transform.SetParent(parent);
            }
            return root;
        }
    }
}
