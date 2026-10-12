# AI-content disclosure (manifest review text)

Generated from ASSETS.yaml and ASSETS.portraits.yaml. Only rows explicitly marked ai_generated: true are listed.

Nothing is generated live while you play.

This is not a licence clearance or a completed store content survey. owner-verify rows still need their recorded review. It does not decide whether AI-assisted script drawings should be flagged, expand globs into files, or infer flags from source wording.

## Images

| Asset pattern | Manifest | Recorded source | Review status |
| --- | --- | --- | --- |
| docs/images/hero-concept-1936.jpg | ASSETS.yaml | Web copy of promo/final/03-gameplay-1936.png (published 2026-10-10); same provenance as promo/final | owner-verify |
| promo/final/0[1-8]-*.png | ASSETS.yaml | ElevenLabs Image API /v1/flows/image (GPT Image 2.5, Nano Banana Pro), prompts in promo/shots.json, generated 2026-10-08/09 by promo/gen.py; text fixes by promo/patch.py | owner-verify |
| promo/ref/*-src.jpg | ASSETS.yaml | Earlier promo/gen.py outputs used as edit inputs for the *_fix shots in promo/shots.json (commit d42bd6b) | owner-verify |
| promo/start-screen/declaration-hall-option-*.png | ASSETS.yaml | Dreamina (dreamina.capcut.com), model GPT Image 2.5, 2026-10-12, 1536x864 as downloaded, with promo/final/03-gameplay-1936.png as the style reference; prompts in promo/start-screen/prompt-b.txt and -c.txt | owner-verify |
| unity/Assets/Art/CommandModels/*.jpg | ASSETS.yaml | Textures accompanying paid Tripo P2.0 generations, 10–11 October 2026; sanitized per-model provenance and file hashes in docs/command-models.json; https://www.tripo3d.ai/terms section 5.2.2 (checked 2026-10-12) | verified |
| unity/Assets/UI/StartScreen/declaration-hall.jpg | ASSETS.yaml | Dreamina (dreamina.capcut.com), model GPT Image 2.5, 2026-10-12, with promo/final/03-gameplay-1936.png as the style reference; the start screen's painting. Option C of promo/start-screen (prompt in promo/start-screen/prompt-c.txt), resized from 1536x864 to 1920x1080 (Lanczos) and saved as JPEG; the Dreamina AI badge is kept | owner-verify |
| unity/Assets/WebGLTemplates/Statecraft/favicon.ico | ASSETS.yaml | Original geometric drawing for the WebGL page's tab icon (issue #47): a gold hexagon with a cream serif I on navy, in the loading page's colours; 16, 32 and 48 px; made 2026-10-11 for PR #169 | verified |

## 3D models

| Asset pattern | Manifest | Recorded source | Review status |
| --- | --- | --- | --- |
| unity/Assets/Art/CommandModels/*.fbx | ASSETS.yaml | Paid Tripo P2.0 generations, 10–11 October 2026; sanitized per-model provenance and file hashes in docs/command-models.json; https://www.tripo3d.ai/terms section 5.2.2 (checked 2026-10-12) | verified |
