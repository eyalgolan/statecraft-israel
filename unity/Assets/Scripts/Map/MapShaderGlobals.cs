#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The global shader values StatecraftMap.hlsl reads, set when the map is built (MapBuilder.Build): the map's
    /// size, sea level and exaggeration, its origin and hex size, the biome colour texture and the haze. Globals, not
    /// material values, so the shared editor-made materials are never changed or instanced at run time.</summary>
    public static class MapShaderGlobals
    {
        public static readonly int Map = Shader.PropertyToID("_ScMap"), MapOrigin = Shader.PropertyToID("_ScMapOrigin"),
            Biome = Shader.PropertyToID("_ScBiomeTex"), Haze = Shader.PropertyToID("_ScHaze"), HazeWater = Shader.PropertyToID("_ScHazeWater"),
            HazeRange = Shader.PropertyToID("_ScHazeRange"), WaterOn = Shader.PropertyToID("_ScWaterOn"), Grid = Shader.PropertyToID("_ScGrid"),
            WaterTime = Shader.PropertyToID("_ScWaterTime"), LandOn = Shader.PropertyToID("_ScLandOn"),
            SeaTexture = Shader.PropertyToID("_ScSeaTexture"), SeaCalm = Shader.PropertyToID("_ScSeaCalm"),
            SeaFoam = Shader.PropertyToID("_ScSeaFoam"), DuneRect = Shader.PropertyToID("_ScDuneRect"),
            DuneRamp = Shader.PropertyToID("_ScDuneRamp"), DuneCount = Shader.PropertyToID("_ScDuneCount");

        /// <summary>Pins the water's clock (waves, surf) to the given seconds, or back to Unity's time with null: for tests and
        /// frame captures that must render chosen moments (promo-sea review I2, M5).</summary>
        public static void PinWaterTime(float? seconds) =>
            Shader.SetGlobalVector(WaterTime, seconds.HasValue ? new Vector4(seconds.Value, 1f, 0f, 0f) : Vector4.zero);

        /// <summary>A warm, dusty haze over land (sRGB): warmer than the ground it covers (R - B 0.26), so the far land reads
        /// as dust, not as a cool milky veil (Task 6 review I1).</summary>
        public static readonly Color HazeColour = new Color(0.84f, 0.74f, 0.58f);
        public const float HazeMax = 0.45f;
        /// <summary>A cooler, weaker haze over water (review I2), so the sea stays the promo's saturated blue towards the
        /// horizon: 0.4 of the land's strength. Issue #709: a deeper blue (was 0.62, 0.74, 0.86), so diorama02's sea reads
        /// as blue as promo 02's: per-material warmth R - B -0.235, was -0.197 (promo -0.260, band [-0.312, -0.208]). The
        /// water haze starts at 120 km (ScWaterHazeLeadM), so the gameplay pose's water (48-91 km) does not change.</summary>
        public static readonly Color WaterHazeColour = new Color(0.42f, 0.62f, 0.88f);
        public const float WaterHazeMax = HazeMax * 0.4f;
        /// <summary>The aerial haze is 1 - exp(-max(d - start, 0) / scale), times HazeMax: none within 200 km (the gameplay
        /// pose, 48-91 km, has none: its depth cue is the tilt-shift), about 2% at the country-wide pose's nearest land
        /// (about 205 km) and 17% at its horizon (about 400 km): a depth cue, not a veil (review I1).</summary>
        public const float HazeStartM = 200000f, HazeDistanceM = 250000f;
        /// <summary>Real elevations between which the haze thickens, low in the Jordan rift.</summary>
        public const float RiftHazeTopM = -100f, RiftHazeFullM = -400f;

        /// <summary>Hex line opacity on land and water, and width in pixels (box-filtered, the same at every zoom). Light,
        /// faint lines as in the promo: water 0.10 (Task 8 review I2: 0.35 gave sea lines 2.6x their background's luma
        /// against the promo's 1.4x). Land 0.35, not 0.25 (merge-point carry, Task 8 N2): after the grade the light land
        /// lines vanished on the dry-grass plain at both promo poses, where promo 02 and 03 show a faint land lattice.</summary>
        public const float GridLandOpacity = 0.35f, GridWaterOpacity = 0.10f, GridWidthPx = 1.5f;

        static Vector4 haze, hazeWater;

        public static void Apply(Terrain terrain, HeightmapMeta meta, float hexSizeM, Texture2D biome, MapRuntimeAssets assets)
        {
            var size = terrain.terrainData.size; var o = terrain.transform.position;
            float Y(float elevM) => o.y + (elevM - meta.elev_min_m) * MapBuilder.VerticalExaggeration;
            Shader.SetGlobalVector(Map, new Vector4(size.x, size.z, Y(0f), MapBuilder.VerticalExaggeration));
            Shader.SetGlobalVector(MapOrigin, new Vector4(o.x, o.y, o.z, hexSizeM));
            Shader.SetGlobalTexture(Biome, biome);
            var c = HazeColour.linear;
            haze = new Vector4(c.r, c.g, c.b, HazeMax);
            var w = WaterHazeColour.linear;
            hazeWater = new Vector4(w.r, w.g, w.b, WaterHazeMax);
            Shader.SetGlobalVector(Haze, haze);
            Shader.SetGlobalVector(HazeWater, hazeWater);
            Shader.SetGlobalVector(HazeRange, new Vector4(Y(RiftHazeTopM), Y(RiftHazeFullM), HazeDistanceM, HazeStartM));
            Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, 1f));
            assets.OnTeardown(VisualToggles.Register("grid", on => Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, on ? 1f : 0f))));
            // Land use (issue #33, StatecraftLand.hlsl): fields, farm tracks, terraces and scrub; off = the bare biome ground.
            Shader.SetGlobalFloat(LandOn, 1f);
            assets.OnTeardown(VisualToggles.Register("land", on => Shader.SetGlobalFloat(LandOn, on ? 1f : 0f)));
            Shader.SetGlobalFloat(WaterOn, 1f);
            assets.OnTeardown(VisualToggles.Register("water", on => Shader.SetGlobalFloat(WaterOn, on ? 1f : 0f)));
            // The far sea's texture (sea round 2, #388: StatecraftMap.hlsl's mottle, ripple and whitecaps); its own toggle, so
            // the profile prices it and the seam test can pin it off.
            Shader.SetGlobalFloat(SeaTexture, 1f);
            assets.OnTeardown(VisualToggles.Register("seatexture", on => Shader.SetGlobalFloat(SeaTexture, on ? 1f : 0f)));
            // Sea boats (#388, StatecraftMap.hlsl): the far sea's calm and broad swells at the gameplay zoom, and the coastal
            // shelf at the overview; its own toggle, so the profile prices it and the tests can compare with it off.
            Shader.SetGlobalFloat(SeaCalm, 1f);
            assets.OnTeardown(VisualToggles.Register("seacalm", on => Shader.SetGlobalFloat(SeaCalm, on ? 1f : 0f)));
            // Sea round 3 (#388, StatecraftMap.hlsl): the surf's lace net; its own toggle, so the profile prices it.
            Shader.SetGlobalFloat(SeaFoam, 1f);
            assets.OnTeardown(VisualToggles.Register("foam", on => Shader.SetGlobalFloat(SeaFoam, on ? 1f : 0f)));
            // The dune coasts (#690, DuneCoasts.json): the water pass widens the surf there, in map metres.
            var duneRect = new Vector4[DuneCoasts.MaxBands]; var duneRamp = new Vector4[DuneCoasts.MaxBands];
            int dunes = DuneCoasts.ToMapMetres(new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z), duneRect, duneRamp);
            Shader.SetGlobalVectorArray(DuneRect, duneRect);
            Shader.SetGlobalVectorArray(DuneRamp, duneRamp);
            Shader.SetGlobalFloat(DuneCount, dunes);
            assets.OnTeardown(VisualToggles.Register("haze", on =>
            {
                Shader.SetGlobalVector(Haze, on ? haze : new Vector4(haze.x, haze.y, haze.z, 0f));
                Shader.SetGlobalVector(HazeWater, on ? hazeWater : new Vector4(hazeWater.x, hazeWater.y, hazeWater.z, 0f));
            }));
        }
    }

    /// <summary>The baked map textures (tools/terrain_bake.py), shared by the editor scripts and the tests.</summary>
    public static class MapTexturePaths
    {
        public const string Bake = "Assets/Textures/Map/terrain_bake.png";
        public const string Coast = "Assets/Textures/Map/coast.png";
        /// <summary>The ambient occlusion on its own, uncompressed (R8): BC1 stepped it on broad slopes (Task 6 fix round 1).</summary>
        public const string Ao = "Assets/Textures/Map/ao.png";
        /// <summary>The land (tools/land_bake.py, issue #33): rgb its linear colour, a its weight over the biome ground.</summary>
        public const string Land = "Assets/Textures/Map/landuse.png";
        /// <summary>The terrain water's mesh as heightmap samples (MapBuilder.WaterMeshBytes; Editor TerrainWaterBake), so a
        /// player does not derive it at boot (issue #195).</summary>
        public const string WaterMesh = "Assets/Textures/Map/terrain_water.bytes";
    }
}
