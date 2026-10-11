#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.Map.Units
{
    /// <summary>A token on its hex.</summary>
    public readonly struct PlacedToken : IEquatable<PlacedToken>
    {
        public readonly UnitToken Token; public readonly int Q, R;
        public PlacedToken(UnitToken token, int q, int r) { Token = token; Q = q; R = r; }
        public bool Equals(PlacedToken o) => Token.Equals(o.Token) && Q == o.Q && R == o.R;
        public override bool Equals(object? o) => o is PlacedToken p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(Token, Q, R);
        public override string ToString() => $"{Token} ({Q},{R})";
    }

    /// <summary>Puts each token on one hex: the nearest hex to its anchor (by distance from the anchor point) that is land
    /// (sea for a ship), holds no other token and is clear (the caller's test: no settlement buildings under the token, no
    /// railway or road under its figures), at most <see cref="MaxShift"/> hexes from the anchor's own hex. A token with no
    /// such hex is left out and reported. Tokens are placed in the data's order, so the result is the same every time.
    /// The shift is a presentation offset, not a claim about where the unit stood (docs/units.md).</summary>
    public static class UnitPlacer
    {
        public const int MaxShift = 3;

        /// <param name="toXZ">lon, lat to the map's ground plane (metres), for distances.</param>
        /// <param name="clear">True when the token may stand on the hex with that centre (q, r).</param>
        public static List<PlacedToken> Place(IReadOnlyList<UnitToken> tokens, IReadOnlyList<MapHex> hexes,
            Func<double, double, (double x, double z)> toXZ, Func<UnitToken, MapHex, bool> clear, Action<string>? skipped = null)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (hexes == null || hexes.Count == 0) throw new ArgumentException("no hexes", nameof(hexes));
            var centres = new (double x, double z)[hexes.Count];
            for (int i = 0; i < hexes.Count; i++) centres[i] = toXZ(hexes[i].Lon, hexes[i].Lat);
            var taken = new HashSet<(int, int)>();
            var result = new List<PlacedToken>();
            foreach (var t in tokens)
            {
                var a = toXZ(t.Anchor.Lon, t.Anchor.Lat);
                int home = Nearest(centres, a);
                var (hq, hr) = (hexes[home].Q, hexes[home].R);
                bool sea = t.Unit.Service == UnitData.Navy;
                int best = -1; double bestD = double.MaxValue;
                for (int i = 0; i < hexes.Count; i++)
                {
                    var h = hexes[i];
                    if (h.Land == sea || taken.Contains((h.Q, h.R))) continue;
                    if (MapActions.Distance(h.Q, h.R, hq, hr) > MaxShift) continue;
                    double d = Sq(centres[i], a);
                    // Ties (equal distance) go to the lower (q, r): the order never depends on the hex list's order.
                    if (d > bestD || (d == bestD && best >= 0 && (h.Q, h.R).CompareTo((hexes[best].Q, hexes[best].R)) > 0)) continue;
                    if (!clear(t, h)) continue;
                    best = i; bestD = d;
                }
                if (best < 0) { skipped?.Invoke($"UNITS no clear hex within {MaxShift} of {t.Anchor.Place} for {t.Unit.Id}; not drawn"); continue; }
                taken.Add((hexes[best].Q, hexes[best].R));
                result.Add(new PlacedToken(t, hexes[best].Q, hexes[best].R));
            }
            return result;
        }

        static int Nearest((double x, double z)[] centres, (double x, double z) p)
        {
            int best = 0; double bestD = double.MaxValue;
            for (int i = 0; i < centres.Length; i++) { double d = Sq(centres[i], p); if (d < bestD) { bestD = d; best = i; } }
            return best;
        }

        static double Sq((double x, double z) a, (double x, double z) b) => (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
    }
}
