# M3 Unity tech notes (Unity 6000.3.25f1, URP 17.3.0, WebGL/IL2CPP)

Researched 2026-10-10. Unity was **not** launched. Every claim is tagged:

- **[SRC]**: read in installed source or binaries. Paths are given. Engine C# was decompiled with ilspycmd from
  `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/*.dll`.
  The XML doc files next to those DLLs are the installed API docs.
- **[DOC]**: official documentation, with URL.
- **[EXP]**: a small experiment run here with the toolchain bundled in the editor install, without Unity.
- **[M0]**: proven in `docs/spikes/m0-results.md`.
- **[UNVERIFIED]**: inference or community report. M3 must check these in its first tasks.

Abbreviations: `$E` = `/Applications/Unity/Hub/Editor/6000.3.25f1`. `$PC` = `unity/Library/PackageCache`.
`$WGL` = `$E/PlaybackEngines/WebGLSupport/BuildTools`.

## 0. Current project state relevant to M3

- `unity/ProjectSettings/ProjectSettings.asset` has these values:
  - `webGLCompressionFormat: 2`. That is Disabled (enum order: Brotli=0, Gzip=1, Disabled=2; M0 doc confirms "Disabled").
  - `webGLDecompressionFallback: 0`, `webGLDataCaching: 1`, `webGLExceptionSupport: 1`, `webGLThreadsSupport: 0`.
  - `webGLTemplate: APPLICATION:Default`, `webGLMaximumMemorySize: 2048`, `defaultScreenWidthWeb/HeightWeb: 960/600`.
  - `activeInputHandler: 1`, which is Input System package only.
  - `runInBackground: 0`. `il2cppCodeGeneration: {}` and `additionalIl2CppArgs:` (empty) mean defaults.
- Project-wide input actions are already set: `ProjectSettings/EditorBuildSettings.asset` →
  `com.unity.input.settings.actions` = `Assets/InputSystem_Actions.inputactions` (guid 052faaac…). Its maps are
  `Player` (Move, Look, Attack, …) and `UI` (Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick,
  ScrollWheel, …). [SRC]
- `Assets/Scripts/Statecraft.Game.asmdef` has `overrideReferences: true` and references only URP/Core.
  **It must add `"Unity.InputSystem"`** before game code can use
  `UnityEngine.InputSystem`. The edit-mode test asmdef has the same need. [SRC]
- `Paths.Content` already switches to `streamingAssetsPath/content` in players. `MapScene.ReadInputs()` uses System.IO
  and must be replaced for WebGL; its doc comment says so.
- `Assets/StreamingAssets/map/heightmap.r16` is **8.0 MB** (8,396,802 bytes). StreamingAssets files are served as-is
  and are *not* covered by Unity's build compression. See §6.
- Package versions installed: inputsystem 1.20.0, test-framework 1.6.0, ugui 2.0.0. UI Toolkit is built into the
  engine module `UnityEngine.UIElementsModule`. Emscripten is **3.1.39** (`$WGL/Emscripten/emscripten/emscripten-version.txt`)
  and its clang is **17.0.0** (`$WGL/Emscripten/llvm/clang --version`). [SRC]

---

## 1. UI Toolkit at runtime

### 1.1 Runtime data binding (works on WebGL IL2CPP [M0 d])
- `[CreateProperty]` is mandatory for a bindable member: "The CreateProperty attribute is mandatory to make a
  property bindable in a data source." [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-runtime-binding-define-data-source.html
- With `INotifyBindablePropertyChanged`, implement `event EventHandler<BindablePropertyChangedEventArgs> propertyChanged`
  and raise `new BindablePropertyChangedEventArgs(nameof(Prop))` in setters. The binding system "doesn't perform checks
  unless it gets notified of a change". [DOC, same page]. The M0 spike `spikes/m0/unity/Assets/Spike/CabinetVM.cs`
  used exactly this. [M0]
- `IDataSourceViewHashProvider.GetViewHashCode()` is optional. It returns a version number, and bindings skip updates
  while the number is unchanged. It is useful for "publish once per turn": bump the version after each `Simulation.Step`. [DOC, same page]
- `BindingUpdateTrigger` installed values: `WhenDirty`, `OnSourceChanged` (default), `EveryUpdate`. [SRC: decompiled enum.]
  The manual text says "`BindingUpdateTrigger.EveryFrame`", but the **installed enum member is `EveryUpdate`**, so use that.
- Binding in code (M0-proven): `label.SetBinding("text", new DataBinding { dataSourcePath = new PropertyPath("Name"), bindingMode = BindingMode.ToTarget });`
  with `element.dataSource = vm`. A data source is inherited by children (`VisualElement.dataSource` XML doc:
  "This data source is inherited by all children"). [SRC xml, M0]
