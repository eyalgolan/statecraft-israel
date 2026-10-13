#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Ambient dressing beside the building models: ships, a fishing boat, an aircraft, vehicles, a locomotive, a
    /// fort and figures from the same Tripo prefabs, at fixed spots under one "PreviewProps"
    /// root beside "Settlements". BuildingPreview.Apply places and removes it (so GameRoot is unchanged), and
    /// MapScene.Build's Clear destroys it with the rest of the map. Art dressing, not sourced content: the spots are
    /// approximate, each the nearest to its story's place that clears every kit town (the game settlements', the 1948
    /// localities' and the Bedouin camps' footprints, as upstream's unit tokens keep off them) and every other prop
    /// (BuildingPreviewDressingTests checks the table against the kits). Nothing here touches the simulation, and
    /// nothing has a collider, so map clicks still pick only the terrain (MapPointer). An entry none of whose prefabs loads
    /// places nothing.</summary>
    public static class PreviewProps
    {
        public const string RootName = "PreviewProps", InstancePrefix = "prop-";

        /// <summary>Symbolic map scale, each prop's largest dimension (m), sized from upstream's unit miniatures so the props read
        /// at the 120 km starting view (Tal, 13 October 2026: big enough to see well, not too big): PieceShare of a unit's
        /// figure (UnitBadge.FigureHeightM x UnitShowcase.FigureScale, about 3.6 km), ship (the corvette's 63.4 m hull x
        /// ShipScale, about 6 km) and aircraft (the Spitfire's 11.2 m span x AircraftScale, about 4.7 km; UnitModels), so the
        /// game's own units stay the larger pieces. Drawn times BuildingPreview.Scale (?previewscale).</summary>
        public const float PieceShare = 0.75f;
        public const float FigureM = PieceShare * Units.UnitBadge.FigureHeightM * Units.UnitShowcase.FigureScale;
        public const float ShipM = PieceShare * 63.4f * Units.UnitShowcase.ShipScale, AircraftM = PieceShare * 11.2f * Units.UnitShowcase.AircraftScale;
        /// <summary>The vehicles keep their earlier proportion to the figures (450 to 350 m), and the Latrun fort, a building,
        /// the settlements' BuildingPreview.BuildingScale (800 m before).</summary>
        public const float VehicleM = FigureM * 9f / 7f, FortM = BuildingPreview.BuildingScale * 800f;
        /// <summary>Props those sizes do not fit: a small fishing boat, half a ship hull; a locomotive with tender (long and slim:
        /// at VehicleM it would read smaller than the truck), twice a figure; and a child, 5/7 of a figure (at FigureM the boy
        /// stood as tall as the adults).</summary>
        public const float BoatM = ShipM / 2f, TrainM = 2f * FigureM, ChildM = FigureM * 5f / 7f;
        /// <summary>The high-res building groups (13 October modules), largest dimension: a city's 4 km, a town's or kibbutz's
        /// 3.2 km (twice the Latrun fort), a village's 2.6 km, a single block 2.4 km.</summary>
        public const float CityGroupM = 4000f, TownGroupM = 3200f, VillageGroupM = 2600f, BlockM = 2400f;
        /// <summary>Height of an airborne prop's bottom above the highest ground under it: low, on approach (two thirds of the
        /// aircraft's span, as before).</summary>
        public const float AirAltitudeM = 2400f;
        /// <summary>Share of a floating hull's height below the sea surface, so it sits in the water rather than on it.</summary>
        public const float DraftShare = 0.1f;
        /// <summary>PreviewBob's gentle motion: a sea swell, and a slower drift aloft. Amplitude (m), period (s), roll (degrees).
        /// About 2 px of rise at the 9 km zoom.</summary>
        public const float SeaBobM = 12f, SeaBobPeriodS = 7f, SeaRollDeg = 1.2f, AirBobM = 30f, AirBobPeriodS = 11f, AirRollDeg = 2f;
        /// <summary>Spreads the bob phases (the golden ratio's fraction): fixed per entry, so every run moves alike.</summary>
        const float PhaseStep = 0.618034f;

        /// <summary>Where a prop rests: on the highest ground under it, on the sea surface (RestY), or AirAltitudeM above
        /// the ground.</summary>
        public enum Kind { Land, Sea, Air }
        /// <summary>None stands still (an Animator is switched off); Bob adds PreviewBob; Idle lets the prefab's Animator play
        /// its default state (PrototypeImport makes it the idle clip).</summary>
        public enum Motion { None, Bob, Idle }

        /// <summary>One prop: the first of <see cref="Names"/> whose prefab loads, centred on (Lon, Lat), turned Yaw degrees
        /// and scaled so its own largest dimension is SizeM, whatever the turn.</summary>
        public sealed class Prop
        {
            public readonly double Lon, Lat; public readonly float Yaw, SizeM; public readonly Kind Kind; public readonly Motion Motion;
            public readonly IReadOnlyList<string> Names;

            public Prop(double lon, double lat, float yaw, float sizeM, Kind kind, Motion motion, params string[] names)
            { Lon = lon; Lat = lat; Yaw = yaw; SizeM = sizeM; Kind = kind; Motion = motion; Names = names; }
        }

        /// <summary>The props, one line each; a later name is a fallback (another export of the same model). Coordinates are
        /// approximate: the nearest spot to each story's place that keeps the prop clear of every kit town and every other
        /// prop (13 October 2026, when the props grew to the starting view); yaws are first guesses, to be tuned from
        /// BuildingPreviewCapture's renders.</summary>
        public static IReadOnlyList<Prop> All { get; } = new[]
        {
            // Each row names the 13 October high-res export first (Tal, 14 October 2026) and the first batch's after it, as
            // its fallback; ships use the waterline-cut exports.
            // The Altalena off the Tel Aviv coast, far enough out that its hull clears Tel Aviv's and Jaffa's towns.
            new Prop(34.666, 32.096, 20f, ShipM, Kind.Sea, Motion.Bob, "vehicle-altalena-lst-1948-v2wl", "altalena-lst-v1", "altalena-lst-v1-7k"),
            // An immigrant steamer in Haifa Bay, north of the city's town.
            new Prop(35.016, 32.911, 60f, ShipM, Kind.Sea, Motion.Bob, "vehicle-immigrant-steamer-1948-v2wl", "immigrant-steamer-v1"),
            // A C-54 on approach to Lod airport over the plain south of Lod.
            new Prop(34.906, 31.903, 300f, AircraftM, Kind.Air, Motion.Bob, "figure-vehicle-c54-skymaster-1948-v2", "airliner-c54-v1"),
            // A "sandwich" armoured truck on the road below Bab al-Wad, on the way up to Jerusalem.
            new Prop(34.996, 31.806, 75f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-sandwich-convoy-1948-v2", "sandwich-armoured-truck-v1"),
            // The Tegart fort at Latrun.
            new Prop(34.967, 31.834, BuildingPreview.DefaultYaw, FortM, Kind.Land, Motion.None, "b12-tegart-fort-v2", "tegart-fort-v1"),
            // Figures: a pioneer farmer in the Lower Galilee fields west of the Jordan valley, an immigrant woman on the Carmel
            // south of Haifa, a soldier west of the Latrun fort.
            new Prop(35.519, 32.668, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c04-sabra-pioneer-v3", "figure-pioneer-farmer-v1-tpose", "figure-pioneer-farmer-v1"),
            new Prop(35.011, 32.738, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c10-woman-blue-dress-v2", "figure-immigrant-woman-v1"),
            new Prop(34.935, 31.840, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c06-young-recruit-v2", "figure-soldier-1948-v1"),
            // A kibbutz field worker in the Harod valley fields, north of Ein Harod (the Palmach scout's model: the Palmach
            // lived and trained on the kibbutzim).
            new Prop(35.364, 32.583, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c12f-palmach-scout-v2", "figure-kibbutz-woman-v1"),
            // A Palmach scout on a Negev trek, south of Revivim.
            new Prop(34.736, 31.016, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c12f-palmach-scout-v2", "figure-palmach-scout-v1"),
            // A field nurse on the plain between Tel Aviv and Lod (Tel HaShomer, the army hospital of 1948, lies in the towns).
            new Prop(34.887, 32.000, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c13-nurse-v2", "figure-nurse-1948-v1"),
            // An immigrant townsman on the Carmel, east of the immigrant woman, turned a little toward her (the first batch's
            // boy stood here; the high-res batch has no child).
            new Prop(35.076, 32.771, 230f, FigureM, Kind.Land, Motion.Idle, "figure-c02-townsman-v2", "figure-immigrant-child-v1"),
            // A newly arrived immigrant on the Carmel, west of the immigrant woman, turned a little toward her.
            new Prop(34.974, 32.722, 170f, FigureM, Kind.Land, Motion.Idle, "figure-c01-arriving-man-v2", "figure-elder-immigrant-v1"),
            // An immigrant on the plain west of Lod, where the 1949-50 airlift from Aden landed (the arriving man's model).
            new Prop(34.802, 31.985, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c01-arriving-man-v2", "figure-yemenite-immigrant-v1"),
            // A Galilee villager in the fields south of Nazareth (the irregular's model, unarmed in its idle).
            new Prop(35.298, 32.663, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c09-irregular-v2", "figure-arab-villager-v1"),
            // A builder on the coastal plain south-west of Rishon LeZion, where the new housing of the 1950s spread.
            new Prop(34.721, 31.901, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c03-smith-carpenter-v2", "figure-builder-1950-v1"),
            // The high-res batch's other characters, near the story each tells: an Arab Legion soldier east of the Latrun fort,
            // an irregular in the hills above Bab al-Wad, a veteran fighter with the Palmach scout in the Negev, a veteran
            // pioneer in the Jezreel fields (still: his only idle is a walk), and a townsman by the Hadera road where the bus runs.
            new Prop(35.002, 31.870, 160f, FigureM, Kind.Land, Motion.Idle, "figure-c08-arab-legion-v2"),
            new Prop(35.052, 31.769, 240f, FigureM, Kind.Land, Motion.Idle, "figure-c09-irregular-v2"),
            new Prop(34.790, 30.975, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.Idle, "figure-c07-veteran-fighter-v2"),
            new Prop(35.395, 32.508, BuildingPreview.DefaultYaw, FigureM, Kind.Land, Motion.None, "figure-c05-pioneer-veteran-v2"),
            new Prop(34.975, 32.385, 220f, FigureM, Kind.Land, Motion.Idle, "figure-c02-townsman-v2"),
            // Vehicles, angled to the camera (the locomotive broadside). The jeep, tractor and truck are long along their own z;
            // the bus, wagon and locomotive along x with the front at +x, so they head to bearing yaw + 90; the boat's bow is
            // at -x, so it heads to yaw + 270 (turntable renders, 11 October 2026). The high-res exports all have their front
            // at +z (TurnOf keeps each heading).
            // An unarmed jeep on the track west of Revivim, heading for it.
            new Prop(34.681, 31.036, 140f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-willys-jeep-1948-v2", "vehicle-willys-jeep-v1"),
            // An intercity bus near Hadera, bound for Tel Aviv.
            new Prop(34.932, 32.411, 140f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-intercity-bus-1948-v2", "vehicle-intercity-bus-v1"),
            // A kibbutz tractor ploughing the Harod valley floor, east of Ein Harod.
            new Prop(35.423, 32.590, 235f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-farm-tractor-1948-v2", "vehicle-farm-tractor-v1"),
            // A truck loaded with orange crates in the groves south of Rehovot.
            new Prop(34.857, 31.839, 225f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-citrus-truck-1948-v2", "vehicle-citrus-truck-v1"),
            // A moshav farmer's horse wagon with hay and milk churns in the Jezreel valley fields north-east of Nahalal, heading home.
            new Prop(35.228, 32.713, 155f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-horse-wagon-1948-v2", "vehicle-horse-wagon-v1"),
            // A steam locomotive with tender on the Jaffa-Jerusalem line in the Shephelah, east of the coastal towns (the static
            // high-res export: the animated one's driving wheels clip).
            new Prop(34.887, 31.787, 10f, TrainM, Kind.Land, Motion.None, "vehicle-steam-locomotive-1948-v2", "vehicle-steam-locomotive-v1"),
            // A wooden fishing boat just off the beach west of Hadera, heading out (open sea all round).
            new Prop(34.857, 32.429, 340f, BoatM, Kind.Sea, Motion.Bob, "vehicle-fishing-boat-1948-v2wl", "vehicle-fishing-boat-v1"),
            // New in the high-res batch: a canvas-tilt convoy lorry on the coastal road south of Rehovot, a 65 mm
            // "Napoleonchik" mountain gun on the way down to the Jordan valley, south-west of the Kinneret (Degania, May 1948).
            new Prop(34.824, 31.760, 20f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-canvas-lorry-1948-v2"),
            new Prop(35.476, 32.650, 250f, VehicleM, Kind.Land, Motion.None, "figure-vehicle-napoleonchik-65mm-1948-v2"),
            // Animals (13 October batch; the walk-only ones stand still, the horse and sheep play their idle): a camel and a striped hyena in
            // the Negev, an ibex in the Judean desert, a flock's sheep in the Hebron hills, a donkey by the Galilee villager,
            // a draught horse in the Jezreel fields by the moshav wagon.
            new Prop(34.942, 30.908, 240f, VehicleM, Kind.Land, Motion.None, "figure-dromedary-camel-1948-v1"),
            new Prop(34.820, 30.800, 160f, FigureM, Kind.Land, Motion.None, "figure-striped-hyena-1948-v1"),
            new Prop(35.300, 31.420, 220f, FigureM, Kind.Land, Motion.None, "figure-nubian-ibex-1948-v1"),
            new Prop(35.116, 31.458, 200f, ChildM, Kind.Land, Motion.Idle, "figure-sheep-idle-1948-v1", "figure-sheep-1948-v1"),
            new Prop(35.255, 32.640, 140f, FigureM, Kind.Land, Motion.None, "figure-donkey-1948-v1"),
            new Prop(35.241, 32.761, 230f, VehicleM, Kind.Land, Motion.Idle, "figure-draught-horse-idle-1948-v2", "figure-draught-horse-1948-v2"),
            // The 13 October building modules as town groups (PrototypeImport.Clusters) beside the towns, whose first-batch
            // models stay (Tal, 14 October 2026: keep the buildings there, add the new ones): each on the nearest open land to
            // its town that clears every kit town and prop. Tel Aviv's group stands inland between Petah Tikva and Kfar Saba,
            // the coast being all towns; Jerusalem's in the corridor west of the city; Haifa's on the bay's industrial plain.
            new Prop(34.924, 32.126, BuildingPreview.DefaultYaw, CityGroupM, Kind.Land, Motion.None, "cluster-tel-aviv-v2"),
            new Prop(35.118, 31.780, BuildingPreview.DefaultYaw, CityGroupM, Kind.Land, Motion.None, "cluster-jerusalem-v2"),
            new Prop(35.098, 32.844, BuildingPreview.DefaultYaw, CityGroupM, Kind.Land, Motion.None, "cluster-haifa-v2"),
            new Prop(34.894, 32.334, BuildingPreview.DefaultYaw, TownGroupM, Kind.Land, Motion.None, "cluster-netanya-v2"),
            // A moshava (house, barn, water tower) east of Petah Tikva, and the wine moshava's group by Zikhron Ya'akov.
            new Prop(34.972, 32.150, BuildingPreview.DefaultYaw, TownGroupM, Kind.Land, Motion.None, "cluster-moshava-v2"),
            new Prop(34.978, 32.594, BuildingPreview.DefaultYaw, TownGroupM, Kind.Land, Motion.None, "cluster-moshava-winery-v2"),
            // A kibbutz (dining hall, communal building, water tower) in the Harod valley, a moshav's farmsteads south of
            // Nahalal, a stone village in the Lower Galilee.
            new Prop(35.356, 32.528, BuildingPreview.DefaultYaw, TownGroupM, Kind.Land, Motion.None, "cluster-kibbutz-v2"),
            new Prop(35.200, 32.658, BuildingPreview.DefaultYaw, TownGroupM, Kind.Land, Motion.None, "cluster-moshav-v2"),
            new Prop(35.400, 32.800, BuildingPreview.DefaultYaw, VillageGroupM, Kind.Land, Motion.None, "cluster-stone-village-v2"),
            // A 1950s shikun block by the builder south-west of Rishon LeZion, and British army barracks near Sarafand.
            new Prop(34.764, 31.922, BuildingPreview.DefaultYaw, BlockM, Kind.Land, Motion.None, "b11-shikun-housing-v2"),
            new Prop(34.930, 31.964, BuildingPreview.DefaultYaw, BlockM, Kind.Land, Motion.None, "b18-military-barracks-v2"),
        };

        /// <summary>Turn added to a row's yaw when that export is the one that loads (degrees), so the high-res vehicles, built
        /// with their front at +z, keep the heading the row's comment gives the first batch's model (front at +x: +90; bow at
        /// -x: +270). A name without an entry adds nothing.</summary>
        static readonly Dictionary<string, float> TurnOf = new Dictionary<string, float>
        {
            ["figure-vehicle-intercity-bus-1948-v2"] = 90f, ["figure-vehicle-horse-wagon-1948-v2"] = 90f,
            ["vehicle-steam-locomotive-1948-v2"] = 90f, ["vehicle-fishing-boat-1948-v2wl"] = 270f,
        };

        /// <summary>Places <see cref="All"/> (BuildingPreview.Apply's call); off, as on GameRoot's default path, it does not
        /// read the table.</summary>
        public static int Apply(MapScene map, bool enabled, Func<string, GameObject?> load) =>
            Apply(map, enabled, load, enabled ? All : Array.Empty<Prop>());

        /// <summary>Removes what an earlier Apply placed; then, when <paramref name="enabled"/> and the map is built, places
        /// each of <paramref name="props"/> whose prefab <paramref name="load"/> returns (BuildingPreview.Stand: no collider,
        /// shadows on), scaled to its SizeM times BuildingPreview.Scale unturned and then turned (Stand fits the turned bounds,
        /// which grow with the turn: a hull at 60 degrees would come out about 6% too long), recentred on its spot, resting as
        /// its Kind says, moving as its Motion says. Off with nothing placed loads nothing and touches nothing. Returns the
        /// number placed.</summary>
        public static int Apply(MapScene map, bool enabled, Func<string, GameObject?> load, IReadOnlyList<Prop> props)
        {
            if (map == null) return 0;
            var old = map.transform.Find(RootName);
            if (old != null) BuildingPreview.Discard(old.gameObject);
            if (!enabled || map.Terrain == null) return 0;
            Transform? root = null; int placed = 0;
            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i];
                var (name, prefab) = BuildingPreview.Resolve(p.Names, load);
                if (name == null || prefab == null) continue;
                if (root == null) { root = new GameObject(RootName).transform; root.SetParent(map.transform, true); }
                var at = map.Frame.ToWorld(p.Lon, p.Lat, 0);
                float size = p.SizeM * BuildingPreview.Scale;
                var go = BuildingPreview.Stand(prefab, at, 0f, root, $"{InstancePrefix}{i:00}-{name}", size, size);
                if (go == null) continue;
                go.transform.rotation = Quaternion.Euler(0f, p.Yaw + (TurnOf.TryGetValue(name, out var turn) ? turn : 0f), 0f);
                Animate(go, p, i);
                var b = BuildingPreview.Measure(go)!.Value;
                go.transform.position += new Vector3(at.x - b.center.x, 0f, at.z - b.center.z);   // the turn moved the footprint
                b = BuildingPreview.Measure(go)!.Value;
                go.transform.position += Vector3.up * (RestY(map.Terrain, p.Kind, b) - b.min.y);
                if (p.Motion == Motion.Bob)
                {
                    bool air = p.Kind == Kind.Air;
                    go.AddComponent<PreviewBob>().Init(air ? AirBobM : SeaBobM, air ? AirBobPeriodS : SeaBobPeriodS, air ? AirRollDeg : SeaRollDeg, Mathf.Repeat(i * PhaseStep, 1f));
                }
                placed++;
            }
            return placed;
        }

        /// <summary>The world height of 0 m (the sea surface), as MapShaderGlobals published it when the map was built: the
        /// water shader's own level, so a hull meets the drawn water.</summary>
        public static float SeaLevelY() => Shader.GetGlobalVector(MapShaderGlobals.Map).z;

        /// <summary>Where the bottom of a prop with bounds <paramref name="b"/> rests. The ground is the highest terrain under
        /// its centre and the corners of its footprint, so a long prop on a slope (the relief is exaggerated 3x) is not buried
        /// at its uphill end (MapScene.Select's idiom). A sea prop floats where all that ground is open sea (within
        /// MapBuilder.SeaToleranceM of 0 m): its keel DraftShare of its height below the surface by design, so it sits in the
        /// water. The heightmap has no bathymetry and the terrain itself draws the water there, so that keel is the one thing
        /// below the terrain's surface. Elsewhere (a shore, or a lake below sea level such as the Kinneret, which the 0 m
        /// surface would leave it hanging hundreds of metres above) it rests on the ground.</summary>
        public static float RestY(Terrain terrain, Kind kind, Bounds b)
        {
            float ground = float.MinValue;
            foreach (var (x, z) in new[] { (b.center.x, b.center.z), (b.min.x, b.min.z), (b.min.x, b.max.z), (b.max.x, b.min.z), (b.max.x, b.max.z) })
                ground = Mathf.Max(ground, terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y);
            if (kind == Kind.Air) return ground + AirAltitudeM;
            if (kind == Kind.Land) return ground;
            float sea = SeaLevelY();
            return Mathf.Abs(ground - sea) <= MapBuilder.SeaToleranceM * MapBuilder.VerticalExaggeration ? sea - DraftShare * b.size.y : ground;
        }

        /// <summary>Idle: every Animator plays (no root motion), and in edit mode, where Animators do not run, the default
        /// clip's first frame is sampled so captures show the idle pose rather than the bind pose. Otherwise every Animator is
        /// off, holding the pose the prefab was saved in.</summary>
        static void Animate(GameObject go, Prop p, int index)
        {
            foreach (var a in go.GetComponentsInChildren<Animator>(true))
            {
                a.applyRootMotion = false;
                a.enabled = p.Motion == Motion.Idle;
                if (a.enabled && !Application.isPlaying) SampleIdle(a);
            }
        }

        /// <summary>Samples the Animator's idle clip (the first named like "idle", else its first clip) at time 0 onto its own
        /// object: edit-mode renders only (captures, turntables). No clip, nothing done.</summary>
        public static void SampleIdle(Animator a)
        {
            var clips = a.runtimeAnimatorController != null ? a.runtimeAnimatorController.animationClips : Array.Empty<AnimationClip>();
            var clip = clips.FirstOrDefault(c => c != null && c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips.FirstOrDefault(c => c != null);
            if (clip != null) clip.SampleAnimation(a.gameObject, 0f);
        }
    }
}
