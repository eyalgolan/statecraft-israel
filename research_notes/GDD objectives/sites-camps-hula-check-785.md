# Camp 80, Tzrifin and the Hula site rows: source comparison (#785)

**Research draft; AI reads only; a person's check pending.** Read on 2 November 2026 (UTC).
This is the four-row slice [approved by the owner](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6584328372)
and [claimed before work](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6590819261).
Refs #785 only. Its person's-check Done-when remains open.

## Scope and method

The baseline is main `991aa3550d109c0a0187b21f26f094351cdaa800`:
[`content/sites.yaml`](../../content/sites.yaml), rows `camp_80` (lines 250–281), `tzrifin` (282–312),
`hula` (313–340) and `hula_field` (341–353). These line numbers were verified on that commit.
The earlier [camps comparison](../Map/camps-1948-51.md), sections 1 and 4, is background;
its earlier map reads are not new map verification here. The existing
[seven-row note](sites-draft-rows-check-785.md) and [Kiryat Shmona / Beit Shemesh note](sites-ks-bs-check-785.md)
cover other slices.

Every finding below is **an agent read 2 November 2026; a person's check pending**. “Matches” means the identified
source prints the claim, not that a historian or sensitivity reviewer approved it. Wikipedia is secondary;
its citation to another publication is not a read of that publication. The PDFs were searched through their
text layers and the cited dates, coordinate line and map captions were checked against rendered pages.
No source image, traced boundary or dataset is added.

## Four-row comparison

| Row | What the reopened sources support | Conflict or limit | Still needed before factual sign-off |
|---|---|---|---|
| `camp_80` | Gshur dates the move from Kfar Yona to Camp 80 near Pardes Hanna to 13 July 1948; Gshur and the Nahal Association both describe training there. | Gshur gives early October for the first course's end; the Association gives September. These are retained as different accounts. | A person's source check; exact course-end day and historical camp footprint remain gaps. |
| `tzrifin` | The 2016 survey describes Compound 1, gives its point, and reproduces earlier plans/aerial imagery. Hebrew Wikipedia prints the row's capture dates and Bahad 4 start year. | A modern compound point and a planning boundary are not the whole 1948–51 camp. The Wikipedia-dependent days/year are not independently established by this survey. | A person's check of the underlying dated sources and historical camp location; the existing sensitivity review remains open. |
| `hula` | FRUS distinguishes the northern swamp/lake drainage from preliminary Jordan works below the lake in the DMZ. JTA reports works resuming in March 1951. | JTA places swamp drainage inside the DMZ; FRUS places the swamp/lake drainage outside it. This source conflict remains unresolved. Neither report validates the row's rebaked swamp hex or clears it against historical villages/cadastral boundaries. | A person's source check and a separate historical-map overlay. |
| `hula_field` | FRUS discusses future reclaimed farmland. | That is not evidence of a completed field at the game's chosen hex or on a specific completion day. | Preserve the row's explicit design-placement classification; no historical-field sign-off follows from this read. |

## Camp 80: the disagreement stays visible

**Agent read 2 November 2026; a person's check pending.** [Gshur][gshur], printed pp. 114–115
(PDF pages 5–6), describes the 1931-born trainees and the move on 13 July 1948 after artillery fire at Kfar Yona.
Printed p. 120 (PDF page 11) describes enlistment on 12 September and about another month of training;
p. 122 (PDF page 13) explicitly puts the first course's end in early October. It does not supply an exact day there.

The [Nahal Association retrospective][nahal], under the “הנח״ל זורם” heading and in the paragraphs beginning
“הנח"ל החל זורם” and “המחזור הראשון”, places training at Camp 80 near Pardes Hanna and ends the first course in September 1948.
The baseline row already prefers the IDF-published, referenced Gshur article; this note records the disagreement
and does not replace that decision with a guessed day.

The [Camp 80 Wikipedia revision][camp-wiki] also cites Gshur for the move. That is reuse of the same source,
not independent corroboration. Its modern location description is not a dated camp survey.
The row's modern reference point and hex are inherited data, not coordinates remeasured by this note.
No historical GIS clearance or absence-of-displacement finding is established.

## Tzrifin: keep the modern compound separate

**Agent read 2 November 2026; a person's check pending.** [Leshem-Shefer][survey], January 2016 cover (PDF page 1),
§1.1, printed p. 5 (PDF page 5), explicitly places Compound 1 in the camp's south-east and gives
`185821, 651281`. The source calls it a point for Compound 1; this note does not convert it or promote it
to a surveyed 1948–51 camp position.

Printed pp. 8–9 (PDF pages 10 and 14) give a retrospective 1948 capture account without the row's
18–19 May days. Figure 4 (PDF page 12) is captioned as a 1939 camp plan; figure 8 (PDF page 17)
as 1946 aerial imagery. Both carry an overlaid planning boundary. That boundary serves the later survey,
not a certified historical camp perimeter. The reproduced material is useful for further location research;
no georeferencing or village/parcel overlay was performed here.

