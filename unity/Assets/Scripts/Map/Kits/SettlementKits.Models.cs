#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Issue #659, owner ruling on #700: "Models up close only". Every building the kit places also gets one of
    /// Tal's (@pixelrenderai-hash) building models (<see cref="TownModel"/>), fitted to the same plot (<see cref="Fit"/>),
    /// standing on the plot's ground level with a stone plinth below it where the ground falls away. Far off the kit's
    /// own buildings draw, exactly as before; up close (TownModelLayer.CloseEnterM) the models draw instead, with each
    /// town's close mesh (KitMesh.ToCloseMesh: its landmarks and palms without the kit buildings, plus the plinths). The
    /// layout, the lots and the census do not change.</summary>
    public static partial class SettlementKits
    {
        /// <summary>One placed model: its town (or locality) id, the model, its world matrix (the model's mesh space to the
        /// world), and the fitted box it fills: centre, yaw (degrees, the kit's), width along the yaw and depth (m), the ground
        /// level it stands on and its height (m).</summary>
        public readonly struct ModelInstance
        {
            public readonly string Id; public readonly TownModel Model; public readonly Matrix4x4 M;
            public readonly Vector2 C; public readonly float Yaw, W, D, Ground, H;
            public ModelInstance(string id, TownModel model, Matrix4x4 m, Vector2 c, float yaw, float w, float d, float ground, float h)
            { Id = id; Model = model; M = m; C = c; Yaw = yaw; W = w; D = d; Ground = ground; H = h; }

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

        /// <summary>How far a house's model may be stretched or squashed upright from its own proportions to meet the kit's
        /// height (a one-room village house of two storeys would otherwise stand twice as tall as its model). The city blocks
        /// (Tel Aviv's four to eight storeys, #652; Jerusalem's, Haifa's and the moshava centres') take the kit's height exactly.</summary>
        public const float MinStretch = 0.75f, MaxStretch = 1.35f;

        /// <summary>A modelled building's plinth shows only where its ground falls more than this (real m, the 2 m sink
        /// included) across its footprint.</summary>
        const float PlinthRiseM = 2.5f;

        /// <summary>Fits <paramref name="model"/> to a plot: its longer side along the plot's longer side (turned a half turn
        /// when <paramref name="flip"/>), scaled to fill <paramref name="w"/> x <paramref name="d"/>, standing on
        /// <paramref name="ground"/>, <paramref name="h"/> tall (<paramref name="exactHeight"/>), or as near
        /// <paramref name="h"/> as <see cref="MinStretch"/>..<see cref="MaxStretch"/> of the model's own proportions allow.</summary>
        public static ModelInstance Fit(string id, TownModel model, Vector2 p, float yaw, float w, float d, float h, float ground, bool exactHeight, bool flip)
        {
            var size = TownModels.Size[(int)model];
            float mw = w, md = d, my = yaw + (flip ? 180f : 0f);
            if ((size.x >= size.y) != (w >= d)) { mw = d; md = w; my += 90f; }
            float kx = mw / size.x, ky = md / size.y, H = h;
            if (!exactHeight)
            {
                float natural = size.z * Mathf.Sqrt(kx * ky);
                H = Mathf.Clamp(h, MinStretch * natural, MaxStretch * natural);
            }
            // Tripo's meshes are Z-up: a quarter turn about x stands them up (mesh z to world y, mesh y to world -z), then the
            // kit's yaw (counter-clockwise in x-z, so a negative turn about Unity's y).
            var m = Matrix4x4.TRS(new Vector3(p.x, ground, p.y), Quaternion.Euler(0f, -my, 0f), new Vector3(kx, H / size.z, ky))
                * Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f));
            return new ModelInstance(id, model, m, p, yaw, w, d, ground, H);
        }

        /// <summary>Places one of <paramref name="from"/> on a plot (TownModels.Pick; a place's first house is its
        /// <see cref="TownModels.Once"/> building), with its plinth on <paramref name="km"/> where the ground falls away.</summary>
        static void PlaceModel(string id, TownModel[] from, Vector2 p, float yaw, float w, float d, float h, float bottom, float top, KitMesh km,
            float m, bool exactHeight)
        {
            var model = TownModels.Pick(from, id, p, out bool flip);
            if (places != null && places.TryGetValue(id, out var s) && TownModels.Once(s) is TownModel once && onceGiven!.Add(id)) model = once;
            AddModel(id, model, p, yaw, w, d, h, bottom, top, km, m, exactHeight, flip);
        }

        static void AddModel(string id, TownModel model, Vector2 p, float yaw, float w, float d, float h, float bottom, float top, KitMesh km,
            float m, bool exactHeight, bool flip)
        {
            if (models == null) throw new System.InvalidOperationException("a town model was placed outside SettlementKits.Build");
            models.Add(Fit(id, model, p, yaw, w, d, h, top, exactHeight, flip));
            if (top - bottom > PlinthRiseM * m) km.Plinths.Box(p.x, p.y, yaw, w, d, bottom, top, 0f, TerraceStone, TerraceStone, Facade.Plain, m, roofed: true);
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
