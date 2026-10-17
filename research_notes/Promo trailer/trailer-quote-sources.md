# Sources for the promo trailer's attributed quotes (Herzl, Rabin, Ben-Gurion, Begin, Netanyahu)

Researched 2026-10-15. Draft for issue #639: the sources first, before any video script guidance. Nothing here changes the trailer or any game file.

## Purpose

The trailer shows seven attributed quotation cards from five speakers. The only record of them in the repository is the caption files `site/public/media/promo-trailer.en.vtt` and `site/public/media/promo-trailer.he.vtt`. This note records, for each card: the text as shown, whether it is verbatim, an excerpt or a different rendering, the full original sentence, the speaker and role, the date and occasion, the original language, who translated it, the source URL, and a status. It uses the rows of the quote source sheet proposed in #639.

## Conventions

- [V] the page was opened and the passage read on it on 2026-10-15 (URL given). [S] a secondary source, or a search snippet, or a page not opened in full. [GAP] not found.
- Status is one of: confirmed, wording differs from source, source not found.
- Dates are written 15 May 1948 style. No quote is invented. The short passages in quotation marks are copied from the cited page, and where a source sentence contains a dash, the quotation stops before it and the rest is described in words.
- Both caption files were read from `upstream/main` (commit `835760f5`). The Hebrew file repeats each English quotation after the words "on the screen, in English" and does not translate the quotations.

## 1. What the trailer shows

| Card | Time in the caption file | Text as shown | Attribution as shown |
|---|---|---|---|
| Q1 | 00:00 to 00:07 | At Basel I founded the Jewish State. | Theodor Herzl, founder of political Zionism |
| Q2 | 00:07 to 00:12 | ...in fifty, everyone will know it. | Theodor Herzl |
| Q3 | 00:20 to 00:25 | At the tender age of sixteen, I was handed a rifle... | Yitzhak Rabin, Prime Minister of Israel |
| Q4 | 00:31 to 00:36 | ...open for Jewish immigration and for the Ingathering of the Exiles. | David Ben-Gurion, Declaration of Independence |
| Q5 | 00:48 to 00:51 | No more war. No more bloodshed. | Menachem Begin, Prime Minister of Israel |
| Q6 | 00:51 to 00:55 | Citizens of Israel, we are at war. | Benjamin Netanyahu, Prime Minister of Israel, 7 October 2023 |
| Q7 | 01:12 to 01:17 | If you will it, it is no dream. | Theodor Herzl |

[V] `site/public/media/promo-trailer.en.vtt` and `promo-trailer.he.vtt` on `upstream/main`. Neither file carries a source, a date (except Q6) or a translator for any card. Each card appears on screen as the quotation, a dash and the attribution. The English file's first note says the trailer has music and no speech.

## 2. Summary

| Card | Class | Status | Primary text opened |
|---|---|---|---|
| Q1 | excerpt of a translated diary entry | confirmed in the German primary text; English translation source not identified | yes [V], *Tagebücher*, vol. 2, p. 24. |
| Q2 | excerpt of the same entry | confirmed in the German primary text; English translation source not identified | yes [V], *Tagebücher*, vol. 2, p. 24. |
| Q3 | verbatim excerpt of the published English text, capital "At" added | confirmed, with a note on the ellipsis | yes [V] |
| Q4 | verbatim excerpt of the MFA English translation, the first clause of a longer sentence | confirmed, with a note on the cut and the attribution | yes [V] |
| Q5 | excerpt with the punctuation changed and the third phrase dropped | wording differs from source | yes [V] |
| Q6 | verbatim excerpt of the PMO English translation (original Hebrew), two lines joined into one | confirmed, with a note on joining | yes [V] |
| Q7 | a common English rendering of a German motto | wording differs from source | no. The German wording is [S]. |

## 3. Quote by quote

### Q1 and Q2. Herzl, diary entry of 3 September 1897 (shown on two cards)

