// Interface strings for both languages. Every key must exist in both; the check at the bottom fails the build otherwise.
// Long page text lives in src/content/<lang>/*.md and in the page files, side by side for both languages.

export const languages = ['en', 'he'] as const;
export type Lang = (typeof languages)[number];

export const pages = [
  '', 'play', 'history', 'history/sources', 'history/approach', 'cabinet', 'media', 'devlog', 'contributors', 'how-we-make-it', 'press',
  'contribute', 'privacy',
] as const;
export type Page = (typeof pages)[number];

const en = {
  'site.name': 'Statecraft: Israel',
  'site.tagline': 'A turn-based strategy game about governing Israel from 15 May 1948. Open source, with a source for every historical fact.',
  'lang.other': 'עברית',
  'lang.other.label': 'Hebrew version of this page',
  'skip': 'Skip to content',
  'nav.label': 'Main',
  'nav.menu': 'Menu',
  'nav.home': 'Home',
  'nav.play': 'Play',
  'nav.history': 'History',
  'nav.sources': 'Sources',
  'nav.approach': 'Our approach',
  'nav.media': 'Media',
  'nav.cabinet': 'Cabinet',
  'nav.devlog': 'Devlog',
  'nav.contributors': 'Contributors',
  'nav.how': 'How we make it',
  'nav.press': 'Press',
  'nav.contribute': 'Contribute',
  'nav.privacy': 'Privacy',
  'footer.more': 'More',
  'footer.repo': 'Source code on GitHub',
  'footer.licence': 'Code MIT. Art and docs CC BY 4.0. Game content CC BY-SA 4.0. AI-generated images CC0.',
  'footer.notaffiliated': 'An independent open-source project. Not affiliated with or endorsed by Firaxis, 2K, the State of Israel or any person depicted.',
  'footer.privacy': 'No analytics and no cookies.',

  'label.engine': 'In-engine capture',
  'label.gameplay': 'Real gameplay',
  'label.concept': 'Concept art — AI-generated',
  'label.concept.target': 'A visual target, not the current build',
  'label.cinematic': 'Cinematic promo — AI-generated, not gameplay',
  'label.archival': 'Archival photograph',
  'label.likeness': 'Shows AI likenesses of real people. Not a photograph.',


  'video.play': 'Play video',
  'video.pause': 'Pause',
  'video.resume': 'Play',
  'hero.video': 'Real gameplay, looping without sound',
  'video.nosound': 'No sound',
  'video.loads': 'The video loads from this site when you press play.',
  'video.length': 'length',

  'cta.play': 'Play',
  'cta.github': 'GitHub',

  'figure.full': 'Full size',
  'date.window': 'In the game from',
  'history.choice': 'The decision in the game',
  'history.historical': 'What happened',
  'history.sources': 'Sources',
  'history.status': 'Review status',
  'history.status.fact-checked': 'fact-checked',
  'history.status.sensitivity-reviewed': 'fact-checked and sensitivity-reviewed',
  'history.en-only': 'The game’s text, which exists in English only for now.',
  'contributors.photo.missing': 'No photo yet',
  'contributors.github': 'GitHub profile',
} as const;

type Dict = Record<keyof typeof en, string>;

