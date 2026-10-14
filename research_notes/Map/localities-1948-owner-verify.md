# Localities 1948: the 52 `owner_verify` rows checked (research note, 2026-10-14)

Status: research for issue #200. It checks every row of
[`content/map/localities_1948.yaml`](../../content/map/localities_1948.yaml) that has `owner_verify: true` (52 rows on
`main` at 04bf7cb; the file last changed in a3202ad) and gives one recommendation per row. Nothing in `content/` was
changed: the owner clears the flags. Scope is the start of the game only, per the owner ruling of 2026-10-11 recorded in the file's header: no later
fate, depopulation date or post-May-1948 status is given here.

## Conventions

- **[V]** read on 2026-10-14 in the cited source: a scanned page, a data API, an article, a map tile.
- **[S]** one weak source, or an inference this note makes that no source states.
- **[C]** sources conflict. Every version is given.
- **[GAP]** searched and not found.
- **VS** is *Village Statistics, 1945* (Government of Palestine, figures as at 1 April 1945). **VS-T** is the
  transcription the file cites: en.wikipedia "Village Statistics, 1945", revision 1359600351. **VS-scan** is the page
  images of the printed book (page headers "VILLAGE STATISTICS - 1945, as at 1.4.45"), pages 4 to 33, hosted at
  `users.cecs.anu.edu.au/~bdm/yabber/census/VSpages/` (the host VS-T itself links for pages 2 and 3). "p31 #44" means
  scan page 31, serial number 44. Columns are Moslems, Jews, Christians, Others, Total, as the book prints them; this
  note writes M, J, C, O.
- **WD** is the row's Wikidata item, read through the `wbgetentities` API. "WD coords match" means the item's P625 is
  within 0.05 km of the row's `lon`/`lat`, and its en/he/ar labels equal the row's `name_he`/`name_ar` unless stated.
- **Map 1946** is the Survey of Palestine 1:250,000 of 1946, read as tiles from Palestine Open Maps (zoom 13 and 14).
  It was used for location only, for 18 rows where the place's identity or position was least obvious. At this scale
  a symbol is placed to within a few hundred metres; a game hex is about 65 km² (5 km circumradius).
- Every request was made with the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## Summary

1. **VS-scan was reached.** Every figure in the 52 rows was read against the printed page. All 52 `population` values
   equal VS-T summed over `listed_as`, and all VS-T rows used equal the scan, **except one**: VS-T's "Ijzim (Nazareth
   subdistrict)" (1,310) is the scan's **'Ilût** (p08 #12), a different village. The `ijzim` row is too high by 1,310
   [V].
2. **The scan's footnotes show six rows that sum more than one place** where VS-T shows one name: Pardes Hanna and
   Karkur (eleven places in all), Nesher (the Jewish locality plus two Arab groups), Kefar Gannim (with Giv'at
   hash Shelosha), Magdiel (with Ganei Am), and, from separate lines inside one serial, 'Iraq el Manshiya (with Gat)
   and Na'ana (whose 590 Jews are not part of the village figure in its own article). VS-T drops the footnotes.
3. **Recommendations:** 41 confirm as is, 5 confirm with a correction, 6 split or merge needed, 0 cannot verify.
   The corrections: `ijzim` population and `listed_as`; `ilar` Wikidata item (and the fields that follow from it);
   `dayr_al_qassi` landmark; `bayt_nuba` `listed_as` spelling; `az_zakariyya` identity-check URL (a redirect).
4. **Outside the 52 rows** (reported, not checked in full): 'Ilut (1,310 in 1945) is missing from the file because
   VS-T mislabels it; the unflagged `ramat_gan` row (10,200) includes Nahalat Gannim (1,100) by the scan's footnote.

## How each row was checked

1. Its VS-T row(s), parsed from the revision's wikitext; the M/J/C/O split gives the flag reasons below.
2. The same serial on the scan page, with any footnote mark and the footnote text.
3. The WD item: P625, labels, P31, enwiki/hewiki sitelinks.
4. The en.wikipedia article the row cites (the identity check), searched for its 1945 statement.
5. For 18 rows, Map 1946 at the row's point.

