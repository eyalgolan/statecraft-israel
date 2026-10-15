// Build-time readers for the repository's own data. The website shows sourced content from content/ and the asset
// records from ASSETS.yaml as they are; it writes no historical claim of its own.
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';

function findRoot(): string {
  for (const dir of [path.resolve(process.cwd(), '..'), process.cwd()]) {
    if (fs.existsSync(path.join(dir, 'ASSETS.yaml')) && fs.existsSync(path.join(dir, 'content'))) return dir;
  }
  throw new Error('site: run the build from site/ inside the statecraft-israel repository');
}

export const repoRoot = findRoot();
const read = (rel: string) => fs.readFileSync(path.join(repoRoot, rel), 'utf8');
const readYaml = (rel: string) => YAML.parse(read(rel));

export interface Source { url: string; checked?: string; note?: string }
export interface Option { id: string; text: string; historical?: boolean }
export interface GameEvent {
  id: string; title: string; text: string; from: string; to?: string; office?: string; tags: string[];
  status: string; options: Option[]; sources: Source[];
}

let officeTitles: Map<string, string> | undefined;
function offices(): Map<string, string> {
  if (!officeTitles) {
    const doc = readYaml('content/offices.yaml');
    officeTitles = new Map((doc.offices as { id: string; title: string }[]).map((o) => [o.id, o.title]));
  }
  return officeTitles;
}

/** Event text names offices as {prime_minister}; the game fills in the holder. Here it reads "the Prime Minister". */
export function fillOffices(text: string): string {
  return text.replace(/\{([a-z_]+)\}/g, (_m, id: string, offset: number, whole: string) => {
    const title = offices().get(id) ?? id.replace(/_/g, ' ');
    const before = whole.slice(0, offset).trimEnd();
    const startOfSentence = before === '' || /[.!?]$/.test(before);
    return `${startOfSentence ? 'The' : 'the'} ${title}`;
  });
}

const toIso = (v: unknown) => (v instanceof Date ? v.toISOString().slice(0, 10) : String(v ?? ''));

/** Every event in content/events/, by date. Only events that pass the release gate's rule: a sensitive tag needs
 * sensitivity-reviewed (docs/CONTENT_POLICY.md 2.1). Drafts are left out. */
export function events(): GameEvent[] {
  const dir = path.join(repoRoot, 'content/events');
  const sensitive = new Set(['holocaust', 'terror', 'massacre', 'war-casualties']);
  return fs.readdirSync(dir).filter((f) => f.endsWith('.yaml')).map((f) => {
    const d = YAML.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
    return {
      id: d.id, title: d.title, text: fillOffices(d.text), from: toIso(d.from), to: d.to ? toIso(d.to) : undefined,
      office: d.office, tags: d.tags ?? [], status: d.status,
      options: (d.options ?? []).map((o: Option) => ({ id: o.id, text: fillOffices(o.text), historical: !!o.historical })),
      sources: (d.sources ?? []).map((s: Source & { checked?: unknown }) => ({ ...s, checked: s.checked ? toIso(s.checked) : undefined })),
    } as GameEvent;
  }).filter((e) => {
    if (e.status === 'draft') return false;
    const needsReview = e.tags.some((t) => sensitive.has(t));
    return !needsReview || e.status === 'sensitivity-reviewed';
  }).sort((a, b) => a.from.localeCompare(b.from) || a.id.localeCompare(b.id));
}

export interface EventPhoto { event: string; file: string; alt: string; credit: string; commons: string; licence: string }

/** The event-card photographs (archival, never AI) with their credit lines, from the game's own mapping. Only rows whose
 * sensitivity review is done when the photo is marked sensitive, as for a shareable build. */
export function eventPhotos(): Map<string, EventPhoto> {
  const doc = readYaml('unity/Assets/UI/EventImages/event-images.yaml');
  const rows = (doc.images as (EventPhoto & { sensitive?: boolean; sensitivity_reviewed?: boolean })[])
    .filter((r) => !r.sensitive || r.sensitivity_reviewed);
  return new Map(rows.map((r) => [r.event, r]));
}

export interface AssetRule { glob: string; licence: string; source: string; status: string; ai_generated?: boolean; note?: string }

export function assetRules(): AssetRule[] {
  return (readYaml('ASSETS.yaml').rules as AssetRule[]);
}

