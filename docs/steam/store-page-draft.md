# Steam "Coming Soon" page: text draft (issue #424)

**DRAFT. Nothing here is published.** No Steamworks account, app, store page or Content Survey answer exists for this
game. This file is text for the owner to edit. It clears nothing: not an asset, not the content. The media, the
AI-disclosure fields, the draft Content Survey answers, the tags, the platforms and the owner's Steamworks checklist are
in [coming-soon-kit.md](coming-soon-kit.md) (P57, 14 October 2026).

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

## 2. About this game, English (FINAL DRAFT for the owner, updated 14 October 2026)

Updated for the Coming Soon kit (P57, [coming-soon-kit.md](coming-soon-kit.md)) against `main` at `c0a38256`. What
changed from the first draft: the map now colours areas by who held them from 11 June 1948 and ends in a wooden frame
(#555, #36); boats sail off Jaffa, Tel Aviv and Haifa (#388); the archival recording of the Declaration shipped (#465);
three historical orders now play a short outcome clip at End Turn; and the desktop builds for Windows, macOS and Linux
exist (M2). The text still separates what the build does today from what is designed for launch, because Steam asks
the page to show launch content and the launch (O10) is the GDD's MVP, which is not playable yet. Open decision 2 in
section 8 (which game the page leads with) still applies.

Steam's rules, from the report: the description may contain no links (the website goes in the Basic Info website
field), and the page may show only what will be in the game at launch, with planned content marked as not yet
released.

> **You are the prime minister of Israel on 15 May 1948.**
>
> Statecraft: Israel is a free, open-source, turn-based strategy game about governing the new state in its first years,
> from 15 May 1948 to the end of 1951: the 1948 war, the austerity period and the mass immigration of those years.
> There is never enough money, arms or food for everything, and each choice is paid for somewhere else.
>
> **History with sources**
>
> - Every event, minister, date and number in the game carries a source. No quotes are invented.
> - You face real decisions at their real dates, such as the First Truce, the Altalena affair and the Bernadotte
>   assassination, through to the foreign-currency crisis of 1951. Most event cards carry an archival photograph.
> - The map is real terrain, built from elevation and climate data, with the towns, villages, Bedouin camps, railways
>   and roads of 1948, and boats off the coast. From 11 June 1948 it colours the land by who held it, from the front
>   lines of an Israel State Archives map; each 1949 armistice line appears from the date of its agreement.
> - The "Why?" panel shows what changed in a turn and what caused each change.
> - The framing is Israeli, and the game says so. It is not neutral, and it does not present contested narratives as
>   parallel accounts.
>
> **In the build today**
>
> - Play from 15 May 1948 to the end of 1951, on Windows, macOS or Linux.
> - Appoint and replace real ministers, such as David Ben-Gurion, Eliezer Kaplan and Moshe Sharett, inside a real
>   coalition.
> - Divide a small budget between security, immigrant absorption, the ma'abarot transit camps and austerity.
> - Manage security on a five-level escalation ladder, from routine threats to full war, and call up the reserves.
> - See the brigades of 1948, 101 Squadron and the first navy ships on the map at their sourced positions. They do not
>   take orders yet.
> - The same choices always give the same result. A saved game is the starting seed plus your list of commands.
>
> **In development for launch (not in the build yet)**
>
> - One month per turn, from 15 May 1948 to January 1952.
> - People are the scarce resource. A civilian can take a job, train as a soldier or train as a pioneer (halutz), and
>   the same people cannot do all three.
> - Cities with housing and jobs. Adding room and improving housing conditions are separate projects.
> - Pioneers who build roads and railways, plough fields, drain swamps and found new settlements (yishuvim) at
>   designated sites, each under that site's name.
> - Military units you train and command.
> - Five things to keep in balance: money, arms, food, social cohesion and international support.
> - Three objectives: survive the war; give the immigrants housing and work; complete the Tel Aviv to Haifa railway
>   and the Burma Road, and found new yishuvim. Each completed objective opens a card with an authentic photograph of
>   the real event, a short account and the real date, next to the date you got there. Getting there earlier earns
>   bonus points.
>
> **What it is not**
>
> An early prototype, built in the open. The settlements and trees on the map are still placeholder art. There is no
> diplomacy, no music and nothing after 1951. The only audio is the archival 1948 recording of David Ben-Gurion
> reading the Declaration on the start screen (Israel State Archives), played only when you press its button.
>
> The game deals with war, terror and the Holocaust as historical events, through text and archival photographs,
> without graphic violence. It is an independent project, not affiliated with or endorsed by the State of Israel or
> any person depicted.

