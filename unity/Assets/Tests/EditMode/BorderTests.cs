#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>promo-borders: the region border renderer (MapBorders, StatecraftBorders.hlsl). The rendered tests draw the
/// synthetic BorderProof rings: the repository holds no real region data yet, and the game draws none.</summary>
public class BorderTests
{
    [TearDown]
    public void Reset()
    {
        MapShaderGlobals.PinWaterTime(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    internal static (MapScene map, Camera cam, CameraRig rig) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        MapShaderGlobals.PinWaterTime(100f);   // renders compared pixel for pixel: the sea must not move between them
        var cam = Camera.main;
        return (map, cam, cam.GetComponent<CameraRig>());
    }

    [Test]
    public void ARegionLayerRejectsBadIdsAndRegionsWithoutAStyle()
    {
        var hexes = new Dictionary<(int q, int r), int> { [(0, 0)] = 1 };
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle>()));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary, [0] = BorderStyle.Primary }));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = (BorderStyle)7 }));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer(" ", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary }));
        var ok = new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Secondary });
        Assert.AreEqual(1, ok.RegionOf(0, 0)); Assert.AreEqual(0, ok.RegionOf(1, 0));
        Assert.AreEqual(BorderStyle.Secondary, ok.StyleOf(1)); Assert.AreEqual(BorderStyle.None, ok.StyleOf(2));
    }

    [Test]
    public void AHexTexelCarriesItsRegionInTwoBytesAndItsStyle()
    {
        var c = MapBorders.Encode(300, BorderStyle.Secondary);
        Assert.AreEqual((44, 1, 2, 255), ((int)c.r, (int)c.g, (int)c.b, (int)c.a));
        Assert.AreEqual(RegionLayer.MaxId, MapBorders.Encode(RegionLayer.MaxId, BorderStyle.None).r + 256 * MapBorders.Encode(RegionLayer.MaxId, BorderStyle.None).g);
    }

    [Test]
    public void TheProofIsRingsOfLandHexesOnlyWithAPrimaryCentre()
    {
        var (map, _, _) = Open();
        var centre = BorderProof.HexAt(map, BorderProof.Centre.lon, BorderProof.Centre.lat);
        var layer = BorderProof.Build(map.Hexes, centre);
        foreach (var h in map.Hexes.hexes)
        {
            int id = layer.RegionOf(h.q, h.r);
            if (!h.land) { Assert.AreEqual(0, id, $"water hex ({h.q}, {h.r}) is in no region"); continue; }
            int ring = BorderProof.Distance((h.q, h.r), centre) / BorderProof.RingWidth;
            Assert.AreEqual(ring + 1, id);
            Assert.AreEqual(ring == 0 ? BorderStyle.Primary : BorderStyle.Secondary, layer.StyleOf(id));
        }
        Assert.AreEqual(2, BorderProof.Distance((0, 0), (1, 1)));
        Assert.AreEqual(3, BorderProof.Distance((0, 0), (3, -3)));
    }

    [Test]
    public void UseRejectsAHexOffTheMapAndWorksOnlyOnABuiltMap()
    {
        var (map, _, _) = Open();
        var bad = new RegionLayer("t", new Dictionary<(int q, int r), int> { [(500, 500)] = 1 }, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary });
        Assert.Throws<System.ArgumentException>(() => MapBorders.Use(bad));
        Reset();
        Assert.Throws<System.InvalidOperationException>(() => MapBorders.Use(null));
    }

    /// <summary>The game ships no region data, so the borders change no pixel: the "borders" toggle exists, and the frame is
    /// identical with it on and off. Once a layer is in use the toggle removes it, and an empty layer draws nothing.</summary>
    [UnityTest]
    public IEnumerator WithoutRegionDataNothingIsDrawn()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        CollectionAssert.Contains(VisualToggles.Names, MapBorders.ToggleName);
        Assert.IsNull(MapBorders.Current);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var on = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 480, 270);
        Assert.AreEqual(0f, Changed(on, off, 0f), "no layer: the toggle changes nothing");
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        var proof = RenderSample.Render(cam, 480, 270);
        Assert.Greater(Changed(proof, off, 0.02f), 0.002f, "the proof draws");
        VisualToggles.Set(MapBorders.ToggleName, false);
        Assert.AreEqual(0f, Changed(RenderSample.Render(cam, 480, 270), off, 0f), "the toggle removes a layer in use");
        VisualToggles.Set(MapBorders.ToggleName, true);
        MapBorders.Use(new RegionLayer("empty", new Dictionary<(int q, int r), int>(), new Dictionary<int, BorderStyle>()));
        Assert.AreEqual(0f, Changed(RenderSample.Render(cam, 480, 270), off, 0f), "an empty layer draws nothing");
    }

    /// <summary>Share of pixels whose largest channel differs by more than <paramref name="by"/>.</summary>
    internal static float Changed(RenderSample a, RenderSample b, float by)
    {
        int n = 0;
        for (int i = 0; i < a.Pixels.Length; i++)
        {
            Color x = a.Pixels[i], y = b.Pixels[i];
            if (Mathf.Max(Mathf.Abs(x.r - y.r), Mathf.Max(Mathf.Abs(x.g - y.g), Mathf.Abs(x.b - y.b))) > by) n++;
        }
        return (float)n / a.Pixels.Length;
    }

    /// <summary>One walk across a hex edge at one point along it: the pixels from 1.5 km inside the inner hex to 1.5 km inside
    /// the outer one, with the borders on and off, and each pixel's signed distance (m; negative: the inner hex).</summary>
    internal sealed class Walk
    {
        public readonly List<Color> On = new List<Color>(), Off = new List<Color>();
        public readonly List<float> SignedM = new List<float>();
        public readonly List<Vector2> Px = new List<Vector2>();
    }

    const float WalkM = 1500f, StepM = 10f;

    /// <summary>Walks across the edge between axial hexes <paramref name="inner"/> and <paramref name="outer"/> (neighbours)
    /// at <paramref name="samples"/> points spread over the middle 60% of the edge, at the camera's current pose, rendering
    /// at width x height with the proof drawn (on) and the borders off. Distinct pixels only, in walk order.</summary>
    internal static List<Walk> WalkEdge(MapScene map, Camera cam, (int q, int r) inner, (int q, int r) outer, int samples, int width, int height)
    {
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        var origin = map.Terrain.transform.position;
        Vector2 ci = HexGrid.Centre(inner.q, inner.r, size), co = HexGrid.Centre(outer.q, outer.r, size);
        var n = (co - ci).normalized; var t = new Vector2(-n.y, n.x);
        int steps = Mathf.RoundToInt(2 * WalkM / StepM) + 1;
        var world = new Vector3[samples * steps]; var signed = new float[world.Length];
        for (int i = 0; i < samples; i++)
            for (int k = 0; k < steps; k++)
            {
                float u = samples == 1 ? 0f : -0.3f + 0.6f * i / (samples - 1), s = -WalkM + k * StepM;
                var p = ci + n * (inradius + s) + t * (u * size);
                var w = HexGrid.ToWorld(p, origin, map.Frame.LengthM);
                w.y = map.Terrain.SampleHeight(w) + origin.y;
                world[i * steps + k] = w; signed[i * steps + k] = s;
            }
        var px = new Vector2[world.Length];
        var on = RenderSample.Render(cam, width, height, world, px);
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, width, height, world, px);
        VisualToggles.Set(MapBorders.ToggleName, true);
        var walks = new List<Walk>();
        for (int i = 0; i < samples; i++)
        {
            var walk = new Walk(); var seen = new HashSet<(int, int)>();
            for (int k = 0; k < steps; k++)
            {
                var q = px[i * steps + k]; int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y);
                if (!seen.Add((x, y))) continue;
                walk.On.Add(on.At(x, y)); walk.Off.Add(off.At(x, y)); walk.SignedM.Add(signed[i * steps + k]); walk.Px.Add(new Vector2(x, y));
            }
            walks.Add(walk);
        }
        return walks;
    }

    /// <summary>The proof's primary-secondary edge in the gameplay03 frame: the hex two east of the centre (ring 0, Primary)
    /// and the next one east (ring 1, Secondary).</summary>
    internal static ((int q, int r) inner, (int q, int r) outer) ProofEdge(MapScene map)
    {
        var c = BorderProof.HexAt(map, BorderProof.Centre.lon, BorderProof.Centre.lat);
        return ((c.q + BorderProof.RingWidth - 1, c.r), (c.q + BorderProof.RingWidth, c.r));
    }

    /// <summary>What one side of a walk shows: its most saturated border pixel (the coloured stripe), its lightest (the light
    /// stripe), the land under them (borders off), and how far from the edge (px along the walk) the stripes reach.</summary>
    internal readonly struct Side
    {
        public readonly Color Stripe, Light, Land; public readonly float WidthPx;
        public Side(Color stripe, Color light, Color land, float widthPx) { Stripe = stripe; Light = light; Land = land; WidthPx = widthPx; }
    }

    /// <summary>The side of the walk with sign <paramref name="sign"/> (-1 inner, +1 outer). The stripe is the pixel that
    /// gains most in <paramref name="hue"/> over the land, the light stripe the lightest pixel. The width counts the band's
    /// pixels: those that gained <paramref name="stripeGain"/> in hue (the coloured stripe) or got lighter by deltaE76 >= 8
    /// (the light stripe; the wash, a darker tint, is not counted).</summary>
    internal static Side Read(Walk w, int sign, System.Func<Color, float> hue, float stripeGain = 0.1f)
    {
        int stripe = -1, light = -1, band = 0; float bestHue = float.MinValue, bestLuma = float.MinValue;
        for (int k = 0; k < w.On.Count; k++)
        {
            if (Mathf.Sign(w.SignedM[k]) != sign || Mathf.Abs(w.SignedM[k]) > 900f) continue;
            float gain = hue(w.On[k]) - hue(w.Off[k]);
            if (gain > bestHue) { bestHue = gain; stripe = k; }
            float luma = RenderSample.Luma(w.On[k]);
            if (luma > bestLuma) { bestLuma = luma; light = k; }
            bool lighter = luma > RenderSample.Luma(w.Off[k]) && RenderSample.DeltaE(w.On[k], w.Off[k]) >= 8f;
            if (gain >= stripeGain || lighter) band++;
        }
        return new Side(stripe < 0 ? Color.clear : w.On[stripe], light < 0 ? Color.clear : w.On[light], stripe < 0 ? Color.clear : w.Off[stripe], band);
    }

    internal static float Blueness(Color c) => c.b - c.r;
    internal static float Redness(Color c) => c.r - c.g;

    /// <summary>The promo 03 look at gameplay03 (1920x1080, tilt-shift off): along the whole edge, the Primary side carries a
    /// blue stripe and a near-white one, the Secondary side a red-orange stripe and a cream one, each band about its promo
    /// width (promo 03 at 1920 px: blue + white about 8 px, red + cream about 5 px), the Primary band the wider; deep inside
    /// either hex nothing changes.</summary>
    [UnityTest]
    public IEnumerator BothStylesFollowTheEdgeAtAboutThePromoWidthAtGameplay03()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var layer = BorderProof.Show(map);
        var (inner, outer) = ProofEdge(map);
        Assert.AreEqual(BorderStyle.Primary, layer.StyleOf(layer.RegionOf(inner.q, inner.r)), "the inner hex is the proof's primary disc");
        Assert.AreEqual(BorderStyle.Secondary, layer.StyleOf(layer.RegionOf(outer.q, outer.r)), "the outer hex is its first ring");
        var walks = WalkEdge(map, cam, inner, outer, 9, 1920, 1080);
        var pw = new List<float>(); var sw = new List<float>();
        foreach (var w in walks)
        {
            var p = Read(w, -1, Blueness, 0.45f); var s = Read(w, +1, Redness);   // 0.45: the blue wash gains about 0.3
            TestContext.WriteLine($"primary stripe {p.Stripe} light {p.Light} land {p.Land} width {p.WidthPx:0.0} px | secondary stripe {s.Stripe} light {s.Light} land {s.Land} width {s.WidthPx:0.0} px");
            Assert.Greater(Blueness(p.Stripe) - Blueness(p.Land), 0.15f, "a blue stripe on the primary side");
            Assert.Greater(Mathf.Min(p.Light.r, Mathf.Min(p.Light.g, p.Light.b)), 0.80f, "a near-white stripe on the primary side");
            Assert.Greater(Redness(s.Stripe) - Redness(s.Land), 0.10f, "a red-orange stripe on the secondary side");
            Assert.Greater(RenderSample.Luma(s.Light), RenderSample.Luma(s.Land) + 0.03f, "a light cream stripe on the secondary side");
            Assert.Greater(s.Light.r, s.Light.b + 0.15f, "the secondary light stripe is cream, not white");
            pw.Add(p.WidthPx); sw.Add(s.WidthPx);
            // 1.2 km or more from the edge (past the widest wash at this zoom) the borders change nothing.
            for (int k = 0; k < w.On.Count; k++)
                if (Mathf.Abs(w.SignedM[k]) >= 1200f) Assert.Less(RenderSample.DeltaE(w.On[k], w.Off[k]), 2f, $"{w.SignedM[k]} m from the edge");
        }
        float pm = Median(pw), sm = Median(sw);
        TestContext.WriteLine($"band widths (median px): primary {pm:0.0}, secondary {sm:0.0}");
        // Promo 03 at 1920 px: about 8 px (primary) and 5 px (secondary) on flat ground. The band is metres wide, so these hill
        // slopes facing the camera draw it wider (9-20 px measured): the bounds hold the median.
        Assert.That(pm, Is.InRange(6f, 14f), "primary band width");
        Assert.That(sm, Is.InRange(3.5f, 10f), "secondary band width");
        Assert.Greater(pm, sm, "the primary band is the heavier");
    }

    /// <summary>A hex in a region next to a hex in none (here the coast: the proof leaves the sea out) gets no band: the
    /// middle of such an edge is unchanged.</summary>
    [UnityTest]
    public IEnumerator NoBandRunsAgainstAHexInNoRegion()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var layer = BorderProof.Show(map);
        var centre = BorderProof.HexAt(map, BorderProof.Centre.lon, BorderProof.Centre.lat);
        // The coastal land hex nearest the centre whose west neighbour is sea, and whose land neighbours share its region,
        // so no other border is near that edge.
        var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
        var steps = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
        (int q, int r)? land = null;
        foreach (var h in map.Hexes.hexes.OrderBy(h => BorderProof.Distance((h.q, h.r), centre)))
        {
            if (!h.land || !byHex.TryGetValue((h.q - 1, h.r), out var west) || west.land) continue;
            int id = layer.RegionOf(h.q, h.r);
            if (steps.Any(d => { int o = layer.RegionOf(h.q + d.Item1, h.r + d.Item2); return o != 0 && o != id; })) continue;
            land = (h.q, h.r); break;
        }
        Assert.IsNotNull(land);
        var walks = WalkEdge(map, cam, land!.Value, (land.Value.q - 1, land.Value.r), 3, 960, 540);
        int moved = 0, total = 0;
        foreach (var w in walks)
            for (int k = 0; k < w.On.Count; k++) { total++; if (RenderSample.DeltaE(w.On[k], w.Off[k]) >= 2f) moved++; }
        TestContext.WriteLine($"coast edge at ({land.Value.q}, {land.Value.r}) region {layer.RegionOf(land.Value.q, land.Value.r)}: {moved}/{total} pixels moved");
        Assert.AreEqual(0, moved);
    }

    /// <summary>At the farthest zoom (diorama02) the outlines stay visible but thin: they change some of the frame and never
    /// wash it (promo 03's minimap shows thin region outlines at the overview).</summary>
    [UnityTest]
    public IEnumerator AtTheOverviewTheBandsAreThin()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        var on = RenderSample.Render(cam, 1920, 1080);
        float share = Changed(on, off, 0.06f);
        TestContext.WriteLine($"diorama02: {share:P2} of pixels changed by the proof's outlines");
        Assert.That(share, Is.InRange(0.01f, 0.15f));
    }

    /// <summary>The same layer and pose give the same pixels (no time, no randomness in the borders).</summary>
    [UnityTest]
    public IEnumerator BordersAreDeterministic()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        BorderProof.Show(map);
        var a = RenderSample.Render(cam, 480, 270);
        BorderProof.Show(map);
        var b = RenderSample.Render(cam, 480, 270);
        Assert.AreEqual(0f, Changed(a, b, 0f));
    }

    static float Median(List<float> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
}
