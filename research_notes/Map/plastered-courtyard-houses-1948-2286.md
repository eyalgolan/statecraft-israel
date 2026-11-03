# Plastered courtyard houses in 1948 Arab villages and towns, to check Tal's courtyard house (05) (#2286, research part)

Status: **draft, needs a person to check**. This is research only. An AI agent opened every source marked "agent read" below on
3 November 2026 and recorded the day it opened each. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to
check every claim against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not
choose which house stands where: any change to the model or its pool follows in a separate PR, and the owner decides it.

Place names follow the `name` in `content/map/localities_1948.yaml` where the place has a row there (Halhul, Silwan, Beit Jala,
Ramallah, Hebron, Nablus, Bethlehem, Kafr Kanna, Majdal, Tantura, Qisariya, Yafa); Safed, Acre, Jaffa, Nazareth and Cana are
written as the sources write them.

## Question and result

#2286 asks: before 1949, were the houses of Arab villages and towns plastered or bare stone, flat, domed or vaulted on top, with
courtyards, how many storeys and what colour, and how did that differ by region? Then: does Tal Heuberger's house 05
(`unity/Assets/Art/Settlements/Prototype/Models/generated-1948-05-plastered-courtyard-house-original/`) match? The pool it
fills is `TownModels.Stone` (`unity/Assets/Scripts/Map/Kits/TownModels.cs:68`; since #2259 merged, 05 is the only house in that pool).

**What closed, from two pre-1949 primary texts and Library of Congress photographs**

- **Walls.** Tawfiq Canaan, who wrote while the Mandate stood (1932 and 1933), says houses were of two kinds: stone in the
  mountains, clay brick in the Mediterranean plain and the Jordan depression. He says plastering "is regarded as a luxury which can
  be introduced any time later", that "Not every peasant whitewashes his house", and that indigo in the whitewash often gave "a decided
  bluish tint". Canaan's plaster passage (pp. 25-26) names "the ceiling (sagf) and the walls (hrtan)" without saying inside or
  outside, so the text does not settle whether a full outer plaster was usual; the one outer surface he names is "the outer
  surface of the jambs and lintel" (p. 27). Reading "no full outer plaster" from this would be an inference, and this note does
  not draw it. A plastered look is shown for the town of Safed, about 30 years before 1948, in the photograph below.
- **Roof.** Canaan: roofs are "more or less flat, with a low dome-like elevation in the centre" (the top of the vault below); clay
  houses have wooden roofs, sealed with a layer of huwar (clay) and straw that "must be renewed each year". A 1989 secondary book
  ([S]) says hill-village houses had a domed roof. The two describe the same vaulted room from inside and outside; how the game
  should draw it is the owner's call ([C], see the table).
- **Courtyards.** Both say a house opened on a shared courtyard; a peasant house was one room, richer ones several, some of two
  storeys.
- **Colour.** Black and white photographs cannot give colour. Text gives: whitewash (bluish with indigo) round the door, door panels
  painted blue or green ([S], 1989). Canaan 1933 p. 25 says wooden shutters were installed ("daffet es-subbak"), so shutters
  are attested; their colour was not found. Model 05's shutters are brown.

**What stays [GAP]:** colour of any wall in 1948 (no colour source opened); houses of Acre, Jaffa and Tantura in 1948 (no
description of house walls found); the Arab quarter of Safed; Galilee hill villages other than the Huleh reed-mat villages; any
1940s survey that counts plastered against bare houses. See "Open gaps".

## Answer table

Tags: [V] verified by an agent reading the record itself (each row says "agent read" and the day); [S] secondary or a page that
is not the record; [C] conflicting, both values kept; [GAP] still open, with what was tried. A photograph row records
what the agent saw in the image file, which is the agent's reading, not the caption.

| Claim | Source and page or URL | Printed text | Tag | Day read |
|---|---|---|---|---|
| Canaan names two house materials by region: stone in the mountains, clay brick in the plain and the Jordan depression | T. Canaan, "The Palestinian Arab House: Its Architecture and Folklore", *Journal of the Palestine Oriental Society* XII (1932), p. 230 (archive.org `journal-of-the-palestine-oriental-society_1932_12`, OCR text) | "most Palestine houses are of two types: those of stone and those of clay. The first occur principally in the mountainous parts of the country where stone is abundant. In the Mediterranean plain and the Jordan depression, where stone is relatively scarce and difficult to transport, clay bricks were and are still used." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Clay houses are weaker and need yearly repair; they "have always wooden roofs" | same page | "Clay houses are cheaper and easier to build, but they are very frail ... Hence they need to be repaired every year." and "The clay houses have always wooden roofs." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Stone houses are of two kinds: thick walls with a vaulted stone roof, thinner walls with a wooden ceiling | same page | "Houses built of stone are of two kinds: those with thick walls and a vaulted stone roof, and those with thinner walls and a wooden ceiling." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Sandstone is used only in towns and villages of the Mediterranean plain | Canaan 1932, p. 234 | "Sandstone (hadjar ramli) is found and used only in the towns and villages of the Mediterranean plain." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Villages sit on hills or mounds, and houses rise "terrace above terrace"; Nazareth and Beit Jala are named | Canaan 1932, p. 228 and note 5 | "in the plain villages are built on small mounds or hillst and on this account houses in many villages rise one above the other, terrace above terrace" (the OCR prints "hillst"; the footnote names "Bet Djala, Nazareth, Hebron, Ain Karim") | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| A peasant house was one room, with one door onto a courtyard; further rooms were added beside it, some of two storeys, in Jerusalem, Hebron, Nablus, Jenin, Gaza and elsewhere | Canaan 1932, p. 229 | "A peasant's house used to consist of one single room. Richer folk had more. Every such abode had but a single door opening into the courtyard." and "some of them being of two stories" and "may still be seen in Jerusalem, Hebron, Nablus, Der Ghassaneh, Djenin, Gaza and elsewhere" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Around Lake Huleh some villages were of reed-mat huts, used by the poor; Canaan also says huts are temporary | Canaan 1932, p. 228 | "These huts used by the poor Bedouin and semi-Bedouin are made of reed-mats held in place by a wooden skeleton" and "This type is used only by the poorer classes. In the neighbourhood of lake Hüleh one meets with villages whose houses are all of this type." and "Huts are only temporary habitations." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The older mountain houses, in towns and villages, were vaulted; flat reinforced-concrete roofs were an innovation | T. Canaan, same article, continued, *JPOS* XIII (1933), p. 12 (archive.org `journal-of-the-palestine-oriental-society_1933_13`) | "The older type of house in the mountainous regions, both in towns and villages, used to be vaulted. Flat roofs made of reinforced concrete are an innovation" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Roofs are flat to the eye with a low dome in the middle | Canaan 1933, p. 25 | "The roofs (sath, pl. stah) are more or less flat, with a low dome-like elevation in the centre, sloping to the periphery so that the rain-water runs off freely. This elevation is the protruding apex of the vault." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Domes proper (the hemispherical dome on a cross-vault) are rare on private houses | Canaan 1933, p. 20 | "A shallow dome is called qubbeh (i)msatlahah. ..." then "Only few private houses of the better-class town-dwellers show this form of vaulting." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The roof is sealed with clay and straw renewed yearly; richer people plaster it with sand and lime | Canaan 1933, p. 24 | "The simplest device adopted in most villages is a layer of huwar and straw ... Such a protecting layer must be renewed each year." and "Richer people choose a more efficient way. The roof is plastered with hasmeh ... and lime." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Peasants moved in before plastering, which was a luxury | Canaan 1933, p. 25 | "Most peasants occupy their houses before they are finished, since plastering is regarded as a luxury which can be introduced any time later when the proprietor has saved money." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Plaster of "the ceiling and the walls" was lime with flax in three coats, polished for days; the text does not say whether this is the inside or the outside finish | Canaan 1933, pp. 25-26 | "The plastering material (gsarah) of the ceiling (sagf) and the walls (hrtan) was made of lime mixed with flax-threads" (the OCR prints "hrtan"); "The plaster consists as a rule of three coats: marseh (rendering coat), sihhiyeh (floating coat) and nameh (setting or fine coat)." and "It needed to be polished for several days" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Not every peasant whitewashed; those who did added indigo, giving a bluish tint, and most whitewashed the door jambs and lintel | Canaan 1933, p. 27 | "Not every peasant whitewashes his house" (where on the house is not said) and "Many peasants use so much nileh that the whitewashing has a decided bluish tint. Most peasants and some townsfolk also whitewash the outer surface of the jambs and lintel, especially around the door." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Joints were pointed with lime and ashes; the pointing "protruded slightly beyond the surface of stones" in the old style | Canaan 1933, p. 27 | "Formerly lime with qusrmal or lime with humrä were used. Such a pointing is known as kuhleh 'arabiyeh. It covered not only the joints but also the edges of the stone" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Highland houses: grey limestone, mostly single-storey, thick walls, a domed roof, one low arched door | S. Amiry and V. Tamari, *The Palestinian Village Home*, British Museum Publications 1989 (RIWAQ reprint 2003, PDF at `https://palestine.araburbanism.com/img/The%20Palestinian%20Village%20Home.pdf`), p. 25 | "Most highland village houses were simple square structures built from the limestone rock abundant in the surrounding hills" and "Most houses were single-storey, with only one low, arched entrance." and "growing freely on the domed roof" | [S] | 2026-11-03 |
| Same book: Mandate-period houses had larger windows, often in pairs; part of the front was whitewashed, doors painted blue or green; wealthier houses had two or three storeys | Amiry and Tamari 1989, pp. 25-26 | "later houses dating from the Mandate period have relatively large windows, often in pairs" and "It was common to whitewash part of the front façade, particularly the arched doorway" and "the wooden panels of the door painted blue or green" and "These wealthier houses often had two or three storeys" | [S] | 2026-11-03 |
| Same book: houses grouped round a family courtyard (hosh), often entered by a large arched doorway | Amiry and Tamari 1989, p. 17 | "Members of each hamuleh lived in a group of adjacent houses connected by one or more courtyards (hosh)." and "sometimes defined by a large, arched doorway leading from the alley" | [S] | 2026-11-03 |
| Walls bare, roof domed or flat: **roof form, [C]** between the 1933 primary text (flat to the eye, low dome at the centre) and the 1989 secondary book (domed roof) | Canaan 1933, p. 25; Amiry and Tamari 1989, p. 25 | both quoted above | [C] both kept; neither is picked here | 2026-11-03 |
| Jaffa in 1906: "The houses are built of tuffstone"; streets narrow and dusty | K. Baedeker, *Palestine and Syria*, 5th ed., Leipzig 1906 (archive.org `palestinesyriawi1906baed`, OCR text), p. 8 | "The old town lies on the sea-coast, on the brow of a rocky hill 115 ft. in height. The houses are built of tuffstone. The streets are generally very narrow and dusty" (the OCR prints "Thé") | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Photograph, Halhul, 1936 (Matson Collection, Library of Congress, LC-DIG-matpc-18149, title "Palestine disturbances 1936. Native houses at Halhul village blown up by the government"): a collapsed house shows rubble stone walls and a flat earth-and-timber roof with beams; a distant wall behind shows no readable surface | image file `https://tile.loc.gov/storage-services/service/pnp/matpc/18100/18149r.jpg` (640 px, black and white); title and date from the search record for item 2019708919 | caption as quoted; the agent's reading of the image is the claim | [V, agent read 2026-11-03; a person's check pending] for what the image shows | 2026-11-03 |
| Photograph, Safed, c.1900-1920 (LC-DIG-matpc-01113, title "Northern views. Safed looking east", date "[approximately 1900 to 1920]"): a stereo pair; white plastered or whitewashed walls, flat roofs, several levels stepped up the slope, a dry-stone terrace wall in front | `https://tile.loc.gov/storage-services/service/pnp/matpc/01100/01113r.jpg` | the agent's reading of the image | [V, agent read 2026-11-03; a person's check pending] for what the image shows | 2026-11-03 |
| Photograph, Abu Ghosh village, between 1934 and 1939 (LC-DIG-matpc-16880, title "Abu Ghosh village"): light cubic houses with flat tops and rectangular windows, terraced on a hillside; the surface finish (plaster or dressed stone) cannot be told at 640 px | `https://tile.loc.gov/storage-services/service/pnp/matpc/16800/16880r.jpg` | the agent's reading of the image | [V, agent read 2026-11-03; a person's check pending] for what the image shows | 2026-11-03 |
| Air photographs, 1932 (series "Air views of Palestine", LC-DIG-matpc-15868 Cana of Galilee, -15869 and -15870 Nazareth, -15884 Jaffa from the land side): Cana is a low cluster of flat-topped houses; Nazareth is dense and mostly flat-roofed with some pitched roofs on newer buildings; Jaffa is a dense waterfront town with ships off it, and its land side shows newer blocks among orchards, roof form not readable | `https://tile.loc.gov/storage-services/service/pnp/matpc/15800/15868r.jpg` (also 15869r, 15870r, 15884r) | the agent's reading of the images | [V, agent read 2026-11-03; a person's check pending] for what the images show | 2026-11-03 |
| Air photographs of Beit Jala (LC-DIG-matpc-22173, [1931] in the LOC record) and Safad (-22278, date not read): dense clusters on slopes and ridges; roof form not readable at this size | `https://tile.loc.gov/storage-services/service/pnp/matpc/22100/22173r.jpg` (and 22200/22278r.jpg) | the agent's reading of the images | [V, agent read 2026-11-03; a person's check pending] for what the images show | 2026-11-03 |
| Model 05's role: "Generic existing Village/ArabTown courtyard variation"; in `TownModels.Stone` alone | `docs/generated-buildings-props-mapping.json:41-48`; `unity/Assets/Scripts/Map/Kits/TownModels.cs:68`; `SettlementKits.Models.cs:199` | "Generic existing Village/ArabTown courtyard variation (including existing Jerusalem Village-style plots)" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Model 05's roof: "one continuous L-shaped roof deck" (flat) | `.../generated-1948-05-plastered-courtyard-house-original/provenance.json`, key `source_caveats` | "one continuous L-shaped roof deck replaces the provider original without its roof deck." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Model 05's surface, from its 4096 px colour map: cream, cracked plaster walls; brown wooden shutters and doors; an arched doorway; small square windows | `.../generated-1948-05-plastered-courtyard-house-original_basecolor.jpg`, viewed at 1000 px | the agent's reading of the image; the 3D model itself was not opened in Unity | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |

