# Offices, governments and office holders, 1977-1993

Researched 13 October 2026. Every URL below was read on that date. Research note for issue #145 (1977-93 item 1,
part of #51). Nothing here is drafted for the game and no file in `content/` is changed.

## Purpose

Fix one sourced value for every government, minister and senior office holder from the 18th government (20 June 1977)
to the 25th (to 13 September 1993, the last day of the planned chapter), so that `governments.yaml`, `offices.yaml`
and `people.yaml` can be filled later without new research. It also answers the *to verify* marks in §2.1 of
[the 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md).

## Conventions

- [V] read in a primary source this session: the Knesset's own records (see Sources). [S] secondary only (Wikipedia
  or Wikidata). [C] a primary source and Wikipedia disagree, or two sources disagree; every value is given.
  [GAP] not found.
- Periods are [from, to), as in `people.yaml`: `to` is the day the next holder or government took office.
- "Knesset row(s)" are `PersonToPositionID` values in the Knesset's position table. Row *n* is
  `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition(n)`. Two ids mean the Knesset split one term
  where a new Knesset convened (20 July 1981, 13 August 1984, 21 November 1988); they are joined here, as earlier
  terms in `people.yaml` are.
- "en.wiki" means the English Wikipedia pages named in Sources, read as raw wikitext on 13 October 2026.

Every [V] in this note was an agent's reading on 2026-10-13, unless the tag gives a later date; a person's check is pending for all of them.

## Sources

Primary:

- Knesset OData, `KNS_PersonToPosition`, one query per government:
  `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?$filter=GovernmentNum%20eq%20N` for N = 17
  to 26. Names from `KNS_Person(PersonID)`, position types from `KNS_Position`. This is the method `people.yaml`
  already uses for 1952-1956.
- Knesset plenum records (Divrei HaKnesset), found through `KNS_PlenumSession` and `KNS_DocumentPlenumSession` and
  read from fs.knesset.gov.il (Word files, converted to text):
  - 20 June 1977, session 491051 (16:04 to 00:23): https://fs.knesset.gov.il/9/Plenum/9_ptm_258249.doc (PDF 9_ptm_254065.pdf)
  - 24 October 1977, session 491062: https://fs.knesset.gov.il/9/Plenum/9_ptm_258260.doc
  - 19 April 1978, election of the President: https://fs.knesset.gov.il/9/Plenum/9_ptm_258280.doc
  - 29 May 1978, President's declaration: https://fs.knesset.gov.il/9/Plenum/9_ptm_258285.doc
  - 5 August 1981, session 482644: https://fs.knesset.gov.il/10/Plenum/10_ptm_236508.DOC
  - 22 March 1983, election of the President: https://fs.knesset.gov.il/10/Plenum/10_ptm_236275.DOC
  - 5 May 1983, President's declaration: https://fs.knesset.gov.il/10/Plenum/10_ptm_236489.DOC
  - 10 October 1983, session 482781: https://fs.knesset.gov.il/10/Plenum/10_ptm_236470.DOC
  - 13 September 1984, session 436746 (12:02 to 01:00): https://fs.knesset.gov.il/11/Plenum/11_ptm_236129.DOC
  - 20 October 1986, session 482713: https://fs.knesset.gov.il/11/Plenum/11_ptm_236049.DOC
  - 22 December 1988, session 437018: https://fs.knesset.gov.il/12/Plenum/12_ptm_236815.DOC
  - 15 March 1990, session 437111: https://fs.knesset.gov.il/12/Plenum/12_ptm_236787.DOC
  - 11 June 1990, session 437127: https://fs.knesset.gov.il/12/Plenum/12_ptm_236866.DOC
  - 13 July 1992, session 477087: https://fs.knesset.gov.il/13/Plenum/13_ptm_532132.docx
  - 24 March 1993, election of the President: https://fs.knesset.gov.il/13/Plenum/13_ptm_532085.docx
  - 13 May 1993, President's declaration: https://fs.knesset.gov.il/13/Plenum/13_ptm_532112.docx

Not reached: the Knesset's web pages (knesset.gov.il/govt/..., main.knesset.gov.il) answer with a bot challenge;
gov.il returns 403; mossad.gov.il and archives.gov.il return a bot challenge or 403; idf.il returns a script shell. So
no gov.il government page, IDF or Mossad page was read, and the Chief of Staff, Aman and Mossad dates below rest on
Wikipedia only.

Secondary, for comparison: en.wiki "Eighteenth government of Israel" to "Twenty-fifth government of Israel",
"List of prime ministers of Israel", "Ministry of Defense (Israel)", "Ministry of Finance (Israel)", "Ministry of
Foreign Affairs (Israel)", "Ministry of Aliyah and Integration", "Ministry of National Security (Israel)", "Chief of
the General Staff (Israel)", "Military Intelligence Directorate (Israel)", "Mossad", "List of presidents of Israel";
he.wiki "ממשלת ישראל השמונה עשרה", "ראש המטה הכללי", "אגף המודיעין", "המוסד למודיעין ולתפקידים מיוחדים"; Wikidata
(birth and death dates). All at https://en.wikipedia.org/wiki/<title>, https://he.wikipedia.org/wiki/<title> and
https://www.wikidata.org/wiki/<Q-id>.

## 1. Governments

The government takes office when the Knesset votes confidence (Basic Law: The Government, s. 15, read out by the
Speaker on 20 June 1977). Each `from` below is the day of that vote in the plenum record; it matches the Knesset
position rows and en.wiki except where marked.

| Government | Prime Minister | From | To | Confidence vote (plenum) | Mark |
|---|---|---|---|---|---|
| 17th (outgoing) | Yitzhak Rabin | 3 June 1974 | 20 June 1977 | — | [V] Knesset rows 3453, 22727 end 20 June 1977 |
| 18th | Menachem Begin | 20 June 1977 | 5 August 1981 | 63 for, 53 against; session 491051 opened 16:04 on 20 June and closed 00:23 on 21 June; the vote's time is not recorded | [C] en.wiki list of PMs and the 18th-government infobox: 21 June 1977; the same infobox's text says formed 20 June, "sworn in the next day"; he.wiki, Knesset rows, en.wiki ministry lists: 20 June |
| 19th | Menachem Begin | 5 August 1981 | 10 October 1983 | 61 for, 58 against | [C] en.wiki 19th-government infobox: dissolved 28 August 1983 |
| 20th | Yitzhak Shamir | 10 October 1983 | 13 September 1984 | 60 for, 53 against | [V] |
| 21st | Shimon Peres | 13 September 1984 | 20 October 1986 | 89 for, 18 against, 1 abstention; session closed 01:00 on 14 September, vote time not recorded | [V] |
| 22nd | Yitzhak Shamir | 20 October 1986 | 22 December 1988 | 82 for, 17 against, 3 abstentions | [V] |
| 23rd | Yitzhak Shamir | 22 December 1988 | 11 June 1990 | 84 for, 19 against, 3 abstentions | [V] |
| 24th | Yitzhak Shamir | 11 June 1990 | 13 July 1992 | 62 for, 57 against, 1 abstention (roll call) | [V] |
| 25th | Yitzhak Rabin | 13 July 1992 | 22 November 1995 (after this era) | 67 for, 53 against | [V] |

Changes of office and government crises the plenum records fix:

- **24 October 1977.** The Democratic Movement for Change joins. The government asks the Knesset to approve Yigael
  Yadin as Deputy Prime Minister, Yisrael Katz as Minister of Labour and Welfare, Meir Amit as Minister of
  Transportation and of Communications, and Shmuel Tamir as Minister of Justice; the agenda item also sets up the
  Ministry of Labour and Welfare. The statement says that when they take office the Prime Minister stops serving as
  Minister of Justice, of Welfare, of Labour, of Transportation and of Communications (9_ptm_258260). [V]
- **15 September 1983.** Begin resigns: the Cabinet Secretary's letter read on 10 October 1983 gives 15 September
  1983. On 21 September 1983 President Herzog gave Shamir the task of forming a government; Shamir told the Speaker on
  5 October 1983 that he had formed one (10_ptm_236470). [V] The day Begin first announced he would resign (late
  August 1983) is not in these records. [GAP]
- **13 September 1984.** The same sitting passed Basic Law: The Government (Amendment No. 5), which creates the office
  of Acting Prime Minister (ממלא מקום ראש הממשלה) held by Shamir in the 21st government and Peres in the 22nd and 23rd
  (third reading before the 19:15 break, 11_ptm_236129). [V]
- **The rotation, October 1986.** On 17 October 1986 President Herzog gave Shamir the task of forming a government; on
  19 October Shamir told the Speaker he had formed one; on 20 October the Knesset approved it 82 to 17 with 3
  abstentions, and the Speaker thanked the outgoing Prime Minister Peres (11_ptm_236049). So Peres is Prime Minister
  [13 September 1984, 20 October 1986) and Shamir from 20 October 1986, with no gap. [V] The day Peres handed in his
  resignation (en.wiki and most accounts: 10 October 1986) is not in the records read. [GAP]
