# Steam "Coming Soon" kit (issues #424, #45)

**PREPARED, NOT SUBMITTED.** Nothing here was uploaded, and no Steamworks account, app, store page or Content Survey
answer exists. Every Steamworks step is the owner's (section 7). This kit clears nothing: not the name, not an asset,
not the content.

Prepared on 14 October 2026 from `main` at `c0a38256` (P57). The text for the page is in
[store-page-draft.md](store-page-draft.md); this file holds the media, the AI-disclosure fields, the draft Content Survey
answers, the tags and platforms, and the owner's checklist.

## 1. Capsules and library art

**Where the files are.** The full kit is about 50 MB, so it is kept **outside git** in
`~/statecraft-release/steam-kit/` on the owner's Mac (manifest in section 3). Small previews are committed in
[kit/](kit/): the header capsule, the small capsule, the library logo and a contact sheet of the screenshots.

**How they were made.** Every image is real engine output; none is AI. The art is two renders of the game's own map
scene (`Assets/Map.unity`, built by `MapCapture`'s scene setup) at 3840 x 2160 on the PC quality tier, with the units
in play on the first turn and the map's borders as on 20 July 1948, made on `main` at `c0a38256` with a temporary
editor method that calls `MapCapture`'s own scene build and `Save` (`sources/SteamKitCapture.cs` in the kit folder; it
is not committed, and was deleted from the worktree before the WebGL build). Editor renders carry no UI or labels, so
the only text on any capsule is the title. Camera poses (longitude, latitude, distance, yaw):

- `telaviv40` (34.85, 32.05, 40 km, 10 degrees): Tel Aviv, Jaffa and its boats, the railway and the front line. Used
  for the header, small, main and vertical capsules, the library capsule and the library header.
- `galilee90` (35.30, 32.75, 90 km, 0 degrees): Haifa with its ships, the Galilee and the Sea of Galilee. Used for the
  library hero.