export const personCategories = ['team', 'historian', 'sensitivity-reviewer'] as const;
export type PersonCategory = (typeof personCategories)[number];

export interface Person {
  github: string;
  name: { en?: string; he?: string };
  title: { en: string; he: string };
  photo?: string | null;
  category: PersonCategory;
  since?: string;
  until?: string;
}

/** The file the contributors page reads. SITE_CONTRIBUTORS (a path from the repo root) swaps in a test fixture. */
export const contributorsFile = process.env.SITE_CONTRIBUTORS || 'site/src/data/contributors.yaml';

export function contributors(): Person[] {
  const doc = YAML.parse(fs.readFileSync(path.resolve(repoRoot, contributorsFile), 'utf8'));
  const people = (doc.people ?? []) as (Omit<Person, 'category' | 'since' | 'until'> & { category?: unknown; since?: unknown; until?: unknown })[];
  const year = /^\d{4}(-\d{2})?$/;
  return people.map((p) => {
    const bad = (msg: string) => { throw new Error(`contributors: ${p.github ?? '(no github)'}: ${msg}`); };
    if (!p.github) bad('no github handle');
    if (!p.title?.en || !p.title?.he) bad('title needs both en and he');
    const category = (p.category ?? 'team') as PersonCategory;
    if (!personCategories.includes(category)) bad(`category must be one of ${personCategories.join(', ')}`);
    // YAML reads a bare 2024 as a number; keep the text.
    const since = p.since == null || p.since === '' ? undefined : String(p.since);
    const until = p.until == null || p.until === '' ? undefined : String(p.until);
    for (const [k, v] of [['since', since], ['until', until]] as const) if (v && !year.test(v)) bad(`${k} must be YYYY or YYYY-MM, not ${v}`);
    if (since && until && until < since) bad('until is before since');
    return { ...p, category, since, until };
  });
}

