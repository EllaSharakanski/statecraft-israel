#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The decision card in the middle (decision cards, 2026-10-13): while it shows, its backdrop takes the pointer and
/// the panels under it are out of reach (ScreenNav.DisableCovered, as #409 does for screens); Hide collapses it to
/// "Decision waiting", which brings it back; a new event opens it again; a new event puts the focus on the card (no option
/// lit as if recommended; Tab goes on to the options) and an open one never takes it back; any open screen, and the start screen, hide it and take it out of reach. Checked on
/// a one-row HUD (1920x1080) and the narrow two-row HUD (960x1080).</summary>
public class DecisionCardTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    sealed class Rig : System.IDisposable
    {
        public readonly VisualElement Root; public readonly SimBridge Bridge; public readonly ScreenNav Nav; public readonly EventCardPresenter Card;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt; readonly List<System.IDisposable> owned = new List<System.IDisposable>();

        public Rig(int w, int h, string eventId)
        {
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("DecisionCardTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
            Bridge = new SimBridge(Pending(eventId), Text);
            owned.Add(new HudPresenter(Root, Bridge, Text));
            Nav = new ScreenNav(Root); owned.Add(Nav);
            owned.Add(new InfoPanelPresenter(Root, Bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)));
            Card = new EventCardPresenter(Root, Bridge, Text, null); owned.Add(Card);
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }

        public VisualElement Hud => Root.Q("root");
        public Button FirstOption => Root.Q("event-options").Q<Button>();
        public VisualElement CardElement => Root.Q("event-card");
        public Button Hide => Root.Q<Button>("event-hide");
        public Button Waiting => Root.Q<Button>("event-reopen");
        public Focusable? Focused => Root.panel.focusController.focusedElement;
        public bool Shows(string name) => Root.Q(name).resolvedStyle.display == DisplayStyle.Flex;
    }

    /// <summary>A new game in the event's own time with only that event pending.</summary>
    static GameSession Pending(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return g;
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void Press(Button b)
    {
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
    }

    static void Covered(Rig r, bool covered, string where)
    {
        Assert.AreEqual(!covered, r.Root.Q("info-panel").enabledSelf, $"{where}: the info panel is {(covered ? "disabled" : "enabled")}");
        Assert.AreEqual(!covered, r.Root.Q("right-column").enabledSelf, $"{where}: the minimap column is {(covered ? "disabled" : "enabled")}");
    }

    static readonly (int w, int h)[] Tiers = { (1920, 1080), (960, 1080) };

    [UnityTest]
    public IEnumerator TheOpenCardTakesThePointerAndThePanelsUnderIt()
    {
        foreach (var (w, h) in Tiers)
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var where = $"{w}x{h}";
            Assert.AreEqual(w == 960, r.Hud.ClassListContains(HudPresenter.TwoRowClass), $"{where}: precondition, the HUD tier");
            Assert.IsTrue(r.Shows("event-card"), $"{where}: the card shows");
            Assert.IsTrue(r.Hud.ClassListContains(EventCardPresenter.OpenClass) && r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Assert.IsTrue(r.Shows("event-scrim"), $"{where}: the backdrop shows");
            Covered(r, true, where);
            // A point of the map clear of every panel: the backdrop takes it, so a map click or drag stops there.
            // (The layer's 16 px gutter at the left, just under the bar: the backdrop reaches the window's edge.)
            var free = new Vector2(4f, r.Root.Q("top-bar").worldBound.yMax + 4f);
            Assert.AreSame(r.Root.Q("event-scrim"), r.Root.panel.Pick(free), $"{where}: the backdrop takes the pointer at {free}");
            Assert.AreSame(r.CardElement, r.Focused, $"{where}: the new event's card has the focus, no option lit");
            Assert.IsTrue(r.FirstOption.canGrabFocus, $"{where}: its options are in reach");

            // Hide: the card collapses to "Decision waiting", which takes the focus; the map and the panels come back.
            Press(r.Hide);
            yield return Frames(3);
            Assert.IsTrue(r.Card.Collapsed, where);
            Assert.IsFalse(r.Shows("event-card"), $"{where}: the card is hidden");
            Assert.IsFalse(r.Shows("event-scrim"), $"{where}: the backdrop is gone");
            Assert.IsTrue(r.Shows("event-reopen"), $"{where}: Decision waiting shows");
            Assert.AreEqual("Decision waiting", r.Waiting.text);
            Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Covered(r, false, where);
            Assert.AreSame(r.Waiting, r.Focused, $"{where}: Decision waiting has the focus");
            Assert.IsFalse(r.FirstOption.canGrabFocus, $"{where}: the hidden card's options are out of reach");
            var pick = r.Root.panel.Pick(free);
            Assert.IsTrue(pick == null || !r.Root.Q("event-layer").Contains(pick), $"{where}: the map takes the pointer again ({pick?.name})");

            // Decision waiting: the card is back, its first option focused, the panels out of reach again.
            Press(r.Waiting);
            yield return Frames(3);
            Assert.IsFalse(r.Card.Collapsed, where);
            Assert.IsTrue(r.Shows("event-card") && r.Shows("event-scrim"), $"{where}: reopened");
            Assert.IsFalse(r.Shows("event-reopen"), where);
            Covered(r, true, where);
            Assert.AreSame(r.CardElement, r.Focused, $"{where}: the reopened card has the focus");
            Assert.IsTrue(r.FirstOption.canGrabFocus, $"{where}: its options are in reach again");
        }
    }

    [UnityTest]
    public IEnumerator ANewEventOpensACollapsedCard()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        r.Card.Collapse();
        yield return Frames(2);
        Assert.IsTrue(r.Card.Collapsed);
        r.Bridge.Replace(Pending("hula_drainage"));
        yield return Frames(3);
        Assert.AreEqual("hula_drainage", r.Card.Shown?.Id);
        Assert.IsFalse(r.Card.Collapsed, "a new event opens the card again");
        Assert.IsTrue(r.Shows("event-card") && !r.Shows("event-reopen"));
        Assert.AreSame(r.CardElement, r.Focused, "and puts the focus on the card");
    }

    [UnityTest]
    public IEnumerator AnOpenEventDoesNotTakeTheFocusBack()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        var endTurn = r.Root.Q<Button>("end-turn");
        endTurn.Focus();
        Assert.AreSame(endTurn, r.Focused, "precondition");
        r.Card.Render();   // the bridge changed (a queued order, a refused one), the same event still open
        yield return Frames(3);
        Assert.AreSame(endTurn, r.Focused, "the top bar keeps the focus");
    }

    [UnityTest]
    public IEnumerator AnOpenScreenOrTheStartScreenHidesTheCardAndTakesItOutOfReach()
    {
        foreach (var (w, h) in Tiers)
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var where = $"{w}x{h}";
            foreach (var s in ScreenNav.Screens)
            {
                r.Nav.Open(s);
                yield return Frames(3);
                Assert.IsFalse(r.Shows("event-layer"), $"{where} {s}: the decision layer is hidden");
                Assert.IsFalse(r.Root.Q("event-layer").enabledSelf, $"{where} {s}: and disabled");
                Assert.IsFalse(r.FirstOption.canGrabFocus, $"{where} {s}: its options are out of reach");
                r.FirstOption.Focus();
                Assert.AreNotSame(r.FirstOption, r.Focused, $"{where} {s}: focus does not reach them");
                Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), $"{where} {s}: no backdrop under a screen");
                // The panels beside a screen are as the screen leaves them (one row, Budget: beside it, in reach).
                if (w == 1920 && s == "budget") Covered(r, false, $"{where} {s}");
            }
            r.Nav.Open(null);
            yield return Frames(3);
            Assert.IsTrue(r.Shows("event-layer") && r.Shows("event-card"), $"{where}: closing the screen brings the card back");
            Assert.IsTrue(r.Root.Q("event-layer").enabledSelf && r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Covered(r, true, where);

            r.Hud.AddToClassList(StartScreenPresenter.OpenClass);
            ScreenNav.DisableCovered(r.Root);
            yield return Frames(2);
            Assert.IsFalse(r.Shows("event-layer"), $"{where}: the start screen hides the layer");
            Assert.IsFalse(r.Root.Q("event-layer").enabledSelf, $"{where}: and disables it");
            Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            r.Hud.RemoveFromClassList(StartScreenPresenter.OpenClass);
            ScreenNav.DisableCovered(r.Root);
        }
    }

    [UnityTest]
    public IEnumerator TheToastClearsTheOpenCardsOptions()
    {
        foreach (var (w, h) in Tiers)
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var toast = r.Root.Q<Label>("toast");
            toast.text = "Not done: Not enough attention (need 4.0, have 2.0)";
            toast.style.display = DisplayStyle.Flex;
            yield return Frames(3);
            var t = toast.worldBound;
            Assert.IsFalse(t.Overlaps(r.Root.Q("event-options").worldBound), $"{w}x{h}: the toast {t} covers the options");
            Assert.IsFalse(t.Overlaps(r.Root.Q("event-title").worldBound), $"{w}x{h}: the toast {t} covers the title");
            Assert.Greater(r.Root.Q("root").IndexOf(toast), r.Root.Q("root").IndexOf(r.Root.Q("event-layer")), "the toast is drawn above the layer");
        }
    }
}
