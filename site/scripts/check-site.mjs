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
//     are linked from the Press page in both languages; the logo download is the Steam kit wordmark byte for byte
//     and is linked there too.
//  9. the history approach page exists in both languages, links to the two documents it restates, and History links to it.
// 10. no page publishes a game portrait whose ASSETS.portraits.yaml row is not verified (the owner-verify GPO
//     photographs are hidden for now: owner, 15 October 2026, decision D-P), the Cabinet page exists in both languages,
//     and Golda Meir's Hebrew card uses the feminine title (title_he_form, #661);
// 11. no page shows an in-engine picture or video that site/src/data/people-in-images.yaml does not clear. A check cannot
//     see inside a screenshot, so that file records, checked by hand, the real people visible in each one; a screenshot
//     of a hidden portrait (the Cabinet screen) is held back here.
// Run after `astro build` (npm run build does both). Exit 1 with one line per problem.
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';
import { plan, zipEntries, zipRead, LOGO, CONCEPT_TAG, LIKENESS_TAG } from './press-kit.mjs';
import { imageRules } from './people-in-images.mjs';

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
    if (!text.includes('In-engine capture') || !text.includes('CC BY-SA 4.0')) problems.push(`downloads/${z.zip}: README.txt lacks the label or the licence`);
    for (const n of names) if (/concept|hero|social|trailer|cinematic|\.(mp4|webm|png)$/i.test(n)) problems.push(`downloads/${z.zip}: ${n} is not an in-engine screenshot`);
  }
  for (const lang of ['en', 'he']) {
    const page = path.join(dist, lang, 'press', 'index.html');
    if (!fs.existsSync(page) || !new RegExp(`href="[^"]*/downloads/${z.zip.replace(/\./g, '\\.')}"`).test(fs.readFileSync(page, 'utf8'))) {
      problems.push(`${lang}/press/index.html: no link to downloads/${z.zip}`);
    }
  }
}
// The press logo is the Steam kit wordmark itself, linked from both Press pages.
const logoBuilt = path.join(dist, 'downloads', LOGO.name);
if (!fs.existsSync(logoBuilt) || !fs.readFileSync(logoBuilt).equals(fs.readFileSync(path.join(path.resolve(dist, '..', '..'), LOGO.file)))) {
  problems.push(`downloads/${LOGO.name}: missing, or not the same bytes as ${LOGO.file}`);
}
for (const lang of ['en', 'he']) {
  const page = path.join(dist, lang, 'press', 'index.html');
  if (!fs.existsSync(page) || !new RegExp(`href="[^"]*/downloads/${LOGO.name.replace(/\./g, '\\.')}"`).test(fs.readFileSync(page, 'utf8'))) {
    problems.push(`${lang}/press/index.html: no link to downloads/${LOGO.name}`);
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
  // The two plates: a person whose game portrait is held back (its ASSETS.portraits.yaml row is not verified) gets
  // "Portrait awaiting a licence check"; a person with no portrait at all keeps "No public-domain portrait yet".
  {
    const peopleDoc = YAML.parse(fs.readFileSync(path.resolve(dist, '..', '..', 'content', 'people.yaml'), 'utf8')).people;
    const withPortrait = new Set(peopleDoc.filter((x) => x.portrait).map((x) => x.id));
    const held = new Set(hidden);
    const texts = { en: { held: 'Portrait awaiting a licence check', none: 'No public-domain portrait yet' }, he: { held: 'הצילום ממתין לבדיקת רישיון', none: 'אין עדיין צילום בנחלת הכלל' } };
    let seen = { held: 0, none: 0 };
    for (const lang of ['en', 'he']) {
      const page = path.join(dist, lang, 'cabinet', 'index.html');
      if (!fs.existsSync(page)) continue;
      const html = fs.readFileSync(page, 'utf8');
      for (const m of html.matchAll(/<article class="minister"[^>]*\sid="([a-z_]+)"[\s\S]*?<\/article>/g)) {
        const [card, id] = m;
        const plate = card.match(/data-plate="(\w+)"/)?.[1];
        const want = held.has(id) ? 'held' : withPortrait.has(id) ? null : 'none';
        if (plate !== (want ?? undefined)) { fail(page, `${id}: plate ${plate ?? 'none shown'}, expected ${want ?? 'the portrait'}`); continue; }
        if (want && !card.includes(texts[lang][want])) fail(page, `${id}: the ${want} plate does not say "${texts[lang][want]}"`);
        if (want) seen[want]++;
      }
    }
    if (held.size && !seen.held) problems.push('cabinet: no "awaiting a licence check" plate found, though some portraits are held back');
    if (!seen.none) problems.push('cabinet: no "no portrait yet" plate found');
  }
  // The feminine Hebrew title (#661, title_he_form: feminine): Golda Meir's card reads שרת העבודה, never שר העבודה.
  const heCab = path.join(dist, 'he', 'cabinet', 'index.html');
  if (fs.existsSync(heCab)) {
    const html = fs.readFileSync(heCab, 'utf8');
    const at = html.indexOf('id="golda_meir"');
    const card = at < 0 ? '' : html.slice(at, html.indexOf('</article>', at));
    if (!card) fail(heCab, 'no card for golda_meir');
    else if (!card.includes('שרת העבודה') || /שר העבודה/.test(card)) fail(heCab, 'Golda Meir\'s card does not read שרת העבודה (title_he_form: feminine)');
  }
}

// 11. In-engine pictures and videos: every file in docs/images/ (the README's screenshots, the hero, the gameplay clip)
// and every file people-in-images.yaml lists must be cleared before a page names it. A built file keeps its source name
// (/_astro/<stem>.<hash>...), and a public/ file keeps its path.
{
  const repo = path.resolve(dist, '..', '..');
  const people = imageRules(repo, process.env.SITE_PEOPLE_IN_IMAGES);
  const candidates = new Set([...people.listed.keys()]);
  for (const n of fs.readdirSync(path.join(repo, 'docs/images'))) if (/\.(jpe?g|png|webp|mp4|webm)$/.test(n)) candidates.add(`docs/images/${n}`);
  const blocked = [...candidates].map((f) => [f, people.why(f)]).filter(([, w]) => w);
  for (const f of pages) {
    const html = fs.readFileSync(f, 'utf8');
    for (const [file, why] of blocked) {
      const base = path.basename(file);
      const stem = base.replace(/\.[^.]+$/, '');
      const esc = (x) => x.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      // A picture is re-encoded (any image extension); a video keeps its own extension.
      const ext = /\.(mp4|webm)$/.test(base) ? base.split('.').pop() : '(?:avif|webp|jpe?g|png)';
      const built = new RegExp(`/_astro/${esc(stem)}\\.[A-Za-z0-9_-]+\\.${ext}(?![a-z0-9])`);
      const direct = file.startsWith('site/public/') ? html.includes(`/${file.slice('site/public/'.length)}`) : false;
      if (built.test(html) || direct) fail(f, `shows ${file}: ${why}`);
    }
  }
}

if (problems.length) {
  console.error(problems.join('\n'));
  console.error(`check-site: ${problems.length} problem(s)`);
  process.exit(1);
}
console.log(`check-site: ${pages.length} pages OK (both languages, labels on every image and video, nothing from other origins, devlog RSS feeds, contributors groups, press zips, history approach page, no unverified portraits, cabinet page, in-engine pictures cleared for the people in them)`);
