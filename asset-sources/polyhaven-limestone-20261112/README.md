# Poly Haven limestone batch, 12 November 2026

Refs #1954. This bounded batch contains only Rock 07 and Rock 09. The game uses these as stylized limestone stand-ins; that label is not a geological claim about the scanned originals. No vegetation, ground textures, Tripo assets or other local art is admitted by this record.

## Rights and sources

- https://polyhaven.com/a/rock_07
- https://polyhaven.com/a/rock_09
- https://polyhaven.com/license
- https://creativecommons.org/publicdomain/zero/1.0/

The official licence page explicitly permits redistribution and commercial use of downloadable assets under CC0, without required attribution. Credit is recorded as Poly Haven contributors. Website previews, logos and other website content are not covered by this asset grant and are not included.

The local source ledger records download on 16 October 2026. The retained glTF, BIN and three texture maps for each rock are exact copies of that local source package. Their file names and glTF image references agree with the official asset identities. This is not an upstream byte-identity attestation: the API could not be read through the available web connector, and local Python HTTPS failed certificate-chain verification. Certificate verification was not disabled. `sources.json` records local bytes and SHA-256 values, not independently fetched upstream hashes.

## Derivatives and reproducibility

`decimate.py` is the retained local transformation script, governed by the project's code terms, not the CC0 art grant. It joins glTF meshes, applies rotation and scale, decimates toward 700 triangles with up to six iterations, and exports an OBJ retaining UVs and normals. It copies diffuse and OpenGL normal JPGs without re-encoding. Original source packages remain intact beside the script. Run with Blender: `blender -b -P decimate.py -- SOURCE_DIRECTORY OUTPUT_DIRECTORY 700`. Blender version and actual output counts belong in the PR test evidence; this record does not claim cross-version byte-identical mesh output.

Runtime outputs are `unity/Assets/Art/HexTiles/Props/shared_rock_07/` and `shared_rock_09/`, each containing the named OBJ, `albedo.jpg` and `normal.jpg`. Exact-file ASSETS.yaml rows cover all ten retained source-art files and six runtime art files. The script is excluded from the CC0 art inventory and listed separately as a tool in sources.json.

## Review boundary

Rows remain `owner-verify` pending batch review. Verified redistribution permission does not assert final artistic approval, release clearance, player testing or performance compliance. This batch must not be represented as completing all of #1954. Issue coordination: https://github.com/eyalgolan/statecraft-israel/issues/1954#issuecomment-6821520716 .

Promoted Unity assets use new GUIDs, with prefab/material references rewritten, so they can coexist with ignored prototypes in a contributor checkout. Meshes and JPG bytes are not changed by that metadata migration. The two delivered meshes contain 700 and 699 triangles respectively.

Runtime texture import is capped at512 pixels, with mipmaps and CPU readability off. Source JPGs stay byte-preserved. See verification.md for measured Editor memory, recipe budgets, approval boundary and remaining player-build blockers.
