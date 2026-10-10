# Reviewing an outside pull request safely

An outside pull request is untrusted input aimed at a machine that holds the maintainer's credentials (GitHub, Unity,
ElevenLabs, AI agents). In this repository, opening or testing a pull request **runs its code**:

- **Unity** compiles and runs every script under `unity/Assets/Editor/` (and `[InitializeOnLoad]` code anywhere in
  `Assets/`) as soon as the project is opened or imported. Opening the PR's branch in the editor is running it.
- **`dotnet build` / `dotnet test`** run MSBuild targets from every `.csproj` and from `Directory.Build.props`.
- **`uv run`** installs the dependencies a script declares inline and runs the script. `pytest` runs `conftest.py`
  at import.

So: **read the diff before running anything, and never run a pull request's code on your own machine before the review
verdict.** CI runs the PR's code on GitHub's machines with a read-only token and no secrets; that is where it belongs.

**Any diff under `.github/` can make every required check green.** CI runs the workflow file from the pull request
itself, so a PR that edits `.github/workflows/ci.yml` can turn `Changelog`, `Repo policy` or any other check into a job
that always passes. Green checks on such a PR mean nothing; read the `.github/` diff first, and the CODEOWNERS review is
the real gate.

## The profile

```sh
# 1. A throwaway checkout of only the pull request, in an empty directory:
rm -rf /tmp/sc-review && mkdir -p /tmp/sc-review && cd /tmp/sc-review
git clone --depth 50 https://github.com/eyalgolan/statecraft-israel.git repo
cd repo && git fetch origin pull/<PR#>/head:pr && git checkout pr

# 2. Read the diff first, especially build and editor surfaces:
git diff origin/main...pr --stat
git diff origin/main...pr -- '*.csproj' 'sim/Directory.Build.props' 'unity/Assets/Editor/**' '.github/**' 'scripts/**' 'tools/**'

# 3. To run anything, run it in a container with no credentials and no network:
docker run --rm -it --network none --cap-drop ALL --security-opt no-new-privileges \
  -v "$PWD":/work:ro -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash
```

Rules:

1. **No credential volumes.** The container gets the checkout only: no `~/.config/gh`, no `~/.claude`, no Unity
   licence, no `.env` file.
2. **No network** (`--network none`). An injected payload's first move is usually to phone home.
3. **AI review only on a throwaway credential.** If you ask an AI agent to review the PR, run it inside the container
   with a separate, low-limit key made for that review, never your own session. Treat its verdict as text you read,
   never as a command you run.
4. **Never open the PR's `unity/` folder in your Unity editor** until the diff has been read and approved. Unity edit-mode
   tests for an outside PR are run by the maintainer after merge-readiness, on a branch the maintainer has read.
5. **No AI agent and no secret in CI for pull requests**, and never `pull_request_target` with a checkout of the PR's code.

This protects the review, not the judgement: merging is still a human decision on what was read.
