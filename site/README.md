# The Statecraft: Israel website

A static site built with [Astro](https://astro.build), in English (`/en/`) and Hebrew (`/he/`, right to left). It is meant for GitHub Pages and is not deployed yet (issue #393).

## Build and preview

You need Node 22.12 or later.

```
cd site
npm ci --ignore-scripts
npm run build      # astro build, then scripts/check-site.mjs
npm run preview    # http://localhost:4321/statecraft-israel/en/
```

`npm run build` fails when a page exists in one language only, when an image or video in a page has no media label, or when anything would load from another site.

## Where things are

- `src/pages/[lang]/`: one file per page; each renders both languages.
- `src/i18n/ui.ts`: interface strings in both languages. The build fails if a Hebrew string is missing.
- `src/content/<lang>/*.md`: longer page text. `src/content/devlog/<lang>/`: devlog posts, the same file name in both languages.
- `src/lib/media.ts`: every image and video the site shows, with its label. The images are read from `docs/images/` and `promo/final/` and converted to AVIF and WebP at build time; they are not copied into `site/`.
- `docs/images/captures.yaml`: the in-engine screenshots. The site's in-engine gallery is the README's `docs/images/build-*.jpg`, described by this file (build, date, title and alt text in both languages). Its `hero:` entry is the home page's hero still, `docs/images/hero-engine-*.jpg`: a clean in-engine render with no interface, with its own build and date (without the entry, the first screenshot is the hero). The build fails if a picture has no entry or an entry has no file.
- `src/lib/repo.ts`: reads `content/events/`, `ASSETS.yaml` and the event photographs' mapping at build time. The History and Sources pages show that content as it is. It also reads the Roadmap table in `README.md` and `README.he.md` for the home page; the build fails if a phase row is missing or has a status it does not know. The Cabinet page reads `content/people.yaml`, `offices.yaml`, `parties.yaml`, the game's portraits in `unity/Assets/UI/Portraits/` and their credits in `ASSETS.portraits.yaml`; it shows no quote, because `content/` holds none.
- `scripts/press-kit.mjs`: the Press page's two downloads, written to `public/downloads/` (git-ignored) at the start of every build and never committed: a zip of the in-engine screenshots from `captures.yaml`, and a zip of the AI concept art in `promo/final/` as full-size JPEG (quality 90), with "AI concept art" in every file name and "AI likeness, not a photograph" in the two with likenesses of real ministers. Each holds a README.txt with the label, licence and provenance of every image, taken from `ASSETS.yaml`.
- `src/data/people-in-images.yaml`: every in-engine picture and video the site shows, with the real people visible in it, checked by hand (a check cannot see inside a screenshot). A file is shown only if it is listed and every portrait and event photograph in it has a `verified` provenance row; `npm run build` (media.ts, press-kit.mjs and check-site) fails otherwise. The Cabinet screenshot is held back this way while its owner-verify portraits are hidden (decision D-P).
- `src/data/contributors.yaml`: the Contributors grid. Photos go in `src/assets/contributors/`, each with an `ASSETS.yaml` row and the person's consent. Never a GitHub avatar, never a generated image of a person.

## The gameplay clip

A real gameplay clip in `docs/images/build-*.mp4` and `.webm`, listed under `video:` in `docs/images/captures.yaml`, is the home page's first video: a click-to-play poster under the hero (no video bytes before the click), labelled as an in-engine capture with its build and date, and the first video in the Media page's in-engine gallery. The hero itself is a still, so the first screen loads no video. Until the clip exists, that section shows the M2 flythrough.

## Refreshing the screenshots

The site always shows the README's screenshots. To refresh both, retake `docs/images/build-*.jpg` (same file names), then set `build` and `date` in `docs/images/captures.yaml`; the labels read "In-engine capture — <build>, <date>". A new screenshot needs an entry with its alt text in both languages.

Visual changes are about to land: new towns with a much better Tel Aviv (#339), warmer land (#392) and the tilt-shift change that keeps far towns readable (#390). Once they are merged, the README screenshots get retaken once and the site picks them up through `captures.yaml`, with no change under `site/`.

## Labels

Every image and video carries one of these labels where it appears, in both languages:

- In-engine capture — the build and date;
- Concept art — AI-generated;
- Cinematic promo — AI-generated, not gameplay;
- Archival photograph, with its credit.

The first image and the first video on the home page are in-engine captures (the build check enforces it). The AI cinematic trailer appears only in its own labelled section on Home, after the gameplay, and in the Media page's cinematic gallery, behind a click and never with sound by itself.

## Deploying

`.github/workflows/site.yml` builds every pull request that touches the site or what it reads. Deploying from `main` needs two owner steps: enable GitHub Pages with "GitHub Actions" as the source, and set the repository variable `SITE_DEPLOY` to `true`.

The site lives at `https://eyalgolan.github.io/statecraft-israel/` until there is a domain. For a custom domain: add `public/CNAME` with the domain, set the repository variables `SITE_URL` to `https://<domain>` and `SITE_BASE` to `/`, and set the domain in the Pages settings. No page needs to change: every link and hreflang URL is built from those two values.

## Hosting the game here

GitHub Pages cannot set response headers, so the browser build must use Unity's Decompression Fallback and stay single-threaded. The Play page explains this; today `Build.Web` turns the fallback off.
