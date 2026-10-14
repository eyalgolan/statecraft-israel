# Steam "Coming Soon" page: text draft (issue #424)

**DRAFT. Nothing here is published.** No Steamworks account, app, store page or Content Survey answer exists for this
game. This file is text for the owner to edit. It clears nothing: not an asset, not the content.

**The name is "Statecraft: Israel".** Owner decision D8 in [docs/oss/DECISIONS.md](../oss/DECISIONS.md) was decided on
14 October 2026: "keep the games name as is", with the trademark and Steam-name findings recorded there accepted as a
risk. The same day the owner decided the game is free (O2), and that the launch is a fully working MVP from 15 May 1948 to
January 1952 that looks like demo2 (O10). Both are in DECISIONS.md, under "Desktop builds and the release pipeline".

Built from:

- the research report `reports/Steam coming soon page.md` (a local file, not in the repository when this was written)
  and the owner's summary of it in issue #424;
- [docs/GDD.md](../GDD.md), [README.md](../../README.md), [README.he.md](../../README.he.md),
  [docs/CONTENT_POLICY.md](../CONTENT_POLICY.md), the store-page checklist in [docs/RELEASING.md](../RELEASING.md),
  [docs/ai-disclosure.md](../ai-disclosure.md), [docs/event-videos.md](../event-videos.md),
  [LICENSING.md](../../LICENSING.md) and [ASSETS.yaml](../../ASSETS.yaml), as they stood on 14 October 2026.

Items marked **[confirm]** are in the "To confirm" list in section 5 or the open decisions in section 8.

## 1. Short description (DRAFT)

> You are Israel's prime minister on 15 May 1948. A turn-based strategy game about the state's first years, 1948 to 1951: fight the war, house and employ the immigrants, and build the country on a real map, with sourced history. Money, arms and food never cover all of it. What comes first?

That is 288 characters. The report gives no limit for this field, so this draft assumes 300 characters **[confirm]**.
The shape follows the report's recommendation: role, place and year, genre, a closing question.

## 2. About this game, English (DRAFT)

Steam's rules, from the report: the description may contain no links (the website goes in the Basic Info website
field), and the page may show only what will be in the game at launch, with planned content marked as not yet
released. The text below therefore has two parts: what the build does today, and what is designed for release and
still in development. Which of the two the page leads with depends on open decision 2 in section 8.

> **You are the prime minister of Israel on 15 May 1948.**
>
> Statecraft: Israel is a turn-based strategy game about governing the state in its first years, 1948 to 1951: the
> 1948 war, the austerity period and the immigration of those years. There is never enough money, arms or food for
> everything, and each choice is paid for somewhere else.
>
> **History with sources**
>
> - Every event, minister, date and number in the game carries a source. No quotes are invented.
> - You face real decisions at their real dates, such as the First Truce, the Altalena affair and the Bernadotte
>   assassination, through to the foreign-currency crisis of 1951. Most event cards carry an archival photograph.
> - The map is real terrain, built from elevation and climate data, with the towns, villages, Bedouin camps, railways
>   and roads of 1948. The 1949 armistice lines appear from the date of each agreement.
> - The "Why?" panel shows what changed in a turn and what caused each change.
> - The framing is Israeli, and the game says so. It is not neutral, and it does not present contested narratives as
>   parallel accounts.
>
> **In the build today**
>
> - Play from 15 May 1948 to the end of 1951.
> - Appoint and replace real ministers, such as David Ben-Gurion, Eliezer Kaplan and Moshe Sharett, inside a real
>   coalition.
> - Divide a small budget between security, immigrant absorption, the ma'abarot transit camps and austerity.
> - Manage security on a five-level escalation ladder, from routine threats to full war, and call up the reserves.
> - See the brigades of 1948, 101 Squadron and the first navy ships on the map at their sourced positions. They do not
>   take orders yet.
> - The same choices always give the same result. A saved game is the starting seed plus your list of commands.
>
> **In development for release (not in the build yet)**
>
> - One month per turn, about 48 turns for 1948 to 1951.
> - People are the scarce resource. A civilian can take a job, train as a soldier or train as a pioneer (halutz), and
>   the same people cannot do all three.
> - Cities with housing and jobs. Adding room and improving housing conditions are separate projects.
> - Pioneers who build roads and railways, plough fields, drain swamps and found new settlements (yishuvim) at
>   designated sites, each under that site's name.
> - Military units you train and command.
> - Five things to keep in balance: money, arms, food, social cohesion and international support.
> - Three objectives: survive the war; give the immigrants housing and work; complete the Tel Aviv to Haifa railway
>   and found new yishuvim. Each completed objective opens a card with an authentic photograph of the real event, a
>   short account and the real date, next to the date you got there. Getting there earlier earns bonus points.
>
> **What it is not**
>
> An early prototype, built in the open. The settlements and trees on the map are still placeholder art. There is no
> diplomacy, no music and nothing after 1951. The only audio is the archival 1948 recording of David Ben-Gurion
> reading the Declaration on the start screen (Israel State Archives). Images on this page captioned "concept art"
> show the look we are working towards, not the current build.
>
> The game deals with war, terror and the Holocaust as historical events. It is an independent project, not affiliated
> with or endorsed by the State of Israel or any person depicted.

