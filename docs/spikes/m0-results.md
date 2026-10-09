# M0 spike results (2026-10-09, Unity 6000.3.25f1)

Project: `spikes/m0/unity`, created headlessly from the Universal 3D template
(`com.unity.template.3d-cross-platform-17.0.14.tgz`, URP `com.unity.render-pipelines.universal` 17.3.0).
Web = WebGL 2 (IL2CPP), Chrome on this Mac. Mac = StandaloneOSX, IL2CPP (GameAssembly.dylib present).

| Q | Question | Answer | Evidence |
|---|---|---|---|
| a | URP Web build runs a 4,000-hex instanced scene | **yes**, fps=120.0 in Chrome on this Mac. Web headroom unmeasured (vsync-capped at 120 fps in a foreground tab; ~1 fps when the tab is backgrounded). The Mac IL2CPP player runs uncapped at 1,850–2,460 fps (1280×720 window, different backend/renderer), which only shows the scene itself is cheap. Real terrain, shading and shadow costs were not spiked | `spikes/m0/evidence/web-dof-off.jpg`; `SPIKE a: fps=120.0` in `spikes/m0/evidence/web-console-spike-lines.txt`; `mac-player-log-spike-lines.txt` |
| b | Unity MCP works with Claude Code on our plan | **Not verified.** The AI Assistant package (`com.unity.ai.assistant`; only pre-releases exist, 2.20.0-pre.2) installs in Unity 6000.3.25f1 with no compile errors, and the relay registers with Claude Code: `claude mcp add unity -- ~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp` (`claude mcp list`: Connected, relay process only). Linking a Unity Cloud project and starting the AI trial need actions in the owner's Unity Cloud account that this session's permission policy blocked, so plan/trial status and the Unity bridge were not tested. Unity's guide requires a Cloud-linked project and an AI trial/subscription. Fallback: batchmode + scripts (proven in Task 2); nothing depends on MCP | Editor log (package resolved); `claude mcp list` output. No console read from Claude Code |
| c | netstandard2.1 DLL + YamlDotNet in Unity (IL2CPP Web and Mac) | **yes** | `SPIKE c: yaml ok count=2 first=David Ben-Gurion` in browser console and in Mac Player.log (both IL2CPP, link.xml as in brief) |
| d | UI Toolkit runtime binding, 13 cards, live update | **yes** | `web-d-before-mutation.jpg` (card 1 shows 5) vs `web-d-after-mutation.jpg` (card 1 shows 9) after `SPIKE d: mutated vm0 competence to 9`; Mac: `mac-dof-on.jpg` shows 9 |
| e | URP Gaussian DoF (tilt-shift look) | **yes**, but only after enabling post-processing on the camera (see API corrections) | `web-dof-on.jpg` vs `web-dof-off.jpg`; `mac-dof-on.jpg` vs `mac-dof-off.jpg` (volume toggled every 4 s, logged as `SPIKE e: dof ON/OFF`). Far hexes blur, near hexes stay sharper |
| f | Legacy TextMesh label renders under URP | **yes** | "Tel Aviv" (font left default) and "Haifa" (font set explicitly) both visible, not magenta, in `web-dof-off.jpg` and `mac-dof-off.jpg`. `SPIKE f2: textmesh default font=LegacyRuntime`: AddComponent<TextMesh> already gets the built-in LegacyRuntime font |

Notes on (a): the browser tab must be foreground/visible. In a background or occluded Chrome window the
player ran at 0.7-1.3 fps (rAF throttling), and the splash screen did not finish. That is browser behaviour, not
a render cost. Web headroom unmeasured (vsync-capped at 120 fps in a foreground tab; ~1 fps when the tab is
backgrounded). The Mac IL2CPP player runs uncapped at 1,850–2,460 fps (1280×720 window, different backend/renderer),
which only shows the scene itself is cheap. Real terrain, shading and shadow costs were not spiked: the stress test is
4,000 flat top-face hex caps with plain URP Lit.

## API corrections
1. `Shader.Find("Universal Render Pipeline/Lit")` returns **null in the player** (Web and Mac), because nothing in the
   build references the shader, so it is stripped. The brief's `new Material(Shader.Find(...))` threw
   `ArgumentNullException: Value cannot be null. Parameter name: shader` at runtime (compiled fine, worked only in editor).
   Fix: create the material in an editor script (`new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true }`
   saved with `AssetDatabase.CreateAsset(mat, "Assets/Spike/HexMat.mat")`) and pass it to the runtime component through a
   serialized `public Material hexMaterial` field. Alternative: add the shader to Always Included Shaders. A material asset
   with `enableInstancing = true` also keeps the INSTANCING_ON variant from being stripped.
2. A camera made by `EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, ...)` has URP post-processing **off**
   (`SPIKE e: camera renderPostProcessing was False`), so a global Volume with DepthOfField has no visible effect.
   Fix: `using UnityEngine.Rendering.Universal; Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;`
   (or set it on the camera in the scene).
3. Standalone (Mac) defaults to the **Mono** scripting backend. To test (and ship) IL2CPP on Mac, set
   `PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);`
   before `BuildPipeline.BuildPlayer`. With it the Mac build worked, YamlDotNet included (link.xml as in the brief).
4. Layout, not an API: a 70 px card height clips the third label (Competence) of a 3-label card at default font size;
   90 px fits. Separately, the default `PanelSettings` (created with `ScriptableObject.CreateInstance<PanelSettings>()`)
   scales with screen DPI, so on the Retina Mac player the cards are about 2.7x larger than in the browser. Tasks 23-25
   should set `PanelSettings.scaleMode` / `referenceResolution` explicitly.

Everything else in the brief compiled and ran unchanged: `INotifyBindablePropertyChanged`, `[CreateProperty]`,
`DataBinding { dataSourcePath, bindingMode = BindingMode.ToTarget }`, `SetBinding("text", ...)`, `VolumeProfile.Add<DepthOfField>`,
`DepthOfFieldMode.Gaussian` with `gaussianStart/End/MaxRadius`, `Graphics.RenderMeshInstanced(RenderParams, ...)`,
`WebGLCompressionFormat.Disabled`, `BuildPipeline.BuildPlayer`/`BuildReport`. The YamlDotNet 18.1.0 publish output matched the brief.

## Decision
- M3 delivery: **browser link** (a = yes, fps 120 ≥ 30 in Chrome on this Mac). Caveat: a backgrounded tab is throttled to ~1 fps.

## Owner steps to finish (b)
Done already: Unity Hub sign-in; package added then reverted (re-add `"com.unity.ai.assistant": "2.20.0-pre.2"` to `spikes/m0/unity/Packages/manifest.json`); relay at `~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64`; `claude mcp add unity -- <relay> --mcp` (re-run if needed).
1. Open `spikes/m0/unity` in the editor.
2. Edit > Project Settings > Services: link or create a Unity Cloud project. Linking writes IDs to local ProjectSettings; do not commit them.
3. Edit > Project Settings > AI: start the AI trial; note the exact terms (Personal plan allowed? trial length? payment details?).
4. Confirm "Unity Bridge: Running" under Unity MCP.
5. Start a new Claude Code session; accept "Pending Connection" in Unity.
6. Open `Assets/Spike/Spike.unity`, press Play, then ask Claude Code to read the Unity console and check for the `SPIKE` lines.