- Text as shown: "At Basel I founded the Jewish State." and "...in fifty, everyone will know it."
- Full passage, as an English rendering gives it: a few days after the First Zionist Congress, Herzl wrote that if he were to sum up the Basel Congress in a word, it would be "At Basel, I founded the Jewish State". The passage goes on that if he said this out loud today he would be answered by universal laughter, and that perhaps in five years, certainly in fifty, everyone will know it. [S] https://en.wikiquote.org/wiki/Theodor_Herzl (opened 2026-10-15). The page cites a secondary book, Judy S. Bertelsen, *Nonstate Nations in International Politics* (1977), p. 37, and no edition of the diaries.
- Speaker and role: Theodor Herzl, founder and leader of the World Zionist Organization [S, Wikiquote]. The card says "founder of political Zionism".
- Date and occasion: diary entry of 3 September 1897, a few days after the First Zionist Congress in Basel [S, Wikiquote]. A search snippet gives 29 August 1897 as the Congress start date [S]. The Congress dates were not read on an opened page [GAP].
- Original language: German. *Theodor Herzls Tagebücher*, vol. 2 (Berlin: Jüdischer Verlag, 1923 on the title page; the archive.org record says 1922), p. 24 [V] https://archive.org/details/theodorherzlstag02herzuoft (page 24 of the scan, read in the second review on PR #648, 15 Oct 2026).
- Translator and translation source: not identified. The Wikiquote page cites the English edition of the diaries (*The Complete Diaries of Theodor Herzl*, ed. Raphael Patai, trans. Harry Zohn, 1960) for other entries but not for this one [S]. Other English renderings of the same sentence end "everyone will perceive it" (a blog and a community site, search snippets [S]). The card's "everyone will know it" matches the Wikiquote rendering and not those.
- Status: confirmed in the German primary text [V], *Tagebücher*, vol. 2, p. 24. To close: the Zohn volume and page, and the wording of the translation the trailer used.
- Note: the card trims "Perhaps in five years, certainly" from the sentence. The ellipsis does not reverse the meaning. The two cards are two parts of one diary entry shown 5 seconds apart, and they should be recorded as one passage.

### Q3. Rabin, Nobel lecture, Oslo

- Text as shown: "At the tender age of sixteen, I was handed a rifle..."
- Full sentence [V] https://www.nobelprize.org/prizes/peace/1994/rabin/lecture/ : "At an age when most youngsters are struggling to unravel the secrets of mathematics and the mysteries of the Bible; at an age when first love blooms; at the tender age of sixteen, I was handed a rifle so that I could defend myself" and the sentence goes on, after a dash, with a clause that he was handed it also so that he could kill in an hour of danger.
- Difference from the source: the card starts with a capital "At" (the source has a lowercase "at" inside a longer sentence). The ellipsis removes both the purpose of the rifle and the second clause.
- Speaker and role: Yitzhak Rabin. The lecture page does not state his office. The NobelPrize.org facts page gives "Role: Prime Minister of Israel" [V] https://www.nobelprize.org/prizes/peace/1994/rabin/facts/ .
- Date and occasion: Nobel Peace Prize ceremony, Oslo City Hall, 10 December 1994, where Rabin, Shimon Peres and Yasser Arafat received the prize. The ceremony date comes from a search snippet of the NobelPrize.org Peres photo gallery [S]. The lecture page opened does not show the date.
- Original language: not stated on the page [GAP]. The text is printed in English in *Nobel Lectures, Peace 1991-1995*, ed. Irwin Abrams (World Scientific, 1999), copyright the Nobel Foundation 1994 [V, citation line on the lecture page].
- Translator: not stated [GAP].
- Status: confirmed (wording). Decision needed: the ellipsis cuts the sentence before its point, which is both a defensive and a killing purpose. See section 4.

### Q4. Declaration of the Establishment of the State of Israel, 14 May 1948

