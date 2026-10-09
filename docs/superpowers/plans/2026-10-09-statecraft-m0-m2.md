# Statecraft: Israel: M0–M2 implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task by task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:**
- Answer the engine unknowns (M0).
- Build the headless 1948–49 simulation with real, sourced content (M1).
- Render a real-terrain hex map of Israel with the 1948 settlements, including a recruiting flythrough (M2).

**Architecture:**
- **Simulation:** a pure C# library (`sim/`, netstandard2.1, C# 9). It is deterministic: seed plus commands gives the same state hash. It is fed by strict YAML content in `content/`, where every fact is sourced.
- **Map data:** Python tools (`tools/`) turn Copernicus elevation data into a heightmap, a hex grid and settlement data.
- **Unity:** a Unity 6.3 URP project (`unity/`) that only renders. It consumes the sim DLL and the map files.

**Tech stack:**
- .NET SDK 10.0.401; YamlDotNet 18.1.0; xUnit.
- Unity 6000.3.25f1 (URP, UI Toolkit).
- Python through uv 0.12.5, with rasterio 1.5.2 (GDAL 3.12.2), numpy 2.5.3, pyyaml 6.0.3, pillow 12.3.0 and pytest 9.1.1.
- ffmpeg.

**Spec:** `docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`

**How this plan was verified (2026-10-09):**

| Part | How it was verified | Result |
|---|---|---|
| M1 (Tasks 4–18) | Every task was run in order in a scratch repo, then replayed from this markdown alone into an empty repo | 0 warnings, 0 errors, **90/90 tests pass**. Unqualified `Simulation.Step` called from `namespace Statecraft.Game` compiles and runs |
| M2 tools (Tasks 19–22) | Run for real against the Copernicus tiles. The preview was checked by eye against `promo/ref/geo-reference.png` | 44 tests pass |
| Unity C# (Tasks 2, 23–25) | **Not compiled.** Unity needs the owner's sign-in (Task 1) | Task 2 is the first compile; its "API corrections" must be applied to Tasks 23–25 before they start |
| M0 spike library | Compiled and run | Parses YAML correctly |
| Settlement YAML in C# | All 25 entries parsed with the exact classes Task 25 uses | Parses correctly |

## Global constraints

- **Sim target:** `netstandard2.1`, `LangVersion 9.0`, `Nullable enable`, warnings as errors. No Unity references in `sim/`.
- **Sim entry point:** static class `Statecraft.Sim.Simulation`. Never name a type after its namespace.
- **Unity:** editor `6000.3.25f1`, URP from the Hub's **Universal 3D** template, and `$UNITY` as set in `docs/dev-setup.md`.
- **Content:**
  - YAML with snake_case keys. Unknown keys and broken references are load errors that name the file and field.
  - **Every real-world fact carries `sources:` (URL + `checked: '2026-10-09'` or a later date).**
- **Narrative:**
  - Israeli perspective and terminology.
  - One sourced value per fact; no contested ranges.
  - No quotes attributed to real people unless sourced verbatim.
  - No treasury or score reward on choices in events tagged `holocaust`, `terror`, `massacre` or `war-casualties`.
- **Real people:** real names. Anyone with `living: true` is excluded from release builds until legal review. Portraits come only from public-domain or CC0 photos, with the licence recorded.
- **Map:** bounds lon 34.20–35.90, lat 29.45–33.35; heightmap 2049² uint16 little-endian, row 0 = north; pointy-top hexes, 5 km circumradius.
- **Copernicus:** the GLO-30 attribution text travels with every map artefact (it lives in `heightmap.json` and `tools/README.md`).
- **Commits:** end every commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Language:** English only. Keep UI strings outside code from M3 on.

## Review focus

These are the inputs most likely to hurt someone using this software that the spec implies but no feature test exercises. Each now has a pinning test in the task that owns the code.

