# Statecraft: Israel: open-source polish after publish, implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the already-public repository `eyalgolan/statecraft-israel` (main at `602183f`) up to the project's open-source bar, in the owner's priority order, by amending the published files instead of recreating them.

**Architecture:** Root policy files (`SECURITY.md`, `CODE_OF_CONDUCT.md`, `TRADEMARK.md`, `NOTICE`, `THIRD-PARTY-NOTICES.md`, `ASSETS.yaml`, `README.he.md`, `BUILDING.md`, `CHANGELOG.md`) are added, and the published `README.md`, `CONTRIBUTING.md`, `LICENSING.md`, `.gitignore`, `promo/gen.py` and `docs/m3-release.md` are amended in place. `.github/` gets one `ci.yml` (sim, tools, repo policy, changelog), issue forms, a PR template and CODEOWNERS. `scripts/oss/` holds small Python checkers with pytest tests (`check_repo.py` for the tip, `scan_history.py` for history, `changelog_check.py`, `pin_actions.py`, `commons_licences.py`), the ruleset JSON with an owner-only apply script, and the release gate. Nothing under `sim/`, `content/`, `unity/` or `tools/` changes, except `content/events/*.yaml` statuses, which only the owner changes (D11).

**Tech Stack:** Python through uv 0.12.5 (pytest 9.1.1, pyyaml 6.0.3, requests 2.32.5); .NET SDK 10.0.401; GitHub Actions (`actions/checkout`, `actions/setup-dotnet`, `astral-sh/setup-uv`, pinned by SHA); GitHub rulesets; `gh` CLI (read-only for agents); colima/docker for the Linux CI parity run.

**Spec:** the lessons and the audit (`research_notes/open source/no_human-lessons.md`, `research_notes/open source/israel-game-audit.md`, both untracked and git-ignored in the `main` checkout); the independent review `.worktrees/m3/.superpowers/sdd/2026-10-10-statecraft-m3/open-source-plan-review.md` (its responses are the last section of this plan); the README report `reports/Open source indie game READMEs.md`; and the design spec `docs/superpowers/specs/2026-10-09-statecraft-israel-design.md` (§1, §5).

**Base:** `main` at `602183f` (`git -C ~/israel-game show 602183f:<path>` reads any file). Every code block in this plan was run in a scratch clone of `602183f`, task by task: at the end of each task its tests pass and CI's policy check prints `OK` (Task 1: 1 test; Task 3: 54; Task 7: 72; Task 10: 84; Task 13: 98; Task 14: 102, with the full `check_repo.py` `OK`). `check_repo.py --release` refuses exactly on the owner items, and `scan_history.py --ref main` reports 0 open secret-shape findings (see the review response tables).

## Owner rulings (binding)

1. **No history scrubbing or rewriting, and no history enforcement** (no signed-commit or linear-history rules). A real secret in history is fixed by revoking or rotating it (owner step).
2. **README hero:** the AI concept art `promo/final/03-gameplay-1936.png`, published as `docs/images/hero-concept-1936.jpg`, at the top with a concept-art caption; real current-build screenshots below. Its provenance is in `ASSETS.yaml`; if the ElevenLabs terms forbid redistribution, that is a blocker for the owner, and no agent drops the image. A planned swap to a real screenshot is in `docs/RELEASING.md`.
3. **Content perspective:** Israeli framing, every fact sourced, no invented quotes.
4. **README patterns** follow the README report: language bar, captioned hero, separate code and asset licence badges, a one-line pitch, a real dated 1+2 screenshot grid, a `[!NOTE]` non-endorsement and content note, What is this / Is this any good / Roadmap / Play / History / Contributing / Build / AI use / Licence / Community and credits.
5. **No public play URL yet:** "Play" is "Run locally"; a hosted browser build is planned for M4.
6. **`README.he.md` is untested for right-to-left rendering;** Task 7 checks it through GitHub's renderer and the owner checks the live page.
7. **Go public now (done 2026-10-10).** The repo is public; `main` is protected by the ruleset `protect-main` (no force-push, no deletion). Merge policy stays in rulesets: `protect-main` is extended from a checked-in JSON by an owner-run script; no classic branch protection.
8. **Contributor terms (D2): inbound = outbound.** No CLA and no DCO; contributions come in under the repository licences (GitHub Terms of Service D.6). Already stated in the published `CONTRIBUTING.md`.
9. **`prepublish.sh` becomes the release gate** (`scripts/oss/release_gate.sh`), run before every release tag; it requires D10 and D11.

## What `602183f` already holds, and what is left

| Item | State at `602183f` | This plan |
|---|---|---|
| `LICENSE` (MIT), `LICENSES/CC-BY-4.0.txt`, `CC-BY-SA-4.0.txt`, `CC0-1.0.txt` | Done | Adds `LICENSES/YamlDotNet-MIT.txt` (Task 10) |
| `LICENSING.md` (per-path table; `content/` CC BY-SA 4.0; Unity template settings under the Unity Companion License; pending owner checks) | Done | Amends the table's gaps (review A2.3) (Task 10) |
| `README.md` (captioned hero, `build-m3-*` screenshots, no "non-commercial", link to CONTRIBUTING) | Done | Amends: language bar, 1936 caption, dated grid caption, link bar, accurate history bullets, Build section, credits (Tasks 7, 10) |
| `CONTRIBUTING.md` (no CLA, inbound = outbound, sensitive tags listed) | Done | Amends: SECURITY link, exact test commands, CI section, changelog and provenance rules, conduct links (Tasks 2, 3, 5, 10) |
| `.gitignore` (`.env`, `*.env`, `*.ulf`, `.superpowers/`, `research_notes/open source/`) | Done | Appends the missing lines (Task 11) |
| UA email in `promo/fetch_portraits.py` | Done (project URL) | — |
| `promo/gen.py` reads another project's `.env` and names it | Not done | Task 11 |
| Private vulnerability reporting | Enabled by the owner | Task 2 verifies |
| Ruleset `protect-main` (non_fast_forward, deletion; no bypass) | Done | Task 13 extends it (owner applies) |
| SECURITY, CI, issue forms, CODE_OF_CONDUCT, CONTENT_POLICY, PR template, README.he.md, BUILDING.md, social preview, provenance, history record, release gate | Not done | Tasks 2–14 |

## Audit results this plan acts on (values never printed)

- **Secrets:** full history of every branch: no API key, token, private key, JWT or Unity cloud id. Re-run on `602183f` with the amended scanner (merge commits included): 8 findings, 0 secret-shape. The 8 are fake home paths in Unity test fixtures, Unity's own template path, and the owner's gmail address in commit `d42bd6b` (`promo/fetch_portraits.py`, fixed at the tip, public in history: decision D10). Task 12 records them in `docs/oss/history-accepted.tsv`.
- **Licence checks still open, now after publication:** D5 (ElevenLabs and image-model terms for the AI images and the hero), D6 (data.gov.il terms for `content/map/precip_stations.json`), D-P (portrait US status; `promo/ref/*-src.jpg` and `*-sheet.jpg` provenance), D-H (no Civilization VII marks in the hero).
- **Sensitive events:** the seven events tagged `holocaust`, `terror`, `massacre` or `war-casualties` are public and not yet sensitivity-reviewed (`Statecraft.Cli --release-gate` on `content/` exits 3 and names all seven). Decision D11.

## Owner decisions (recorded in `docs/oss/DECISIONS.md`; the release gate refuses while any is unticked)