/** The first `## Heading` section of a Markdown file, without the heading. */
export function markdownSection(rel: string, heading: string): string {
  const lines = read(rel).split('\n');
  const start = lines.findIndex((l) => l.trim() === `## ${heading}`);
  if (start < 0) return '';
  const end = lines.findIndex((l, i) => i > start && /^## /.test(l));
  return lines.slice(start + 1, end < 0 ? undefined : end).join('\n').trim();
}

// The roadmap table in README.md and README.he.md ("| 0 | what it adds | status | [#375](url) |"), read as it is, so the
// home page never says something the README does not. An unknown status word fails the build instead of guessing.
export type PhaseState = 'done' | 'now' | 'next' | 'later';
export interface Phase { n: string; what: { text: string; href?: string }[]; status: string; state: PhaseState; issue: string; url: string }
const STATES: [RegExp, PhaseState][] = [[/^(Done|הושלם)/, 'done'], [/^(In progress|בתהליך)/, 'now'], [/^(Next|הבא)/, 'next'], [/^(Later|בהמשך)/, 'later']];
export function roadmap(lang: 'en' | 'he'): Phase[] {
  const md = read(lang === 'he' ? 'README.he.md' : 'README.md');
  const rows = [...md.matchAll(/^\| (\d) \| (.+?) \| (.+?) \| \[(#\d+)\]\(([^)]+)\) \|$/gm)];
  if (rows.length < 8) throw new Error(`site: the roadmap table in ${lang === 'he' ? 'README.he.md' : 'README.md'} has ${rows.length} phase rows, expected 8`);
  return rows.map(([, n, what, status, issue, url]) => {
    const state = STATES.find(([re]) => re.test(status))?.[1];
    if (!state) throw new Error(`site: roadmap phase ${n} has an unknown status: ${status}`);
    // Markdown links in the cell become links; a repository path is resolved against the repository on GitHub.
    const parts = what.split(/(\[[^\]]+\]\([^)]+\))/).filter(Boolean).map((seg) => {
      const m = seg.match(/^\[([^\]]+)\]\(([^)]+)\)$/);
      return m ? { text: m[1], href: /^https?:/.test(m[2]) ? m[2] : `https://github.com/eyalgolan/statecraft-israel/blob/main/${m[2]}` } : { text: seg };
    });
    return { n, what: parts, status, state, issue, url };
  });
}

// The Cabinet page: everyone who held an office in the game's years, read from content/people.yaml, offices.yaml and
// parties.yaml as they are: terms from the state's founding on 14 May 1948 (the game starts the next day) to the end of
// 1951. Nothing here is written for the website: the tagline is built from the sourced offices and dates, never a quote
// (content/ holds no sourced quotes).
export interface Named { en: string; he?: string }
export interface Term { office: string; title: Named; party?: Named; from: string; to?: string; sources: Source[] }
/** A portrait the site may publish: only ASSETS.portraits.yaml rows marked verified (owner, 15 October 2026: the
 * owner-verify GPO photographs are hidden for now, decision D-P). `credit` is the attribution an open licence requires,
 * from NOTICE: the photographer and year, the collection, the licence with its link, and that the file was adapted. */
export interface Portrait {
  file: string; source: string; focus_y?: number;
  credit: { who: string; collection?: string; licence: string; licenceUrl?: string; adapted?: string };
}
export interface Person {
  id: string; name: string; name_he: string; born: string; died?: string; party?: Named;
  status: string; competence: number; loyalty: number; terms: Term[]; group: 'government' | 'state' | 'deputy';
  portrait?: Portrait; bioSources: Source[];
  /** The game has a portrait, but its licence row is not verified yet (owner-verify, D-P): the site holds it back. */
  portraitHeld: boolean;
}
const GAME_START = '1948-05-14';
const GAME_END = '1952-01-01';
const PORTRAIT_DIR = 'unity/Assets/UI/Portraits/';
// The Hebrew form of a person's office titles (#661): `title_he_form: feminine` in content/people.yaml, sourced under
// biography_sources.title_he_form, picks the office's feminine title (offices.yaml title_he_f, "שרת העבודה"); left out,
// the title is title_he. The sim's mirror is Office.TitleHeFor(Person?). Any other value fails the build.
const feminine = (p: Record<string, any>) => {
  const v = p.title_he_form;
  if (v === undefined || v === null) return false;
  if (v !== 'feminine') throw new Error(`site: ${p.id} has title_he_form ${JSON.stringify(v)}; the only value is "feminine"`);
  return true;
};

/** The CC BY 4.0 and CC BY-SA 4.0 portrait credits in NOTICE ("Portraits:" paragraph): each licence block names its
 * collection, and each file line its photographer, year and "cropped". */
function noticeCredits(): Map<string, { who: string; collection: string; licence: string; licenceUrl: string; adapted: string }> {
  const text = read('NOTICE');
  const start = text.indexOf('Portraits:');
  const end = text.indexOf('\n\n', start);
  if (start < 0 || end < 0) throw new Error('site: NOTICE has no "Portraits:" paragraph');
  const para = text.slice(start, end);
  const out = new Map<string, { who: string; collection: string; licence: string; licenceUrl: string; adapted: string }>();
  const blocks = [...para.matchAll(/(CC BY(?:-SA)? 4\.0) \((https:\/\/creativecommons\.org\/licenses\/[^)]+)\),([\s\S]*?)via Wikimedia(?:\s|\n)+Commons([\s\S]*?)(?=CC BY(?:-SA)? 4\.0 \(https|$)/g)];
  for (const [, licence, url, collection, body] of blocks) {
    for (const m of body.matchAll(/^- ([a-z_]+)\.jpg: (.+?), (CC BY(?:-SA)? 4\.0), cropped\.$/gm)) {
      if (m[3] !== licence) throw new Error(`site: NOTICE lists ${m[1]}.jpg as ${m[3]} under ${licence}`);
      out.set(m[1], { who: m[2], collection: collection.replace(/\s+/g, ' ').trim().replace(/,$/, ''), licence, licenceUrl: url, adapted: 'cropped and resized' });
    }
  }
  return out;
}

export function cabinet(): Person[] {
  const officeDoc = readYaml('content/offices.yaml').offices as { id: string; title: string; title_he?: string; title_he_f?: string }[];
  const officeOrder = new Map(officeDoc.map((o, i) => [o.id, i]));
  const office = new Map(officeDoc.map((o) => [o.id, o]));
  const parties = new Map((readYaml('content/parties.yaml').parties as { id: string; name: string; name_he?: string }[]).map((p) => [p.id, p]));
  const party = (id?: string): Named | undefined => {
    if (!id) return undefined;
    const x = parties.get(id);
    if (!x) throw new Error(`site: party ${id} is not in parties.yaml`);
    return { en: x.name, he: x.name_he };
  };
  const credits = new Map((readYaml('ASSETS.portraits.yaml').rules as { glob: string; licence: string; source: string; artist?: string; status: string }[]).map((r) => [r.glob, r]));
  const notice = noticeCredits();
  const people = readYaml('content/people.yaml').people as Record<string, any>[];
  const list: Person[] = [];
  for (const p of people) {
    if (p.status === 'draft') continue;
    const raw = (p.offices as Record<string, any>[])
      .map((o) => ({ office: o.office as string, party: o.party as string | undefined, from: toIso(o.from), to: o.to ? toIso(o.to) : undefined, sources: o.sources as Source[] }))
      .filter((o) => o.from < GAME_END && (!o.to || o.to > GAME_START))
      .sort((a, b) => a.from.localeCompare(b.from));
    if (!raw.length) continue;
    // Terms are split per government in content/; join a term to the one before it when the office runs on unbroken
    // for the same list (a term's own `party` is the list the holder sat for then; otherwise the person's applies).
    const terms: (Term & { partyId?: string })[] = [];
    for (const o of raw) {
      const partyId = o.party ?? p.party ?? undefined;
      const prev = [...terms].reverse().find((t) => t.office === o.office);
      if (prev && prev.to === o.from && prev.partyId === partyId) {
        prev.to = o.to;
        for (const s of o.sources) if (!prev.sources.some((x) => x.url === s.url)) prev.sources.push(s);
      } else {
        const off = office.get(o.office);
        if (!off) throw new Error(`site: ${p.id} holds an office that offices.yaml does not define: ${o.office}`);
        const he = feminine(p) && off.title_he_f ? off.title_he_f : off.title_he;
        terms.push({ office: o.office, title: { en: off.title, he }, partyId, party: party(partyId), from: o.from, to: o.to, sources: [...o.sources] });
      }
    }
    terms.sort((a, b) => (officeOrder.get(a.office)! - officeOrder.get(b.office)!) || a.from.localeCompare(b.from));
    const ids = terms.map((t) => t.office);
    const group = ids.some((i) => i === 'prime_minister' || (i.endsWith('_minister') && !i.startsWith('deputy_')))
      ? 'government' : ids.some((i) => i.startsWith('deputy_')) ? 'deputy' : 'state';
    let portrait: Portrait | undefined;
    let portraitHeld = false;
    if (p.portrait) {
      const file = `${PORTRAIT_DIR}${p.id}.jpg`;
      const row = credits.get(file);
      if (!row) throw new Error(`site: portrait ${file} has no row in ASSETS.portraits.yaml`);
      portraitHeld = row.status !== 'verified';
      if (row.status === 'verified') {
        const open = /^CC-BY/.test(row.licence);
        const n = notice.get(p.id);
        if (open && !n) throw new Error(`site: ${file} is ${row.licence} but NOTICE has no credit line for it`);
        portrait = {
          file, source: row.source, focus_y: p.portrait.focus_y,
          credit: n ? { who: n.who, collection: n.collection, licence: n.licence, licenceUrl: n.licenceUrl, adapted: n.adapted }
            : { who: row.artist ?? '', licence: row.licence === 'CC0-1.0' ? 'CC0' : 'Public domain' },
        };
      }
    }
    const bio = p.biography_sources ?? {};
    const bioSources: Source[] = [];
    for (const k of ['born', 'died', 'name_he', 'title_he_form']) for (const s of (bio[k] ?? []) as Source[]) if (!bioSources.some((x) => x.url === s.url)) bioSources.push(s);
    list.push({
      id: p.id, name: p.name, name_he: p.name_he, born: toIso(p.born), died: p.died ? toIso(p.died) : undefined,
      party: party(p.party || undefined), status: p.status, competence: p.competence, loyalty: p.loyalty,
      terms: terms.map(({ partyId: _x, ...t }) => t), group, bioSources, portrait, portraitHeld,
    });
  }
  const rank = (x: Person) => Math.min(...x.terms.map((t) => officeOrder.get(t.office)!));
  return list.sort((a, b) => rank(a) - rank(b) || a.terms[0].from.localeCompare(b.terms[0].from) || a.name.localeCompare(b.name));
}