The [Tzrifin Wikipedia revision][tzrifin-wiki] prints 18–19 May 1948 and cites *Al HaMishmar* and *Haaretz*,
20 May 1948. Those newspaper articles were **not opened in this pass**; a Wikipedia citation is not their verification.
The [Bahad 4 revision][bahad-wiki] prints 1949 as the starting year at Tzrifin, without a citation attached to that
sentence. Neither that year nor an exact opening day is independently verified here.
The row's historical-location gap and sensitivity flag remain; this note supplies no sensitive card text or sign-off.

## Hula swamp and field: reported works are not a surveyed field

**Agent read 2 November 2026; a person's check pending.** [FRUS doc. 288][frus] is Tyler's telegram of 5 March 1951,
relaying General Riley's account, including a Syrian complaint about works starting around 14 February.
Its numbered points distinguish northern drainage outside the DMZ from preliminary Jordan work within it.
This diplomatic report supplies no surveyed polygon.

[JTA's 27 March 1951 report][jta] places the resumed swamp-drainage work inside the DMZ,
while FRUS places the swamp/lake drainage outside it and the preliminary Jordan works within it.
The sources conflict on where the swamp works were; this note does not resolve that conflict.
JTA also describes activity along the Jordan and presents future farmland and completion as expectations.
The reports concern different dates, and neither provides independent geographical clearance of the same boundary.

For `hula_field`, FRUS's projected farmland is not a field-completion record. The baseline row explicitly
makes the field a design placement at the swamp site, available after drainage. Its hex and project-state rule
are not historical findings from either report. No rule, availability date, swamp boundary, historical village
assessment or earlier map-research result changes here.

## Source access and review hand-off

All reads in this table are **agent reads 2 November 2026; a person's check pending**. No `checked:`, `status`,
`reviewed_by`, coordinate or hex is edited in `content/`.

| Source | Route and extent actually read on 2 November 2026 (UTC) |
|---|---|
| [Gshur PDF][gshur] | Web reader returned 403; direct HTTPS download returned 200 and a 23-page PDF. Printed pp. 114–115, 120, 122 read in text and rendered pages. |
| [Nahal Association][nahal] | Web reader returned the article text, including its September course-end paragraph. |
| [2016 survey PDF][survey] | Web reader exceeded its size limit; direct HTTPS download returned 200 and a 71-page PDF. Cover, §1.1, printed pp. 8–9 and figures 4/8 read in rendered pages and text where available. |
| [Three Wikipedia revisions][camp-wiki] ([Tzrifin][tzrifin-wiki], [Bahad 4][bahad-wiki]) | Web reader cache misses; exact revision wikitext retrieved with `action=raw`, HTTP 200 for each. No live-page substitution or coordinate-template/Wikidata validation. |
| [FRUS doc. 288][frus] | Web reader returned the telegram text and numbered project distinction. |
| [JTA, 27 March 1951][jta] | Web reader returned the dated article body; the linked original daily-bulletin PDF was not opened. |

A person still needs to open the cited passages, record their own findings and resolve the remaining
source/date gaps before any factual promotion. Historical map clearance and sensitivity decisions remain
separate review work. This note neither closes #785 nor signs any row.

## AI assistance

OpenAI Codex reopened the public sources, compared them with the pinned rows and drafted this note.
The source and diff review described here was performed by an AI agent, not an independent human reviewer.
For the 3 November 2026 review follow-up, Codex reopened FRUS, the JTA archive article and the Nahal page;
the other 2 November reads were not re-certified.

[gshur]: https://www.maarachot.idf.il/media/qyen3yoj/%D7%9E%D7%9B%D7%95%D7%91%D7%A2-%D7%92%D7%A8%D7%91-%D7%9C%D7%9B%D7%95%D7%91%D7%A2-%D7%98%D7%9E%D7%91%D7%9C-%D7%94%D7%A0%D7%97%D7%9C-%D7%91%D7%A9%D7%A0%D7%AA%D7%95-%D7%94%D7%A8%D7%90%D7%A9%D7%95%D7%A0%D7%94.pdf
[nahal]: https://www.nahal.co.il/%D7%9E%D7%92%D7%9C-%D7%95%D7%97%D7%A8%D7%91/
[survey]: https://soil-remediation.co.il/wp-content/uploads/2019/06/%D7%A1%D7%A7%D7%A8-%D7%96%D7%99%D7%94%D7%95%D7%9D-%D7%A7%D7%A8%D7%A7%D7%A2-2016.pdf
[camp-wiki]: https://he.wikipedia.org/w/index.php?oldid=43463565
[tzrifin-wiki]: https://he.wikipedia.org/w/index.php?oldid=43963697
[bahad-wiki]: https://he.wikipedia.org/w/index.php?oldid=43873223
[frus]: https://history.state.gov/historicaldocuments/frus1951v05/d288
[jta]: https://www.jta.org/archive/israel-resumes-drainage-of-huleh-swamps-syria-concentrates-troops-situation-tense