## Model 05 against the sources, by region

Result key: **matches**, **differs** (with what), **cannot tell**. All of it rests on the rows above.

| Region | Wall finish | Roof | Courtyard, storeys | Colour | Result for 05 |
|---|---|---|---|---|---|
| Mountainous or highland villages, as the sources say (Canaan; Amiry) | Limestone with part of the front, above all the door, whitewashed is the case described (Amiry 1989 pp. 25-26 [S]); plaster a later luxury and "Not every peasant whitewashes" (Canaan 1933 pp. 25, 27); full plaster on peasant houses not described; inside or outside of the plaster passage not stated | Vault inside, flat with a low dome outside (Canaan); "domed" ([S]) | Courtyard yes (both); mostly one storey (Amiry p. 25 [S]), two in larger houses (Canaan 1932 p. 229) | whitewash bluish with indigo; doors blue or green ([S]) | Full cream plaster: **cannot tell** (the text does not settle the outside finish), but a fully plastered house is not what Amiry describes; brown shutters: wooden shutters are attested, colour not found; courtyard house by name, layout not read, so **cannot tell**; roof **cannot tell** ([C]) |
| Coastal plain villages (Tantura, Majdal, Qisariya) | Clay brick, for "the Mediterranean plain" generally, not these villages (Canaan 1932 p. 230); plaster not stated | Wooden, clay and straw, yearly renewal (Canaan) | Courtyard not described for the plain | not found | **Cannot tell**: a plastered clay house could look like 05, but no source says it did |
| Galilee villages (Kafr Kanna, Nazareth area) | Not found for walls; Cana and Nazareth air photographs show flat tops only | flat tops in the 1932 air photographs | not found | not found | **Cannot tell** |
| Huleh villages | Reed-mat huts of the poor, called temporary by Canaan (1932 p. 228) | reed mats | none described | not found | **Differs** for those huts only; a 1932 text, so what stood there in 1948 **cannot tell** |
| Safed | white plaster or whitewash, stepped levels, flat roofs in the c.1900-1920 photograph (the town's look about 30 years before 1948; the title names no quarter, so the Arab quarter is not identified) | flat | stepped levels | black and white only | **Matches** the plastered look and flat roof for the town as it was c.1900-1920; storeys, courtyard and the quarter **cannot tell** |
| Jaffa and Acre | Jaffa "tuffstone" in 1906 (Baedeker p. 8); Acre not found; Jaffa's land side in 1932 shows newer blocks among orchards | roof form not readable in the air photograph | not found | not found | **Cannot tell**; the courtyard house is not shown to fit |
| Jerusalem village plots (Silwan, Lifta) | the Siloam photograph found shows only distant shapes; no wall finish read | not read | not read | not read | **Cannot tell** |

## For the owner (no change made here)

- 05 reads as a plausible rich-house or small-town look (plaster, flat deck, courtyard): the Safed photograph is the town's look
  about 30 years before 1948 and the quarter is not identified. It is a weaker fit for the hill peasant house described by Amiry
  (limestone, door surround whitewashed, blue or green doors); Canaan does not settle the outside finish.
- If a second look is wanted: a bare-stone variant for the hill villages, a clay-brick variant for the plain, and nothing for the
  Huleh reed-mat villages. This note does not say that any of them must be built.
- Wooden shutters are attested (Canaan 1933 p. 25); only their colour was not found. Blue or green is for door panels ([S]), and no
  source read gives shutter colour, so the brown on 05 is neither supported nor contradicted.

## Search log

Day run: 2026-11-03. Tools: `curl`, the built-in WebSearch and WebFetch (no Tavily key on this box).

| Search or fetch | Where | What it returned |
|---|---|---|
| archive.org advancedsearch, "Journal of the Palestine Oriental Society" | `https://archive.org/advancedsearch.php` | 8 rows; volumes 1 (1920-21) and 13 (1933) among them; volume 12 opened by its identifier `journal-of-the-palestine-oriental-society_1932_12` (HTTP 200, 627,972 bytes) |
| archive.org advancedsearch, "survey of palestine anglo-american committee" | same | `surveyofpalestin0000unse` is a 1990 reprint and "access-restricted"; its full text returned HTTP 401. Not opened. |
| archive.org advancedsearch for Hope Simpson 1930 and for Johnson and Crosbie | same | no row for either report; not found on archive.org by this search |
| *Handbook of Palestine* (1922), `handbookofpalest00luke` OCR text, searched for roof, plaster, whitewash, dwelling | archive.org | no passage on village house walls or roofs; the Hebron entry says "tall stone houses, narrow streets" (not used as a row) |
| Baedeker 1906 OCR, searched for flat roof, whitewash, plastered, houses | archive.org | only the Jaffa line used; entries for Acre, Safed, Nazareth, Bethlehem, Beit Jala and 'Ain Karim give no house material |
| Library of Congress Matson search, `https://www.loc.gov/pictures/search/?q=<place>&co=matpc&fo=json`, about 60 place queries | loc.gov | worked for the first queries, then HTTP 429 and a Cloudflare "Just a moment" page for every later search and for item and resource pages (`/pictures/item/..`, `/item/..`, `/resource/..` gave 429 or 403). Not bypassed. Image files on `tile.loc.gov` still answered 200. |
| Wikimedia Commons API, `list=search`, namespace 6, queries "matpc <place>" | commons.wikimedia.org | gave the `matpc` numbers used to build the `tile.loc.gov` URLs above; dates are from the LOC search records read earlier, matched by title |
| Palestine Birzeit museum page for Canaan's writings `https://museum.birzeit.edu/research/tawfiq-kanaan/jpos` | WebFetch and curl | connection refused (ECONNREFUSED); not read |
| WebSearch, "Mandate Palestine Arab village houses stone flat roof domed whitewashed plastered ..." | built-in WebSearch | links only; its summary was not used; the one useful link was opened (Amiry and Tamari PDF, HTTP 200, 896 lines of text) |

## Open gaps

- **[GAP]** Wall finish and colour of Arab houses in Acre, Jaffa, Tantura, the Safed Arab quarter and Galilee hill villages in
  1948. Tried: the sources in the search log. Not opened: Canaan's 1933 book edition (Syrian Orphanage Press), T. Kanaan 1964
  (not found online), Jaeger 1912 *Das Bauernhaus in Palästina* (cited by Canaan, not opened), *A Survey of Palestine* (1946,
  borrowable only), the Library of Congress item pages (rate limited).
- **[GAP]** Any 1940s count of plastered against bare houses.
- **[GAP]** Colour: only black and white photographs were read.
- **[GAP]** Model 05's storey count and size were not read from the 3D model, only its roof note and colour map.
- **[GAP]** The Siloam and Majdel photographs opened (LC-DIG-matpc-04215, -19867) show no house surface at this size and are not rows.
