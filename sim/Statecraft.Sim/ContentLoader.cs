using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    public static class ContentLoader
    {
        sealed class OfficesFile { public List<Office> Offices { get; set; } = new List<Office>(); }
        sealed class PartiesFile { public List<Party> Parties { get; set; } = new List<Party>(); }
        sealed class PeopleFile { public List<Person> People { get; set; } = new List<Person>(); }
        sealed class LegislaturesFile { public List<Legislature> Legislatures { get; set; } = new List<Legislature>(); }
        sealed class GovernmentsFile { public List<Government> Governments { get; set; } = new List<Government>(); }
        sealed class ModelFile
        {
            public List<Indicator> Indicators { get; set; } = new List<Indicator>();
            public List<Policy> Policies { get; set; } = new List<Policy>();
            public List<Effect> Effects { get; set; } = new List<Effect>();
            public List<Situation> Situations { get; set; } = new List<Situation>();
            public List<VoterGroup> VoterGroups { get; set; } = new List<VoterGroup>();
            public List<Actor> Actors { get; set; } = new List<Actor>();
        }
        sealed class AliyahFile { public List<AliyahPeriod> Series { get; set; } = new List<AliyahPeriod>(); }
        sealed class PopulationFile { public List<PopulationAnchor> Anchors { get; set; } = new List<PopulationAnchor>(); }

        static readonly IDeserializer Yaml = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        static T Read<T>(string root, string relPath)
        {
            var path = Path.Combine(root, relPath);
            if (!File.Exists(path)) throw new ContentException(relPath, "(file)", "missing file");
            try { return Yaml.Deserialize<T>(File.ReadAllText(path)) ?? throw new ContentException(relPath, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(relPath, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
        }

        public static Content Load(string contentRoot)
        {
            var c = new Content();
            c.Offices = Read<OfficesFile>(contentRoot, "offices.yaml").Offices;
            c.Parties = Read<PartiesFile>(contentRoot, "parties.yaml").Parties;
            c.People = Read<PeopleFile>(contentRoot, "people.yaml").People;
            c.Legislatures = Read<LegislaturesFile>(contentRoot, "legislatures.yaml").Legislatures;
            c.Governments = Read<GovernmentsFile>(contentRoot, "governments.yaml").Governments;
            var m = Read<ModelFile>(contentRoot, "model.yaml");
            c.Indicators = m.Indicators; c.Policies = m.Policies; c.Effects = m.Effects;
            c.Situations = m.Situations; c.VoterGroups = m.VoterGroups; c.Actors = m.Actors;
            c.Rules = Read<Rules>(contentRoot, "rules.yaml");
            c.Aliyah = Read<AliyahFile>(contentRoot, "aliyah.yaml").Series;
            c.PopulationAnchors = Read<PopulationFile>(contentRoot, "population.yaml").Anchors;
            var eventsDir = Path.Combine(contentRoot, "events");
            if (Directory.Exists(eventsDir))
                foreach (var f in Directory.GetFiles(eventsDir, "*.yaml").OrderBy(p => p, StringComparer.Ordinal))
                    c.Events.Add(Read<GameEvent>(contentRoot, Path.Combine("events", Path.GetFileName(f))));
            Validate(c);
            return c;
        }

        static void Unique(string file, string list, IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
                if (!seen.Add(id)) throw new ContentException(file, $"{list}[{id}].id", "duplicate id");
        }

        static void Validate(Content c)
        {
            var offices = new HashSet<string>(c.Offices.Select(o => o.Id));
            var parties = new HashSet<string>(c.Parties.Select(p => p.Id));
            var indicators = new HashSet<string>(c.Indicators.Select(i => i.Id));
            var sources = new HashSet<string>(c.Policies.Select(p => p.Id).Concat(indicators));
            Unique("offices.yaml", "offices", c.Offices.Select(o => o.Id));
            Unique("parties.yaml", "parties", c.Parties.Select(p => p.Id));
            Unique("people.yaml", "people", c.People.Select(p => p.Id));
            Unique("legislatures.yaml", "legislatures", c.Legislatures.Select(l => l.Id));
            Unique("governments.yaml", "governments", c.Governments.Select(g => g.Id));
            Unique("model.yaml", "indicators", c.Indicators.Select(i => i.Id));
            Unique("model.yaml", "policies", c.Policies.Select(p => p.Id));
            Unique("model.yaml", "effects", c.Effects.Select(e => e.Id));
            Unique("model.yaml", "situations", c.Situations.Select(s => s.Id));
            Unique("model.yaml", "voter_groups", c.VoterGroups.Select(v => v.Id));
            Unique("model.yaml", "actors", c.Actors.Select(a => a.Id));
            foreach (var p in c.Policies)
                if (indicators.Contains(p.Id)) throw new ContentException("model.yaml", $"policies[{p.Id}].id", "same id as an indicator (effect sources would be ambiguous)");

            foreach (var p in c.People)
            {
                if (p.Party != "" && !parties.Contains(p.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].party", $"unknown party '{p.Party}'");
                foreach (var t in p.Offices)
                {
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", $"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})");
                }
            }
            var terms = c.People.SelectMany(p => p.Offices.Select(t => (person: p.Id, term: t))).ToList();
            for (int i = 0; i < terms.Count; i++)
                for (int j = i + 1; j < terms.Count; j++)
                {
                    var (a, b) = (terms[i], terms[j]);
                    if (a.term.Office != b.term.Office || a.person == b.person) continue;
                    if (a.term.From < (b.term.To ?? DateTime.MaxValue) && b.term.From < (a.term.To ?? DateTime.MaxValue))
                        throw new ContentException("people.yaml", $"offices.{a.term.Office}", $"held by both {a.person} and {b.person} on overlapping dates");
                }
            foreach (var l in c.Legislatures)
            {
                foreach (var kv in l.Seats)
                    if (!parties.Contains(kv.Key)) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "unknown party");
                if (l.Seats.Values.Sum() != l.Size) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", $"seats sum to {l.Seats.Values.Sum()}, size is {l.Size}");
            }
            foreach (var g in c.Governments)
                foreach (var p in g.Coalition)
                    if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"governments[{g.Id}].coalition.{p}", "unknown party");
            foreach (var p in c.Policies)
                if (!offices.Contains(p.Office)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", $"unknown office '{p.Office}'");
            foreach (var e in c.Effects)
            {
                if (!sources.Contains(e.Source)) throw new ContentException("model.yaml", $"effects[{e.Id}].source", $"unknown policy or indicator '{e.Source}'");
                if (!indicators.Contains(e.Target)) throw new ContentException("model.yaml", $"effects[{e.Id}].target", $"unknown indicator '{e.Target}'");
                if (e.Inertia < 1) throw new ContentException("model.yaml", $"effects[{e.Id}].inertia", "must be >= 1");
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", $"unknown office '{e.Office}'");
            }
            foreach (var s in c.Situations)
            {
                if (!indicators.Contains(s.Indicator)) throw new ContentException("model.yaml", $"situations[{s.Id}].indicator", $"unknown indicator '{s.Indicator}'");
                bool below = s.OnBelow.HasValue && s.OffAbove.HasValue, above = s.OnAbove.HasValue && s.OffBelow.HasValue;
                if (below == above) throw new ContentException("model.yaml", $"situations[{s.Id}]", "needs exactly one pair: on_below+off_above or on_above+off_below");
            }
            foreach (var v in c.VoterGroups)
                foreach (var k in v.Weights.Keys)
                    if (!indicators.Contains(k)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{k}", "unknown indicator");
            if (c.Rules.RungMinMobilization.Count != 5) throw new ContentException("rules.yaml", "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
            foreach (var k in c.Rules.BudgetStart.Keys)
                if (!offices.Contains(k)) throw new ContentException("rules.yaml", $"budget_start.{k}", "unknown office");
            EventRules.Validate(c, offices, indicators);
        }
    }
}
