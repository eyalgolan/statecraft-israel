# Licensing

Different parts of this repository are under different licences. Where a file is not listed below, the rule for its folder applies.

| What | Paths | Licence |
|---|---|---|
| Command model contribution (explicit path exception) | `unity/Assets/Art/CommandModels/`, its folder metadata `unity/Assets/Art/CommandModels.meta`, and every file beneath it, including models, textures, materials, prefabs, animations and `.meta` files | [Statecraft Models License 1.0](LICENSES/Statecraft-Models.txt), `LicenseRef-Statecraft-Models`. Download, use, modification and redistribution, including commercial games, are allowed; standalone asset sales and paid asset-pack/marketplace redistribution require the original contributing model creator's written permission. This exception overrides the general MIT/CC BY folder rules for this path only. |
| Code | `sim/`, `tools/` (Python), `scripts/`, `.github/`, `.gitignore`, `unity/Assets/Scripts/`, `unity/Assets/Editor/`, `unity/Assets/Tests/`, `promo/*.py`, `spikes/` code, and everything else under `unity/Assets/` not listed below (scenes such as `Map.unity`, `WebGLTemplates/`, `link.xml`, `*.inputactions`, `.meta` files) | [MIT](LICENSE) |
| Game content (text and data) | `content/` (events, people, offices, model, rules, map YAML/JSON), except the third-party map data below | [CC BY-SA 4.0](LICENSES/CC-BY-SA-4.0.txt), credit "Statecraft: Israel contributors". The text paraphrases its cited sources, many from Wikipedia (CC BY-SA), so share-alike applies; each entry lists its sources |
| Original docs, art and UI assets | `docs/`, the root `*.md` files (except `LICENSE`), `ASSETS.yaml`, `ASSETS.portraits.yaml`, `promo/shots.json`, `promo/CREDITS.md`, `tools/README.md`, `reports/` (our own text), `research_notes/` (our own text), `unity/Assets/UI/*.uxml`, `*.uss`, `strings.en.yaml`, `unity/Assets/Materials/`, `unity/Assets/Resources/Units/` (materials), `unity/Assets/Shaders/`, `unity/Assets/UI/*.asset`, `unity/Assets/WebGLTemplates/Statecraft/favicon.ico`, `unity/Assets/Resources/EventImageCatalog.asset` (the event-photo catalog: alt texts, credits and flags; the pictures it points to are under their own licences below), `promo/ref/geo-reference.png` | [CC BY 4.0](LICENSES/CC-BY-4.0.txt), credit "Statecraft: Israel contributors" |
| Unity template-derived files | `unity/Assets/Settings/` (URP assets, renderers, volume profiles), `spikes/m0/unity/Assets/Settings/`, `spikes/m0/unity/Assets/UI Toolkit/UnityThemes/`, and any Shader Graph generated from a Unity template (for example `unity/Assets/Shaders/StatecraftTerrain.shadergraph`, from "Terrain Simple") | Created from Unity's project or graph templates and edited; under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) (use with Unity), not MIT |
| AI-generated images | `promo/final/01`–`08`, `promo/ref/*-src.jpg`, `docs/images/hero-concept-1936.jpg`, `promo/start-screen/*.png`, `unity/Assets/UI/StartScreen/declaration-hall.jpg` | Marked [CC0 1.0](LICENSES/CC0-1.0.txt). No rights are claimed. Purely AI-generated output may not be copyrightable. Made with the ElevenLabs Image API (see `promo/CREDITS.md`), except the start screen's painting and its options, made with Dreamina (`ASSETS.yaml`). |
| Quoted public-domain text | `docs/sources/declaration-1948/` | The Declaration of the Establishment of the State of Israel (1948): the Hebrew of the Official Gazette and the English translation. Public domain; sources in each file and in `ASSETS.yaml`. |
| In-engine captures | `promo/final/09`, `10`, `docs/images/build-*.jpg`, `docs/images/social-preview.jpg`, `spikes/m0/evidence/*.jpg`, `spikes/m0/evidence/*.txt` (logs) | CC BY 4.0. They show the Copernicus-derived terrain; see below. |
| Portraits | `unity/Assets/UI/Portraits/`, `promo/ref/people/`, `promo/ref/*-sheet.jpg` | Public-domain photographs from Wikimedia Commons, plus one CC0 file (Moshe Dayan, 1962). Per-file sources are in `content/people.yaml` and `promo/CREDITS.md`. |
| Event-card photographs | `unity/Assets/UI/EventImages/*.jpg` | Archival photographs from Wikimedia Commons, each under its own licence: CC BY 4.0 (Meitar Collection, National Library of Israel; credits in [NOTICE](NOTICE)), CC0 1.0 (Nationaal Archief) or public domain (US National Archives). All are adapted (graded and cropped). Per-file records in `ASSETS.yaml`; the mapping `event-images.yaml` beside them is CC BY 4.0 like the other UI assets. |
| Fonts | `unity/Assets/UI/Fonts/` | EB Garamond, [SIL Open Font License 1.1](unity/Assets/UI/Fonts/OFL.txt); Frank Ruhl Libre (`FrankRuhlLibre/`), [SIL Open Font License 1.1](unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt) |
| Terrain heightmap | `unity/Assets/StreamingAssets/map/heightmap.*`, `content/map/hexes.json` (elevations) | Derived from Copernicus GLO-30 under the Copernicus DEM licence. Required notice: "produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved" |
| Precipitation grid | `content/map/precip_bio12.*` | CHELSA V2.1, CC0 1.0 (Karger et al. 2017, 2021; see `tools/README.md`) |
| Rain-gauge data | `content/map/precip_stations.json` | Israel Meteorological Service, data.gov.il dataset 481, under the licence stated on the dataset ("Other (Open)") |
| Unity engine and packages | `unity/Packages/`, `unity/ProjectSettings/` | Configuration only. The Unity engine and its packages are under Unity's own terms and are not included. Two editor-only packages come from OpenUPM under their own licences and are not included either: `com.ivanmurzak.unity.mcp` (Unity-MCP, Apache-2.0) and its dependency `extensions.unity.playerprefsex` (MIT); see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). No Asset Store content is in this repository. |
| In every build | `YamlDotNet.dll` | MIT, Copyright (c) Antoine Aubry and contributors ([LICENSES/YamlDotNet-MIT.txt](LICENSES/YamlDotNet-MIT.txt)) |

