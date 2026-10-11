# Event-card photographs

The event card's header picture, as in the promo (`promo/final/03-gameplay-1936.png`, `05-event-transit-camps.png`), is a
real archival photograph of the event, never an AI image. 25 of the 31 events in `content/events/` have one. The other
six show the responsible minister's portrait in the card's head, as before: `rhodes_talks`, `armistice_lebanon`,
`armistice_syria`, `education_registration_1951`, `reparations_claim` and `forex_crisis_1951`. No free photograph that
fits them was found.

Owner rulings, 2026-10-11 (quoted in `docs/CONTENT_POLICY.md` section 4):
- CC BY 4.0 archival event photos are allowed with a credit line. The real-people rule stays strict for portraits.
- Photos are shown as authentic black and white, with a subtle warm sepia/navy grade and the gold frame. There is no
  AI colourising and no change beyond grade, crop and resize. The adaptation is recorded as "graded and cropped".

## Where things are

| What | Where |
|---|---|
| The pictures, 720 x 320 JPEG (9:4), each under 200 KB | `unity/Assets/UI/EventImages/<event>.jpg` |
| The mapping: event, Commons page, original URL and SHA-1, licence, artist, credit, alt text, crop, sensitivity flags | `unity/Assets/UI/EventImages/event-images.yaml` |
| The catalog the game loads (`Resources.Load`, so there is no scene field) | `unity/Assets/Resources/EventImageCatalog.asset` |
| Provenance rows, one per file, `status: owner-verify` | `ASSETS.yaml` |
| CC BY 4.0 credits | `NOTICE`, and under the picture on the card |
| The grade and crop | `tools/event_images.py` |

The mapping is **not game content**. The event YAMLs feed the content hash and save compatibility, so nothing about
pictures may go into `content/`. `EventImageMappingTests.TheMappingIsNotGameContent` guards this, and the golden hashes
and `SaveGameTests` stay unchanged.

## The card

When the event has a picture, it fills the top of the card in a gold frame, as in the promo. Its credit line runs as a
small italic caption strip along the picture's bottom edge, so the credit is always visible but costs the card no
height. The minister's portrait is hidden, and the office line still names the minister. The alt text is the picture's
`tooltip`, and `EventCardPresenter.PhotoAlt` exposes it. On a short window the picture shrinks first; below 48 px
(`EventCardPresenter.MinPhotoHeight`) it is dropped for that card, so the body text and the options never scroll
because of it (`UiLayoutTests.EveryEventCardStaysAboveTheMinimap`). The
card's height cap rose from 560 to 600 px to make room; the right-hand column still bounds it above the minimap.

## Adding or replacing a picture

1. Find a file on Wikimedia Commons that is public domain, CC0 or CC BY 4.0. Never use share-alike files. Never use
   GPO, PD-Israel-only or PD-India files: the URAA may have restored their US copyright (decision D-P).
2. Read the licence, the licence templates and the artist on the file page. For a Meitar Collection file, the credit
   is the licensor's credit line, which the page's `{{cc-by-4.0|1=...}}` template expands to, for example "Benno
   Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National Photography Collection".
3. Download the original once, into an empty folder outside the repo, as `<event>.jpg`. It is untrusted data.
4. Add the row to `event-images.yaml`:
   - The credit ends with " · CC BY 4.0 · graded and cropped" (or "CC0" / "public domain").
   - The alt text says what the picture shows. A date in it is the event's own date, or only a month or a year:
     library dates are sometimes wrong (the NLI dates the Altalena shelling 20 June; it was 22 June).
   - Set `sensitive: true` for an event with a sensitive tag, or for any picture the owner must see before release.
5. Make the JPG with `uv run --with pillow==12.3.0 --with pyyaml==6.0.3 python -I tools/event_images.py --originals DIR
   --only <event>`. It refuses a file whose SHA-1 does not match the row.
6. Add the `ASSETS.yaml` row: glob, licence, source (the Commons page), `status: owner-verify`, artist, credit,
   `adapted: "graded and cropped"`, and a note. For a CC BY file, add the credit to `NOTICE`.
7. Run `Statecraft.Game.EditorTools.EventImageImport.Run` in Unity, then the EditMode tests.

## Owner steps

- **Sensitivity review.** Seven pictures are marked `sensitive: true`: `altalena`, `ten_days`, `operation_yoav`,
  `operation_hiram`, `operation_horev`, `bernadotte_assassination` and `sinai_withdrawal` (an RAF wreck).
  - A `-shareable` build (`Build.Web`/`Build.Mac`, the release flow in `docs/RELEASING.md`) refuses until each one is
    `sensitivity_reviewed: true` and `EventImageImport.Run` has rebuilt the catalog.
  - Dev builds still show them, so the owner can review them in the game.
  - Marking a picture reviewed is the owner's step only.
  - `tools/package_release.py` checks only the content inside a build folder; it cannot see the pictures, which are
    packed into Unity's data files. So the picture gate is the `-shareable` build, which `docs/RELEASING.md` already
    uses.
- **Provenance.** Every `ASSETS.yaml` row stays `owner-verify` until the owner checks it. `check_repo.py --release`, in
  the release gate, refuses those rows until then.
