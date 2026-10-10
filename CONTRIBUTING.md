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
   - the tools tests: the command is in `tools/README.md`;
   - Unity EditMode tests, if you touched `unity/`. The command is in `docs/dev-setup.md`. There must be 0 `warning CS` in the log.
4. **Text the player sees** has no internal ids, gives dates as "15 May 1948", and shows money as "IL 1.2m" or "$1.2m". The tests enforce this.
5. **Keep the pull request focused:** one change per PR, and no unrelated refactors.

## History and content rules

- **Every fact needs a source.** Every event, minister, office term, date and number in `content/` carries `sources:` with a URL and the date you checked it. A claim without a source can't be merged.
- **No invented quotes.** Paraphrase, and never put words in a real person's mouth.
- **Framing.** The project tells the story from the Israeli perspective. Contested narratives are not shown as parallel accounts.
- **Sensitive events.** Events tagged `holocaust`, `terror`, `massacre` or `war-casualties` need the maintainer's sensitivity review before they can ship. A release build refuses to package until that review is done.
- **Reporting a mistake.** Open an issue with the source that shows the correct fact.

## AI-assisted contributions

AI-assisted work is welcome. Say so in the pull request: which parts, and how you checked them. You are responsible for every line you submit. For content, check every fact against its source yourself, and never take one from an AI tool unchecked.

## Pull requests

- Base your branch on `main`. History on `main` is protected, so no force-pushes.
- In the description, say what changed and why. Paste the test commands you ran and their output; a description of the output doesn't count.
- The maintainer reviews every PR. CI is being set up; until then, the pasted test output is the evidence.

## Security

Never commit keys, tokens or `.env` files. Secret scanning and push protection are on. To report a vulnerability, use **Security → Report a vulnerability** on the repository page (GitHub private reporting); don't open a public issue.

## Conduct

Be respectful. Argue about facts with sources, not with people. Political or personal attacks are not tolerated.
