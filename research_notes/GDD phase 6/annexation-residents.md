# Annexation: the resident counts for Acre and Nazareth (research note, 2026-10-23)

Status: research for #799 (OWNER QUESTION 1 (a) of plan #1520, "the same rule as the 12 starting cities:
a sourced resident count you approve"). The two counts are in `content/settlement_targets.yaml` as `annexation.people`.
The owner approved both on 24 Oct 2026 ("Approve both", https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6379785979).

## The rule they follow

The starting cities use the 8 November 1948 Registration of Population (the census day), all residents counted, labelled
with their date (owner, 15 October 2026, on #569; `research_notes/GDD phase 3/city-start-values-1948-cbs.md`). The same
source and basis are used here. The 1945 Village Statistics figures (Acre 12,360; Nazareth 14,200) are "before" values
and are not used (plan #1520, global constraints).

## The figures [V]

Both read from the scans on the CBS website on 2026-10-23, agent read; a person's check is pending.

| place | 8 XI 1948 | 31 XII 1951 | where |
|---|---|---|---|
| Nazareth (נצרת (עיר)) | 16,994 | 20,000 | CBS Statistical Abstract of Israel No. 3 (1951/52), list of settlements, p. 141. https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1952_num3.pdf |
| Acre (עכו (עיר)) | 4,016 | 15,650 | same list, p. 142 |

Cross-check: CBS Statistical Abstract No. 14 (1963), Table 11, p. 31
(https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf) gives Akko 4,016 on 8 XI 1948, the
same figure. For Nazareth that table shows ".." (no figure) on 8 XI 1948 and 18,500 at the end of 1949. The same SA 3 page
gives Netanya 11,589, which matches SA 14 and `cities.yaml`, so the column is the census day.

## What the counts are and are not [G]

- They count everyone present on the census day. The research packet for #793 (`settlement-targets.md`) lists
  "residents and refugees not separable" as a gap for Nazareth, so the 16,994 may include people who were not residents
  before July 1948. Not checked further here.
- Acre's figure is taken nearly six months after its surrender (17-18 May 1948). The #793 packet records about 5,000 remaining
  after the surrender, many of them refugees from Haifa and non-local workers (Morris pp. 228-232). Whether the 4,016 also
  includes new Jewish residents settled in Acre by November 1948 was not checked [G].
- Jobs: no per-town working share was found; the national share 0.439 (SA No. 2, as for seven of the starting cities) is
  used as a design value, as the owner's answer says.

## Owner decision

The owner approved 16,994 for Nazareth and 4,016 for Acre on 24 Oct 2026 ("Approve both", https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6379785979).
The `mvp-annex-nazareth` golden pins the approved Nazareth count.
