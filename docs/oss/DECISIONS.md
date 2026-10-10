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
- [x] D7 Security and conduct contact. Decided (2026-10-11): GitHub private reporting for both security and conduct (conduct reports start the title with "Conduct:"); the profile has no public email. The owner may later add a dedicated contact, and then updates SECURITY.md, CODE_OF_CONDUCT.md and the issue-form config together.
- [ ] D8 Name clearance for "Statecraft: Israel" (USPTO, EUIPO, Steam). Owner checks. Decided:
- [ ] D9 Unity edit-mode tests stay out of CI. Recommended: yes, local gate with pasted evidence. Decided:
- [ ] D10 I know that my personal email (commit d42bd6b, `promo/fetch_portraits.py`) and the internal plans, specs and research notes in history are public and cannot be removed (owner ruling: no history rewriting); key rotation is the only remedy for a secret. Decided:
- [x] D11 The seven sensitive events (altalena, ten_days, operation_yoav, operation_hiram, operation_horev, bernadotte_assassination, reparations_claim) are sensitivity-reviewed: each event's YAML says `status: sensitivity-reviewed` (docs/m3-release.md, "OWNER STEP: sensitivity review"). The release gate also runs the sim's own check. Decided: the owner signed off all seven on 2026-10-10 for the itch.io build.
- [ ] D12 Ruleset bypass. Recommended: the repository admin role bypasses `protect-main` always (you keep merging locally and pushing); everyone else needs review and green checks. Alternative: admins bypass only through a pull request. Decided:
- [ ] D-P Portraits: every `owner-verify` row in `ASSETS.portraits.yaml` checked on Commons for its US status, and the `promo/ref/*-src.jpg` and `*-sheet.jpg` provenance confirmed. Decided:
- [ ] D-H The AI concept art reproduces no Civilization VII icon, logo or wordmark: every image in `promo/final/` (`01` to `08`) and the hero (`docs/images/hero-concept-1936.jpg`), all prompted "in the style of Civilization VII". Owner looks at each. Decided:
