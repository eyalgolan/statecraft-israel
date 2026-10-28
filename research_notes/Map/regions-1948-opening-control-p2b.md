# Opening control on 15 May 1948: Central Plain, Jerusalem Hills, Northern Hills, Southern Coast and Negev

Web research by an AI agent, 27 Oct 2026. A person still needs to check the history, sources and quotations. This note covers opening control in Central Plain, Jerusalem Hills, Northern Hills, Southern Coast and Negev, including Kiryat Shmuel camp. Sources: Palmach Heritage, IDF accounts, Knesset, Lapidot, FRUS, Dupuy, contemporary press, Zochrot, Wikipedia and the public repository notes identified below. Access limits and uncertainty remain part of the evidence.


Research date: 2026-10-27.
Scope: every P2 item with field "control on 15 May 1948" in central_plain, jerusalem_hills, northern_hills,
southern_coast and negev, plus gdd03-0731 (Kiryat Shmuel camp). That is 181 items. This was web research only.



**Research rule.** Control is never inferred from ethnicity, geometry,
armistice lines or later events.
- A capture after 15 May shows only that the Yishuv did not yet hold the place. Such items say "not-Yishuv
  (holder unresolved)" and are PARTIAL.
- A capture before 15 May counts as Yishuv-held only where the source says the Yishuv occupied or held it.

## Status definitions

- **SOURCED:** a fetched, dated, place-specific source states who held the place on or within about 1-2 days of
  14-15 May.
- **PARTIAL:** the source pins only one side, or the evidence is region-level, a snippet, or Wikipedia only.
- **NEEDS-RESEARCH:** no dated evidence was found.

## Reading the repo codes

The "repo opening code" is the first period character of the place's hex in
`origin/main:content/map/control_hexes_1948_49.yaml`. That period is the 9 July 1948 lines adjusted, not a traced
15 May line.

The hex comes from one of three places:
- `content/map/settlement_hexes.yaml`, where the place has a row;
- the proposed association row's hex;
- otherwise, computed with the repository placement rule from the proposed association lon/lat.

Many villages share a 5 km hex with a Jewish settlement, so a hex code cannot represent both. A "CONTRADICTION"
means a fetched source conflicts with the code for the place's own hex. It may come from the hex scale rather
than from a wrong polygon.

## Source access limits

- palestineremembered.com and palquest.org returned HTTP 403 throughout.
  Access remained unavailable; no bot-check workaround was pursued.
- The archive.org copies of Morris (2004, 2008) need a login.
- As a result, Khalidi and Morris page numbers are cited as Wikipedia or Zochrot gives them. A person must check
  them in the books.
- Primary-ish sources actually fetched:
  - Palmach Heritage database pages;
  - IDF text (via the Jewish Virtual Library);
  - the Knesset "Liberation of Jerusalem" PDF;
  - Lapidot, "Besieged" (daat.ac.il);
  - FRUS 1948 vol. V;
  - Dupuy, *Elusive Victory* (Stanford excerpt PDF, printed pages);
  - UP and AP dispatches of 16 and 23 May 1948 (cojs.org reprints);
  - Zochrot village pages, which give a dated occupation-date and unit field;
  - moreshet-map.org.il.
- Sefer HaPalmach and Toldot HaHagana were not found online as searchable text. They are cited only where a
  Palmach or Wikipedia page quotes them.

## Research checks

