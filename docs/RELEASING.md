# Releasing

A release is a tagged commit on `main` with a CHANGELOG section and, from the first shared build on, a WebGL zip on the
GitHub Releases page (and, from M4, on itch.io). Steps 1–3 are a normal pull request. Steps 4–7 happen on the merged `main`, and steps 5–7 are owner steps.

1. **Choose the version** (Semantic Versioning, pre-1.0 while the game is pre-release).
2. **Bump every version site in one pull request:** `sim/Statecraft.Sim/SimInfo.cs` (`Version`) and
   `unity/ProjectSettings/ProjectSettings.asset` (`bundleVersion`). Then move the `## [Unreleased]` entries in
   `CHANGELOG.md` under `## [X.Y.Z] - YYYY-MM-DD` and leave an empty `## [Unreleased]` above it.
   `scripts/oss/tests/test_release.py::test_versions_agree` fails if the three disagree.
3. **Merge** the pull request (squash).
4. **Run the release gate** on the merged `main` (it refuses to run elsewhere): `bash scripts/oss/release_gate.sh`. It must print
   `RELEASE GATE OK`. It refuses while an owner decision is open (including D10, history acknowledgement, and D11, the
   sensitivity review), while an asset row is `owner-verify`, while the sim's sensitivity gate refuses `content/`, or
   while the history scan shows an open secret-shape row.
5. **Build and package** on the owner's machine, from the merged `main`:

   ```bash
   "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -shareable -logFile "$PWD/unity-build-web-br.log"
   git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
   uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5" \
     --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" \
     --expect "GOLDEN OK 1952 62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8" \
     --expect "SMOKE OK 5 turns 1948-06-19 d3fdd459ddb876bd8d775c20edd1202cf56ca7134a8ad06f873d4d92034aa5b6"
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
4. Rebuild `docs/images/social-preview.jpg` (1280×640) from the new map shot: `sips --resampleWidth 1280 <shot> --out /tmp/sp.jpg` then `sips --padToHeightWidth 640 1280 --padColor 0B1220 /tmp/sp.jpg --out docs/images/social-preview.jpg` (padded, not cropped, so no text is cut off). The owner re-uploads it under Settings →
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