1. **Office term ending before it starts** (a historian's typo in `people.yaml`). Expected: a load error naming the file, the person id and `offices`. *Test in Task 6.*
2. **Two people holding the same office on overlapping dates.** Expected: a load error naming the office and both people, so "who held office on this date" is never silently ambiguous. *Test in Task 7.*
3. **A duplicate `id` in any content list.** Expected: a load error naming the file and the id. *Test in Task 6.*
4. **Arrivals spread by day across leap days (1948, 1952, 1956) and year boundaries.** Expected: per-turn arrivals summed over a data period equal the period total. *Test in Task 11.*
5. **A truncated or wrong-size `heightmap.r16`** (an interrupted tool run or a partial git checkout). Expected: a clear `InvalidDataException` stating the expected byte count, not a garbled map. *Test in Task 24.*

---

## Milestone M0: spike (throwaway code, real answers), Tasks 1–3

The M0 spike answers five questions with evidence, recorded in `docs/spikes/m0-results.md`:
- (a) Does a URP **Web** build run our map test in the browser?
- (b) Does **Unity MCP** work with Claude Code on our plan?
- (c) Can a plain **.NET Standard 2.1 DLL that uses YamlDotNet** load in Unity, including in an IL2CPP Web build?
- (d) Can **UI Toolkit runtime data binding** drive a 13-card cabinet panel?
- (e) Can URP give the **tilt-shift depth-of-field** look (Gaussian distance blur)?

Spike code lives in `spikes/m0/` and is labelled throwaway. Nothing in `sim/` or `unity/` depends on it.

> **Verification status:** the C# Unity code in Tasks 2, 23, 24 and 25 has **not** been compiled yet, because Unity is not installed until Task 1. Task 2's purpose is to compile and run these APIs for the first time.
>
> If an API differs in Unity 6000.3.25f1:
> 1. Fix the spike.
> 2. Record the correction in `docs/spikes/m0-results.md` under "API corrections".
> 3. Apply the same correction to Tasks 23–25 **before** starting them.

### Task 1: Developer environment

**Files:**
- Create: `docs/dev-setup.md`

**Interfaces:**
- Produces:
  - Editor binary at `$UNITY` = `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity`. All later Unity steps use `$UNITY`.
  - `dotnet` 10.0.401 on PATH.

- [ ] **Step 1: Check that .NET is installed**

Run: `dotnet --list-sdks`
Expected: `10.0.401 [/opt/homebrew/Cellar/dotnet/10.0.401/libexec/sdk]` (installed on 2026-10-09 with `brew install dotnet`)

- [ ] **Step 2: Install Unity Hub**

Run: `brew install --cask unity-hub`
Expected: `unity-hub` 3.22.2 is installed to `/Applications/Unity Hub.app`.

- [ ] **Step 3 (owner, interactive): Sign in and install the editor**

We use the Hub app here, not a CLI. Unity marks the Hub CLI as deprecated since Hub 3.18.0 and the new Unity CLI as experimental.

1. Open Unity Hub.
2. Sign in, and activate a **Personal** license.
3. Go to Installs → Install Editor → Archive / version **6000.3.25f1**. This is 6.3 LTS, released 2026-09-24, changeset `e1dba0a9aba4`.
4. Select the modules **Web Build Support** (`webgl`) and **Mac Build Support (IL2CPP)** (`mac-il2cpp`).

- [ ] **Step 4: Check that the editor is installed**

Run:
```bash
export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"
ls "$UNITY" && ls "/Applications/Unity/Hub/Editor/6000.3.25f1/PlaybackEngines/" | grep -i -E 'web|mac'
```
Expected: the binary path is printed, and the PlaybackEngines list includes a WebGL/Web support folder and a MacStandaloneSupport folder.

If the Hub installed the editor somewhere else, run `ls /Applications/Unity/Hub/Editor/` and correct `$UNITY` in `docs/dev-setup.md`.

- [ ] **Step 5: Write `docs/dev-setup.md`**

```markdown
# Developer setup (verified 2026-10-09 on macOS 27.0.1, arm64)

- .NET SDK 10.0.401: `brew install dotnet`
- Unity Hub 3.22.2: `brew install --cask unity-hub`. Sign in and activate a Personal license.
- Unity Editor 6000.3.25f1 (6.3 LTS), changeset e1dba0a9aba4, installed through the Hub app.
  Modules: Web Build Support, Mac Build Support (IL2CPP).
- `export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"`
- Python tools: `uv` (0.12.5 verified). Dependencies are declared inline in each tools/*.py script.
- ffmpeg (Homebrew) is used for flythrough videos.
```

- [ ] **Step 6: Commit**

```bash
git add docs/dev-setup.md
git commit -m "docs: developer environment setup

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 2: Spike: DLL plus YAML in Unity, UI Toolkit binding, URP depth of field, Web and Mac builds

**Files:**
- Create: `spikes/m0/lib/SpikeLib.csproj`, `spikes/m0/lib/Probe.cs`
- Create (Unity project made from the Hub's **Universal 3D** template at `spikes/m0/unity`):
  - `Assets/Spike/SpikeBoot.cs`
  - `Assets/Spike/CabinetVM.cs`
  - `Assets/Editor/SpikeBuild.cs`
  - `Assets/link.xml`
- Create: `docs/spikes/m0-results.md`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: `$UNITY` from Task 1.
- Produces: `docs/spikes/m0-results.md`, with answers (a), (c), (d), (e) and (f), FPS numbers, and an "API corrections" list.

- [ ] **Step 1 (owner, interactive): Create the Unity project**

In Unity Hub: Projects → New project → template **Universal 3D** → location `~/israel-game/.worktrees/m0-m2/spikes/m0` (the m0-m2 worktree, not the main checkout), name `unity`, editor 6000.3.25f1. Wait for the project to open, then close the editor.

We use the template because it sets up URP correctly. Getting the URP package version for 6000.3 right by hand is error-prone.

- [ ] **Step 2: Write the spike library**

`spikes/m0/lib/SpikeLib.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="YamlDotNet" Version="18.1.0" />
  </ItemGroup>
</Project>
```

`spikes/m0/lib/Probe.cs`:
```csharp
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SpikeLib
{
    public sealed class Minister
    {
        public string Name { get; set; } = "";
        public string Portfolio { get; set; } = "";
    }

    public static class Probe
    {
        public static List<Minister> ParseMinisters(string yaml) =>
            new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
                .Deserialize<List<Minister>>(yaml);
    }
}
```

- [ ] **Step 3: Build it into the Unity plugins folder**

Run: `dotnet publish spikes/m0/lib -c Release -o spikes/m0/unity/Assets/Plugins/SpikeLib && ls spikes/m0/unity/Assets/Plugins/SpikeLib`
Expected (verified 2026-10-09 by building this exact library in scratch):
```
SpikeLib.deps.json
SpikeLib.dll
SpikeLib.pdb
YamlDotNet.dll
```

- [ ] **Step 4: Protect the reflection-based types from IL2CPP code stripping**

`spikes/m0/unity/Assets/link.xml`:
```xml
<linker>
  <assembly fullname="YamlDotNet" preserve="all" />
  <assembly fullname="SpikeLib" preserve="all" />
</linker>
```

**Why:** YamlDotNet builds objects through reflection. IL2CPP managed code stripping can remove the constructors and setters it needs, and that would only fail in the Web or Mac player, never in the editor.

- [ ] **Step 5: Write the view model and the spike scene script**

`spikes/m0/unity/Assets/Spike/CabinetVM.cs`:
```csharp
using System;
using Unity.Properties;
using UnityEngine.UIElements;

public sealed class CabinetVM : INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
    string _name = "", _portfolio = "";
    int _competence;

    [CreateProperty] public string Name { get => _name; set { _name = value; Notify(nameof(Name)); } }
    [CreateProperty] public string Portfolio { get => _portfolio; set { _portfolio = value; Notify(nameof(Portfolio)); } }
    [CreateProperty] public int Competence { get => _competence; set { _competence = value; Notify(nameof(Competence)); } }

    void Notify(string p) => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(p));
}
```

`spikes/m0/unity/Assets/Spike/SpikeBoot.cs`:
```csharp
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public sealed class SpikeBoot : MonoBehaviour
{
    const string Yaml = "- name: David Ben-Gurion\n  portfolio: Prime Minister & Defense\n- name: Eliezer Kaplan\n  portfolio: Finance\n";
    public PanelSettings panelSettings;
    readonly List<CabinetVM> _vms = new List<CabinetVM>();
    Mesh _hex; Material _mat; Matrix4x4[] _xf; float _fpsTimer; int _frames;

    void Start()
    {
        // (c) A plain .NET Standard DLL plus YamlDotNet, used from Unity.
        var ministers = SpikeLib.Probe.ParseMinisters(Yaml);
        Debug.Log($"SPIKE c: yaml ok count={ministers.Count} first={ministers[0].Name}");

        // (d) UI Toolkit runtime data binding: 13 cards, each bound to its own view model.
        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        var root = doc.rootVisualElement;
        root.style.flexDirection = FlexDirection.Row; root.style.flexWrap = Wrap.Wrap;
        for (int i = 0; i < 13; i++)
        {
            var vm = new CabinetVM { Name = i < ministers.Count ? ministers[i].Name : $"Minister {i}", Portfolio = "Portfolio", Competence = 5 };
            _vms.Add(vm);
            var card = new VisualElement { dataSource = vm };
            card.style.width = 180; card.style.height = 70; card.style.backgroundColor = new Color(0.07f, 0.1f, 0.18f, 0.9f);
            card.style.marginLeft = 4; card.style.marginTop = 4;
            foreach (var (prop, path) in new[] { ("text", nameof(CabinetVM.Name)), ("text", nameof(CabinetVM.Portfolio)), ("text", nameof(CabinetVM.Competence)) })
            {
                var label = new Label(); label.style.color = new Color(0.9f, 0.85f, 0.7f);
                label.SetBinding(prop, new DataBinding { dataSourcePath = new PropertyPath(path), bindingMode = BindingMode.ToTarget });
                card.Add(label);
            }
            root.Add(card);
        }
        Invoke(nameof(MutateOne), 2f);

        // (e) URP depth of field: the tilt-shift look.
        var volume = new GameObject("DoF").AddComponent<Volume>();
        volume.isGlobal = true; volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        // Gaussian, not Bokeh: Bokeh is physical, so at map-scale distances everything is in focus.
        var dof = volume.profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(80f);
        dof.gaussianEnd.Override(160f); dof.gaussianMaxRadius.Override(1.5f);
        Debug.Log("SPIKE e: gaussian dof volume created");

        // Legacy TextMesh labels are used for settlement names in Task 25; check they render under URP.
        var textProbe = new GameObject("LabelProbe").AddComponent<TextMesh>();
        textProbe.text = "Tel Aviv"; textProbe.characterSize = 1f; textProbe.color = Color.black;
        textProbe.transform.position = new Vector3(45, 6, 30); textProbe.transform.rotation = Quaternion.Euler(50, 0, 0);
        Debug.Log("SPIKE f: textmesh label created (check it is visible, not missing or magenta)");

        // (a) Map stress test: 4,000 instanced hex caps (about the hex count of the real map).
        _hex = BuildHexCap(); _mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
        _xf = new Matrix4x4[4000];
        for (int i = 0; i < _xf.Length; i++)
        {
            int q = i % 50, r = i / 50;
            var p = new Vector3(Mathf.Sqrt(3f) * (q + r / 2f), Mathf.PerlinNoise(q * 0.1f, r * 0.1f) * 3f, 1.5f * r);
            _xf[i] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one);
        }
        var cam = Camera.main; cam.transform.position = new Vector3(45, 70, -10); cam.transform.rotation = Quaternion.Euler(50, 0, 0); cam.fieldOfView = 35;
    }

    void MutateOne() { _vms[0].Competence = 9; Debug.Log("SPIKE d: mutated vm0 competence to 9 (check card shows 9)"); }

    void Update()
    {
        var rp = new RenderParams(_mat);
        for (int i = 0; i < _xf.Length; i += 1023)
            Graphics.RenderMeshInstanced(rp, _hex, 0, _xf, Mathf.Min(1023, _xf.Length - i), i);
        _frames++; _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 5f) { Debug.Log($"SPIKE a: fps={_frames / _fpsTimer:F1}"); _frames = 0; _fpsTimer = 0; }
    }

    static Mesh BuildHexCap() // top face only: enough to load the instancing path
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int k = 0; k < 6; k++) { float a = Mathf.Deg2Rad * (60 * k - 30); v.Add(new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a))); }
        for (int k = 1; k < 5; k++) { t.Add(0); t.Add(k + 1); t.Add(k); }
        var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
    }
}
```

- [ ] **Step 6: Write the editor build script (sets up the scene and builds Web and Mac)**

`spikes/m0/unity/Assets/Editor/SpikeBuild.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class SpikeBuild
{
    const string ScenePath = "Assets/Spike/Spike.unity";

    public static void MakeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        AssetDatabase.CreateAsset(ps, "Assets/Spike/SpikePanel.asset");
        var boot = new GameObject("SpikeBoot").AddComponent<SpikeBoot>();
        boot.panelSettings = ps;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void BuildWeb()
    {
        MakeScene();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // plain http.server can serve it
        Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "../build/web", target = BuildTarget.WebGL }));
    }

    public static void BuildMac()
    {
        MakeScene();
        Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "../build/mac/Spike.app", target = BuildTarget.StandaloneOSX }));
    }

    static void Report(UnityEditor.Build.Reporting.BuildReport r)
    {
        Debug.Log($"SPIKE build {r.summary.platform}: {r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize}");
        if (r.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
```

- [ ] **Step 7: Build the Web player in batch mode**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath spikes/m0/unity -executeMethod SpikeBuild.BuildWeb -logFile spikes/m0/build-web.log; echo exit=$?; grep -E "SPIKE build|error CS" spikes/m0/build-web.log
```
Expected: `exit=0` and `SPIKE build WebGL: Succeeded errors=0`.

On `error CS…`, fix the API usage in the spike, note the correction under "API corrections" in the results doc, and rerun.

- [ ] **Step 8: Run it in the browser and capture the evidence**

Run: `python3 -m http.server 8080 -d spikes/m0/build/web`. Then open `http://localhost:8080` in Chrome, using the claude-in-chrome tools to read the console.

Expected console lines:
- `SPIKE c: yaml ok count=2 first=David Ben-Gurion` proves that YAML works in the IL2CPP Web build.
- `SPIKE e: gaussian dof volume created`.
- `SPIKE f: textmesh label created …`. The "Tel Aviv" label must be visible in the screenshot.
- `SPIKE d: mutated vm0 competence to 9`. The first card must then *show* 9.
- `SPIKE a: fps=…` every 5 s.

Take a screenshot. Then run `SpikeBuild.BuildMac` the same way, run the app, and read `~/Library/Logs/DefaultCompany/unity/Player.log` for the same `SPIKE` lines.

- [ ] **Step 9: Record the answers**

`docs/spikes/m0-results.md`:
```markdown
# M0 spike results (date, Unity 6000.3.25f1)

| Q | Question | Answer | Evidence |
|---|---|---|---|
| a | URP Web build runs a 4,000-hex instanced scene | yes/no, fps=… (Chrome, this Mac) | screenshot path, console line |
| c | netstandard2.1 DLL + YamlDotNet in Unity (IL2CPP Web and Mac) | yes/no | `SPIKE c` line from browser and Player.log |
| d | UI Toolkit runtime binding, 13 cards, live update | yes/no | screenshot before/after the mutation |
| e | URP Gaussian DoF (tilt-shift look) | yes/no | screenshot |
| f | Legacy TextMesh label renders under URP | yes/no | screenshot; if no, Task 25 must switch to UI Toolkit world-space labels |

## API corrections
- (list every change made to compile or run, so Tasks 23–25 get the same fix)

## Decision
- M3 delivery: browser link (if a = yes and fps ≥ 30) / downloadable build (otherwise).
```

- [ ] **Step 10: Commit the answers and the spike source (not the build output)**

```bash
printf 'spikes/m0/build/\nspikes/m0/*.log\nspikes/m0/lib/bin/\nspikes/m0/lib/obj/\nspikes/m0/unity/Library/\nspikes/m0/unity/Temp/\nspikes/m0/unity/Logs/\nspikes/m0/unity/UserSettings/\nspikes/m0/unity/Assets/Plugins/\n' >> .gitignore
git add .gitignore spikes/m0 docs/spikes/m0-results.md
git commit -m "spike(m0): answers for web build, DLL+YAML, UI binding, DoF

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 3: Spike: Unity MCP with Claude Code on our plan

**Files:**
- Modify: `docs/spikes/m0-results.md` (add row b)

**Interfaces:**
- Produces: a yes/no on MCP, and the exact `claude mcp add` line that worked (or the reason it didn't).

The steps come from Unity's guide (https://unity.com/blog/unity-ai-mcp-how-to-get-started, read 2026-10-09). It requires:
- Unity 6 or later with the AI Assistant package;
- a project connected to Unity Cloud;
- an active trial or subscription to Unity's AI tools beta.

The guide does not say whether a Personal plan qualifies. Answering that is the point of this task.

- [ ] **Step 1 (owner, interactive): Install and connect**

In `spikes/m0/unity`:
1. Open Window → Package Manager and install the AI Assistant package (`com.unity.ai.assistant`).
2. Connect the project to Unity Cloud: Edit → Project Settings → Services.
3. Start the AI tools trial if prompted.

- [ ] **Step 2: Check that the bridge is running**

Edit → Project Settings → AI → Unity MCP must show **Unity Bridge: Running** (green). If it shows Stopped, press Start.

- [ ] **Step 3: Register the MCP server with Claude Code**

Run: `ls ~/.unity/relay/` to find the macOS Apple Silicon relay binary, then:
```bash
claude mcp add unity -- ~/.unity/relay/<binary-from-ls> --mcp
```
In the Unity window, accept the "Pending Connection".

- [ ] **Step 4: Test it**

In the editor, open `Assets/Spike/Spike.unity` (created by Task 2's `MakeScene`) and press **Play**, so the `SPIKE` lines reach the editor console. Then, in Claude Code, ask: "Read the Unity console messages and summarize any warnings or errors."
Expected: a summary that includes the `SPIKE` log lines.

- [ ] **Step 5: Record and commit**

Add row **b** to the table, with: plan or trial status, yes/no, and the exact command used. If the answer is "no" or "trial only", the fallback is batch mode plus scripts, as used in Task 2. That fallback blocks nothing.

```bash
git add docs/spikes/m0-results.md spikes/m0/unity/Packages spikes/m0/unity/ProjectSettings
git commit -m "spike(m0): Unity MCP result

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

---

## Milestone M1: headless simulation core and 1948–49 content, Tasks 4–18

**Goal of this milestone:** a pure C# simulation library (`sim/Statecraft.Sim`, netstandard2.1, no Unity references) that plays 1948–49 from sourced YAML content, deterministically, with a text CLI and a calibration test against CBS population data.

**How every block below was verified (2026-10-09):** each task's steps were executed in order in a scratch copy of the repo with .NET SDK 10.0.401 (Homebrew). The failing-test outputs and pass lines below are the real outputs from that run. The final state builds with `0 Warning(s) 0 Error(s)` under `TreatWarningsAsErrors` and passes 90 tests.

**Conventions used by every M1 task:**
- Run all commands from the repository root.
- `sim/Directory.Build.props` turns warnings into errors, so a warning fails the task.
- Content YAML uses snake_case keys (YamlDotNet `UnderscoredNamingConvention`); unknown keys are load errors by design.
- Historical facts in `content/` carry `sources:` (URL + `checked: 2026-10-09`) and `status: fact-checked`; game-balance numbers are labelled as such in file comments.
- Modified files are shown as exact unified diffs; apply them literally.

### Task 4: Solution scaffold

Three projects: the netstandard2.1 sim library (loadable by Unity), an xUnit test project and a CLI. Package versions are pinned to what was verified.

**Files:**
- Create: `sim/Directory.Build.props`
- Create: `sim/Statecraft.Cli/Program.cs`
- Create: `sim/Statecraft.Cli/Statecraft.Cli.csproj`
- Create: `sim/Statecraft.Sim.Tests/SmokeTests.cs`
- Create: `sim/Statecraft.Sim.Tests/Statecraft.Sim.Tests.csproj`
- Create: `sim/Statecraft.Sim/SimInfo.cs`
- Create: `sim/Statecraft.Sim/Statecraft.Sim.csproj`
- Modify: `.gitignore`
- Create (by command in a later step): `sim/Statecraft.sln`

**Interfaces:**
- Consumes: nothing (first M1 task); .NET SDK 10.0.401 installed in Task 1.
- Produces: `Statecraft.Sim.SimInfo.Version` (`const string`, "0.1.0"); solution `sim/Statecraft.sln` with projects `Statecraft.Sim`, `Statecraft.Sim.Tests`, `Statecraft.Cli`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/SmokeTests.cs`:

```csharp
using Statecraft.Sim;

public class SmokeTests
{
    [Fact]
    public void SimLibraryIsReferenced() => Assert.Equal("0.1.0", SimInfo.Version);
}
```

`sim/Statecraft.Sim.Tests/Statecraft.Sim.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Statecraft.Sim/Statecraft.Sim.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): `MSBUILD : error MSB1009: Project file does not exist.` (the solution does not exist yet)

- [ ] **Step 3: Implement**

`sim/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

`sim/Statecraft.Cli/Program.cs`:

```csharp
Console.WriteLine($"Statecraft sim {Statecraft.Sim.SimInfo.Version}");
```

`sim/Statecraft.Cli/Statecraft.Cli.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Statecraft.Sim/Statecraft.Sim.csproj" />
  </ItemGroup>
</Project>
```

`sim/Statecraft.Sim/SimInfo.cs`:

```csharp
namespace Statecraft.Sim
{
    public static class SimInfo
    {
        public const string Version = "0.1.0";
    }
}
```

`sim/Statecraft.Sim/Statecraft.Sim.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <RootNamespace>Statecraft.Sim</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="YamlDotNet" Version="18.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Ignore build output**

Append to the repository's `.gitignore`:

```
sim/**/bin/
sim/**/obj/
```

Run `git check-ignore -v sim/Statecraft.Sim/bin/x`. Expected: a line naming `.gitignore` and `sim/**/bin/`.

- [ ] **Step 5: Create the solution file and add the projects**

Run:

```bash
cd sim
dotnet new sln -n Statecraft --format sln
dotnet sln Statecraft.sln add Statecraft.Sim/Statecraft.Sim.csproj Statecraft.Sim.Tests/Statecraft.Sim.Tests.csproj Statecraft.Cli/Statecraft.Cli.csproj
cd ..
```

Expected: three lines `Project ... added to the solution.` (.NET 10 defaults to `.slnx`; `--format sln` keeps the `Statecraft.sln` name from the spec.)

Also run `dotnet run --project sim/Statecraft.Cli`. Expected: `Statecraft sim 0.1.0`.

- [ ] **Step 6: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    1, Skipped:     0, Total:    1`

- [ ] **Step 7: Commit**

```bash
git add .gitignore sim/Directory.Build.props sim/Statecraft.Cli/Program.cs sim/Statecraft.Cli/Statecraft.Cli.csproj sim/Statecraft.Sim.Tests/SmokeTests.cs sim/Statecraft.Sim.Tests/Statecraft.Sim.Tests.csproj sim/Statecraft.Sim/SimInfo.cs sim/Statecraft.Sim/Statecraft.Sim.csproj sim/Statecraft.sln
git commit -m "feat(sim): scaffold solution, sim library, tests and CLI

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 5: Time, deterministic RNG and state hash

Turn lengths (quarter/month/week), a SplitMix64 generator whose whole state is one `ulong` in `GameState`, and the full `GameState` contract with a canonical SHA-256 hash. Later tasks add logic but never change the state's shape.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/CoreTests.cs`
- Create: `sim/Statecraft.Sim/GameState.cs`
- Create: `sim/Statecraft.Sim/Rng.cs`
- Create: `sim/Statecraft.Sim/SimTime.cs`

**Interfaces:**
- Consumes: Task 4 projects.
- Produces: `enum TurnLength {Quarter, Month, Week}`; `SimTime.Advance(DateTime, TurnLength) -> DateTime`; `SimTime.YearFraction(TurnLength) -> double`; `struct Rng { ulong State; Rng(ulong); ulong NextULong(); double NextDouble(); }`; `enum Mode {Historical, Open}`; `sealed class GameState` (public fields listed in the file) with `Clone() -> GameState`, `Hash() -> string`, `TotalPopulation -> double`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/CoreTests.cs`:

```csharp
using System;
using Statecraft.Sim;

public class CoreTests
{
    [Theory]
    [InlineData(TurnLength.Quarter, "1948-08-15")]
    [InlineData(TurnLength.Month, "1948-06-15")]
    [InlineData(TurnLength.Week, "1948-05-22")]
    public void AdvanceMovesDateByTurnLength(TurnLength len, string expected)
    {
        var start = new DateTime(1948, 5, 15);
        Assert.Equal(DateTime.Parse(expected), SimTime.Advance(start, len));
    }

    [Fact]
    public void TurnFractionOfYear()
    {
        Assert.Equal(0.25, SimTime.YearFraction(TurnLength.Quarter));
        Assert.Equal(1.0 / 12, SimTime.YearFraction(TurnLength.Month), 10);
        Assert.Equal(7.0 / 365, SimTime.YearFraction(TurnLength.Week), 10);
    }

    [Fact]
    public void RngIsDeterministicForSameSeed()
    {
        var a = new Rng(42); var b = new Rng(42);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextDouble(), b.NextDouble());
    }

    [Fact]
    public void RngValuesAreInUnitInterval()
    {
        var r = new Rng(7);
        for (int i = 0; i < 1000; i++) { var v = r.NextDouble(); Assert.True(v >= 0.0 && v < 1.0, $"NextDouble out of [0,1): {v}"); }
    }

    [Fact]
    public void HashIsStableAndSensitiveToState()
    {
        var s1 = new GameState { Date = new DateTime(1948, 5, 15), RngState = 1 };
        s1.Indicators["morale"] = 0.5;
        var s2 = s1.Clone();
        Assert.Equal(s1.Hash(), s2.Hash());
        s2.Indicators["morale"] = 0.6;
        Assert.NotEqual(s1.Hash(), s2.Hash());
        Assert.Equal(0.5, s1.Indicators["morale"]); // clone is deep
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0246: The type or namespace name 'TurnLength' could not be found (are you missing a using directive or an assembly reference?)`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/GameState.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Statecraft.Sim
{
    public enum Mode { Historical, Open }

    /// <summary>All mutable game data. Systems never share references between states: Step works on Clone().</summary>
    public sealed class GameState
    {
        public DateTime Date;
        public ulong RngState;
        public Mode Mode;
        public int Turn;
        public Dictionary<string, double> Indicators = new Dictionary<string, double>();
        public Dictionary<string, double> Policies = new Dictionary<string, double>();
        public Dictionary<string, double> EffectContributions = new Dictionary<string, double>();
        public SortedSet<string> ActiveSituations = new SortedSet<string>(StringComparer.Ordinal);
        public Dictionary<string, double> Budget = new Dictionary<string, double>();
        public double Treasury;
        public double Debt;
        public double FxReserves;
        public Dictionary<string, double> Population = new Dictionary<string, double>();
        public int Rung = 1;
        public int CeasefireWeeksLeft;
        public double Mobilization;
        public double CoalitionStability = 1.0;
        public double Attention;
        public SortedSet<string> FiredEvents = new SortedSet<string>(StringComparer.Ordinal);
        public List<string> PendingEvents = new List<string>();
        public string? GameOver;

        public double TotalPopulation => Population.Values.Sum();

        public GameState Clone()
        {
            var c = (GameState)MemberwiseClone();
            c.Indicators = new Dictionary<string, double>(Indicators);
            c.Policies = new Dictionary<string, double>(Policies);
            c.EffectContributions = new Dictionary<string, double>(EffectContributions);
            c.ActiveSituations = new SortedSet<string>(ActiveSituations, StringComparer.Ordinal);
            c.Budget = new Dictionary<string, double>(Budget);
            c.Population = new Dictionary<string, double>(Population);
            c.FiredEvents = new SortedSet<string>(FiredEvents, StringComparer.Ordinal);
            c.PendingEvents = new List<string>(PendingEvents);
            return c;
        }

        /// <summary>SHA-256 over a canonical text form (ordinal-sorted keys, round-trip doubles).</summary>
        public string Hash()
        {
            var sb = new StringBuilder();
            void Num(string k, double v) => sb.Append(k).Append('=').Append(v.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            void Map(string name, Dictionary<string, double> m)
            {
                sb.Append(name).Append('{');
                foreach (var kv in m.OrderBy(k => k.Key, StringComparer.Ordinal)) Num(kv.Key, kv.Value);
                sb.Append('}');
            }
            sb.Append(Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
              .Append(RngState).Append(';').Append(Mode).Append(';').Append(Turn).Append(';');
            Map("ind", Indicators); Map("pol", Policies); Map("eff", EffectContributions);
            Map("bud", Budget); Map("pop", Population);
            sb.Append("sit[").Append(string.Join(",", ActiveSituations)).Append(']');
            Num("treasury", Treasury); Num("debt", Debt); Num("fx", FxReserves);
            sb.Append("rung=").Append(Rung).Append(";cf=").Append(CeasefireWeeksLeft).Append(';');
            Num("mob", Mobilization); Num("stab", CoalitionStability); Num("att", Attention);
            sb.Append("fired[").Append(string.Join(",", FiredEvents)).Append(']');
            sb.Append("pending[").Append(string.Join(",", PendingEvents)).Append(']');
            sb.Append("over=").Append(GameOver ?? "");
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }
    }
}
```

`sim/Statecraft.Sim/Rng.cs`:

```csharp
namespace Statecraft.Sim
{
    /// <summary>SplitMix64. The whole generator state is one ulong, stored in GameState.</summary>
    public struct Rng
    {
        public ulong State;
        public Rng(ulong state) { State = state; }

        public ulong NextULong()
        {
            ulong z = State += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);
    }
}
```

`sim/Statecraft.Sim/SimTime.cs`:

```csharp
using System;

namespace Statecraft.Sim
{
    public enum TurnLength { Quarter, Month, Week }

    public static class SimTime
    {
        public static DateTime Advance(DateTime date, TurnLength len) => len switch
        {
            TurnLength.Quarter => date.AddMonths(3),
            TurnLength.Month => date.AddMonths(1),
            _ => date.AddDays(7),
        };

        /// <summary>Share of a year one turn represents (used to scale yearly rates).</summary>
        public static double YearFraction(TurnLength len) => len switch
        {
            TurnLength.Quarter => 0.25,
            TurnLength.Month => 1.0 / 12,
            _ => 7.0 / 365,
        };
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    8, Skipped:     0, Total:    8`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/CoreTests.cs sim/Statecraft.Sim/GameState.cs sim/Statecraft.Sim/Rng.cs sim/Statecraft.Sim/SimTime.cs
git commit -m "feat(sim): turn lengths, deterministic RNG, state hash

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 6: Content schema, loader and reference validation

All content files and their data classes, a strict YAML loader (unknown keys fail) and cross-reference validation. Every error is a `ContentException` naming the file and the field. A shared test `Fixture` writes a small valid content set to a temp folder. Ids must be unique within every id-keyed list (a policy may not reuse an indicator id, which would make effect sources ambiguous), and an office term may not end before it starts. `EventRules.Validate` is an empty seam here and is filled in by Task 13.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/ContentLoaderTests.cs`
- Create: `sim/Statecraft.Sim.Tests/Fixture.cs`
- Create: `sim/Statecraft.Sim/Content.cs`
- Create: `sim/Statecraft.Sim/ContentLoader.cs`
- Create: `sim/Statecraft.Sim/EventRules.cs`

**Interfaces:**
- Consumes: Task 5 types.
- Produces: data classes in `Content.cs` (`Content`, `Person`, `OfficeTerm`, `Legislature`, `Government`, `Indicator`, `Policy`, `Effect`, `Situation`, `VoterGroup`, `Actor`, `Rules`, `AliyahPeriod`, `PopulationAnchor`, `GameEvent`, `EventOption`, `OptionEffect`, `EventCondition`, `Source`, `Portrait`, `Office`, `Party`); `ContentLoader.Load(string contentRoot) -> Content` (rejects duplicate ids as `<list>[<id>].id` and terms ending before they start as `people[<id>].offices.<office>`); `ContentException(string file, string field, string message)` with `File`, `Field`; test helper `Fixture.Write(Dictionary<string,string>? overrides = null, Dictionary<string,string>? events = null) -> string`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/ContentLoaderTests.cs`:

```csharp
using System.Collections.Generic;
using Statecraft.Sim;

public class ContentLoaderTests
{
    [Fact]
    public void LoadsValidFixture()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(2, c.Parties.Count);
        Assert.Equal(2, c.People.Count);
        Assert.Equal(10, c.Legislatures[1].Size);
        Assert.Equal(2, c.Effects.Count);
        Assert.Equal(5, c.Rules.StartRung);
    }

    [Fact]
    public void UnknownPartyInSeatsNamesFileAndField()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("beta: 6", "gamma: 6");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].seats.gamma", ex.Field);
    }

    [Fact]
    public void SeatsMustSumToSize()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("size: 10", "size: 11");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures[house].size", ex.Field);
    }

    [Fact]
    public void UnknownYamlPropertyIsAnError()
    {
        var bad = Fixture.Files["parties.yaml"].Replace("name: Beta", "nmae: Beta");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = bad })));
        Assert.Equal("parties.yaml", ex.File);
    }

    [Fact]
    public void EffectTargetMustBeAnIndicator()
    {
        var bad = Fixture.Files["model.yaml"].Replace("target: morale", "target: moral");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = bad })));
        Assert.Equal("effects[rationing_morale].target", ex.Field);
    }

    [Fact]
    public void PersonOfficeMustExist()
    {
        var bad = Fixture.Files["people.yaml"].Replace("office: finance_minister", "office: treasury_boss");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people[def_two].offices.treasury_boss", ex.Field);
    }

    [Fact]
    public void OfficeTermEndingBeforeItStartsIsAnError()
    {
        var bad = Fixture.Files["people.yaml"].Replace("to: 1949-01-01", "to: 1947-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].offices.defense_minister", ex.Field);
    }

    [Theory]
    [InlineData("offices.yaml", "id: finance_minister", "id: defense_minister", "offices[defense_minister].id")]
    [InlineData("parties.yaml", "id: beta", "id: alpha", "parties[alpha].id")]
    [InlineData("people.yaml", "id: def_two", "id: pm_one", "people[pm_one].id")]
    [InlineData("legislatures.yaml", "id: house", "id: council", "legislatures[council].id")]
    [InlineData("governments.yaml", "id: gov2", "id: gov1", "governments[gov1].id")]
    [InlineData("model.yaml", "id: food_supply", "id: morale", "indicators[morale].id")]
    [InlineData("model.yaml", "id: rationing\n    default", "id: morale\n    default", "policies[morale].id")]
    public void DuplicateIdIsAnError(string file, string original, string duplicate, string field)
    {
        var bad = Fixture.Files[file].Replace(original, duplicate);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { [file] = bad })));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }
}
```

`sim/Statecraft.Sim.Tests/Fixture.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>Writes a small, valid content set to a temp dir. Tests override single files to test errors.</summary>
public static class Fixture
{
    public static readonly Dictionary<string, string> Files = new Dictionary<string, string>
    {
        ["offices.yaml"] = @"
offices:
  - id: prime_minister
    title: Prime Minister
  - id: defense_minister
    title: Minister of Defense
  - id: finance_minister
    title: Minister of Finance
",
        ["parties.yaml"] = @"
parties:
  - id: alpha
    name: Alpha
  - id: beta
    name: Beta
",
        ["people.yaml"] = @"
people:
  - id: pm_one
    name: Pm One
    living: false
    competence: 8
    loyalty: 9
    offices:
      - office: prime_minister
        from: 1948-05-14
      - office: defense_minister
        from: 1948-05-14
        to: 1949-01-01
  - id: def_two
    name: Def Two
    living: false
    competence: 4
    loyalty: 5
    offices:
      - office: defense_minister
        from: 1949-01-01
      - office: finance_minister
        from: 1948-05-14
",
        ["legislatures.yaml"] = @"
legislatures:
  - id: council
    body: Council
    from: 1948-05-14
    to: 1949-02-14
    size: 5
    budget_approval: false
    seats: { alpha: 3, beta: 2 }
  - id: house
    body: House
    from: 1949-02-14
    size: 10
    budget_approval: true
    seats: { alpha: 4, beta: 6 }
",
        ["governments.yaml"] = @"
governments:
  - id: gov1
    name: Government One
    from: 1948-05-14
    to: 1949-03-10
    coalition: [alpha]
  - id: gov2
    name: Government Two
    from: 1949-03-10
    coalition: [alpha, beta]
",
        ["model.yaml"] = @"
indicators:
  - id: morale
    base: 0.5
  - id: food_supply
    base: 0.4
policies:
  - id: rationing
    default: 0.0
    office: finance_minister
effects:
  - id: rationing_food
    source: rationing
    target: food_supply
    k: 0.4
    inertia: 2
    office: finance_minister
  - id: rationing_morale
    source: rationing
    target: morale
    k: -0.2
    inertia: 1
situations:
  - id: hunger
    indicator: food_supply
    on_below: 0.3
    off_above: 0.45
voter_groups:
  - id: everyone
    weights: { morale: 1.0 }
actors:
  - id: usa
    name: United States
    standing: 0.5
",
        ["rules.yaml"] = @"
start: 1948-05-15
start_rung: 5
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
rung_attention_step: 0.25
ceasefire_weeks: 4
treasury_start: 100
fx_start: 50
tax_per_capita_per_year: 0.0004
spending_per_capita_per_year: 0.0005
exports_per_year: 20
imports_per_capita_per_year: 0.0001
mobilization_cost_per_year: 40
natural_increase_per_year: 0.017
initial_population: { veterans: 1000 }
budget_start: { finance_minister: 1.0 }
",
        ["population.yaml"] = @"
anchors: []
",
        ["aliyah.yaml"] = @"
series:
  - from: 1948-05-15
    to: 1948-12-31
    origins: { europe: 230, asia: 0 }
",
    };

    public static string Write(Dictionary<string, string>? overrides = null, Dictionary<string, string>? events = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "events"));
        foreach (var kv in Files)
            File.WriteAllText(Path.Combine(dir, kv.Key), overrides != null && overrides.TryGetValue(kv.Key, out var o) ? o : kv.Value);
        if (events != null)
            foreach (var kv in events) File.WriteAllText(Path.Combine(dir, "events", kv.Key), kv.Value);
        return dir;
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'ContentLoader' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Content.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    // Plain data classes filled by YamlDotNet (UnderscoredNamingConvention: SeatsByParty <- seats_by_party).

    public sealed class Source { public string Url { get; set; } = ""; public DateTime Checked { get; set; } }

    public sealed class Office { public string Id { get; set; } = ""; public string Title { get; set; } = ""; }

    public sealed class Party { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }

    public sealed class OfficeTerm
    {
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    public sealed class Portrait { public string File { get; set; } = ""; public string License { get; set; } = ""; public string Source { get; set; } = ""; }

    public sealed class Person
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameHe { get; set; } = "";
        /// <summary>Required (null only when missing, which is a load error): a living person is excluded from release builds.</summary>
        public bool? Living { get; set; }
        public int Competence { get; set; }
        public int Loyalty { get; set; }
        public string Party { get; set; } = "";
        public Portrait? Portrait { get; set; }
        public List<OfficeTerm> Offices { get; set; } = new List<OfficeTerm>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Legislature
    {
        public string Id { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public int Size { get; set; }
        public bool BudgetApproval { get; set; }
        public Dictionary<string, int> Seats { get; set; } = new Dictionary<string, int>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Government
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<string> Coalition { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Indicator { public string Id { get; set; } = ""; public double Base { get; set; } }

    public sealed class Policy
    {
        public string Id { get; set; } = "";
        public double Default { get; set; }
        public string Office { get; set; } = "";
    }

    /// <summary>Additive effect: its contribution to Target moves toward K * source value over Inertia turns.</summary>
    public sealed class Effect
    {
        public string Id { get; set; } = "";
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public double K { get; set; }
        public int Inertia { get; set; } = 1;
        public string Office { get; set; } = "";
    }

    /// <summary>Turns on past one threshold and off past another (hysteresis), so it does not flicker.</summary>
    public sealed class Situation
    {
        public string Id { get; set; } = "";
        public string Indicator { get; set; } = "";
        public double? OnBelow { get; set; }
        public double? OffAbove { get; set; }
        public double? OnAbove { get; set; }
        public double? OffBelow { get; set; }
    }

    public sealed class VoterGroup { public string Id { get; set; } = ""; public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>(); }

    public sealed class Actor { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double Standing { get; set; } }

    public sealed class Rules
    {
        public DateTime Start { get; set; }
        public int StartRung { get; set; } = 1;
        public double AttentionPerTurn { get; set; }
        public Dictionary<string, double> AttentionCost { get; set; } = new Dictionary<string, double>();
        public double RungAttentionStep { get; set; }
        public int CeasefireWeeks { get; set; }
        public double TreasuryStart { get; set; }
        public double FxStart { get; set; }
        public double TaxPerCapitaPerYear { get; set; }
        public double SpendingPerCapitaPerYear { get; set; }
        public double ExportsPerYear { get; set; }
        public double ImportsPerCapitaPerYear { get; set; }
        public double MobilizationCostPerYear { get; set; }
        public double NaturalIncreasePerYear { get; set; }
        public Dictionary<string, double> InitialPopulation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> BudgetStart { get; set; } = new Dictionary<string, double>();
    }

    public sealed class AliyahPeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, double> Origins { get; set; } = new Dictionary<string, double>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class PopulationAnchor
    {
        public DateTime Date { get; set; }
        public double Jews { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class EventCondition { public string Indicator { get; set; } = ""; public double? Min { get; set; } public double? Max { get; set; } }

    public sealed class OptionEffect { public string Target { get; set; } = ""; public double Delta { get; set; } }

    public sealed class EventOption
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public bool Historical { get; set; }
        public bool Recommended { get; set; }
        public List<OptionEffect> Effects { get; set; } = new List<OptionEffect>();
        public int? SetRung { get; set; }
    }

    public sealed class GameEvent
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public string Anchor { get; set; } = "soft";
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<EventCondition> Conditions { get; set; } = new List<EventCondition>();
        public List<EventOption> Options { get; set; } = new List<EventOption>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Content
    {
        public List<Office> Offices = new List<Office>();
        public List<Party> Parties = new List<Party>();
        public List<Person> People = new List<Person>();
        public List<Legislature> Legislatures = new List<Legislature>();
        public List<Government> Governments = new List<Government>();
        public List<Indicator> Indicators = new List<Indicator>();
        public List<Policy> Policies = new List<Policy>();
        public List<Effect> Effects = new List<Effect>();
        public List<Situation> Situations = new List<Situation>();
        public List<VoterGroup> VoterGroups = new List<VoterGroup>();
        public List<Actor> Actors = new List<Actor>();
        public Rules Rules = new Rules();
        public List<AliyahPeriod> Aliyah = new List<AliyahPeriod>();
        public List<PopulationAnchor> PopulationAnchors = new List<PopulationAnchor>();
        public List<GameEvent> Events = new List<GameEvent>();
    }

    public sealed class ContentException : Exception
    {
        public string File { get; }
        public string Field { get; }
        public ContentException(string file, string field, string message) : base($"{file}: {field}: {message}")
        { File = file; Field = field; }
    }
}
```

`sim/Statecraft.Sim/ContentLoader.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    public static class ContentLoader
    {
        sealed class OfficesFile { public List<Office> Offices { get; set; } = new List<Office>(); }
        sealed class PartiesFile { public List<Party> Parties { get; set; } = new List<Party>(); }
        sealed class PeopleFile { public List<Person> People { get; set; } = new List<Person>(); }
        sealed class LegislaturesFile { public List<Legislature> Legislatures { get; set; } = new List<Legislature>(); }
        sealed class GovernmentsFile { public List<Government> Governments { get; set; } = new List<Government>(); }
        sealed class ModelFile
        {
            public List<Indicator> Indicators { get; set; } = new List<Indicator>();
            public List<Policy> Policies { get; set; } = new List<Policy>();
            public List<Effect> Effects { get; set; } = new List<Effect>();
            public List<Situation> Situations { get; set; } = new List<Situation>();
            public List<VoterGroup> VoterGroups { get; set; } = new List<VoterGroup>();
            public List<Actor> Actors { get; set; } = new List<Actor>();
        }
        sealed class AliyahFile { public List<AliyahPeriod> Series { get; set; } = new List<AliyahPeriod>(); }
        sealed class PopulationFile { public List<PopulationAnchor> Anchors { get; set; } = new List<PopulationAnchor>(); }

        static readonly IDeserializer Yaml = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        static T Read<T>(string root, string relPath)
        {
            var path = Path.Combine(root, relPath);
            if (!File.Exists(path)) throw new ContentException(relPath, "(file)", "missing file");
            try { return Yaml.Deserialize<T>(File.ReadAllText(path)) ?? throw new ContentException(relPath, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(relPath, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
        }

        public static Content Load(string contentRoot)
        {
            var c = new Content();
            c.Offices = Read<OfficesFile>(contentRoot, "offices.yaml").Offices;
            c.Parties = Read<PartiesFile>(contentRoot, "parties.yaml").Parties;
            c.People = Read<PeopleFile>(contentRoot, "people.yaml").People;
            c.Legislatures = Read<LegislaturesFile>(contentRoot, "legislatures.yaml").Legislatures;
            c.Governments = Read<GovernmentsFile>(contentRoot, "governments.yaml").Governments;
            var m = Read<ModelFile>(contentRoot, "model.yaml");
            c.Indicators = m.Indicators; c.Policies = m.Policies; c.Effects = m.Effects;
            c.Situations = m.Situations; c.VoterGroups = m.VoterGroups; c.Actors = m.Actors;
            c.Rules = Read<Rules>(contentRoot, "rules.yaml");
            c.Aliyah = Read<AliyahFile>(contentRoot, "aliyah.yaml").Series;
            c.PopulationAnchors = Read<PopulationFile>(contentRoot, "population.yaml").Anchors;
            var eventsDir = Path.Combine(contentRoot, "events");
            if (Directory.Exists(eventsDir))
                foreach (var f in Directory.GetFiles(eventsDir, "*.yaml").OrderBy(p => p, StringComparer.Ordinal))
                    c.Events.Add(Read<GameEvent>(contentRoot, Path.Combine("events", Path.GetFileName(f))));
            Validate(c);
            return c;
        }

        static void Unique(string file, string list, IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
                if (!seen.Add(id)) throw new ContentException(file, $"{list}[{id}].id", "duplicate id");
        }

        static void Validate(Content c)
        {
            var offices = new HashSet<string>(c.Offices.Select(o => o.Id));
            var parties = new HashSet<string>(c.Parties.Select(p => p.Id));
            var indicators = new HashSet<string>(c.Indicators.Select(i => i.Id));
            var sources = new HashSet<string>(c.Policies.Select(p => p.Id).Concat(indicators));
            Unique("offices.yaml", "offices", c.Offices.Select(o => o.Id));
            Unique("parties.yaml", "parties", c.Parties.Select(p => p.Id));
            Unique("people.yaml", "people", c.People.Select(p => p.Id));
            Unique("legislatures.yaml", "legislatures", c.Legislatures.Select(l => l.Id));
            Unique("governments.yaml", "governments", c.Governments.Select(g => g.Id));
            Unique("model.yaml", "indicators", c.Indicators.Select(i => i.Id));
            Unique("model.yaml", "policies", c.Policies.Select(p => p.Id));
            Unique("model.yaml", "effects", c.Effects.Select(e => e.Id));
            Unique("model.yaml", "situations", c.Situations.Select(s => s.Id));
            Unique("model.yaml", "voter_groups", c.VoterGroups.Select(v => v.Id));
            Unique("model.yaml", "actors", c.Actors.Select(a => a.Id));
            foreach (var p in c.Policies)
                if (indicators.Contains(p.Id)) throw new ContentException("model.yaml", $"policies[{p.Id}].id", "same id as an indicator (effect sources would be ambiguous)");

            foreach (var p in c.People)
            {
                if (p.Party != "" && !parties.Contains(p.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].party", $"unknown party '{p.Party}'");
                foreach (var t in p.Offices)
                {
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", $"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})");
                }
            }
            foreach (var l in c.Legislatures)
            {
                foreach (var kv in l.Seats)
                    if (!parties.Contains(kv.Key)) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "unknown party");
                if (l.Seats.Values.Sum() != l.Size) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", $"seats sum to {l.Seats.Values.Sum()}, size is {l.Size}");
            }
            foreach (var g in c.Governments)
                foreach (var p in g.Coalition)
                    if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"governments[{g.Id}].coalition.{p}", "unknown party");
            foreach (var p in c.Policies)
                if (!offices.Contains(p.Office)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", $"unknown office '{p.Office}'");
            foreach (var e in c.Effects)
            {
                if (!sources.Contains(e.Source)) throw new ContentException("model.yaml", $"effects[{e.Id}].source", $"unknown policy or indicator '{e.Source}'");
                if (!indicators.Contains(e.Target)) throw new ContentException("model.yaml", $"effects[{e.Id}].target", $"unknown indicator '{e.Target}'");
                if (e.Inertia < 1) throw new ContentException("model.yaml", $"effects[{e.Id}].inertia", "must be >= 1");
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", $"unknown office '{e.Office}'");
            }
            foreach (var s in c.Situations)
            {
                if (!indicators.Contains(s.Indicator)) throw new ContentException("model.yaml", $"situations[{s.Id}].indicator", $"unknown indicator '{s.Indicator}'");
                bool below = s.OnBelow.HasValue && s.OffAbove.HasValue, above = s.OnAbove.HasValue && s.OffBelow.HasValue;
                if (below == above) throw new ContentException("model.yaml", $"situations[{s.Id}]", "needs exactly one pair: on_below+off_above or on_above+off_below");
            }
            foreach (var v in c.VoterGroups)
                foreach (var k in v.Weights.Keys)
                    if (!indicators.Contains(k)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{k}", "unknown indicator");
            foreach (var k in c.Rules.BudgetStart.Keys)
                if (!offices.Contains(k)) throw new ContentException("rules.yaml", $"budget_start.{k}", "unknown office");
            EventRules.Validate(c, offices, indicators);
        }
    }
}
```

`sim/Statecraft.Sim/EventRules.cs`:

```csharp
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>Load-time checks for events. Extended in the events task.</summary>
    static class EventRules
    {
        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators) { }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    22, Skipped:     0, Total:    22`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/ContentLoaderTests.cs sim/Statecraft.Sim.Tests/Fixture.cs sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/ContentLoader.cs sim/Statecraft.Sim/EventRules.cs
git commit -m "feat(sim): content schema, strict loader, reference validation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 7: Office resolver

"Who held this office on this date" and "which legislature and government sat on this date", with `[from, to)` periods. Event text uses `{office_id}` placeholders resolved here, so names are never hard-coded. The loader also rejects two different people holding one office on overlapping dates, so the resolver can never silently pick one.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/OfficesTests.cs`
- Modify: `sim/Statecraft.Sim/ContentLoader.cs`
- Create: `sim/Statecraft.Sim/Offices.cs`

**Interfaces:**
- Consumes: `Content`, `Person`, `Legislature`, `Government` (Task 6).
- Produces: `Offices.Holder(Content, string office, DateTime) -> Person?`; `Offices.Legislature(Content, DateTime) -> Legislature?`; `Offices.Government(Content, DateTime) -> Government?`; `Offices.Fill(Content, string text, DateTime) -> string` (throws `InvalidOperationException` when an office has no holder); loader error `offices.<office>` naming both person ids on overlapping terms.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/OfficesTests.cs`:

```csharp
using System;
using Statecraft.Sim;

public class OfficesTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void ResolvesHolderOnDate()
    {
        Assert.Equal("pm_one", Offices.Holder(C, "defense_minister", new DateTime(1948, 12, 31))!.Id);
        Assert.Equal("def_two", Offices.Holder(C, "defense_minister", new DateTime(1949, 1, 1))!.Id); // 'to' is exclusive
    }

    [Fact]
    public void NoHolderBeforeFirstTerm() => Assert.Null(Offices.Holder(C, "prime_minister", new DateTime(1948, 1, 1)));

    [Fact]
    public void CurrentLegislatureAndGovernment()
    {
        Assert.Equal("council", Offices.Legislature(C, new DateTime(1948, 6, 1))!.Id);
        Assert.Equal("house", Offices.Legislature(C, new DateTime(1949, 2, 14))!.Id);
        Assert.Equal("gov2", Offices.Government(C, new DateTime(1949, 3, 10))!.Id);
    }

    [Fact]
    public void FillsOfficePlaceholders()
    {
        var text = Offices.Fill(C, "{defense_minister} briefs {prime_minister}.", new DateTime(1949, 6, 1));
        Assert.Equal("Def Two briefs Pm One.", text);
    }

    [Fact]
    public void UnfilledPlaceholderThrows() =>
        Assert.Throws<InvalidOperationException>(() => Offices.Fill(C, "{prime_minister}", new DateTime(1947, 1, 1)));

    [Fact]
    public void TwoPeopleCannotHoldOneOfficeOnOverlappingDates()
    {
        var bad = Fixture.Files["people.yaml"].Replace("to: 1949-01-01", "to: 1949-06-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new System.Collections.Generic.Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("offices.defense_minister", ex.Field);
        Assert.Contains("pm_one", ex.Message);
        Assert.Contains("def_two", ex.Message);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'Offices' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/ContentLoader.cs` (apply this change):

```diff
@@ -97,6 +97,15 @@ namespace Statecraft.Sim
                     if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", $"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})");
                 }
             }
+            var terms = c.People.SelectMany(p => p.Offices.Select(t => (person: p.Id, term: t))).ToList();
+            for (int i = 0; i < terms.Count; i++)
+                for (int j = i + 1; j < terms.Count; j++)
+                {
+                    var (a, b) = (terms[i], terms[j]);
+                    if (a.term.Office != b.term.Office || a.person == b.person) continue;
+                    if (a.term.From < (b.term.To ?? DateTime.MaxValue) && b.term.From < (a.term.To ?? DateTime.MaxValue))
+                        throw new ContentException("people.yaml", $"offices.{a.term.Office}", $"held by both {a.person} and {b.person} on overlapping dates");
+                }
             foreach (var l in c.Legislatures)
             {
                 foreach (var kv in l.Seats)
```

`sim/Statecraft.Sim/Offices.cs`:

```csharp
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>"Who held this office / which body sat on this date." Periods are [from, to).</summary>
    public static class Offices
    {
        static bool In(DateTime d, DateTime from, DateTime? to) => d >= from && (to == null || d < to.Value);

        public static Person? Holder(Content c, string office, DateTime date) =>
            c.People.FirstOrDefault(p => p.Offices.Any(t => t.Office == office && In(date, t.From, t.To)));

        public static Legislature? Legislature(Content c, DateTime date) =>
            c.Legislatures.FirstOrDefault(l => In(date, l.From, l.To));

        public static Government? Government(Content c, DateTime date) =>
            c.Governments.FirstOrDefault(g => In(date, g.From, g.To));

        static readonly Regex Placeholder = new Regex(@"\{([a-z_]+)\}");

        /// <summary>Replaces {office_id} with the name of the person holding it on date.</summary>
        public static string Fill(Content c, string text, DateTime date) =>
            Placeholder.Replace(text, m =>
                Holder(c, m.Groups[1].Value, date)?.Name
                ?? throw new InvalidOperationException($"no holder of '{m.Groups[1].Value}' on {date:yyyy-MM-dd}"));
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/OfficesTests.cs sim/Statecraft.Sim/ContentLoader.cs sim/Statecraft.Sim/Offices.cs
git commit -m "feat(sim): office resolver and text placeholders

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 8: Effects web

Democracy-style effects: each effect's contribution moves toward `k x source` over `inertia` turns, scaled by the responsible minister's competence; indicators clamp to 0..1; situations switch on and off at different thresholds (hysteresis); voter-group approval. Introduces `Cause`, the unit of the "Why did this happen?" report.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/EffectsTests.cs`
- Create: `sim/Statecraft.Sim/Cause.cs`
- Create: `sim/Statecraft.Sim/EffectsSystem.cs`

**Interfaces:**
- Consumes: `Offices.Holder` (Task 7); `GameState` (Task 5).
- Produces: `sealed class Cause { string System; string Target; double Delta; string Message; }`; `EffectsSystem.Init(Content, GameState)`; `EffectsSystem.Multiplier(Content, string office, DateTime) -> double` (Task 10 changes it to take the state); `EffectsSystem.Apply(Content, GameState, List<Cause>)`; `EffectsSystem.UpdateSituations(Content, GameState, List<Cause>)`; `EffectsSystem.Approval(Content, GameState) -> double`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/EffectsTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class EffectsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh(DateTime date)
    {
        var s = new GameState { Date = date };
        EffectsSystem.Init(C, s);
        return s;
    }

    [Fact]
    public void InitSetsBasesAndPolicyDefaults()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        Assert.Equal(0.5, s.Indicators["morale"]);
        Assert.Equal(0.0, s.Policies["rationing"]);
    }

    [Fact]
    public void EffectMovesTowardTargetOverInertiaAndScalesByMinisterCompetence()
    {
        // finance_minister on 1948-06-01 is def_two, competence 4 -> multiplier 0.5 + 4/20 = 0.7
        var s = Fresh(new DateTime(1948, 6, 1));
        s.Policies["rationing"] = 1.0;
        var why = new List<Cause>();
        EffectsSystem.Apply(C, s, why);
        // target contribution 0.4 * 1.0 * 0.7 = 0.28, inertia 2 -> half way = 0.14
        Assert.Equal(0.54, s.Indicators["food_supply"], 6);
        EffectsSystem.Apply(C, s, why);
        Assert.Equal(0.61, s.Indicators["food_supply"], 6);
        Assert.Contains(why, w => w.Target == "food_supply" && w.Message.Contains("rationing_food"));
    }

    [Fact]
    public void EffectWithoutOfficeHasNoMultiplierAndIndicatorsClampToUnitRange()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        s.Policies["rationing"] = 5.0; // morale target 0.5 + (-0.2 * 5) = -0.5 -> clamped to 0
        EffectsSystem.Apply(C, s, new List<Cause>());
        Assert.Equal(0.0, s.Indicators["morale"]);
    }

    [Fact]
    public void SituationUsesHysteresis()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        var why = new List<Cause>();
        s.Indicators["food_supply"] = 0.29; EffectsSystem.UpdateSituations(C, s, why);
        Assert.Contains("hunger", s.ActiveSituations);
        s.Indicators["food_supply"] = 0.40; EffectsSystem.UpdateSituations(C, s, why); // between thresholds: stays on
        Assert.Contains("hunger", s.ActiveSituations);
        s.Indicators["food_supply"] = 0.46; EffectsSystem.UpdateSituations(C, s, why);
        Assert.DoesNotContain("hunger", s.ActiveSituations);
        Assert.Contains(why, w => w.Target == "hunger");
    }

    [Fact]
    public void ApprovalIsMeanOfClampedVoterGroupSupport()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        Assert.Equal(0.5, EffectsSystem.Approval(C, s), 6);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'EffectsSystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Cause.cs`:

```csharp
namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        public Cause(string system, string target, double delta, string message)
        { System = system; Target = target; Delta = delta; Message = message; }
        public override string ToString() => $"[{System}] {Message}";
    }
}
```

`sim/Statecraft.Sim/EffectsSystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EffectsSystem
    {
        public static void Init(Content c, GameState s)
        {
            foreach (var i in c.Indicators) s.Indicators[i.Id] = i.Base;
            foreach (var p in c.Policies) s.Policies[p.Id] = p.Default;
            foreach (var e in c.Effects) s.EffectContributions[e.Id] = 0;
        }

        /// <summary>0.5 + competence/20: a 10-competence minister delivers the full effect, a 0 delivers half.</summary>
        public static double Multiplier(Content c, string office, DateTime date)
        {
            if (office == "") return 1.0;
            var holder = Offices.Holder(c, office, date);
            return holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
        }

        public static void Apply(Content c, GameState s, List<Cause> why)
        {
            // Read every source before writing anything, so effect order cannot change results.
            var targets = c.Effects.ToDictionary(e => e.Id, e =>
                e.K * (s.Policies.TryGetValue(e.Source, out var p) ? p : s.Indicators[e.Source]) * Multiplier(c, e.Office, s.Date));
            foreach (var e in c.Effects)
            {
                var old = s.EffectContributions[e.Id];
                var now = old + (targets[e.Id] - old) / e.Inertia;
                s.EffectContributions[e.Id] = now;
                if (Math.Abs(now - old) > 1e-9)
                    why.Add(new Cause("effects", e.Target, now - old, $"{e.Target} {now - old:+0.000;-0.000} ({e.Id})"));
            }
            foreach (var i in c.Indicators)
            {
                var sum = i.Base + c.Effects.Where(e => e.Target == i.Id).Sum(e => s.EffectContributions[e.Id]);
                s.Indicators[i.Id] = Math.Max(0, Math.Min(1, sum));
            }
            UpdateSituations(c, s, why);
        }

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
        {
            foreach (var sit in c.Situations)
            {
                var v = s.Indicators[sit.Indicator];
                bool on = s.ActiveSituations.Contains(sit.Id);
                bool turnOn = sit.OnBelow.HasValue ? v < sit.OnBelow.Value : v > sit.OnAbove!.Value;
                bool turnOff = sit.OffAbove.HasValue ? v > sit.OffAbove.Value : v < sit.OffBelow!.Value;
                if (!on && turnOn) { s.ActiveSituations.Add(sit.Id); why.Add(new Cause("situations", sit.Id, 1, $"{sit.Id} began ({sit.Indicator} = {v:0.00})")); }
                else if (on && turnOff) { s.ActiveSituations.Remove(sit.Id); why.Add(new Cause("situations", sit.Id, -1, $"{sit.Id} ended ({sit.Indicator} = {v:0.00})")); }
            }
        }

        public static double Approval(Content c, GameState s) =>
            c.VoterGroups.Count == 0 ? 0.5 :
            c.VoterGroups.Average(g => Math.Max(0, Math.Min(1, g.Weights.Sum(w => w.Value * s.Indicators[w.Key]))));
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/EffectsTests.cs sim/Statecraft.Sim/Cause.cs sim/Statecraft.Sim/EffectsSystem.cs
git commit -m "feat(sim): effects web with inertia, competence and hysteresis

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 9: Legislature, coalition and elections

Coalition seats come from the government's parties in the legislature sitting on that date (Provisional State Council or Knesset); votes need a majority; coalition stability drifts toward public approval and collapse ends the premiership; new legislatures and governments are reported (with election date).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/PoliticsTests.cs`
- Modify: `sim/Statecraft.Sim/Content.cs`
- Create: `sim/Statecraft.Sim/PoliticsSystem.cs`

**Interfaces:**
- Consumes: `Offices.Legislature/Government` (Task 7); `EffectsSystem.Approval` (Task 8).
- Produces: `Legislature.Elected` (`DateTime?`); `PoliticsSystem.CoalitionSeats(Content, DateTime) -> (int seats, int size)`; `HasMajority(Content, DateTime) -> bool`; `BudgetNeedsVote(Content, DateTime) -> bool`; `Vote(Content, GameState, ref Rng) -> bool`; `UpdateStability(Content, GameState, double yearFraction, List<Cause>)`; `ReportChanges(Content, DateTime from, DateTime to, List<Cause>)`; `const double CollapseBelow = 0.2`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/PoliticsTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class PoliticsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void CoalitionSeatsFollowGovernmentAndLegislatureOnDate()
    {
        Assert.Equal((3, 5), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));   // gov1 [alpha] in council
        Assert.Equal((4, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 1)));  // gov1 [alpha] in house
        Assert.Equal((10, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 10))); // gov2 [alpha,beta]
        Assert.False(PoliticsSystem.HasMajority(C, new DateTime(1949, 3, 1)));
        Assert.True(PoliticsSystem.HasMajority(C, new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void BudgetVoteRequirementIsPerLegislature()
    {
        Assert.False(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1948, 6, 1)));
        Assert.True(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1949, 6, 1)));
    }

    [Fact]
    public void VoteFailsWithoutMajorityAndPassesWithStableMajority()
    {
        var rng = new Rng(1);
        var s = new GameState { Date = new DateTime(1949, 3, 1), CoalitionStability = 1.0 };
        Assert.False(PoliticsSystem.Vote(C, s, ref rng));
        s.Date = new DateTime(1949, 6, 1);
        Assert.True(PoliticsSystem.Vote(C, s, ref rng));
    }

    [Fact]
    public void StabilityMovesTowardApprovalAndCollapseEndsGame()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), CoalitionStability = 1.0 };
        EffectsSystem.Init(C, s); // approval 0.5
        var why = new List<Cause>();
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal(0.75, s.CoalitionStability, 6);
        s.CoalitionStability = 0.1; s.Indicators["morale"] = 0.0;
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal("coalition_collapse", s.GameOver);
    }

    [Fact]
    public void ReportsNewLegislatureAndGovernment()
    {
        var why = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 5, 1), why);
        Assert.Contains(why, w => w.Target == "house");
        Assert.Contains(why, w => w.Target == "gov2");
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'PoliticsSystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Content.cs` (apply this change):

```diff
@@ -39,6 +39,7 @@ namespace Statecraft.Sim
     {
         public string Id { get; set; } = "";
         public string Body { get; set; } = "";
+        public DateTime? Elected { get; set; }
         public DateTime From { get; set; }
         public DateTime? To { get; set; }
         public int Size { get; set; }
```

`sim/Statecraft.Sim/PoliticsSystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class PoliticsSystem
    {
        public const double CollapseBelow = 0.2;

        public static (int seats, int size) CoalitionSeats(Content c, DateTime date)
        {
            var leg = Offices.Legislature(c, date);
            var gov = Offices.Government(c, date);
            if (leg == null || gov == null) return (0, 0);
            return (gov.Coalition.Sum(p => leg.Seats.TryGetValue(p, out var n) ? n : 0), leg.Size);
        }

        public static bool HasMajority(Content c, DateTime date)
        {
            var (seats, size) = CoalitionSeats(c, date);
            return size > 0 && seats * 2 > size;
        }

        public static bool BudgetNeedsVote(Content c, DateTime date) => Offices.Legislature(c, date)?.BudgetApproval ?? false;

        /// <summary>No majority: fails. Majority with stability >= 0.5: passes. Otherwise passes with probability 2 * stability.</summary>
        public static bool Vote(Content c, GameState s, ref Rng rng)
        {
            if (!HasMajority(c, s.Date)) return false;
            return s.CoalitionStability >= 0.5 || rng.NextDouble() < 2 * s.CoalitionStability;
        }

        public static void UpdateStability(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            var old = s.CoalitionStability;
            s.CoalitionStability = old + (EffectsSystem.Approval(c, s) - old) * Math.Min(1.0, yearFraction * 2);
            if (Math.Abs(s.CoalitionStability - old) > 1e-9)
                why.Add(new Cause("politics", "coalition_stability", s.CoalitionStability - old, $"coalition stability {s.CoalitionStability - old:+0.00;-0.00} (public approval)"));
            if (s.CoalitionStability < CollapseBelow && s.GameOver == null)
            {
                s.GameOver = "coalition_collapse";
                why.Add(new Cause("politics", "game_over", 0, "The coalition collapsed. Your premiership is over."));
            }
        }

        public static void ReportChanges(Content c, DateTime from, DateTime to, List<Cause> why)
        {
            var l0 = Offices.Legislature(c, from); var l1 = Offices.Legislature(c, to);
            if (l1 != null && l1 != l0)
            {
                var (seats, size) = CoalitionSeats(c, to);
                var elected = l1.Elected.HasValue ? $", elected {l1.Elected:yyyy-MM-dd}" : "";
                why.Add(new Cause("politics", l1.Id, 0, $"{l1.Body} convened ({l1.Size} seats{elected}); coalition holds {seats}/{size}"));
            }
            var g0 = Offices.Government(c, from); var g1 = Offices.Government(c, to);
            if (g1 != null && g1 != g0)
                why.Add(new Cause("politics", g1.Id, 0, $"{g1.Name} took office ({string.Join(", ", g1.Coalition)})"));
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    38, Skipped:     0, Total:    38`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/PoliticsTests.cs sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/PoliticsSystem.cs
git commit -m "feat(sim): legislature, coalition stability and elections

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 10: Budget, economy and foreign-currency reserves

Tax, spending and reserve call-up costs move the treasury (deficits become debt); exports and imports move foreign-currency reserves, with an `fx_crisis` situation that has hysteresis; budget allocation is validated; a ministry's budget share scales its effects (an equal split is neutral, so Task 8 results are unchanged).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/EconomyTests.cs`
- Create: `sim/Statecraft.Sim/EconomySystem.cs`
- Modify: `sim/Statecraft.Sim/EffectsSystem.cs`

**Interfaces:**
- Consumes: `EffectsSystem` (Task 8); `Rules` (Task 6).
- Produces: `EconomySystem.Init(Content, GameState)`; `EconomySystem.Apply(Content, GameState, double yearFraction, List<Cause>)`; `EconomySystem.Allocate(Content, GameState, Dictionary<string,double>) -> string?` (null = success); `EconomySystem.FxCrisis` ("fx_crisis"); `EffectsSystem.Multiplier(Content, GameState, string office) -> double`; `EffectsSystem.BudgetFactor(GameState, string office) -> double`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/EconomyTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class EconomyTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(C, s);
        s.Population["veterans"] = 1000;
        return s;
    }

    [Fact]
    public void InitCopiesStartValues()
    {
        var s = Fresh();
        Assert.Equal(100, s.Treasury); Assert.Equal(50, s.FxReserves); Assert.Equal(1.0, s.Budget["finance_minister"]);
    }

    [Fact]
    public void QuarterOfTaxSpendingAndTrade()
    {
        var s = Fresh();
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(100 + 0.1 - 0.125, s.Treasury, 9);   // tax 0.0004*1000/4, spend 0.0005*1000/4
        Assert.Equal(50 + 5 - 0.025, s.FxReserves, 9);   // exports 20/4, imports 0.0001*1000/4
    }

    [Fact]
    public void MobilizationCostsTreasuryAndDeficitBecomesDebt()
    {
        var s = Fresh(); s.Treasury = 1; s.Mobilization = 1.0;
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(0, s.Treasury);
        Assert.Equal(10 + 0.025 - 1, s.Debt, 9);
    }

    [Fact]
    public void FxCrisisHasHysteresis()
    {
        var s = Fresh(); var why = new List<Cause>();
        s.FxReserves = -10; EconomySystem.Apply(C, s, 0.25, why);
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 5; EconomySystem.Apply(C, s, 0.25, why);   // 5 + 4.975 < 12.5 (quarter of start): still on
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 20; EconomySystem.Apply(C, s, 0.25, why);
        Assert.DoesNotContain("fx_crisis", s.ActiveSituations);
    }

    [Theory]
    [InlineData("finance_minister", 1.0, null)]
    [InlineData("finance_minister", 0.9, "Budget shares must sum to 1")]
    [InlineData("chief_rabbi", 1.0, "Unknown office 'chief_rabbi'")]
    [InlineData("finance_minister", -1.0, "Budget shares must sum to 1")]
    public void AllocateValidates(string office, double share, string? error)
    {
        var s = Fresh();
        Assert.Equal(error, EconomySystem.Allocate(C, s, new Dictionary<string, double> { [office] = share }));
    }

    [Fact]
    public void BudgetShareScalesEffects()
    {
        var s = Fresh();
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(s, "finance_minister"));   // equal split is neutral
        s.Budget = new Dictionary<string, double> { ["finance_minister"] = 0.0, ["defense_minister"] = 1.0 };
        Assert.Equal(0.5, EffectsSystem.BudgetFactor(s, "finance_minister"));
        Assert.Equal(1.5, EffectsSystem.BudgetFactor(s, "defense_minister"));
    }
}

public class EconomyNegativeShareTests
{
    [Fact]
    public void NegativeShareRejectedEvenIfSumIsOne()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = new GameState();
        EconomySystem.Init(c, s);
        var err = EconomySystem.Allocate(c, s, new System.Collections.Generic.Dictionary<string, double> { ["finance_minister"] = 1.5, ["defense_minister"] = -0.5 });
        Assert.Equal("Budget shares cannot be negative", err);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'EconomySystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/EconomySystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EconomySystem
    {
        public const string FxCrisis = "fx_crisis";

        public static void Init(Content c, GameState s)
        {
            s.Treasury = c.Rules.TreasuryStart;
            s.FxReserves = c.Rules.FxStart;
            s.Budget = new Dictionary<string, double>(c.Rules.BudgetStart);
        }

        public static void Apply(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            var r = c.Rules; var pop = s.TotalPopulation;
            var revenue = r.TaxPerCapitaPerYear * pop * yearFraction;
            var spending = r.SpendingPerCapitaPerYear * pop * yearFraction;
            var mobilization = r.MobilizationCostPerYear * s.Mobilization * yearFraction;
            s.Treasury += revenue - spending - mobilization;
            why.Add(new Cause("economy", "treasury", revenue - spending - mobilization,
                $"treasury {revenue - spending - mobilization:+0.00;-0.00} (tax {revenue:0.00}, spending {spending:0.00}, reserve call-up {mobilization:0.00})"));
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {-s.Treasury:0.00} to cover the deficit"));
                s.Treasury = 0;
            }

            var exports = r.ExportsPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            s.FxReserves += exports - imports;
            why.Add(new Cause("economy", "fx_reserves", exports - imports, $"foreign currency {exports - imports:+0.00;-0.00} (exports {exports:0.00}, imports {imports:0.00})"));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted")); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased")); }
        }

        /// <summary>Returns null on success, or the reason the allocation was rejected.</summary>
        public static string? Allocate(Content c, GameState s, Dictionary<string, double> shares)
        {
            var unknown = shares.Keys.FirstOrDefault(k => c.Offices.All(o => o.Id != k));
            if (unknown != null) return $"Unknown office '{unknown}'";
            if (Math.Abs(shares.Values.Sum() - 1.0) > 1e-6) return "Budget shares must sum to 1";
            if (shares.Values.Any(v => v < 0)) return "Budget shares cannot be negative";
            s.Budget = new Dictionary<string, double>(shares);
            return null;
        }
    }
}
```

`sim/Statecraft.Sim/EffectsSystem.cs` (apply this change):

```diff
@@ -13,19 +13,28 @@ namespace Statecraft.Sim
             foreach (var e in c.Effects) s.EffectContributions[e.Id] = 0;
         }
 
-        /// <summary>0.5 + competence/20: a 10-competence minister delivers the full effect, a 0 delivers half.</summary>
-        public static double Multiplier(Content c, string office, DateTime date)
+        /// <summary>Competence (0.5 + competence/20) times budget factor. No office: 1.0.</summary>
+        public static double Multiplier(Content c, GameState s, string office)
         {
             if (office == "") return 1.0;
-            var holder = Offices.Holder(c, office, date);
-            return holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
+            var holder = Offices.Holder(c, office, s.Date);
+            var competence = holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
+            return competence * BudgetFactor(s, office);
+        }
+
+        /// <summary>An equal split of the budget is neutral (1.0); a ministry's factor runs from 0.5 (no money) to 1.5.</summary>
+        public static double BudgetFactor(GameState s, string office)
+        {
+            if (s.Budget.Count == 0) return 1.0;
+            var share = s.Budget.TryGetValue(office, out var v) ? v : 0.0;
+            return Math.Min(1.5, 0.5 + share * s.Budget.Count / 2.0);
         }
 
         public static void Apply(Content c, GameState s, List<Cause> why)
         {
             // Read every source before writing anything, so effect order cannot change results.
             var targets = c.Effects.ToDictionary(e => e.Id, e =>
-                e.K * (s.Policies.TryGetValue(e.Source, out var p) ? p : s.Indicators[e.Source]) * Multiplier(c, e.Office, s.Date));
+                e.K * (s.Policies.TryGetValue(e.Source, out var p) ? p : s.Indicators[e.Source]) * Multiplier(c, s, e.Office));
             foreach (var e in c.Effects)
             {
                 var old = s.EffectContributions[e.Id];
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/EconomyTests.cs sim/Statecraft.Sim/EconomySystem.cs sim/Statecraft.Sim/EffectsSystem.cs
git commit -m "feat(sim): budget, economy and foreign-currency reserves

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 11: Population and aliyah

Jewish population by origin group. Historical arrivals come from the sourced aliyah series, spread evenly by day across each period (inclusive dates); natural increase applies before arrivals. A test spreads a fixture period across a year end and 29 Feb 1952 and checks consecutive quarters sum to the period total.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/PopulationTests.cs`
- Create: `sim/Statecraft.Sim/PopulationSystem.cs`

**Interfaces:**
- Consumes: `Rules.InitialPopulation`, `Rules.NaturalIncreasePerYear`, `Content.Aliyah` (Task 6).
- Produces: `PopulationSystem.Init(Content, GameState)`; `PopulationSystem.Arrivals(Content, DateTime from, DateTime to) -> Dictionary<string,double>` (window `[from, to)`); `PopulationSystem.Apply(Content, GameState, DateTime to, double yearFraction, List<Cause>)`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/PopulationTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class PopulationTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void ArrivalsAreSpreadByOverlappingDays()
    {
        // Fixture period 1948-05-15..1948-12-31 inclusive = 231 days, 230 from europe.
        var a = PopulationSystem.Arrivals(C, new DateTime(1948, 5, 15), new DateTime(1948, 8, 15)); // 92 days
        Assert.Equal(230.0 * 92 / 231, a["europe"], 9);
        Assert.Equal(0.0, a["asia"], 9);
    }

    [Fact]
    public void WholePeriodSumsToSeriesTotal()
    {
        var a = PopulationSystem.Arrivals(C, new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
        Assert.Equal(230.0, a["europe"], 9);
    }

    [Fact]
    public void NoArrivalsOutsideSeries() =>
        Assert.Empty(PopulationSystem.Arrivals(C, new DateTime(1950, 1, 1), new DateTime(1950, 4, 1)));

    [Fact]
    public void ApplyAddsNaturalIncreaseThenArrivals()
    {
        var s = new GameState { Date = new DateTime(1948, 5, 15) };
        PopulationSystem.Init(C, s);
        var why = new List<Cause>();
        PopulationSystem.Apply(C, s, new DateTime(1948, 8, 15), 0.25, why);
        Assert.Equal(1000 * (1 + 0.017 * 0.25), s.Population["veterans"], 9);
        Assert.Equal(230.0 * 92 / 231, s.Population["europe"], 9);
        Assert.Contains(why, w => w.Target == "europe");
    }

    [Fact]
    public void ArrivalsAcrossLeapDayAndYearEndSumToThePeriodTotal()
    {
        // 1951-12-10..1952-03-20 inclusive = 22 + 31 + 29 (leap year) + 20 = 102 days.
        var aliyah = "series:\n  - from: 1951-12-10\n    to: 1952-03-20\n    origins: { europe: 1000 }\n";
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["aliyah.yaml"] = aliyah }));
        double sum = 0;
        for (var d = new DateTime(1951, 10, 1); d < new DateTime(1952, 7, 1); d = d.AddMonths(3))
            sum += PopulationSystem.Arrivals(c, d, d.AddMonths(3)).TryGetValue("europe", out var v) ? v : 0;
        Assert.Equal(1000.0, sum, 6);
        Assert.Equal(1000.0 * 80 / 102, PopulationSystem.Arrivals(c, new DateTime(1952, 1, 1), new DateTime(1952, 4, 1))["europe"], 6);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'PopulationSystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/PopulationSystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Jewish population by origin group. Historical mode takes arrivals from the sourced aliyah series.</summary>
    public static class PopulationSystem
    {
        public static void Init(Content c, GameState s) => s.Population = new Dictionary<string, double>(c.Rules.InitialPopulation);

        /// <summary>Arrivals in [from, to). Each series period is inclusive of both dates and spread evenly by day.</summary>
        public static Dictionary<string, double> Arrivals(Content c, DateTime from, DateTime to)
        {
            var result = new Dictionary<string, double>();
            foreach (var p in c.Aliyah)
            {
                var pEnd = p.To.AddDays(1);
                var start = from > p.From ? from : p.From;
                var end = to < pEnd ? to : pEnd;
                var overlap = (end - start).TotalDays;
                if (overlap <= 0) continue;
                var share = overlap / (pEnd - p.From).TotalDays;
                foreach (var kv in p.Origins)
                    result[kv.Key] = (result.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value * share;
            }
            return result;
        }

        public static void Apply(Content c, GameState s, DateTime to, double yearFraction, List<Cause> why)
        {
            var rate = c.Rules.NaturalIncreasePerYear * yearFraction;
            foreach (var k in s.Population.Keys.ToList()) s.Population[k] *= 1 + rate;
            foreach (var kv in Arrivals(c, s.Date, to).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Population[kv.Key] = (s.Population.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                if (kv.Value > 0) why.Add(new Cause("population", kv.Key, kv.Value, $"{kv.Value:N0} immigrants arrived from {kv.Key}"));
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:    53`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/PopulationTests.cs sim/Statecraft.Sim/PopulationSystem.cs
git commit -m "feat(sim): population and aliyah from sourced series

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 12: Security escalation ladder

Rungs 1-5 (spec section 3): turn length shortens at rungs 4-5; entering rung 5 starts a UN ceasefire clock that forces de-escalation to 3. Each rung has a minimum reserve call-up (content table), so every rung costs the economy; lowering the rung never demobilises automatically.

**Files:**
- Modify: `sim/Statecraft.Sim.Tests/Fixture.cs`
- Create: `sim/Statecraft.Sim.Tests/SecurityTests.cs`
- Modify: `sim/Statecraft.Sim/Content.cs`
- Modify: `sim/Statecraft.Sim/ContentLoader.cs`
- Create: `sim/Statecraft.Sim/SecuritySystem.cs`

**Interfaces:**
- Consumes: `Rules` (Task 6); `GameState.Mobilization` (Task 5); economy cost of mobilization (Task 10).
- Produces: `Rules.RungMinMobilization` (`List<double>`, exactly 5); `SecuritySystem.TurnLengthFor(int rung) -> TurnLength`; `Init(Content, GameState, List<Cause>)`; `SetRung(Content, GameState, int rung, List<Cause>)`; `SetMobilization(Content, GameState, double, List<Cause>) -> string?`; `Apply(Content, GameState, TurnLength, List<Cause>)`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/Fixture.cs` (apply this change):

```diff
@@ -117,6 +117,7 @@ attention_per_turn: 3
 attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
 rung_attention_step: 0.25
 ceasefire_weeks: 4
+rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
 treasury_start: 100
 fx_start: 50
 tax_per_capita_per_year: 0.0004
```

`sim/Statecraft.Sim.Tests/SecurityTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class SecurityTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Theory]
    [InlineData(1, TurnLength.Quarter)] [InlineData(3, TurnLength.Quarter)]
    [InlineData(4, TurnLength.Month)] [InlineData(5, TurnLength.Week)]
    public void TurnLengthByRung(int rung, TurnLength expected) => Assert.Equal(expected, SecuritySystem.TurnLengthFor(rung));

    [Fact]
    public void StartAtWarSetsCeasefireClockAndCallUp()
    {
        var s = new GameState(); SecuritySystem.Init(C, s, new List<Cause>());
        Assert.Equal(5, s.Rung); Assert.Equal(4, s.CeasefireWeeksLeft); Assert.Equal(0.8, s.Mobilization);
    }

    [Fact]
    public void CeasefireClockForcesDeescalation()
    {
        var s = new GameState(); var why = new List<Cause>();
        SecuritySystem.Init(C, s, why);
        for (int i = 0; i < 3; i++) SecuritySystem.Apply(C, s, TurnLength.Week, why);
        Assert.Equal(5, s.Rung);
        SecuritySystem.Apply(C, s, TurnLength.Week, why);
        Assert.Equal(3, s.Rung); Assert.Equal(0, s.CeasefireWeeksLeft);
        Assert.Contains(why, w => w.Target == "rung" && w.Message.Contains("ceasefire"));
        Assert.Equal(0.8, s.Mobilization); // de-escalation does not demobilize
    }

    [Fact]
    public void EscalationRaisesCallUpFloor()
    {
        var s = new GameState { Rung = 1 }; var why = new List<Cause>();
        SecuritySystem.SetRung(C, s, 4, why);
        Assert.Equal(0.4, s.Mobilization);
        SecuritySystem.SetRung(C, s, 9, why);
        Assert.Equal(5, s.Rung); Assert.Equal(4, s.CeasefireWeeksLeft);
    }

    [Fact]
    public void MobilizationOrdersAreValidatedAndFloored()
    {
        var s = new GameState { Rung = 3 }; var why = new List<Cause>();
        Assert.Equal("Mobilization must be between 0 and 1", SecuritySystem.SetMobilization(C, s, 1.5, why));
        Assert.Null(SecuritySystem.SetMobilization(C, s, 0.0, why));
        Assert.Equal(0.15, s.Mobilization);
    }

    [Fact]
    public void LoaderRequiresFiveRungFloors()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.1]");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("rung_min_mobilization", ex.Field);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'SecuritySystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Content.cs` (apply this change):

```diff
@@ -103,6 +103,7 @@ namespace Statecraft.Sim
         public Dictionary<string, double> AttentionCost { get; set; } = new Dictionary<string, double>();
         public double RungAttentionStep { get; set; }
         public int CeasefireWeeks { get; set; }
+        public List<double> RungMinMobilization { get; set; } = new List<double>();
         public double TreasuryStart { get; set; }
         public double FxStart { get; set; }
         public double TaxPerCapitaPerYear { get; set; }
```

`sim/Statecraft.Sim/ContentLoader.cs` (apply this change):

```diff
@@ -133,6 +133,7 @@ namespace Statecraft.Sim
             foreach (var v in c.VoterGroups)
                 foreach (var k in v.Weights.Keys)
                     if (!indicators.Contains(k)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{k}", "unknown indicator");
+            if (c.Rules.RungMinMobilization.Count != 5) throw new ContentException("rules.yaml", "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
             foreach (var k in c.Rules.BudgetStart.Keys)
                 if (!offices.Contains(k)) throw new ContentException("rules.yaml", $"budget_start.{k}", "unknown office");
             EventRules.Validate(c, offices, indicators);
```

`sim/Statecraft.Sim/SecuritySystem.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>The always-on escalation ladder: 1 routine, 2 wave, 3 limited operation, 4 prolonged conflict, 5 full war.</summary>
    public static class SecuritySystem
    {
        public static TurnLength TurnLengthFor(int rung) =>
            rung >= 5 ? TurnLength.Week : rung == 4 ? TurnLength.Month : TurnLength.Quarter;

        static int Weeks(TurnLength len) => len == TurnLength.Week ? 1 : len == TurnLength.Month ? 4 : 13;

        public static void Init(Content c, GameState s, List<Cause> why) => SetRung(c, s, c.Rules.StartRung, why);

        public static void SetRung(Content c, GameState s, int rung, List<Cause> why)
        {
            rung = Math.Max(1, Math.Min(5, rung));
            if (rung == 5 && s.Rung != 5) s.CeasefireWeeksLeft = c.Rules.CeasefireWeeks;
            if (rung != 5) s.CeasefireWeeksLeft = 0;
            if (rung != s.Rung) why.Add(new Cause("security", "rung", rung - s.Rung, $"security level {s.Rung} -> {rung}"));
            s.Rung = rung;
            RaiseToFloor(c, s, why);
        }

        /// <summary>Returns null on success, or the reason the order was rejected.</summary>
        public static string? SetMobilization(Content c, GameState s, double level, List<Cause> why)
        {
            if (level < 0 || level > 1) return "Mobilization must be between 0 and 1";
            s.Mobilization = level;
            RaiseToFloor(c, s, why);
            return null;
        }

        static void RaiseToFloor(Content c, GameState s, List<Cause> why)
        {
            var floor = c.Rules.RungMinMobilization[s.Rung - 1];
            if (s.Mobilization < floor)
            {
                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, $"reserve call-up raised to {floor:P0}, the minimum for security level {s.Rung}"));
                s.Mobilization = floor;
            }
        }

        public static void Apply(Content c, GameState s, TurnLength len, List<Cause> why)
        {
            if (s.Rung != 5) return;
            s.CeasefireWeeksLeft = Math.Max(0, s.CeasefireWeeksLeft - Weeks(len));
            if (s.CeasefireWeeksLeft == 0)
            {
                why.Add(new Cause("security", "rung", -2, "UN ceasefire imposed: full war ends"));
                SetRung(c, s, 3, why);
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    62, Skipped:     0, Total:    62`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/Fixture.cs sim/Statecraft.Sim.Tests/SecurityTests.cs sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/ContentLoader.cs sim/Statecraft.Sim/SecuritySystem.cs
git commit -m "feat(sim): security escalation ladder with ceasefire clock

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 13: Events engine and sensitive-content rule

Events trigger by date window and indicator conditions (at most 3 new per turn, each once); options apply effects and may set the rung; unanswered events are delegated to the responsible minister (competence decides how well). Load-time rules: placeholders must be offices, targets must exist, and events tagged holocaust/terror/massacre/war-casualties may not reward the treasury (spec section 5.5).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/EventTests.cs`
- Modify: `sim/Statecraft.Sim/Content.cs`
- Modify: `sim/Statecraft.Sim/ContentLoader.cs`
- Modify: `sim/Statecraft.Sim/EventRules.cs`
- Create: `sim/Statecraft.Sim/EventSystem.cs`

**Interfaces:**
- Consumes: `Offices.Fill` (Task 7); `SecuritySystem.SetRung` (Task 12); `Rng` (Task 5).
- Produces: `GameEvent.File` (set by loader); `EventSystem.Trigger(Content, GameState, List<Cause>)`; `EventSystem.Choose(Content, GameState, string eventId, string optionId, List<Cause>) -> string?`; `EventSystem.Delegate(Content, GameState, List<Cause>)`; `EventSystem.Render(Content, GameEvent, DateTime) -> string`; `EventSystem.MaxNewPerTurn = 3`; `EventRules.SensitiveTags`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/EventTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class EventTests
{
    const string Shortage = @"
id: shortage
title: Bread lines
text: '{finance_minister} reports shortages.'
office: finance_minister
from: 1948-06-01
conditions:
  - indicator: food_supply
    max: 0.45
options:
  - id: ration
    text: Ration food
    historical: true
    recommended: true
    effects:
      - target: food_supply
        delta: 0.2
  - id: import
    text: Buy abroad
    effects:
      - target: fx_reserves
        delta: -10
  - id: escalate
    text: Blame the border
    set_rung: 3
";

    static Content Load(Dictionary<string, string> events) => ContentLoader.Load(Fixture.Write(events: events));

    static GameState State(Content c, DateTime d)
    {
        var s = new GameState { Date = d, RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    [Fact]
    public void TriggersWhenDateAndConditionsHoldOnlyOnce()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var why = new List<Cause>();
        var s = State(c, new DateTime(1948, 5, 20));
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // too early
        s.Date = new DateTime(1948, 6, 1);
        EventSystem.Trigger(c, s, why); Assert.Equal(new[] { "shortage" }, s.PendingEvents);
        s.PendingEvents.Clear();
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // fires once
    }

    [Fact]
    public void ConditionBlocksTrigger()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); s.Indicators["food_supply"] = 0.9;
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Empty(s.PendingEvents);
    }

    [Fact]
    public void ChooseAppliesEffectsAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Equal(40, s.FxReserves);
        Assert.Empty(s.PendingEvents);
        Assert.Equal("Event 'shortage' is not waiting for a decision", EventSystem.Choose(c, s, "shortage", "ration", why));
    }

    [Fact]
    public void UnknownOptionIsRejected()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal("Event 'shortage' has no option 'nap'", EventSystem.Choose(c, s, "shortage", "nap", why));
    }

    [Fact]
    public void DelegationIsDeterministicAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        GameState Run()
        {
            var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
            EventSystem.Trigger(c, s, why); EventSystem.Delegate(c, s, why);
            return s;
        }
        var a = Run(); var b = Run();
        Assert.Empty(a.PendingEvents);
        Assert.Equal(a.Hash(), b.Hash());
    }

    [Fact]
    public void RenderFillsOfficeHolder()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        Assert.Equal("Def Two reports shortages.", EventSystem.Render(c, c.Events[0], new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void SensitiveEventCannotRewardTreasury()
    {
        var bad = Shortage.Replace("title: Bread lines", "title: Bread lines\ntags: [massacre]")
                          .Replace("target: fx_reserves\n        delta: -10", "target: treasury\n        delta: 50");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.treasury", ex.Field);
    }

    [Fact]
    public void PlaceholderMustBeAnOffice()
    {
        var bad = Shortage.Replace("{finance_minister}", "{king}");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].text", ex.Field);
    }

    [Fact]
    public void EffectTargetMustExist()
    {
        var bad = Shortage.Replace("target: food_supply", "target: bread");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].options[ration].effects.bread", ex.Field);
    }

    [Fact]
    public void DuplicateEventIdIsAnError()
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["a.yaml"] = Shortage, ["b.yaml"] = Shortage }));
        Assert.Equal("events/b.yaml", ex.File);
        Assert.Equal("events[shortage].id", ex.Field);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'EventSystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Content.cs` (apply this change):

```diff
@@ -1,5 +1,6 @@
 using System;
 using System.Collections.Generic;
+using YamlDotNet.Serialization;
 
 namespace Statecraft.Sim
 {
@@ -161,6 +162,8 @@ namespace Statecraft.Sim
         public List<EventOption> Options { get; set; } = new List<EventOption>();
         public List<Source> Sources { get; set; } = new List<Source>();
         public string Status { get; set; } = "draft";
+        /// <summary>Path relative to the content root, set by the loader (not read from YAML).</summary>
+        [YamlIgnore] public string File { get; set; } = "";
     }
 
     public sealed class Content
```

`sim/Statecraft.Sim/ContentLoader.cs` (apply this change):

```diff
@@ -56,7 +56,12 @@ namespace Statecraft.Sim
             var eventsDir = Path.Combine(contentRoot, "events");
             if (Directory.Exists(eventsDir))
                 foreach (var f in Directory.GetFiles(eventsDir, "*.yaml").OrderBy(p => p, StringComparer.Ordinal))
-                    c.Events.Add(Read<GameEvent>(contentRoot, Path.Combine("events", Path.GetFileName(f))));
+                {
+                    var rel = "events/" + Path.GetFileName(f);
+                    var e = Read<GameEvent>(contentRoot, rel);
+                    e.File = rel;
+                    c.Events.Add(e);
+                }
             Validate(c);
             return c;
         }
```

`sim/Statecraft.Sim/EventRules.cs` (apply this change):

```diff
@@ -1,10 +1,50 @@
 using System.Collections.Generic;
+using System.Linq;
+using System.Text.RegularExpressions;
 
 namespace Statecraft.Sim
 {
-    /// <summary>Load-time checks for events. Extended in the events task.</summary>
+    /// <summary>Load-time checks for events, including the sensitive-content rule.</summary>
     static class EventRules
     {
-        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators) { }
+        public static readonly HashSet<string> SensitiveTags = new HashSet<string> { "holocaust", "terror", "massacre", "war-casualties" };
+        public static readonly HashSet<string> SpecialTargets = new HashSet<string> { "treasury", "fx_reserves" };
+        static readonly Regex Placeholder = new Regex(@"\{([a-z_]+)\}");
+
+        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators)
+        {
+            var ids = new HashSet<string>();
+            foreach (var e in c.Events)
+            {
+                string F(string field) => $"events[{e.Id}].{field}";
+                if (!ids.Add(e.Id)) throw new ContentException(e.File, F("id"), "duplicate event id");
+                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException(e.File, F("office"), $"unknown office '{e.Office}'");
+                CheckPlaceholders(e.File, F("title"), e.Title, offices);
+                CheckPlaceholders(e.File, F("text"), e.Text, offices);
+                foreach (var cond in e.Conditions)
+                    if (!indicators.Contains(cond.Indicator)) throw new ContentException(e.File, F($"conditions.{cond.Indicator}"), "unknown indicator");
+                if (e.Options.Count == 0) throw new ContentException(e.File, F("options"), "at least one option required");
+                if (e.Options.Count(o => o.Historical) > 1) throw new ContentException(e.File, F("options"), "at most one option can be historical");
+                bool sensitive = e.Tags.Any(SensitiveTags.Contains);
+                foreach (var o in e.Options)
+                {
+                    CheckPlaceholders(e.File, F($"options[{o.Id}].text"), o.Text, offices);
+                    if (o.SetRung.HasValue && (o.SetRung < 1 || o.SetRung > 5)) throw new ContentException(e.File, F($"options[{o.Id}].set_rung"), "must be 1-5");
+                    foreach (var fx in o.Effects)
+                    {
+                        if (!indicators.Contains(fx.Target) && !SpecialTargets.Contains(fx.Target))
+                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), "unknown target");
+                        if (sensitive && fx.Target == "treasury" && fx.Delta > 0)
+                            throw new ContentException(e.File, F($"options[{o.Id}].effects.treasury"), "sensitive events cannot reward the treasury");
+                    }
+                }
+            }
+        }
+
+        static void CheckPlaceholders(string file, string field, string text, HashSet<string> offices)
+        {
+            foreach (Match m in Placeholder.Matches(text))
+                if (!offices.Contains(m.Groups[1].Value)) throw new ContentException(file, field, $"unknown office placeholder '{m.Value}'");
+        }
     }
 }
```

`sim/Statecraft.Sim/EventSystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EventSystem
    {
        public const int MaxNewPerTurn = 3;

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        public static void Trigger(Content c, GameState s, List<Cause> why)
        {
            var eligible = c.Events
                .Where(e => !s.FiredEvents.Contains(e.Id) && e.From <= s.Date && (e.To == null || s.Date < e.To.Value))
                .Where(e => e.Conditions.All(k => (!k.Min.HasValue || s.Indicators[k.Indicator] >= k.Min.Value)
                                               && (!k.Max.HasValue || s.Indicators[k.Indicator] <= k.Max.Value)))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .Take(MaxNewPerTurn);
            foreach (var e in eligible)
            {
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Offices.Fill(c, e.Title, s.Date)}"));
            }
        }

        /// <summary>Returns null on success, or the reason the choice was rejected.</summary>
        public static string? Choose(Content c, GameState s, string eventId, string optionId, List<Cause> why)
        {
            if (!s.PendingEvents.Contains(eventId)) return $"Event '{eventId}' is not waiting for a decision";
            var option = Get(c, eventId).Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null) return $"Event '{eventId}' has no option '{optionId}'";
            Resolve(c, s, eventId, option, "chosen", why);
            return null;
        }

        static void Resolve(Content c, GameState s, string eventId, EventOption o, string how, List<Cause> why)
        {
            s.PendingEvents.Remove(eventId);
            why.Add(new Cause("events", eventId, 0, $"{how}: {Offices.Fill(c, o.Text, s.Date)}"));
            foreach (var fx in o.Effects)
            {
                if (fx.Target == "treasury") s.Treasury += fx.Delta;
                else if (fx.Target == "fx_reserves") s.FxReserves += fx.Delta;
                else s.Indicators[fx.Target] = Math.Max(0, Math.Min(1, s.Indicators[fx.Target] + fx.Delta));
                why.Add(new Cause("events", fx.Target, fx.Delta, $"{fx.Target} {fx.Delta:+0.00;-0.00} ({eventId}/{o.Id})"));
            }
            if (o.SetRung.HasValue) SecuritySystem.SetRung(c, s, o.SetRung.Value, why);
        }

        /// <summary>Unanswered events go to the responsible minister: with probability competence/10 they pick the
        /// recommended option (else the first), otherwise a random option.</summary>
        public static void Delegate(Content c, GameState s, List<Cause> why)
        {
            var rng = new Rng(s.RngState);
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                var minister = e.Office == "" ? null : Offices.Holder(c, e.Office, s.Date);
                var competence = (minister?.Competence ?? 5) / 10.0;
                var option = rng.NextDouble() < competence
                    ? e.Options.FirstOrDefault(o => o.Recommended) ?? e.Options[0]
                    : e.Options[(int)(rng.NextDouble() * e.Options.Count)];
                Resolve(c, s, id, option, $"delegated to {minister?.Name ?? "the cabinet"}", why);
            }
            s.RngState = rng.State;
        }

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, date);
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    72, Skipped:     0, Total:    72`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/EventTests.cs sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/ContentLoader.cs sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Sim/EventSystem.cs
git commit -m "feat(sim): events engine, delegation and sensitive-content rule

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 14: Attention

The PM's per-turn capacity (spec section 4): every command costs attention, costs rise 25% per security rung above 1, and the loader requires a cost for exactly the known command kinds.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/AttentionTests.cs`
- Create: `sim/Statecraft.Sim/AttentionSystem.cs`
- Modify: `sim/Statecraft.Sim/ContentLoader.cs`

**Interfaces:**
- Consumes: `Rules.AttentionPerTurn`, `Rules.AttentionCost`, `Rules.RungAttentionStep` (Task 6).
- Produces: `AttentionSystem.CommandKinds`; `Refill(Content, GameState)`; `Cost(Content, GameState, string kind) -> double`; `TrySpend(Content, GameState, string kind) -> string?`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/AttentionTests.cs`:

```csharp
using System.Collections.Generic;
using Statecraft.Sim;

public class AttentionTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void CostRisesWithSecurityLevel()
    {
        var s = new GameState { Rung = 1 };
        Assert.Equal(1.0, AttentionSystem.Cost(C, s, "set_policy"));
        s.Rung = 5;
        Assert.Equal(2.0, AttentionSystem.Cost(C, s, "set_policy")); // 1 * (1 + 0.25 * 4)
    }

    [Fact]
    public void SpendingStopsWhenAttentionRunsOut()
    {
        var s = new GameState { Rung = 5 };
        AttentionSystem.Refill(C, s);                 // 3
        Assert.Null(AttentionSystem.TrySpend(C, s, "set_policy"));   // 3 - 2 = 1
        Assert.Equal("Not enough attention (need 2.0, have 1.0)", AttentionSystem.TrySpend(C, s, "set_policy"));
        Assert.Equal(1.0, s.Attention);
    }

    [Fact]
    public void LoaderRejectsUnknownCommandCost()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("set_policy: 1,", "set_polcy: 1,");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("attention_cost.set_polcy", ex.Field);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0103: The name 'AttentionSystem' does not exist in the current context`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/AttentionSystem.cs`:

```csharp
using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>The PM's per-turn capacity. Each command costs attention; costs rise with the security level.</summary>
    public static class AttentionSystem
    {
        public static readonly HashSet<string> CommandKinds = new HashSet<string>
            { "set_policy", "allocate_budget", "choose_event_option", "set_mobilization", "security_order" };

        public static void Refill(Content c, GameState s) => s.Attention = c.Rules.AttentionPerTurn;

        public static double Cost(Content c, GameState s, string kind) =>
            c.Rules.AttentionCost[kind] * (1 + c.Rules.RungAttentionStep * (s.Rung - 1));

        /// <summary>Returns null and deducts on success, or the reason it could not be afforded.</summary>
        public static string? TrySpend(Content c, GameState s, string kind)
        {
            var cost = Cost(c, s, kind);
            if (cost > s.Attention + 1e-9)
                return string.Format(CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s.Attention);
            s.Attention -= cost;
            return null;
        }
    }
}
```

`sim/Statecraft.Sim/ContentLoader.cs` (apply this change):

```diff
@@ -138,6 +138,10 @@ namespace Statecraft.Sim
             foreach (var v in c.VoterGroups)
                 foreach (var k in v.Weights.Keys)
                     if (!indicators.Contains(k)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{k}", "unknown indicator");
+            foreach (var k in c.Rules.AttentionCost.Keys)
+                if (!AttentionSystem.CommandKinds.Contains(k)) throw new ContentException("rules.yaml", $"attention_cost.{k}", "unknown command kind");
+            foreach (var k in AttentionSystem.CommandKinds)
+                if (!c.Rules.AttentionCost.ContainsKey(k)) throw new ContentException("rules.yaml", $"attention_cost.{k}", "missing cost");
             if (c.Rules.RungMinMobilization.Count != 5) throw new ContentException("rules.yaml", "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
             foreach (var k in c.Rules.BudgetStart.Keys)
                 if (!offices.Contains(k)) throw new ContentException("rules.yaml", $"budget_start.{k}", "unknown office");
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    75, Skipped:     0, Total:    75`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/AttentionTests.cs sim/Statecraft.Sim/AttentionSystem.cs sim/Statecraft.Sim/ContentLoader.cs
git commit -m "feat(sim): attention budget per turn

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 15: Commands, turn pipeline, historical AI and determinism

The public API the Unity layer will call. `Step` never mutates its input; it applies commands in order (attention first, refunded on rejection, each rejection carries a reason), delegates unanswered events, then resolves the turn in a fixed system order. The determinism test plays 20 turns twice and compares hashes.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/SimTests.cs`
- Create: `sim/Statecraft.Sim/Commands.cs`
- Create: `sim/Statecraft.Sim/HistoricalAI.cs`
- Create: `sim/Statecraft.Sim/Simulation.cs`

**Interfaces:**
- Consumes: every system from Tasks 7-14.
- Produces: `abstract class Command { string Kind }` with `SetPolicy(string policy, double level)`, `AllocateBudget(Dictionary<string,double>)`, `ChooseEventOption(string eventId, string optionId)`, `SetMobilization(double)`, `SecurityOrder(int delta)`; `Rejection { Command; string Reason }`; `StepResult { GameState State; IReadOnlyList<Cause> Why; IReadOnlyList<Rejection> Rejected }`; `Simulation.NewGame(Content, ulong seed, Mode) -> GameState` (Open throws `NotSupportedException` until M4); `Simulation.Step(Content, GameState, IReadOnlyList<Command>) -> StepResult`; `HistoricalAI.Commands(Content, GameState) -> IReadOnlyList<Command>`.

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/SimTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class SimTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void NewGameStartsAtRulesStartAtWar()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(new DateTime(1948, 5, 15), s.Date);
        Assert.Equal(5, s.Rung);
        Assert.Equal(3, s.Attention);
        Assert.Equal(1000, s.TotalPopulation);
    }

    [Fact]
    public void OpenModeIsNotAvailableYet() =>
        Assert.Throws<NotSupportedException>(() => Simulation.NewGame(C, 1, Mode.Open));

    [Fact]
    public void StepAdvancesByRungTurnLengthAndExplainsItself()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, None);
        Assert.Equal(new DateTime(1948, 5, 22), r.State.Date);   // rung 5 = weekly
        Assert.Equal(1, r.State.Turn);
        Assert.Contains(r.Why, w => w.System == "population");
        Assert.Contains(r.Why, w => w.System == "economy");
    }

    [Fact]
    public void StepDoesNotMutateItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s0.Hash();
        Simulation.Step(C, s0, new Command[] { new SetMobilization(1.0) });
        Assert.Equal(before, s0.Hash());
    }

    [Fact]
    public void InvalidCommandsAreRejectedWithReasonsAndAttentionRefunded()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("curfew", 1), new SetMobilization(2) });
        Assert.Equal(new[] { "Unknown policy 'curfew'", "Mobilization must be between 0 and 1" }, r.Rejected.Select(x => x.Reason));
    }

    [Fact]
    public void AttentionLimitsCommandsPerTurn()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // attention 3, rung 5 -> each command costs 2
        var r = Simulation.Step(C, s0, new Command[] { new SetMobilization(0.9), new SetMobilization(1.0) });
        Assert.Single(r.Rejected);
        Assert.StartsWith("Not enough attention", r.Rejected[0].Reason);
    }

    [Fact]
    public void SecurityOrderChangesRung()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-2) });
        Assert.Equal(3, r.State.Rung);
        Assert.Equal(new DateTime(1948, 8, 15), r.State.Date);  // now quarterly
    }

    [Fact]
    public void GameOverRejectsEverything()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical); s0.GameOver = "coalition_collapse";
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-1) });
        Assert.Equal("The game is over: coalition_collapse", r.Rejected[0].Reason);
        Assert.Equal(s0.Hash(), r.State.Hash());
    }

    [Fact]
    public void SameSeedAndCommandsGiveIdenticalHashes()
    {
        string Play(ulong seed)
        {
            var s = Simulation.NewGame(C, seed, Mode.Historical);
            for (int i = 0; i < 20; i++)
                s = Simulation.Step(C, s, i == 3 ? new Command[] { new SecurityOrder(-3) } : HistoricalAI.Commands(C, s)).State;
            return s.Hash();
        }
        Assert.Equal(Play(7), Play(7));
        Assert.NotEqual(Play(7), Play(8));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS0246: The type or namespace name 'Command' could not be found (are you missing a using directive or an assembly reference?)`

- [ ] **Step 3: Implement**

`sim/Statecraft.Sim/Commands.cs`:

```csharp
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>A player (or AI) order for one turn. Kind keys rules.yaml attention_cost.</summary>
    public abstract class Command { public abstract string Kind { get; } }

    public sealed class SetPolicy : Command
    {
        public override string Kind => "set_policy";
        public string Policy { get; } public double Level { get; }
        public SetPolicy(string policy, double level) { Policy = policy; Level = level; }
    }

    public sealed class AllocateBudget : Command
    {
        public override string Kind => "allocate_budget";
        public Dictionary<string, double> Shares { get; }
        public AllocateBudget(Dictionary<string, double> shares) { Shares = shares; }
    }

    public sealed class ChooseEventOption : Command
    {
        public override string Kind => "choose_event_option";
        public string EventId { get; } public string OptionId { get; }
        public ChooseEventOption(string eventId, string optionId) { EventId = eventId; OptionId = optionId; }
    }

    public sealed class SetMobilization : Command
    {
        public override string Kind => "set_mobilization";
        public double Level { get; }
        public SetMobilization(double level) { Level = level; }
    }

    public sealed class SecurityOrder : Command
    {
        public override string Kind => "security_order";
        public int Delta { get; }
        public SecurityOrder(int delta) { Delta = delta; }
    }

    public sealed class Rejection
    {
        public Command Command { get; } public string Reason { get; }
        public Rejection(Command command, string reason) { Command = command; Reason = reason; }
    }

    public sealed class StepResult
    {
        public GameState State { get; }
        public IReadOnlyList<Cause> Why { get; }
        public IReadOnlyList<Rejection> Rejected { get; }
        public StepResult(GameState state, IReadOnlyList<Cause> why, IReadOnlyList<Rejection> rejected) { State = state; Why = why; Rejected = rejected; }
    }
}
```

`sim/Statecraft.Sim/HistoricalAI.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: picks each pending event's historical option.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s) =>
            s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Command)new ChooseEventOption(e.Id, (e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0]).Id))
                .ToList();
    }
}
```