- Two contradiction sources were re-fetched:
  - Zochrot 49367 (Sarafand el-'Amar): "Occupation date: 20/05/1948", occupying unit "גבעתי".
  - en.wiki Tell es-Safi: depopulation "9-10 July 1948", "Military assault by Yishuv forces" (Morris 2004 p. xix),
    with the Givati order of 7 July (Morris 2004 p. 436).
  - Both confirmed.
- gdd03-0688 (Iraq Suwaydan fort) was downgraded from SOURCED to PARTIAL, because its evidence is
  Wikipedia only.

## Counts by status

| region | SOURCED | PARTIAL | NEEDS-RESEARCH | items |
|---|---|---|---|---|
| central_plain | 5 | 26 | 3 | 34 |
| jerusalem_hills | 3 | 55 | 5 | 63 |
| northern_hills | 0 | 36 | 0 | 36 |
| southern_coast | 5 | 30 | 3 | 38 |
| negev | 1 | 1 | 7 | 9 |
| unbound_reception | 0 | 1 | 0 | 1 |
| **total** | **14** | **149** | **18** | **181** |

## Places where repo control data conflicts with a fetched source

Hard conflicts: the source says the Yishuv did not hold the place on 15 May, but the repo opening code is `I`.

| id | place | repo opening | source (as fetched) |
|---|---|---|---|
| gdd03-0353 | Sarafand el-'Amar | I | Zochrot 49367: "Occupation date: 20/05/1948", Givati (re-checked) |
| gdd03-0361 | Qubeiba (Ramle) | I | Zochrot gives occupation on 27/05/1948; en.wiki gives 27 May (en.wiki Ness Ziona gives 19 May) |
| gdd03-0375 | Nabi Rubin | I | Zochrot gives 01/06/1948; en.wiki (Morris p. 256) says the 10-12 May raids were "apparently without success" |
| gdd03-0399 | Tzrifin (Sarafand camp) | I | he.wiki, citing Haaretz and Al HaMishmar of 20 May 1948: the British left at 09:00 on 15 May and "כוחות ערבים נכנסו אליו"; Givati took it on 18-19 May |
| gdd03-0502 | Tell es-Safi | I (all periods) | en.wiki (Morris 2004 pp. 436-437, p. xix): Givati assault, 9-10 July 1948 (re-checked) |
| gdd03-0648 | Masmiya el-Kabira | I (all periods) | taken in Operation An-Far, 8-11 July 1948 (Morris p. xvii; Khalidi p. 125, via en.wiki) |
| gdd03-0652 | Barbara | I | AP of 23 May 1948 quotes an Egyptian communique: the army "occupied ... Barhara" about 21 May; Israel took it on 5 Nov (hex shared with Yad Mordechai) |
| gdd03-0654 | Hirbiya | I | the same AP communique: "occupied Hirbiya" about 21 May; fell in early Nov (hex shared with Yad Mordechai) |
| gdd03-0666 | Karatiya | I | captured on 18 July 1948 by the 89th Battalion (Morris p. 437 via en.wiki; hex shared with Negba) |
| gdd03-0688 | Iraq Suwaydan police fort | I | the British handed it over on 12 May; Givati attacks on 14, 18-19 and 21-22 May failed; it fell on 9 Nov (en.wiki, Tal p. 177; hex shared with Negba) |

Probable conflicts. The repo opening code is `A`, but secondary sources say the Yishuv held the place:

| id | place | repo opening | source |
|---|---|---|---|
| gdd03-0456 | Beit Mahsir | A | Harel occupied it on 11 May (he.wiki citing Narkiss pp. 90-92; en.wiki citing Morris); Israeli troops were there on 13 May (Morris & Kedar 2022 p. 7) |
| gdd03-0528 | Bab al-Wad / Sha'ar HaGai | A | Harel held the heights on 9-11 May (Morris 2008 p. 132 via en.wiki); the Legion blocked the road by 18 May; he.wiki describes a vacuum on 15-17 May |

Mismatches that are not hard conflicts (for the owner):

- **Repo `c` (contested), sources read Arab-held:** Yibna and Zarnuqa had armed Arab presence until their capture
  between 27 May and 5 June. Zir'in had Arab irregulars until Golani took it on 28 May (Dupuy p. 51).
- **Repo `c`, sources show an earlier Givati capture:** Givati took Bashshit on 11 May, but no source says it
  still held the village on 15 May.
- **Repo `I`, sources disagree on Qubab:** it was taken on 14 May per en.wiki "Battles of Latrun" and he.wiki,
  but Zochrot gives 6 June and Morris dates the depopulation to 15 July.
- **Repo `-` (no colour), sources show Yishuv-held:**
  - Beit Daras: Givati from 11 May (secondary sources; Sudanese troops are said to have reoccupied it in June).
  - Bureir: occupied 12-13 May (Palmach Heritage).
  - Revivim: held out throughout.
- **Repo `I`, no dated source:** Beit Kama and Beit Kama fields look back-projected from later lines.
- **Old City (`content/map/locations_1948.yaml:215`):** the record gives `starting_control: israeli` for the
  whole Old City marker, under an owner ruling of 18 Oct. The fetched sources (Knesset; Lapidot) support only the
  Jewish Quarter as Yishuv-held. Arab fighters held the rest, and the Legion entered on 18 May. The ruling is the
  owner's; this is flagged, not contradicted.
- **Latrun police fort (repo `A`):** sources conflict on 15 May. Lapierre & Collins p. 611 say a Givati platoon
  was in the fort; Morris says a Legion platoon with irregulars held it.
- **Beit Shemesh hex (-3, 24), repo `A`:** Jewish Hartuv was held until Givati evacuated it on the night of
  15-16 May. This conflicts with the repo only if the hex centre is Hartuv's ground.
- **Nitzanim:** the place has no repo hex or code. Its 1948 site was about 2 km north-west of today's kibbutz.


## Summary table (all 181 items, by id)

| id | place (place_id) | holder on 15 May 1948 | status | confidence | repo opening code | contradiction? |
|---|---|---|---|---|---|---|
| gdd03-0337 | Lydda (lod) | not-Yishuv (holder unresolved; local militia, Arab Legion element reported) | PARTIAL | medium | A | no |
| gdd03-0338 | Ramle (ramla) | not-Yishuv (holder unresolved; local national committee, Arab Legion element reported) | PARTIAL | medium | A | no |
| gdd03-0339 | Rishon LeZion (rishon_lezion) | Yishuv/Haganah | PARTIAL | high | I | no |
| gdd03-0340 | Rehovot (rehovot) | Yishuv/Haganah | PARTIAL | high | I | no |
| gdd03-0341 | Yibna (yibna) | Arab irregulars / local militia | PARTIAL | medium | c | no (possible: repo c, sources show Arab-held until 4-5 June) |
| gdd03-0343 | Aqir (aqir) | Yishuv/Haganah (Givati) | SOURCED | medium | I | no |
| gdd03-0345 | Zarnuqa (zarnuqa) | Arab irregulars / local militia | PARTIAL | medium | c | no (possible: as Yibna) |
| gdd03-0347 | Beit Nabala (bayt_nabala) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no (the sources disagree with each other) |
| gdd03-0349 | Na'ana (naana) | Yishuv/Haganah (Givati, taken 14 May) | SOURCED | medium | I | no |
| gdd03-0351 | Qubab (al_qubab) | mixed/contested (sources disagree) | PARTIAL | low | I | possible (the sources conflict) |
| gdd03-0353 | Sarafand el 'Amar (sarafand_al_amar) | not-Yishuv (Arab forces entered the camp next door on 15 May; village holder unresolved) | PARTIAL | medium | I | **CONTRADICTION** |
| gdd03-0355 | Ness Ziona (ness_ziona) | Yishuv/Haganah | PARTIAL | high | I | no |
| gdd03-0357 | Deir Tarif (dayr_tarif) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0359 | Mughar (al_maghar) | mixed/contested (Givati attack and capture on 15 May) | SOURCED | medium | c | no |
| gdd03-0361 | Qubeiba (al_qubayba_ramla) | not-Yishuv (holder unresolved) | PARTIAL | medium | I | **CONTRADICTION** |
| gdd03-0363 | Bashshit (bashshit) | Yishuv/Haganah (Givati took it 11 May; whether it was held on 15 May is not stated) | PARTIAL | medium | c | possible (repo c against a capture on 11 May) |
| gdd03-0365 | Wadi Hunein (wadi_hunayn) | Yishuv/Haganah | SOURCED | medium | I | no |
| gdd03-0367 | Majdal Yaba (majdal_yaba) | not-Yishuv (holder unresolved; Iraqi army there by July) | PARTIAL | medium | A | no |
| gdd03-0369 | Jimzu (jimzu) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0371 | Imwas (imwas) | Arab irregulars, with the first Arab Legion troops arriving (region-level) | PARTIAL | medium | A | no |
| gdd03-0373 | Inaba (innaba) | not-Yishuv (holder unresolved; villagers defended it in July) | PARTIAL | medium | A | no |
| gdd03-0375 | Nabi Rubin (nabi_rubin) | not-Yishuv (holder unresolved) | PARTIAL | medium | I | **CONTRADICTION** |
| gdd03-0377 | Ni'lin (nilin) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0379 | Tira (al_tira) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0381 | Rantis (rantis) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0383 | Qibya (qibya) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0385 | Beit Nuba (bayt_nuba) | Arab irregulars, with the first Arab Legion troops arriving (region-level) | PARTIAL | low | A | no |
| gdd03-0387 | Yalu (yalo) | Arab irregulars, with the first Arab Legion troops arriving (region-level) | PARTIAL | low | A | no |
| gdd03-0389 | Qatra (qatra) | Yishuv/Haganah (Givati) | SOURCED | medium | I | no |
| gdd03-0391 | Muzeiri'a (al_muzayria) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0393 | Sarafand el Kharab (sarafand_al_kharab) | Yishuv/Haganah (probable) | PARTIAL | medium | I | no |
| gdd03-0395 | Givat Brenner (givat_brenner) | Yishuv/Haganah | PARTIAL | high | I | no |
| gdd03-0397 | Qula (qula) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0399 | Tzrifin (tzrifin) | Arab forces (irregulars or villagers) entered on 15 May after the British left | PARTIAL | medium | I | **CONTRADICTION** |
| gdd03-0413 | West Jerusalem (jerusalem) | Yishuv/Haganah (with IZL) | SOURCED | high | m | note |
| gdd03-0414 | Hebron (hebron) | not-Yishuv | PARTIAL | low | A | no |
| gdd03-0416 | Dura (dura_hebron) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0418 | Bethlehem (bethlehem) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0420 | Yatta (yatta) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0422 | Ramallah (ramallah) | not-Yishuv; Arab Legion deployment planned | PARTIAL | medium | A | no |
| gdd03-0424 | Silwan (silwan) | unresolved | PARTIAL | low | m | no |
| gdd03-0426 | Dhahiriya (al_dahiriya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0428 | Ajjur (ajjur) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0430 | Dawayima (al_dawayima) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0432 | Beit Jala (beit_yala) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0434 | Halhul (halhul) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0436 | Ein Karim (ein_karem) | not-Yishuv | PARTIAL | medium | c | no |
| gdd03-0438 | Jericho (jericho) | Arab Legion | SOURCED | medium | A | no |
| gdd03-0440 | Bira (al_bireh) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0442 | Tur (at_tur) | unresolved | PARTIAL | low | m | no |
| gdd03-0444 | Beit Sahur (beit_sahour) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0446 | Si'ir (sair) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0448 | Lifta (lifta) | unresolved | PARTIAL | low | c | no |
| gdd03-0450 | Samu' (as_samu) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0452 | Sur Bahir (sur_bahir) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0454 | Beit Jibrin (bayt_jibrin) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0456 | Beit Mahsir (bayt_mahsir) | Yishuv/Haganah | PARTIAL | medium-high | A | CONTRADICTION (probable) |
| gdd03-0458 | Idna (idna) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0460 | Surif (surif) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0462 | Bani Na'im (bani_naim) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0464 | Beit Nattif (bayt_nattif) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0466 | Deiraban (dayr_aban) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0468 | Deir Dibwan (deir_dibwan) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0470 | Abu Dis (abu_dis) | unresolved | PARTIAL | low | m | no |
| gdd03-0472 | Malha (malha) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0474 | Silwad (silwad) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0476 | Beit 'Ummar (beit_ummar) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0478 | Walaja (al_walaja) | not-Yishuv | PARTIAL | medium | A | no |
| gdd03-0480 | Beit Hanina (beit_hanina) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0482 | Bir Zeit (birzeit) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0484 | Tarqumiya (tarqumiyah) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0486 | Beituniya (beitunia) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0488 | Beit Fajjar (beit_fajjar) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0490 | Beit Iksa (beit_iksa) | unresolved | NEEDS-RESEARCH | low | c | no |
| gdd03-0492 | Beit Safafa (beit_safafa) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0494 | Mazra'a esh Sharqiya (al_mazraa_ash_sharqiya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0496 | Taiyiba (taybeh_ramallah) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0498 | Sinjil (sinjil) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0500 | Beit Aula (beit_ula) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0502 | Tell es Safi (tell_es_safi) | not-Yishuv | PARTIAL | medium | I | CONTRADICTION |
| gdd03-0504 | Shuyukh (ash_shuyukh) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0506 | Zakariya (az_zakariyya) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0508 | Qatanna (qatanna) | not-Yishuv | PARTIAL | low | A | no |
| gdd03-0510 | Khadr (al_khader) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0512 | Kafr Malik (kafr_malik) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0514 | Abud (aboud) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0516 | Deir Jarir (deir_jarir) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0518 | Eizariya (al_eizariya) | unresolved | PARTIAL | low | m | no |
| gdd03-0520 | Qubeiba (al_qubayba_hebron) | not-Yishuv (holder unresolved) | PARTIAL | medium | A | no |
| gdd03-0522 | Battir (battir) | unresolved | NEEDS-RESEARCH | low | A | no |
| gdd03-0524 | Beit Liqya (beit_liqya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0526 | Hulda (hulda) | Yishuv/Haganah | PARTIAL | medium | I | no |
| gdd03-0527 | Latrun police fort (latrun_fort) | mixed/contested | PARTIAL | medium for "contested", low for any single holder | A | no |
| gdd03-0528 | Bab al-Wad (Sha'ar HaGai) (shaar_hagai) | Yishuv/Haganah | PARTIAL | medium | A | CONTRADICTION (probable) |
| gdd03-0529 | East Jerusalem / Old City (east_jerusalem_old_city) | mixed/contested | SOURCED | medium | m | note |
| gdd03-0530 | Beit Shemesh (beit_shemesh) | mixed/unresolved | PARTIAL | medium for Hartuv, low for the site | A | note |
| gdd03-0532 | Beit Shemesh fields (beit_shemesh_field) | mixed/unresolved | PARTIAL | medium for Hartuv, low for the site | A | note |
| gdd03-0540 | Nablus (nablus) | not-Yishuv (holder unresolved; Arab Legion 1st Brigade heading toward Nablus on 15 May, ALA HQ there per Dupuy) | PARTIAL | medium | A | no |
| gdd03-0542 | Tubas (tubas) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0544 | Umm el Fahm (umm_al_fahm) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0546 | Jenin (jenin) | not-Yishuv (holder unresolved; local irregulars likely, Iraqis arrive ~1 June) | PARTIAL | medium | A | no |
| gdd03-0548 | Arraba (arraba_2) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0550 | Qabatiya (qabatiya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0552 | Ya'bad (yabad) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0554 | Silat edh Dhahr (silat_adh_dhahr) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0556 | Buraq (burqa_nablus) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0558 | Yamun (al_yamun) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0560 | Kafr Ra'i (kafr_rai) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0562 | Jaba' (jaba_2) | not-Yishuv (holder unresolved; ALA HQ here in March 1948) | PARTIAL | low | A | no |
| gdd03-0564 | Tammun (tammun) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0566 | Aqraba (aqraba) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0568 | Asira esh Shamaliya (asira_ash_shamaliya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0570 | Silat el Harithiya (silat_al_harithiya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0572 | Salfit (salfit) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0574 | Talluza (talluza) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0576 | Beita (beita) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0578 | Birqin (burqin) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0580 | Awarta (awarta) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0582 | Zir'in (zirin) | Arab irregulars / local militia (as of 28 May; 15 May not directly stated) | PARTIAL | medium | c | no |
| gdd03-0584 | Bidya (biddya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0586 | Meithalun (meithalun) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0588 | Qabalan (qabalan) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0590 | Huwwara (huwara) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0592 | Beit Furik (beit_furik) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0594 | Jamma'in (jammain) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0596 | Kafr Qaddum (kafr_qaddum) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0598 | Burin (burin) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0600 | Deir Istiya (deir_istiya) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0602 | Qusra (qusra) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0604 | Tell (tell_nablus) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0606 | Sanur (sanur_jenin) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0608 | Sabastiya (sebastia) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0610 | Barta'a (bartaa) | not-Yishuv (holder unresolved) | PARTIAL | low | A | no |
| gdd03-0626 | Gaza (gaza) | Egyptian army (entry claimed for 15 May) | PARTIAL | medium | - | no |
| gdd03-0627 | Yad Mordechai (yad_mordechai) | Yishuv/Haganah (Palmach + kibbutz) | SOURCED | high | I | no |
| gdd03-0628 | Khan Yunis (khan_yunis) | not-Yishuv (holder unresolved; Egyptian coastal column from 15 May) | PARTIAL | medium | - | no |
| gdd03-0630 | Majdal (al_majdal) | not-Yishuv (holder unresolved; Egyptian entry ~21 May) | PARTIAL | medium | - | no |
| gdd03-0632 | Hamama (hamama) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0634 | Isdud (isdud) | not-Yishuv (holder unresolved; Egyptian from 29 May) | PARTIAL | medium | - | no |
| gdd03-0636 | Faluja (al_faluja) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0638 | Jabaliya (jabalia) | not-Yishuv (holder unresolved; Egyptian army at Gaza from 15 May) | PARTIAL | low | - | no |
| gdd03-0640 | Bani Suheila (bani_suheila) | not-Yishuv (holder unresolved; Egyptian coastal column) | PARTIAL | low | - | no |
| gdd03-0642 | Beit Daras (bayt_daras) | Yishuv/Haganah (Givati, from 11 May) | PARTIAL | medium | - | no (mismatch: repo uncoloured) |
| gdd03-0644 | Bureir (burayr) | Yishuv/Haganah (Palmach Negev Brigade, 12-13 May) | SOURCED | medium | - | no (mismatch: repo uncoloured) |
| gdd03-0646 | Deir el Balah (deir_al_balah) | not-Yishuv (Egyptian forces / Muslim Brotherhood at adjacent Kfar Darom 15 May) | PARTIAL | medium | - | no |
| gdd03-0648 | Masmiya el Kabira (al_masmiyya_al_kabira) | not-Yishuv (holder unresolved; captured July) | PARTIAL | medium | I | CONTRADICTION |
| gdd03-0650 | Jura (al_jura) | not-Yishuv (holder unresolved; Egyptian ~21 May) | PARTIAL | medium | - | no |
| gdd03-0652 | Barbara (barbara_gaza) | not-Yishuv (holder unresolved; Egyptian ~21 May) | PARTIAL | medium | I | CONTRADICTION |
| gdd03-0654 | Hirbiya (hiribya) | not-Yishuv (holder unresolved; Egyptian ~21 May) | PARTIAL | medium | I | CONTRADICTION |
| gdd03-0656 | Abasan (abasan_al_kabira) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0658 | 'Iraq el Manshiya (iraq_al_manshiyya) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0660 | Rafah (rafah) | Egyptian army crossing the frontier 15 May (place not named) | PARTIAL | medium | - | no |
| gdd03-0662 | Beit Hanun (beit_hanoun) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0664 | Beit Lahiya (beit_lahia) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0666 | Karatiya (karatiyya) | not-Yishuv (holder unresolved; captured 18 July) | PARTIAL | medium | I | CONTRADICTION |
| gdd03-0668 | Sumsum (simsim_gaza) | Yishuv raid/occupation 12-15 May, continuity unclear (mixed/contested) | PARTIAL | low | I | no |
| gdd03-0670 | Nazla (nazla) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0672 | Ni'ilya (niilya) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0674 | Jiya (al_jiyya) | not-Yishuv (holder unresolved) | PARTIAL | low | - | no |
| gdd03-0676 | Juseir (jusayr) | not-Yishuv (holder unresolved; captured 17-18 July) | PARTIAL | medium | - | no |
| gdd03-0678 | Yasur (yasur) | not-Yishuv (holder unresolved; assaulted 9 June) | PARTIAL | medium | - | no |
| gdd03-0680 | Beit Tima (bayt_tima) | not-Yishuv (holder unresolved; Negev Brigade claim 30/31 May) | PARTIAL | medium | - | no |
| gdd03-0682 | Sawafir el Gharbiya (al_sawafir_al_gharbiyya) | mixed/contested (Givati raids and withdrawals; second conquest 18 May) | PARTIAL | low | - | no |
| gdd03-0684 | Julis (julis_2) | not-Yishuv (holder unresolved; taken 11 June) | PARTIAL | medium | - | no |
| gdd03-0686 | Negba (negba) | Yishuv/Haganah (kibbutz defenders) | SOURCED | medium | I | no |
| gdd03-0687 | Nir Am (nir_am) | Yishuv/Haganah (Palmach Negev Brigade HQ) | SOURCED | medium | I | no |
| gdd03-0688 | Iraq Suwaydan police fort (iraq_suwaydan_fort) | Arab: local villagers / Egyptian (sources disagree on type); Givati attack failed 14 May | PARTIAL (downgraded from SOURCED, Wikipedia-only) | medium | I | CONTRADICTION |
| gdd03-0689 | Jisr Isdud / Ad Halom bridge (ad_halom_bridge) | unresolved | NEEDS-RESEARCH | low | - | no |
| gdd03-0690 | Mefalsim (mefalsim) | unresolved (site of a 1949 founding) | NEEDS-RESEARCH | low | - | no |
| gdd03-0692 | Mefalsim fields (mefalsim_field) | unresolved (design placement on the Mefalsim hex) | NEEDS-RESEARCH | low | - | no |
| gdd03-0694 | Nitzanim (nitzanim) | Yishuv/Haganah (Givati 53rd Bn platoons + kibbutz) | SOURCED | medium | (none: no repo hex/code in the group file) | no |
| gdd03-0712 | Beersheba (beersheba) | not-Yishuv (holder unresolved; Arab town, Egyptian advance guard arrives 17 May) | PARTIAL | medium | - | no |
| gdd03-0713 | Revivim (revivim) | Yishuv/Haganah (kibbutz garrison, Palmach-reinforced) | SOURCED | medium | - | no (mismatch: repo uncoloured) |
| gdd03-0714 | Beit Kama (beit_kama) | unresolved | NEEDS-RESEARCH | low | I | no (repo I unsourced) |
| gdd03-0716 | Beit Kama fields (beit_kama_field) | unresolved | NEEDS-RESEARCH | low | I | no (repo I unsourced) |
| gdd03-0718 | Re'im (reim) | unresolved | NEEDS-RESEARCH | low | - | no |
| gdd03-0720 | Re'im fields (reim_field) | unresolved | NEEDS-RESEARCH | low | - | no |
| gdd03-0722 | Gilat (gilat) | unresolved | NEEDS-RESEARCH | low | - | no |
| gdd03-0724 | Gilat fields (gilat_field) | unresolved | NEEDS-RESEARCH | low | - | no |
| gdd03-0726 | Umm Rashrash / future Eilat (umm_rashrash) | unresolved (not Yishuv-held; first Israeli arrival 10 March 1949) | NEEDS-RESEARCH | low | none (no hex) | no |
| gdd03-0731 | Kiryat Shmuel camp, Haifa (kiryat_shmuel_camp_haifa) | unresolved (Haganah-held Haifa Bay area vs British Haifa enclave) | PARTIAL (human check required) | low | none (no hex) | no |

# Per-item sections

Sections are grouped by region. Shared-evidence labels (R1, R-BARAK, R-LAT and so on) are local to their region block.

## Central Plain: research notes and sections


Web research, 2026-10-27.

Access notes (checked 27 Oct 2026):
- palestineremembered.com returned HTTP 403 on both access attempts, so no Khalidi or Morris text was read there.
- The archive.org copies of Morris (2004: `birthofpalestini0000morr_e6e3`; 2008: `1948historyoffir00morr`) need a login (401). Search-inside said "Item not available".
- The palquest copy of Munayyer's "The Fall of Lydda" returned 403. The web.archive.org copy is a scanned PDF with no text layer, so it was not read.
- What was used instead:
  - Zochrot village pages (zochrot.org/villages/village_details/<id>/en). Each has dated "Occupation date" and "Occupying unit" fields, and some reproduce Khalidi's "All That Remains" text.
  - The raw wikitext of en.wikipedia and he.wikipedia articles, fetched 2026-10-27 at the revision ids given.
  - The public repository notes at origin/main.
- Every quote below was copied from text fetched on 2026-10-27.
- Wikipedia-only items are capped at PARTIAL, as the research criteria require. Zochrot is a secondary database, but its fields are dated and place-specific, so an item counts as SOURCED where a Zochrot field and another source agree on the occupation date and unit.
- Zochrot URLs below use the pattern `https://www.zochrot.org/villages/village_details/<id>/en`.

### Summary table


Counts: SOURCED 5, PARTIAL 26, NEEDS-RESEARCH 3. CONTRADICTION 4 (plus 5 "possible" flags).

Region-level sources used several times (each fetched 2026-10-27):
- **R-LAT**: en.wikipedia "Battles of Latrun (1948)", rev 1370603666, raw wikitext. Event dates 12-18 May 1948. It is secondary; its sources are Gelber pp. 138-145, Morris 2002 p. 152, Morris 2008 pp. 207-24, and Lapierre and Collins.
  - "Between 14 and 15 May, its 52nd battalion took the villages of [[Abu Shusha]], [[Al-Na'ani]] and [[al-Qubab]] north of Latrun, thus cutting off the zone from Ramla"
  - "around 14–15 May, an order was given to [[Fawzi al-Qawuqji]] and his Arab Liberation Army to withdraw and to leave the place to the [[Arab Legion]]. According to [[Yoav Gelber]], this departure occurred before the arrival of the Jordanian troops at Latrun and the position was held by just 200 irregulars. [[Benny Morris]] nevertheless points out that a platoon of legionnaires of the 11th Company along with irregulars was there and took over the fort."
  - "During the night between 15 and 16 May, the first contingent of 40 legionnaires seconded by an undetermined number of [[Bedouin]]s strengthened the position, and the remainder of the regiment reached the area on 17 May. On 18 May, the strength of the Arab Legion deployed around Latrun and Bab al-Wad was sufficient, and the road was blocked again."
- **R-LAT-HE**: he.wikipedia "קרבות לטרון", rev 43792411. Event dates 14-18 May.
  - "אחרי עזיבת הבריטים בליל 14 במאי 1948, הוצאו גם כוחותיו של קאוקג'י מאזור לטרון ... כתוצאה מכך נותר אזור לטרון ריק כמעט לחלוטין מכוחות ערביים במשך כשלושה ימים"
  - "בימים 15–18 במאי השתלטו כוחות מחטיבות "גבעתי" ו"הראל" באופן זמני על מספר נקודות מפתח ... אך בכל פעם נטשו הכוחות עם אור היום"
- **R-BARAK**: he.wikipedia "מבצע ברק", rev 43021951. Event dates 4-31 May 1948. It cites Slutsky's Toldot HaHagana (abridged) pp. 481-482; Gelber, "Komemiyut VeNakba", p. 179; and Morris, "1948", p. 185.
- **R-CIA**: repo `research_notes/Map borders/control-15-may-1948.md`, line 154. The note reads the CIA 18 May 1948 map (map 10981) as putting "the Rishon-Sarafand area inside a dashed (tenuous) border". This was not re-read here; the map image could not be rendered.

---

#### gdd03-0337 - Lydda (lod)
- Holder on 15 May 1948: not-Yishuv. The holder type is unresolved: local Arab authority and militia, with an Arab Legion element reported from April.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49245 (al-Lydd). Event date: 11 July 1948. Quote: "Occupation date: 11/07/1948". Place-specific. It shows that the Yishuv did not hold Lydda on 15 May.
  - en.wikipedia "Palestinian expulsion from Lydda and Ramle", rev 1372785153, raw wikitext. Event dates: April-June 1948. Place-specific, secondary. The paragraph cites Khalidi 1998, "The Fall of Lydda", JPS 27(4), p. 81; the Morris page is not shown in the raw ref. Quote: "According to Morris, a number of Arab Legion soldiers, including 200–300 Bedouin volunteers, had arrived in Lydda and Ramle in April, and a company-sized force had set itself up in the former [[Palestine Police Force]] stations in Lydda and on the Lydda-Ramle road". Same page: "Lydda's local Arab authority, officially subordinated to the [[Arab Higher Committee]], assumed local civic and military powers" (citing Kadish and Sela 2005).
  - Repo `research_notes/GDD phase 6/settlement-targets.md`, line 145 (Morris p. 424). It describes the July defence only.
- Repo: A. No contradiction.
- Open gaps: no source is dated exactly 14-15 May for Lydda. Two things need checking in the book text: when the Legion company sat in the Lydda police stations (Morris 2004, p. 424, and pp. 207-24 of "1948"), and whether those Legion troops left with the British on 14 May and came back.

#### gdd03-0338 - Ramle (ramla)
- Holder on 15 May 1948: not-Yishuv. The holder type is unresolved: a local national committee controlled the town exits from 5 to 15 May, and an Arab Legion element was reported from April.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49477 (al-Ramla). Event date: 12 July 1948. Quote: "Occupation date: 12/07/1948". Place-specific.
  - he.wikipedia "רמלה", rev 44119652. Event dates: 5-15 May 1948. Place-specific; cites Morris, "לידתה של בעיית הפליטים", p. 102, and Gelber, "Komemiyut VeNakba", p. 176. Quote: "בין 5 ל-15 במאי פרסמו "הוועדות הלאומיות" ... "הוועדה הלאומית" של רמלה הציבה שומרים בנקודות היציאה מן העיר, במטרה למנוע מתושביה לצאת." Same page: "במהלך המחצית השנייה של מאי ובראשית יוני נערכו קרבות האצ"ל ברמלה ... כל התקפות האצ"ל נהדפו".
  - The en.wikipedia expulsion article (as for Lydda) says the Legion company sat "on the Lydda-Ramle road".
- Repo: A. No contradiction.
- Open gaps: as for Lydda. The IZL attacks on Ramle in late May (Gesher, "עלי זית וחרב" 16, 2016, pp. 88-142) may name the defenders. The CIA 25 May map labels the Lydda-Ramle concentration "Arab Legion" (repo source 2), but that is 10 days late.

#### gdd03-0339 - Rishon LeZion (rishon_lezion)
- Holder on 15 May 1948: Yishuv/Haganah (a Jewish moshava).
- Status / confidence: PARTIAL / high. No fetched source states control on 14-15 May in so many words.
- Evidence:
  - R-CIA (18 May 1948; region-level): the Rishon-Sarafand area lies inside the tenuous Jewish-held border.
  - he.wikipedia "ראשון לציון", rev 44078272. Event date: 3 June 1948. Place-specific. Quote: "ב-3 ביוני הפציצו מטוסים מצריים את ראשון לציון. בהפצצה נהרגו 25 בני אדם". This is later context only.
- Repo: I. No contradiction.
- Open gaps: a dated Haganah or Givati document placing Rishon inside the Givati zone on 15 May (for example the IDF map "מערך חטיבות צה"ל ב-15.5.1948", repo source 10).

#### gdd03-0340 - Rehovot (rehovot)
- Holder on 15 May 1948: Yishuv/Haganah. Givati brigade headquarters was in the town.
- Status / confidence: PARTIAL / high
- Evidence:
  - he.wikipedia "רחובות", rev 44049829. The entry is undated within the war. Place-specific. Quote: "ב[[מלחמת העצמאות]] שכנה בעיר המפקדה הראשית של חטיבת הדרום, [[חטיבת גבעתי (תש"ח)|חטיבת גבעתי (ה'תש"ח)]]."
  - he.wikipedia "מע'אר (השפלה)", rev 43882295. Event date: night of 15 May 1948. Quote: "המבצע החל בליל ה-15 במאי 1948. בשעה 01.00 יצא כוח מגבעת ברנר דרומה". This shows Givati operating from next to Rehovot on that date. Region-level.
- Repo: I. No contradiction.
- Open gaps: a dated primary for the Givati headquarters at Rehovot in May 1948.

#### gdd03-0341 - Yibna (yibna)
- Holder on 15 May 1948: Arab irregulars and local militia. Iraqi volunteers were in the village from March; it was not taken until 4-5 June.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49202. Event date: 4 June 1948. Quote: "Occupation date: 04/06/1948 Occupying unit: Giva'ati". Place-specific.
  - en.wikipedia "Yibna", rev 1375084946 (citing Morris 2004, p. 259). Quotes: "In mid-March 1948, a contingent of Iraqi volunteers moved into the village." and "On 27 May, following the fall of nearby ... al-Qubayba and Zarnuqa, most of the population of Yibna fled to Isdud, but Yibna's armed males were forced back to Yibna by Isdud's militiamen. ... On June 5, after a brief firefight, they occupied the village".
  - he.wikipedia "יבנה", rev 44050143. Quote: "עם פרוץ המלחמה הגיעו 200 חיילים עיראקים מצבא ההצלה ליבנא, וקבעו את בסיסם בתחנת הרכבת." It gives the capture as 31 May.
  - R-BARAK: "בשלב ראשון תקפה החטיבה כפרים באזור מג'דל-איסדוד-יבנא" (first half of May).
- Repo: c. The repo's own note says Yibna is "A until 5 June". The sources show armed Arab presence until late May or early June. This is a "possible" flag only, because c is not a direct conflict.
- Open gaps: whether the Iraqi or ALA contingent was still in Yibna on 15 May (Morris 2004, pp. 258-259).

#### gdd03-0343 - Aqir (aqir)
- Holder on 15 May 1948: Yishuv/Haganah (Givati).
- Status / confidence: SOURCED / medium
- Evidence:
  - Zochrot 49390. Event date: 4 May 1948. Quote: "Occupation date: 04/05/1948 Occupying unit: Giva'ati". Place-specific.
  - R-BARAK. Event date: 4 May 1948. Quote: "עוד קודם לכן ב-4 במאי 1948, במסגרת מבצע כיתור הושלם כיבושם של שני כפרים שחלשו על צירי תנועה חיוניים: עקיר הסמוך למזכרת בתיה ... וקאטרה הסמוכה לגדרה".
  - en.wikipedia "Aqir", rev 1374689417. Quote: "The village was depopulated ... on 6 May 1948 during [[Operation Barak]] by the [[Givati Brigade]]. The remaining village houses were taken over by [[Kiryat Ekron]] soon after." (citing the Kiryat Ekron municipality page)
- Repo: I. No contradiction.
- Open gaps: a primary showing a garrison or settlers in Aqir on 15 May itself. Morris's table dates the depopulation "6 (??) May".

#### gdd03-0345 - Zarnuqa (zarnuqa)
- Holder on 15 May 1948: Arab irregulars and local militia (irregulars from April; captured 27-28 May).
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49155. Event date: 27 May 1948. Quote: "Occupation date: 27/05/1948 Occupying unit: Giva'ati".
  - en.wikipedia "Zarnuqa", rev 1378494661 (citing Morris 2004, pp. 258-259). Quotes: "In April 1948, Arab irregulars moved into the village." and "Zarnuqa was depopulated on 27–28 May by the [[Givati Brigade]]".
  - he.wikipedia "זרנוגה", rev 43009415. Quote: "ב-28 במאי 1948 נכבש הכפר במבצע ברק ... על ידי גדוד 55 של חטיבת גבעתי."
  - en.wikipedia "Ness Ziona", rev 1381979008, gives 19 May: "on May 19, al-Qubayba and Zarnuqa were conquered by the [[Givati Brigade]]". Both dates fall after 15 May.
- Repo: c. Possible flag only: the sources point to an Arab-held village, not a contested one.
- Open gaps: whether the irregulars were still there on 15 May.

#### gdd03-0347 - Beit Nabala (bayt_nabala)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. The village was evacuated on 13 May on Arab Legion orders (Morris) and occupied in July.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49020. Event date: 12 July 1948. Quote: "Occupation date: 12/07/1948". The page also has: "Benny Morris writes that the village residents abandoned it on Arab orders on 13 May 1948. However, according to Walid Khalidi, this cannot be confirmed."
  - he.wikipedia "בית נבאלא", rev 43555550 (citing Morris, "לידתה", p. 99). Quote: "פונה הכפר ב-13 במאי 1948, לפי הוראת הלגיון הערבי."
  - he.wikipedia "לוד" (raw wikitext fetched 2026-10-27; revision id not recorded). It quotes Rabin's Dani order of 12 July: "יש לכוונם כלפי בית נבאללה". This implies the village was outside Israeli control in July.
  - Against: en.wikipedia "Bayt Nabala", rev 1377940870: "It was occupied by Israeli forces on 13 May 1948 and was completely destroyed by them on 13 September 1948" (citing Morris 2004, p. 354). This conflicts with all the other sources and looks like a misreading of the 13 May evacuation date.
- Repo: A. No contradiction with the repo. The sources conflict with each other.
- Open gaps: Morris 2004, p. 99 and p. 354, to settle the en.wiki sentence; and who, if anyone, garrisoned the empty village on 15 May.

#### gdd03-0349 - Na'ana (naana)
- Holder on 15 May 1948: Yishuv/Haganah (Givati, 52nd or 54th battalion), taken on 14 May.
- Status / confidence: SOURCED / medium
- Evidence:
  - Zochrot 49310 (al-Na'ani). Event date: 14 May 1948. Quote: "Occupation date: 14/05/1948 Occupying unit: Giva'ati". Place-specific.
  - R-LAT: "Between 14 and 15 May, its 52nd battalion took the villages of [[Abu Shusha]], [[Al-Na'ani]] and [[al-Qubab]]".
  - en.wikipedia "Al-Na'ani", rev 1377919782. Quote: "It was depopulated during the [[1948 Arab–Israeli War]] on May 14, 1948, by the [[Givati Brigade]] during [[Operation Barak]]."
- Repo: I. No contradiction.
- Open gaps: whether Givati stayed after taking the village (R-LAT-HE says forces near Latrun withdrew by day; that sentence is about the Latrun key points, not Na'ani).

#### gdd03-0351 - Qubab (al_qubab)
- Holder on 15 May 1948: mixed/contested. The sources disagree on the capture date.
- Status / confidence: PARTIAL / low
- Evidence:
  - R-LAT (14-15 May): the 52nd battalion "took ... [[al-Qubab]]".
  - he.wikipedia "אל-קובאב", rev 40432997 (citing an Uri Avnery article). Quote: "ב-14 במאי 1948 נכבש הכפר (שתושביו עזבוהו לפני כן) בידי גדוד 54 של חטיבת גבעתי".
  - Against:
    - Zochrot 49458: "Occupation date: 06/06/1948 Occupying unit: Giva'ati & Palmach".
    - en.wikipedia "Al-Qubab", rev 1375409575: "It was depopulated in July 1948 during the [[Operation Dani]]" (Morris 2004, p. xix: "July 15, 1948"). The same page has a Palmach photo caption: "Palmach forces in al-Qubab on 11 June 1948 (Operation Yoram)".
- Repo: I. Possible flag only: the 14 May sources support I, while the June and July dates would mean the village changed hands after 15 May. Do not change the repo on this alone.
- Open gaps: the Givati history (Ayalon) for 14-15 May; whether al-Qubab was given up after 15 May and retaken in June.

#### gdd03-0353 - Sarafand el 'Amar (sarafand_al_amar)
- Holder on 15 May 1948: not-Yishuv. The village was depopulated around 15 May, and Arab forces entered the British camp next door on 15 May. Givati occupied the village on 20 May.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49367. Event date: 20 May 1948. Quote: "Occupation date: 20/05/1948 Occupying unit: Giva'ati". Place-specific.
  - en.wikipedia "Tzrifin", rev 1373697470. Quote: "The adjacent Arab village Sarafand al-'Amr was depopulated on 15 May."
  - he.wikipedia "צריפין", rev 43963697 (see gdd03-0399).
  - en.wikipedia "Sarafand al-Amar", rev 1378603064: Morris gives the date and cause of depopulation as "not known".
- Repo: I. **CONTRADICTION**: Zochrot dates the occupation 20 May, so the Yishuv did not hold the village on 15 May.
- Open gaps: who was physically in the village on 15 May (villagers, or the "Arab forces" who entered the camp).

#### gdd03-0355 - Ness Ziona (ness_ziona)
- Holder on 15 May 1948: Yishuv/Haganah.
- Status / confidence: PARTIAL / high
- Evidence:
  - en.wikipedia "Ness Ziona", rev 1381979008. Place-specific; the period is 1947-48. Quote: "The outlying villages of Kfar Aharon and Tirat Shalom (now part of Ness Ziona) frequently exchanged fire with the Arab villages ... al-Qubayba and Zarnuqa ... Most of Ness Ziona's youth joined the [[Haganah]]".
  - R-CIA (18 May): the Rishon-Sarafand area lies inside the tenuous Jewish-held border.
  - he.wikipedia "נס ציונה", rev 44026923: the Arab quarter, Wadi Hunayn, was abandoned on 17 April 1948 (see gdd03-0365).
- Repo: I. No contradiction.
- Open gaps: a dated 14-15 May source.

#### gdd03-0357 - Deir Tarif (dayr_tarif)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. The Arab Legion defended it in July.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49105. Event date: 12 July 1948. Quote: "Occupation date: 12/07/1948 Occupying unit: Kiriati". The page also has: "The village, with a population of 2,030, was defended by the Jordanian Army".
  - en.wikipedia "Dayr Tarif", rev 1377970221 (Morris 2004, p. 354) gives 10 July, by "the Ninth Commando Battalion of the Armored Brigade of [[Operation Dani]]".
- Repo: A. No contradiction.
- Open gaps: whether the Legion was in Deir Tarif on 15 May, or only arrived in June.

#### gdd03-0359 - Mughar (al_maghar)
- Holder on 15 May 1948: mixed/contested. Givati's 3rd (or 53rd) battalion attacked from about 01:00 on the night of 15 May and captured the village that day. Most of the village was already abandoned.
- Status / confidence: SOURCED / medium (the sources agree on 15 May; en.wiki gives 18 May)
- Evidence:
  - Zochrot 49280. Event date: 15 May 1948. Quote: "Occupation date: 15/05/1948 Occupying unit: Giva'ati". Place-specific.
  - R-BARAK: "ב-15 במאי כבש גדוד 3 של חטיבת גבעתי את הכפר מע'אר ששכן מעל צומת תל נוף ובתיו פוצצו. רובו של הכפר כבר היה נטוש באותו שלב." (citing Morris, "1948", p. 185)
  - he.wikipedia "מע'אר (השפלה)", rev 43882295: "המבצע החל בליל ה-15 במאי 1948. בשעה 01.00 יצא כוח מגבעת ברנר דרומה וכוח מגדרה צפונה."
  - Against: en.wikipedia "Al-Maghar", rev 1377919466: "The village was attacked, occupied and depopulated on 18 May 1948." (Morris 2004, p. 95)
- Repo: c. Consistent.
- Open gaps: settle 15 May against 18 May in Morris 2004 and the Givati history.

#### gdd03-0361 - Qubeiba (al_qubayba_ramla)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. Captured 27 May (or 19 May).
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49456. Event date: 27 May 1948. Quote: "Occupation date: 27/05/1948 Occupying unit: Giva'ati". Place-specific.
  - en.wikipedia "Al-Qubayba, Ramle", rev 1377919867. Quote: "It was depopulated during the [[1948 Arab-Israeli War]] on May 27, 1948, by the [[Givati Brigade]] as part of the Second stage of [[Operation Barak]]."
  - en.wikipedia "Ness Ziona", rev 1381979008: "on May 19, al-Qubayba and Zarnuqa were conquered by the [[Givati Brigade]]".
  - R-BARAK puts Qubeiba among the villages taken "בחציו השני של חודש מאי".
- Repo: I. **CONTRADICTION**: every dated source has the capture after 15 May.
- Open gaps: the holder (villagers or irregulars) on 15 May.

#### gdd03-0363 - Bashshit (bashshit)
- Holder on 15 May 1948: Yishuv/Haganah attacked and took the village on 11 May (or 10-13 May). The sources do not say whether it was held on 15 May; the houses were blown up.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49039. Event date: 11 May 1948. Quote: "Occupation date: 11/05/1948 Occupying unit: Giva'ati".
  - R-BARAK: "ב-11 במאי כבשו כוחות של חטיבת גבעתי את הכפר בשית לאחר קרב קשה."
  - he.wikipedia "בשית", rev 43009334: "הותקפה בשית ב-11 במאי 1948 על ידי כוחות מגדוד 52 ו-54 של חטיבת גבעתי. לאחר קרב קשה הכפר נכבש ובתיו פוצצו."
  - en.wikipedia "Bashshit", rev 1377940254: "Between May 10 and May 13, 1948, the village was attacked by the 52nd and 53rd battalions of the [[Givati Brigade]]" (Morris 2004, p. 256).
- Repo: c. Possible flag: a capture on 11 May would suggest I, unless Givati withdrew. In the same operation, Beit Daras was left after the raid ("סיור שעבר בהם לאחר נסיגת הכוחות מצא אותם ריקים").
- Open gaps: whether Givati garrisoned Bashshit after 11 May.

#### gdd03-0365 - Wadi Hunein (wadi_hunayn)
- Holder on 15 May 1948: Yishuv/Haganah. The village was abandoned on 17 April and the Haganah entered on 19 April. It is next to Jewish Ness Ziona.
- Status / confidence: SOURCED / medium
- Evidence:
  - Zochrot 49132 ("Wadi Haunayn"). Event date: 17 April 1948. Quote: "Occupation date: 17/04/1948".
  - en.wikipedia "Wadi Hunayn", rev 1377160349 (citing Haaretz, Meron Rapoport). Quote: "the remaining population being transported into Jordan by the [[Haganah]] who entered the village on 19 April 1948".
  - he.wikipedia "נס ציונה", rev 44026923 (citing Davar, 18 April 1948). Quote: "ננטש היישוב ואדי חנין על ידי תושביו הערבים ב-17 באפריל 1948".
- Repo: I. No contradiction.
- Open gaps: none critical.

#### gdd03-0367 - Majdal Yaba (majdal_yaba)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. The Iraqi army defended it in July.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49254. Event date: 13 July 1948. Quote: "Occupation date: 13/07/1948 Occupying unit: Alexandroni".
  - en.wikipedia "Majdal Yaba", rev 1380120658. Quote: "it was occupied by the Second Battalion of the [[Alexandroni Brigade]] on July 12, 1948, in [[Operation Danny]], after wresting it from the [[Iraqi Army]] who were defending the village" (citing the New York Times as quoted in Khalidi, p. 397).
- Repo: A. No contradiction.
- Open gaps: the Iraqi army reached this sector in late May or early June. Who held the village on 15 May (villagers or irregulars) is unknown.

#### gdd03-0369 - Jimzu (jimzu)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49077. Event date: 10 July 1948. Quote: "Occupation date: 10/07/1948 Occupying unit: Yiftah (Palmach)".
  - en.wikipedia "Jimzu", rev 1378154431. Quote: "Jizmu was occupied by the [[Yiftach Brigade]] ... on July 10, 1948, in the first phase of [[Operation Dani]]" (with Yiftah HQ to Dani HQ, 11 July 1948, IDFA 922\75\\1237, quoted in Morris 2004).
- Repo: A. No contradiction.
- Open gaps: the holder type on 15 May.

#### gdd03-0371 - Imwas (imwas)
- Holder on 15 May 1948: Arab irregulars, with the first Arab Legion troops arriving (region-level, Latrun salient). The ALA had held the village from April to mid-May.
- Status / confidence: PARTIAL / medium
- Evidence:
  - en.wikipedia "Imwas", rev 1376238592 (Morris 2008, index). Quote: "[[Arab Liberation Army]] forces were there from April to the middle of May until the arrival of the [[Arab Legion]]."
  - R-LAT (14-16 May): the ALA withdrew around 14-15 May; "the position was held by just 200 irregulars" (Gelber); a Legion platoon and irregulars "took over the fort" (Morris); 40 legionnaires arrived on the night of 15-16 May.
  - R-LAT-HE: the area was "ריק כמעט לחלוטין מכוחות ערביים במשך כשלושה ימים", and Givati and Harel held key points for short periods from 15 to 18 May.
- Repo: A. No contradiction.
- Open gaps: Morris 2008, pp. 207-24, and Gelber pp. 138-145, for who was in Imwas itself on 15 May.

#### gdd03-0373 - Inaba (innaba)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. Villagers defended it when it fell in July.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49376, which reproduces Khalidi's "All That Remains" (the page has "Source: al-Khalidi, Walid (ed.). All that remains", no page number). Event dates: 8-9 June and 10 July 1948. Quote: "The village was first attacked during Operation Yoram, the fourth assault on the al-Latrun area, on 8-9 June 1948 ... But 'Innaba was not occupied until the following month, during Operation Dani." Field: "Occupation date: 10/07/1948 Occupying unit: Yiftah (Palmach)& 8th Brigade".
- Repo: A. No contradiction.
- Open gaps: the holder on 15 May.

#### gdd03-0375 - Nabi Rubin (nabi_rubin)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. Haganah raids on 10-12 May failed; the village was captured on 1 June (or 20 May).
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49305. Event date: 1 June 1948. Quote: "Occupation date: 01/06/1948 Occupying unit: Giva'ati".
  - en.wikipedia "Nabi Rubin", rev 1376879149 (Morris 2004, p. 256). Quotes: "During the 10–12 May 1948, units of the Ephraim subdistrict, apparently without success, repeatedly mortared and raided Nabi Rubin, to force evacuation." and "On June 1, 1948, Israel's [[Givati Brigade]] captured the village".
  - he.wikipedia "נבי רובין", rev 44001160 (citing moreshet-map). Quote: "בתאריך 20 מאי 1948 נכבש הכפר על ידי חטיבת גבעתי". Also: "במקום נשארו כנופיות ולוחמים ערבים" (after the capture).
- Repo: I. **CONTRADICTION**: every dated source has the capture after 15 May, and the 10-12 May raids failed.
- Open gaps: the holder type. Armed Arab groups stayed in the dunes until August.

#### gdd03-0377 - Ni'lin (nilin)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: none dated. en.wikipedia "Ni'lin" gives only the postwar position ("after the [[1949 Armistice Agreements]], Ni'lin came under Jordanian rule"), and the research criteria do not allow that to be used. A web search for Ni'lin, Rantis and Qibya in May 1948 found nothing dated.
- Repo: A. No contradiction.
- Open gaps: an Arab Legion or Iraqi deployment record for the Ramallah western foothills, 15-20 May.

#### gdd03-0379 - Tira (al_tira)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49201. Event date: 10 July 1948. Quote: "Occupation date: 10/07/1948 Occupying unit: Alexandroni & 8th brigade".
  - en.wikipedia "Al-Tira, Ramle", rev 1374585448 (Morris 2004, p. xviii).
  - he.wikipedia "מבצע דני", rev 43258522: "נכבשו על ידי גדוד 33 של 'אלכסנדרוני', וגדוד 89, הכפרים ... קולה, לאחר קרב קשה, וטירה" (10 July).
- Repo: A. No contradiction.
- Open gaps: the holder type on 15 May.

#### gdd03-0381 - Rantis (rantis)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: none dated. en.wikipedia "Rantis" has only "after the [[1949 Armistice Agreements]], Rantis came under [[Jordanian annexation of the West Bank|Jordanian rule]]". That is a later event and cannot be used.
- Repo: A. No contradiction.
- Open gaps: as for Ni'lin. The Iraqi sector started near Majdal Yaba and Qula (July sources); its May boundary is not sourced.

#### gdd03-0383 - Qibya (qibya)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: none dated. en.wikipedia "Qibya" has only the postwar position under Jordan.
- Repo: A. No contradiction.
- Open gaps: as for Ni'lin.

#### gdd03-0385 - Beit Nuba (bayt_nuba)
- Holder on 15 May 1948: Arab irregulars, with the first Arab Legion troops arriving (region-level, Latrun salient). The Legion garrisoned the village later.
- Status / confidence: PARTIAL / low
- Evidence:
  - en.wikipedia "Bayt Nuba", rev 1381372654 (Morris 2008, pp. 239-240). Quote: "During the [[1948 Arab–Israeli War]], the village was garrisoned by the [[Arab Legion]] to defend the Latrun salient." The text gives no date for the garrison; a June 8 action follows.
  - R-LAT and R-LAT-HE (14-18 May; region-level).
- Repo: A. No contradiction.
- Open gaps: the date the Legion garrison arrived.

#### gdd03-0387 - Yalu (yalo)
- Holder on 15 May 1948: Arab irregulars, with the first Arab Legion troops arriving (region-level, Latrun salient).
- Status / confidence: PARTIAL / low
- Evidence:
  - R-LAT and R-LAT-HE (14-18 May; region-level).
  - en.wikipedia "Yalo", rev 1378290061: only a Haganah raid of 27 December 1947, and Jordanian rule after the war.
- Repo: A. No contradiction.
- Open gaps: a place-specific May 1948 source.

#### gdd03-0389 - Qatra (qatra)
- Holder on 15 May 1948: Yishuv/Haganah (Givati). Givati entered on 5-6 May and reoccupied the village by 12 May; the population was expelled on 17 May.
- Status / confidence: SOURCED / medium
- Evidence:
  - Zochrot 49450. Event date: 6 May 1948. Quote: "Occupation date: 06/05/1948 Occupying unit: Giva'ati".
  - R-BARAK: the capture of "קאטרה הסמוכה לגדרה" was completed on 4 May (Operation Kitur).
  - en.wikipedia "Qatra", rev 1378153271 (Morris 2004, p. 255, citing 'Doron (Elitzur)' to HIS-AD, "The Conquest of Qatra", 12 May 1948, HA 105\92aleph; and HIS-AD, 30 June 1948). Quotes: "The Givati troops entered and conducted an arms collection operation on 5–6 May." and "The [[Haganah]] reoccupied the village and its entire population were either intimidated into flight or expelled on 17 May."
- Repo: I. No contradiction.
- Open gaps: the exact reoccupation day (the report is dated 12 May).

#### gdd03-0391 - Muzeiri'a (al_muzayria)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49287. Event date: 16 July 1948. Quote: "Occupation date: 16/07/1948".
  - en.wikipedia "Al-Muzayri'a", rev 1377746617. Quote: "Al-Muzayri was depopulated on 12 July 1948, after a military assault by Israeli forces." (Morris 2004, p. xviii)
- Repo: A. No contradiction.
- Open gaps: the holder type on 15 May.

#### gdd03-0393 - Sarafand el Kharab (sarafand_al_kharab)
- Holder on 15 May 1948: Yishuv/Haganah (probable). The sources date the occupation or depopulation to April, but one says it was evacuated on 15 May.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49366. Event date: 1 April 1948. Quote: "Occupation date: 01/04/1948". The page lists "Rishon LeZion" as a Jewish settlement on village land before 1948.
  - en.wikipedia "Sarafand al-Kharab", rev 1377940483: date of depopulation "April 20, 1948" (Morris 2004, p. xix, village #234).
  - Against: en.wikipedia "Ness Ziona", rev 1381979008: "On May 15, 1948, [[Sarafand al-Kharab]] was evacuated of Arab inhabitants".
- Repo: I. No contradiction (April occupation, or evacuation on 15 May, beside Jewish Rishon and Ness Ziona).
- Open gaps: Khalidi's entry for the occupying unit and date.

#### gdd03-0395 - Givat Brenner (givat_brenner)
- Holder on 15 May 1948: Yishuv/Haganah. Givati used it as a starting base on the night of 15 May.
- Status / confidence: PARTIAL / high
- Evidence: he.wikipedia "מע'אר (השפלה)", rev 43882295. Event date: night of 15 May 1948. Place-specific for Givat Brenner as a Haganah base. Quote: "בשעה 01.00 יצא כוח מגבעת ברנר דרומה וכוח מגדרה צפונה. ... כן יצאה מגבעת ברנר יחידה של 4 משוריינים". (It is Wikipedia only, so the item stays PARTIAL.)
- Repo: I. No contradiction.
- Open gaps: the primary for this account (the Givati history).

#### gdd03-0397 - Qula (qula)
- Holder on 15 May 1948: not-Yishuv; holder unresolved. The Arab Legion fought for it in July.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Zochrot 49461. Event date: 18 July 1948. Quote: "Occupation date: 18/07/1948 Occupying unit: Alexandroni".
  - en.wikipedia "Qula", rev 1378153862 (Morris 2008, p. 293). Quote: "The IDF captured the village for the last time on July 18, just before the start of the [[1948 Arab–Israeli War#Second truce: 18 July .E2.80.93 15 October 1948|Second Truce]]."
- Repo: A. No contradiction.
- Open gaps: the holder type on 15 May.

#### gdd03-0399 - Tzrifin (tzrifin)
- Holder on 15 May 1948: Arab forces. The British left the camp at 09:00 on 15 May (en.wiki says 14 May) and Arab forces or local villagers entered it. Givati took the camp on 18-19 May.
- Status / confidence: PARTIAL / medium (Wikipedia only, but the he.wiki article cites newspapers of 20 May 1948)
- Evidence:
  - he.wikipedia "צריפין", rev 43963697 (citing Al HaMishmar, Jon Kimche, "טירוני סרפנד חוזרים אליה ככובשים", 20 May 1948, and Haaretz, "כיבוש מחנה צריפין", 20 May 1948). Event dates: 15 and 18-19 May 1948. Quote: "למחרת הכרזת העצמאות ב-15 במאי 1948 בשעה 9 בבוקר עזבו הבריטים את מחנה צריפין וכוחות ערבים נכנסו אליו. המחנה נכבש על ידי חיילי חטיבת "גבעתי", בקרב קצר שנערך בתאריך 18–19 במאי 1948 ללא נפגעים".
  - en.wikipedia "Tzrifin", rev 1373697470. Quote: "On 14 May 1948 ... British forces vacated Sarafand. False rumours suggested the British sold the base to the Arabs, but only Arab residents of nearby villages, some of whom worked in the base, entered the base for looting. ... After a two-day battle, between the 18th and 19 May, the base was captured by the Jewish forces from the [[Givati Brigade]]."
- Repo: I. **CONTRADICTION**: both sources say the Jewish forces took the camp only on 18-19 May.
- Open gaps: read the two newspaper articles of 20 May 1948 (NLI JPress) to confirm the departure time and who entered (an organised force or looters).

---

### Contradictions (fetched source against the repo opening code)

1. **gdd03-0353 Sarafand el 'Amar**: repo I. Zochrot says "Occupation date: 20/05/1948 Occupying unit: Giva'ati", and en.wiki Tzrifin says it was depopulated on 15 May. The Yishuv did not hold it on 15 May.
2. **gdd03-0361 Qubeiba (Ramle)**: repo I. Zochrot says "Occupation date: 27/05/1948"; en.wiki gives 27 May and en.wiki Ness Ziona gives 19 May. All dates fall after 15 May.
3. **gdd03-0375 Nabi Rubin**: repo I. Zochrot says "Occupation date: 01/06/1948"; en.wiki (Morris p. 256) says the 10-12 May raids were "apparently without success"; he.wiki gives 20 May. All dates fall after 15 May.
4. **gdd03-0399 Tzrifin (Sarafand camp)**: repo I. he.wiki (citing Haaretz and Al HaMishmar, 20 May 1948) says the British left at 09:00 on 15 May and "כוחות ערבים נכנסו אליו", and that Givati captured it on 18-19 May. en.wiki agrees on the 18-19 May capture.

Possible (the sources conflict, or the repo code is c rather than a hard conflict):
- gdd03-0351 Qubab: repo I. 14 May capture (R-LAT, he.wiki) against Zochrot 6 June and Morris 15 July.
- gdd03-0363 Bashshit: repo c, against a Givati capture on 11 May (Zochrot, he.wiki). Whether Givati held it on 15 May is not stated.
- gdd03-0341 Yibna and gdd03-0345 Zarnuqa: repo c. The sources show armed Arab presence (Iraqi or ALA volunteers, irregulars) until capture on 27 May to 5 June, which is Arab-held rather than contested.
- gdd03-0347 Beit Nabala: the sources conflict with each other (en.wiki "occupied by Israeli forces on 13 May" against Zochrot 12 July and he.wiki's evacuation on Legion orders on 13 May). The repo's A agrees with the majority.

## Jerusalem Hills: research notes and sections


Web research, 2026-10-27. Quotes are short fragments returned through a summariser, so a few items are marked paraphrase. palestineremembered.com returned HTTP 403 on every fetch (4 tried); no workaround was used, so Khalidi/Morris are cited only as Wikipedia gives them (hence most village items are capped at PARTIAL). en/he Wikipedia items are secondary; nothing here is SOURCED from Wikipedia alone except where a non-wiki page (Knesset PDF, Lapidot on daat.ac.il, FRUS) carries the claim.

Repo files read (origin/main, read-only): research_notes/Map borders/control-15-may-1948.md, research_notes/Map/enemy-held-1948-05-15.md, research_notes/GDD phase 6/settlement-targets.md, research_notes/GDD phase 6/enemy-captures-of-player-localities.md, content/map/locations_1948.yaml, content/map/localities_1948.yaml, content/map/settlements_1948.yaml. They hold no place-specific 15 May holder for these places beyond: Jerusalem/Old City starting_control in locations_1948.yaml (proposed association) and the section 4 table (Hebron A; Jerusalem m; Latrun c 15-18 May).


#### gdd03-0413 - West Jerusalem (jerusalem)

- Holder on 15 May 1948: Yishuv/Haganah (with IZL)
- Status / confidence: SOURCED / high (Jewish neighbourhoods and the British security zones taken 14 May); medium for the exact footprint
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
  - https://en.wikipedia.org/wiki/Operation_Pitchfork (en.wiki, secondary; read 2026-10-27); 13-18 May 1948; Irgun took the Generali Building, the Russian Compound and the Police Academy on 14 May (Rosenne 1993 p. 646, paraphrase from summarised source access); aim "to safeguard the Jewish area of Jerusalem" (Morris 2008 p. 163).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no (hex-level tension: hex 0,23 is shared with the Old City; repo opening m).
- Open gaps: Exact line on 15 May between West Jerusalem and Arab-held quarters (Abu Tor, Musrara, Sheikh Jarrah lower) still to trace; Katamon was taken earlier (Operation Yevusi, 30 Apr-1 May) - not fetched 2026-10-27.

#### gdd03-0414 - Hebron (hebron)

- Holder on 15 May 1948: not-Yishuv (holder unresolved): Arab Legion units and local irregulars active around Hebron 12-14 May; Egypt in control "at the beginning of the war" per one secondary [C]
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-4, 27]). no.
- Open gaps: A dated place-specific source for Hebron town on 15 May (Legion company? local National Committee? Egyptian/Muslim Brotherhood arrival date, believed later in May - not verified).

#### gdd03-0416 - Dura (dura_hebron)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-4, 27]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0418 - Bethlehem (bethlehem)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: No dated place-specific source found for 15 May. Search done: Egyptian/Muslim Brotherhood arrival in Bethlehem (no dated hit); a Ben-Gurion diary of 30 Dec 1948 (bengurionarchive.bgu.ac.il node 5237, snippet only) names "Egyptian forces from Bethlehem and Hebron", far after the date.

#### gdd03-0420 - Yatta (yatta)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-4, 28]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0422 - Ramallah (ramallah)

- Holder on 15 May 1948: not-Yishuv; Arab Legion deployment planned (14 May) and under way from 15 May (day of arrival unsourced)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [0, 22]). no.
- Open gaps: Exact day the Legion (2nd Brigade per en.wiki Arab Legion, not opened) reached Ramallah; who held the town on 15 May before it arrived (local National Guard?).

#### gdd03-0424 - Silwan (silwan)

- Holder on 15 May 1948: unresolved (Arab side; the Arab Legion reached Jerusalem only on 18 May)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no.
- Open gaps: No place-specific dated source for 15 May. Search for Mount of Olives / Augusta Victoria / Silwan in May 1948 returned nothing dated. Region-level only: Arab fighters held the Old City outside the Jewish Quarter; Legion arrival 18 May.

#### gdd03-0426 - Dhahiriya (al_dahiriya)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-6, 29]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0428 - Ajjur (ajjur)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Ajjur (en.wiki, secondary; read 2026-10-27); attack 23-24 July 1948 (Khalidi 1992 p. 207); "the Fourth Battalion of Israel‘s Giv'ati Brigade occupied ‘Ajjur" on 23 Oct 1948 (Khalidi 1992 p. 207).
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-4, 24]). no.
- Open gaps: Capture Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced.

#### gdd03-0430 - Dawayima (al_dawayima)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Al-Dawayima (en.wiki, secondary; read 2026-10-27); captured "on 29 October 1948, five days after the start of the truce" by the 89th Battalion (Operation Yoav); infobox Morris 2004 p. xix #324.
- Repo: opening code A (all periods AMMMMMMMMMMIIIII; hex [-6, 27]). no.
- Open gaps: Capture 29 Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced.

#### gdd03-0432 - Beit Jala (beit_yala)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: No dated place-specific source found for 15 May. Search done: Egyptian/Muslim Brotherhood arrival in Bethlehem (no dated hit); a Ben-Gurion diary of 30 Dec 1948 (bengurionarchive.bgu.ac.il node 5237, snippet only) names "Egyptian forces from Bethlehem and Hebron", far after the date.

#### gdd03-0434 - Halhul (halhul)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0436 - Ein Karim (ein_karem)

- Holder on 15 May 1948: not-Yishuv (Arab irregulars with ALA/Iraqi volunteers present Feb-July; day-specific holder not sourced)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Ein_Karem (en.wiki, secondary; read 2026-10-27); "The village was finally captured by Israeli forces during the ten-day campaign of July 1948"; ALA forces left 14-16 July (Morris 2004 p. 436); Feb 1948 ALA reinforcement and 10 March Iraqi detachment (no footnote).
  - https://he.wikipedia.org/wiki/עין_כרם (he.wiki; read 2026-10-27); "שנכבשה ב-18 ביולי על ידי פלוגת יהונתן" (cites Lapidot, Belahav HaMered); "בהמשך, התבססו בעין כרם לוחמים עיראקיים" (undated, no source).
- Repo: opening code c (all periods cIIIIIIIIIIIIIII; hex [-1, 23]). no (repo opening c).
- Open gaps: A source naming the force in Ein Karem on 14-16 May 1948.

#### gdd03-0438 - Jericho (jericho)

- Holder on 15 May 1948: Arab Legion (from early morning 15 May; same-day change, prior holder not sourced)
- Status / confidence: SOURCED / medium
- Evidence:
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [3, 22]). no.
- Open gaps: A second, primary-ish source for the Legion at Jericho on 15 May (Glubb memoir; cojs.org excerpt seen as snippet only). Who held Jericho on 14 May (British withdrawal date) not sourced.

#### gdd03-0440 - Bira (al_bireh)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [1, 21]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0442 - Tur (at_tur)

- Holder on 15 May 1948: unresolved (Arab side; the Arab Legion reached Jerusalem only on 18 May)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no.
- Open gaps: No place-specific dated source for 15 May. Search for Mount of Olives / Augusta Victoria / Silwan in May 1948 returned nothing dated. Region-level only: Arab fighters held the Old City outside the Jewish Quarter; Legion arrival 18 May.

#### gdd03-0444 - Beit Sahur (beit_sahour)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: No dated place-specific source found for 15 May. Search done: Egyptian/Muslim Brotherhood arrival in Bethlehem (no dated hit); a Ben-Gurion diary of 30 Dec 1948 (bengurionarchive.bgu.ac.il node 5237, snippet only) names "Egyptian forces from Bethlehem and Hebron", far after the date.

#### gdd03-0446 - Si'ir (sair)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0448 - Lifta (lifta)

- Holder on 15 May 1948: unresolved (Yishuv presence attested before 15 May: a Lehi camp on village land; village empty since Jan-Feb 1948)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Lifta (en.wiki, secondary; read 2026-10-27); by early Feb 1948 irregulars had abandoned the village (paraphrase; Morris 2004 p. xx village #363: depopulated Jan 1948, military assault).
  - https://he.wikipedia.org/wiki/ליפתא (he.wiki, secondary; read 2026-10-27); quotes: "בפברואר 1948 היו כבר כל כפרי האזור ריקים מתושביהם"; "המחנה הראשון הוקם על אדמת הכפר ליפתא, עוד לפני הקמת המדינה" (Lehi camp, later 'Eldad'; source: Lehi heritage association; undated). Scope: place-specific, not dated to 15 May.
- Repo: opening code c (all periods cIIIIIIIIIIIIIII; hex [-1, 23]). no (repo opening c).
- Open gaps: A dated record that Haganah/Lehi occupied or garrisoned Lifta village itself by 15 May (rule Q2: an abandoned village is not Yishuv-held unless stated).

#### gdd03-0450 - Samu' (as_samu)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-5, 29]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0452 - Sur Bahir (sur_bahir)

- Holder on 15 May 1948: unresolved
- Status / confidence: NEEDS-RESEARCH / low
- Evidence:
  - https://en.wikipedia.org/wiki/Sur_Baher (en.wiki, secondary; read 2026-10-27); "Sur Baher was occupied and later annexed by Jordan." (Dinstein & Tabory 1994 p. 41) - undated.
  - "Battle of Ramat Rachel" en.wiki title returned 404; a search snippet (cyclowiki, Russian) gives the Ramat Rachel battle as 21-25 May 1948 - snippet only, after 15 May.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: Dated source for Sur Bahir 14-16 May (Ramat Rachel kibbutz histories; Egyptian/MB arrival).

#### gdd03-0454 - Beit Jibrin (bayt_jibrin)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Bayt_Jibrin (en.wiki, secondary; read 2026-10-27); "The First Battalion of the Egyptian Army were ordered to take up position in Bayt Jibrin." (second half of May, no footnote); Giv'ati occupied the village 27 Oct 1948 (Morris 2004 p. 468).
- Repo: opening code A (all periods AMMMMMMMMMMIIIII; hex [-5, 26]). no.
- Open gaps: Capture 27 Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced. Beit Jibrin: the Egyptian 1st Battalion order is dated "second half of May" = after 15 May.

#### gdd03-0456 - Beit Mahsir (bayt_mahsir)

- Holder on 15 May 1948: Yishuv/Haganah (Palmach Harel): occupied 11 May, Israeli troops at the village 13 May
- Status / confidence: PARTIAL / medium-high
- Evidence:
  - https://en.wikipedia.org/wiki/Operation_Maccabi (en.wiki, secondary; read 2026-10-27); quotes: "Between 9–11 May, a battalion of the Harel brigade attacked and took the village of Bayt Mahsir" (Morris 2008 p. 132); the Sha'ar HaGai battalion held hills north and south of the road (paraphrase, Morris 2008 p. 132); Givati platoon penetrated the Latrun police fort "without encountering any resistance on the morning of 15 May" (Lapierre & Collins p. 611); Morris (2002 p. 152): a Legion platoon of the 11th Company with irregulars took over the fort (paraphrase); "The first contingent of 40 legionnaires" on the night of 15-16 May (Morris 2002 p. 152).
  - https://he.wikipedia.org/wiki/מבצע_מכבי (he.wiki, secondary; read 2026-10-27); quotes: 10 May "כוח מהגדוד הרביעי כבש את משלט המסרק (משלט 17)" (and found Beit Mahsir empty, paraphrase); 14 May "פינו הבריטים את לטרון"; 15 May "עזבו כוחות 'צבא ההצלה'" (vacuum 15-17 May, paraphrase); night 15-16 May "כוח של חטיבת גבעתי כבש את הכפר לטרון"; night 16-17 May "כוח של הלגיון תפס את לטרון".
  - https://he.wikipedia.org/wiki/בית_מחסיר (he.wiki rev. 43143383 per fetch; read 2026-10-27): captured 11 May 1948 by Harel (4th bn) in Operation Maccabi; "פוצצו יותר מ-100 מבתי הכפר" (11 May; cites Uzi Narkiss, Hayal shel Yerushalayim (1991) pp. 90-92).
  - https://en.wikipedia.org/wiki/Bayt_Mahsir (en.wiki, secondary; read 2026-10-27); depopulated 10-11 May 1948, Harel in Operation Maccabi (Morris 2004 pp. 233, 235, 237); on 13 May 1948 Israeli troops contaminated the village wells (Morris & Kedar, Middle Eastern Studies 2022 p. 7) (paraphrase from summarised source access).
- Repo: opening code A (all periods AMcIIIIIIIIIIIII; hex [-2, 23]). CONTRADICTION (probable): repo opening A.
- Open gaps: A primary statement that Harel still held Beit Mahsir on 15 May (Palmach site item "כיבוש בית מחסיר" not reached; Sefer HaPalmach). Only secondary sources fetched, hence PARTIAL.

#### gdd03-0458 - Idna (idna)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-5, 27]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0460 - Surif (surif)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 25]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0462 - Bani Na'im (bani_naim)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 27]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0464 - Beit Nattif (bayt_nattif)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Bayt_Nattif (en.wiki, secondary; read 2026-10-27); occupied 21 Oct 1948 in Operation Ha-Har "by the Fourth Battalion of the Har'el Brigade" (Khalidi 1992 pp. 211-212); Haganah "punitive" attack after 16 Jan 1948 (Khalidi p. 212).
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-3, 24]). no.
- Open gaps: Capture 21 Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced.

#### gdd03-0466 - Deiraban (dayr_aban)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Dayr_Aban (en.wiki, secondary; read 2026-10-27); depopulated 19-20 Oct 1948 in Operation Ha-Har (Morris 2004 p. xx #335, p. 462); heavy fighting reported 4 Aug 1948 (Morris p. 447 n. 211).
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-3, 24]). no.
- Open gaps: Capture 19-20 Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced.

#### gdd03-0468 - Deir Dibwan (deir_dibwan)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [1, 21]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0470 - Abu Dis (abu_dis)

- Holder on 15 May 1948: unresolved (Arab side; the Arab Legion reached Jerusalem only on 18 May)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no.
- Open gaps: No place-specific dated source for 15 May. Search for Mount of Olives / Augusta Victoria / Silwan in May 1948 returned nothing dated. Region-level only: Arab fighters held the Old City outside the Jewish Quarter; Legion arrival 18 May.

#### gdd03-0472 - Malha (malha)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Al-Maliha (en.wiki, secondary; read 2026-10-27); depopulated 21 Apr and 15 July 1948 (Morris 2004 p. xx #361); Irgun attack 14 July (Morris 2004 pp. 239, 435-436); non-aggression pact with the Haganah earlier (Morris 2004 pp. 75, 91) (paraphrases from summarised source access).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: Who held Malha 15 May (villagers? irregulars?) not stated.

#### gdd03-0474 - Silwad (silwad)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0476 - Beit 'Ummar (beit_ummar)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0478 - Walaja (al_walaja)

- Holder on 15 May 1948: not-Yishuv (holder unresolved; defenders named but undated: Egyptian Muslim Brotherhood, ALA, local militia)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Al-Walaja (en.wiki, secondary; read 2026-10-27); depopulated 21 Oct 1948, military assault (Morris 2004 p. xx #349); surrendered to Israeli troops 21 Oct after Arab forces retook it more than once (Khalidi 1992 pp. 206-207) (paraphrase).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: Date the Muslim Brotherhood/ALA defenders arrived; 15 May position.

#### gdd03-0480 - Beit Hanina (beit_hanina)

- Holder on 15 May 1948: unresolved
- Status / confidence: NEEDS-RESEARCH / low
- Evidence:
  - none fetched.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [0, 22]). no.
- Open gaps: No source fetched; nearby Neve Yaakov was evacuated 18 May and Atarot 17 May (repo note control-15-may-1948.md section 3) - context only, not evidence for the Arab village.

#### gdd03-0482 - Bir Zeit (birzeit)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [1, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0484 - Tarqumiya (tarqumiyah)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-4, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0486 - Beituniya (beitunia)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [0, 21]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0488 - Beit Fajjar (beit_fajjar)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-2, 25]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0490 - Beit Iksa (beit_iksa)

- Holder on 15 May 1948: unresolved
- Status / confidence: NEEDS-RESEARCH / low
- Evidence:
  - https://en.wikipedia.org/wiki/Beit_Iksa (en.wiki, secondary; read 2026-10-27); April 1948: "the Haganah entered the village destroying many buildings" (Morris 1987 pp. 114, 158); post-war Jordanian rule (no footnote).
- Repo: opening code c (all periods cIIIIIIIIIIIIIII; hex [-1, 23]). no (repo opening c).
- Open gaps: Whether the Yishuv stayed after the April raid; Nabi Samwil front position on 15 May.

#### gdd03-0492 - Beit Safafa (beit_safafa)

- Holder on 15 May 1948: unresolved
- Status / confidence: NEEDS-RESEARCH / low
- Evidence:
  - https://en.wikipedia.org/wiki/Beit_Safafa (en.wiki, secondary; read 2026-10-27); only the post-war division ("the village was divided in two", Klein 2007 p. 70); no 1948 dated event.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 24]). no.
- Open gaps: Dated source for 15 May (Mekor Haim / Ramat Rachel fronts).

#### gdd03-0494 - Mazra'a esh Sharqiya (al_mazraa_ash_sharqiya)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0496 - Taiyiba (taybeh_ramallah)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0498 - Sinjil (sinjil)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 19]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0500 - Beit Aula (beit_ula)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-4, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0502 - Tell es Safi (tell_es_safi)

- Holder on 15 May 1948: not-Yishuv (holder unresolved; Givati took the area 9-10 July in Operation An-Far)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Tell_es-Safi (en.wiki, secondary; read 2026-10-27); Givati order of 7 July 1948 to take the Tall al-Safi area (Morris 2004 p. 436); depopulated 9-10 July 1948, "Military assault by Yishuv forces" (Morris 2004 p. xix).
- Repo: opening code I (all periods IIIIIIIIIIIIIIII; hex [-5, 24]). CONTRADICTION: repo opening I (all periods I).
- Open gaps: Who was in Tell es-Safi on 15 May (villagers; Egyptian forces?). Check whether hex (-5,24) centre is the village or Jewish-held ground (repo I in every period).

#### gdd03-0504 - Shuyukh (ash_shuyukh)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://en.wikipedia.org/wiki/Hebron (en.wiki, secondary; read 2026-10-27); quote: "At the beginning of the 1948 Arab–Israeli War, Egypt took control of Hebron." [fn 154, not opened]; same article: May-Oct 1948 Egypt and Jordan each appointed a military governor (paraphrase from summarised source access). No day given. Conflicts in timing with the Legion presence at Etzion 12-14 May [C].
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-3, 26]). no.
- Open gaps: No place-specific dated source found; region-level only (Hebron hills: Legion companies and local irregulars at Etzion 12-14 May; Legion plan for Hebron 14 May). Never captured by the Yishuv in 1948 per later armistice - NOT used as evidence (Q2).

#### gdd03-0506 - Zakariya (az_zakariyya)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Az-Zakariyya (en.wiki, secondary; read 2026-10-27); taken in Oct 1948; Givati 54th Battalion found it "almost empty" (Morris 2004 p. 521).
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-4, 24]). no.
- Open gaps: Capture Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced.