- **13-15 March 1990.** On 13 March 1990 Shamir removed Peres as Acting Prime Minister and Minister of Finance; the
  same day ten Alignment ministers (Edri, Bar-Lev, Gur, Weizman, Yaacobi, Katz-Oz, Navon, Tzur, Rabin, Shahal) gave
  notice of resignation, which took effect on the morning of 15 March 1990. That day the Alignment's motion of no
  confidence passed 60 to 55 (12_ptm_236787). The design's *to verify* on the 15 March vote is answered. [V] The
  government stayed in office as a transitional government until 11 June 1990; Shamir held the vacated portfolios
  (Knesset rows below).
- **Presidents.** See §4.

Coalition parties per government are not in the Knesset position table and were not checked in a primary source;
en.wiki's infoboxes give them [S]. They belong to 1977-93 item 2 (Knessets and parties) and are listed in §6 only as
a starting point.

## 2. Ministers, by government

All rows [V] from the Knesset position table unless marked. Office ids are those in `content/offices.yaml`; "to add"
and "(new)" are offices the content does not have yet (see §5). Rows that start on or after 13 September 1993 are left
out; terms that run past it show their real end.

### Government 18

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Menachem Begin | 20 June 1977 | 5 August 1981 | 4065, 22752 | [C] see §3, item 1: en.wiki list and 18th-government infobox give 21 June 1977 |
| deputy_prime_minister | Yigael Yadin | 24 October 1978 | 5 August 1981 | 3944, 22755 | [C] Knesset row says 24 October 1978; the plenum record of 24 October 1977 names him Deputy Prime Minister that day (9_ptm_258260), and en.wiki agrees on 24 October 1977. Use 24 October 1977 |
| deputy_prime_minister | Simha Erlich | 7 November 1979 | 5 August 1981 | 4554, 22749 | [C] en.wiki 18th government lists him Deputy PM for the whole government; the Knesset row starts 7 November 1979, the day he left Finance |
| defense_minister | Ezer Weizman | 20 June 1977 | 28 May 1980 | 4167 | [V] |
| defense_minister | Menachem Begin | 28 May 1980 | 5 August 1981 | 4069, 22750 | [V] |
| finance_minister | Simha Erlich | 20 June 1977 | 7 November 1979 | 4553 | [C] en.wiki 18th government table: "until 7/11/1977" (a typo); en.wiki Finance list: 7 November 1979 |
| finance_minister | Yigal Hurvitz | 7 November 1979 | 13 January 1981 | 4579 | [V] |
| finance_minister | Menachem Begin | 13 January 1981 | 21 January 1981 | 22777 | [C] en.wiki lists omit Begin; they leave 13-21 January 1981 empty (the design's "gap to verify") |
| finance_minister | Yoram Aridor | 21 January 1981 | 5 August 1981 | 22775, 22776 | [V] |
| foreign_minister | Moshe Dayan | 20 June 1977 | 23 October 1979 | 3981 | [V] |
| foreign_minister | Menachem Begin | 23 October 1979 | 10 March 1980 | 4071 | [V] |
| foreign_minister | Yitzhak Shamir | 10 March 1980 | 5 August 1981 | 4522, 22767 | [V] |
| health_minister | Eliezer Shostak | 20 June 1977 | 5 August 1981 | 4361, 22766 | [V] |
| interior_minister | Yosef Burg | 20 June 1977 | 5 August 1981 | 4232, 22753 | [V] |
| justice_minister | Menachem Begin | 20 June 1977 | 24 October 1977 | 4072 | [V] |
| justice_minister | Shmuel Tamir | 24 October 1977 | 5 August 1980 | 3516 | [V] |
| justice_minister | Menachem Begin | 5 August 1980 | 13 August 1980 | 22772 | [C] not in en.wiki (gap) |
| justice_minister | Moshe Nissim | 13 August 1980 | 5 August 1981 | 22773, 22774 | [V] |
| agriculture_minister | Ariel Sharon | 20 June 1977 | 5 August 1981 | 4258, 22768 | [V] |
| labour_minister / welfare_minister | Menachem Begin | 20 June 1977 | 24 October 1977 | 4562, 22751 | [V] two rows for one term; until 24 October 1977 Labour and Welfare were separate ministries, both held by Begin (plenum 24 October 1977) |
| labour_minister / welfare_minister | Yisrael Katz | 24 October 1977 | 5 August 1981 | 3756, 22756 | [V] |
| religions_minister | Aharon Abuhatzira | 20 June 1977 | 5 August 1981 | 3506, 22747 | [V] |
| trade_minister | Yigal Hurvitz | 20 June 1977 | 15 January 1979 | 4228 | [C] en.wiki 18th government: until 1 October 1978; he.wiki and Knesset: 15 January 1979 |
| trade_minister | Gideon Patt | 15 January 1979 | 5 August 1981 | 4268, 22763 | [V] |
| transportation_minister | Menachem Begin | 20 June 1977 | 24 October 1977 | 4561 | [V] |
| transportation_minister | Meir Amit | 24 October 1977 | 15 September 1978 | 22778 | [V] |
| transportation_minister | Menachem Begin | 15 September 1978 | 15 January 1979 | 22771 | [C] not in en.wiki (gap) |
| transportation_minister | Haim Landau | 15 January 1979 | 5 August 1981 | 3927, 22759 | [V] |
| posts_minister (Communications) | Menachem Begin | 20 June 1977 | 24 October 1977 | 4073 | [V] |
| posts_minister (Communications) | Meir Amit | 24 October 1977 | 15 September 1978 | 4148 | [V] |
| posts_minister (Communications) | Menachem Begin | 15 September 1978 | 15 January 1979 | 22769 | [C] not in en.wiki (gap 15 September 1978 - 15 January 1979) |
| posts_minister (Communications) | Yitzhak Moda'i | 15 January 1979 | 28 December 1980 | 4177 | [C] en.wiki 18th government: until 22 December 1980 |
| posts_minister (Communications) | Menachem Begin | 28 December 1980 | 5 January 1981 | 22770 | [C] not in en.wiki |
| posts_minister (Communications) | Yoram Aridor | 5 January 1981 | 5 August 1981 | 4311, 22748 | [V] |
| education_minister | Zevulun Hammer | 20 June 1977 | 5 August 1981 | 4341, 22754 | [V] |
| absorption_minister (to add) | David Levy | 20 June 1977 | 5 August 1981 | 4270, 22757 | [V] |
| minister_without_portfolio | Haim Landau | 10 January 1978 | 15 January 1979 | 3926 | [V] |
| minister_without_portfolio | Moshe Nissim | 10 January 1978 | 13 August 1980 | 4205 | [V] |
| [other] housing | Gideon Patt | 21 June 1977 | 15 January 1979 | 4264 | [C] the only 18th-government row that starts 21 June; he.wiki 20 June 1977 |
| [other] housing | David Levy | 15 January 1979 | 5 August 1981 | 4271, 22758 | [V] |
| [other] tourism | Yigal Hurvitz | 20 June 1977 | 15 January 1979 | 4227 | [V] |
| [other] tourism | Gideon Patt | 15 January 1979 | 5 August 1981 | 4266, 22762 | [V] |
| [other] energy | Yitzhak Moda'i | 20 June 1977 | 5 August 1981 | 4173, 22760 | [V] |

### Government 19

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Menachem Begin | 5 August 1981 | 10 October 1983 | 4066 | [C] en.wiki 19th government infobox ends the government 28 August 1983; Knesset row and plenum (Begin's resignation 15 September 1983, Shamir sworn 10 October 1983) end it 10 October 1983 |
| deputy_prime_minister | Simha Erlich | 5 August 1981 | 19 June 1983 | 4047 | [V] |
| deputy_prime_minister | David Levy | 3 November 1981 | 10 October 1983 | 4221 | [V] |
| defense_minister | Ariel Sharon | 5 August 1981 | 14 February 1983 | 4256 | [V] |
| defense_minister | Menachem Begin | 14 February 1983 | 23 February 1983 | 4070 | [V] |
| defense_minister | Moshe Arens | 23 February 1983 | 10 October 1983 | 22817 | [V] |
| finance_minister | Yoram Aridor | 5 August 1981 | 10 October 1983 | 4310 | [V] |
| foreign_minister | Yitzhak Shamir | 5 August 1981 | 10 October 1983 | 4523 | [V] |
| health_minister | Eliezer Shostak | 5 August 1981 | 10 October 1983 | 4362 | [V] |
| interior_minister | Yosef Burg | 5 August 1981 | 10 October 1983 | 4233 | [V] |
| justice_minister | Moshe Nissim | 5 August 1981 | 10 October 1983 | 4209 | [V] |
| agriculture_minister | Simha Erlich | 5 August 1981 | 19 June 1983 | 4046 | [V] |
| agriculture_minister | Menachem Begin | 19 June 1983 | 10 October 1983 | 22818 | [V] |
| labour_minister / welfare_minister | Aharon Abuhatzira | 5 August 1981 | 2 May 1982 | 4355 | [V] |
| labour_minister / welfare_minister | Menachem Begin | 2 May 1982 | 4 May 1982 | 22821 | [C] en.wiki 19th government: Abuhatzira until 2 May, Uzan from 3 May 1982; no Begin |
| labour_minister / welfare_minister | Aharon Uzan | 4 May 1982 | 10 October 1983 | 21263 | [V] |
| religions_minister | Yosef Burg | 5 August 1981 | 10 October 1983 | 4229 | [V] |
| trade_minister | Gideon Patt | 5 August 1981 | 10 October 1983 | 4269 | [V] |
| transportation_minister | Haim Corfu | 5 August 1981 | 10 October 1983 | 4225 | [V] |
| posts_minister (Communications) | Mordechai Tzipori | 5 August 1981 | 10 October 1983 | 3888 | [V] |
| education_minister | Zevulun Hammer | 5 August 1981 | 10 October 1983 | 4342 | [V] |
| absorption_minister (to add) | Aharon Abuhatzira | 5 August 1981 | 2 May 1982 | 4354 | [C] en.wiki Absorption list and 19th government: until 4 May 1982 |
| absorption_minister (to add) | Menachem Begin | 2 May 1982 | 4 May 1982 | 22819 | [C] not in en.wiki |
| absorption_minister (to add) | Aharon Uzan | 4 May 1982 | 10 October 1983 | 21264 | [V] |
| minister_without_portfolio | Yitzhak Moda'i | 5 August 1981 | 19 October 1982 | 4170 | [V] |
| minister_without_portfolio | Mordechai Ben-Porat | 5 July 1982 | 10 October 1983 | 4541 | [V] |
| minister_without_portfolio | Ariel Sharon | 14 February 1983 | 10 October 1983 | 4255 | [C] en.wiki 19th government: "from 14 February 1982" (a typo) |
| minister_without_portfolio | Sarah Doron | 5 July 1983 | 10 October 1983 | 4547 | [V] |
| [other] housing | David Levy | 5 August 1981 | 10 October 1983 | 4244 | [V] |
| [other] tourism | Avraham Sharir | 5 August 1981 | 10 October 1983 | 4352 | [C] en.wiki 19th government: Patt until 11 August 1981, Sharir from 1 August 1981 |
| [other] energy | Yitzhak Berman | 5 August 1981 | 30 September 1982 | 4128 | [V] |
| [other] energy | Menachem Begin | 30 September 1982 | 19 October 1982 | 22820 | [C] not in en.wiki |
| [other] energy | Yitzhak Moda'i | 19 October 1982 | 10 October 1983 | 4174 | [V] |
| [other] economy and planning | Ya'akov Meridor | 5 August 1981 | 10 October 1983 | 3904 | [V] |
| [other] science | Yuval Ne'eman | 26 July 1982 | 10 October 1983 | 3571 | [V] |

### Government 20

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Shamir | 10 October 1983 | 13 September 1984 | 4296, 22836 | [V] |
| deputy_prime_minister | David Levy | 10 October 1983 | 13 September 1984 | 4222, 22828 | [V] |
| defense_minister | Moshe Arens | 10 October 1983 | 13 September 1984 | 4537, 22822 | [V] |
| finance_minister | Yoram Aridor | 10 October 1983 | 15 October 1983 | 22838 | [V] |
| finance_minister | Yitzhak Shamir | 15 October 1983 | 18 October 1983 | 22839 | [C] en.wiki 20th government: Cohen-Orgad from 15 October 1983; en.wiki Finance list leaves 15-18 October empty |
| finance_minister | Yigal Cohen-Orgad | 18 October 1983 | 13 September 1984 | 4307, 22827 | [C] en.wiki 20th government: 15 October 1983; en.wiki Finance list: 18 October 1983 |
| foreign_minister | Yitzhak Shamir | 10 October 1983 | 13 September 1984 | 4272, 22835 | [V] |
| health_minister | Eliezer Shostak | 10 October 1983 | 13 September 1984 | 22813, 22814 | [V] |
| interior_minister | Yosef Burg | 10 October 1983 | 13 September 1984 | 22797, 22798 | [V] |
| justice_minister | Moshe Nissim | 10 October 1983 | 13 September 1984 | 22805, 22806 | [V] |
| agriculture_minister | Pesach Grupper | 10 October 1983 | 13 September 1984 | 4546, 22823 | [V] |
| labour_minister / welfare_minister | Aharon Uzan | 10 October 1983 | 13 September 1984 | 4199, 21283 | [V] |
| religions_minister | Yosef Burg | 10 October 1983 | 13 September 1984 | 22795, 22796 | [V] |
| trade_minister | Gideon Patt | 10 October 1983 | 13 September 1984 | 22807, 22808 | [V] |
| transportation_minister | Haim Corfu | 10 October 1983 | 13 September 1984 | 4442, 22811 | [V] |
| posts_minister (Communications) | Mordechai Tzipori | 10 October 1983 | 13 September 1984 | 22809, 22810 | [V] |
| education_minister | Zevulun Hammer | 10 October 1983 | 13 September 1984 | 4582, 22826 | [V] |
| absorption_minister (to add) | Aharon Uzan | 10 October 1983 | 13 September 1984 | 21282, 21281 | [V] |
| minister_without_portfolio | Ariel Sharon | 10 October 1983 | 13 September 1984 | 4526, 22837 | [V] |
| minister_without_portfolio | Mordechai Ben-Porat | 10 October 1983 | 31 January 1984 | 4542 | [V] |
| minister_without_portfolio | Sarah Doron | 10 October 1983 | 13 September 1984 | 4398, 22824 | [V] |
| [other] housing | David Levy | 10 October 1983 | 13 September 1984 | 22801, 22802 | [V] |
| [other] tourism | Avraham Sharir | 10 October 1983 | 13 September 1984 | 22815, 22816 | [V] |
| [other] energy | Yitzhak Moda'i | 10 October 1983 | 13 September 1984 | 4175, 22829 | [V] |
| [other] economy and planning | Ya'akov Meridor | 10 October 1983 | 13 September 1984 | 22803, 22804 | [V] |
| [other] science | Yuval Ne'eman | 10 October 1983 | 13 September 1984 | 3410, 22830 | [V] |

### Government 21

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Shimon Peres | 13 September 1984 | 20 October 1986 | 4317 | [V] |
| (new) acting PM, ממלא מקום ראש הממשלה | Yitzhak Shamir | 13 September 1984 | 20 October 1986 | 4248 | [V] |
| deputy_prime_minister | David Levy | 13 September 1984 | 20 October 1986 | 4223 | [V] |
| deputy_prime_minister | Yitzhak Navon | 13 September 1984 | 20 October 1986 | 4217 | [V] |
| defense_minister | Yitzhak Rabin | 13 September 1984 | 20 October 1986 | 4180 | [V] |
| finance_minister | Yitzhak Moda'i | 13 September 1984 | 16 April 1986 | 3474 | [V] |
| finance_minister | Moshe Nissim | 16 April 1986 | 20 October 1986 | 4207 | [V] |
| foreign_minister | Yitzhak Shamir | 13 September 1984 | 20 October 1986 | 4273 | [V] |
| health_minister | Mordechai Gur | 13 September 1984 | 20 October 1986 | 4578 | [V] |
| interior_minister | Shimon Peres | 13 September 1984 | 24 December 1984 | 4276 | [V] |
| interior_minister | Yitzhak Peretz | 24 December 1984 | 20 October 1986 | 4191 | [V] |
| justice_minister | Moshe Nissim | 13 September 1984 | 16 April 1986 | 4210 | [V] |
| justice_minister | Yitzhak Moda'i | 16 April 1986 | 28 July 1986 | 3475 | [C] en.wiki: until 23 July 1986 |
| justice_minister | Avraham Sharir | 28 July 1986 | 20 October 1986 | 22914 | [C] en.wiki: from 23 July 1986 |
| agriculture_minister | Aryeh Nehemkin | 13 September 1984 | 20 October 1986 | 4254 | [V] |
| labour_minister / welfare_minister | Moshe Katsav | 13 September 1984 | 20 October 1986 | 4213 | [V] |
| police_minister | Haim Bar-Lev | 13 September 1984 | 20 October 1986 | 4286 | [V] |
| religions_minister | Shimon Peres | 13 September 1984 | 24 December 1984 | 4321 | [C] en.wiki: until 23 December 1984 |
| religions_minister | Yosef Burg | 24 December 1984 | 7 October 1986 | 4230 | [C] en.wiki: 23 December 1984 - 5 October 1986 |
| religions_minister | Zevulun Hammer | 7 October 1986 | 20 October 1986 | 22921 | [C] en.wiki: from 5 October 1986 |
| trade_minister | Ariel Sharon | 13 September 1984 | 20 October 1986 | 4527 | [V] |
| transportation_minister | Haim Corfu | 13 September 1984 | 20 October 1986 | 4226 | [V] |
| posts_minister (Communications) | Amnon Rubinstein | 13 September 1984 | 20 October 1986 | 4253 | [V] |
| education_minister | Yitzhak Navon | 13 September 1984 | 20 October 1986 | 4243 | [V] |
| absorption_minister (to add) | Ya'akov Tzur | 13 September 1984 | 20 October 1986 | 4142 | [V] |
| minister_without_portfolio | Moshe Arens | 13 September 1984 | 20 October 1986 | 4195 | [V] |
| minister_without_portfolio | Yosef Burg | 13 September 1984 | 24 December 1984 | 22883 | [C] en.wiki: until 23 December 1984 |
| minister_without_portfolio | Yigal Hurvitz | 13 September 1984 | 20 October 1986 | 4580 | [V] |
| minister_without_portfolio | Ezer Weizman | 13 September 1984 | 20 October 1986 | 4166 | [V] |
| minister_without_portfolio | Yitzhak Peretz | 13 September 1984 | 18 December 1984 | 4188 | [V] |
| minister_without_portfolio | Yosef Shapira | 13 September 1984 | 20 October 1986 | 4291 | [V] |
| [other] housing | David Levy | 13 September 1984 | 20 October 1986 | 4245 | [V] |
| [other] tourism | Avraham Sharir | 13 September 1984 | 20 October 1986 | 4353 | [V] |
| [other] energy | Moshe Shahal | 13 September 1984 | 20 October 1986 | 4161 | [V] |
| [other] economy and planning | Gad Yaacobi | 13 September 1984 | 20 October 1986 | 4261 | [V] |
| [other] science | Gideon Patt | 13 September 1984 | 20 October 1986 | 22931 | [V] |

### Government 22

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Shamir | 20 October 1986 | 22 December 1988 | 4297, 22936 | [V] |
| (new) acting PM, ממלא מקום ראש הממשלה | Shimon Peres | 20 October 1986 | 22 December 1988 | 4278, 22929 | [V] |
| deputy_prime_minister | David Levy | 20 October 1986 | 22 December 1988 | 22892, 22893 | [V] |
| deputy_prime_minister | Yitzhak Navon | 20 October 1986 | 22 December 1988 | 22896, 22897 | [V] |
| defense_minister | Yitzhak Rabin | 20 October 1986 | 22 December 1988 | 4181, 22935 | [V] |
| finance_minister | Moshe Nissim | 20 October 1986 | 22 December 1988 | 4208, 22926 | [V] |
| foreign_minister | Shimon Peres | 20 October 1986 | 22 December 1988 | 4274, 22928 | [C] en.wiki Foreign Affairs list: until 23 December 1988 |
| health_minister | Shoshana Arbeli-Almozlino | 20 October 1986 | 22 December 1988 | 4397, 22916 | [V] |
| interior_minister | Yitzhak Peretz | 20 October 1986 | 6 January 1987 | 4518 | [V] |
| interior_minister | Yitzhak Shamir | 6 January 1987 | 22 December 1988 | 22937, 22938 | [C] not in en.wiki 22nd government table (Peretz "until 6 January 1987", no successor) |
| justice_minister | Avraham Sharir | 20 October 1986 | 22 December 1988 | 4351, 22939 | [V] |
| agriculture_minister | Aryeh Nehemkin | 20 October 1986 | 22 December 1988 | 22898, 22899 | [V] |
| labour_minister / welfare_minister | Moshe Katsav | 20 October 1986 | 22 December 1988 | 4214, 22934 | [V] |
| police_minister | Haim Bar-Lev | 20 October 1986 | 22 December 1988 | 4573, 22918 | [V] |
| religions_minister | Zevulun Hammer | 20 October 1986 | 22 December 1988 | 4583, 22922 | [V] |
| trade_minister | Ariel Sharon | 20 October 1986 | 22 December 1988 | 22910, 22911 | [V] |
| transportation_minister | Haim Corfu | 20 October 1986 | 22 December 1988 | 22933, 4443 | [V] |
| posts_minister (Communications) | Amnon Rubinstein | 20 October 1986 | 26 May 1987 | 22902 | [V] |
| posts_minister (Communications) | Yitzhak Shamir | 26 May 1987 | 8 June 1987 | 22903 | [C] not in en.wiki |
| posts_minister (Communications) | Gad Yaacobi | 8 June 1987 | 22 December 1988 | 22904, 22905 | [C] en.wiki: from 9 June 1987 |
| education_minister | Yitzhak Navon | 20 October 1986 | 22 December 1988 | 22894, 22895 | [V] |
| absorption_minister (to add) | Ya'akov Tzur | 20 October 1986 | 22 December 1988 | 22900, 22901 | [V] |
| minister_without_portfolio | Moshe Arens | 20 October 1986 | 4 September 1987 | 4196 | [C] en.wiki: one term "4 September 1987 - 18 April 1988", the reverse of the Knesset rows |
| minister_without_portfolio | Yigal Hurvitz | 20 October 1986 | 22 December 1988 | 22884, 22885 | [V] |
| minister_without_portfolio | Ezer Weizman | 20 October 1986 | 22 December 1988 | 22886, 22887 | [V] |
| minister_without_portfolio | Yitzhak Moda'i | 20 October 1986 | 22 December 1988 | 4171, 22924 | [V] |
| minister_without_portfolio | Yosef Shapira | 20 October 1986 | 22 December 1988 | 22908, 22909 | [V] |
| minister_without_portfolio | Yitzhak Peretz | 25 May 1987 | 22 December 1988 | 4189, 22930 | [V] |
| minister_without_portfolio | Moshe Arens | 18 April 1988 | 22 December 1988 | 4197, 22917 | [C] see the row above |
| minister_without_portfolio | Mordechai Gur | 18 April 1988 | 22 December 1988 | 4192, 22919 | [C] en.wiki: from 18 April 1987 |
| [other] housing | David Levy | 20 October 1986 | 22 December 1988 | 22890, 22891 | [V] |
| [other] tourism | Avraham Sharir | 20 October 1986 | 22 December 1988 | 22912, 22913 | [V] |
| [other] energy | Moshe Shahal | 20 October 1986 | 22 December 1988 | 22906, 22907 | [V] |
| [other] economy and planning | Gad Yaacobi | 20 October 1986 | 22 December 1988 | 22888, 22889 | [V] |
| [other] science | Gideon Patt | 20 October 1986 | 22 December 1988 | 4265, 22932 | [V] |

### Government 23

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Shamir | 22 December 1988 | 11 June 1990 | 4298 | [V] |
| (new) acting PM, ממלא מקום ראש הממשלה | Shimon Peres | 22 December 1988 | 15 March 1990 | 4279 | [V] |
| deputy_prime_minister | David Levy | 22 December 1988 | 11 June 1990 | 4224 | [V] |
| deputy_prime_minister | Yitzhak Navon | 22 December 1988 | 15 March 1990 | 4187 | [V] |
| defense_minister | Yitzhak Rabin | 22 December 1988 | 15 March 1990 | 4182 | [V] |
| defense_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22945 | [V] |
| finance_minister | Shimon Peres | 22 December 1988 | 15 March 1990 | 4299 | [V] |
| finance_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22944 | [V] |
| foreign_minister | Moshe Arens | 22 December 1988 | 11 June 1990 | 22948 | [C] en.wiki Foreign Affairs list: 23 December 1988 - 12 June 1990 |
| health_minister | Ya'akov Tzur | 22 December 1988 | 15 March 1990 | 4145 | [V] |
| health_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22946 | [C] not in en.wiki 23rd government table |
| interior_minister | Aryeh Deri | 22 December 1988 | 11 June 1990 | 4075 | [V] |
| justice_minister | Dan Meridor | 22 December 1988 | 11 June 1990 | 4334 | [V] |
| agriculture_minister | Avraham Katz-Oz | 22 December 1988 | 15 March 1990 | 4347 | [V] |
| agriculture_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22949 | [C] not in en.wiki 23rd government table |
| labour_minister / welfare_minister | Yitzhak Shamir | 22 December 1988 | 7 March 1990 | 4524 | [V] |
| labour_minister / welfare_minister | Roni Milo | 7 March 1990 | 11 June 1990 | 22956 | [V] |
| police_minister | Haim Bar-Lev | 22 December 1988 | 15 March 1990 | 4574 | [V] |
| police_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22950 | [C] not in en.wiki Public Security list (Bar-Lev to 15 March, Milo from 11 June) |
| religions_minister | Yitzhak Shamir | 22 December 1988 | 27 December 1988 | 22947 | [C] not in en.wiki (Hammer for the whole government) |
| religions_minister | Zevulun Hammer | 27 December 1988 | 11 June 1990 | 4340 | [V] |
| trade_minister | Ariel Sharon | 22 December 1988 | 20 February 1990 | 4259 | [V] |
| trade_minister | Yitzhak Shamir | 20 February 1990 | 7 March 1990 | 22952 | [C] not in en.wiki |
| trade_minister | Moshe Nissim | 7 March 1990 | 11 June 1990 | 22953 | [C] en.wiki: from 20 February 1990 |
| transportation_minister | Moshe Katsav | 22 December 1988 | 11 June 1990 | 4215 | [V] |
| posts_minister (Communications) | Gad Yaacobi | 22 December 1988 | 15 March 1990 | 4263 | [V] |
| posts_minister (Communications) | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22954 | [C] not in en.wiki 23rd government table |
| education_minister | Yitzhak Navon | 22 December 1988 | 15 March 1990 | 4216 | [V] |
| education_minister | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22951 | [C] not in en.wiki 23rd government table |
| absorption_minister (to add) | Yitzhak Peretz | 22 December 1988 | 11 June 1990 | 4190 | [V] |
| minister_without_portfolio | Ehud Olmert | 22 December 1988 | 11 June 1990 | 4356 | [V] |
| minister_without_portfolio | Rafael Edri | 22 December 1988 | 7 March 1990 | 4383 | [V] |
| minister_without_portfolio | Mordechai Gur | 22 December 1988 | 15 March 1990 | 4193 | [V] |
| minister_without_portfolio | Moshe Nissim | 22 December 1988 | 7 March 1990 | 4206 | [V] |
| minister_without_portfolio | Avner-Hai Shaki | 27 December 1988 | 11 June 1990 | 4591 | [V] |
| minister_without_portfolio | David Magen | 7 March 1990 | 11 June 1990 | 4330 | [V] |
| [other] housing | David Levy | 22 December 1988 | 11 June 1990 | 4218 | [V] |
| [other] tourism | Gideon Patt | 22 December 1988 | 11 June 1990 | 4267 | [V] |
| [other] energy | Moshe Shahal | 22 December 1988 | 15 March 1990 | 4162 | [V] |
| [other] energy | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22955 | [V] |
| [other] economy and planning | Yitzhak Moda'i | 22 December 1988 | 11 June 1990 | 4176 | [V] |
| [other] science | Ezer Weizman | 22 December 1988 | 15 March 1990 | 4168 | [V] |
| [other] science | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22959 | [V] |
| [other] environment | Roni Milo | 22 December 1988 | 7 March 1990 | 4379 | [V] |
| [other] environment | Rafael Edri | 7 March 1990 | 15 March 1990 | 4530 | [V] |
| [other] environment | Yitzhak Shamir | 15 March 1990 | 11 June 1990 | 22957 | [V] |

### Government 24

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Shamir | 11 June 1990 | 13 July 1992 | 4525 | [V] |
| deputy_prime_minister | David Levy | 11 June 1990 | 13 July 1992 | 22967 | [V] |
| deputy_prime_minister | Moshe Nissim | 11 June 1990 | 13 July 1992 | 4212 | [V] |
| defense_minister | Moshe Arens | 11 June 1990 | 13 July 1992 | 4198 | [V] |
| finance_minister | Yitzhak Moda'i | 11 June 1990 | 13 July 1992 | 4172 | [V] |
| foreign_minister | David Levy | 11 June 1990 | 13 July 1992 | 4219 | [C] en.wiki Foreign Affairs list: from 13 June 1990 (and Arens to 12 June) |
| health_minister | Ehud Olmert | 11 June 1990 | 13 July 1992 | 4357 | [V] |
| interior_minister | Aryeh Deri | 11 June 1990 | 13 July 1992 | 22966 | [V] |
| justice_minister | Dan Meridor | 11 June 1990 | 13 July 1992 | 3364 | [V] |
| agriculture_minister | Rafael Eitan | 11 June 1990 | 31 December 1991 | 4384 | [V] |
| agriculture_minister | Yitzhak Shamir | 31 December 1991 | 13 July 1992 | 22969 | [C] not in en.wiki 24th government table |
| labour_minister / welfare_minister | Yitzhak Shamir | 11 June 1990 | 13 July 1992 | 4246 | [V] |
| police_minister | Roni Milo | 11 June 1990 | 13 July 1992 | 4377 | [V] |
| religions_minister | Avner-Hai Shaki | 11 June 1990 | 13 July 1992 | 4350 | [V] |
| trade_minister | Moshe Nissim | 11 June 1990 | 13 July 1992 | 4211 | [V] |
| transportation_minister | Moshe Katsav | 11 June 1990 | 13 July 1992 | 4185 | [V] |
| posts_minister (Communications) | Rafael Pinhasi | 11 June 1990 | 13 July 1992 | 4388 | [V] |
| education_minister | Zevulun Hammer | 11 June 1990 | 13 July 1992 | 4343 | [V] |
| absorption_minister (to add) | Yitzhak Peretz | 11 June 1990 | 13 July 1992 | 4519 | [V] |
| minister_without_portfolio | Rehavam Ze'evi | 5 February 1991 | 21 January 1992 | 4102 | [C] en.wiki: until 12 January 1992 |
| [other] housing | Ariel Sharon | 11 June 1990 | 13 July 1992 | 4257 | [V] |
| [other] tourism | Gideon Patt | 11 June 1990 | 13 July 1992 | 22968 | [V] |
| [other] energy | Yuval Ne'eman | 11 June 1990 | 21 January 1992 | 4312 | [V] |
| [other] energy | Yitzhak Shamir | 21 January 1992 | 13 July 1992 | 22970 | [V] |
| [other] economy and planning | David Magen | 11 June 1990 | 13 July 1992 | 4331 | [V] |
| [other] science | Yuval Ne'eman | 11 June 1990 | 21 January 1992 | 4313 | [C] en.wiki: "until 21 January 1990" (a typo) |
| [other] science | Yitzhak Shamir | 21 January 1992 | 13 July 1992 | 22971 | [V] |
| [other] environment | Yitzhak Shamir | 11 June 1990 | 13 July 1992 | 4247 | [V] |
| [other] Jerusalem affairs | Yitzhak Shamir | 26 November 1990 | 13 July 1992 | 4505 | [C] en.wiki: from 27 November 1990 |

### Government 25

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Rabin | 13 July 1992 | 4 November 1995 | 4178 | [V] ends after this era |
| defense_minister | Yitzhak Rabin | 13 July 1992 | 4 November 1995 | 4183 | [V] |
| finance_minister | Avraham Shochat | 13 July 1992 | 22 November 1995 | 4146 | [V] |
| foreign_minister | Shimon Peres | 13 July 1992 | 22 November 1995 | 4275 | [C] en.wiki Foreign Affairs list: from 14 July 1992 |
| health_minister | Haim Ramon | 13 July 1992 | 8 February 1994 | 4304 | [V] |
| interior_minister | Aryeh Deri | 13 July 1992 | 19 May 1993 | 4076 | [C] en.wiki: until 11 May 1993 |
| interior_minister | Yitzhak Rabin | 19 May 1993 | 31 May 1993 | 22992 | [C] not in en.wiki |
| interior_minister | Aryeh Deri | 31 May 1993 | 14 September 1993 | 4077 | [C] en.wiki: 7 June - 14 September 1993. Ends after the game's last day (13 September 1993) |
| justice_minister | David Libai | 13 July 1992 | 22 November 1995 | 4327 | [V] |
| agriculture_minister | Ya'akov Tzur | 13 July 1992 | 22 November 1995 | 4143 | [V] |
| labour_minister / welfare_minister | Yitzhak Rabin | 13 July 1992 | 31 December 1992 | 4184 | [V] |
| labour_minister / welfare_minister | Ora Namir | 31 December 1992 | 22 November 1995 | 4358 | [V] |
| police_minister | Moshe Shahal | 13 July 1992 | 22 November 1995 | 4164 | [V] |
| religions_minister | Yitzhak Rabin | 13 July 1992 | 27 February 1995 | 4179 | [C] en.wiki: until 27 January 1995 (after this era) |
| trade_minister | Michael Harish | 13 July 1992 | 22 November 1995 | 4367 | [V] |
| transportation_minister | Yisrael Kessar | 13 July 1992 | 22 November 1995 | 4315 | [V] |
| posts_minister (Communications) | Moshe Shahal | 13 July 1992 | 31 May 1993 | 4165 | [C] en.wiki: until 7 June 1993 |
| posts_minister (Communications) | Shulamit Aloni | 31 May 1993 | 22 November 1995 | 4532 | [V] |
| education_minister | Shulamit Aloni | 13 July 1992 | 19 May 1993 | 4393 | [C] en.wiki: until 11 May 1993 |
| education_minister | Yitzhak Rabin | 19 May 1993 | 31 May 1993 | 22974 | [C] en.wiki: 11 May - 7 June 1993 |
| education_minister | Amnon Rubinstein | 31 May 1993 | 22 November 1995 | 4250 | [C] en.wiki: "from 30 May 1994" |
| absorption_minister (to add) | Yair Tzaban | 13 July 1992 | 22 November 1995 | 4305 | [V] |
| minister_without_portfolio | Shulamit Aloni | 19 May 1993 | 31 May 1993 | 4531 | [C] en.wiki: 11 May - 7 June 1993 |
| minister_without_portfolio | Aryeh Deri | 19 May 1993 | 31 May 1993 | 4074 | [C] en.wiki: 11 May - 7 June 1993 |
| [other] housing | Binyamin Ben-Eliezer | 13 July 1992 | 22 November 1995 | 4346 | [V] |
| [other] tourism | Uzi Baram | 13 July 1992 | 22 November 1995 | 4372 | [V] |
| [other] energy | Amnon Rubinstein | 13 July 1992 | 31 May 1993 | 4249 | [C] en.wiki: until 7 June 1993 |
| [other] energy | Moshe Shahal | 31 May 1993 | 9 January 1995 | 4163 | [C] en.wiki: from 7 June 1993 |
| [other] economy and planning | Shimon Shetreet | 13 July 1992 | 18 July 1995 | 4124 | [V] |
| [other] science | Amnon Rubinstein | 13 July 1992 | 31 December 1992 | 4252 | [V] |
| [other] science | Shimon Shetreet | 31 December 1992 | 31 May 1993 | 4521 | [C] en.wiki: until 7 June 1993 |
| [other] science | Shulamit Aloni | 31 May 1993 | 22 November 1995 | 4394 | [V] |
| [other] environment | Ora Namir | 13 July 1992 | 31 December 1992 | 4360 | [V] |
| [other] environment | Yossi Sarid | 31 December 1992 | 22 November 1995 | 4528 | [V] |
| [other] Jerusalem affairs | Yitzhak Rabin | 13 July 1992 | 13 December 1992 | 22993 | [C] en.wiki: until 31 December 1992 |


Notes on the Knesset table itself:

- Row 21260 (Benjamin Netanyahu, acting Minister of Education, June 2019) is filed under government 21. It is a data
  error and is left out.
- Row 3944 starts Yadin as Deputy Prime Minister on 24 October 1978. The plenum record of 24 October 1977 and every
  other Democratic Movement for Change row give 24 October 1977; use 1977.
- The Ministry of Police has no rows from 20 June 1977 to 13 September 1984. en.wiki's National Security page says
  the post was abolished in 1977 and restored in 1984 [S]; the Knesset table agrees by having no holder.

## 3. Wikipedia against the primary sources

Every row marked [C] in §1 and §2, in one list. "K" is the Knesset position table, "P" the plenum record.

1. Begin's start as Prime Minister and the 18th government: K and P 20 June 1977 (the sitting began that afternoon
   and ran past midnight); en.wiki list and infobox 21 June 1977. The design (§2.1, §9.1) already takes 20 June;
   the sources support it, with the caveat that the vote may have fallen after midnight.
2. 19th government's end: K and P 10 October 1983; en.wiki infobox 28 August 1983.
3. Yadin, Deputy Prime Minister: P and en.wiki 24 October 1977; K row 3944 24 October 1978 (K error).
4. Erlich, Deputy Prime Minister in the 18th government: K from 7 November 1979; en.wiki for the whole government.
5. Erlich, Finance: K and en.wiki Finance list to 7 November 1979; en.wiki 18th government "7/11/1977".
6. Finance, 13-21 January 1981: K Begin; en.wiki empty.
7. Communications, 18th government: K Moda'i to 28 December 1980, then Begin to 5 January 1981; en.wiki Moda'i to
   22 December 1980, no Begin. K Begin 15 September 1978 - 15 January 1979; en.wiki empty.
8. Transportation, 15 September 1978 - 15 January 1979: K Begin; en.wiki empty.
9. Justice, 5-13 August 1980: K Begin; en.wiki empty.
10. Industry and Trade: K Hurvitz to 15 January 1979; en.wiki 18th government 1 October 1978.
11. Housing: K Patt from 21 June 1977 (the only such row); he.wiki 20 June 1977.
12. Labour and Welfare and Absorption, May 1982: K Abuhatzira to 2 May, Begin 2-4 May, Uzan from 4 May; en.wiki
    Absorption list Abuhatzira to 4 May; en.wiki 19th government Labour: Uzan from 3 May, no Begin.
13. Sharon without portfolio: K from 14 February 1983; en.wiki "14 February 1982".
14. Energy, 30 September - 19 October 1982: K Begin; en.wiki empty.
15. Tourism, August 1981: K Sharir from 5 August 1981; en.wiki Patt to 11 August and Sharir from 1 August.
16. Finance, October 1983: K Aridor to 15 October, Shamir 15-18 October, Cohen-Orgad from 18 October; en.wiki 20th
    government Cohen-Orgad from 15 October; en.wiki Finance list 18 October with 15-18 October empty.
17. Religious Affairs, 21st government: K Peres to 24 December 1984, Burg 24 December 1984 - 7 October 1986, Hammer
    from 7 October 1986; en.wiki 23 December 1984 and 5 October 1986. Burg without portfolio: K to 24 December, en.wiki
    23 December 1984.
18. Justice, 1986: K Moda'i 16 April - 28 July 1986, Sharir from 28 July; en.wiki 23 July.
19. Interior from 6 January 1987: K Shamir; en.wiki empty.
20. Communications, 1987: K Rubinstein to 26 May, Shamir 26 May - 8 June, Yaacobi from 8 June; en.wiki Yaacobi from
    9 June, no Shamir.
21. Ministers without portfolio, 22nd government: K Arens 20 October 1986 - 4 September 1987 and from 18 April 1988;
    en.wiki 4 September 1987 - 18 April 1988 (reversed). K Gur from 18 April 1988; en.wiki 18 April 1987.
22. Foreign Affairs, December 1988: K Peres to 22 December, Arens from 22 December; en.wiki list 23 December.
23. Religious Affairs, 22-27 December 1988: K Shamir, Hammer from 27 December; en.wiki Hammer throughout.
24. Industry and Trade, 1990: K Sharon to 20 February, Shamir 20 February - 7 March, Nissim from 7 March; en.wiki
    Nissim from 20 February.
25. 15 March - 11 June 1990: K Shamir holds Police, Health, Agriculture, Communications, Education, Energy, Science
    and Environment as well as Defence and Finance; en.wiki lists him only for Defence and Finance.
26. Foreign Affairs, June 1990: K Arens to 11 June, Levy from 11 June; en.wiki list Arens to 12 June, Levy from 13
    June. On en.wiki's dates the office is empty for a day; on the Knesset's it is not.
27. Ze'evi without portfolio: K to 21 January 1992; en.wiki 12 January 1992.
28. Agriculture from 31 December 1991: K Shamir; en.wiki empty.
29. Science, 24th government: K Ne'eman to 21 January 1992; en.wiki "21 January 1990".
30. Jerusalem Affairs: K Shamir from 26 November 1990; en.wiki 27 November 1990. K Rabin to 13 December 1992;
    en.wiki 31 December 1992.
31. Foreign Affairs, July 1992: K Peres from 13 July; en.wiki list 14 July (a one-day gap on en.wiki's dates).
32. The May 1993 reshuffle: K Deri (Interior) and Aloni (Education) leave on 19 May 1993, Rabin holds both 19-31 May,
    Deri and Aloni are ministers without portfolio 19-31 May, and the new posts start 31 May (Deri Interior, Aloni
    Communications and Science, Rubinstein Education, Shahal Energy). en.wiki gives 11 May and 7 June throughout, and
    "30 May 1994" for Rubinstein.
33. Religious Affairs, 25th government: K Rabin to 27 February 1995; en.wiki 27 January 1995 (after this era).
34. Chief of Staff, Aman and Mossad: no primary source reached; see §4 for where en.wiki and he.wiki disagree.

Of these, 1, 2, 22, 26 and 31 touch the design's "no one-day vacancy" test (#145 Done-when). On the Knesset dates
none of the 1988, 1990 and 1992 changes leaves a vacancy.

## 4. Non-ministerial offices

| Office id | Holder | From | To | Source | Mark |
|---|---|---|---|---|---|
| president | Ephraim Katzir | 24 May 1973 | 29 May 1978 | P 29 May 1978 (end of term, Navon's declaration); en.wiki list | [V] end; start [S] |
| president | Yitzhak Navon | 29 May 1978 | 5 May 1983 | P 19 April 1978: elected, 86 votes, 23 blank; P 29 May 1978: declaration | [V] |
| president | Chaim Herzog | 5 May 1983 | 13 May 1993 | P 22 March 1983: elected 61 to 57 (Menachem Elon), 2 blank; the Speaker set the declaration for 5 May 1983; P 5 May 1983 | [V] |
| president | Ezer Weizman | 13 May 1993 | (after this era) | P 24 March 1993: elected 66 to 53 (Dov Shilansky), 1 blank; P 13 May 1993: declaration | [V] |
| chief_of_staff | Mordechai Gur | 16 April 1974 | 16 April 1978 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Rafael Eitan | 16 April 1978 | 19 April 1983 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Moshe Levy | 19 April 1983 | 19 April 1987 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Dan Shomron | 19 April 1987 | 1 April 1991 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Ehud Barak | 1 April 1991 | 1 January 1995 | en.wiki and he.wiki agree | [S] |
| intelligence_chief (Aman) | Shlomo Gazit | 1974 (he.wiki April 1974) | 1978 (en.wiki) / February 1979 (he.wiki) | | [C] [S] |
| intelligence_chief (Aman) | Yehoshua Sagi | 1979 (he.wiki February 1979) | 1983 (he.wiki March 1983) | | [S] |
| intelligence_chief (Aman) | Ehud Barak | 1983 (he.wiki May 1983) | 1985 (en.wiki) / January 1986 (he.wiki) | | [C] [S] |
| intelligence_chief (Aman) | Amnon Lipkin-Shahak | 1986 (he.wiki January 1986) | 1991 (he.wiki March 1991) | | [S] |
| intelligence_chief (Aman) | Uri Sagi | 1991 (he.wiki March 1991) | 1995 (he.wiki June 1995) | | [S] |
| mossad_director | Yitzhak Hofi | 1973 (en.wiki) / 1 September 1974 (he.wiki) | 12 September 1982 (he.wiki; en.wiki 1982) | | [C] [S] |
| mossad_director | Nahum Admoni | 12 September 1982 (he.wiki) | 19 April 1989 (he.wiki; en.wiki 1989) | | [S] |
| mossad_director | Shabtai Shavit | 19 April 1989 (he.wiki) | 1996 | | [S] |

For Aman, en.wiki gives Sagi 1979 after Gazit to 1978, which leaves 1978-79 open; he.wiki's months close the gap.
The Aman and Mossad days stay unverified until an IDF or Mossad page, or a contemporary report, is read. The Chief of
Staff days have no primary source either; the Knesset table does not cover the post.

## 5. Mapping this note into content later

No content is changed here. When item 7 or 8 writes the era:

- **`governments.yaml`**: one record per row of §1. Ids in the existing style (`eighteenth_government` ...
  `twenty_fifth_government`); `from` and `to` from §1; sources: the Knesset query for that `GovernmentNum` and the
  plenum file of the formation day. The 23rd government needs a `coalition_changes` entry dated 15 March 1990 (the
  Alignment leaves). Coalition lists need item 2's party ids and a primary check (§6).
- **`people.yaml`**: one term per row of §2, split per government as today. `sources` per term: the Knesset row URL(s)
  in the row, plus the government's plenum file when the term starts on a formation day. Where a row is [C], use the
  Knesset value and put the other value in a YAML comment, as Ben-Gurion's 1953 terms do; for Yadin use 24 October 1977
  (plenum) and say why. People with an existing id (`burg`, `dayan`, `yadin`) get new terms, not new records; reuse any
  id the 1957-1977 research (#113) adds for Peres, Rabin, Allon and others. Suggested ids for new people are in §7;
  they avoid the existing `namir` (Mordechai Namir), `tzur` (Ze'ev Tzur), `shapira` (Haim-Moshe Shapira), `levin`,
  `rosen` and `weizmann`.
- **`offices.yaml`** questions for the maintainer (each needs a decision before encoding):
  - `absorption_minister` is not in `offices.yaml` yet; #113 adds it from 1 July 1968. Its 1977-1993 holders are in §2.
  - Labour and Welfare were merged on 24 October 1977 (plenum). Options: (a) give each merged-ministry term to both
    `labour_minister` and `welfare_minister`; (b) close `welfare_minister` with `until: 1977-10-24` and keep one office.
    (a) keeps every effect that looks up either office working; (b) is closer to the record.
  - `police_minister` needs a window: no holder from 20 June 1977 to 13 September 1984. Either `until: 1977-06-20`
    plus a second record from `1984-09-13`, or one office with a gap, which the schema may not allow.
  - `posts_minister` is the Ministry of Communications throughout this era; the title may want an era-dated rename.
  - `deputy_prime_minister` needs `several_holders: true`: two people held it at once in the 18th, 19th, 21st, 22nd, 23rd
    and 24th governments.
  - The Acting Prime Minister (ממלא מקום ראש הממשלה, from Basic Law: The Government (Amendment No. 5), 13 September
    1984) is a new historical-only title; a proposed id is `acting_prime_minister`.
  - Housing, Tourism, Energy, Economy and Planning, Science, Environment (from 22 December 1988) and Jerusalem Affairs
    (from 26 November 1990) have no office id. They are listed under "[other]" so a later item can decide whether any
    is needed.
  - Agriculture is the "Ministry of Agriculture and Rural Development" in some rows; the office id does not change.
- **`living`**: §7 gives a value for every person in §2 and §4. `false` comes from a death date in Wikidata [S]; the
  existing rule in `people.yaml` is to confirm each against the en.wiki infobox before setting it. `true` means no
  death date was found on 13 October 2026; confirm each before encoding, because a recent death may not be in
  Wikidata yet.

## 6. Coalition parties (en.wiki infoboxes, not checked) [S]

| Government | Parties as en.wiki lists them |
|---|---|
| 18th | Likud; Dash (24 October 1977 - 14 September 1978); Democratic Movement (from 14 September 1978); National Religious Party; Agudat Yisrael |
| 19th | Likud; National Religious Party; Telem; Tami; Tehiya; Movement for the Renewal of Social Zionism; Agudat Yisrael |
| 20th | Likud; National Religious Party; Tami; Tehiya; Movement for the Renewal of Social Zionism; Agudat Yisrael |
| 21st | Alignment; Likud; Shinui; Shas; National Religious Party; Ometz; Yahad; Agudat Yisrael; Morasha |
| 22nd | Likud; Alignment; Shinui (until 26 May 1987); Shas; National Religious Party; Ometz; Agudat Yisrael |
| 23rd | Likud; Alignment (until 15 March 1990); Shas; National Religious Party; Agudat Yisrael; Degel HaTorah |
| 24th | Likud; Tzomet (until 31 December 1991); Shas; National Religious Party; Moledet (until 21 January 1992); Agudat Yisrael; Unity for Peace and Immigration; New Liberal Party; Geulat Yisrael; Degel HaTorah; Tehiya (until 21 January 1992); one Alignment member |
| 25th | Labor; Meretz; Shas (until 1993); Yiud (from 1995) |

The Knesset table's `FactionID` is empty on government rows, so it cannot confirm these. The plenum statements on
each formation day name the parties and are the place to check them.

## 7. People and `living`

Every person who holds an office in §2 or §4. Born and died from Wikidata [S]. "—" in the PersonID column: not in KNS_Person. "(exists)" marks an id already in
`people.yaml`.

| Suggested id | Name (en.wiki title) | Role in this note | Knesset PersonID | Born | Died | living | Wikidata |
|---|---|---|---|---|---|---|---|
| abuhatzira | Aharon Abuhatzira | minister | 2119 | 1938-10-28 | 2021-09-21 | false | Q2910680 |
| admoni | Nahum Admoni | Mossad | — | 1929-11-21 | none found | true (no death date found; confirm) | Q1367853 |
| aloni | Shulamit Aloni | minister | 2143 | 1928-12-27 | 2014-01-24 | false | Q267335 |
| amit | Meir Amit | minister | 2527 | 1921-03-17 | 2009-07-17 | false | Q449095 |
| arbeli_almozlino | Shoshana Arbeli-Almozlino | minister | 2162 | 1926-01-26 | 2015-06-12 | false | Q2777751 |
| arens | Moshe Arens | minister | 463 | 1925-12-27 | 2019-01-07 | false | Q548185 |
| aridor | Yoram Aridor | minister | 2168 | 1933-10-24 | none found | true (no death date found; confirm) | Q434195 |
| bar_lev | Haim Bar-Lev | minister | 2216 | 1924-11-16 | 1994-05-07 | false | Q467150 |
| barak | Ehud Barak | Aman, Chief of Staff | 2226 | 1942-02-12 | none found | true (no death date found; confirm) | Q125731 |
| baram | Uzi Baram | minister | 861 | 1937-04-06 | none found | true (no death date found; confirm) | Q2909091 |
| begin | Menachem Begin | minister | 2179 | 1913-08-16 | 1992-03-09 | false | Q130873 |
| ben_eliezer | Binyamin Ben-Eliezer | minister | 435 | 1936-02-12 | 2016-08-28 | false | Q128888 |
| ben_porat | Mordechai Ben-Porat | minister | 2206 | 1923-09-12 | 2022-01-03 | false | Q2911774 |
| berman | Yitzhak Berman | minister | 2222 | 1913-06-03 | 2013-08-04 | false | Q983112 |
| burg (exists) | Yosef Burg | minister | 2187 | 1909-01-31 | 1999-10-15 | false | Q216021 |
| chaim_herzog | Chaim Herzog | President | — | 1918-09-17 | 1997-04-17 | false | Q295141 |
| cohen_orgad | Yigal Cohen-Orgad | minister | 2399 | 1937-08-30 | 2019-08-27 | false | Q504126 |
| corfu | Haim Corfu | minister | 2581 | 1921-01-06 | 2015-02-23 | false | Q2906138 |
| dan_meridor | Dan Meridor | minister | 546 | 1947-04-23 | none found | true (no death date found; confirm) | Q1159122 |
| david_levy | David Levy (Israeli politician) | minister | 536 | 1937-12-21 | 2024-06-02 | false | Q1175220 |
| dayan (exists) | Moshe Dayan | minister | 2277 | 1915-05-20 | 1981-10-16 | false | Q188783 |
| deri | Aryeh Deri | minister | 2291 | 1959-02-17 | none found | true (no death date found; confirm) | Q2670985 |
| doron | Sarah Doron | minister | 2273 | 1922-06-20 | 2010-11-03 | false | Q2778006 |
| edri | Rafael Edri | minister | 2128 | 1937-09-10 | 2024-06-22 | false | Q2905752 |
| erlich | Simha Erlich | minister | 2169 | 1915-12-15 | 1983-06-19 | false | Q361315 |
| ezer_weizman | Ezer Weizman | minister; President from 13 May 1993 | 2320 | 1924-06-15 | 2005-04-24 | false | Q128911 |
| gazit | Shlomo Gazit | Aman | — | 1926-10-22 | 2020-10-08 | false | Q2902267 |
| grupper | Pesah Grupper | minister | 2262 | 1924-08-21 | 2013-04-29 | false | Q2904677 |
| hammer | Zevulun Hammer | minister | 2300 | 1936-05-31 | 1998-01-20 | false | Q2910400 |
| harish | Michael Harish | minister | 2357 | 1936-11-28 | none found | true (no death date found; confirm) | Q2778335 |
| hofi | Yitzhak Hofi | Mossad | — | 1927-01-25 | 2014-09-15 | false | Q946803 |
| hurvitz | Yigal Hurvitz | minister | 2294 | 1918-10-15 | 1994-01-10 | false | Q434182 |
| katsav | Moshe Katsav | minister | 862 | 1945-12-05 | none found | true (no death date found; confirm) | Q181832 |
| katz_oz | Avraham Katz-Oz | minister | 2413 | 1935-01-01 | none found | true (no death date found; confirm) | Q2908982 |
| katzir | Ephraim Katzir | President | — | 1916-05-16 | 2009-05-30 | false | Q110330 |
| kessar | Yisrael Kessar | minister | 2583 | 1931-05-20 | 2019-09-08 | false | Q2776909 |
| landau | Haim Landau | minister | 2442 | 1916-09-10 | 1981-10-06 | false | Q2613376 |
| libai | David Libai | minister | 2432 | 1934-10-22 | 2023-12-24 | false | Q434987 |
| lipkin_shahak | Amnon Lipkin-Shahak | Aman | — | 1944-03-18 | 2012-12-19 | false | Q473775 |
| magen | David Magen | minister | 544 | 1945-09-04 | none found | true (no death date found; confirm) | Q2907544 |
| milo | Roni Milo | minister | 545 | 1949-11-26 | none found | true (no death date found; confirm) | Q945024 |
| modai | Yitzhak Moda'i | minister | 2451 | 1926-01-17 | 1998-05-14 | false | Q911074 |
| mordechai_gur | Mordechai Gur | minister (Chief of Staff to 16 April 1978) | 2247 | 1930-05-06 | 1995-07-16 | false | Q1335169 |
| moshe_levy | Moshe Levy (chief of staff) | Chief of Staff | — | 1936-04-18 | 2008-01-08 | false | Q1391622 |
| navon | Yitzhak Navon | President; minister | 2476 | 1921-04-09 | 2015-11-07 | false | Q311583 |
| neeman | Yuval Ne'eman | minister | 2475 | 1925-05-14 | 2006-04-26 | false | Q471965 |
| nehemkin | Aryeh Nehemkin | minister | 2483 | 1925-11-02 | 2021-11-24 | false | Q2905257 |
| nissim | Moshe Nissim | minister | 2489 | 1935-04-10 | none found | true (no death date found; confirm) | Q941785 |
| olmert | Ehud Olmert | minister | 1064 | 1945-09-30 | none found | true (no death date found; confirm) | Q93181 |
| ora_namir | Ora Namir | minister | 2487 | 1930-09-01 | 2019-07-07 | false | Q2908342 |
| patt | Gideon Patt | minister | 2560 | 1933-02-22 | 2020-04-27 | false | Q2906997 |
| peres | Shimon Peres | minister | 456 | 1923-08-02 | 2016-09-28 | false | Q57410 |
| pinhasi | Rafael Pinhasi | minister | 2545 | 1940-01-01 | none found | true (no death date found; confirm) | Q2906605 |
| rabin | Yitzhak Rabin | minister | 2595 | 1922-03-01 | 1995-11-04 | false | Q34060 |
| rafael_eitan | Rafael Eitan | Chief of Staff; minister | 2138 | 1929-01-11 | 2004-11-23 | false | Q365524 |
| ramon | Haim Ramon | minister | 458 | 1950-04-10 | none found | true (no death date found; confirm) | Q652581 |
| rubinstein | Amnon Rubinstein | minister | 510 | 1931-09-05 | 2024-01-18 | false | Q2890919 |
| sarid | Yossi Sarid | minister | 512 | 1940-10-24 | 2015-12-04 | false | Q143172 |
| shahal | Moshe Shahal | minister | 2633 | 1934-05-20 | none found | true (no death date found; confirm) | Q2906636 |
| shaki | Avner Shaki | minister | 2620 | 1926-02-05 | 2005-05-28 | false | Q2909821 |
| shamir | Yitzhak Shamir | minister | 2652 | 1915-10-22 | 2012-06-30 | false | Q184351 |
| sharir | Avraham Sharir | minister | 2667 | 1932-12-23 | 2017-03-24 | false | Q434978 |
| sharon | Ariel Sharon | minister | 482 | 1928-02-26 | 2014-01-11 | false | Q60206 |
| shavit | Shabtai Shavit | Mossad | — | 1939-07-17 | 2023-09-05 | false | Q1367833 |
| shetreet | Shimon Shetreet | minister | 2634 | 1946-03-01 | none found | true (no death date found; confirm) | Q2909079 |
| shochat | Avraham Shochat | minister | 459 | 1936-06-14 | 2024-02-28 | false | Q791348 |
| shomron | Dan Shomron | Chief of Staff | — | 1937-08-05 | 2008-02-26 | false | Q901545 |
| shostak | Eliezer Shostak | minister | 2627 | 1911-12-16 | 2001-08-20 | false | Q2334832 |
| tamir | Shmuel Tamir | minister | 2676 | 1923-03-10 | 1987-06-29 | false | Q434957 |
| tzaban | Yair Tzaban | minister | 2563 | 1930-08-23 | none found | true (no death date found; confirm) | Q2253290 |
| tzipori | Mordechai Tzipori | minister | 2573 | 1924-09-15 | 2017-05-29 | false | Q2904724 |
| uri_sagi | Uri Sagi | Aman | — | 1943-08-05 | none found | true (no death date found; confirm) | Q2906348 |
| uzan | Aharon Uzan | minister | 2129 | 1924-11-01 | 2007-01-23 | false | Q2906629 |
| yaacobi | Gad Yaacobi | minister | 2380 | 1935-01-18 | 2007-08-27 | false | Q612860 |
| yaakov_meridor | Ya'akov Meridor | minister | 2471 | 1913-09-29 | 1995-06-30 | false | Q2613811 |
| yaakov_tzur | Ya'akov Tzur | minister | 2568 | 1937-04-04 | none found | true (no death date found; confirm) | Q2776738 |
| yadin (exists) | Yigael Yadin | minister | 2369 | 1917-03-20 | 1984-06-28 | false | Q359699 |
| yehoshua_sagi | Yehoshua Sagi | Aman | — | 1933-09-27 | 2021-02-18 | false | Q2778280 |
| yisrael_katz | Yisrael Katz | minister | 3260 | 1927-12-06 | 2010-10-29 | false | Q487316 |
| yitzhak_peretz | Yitzhak Peretz (politician born 1938) | minister | 2559 | 1938-03-26 | none found | true (no death date found; confirm) | Q2907019 |
| yosef_shapira | Yosef Shapira | minister | 3263 | 1926-12-20 | 2013-12-28 | false | Q8055899 |
| zeevi | Rehavam Ze'evi | minister | 417 | 1926-06-20 | 2001-10-17 | false | Q170310 |