- Property bags: reflection is the default and is created lazily, once per type. Optional codegen comes from
  `[GeneratePropertyBag]` on the type plus `[assembly: GeneratePropertyBagsForAssembly]`, which avoids reflection
  and its GC cost "in IL2CPP builds". [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/property-bags.html.
  The M0 spike worked on WebGL with reflection and no codegen. [M0]. Stripping: `unity/Assets/link.xml` already preserves
  `Statecraft.Sim` and `Statecraft.Game.Map.*`. **Add the UI view-model namespace to link.xml**, or use codegen, so
  IL2CPP managed stripping cannot remove reflected getters. [UNVERIFIED that it is needed; cheap insurance.]
- Architecture implication: `Simulation.Step` returns a *new* `GameState` (it clones). The UI needs a view-model layer
  (`[CreateProperty]` + notify) that is refreshed from each new state. Do not bind directly to `GameState`.

### 1.2 ListView with bound items
- `BaseListView.bindingSourceSelectionMode`: `BindingSourceSelectionMode.AutoAssign` means "the items source and indexed
  path are automatically assigned to each item's data source". `Manual` means user code does it in `bindItem`. [SRC xml]
- With AutoAssign, `makeItem` alone is valid; `bindItem` is not needed. Source:
  `HasValidDataAndBindings() => … (autoAssignSource && makeItem != null) || makeItem != null == (bindItem != null)`
  in decompiled `ListView`. [SRC]
- With AutoAssign, ListView schedules a poll that calls `RefreshItems()` when `itemsSource.Count` changes. This is
  decompiled `BaseListView.trackItemCount`/`trackCount`. A count change is picked up automatically. A **same-count
  content change is not**, so call `listView.RefreshItems()` ("Rebinds all items currently visible"), or rely on the item
  view-models' own notifications. `Rebuild()` recreates all visible elements. [SRC]
- UXML pattern from the manual: `<ui:ListView binding-source-selection-mode="AutoAssign" item-template="Row.uxml">`
  plus `<Bindings><ui:DataBinding property="itemsSource" data-source-path="switches" /></Bindings>`. **Remove
  `engine=`/`editor=` namespace attributes from the root tag**, because they break `<Bindings>`. The C# alternative is
  `listView.dataSource = asset`. [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-runtime-binding-list-view.html
- Code-only alternative (simplest for M3): `listView.itemsSource = vmList; listView.makeItem = () => rowTemplate.Instantiate();
  listView.bindingSourceSelectionMode = BindingSourceSelectionMode.AutoAssign;`. Each row's bindings then resolve
  against its list item. `ListView.itemTemplate` (VisualTreeAsset) also exists. [SRC]
- `fixedItemHeight` / `virtualizationMethod` (`CollectionVirtualizationMethod`) exist. Use fixed height for ~13-40 rows. [SRC xml]

### 1.3 PanelSettings scale mode for 1920×1080 and browser resizing
- Installed defaults, from decompiled `PanelSettings`: `m_ScaleMode = ConstantPhysicalSize`, `m_ReferenceResolution = (1200, 800)`,
  `m_ScreenMatchMode = MatchWidthOrHeight`, `m_Match = 0`, `m_ReferenceDpi = 96`, `m_FallbackDpi = 96`. [SRC]
  ConstantPhysicalSize computes `scale = referenceDpi / screenDpi`. That explains M0's "2.7× larger on Retina Mac". [SRC: `ResolveScale`]
- `PanelScaleMode` enum: `ConstantPixelSize`=0, `ConstantPhysicalSize`=1, `ScaleWithScreenSize`=2.
  `PanelScreenMatchMode`: `MatchWidthOrHeight`, `Shrink`, `Expand`. [SRC]
- **Recommendation:** `scaleMode = ScaleWithScreenSize`, `referenceResolution = new Vector2Int(1920, 1080)`,
  `screenMatchMode = MatchWidthOrHeight`, `match = 0.5f`. `Expand` is the alternative, if layouts must never be cropped.
  ScaleWithScreenSize uses `targetRect.width / ref.x` and `targetRect.height / ref.y` (decompiled `ResolveScale`).
  It is recomputed whenever the target rect changes (`m_TargetRect.width != targetRect.width …` in `ApplyPanelSettings`).
  A browser resize therefore re-scales automatically, *provided the canvas itself resizes*. [SRC]
- Canvas resizing is a **template issue**. The `Default` and `Minimal` templates set a **fixed** CSS size of
  `{{{WIDTH}}}×{{{HEIGHT}}}` (960×600 here) on desktop: `$WGL/WebGLTemplates/Base/Default/index.html`, `.../Minimal/index.html`.
  The `PWA` template's `TemplateData/style.css` has `#unity-container{position:fixed;width:100%;height:100%}`
  and `#unity-canvas{width:100%;height:100%}`. [SRC] Unity keeps the WebGL render size matched to the canvas DOM
  size × `devicePixelRatio` unless `config.matchWebGLToCanvasSize = false` (template comment). [SRC]
- **Recommendation:** add a custom template at `Assets/WebGLTemplates/Statecraft/` (copy `Minimal`; make the canvas
  100%×100%; set `config.autoSyncPersistentDataPath = true` (§3)). Custom templates go in `Assets/WebGLTemplates`
  and appear in Player settings by folder name. [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/web-templates-add.html.
  From script: `PlayerSettings.WebGL.template = "PROJECT:Statecraft"`. The API is
  [SRC xml `P:UnityEditor.PlayerSettings.WebGL.template`]. The `PROJECT:` prefix is [UNVERIFIED]; it is inferred from the
  serialized `APPLICATION:Default`. Avoid PWA: its service worker caches the build and complicates updates.
- **Create PanelSettings as an asset in an editor script** (as M0 did). Creating the asset auto-assigns the default
  runtime theme: M0's `Spike/SpikePanel.asset` got `themeUss` → `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss`
  (`@import url("unity-theme://default");`). [M0 evidence] A `PanelSettings` made with `CreateInstance` at runtime
  has no theme, so built-in controls (Button, ListView, ScrollView) are unstyled. [UNVERIFIED for the runtime case;
  `m_DisableNoThemeWarning` exists in source.]

### 1.4 Loading UXML/USS in a player
- Reference `VisualTreeAsset`/`StyleSheet`/`PanelSettings` through `[SerializeField]` fields, or `UIDocument.visualTreeAsset`,
  on a scene object. Those assets and their `<Style src>` dependencies are then included in the build. This is the same lesson as
  M0 correction 1 (`Shader.Find` returned null because nothing referenced the shader). `AssetDatabase` is editor-only.
  `Resources.Load` works only for files under a `Resources/` folder; avoid it. [M0 + general Unity rule]
- `VisualTreeAsset.Instantiate()` returns a `TemplateContainer`. `CloneTree(VisualElement target)` clones into an existing
  root. `CloneTree(string bindingPath)`/`Instantiate(string)` also exist. [SRC xml]
- PanelSettings holds serialized shader refs (`m_DefaultShader`, `m_SDFShader`, `m_BitmapShader`, `m_SpriteShader`,
  `m_AtlasBlitShader`, …) [SRC]. A PanelSettings **asset** keeps the UI shaders referenced in builds. This is another
  reason to use an asset. [inference; M0 runtime PanelSettings asset rendered fine on WebGL]

### 1.5 Fonts (TextCore)
- Fonts are set with USS `-unity-font-definition: url("project://database/Assets/UI/Fonts/X SDF.asset")` (or
  `resource(...)`). It takes precedence over legacy `-unity-font`. In code:
  `style.unityFontDefinition = new StyleFontDefinition(fontAsset)` / `FontDefinition.FromSDFFont(FontAsset)`. [SRC xml; DOC]
- Static vs dynamic font assets: Static pre-bakes the characters, and the build does **not** need the source font.
  Dynamic fills the atlas at runtime, and the build "requires to include all source font files". [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-font-asset.html
  **Recommendation:** Static SDF font asset (Latin + punctuation + digits + €/£/₪ if needed) for deterministic WebGL size.
  Hebrew/RTL is deferred.
- `PanelSettings.textSettings` takes a `PanelTextSettings` (a TextCore `TextSettings`) for fallback fonts and sprites. If it is null,
  a default instance is created (decompiled `PanelTextSettings.defaultPanelTextSettings`). [SRC]
- The default font the runtime theme uses is not documented on the font page. [UNVERIFIED] Set a project font explicitly
  in the root USS.

### 1.6 Testing UI Toolkit in edit-mode tests
Facts from decompiled source:
- `VisualElement.SendEvent(e)` is `elementPanel?.SendEvent(e)`. **Without a panel, events are silently dropped.** A tree
  from `vta.Instantiate()` that is not attached to a panel can be queried (`root.Q<Button>("end-turn")`), but it cannot
  receive events. [SRC `VisualElement.SendEvent`]
- When nothing is being dispatched, events are processed **immediately**, synchronously:
  `dispatchImmediately => m_Immediate || m_GateCount == 0` in `EventDispatcher.Dispatch`. [SRC]
- **`Button.clicked` does not listen to `ClickEvent`.** The Button's `Clickable` manipulator registers
  `PointerDown/Move/Up/Cancel/CaptureOut` only. A synthetic `ClickEvent` reaches `RegisterCallback<ClickEvent>`
  handlers, but it **does not fire `Button.clicked`**. [SRC `Clickable.RegisterCallbacksOnTarget`]
- `Button` registers `NavigationSubmitEvent` → `clickable?.SimulateSingleClick(evt)`, which invokes `clicked` and
  `clickedWithEventInfo`, then `StopPropagation()`. **`NavigationSubmitEvent` is the reliable way to "press" a Button
  in tests.** [SRC `Button..ctor` / `OnNavigationSubmit`]
- Pooled events: `NavigationSubmitEvent.GetPooled(EventModifiers modifiers = None)` (from `NavigationEventBase<T>`).
  `ClickEvent.GetPooled()` comes from `EventBase<T>`. `EventBase.target` has a public setter. [SRC]

Recommended test pattern. It is [UNVERIFIED] as a whole until run, but every API is confirmed:
```csharp
// Edit-mode: give the tree a real runtime panel via UIDocument ([ExecuteAlways]; OnEnable creates the UI in edit mode
// when not entering play mode — decompiled UIDocument._Enable).
var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/StatecraftPanel.asset");
var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Hud.uxml");
var go = new GameObject("UITest"); go.SetActive(false);
var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = vta;
go.SetActive(true);                                   // OnEnable -> RecreateUI
var button = doc.rootVisualElement.Q<Button>("end-turn");
Assert.NotNull(button.panel);
using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }   // fires button.clicked
// For a plain RegisterCallback<ClickEvent> handler: using (var c = ClickEvent.GetPooled()) { c.target = el; el.SendEvent(c); }
Object.DestroyImmediate(go);
```
- Bindings and layout are applied in the panel update. In edit mode that update comes from the editor loop, so use
  `[UnityTest] IEnumerator` plus `yield return null` (allowed in edit-mode UnityTests) before asserting bound text.
  [UNVERIFIED that one frame is enough in `-batchmode`; if not, loop up to N frames until the value appears.]
- Wiring logic tests can skip the panel: query the instantiated tree and call the presenter's handler directly. Keep
  one panel-backed smoke test for `NavigationSubmitEvent`.
- `UIDocument` is `[ExecuteAlways]`, `[DefaultExecutionOrder(-100)]`, `[DisallowMultipleComponent]`. `_Enable` runs
  `RecreateUI()` when `!IsEditorPlayingOrWillChangePlaymode() || IsEditorPlaying()`. [SRC]

---

## 2. WebGL file access (StreamingAssets)

- **Confirmed:** "On Android and the Web platform, it's impossible to access the streaming asset files directly via file
  system APIs … Use the UnityWebRequest class." StreamingAssets is read-only at runtime. [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/StreamingAssets.html
- API, all members present in installed XML (`UnityEngine.UnityWebRequestModule.xml`):
  `UnityWebRequest.Get(string)`, `req.SendWebRequest()`, `req.result` (`UnityWebRequest.Result.Success / ConnectionError /
  ProtocolError / DataProcessingError / InProgress`), `req.downloadHandler.text` (string), `req.downloadHandler.data` (byte[]),
  `nativeData`. [SRC]
- **`await` on a request works:** `AsyncOperationAwaitableExtensions.GetAwaiter(this AsyncOperation op) => Awaitable.FromAsyncOperation(op).GetAwaiter()`
  (decompiled `UnityEngine.CoreModule`), and `UnityWebRequestAsyncOperation : AsyncOperation`. So
  `await req.SendWebRequest();` compiles and runs in Unity 6. [SRC]
- **Awaitable on WebGL:** the Web technical overview says managed threads aren't supported, "anything in the C#
  `System.Threading` namespace isn't supported" (timers never fire, cancellation timeouts never trigger), and the
  recommended async approach is **`Awaitable`** (coroutines also work). [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html
  - Avoid `Awaitable.BackgroundThreadAsync()` ("Resumes execution on a ThreadPool background thread"), `Task.Run`,
    `Task.Delay`, and `CancellationTokenSource(timeout)` on WebGL. [DOC + community reports, UNVERIFIED in detail]
  - An Awaitable instance must never be awaited twice ("undefined behavior such as an exception or a deadlock"). [DOC]
    https://docs.unity3d.com/6000.3/Documentation/Manual/async-awaitable-introduction.html
  - Continuations run synchronously on the main thread when the operation completes. [DOC, same page]
- **No directory listing:** `streamingAssetsPath` on Web is a URL, and a static HTTP server gives no listing.
  `ContentLoader.Load(string root)` uses `Directory.GetFiles`, which cannot work there. Use the existing
  `ContentLoader.Load(Func<string,string?> readFile, IEnumerable<string> eventFiles)`
  (`sim/Statecraft.Sim/ContentLoader.cs:90`). Its doc comment already names WebGL. The callback is **synchronous**, so
  prefetch first:
  1. A build step (`IPreprocessBuildWithReport`, or the `-executeMethod` build script) copies `content/**` into
     `Assets/StreamingAssets/content/` and writes `content/manifest.json` (a list of relative paths, e.g.
     `["offices.yaml",…,"events/armistice_egypt.yaml",…,"map/hexes.json"]`). There are 19 content files today (424 KB).
  2. A boot scene fetches the manifest, then **starts all requests before awaiting any of them** (parallel downloads; Awaitable has
     no WhenAll, so start N `SendWebRequest()` ops, then await each), and fills a `Dictionary<string,string>` (text) / `byte[]` (r16).
  3. Call `ContentLoader.Load(rel => dict.TryGetValue(rel, out var t) ? t : null, manifest.Where(p => p.StartsWith("events/")))`.
  4. Then build the map from the already-fetched strings/bytes. `HeightmapLoader.Load(string, byte[], …)` already
     takes in-memory inputs.
  - In the editor, keep System.IO reads of the repo (fast tests). `UnityWebRequest` on desktop/editor needs a `file://`
    URI, not a bare path. [UNVERIFIED for 6.3; use `new System.Uri(path).AbsoluteUri`.]
- A local test needs an HTTP server; `file://` does not work. `python3 -m http.server` (Python 3.9.6 here) maps
  `.wasm → application/wasm` and `.js → text/javascript`. [EXP: `mimetypes.guess_type`] That is enough with
  compression Disabled. Unity also ships `$WGL/SimpleWebServer.exe` (used by Build And Run).

---

## 3. Saves on WebGL

- `Application.persistentDataPath` on Web "points to a location in the browser's IndexedDB file system. The path is formed
  as `/idbfs/<hash>`". `<hash>` is an MD5 of the URL of the *directory* containing the page; the file name and query don't count. [DOC]
  https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html
- Mount and sync, from installed source:
  - `$WGL/prejs/IdbFs.js`: at preRun, `FS.mkdir('/idbfs')`, then
    `FS.mount(IDBFS, { autoPersist: !!Module['autoSyncPersistentDataPath'] }, '/idbfs')`, then `FS.syncfs(true, …)`
    to populate memory from IndexedDB before start. With `autoPersist`, a write to a file sets `isModified`, and on
    **close** it calls `IDBFS.queuePersist` (batched with `setTimeout(…,0)`). mkdir/unlink/rename/rmdir also persist. [SRC]
  - `$WGL/lib/FileSystem.js` `JS_FileSystem_Sync` (the old manual path) prints: *"Manual synchronization of Unity
    Application.persistentDataPath via JS_FileSystem_Sync() is deprecated … Pass config.autoSyncPersistentDataPath = true;
    to configuration in createUnityInstance() to opt in to the new behavior."* [SRC]
  - The Default template comment says autosync "is currently not the default behavior". **Unity 6.3 does not autosync
    unless the template sets `config.autoSyncPersistentDataPath = true;`.** [SRC `$WGL/WebGLTemplates/Base/Default/index.html`]
  - **Answer:** no manual `FS.syncfs` jslib is needed in 6.3 *if the custom template sets `autoSyncPersistentDataPath = true`*.
    Write with `File.WriteAllText` (System.IO works against the in-memory FS). The flush happens when the file is closed.
- **PlayerPrefs on Web:** "Unity stores up to 1MB of PlayerPrefs data using the browser's IndexedDB API." [DOC]
  https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html. Do not use it for a game save.
- **Recommended save (a few hundred KB of JSON):** `File.WriteAllText(Path.Combine(dir, "slot1.json.tmp"), json)`, then
  `File.Move`/replace to `slot1.json` (rename also triggers persist), with autosync on. IndexedDB quotas are far above
  this size. [inference]
- **Hosting pitfall (important for itch.io):** the persistent path hashes the page's directory URL. itch.io serves every
  upload from a new subfolder, so **a new upload changes the hash and old saves "disappear"**. This is reported by several
  itch devs. Workarounds: a fixed folder under the `/idbfs` mount (e.g. `/idbfs/statecraft-saves/`). It is mounted with
  the same autoPersist option per IdbFs.js, so it should persist and stay URL-independent within one origin. Or use a
  localStorage jslib. [UNVERIFIED; community: https://itch.io/t/1841689/unity3d-playerprefs-problem-on-updating-build,
  https://stebeegames.itch.io/feed-the-bunny/devlog/329860/unity3d-playerprefs-problem-on-itchio-and-my-solution].
  IndexedDB is per-origin, so namespace the folder. **M3 test:** upload, save, re-upload, then load. Also offer save
  export/import later as a backstop.

---

## 4. IL2CPP/WebGL determinism

### 4.1 WebAssembly floating point (spec)
- "Floating-point arithmetic follows the IEEE 754 standard"; "All operators use round-to-nearest ties-to-even"; "Non-default
  directed rounding attributes are not supported"; exceptions are non-stop and not observable. Non-determinism is limited
  to **NaN** results (payload and sign). [DOC] https://webassembly.github.io/spec/core/exec/numerics.html#floating-point-operations
- **There is no scalar FMA instruction.** Scalar float ops are only `abs neg sqrt ceil floor trunc nearest` (unary)
  and `add sub mul div min max copysign` (binary). Each is a separately rounded IEEE op. [DOC] https://webassembly.github.io/spec/core/syntax/instructions.html
- The only fused op is the *relaxed SIMD* `f32x4/f64x2.relaxed_madd/nmadd`, which "allows for fused or unfused results"
  (implementation-dependent). The deterministic profile uses unfused. [DOC, numerics page, "Relaxed operations"]
- Engines (V8/SpiderMonkey/JSC) must implement these semantics, so a conforming browser cannot fuse `f64.mul`+`f64.add`.
  WebGL double math is therefore bit-reproducible across browsers, except NaN bits. The sim hash already normalizes -0.0;
  NaN should never appear. [spec-derived]

### 4.2 What Unity's toolchain emits
- [EXP] Compiled `double f(a,b,c){return a*b+c;}` and a loop with the **bundled** clang 17 (`$WGL/Emscripten/llvm/clang
  --target=wasm32-unknown-emscripten`) under `-O3`, `-O3 -ffp-contract=fast`, `+ -msimd128`, `-ffast-math -msimd128`,
  `-ffp-contract=fast -mrelaxed-simd`, and `-ffast-math -mrelaxed-simd`. **Every variant emitted only `f64.mul` + `f64.add`
  (or `f64x2.mul` + `f64x2.add`), never a fused/relaxed madd.** So FMA contraction cannot occur in the WebGL build.
- [SRC] IL2CPP's GCC-like compiler settings (`$E/Unity.app/Contents/Resources/Scripting/il2cpp/build/deploy/Bee.Toolchain.GNU.dll`,
  decompiled `GccLikeCompilerSettings`) map `FloatingPointOptimization {Default, Fast, Strict}` to
  (none) / `-ffast-math` / `-fno-unsafe-math-optimizations`, and `FloatingPointContract {CompilerDefault, On, Off, Fast}`
  to (none) / `-ffp-contract=on|off|fast`. `Strict` forces contract `Off`. No caller in the il2cpp deploy dir or the
  WebGL module sets them (grep of all DLLs), so WebGL uses **Default/CompilerDefault**: no fast-math and clang's default
  contract mode. The emscripten settings add `-msimd128 -mbulk-memory -mnontrapping-fptoint -fwasm-exceptions` only
  when "Wasm2023" is enabled (`EmscriptenCompilerSettings`). This is SIMD128, **not** relaxed-simd. [SRC]
- The extra-flags hook, if ever needed: `il2cpp --compiler-flags=<value,…>` ("Additional flags to pass to the C++ compiler")
  [SRC: `il2cpp --help`], set from script with `PlayerSettings.SetAdditionalIl2CppArgs("--compiler-flags=-ffp-contract=off")`
  [SRC xml]. Not needed for WebGL, per the experiment above.
- `Il2CppCodeGeneration` (`OptimizeSpeed` / `OptimizeSize`) via `PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, …)`,
  and `Il2CppCompilerConfiguration` (Debug/Release/Master). There is no documented FP effect. Fast-math is never enabled, so
  neither changes IEEE semantics. [SRC xml + above; UNVERIFIED by a build]
- **Native Mac IL2CPP caveat:** Apple clang 21 at `-O3` with its default contract mode emitted `fmadd`/`fmla` for `a*b+c`
  [EXP; `-ffp-contract=off` removes it]. Whether IL2CPP's generated C++ (temporaries plus `il2cpp_codegen_*` helpers) lets
  clang see a contractible expression is [UNVERIFIED]. If a Mac player golden mismatches, add `--compiler-flags=-ffp-contract=off`.
  WebGL is unaffected.
- Other determinism risks to cover with the in-player golden test:
  - The sim uses only `Math.Abs/Min/Max` (no transcendental libm calls). [SRC grep of `sim/Statecraft.Sim`]
  - YAML double parsing under Unity's class libraries vs .NET Core is [UNVERIFIED].
  - Dictionary/HashSet enumeration order: the hash sorts keys ordinally (`GameState.Hash`), but sim logic iterating
    unsorted dictionaries could differ only if insertion/removal patterns differ (same code, so unlikely).
  - `(int)double` of NaN/out-of-range values differs between platforms. `webGLWasmArithmeticExceptions` (enum
    `Throw`/`Ignore`) controls trapping. [SRC xml]

### 4.3 Running the golden playthrough in a player and reading the result
- Recommended: a dedicated `Golden.unity` scene with a MonoBehaviour that runs these steps:
  1. Fetch the content via the §2 loader (Awaitable).
  2. Run the same loop as `DeterminismTests.GoldenPlaythrough` (seed 1948, Historical, `HistoricalAI` until 1950-01-01).
  3. Log `GOLDEN OK <hash>` or `GOLDEN FAIL <hash> expected <GoldenHash>`.

  Content loading and the playthrough are reused, so a separate build is not needed if the main boot reads a URL flag. On
  WebGL, `Debug.Log` reaches the browser console, as M0 showed (`SPIKE …` lines in `web-console-spike-lines.txt`). [M0]
- `GoldenHash` lives in `sim/Statecraft.Sim.Tests/DeterminismTests.cs` (test assembly). Move the constant, or duplicate
  it with a test asserting equality, so the Unity scene can reference it.
- Headless browser: Playwright browsers are already cached (`~/Library/Caches/ms-playwright/chromium-1228`,
  `chromium_headless_shell-1228`), and node v24 is available. Use `page.on('console', m => …)`, wait for `/GOLDEN (OK|FAIL)/`,
  and exit non-zero on FAIL or timeout. Serve the build with `python3 -m http.server`.
  - WebGL in headless Chrome: since Chrome 139, WebGL no longer falls back to SwiftShader silently. Pass
    `--enable-unsafe-swiftshader` (or `--use-gl=angle --use-angle=swiftshader-webgl --enable-unsafe-swiftshader`) when no
    GPU is available. [DOC] https://chromium.googlesource.com/chromium/src/+/798e59c5275d4131797e8ea2e96e6c6000256b58/docs/gpu/swiftshader.md,
    https://groups.google.com/a/chromium.org/g/blink-dev/c/yhFguWS_3pM/m/oLrB5up_BwAJ. Whether headless on this Mac gets
    the Metal GPU is [UNVERIFIED]. Run headed (`headless:false`) as a fallback. M0 saw rAF throttling (~1 fps) in background
    tabs, so keep the page foreground.
- **Headless WebGL build:**
  `"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Editor.Build.Web -logFile build-web.log`.
  The method calls `BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=…, locationPathName="Builds/Web", target=BuildTarget.WebGL, options=BuildOptions.None })`
  and checks `report.summary.result`. On failure it calls `EditorApplication.Exit(1)` (or throws, which makes `-quit` exit
  non-zero). This is the M0 pattern, proven (`docs/superpowers/plans/2026-10-09-statecraft-m0-m2.md:394`; M0 used
  `WebGLCompressionFormat.Disabled`). [M0] `-buildTarget WebGL` makes the editor start on WebGL and avoids a platform
  switch inside the method. [UNVERIFIED timing benefit]
- **`-runTests -testPlatform WebGL`:** mechanically supported by test-framework 1.6.0. `SettingsBuilder.GetBuildTarget`
  parses any `BuildTarget` name (`$PC/com.unity.test-framework@bd7f943e9647/UnityEditor.TestRunner/CommandLineTest/SettingsBuilder.cs:160`).
  `PlayerLauncher` sets `PlayerSettings.WebGL.closeOnQuit = true` for WebGL and builds and runs the player
  (`…/TestLaunchers/PlayerLauncher.cs:168`). The browser is chosen by `webGLClientBrowserType` (`Chrome`, `Chromium`, `Default`,
  `Edge`, `Firefox`, `Safari`) / `webGLClientBrowserPath` in a `-testSettingsFile` (`TestSettings.cs:218`). Results come
  back over the player connection to the still-running editor, and it **launches a real browser window**. Running it
  headless is [UNVERIFIED] (possibly via a wrapper script as `webGLClientBrowserPath`). **Recommendation:** use the custom
  `GOLDEN` scene + Playwright as the CI-style gate. Treat `-testPlatform WebGL` as optional and timebox it.

---

## 5. Input (Input System 1.20.0 + UI Toolkit)

- **No EventSystem and no InputSystemUIInputModule is needed for UI Toolkit.** `UIToolkitInputBackendOption.Default` =
  `InputSystemCompatibleBackend`: "allows the Input System package to send its input to UI Toolkit directly, removing the
  need for an EventSystem in the user scene". `SetRuntimeInputBackend` "has no effect if there is an EventSystem in the
  user scene" (that EventSystem then drives UI Toolkit). [SRC xml `UIToolkitInputConfiguration`]
  If a uGUI EventSystem is added, UI Toolkit adds `PanelRaycaster`/`PanelEventHandler`, and "the presence of these components
  deactivates the UI Toolkit's automated, built-in input processing". [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Event-System.html
  `InputSystemUIInputModule` is the uGUI path; do not add it.
- The integration lives in `$PC/com.unity.inputsystem@7a4e1a2a8194/InputSystem/Runtime/Plugins/InputForUI/InputSystemProvider.cs`.
  It uses `InputSystem.actions` (project-wide) if it has a `UI` map, else a built-in `DefaultInputActions`. It binds
  `UI/Point`, `UI/Navigate`, `UI/Submit`, `UI/Cancel`, `UI/Click`, `UI/MiddleClick`, `UI/RightClick`, `UI/ScrollWheel`,
  and enables only the `UI` map. [SRC] The project asset has all of these. [SRC] **Keep those action names.**
- Camera controls: add a `Camera` action map to `Assets/InputSystem_Actions.inputactions` (project-wide). Suggested actions:
  - `Pan`: Value/Vector2, a 2DVector composite (WASD + arrows).
  - `DragPan`: Value/Vector2, the `OneModifier` composite with modifier `<Mouse>/rightButton` (or middle) and binding `<Mouse>/delta`.
  - `Zoom`: Value/Axis `<Mouse>/scroll/y`.
  - `Orbit`: a 1D Axis composite on Q/E, plus optionally a OneModifier on `<Mouse>/delta/x`.

  Composites present: `AxisComposite`, `Vector2Composite`, `OneModifierComposite`, `TwoModifiersComposite`,
  `ButtonWithOneModifier`, … (`…/Runtime/Actions/Composites/`). [SRC] Read them with `InputSystem.actions.FindAction("Camera/Pan")`
  and enable the map (`FindActionMap("Camera").Enable()`). `CameraRig` has a fixed 50° pitch, so "orbit" = yaw around `LookAt`.
- **Block camera input over UI:** `panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPos))` returns the top element or
  null [SRC xml]. **`ScreenToPanel` does not flip Y.** It is `screenToPanelSpace(screen) / scale`, identity by default
  (decompiled `BaseRuntimePanel`), and panel space is y-down. Input System `Mouse.current.position` is y-up, so pass
  `new Vector2(p.x, Screen.height - p.y)`. [SRC] Give the root/background elements `picking-mode: ignore` so empty
  areas don't block the map.
- Scroll deltas differ between browsers and OSes (line vs pixel). Normalize/clamp zoom steps. [UNVERIFIED magnitude; tune in-browser]

---

## 6. WebGL build size, compression, hosting

- Unity 6.3 compression options [DOC] https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/building-distribution/deploying.md:
  - gzip ("default option", natively supported over HTTP and HTTPS).
  - Brotli (smallest; "Chrome and Firefox only natively support Brotli compression over HTTPS").
  - Disabled ("if you plan to use static compression on the hosting server").
  - Compression applies to **release builds only**.
  - Serving pre-compressed files needs `Content-Encoding: gzip|br`, and `.wasm` should be served as `application/wasm`
    (needed for streaming compilation).
  - **Decompression Fallback** embeds a JS decompressor and renames files `.unityweb`. It is for hosts where headers
    can't be configured, at the cost of a larger loader and no wasm streaming.
- Loader behaviour [SRC `$WGL/UnityLoader/UnityLoader.js`]: when compressed content arrives without the right
  `Content-Encoding`, it shows blocking error banners (e.g. lines 696 and 873). It also notes a Safari bug with
  `gzip` + `application/octet-stream`. Brotli fails over plain HTTP in Firefox.
- Host constraints:
  - **itch.io** [DOC] https://itch.io/docs/creators/html5:
    - Limits: ≤500 MB extracted, ≤**1,000 files**, ≤**200 MB per file**, path ≤240 chars, case-sensitive.
    - Pre-compressed gzip is auto-detected and served with `content-encoding: gzip`.
    - **`.br` files get `content-encoding: br`** (detected by extension; noted as "commonly used by Unity").
    - The CDN auto-gzips uncompressed `html, js, css, svg, wasm, wav, glb, pck` and some JSON/text MIME types.
    - itch recommends a "Better Minimal WebGL Template". With Disabled, `.wasm/.js` still get gzipped by the CDN.
      `.data` and `.r16` probably do not. [UNVERIFIED]
  - **GitHub Pages** [DOC] https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits:
    - Site ≤1 GB, soft 100 GB/month bandwidth, deploys time out after 10 min.
    - Git blocks files >100 MiB (https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).
    - **No custom headers**, so pre-compressed builds need Decompression Fallback, or use Disabled.
  - **Netlify** [DOC] https://docs.netlify.com/manage/routing/headers/:
    - `_headers` / `netlify.toml` set custom headers, but **`Content-Encoding` is on the list of headers Netlify ignores**,
      so pre-compressed Unity files need Decompression Fallback (or Disabled).
    - No official per-file size limit is documented; forum staff say files above ~5-10 MB are poorly handled by their CDN. [UNVERIFIED]
- **Recommendation for M3 ("browser link"):**
  - Host on **itch.io**, an HTML project, possibly restricted or draft. Simplest and free, it handles `.br`, and the
    1,000-file limit matters if many StreamingAssets files are added (today: ~20 content files + 2 map files + build files).
  - Use **Brotli, no fallback** on itch (smallest download, proper headers).
  - For GitHub Pages/Netlify/other generic static hosts, use **Gzip + Decompression Fallback**, or keep Disabled.
  - Keep **Disabled** for local/CI golden runs (python http.server; no header setup).
  - Make the build script take the compression as a parameter.
- Size levers:
  - The 8 MB `heightmap.r16` in StreamingAssets is not compressed by Unity, and itch may not gzip `.r16`. Ship it as
    `.bytes` TextAsset/`Resources`-free serialized reference in the `.data` file (covered by build compression), or quantize/
    downsample it. [inference]
  - `PlayerSettings.SetManagedStrippingLevel`, `SetIl2CppCodeGeneration(OptimizeSize)`, `stripEngineCode: 1` (already on).
  - `nameFilesAsHashes` helps cache-busting.
  - No WebGL build of `unity/` exists yet, so **measure the real size in M3's first build** (no numbers available now).

---

## 7. Portraits (~40 public-domain JPEGs) in UI Toolkit on WebGL

- Display: `Image` element (`image` = Texture2D, `scaleMode`), or `style.backgroundImage = new StyleBackground(texture2D)`
  / `Background.FromTexture2D(tex)`. Sprites and VectorImage are also accepted. [SRC xml]
- **Recommendation: Texture2D assets referenced from a `PortraitCatalog : ScriptableObject`** (person id → Texture2D),
  assigned to a serialized field on a scene object.
  - Synchronous; no async loader or failure path.
  - Included in the build `.data`, so covered by build compression and the `dataCaching` IndexedDB cache.
  - Edit-mode testable (`AssetDatabase.LoadAssetAtPath`). This matches the M0 lesson: reference assets, don't look them up by name.
  - Import settings:
    - Max Size 256 or 512, mipmaps off, sRGB on, Wrap Clamp.
    - Format: desktop-web DXT/BC ("Desktop browsers with DXT set as the texture compression format", ASTC for mobile;
      software decompression fallback when unsupported) [DOC] https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-texture-compression.html.
      **Use Crunch compression** to keep the download close to JPEG size. [UNVERIFIED size; measure.]
  - Rough memory: 512×640 DXT1 ≈ 160 KB GPU each, ≈ 6.4 MB for 40. [arithmetic]
- Runtime alternative, *not* recommended for M3: `UnityWebRequestTexture.GetTexture(url)` + `DownloadHandlerTexture.GetContent(req)`
  [SRC xml].
  - Gives a smaller first download (JPEG bytes).
  - But textures decode to uncompressed RGBA32 (≈1.3 MB each at 512×640, ≈52 MB for 40, unless made non-readable),
    and there are 40 extra HTTP files (itch 1,000-file cap) plus async/error handling.
- Keep source/attribution metadata (license, URL) in content YAML next to `people.yaml`. The catalog ScriptableObject can
  be generated by an editor script from a folder of `<person_id>.jpg`.

---

## Blockers / open items for the M3 planner
1. Edit-mode UI event tests need a panel. Use the `UIDocument` + `NavigationSubmitEvent` pattern (§1.6). Run it once early;
   batchmode panel-update timing is unverified.
2. The itch.io per-upload URL change breaks `persistentDataPath` saves (§3). Decide on the fixed `/idbfs/<name>` path, and
   test with two uploads.
3. Headless-Chrome WebGL on this Mac (GPU vs SwiftShader flags) is unverified (§4.3). Spike it on the first build.
4. `Statecraft.Game.asmdef` needs the `Unity.InputSystem` reference (§0, §5).
5. The 8 MB uncompressed heightmap in StreamingAssets (§6).
6. The golden constant must be reachable from Unity code (§4.3).
