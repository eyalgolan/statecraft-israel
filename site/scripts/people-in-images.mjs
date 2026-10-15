// Which in-engine pictures the website may show, from site/src/data/people-in-images.yaml (checked by hand) and the
// provenance rows: a file is shown only if it is listed, every game portrait visible in it has a verified
// ASSETS.portraits.yaml row, every event photograph visible in it has a verified ASSETS.yaml row, and no visible portrait
// is unidentified. Used by src/lib/media.ts (what the pages show), press-kit.mjs (the screenshots zip) and
// check-site.mjs (nothing else reaches dist/).
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';

/** @returns {{ listed: Map<string, object>, why: (file: string) => string | null }} why() is null when the file may be shown. */
export function imageRules(repo, list = path.join(repo, 'site/src/data/people-in-images.yaml')) {
  const read = (p) => YAML.parse(fs.readFileSync(path.isAbsolute(p) ? p : path.join(repo, p), 'utf8'));
  const listed = new Map(Object.entries(read(list).images ?? {}));
  const status = new Map();
  for (const r of read('ASSETS.yaml').rules) status.set(r.glob, r.status);
  for (const r of read('ASSETS.portraits.yaml').rules) status.set(r.glob, r.status);
  const why = (file) => {
    const e = listed.get(file);
    if (!e) return `${file} is not in site/src/data/people-in-images.yaml (not checked by hand for the people in it)`;
    if (!e.checked) return `${file}: its people-in-images entry has no checked date`;
    if ((e.unidentified ?? 0) > 0) return `${file} shows ${e.unidentified} portrait(s) nobody has identified`;
    for (const id of e.portraits ?? []) {
      const s = status.get(`unity/Assets/UI/Portraits/${id}.jpg`);
      if (s !== 'verified') return `${file} shows the portrait of ${id}, whose ASSETS.portraits.yaml row is ${s ?? 'missing'}`;
    }
    for (const id of e.photos ?? []) {
      const s = status.get(`unity/Assets/UI/EventImages/${id}.jpg`);
      if (s !== 'verified') return `${file} shows the event photograph ${id}, whose ASSETS.yaml row is ${s ?? 'missing'}`;
    }
    return null;
  };
  return { listed, why };
}
