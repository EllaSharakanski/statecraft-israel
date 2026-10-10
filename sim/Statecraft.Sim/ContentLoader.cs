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
            .WithDuplicateKeyChecking()   // two equal keys would otherwise silently keep only the last value
            .Build();

        static T Read<T>(Func<string, string?> readFile, string relPath)
        {
            string? text;
            try { text = readFile(relPath); }
            catch (IOException e) { throw new ContentException(relPath, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(relPath, "(file)", e.Message); }
            if (text == null) throw new ContentException(relPath, "(file)", "missing file");
            T result;
            try { result = Yaml.Deserialize<T>(text) ?? throw new ContentException(relPath, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(relPath, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            CheckNulls(relPath, result is GameEvent ev ? $"events[{ev.Id}]" : "", result!);
            return result;
        }

        /// <summary>A key written with no value (`tags:`) deserialises to null. Null lists and maps become load errors
        /// naming the field; null strings become "" (the default every check already handles). Walks nested content
        /// objects, naming list entries by id when they have one, else by index.</summary>
        static void CheckNulls(string file, string path, object o)
        {
            foreach (var p in o.GetType().GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal))
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

        /// <summary>Loads content through <paramref name="readFile"/>, which takes a path relative to the content root
        /// ("people.yaml", "events/x.yaml") and returns the file's text, or null when it is missing. This is the
        /// entry point for platforms without a file system (a WebGL build reads StreamingAssets through a URL).
        /// <paramref name="eventFiles"/> are the event files, relative to the content root; they load in ordinal order.</summary>
        public static Content Load(Func<string, string?> readFile, IEnumerable<string> eventFiles)
        {
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));
            if (eventFiles == null) throw new ArgumentNullException(nameof(eventFiles));
            var c = new Content();
            c.Offices = Read<OfficesFile>(readFile, "offices.yaml").Offices;
            c.Parties = Read<PartiesFile>(readFile, "parties.yaml").Parties;
            c.People = Read<PeopleFile>(readFile, "people.yaml").People;
            c.Legislatures = Read<LegislaturesFile>(readFile, "legislatures.yaml").Legislatures;
            c.Governments = Read<GovernmentsFile>(readFile, "governments.yaml").Governments;
            var m = Read<ModelFile>(readFile, "model.yaml");
            c.Indicators = m.Indicators; c.Policies = m.Policies; c.Effects = m.Effects;
            c.Situations = m.Situations; c.VoterGroups = m.VoterGroups; c.Actors = m.Actors;
            c.Rules = Read<Rules>(readFile, "rules.yaml");
            c.Aliyah = Read<AliyahFile>(readFile, "aliyah.yaml").Series;
            c.PopulationAnchors = Read<PopulationFile>(readFile, "population.yaml").Anchors;
            foreach (var rel in eventFiles.OrderBy(p => p, StringComparer.Ordinal))
            {
                var e = Read<GameEvent>(readFile, rel);
                e.File = rel;
                c.Events.Add(e);
            }
            Validate(c);
            return c;
        }

        /// <summary>Loads content from a directory: every events/*.yaml file is an event.</summary>
        public static Content Load(string contentRoot)
        {
            var eventsDir = Path.Combine(contentRoot, "events");
            var eventFiles = Directory.Exists(eventsDir)
                ? Directory.GetFiles(eventsDir, "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList()
                : new List<string>();
            return Load(rel =>
            {
                var path = Path.Combine(contentRoot, rel);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }, eventFiles);
        }

        internal const string IdRule = "must be lowercase letters, digits and underscores, starting with a letter";

        /// <summary>Every id has the office-id shape (so it is safe in placeholders, CLI input and save files) and is unique in its list.</summary>
        static void Unique(string file, string list, IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                if (!Offices.OfficeId.IsMatch(id)) throw new ContentException(file, $"{list}[{id}].id", IdRule);
                if (!seen.Add(id)) throw new ContentException(file, $"{list}[{id}].id", "duplicate id");
            }
        }

        static readonly HashSet<string> Statuses = new HashSet<string> { "draft", "fact-checked", "historian-reviewed", "sensitivity-reviewed" };

        internal static void Status(string file, string field, string status)
        {
            if (!Statuses.Contains(status))
                throw new ContentException(file, field, $"unknown status '{status}' (must be draft, fact-checked, historian-reviewed or sensitivity-reviewed)");
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        internal static void Finite(string file, string field, double v)
        {
            if (!IsFinite(v)) throw new ContentException(file, field, "must be a finite number");
        }

        internal static void Unit(string file, string field, double v)
        {
            if (!(v >= 0 && v <= 1)) throw new ContentException(file, field, "must be a number from 0 to 1");
        }

        static void Count(string file, string field, double v)
        {
            if (!(v >= 0 && IsFinite(v))) throw new ContentException(file, field, "must be a finite number >= 0");
        }

        /// <summary>from is required (a missing date deserialises to DateTime default); to, when given, must be after from.</summary>
        internal static void Period(string file, string path, DateTime from, DateTime? to)
        {
            if (from == default) throw new ContentException(file, path + ".from", "required");
            if (to.HasValue && to.Value <= from) throw new ContentException(file, path + ".to", FormattableString.Invariant($"must be after from ({from:yyyy-MM-dd})"));
        }

        /// <summary>Only one legislature sits, and one government holds office, on any date: "which body sat on this
        /// date" must never be ambiguous. Periods are [from, to); sorted by from, each must end on or before the next
        /// one starts (an open-ended period can only be the last).</summary>
        static void NoOverlap(string file, string list, IEnumerable<(string id, DateTime from, DateTime? to)> periods)
        {
            var sorted = periods.OrderBy(p => p.from).ThenBy(p => p.id, StringComparer.Ordinal).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                var (prev, next) = (sorted[i - 1], sorted[i]);
                if (!prev.to.HasValue || prev.to.Value > next.from)
                    throw new ContentException(file, $"{list}[{next.id}].from", FormattableString.Invariant(
                        $"overlaps {prev.id} ({prev.from:yyyy-MM-dd} to {(prev.to.HasValue ? prev.to.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "open")})"));
            }
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

            foreach (var p in c.People) Status("people.yaml", $"people[{p.Id}].status", p.Status);
            foreach (var l in c.Legislatures) Status("legislatures.yaml", $"legislatures[{l.Id}].status", l.Status);
            foreach (var g in c.Governments) Status("governments.yaml", $"governments[{g.Id}].status", g.Status);
            for (int i = 0; i < c.Aliyah.Count; i++) Status("aliyah.yaml", FormattableString.Invariant($"series[{i}].status"), c.Aliyah[i].Status);
            for (int i = 0; i < c.PopulationAnchors.Count; i++) Status("population.yaml", FormattableString.Invariant($"anchors[{i}].status"), c.PopulationAnchors[i].Status);

            foreach (var p in c.People)
            {
                if (!p.Living.HasValue) throw new ContentException("people.yaml", $"people[{p.Id}].living", "required (true or false)");
                if (p.Competence < 0 || p.Competence > 10) throw new ContentException("people.yaml", $"people[{p.Id}].competence", "must be 0-10");
                if (p.Loyalty < 0 || p.Loyalty > 10) throw new ContentException("people.yaml", $"people[{p.Id}].loyalty", "must be 0-10");
                if (p.Party != "" && !parties.Contains(p.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].party", $"unknown party '{p.Party}'");
                foreach (var t in p.Offices)
                {
                    if (string.IsNullOrEmpty(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices", "office term without an office");
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.From == default) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.from", "required");
                    if (t.Party != "" && !parties.Contains(t.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.party", $"unknown party '{t.Party}'");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", FormattableString.Invariant($"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})"));
                }
            }
            foreach (var o in c.Offices.Where(o => o.Until.HasValue))
                foreach (var p in c.People)
                    foreach (var t in p.Offices.Where(t => t.Office == o.Id))
                        if (!t.To.HasValue || t.To.Value > o.Until!.Value)
                            throw new ContentException("people.yaml", $"people[{p.Id}].offices.{o.Id}.to", FormattableString.Invariant($"must be on or before {o.Until:yyyy-MM-dd}, when the office was abolished"));
            foreach (var p in c.People.Where(p => p.Died.HasValue))
                foreach (var t in p.Offices)
                    if (!t.To.HasValue || t.To.Value > p.Died!.Value)
                        throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.to", FormattableString.Invariant($"must be on or before {p.Died:yyyy-MM-dd}, the date of death"));
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
                Period("legislatures.yaml", $"legislatures[{l.Id}]", l.From, l.To);
                if (l.Size <= 0) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", "must be > 0");
                foreach (var kv in l.Seats)
                {
                    if (!parties.Contains(kv.Key)) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "unknown party");
                    if (kv.Value < 0) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "must be >= 0");
                }
                if (l.Seats.Values.Sum() != l.Size) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", FormattableString.Invariant($"seats sum to {l.Seats.Values.Sum()}, size is {l.Size}"));
            }
            foreach (var g in c.Governments)
            {
                Period("governments.yaml", $"governments[{g.Id}]", g.From, g.To);
                foreach (var p in g.Coalition)
                    if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"governments[{g.Id}].coalition.{p}", "unknown party");
            }
            NoOverlap("legislatures.yaml", "legislatures", c.Legislatures.Select(l => (l.Id, l.From, l.To)));
            NoOverlap("governments.yaml", "governments", c.Governments.Select(g => (g.Id, g.From, g.To)));
            foreach (var i in c.Indicators)
            {
                Finite("model.yaml", $"indicators[{i.Id}].base", i.Base);
                Unit("model.yaml", $"indicators[{i.Id}].base", i.Base);
            }
            foreach (var p in c.Policies)
            {
                Finite("model.yaml", $"policies[{p.Id}].default", p.Default);
                Unit("model.yaml", $"policies[{p.Id}].default", p.Default);
            }
            foreach (var p in c.Policies)
                if (!offices.Contains(p.Office)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", $"unknown office '{p.Office}'");
            foreach (var e in c.Effects)
            {
                if (!sources.Contains(e.Source)) throw new ContentException("model.yaml", $"effects[{e.Id}].source", $"unknown policy or indicator '{e.Source}'");
                if (!indicators.Contains(e.Target)) throw new ContentException("model.yaml", $"effects[{e.Id}].target", $"unknown indicator '{e.Target}'");
                Finite("model.yaml", $"effects[{e.Id}].k", e.K);
                if (e.Inertia < 1) throw new ContentException("model.yaml", $"effects[{e.Id}].inertia", "must be >= 1");
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", $"unknown office '{e.Office}'");
            }
            foreach (var s in c.Situations)
            {
                if (!indicators.Contains(s.Indicator)) throw new ContentException("model.yaml", $"situations[{s.Id}].indicator", $"unknown indicator '{s.Indicator}'");
                bool below = s.OnBelow.HasValue && s.OffAbove.HasValue, above = s.OnAbove.HasValue && s.OffBelow.HasValue;
                if (below == above) throw new ContentException("model.yaml", $"situations[{s.Id}]", "needs exactly one pair: on_below+off_above or on_above+off_below");
                foreach (var (key, v) in new[] { ("on_below", s.OnBelow), ("off_above", s.OffAbove), ("on_above", s.OnAbove), ("off_below", s.OffBelow) })
                    if (v.HasValue) Unit("model.yaml", $"situations[{s.Id}].{key}", v.Value);
            }
            foreach (var v in c.VoterGroups)
                foreach (var kv in v.Weights)
                {
                    if (!indicators.Contains(kv.Key)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{kv.Key}", "unknown indicator");
                    Finite("model.yaml", $"voter_groups[{v.Id}].weights.{kv.Key}", kv.Value);
                }
            foreach (var a in c.Actors) Unit("model.yaml", $"actors[{a.Id}].standing", a.Standing);
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
            for (int i = 0; i < 5; i++)
            {
                var field = FormattableString.Invariant($"rung_min_mobilization[{i}]");
                Unit("rules.yaml", field, c.Rules.RungMinMobilization[i]);
                if (i > 0 && c.Rules.RungMinMobilization[i] < c.Rules.RungMinMobilization[i - 1])
                    throw new ContentException("rules.yaml", field, "must not be below the previous rung's floor");
            }
            if (c.Rules.StartRung < 1 || c.Rules.StartRung > 5) throw new ContentException("rules.yaml", "start_rung", "must be 1-5");
            if (c.Rules.CeasefireWeeks <= 0) throw new ContentException("rules.yaml", "ceasefire_weeks", "must be > 0");
            var r = c.Rules;
            foreach (var (key, v) in new[] { ("treasury_start", r.TreasuryStart), ("fx_start", r.FxStart), ("tax_per_capita_per_year", r.TaxPerCapitaPerYear),
                ("spending_per_capita_per_year", r.SpendingPerCapitaPerYear), ("exports_per_year", r.ExportsPerYear), ("imports_per_capita_per_year", r.ImportsPerCapitaPerYear),
                ("mobilization_cost_per_year", r.MobilizationCostPerYear), ("natural_increase_per_year", r.NaturalIncreasePerYear),
                ("capital_inflow_per_year", r.CapitalInflowPerYear), ("fx_crisis_food_per_year", r.FxCrisisFoodPerYear) })
                Finite("rules.yaml", key, v);
            Unit("rules.yaml", "appoint_stability_cost_other_party", r.AppointStabilityCostOtherParty);
            Unit("rules.yaml", "appoint_stability_cost_same_party", r.AppointStabilityCostSameParty);
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
            if (r.FxCrisisFoodPerYear < 0 && !indicators.Contains("food_supply")) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "needs a food_supply indicator in model.yaml");
            foreach (var kv in r.InitialPopulation) Count("rules.yaml", $"initial_population.{kv.Key}", kv.Value);
            foreach (var kv in r.BudgetStart)
            {
                if (!offices.Contains(kv.Key)) throw new ContentException("rules.yaml", $"budget_start.{kv.Key}", "unknown office");
                // A budget line must not silently stop mattering when its office is abolished: move it to the office that
                // took over (plan amendment C3).
                var until = c.Offices.First(o => o.Id == kv.Key).Until;
                if (until.HasValue)
                    throw new ContentException("rules.yaml", $"budget_start.{kv.Key}", FormattableString.Invariant($"office '{kv.Key}' is abolished on {until:yyyy-MM-dd}: give its budget line to the office that took over"));
                Unit("rules.yaml", $"budget_start.{kv.Key}", kv.Value);
            }
            // The same rule EconomySystem.Allocate applies to every later allocation.
            if (r.BudgetStart.Count > 0 && Math.Abs(r.BudgetStart.Values.Sum() - 1.0) > 1e-6) throw new ContentException("rules.yaml", "budget_start", "shares must sum to 1");
            for (int i = 0; i < c.Aliyah.Count; i++)
            {
                var a = c.Aliyah[i]; var path = FormattableString.Invariant($"series[{i}]");
                if (a.From == default) throw new ContentException("aliyah.yaml", path + ".from", "required");
                if (a.To < a.From) throw new ContentException("aliyah.yaml", path + ".to", "required, on or after from (both dates are inclusive)");
                foreach (var kv in a.Origins) Count("aliyah.yaml", $"{path}.origins.{kv.Key}", kv.Value);
            }
            for (int i = 0; i < c.PopulationAnchors.Count; i++)
            {
                var a = c.PopulationAnchors[i]; var path = FormattableString.Invariant($"anchors[{i}]");
                if (a.Date == default) throw new ContentException("population.yaml", path + ".date", "required");
                Count("population.yaml", path + ".jews", a.Jews);
            }
            EventRules.Validate(c, offices, indicators);
        }
    }
}
