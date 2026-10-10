# Developer setup (verified 2026-10-09 on macOS 27.0.1, arm64)

- .NET SDK 10.0.401: `brew install dotnet`
- Unity Hub 3.22.2: `brew install --cask unity-hub`. Sign in and activate a Personal license.
- Unity Editor 6000.3.25f1 (6.3 LTS), changeset e1dba0a9aba4, installed through the Hub CLI:
  `"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install -v 6000.3.25f1 -c e1dba0a9aba4 -a arm64 -m webgl -m mac-il2cpp`
  Modules: Web Build Support, Mac Build Support (IL2CPP).
- `export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"`
- Python tools: `uv` (0.12.5 verified). Dependencies are declared inline in each tools/*.py script.
- ffmpeg (Homebrew) is used for flythrough videos.

## After cloning
- Run `tools/build_sim_for_unity.sh` before opening `unity/`. The sim DLLs are build output and are git-ignored.

## Builds and browser checks (M3)
- Web build: `"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"`
  (`-compression` is `disabled`, `gzip` or `brotli`; output in `unity/Builds/Web-<compression>/`, about 2 minutes).
- macOS IL2CPP build: `"$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile "$PWD/unity-build-mac.log"`
  (output `unity/Builds/Mac/Statecraft.app`).
- Browser check: `uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&smoke=5" --expect "GOLDEN OK 1952" --expect "SMOKE OK 5 turns"`.
  It uses Chrome for Testing from the Playwright cache (`~/Library/Caches/ms-playwright/chromium-1228` on this Mac;
  elsewhere `npx playwright install chromium`).
- Page flags: `?golden` (golden check), `?smoke=N` (play N turns through the UI; bare `?smoke` = 5, `?smoke=0` = off),
  `?profile` (terrain profile), `?camlog` (log the camera distance). Desktop players take `-golden`, `-smoke N`, `-profile`
  and `-camlog`. The console also shows `GAME OK <date> turn <n> hash <h>` at start and `TURN <n> <date> hash <h>` per turn.
- A player build rewrites some URP asset and player-setting fields as build-time caches; drop them afterwards with
  `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.
- Play a build locally: `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.
  A Brotli build needs `Content-Encoding: br`; serve it with web_check's handler (command in `docs/m3-release.md`).