### Command model license exception offered for maintainer acceptance

The original contributing model creator is identified by the pull request introducing the 34 models; no personal
identity is inferred from an account name. That contributor authorized public release under the model-specific
terms on 12 October 2026. [The sanitized manifest](docs/command-models.json) records the paid Tripo P2.0 generations
from 10–11 October 2026. [NOTICE](NOTICE) supplies the credit. Materials, prefabs and Unity metadata in the model
folder use the same exception so downstream users have one set of terms for each complete asset.

This is a restricted asset license, not an OSI open-source or unrestricted CC BY/MIT license. It is an explicit
exception offered with the draft pull request: **maintainer acceptance is required before merge**. Publishing the
draft is not a claim that the maintainer has accepted the exception. The contribution is offered under this custom
license, not under the general inbound license for other art. All other repository licensing and release checks
remain unchanged.

The [Tripo terms, section 5.2.2](https://www.tripo3d.ai/terms) describe paid-user output rights subject to their
conditions. They do not establish copyrightability or clear every third-party right. The model license therefore
grants only rights the contributor can grant and preserves applicable law and existing platform permissions.
[GitHub's terms, sections D.4–D.6](https://docs.github.com/en/site-policy/github-terms/github-terms-of-service#d-user-generated-content)
cover platform permissions, public viewing/forking and repository contributions. The custom terms do not revoke
those permissions. Both sources were checked on 12 October 2026. The manifest's verified provenance/authorization
status is not a guarantee of copyright, exclusivity or legal enforceability.

Historical facts are not owned by anyone. The text describing them paraphrases the cited sources. The research notes and reports quote short passages from their sources for commentary; those quotes remain © their authors.

Still being checked by the owner: the ElevenLabs and Dreamina terms for the AI images, the data.gov.il terms for the rain-gauge data, and the US public-domain status of each portrait (decisions D5, D-D, D6 and D-P in [docs/oss/DECISIONS.md](docs/oss/DECISIONS.md); per-file status in [ASSETS.yaml](ASSETS.yaml)). These files were published before the checks. If a check fails, the file is removed from the current tree; git history keeps it. Attribution texts: [NOTICE](NOTICE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

"Civilization" is a trademark of Take-Two Interactive. It is mentioned only to describe the genre. This project is not affiliated with or endorsed by its owners.
