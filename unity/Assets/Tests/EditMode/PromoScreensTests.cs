#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Cabinet and Defence screens laid out as promo/final/04 and 08: the half circle of seats, the Prime
/// Minister's card, the person cards with competence and loyalty bars, the leadership, the ladder, the call-up bar and the
/// defence budget share. Every figure on them is bound to the sim's data, none is invented, and both screens fit the window
/// shapes the HUD supports, with the HUD's keyboard focus rules.</summary>
public class PromoScreensTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game stepped until <paramref name="date"/> (the opening when it is the start).</summary>
    static GameSession At(DateTime date)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; g.State.Date < date; t++)
        {
            if (t >= 400 || g.State.GameOver != null) Assert.Fail($"the historical game did not reach {date:yyyy-MM-dd}");
            g.Step(HistoricalAI.Commands(C, g.State));
        }
        return g;
    }

    /// <summary>One date in each legislature the content has: the Provisional State Council, the First and the Second Knesset.</summary>
    static IEnumerable<DateTime> OneDatePerLegislature() => C.Legislatures.Select(l => l.From < C.Rules.Start ? C.Rules.Start : l.From.AddDays(30));

    [Test]
    public void TheHalfCircleSeatsTheRealLegislatureWithTheCoalitionFirst()
    {
        foreach (var date in OneDatePerLegislature())
        {
            var g = At(date);
            using var ui = new UiTestKit();
            var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
            var leg = Offices.Legislature(C, g.State.Date)!;
            var gov = Offices.Government(C, g.State.Date)!;
            var where = $"{g.State.Date:yyyy-MM-dd} {leg.Id}";
            var house = ui.Q<Hemicycle>("cabinet-hemicycle");
            Assert.AreEqual(leg.Size, house.Size, where);
            Assert.AreEqual(leg.Size, Hemicycle.Layout(leg.Size).Count, where + ": one dot per seat");
            Assert.AreEqual(leg.Size / 2 + 1, house.Majority, where);
            foreach (var kv in leg.Seats.Where(kv => kv.Value > 0))
            {
                var block = house.Groups.Single(b => b.Party == kv.Key);
                Assert.AreEqual(kv.Value, block.Seats, where + " " + kv.Key);
                Assert.AreEqual(gov.Coalition.Contains(kv.Key), block.Coalition, where + " " + kv.Key);
            }
            Assert.AreEqual(leg.Seats.Count(kv => kv.Value > 0), house.Groups.Count, where + ": only parties with seats");
            var (seats, size) = PoliticsSystem.CoalitionSeats(C, g.State.Date);
            Assert.AreEqual(seats, house.Groups.Where(b => b.Coalition).Sum(b => b.Seats), where + ": the coalition's seats");
            // Coalition first: the first <seats> dots from the left are coalition colours, the rest are not.
            var coalitionColours = new HashSet<Color>(house.Groups.Where(b => b.Coalition).Select(b => b.Colour));
            var dots = house.SeatColours();
            for (int i = 0; i < dots.Count; i++)
                Assert.AreEqual(i < seats, dots[i].HasValue && coalitionColours.Contains(dots[i]!.Value), $"{where}: seat {i}");
            Assert.AreEqual(size, dots.Count);
            Assert.AreEqual(leg.Size.ToString(), ui.Q<Label>("cabinet-house-size").text, where);
            Assert.AreEqual($"Coalition: {seats} of {size} seats · stability {PlayerText.Percent(g.State.CoalitionStability)}", ui.Q<Label>("cabinet-coalition").text, where);
            Assert.AreEqual($"Opposition: {leg.Seats.Where(kv => !gov.Coalition.Contains(kv.Key)).Sum(kv => kv.Value)} seats", ui.Q<Label>("cabinet-opposition").text, where);
            Assert.AreEqual($"Majority: {size / 2 + 1} seats", ui.Q<Label>("cabinet-majority").text, where);
            var top = leg.Seats.OrderByDescending(kv => kv.Value).First();
            Assert.AreEqual($"Largest party: {C.Parties.First(p => p.Id == top.Key).Name}, {top.Value} seats", ui.Q<Label>("cabinet-largest").text, where);
            Assert.AreEqual($"Answerable to the {leg.Body} · {leg.Size} members", ui.Q<Label>("cabinet-legislature").text, where);
            Assert.AreEqual((float)g.State.CoalitionStability, ui.Q<UiBar>("cabinet-stability").Value, 1e-6f, where);
            foreach (var kv in leg.Seats.Where(kv => kv.Value > 0))
                Assert.AreEqual($"{C.Parties.First(p => p.Id == kv.Key).Name} {kv.Value}", ui.Q<Label>("party-" + kv.Key + "-label").text, where);
            cabinet.Dispose();
        }
    }

    [Test]
    public void TheMajorityLineSeparatesAMajorityFromTheRest()
    {
        foreach (var size in new[] { 37, 120, 1, 2, 7, 61 })
        {
            var seats = Hemicycle.Layout(size);
            Assert.AreEqual(size, seats.Count, size + " seats");
            for (int i = 1; i < seats.Count; i++)
                Assert.GreaterOrEqual(Mathf.Atan2(seats[i - 1].y, seats[i - 1].x) + 1e-4f, Mathf.Atan2(seats[i].y, seats[i].x), $"{size}: left to right");
            foreach (var p in seats) Assert.LessOrEqual(p.magnitude, 1.0001f, $"{size}: inside the unit half circle");
            // No two dots overlap.
            var r = Hemicycle.DotRadius(size);
            for (int i = 0; i < seats.Count; i++)
                for (int j = i + 1; j < seats.Count; j++)
                    Assert.Greater((seats[i] - seats[j]).magnitude, 2 * r, $"{size}: dots {i} and {j} overlap");
        }
        var house = new Hemicycle();
        house.Set(new[] { new Hemicycle.Group("a", 30, Color.blue, true), new Hemicycle.Group("b", 7, Color.gray, false) }, 37);
        var line = house.MajorityAngle();
        var layout = Hemicycle.Layout(37);
        // Seats sharing the line's angle (the middle column, when rows hold odd counts) may sit on it.
        for (int i = 0; i < 37; i++)
        {
            var angle = Mathf.Atan2(layout[i].y, layout[i].x);
            if (i < house.Majority) Assert.GreaterOrEqual(angle, line - 1e-4f, $"seat {i} is on the majority side");
            else Assert.LessOrEqual(angle, line + 1e-4f, $"seat {i} is on the other side");
        }
        Assert.Throws<ArgumentException>(() => house.Set(new[] { new Hemicycle.Group("a", 38, Color.blue, true) }, 37), "never more seats than the body has");
        house.Set(new[] { new Hemicycle.Group("a", 30, Color.blue, true) }, 37);
        Assert.AreEqual(7, house.SeatColours().Count(c => c == null), "unassigned seats stay unassigned, never given to a party");
    }

    /// <summary>Each card is its office holder's people.yaml record; the Prime Minister's card lists every office the PM
    /// holds (Ben-Gurion was also Minister of Defense in 1948).</summary>
    [Test]
    public void EveryCardShowsItsHoldersRealRecord()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        var lineup = Cabinet.Lineup(C, g.State);
        foreach (var seat in lineup)
        {
            var id = "seat-" + seat.Office.Id;
            Assert.IsNotNull(ui.Root.Q<Button>(id), id);
            if (seat.Office.Id != CabinetPresenter.PrimeMinister)
                Assert.AreEqual(seat.Office.Title, ui.Q<Label>(id + "-office").text, id);
            if (seat.Holder == null)
            {
                Assert.AreEqual("(vacant)", ui.Q<Label>(id + "-holder").text, id);
                Assert.IsNull(ui.Root.Q<UiBar>(id + "-competence"), id + ": no bars for a vacancy");
                continue;
            }
            var p = seat.Holder;
            Assert.AreEqual(p.Name, ui.Q<Label>(id + "-holder").text, id);
            var party = Offices.PartyOf(C, p, g.State.Date);
            var partyName = party == "" ? "no party" : C.Parties.First(x => x.Id == party).Name;
            StringAssert.EndsWith(partyName, ui.Q<Label>(id + "-party").text, id);
            Assert.AreEqual(p.Competence / 10f, ui.Q<UiBar>(id + "-competence").Value, 1e-6f, id);
            Assert.AreEqual(p.Loyalty / 10f, ui.Q<UiBar>(id + "-loyalty").Value, 1e-6f, id);
            Assert.AreEqual(p.Competence.ToString(), ui.Q<Label>(id + "-competence-value").text, id);
            Assert.AreEqual(p.Loyalty.ToString(), ui.Q<Label>(id + "-loyalty-value").text, id);
        }
        var pm = Cabinet.Holder(C, g.State, "prime_minister")!;
        var held = lineup.Where(x => x.Holder?.Id == pm.Id).Select(x => x.Office.Title).ToList();
        Assert.AreEqual(string.Join(" · ", held), ui.Q<Label>("seat-prime_minister-office").text);
        CollectionAssert.Contains(held, "Minister of Defense", "Ben-Gurion held Defence too");
        Assert.AreEqual("Party: Mapai", ui.Q<Label>("seat-prime_minister-party").text);
        Assert.IsNull(ui.Q<ScrollView>("cabinet-seats").Q<Button>("seat-prime_minister"), "the PM is the side card, not a grid card");
    }

    [Test]
    public void APersonWithoutAPortraitGetsTheirInitialsNeverAnotherFace()
    {
        Assert.AreEqual("DB", PersonCard.Initials("David Ben-Gurion"));
        Assert.AreEqual("HS", PersonCard.Initials("Haim-Moshe Shapira"));
        Assert.AreEqual("M", PersonCard.Initials("Mordechai"));
        Assert.AreEqual("", PersonCard.Initials(""));
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        var catalog = ScriptableObject.CreateInstance<PortraitCatalog>();
        try
        {
            catalog.entries.Add(new PortraitCatalog.Entry { personId = "kaplan", image = Texture2D.whiteTexture, focusY = 0.3f });
            new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, catalog);
            new DefencePresenter(ui.Root, new SimBridge(g, Text), Text, catalog);
            foreach (var card in ui.Root.Query<VisualElement>(className: "card-plate").ToList())
            {
                var prefix = card.name.Substring(0, card.name.Length - "-plate".Length);
                var portrait = ui.Q<VisualElement>(prefix + "-portrait");
                var initials = ui.Q<Label>(prefix + "-initials");
                var holder = ui.Q<Label>(prefix + "-holder").text;
                bool kaplan = holder.StartsWith("Eliezer Kaplan");
                Assert.AreEqual(kaplan ? DisplayStyle.Flex : DisplayStyle.None, portrait.style.display.value, prefix);
                Assert.AreEqual(kaplan ? DisplayStyle.None : DisplayStyle.Flex, initials.style.display.value, prefix);
                if (!kaplan) Assert.AreEqual(holder == "(vacant)" ? "" : PersonCard.Initials(holder), initials.text, prefix);
            }
        }
        finally { Object.DestroyImmediate(catalog); }
    }

    [Test]
    public void TheLeadershipIsTheRealCommandAndTheBarsAreTheSims()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        var bridge = new SimBridge(g, Text);
        new ToastPresenter(ui.Root, bridge, Text);
        var defence = new DefencePresenter(ui.Root, bridge, Text);
        foreach (var office in DefencePresenter.Leaders)
        {
            var holder = Cabinet.Holder(C, g.State, office)!;
            Assert.AreEqual(holder.Name, ui.Q<Label>("lead-" + office + "-holder").text, office);
            Assert.AreEqual(C.Offices.First(o => o.Id == office).Title, ui.Q<Label>("lead-" + office + "-office").text, office);
            Assert.AreEqual(holder.Competence / 10f, ui.Q<UiBar>("lead-" + office + "-competence").Value, 1e-6f, office);
            Assert.AreEqual(holder.Loyalty / 10f, ui.Q<UiBar>("lead-" + office + "-loyalty").Value, 1e-6f, office);
        }
        var bar = ui.Q<UiBar>("mob-bar");
        Assert.AreEqual((float)g.State.Mobilization, bar.Value, 1e-6f);
        Assert.AreEqual((float)SecuritySystem.MinMobilization(C, g.State.Rung), bar.Mark!.Value, 1e-6f, "the level's minimum is marked");
        Assert.AreEqual(PlayerText.Percent(g.State.Mobilization), ui.Q<Label>("mob-percent").text);
        Assert.AreEqual((float)g.State.Budget[DefencePresenter.BudgetLine], ui.Q<UiBar>("defence-budget-bar").Value, 1e-6f);
        Assert.AreEqual($"{PlayerText.Percent(g.State.Budget[DefencePresenter.BudgetLine])} of budget spending", ui.Q<Label>("defence-budget-share").text);
        // An order to lower the level: the target rung is marked, and the bar's minimum follows it.
        ui.Press("de-escalate");
        Assert.IsTrue(ui.Q<VisualElement>("rung-4").ClassListContains("rung-target"));
        Assert.IsTrue(ui.Q<VisualElement>("rung-5").ClassListContains("rung-current"));
        Assert.IsFalse(ui.Q<VisualElement>("rung-5").ClassListContains("rung-target"));
        Assert.AreEqual((float)SecuritySystem.MinMobilization(C, 4), bar.Mark!.Value, 1e-6f);
        ui.Press("mob-down");
        Assert.AreEqual((float)defence.ProposedMobilization, bar.Value, 1e-6f, "the bar shows the ordered call-up");
        ui.Press("escalate");
        Assert.IsFalse(ui.Root.Query<VisualElement>(className: "rung-target").ToList().Any(), "no order, no target");
        defence.Dispose();
    }

    /// <summary>Every number printed on either screen, and every bar, comes from the sim's data: the figures the sim's own
    /// formatting gives for real values (competence, loyalty, seats, size, majority, stability, call-up, its minimum and
    /// cost, the budget share, the ceasefire clock, the levels), so no promo figure (readiness, strength) slipped in.</summary>
    [Test]
    public void NoFigureOnEitherScreenIsInvented()
    {
        foreach (var date in OneDatePerLegislature())
        {
            var g = At(date);
            var s = g.State;
            using var ui = new UiTestKit();
            var bridge = new SimBridge(g, Text);
            var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
            var defence = new DefencePresenter(ui.Root, bridge, Text);
            var allowed = new HashSet<string>();
            void Add(string formatted) { foreach (Match m in Number.Matches(formatted)) allowed.Add(m.Value); }
            var leg = Offices.Legislature(C, s.Date)!;
            foreach (var p in C.People) { Add(p.Competence.ToString()); Add(p.Loyalty.ToString()); }
            foreach (var v in leg.Seats.Values) Add(v.ToString());
            var (seats, size) = PoliticsSystem.CoalitionSeats(C, s.Date);
            Add(seats.ToString()); Add(size.ToString()); Add((size / 2 + 1).ToString()); Add((size - seats).ToString());
            Add(PlayerText.Percent(s.CoalitionStability));
            for (int rung = 1; rung <= 5; rung++) { Add(rung.ToString()); Add(PlayerText.Percent(SecuritySystem.MinMobilization(C, rung))); }
            Add(PlayerText.Percent(s.Mobilization)); Add(PlayerText.Money(C, "treasury", EconomySystem.MobilizationCost(C, s.Mobilization)));
            Add(PlayerText.Percent(s.Budget[DefencePresenter.BudgetLine]));
            Add(s.CeasefireWeeksLeft.ToString()); Add(C.Rules.CeasefireWeeks.ToString());
            Add(leg.Size.ToString());
            Add(PlayerText.Percent(DefencePresenter.MobilizationStep));   // the -5% / +5% buttons
            foreach (var name in new[] { "screen-cabinet", "screen-defence" })
                foreach (var label in ui.Q<VisualElement>(name).Query<TextElement>().ToList().Where(l => !string.IsNullOrEmpty(l.text)))
                    foreach (Match m in Number.Matches(label.text))
                        if (!allowed.Contains(m.Value) && !IsDatePart(label.text, m)) Assert.Fail($"{s.Date:yyyy-MM-dd}: '{m.Value}' in {label.name} \"{label.text}\" is not a figure the sim has");
            // Bars: each is a real value on its scale.
            var real = new List<float> { (float)s.CoalitionStability, (float)s.Mobilization, (float)s.Budget[DefencePresenter.BudgetLine] };
            real.AddRange(C.People.SelectMany(p => new[] { p.Competence / 10f, p.Loyalty / 10f }));
            foreach (var bar in ui.Q<VisualElement>("screen-cabinet").Query<UiBar>().ToList().Concat(ui.Q<VisualElement>("screen-defence").Query<UiBar>().ToList()))
                Assert.IsTrue(real.Any(v => Mathf.Abs(v - bar.Value) < 1e-6f), $"{s.Date:yyyy-MM-dd}: bar {bar.name} = {bar.Value} is no sim value");
            // No promo figure the sim lacks.
            var words = string.Join("\n", ui.Q<VisualElement>("screen-defence").Query<TextElement>().ToList().Select(l => l.text));
            foreach (var missing in new[] { "Readiness", "Strength", "Intelligence", "Procurement", "Stance", "Air Force", "Navy" })
                StringAssert.DoesNotContain(missing, words, "the sim has no " + missing);
            cabinet.Dispose(); defence.Dispose();
        }
    }

    static readonly Regex Number = new Regex(@"\d+(?:[.,]\d+)*");

    /// <summary>A day or a year inside a "15 May 1948" date (an appointment date in a candidate's reason).</summary>
    static bool IsDatePart(string text, Match m) => Regex.IsMatch(text, @"\b\d{1,2} [A-Z][a-z]+ \d{4}\b") && Regex.Matches(text, @"\b(\d{1,2}) [A-Z][a-z]+ (\d{4})\b").Cast<Match>().Any(d => d.Index <= m.Index && m.Index < d.Index + d.Length);

    static float Lum(Color c, Color under)
    {
        float Ch(float v, float u) { var x = v * c.a + u * (1 - c.a); return x <= 0.04045f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f); }
        return 0.2126f * Ch(c.r, under.r) + 0.7152f * Ch(c.g, under.g) + 0.0722f * Ch(c.b, under.b);
    }

    static float Contrast(Color a, Color b, Color under) { float x = Lum(a, under), y = Lum(b, under); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }

    static readonly Color ScreenNavy = new Color32(11, 18, 32, 255), Deep = new Color32(9, 14, 26, 255);

    /// <summary>Non-text contrast (WCAG 1.4.11): every bar's fill is at least 3:1 against its track, and every party colour
    /// at least 3:1 against the screen behind the half circle.</summary>
    [UnityTest]
    public IEnumerator BarsAndPartyColoursMeetThreeToOne()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        var defence = new DefencePresenter(ui.Root, bridge, Text);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        new ScreenNav(ui.Root).Open("cabinet");
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        for (int i = 0; i < 4; i++) yield return null;
        try
        {
            var seen = new HashSet<string>();
            foreach (var bar in ui.Root.Query<UiBar>().ToList())
            {
                var modifier = bar.GetClasses().FirstOrDefault(c => c.StartsWith("ui-bar--"));
                Assert.IsNotNull(modifier, bar.name + ": a colour modifier");
                seen.Add(modifier!);
                var ratio = Contrast(bar.Fill.resolvedStyle.backgroundColor, bar.resolvedStyle.backgroundColor, ScreenNavy);
                Assert.GreaterOrEqual(ratio, 3f, $"{bar.name} ({modifier}): fill on track {ratio:0.00}:1");
            }
            CollectionAssert.IsSupersetOf(seen, new[] { "ui-bar--competence", "ui-bar--loyalty", "ui-bar--stability", "ui-bar--mobilization", "ui-bar--budget" });
            foreach (var colour in CabinetPresenter.CoalitionColours.Concat(CabinetPresenter.OppositionColours))
                Assert.GreaterOrEqual(Contrast(colour, ScreenNavy, ScreenNavy), 3f, $"party colour {colour}");
        }
        finally { cabinet.Dispose(); defence.Dispose(); }
    }

    /// <summary>The HUD's focus rules on the cards (reviews I3, N1): keyboard focus changes a card by at least 3:1 and keeps
    /// its text at least 4.5:1, and a pointer click leaves no card lit.</summary>
    [UnityTest]
    public IEnumerator ACardsFocusIsVisibleAndAClickLeavesItUnlit()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        var blur = new PointerBlur(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        new ScreenNav(ui.Root).Open("cabinet");
        try
        {
            for (int i = 0; i < 4; i++) yield return null;
            foreach (var name in new[] { "seat-finance_minister", "seat-prime_minister" })
            {
                var card = ui.Q<Button>(name);
                var before = card.resolvedStyle.backgroundColor;
                card.Focus();
                for (int i = 0; i < 10 && card.focusController.focusedElement != card; i++) yield return null;
                yield return null; yield return null;
                Assert.AreSame(card, card.focusController.focusedElement, name + ": focused");
                var fill = card.resolvedStyle.backgroundColor;
                Assert.GreaterOrEqual(Contrast(before, fill, Deep), 3f, name + ": focus changes the card by 3:1");
                foreach (var label in card.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && l.resolvedStyle.display == DisplayStyle.Flex && l.GetFirstAncestorOfType<VisualElement>()?.ClassListContains("card-plate") != true))
                    Assert.GreaterOrEqual(Contrast(label.resolvedStyle.color, fill, Deep), 4.5f, $"{name}: '{label.text}' on the focus fill");
                using (var e = ClickEvent.GetPooled()) { e.target = card; card.SendEvent(e); }
                yield return null; yield return null;
                Assert.AreNotSame(card, card.focusController.focusedElement, name + ": a click leaves no focus");
            }
        }
        finally { cabinet.Dispose(); blur.Dispose(); }
    }

    /// <summary>Both screens at the window shapes the HUD supports (16:9 at 1920x1080 and 1366x768, 16:10, 4:3, 5:4, the
    /// wide-and-short browser, square and narrower): each opens under the top bar, clear of the event column and the
    /// minimap (over the info panel, which it is drawn above), and nothing on it leaves it or its panel sideways; every
    /// one-line text fits its box.</summary>
    [UnityTest]
    public IEnumerator BothScreensFitEveryWindowShape()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1366, 768), (1280, 720), (1440, 900), (1920, 1200), (1024, 768), (1280, 1024), (1531, 784), (1024, 1024), (960, 1080), (800, 1000) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("ScreensTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var owned = new List<IDisposable>();
            try
            {
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var bridge = new SimBridge(g, Text);
                owned.Add(new HudPresenter(root, bridge, Text));
                owned.Add(new CabinetPresenter(root, bridge, Text, null));
                owned.Add(new DefencePresenter(root, bridge, Text));
                var nav = new ScreenNav(root); owned.Add(nav);
                foreach (var s in new[] { "cabinet", "defence" })
                {
                    nav.Open(s);
                    for (int i = 0; i < 6; i++) yield return null;
                    var where = $"{w}x{h} {s}";
                    var screen = root.Q("screen-" + s); var sr = screen.worldBound;
                    if (!(sr.width > 300)) { problems.Add($"{where}: the screen is {sr}"); continue; }
                    if (sr.yMin < root.Q("top-bar").worldBound.yMax - 0.5f) problems.Add($"{where}: starts over the top bar");
                    foreach (var f in new[] { "right-column" })
                        if (sr.Overlaps(root.Q(f).worldBound)) problems.Add($"{where}: overlaps {f} {root.Q(f).worldBound}");
                    var parent = root.Q("root");
                    if (parent.IndexOf(screen) < parent.IndexOf(root.Q("info-panel"))) problems.Add($"{where}: drawn under the info panel");
                    // Everything shown stays inside the screen sideways, and inside its panel (a card, a Defence panel, the house).
                    foreach (var e in screen.Query<VisualElement>().ToList().Where(e => e != screen && e.resolvedStyle.display == DisplayStyle.Flex && e.worldBound.width > 0
                                                                                         && !(e is UiFrame) && e.GetFirstAncestorOfType<Scroller>() == null && !(e is Scroller)))
                    {
                        var r = e.worldBound;
                        var clip = e.GetFirstAncestorOfType<ScrollView>();
                        if (clip != null && (r.yMax < clip.contentViewport.worldBound.yMin || r.yMin > clip.contentViewport.worldBound.yMax)) continue;   // scrolled out of view
                        if (r.xMin < sr.xMin - 0.5f || r.xMax > sr.xMax + 0.5f) problems.Add($"{where}: {e.name}/{e.GetClasses().FirstOrDefault()} {r} leaves the screen {sr}");
                        var panel = e.parent == null ? null : Ancestor(e);
                        if (panel != null && (r.xMin < panel.worldBound.xMin - 0.5f || r.xMax > panel.worldBound.xMax + 0.5f))
                            problems.Add($"{where}: {e.name}/{e.GetClasses().FirstOrDefault()} {r} leaves {panel.name}/{panel.GetClasses().FirstOrDefault()} {panel.worldBound}");
                        if (e is Label l && !string.IsNullOrEmpty(l.text) && l.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
                        {
                            var natural = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                            if (natural.x > l.contentRect.width + 1f) problems.Add($"{where}: '{l.text}' ({natural.x:0} px) is cut to {l.contentRect.width:0} px");
                        }
                    }
                    if (s == "cabinet")
                    {
                        var hemi = root.Q("cabinet-hemicycle").worldBound;
                        if (!(hemi.width > 100 && hemi.height > 60)) problems.Add($"{where}: the half circle is {hemi}");
                        var cards = root.Q<ScrollView>("cabinet-seats").Query<Button>(className: "seat").ToList();
                        if (cards.Count < 5) problems.Add($"{where}: {cards.Count} cards");
                        if (!(cards[0].worldBound.yMin < root.Q<ScrollView>("cabinet-seats").contentViewport.worldBound.yMax - 100)) problems.Add($"{where}: no card row in view");
                    }
                }
            }
            finally
            {
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems.Take(40)));
    }

    /// <summary>The nearest box an element belongs in: a card, a Defence panel or the house.</summary>
    static VisualElement? Ancestor(VisualElement e)
    {
        for (var p = e.parent; p != null; p = p.parent)
            if (p.ClassListContains("seat") || p.ClassListContains("leader") || p.ClassListContains("defence-panel") || p.ClassListContains("cabinet-house") || p.ClassListContains("cabinet-side"))
                return p;
        return null;
    }
}
