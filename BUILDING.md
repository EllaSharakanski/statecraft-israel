# Building Statecraft: Israel

The README gives the short version. This page has the exact commands. The full developer setup, including the Unity
Hub install command, is in [`docs/dev-setup.md`](docs/dev-setup.md).

## Requirements

- Unity **6000.3.25f1** (6.3 LTS) with Web Build Support. The exact version and changeset are in
  [`unity/ProjectSettings/ProjectVersion.txt`](unity/ProjectSettings/ProjectVersion.txt); other versions are not
  tested. You need your own Unity licence, activated in Unity Hub (sign in, then Preferences → Licenses) before any
  batch-mode command; Unity Personal is free within Unity's terms.
- .NET SDK 10.0.401 and [uv](https://github.com/astral-sh/uv) 0.12.5.
- No Git LFS: a plain `git clone` is complete.

## Run locally

```bash
git clone https://github.com/eyalgolan/statecraft-israel.git && cd statecraft-israel
tools/build_sim_for_unity.sh                     # the sim DLLs Unity loads (build output, not committed)
export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"   # macOS path
# Windows: "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"; Linux: ~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
python3 -m http.server 8000 --directory unity/Builds/Web-disabled   # then open http://localhost:8000/
```

A build rewrites some cached settings. Drop them afterwards with
`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.

## In the editor

Unity Hub → **Add** → select `unity/`. Run `tools/build_sim_for_unity.sh` again after every change under `sim/`.

### First open in Unity

The game scene is `Assets/Map.unity`, and it opens automatically. If it doesn't (you see an empty "Untitled" scene), double-click `Map` in the Project window's Assets folder. Then press Play.

Troubleshooting:

- `error CS0246: The type or namespace name 'PlayerPrefsBool' could not be found`: the Unity-MCP package (`com.ivanmurzak.unity.mcp`) was installed without its OpenUPM registry (`https://package.openupm.com`), so its dependency `extensions.unity.playerprefsex` is missing. Restore `Packages/manifest.json` and `Packages/packages-lock.json` from git (`git checkout -- unity/Packages`); they already carry the package and the registry.
- Compile errors from a package that isn't in our `Packages/manifest.json`: remove that package from your local manifest or update it. Unity can't run while there is any compile error.
- macOS says "Unity is damaged and can't be opened. Move to Trash": this can happen when the editor is launched outside Unity Hub after Hub installed modules (such as Mac Build Support) inside the app. Click **Cancel**, never Move to Trash. Open the project through Unity Hub. If Hub refuses too, reinstall that editor version (6000.3.25f1) through Unity Hub.

## Working with an AI agent in Unity (Unity-MCP)

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (Apache-2.0) is the project's standard way to let an AI agent (Claude Code, Cursor, Copilot and others) work in the Unity editor: read the Console, inspect scenes, run tests. It runs locally, with no account and no cloud.

- **Install:** nothing to do. The package (`com.ivanmurzak.unity.mcp` 0.93.3) and the OpenUPM registry it needs are already in `unity/Packages/manifest.json`. On the first open, the plugin downloads its NuGet DLLs from nuget.org into `Assets/Plugins/NuGet/` (git-ignored) and its local server from the plugin's GitHub releases into `Library/`. Then Unity compiles once more.
- **Open** Window → AI Game Developer — MCP.
- **Connection → Custom** (local). Set the Server URL to `http://localhost:27766` and the transport to `http`. Never choose Cloud, and never sign in: nothing in this project needs an ai-game.dev account. These settings are per user, in `unity/UserSettings/AI-Game-Developer-Config.json` (git-ignored, and it holds your local token), so every contributor sets them once.
- **Choose your agent** and click **Configure**, or click **Generate** to write skill files instead. The files these steps write (`.mcp.json`, `unity/.claude/` and the other agent folders) are git-ignored; don't commit them.
- **The rules still apply:** no game rules in Unity (they live in the sim), tests first, sources for every fact in `content/`, and disclose the AI assistance in your pull request (see "Rules every change follows" and "AI-assisted contributions" in [CONTRIBUTING.md](CONTRIBUTING.md)).
- **Security:** the local server lets the agent run editor actions, including editing files and executing code in the editor. Keep it local: never expose port 27766 (no port forwarding, tunnels or `0.0.0.0` binds), and stop the server when you're done.
- **Not connecting?** Connect only works in an interactive editor: in batch mode (tests and builds) the plugin is held offline on purpose, and its window doesn't open. Check that the Server URL port matches the running server (the plugin's default port is derived from the project path, which is why everyone sets 27766), that the server shows Running, then click Connect. The first attempt can fail while the server is still starting; the plugin retries. If the editor is stopped without quitting, the `gamedev-mcp-server` process can stay behind on the port: stop it with the window's Stop button or by its PID.
- **Batch mode and builds:** `UnityMcpGuard` (`unity/Assets/Editor/UnityMcpGuard.cs`) keeps the plugin from connecting or starting its server in batch mode (tests and builds), and drops its assemblies from every player build. The browser build's golden hashes don't change.
- **Leave one change uncommitted:** the plugin adds `UNITY_MCP_READY;UNITY_MCP_DEPS_7` to the scripting defines in `unity/ProjectSettings/ProjectSettings.asset` once its DLLs are in place. Don't commit that change: on a fresh clone the defines would arrive before the DLLs, and the plugin wouldn't compile. A policy test refuses it. The post-build cleanup (`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`) drops the defines too; the plugin adds them back on the next open, at the cost of one extra compile.

## Tests

The test commands, and why Unity tests are not in CI, are in [`CONTRIBUTING.md`](CONTRIBUTING.md) ("Rules every change follows", "What CI runs"). The browser check of a build is `tools/web_check.py` ([`docs/m3-release.md`](docs/m3-release.md)).

## Repository layout

- `sim/`: the simulation, a pure C# library (netstandard2.1) with xUnit tests. No Unity dependency.
- `content/`: the game's data (YAML and JSON): people, offices, parties, events, map data. Every fact carries `sources:`.
- `unity/`: the Unity project, which presents the sim. No game rules live here.
- `tools/`: Python data tools (map and rainfall pipelines, browser check), run with `uv`.
- `scripts/oss/`: repository policy checks.