`sim/Statecraft.Sim/Simulation.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class Simulation
    {
        public static GameState NewGame(Content c, ulong seed, Mode mode)
        {
            if (mode == Mode.Open) throw new NotSupportedException("Open mode arrives with milestone M4.");
            var s = new GameState { Date = c.Rules.Start, RngState = seed, Mode = mode };
            var why = new List<Cause>();
            EffectsSystem.Init(c, s);
            EconomySystem.Init(c, s);
            PopulationSystem.Init(c, s);
            SecuritySystem.Init(c, s, why);
            AttentionSystem.Refill(c, s);
            EventSystem.Trigger(c, s, why);
            return s;
        }

        /// <summary>Applies commands in order, delegates unanswered events, then advances one turn.
        /// Never mutates <paramref name="state"/>.</summary>
        public static StepResult Step(Content c, GameState state, IReadOnlyList<Command> commands)
        {
            var s = state.Clone();
            var why = new List<Cause>();
            var rejected = new List<Rejection>();
            if (s.GameOver != null)
            {
                foreach (var cmd in commands) rejected.Add(new Rejection(cmd, $"The game is over: {s.GameOver}"));
                return new StepResult(s, why, rejected);
            }

            foreach (var cmd in commands)
            {
                var before = s.Attention;
                var error = AttentionSystem.TrySpend(c, s, cmd.Kind) ?? Execute(c, s, cmd, why);
                if (error != null) { s.Attention = before; rejected.Add(new Rejection(cmd, error)); }
            }
            EventSystem.Delegate(c, s, why);

            var len = SecuritySystem.TurnLengthFor(s.Rung);
            var next = SimTime.Advance(s.Date, len);
            var yf = SimTime.YearFraction(len);
            EffectsSystem.Apply(c, s, why);
            PopulationSystem.Apply(c, s, next, yf, why);
            EconomySystem.Apply(c, s, yf, why);
            PoliticsSystem.UpdateStability(c, s, yf, why);
            SecuritySystem.Apply(c, s, len, why);
            PoliticsSystem.ReportChanges(c, s.Date, next, why);
            s.Date = next;
            s.Turn++;
            AttentionSystem.Refill(c, s);
            EventSystem.Trigger(c, s, why);
            return new StepResult(s, why, rejected);
        }

        static string? Execute(Content c, GameState s, Command cmd, List<Cause> why)
        {
            switch (cmd)
            {
                case SetPolicy p:
                    if (c.Policies.All(x => x.Id != p.Policy)) return $"Unknown policy '{p.Policy}'";
                    if (p.Level < 0 || p.Level > 1) return "Policy level must be between 0 and 1";
                    if (!PassVote(c, s)) return $"{Offices.Legislature(c, s.Date)?.Body ?? "The legislature"} voted it down";
                    s.Policies[p.Policy] = p.Level;
                    why.Add(new Cause("policy", p.Policy, p.Level, $"{p.Policy} set to {p.Level:0.00}"));
                    return null;
                case AllocateBudget b:
                    if (PoliticsSystem.BudgetNeedsVote(c, s.Date) && !PassVote(c, s))
                        return $"{Offices.Legislature(c, s.Date)!.Body} voted the budget down";
                    return EconomySystem.Allocate(c, s, b.Shares);
                case ChooseEventOption e:
                    return EventSystem.Choose(c, s, e.EventId, e.OptionId, why);
                case SetMobilization m:
                    return SecuritySystem.SetMobilization(c, s, m.Level, why);
                case SecurityOrder o:
                    SecuritySystem.SetRung(c, s, s.Rung + o.Delta, why);
                    return null;
                default:
                    return $"Unknown command {cmd.Kind}";
            }
        }

        static bool PassVote(Content c, GameState s)
        {
            var rng = new Rng(s.RngState);
            var passed = PoliticsSystem.Vote(c, s, ref rng);
            s.RngState = rng.State;
            return passed;
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    84, Skipped:     0, Total:    84`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/SimTests.cs sim/Statecraft.Sim/Commands.cs sim/Statecraft.Sim/HistoricalAI.cs sim/Statecraft.Sim/Simulation.cs
git commit -m "feat(sim): turn pipeline, commands, historical AI, determinism

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 16: Real 1948-49 content

The first real content: 20 offices, 21 parties, 18 people with sourced terms and public-domain portraits, the Provisional State Council (37) and First Knesset (120), both governments, CBS immigration by continent for 1948 and 1949, CBS population anchors, 13 actors and five 1949 events (four armistices, austerity). `open_questions` makes the one unverified field (budget approval, spec section 5) explicit and tested. Facts verified 2026-10-09 against the URLs in the files. Hebrew names and birth/death dates are left empty until verified.

**Files:**
- Create: `content/aliyah.yaml`
- Create: `content/events/armistice_egypt.yaml`
- Create: `content/events/armistice_jordan.yaml`
- Create: `content/events/armistice_lebanon.yaml`
- Create: `content/events/armistice_syria.yaml`
- Create: `content/events/austerity_1949.yaml`
- Create: `content/governments.yaml`
- Create: `content/legislatures.yaml`
- Create: `content/model.yaml`
- Create: `content/offices.yaml`
- Create: `content/parties.yaml`
- Create: `content/people.yaml`
- Create: `content/population.yaml`
- Create: `content/rules.yaml`
- Create: `sim/Statecraft.Sim.Tests/RealContentTests.cs`
- Modify: `sim/Statecraft.Sim/Content.cs`

**Interfaces:**
- Consumes: `ContentLoader`, `Offices`, `PoliticsSystem` (Tasks 6-9).
- Produces: `Legislature.OpenQuestions` (`List<string>`); `content/*.yaml` and `content/events/*.yaml`; test helper `RealContentTests.ContentRoot() -> string` (walks up to the folder containing `content/people.yaml`).

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/RealContentTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Checks the real content/ folder at the repository root.</summary>
public class RealContentTests
{
    public static string ContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "content", "people.yaml"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("content/people.yaml not found above test dir"), "content");
    }

    static readonly Content C = ContentLoader.Load(ContentRoot());
    static readonly DateTime Checked = new DateTime(2026, 10, 9);

    [Fact]
    public void EveryHistoricalRecordIsSourcedAndFactChecked()
    {
        foreach (var p in C.People)
        {
            Assert.True(p.Status == "fact-checked", $"people[{p.Id}] status {p.Status}");
            foreach (var t in p.Offices) Assert.True(t.Sources.Count > 0, $"people[{p.Id}].offices.{t.Office} has no source");
        }
        foreach (var l in C.Legislatures) { Assert.Equal("fact-checked", l.Status); Assert.NotEmpty(l.Sources); }
        foreach (var g in C.Governments) { Assert.Equal("fact-checked", g.Status); Assert.NotEmpty(g.Sources); }
        foreach (var a in C.Aliyah) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var a in C.PopulationAnchors) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var e in C.Events) { Assert.Equal("fact-checked", e.Status); Assert.NotEmpty(e.Sources); }
        var all = C.People.SelectMany(p => p.Offices.SelectMany(t => t.Sources))
            .Concat(C.Legislatures.SelectMany(l => l.Sources)).Concat(C.Governments.SelectMany(g => g.Sources))
            .Concat(C.Aliyah.SelectMany(a => a.Sources)).Concat(C.PopulationAnchors.SelectMany(a => a.Sources))
            .Concat(C.Events.SelectMany(e => e.Sources));
        Assert.All(all, s => { Assert.StartsWith("https://", s.Url); Assert.Equal(Checked, s.Checked); });
    }

    [Fact]
    public void OpenQuestionsAreExactlyTheKnownOnes()
    {
        var open = C.Legislatures.SelectMany(l => l.OpenQuestions.Select(q => $"{l.Id}.{q}")).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "first_knesset.budget_approval", "provisional_state_council.budget_approval" }, open);
    }

    [Fact]
    public void EveryEventTextResolvesOnItsStartDate()
    {
        foreach (var e in C.Events)
        {
            Offices.Fill(C, e.Title, e.From);
            Offices.Fill(C, e.Text, e.From);
            foreach (var o in e.Options) Offices.Fill(C, o.Text, e.From);
        }
    }

    [Fact]
    public void SpotChecksAgainstVerifiedFacts()
    {
        Assert.Equal("ben_gurion", Offices.Holder(C, "defense_minister", new DateTime(1948, 6, 1))!.Id);
        Assert.Equal("dori", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 8))!.Id);
        Assert.Equal("yadin", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 9))!.Id);
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 6, 1))!.Id);
        Assert.Equal((30, 37), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));
        Assert.Equal((73, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 6, 1)));
        Assert.Equal(101828, C.Aliyah[0].Origins.Values.Sum());
        Assert.Equal(239954, C.Aliyah[1].Origins.Values.Sum());
        Assert.Equal(13, C.Actors.Count);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): build fails with `error CS1061: 'Legislature' does not contain a definition for 'OpenQuestions' and no accessible extension method 'OpenQuestions' accepting a first argument of type 'Legislature' could be found (are you missing a using directive or an assembly reference?)`

- [ ] **Step 3: Implement**

`content/aliyah.yaml`:

```yaml
# Immigrants by continent of origin, CBS figures as tabulated by the Jewish Virtual Library.
series:
  - from: 1948-05-15
    to: 1948-12-31
    origins: { asia: 4739, africa: 8192, europe: 76554, america_oceania: 478, unknown: 11865 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-09 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-09 }
    status: fact-checked
  - from: 1949-01-01
    to: 1949-12-31
    # The research notes saw 249,954 in one secondary source; CBS-based tables give 239,954, which is used.
    origins: { asia: 71652, africa: 39215, europe: 121963, america_oceania: 1422, unknown: 5702 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-09 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-09 }
    status: fact-checked
```

`content/events/armistice_egypt.yaml`:

```yaml
id: armistice_egypt
title: Armistice with Egypt
text: 'Talks in Rhodes have produced an armistice agreement with Egypt. {prime_minister} must decide whether Israel signs.'
anchor: hard
office: prime_minister
from: 1949-02-24
options:
  - id: sign
    text: Sign the armistice
    historical: true
    recommended: true
    set_rung: 2
    effects:
      - { target: intl_standing, delta: 0.05 }
  - id: hold_out
    text: Refuse and keep fighting
    effects:
      - { target: intl_standing, delta: -0.10 }
sources:
  - { url: "https://en.wikipedia.org/wiki/1949_Armistice_Agreements", checked: 2026-10-09 }
status: fact-checked
```

`content/events/armistice_jordan.yaml`:

```yaml
id: armistice_jordan
title: Armistice with Jordan
text: 'Negotiators have reached an armistice agreement with Jordan. {prime_minister} must decide whether Israel signs.'
anchor: hard
office: prime_minister
from: 1949-04-03
options:
  - id: sign
    text: Sign the armistice
    historical: true
    recommended: true
    set_rung: 2
    effects:
      - { target: intl_standing, delta: 0.05 }
  - id: hold_out
    text: Refuse and keep fighting
    set_rung: 4
    effects:
      - { target: intl_standing, delta: -0.10 }
sources:
  - { url: "https://en.wikipedia.org/wiki/1949_Armistice_Agreements", checked: 2026-10-09 }
status: fact-checked
```

`content/events/armistice_lebanon.yaml`:

```yaml
id: armistice_lebanon
title: Armistice with Lebanon
text: 'Negotiators have reached an armistice agreement with Lebanon. {prime_minister} must decide whether Israel signs.'
anchor: hard
office: prime_minister
from: 1949-03-23
options:
  - id: sign
    text: Sign the armistice
    historical: true
    recommended: true
    set_rung: 2
    effects:
      - { target: intl_standing, delta: 0.05 }
  - id: hold_out
    text: Refuse and keep fighting
    set_rung: 4
    effects:
      - { target: intl_standing, delta: -0.10 }
sources:
  - { url: "https://en.wikipedia.org/wiki/1949_Armistice_Agreements", checked: 2026-10-09 }
status: fact-checked
```

`content/events/armistice_syria.yaml`:

```yaml
id: armistice_syria
title: Armistice with Syria
text: 'Negotiators have reached an armistice agreement with Syria. {prime_minister} must decide whether Israel signs.'
anchor: hard
office: prime_minister
from: 1949-07-20
options:
  - id: sign
    text: Sign the armistice
    historical: true
    recommended: true
    set_rung: 2
    effects:
      - { target: intl_standing, delta: 0.05 }
  - id: hold_out
    text: Refuse and keep fighting
    set_rung: 4
    effects:
      - { target: intl_standing, delta: -0.10 }
sources:
  - { url: "https://en.wikipedia.org/wiki/1949_Armistice_Agreements", checked: 2026-10-09 }
status: fact-checked
```

`content/events/austerity_1949.yaml`:

```yaml
id: austerity_1949
title: Austerity
text: 'Food and foreign currency cannot keep pace with the immigrants arriving every week. {rationing_minister}, heading the new Ministry of Rationing and Supply, proposes strict rationing.'
anchor: hard
office: rationing_minister
from: 1949-03-10
to: 1950-01-01
options:
  - id: impose
    text: Impose rationing
    historical: true
    recommended: true
    effects:
      - { target: food_supply, delta: 0.15 }
      - { target: morale, delta: -0.05 }
  - id: decline
    text: Keep markets free and import more
    effects:
      - { target: fx_reserves, delta: -8 }
      - { target: food_supply, delta: -0.05 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Austerity_in_Israel", checked: 2026-10-09 }
  - { url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-09 }
status: fact-checked
```

`content/governments.yaml`:

```yaml
governments:
  - id: provisional_government
    name: Provisional Government
    from: 1948-05-14
    to: 1949-03-10
    coalition: [mapai, mapam, general_zionists, new_aliyah, mizrachi, hapoel_hamizrachi, agudat_yisrael, sephardim, ahdut_haavoda_poalei_tzion, poale_zion_left, independent]
    sources:
      - { url: "https://en.wikipedia.org/wiki/Provisional_government_of_Israel", checked: 2026-10-09 }
      - { url: "https://en.wikipedia.org/wiki/Provisional_State_Council", checked: 2026-10-09 }
    status: fact-checked
  - id: first_government
    name: First Government
    # Sources differ (8 vs 10 March 1949); 10 March is the Knesset confidence vote recorded in Ben-Gurion's diary.
    from: 1949-03-10
    coalition: [mapai, united_religious_front, progressive, sephardim, democratic_list_nazareth]
    sources:
      - { url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-09 }
      - { url: "https://bengurionarchive.bgu.ac.il/en/node/14566", checked: 2026-10-09 }
    status: fact-checked
```

`content/legislatures.yaml`:

```yaml
legislatures:
  - id: provisional_state_council
    body: Provisional State Council
    from: 1948-05-14
    to: 1949-02-14
    size: 37
    budget_approval: false
    open_questions: [budget_approval]
    seats:
      mapai: 10
      mapam: 3
      general_zionists: 3
      new_aliyah: 3
      mizrachi: 3
      hapoel_hamizrachi: 2
      agudat_yisrael: 2
      sephardim: 1
      ahdut_haavoda_poalei_tzion: 1
      poale_zion_left: 1
      independent: 1
      hatzohar: 3
      poalei_agudat_yisrael: 1
      maki: 1
      wizo: 1
      yemenite_association: 1
    sources:
      - { url: "https://en.wikipedia.org/wiki/Provisional_State_Council", checked: 2026-10-09 }
    status: fact-checked
  - id: first_knesset
    body: Constituent Assembly (First Knesset)
    elected: 1949-01-25
    from: 1949-02-14
    size: 120
    budget_approval: true
    open_questions: [budget_approval]
    seats:
      mapai: 46
      mapam: 19
      united_religious_front: 16
      herut: 14
      general_zionists: 7
      progressive: 5
      sephardim: 4
      maki: 4
      democratic_list_nazareth: 2
      fighters_list: 1
      wizo: 1
      yemenite_association: 1
    sources:
      - { url: "https://en.wikipedia.org/wiki/1949_Israeli_Constituent_Assembly_election", checked: 2026-10-09 }
      - { url: "https://en.wikipedia.org/wiki/Knesset", checked: 2026-10-09 }
    status: fact-checked
```

`content/model.yaml`:

```yaml
# Game design values (not historical facts): indicators run 0..1.
indicators:
  - { id: morale, base: 0.55 }
  - { id: food_supply, base: 0.40 }
  - { id: housing, base: 0.35 }
  - { id: security, base: 0.30 }
  - { id: intl_standing, base: 0.50 }
policies:
  - { id: rationing, default: 0.0, office: rationing_minister }
  - { id: housing_construction, default: 0.3, office: labour_minister }
  - { id: border_settlement, default: 0.2, office: agriculture_minister }
effects:
  - { id: rationing_feeds, source: rationing, target: food_supply, k: 0.40, inertia: 2, office: rationing_minister }
  - { id: rationing_grates, source: rationing, target: morale, k: -0.15, inertia: 1 }
  - { id: building_houses, source: housing_construction, target: housing, k: 0.35, inertia: 4, office: labour_minister }
  - { id: homes_lift_morale, source: housing, target: morale, k: 0.15, inertia: 2 }
  - { id: border_villages_secure, source: border_settlement, target: security, k: 0.30, inertia: 4, office: agriculture_minister }
  - { id: safety_lifts_morale, source: security, target: morale, k: 0.10, inertia: 2 }
situations:
  - { id: food_crisis, indicator: food_supply, on_below: 0.30, off_above: 0.45 }
  - { id: housing_crisis, indicator: housing, on_below: 0.25, off_above: 0.40 }
voter_groups:
  - { id: veterans, weights: { morale: 0.6, security: 0.4 } }
  - { id: new_immigrants, weights: { housing: 0.5, food_supply: 0.5 } }
  - { id: religious, weights: { morale: 0.7, security: 0.3 } }
  - { id: labour_movement, weights: { morale: 0.5, housing: 0.5 } }
actors:
  - { id: usa, name: United States, standing: 0.6 }
  - { id: ussr, name: Soviet Union, standing: 0.6 }
  - { id: france, name: France, standing: 0.5 }
  - { id: uk, name: United Kingdom, standing: 0.3 }
  - { id: west_germany, name: West Germany, standing: 0.0 }
  - { id: un, name: United Nations, standing: 0.5 }
  - { id: egypt, name: Egypt, standing: 0.0 }
  - { id: jordan, name: Jordan, standing: 0.1 }
  - { id: syria, name: Syria, standing: 0.0 }
  - { id: lebanon, name: Lebanon, standing: 0.1 }
  - { id: iraq, name: Iraq, standing: 0.0 }
  - { id: arab_refugees, name: Arab refugees, standing: 0.0 }
  - { id: arab_citizens, name: Arab citizens of Israel, standing: 0.3 }
```

`content/offices.yaml`:

```yaml
offices:
  - { id: prime_minister, title: Prime Minister }
  - { id: defense_minister, title: Minister of Defense }
  - { id: finance_minister, title: Minister of Finance }
  - { id: foreign_minister, title: Minister of Foreign Affairs }
  - { id: health_minister, title: Minister of Health }
  - { id: immigration_minister, title: Minister of Immigration }
  - { id: interior_minister, title: Minister of Internal Affairs }
  - { id: justice_minister, title: Minister of Justice }
  - { id: agriculture_minister, title: Minister of Agriculture }
  - { id: labour_minister, title: Minister of Labour }
  - { id: police_minister, title: Minister of Police }
  - { id: minority_affairs_minister, title: Minister of Minority Affairs }
  - { id: religions_minister, title: Minister of Religions }
  - { id: war_victims_minister, title: Minister of War Victims }
  - { id: trade_minister, title: Minister of Trade and Industry }
  - { id: transportation_minister, title: Minister of Transportation }
  - { id: welfare_minister, title: Minister of Welfare }
  - { id: education_minister, title: Minister of Education and Culture }
  - { id: rationing_minister, title: Minister of Rationing and Supply }
  - { id: chief_of_staff, title: IDF Chief of the General Staff }
```

`content/parties.yaml`:

```yaml
# Names as listed in the Provisional State Council and 1949 election sources (see legislatures.yaml).
parties:
  - { id: mapai, name: Mapai }
  - { id: mapam, name: Mapam }
  - { id: general_zionists, name: General Zionists }
  - { id: new_aliyah, name: New Aliyah }
  - { id: mizrachi, name: Mizrachi }
  - { id: hapoel_hamizrachi, name: Hapoel HaMizrachi }
  - { id: agudat_yisrael, name: Agudat Yisrael }
  - { id: sephardim, name: Sephardim and Oriental Communities }
  - { id: ahdut_haavoda_poalei_tzion, name: Ahdut HaAvoda-Poalei Tzion }
  - { id: poale_zion_left, name: Poale Zion Left }
  - { id: independent, name: Independent }
  - { id: hatzohar, name: Hatzohar }
  - { id: poalei_agudat_yisrael, name: Poalei Agudat Yisrael }
  - { id: maki, name: Maki }
  - { id: wizo, name: WIZO }
  - { id: yemenite_association, name: Yemenite Association }
  - { id: united_religious_front, name: United Religious Front }
  - { id: herut, name: Herut }
  - { id: progressive, name: Progressive Party }
  - { id: democratic_list_nazareth, name: Democratic List of Nazareth }
  - { id: fighters_list, name: Fighters' List }
```

`content/people.yaml`:

```yaml
# Offices and dates are historical facts (sourced per term). competence/loyalty are game-balance values, not facts.
# Periods are [from, to). Terms without `to` continue beyond the M1 content range (end of 1949).
# Portraits: public-domain files fetched with license check (promo/CREDITS.md).
people:
  - id: ben_gurion
    name: David Ben-Gurion
    party: mapai
    competence: 9
    loyalty: 10
    portrait: { file: promo/ref/people/David_Ben-Gurion.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:David_Ben-Gurion_(D597-087).jpg" }
    offices:
      - { office: prime_minister, from: 1948-05-14, sources: &prov [{ url: "https://en.wikipedia.org/wiki/Provisional_government_of_Israel", checked: 2026-10-09 }] }
      - { office: defense_minister, from: 1948-05-14, sources: *prov }
    status: fact-checked
  - id: zisling
    name: Aharon Zisling
    party: mapam
    competence: 6
    loyalty: 5
    portrait: { file: promo/ref/people/Aharon_Zisling.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Aharon_Zisling.jpg" }
    offices:
      - { office: agriculture_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: kaplan
    name: Eliezer Kaplan
    party: mapai
    competence: 8
    loyalty: 9
    portrait: { file: promo/ref/people/Eliezer_Kaplan.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Eliezer_Kaplan.jpg" }
    offices:
      - { office: finance_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: finance_minister, from: 1949-03-10, sources: &first [{ url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-09 }, { url: "https://bengurionarchive.bgu.ac.il/en/node/14566", checked: 2026-10-09 }] }
      - { office: trade_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: sharett
    name: Moshe Sharett
    party: mapai
    competence: 8
    loyalty: 8
    portrait: { file: promo/ref/people/Moshe_Sharett.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Moshe_Sharett_Portrait.jpg" }
    offices:
      - { office: foreign_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: foreign_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: shapira
    name: Haim-Moshe Shapira
    party: hapoel_hamizrachi
    competence: 7
    loyalty: 7
    portrait: { file: promo/ref/people/Haim-Moshe_Shapira.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Moshe_Shapira.jpg" }
    offices:
      - { office: health_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: immigration_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: health_minister, from: 1949-03-10, sources: *first }
      - { office: immigration_minister, from: 1949-03-10, sources: *first }
      - { office: interior_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: gruenbaum
    name: Yitzhak Gruenbaum
    party: independent
    competence: 6
    loyalty: 6
    portrait: { file: promo/ref/people/Yitzhak_Gruenbaum.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yitzhak_Gruenbaum_1948.jpg" }
    offices:
      - { office: interior_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: rosen
    name: Pinchas Rosen
    party: new_aliyah
    competence: 8
    loyalty: 7
    portrait: { file: promo/ref/people/Pinchas_Rosen.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Pinchas_Rosen.jpg" }
    offices:
      - { office: justice_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: justice_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: bentov
    name: Mordechai Bentov
    party: mapam
    competence: 7
    loyalty: 5
    portrait: { file: promo/ref/people/Mordechai_Bentov.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:MordechaiBentov.jpg" }
    offices:
      - { office: labour_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: sheetrit
    name: Bechor-Shalom Sheetrit
    party: sephardim
    competence: 7
    loyalty: 7
    portrait: { file: promo/ref/people/Bechor-Shalom_Sheetrit.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Bechor-Shalom_Shitreet.jpg" }
    offices:
      - { office: police_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: minority_affairs_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: police_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: maimon
    name: Yehuda Leib Maimon
    party: mizrachi
    competence: 6
    loyalty: 7
    portrait: { file: promo/ref/people/Yehuda_Leib_Maimon.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yehuda_Leib_Maimon.jpg" }
    offices:
      - { office: religions_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: war_victims_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: religions_minister, from: 1949-03-10, sources: *first }
      - { office: war_victims_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: bernstein
    name: Peretz Bernstein
    party: general_zionists
    competence: 7
    loyalty: 6
    portrait: { file: promo/ref/people/Peretz_Bernstein.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Peretz_Bernstein.jpg" }
    offices:
      - { office: trade_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: remez
    name: David Remez
    party: mapai
    competence: 7
    loyalty: 8
    portrait: { file: promo/ref/people/David_Remez.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:David_Remez_(cropped).jpg" }
    offices:
      - { office: transportation_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: transportation_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: levin
    name: Yitzhak-Meir Levin
    party: agudat_yisrael
    competence: 6
    loyalty: 6
    portrait: { file: promo/ref/people/Yitzhak-Meir_Levin.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yitzhak_Meir_Levin.jpg" }
    offices:
      - { office: welfare_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: welfare_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: dov_yosef
    name: Dov Yosef
    party: mapai
    competence: 7
    loyalty: 8
    offices:
      - { office: agriculture_minister, from: 1949-03-10, sources: *first }
      - { office: rationing_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: shazar
    name: Zalman Shazar
    party: mapai
    competence: 7
    loyalty: 8
    offices:
      - { office: education_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: golda_meir
    name: Golda Meir
    party: mapai
    competence: 8
    loyalty: 9
    offices:
      - { office: labour_minister, from: 1949-03-10, sources: *first }
    status: fact-checked
  - id: dori
    name: Yaakov Dori
    competence: 7
    loyalty: 8
    offices:
      - { office: chief_of_staff, from: 1947-06-01, to: 1949-11-09, sources: &cos [{ url: "https://en.wikipedia.org/wiki/Chief_of_the_General_Staff_(Israel)", checked: 2026-10-09 }] }
    status: fact-checked
  - id: yadin
    name: Yigael Yadin
    competence: 8
    loyalty: 8
    offices:
      - { office: chief_of_staff, from: 1949-11-09, sources: *cos }
    status: fact-checked
```

`content/population.yaml`:

```yaml
# Jewish population, CBS Statistical Abstract table 2.1 (thousands, converted).
anchors:
  - date: 1948-11-08
    jews: 716700        # census day
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-09 }]
    status: fact-checked
  - date: 1948-12-31
    jews: 758700        # end of year
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-09 }]
    status: fact-checked
  - date: 1949-12-31
    jews: 1013900       # end of year
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-09 }]
    status: fact-checked
```

`content/rules.yaml`:

```yaml
# Game rules. Values are game balance, except where a comment cites a source.
start: 1948-05-15                 # first day covered by the CBS 1948 immigration series (aliyah.yaml)
start_rung: 5                     # the war is under way when the slice opens
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
rung_attention_step: 0.25
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
ceasefire_weeks: 52               # long enough that the historical armistice events end the war first
treasury_start: 20
fx_start: 40
tax_per_capita_per_year: 0.00012
spending_per_capita_per_year: 0.00016
exports_per_year: 30
imports_per_capita_per_year: 0.00008
mobilization_cost_per_year: 60
# Derived from CBS table 2.1 (1949): (end-1949 1,013.9k - end-1948 758.7k - 1949 immigrants 239,954) / average 1949 population 901.0k.
natural_increase_per_year: 0.0169
# Derived: CBS end-1948 Jewish population 758,700 minus 101,828 immigrants (15 May-31 Dec 1948) minus natural increase over
# 7.5 months at the rate above. Checked against the 8 Nov 1948 census by the calibration test.
initial_population: { veterans: 649400 }
budget_start:
  defense_minister: 0.45
  finance_minister: 0.05
  immigration_minister: 0.15
  labour_minister: 0.10
  health_minister: 0.07
  education_minister: 0.06
  agriculture_minister: 0.07
  interior_minister: 0.05
```

`sim/Statecraft.Sim/Content.cs` (apply this change):

```diff
@@ -46,6 +46,8 @@ namespace Statecraft.Sim
         public int Size { get; set; }
         public bool BudgetApproval { get; set; }
         public Dictionary<string, int> Seats { get; set; } = new Dictionary<string, int>();
+        /// <summary>Fields of this record not yet verified (spec section 5 "open fact"). Kept explicit and tested.</summary>
+        public List<string> OpenQuestions { get; set; } = new List<string>();
         public List<Source> Sources { get; set; } = new List<Source>();
         public string Status { get; set; } = "draft";
     }
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88`

- [ ] **Step 5: Commit**

