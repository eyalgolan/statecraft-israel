# City-project availability in May 1948

> **Owner ruling, 19 October 2026: `existing-policy-only`.** No current `projects.yaml` kind gets an additional lock, so none is locked (`docs/rules/mvp.md:271`).

> **DRAFT - AI-read research; a person's check is pending for every sourced fact and quotation below.** The sources were opened and read by an AI agent on 19 October 2026. This note records research behind the ruling; it does not set an unlock rule.

## Answer under the ruling

`housing` and `industry` are available on 15 May 1948. `transit_camp` has no project-kind lock, but the existing
`maabarot` policy gate makes it unavailable until that policy reaches 0.5. This uses only the existing policy rule in
`docs/rules/mvp.md:79`; it needs no new lock or code.

## Scope read from the repository

`content/projects.yaml:16-19` at worktree HEAD `00441e1d77b07f008c949ef3cd0ec2c8b76730b5` contains exactly three
project-kind ids: `housing`, `transit_camp`, and `industry`. None has `locked: true`.

`sim/Statecraft.Sim/ContentLoader.cs:371-377` reads `projects.yaml` into `Content.ProjectKinds` only in the GDD loop and
then validates the GDD records. The GDD also discusses specialized industry (`docs/GDD.md:142-150`), but
there is no specialized-industry row in `content/projects.yaml`, so it is outside this per-kind note.

## Findings by kind

### `housing`: historically available at the start

The official Central Bureau of Statistics yearbook says that building activity existed before the state and that the
Jewish Agency collected records of building authorized, started, and completed from April 1947 through January 1948.
This is evidence that ordinary construction was already possible before May 1948; no later law, agreement, loan, or
newly founded institution is needed as an unlock event for this broad project kind.

Source [AI-read, person's check pending]: Central Bureau of Statistics, *Statistical Abstract of Israel 1950*, building
notes, pp. 137-138,
https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1950_num1.pdf, checked by AI: 2026-10-19.

Quoted sentence [person's check pending]: "Before the establishment of the State, the main building activity was
concentrated in the settled areas included in the current statistics."

Research conclusion: available on 15 May 1948. This matches the owner's ruling.

### `transit_camp`: historically unavailable at the start under the existing policy gate

The Technion-hosted article identifies the ma'abara as a new state policy intended to replace immigrant camps and work
villages. It says the first ma'abara opened at Ksalon in May 1950 and cites a *Davar* report dated 23 May 1950 describing
families already housed and employed there.

Source [AI-read, person's check pending]: Yael Allweil, "Israeli Housing and Nation Building," *Traditional Dwellings
and Settlements Review* 23(2), pp. 60, 67,
https://allweil.net.technion.ac.il/files/2019/07/23.2_TDSR_Spr_12_allweil.pdf, checked by AI: 2026-10-19.

Quoted sentence [person's check pending]: "The first maabara opened in Ksalon in the Jerusalem Mountains in May 1950."

Research conclusion: unavailable on 15 May 1948. The earlier draft proposed a project-kind lock and a conservative
unlock date of 23 May 1950. The owner's later `existing-policy-only` ruling supersedes that implementation proposal:
`transit_camp` remains unlocked as a kind, while `StartProject` refuses it until the existing `maabarot` policy reaches
0.5 (`docs/rules/mvp.md:79`; `sim/Statecraft.Sim/Projects.cs:212-217`).

The source directly supports the month. The exact day is the article's date for the report, not a claimed opening day.
A person must open the underlying newspaper or archival record before using 23 May 1950 as an exact historical opening
date.

### `industry`: historically available at the start

The official CBS yearbook contains industrial-production data beginning in January 1948 and an industrial-employment
index covering 1947-1949. These establish that industrial production and industrial workplaces existed before May
1948; no later enabling event is needed for the broad `industry` kind.

Source [AI-read, person's check pending]: Central Bureau of Statistics, *Statistical Abstract of Israel 1950*,
"Industrial Production, by Item (I 1948-III 1950)" and "Index Numbers of Persons Employed (1947-1949)," pp. 82-84,
https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1950_num1.pdf, checked by AI: 2026-10-19.

Quoted sentence [person's check pending]: "The indexes of persons employed, man-days worked, wages and salaries, and
daily earnings per employee are calculated and published by the Bureau."

Quoted table text [person's check pending]: "Table 2. INDEX NUMBERS of PERSONS EMPLOYED" and "(1947-1949)".

Research conclusion: available on 15 May 1948. This matches the owner's ruling.

## Result

| Project kind | Available on 15 May 1948? | Governing rule | Historical basis and pending check |
|---|---|---|---|
| `housing` | Yes | No added lock | Official statistics describe pre-state building activity. A person must check the quoted page. |
| `transit_camp` | No | Existing `maabarot` policy gate, not a project-kind lock | May 1950 is directly stated in the AI-read article. A person must check it and the underlying *Davar* item before using an exact day. |
| `industry` | Yes | No added lock | Official statistics cover pre-state industrial activity. A person must check the cited pages. |

## AI assistance

This note was researched and drafted by AI. An earlier AI agent opened the sources tagged above on 19 October 2026.
This agent adapted that draft to current repository policy without network access and did not reopen the sources. A
person's check remains pending for every sourced fact and quotation before any of them are used as verified content.
