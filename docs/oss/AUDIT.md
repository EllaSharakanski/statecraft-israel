# Statecraft: Israel: open-source risk audit (read-only)

This is the public copy of the audit. It omits the paths of credential files. Findings are by file, commit and pattern type only, never by value.

Audit date: 2026-10-10. Scope: `~/israel-game` `main` at `ce5b836` and `.worktrees/m3` (branch `m3` at `8978566`, 120 commits ahead of `main`), plus every other local branch: `m3-events`, `m3-fix`, `m3-sim`, `m3-ui`, `m3-web`. Nothing was modified. The repository has **no remote**: `git remote -v` printed nothing.

Owner rulings that frame this audit: no history rewriting and no history enforcement. A secret found in history is fixed by rotation (an owner step). Findings are reported by file, commit and pattern type only, never by value.

## 1. Secrets across the full history of every branch

**Tool.** gitleaks, trufflehog and detect-secrets are not installed. A purpose-written scanner (`scratchpad/scan.py`, not committed) read `git log --all -p` (223 commits, about 5.3 MB of patch text) and matched every added line. It printed only the pattern type, the file and the commit. The patterns:
- ElevenLabs `sk_<hex>`, Anthropic `sk-ant-`, OpenAI `sk-`/`sk-proj-`, AWS `AKIA`/`ASIA`, GitHub `ghp_`/`gho_`/`github_pat_`, Google `AIza`, Slack `xox?-`, PEM private-key headers, JWTs;
- a literal `xi-api-key` value, and generic `api_key|secret|token|password = "<16+ chars>"`;
- Unity `cloudProjectId: <guid>`, a non-empty `organizationId:`, `projectId <guid>`, and `UNITY_SERIAL`/`UNITY_PASSWORD`/`UNITY_LICENSE` assignments;
- email addresses, absolute home paths, and references to environment files.

