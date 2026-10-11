#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The legislature as a half circle of seats (promo/final/04), drawn with Painter2D: one dot per seat of the
    /// body that sits on the date (legislatures.yaml size), coloured by party in the order the presenter gives (the
    /// coalition's parties first, from the left), with a gold line at the majority (more than half the seats). Seats a
    /// record does not assign to a party are drawn as hollow rings, never given to a party. It takes no clicks.</summary>
    [UxmlElement]
    public partial class Hemicycle : VisualElement
    {
        /// <summary>One party's block of seats, in drawing order.</summary>
        public readonly struct Group
        {
            public readonly string Party; public readonly int Seats; public readonly Color Colour; public readonly bool Coalition;
            public Group(string party, int seats, Color colour, bool coalition) { Party = party; Seats = seats; Colour = colour; Coalition = coalition; }
        }

        static readonly Color Gold = new Color32(232, 204, 142, 255), Unrecorded = new Color32(170, 164, 150, 255);
        /// <summary>Inner radius of the half circle, as a share of the outer.</summary>
        const float Inner = 0.42f;

        IReadOnlyList<Group> groups = Array.Empty<Group>();
        int size;
        List<Vector2> seats = new List<Vector2>();

        public Hemicycle()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public IReadOnlyList<Group> Groups => groups;
        /// <summary>The number of seats in the body.</summary>
        public int Size => size;
        /// <summary>Seats needed for a majority: more than half.</summary>
        public int Majority => size / 2 + 1;

        /// <summary>Sets the body's size and its party blocks (their seats may sum to less than the size: the rest are drawn
        /// unassigned; never more).</summary>
        public void Set(IReadOnlyList<Group> blocks, int bodySize)
        {
            if (bodySize < 0) throw new ArgumentOutOfRangeException(nameof(bodySize));
            if (blocks.Sum(g => g.Seats) > bodySize) throw new ArgumentException("more party seats than the body has", nameof(blocks));
            groups = blocks.ToList(); size = bodySize;
            seats = Layout(size);
            MarkDirtyRepaint();
        }

        /// <summary>Each seat's colour from left to right: the blocks in order, then unassigned seats (null).</summary>
        public IReadOnlyList<Color?> SeatColours()
        {
            var list = new List<Color?>(size);
            foreach (var g in groups) for (int i = 0; i < g.Seats; i++) list.Add(g.Colour);
            while (list.Count < size) list.Add(null);
            return list;
        }

        /// <summary>Rows of seats for a body of <paramref name="seatCount"/>.</summary>
        public static int Rows(int seatCount) => seatCount <= 12 ? 2 : seatCount <= 40 ? 4 : seatCount <= 90 ? 5 : seatCount <= 160 ? 7 : 9;

        /// <summary>Seat centres on a unit half circle (x in -1..1, y in 0..1, y up), from left to right by angle (and from
        /// the inside out within an angle): concentric rows, each holding seats in proportion to its length.</summary>
        public static List<Vector2> Layout(int seatCount)
        {
            var result = new List<Vector2>(seatCount);
            if (seatCount <= 0) return result;
            int rows = Rows(seatCount);
            var radii = Enumerable.Range(0, rows).Select(i => rows == 1 ? 1f : Inner + (1f - Inner) * i / (rows - 1)).ToArray();
            var total = radii.Sum();
            var counts = radii.Select(r => (int)Math.Floor(seatCount * r / total)).ToArray();
            for (int i = rows - 1, left = seatCount - counts.Sum(); left > 0; i = i == 0 ? rows - 1 : i - 1, left--) counts[i]++;   // the rest to the outer rows
            var placed = new List<(float angle, float radius)>(seatCount);
            for (int row = 0; row < rows; row++)
                for (int j = 0; j < counts[row]; j++)
                    placed.Add((counts[row] == 1 ? Mathf.PI / 2 : Mathf.PI * (1f - (float)j / (counts[row] - 1)), radii[row]));
            foreach (var (a, r) in placed.OrderByDescending(p => Math.Round(p.angle, 5)).ThenBy(p => p.radius))
                result.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Max(0f, Mathf.Sin(a)) * r));   // sin(pi) is -9e-8 in float: keep the ends on the base line
            return result;
        }

        /// <summary>A dot's radius on the unit half circle: under half the gap between rows and between seats in the outer row.</summary>
        public static float DotRadius(int seatCount)
        {
            if (seatCount <= 0) return 0f;
            int rows = Rows(seatCount);
            var rowGap = rows == 1 ? 1f : (1f - Inner) / (rows - 1);
            var outer = Layout(seatCount).Count(p => Mathf.Abs(p.magnitude - 1f) < 1e-4f);
            var seatGap = outer <= 1 ? 1f : Mathf.PI / (outer - 1);
            return 0.42f * Mathf.Min(rowGap, seatGap);
        }

        /// <summary>The angle (radians, 0 = right, pi = left) of the majority line: between the last seat of a majority
        /// counted from the left and the next one.</summary>
        public float MajorityAngle()
        {
            if (size < 2) return Mathf.PI / 2;
            float At(int i) => Mathf.Atan2(seats[i].y, seats[i].x);
            return (At(Majority - 1) + At(Math.Min(Majority, size - 1))) / 2f;
        }

        void Draw(MeshGenerationContext mgc)
        {
            var box = contentRect;
            if (size == 0 || !(box.width > 8) || !(box.height > 8)) return;
            var dot = DotRadius(size);
            // The unit half circle plus a dot's margin, fitted into the box and sat on its bottom edge.
            var scale = Mathf.Min(box.width / (2f + 2f * dot), box.height / (1f + 2f * dot));
            var centre = new Vector2(box.center.x, box.yMax - dot * scale);
            Vector2 At(Vector2 unit) => centre + new Vector2(unit.x, -unit.y) * scale;
            var p = mgc.painter2D;
            var colours = SeatColours();
            for (int i = 0; i < size; i++)
            {
                var c = colours[i];
                p.BeginPath(); p.Arc(At(seats[i]), dot * scale, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath();
                if (c.HasValue) { p.fillColor = c.Value; p.Fill(); }
                else { p.strokeColor = Unrecorded; p.lineWidth = 1.2f; p.Stroke(); }
            }
            // The majority line runs radially through the gap, a little past the rows at both ends.
            var a = MajorityAngle();
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            p.strokeColor = Gold; p.lineWidth = 2f; p.lineCap = LineCap.Round;
            p.BeginPath(); p.MoveTo(At(dir * (Inner - 1.2f * dot))); p.LineTo(At(dir * (1f + 2.2f * dot))); p.Stroke();
        }
    }
}
