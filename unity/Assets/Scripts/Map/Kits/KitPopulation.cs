#nullable enable
using System.Collections.Generic;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Population of each settlement for the size of its kit's footprint (owner ruling: towns spread over hexes in
    /// proportion to population). Presentation only: the sim never reads it.
    /// Source: the Mandate government's Village Statistics, 1945 (urban totals), as listed on
    /// https://en.wikipedia.org/wiki/Village_Statistics,_1945 (checked 2026-10-11; status owner-verify: read off the page
    /// by a tool, not yet checked against the scanned original). Degania: Deganiya 'A' only. Nahalal: "Nahalal and
    /// Shimron" combined. Revivim (founded 1943) and Yad Mordechai (1943) are not on the page: they take the smallest
    /// footprint. When settlements_1948.yaml gains a sourced population field, this table moves there.</summary>
    public static class KitPopulation
    {
        public const string SourceUrl = "https://en.wikipedia.org/wiki/Village_Statistics,_1945";

        static readonly Dictionary<string, int> People = new Dictionary<string, int>
        {
            ["jerusalem"] = 157080, ["tel_aviv"] = 166660, ["jaffa"] = 94310, ["haifa"] = 138300, ["acre"] = 12360,
            ["tiberias"] = 11310, ["safed"] = 11930, ["nazareth"] = 14200, ["lod"] = 16780, ["ramla"] = 15160,
            ["gaza"] = 34250, ["beersheba"] = 5570, ["petah_tikva"] = 17250, ["rishon_lezion"] = 8100,
            ["zikhron_yaakov"] = 1740, ["rosh_pinna"] = 340, ["rehovot"] = 10020, ["hadera"] = 7830, ["netanya"] = 5070,
            ["degania_alef"] = 290, ["ein_harod"] = 1060, ["nahalal"] = 1090, ["hanita"] = 240,
        };

        /// <summary>The 1945 population, or 0 when the source has none (the kit then takes the smallest footprint).</summary>
        public static int Of(string id) => People.TryGetValue(id, out var n) ? n : 0;
    }
}
