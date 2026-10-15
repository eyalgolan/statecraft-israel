# Sourcing: a checklist for contributors

The rules live in `docs/CONTENT_POLICY.md` (section 1 framing, section 2 sources) and `docs/WRITING_EVENTS.md`. This page is the short version for the moment you are looking for a source, plus what to do when you cannot reach one.

## Before you write a fact

- [ ] Every fact has a `https://` source and the day you checked it (`sources: - { url, checked }`).
- [ ] You opened the page and read the claim there. A search snippet or a summary is not a source.
- [ ] If the fact is a date, the source gives that date, not a nearby one. A term's `sources:` must support the date the term carries (a source for the announcement does not support the start date).
- [ ] One value per fact, chosen by the rule in `docs/CONTENT_POLICY.md` section 1. When two sources disagree, record both in the PR and let the maintainer choose.
- [ ] No quotes unless verbatim and sourced. Paraphrase otherwise.
- [ ] Images: portraits are public-domain, CC0, CC BY 4.0 or CC BY-SA 4.0 photographs; a portrait cropped from a CC BY-SA 4.0 photograph is itself released under CC BY-SA 4.0 and credited in `NOTICE`, and any other licence is refused (`docs/CONTENT_POLICY.md` principle 5, `LICENSING.md`). A file whose only licence is a PD-Israel tag on Wikimedia Commons is refused for event photographs (`docs/CONTENT_POLICY.md` section 4), and for portraits it is an open decision (D-P in `docs/oss/DECISIONS.md`); ask before using one.

## Where to look

- The Israeli sources named in `docs/CONTENT_POLICY.md` section 1 (the Central Bureau of Statistics, the IDF, the Foreign Ministry, the State Archives) and the Knesset's own records, then mainstream Israeli historiography.
- English and Hebrew Wikipedia are accepted cited sources here. Cite the exact article and, for a number or date, the footnote it rests on, and say so if it rests on one secondary source.
- When you add primary or official sources to a record that cites Wikipedia, drop a Wikipedia article only if the new sources cover every line it supported. Otherwise keep it, and say in the PR which lines still rest on it.
- Knesset plenary records and laws are PDFs on `fs.knesset.gov.il`.

## When you cannot reach a source

Some sources fail for the reader, not because the fact is wrong. Seen so far:

- `fs.knesset.gov.il` PDFs and `main.knesset.gov.il` pages can return an HTML "geo-block" page to non-Israeli networks or to scripted requests, with a 200 status. If you saved a "PDF" and it opens as HTML, that is what happened.
- Long PDFs can be cut off by a fetch tool, so the end of a sitting (a final vote tally) is missing from the text you got.
- Some newspaper archives and the Wayback Machine can return 403 or be offline.
- Wikimedia Commons' API limits fast clients; use a descriptive User-Agent and pause between calls.

What to do, in this order:

1. Try another route to the same record: another sitting or page, the Hebrew or English version, the printed page numbers, an official mirror.
2. If you still cannot read the claim, do **not** fill the gap from memory or from a nearby date. Leave the value as it is (or leave the field out) and write in the PR and in any research note (`research_notes/`): what you wanted, which URLs you tried, what each returned ("geo-block page", "text stops at item 20", "403"), and what is still open.
3. A secondary source may carry the fact. Cite it as what it is and say in the PR that the primary source was not reachable. The maintainer decides whether that is enough.
4. If a reviewer cannot open your source either, say so, and attach what you saw (for example the page text you read, with the URL and the day).

## Disclose how you worked

If an AI assistant helped, say so under "AI assistance" in the PR (`CONTRIBUTING.md`) and say which claims you checked yourself against which pages.
