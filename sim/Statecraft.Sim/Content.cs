using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    // Plain data classes filled by YamlDotNet (UnderscoredNamingConvention: SeatsByParty <- seats_by_party).

    public sealed class Source { public string Url { get; set; } = ""; public DateTime Checked { get; set; } }

    public sealed class Office { public string Id { get; set; } = ""; public string Title { get; set; } = ""; }

    public sealed class Party { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }

    public sealed class OfficeTerm
    {
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    public sealed class Portrait { public string File { get; set; } = ""; public string License { get; set; } = ""; public string Source { get; set; } = ""; }

    public sealed class Person
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameHe { get; set; } = "";
        public bool Living { get; set; }
        public int Competence { get; set; }
        public int Loyalty { get; set; }
        public string Party { get; set; } = "";
        public Portrait? Portrait { get; set; }
        public List<OfficeTerm> Offices { get; set; } = new List<OfficeTerm>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Legislature
    {
        public string Id { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime? Elected { get; set; }
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public int Size { get; set; }
        public bool BudgetApproval { get; set; }
        public Dictionary<string, int> Seats { get; set; } = new Dictionary<string, int>();
        /// <summary>Fields of this record not yet verified (spec section 5 "open fact"). Kept explicit and tested.</summary>
        public List<string> OpenQuestions { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Government
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<string> Coalition { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Indicator { public string Id { get; set; } = ""; public double Base { get; set; } }

    public sealed class Policy
    {
        public string Id { get; set; } = "";
        public double Default { get; set; }
        public string Office { get; set; } = "";
    }

    /// <summary>Additive effect: its contribution to Target moves toward K * source value over Inertia turns.</summary>
    public sealed class Effect
    {
        public string Id { get; set; } = "";
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public double K { get; set; }
        public int Inertia { get; set; } = 1;
        public string Office { get; set; } = "";
    }

    /// <summary>Turns on past one threshold and off past another (hysteresis), so it does not flicker.</summary>
    public sealed class Situation
    {
        public string Id { get; set; } = "";
        public string Indicator { get; set; } = "";
        public double? OnBelow { get; set; }
        public double? OffAbove { get; set; }
        public double? OnAbove { get; set; }
        public double? OffBelow { get; set; }
    }

    public sealed class VoterGroup { public string Id { get; set; } = ""; public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>(); }

    public sealed class Actor { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double Standing { get; set; } }

    public sealed class Rules
    {
        public DateTime Start { get; set; }
        public int StartRung { get; set; } = 1;
        public double AttentionPerTurn { get; set; }
        public Dictionary<string, double> AttentionCost { get; set; } = new Dictionary<string, double>();
        public double RungAttentionStep { get; set; }
        public int CeasefireWeeks { get; set; }
        public List<double> RungMinMobilization { get; set; } = new List<double>();
        public double TreasuryStart { get; set; }
        public double FxStart { get; set; }
        public double TaxPerCapitaPerYear { get; set; }
        public double SpendingPerCapitaPerYear { get; set; }
        public double ExportsPerYear { get; set; }
        public double ImportsPerCapitaPerYear { get; set; }
        public double MobilizationCostPerYear { get; set; }
        public double NaturalIncreasePerYear { get; set; }
        public Dictionary<string, double> InitialPopulation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> BudgetStart { get; set; } = new Dictionary<string, double>();
    }

    public sealed class AliyahPeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, double> Origins { get; set; } = new Dictionary<string, double>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class PopulationAnchor
    {
        public DateTime Date { get; set; }
        public double Jews { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class EventCondition { public string Indicator { get; set; } = ""; public double? Min { get; set; } public double? Max { get; set; } }

    public sealed class OptionEffect { public string Target { get; set; } = ""; public double Delta { get; set; } }

    public sealed class EventOption
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public bool Historical { get; set; }
        public bool Recommended { get; set; }
        public List<OptionEffect> Effects { get; set; } = new List<OptionEffect>();
        public int? SetRung { get; set; }
    }

    public sealed class GameEvent
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public string Anchor { get; set; } = "soft";
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<EventCondition> Conditions { get; set; } = new List<EventCondition>();
        public List<EventOption> Options { get; set; } = new List<EventOption>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>Path relative to the content root, set by the loader (not read from YAML).</summary>
        [YamlIgnore] public string File { get; set; } = "";
    }

    public sealed class Content
    {
        public List<Office> Offices = new List<Office>();
        public List<Party> Parties = new List<Party>();
        public List<Person> People = new List<Person>();
        public List<Legislature> Legislatures = new List<Legislature>();
        public List<Government> Governments = new List<Government>();
        public List<Indicator> Indicators = new List<Indicator>();
        public List<Policy> Policies = new List<Policy>();
        public List<Effect> Effects = new List<Effect>();
        public List<Situation> Situations = new List<Situation>();
        public List<VoterGroup> VoterGroups = new List<VoterGroup>();
        public List<Actor> Actors = new List<Actor>();
        public Rules Rules = new Rules();
        public List<AliyahPeriod> Aliyah = new List<AliyahPeriod>();
        public List<PopulationAnchor> PopulationAnchors = new List<PopulationAnchor>();
        public List<GameEvent> Events = new List<GameEvent>();
    }

    public sealed class ContentException : Exception
    {
        public string File { get; }
        public string Field { get; }
        public ContentException(string file, string field, string message) : base($"{file}: {field}: {message}")
        { File = file; Field = field; }
    }
}
