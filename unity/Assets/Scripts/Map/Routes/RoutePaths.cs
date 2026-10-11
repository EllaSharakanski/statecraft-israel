#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>One traced route of content/map/route_paths_1948.yaml: a railway, a main road or a track (the 1948 Burma
    /// Road), as a lon/lat polyline (issue #35; tools/routes_1948.py writes the file, routes_1948.yaml holds the sources).</summary>
    public sealed class RoutePath
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Status { get; set; } = "";
        public double LengthKm { get; set; }
        /// <summary>[lon, lat] pairs, in order along the route.</summary>
        public List<List<double>> Points { get; set; } = new List<List<double>>();
    }

    public sealed class RoutePathFile { public List<RoutePath> Routes { get; set; } = new List<RoutePath>(); }

    /// <summary>Reads route_paths_1948.yaml. The map draws it (RouteLayer); the sim never reads it, so it is not in the
    /// content hash (like settlements_1948.yaml's coordinates).</summary>
    public static class RoutePaths
    {
        public const string DefaultFileName = "route_paths_1948.yaml";
        public const string Rail = "rail", Road = "road", Track = "track";
        static readonly HashSet<string> Kinds = new HashSet<string> { Rail, Road, Track };
        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");

        /// <summary>Parses the file. Every problem throws InvalidDataException naming <paramref name="file"/> and, for
        /// content errors, the route id: YAML syntax, duplicate or unknown keys, no routes, a bad or duplicate id, an unknown
        /// kind, fewer than two points, a point that is not a [lon, lat] pair of finite numbers or lies outside
        /// <paramref name="bounds"/> when given.</summary>
        public static List<RoutePath> Parse(string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null,
            string file = DefaultFileName)
        {
            RoutePathFile doc;
            try
            {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking()
                    .Build().Deserialize<RoutePathFile>(yaml);
            }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            if (doc?.Routes == null || doc.Routes.Count == 0) throw new InvalidDataException($"{file}: no routes");
            var ids = new HashSet<string>();
            foreach (var r in doc.Routes)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.Id)) throw new InvalidDataException($"{file}: a route has an empty id");
                var at = $"{file}: '{r.Id}'";
                if (!IdRule.IsMatch(r.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!ids.Add(r.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (!Kinds.Contains(r.Kind)) throw new InvalidDataException($"{at} has unknown kind '{r.Kind}' (rail, road or track)");
                if (r.Points == null || r.Points.Count < 2) throw new InvalidDataException($"{at} needs at least two points");
                for (int i = 0; i < r.Points.Count; i++)
                {
                    var p = r.Points[i];
                    if (p == null || p.Count != 2 || !IsFinite(p[0]) || !IsFinite(p[1]))
                        throw new InvalidDataException($"{at} point {i} is not a [lon, lat] pair of numbers");
                    if (bounds is { } b && (p[0] < b.lon0 || p[0] > b.lon1 || p[1] < b.lat0 || p[1] > b.lat1))
                        throw new InvalidDataException(FormattableString.Invariant($"{at} point {i} ({p[0]}, {p[1]}) is outside the map"));
                }
            }
            return doc.Routes;
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
