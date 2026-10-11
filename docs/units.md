# Unit miniatures: the art slice

The owner's target for units (2026-10-11) is the promo: `promo/final/03-gameplay-1936.png` and the demo trailer
(`promo/demo/out/demo-16x9.mp4`, 0:06–0:30). It shows Civ-style miniatures standing on hexes at symbolic scale: a trio of
era-correct riflemen with a selection ring, a mounted rider, and aircraft with Israeli roundels flying low with a shadow.
This page records the slice that matches it, the pipeline choice behind it, the scale, and the sources for every uniform,
aircraft and ship.

The slice is a **showcase only**. It reads nothing from the sim and changes no game state. Where units stand, move and
fight is war mode's job (#104, with the sim side in #21). The golden hashes do not move.

## How to see it

- **WebGL:** add `units` to the page URL (`index.html?units`). Normal play never shows it, and neither does a plain
  `?shot`: every track's art-checkpoint metrics stay comparable (controller ruling, 2026-10-11).
- **Art checkpoint:** `tools/art_checkpoint.sh --units <name>` adds `&units` to the WebGL screenshots and `-units` to the
  editor renders. With `shot` the aircraft holds a fixed point, so the screenshots repeat.
- **Desktop player:** run with `-units`.
- **Editor:** `MapCapture.Promo` renders it when given `-units`.
- **Frame cost:** `?vprofile&units` prices it as the `units` toggle (`median(all) - median(-units)`).

Code: `unity/Assets/Scripts/Map/Units/`. The materials are made by `UnitMaterialAssets.Create` in
`unity/Assets/Resources/Units/`. Tests: `UnitShowcaseTests`.

## Owner rulings (2026-10-11)

1. **Units are Civ-style tokens at about 2,000x real size** (see "Scale").
2. **Promo-quality close-zoom models come from a separate track:** a cloud-GPU AI-3D pipeline (TRELLIS, MIT). The
   code-built miniatures here stay as the far-zoom and fallback version.

**The art hook.** An authored model replaces the code-built one for its unit type when a prefab exists at
`unity/Assets/Resources/Units/Authored/<unit type>`. The unit types are the `*Type` constants in `UnitShowcase`, for
example `rifleman-1948-steel-helmet` and `spitfire-mk9e-101sqn-1948`.

An authored model follows the code-built convention:
- real metres, facing +Z;
- origin on the ground (an aircraft: its centre of gravity);
- its own materials;
- an authored aircraft brings its own propeller.

The showcase places and scales it as it does the code-built model. That is the whole hook: there is no registry beyond
it, and there are no level-of-detail switches yet.

## What is in the slice

| Piece | Model | Where (PromoPose.Gameplay03) |
|---|---|---|
| Rifle trio | three 1948 IDF riflemen: bush hat with rifle held ready, Mk II steel helmet with rifle grounded, Palmach stocking cap with rifle slung | on the centre of the hex nearest 34.88 E, 32.01 N (east of Tel Aviv, clear of the settlement labels), inside a white selection ring that fits within the hex |
| Aircraft | Supermarine Spitfire LF Mk IXe of 101 Squadron, late 1948 – January 1949: red spinner and red-and-white rudder (attested on Spitfires for January 1949), roundel without an outer ring | a 16 km loop over Tel Aviv and the coast, 4.2 km above the highest ground under it, banked 28 degrees, its shadow cast on the ground by the sun |
| Navy | modified Flower-class corvette INS Haganah (K-20), 1948, 63.4 m | off Jaffa (34.63 E, 32.02 N) |

