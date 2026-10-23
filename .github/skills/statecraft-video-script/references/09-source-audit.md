# 09 Source audit, 17 October 2026

This records AI-assisted source checks for the examples and cited guidance. It is not a human factual sign-off. Repository checks use upstream 46abd99c. No historical content file or existing review status was changed.

## Historical examples

| Claim | Source read and locator | Result |
|---|---|---|
| Hula works in the demilitarised zone; concession; Syrian complaint and stop-work request | [FRUS 1951 vol. V, doc. 288](https://history.state.gov/historicaldocuments/frus1951v05/d288), 5 March telegram, paragraphs on works and footnote 5 | Supports the narrow claims in both examples. The footnote records the 7 March memorandum; the telegram is dated 5 March. |
| Contemporary drainage reporting | [JTA, 27 March 1951](https://www.jta.org/archive/israel-resumes-drainage-of-huleh-swamps-syria-concentrates-troops-situation-tense), report body | Opened as corroborating contemporary reporting. Neither example adopts its casualty, cost or completion estimates. |
| Hula geography | [Israel Nature and Parks Authority](https://en.parks.org.il/reserve-park/hula-nature-reserve/), historic lake and access; [Upper Galilee regional guide](https://www.parks.org.il/palgey-maim/) | Supports the northern-Israel setting. Do not confuse the historic lake with the later restored lake or Agamon park. |
| Carrier route and length | [Mekorot history](https://www.mekorot-int.com/our-history/), 1960s entry | Operator describes the Sea of Galilee to arid-south route and 130 km. The old superlative about central population is omitted. |
| Carrier operating date | [Mekorot visitor centre](https://www.mekorot-int.com/visitor-center/), Sapir tour | Pumps described as operating since 1964. Used alongside the operator's route history. |
| Treaty additional-water commitment | [Primary treaty text reproduced by Yale Avalon](https://avalon.law.yale.edu/20th_century/jordan_treaty_annex2.asp), Annex II, Article I.3 | Corrected: both parties seek sources for an additional 50 million cubic metres annually for Jordan. This clause is distinct from the specified transfers in Article I.2. The UN page was inaccessible in the direct recheck; Avalon text was opened and read. |
| Ashkelon chronology | [Y. Dreizin, A. Tenne and D. Hoffman, Integrating large scale seawater desalination plants within Israel's water supply system](https://www.gov.il/BlobFolder/generalpage/desalination-main/he/GRCE0035.pdf), 2007 paper, PDF page 12 | PDF downloaded and passage read. September 2000 tender; November 2001 initial contract; April 2002 capacity increase. The script's old 2002 tender claim is corrected. Water Commission-affiliated authors provide a planners' account. |
| Half of drinking water desalinated in 2015 | Inherited Wikipedia-based figure | Removed from the rewrite. No verified primary-source numerator and denominator are asserted. This remains unverified rather than being assigned a new check date. |
| 2002 desalination card and options | No matching current event definition established | Removed from the rewrite. The [maintainer's #639 note](https://github.com/eyalgolan/statecraft-israel/issues/639#issuecomment-6191440479) that a card is shown in a promo does not establish implementation or its historical accuracy. |

## Existing trailer and Negba limits

- The [merged trailer source sheet](../../../../research_notes/Promo%20trailer/trailer-quote-sources.md) is an existing research record, not a blanket verification stamp. Its unresolved translator and language questions remain visible there.
- Begin's three-part formulation was rechecked in the [American Presidency Project ceremony transcript](https://www.presidency.ucsb.edu/documents/remarks-president-carter-president-anwar-al-sadat-egypt-and-prime-minister-menahem-begin), Begin's civil-courage paragraph. The retained card shortens it. This audit does not alter the owner-retained wording.
- The maintainer's [#639 narration and caption record](https://github.com/eyalgolan/statecraft-israel/issues/639#issuecomment-6191440479), [shell-count and voice decision](https://github.com/eyalgolan/statecraft-israel/issues/639#issuecomment-6196244797), and [#650 retained-v1 ruling](https://github.com/eyalgolan/statecraft-israel/issues/650#issuecomment-6218396185) were re-read through GitHub's API. They establish editorial decisions and supplied wording, not the historical truth of every caption.
- Negba's exact shell count remains unverified against the Bar-Yosef book named by the maintainer. A separate [Project Ben-Yehuda text](https://benyehuda.org/read/67160), Negba chapter, also contains a 150-shell account. Its preface explicitly allows imaginative reconstruction; it does not close the documentary gap. The count stays out of the teaching rewrite.
- The earlier transcript's cut identifier and timestamps remain unestablished. The final promo 3 cue sheet and video were not inspected. Editorial observations about that transcript do not establish defects in the released cut.

## Repository claims

- README.md supports the free, non-commercial description, historical-source standard and Why panel.
- content/events/hula_drainage.yaml defines the prime-minister decision, its two options and their security/intl_standing effects. This is a file check, not proof of what a particular recorded build displays.
- docs/GDD.md limits the current MVP to May 1948 through January 1952. Later historical graphics must be labelled as context; they do not promise playable content.
- docs/event-videos.md distinguishes illustration loops from 3.5-second outcome clips, requires silent video and UI disclosure, and excludes Deport options. The channel table now preserves that distinction.
- docs/RELEASING.md supplies the real-build Steam rule and the separately disclosed cinematic README hero pattern.

## Platform and accessibility guidance

- [Steamworks Trailers](https://partner.steamgames.com/doc/store/trailer), opened 17 October 2026: trailer requirement, gameplay and sound-off advice, upload settings, automatic micro-trailer, poster guidance and English base-language setting checked. Exact export settings should still be read at export. Advice is not restated as a mandatory platform rule.
- [W3C captions guidance](https://www.w3.org/WAI/WCAG22/Understanding/captions-prerecorded.html): dialogue, speaker identity and meaningful non-speech sound checked.
- [W3C audio-description guidance](https://www.w3.org/WAI/WCAG22/Understanding/audio-description-prerecorded.html): additional description is unnecessary only when existing audio already conveys all important visual information. The final edit must meet that condition; the guide does not claim conformance merely from a script.
- [National Digital Agency language-guide landing page](https://harhayeda.gov.il/guides-digital-transformation/language-characterization-guide/): identity and general purpose checked. Detailed Hebrew recommendations remain tied to the separate UI-guide draft and native review; its embedded document was not freshly inspected in this audit.

## Remaining production work

Record footage and build id; check final timing and captions against the cut; obtain asset-specific permissions; have a human check the factual claims and a native reader check Hebrew. These are specific release tasks, not a claim that the original unchecked draft is ready to publish as a video.
