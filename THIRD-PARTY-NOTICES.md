# Third-party notices

Statecraft: Israel. Copyright (c) 2026 Eyal Golan and contributors.

**This file is not a licence.** The licences are in [`LICENSE`](LICENSE), [`LICENSING.md`](LICENSING.md) and
[`LICENSES/`](LICENSES/). This file records attribution and the obligations that come with third-party material, split
by what you are handling. Per-file provenance is in [`ASSETS.yaml`](ASSETS.yaml) and `ASSETS.portraits.yaml`; the
required attribution texts are in [`NOTICE`](NOTICE).

## The source tree (what a clone carries)

| Material | Where | Licence | Obligation |
|---|---|---|---|
| Copernicus DEM GLO-30 (derived heightmap, hex elevations) | `unity/Assets/StreamingAssets/map/`, `content/map/hexes.json` | Copernicus DEM licence | Keep the attribution text in `NOTICE` with the data and in any credits |
| CHELSA V2.1 bio12 | `content/map/precip_bio12.*` | CC0 1.0 | None; the citation in `NOTICE` is requested |
| IMS rain-gauge normals (data.gov.il dataset 481) | `content/map/precip_stations.json` | "Other (Open)" (data.gov.il) | Attribution in `NOTICE`; terms under owner review (decision D6) |
| Wikipedia text and coordinates | cited throughout `content/` | CC BY-SA 4.0 | Attribution (source URL per entry); share-alike, which is why `content/` is CC BY-SA 4.0 |
| Portraits (Wikimedia Commons) | `promo/ref/people/`, `unity/Assets/UI/Portraits/` | Public domain, CC0, CC BY 4.0 or CC BY-SA 4.0, per file | Public domain and CC0: none legally. CC BY 4.0 and CC BY-SA 4.0: keep the credit (artist, licence, source) in `NOTICE`; a portrait cropped from a CC BY-SA 4.0 photograph is itself CC BY-SA 4.0. The per-file record in `ASSETS.portraits.yaml` is kept as provenance |
| Event-card photographs (Wikimedia Commons: Meitar Collection / National Library of Israel, Nationaal Archief, US National Archives) | `unity/Assets/UI/EventImages/` | CC BY 4.0, CC0 1.0 or public domain, per file | CC BY 4.0: keep the credit line (photographer, Meitar Collection, National Library of Israel, The Pritzker Family National Photography Collection; licence; "graded and cropped") in `NOTICE` and under the picture in the game; per-file records in `ASSETS.yaml` |
| EB Garamond | `unity/Assets/UI/Fonts/` | SIL Open Font License 1.1 | Keep `OFL.txt` with the fonts; do not sell the fonts on their own |
| Frank Ruhl Libre | `unity/Assets/UI/Fonts/FrankRuhlLibre/` | SIL Open Font License 1.1 | Keep `OFL.txt` with the fonts; do not sell the fonts on their own |
| Unity template-derived files | `unity/Assets/Settings/`, `spikes/m0/unity/Assets/Settings/`, `spikes/m0/unity/Assets/UI Toolkit/UnityThemes/`, and Shader Graphs generated from a Unity template (`unity/Assets/Shaders/StatecraftTerrain.shadergraph`) | Unity Companion License | Use with Unity; see `LICENSING.md` |
| AI-generated concept art | `promo/final/01`–`08`, `promo/ref/*-src.jpg`, `docs/images/hero-concept-1936.jpg`, `promo/start-screen/*.png`, `unity/Assets/UI/StartScreen/declaration-hall.jpg` (in every build) | CC0 / no rights claimed (`LICENSING.md`); generator terms under owner review (D5; D-D for the start screen's painting and options) | Disclose as AI-generated (Steam and itch.io require it) |

Code dependencies are fetched at build or run time under their own licences and are not vendored: NuGet packages in
`sim/**/*.csproj` (YamlDotNet MIT; test-only: xUnit Apache-2.0, Microsoft.NET.Test.Sdk MIT, xunit.runner.visualstudio
Apache-2.0), Unity packages in `unity/Packages/manifest.json` (Unity Companion License or the Unity Terms of Service),
and Python packages declared inline in `tools/*.py` and `scripts/oss/*.py`.

You need your own Unity licence (Unity Personal is free within Unity's eligibility terms) to open or build `unity/`.

The AI-agent bridge used in the editor (see `CONTRIBUTING.md`) is also fetched, not vendored, and is editor-only:

| Component | Fetched from | Licence | Where it goes |
|---|---|---|---|
| Unity-MCP, `com.ivanmurzak.unity.mcp` 0.93.3 (Ivan Murzak, https://github.com/IvanMurzak/Unity-MCP) | OpenUPM (`https://package.openupm.com`), pinned in `unity/Packages/manifest.json` and `packages-lock.json` | Apache-2.0 | `unity/Library/PackageCache/` |
| `extensions.unity.playerprefsex` 2.1.5 (Ivan Murzak), a dependency of the above | OpenUPM | MIT | `unity/Library/PackageCache/` |
| NuGet DLLs the plugin's resolver downloads on first open: com.IvanMurzak.McpPlugin and McpPlugin.Common 8.7.0 and com.IvanMurzak.ReflectorNet 5.4.1 (Apache-2.0), R3 1.3.0 (MIT), and Microsoft.AspNetCore.SignalR.\*, Microsoft.Extensions.\*, Microsoft.CodeAnalysis.\*, System.\* and Microsoft.Bcl.\* packages (.NET Foundation and Microsoft, MIT) | nuget.org, pinned versions over HTTPS, with no hash or signature check (nuget.org versions are immutable) | Apache-2.0 or MIT, per package | `unity/Assets/Plugins/NuGet/` (git-ignored) |
| GameDev-MCP-Server 9.2.10, the local MCP server binary (https://github.com/IvanMurzak/GameDev-MCP-Server) | that repository's GitHub releases; the plugin checks its SHA-256 against the release's own `SHA256SUMS` (integrity, not authenticity: a compromised release would pass) | Apache-2.0 | `unity/Library/mcp-server/` |

None of these is redistributed by this repository or shipped in a build: `UnityMcpGuard` (`unity/Assets/Editor/UnityMcpGuard.cs`) drops the plugin's assemblies and its NuGet DLLs from every player build. The licence texts come with the OpenUPM packages, with each package on nuget.org (the resolver keeps only the DLLs) and with the server's GitHub releases.

## Builds (what a WebGL or desktop build carries)

A build is a different artefact. It contains the Unity runtime, `YamlDotNet.dll`, the fonts, the portraits, the event
photographs (CC BY 4.0 credits in `NOTICE` and on the card) and the map data. Before a build is handed to anyone, it must carry `LICENSE`, `NOTICE`, this file, `LICENSES/` and
each font's licence: `unity/Assets/UI/Fonts/OFL.txt` as `OFL-EBGaramond.txt` and
`unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt` as `OFL-FrankRuhlLibre.txt`. `scripts/oss/package_web.sh` (WebGL) and
`tools/release_build.py` (desktop) copy them in, and each refuses to finish unless the archive lists every one of them
(see [`docs/RELEASING.md`](docs/RELEASING.md)).

## Third-party trademarks

Names below belong to their owners and are used only to say truthfully what this project uses, runs on or is compared
with. None of them endorses this project, and this repository ships none of their logos.

- Unity is a trademark of Unity Technologies.
- Civilization and Civilization VII are trademarks of Take-Two Interactive Software; they are named as a genre and style
  reference.
- ElevenLabs is a trademark of Eleven Labs, Inc.
- Steam is a trademark of Valve Corporation.
- Wikipedia and Wikimedia Commons are trademarks of the Wikimedia Foundation.
- Copernicus is a programme of the European Union.
- GitHub is a trademark of GitHub, Inc.

The State of Israel's emblem and flag are not used as project marks. Commercial use of the state emblem needs a permit
under Israel's 1949 Flag and Emblem Law (design spec §5.3); any build that shows it must record that permit here.

If you own a mark named here and something in this repository misuses it, open an issue and it will be fixed.
