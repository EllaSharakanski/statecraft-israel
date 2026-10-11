#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>What the region renderer draws on a date (owner rulings 2026-10-11): the Mandate sub-districts
    /// (<see cref="Subdistricts1948"/>) from 15 May 1948, and each front's 1949 armistice line from its agreement's date
    /// (<see cref="Armistice1949"/>), only for fronts whose line exists. A front's line replaces the Mandate layer there: its
    /// zones (the Demilitarized Zones) are drawn over the sub-districts in the secondary style, and its line is named in the
    /// info panel on the hexes it passes through (where it is the Mandate frontier, as for Lebanon, the band already drawn
    /// is that line). A front without line data (Jordan) keeps the Mandate layer, and the map label says so. The layer as a
    /// whole leaves the map only on the sub-district layer's own end (<see cref="Subdistricts1948.ShownUntil"/>), which is
    /// null while any front has no line. Display only: reads no sim state but the date it is given.</summary>
    public sealed class MapRegions
    {
        /// <summary>Zones are region ids ZoneIdBase + zone id in the composed layer, apart from the sub-districts' ids.</summary>
        public const int ZoneIdBase = 1000;

        public readonly Subdistricts1948 Subdistricts;
        public readonly Armistice1949? Armistice;
        readonly Dictionary<string, RegionLayer> composed = new Dictionary<string, RegionLayer>();

        public MapRegions(Subdistricts1948 subdistricts, Armistice1949? armistice)
        {
            Subdistricts = subdistricts ?? throw new ArgumentNullException(nameof(subdistricts));
            Armistice = armistice;
        }

        /// <summary>The armistice zones drawn on <paramref name="date"/>, in file order.</summary>
        public IReadOnlyList<Armistice1949.Item> ZonesOn(DateTime date) =>
            Armistice == null ? Array.Empty<Armistice1949.Item>() : Armistice.Zones.Where(z => z.InForce(date)).ToList();

        /// <summary>The layer drawn on <paramref name="date"/>: null outside the sub-district layer's window; the
        /// sub-district layer itself while no armistice zone is in force; otherwise that layer with the zones in force drawn
        /// over it (one composed layer per set of zones, built once).</summary>
        public RegionLayer? LayerOn(DateTime date)
        {
            if (!Subdistricts.ShowsOn(date)) return null;
            var zones = ZonesOn(date);
            if (zones.Count == 0) return Subdistricts.Layer;
            var key = string.Join(",", zones.Select(z => z.Id));
            if (composed.TryGetValue(key, out var layer)) return layer;
            var hexes = Subdistricts.Layer.Hexes.ToDictionary(h => h, h => Subdistricts.Layer.RegionOf(h.q, h.r));
            var styles = hexes.Values.Distinct().ToDictionary(id => id, id => Subdistricts.Layer.StyleOf(id));
            foreach (var z in zones)
            {
                styles[ZoneIdBase + z.Id] = z.Style;
                foreach (var h in Armistice!.HexesOf(z)) hexes[h] = ZoneIdBase + z.Id;
            }
            return composed[key] = new RegionLayer(Subdistricts.Layer.Name + "+armistice_1949", hexes, styles);
        }

        bool? shownState; RegionLayer? drawn;
        /// <summary>Whether the last <see cref="Apply"/> drew a layer.</summary>
        public bool Shown => shownState == true;

        /// <summary>Draws the layer for <paramref name="date"/> (MapBorders.Use) when it differs from the one drawn; a call
        /// that changes nothing leaves the map alone. Off the window, the layer is taken off if it is still ours.</summary>
        public void Apply(DateTime date)
        {
            var layer = LayerOn(date);
            if (shownState != null && ReferenceEquals(layer, drawn)) return;
            shownState = layer != null;
            if (layer != null) MapBorders.Use(layer);
            else if (drawn != null && MapBorders.Current == drawn) MapBorders.Use(null);
            drawn = layer;
        }

        /// <summary>The fronts whose armistice line is shown on <paramref name="date"/>, and those that keep the Mandate
        /// layer for want of line data (only listed once some front's line is shown).</summary>
        public (IReadOnlyList<Armistice1949.Front> inForce, IReadOnlyList<Armistice1949.Front> noData) FrontsOn(DateTime date)
        {
            if (Armistice == null) return (Array.Empty<Armistice1949.Front>(), Array.Empty<Armistice1949.Front>());
            var on = Armistice.Fronts.Where(f => f.InForce(date)).ToList();
            var none = on.Count == 0 ? new List<Armistice1949.Front>() : Armistice.Fronts.Where(f => f.ShownFrom == null).ToList();
            return (on, none);
        }

        /// <summary>The map label: the sub-district layer's, and once a front's line is shown, those fronts and the fronts
        /// still without line data. <paramref name="withLines"/> formats {0} label, {1} armistice label, {2} front names;
        /// <paramref name="noData"/> formats {0} a front's name, appended for each front without data.</summary>
        public string Label(DateTime date, string withLines, string noData)
        {
            var (on, none) = FrontsOn(date);
            if (on.Count == 0 || Armistice == null) return Subdistricts.Label;
            var text = string.Format(CultureInfo.InvariantCulture, withLines, Subdistricts.Label, Armistice.Label, string.Join(", ", on.Select(f => f.Name)));
            foreach (var f in none) text += string.Format(CultureInfo.InvariantCulture, noData, f.Name);
            return text;
        }

        /// <summary>The agreement's date as players read it ("24 February 1949").</summary>
        public static string DateText(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
    }
}
