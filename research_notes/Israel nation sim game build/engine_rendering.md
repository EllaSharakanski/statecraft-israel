# Engine & Rendering-Technology Choice for a Civ-VII-Quality Stylized 3D Hex Strategy Map (small team, AI-agent-written code, Steam Win/Mac/Deck) — as of Oct 2026

Note on method: ~16 searches/fetches. Many 2026 facts come from secondary sources (vendor blogs, aggregators); these are flagged. Items marked "[background knowledge, unsourced]" are from the researcher's prior knowledge and must be verified before being stated as fact in the final report.

## 1. What engines do shipped hex/4X/grand-strategy games with high-end visuals use, and why?

### Takeaway
High-end 4X games split between proprietary in-house engines (Civ VII / Firaxis, Ara / Oxide Nitrous) and Unity (Humankind, Endless Legend, Old World, Millennia). No major Unreal 4X and no high-visual-fidelity Godot 4X with published postmortems were found. Unity is the dominant commercial off-the-shelf choice in this genre.

### Cited Findings
- Civilization VII is built on a Firaxis proprietary engine, and Firaxis handled shipping to all platforms in-house — [Game Developer](https://www.gamedeveloper.com/design/firaxis-big-swing-with-civilization-vii-convincing-players-to-actually-finish-their-games)
- Civ VII art direction aims for "readable realism": zoom in to see detail, zoom out to read the whole landscape (Lead Concept Artist Ryan Andrade) — [Checkpoint Gaming](https://checkpointgaming.net/features/2024/10/speaking-with-firaxis-about-ambitious-plans-with-civilization-vii)
- Outsourced artists (2K Valencia) built buildings/wonders and integrated them into Civ VII's engine under the Firaxis environment team's guidance — [2K Valencia](https://valencia.2k.com/civ-vii-environment-art/)
- Civ VII UI is built on Coherent Labs Gameface (HTML/CSS/JS). Firaxis used Chrome Inspector profiling and the heap analyzer for UI performance. This is a vendor case study — [Coherent Labs](https://coherent-labs.com/blog/casestudies/firaxis-case-study/); confirmed independently by [Side case study](https://side.inc/lab/sid-meiers-civilization-vii-case-study)
- Community analysis says the Civ VII UI scripting runtime is JavaScript on V8, using `engine.trigger` and `engine.createJSModel` bindings. This is user analysis, not official — [CivFanatics](https://forums.civfanatics.com/threads/scripting-runtime-information.695101/)
- Firaxis acknowledged the launch UI "did not meet expectations" and split the team into quick fixes plus a longer-term rework of how the UI is built — [2K/Civ official (DE)](https://civilization.2k.com/de-DE/civ-vii/game-guide/gameplay/loading-screen/)
- Ara: History Untold (Oxide Games) uses Oxide's proprietary Nitrous engine (also used for Ashes of the Singularity) with SWARM multi-core rendering — [Ara official](https://arahistoryuntold.com/nitrous-engine-with-dan-baker); [Wikipedia: Oxide Games](https://en.wikipedia.org/wiki/Oxide_Games)
- Humankind (Amplitude) uses Unity — [Wikipedia](https://en.wikipedia.org/wiki/Humankind_(video_game)). Its render pipeline (HDRP vs built-in) was not confirmed.
- Endless Legend (Amplitude) uses Unity. Amplitude reused its engine from Endless Space but "rewrote almost everything code-wise" — [Wikipedia](https://en.wikipedia.org/wiki/Endless_Legend); [MCV UK](https://mcvuk.com/?p=71149)
- Endless Legend 2 reportedly reached 1.0 on Sept 17, 2026 on PC/PS5/Xbox. The engine was not confirmed by search — [Sortiraparis (secondary)](https://www.sortiraparis.com/ar/altarfihi/alalab/articles/325738-andls-lyjnd-2-alasdar-1-0-ysl-aly-pc-wps5-wxbox-series-rdna-almsbq)
- Old World (Mohawk Games) runs on Unity. A Feb 2026 update reportedly migrated it from Unity 2022 to Unity 6.3 with a deep rendering overhaul (secondary, Russian-language source) — [rutab.net](https://rutab.net/b/video-games/2026/02/19/razrabotchik-civilization-4-vypustil-krupnoe-obnovlenie-dlya-svoey-strategii-old-world.html)
- Millennia is reportedly built in Unity/C# (machine-translated secondary article) — [foro3d](https://foro3d.com/en/2026/mayo/millennia-evolucion-visual-y-mecanicas-4x-en-unity-con-c.html)
- Songs of Syx appears to be Java-based. A crash report shows the Java Runtime, and early builds launched via `java -jar`. These are prototype-era sources — [Steam forum](https://steamcommunity.com/app/1162750/discussions/1/3800524254165144355); [itch.io](https://itch.io/post/875952)
- Civ VI (the predecessor) had a new terrain generation system, procedural object placement for cities and roads, and PBR lighting on all assets — [PCGH interview](https://www.pcgameshardware.de/Civilization-6-Spiel-56986/Specials/Interview-technical-details-1199995/)
- Terrain3D, the main Godot terrain plugin, was built for Out of the Ashes (an action-adventure, not strategy). Godot strategy games found on itch.io are small-scale. No shipped high-fidelity Godot 4X was found — [Terrain3D GitHub](https://github.com/TokisanGames/Terrain3D/releases); [godot-open-rts](https://github.com/williamsokol/godot-open-rts)

### Inferences
- Unity is the de facto mid-to-large studio choice for 3D 4X (Amplitude, Mohawk, and reportedly C Prompt/Paradox for Millennia). The reasons look like C# productivity, mature tooling, and strong 2D/3D hybrid UI support. Studios with AAA budgets (Firaxis, Oxide) build their own engines, which is not an option for a tiny team.
- The Civ VII model is a native 3D renderer plus an HTML/CSS/JS UI layer. That is a strong pattern for data-heavy strategy UI, and it maps well to AI coding agents, given the volume of web training data.
- Old World's 2026 move to Unity 6.3 suggests Unity 6 LTS is viable for a long-lived 4X today.

### Gaps
- Humankind's render pipeline, Endless Legend 2's engine, and Millennia's developer/engine were not confirmed by primary sources. [background knowledge, unsourced: Humankind is widely reported as Unity with a custom pipeline. Millennia is by C Prompt Games and published by Paradox.]
- No Civ VII GDC talk or rendering postmortem was found. The Intel interview with lead graphics programmer John Kloetzli ([Intel](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/)) was listed but its content not retrieved.
- No Unreal Engine 4X/hex strategy title with published tech rationale was found. [background knowledge, unsourced: some UE strategy titles exist, e.g. Manor Lords (UE4, city builder), but not hex 4X.]

## 2. Rendering techniques needed and how each engine supports them

### Takeaway
All required techniques (heightmap terrain, splat/biome blending, hex overlay via shader or decals, water/coast, fog of war, border meshes, GPU instancing, tilt-shift DoF, day/night, LOD) are standard and implementable in Unity URP, Unreal, and Godot 4. The deciding factors are tooling maturity and how easily an AI agent can author shaders as text. Unity (HLSL plus Shader Graph) and Godot (text `.gdshader`) are agent-friendly. Unreal's material graphs are binary `.uasset` and hard for agents to author.

### Cited Findings
- The canonical hex-map tutorial series (Catlike Coding "Hex Map", 27+ parts) is written for Unity. It covers hex mesh generation, terrain elevation, rivers, roads, water/shore shaders, terrain texture blending (splat arrays), fog of war, exploration, and unit pathfinding — [Catlike Coding Hex Map](https://catlikecoding.com/unity/tutorials/hex-map/)
- Red Blob Games' hex grid guide (axial/cube coordinates, distances, line-drawing, pathfinding) is the standard engine-agnostic reference — [Red Blob Games](https://www.redblobgames.com/grids/hexagons/)
- Unity HDRP/URP terrain features: GPU-instanced terrain details, SpeedTree, and Shader Graph grass materials. Unity ships Terrain demo scenes for HDRP and URP — [Unity blog](https://unity.com/en/blog/games/experience-the-new-unity-terrain-demo-scenes-for-hdrp-and-urp)
- Godot Terrain3D is a GPU-driven clipmap terrain with up to 10 LODs and foliage instancing. It supports Godot 4.4–4.6+, and version 1.1 is slated to add displacement/tessellation — [Terrain3D releases](https://github.com/TokisanGames/Terrain3D/releases); [80.lv](https://origin.80.lv/articles/this-free-terrain-system-for-godot-engine-enters-beta)
- Godot 4.5 (Sept 2025) added stencil buffer support, useful for selective rendering such as highlight masks and fog-of-war/border effects, plus a shader baker that cuts shader-compile stutter — [Godot 4.5 release](https://godotengine.org/releases/4.5/); [Digital Production](https://digitalproduction.com/2025/09/17/godot-4-5-new-buffers-better-access-more-control/)
- Godot 4.6 (late Jan 2026) includes an SSR rewrite and makes Jolt the default 3D physics. Maintenance release 4.6.3 shipped May 20, 2026 — [GameFromScratch](https://gamefromscratch.com/godot-4-6-released/); [GameFromScratch 4.6 sneak peek](https://gamefromscratch.com/godot-4-6-sneak-peek/)
- UE 5.7 (Nov 2025) made Substrate materials production-ready and added experimental Nanite Foliage. Reported CPU gains reach 50% on PS5 (memory API) — [CGPress](https://cgpress.org/archives/unreal-engine-5-7-released.html); [UE 5.7 release notes](https://dev.epicgames.com/documentation/unreal-engine/unreal-engine-5-7-release-notes)
- Three.js has a WebGPU renderer (`WebGPURenderer`) with WebGL fallback, but sources disagree on maturity ("experimental" vs "production-ready for most uses") — [Utsubo 2026](https://www.utsubo.com/blog/threejs-2026-what-changed); [svilenkovic](https://www.svilenkovic.com/3d/webgpu-three-js)

### Inferences
- A Civ-style hex map is not a "large open world" problem. It is a bounded map, roughly 100x60 to 200x120 hexes for an Israel-plus-region map, viewed from a tilted top-down camera. Nanite/Lumen (Unreal's main advantages) bring little benefit here, and the stylized look comes from art direction, post-processing (tilt-shift DoF, color grading), and custom terrain shaders. All four options can reach it. Unreal's advantage shrinks, and its costs (C++/Blueprint binary assets, heavy editor, Mac/Deck performance) remain.
- Real geography from DEM/SRTM: in every engine the pipeline is offline. Python/GDAL reprojects SRTM into a heightmap, samples per-hex elevation and biome, and exports JSON plus a 16-bit PNG/RAW heightmap. That pipeline is engine-agnostic and well suited to AI-agent scripting. [Inference; no engine-specific source checked]
- Thousands of units and buildings: Unity (GPU instancing, `Graphics.RenderMeshInstanced`, Entities Graphics), Godot (MultiMeshInstance3D), Three.js (InstancedMesh/BatchedMesh) all handle this. [background knowledge, unsourced]

### Gaps
- No direct performance benchmarks were found comparing engines on a hex terrain workload.
- No source confirmed Civ VII's specific terrain techniques (e.g., virtual texturing, hex blending method).

## 3. Fit with AI coding agents (text formats, CLI builds, headless tests, language, MCP servers)

### Takeaway
As of Oct 2026 all three native engines have MCP integrations. Unity's is official (open beta, May 2026). Unreal's is official but experimental and minimal (UE 5.8). Godot's are community-built (Coding-Solo/godot-mcp is the most popular). Godot is the most text-native: `.tscn`/`.tres`/`.gdshader` are human-readable, the engine is one small binary with headless mode. Unity is close behind with YAML scenes, C#, batchmode CLI, and the Test Framework. Unreal is the least agent-friendly because Blueprints and materials are binary `.uasset` files and C++ compile cycles are slow. Web stacks (TypeScript) are the most agent-native of all.

### Cited Findings
- **Unity AI with official MCP server**: open beta announced May 2026 for Unity 6 (6000.0+). The MCP bridge lets external agents (Claude Code, Cursor, Windsurf, Claude Desktop, VS Code Copilot) inspect scenes, read console output, edit scripts, and trigger editor actions. It requires the AI Assistant package, a Unity Cloud-connected project, and an active subscription/trial; MCP use does not consume AI credits. Setup is under Edit > Project Settings > AI > Unity MCP — [Unity blog](https://unity.com/blog/unity-ai-mcp-how-to-get-started). Unity AI succeeds the deprecated Unity Muse — [cinevva (secondary)](https://app.cinevva.com/news/2026-05-04-unity-ai-open-beta)
- Third-party vendors (e.g., Convai) extend Unity's official MCP server with their own tools — [Convai](https://convai.com/blog/convai-unity-mcp-build-ai-characters-with-codex-claude-code-and-cursor)
- **Unreal**: Epic reportedly shipped an experimental, first-party MCP plugin with UE 5.8, around State of Unreal 2026. It runs an MCP server inside the editor with a "minimal, foundational" toolset and limited read-back. Source is a competing vendor's blog, unverified against Epic docs — [StraySpark](https://www.strayspark.studio/blog/epic-official-mcp-plugin-ue5-8-vs-third-party). Epic is also reported to be bringing MCP-based AI agents into UEFN (Aug 2026, secondary) — [famesystems](https://famesystems.de/article/epic-games-brings-ai-agents-into-unreal-engine-as-co-developers-uu5cym8n). Third-party UE MCP servers include ChiR24/unreal_mcp (C++ Automation Bridge) and ziggymar/unreal-mcp — [sourcepulse](https://www.sourcepulse.org/projects/23868058); [mcpservers.org](https://mcpservers.org/servers/ziggymar/unreal-mcp)
- **Godot**: community MCP servers only.
  - Coding-Solo/godot-mcp: ~4.6k stars, MIT, updated Apr 2026. It launches the editor, runs projects, captures debug output, and edits scenes — [dev.co](https://dev.co/ai/mcp/godot-mcp)
  - struktured-labs/godot-mcp: runs GUT tests with structured results, checks parse errors, and exports — [glama](https://glama.ai/mcp/servers/@struktured-labs/godot-mcp)
  - Godot MCP Pro: paid, 167 tools — [glama](https://glama.ai/mcp/servers/neondeex/GodotMCP-Pro-Free-Client)
- Godot 4.6 added LibGodot, which lets Godot be embedded as a library in other applications — [GameFromScratch](https://gamefromscratch.com/godot-4-6-released/)
- Web stack: Steam integration via steamworks.js (Node binding) inside Electron. Tauri uses the system webview and some developers report missing WebGL2 on some platforms. Electron ships a known Chromium, so behavior is predictable at the cost of size — [three.js forum](https://discourse.threejs.org/t/best-solution-for-running-three-js-in-a-desktop-app/47485); [Summer Engine guide](https://www.summerengine.com/make-great-games/threejs-game-on-steam)

### Inferences
- Agent-friendliness ranking: Web/TypeScript > Godot (GDScript/C#, text scenes, headless `--headless`, tiny binary) ≈ Unity (C#, YAML scenes that are verbose and full of GUIDs and fileIDs, `-batchmode -runTests` CLI, the largest game-engine training corpus) >> Unreal (binary assets, Blueprints not diffable, long C++ compiles, large editor).
- C# has very large LLM training coverage. Unity C# in particular has the most game-engine-specific examples online. GDScript has less training data, and LLMs sometimes emit Godot 3 syntax for Godot 4. Pinning docs or version notes in CLAUDE.md mitigates this. [background knowledge, unsourced]
- Unity's official MCP needs a paid or trial subscription and Unity Cloud. That is a minor cost and lock-in consideration. Godot's MCPs are free but community-maintained.
- Best agent workflow regardless of engine: keep the simulation as a pure engine-independent library (C#, or TypeScript for web) with unit tests the agent can run headlessly in seconds. The engine becomes a thin view layer.

### Gaps
- Epic's official MCP plugin was not verified on epicgames.com, and its tool list is unknown.
- No objective benchmark exists of LLM coding accuracy per engine (GDScript vs Unity C# vs UE C++).
- Unity 6 hot reload (Domain Reload disabled, third-party "Hot Reload" asset) was not researched in this pass.

## 4. Licensing/cost, Steam deployment, Mac/Steam Deck, performance

### Takeaway
The Unity Runtime Fee is dead (cancelled Sept 2024). Unity Personal is free under $200k revenue and funding. Pro is $2,310/seat/yr from Jan 12, 2026. Unreal is free until $1M lifetime gross per product, then 5% (Epic Games Store sales are royalty-free, or the rate drops to 3.5% with EGS simultaneous launch). Godot is MIT and free. Web stacks are free (MIT). All can ship to Steam on Windows/Mac/Linux, but Unreal on Mac/Steam Deck is the weakest.

### Cited Findings
- Unity Runtime Fee was scrapped in Sept 2024. Unity now considers price changes annually. Pro is $2,310/seat/yr or $210/month after a 5% rise effective Jan 12, 2026. Personal is free for under $200k revenue and funding. Havok Physics is no longer bundled with Pro from 6.3 LTS — [Unity pricing updates](https://unity.com/products/pricing-updates); [CG Channel](https://www.cgchannel.com/?p=171673); [80.lv](https://80.lv/articles/unity-announces-its-upcoming-2026-price-changes)
- Unity 6.3 LTS released Dec 4, 2025, supported through Dec 2027. Unity 6.4 has released and 6.5 was in beta as of early 2026 — [Unity blog](https://unity.com/ru/blog/unity-6-3-lts-is-now-available); [Unity discussions](https://discussions.unity.com/t/in-case-you-missed-it-february-march-edition/1715335)
- Unreal: 5% of gross revenue above the first $1M lifetime per product, reported quarterly, with no royalty in quarters under $10k. Royalty-free on Epic Games Store sales — [Unreal FAQ](https://www.unrealengine.com/en-US/faq?active); [UE EULA](https://www.unrealengine.com/eula); [custom license](https://www.unrealengine.com/custom-license). A 3.5% rate applies if launching on EGS simultaneously or first (effective Jan 1, 2025) — [Twinmotion/Epic FAQ](https://map.twinmotion.com/en-US/faq)
- UE 5.7 on Apple Silicon: one unverified developer report shows 10–15 FPS on an M4 for an unnamed PC title, with no official MetalFX integration — [Apple dev forums](https://origin-devforums.apple.com/forums/profile/CanhNguyen)
- Godot is MIT-licensed. 4.5 shipped Sept 15, 2025 and 4.6 on Jan 26, 2026 — [Godot 4.5](https://godotengine.org/releases/4.5/); [Godot blog](https://godotengine.org/blog/release)
- Web precedents on Steam: CrossCode (NW.js) and Vampire Survivors (Phaser/JS at 1.0) — [Summer Engine guide](https://www.summerengine.com/make-great-games/threejs-game-on-steam)
- Old World, a Unity 4X, migrated to Unity 6.3 in Feb 2026 — [rutab.net](https://rutab.net/b/video-games/2026/02/19/razrabotchik-civilization-4-vypustil-krupnoe-obnovlenie-dlya-svoey-strategii-old-world.html)

### Inferences
- For a tiny team below $200k, Unity Personal costs $0. Above that, roughly $2.3k per seat per year. The Unity MCP/AI features need a subscription; it is unclear whether Personal plus a trial qualifies. Verify this.
- Steam Deck is Linux/Proton. Unity and Godot both export native Linux or run Windows builds via Proton. Electron also runs on Linux. A stylized hex map at 800p is achievable in Unity URP or Godot (Forward+ or Mobile renderer). HDRP and UE5 Lumen are heavy on Deck. [background knowledge, unsourced]
- Mac: Unity (Metal) and Godot (Metal backend in 4.4+ [background knowledge, unsourced]; otherwise Vulkan via MoltenVK) are well supported. Unreal on Mac has weaker performance, as the M4 report suggests, and needs Xcode. Electron runs WebGL via ANGLE/Metal.

### Gaps
- No official Steam Deck benchmarks were found for any engine on a strategy-map workload.
- Whether Unity AI/MCP works on Unity Personal was not confirmed.
- Godot console porting, not needed for Steam, was not researched.

## 5. UI tooling for data-heavy simulation UI

### Takeaway
Civ VII validates web-tech UI (Coherent Gameface: HTML/CSS/JS) for AAA strategy. That approach is the most AI-agent-friendly, but it costs a middleware license in native engines and caused Firaxis launch-quality problems. Unity UI Toolkit (UXML/USS, CSS-like, text files) is the closest native analog. Godot Control nodes are capable and text-serialized but less web-like. Unreal UMG/Common UI is Blueprint and binary-heavy. A web stack gets HTML/CSS UI natively and for free.

### Cited Findings
- Civ VII UI uses Coherent Gameface (HTML/CSS/JS). Firaxis cited one consistent UI across PC, console, and handheld, plus Chrome DevTools profiling — [Coherent Labs](https://coherent-labs.com/blog/casestudies/firaxis-case-study/); [Side](https://side.inc/lab/sid-meiers-civilization-vii-case-study); [Gameface product](https://coherent-labs.com/products/coherent-gameface)
- Firaxis admitted the Civ VII launch UI underperformed expectations and began a longer-term UI architecture rework — [2K](https://civilization.2k.com/de-DE/civ-vii/game-guide/gameplay/loading-screen/)
- Coherent has Unity integration documentation (historically Coherent UI for Unity3D) — [Coherent docs](https://coherent-labs.com/Documentation/unity/namespaces.html)
- Godot 4.5 added partial screen-reader support (AccessKit) for Control nodes — [It's FOSS](https://itsfoss.com/news/godot-4-5-release.md)
- Unity community material covers a UI Toolkit World Space render mode in the 6.x line (tutorial, not changelog) — [Unity discussions](https://discussions.unity.com/t/in-case-you-missed-it-february-march-edition/1715335)

### Inferences
- Civ VII's UI problems were design and UX issues, not a failure of web tech. The tech itself is endorsed by Firaxis's choice.
- For an agent-built sim with many panels and charts, HTML/CSS (web stack) or Unity UI Toolkit (UXML/USS) gives the agent the most leverage. Chart libraries (e.g., ECharts or Chart.js in web; XCharts in Unity [background knowledge, unsourced]) are readily available.
- A hybrid is possible: a native engine renders the map with an embedded web UI. Options include Gameface (commercial; pricing not public), or OneJS / ReactUnity, which bring a React/TS layer on top of Unity UI Toolkit [background knowledge, unsourced]. This adds complexity.

### Gaps
- Gameface pricing and indie licensing terms were not found.
- Unity UI Toolkit runtime maturity for complex game HUDs (data binding in Unity 6) was not deeply verified in this pass.

## 6. Simulation architecture (deterministic core, data-driven content, save/load, modding)

### Takeaway
Shipped 4X games separate the sim from rendering and expose data-driven content. Civ VII exposes a JS UI/scripting layer, and Old World/Humankind are moddable via XML/data files [partly background knowledge]. The engine-neutral recommendation is a pure, deterministic sim library (C# for Unity/Godot-C#, TypeScript for web) driven by JSON/YAML content, with engine code only rendering state snapshots and sending commands.

### Cited Findings
- Civ VII's UI/script layer uses `engine.trigger` events and `engine.createJSModel` data models between the JS UI and native game. That is a clear sim/UI boundary (community analysis) — [CivFanatics](https://forums.civfanatics.com/threads/scripting-runtime-information.695101/); [Coherent `engine` docs](https://coherent-labs.com/Documentation/cpp/namespaces.html)
- Godot test tooling via MCP supports running GUT unit tests headlessly with structured results, which enables agent-driven TDD on the sim — [glama struktured-labs](https://glama.ai/mcp/servers/@struktured-labs/godot-mcp)
- A secondary article claims Millennia uses Unity ScriptableObjects for data-driven 4X content (unverified) — [foro3d](https://foro3d.com/en/2026/mayo/millennia-evolucion-visual-y-mecanicas-4x-en-unity-con-c.html)

### Inferences
- A plain C# (.NET) sim library works in both Unity and Godot (Godot 4 .NET build), giving engine portability: start in Godot, move to Unity, or the reverse. Note that Godot 4 C# projects cannot export to web [background knowledge, unsourced; verify the status as of 4.6], which is irrelevant for Steam.
- Determinism: avoid floats in sim logic (fixed-point or integers), use a seeded RNG, and make turn-based command logs replayable. This gives free save/load (serialize state or command log) and multiplayer-readiness. Unity ECS/DOTS is unnecessary for a turn-based sim of this scale.
- Modding: JSON/YAML content plus the sim's public API. Web stacks are inherently moddable (JS), and Unity games commonly use Harmony/BepInEx-style patching or official data mods.

### Gaps
- No primary source on Humankind/Old World sim architecture or mod pipeline was retrieved.

---

## Ranking (synthesis for this use case: tiny team, AI agents write most code, Civ-VII-quality stylized hex map, Steam Win/Mac/Deck)

1. **Unity 6.3 LTS (URP) + C#**. This is the best balance. It has proven genre precedent (Humankind, Endless Legend, Old World on 6.3, Millennia), the Catlike Coding Hex Map tutorial as a direct blueprint, an official MCP server (2026), the largest game-engine C# training corpus, CLI batchmode builds and tests, URP that runs on Mac and Deck, and UI Toolkit (CSS-like) for data-heavy UI. Costs: free under $200k, then $2,310/seat/yr. Official MCP needs a subscription. YAML scenes are noisy for agents, and there is residual trust risk from the 2023 runtime-fee episode (since cancelled). Use URP, not HDRP: HDRP is heavy on Deck and Mac and offers little gain for a stylized look.
2. **Godot 4.6 (C# or GDScript)**. This is the most agent-friendly native engine: text scenes, tiny headless binary, MIT license, no royalties, several community MCP servers, Terrain3D. Weaknesses: no shipped high-fidelity 3D 4X precedent, a less mature 3D renderer and post-processing for AAA-polish looks, less training data for GDScript (Godot 3/4 confusion), and community-only MCP. It is a strong choice if budget or freedom matters most and the art style is stylized rather than realistic.
3. **Web stack (Three.js r17x/WebGPU or Babylon.js + TypeScript, wrapped in Electron + steamworks.js)**. This has the highest agent leverage (TS plus HTML/CSS UI, as Civ VII's UI does) and the fastest iteration. Weaknesses: no shipped high-visual 3D 4X on Steam, WebGPU maturity disputed, higher performance overhead on Deck, Electron bloat and Tauri WebGL2 gaps, and no editor for level and art work. It is strong for a UI-heavy sim with a modestly detailed map. It is a risky path to Civ-VII-grade visuals.
4. **Unreal Engine 5.7/5.8**. It has the best raw rendering, but that rendering (Nanite, Lumen) targets open worlds rather than tilted-camera hex maps. It is the worst fit for AI agents (binary Blueprints and materials, slow C++ iteration, official MCP experimental and minimal), heavy on Steam Deck and Mac (an unverified M4 report shows 10–15 FPS), carries a 5% royalty above $1M, and has no 4X genre precedent found.

Alternative worth noting: a hybrid of Unity (map) with an HTML/JS UI layer (Gameface, or a React-on-UI-Toolkit layer like OneJS) mirrors Civ VII's architecture. It adds integration cost, so only consider it if UI volume dominates.
