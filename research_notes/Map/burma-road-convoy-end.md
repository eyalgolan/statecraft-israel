# Burma Road convoy traffic: approximate end date (#1040)

Status: research only. This note does not change `content/` or code.

## Finding

**Approximate route end: 7 December 1948.** No source found dates the last convoy or the last vehicle to use the Burma
Road. The date is therefore a proxy for the change of route, not an exact last-convoy date.

The Ministry of Defense's Yizkor site supplies the two parts needed for that proxy in one statement: the paved Road of
Valour replaced the Burma Road dirt track, and it was inaugurated on **7 December 1948**. Doron Rosen's 2022 scholarly
history independently says that the Road of Valour replaced the Burma Road and the old Jerusalem road and became the
single permanent supply and traffic artery from the coastal plain and lowlands to Jerusalem. An IDF journal article
independently dates the Road of Valour opening ceremony near Hulda to **7 December 1948**.

This does **not** prove that no isolated vehicle used the Burma Road after that date. It supports the narrower finding
that regular Jerusalem supply and traffic transferred to the replacement road at its opening. Under the owner's #949
rule, use the first day of a sourced approximate range; here the evidence identifies the replacement event on one
best-supported day, so the approximate route-end value is **7 December 1948**.

## Sources

| Source | What it supports | `checked:` | Assessment |
|---|---|---|---|
| [Ministry of Defense, Yizkor: memorial to the Burma Road breakers](https://www.izkor.gov.il/monument/en_818728be2bfcfa7a9e62d512cfb62989) | Names the dirt track the Burma Road and says it was replaced by a road inaugurated on 7 December 1948, which (as Route 44) carried the traffic to Jerusalem until after June 1967. | 2026-10-18 | Official Israeli source; direct support for the replacement and date. |
| [Shmuel Cohen Shani, “Between the Beret and the Top Hat,” *Bein HaKtavim* 24–25, IDF](https://www.idf.il/media/otcd0dxl/%D7%9B%D7%94%D7%9F-%D7%A9%D7%A0%D7%99.pdf), p. 108 (PDF p. 6) | Dates the opening ceremony near Hulda to 7 December 1948. | 2026-10-18 | IDF journal; corroborates the ceremony date, but does not discuss the last Burma Road convoy. |
| [Doron Rosen, *Paving, Building and Fire: Solel Boneh's Activity in the War of Independence* (2022), Kotar catalogue and synopsis](https://kotar.cet.ac.il/kotarapp/index/Book.aspx?nBookID=111218448) | Says the Road of Valour replaced the Burma Road during the war and became the single permanent supply and traffic artery to Jerusalem. | 2026-10-18 | Standard scholarship; direct support for replacement, but the accessible synopsis gives no day. |
| [*HaTzofe*, 7 December 1948, “The Road of Valour will open today”](https://www.nli.org.il/he/newspapers/?olive_path=%2Folive%2Fapa%2Fnli_heb%2FSharedView.Article.aspx&olive_query=href%3DHZH%252F1948%252F12%252F07%26id%3DAr00416) | Contemporary headline, "כביש הגבורה יפתח היום" ("The Road of Valour will open today"), announces the opening for 7 December. | 2026-10-18 | **Not opened in the 18 October source check:** the NLI page returned HTTP 403. The headline and date are confirmed only through Hebrew Wikipedia, [כביש הגבורה](https://he.wikipedia.org/wiki/%D7%9B%D7%91%D7%99%D7%A9_%D7%94%D7%92%D7%91%D7%95%D7%A8%D7%94), footnote 12 (checked: 2026-10-18), which cites it. Corroboration only; the finding does not rest on it. |
| [Jewish Telegraphic Agency daily bulletin, 9 December 1948](https://www.jta.org/archive/israel-elated-over-opening-of-new-road-to-jerusalem-impressive-ceremonies-held) | Reports that the formal opening was marked “yesterday,” implying 8 December 1948. | 2026-10-18 | Contemporary report; opening-date conflict `[C]`. It does not date the last Burma Road convoy. |

No Wikipedia claim is needed for the finding. Wikipedia is used only to confirm the *HaTzofe* headline above.

## Conflicts and limit

- **[C] Opening date:** the official Ministry of Defense page, the IDF article and the contemporary *HaTzofe*
  headline support **7 December 1948**. The JTA bulletin dated 9 December says the opening was “yesterday,” implying
  **8 December**. Existing project research also records **10 December** from a footnote reproducing Ya'akov Dori's
  order of the day and **12 December** from later books, neither read directly for this note. The official Israeli
  sources and the same-day newspaper notice make 7 December the strongest value; the alternatives remain `[C]`.
- **Gap:** none of the sources names or dates a final Burma Road convoy. “7 December 1948” must remain labelled
  approximate and described as the replacement-road boundary, not as a witnessed last journey.

## Implication for #1028 `RoadVehicles`

In a follow-up code PR, give both `burma_road` plans (`SandwichConvoy` and `CanvasLorry`) an exclusive route end of
`1948-12-07`. `RoadVehicles.Plan.Covers` treats `To` as exclusive, so the vehicles would be present through
6 December and absent from 7 December, the first day of the approximate replacement boundary under the owner's #949
rule. Add an EditMode boundary test for 6 and 7 December. Do not change the drawn Burma Road route: this finding is
about regular convoy traffic, not the track's physical existence.

## Confidence

**Moderate.** Confidence is high that 7 December is the best-supported opening/replacement date, but only moderate for
using it as the convoy end because no source found records the final Burma Road convoy and one contemporary report
implies 8 December.

## Source check (18 October 2026)

Every URL above was reopened on 18 October 2026. The Yizkor page, the IDF article (the opening ceremony near Hulda
on 7 December 1948 is on printed p. 108, which is PDF page 6, not 5), the Kotar synopsis and the JTA bulletin say
what the table says. The *HaTzofe* page could not be opened (HTTP 403), so it is marked as confirmed only through
Hebrew Wikipedia.

## Question for the owner

Your #1028 ruling gave "the opening of the Valor Road" as an example of an end date. No source dates the last Burma
Road convoy. Should the two vehicles stop on 7 December 1948 (the road's opening, flagged as approximate), or stay
while the road is drawn?
