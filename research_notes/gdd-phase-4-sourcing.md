# GDD Phase 4 sourcing: camp sites, the 1949 rail link, the Tel Aviv-Netanya road, the Burma Road (research note, 2026-10-15)

Status: research only, for the [Phase 4 plan](../docs/superpowers/plans/2026-10-15-gdd-phase-4.md) (issue #384, owner
decisions #375): Task 1.0 (Q-CAMP-2), Task 2.0 (Q-RAIL-1, Q-COR-3) and the Q-COR-1 wording. Nothing here is in
`content/`. Which camp sites, corridors and card wording the game uses are the owner's calls; this note gives the
sourced facts, the conflicts and the gaps. It builds on the
[corridors note](GDD%20phase%204/corridors-1948-51.md) (2026-10-13), whose findings it does not repeat except where a
source read today changes or confirms them.

## How to read this note

- **[V]** I read the claim on the page or scan named, on 2026-10-15. **[S]** the claim is from a Wikipedia article
  (revision given), not checked against the source the article cites; the cited source is named so a fact-checker can
  go to it. **[P]** the claim rests on another person's reading of a page I could not open (named each time).
  **[C]** sources disagree. **[U]** one weak source, or the claim could not be checked. **[G]** gap: no source found.
- Every source was checked on **2026-10-15** unless a row says otherwise.
- **Coordinates** are the point the place's Wikipedia article gives (API `prop=coordinates`, read 2026-10-15), rounded
  to 4 decimals. A town or station point is not a surveyed route point. Distances are great-circle distances to the
  `content/map/settlements_1948.yaml` point of the city.
- **Access.** The National Library's newspaper site (`nli.org.il/he/newspapers`, which JPress now redirects to)
  answered HTTP 403 to every scripted request today, so I read no Hebrew newspaper page myself. The Israel Air Force
  site (iaf.org.il) also answered 403. Where a Hebrew paper is named below, it is either [P] (read by Agam in a browser
  on #384, 14 October 2026) or [S] (cited by a Wikipedia article). The press links are listed so a human can open them.
- Every request used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## Summary

1. **Q-CAMP-2: two camp sites qualify on a source I read; three more qualify only on Wikipedia and its cited books.**
   Read today [V]: **Camp 80 near Pardes Hanna**, 6.7 km from Hadera, where the Gadna training of settlement nuclei
   (the Nahal's first intake) moved on **13 July 1948**; and the **Gadna camp at Kfar Yona**, 7 km from Netanya, where
   that training began in **the first week of July 1948** (Gshur 2025, an IDF journal article). Wikipedia only [S]:
   **Camp Dora** by Netanya (the infantry school, Bahad 3, from 1948; its intelligence school from September 1948; the
   officers' school next door from January 1949); **Tzrifin** by Rishon LeZion (an IDF camp from 18-19 May 1948, the
   national training base from 1949); **Sirkin** by Petah Tikva (the flying school from 20 October 1949). For Jerusalem,
   **Schneller** is a sourced base held from 17 March 1948 (before the game starts), so whether it counts is an owner
   question; its training role is [U]. For Haifa, Tiberias, Safed, Rosh Pinna, Rehovot and Zikhron Ya'akov I found **no**
   training base with a sourced place and date in the window [G].
2. **Q-RAIL-1: both new segments and the day are confirmed by Israel Railways' own history [V]; the bypass's
   alignment is not given by any source [G].** Israel Railways' 1998 booklet says that in the summer of 1949 it laid two
   new sections, one bypassing Tulkarm station and one joining the Rosh HaAyin-Petah Tikva line to Tel Aviv North, and
   that regular passenger service Haifa-Binyamina-Hadera East-Rosh HaAyin-Tel Aviv North began on **20 September 1949**.
   The press confirmation of the day (*Haboker*, 14 September 1949; *Maariv*, 22 September 1949) is Agam's reading [P];
   I could not open it. The Yarkon link ran about 7 km west from the Petah Tikva branch to Tel Aviv North [S]. No source
   gives the Tulkarm bypass's line, only "west of Tulkarm", near the armistice line [G]: by the plan's own rule the
   corridor is then the Yarkon link alone.
3. **Q-COR-3: the opening is confirmed and the road was not finished when it opened [V].** *The Jerusalem Post*, 30
   July 1950: the 26 km road opened "yesterday" (Friday 28 July 1950), starting just north of the Yarkon bridge; an
   8 km stretch at Wadi Falik (Nahal Poleg) was still unbuilt and traffic took a detour. The same report has the
   Ministry of Labour's Director-General saying work "started more than a year ago" [C] against "from November 1949 at
   both ends" (Hebrew Wikipedia, citing *Davar* and *Maariv*) [S].
4. **Q-COR-1: the two dates are two different events on two different roads.** 14 June 1948 is when the Burma Road was
   counted finished: "UN inspectors pronounced the road paved" (Isseroff) [V], or the UN approved it (Hebrew Wikipedia)
   [S]; trucks used it from 10 or 11 June [C]. 7 December 1948 is the official opening of the **Road of Valour**, a
   separate permanent road built by the Engineering Corps in September and October 1948 [P, S]. Two other dates are in
   print for that opening: 12 December 1948 (two books) and 10 December 1948 (a footnote on the Chief of Staff's order
   of the day) [C].

## 1. Q-CAMP-2: training bases near MVP cities with no camp at the start

**Which cities.** The MVP's twelve cities are the `city` and `town` rows of `content/map/settlements_1948.yaml` (Phase 3
plan, PR 1): Jerusalem, Tel Aviv, Haifa, Tiberias, Safed, Petah Tikva, Rishon LeZion, Zikhron Ya'akov, Rosh Pinna,
Rehovot, Hadera and Netanya. `content/cities.yaml` does not exist yet; the slice city in `sim/Statecraft.Sim/MvpSlice.cs`
(lines 22-31) is Tel Aviv, with `facilities: [military_camp, hakhshara]` (D-D5, a design assumption). So every city
except Tel Aviv starts without a camp. If Phase 3 PR 1 gives other cities start facilities, this list shrinks.

**The rule** (plan, Q-CAMP-2 and Task 1.0): a candidate is a site only with a sourced place and a sourced date. I
read that as "a source for each"; a [S] candidate meets it on Wikipedia and its cited book, which a human still has to
read before the site is `fact-checked`.

### Candidates that qualify

| # | Site | Point (lat, lon) | Nearest MVP city | Date in the window | What it was | Mark |
|---|---|---|---|---|---|---|
| C1 | Camp 80 (מחנה 80, today Bahad Dotan), north-east entrance to Pardes Hanna-Karkur, on Route 65 | 32.4628, 34.9866 [S, HE C80] | Hadera 6.7 km; Zikhron Ya'akov 12.4 km | **13 July 1948**: the Gadna training of the 1931-born intake, including the settlement nuclei that became the Nahal's first company, moved here from Kfar Yona after artillery fire hit that camp during the Ten Days. It was meant to be temporary, but the camp stayed tied to the Nahal for decades. On 12 September 1948, when the Nahal was founded, the nuclei training here were made Nahal soldiers and trained about another month. In December 1948 the Nahal's second intake trained here. | [V] Gshur 2025, pp. 114-115, 120 and 126 (place and date). The point is [S]. HE C80 adds that the officers' school moved here in May 1948 (HE Bahad 1, no citation [U]) and was here, with the Nahal gone, from 1949 to 1955 [S, cited: *Bamahane*, 22 June 1994] |
| C2 | The Gadna training camp at Kfar Yona (Hebrew Wikipedia calls the Gadna base "Camp Beit Lid", today Camp Gur, by Kfar Yona) | Kfar Yona 32.3155, 34.9320 [S]; Camp Gur 32.3337, 34.9235 [S, HE Gur] | Netanya 7.2 km (Kfar Yona) or 6.3 km (Camp Gur) | **First week of July 1948**: training of the 1931-born Gadna intake (about 1,400 youths called up on 23 June 1948, five settlement nuclei among them) began at Kfar Yona under Elhanan Yishai, the Gadna's deputy commander. It left on 13 July 1948 after artillery fire (C1). HE Gadna: the command training base at Camp Beit Lid "began its activity on 23 June 1948" and was suspended during the war because it was shelled several times | [V] Gshur 2025, pp. 114-115 (place "Kfar Yona" and the date). [S] HE Gadna for 23 June 1948 and the camp name, no citation [U]. **[U]** that Gshur's Kfar Yona camp and the Beit Lid camp are the same place: no source read says so |
| C3 | Camp Dora (מחנה דורה), south of Netanya, around the Dora pool | Dora pool 32.2913, 34.8469 [S, HE Dora pool: the British camp "surrounded the pool"] | Netanya 4.2 km | **1948**: Bahad 3, the infantry school, founded at Camp Dora; **September 1948**: the permanent combat intelligence school set up there; **January 1949**: the officers' school (Bahad 1) opened at Camp Eliyahu, next to Camp Dora. The camp had been the Alexandroni Brigade's base and then the Eastern Front's headquarters (to May 1949); the Jewish Agency bought it from the British in January 1948 | [S] HE Bahad 3 (cited: Zalman Gandler, *Mabat Malam* 53, April 2009, for September 1948; Nir Man, *HaKirya*, 2012, p. 38, for the purchase); [S] HE Bahad 1 (cited: Hanoch Petishi, *Zaro*, 2013, p. 148, for January 1949). *HaTzofe*, 20 September 1949, reports the Prime Minister's visit to the infantry school [S, cited by HE Bahad 3]. None read |
| C4 | Tzrifin (צריפין, Sarafand) | 31.9587, 34.8395 [S, HE Tzrifin] | Rishon LeZion 3.9 km; Rehovot 7.2 km | **18-19 May 1948**: taken by the Givati Brigade without loss, and at once an IDF camp. **1949**: the national training base (Bahad 4, recruits) there until 1967. **January to June 1950**: the third battalion commanders' course there, as Bahad 2 | [S] HE Tzrifin (cited: *Al HaMishmar* and *Haaretz*, 20 May 1948); [S] HE Bahad 4 (1949, year only, no citation [U]); [S] HE Bahad 2 (cited: Sagi Turgan, 2017, p. 281). None read. **Sensitivity:** a prisoner-of-war camp for Arabs captured in the fighting stood on the site (over 1,100 prisoners in December 1948; closed in summer 1949) [S, HE Tzrifin, cited: *Haaretz*, 1 December 1948; *HaBoker*, 22 August 1949; neither read]. Any card text built from this site goes through D-F1 review |
| C5 | Sirkin airfield (מחנה סירקין, "Alon" base) | 32.0886, 34.9156 [S, HE Sirkin] | Petah Tikva 2.75 km | **20 October 1949**: the Air Force's flying school ("Training Wing 12") founded at the new Alon base; flying courses there until March 1955 | [S] HE Sirkin; HE Flying School (cited: Yishgav Nakdimon, *Bitaon Heil HaAvir* 175, July 1990). Not read; iaf.org.il refused the request |

**Fit to the game's two facility kinds** (a design reading, not a fact): C1 and C2 trained pioneer settlement nuclei
inside the Gadna and then the Nahal (Gshur 2025), so they fit a `hakhshara` site better than a recruit camp; C1 also
held the officers' school. C3 and C4 were IDF schools and training bases (`military_camp`). C5 trained pilots, not
recruits; whether it counts as a "military camp" for the recruit track is the owner's call. Which kind each site gets,
and whether an air base counts, is Q-CAMP-3 and Q-SENS-1 territory.

### Jerusalem: Schneller, a sourced base held before the start (owner question)

| Site | Point (lat, lon) | Nearest MVP city | Date | What it was | Mark |
|---|---|---|---|---|---|
| Schneller camp (the Schneller orphanage compound, מחנה שנלר) | 31.7908, 35.2127 [S, HE Schneller] | Jerusalem 1.8 km | **17 March 1948**: the British evacuated the camp and handed it to the Etzioni Brigade, the Haganah's Jerusalem district force; it was then called "permanent camp no. 1" and served as an army base for 60 years | [S] HE Schneller [43121200] (cited: Rachel Yanait, Yitzhak Avrahami and Yerah Etzion, *HaHagana BeYerushalayim*, vol. 2, Jerusalem, 5735 (1974-75), pp. 96, 98; not read). **Training role [U]:** Arutz 7, 26 November 2008 (https://www.inn.co.il/news/182116, read 2026-10-15), says the camp served "among other things as a training camp of the Etzioni Brigade", with no date for that use |

**Owner question:** Schneller (from 17 March 1948) and Tel HaShomer (from 15 April 1948, below) were both held before
15 May 1948. Q-CAMP-2 asks for bases near cities with no camp at the start. It does not say whether a camp already held
before the start counts as one the player builds or as a facility the city already has. This note excluded Tel HaShomer
on that ground; the same rule decides Schneller. Which way it goes is the owner's call, and it decides both sites.

Agam's parallel research (#612) raised Schneller and the Tzrifin prisoner-of-war camp; both rows above cite their
sources directly.

### Candidates checked that do not qualify

| Candidate | Why not | Source |
|---|---|---|
| Tel HaShomer (Camp Tel Litvinsky, today Camp Dori), 32.0434, 34.8573; Petah Tikva 5.8 km | Taken by the Alexandroni Brigade on 15 April 1948 (before the game starts; see the owner question above). The women's training base (Bahad 12) was set up there "in the early 1950s"; its infobox says 1950, with no month and no citation | [S] HE Tel HaShomer camp (cited: *HaBoker*, *Kol HaAm*, *Davar*, 16 April 1948); [S] HE Bahad 12 [U] |
| Ju'ara, 32.5995, 35.1088; Zikhron Ya'akov 15.1 km, Haifa 26.5 km | The Haganah's main training base from 1938 to 1948, so it existed before the game starts; Golani's squad commanders' school from 1950 (year only). About two hexes from the nearest city | [S] HE Ju'ara (cited: Meir Shalev, *Bamahane*, 1988) |
| The Gadna base at Givat Ram, Jerusalem | A 1948 photograph of its entrance sign exists; no opening date found (Schneller, above, is the Jerusalem candidate) | [S] HE Gadna (photograph caption only) [G] |
| Tel Nof (Ekron), by Rehovot | An operational air base; a flying course got its wings there on 10 May 1949, but no source names a training base opened there in the window | [S] HE Flying School [G] |
| Haifa, Tiberias, Safed, Rosh Pinna | No training base with a place and a date in 1948-51 found in any source read | [G] |

The war research named in Task 1.0 (`research_notes/M4 war mode/war-1948.md`) names the Julis camp (a brigade base,
December 1948) and the Mekorot camp near Nir Am (a brigade headquarters); neither is a training base or near an MVP
city. #570's unit anchors were not opened for this note.

## 2. Q-RAIL-1: the 1949 Haifa-Tel Aviv restoration

### What was built, and when

| Date | Event | Source | Mark |
|---|---|---|---|
| summer 1949 | Israel Railways laid two new sections of track: one bypassing Tulkarm station, which stayed in Jordanian hands, restoring continuous rail traffic between Haifa and the centre; the other joining the Rosh HaAyin-Petah Tikva line to the old Tel Aviv North station (today Bnei Brak station, by the Ramat Gan stadium) | Israel Railways, *50 Years of Israel Railways* (Hebrew booklet, September 1998), p. 15 | [V] |
| 4 August 1949 | "Train traffic to Haifa in September; the section bypassing Tulkarm completed" (headline) | *Haaretz*, 4 August 1949, cited by HE Coastal | [S] |
| 4 August 1949 | "On the 21st of the month the freight train service will be renewed" (headline), i.e. 21 August 1949 | *Al HaMishmar*, 4 August 1949, cited by HE IR | [S] |
| August 1949 or August 1948 | The bypass laid | HE Tulkarm station gives **August 1949**, citing Cotterell 1984, p. 86; EN PR gives **August 1948**, citing the same page | **[C]** on Cotterell's page; 1949 is supported by the booklet [V], *Israel, 1954* and *Railway World* 1973 (corridors note, Corroboration row 5) |
| 12 September 1949 | "The new railway station in north Tel Aviv will be finished this month" (headline) | *Haaretz*, 12 September 1949, cited by HE Bnei Brak station | [S] |
| 14 September 1949 | The Haifa-Tel Aviv line to open to passengers on the 20th: four trains a day, about 2¼ hours, by the existing track to Rosh HaAyin and the new line to the new station by the Yarkon bridge | *Haboker*, 14 September 1949, https://www.nli.org.il/he/newspapers/hbkr/1949/09/14/01/article/70 | [P] Agam, #384 |
| **20 September 1949** | Regular passenger service Haifa-Binyamina-Hadera East-Rosh HaAyin-Tel Aviv North; 2 hours 20 minutes; at Rosh HaAyin the engine moved from the southern to the northern end of the train (HE Yarkon [S]: there was no direct connection from the branch to the line north) | Israel Railways 1998 booklet, p. 15 | [V] |
| 20 September 1949 | Same day: "The railway along the lowland inaugurated" (headline) | *Maariv*, 20 September 1949, p. 1, https://www.nli.org.il/he/newspapers/mar/1949/09/20/01/article/1 , cited by HE Bnei Brak station and HE Tulkarm station | [S] |
| 21 September 1949 | Service on the shortened Tel Aviv-Haifa line opened (headline); a first-train report says the line ran tens to hundreds of metres from the border near Qalqilya and Tulkarm, and that about 2 km of track by Tulkarm station stayed under Jordan | *Al HaMishmar*, 21 September 1949, https://www.nli.org.il/he/newspapers/ahr/1949/09/21/01/article/11 ; *Davar*, 21 September 1949 (S. Zarhi); *HaTzofe*, 21 September 1949; cited by HE Eastern and HE IR | [S] |
| 22 September 1949 | First-days report: about 100 passengers on the first train from Tel Aviv, about 200 from Haifa | *Maariv*, 22 September 1949, https://www.nli.org.il/he/newspapers/mar/1949/09/22/01/article/21 | [P] Agam, #384 |
| 1949 | The Haifa-Tel Aviv line reopened, shortened by new sections, "including one skirting a salient on the Jordan frontier" | *Israel, 1954* (corridors note, Corroboration row 2) | [V on 2026-10-13, not re-read] |

**Verdict on the day.** 20 September 1949 is confirmed by Israel Railways' own history [V] and by Agam's reading of
*Haboker* (14 September, the announcement) and *Maariv* (22 September, the first days) [P]. I could not open either
paper. Three more same-week papers are cited by Wikipedia [S]. Nothing read contradicts the day.

### The Tulkarm bypass

- **Alignment: not given by any source read [G].** The sources say only "west of Tulkarm" (HE Eastern, EN Eastern), "a
  short stretch of new track just west of what was to become the Armistice Line" (EN PR, citing Cotterell 1984, p. 86
  [S]), "skirting a salient on the Jordan frontier" (*Israel, 1954*) and "bypassing Tulkarm station" (the 1998
  booklet [V]). About 2 km of the old line by Tulkarm station stayed under Jordan (HE IR, citing *Davar*, 21 September
  1949 [S]). No length, no end points and no map of the new track were found.
- **What brackets it (not its line).** The old Eastern Railway's stations nearest Tulkarm that stayed in use after 1949
  were Ahituv (Qaqun) to the north and Eyal to the south (HE Eastern [S]). Tulkarm station itself was cut off. These are
  reference points only; the bypass's ends lay somewhere between them, and no source says where.
- **Construction dates:** summer 1949 [V, booklet]; finished by 4 August 1949 [S, *Haaretz*]; freight from 21 August
  1949 [S, *Al HaMishmar* headline, not read]. The work start is not given [G]. The armistice with Jordan was signed on 3
  April 1949, so the work came after it (corridors note, R1).
- **Opening:** freight about 21 August 1949 [S]; passengers 20 September 1949 [V].
- **For the plan:** Q-RAIL-1 and Task 2.0 say that if no source gives the bypass's alignment, the corridor is the
  Yarkon link alone. That condition holds on the sources read. A Survey of Israel map of the early 1950s, or Cotterell
  1984 p. 86, might give the line; neither was reached.

### The Rosh HaAyin-Tel Aviv North (Yarkon) link

- **Route.** The 1921 branch ran from Rosh HaAyin South station north and then west to the Petah Tikva station
  (HE Yarkon [S]). In 1949 it was extended about 7 km west along the Yarkon to the new Tel Aviv North station, which was
  renamed Bnei Brak after 1954 (HE Bnei Brak station, HE Yarkon [S]; the 1998 booklet [V] gives the ends, not the
  length). The Petah Tikva station was left at the end of a spur off the new line and lost its passenger service (HE
  Yarkon [S]). So the new track ran from the branch near the Petah Tikva station to Tel Aviv North. Where exactly it left
  the branch is not given [G]. In 1953 the line was carried on west to Tel Baruch to meet the coastal railway (HE
  Yarkon [S]), so today's Yarkon railway between Petah Tikva and Bnei Brak lies on the 1949 link [U: no source says the
  line was never moved].
