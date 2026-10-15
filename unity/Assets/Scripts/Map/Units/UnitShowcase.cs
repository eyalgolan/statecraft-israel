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
        /// <summary>The house T29's ratio is measured against at close30, as the T54 audit did: the White City block beside
        /// the showcase trio, the kit's seven-storey heart block (7 x 3.3 m at <see cref="Kits.KitScale.DefaultCityBlocks"/>;
        /// measured on the close30 render at about 1,300 m). A two-storey village house is about half of it.</summary>
        public const float ReferenceHouseM = 7f * 3.3f * Kits.KitScale.DefaultCityBlocks;
        /// <summary>A figure's height over <see cref="ReferenceHouseM"/>: about 1.8, as T29.</summary>
        public const float HouseRatio = UnitBadge.FigureHeightM * FigureScale / ReferenceHouseM;
        public const float AircraftScale = 420f;        // span 11.2 m x 420 = 4.7 km: wider than a man is tall
        public const float ShipScale = 95f;             // 63.4 m x 95 = 6.0 km: about a hex, like the promo's boats
        /// <summary>Trio spacing and the ring, as fractions of the hex circumradius. <see cref="TrioSpread"/> is the spacing
        /// the placer checks a token's spot with (UnitGround), kept so no spot moved.</summary>
        public const float TrioSpread = 0.40f, RingRadius = 0.66f, RingWidth = 0.035f, RingClearanceM = 25f;
        /// <summary>The spacing the figures are drawn at: shoulder to shoulder, as T29's trios (about 1.15 figure heights
        /// across; ours 1.15 too: 2 x 950 m between the outer figures' centres plus a figure's width, against a 2.5 km man).
        /// Narrower than <see cref="TrioSpread"/>, so the drawn figures stand inside the ground the placer checked.</summary>
        public const float DrawSpread = 0.19f;
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

        /// <summary>The art hook (owner ruling, 2026-10-11): promo-quality close-zoom models come from a separate AI-3D track,
        /// and the code-built models stay as the far-zoom and fallback version. A prefab at
        /// Resources/<see cref="AuthoredFolder"/>&lt;unit type&gt; replaces the code-built model of that type. It follows the
        /// same convention: real metres, facing +Z, origin on the ground (an aircraft: its centre of gravity), with its own
        /// materials; an authored aircraft brings its own propeller. Without one, the code-built mesh is used. Unit types:
        /// the <c>*Type</c> constants below.</summary>
        public const string AuthoredFolder = "Units/Authored/";
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

        /// <summary>Finds an authored model for a unit type, or null (a seam: tests replace it).</summary>
        internal static Func<string, GameObject?> AuthoredModel = type => Resources.Load<GameObject>(AuthoredFolder + type);

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
            Mesh Built(Mesh m) { assets.Own(m); m.UploadMeshData(true); return m; }   // no CPU copy kept in players
            float hex = map.Hexes.hex_size_km * 1000f;

            // The trio: three riflemen shoulder to shoulder on their base, under the flag, at the clear spot nearest TrioAt.
            var centre = TrioSpot(map);
            var looks = Trio;
            var offsets = FigureOffsets(looks.Length);
            float plinth = UnitPlinth.Radius(UnitPlinth.TrioWidth(offsets, DrawSpread * hex));
            var rim = assets.Own(new Material(ringTemplate) { name = "UnitPlinthRim", color = UnitPlinth.RimColour });
            var flagWhite = assets.Own(new Material(ringTemplate) { name = "UnitFlag (white)", color = UnitFlags.WhiteColour });
            var flagBlue = assets.Own(new Material(ringTemplate) { name = "UnitFlag (blue)", color = UnitFlags.BlueColour });
            flagBlue.renderQueue = flagWhite.renderQueue + 1;   // the blue over the white
            UnitPlinth.Build(map, root.transform, centre, plinth, UnitData.Israel, m => assets.Own(m), (_, build) => Built(build()), paint, rim, flagWhite, flagBlue);
            var holes = new List<(Vector2 centre, float radius)> { (new Vector2(centre.x, centre.z), plinth * UnitPlinth.RimOuter) };
            for (int i = 0; i < looks.Length; i++)
            {
                var (type, hat, pose, faded, heading) = looks[i];
                var p = centre + new Vector3(offsets[i].x, 0, offsets[i].y) * DrawSpread * hex;
                p.y = UnitPlinth.FigureY(map, plinth, p.x, p.z);
                Piece(root.transform, $"Rifleman{i}", type, () => Built(UnitModels.Rifleman(hat, pose, faded, $"Rifleman1948-{i}")),
                    paint, p, Quaternion.Euler(0, heading, 0), FigureScale);
            }
            Ring(map, assets, ringTemplate, root.transform, centre, RingRadius * hex);

            // The aircraft and, when code-built, its propeller.
            var plane = new GameObject("Spitfire1948");
            plane.transform.SetParent(root.transform, false);
            Piece(plane.transform, "Airframe", SpitfireType, () => Built(UnitModels.Spitfire("Spitfire1948")), paint,
                Vector3.zero, Quaternion.identity, 1f, out bool authoredAircraft);
            Transform? prop = null;
            if (!authoredAircraft)
            {
                prop = Piece(plane.transform, "Propeller", Built(UnitModels.SpitfirePropeller("SpitfirePropeller")), paint, Vector3.zero, Quaternion.identity, 1f).transform;
                prop.localPosition = UnitModels.SpitfirePropellerHub;
            }
            plane.transform.localScale = Vector3.one * AircraftScale;
            var loopCentre = map.Frame.ToWorld(LoopCentre.lon, LoopCentre.lat, 0);
            var flight = plane.AddComponent<ShowcaseFlight>();
            flight.Configure(loopCentre, LoopRadiusM, LoopFloor(map, loopCentre, LoopRadiusM) + AltitudeM, LoopSeconds, BankDegrees, prop, still ? StillPhase : (float?)null);

            // The corvette, at the sea surface.
            var ship = Ground(map, map.Frame.ToWorld(ShipAt.lon, ShipAt.lat, 0));
            Piece(root.transform, "Corvette1948", CorvetteType, () => Built(UnitModels.Corvette("CorvetteK20")), paint, ship,
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

        /// <summary>One unit model: the authored prefab for <paramref name="type"/> when there is one (the art hook),
        /// otherwise the code-built mesh.</summary>
        static GameObject Piece(Transform parent, string name, string type, Func<Mesh> codeBuilt, Material paint, Vector3 position,
            Quaternion rotation, float scale) => Piece(parent, name, type, codeBuilt, paint, position, rotation, scale, out _);

        internal static GameObject Piece(Transform parent, string name, string type, Func<Mesh> codeBuilt, Material paint, Vector3 position,
            Quaternion rotation, float scale, out bool authored)
        {
            var prefab = AuthoredModel(type);
            authored = prefab != null;
            if (prefab == null) return Piece(parent, name, codeBuilt(), paint, position, rotation, scale);
            var go = UnityEngine.Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

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
