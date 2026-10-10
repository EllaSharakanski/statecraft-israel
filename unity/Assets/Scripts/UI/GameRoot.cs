#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Starts the game once GameBoot has loaded the inputs and built the map (GameBoot.Ready): continues the
    /// autosave or starts a new game (GameStart.Begin, with its notes in the toast), creates every screen, autosaves each
    /// turn, and wires the camera and the map pointer to the game panel (UiPointer.IsOverUi), map clicks to the info panel,
    /// the minimap to the camera, and State.Placements to the placement layer. Disabling it disposes all of that.
    /// With "smoke" in the page URL (WebGL, e.g. ?smoke=5) or -smoke N (desktop) it starts a fresh game with the golden
    /// seed (the player's autosave is neither read nor written), plays N turns through the UI's own buttons and logs
    /// "SMOKE OK ..." (or "SMOKE FAIL ...").
    /// ExecuteAlways only so that disabling it disposes the presenters in edit mode too (tests); GameBoot raises Ready
    /// in play mode and players only.</summary>
    [ExecuteAlways]
    public sealed class GameRoot : MonoBehaviour
    {
        public const int DefaultSmokeTurns = 5;

        [SerializeField] GameBoot boot = null!;
        [SerializeField] UIDocument document = null!;
        [SerializeField] TextAsset strings = null!;
        [SerializeField] PortraitCatalog portraits = null!;
        [SerializeField] MapScene map = null!;
        [SerializeField] PlacementLayer placements = null!;
        [SerializeField] MapPointer pointer = null!;
        [SerializeField] CameraController cameraController = null!;

        /// <summary>Everything Begin created that must be undone, disposed in reverse order.</summary>
        readonly List<IDisposable> owned = new List<IDisposable>();
        MinimapPresenter? minimap;
        CameraRig? rig;
        List<(string, int, int, DateTime)>? drawn;

        public SimBridge? Bridge { get; private set; }
        /// <summary>Where the autosave lives; null (the default) = SaveStore.ForPlatform(). Tests set a temporary folder.</summary>
        public SaveStore? Store { get; set; }

        public void Configure(GameBoot boot, UIDocument document, TextAsset strings, PortraitCatalog portraits, MapScene map,
            PlacementLayer placements, MapPointer pointer, CameraController cameraController)
        {
            this.boot = boot; this.document = document; this.strings = strings; this.portraits = portraits; this.map = map;
            this.placements = placements; this.pointer = pointer; this.cameraController = cameraController;
        }

        void OnEnable() { if (boot != null) boot.Ready += Begin; }

        void OnDisable()
        {
            if (boot != null) boot.Ready -= Begin;
            Shutdown();
        }

        /// <summary>Smoke-test turn count: from the page URL on WebGL ("?smoke=N", or "?smoke" for DefaultSmokeTurns; the flag
        /// must start a query parameter and end at a word boundary, UrlFlags), else from the command line ("-smoke N").
        /// 0 when not asked.</summary>
        public static int SmokeTurns(string? url, string[] args, bool webgl)
        {
            if (webgl)
            {
                if (!UrlFlags.Has(url, "smoke")) return 0;
                var n = UrlFlags.Int(url, "smoke");
                return n > 0 ? n : DefaultSmokeTurns;
            }
            var i = Array.IndexOf(args, "-smoke");
            return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var t) ? t : 0;
        }

        public static int SmokeTurns() =>
            SmokeTurns(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>GameBoot.Ready: starts the game, smoke-testing when the URL or command line asks.</summary>
        public void Begin(GameInputs inputs) => Begin(inputs, SmokeTurns());

        /// <summary>Starts (or restarts) the game over the built map. <paramref name="smoke"/> &gt; 0 plays that many turns.</summary>
        public void Begin(GameInputs inputs, int smoke)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            foreach (var (value, field) in new (UnityEngine.Object?, string)[] { (document, nameof(document)), (strings, nameof(strings)),
                (map, nameof(map)), (placements, nameof(placements)), (pointer, nameof(pointer)), (cameraController, nameof(cameraController)) })
                if (value == null) throw new InvalidOperationException($"GameRoot: {field} is not assigned (run MapCapture.MakeScene)");
            if (map.Terrain == null) throw new InvalidOperationException("GameRoot: the map is not built (GameBoot builds it before Ready)");
            Shutdown();

            var text = UiText.Parse(strings.text);
            var store = Store ?? SaveStore.ForPlatform();
            var notes = new List<string>();
            // A smoke run is reproducible: a fresh game with the golden seed, never the player's autosave.
            var session = smoke > 0
                ? GameStart.Begin(inputs.Content, store, () => GoldenRun.Seed, notes, text, fresh: true)
                : GameStart.Begin(inputs.Content, store, GameStart.NewSeed, notes, text);
            var bridge = Bridge = new SimBridge(session, text);
            var root = document.rootVisualElement;

            // The toast first: the autosave (below) must report a failed write after the toast has shown the turn's refusals.
            var toast = Own(new ToastPresenter(root, bridge, text));
            Own(new HudPresenter(root, bridge, text));
            Own(new ScreenNav(root));                                      // hides every screen: the USS has no display:none for them
            var card = Own(new EventCardPresenter(root, bridge, text, portraits));
            Own(new DefencePresenter(root, bridge, text));
            Own(new BudgetPresenter(root, bridge, text));
            Own(new WhyPresenter(root, bridge, text));
            Own(new CabinetPresenter(root, bridge, text, portraits));
            var names = SettlementPlacer.Parse(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.Name);
            var info = Own(new InfoPanelPresenter(root, bridge, text, names));
            Own(new NewGamePresenter(root, bridge, store, toast, text, GameStart.NewSeed));
            if (smoke == 0) Own(GameStart.Autosave(bridge, store, text, toast.Show));

            Func<Vector2, bool> overUi = p => UiPointer.IsOverUi(root.panel, p, Screen.height);
            var terrain = map.Terrain;
            var size = terrain.terrainData.size;
            cameraController.IsOverUi = overUi;
            cameraController.Bounds = new Rect(terrain.transform.position.x, terrain.transform.position.z, size.x, size.z);
            pointer.Map = map;
            pointer.IsOverUi = overUi;
            Action<int, int> onHex = info.Show;
            pointer.HexClicked += onHex;
            var camRig = rig = cameraController.GetComponent<CameraRig>();
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes));
            mini.Clicked += (lon, lat) => { camRig.LookAt = map.Frame.ToWorld(lon, lat, 0); camRig.Apply(); SyncMinimap(); };
            Action onChanged = RenderPlacementsIfChanged;
            bridge.Changed += onChanged;
            owned.Add(new Undo(() =>
            {
                bridge.Changed -= onChanged;
                pointer.HexClicked -= onHex;
                if (pointer.IsOverUi == overUi) pointer.IsOverUi = null;
                if (cameraController.IsOverUi == overUi) cameraController.IsOverUi = null;
            }));
            drawn = null;
            RenderPlacementsIfChanged();
            SyncMinimap();

            if (notes.Count > 0) toast.Show(notes);
            Debug.Log(FormattableString.Invariant($"GAME OK {bridge.State.Date:yyyy-MM-dd} turn {bridge.State.Turn}"));
            if (smoke > 0)
            {
                if (Application.isPlaying) StartCoroutine(Smoke(root, bridge, card, smoke, Debug.Log));
                else Debug.LogWarning("SMOKE FAIL the smoke run needs play mode or a player");
            }
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        /// <summary>Disposes every presenter, the autosave and the camera/pointer hooks (newest first). Safe to call twice.</summary>
        void Shutdown()
        {
            StopAllCoroutines();
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            owned.Clear();
            minimap = null; rig = null; drawn = null;
        }

        /// <summary>Draws the placements only when State.Placements differs from what was drawn (every order and every turn
        /// raises Changed; rebuilding the markers each time would be wasted work).</summary>
        void RenderPlacementsIfChanged()
        {
            if (Bridge == null) return;
            var now = Bridge.State.Placements.Select(p => (p.Action, p.Q, p.R, p.Date)).ToList();
            if (drawn != null && drawn.SequenceEqual(now)) return;
            drawn = now;
            placements.Render(Bridge.State.Placements, Bridge.Content, map);
        }

        void SyncMinimap()
        {
            if (minimap == null || rig == null) return;
            minimap.SetView(map.Frame, rig.LookAt, rig.Distance);
        }

        void Update() => SyncMinimap();

        /// <summary>Plays turns the way a player does: answers each open event by pressing the card's first option button,
        /// then presses End Turn. Logs "SMOKE turn &lt;n&gt; &lt;date&gt; refused &lt;k&gt;" per turn through <paramref name="log"/>,
        /// then "SMOKE OK &lt;n&gt; turns &lt;date&gt; &lt;hash&gt;", or "SMOKE FAIL &lt;reason&gt;" when a press does not take effect.
        /// Static, so an edit-mode test can drive it step by step.</summary>
        public static IEnumerator Smoke(VisualElement root, SimBridge bridge, EventCardPresenter card, int turns, Action<string> log)
        {
            for (int t = 0; t < turns; t++)
            {
                yield return null;
                for (int guard = 0; card.Shown != null; guard++)
                {
                    var shown = card.Shown;
                    var option = root.Q<VisualElement>("event-options")?.Q<Button>();
                    if (option == null || guard >= 50) { log($"SMOKE FAIL event {shown.Id} has no option to press"); yield break; }
                    if (!Press(option, log)) yield break;
                    if (card.Shown == shown) { log($"SMOKE FAIL the option '{option.text}' for event {shown.Id} was not taken"); yield break; }
                    yield return null;
                }
                var turn = bridge.State.Turn;
                int refused = 0;
                Action<IReadOnlyList<Rejection>> count = r => refused = r.Count;
                bridge.TurnEnded += count;
                bool pressed;
                try { pressed = Press(root.Q<Button>("end-turn"), log); }
                finally { bridge.TurnEnded -= count; }
                if (!pressed) yield break;
                if (bridge.State.Turn != turn + 1) { log($"SMOKE FAIL End Turn did not end turn {turn}"); yield break; }
                var s = bridge.State;
                log(FormattableString.Invariant($"SMOKE turn {s.Turn} {s.Date:yyyy-MM-dd} refused {refused}"));
            }
            log(FormattableString.Invariant($"SMOKE OK {turns} turns {bridge.State.Date:yyyy-MM-dd} {bridge.State.Hash()}"));
        }

        static bool Press(Button? b, Action<string> log)
        {
            if (b == null) { log("SMOKE FAIL button missing"); return false; }
            try
            {
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
                return true;
            }
            catch (Exception e) { log("SMOKE FAIL " + e.Message); return false; }
        }

        sealed class Undo : IDisposable
        {
            readonly Action undo;
            public Undo(Action undo) { this.undo = undo; }
            public void Dispose() => undo();
        }
    }
}