- Text as shown: "...open for Jewish immigration and for the Ingathering of the Exiles."
- Full sentence [V] https://www.gov.il/en/pages/declaration-of-establishment-state-of-israel : "THE STATE OF ISRAEL will be open for Jewish immigration and for the Ingathering of the Exiles; it will foster the development of the country for the benefit of all its inhabitants" and the sentence continues through further clauses, including "it will ensure complete equality of social and political rights to all its inhabitants irrespective of religion, race or sex".
- Speaker and role: the text is the proclamation approved by the Jewish People's Council at the Tel Aviv Museum on 14 May 1948. The page's picture caption says David Ben-Gurion reading it. The page does not say Ben-Gurion wrote the sentence [V].
- Date and occasion: 14 May 1948 (5 Iyar 5708), published in the Official Gazette No. 1 of 5 Iyar 5708 [V, same page].
- Original language: no opened page states it [GAP]. The opened page publishes the English text and names the Official Gazette as the place of publication. The Hebrew text itself was not opened [GAP].
- Translator: not named on the page [GAP]. The page sits under the Ministry of Foreign Affairs on gov.il.
- Status: confirmed (wording).
- Note: the card is attributed to Ben-Gurion and to the Declaration. The page supports "the Declaration of Independence, read by David Ben-Gurion". It does not support him as the author of the clause.

### Q5. Begin, signing of the Egypt-Israel peace treaty, 26 March 1979

