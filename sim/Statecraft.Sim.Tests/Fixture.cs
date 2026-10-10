using System;
using System.Collections.Generic;
using System.IO;

/// <summary>Writes a small, valid content set to a temp dir. Tests override single files to test errors.</summary>
public static class Fixture
{
    public static readonly Dictionary<string, string> Files = new Dictionary<string, string>
    {
        ["offices.yaml"] = @"
offices:
  - id: prime_minister
    title: Prime Minister
  - id: defense_minister
    title: Minister of Defense
  - id: finance_minister
    title: Minister of Finance
",
        ["parties.yaml"] = @"
parties:
  - id: alpha
    name: Alpha
  - id: beta
    name: Beta
",
        ["people.yaml"] = @"
people:
  - id: pm_one
    name: Pm One
    living: false
    competence: 8
    loyalty: 9
    offices:
      - office: prime_minister
        from: 1948-05-14
      - office: defense_minister
        from: 1948-05-14
        to: 1949-01-01
  - id: def_two
    name: Def Two
    living: false
    competence: 4
    loyalty: 5
    offices:
      - office: defense_minister
        from: 1949-01-01
      - office: finance_minister
        from: 1948-05-14
",
        ["legislatures.yaml"] = @"
legislatures:
  - id: council
    body: Council
    from: 1948-05-14
    to: 1949-02-14
    size: 5
    budget_approval: false
    seats: { alpha: 3, beta: 2 }
  - id: house
    body: House
    from: 1949-02-14
    size: 10
    budget_approval: true
    seats: { alpha: 4, beta: 6 }
",
        ["governments.yaml"] = @"
governments:
  - id: gov1
    name: Government One
    from: 1948-05-14
    to: 1949-03-10
    coalition: [alpha]
  - id: gov2
    name: Government Two
    from: 1949-03-10
    coalition: [alpha, beta]
",
        ["model.yaml"] = @"
indicators:
  - id: morale
    base: 0.5
  - id: food_supply
    base: 0.4
policies:
  - id: rationing
    default: 0.0
    office: finance_minister
effects:
  - id: rationing_food
    source: rationing
    target: food_supply
    k: 0.4
    inertia: 2
    office: finance_minister
  - id: rationing_morale
    source: rationing
    target: morale
    k: -0.2
    inertia: 1
situations:
  - id: hunger
    indicator: food_supply
    on_below: 0.3
    off_above: 0.45
voter_groups:
  - id: everyone
    weights: { morale: 1.0 }
actors:
  - id: usa
    name: United States
    standing: 0.5
",
        ["rules.yaml"] = @"
start: 1948-05-15
start_rung: 5
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 1 }
appoint_stability_cost_other_party: 0.1
appoint_stability_cost_same_party: 0.03
rung_attention_step: 0.25
ceasefire_weeks: 4
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
treasury_start: 100
fx_start: 50
tax_per_capita_per_year: 0.0004
spending_per_capita_per_year: 0.0005
exports_per_year: 20
imports_per_capita_per_year: 0.0001
mobilization_cost_per_year: 40
natural_increase_per_year: 0.017
initial_population: { veterans: 1000 }
budget_start: { finance_minister: 1.0 }
",
        ["population.yaml"] = @"
anchors: []
",
        ["aliyah.yaml"] = @"
series:
  - from: 1948-05-15
    to: 1948-12-31
    origins: { europe: 230, asia: 0 }
",
    };

    public static string Write(Dictionary<string, string>? overrides = null, Dictionary<string, string>? events = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "events"));
        foreach (var kv in Files)
            File.WriteAllText(Path.Combine(dir, kv.Key), overrides != null && overrides.TryGetValue(kv.Key, out var o) ? o : kv.Value);
        if (events != null)
            foreach (var kv in events) File.WriteAllText(Path.Combine(dir, "events", kv.Key), kv.Value);
        return dir;
    }
}