Notes on this text:

- Every historical statement above is one that README.md or the GDD already makes. Nothing was added.
- "In the build today" restates README.md's "What works now (M3, 1948-1951)". "In development" restates only what
  the GDD fixes. It leaves out everything the GDD marks Open, Proposal or TBD: costs, durations, point values, how
  ministers are chosen and what they do, the role of the coalition, combat rules, and the whole settlement-control
  system (raid, occupy, deport, puppet regime, annexation).
- The current build's Cabinet screen and the GDD's "Minister selection TBD" are not the same thing. The text claims
  only the first.
- The GDD says "Liberation War". README.md says "the 1948 war". The draft uses README.md's term **[confirm]**.
- The audio sentence is worded to stay true whether or not PR #465 has merged: #465 adds the Israel State Archives
  recording of the Declaration to the start screen, behind a "Play the recording" button. README.md still says
  "Sound: none yet" and changes with that PR. If #465 does not ship, cut the sentence about the recording; if music
  or other sound ships, rewrite both.
- Historian review is planned for M4 and has not happened, so the text does not mention historians.

## 3. About this game, Hebrew (DRAFT, needs a native-speaker review)

**טיוטה. לא פורסם דבר. הנוסח דורש עריכת לשון של דובר עברית לפני כל שימוש.** It follows README.he.md's voice and terms, and it
is not a line-by-line translation of section 2. Two facts for the owner: the game's Hebrew interface exists as data
but is not enabled in the build (CHANGELOG.md), and the report recommends listing Hebrew on Steam as "Game Support
Only" **[confirm]**.

<div dir="rtl">