const he: Dict = {
  'site.name': 'Statecraft: Israel',
  'site.tagline': 'משחק אסטרטגיה מבוסס־תורות על ניהול מדינת ישראל מ־15 במאי 1948. קוד פתוח, ולכל עובדה היסטורית יש מקור.',
  'lang.other': 'English',
  'lang.other.label': 'הגרסה האנגלית של העמוד',
  'skip': 'דילוג לתוכן',
  'nav.label': 'ראשי',
  'nav.menu': 'תפריט',
  'nav.home': 'בית',
  'nav.play': 'לשחק',
  'nav.history': 'היסטוריה',
  'nav.sources': 'מקורות',
  'nav.approach': 'הגישה שלנו',
  'nav.media': 'מדיה',
  'nav.cabinet': 'הממשלה',
  'nav.devlog': 'יומן פיתוח',
  'nav.contributors': 'תורמים',
  'nav.how': 'איך המשחק נעשה',
  'nav.press': 'לעיתונות',
  'nav.contribute': 'איך משתתפים',
  'nav.privacy': 'פרטיות',
  'footer.more': 'עוד',
  'footer.repo': 'קוד המקור ב־GitHub',
  'footer.licence': 'קוד: MIT. אמנות ומסמכים: CC BY 4.0. תוכן המשחק: CC BY-SA 4.0. תמונות שנוצרו בבינה מלאכותית: CC0.',
  'footer.notaffiliated': 'פרויקט קוד פתוח עצמאי. אין לו קשר ל־Firaxis, ל־2K, למדינת ישראל או לאדם כלשהו המופיע בו, והם אינם תומכים בו.',
  'footer.privacy': 'בלי כלי אנליטיקה ובלי עוגיות.',

  'label.engine': 'צילום מתוך המשחק',
  'label.gameplay': 'צילום של משחק אמיתי',
  'label.concept': 'אמנות קונספט — נוצרה בבינה מלאכותית',
  'label.concept.target': 'יעד חזותי, לא הגרסה הנוכחית',
  'label.cinematic': 'פרומו קולנועי — נוצר בבינה מלאכותית, אינו צילום מהמשחק',
  'label.archival': 'צילום ארכיון',
  'label.likeness': 'מופיעות בה דמויות של אנשים אמיתיים שנוצרו בבינה מלאכותית. זה אינו צילום.',


  'video.play': 'הפעלת הווידאו',
  'video.pause': 'השהיה',
  'video.resume': 'הפעלה',
  'hero.video': 'משחק אמיתי, בלולאה וללא קול',
  'video.nosound': 'ללא קול',
  'video.loads': 'הווידאו נטען מהאתר הזה רק כשלוחצים על הפעלה.',
  'video.length': 'אורך',

  'cta.play': 'לשחק',
  'cta.github': 'GitHub',

  'figure.full': 'בגודל מלא',
  'date.window': 'במשחק מ־',
  'history.choice': 'ההחלטה במשחק',
  'history.historical': 'מה שקרה בפועל',
  'history.sources': 'מקורות',
  'history.status': 'מצב הבדיקה',
  'history.status.fact-checked': 'נבדק מול המקורות',
  'history.status.sensitivity-reviewed': 'נבדק מול המקורות ועבר בדיקת רגישות',
  'history.en-only': 'הטקסט מתוך המשחק, שקיים כרגע באנגלית בלבד.',
  'contributors.photo.missing': 'עדיין אין תמונה',
  'contributors.github': 'הפרופיל ב־GitHub',
};

export const ui: Record<Lang, Dict> = { en, he };

// Fail the build if a Hebrew string is missing or left empty where the English has text.
for (const key of Object.keys(en) as (keyof typeof en)[]) {
  if (en[key] && !he[key]) throw new Error(`i18n: Hebrew string missing for ${key}`);
}

export function t(lang: Lang, key: keyof typeof en): string {
  return ui[lang][key];
}

export const dir = (lang: Lang) => (lang === 'he' ? 'rtl' : 'ltr');
export const otherLang = (lang: Lang): Lang => (lang === 'en' ? 'he' : 'en');
export const ogLocale = (lang: Lang) => (lang === 'he' ? 'he_IL' : 'en_US');

const base = import.meta.env.BASE_URL.replace(/\/?$/, '/');

/** A site path with the base, for example href('he', 'media') -> /statecraft-israel/he/media/ */
export function href(lang: Lang | null, page: Page | string = ''): string {
  const parts = [lang, page].filter(Boolean).join('/');
  return parts ? `${base}${parts}/` : base;
}

/** A file under public/ with the base. */
export const asset = (p: string) => `${base}${p.replace(/^\//, '')}`;

/** The pages' content security policy: nothing loads from another origin. */
export const CSP = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; media-src 'self'; font-src 'self'; connect-src 'self'; frame-src 'none'; object-src 'none'; base-uri 'self'; form-action 'none'";

export const REPO = 'https://github.com/eyalgolan/statecraft-israel';
export const repoFile = (p: string) => `${REPO}/blob/main/${p}`;

const MONTHS_HE = ['בינואר', 'בפברואר', 'במרץ', 'באפריל', 'במאי', 'ביוני', 'ביולי', 'באוגוסט', 'בספטמבר', 'באוקטובר', 'בנובמבר', 'בדצמבר'];
const MONTHS_EN = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

/** "15 May 1948" / "15 במאי 1948", the repo's date style (AGENTS.md). */
export function formatDate(lang: Lang, iso: string | Date): string {
  const d = typeof iso === 'string' ? new Date(`${iso}T00:00:00Z`) : iso;
  const months = lang === 'he' ? MONTHS_HE : MONTHS_EN;
  return `${d.getUTCDate()} ${months[d.getUTCMonth()]} ${d.getUTCFullYear()}`;
}
