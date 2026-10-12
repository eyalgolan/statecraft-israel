[English](README.md) · **עברית**

<div align="center">

https://github.com/user-attachments/assets/56571db8-446b-444b-8db9-fce57dac53a7

<em>הטריילר הקולנועי (1:23, עם קול), שנוצר בכלי וידאו של בינה מלאכותית מאמנות הקונספט שלנו; אינו צילום מתוך המשחק.</em>

# Statecraft: Israel

</div>

<div dir="rtl">

*אמנות קונספט שנוצרה בבינה מלאכותית: המראה שאליו אנחנו שואפים, בתמונה שמתרחשת ב־1936, לפני נקודת הפתיחה של המשחק ב־1948. זו אינה הגרסה הנוכחית, והיא מציגה דברים שאין עדיין במשחק. צילומי מסך אמיתיים מהגרסה הנוכחית מופיעים למטה.*

**משחק אסטרטגיה מבוסס־תורות על ניהול מדינת ישראל. אתם ראש הממשלה ב־15 במאי 1948. השרים אמיתיים, המפה אמיתית, ולכל עובדה היסטורית יש מקור.**

העמוד הזה תמצית של [<span dir="ltr">README.md</span>](README.md). הנוסח האנגלי הוא המחייב, ואם יש הבדל בין השניים, האנגלי קובע. תיקוני לשון לעמוד הזה יתקבלו בברכה.

</div>

<div align="center">

<img src="docs/images/build-m3-map.jpg" width="820" alt="הגרסה הנוכחית 0.1.0 (M3): מפת השטח עם הסרגל העליון וכרטיס אירוע.">
<br>
<img src="docs/images/build-m3-cabinet.jpg" width="405" alt="הגרסה הנוכחית 0.1.0 (M3): מסך הקבינט.">
<img src="docs/images/build-m3-why.jpg" width="405" alt="הגרסה הנוכחית 0.1.0 (M3): לוח 'למה זה קרה?'.">

</div>

<div dir="rtl">

<p align="center"><em>צילומי מסך אמיתיים מגרסה 0.1.0 (M3), 2026-10-11.</em></p>

</div>

> [!NOTE]
> <div dir="rtl">Statecraft: Israel הוא פרויקט קוד פתוח עצמאי, ומתוכננת לו גרסה מסחרית. אין לפרויקט קשר ל־Firaxis, ל־2K, למדינת ישראל או לאדם כלשהו המופיע בו, והם אינם תומכים בו. המשחק עוסק במלחמה, בטרור ובשואה כאירועים היסטוריים. המסגור שלו ישראלי, לכל עובדה יש מקור, ואף ציטוט אינו בדוי. לפרטים: <a href="docs/CONTENT_POLICY.md">מדיניות התוכן ההיסטורי</a>.</div>

<div dir="rtl">

##### [דיווח על באג](https://github.com/eyalgolan/statecraft-israel/issues/new?template=bug_report.yml) | [דיווח על טעות היסטורית](https://github.com/eyalgolan/statecraft-israel/issues/new?template=historical_accuracy.yml) | [דיונים](https://github.com/eyalgolan/statecraft-israel/discussions) | [יומן שינויים](CHANGELOG.md)

## על מה המשחק?

<img src="docs/images/hero-concept-1936.jpg" width="820" alt="אמנות קונספט שנוצרה בבינה מלאכותית של המראה שאליו שואפים, ב־1936. זו אינה הגרסה הנוכחית.">

אתם יושבים בכיסא ראש הממשלה מיום הקמת המדינה. בכל תור אתם ממנים ומחליפים שרים אמיתיים בתוך קואליציה אמיתית, מחלקים תקציב זעום, מנהלים את הביטחון על סולם הסלמה, ומכריעים בהחלטות אמיתיות במועדן האמיתי. מתחת לפני השטח רצה סימולציה דטרמיניסטית ב־C#: אותן בחירות תמיד נותנות אותה תוצאה. המפה נבנתה מנתוני גובה ואקלים אמיתיים.

## מה המצב היום?

***אב־טיפוס מוקדם: אבן הדרך M3 הושלמה, והשנים 1948 עד 1951 ניתנות למשחק כשבונים מקוד המקור.*** עדיין אין גרסה שאפשר לשחק בה בדפדפן בלחיצה אחת; גרסה מתארחת מתוכננת לאבן הדרך M4. הוראות ההרצה המקומית (באנגלית): [<span dir="ltr">BUILDING.md</span>](BUILDING.md). הבנייה דורשת Unity בגרסה 6000.3.25f1.

## פתיחה ראשונה ב־Unity