#### gdd03-0508 - Qatanna (qatanna)

- Holder on 15 May 1948: not-Yishuv (holder unresolved; village "not occupied or depopulated in 1948")
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Qatanna (en.wiki, secondary; read 2026-10-27); "although Qatanna itself was not occupied or depopulated in 1948" (Abu Haneya, Jerusalem Quarterly 93 (2023) p. 39). Undated, whole-year statement.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 22]). no.
- Open gaps: Who held Qatanna on 15 May (local militia? ALA? Legion from late May?).

#### gdd03-0510 - Khadr (al_khader)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (en.wiki, secondary; read 2026-10-27); event 12-14 May 1948; quotes: "Parts of two Arab Legion companies, assisted by hundreds of local irregulars" (no footnote on the sentence; nearest cite Morris, The Road to Jerusalem (2003) pp. 137-38) / the other settlements "on May 13–14 surrendered to the Legion" (cites Morris, Tablet, 25 June 2014). Scope: Etzion bloc, region-level for the Hebron hills.
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-2, 25]). no.
- Open gaps: No dated place-specific source found for 15 May. Search done: Egyptian/Muslim Brotherhood arrival in Bethlehem (no dated hit); a Ben-Gurion diary of 30 Dec 1948 (bengurionarchive.bgu.ac.il node 5237, snippet only) names "Egyptian forces from Bethlehem and Hebron", far after the date.

