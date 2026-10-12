# Promo concept art: sources

Images 01-08 are AI-generated concept mockups (ElevenLabs Image API: GPT Image 2.5, Nano Banana Pro),
not final game art. Steam requires disclosure of AI-generated marketing assets.
09-m2-flythrough.mp4 and 10-m2-topdown.png are not AI-generated: they are in-engine Unity captures of the M2 map
(`MapCapture.Flythrough` and `MapCapture.TopDown`; see docs/m2-geography-check.md).

## Likeness references (Wikimedia Commons; licence checked by API on 2026-10-09)
Public domain: David Ben-Gurion (D597-087), Eliezer Kaplan, Moshe Sharett (Portrait), Haim-Moshe Shapira
(Moshe_Shapira.jpg), Yitzhak Gruenbaum (1948), Pinchas Rosen, Aharon Zisling, Mordechai Bentov,
Bechor-Shalom Sheetrit (Shitreet), Yehuda Leib Maimon, Peretz Bernstein, David Remez (cropped),
Yitzhak Meir Levin.
CC0: "Moshe Dayan met mgr. A.C. Ramselaar (cropped).jpg" (1962).

## In-game portraits from US-safe sources (licence checked on 2026-10-11)
- David Ben-Gurion: crop of Abbie Rowe (National Park Service), 8 May 1951, NARA 200318, {{PD-USGov}}:
  https://commons.wikimedia.org/wiki/File:Photograph_of_President_Truman_in_the_Oval_Office%2C_evidently_receiving_a_Menorah_as_a_gift_from_the_Prime_Minister_of..._-_NARA_-_200318.jpg
  (Truman Library record 73-3680 names Ben-Gurion and marks it public domain). The D597-087 photo above stays as a
  reference only.
- Golda Meir: crop of Willem van de Poll / Nationaal Archief 255-4314, 1964, {{Cc-zero}}:
  https://commons.wikimedia.org/wiki/File:Golda_Meir%2C_minister_van_buitenlandse_zaken_van_Israel%2C_Bestanddeelnr_255-4314.jpg

- Chaim Weizmann: crop of Bain News Service / Library of Congress ggbain.32095, ca. 1915-1920,
  {{PD-Bain}} / {{PD-old-70-expired}} (including explicit US-expired status):
  https://commons.wikimedia.org/wiki/File:C._(Chaim)_Weizmann_LCCN2014712248.jpg
  https://www.loc.gov/pictures/item/2014712248/
  Earlier likeness, not a photograph of his presidential term.
- Yitzhak Ben-Zvi: crop of Harry Pot / Anefo / Nationaal Archief 909-7131, 15 July 1958, {{Cc-zero}}:
  https://commons.wikimedia.org/wiki/File:President_Isaac_Ben_Zwi_,_president_van_Israel_en_echtgenote_ontvangen_door_het_,_Bestanddeelnr_909-7131.jpg
  https://www.nationaalarchief.nl/onderzoeken/fotocollectie/a98090b0-d0b4-102d-bcf8-003048976d84
  Later than the Founding slice; profile crop, not a 1952-1956 photograph.
- Yaakov Dori: crop of Willem van de Poll / Nationaal Archief 255-0981, {{Cc-zero}}:
  https://commons.wikimedia.org/wiki/File:Stafchef_Dori_houdt_toespraak_tijdens_de_militaire_parade_bij_gelegenheid_van_de,_Bestanddeelnr_255-0981.jpg
  https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed504c4-d0b4-102d-bcf8-003048976d84
  The caption gives 15 May 1949; the archive date field says 1 January 1948. Exact capture date remains uncertain.
- Zalman Shazar: crop of Yoichi Okamoto / LBJ Library, 2 August 1966, {{PD-USGov}}:
  https://commons.wikimedia.org/wiki/File:President_Lyndon_B._Johnson_with_President_Zalman_Shazar_of_Israel.jpg
  (official White House photograph; later than the game period; human likeness and era review required).

- Moshe Sharett: crop of Willem van de Poll / Nationaal Archief 255-1438, {{Cc-zero}}:
  https://commons.wikimedia.org/wiki/File:Moshe_Sharett,_de_eerste_minister_van_buitenlandse_zaken_van_de_staat_Israel,_tu,_Bestanddeelnr_255-1438.jpg
  https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed665c6-d0b4-102d-bcf8-003048976d84
  Collection Israel 1948-1949; archive date field 1948-01-01 conflicts with the caption identifying the
  new state's foreign minister. No exact day is inferred. Old GPO likeness reference above remains unchanged.

## Facts shown (verified 2026-10-09)
- Provisional Government (May 1948): 13 ministers and portfolios; Provisional State Council 37 members,
  coalition 30 / opposition 7, Mapai 10, president Chaim Weizmann (Wikipedia: Provisional government of
  Israel; Provisional State Council).
- Defense, Oct 1955: PM Moshe Sharett (7 Dec 1953 – 3 Nov 1955); Ben-Gurion Minister of Defense after
  Lavon's resignation was accepted 18 Feb 1955; Dayan Chief of Staff from 7 Dec 1953; Egypt–Czechoslovakia
  arms deal announced 27 Sep 1955.
- Geography: promo/ref/geo-reference.png, drawn from gazetteer coordinates by promo/refmap.py.

Readiness/strength bars, competence/loyalty scores and budget bars are illustrative game values.

## Map data (09-m2-flythrough.mp4, 10-m2-topdown.png)
Wording copied verbatim from tools/README.md.

Terrain (Copernicus DEM licence, modified data):
produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

Biomes (precipitation inputs):
Precipitation: CHELSA V2.1 bio12 (1981-2010), CC0 1.0. Karger, D.N., Conrad, O., Böhner, J., Kawohl, T., Kreft, H.,
Soria-Auza, R.W., Zimmermann, N.E., Linder, H.P. & Kessler, M. (2021) Climatologies at high resolution for the earth's land
surface areas. EnviDat. https://doi.org/10.16904/envidat.228.v2.1 ; Karger et al. (2017) Scientific Data 4, 170122.

Rain-gauge data: Israel Meteorological Service (השירות המטאורולוגי), meteorological data archive, data.gov.il dataset 481
(https://data.gov.il/dataset/481), licence "Other (Open)" as stated on the dataset.

1948 settlements (tools/README.md has no settlement wording; this is the source note of
content/map/settlements_1948.yaml): coordinates from the Wikipedia API (prop=coordinates); founding years from the
cited article's text. Each settlement's source URLs and check dates are listed in that file.