סצנת המשחק היא <span dir="ltr">`Assets/Map.unity`</span>, והיא נפתחת אוטומטית. אם זה לא קרה (רואים סצנה ריקה בשם Untitled), לחצו פעמיים על Map בתיקיית Assets בחלון Project. אחר כך לחצו Play.

פתרון בעיות:

- השגיאה <span dir="ltr">`error CS0246 'PlayerPrefsBool'`</span> פירושה שחבילת Unity-MCP (<span dir="ltr">`com.ivanmurzak.unity.mcp`</span>) הותקנה בלי מאגר OpenUPM שלה (<span dir="ltr">`https://package.openupm.com`</span>), ולכן חסרה התלות <span dir="ltr">`extensions.unity.playerprefsex`</span>. שחזרו מ־git את <span dir="ltr">`Packages/manifest.json`</span> ואת <span dir="ltr">`Packages/packages-lock.json`</span>; החבילה והמאגר כבר מוגדרים בהם.
- כ־2,000 שגיאות <span dir="ltr">`error CS0234`</span> מ־<span dir="ltr">`com.ivanmurzak.unity.mcp`</span> בפתיחה ראשונה, והרצת batch שנתקעת אחריהן: Unity רץ כשבסביבה מוגדר <span dir="ltr">`CI=true`</span>, <span dir="ltr">`GITHUB_ACTIONS=true`</span> או <span dir="ltr">`TF_BUILD=true`</span>, ולכן התוסף דילג על הורדת ה־NuGet אבל הוסיף את ההגדרות שלו. הריצו <span dir="ltr">`git checkout -- unity/ProjectSettings/ProjectSettings.asset`</span> והפעילו את Unity שוב עם <span dir="ltr">`-CI false -GITHUB_ACTIONS false -TF_BUILD false`</span>.
- שגיאות קומפילציה מחבילה שאינה ב־<span dir="ltr">`Packages/manifest.json`</span> שלנו: הסירו את החבילה מקובץ ה־manifest המקומי שלכם או עדכנו אותה. Unity אינו יכול לרוץ כל עוד יש שגיאת קומפילציה.
- ב־macOS מופיעה ההודעה "Unity is damaged and can't be opened. Move to Trash": זה יכול לקרות כשמפעילים את העורך מחוץ ל־Unity Hub אחרי ש־Hub התקין מודולים (כמו Mac Build Support) בתוך האפליקציה. לחצו **Cancel**, ולעולם לא Move to Trash. פתחו את הפרויקט דרך Unity Hub. אם גם Hub מסרב, התקינו מחדש את גרסת העורך (6000.3.25f1) דרך Unity Hub.

## היסטוריה, מקורות ודיוק

- לכל אירוע, שר, תאריך ומספר יש מקורות ותאריך בדיקה. ויקיפדיה (באנגלית ובעברית) היא מקור מצוטט מרכזי לעובדות רבות, ולכן תוכן המשחק (`content`) מופץ ברישיון CC BY-SA 4.0.
- המודל שמאחורי המשחק והמסגור שלו מבוססים על הנחות. המשחק אינו ניטרלי, והעמוד הזה אומר זאת במפורש.
- אף ציטוט לא מיוחס לאדם אמיתי אלא אם הוא מתועד מילה במילה.
- אירועים על מלחמה, טרור ושואה מסומנים כרגישים. הם עוברים בדיקת רגישות לפני שמשתפים גרסה כלשהי, וכלי הבנייה מסרבים לארוז עד שהבדיקה נרשמה. סקירה של היסטוריונים מתוכננת לאבן הדרך M4.
- מצאתם טעות? מלאו את טופס [טעות היסטורית](.github/ISSUE_TEMPLATE/historical_accuracy.yml) וצרפו מקור, כי אי אפשר לפעול על טענה בלי מקור. הכללים המלאים: [<span dir="ltr">docs/CONTENT_POLICY.md</span>](docs/CONTENT_POLICY.md).

## איך משתתפים