#### gdd03-0512 - Kafr Malik (kafr_malik)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0514 - Abud (aboud)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [0, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0516 - Deir Jarir (deir_jarir)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [2, 20]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found.

#### gdd03-0518 - Eizariya (al_eizariya)

- Holder on 15 May 1948: unresolved (Arab side; the Arab Legion reached Jerusalem only on 18 May)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no.
- Open gaps: No place-specific dated source for 15 May. Search for Mount of Olives / Augusta Victoria / Silwan in May 1948 returned nothing dated. Region-level only: Arab fighters held the Old City outside the Jewish Quarter; Legion arrival 18 May.

#### gdd03-0520 - Qubeiba (al_qubayba_hebron)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Al-Qubayba,_Hebron (en.wiki, secondary; read 2026-10-27); "was taken by Israeli forces in the final stages of Operation Yoav on 28 October 1948" (no footnote; infobox Morris 2004 p. xix #323); "first attacked during Operation Barak" (undated, no footnote).
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-6, 26]). no.
- Open gaps: Capture 28 Oct 1948 shows only that the Yishuv did not hold it on 15 May; who did (villagers, irregulars, Egyptian/Brotherhood) on 15 May is unsourced. Qubeiba: date of the Operation Barak attack (9-12 May?) needed - if it was occupied in Barak the holder changes.