```bash
git add content/aliyah.yaml content/events/armistice_egypt.yaml content/events/armistice_jordan.yaml content/events/armistice_lebanon.yaml content/events/armistice_syria.yaml content/events/austerity_1949.yaml content/governments.yaml content/legislatures.yaml content/model.yaml content/offices.yaml content/parties.yaml content/people.yaml content/population.yaml content/rules.yaml sim/Statecraft.Sim.Tests/RealContentTests.cs sim/Statecraft.Sim/Content.cs
git commit -m "content: sourced 1948-49 people, legislatures, aliyah, population, events

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 17: Text CLI

A terminal front end: `--auto historical --turns N` plays the historical choices; without `--auto` it reads commands interactively. Output uses the invariant culture so it is identical on every machine. Also records a cause whenever a call-up order is given (found by running the CLI: the order was otherwise silent). Before this task, `dotnet run --project sim/Statecraft.Cli -- --auto historical --turns 2` prints only `Statecraft sim 0.1.0`.

**Files:**
- Modify: `sim/Statecraft.Cli/Program.cs`
- Modify: `sim/Statecraft.Sim/SecuritySystem.cs`

**Interfaces:**
- Consumes: `Simulation`, `HistoricalAI`, `EventSystem.Render`, `Offices` (Tasks 7-16).
- Produces: `dotnet run --project sim/Statecraft.Cli -- [--content DIR] [--seed N] [--auto historical --turns N]`; exit code 2 with `Content error: <file>: <field>: <message>` on bad content.

- [ ] **Step 1: Write the CLI (and the call-up cause)**

`sim/Statecraft.Cli/Program.cs` (apply this change):

```diff
@@ -1 +1,72 @@
-Console.WriteLine($"Statecraft sim {Statecraft.Sim.SimInfo.Version}");
+using System.Globalization;
+using Statecraft.Sim;
+
+// Text front end for the headless sim.
+//   dotnet run --project Statecraft.Cli -- [--content DIR] [--seed N] [--auto historical --turns N]
+// Interactive commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n (end turn) | q
+CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
+CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
+
+string? Arg(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
+
+string FindContent()
+{
+    for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
+        if (File.Exists(Path.Combine(d.FullName, "content", "people.yaml"))) return Path.Combine(d.FullName, "content");
+    throw new DirectoryNotFoundException("content/ not found; pass --content DIR");
+}
+
+Content content;
+try { content = ContentLoader.Load(Arg("--content") ?? FindContent()); }
+catch (ContentException e) { Console.Error.WriteLine($"Content error: {e.Message}"); return 2; }
+
+var state = Simulation.NewGame(content, ulong.Parse(Arg("--seed") ?? "1948"), Mode.Historical);
+bool auto = Arg("--auto") == "historical";
+int turns = int.Parse(Arg("--turns") ?? "1000");
+
+for (int t = 0; t < turns && state.GameOver == null; t++)
+{
+    Briefing(content, state);
+    var commands = auto ? HistoricalAI.Commands(content, state).ToList() : ReadCommands();
+    if (commands == null) break;
+    var result = Simulation.Step(content, state, commands);
+    foreach (var r in result.Rejected) Console.WriteLine($"  rejected: {r.Reason}");
+    foreach (var w in result.Why.Where(w => w.System != "effects" && w.System != "economy")) Console.WriteLine($"  {w}");
+    state = result.State;
+}
+Console.WriteLine($"End {state.Date:yyyy-MM-dd}: population {state.TotalPopulation:N0}, level {state.Rung}, treasury {state.Treasury:0.0}, debt {state.Debt:0.0}, fx {state.FxReserves:0.0}, stability {state.CoalitionStability:0.00}{(state.GameOver != null ? ", GAME OVER: " + state.GameOver : "")}");
+Console.WriteLine($"State hash {state.Hash()}");
+return 0;
+
+static void Briefing(Content c, GameState s)
+{
+    var gov = Offices.Government(c, s.Date);
+    Console.WriteLine($"== {s.Date:yyyy-MM-dd} | turn {s.Turn} | {gov?.Name} | security level {s.Rung} | attention {s.Attention:0.0}");
+    foreach (var id in s.PendingEvents)
+    {
+        var e = c.Events.First(x => x.Id == id);
+        Console.WriteLine($"  EVENT {e.Id}: {Offices.Fill(c, e.Title, s.Date)} - {EventSystem.Render(c, e, s.Date)}");
+        foreach (var o in e.Options) Console.WriteLine($"    {o.Id}: {Offices.Fill(c, o.Text, s.Date)}");
+    }
+}
+
+static List<Command>? ReadCommands()
+{
+    var list = new List<Command>();
+    while (true)
+    {
+        Console.Write("> ");
+        var parts = (Console.ReadLine() ?? "q").Split(' ', StringSplitOptions.RemoveEmptyEntries);
+        if (parts.Length == 0) continue;
+        switch (parts[0])
+        {
+            case "q": return null;
+            case "n": return list;
+            case "c" when parts.Length == 3: list.Add(new ChooseEventOption(parts[1], parts[2])); break;
+            case "p" when parts.Length == 3: list.Add(new SetPolicy(parts[1], double.Parse(parts[2]))); break;
+            case "m" when parts.Length == 2: list.Add(new SetMobilization(double.Parse(parts[1]))); break;
+            case "s" when parts.Length == 2: list.Add(new SecurityOrder(int.Parse(parts[1]))); break;
+            default: Console.WriteLine("commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n | q"); break;
+        }
+    }
+}
```

`sim/Statecraft.Sim/SecuritySystem.cs` (apply this change):

```diff
@@ -28,6 +28,7 @@ namespace Statecraft.Sim
         {
             if (level < 0 || level > 1) return "Mobilization must be between 0 and 1";
             s.Mobilization = level;
+            why.Add(new Cause("security", "mobilization", level, $"reserve call-up set to {level * 100:0}%"));
             RaiseToFloor(c, s, why);
             return null;
         }
@@ -37,7 +38,7 @@ namespace Statecraft.Sim
             var floor = c.Rules.RungMinMobilization[s.Rung - 1];
             if (s.Mobilization < floor)
             {
-                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, $"reserve call-up raised to {floor:P0}, the minimum for security level {s.Rung}"));
+                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, $"reserve call-up raised to {floor * 100:0}%, the minimum for security level {s.Rung}"));
                 s.Mobilization = floor;
             }
         }
```

- [ ] **Step 2: Run the historical autoplay**

Run: `dotnet run --project sim/Statecraft.Cli -- --auto historical --turns 60 | grep -E "EVENT|chosen|security level [0-9] ->|convened|took office|End"`

Expected (verified; middle lines elided):

```
  [politics] Constituent Assembly (First Knesset) convened (120 seats, elected 1949-01-25); coalition holds 76/120
  EVENT armistice_egypt: Armistice with Egypt - Talks in Rhodes have produced an armistice agreement with Egypt. David Ben-Gurion must decide whether Israel signs.
  [events] chosen: Sign the armistice
  [security] security level 5 -> 2
  [politics] First Government took office (mapai, united_religious_front, progressive, sephardim, democratic_list_nazareth)
  EVENT austerity_1949: Austerity - Food and foreign currency cannot keep pace with the immigrants arriving every week. Dov Yosef, heading the new Ministry of Rationing and Supply, proposes strict rationing.
  ...
  EVENT armistice_syria: Armistice with Syria - Negotiators have reached an armistice agreement with Syria. David Ben-Gurion must decide whether Israel signs.
  [events] chosen: Sign the armistice
End 1953-11-26: population 1,082,096, level 2, ...
```

Also run `printf 'm 0.9\nn\nq\n' | dotnet run --project sim/Statecraft.Cli | grep call-up`. Expected: `[security] reserve call-up set to 90%`.

And `dotnet run --project sim/Statecraft.Cli -- --content /nonexistent`. Expected: `Content error: offices.yaml: (file): missing file` (exit code 2).

- [ ] **Step 3: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88`

- [ ] **Step 4: Commit**

```bash
git add sim/Statecraft.Cli/Program.cs sim/Statecraft.Sim/SecuritySystem.cs
git commit -m "feat(cli): text front end with historical autoplay

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 18: Calibration test v1

Plays the historical choices through 1949 and compares Jewish population with the three CBS anchors (8 Nov 1948 census, end-1948, end-1949), interpolating between turns, tolerance 3%. Also asserts all four armistices fire and the war ends (rung 2). The two derived parameters in `rules.yaml` (initial population, natural increase) are documented there with their derivation from CBS table 2.1.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/CalibrationTests.cs`

**Interfaces:**
- Consumes: `Simulation`, `HistoricalAI` (Task 15); real content (Task 16).
- Produces: `CalibrationTests` (tolerance constant `0.03`).

- [ ] **Step 1: Write the failing tests**

`sim/Statecraft.Sim.Tests/CalibrationTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Plays the historical choices and compares the simulation with CBS anchors (content/population.yaml).</summary>
public class CalibrationTests
{
    const double Tolerance = 0.03;
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Jewish population on a date, linearly interpolated between the two turns around it.</summary>
    static double PopulationOn(List<GameState> history, DateTime date)
    {
        var after = history.First(s => s.Date >= date);
        var before = history.Last(s => s.Date <= date);
        if (after.Date == before.Date) return after.TotalPopulation;
        var t = (date - before.Date).TotalDays / (after.Date - before.Date).TotalDays;
        return before.TotalPopulation + t * (after.TotalPopulation - before.TotalPopulation);
    }

    [Fact]
    public void HistoricalPlaythroughMatchesCbsPopulationAnchors()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.Date <= new DateTime(1950, 1, 1))
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            history.Add(s);
        }
        Assert.Null(s.GameOver);
        Assert.Equal(3, C.PopulationAnchors.Count);
        var report = C.PopulationAnchors.Select(a =>
        {
            var sim = PopulationOn(history, a.Date);
            return (a, sim, error: (sim - a.Jews) / a.Jews);
        }).ToList();
        var lines = string.Join("; ", report.Select(r => $"{r.a.Date:yyyy-MM-dd}: sim {r.sim:N0} vs CBS {r.a.Jews:N0} ({r.error * 100:+0.0;-0.0}%)"));
        Assert.True(report.All(r => Math.Abs(r.error) <= Tolerance), lines);
    }

    [Fact]
    public void HistoricalPlaythroughSignsAllArmisticesAndEndsTheWar()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        while (s.Date <= new DateTime(1950, 1, 1)) s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
        Assert.Subset(s.FiredEvents, new SortedSet<string> { "armistice_egypt", "armistice_lebanon", "armistice_jordan", "armistice_syria" });
        Assert.Equal(2, s.Rung);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test sim/Statecraft.sln`

Expected (abbreviated): the new tests PASS immediately (they calibrate existing code). To prove they can fail, temporarily set `const double Tolerance = 0.0;` and run again. Expected failure message (verified): `1948-11-08: sim 733,075 vs CBS 716,700 (+2.3%); 1948-12-31: sim 758,264 vs CBS 758,700 (-0.1%); 1949-12-31: sim 998,533 vs CBS 1,013,900 (-1.5%)`. Then restore `0.03`.

- [ ] **Step 3: Implement**

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test sim/Statecraft.sln`

Expected: `Passed!  - Failed:     0, Passed:    90, Skipped:     0, Total:    90`

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/CalibrationTests.cs
git commit -m "test(sim): calibration against CBS population anchors

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Milestone M2a: map-data tooling (Python), Tasks 19–22

These tasks produce the map data that the Unity map tasks consume. They are all plain Python tools run with `uv`, and every step below was **executed for real** on 2026-10-09 in a scratch copy of the repo. The outputs quoted in the Expected lines are real results, abbreviated.

**Shared test command.** All four tasks use it. Run it from the repo root:

```bash
uv run --quiet --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with requests==2.32.5 --with pillow==12.3.0 --with pyyaml==6.0.3 pytest tools/tests -q
```

**What later tasks rely on.** The Unity map tasks read these files in exactly these formats:

- `unity/Assets/StreamingAssets/map/heightmap.r16`
  - Raw little-endian `uint16` values, 2049 × 2049.
  - **Row 0 is the north edge.**
  - Each value = `round((elev_m - elev_min_m) / (elev_max_m - elev_min_m) * 65535)`.
  - Sea is 0 m before scaling, which encodes as raw value about 10015.
- `unity/Assets/StreamingAssets/map/heightmap.json` holds the size, bounds, real elevation range, physical size and attribution:
  - Fields: `width`, `height`, `lon0`, `lon1`, `lat0`, `lat1`, `elev_min_m`, `elev_max_m`, `width_km`, `length_km`, `attribution`.
  - Real values: elevation range **−427.04 to 2367.42 m**; physical size **161.53 km east–west by 431.22 km north–south**.
- `content/map/hexes.json` holds the hex grid:
  - Layout: `{"bounds":{lon0,lon1,lat0,lat1},"hex_size_km":5.0,"layout":"pointy","hexes":[{q,r,lon,lat,elev_m,land,biome}]}`.
  - Coordinates are Red Blob Games axial, pointy-top. The hex size is the circumradius.
  - Projection: x is km east of `lon0`, y is km **south** of `lat1`. Longitude km use the cosine of the bounds' mid-latitude (31.4°).
  - The real grid has **1,102 hexes**: coastal_plain 108, desert 309, hills 143, lake 16, mountains 251, sea 216, valley 59.
- `content/map/settlements_1948.yaml` lists 25 sourced places, in the shape `{settlements:[{id,name,lon,lat,type,founded,pre_modern?,sources:[{url,checked}]}]}`.
  - `founded` is `null` with `pre_modern: true` for cities that predate the modern era.
  - **Hex ownership rule (for the sim, M3):** a settlement belongs to the hex that contains it, if that hex is land. Otherwise it belongs to the land neighbour whose centre is nearest. This is `hexgrid.settlement_hex`, and Tiberias is the case that needs it. Unity *renders* markers at the sourced lon/lat (Task 25), which is on the real shore.

---

### Task 19: DEM download and heightmap (`tools/dem_to_heightmap.py`)

**Files:**
- Create: `tools/dem_to_heightmap.py`, `tools/tests/conftest.py`, `tools/tests/test_dem_to_heightmap.py`, `tools/README.md`
- Modify: `.gitignore`, appending `tools/.cache/` and `__pycache__/`
- Generated, and committed: `unity/Assets/StreamingAssets/map/heightmap.r16` (8.0 MB) and `heightmap.json`

**Interfaces:**
- Consumes: nothing from earlier tasks. Needs network access to the public Copernicus S3 bucket.
- Produces:
  - `tile_url(lat:int, lon:int) -> str`
  - `tiles_for_bounds(b:dict) -> list[tuple[int,int]]`
  - `fetch_tile(lat, lon, cache=CACHE) -> Path | None`, where `None` means HTTP 404, i.e. open sea
  - `mosaic(paths:list[Path], b:dict, size:int) -> np.ndarray`, float32 with row 0 at the north
  - `write_heightmap(elev, b, out:Path) -> dict`
  - Constants: `BOUNDS`, `SIZE = 2049`, `ATTRIBUTION`
  - The `.r16` and `.json` files described above

- [ ] **Step 1: Ignore caches.** Append these two lines to `.gitignore`:

```
tools/.cache/
__pycache__/
```

- [ ] **Step 2: Write the failing tests.**

`tools/tests/conftest.py`:

```python
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
```

`tools/tests/test_dem_to_heightmap.py`:

```python
import json

import numpy as np
import pytest
import rasterio
from rasterio.transform import from_origin

import dem_to_heightmap as dem


def make_tile(path, lat, lon, value, size=60):
    """1x1 degree synthetic GeoTIFF whose top-left corner is (lon, lat + 1)."""
    data = np.full((size, size), value, dtype="float32")
    with rasterio.open(path, "w", driver="GTiff", height=size, width=size, count=1, dtype="float32",
                       crs="EPSG:4326", transform=from_origin(lon, lat + 1, 1 / size, 1 / size)) as ds:
        ds.write(data, 1)


def test_tile_url_matches_copernicus_naming():
    assert dem.tile_url(31, 34) == (
        "https://copernicus-dem-30m.s3.amazonaws.com/Copernicus_DSM_COG_10_N31_00_E034_00_DEM/"
        "Copernicus_DSM_COG_10_N31_00_E034_00_DEM.tif")


def test_tiles_for_bounds_covers_every_degree_cell():
    assert dem.tiles_for_bounds(dem.BOUNDS) == [(lat, lon) for lat in range(29, 34) for lon in (34, 35)]


def test_missing_tile_is_sea_and_mosaic_keeps_north_up(tmp_path):
    make_tile(tmp_path / "n31e034.tif", 31, 34, 100.0)   # southern tile
    make_tile(tmp_path / "n32e034.tif", 32, 34, -50.0)   # northern tile
    bounds = {"lon0": 34.0, "lon1": 35.0, "lat0": 30.0, "lat1": 33.0}  # lat 30-31 has no tile -> sea
    elev = dem.mosaic([tmp_path / "n31e034.tif", tmp_path / "n32e034.tif"], bounds, 30)
    assert elev.shape == (30, 30)
    assert elev[0, 15] == pytest.approx(-50.0)   # row 0 = north edge (lat 33)
    assert elev[15, 15] == pytest.approx(100.0)  # middle third = lat 31-32
    assert elev[-1, 15] == 0.0                    # no tile: sea level


def test_write_heightmap_normalises_and_writes_metadata(tmp_path):
    elev = np.array([[-430.0, 0.0], [570.0, 1000.0]], dtype="float32")
    meta = dem.write_heightmap(elev, dem.BOUNDS, tmp_path)
    raw = np.fromfile(tmp_path / "heightmap.r16", dtype="<u2").reshape(2, 2)
    assert raw[0, 0] == 0 and raw[1, 1] == 65535
    assert raw[1, 0] == round((570 + 430) / 1430 * 65535)
    on_disk = json.loads((tmp_path / "heightmap.json").read_text())
    assert on_disk == meta
    assert meta["elev_min_m"] == -430.0 and meta["elev_max_m"] == 1000.0
    assert meta["width"] == 2 and meta["height"] == 2
    assert meta["width_km"] == pytest.approx(1.7 * 111.32 * np.cos(np.radians(31.4)), rel=1e-6)
    assert meta["length_km"] == pytest.approx(3.9 * 110.57, rel=1e-6)
    assert "Copernicus" in meta["attribution"]
```

- [ ] **Step 3: Run the tests to verify they fail.**

Run: the shared test command.
Expected: a collection error, `ModuleNotFoundError: No module named 'dem_to_heightmap'`.

- [ ] **Step 4: Implement `tools/dem_to_heightmap.py`.**

How it works:
- The script mosaics every tile with `reproject(..., init_dest_nodata=False)`, so each tile fills only the area it covers.
- Any cell no tile covers stays NaN, and becomes 0 m (sea).
- That is why the missing tile `N33 E034` is safe.

The attribution string is the licence's required notice for *modified* WorldDEM-30 data. Its wording is quoted on the [Earth Engine GLO-30 catalogue page](https://developers.google.com/earth-engine/datasets/catalog/COPERNICUS_DEM_GLO30_2024_1), checked 2026-10-09.

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["rasterio==1.5.2", "numpy==2.5.3", "requests==2.32.5"]
# ///
"""Copernicus GLO-30 DEM -> Unity heightmap (16-bit RAW, row 0 = north) + metadata JSON.

Usage: uv run tools/dem_to_heightmap.py
Tiles are cached in tools/.cache/dem/ (not committed). A tile that returns 404 is open sea."""
import json
import math
from pathlib import Path

import numpy as np
import rasterio
import requests
from rasterio.transform import from_bounds
from rasterio.warp import Resampling, reproject

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "tools" / ".cache" / "dem"
OUT = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
BOUNDS = {"lon0": 34.20, "lon1": 35.90, "lat0": 29.45, "lat1": 33.35}
SIZE = 2049  # Unity Terrain heightmap resolution must be 2^n + 1
KM_PER_DEG_LAT = 110.57
KM_PER_DEG_LON_EQ = 111.32
# Required notice for adapted/modified WorldDEM-30 data (Copernicus DEM licence; wording as quoted on
# developers.google.com/earth-engine/datasets/catalog/COPERNICUS_DEM_GLO30_2024_1, checked 2026-10-09).
ATTRIBUTION = ("produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space "
               "GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved")


def tile_url(lat: int, lon: int) -> str:
    name = f"Copernicus_DSM_COG_10_N{lat:02d}_00_E{lon:03d}_00_DEM"
    return f"https://copernicus-dem-30m.s3.amazonaws.com/{name}/{name}.tif"


def tiles_for_bounds(b: dict) -> list[tuple[int, int]]:
    return [(lat, lon) for lat in range(math.floor(b["lat0"]), math.ceil(b["lat1"]))
            for lon in range(math.floor(b["lon0"]), math.ceil(b["lon1"]))]


def fetch_tile(lat: int, lon: int, cache: Path = CACHE) -> Path | None:
    """Download one tile into the cache; None if it does not exist (404 = all sea)."""
    path = cache / f"N{lat:02d}E{lon:03d}.tif"
    if path.exists():
        return path
    r = requests.get(tile_url(lat, lon), timeout=600)
    if r.status_code == 404:
        return None
    r.raise_for_status()
    cache.mkdir(parents=True, exist_ok=True)
    path.write_bytes(r.content)
    return path


def mosaic(paths: list[Path], b: dict, size: int) -> np.ndarray:
    """Average-resample all tiles onto a size x size lon/lat grid; uncovered cells = 0 m (sea)."""
    dst = np.full((size, size), np.nan, dtype="float32")
    transform = from_bounds(b["lon0"], b["lat0"], b["lon1"], b["lat1"], size, size)
    for p in paths:
        with rasterio.open(p) as src:
            reproject(source=rasterio.band(src, 1), destination=dst, dst_transform=transform,
                      dst_crs="EPSG:4326", dst_nodata=np.nan, init_dest_nodata=False,
                      resampling=Resampling.average)
    return np.nan_to_num(dst, nan=0.0)


def write_heightmap(elev: np.ndarray, b: dict, out: Path) -> dict:
    lo, hi = float(elev.min()), float(elev.max())
    raw = np.round((elev - lo) / (hi - lo) * 65535).astype("<u2")
    out.mkdir(parents=True, exist_ok=True)
    raw.tofile(out / "heightmap.r16")
    meta = {"width": int(elev.shape[1]), "height": int(elev.shape[0]), **b,
            "elev_min_m": lo, "elev_max_m": hi,
            "width_km": (b["lon1"] - b["lon0"]) * KM_PER_DEG_LON_EQ
                        * math.cos(math.radians((b["lat0"] + b["lat1"]) / 2)),
            "length_km": (b["lat1"] - b["lat0"]) * KM_PER_DEG_LAT,
            "attribution": ATTRIBUTION}
    (out / "heightmap.json").write_text(json.dumps(meta, indent=2))
    return meta


def main() -> None:
    paths = []
    for lat, lon in tiles_for_bounds(BOUNDS):
        p = fetch_tile(lat, lon)
        print(f"N{lat} E{lon:03d}: {'sea (404)' if p is None else p.name}")
        if p:
            paths.append(p)
    meta = write_heightmap(mosaic(paths, BOUNDS, SIZE), BOUNDS, OUT)
    print(f"wrote {OUT / 'heightmap.r16'}: elev {meta['elev_min_m']:.1f}..{meta['elev_max_m']:.1f} m, "
          f"{meta['width_km']:.1f} x {meta['length_km']:.1f} km")


if __name__ == "__main__":
    main()
```

- [ ] **Step 5: Run the tests to verify they pass.**

Run: the shared test command.
Expected: `4 passed`.

- [ ] **Step 6: Generate the real heightmap.** This downloads about 320 MB on the first run; later runs use the cache.

Run: `uv run --quiet tools/dem_to_heightmap.py`
Expected:

```
N29 E034: N29E034.tif   … (N29–N32 × E034/E035 all download)
N33 E034: sea (404)
N33 E035: N33E035.tif
wrote …/unity/Assets/StreamingAssets/map/heightmap.r16: elev -427.0..2367.4 m, 161.5 x 431.2 km
```

- [ ] **Step 7: Spot-check real elevations.** This is the geography gate for the raw data.

Run:

```bash
uv run --quiet --with numpy==2.5.3 python - <<'PY'
import json, numpy as np
m = json.load(open('unity/Assets/StreamingAssets/map/heightmap.json')); N = m['width']
raw = np.fromfile('unity/Assets/StreamingAssets/map/heightmap.r16', dtype='<u2').reshape(N, N)
e = m['elev_min_m'] + raw / 65535 * (m['elev_max_m'] - m['elev_min_m'])
def at(lon, lat):
    return e[round((m['lat1'] - lat) / (m['lat1'] - m['lat0']) * (N - 1)), round((lon - m['lon0']) / (m['lon1'] - m['lon0']) * (N - 1))]
for name, lon, lat in [("sea off Tel Aviv", 34.6, 32.1), ("Jerusalem", 35.22, 31.78), ("Sea of Galilee", 35.58, 32.82),
                       ("Dead Sea", 35.5, 31.5), ("Beersheba", 34.79, 31.25), ("Gulf of Aqaba", 34.95, 29.5)]:
    print(f"{name:18s} {at(lon, lat):8.1f}")
PY
```

Expected:
- sea off Tel Aviv `0.0`
- Jerusalem `788.4`
- Sea of Galilee `-214.5`
- Dead Sea `-427.0`
- Beersheba `289.0`
- Gulf of Aqaba `0.0`

Values must be within ±1 m of these. Any other result means the mosaic or the north/south orientation is wrong. Stop and fix it before committing.

- [ ] **Step 8: Add the attribution note.** Create `tools/README.md`:

```markdown
# tools/

Offline data tools (uv inline-script Python). Run from the repo root.

- `dem_to_heightmap.py` downloads Copernicus GLO-30 tiles into `tools/.cache/dem/` (not committed) and writes
  `unity/Assets/StreamingAssets/map/heightmap.r16` + `heightmap.json`.
- `hexgrid.py` writes `content/map/hexes.json` from that heightmap.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.

## Required attribution (Copernicus DEM licence, modified data)

produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

This notice must also appear in the game credits and the Steam page legal notes.
```

- [ ] **Step 9: Commit.**

```bash
git add .gitignore tools/dem_to_heightmap.py tools/README.md tools/tests/conftest.py tools/tests/test_dem_to_heightmap.py \
  unity/Assets/StreamingAssets/map/heightmap.r16 unity/Assets/StreamingAssets/map/heightmap.json
git commit -m "feat(tools): Copernicus DEM to Unity heightmap

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 20: Hex grid with biomes (`tools/hexgrid.py`)

**Files:**
- Create: `tools/hexgrid.py`, `tools/tests/test_hexgrid.py`
- Generated, and committed: `content/map/hexes.json` (100 KB)

**Interfaces:**
- Consumes: from Task 19, `heightmap.r16` and `heightmap.json`.
- Produces:
  - `km_per_deg_lon(meta:dict) -> float`
  - `hex_center_km(q, r, size) -> (x_km, y_km)`
  - `point_to_hex(x:np.ndarray, y:np.ndarray, size) -> (list[int] q, list[int] r)`
  - `water_share(v:np.ndarray) -> float`
  - `classify(stats:dict{sea_share, water_share, median}, lat, lon) -> str`
  - `load_elevation(map_dir) -> (elev ndarray, meta dict)`
  - `build(map_dir, size=5.0) -> dict`, in the `hexes.json` shape
  - Constants: `MAP_DIR`, `KM_PER_DEG_LAT = 110.57`, `HEX_SIZE_KM = 5.0`

**Biome rules.** The first rule that matches wins. Each rule is computed over all heightmap pixels inside the hex:

| # | Biome | Rule |
|---|---|---|
| 1 | sea | ≥ 50% of pixels within 0.5 m of 0 m (Copernicus sea is exactly 0 m) |
| 2 | lake | ≥ 40% "flat water" pixels: below −150 m and within 1.5 m of the hex's most common sub-−150 m level |
| 3 | valley | median < 0 m |
| 4 | mountains | median ≥ 700 m |
| 5 | desert | lat < 31.25, or the Judean Desert strip: 31.25 ≤ lat < 32.0 and 35.28 ≤ lon < 35.60 |
| 6 | hills | median ≥ 200 m |
| 7 | coastal_plain | everything else |

An earlier version of the lake rule used "median < −150 and p90 − p10 < 5 m". It found only 5 lake hexes, missing most of the Dead Sea and half of the Sea of Galilee, because shore cliffs inside a hex break the flatness test. The water-share rule finds 16, and it was checked on the preview in Task 22.

- [ ] **Step 1: Write the failing tests.** Create `tools/tests/test_hexgrid.py`:

```python
import json

import numpy as np
import pytest

import hexgrid as hg


def test_hex_center_round_trips_through_point_to_hex():
    for q, r in [(0, 0), (3, 5), (-2, 7), (10, 40)]:
        x, y = hg.hex_center_km(q, r, 5.0)
        assert hg.point_to_hex(np.array([x]), np.array([y]), 5.0) == ([q], [r])


def test_neighbouring_centres_are_one_hex_width_apart():
    x0, y0 = hg.hex_center_km(0, 0, 5.0)
    x1, y1 = hg.hex_center_km(1, 0, 5.0)
    assert x1 - x0 == pytest.approx(5.0 * 3 ** 0.5) and y1 == y0


@pytest.mark.parametrize("stats, lat, lon, biome", [
    ({"sea_share": 0.6, "median": 0.0, "water_share": 0.0}, 32.0, 34.7, "sea"),
    ({"sea_share": 0.0, "water_share": 0.7, "median": -212.0}, 32.8, 35.6, "lake"),
    ({"sea_share": 0.0, "median": -250.0, "water_share": 0.0}, 32.3, 35.55, "valley"),
    ({"sea_share": 0.0, "median": 780.0, "water_share": 0.0}, 31.78, 35.22, "mountains"),
    ({"sea_share": 0.0, "median": 290.0, "water_share": 0.0}, 31.25 - 0.01, 34.79, "desert"),
    ({"sea_share": 0.0, "median": 300.0, "water_share": 0.0}, 31.6, 35.35, "desert"),
    ({"sea_share": 0.0, "median": 400.0, "water_share": 0.0}, 32.7, 35.3, "hills"),
    ({"sea_share": 0.1, "median": 25.0, "water_share": 0.0}, 32.08, 34.8, "coastal_plain"),
])
def test_biome_rules(stats, lat, lon, biome):
    assert hg.classify(stats, lat, lon) == biome


def test_water_share_counts_flat_water_below_minus_150():
    v = np.r_[np.full(60, -430.0), np.linspace(-400, 200, 40)]   # lake surface + cliffs
    assert hg.water_share(v) == pytest.approx(0.6, abs=0.02)
    assert hg.water_share(np.full(10, 300.0)) == 0.0


