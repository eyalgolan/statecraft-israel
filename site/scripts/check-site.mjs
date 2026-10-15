// Checks the built site (dist/) for the rules a page template could break without anyone noticing:
//  1. every page exists in both languages (/en/x/ and /he/x/);
//  2. each page declares its language and direction on <html>, and lists itself, its counterpart and x-default;
//  3. every image and video in the page body sits in a <figure> that carries a visible media label
//     (In-engine capture, Concept art — AI-generated, Cinematic promo — AI-generated, not gameplay, or Archival
//     photograph), except contributor photos, which are portraits supplied by the owner;
//  4. nothing loads from another origin: no external script, stylesheet, font, image, video or frame;
//  5. every file under _astro/ that a page names exists;
//  6. both devlog RSS feeds exist, are well-formed, list every post, and the devlog page links to them;
//  7. the contributors page puts each person from contributors.yaml in their group (team, historians and sensitivity
//     reviewers, past contributors, with years), and leaves out a group with no one in it. SITE_CONTRIBUTORS, as in
//     the build, points at a fixture instead (site/scripts/fixtures/contributors.yaml has one of each).
//  8. the Press page's two zips (scripts/press-kit.mjs) exist, hold exactly the expected files and a README that names
//     each of them, keep concept art out of the screenshots, carry "AI concept art" in every concept file name
//     (and "AI likeness, not a photograph" in 04 and 08, which the README's first line names), and
//     are linked from the Press page in both languages.
//  9. the history approach page exists in both languages, links to the two documents it restates, and History links to it.
// 10. no page publishes a game portrait whose ASSETS.portraits.yaml row is not verified (the owner-verify GPO
//     photographs are hidden for now: owner, 15 October 2026, decision D-P), and the Cabinet page exists in both languages.
// Run after `astro build` (npm run build does both). Exit 1 with one line per problem.
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';
import { plan, zipEntries, zipRead, CONCEPT_TAG, LIKENESS_TAG } from './press-kit.mjs';

const dist = path.resolve(process.argv[2] ?? 'dist');
const problems = [];
const fail = (file, msg) => problems.push(`${path.relative(dist, file)}: ${msg}`);

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((d) => {
    const p = path.join(dir, d.name);
    return d.isDirectory() ? walk(p) : p.endsWith('.html') ? [p] : [];
  });
}

const pages = walk(dist);
const rel = (f) => path.relative(dist, f).split(path.sep).join('/');
const byLang = { en: new Set(), he: new Set() };
for (const f of pages) {
  const r = rel(f);
  const m = r.match(/^(en|he)\/(.*)$/);
  if (m) byLang[m[1]].add(m[2]);
}
for (const [a, b] of [['en', 'he'], ['he', 'en']]) {
  for (const p of byLang[a]) if (!byLang[b].has(p)) problems.push(`${a}/${p}: no ${b}/${p}`);
}
if (byLang.en.size === 0) problems.push('no /en/ pages were built');

