#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    /// <summary>The sub-districts of Mandatory Palestine in force on 15 May 1948, the game's start, as the map draws them:
    /// content/map/subdistrict_hexes_1948.yaml, which tools/borders_bake.py snaps from the true digitised lines
    /// (content/map/subdistricts_1948.geojson; tools/borders_digitise.py). Each land hex is in one sub-district, or in the
    /// "outside" region (style none: the sub-districts' bands run along the Mandate frontier, nothing beyond it); sea and
    /// lake hexes are in none, so the coast is not outlined. Display only: the sim never reads the file.</summary>
    public sealed class Subdistricts1948
    {
        public const string FileName = "subdistrict_hexes_1948.yaml";

        public sealed class Region
        {
            public int Id; public string Key = "", Name = "", District = ""; public BorderStyle Style;
        }

        public readonly RegionLayer Layer;
        /// <summary>What the map shows, and where the true line is (the file's "line").</summary>
        public readonly string Title, Line;
        readonly Dictionary<int, Region> regions;

        Subdistricts1948(RegionLayer layer, string title, string line, Dictionary<int, Region> regions)
        { Layer = layer; Title = title; Line = line; this.regions = regions; }

        public IReadOnlyCollection<Region> Regions => regions.Values;

        /// <summary>The sub-district a hex is drawn in, or null (sea, lake, or outside Mandatory Palestine).</summary>
        public Region? SubdistrictAt(int q, int r) =>
            regions.TryGetValue(Layer.RegionOf(q, r), out var x) && x.Style != BorderStyle.None ? x : null;

        sealed class FileRow { public int Id { get; set; } public string Key { get; set; } = ""; public string Name { get; set; } = ""; public string District { get; set; } = ""; public string Style { get; set; } = ""; }
        sealed class FileDoc { public string Layer { get; set; } = ""; public string Title { get; set; } = ""; public string Line { get; set; } = ""; public List<FileRow> Regions { get; set; } = new List<FileRow>(); public List<List<int>> Hexes { get; set; } = new List<List<int>>(); }

        /// <summary>Parses the baked file. Every malformed input throws InvalidDataException naming the file and the entry.</summary>
        public static Subdistricts1948 Parse(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml)) throw new InvalidDataException($"{FileName}: empty");
            FileDoc? doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<FileDoc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: line {e.Start.Line}: {e.InnerException?.Message ?? e.Message}", e); }
            if (doc == null || string.IsNullOrWhiteSpace(doc.Layer)) throw new InvalidDataException($"{FileName}: no layer");
            if (doc.Regions.Count == 0) throw new InvalidDataException($"{FileName}: no regions");
            var regions = new Dictionary<int, Region>(); var styles = new Dictionary<int, BorderStyle>();
            foreach (var r in doc.Regions)
            {
                var style = r.Style switch
                {
                    "primary" => BorderStyle.Primary, "secondary" => BorderStyle.Secondary, "none" => BorderStyle.None,
                    _ => throw new InvalidDataException($"{FileName}: region {r.Id} ({r.Key}): unknown style '{r.Style}'"),
                };
                if (string.IsNullOrWhiteSpace(r.Key) || string.IsNullOrWhiteSpace(r.Name)) throw new InvalidDataException($"{FileName}: region {r.Id}: key and name are required");
                if (regions.ContainsKey(r.Id)) throw new InvalidDataException($"{FileName}: region id {r.Id} twice");
                regions[r.Id] = new Region { Id = r.Id, Key = r.Key, Name = r.Name, District = r.District, Style = style };
                styles[r.Id] = style;
            }
            var hexes = new Dictionary<(int q, int r), int>();
            foreach (var h in doc.Hexes)
            {
                if (h == null || h.Count != 3) throw new InvalidDataException($"{FileName}: a hex row is not [q, r, region]");
                if (!regions.ContainsKey(h[2])) throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) is in region {h[2]}, which is not listed");
                if (hexes.ContainsKey((h[0], h[1]))) throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) twice");
                hexes[(h[0], h[1])] = h[2];
            }
            RegionLayer layer;
            try { layer = new RegionLayer(doc.Layer, hexes, styles); }
            catch (ArgumentException e) { throw new InvalidDataException($"{FileName}: {e.Message}", e); }
            return new Subdistricts1948(layer, doc.Title, doc.Line, regions);
        }

        /// <summary>Parses the file and draws it on the built map (MapBorders.Use).</summary>
        public static Subdistricts1948 Show(string yaml)
        {
            var m = Parse(yaml);
            MapBorders.Use(m.Layer);
            return m;
        }
    }
}