#### gdd03-0522 - Battir (battir)

- Holder on 15 May 1948: unresolved
- Status / confidence: NEEDS-RESEARCH / low
- Evidence:
  - https://en.wikipedia.org/wiki/Battir (en.wiki, secondary; read 2026-10-27); undated 1948: "When nearing the village, the Israelis thought Battir was still inhabited and gave up attacking." (FAZ, 21 June 2012).
- Repo: opening code A (all periods AMMMMMMMMMMIIIII; hex [-2, 24]). no.
- Open gaps: Any dated 1948 record.

#### gdd03-0524 - Beit Liqya (beit_liqya)

- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low
- Evidence:
  - https://history.state.gov/historicaldocuments/frus1948v05p2/d268 (FRUS 1948 vol. V pt 2, doc. 268, Ambassador in UK (Douglas) to Secretary of State, London, 14 May 1948, 5 p.m.); quote: "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector". Scope: region-level; a stated PLAN on 14 May, not an observed position.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-1, 22]). no.
- Open gaps: Region-level only (Ramallah sub-district). No place-specific dated record found. Beit Liqya lies near the Latrun-Beit Horon front: check Operation Maccabi / Yoram sources for a May action.

#### gdd03-0526 - Hulda (hulda)

- Holder on 15 May 1948: Yishuv/Haganah (kibbutz; staging base for Jerusalem convoys; neighbouring Khulda village taken 6 April)
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://he.wikipedia.org/wiki/חולדה_(קיבוץ) (he.wiki; read 2026-10-27): "במלחמת העצמאות הייתה חולדה הנקודה המזרחית ביותר אשר היה לה קשר גאוגרפי ישיר עם היישוב ומוסדותיו" (Yad Tabenkin archive, n. 11); "עם קום המדינה היוותה יעד להתקפות מטוסי האויב אשר נמשכו כשמונה ימים רצופים" (no cite).
  - https://www.hamichlol.org.il/ח'ולדה (Hamichlol, Khulda village; read 2026-10-27): "במהלך מבצע נחשון ב-6 באפריל 1948 הכפר נכבש, והתרוקן מתושביו"; "המקום היה הבסיס העיקרי של הכוחות שהגיעו מהשפלה".
  - https://en.wikipedia.org/wiki/Operation_Nachshon (en.wiki, secondary; read 2026-10-27); Khulda, 6 April 1948, Haganah, "no resistance"; bulldozed 20 April (table from Khalidi / Morris).
- Repo: opening code I (all periods IIIIIIIIIIIIIIII; hex [-4, 23]). no (repo I).
- Open gaps: A source dated 14-16 May naming the Hulda garrison (the "with the state's founding" air attacks sentence is uncited).

#### gdd03-0527 - Latrun police fort (latrun_fort)

- Holder on 15 May 1948: mixed/contested (British leave 14 or 15 May [C]; Givati platoon enters the fort morning 15 May per Lapierre & Collins, or a Legion platoon with irregulars takes it over per Morris [C]; Legion holds it by 16-18 May)
- Status / confidence: PARTIAL / medium for "contested", low for any single holder
- Evidence:
  - https://en.wikipedia.org/wiki/Operation_Maccabi (en.wiki, secondary; read 2026-10-27); quotes: "Between 9–11 May, a battalion of the Harel brigade attacked and took the village of Bayt Mahsir" (Morris 2008 p. 132); the Sha'ar HaGai battalion held hills north and south of the road (paraphrase, Morris 2008 p. 132); Givati platoon penetrated the Latrun police fort "without encountering any resistance on the morning of 15 May" (Lapierre & Collins p. 611); Morris (2002 p. 152): a Legion platoon of the 11th Company with irregulars took over the fort (paraphrase); "The first contingent of 40 legionnaires" on the night of 15-16 May (Morris 2002 p. 152).
  - https://he.wikipedia.org/wiki/מבצע_מכבי (he.wiki, secondary; read 2026-10-27); quotes: 10 May "כוח מהגדוד הרביעי כבש את משלט המסרק (משלט 17)" (and found Beit Mahsir empty, paraphrase); 14 May "פינו הבריטים את לטרון"; 15 May "עזבו כוחות 'צבא ההצלה'" (vacuum 15-17 May, paraphrase); night 15-16 May "כוח של חטיבת גבעתי כבש את הכפר לטרון"; night 16-17 May "כוח של הלגיון תפס את לטרון".
  - https://en.wikipedia.org/wiki/Battles_of_Latrun_(1948) (en.wiki, secondary; read 2026-10-27): British left 14 May (no source given); ALA ordered to withdraw ~14-15 May, Gelber pp. 138-145: ~200 irregulars held it; 18 May the Legion's strength around Latrun and Bab al-Wad sufficient to block the road (Morris 2002 p. 152) (paraphrases from summarised source access).
  - https://en.wikipedia.org/wiki/Burma_Road_(Israel) (en.wiki, secondary; read 2026-10-27); quotes: "British forces withdrew from the Latrun monastery and Tegart fort that dominated the road" (15 May, no footnote); footnote 2 (Hecht, Jerusalem Post, 8 Sep 2009): "as the British garrison left Latrun on May 14, 1948, the Arab Legion of Transjordan moved in"; "on the night of May 18, British-officered Arab Legion forces from Transjordan seized Latrun". [C] on 14/15/18 May.
- Repo: opening code A (all periods AMMMMMMMMMMJJJJJ; hex [-2, 22]). no (repo opening A; sources conflict, none directly says Yishuv held it through 15 May).
- Open gaps: Resolve L&C p. 611 vs Morris 2002 p. 152 (and Hecht: Legion "moved in" 14 May) with a primary: IDF/Givati report or Glubb.

#### gdd03-0528 - Bab al-Wad (Sha'ar HaGai) (shaar_hagai)

- Holder on 15 May 1948: Yishuv/Haganah (Harel held the heights north and south of Bab al-Wad from 9-11 May); 15-17 May "vacuum" in the Latrun sector; Legion blocks the road by 18 May
- Status / confidence: PARTIAL / medium
- Evidence:
  - https://en.wikipedia.org/wiki/Operation_Maccabi (en.wiki, secondary; read 2026-10-27); quotes: "Between 9–11 May, a battalion of the Harel brigade attacked and took the village of Bayt Mahsir" (Morris 2008 p. 132); the Sha'ar HaGai battalion held hills north and south of the road (paraphrase, Morris 2008 p. 132); Givati platoon penetrated the Latrun police fort "without encountering any resistance on the morning of 15 May" (Lapierre & Collins p. 611); Morris (2002 p. 152): a Legion platoon of the 11th Company with irregulars took over the fort (paraphrase); "The first contingent of 40 legionnaires" on the night of 15-16 May (Morris 2002 p. 152).
  - https://he.wikipedia.org/wiki/מבצע_מכבי (he.wiki, secondary; read 2026-10-27); quotes: 10 May "כוח מהגדוד הרביעי כבש את משלט המסרק (משלט 17)" (and found Beit Mahsir empty, paraphrase); 14 May "פינו הבריטים את לטרון"; 15 May "עזבו כוחות 'צבא ההצלה'" (vacuum 15-17 May, paraphrase); night 15-16 May "כוח של חטיבת גבעתי כבש את הכפר לטרון"; night 16-17 May "כוח של הלגיון תפס את לטרון".
  - https://en.wikipedia.org/wiki/Battles_of_Latrun_(1948) (en.wiki, secondary; read 2026-10-27): British left 14 May (no source given); ALA ordered to withdraw ~14-15 May, Gelber pp. 138-145: ~200 irregulars held it; 18 May the Legion's strength around Latrun and Bab al-Wad sufficient to block the road (Morris 2002 p. 152) (paraphrases from summarised source access).
  - https://en.wikipedia.org/wiki/Sha%27ar_HaGai (en.wiki, secondary; read 2026-10-27); Harel "managed to capture Bab al-Wad itself" (undated, unsourced); 20 Apr Arabs "recaptured the heights around Bab al-Wad" (Bowyer Bell 1966 p. 216).
- Repo: opening code A (all periods AMcIIIIIIIIIIIII; hex [-2, 23]). CONTRADICTION (probable): repo opening A.
- Open gaps: Primary dated source that the Sha'ar HaGai battalion was on the Bab al-Wad positions on 15 May (Palmach database, Sefer HaPalmach). The hex (-2,23) holder vs the road site may differ.

#### gdd03-0529 - East Jerusalem / Old City (east_jerusalem_old_city)

- Holder on 15 May 1948: mixed/contested (Jewish Quarter: Haganah, besieged; rest of the Old City: Arab irregulars; Arab Legion only from 18 May)
- Status / confidence: SOURCED / medium
- Evidence:
  - https://m.knesset.gov.il/En/About/Documents/JerEnPDF1.pdf (Knesset, "Liberation of Jerusalem"; PDF created 29 Apr 2021 per its metadata; text extracted from the fetched PDF, spacing normalised); event dates 14 and 18 May 1948; quotes: "On May 14, 1948, upon the departure of the British from the country, the Israeli forces began to take over compounds held by the Mandatory government." / "On May 18, the Arab Legion reached Jerusalem and entered the Old City." Scope: city-level.
  - https://www.daat.ac.il/DAAT/english/history/lapidot/30.htm (Yehuda Lapidot, "Besieged", section "The Arab Legion attacks Jerusalem"; book date not shown on page); event dates 13-19 May 1948; quotes (through summarised source access): "In the early morning of May 15, 1948, the Arab Legion crossed the Jordan by the Allenby Bridge and reached Jericho." / "its forces were deployed along the Nablus-Jenin-Tulkarem triangle and in Ramallah." / "Haganah and Irgun forces seized the 'security zones' (most of them in Jewish-owned buildings) without a fight" (14 May) / "On May 13, the British Army evacuated the Old City" / Old City: "more than 700 well-equipped Arab fighters there as against about 100 ill-equipped Jewish ones". Scope: Jerusalem and region-level (Ramallah, Jericho).
- Repo: opening code m (all periods mMMMMMMMMMMJJJJJ; hex [0, 23]). no vs opening code m; NOTE: content/map/locations_1948.yaml gives starting_control: israeli for the whole Old City marker, which the fetched sources do not support beyond the Jewish Quarter.
- Open gaps: Holder of the non-Jewish quarters on 15 May named only as "Arab fighters" (Lapidot); which formation (Jihad al-Muqaddas, ALA remnants, local) unsourced. Repo locations_1948 starting_control: israeli covers the whole marker: owner to rule on scope.

#### gdd03-0530 - Beit Shemesh (beit_shemesh)

- Holder on 15 May 1948: mixed/unresolved: the Jewish moshava Hartuv (about 2 km N of the marker) was held by its defenders until evacuated on the night of 15-16 May by Givati bn 52, then held by a Muslim Brotherhood force until 17-18 July; the marker site itself (Arab village lands) unresolved
- Status / confidence: PARTIAL / medium for Hartuv, low for the site
- Evidence:
  - https://he.wikipedia.org/wiki/הרטוב (he.wiki "הר-טוב"; read 2026-10-27): "בלילה שבין ה-15 ל-16 במאי 1948 יצאו במסע לילי מאובטח"; "המשימה הוטלה על כוח מגדוד 52 של גבעתי בפיקודו של שלמה להט"; afterwards "הר-טוב נשלטה בידי כוח של האחים המוסלמים" until freed by Harel between 17 and 18 July 1948.
  - https://en.wikipedia.org/wiki/Hartuv (en.wiki, secondary; read 2026-10-27); "was abandoned again" in 1948, no date.
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-3, 24]). no direct contradiction (repo opening A); POSSIBLE if the hex (-3,24) centre is Hartuv.
- Open gaps: Check Hartuv coordinates vs hex (-3,24) centre; dated holder of Artuf / Ayn Shams lands on 15 May. beit_shemesh_field has no lon/lat in the group file.

#### gdd03-0532 - Beit Shemesh fields (beit_shemesh_field)