const external = /^(?:https?:)?\/\//i;
for (const f of pages) {
  const html = fs.readFileSync(f, 'utf8');
  const r = rel(f);
  const lang = r.match(/^(en|he)\//)?.[1];
  if (lang) {
    const tag = html.match(/<html[^>]*>/)?.[0] ?? '';
    if (!tag.includes(`lang="${lang}"`)) fail(f, `<html> lacks lang="${lang}"`);
    if (!tag.includes(`dir="${lang === 'he' ? 'rtl' : 'ltr'}"`)) fail(f, '<html> has the wrong dir');
    for (const h of ['en', 'he', 'x-default']) {
      if (!new RegExp(`<link rel="alternate" hreflang="${h}" href="https?://`).test(html)) fail(f, `missing absolute hreflang ${h}`);
    }
  }

  // 4. Resources from other origins. Links (<a href>) are fine: they load nothing until followed.
  const resource = [
    ...html.matchAll(/<script[^>]*\ssrc="([^"]+)"/g), ...html.matchAll(/<link[^>]*rel="(?:stylesheet|preload|icon|modulepreload|manifest)"[^>]*href="([^"]+)"/g),
    ...html.matchAll(/<(?:img|source|video|audio|track|iframe|embed)[^>]*\ssrc="([^"]+)"/g),
    ...html.matchAll(/\ssrcset="([^"]+)"/g), ...html.matchAll(/\simagesrcset="([^"]+)"/g), ...html.matchAll(/url\(['"]?([^'")]+)/g),
    ...html.matchAll(/<iframe/g),
  ];
  for (const m of resource) {
    if (m[0] === '<iframe') { fail(f, 'contains an <iframe>'); continue; }
    for (const u of m[1].split(',').map((s) => s.trim().split(/\s+/)[0])) {
      if (external.test(u)) fail(f, `loads from another origin: ${u}`);
    }
  }

  // 5. Every built asset a page names exists (the prune step must never remove a file in use).
  for (const m of html.matchAll(/\/_astro\/([^"'\s,)&]+)/g)) {
    if (!fs.existsSync(path.join(dist, '_astro', m[1]))) fail(f, `names a missing file: _astro/${m[1]}`);
  }

  // 3. Every image and video in <main> carries a media label in its <figure>.
  const main = html.match(/<main[\s\S]*<\/main>/)?.[0] ?? '';
  for (const m of main.matchAll(/<(img|video)\b[^>]*>/g)) {
    if (/class="[^"]*\bportrait\b/.test(m[0])) continue; // contributor photo
    const before = main.lastIndexOf('<figure', m.index);
    const closeBefore = main.lastIndexOf('</figure>', m.index);
    const after = main.indexOf('</figure>', m.index);
    if (before < 0 || closeBefore > before || after < 0) { fail(f, `<${m[1]}> outside a <figure>: ${m[0].slice(0, 90)}`); continue; }
    const figure = main.slice(before, after);
    if (!/data-media-label="(engine|concept|cinematic|archival)"/.test(figure)) fail(f, `<${m[1]}> without a media label: ${m[0].slice(0, 90)}`);
  }
  // The facade's video, made on click, takes the place of a button inside a labelled figure; check the button too.
  for (const m of main.matchAll(/<button[^>]*class="facade"[^>]*>/g)) {
    const before = main.lastIndexOf('<figure', m.index);
    const figure = main.slice(before, main.indexOf('</figure>', m.index));
    if (!/data-media-label=/.test(figure)) fail(f, 'video facade without a media label');
  }
}

// The hero and the first video on the home page are real gameplay.
for (const lang of ['en', 'he']) {
  const f = path.join(dist, lang, 'index.html');
  if (!fs.existsSync(f)) continue;
  const main = fs.readFileSync(f, 'utf8').match(/<main[\s\S]*<\/main>/)?.[0] ?? '';
  const firstLabel = main.match(/data-media-label="(\w+)"/)?.[1];
  if (firstLabel !== 'engine') fail(f, `the first image is labelled ${firstLabel}, not an in-engine capture`);
  // The first video (a <video> or a click-to-play facade) sits in a figure labelled as an in-engine capture.
  const v = main.search(/<video\b|class="facade"/);
  if (v >= 0) {
    const fig = main.slice(main.lastIndexOf('<figure', v), main.indexOf('</figure>', v));
    const kind = fig.match(/data-media-label="(\w+)"/)?.[1];
    if (kind !== 'engine') fail(f, `the first video is labelled ${kind}, not an in-engine capture`);
  }
}

// 6. Each devlog has an RSS feed that is well-formed, lists every post, and is linked from the devlog page and its <head>.
function xmlProblem(xml) {
  const stack = [];
  const body = xml.replace(/^<\?xml[^?]*\?>/, '').replace(/<!\[CDATA\[[\s\S]*?\]\]>/g, '');
  if (/&(?!(?:amp|lt|gt|quot|apos|#\d+|#x[0-9a-f]+);)/i.test(body)) return 'a bare &';
  for (const m of body.matchAll(/<(\/?)([A-Za-z][\w:.-]*)((?:"[^"]*"|'[^']*'|[^'">])*?)(\/?)>|<|>/g)) {
    if (m[0] === '<') return 'a stray <';
    if (m[0] === '>') continue;
    if (m[4]) continue;
    if (!m[1]) stack.push(m[2]);
    else if (stack.pop() !== m[2]) return `</${m[2]}> closes the wrong element`;
  }
  return stack.length ? `<${stack.at(-1)}> is not closed` : (/^\s*<rss[\s>]/.test(body) ? null : 'no <rss> root');
}
for (const lang of ['en', 'he']) {
  const feed = path.join(dist, lang, 'devlog', 'rss.xml');
  if (!fs.existsSync(feed)) { problems.push(`${lang}/devlog/rss.xml: missing`); continue; }
  const xml = fs.readFileSync(feed, 'utf8');
  const bad = xmlProblem(xml);
  if (bad) fail(feed, `not well-formed: ${bad}`);
  if (!xml.includes(`<language>${lang}</language>`)) fail(feed, `no <language>${lang}</language>`);
  const links = [...xml.matchAll(/<item>[\s\S]*?<link>([^<]+)<\/link>/g)].map((m) => m[1]);
  if (links.some((u) => !/^https?:\/\//.test(u))) fail(feed, 'an item link is not absolute');
  const src = path.resolve(dist, '..', 'src', 'content', 'devlog', lang);
  for (const md of fs.readdirSync(src).filter((n) => n.endsWith('.md'))) {
    if (!links.some((u) => u.endsWith(`#${md.slice(0, -3)}`))) fail(feed, `does not list ${md}`);
  }
  const page = path.join(dist, lang, 'devlog', 'index.html');
  const html = fs.existsSync(page) ? fs.readFileSync(page, 'utf8') : '';
  if (!/<link rel="alternate" type="application\/rss\+xml"[^>]*href="https?:\/\/[^"]*\/devlog\/rss\.xml"/.test(html)) fail(page, 'no RSS <link> in <head>');
  if (!/<main[\s\S]*<a href="[^"]*\/devlog\/rss\.xml"[\s\S]*<\/main>/.test(html)) fail(page, 'no link to the RSS feed');
}

// 7. The contributors page: everyone in the right group, no empty group, years on every past entry.
{
  const file = path.resolve(import.meta.dirname, '../..', process.env.SITE_CONTRIBUTORS || 'site/src/data/contributors.yaml');
  const people = YAML.parse(fs.readFileSync(file, 'utf8')).people ?? [];
  const group = (p) => (p.until != null && p.until !== '' ? 'past' : (p.category ?? 'team') === 'team' ? 'team' : 'reviewers');
  for (const lang of ['en', 'he']) {
    const f = path.join(dist, lang, 'contributors', 'index.html');
    if (!fs.existsSync(f)) { problems.push(`${lang}/contributors/: missing`); continue; }
    const html = fs.readFileSync(f, 'utf8');
    for (const g of ['team', 'reviewers', 'past']) {
      const want = people.filter((p) => group(p) === g).map((p) => p.github).sort();
      const at = html.indexOf(`data-people="${g}"`);
      if (!want.length) { if (at >= 0) fail(f, `shows an empty ${g} section`); continue; }
      if (at < 0) { fail(f, `no ${g} section for ${want.join(', ')}`); continue; }
      const section = html.slice(at, html.indexOf('</section>', at));
      const got = [...section.matchAll(/class="gh"[^>]*>@([^<]+)</g)].map((m) => m[1]).sort();
      if (got.join() !== want.join()) fail(f, `${g} section lists ${got.join(', ') || 'no one'}, expected ${want.join(', ')}`);
      if (g !== 'team' && !/<h2[^>]*>[^<]+<\/h2>/.test(section)) fail(f, `${g} section has no heading`);
      if (g === 'past' && (section.match(/class="tenure"/g) ?? []).length !== want.length) fail(f, 'a past contributor without years');
    }
  }
}

// 8. The press downloads.
const [shotsZip, artZip] = plan();
for (const z of [shotsZip, artZip]) {
  const file = path.join(dist, 'downloads', z.zip);
  if (!fs.existsSync(file) || fs.statSync(file).size === 0) { problems.push(`downloads/${z.zip}: missing or empty`); continue; }
  const buf = fs.readFileSync(file);
  let entries;
  try { entries = zipEntries(buf); } catch (e) { problems.push(`downloads/${z.zip}: not a readable zip (${e.message})`); continue; }
  const names = entries.map((e) => e.name);
  const want = [`${z.dir}/README.txt`, ...z.entries.map((e) => `${z.dir}/${e.name}`)].sort();
  if (JSON.stringify([...names].sort()) !== JSON.stringify(want)) problems.push(`downloads/${z.zip}: holds ${names.join(', ')}; expected ${want.join(', ')}`);
  if (z.entries.length === 0) problems.push(`downloads/${z.zip}: no images`);
  for (const e of entries) if (e.size === 0) problems.push(`downloads/${z.zip}: ${e.name} is empty`);
  const readme = entries.find((e) => e.name.endsWith('/README.txt'));
  const text = readme ? zipRead(buf, readme).toString('utf8') : '';
  for (const e of z.entries) if (!text.includes(e.name)) problems.push(`downloads/${z.zip}: README.txt does not list ${e.name}`);
  if (z === artZip) {
    if (!text.includes('AI-generated concept art')) problems.push(`downloads/${z.zip}: README.txt does not say the images are AI-generated concept art`);
    for (const e of z.entries) if (!e.name.includes(CONCEPT_TAG)) problems.push(`downloads/${z.zip}: ${e.name} lacks "${CONCEPT_TAG}" in its name`);
    // The two images with AI likenesses of real ministers say so in the README's first line and in their file names.
    const first = text.split('\n')[0];
    for (const stem of ['04-cabinet-1948', '08-defense-1955']) {
      const e = z.entries.find((x) => x.name.includes(stem));
      if (!e || !e.name.includes(LIKENESS_TAG)) problems.push(`downloads/${z.zip}: ${stem} lacks "${LIKENESS_TAG}" in its name`);
      if (!first.includes(stem)) problems.push(`downloads/${z.zip}: README.txt's first line does not name ${stem}`);
    }
    if (!/AI likeness/.test(first) || !/not photographs?/.test(first)) problems.push(`downloads/${z.zip}: README.txt's first line does not say the likenesses are not photographs`);
  } else {
    if (!text.includes('In-engine capture') || !text.includes('CC BY 4.0')) problems.push(`downloads/${z.zip}: README.txt lacks the label or the licence`);
    for (const n of names) if (/concept|hero|social|trailer|cinematic|\.(mp4|webm|png)$/i.test(n)) problems.push(`downloads/${z.zip}: ${n} is not an in-engine screenshot`);
  }
  for (const lang of ['en', 'he']) {
    const page = path.join(dist, lang, 'press', 'index.html');
    if (!fs.existsSync(page) || !new RegExp(`href="[^"]*/downloads/${z.zip.replace(/\./g, '\\.')}"`).test(fs.readFileSync(page, 'utf8'))) {
      problems.push(`${lang}/press/index.html: no link to downloads/${z.zip}`);
    }
  }
}

// 9. The history approach page (#414) exists in both languages, the History page links to it, and it links to the two
// documents it restates.
for (const lang of ['en', 'he']) {
  const page = path.join(dist, lang, 'history', 'approach', 'index.html');
  if (!fs.existsSync(page)) { problems.push(`${lang}/history/approach/: missing`); continue; }
  const html = fs.readFileSync(page, 'utf8');
  for (const doc of ['docs/CONTENT_POLICY.md', 'docs/GDD.md']) if (!html.includes(doc)) fail(page, `no link to ${doc}`);
  const index = path.join(dist, lang, 'history', 'index.html');
  if (!fs.existsSync(index) || !fs.readFileSync(index, 'utf8').includes(`/${lang}/history/approach/"`)) fail(index, 'no link to history/approach/');
}

// 10. Unverified portraits are never published. A built image keeps its source name (kaplan.<hash>_<hash>.avif), so
// a page naming /_astro/<id>.<...> for an unverified <id>.jpg publishes it. SITE_PORTRAITS points at a fixture instead.
{
  const rows = YAML.parse(fs.readFileSync(process.env.SITE_PORTRAITS ?? path.resolve(dist, '..', '..', 'ASSETS.portraits.yaml'), 'utf8')).rules ?? [];
  const dir = 'unity/Assets/UI/Portraits/';
  const hidden = rows.filter((r) => r.glob.startsWith(dir) && r.status !== 'verified').map((r) => r.glob.slice(dir.length).replace(/\.jpg$/, ''));
  if (!rows.some((r) => r.glob.startsWith(dir))) problems.push('ASSETS.portraits.yaml: no game portrait rows found');
  for (const f of pages) {
    const html = fs.readFileSync(f, 'utf8');
    for (const id of hidden) if (new RegExp(`/_astro/${id}\\.[A-Za-z0-9_-]+\\.(?:avif|webp|jpe?g|png)`).test(html)) fail(f, `publishes the unverified portrait ${id}.jpg (ASSETS.portraits.yaml status is not verified)`);
  }
  for (const lang of ['en', 'he']) if (!fs.existsSync(path.join(dist, lang, 'cabinet', 'index.html'))) problems.push(`${lang}/cabinet/index.html: missing`);
}

if (problems.length) {
  console.error(problems.join('\n'));
  console.error(`check-site: ${problems.length} problem(s)`);
  process.exit(1);
}
console.log(`check-site: ${pages.length} pages OK (both languages, labels on every image and video, nothing from other origins, devlog RSS feeds, contributors groups, press zips, history approach page, no unverified portraits, cabinet page)`);
