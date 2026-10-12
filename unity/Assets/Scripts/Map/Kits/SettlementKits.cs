#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The four procedural archetypes of the settlement art spike (issues #31, #32).</summary>
    public enum Archetype { WhiteCity, OldTown, ArabTown, Moshava }

    /// <summary>Procedural settlement kits at the owner's symbolic scale (ruling 2026-10-11): buildings about
    /// <see cref="KitScale"/> times their real size (per class), a town's footprint spread over neighbouring hexes in
    /// proportion to its population, geography real (towns stay on land, at their sourced coordinates). Built at load time
    /// from the settlement id's seed (<see cref="KitRandom"/>), so every load of a build draws the same towns; nothing here reads or
    /// writes the sim.
    /// Towns round 2 (docs/visual-v1.md, "Towns round 2"): every town is laid out on plot lattices (streets of plots at the
    /// town's grid angle; organic towns jittered), each plot built with a probability that falls from the core to the edge,
    /// instead of random spots tried against every lot. The landmarks of the four cities live in SettlementKits.Landmarks.cs.</summary>
    public static partial class SettlementKits
    {
        /// <summary>Settled population per hex of footprint: a town of this many people covers one hex (about 65 km^2).</summary>
        public const float PeoplePerHex = 60000f;
        /// <summary>The smallest footprint, in hexes, so a kibbutz of 240 still reads as a place at the gameplay zoom.</summary>
        public const float MinHexes = 0.08f;
        /// <summary>How far past its footprint radius a town's plots reach (UnitGround.FootprintMargin: the units assume no
        /// building beyond it; PaintGround tints the same disc).</summary>
        public const float PlotReach = 1.1f;

        public static Archetype ArchetypeOf(Settlement s)
        {
            switch (s.Id)
            {
                case "tel_aviv": case "haifa": return Archetype.WhiteCity;
                case "jaffa": case "acre": case "gaza": case "jerusalem": return Archetype.OldTown;
            }
            switch (s.Type)
            {
                case "city": return Archetype.WhiteCity;
                case "arab_town": case "village": return Archetype.ArabTown;
                case "town": return s.Id == "tiberias" || s.Id == "safed" ? Archetype.ArabTown : Archetype.Moshava;
                default: return Archetype.Moshava;   // kibbutz, moshav
            }
        }

        /// <summary>Footprint radius (m) of a settlement: the radius of a disc of (population / PeoplePerHex) hexes.</summary>
        public static float FootprintRadiusM(Settlement s, float hexSizeM)
        {
            float hexArea = 1.5f * 1.7320508f * hexSizeM * hexSizeM;   // a pointy hex of circumradius hexSizeM
            float hexes = Mathf.Max(MinHexes, KitPopulation.Of(s.Id) / PeoplePerHex);
            return Mathf.Sqrt(hexes * hexArea / Mathf.PI);
        }

        /// <summary>Claims every settlement's footprint on <paramref name="ground"/> (largest first, so a big town's disc is
        /// whole and its small neighbour fits around it) and returns one mesh per settlement, keyed by id.</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground) =>
            Build(list, ground, out _);

        /// <summary>As Build, also returning every claimed lot (centre, radius, owning settlement id): buildings and landmarks
        /// of all towns, none overlapping another (tests); and, when <paramref name="columns"/> is given, filling it with each
        /// non-empty kit's columns by settlement id (KitMesh.Columns, read before the mesh's CPU copy is dropped).</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, Dictionary<string, Bounds[]>? columns = null) =>
            Build(list, ground, out claimed, out _, columns);

        /// <summary>One thing a kit placed, for the tests' and the docs' town census: a building (<see cref="Placed.Kind"/>
        /// "building", or "wing" for a block's lower part) or a landmark (its name: "mosque", "clock_tower", ...), with its
        /// world footprint (centre, yaw in degrees, width and depth in m), its wall height (m) and wall colour.</summary>
        public readonly struct Placed
        {
            public readonly string Id, Kind; public readonly Vector2 C; public readonly float Yaw, W, D, H; public readonly Color32 Wall;
            public Placed(string id, string kind, Vector2 c, float yaw, float w, float d, float h, Color32 wall)
            { Id = id; Kind = kind; C = c; Yaw = yaw; W = w; D = d; H = h; Wall = wall; }
            /// <summary>Harbour works stand in the sea by design; everything else stands on land.</summary>
            public bool InSea => Kind == "port" || Kind == "breakwater";
        }

        [System.ThreadStatic] static List<Placed>? census;
        static void Record(string id, string kind, Vector2 c, float yaw, float w, float d, float h, Color32 wall) =>
            census?.Add(new Placed(id, kind, c, yaw, w, d, h, wall));

        /// <summary>As Build, also listing everything placed (<see cref="Placed"/>), in placement order.</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, out List<Placed> placed, Dictionary<string, Bounds[]>? columns = null)
        {
            census = placed = new List<Placed>();
            try { return BuildAll(list, ground, out claimed, columns); }
            finally { census = null; }
        }

        static List<(Settlement s, Mesh mesh, int buildings)> BuildAll(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, Dictionary<string, Bounds[]>? columns)
        {
            var order = new List<Settlement>(list);
            order.Sort((a, b) => KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) != 0
                ? KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) : string.CompareOrdinal(a.Id, b.Id));
            var centres = new Dictionary<string, Vector2>();
            foreach (var s in order)
            {
                var w = ground.Frame.ToWorld(s.Lon, s.Lat, 0);
                centres[s.Id] = new Vector2(w.x, w.z);
                ground.Footprints.Add((centres[s.Id], FootprintRadiusM(s, ground.HexSizeM), s.Id));
            }
            // One lot list for every town. Every town's landmarks first (largest town first), so no town's houses take
            // another's landmark spot (a harbour, a wall); then the houses, largest town first.
            var lots = new Lots();
            var meshes = new Dictionary<string, KitMesh>(); var streams = new Dictionary<string, KitRandom>(); var counts = new Dictionary<string, int>();
            foreach (var s in order)
            {
                var rnd = new KitRandom(s.Id, 1);
                var km = meshes[s.Id] = new KitMesh();
                counts[s.Id] = PlaceLandmarks(s, centres[s.Id], ground, km, lots, ref rnd);
                streams[s.Id] = rnd;
            }
            var result = new List<(Settlement, Mesh, int)>();
            foreach (var s in order)
            {
                var rnd = streams[s.Id]; var km = meshes[s.Id];
                int n = counts[s.Id] + PlaceHouses(s, centres[s.Id], ground, km, lots, ref rnd);
                if (columns != null && km.VertexCount > 0) columns[s.Id] = km.Columns();
                result.Add((s, km.ToMesh(s.Id + "_kit"), n));
            }
            claimed = lots.List.ConvertAll(l => (centre: l.c, radius: l.r, id: l.id));
            return result;
        }

        /// <summary>Tints the ground of each settlement's footprint in the baked biome colour texels (sRGB, row 0 = south):
        /// pale paving and sand in the towns, as in promo 03, fading out over the outer half of the footprint.</summary>
        public static void PaintGround(Color32[] px, int res, GeoFrame frame, IReadOnlyList<Settlement> list, float hexSizeM)
        {
            foreach (var s in list)
            {
                var arch = ArchetypeOf(s);
                var (sand, strength) = arch switch
                {
                    Archetype.WhiteCity => (new Color(0.80f, 0.75f, 0.64f), 0.62f),
                    Archetype.OldTown => (new Color(0.78f, 0.68f, 0.52f), 0.62f),
                    Archetype.ArabTown => (new Color(0.74f, 0.64f, 0.48f), 0.5f),
                    _ => (new Color(0.70f, 0.63f, 0.47f), 0.32f),
                };
                var c = frame.ToWorld(s.Lon, s.Lat, 0);
                float R = FootprintRadiusM(s, hexSizeM) * 1.1f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - R) / frame.WidthM * res)), x1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.x + R) / frame.WidthM * res));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((c.z - R) / frame.LengthM * res)), y1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.z + R) / frame.LengthM * res));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float wx = (x + 0.5f) / res * frame.WidthM - c.x, wz = (y + 0.5f) / res * frame.LengthM - c.z;
                        float d = Mathf.Sqrt(wx * wx + wz * wz) / R;
                        if (d >= 1f) continue;
                        float w = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, d)));
                        int i = y * res + x;
                        px[i] = Color32.Lerp(px[i], sand, w);
                    }
            }
        }

        /// <summary>A claimed lot: its disc, its town and, for a plot, its lattice (<see cref="group"/>; -1 for a landmark).
        /// Plots of one lattice never overlap by construction, so they do not test each other's discs.</summary>
        sealed class Lot { public Vector2 c; public float r; public string id = ""; public int group = -1; }

        /// <summary>The shared lot list, in claim order, with a grid index (DiscIndex, as KitGround's) for the free-spot test.</summary>
        sealed class Lots
        {
            public readonly List<Lot> List = new List<Lot>();
            readonly DiscIndex index = new DiscIndex(1f, KitGround.LotQueryM);
            public void Add(Lot l) { List.Add(l); index.Add((l.c, l.r, l.id)); }

            /// <summary>No lot within reach of a disc of radius r at p, except the plots of <paramref name="id"/>'s lattice
            /// <paramref name="group"/> (pass group -1 to test against every lot).</summary>
            public bool Free(Vector2 p, float r, string id = "", int group = -1)
            {
                if (r > KitGround.LotQueryM)
                {
                    foreach (var l in List) if (!Same(l, id, group) && (l.c - p).sqrMagnitude < (l.r + r) * (l.r + r)) return false;
                    return true;
                }
                var near = index.Near(p);
                for (int k = 0; k < near.Count; k++)
                {
                    var l = List[near[k]];
                    if (!Same(l, id, group) && (l.c - p).sqrMagnitude < (l.r + r) * (l.r + r)) return false;
                }
                return true;
            }

            static bool Same(Lot l, string id, int group) => group >= 0 && l.group == group && l.id == id;
        }

        static bool FreeAt(Lots lots, Vector2 p, float r) => lots.Free(p, r);

        /// <summary>A landmark's spot (review N2): the dry spot nearest <paramref name="want"/> (rings out to four radii) that no
        /// lot of any town already holds; claimed for <paramref name="owner"/>. False, and nothing claimed, when there is none:
        /// the landmark is then left out rather than overlapping a bigger neighbour.</summary>
        static bool Claim(Lots lots, Vector2 want, float r, string owner, KitGround ground, out Vector2 at)
        {
            for (int ring = 0; ring <= 8; ring++)
            {
                int around = ring == 0 ? 1 : 8 * ring;
                for (int k = 0; k < around; k++)
                {
                    float a = 2f * Mathf.PI * k / around, d = ring * r * 0.5f;
                    var p = want + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !FreeAt(lots, p, r) || ground.OnRoute(p, r)) continue;
                    lots.Add(new Lot { c = p, r = r, id = owner }); at = p; return true;
                }
            }
            at = want; return false;
        }

        // ---- Plots ----------------------------------------------------------------------------------------------------

        /// <summary>The kinds of building a plot lattice lays out. Real sizes (m, before the symbolic scale) and storeys are
        /// in <see cref="Pick"/>; lattice pitch, density and scale in <see cref="Grid"/>.</summary>
        enum Style
        {
            /// <summary>Tel Aviv's and Hadar HaCarmel's International Style blocks: white plaster, flat roofs (60x).</summary>
            WhiteBlock,
            /// <summary>The plastered two- and three-storey centre of a large moshava (Petah Tikva, Rehovot) (60x).</summary>
            TownBlock,
            /// <summary>Jerusalem's new city: stone-faced blocks, Storrs' 1918 ordinance (60x).</summary>
            StoneBlock,
            /// <summary>An Ottoman old town's stone house: Jaffa, Acre, Gaza, Jerusalem within the walls, Haifa's lower town (100x).</summary>
            OldHouse,
            /// <summary>An Arab town's or village's stone or mud-brick house (100x).</summary>
            Village,
            /// <summary>A moshava's, kibbutz's or moshav's red-roofed house (100x).</summary>
            Moshava,
            /// <summary>A Templer house of Haifa's German Colony: stone, red roof (100x; placed along its avenue, not on a lattice).</summary>
            Templer,
        }

        /// <summary>A plot lattice: rows of plots at its style's pitch (<see cref="Grid"/>), turned <see cref="Yaw"/> degrees;
        /// <see cref="Group"/> tells a town's lattices apart (plots of one lattice never overlap, so they skip each other's lot test).</summary>
        readonly struct Lattice
        {
            public readonly Style Style; public readonly float Yaw; public readonly int Group;
            public Lattice(Style style, float yaw, int group) { Style = style; Yaw = yaw; Group = group; }
        }

        /// <summary>Plot pitch along and across the street (real m), jitter (share of the pitch), the symbolic scale, and
        /// the share of plots built in the core (out to t0 of the radius), at the radius, and past it (falling to none at
        /// <see cref="PlotReach"/>).</summary>
        static (float pu, float pv, float jitter, float scale, float core, float t0, float edge) Grid(Style st) => st switch
        {
            Style.WhiteBlock => (18.5f, 13.5f, 0f, KitScale.CityBlocks, 1.00f, 0.60f, 0.70f),
            Style.TownBlock => (20f, 16f, 0.03f, KitScale.CityBlocks, 1.00f, 0.30f, 0.80f),
            Style.StoneBlock => (23f, 17.5f, 0.03f, KitScale.CityBlocks, 0.97f, 0.50f, 0.60f),
            Style.OldHouse => (7.2f, 6.8f, 0.05f, KitScale.Houses, 1.00f, 0.60f, 0.75f),
            Style.Village => (7.6f, 7.2f, 0.07f, KitScale.Houses, 1.00f, 0.50f, 0.60f),
            _ => (11.5f, 9.8f, 0.04f, KitScale.Houses, 0.95f, 0.40f, 0.60f),
        };

        static float Density(Style st, float t)
        {
            var g = Grid(st);
            if (t <= g.t0) return g.core;
            if (t <= 1f) return Mathf.Lerp(g.core, g.edge, (t - g.t0) / (1f - g.t0));
            return Mathf.Lerp(g.edge, 0f, (t - 1f) / (PlotReach - 1f));
        }

        /// <summary>The lattices a town is laid out on: one per archetype, two or three where a town has quarters of different
        /// building (Jerusalem: the walled Old City, the stone new city, the Arab quarters and villages round it; Haifa: the
        /// lower town by the port and Hadar on the slope; a large moshava's plastered centre).</summary>
        static List<Lattice> Lattices(Settlement s, Archetype arch)
        {
            float yaw = GridYaw(s.Id);
            switch (s.Id)
            {
                case "jerusalem": return new List<Lattice> { new Lattice(Style.OldHouse, 0f, 0), new Lattice(Style.StoneBlock, yaw, 1), new Lattice(Style.Village, yaw, 2) };
                case "haifa": return new List<Lattice> { new Lattice(Style.WhiteBlock, yaw, 0), new Lattice(Style.OldHouse, yaw, 1) };
            }
            switch (arch)
            {
                case Archetype.WhiteCity: return new List<Lattice> { new Lattice(Style.WhiteBlock, yaw, 0) };
                case Archetype.OldTown: return new List<Lattice> { new Lattice(Style.OldHouse, yaw, 0) };
                case Archetype.ArabTown: return new List<Lattice> { new Lattice(Style.Village, yaw, 0) };
                default:
                    return s.Type == "town" && KitPopulation.Of(s.Id) >= LargeMoshava
                        ? new List<Lattice> { new Lattice(Style.TownBlock, yaw, 0), new Lattice(Style.Moshava, yaw, 1) }
                        : new List<Lattice> { new Lattice(Style.Moshava, yaw, 0) };
            }
        }

        /// <summary>A moshava of at least this many people (1945) gets a plastered town centre (Petah Tikva, Rehovot, Rishon
        /// LeZion, Hadera).</summary>
        const int LargeMoshava = 7500;

        /// <summary>Whether lattice <paramref name="L"/> builds at <paramref name="p"/> (t: distance over the footprint radius).</summary>
        static bool Allows(Settlement s, Lattice L, Vector2 p, float t, KitGround ground)
        {
            switch (s.Id)
            {
                case "jerusalem":
                {
                    var (oc, half) = OldCity(ground);
                    var d = p - oc;
                    bool inside = Mathf.Abs(d.x) < half - OldCityInset && Mathf.Abs(d.y) < half - OldCityInset;
                    bool outside = Mathf.Abs(d.x) > half + OldCityInset || Mathf.Abs(d.y) > half + OldCityInset;
                    // West of the Old City: the Jewish new city (the city centre, Rehavia); north, east and south: the Arab
                    // quarters and villages. (The Old City's symbolic square covers the ground of its nearest neighbourhoods.)
                    bool west = d.x < -half * 0.6f;
                    return L.Style == Style.OldHouse ? inside : outside && (L.Style == Style.StoneBlock) == west;
                }
                case "haifa":
                    // The lower town by the bay (old Arab Haifa, below about 35 m) and Hadar HaCarmel up the slope.
                    return (L.Style == Style.OldHouse) == (ground.ElevM(p.x, p.y) < 35f);
                case "nahalal":
                    return t > 0.42f && t < 0.88f;   // Nahalal's ring of farmsteads round the public buildings
            }
            if (L.Style == Style.TownBlock) return t < 0.42f;
            if (L.Style == Style.Moshava && L.Group == 1) return t > 0.36f;
            return true;
        }

        /// <summary>Places a town's houses on its lattices, nearest the centre first; returns how many.</summary>
        static int PlaceHouses(Settlement s, Vector2 centre, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            var arch = ArchetypeOf(s);
            float R = FootprintRadiusM(s, ground.HexSizeM), reach = R * PlotReach;
            var lattices = Lattices(s, arch);

            // A point belongs to the settlement whose footprint it is deepest inside (distance over radius), so Jaffa and Tel
            // Aviv, or Lod and Ramla, meet along a seam instead of overlapping.
            bool Mine(Vector2 p)
            {
                float mine = (p - centre).magnitude / R;
                var fp = ground.Footprints;
                var near = fp.Near(p);
                for (int k = 0; k < near.Count; k++)
                {
                    int i = near[k];
                    if (fp[i].id != s.Id && (p - fp[i].centre).magnitude / fp[i].radius < mine) return false;
                }
                return true;
            }

            var plots = new List<(float d, int order, Vector2 p, int lattice)>();
            for (int li = 0; li < lattices.Count; li++)
            {
                var g = Grid(lattices[li].Style);
                float pu = g.pu * g.scale, pv = g.pv * g.scale, a = lattices[li].Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                int nu = Mathf.CeilToInt(reach / pu) + 1, nv = Mathf.CeilToInt(reach / pv) + 1;
                for (int j = -nv; j <= nv; j++)
                    for (int i = -nu; i <= nu; i++)
                    {
                        // Organic towns stagger alternate rows by half a plot: no straight cross streets.
                        float u = (i + (g.jitter > 0f && (j & 1) != 0 ? 0.5f : 0f)) * pu, v = j * pv;
                        var p = centre + new Vector2(u * ca - v * sa, u * sa + v * ca);
                        float d = (p - centre).magnitude;
                        if (d <= reach) plots.Add((d, plots.Count, p, li));
                    }
            }
            plots.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : x.order.CompareTo(y.order));

            int placed = 0;
            foreach (var (d, _, p0, li) in plots)
            {
                var L = lattices[li]; var g = Grid(L.Style);
                float t = d / R;
                // Every plot draws the same numbers whether or not it is built, so one plot's fate never shifts another's.
                bool build = rnd.Chance(Density(L.Style, t));
                float ju = rnd.Range(-1f, 1f) * g.jitter * g.pu * g.scale, jv = rnd.Range(-1f, 1f) * g.jitter * g.pv * g.scale;
                var spec = Pick(L.Style, t, ref rnd, g.scale);
                // Inside Jerusalem's walls, one or two storeys: the walls and the Dome of the Rock stand above the roofs.
                if (s.Id == "jerusalem" && L.Style == Style.OldHouse && spec.storeys > 2) { spec.storeys = 2; spec.h = 2f * 3.3f * g.scale; }
                float yaw = L.Yaw + (g.jitter > 0f ? rnd.Range(-4f, 4f) : 0f);
                ulong detail = rnd.Next();
                if (!build) continue;
                float a = L.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var p = p0 + new Vector2(ju * ca - jv * sa, ju * sa + jv * ca);
                if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !Mine(p) || !Allows(s, L, p, t, ground)) continue;
                bool hillTown = L.Style == Style.OldHouse || L.Style == Style.Village;
                // The full building, else a smaller one on the plot or nudged within it: a plot by a road or a neighbour's
                // lot keeps a smaller building instead of standing empty. A nudged, shrunk building still stays inside its
                // plot, so it never reaches a neighbouring plot's building.
                bool fitted = false; Spec b = spec; Vector2 q = p;
                foreach (var (shrink, du, dv) in Fits)
                {
                    b = Shrink(spec, shrink);
                    float u = du * g.pu * g.scale, v = dv * g.pv * g.scale;
                    q = p + new Vector2(u * ca - v * sa, u * sa + v * ca);
                    if ((du != 0f || dv != 0f) && (!ground.Inside(q.x, q.y) || ground.Water(q.x, q.y) || !Mine(q) || !Allows(s, L, q, t, ground))) continue;
                    if (!lots.Free(q, b.lotR, s.Id, L.Group) || ground.OnRoute(q, b.lotR)) continue;   // off the railways and roads too
                    if (ground.Slope(q.x, q.y, b.w) > (hillTown ? 0.6f : 0.35f)) continue;
                    if (CornerInWater(q, yaw, b.w, b.d, ground)) continue;   // the whole footprint is dry
                    if (L.Style != Style.OldHouse && NearSea(q, yaw, b.w + 2f * BeachSetbackM, b.d + 2f * BeachSetbackM, ground)) continue;
                    fitted = true; break;
                }
                if (!fitted) continue;
                lots.Add(new Lot { c = q, r = b.lotR, id = s.Id, group = L.Group });
                var drnd = new KitRandom(detail);
                Emit(s.Id, L.Style, q, yaw, b, t, ground, km, ref drnd);
                placed++;
            }
            return placed;
        }

        /// <summary>The fits a plot tries in turn: (size share, nudge along and across the street as a share of the pitch).</summary>
        static readonly (float shrink, float du, float dv)[] Fits =
        {
            (1f, 0f, 0f), (0.72f, 0f, 0f), (0.72f, 0.15f, 0f), (0.72f, -0.15f, 0f), (0.72f, 0f, 0.15f), (0.72f, 0f, -0.15f), (0.5f, 0f, 0f),
        };

        static Spec Shrink(Spec b, float k) =>
            k >= 1f ? b : new Spec { w = b.w * k, d = b.d * k, h = b.h, storeys = b.storeys, lotR = b.lotR * k };

        /// <summary>How far (m) a new town's buildings keep back from the sea: the beach strip (promo-sea's ScBeachM, 650 m on
        /// average) stays sand, with its palms, as in promo 03's Tel Aviv. The old towns (Jaffa, Acre, Gaza, Haifa's lower
        /// town by the port) meet the water.</summary>
        const float BeachSetbackM = 700f;

        /// <summary>Any water at the corners or edge midpoints of a w x d rectangle at <paramref name="p"/>.</summary>
        static bool NearSea(Vector2 p, float yawDeg, float w, float d, KitGround ground)
        {
            if (CornerInWater(p, yawDeg, w, d, ground)) return true;
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            foreach (var (u, v) in new[] { (0.5f * w, 0f), (-0.5f * w, 0f), (0f, 0.5f * d), (0f, -0.5f * d) })
                if (ground.Water(p.x + u * ca - v * sa, p.y + u * sa + v * ca)) return true;
            return false;
        }

        static bool CornerInWater(Vector2 p, float yawDeg, float w, float d, KitGround ground)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int i = 0; i < 4; i++)
            {
                float u = ((i & 1) == 0 ? -0.5f : 0.5f) * w, v = ((i & 2) == 0 ? -0.5f : 0.5f) * d;
                if (ground.Water(p.x + u * ca - v * sa, p.y + u * sa + v * ca)) return true;
            }
            return false;
        }

        /// <summary>The street grid's angle per town (Tel Aviv's Geddes plan runs about 20 degrees off north, parallel to the
        /// shore; the others get a seeded angle).</summary>
        static float GridYaw(string id) => id == "tel_aviv" ? 20f : id == "haifa" ? 35f : new KitRandom(id, 7).Range(0f, 90f);

        struct Spec { public float w, d, h, lotR; public int storeys; }

        /// <summary>A plot's building: real size and storeys by style and by t (0 at the heart, 1 at the footprint's edge),
        /// times the scale <paramref name="m"/>. Every size fits its lattice's plot with a street to spare.</summary>
        static Spec Pick(Style st, float t, ref KitRandom rnd, float m)
        {
            float w, d; int storeys;
            switch (st)
            {
                case Style.WhiteBlock:
                    w = rnd.Range(12f, 16f); d = rnd.Range(9f, 11.5f);
                    // Taller near the centre, a few taller still: promo 03's five- to eight-storey skyline, above the real White
                    // City's three or four storeys (owner ruling 2026-10-10: where the promo departs from the era, the promo wins).
                    storeys = Mathf.RoundToInt(Mathf.Lerp(7f, 3f, Mathf.Clamp01(t)) + rnd.Range(-1.5f, 1.5f));
                    if (t < 0.5f && rnd.Chance(0.1f)) storeys = rnd.Range(8, 11);
                    storeys = Mathf.Clamp(storeys, 2, 10);
                    break;
                case Style.TownBlock:
                    w = rnd.Range(12f, 17f); d = rnd.Range(9f, 13f);
                    storeys = rnd.Chance(Mathf.Lerp(0.6f, 0.2f, t / 0.42f)) ? 3 : 2;
                    break;
                case Style.StoneBlock:
                    w = rnd.Range(13f, 20f); d = rnd.Range(10f, 14.5f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.6f, 2f, Mathf.Clamp01(t)) + rnd.Range(-0.8f, 0.8f)), 2, 4);
                    break;
                case Style.OldHouse:
                    w = rnd.Range(4.6f, 6.6f); d = rnd.Range(4.4f, 6.2f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.2f, 1.3f, Mathf.Clamp01(t)) + rnd.Range(-0.8f, 0.8f)), 1, 4);
                    break;
                case Style.Village:
                    w = rnd.Range(4.4f, 6.6f); d = rnd.Range(4.2f, 6.0f);
                    storeys = rnd.Chance(Mathf.Lerp(0.45f, 0.1f, Mathf.Clamp01(t))) ? 2 : 1;
                    break;
                case Style.Templer:
                    w = rnd.Range(8.5f, 10.5f); d = rnd.Range(7.5f, 9f);
                    storeys = 2;
                    break;
                default:
                    w = rnd.Range(7.5f, 10f); d = rnd.Range(6f, 7.8f);
                    storeys = rnd.Chance(0.15f) ? 2 : 1;
                    break;
            }
            return new Spec { w = w * m, d = d * m, h = storeys * 3.3f * m, storeys = storeys, lotR = 0.5f * Mathf.Sqrt(w * w + d * d) * m };
        }

        // sRGB palettes (promo 03: white and cream city, honey stone Jaffa, tan villages, terracotta roofs).
        static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
        static Color32 Jitter(Color32 c, ref KitRandom rnd, float amount)
        {
            float k = 1f + rnd.Range(-amount, amount), warm = rnd.Range(-amount, amount) * 0.5f;
            return C(Mathf.Clamp01(c.r / 255f * k * (1 + warm)), Mathf.Clamp01(c.g / 255f * k), Mathf.Clamp01(c.b / 255f * k * (1 - warm)));
        }

        /// <summary>Draws one plot's building and records it in the census.</summary>
        static void Emit(string id, Style st, Vector2 p, float yaw, Spec b, float t, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = Grid(st).scale;
            var (bottom, top) = GroundSpan(p, yaw, b.w, b.d, ground, m);
            float storey = 3.3f * m;
            switch (st)
            {
                case Style.WhiteBlock:
                case Style.TownBlock:
                {
                    var wall = rnd.Chance(st == Style.TownBlock ? 0.3f : 0.12f) ? Jitter(C(0.90f, 0.84f, 0.72f), ref rnd, 0.04f) : Jitter(C(0.94f, 0.92f, 0.87f), ref rnd, 0.035f);
                    var roof = Jitter(C(0.80f, 0.78f, 0.74f), ref rnd, 0.05f);
                    // A third of the blocks step down at one end: a lower wing within the plot (L and stepped massing).
                    if (b.storeys >= 3 && rnd.Chance(0.33f))
                    {
                        float a = yaw * Mathf.Deg2Rad, share = rnd.Range(0.58f, 0.72f), side = rnd.Chance(0.5f) ? 1f : -1f;
                        float w1 = b.w * share, w2 = b.w - w1, d2 = b.d * rnd.Range(0.7f, 1f), h2 = b.h - storey * rnd.Range(1, 3);
                        float u1 = side * (b.w - w1) * 0.5f, u2 = -side * (b.w - w2) * 0.5f, v2 = (b.d - d2) * 0.5f;
                        var c1 = p + new Vector2(u1 * Mathf.Cos(a), u1 * Mathf.Sin(a));
                        var c2 = p + new Vector2(u2 * Mathf.Cos(a) - v2 * Mathf.Sin(a), u2 * Mathf.Sin(a) + v2 * Mathf.Cos(a));
                        km.Box(c1.x, c1.y, yaw, w1, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                        km.Box(c2.x, c2.y, yaw, w2 * 1.01f, d2, bottom, top, h2, wall, roof, Facade.WhiteCity, m);
                        Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                        Record(id, "wing", c2, yaw, w2, d2, h2, wall);
                    }
                    else
                    {
                        km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                        Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    }
                    // A stair tower or roof room on many Bauhaus blocks.
                    if (rnd.Chance(0.5f))
                    {
                        float rw = b.w * rnd.Range(0.22f, 0.36f), rd = b.d * rnd.Range(0.3f, 0.45f);
                        km.Box(p.x + b.w * rnd.Range(-0.15f, 0.15f), p.y + b.d * rnd.Range(-0.12f, 0.12f), yaw, rw, rd, top + b.h - 2f, top + b.h, 2.8f * m, wall, roof, Facade.Plain, m);
                    }
                    break;
                }
                case Style.StoneBlock:
                {
                    var wall = Jitter(C(0.86f, 0.79f, 0.64f), ref rnd, 0.05f);
                    var roof = Jitter(C(0.82f, 0.77f, 0.66f), ref rnd, 0.04f);
                    bool tiled = rnd.Chance(0.15f);   // the red-roofed villas of the new city's garden suburbs
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: tiled);
                    if (tiled) km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.6f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    else if (rnd.Chance(0.35f))
                        km.Box(p.x, p.y, yaw, b.w * 0.3f, b.d * 0.35f, top + b.h - 2f, top + b.h, 2.6f * m, wall, roof, Facade.Plain, m);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.OldHouse:
                {
                    var wall = Jitter(C(0.83f, 0.72f, 0.54f), ref rnd, 0.06f);
                    var roof = Jitter(C(0.86f, 0.77f, 0.60f), ref rnd, 0.05f);
                    // Outside the core, some Ottoman-era houses have red tiled roofs (Jaffa's Ajami, Haifa's lower town).
                    bool tiled = rnd.Chance(t > 0.6f ? 0.14f : 0.03f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: tiled);
                    if (tiled) km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.2f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    else if (rnd.Chance(0.22f)) km.Dome(p.x, p.y, top + b.h - 1f, Mathf.Min(b.w, b.d) * 0.33f, Jitter(C(0.88f, 0.84f, 0.76f), ref rnd, 0.05f));
                    else if (b.storeys >= 2 && rnd.Chance(0.3f))   // a roof room on the flat roof
                        km.Box(p.x + b.w * 0.2f, p.y - b.d * 0.15f, yaw, b.w * 0.45f, b.d * 0.5f, top + b.h - 2f, top + b.h, storey * 0.9f, wall, roof, Facade.Stone, m);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.Village:
                {
                    var wall = Jitter(C(0.80f, 0.70f, 0.53f), ref rnd, 0.07f);
                    var roof = Jitter(C(0.84f, 0.75f, 0.59f), ref rnd, 0.06f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m);
                    if (rnd.Chance(0.10f)) km.Dome(p.x, p.y, top + b.h - 1f, Mathf.Min(b.w, b.d) * 0.3f, roof);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.Templer:
                {
                    var wall = Jitter(C(0.86f, 0.79f, 0.66f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.72f, 0.31f, 0.20f), ref rnd, 0.06f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.55f, roof, wall);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                default:
                {
                    var wall = Jitter(C(0.92f, 0.89f, 0.82f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.74f, 0.31f, 0.20f), ref rnd, 0.08f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.42f, roof, wall);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
            }
        }

        /// <summary>Wall bottom (sunk 2 m below the lowest corner, so a house on a slope stands on a plinth, not on air) and the
        /// ground level the walls rise from (the highest corner, so the slope never buries a storey).</summary>
        static (float bottom, float ground) GroundSpan(Vector2 p, float yawDeg, float w, float d, KitGround ground, float m)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 5; i++)
            {
                float u = i == 4 ? 0 : ((i & 1) == 0 ? -0.5f : 0.5f) * w, v = i == 4 ? 0 : ((i & 2) == 0 ? -0.5f : 0.5f) * d;
                float y = ground.Y(p.x + u * ca - v * sa, p.y + u * sa + v * ca);
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return (lo - 2f * m, hi);
        }

        static Vector2 DryNear(Vector2 c, KitGround ground, float within, ref KitRandom rnd)
        {
            if (ground.Inside(c.x, c.y) && !ground.Water(c.x, c.y)) return c;
            for (int i = 0; i < 64; i++)
            {
                float a = rnd.Range(0f, 2f * Mathf.PI), r = within * (i + 1) / 64f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (ground.Inside(p.x, p.y) && !ground.Water(p.x, p.y)) return p;
            }
            return c;
        }
    }

    /// <summary>The symbolic scale per class (owner ruling 2026-10-11, superseding the 10-25x ruling: match promo 03's
    /// measured scale per class): city blocks about 60x their real size, houses and trees about 100x; geography stays real.
    /// Landmarks (a clock tower, a mosque, a harbour) take the city-block scale. Fields, not constants, so the capture tool
    /// and the player's ?kitscale / ?housescale / ?treescale flags (clamped to <see cref="Min"/>..<see cref="Max"/>) can show
    /// another scale. Counts are tuned at <see cref="CountReference"/> and fall with the square of the scale.</summary>
    public static class KitScale
    {
        public const float DefaultCityBlocks = 60f, DefaultHouses = 100f, DefaultTrees = 100f;
        public const float Min = 5f, Max = 200f;
        /// <summary>The scale the building and tree counts were tuned at: a class drawn at k times this has 1/k^2 as many.</summary>
        public const float CountReference = 25f;
        public static float CityBlocks = DefaultCityBlocks;
        public static float Houses = DefaultHouses;
        public static float Trees = DefaultTrees;

        public static float Landmarks => CityBlocks;

        public static float Clamp(float v) => Mathf.Clamp(v, Min, Max);

        public static void Reset() { CityBlocks = DefaultCityBlocks; Houses = DefaultHouses; Trees = DefaultTrees; }
    }
}
