עדיין אין גרסה מתארחת. גרסת דפדפן שאפשר לשחק בה בלחיצה אחת מתוכננת לאבן הדרך M4. עד אז מריצים את המשחק מקוד המקור, במחשב שלכם.

## להריץ בעצמכם

1. התקינו את הכלים שב־[<span dir="ltr">docs/dev-setup.md</span>](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md) (באנגלית): ‏<span dir="ltr">.NET 10</span>, ‏<span dir="ltr">Unity 6000.3.25f1</span> עם Web Build Support, ו־<span dir="ltr">`uv`</span>.
2. הריצו את <span dir="ltr">`tools/build_sim_for_unity.sh`</span>.
3. בנו את גרסת הדפדפן. הפקודה נמצאת ב־[<span dir="ltr">docs/dev-setup.md</span>](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md).
4. הריצו <span dir="ltr">`python3 -m http.server 8000 --directory unity/Builds/Web-disabled`</span>, ופתחו את <span dir="ltr">http://localhost:8000/</span>.

הפקודות המדויקות, הבדיקות ובדיקת הדפדפן נמצאות ב־[<span dir="ltr">BUILDING.md</span>](https://github.com/eyalgolan/statecraft-israel/blob/main/BUILDING.md) (באנגלית).

## במה אפשר לשחק

תורות מ־15 במאי 1948 ועד סוף 1951: מסך הפתיחה, מפת 1948 עם העיירות, הכפרים, המסילות, הדרכים וקווי שביתת הנשק, מסך הממשלה, מסכי התקציב והביטחון, אירועים עם מקורות במועדם האמיתי, לוח "למה?" ושמירה אוטומטית. היישובים והעצים הם עדיין אמנות זמנית. אין עדיין דיפלומטיה, ואף אחת מהמערכות החדשות של ה־GDD (תורות חודשיים, יחידות בפיקודכם, ערים, בנייה, קרבות) אינה בגרסה.

## לתורמים: איך מעלים את המשחק לאתר הזה

האתר מוגש מ־GitHub Pages, שאינו יכול לקבוע כותרות תגובה (response headers). זה קובע איך צריך לבנות את גרסת הדפדפן לפני שאפשר להעלות אותה לכאן:

- **‏Decompression Fallback.** גרסאות Brotli ו־gzip של Unity דורשות שהשרת ישלח <span dir="ltr">`Content-Encoding`</span>, ו־GitHub Pages לא יכול. גרסה לאתר הזה צריכה את ה־Decompression Fallback של Unity: הקבצים הדחוסים מקבלים את הסיומת <span dir="ltr">`.unityweb`</span>, והטוען פורס אותם ב־JavaScript, במחיר טוען גדול יותר. ‏Unity ממליצה על כך כשאי אפשר להגדיר את השרת ([המדריך של Unity, באנגלית](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-deploying.html)). <span dir="ltr">`Build.Web`</span> שב־<span dir="ltr">`unity/Assets/Editor/Build.cs`</span> מדליק את האפשרות עם <span dir="ltr">`-decompressionFallback`</span> (כבויה כברירת מחדל, כי itch.io מגיש קובצי <span dir="ltr">`.br`</span> כראוי); <span dir="ltr">`docs/RELEASING.md`</span> בונה איתה את גרסת ה־Pages.
- **תהליכון יחיד.** גרסאות Web מרובות תהליכונים דורשות את הכותרות COOP ו־COEP, וגם אותן GitHub Pages לא שולח. הגרסאות של הפרויקט בנויות היום בתהליכון יחיד (<span dir="ltr">`webGLThreadsSupport: 0`</span>), וכך הן צריכות להישאר בשביל האתר הזה.
- **גרסה לשיתוף.** מותר לפרסם רק גרסה שנבנתה עם <span dir="ltr">`-shareable`</span>: היא מסרבת להיבנות כל עוד לאירוע או לצילום רגיש חסרה בדיקת רגישות ([<span dir="ltr">docs/m3-release.md</span>](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/m3-release.md), באנגלית).
- **לא נשמרת ב־git.** גרסת M3 שקלה 23 מגה־בייט ב־51 קבצים, ושום קובץ במאגר לא יכול לעבור 10 מגה־בייט. תהליך הפריסה מוסיף את גרסת ה־Pages של המהדורה האחרונה לאתר תחת <span dir="ltr">`/demo/`</span> בזמן הפריסה, לא מתוך git.