- Holder on 15 May 1948: mixed/unresolved: the Jewish moshava Hartuv (about 2 km N of the marker) was held by its defenders until evacuated on the night of 15-16 May by Givati bn 52, then held by a Muslim Brotherhood force until 17-18 July; the marker site itself (Arab village lands) unresolved
- Status / confidence: PARTIAL / medium for Hartuv, low for the site
- Evidence:
  - https://he.wikipedia.org/wiki/הרטוב (he.wiki "הר-טוב"; read 2026-10-27): "בלילה שבין ה-15 ל-16 במאי 1948 יצאו במסע לילי מאובטח"; "המשימה הוטלה על כוח מגדוד 52 של גבעתי בפיקודו של שלמה להט"; afterwards "הר-טוב נשלטה בידי כוח של האחים המוסלמים" until freed by Harel between 17 and 18 July 1948.
  - https://en.wikipedia.org/wiki/Hartuv (en.wiki, secondary; read 2026-10-27); "was abandoned again" in 1948, no date.
- Repo: opening code A (all periods AMMMMMccIIIIIIII; hex [-3, 24]). no direct contradiction (repo opening A); POSSIBLE if the hex (-3,24) centre is Hartuv.
- Open gaps: Check Hartuv coordinates vs hex (-3,24) centre; dated holder of Artuf / Ayn Shams lands on 15 May. beit_shemesh_field has no lon/lat in the group file.

### Contradictions

1. tell_es_safi (gdd03-0502): repo opening I (and I in every period) vs en.wiki Tell es-Safi citing Morris 2004 pp. 436-437 and p. xix: Givati ordered to take the area on 7 July and the village was depopulated by a Yishuv assault on 9-10 July 1948, so not Yishuv-held on 15 May (place-level; the hex centre may differ).
2. bayt_mahsir (gdd03-0456), probable: repo opening A vs Harel occupation on 11 May (he.wiki citing Narkiss pp. 90-92; en.wiki citing Morris 2004/2008) and Israeli troops at the village on 13 May (Morris & Kedar 2022 p. 7). Secondary sources only.
3. shaar_hagai (gdd03-0528), probable: repo opening A vs Harel's Sha'ar HaGai battalion holding the heights north and south of the road from 9-11 May (Morris 2008 p. 132 via en.wiki) and the Legion blocking the road only by 18 May (Morris 2002 p. 152; he.wiki: vacuum 15-17 May).

Notes (not counted as contradictions):
- east_jerusalem_old_city: content/map/locations_1948.yaml starting_control: israeli (1948-05-15) for the whole Old City marker; Lapidot puts >700 Arab fighters in the Old City vs ~100 Jewish defenders, and the Knesset dates the Legion's entry to 18 May. Only the Jewish Quarter was Yishuv-held.
- jerusalem (West): sources say Haganah/IZL held it from 14 May; repo hex (0,23) is m because it is shared with the Old City (hex rule), so no direct conflict at hex scale.
- latrun_fort: sources conflict on 15 May (Givati platoon vs Legion platoon in the fort); repo A is consistent with one of them.
- beit_shemesh / beit_shemesh_field: Hartuv Yishuv-held until the night of 15-16 May; conflict with repo A only if the hex (-3,24) centre is Hartuv.

## Northern Hills, Negev and Kiryat Shmuel camp: research notes and sections


Web research, 2026-10-27. 46 items: northern_hills 36, negev 9, Kiryat Shmuel camp 1. palestineremembered.com returned HTTP 403 to every fetch, so Khalidi/Morris village texts were not read. Dupuy (1978) was read from the Stanford excerpt PDF; page numbers are the printed ones.


### Region-level evidence shared by the northern_hills items (R1-R5)