> **אתם ראש הממשלה ב־15 במאי 1948.**
>
> <span dir="ltr">Statecraft: Israel</span> הוא משחק אסטרטגיה מבוסס־תורות על ניהול המדינה בשנותיה הראשונות, 1948 עד 1951:
> מלחמת השחרור, תקופת הצנע והעלייה של אותן שנים. אין מספיק כסף, נשק ומזון לכל מה שצריך, ועל כל החלטה משלמים במקום
> אחר.
>
> **היסטוריה עם מקורות**
>
> - לכל אירוע, שר, תאריך ומספר במשחק יש מקור. אף ציטוט אינו בדוי.
> - אתם מכריעים בהחלטות אמיתיות במועדן האמיתי, כמו ההפוגה הראשונה, פרשת אלטלנה ורצח ברנדוט, ועד משבר המטבע הזר של
>   1951. ברוב כרטיסי האירועים יש צילום ארכיון.
> - המפה היא מפת שטח אמיתית, שנבנתה מנתוני גובה ואקלים, ועליה העיירות, הכפרים, מאהלי הבדואים, מסילות הברזל והדרכים של
>   1948. קווי שביתת הנשק של 1949 מופיעים מהתאריך של כל הסכם.
> - לוח "למה?" מפרט מה השתנה בתור ומה גרם לכל שינוי.
> - המסגור ישראלי, והמשחק אומר זאת במפורש. הוא אינו ניטרלי, ואינו מציג נרטיבים שנויים במחלוקת כגרסאות מקבילות.
>
> **מה יש בגרסה היום**
>
> - משחקים מ־15 במאי 1948 ועד סוף 1951.
> - ממנים ומחליפים שרים אמיתיים, כמו דוד בן־גוריון, אליעזר קפלן ומשה שרת, בתוך קואליציה אמיתית.
> - מחלקים תקציב זעום בין ביטחון, קליטת עלייה, המעברות והצנע.
> - מנהלים את הביטחון על סולם הסלמה של חמש דרגות, מאיום שגרתי ועד מלחמה כוללת, ומגייסים מילואים.
> - חטיבות 1948, טייסת 101 וספינות חיל הים הראשונות מוצגות על המפה במקומן לפי המקורות. הן עדיין לא מקבלות פקודות.
> - אותן בחירות תמיד נותנות אותה תוצאה. משחק שמור הוא נקודת הפתיחה ורשימת הפקודות שלכם.
>
> **בפיתוח לקראת ההשקה (עוד לא בגרסה)**
>
> - כל תור הוא חודש, וכ־48 תורות לשנים 1948 עד 1951.
> - האנשים הם המשאב שחסר. אזרח יכול לעבוד, להתאמן כחייל או לצאת להכשרה ולהיות חלוץ, ואי אפשר לעשות את שלושתם
>   באותם אנשים.
> - ערים עם דיור ומקומות עבודה. הוספת מקום ושיפור תנאי הדיור הם שני פרויקטים נפרדים.
> - חלוצים שסוללים דרכים ומסילות, חורשים שדות, מייבשים ביצות ומקימים יישובים חדשים באתרים שנקבעו, כל יישוב בשם של
>   האתר שלו.
> - יחידות צבא שמאמנים ומפקדים עליהן.
> - חמישה דברים שצריך לאזן: כסף, נשק, מזון, לכידות חברתית ותמיכה בינלאומית.
> - שלוש מטרות: לשרוד את המלחמה; לתת לעולים דיור ועבודה; להשלים את מסילת הברזל מתל אביב לחיפה ולהקים יישובים חדשים.
>   כל מטרה שהושלמה פותחת כרטיס עם צילום אותנטי של האירוע האמיתי, תיאור קצר והתאריך האמיתי, לצד התאריך שבו אתם
>   הגעתם לשם. מי שמקדים מקבל נקודות בונוס.
>
> **מה עוד אין**
>
> זה אב־טיפוס מוקדם שנבנה בקוד פתוח. היישובים והעצים במפה הם עדיין אמנות זמנית. אין דיפלומטיה, אין מוזיקה ואין דבר
> אחרי 1951. הקול היחיד במשחק הוא ההקלטה הארכיונית מ־1948 של דוד בן־גוריון קורא את ההכרזה, במסך הפתיחה (ארכיון
> המדינה). תמונות בעמוד הזה שמסומנות "אמנות קונספט" מציגות את המראה שאליו אנחנו שואפים, ולא את הגרסה הנוכחית.
>
> המשחק עוסק במלחמה, בטרור ובשואה כאירועים היסטוריים. זה פרויקט עצמאי, ואין לו קשר למדינת ישראל או לאדם כלשהו
> המופיע בו, והם אינם תומכים בו.

</div>

