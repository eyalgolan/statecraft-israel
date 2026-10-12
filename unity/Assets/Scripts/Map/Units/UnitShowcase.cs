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

        /// <summary>Symbolic scale, measured on the promo against the art checkpoint (docs/units.md): at 1920 px wide the
        /// promo's riflemen stand about 87 px tall inside a ring about 190 px across; the first checkpoint, at 2600x, drew
        /// them 113 px tall. So a man is drawn <see cref="FigureScale"/> times his real size (about 3.6 km at 2000x), and
        /// the aircraft and the ship are sized against him, as Civ draws them, not against each other.</summary>
        public const float FigureScale = 2000f;
        public const float AircraftScale = 420f;        // span 11.2 m x 420 = 4.7 km: wider than a man is tall
        public const float ShipScale = 95f;             // 63.4 m x 95 = 6.0 km: about a hex, like the promo's boats
        /// <summary>Trio spacing and the ring, as fractions of the hex circumradius.</summary>
        public const float TrioSpread = 0.40f, RingRadius = 0.66f, RingWidth = 0.035f, RingClearanceM = 25f;
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

        /// <summary>The rifle trio's three looks, in the order a token adds them (1, 2 or 3 figures): offset (in hex
        /// circumradii times <see cref="TrioSpread"/>) and heading.</summary>
        internal static readonly (string type, UnitModels.Headgear hat, UnitModels.RiflePose pose, bool faded, UnityEngine.Vector2 offset, float heading)[] Trio =
        {
            (RiflemanSlouchHatType, UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, new Vector2(-0.62f, -0.30f), 200f),
            (RiflemanHelmetType, UnitModels.Headgear.SteelHelmet, UnitModels.RiflePose.Grounded, true, new Vector2(0.05f, 0.05f), 185f),
            (RiflemanWoolCapType, UnitModels.Headgear.WoolCap, UnitModels.RiflePose.Slung, false, new Vector2(0.66f, -0.40f), 170f),
        };

        /// <summary>Where an in-play token's figures stand (in hex circumradii times <see cref="TrioSpread"/>): one in the
        /// middle, two side by side, or three spread across the ring as in promo 03, side by side across the view (the
        /// middle one a step back), about 2 km apart so each figure reads on its own (review I6).</summary>
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

            // The trio: three riflemen in a loose triangle facing the camera's side of south-west, around the hex's centre.
            var centre = HexCentreNear(map, TrioAt.lon, TrioAt.lat);
            var looks = Trio;
            for (int i = 0; i < looks.Length; i++)
            {
                var (type, hat, pose, faded, offset, heading) = looks[i];
                var p = Stand(map, centre + new Vector3(offset.x, 0, offset.y) * TrioSpread * hex);
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

            assets.OnTeardown(VisualToggles.Register(ToggleName, on => { if (root != null) root.SetActive(on); }));
            return root;
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

        /// <summary>The point on the terrain under <paramref name="p"/> (world x, z).</summary>
        public static Vector3 Ground(MapScene map, Vector3 p)
        {
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y;
            return p;
        }

        /// <summary>A figure's boots reach about 0.2 m from its centre: at <see cref="FigureScale"/>, 400 m on the map.</summary>
        public const float FootRadiusM = 0.2f * FigureScale;

        /// <summary>Where a figure stands at <paramref name="p"/> (world x, z): on the highest ground under its boots
        /// (<see cref="StandHeight"/> over <see cref="FootRadiusM"/>), so on a slope no boot sinks into the hill; the downhill
        /// boot floats by at most the slope's rise over the foot radius (owner question, 2026-10-11: units on hard terrain).</summary>
        public static Vector3 Stand(MapScene map, Vector3 p)
        {
            p.y = StandHeight((x, z) => map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y, p.x, p.z, FootRadiusM);
            return p;
        }

        /// <summary>The highest of <paramref name="heightAt"/> at (x, z), on two rings inside the circle of radius r (at a
        /// half and three quarters of it) and at the corners of a 16-gon drawn round it (at r / cos 11.25 degrees), sixteen
        /// points each: on a planar slope no point of the circle is higher, and a ridge under the boots is caught too.</summary>
        public static float StandHeight(Func<float, float, float> heightAt, float x, float z, float r) => StandRange(heightAt, x, z, r).top;

        /// <summary>The highest and lowest of the same samples as <see cref="StandHeight"/>: where a figure stands, and
        /// how far down the slope under its boots goes (its base reaches there: <see cref="Stand"/>).</summary>
        public static (float top, float low) StandRange(Func<float, float, float> heightAt, float x, float z, float r)
        {
            float top = heightAt(x, z), low = top;
            foreach (var ring in new[] { 0.5f * r, 0.75f * r, r / Mathf.Cos(Mathf.PI / 16f) })
                for (int k = 0; k < 16; k++)
                {
                    float a = Mathf.PI / 8f * k, h = heightAt(x + Mathf.Cos(a) * ring, z + Mathf.Sin(a) * ring);
                    top = Mathf.Max(top, h); low = Mathf.Min(low, h);
                }
            return (top, low);
        }

        /// <summary>A figure on a slope stands on a base, as a painted miniature does: a short dark grey-brown disc (the palette deck shade) of the
        /// boots' radius from its feet down to the lowest ground under them, so the downhill boot never floats. Not drawn
        /// on ground flatter than this rise (m).</summary>
        public const float BaseFromRiseM = 20f;

        /// <summary>The unit disc a base is drawn with (radius 1, from y = -1 to 0), scaled per figure.</summary>
        public static Mesh BaseMesh(string name) =>
            new MiniatureMesh().Lathe(Matrix4x4.identity, new List<Vector2> { new Vector2(-1f, 1f), new Vector2(0f, 1f) }, UnitPalette.Deck, 16, shade: 0.55f).Build(name);

        /// <summary>Adds the base under a figure standing at <paramref name="feet"/> when the ground under its boots falls
        /// more than <see cref="BaseFromRiseM"/> below them (see <see cref="StandRange"/>).</summary>
        internal static void AddBase(MapScene map, Transform figure, Vector3 feet, Mesh mesh, Material paint)
        {
            var (_, low) = StandRange((x, z) => map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y, feet.x, feet.z, FootRadiusM);
            float drop = feet.y - low;
            if (drop <= BaseFromRiseM) return;
            var go = Piece(figure.parent, "Base" + figure.name, mesh, paint, feet, Quaternion.identity, 1f);
            go.transform.localScale = new Vector3(FootRadiusM, drop + 30f, FootRadiusM);   // 30 m into the ground: the lowest sample is not the lowest point
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
