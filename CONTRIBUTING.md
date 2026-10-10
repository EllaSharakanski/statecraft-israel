# Contributing to Statecraft: Israel

Thanks for helping. This file covers how to get set up, the rules every change follows, and how pull requests are reviewed.

## Ways to help

- **Code:** the C# simulation (`sim/`), the Unity client (`unity/`), and the Python map and data tools (`tools/`).
- **History:** fact-checking, sourcing, and drafting new events, ministers and dates (`content/`).
- **Art:** 3D settlements, vegetation and units for the era-correct look. See the roadmap in the [README](README.md).
- **Design and writing:** event choices, balance, and the text the player reads.

If you're unsure where to start, open an issue first and describe what you want to do.

## Licence of your contribution

There is no CLA and no sign-off. By opening a pull request you agree that your contribution is licensed under this repository's licences, as GitHub's Terms of Service (section D.6) also say:

- code under [MIT](LICENSE);
- `content/` under [CC BY-SA 4.0](LICENSES/CC-BY-SA-4.0.txt);
- other original material under [CC BY 4.0](LICENSES/CC-BY-4.0.txt).

[LICENSING.md](LICENSING.md) lists which licence covers each path. Only contribute work you have the right to license this way. If you add a third-party file, include its source and licence. Material from the Unity Asset Store is never accepted.

## Setup

Follow [docs/dev-setup.md](docs/dev-setup.md). In short:

- **Sim and tools only:** .NET SDK 10 and `uv`. Unity is not needed.
- **Unity work:** Unity 6000.3.25f1 (6.3 LTS) with Web Build Support, a free Unity account, and running `tools/build_sim_for_unity.sh` before you open `unity/`.

## Rules every change follows

1. **Game rules live in the sim, not in Unity.** Unity draws, reads input and calls sim helpers. A rule written in a presenter is a bug.
2. **Determinism.** The same seed and commands must give the same state hash.
   - If your change alters the simulation on purpose, the golden hashes will change.
   - Record the new values everywhere they are pinned (see [docs/determinism.md](docs/determinism.md)), with a one-line reason in its history table.
3. **Tests first.** Add or change a test that fails without your change, then make it pass. Run:
   - `dotnet test sim/Statecraft.sln`, which must pass with 0 warnings (warnings are errors);
   - the tools tests (they need the .NET SDK too: one runs the sim's release gate):
     `uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q`;
   - the repository policy checks:
     `uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q` and `uv run scripts/oss/check_repo.py`;
   - Unity EditMode tests, if you touched `unity/` (`-buildTarget OSXUniversal` keeps an earlier WebGL build from leaving the editor on the wrong platform):
     `"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log`. There must be 0 `warning CS` in the log.
4. **Text the player sees** has no internal ids, gives dates as "15 May 1948", and shows money as "IL 1.2m" or "$1.2m". The tests enforce this.
5. **Keep the pull request focused:** one change per PR, and no unrelated refactors.
6. **Changelog.** If you change `sim/`, `content/` or `unity/` (tests excepted), add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). CI's `Changelog` job checks it; the maintainer can add the `no-changelog` label to a change no player would notice.

## History and content rules

- **Every fact needs a source.** Every event, minister, office term, date and number in `content/` carries `sources:` with a URL and the date you checked it. A claim without a source can't be merged.
- **No invented quotes.** Paraphrase, and never put words in a real person's mouth.
- **Framing.** The project tells the story from the Israeli perspective. Contested narratives are not shown as parallel accounts.
- **Sensitive events.** Events tagged `holocaust`, `terror`, `massacre` or `war-casualties` need the maintainer's sensitivity review before they can ship. A release build refuses to package until that review is done.
- **Reporting a mistake.** Use the **Historical accuracy** issue form; it asks for the source that shows the correct fact.

## AI-assisted contributions

AI-assisted work is welcome. Say so in the pull request: which parts, and how you checked them. You are responsible for every line you submit. For content, check every fact against its source yourself, and never take one from an AI tool unchecked.

## Pull requests

- Base your branch on `main`. History on `main` is protected, so no force-pushes.
- In the description, say what changed and why. Paste the test commands you ran and their output; a description of the output doesn't count.
- The maintainer reviews every PR (`.github/CODEOWNERS`); a push after approval needs a fresh approval. PRs are squash-merged and keep you as the author.

## What CI runs

Every pull request and every push to `main` runs `Sim (dotnet test)`, `Tools (pytest)`, `Repo policy`, and, on pull requests, `Changelog`. The jobs use no secrets, call no paid API, run no AI agent and push nothing.

**Unity EditMode tests are not in CI.** Running Unity in CI needs the maintainer's Unity account credentials stored as repository secrets. GitHub never gives secrets to pull requests from forks, so the job could not run on the pull requests that most need it, and those credentials would sit on machines that run outside code. So the Unity tests are a local gate: paste their result in your PR, and the maintainer re-runs them before merging any change under `unity/`.

## Security

Never commit keys, tokens or `.env` files. Secret scanning and push protection are on. To report a vulnerability, use **Security → Report a vulnerability** on the repository page (GitHub private reporting); don't open a public issue. What is in scope and what to expect: [SECURITY.md](SECURITY.md). How the maintainer reviews outside pull requests without running their code: [docs/UNTRUSTED_PR_REVIEW.md](docs/UNTRUSTED_PR_REVIEW.md).

## Conduct

Be respectful. Argue about facts with sources, not with people. Political or personal attacks are not tolerated. The full [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) applies in every project space, including its section on historical and political discussion. The content rules, and how to dispute a fact, are in [docs/CONTENT_POLICY.md](docs/CONTENT_POLICY.md).