| ID | Decision | State / recommendation |
|---|---|---|
| D1 | Licences | Published split (code MIT; `content/` CC BY-SA 4.0; original docs/art/UI CC BY 4.0; AI-only images CC0 / no rights claimed; Unity template-derived files Unity Companion License). Confirm. |
| D2 | Contributor terms | **Decided:** inbound = outbound; no CLA, no DCO. |
| D3 | Repository | `eyalgolan/statecraft-israel`, public. Confirm. |
| D4 | Keep publishing `research_notes/` and `docs/superpowers/` | Recommended yes; `research_notes/open source/` is never committed (git-ignored). |
| D5 | ElevenLabs and image-model terms allow public redistribution of the AI images and the hero | Owner verifies. Published before checked. |
| D6 | data.gov.il terms allow redistribution of the IMS station normals | Owner verifies. Published before checked. |
| D7 | Security and conduct contact | GitHub private vulnerability reporting only (enabled). |
| D8 | Name clearance for "Statecraft: Israel" | Owner searches USPTO, EUIPO, Steam. |
| D9 | Unity edit-mode tests out of CI | Recommended yes (they need the owner's Unity credentials as secrets, which fork PRs never get). |
| D10 | Acknowledge what history publishes | The personal email in `d42bd6b` and the internal plans, specs and research are public for good; rotation is the only remedy for a secret. |
| D11 | Sensitivity sign-off of the seven events | Owner sets `status: sensitivity-reviewed` in each event's YAML. The release gate also runs `Statecraft.Cli --release-gate`. |
| D12 | Ruleset bypass | Recommended: the admin role bypasses `protect-main` always (the owner keeps merging locally and pushing); everyone else needs review and checks. Alternative: admins bypass only through a pull request. |
| D-P | Portraits and reference images | Owner checks each `owner-verify` row of `ASSETS.portraits.yaml` on Commons, and the `-src`/`-sheet` provenance. |
| D-H | Hero marks | Owner looks for any Civilization VII icon, logo or wordmark in the hero. |

## Sequencing with active work

1. This plan works in its own worktree from `602183f` or later: `git -C ~/israel-game worktree add .worktrees/oss -b oss main`.
2. **visual-v1** runs in parallel in its own worktree. Files are disjoint: visual-v1 owns `tools/**`, `docs/dev-setup.md`, `docs/visual-v1.md`, `unity/Assets/**`, `unity/ProjectSettings/**`; this plan owns the root policy files, `README*`, `BUILDING.md`, `CONTRIBUTING.md`, `LICENSING.md`, `ASSETS*.yaml`, `CHANGELOG.md`, `.github/**`, `scripts/oss/**`, `docs/oss/**`, `docs/images/**`, `docs/CONTENT_POLICY.md`, `docs/UNTRUSTED_PR_REVIEW.md`, `docs/RELEASING.md`, `LICENSES/**`, `research_notes/README.md`, `.gitignore`, `promo/gen.py`, and one line of `docs/m3-release.md`.
3. Shared hazards: visual-v1's new tools tests first run on Linux in this plan's CI (Task 15 re-runs the Linux parity after its merge); a Unity build or headless Chrome here can disturb visual-v1 profiling, so this plan reuses the published screenshots and takes new ones only with visual-v1's `web_check.py --shots` under its machine lock (Task 15); visual-v1's textures are covered by the `unity/Assets/Textures/*` rule.
4. **The README screenshot refresh after visual-v1 is a gating step** (Task 15), done in the pull request that merges visual-v1 or immediately after it, never left as a follow-up.
5. **Merge-order rule with visual-v1** (re-review §3):
   1. Land this plan's Task 3 (CI) on `main` before visual-v1 merges, so visual-v1's merge is checked by CI.
   2. Before pushing the visual-v1 merge to `main`, run Task 3 Step 10's Linux docker parity on the merge result, so `main` never goes red; do Task 15 (README screenshots, "Is this any good?"/Roadmap text, the CHANGELOG line) in the same push.
   3. If visual-v1 merges first, rebase `oss` onto it and re-run Task 3 Steps 9–10 before landing the CI.
   4. In either order, whichever side lands second re-runs `check_repo.py` and the tools tests on the combined tree.
   5. The tools test command (six `--with` pins) appears in `ci.yml`, CONTRIBUTING, `release_gate.sh` and visual-v1's plan; a new tools dependency changes all four in one commit.
   6. If visual-v1 removes the fake-path fixtures from `GameSceneTests.cs`, `allow.yaml`'s row turns `stale-allow` and `Repo policy` fails; delete the row in the same commit.
6. Owner steps: Task 9 (D5, D6, D10, D11, D-P, D-H, sensitivity review), Task 8 (social preview upload), Task 13 (apply the ruleset), Task 16 (final checks).

## Global Constraints

- Base `602183f` or later; worktree `.worktrees/oss`, branch `oss`; commands run from the worktree root.
- Define the test command as a shell function in every shell (Task 1, Step 1); a function works in both zsh and bash, where an unquoted variable would not word-split in zsh:
  `oss_pytest() { uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q "$@"; }`
- **Every task's tests pass at the end of that task** (review N3): no test asserts a file or a line that a later task creates, so `main` stays green whichever tasks land early.
- **Amend the published files; never recreate them under other names.** Before replacing a whole published file, `git diff 602183f HEAD -- <file>` must be empty; otherwise apply the change as an edit.
- **Never modify** `sim/**`, `content/**`, `unity/**` or `tools/**`; each task ends with `git diff --stat 602183f...HEAD -- sim content unity tools` printing nothing. The only exception is D11, which the owner does.
- **No history rewriting** (`filter-repo`, `filter-branch`, rebasing pushed history) and **no CI rule about past commits**.
- **Agents never** run `git push`, `gh repo create|edit`, `gh api` with `-X PATCH|PUT|POST|DELETE`, `gh label create`, or `scripts/oss/apply_github_settings.sh`. Read-only `gh api` GETs are allowed (Task 3 resolves action SHAs), and so is `gh api markdown`, a stateless render (Task 7).
- **Never print a secret value.** Scanners print the pattern type, file and commit.
- Python through `uv` 0.12.5, inline `# /// script` metadata.
- Copy rules: English (plus the Hebrew README); no counts or sizes in prose that no test checks; every concept image is captioned as concept art; other companies' marks in plain text only.
- Commit messages: Conventional Commits, ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Any address in this plan that would read as a personal email is written with an escaped dot, so the plan file can be committed without tripping its own checker.

## Review Focus

1. **A pull request from a fork** gets full CI with no secret: the workflow references no `secrets.`, checkouts keep no credentials (`test_ci_uses_no_secret_but_the_read_only_token`, `test_checkouts_keep_no_credentials`, Task 3).
2. **A contributor adds an asset with no provenance row** (any extension in the list, or any file git sees as binary): `Repo policy` names it (`test_every_listed_asset_extension_needs_provenance`, `test_a_file_git_sees_as_binary_needs_provenance_whatever_its_name`, Task 3).
3. **The maintainer toggles `no-changelog` after CI ran:** the `Changelog` job re-runs and diffs the PR head, not the merge ref (`test_pull_request_types_rerun_on_label_changes`, `test_changelog_diffs_the_pr_head_not_the_merge_ref`, Task 3).
4. **A Unity package id or a test fixture looks like personal data:** no false alarm, and an allowlisted fixture line still fails anywhere else (`test_package_ids_with_numeric_versions_are_not_emails`, `test_allowlisted_fixture_passes_and_the_same_line_elsewhere_fails`, Task 3).
5. **The owner tags a release while D10/D11 or a licence check is open, or after rotating a key:** the release gate refuses on open items, and a rotated key recorded in `history-accepted.tsv` no longer blocks (`test_release_mode_refuses_open_decisions`, `test_release_mode_refuses_owner_verify_rows`, `test_accepted_rows_are_shown_as_accepted`, Tasks 3, 12, 14).

---

### Task 1: Worktree and the decisions record

**Files:**
- Create: `docs/oss/DECISIONS.md`, `scripts/oss/tests/test_policies.py`

**Interfaces:**
- Produces: `docs/oss/DECISIONS.md` lines `- [ ] D<n> …` (and `D-P`, `D-H`), which `check_repo.py --release` (Task 3) refuses while unticked.

- [ ] **Step 1: Create the worktree and the test command.**

```bash
git -C ~/israel-game log -1 --format='%h %s' main      # 602183f or later
git -C ~/israel-game worktree add .worktrees/oss -b oss main
cd ~/israel-game/.worktrees/oss
oss_pytest() { uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q "$@"; }
mkdir -p docs/oss scripts/oss/tests
```

- [ ] **Step 2: Write the failing test.** Create `scripts/oss/tests/test_policies.py`:

```python
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def test_decisions_record_d2_decided_and_d10_d11_open_for_the_owner():
    text = read("docs/oss/DECISIONS.md")
    assert "- [x] D2" in text and "inbound = outbound" in text
    assert "- [ ] D10" in text and "d42bd6b" in text and "- [ ] D11" in text and "Published before checked" in text
```

- [ ] **Step 3: Run it to verify it fails.** Run: `oss_pytest scripts/oss/tests/test_policies.py`. Expected: FAIL with `FileNotFoundError` for `docs/oss/DECISIONS.md`.

- [ ] **Step 4: Write `docs/oss/DECISIONS.md`.**

```markdown
# Open-source owner decisions

Each line is an owner decision from the open-source plan (`docs/superpowers/plans/2026-10-11-statecraft-open-source.md`,
"Owner decisions"). Tick a line (`- [x]`) and write the choice after "Decided:" when you have made it.
`scripts/oss/check_repo.py --release`, run by `scripts/oss/release_gate.sh` before every release, refuses while any
line is unticked.

**Published before checked.** The repository went public on 2026-10-10 (`554ca2c`, then `4950a4e`). D5, D6, the
portrait US status and the `promo/ref/*-src.jpg` / `*-sheet.jpg` provenance were not checked first, so those files
are already public. If a check fails, the file is removed from the tip with the removal procedure in the plan
(Task 10); history keeps it (owner ruling: no history rewriting).

- [ ] D1 Licences. As published in `4950a4e`: code MIT; `content/` CC BY-SA 4.0 (it paraphrases Wikipedia); original docs, art, UI assets and reports CC BY 4.0; AI-only images CC0 / no rights claimed; Unity template-derived files under the Unity Companion License. Confirm, or name a change. Decided:
- [x] D2 Contributor terms. Decided (owner ruling, 2026-10-11): inbound = outbound. No CLA and no DCO; contributions come in under the repository licences for the paths they change (GitHub Terms of Service D.6, `LICENSING.md`).
- [ ] D3 Repository: `eyalgolan/statecraft-israel`, public (done 2026-10-10). Decided:
- [ ] D4 Keep publishing `research_notes/` and `docs/superpowers/` in future commits. Recommended: yes; `research_notes/open source/` is never committed. Decided:
- [ ] D5 The ElevenLabs Terms of Service for the account's plan on 2026-10-08/09, and the image models' output terms, allow public redistribution of the AI promo images and the hero. Owner verifies. Decided:
- [ ] D6 The data.gov.il terms allow redistribution of the IMS station normals (`content/map/precip_stations.json`). Owner verifies. Decided:
- [ ] D7 Security and conduct contact. Recommended: GitHub private vulnerability reporting only. Decided:
- [ ] D8 Name clearance for "Statecraft: Israel" (USPTO, EUIPO, Steam). Owner checks. Decided:
- [ ] D9 Unity edit-mode tests stay out of CI. Recommended: yes, local gate with pasted evidence. Decided:
- [ ] D10 I know that my personal email (commit d42bd6b, `promo/fetch_portraits.py`) and the internal plans, specs and research notes in history are public and cannot be removed (owner ruling: no history rewriting); key rotation is the only remedy for a secret. Decided:
- [ ] D11 The seven sensitive events (altalena, ten_days, operation_yoav, operation_hiram, operation_horev, bernadotte_assassination, reparations_claim) are sensitivity-reviewed: each event's YAML says `status: sensitivity-reviewed` (docs/m3-release.md, "OWNER STEP: sensitivity review"). The release gate also runs the sim's own check. Decided:
- [ ] D12 Ruleset bypass. Recommended: the repository admin role bypasses `protect-main` always (you keep merging locally and pushing); everyone else needs review and green checks. Alternative: admins bypass only through a pull request. Decided:
- [ ] D-P Portraits: every `owner-verify` row in `ASSETS.portraits.yaml` checked on Commons for its US status, and the `promo/ref/*-src.jpg` and `*-sheet.jpg` provenance confirmed. Decided:
- [ ] D-H The hero (`docs/images/hero-concept-1936.jpg`) reproduces no Civilization VII icon, logo or wordmark (it was prompted "in the style of Civilization VII"). Owner looks. Decided:
```

- [ ] **Step 5: Run the test.** Expected: `1 passed`.

- [ ] **Step 6: Commit.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add docs/oss/DECISIONS.md scripts/oss/tests/test_policies.py
git commit -m "docs(oss): owner decisions record, published-before-checked stated, D2 decided (inbound = outbound)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: SECURITY.md and the untrusted-PR review profile (private reporting is on)

**Files:**
- Create: `SECURITY.md`, `docs/UNTRUSTED_PR_REVIEW.md`
- Modify: `CONTRIBUTING.md` ("Security" section), `scripts/oss/tests/test_policies.py` (append)

**Interfaces:**
- Consumes: private vulnerability reporting, enabled by the owner on 2026-10-10.
- Produces: the private-report URL `https://github.com/eyalgolan/statecraft-israel/security/advisories/new`, used by the conduct contact (Task 5) and the issue-form config (Task 4).

- [ ] **Step 1: Verify private reporting is on (read-only).** Run: `gh api repos/eyalgolan/statecraft-israel/private-vulnerability-reporting --jq .enabled`. Expected: `true`. If `false`, stop and ask the owner to run `gh api -X PUT repos/eyalgolan/statecraft-israel/private-vulnerability-reporting`.

- [ ] **Step 2: Append the failing tests to `scripts/oss/tests/test_policies.py`:**

```python
def test_security_policy_uses_private_reporting_only():
    text = read("SECURITY.md")
    assert "security/advisories/new" in text and "@" not in text.replace("@eyalgolan", "")
    assert "revoked at its provider; history is not rewritten" in text


def test_contributing_links_security_and_untrusted_review():
    text = read("CONTRIBUTING.md")
    assert "(SECURITY.md)" in text and "docs/UNTRUSTED_PR_REVIEW.md" in text


def test_contributing_has_no_cla_and_names_every_sensitive_tag():
    text = read("CONTRIBUTING.md")
    assert "There is no CLA and no sign-off" in text
    for tag in ("holocaust", "terror", "massacre", "war-casualties"):
        assert f"`{tag}`" in text, tag


def test_untrusted_pr_review_forbids_running_pr_code_and_warns_about_github_diffs():
    text = read("docs/UNTRUSTED_PR_REVIEW.md")
    assert "--network none" in text and "Assets/Editor" in text and "Directory.Build.props" in text
    assert "Any diff under `.github/` can make every required check green" in text
```

- [ ] **Step 3: Run them to verify they fail.** Run: `oss_pytest scripts/oss/tests/test_policies.py`. Expected: FAIL (`SECURITY.md` missing); `test_contributing_has_no_cla_and_names_every_sensitive_tag` already passes on the published CONTRIBUTING.

- [ ] **Step 4: Write `SECURITY.md`.**

```markdown
# Security policy

## Supported versions

Statecraft: Israel is pre-release and ships from `main`. Only the latest commit on `main` is supported.

## Reporting a vulnerability

Do not open a public issue for a security problem. Use GitHub's private vulnerability reporting: open the repository's
**Security** tab and choose **Report a vulnerability**
(https://github.com/eyalgolan/statecraft-israel/security/advisories/new). Only you and the maintainers can see it.

Please include what the issue is and what an attacker gets from it, the steps to reproduce, the commit SHA you
tested, and your OS and browser (for the WebGL build) or Unity and .NET versions.

## What to expect

Acknowledgement within 5 business days, and an assessment with a rough fix timeline within 15. This is a
single-maintainer project: those are targets, not a contract. There is no bug bounty. You are credited in the advisory
and the changelog unless you ask otherwise.

## Scope

In scope:

- The WebGL and desktop builds, including save files (`SaveGame`) and the page flags they read (`?golden`, `?smoke`).
- The content loader: a crafted file under `content/` or `StreamingAssets/` that crashes or hangs the game or escapes
  its folder.
- The repository's CI and scripts (`.github/`, `scripts/`, `tools/`): anything that lets a pull request reach a secret,
  write to the repository, or run code with more than read access.
- A credential, token or personal data committed to this repository.

Out of scope:

- Vulnerabilities in Unity, .NET, browsers or third-party packages with no exploitable path through this project
  (report them upstream).
- Running a build or a tool you modified yourself.

## Credentials

This repository holds no credentials and never should. Tools that need one (for example `promo/gen.py`) read it from an
environment variable. If you find one in this repository or its history, report it privately as above. It will be
revoked at its provider; history is not rewritten.
```

- [ ] **Step 5: Write `docs/UNTRUSTED_PR_REVIEW.md`.** (It now says that any `.github/` diff can make every check green: review I8.)

````markdown
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
````

- [ ] **Step 6: Amend `CONTRIBUTING.md`.** Apply this edit (the old text must match exactly once):

1. Replace:

```markdown
Never commit keys, tokens or `.env` files. Secret scanning and push protection are on. To report a vulnerability, use **Security → Report a vulnerability** on the repository page (GitHub private reporting); don't open a public issue.
```

   with:

```markdown
Never commit keys, tokens or `.env` files. Secret scanning and push protection are on. To report a vulnerability, use **Security → Report a vulnerability** on the repository page (GitHub private reporting); don't open a public issue. What is in scope and what to expect: [SECURITY.md](SECURITY.md). How the maintainer reviews outside pull requests without running their code: [docs/UNTRUSTED_PR_REVIEW.md](docs/UNTRUSTED_PR_REVIEW.md).
```

- [ ] **Step 7: Run the tests.** Expected: all in `test_policies.py` pass.

- [ ] **Step 8: Commit.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add SECURITY.md docs/UNTRUSTED_PR_REVIEW.md CONTRIBUTING.md scripts/oss/tests/test_policies.py
git commit -m "docs(security): SECURITY.md on private reporting, and the untrusted-PR review profile

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: CI with the policy checker (C1 and C2 fixed)

**Files:**
- Create:
  - `scripts/oss/oss_policy.py`, `scripts/oss/check_repo.py`, `scripts/oss/allow.yaml`;
  - `scripts/oss/changelog_check.py`, `scripts/oss/pin_actions.py`;
  - `scripts/oss/tests/conftest.py`, `test_check_repo.py`, `test_assets.py`, `test_changelog_check.py`, `test_pin_actions.py`, `test_github_config.py`;
  - `.github/workflows/ci.yml`, `.github/CODEOWNERS`, `CHANGELOG.md`.
- Modify: `CONTRIBUTING.md` (test commands, changelog rule, "What CI runs"), `scripts/oss/tests/test_policies.py` (append).

**Interfaces:**
- Produces:
  - `oss_policy.secret_kinds/email_kinds/home_path_kinds(line) -> list[str]`; constants `REQUIRED_FILES`, `BINARY_EXTENSIONS`, `PROVENANCE_GLOBS`, `ALLOWED_LICENCES`, `STATUSES`, `RULE_KEYS`, `MAX_FILE_BYTES`;
  - `check_repo.Finding(check, path, line, kind)`, `check_repo.CHECKS` (keys `secrets, emails, home-paths, sizes, symlinks, required, decisions, assets`), `check_repo.main(argv) -> int`; allowlist `scripts/oss/allow.yaml` rows `{check, glob, reason}` (a row that silences nothing is `stale-allow`);
  - `changelog_check.verdict(paths, skip_label) -> (bool, str)`; `pin_actions.pin_text(text, resolve) -> str`;
  - CI job names `Sim (dotnet test)`, `Tools (pytest)`, `Repo policy`, `Changelog`: the required checks of Task 13.
- Until Task 14 adds the last required files, CI's `Repo policy` runs `check_repo.py --only secrets,emails,home-paths,sizes,symlinks`; Task 14 switches it to the full check.

- [ ] **Step 1: Write the failing tests.**

`scripts/oss/tests/conftest.py`:

```python
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))


def git(root: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True, check=True).stdout


def commit_all(root: Path, msg: str) -> str:
    git(root, "add", "-A")
    git(root, "-c", "user.name=t", "-c", "user.email=t@users.noreply.github.com", "commit", "-qm", msg)
    return git(root, "rev-parse", "HEAD").strip()[:10]


@pytest.fixture
def make_repo(tmp_path):
    """A throwaway git repo with the given files tracked. Values are bytes or str; a str starting with '->' is a
    symlink target."""
    def _make(files: dict[str, bytes | str], untracked: dict[str, str] | None = None) -> Path:
        root = tmp_path / "repo"
        root.mkdir()
        git(root, "init", "-q", "-b", "main")
        for rel, data in files.items():
            p = root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            if isinstance(data, str) and data.startswith("->"):
                p.symlink_to(data[2:])
            elif isinstance(data, bytes):
                p.write_bytes(data)
            else:
                p.write_text(data, encoding="utf-8")
        git(root, "add", "-A")
        for rel, data in (untracked or {}).items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(data, encoding="utf-8")
        return root
    return _make
```

`scripts/oss/tests/test_check_repo.py`:

```python
import check_repo
import oss_policy

# Fake secrets are assembled at run time so that this file never holds a secret-shaped literal.
FAKE_ELEVENLABS = "sk_" + "ab" * 24
FAKE_GITHUB = "ghp_" + "A1" * 18
ALLOW = 'allow:\n  - check: home-paths\n    glob: "tests/fixtures/*"\n    reason: "redaction fixtures"\n'


def run(root, *args):
    return check_repo.main(["--root", str(root), *args])


def test_fake_key_is_reported_by_type_and_never_by_value(make_repo, capsys):
    root = make_repo({"promo/x.py": f'KEY = "{FAKE_ELEVENLABS}"\nTOKEN = "{FAKE_GITHUB}"\n'})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "secrets: promo/x.py:1: elevenlabs-key" in out
    assert "secrets: promo/x.py:2: github-token" in out
    assert FAKE_ELEVENLABS not in out and FAKE_GITHUB not in out


def test_clean_tree_passes(make_repo, capsys):
    root = make_repo({"sim/a.cs": "class A {}\n"})
    assert run(root, "--only", "secrets,emails,home-paths,sizes,symlinks") == 0
    assert "OK" in capsys.readouterr().out


def test_unity_cloud_ids_empty_pass(make_repo, capsys):
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset": "  cloudProjectId: \n  organizationId: \n"})
    assert run(root, "--only", "secrets") == 0


def test_unity_cloud_ids_filled_fail(make_repo, capsys):
    guid = "1234abcd-" + "12ab-" * 3 + "1234567890ab"
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset":
                      f"  cloudProjectId: {guid}\n  organizationId: some-org\n"})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "unity-cloud-project-id" in out and "unity-organization-id" in out
    assert guid not in out and "some-org" not in out


def test_personal_email_fails_and_noreply_passes(make_repo, capsys):
    personal = "someone" + "@" + "gmail.com"
    root = make_repo({"a.py": f'UA = "x ({personal})"\n',
                      "b.md": "Co-Authored-By: Claude <noreply" + "@" + "anthropic.com>\n",
                      "c.md": "x <5146175+eyalgolan" + "@" + "users.noreply.github.com>\n",
                      "d.py": "    @pytest.mark.parametrize('a', [1])\n"})
    assert run(root, "--only", "emails") == 1
    out = capsys.readouterr().out
    assert "emails: a.py:1: personal-email" in out
    assert "b.md" not in out and "c.md" not in out and "d.py" not in out
    assert personal not in out


def test_package_ids_with_numeric_versions_are_not_emails(make_repo, capsys):
    # Review C1: ProjectSettings.asset and docs carry Unity package ids such as these.
    root = make_repo({"ProjectSettings.asset": "  templatePackageId: com.unity.template.urp-blank" + "@" + "17.0.14\n",
                      "m0.md": "com.unity.ai.assistant" + "@" + "2.20.0-pre.2 and pkg" + "@" + "17.0.14\n"})
    assert run(root, "--only", "emails") == 0


def test_home_path_fails_and_tilde_passes(make_repo, capsys):
    root = make_repo({"a.md": "see /Users/" + "someone/x\n", "b.md": "see ~/Library/Caches\n",
                      "c.md": "see /home/" + "someone/x\n"})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "a.md:1: home-path" in out and "c.md:1: home-path" in out and "b.md" not in out


def test_allowlisted_fixture_passes_and_the_same_line_elsewhere_fails(make_repo, capsys):
    line = 'var p = "/Users/' + 'someone/game";\n'
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "tests/fixtures/a.cs": line, "src/b.cs": line})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "src/b.cs:1: home-path" in out and "tests/fixtures/a.cs" not in out


def test_stale_allow_row_is_reported(make_repo, capsys):
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "src/b.cs": "fine\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "stale-allow home-paths tests/fixtures/*" in capsys.readouterr().out


def test_allow_row_without_reason_is_reported(make_repo, capsys):
    rows = 'allow:\n  - check: home-paths\n    glob: "x/*"\n'
    root = make_repo({"scripts/oss/allow.yaml": rows, "x/a.cs": "/home/" + "someone/a\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "allow-row-without-reason x/*" in capsys.readouterr().out


def test_untracked_files_are_not_scanned(make_repo, capsys):
    root = make_repo({"a.txt": "fine\n"}, untracked={"secret.env": f"K={FAKE_ELEVENLABS}\n"})
    assert run(root, "--only", "secrets") == 0


def test_binary_files_are_not_text_scanned(make_repo, capsys):
    root = make_repo({"img.png": b"\x89PNG\x00\x00" + FAKE_GITHUB.encode()})
    assert run(root, "--only", "secrets") == 0


def test_large_file_fails(make_repo, capsys):
    root = make_repo({"big.bin": b"\x00" * (oss_policy.MAX_FILE_BYTES + 1)})
    assert run(root, "--only", "sizes") == 1
    assert "sizes: big.bin: over-size-limit" in capsys.readouterr().out


def test_absolute_symlink_fails(make_repo, capsys):
    root = make_repo({"link": "->/etc/hosts", "ok": "->a.txt", "a.txt": "x\n"})
    assert run(root, "--only", "symlinks") == 1
    out = capsys.readouterr().out
    assert "symlinks: link: symlink-outside-repo" in out and "ok:" not in out


def test_required_files_are_named_when_missing(make_repo, capsys):
    root = make_repo({"README.md": "x\n"})
    assert run(root, "--only", "required") == 1
    out = capsys.readouterr().out
    assert "required: LICENSE: missing" in out and "required: README.md:" not in out


def test_release_mode_refuses_open_decisions(make_repo, capsys):
    root = make_repo({"docs/oss/DECISIONS.md": "- [x] D1 decided\n- [ ] D2 open\n"})
    assert run(root, "--only", "decisions") == 0
    assert run(root, "--only", "decisions", "--release") == 1
    assert "decisions: docs/oss/DECISIONS.md:2: open-decision" in capsys.readouterr().out


def test_unknown_check_name_is_an_error(make_repo, capsys):
    root = make_repo({"a.txt": "x\n"})
    assert run(root, "--only", "nonsense") == 2
```

`scripts/oss/tests/test_assets.py`:

```python
import check_repo

RULES = """rules:
  - glob: "img/*.png"
    licence: CC-BY-4.0
    source: ours
    status: verified
  - glob: "future/*"
    licence: Copernicus-DEM
    source: visual-v1 bakes
    status: verified
    allow_empty: true
removed: []
"""


def run(root, *args):
    return check_repo.main(["--root", str(root), "--only", "assets", *args])


def test_covered_binary_passes(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG"})
    assert run(root) == 0


def test_uncovered_binary_is_named(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "art/new.jpg": b"\xff\xd8"})
    assert run(root) == 1
    assert "assets: art/new.jpg: no-provenance" in capsys.readouterr().out


def test_every_listed_asset_extension_needs_provenance(make_repo, capsys):
    exts = [".svg", ".psd", ".exr", ".fbx", ".woff", ".woff2", ".otf", ".tga", ".hdr", ".glb", ".gltf", ".blend",
            ".wav", ".ogg", ".mp3"]
    files = {"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG"}
    files.update({f"art/f{e}": "text-like content\n" for e in exts})
    root = make_repo(files)
    assert run(root) == 1
    out = capsys.readouterr().out
    for e in exts:
        assert f"assets: art/f{e}: no-provenance" in out, e


def test_a_file_git_sees_as_binary_needs_provenance_whatever_its_name(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "data/blob.dat": b"\x00\x01\x02binary"})
    assert run(root) == 1
    assert "assets: data/blob.dat: no-provenance" in capsys.readouterr().out


def test_provenance_globs_cover_map_data_text_files(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "content/map/x.json": "{}"})
    assert run(root) == 1
    assert "content/map/x.json: no-provenance" in capsys.readouterr().out


def test_stale_rule_is_named_unless_allow_empty(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES})
    assert run(root) == 1
    out = capsys.readouterr().out
    assert "assets: ASSETS.yaml: stale-rule img/*.png" in out and "future/*" not in out


def test_rule_missing_a_key_is_a_bad_rule_not_a_crash(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": "rules:\n  - licence: MIT\n    source: x\n    status: verified\n"})
    assert run(root) == 1
    assert "bad-rule missing glob" in capsys.readouterr().out


def test_unknown_licence_and_bad_status(make_repo, capsys):
    bad = RULES.replace("licence: CC-BY-4.0", "licence: WTFPL").replace("status: verified\n  - glob: \"future",
                                                                      "status: maybe\n  - glob: \"future")
    root = make_repo({"ASSETS.yaml": bad, "img/a.png": b"\x89PNG"})
    assert run(root) == 1
    out = capsys.readouterr().out
    assert "unknown-licence WTFPL" in out and "bad-status maybe" in out


def test_release_mode_refuses_owner_verify_rows(make_repo, capsys):
    rules = RULES.replace("source: ours\n    status: verified", "source: ours\n    status: owner-verify")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 1
    assert "assets: ASSETS.yaml: owner-verify img/*.png" in capsys.readouterr().out


def test_release_mode_refuses_unknown_licence(make_repo, capsys):
    rules = RULES.replace("licence: CC-BY-4.0", "licence: unknown")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 1


def test_portraits_file_is_read_too(make_repo, capsys):
    portraits = 'rules:\n  - glob: "p/a.jpg"\n    licence: Public-Domain\n    source: commons\n    status: verified\n'
    root = make_repo({"ASSETS.yaml": RULES, "ASSETS.portraits.yaml": portraits, "img/a.png": b"\x89PNG",
                      "p/a.jpg": b"\xff\xd8"})
    assert run(root) == 0


def test_removed_path_must_not_be_tracked(make_repo, capsys):
    rules = RULES.replace("removed: []", 'removed:\n  - { path: "img/a.png", reason: "x", date: 2026-10-11 }')
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 1
    assert "img/a.png: removed-but-tracked" in capsys.readouterr().out
```

`scripts/oss/tests/test_changelog_check.py`:

```python
import changelog_check as cc


def test_user_visible_change_without_changelog_fails_and_names_the_paths():
    ok, msg = cc.verdict(["content/events/x.yaml", "README.md"], skip_label=False)
    assert not ok and "content/events/x.yaml" in msg and "## [Unreleased]" in msg and "no-changelog" in msg


def test_changelog_entry_satisfies_it():
    assert cc.verdict(["sim/Statecraft.Sim/Rng.cs", "CHANGELOG.md"], skip_label=False)[0]


def test_label_satisfies_it():
    assert cc.verdict(["unity/Assets/Scripts/UI/GameRoot.cs"], skip_label=True)[0]


def test_tests_docs_tools_and_ci_need_no_entry():
    for p in ("unity/Assets/Tests/EditMode/MapTests.cs", "sim/Statecraft.Sim.Tests/CoreTests.cs", "docs/x.md",
              "tools/hexgrid.py", ".github/workflows/ci.yml", "scripts/oss/check_repo.py"):
        assert cc.verdict([p], skip_label=False)[0], p


def test_every_user_visible_root_is_named():
    for p in ("content/people.yaml", "sim/Statecraft.Sim/Rng.cs", "sim/Statecraft.Cli/Program.cs",
              "unity/Assets/UI/Game.uxml", "unity/ProjectSettings/ProjectSettings.asset", "unity/Packages/manifest.json"):
        assert cc.user_visible([p]) == [p], p
```

`scripts/oss/tests/test_pin_actions.py`:

```python
import pin_actions

SHA = "a" * 40


def test_tags_are_replaced_by_sha_with_the_tag_kept_as_a_comment():
    text = "steps:\n  - uses: actions/checkout@v4\n  - uses: astral-sh/setup-uv@v6\n    with:\n      version: x\n"
    out = pin_actions.pin_text(text, lambda repo, ref: SHA)
    assert f"  - uses: actions/checkout@{SHA} # v4\n" in out
    assert f"  - uses: astral-sh/setup-uv@{SHA} # v6\n" in out


def test_already_pinned_lines_are_left_alone():
    text = f"  - uses: actions/checkout@{SHA} # v4\n"
    assert pin_actions.pin_text(text, lambda repo, ref: (_ for _ in ()).throw(AssertionError)) == text
```

`scripts/oss/tests/test_github_config.py`:

```python
import json
import re
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[3]
CI = ROOT / ".github/workflows/ci.yml"
RULESET = ROOT / "scripts/oss/github/ruleset-main-review.json"
EXPECTED_JOBS = {"Sim (dotnet test)", "Tools (pytest)", "Repo policy", "Changelog"}


def ci() -> dict:
    return yaml.safe_load(CI.read_text(encoding="utf-8"))


def triggers(wf: dict) -> dict:
    return wf.get("on", wf.get(True))  # PyYAML reads the bare key `on` as True


def steps(wf: dict):
    return [s for j in wf["jobs"].values() for s in j["steps"]]


def test_job_names_are_the_required_checks():
    assert {j["name"] for j in ci()["jobs"].values()} == EXPECTED_JOBS
    if RULESET.exists():
        rules = json.loads(RULESET.read_text())["rules"]
        checks = next(r for r in rules if r["type"] == "required_status_checks")["parameters"]
        assert {c["context"] for c in checks["required_status_checks"]} == EXPECTED_JOBS


def test_default_token_is_read_only_and_nothing_targets_the_base_repo():
    wf = ci()
    assert wf["permissions"] == {"contents": "read"}
    t = triggers(wf)
    assert "pull_request_target" not in t and "schedule" not in t and "workflow_run" not in t


def test_checkouts_keep_no_credentials():
    for s in steps(ci()):
        if s.get("uses", "").startswith("actions/checkout@"):
            assert s.get("with", {}).get("persist-credentials") is False, s


def test_pull_request_types_rerun_on_label_changes():
    types = set(triggers(ci())["pull_request"]["types"])
    assert {"opened", "synchronize", "reopened", "labeled", "unlabeled"} <= types


def test_ci_uses_no_secret_but_the_read_only_token():
    text = CI.read_text(encoding="utf-8")
    assert "secrets." not in text
    assert set(re.findall(r"\$\{\{\s*github\.token\s*\}\}", text)) <= {"${{ github.token }}"}


def test_every_action_is_pinned_to_a_full_sha():
    for wf in (ROOT / ".github/workflows").glob("*.yml"):
        for ref in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@(\S+)", wf.read_text(encoding="utf-8")):
            assert re.fullmatch(r"[0-9a-f]{40}", ref), f"{wf.name}: {ref}"


def test_tools_job_installs_dotnet():
    # Review C2: tools/tests/test_package_release.py runs the sim's release gate through dotnet.
    tools = next(j for j in ci()["jobs"].values() if j["name"] == "Tools (pytest)")
    assert any(s.get("uses", "").startswith("actions/setup-dotnet@") for s in tools["steps"])
    assert "--deselect" not in CI.read_text(encoding="utf-8")


def test_changelog_diffs_the_pr_head_not_the_merge_ref():
    text = CI.read_text(encoding="utf-8")
    assert "github.event.pull_request.head.sha" in text and '--head "$HEAD_SHA"' in text


def test_no_cla_or_dco_machinery():
    # Decision D2 (owner ruling): inbound = outbound under the repository licences; no CLA, no DCO.
    text = CI.read_text(encoding="utf-8")
    assert "CLA" not in text.replace("no CLA or DCO", "").replace("There is no CLA", "") and "Signed-off-by" not in text
    assert not (ROOT / "CLA.md").exists() and not (ROOT / "contributors").exists()
    assert not (ROOT / ".github/workflows/cla-nudge.yml").exists()


def test_ci_commands_match_contributing():
    contributing = (ROOT / "CONTRIBUTING.md").read_text(encoding="utf-8")
    runs = [s["run"].strip() for s in steps(ci()) if "run" in s]
    for cmd in ("dotnet test sim/Statecraft.sln -c Release", "pytest tools/tests -q", "pytest scripts/oss/tests -q",
                "uv run scripts/oss/check_repo.py"):
        assert any(cmd in r for r in runs), cmd
        assert cmd.replace(" -c Release", "") in contributing, cmd


def test_codeowners_makes_the_maintainer_owner_of_everything():
    lines = [l for l in (ROOT / ".github/CODEOWNERS").read_text().splitlines() if l and not l.startswith("#")]
    assert lines == ["* @eyalgolan"]
```

Append to `scripts/oss/tests/test_policies.py`:

```python
def test_contributing_names_every_ci_command_and_the_unity_gate():
    text = read("CONTRIBUTING.md")
    for cmd in ("dotnet test sim/Statecraft.sln",
                "uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 "
                "--with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q",
                "uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q",
                "uv run scripts/oss/check_repo.py",
                "-buildTarget OSXUniversal -runTests -testPlatform EditMode", "## What CI runs", "CHANGELOG.md"):
        assert cmd in text, cmd


def test_changelog_has_unreleased_on_top():
    text = read("CHANGELOG.md")
    assert text.index("## [Unreleased]") == text.index("## [")
```

- [ ] **Step 2: Run them to verify they fail.** Run: `oss_pytest`. Expected: collection fails with `ModuleNotFoundError: No module named 'check_repo'`.

- [ ] **Step 3: Write the policy module and the checker.**

`scripts/oss/oss_policy.py`:

```python
"""Shared rules for the open-source checks: what a secret looks like, which emails and paths are personal, what files a
public tree must carry, which files need a provenance row, and how large a file may be. One module, so check_repo.py
(tip of tree) and scan_history.py (every commit) can never disagree about what they look for."""
from __future__ import annotations

import re

SECRET_PATTERNS: dict[str, re.Pattern[str]] = {
    "elevenlabs-key": re.compile(r"\bsk_[0-9a-f]{40,}\b"),
    "anthropic-key": re.compile(r"sk-ant-[A-Za-z0-9_\-]{20,}"),
    "openai-key": re.compile(r"\bsk-(?:proj-)?[A-Za-z0-9]{20,}"),
    "aws-access-key-id": re.compile(r"\b(?:AKIA|ASIA)[0-9A-Z]{16}\b"),
    "github-token": re.compile(r"\b(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}"),
    "google-api-key": re.compile(r"\bAIza[0-9A-Za-z_\-]{35}\b"),
    "slack-token": re.compile(r"xox[baprs]-[A-Za-z0-9-]{10,}"),
    "private-key": re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----"),
    "jwt": re.compile(r"\beyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}"),
    "secret-assignment": re.compile(
        r"(?i)(?:api[_-]?key|secret|token|passw(?:or)?d)['\"]?\s*[:=]\s*['\"][A-Za-z0-9_\-/+=]{16,}['\"]"),
    # Unity writes these into ProjectSettings.asset when the project is linked to Unity Cloud. Empty is fine. The value
    # shapes are narrow on purpose (a GUID; a slug that ends the line) so that this file and its tests, which mention
    # the keys, do not match themselves.
    "unity-cloud-project-id": re.compile(r"cloudProjectId:[ \t]*[0-9a-fA-F]{8}-"),
    "unity-organization-id": re.compile(r"organizationId:[ \t]*[A-Za-z0-9][\w.-]*[ \t]*$"),
    "unity-licence-secret": re.compile(r"(?i)UNITY_(?:SERIAL|PASSWORD|LICENSE)\s*[:=]\s*\S{6,}"),
}

# The local part must start with a letter or digit (so "@pytest.mark" is not an address), and the last domain label
# must be letters only (so a Unity package id such as "com.unity.template.urp-blank@17.0.14" is not an address).
EMAIL = re.compile(r"(?<![\w.+-])[A-Za-z0-9][A-Za-z0-9._%+\-]*@((?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,})(?![\w.-])")
ALLOWED_EMAIL_DOMAINS = frozenset({"users.noreply.github.com", "anthropic.com", "example.com", "example.org",
                                   "example.net"})

HOME_PATH = re.compile(r"(?:/Users/(?!Shared/)|/home/)[A-Za-z][\w.\-]*/")

MAX_FILE_BYTES = 10 * 1024 * 1024

REQUIRED_FILES: tuple[str, ...] = (
    "LICENSE", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt", "LICENSES/CC0-1.0.txt",
    "LICENSES/YamlDotNet-MIT.txt", "LICENSING.md", "NOTICE", "THIRD-PARTY-NOTICES.md", "ASSETS.yaml",
    "README.md", "README.he.md", "BUILDING.md", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "SECURITY.md",
    "TRADEMARK.md", "CHANGELOG.md", "docs/CONTENT_POLICY.md",
    "docs/UNTRUSTED_PR_REVIEW.md", "docs/RELEASING.md", "docs/oss/DECISIONS.md", ".github/CODEOWNERS",
    ".github/pull_request_template.md", ".github/workflows/ci.yml",
)

# Files whose provenance ASSETS.yaml must record (check "assets"). Any file git itself treats as binary counts too.
BINARY_EXTENSIONS = frozenset({
    ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff", ".tga", ".psd", ".exr", ".hdr", ".svg",
    ".mp4", ".mov", ".webm", ".r16", ".u16", ".raw",
    ".ttf", ".otf", ".woff", ".woff2",
    ".wav", ".ogg", ".mp3", ".flac", ".aiff", ".m4a",
    ".fbx", ".obj", ".blend", ".glb", ".gltf",
    ".pdf", ".unitypackage",
})
PROVENANCE_GLOBS: tuple[str, ...] = ("content/map/*", "unity/Assets/StreamingAssets/map/*")

ALLOWED_LICENCES = frozenset({"MIT", "CC-BY-4.0", "CC-BY-SA-4.0", "CC0-1.0", "OFL-1.1", "Public-Domain",
                              "Copernicus-DEM", "IMS-data.gov.il", "Unity-Companion-License", "unknown"})
STATUSES = frozenset({"verified", "owner-verify"})
RULE_KEYS = ("glob", "licence", "source", "status")


def secret_kinds(line: str) -> list[str]:
    return [kind for kind, rx in SECRET_PATTERNS.items() if rx.search(line)]


def email_kinds(line: str) -> list[str]:
    for m in EMAIL.finditer(line):
        domain = m.group(1).lower()
        local = m.group(0).split("@", 1)[0].lower()
        if domain in ALLOWED_EMAIL_DOMAINS or local == "noreply":
            continue
        return ["personal-email"]
    return []


def home_path_kinds(line: str) -> list[str]:
    return ["home-path"] if HOME_PATH.search(line) else []
```

`scripts/oss/check_repo.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Policy checks over the TRACKED files at the tip of the tree, the tree a clone receives. Prints one line per finding
(check, path, line, kind) and never the matched text, so a found secret is not copied into a CI log.

Known, reviewed exceptions live in scripts/oss/allow.yaml ({check, glob, reason}); a row that suppresses nothing is
itself reported (stale-allow), so the list cannot silently outlive its reason.

Usage: uv run scripts/oss/check_repo.py [--root DIR] [--only a,b,...] [--release]
--release also refuses open owner decisions (docs/oss/DECISIONS.md) and ASSETS.yaml rows still marked owner-verify
or with licence unknown; CI runs without it, the release gate with it. Exit 0 clean, 1 findings, 2 usage error."""
from __future__ import annotations

import argparse
import fnmatch
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import yaml

sys.path.insert(0, str(Path(__file__).resolve().parent))
import oss_policy as P  # noqa: E402

ALLOW_FILE = "scripts/oss/allow.yaml"
ASSET_FILES = ("ASSETS.yaml", "ASSETS.portraits.yaml")


@dataclass(frozen=True)
class Finding:
    check: str
    path: str
    line: int  # 0 when the finding is about the whole file
    kind: str

    def __str__(self) -> str:
        where = f"{self.path}:{self.line}" if self.line else self.path
        return f"{self.check}: {where}: {self.kind}"


def tracked_files(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return sorted(p for p in out.decode("utf-8").split("\0") if p)


def git_binary_files(root: Path) -> set[str]:
    """Files git's own attribute logic treats as binary in the index (`i/-text`)."""
    out = subprocess.run(["git", "-C", str(root), "ls-files", "--eol", "-z"], capture_output=True, check=True).stdout
    found = set()
    for rec in out.decode("utf-8").split("\0"):
        if rec.startswith("i/-text"):
            found.add(rec.split("\t", 1)[1])
    return found


def _text(root: Path, rel: str) -> str | None:
    p = root / rel
    if p.is_symlink() or not p.is_file():
        return None
    data = p.read_bytes()
    if b"\0" in data[:8192]:
        return None
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError:
        return None


def _line_check(name: str, fn: Callable[[str], list[str]]):
    def check(root: Path, files: list[str], release: bool) -> list[Finding]:
        found = []
        for rel in files:
            text = _text(root, rel)
            if text is None:
                continue
            for n, line in enumerate(text.splitlines(), 1):
                found += [Finding(name, rel, n, k) for k in fn(line)]
        return found
    return check


def check_sizes(root: Path, files: list[str], release: bool) -> list[Finding]:
    return [Finding("sizes", rel, 0, "over-size-limit") for rel in files
            if (root / rel).is_file() and not (root / rel).is_symlink()
            and (root / rel).stat().st_size > P.MAX_FILE_BYTES]


def check_symlinks(root: Path, files: list[str], release: bool) -> list[Finding]:
    found = []
    for rel in files:
        p = root / rel
        if not p.is_symlink():
            continue
        target = os.readlink(p)
        inside = (p.parent / target).resolve().is_relative_to(root.resolve())
        if os.path.isabs(target) or not inside:
            found.append(Finding("symlinks", rel, 0, "symlink-outside-repo"))
    return found


def check_required(root: Path, files: list[str], release: bool) -> list[Finding]:
    tracked = set(files)
    return [Finding("required", rel, 0, "missing") for rel in P.REQUIRED_FILES if rel not in tracked]


def check_decisions(root: Path, files: list[str], release: bool) -> list[Finding]:
    rel = "docs/oss/DECISIONS.md"
    if not release or rel not in files:
        return []
    text = _text(root, rel) or ""
    return [Finding("decisions", rel, n, "open-decision")
            for n, line in enumerate(text.splitlines(), 1) if line.lstrip().startswith("- [ ]")]


def load_asset_rules(root: Path) -> tuple[list[dict], list[dict]]:
    rules: list[dict] = []
    removed: list[dict] = []
    for name in ASSET_FILES:
        p = root / name
        if p.is_file():
            doc = yaml.safe_load(p.read_text(encoding="utf-8")) or {}
            rules += [dict(r, _file=name) for r in doc.get("rules") or []]
            removed += doc.get("removed") or []
    return rules, removed


def _needs_provenance(rel: str, binary: set[str]) -> bool:
    return (rel in binary or Path(rel).suffix.lower() in P.BINARY_EXTENSIONS
            or any(fnmatch.fnmatch(rel, g) for g in P.PROVENANCE_GLOBS))


def check_assets(root: Path, files: list[str], release: bool) -> list[Finding]:
    """Note: fnmatch's '*' also matches '/', so "promo/ref/*-src.jpg" covers subfolders too. That is intended."""
    rules, removed = load_asset_rules(root)
    found: list[Finding] = []
    good: list[dict] = []
    for r in rules:
        missing = [k for k in P.RULE_KEYS if not r.get(k)]
        if missing:
            found.append(Finding("assets", r["_file"], 0, f"bad-rule missing {','.join(missing)}"))
            continue
        good.append(r)
        if r["licence"] not in P.ALLOWED_LICENCES:
            found.append(Finding("assets", r["_file"], 0, f"unknown-licence {r['licence']}"))
        if r["status"] not in P.STATUSES:
            found.append(Finding("assets", r["_file"], 0, f"bad-status {r['status']}"))
        elif release and (r["status"] == "owner-verify" or r["licence"] == "unknown"):
            found.append(Finding("assets", r["_file"], 0, f"owner-verify {r['glob']}"))
        if not r.get("allow_empty") and not any(fnmatch.fnmatch(f, r["glob"]) for f in files):
            found.append(Finding("assets", r["_file"], 0, f"stale-rule {r['glob']}"))
    binary = git_binary_files(root)
    for rel in files:
        if rel in ASSET_FILES or not _needs_provenance(rel, binary):
            continue
        if not any(fnmatch.fnmatch(rel, r["glob"]) for r in good):
            found.append(Finding("assets", rel, 0, "no-provenance"))
    tracked = set(files)
    found += [Finding("assets", d["path"], 0, "removed-but-tracked") for d in removed if d["path"] in tracked]
    return found


CHECKS: dict[str, Callable[[Path, list[str], bool], list[Finding]]] = {
    "secrets": _line_check("secrets", P.secret_kinds),
    "emails": _line_check("emails", P.email_kinds),
    "home-paths": _line_check("home-paths", P.home_path_kinds),
    "sizes": check_sizes,
    "symlinks": check_symlinks,
    "required": check_required,
    "decisions": check_decisions,
    "assets": check_assets,
}


def apply_allowlist(root: Path, findings: list[Finding], checks: list[str]) -> list[Finding]:
    p = root / ALLOW_FILE
    rows: list[dict] = []
    if p.is_file():
        rows = (yaml.safe_load(p.read_text(encoding="utf-8")) or {}).get("allow") or []
    kept, used = [], set()
    for f in findings:
        hit = next((i for i, r in enumerate(rows)
                    if r.get("check") == f.check and fnmatch.fnmatch(f.path, r.get("glob", ""))), None)
        if hit is None:
            kept.append(f)
        else:
            used.add(hit)
    for i, r in enumerate(rows):
        if not r.get("reason"):
            kept.append(Finding("allow", ALLOW_FILE, 0, f"allow-row-without-reason {r.get('glob')}"))
        elif r.get("check") in checks and i not in used:
            kept.append(Finding("allow", ALLOW_FILE, 0, f"stale-allow {r.get('check')} {r.get('glob')}"))
    return kept


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--root", type=Path, default=Path.cwd())
    ap.add_argument("--only", default="")
    ap.add_argument("--release", action="store_true")
    args = ap.parse_args(argv)
    names = [n for n in args.only.split(",") if n] or list(CHECKS)
    unknown = [n for n in names if n not in CHECKS]
    if unknown:
        print(f"unknown check(s): {', '.join(unknown)}; known: {', '.join(CHECKS)}")
        return 2
    root = args.root.resolve()
    files = tracked_files(root)
    findings = [f for n in names for f in CHECKS[n](root, files, args.release)]
    findings = apply_allowlist(root, findings, names)
    for f in findings:
        print(f)
    if findings:
        print(f"FAIL: {len(findings)} finding(s) in checks {', '.join(names)}")
        return 1
    print(f"OK: checks {', '.join(names)} over {len(files)} tracked files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

`scripts/oss/allow.yaml` (review C1: the only fixtures on `602183f` that look like home paths):

```yaml
# Reviewed exceptions to scripts/oss/check_repo.py. Each row: the check it silences, a glob (fnmatch: '*' also
# matches '/'), and the reason. A row that silences nothing is reported as stale-allow.
allow:
  - check: home-paths
    glob: "unity/Assets/Tests/*"
    reason: "Fake home paths in the boot-failure path-redaction tests (GameSceneTests, SaveUiTests); fixtures by design"
```

- [ ] **Step 4: Write the changelog check and the action pinner.**

`scripts/oss/changelog_check.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Per-pull-request changelog rule: if the PR changes a user-visible path, it must touch CHANGELOG.md or carry the
`no-changelog` label. Looks at the PR's own diff only, never at earlier history.

Usage: uv run scripts/oss/changelog_check.py --base <base-sha> [--head HEAD] [--skip-label true|false]"""
from __future__ import annotations

import argparse
import subprocess
import sys

USER_VISIBLE = ("content/", "sim/Statecraft.Sim/", "sim/Statecraft.Cli/", "unity/Assets/", "unity/ProjectSettings/",
                "unity/Packages/")
NOT_USER_VISIBLE = ("unity/Assets/Tests/",)


def user_visible(paths: list[str]) -> list[str]:
    return [p for p in paths if p.startswith(USER_VISIBLE) and not p.startswith(NOT_USER_VISIBLE)]


def verdict(paths: list[str], skip_label: bool) -> tuple[bool, str]:
    visible = user_visible(paths)
    if not visible:
        return True, "No user-visible change; no CHANGELOG entry needed."
    if "CHANGELOG.md" in paths:
        return True, "CHANGELOG.md is updated."
    if skip_label:
        return True, "The no-changelog label is set."
    listed = "\n".join(f"  {p}" for p in visible)
    return False, ("These user-visible paths changed without a CHANGELOG.md entry:\n" + listed +
                   "\nAdd a line under ## [Unreleased] in CHANGELOG.md, or ask a maintainer for the no-changelog label.")


def changed_paths(base: str, head: str) -> list[str]:
    out = subprocess.run(["git", "diff", "--name-only", f"{base}...{head}"], capture_output=True, text=True, check=True)
    return [p for p in out.stdout.splitlines() if p]


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", required=True)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--skip-label", default="false")
    a = ap.parse_args(argv)
    ok, msg = verdict(changed_paths(a.base, a.head), a.skip_label.lower() == "true")
    print(msg)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
```

`scripts/oss/pin_actions.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Pin every `uses: owner/repo@ref` in .github/workflows/*.yml to the ref's full commit SHA, keeping the ref as a
trailing comment. Resolves with a read-only `gh api` GET. A tag can be moved by its owner; a SHA cannot (no_human
5f99b7af3: unpinned third-party downloads in the release build).

Usage: uv run scripts/oss/pin_actions.py"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path
from typing import Callable

ROOT = Path(__file__).resolve().parents[2]
USES = re.compile(r"^(?P<lead>\s*-?\s*uses:\s*)(?P<action>[\w.-]+/[\w./-]+)@(?P<ref>[^\s#]+)(?P<rest>.*)$")


def gh_resolve(action: str, ref: str) -> str:
    repo = "/".join(action.split("/")[:2])
    out = subprocess.run(["gh", "api", f"repos/{repo}/commits/{ref}", "--jq", ".sha"], capture_output=True,
                         text=True, check=True).stdout.strip()
    if not re.fullmatch(r"[0-9a-f]{40}", out):
        raise SystemExit(f"could not resolve {action}@{ref}: {out!r}")
    return out


def pin_text(text: str, resolve: Callable[[str, str], str]) -> str:
    lines = []
    for line in text.splitlines(keepends=True):
        m = USES.match(line.rstrip("\n"))
        if m and not re.fullmatch(r"[0-9a-f]{40}", m["ref"]):
            sha = resolve(m["action"], m["ref"])
            line = f"{m['lead']}{m['action']}@{sha} # {m['ref']}\n"
        lines.append(line)
    return "".join(lines)


def main() -> int:
    for wf in sorted((ROOT / ".github/workflows").glob("*.yml")):
        before = wf.read_text(encoding="utf-8")
        after = pin_text(before, gh_resolve)
        if after != before:
            wf.write_text(after, encoding="utf-8")
            print(f"pinned {wf.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 5: Write `CHANGELOG.md`, `.github/CODEOWNERS` and `.github/workflows/ci.yml`.**

`CHANGELOG.md`:

```markdown
# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Added
- Browser-playable 1948–1951 (milestone M3): HUD, Cabinet with appointments, Budget, Events, Defence and "Why did this
  happen?" screens over the 3D hex map; sourced content to 31 December 1951; saves; a calibrated economy; the golden
  hash checked in WebGL and macOS players.
- The map (milestone M2): terrain from Copernicus elevation data, biomes from rainfall, 1948 settlements on hexes.
- Open-source release (public since 2026-10-10): licences (MIT code, CC BY-SA 4.0 content, CC BY 4.0 original docs
  and art, CC0 for AI-only images), asset provenance (`ASSETS.yaml`), contributor
  and content policies, CI for the sim, tools and repository policy.
```

`.github/CODEOWNERS`:

```
# Every pull request needs the maintainer's approval. the `main-review` ruleset requires a code-owner review, and this
# file makes the maintainer the owner of every path, so another collaborator's approval does not satisfy it.
* @eyalgolan
```

`.github/workflows/ci.yml` (written with tags; Step 6 pins them):

```yaml
name: CI

# Nothing here uses a secret, calls a paid API, runs an AI agent or pushes anywhere. Pull requests from forks run
# untrusted code (MSBuild targets, pytest conftest files, uv inline scripts), so the token is read-only, checkouts keep
# no credentials, and there is no secret to steal. A pull request can also edit this file and make its own checks green;
# CODEOWNERS review of any .github/ diff is the guard (docs/UNTRUSTED_PR_REVIEW.md). There is no CLA or DCO check:
# contributions come in under the repository licences (inbound = outbound, GitHub Terms of Service D.6; decision D2).
# Unity edit-mode tests are not run here; CONTRIBUTING.md, "What CI runs", says why.
# No `schedule:` and no `pull_request_target`, on purpose. Every action is pinned to a commit SHA
# (scripts/oss/pin_actions.py). The job names are the required checks in scripts/oss/github/ruleset-protect-main.json;
# a test keeps the two identical.

on:
  push:
    branches: [main]
  pull_request:
    types: [opened, synchronize, reopened, labeled, unlabeled]

permissions:
  contents: read

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  sim:
    name: Sim (dotnet test)
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@v4
        with:
          persist-credentials: false
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Simulation and content tests
        run: dotnet test sim/Statecraft.sln -c Release

  tools:
    name: Tools (pytest)
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@v4
        with:
          persist-credentials: false
      # tools/tests/test_package_release.py runs the sim's release gate (dotnet run --project sim/Statecraft.Cli).
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - uses: astral-sh/setup-uv@v6
        with:
          version: "0.12.5"
      - name: Data tool tests
        run: uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q

  policy:
    name: Repo policy
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - uses: actions/checkout@v4
        with:
          persist-credentials: false
      - uses: astral-sh/setup-uv@v6
        with:
          version: "0.12.5"
      - name: Policy checker tests
        run: uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q
      - name: Licences, provenance, secrets and required files at the tip
        run: uv run scripts/oss/check_repo.py --only secrets,emails,home-paths,sizes,symlinks

  changelog:
    name: Changelog
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-latest
    timeout-minutes: 5
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
          persist-credentials: false
      - uses: astral-sh/setup-uv@v6
        with:
          version: "0.12.5"
      - name: A user-visible change carries a CHANGELOG line or the no-changelog label
        env:
          BASE_SHA: ${{ github.event.pull_request.base.sha }}
          HEAD_SHA: ${{ github.event.pull_request.head.sha }}
          SKIP_LABEL: ${{ contains(github.event.pull_request.labels.*.name, 'no-changelog') }}
        run: uv run scripts/oss/changelog_check.py --base "$BASE_SHA" --head "$HEAD_SHA" --skip-label "$SKIP_LABEL"
```

- [ ] **Step 6: Pin the actions (read-only `gh api` GETs).**

```bash
gh auth status
uv run scripts/oss/pin_actions.py           # prints "pinned .github/workflows/ci.yml"
grep -n "uses:" .github/workflows/ci.yml    # every line: owner/repo@<40 hex> # vN
```

- [ ] **Step 7: Amend `CONTRIBUTING.md`.** Apply these edits in order:

1. Replace:

```markdown
   - `dotnet test sim/Statecraft.sln`, which must pass with 0 warnings (warnings are errors);
   - the tools tests: the command is in `tools/README.md`;
   - Unity EditMode tests, if you touched `unity/`. The command is in `docs/dev-setup.md`. There must be 0 `warning CS` in the log.
```

   with:

```markdown
   - `dotnet test sim/Statecraft.sln`, which must pass with 0 warnings (warnings are errors);
   - the tools tests (they need the .NET SDK too: one runs the sim's release gate):
     `uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q`;
   - the repository policy checks:
     `uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q` and `uv run scripts/oss/check_repo.py`;
   - Unity EditMode tests, if you touched `unity/` (`-buildTarget OSXUniversal` keeps an earlier WebGL build from leaving the editor on the wrong platform):
     `"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log`. There must be 0 `warning CS` in the log.
```

2. Replace:

```markdown
5. **Keep the pull request focused:** one change per PR, and no unrelated refactors.
```

   with:

```markdown
5. **Keep the pull request focused:** one change per PR, and no unrelated refactors.
6. **Changelog.** If you change `sim/`, `content/` or `unity/` (tests excepted), add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). CI's `Changelog` job checks it; the maintainer can add the `no-changelog` label to a change no player would notice.
```

3. Replace:

```markdown
- The maintainer reviews every PR. CI is being set up; until then, the pasted test output is the evidence.
```

   with:

```markdown
- The maintainer reviews every PR (`.github/CODEOWNERS`); a push after approval needs a fresh approval. PRs are squash-merged and keep you as the author.

## What CI runs

Every pull request and every push to `main` runs `Sim (dotnet test)`, `Tools (pytest)`, `Repo policy`, and, on pull requests, `Changelog`. The jobs use no secrets, call no paid API, run no AI agent and push nothing.

**Unity EditMode tests are not in CI.** Running Unity in CI needs the maintainer's Unity account credentials stored as repository secrets. GitHub never gives secrets to pull requests from forks, so the job could not run on the pull requests that most need it, and those credentials would sit on machines that run outside code. So the Unity tests are a local gate: paste their result in your PR, and the maintainer re-runs them before merging any change under `unity/`.
```

- [ ] **Step 8: Run the tests.** Run: `oss_pytest`. Expected: all pass (54 in the scratch run, with the actions pinned).

- [ ] **Step 9: Prove the checker on the published tree (review C1).**

```bash
uv run scripts/oss/check_repo.py --only secrets,emails,home-paths,sizes,symlinks
```

Expected: `OK: checks secrets, emails, home-paths, sizes, symlinks over …`. (Measured in a scratch clone of `602183f` plus this task's files: OK. Without `allow.yaml` it reports exactly `unity/Assets/Tests/EditMode/GameSceneTests.cs:627`, `:643` and `SaveUiTests.cs:227`, all `home-path`; the old email pattern also flagged the Unity package ids `…urp-blank@17.0.14` and `…assistant@2.20.0-pre.2`, which the letters-only TLD rule no longer reads as addresses.)

- [ ] **Step 10: Run the CI commands on Linux locally (review C2: the tools job needs dotnet).**

```bash
colima start
docker run --rm -v "$PWD":/w -w /w mcr.microsoft.com/dotnet/sdk:10.0 dotnet test sim/Statecraft.sln -c Release
docker run --rm -v "$PWD":/w -w /w mcr.microsoft.com/dotnet/sdk:10.0 bash -c 'apt-get update -qq && apt-get install -y -qq python3 python3-pip >/dev/null && pip install -q --break-system-packages uv==0.12.5 && uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q && uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q && uv run scripts/oss/check_repo.py --only secrets,emails,home-paths,sizes,symlinks'
colima stop
```

Expected: `dotnet test` `Passed!`, the tools tests pass (including `test_package_release.py`, which runs `dotnet run --project sim/Statecraft.Cli`), the policy tests pass, and the checker prints `OK`. **Never `--deselect` a test** unless it is genuinely platform-bound (it needs macOS or a GPU); if one is, name it and the reason in the commit message and in CONTRIBUTING, and report it.

- [ ] **Step 11: Commit.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add scripts/oss .github CHANGELOG.md CONTRIBUTING.md
git commit -m "ci(oss): sim, tools (with dotnet) and repo-policy jobs, changelog check, pinned actions, CODEOWNERS

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Issue forms, including the historical-error form that requires a source

**Files:**
- Create: `.github/ISSUE_TEMPLATE/config.yml`, `bug_report.yml`, `historical_accuracy.yml`, `feature_request.yml`
- Modify: `scripts/oss/tests/test_policies.py` (append)

**Interfaces:**
- Produces: `?template=historical_accuracy.yml` and `?template=bug_report.yml`, which the README link bar (Task 7) uses; the `historical-accuracy` label (Task 13).

- [ ] **Step 1: Append the failing test:**

```python
def test_historical_accuracy_form_requires_a_source():
    import yaml
    form = yaml.safe_load(read(".github/ISSUE_TEMPLATE/historical_accuracy.yml"))
    body = {b.get("id"): b for b in form["body"]}
    assert body["sources"]["validations"]["required"] is True
    assert {"where", "wrong", "sources"} <= set(body)
    cfg = yaml.safe_load(read(".github/ISSUE_TEMPLATE/config.yml"))
    assert cfg["blank_issues_enabled"] is False
    assert any("security/advisories/new" in c["url"] for c in cfg["contact_links"])
```

- [ ] **Step 2: Run it to verify it fails** (`FileNotFoundError` for the form).

- [ ] **Step 3: Write the forms.**

`.github/ISSUE_TEMPLATE/config.yml`:

```yaml
blank_issues_enabled: false
contact_links:
  - name: Security vulnerability or conduct report
    url: https://github.com/eyalgolan/statecraft-israel/security/advisories/new
    about: Report privately. For a conduct report, start the title with "Conduct:". Do not open a public issue.
```

`.github/ISSUE_TEMPLATE/bug_report.yml`:

```yaml
name: Bug report
description: The game or a tool behaves differently from what it should
labels: ["bug"]
body:
  - type: textarea
    id: what
    attributes:
      label: What happened
      description: What you expected, and what you got instead.
    validations:
      required: true
  - type: textarea
    id: repro
    attributes:
      label: Steps to reproduce
      placeholder: |
        1. Start a new game
        2. Open the Cabinet screen
        3. ...
    validations:
      required: true
  - type: textarea
    id: logs
    attributes:
      label: Console or log output
      description: For the browser build, the lines from the developer console (GAME OK / TURN / errors).
      render: text
  - type: input
    id: version
    attributes:
      label: Commit SHA
      placeholder: git rev-parse --short HEAD
    validations:
      required: true
  - type: input
    id: env
    attributes:
      label: Platform
      placeholder: macOS 15, Chrome 140 (WebGL build) / Unity 6000.3.25f1
    validations:
      required: true
  - type: checkboxes
    id: nosecrets
    attributes:
      label: Before you submit
      options:
        - label: The text above contains no token, key or personal data.
          required: true
```

`.github/ISSUE_TEMPLATE/historical_accuracy.yml`:

```yaml
name: Historical accuracy
description: A fact in the game is wrong or its source does not support it
labels: ["historical-accuracy"]
body:
  - type: markdown
    attributes:
      value: |
        Read docs/CONTENT_POLICY.md first. Facts are open to correction with sources. The game's Israeli perspective is a
        stated design decision and is not changed through issues (section 1).
  - type: input
    id: where
    attributes:
      label: Where
      description: File and field, or the screen and date in the game.
      placeholder: content/events/altalena.yaml, text
    validations:
      required: true
  - type: textarea
    id: wrong
    attributes:
      label: What is wrong, and what it should say
    validations:
      required: true
  - type: textarea
    id: sources
    attributes:
      label: Sources
      description: At least one published book, article, official record or reputable reference the reader can check. Links, with page numbers where possible.
    validations:
      required: true
  - type: checkboxes
    id: conduct
    attributes:
      label: Conduct
      options:
        - label: I am disputing a fact with sources, not the game's perspective or a person (CODE_OF_CONDUCT.md).
          required: true
```

`.github/ISSUE_TEMPLATE/feature_request.yml`:

```yaml
name: Idea or feature request
description: Something the game or its tools should do
labels: ["enhancement"]
body:
  - type: textarea
    id: problem
    attributes:
      label: What problem does this solve
      description: The situation in the game or the workflow, not the solution yet.
    validations:
      required: true
  - type: textarea
    id: proposal
    attributes:
      label: What you propose
  - type: textarea
    id: history
    attributes:
      label: Historical basis (for gameplay or content ideas)
      description: Sources for the history the idea models, if any.
```

- [ ] **Step 4: Run the tests** (all of `test_policies.py` written so far pass) **and commit.**

```bash
git add .github/ISSUE_TEMPLATE scripts/oss/tests/test_policies.py
git commit -m "docs(github): issue forms; a historical-accuracy report requires a source

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: CODE_OF_CONDUCT and CONTENT_POLICY

**Files:**
- Create: `CODE_OF_CONDUCT.md`, `docs/CONTENT_POLICY.md`
- Modify: `CONTRIBUTING.md` ("History and content rules", "Conduct"), `scripts/oss/tests/test_policies.py` (append)

**Interfaces:**
- Consumes: the private-report URL (Task 2); the historical-accuracy form (Task 4).
- Produces: `docs/CONTENT_POLICY.md` §1 (perspective), §2 (rules, including only mechanisms that exist: review I3), §3 (how to dispute a fact), §4 (what not to add, including AI likenesses: review M10).

- [ ] **Step 1: Append the failing tests:**

```python
def test_content_policy_states_the_owner_rules_and_only_real_mechanisms():
    text = read("docs/CONTENT_POLICY.md")
    for s in ("Israeli framing", "Every fact is sourced", "No invented quotes", "How to dispute a fact",
              "Statecraft.Cli --release-gate", "Historian review is planned for M4", "Wikipedia"):
        assert s in text, s
    assert "DRAFT stamp" not in text and "Release builds include only fully reviewed content" not in text


def test_code_of_conduct_has_a_private_contact_and_the_history_section():
    text = read("CODE_OF_CONDUCT.md")
    assert "[INSERT" not in text and "getnohuman" not in text
    assert "Conduct:" in text and "security/advisories/new" in text
    assert "## Historical content and political discussion" in text
    assert "Contributor Covenant" in text and "version 2.1" in text
```

- [ ] **Step 2: Run them to verify they fail.**

- [ ] **Step 3: Write `docs/CONTENT_POLICY.md`.**

```markdown
# Historical content policy

Statecraft: Israel puts real people, real places and real wars on screen. This page is the contract for every word,
number and picture in `content/` and in the game. It binds the maintainer, contributors and AI agents alike. It is
written from the project's design spec (`docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`, §1 and §5).

## 1. The perspective is Israeli, and it says so

The player leads the Yishuv and the State of Israel. Narration and terminology use **Israeli framing**, and each fact
takes one value from Israeli official or mainstream sources: the Central Bureau of Statistics, the IDF, the Ministry of
Foreign Affairs, the State Archives, and mainstream Israeli historiography. Wikipedia (English and Hebrew) is a primary
cited source for many facts; where it is the source, the entry cites it, and that is why `content/` is CC BY-SA 4.0. Contested ranges and opposing-narrative
panels are not shown. Arab and Palestinian actors are computer-run actors with their own goals.

This is a design decision by the maintainer. Issues and pull requests that ask to change the perspective will be closed
with a link to this section. That is not a judgement on the person asking, and it is not up for vote. A fork can make
a different choice under its own name (see `TRADEMARK.md`).

## 2. What holds regardless of perspective

These rules protect the game and the people in it. They are not a viewpoint:

1. **Every fact is sourced.** Every date, number, name and office in `content/` carries `sources:` (a citation or a
   URL, plus the date it was checked) and a review `status`. As of M3 every fact is at least `fact-checked`. Events
   with a sensitive tag must also be `sensitivity-reviewed` before any build leaves the maintainer's machine; the
   simulation enforces that (`Statecraft.Cli --release-gate`, the `-shareable` build flag and
   `tools/package_release.py` all refuse otherwise). Historian review is planned for M4 and is not yet a gate.
2. **No invented quotes.** No words are attributed to a real person unless they are sourced verbatim. Event text
   describes; it does not put speech in anyone's mouth.
3. **Choices never reward atrocities.** Options tagged `holocaust`, `terror`, `massacre` or `war-casualties` cannot grant
   treasury or score rewards for the atrocity side of a choice, and they need a recorded sensitivity review. Automated
   tests enforce this (`dotnet test`).
4. **The Holocaust is an event the player reacts to, never one the player plays.** It is shown without swastikas.
5. **Real people.** People appear in their public roles and documented acts. Competence and loyalty values are game
   balance, not judgements of the person, and `content/people.yaml` labels them so. People who are alive are excluded
   from release builds until a legal review is recorded. Portraits are public-domain or CC0 photographs only; share-alike
   sources are refused.
6. **AI-assisted writing** drafts only from a provided source packet and lists its factual claims. A separate verifier
   checks each claim against the packet, and any mismatch blocks the text. A human reviews before `fact-checked`.

## 3. How to dispute a fact

Facts are open to correction. The framing (section 1) is not.

1. Open an issue with the **Historical accuracy** form. Name the file and the field (for example
   `content/events/altalena.yaml`, `text`), say what is wrong, and give at least one source the reader can check. A
   source is a published book or article, an official record, or a reputable reference work. A link to a social-media
   post is not a source.
2. The maintainer checks the source against the current one and answers in the issue. When the sources disagree, the
   game keeps one value, chosen by the rule in section 1, and the issue records why.
3. A correction lands as a pull request that changes the value **and** its `sources:` entry, with today's date in
   `checked:`.
4. The maintainer's decision closes the dispute. Re-opening the same dispute without a new source is off-topic under
   `CODE_OF_CONDUCT.md`.

## 4. What contributors must not add

- Content without sources, or with a source that does not support the claim.
- Quotes, speeches or letters that are not sourced verbatim.
- Mechanics or text that reward or celebrate violence against civilians, from any side.
- Images of real people that are not public domain or CC0. AI-generated likenesses of real people appear only in
  concept art (`promo/final/04`, `08`), are marked AI-generated in `ASSETS.yaml`, and are never presented as photographs
  or used in the game.
- Hate speech, slurs or dehumanising language about any group, in content, code comments, issues or commits.
```

- [ ] **Step 4: Write `CODE_OF_CONDUCT.md`** from the Contributor Covenant 2.1 text in no_human (local, the same text), with a private contact, then append the project section:

```bash
sed 's|may be reported to the community leaders responsible for enforcement at conduct@getnohuman\.com\.|may be reported to the community leaders responsible for enforcement privately, through this repository'"'"'s Security tab: choose "Report a vulnerability" and start the title with "Conduct:" (https://github.com/eyalgolan/statecraft-israel/security/advisories/new). Only the maintainers can read it.|' \
  ~/git/no_human-public/CODE_OF_CONDUCT.md > CODE_OF_CONDUCT.md
grep -c "getnohuman" CODE_OF_CONDUCT.md   # 0
cat >> CODE_OF_CONDUCT.md <<'EOF2'

## Historical content and political discussion

This project depicts the history of Israel, including its wars, with real people and an Israeli perspective (see
`docs/CONTENT_POLICY.md`). People who take part will disagree about that history. In every project space:

- Discuss sources, not people. Argue with evidence, and link to it.
- Personal or political attacks on anyone, including on public figures as people, are unacceptable, and so are slurs,
  dehumanising language, denial or celebration of atrocities, and calls for violence against any group.
- The game's perspective is a stated design decision. Repeatedly re-opening it, in issues, pull requests or reviews,
  after the maintainer has pointed to `docs/CONTENT_POLICY.md` is disruptive conduct.
- Fact disputes follow `docs/CONTENT_POLICY.md` section 3. The maintainer's decision closes a dispute; a new source
  re-opens it.
- Moderators may lock threads that have turned into political argument, and will say why.
EOF2
```

- [ ] **Step 5: Amend `CONTRIBUTING.md`:**

1. Replace:

```markdown
Be respectful. Argue about facts with sources, not with people. Political or personal attacks are not tolerated.
```

   with:

```markdown
Be respectful. Argue about facts with sources, not with people. Political or personal attacks are not tolerated. The full [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) applies in every project space, including its section on historical and political discussion. The content rules, and how to dispute a fact, are in [docs/CONTENT_POLICY.md](docs/CONTENT_POLICY.md).
```

2. Replace:

```markdown
- **Reporting a mistake.** Open an issue with the source that shows the correct fact.
```

   with:

```markdown
- **Reporting a mistake.** Use the **Historical accuracy** issue form; it asks for the source that shows the correct fact.
```

- [ ] **Step 6: Run the tests and commit.**

```bash
oss_pytest scripts/oss/tests/test_policies.py
git add CODE_OF_CONDUCT.md docs/CONTENT_POLICY.md CONTRIBUTING.md scripts/oss/tests/test_policies.py
git commit -m "docs(oss): code of conduct with a historical-discussion section, and the content policy

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Pull request template

**Files:**
- Create: `.github/pull_request_template.md`
- Modify: `scripts/oss/tests/test_policies.py` (append)

- [ ] **Step 1: Append the failing test:**

```python
def test_pr_template_has_evidence_and_no_cla_item():
    text = read(".github/pull_request_template.md")
    assert "## Evidence" in text and "LICENSING.md" in text and "ASSETS.yaml" in text
    assert "CLA" not in text and "contributors/" not in text
```

- [ ] **Step 2: Run it to verify it fails.**

- [ ] **Step 3: Write `.github/pull_request_template.md`** (no CLA item: D2):

````markdown
## What this changes

<!-- One or two sentences. Link the issue: Fixes #123 -->

## Evidence

<!-- Paste the commands you ran and the end of their output. Not a description of the output. -->

```
```

## Checklist

- [ ] Tests cover the change, and they fail without it.
- [ ] Test count did not go down, or this description explains why.
- [ ] `CHANGELOG.md` has a line under `## [Unreleased]` if this changes `sim/`, `content/` or `unity/`.
- [ ] Every fact added or changed in `content/` has `sources:` with a checked date (`docs/CONTENT_POLICY.md`).
- [ ] No quote is attributed to a real person unless it is sourced verbatim.
- [ ] Every new image, video, font, audio or map-data file has an `ASSETS.yaml` row.
- [ ] If `unity/` changed: Unity edit-mode tests ran locally, and their result is pasted above (CI does not run them).
- [ ] Nothing in the diff contains a credential, a Unity Cloud id or personal data.
- [ ] AI-assisted parts are disclosed above, and I can defend every line (`CONTRIBUTING.md`).
- [ ] I contribute this under the repository's licences for the paths it changes (`LICENSING.md`).
````

- [ ] **Step 4: Run the tests and commit.**

```bash
git add .github/pull_request_template.md scripts/oss/tests/test_policies.py
git commit -m "docs(github): pull request template with an evidence section

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: README amendments, README.he.md and BUILDING.md

**Files:**
- Create: `README.he.md`, `BUILDING.md`, `scripts/oss/tests/test_readme.py`
- Modify: `README.md` (amendments below; the published images and names stay)

**Interfaces:**
- Consumes: the published images `docs/images/hero-concept-1936.jpg`, `build-m3-map.jpg`, `build-m3-cabinet.jpg`, `build-m3-why.jpg` (no new screenshots here: review I4); the issue forms (Task 4); `SECURITY.md`, `CODE_OF_CONDUCT.md`, `docs/CONTENT_POLICY.md`, `CHANGELOG.md`.
- Produces: README headings `## Build from source` and `## Community and credits`; `BUILDING.md`, which CONTRIBUTING and README link to.

- [ ] **Step 1: Write the failing test.** `scripts/oss/tests/test_readme.py`:

```python
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
HERO = "docs/images/hero-concept-1936.jpg"
GRID = ["docs/images/build-m3-map.jpg", "docs/images/build-m3-cabinet.jpg", "docs/images/build-m3-why.jpg"]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def images(text: str) -> list[str]:
    return [a or b for a, b in re.findall(r'<img src="([^"]+)"|!\[[^\]]*\]\(([^)]+)\)', text)
            if not (a or b).startswith("https://img.shields.io")]


def section(text: str, heading: str) -> str:
    return text.split(heading, 1)[1].split("\n## ", 1)[0]


def test_language_bar_comes_first():
    first = read("README.md").lstrip().splitlines()[0]
    assert "English" in first and "README.he.md" in first


def test_hero_is_the_first_image_captioned_as_concept_art_in_alt_and_caption():
    text = read("README.md")
    assert images(text)[0] == HERO
    tag = re.search(rf'<img src="{re.escape(HERO)}"[^>]*>', text, re.S).group(0)
    assert "AI-generated concept art" in tag and "not the current build" in tag
    after = text.split(tag, 1)[1][:600]
    assert "Concept art (AI-generated)" in after and "set in 1936" in after and "not the current build" in after


def test_real_dated_screenshots_in_a_one_plus_two_grid():
    text = read("README.md")
    assert images(text)[1:4] == GRID
    for path, width in zip(GRID, ("820", "405", "405")):
        assert re.search(rf'<img src="{re.escape(path)}"[^>]*width="{width}"', text), path
    assert re.search(r"Real screenshots from build \d+\.\d+\.\d+ \(M3\), \d{4}-\d{2}-\d{2}\.", text)


def test_note_says_independent_and_planned_commercial_before_what_is_this():
    text = read("README.md")
    note = text.index("> [!NOTE]")
    assert note < text.index("## What is this?")
    block = text[note:note + 600]
    assert "not affiliated with or endorsed by" in block and "any person depicted" in block
    assert "commercial edition is planned" in block and "non-commercial" not in text


def test_link_bar_routes_errors_to_the_form():
    text = read("README.md")
    assert "template=historical_accuracy.yml" in text.split("## What is this?", 1)[0]


def test_history_section_is_accurate():
    hist = section(read("README.md"), "## History, sources and accuracy")
    assert "historical_accuracy.yml" in hist and "docs/CONTENT_POLICY.md" in hist and "not neutral" in hist
    assert "Wikipedia" in hist and "Historian review is planned for M4" in hist
    assert "DRAFT" not in read("README.md")  # review I3: no such stamp exists


def test_play_is_run_locally_with_hosted_build_planned_for_m4():
    play = section(read("README.md"), "## Play")
    assert "planned for M4" in play and "BUILDING.md" in play and "TODO" not in read("README.md")


def test_build_section_pins_unity_and_points_to_building():
    build = section(read("README.md"), "## Build from source")
    assert "6000.3.25f1" in build and "unity/ProjectSettings/ProjectVersion.txt" in build and "BUILDING.md" in build


def test_ai_use_uses_itch_categories():
    ai = section(read("README.md"), "## AI use")
    for cat in ("Code", "Graphics", "Text", "Sound"):
        assert f"**{cat}:**" in ai, cat


def test_no_counts_in_building_prose():
    assert "five commands" not in read("BUILDING.md") and "five commands" not in read("README.md")


def test_every_local_link_and_image_exists():
    for rel in ("README.md", "README.he.md", "BUILDING.md", "CONTRIBUTING.md"):
        text = read(rel)
        targets = re.findall(r'\]\(([^)#\s]+)', text) + re.findall(r'(?:src|href)="([^"#]+)"', text)
        local = [t for t in targets if not t.startswith(("http://", "https://", "mailto:"))]
        missing = [t for t in local if not (ROOT / t).exists()]
        assert not missing, (rel, missing)


def test_hebrew_readme_is_rtl_and_names_english_as_authoritative():
    he = read("README.he.md")
    assert he.lstrip().splitlines()[0].startswith("[English](README.md)")
    assert he.count('<div dir="rtl">') >= 2
    assert he.count("<div") == he.count("</div>")
    assert "הנוסח האנגלי" in he and images(he)[:4] == [HERO, *GRID]


def test_images_are_web_sized_and_the_original_stays():
    for p in (ROOT / "docs/images").glob("*.jpg"):
        assert p.stat().st_size < 1_000_000, p.name
    assert (ROOT / "promo/final/03-gameplay-1936.png").is_file()


def test_building_names_contributing_headings_that_exist():
    building, contributing = read("BUILDING.md"), read("CONTRIBUTING.md")
    for heading in ("Rules every change follows", "What CI runs"):
        assert f'"{heading}"' in building and f"## {heading}" in contributing, heading
```

- [ ] **Step 2: Run it to verify it fails** (no language bar; no `README.he.md`).

- [ ] **Step 3: Amend `README.md`.** Apply these edits in order (each old text matches exactly once in `602183f`):

1. Replace:

```markdown
<p align="center">
  <img src="docs/images/hero-concept-1936.jpg"
```

   with:

```markdown
**English** · [עברית](README.he.md)

<p align="center">
  <img src="docs/images/hero-concept-1936.jpg"
```

2. Replace:

```markdown
  <em>Concept art (AI-generated): the look we are building toward. It is not the current build, and it shows things the game does not have.
```

   with:

```markdown
  <em>Concept art (AI-generated): the look we are building toward, set in 1936, before the game's 1948 start. It is not the current build, and it shows things the game does not have.
```

3. Replace:

```markdown
**A Civilization-style strategy game about governing Israel.
```

   with:

```markdown
**A turn-based grand-strategy game about governing Israel.
```

4. Replace:

```markdown
> [!NOTE]
> This is an independent open-source project.
```

   with:

```markdown
> [!NOTE]
> This is an independent open-source project. A commercial edition is planned.
```

5. Replace:

```markdown
## What is this?
```

   with:

```markdown
##### [Report a bug](https://github.com/eyalgolan/statecraft-israel/issues/new?template=bug_report.yml) | [Report a historical error](https://github.com/eyalgolan/statecraft-israel/issues/new?template=historical_accuracy.yml) | [Discussions](https://github.com/eyalgolan/statecraft-israel/discussions) | [Changelog](CHANGELOG.md)

## What is this?
```

6. Delete:

```markdown
<!-- Screenshots of the current build (M3), taken on 2026-10-10. -->
```

7. Replace:

```markdown
  <img src="docs/images/build-m3-why.jpg" alt="Current build: the Why panel explaining a turn's changes" width="405">
</p>
```

   with:

```markdown
  <img src="docs/images/build-m3-why.jpg" alt="Current build: the Why panel explaining a turn's changes" width="405">
  <br><em>Real screenshots from build 0.1.0 (M3), 2026-10-10.</em>
</p>
```

8. Replace:

```markdown
4. Run `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.
```

   with:

```markdown
4. Run `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.

The exact commands are in [BUILDING.md](BUILDING.md).
```

9. Replace:

```markdown
- Every event, minister, date and number in `content/` carries `sources:` with URLs and check dates.
```

   with:

```markdown
- Every event, minister, date and number in `content/` carries `sources:` with URLs and check dates. Wikipedia (English and Hebrew) is a primary cited source for many facts, which is why `content/` is CC BY-SA 4.0.
- The model behind the game, and its framing, embed assumptions. It is not neutral, and this page says so.
```

10. Replace:

```markdown
- Events about war, terror and the Holocaust are tagged sensitive. They get a sensitivity review before any build is shared.
- Found an error? Open an issue with a source. A claim without a source can't be acted on.
```

   with:

```markdown
- Events about war, terror and the Holocaust are tagged sensitive. They get a sensitivity review before any build is shared, and the build tools refuse to package until it is recorded. Historian review is planned for M4.
- Found an error? Use the [historical error form](.github/ISSUE_TEMPLATE/historical_accuracy.yml). It asks for a source, because a claim without one can't be acted on. The full rules: [docs/CONTENT_POLICY.md](docs/CONTENT_POLICY.md).
```

11. Replace:

```markdown
- Run the tests: `dotnet test sim/Statecraft.sln`, the tools tests, and Unity EditMode.
```

   with:

```markdown
- Run the tests listed in [CONTRIBUTING.md](CONTRIBUTING.md).
```

12. Replace:

```markdown
## AI use
```

   with:

```markdown
## Build from source

You need Unity **6000.3.25f1 (6.3 LTS)** with Web Build Support; the exact version is in [`unity/ProjectSettings/ProjectVersion.txt`](unity/ProjectSettings/ProjectVersion.txt). Clone, run `tools/build_sim_for_unity.sh`, add `unity/` in Unity Hub and open `Assets/Map.unity`. The batch build, the tests and the browser check are in [BUILDING.md](BUILDING.md).

## AI use
```

13. Replace:

```markdown
## Credits

Created by Eyal Golan. Source credits live with each fact in `content/`, and the portrait credits are in `content/people.yaml`.
```

   with:

```markdown
## Community and credits

Questions and ideas go in [Discussions](https://github.com/eyalgolan/statecraft-israel/discussions); bugs and historical errors go in the [issue tracker](https://github.com/eyalgolan/statecraft-israel/issues). Report security problems privately ([SECURITY.md](SECURITY.md)). [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) applies everywhere.

Created by Eyal Golan. Source credits live with each fact in `content/`, and the portrait credits are in `content/people.yaml`.
```

- [ ] **Step 4: Write `BUILDING.md`** (no counts in prose: review M11; licence activation and Windows/Linux paths: review C-4):

````markdown
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

Unity Hub → **Add** → select `unity/`, then open `Assets/Map.unity` and press Play. Run `tools/build_sim_for_unity.sh`
again after every change under `sim/`.

## Tests

The test commands, and why Unity tests are not in CI, are in [`CONTRIBUTING.md`](CONTRIBUTING.md) ("Rules every change follows", "What CI runs"). The browser check of a build is `tools/web_check.py` ([`docs/m3-release.md`](docs/m3-release.md)).

## Repository layout

- `sim/`: the simulation, a pure C# library (netstandard2.1) with xUnit tests. No Unity dependency.
- `content/`: the game's data (YAML and JSON): people, offices, parties, events, map data. Every fact carries `sources:`.
- `unity/`: the Unity project, which presents the sim. No game rules live here.
- `tools/`: Python data tools (map and rainfall pipelines, browser check), run with `uv`.
- `scripts/oss/`: repository policy checks.
````

- [ ] **Step 5: Write `README.he.md`.** Shorter than the English page, which is authoritative; Hebrew text sits in `<div dir="rtl">` blocks, images and links outside them.

```markdown
[English](README.md) · **עברית**

<div align="center">

<img src="docs/images/hero-concept-1936.jpg" width="820" alt="AI-generated concept art of the target look, set in 1936. This is not the current build.">

# Statecraft: Israel

</div>

<div dir="rtl">

*אמנות קונספט שנוצרה בבינה מלאכותית: המראה שאליו אנחנו שואפים, בשנת 1936, לפני נקודת הפתיחה של המשחק ב־1948. זו אינה הגרסה הנוכחית, והיא מראה דברים שאין במשחק. צילומי מסך מהגרסה הנוכחית למטה.*

**משחק אסטרטגיה מבוסס תורות על ניהול מדינת ישראל. אתם ראש הממשלה ב־15 במאי 1948. השרים אמיתיים, המפה אמיתית, ולכל עובדה היסטורית יש מקור.**

הנוסח האנגלי ([README.md](README.md)) הוא הנוסח המחייב. אם יש הבדל, האנגלי קובע. תיקונים לעמוד הזה יתקבלו בברכה.

</div>

<div align="center">

<img src="docs/images/build-m3-map.jpg" width="820" alt="Current build 0.1.0 (M3): the terrain map with the top bar and an event card.">
<br>
<img src="docs/images/build-m3-cabinet.jpg" width="405" alt="Current build 0.1.0 (M3): the Cabinet screen.">
<img src="docs/images/build-m3-why.jpg" width="405" alt="Current build 0.1.0 (M3): the Why panel.">

</div>

<div dir="rtl">

> [!NOTE]
> Statecraft: Israel הוא פרויקט קוד פתוח עצמאי. אין לו קשר ל־Firaxis, ל־2K, למדינת ישראל או לאף אדם המופיע בו, והם אינם תומכים בו. המשחק עוסק במלחמה, בטרור ובשואה כאירועים היסטוריים, ומספר את ההיסטוריה מנקודת מבט ישראלית ([למה, ומה זה אומר](docs/CONTENT_POLICY.md)).

## מה זה?

אתם יושבים בכיסא ראש הממשלה מיום הקמת המדינה. בכל תור אתם ממנים ומחליפים שרים אמיתיים בתוך קואליציה אמיתית, מחלקים תקציב זעום, מנהלים את הביטחון בסולם הסלמה, ומכריעים בהחלטות אמיתיות במועדן האמיתי. המודל שמאחורי המשחק הוא מערכת של הנחות, והוא לא ניטרלי.

## מה המצב?

***אב־טיפוס מוקדם: אבן הדרך M3 הושלמה, והשנים 1948 עד 1951 ניתנות למשחק בבנייה מקוד המקור.*** אין עדיין גרסה מתארחת בדפדפן; היא מתוכננת לאבן הדרך M4. הוראות ההרצה המקומית (באנגלית): [BUILDING.md](BUILDING.md).

## היסטוריה ומקורות

לכל אירוע, שר, תאריך ומספר יש מקור ותאריך בדיקה. ויקיפדיה (באנגלית ובעברית) היא מקור מצוטט מרכזי לעובדות רבות. אף ציטוט לא מיוחס לאדם אמיתי אלא אם הוא מתועד מילה במילה. מצאתם טעות? פתחו דיווח עם מקור בטופס [טעות היסטורית](.github/ISSUE_TEMPLATE/historical_accuracy.yml). הכללים המלאים: [docs/CONTENT_POLICY.md](docs/CONTENT_POLICY.md).

## השתתפות

מתכנתים, היסטוריונים, אמנים וכותבי תרחישים: התחילו ב־[CONTRIBUTING.md](CONTRIBUTING.md) (באנגלית). שימוש בבינה מלאכותית מותר, בתנאי שהוא מוצהר ושכל טענה היסטורית נבדקה מול מקור.

## רישיון

קוד: MIT. תוכן המשחק (`content/`): CC BY-SA 4.0. מסמכים ואמנות מקוריים: CC BY 4.0. תמונות שנוצרו בבינה מלאכותית: CC0, ללא טענת זכויות. הפירוט: [LICENSING.md](LICENSING.md).

</div>
```

- [ ] **Step 6: Run the tests.** Run: `oss_pytest`. Expected: all pass (72 in the scratch run). The edits above link only to files that exist after Tasks 1–6; Task 10 adds the links to `NOTICE`, `THIRD-PARTY-NOTICES.md` and `ASSETS.yaml` together with those files. `test_building_names_contributing_headings_that_exist` pins BUILDING's references to CONTRIBUTING's real headings (review N5).

- [ ] **Step 7: Verify the Hebrew page renders right-to-left (untested on GitHub; ruling 6).**

```bash
CHROME="$(ls -d "$HOME"/Library/Caches/ms-playwright/chromium-*/chrome-mac*/"Google Chrome for Testing.app"/Contents/MacOS/"Google Chrome for Testing" | tail -1)"
gh api markdown -f mode=gfm -f context=eyalgolan/statecraft-israel -f text="$(cat README.he.md)" > /tmp/he-body.html
grep -c 'dir="rtl"' /tmp/he-body.html        # 2 or more: GitHub keeps the dir attribute
grep -c 'markdown-alert' /tmp/he-body.html   # 1 or more: the [!NOTE] callout rendered inside the RTL block
printf '<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.1/github-markdown.min.css"><body class="markdown-body" style="max-width:980px;margin:auto">%s</body>' "$(cat /tmp/he-body.html)" > /tmp/he.html
"$CHROME" --headless=new --window-size=1000,1800 --screenshot=/tmp/he.png "file:///tmp/he.html"
```

Open `/tmp/he.png`: Hebrew paragraphs right-aligned, headings on the right, mixed Hebrew/English lines (the project name, "M3", "CC BY-SA 4.0", file links) in the right order, the NOTE callout right-to-left, images centred. If `dir="rtl"` is stripped (count 0), replace each `<div dir="rtl">` block with the same text with every paragraph prefixed by U+200F RIGHT-TO-LEFT MARK, and re-run. The owner checks the live page in Task 16.

- [ ] **Step 8: Commit.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add README.md README.he.md BUILDING.md scripts/oss/tests/test_readme.py
git commit -m "docs(readme): language bar, Hebrew README, BUILDING.md, dated screenshot caption, accurate history section

Hebrew RTL through GitHub's renderer: <what Step 7 showed, e.g. 'dir kept, 2 RTL blocks, callout rendered'>.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Social preview from a real build image

**Files:**
- Create: `docs/images/social-preview.jpg`

- [ ] **Step 1: Crop the published map screenshot to GitHub's recommended 1280×640** (a real build image, not concept art; README report risk note):

```bash
sips --resampleHeight 640 docs/images/build-m3-map.jpg --out /tmp/sp.jpg
sips --cropToHeightWidth 640 1280 /tmp/sp.jpg --out docs/images/social-preview.jpg
sips -g pixelWidth -g pixelHeight docs/images/social-preview.jpg   # 1280 x 640
ls -l docs/images/social-preview.jpg                              # under 1 MB (scratch: 141,915 bytes)
```

- [ ] **Step 2: Commit.**

```bash
git add docs/images/social-preview.jpg
git commit -m "docs(images): 1280x640 social preview cropped from the real M3 build

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 3: OWNER STEP.** After this branch merges: Settings → General → Social preview → Edit → Upload an image → `docs/images/social-preview.jpg`. There is no API for it.

---

### Task 9: OWNER STEPS: D5, D6, D10, D11, plus D-P and D-H (never run by an agent)

These were published before they were checked (`docs/oss/DECISIONS.md` says so). Record each result by ticking its line in `docs/oss/DECISIONS.md` with the choice.

- [ ] **D11 (do first): sensitivity review of the seven events.** For each of `altalena`, `ten_days`, `operation_yoav`, `operation_hiram`, `operation_horev`, `bernadotte_assassination`, `reparations_claim`, read the event YAML in `content/events/` against the "What to review" column of `docs/m3-release.md` ("OWNER STEP: sensitivity review"). Pay particular attention to `ten_days`, whose omission of the Lydda and Ramle flight and expulsion the notes flag for explicit approval. Record the sign-off **only** by changing that event's `status:` to `sensitivity-reviewed` and committing (this is the one change under `content/` in this plan, and only you make it). Then check:

```bash
dotnet run --project sim/Statecraft.Cli -v q -- --release-gate --content "$PWD/content"; echo exit=$?   # exit=0
```

(On `602183f` it exits 3 and names all seven.)

- [ ] **D5:** open the ElevenLabs Terms of Service and the plan the account had on 2026-10-08/09 (dashboard → Subscription → Billing history), and the output terms of GPT Image 2.5 and Nano Banana Pro as used through ElevenLabs. Confirm public, commercial redistribution in a public repository is allowed. If yes, set `status: verified` on the `promo/final/0[1-8]-*.png`, `promo/ref/*-src.jpg` and `docs/images/hero-concept-1936.jpg` rows of `ASSETS.yaml` (after Task 10) with `checked: <date>, <terms URL>` in the note. If no: **blocker**; decide what happens to the images; agents never drop the hero.

- [ ] **D6:** read the data.gov.il terms of use for dataset 481. If redistribution is allowed, set the `content/map/precip_stations.json` row to `verified`. If not, have an agent remove the file from the tip with the removal procedure (Task 10) and open a sim/tools follow-up to regenerate the hexes without station normals.

- [ ] **D10:** read `docs/oss/history-accepted.tsv` (Task 12) and tick D10: the personal email in `d42bd6b` and the internal plans, specs and research notes are public for good.

- [ ] **D-P and D-H:** after Task 10 generates `ASSETS.portraits.yaml`, open each `owner-verify` portrait's Commons page and confirm its US status; confirm `promo/ref/*-src.jpg` are our own earlier generations and `*-sheet.jpg` contain only the public-domain portraits; look at the hero for any Civilization VII icon, logo or wordmark.

---

### Task 10: Provenance, notices and the LICENSING gaps

**Files:**
- Create: `ASSETS.yaml`, `ASSETS.portraits.yaml` (generated), `scripts/oss/commons_licences.py`, `scripts/oss/tests/test_commons_licences.py`, `scripts/oss/tests/test_licensing.py`, `NOTICE`, `THIRD-PARTY-NOTICES.md`, `LICENSES/YamlDotNet-MIT.txt`, `research_notes/README.md`, `TRADEMARK.md`
- Modify: `LICENSING.md` (table gaps: review A2.3), `CONTRIBUTING.md` (provenance rule), `README.md` (licence and credits links), `scripts/oss/tests/test_policies.py` (append)

**Interfaces:**
- Consumes: `check_repo.check_assets` (Task 3).
- Produces: `ASSETS.yaml` rules `{glob, licence, source, status, note?, allow_empty?, ai_generated?}` (AI files are `licence: CC0-1.0` with `ai_generated: true`: review M5); `commons_licences.rule_for(path, source, licence_short, templates, artist) -> dict` (a non-PD, non-CC0 file is `licence: unknown`, which `--release` refuses: review M3).

- [ ] **Step 1: Write the failing tests.**

`scripts/oss/tests/test_commons_licences.py`:

```python
import commons_licences as cl


def test_public_domain_with_us_status_is_verified():
    r = cl.rule_for("p/a.jpg", "https://commons.wikimedia.org/wiki/File:A.jpg", "Public domain",
                    ["Template:PD-Israel", "Template:PD-US-expired"], "<a href='x'>GPO</a>")
    assert r == {"glob": "p/a.jpg", "licence": "Public-Domain", "source": "https://commons.wikimedia.org/wiki/File:A.jpg",
                 "status": "verified", "commons": "Public domain; PD-Israel, PD-US-expired", "artist": "GPO"}


def test_public_domain_without_us_status_needs_the_owner():
    r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:PD-Israel"], "")
    assert r["status"] == "owner-verify" and "no explicit US status" in r["note"]


def test_cc0_is_verified():
    r = cl.rule_for("p/a.jpg", "s", "CC0", ["Template:Cc-zero"], "")
    assert r["licence"] == "CC0-1.0" and r["status"] == "verified"


def test_share_alike_is_recorded_as_unknown_licence():
    r = cl.rule_for("p/a.jpg", "s", "CC BY-SA 4.0", ["Template:Cc-by-sa-4.0"], "")
    assert r["licence"] == "unknown" and r["status"] == "owner-verify"
    assert "not public domain or CC0" in r["note"]
```

`scripts/oss/tests/test_licensing.py`:

```python
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def test_mit_licence_names_the_copyright_holder():
    text = (ROOT / "LICENSE").read_text(encoding="utf-8")
    assert "MIT License" in text and "Eyal Golan" in text


def test_licence_texts_are_the_real_ones():
    for name, marker in (("CC-BY-4.0", "Attribution 4.0 International"),
                         ("CC-BY-SA-4.0", "Attribution-ShareAlike 4.0 International"),
                         ("CC0-1.0", "CC0 1.0 Universal")):
        assert marker in (ROOT / f"LICENSES/{name}.txt").read_text(encoding="utf-8"), name
    yaml_dot_net = (ROOT / "LICENSES/YamlDotNet-MIT.txt").read_text(encoding="utf-8")
    assert "Antoine Aubry" in yaml_dot_net and "Permission is hereby granted" in yaml_dot_net


def test_licensing_table_covers_every_top_level_area():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    for d in ("sim/", "tools/", "scripts/", ".github/", "unity/Assets/", "content/", "docs/", "reports/",
              "research_notes/", "promo/", "spikes/", "ASSETS.yaml", "unity/Assets/Settings/", "unity/Packages/"):
        assert f"`{d}" in text, d


def test_licensing_puts_unity_template_files_under_the_companion_licence():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    row = next(l for l in text.splitlines() if l.startswith("| Unity template-derived files"))
    assert "Unity Companion License" in row and "spikes/m0/unity/Assets/Settings/" in row and "not MIT" in row


def test_licensing_points_at_decisions_and_assets():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    assert "docs/oss/DECISIONS.md" in text and "ASSETS.yaml" in text and "published before the checks" in text


def test_notice_carries_the_required_attributions():
    text = (ROOT / "NOTICE").read_text(encoding="utf-8")
    assert "produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014" in text
    for s in ("CHELSA V2.1", "Israel Meteorological Service", "SIL Open Font License", "Wikipedia", "YamlDotNet"):
        assert s in text, s
```

Append to `scripts/oss/tests/test_policies.py`:

```python
def test_trademark_qualifies_the_concept_art():
    text = read("TRADEMARK.md")
    assert "prompted in that style" in text and "D-H" in text


def test_contributing_requires_a_provenance_row():
    text = read("CONTRIBUTING.md")
    assert "**Provenance.**" in text and "[ASSETS.yaml](ASSETS.yaml)" in text
```

- [ ] **Step 2: Run them to verify they fail.**

- [ ] **Step 3: Write `scripts/oss/commons_licences.py`** and generate the portrait rows (network: Wikimedia Commons, read-only):

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["requests==2.32.5", "pyyaml==6.0.3"]
# ///
"""Write ASSETS.portraits.yaml: one provenance rule per portrait file, read from Wikimedia Commons (licence short name,
the licence templates on the file page, and the artist). A portrait is 'verified' only when it is public domain or
CC0 AND its page carries an explicit US-status template (PD-Israel alone does not settle US copyright). Anything else
is 'owner-verify'; a licence that is neither public domain nor CC0 is recorded as licence 'unknown', which the release
gate refuses outright.

Usage: uv run scripts/oss/commons_licences.py   (network: commons.wikimedia.org, read-only)"""
from __future__ import annotations

import html
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parents[2]
UA = {"User-Agent": "statecraft-israel-licence-audit/1.0 (https://github.com/eyalgolan/statecraft-israel)"}
US_STATUS = ("PD-US", "PD-1996", "Cc-zero")
# Portrait files with no people.yaml record (promo/CREDITS.md lists Dayan as CC0).
EXTRA = [("promo/ref/people/Moshe_Dayan_1962_CC0.jpg",
          "https://commons.wikimedia.org/wiki/File:Moshe_Dayan_met_mgr._A.C._Ramselaar_(cropped).jpg")]


def rule_for(path: str, source: str, licence_short: str, templates: list[str], artist: str) -> dict:
    names = [t.removeprefix("Template:") for t in templates]
    low = licence_short.lower()
    licence = "CC0-1.0" if low.startswith("cc0") else "Public-Domain" if low.startswith(("public domain", "pd")) \
        else "unknown"
    rule = {"glob": path, "licence": licence, "source": source, "status": "verified",
            "commons": f"{licence_short}; {', '.join(names)}",
            "artist": re.sub(r"<[^>]+>", "", html.unescape(artist)).strip()}
    if licence == "unknown":
        rule.update(status="owner-verify", note=f"Commons licence '{licence_short}' is not public domain or CC0")
    elif not any(n.startswith(US_STATUS) for n in names):
        rule.update(status="owner-verify", note="no explicit US status template on the Commons page")
    return rule


def _commons(title: str, session) -> tuple[str, list[str], str]:
    j = session.get("https://commons.wikimedia.org/w/api.php", timeout=30, params={
        "action": "query", "titles": title, "prop": "imageinfo|templates", "iiprop": "extmetadata",
        "tlnamespace": 10, "tllimit": "max", "format": "json", "redirects": 1}).json()
    page = next(iter(j["query"]["pages"].values()))
    meta = (page.get("imageinfo") or [{}])[0].get("extmetadata", {})
    templates = [t["title"] for t in page.get("templates", [])
                 if re.match(r"Template:(PD-|Cc-zero|Cc-by)", t["title"], re.I)]
    return meta.get("LicenseShortName", {}).get("value", "UNKNOWN"), templates, meta.get("Artist", {}).get("value", "")


def main() -> int:
    import requests
    import yaml
    people = yaml.safe_load((ROOT / "content/people.yaml").read_text(encoding="utf-8"))["people"]
    pairs: list[tuple[str, str]] = []
    for p in people:
        portrait = p.get("portrait")
        if not portrait:
            continue
        pairs.append((portrait["file"], portrait["source"]))
        built = f"unity/Assets/UI/Portraits/{p['id']}.jpg"
        if (ROOT / built).exists():
            pairs.append((built, portrait["source"]))
    pairs += EXTRA
    s = requests.Session()
    s.headers.update(UA)
    rules = []
    for path, source in pairs:
        title = "File:" + unquote(urlparse(source).path.rsplit("File:", 1)[1]).replace("_", " ")
        short, templates, artist = _commons(title, s)
        rules.append(rule_for(path, source, short, templates, artist))
        print(f"{rules[-1]['status']:13} {path}  [{rules[-1]['commons']}]")
    header = ("# GENERATED by scripts/oss/commons_licences.py from content/people.yaml and Wikimedia Commons. Do not edit\n"
              "# by hand: re-run the script. A row is 'verified' only with an explicit US-status template.\n")
    (ROOT / "ASSETS.portraits.yaml").write_text(
        header + yaml.safe_dump({"rules": rules}, sort_keys=False, allow_unicode=True, width=120), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

```bash
uv run scripts/oss/commons_licences.py   # one line per portrait file: verified or owner-verify; writes ASSETS.portraits.yaml
```

Report every `owner-verify` line to the owner (D-P, Task 9).

- [ ] **Step 4: Write `ASSETS.yaml`.**

```yaml
# Provenance of every image, video, font, audio, 3D-model and map-data file in this repository.
# scripts/oss/check_repo.py --only assets fails when such a file (by extension, or any file git treats as binary, or
# a file under content/map/ or StreamingAssets/map/) matches no rule, when a rule matches nothing (unless allow_empty),
# when a rule lacks glob/licence/source/status, or when a licence is not in scripts/oss/oss_policy.py ALLOWED_LICENCES.
# With --release (the release gate) it also fails on any row marked owner-verify or with licence unknown.
# Globs use fnmatch: '*' also matches '/'. Portrait rows live in ASSETS.portraits.yaml (scripts/oss/commons_licences.py).
# ai_generated: true marks AI output (the future Steam/itch.io disclosure is generated from it).
rules:
  # --- AI-generated concept art: CC0 / no rights claimed (LICENSING.md); published 2026-10-10 before D5 was checked ---
  - glob: "promo/final/0[1-8]-*.png"
    licence: CC0-1.0
    ai_generated: true
    source: "ElevenLabs Image API /v1/flows/image (GPT Image 2.5, Nano Banana Pro), prompts in promo/shots.json, generated 2026-10-08/09 by promo/gen.py; text fixes by promo/patch.py"
    status: owner-verify
    note: "Decision D5: the ElevenLabs Terms of Service for the account's plan at generation time, and the model providers' output terms, allow public redistribution. Concept art, not game footage. 04-cabinet-1948 and 08-defense-1955 are AI likenesses of real ministers made from public-domain reference sheets: never presented as photographs (docs/CONTENT_POLICY.md section 4)."
  - glob: "promo/ref/*-src.jpg"
    licence: CC0-1.0
    ai_generated: true
    source: "Earlier promo/gen.py outputs used as edit inputs for the *_fix shots in promo/shots.json (commit d42bd6b)"
    status: owner-verify
    note: "Decision D-P: the owner confirms these are our own earlier generations, not third-party screenshots; D5 applies."
  - glob: "docs/images/hero-concept-1936.jpg"
    licence: CC0-1.0
    ai_generated: true
    source: "Web copy of promo/final/03-gameplay-1936.png (published 2026-10-10); same provenance as promo/final"
    status: owner-verify
    note: "Owner-ruled README hero, captioned as concept art. Blocker if D5 disallows redistribution: the owner rules, an agent never drops it. D-H: no Civilization VII icon, logo or wordmark."
  # --- Reference sheets composed from the public-domain portraits ---
  - glob: "promo/ref/*-sheet.jpg"
    licence: Public-Domain
    source: "Reference sheets composed for the edit model (cabinet 1948, defense 1955) from the public-domain portraits in promo/ref/people/"
    status: owner-verify
    note: "Decision D-P: the owner confirms the sheets contain only the public-domain portraits and our plain layout."
  # --- Our own captures and drawings: CC BY 4.0 ---
  - glob: "promo/final/09-m2-flythrough.mp4"
    licence: CC-BY-4.0
    source: "In-engine Unity capture of the M2 map (MapCapture.Flythrough); terrain derived from Copernicus DEM (NOTICE)"
    status: verified
  - glob: "promo/final/10-m2-topdown.png"
    licence: CC-BY-4.0
    source: "In-engine Unity capture of the M2 map (MapCapture.TopDown); terrain derived from Copernicus DEM (NOTICE)"
    status: verified
  - glob: "promo/ref/geo-reference.png"
    licence: CC-BY-4.0
    source: "Drawn by promo/refmap.py from gazetteer coordinates"
    status: verified
  - glob: "docs/images/build-*.jpg"
    licence: CC-BY-4.0
    source: "Real screenshots of the game's own WebGL build, dated in the README caption; map derived from Copernicus DEM, portraits public domain"
    status: verified
  - glob: "docs/images/social-preview.jpg"
    licence: CC-BY-4.0
    source: "Crop of docs/images/build-m3-map.jpg, 1280x640, for the GitHub social preview (a real build image, not concept art)"
    status: verified
  - glob: "spikes/m0/evidence/*.jpg"
    licence: CC-BY-4.0
    source: "Screenshots of the M0 spike builds"
    status: verified
  # --- Fonts ---
  - glob: "unity/Assets/UI/Fonts/*.ttf"
    licence: OFL-1.1
    source: "https://github.com/octaviopardo/EBGaramond12 @ 106a4a6d377987459ae5e68673a4570f13b957fb (unity/Assets/UI/Fonts/SOURCE.txt)"
    status: verified
  # --- Map data ---
  - glob: "unity/Assets/StreamingAssets/map/heightmap.*"
    licence: Copernicus-DEM
    source: "Copernicus DEM GLO-30 via tools/dem_to_heightmap.py; attribution in NOTICE"
    status: verified
  - glob: "content/map/hexes.json"
    licence: CC-BY-SA-4.0
    source: "tools/hexgrid.py from the heightmap (Copernicus), CHELSA bio12 and IMS station normals; attributions in NOTICE; content/ is CC BY-SA 4.0"
    status: verified
  - glob: "content/map/precip_bio12.*"
    licence: CC0-1.0
    source: "CHELSA V2.1 bio12 1981-2010 via tools/precip_clip.py; citation in NOTICE"
    status: verified
  - glob: "content/map/precip_stations.json"
    licence: IMS-data.gov.il
    source: "Israel Meteorological Service, data.gov.il dataset 481, via tools/ims_normals.py; licence 'Other (Open)' with no licence URL"
    status: owner-verify
    note: "Decision D6 (the M3 plan's deferred 'Legal review of map-data licences' row). Published 2026-10-10 before the check."
  - glob: "content/map/settlement*.yaml"
    licence: CC-BY-SA-4.0
    source: "Coordinates from the Wikipedia API (prop=coordinates) and tools/settlement_hexes.py; per-settlement URLs and check dates in settlements_1948.yaml"
    status: verified
  # --- Reserved for the visual-v1 plan's baked textures (derived from the heightmap) ---
  - glob: "unity/Assets/Textures/*"
    licence: Copernicus-DEM
    source: "tools/terrain_bake.py from the Copernicus-derived heightmap (visual-v1 plan)"
    status: verified
    allow_empty: true
removed: []
```

- [ ] **Step 5: Fetch the YamlDotNet licence verbatim (review M8).** The NuGet package ships no licence file, so take the upstream text at the release tag:

```bash
{ echo "YamlDotNet 18.1.0 (https://github.com/aaubry/YamlDotNet), bundled in builds as YamlDotNet.dll."; echo; \
  curl -fsSL https://raw.githubusercontent.com/aaubry/YamlDotNet/v18.1.0/LICENSE.txt \
  || curl -fsSL https://raw.githubusercontent.com/aaubry/YamlDotNet/v18.1.0/LICENSE; } > LICENSES/YamlDotNet-MIT.txt
grep -c "Antoine Aubry" LICENSES/YamlDotNet-MIT.txt && grep -c "Permission is hereby granted" LICENSES/YamlDotNet-MIT.txt   # 1 and 1
```

If both URLs fail, stop and report; do not hand-type it.

- [ ] **Step 6: Write `NOTICE`, `THIRD-PARTY-NOTICES.md`, `research_notes/README.md` and `TRADEMARK.md`.**

`NOTICE`:

```
Statecraft: Israel
Copyright (c) 2026 Eyal Golan and Statecraft: Israel contributors
https://github.com/eyalgolan/statecraft-israel

Code is MIT (LICENSE). Game content (content/) is CC BY-SA 4.0. Original docs, art and UI assets are CC BY 4.0.
AI-only images are CC0 / no rights claimed. Files derived from Unity's templates are under the Unity Companion License.
See LICENSING.md for the path-by-path map.

This product includes or is derived from the following third-party material. Per-file records: ASSETS.yaml,
ASSETS.portraits.yaml, THIRD-PARTY-NOTICES.md.

Terrain (Copernicus DEM licence, modified data):
produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

Precipitation: CHELSA V2.1 bio12 (1981-2010), CC0 1.0. Karger, D.N., Conrad, O., Böhner, J., Kawohl, T., Kreft, H.,
Soria-Auza, R.W., Zimmermann, N.E., Linder, H.P. & Kessler, M. (2021) Climatologies at high resolution for the earth's
land surface areas. EnviDat. https://doi.org/10.16904/envidat.228.v2.1 ; Karger et al. (2017) Scientific Data 4, 170122.

Rain-gauge data: Israel Meteorological Service, meteorological data archive, data.gov.il dataset 481
(https://data.gov.il/dataset/481), licence "Other (Open)" as stated on the dataset.

Wikipedia (CC BY-SA 4.0): many facts in content/ cite English and Hebrew Wikipedia articles, and the 1948 settlement
coordinates come from the Wikipedia API; each entry lists its source URLs.

Portraits: public-domain and CC0 photographs from Wikimedia Commons; per-file records in ASSETS.portraits.yaml.

Fonts: EB Garamond, SIL Open Font License 1.1 (unity/Assets/UI/Fonts/OFL.txt).

Builds also contain YamlDotNet, Copyright (c) Antoine Aubry and contributors, MIT (LICENSES/YamlDotNet-MIT.txt), and
the Unity runtime, used under the Unity Terms of Service.
```

`THIRD-PARTY-NOTICES.md`:

```markdown
# Third-party notices

Statecraft: Israel. Copyright (c) 2026 Eyal Golan and contributors.

**This file is not a licence.** The licences are in [`LICENSE`](LICENSE), [`LICENSING.md`](LICENSING.md) and
[`LICENSES/`](LICENSES/). This file records attribution and the obligations that come with third-party material, split
by what you are handling. Per-file provenance is in [`ASSETS.yaml`](ASSETS.yaml) and `ASSETS.portraits.yaml`; the
required attribution texts are in [`NOTICE`](NOTICE).

## The source tree (what a clone carries)

| Material | Where | Licence | Obligation |
|---|---|---|---|
| Copernicus DEM GLO-30 (derived heightmap, hex elevations) | `unity/Assets/StreamingAssets/map/`, `content/map/hexes.json` | Copernicus DEM licence | Keep the attribution text in `NOTICE` with the data and in any credits |
| CHELSA V2.1 bio12 | `content/map/precip_bio12.*` | CC0 1.0 | None; the citation in `NOTICE` is requested |
| IMS rain-gauge normals (data.gov.il dataset 481) | `content/map/precip_stations.json` | "Other (Open)" (data.gov.il) | Attribution in `NOTICE`; terms under owner review (decision D6) |
| Wikipedia text and coordinates | cited throughout `content/` | CC BY-SA 4.0 | Attribution (source URL per entry); share-alike, which is why `content/` is CC BY-SA 4.0 |
| Portraits (Wikimedia Commons) | `promo/ref/people/`, `unity/Assets/UI/Portraits/` | Public domain or CC0, per file | None legally; the per-file record in `ASSETS.portraits.yaml` is kept as provenance |
| EB Garamond | `unity/Assets/UI/Fonts/` | SIL Open Font License 1.1 | Keep `OFL.txt` with the fonts; do not sell the fonts on their own |
| Unity template-derived settings | `unity/Assets/Settings/`, `spikes/m0/unity/Assets/Settings/`, `spikes/m0/unity/Assets/UI Toolkit/UnityThemes/` | Unity Companion License | Use with Unity; see `LICENSING.md` |
| AI-generated concept art | `promo/final/01`–`08`, `promo/ref/*-src.jpg`, `docs/images/hero-concept-1936.jpg` | CC0 / no rights claimed (`LICENSING.md`); generator terms under owner review (D5) | Disclose as AI-generated (Steam and itch.io require it) |

Code dependencies are fetched at build or run time under their own licences and are not vendored: NuGet packages in
`sim/**/*.csproj` (YamlDotNet MIT; test-only: xUnit Apache-2.0, Microsoft.NET.Test.Sdk MIT, xunit.runner.visualstudio
Apache-2.0), Unity packages in `unity/Packages/manifest.json` (Unity Companion License or the Unity Terms of Service),
and Python packages declared inline in `tools/*.py` and `scripts/oss/*.py`.

You need your own Unity licence (Unity Personal is free within Unity's eligibility terms) to open or build `unity/`.

## Builds (what a WebGL or desktop build carries)

A build is a different artefact. It contains the Unity runtime, `YamlDotNet.dll`, the fonts, the portraits and the map
data. Before a build is handed to anyone, it must carry `LICENSE`, `NOTICE`, this file, `LICENSES/` and
`unity/Assets/UI/Fonts/OFL.txt`. `scripts/oss/package_web.sh` copies them into the build folder, has
`tools/package_release.py` (the sensitivity gate) zip it, and refuses to finish unless the zip lists every one of them
(see [`docs/RELEASING.md`](docs/RELEASING.md)).

## Third-party trademarks

Names below belong to their owners and are used only to say truthfully what this project uses, runs on or is compared
with. None of them endorses this project, and this repository ships none of their logos.

- Unity is a trademark of Unity Technologies.
- Civilization and Civilization VII are trademarks of Take-Two Interactive Software; they are named as a genre and style
  reference.
- ElevenLabs is a trademark of Eleven Labs, Inc.
- Steam is a trademark of Valve Corporation.
- Wikipedia and Wikimedia Commons are trademarks of the Wikimedia Foundation.
- Copernicus is a programme of the European Union.
- GitHub is a trademark of GitHub, Inc.

The State of Israel's emblem and flag are not used as project marks. Commercial use of the state emblem needs a permit
under Israel's 1949 Flag and Emblem Law (design spec §5.3); any build that shows it must record that permit here.

If you own a mark named here and something in this repository misuses it, open an issue and it will be fixed.
```

`research_notes/README.md`:

```markdown
# Research notes

Working research for Statecraft: Israel: sources, findings and the reasoning behind design and content decisions.
They are kept public so the sourcing behind the game can be checked.

- Our own text here is CC BY 4.0 (see `LICENSING.md`).
- Quotations from books, articles, websites and store pages remain the work of their authors. They are quoted
  briefly, with a link, for research and commentary. This project does not license them to you; if you reuse one,
  check its source's terms.
- Notes are dated and are not kept up to date. The game's content files (`content/`) and their `sources:` fields are
  the authority for any fact the game uses.
- If a quotation here is yours and you want it shortened or removed, open an issue.
```

`TRADEMARK.md`:

```markdown
# Trademark policy

The licences in `LICENSING.md` give you broad rights over the code and the content. They say nothing about the name.
This page says what you may do with the name "Statecraft: Israel" and with the project's visual identity.

The short version: use the name to say accurately what your thing is or works with. Do not use it in a way that
suggests your thing is this project, or is endorsed by it.

## The mark

"Statecraft: Israel" and its logo and wordmark (when there is one) are unregistered marks of Eyal Golan, the maintainer.
No registration is claimed. The maintainer intends to sell the game, for example on Steam, under this name.

## What you may do without asking

- Say what your work is for: "a mod for Statecraft: Israel", "a map tool compatible with Statecraft: Israel".
- Say that you forked it: "a fork of Statecraft: Israel", "based on Statecraft: Israel".
- Write about it, stream it, review it and make videos of it, favourable or not.
- Redistribute unmodified builds under the name, if they really are unmodified.

## What needs permission

- Shipping a modified game, or a game built from this repository's code or content, under this name or a name that
  reads as an official edition ("Statecraft: Israel Gold", "Statecraft: Israel 2").
- Store pages, domains, company names or social accounts that a reader would take as official.
- Using the logo as your own product's identity.
- Saying that you are official, endorsed, certified or partnered.

## Forks

The licences let you fork this project, and this page does not limit that. Give your fork its own name and say in its
README that it began as a fork of Statecraft: Israel.

## Other people's marks

Other companies' names that appear in this repository, and their owners, are listed once, in
`THIRD-PARTY-NOTICES.md` under "Third-party trademarks". They are used only to say truthfully what the project uses or
is compared with. This repository ships none of their logos. The research notes and the image prompts name
*Civilization VII* as a style reference. The game uses no Civilization assets or names. The AI concept art in
`promo/final/` and `docs/images/hero-concept-1936.jpg` was prompted in that style; it is not game art, it carries no
Civilization logo or wordmark (checked by the owner, decision D-H), and it is captioned as concept art wherever it
appears.

## Asking

For anything not covered here, open an issue and ask.
```

- [ ] **Step 7: Amend `LICENSING.md`, `CONTRIBUTING.md` and `README.md`.**

`LICENSING.md`:

1. Replace:

```markdown
| Code | `sim/`, `tools/` (Python), `unity/Assets/Scripts/`, `unity/Assets/Editor/`, `unity/Assets/Tests/`, `promo/*.py`, `spikes/` code | [MIT](LICENSE) |
```

   with:

```markdown
| Code | `sim/`, `tools/` (Python), `scripts/`, `.github/`, `.gitignore`, `unity/Assets/Scripts/`, `unity/Assets/Editor/`, `unity/Assets/Tests/`, `promo/*.py`, `spikes/` code, and everything else under `unity/Assets/` not listed below (scenes such as `Map.unity`, `WebGLTemplates/`, `link.xml`, `*.inputactions`, `.meta` files) | [MIT](LICENSE) |
```

2. Replace:

```markdown
| Original docs, art and UI assets | `docs/`, `reports/` (our own text), `research_notes/` (our own text),
```

   with:

```markdown
| Original docs, art and UI assets | `docs/`, the root `*.md` files (except `LICENSE`), `ASSETS.yaml`, `ASSETS.portraits.yaml`, `promo/shots.json`, `promo/CREDITS.md`, `tools/README.md`, `reports/` (our own text), `research_notes/` (our own text),
```

3. Replace:

```markdown
| Unity template-derived settings | `unity/Assets/Settings/` (URP assets, renderers, volume profiles) | Created from Unity's URP project template and edited; under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) |
```

   with:

```markdown
| Unity template-derived files | `unity/Assets/Settings/` (URP assets, renderers, volume profiles), `spikes/m0/unity/Assets/Settings/`, `spikes/m0/unity/Assets/UI Toolkit/UnityThemes/`, and any Shader Graph generated from a Unity template (for example `unity/Assets/Shaders/StatecraftTerrain.shadergraph`, from "Terrain Simple") | Created from Unity's project or graph templates and edited; under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) (use with Unity), not MIT |
```

4. Replace:

```markdown
| In-engine captures | `promo/final/09`, `10`, `docs/images/build-*.jpg` | CC BY 4.0. They show the Copernicus-derived terrain; see below. |
```

   with:

```markdown
| In-engine captures | `promo/final/09`, `10`, `docs/images/build-*.jpg`, `docs/images/social-preview.jpg`, `spikes/m0/evidence/*.jpg` | CC BY 4.0. They show the Copernicus-derived terrain; see below. |
```

5. Replace:

```markdown
| Unity engine and packages | `unity/Packages/`, `unity/ProjectSettings/` | Configuration only. The Unity engine and its packages are under Unity's own terms and are not included. No Asset Store content is in this repository. |
```

   with:

```markdown
| Unity engine and packages | `unity/Packages/`, `unity/ProjectSettings/` | Configuration only. The Unity engine and its packages are under Unity's own terms and are not included. No Asset Store content is in this repository. |
| In every build | `YamlDotNet.dll` | MIT, Copyright (c) Antoine Aubry and contributors ([LICENSES/YamlDotNet-MIT.txt](LICENSES/YamlDotNet-MIT.txt)) |
```

6. Replace:

```markdown
Still being checked by the owner: the ElevenLabs terms for the AI images, the data.gov.il terms for the rain-gauge data, and the US public-domain status of each portrait. If any check fails, the file is replaced or removed.
```

   with:

```markdown
Still being checked by the owner: the ElevenLabs terms for the AI images, the data.gov.il terms for the rain-gauge data, and the US public-domain status of each portrait (decisions D5, D6 and D-P in [docs/oss/DECISIONS.md](docs/oss/DECISIONS.md); per-file status in [ASSETS.yaml](ASSETS.yaml)). These files were published before the checks. If a check fails, the file is removed from the current tree; git history keeps it. Attribution texts: [NOTICE](NOTICE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
```

`CONTRIBUTING.md`:

1. Replace:

```markdown
6. **Changelog.** If you change `sim/`, `content/` or `unity/` (tests excepted), add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). CI's `Changelog` job checks it; the maintainer can add the `no-changelog` label to a change no player would notice.
```

   with:

```markdown
6. **Changelog.** If you change `sim/`, `content/` or `unity/` (tests excepted), add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). CI's `Changelog` job checks it; the maintainer can add the `no-changelog` label to a change no player would notice.
7. **Provenance.** Every new image, video, font, audio, 3D-model or map-data file gets a row in [ASSETS.yaml](ASSETS.yaml) with its source, licence and status. CI's `Repo policy` job fails on a file with no row.
```

`README.md`:

1. Replace:

```markdown
Details are in [LICENSING.md](LICENSING.md).
```

   with:

```markdown
Details are in [LICENSING.md](LICENSING.md); attributions are in [NOTICE](NOTICE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), and every image's source is in [ASSETS.yaml](ASSETS.yaml).
```

2. Replace:

```markdown
Created by Eyal Golan. Source credits live with each fact in `content/`, and the portrait credits are in `content/people.yaml`.
```

   with:

```markdown
Created by Eyal Golan. Source credits live with each fact in `content/`, the portrait credits are in `content/people.yaml`, and third-party credits are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
```

- [ ] **Step 8: Run the tests and the assets check.**

```bash
oss_pytest
uv run scripts/oss/check_repo.py --only assets          # OK
uv run scripts/oss/check_repo.py --only assets --release  # lists exactly the owner-verify rows (D5, D6, D-P)
```

Any `no-provenance` line is a file the audit missed: add a rule with its real source, or `status: owner-verify` with `note: "source unknown; owner identifies"`. Never invent a source.

- [ ] **Step 9: Commit.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add ASSETS.yaml ASSETS.portraits.yaml NOTICE THIRD-PARTY-NOTICES.md LICENSES/YamlDotNet-MIT.txt research_notes/README.md TRADEMARK.md LICENSING.md CONTRIBUTING.md README.md scripts/oss
git commit -m "docs(oss): per-asset provenance, notices, LICENSING gaps closed, trademark policy

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

**Removal procedure** (when the owner rules a file not redistributable; never for the hero without the owner's ruling): `git rm <path>`; delete or narrow its rule; add `{ path: "<path>", reason: "<owner's reason>", date: <ISO date> }` under `removed:`; add a line to `docs/oss/AUDIT.md` ("removed from the tip on <date>; history keeps it"). `check_repo.py --only assets` then refuses if the file returns.

---

### Task 11: Tip hygiene: the key from the environment, and the rest of `.gitignore`

**Files:**
- Modify: `promo/gen.py` (whole file), `.gitignore` (append)

- [ ] **Step 1: Confirm the published `promo/gen.py` is unchanged.** Run: `git diff 602183f HEAD -- promo/gen.py` (no output) and `grep -c drowning promo/gen.py` (5).

- [ ] **Step 2: Replace `promo/gen.py` with** (the key from `ELEVENLABS_API_KEY` only; no other project named):

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["requests"]
# ///
"""Generate promo concept art via the ElevenLabs Image API (/v1/flows/image).

Usage: ELEVENLABS_API_KEY=... uv run promo/gen.py SHOT_ID [MODEL]   (shot prompts live in promo/shots.json)
The API key is read from the ELEVENLABS_API_KEY environment variable only and is never printed or written. Keep it
in your own key store, never in this repository. Refuses to run if the account's credit pool would drop below FLOOR."""
import base64, json, mimetypes, os, sys, time
from pathlib import Path
import requests

BASE = "https://api.elevenlabs.io"
HERE = Path(__file__).resolve().parent
FLOOR = 50_000          # credits kept in reserve for other work on the same account, never spent here
ESTIMATE = 2_000        # upper bound seen per 2K image on this account (Nano Banana Pro: 1,827)

def session() -> requests.Session:
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    if not key:
        raise SystemExit("ELEVENLABS_API_KEY is not set (export it from your own key store; never commit it)")
    s = requests.Session(); s.headers["xi-api-key"] = key
    return s

def left(s) -> int:
    j = s.get(f"{BASE}/v1/user/subscription", timeout=30).json()
    return int(j["character_limit"]) - int(j["character_count"])

def main(shot_id: str, model: str = "gpt-image-2.5-sunburst"):
    shot = json.loads((HERE / "shots.json").read_text())[shot_id]
    s = session()
    before = left(s)
    if before - ESTIMATE < FLOOR:
        raise SystemExit(f"BLOCKED: {before} left, floor {FLOOR}")
    body = {"model_id": model, "prompt": shot["prompt"], "aspect_ratio": shot.get("aspect", "16:9"),
            "resolution": "2K"}
    if model.startswith("gpt-image"):
        body["quality"] = "high"
    if shot.get("refs"):  # reference images, paths relative to promo/
        body["images"] = [{"type": "inline_base64", "mime_type": mimetypes.guess_type(p)[0] or "image/png",
                           "content_base64": base64.b64encode((HERE / p).read_bytes()).decode()}
                          for p in shot["refs"]]
    r = s.post(f"{BASE}/v1/flows/image", json=body, timeout=120)
    if not r.ok:
        raise SystemExit(f"{r.status_code}: {r.text[:500]}")
    gid = r.json()["id"]
    out = HERE / "out"; out.mkdir(exist_ok=True)
    tag = f"{shot_id}-{model.split('-')[0]}"
    (out / f"{tag}.id").write_text(gid)  # paid: keep the id for recovery
    t0 = time.time()
    while True:
        time.sleep(5)
        try:
            j = s.get(f"{BASE}/v1/flows/image/{gid}", timeout=30).json()
        except (requests.RequestException, ValueError):
            continue
        if j.get("status") == "completed": break
        if j.get("status") == "failed": raise SystemExit(f"{tag} failed: {j}")
        if time.time() - t0 > 900: raise SystemExit(f"{tag} timeout; id saved")
    d = requests.get(j["content_url"], timeout=300); d.raise_for_status()  # signed CDN URL: no key sent
    ext = {"image/jpeg": ".jpg", "image/webp": ".webp"}.get(d.headers.get("content-type", ""), ".png")
    path = out / (tag + ext); path.write_bytes(d.content)
    print(path)  # cost is measured per batch (parallel runs share one counter): see `uv run promo/gen.py balance`

if __name__ == "__main__":
    if sys.argv[1:] == ["balance"]:
        print("left", left(session()))
    else:
        main(*sys.argv[1:])
```

- [ ] **Step 3: Append to `.gitignore`:**

```
.env.*
*.alf
Unity_v*.alf
.claude/settings.local.json
.pytest_cache/
.vs/
.idea/
unity/obj/
unity/MemoryCaptures/
```

- [ ] **Step 4: Verify and commit.**

```bash
grep -c drowning promo/gen.py   # 0
env -u ELEVENLABS_API_KEY uv run promo/gen.py balance; echo "exit=$?"   # "ELEVENLABS_API_KEY is not set …", exit=1, no network call
git check-ignore -v .env.local x.alf .vs/a unity/obj/a   # each names a .gitignore rule
git add promo/gen.py .gitignore
git commit -m "chore(promo): ElevenLabs key from the environment only; ignore editor and licence files

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 12: History record (information only)

**Files:**
- Create: `scripts/oss/scan_history.py`, `scripts/oss/tests/test_scan_history.py`, `docs/oss/history-accepted.tsv`, `docs/oss/AUDIT.md`

**Interfaces:**
- Produces: `scan_history.scan(repo, refs) -> list[(commit10, path | "(message)" | "(identity)", kind)]` (merge commits diffed with `-m`: review I9; author/committer emails scanned); `--ref main` or `--all` (review M2); rows in `docs/oss/history-accepted.tsv` print as `accepted` (review I2), and the summary line ends `N open secret-shape findings`, which the release gate reads.

- [ ] **Step 1: Write the failing test.** `scripts/oss/tests/test_scan_history.py`:

```python
import subprocess

import scan_history
from conftest import commit_all, git

FAKE = "sk_" + "cd" * 24


def new_repo(tmp_path):
    root = tmp_path / "r"
    root.mkdir()
    git(root, "init", "-q", "-b", "main")
    return root


def write(root, rel, text):
    p = root / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")


def test_a_key_added_then_deleted_is_still_reported_by_type(tmp_path, capsys):
    root = new_repo(tmp_path)
    write(root, "promo/k.py", f'K = "{FAKE}"\n')
    c1 = commit_all(root, "add tool")
    write(root, "promo/k.py", "K = None\n")
    commit_all(root, "remove key")
    assert (c1, "promo/k.py", "elevenlabs-key") in scan_history.scan(root, ["--all"])
    assert scan_history.main(["--repo", str(root)]) == 0
    out = capsys.readouterr().out
    assert f"{c1}  promo/k.py  elevenlabs-key  OPEN" in out and FAKE not in out
    assert "1 open secret-shape findings" in out and "rotated" in out


def test_accepted_rows_are_shown_as_accepted(tmp_path, capsys):
    root = new_repo(tmp_path)
    write(root, "promo/k.py", f'K = "{FAKE}"\n')
    c1 = commit_all(root, "add tool")
    write(root, "docs/oss/history-accepted.tsv",
          f"# commit\tpath\tkind\taction\tdate\n{c1}\tpromo/k.py\televenlabs-key\trevoked at provider\t2026-10-11\n")
    commit_all(root, "record rotation")
    scan_history.main(["--repo", str(root)])
    out = capsys.readouterr().out
    assert f"{c1}  promo/k.py  elevenlabs-key  accepted" in out and "0 open secret-shape findings" in out


def test_content_added_in_a_merge_resolution_is_read(tmp_path):
    root = new_repo(tmp_path)
    write(root, "a.txt", "base\n")
    commit_all(root, "base")
    git(root, "checkout", "-q", "-b", "side")
    write(root, "a.txt", "side\n")
    commit_all(root, "side")
    git(root, "checkout", "-q", "main")
    write(root, "a.txt", "main\n")
    commit_all(root, "main")
    r = subprocess.run(["git", "-C", str(root), "merge", "-q", "side"], capture_output=True)
    assert r.returncode != 0  # conflict
    write(root, "a.txt", f'resolved "{FAKE}"\n')
    merge = commit_all(root, "merge side")
    assert (merge, "a.txt", "elevenlabs-key") in scan_history.scan(root, ["main"])


def test_commit_messages_and_identities_are_scanned(tmp_path):
    root = new_repo(tmp_path)
    write(root, "a.txt", "x\n")
    c1 = commit_all(root, "contact someone" + "@" + "gmail.com for access")
    write(root, "b.txt", "y\n")
    git(root, "add", "-A")
    git(root, "-c", "user.name=t", "-c", "user.email=" + "person" + "@" + "corp.com", "commit", "-qm", "b")
    c2 = git(root, "rev-parse", "HEAD").strip()[:10]
    hits = scan_history.scan(root, ["--all"])
    assert (c1, "(message)", "personal-email") in hits and (c2, "(identity)", "personal-email") in hits


def test_ref_and_all_flags_parse(tmp_path, capsys):
    root = new_repo(tmp_path)
    write(root, "a.txt", "fine\n")
    commit_all(root, "feat: fine")
    assert scan_history.main(["--repo", str(root), "--ref", "main"]) == 0
    assert scan_history.main(["--repo", str(root), "--all"]) == 0
    assert "no findings" in capsys.readouterr().out
```

- [ ] **Step 2: Run it to verify it fails** (`No module named 'scan_history'`).

- [ ] **Step 3: Write `scripts/oss/scan_history.py`.**

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Read-only scan of every ADDED line in every commit reachable from the given ref (merge commits included, diffed
against each parent), plus every commit message and author/committer email, for secret shapes, personal emails and
home paths (the patterns in oss_policy.py). Prints commit, file and pattern type, NEVER the matched text.

Information only (owner ruling 2026-10-11: no history rewriting, no history enforcement), so the exit code is always 0.
Rows already handled are listed in docs/oss/history-accepted.tsv (commit, path, kind, action, date) and are shown as
"accepted", so a rotated key or a reviewed false positive never blocks anything twice. A real secret is fixed by
revoking or rotating it at its provider; history is never rewritten.

Usage: uv run scripts/oss/scan_history.py [--repo DIR] [--ref main | --all]   (default --all)"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import oss_policy as P  # noqa: E402

ACCEPTED = "docs/oss/history-accepted.tsv"
SECRET_KINDS = frozenset(P.SECRET_PATTERNS)


def _kinds(line: str) -> list[str]:
    return P.secret_kinds(line) + P.email_kinds(line) + P.home_path_kinds(line)


def scan(repo: Path, refs: list[str]) -> list[tuple[str, str, str]]:
    hits: set[tuple[str, str, str]] = set()
    # -m: merge commits are diffed against each parent, so content added while resolving a conflict is read too.
    # --no-ext-diff/--no-textconv: a repo's own diff drivers must not run while we read it.
    cmd = ["git", "-C", str(repo), "log", *refs, "-m", "-p", "--no-color", "--no-ext-diff", "--no-textconv",
           "--format=COMMIT %H"]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE)
    commit = path = ""
    assert proc.stdout is not None
    for raw in proc.stdout:
        line = raw.decode("utf-8", "replace").rstrip("\n")
        if line.startswith("COMMIT "):
            commit, path = line[7:17], ""
        elif line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else ""
        elif line.startswith("+") and path:
            hits.update((commit, path, k) for k in _kinds(line[1:]))
    proc.wait()
    meta = subprocess.run(["git", "-C", str(repo), "log", *refs, "--format=%H%x00%ae%x00%ce%x00%B%x1e"],
                          capture_output=True, check=True).stdout.decode("utf-8", "replace")
    for rec in meta.split("\x1e"):
        parts = rec.strip("\n").split("\x00", 3)
        if len(parts) < 4:
            continue
        sha, ae, ce, body = parts
        for who in (ae, ce):
            hits.update((sha[:10], "(identity)", k) for k in P.email_kinds(who))
        for line in body.splitlines():
            hits.update((sha[:10], "(message)", k) for k in _kinds(line))
    return sorted(hits)


def accepted(repo: Path) -> set[tuple[str, str, str]]:
    p = repo / ACCEPTED
    rows = set()
    if p.is_file():
        for line in p.read_text(encoding="utf-8").splitlines():
            if line and not line.startswith("#"):
                commit, path, kind = line.split("\t")[:3]
                rows.add((commit[:10], path, kind))
    return rows


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--repo", type=Path, default=Path.cwd())
    ap.add_argument("--ref", default=None)
    ap.add_argument("--all", action="store_true")
    args = ap.parse_args(argv)
    refs = [args.ref] if args.ref and not args.all else ["--all"]
    hits = scan(args.repo, refs)
    done = accepted(args.repo)
    open_secrets = 0
    for commit, path, kind in hits:
        state = "accepted" if (commit, path, kind) in done else "OPEN"
        open_secrets += state == "OPEN" and kind in SECRET_KINDS
        print(f"{commit}  {path}  {kind}  {state}")
    count = len(subprocess.run(["git", "-C", str(args.repo), "rev-list", *refs], capture_output=True, text=True,
                               check=True).stdout.split())
    print(f"scanned {count} commits ({' '.join(refs)}): {len(hits) or 'no'} findings, "
          f"{open_secrets} open secret-shape findings")
    print("NOTE: information only. An OPEN secret-shape row must be revoked or rotated at its provider (owner step), "
          f"then recorded in {ACCEPTED}; history is never rewritten.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 4: Run the tests** (`5 passed`), **then scan `main`:** `uv run scripts/oss/scan_history.py --ref main`. Expected on `602183f`: 8 findings, `0 open secret-shape findings` (the rows listed in Step 5). Any secret-shape row: stop and tell the owner (file, commit, kind); the remedy is rotation (Task 16, O-5).

- [ ] **Step 5: Write `docs/oss/history-accepted.tsv`** (tab-separated; the rows the Step 4 scan printed on `602183f`):

```
# Rows of scripts/oss/scan_history.py that are known and handled. Columns: commit, path, kind, action, date.
# A rotated key stays in history (owner ruling: no rewriting); its row here records the rotation.
203b64688e	unity/Assets/Tests/EditMode/SaveUiTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
4061a8beaa	unity/Assets/Tests/EditMode/GameSceneTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
4061a8beaa	unity/Assets/Tests/EditMode/SaveUiTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
4cc34d6f67	unity/Assets/Tests/EditMode/GameSceneTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
581760d618	spikes/m0/unity/Assets/TutorialInfo/Layout.wlt	home-path	Unity's own template path, not ours	2026-10-11
746207491f	unity/Assets/Tests/EditMode/SaveUiTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
8f90aff336	unity/Assets/Tests/EditMode/SaveUiTests.cs	home-path	fake path in a redaction test fixture	2026-10-11
d42bd6ba6c	promo/fetch_portraits.py	personal-email	owner's address; tip fixed; public in history (decision D10)	2026-10-11
```

- [ ] **Step 6: Write `docs/oss/AUDIT.md`.** Copy sections 1–4 of `~/israel-game/research_notes/open source/israel-game-audit.md`, **omitting every credential-file path** (the two `.env` locations under `$HOME` and the no_human credential file names; review I6), then append the re-run table with today's date, the commit count and `0 open secret-shape findings` from Step 4. `research_notes/open source/` itself stays untracked (it is git-ignored).

- [ ] **Step 7: Commit.**

```bash
grep -c "\.env\b" docs/oss/AUDIT.md   # 0
git add scripts/oss/scan_history.py scripts/oss/tests/test_scan_history.py docs/oss/history-accepted.tsv docs/oss/AUDIT.md
git commit -m "feat(oss): informational history scan (merges included) and the accepted-findings record

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 13: Merge policy as a ruleset (applied only by the owner)

**Files:**
- Create: `scripts/oss/github/ruleset-protect-main.json`, `scripts/oss/github/ruleset-main-review.json`, `scripts/oss/github/repo-settings.json`, `scripts/oss/github/actions-permissions.json`, `scripts/oss/github/actions-selected.json`, `scripts/oss/github/labels.tsv`, `scripts/oss/apply_github_settings.sh`, `scripts/oss/tests/test_merge_policy.py`

**Interfaces:**
- Consumes: the CI job names (Task 3); the live ruleset `protect-main` (read 2026-10-10: `non_fast_forward`, `deletion`, no bypass actors; always looked up by name).
- Produces: two rulesets, both looked up **by name** (review N2, N7). `protect-main` (the live one, kept) blocks force-push and deletion with **no bypass actors**, so nobody, the owner included, can rewrite public history. `main-review` (new) adds code-owner review, squash-only merging and the four required checks, and only it carries the admin bypass of D12. `apply_github_settings.sh OWNER/REPO` PUTs or POSTs both, patches repo settings, and creates labels. No classic branch protection. Fix round 1 (review I5): it also PUTs `actions-permissions.json` (`{"enabled": true, "allowed_actions": "selected", "sha_pinning_required": true}`) and `actions-selected.json` (`{"github_owned_allowed": false, "verified_allowed": false, "patterns_allowed": ["actions/*", "astral-sh/setup-uv@*"]}`), so only those actions run and every `uses:` must be a full SHA (`.github/dependabot.yml` keeps the pins current). Add to `test_merge_policy.py`: a test that `actions-permissions.json` has `sha_pinning_required` true and `allowed_actions` `selected`, and that the allow-list is exactly those two patterns. Add to `apply_github_settings.sh`, before the labels loop: `gh api -X PUT "repos/$repo/actions/permissions" --input "$here/github/actions-permissions.json" >/dev/null` and `gh api -X PUT "repos/$repo/actions/permissions/selected-actions" --input "$here/github/actions-selected.json" >/dev/null`.
- **Docs follow the live settings (fix round 1, review I2).** Until the owner applies `main-review`, CONTRIBUTING.md and `.github/CODEOWNERS` describe only what is live (`protect-main`, private vulnerability reporting). In Step 5, after the owner applies the settings, update both in the same change: CONTRIBUTING "Pull requests" gains "a push after approval needs a fresh approval; PRs are squash-merged and keep you as the author", and the CODEOWNERS comment gains "the `main-review` ruleset requires a code-owner review". A test for each sentence lands with that change, not before.

- [ ] **Step 1: Write the failing test.** `scripts/oss/tests/test_merge_policy.py`:

```python
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
GH = ROOT / "scripts/oss/github"


def load(name: str) -> dict:
    return json.loads((GH / name).read_text())


def rule(ruleset: dict, kind: str) -> dict | None:
    return next((r for r in ruleset["rules"] if r["type"] == kind), None)


def test_squash_only_no_auto_merge_branches_deleted_discussions_on():
    s = load("repo-settings.json")
    assert s["allow_squash_merge"] is True
    assert s["allow_merge_commit"] is False and s["allow_rebase_merge"] is False
    assert s["allow_auto_merge"] is False  # no_human's live setting was true despite its docs
    assert s["delete_branch_on_merge"] is True and s["has_discussions"] is True


def test_history_ruleset_is_the_live_one_and_nobody_bypasses_it():
    # Review N2: force-push and deletion stay blocked for everyone, the owner included (owner ruling 1).
    r = load("ruleset-protect-main.json")
    assert r["name"] == "protect-main" and r["enforcement"] == "active"
    assert r["conditions"]["ref_name"]["include"] == ["~DEFAULT_BRANCH"]
    assert {x["type"] for x in r["rules"]} == {"non_fast_forward", "deletion"}
    assert r["bypass_actors"] == []


def test_review_ruleset_requires_code_owner_review_squash_and_checks():
    r = load("ruleset-main-review.json")
    assert r["name"] == "main-review" and r["enforcement"] == "active"
    assert r["conditions"]["ref_name"]["include"] == ["~DEFAULT_BRANCH"]
    assert {x["type"] for x in r["rules"]} == {"pull_request", "required_status_checks"}
    pr = rule(r, "pull_request")["parameters"]
    assert pr["require_code_owner_review"] and pr["dismiss_stale_reviews_on_push"] and pr["require_last_push_approval"]
    assert pr["required_approving_review_count"] == 1 and pr["required_review_thread_resolution"]
    assert pr["allowed_merge_methods"] == ["squash"]
    assert rule(r, "required_status_checks")["parameters"]["strict_required_status_checks_policy"] is True


def test_no_history_enforcement():
    # Owner ruling 2026-10-11: no signed-commit or linear-history requirement.
    for name in ("ruleset-protect-main.json", "ruleset-main-review.json"):
        r = load(name)
        assert rule(r, "required_signatures") is None and rule(r, "required_linear_history") is None


def test_only_the_review_ruleset_has_the_admin_bypass():
    # Decision D12: the maintainer keeps merging locally and pushing; everyone else goes through review and checks.
    assert load("ruleset-main-review.json")["bypass_actors"] == [
        {"actor_id": 5, "actor_type": "RepositoryRole", "bypass_mode": "always"}]


def test_apply_script_looks_rulesets_up_by_name():
    text = (ROOT / "scripts/oss/apply_github_settings.sh").read_text()
    assert "put_ruleset protect-main" in text and "put_ruleset main-review" in text
    assert "24834491" not in text


def test_no_classic_branch_protection_file():
    assert not (GH / "branch-protection.json").exists()
    assert "branches/main/protection\" --input" not in (ROOT / "scripts/oss/apply_github_settings.sh").read_text()


def test_labels_include_the_ones_ci_forms_and_readme_use():
    names = {l.split("\t")[0] for l in (GH / "labels.tsv").read_text().splitlines() if l}
    assert {"no-changelog", "historical-accuracy", "bug", "enhancement", "spam", "good first issue"} <= names


def test_apply_script_refuses_without_a_terminal_and_calls_nothing(tmp_path):
    marker = tmp_path / "gh-called"
    fake = tmp_path / "gh"
    fake.write_text(f"#!/bin/sh\ntouch {marker}\n")
    fake.chmod(0o755)
    env = dict(os.environ, PATH=f"{tmp_path}:{os.environ['PATH']}")
    r = subprocess.run(["bash", str(ROOT / "scripts/oss/apply_github_settings.sh"), "eyalgolan/statecraft-israel"],
                       stdin=subprocess.DEVNULL, capture_output=True, text=True, env=env)
    assert r.returncode == 2 and "interactive terminal" in r.stdout
    assert not marker.exists()
```

- [ ] **Step 2: Run it to verify it fails.**

- [ ] **Step 3: Write the settings.**

`scripts/oss/github/ruleset-protect-main.json` (the live ruleset, unchanged in substance; no bypass):

```json
{
  "name": "protect-main",
  "target": "branch",
  "enforcement": "active",
  "conditions": { "ref_name": { "include": ["~DEFAULT_BRANCH"], "exclude": [] } },
  "bypass_actors": [],
  "rules": [
    { "type": "non_fast_forward" },
    { "type": "deletion" }
  ]
}
```

`scripts/oss/github/ruleset-main-review.json` (bypass per D12's recommendation; actor 5 is the repository admin role):

```json
{
  "name": "main-review",
  "target": "branch",
  "enforcement": "active",
  "conditions": { "ref_name": { "include": ["~DEFAULT_BRANCH"], "exclude": [] } },
  "bypass_actors": [
    { "actor_id": 5, "actor_type": "RepositoryRole", "bypass_mode": "always" }
  ],
  "rules": [
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 1,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true,
        "require_last_push_approval": true,
        "required_review_thread_resolution": true,
        "allowed_merge_methods": ["squash"]
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          { "context": "Sim (dotnet test)" },
          { "context": "Tools (pytest)" },
          { "context": "Repo policy" },
          { "context": "Changelog" }
        ]
      }
    }
  ]
}
```

`scripts/oss/github/repo-settings.json` (the live repo still allows merge and rebase commits and has Discussions off; this fixes both):

```json
{
  "has_issues": true,
  "has_wiki": false,
  "has_projects": false,
  "has_discussions": true,
  "allow_squash_merge": true,
  "allow_merge_commit": false,
  "allow_rebase_merge": false,
  "allow_auto_merge": false,
  "allow_update_branch": true,
  "delete_branch_on_merge": true,
  "squash_merge_commit_title": "PR_TITLE",
  "squash_merge_commit_message": "PR_BODY",
  "web_commit_signoff_required": false,
  "security_and_analysis": {
    "secret_scanning": { "status": "enabled" },
    "secret_scanning_push_protection": { "status": "enabled" }
  }
}
```

`scripts/oss/github/labels.tsv` (tab-separated name, colour, description):

```
no-changelog	cfd3d7	No player-visible change; the Changelog check passes without an entry
historical-accuracy	1d76db	A fact or its source is disputed (docs/CONTENT_POLICY.md)
content	0e8a16	Game content under content/
bug	d73a4a	Something behaves differently from what it should
enhancement	a2eeef	An idea or feature request
spam	000000	Closed without review (CONTRIBUTING.md, AI-assisted contributions)
good first issue	7057ff	A small, well-described task for a first contribution
```

`scripts/oss/apply_github_settings.sh`:

```bash
#!/usr/bin/env bash
# OWNER ONLY. Applies scripts/oss/github/* to the GitHub repository: repo settings (squash only, no auto-merge, secret
# scanning, Discussions on), two rulesets (protect-main: force-push and deletion blocked, no bypass; main-review:
# required review and checks, admin bypass; no classic branch protection), private vulnerability reporting, Dependabot
# alerts, a read-only default workflow token, approval for outside contributors' workflow runs, and the labels.
# Agents never run this. It refuses without an interactive terminal and asks you to type the repository name.
# Usage: bash scripts/oss/apply_github_settings.sh OWNER/REPO
set -euo pipefail
repo="${1:?usage: apply_github_settings.sh OWNER/REPO}"
here="$(cd "$(dirname "$0")" && pwd)"

if [ ! -t 0 ]; then
  echo "refusing: this owner step needs an interactive terminal; nothing was applied"
  exit 2
fi
read -r -p "Type the repository name ($repo) to apply the settings: " typed
if [ "$typed" != "$repo" ]; then
  echo "names differ; nothing was applied"
  exit 2
fi

gh api -X PATCH "repos/$repo" --input "$here/github/repo-settings.json" >/dev/null
echo "repo settings applied"
# Two rulesets, looked up by name. protect-main (the live one, kept) blocks force-push and deletion with NO bypass:
# nobody, the owner included, can rewrite public history (owner ruling 1). main-review adds review and required checks,
# and only it carries the admin bypass (decision D12).
put_ruleset() {
  local name="$1" file="$2" id
  id="$(gh api "repos/$repo/rulesets" --jq ".[] | select(.name==\"$name\") | .id")"
  if [ -n "$id" ]; then
    gh api -X PUT "repos/$repo/rulesets/$id" --input "$file" >/dev/null
    echo "ruleset $name ($id) updated"
  else
    gh api -X POST "repos/$repo/rulesets" --input "$file" >/dev/null
    echo "ruleset $name created"
  fi
}
put_ruleset protect-main "$here/github/ruleset-protect-main.json"
put_ruleset main-review "$here/github/ruleset-main-review.json"
gh api -X PUT "repos/$repo/private-vulnerability-reporting" >/dev/null
echo "private vulnerability reporting on"
gh api -X PUT "repos/$repo/vulnerability-alerts" >/dev/null
echo "Dependabot alerts on"
gh api -X PUT "repos/$repo/actions/permissions/workflow" -f default_workflow_permissions=read \
  -F can_approve_pull_request_reviews=false >/dev/null
echo "default workflow token read-only"
if gh api -X PUT "repos/$repo/actions/permissions/fork-pr-contributor-approval" \
     -f approval_policy=all_external_contributors >/dev/null 2>&1; then
  echo "outside contributors' workflow runs need approval"
else
  echo "WARN: set it by hand: Settings > Actions > General > 'Require approval for all external contributors'"
fi
while IFS=$'\t' read -r name color desc; do
  [ -n "$name" ] && gh label create "$name" --repo "$repo" --color "$color" --description "$desc" --force >/dev/null
done < "$here/github/labels.tsv"
echo "labels created"

echo "--- verify ---"
gh api "repos/$repo" --jq '{allow_auto_merge, allow_merge_commit, allow_rebase_merge, allow_squash_merge, delete_branch_on_merge, has_discussions, visibility}'
for name in protect-main main-review; do
  id="$(gh api "repos/$repo/rulesets" --jq ".[] | select(.name==\"$name\") | .id")"
  gh api "repos/$repo/rulesets/$id" --jq '{name, enforcement, bypass: [.bypass_actors[] | .bypass_mode], rules: [.rules[].type], checks: [.rules[] | select(.type=="required_status_checks") | .parameters.required_status_checks[].context]}'
done
gh api "repos/$repo/branches/main/protection" >/dev/null 2>&1 && echo "WARN: classic branch protection exists too; remove it (rulesets only)" || echo "no classic branch protection (rulesets only)"
```

```bash
chmod +x scripts/oss/apply_github_settings.sh
```

- [ ] **Step 4: Run the tests** (`oss_pytest`; `test_job_names_are_the_required_checks` now also checks the ruleset) **and commit.**

```bash
git add scripts/oss/github scripts/oss/apply_github_settings.sh scripts/oss/tests/test_merge_policy.py
git commit -m "chore(oss): merge policy as the protect-main ruleset: squash only, code-owner review, required checks

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 5: OWNER STEP, after this branch merges and CI has run green once on `main`** (the required checks must exist first): `bash scripts/oss/apply_github_settings.sh eyalgolan/statecraft-israel`, type the name when asked. Expected verify output: `allow_auto_merge: false`, `allow_merge_commit: false`, `allow_rebase_merge: false`, `allow_squash_merge: true`, `delete_branch_on_merge: true`, `has_discussions: true`; `protect-main`: `enforcement: active`, rules `non_fast_forward, deletion`, `bypass: []`; `main-review`: `enforcement: active`, rules `pull_request, required_status_checks`, the four checks, `bypass: ["always"]`; and "no classic branch protection".

---

### Task 14: Release gate, notices inside the artefact, and the full CI policy check

**Files:**
- Create: `scripts/oss/package_web.sh`, `scripts/oss/release_gate.sh`, `docs/RELEASING.md`, `scripts/oss/tests/test_release.py`
- Modify: `docs/m3-release.md` (one line), `.github/workflows/ci.yml` (full policy check)

**Interfaces:**
- Consumes: `tools/package_release.py` (M3's sensitivity gate and zipper; unchanged); `Statecraft.Cli --release-gate`; `check_repo.py --release`; `scan_history.py`'s summary line.
- Produces: `package_web.sh BUILD_DIR ZIP` (copies the notices in **before** the gate zips: review I5); `release_gate.sh` (prints `RELEASE GATE OK` or `RELEASE GATE FAILED`).

- [ ] **Step 1: Write the failing test.** `scripts/oss/tests/test_release.py`:

```python
import os
import re
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
NOTICES = ["LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt",
           "LICENSES/CC0-1.0.txt", "LICENSES/YamlDotNet-MIT.txt", "OFL.txt"]
# Stands in for tools/package_release.py (which needs dotnet and real content): zips the folder, index.html at the root.
STUB = """import argparse, zipfile
from pathlib import Path
a = argparse.ArgumentParser(); a.add_argument("--build"); a.add_argument("--zip"); a = a.parse_args()
b = Path(a.build)
with zipfile.ZipFile(a.zip, "w") as z:
    for f in sorted(p for p in b.rglob("*") if p.is_file()):
        z.write(f, f.relative_to(b).as_posix())
print("PACKAGED", a.zip)
"""


def run(tmp_path, build, out, stub=STUB):
    s = tmp_path / "stub.py"
    s.write_text(stub)
    return subprocess.run(["bash", str(ROOT / "scripts/oss/package_web.sh"), str(build), str(out)], cwd=ROOT,
                          capture_output=True, text=True, env=dict(os.environ, PACKAGE_RELEASE=str(s)))


def fake_build(tmp_path):
    build = tmp_path / "Web-brotli"
    (build / "Build").mkdir(parents=True)
    (build / "index.html").write_text("<html></html>")
    (build / "Build" / "x.data.br").write_bytes(b"\x00")
    return build


def test_the_gate_zip_carries_every_notice(tmp_path):
    out = tmp_path / "game.zip"
    r = run(tmp_path, fake_build(tmp_path), out)
    assert r.returncode == 0, r.stdout + r.stderr
    names = set(zipfile.ZipFile(out).namelist())
    assert "index.html" in names and "Build/x.data.br" in names
    for n in NOTICES:
        assert n in names, n


def test_a_refusing_gate_stops_the_package(tmp_path):
    out = tmp_path / "game.zip"
    r = run(tmp_path, fake_build(tmp_path), out, stub="import sys\nprint('PACKAGE REFUSED: review')\nsys.exit(1)\n")
    assert r.returncode != 0 and "PACKAGE REFUSED" in r.stdout and not out.exists()


def test_package_refuses_a_missing_build(tmp_path):
    r = run(tmp_path, tmp_path / "nope", tmp_path / "z.zip")
    assert r.returncode != 0 and not (tmp_path / "z.zip").exists()


def test_versions_agree():
    sim = re.search(r'Version = "([^"]+)"', (ROOT / "sim/Statecraft.Sim/SimInfo.cs").read_text()).group(1)
    unity = re.search(r"bundleVersion: (\S+)", (ROOT / "unity/ProjectSettings/ProjectSettings.asset").read_text()).group(1)
    assert sim == unity
    released = re.findall(r"^## \[(\d+\.\d+\.\d+)\]", (ROOT / "CHANGELOG.md").read_text(), re.M)
    if released:
        assert released[0] == sim
```

- [ ] **Step 2: Run it to verify it fails.**

- [ ] **Step 3: Write the scripts.**

`scripts/oss/package_web.sh`:

```bash
#!/usr/bin/env bash
# Put the licence and attribution files INSIDE a WebGL build, have M3's sensitivity gate zip it
# (tools/package_release.py: it refuses until every sensitive event is sensitivity-reviewed), then prove the notices
# are in the zip by listing it. no_human shipped a bundle whose notices file pointed at a script that never copied
# them; this script checks the artefact itself.
# Usage: bash scripts/oss/package_web.sh unity/Builds/Web-brotli unity/Builds/statecraft-israel-vX.Y.Z-web.zip
# PACKAGE_RELEASE overrides the zipping script (tests only).
set -euo pipefail
build="${1:?usage: package_web.sh BUILD_DIR ZIP_PATH}"
zip_path="${2:?usage: package_web.sh BUILD_DIR ZIP_PATH}"
root="$(cd "$(dirname "$0")/../.." && pwd)"
packager="${PACKAGE_RELEASE:-$root/tools/package_release.py}"
[ -f "$build/index.html" ] || { echo "not a WebGL build (no index.html): $build"; exit 1; }
case "$zip_path" in /*) ;; *) zip_path="$PWD/$zip_path" ;; esac

mkdir -p "$build/LICENSES"
cp "$root/LICENSE" "$root/NOTICE" "$root/THIRD-PARTY-NOTICES.md" "$build/"
cp "$root/LICENSES/"*.txt "$build/LICENSES/"
cp "$root/unity/Assets/UI/Fonts/OFL.txt" "$build/OFL.txt"

python3 "$packager" --build "$build" --zip "$zip_path"

listing="$(unzip -Z1 "$zip_path")"
for f in LICENSE NOTICE THIRD-PARTY-NOTICES.md OFL.txt LICENSES/CC-BY-4.0.txt LICENSES/CC-BY-SA-4.0.txt \
         LICENSES/CC0-1.0.txt LICENSES/YamlDotNet-MIT.txt index.html; do
  printf '%s\n' "$listing" | grep -qxF "$f" || { echo "MISSING in zip: $f"; rm -f "$zip_path"; exit 1; }
done
shasum -a 256 "$zip_path"
echo "OK: notices inside $zip_path"
```

`scripts/oss/release_gate.sh`:

```bash
#!/usr/bin/env bash
# The release gate: run on main before every release tag (docs/RELEASING.md). Read-only: it changes nothing locally or
# on GitHub. Exit 0 means the tip of main may be released; anything else names what is open.
set -uo pipefail
cd "$(dirname "$0")/../.."
fail=0
step() { echo; echo "== $1"; }

step "on main, clean tree"
[ "$(git rev-parse --abbrev-ref HEAD)" = "main" ] || { echo "not on main"; fail=1; }
[ -z "$(git status --porcelain --untracked-files=no)" ] || { echo "tracked changes present"; fail=1; }

step "tip policy, release mode (open decisions D1-D12, owner-verify and unknown-licence rows refuse)"
uv run scripts/oss/check_repo.py --release || fail=1

step "policy tests"
uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q || fail=1

step "simulation and content tests"
dotnet test sim/Statecraft.sln -c Release --nologo -v q || fail=1

step "sensitivity gate on content/ (decision D11; Statecraft.Cli --release-gate)"
dotnet run --project sim/Statecraft.Cli -v q -- --release-gate --content "$PWD/content" || fail=1

step "tools tests"
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q || fail=1

step "history scan of main (information; rows in docs/oss/history-accepted.tsv are accepted)"
uv run scripts/oss/scan_history.py --ref main | tee /tmp/oss-release-scan.txt
if ! grep -q ' 0 open secret-shape findings' /tmp/oss-release-scan.txt; then
  echo "OPEN SECRET-SHAPE FINDING: rotate it at its provider (owner), then add the row to docs/oss/history-accepted.tsv"
  fail=1
fi

echo
if [ "$fail" -eq 0 ]; then echo "RELEASE GATE OK"; else echo "RELEASE GATE FAILED"; fi
exit "$fail"
```

```bash
chmod +x scripts/oss/package_web.sh scripts/oss/release_gate.sh
```

- [ ] **Step 4: Write `docs/RELEASING.md`** (also the screenshot refresh, a gating step, and the planned hero swap):

````markdown
# Releasing

A release is a tagged commit on `main` with a CHANGELOG section and, from the first shared build on, a WebGL zip on the
GitHub Releases page (and, from M4, on itch.io). Steps 1–4 are a normal pull request. Steps 5–7 are owner steps.

1. **Choose the version** (Semantic Versioning, pre-1.0 while the game is pre-release).
2. **Bump every version site in one pull request:** `sim/Statecraft.Sim/SimInfo.cs` (`Version`) and
   `unity/ProjectSettings/ProjectSettings.asset` (`bundleVersion`). Then move the `## [Unreleased]` entries in
   `CHANGELOG.md` under `## [X.Y.Z] - YYYY-MM-DD` and leave an empty `## [Unreleased]` above it.
   `scripts/oss/tests/test_release.py::test_versions_agree` fails if the three disagree.
3. **Run the release gate** on the merged `main`: `bash scripts/oss/release_gate.sh`. It must print
   `RELEASE GATE OK`. It refuses while an owner decision is open (including D10, history acknowledgement, and D11, the
   sensitivity review), while an asset row is `owner-verify`, while the sim's sensitivity gate refuses `content/`, or
   while the history scan shows an open secret-shape row.
4. **Merge** the pull request (squash).
5. **Build and package** on the owner's machine, from the merged `main`:

   ```bash
   "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -shareable -logFile "$PWD/unity-build-web-br.log"
   git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
   uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5" --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952" --expect "SMOKE OK 5 turns"
   bash scripts/oss/package_web.sh unity/Builds/Web-brotli unity/Builds/statecraft-israel-vX.Y.Z-web.zip
   ```

   `package_web.sh` copies the licence and attribution files into the build folder, then runs
   `tools/package_release.py`, M3's sensitivity gate, which zips the folder and prints `PACKAGED …` or
   `PACKAGE REFUSED …`. It then lists the zip, and prints the SHA-256 and `OK: notices inside …`. If it prints anything
   else, stop. This one zip is the file for GitHub Releases and for the M4 itch.io upload (`docs/m3-release.md`); never
   zip a build by hand.
6. **Tag and publish (owner):**

   ```bash
   git tag -a vX.Y.Z -m "Statecraft: Israel vX.Y.Z" && git push origin vX.Y.Z
   gh release create vX.Y.Z unity/Builds/statecraft-israel-vX.Y.Z-web.zip --title "vX.Y.Z" --notes-file <(awk '/^## \[X.Y.Z\]/{f=1;next} /^## \[/{f=0} f' CHANGELOG.md)
   ```

   Paste the SHA-256 from step 5 into the release notes.
7. **Check every published artefact:** download each uploaded zip (GitHub Releases, and itch.io once used) into an empty
   folder, then run `unzip -Z1 <zip> | grep -E '^(LICENSE|NOTICE|THIRD-PARTY-NOTICES.md|OFL.txt)$'`. It must print four
   lines for each zip.

## Refreshing the README screenshots

The README's screenshots are claims about the current build. Re-take them whenever the build looks different, and
**always in the pull request that merges a visual change** (visual-v1 included), never as a follow-up:

1. Build the web player (`docs/dev-setup.md`) and serve it. After visual-v1 has merged, capture with its
   `uv run tools/web_check.py <build> --shots …` under its machine lock, so the capture does not disturb profiling.
   Before that, capture by hand at 1640×920 in Chrome.
2. Replace `docs/images/build-m3-map.jpg`, `build-m3-cabinet.jpg` and `build-m3-why.jpg` (or add `build-<milestone>-*`
   and update the README to the new names). Each must show a non-sensitive moment: no unreviewed sensitive event card.
3. Update the visible caption "Real screenshots from build X.Y.Z (Mn), YYYY-MM-DD." in `README.md`, and the
   "Is this any good?" and "Roadmap" text the change made stale.
4. Re-crop `docs/images/social-preview.jpg` (1280×640) from the new map shot. The owner re-uploads it under Settings →
   General → Social preview. Never use concept art for the social preview.

## Replacing the concept-art hero (planned)

The hero at the top of the README is AI concept art of the target look (owner ruling, 2026-10-11). When a build reaches
that look (the owner judges this at a visual-v1 or later art checkpoint, side by side with
`promo/final/03-gameplay-1936.png`), a real screenshot takes its place:

1. Capture the build at the promo pose, 2000 px wide, as `docs/images/hero-build.jpg`. Keep
   `docs/images/hero-concept-1936.jpg`.
2. In `README.md` and `README.he.md`, point the hero `<img>` at `hero-build.jpg` with alt text describing the real
   build, and move the concept art down to a "Where it's going" line under the screenshots, still captioned as concept
   art.
3. In `ASSETS.yaml`, add a `docs/images/hero-build.jpg` row (`licence: CC-BY-4.0`, `status: verified`, source: the build
   version and date).
4. In `scripts/oss/tests/test_readme.py`, change `HERO` to `docs/images/hero-build.jpg`, and keep the assertion that
   any concept-art image carries the concept-art caption.
5. Add a CHANGELOG line, and land it as a normal pull request.
````

- [ ] **Step 5: Point the M4 itch.io step at the packaging script.** In `docs/m3-release.md`:

1. Replace:

```markdown
3. Under Uploads, upload `unity/Builds/statecraft-m3-web.zip` (made by `python3 tools/package_release.py`) and tick **This file will be played in the browser**.
```

   with:

```markdown
3. Under Uploads, upload the zip made by `bash scripts/oss/package_web.sh unity/Builds/Web-brotli unity/Builds/statecraft-m3-web.zip` (it puts the licence and attribution files inside, then runs `tools/package_release.py`; see `docs/RELEASING.md`) and tick **This file will be played in the browser**.
```

- [ ] **Step 6: Switch CI's `Repo policy` to the full check.** In `.github/workflows/ci.yml`, replace `run: uv run scripts/oss/check_repo.py --only secrets,emails,home-paths,sizes,symlinks` with `run: uv run scripts/oss/check_repo.py`.

- [ ] **Step 7: Run everything.**

```bash
oss_pytest                                   # all pass (98 in the scratch run)
uv run scripts/oss/check_repo.py              # OK (every required file now exists)
uv run scripts/oss/check_repo.py --release    # lists only the owner items: unticked decisions and owner-verify rows
```

- [ ] **Step 8: Commit, then hand off.**

```bash
git diff --stat 602183f...HEAD -- sim content unity tools   # prints nothing
git add scripts/oss docs/RELEASING.md docs/m3-release.md .github/workflows/ci.yml
git commit -m "build(oss): release gate, notices inside the shipped zip, full policy check in CI

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Use superpowers:finishing-a-development-branch to merge `oss` into `main`.

---

### Task 15: After visual-v1 merges (gating, in the same PR or immediately after)

- [ ] **Step 1: Linux CI parity on the merged `main`** (review I11): re-run Task 3 Step 10's docker commands (with the full `check_repo.py`). visual-v1's tools tests (`machine_lock`, `art_compare`, `boot_report`, `terrain_*`) run on Linux here first. Fix or report; never deselect a test that is not genuinely platform-bound.
- [ ] **Step 2: Refresh the README screenshots** with `docs/RELEASING.md` "Refreshing the README screenshots": capture with visual-v1's `web_check.py --shots` under its machine lock, show no unreviewed sensitive event card, update the dated caption, the "Is this any good?" and "Roadmap" text, `README.he.md`'s images, and re-crop `docs/images/social-preview.jpg` (owner re-uploads).
- [ ] **Step 3: Add a CHANGELOG `[Unreleased]` line for the visual pass** (visual-v1 does not write one).
- [ ] **Step 4:** `oss_pytest` and `uv run scripts/oss/check_repo.py` pass; commit (`docs(readme): screenshots and status after visual v1`).

---

### Task 16: OWNER STEPS remaining (never run by an agent)

O-8 (create private, push) and O-10 (flip public) are done and dropped.

- [ ] **O-1: Decisions.** Tick D1, D3, D4, D7, D8, D9, D12 in `docs/oss/DECISIONS.md` (D5, D6, D10, D11, D-P, D-H are Task 9).
- [ ] **O-5: Rotation, only if a scan ever shows an open secret-shape row.** Revoke the key at its provider (ElevenLabs: dashboard → Profile → API Keys; GitHub: Settings → Developer settings → Tokens), put the new key only in your own key store, and add the row to `docs/oss/history-accepted.tsv` with "revoked at provider" and the date. History is not rewritten.
- [ ] **O-9: Look at the live pages.** `https://github.com/eyalgolan/statecraft-israel/blob/main/README.he.md` renders right-to-left (paragraphs and headings on the right, mixed lines in order, NOTE callout rendered); if not, have an agent apply Task 7 Step 7's fallback. `README.md` shows the language bar, the hero caption under the hero, and the dated grid. The Actions tab shows the four CI jobs green on `main`.
- [ ] **O-10a: Social preview** (Task 8, Step 3).
- [ ] **O-10b: Apply the merge policy** (Task 13, Step 5).
- [ ] **O-11: Verify.**

```bash
gh api repos/eyalgolan/statecraft-israel --jq '{visibility, allow_auto_merge, allow_merge_commit, allow_rebase_merge, has_discussions, security_and_analysis}'
for name in protect-main main-review; do
  id="$(gh api repos/eyalgolan/statecraft-israel/rulesets --jq ".[] | select(.name==\"$name\") | .id")"
  gh api "repos/eyalgolan/statecraft-israel/rulesets/$id" --jq '{name, enforcement, rules: [.rules[].type], bypass: [.bypass_actors[].bypass_mode]}'
done   # protect-main: bypass []; main-review: bypass ["always"]
gh api repos/eyalgolan/statecraft-israel/private-vulnerability-reporting --jq .enabled   # true
rm -rf /tmp/sc-verify && git clone https://github.com/eyalgolan/statecraft-israel.git /tmp/sc-verify && (cd /tmp/sc-verify && uv run scripts/oss/check_repo.py)
```

- [ ] **O-12: Test the contributor path once.** From a second account or a collaborator, open a trivial PR from a fork that edits `docs/CONTENT_POLICY.md`. Expect: the workflow waits for your approval (outside contributor); `Changelog` passes (docs only); the merge button needs your approval. Close it unmerged.
- [ ] **Before every release:** `bash scripts/oss/release_gate.sh` prints `RELEASE GATE OK` (`docs/RELEASING.md`).

---

## Self-review

- **Coverage of the amendment brief:** obsolete steps dropped (O-8, O-10); rulesets only (Task 13); release gate (Task 14); C1 (Task 3: letters-only TLD, allowlist, proven on the published tree); C2 (Task 3: dotnet in the tools job and in the Linux parity run); I2–I9, I11 (see table below); I1, I10 obsolete (D2); D10, D11 (Task 1, Task 9, release gate); `research_notes/open source/` ignored and AUDIT redacted (Task 12); `promo/gen.py` (Task 11); language bar, Hebrew README with RTL check, BUILDING.md as amendments (Task 7); screenshot refresh gating (Task 15); asset extensions (Task 3 `BINARY_EXTENSIONS` plus git-binary detection); the urgent order (Tasks 2–9).
- **Placeholders:** the only angle-bracket values are runtime inputs (`<PR#>`, `vX.Y.Z`, the Task 7 commit-message result, owner-supplied decisions).
- **Type consistency:** `Finding`, `CHECKS`, rule keys, job names (`ci.yml` = ruleset contexts = `EXPECTED_JOBS` = CONTRIBUTING) are pinned by tests.
- **Review Focus:** each line names its test and owning task.
- **Per-task green (N3):** checked by applying the plan task by task to a fresh clone of `602183f`; every stage's tests and CI policy check pass (counts in "Base").

## Review round 1 response

| Item | Response |
|---|---|
| A1 published state | Plan rebased on `602183f`; published files are amended in place (README, CONTRIBUTING, LICENSING, .gitignore); published image names kept; Tasks 3/4 of the old plan (UA email, LICENSE, LICENSES) dropped as done. |
| A1 O-8 / O-10 | Dropped. |
| A1 branch protection | Rulesets only: `protect-main` extended from JSON by `apply_github_settings.sh` (Task 13); the old `branch-protection.json` is gone and a test forbids classic protection. |
| A1 `prepublish.sh` | Now `scripts/oss/release_gate.sh`, run before every release tag (Task 14); the "only main" step removed. |
| A2.1 "non-commercial" | Already fixed in `4950a4e`; Task 7 adds "A commercial edition is planned" to the NOTE and a test forbids "non-commercial". |
| A2.2 Wikipedia paraphrase | Resolved by `4950a4e` (`content/` CC BY-SA 4.0); NOTICE, THIRD-PARTY and CONTENT_POLICY now say Wikipedia is a primary cited source. |
| A2.3 LICENSING gaps | Task 10 edits: `unity/Assets/` catch-all (MIT), template-derived files (spikes, UnityThemes, generated Shader Graphs) under the Unity Companion License, `scripts/`, `.github/`, root docs, `ASSETS*.yaml`, `promo/shots.json`, `tools/README.md`, spike evidence, YamlDotNet. |
| A2.4 checks after the fact | `DECISIONS.md` states it; D5, D6, D-P are Task 9, before the remaining polish. |
| A2.5 Civilization | Pitch changed to "turn-based grand-strategy"; D-H owner look; TRADEMARK qualified for the concept art. |
| A2.6 date in a comment | Visible dated caption, test-pinned (Task 7). |
| A2.7 no test commands | CONTRIBUTING gets the exact commands (Task 3); README points there. |
| A3 urgent order | Followed the coordinator's latest order: SECURITY, CI, issue forms, COC+CONTENT_POLICY, PR template, README/Hebrew/BUILDING, social preview, owner D5/D6/D10/D11. |
| C1 | Letters-only TLD rule, `allow.yaml` with stale-row detection, tests for package ids and allowlisted fixtures; proven `OK` on `602183f` (Task 3 Step 9). |
| C2 | `actions/setup-dotnet` in the tools job; Linux parity runs in `mcr.microsoft.com/dotnet/sdk:10.0`; a test forbids `--deselect` in CI. |
| I1 CLA opener | **Obsolete**: D2 is inbound = outbound; no CLA job exists (test `test_no_cla_or_dco_machinery`). |
| I2 permanent block | `docs/oss/history-accepted.tsv`; accepted rows print `accepted`; the gate fails only on open secret-shape rows (test). |
| I3 claimed mechanisms | CONTENT_POLICY and README state only what M3 does (sensitivity gate; historian review planned for M4; Wikipedia a primary source); tests forbid "DRAFT". |
| I4 screenshots | No Unity build in this plan; published screenshots kept; new ones only with visual-v1's `--shots` under its lock, non-sensitive (Task 15, RELEASING). |
| I5 notices in the itch zip | `package_web.sh` copies notices before `tools/package_release.py` zips; `docs/m3-release.md` points at it; RELEASING checks every uploaded zip. |
| I6 history acknowledgement | D10; AUDIT copy omits credential-file paths; `research_notes/open source/` stays ignored. |
| I7 sensitivity sign-off | D11 (owner, Task 9 first item); the release gate runs `Statecraft.Cli --release-gate` on `content/` (exit 3 today, naming all seven). |
| I8 `.github` bootstrapping | Stated in `UNTRUSTED_PR_REVIEW.md`; `persist-credentials: false` on every checkout; `changelog_check --head` uses the PR head SHA (tests). |
| I9 merges | `git log -m -p`; a test with a conflict-resolution merge; identities scanned too. |
| I10 CLA vs share-alike | **Obsolete** (no CLA). |
| I11 Linux parity after visual-v1 | Task 15 Step 1, plus the CHANGELOG line. |
| M1 | One-sentence Goal; the test command defined in Task 1 (now the `oss_pytest` function, N4); test renamed `test_unity_cloud_ids_empty_pass`. |
| M2 | `--ref main` or `--all` flag; tested. |
| M3 | Missing keys are `bad-rule`; non-PD portraits are `licence: unknown`, refused by `--release`. |
| M4 | Extensions extended (svg, psd, tga, tif, exr, hdr, bmp, fbx, obj, blend, glb, gltf, woff, woff2, otf, wav, ogg, mp3, flac, aiff, m4a, pdf, unitypackage) plus git-binary detection; fnmatch crossing `/` documented. |
| M5 | AI files `licence: CC0-1.0` + `ai_generated: true`. |
| M6 | Noted; `gh api markdown` works on the public repo. |
| M7 | `-buildTarget OSXUniversal` in the EditMode command. |
| M8 | YamlDotNet licence fetched verbatim from the upstream tag; stop if unavailable. |
| M9 | Apply script works on the public repo, verifies the ruleset, warns if classic protection exists. |
| M10 | ASSETS note and CONTENT_POLICY §4 on AI likenesses. |
| M11 | Counts removed from BUILDING/README; tested. |
| C-1..C-6 | Covered by the rows above (form, scripts, licences, README, gaps, parallel work). |

## Review round 2 response (re-review 1)

| Item | Response |
|---|---|
| N1 `read()` undefined | Task 1 Step 2 now writes the test file's header with `ROOT` and `read()` (the assembler had cut the header at the first function, which was `read`). |
| N2 bypass reaches force-push | Two rulesets, both looked up by name: `protect-main` (live, kept) with `non_fast_forward` and `deletion` and **no bypass actors**; `main-review` with review and checks and the D12 admin bypass. Tests `test_history_ruleset_is_the_live_one_and_nobody_bypasses_it`, `test_only_the_review_ruleset_has_the_admin_bypass`, `test_apply_script_looks_rulesets_up_by_name`; apply script, Task 13 Step 5 and O-11 updated. |
| N3 red between tasks | Every task's tests pass at its end (Global Constraints). The `ASSETS.yaml` assertion moved from Task 3's CONTRIBUTING test to Task 10 (`test_contributing_requires_a_provenance_row`); Task 7's link test passes because Task 7 links only existing files and Task 10 adds the notice links with the notice files. Verified stage by stage on a fresh clone of `602183f`: 1, 5, 54, 55, 57, 58, 72, 72, 84, 84, 89, 98, 102 tests, all green; CI's policy check `OK` at every stage. |
| N4 zsh word-splitting | `oss_pytest` shell function replaces `$OSS_PYTEST` everywhere. |
| N5 BUILDING heading | BUILDING names "Rules every change follows" and "What CI runs", both real CONTRIBUTING headings; pinned by `test_building_names_contributing_headings_that_exist`. |
| N6 CLA ledger / CODEOWNERS | UNTRUSTED_PR_REVIEW no longer names a `CLA ledger` job; CODEOWNERS says "the `main-review` ruleset requires a code-owner review". |
| N7 hard-coded ruleset id | O-11 and the apply script look ids up by name; a test forbids the literal id in the script. |
| N8 network-only steps | Unchanged and safe: Task 10 stops if the YamlDotNet fetch fails; a failed Commons lookup leaves `owner-verify` rows the release gate refuses. |
| §3 merge-order rule | Recorded in "Sequencing with active work", item 5. |