Flag reasons follow the header's rule: **match low/manual**; **multi-place** (the VS row names more than one place,
or the row sums two VS rows); **mixed** (Jews and Muslims each over 10% of the row's total).

What counts as a split: "split or merge needed" is used only where VS itself gives separate figures for the places in
the row (separate lines in a serial, or a footnote), so a split is possible from the source and changes the number.
Where VS gives one figure for "X and Y" or "X (includes ...)", no split is possible from VS, and the row is confirmed
as VS prints it, with the other places named.

## Per-row check

Rows are in file order (largest first). Each gives: flag reason; VS-T; VS-scan; WD; article; map where read;
recommendation.

### Gaza, Jaffa, Ramle, Hebron and Jerusalem subdistricts, and the towns

**`khan_yunis` Khan Yunis (11,220).** Flag: match manual. VS-T "Khān Yūnis (Urban)" M 11,180, C 40 = 11,220. Scan p31
#44 the same [V]; "Khān Yūnis (Rural)" #45 has no figure [V]. WD Q309348 coords and labels match. Article: 11,220
(11,180 Muslims, 40 Christians) [V]. **Confirm as is.**

**`al_majdal` Majdal (9,910).** Flag: match manual. VS-T "Majdal, El (Urban)" M 9,820, C 90 = 9,910. Scan p32 #48
the same; "(Rural)" #49 blank [V]. WD Q60956 is Ashkelon, the item the row uses for `today`; its first P625 matches
the row (a second P625 is 2 km west); its he label is אשקלון, while the row's `name_he` is אל-מג'דל, the 1948 name
(the header allows the he Wikipedia title as a fallback, so this is a choice, not an error). Article Ashkelon: 9,910,
ninety Christians and 9,820 Muslims [V]. **Confirm as is.**

**`bnei_brak` Bnei Brak (5,760).** Flag: match low (VS spells it "Benei Beraq"). VS-T "Benei Beraq (Urban)" J 5,760.
Scan p27 #4 the same; "(Rural)" #5 blank [V]. WD Q152467 coords match; `name_ar` is empty while WD has an ar label
(بني براك), consistent with the other Jewish rows. Article: no 1945 figure [GAP]. Map 1946: the point is about 1.5 km
south-east of the "Benei Beraq" label, near the "Ramat Gan" label [S: label placement, not a measured position].
**Confirm as is.**

**`sur_bahir` Sur Bahir (2,450).** Flag: match manual; multi-place ("Sur Bahir and Umm Tuba"). VS-T M 2,450. Scan p25
#92 "Sûr Bâhir and Umm Tûbâ", 2,450, one figure for both [V]. WD Q2916287 coords match. Article: Sur Baher with Umm
Tuba, 2,450 [V]. **Confirm as is** (VS gives no separate figure for Umm Tuba).

**`abasan_al_kabira` Abasan (2,230).** Flag: match manual. VS-T "Abasân" M 2,230. Scan p31 #1 the same [V]. WD
Q3303148 coords match. Article Abasan al-Kabira: "the two Abasans were still counted together", 2,230 [V]; so the
one VS row covers both Abasans [V, article]. **Confirm as is.**

**`iraq_al_manshiyya` 'Iraq el Manshiya (2,220).** Flag: multi-place ("'Iraq el Manshiya and Gat"). VS-T M 2,010,
J 210 = 2,220. Scan p31 #31: two lines in one serial, "'Irâq el Manshîya" M 2,010 and "Gat" J 210 [V]. WD Q2777842
coords match. Article: "the population of al-Manshiyya was counted with that of Gat ... 2,010 Muslims and 210 Jews
respectively"; infobox 2010; Gat is the kibbutz founded in 1941 on land bought from the village [V]. **Split or
merge needed:** the village is 2,010 [V, scan line and article]; Gat's 210 is a separate place under the 1,000 line.
If split, the row's population becomes 2,010 and its source line stays the same VS serial.

**`rafah` Rafah (2,220).** Flag: match manual. VS-T M 2,220. Scan p32 #59 the same [V]. WD Q172343 coords match.
Article: 2,220 in 1945 [V]. **Confirm as is.**

**`jabalia` Jabaliya (3,520).** Flag: match low. VS-T "Jabāliya" M 3,520. Scan p31 #34 the same [V]. WD Q1015472
coords match. Article: 3,520, all Muslims [V]. **Confirm as is.**

**`bani_suheila` Bani Suheila (3,220).** Flag: match manual. VS-T M 3,220. Scan p31 #4 the same [V]. WD Q1983186
coords match. Article: 3,220 [V]. **Confirm as is.**

**`kfar_ganim` Kfar Ganim (1,720).** Flag: match low. VS-T "Kefar Gannim" J 1,720. Scan p27 #25 "Kefar Gannim" 1,720
with footnote (c): "includes 1,010 Kefar Gannim, 710 Giv'at hash Shelosha" (p28) [V]. WD Q7033804 (a neighbourhood
item, he label כפר גנים) coords match; no enwiki article. Map 1946: the point is inside the "Petah Tiqva" label area
[S]. **Split or merge needed:** Kefar Gannim itself is 1,010 [V, scan footnote]; Giv'at hash Shelosha (710) is a
separate place under the 1,000 line. If split, `population` 1,720 becomes 1,010 (the row stays above 1,000).

**`fajja` Fajja (1,570).** Flag: mixed (J 370 of 1,570). VS-T M 1,200, J 370. Scan p27 #7 the same, no footnote [V].
WD Q5431395 coords match. Article: "Fajja had 1,200 Muslim inhabitants, in addition to 370 Jews"; its infobox gives
1,200 [V] [C on which figure the article treats as the village's]. Map 1946: Fajja's symbol is at the point [V]. VS
does not say where the 370 Jews lived [GAP]. **Confirm as is** (the VS figure and the identity are verified); the
owner may prefer 1,200 if the 370 are later shown to be a separate place.

**`naana` Na'ana (2,060).** Flag: match manual; mixed (J 590). VS-T "Na'ana (Ni'ana)" M 1,450, J 590, C 20. Scan p30
#67 the same on one line, no footnote [V]. WD Q4703007 (Al-Na'ani) coords match; no he label, so `name_he` is empty.
Article Al-Na'ani: "the village had a population of 1,470; 1,450 Muslims and 20 Christians"; infobox 1,470 [V]. Map
1946: Na'ana's symbol is at the point, and a separate symbol "Na'an" is about 1.6 km to the north-west [V]. That the
590 Jews are kibbutz Na'an is this note's inference: VS does not name them [S]. **Split or merge needed:** the village
is 1,470 by its article and by VS's own M and C columns [V]; the 590 Jews belong to a separate place [S]. If split,
`population` 2,060 becomes 1,470, and the row is no longer mixed.

**`ness_ziona` Ness Ziona (1,760).** Flag: match manual. VS-T "Wadi Hunein (Nes Tsiyona)" J 1,760. Scan p30 #97: two
lines in one serial, "Wâdi Hunein" M 1,620 and "(Nes Tsiyona)" J 1,760 [V]. VS-T gives them as two rows, and the file
already has Wadi Hunein as its own row (`wadi_hunayn`, 1,620). Article Wadi Hunayn: 1,620 Muslims and 1,760 Jews in
Wadi Hunayn and Ness Ziona together [V]. WD Q168162 coords match. Map 1946: the point is about 0.7 km from the symbol
labelled "...siyona" [S]. **Confirm as is** (the 1,760 is the Jewish line only; the `listed_as` text is VS-T's label).

**`magdiel` Magdiel (1,260).** Flag: match manual. VS-T J 1,260. Scan p27 #30 "Magdiel" 1,260 with footnote (e):
"includes 1,100 Magdiel, 160 Ganei Am" (p28) [V]. WD Q2910711 coords match. Map 1946: Magdiel's symbol is at the
point [V]. Article: no 1945 figure [GAP]. **Split or merge needed:** Magdiel itself is 1,100 [V, scan footnote]; Ganei
Am (160) is a separate place. If split, `population` becomes 1,100.

**`bayt_nuba` Beit Nuba (1,240).** Flag: match low; multi-place ("Beit Nuba and 'Ajanul"). VS-T M 1,240. Scan p29 #11
"Beit Nûbâ and 'Ajanjûl" 1,240 [V]. WD Q4874907 first P625 matches (two others are 0.1 and 1.2 km away). Article:
"the population of Beit Nuba and Ajanjul was 1,240" [V]. **Confirm with a correction:** `listed_as` "Beit Nuba and
'Ajanul (Ramle subdistrict)" is a VS-T typo; the scan and the article read **'Ajanjul**. The figure is right and VS
gives no split.

**`sair` Si'ir (2,710).** Flag: match low. VS-T "Si'ir" M 2,710. Scan p23 #30 the same [V]. WD Q7395443 first P625
matches (a second is 0.8 km away); he label סיעיר. Article Sa'ir: 2,710 [V]. **Confirm as is.**

**`tarqumiyah` Tarqumiya (1,550).** Flag: match manual. VS-T M 1,550. Scan p23 #33 the same [V]. WD Q1998630 coords
match. Article: 1,550 Muslims [V]. **Confirm as is.**

**`az_zakariyya` Zakariya (1,180).** Flag: match low. VS-T "Zakariya" (Hebron subdistrict) M 1,180. Scan p23 #36
"Zakarîyâ" 1,180 [V]. WD Q4831967 coords match; no he label. The cited article URL `en.wikipedia.org/wiki/Az-Zakariyya`
is now a redirect to "Zekharia" (tagged "R from merge"); that article's infobox and text give 1,180 for 1945 [V].
**Confirm with a correction:** the identity-check source `https://en.wikipedia.org/wiki/Az-Zakariyya` should read
`https://en.wikipedia.org/wiki/Zekharia`, the page the old title now serves. No other field changes (`today` stays
empty under the header's rule, since the WD item is the village's own).

### Haifa subdistrict

**`pardes_hanna_karkur` Pardes Hanna (5,350).** Flag: match manual; multi-place (sums two VS rows); mixed (M 670 of
5,350). VS-T "Karkur" J 2,380 and "Pardes Hanna" M 670, J 2,300. Scan p14 #40 "Karkûr" 2,380 with footnote (e), and
#57 "Pardes Hanna" M 670, J 2,300 with footnote (h) [V]. The footnotes (p15) [V]:
- (e) "Includes Karkur 900, Ein Shemer 310, Yan Hash Shomron 160, Ein Iron 40, Kefar Pines 280, Mishmarot 270,
  Ma'anit Narbata 220". These sum to 2,180, not the 2,380 printed in the table [C, internal to VS: 200 unexplained].
- (h) "Includes Pardess-Hanna 1,860, Meged 250, Kefar Glickson 190" (sums to 2,300).

WD Q2475182 coords match. Article Pardes Hanna-Karkur: Karkur 2,380 with "Karkur itself having 900"; Pardes Hanna
2,300 Jews and 670 Muslims, "with 1,860 ... in Pardes Hanna itself" [V]. Map 1946: the point is beside the "Pardes
Hanna" label [V]. **Split or merge needed:** the row sums nine Jewish places and 670 Muslims whom VS does not place.
Pardes Hanna (1,860) and Karkur (900) are each a place in their own right, and together 2,760 [V]. The owner's options:
one row for Pardes Hanna (1,860) and one for Karkur (900, under the 1,000 line); or one merged row of 2,760 named for
both; in either case the other seven places and the 670 Muslims leave the row, and it is no longer mixed.

**`ijzim` Ijzim (4,280).** Flag: match manual; multi-place (sums two VS rows). VS-T "Ijzim (Haifa)" M 2,830, C 140 =
2,970 and "Ijzim (Nazareth)" M 1,310. Scan: p14 #33 "Ijzim" 2,830, 140, 2,970 [V]; but Nazareth p08 #12 is
**"'Ilût"**, M 1,310, between Iksâl (#11) and Indûr (#13) in the book's alphabetical order; there is no Ijzim on the
Nazareth pages [V]. Article Ijzim: 2,970 (2,830 Muslims, 140 Christians), infobox 2,970 [V]. Article Ilut: "the
population was 1,310, all Muslims" in VS [V]; WD Q2915756 puts 'Ilut at 32.717, 35.263, 27 km east of Ijzim [V]. WD
Q2918334 coords match the row. Map 1946: Ijzim's symbol is at the point [V]. **Confirm with a correction:**
`population` 4,280 becomes **2,970**, and `listed_as` drops "Ijzim (Nazareth subdistrict)", keeping only "Ijzim (Haifa
subdistrict)". Source: the scan, p14 #33 and p08 #12. (VS-T's error is in the transcription, so the file's VS-T URL
still supports 2,970 for the Haifa row.)

**`shefa_amr` Shafa 'Amr (3,640).** Flag: match manual. VS-T "Shafa 'Amr (Urban)" M 1,380, J 10, C 1,560, O 690 =
3,640. Scan p15 #70 the same [V]; #71 "Shafâ 'Amr (Rural)" M 3,560 with footnote (j), a list of Arab groups and farms
(Arab el Zubeidat 410, Arab el Ameriya 410, Ibtan Farm 260, and others) [V]. WD Q990657 coords match. Article: urban
1,560 Christians, 1,380 Muslims, 10 Jews, 690 others, and a rural population of 3,560 Muslims [V]. **Confirm as is**
(the urban row is the town; the rural row is a list of separate groups, so leaving it out is right). The landmark
(church, Christian plurality) follows the header's rule.

**`nesher` Nesher (2,830).** Flag: match manual; mixed (M 1,430, J 1,400). VS-T M 1,430, J 1,400. Scan p14 #56 the
same, with footnote (g) on the 1,430: "Includes Arab Abu Junieh or Tawtha 680, Arab Awadien 750" (p15) [V]. WD Q135432
coords match. Article: no 1945 figure [GAP]. Map 1946: Nesher's symbol is about 0.5 km from the point, with Balad esh
Sheikh's label just north-west [V]. **Split or merge needed:** by the footnote, the Muslim figure is two named Arab
groups, not residents of the Jewish locality [V]. If split, Nesher is 1,400 Jews; `kind` (mixed_locality), `landmark`
(mosque_and_water_tower) and the mixed flag would follow the owner's choice for the two groups (VS lists them as
"Arab" groups, the form the header treats under encampments elsewhere).

**`kiryat_ata` Kfar Ata (1,690).** Flag: match manual. VS-T "Kefar 'Atta (Kufritta)" J 1,690. Scan p14 #41 the same
[V]. WD Q152602 coords match (they are rounded to 32.8, 35.1). Article Kiryat Ata: "Kfar Atta (Kufritta) consisted of
1,690 Jews" [V]. Map 1946: the point is about 0.8 km west of the "Kefar 'Atta" symbol [V]. **Confirm as is.** Two
notes for the owner: `name_he` is קריית אתא, today's name, while `name` is the 1948 name; and the coordinates are WD's
rounded values (both within the header's rules).

**`binyamina` Binyamina (1,520).** Flag: mixed (M 270 of 1,520). VS-T M 270, J 1,250. Scan p13 #12 the same, no
footnote [V]. WD Q863886 coords match; hewiki only, no enwiki; no `today`. Map 1946: Binyamina's symbol is at the
point [V]. VS does not say where the 270 Muslims lived [GAP]. **Confirm as is.**

**`yagur` Yagur (1,220).** Flag: match manual. VS-T "Mesheq Yagur" J 1,220. Scan p14 #54 "Mesheq Yagur (includes
Mesheq)" 1,220 [V]. WD Q586687 coords match. Map 1946: "Yagur" symbol at the point [V]. **Confirm as is.**

**`caesarea` Qisariya (1,120).** Flag: mixed (J 160). VS-T "Qisariya (Caesarea)" M 930, J 160, C 30. Scan p14 #61 the
same [V]. WD Q1615560 is the modern town Caesarea (coords rounded to 32.5, 34.9); the article it cites, "Caesarea
(modern town)", has no 1945 figure [GAP]; the en title "Qisarya" redirects to "Caesarea". Map 1946: the point is about
0.9 km east of the coastal "Caesarea" symbol [V]. Sdot Yam's article says the kibbutz moved to the edge of the
Caesarea ruins in 1940 [V]; that VS's 160 Jews are Sdot Yam is this note's inference [S]. **Confirm as is** (the
figure and the place are verified; the point is within a hex of the 1946 symbol).

### Nazareth, Tiberias, Beisan, Acre and Safad subdistricts

**`beit_shean` Beisan (5,180).** Flag: match manual. VS-T "Beisan (Urban)" M 4,730, C 430, O 20. Scan p06 #6 the same
[V]. WD Q152368 coords match. Article Beit She'an gives "5,080 Muslim Arabs out of 5,540" from unnamed Mandate
surveys, not VS [C]; it gives no VS figure. **Confirm as is** (VS is the file's stated source).

**`sepphoris` Saffuriya (4,330).** Flag: match manual. VS-T M 4,320, C 10. Scan p08 #38 the same [V]. WD Q1132798
first P625 matches (second 0.4 km away). Article: 4,330; 4,320 Muslims and 10 Christians [V]. **Confirm as is.**

**`yafa_an_naseriyye` Yafa (1,070).** Flag: match manual. VS-T M 580, C 490. Scan p08 #46 the same [V]. WD Q2468131
coords match. Article: 1,070; 580 Muslims and 490 Christians [V]. **Confirm as is.**

**`maghar` Maghar (2,140).** Flag: match manual; multi-place ("Maghar and El Mansura"). VS-T M 90, C 800, O 1,250.
Scan p12 #23 the same, one figure [V]. WD Q930847 coords match. Articles Maghar and Al-Mansura (Tiberias): the two
together 2,140 [V]. Landmark `none` (Druze "others" the largest group) follows the header's rule. **Confirm as is**.

**`ashdot_yaakov` Ashdot Ya'akov (1,020).** Flag: match low. VS-T "Ashdot Ya'aqov" J 1,020. Scan p12 #2 the same [V].
WD Q2891543 coords match. Map 1946: "Ashdot Ya'aqov" symbol at the point [V]. Article: no 1945 figure [GAP].
**Confirm as is.**

**`dayr_al_qassi` Deir el Qasi (1,250).** Flag: match manual; multi-place ("Deir el Qasi and El Mansura"). VS-T M 370,
C 880. Scan p04 #14: one serial, "Fassûta and Deir el Qâsi and El Mansûra", with two population lines: Fassuta 1,050
(printed in the Moslems column) and Deir el Qasi and El Mansura M 370, C 880 = 1,250 [V]. Fassuta has its own row in
the file. WD Q5243305 coords match. Article Dayr al-Qassi: "1,250 inhabitants; 370 Christians and 880 Muslims", the
reverse of the scan's columns, and "together with Fassuta and al-Mansura, the population was 2,300" [C]. Article
Al-Mansura (Acre): "the population was predominantly Christian" [V]. **Confirm with a correction:** the header derives
`landmark` from VS's religion columns, which give a Christian majority (880 of 1,250), so `landmark` mosque becomes
**church**. The conflict with the article is recorded; if the owner prefers the article's reading, the landmark stays
mosque and the header's rule needs an exception. The population (1,250) is right; it includes El Mansura, and VS
gives no split.

**`al_manshiyya_acre` Manshiya (1,080).** Flag: mixed (J 270). VS-T M 810, J 270. Scan p04 #31 the same, no footnote
[V]. WD Q4702804 coords match. Article: "810 Muslim inhabitants and 270 Jewish inhabitants"; infobox 810 [V]. Map 1946:
"Manshîya" symbol about 0.4 km from the point [V]. VS does not say where the 270 Jews lived [GAP]. **Confirm as is.**

**`tarbikha` Tarbiha (1,000).** Flag: multi-place ("includes En Nabi Rubin and Suruh"). VS-T M 1,000. Scan p05 #46
the same [V]. WD Q2777821 coords match. Article: counted with Suruh and Al-Nabi Rubin, 1,000 [V]. **Confirm as is.**

**`hunin` Hunin (1,620).** Flag: multi-place ("includes Hula and 'Udeisa"). VS-T M 1,620. Scan p09 #29 the same [V].
WD Q12406488 coords match. Article: 1,620 with Hula and Udeisa; it carries a "dubious" tag asking why two villages
now in Lebanon are in a Mandate statistic [V]. **Confirm as is** (VS prints it so and gives one figure).

**`al_naima` Na'ima (1,240).** Flag: mixed (J 210). VS-T "Na'ima (En)" M 1,030, J 210. Scan p10 #58 the same [V]. WD
Q4703008 coords match; no he label. Article: text "1,240 of whom 210 were Jews" [V]; infobox "1240 (1,340 Arabs and 210
Jews)", where 1,340 is an error for 1,030 [C, internal to the article]. Map 1946: two symbols are labelled "Na'ima",
about 1.5 km apart; the point is at the northern one [V]. VS does not say where the 210 Jews lived [GAP]. **Confirm as
is.**

**`saliha` Saliha (1,070).** Flag: multi-place ("includes Marun er Ras and Yarun"). VS-T M 1,070. Scan p11 #71 the
same [V]. WD Q7404513 coords match. Article: counted with Maroun al-Ras and Yaroun, 1,070 [V]. **Confirm as is.**

### Jenin, Nablus, Tulkarm and Ramallah subdistricts

**`tubas` Tubas (5,530).** Flag: multi-place ("includes Kashda and Bardala"). VS-T M 5,470, C 60. Scan p19 #82 the same,
the total marked with an asterisk: "Includes 90 Moslems in 'Aqrabaniya" [V]. WD Q275919 coords match. Article: Tubas
and Bardala 5,530 [V]. **Confirm as is** (one VS figure; the footnote adds a third place, 90 people).

**`umm_al_fahm` Umm el Fahm (5,490).** Flag: multi-place (eight places included). VS-T M 5,430, C 60. Scan p17 #51
the same [V]. WD Q168245 coords match. Article: counted with the Wadi Ara villages [V]. **Confirm as is.**

**`yabad` Ya'bad (3,480).** Flag: multi-place (five hamlets included). VS-T M 3,480. Scan p17 #53 the same [V]. WD
Q1828708 coords match. Article: the same five hamlets, 3,480 [V]. **Confirm as is.**

**`silat_adh_dhahr` Silat edh Dhahr (2,850).** Flag: match low. VS-T M 2,850. Scan p17 #45 the same [V]. WD Q2898957
coords match. Article: 2,850 [V]. **Confirm as is.**

**`anabta` Anabta (3,120).** Flag: match manual; multi-place ("Anabta and Iktaba"). VS-T M 3,080, C 40. Scan p20 #1
the same, one figure [V]. WD Q2701952 coords match. Article: "the combined population of Anabta and Iktaba was 3,120"
[V]. **Confirm as is.**

**`deir_al_ghusun` Deir el Ghusun (2,860).** Flag: multi-place (six hamlets included). VS-T M 2,860. Scan p20 #16 the
same [V]. WD Q627230 coords match. Article: 2,860 with the villages that later made up Zemer [V]; a later paragraph of
the same article says "a census in 1945" gave 2,220 [C, internal to the article; VS prints 2,860]. **Confirm as is.**

**`shwiaka_tulkarm` Shuweika (2,370).** Flag: match low. VS-T M 2,370. Scan p21 #90 "Shuweika" 2,370 [V]. WD
Q12219298 (en label "Shwiaka, Tulkarm", a neighbourhood item; hewiki only) coords match. Map 1946: "Shuweika" symbol
about 0.3 km from the point [V]. **Confirm as is.**

**`qaffin` Qaffin (1,570).** Flag: multi-place (two khirbas included). VS-T M 1,570. Scan p21 #79 the same [V]. WD
Q2892780 coords match. Article: 1,570 [V]. **Confirm as is.**

**`ilar` Ilar (1,450).** Flag: match low. VS-T "Ilar" M 1,450. Scan p20 #39 "Illâr" 1,450 [V]. The row's WD Q33534652
is a bare GeoNames-style item (en label "'Illār", P31 human settlement, a cebwiki sitelink only, no he or ar label).
The place's main item is **Q5998781** "Illar, Tulkarm" (P31 municipality, he עילאר, ar علار, enwiki "Illar, Tulkarm",
P625 32.370086, 35.107486, 0.04 km from the row's point) [V]. Article "Illar, Tulkarm": 1,450, all Muslims, in VS [V].
Map 1946: "'Illar" symbol at the point [V]. **Confirm with a correction:** `wikidata` Q33534652 becomes **Q5998781**,
and the WD source URL with it; add `https://en.wikipedia.org/wiki/Illar,_Tulkarm` as the identity check. Under the
header's rules that also fills `name_he` עילאר and `name_ar` علار, and `today` {name: "Illar, Tulkarm", name_he:
עילאר}. `lon`/`lat` need not change.

**`awarta` Awarta (1,470).** Flag: multi-place ("Awarta and Udala"). VS-T M 1,470. Scan p18 #7 "'Awartâ and Udala"
1,470 [V]. WD Q2905293 coords match. Article: Awarta with Odala, 1,470 [V]. **Confirm as is.**

**`biddya` Bidya (1,360).** Flag: match low. VS-T M 1,360. Scan p18 #18 "Biddyâ" 1,360 [V]. WD Q2533232 coords match.
Article: 1,360 [V]. **Confirm as is.**

**`qabalan` Qabalan (1,310).** Flag: match manual. VS-T M 1,310. Scan p19 #61 the same [V]. WD Q7265686 coords match.
Article: 1,310 [V]. **Confirm as is.**

**`burin` Burin (1,200).** Flag: multi-place ("Burin and 'Iraq Burin"). VS-T M 1,200. Scan p18 #19 the same, one figure
[V]. WD Q2896916 first P625 matches. Article: Burin with Iraq Burin, 1,200 [V]. **Confirm as is.**

**`azzoun` Azzun (1,190).** Flag: multi-place ("Azzun and En Nabi Ilyas and 'Isla"). VS-T M 1,170, C 20. Scan p20 #5
the same [V]. WD Q2918963 coords match. Article: with Nabi Ilyas and Islah, 1,190 [V]. **Confirm as is.**

**`al_bireh` Bira (2,920).** Flag: match manual. VS-T "Bira (El) (Urban)" M 2,640, C 280. Scan p26 #17 the same; "(Rural)"
#18 blank [V]. WD Q311326 coords match. Article: 2,920; 280 Christians and 2,640 Muslims [V]. **Confirm as is.**

## Related findings outside the 52 rows

These were seen while reading the scan. They are not checked in full and are listed for the owner.

- **'Ilut is missing.** VS has 'Ilût (Nazareth) at 1,310 (scan p08 #12; article Ilut [V]). VS-T labels it "Ijzim", so
  the file's scope rule (every VS place of 1,000 or more) missed it.
- **`ramat_gan` (10,200, not flagged)** includes Nahalat Gannim: Jaffa footnote (g), "includes 9,100 Ramat Gan, 1,100
  Nahalat Gannim" (scan p28) [V]. The same kind of split as `kfar_ganim` and `magdiel`.
- VS-T has no footnotes. Other footnoted totals in the scan (for example Haifa (d) for "Haifa (Urban)", Jaffa (f) for
  "Petah Tiqva (Rural)") were not checked against the file.

## Summary table

| Row id | Name | Flag reason | Recommendation |
| --- | --- | --- | --- |
| khan_yunis | Khan Yunis | match manual | Confirm as is |
| al_majdal | Majdal | match manual | Confirm as is |
| bnei_brak | Bnei Brak | match low | Confirm as is |
| tubas | Tubas | multi-place | Confirm as is |
| umm_al_fahm | Umm el Fahm | multi-place | Confirm as is |
| pardes_hanna_karkur | Pardes Hanna | manual; multi-place; mixed | Split or merge needed (footnotes e, h) |
| beit_shean | Beisan | match manual | Confirm as is |
| sepphoris | Saffuriya | match manual | Confirm as is |
| ijzim | Ijzim | manual; multi-place | Correction: population 4,280 to 2,970; drop the Nazareth `listed_as` |
| shefa_amr | Shafa 'Amr | match manual | Confirm as is |
| jabalia | Jabaliya | match low | Confirm as is |
| yabad | Ya'bad | multi-place | Confirm as is |
| bani_suheila | Bani Suheila | match manual | Confirm as is |
| anabta | Anabta | manual; multi-place | Confirm as is |
| al_bireh | Bira | match manual | Confirm as is |
| deir_al_ghusun | Deir el Ghusun | multi-place | Confirm as is |
| silat_adh_dhahr | Silat edh Dhahr | match low | Confirm as is |
| nesher | Nesher | manual; mixed | Split or merge needed (footnote g) |
| sair | Si'ir | match low | Confirm as is |
| sur_bahir | Sur Bahir | manual; multi-place | Confirm as is |
| shwiaka_tulkarm | Shuweika | match low | Confirm as is |
| abasan_al_kabira | Abasan | match manual | Confirm as is |
| iraq_al_manshiyya | 'Iraq el Manshiya | multi-place | Split or merge needed (Gat line) |
| rafah | Rafah | match manual | Confirm as is |
| maghar | Maghar | manual; multi-place | Confirm as is |
| naana | Na'ana | manual; mixed | Split or merge needed (village 1,470) |
| ness_ziona | Ness Ziona | match manual | Confirm as is |
| kfar_ganim | Kfar Ganim | match low | Split or merge needed (footnote c) |
| kiryat_ata | Kfar Ata | match manual | Confirm as is |
| hunin | Hunin | multi-place | Confirm as is |
| fajja | Fajja | mixed | Confirm as is |
| qaffin | Qaffin | multi-place | Confirm as is |
| tarqumiyah | Tarqumiya | match manual | Confirm as is |
| binyamina | Binyamina | mixed | Confirm as is |
| awarta | Awarta | multi-place | Confirm as is |
| ilar | Ilar | match low | Correction: `wikidata` Q33534652 to Q5998781 (and the fields that follow) |
| biddya | Bidya | match low | Confirm as is |
| qabalan | Qabalan | match manual | Confirm as is |
| magdiel | Magdiel | match manual | Split or merge needed (footnote e) |
| dayr_al_qassi | Deir el Qasi | manual; multi-place | Correction: `landmark` mosque to church [C] |
| al_naima | Na'ima | mixed | Confirm as is |
| bayt_nuba | Beit Nuba | low; multi-place | Correction: `listed_as` 'Ajanul to 'Ajanjul |
| yagur | Yagur | match manual | Confirm as is |
| burin | Burin | multi-place | Confirm as is |
| azzoun | Azzun | multi-place | Confirm as is |
| az_zakariyya | Zakariya | match low | Correction: article URL Az-Zakariyya (redirect) to Zekharia |
| caesarea | Qisariya | mixed | Confirm as is |
| al_manshiyya_acre | Manshiya | mixed | Confirm as is |
| saliha | Saliha | multi-place | Confirm as is |
| yafa_an_naseriyye | Yafa | match manual | Confirm as is |
| ashdot_yaakov | Ashdot Ya'akov | match low | Confirm as is |
| tarbikha | Tarbiha | multi-place | Confirm as is |

Counts: **confirm as is 41; confirm with a correction 5; split or merge needed 6; cannot verify 0** (52 rows).

## Sources not reached

- National Library of Israel catalogue record for VS 1945 (`www.nli.org.il/en/books/NNL_ALEPH002249756/NLI`, the record
  VS-T cites): 403.
- The NLI Rosetta item VS-T cites (`rosetta.nli.org.il/delivery/DeliveryManagerServlet?dps_pid=IE13978876`) opened, but
  its title is "Village statistics, February 1938", not the 1945 edition; not used.
- Survey of Palestine 1:20,000 sheets: not tried; only the 1:250,000 of 1946 was read, and only for 18 rows.
- Articles with no 1945 figure (searched, [GAP]): Bnei Brak, Nesher, Ness Ziona, Magdiel, Yagur, Ashdot Ya'akov,
  Caesarea (modern town); Kfar Ganim, Binyamina, Shwiaka and the row's old Illar item have no enwiki article.
- Who the Jews in Fajja (370), Na'ima (210) and Manshiya (270), and the Muslims in Binyamina (270) and Pardes Hanna
  (670), were: VS does not say, and no source read here does [GAP].

## Sources (all checked 2026-10-14)

- Government of Palestine, *Village Statistics, 1945* (as at 1.4.45), page images pp. 4-33:
  https://users.cecs.anu.edu.au/~bdm/yabber/census/VSpages/VS1945_p04.jpg (and `_p05` to `_p33`).
- en.wikipedia, "Village Statistics, 1945", revision 1359600351 (VS-T):
  https://en.wikipedia.org/w/index.php?title=Village_Statistics,_1945&oldid=1359600351
- Wikidata, `wbgetentities` for the 52 items in the rows, and for Q5998781 and Q2915756:
  https://www.wikidata.org/w/api.php?action=wbgetentities
- en.wikipedia articles, current revisions, read as wikitext: each row's cited article (the URLs in the YAML), and Ilut,
  Illar, Tulkarm, Zekharia, Wadi Hunayn, Na'an, Fassuta, Al-Mansura (Acre), Al-Mansura (Tiberias), Gat (Israel), Sdot
  Yam: https://en.wikipedia.org/wiki/Ilut, https://en.wikipedia.org/wiki/Illar,_Tulkarm,
  https://en.wikipedia.org/wiki/Zekharia, https://en.wikipedia.org/wiki/Wadi_Hunayn, https://en.wikipedia.org/wiki/Na%27an,
  https://en.wikipedia.org/wiki/Fassuta, https://en.wikipedia.org/wiki/Al-Mansura,_Acre,
  https://en.wikipedia.org/wiki/Al-Mansura,_Tiberias, https://en.wikipedia.org/wiki/Gat,_Israel,
  https://en.wikipedia.org/wiki/Sdot_Yam
- Survey of Palestine, Palestine 1:250,000, 1946, via Palestine Open Maps: https://palopenmaps.org/en/maps?basemap=pal250k1946
  (tiles https://palopenmaps.org/tiles/pal250k-1946/{z}/{x}/{y}.jpg, zoom 13 and 14).
- In the repo: [`localities_1948.yaml`](../../content/map/localities_1948.yaml) (header and rows),
  [`localities_bake.py`](../../tools/localities_bake.py), [`CONTENT_POLICY.md`](../../docs/CONTENT_POLICY.md),
  [`SOURCING_FOR_CONTRIBUTORS.md`](../../docs/SOURCING_FOR_CONTRIBUTORS.md).