**Results**
| Pattern | Hits |
|---|---|
| Any API key or token pattern | **0** |
| Private keys or JWTs | **0** |
| Unity `cloudProjectId` with a GUID / non-empty `organizationId` | **0** in every version of every `ProjectSettings.asset`, including `spikes/m0/unity`. At the tip, `unity/ProjectSettings/ProjectSettings.asset:933-938` has `cloudProjectId:` empty, `projectName:` empty, `organizationId:` empty and `cloudEnabled: 0`. |
| Absolute home path under `/Users/` (the owner's username) | **0** |
| Environment-file reference | 1: `promo/gen.py`, commit `a64011ae7`. It read the ElevenLabs key at runtime from a file in another project's folder and never printed it. The key itself was never committed. Fixed at the tip: the script now reads `ELEVENLABS_API_KEY` from the environment only. |
| Personal email | 1: the owner's own `@gmail.com` address in an HTTP `User-Agent` string in `promo/fetch_portraits.py`, commit `d42bd6ba6`. It is still present at the tip. |
| Other `@` matches | 10 false positives (`@pytest.mark.parametrize`) in `tools/tests/*` and the M0–M2 plan |

**Commit identities.** All 223 commits have author and committer `eyalgolan <5146175+eyalgolan@users.noreply.github.com>`, so there is no personal email in the metadata. Many commits carry a `Co-Authored-By: Claude …` trailer, which is fine.

**Media metadata.**
- `promo/final/*.png` carry `caBX` (C2PA content credentials, the AI provenance) and, in two files, IPTC/XMP blocks.
- `10-m2-topdown.png` has an `eXIf` block.
- A `strings` match in `09-m2-flythrough.mp4` was binary noise, not a path.

**Verdict:** no secret to rotate. The owner's gmail address is in history (the no-rewrite ruling makes that permanent once published) and should be replaced at the tip. The ElevenLabs key lives outside the repo.

## 2. Licences and provenance, by asset class (tip of `m3`)

| Class | Files | Provenance and licence | Risk |
|---|---|---|---|
| Code | `sim/**`, `tools/**`, `unity/Assets/Scripts/**`, `unity/Assets/Editor/**`, `unity/Assets/Tests/**`, `unity/Assets/Shaders/**`, `promo/*.py`, the WebGL template `unity/Assets/WebGLTemplates/Statecraft/index.html` | Written in this repo (owner plus AI agents). The WebGL template is our own HTML using Unity's `{{{ }}}` placeholders. | None. Licence of choice. |
| Content YAML | `content/*.yaml`, `content/events/*.yaml` | Facts with `sources:` URLs and check dates. The prose is ours and paraphrases sources such as en.wikipedia. Wikipedia text is CC BY-SA 4.0, so close paraphrase favours a compatible content licence. | Low. |
| Map data (derived) | `unity/Assets/StreamingAssets/map/heightmap.r16` (8 MB), `content/map/hexes.json` | Copernicus DEM GLO-30, used under the Copernicus licence with the required attribution text (`promo/CREDITS.md`, `tools/README.md`). | Low, provided the attribution travels. |
| | `content/map/precip_bio12.{json,u16}` | CHELSA V2.1, CC0, citation requested. | None. |
| | `content/map/precip_stations.json` | IMS via data.gov.il dataset 481, licence "Other (Open)" **with no licence URL**. This is the deferred row: M0–M2 plan line 6282, carried in the M3 plan, line 10816: "Legal review of map-data licences … M5 / owner". | **Open**. Owner verifies the data.gov.il terms of use. |
| | `content/map/settlements_1948.yaml`, `settlement_hexes.yaml` | Coordinates from the Wikipedia API, each with source URLs. | Low (facts). |
| Portraits | 13 photos in `unity/Assets/UI/Portraits/*.jpg`; 14 photos in `promo/ref/people/*.jpg` (the extra one is Dayan, CC0) | Wikimedia Commons. Licence recorded only as "Public domain" (`PORTRAITS.txt`, `people.yaml`, `promo/CREDITS.md`), not as the Commons template (PD-Israel, PD-IsraelGov and so on), the author or the date. | Medium: record the exact template per file. PD-Israel photos can still be copyrighted in the US (URAA), so check the Commons "PD in US" tag. |
| Fonts | `unity/Assets/UI/Fonts/EBGaramond-{Regular,SemiBold}.ttf` | SIL OFL 1.1, with `OFL.txt` and `SOURCE.txt` (repo, commit and sha256) beside them. | None, as long as OFL.txt ships in builds. |
| AI promo images | `promo/final/01–08*.png` (concept mockups); `promo/ref/*-src.jpg` and `*-sheet.jpg` (inputs to the edit model) | ElevenLabs Image API (`/v1/flows/image`) using GPT Image 2.5 and Nano Banana Pro (`promo/CREDITS.md`, `promo/gen.py`). Three `*_fix` shots edit "the attached game screenshot" `promo/ref/*-src.jpg`, which by commit history (`a64011a`, then `d42bd6b`) look like the earlier AI outputs. The prompts in `promo/shots.json` say "in the style of Civilization VII". | **Owner verifies** that the ElevenLabs terms (and the underlying model providers' terms) allow public redistribution of outputs for the plan tier the account had on 2026-10-08/09. Not verified here, because no external service was contacted. Also: AI output may not be copyrightable, so a licence on it may grant nothing. |
| Owner-ruled README hero | `promo/final/03-gameplay-1936.png` (7.5 MB, 2752×1536) | An AI edit of `promo/ref/gameplay-1936-src.jpg`. It shows features the game will not have, such as tile yields. | Caption required. Web copy needed (size). The ElevenLabs terms check above applies. |
| In-engine captures | `promo/final/09-m2-flythrough.mp4`, `10-m2-topdown.png` | Unity captures of our map (`MapCapture`), derived from Copernicus data. | Low: Copernicus attribution. |
| Geo reference | `promo/ref/geo-reference.png` | Drawn by `promo/refmap.py` from gazetteer coordinates. | None. |
| Spike evidence | `spikes/m0/evidence/*.jpg`, `*.txt` | Our own screenshots and logs. | None. |
| Unity project files | `unity/ProjectSettings/*`, `unity/Assets/Settings/*` (URP template-derived), `unity/Packages/manifest.json`, `packages-lock.json`, `spikes/m0/unity/**`. History also has the deleted Unity template `TutorialInfo/*` and `Readme.asset`. | Unity packages are fetched by the Package Manager and not committed (`Library/` is ignored). They fall under the Unity Companion License or the Unity Terms of Service. Template-derived settings assets are normal to publish. | Low. Users need their own Unity licence (Personal is free under its revenue cap). Do not ship the Unity logo. |
| Research notes | `research_notes/**` (10 files, about 430 KB), `reports/Israel nation sim game build.md` | Research write-ups with cited URLs. A count of quoted strings of 200 characters or more found 28 across 11 files: 8 in `civ7_art_direction.md`, 8 in the report, 3 in `design_systems.md`, 3 in `eras_events_catalog.md` (plus 3 blockquote lines), and the rest spread across the other files. Most are short attributed quotes from news, Steam and policy pages. | Low to medium. Quotation with attribution, but the notes cannot be relicensed under CC BY-SA as if they were all ours. Mark them "© respective authors; quoted for commentary". **Owner decision**: whether to keep them in future commits. History keeps them either way. |
| Plans and specs | `docs/superpowers/**` (the M3 plan alone is about 520 KB) | Internal plans. The no_human lesson was to keep these private. | **Owner decision.** They are already in history, and the no-rewrite ruling means they will be public. They contain no secrets: the scan was clean. They do mention another of the owner's projects and its ElevenLabs credit pool (`docs/superpowers/specs/…design.md:302`). |

## 3. Sensitive content

The spec fixes the policy (`docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`):
- line 30: Israeli-perspective narrative, one sourced value per fact;
- lines 37–38: every fact sourced, no invented quotes;
- lines 201–235: the source/status pipeline, sensitivity tags and their reviews, public-domain/CC0 portraits only;
- line 298: review-bombing risk accepted.

The public docs must state this honestly: README, CODE_OF_CONDUCT, a CONTENT_POLICY, and a historical-accuracy issue form. Specifically:
- the framing is a design choice and not up for debate in PRs;
- facts are, and are corrected with sources;
- no quote is attributed to a real person unless it is sourced verbatim;
- personal or political attacks are banned;
- the maintainer decides disputes.

Platform evidence for why this matters is in `research_notes/Israel nation sim game build/sensitive_content_platforms.md` (Fursan al-Aqsa takedowns, review bombing, Germany's age-rating requirement).

## 4. Repo hygiene

- **No LFS, no `.gitattributes`.** The pack is 92 MB. The largest blobs in history: `heightmap.r16` 8.0 MB, `03-gameplay-1936.png` 7.5 MB, `02-map-diorama.png` 7.4 and 7.1 MB, `04-cabinet-1948.png` 6.1 MB, `09-m2-flythrough.mp4` 5.9 MB. Everything is under GitHub's 50 MB warning and 100 MB limit. Moving to LFS would mean a rewrite (ruled out), so keep plain git and add a per-PR size check for new files.
- **`.gitignore` on `m3`** covers `unity/Library|Temp|Logs|UserSettings|Build|Builds`, the sim plugin DLLs, `*.csproj`/`*.slnx`, `unity-*.log`/`unity-*.xml`, `promo/out|preview|*.log`, `.worktrees/`, `tools/.cache/`, `captures/`, `__pycache__/`, `.DS_Store` and `StreamingAssets/content`. It is **missing** environment files, Unity licence files (`*.ulf`, `*.alf`), `.superpowers/`, `.pytest_cache/` (pytest adds its own) and `.claude/settings.local.json`.
- **Untracked in the `main` checkout:** `research_notes/M4 visual pipeline/` and `research_notes/open source/` (these notes). The `m3` worktree has an untracked `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`.
- **Seven local branches.** Only `main` should ever be pushed.

## 5. Re-run on `main` (information only)

| Date | Tool | Scope | Result |
|---|---|---|---|
| 2026-10-11 | `scripts/oss/scan_history.py --ref main` | 248 commits, every added line (merges included), every message and identity | 8 findings, **0 open secret-shape findings**. The 8 are 7 fake or template home paths and the owner's own email in `promo/fetch_portraits.py` (commit `d42bd6ba6c`, decision D10). All 8 are listed in `docs/oss/history-accepted.tsv`. |
| 2026-10-11 | `scripts/oss/check_repo.py` (secrets, emails, home paths, sizes, symlinks) | the tip of `main` | OK |

## 6. Since the snapshot

Snapshot as of 2026-10-10 (sections 1 to 4). Since then: the repository is public on GitHub (it had no remote when the audit ran); the owner's email was removed from the tip of `promo/fetch_portraits.py` (it stays in history, decision D10); the `.gitignore` gaps listed in section 4 are closed (5411f02); and `promo/gen.py` reads its key from the environment only. Sections 1 to 4 above are left as they were written.