- **Dates:** built summer 1949 [V, booklet]; station due to be finished in September 1949 [S, *Haaretz*, 12 September
  1949]; opened 20 September 1949 with the passenger line [V].
- **Waypoints** (the plan's Task 2.0 traces between them):

| Waypoint | lat, lon | Source of the point | Role |
|---|---|---|---|
| Rosh HaAyin South station | 32.1042, 34.9347 | HE Rosh HaAyin South station [S]; `routes_1948.yaml` line 104 has the same point | East end of the existing branch; trains reversed here |
| Petah Tikva historic station | 32.0968, 34.8813 | HE Petah Tikva historic station [S] | End of the 1921 branch; the 1949 extension started near it |
| Tel Aviv North (today Bnei Brak) station | 32.1029, 34.8310 | HE and EN Bnei Brak station [S] | West end; new station, opened 20 September 1949 |

Rosh HaAyin to Petah Tikva is existing 1921 track; only Petah Tikva to Tel Aviv North is new. The straight line from
the Petah Tikva point to the Tel Aviv North point is about 4.8 km against the sources' "about 7 km", so the 1949 track
did not run straight; it followed the Yarkon (HE Bnei Brak station [S]).

**Waypoints of the whole 1949 route** (existing track, for the card or the map, not new build): Haifa East station
(32.8119, 35.0047, `routes_1948.yaml`); Binyamina (named in the booklet, not looked up); Hadera East station (32.4460,
34.9501, HE Hadera East station [S]); Ahituv / Qaqun station (32.3836, 35.0019, HE Ahituv station [S]); the bypass [G];
Eyal station (32.2116, 34.9639, HE Eyal station [S]); Rosh HaAyin South; Petah Tikva; Tel Aviv North, as above.

## 3. Q-COR-3: the Tel Aviv-Netanya coastal road

### Dates

| Date | Event | Source | Mark |
|---|---|---|---|
| August 1949 | The Tel Aviv-Netanya section announced | HE R2 [43966837] | [S] no citation for this sentence |
| November 1949 | Work begins at Netanya and at Tel Aviv at the same time | HE R2 (cited: *Davar*, 17 February 1950, "New roads being built") | [S] |
| more than a year before 28 July 1950 | Z. Berensohn, Director-General of the Ministry of Labour and National Insurance, at the opening: work "started more than a year ago" | *The Jerusalem Post*, 30 July 1950, p. 1 | [V] **[C]** with November 1949 |
| February 1950 | About half paved | HE R2 (cited: *Davar*, 17 February 1950) | [S] |
| end of March 1950 | More than half paved | HE R2 (cited: *Maariv*, 27 March 1950, "Birth of a road") | [S] |
| 27 July 1950 | Announcement: Golda Myerson to open the road at 11 the next morning; regular traffic from 2 p.m.; Egged bus fares to Natanya 125 pruta against 166 by the old road | *The Jerusalem Post*, 28 July 1950 (datelined Thursday) | [V] (OCR read, matches the corridors note) |
| **28 July 1950** | Officially opened "yesterday" (report datelined Saturday): a 26 km road, Natanya half an hour from Tel Aviv and Herzlia ten minutes; Golda Myerson cut a blue-and-white tape "just north of the Yarkon bridge"; the convoy left from Tel Aviv | *The Jerusalem Post*, 30 July 1950, p. 1 | [V] page image read |
| 28 July 1950 | Not finished when opened: "completed neither in its length nor in its width"; "an eight kilometre stretch of the seven metre wide highway at Wadi Falik is still to be completed", with a detour on a side track; a second carriageway planned | *The Jerusalem Post*, 30 July 1950, p. 3 | [V] page image read |

The report also calls the road "the first main road to be built since the completion of the Road of Courage" (p. 1) [V].

**The construction start [C].** "From November 1949 at both ends" stands on Hebrew Wikipedia and *Davar* of 17 February
1950 [S], not read. The Director-General's "more than a year ago" [V] puts the start before July 1949. The two may
measure different things (survey and earthworks against paving); no source read says so. The *Davar* and *Maariv*
articles of 1950 are the next step. For Q-COR-4, "at both ends" is [S] only.

### Waypoints

All named in *The Jerusalem Post*, 30 July 1950 [V] unless the row says otherwise. Points are approximate, from the
place's Wikipedia article.

| # | Waypoint | lat, lon | Source of the place | Source of the point |
|---|---|---|---|---|
| N1 | The northern end of the Yarkon bridge, Tel Aviv: Mayor Rokach spoke of "the northern end of the Yarkon bridge where the new road now starts" | 32.0958, 34.7800 (the Yarkon River's article point, not the bridge) [U] | JP p. 1 [V] | EN Yarkon River [S]. **Which bridge it was is not given [G]**; Tel Aviv's 1950 Yarkon crossings were not looked up |
| N2 | The entrance to Herzlia, reached "less than ten minutes" from Tel Aviv, where the Local Council chairman B. Z. Michaeli met the convoy | 32.1700, 34.8343 (HE Herzliya); the corridors note used 32.1653, 34.8458 (EN, 2026-10-13) | JP p. 3 [V] | [S] |
| N3 | Rishpon: "a horse-drawn cart was seen on the road near Rishpon" | 32.2006, 34.8244 | JP p. 1 [V] | HE Rishpon [S] |
| N4 | Wadi Falik (Nahal Poleg): the unfinished 8 km stretch and its detour; Netanya's mayor O. Ben Ami drove out here to meet the convoy | 32.2681, 34.8357 (the Nahal Poleg nature reserve point) [U] | JP pp. 1 and 3 [V] | EN Poleg [S]. Where the road crossed the stream is not given [G] |
| N5 | The entrance to Natanya: the road "enters the township between the area set aside for the new industrial zone and immigrants' quarters"; a ceremony beneath an archway at the entrance to the town | 32.3286, 34.8567 (`settlements_1948.yaml`); HE Netanya gives 32.3320, 34.8550 | JP p. 3 [V] | [S] |

The bus fare notice in the same issue (p. 2 or 3, OCR only, badly garbled) appears to list fare stages at Kfar
Shmaryahu and Shefayim (and "Shefayim settlement" against "Shefayim, on the road") [U: OCR]. If a human confirms it on
the scan, Kfar Shmaryahu (32.1863, 34.8211, HE) and Shefayim (32.2173, 34.8240, HE) can join the list between N2 and
N4. The order N1 to N5 follows the reports and the map; no source gives the line between the places, so the trace is
between waypoints, `status: draft`, as the plan's Task 2.0 requires.

## 4. Q-COR-1: the Burma Road and the Road of Valour (wording input; no decision)

### The Burma Road, 14 June 1948

| Date | Event | Source | Mark |
|---|---|---|---|
| night of 31 May - 1 June 1948 | A jeep convoy gets through to the Harel Brigade: "the unofficial breakthrough" | Isseroff, "Burma Road", zionism-israel.com (archived 15 April 2021), https://web.archive.org/web/20210415133543/https://zionism-israel.com/dic/Burma_Road.htm | [V] |
| 8 June 1948 | Solel Boneh starts work on the road "in earnest" | Isseroff | [V] |
| 10 June 1948 | First truck convoy over the road | HE Burma [43120821] | [S] |
| 11 June 1948 | "The first day of the cease fire, the road was used for the first time by completely motorized vehicle transport" | Isseroff | [V] **[C, minor]** with 10 June |
| **14 June 1948** | "It was not really complete until June 14, when UN inspectors pronounced the road paved" | Isseroff | [V] |
| 14 June 1948 | The UN approved the road's existence; an official ceremony for its opening the next day (15 June) | HE Burma (cited: *Israel 50*, June 1948 page) | [S] |
| 14 June 1948 | "The road was finally completed on June 14" | EN Burma [1355307500] (cited: Isseroff 2008; Rosenbloom 2001) | [S] |

So 14 June 1948 is the day the track was counted complete (paved, and accepted by the UN under the truce), not the
first day it carried supplies. Isseroff's page is a secondary web source; no 1948 newspaper or archive document was
read for the day [U].

### The Road of Valour, 7 December 1948

What the 7 December event was: **the official opening (inauguration) of the Road of Valour, a separate permanent road,
by Prime Minister Ben-Gurion and Chief of Staff Ya'akov Dori, with the cornerstone of its memorial laid and the name
"Road of Valour" given.** It was not the Burma Road's completion.

| Claim | Source | Mark |
|---|---|---|
| "Inaugurated yesterday" (8 December paper), opened by Ben-Gurion and Dori; also called the "Har El road", linking the State with Jerusalem | *Hayom*, 8 December 1948, p. 1, https://www.nli.org.il/he/newspapers/hayomjlm/1948/12/08/01/article/2 | [P] Agam, #384 |
| The ceremony: flag raised at 2 p.m., Dori read the order of the day, Ben-Gurion laid the cornerstone | *Haaretz*, 8 December 1948, https://www.nli.org.il/he/newspapers/haretz/1948/12/08/01/article/18 | [P] Agam, #384 |
| Same-day coverage, "From 'Burma' to the Road of Valour" | *Maariv*, 7 December 1948, p. 10, https://www.nli.org.il/he/newspapers/mar/1948/12/07/01/article/10 | [P] Agam, #384 |
| "The Road of Valour will be opened today" (headline) | *HaTzofe*, 7 December 1948, cited by HE Gvura | [S] |
| Built 4 September to 30 October 1948 by the Engineering Corps, 40.5 km, about 5 km a week, mostly 5 m wide | HE Gvura [43319595] (cited: *Maariv*, 7 December 1948; *HaTzofe*, 7 December 1948) | [S]; the start conflicts with Operation HaHar (corridors note, D3) [C] |
| The name was given at the opening; at that time it covered Mazkeret Batya to al-Qastal | HE Gvura (cited: *Israel Hayom*, 7 December 2018) | [S] |
| In July 1950, the Tel Aviv-Netanya road was "the first main road to be built since the completion of the Road of Courage" | *The Jerusalem Post*, 30 July 1950, p. 1 | [V] |

**Conflicting dates for the opening [C].** 12 December 1948 (Schubert and Vogel 1957; *50 Speeches* 2017; snippets,
corridors note row 4b). A footnote in HE Burma quotes Dori's order of the day "for the opening of the Road of Valour to
Jerusalem on 10 December 1948" (cited: Shlomo Shamir, *Bekhol Mehir - LeYerushalayim*, Maarachot, 1994, p. 452) [S,
not read]. Against both, two papers of 8 December report the opening as "yesterday" [P]. On the evidence, 7 December
1948 is the strongest; the 10 December footnote may give the order's printed date rather than the ceremony's, but no
source read says so [U].

**Wording input for the owner (not a decision).** The facts support wording that keeps the two roads apart, for example
the corridors document's "the Road of Valour, which replaced the Burma Road track". Wording that calls 7 December 1948
the Burma Road's opening or completion would not match the sources: the Burma Road was complete on 14 June 1948, and
7 December 1948 opened a different, permanent road on a southern line (Nahshon and Shimshon junctions to Sha'ar
HaGai; corridors note, D3).

## Unverified and conflicting

1. **No Hebrew newspaper was read by me.** The NLI site refused scripted access (HTTP 403). Every *Haboker*, *Maariv*,
   *Hayom*, *Haaretz*, *Davar*, *Al HaMishmar* and *HaTzofe* item above is [P] (Agam's browser read on #384, with his
   caveat that JPress OCR is automatic) or [S] (a Wikipedia citation). A human should open each linked page before a
   value is `fact-checked`.
2. **Tulkarm bypass alignment** [G]: no source gives it. Cotterell 1984 (p. 86) is unread and is cited for both August
   1948 (EN PR) and August 1949 (HE Tulkarm station) [C].
3. **Where the Yarkon link left the Petah Tikva branch** [G], and whether today's line is the 1949 line [U].
4. **Tel Aviv-Netanya construction start** [C]: November 1949 at both ends (HE R2, *Davar* 17 February 1950 [S]) against
   "more than a year ago" in July 1950 (JP [V]).
5. **Which Yarkon bridge** the road started from [G]; where it crossed Wadi Falik [G]; the fare stages in the JP notice
   [U, OCR].
6. **Burma Road first vehicles** [C, minor]: 10 June (HE Burma [S]) or 11 June 1948 (Isseroff [V]).
7. **Road of Valour opening** [C]: 7 December [P] against 12 December (two books, snippets) and 10 December (a footnote
   on Dori's order of the day [S]). Its build start [C]: 4 September 1948 against after Operation HaHar (October 1948).
8. **Camp sites at [S] level** (C3, C4, C5): place and date rest on Wikipedia and cited books not read (Turgan 2017,
   Petishi 2013, Gandler 2009, Nakdimon 1990). **C2's identity** [U]: Gshur's "Kfar Yona" camp and Wikipedia's "Camp Beit
   Lid" Gadna base (from 23 June 1948, uncited) may be the same place; nothing read says so. **C1's officers' school from
   May 1948** [U]: no citation.
9. **The Israel Railways booklet has at least one error**: it dates the first train to Jerusalem "17 August 1948" (p.
   15), where the press gives 7 August 1949 (corridors note, row 3a). Its 1949 rail facts above agree with the other
   sources, but it is a popular history, not a record.

## Sources

Read on 2026-10-15 unless noted. Wikipedia articles read through the API (revision in brackets); licence CC BY-SA 4.0.

Primary and scholarly, read:
- **Gshur 2025:** Benny Gshur, "מכובע גרב לכובע טמבל: הנח"ל בשנתו הראשונה" (Nahal's first year), *Hevra, Tzava
  uVitahon Leumi* 9 (July 2025), pp. 110-129, IDF Maarachot,
  https://www.maarachot.idf.il/media/qyen3yoj/מכובע-גרב-לכובע-טמבל-הנחל-בשנתו-הראשונה.pdf (PDF text read; pp. 114-115,
  120 and 126 used).
- **IR 1998:** Israel Railways, *50 שנה לרכבת ישראל* (50 Years of Israel Railways), booklet, September 1998, p. 15,
  archived at https://web.archive.org/web/20140327112217/http://www.rail.co.il/HE/Fun/Museum/Documents/50Years.pdf
  (scanned pages read as images).
- **JP:** *The Jerusalem Post*, 28 July 1950 (OCR text) and 30 July 1950 (page images of pp. 1 and 3),
  https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-07-28_26_7317 and
  https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-07-30_26_7318 .
- **Isseroff:** Ami Isseroff, "Burma Road", zionism-israel.com, archived 15 April 2021,
  https://web.archive.org/web/20210415133543/https://zionism-israel.com/dic/Burma_Road.htm (the live page answered 403).
- **Israel, 1954**, Railway World (May 1973) and the other corroboration sources: as listed in the corridors note
  (read 2026-10-13; not re-read).

Press read by Agam in a browser on 14 October 2026 (#384) [P]: *Haboker* 14 September 1949; *Maariv* 22 September 1949;
*Hayom* 8 December 1948; *Haaretz* 8 December 1948; *Maariv* 7 December 1948 (links in the tables above).

Wikipedia, used as a pointer to the sources each row names:
- HE C80: "בט"ר דותן" (Camp 80) [43463565]; HE Bahad 1: "בית הספר לקצינים של צה"ל" [44064227]; HE Bahad 2: "קורס מפקדי
  פלוגות ומפקדי גדודים" [43845695]; HE Bahad 3: "בה"ד 3" [43853162]; HE Bahad 4: "בה"ד 4" [43873223]; HE Bahad 12:
  "בה"ד 12" [42361926]; HE Dora pool: "בריכת דורה" [43444047]; HE Gur: "מחנה גור" [43696149]; HE Gadna: "גדנ"ע"
  [43528484]; HE Nahal: "נח"ל" [43851153]; HE Tzrifin: "צריפין" [43963697]; HE Tel HaShomer camp: "מחנה דורי"
  [43522123]; HE Sirkin: "מחנה סירקין" [42890514]; HE Flying School: "בית הספר לטיסה" [44005589]; HE Ju'ara: "ג'וערה"
  [43491462]; HE Kfar Yona: "כפר יונה" [44063210].
- HE Eastern: "המסילה המזרחית ההיסטורית" [43519136]; HE Tulkarm station: "תחנת הרכבת טולכרם" [43123727]; HE Yarkon:
  "מסילת הירקון" [42711895]; HE Bnei Brak station: "תחנת הרכבת בני ברק – רמת החייל" [43883317]; HE Petah Tikva historic
  station: "תחנת הרכבת ההיסטורית של פתח תקווה" [42979840]; HE Rosh HaAyin South station [43439919]; HE Hadera East
  station [43875718]; HE Ahituv station [43556394]; HE Eyal station [43858707]; HE Coastal: "מסילת החוף" [44068998];
  HE IR: "רכבת ישראל" [43876266]; EN Eastern: "Eastern Railway (Israel)" [1357861699]; EN Bnei Brak station [1370812274].
- HE R2: "כביש 2" [43966837]; HE Herzliya [44065797]; HE Rishpon [43624312]; HE Netanya [43931296]; HE Kfar Shmaryahu
  [43987536]; HE Shefayim [43556388]; EN Poleg [1362895175]; EN Yarkon River [1368197351].
- HE Burma: "דרך בורמה (ישראל)" [43120821]; EN Burma: "Burma Road (Israel)" [1355307500]; HE Gvura: "כביש הגבורה"
  [43319595].

Not reachable on 2026-10-15: nli.org.il (all newspaper pages, HTTP 403); iaf.org.il (403); zionism-israel.com live
(403; archived copy used); Cotterell 1984 (no copy found on the Internet Archive).