Not in the slice:
- **The mounted rider.** No 1948 IDF mounted unit was sourced for this slice. The promo's rider belongs to 1936, so it
  waits for the Yishuv era (#49) and a source.
- **Civilian boats.** The owner ruled them out of scope; navy ships are units.

## Pipeline choice: code-built miniatures

Three pipelines were tried on a development Mac (Apple M5 Pro, 48 GB) on 2026-10-11. Each was placed on the map at the
same scale and rendered at the gameplay zoom and close up, next to the promo's trio; the table summarises what each
produced. The renders are not in the repository.

| Pipeline | Result | Licence |
|---|---|---|
| (a) Image-to-3D, run locally: TripoSR (MIT code and weights), CPU, about 3.5 s per mesh | Inputs were a promo crop and a 1948 photograph. Out came a blurry blob of about 45,700 triangles with baked vertex colours: the head and rifle were lost to the background cut-out, the side view was a flat card for the rider, and there was no rig. It would need clean, sharp turnaround references, which we cannot make locally, plus manual retopology. | MIT; fine. |
| (a) Hunyuan3D-2, TRELLIS | Not run locally. Hunyuan3D-2's community licence excludes the EU, the UK and South Korea, which is not compatible with an open project. TRELLIS needs an NVIDIA GPU with CUDA; the owner chose it for the cloud-GPU close-zoom track (rulings above). | Hunyuan3D-2: not compatible. TRELLIS: MIT. |
| (b) CC0 base meshes: Quaternius Universal Base Characters (free "Standard" kit, CC0) | A clean, rigged body, but a stylised "superhero" figure in underwear, in a T-pose. Every uniform, hat, belt and rifle would have to be modelled by hand in Blender. Kenney's characters are blocky, low-poly figures, which the spec rules out (§6, "no low-poly packs mixed in"). | CC0; fine. |
| **(c) Code-built from primitives (chosen)** | Lathed limbs and torsos, ellipsoids, boxes and plates, painted from one palette texture with a dark wash at the feet and a dry-brushed highlight on top. One URP Lit material, made at runtime, with no texture file. About 3,600 triangles per figure. Real silhouettes for each headgear and rifle pose. The figures cast and receive shadows. | Our own code, MIT. |

**Why (c):** at the gameplay zoom a figure is about 90 px tall. What reads at that size is the silhouette (hat brim,
rifle, stance), the colour blocks (khaki shirt, dark trousers, pale webbing) and the shadow. In the gameplay03
checkpoint the code-built trio matches the promo's trio in size (87 px) and reads with all three. They are exact to the
sources: each part is a sourced item, and a change is a reviewed line of code. Era variants (1956 helmets, 1967 Uzis, 1973 flak vests) are parameters. Builds are deterministic, there is no
licence question, and the build-size cost is code only.

**Their limit:** close up (the 6 km hero zoom) they look like wooden mannequins, with faceted limbs and no faces or
folds. The promo's close shots (0:21) show sculpted miniatures. The way past that limit is the owner's close-zoom track,
which plugs authored models in through the art hook above.

## Scale

Measured on the promo scaled to 1920 px wide: a rifleman is about 87 px tall, inside a ring about 190 px across. Our
gameplay03 pose frames the same hex count as the promo (about 9 across: see `PromoPose`). The first checkpoint, at 2600x,
drew our riflemen 113 px tall. So the slice uses `UnitShowcase.FigureScale = 2000`: a man about 3.6 km tall on 5 km
hexes, with the ring at 0.66 of a hex's circumradius. The aircraft (x420, a 4.7 km span) and the ship (x95, 6.0 km) are
sized against the figures, as Civ sizes them, not against each other.

**Owner ruling (2026-10-11): units stay Civ-style tokens at about 2,000x.** This is far above the 10–25x ruling for
buildings and trees. At 10–25x a man is 18–45 m tall, which is under one pixel at 60 km. The promo, which wins over strict
scale by the owner's ruling, draws units as tokens. The scale is one constant.

## Sources (checked 2026-10-11)

Uniform and equipment, 1948:
- Palmach (Negev Brigade) soldiers briefed before an operation, 1948: khaki shirts and trousers, web belts with pouches,
  knitted stocking caps, rifles. Public domain.
  https://commons.wikimedia.org/wiki/File:Palmach-negev-instructed2operation.jpg
- Negev Brigade soldiers, 20 October 1948: Mk II steel helmets, khaki drill, field gun. Public domain.
  https://commons.wikimedia.org/wiki/File:Negev_Brigade_soldiers_1948.jpg
- Beersheba, 21 October 1948: brimmed bush hats, khaki shirts and shorts, belts. CC BY 4.0 (National Library of Israel).
  https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_997008137085905171.jpg
- A woman fighter in Jerusalem, about March 1948 (photograph by Yehuda Aizenshtark): khaki shirt and trousers, web
  belt, a grenade, and a Lee-Enfield No. 4 rifle (per the Commons category). CC BY 2.5 (PikiWiki).
  https://commons.wikimedia.org/wiki/File:PikiWiki_Israel_38816_Israel_Defense_Forces.jpg

Aircraft:
- Israel bought 59 Spitfire Mk IXs from Czechoslovakia in 1948–49; IAF Spitfires fought in the 1948 war (the last air
  combat was on 7 January 1949). https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history
- Ferry flights of the Czech Spitfires to Israel (Operation Velvetta, 1948). https://en.wikipedia.org/wiki/Operation_Velvetta
- The Avia S-199 flew first, on 29 May 1948 (Operation Pleshet). https://en.wikipedia.org/wiki/Avia_S-199
  The Spitfire was chosen over it because the promo shows a Spitfire (promo beats era, and both are 1948-correct).
- Spitfire Mk IX shape (an elliptical wing, a four-blade propeller): the IAF Museum airframe at Hatzerim. CC BY 2.5.
  https://commons.wikimedia.org/wiki/File:Spitfire-MkIX-hatzerim-1-1.jpg
  Many IAF IXes had clipped wing tips (Fundekals review, below). The slice keeps the full elliptical wing that the promo
  shows.
- Variant **LF.IXe**, and 101 Squadron's **red spinner** and **red-and-white striped rudder**. On the Spitfires these are
  attested for **January 1949** (IsraDecal); the slice is dated "late 1948 – January 1949".
  - IsraDecal Studio, "Spitfire Mk.IX, Israeli Air Force" (IAF-123, 1:24) marking instructions, May 2023. It lists
    "Spitfire LF.IXe 2011/26, 101 Squadron, January 1949", painted in RAF colours, with a red spinner and the rudder
    painted "in White. Paint Red stripes".
    https://www.isradecal.com/_files/ugd/ddb4b4_42f20e2bc83f4ba895c816e97ad03151.pdf
  - Eduard Info 11/2025: "In preparation for Operation Ten Plagues (October 15-22, 1948), later renamed Operation Yoav,
    101 Tayeset fighters obtained high visibility markings consisting of red spinners and red and white stripes on the
    rudders". https://info.eduard.com/en/11-2025-1/strana-53
    The page shows the squadron's Avia S-199s ("Messers"), not its Spitfires. So it dates the squadron's markings to
    October 1948, but **on the Spitfires the October 1948 date is unverified**.
  - A Fundekals decal review also says 101 Squadron "had the white and red stripes on the rudder".
    https://mail.modelingmadness.com/scott/decals/fundekals/fd48idfspit.htm
  - **Unverified:** the stripes' direction. The slice draws them across the rudder.
- Roundel of 1948–49: a blue Star of David on a white disc, the star clear of the disc's edge, **with no outer blue ring**.
  - Flags of the World, citing a 2002 survey of *Born in Battle*: "almost all the roundels show the star within, but not
    reaching the edges of, the white disk ... This includes early planes from the 1948/49 war of independence (like
    Spitfire ...)". It adds: "the only aircraft showing a roundel with a thin blue ring around are Mosquitos and Meteors
    from the 1950ies". https://crwflags.com/fotw/flags/il%5Eairfo.html
  - The Commons "early variant" SVG with the ring is drawn from a Mosquito photograph, a 1950s type, so it is not used.
- **Unverified (one hobby source, IsraDecal):** the camouflage, RAF ocean grey and dark green over medium sea grey. A
  forum report of brown overpaint in theatre is an open question. The museum airframe is bare metal in post-war markings.
  The standard reference, Alex Yofe's *Spitfire Mk. IX in Israeli Air Force Service 1948–1956*, has not been checked.

Navy:
- INS Haganah (K-20), ex-HMCS Norsyd, a modified Flower-class corvette, 208 ft (63.4 m) overall, with an upright
  funnel. https://en.wikipedia.org/wiki/HMCS_Norsyd
  - In 1946 it was sold to the Mossad for Aliyah Bet as the *Balboa*, renamed *Haganah*, and carried Holocaust survivors
    towards Mandate Palestine.
  - The Israeli Navy took it over and rearmed it in 1948.
- Photograph of K-20 in Haifa harbour, 1948: light grey, mast at the bridge, funnel aft of it. Public domain.
  https://commons.wikimedia.org/wiki/File:Hagana_K-20_ship_Israel.jpg
- Photograph of INS Haganah, about 1949. Public domain. https://commons.wikimedia.org/wiki/File:INSHaganah.jpg

The photographs are references only: no image file is copied into the repository.
