#nullable enable
using System.Collections.Generic;
using Statecraft.Game.Map.Routes;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Issue #659, owner ruling on #700: "Models up close only". Every building the kit places also gets one of
    /// MATKOT's building models (<see cref="TownModel"/>), stood on the same plot at one world scale (<see cref="Fit"/>,
    /// <see cref="ModelScale"/>),
    /// standing on the plot's ground level with a stone plinth below it where the ground falls away. Far off the kit's
    /// own buildings draw, exactly as before; up close (TownModelLayer.CloseEnterM) the models draw instead, with each
    /// town's close mesh (KitMesh.ToCloseMesh: its landmarks and palms without the kit buildings, plus the plinths). The
    /// layout, the lots and the census do not change.</summary>
    public static partial class SettlementKits
    {
        /// <summary>One placed model: its town (or locality) id, the model, its world matrix (the model's mesh space to the
        /// world), and the box it fills: centre, yaw (degrees, the kit's), width along the yaw and depth (m), the ground
        /// level it stands on and its height (m); and the centre of the kit plot it stands on (<see cref="Plot"/>: a plot
        /// holds several models at the world scale, <see cref="PlaceModel"/>).</summary>
        public readonly struct ModelInstance
        {
            public readonly string Id; public readonly TownModel Model; public readonly Matrix4x4 M;
            public readonly Vector2 C, Plot; public readonly float Yaw, W, D, Ground, H;
            public ModelInstance(string id, TownModel model, Matrix4x4 m, Vector2 c, float yaw, float w, float d, float ground, float h)
                : this(id, model, m, c, yaw, w, d, ground, h, c) { }
            public ModelInstance(string id, TownModel model, Matrix4x4 m, Vector2 c, float yaw, float w, float d, float ground, float h, Vector2 plot)
            { Id = id; Model = model; M = m; C = c; Yaw = yaw; W = w; D = d; Ground = ground; H = h; Plot = plot; }
            /// <summary>This instance on the kit plot centred at <paramref name="plot"/>.</summary>
            public ModelInstance OnPlot(Vector2 plot) => new ModelInstance(Id, Model, M, C, Yaw, W, D, Ground, H, plot);

            /// <summary>The fitted box's eight corners (world space): the map labels keep clear of them (issue #214).</summary>
            public void Corners(List<Vector3> into)
            {
                float a = Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (int i = 0; i < 8; i++)
                {
                    float u = ((i & 1) == 0 ? -0.5f : 0.5f) * W, v = ((i & 2) == 0 ? -0.5f : 0.5f) * D;
                    into.Add(new Vector3(C.x + u * ca - v * sa, (i & 4) == 0 ? Ground : Ground + H, C.y + u * sa + v * ca));
                }
            }
        }

        [System.ThreadStatic] static List<ModelInstance>? models;
        [System.ThreadStatic] static Dictionary<string, Settlement>? places;
        [System.ThreadStatic] static HashSet<string>? onceGiven;

        /// <summary>The house the world scale is set by (m, real): the middle of the kit's default house, 7.5 to 10 m long
        /// (SettlementKits.Pick).</summary>
        public const float ReferenceHouseM = 8.5f;

        /// <summary>The one world scale of MATKOT's building models (the owner on v0.1.1, 18 October 2026: the buildings were
        /// "way bigger then the train" and Jaffa's stood over Tel Aviv's): the reference house is as long as the locomotive's
        /// car of the steam train the map runs (ServiceTrain: MATKOT's locomotive and tender at RouteTrain.Scale x
        /// ServiceTrain.ModelSize, about 816 m in ServiceTrain.Cars cars of about 204 m), so 204 / 8.5 = 24x a building's real
        /// size, for every model. Before, each model filled its kit plot, at the kits' own symbolic 60x (city blocks) and 100x
        /// (houses): a Tel Aviv block up to 1.6 km and a house 800 m, and Jaffa's 100x houses beside Tel Aviv's 60x blocks.
        /// The kits far off keep their own scale.</summary>
        public const float ModelScale = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize / ServiceTrain.Cars / ReferenceHouseM;

        /// <summary>How far a house's model may be stretched or squashed upright from its own proportions to meet the kit's
        /// height (a one-room village house of two storeys would otherwise stand twice as tall as its model). The city blocks
        /// (Tel Aviv's four to eight storeys, #652; Jerusalem's, Haifa's and the moshava centres') take the kit's height exactly.</summary>
        public const float MinStretch = 0.75f, MaxStretch = 1.35f;

        /// <summary>A modelled building's plinth shows only where its ground falls more than this (real m, the 2 m sink
        /// included) across its footprint.</summary>
        const float PlinthRiseM = 2.5f;

        /// <summary>Stands <paramref name="model"/> on a plot at the world scale: the plot's building is <paramref name="w"/> x
        /// <paramref name="d"/> x <paramref name="h"/> at the kit's scale <paramref name="kitScale"/> (its real size times
        /// that), so its real size times <see cref="ModelScale"/> is what the model stands for. The model keeps its own
        /// proportions on the ground (one scale on both axes, its longer side along the plot's longer side, as long as the
        /// building's longer side at the world scale; turned a half turn when <paramref name="flip"/>), centred on the plot,
        /// on <paramref name="ground"/>, as tall as the building at the world scale (<paramref name="exactHeight"/>: the city
        /// blocks' storeys), or as near that as <see cref="MinStretch"/>..<see cref="MaxStretch"/> of its own proportions
        /// allow. The plot keeps the space around it.</summary>
        public static ModelInstance Fit(string id, TownModel model, Vector2 p, float yaw, float w, float d, float h, float ground, bool exactHeight, bool flip,
            float kitScale)
        {
            var size = TownModels.Size[(int)model];
            float toWorld = ModelScale / kitScale, bh = h * toWorld, my = yaw + (flip ? 180f : 0f);
            bool turn = (size.x >= size.y) != (w >= d);
            if (turn) my += 90f;
            float k = Mathf.Max(w, d) * toWorld / Mathf.Max(size.x, size.y);
            float natural = size.z * k;
            float H = exactHeight ? bh : Mathf.Clamp(bh, MinStretch * natural, MaxStretch * natural);
            // Tripo's meshes are Z-up: a quarter turn about x stands them up (mesh z to world y, mesh y to world -z), then the
            // kit's yaw (counter-clockwise in x-z, so a negative turn about Unity's y).
            var m = Matrix4x4.TRS(new Vector3(p.x, ground, p.y), Quaternion.Euler(0f, -my, 0f), new Vector3(k, H / size.z, k))
                * Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f));
            return new ModelInstance(id, model, m, p, yaw, (turn ? size.y : size.x) * k, (turn ? size.x : size.y) * k, ground, H);
        }

        /// <summary>Places one of <paramref name="from"/> on a plot (TownModels.Pick; a place's first house is its
        /// <see cref="TownModels.Once"/> building), with its plinth on <paramref name="km"/> where the ground falls away.</summary>
        /// <summary>The gap between the models a plot holds, as a share of a model's narrower side.</summary>
        public const float PlotGapShare = 0.25f;
        /// <summary>The most models a plot holds along either of its sides.</summary>
        public const int MaxPerSide = 4;

        /// <summary>Fills a house's or block's plot with models at the world scale (<see cref="ModelScale"/>): the plot is the
        /// kit's building at the kit's symbolic scale, several times the building at the world scale, so it holds a grid of
        /// them (as many as fit along each side with <see cref="PlotGapShare"/> between them, at most
        /// <see cref="MaxPerSide"/> a side), each one of <paramref name="from"/> (TownModels.Pick at its own spot), turned
        /// with the plot and centred on it, on the plot's ground level. A place's first house is its
        /// <see cref="TownModels.Once"/> building, alone on its plot. One plinth stands under the group where the ground falls
        /// away.</summary>
        static void PlaceModel(string id, TownModel[] from, Vector2 p, float yaw, float w, float d, float h, float bottom, float top, KitMesh km,
            float m, bool exactHeight)
        {
            if (models == null) throw new System.InvalidOperationException("a town model was placed outside SettlementKits.Build");
            var first = TownModels.Pick(from, id, p, out bool flip);
            bool once = places != null && places.TryGetValue(id, out var s) && TownModels.Once(s) is TownModel o && onceGiven!.Add(id) && (first = o) == o;
            // The grid's cell holds the widest and deepest of the models the plot may take, so none reaches past its cell.
            float cw = 0f, cd = 0f;
            foreach (var cand in once ? new[] { first } : from)
                foreach (bool f in new[] { false, true })
                {
                    var probe = Fit(id, cand, p, yaw, w, d, h, top, exactHeight, f, m);
                    cw = Mathf.Max(cw, probe.W); cd = Mathf.Max(cd, probe.D);
                }
            float gap = PlotGapShare * Mathf.Min(cw, cd), pu = cw + gap, pv = cd + gap;
            int nu = once ? 1 : Mathf.Clamp(Mathf.FloorToInt((w + gap) / pu), 1, MaxPerSide);
            int nv = once ? 1 : Mathf.Clamp(Mathf.FloorToInt((d + gap) / pv), 1, MaxPerSide);
            float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    float u = (i - 0.5f * (nu - 1)) * pu, v = (j - 0.5f * (nv - 1)) * pv;
                    var q = p + new Vector2(u * ca - v * sa, u * sa + v * ca);
                    var model = i == 0 && j == 0 ? first : TownModels.Pick(from, id, q, out flip);
                    models.Add(Fit(id, model, q, yaw, w, d, h, top, exactHeight, flip, m).OnPlot(p));
                }
            if (top - bottom > PlinthRiseM * m)
                km.Plinths.Box(p.x, p.y, yaw, nu * pu - gap, nv * pv - gap, bottom, top, 0f, TerraceStone, TerraceStone, Facade.Plain, m, roofed: true);
        }

        static void AddModel(string id, TownModel model, Vector2 p, float yaw, float w, float d, float h, float bottom, float top, KitMesh km,
            float m, bool exactHeight, bool flip)
        {
            if (models == null) throw new System.InvalidOperationException("a town model was placed outside SettlementKits.Build");
            var mi = Fit(id, model, p, yaw, w, d, h, top, exactHeight, flip, m);
            models.Add(mi);
            // The plinth under the model itself, not the whole plot.
            if (top - bottom > PlinthRiseM * m) km.Plinths.Box(p.x, p.y, yaw, mi.W, mi.D, bottom, top, 0f, TerraceStone, TerraceStone, Facade.Plain, m, roofed: true);
        }

        /// <summary>A town plot's models by its style and its place (issue #659's table).</summary>
        static TownModel[] ModelsFor(Style st, string id, Spec b)
        {
            var s = places![id];
            switch (st)
            {
                case Style.Kibbutz: return TownModels.ForKibbutz(b.part);
                case Style.TelAviv: return TownModels.TelAviv;
                case Style.WhiteBlock: return id == "haifa" ? TownModels.Haifa : TownModels.WhiteCity;
                case Style.TownBlock: return TownModels.Netanya;
                case Style.StoneBlock: return TownModels.Jerusalem;
                case Style.OldHouse: return TownModels.ForArchetype(Archetype.OldTown, s);
                case Style.Village: return TownModels.Stone;
                case Style.Templer: return TownModels.Templer;
                default: return TownModels.Houses(s);
            }
        }

        /// <summary>Whether a style's buildings take the kit's height exactly: the city blocks (60x), by their storeys.</summary>
        static bool ExactHeight(Style st) => st == Style.TelAviv || st == Style.WhiteBlock || st == Style.TownBlock || st == Style.StoneBlock;

        /// <summary>The height a style's model fills: the walls, plus the gable for the red-roofed houses (their kit roof's
        /// rise, Emit).</summary>
        static float ModelHeight(Style st, Spec b) => st switch
        {
            Style.Moshava => b.h + Mathf.Min(b.w, b.d) * 0.42f,
            Style.Kibbutz => b.h + Mathf.Min(b.w, b.d) * KibbutzGable(b.part),
            Style.Templer => b.h + Mathf.Min(b.w, b.d) * 0.55f,
            _ => b.h,
        };

        /// <summary>A kibbutz building's gable rise over its narrow side (SettlementKits.KibbutzBuilding: a shed's low roof).</summary>
        static float KibbutzGable(KibbutzPart part) => part == KibbutzPart.Shed ? 0.3f : 0.42f;
    }
}
