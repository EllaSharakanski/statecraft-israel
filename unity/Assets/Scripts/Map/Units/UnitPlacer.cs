#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Where an anchor's token stands: its hex, its centre on the ground plane (metres) and whether it is drawn
    /// compact (a smaller ring and a tighter trio, to fit beside a town).</summary>
    public readonly struct AnchorSpot : IEquatable<AnchorSpot>
    {
        public readonly int Q, R; public readonly double X, Z; public readonly bool Compact;
        public AnchorSpot(int q, int r, double x, double z, bool compact) { Q = q; R = r; X = x; Z = z; Compact = compact; }
        public bool Equals(AnchorSpot o) => Q == o.Q && R == o.R && X == o.X && Z == o.Z && Compact == o.Compact;
        public override bool Equals(object? o) => o is AnchorSpot s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Q, R, X, Z, Compact);
        public override string ToString() => $"({Q},{R}){(Compact ? " compact" : "")}";
    }

    /// <summary>A token with its spot.</summary>
    public readonly struct PlacedToken : IEquatable<PlacedToken>
    {
        public readonly UnitToken Token; public readonly AnchorSpot Spot;
        public PlacedToken(UnitToken token, AnchorSpot spot) { Token = token; Spot = spot; }
        public int Q => Spot.Q;
        public int R => Spot.R;
        public bool Equals(PlacedToken o) => Token.Equals(o.Token) && Spot.Equals(o.Spot);
        public override bool Equals(object? o) => o is PlacedToken p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(Token, Spot);
        public override string ToString() => $"{Token} {Spot}";
    }

    /// <summary>Puts every anchor of the unit data on one spot, once, from the data alone (review M9: a token never moves
    /// because another unit appears). The spot is the nearest to the anchor point (review I2: within one hex step, about
    /// 8.7 km between hex centres on 5 km hexes) among:
    /// 1. the centres of the anchor's own hex and its six neighbours, drawn full size;
    /// 2. then, when none is clear, those centres and six points inside each hex (at <see cref="InsideOffset"/> of the hex
    ///    circumradius), drawn compact.
    /// A spot must be land (sea for a ship), on a hex no other anchor with an overlapping window holds, and clear (the
    /// caller's test: the ring off the settlement footprints, the figures on land and off the railways and roads). An
    /// anchor with no such spot is left out and reported. The map holds no 1948 front line yet, so the one-hex limit is
    /// what keeps a token on its own side (docs/units.md). Anchors are placed in the data's order.</summary>
    public static class UnitPlacer
    {
        /// <summary>Hexes from the anchor's own hex (axial distance).</summary>
        public const int MaxShift = 1;
        /// <summary>The inside points' distance from a hex centre, as a fraction of the circumradius (inside the inradius).</summary>
        public const double InsideOffset = 0.45;

        /// <param name="toXZ">lon, lat to the map's ground plane (metres).</param>
        /// <param name="clear">(service, x, z, compact): true when a token of that service may stand there.</param>
        public static Dictionary<UnitAnchor, AnchorSpot> PlaceAll(IReadOnlyList<UnitEntry> units, IReadOnlyList<MapHex> hexes, double hexSizeM,
            Func<double, double, (double x, double z)> toXZ, Func<string, double, double, bool, bool> clear, Action<string>? skipped = null)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (hexes == null || hexes.Count == 0) throw new ArgumentException("no hexes", nameof(hexes));
            var centres = new (double x, double z)[hexes.Count];
            for (int i = 0; i < hexes.Count; i++) centres[i] = toXZ(hexes[i].Lon, hexes[i].Lat);
            double maxOffset = Math.Sqrt(3.0) * hexSizeM + 1.0;   // one hex step (plus a metre of rounding)
            var held = new List<(int q, int r, DateTime from, DateTime to)>();
            var result = new Dictionary<UnitAnchor, AnchorSpot>();
            foreach (var u in units)
            {
                if (u.Hidden != null) continue;
                bool sea = u.Service == UnitData.Navy;
                foreach (var a in u.Anchors)
                {
                    var p = toXZ(a.Lon, a.Lat);
                    DateTime from = UnitData.Day(a.From), to = UnitData.Day(a.To);
                    int home = Nearest(centres, p);
                    var (hq, hr) = (hexes[home].Q, hexes[home].R);
                    AnchorSpot? best = null;
                    foreach (var compact in new[] { false, true })
                    {
                        double bestD = double.MaxValue;
                        for (int i = 0; i < hexes.Count; i++)
                        {
                            var h = hexes[i];
                            if (h.Land == sea || MapActions.Distance(h.Q, h.R, hq, hr) > MaxShift) continue;
                            if (held.Exists(x => x.q == h.Q && x.r == h.R && x.from < to && from < x.to)) continue;
                            for (int k = compact ? 0 : -1; k < (compact ? 7 : 0); k++)
                            {
                                var (x, z) = k <= 0 ? centres[i] : Inside(centres[i], k - 1, hexSizeM);
                                double d = Math.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z));
                                if (d > maxOffset || d > bestD) continue;
                                // Ties go to the lower (q, r): the result never depends on the hex list's order.
                                if (d == bestD && best != null && (h.Q, h.R).CompareTo((best.Value.Q, best.Value.R)) > 0) continue;
                                if (!clear(u.Service, x, z, compact)) continue;
                                best = new AnchorSpot(h.Q, h.R, x, z, compact); bestD = d;
                            }
                        }
                        if (best != null) break;
                    }
                    if (best == null) { skipped?.Invoke($"UNITS no clear spot within one hex of {a.Place} for {u.Id}; not drawn"); continue; }
                    held.Add((best.Value.Q, best.Value.R, from, to));
                    result[a] = best.Value;
                }
            }
            return result;
        }

        /// <summary>The tokens on their anchors' spots; a token whose anchor has no spot is left out.</summary>
        public static List<PlacedToken> Place(IReadOnlyList<UnitToken> tokens, IReadOnlyDictionary<UnitAnchor, AnchorSpot> spots)
        {
            var list = new List<PlacedToken>(tokens.Count);
            foreach (var t in tokens) if (spots.TryGetValue(t.Anchor, out var s)) list.Add(new PlacedToken(t, s));
            return list;
        }

        static (double x, double z) Inside((double x, double z) c, int k, double hexSizeM)
        {
            double ang = Math.PI / 3.0 * k;
            return (c.x + Math.Cos(ang) * InsideOffset * hexSizeM, c.z + Math.Sin(ang) * InsideOffset * hexSizeM);
        }

        static int Nearest((double x, double z)[] centres, (double x, double z) p)
        {
            int best = 0; double bestD = double.MaxValue;
            for (int i = 0; i < centres.Length; i++)
            {
                double d = (centres[i].x - p.x) * (centres[i].x - p.x) + (centres[i].z - p.z) * (centres[i].z - p.z);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
