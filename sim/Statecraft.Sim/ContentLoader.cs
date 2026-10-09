using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            T result;
            try { result = Yaml.Deserialize<T>(File.ReadAllText(path)) ?? throw new ContentException(relPath, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(relPath, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            catch (IOException e) { throw new ContentException(relPath, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(relPath, "(file)", e.Message); }
            CheckNulls(relPath, result is GameEvent ev ? $"events[{ev.Id}]" : "", result!);
            return result;
        }

        /// <summary>A key written with no value (`tags:`) deserialises to null. Null lists and maps become load errors
        /// naming the field; null strings become "" (the default every check already handles). Walks nested content
        /// objects, naming list entries by id when they have one, else by index.</summary>
        static void CheckNulls(string file, string path, object o)
        {
            foreach (var p in o.GetType().GetProperties())
            {
                if (p.GetCustomAttribute<YamlIgnoreAttribute>() != null) continue;
                var key = (path == "" ? "" : path + ".") + UnderscoredNamingConvention.Instance.Apply(p.Name);
                var t = p.PropertyType;
                var v = p.GetValue(o);
                if (t == typeof(string)) { if (v == null) p.SetValue(o, ""); }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (v == null) throw new ContentException(file, key, "must be a list");
                    var list = (IList)v;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item == null) throw new ContentException(file, FormattableString.Invariant($"{key}[{i}]"), "empty entry");
                        if (!IsContentType(item.GetType())) continue;
                        var id = item.GetType().GetProperty("Id")?.GetValue(item) as string;
                        CheckNulls(file, string.IsNullOrEmpty(id) ? FormattableString.Invariant($"{key}[{i}]") : $"{key}[{id}]", item);
                    }
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    if (v == null) throw new ContentException(file, key, "must be a map");
                }
                else if (v != null && IsContentType(t)) CheckNulls(file, key, v);
            }
        }

        static bool IsContentType(Type t) => t.IsClass && t != typeof(string) && t.Namespace == typeof(Content).Namespace;

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
                {
                    var rel = "events/" + Path.GetFileName(f);
                    var e = Read<GameEvent>(contentRoot, rel);
                    e.File = rel;
                    c.Events.Add(e);
                }
            Validate(c);
            return c;
        }

        static void Unique(string file, string list, IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
                if (!seen.Add(id)) throw new ContentException(file, $"{list}[{id}].id", "duplicate id");
        }

        static readonly HashSet<string> Statuses = new HashSet<string> { "draft", "fact-checked", "historian-reviewed", "sensitivity-reviewed" };

        internal static void Status(string file, string field, string status)
        {
            if (!Statuses.Contains(status))
                throw new ContentException(file, field, $"unknown status '{status}' (must be draft, fact-checked, historian-reviewed or sensitivity-reviewed)");
        }

        static void Validate(Content c)
        {
            var offices = new HashSet<string>(c.Offices.Select(o => o.Id));
            var parties = new HashSet<string>(c.Parties.Select(p => p.Id));
            var indicators = new HashSet<string>(c.Indicators.Select(i => i.Id));
            var sources = new HashSet<string>(c.Policies.Select(p => p.Id).Concat(indicators));
            Unique("offices.yaml", "offices", c.Offices.Select(o => o.Id));
            foreach (var o in c.Offices)
                if (!Offices.OfficeId.IsMatch(o.Id)) throw new ContentException("offices.yaml", $"offices[{o.Id}].id", "must be lowercase letters, digits and underscores, starting with a letter");
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

            foreach (var p in c.People) Status("people.yaml", $"people[{p.Id}].status", p.Status);
            foreach (var l in c.Legislatures) Status("legislatures.yaml", $"legislatures[{l.Id}].status", l.Status);
            foreach (var g in c.Governments) Status("governments.yaml", $"governments[{g.Id}].status", g.Status);
            for (int i = 0; i < c.Aliyah.Count; i++) Status("aliyah.yaml", FormattableString.Invariant($"series[{i}].status"), c.Aliyah[i].Status);
            for (int i = 0; i < c.PopulationAnchors.Count; i++) Status("population.yaml", FormattableString.Invariant($"anchors[{i}].status"), c.PopulationAnchors[i].Status);

            foreach (var p in c.People)
            {
                if (p.Party != "" && !parties.Contains(p.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].party", $"unknown party '{p.Party}'");
                foreach (var t in p.Offices)
                {
                    if (string.IsNullOrEmpty(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices", "office term without an office");
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.Party != "" && !parties.Contains(t.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.party", $"unknown party '{t.Party}'");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", FormattableString.Invariant($"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})"));
                }
            }
            foreach (var p in c.People)
                for (int i = 0; i < p.Offices.Count; i++)
                    for (int j = i + 1; j < p.Offices.Count; j++)
                    {
                        var (a, b) = (p.Offices[i], p.Offices[j]);
                        if (a.Party == "" || b.Party == "" || a.Party == b.Party) continue;
                        if (a.From < (b.To ?? DateTime.MaxValue) && b.From < (a.To ?? DateTime.MaxValue))
                            throw new ContentException("people.yaml", $"people[{p.Id}].offices", "overlapping terms disagree on party");
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
                if (l.Seats.Values.Sum() != l.Size) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", FormattableString.Invariant($"seats sum to {l.Seats.Values.Sum()}, size is {l.Size}"));
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
            if (c.Rules.Start == default) throw new ContentException("rules.yaml", "start", "required");
            if (!(c.Rules.AttentionPerTurn >= 0 && !double.IsInfinity(c.Rules.AttentionPerTurn))) throw new ContentException("rules.yaml", "attention_per_turn", "must be a finite non-negative number");
            if (!(c.Rules.RungAttentionStep >= 0 && !double.IsInfinity(c.Rules.RungAttentionStep))) throw new ContentException("rules.yaml", "rung_attention_step", "must be a finite non-negative number");
            foreach (var kv in c.Rules.AttentionCost)
                if (!(kv.Value >= 0 && !double.IsInfinity(kv.Value))) throw new ContentException("rules.yaml", $"attention_cost.{kv.Key}", "must be a finite non-negative number");
            foreach (var k in c.Rules.AttentionCost.Keys)
                if (!AttentionSystem.IsCommandKind(k)) throw new ContentException("rules.yaml", $"attention_cost.{k}", "unknown command kind");
            foreach (var k in AttentionSystem.CommandKinds)
                if (!c.Rules.AttentionCost.ContainsKey(k)) throw new ContentException("rules.yaml", $"attention_cost.{k}", "missing cost");
            if (c.Rules.RungMinMobilization.Count != 5) throw new ContentException("rules.yaml", "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
            foreach (var k in c.Rules.BudgetStart.Keys)
                if (!offices.Contains(k)) throw new ContentException("rules.yaml", $"budget_start.{k}", "unknown office");
            EventRules.Validate(c, offices, indicators);
        }
    }
}