Each northern_hills section repeats R1-R5 so it stands alone. Reading: no fetched source puts the Yishuv in the Nablus-Jenin hills on 15 May. On that day the Arab Legion held the Damiya and Allenby bridges and its 1st Brigade turned toward Nablus. The ALA had its HQ in the area (Jaba' in March, Nablus by late May per Dupuy) and gathered its central units in the triangle after 15 May. The Iraqis reached Nablus only after 16-17 May. So the holder type on 15 May (Legion, ALA, or local militia) is unresolved for every village.

#### gdd03-0540 - Nablus (nablus)
- Holder on 15 May 1948: not-Yishuv (holder unresolved; Arab Legion 1st Brigade heading toward Nablus on 15 May, ALA HQ there per Dupuy)
- Status / confidence: PARTIAL / medium
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - Place-specific: R1 names Nablus as the place where Kaukji "had his ALA headquarters" when the Iraqis moved in (after 16-17 May); R3 has the Legion's 1st Brigade turning "צפונה לעבר שכם" on 15 May. Neither says who was in the town on 15 May itself. Note the conflict on the ALA HQ site: en.wiki Arab Liberation Army (fetched 2026-10-27) says Qawuqji in March 1948 "set his headquarters in the Jaba village between Nablus and Jenin" (no footnote); Dupuy p. 51 says Nablus.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0542 - Tubas (tubas)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - The 25 May CIA map's "Nablus-Tubas" Arab troop concentration (R5) is the only item naming Tubas; dated 25 May, not 15 May.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0544 - Umm el Fahm (umm_al_fahm)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - Context only: Dupuy p. 52 has the Alexandroni Brigade "expected to carry out an attack through the Wadi Ara" in early June 1948 (Arab-held corridor then). Repo codes later turn `I` (the 1949 Wadi Ara transfer), which says nothing about 15 May.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJIII`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0546 - Jenin (jenin)
- Holder on 15 May 1948: not-Yishuv (holder unresolved; local irregulars likely, Iraqis arrive ~1 June)
- Status / confidence: PARTIAL / medium
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - en.wiki Battle of Jenin (1948) (https://en.wikipedia.org/wiki/Battle_of_Jenin_(1948), fetched 2026-10-27; secondary, cites Malovany 2017 pp. 40-44 and Tal 2004 p. 233): the 4th Brigade's advance force and the 5th Brigade's 2nd Battalion "already arrived on 1 June"; infobox lists "Arab irregulars" and "Local Arab militias" (Tal 2004, p. 233). Dupuy p. 52: on 4 June "the Golani battalion occupied Jenin" and withdrew the next night. Event dates 28 May - 4 June; they show Jenin was not Yishuv-held before 4 June but do not name the 15 May holder.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0548 - Arraba (arraba_2)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0550 - Qabatiya (qabatiya)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0552 - Ya'bad (yabad)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0554 - Silat edh Dhahr (silat_adh_dhahr)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0556 - Buraq (burqa_nablus)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0558 - Yamun (al_yamun)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0560 - Kafr Ra'i (kafr_rai)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0562 - Jaba' (jaba_2)
- Holder on 15 May 1948: not-Yishuv (holder unresolved; ALA HQ here in March 1948)
- Status / confidence: PARTIAL / low
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - Place-specific but not dated to May: en.wiki Arab Liberation Army (https://en.wikipedia.org/wiki/Arab_Liberation_Army, fetched 2026-10-27): "On 5 March 1948 Qawuqji returned to Palestine and set his headquarters in the Jaba village between Nablus and Jenin." (no footnote). Dupuy p. 51 places the ALA HQ at Nablus by late May. Whether the ALA was still at Jaba' on 15 May is unsourced. Check that this Jaba' (35.2222, 32.3239) is the same Jaba' as Qawuqji's HQ (the Jaba' between Nablus and Jenin) before relying on it.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0564 - Tammun (tammun)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0566 - Aqraba (aqraba)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0568 - Asira esh Shamaliya (asira_ash_shamaliya)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0570 - Silat el Harithiya (silat_al_harithiya)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0572 - Salfit (salfit)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0574 - Talluza (talluza)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0576 - Beita (beita)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0578 - Birqin (burqin)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0580 - Awarta (awarta)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0582 - Zir'in (zirin)
- Holder on 15 May 1948: Arab irregulars / local militia (as of 28 May; 15 May not directly stated)
- Status / confidence: PARTIAL / medium
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - Place-specific: Dupuy p. 51 (R1 PDF): "On May 28 the Golani Brigade began its offensive against Arab irregulars holding the Mt. Bilboa [sic, Gilboa] Range northeast of Jenin, and captured Zaryin." Event date 28 May 1948.
  - en.wiki Zir'in (https://en.wikipedia.org/wiki/Zir%27in, fetched 2026-10-27; secondary): "the village was captured on May 28 by the Thirteenth Battalion of the Golani Brigade" (fn: Tal 2004, p. 258); "On April 19, Zir'in was briefly captured" (fn: Morris 1987, p. 159, quoted in Khalidi 1992, p. 339); defenders in March were "the Arab Liberation Army (ALA)" (fn: Khalidi 1992, p. 339).
  - Reading: not Yishuv-held on 15 May (captured 28 May; the 19 April capture was "briefly"). Arab irregulars held it on 28 May; that they were the holders on 15 May is likely but not stated for that day.
- Repo: opening code `c` (contested; all periods `cIcMMMMMMMMJJJII`). No fetched source directly conflicts with it. The sources read as not Yishuv-held, Arab irregulars present, which would fit `A`; `c` versus `A` is for the owner to decide. Not a CONTRADICTION.
- Open gaps: read Khalidi p. 339 and Morris (2004) on Zir'in directly (palestineremembered 403 here); confirm no Haganah presence between 19 April and 28 May. Repo opening `c` (contested) is not contradicted by any fetched source but is also not supported by a dated 15 May record.

#### gdd03-0584 - Bidya (biddya)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0586 - Meithalun (meithalun)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0588 - Qabalan (qabalan)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0590 - Huwwara (huwara)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0592 - Beit Furik (beit_furik)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0594 - Jamma'in (jammain)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0596 - Kafr Qaddum (kafr_qaddum)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0598 - Burin (burin)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0600 - Deir Istiya (deir_istiya)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0602 - Qusra (qusra)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0604 - Tell (tell_nablus)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0606 - Sanur (sanur_jenin)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0608 - Sabastiya (sebastia)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJJJJ`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0610 - Barta'a (bartaa)
- Holder on 15 May 1948: not-Yishuv (holder unresolved)
- Status / confidence: PARTIAL / low (region-level evidence only)
- Evidence:
  - R1. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27, text extracted locally); book date 1978; event dates 15 to 25 May 1948; p. 51; "the Damiya and Allenby Bridges, which had been occupied intact early on the morning of May 15 by the Trans-jordanian Arab Legion. They were able to move their entire contingent across the river to Nablus, where Kaukji had his ALA headquarters." and "On May 25 the Iraqis struck west from Nablus past the Arab city of Tulkarm."; scope: region-level (Nablus area). The Iraqi move to Nablus comes after the Gesher attacks of 16-17 May, so it does not date an Iraqi presence on 15 May.
  - R2. Same PDF, chapter 6 "The Battles for Jerusalem", p. 60 (chapter-opening page, number not printed in the extract); event date: after 15 May 1948; "When Kaukji learned of the coordinated Arab invasions of May 15 ... ordered his units that had been operating north of the Jerusalem Corridor to concentrate in the Samarian \"triangle,\""; scope: region-level (Samaria).
  - R3. he.wiki הלגיון הערבי (https://he.wikipedia.org/wiki/%D7%94%D7%9C%D7%92%D7%99%D7%95%D7%9F_%D7%94%D7%A2%D7%A8%D7%91%D7%99, fetched 2026-10-27, current revision; secondary, cites Morris, *Righteous Victims*, p. 215); event date 15 May 1948; "ב-15 במאי עם שחר חצה הלגיון את גבול ארץ ישראל." and "חטיבה 1 של הלגיון פנתה צפונה לעבר שכם."; scope: region-level (toward Nablus; not an arrival time).
  - R4. FRUS 1948 vol. V pt 2, doc. 268, Douglas (London) to the Secretary of State, 14 May 1948, 5 p.m. (https://history.state.gov/historicaldocuments/frus1948v05p2/d268, fetched 2026-10-27); "Arab legion will establish itself Hebron, Ramallah and Nablus and then extend its sector."; scope: a PLAN, not proof of a garrison.
  - R5. Repo (read-only), `research_notes/Map borders/control-15-may-1948.md` (origin/main), sections 1-2 and the sub-district table: CIA Intelligence Summary map 10981, "Military Situation - 18 May 1948", and the table row "Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn)"; the 25 May map (10984) shows "Arab troop concentrations (... Nablus-Tubas ...)". Dated 18 and 25 May 1948; region-level; line courses [S] at overview resolution.
  - Context only: as Umm el Fahm (Wadi Ara edge; Dupuy p. 52). Later `I` codes reflect the 1949 armistice, not 15 May.
- Repo: opening code `A` (all periods `AMMMMMMMMMMJJIII`). No fetched source conflicts with it; `A` (other side, unnamed) matches the not-Yishuv reading. Not a CONTRADICTION.
- Open gaps: no place-specific record for 14-15 May 1948 was found. A person must find who was physically in the village on 15 May (local National Committee or village militia, ALA remnants, the Arab Legion's 1st Brigade passing toward Nablus, or nobody under arms). Candidate primaries: Glubb, *A Soldier with the Arabs* (1957); the IDF History Branch's *Toldot Milhemet HaKomemiyut* (1959); the Haganah Archive intelligence (Shai) village files; Arab Legion war diaries. palestineremembered.com returned HTTP 403 during this research, so Khalidi/Morris texts were not read for these places.

#### gdd03-0712 - Beersheba (beersheba)
- Holder on 15 May 1948: not-Yishuv (holder unresolved). An Arab town; the Egyptian inland column was en route on 15 May and its advance guard arrived on 17 May. Who was under arms in the town on 15 May (local militia, Muslim Brotherhood volunteers, police) is not stated.
- Status / confidence: PARTIAL / medium
- Evidence:
  - Dupuy, *Elusive Victory* (1978), excerpt PDF https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf (fetched 2026-10-27), p. 56: "Early on May 15, both columns crossed the frontier, the larger brigade at Rafah, the smaller at El Auja." and the plan: "The inland column was to advance by way of Beersheba and Hebron"; p. 58: "Unopposed in this desolate Arab territory, the motorized advance guard of the column reached Beersheba on May 17. The main body arrived there on the 20th." Event dates 15-20 May 1948; place-specific. Shows no Egyptian army in the town on 15 May and no Israeli presence.
  - Jewish Virtual Library, "The War of Independence: The Negev and The South" (https://www.jewishvirtuallibrary.org/the-negev-and-the-south-may-1948, credits idf.il; fetched 2026-10-27): the Egyptians "advanced in two columns: one North along the coastal Road and the other South-east towards Beersheba." (15 May; region-level). Note: the same page says "Syrian forces attacked Kibbutz Nirim", an evident error, so treat it as weak.
  - Repo: `research_notes/GDD phase 6/settlement-targets.md` (origin/main) Beersheba section: Egyptian arrival 17 May (Tal), 19 May (Morris), 20 May (O'Ballance), advance guard 17 / main body 20 May (Dupuy p. 58) [C]; "Beersheba was in Arab hands before the Egyptian inland column arrived in May." The Dupuy dates are now confirmed from the fetched PDF.
  - Repo `research_notes/Map/enemy-held-1948-05-15.md` line 43 (proposed association): "say \"the other side\", not \"Egypt\", for that day." Consistent with this finding.
- Repo: opening code `-` (all periods `-MMMMMccIIIIIIII`). No colour; no fetched source conflicts. If coloured, `A` (other side, unnamed) fits; `E` would back-project 17-20 May. Not a CONTRADICTION.
- Open gaps: who held the town on 14-15 May (the British left Beersheba before 14 May? date not found); read Morris 1948 (p. 466 area) and the Negev Brigade history for Beersheba in mid-May.

#### gdd03-0713 - Revivim (revivim)
- Holder on 15 May 1948: Yishuv/Haganah (the kibbutz's own defenders, reinforced by a Palmach company from February 1948). Original site (the kibbutz moved after the war).
- Status / confidence: SOURCED / medium. Basis: a place-specific source states a continuous hold from December 1947 through the Egyptian invasion to December 1948; it does not name 15 May itself.
- Evidence:
  - Encyclopaedia Judaica (2007), "Revivim", on the Jewish Virtual Library (https://www.jewishvirtuallibrary.org/revivim, fetched 2026-10-27): "In December 1947, after the outbreak of hostilities, ... the kibbutz rebuffed strong attacks of Bedouin irregulars" and "later, completely cut off by the invading Egyptian army, it held out" "until Israel forces broke through in Operation Yo'av (December 1948)." (sic; Yoav was October 1948, Horev December). Place-specific, span covering 15 May.
  - Palmach heritage database, "הגדוד השמיני – הנגב הדרומי" (https://palmach.org.il/history/database/?itemId=6201, fetched 2026-10-27): February 1948: "פלוגה מוקטנת שנייה בפיקוד \"גנדי\" (יהושע ברנשטיין) תגברה את הישובים הדרומיים רביבים, חלוצה ועלומים". Place-specific, dated February 1948.
  - en.wiki Revivim (https://en.wikipedia.org/wiki/Revivim, fetched 2026-10-27; secondary, cites W. O. Douglas 1951, p. 287): "Revivim fell behind Egyptian lines for several months."
  - he.wiki רביבים (fetched 2026-10-27): "הקיבוץ עלה לקרקע ב-7 ביולי 1943"; "היה הקיבוץ נתון תחת מצור מצרי" (no date, no footnote).
- Repo: opening code `-` (all periods `-----------IIIII`). The repo note (`control-15-may-1948.md` section 6) says Negev settlements are sub-hex islands that cannot be drawn as areas. A source says Yishuv-held while the hex is uncoloured: a mismatch to note, not a direct conflict (`-` names no holder). Not listed as a CONTRADICTION.
- Open gaps: a document dated 14-16 May naming Revivim (Negev Brigade operations log, Ben-Gurion diary 15 May). The original site's coordinates against the hex (-11, 34) should be checked, since the kibbutz moved in 1949.

#### gdd03-0714 - Beit Kama (beit_kama)
- Holder on 15 May 1948: unresolved. A future founding site (1949); nothing was there to "hold" as a settlement, and no fetched source places either side's forces on this ground on 15 May.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: region-level only. Dupuy p. 44 (PDF above): the Negev Brigade "was responsible for the security of the semi-isolated Israeli Negev settlements, west and northwest of Beersheba"; p. 55: on 14 May, after the British left, Azziz occupied Iraq Suwaydan, which "bisected the Jewish supply road to the Negev through Brier and Huleiqat". Repo `control-15-may-1948.md` section 1 item 4: "The south cannot be coloured by side on 15 May" (CIA 18 and 25 May maps draw no Jewish-held border there). None of this is about the Beit Kama site.
- Repo: opening code `I` (all periods `IIIIIIIIIIIIIIII`, hex source "proposed association"). No fetched source supports `I` on 15 May; none contradicts it directly. Flag: the `I` opening looks back-projected from the later Israeli line (the research rule), so it should be checked. Not a CONTRADICTION.
- Open gaps: identify the nearest 1948 localities to (34.761, 31.449) (Bedouin encampments, Ruhama, Dorot) and their 15 May status; the IDF 1:100,000 battle-map sheets (repo source 8).

#### gdd03-0716 - Beit Kama fields (beit_kama_field)
- Holder on 15 May 1948: unresolved (same hex as Beit Kama; no coordinates given).
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: as gdd03-0714; nothing field-specific found.
- Repo: opening code `I`; as gdd03-0714 (unsourced, not contradicted).
- Open gaps: as gdd03-0714.

#### gdd03-0718 - Re'im (reim)
- Holder on 15 May 1948: unresolved. Future founding site (1949), near the Gaza Strip edge.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: region-level only. Dupuy p. 56: "On May 14, with invasion imminent, two battalions of the Negev Brigade, consisting of about 800 men, were deployed to observe the southwestern frontier." Palmach item 6201 (above): the 8th Battalion's sector was "הישובים שמדרום לקו חצרים-בארי". Neither places forces on the Re'im ground on 15 May. The repo's later `E` periods do not apply to 15 May.
- Repo: opening code `-` (all periods `-EEEEEEEE-------`). Consistent with "no sourced holder". Not a CONTRADICTION.
- Open gaps: the status of nearby Be'eri (1946) and of the Arab/Bedouin land around (34.459, 31.386) on 15 May; the palmach.org.il items the repo's sites.yaml cites (7953, 5312) were not reopened here.

#### gdd03-0720 - Re'im fields (reim_field)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: as gdd03-0718.
- Repo: opening code `-`; consistent.
- Open gaps: as gdd03-0718.

#### gdd03-0722 - Gilat (gilat)
- Holder on 15 May 1948: unresolved. Future founding site (1949).
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: region-level only (Dupuy p. 44 and p. 56; repo note section 1 item 4, as above). Nothing about this site.
- Repo: opening code `-` (all periods `-IIIIIIIIIIIIIII`). Consistent. Not a CONTRADICTION.
- Open gaps: the nearest 1948 points to (34.652, 31.328) and their mid-May status.

#### gdd03-0724 - Gilat fields (gilat_field)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence: as gdd03-0722.
- Repo: opening code `-`; consistent.
- Open gaps: as gdd03-0722.

#### gdd03-0726 - Umm Rashrash / future Eilat (umm_rashrash)
- Holder on 15 May 1948: unresolved. No 1948 source found. Later evidence shows only that the Yishuv did not hold it until 10 March 1949.
- Status / confidence: NEEDS-RESEARCH / low
- Evidence (all later, context only):
  - Hamichlol אום רשרש (https://www.hamichlol.org.il/%D7%90%D7%95%D7%9D_%D7%A8%D7%A9%D7%A8%D7%A9, fetched 2026-10-27): 1933 police post, "השוטרים היו ברובם בדואים בני הסביבה בפיקוד בריטי"; on 10 March 1949, "תחנת המשטרה הנטושה" (the abandoned police station). No 1947-48 holder given.
  - Search snippets only (not fetched): a UN acting-mediator cablegram of 13 March 1949 reports Israeli forces occupied "Umm Reshresh" on 10 March; a FRUS 1949 telegram (frus1949v06/d529) reportedly says Israeli units saw an Arab Legion unit entrenched there. This conflicts with "abandoned" but concerns March 1949, not May 1948.
- Repo: no hex, no opening code.
- Open gaps: Mandate police records for the post's evacuation (when the British left, before or after 15 May 1948); Arab Legion southern deployments in May 1948 (Aqaba garrison).

#### gdd03-0731 - Kiryat Shmuel camp, Haifa (kiryat_shmuel_camp_haifa)
- Holder on 15 May 1948: unresolved. Two candidates: (a) inside the Haganah-held Haifa Bay area; (b) inside or under the British Haifa evacuation enclave (British forces stayed until 30 June). No fetched source places the camp's footprint inside or outside the enclave.
- Status / confidence: PARTIAL / low. human_check_required: a person must inspect the primaries (Yassky memorandum of 18 July 1947; Davar 7 Feb 1947 and 4 Jan 1948; 9 Sep and 6 Dec 1949 for later context only).
- Evidence:
  - he.wiki מחנה העולים בקריית שמואל, oldid 39577890 (7 Sep 2024) (https://he.wikipedia.org/w/index.php?oldid=39577890, fetched 2026-10-27; secondary): "המחנה עצמו הוקם לקראת ראשית 1947 ופעל עד הקמת מדינת ישראל"; the camps were run inside by the Jewish Agency and Hadassah and "נשמרו מבחוץ על ידי שוטרים בריטיים" (cites the Yassky memorandum, 18 July 1947, *Political Documents of the Jewish Agency 1947*, pp. 468-469); "בינואר 1948 הובלו למחנה העולים בקריית שמואל עולים בלתי לגליים" (Davar, 4 Jan 1948). So British police guarded the camp's perimeter in 1947 to January 1948. Nothing dated May 1948.
  - Royal Marines History, "40 Cdo in Haifa ... January to June, 1948" (https://www.royalmarineshistory.com/post/40-cdo-in-haifa-palestine-mandate-and-the-withdrawal-of-british-troops-january-to-june-1948, fetched 2026-10-27): "The Mandate ended on 14 May and the British withdrew onto Haifa through a series of enclaves." It gives no perimeter; it does not mention the Krayot or the camp.
  - Repo `research_notes/Map borders/control-15-may-1948.md` section 1 item 5 (origin/main): S/1025 para. 28 "the Haifa enclave", the camps dispute settled 18 June, evacuation complete 30 June [V there]; the 25 May CIA map stipples a "British evacuation area" around Haifa and inland to an "(Air field)" [V there]; "No source found gives its boundary to better than that sketch". Section 6: the enclave "lay inside the Israeli-held area". Region-level. (S/1025 itself did not render through the source access.)
- Repo: no hex, no opening code. A "friendly region" design need does not establish control.
- Open gaps: (1) the camp's exact footprint in Kiryat Shmuel (Haifa Bay); (2) whether that footprint was within the British enclave between 15 May and 30 June: check S/1025 para. 28 directly, the 25 May CIA map 10984 stipple at full resolution, and Haganah/Carmeli records of the British perimeter; (3) whether British police still guarded the camp on 14-15 May. The person-check of the Yassky memorandum and the Davar issues is still required.

### Contradictions

None found. No fetched source directly conflicts with a repo opening code.

Mismatches worth a look (not contradictions, since `-` and `c` name no holder and `I` is unsupported rather than refuted):
- Revivim (gdd03-0713): the sources give Yishuv-held (EJ: held out from Dec 1947 through the invasion; Palmach reinforcement in Feb 1948). The repo opening is `-` (uncoloured; the repo note treats Negev settlements as sub-hex islands).
- Beit Kama and Beit Kama fields (gdd03-0714, -0716): the repo opening is `I` with no dated 15 May source. It looks back-projected from later lines.
- Zir'in (gdd03-0582): the repo opening is `c`. Dupuy has Arab irregulars holding the area up to the Golani capture of 28 May, so it was not Yishuv-held; `c` versus `A` is for the owner to decide.
- A source-to-source conflict on the ALA HQ: en.wiki puts it at Jaba' (March 1948), Dupuy p. 51 at Nablus (late May).
- A source-to-source conflict at Umm Rashrash (March 1949, context only): hamichlol says the police post was abandoned; a FRUS snippet says an Arab Legion unit was entrenched there.

## Southern Coast: research notes and sections


Web research, 2026-10-27. Sources: English Wikipedia village articles (secondary pointers
citing Morris 2004 / Khalidi 1992), Palmach Heritage English database pages (8474, 8476, 8480, 6190, 8465),
Jewish Virtual Library "The Negev and The South (May 1948)" (credited to the IDF), cojs.org reprints of the Kansas
City Star 16 May 1948 (UP) and AP 23 May 1948 dispatches, hamichlol.org.il (Hebrew wiki mirror) "הקרבות על בית
דראס", moreshet-map.org.il Nitzanim page. palestineremembered.com and palquest.org returned HTTP 403,
so no Khalidi/Morris text was read first-hand; every Morris/Khalidi page number below is "as cited by" the
Wikipedia article and needs a person to check the book. Quotes below are short fragments returned through a summariser
reading the page; they remain unverified against the original. Search-result-only facts are marked "snippet".

Repo opening code reminder: the opening period is the 9 July 1948 lines adjusted, not a traced 15 May line; many
items share one hex with a Jewish settlement, so a hex code cannot represent both.

### Summary


Counts: SOURCED 6, PARTIAL 29, NEEDS-RESEARCH 3 (total 38).

### Shared region-level evidence (cited below as R1-R5)

- **R1** Jewish Virtual Library, "The War of Independence: The Negev and The South (May 1948)", page credits the
  Israel Defense Forces. https://www.jewishvirtuallibrary.org/the-negev-and-the-south-may-1948 . Event 15 May 1948:
  "In the early morning hours of May 15, 1948, Egyptian forces crossed the international border separating Israel and
  Egypt". Elsewhere the same page (via search summary, snippet) says they "advanced in two columns", one north along
  the coastal road. Region-level.
- **R2** United Press, "Arabs Push On", Kansas City Star, 16 May 1948 (reprint https://cojs.org/?p=4708 /
  https://cojs.org/arabs_push_on-_united_press-_kansas_city_star-_may_16-_1948/). Egyptian claims for 15 May: "has
  penetrated twenty miles into Palestine and captured the important city of Gaza"; Premier Nokrashy: "advance units had
  entered Gaza twelve hours after crossing the frontier". Egyptian government claim relayed by the press, not an
  independent observation. Place-specific for Gaza.
- **R3** Associated Press, "Smash in Israel", dateline Cairo 22 May, printed 23 May 1948 (reprint
  https://cojs.org/?p=7672). Egyptian communique: "The communique said that yesterday Egyptian forces 'carried on large
  scale operations' and advanced northward to Majdal"; "On the way the Egyptian troops occupied Hirbiya, Barhara, Beit
  Hogam and Arbra and also took over Jura on the seacoast." Event ~21 May 1948. Place-specific for Majdal, Hirbiya,
  "Barhara" (= Barbara, my reading of the transliteration), Jura. Shows the Egyptian army arrived there only ~21 May; it
  names no 15 May holder.
- **R4** Palmach Heritage, "Kfar-Darom resistance of attacks and siege",
  https://palmach.org.il/en/history/database/?itemId=8476 . "Kfar-Darom was established on the coastal road between
  Gaza and Han-Yunes, 800 m from the Dir-El-Balach train station"; "The 10th of May, 1948- The 'Muslim Brothers' first
  attack"; "On the 13th of May, 1948, all available 'Negev' Brigade fighters were busy occupying Brayer"; "On the 15th
  of May, the Egyptian transport Corp invaded Israel and attacked Nirim and Kfar-Darom".
- **R5** Palmach Heritage, "Battling the Egyptian Invaders - 15.5.-10.6.1948",
  https://www.palmach.org.il/en/history/database/?itemId=8474 . "The Egyptian army invaded on the 15th of May and
  advanced as far as Gaza."; "on the 19th of May, they attacked Yad Mordechai again and were repelled."; "The Yad
  Mordechai force was evacuated to Gvar Am at dawn on the 24th of May"; "the Egyptians continued on their advance to
  Majdal and Ashdod."
- Repo context (not re-fetched): `research_notes/Map borders/control-15-may-1948.md:78` summarises the CIA 18 May map
  as showing "Egyptian forces up the coast from Khan Yunis and north from Beersheba" (18 May, context only).

### Items

#### gdd03-0626 - Gaza (gaza)
- Holder on 15 May 1948: Egyptian army (entry claimed on 15 May itself).
- Status / confidence: PARTIAL / medium.
- Evidence: R2 (place-specific; Egyptian claim, press 16 May 1948: advance units "entered Gaza twelve hours after
  crossing the frontier"). R5 (place-specific but no day for Gaza itself: "invaded on the 15th of May and advanced as
  far as Gaza"). R4 shows Muslim Brotherhood/Egyptian volunteers active south of Gaza from 10 May (region-level).
- Repo: `-`. No contradiction (uncoloured, not a side).
- Open gaps: an independent (British/UN/Haganah intelligence) record of the Egyptian entry into Gaza dated 15 May;
  the repo's Phase 6 note records 15 vs 19 May as a conflict. Who policed Gaza on 14 May after British withdrawal.

#### gdd03-0627 - Yad Mordechai (yad_mordechai)
- Holder on 15 May 1948: Yishuv/Haganah (kibbutz members plus Palmach squads).
- Status / confidence: SOURCED / high.
- Evidence: R5, Palmach Heritage (place-specific): attacked "on the 19th of May ... and were repelled"; evacuated "at
  dawn on the 24th of May". en.wikipedia "Battle of Yad Mordechai" (secondary): defenders included "two squads of
  Palmachniks" (Morris 2008 p. 237, as cited); "Yad Mordechai finally fell to the Egyptians on May 24" (Morris 2008
  p. 239, as cited); Egyptians reached the area 16 May (uncited in article). Held from before 15 May until 24 May.
- Repo: `I`. Consistent.
- Open gaps: none material for 15 May.

#### gdd03-0628 - Khan Yunis (khan_yunis)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (the Egyptian coastal column passed along this road from 15 May).
- Status / confidence: PARTIAL / medium.
- Evidence: R1 (region-level, crossing 15 May). R4 places Kfar Darom on the road "between Gaza and Han-Yunes" and has
  Egyptian forces attacking there on 15 May (region-level). Repo CIA 18 May summary: "up the coast from Khan Yunis"
  (context). No fetched source names Khan Yunis's holder on 15 May.
- Repo: `-`. No contradiction.
- Open gaps: a dated record of the Egyptian column entering Khan Yunis (Egyptian army history, Mwawi's reports, British
  withdrawal from the Khan Yunis police/camp).

#### gdd03-0630 - Majdal (al_majdal)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (local Arab town; Egyptian army entered ~21 May).
- Status / confidence: PARTIAL / medium.
- Evidence: R3 (place-specific, Egyptian communique of 22 May: advanced "yesterday" to Majdal). R5 (after 24 May the
  Egyptians "continued on their advance to Majdal and Ashdod"). en.wikipedia "Al-Majdal, Askalan": "Majdal was
  occupied by the Egyptian army in the early stages of the 1948 Palestine war" (no date, uncited) and captured 4/5
  November 1948.
- Repo: `-`. No contradiction.
- Open gaps: who garrisoned Majdal on 15 May (National Committee militia? British police withdrawal date?). Note R3 and
  R5 differ slightly on the Majdal entry date (~21 May vs after 24 May).

#### gdd03-0632 - Hamama (hamama)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Hamama": "It was captured by Israel from the Egyptian Army in the first stage of Operation Yoav
  on October 28" (Morris 1987 p. 220, as quoted in Khalidi 1992 p. 99, as cited). Shows only that the Yishuv did not
  hold it before late October. No May date.
- Repo: `-`. No contradiction.
- Open gaps: Khalidi p. 99 text for any May event; date of Egyptian arrival.

#### gdd03-0634 - Isdud (isdud)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (Egyptian army only from 29 May).
- Status / confidence: PARTIAL / medium.
- Evidence: moreshet-map.org.il Nitzanim page (fetched): "ב-29 במאי הצבא המצרי העמיק את שליטתו באזור מישור החוף
  והשתלט על אסדוד" (on 29 May the Egyptian army seized Ashdod/Isdud). en.wikipedia "Isdud": "occupied by the Egyptian
  army on May 29, 1948" (uncited); depopulated 28 October 1948 (Morris 2004 p. xvii, as cited). Palmach 8480
  (Operation Pleshet, 2-3 June) has Egyptian infantry "based in Ishdod".
- Repo: `-`. No contradiction.
- Open gaps: the 15 May defenders of Isdud (local militia, ALA/Muslim Brotherhood volunteers?).

#### gdd03-0636 - Faluja (al_faluja)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Al-Faluja": "the men of the village blockaded the local Jewish communities" (Filiu p. 59, as
  cited, undated); Egyptian column stopped near Ashdod "retreated to and encamped at al-Faluja and Iraq al-Manshiyya"
  (date not given, after 29 May); Egyptian brigade withdrew 26 February 1949 (Khalidi, as cited).
- Repo: `-`. No contradiction.
- Open gaps: dated May record for Faluja (Khalidi entry; Givati May reports).

#### gdd03-0638 - Jabaliya (jabalia)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (immediately north of Gaza, where R2 places Egyptian advance
  units on 15 May).
- Status / confidence: PARTIAL / low.
- Evidence: R1, R2 (region-level). Snippet only: an Egyptian communique of 19 May (UP, NY Herald Tribune 20 May 1948,
  cojs.org) on capturing "a position five miles north-east of Gaza" (not fetched).
- Repo: `-`. No contradiction.
- Open gaps: place-specific record.

#### gdd03-0640 - Bani Suheila (bani_suheila)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (on the Khan Yunis stretch of the Egyptian route).
- Status / confidence: PARTIAL / low.
- Evidence: R1, R4 (region-level). No place-specific source found.
- Repo: `-`. No contradiction.
- Open gaps: place-specific record.

#### gdd03-0642 - Beit Daras (bayt_daras)
- Holder on 15 May 1948: Yishuv/Haganah (Givati 53rd Battalion, in the abandoned village from 11 May).
- Status / confidence: PARTIAL / medium (secondary sources only; one conflicting account).
- Evidence: hamichlol.org.il "הקרבות על בית דראס" (Hebrew wiki mirror; footnote: Givati fighters' memoirs): "הקרב השני
  התנהל ב-11 במאי 1948 על ידי חטיבת גבעתי"; "יחידות של גדוד 53 נשארו בכפר שננטש מתושביו" (units of Bn 53 remained in
  the village); it describes the Sudanese attack on the Beit Daras outpost on 8 July repelled by "הכוח המגן". en.wikipedia
  "Bayt Daras": "attacked and captured on May 11, 1948 by the Givati Brigade during Operation Barak" (Morris 2004 pp.
  256-257, as cited); depopulated 11 May (Morris p. xix, as cited). Conflict: the same article says Nasser's memoirs
  have Sudanese forces reoccupying the empty village in June 1948.
- Repo: `-` (later periods E then I). Not a CONTRADICTION by the research definition (uncoloured), but the opening hex
  (-7,24, shared with Isdud) does not show the Givati hold.
- Open gaps: Morris pp. 256-257 text on whether Givati garrisoned it after 11 May; the adhalom.blogspot "כיבוש בית-דראס
  11/5/1948" page linked from hamichlol; Nasser's June claim.

#### gdd03-0644 - Bureir (burayr)
- Holder on 15 May 1948: Yishuv/Haganah (Palmach Negev Brigade with Givati), occupied 12-13 May.
- Status / confidence: SOURCED / medium (occupation dated within 2 days; continued holding not stated in so many words).
- Evidence: R4, Palmach Heritage (place-specific): "On the 13th of May, 1948, all available 'Negev' Brigade fighters
  were busy occupying Brayer". en.wikipedia "Burayr": "In May 1948 the village was captured by the Palmach's Negev
  Brigade and Givati Brigade as part of Operation Barak" (Khalidi 1992 p. 92, as cited); depopulated 12 May (Morris
  2004 p. xix, as cited); remaining inhabitants fled to Gaza (Morris p. 258, as cited).
- Repo: `-` (period 2 `I`). Not a CONTRADICTION (uncoloured), but the opening hex does not show the hold.
- Open gaps: whether a garrison stayed (Morris p. 258).

#### gdd03-0646 - Deir el Balah (deir_al_balah)
- Holder on 15 May 1948: not-Yishuv; Egyptian forces and Muslim Brotherhood were besieging adjacent Kfar Darom.
- Status / confidence: PARTIAL / medium (region-level, adjacent).
- Evidence: R4: Kfar Darom "bordered with Dir-El-Balach, was under siege prior to the Egyptian invasion"; "On the
  morning of the 15th of May, the Egyptian forces attacking Kfar-Darom included: 2 infantry companies". R1: "On the
  night of May 14-15, the Egyptians attempted once again to conquer Kfar Darom".
- Repo: `-`. No contradiction.
- Open gaps: who held the town itself (local militia vs Egyptian/Brotherhood garrison).

#### gdd03-0648 - Masmiya el Kabira (al_masmiyya_al_kabira)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (Givati captured it only in July).
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Al-Masmiyya al-Kabira": "captured by the Israeli forces of the Givati Brigade during Operation
  An-Far" (paragraph cites Khalidi 1992 p. 125); depopulated 8 July 1948 (Morris 2004 p. xvii, as cited); NYT reported
  it "had been occupied on 11 July" (uncited).
- Repo: `I` (all periods I). CONTRADICTION: capture by Givati 8-11 July means the Yishuv did not hold the village on 15
  May; repo opening says I. Caveat: hex (-5,23) likely also holds Jewish settlements, so this may be a hex-scale issue.
- Open gaps: Khalidi p. 125 text; who held it in May.

#### gdd03-0650 - Jura (al_jura)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (Egyptian army "took over Jura" ~21 May).
- Status / confidence: PARTIAL / medium.
- Evidence: R3 (place-specific, ~21 May: "also took over Jura on the seacoast"). en.wikipedia "Al-Jura": "defended by
  the Egyptian Army", captured by Givati ~4-5 November 1948 (Morris p. xix, as cited).
- Repo: `-`. No contradiction.
- Open gaps: holder 15-20 May (villagers).

#### gdd03-0652 - Barbara (barbara_gaza)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (Egyptian army occupied it ~21 May).
- Status / confidence: PARTIAL / medium.
- Evidence: R3: "On the way the Egyptian troops occupied Hirbiya, Barhara, ..." (~21 May; "Barhara" read as Barbara).
  en.wikipedia "Barbara, Gaza": captured 5 November 1948 (uncited); depopulated 5 November 1948 (Morris 2004 p. xvii,
  as cited).
- Repo: `I` (hex -9,26, shared with Yad Mordechai and Sumsum). CONTRADICTION at village level: sources have it Egyptian-
  occupied from ~21 May and taken by Israel only in November, so not Yishuv-held on 15 May; repo opening says I.
- Open gaps: confirm "Barhara" = Barbara; holder 15-20 May.

#### gdd03-0654 - Hirbiya (hiribya)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (Egyptian army occupied it ~21 May).
- Status / confidence: PARTIAL / medium.
- Evidence: R3 ("the Egyptian troops occupied Hirbiya", ~21 May). en.wikipedia "Hiribya": "a large Egyptian Army force
  was quartered in the village" (October 1948, uncited); fell early November (Khalidi 1992 p. 102, as cited).
- Repo: `I` (hex -9,26, shared with Yad Mordechai). CONTRADICTION at village level, as for Barbara.
- Open gaps: holder 15-20 May.

#### gdd03-0656 - Abasan (abasan_al_kabira)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: R1, R4 (region-level; Egyptian route via Khan Yunis). No place-specific source.
- Repo: `-`. No contradiction.
- Open gaps: place-specific record.

#### gdd03-0658 - 'Iraq el Manshiya (iraq_al_manshiyya)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Iraq al-Manshiyya": no May information; Egyptian-controlled in the Faluja pocket later;
  depopulated February-June 1949 (Morris 2004 p. xix, as cited). en.wikipedia "Al-Faluja": the Egyptian column
  "encamped at al-Faluja and Iraq al-Manshiyya" after being stopped near Ashdod (after 29 May).
- Repo: `-`. No contradiction.
- Open gaps: dated May record.

#### gdd03-0660 - Rafah (rafah)
- Holder on 15 May 1948: Egyptian army crossing the frontier at dawn on 15 May (Rafah, the border town, not named).
- Status / confidence: PARTIAL / medium.
- Evidence: R1 (crossing the international border early 15 May); R2 ("twelve hours after crossing the frontier").
  Neither names Rafah.
- Repo: `-`. No contradiction.
- Open gaps: a source naming Rafah's occupation/British evacuation of the Rafah camp, 14-15 May.

#### gdd03-0662 - Beit Hanun (beit_hanoun)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: R2, R5 (Egyptian army reached Gaza from 15 May; Yad Mordechai, just north, attacked 19 May; region-level).
  Snippet only: Egyptian 19 May communique, "position five miles north-east of Gaza" (could be Beit Hanun; not fetched).
- Repo: `-`. No contradiction.
- Open gaps: place-specific record; fetch the NY Herald Tribune 20 May 1948 reprint on cojs.org.

#### gdd03-0664 - Beit Lahiya (beit_lahia)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: as Beit Hanun (region-level).
- Repo: `-`. No contradiction.
- Open gaps: place-specific record.

#### gdd03-0666 - Karatiya (karatiyya)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (captured 18 July).
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Karatiyya": "captured by the Israeli Army's 89th Mechanized Battalion" on 18 July 1948 (Dayan,
  Story of My Life pp. 112-121, as cited); "harassed by machine-gun fire and abandoned by its inhabitants" (Givati HQ
  document cited in Morris 2004 p. 437 n. 130, as cited); depopulated 17-18 July (Morris p. xix, as cited). Pre-war:
  Haganah demolished a house 9 December 1947.
- Repo: `I` (hex -7,25, shared with Negba and the Iraq Suwaydan fort). CONTRADICTION at village level: Arab village not
  taken until 18 July, repo opening I.
- Open gaps: May holder (villagers? Egyptians from late May?).

#### gdd03-0668 - Sumsum (simsim_gaza)
- Holder on 15 May 1948: mixed/contested - Negev Brigade expelled the villagers 12-13 May; whether they stayed is unclear.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Simsim, Gaza": villagers "were driven out by soldiers from the Negev Brigade on 12-13 May 1948"
  (uncited); troops blew up five houses and threatened the rest (Morris 2004 p. 258, as cited); late May a Negev unit
  "swept through the village, meeting resistance"; 9/10 June troops returned (Morris p. 258, as cited). en.wikipedia
  "Operation Barak" table: Simsim captured 15 May 1948, Negev Brigade. The late-May resistance suggests no continuous
  garrison.
- Repo: `I`. No contradiction (Yishuv raid 12-15 May at least).
- Open gaps: Morris p. 258 on whether a post remained.

#### gdd03-0670 - Nazla (nazla)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: as Jabaliya (region-level; adjacent to Gaza/Jabaliya).
- Repo: `-`. No contradiction.
- Open gaps: place-specific record.

#### gdd03-0672 - Ni'ilya (niilya)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Ni'ilya": "The village was defended by the Egyptian Army" (uncited, period unstated);
  depopulated 4-5 November 1948 (Morris 2004 p. xix, as cited). No May data.
- Repo: `-`. No contradiction.
- Open gaps: May record.

#### gdd03-0674 - Jiya (al_jiyya)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Al-Jiyya" (rev. 1377919372): "captured by Israel's Giv'ati Brigade on 4 November 1948 during
  Operation Yoav"; depopulated 4-5 November (Morris p. xix, as cited).
- Repo: `-`. No contradiction.
- Open gaps: May record.

#### gdd03-0676 - Juseir (jusayr)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Jusayr": depopulated "July 17-18, 1948" (Morris 2004 p. xix, village #300, as cited).
- Repo: `-`. No contradiction.
- Open gaps: May record.

#### gdd03-0678 - Yasur (yasur)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Yasur (village)": "Its inhabitants fled a military assault by the First Battalion of Israel's
  Givati Brigade on 9 June 1948" (cites Palestine Remembered); depopulated 11 June 1948 (Morris 2004 p. xix, as cited).
- Repo: `-` (later I). No contradiction.
- Open gaps: May record.

#### gdd03-0680 - Beit Tima (bayt_tima)
- Holder on 15 May 1948: not-Yishuv; holder unresolved (local militiamen recorded February and July).
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Bayt Tima": February 1948 attempt "driven back by a 'hail of bullets' from the local
  militiamen" (Filastin 11.02.1948 via Khalidi p. 89, as cited); "On 30/31 May the Negev Brigade reported that they had
  conquered Bayt Tima" (Morris p. 258 n. 784, as cited); "later reconquered by the Egyptian army"; occupied 18-19
  October by Givati.
- Repo: `-` (period 2 I). No contradiction.
- Open gaps: holder 14-15 May.

#### gdd03-0682 - Sawafir el Gharbiya (al_sawafir_al_gharbiyya)
- Holder on 15 May 1948: mixed/contested (Givati raids with withdrawals; villagers returning).
- Status / confidence: PARTIAL / low.
- Evidence: en.wikipedia "Al-Sawafir al-Gharbiyya": "On May 18, the Givati Brigade for a second time conquered"
  al-Sawafir al-Sharqiyya "together with Al-Sawafir al-Gharbiyya" (Morris 2004 pp. 257, 306, as cited); "after they
  withdrew, the Palestinian villagers returned"; late June both villages "full of Arabs". The first conquest is not
  dated in the fetched text (the Operation Barak table dates al-Sawafir al-Shamaliyya, a different village, to 12 May).
- Repo: `-` (later I). No contradiction.
- Open gaps: date of the first Givati conquest and whether troops were present on 15 May (Morris p. 257).

#### gdd03-0684 - Julis (julis_2)
- Holder on 15 May 1948: not-Yishuv; holder unresolved.
- Status / confidence: PARTIAL / medium.
- Evidence: en.wikipedia "Julis, Gaza": Givati captured "a military barracks in Julis during Operation Barak, but failed
  to gain control of the village itself" (dated May 27-28 in the article; paragraph cites Khalidi 1992 p. 115); Haganah
  history dates the capture to 11 June; depopulated 11 June (Morris p. xix, as cited).
- Repo: `-` (later I). No contradiction.
- Open gaps: who held the Julis barracks/village 14-15 May (British evacuation date of the camp).

#### gdd03-0686 - Negba (negba)
- Holder on 15 May 1948: Yishuv/Haganah (kibbutz defenders).
- Status / confidence: SOURCED / medium.
- Evidence: R1 (IDF via JVL): "In the afternoon hours of May 21, 1948 kibbutz Negbah was attacked by an Egyptian armored
  column and strafed from the air." R3: Egyptian bombing of "fortifications at Negba" (~21-22 May). en.wikipedia "Negba":
  the Egyptians "captured the police station" and seized "Arab villages near the kibbutz"; kibbutz never reported as
  fallen. Held before and after 15 May.
- Repo: `I`. Consistent (but the same hex holds the Arab-held fort and Karatiyya).
- Open gaps: an explicit 14-15 May Negba record (Givati war diary).

#### gdd03-0687 - Nir Am (nir_am)
- Holder on 15 May 1948: Yishuv/Haganah (Palmach Negev Brigade and 2nd Battalion HQ).
- Status / confidence: SOURCED / medium.
- Evidence: Palmach Heritage "The Palmach and the Negev Settlements" (https://www.palmach.org.il/en/history/database/?itemId=8465):
  "The headquarters of the Negev Brigade and the 2nd Battalion were at Nir-Am that had become the 'Capital of the
  Negev'." Palmach "Hativat Hanegev (12)" (itemId=6190): "The day after the invasion of the Egyptian army and the
  bombing of Kibbutz Nir - Am..." HQ moved to Dorot (anchors the HQ at Nir Am on 15 May).
- Repo: `I`. Consistent.
- Open gaps: none material.

#### gdd03-0688 - Iraq Suwaydan police fort (iraq_suwaydan_fort)
- Holder on 15 May 1948: Arab side - local armed villagers (Haganah intelligence, 13 May) or Egyptian army (Tal); not
  Yishuv: Givati's attack on 14 May failed.
- Status / confidence: PARTIAL / medium. Research correction: previously SOURCED (non-Yishuv), but its evidence is Wikipedia-only (Operation Shmone; Iraq Suwaydan, citing Tal p. 177), which the research criteria cap at PARTIAL. Holder type disputed.
- Evidence: en.wikipedia "Operation Shmone": British handed the fort "to Muslim Brotherhood forces"; 12 May telegram:
  "the Iraq Suwaydan police fort was captured by Egyptian forces, in cooperation with a number of armed villagers";
  next day Haganah intelligence reported the Egyptians had left and local villagers held it; Givati attacks on 14 May,
  18-19 May and 21-22 May all failed. en.wikipedia "Iraq Suwaydan": "On 12 May 1948, the British authorities withdrew,
  handing over the police fort to the Egyptian army, which then garrisoned it" (Tal 2004 p. 177, as cited); fell 9
  November 1948 (Khalidi p. 109, as cited). palquest.org (403, snippet only): British gave the local population control
  of the station on the eve of their 15 May evacuation.
- Repo: `I` (hex -7,25, shared with Negba). CONTRADICTION: the fort was Arab-held on 15 May and until 9 November; repo
  opening I.
- Open gaps: the Dubno telegram and Haganah intelligence report (IDFA) behind the Operation Shmone article; Tal p. 177.

#### gdd03-0689 - Jisr Isdud / Ad Halom bridge (ad_halom_bridge)
- Holder on 15 May 1948: unresolved.
- Status / confidence: NEEDS-RESEARCH / low.
- Evidence: en.wikipedia "Jisr Isdud": blown up "again in 1948 by the Golani Brigade [sic] in early stages of the 1948
  Arab-Israeli War" (Dvir, Haaretz 10 March 2010, as cited; "to stop the Egyptian army's advance", no day). Nothing
  dated for 15 May. Adjacent Isdud: Egyptian from 29 May (see Isdud).
- Repo: `-`. No contradiction.
- Open gaps: date of the 1948 demolition (Givati, late May?) and who held the bridge before.

#### gdd03-0690 - Mefalsim (mefalsim)
- Holder on 15 May 1948: unresolved. The kibbutz did not exist (going onto the land 12 June 1949, repo `content/sites.yaml`);
  the question is the footprint. The repo hex (-10,27) is the present kibbutz; the 1949 site (a kurkar hill facing Gaza)
  has no coordinates.
- Status / confidence: NEEDS-RESEARCH / low.
- Evidence: none dated for this footprint. Nearest dated anchors: Nir Am (~2 km N-NE) was Yishuv-held (Palmach HQ);
  Beit Hanun to the west is not-Yishuv, holder unresolved. A search for the land's 1948 owner/village found nothing.
- Repo: `-`. No contradiction.
- Open gaps: whose village lands the site was (Beit Hanun? Dimra?), and any May 1948 patrol/outpost record between Nir
  Am and Beit Hanun.

#### gdd03-0692 - Mefalsim fields (mefalsim_field)
- Holder on 15 May 1948: unresolved (a design placement on the Mefalsim hex, no historical footprint).
- Status / confidence: NEEDS-RESEARCH / low.
- Evidence: as Mefalsim.
- Repo: `-`. No contradiction.
- Open gaps: as Mefalsim.

#### gdd03-0694 - Nitzanim (nitzanim)
- Holder on 15 May 1948: Yishuv/Haganah (kibbutz defenders plus Givati 53rd Battalion platoons).
- Status / confidence: SOURCED / medium.
- Evidence: moreshet-map.org.il "ניצנים" (fetched): after the Egyptians seized Isdud on 29 May "וכשבוע לאחר מכן הצליח
  לכבוש את הקיבוץ בקרב קשה"; defenders "נפלו 33 ממגני הקיבוץ ו-105 נפלו בשבי"; original site "כשני קילומטר מצפון מערב
  לקיבוץ ניצנים" of today. en.wikipedia "Battle of Nitzanim": "Nitzanim was isolated after December 31, 1947" (Ayalon
  1963 pp. 99-105, as cited); "At 16:00 on June 7, 105 exhausted Israeli defenders" surrendered (Yitzhaki 1988, as
  cited); "located 700 meters west of the coastal road" (Hashavia 2005 p. 168, as cited). Held continuously to 7 June.
- Repo: the group file has no hex or opening code (repo_hex null). Note: the 1948 site is ~2 km NW of today's kibbutz,
  so the hex should come from the 1948 site (infobox 31°43′3″N 34°38′8″E per en.wikipedia).
- Open gaps: an explicit 15 May record (Givati diary); confirm the 1948 coordinates.

### CONTRADICTIONS

1. gdd03-0648 Masmiya el Kabira: source - Givati captured it in Operation An-Far, depopulated 8 July / occupied 11 July
   1948 (Morris p. xvii; Khalidi p. 125, via en.wikipedia), so not Yishuv-held on 15 May; repo opening `I`.
2. gdd03-0652 Barbara: source - Egyptian troops "occupied ... Barhara" ~21 May (AP 23 May 1948 communique), captured by
   Israel 5 November (Morris p. xvii via en.wikipedia); repo opening `I` (hex shared with Yad Mordechai).
3. gdd03-0654 Hirbiya: source - Egyptian troops "occupied Hirbiya" ~21 May (same communique), fell to Israel early
   November (Khalidi p. 102 via en.wikipedia); repo opening `I` (hex shared with Yad Mordechai).
4. gdd03-0666 Karatiya: source - captured 18 July 1948 by the 89th Battalion (Dayan; Morris p. 437 via en.wikipedia);
   repo opening `I` (hex shared with Negba).
5. gdd03-0688 Iraq Suwaydan police fort: source - handed by the British on 12 May to Egyptian forces/Muslim Brotherhood,
   held by armed villagers 13 May, Givati attack 14 May failed, fell 9 November 1948 (Operation Shmone / Iraq Suwaydan
   en.wikipedia; Tal p. 177; Khalidi p. 109); repo opening `I` (hex shared with Negba).

Not contradictions but worth a look: Beit Daras (Givati-held from 11 May) and Bureir (occupied 12-13 May) sit in
uncoloured (`-`) opening hexes; Nitzanim has no repo hex/code in the group file.

## Appendix: Kiryat Shmuel reception camp, three findings

The broader camp research is `research_notes/Map/camps-1948-51.md` (#1719) by a contributor, Beri; this appendix does not restate or extend it.

1. **Operation dates.** No source read gives a day in May 1948 on which Kiryat Shmuel camp held immigrants. The dated evidence brackets that month: set up towards the beginning of 1947 and operated until the state's founding (Yassky memorandum, 18 July 1947; Davar 7 Feb 1947); January 1948 arrivals from Nahariya (Davar, 4 Jan 1948); after the state's founding, family housing blocks; September 1949: 268 families (Davar 9 Sep 1949); December 1949: 950 people (Davar 6 Dec 1949). Source: [Hebrew Wikipedia, "מחנה העולים בקריית שמואל"](https://he.wikipedia.org/wiki/מחנה_העולים_בקריית_שמואל), rev. 39577890 (7 Sep 2024), citing those primary records second-hand; the primary records still need a person's check.
2. **Services.** Hadassah provided medical services at Kiryat Shmuel camp: [Hebrew Wikipedia](https://he.wikipedia.org/wiki/מחנה_העולים_בקריית_שמואל). In 1948, arrivals at Haifa port underwent DDT disinfection, a light meal, ID card issue, vaccinations and customs, then referral and transport to a camp; this chain moved into the reception and sorting camp in early 1949 (cites Jewish Frontier 1949 p. 40): [Hebrew Wikipedia, "שער העלייה"](https://he.wikipedia.org/wiki/שער_העלייה). "From 1948 to 1949 new immigrants were processed directly at the Haifa port" (Lissak): [Seidelman, PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC3484001/). The supplied research infers registration, medical checks, ID and customs at Haifa port, distinct from camp medical services; this inference needs a person's historical check.
3. **Housing.** Family housing blocks are described only "After the founding of the state": [Hebrew Wikipedia](https://he.wikipedia.org/wiki/מחנה_העולים_בקריית_שמואל), rev. 39577890 (7 Sep 2024). No source read establishes what Kiryat Shmuel had on the opening date specifically, or a count of beds, kitchens or classrooms on 15 May 1948. These are gaps in the AI research, not an opening-date facility inventory; a person's historical check remains necessary.