- Text as shown: "No more war. No more bloodshed."
- Full sentence [V] https://www.presidency.ucsb.edu/documents/remarks-president-carter-president-anwar-al-sadat-egypt-and-prime-minister-menahem-begin : Begin says it is time for all of them to show civil courage "in order to proclaim to our peoples and to others: No more war, no more bloodshed, no more bereavement." It continues "Peace unto you, shalom, salaam" and a closing "forever", with a dash in the source before the last word.
- Differences from the source: the card uses full stops where the source has commas, and it drops "no more bereavement". The card therefore ends on a pair, where the speaker said three.
- Speaker and role: Menachem Begin, Prime Minister of Israel (the page lists him as "Prime Minister Begin") [V].
- Date and occasion: 26 March 1979, the North Lawn of the White House, at the signing ceremony of the treaty (the page's note says it was broadcast live) [V]. The page's note gives the time of the President's remarks as 2:12 p.m.
- Original language: English. The page marks Begin's Hebrew passages ("[In Hebrew]", "[At this point, Prime Minister Begin spoke in Hebrew]") and the line used on the card sits in the English part [V]. No translator is needed.
- Source: the American Presidency Project, citing Jimmy Carter, Remarks of President Carter, President Anwar al-Sadat of Egypt, and Prime Minister Menahem Begin of Israel at the Egyptian-Israeli Peace Treaty Signing Ceremony, https://www.presidency.ucsb.edu/node/249358 [V].
- A second source agrees on the three-part line: a UPI year-in-review audio page attributes "No more war, no more bloodshed, no more bereavement. Peace unto you, shalom!" to Begin [S, search snippet]. The Washington Post and the New York Times pages for 27 March 1979 were not opened.
- Do not confuse with: in the same ceremony Sadat said "Let there be no more wars or bloodshed between Arabs and Israelis". That is Sadat, not Begin [V, same page].
- Status: wording differs from source (punctuation and the dropped phrase).

### Q6. Netanyahu, statement of 7 October 2023

- Text as shown: "Citizens of Israel, we are at war."
- Full text [V] https://www.gov.il/en/pages/statement-by-prime-minister-benjamin-netanyahu-7-oct-2023 : "Citizens of Israel," on its own line, then "We are at war, not in an operation or in rounds, but at war."
- Difference: the card joins the two lines, lowercases "we" and ends on a full stop where the source continues. The sentence on the card is the first clause of the source sentence.
- Speaker and role: Prime Minister Benjamin Netanyahu [V, page title].
- Date and place: Saturday 7 October 2023, from the Kirya in Tel Aviv [V].
- Original language: Hebrew. The page says "[translated from Hebrew]" [V].
- Translator: not named. The gov.il page is the Prime Minister's Office text [V].
- Status: confirmed (wording), as a joined excerpt. The page's own heading line is "We are at war, not in an operation or in rounds, but at war".

### Q7. Herzl, motto of *Altneuland* (1902)

- Text as shown: "If you will it, it is no dream."
- Original: German, "Wenn ihr wollt, ist es kein Märchen" [S]. The German phrase is the title of several German-language publications and of a Brill reference entry (search snippets, not opened). The Wikiquote Herzl page gives the German, places it as the prefix to *Altneuland* (1902), and renders it "If you will, it is no legend" [S] https://en.wikiquote.org/wiki/Theodor_Herzl (opened).
- Difference: "Märchen" means a fairy tale or fable. "Dream" is a looser rendering and the plural "ihr" is "you" addressed to a group. The English line on the card is common, but it is not the literal sense, and the repository does not say which translation it follows.
- Translator: not identified for the card's wording. The standard English edition of the novel was translated by Lotta Levensohn (Bloch, 1941), and the first Hebrew edition, "Tel Aviv", by Nahum Sokolow (Warsaw, 1902) [S, en.wikipedia "The Old New Land", opened]. Whether Levensohn's motto reads "no dream" was not checked [GAP]. A *Jerusalem Post* article of 12 July 2020 gives "If you will it, it is no dream" as the English of "im tirtzu, ein zo agada" [S, lead, not opened] https://www.jpost.com/israel-news/remembering-herzl-on-his-116th-yahrtzeit-634781 .
- Date: 1902, the publication year [S, Wikipedia and Wikiquote].
- Status: wording differs from source (a translation choice). To close: the German title page of the 1902 edition and the wording in a named English translation.

## 4. Points for the maintainers

1. Begin (Q5): the card shortens a three-part line to two and changes the punctuation. The owner's ruling on #650 of 16 October 2026 is to "keep v1 everywhere", so the original two-part card stays. This supersedes the 15 October maintainer decision to re-render it with the full line. The status remains "wording differs from source".
2. Rabin (Q3): the ellipsis stops before "so that I could defend myself". Decide whether to complete that clause.
3. Declaration (Q4): decide the attribution line. The page supports "Declaration of Independence, 14 May 1948" and a reading by Ben-Gurion.
4. Herzl (Q1, Q2, Q7): the owner's ruling on #650 of 16 October 2026 is to "keep v1 everywhere", with the motto "If you will it, it is no dream." This supersedes the 15 October maintainer decision to use Levensohn's "If you will it, it is no fable." A named translation for Q1 and Q2 would still identify their translator; Q7's status remains "wording differs from source".
5. Translated quotations (Q1, Q2, Q4, Q6, Q7 are translated, and Q3 may be): the cards do not say the English is a translation. The caption files do not carry a translator either.
6. The Hebrew caption file describes the English text on screen and gives no Hebrew rendering of any quotation. If a Hebrew version of a card is ever shown, each quotation that was spoken or written in Hebrew needs its original Hebrew wording. Netanyahu's statement was Hebrew [V]. The Declaration's original language is not yet sourced [GAP, see Q4]. Rabin's language is a gap. The Hebrew wording goes to the native reviewer in #564.

## 5. Gaps

- Herzl: the Zohn volume and page for the English rendering of the entry of 3 September 1897; the First Zionist Congress dates on an opened page.
- Rabin: the language of delivery and any translator; the lecture date on an opened Nobel page.
- Declaration: a source for its original language, the Hebrew text and the name of the translator of the gov.il English text.
- Altneuland: the German title page; the wording of a named English translation.

## 6. Sources opened on 2026-10-15

- https://www.nobelprize.org/prizes/peace/1994/rabin/lecture/ [V]
- https://www.gov.il/en/pages/declaration-of-establishment-state-of-israel [V]
- https://www.presidency.ucsb.edu/documents/remarks-president-carter-president-anwar-al-sadat-egypt-and-prime-minister-menahem-begin [V]
- https://www.gov.il/en/pages/statement-by-prime-minister-benjamin-netanyahu-7-oct-2023 [V]
- https://en.wikiquote.org/wiki/Theodor_Herzl [S, secondary]
- https://en.wikipedia.org/wiki/The_Old_New_Land [S, secondary]
- `site/public/media/promo-trailer.he.vtt` and `site/public/media/promo-trailer.en.vtt` on `upstream/main` [V]
