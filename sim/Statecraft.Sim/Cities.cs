using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 cities (docs/GDD.md "Cities and construction"; Phase 1 PR 3): civilians and refugees are counts
    /// per city (D-A2), in population units (D-A1). Runs only when eras.yaml systems.loop is gdd.</summary>
    public static class Cities
    {
        public const string Kind = "assign_workers";

        /// <summary>Fills the state's cities from cities.yaml, with no refugees yet. Does nothing in the legacy loop.</summary>
        public static void Init(Content c, GameState s)
        {
            if (!c.IsGdd) return;
            foreach (var x in c.Cities) s.Cities[x.Id] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, 0);
        }

        /// <summary>R-CITY-001: sets the number of the city's civilians in jobs (a standing order). Refused, state untouched,
        /// when the city or role is unknown or the count is negative, above the job slots or above the civilians. Step has
        /// already refused it in the legacy loop.</summary>
        public static string? Assign(Content c, GameState s, string city, string role, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (role != "job") return "There is no such role";   // craftsman: Phase 3 (D-D4)
            if (count < 0) return "The number of workers cannot be negative";
            if (count > x.JobSlots) return FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.JobSlots} jobs");
            if (count > x.Civilians) return FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.Civilians} civilians");
            s.Cities[city] = x.WithCounts(x.Civilians, count, x.Refugees);
            why.Add(new Cause("city", city, count - x.Employed, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: {count} groups in work")));
            return null;
        }
    }
}