Terms taken from the repository, for the reviewer: "מלחמת השחרור" (the website's Hebrew text; "מלחמת העצמאות" is the
alternative, and the website also writes "מלחמת 1948"), "מאהלי בדואים" and "משבר המטבע הזר" (the website),
"תקופת הצנע" and "לכידות חברתית" (the GDD), "איום שגרתי" and "מלחמה כוללת" (the game's Hebrew strings), "אמנות
קונספט", "צילום ארכיון", "אב־טיפוס" and "מבוסס־תורות" (README.he.md). "סולם הסלמה של חמש דרגות", "תמיכה בינלאומית",
"אמנות זמנית", "חלוץ" for the GDD's Halutz unit and "נקודות בונוס" are this draft's own wording and need checking
most **[confirm]**.

## 4. AI-disclosure text (DRAFT)

For the generative-AI section of the Content Survey, which Steam shows publicly on the page. The report's proposed
wording covered store media only and ended with a placeholder for AI content that ships in the build. This version
fills that placeholder and accounts for decisions D-M (music) and D-V (video clips in the game).

> **In the game.** The painting behind the start screen is AI-generated. On 11 decision cards, an optional
> "Illustration" switch plays a short, silent, AI-generated video loop in place of the card's archival photograph.
> The loops are AI-generated, show no recognisable real person, and play only if you press the switch. They carry no
> on-screen caption; the four made with Dreamina keep Dreamina's own small "AI" watermark in a corner. Event and content text was drafted with AI and every fact in it is checked by a
> person against its cited source. The code was written with AI coding agents and goes through tests and review. The
> game has no music. Its only audio is the archival 1948 recording of David Ben-Gurion reading the Declaration on the
> start screen (Israel State Archives), which is not AI. The portraits and the event photographs are archival photographs, not AI images, and
> the terrain is built from real elevation data. The game does not generate content with AI while you play.
>
> **On this store page.** The capsule art and the images captioned "concept art" were made with AI image tools. They
> show the look we are working towards, not the current build. [If image 04 is used:] The AI images of historical
> ministers are illustrations, not photographs. [If a cinematic trailer is used:] The cinematic teaser was made with
> AI video, voice and music tools and is not gameplay. All screenshots and the gameplay trailer are captured from the
> game. A screenshot of the start screen, or of a decision card with the Illustration switch on, shows the AI content
> described above.

What each statement rests on:

| Statement | Record | Status |
| --- | --- | --- |
| Start-screen painting is AI-generated | `unity/Assets/UI/StartScreen/declaration-hall.jpg` in ASSETS.yaml: ElevenLabs Image API, GPT Image 2.5, with a Dreamina painting as its composition reference | Row is `owner-verify`; decision D-D is open |
| 11 silent illustration clips on decision cards | `unity/Assets/StreamingAssets/video/*.mp4`, 11 rows in ASSETS.yaml: 9 made with Veo 3.1 or Veo 3.1 Fast through ElevenLabs, 2 with Seedance 2.0 through Dreamina (`austerity_1949`, `iraqi_exodus`); added in PR #454 | Rows `verified` under D-V, which covers free, commercial and paid builds |
| Clips are AI-generated, silent, opt-in, no recognisable real person | [docs/event-videos.md](../event-videos.md); provenance in ASSETS.yaml (`ai_generated: true` on every clip) and [docs/ai-disclosure.md](../ai-disclosure.md) | Silence is enforced by the encoder. No on-screen caption: owner ruling of 2026-10-14 ([DECISIONS](../oss/DECISIONS.md) G4). The four Dreamina clips (`austerity_1949`, `iraqi_exodus`, `maabarot_policy-outcome`, `operation_horev-outcome`) keep Dreamina's own small "AI" watermark: owner ruling "Keep the badge" (G5). The sensitive clips (`ten_days`, `operation_yoav`, the `operation_horev` outcome) were reviewed by the owner on 2026-10-14 (G4) |
| Text drafted with AI, each fact checked by a person | README.md "AI use"; CONTENT_POLICY section 2, rule 6 | Stated project rule |
| Code written with AI agents | README.md "AI use" | Steam's form is about content players see, not tools, so this sentence is optional |
| No music; the only audio is the archival recording on the start screen, not AI | PR #465 (the Israel State Archives recording of the Declaration, behind a "Play the recording" button); README.md "Sound: none yet" until that PR merges | Open PR on 14 October 2026. Check the recording's ASSETS.yaml row once it merges; cut the recording sentence if it does not ship |
| Portraits and event photographs are not AI | LICENSING.md, NOTICE | Portrait rights are a separate open decision (D-P) |
| Nothing generated live | README.md, docs/ai-disclosure.md | "Keep the statement only while it remains true" (RELEASING.md) |
| Capsule art and concept art are AI | `promo/final/01` to `08` in ASSETS.yaml: ElevenLabs Image API (GPT Image 2.5, Nano Banana Pro) | Rows `owner-verify`; D5 found redistribution allowed |
| Cinematic teaser: AI video, voice and music | The report, citing the demo2 production notes; music is ElevenLabs Music, licensed for commercial use including a store trailer by owner decision D-M | Applies only if demo2 goes on the page (section 6) |

Not AI, and useful to know when answering the survey: `promo/final/09-m2-flythrough.mp4`, `promo/final/10-m2-topdown.png`
and everything under `docs/images/build-*` are captures from the engine. The game's tab icon in the browser build was
drawn with AI help (README.md); it would not be part of a desktop Steam build.

RELEASING.md's checklist still applies before this text is used: regenerate `docs/ai-disclosure.md`, compare it with
both manifests and with the build that actually ships, and do not treat an `owner-verify` row as cleared because it
is disclosed.

## 5. To confirm

Facts this draft could not settle. Each is flagged above instead of being asserted.

1. **Short-description limit.** Assumed 300 characters. Check the field in Steamworks.
2. **Whether a person has checked every fact in the text that ships.** The disclosure says so because CONTENT_POLICY
   requires it and says every fact is at least `fact-checked` as of M3. The owner should confirm it for the launch
   build's content.
3. **Hebrew text in the game.** The Hebrew interface strings exist but are not enabled. The repository does not
   record whether they were translated with AI. If Hebrew ships, the disclosure and the language list both change.
4. **Sound.** The text names one piece of audio: the Israel State Archives recording of the Declaration on the start
   screen (PR #465, open when this was written). Confirm it shipped and how its ASSETS.yaml row records the rights.
   If music or other sound is added before release (issue #39), the "no music" sentence goes and the source of that
   audio is disclosed.
5. **Demo2's voices and score.** This draft did not read the production files under `promo/`. "AI video, voice and
   music tools" comes from the report, which says demo2 uses ElevenLabs premade voices and ElevenLabs Music. Confirm
   against the final cut.
6. **The demo 1 score's source.** Settled (owner, 2026-10-14): the cinematic trailer's music is AI-generated, made
   with ElevenLabs Music, and D-M covers its licence (#521 records it). Demo 1 stays off Steam.
7. **What image 01 shows.** Its generation logs under `promo/` are named `keyart_2026`, which suggests the key art
   shows the country in 2026. The report clears 01 for the capsules but rules out 02, 06 and 07 for showing years
   outside 1948 to 1951. The owner should look at 01 with the launch-content rule in mind.
8. **Capsule art and the name.** Whether 01 has the title painted into it was not checked. Capsules need a readable
   logo, and no logo or wordmark file exists (TRADEMARK.md: "when there is one").
9. **Start-screen painting in a paid release.** The game is free (O2), so this matters only if that changes. D-D's
   recorded recommendation is that a paid release needs confirmed
   commercial rights for the Dreamina plan used, or a replacement image. The current painting was made on ElevenLabs
   but used the Dreamina painting as its composition reference, and is recorded under the same open sign-off.
10. **"1948 war" or "Liberation War"** in English, and **"מלחמת השחרור" or "מלחמת העצמאות"** in Hebrew.
11. **The Hebrew wording** listed at the end of section 3, and the whole Hebrew text, by a native speaker.
12. **Platform.** The game ships only as a browser (WebGL) build today. The owner decided on 14 October 2026 (O1, O7) that
    release builds are made on the owner's Mac for Windows, macOS and Linux. None exists yet, so the draft names no platform.
13. **The store text is itself AI-drafted.** Sections 1 to 3 were written by an AI agent for the owner to edit. Whether
    that needs a line in the disclosure is the owner's call once the text is final.

## 6. Mature-content description (DRAFT)

The report's wording, which follows CONTENT_POLICY section 1 (the perspective is Israeli and says so) and section 2
(rules 3, 4 and 5). One sentence is added for the illustration clips, which did not exist when the report was written.

> Statecraft: Israel depicts real events of 1948-1951 from the perspective of Israel's government, including war,
> terror attacks, refugees and displacement, and references to the Holocaust. These are shown through sourced text,
> archival photographs and a symbolic map, not graphic violence. Some decision cards can also show a short, silent,
> AI-generated illustration, labelled as such. Real historical figures appear in their public roles; their in-game
> ratings are game balance, not judgements of the people.

Notes:

- This is draft wording, not a survey answer. RELEASING.md requires the owner to answer every survey section from
  what actually ships, including bundled content that cannot be reached in play, and not from the project's subject.
- Each screenshot's "suitable for all ages" flag is judged on its own. A sensitive scene is never flagged suitable to
  gain placement (RELEASING.md).
- If the GDD's settlement-control rules ship, the description must also cover the Deport outcome. Proposed addition,
  from the owner ruling of 12 October 2026 and CONTENT_POLICY section 4: "The player can order the deportation of a
  captured settlement's residents. The game gives no reward for it and shows its costs." Those rules are still open in
  the GDD, so the sentence is not in the draft above.
- The seven sensitive events of the current build were sensitivity-reviewed by the owner (decision D11). Later events
  and the two sensitive clips need their own sign-off before any shareable build.

## 7. Media placement

Sizes for the capsules and screenshots are the ones in RELEASING.md's checklist. "Report" means the placement table
in the research report. Files under `promo/demo*` are local, not in the repository, and were listed by name only.

### Capsules and library art

| Slot | Upload size | Asset | State |
| --- | --- | --- | --- |
| Header capsule | 920 x 430 | Crop of `promo/final/01-key-art.png` (2048 x 1152, AI) with the title | Not made. The name is settled (D8); see To confirm 7 and 8 |
| Small capsule | 462 x 174 | Same source | Not made. Check it at 120 x 45 and 184 x 69 (RELEASING.md) |
| Main capsule | 1232 x 706 | Same source | Not made |
| Vertical capsule | 748 x 896 | Same source, portrait crop | Not made |
| Library hero | 3840 x 1240, no text (report) | None | **No asset.** 01 is 2048 px wide, too small without an upscale or a new render |
| Other library art (capsule, logo) | Check in Steamworks | None | **No asset.** No logo or wordmark exists |
| Page background | Check in Steamworks | Report suggests `promo/final/10-m2-topdown.png` | **Not usable as is:** the file is 374 x 1000 px |

Capsule text is the game title only. Text on an asset must exist in every language the page lists, so the capsules
carry the English title unless a Hebrew variant is made (report).

### Screenshots (at least five, 1920 x 1080 or larger, 16:9, real build only)

| Candidate | Size | State |
| --- | --- | --- |
| `docs/images/build-m3-map.jpg`, `build-m3-city.jpg`, `build-m3-cabinet.jpg`, `build-m3-why.jpg`, `build-m3-start.jpg` | 1568 x 882 | Too small. They show the right subjects (map with an event card, city close-up, Cabinet, "Why?" panel), so they are the shot list for the recapture. The start screen is not gameplay and shows the AI painting, so it should not be one of the five |
| New captures from the release build | 1920 x 1080 | **No asset.** RELEASING.md requires the build commit, the zip SHA-256 and the capture date to be recorded with them |

No AI image can go in a screenshot slot: not `promo/final/01` to `08`, and not `docs/images/hero-concept-1936.jpg`.

### Trailers

| Slot | Asset | State |
| --- | --- | --- |
| Trailer 1: gameplay, from the real build | None | **No asset.** The 1920 x 1080, 60 fps capture and cut are the pending trailer part of issue #45. The report recommends 30 to 60 seconds with gameplay in the first 5 seconds. `docs/images/build-gameplay.mp4` (1920 x 1080, about 11 s, real gameplay, commit f93fd5e) shows the kind of footage but is too short to be the trailer |
| Trailer 2: "Cinematic teaser (AI-generated, not gameplay)", category General / Cinematic | The final demo2 cut (in progress) | **Conditional.** Music is cleared by D-M. Still needed: the owner's amendment to RELEASING.md's "real build only" trailer rule, and the final cut itself, which is being re-cut and has no file to name yet |
| Not for Steam | `site/public/media/promo-trailer-720p.mp4` (demo 1, the README trailer) | Excluded by the report and by issue #424: it shows the AI ministers as a game screen and quotes a living person |
| Not assessed | `promo/demo3/out/demo3.mp4` | A third promo video exists locally. The report does not cover it, so this draft places it nowhere |

### Images and clips inside "About this game" (15 MB in total, clips up to 12 seconds, per the report)

| Asset | Caption | State |
| --- | --- | --- |
| `promo/final/03-gameplay-1936.png` (AI) | "Concept art: the target look, not the current build" | Allowed here only, never as a screenshot. It is set in 1936, before the game's 1948 start |
| `promo/final/05-event-transit-camps.png` (AI) | "Concept art" | Fits 1948 to 1951 (report) |
| `promo/final/04-cabinet-1948.png` (AI likenesses) | "AI concept art, not photographs" | **Blocked by D-P.** Only after the image is regenerated from US-safe references. Never on a capsule |
| `promo/final/09-m2-flythrough.mp4` (engine capture, 1920 x 1080, 8 s) | "In-engine capture of an earlier milestone (M2)" | Not AI. Usable as a short clip, though it shows an older look. RELEASING.md bars it as the trailer |
| `docs/images/build-gameplay.mp4` (real gameplay, 1920 x 1080, about 11 s) | "Real gameplay, build 0.1.0" | Not AI. Fits the 12-second limit. Recapture it with the screenshots so the page shows one build |
| `promo/final/02-map-diorama.png`, `06-diplomacy-1958.png`, `07-eras-tel-aviv.png`, `08-defense-1955.png` | none | **Off the page.** They show years outside 1948 to 1951, and 08 is also blocked by D-P |
| The 11 illustration clips in `unity/Assets/StreamingAssets/video/` | none | Not proposed for the page. They are in-game illustrations and appear only if a screenshot or trailer happens to show one |

## 8. Open decisions for the owner

1. **D8, the name.** Decided (owner, 2026-10-14): "keep the games name as is". The live USPTO class-9 registration for
   STATECRAFT and the two games named "Statecraft" on Steam, recorded in DECISIONS.md, are accepted as a risk.
2. **Which game the page describes.** The current build (the Cabinet, Budget and Defence prototype) or the GDD game
   (units, cities, construction, objectives), which is not playable yet. Steam wants the page to show launch content.
   Section 2 separates the two so either answer is an edit, not a rewrite. The owner's O10 answer (2026-10-14) sets the
   launch as a fully working MVP from 15 May 1948 to January 1952 that looks like demo2. That points to the GDD game, but
   which game the page shows before that build exists is still the owner's call.
3. **Whether the page may open before the release gates.** RELEASING.md's checklist says to finish the gates first.
   Opening earlier is an amendment the owner records.
4. **Whether an AI cinematic may be the second trailer.** RELEASING.md says trailers come from the real build only.
   Demo2 needs a recorded amendment, and its final cut.
5. **D-P, the portraits.** Open. It blocks `promo/final/04` and `08`, and it bears on Steam's requirement that the
   developer holds rights to everything shown, including the in-game portraits still marked `owner-verify`.
6. **D-D, the start-screen painting.** Open. It decides whether the painting can ship in a paid build.
7. **Free or paid.** Decided (owner, 2026-10-14, O2): "free". The Steam setup is "This is a free product". README.md
   and TRADEMARK.md say the game is free and non-commercial (#521).
8. **Languages.** English only, or Hebrew listed as "Game Support Only" as the report suggests, given that Hebrew is
   not enabled in the build.
9. **Desktop build.** Needed for a Steam release, not for the page. The plan is
   `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (issue #482). Its decisions are in
   DECISIONS.md; O9 (how testers get builds) and O11 (the Steamworks wrapper) are still open.
10. **Other open lines in DECISIONS.md** that the release gate refuses on: D13 (land-use texture licence) and D-A
    (armistice lines). They do not touch the store text, but they hold the release.
