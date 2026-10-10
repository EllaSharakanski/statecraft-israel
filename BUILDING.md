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

Troubleshooting: if the Console shows compile errors from a package that isn't in our `Packages/manifest.json` (for example an AI or MCP connector such as `com.ivanmurzak.unity.mcp` that added itself), remove that package from your local manifest or update it. Unity can't run while there is any compile error.

## Tests

The test commands, and why Unity tests are not in CI, are in [`CONTRIBUTING.md`](CONTRIBUTING.md) ("Rules every change follows", "What CI runs"). The browser check of a build is `tools/web_check.py` ([`docs/m3-release.md`](docs/m3-release.md)).

## Repository layout

- `sim/`: the simulation, a pure C# library (netstandard2.1) with xUnit tests. No Unity dependency.
- `content/`: the game's data (YAML and JSON): people, offices, parties, events, map data. Every fact carries `sources:`.
- `unity/`: the Unity project, which presents the sim. No game rules live here.
- `tools/`: Python data tools (map and rainfall pipelines, browser check), run with `uv`.
- `scripts/oss/`: repository policy checks.