מתכנתים, היסטוריונים, אמנים, מעצבי משחק, כותבים ומתרגמים: התחילו ב־[<span dir="ltr">CONTRIBUTING.md</span>](CONTRIBUTING.md) (באנגלית). להוספת אירוע היסטורי ראו [<span dir="ltr">docs/WRITING_EVENTS.md</span>](docs/WRITING_EVENTS.md) (באנגלית). אפשר לעבוד גם עם בינה מלאכותית, בתנאי שמצהירים על כך ושכל טענה היסטורית נבדקה מול מקור. אין הסכם CLA: תרומות נכנסות תחת הרישיונות של המאגר. מחפשים משימה ראשונה? ראו את הסינון [good first issue](https://github.com/eyalgolan/statecraft-israel/labels/good%20first%20issue), את [help wanted](https://github.com/eyalgolan/statecraft-israel/labels/help%20wanted) ואת [הבעיות הפתוחות](https://github.com/eyalgolan/statecraft-israel/issues).

### עבודה עם סוכן בינה מלאכותית ב־Unity (<span dir="ltr">Unity-MCP</span>)

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (רישיון Apache-2.0) היא הדרך המקובלת בפרויקט לתת לסוכן בינה מלאכותית (Claude Code, Cursor, Copilot ואחרים) לעבוד בעורך של Unity. היא רצה מקומית, בלי חשבון ובלי ענן. הפירוט המלא (באנגלית) ב־[<span dir="ltr">CONTRIBUTING.md</span>](CONTRIBUTING.md).

- **התקנה:** אין מה להתקין. החבילה (<span dir="ltr">`com.ivanmurzak.unity.mcp`</span> בגרסה 0.93.3) ומאגר OpenUPM שהיא צריכה כבר נמצאים ב־<span dir="ltr">`unity/Packages/manifest.json`</span>. בפתיחה הראשונה התוסף מוריד את קובצי ה־DLL שלו מ־nuget.org ואת השרת המקומי שלו, <span dir="ltr">`gamedev-mcp-server`</span>, ו־Unity מקמפל פעם נוספת.
- **פתיחה:** <span dir="ltr">Window → AI Game Developer — MCP</span>.
- **חיבור: Custom, נקבע בפרויקט.** <span dir="ltr">`UnityMcpGuard`</span> מחזיק את התוסף במצב Custom (מקומי) בכל עורך, ולכן Cloud מנוטרל. אם בוחרים Cloud בחלון, ההגדרה חוזרת ל־Custom כשהתוסף שומר ושום דבר לא מתחבר לענן, אבל החלון עלול להמשיך להציג Cloud (עם אזור ההתחברות שלו) והשרת המקומי נעצר: פתחו את החלון מחדש ולחצו Start. לעולם אל תתחברו לשום חשבון; הפרויקט לא צריך חשבון ai-game.dev. המשתנה <span dir="ltr">`UNITY_MCP_CONNECTION_MODE`</span> של התוסף היה עוקף את זה, והפרויקט לא תומך בכך. קובץ ההגדרות האישי, <span dir="ltr">`unity/UserSettings/AI-Game-Developer-Config.json`</span> (מחוץ ל־git, ויש בו גם הטוקן המקומי שלכם), עשוי עדיין להראות Cloud: בחירת הפרויקט מוחלת בזיכרון ולא נכתבת לקובץ, אז אל תערכו אותו ידנית.
- **כתובת השרת:** השאירו אותה כפי שהחלון מציג, עם תעבורה http. הפורט נגזר מנתיב הפרויקט, כך שלכל שכפול ו־worktree יש שרת משלו. עורך נוסף שמוצא שרת שכבר רץ על הפורט שלו מתחבר אליו, ולכן שני פרויקטים לא חולקים שרת כל עוד הפורטים שלהם שונים.
- **הרשאה: token, נקבע בפרויקט.** השרת המקומי בודק טוקן Bearer (הטוקן המקומי שלכם). לעולם אל תבחרו "Account sign-in" (oauth): הוא גורם לשרת המקומי לאמת טוקנים ברשת מול ai-game.dev.
- **השרת המקומי עולה עם העורך.** אם אינכם עובדים עם סוכן, כבו את *Keep server running* בחלון.
- **הסוכן:** בחרו את הסוכן שלכם ולחצו Configure, או לחצו Generate כדי ליצור קובצי skills. Configure כותב את כתובת השרת ואת כותרת ה־Bearer אל <span dir="ltr">`unity/.mcp.json`</span> (עבור Claude Code); הפעילו את הסוכן בתיקייה <span dir="ltr">`unity`</span> או הפנו אותו לקובץ הזה. הקבצים שנוצרים (<span dir="ltr">`.mcp.json`</span> ותיקיות הסוכנים) נמצאים מחוץ ל־git; אל תוסיפו אותם.
- **הכללים בתוקף:** כללי המשחק נשארים בסימולציה ולא ב־Unity, בדיקות קודם, מקור לכל עובדה, ומצהירים בבקשת המיזוג על הסיוע של בינה מלאכותית.
- **אבטחה:** השרת המקומי מאפשר לסוכן להריץ פעולות בעורך, כולל עריכת קבצים והרצת קוד. הוא מאזין רק לכתובות המקומיות ודוחה דפי דפדפן מאתרים אחרים, והטוקן מונע מתוכנות מקומיות אחרות להשתמש בו. השאירו אותו מקומי ולעולם אל תחשפו את הפורט שלו החוצה.
- **אין חיבור?** Connect עובד רק בעורך אינטראקטיבי: במצב batch (בדיקות ובנייה) התוסף מנותק בכוונה, והחלונות שלו לא נפתחים. ודאו שהשרת מסומן Running על הפורט שבכתובת השרת, ולחצו Connect. הניסיון הראשון יכול להיכשל בזמן שהשרת עולה; התוסף מנסה שוב. אם העורך נעצר בלי יציאה מסודרת, <span dir="ltr">`gamedev-mcp-server`</span> עלול להישאר על הפורט: עצרו אותו בכפתור Stop בחלון או לפי ה־PID שלו.
- **מצב batch ובנייה:** <span dir="ltr">`UnityMcpGuard`</span> (<span dir="ltr">`unity/Assets/Editor/UnityMcpGuard.cs`</span>) מונע מהתוסף להתחבר, להפעיל את השרת או לפתוח חלון במצב batch (בדיקות ובנייה), ומוציא את הרכיבים שלו מכל בניית נגן. ערכי הגיבוב (hash) של בדיקות ה־golden בגרסת הדפדפן לא משתנים. הריצו Unity במצב batch עם <span dir="ltr">`-CI false -GITHUB_ACTIONS false -TF_BUILD false`</span> (כמו בכל הפקודות במסמכים): התוסף מתייחס ל־<span dir="ltr">`CI`</span>, <span dir="ltr">`GITHUB_ACTIONS`</span> או <span dir="ltr">`TF_BUILD`</span> שערכם <span dir="ltr">`true`</span> כאל CI (כל שרת CI מגדיר לפחות אחד מהם, וכך גם חלק מסביבות הסוכנים), ואז הוא מדלג על הורדת ה־NuGet אבל עדיין מוסיף את ההגדרות שלו, ושכפול חדש לא מתקמפל. חלון העדכונים כבוי לכל הצוות (<span dir="ltr">`unity/ProjectSettings/AI-Game-Developer-UpdateSettings.asset`</span>); עדכוני גרסה מגיעים בבקשת מיזוג.
- **שינוי שלא מוסיפים ל־git:** אחרי שקובצי ה־DLL במקומם, התוסף מוסיף את <span dir="ltr">`UNITY_MCP_READY;UNITY_MCP_DEPS_7`</span> להגדרות הסקריפטים ב־<span dir="ltr">`unity/ProjectSettings/ProjectSettings.asset`</span>. אל תוסיפו את השינוי הזה ל־commit: בשכפול חדש ההגדרות יגיעו לפני קובצי ה־DLL והתוסף לא יתקמפל. בדיקת מדיניות חוסמת אותו. הניקוי שאחרי בנייה (<span dir="ltr">`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`</span>) מוחק גם את ההגדרות; התוסף מחזיר אותן בפתיחה הבאה, במחיר קומפילציה נוספת אחת.
- **מצב אישי שהתוסף שומר:** מלבד קובץ ההגדרות, הוא שומר את <span dir="ltr">`Unity-MCP.MainWindow.Initialized`</span> ואת <span dir="ltr">`Unity_MCP_SelectedAiAgent`</span> ב־PlayerPrefs של העורך בפרויקט הזה, ואת ה־PID של השרת ואת מצב בדיקת העדכונים ב־EditorPrefs.

## שימוש בבינה מלאכותית

הפרויקט נבנה בסיוע נרחב של בינה מלאכותית. קוד נכתב עם סוכני קוד ונבדק בבדיקות ובסקירות קוד; תמונות הקונספט והפרומו הן תוצר של בינה מלאכותית, סמל הלשונית של דף המשחק צויר בסיועה, וטקסט האירועים נוסח בסיועה וכל עובדה בו נבדקת מול המקור המצוטט. הפורטרטים הם צילומים בנחלת הכלל או ברישיון CC0, ואין עדיין סאונד. דבר אינו נוצר בזמן אמת במהלך המשחק.

## רישיון

קוד: MIT. תוכן המשחק (`content`): CC BY-SA 4.0. מסמכים, נתונים ואמנות מקוריים: CC BY 4.0. תמונות שנוצרו בבינה מלאכותית: CC0, ללא טענת זכויות. הפירוט: [<span dir="ltr">LICENSING.md</span>](LICENSING.md).

על בעיות אבטחה מדווחים באופן פרטי, ראו [<span dir="ltr">SECURITY.md</span>](SECURITY.md). קוד ההתנהגות: [<span dir="ltr">CODE_OF_CONDUCT.md</span>](CODE_OF_CONDUCT.md).

</div>
