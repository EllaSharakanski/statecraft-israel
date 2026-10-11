#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The landmark a locality's kit gets at its heart (content/map/localities_1948.yaml, from Village Statistics'
    /// religion columns): a mosque, a church, none (the Druze villages), or a water tower (the Jewish places).</summary>
    public enum Landmark { Mosque, Church, None, WaterTower }

    /// <summary>The localities of 15 May 1948 beyond the 25 game settlements (owner rulings 2026-10-11: places of 1,000 or more
    /// people first, shown by their 1948 names with today's name in the label's tooltip) and the Bedouin encampment areas
    /// (one per sub-district whose Village Statistics rows name tribes or nomads, with their total). Read from
    /// content/map/localities_1948.yaml through the map inputs (MapInputs.LocalitiesYaml). Presentation only: the sim never
    /// reads the file, so it is outside the content hash (SaveGameTests.TheLocalitiesFileIsOutsideTheContentHash).</summary>
    public static class Localities
    {
        public const string FileName = "localities_1948.yaml";

        public sealed class Place
        {
            /// <summary>The kit's and label's settlement record: id, the 1948 name, the repo type, coordinates.</summary>
            public Settlement Settlement = null!;
            public string NameToday = "";
            public int Population;
            public Landmark Landmark;
        }

        public sealed class Area
        {
            public string Subdistrict = "";
            public int Population;
            public List<(int q, int r)> Hexes = new List<(int, int)>();
        }

        public sealed class Doc { public List<Place> Places = new List<Place>(); public List<Area> Encampments = new List<Area>(); }

        sealed class SourceRow
        {
            public string Url { get; set; } = ""; public string Checked { get; set; } = "";
            public string For { get; set; } = ""; public string Licence { get; set; } = "";
        }

        sealed class PlaceRow
        {
            public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string NameBasis { get; set; } = "";
            public string NameToday { get; set; } = ""; public string? NameHe { get; set; } public string? NameAr { get; set; }
            public double Lon { get; set; } = double.NaN; public double Lat { get; set; } = double.NaN;
            public string Kind { get; set; } = ""; public string Type { get; set; } = ""; public string Landmark { get; set; } = "";
            public int Population { get; set; } = -1; public List<string> ListedAs { get; set; } = new List<string>();
            public string Match { get; set; } = ""; public bool OwnerVerify { get; set; } public string Wikidata { get; set; } = "";
            public string? DraftId { get; set; } public string? Note { get; set; }
            public string Checked { get; set; } = ""; public string Status { get; set; } = "";
            public List<SourceRow> Sources { get; set; } = new List<SourceRow>();
        }

        sealed class AreaRow
        {
            public string Subdistrict { get; set; } = ""; public int Population { get; set; } = -1;
            public List<string> ListedAs { get; set; } = new List<string>(); public string? Note { get; set; }
            public List<List<int>> Hexes { get; set; } = new List<List<int>>();
            public string Checked { get; set; } = ""; public string Status { get; set; } = "";
            public List<SourceRow> Sources { get; set; } = new List<SourceRow>();
        }

        sealed class FileRow
        {
            public string Status { get; set; } = "";
            public List<PlaceRow> Places { get; set; } = new List<PlaceRow>();
            public List<AreaRow> Encampments { get; set; } = new List<AreaRow>();
        }

        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");
        static readonly HashSet<string> Types = new HashSet<string> { "town", "kibbutz", "moshav", "arab_town", "village" };
        static readonly HashSet<string> Statuses = new HashSet<string> { "draft", "owner-verify", "fact-checked", "verified" };
        static readonly Dictionary<string, Landmark> Landmarks = new Dictionary<string, Landmark>
        {
            ["mosque"] = Landmark.Mosque, ["church"] = Landmark.Church, ["none"] = Landmark.None, ["water_tower"] = Landmark.WaterTower,
        };

        /// <summary>Parses the file. Every malformed input throws InvalidDataException naming <paramref name="file"/> (and the
        /// id or sub-district): YAML errors, unknown keys, an empty, non-snake_case or duplicate id (or one of
        /// <paramref name="reserved"/>, the game settlements' ids), an empty name or today's name, an unknown type or
        /// landmark, a missing or out-of-bounds coordinate, a population under 1, a status outside draft / owner-verify /
        /// fact-checked / verified, no checked date, no sources or a source without url and checked; an encampment area with
        /// no hexes or a hex that is not a [q, r] pair.</summary>
        public static Doc Parse(string yaml, (double lon0, double lon1, double lat0, double lat1) bounds,
            ICollection<string> reserved, string file = FileName)
        {
            FileRow f;
            try { f = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<FileRow>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            if (f?.Places == null || f.Places.Count == 0) throw new InvalidDataException($"{file}: no places");
            var doc = new Doc(); var ids = new HashSet<string>();
            foreach (var p in f.Places)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Id)) throw new InvalidDataException($"{file}: a place has an empty id");
                string at = $"{file}: '{p.Id}'";
                if (!IdRule.IsMatch(p.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!ids.Add(p.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (reserved.Contains(p.Id)) throw new InvalidDataException($"{at} is a game settlement's id (settlements_1948.yaml keeps those)");
                if (string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.NameToday)) throw new InvalidDataException($"{at} needs a name and a name_today");
                if (!Types.Contains(p.Type ?? "")) throw new InvalidDataException($"{at} has unknown type '{p.Type}'");
                if (!Landmarks.TryGetValue(p.Landmark ?? "", out var landmark)) throw new InvalidDataException($"{at} has unknown landmark '{p.Landmark}'");
                if (double.IsNaN(p.Lon) || double.IsNaN(p.Lat) || p.Lon < bounds.lon0 || p.Lon > bounds.lon1 || p.Lat < bounds.lat0 || p.Lat > bounds.lat1)
                    throw new InvalidDataException($"{at} lon/lat {p.Lon}, {p.Lat} missing or outside the map");
                if (p.Population < 1) throw new InvalidDataException($"{at} population {p.Population} is not a count of people");
                CheckSourced(at, p.Status, p.Checked, p.Sources);
                doc.Places.Add(new Place
                {
                    Settlement = new Settlement { Id = p.Id, Name = p.Name, Type = p.Type, Lon = p.Lon, Lat = p.Lat, PreModern = true },
                    NameToday = p.NameToday, Population = p.Population, Landmark = landmark,
                });
            }
            var subs = new HashSet<string>();
            foreach (var a in f.Encampments ?? new List<AreaRow>())
            {
                if (a == null || string.IsNullOrWhiteSpace(a.Subdistrict)) throw new InvalidDataException($"{file}: an encampment row has no subdistrict");
                string at = $"{file}: encampments '{a.Subdistrict}'";
                if (!subs.Add(a.Subdistrict)) throw new InvalidDataException($"{at} duplicate subdistrict");
                if (a.Population < 1) throw new InvalidDataException($"{at} population {a.Population} is not a count of people");
                if (a.Hexes == null || a.Hexes.Count == 0) throw new InvalidDataException($"{at} has no hexes");
                CheckSourced(at, a.Status, a.Checked, a.Sources);
                var area = new Area { Subdistrict = a.Subdistrict, Population = a.Population };
                foreach (var h in a.Hexes)
                {
                    if (h == null || h.Count != 2) throw new InvalidDataException($"{at} a hex is not a [q, r] pair");
                    area.Hexes.Add((h[0], h[1]));
                }
                doc.Encampments.Add(area);
            }
            return doc;
        }

        static void CheckSourced(string at, string status, string checkedOn, List<SourceRow> sources)
        {
            if (!Statuses.Contains(status ?? "")) throw new InvalidDataException($"{at} status '{status}' is not draft, owner-verify, fact-checked or verified");
            if (string.IsNullOrWhiteSpace(checkedOn)) throw new InvalidDataException($"{at} has no checked date");
            if (sources == null || sources.Count == 0) throw new InvalidDataException($"{at} sources is empty");
            for (int i = 0; i < sources.Count; i++)
                if (string.IsNullOrWhiteSpace(sources[i]?.Url) || string.IsNullOrWhiteSpace(sources[i].Checked))
                    throw new InvalidDataException($"{at} sources[{i}] needs a url and a checked date");
        }

        static Dictionary<string, Landmark> landmarks = new Dictionary<string, Landmark>();

        /// <summary>The landmarks the next kit build reads (MapScene.Build sets them with the places).</summary>
        public static void Use(IEnumerable<Place> places)
        {
            landmarks = new Dictionary<string, Landmark>();
            foreach (var p in places) landmarks[p.Settlement.Id] = p.Landmark;
        }

        /// <summary>A locality's landmark, or null for a game settlement (its archetype's own landmark).</summary>
        public static Landmark? LandmarkOf(string id) => landmarks.TryGetValue(id, out var l) ? l : (Landmark?)null;
    }
}
