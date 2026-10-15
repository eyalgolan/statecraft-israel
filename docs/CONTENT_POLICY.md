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
   balance, not judgements of the person, and `content/people.yaml` labels them so. The maintainer does not add living
   people, and records a legal review before any release that includes one. Portraits are public-domain, CC0, CC BY 4.0
   (owner ruling on #30, 13 October 2026) or CC BY-SA 4.0 photographs (owner, 14 October 2026, `docs/oss/DECISIONS.md`
   D-P: "yes" to CC BY-SA 4.0 for portraits); share-alike applies, so a portrait cropped from a CC BY-SA 4.0 photograph
   is itself released under CC BY-SA 4.0 and credited in `NOTICE`. The maintainer refuses any other licence at review.
6. **AI-assisted writing** is a draft. A human checks every factual claim in it against its sources, one by one,
   before the entry is marked `fact-checked`; the maintainer applies this rule at review. A Hebrew display name
   (`name_he`, `title_he`, `title_he_f`, `body_he`) that an AI agent checked against its sources, but no person has read
   yet, carries its own flag beside it, `*_review: agent-checked` (for example `name_he_review`), whatever the row's
   `status` says about its English facts (owner, 15 October 2026). A native Hebrew speaker reviews these names (#564)
   and then removes the line; the loader accepts only `agent-checked` (a blank value is an error), and only next to a
   name.
7. **A sign-off names a person.** A row a person has reviewed carries, beside its `status`, `reviewed_by` (the
   reviewer's GitHub handle, never an email) and `reviewed_on` (the day), both or neither (owner, 15 October 2026,
   #375: "Yes, humans only"). Only that person writes the two lines, in their own commit; an AI agent never adds, changes
   or removes them, and `scripts/oss/reviewed_by_guard.py` fails any pull request where a commit with an AI co-author
   trailer does. A signed row is closed to agents too (owner, 15 October 2026, on #629: "Block it in CI"): the guard
   fails a pull request where a commit with an AI co-author trailer changes any field of a signed row, the `id`
   included, until a person signs the row again or removes its sign-off in a commit of their own. How to sign off:
   [HISTORIAN_REVIEW.md](HISTORIAN_REVIEW.md#sign-the-row-off). A row with a sign-off leaves the review queue
   ([review-queue.md](review-queue.md)).

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
- Mechanics or text that reward or celebrate violence against civilians, from any side. A player choice that harms
  civilians, such as the GDD's Deport outcome, may exist only as a historical possibility that never pays: it carries its
  real costs (refugees, Social cohesion, International Support), gives no reward, and is shown soberly through sourced
  events (owner ruling, 12 October 2026).
- Images of real people that are not public domain or CC0. One narrow exception, by owner ruling 2026-10-11
  (summarised): archival photographs of events (not portraits) under CC BY 4.0 are allowed, on these terms:
  - Each carries a credit line naming the photographer and collection (for the Meitar Collection: photographer,
    Meitar Collection, National Library of Israel, The Pritzker Family National Photography Collection), with the
    licence, both on screen with the picture and in `NOTICE`.
  - The only changes are a subtle warm tone grade (the pictures stay black and white, in the gold frame), a crop and a
    resize. No AI colourising and no change to what the picture shows. The adaptation is recorded with the credit as
    "graded and cropped".
  - Government Press Office (GPO) photographs are refused. So are files whose only licence is a PD-Israel or PD-India
    tag, and share-alike files (CC BY-SA).
  - Each photograph gets a sensitivity review before a shareable build may carry it: every one is screened, and any
    marked sensitive waits for the owner's sign-off (the `-shareable` build and the packager refuse until then).

  Each event photograph's credit, alt text and sensitivity review are recorded in
  `unity/Assets/UI/EventImages/event-images.yaml` and `ASSETS.yaml` (see `docs/event-images.md`).

  AI-generated likenesses of real people appear only in concept art (`promo/final/04`, `08`), are recorded as
  AI-generated in the asset provenance records, and are never presented as photographs or used in the game.
- Hate speech, slurs or dehumanising language about any group, in content, code comments, issues or commits.
