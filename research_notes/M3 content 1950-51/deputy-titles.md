# Deputy titles for issue #4

Checked 11 October 2026. This note distinguishes historical titles from gameplay responsibilities.

## Model

`historical_only: true` keeps ordinary dated office terms and text lookup, but does not add a Cabinet seat,
player appointment, policy owner, effect owner, event delegate or budget line. Stale player appointments
cannot replace its historical holder. No parent-office relationship or roster UI is added.

The three new people have neutral game ratings (competence 5, loyalty 5), not historical assessments.
Like other people, they can still be candidates for ordinary ministerial appointments. The historical-only
flag restricts a title, not a person.

## Records and dates

| Person | Title | Interval [from, to) | Basis |
| --- | --- | --- | --- |
| Yosef Efrati | Deputy Agriculture | 26 Feb 1951-8 Oct 1951 | Official mirrored role rows 21964/21965; notification agenda 26 Feb |
| Yosef Efrati | Deputy Agriculture | 4 Nov 1951-25 Jun 1952 | Official mirrored role row 21963; deputy appointments agenda 4 Nov |
| Reuven Shari | Deputy Transport | 2 Apr 1951-8 Oct 1951 | Notification agenda 2 Apr and 1951 law; biography agrees |
| Kalman Kahana | Deputy Education and Culture | 4 Nov 1951-24 Dec 1952 | Official mirrored role row 4586; deputy appointments agenda 4 Nov |
| Eliezer Kaplan | Deputy Prime Minister | 25 Jun 1952-13 Jul 1952 | Primary 25 Jun 1952 proceeding and mirrored role row 3845; included with minimal 1952 succession fix |

The role-table rows split at 20 Aug 1951 when the Knesset changes. Adjacent pieces of the same office
are joined, without creating a break in the appointment. Efrati's 9 Jul 1952 reappointment is outside the
1949-1951 starting-term scope and is not part of these entries.

## Source chain

Open Knesset describes its data as taken mostly from official Knesset interfaces, updated daily:
https://oknesset.org/

Downloaded and inspected:
- Names/identities: https://production.oknesset.org/pipelines/data/members/kns_person/kns_person.csv
- Role definitions: https://production.oknesset.org/pipelines/data/members/kns_position/kns_position.csv
- Dated roles and factions: https://production.oknesset.org/pipelines/data/members/kns_persontoposition/kns_persontoposition.csv
- Session dates: https://production.oknesset.org/pipelines/data/plenum/kns_plenumsession/kns_plenumsession.csv
- Session agenda: https://production.oknesset.org/pipelines/data/plenum/kns_plmsessionitem/kns_plmsessionitem.csv
- Transcript links: https://production.oknesset.org/pipelines/data/plenum/kns_documentplenumsession/kns_documentplenumsession.csv

Resolved PersonIDs 2161 Efrati, 2666 Shari, 2404 Kahana, 2590 Kaplan. Position 40 is deputy minister, 50 Deputy PM.
Roles/factions corroborate Mapai for Efrati/Shari and Poalei Agudat Yisrael for Kahana in the relevant terms.
These are community-hosted mirrors of official records, not an independent historical source.

The 1951 Transition Law amendment says a deputy enters office upon notification to the Knesset,
acts within the minister's allocated matters/powers, and does not diminish the minister's responsibility:
https://fs.knesset.gov.il/1/law/1_lsr_208918.pdf

Notification agenda joins:
- 26 Feb 1951 session 157470, item 492460: appointment of deputy Agriculture minister.
- 2 Apr 1951 session 157481, item 492521: Transport minister notification of deputy appointment.
- 4 Nov 1951 session 489635, item 492690: Justice minister notification of deputy appointments.

The exact underlying transcripts were located but could not be read due to geographic-unavailable responses:
https://fs.knesset.gov.il/1/Plenum/1_ptm_250070.pdf
https://fs.knesset.gov.il/1/Plenum/1_ptm_250077.pdf
https://fs.knesset.gov.il/2/Plenum/2_ptm_250618.pdf
The agenda proves the notification date/topic, not the verbatim appointment text.

Biographies checked for identity, non-living status and party:
https://en.wikipedia.org/wiki/Yosef_Efrati
https://en.wikipedia.org/wiki/Reuven_Shari
https://en.wikipedia.org/wiki/Kalman_Kahana

## Conflicting claims retained

- Shari's role-table start is 1 Apr 1951, while the notification is 2 Apr and his English biography says 2 Apr.
  Use 2 Apr under the notification-date rule. Whether 1 Apr was cabinet approval is unverified.
- Efrati's Hebrew biography cites an Al HaMishmar article dated 5 Jan 1951 for an appointment, while the
  formal role table/agenda gives 26 Feb. The article was not accessible, so no claim about its meaning
  is made.
- Kahana's Hebrew biography starts 8 Oct 1951 and the Education ministry table ends 18 Sep 1952. The
  official mirrored role data gives 4 Nov 1951-24 Dec 1952, agreeing with the Third Government table.
  The 4 Nov notification supports the later start; an end-date transcript remains unchecked.
- Kaplan's Deputy PM start: en.wikipedia.org/wiki/Third_government_of_Israel lists him as Deputy Prime Minister from
  8 Oct 1951, as does facts.md ("Addendum (2026-10-10): Third Government portfolios"). The 25 Jun 1952 proceeding, the mirrored role row
  and en.wikipedia.org/wiki/Deputy_Prime_Minister_of_Israel (25 Jun-13 Jul 1952) give the later date. Use 25 Jun 1952.
- Do not repeat the English Shari biography's "first ever deputy" claim: Efrati's formal record starts earlier.

## Kaplan and 1952 scope

The 25 Jun 1952 proceeding explicitly says Kaplan leaves Finance, stays minister without portfolio
as Deputy PM, Eshkol takes Finance and Naftali takes Agriculture:
https://fs.knesset.gov.il/2/Plenum/2_ptm_250662.pdf
The mirrored roles give Deputy PM 25 Jun-13 Jul 1952. Before this change, content left Kaplan in Finance and
Eshkol in Agriculture open-ended after 1951. Adding Kaplan's title alone does not fix those 1952 senior
portfolio gaps. This PR proposes the minimal 25 June 1952 succession fix for #4. The new Finance/Agriculture terms stay open-ended,
like the rest of the file, until #26 adds the Fourth Government (24 December 1952); the rest of #26 remains separate.
The same day Justice passed from Dov Yosef to Haim Cohn, and Lavon became Minister without Portfolio on 17 Aug 1952
(en.wikipedia.org/wiki/Third_government_of_Israel); both are left to #26.
No new Development office, government records or later terms are added.

The existing 1950/1952 goldens end 27 February 1950 and 27 February 1952, before this June change.
Both hashes stay unchanged. The new Cabinet boundary test checks the later rollover directly.