def test_build_hexes_on_synthetic_map(tmp_path):
    n = 201
    elev = np.zeros((n, n), dtype="float32")
    elev[:, n // 2:] = 300.0                       # west half sea, east half land at 300 m
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    np.round(elev / 300.0 * 65535).astype("<u2").tofile(tmp_path / "heightmap.r16")
    out = hg.build(tmp_path, 5.0)
    assert out["layout"] == "pointy" and out["hex_size_km"] == 5.0
    assert set(out["hexes"][0]) == {"q", "r", "lon", "lat", "elev_m", "land", "biome"}
    west = [h for h in out["hexes"] if h["lon"] < 35.2]
    east = [h for h in out["hexes"] if h["lon"] > 35.3]
    assert west and all(h["biome"] == "sea" and not h["land"] for h in west)
    assert east and all(h["biome"] == "hills" and h["land"] for h in east)
    assert len({(h["q"], h["r"]) for h in out["hexes"]}) == len(out["hexes"])
```

- [ ] **Step 2: Run the tests to verify they fail.**

Run: the shared test command.
Expected: `ModuleNotFoundError: No module named 'hexgrid'`.

- [ ] **Step 3: Implement `tools/hexgrid.py`.**

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3"]
# ///
"""Heightmap -> hex grid (Red Blob Games axial, pointy-top) with elevation and biome per hex.

Usage: uv run tools/hexgrid.py   (reads unity/Assets/StreamingAssets/map/, writes content/map/hexes.json)
Projection: equirectangular km, x east from lon0, y south from lat1 (so r grows southward).
Biome rules, first match wins (per hex, over the heightmap pixels inside it):
  1 sea           >= 50% of pixels within 0.5 m of 0 m (Copernicus sea is exactly 0 m)
  2 lake          >= 40% of pixels are flat water: below -150 m and within 1.5 m of the hex's most common
                  sub -150 m level (Sea of Galilee ~-210 m, Dead Sea ~-430 m, evaporation ponds)
  3 valley        median < 0 m (Jordan rift / Arava floor)
  4 mountains     median >= 700 m
  5 desert        lat < 31.25 (Negev, Sinai, Arava, Edom foothills) or the Judean Desert strip
                  31.25 <= lat < 32.0 and 35.28 <= lon < 35.60
  6 hills         median >= 200 m
  7 coastal_plain everything else
"""
import json
import math
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT = ROOT / "content" / "map" / "hexes.json"
HEX_SIZE_KM = 5.0  # circumradius (centre to corner)
KM_PER_DEG_LAT = 110.57
KM_PER_DEG_LON_EQ = 111.32
SQRT3 = math.sqrt(3)


def km_per_deg_lon(meta: dict) -> float:
    return KM_PER_DEG_LON_EQ * math.cos(math.radians((meta["lat0"] + meta["lat1"]) / 2))


def hex_center_km(q: int, r: int, size: float) -> tuple[float, float]:
    return size * (SQRT3 * q + SQRT3 / 2 * r), size * 1.5 * r


def point_to_hex(x: np.ndarray, y: np.ndarray, size: float) -> tuple[list[int], list[int]]:
    """Vectorised axial rounding (cube-coordinate round), Red Blob Games 'pixel to hex'."""
    qf = (SQRT3 / 3 * x - y / 3) / size
    rf = (2 / 3 * y) / size
    sf = -qf - rf
    q, r, s = np.round(qf), np.round(rf), np.round(sf)
    dq, dr, ds = np.abs(q - qf), np.abs(r - rf), np.abs(s - sf)
    fix_q = (dq > dr) & (dq > ds)
    fix_r = ~fix_q & (dr > ds)
    q = np.where(fix_q, -r - s, q)
    r = np.where(fix_r, -q - s, r)
    return q.astype(int).tolist(), r.astype(int).tolist()


def water_share(v: np.ndarray) -> float:
    low = np.round(v[v < -150])
    if low.size == 0:
        return 0.0
    vals, counts = np.unique(low, return_counts=True)
    level = vals[np.argmax(counts)]
    return float(np.mean((v < -150) & (np.abs(v - level) < 1.5)))


def classify(st: dict, lat: float, lon: float) -> str:
    if st["sea_share"] >= 0.5:
        return "sea"
    if st["water_share"] >= 0.4:
        return "lake"
    if st["median"] < 0:
        return "valley"
    if st["median"] >= 700:
        return "mountains"
    if lat < 31.25 or (31.25 <= lat < 32.0 and 35.28 <= lon < 35.60):
        return "desert"
    if st["median"] >= 200:
        return "hills"
    return "coastal_plain"


def load_elevation(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = np.fromfile(map_dir / "heightmap.r16", dtype="<u2").reshape(meta["height"], meta["width"])
    elev = meta["elev_min_m"] + raw.astype("float64") / 65535 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev, meta


def build(map_dir: Path, size: float = HEX_SIZE_KM) -> dict:
    elev, meta = load_elevation(map_dir)
    h, w = elev.shape
    width_km = (meta["lon1"] - meta["lon0"]) * km_per_deg_lon(meta)
    length_km = (meta["lat1"] - meta["lat0"]) * KM_PER_DEG_LAT
    ys, xs = np.mgrid[0:h, 0:w]
    q, r = point_to_hex(xs.ravel() / (w - 1) * width_km, ys.ravel() / (h - 1) * length_km, size)
    q, r = np.array(q), np.array(r)
    values = elev.ravel()
    order = np.lexsort((q, r))
    keys = np.stack([r[order], q[order]], axis=1)
    starts = np.flatnonzero(np.r_[True, np.any(keys[1:] != keys[:-1], axis=1)])
    hexes = []
    for a, b in zip(starts, np.r_[starts[1:], len(order)]):
        hr, hq = (int(v) for v in keys[a])
        cx, cy = hex_center_km(hq, hr, size)
        if not (0 <= cx <= width_km and 0 <= cy <= length_km):
            continue  # partial hex on the map edge: centre outside the bounds
        v = values[order[a:b]]
        st = {"sea_share": float(np.mean(np.abs(v) < 0.5)), "water_share": water_share(v),
              "median": float(np.median(v))}
        lon = meta["lon0"] + cx / km_per_deg_lon(meta)
        lat = meta["lat1"] - cy / KM_PER_DEG_LAT
        biome = classify(st, lat, lon)
        hexes.append({"q": hq, "r": hr, "lon": round(lon, 5), "lat": round(lat, 5), "elev_m": round(st["median"], 1),
                      "land": biome not in ("sea", "lake"), "biome": biome})
    bounds = {k: meta[k] for k in ("lon0", "lon1", "lat0", "lat1")}
    return {"bounds": bounds, "hex_size_km": size, "layout": "pointy", "hexes": hexes}


def main() -> None:
    out = build(MAP_DIR)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(out, separators=(",", ":")))
    counts = {}
    for hx in out["hexes"]:
        counts[hx["biome"]] = counts.get(hx["biome"], 0) + 1
    print(f"wrote {OUT}: {len(out['hexes'])} hexes {dict(sorted(counts.items()))}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 4: Run the tests to verify they pass.**

Run: the shared test command.
Expected: `16 passed`.

- [ ] **Step 5: Generate the real grid.**

Run: `uv run --quiet tools/hexgrid.py`
Expected: `wrote …/content/map/hexes.json: 1102 hexes {'coastal_plain': 108, 'desert': 309, 'hills': 143, 'lake': 16, 'mountains': 251, 'sea': 216, 'valley': 59}`

- [ ] **Step 6: Commit.**

```bash
git add tools/hexgrid.py tools/tests/test_hexgrid.py content/map/hexes.json
git commit -m "feat(tools): hex grid with elevation and biome per hex

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 21: 1948 settlements and the placement rule

**Files:**
- Create: `content/map/settlements_1948.yaml`, `tools/tests/test_settlements.py`
- Modify: `tools/hexgrid.py`, adding `NEIGHBOURS`, `lonlat_to_km` and `settlement_hex`

**Interfaces:**
- Consumes: from Task 20, `content/map/hexes.json` and `point_to_hex`, `hex_center_km`, `km_per_deg_lon`, `KM_PER_DEG_LAT`.
- Produces:
  - `lonlat_to_km(lon, lat, bounds) -> (x_km, y_km)`
  - `settlement_hex(lon, lat, grid:dict) -> (q, r)`, which raises `ValueError` if there is no land at or next to the point
  - `NEIGHBOURS`, the six axial direction vectors
  - `settlements_1948.yaml`

**How the facts were verified.** This was done on 2026-10-09 for all 25 entries.
- **Coordinates:** taken from the Wikipedia API (`prop=coordinates`, `colimit=max`), not typed by hand.
- **Founding years:** taken from the article text, for example:
  - "founded in 1909" (Tel Aviv)
  - "founded on 31 July 1882" (Rishon LeZion)
  - "established in 1890" (Rehovot)
  - "Founded in 1929" (Netanya)
  - "established in 1891" (Hadera)
  - "arrived … September 11, 1921" (Nahalal)
  - "established on 21 March 1938" (Hanita)
  - "established as Gei Oni in 1878" (Rosh Pinna)
  - "founded in 1882" (Zikhron Ya'akov)
  - "established on 7 July 1943" (Revivim)
  - "moved to its site near Ashkelon in December 1943" (Yad Mordechai)
  - "founded in 1910" (Degania Alef)
  - "Founded in 1921" (Ein Harod)
  - "founded in 1878" (Petah Tikva)
  - "around 1900" (Beersheba, as an Ottoman town)

  Ramla ("early 8th century") and the ancient cities use `founded: null` with `pre_modern: true`.
- **Names:** these follow the spec's Israeli-perspective rule.

- [ ] **Step 1: Create the data file** `content/map/settlements_1948.yaml`, exactly as below:

```yaml
# Key places that existed in May 1948 (M2 map seed). Coordinates: Wikipedia API prop=coordinates;
# founding years: the cited article's text. founded: null + pre_modern: true = pre-19th-century city.
# Naming follows the spec's Israeli-perspective rule.
settlements:
- id: jerusalem
  name: Jerusalem
  lon: 35.2256
  lat: 31.7789
  type: city
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Jerusalem
    checked: '2026-10-09'
- id: tel_aviv
  name: Tel Aviv
  lon: 34.78
  lat: 32.08
  type: city
  founded: 1909
  sources:
  - url: https://en.wikipedia.org/wiki/Tel_Aviv
    checked: '2026-10-09'
- id: jaffa
  name: Jaffa
  lon: 34.7531
  lat: 32.0522
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Jaffa
    checked: '2026-10-09'
- id: haifa
  name: Haifa
  lon: 34.9992
  lat: 32.8192
  type: city
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Haifa
    checked: '2026-10-09'
- id: acre
  name: Acre
  lon: 35.0817
  lat: 32.9278
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Acre,_Israel
    checked: '2026-10-09'
- id: tiberias
  name: Tiberias
  lon: 35.5333
  lat: 32.7944
  type: town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Tiberias
    checked: '2026-10-09'
- id: safed
  name: Safed
  lon: 35.4983
  lat: 32.9658
  type: town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Safed
    checked: '2026-10-09'
- id: nazareth
  name: Nazareth
  lon: 35.3033
  lat: 32.7019
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Nazareth
    checked: '2026-10-09'
- id: lod
  name: Lod
  lon: 34.8881
  lat: 31.9519
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Lod
    checked: '2026-10-09'
- id: ramla
  name: Ramla
  lon: 34.8625
  lat: 31.9275
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Ramla
    checked: '2026-10-09'
- id: gaza
  name: Gaza
  lon: 34.4597
  lat: 31.5075
  type: arab_town
  founded: null
  pre_modern: true
  sources:
  - url: https://en.wikipedia.org/wiki/Gaza_City
    checked: '2026-10-09'
- id: beersheba
  name: Beersheba
  lon: 34.7867
  lat: 31.2522
  type: arab_town
  founded: 1900
  sources:
  - url: https://en.wikipedia.org/wiki/Beersheba
    checked: '2026-10-09'
- id: petah_tikva
  name: Petah Tikva
  lon: 34.8864
  lat: 32.0889
  type: town
  founded: 1878
  sources:
  - url: https://en.wikipedia.org/wiki/Petah_Tikva
    checked: '2026-10-09'
- id: rishon_lezion
  name: Rishon LeZion
  lon: 34.8
  lat: 31.95
  type: town
  founded: 1882
  sources:
  - url: https://en.wikipedia.org/wiki/Rishon_LeZion
    checked: '2026-10-09'
- id: zikhron_yaakov
  name: Zikhron Ya'akov
  lon: 34.9517
  lat: 32.5708
  type: town
  founded: 1882
  sources:
  - url: https://en.wikipedia.org/wiki/Zikhron_Ya'akov
    checked: '2026-10-09'
- id: rosh_pinna
  name: Rosh Pinna
  lon: 35.5422
  lat: 32.97
  type: town
  founded: 1878
  sources:
  - url: https://en.wikipedia.org/wiki/Rosh_Pinna
    checked: '2026-10-09'
- id: rehovot
  name: Rehovot
  lon: 34.8167
  lat: 31.8969
  type: town
  founded: 1890
  sources:
  - url: https://en.wikipedia.org/wiki/Rehovot
    checked: '2026-10-09'
- id: hadera
  name: Hadera
  lon: 34.9167
  lat: 32.45
  type: town
  founded: 1891
  sources:
  - url: https://en.wikipedia.org/wiki/Hadera
    checked: '2026-10-09'
- id: netanya
  name: Netanya
  lon: 34.8567
  lat: 32.3286
  type: town
  founded: 1929
  sources:
  - url: https://en.wikipedia.org/wiki/Netanya
    checked: '2026-10-09'
- id: degania_alef
  name: Degania Alef
  lon: 35.5747
  lat: 32.7081
  type: kibbutz
  founded: 1910
  sources:
  - url: https://en.wikipedia.org/wiki/Degania_Alef
    checked: '2026-10-09'
- id: ein_harod
  name: Ein Harod
  lon: 35.3908
  lat: 32.5603
  type: kibbutz
  founded: 1921
  sources:
  - url: https://en.wikipedia.org/wiki/Ein_Harod
    checked: '2026-10-09'
- id: nahalal
  name: Nahalal
  lon: 35.1967
  lat: 32.69
  type: moshav
  founded: 1921
  sources:
  - url: https://en.wikipedia.org/wiki/Nahalal
    checked: '2026-10-09'
- id: hanita
  name: Hanita
  lon: 35.1736
  lat: 33.0875
  type: kibbutz
  founded: 1938
  sources:
  - url: https://en.wikipedia.org/wiki/Hanita
    checked: '2026-10-09'
- id: revivim
  name: Revivim
  lon: 34.7211
  lat: 31.0436
  type: kibbutz
  founded: 1943
  sources:
  - url: https://en.wikipedia.org/wiki/Revivim
    checked: '2026-10-09'
- id: yad_mordechai
  name: Yad Mordechai
  lon: 34.5583
  lat: 31.5886
  type: kibbutz
  founded: 1943
  sources:
  - url: https://en.wikipedia.org/wiki/Yad_Mordechai
    checked: '2026-10-09'
```

- [ ] **Step 2: Write the failing tests.** Create `tools/tests/test_settlements.py`:

```python
"""Checks the committed map content: content/map/settlements_1948.yaml against content/map/hexes.json."""
import json
from pathlib import Path

import numpy as np
import pytest
import yaml

import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
TYPES = {"city", "town", "kibbutz", "moshav", "arab_town", "village"}
HEXES = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())
TOWNS = yaml.safe_load((ROOT / "content" / "map" / "settlements_1948.yaml").read_text())["settlements"]
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}


def test_ids_unique_and_fields_valid():
    assert len({s["id"] for s in TOWNS}) == len(TOWNS) >= 20
    for s in TOWNS:
        assert s["type"] in TYPES, s["id"]
        assert (s["founded"] is None and s.get("pre_modern") is True) or s["founded"] <= 1948, s["id"]
        assert s["sources"] and all(src["url"].startswith("https://") and src["checked"] for src in s["sources"])


@pytest.mark.parametrize("s", TOWNS, ids=lambda s: s["id"])
def test_settlement_inside_bounds_on_land_hex(s):
    b = HEXES["bounds"]
    assert b["lon0"] <= s["lon"] <= b["lon1"] and b["lat0"] <= s["lat"] <= b["lat1"]
    q, r = hg.settlement_hex(s["lon"], s["lat"], HEXES)
    assert BY_QR[(q, r)]["land"], f"{s['id']} placed on {BY_QR[(q, r)]['biome']} hex ({q},{r})"


def test_shore_settlement_moves_to_nearest_land_neighbour():
    tiberias = next(s for s in TOWNS if s["id"] == "tiberias")
    x, y = hg.lonlat_to_km(tiberias["lon"], tiberias["lat"], HEXES["bounds"])
    (q0,), (r0,) = hg.point_to_hex(np.array([x]), np.array([y]), HEXES["hex_size_km"])
    assert BY_QR[(q0, r0)]["biome"] == "lake"          # its own hex is mostly Sea of Galilee
    q, r = hg.settlement_hex(tiberias["lon"], tiberias["lat"], HEXES)
    assert (q - q0, r - r0) in hg.NEIGHBOURS
```

- [ ] **Step 3: Run the tests to verify they fail.**

Run: the shared test command.
Expected: `26 failed, 17 passed`. 25 of the failures say `module 'hexgrid' has no attribute 'settlement_hex'`, and the shore test says `no attribute 'lonlat_to_km'`.

- [ ] **Step 4: Add the placement code to `tools/hexgrid.py`.**

1. Directly below the line `SQRT3 = math.sqrt(3)`, add:

```python
NEIGHBOURS = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)]  # axial directions
```

2. Directly above `def hex_center_km`, add:

```python
def lonlat_to_km(lon: float, lat: float, b: dict) -> tuple[float, float]:
    """Map projection used by every tool: x km east of lon0, y km south of lat1."""
    return (lon - b["lon0"]) * km_per_deg_lon(b), (b["lat1"] - lat) * KM_PER_DEG_LAT
```

3. Directly above `def classify`, add:

```python
def settlement_hex(lon: float, lat: float, grid: dict) -> tuple[int, int]:
    """Placement rule: the containing hex if it is land, else the land neighbour with the nearest centre
    (shore towns such as Tiberias). Raises if neither exists, which the content tests turn into a failure."""
    size = grid["hex_size_km"]
    x, y = lonlat_to_km(lon, lat, grid["bounds"])
    (q,), (r,) = point_to_hex(np.array([x]), np.array([y]), size)
    land = {(h["q"], h["r"]) for h in grid["hexes"] if h["land"]}
    if (q, r) in land:
        return q, r
    options = [(q + dq, r + dr) for dq, dr in NEIGHBOURS if (q + dq, r + dr) in land]
    if not options:
        raise ValueError(f"no land hex at or next to ({lon}, {lat})")
    return min(options, key=lambda c: math.dist(hex_center_km(*c, size), (x, y)))
```

- [ ] **Step 5: Run the tests to verify they pass.**

Run: the shared test command.
Expected: `43 passed`.

`test_shore_settlement_moves_to_nearest_land_neighbour` covers Tiberias. Its own hex is mostly Sea of Galilee, so it moves to the land neighbour with the nearest centre. Without this rule, the real data fails with `tiberias falls on lake hex (11,8)`.

- [ ] **Step 6: Commit.**

```bash
git add content/map/settlements_1948.yaml tools/hexgrid.py tools/tests/test_settlements.py
git commit -m "feat(map): sourced 1948 settlements and shore placement rule

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 22: Geography check image (`tools/preview_map.py`)

**Files:**
- Create: `tools/preview_map.py`, `tools/tests/test_preview_map.py`

**Interfaces:**
- Consumes: from Task 20, `hexgrid.load_elevation`, `hex_center_km`, `km_per_deg_lon`, `KM_PER_DEG_LAT` and `MAP_DIR`; `hexes.json`; and from Task 21, `settlements_1948.yaml`.
- Produces:
  - `render(map_dir, hexes:dict, settlements:list[dict], out:Path, width_px=800) -> None`
  - `lonlat_to_px(lon, lat, meta, size) -> (x, y)`
  - `hillshade(elev) -> ndarray`
  - Constants: `BIOME_RGB`, `DOT_RGB`
  - The image `tools/.cache/map-preview.png`: north up, true km aspect ratio, 800 × 2136 px.

- [ ] **Step 1: Write the failing test.** Create `tools/tests/test_preview_map.py`:

```python
import json

import numpy as np
from PIL import Image

import hexgrid as hg
import preview_map as pm


def test_preview_renders_expected_size_and_marks_settlement(tmp_path):
    n = 201
    elev = np.zeros((n, n), dtype="float32"); elev[:, n // 2:] = 300.0
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    np.round(elev / 300.0 * 65535).astype("<u2").tofile(tmp_path / "heightmap.r16")
    hexes = hg.build(tmp_path, 5.0)
    towns = [{"id": "x", "name": "Testville", "lon": 35.4, "lat": 32.5, "type": "town"}]
    out = tmp_path / "preview.png"
    pm.render(tmp_path, hexes, towns, out, width_px=400)
    img = Image.open(out)
    assert img.width == 400
    km_w = 0.5 * hg.km_per_deg_lon(meta); km_h = 0.5 * hg.KM_PER_DEG_LAT
    assert abs(img.height - round(400 * km_h / km_w)) <= 1
    x, y = pm.lonlat_to_px(35.4, 32.5, meta, img.size)
    assert img.getpixel((round(x), round(y)))[:3] == pm.DOT_RGB
```

- [ ] **Step 2: Run the tests to verify they fail.**

Run: the shared test command.
Expected: `ModuleNotFoundError: No module named 'preview_map'`.

- [ ] **Step 3: Implement `tools/preview_map.py`.**

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""M2 geography check: shaded relief + biome hexes + settlements, north up, true km aspect.

Usage: uv run tools/preview_map.py [out.png]   (default: tools/.cache/map-preview.png)
Compare the result by eye against promo/ref/geo-reference.png before accepting map data."""
import json
import math
import sys
from pathlib import Path

import numpy as np
import yaml
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hexgrid as hg  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BIOME_RGB = {"sea": (70, 130, 180), "lake": (40, 110, 200), "coastal_plain": (175, 215, 120),
             "hills": (80, 135, 55), "mountains": (150, 120, 90), "valley": (215, 160, 50),
             "desert": (220, 195, 140)}
DOT_RGB = (200, 20, 20)


def lonlat_to_px(lon: float, lat: float, meta: dict, size: tuple[int, int]) -> tuple[float, float]:
    return ((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * (size[0] - 1),
            (meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * (size[1] - 1))


def hillshade(elev: np.ndarray) -> np.ndarray:
    gy, gx = np.gradient(elev)
    shade = (-gx - gy) / np.sqrt(gx ** 2 + gy ** 2 + 400.0)  # light from the north-west
    return np.clip(0.75 + 0.5 * shade, 0, 1)


def render(map_dir: Path, hexes: dict, settlements: list[dict], out: Path, width_px: int = 800) -> None:
    elev, meta = hg.load_elevation(map_dir)
    km_w = (meta["lon1"] - meta["lon0"]) * hg.km_per_deg_lon(meta)
    km_h = (meta["lat1"] - meta["lat0"]) * hg.KM_PER_DEG_LAT
    size = (width_px, round(width_px * km_h / km_w))
    shade = Image.fromarray((hillshade(elev) * 255).astype("uint8")).resize(size, Image.BILINEAR)
    img = Image.merge("RGB", [shade] * 3).convert("RGBA")
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    px_per_km = width_px / km_w
    for hx in hexes["hexes"]:
        cx, cy = hg.hex_center_km(hx["q"], hx["r"], hexes["hex_size_km"])
        corners = [((cx + hexes["hex_size_km"] * math.cos(math.radians(30 + 60 * i))) * px_per_km,
                    (cy + hexes["hex_size_km"] * math.sin(math.radians(30 + 60 * i))) * px_per_km) for i in range(6)]
        d.polygon(corners, fill=BIOME_RGB[hx["biome"]] + (110,), outline=(255, 255, 255, 60))
    img = Image.alpha_composite(img, layer).convert("RGB")
    d = ImageDraw.Draw(img)
    font = ImageFont.load_default(size=max(10, width_px // 60))
    for s in settlements:
        x, y = lonlat_to_px(s["lon"], s["lat"], meta, size)
        rad = max(3, width_px // 160)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=DOT_RGB)
        d.text((x + rad + 2, y - rad - 2), s["name"], fill=(0, 0, 0), font=font)
    img.save(out)


def main() -> None:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "tools" / ".cache" / "map-preview.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    hexes = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())
    towns_file = ROOT / "content" / "map" / "settlements_1948.yaml"
    towns = yaml.safe_load(towns_file.read_text())["settlements"] if towns_file.exists() else []
    render(hg.MAP_DIR, hexes, towns, out)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 4: Run the tests to verify they pass.**

Run: the shared test command.
Expected: `44 passed`.

- [ ] **Step 5: Render the image and check the geography by eye.** This is the M2 gate. Do not accept the map data until every item passes.

Run: `uv run --quiet tools/preview_map.py`. Then open `tools/.cache/map-preview.png` next to `promo/ref/geo-reference.png`.

Check that:
- [ ] The Mediterranean coast runs down the west side, the Carmel cape and Haifa Bay sit in the north, and Acre is across the bay from Haifa.
- [ ] The Sea of Galilee is 2–3 lake hexes. Tiberias is on its west shore, and Degania Alef is at its southern tip.
- [ ] The Jordan rift is a continuous strip of valley hexes from Galilee down to the Dead Sea.
- [ ] The Dead Sea's northern basin is lake hexes, its northern tip is level with Jerusalem, and the southern ponds are lake hexes too.
- [ ] Jerusalem sits in the hills and mountains, south-east of Tel Aviv.
- [ ] The coastal chain runs north to south in this order: Zikhron Ya'akov, Hadera, Netanya, Tel Aviv with Jaffa just south of it, Rishon LeZion, Rehovot, Yad Mordechai, Gaza. Petah Tikva is east of Tel Aviv; Lod and Ramla are south-east.
- [ ] Beersheba and Revivim are in desert hexes in the Negev, and the Gulf of Aqaba appears at the bottom.
- [ ] Nahalal, Nazareth and Ein Harod are in the Jezreel / Lower Galilee area. Safed and Rosh Pinna are north-west of the Sea of Galilee, and Hanita is near the northern edge of the land.

All of these checks passed on 2026-10-09. Known cosmetic issue: some labels overlap (Safed / Rosh Pinna, Rishon LeZion / Lod). This is a check image, not a game asset.

- [ ] **Step 6: Commit.**

```bash
git add tools/preview_map.py tools/tests/test_preview_map.py
git commit -m "feat(tools): geography check image for map data

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Milestone M2b: map in Unity, Tasks 23–25

These tasks consume the M1 sim library (public interface defined in Task 15) and the M2 tool outputs (Tasks 19–21):
- `unity/Assets/StreamingAssets/map/heightmap.r16`
- `unity/Assets/StreamingAssets/map/heightmap.json`
- `content/map/hexes.json`
- `content/map/settlements_1948.yaml`

**Before starting:** apply every "API correction" from `docs/spikes/m0-results.md` to the code below.

### Task 23: Game Unity project, with the sim DLL and an edit-mode smoke test

**Files:**
- Create (Hub, **Universal 3D** template, at `unity/`): the Unity project
- Create:
  - `tools/build_sim_for_unity.sh`
  - `unity/Assets/link.xml`
  - `unity/Assets/Scripts/Statecraft.Game.asmdef`
  - `unity/Assets/Scripts/Paths.cs`
  - `unity/Assets/Tests/EditMode/Statecraft.Tests.EditMode.asmdef`
  - `unity/Assets/Tests/EditMode/SimSmokeTests.cs`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: the M1 public interface (defined in Task 15): `ContentLoader.Load(string)`, `Simulation.NewGame(...)`, `Simulation.Step(...)`, `HistoricalAI.Commands(...)`, `GameState.Hash()`.
- Consumes: the golden hash constant in `docs/determinism.md` (also in `sim/Statecraft.Sim.Tests/DeterminismTests.cs`).
- Produces:
  - `Paths.RepoRoot`, `Paths.Content` and `Paths.StreamingMap` (string properties).
  - The `Statecraft.Game` assembly, which Tasks 24–25 add scripts to.

- [ ] **Step 1 (owner, interactive): Create the project**

`unity/` already exists after merging m2-tools: it holds `unity/Assets/StreamingAssets/map/` from Task 19. Unity Hub may refuse a non-empty folder or overwrite it, so:

1. In Unity Hub: Projects → New project → **Universal 3D** → location `~/israel-game/.worktrees/m0-m2`, name **`unity-tmp`**, editor 6000.3.25f1. Open it once, then close it.
2. Merge it into `unity/` without touching the map files, then remove the temp folder:
```bash
rsync -a unity-tmp/ unity/ && rm -rf unity-tmp && ls unity/Assets/StreamingAssets/map unity/ProjectSettings | head
```
Expected: `heightmap.json` and `heightmap.r16` are still listed, alongside `ProjectSettings` files.

- [ ] **Step 2: Write the build script for the sim DLL**

`tools/build_sim_for_unity.sh`:
```bash
#!/usr/bin/env bash
# Publishes the pure C# sim (netstandard2.1) plus YamlDotNet into Unity's plugin folder.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet publish sim/Statecraft.Sim -c Release -o unity/Assets/Plugins/Statecraft
ls unity/Assets/Plugins/Statecraft/Statecraft.Sim.dll unity/Assets/Plugins/Statecraft/YamlDotNet.dll
```
Run: `chmod +x tools/build_sim_for_unity.sh && tools/build_sim_for_unity.sh`
Expected: both DLL paths are printed.

- [ ] **Step 3: Protect the reflection-based types from stripping**

`unity/Assets/link.xml`:
```xml
<linker>
  <assembly fullname="YamlDotNet" preserve="all" />
  <assembly fullname="Statecraft.Sim" preserve="all" />
</linker>
```

- [ ] **Step 4: Write the assembly definitions and the paths helper**

`unity/Assets/Scripts/Statecraft.Game.asmdef`:
```json
{ "name": "Statecraft.Game", "references": ["Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Core.Runtime"], "overrideReferences": true, "precompiledReferences": ["Statecraft.Sim.dll", "YamlDotNet.dll"], "autoReferenced": true }
```

`unity/Assets/Scripts/Paths.cs`:
```csharp
using System.IO;
using UnityEngine;

namespace Statecraft.Game
{
    public static class Paths
    {
        // In the editor, content is read from the repo. Copying content into player builds comes in M3 (first player build).
        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        public static string Content => Application.isEditor ? Path.Combine(RepoRoot, "content") : Path.Combine(Application.streamingAssetsPath, "content");
        public static string StreamingMap => Path.Combine(Application.streamingAssetsPath, "map");
    }
}
```

`unity/Assets/Tests/EditMode/Statecraft.Tests.EditMode.asmdef`:
```json
{ "name": "Statecraft.Tests.EditMode", "references": ["Statecraft.Game", "UnityEngine.TestRunner", "UnityEditor.TestRunner"], "includePlatforms": ["Editor"], "overrideReferences": true, "precompiledReferences": ["nunit.framework.dll", "Statecraft.Sim.dll", "YamlDotNet.dll"], "defineConstraints": ["UNITY_INCLUDE_TESTS"] }
```

- [ ] **Step 5: Write the failing smoke test**

`unity/Assets/Tests/EditMode/SimSmokeTests.cs`. The type names below match Task 15's Interfaces block (`Statecraft.Sim.Simulation`, `ContentLoader`, `Mode`, `Command`).
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;

public class SimSmokeTests
{
    [Test]
    public void ContentLoads_AndStepIsDeterministicInsideUnity()
    {
        var content = ContentLoader.Load(Paths.Content);
        var a = Simulation.NewGame(content, 42UL, Mode.Historical);
        var b = Simulation.NewGame(content, 42UL, Mode.Historical);
        var ra = Simulation.Step(content, a, new List<Command>());
        var rb = Simulation.Step(content, b, new List<Command>());
        Assert.AreEqual(ra.State.Hash(), rb.State.Hash());
        Assert.IsNotEmpty(ra.State.Hash());
    }

    /// <summary>The same playthrough and constant as sim/Statecraft.Sim.Tests/DeterminismTests.cs (see docs/determinism.md).
    /// It proves Unity's runtime computes, bit for bit, what .NET computes. Update all three places together.</summary>
    public const string GoldenHash = "eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9";

    [Test]
    public void GoldenPlaythroughMatchesDotNet()
    {
        var c = ContentLoader.Load(Paths.Content);
        var s = Simulation.NewGame(c, 1948UL, Mode.Historical);
        for (int turn = 0; turn < 1000 && s.Date < new System.DateTime(1950, 1, 1) && s.GameOver == null; turn++)
            s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
        Assert.IsNull(s.GameOver);
        Assert.GreaterOrEqual(s.Date, new System.DateTime(1950, 1, 1));
        Assert.AreEqual(GoldenHash, s.Hash());
    }
}
```

`GoldenPlaythroughMatchesDotNet` mirrors the xUnit `DeterminismTests.GoldenPlaythroughHashIsFixed` line for line. Edit mode runs on the editor's Mono runtime. IL2CPP is checked when the first player build (M3) runs the same test in a player. If the two hashes differ, the cause is a determinism bug, not a reason to change the constant.

- [ ] **Step 6: Run the edit-mode tests in batch mode**

Run:
```bash
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?; grep -o 'result="[A-Za-z]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `result="Passed"`. (Step 5's test passes once Step 2's DLLs exist. If it fails, the log names the missing type or path.)

- [ ] **Step 7: Commit**

```bash
printf 'unity/Library/\nunity/Temp/\nunity/Logs/\nunity/UserSettings/\nunity/Build/\nunity/Assets/Plugins/Statecraft/\nunity-*.xml\nunity-*.log\n' >> .gitignore
printf '\n## After cloning\n- Run `tools/build_sim_for_unity.sh` before opening `unity/`. The sim DLLs are build output and are git-ignored.\n' >> docs/dev-setup.md
git add .gitignore docs/dev-setup.md tools/build_sim_for_unity.sh unity/Assets unity/Packages unity/ProjectSettings
git commit -m "feat(unity): game project with sim DLL and edit-mode smoke test

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 24: Terrain from the real heightmap, plus hex overlay and biome colours

**Files:**
- Create:
  - `unity/Assets/Scripts/Map/GeoFrame.cs`
  - `unity/Assets/Scripts/Map/HeightmapLoader.cs`
  - `unity/Assets/Scripts/Map/HexMath.cs`
  - `unity/Assets/Scripts/Map/MapBuilder.cs`
  - `unity/Assets/Tests/EditMode/MapTests.cs`

**Interfaces:**
- Consumes:
  - `heightmap.r16` and `heightmap.json`, exactly as Task 19 specifies. Row 0 is the north edge, with N = 2049.
  - `hexes.json`, as Task 20 specifies.
- Produces:
  - `GeoFrame(lon0, lon1, lat0, lat1, widthM, lengthM)` with `Vector3 ToWorld(double lon, double lat, float y)`.
  - `HeightmapLoader.Load(string dir) -> (HeightmapMeta meta, float[,] heights)`. `heights[y, x]` has y = 0 at the **south** edge, the order Unity's `TerrainData.SetHeights` expects.
  - `HexMath.Corner(Vector2 center, float sizeM, int i)` for pointy-top hexes.
  - `MapBuilder.Build(string mapDir, string hexesJson) -> GameObject`.

- [ ] **Step 1: Write the failing tests (orientation, projection, hex corners)**

`unity/Assets/Tests/EditMode/MapTests.cs`:
```csharp
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;

public class MapTests
{
    [Test]
    public void HeightmapRowZeroIsNorth_SoUnityRowZeroIsSouth()
    {
        // 3x3 raster: the north row is all 65535 and the rest 0. After loading, Unity row 2 (north) must be 1.
        var raw = new ushort[] { 65535, 65535, 65535, 0, 0, 0, 0, 0, 0 };
        var h = HeightmapLoader.ToUnityHeights(raw, 3);
        Assert.AreEqual(1f, h[2, 0], 1e-6); Assert.AreEqual(0f, h[0, 0], 1e-6);
    }

    [Test]
    public void GeoFrame_MapsCornersAndJerusalemEastOfTelAviv()
    {
        var f = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161000f, 433000f);
        Assert.AreEqual(0f, f.ToWorld(34.2, 29.45, 0).x, 1e-3);
        Assert.AreEqual(433000f, f.ToWorld(35.9, 33.35, 0).z, 1);
        var telAviv = f.ToWorld(34.78, 32.08, 0); var jerusalem = f.ToWorld(35.22, 31.78, 0);
        Assert.Greater(jerusalem.x, telAviv.x);   // east
        Assert.Less(jerusalem.z, telAviv.z);      // south
    }

    [Test]
    public void PointyTopCornerZeroIsAtMinus30Degrees()
    {
        var c = HexMath.Corner(Vector2.zero, 1f, 0); // Red Blob: angle = 60*i - 30 degrees
        Assert.AreEqual(Mathf.Cos(-30 * Mathf.Deg2Rad), c.x, 1e-5);
        Assert.AreEqual(Mathf.Sin(-30 * Mathf.Deg2Rad), c.y, 1e-5);
    }

    [Test]
    public void TruncatedHeightmapIsRejectedWithExpectedSize()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hm_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "heightmap.json"),
            "{\"width\":3,\"height\":3,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367,\"width_km\":161.5,\"length_km\":431.2,\"attribution\":\"x\"}");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "heightmap.r16"), new byte[10]); // 3x3 needs 18 bytes
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => HeightmapLoader.Load(dir));
        StringAssert.Contains("expected 18", ex.Message);
    }
}
```

The real widthM and lengthM come from `heightmap.json`. The test uses fixed numbers only to check direction.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: the Task 23 Step 6 command.
Expected: compile errors, because `HeightmapLoader`, `GeoFrame` and `HexMath` are not defined yet.

- [ ] **Step 3: Implement**

`unity/Assets/Scripts/Map/GeoFrame.cs`:
```csharp
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Equirectangular lon/lat -> world metres. x = east, z = north, origin = south-west corner.</summary>
    public readonly struct GeoFrame
    {
        public readonly double Lon0, Lon1, Lat0, Lat1; public readonly float WidthM, LengthM;
        public GeoFrame(double lon0, double lon1, double lat0, double lat1, float widthM, float lengthM)
        { Lon0 = lon0; Lon1 = lon1; Lat0 = lat0; Lat1 = lat1; WidthM = widthM; LengthM = lengthM; }
        public Vector3 ToWorld(double lon, double lat, float y) =>
            new Vector3((float)((lon - Lon0) / (Lon1 - Lon0) * WidthM), y, (float)((lat - Lat0) / (Lat1 - Lat0) * LengthM));
    }
}
```

`unity/Assets/Scripts/Map/HeightmapLoader.cs`:
```csharp
using System;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable]
    public sealed class HeightmapMeta
    {
        public int width, height; public double lon0, lon1, lat0, lat1;
        public float elev_min_m, elev_max_m, width_km, length_km; public string attribution;
    }

    public static class HeightmapLoader
    {
        public static (HeightmapMeta meta, float[,] heights) Load(string dir)
        {
            var meta = JsonUtility.FromJson<HeightmapMeta>(File.ReadAllText(Path.Combine(dir, "heightmap.json")));
            if (meta.width != meta.height) throw new InvalidDataException($"heightmap.json: width {meta.width} != height {meta.height}");
            var bytes = File.ReadAllBytes(Path.Combine(dir, "heightmap.r16"));
            if (bytes.Length != meta.width * meta.height * 2) throw new InvalidDataException($"heightmap.r16: {bytes.Length} bytes, expected {meta.width * meta.height * 2}");
            var raw = new ushort[meta.width * meta.height];
            Buffer.BlockCopy(bytes, 0, raw, 0, bytes.Length); // little-endian on all our targets
            return (meta, ToUnityHeights(raw, meta.width));
        }

        /// <summary>File row 0 is north; Unity heights[y, x] has y = 0 at south.</summary>
        public static float[,] ToUnityHeights(ushort[] raw, int n)
        {
            var h = new float[n, n];
            for (int fileRow = 0; fileRow < n; fileRow++)
                for (int x = 0; x < n; x++)
                    h[n - 1 - fileRow, x] = raw[fileRow * n + x] / 65535f;
            return h;
        }
    }
}
```

`unity/Assets/Scripts/Map/HexMath.cs`:
```csharp
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Pointy-top hex geometry (Red Blob Games): corner i at 60*i - 30 degrees.</summary>
    public static class HexMath
    {
        public static Vector2 Corner(Vector2 center, float sizeM, int i)
        {
            float a = Mathf.Deg2Rad * (60f * i - 30f);
            return new Vector2(center.x + sizeM * Mathf.Cos(a), center.y + sizeM * Mathf.Sin(a));
        }
    }
}
```

`unity/Assets/Scripts/Map/MapBuilder.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable] public sealed class HexJson { public int q, r; public double lon, lat; public float elev_m; public bool land; public string biome; }
    [Serializable] public sealed class HexFile { public float hex_size_km; public string layout; public HexJson[] hexes; }

    public static class MapBuilder
    {
        public const float VerticalExaggeration = 3f; // diorama look; real relief would read flat at 400 km scale

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        public static GameObject Build(string mapDir, string hexesJson)
        {
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            float widthM = meta.width_km * 1000f, lengthM = meta.length_km * 1000f;
            float reliefM = (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration;
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, widthM, lengthM);

            var data = new TerrainData { heightmapResolution = meta.width };
            data.size = new Vector3(widthM, reliefM, lengthM);
            data.SetHeights(0, 0, heights);

            var hexFile = JsonUtility.FromJson<HexFile>(File.ReadAllText(hexesJson));
            if (hexFile.layout != "pointy") throw new InvalidDataException($"hexes.json: layout '{hexFile.layout}' unsupported");
            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            ApplyBiomeColours(data, frame, hexFile, meta);

            var overlay = BuildHexOverlay(frame, hexFile, data, meta, reliefM);
            overlay.transform.SetParent(terrainGo.transform, false);
            return terrainGo;
        }

        /// <summary>One terrain layer per biome; each alphamap texel takes the biome of its nearest hex centre.</summary>
        static void ApplyBiomeColours(TerrainData data, GeoFrame frame, HexFile hexes, HeightmapMeta meta)
        {
            var names = new List<string>(Biome.Keys);
            var layers = new TerrainLayer[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Biome[names[i]]); tex.Apply();
                layers[i] = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(1000, 1000) };
            }
            data.terrainLayers = layers;
            int res = 512; data.alphamapResolution = res;
            var centres = new List<(Vector2 p, int layer)>();
            foreach (var h in hexes.hexes)
            {
                int idx = names.IndexOf(h.biome);
                if (idx < 0) throw new InvalidDataException($"hexes.json: unknown biome '{h.biome}' at q={h.q} r={h.r}");
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add((new Vector2(w.x, w.z), idx));
            }
            var grid = BucketGrid(centres, hexes.hex_size_km * 1000f * 2f);
            var a = new float[res, res, names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    var p = new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM);
                    a[y, x, Nearest(grid, p, hexes.hex_size_km * 1000f * 2f)] = 1f;
                }
            data.SetAlphamaps(0, 0, a);
        }

        static Dictionary<(int, int), List<(Vector2 p, int layer)>> BucketGrid(List<(Vector2 p, int layer)> pts, float cell)
        {
            var g = new Dictionary<(int, int), List<(Vector2, int)>>();
            foreach (var c in pts)
            {
                var k = ((int)(c.p.x / cell), (int)(c.p.y / cell));
                if (!g.TryGetValue(k, out var l)) g[k] = l = new List<(Vector2, int)>();
                l.Add(c);
            }
            return g;
        }

        static int Nearest(Dictionary<(int, int), List<(Vector2 p, int layer)>> g, Vector2 p, float cell)
        {
            int cx = (int)(p.x / cell), cy = (int)(p.y / cell); float best = float.MaxValue; int layer = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (g.TryGetValue((cx + dx, cy + dy), out var l))
                        foreach (var c in l) { float d = (c.p - p).sqrMagnitude; if (d < best) { best = d; layer = c.layer; } }
            return layer;
        }

        /// <summary>Thin line mesh of every land hex outline, draped 30 m above the terrain.</summary>
        static GameObject BuildHexOverlay(GeoFrame frame, HexFile hexes, TerrainData data, HeightmapMeta meta, float reliefM)
        {
            float size = hexes.hex_size_km * 1000f;
            var verts = new List<Vector3>(); var idx = new List<int>();
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                var c = frame.ToWorld(h.lon, h.lat, 0);
                int start = verts.Count;
                for (int i = 0; i < 6; i++)
                {
                    var k = HexMath.Corner(new Vector2(c.x, c.z), size, i);
                    float y = data.GetInterpolatedHeight(k.x / frame.WidthM, k.y / frame.LengthM) + 30f;
                    verts.Add(new Vector3(k.x, y, k.y));
                }
                for (int i = 0; i < 6; i++) { idx.Add(start + i); idx.Add(start + (i + 1) % 6); }
            }
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetIndices(idx, MeshTopology.Lines, 0);
            var go = new GameObject("HexOverlay");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.15f, 0.12f, 0.08f, 0.35f) };
            return go;
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: the Task 23 Step 6 command.
Expected: `exit=0` and `result="Passed"` (smoke test plus the 4 map tests).

