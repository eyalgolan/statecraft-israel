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
| site/public/favicon.svg | ASSETS.yaml | Original geometric drawing for the website's tab icon (a gold hexagon with a cream serif I on navy, after the game's own tab icon), written as SVG by an AI coding agent on 2026-10-13 | verified |
| site/src/assets/media/cinematic-trailer-1-poster.jpg | ASSETS.yaml | The frame at 3 s of the cinematic trailer published in README.md (the GitHub-hosted 1:23 video, the same cut as site/public/media/promo-trailer-720p.mp4; local file sha256 36322b1baca2df5248cb30dc3a36853566f7d8ef02d0d7cd07251fa0d6bffe4e), scaled to 1280 px with ffmpeg; the trailer was made with AI video tools from promo/final concept art | owner-verify |
| unity/Assets/UI/StartScreen/declaration-hall.jpg | ASSETS.yaml | ElevenLabs Image API (Pro plan, decision D5), model GPT Image 2.5 Sunburst (gpt-image-2.5-sunburst), 2K, quality high, 2026-10-12 (#268); the start screen's painting, native 2048x1152, saved as JPEG (quality 90), no upscale. Take 1: prompt promo/start-screen/prompt-d.txt, with the previous painting (this file as merged in #255: Dreamina option C, resized to 1920x1080) as the composition reference and promo/final/03-gameplay-1936.png as the style reference. Take 2 (used): prompt promo/start-screen/prompt-d-edit.txt, an edit of take 1 that changes only the row of figures at the table. The previous painting (Dreamina, #255) showed sunbeams through the windows | owner-verify |
| unity/Assets/WebGLTemplates/Statecraft/favicon.ico | ASSETS.yaml | Original geometric drawing for the WebGL page's tab icon (issue #47): a gold hexagon with a cream serif I on navy, in the loading page's colours; 16, 32 and 48 px; made 2026-10-11 for PR #169 | verified |

## Video

| Asset pattern | Manifest | Recorded source | Review status |
| --- | --- | --- | --- |
| site/public/media/promo-trailer-720p.mp4 | ASSETS.yaml | Web encodes of the cinematic trailer (demo 1), promo/demo/out/demo-16x9.mp4 (not in the repository; sha256 ef791cfbdca0868ae43d09157793d54558605cc9477547cf6231a088d9986951, 1920x1080, 83 s), scaled to 1280x720 with ffmpeg on 2026-10-13: H.264 High@3.1, two-pass 660 kb/s, with AAC 96 kb/s; no other change. The same cut as the video in README.md (that upload is a separate export of it, sha256 36322b1b...). The trailer was made with AI video tools from promo/final concept art; its score is planned in promo/demo/MUSIC.md as an ElevenLabs Music cue (owner to confirm the score's source) | owner-verify |

## Other

| Asset pattern | Manifest | Recorded source | Review status |
| --- | --- | --- | --- |
| unity/Assets/Art/Settlements/Prototype/Models/* | ASSETS.yaml | Tripo Studio (tripo3d.ai), paid Max plan, account pixelrenderai, made 10-13 October 2026 from concept images generated in the same studio. Building modules decimated to 6,000-8,000 triangles in Blender 4.4; characters converted from GLB to FBX in Blender; colour maps 1024 px. The masters and per-model records (prompts, task ids, credits) are kept outside the repository. | owner-verify |
