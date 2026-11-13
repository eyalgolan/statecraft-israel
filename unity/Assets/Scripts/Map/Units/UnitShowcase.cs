#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The unit art slice (owner, 2026-10-11: "make sure the units are like in the promo video"): a 1948 rifle
    /// trio with a selection ring, a Spitfire flying a loop with its shadow on the ground, and a corvette at sea, all at
    /// <see cref="PromoPose.Gameplay03"/>. A showcase only: it reads nothing from the sim. Normal play draws the units the
    /// sim's state and the sourced unit data give (<see cref="UnitLayer"/>), with the same models; this slice appears only
    /// when asked for ("units" in the WebGL page URL or -units on desktop), in the art checkpoint's screenshots and in the
    /// editor's promo renders. Registers the "unitshowcase" visual toggle, so the frame profile can price it
    /// (?vprofile&amp;units); the "units" toggle is the in-play layer's.</summary>
    public static class UnitShowcase
    {
        public const string RootName = "UnitShowcase", ToggleName = "unitshowcase";

        /// <summary>Symbolic scale: a man is drawn <see cref="FigureScale"/> times his real size (about 2.5 km). The first
        /// round matched promo 03 at 2000x (docs/units.md); demo2 T29 (owner's reference, 14 Oct) stands its figures about
        /// <see cref="HouseRatio"/> times a house, where 2000x stood them about 2.5 times the White City blocks beside the
        /// showcase trio at close30 (T54 audit), so 1400x (refs #197). The aircraft and the ship are sized against him, as
        /// Civ draws them, not against each other.</summary>
        public const float FigureScale = 1400f;
        /// <summary>DESIGN VALUES (#2430, the owner judges them on the captures): a token's figures and flag from a far camera, as a
        /// share of <see cref="FigureScale"/>. Up to <see cref="ZoomScaleFromM"/>, the far edge of the close switch's hysteresis
        /// (TownModelLayer.CloseExitM), they keep FigureScale, so T29's ratio to a house holds wherever a close building model
        /// draws, and SpecialBuildingModels.FigureShadowM's reach holds wherever a workshop does. From there they grow to <see cref="ZoomScaleMax"/> at
        /// <see cref="ZoomScaleToM"/> and stay there: a figure at 60 km was about 57 px of 1080, and this holds it near promo 03's
        /// about 71 px. The token's base does not grow: the figures keep their spacing and <see cref="UnitPlinth.BaseToTrio"/> leaves
        /// them room on it. The constant stays: spacing elsewhere (CivilianPeople) derives from it.</summary>
        public const float ZoomScaleFromM = Kits.TownModelLayer.CloseExitM, ZoomScaleToM = 60000f, ZoomScaleMax = 1.3f;
        /// <summary>The share of <see cref="FigureScale"/> a token's figures draw at for a camera at <paramref name="viewM"/>
        /// (TownModelLayer.ViewDistance).</summary>
        public static float ZoomScale(float viewM) =>
            Mathf.Lerp(1f, ZoomScaleMax, Mathf.InverseLerp(ZoomScaleFromM, ZoomScaleToM, viewM));
        /// <summary>The house T29's ratio is measured against at close30, as the T54 audit did: the White City block beside
        /// the showcase trio, the kit's seven-storey heart block (7 x 3.3 m at <see cref="Kits.KitScale.DefaultCityBlocks"/>;
        /// measured on the close30 render at about 1,300 m). A two-storey village house is about half of it.</summary>
        public const float ReferenceHouseM = 7f * 3.3f * Kits.KitScale.DefaultCityBlocks;
        /// <summary>A figure's height over <see cref="ReferenceHouseM"/>: about 1.8, as T29.</summary>
        public const float HouseRatio = UnitBadge.FigureHeightM * FigureScale / ReferenceHouseM;
        public const float AircraftScale = 420f;        // span 11.2 m x 420 = 4.7 km: wider than a man is tall

        /// <summary>DESIGN VALUE (agent-chosen, coordinator-approved plan, 5 November 2026; checked in captures): the aircraft's
        /// paint smoothness (URP Lit), a satin sheen in the map's sun. The Egyptian Spitfire's streamed model is fully matte
        /// (0) and the units' shared paint 0.18; only the code-built 101 Squadron Spitfire and the Egyptian one take it. 0.4 read
        /// as still mostly matte at 31 km in e21's AFTER captures (5 November 2026).</summary>
        public const float AircraftSmoothness = 0.5f;

        /// <summary>The URP Lit smoothness property.</summary>
        internal const string SmoothnessProperty = "_Smoothness";

        /// <summary>DESIGN VALUE: the 101 Squadron Spitfire's propeller blur radius in the aircraft's frame: the blades' reach
        /// (UnitModels.SpitfirePropeller, 1.6) and a little more.</summary>
        public const float PropellerBlurRadius = 1.65f;

        /// <summary>DESIGN VALUE: the 101 Squadron Spitfire's haze over the Egyptian one's opacity, in one layer: at the same
        /// opacity in two layers it read as a near-opaque white plate under its dark rods (e21's AFTER captures of #2728).</summary>
        public const float PropellerBlurOpacity = 0.65f;

        /// <summary>The names of the 101 Squadron Spitfire's propeller blur (a child of its propeller: the code-built one, then the
        /// streamed model's, <see cref="CarryBlur"/>) and ground shadow (a child of the aircraft).</summary>
        public const string PropellerBlurName = "PropellerBlur", ShadowName = "Shadow";

        /// <summary>A copy of <paramref name="source"/> (same shader, maps and colours) with <see cref="AircraftSmoothness"/>.
        /// The caller owns it.</summary>
        internal static Material Glossy(Material source)
        {
            var m = new Material(source) { name = source.name + " (glossy)" };
            m.SetFloat(SmoothnessProperty, AircraftSmoothness);
            return m;
        }

        /// <summary>The blurs' and the shadow's material: alpha-blended, unlit, both faces, its colour the meshes' vertex colours
        /// (HexMaterials' shipped under-attack border template, so a player build keeps the shader's variant), as the Egyptian
        /// Spitfire's (SeaAirCollection). The caller owns it.</summary>
        internal static Material BlurMaterial() => Statecraft.HexTiles.HexMaterials.New(Statecraft.HexTiles.HexMaterials.AttackBorder);

        /// <summary>DESIGN VALUES: the 101 Squadron Spitfire's propeller blur, the Egyptian Spitfire's light haze
        /// (SeaAirCollection.PropBlurColour, PropBlurAlpha) for its four blades (UnitModels.SpitfirePropeller: every 90 degrees
        /// from 20, which this disc, turned to face along +z, sees at 70 degrees and every 90 after), clear at the spinner,
        /// fading out at the rim, one layer through the blades' hub, at <see cref="PropellerBlurOpacity"/>. Uploaded; the caller owns it.</summary>
        internal static Mesh PropellerBlurMesh() => Collection.SeaAirCollection.Disc("SpitfirePropellerBlur101", 48, new[] { 0.15f, 0.4f, 0.7f, 0.9f, 1f },
            new[] { 0f }, Collection.SeaAirCollection.PropBlurColour,
            (r, deg) => r >= 1f ? 0f : PropellerBlurOpacity * Mathf.Min(1f, r / 0.4f) * Collection.SeaAirCollection.PropBlurAlpha(deg, 90f, 70f));

        /// <summary>Puts the light blur <paramref name="mesh"/> on the propeller <paramref name="prop"/> (it turns about +z), so it
        /// turns with it: the disc (made in XZ) turned to face along +z, <paramref name="radius"/> across.</summary>
        internal static Transform PropellerBlur(Transform prop, Mesh mesh, Material blur, float radius = PropellerBlurRadius)
        {
            var go = new GameObject(PropellerBlurName);
            go.transform.SetParent(prop, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * radius;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = blur; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>The flying 101 Squadron Spitfire's soft ground shadow (the Egyptian Spitfire's disc, SeaAirCollection.ShadowDisc),
        /// a child of <paramref name="plane"/> that ShowcaseFlight lays on the ground each frame, so it goes with the aircraft.</summary>
        internal static Transform GroundShadow(Transform plane, Mesh mesh, Material blur)
        {
            var go = new GameObject(ShadowName);
            go.transform.SetParent(plane, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = blur; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>A flying 101 Squadron Spitfire's look (coordinator, 5 November 2026), on its code-built model: its propeller
        /// <paramref name="codeProp"/> carries the light blur; a soft shadow, sized to the airframe as drawn, lies on the ground
        /// (ShowcaseFlight lays it); and the aircraft casts no shadow of its own (<see cref="CastNoShadow"/>). When the
        /// Blender-built model streams in over it (<see cref="StreamedAircraft"/>), the blur moves to that model's propeller
        /// (<see cref="CarryBlur"/>), the shadow stays, and the model casts none either. Returns the shadow and its half size
        /// across and along the heading (m). Call before the aircraft is placed or turned: the size is measured in its own frame.</summary>
        internal static void FlyingSpitfire(Transform plane, Transform codeProp, Mesh blurMesh, Material blur,
            Mesh shadowMesh, out Transform shadow, out Vector2 shadowHalf)
        {
            var size = SizeInFrame(plane);   // before the blur and the shadow are added
            shadowHalf = 0.5f * Collection.SeaAirCollection.ShadowShare * AircraftScale * new Vector2(size.x, size.z);
            PropellerBlur(codeProp, blurMesh, blur, BlurRadius(CodeBuiltPropellerReach));
            shadow = GroundShadow(plane, shadowMesh, blur);
            CastNoShadow(plane);
        }

        /// <summary>How far the code-built Spitfire's blades reach from the hub (UnitModels.SpitfirePropeller).</summary>
        const float CodeBuiltPropellerReach = 1.6f;

        /// <summary>The blur's radius for blades reaching <paramref name="reach"/> from the hub: a little beyond them, as
        /// <see cref="PropellerBlurRadius"/> is for the code-built blades (<see cref="PropellerBlurRadius"/> when there are none).</summary>
        static float BlurRadius(float reach) => reach > 0f ? reach * PropellerBlurRadius / CodeBuiltPropellerReach : PropellerBlurRadius;

        /// <summary>Moves a flying Spitfire's <paramref name="blur"/> (<see cref="PropellerBlur"/>, on the code-built propeller)
        /// onto <paramref name="prop"/>, the streamed model's own propeller (StreamedAircraft, on the swap), so it turns with it
        /// and keeps drawing: through its hub, facing along its +z, sized to its blades (<see cref="PropellerReach"/>, in its
        /// own units). The blur's mesh and material stay the aircraft's own (MapRuntimeAssets frees them).</summary>
        internal static void CarryBlur(Transform blur, Transform prop)
        {
            float reach = PropellerReach(prop);   // before the blur joins it
            blur.SetParent(prop, false);
            blur.localPosition = Vector3.zero;
            blur.localRotation = Quaternion.Euler(90f, 0f, 0f);
            blur.localScale = Vector3.one * BlurRadius(reach);
        }

        /// <summary>The meshes under <paramref name="plane"/> measured in its own frame, in the model's units (metres for a
        /// model to scale: the frame's own scale is taken out).</summary>
        static Vector3 SizeInFrame(Transform plane)
        {
            Bounds? box = null;
            foreach (var p in Corners(plane, plane))
                if (box is Bounds b) { b.Encapsulate(p); box = b; } else box = new Bounds(p, Vector3.zero);
            return box?.size ?? Vector3.zero;
        }

        /// <summary>How far a streamed propeller's blades reach from its hub in its turning plane (its local X and Y), in its
        /// own units: the largest of its meshes' extents along either axis (a bounding box's corner would overstate a
        /// blade by up to the square root of 2). 0 when it has no mesh.</summary>
        static float PropellerReach(Transform prop)
        {
            float reach = 0f;
            foreach (var p in Corners(prop, prop)) reach = Mathf.Max(reach, Mathf.Abs(p.x), Mathf.Abs(p.y));
            return reach;
        }

        /// <summary>The corners of every mesh's bounds under <paramref name="root"/>, in <paramref name="frame"/>'s local space.</summary>
        static IEnumerable<Vector3> Corners(Transform root, Transform frame)
        {
            foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) continue;
                var m = frame.worldToLocalMatrix * f.transform.localToWorldMatrix;
                var b = f.sharedMesh.bounds;
                for (int c = 0; c < 8; c++)
                    yield return m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
            }
        }

        /// <summary>A flying code-built Spitfire casts no shadow of its own: its soft disc (<see cref="GroundShadow"/>) is its
        /// shadow, and a cast one would lie about 7 km off at its drawn height (e21's AFTER captures of #2728).</summary>
        internal static void CastNoShadow(Transform plane)
        {
            foreach (var r in plane.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The sea's surface (world y), as KitGround has it: the heightmap's 0 m.</summary>
        internal static float SeaLevelY(MapScene map) => map.Terrain.transform.position.y - map.ElevMinM * MapBuilder.VerticalExaggeration;
        public const float ShipScale = 95f;             // 63.4 m x 95 = 6.0 km: about a hex, like the promo's boats
        /// <summary>Trio spacing and the ring, as fractions of the hex circumradius. <see cref="TrioSpread"/> is the spacing
        /// the placer checks a token's spot with (UnitGround), kept so no spot moved.</summary>
        public const float TrioSpread = 0.40f, RingRadius = 0.66f, RingWidth = 0.035f, RingClearanceM = 25f;
        /// <summary>The spacing the figures are drawn at: shoulder to shoulder, as T29's trios (about 1.15 figure heights
        /// across; ours 1.15 too: 2 x 950 m between the outer figures' centres plus a figure's width, against a 2.5 km man).
        /// Narrower than <see cref="TrioSpread"/>, so the drawn figures stand inside the ground the placer checked.</summary>
        public const float DrawSpread = 0.19f;
        /// <summary>The share of <see cref="DrawSpread"/> a compact token's figures stand at (#2430): at the compact ring's half
        /// (UnitGround.CompactScale) three figures merged into one shape. DESIGN VALUE: wider and the base's ring runs into the
        /// selection ring, which stands at the town clearing's radius (UnitGround.RingRadius, #588).</summary>
        public const float CompactSpread = 0.6f;
        /// <summary>How far (in hexes) from <see cref="TrioAt"/> the showcase trio may move to stand clear of the kits and
        /// the routes (<see cref="TrioSpot"/>).</summary>
        public const float SpotSearchHexes = 2.2f;
        /// <summary>The flight: a circle of <see cref="LoopRadiusM"/> about <see cref="LoopCentre"/>, at
        /// <see cref="AltitudeM"/> above the highest ground under it, once every <see cref="LoopSeconds"/>, banked inwards.</summary>
        public const float LoopRadiusM = 16000f, AltitudeM = 4200f, LoopSeconds = 48f, BankDegrees = 28f;
        /// <summary>The loop's phase (0..1) used for still images (screenshots, editor renders), so they are repeatable: the
        /// aircraft over the coast off Tel Aviv, inside the tilt-shift's sharp band, its shadow on the coastal plain (chosen
        /// from renders at 0.40-0.74, docs/units.md).</summary>
        public const float StillPhase = 0.52f;

        /// <summary>Where the slice stands, inside the gameplay03 frame: the trio on the coastal plain east of Tel Aviv, in
        /// the frame's sharp band and clear of every settlement label (at 31.93 N it stood on Ramla's label), the loop over
        /// Tel Aviv and the coast, the corvette off Jaffa.</summary>
        public static readonly (double lon, double lat) TrioAt = (34.88, 32.01), LoopCentre = (34.86, 32.03), ShipAt = (34.63, 32.02);
        public const float ShipHeadingDegrees = 200f;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>Only "units" (WebGL URL) or -units (desktop) asks for it. A plain "shot" does not (controller ruling,
        /// 2026-10-11): every track's checkpoint metrics stay comparable, and the units' checkpoint adds the flag
        /// (tools/art_checkpoint.sh --units).</summary>
        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "units") : args.Contains("-units");

        /// <summary>True when the flight should hold its still phase (screenshots: the same image every run).</summary>
        public static bool StillRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "shot") : args.Contains("-shot");

        /// <summary>The unit types. The code-built models (owner ruling, 2026-10-11: they stay as the far-zoom and fallback
        /// version) are drawn for every type at once; no unit model is loaded from Resources (#2734: the WebGL player's first
        /// download). Better models stream in over them from the Collection: the riflemen's figures (UnitFigures) and the
        /// Blender-built Spitfire (<see cref="StreamedAircraft"/>, whose catalog id is <see cref="SpitfireType"/>).</summary>
        public const string RiflemanSlouchHatType = "rifleman-1948-slouch-hat", RiflemanHelmetType = "rifleman-1948-steel-helmet",
            RiflemanWoolCapType = "rifleman-1948-wool-cap", SpitfireType = "spitfire-mk9e-101sqn-1948", CorvetteType = "corvette-k20-1948",
            S199Type = "avia-s199-101sqn-1948";

        /// <summary>The rifle trio's three looks, in the order a token adds them (1, 2 or 3 figures), and their headings.
        /// They stand at <see cref="FigureOffsets"/>, the showcase as an in-play token.</summary>
        internal static readonly (string type, UnitModels.Headgear hat, UnitModels.RiflePose pose, bool faded, float heading)[] Trio =
        {
            (RiflemanSlouchHatType, UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, 200f),
            (RiflemanHelmetType, UnitModels.Headgear.SteelHelmet, UnitModels.RiflePose.Grounded, true, 185f),
            (RiflemanWoolCapType, UnitModels.Headgear.WoolCap, UnitModels.RiflePose.Slung, false, 170f),
        };

        /// <summary>Where a token's figures stand (in hex circumradii times the spread): one in the middle, two side by side,
        /// or three side by side across the view (the middle one a step back). The placer checks them at
        /// <see cref="TrioSpread"/>; they are drawn at <see cref="DrawSpread"/>, shoulder to shoulder as in T29.</summary>
        internal static Vector2[] FigureOffsets(int figures) => figures switch
        {
            1 => new[] { Vector2.zero },
            2 => new[] { new Vector2(-0.6f, 0f), new Vector2(0.6f, 0f) },
            3 => new[] { new Vector2(-1.0f, -0.1f), new Vector2(0f, 0.2f), new Vector2(1.0f, -0.1f) },
            _ => throw new ArgumentOutOfRangeException(nameof(figures), "a token has 1, 2 or 3 figures"),
        };

        /// <summary>Builds the showcase under the map's terrain (so a map rebuild destroys it). Its meshes, palette and
        /// material are owned by the showcase root's own MapRuntimeAssets, so destroying the root (a re-spawn or a map
        /// rebuild) frees them, and the "unitshowcase" toggle is registered through that teardown. The trio stands on the centre of
        /// the hex nearest <see cref="TrioAt"/>. <paramref name="still"/> freezes the flight at <see cref="StillPhase"/>.
        /// Returns the root.</summary>
        public static GameObject Spawn(MapScene map, bool still)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("UnitShowcase needs a built map");
            var terrain = map.Terrain;
            var old = terrain.transform.Find(RootName);
            if (old != null)
            {
                foreach (var a in old.GetComponentsInChildren<MapRuntimeAssets>(true)) a.RunTeardown();
                old.SetParent(null);
                if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject); else UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterialAssets.Create)");
            var ringTemplate = Resources.Load<Material>(UnitMaterials.RingResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.RingResource}.mat is missing (run UnitMaterialAssets.Create)");

            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;   // children are placed in world space
            var assets = root.AddComponent<MapRuntimeAssets>();
            var palette = assets.Own(UnitPalette.MakeTexture(readable: false));
            var paint = assets.Own(new Material(template) { name = "UnitMiniature (palette)" });
            paint.SetTexture("_BaseMap", palette);
            var aircraftPaint = assets.Own(Glossy(paint));
            Mesh Built(Mesh m) { assets.Own(m); return UnitPlinth.Upload(m); }   // no CPU copy kept in players
            float hex = map.Hexes.hex_size_km * 1000f;

            // The trio: three riflemen shoulder to shoulder on their base, under the flag, at the clear spot nearest TrioAt.
            var centre = TrioSpot(map);
            var looks = Trio;
            var offsets = FigureOffsets(looks.Length);
            float plinth = UnitPlinth.Radius(UnitPlinth.TrioWidth(offsets, DrawSpread * hex));
            var rim = assets.Own(new Material(ringTemplate) { name = "UnitPlinthRim", color = UnitPlinth.RimColour });
            UnitPlinth.Build(map, root.transform, centre, plinth, UnitData.Israel, m => UnitPlinth.Upload(assets.Own(m)), (_, build) => Built(build()), paint, rim,
                side => UnitFlags.ClothMaterials(side, ringTemplate, assets.Own));
            var holes = new List<(Vector2 centre, float radius)> { (new Vector2(centre.x, centre.z), plinth * UnitPlinth.RimOuter) };
            for (int i = 0; i < looks.Length; i++)
            {
                var (type, hat, pose, faded, heading) = looks[i];
                var p = centre + new Vector3(offsets[i].x, 0, offsets[i].y) * DrawSpread * hex;
                p.y = UnitPlinth.FigureY(map, plinth, p.x, p.z);
                var figure = Piece(root.transform, $"Rifleman{i}", Built(UnitModels.Rifleman(hat, pose, faded, $"Rifleman1948-{i}")),
                    paint, p, Quaternion.Euler(0, heading, 0), FigureScale);
                var headgear = UnitData.Headgears.First(h => UnitRoster.RiflemanType(h) == type);
                UnitFigures.Dress(figure.transform, UnitFigures.ByHeadgear[headgear], map.Residency.Units);   // the Collection's soldier, once streamed (#665)
            }
            Ring(map, assets, ringTemplate, root.transform, centre, RingRadius * hex);

            // The aircraft: the code-built airframe and propeller, drawn until the Blender-built model streams in over them.
            var plane = new GameObject("Spitfire1948");
            plane.transform.SetParent(root.transform, false);
            var airframe = Piece(plane.transform, "Airframe", Built(UnitModels.Spitfire("Spitfire1948")), aircraftPaint, Vector3.zero, Quaternion.identity, 1f);
            var prop = Piece(plane.transform, "Propeller", Built(UnitModels.SpitfirePropeller("SpitfirePropeller")), aircraftPaint, Vector3.zero, Quaternion.identity, 1f).transform;
            prop.localPosition = UnitModels.SpitfirePropellerHub;
            // Its blur, shadow and no cast shadow (the streamed model takes the blur over when it arrives, StreamedAircraft).
            FlyingSpitfire(plane.transform, prop, assets.Own(PropellerBlurMesh()), assets.Own(BlurMaterial()),
                assets.Own(Collection.SeaAirCollection.SpitfireShadowDisc()), out var shadow, out var shadowHalf);
            plane.transform.localScale = Vector3.one * AircraftScale;
            var loopCentre = map.Frame.ToWorld(LoopCentre.lon, LoopCentre.lat, 0);
            var flight = plane.AddComponent<ShowcaseFlight>();
            flight.Configure(loopCentre, LoopRadiusM, LoopFloor(map, loopCentre, LoopRadiusM) + AltitudeM, LoopSeconds, BankDegrees, prop, still ? StillPhase : (float?)null,
                shadow, map.Terrain, SeaLevelY(map), shadowHalf);
            StreamedAircraft.Attach(plane, airframe, prop);

            // The corvette, at the sea surface.
            var ship = Ground(map, map.Frame.ToWorld(ShipAt.lon, ShipAt.lat, 0));
            Piece(root.transform, "Corvette1948", Built(UnitModels.Corvette("CorvetteK20")), paint, ship,
                Quaternion.Euler(0, ShipHeadingDegrees, 0), ShipScale);

            HideTrees(map, ToggleName, holes);
            assets.OnTeardown(() => HideTrees(map, ToggleName, NoHoles));
            assets.OnTeardown(VisualToggles.Register(ToggleName, on =>
            {
                if (root != null) root.SetActive(on);
                HideTrees(map, ToggleName, on ? holes : NoHoles);
            }));
            return root;
        }

        static readonly IReadOnlyList<(Vector2 centre, float radius)> NoHoles = Array.Empty<(Vector2, float)>();

        /// <summary>Leaves out the map's kit trees under <paramref name="holes"/> for <paramref name="owner"/> (an empty list
        /// gives them back), at draw time (Kits.VegetationLayer.Hide), so the kits' layout never changes. A map without kits
        /// has no trees to hide.</summary>
        internal static void HideTrees(MapScene map, string owner, IReadOnlyList<(Vector2 centre, float radius)> holes)
        {
            if (map == null) return;
            var veg = map.GetComponentInChildren<Kits.VegetationLayer>(true);
            if (veg != null) veg.Hide(owner, holes);
        }

        /// <summary>One code-built unit model: <paramref name="mesh"/> drawn with <paramref name="mat"/>, placed in world space.</summary>
        internal static GameObject Piece(Transform parent, string name, Mesh mesh, Material mat, Vector3 position, Quaternion rotation, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>The ground point at the centre of the map hex nearest (<paramref name="lon"/>, <paramref name="lat"/>),
        /// from hexes.json (as MapScene.Select places its outline), so a unit stands in one hex.</summary>
        public static Vector3 HexCentreNear(MapScene map, double lon, double lat)
        {
            var want = map.Frame.ToWorld(lon, lat, 0);
            Vector3 best = want; float bestD = float.MaxValue;
            foreach (var h in map.Hexes.hexes)
            {
                var c = map.Frame.ToWorld(h.lon, h.lat, 0);
                float d = (c.x - want.x) * (c.x - want.x) + (c.z - want.z) * (c.z - want.z);
                if (d < bestD) { bestD = d; best = c; }
            }
            return Ground(map, best);
        }

        /// <summary>Where the showcase trio stands (review of #559): the centre of the land hex nearest <see cref="TrioAt"/>,
        /// within <see cref="SpotSearchHexes"/>, whose base keeps off every railway's and road's corridor. The kits keep a
        /// clearing there when the run asks for the showcase (<see cref="Clearing"/>, MapScene.ShowcaseClearing), as they do
        /// for a unit standing in a town, so no kit building stands on the base.</summary>
        public static Vector3 TrioSpot(MapScene map) { var c = TrioSpotXZ(map); return Ground(map, new Vector3(c.x, 0f, c.y)); }

        /// <summary>The kits' clearing for the showcase: its spot and its base's reach plus <see cref="ClearingMarginM"/>.</summary>
        public static (Vector2 centre, float radius) Clearing(MapScene map) => (TrioSpotXZ(map), BaseReach(map) + ClearingMarginM);
        public const float ClearingMarginM = 300f;

        static float BaseReach(MapScene map) =>
            UnitPlinth.Radius(UnitPlinth.TrioWidth(FigureOffsets(3), DrawSpread * map.Hexes.hex_size_km * 1000f)) * UnitPlinth.RimOuter;

        static Vector2 TrioSpotXZ(MapScene map)
        {
            float hex = map.Hexes.hex_size_km * 1000f, reach = BaseReach(map);
            var w = map.Frame.ToWorld(TrioAt.lon, TrioAt.lat, 0); var want = new Vector2(w.x, w.z);
            var candidates = map.Hexes.hexes.Where(h => h.land).Select(h => { var c = map.Frame.ToWorld(h.lon, h.lat, 0); return new Vector2(c.x, c.z); })
                .Where(c => (c - want).magnitude <= SpotSearchHexes * hex).OrderBy(c => (c - want).sqrMagnitude).ToList();
            foreach (var c in candidates)
                if (RoutesClear(map, c, reach)) return c;
            return candidates.Count > 0 ? candidates[0] : want;
        }

        /// <summary>True when a base reaching <paramref name="reach"/> (m) from <paramref name="c"/> keeps off every route
        /// corridor.</summary>
        internal static bool RoutesClear(MapScene map, Vector2 c, float reach)
        {
            foreach (var (line, half) in map.RouteCorridors)
                for (int i = 0; i + 1 < line.Count; i++)
                    if (UnitGround.DistanceToSegment(c, line[i], line[i + 1]) < half + reach) return false;
            return true;
        }

        /// <summary>The point on the terrain under <paramref name="p"/> (world x, z).</summary>
        public static Vector3 Ground(MapScene map, Vector3 p)
        {
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y;
            return p;
        }

        /// <summary>The highest ground under the loop (sampled every 5 degrees and at its centre), so the aircraft never
        /// dips into a ridge; its altitude is constant, so its shadow keeps one offset.</summary>
        public static float LoopFloor(MapScene map, Vector3 centre, float radius)
        {
            float top = Ground(map, centre).y;
            for (int a = 0; a < 360; a += 5)
            {
                var p = centre + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)) * radius;
                top = Mathf.Max(top, Ground(map, p).y);
            }
            return top;
        }

        /// <summary>The selection ring: a thin flat band on the ground around the trio, drawn with the hex selection's
        /// unlit ivory and depth offset (the promo's white ring). Each vertex sits <see cref="RingClearanceM"/> above the
        /// highest ground sampled across the band at its own angle and halfway to each neighbour, so a ridge between two
        /// vertices cannot rise through the straight edge between them (MapBuilder.DrapedRing's rule).</summary>
        internal static GameObject Ring(MapScene map, MapRuntimeAssets assets, Material ringTemplate, Transform parent, Vector3 centre, float radius)
        {
            const int n = 96;
            var verts = new List<Vector3>(2 * n); var tris = new List<int>(6 * n);
            float w = RingWidth * radius / RingRadius;
            float Across(float step)
            {
                float a = 2f * Mathf.PI * step / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                return Mathf.Max(Ground(map, centre + d * (radius + w / 2)).y, Ground(map, centre + d * (radius - w / 2)).y);
            }
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float y = Mathf.Max(Across(i), Mathf.Max(Across(i - 0.5f), Across(i + 0.5f))) + RingClearanceM;
                var outer = centre + d * (radius + w / 2); var inner = centre + d * (radius - w / 2);
                outer.y = y; inner.y = y;
                verts.Add(outer); verts.Add(inner);
            }
            for (int i = 0; i < n; i++)
            {
                int a = 2 * i, b = 2 * ((i + 1) % n);
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });   // the ring shader draws both faces
            }
            var mesh = assets.Own(new Mesh { name = "UnitSelectionRing" });
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = ringTemplate;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