- [ ] **Step 5: Commit**

```bash
git add unity/Assets   # includes the .meta files Unity generates; Plugins/Statecraft stays ignored
git commit -m "feat(map): terrain from Copernicus heightmap, biome layers, hex overlay

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 25: 1948 settlements, diorama camera, geography check, flythrough video

**Files:**
- Create:
  - `unity/Assets/Scripts/Map/SettlementPlacer.cs`
  - `unity/Assets/Scripts/Map/CameraRig.cs`
  - `unity/Assets/Scripts/Map/MapScene.cs`
  - `unity/Assets/Editor/MapCapture.cs`
  - `unity/Assets/Tests/EditMode/SettlementTests.cs`
  - `docs/m2-geography-check.md`

**Interfaces:**
- Consumes:
  - `content/map/settlements_1948.yaml` (Task 21): `{settlements: [{id, name, lon, lat, type, founded (int or null), pre_modern (optional bool), sources: [{url, checked}]}]}`. The C# classes below were verified on 2026-10-09 by parsing all 25 entries of Task 21's file with YamlDotNet 18.1.0 in scratch.
  - Rendering uses each settlement's sourced lon/lat. Task 21's hex-assignment rule (nearest land hex, e.g. Tiberias) decides *ownership* for the sim, not the marker's position.
  - `GeoFrame` and `MapBuilder.Build` (Task 24).
- Produces:
  - The `Assets/Map.unity` scene.
  - `MapCapture.TopDown()`, which writes `captures/topdown.png`.
  - `MapCapture.Flythrough()`, which writes `captures/fly/*.png`, encoded to `captures/flythrough.mp4`.

- [ ] **Step 1: Write the failing test (types map to markers; unknown types are an error)**

`unity/Assets/Tests/EditMode/SettlementTests.cs`:
```csharp
using System.IO;
using NUnit.Framework;
using Statecraft.Game.Map;

public class SettlementTests
{
    [Test]
    public void ParsesYamlAndRejectsUnknownType()
    {
        var ok = SettlementPlacer.Parse("settlements:\n- {id: jlm, name: Jerusalem, lon: 35.2256, lat: 31.7789, type: city, founded: null, pre_modern: true, sources: [{url: u, checked: '2026-10-09'}]}\n");
        Assert.AreEqual("Jerusalem", ok[0].Name); Assert.IsNull(ok[0].Founded); Assert.IsTrue(ok[0].PreModern);
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n- {id: q, name: Q, lon: 35, lat: 32, type: castle, founded: 1900, sources: [{url: u, checked: x}]}\n"));
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

Run: the Task 23 Step 6 command. Expected: a compile error, `SettlementPlacer` not defined.

- [ ] **Step 3: Implement the placer, camera rig and scene bootstrap**

`unity/Assets/Scripts/Map/SettlementPlacer.cs`:
```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    public sealed class SourceRef { public string Url { get; set; } = ""; public string Checked { get; set; } = ""; }

    public sealed class Settlement
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "";
        public double Lon { get; set; } public double Lat { get; set; }
        public int? Founded { get; set; }      // null for pre-modern cities (no verifiable founding year)
        public bool PreModern { get; set; }
        public List<SourceRef> Sources { get; set; } = new List<SourceRef>();
    }

    public sealed class SettlementFile { public List<Settlement> Settlements { get; set; } = new List<Settlement>(); }

    public static class SettlementPlacer
    {
        static readonly Dictionary<string, (Color c, float h)> Marker = new Dictionary<string, (Color, float)>
        {
            ["city"] = (new Color(0.93f, 0.92f, 0.88f), 1800f), ["town"] = (new Color(0.88f, 0.84f, 0.76f), 1100f),
            ["kibbutz"] = (new Color(0.80f, 0.35f, 0.25f), 600f), ["moshav"] = (new Color(0.78f, 0.45f, 0.30f), 600f),
            ["arab_town"] = (new Color(0.82f, 0.74f, 0.58f), 1100f), ["village"] = (new Color(0.76f, 0.68f, 0.52f), 600f),
        };

        public static List<Settlement> Parse(string yaml)
        {
            var list = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build().Deserialize<SettlementFile>(yaml).Settlements;
            foreach (var s in list)
                if (!Marker.ContainsKey(s.Type)) throw new InvalidDataException($"settlements: '{s.Id}' has unknown type '{s.Type}'");
            return list;
        }

        /// <summary>Placeholder markers (real per-era prefabs come in M3/M4): a box sized by type plus a name label.</summary>
        public static void Place(List<Settlement> list, GeoFrame frame, Terrain terrain)
        {
            var parent = new GameObject("Settlements").transform;
            foreach (var s in list)
            {
                var p = frame.ToWorld(s.Lon, s.Lat, 0); p.y = terrain.SampleHeight(p);
                var (c, h) = Marker[s.Type];
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = s.Id; box.transform.SetParent(parent);
                box.transform.position = p + Vector3.up * h / 2; box.transform.localScale = new Vector3(h, h, h);
                box.GetComponent<Renderer>().material.color = c;
                var label = new GameObject(s.Id + "_label").AddComponent<TextMesh>();
                label.text = s.Name; label.characterSize = 400; label.anchor = TextAnchor.LowerCenter; label.color = Color.black;
                label.transform.position = p + Vector3.up * (h + 400); label.transform.rotation = Quaternion.Euler(50, 0, 0);
                label.transform.SetParent(parent);
            }
        }
    }
}
```

`unity/Assets/Scripts/Map/CameraRig.cs`:
```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, Gaussian far blur beyond the look-at point (tilt-shift look).
    /// Gaussian, not Bokeh: physical Bokeh DoF keeps everything sharp at 100 km focus distances.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 50f, Fov = 35f;
        public Vector3 LookAt; public float Distance = 120000f;
        DepthOfField _dof;

        public void Init()
        {
            var cam = GetComponent<Camera>(); cam.fieldOfView = Fov; cam.farClipPlane = 1_000_000f; cam.nearClipPlane = 50f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.isGlobal = true;
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _dof = vol.profile.Add<DepthOfField>(true); _dof.mode.Override(DepthOfFieldMode.Gaussian);
            _dof.gaussianMaxRadius.Override(1.5f);
            Apply();
        }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(Pitch, 0, 0);
            transform.position = LookAt - transform.forward * Distance;
            if (_dof != null) { _dof.gaussianStart.Override(Distance * 1.05f); _dof.gaussianEnd.Override(Distance * 1.8f); }
        }
    }
}
```

`unity/Assets/Scripts/Map/MapScene.cs`:
```csharp
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Builds the whole map at runtime: terrain, hexes, 1948 settlements, camera.</summary>
    public sealed class MapScene : MonoBehaviour
    {
        public GeoFrame Frame { get; private set; }
        public Terrain Terrain { get; private set; }

        void Awake() => Build();

        public void Build()
        {
            var terrainGo = MapBuilder.Build(Paths.StreamingMap, Path.Combine(Paths.Content, "map", "hexes.json"));
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            var (meta, _) = HeightmapLoader.Load(Paths.StreamingMap);
            Frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            SettlementPlacer.Place(SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", "settlements_1948.yaml"))), Frame, Terrain);
            var rig = Camera.main.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
        }
    }
}
```

Reading the heightmap twice is deliberate and cheap (one file read at startup). It keeps `MapBuilder.Build` with a single return value.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: the Task 23 Step 6 command. Expected: `result="Passed"`.

- [ ] **Step 5: Write the capture tools (top-down geography check and flythrough frames)**

`unity/Assets/Editor/MapCapture.cs`:
```csharp
using System.IO;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapCapture
{
    const string ScenePath = "Assets/Map.unity";

    public static void MakeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("MapScene").AddComponent<MapScene>();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    /// <summary>Orthographic straight-down render of the whole map, for comparison with promo/ref/geo-reference.png.</summary>
    public static void TopDown()
    {
        MakeScene();
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = new GameObject("TopCam").AddComponent<Camera>();
        var size = map.Terrain.terrainData.size;
        cam.orthographic = true; cam.orthographicSize = size.z / 2; cam.aspect = size.x / size.z;
        cam.transform.position = new Vector3(size.x / 2, 100000, size.z / 2); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.farClipPlane = 300000;
        Save(cam, Mathf.RoundToInt(2000 * size.x / size.z), 2000, "../captures/topdown.png");
    }

    /// <summary>240 frames flying north along the coast at diorama pitch; encode with ffmpeg.</summary>
    public static void Flythrough()
    {
        MakeScene();
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var rig = Camera.main.GetComponent<CameraRig>();
        Directory.CreateDirectory("../captures/fly");
        for (int i = 0; i < 240; i++)
        {
            double lat = 31.3 + (32.9 - 31.3) * i / 239.0; // from the northern Negev to the Carmel
            rig.LookAt = map.Frame.ToWorld(34.95, lat, 0); rig.Distance = 90000f; rig.Apply();
            Save(Camera.main, 1920, 1080, $"../captures/fly/{i:D4}.png");
        }
    }

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt);
    }
}
```

- [ ] **Step 6: Render the top-down capture and run the geography check**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod MapCapture.TopDown -logFile unity-capture.log; echo exit=$?; ls -la captures/topdown.png
```
Expected: `exit=0` and a PNG about 750×2000 px.

Then **open `captures/topdown.png` and `promo/ref/geo-reference.png` side by side** and check each item in `docs/m2-geography-check.md`:
```markdown
# M2 geography check (captures/topdown.png vs promo/ref/geo-reference.png)
- [ ] Coast on the west; Carmel ridge reaching the coast at Haifa
- [ ] Sea of Galilee north-east; Jordan valley running south to the Dead Sea
- [ ] Dead Sea's north tip level with Jerusalem; Jerusalem south-east of Tel Aviv
- [ ] Negev widening to the south; Beersheba inland, south of the coastal plain
- [ ] Every 1948 settlement marker sits on its correct place (spot-check Tel Aviv, Jaffa, Haifa, Jerusalem, Beersheba, Tiberias)
- [ ] No settlement in the sea; no hex outline over open sea
Result: PASS / FAIL (with notes)
```
Also compare with `tools/preview_map.py`'s image (Task 22): Unity's render and the tool's preview must agree.

Any FAIL blocks this task. Fix the tool or data upstream (Tasks 19–21), not the picture.

If Step 6 shows a black or empty image in batch mode, run the same `-executeMethod` *without* `-batchmode`. Note this in the spike results doc.

- [ ] **Step 7: Render and encode the flythrough (the M2 recruiting asset)**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod MapCapture.Flythrough -logFile unity-fly.log && \
ffmpeg -y -framerate 30 -i captures/fly/%04d.png -c:v libx264 -pix_fmt yuv420p -crf 18 captures/flythrough.mp4 && ls -la captures/flythrough.mp4
```
Expected: an 8-second, 1920×1080 MP4. Watch it: the coast is on the left, heading north, with labelled 1948 places.

- [ ] **Step 8: Commit (the captures stay out of git; the check result goes in)**

```bash
printf 'captures/\n' >> .gitignore
git add .gitignore unity/Assets docs/m2-geography-check.md   # unity/Assets includes Map.unity and all .meta files
git commit -m "feat(map): 1948 settlements, diorama camera, geography check, flythrough

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Deferred to later plans (spec items deliberately outside M0–M2)

Each item below is in the spec and has a home. None is silently dropped.

| Spec item | Plan |
|---|---|
| Release-build gating: only `historian-reviewed` content; `sensitivity-reviewed` required on sensitive tags; `living: true` people excluded until legal review (release builds include a person only when `living == false`; `null` is a load error and `true` is excluded until legal review is recorded) | M5 (release pipeline). M1 already records `status` and `sources` on every fact |
| Saves (seed + command log + content hash + schema version, migrations, autosave each turn) | M3 (first player build) |
| UI screens (HUD, Cabinet, Budget, Defence, Diplomacy, Event/decision scene, "Why did this happen?", legacy report) and content copied into StreamingAssets | M3 |
| `Appoint` and `MapAction` commands; transit camps and housing (from 1950) | M3–M4 |
| Full Defence system beyond the ladder: command traits, intelligence estimates with uncertainty, procurement, manpower policy, war mode on the hex map | M4 |
| Open history mode (M1 throws `NotSupportedException` for `Mode.Open`) | M4 |
| Economy tuning: M1's economy is untuned, and historical autoplay builds up debt and runs foreign currency reserves negative after 1949 | M4 (calibration v2 against Bank of Israel / CBS series) |
| Per-era art prefabs that replace the M2 placeholder markers; in-game portrait pipeline | M3–M4 |
| Steam page and AI-disclosure manifest | M5 |
| Open fact: how the budget was approved in 1948–56 (`open_questions: [budget_approval]` is pinned by a test) | Content work before M4 |
| Hebrew names (`name_he`) and birth/death dates for people (spec §5.3) | Content work before M4 |
| "Military or economic collapse ends the state" (spec §4) | M4 |
| Validation that legislature periods do not overlap, and that government periods do not overlap | Before M4 adds the 1951 and 1955 Knessets |
