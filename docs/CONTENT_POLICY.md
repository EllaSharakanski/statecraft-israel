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