Notes on this text:

- Every historical statement above is one that README.md, CHANGELOG.md or the GDD already makes. Nothing was added.
- "In the build today" restates README.md's "What works now (M3, 1948-1951)" and the CHANGELOG's Unreleased section.
  "In development" restates only what the GDD fixes, plus G1 (the Burma Road joins objective 3). It leaves out
  everything the GDD marks Open, Proposal or TBD: costs, durations, point values, how ministers are chosen and what
  they do, the role of the coalition, combat rules, and the whole settlement-control system.
- "on Windows, macOS or Linux" stands only for the platforms that pass M2 (section 9). Cut the platforms that do not.
- The sentence about concept-art captions was dropped: this kit's capsules and screenshots are all real engine
  output, so no image on the page is concept art unless the owner adds one (section 7).
- The GDD says "Liberation War". README.md says "the 1948 war". The text keeps README.md's term **[confirm]**.
- Historian review is planned for M4 and has not happened, so the text does not mention historians.
- Text on the capsules is the title only (RELEASING.md, Valve's asset rules).

## 3. About this game, Hebrew (DRAFT, needs a native-speaker review)

**טיוטה. לא פורסם דבר. הנוסח דורש עריכת לשון של דובר עברית לפני כל שימוש.** It follows README.he.md's voice and terms, and it
is not a line-by-line translation of section 2. Two facts for the owner: the game's Hebrew interface is only a preview,
asked for by hand (`-lang he` on desktop, `?lang=he` on the web; #193), and the event and content text is English only
until #55; and the report recommends listing Hebrew on Steam as "Game Support Only" **[confirm]**. Updated on 14
October 2026 to match section 2 (free and open source, the colours by side, the boats, the platforms, the launch dates,
the Burma Road); the added sentences need the same native-speaker review.

<div dir="rtl">

> **אתם ראש הממשלה ב־15 במאי 1948.**
>
> <span dir="ltr">Statecraft: Israel</span> הוא משחק אסטרטגיה מבוסס־תורות, חינמי ובקוד פתוח, על ניהול המדינה בשנותיה
> הראשונות, מ־15 במאי 1948 ועד סוף 1951: מלחמת השחרור, תקופת הצנע והעלייה ההמונית של אותן שנים. אין מספיק כסף, נשק ומזון לכל מה שצריך, ועל כל החלטה משלמים במקום
> אחר.
>
> **היסטוריה עם מקורות**
>
> - לכל אירוע, שר, תאריך ומספר במשחק יש מקור. אף ציטוט אינו בדוי.
> - אתם מכריעים בהחלטות אמיתיות במועדן האמיתי, כמו ההפוגה הראשונה, פרשת אלטלנה ורצח ברנדוט, ועד משבר המטבע הזר של
>   1951. ברוב כרטיסי האירועים יש צילום ארכיון.
> - המפה היא מפת שטח אמיתית, שנבנתה מנתוני גובה ואקלים, ועליה העיירות, הכפרים, מאהלי הבדואים, מסילות הברזל והדרכים של
>   1948, וסירות מול החוף. מ־11 ביוני 1948 היא צובעת את השטח לפי מי שהחזיק בו, על פי קווי החזית במפה של ארכיון
>   המדינה, וכל קו של שביתת הנשק של 1949 מופיע מהתאריך של ההסכם שלו.
> - לוח "למה?" מפרט מה השתנה בתור ומה גרם לכל שינוי.
> - המסגור ישראלי, והמשחק אומר זאת במפורש. הוא אינו ניטרלי, ואינו מציג נרטיבים שנויים במחלוקת כגרסאות מקבילות.
>
> **מה יש בגרסה היום**
>
> - משחקים מ־15 במאי 1948 ועד סוף 1951, ב־Windows, ב־macOS או ב־Linux.
> - ממנים ומחליפים שרים אמיתיים, כמו דוד בן־גוריון, אליעזר קפלן ומשה שרת, בתוך קואליציה אמיתית.
> - מחלקים תקציב זעום בין ביטחון, קליטת עלייה, המעברות והצנע.
> - מנהלים את הביטחון על סולם הסלמה של חמש דרגות, מאיום שגרתי ועד מלחמה כוללת, ומגייסים מילואים.
> - חטיבות 1948, טייסת 101 וספינות חיל הים הראשונות מוצגות על המפה במקומן לפי המקורות. הן עדיין לא מקבלות פקודות.
> - אותן בחירות תמיד נותנות אותה תוצאה. משחק שמור הוא נקודת הפתיחה ורשימת הפקודות שלכם.
>
> **בפיתוח לקראת ההשקה (עוד לא בגרסה)**
>
> - כל תור הוא חודש, מ־15 במאי 1948 ועד ינואר 1952.
> - האנשים הם המשאב שחסר. אזרח יכול לעבוד, להתאמן כחייל או לצאת להכשרה ולהיות חלוץ, ואי אפשר לעשות את שלושתם
>   באותם אנשים.
> - ערים עם דיור ומקומות עבודה. הוספת מקום ושיפור תנאי הדיור הם שני פרויקטים נפרדים.
> - חלוצים שסוללים דרכים ומסילות, חורשים שדות, מייבשים ביצות ומקימים יישובים חדשים באתרים שנקבעו, כל יישוב בשם של
>   האתר שלו.
> - יחידות צבא שמאמנים ומפקדים עליהן.
> - חמישה דברים שצריך לאזן: כסף, נשק, מזון, לכידות חברתית ותמיכה בינלאומית.
> - שלוש מטרות: לשרוד את המלחמה; לתת לעולים דיור ועבודה; להשלים את מסילת הברזל מתל אביב לחיפה ואת דרך
>   בורמה, ולהקים יישובים חדשים.
>   כל מטרה שהושלמה פותחת כרטיס עם צילום אותנטי של האירוע האמיתי, תיאור קצר והתאריך האמיתי, לצד התאריך שבו אתם
>   הגעתם לשם. מי שמקדים מקבל נקודות בונוס.
>
> **מה עוד אין**
>
> זה אב־טיפוס מוקדם שנבנה בקוד פתוח. היישובים והעצים במפה הם עדיין אמנות זמנית. אין דיפלומטיה, אין מוזיקה ואין דבר
> אחרי 1951. הקול היחיד במשחק הוא ההקלטה הארכיונית מ־1948 של דוד בן־גוריון קורא את ההכרזה, במסך הפתיחה (ארכיון
> המדינה), והיא מושמעת רק כשלוחצים על הכפתור שלה.
>
> המשחק עוסק במלחמה, בטרור ובשואה כאירועים היסטוריים, בטקסט ובצילומי ארכיון ובלי אלימות גרפית. זה פרויקט עצמאי, ואין לו קשר למדינת ישראל או לאדם כלשהו
> המופיע בו, והם אינם תומכים בו.

</div>

Terms taken from the repository, for the reviewer: "מלחמת השחרור" (the website's Hebrew text; "מלחמת העצמאות" is the
alternative, and the website also writes "מלחמת 1948"), "מאהלי בדואים" and "משבר המטבע הזר" (the website),
"תקופת הצנע" and "לכידות חברתית" (the GDD), "איום שגרתי" and "מלחמה כוללת" (the game's Hebrew strings), "אמנות
קונספט", "צילום ארכיון", "אב־טיפוס" and "מבוסס־תורות" (README.he.md). "סולם הסלמה של חמש דרגות", "תמיכה בינלאומית",
"אמנות זמנית", "חלוץ" for the GDD's Halutz unit and "נקודות בונוס" are this draft's own wording and need checking
most **[confirm]**.

## 4. AI-disclosure text (FINAL DRAFT for the owner)

The text for Steam's generative-AI fields, formatted field by field, is in
[coming-soon-kit.md, section 4](coming-soon-kit.md#4-ai-content-disclosure-steam-fields). It was regenerated from
`uv run scripts/oss/ai_disclosure.py` on `main` at `c0a38256` (identical to the committed `docs/ai-disclosure.md`). The
public summary it ends with is repeated here:

> **In the game.** The painting behind the start screen is AI-generated. On 11 decision cards, an optional
> "Illustration" switch plays a short, silent, AI-generated video loop in place of the card's archival photograph, and
> when you order the historical option of three events (the transit camps, the Hula works, Operation Horev), a 3.5-second
> silent AI-generated clip plays once at End Turn. The 14 clips show no recognisable real person and carry no on-screen
> caption; the four made with Dreamina keep Dreamina's own small "AI" mark in a corner. Event and content text was
> drafted with AI, and every fact in it is checked by a person against its cited source. The game has no music. Its
> only audio is the archival 1948 recording of David Ben-Gurion reading the Declaration (Israel State Archives), which
> is not AI. The portraits and the event photographs are archival photographs, not AI images, and the terrain is built
> from real elevation data. The game does not generate content with AI while you play.
>
> **On this store page.** The capsules, the library art and all screenshots are captured from the game's own engine;
> the title on them is set in the EB Garamond typeface. A screenshot of the start screen, or of a decision card with
> the Illustration switch on, would show the AI content described above; none of this kit's screenshots does.
> [Only if a cinematic trailer is added:] The cinematic teaser was made with AI video, voice and music tools and is
> not gameplay.

What each statement rests on:

| Statement | Record | Status |
| --- | --- | --- |
| Start-screen painting is AI-generated | `unity/Assets/UI/StartScreen/declaration-hall.jpg` in ASSETS.yaml: ElevenLabs Image API, GPT Image 2.5, with a Dreamina painting as its composition reference | Row is `owner-verify`; D-D decided 2026-10-14 (PR #565) for the free, non-commercial game; a paid release needs the commercial-rights check or a new image |
| 11 silent illustration clips on decision cards and 3 outcome clips at End Turn | `unity/Assets/StreamingAssets/video/*.mp4`, 14 rows in ASSETS.yaml: 10 made with Veo 3.1 or Veo 3.1 Fast through ElevenLabs, 4 with Seedance 2.0 through Dreamina (`austerity_1949`, `iraqi_exodus`, `maabarot_policy-outcome`, `operation_horev-outcome`); PRs #454 and #466 | Rows `verified` under D-V, which covers free, commercial and paid builds |
| Clips are AI-generated, silent, opt-in, no recognisable real person | [docs/event-videos.md](../event-videos.md); provenance in ASSETS.yaml (`ai_generated: true` on every clip) and [docs/ai-disclosure.md](../ai-disclosure.md) | Silence is enforced by the encoder. No on-screen caption: owner ruling of 2026-10-14 ([DECISIONS](../oss/DECISIONS.md) G4). The four Dreamina clips (`austerity_1949`, `iraqi_exodus`, `maabarot_policy-outcome`, `operation_horev-outcome`) keep Dreamina's own small "AI" watermark: owner ruling "Keep the badge" (G5). The sensitive clips (`ten_days`, `operation_yoav`, the `operation_horev` outcome) were reviewed by the owner on 2026-10-14 (G4) |
| Text drafted with AI, each fact checked by a person | README.md "AI use"; CONTENT_POLICY section 2, rule 6 | Stated project rule |
| Code written with AI agents | README.md "AI use" | Steam's form is about content players see, not tools, so this sentence is optional |
| No music; the only audio is the archival recording on the start screen, not AI | PR #465, merged: `unity/Assets/StreamingAssets/audio/declaration-1948-isa-00113hj.wav`, behind a "Play the recording" button; README.md "Sound: no AI sound" | Shipped. Its ASSETS.yaml row is `owner-verify` under D-R, whose part (a) is still open |
| Portraits and event photographs are not AI | LICENSING.md, NOTICE | Portrait rights are a separate open decision (D-P) |
| Nothing generated live | README.md, docs/ai-disclosure.md | "Keep the statement only while it remains true" (RELEASING.md) |
| Capsule art is not AI | This kit's capsules are engine renders plus the EB Garamond title ([coming-soon-kit.md](coming-soon-kit.md)); `promo/final/01` to `08` (AI) are not used | Concept art goes on the page only if the owner adds it, captioned, with the disclosure's store-page paragraph extended |
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
4. **Sound.** Settled for the text: the Israel State Archives recording of the Declaration shipped with PR #465. Its
   ASSETS.yaml row is `owner-verify` under D-R, whose part (a) is still open. If music or other sound is added before
   release (issue #39), the "no music" sentence goes and the source of that audio is disclosed.
5. **Demo2's voices and score.** This draft did not read the production files under `promo/`. "AI video, voice and
   music tools" comes from the report, which says demo2 uses ElevenLabs premade voices and ElevenLabs Music. Confirm
   against the final cut.
6. **The demo 1 score's source.** Settled (owner, 2026-10-14): the cinematic trailer's music is AI-generated, made
   with ElevenLabs Music, and D-M covers its licence (#521 records it). Demo 1 stays off Steam.
7. **What image 01 shows.** No longer needed for the page: the kit's capsules are built from engine renders, not from
   `promo/final/01` (AI). If the owner prefers 01 for the capsules, its `keyart_2026` naming (the country in 2026?) still
   needs a look under the launch-content rule, and the disclosure's store-page paragraph changes.
8. **Capsule art and the name.** The kit makes a wordmark: "Statecraft: Israel" in EB Garamond SemiBold, cream on
   transparency (`library_logo_1280w.png`). Whether it becomes the project's logo for TRADEMARK.md ("when there is
   one") is the owner's call.
9. **Start-screen painting in a paid release.** D-D was decided on 14 October 2026 (PR #565) for free, non-commercial
   distribution. The game is free (O2), so this matters only if that changes: before any paid release, the owner
   confirms commercial rights for the Dreamina plan used, or replaces the image. The current painting was made on
   ElevenLabs but used the Dreamina painting as its composition reference, and falls under the same caveat.
10. **"1948 war" or "Liberation War"** in English, and **"מלחמת השחרור" or "מלחמת העצמאות"** in Hebrew.
11. **The Hebrew wording** listed at the end of section 3, and the whole Hebrew text, by a native speaker.
12. **Platform.** The desktop builds (O1, O7) were made on the owner's Mac for M2 on 14 October 2026 (`v0.1.0-rc.0`).
    The supported-OS list in section 9 names only the platforms whose build passes M2; until the coordinator records
    that result, it is marked "pending M2 result".
13. **The store text is itself AI-drafted.** Sections 1 to 3 were written by an AI agent for the owner to edit. Whether
    that needs a line in the disclosure is the owner's call once the text is final.

## 6. Mature-content description (DRAFT)

The report's wording, which follows CONTENT_POLICY section 1 (the perspective is Israeli and says so) and section 2
(rules 3, 4 and 5). One sentence is added for the illustration clips, which did not exist when the report was written.
Updated 14 October 2026: the clips carry no on-screen caption since G4, so "labelled as such" was replaced by a pointer
to the AI disclosure, and the three outcome clips (#466) are named. This is the text for the Content Survey's mature
content description; the per-topic answers are in [coming-soon-kit.md, section 5](coming-soon-kit.md#5-content-survey-draft-answers).

> Statecraft: Israel depicts real events of 1948-1951 from the perspective of Israel's government, including war,
> terror attacks, refugees and displacement, and references to the Holocaust. These are shown through sourced text,
> archival photographs and a symbolic map, not graphic violence. Some decision cards can also show a short, silent,
> AI-generated illustration (disclosed in the AI section of this page), and three historical orders play a short
> silent clip when the turn ends. Real historical figures appear in their public roles; their in-game ratings are game
> balance, not judgements of the people.

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

Updated 14 October 2026 (P57): the capsules, library art and screenshots now exist. They are listed, with Valve's
current sizes and their sources, in [coming-soon-kit.md](coming-soon-kit.md) sections 1 to 3, and kept outside git in
`~/statecraft-release/steam-kit/` (previews in [kit/](kit/)). "Report" means the placement table in the research
report. Files under `promo/demo*` are local, not in the repository, and were listed by name only.

### Capsules and library art

All made from real engine renders with the title in EB Garamond, not from `promo/final/01` (AI): the owner chose real
captures for itch.io, and this keeps the page's art free of AI. Header 920 x 430, small 462 x 174, main 1232 x 706,
vertical 748 x 896, library capsule 600 x 900, library header 920 x 430, library hero 3840 x 1240 (no text) and the
library logo (1280 px wide, transparent). The page background (optional, 1438 x 810) is not made: Steam builds one from
the last screenshot if none is uploaded.

Capsule text is the game title only. Text on an asset must exist in every language the page lists, so the capsules
carry the English title unless a Hebrew variant is made (report). Valve's localisation rule for capsule text is
written for artwork overrides; the game's name is the one text base capsules may carry.

### Screenshots (at least five, 1920 x 1080 or larger, 16:9, real build only)

Eight real-build screenshots at 1920 x 1080, seven proposed as suitable for all ages: coastal plain (gameplay03), the
whole map (diorama02), the centre with the areas coloured by side, Jaffa's boats and the front line close up, the front
line by Ramle, the Budget screen, the "Why?" panel, and the Defence screen (not flagged all ages). See
coming-soon-kit.md section 2. The README's 1568 x 882 shots stay too small. No AI image can go in a screenshot slot: not
`promo/final/01` to `08`, and not `docs/images/hero-concept-1936.jpg`.

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
6. **D-D, the start-screen painting.** Decided (owner, 2026-10-14, PR #565): Dreamina output is approved for free,
   non-commercial distribution and CC0 marking. The game is free (O2), so the painting can ship on the Coming Soon page
   and in the free build. **Paid-release caveat, still standing:** before any paid release, the owner confirms
   commercial rights for the Dreamina plan used, or replaces the image.
7. **Free or paid.** Decided (owner, 2026-10-14, O2): "free". The Steam setup is "This is a free product". README.md
   and TRADEMARK.md say the game is free and non-commercial (#521).
8. **Languages.** English only, or Hebrew listed as "Game Support Only" as the report suggests, given that Hebrew is
   not enabled in the build.
9. **Desktop build.** Needed for a Steam release, not for the page. The plan is
   `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (issue #482). Its decisions are in
   DECISIONS.md; O9 (how testers get builds) and O11 (the Steamworks wrapper) are still open.
10. **D13 and D-A.** Decided (owner, 2026-10-14, PR #565). D13: the land-use, biome and route bakes are CC BY-SA 4.0.
    D-A: the armistice lines are accepted, and the Jordan features stay marked `low_res_scan`. Neither touches the store
    text. D-P (the four remaining GPO portraits) and D-R (a) are still open.