**The title** is "Statecraft: Israel" in EB Garamond SemiBold, the font the game's UI already ships
(`unity/Assets/UI/Fonts/EBGaramond-SemiBold.ttf`, SIL Open Font License 1.1; "Copyright 2017 The EB Garamond Project
Authors (https://github.com/octaviopardo/EBGaramond12)"; the licence has no Reserved Font Name). Setting text in the font
inside an image is use of the font, not a modified font, so the images are not under the OFL; the licence text sits in
the kit folder as `OFL-EBGaramond.txt` for the credit. Cream (#F6EEDC) on a dark gradient (#0E1622), as the game's UI.
`sources/capsules.py` composes them (Pillow, Lanczos resampling).

**Rules followed** (RELEASING.md and Valve's [Graphical Asset Rules](https://partner.steamgames.com/doc/store/assets/rules),
read 14 October 2026): the title only, no quotes, awards or other text; no text at all on the hero; the logo alone on
transparency; artwork PG-13 (map, towns, boats and unit miniatures).

| Asset | File (kit folder) | Pixels made | Valve's size | Source of the size (read 14 October 2026) |
| --- | --- | --- | --- | --- |
| Header capsule | `capsules/header_capsule_920x430.png` | 920 x 430 | 920px x 430px | [store/assets](https://partner.steamgames.com/doc/store/assets), [store/assets/standard](https://partner.steamgames.com/doc/store/assets/standard) |
| Small capsule | `capsules/small_capsule_462x174.png` | 462 x 174 | 462px x 174px; Steam generates 120x45 and 184x69 | same two pages |
| Main capsule | `capsules/main_capsule_1232x706.png` | 1232 x 706 | 1232px x 706px | same two pages |
| Vertical capsule | `capsules/vertical_capsule_748x896.png` | 748 x 896 | 748px x 896px | same two pages |
| Library capsule | `library/library_capsule_600x900.png` | 600 x 900 | 600px x 900px | [store/assets](https://partner.steamgames.com/doc/store/assets), [store/assets/libraryassets](https://partner.steamgames.com/doc/store/assets/libraryassets) |
| Library header | `library/library_header_920x430.png` | 920 x 430 | 920px x 430px | same two pages |
| Library hero | `library/library_hero_3840x1240.png` | 3840 x 1240, PNG, no text | 3840px x 1240px .png; safe area 860px x 380px at the centre | same two pages |
| Library logo | `library/library_logo_1280w.png` | 1280 x 183, PNG with transparency | "Either 1280px wide and/or 720px tall (.png)", transparent background | same two pages |
| Shortcut icon (Community; one field) | `community/shortcut_icon_256x256.png` (alternative: the monogram .ico below) | 256 x 256 PNG | "256px x 256px or 512px x 512px, ICO or PNG"; Steam generates the .ico from a PNG | [store/assets/community](https://partner.steamgames.com/doc/store/assets/community) |
| App icon (Community) | `community/app_icon_184x184.jpg` | 184 x 184 JPG | 184px x 184px .jpg | same page |
| Shortcut icon, alternative for the same field (monogram) | `community/monogram/monogram.ico` (16, 24, 32, 48, 64, 128 and 256 px; `monogram_<n>.png` at each size) | 16 to 256 | as above: one Shortcut Icon field, ICO or PNG | [store/assets/community](https://partner.steamgames.com/doc/store/assets/community) |

Valve's overview page also says, under "August, 2024", that "Old dimensions are no longer accepted"; the sizes above
are the current ones on that page. The two Community icons, which Valve lists as Required, are made the same way: a
square crop of `telaviv40` (dimmed) with the title on two lines (`sources/icons.py`). The title reads at 256 and 184 px,
but not at the 32 and 48 px sizes a Windows shortcut uses.

**The monogram (owner's choice, 15 October 2026: "A simple monogram").** For those small sizes the kit has an "S" in EB
Garamond SemiBold in the map's land tan on the map's sea blue, with a darker rim so the tile reads on light and dark
backgrounds (`sources/monogram.py`). The colours are taken from the game's own palette on `main` at `d304661a`: the sea
is `ScDeepSrgb` (0.27, 0.40, 0.54) in `unity/Assets/Shaders/StatecraftMap.hlsl`, sRGB (69, 102, 138); the land is the
"desert" biome colour (0.80, 0.68, 0.48) in `unity/Assets/Scripts/Map/MapBuilder.cs`, sRGB (204, 173, 122); the rim
is the sea at 70 %. Each size is drawn on its own pixel grid (8x supersampled, then box-reduced), not shrunk from 256.
Checked at 1:1 on light and dark backgrounds: the "S" reads at 16, 24, 32 and 48 px. Valve lists no small sizes of
its own beyond the 256 x 256 shortcut icon and says it generates a .ico from a PNG; uploading `monogram.ico` instead
gives Windows the monogram at every shortcut size. Steam has one Shortcut Icon field, so the owner uploads either the
title PNG or the monogram .ico, not both. **macOS:** Valve's Community assets page (read 15 October 2026) says a
macOS desktop shortcut needs "an icon file in ICNS format in the 'Mac Icon' asset field", or it shows Steam's default
icon. The kit has no ICNS yet; `sources/monogram.py` could produce the sizes for one if the owner wants the monogram
there too. Not in the kit: the optional page background (1438 x 810) and the
event assets.

**Legibility at the small display sizes.** The small capsule sets the title on two lines so that it nearly fills the
capsule, as Valve's page advises, over a dimmed background. Previews at Steam's generated sizes,
`capsules/preview_small_120x45.png` and `preview_small_184x69.png`, were checked by eye: both words read at 184 x 69,
and at 120 x 45 "Statecraft:" and "Israel" are still distinct. The owner should look again in Steam's own preview.

## 2. Screenshots

Captured from the game's WebGL build of `main` at `c0a38256` (the Unity 6000.3.25f1 build `Build.Web`, compression off,
not `-shareable`; `index.html` SHA-256 `e7f89d3fa7f705d01b7290c82f49d6a857a3ce496a39bcfac4fab06473aaade9`) in Chrome
for Testing at a 1920 x 1080 viewport, DPR 1, on 14-15 October 2026. Two came from the established `?shot` route
(`tools/web_check.py --shots`, the art checkpoint's poses), in the same run that printed both `GOLDEN OK` lines. The
rest came from a headed run after `?smoke=5` (5 turns, to 19 June 1948, `SMOKE OK ... d3fdd459...`, the pinned value),
driven over CDP with `tools/headed.py`'s `Browser` (`sources/capture.py` and its step files). All are real gameplay
with the game's UI. **RELEASING.md asks for captures from the release build with its zip SHA-256**: this build is
`main` four commits after M2's `v0.1.0-rc.0` (`411b1124`), including the two-colour borders (#555) that the
screenshots show; re-capture from the
release build before upload if the owner holds to that line strictly.

`uv run tools/store_screenshots.py` on all eight: `DIMENSIONS OK: count and image dimensions only, not gameplay
provenance, rights or age suitability`.

No screenshot shows the Cabinet screen, a minister's portrait other than David Ben-Gurion's (public domain, NARA) and
Yaakov Dori's (CC0), the start screen's AI painting, an AI clip, or a sensitive event card. The Budget screen names
ministers in text only.

| # | File (`screenshots/`) | What it shows | All ages |
| --- | --- | --- | --- |
| 1 | `01-gameplay03-tel-aviv-1948-05-15.png` | The coastal plain from Tel Aviv and Jaffa to Rehovot on 15 May 1948: boats, the railway, the HUD and minimap (`?shot`, pose gameplay03) | **Yes** |
| 2 | `02-diorama02-country-1948-05-15.png` | The whole map on its wooden frame, 15 May 1948 (`?shot`, pose diorama02) | **Yes** |
| 3 | `03-central-front-1948-06-19.png` | The centre of the country on 19 June 1948, with the areas coloured by who held them, unit tokens and the legend | **Yes** |
| 4 | `04-jaffa-boats-borders-close.png` | Close view of Jaffa's harbour and boats, Tel Aviv and the blue and orange-red front line | **Yes** |
| 5 | `05-front-lines-ramle-close.png` | Close view of the front line by Ramle and Lydda, with the miniature rifle units | **Yes** |
| 6 | `06-ui-budget.png` | The Budget screen | **Yes** |
| 7 | `07-ui-why-panel.png` | The "Why did this happen?" panel | **Yes** |
| 8 | `08-ui-defence.png` | The Defence screen: Ben-Gurion and Dori, the escalation ladder at "Full war", the reserve call-up | **No** (shows no violence, but its subject is the war level; left unflagged as Valve's guidance and RELEASING.md advise when in doubt) |

Seven are marked suitable for all ages, above Valve's "at least four". Valve: "Suitable screenshots should not contain
gore, violence, or suggestive themes" ([store/assets/standard](https://partner.steamgames.com/doc/store/assets/standard),
read 14 October 2026). The flags are this kit's proposal; the owner sets them in Steamworks. For the owner's
all-ages call:

- Screenshots 3 to 5 show miniature soldier figures and front lines, with no fighting. The owner may prefer to unflag
  them.
- Screenshot 7 (the "Why?" panel) lists "New: The Altalena" under Events. It is only the event's title, with no image
  or text from the card, but the Altalena event carries the sensitive `war-casualties` tag.

## 3. Media manifest (files outside git)

Folder: `~/statecraft-release/steam-kit/` on the owner's Mac.

**Licences.** Every file derived from an engine render (the capsules, the library capsule, header and hero, the
small-capsule previews, the two Community icons, the screenshots and the two source renders) is **CC BY-SA 4.0**. That is a licence fact, not a
choice (coordinator ruling, 15 October 2026): the renders show the land-use, biome and route bakes
(`unity/Assets/Textures/Map/landuse.png`, `unity/Assets/Resources/Map/BiomeColours.bytes`,
`unity/Assets/Resources/Routes/RouteRibbons.bytes`), which D13 (PR #565) made CC BY-SA 4.0. The library logo has no
render in it: it is the title set in EB Garamond on transparency, so it is **CC BY 4.0**. The monogram icons have
no render pixels either (the font and two palette colours), so they are **CC BY 4.0** too. The font is OFL-1.1; it is
credited, and the images are not under the OFL. The scripts in `sources/` are MIT.

CC BY-SA 4.0 asks two things of anyone who shares these images, Valve's store included:

- **Attribution**, with the licence and a link to it, and a note that the images are adapted from the game. Text to use,
  in the store page's legal notes and wherever the images are shared outside Steam:

  > Store images: in-game captures and renders of Statecraft: Israel, (c) 2026 Eyal Golan and Statecraft: Israel
  > contributors, licensed CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/). Map terrain produced using
  > Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under
  > COPERNICUS by the European Union and ESA; all rights reserved. Title set in EB Garamond (SIL Open Font License 1.1).

- **Share-alike**: anyone who adapts the images (a cropped capsule, a press kit, a fan edit) must license the result
  under CC BY-SA 4.0 too. Uploading them unchanged to Steam is sharing, not adapting; the attribution line above
  covers it. Steam's own resized copies (the 120 x 45 and 184 x 69 small capsules, the half-size library art) are
  technical resizes by the platform.

Sources per file: capsules and library art from `sources/PC-telaviv40.png` (hero: `sources/PC-galilee90.png`);
screenshots as in section 2.

| File | Bytes | Pixels | SHA-256 | Licence |
| --- | --- | --- | --- | --- |
| community/app_icon_184x184.jpg | 16950 | 184x184 | fe89fbe88126c2a9eae11a71c1d39b8243279c1525f47d107b0edaab7d340498 | CC BY-SA 4.0 |
| community/shortcut_icon_256x256.png | 122559 | 256x256 | b37d19ec6b77ded41673d9ba1311e907b127c45cb12daee5423078a997c2f7d7 | CC BY-SA 4.0 |
| community/monogram/monogram.ico | 15173 | 16 to 256 | 3f95ff84cc50a9abf2b3322e4ad78ec2162d36387ed8157aac5f2efd18d44065 | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_128.png | 3197 | 128x128 | 788876452fe03c07a43fd8d87ee5d2016b7a653bf307cf9bab544fb65b8aed4a | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_16.png | 470 | 16x16 | 253cae3382acf9bcb849e4f07a0bd338c259cf17887228c3fac37bf422a19cae | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_24.png | 678 | 24x24 | 01ab2f8c6206a96cd96f0432589ab9962082a0da0a1d2a9b1b6dfa179444f49f | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_256.png | 6484 | 256x256 | 516546295ea7807aff9213fb458dbe28450b452f10ffbbc2e21c14e5f02b5fe1 | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_32.png | 881 | 32x32 | af3c9dada4af535d20a10884c649272883b120b98bd9a589eb891716fe7d2a3b | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_48.png | 1277 | 48x48 | cf234617ee958db154fd3ee0f2496a53b29f865e61b1e80f1fc5a8e74a2ce075 | CC BY 4.0 (font OFL-1.1, credited) |
| community/monogram/monogram_64.png | 1649 | 64x64 | ced9f026060eb8a7f6a743042e6ece041668cc57eb5af7bfb50b75621eb46296 | CC BY 4.0 (font OFL-1.1, credited) |
| capsules/header_capsule_920x430.png | 681357 | 920x430 | e2d4522ce4a0c7615eeb0a77d55e2abbcc94b62b3c566b3f09043b73f0bb43a8 | CC BY-SA 4.0 |
| capsules/main_capsule_1232x706.png | 1342138 | 1232x706 | 18b6e75f5319fa49931029b42b695a41c122d4a7448083c59f714cbe1ff44a48 | CC BY-SA 4.0 |
| capsules/preview_small_120x45.png | 13021 | 120x45 | 60f209b338a474870ecf1cf4c46c1f3f4176777972e3c28579e73cd7a787cfb7 | CC BY-SA 4.0 |
| capsules/preview_small_184x69.png | 28120 | 184x69 | 63adcfa8ad2c1959a0e39fd13ffa7146169ea3cd7f8c24b21bc1c6fa963efde6 | CC BY-SA 4.0 |
| capsules/small_capsule_462x174.png | 128087 | 462x174 | a9b73f184e0cc0372a27aea0a3daa4c4b95bda293bf0c7a1c35e963c96d0020d | CC BY-SA 4.0 |
| capsules/vertical_capsule_748x896.png | 992268 | 748x896 | b424b64c7161ba9910d906e3ad4a8d6e7c9ec45e62dab808ba96c656c0483230 | CC BY-SA 4.0 |
| library/library_capsule_600x900.png | 801421 | 600x900 | 125f5dbfbe5244852e4c23f30a0be363ff5fe9a837df13d9a2ecf55e0a153a93 | CC BY-SA 4.0 |
| library/library_header_920x430.png | 681357 | 920x430 | e2d4522ce4a0c7615eeb0a77d55e2abbcc94b62b3c566b3f09043b73f0bb43a8 | CC BY-SA 4.0 |
| library/library_hero_3840x1240.png | 6265271 | 3840x1240 | 5efbb2ba94696be086211924ad80e6304778417ecf866eea9c2a0147a640bb5b | CC BY-SA 4.0 |
| library/library_logo_1280w.png | 77557 | 1280x183 | a5b86daedadd99f451b9ca2e0dcc2148bdc607ff11e2a8758cd90e97224d5c59 | CC BY 4.0 (font OFL-1.1, credited) |
| screenshots/01-gameplay03-tel-aviv-1948-05-15.png | 2747348 | 1920x1080 | 3e1ffab19fefae4f80958676114ac068a354cf28a52a85728de41d3f56158b6d | CC BY-SA 4.0 |
| screenshots/02-diorama02-country-1948-05-15.png | 2049721 | 1920x1080 | 28506228c0c88e4e6d9186acc5f43771123ea1fb9fb1a176f31d76723a282326 | CC BY-SA 4.0 |
| screenshots/03-central-front-1948-06-19.png | 3082435 | 1920x1080 | ac1333a46303d2fa78091b915d9caa543038d33ec120effca2bcbb8ae874d0bd | CC BY-SA 4.0 |
| screenshots/04-jaffa-boats-borders-close.png | 2642788 | 1920x1080 | 9478704ab7f12b0fcfa0ea5d09415648232b85c5a73ba32c3bbeea8d4d3f1419 | CC BY-SA 4.0 |
| screenshots/05-front-lines-ramle-close.png | 2600200 | 1920x1080 | 7f341613dab8a90d052f671733ebf1a8fb1a5cee8087f8148f72a699c9276355 | CC BY-SA 4.0 |
| screenshots/06-ui-budget.png | 1772390 | 1920x1080 | dbc7c4ac27d3cefdcb075b84704a42379987e3b452b9e62622246d034ccb764d | CC BY-SA 4.0 |
| screenshots/07-ui-why-panel.png | 1748909 | 1920x1080 | fda7a8284471202368abd34dd9be6727661aeb0a92d40b476a1b122a005dae94 | CC BY-SA 4.0 |
| screenshots/08-ui-defence.png | 1415159 | 1920x1080 | 7fc20e61c366a344f27764be2f5b1b26b17a92fa2768b2bb4a063a25baafb63c | CC BY-SA 4.0 |
| sources/PC-galilee90.png | 11835372 | 3840x2160 | 7d87b0beb822d0d724748f72a8ff13d964b4ef47083ea4ff111360e012008b11 | CC BY-SA 4.0 |
| sources/PC-telaviv40.png | 9851346 | 3840x2160 | 7101093fbf6348a9e588f0b6446217832d6f8540aa8fd13e25f04c6d5d4f9de5 | CC BY-SA 4.0 |
| sources/SteamKitCapture.cs | 2635 | - | a57eb366b8e38b0f9eeb253cb92b886310c483ac17c26ca0d45f007c37a33f68 | MIT |
| sources/capsules.py | 5999 | - | 2f79346dc4d426da74b882b52708b1e5baf47c46f107f78d7f7eab869b14ec3d | MIT |
| sources/capture.py | 4367 | - | 728275b9bc35436264897bf9fd3904059c4c0d9401fd4e37a4a1c465900d02f9 | MIT |
| sources/icons.py | 1640 | - | 249683bb8c357ceeb4ed24e9083376eac9def2024c0ce6256688bde4702dd235 | MIT |
| sources/monogram.py | 2923 | - | f8f3965baab4c7058fc7383f032e5b04a1606cccb7c2c7155abdee90818e2800 | MIT |
| sources/steps1.json | 393 | - | c1d6f57eda508aa0f6900ce24077980d7cc198f01ca6b7e8fa16dfb9ce32e057 | MIT |
| sources/steps5.json | 208 | - | 6eea8d0e91be8ae415cf706ddee31b57b9b6cede5370970fb0a2cb8315ad9422 | MIT |
| sources/steps6.json | 209 | - | 716a73fa69e13a4b94ae09870273ad0ed80ee1db8e73b6b5a338a838f4a1c4c8 | MIT |
| OFL-EBGaramond.txt | 4454 | - | 058611bd968817d532cedeab6acaa055e882d365a88523df00e7917ad7a0f704 | OFL-1.1 (licence text) |

The committed previews in [kit/](kit/) are byte-identical copies of `capsules/header_capsule_920x430.png`,
`capsules/small_capsule_462x174.png` and `library/library_logo_1280w.png`, plus `screenshots-contact-sheet.jpg` (the
eight screenshots at half size). Their rows are in ASSETS.yaml.


## 4. AI-content disclosure (Steam fields)

Generated with `uv run scripts/oss/ai_disclosure.py` on `main` at `c0a38256`; the output is identical to the committed
[docs/ai-disclosure.md](../ai-disclosure.md), so that file is the manifest list behind these answers. It lists only rows
marked `ai_generated: true`. Steam's form asks about content that ships and that players consume (artwork, sound,
narrative, localization), not about efficiency tools
([Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey), read 14 October 2026). The
per-field wording of the form is visible only inside Steamworks, so the field names below follow that public page:
**[confirm]** them on the form.

**Owner rulings this text follows:** the clips carry no burned-in caption (G4), while the game's UI labels each one "AI-generated illustration" as it plays (`event.ai_label`, shown by `EventCardPresenter` and `OutcomePresenter`); the four Dreamina clips keep Dreamina's
own small "AI" mark (G5); D-V covers the clips in every build; D-D (PR #565) covers the start-screen painting for the
free game.

**Field: "Pre-Generated" content.** Tick it. Describe it as:

> Statecraft: Israel ships these AI-generated pieces, all made during development:
>
> - The painting behind the start screen (an imagined 1948 hall with no identifiable person), made with an AI image
>   model.
> - 14 short, silent video clips. 11 play on decision cards only when the player presses the card's "Illustration"
>   switch, in place of the card's archival photograph; 3 play once at End Turn when the player has ordered the
>   historical option of the transit camps, the Hula works or Operation Horev. They were made with Veo 3.1 (10 clips)
>   and Seedance 2.0 (4 clips). They show no recognisable real person. While a clip plays, the game labels it
>   "AI-generated illustration". The four Seedance clips keep the generator's small
>   "AI" mark in a corner.
> - Event and content text was drafted with AI. A person checks every fact in it against its cited source, and every
>   fact carries its source in the game's data.
>
> Not AI: the portraits and the event photographs are archival photographs; the terrain is built from real elevation
> and climate data; the only audio is the archival 1948 recording of David Ben-Gurion reading the Declaration (Israel
> State Archives). The game has no music. The code was written with the help of AI coding agents and goes through
> tests and review.

**Field: "Live-Generated" content.** Leave unticked. The game generates nothing with AI while it runs
(`docs/ai-disclosure.md`: "Nothing is generated live while you play"; README.md "AI use"). RELEASING.md: keep this only
while it remains true.

**Store-page media.** None of this kit's media is AI: the capsules, library art and screenshots are engine output with
the title set in EB Garamond. If the owner adds concept art (`promo/final/03`, `05`) to "About this game", or the
cinematic demo2 as a second trailer, add to the description: "Images captioned 'concept art' were made with AI image
tools and show the look we are working towards, not the current build" and "The cinematic teaser was made with AI
video, voice and music tools and is not gameplay." Both still need the decisions listed in store-page-draft.md section 8.

**Items to check before ticking (RELEASING.md):**

- Compare `docs/ai-disclosure.md` with the build that actually ships. The web build's tab icon
  (`unity/Assets/WebGLTemplates/Statecraft/favicon.ico`, AI-assisted drawing) is not in a desktop build; the website's
  files (`site/`) and `promo/` are not in any build.
- An `owner-verify` row is not cleared by appearing here: the start-screen painting's row is still `owner-verify`.
- The Hebrew UI strings (a hand-asked preview, #193): the repository does not record whether they were translated with
  AI. If the owner lists Hebrew, settle this first **[confirm]**.
- The store text itself was drafted by an AI agent for the owner to edit (store-page-draft.md, To confirm 13).

## 5. Content Survey: draft answers

Steam's public page describes three sections, General Content, Mature Content and Generative AI
([Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey), read 14 October 2026). It says the
General Content answers generate ratings for regional boards, the Mature Content answers decide how the game shows
against each customer's preferences, and "You must disclose all the adult content you've uploaded in your builds, even
if it's not accessible or presented in your product." The questions themselves are visible only in Steamworks, so
these drafts are by topic. **They are drafts for the owner; the owner answers the survey** (RELEASING.md).

What ships, as of `c0a38256` (the facts the answers rest on):

- War: the 1948 war through sourced event text, archival photographs and a map with unit tokens that take no orders.
  No combat is shown, no blood, no bodies, no gore. Five events carry the `war-casualties` tag (`ten_days`,
  `operation_yoav`, `operation_hiram`, `operation_horev`, `altalena`); their text names deaths as historical fact.
- Terror: one event tagged `terror` (`bernadotte_assassination`), as text and an archival photograph of the coffin's
  departure, not the killing.
- The Holocaust: one event tagged `holocaust` (`reparations_claim`), as text. No swastikas or Nazi symbols anywhere
  (CONTENT_POLICY section 2, rule 4). The Holocaust is an event the player reacts to, never one the player plays.
- AI clips of military operations (`ten_days`, `operation_yoav`, the `operation_horev` outcome): vehicles and troops at
  a distance, no violence shown; owner-reviewed (G4).
- All seven sensitive events and the sensitive photographs and clips are `sensitivity-reviewed` (D11, G4,
  `event-images.yaml`).
- No sex or nudity, no drugs shown, no gambling, no strong language, no purchases, no user-generated content, no online
  features.

| Topic | Draft answer | Flag |
| --- | --- | --- |
| Violence | Present, **non-graphic and historical**: war, military operations and an assassination are described in text and shown in archival photographs and silent clips with no injury or death on screen. The player orders operations and calls up reserves; no violence is acted out. Not "Frequent Violence or Gore". | **OWNER: sensitive.** Answer from the shipped text and photographs. The casualty figures in event text are factual; if the form asks about "realistic violence" or "violence against humans", the honest answer is that real killings are described, not depicted. |
| Blood and gore | None. | |
| The Holocaust | Present as **historical reference**: one event card (the reparations claim) and the post-war context of immigration. No imagery of the camps, no Nazi symbols. | **OWNER: sensitive.** Mention it in the mature-content description (store-page-draft.md section 6 already does). See the German note below. |
| Terror | Present as **historical reference** (the Bernadotte assassination, 17 September 1948), text and a non-violent archival photograph. | **OWNER: sensitive.** |
| Real-world political conflict | Present and central: the game depicts the Arab-Israeli war of 1948 and its aftermath **from the Israeli government's perspective, and says so**. Contested narratives are not shown as parallel accounts (CONTENT_POLICY section 1). Arab and Palestinian actors are computer-run. Refugees and displacement are referenced. | **OWNER: sensitive.** This is the answer most likely to draw reviewer questions and user reports. Use the store-page-draft.md section 6 description so the framing is disclosed, not discovered. |
| Real people | Real historical figures (ministers, officers) in their public roles, with public-domain, CC0, CC BY or CC BY-SA photographs; ratings are game balance. No living people. | Flag only if the form asks about real people. D-P (four GPO portraits) is a rights question, not a survey one. |
| Sexual content, nudity | None. | |
| Drugs, alcohol, tobacco | None shown. | **[confirm]** against the archival photographs (a cigarette in a 1948 photograph would be "tobacco reference" on some boards). |
| Language | None (no profanity). | |
| Gambling, purchases | None. Free, no in-game purchases. | |
| User interaction / online | None. Single-player, offline. | |
| Fear / horror | None. | |
| General Mature Content | **Tick** it and give the description from store-page-draft.md section 6. | **OWNER: sensitive.** The checkbox puts the game behind some users' content preferences; given war, terror and the Holocaust as subjects, not ticking it would understate the content. |
| Generative AI | Pre-Generated: yes (section 4). Live-Generated: no. | |

**The German age rating.** Since 15 November 2024 Steam does not show a game in Germany without a valid age rating,
which comes either from the USK directly or from Valve's own process, and "Your game cannot receive a content review
rating from Valve if you have not filled in the game's content survey"
([Age Ratings Mandatory in Germany](https://partner.steamgames.com/doc/gettingstarted/contentsurvey/germany), read 14
October 2026). The same page says some content is not allowed for sale in Germany and must be disclosed. For this game:

- Do **not** enter a USK rating: the game has none, and Valve's page forbids entering one from IARC on another store.
- The relevant German rule is on symbols of unconstitutional organisations (German Criminal Code, section 86a). The game
  shows none: CONTENT_POLICY section 2, rule 4 ("shown without swastikas"). The owner should check every archival
  photograph once more for a Nazi symbol before submitting **[confirm]**.
- Expect the generated rating to reflect war as a subject; the owner sees it after submitting the survey and can check
  the missing-rating health check page that Valve's doc links.

**Answer every section from the build that ships**, including bundled content that cannot be reached in play
(RELEASING.md; Valve's survey page). Re-check if the GDD's settlement-control rules ship: the Deport outcome then
needs its own line (store-page-draft.md section 6).

## 6. Tags, languages and platforms

**Tags** (Steam lets the developer pick up to 20 from its list; pick in Steamworks **[confirm]** the names there):
Strategy, Turn-Based Strategy, Historical, Political Sim, Simulation, Management, Military, War, Singleplayer,
Free to Play, Education, Turn-Based, Political, Realistic, Grand Strategy **[confirm: "Grand Strategy" may oversell a
single-country game; drop it if the owner thinks so]**. Lead with Strategy, Turn-Based Strategy, Historical and Political
Sim (the first tags shown carry the most weight).

**Languages:** English (interface and subtitles; there is no voice-over). Hebrew only if the owner accepts the report's
"Game Support Only" idea; the in-game Hebrew UI is a hand-asked preview and the content text is English (#193, #55).

**Supported OS: Windows and macOS. Linux pending.** This follows M2's real run, the `Player golden` workflow on the
pre-release `v0.1.0-rc.0` (commit `411b1124`, built 2026-10-14T19:54:20Z):
[run 38637103937](https://github.com/eyalgolan/statecraft-israel/actions/runs/38637103937), with its result in a comment
on #533 (issuecomment-6178471285).

| Platform | M2 archive | Player golden check on CI | Steam OS box |
| --- | --- | --- | --- |
| Windows (64-bit) | `statecraft-israel-v0.1.0-rc.0-windows-x64.zip`, Mono | **pass** (windows-2025: both `GOLDEN OK` lines, `PLAYER GOLDEN OK windows`) | **List** |
| macOS (Universal: Apple silicon arm64 and Intel x86_64) | `statecraft-israel-v0.1.0-rc.0-macos-universal.zip`, IL2CPP | **pass** on both slices (arm64 on macos-15, x86_64 on macos-15-intel: both `GOLDEN OK` lines, `PLAYER GOLDEN OK macos`); also passed on the owner's Mac | **List** |
| Linux (x64; "SteamOS + Linux" on the form) | `statecraft-israel-v0.1.0-rc.0-linux-x64.tar.gz`, Mono | **fail**: the player crashed at startup on the headless ubuntu-24.04 runner (`signo:11`, no window backend) | **Pending.** A container repro shows the crash happens only headless (the player runs with a display), and fix PR #581 is open, but the Linux player has not yet passed in CI. List Linux only after a CI run passes. |

Signing: none (O4 for Windows; the macOS build is not yet notarized), which the owner should weigh before listing macOS.

## 7. Owner checklist: the Steamworks steps, in order

None of these is done. Each is the owner's action.

1. **Account.** Sign in to [partner.steamgames.com](https://partner.steamgames.com) as an individual (O5), accept the
   Steam Distribution Agreement, complete the identity, tax (interview) and bank forms. #424's research: tax
   verification takes about 10 to 15 business days.
2. **App.** Pay the Steam Direct fee (US$100 per app, per #424's research) and create the app named "Statecraft: Israel"
   (D8). Set "This is a free product" (O2).
3. **Store page, Basic Info.** Name, developer and publisher (the owner), the website (the project site or the GitHub
   repository; no links in the description), "Coming Soon" with no date, the supported OS from section 6 (Windows and macOS; Linux only after a CI pass), languages from
   section 6, and the system requirements (from the M2 build record).
4. **Store page, Description.** Short description (store-page-draft.md section 1; check the field's character limit),
   "About this game" (section 2; Hebrew from section 3 only after a native speaker's review), the mature-content
   description (section 6), and the legal line: the Copernicus DEM notice from NOTICE, which `tools/README.md` says must
   appear in the Steam page legal notes, with the CHELSA, IMS and Israel State Archives credits, and the CC BY-SA 4.0
   attribution for the store images (section 3).
5. **Tags** from section 6.
6. **Graphical assets.** Upload the capsules and library art from section 1 (header, small, main, vertical, library
   capsule, library header, library hero, library logo); place the logo on the hero with Steam's preview tool. Upload
   the Community icons: for the shortcut icon, `community/monogram/monogram.ico` (the owner's monogram choice: it
   reads at the 32 and 48 px shortcut sizes), or `shortcut_icon_256x256.png` if the title version is wanted at 256 px
   and Steam's generated .ico is accepted (one field: upload one of them); for the app icon, `app_icon_184x184.jpg`.
   The "Mac Icon" field (ICNS) has no file in the kit yet; without one, macOS shortcuts show Steam's default icon
   (section 1).
7. **Screenshots.** Upload the screenshots from section 2 and tick "suitable for all ages" only on the ones marked so
   there (at least four).
8. **Trailer.** A real-build gameplay trailer is still pending (#45). Check in Steamworks whether the Coming Soon
   review needs one; Steam's trailer page says the game cannot be *released* while a trailer is still encoding.
9. **Content Survey.** All three sections, from section 5 and the build that ships. Do not enter a USK rating.
10. **AI disclosure.** The Generative AI section from section 4: Pre-Generated ticked with its text, Live-Generated
    unticked.
11. **Preview.** Look at the whole page in Steam's preview: the capsules at their small sizes, the screenshots, the
    text and the AI disclosure together (RELEASING.md's last checklist line).
12. **Submit the store page for review** (#424's research: about 3 to 5 business days), answer any reviewer
    questions, then make the Coming Soon page visible yourself once it is approved.
