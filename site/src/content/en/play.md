There is no hosted build yet. A browser build you can play with one click is planned for M4. Until then the game runs from source on your own machine.

## Run it yourself

1. Install the tools in [docs/dev-setup.md](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md): .NET 10, Unity 6000.3.25f1 with Web Build Support, and `uv`.
2. Run `tools/build_sim_for_unity.sh`.
3. Build the web version. The command is in [docs/dev-setup.md](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md).
4. Run `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.

The exact commands, the tests and the browser check are in [BUILDING.md](https://github.com/eyalgolan/statecraft-israel/blob/main/BUILDING.md).

## What you can play

Turns from 15 May 1948 to the end of 1951: the Cabinet, the Budget and Defence screens, sourced events at their real dates, the “Why?” panel, and autosave. Settlements and trees are still placeholder art, and there is no diplomacy or war mode yet.

## For contributors: putting the build on this site

This site is served by GitHub Pages, which cannot set response headers. That decides how the browser build must be made before it can go here:

- **Decompression Fallback.** Unity's Brotli and gzip builds need the server to send `Content-Encoding`, which GitHub Pages cannot do. A build for this site needs Unity's Decompression Fallback: the compressed files get the `.unityweb` extension and the loader decompresses them in JavaScript, at the cost of a larger loader. Unity recommends it when the server cannot be configured ([Unity manual: deploying a Web application](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-deploying.html)). Today `Build.Web` in `unity/Assets/Editor/Build.cs` always turns the fallback off, because itch.io serves `.br` files correctly; a Pages build needs an option to turn it on.
- **Single-threaded.** Multithreaded Web builds need the COOP and COEP headers, which GitHub Pages cannot send either. The project's builds are single-threaded now (`webGLThreadsSupport: 0`), and they must stay that way for this site.
- **Shareable.** Only a `-shareable` build may be published: it refuses while any sensitive event or photograph lacks its sensitivity review ([docs/m3-release.md](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/m3-release.md)).
- **Not committed.** The M3 build was 23 MB in 51 files, and no file in the repository may pass 10 MB. The deploy workflow would add the build to the site at deploy time, not from git.
