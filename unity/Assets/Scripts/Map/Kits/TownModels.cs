#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The building models the towns are built from (issue #659): MATKOT's Tripo models from #507,
    /// imported in #675 under Assets/Art/Settlements/Prototype/Models. Only the models a town kit places are listed, so only
    /// these enter a build (through <see cref="TownModelSet"/>, referenced by the map scene: they load with the map). b11
    /// shikun, b21 workshop, b22 factory, b12 Tegart fort and b18 barracks have no plot in today's 1948 town kits and are not
    /// listed.</summary>
    public enum TownModel
    {
        B01WhiteCity, B02aCornerBlock, B02bNarrowBlock,
        B03aArches, B03bExternalStair, B03cPlain,
        B04aStoneBalconies, B04bLoadingDoors,
        B05aMoshavaHouse, B07DiningHall, B09aCommunal, B13aTwoStorey,
        B16Farmhouse, B17CourtyardHouse, B23Winery, B24Barn, B28WaterTower, B32CargoShed,
    }

    /// <summary>Which models a plot may take (issue #659's table), by the kit's plot style or archetype and the town.</summary>
    public static class TownModels
    {
        /// <summary>The asset folder of each model, by <see cref="TownModel"/>.</summary>
        public static readonly string[] Ids =
        {
            "b01-white-city-v2", "b02a-tel-aviv-corner-block-v2", "b02b-tel-aviv-narrow-block-v2",
            "b03a-jerusalem-arches-v2", "b03b-jerusalem-external-stair-v2", "b03c-jerusalem-plain-v2",
            "b04a-haifa-stone-balconies-v2", "b04b-haifa-loading-doors-v2",
            "b05a-moshava-house-v2", "b07-kibbutz-dining-hall-v2", "b09a-kibbutz-communal-building-v2", "b13a-netanya-two-storey-v2",
            "b16-farmhouse-v2", "b17-stone-courtyard-house-v2", "b23-winery-v2", "b24-agricultural-barn-v2", "b28-water-tower-v2",
            "b32-port-cargo-shed-v2",
        };

        /// <summary>Each model's size in its own mesh space (x wide, y deep, z up; Tripo's models are Z-up, about one unit
        /// across, centred on x and y, standing on z = 0), by <see cref="TownModel"/>; BuildingModelImportTests checks
        /// these against the imported meshes. The kit fits each model to its plot from these.</summary>
        public static readonly Vector3[] Size =
        {
            new Vector3(1.00f, 0.95f, 0.94f), new Vector3(0.92f, 0.79f, 1.00f), new Vector3(0.88f, 0.63f, 1.00f),
            new Vector3(0.94f, 1.00f, 0.95f), new Vector3(1.00f, 0.89f, 0.91f), new Vector3(0.95f, 1.00f, 0.98f),
            new Vector3(0.92f, 0.97f, 1.00f), new Vector3(0.94f, 1.00f, 0.86f),
            new Vector3(1.00f, 0.97f, 0.88f), new Vector3(1.00f, 0.63f, 0.34f), new Vector3(0.87f, 1.00f, 0.65f), new Vector3(0.89f, 1.00f, 0.96f),
            new Vector3(0.95f, 0.90f, 0.57f), new Vector3(0.86f, 1.00f, 0.55f), new Vector3(0.77f, 1.00f, 0.52f), new Vector3(0.70f, 1.00f, 0.41f),
            new Vector3(0.53f, 0.55f, 1.00f), new Vector3(1.00f, 0.50f, 0.38f),
        };

        public const string Folder = "Assets/Art/Settlements/Prototype/Models";
        /// <summary>The <see cref="TownModelSet"/> the map scene references.</summary>
        public const string SetPath = "Assets/Art/Settlements/TownModels.asset";

        internal static readonly TownModel[] TelAviv = { TownModel.B01WhiteCity, TownModel.B01WhiteCity, TownModel.B02aCornerBlock, TownModel.B02bNarrowBlock };
        internal static readonly TownModel[] WhiteCity = { TownModel.B01WhiteCity, TownModel.B02aCornerBlock };
        internal static readonly TownModel[] Haifa = { TownModel.B04aStoneBalconies, TownModel.B04bLoadingDoors };
        internal static readonly TownModel[] Jerusalem = { TownModel.B03aArches, TownModel.B03bExternalStair, TownModel.B03cPlain };
        internal static readonly TownModel[] Stone = { TownModel.B17CourtyardHouse };
        internal static readonly TownModel[] Netanya = { TownModel.B13aTwoStorey };
        /// <summary>A moshava's or moshav's houses: farmhouses and houses, a barn in about one plot in five.</summary>
        internal static readonly TownModel[] Moshava =
        {
            TownModel.B05aMoshavaHouse, TownModel.B05aMoshavaHouse, TownModel.B16Farmhouse, TownModel.B16Farmhouse, TownModel.B24Barn,
        };
        /// <summary>A kibbutz's members' houses (#820's row houses).</summary>
        internal static readonly TownModel[] Kibbutz = { TownModel.B09aCommunal };
        /// <summary>A kibbutz's farm sheds (#820's farm yard): the agricultural barn.</summary>
        internal static readonly TownModel[] FarmShed = { TownModel.B24Barn };
        /// <summary>Haifa's German Colony: the Templers' stone houses with red roofs.</summary>
        internal static readonly TownModel[] Templer = { TownModel.B05aMoshavaHouse };

        /// <summary>The wine moshavot (the Carmel wineries of Rishon LeZion, 1890, and Zikhron Ya'akov, 1892): their first
        /// house nearest the heart is the winery.</summary>
        public static bool WineMoshava(string id) => id == "rishon_lezion" || id == "zikhron_yaakov";

        /// <summary>A kibbutz plot's models by what stands on it (#820): a farm shed is a barn, a house a member's house.</summary>
        internal static TownModel[] ForKibbutz(KibbutzPart part) => part == KibbutzPart.Shed ? FarmShed : Kibbutz;

        /// <summary>A house by archetype (a 1948 locality's, or a small town's fallback house: #317).</summary>
        public static TownModel[] ForArchetype(Archetype a, Settlement s) => a switch
        {
            Archetype.WhiteCity => WhiteCity,
            Archetype.OldTown => s.Id == "jerusalem" ? Jerusalem : s.Id == "haifa" ? Haifa : Stone,
            Archetype.ArabTown => Stone,
            _ => Houses(s),
        };

        /// <summary>A Jewish place's red-roofed houses: Netanya's two-storey houses, a kibbutz's communal buildings, a
        /// moshava's or moshav's houses, farmhouses and barns.</summary>
        internal static TownModel[] Houses(Settlement s) => s.Id == "netanya" ? Netanya : s.Type == "kibbutz" ? Kibbutz : Moshava;

        /// <summary>The one building a place has once, given to its first house (the one nearest its heart): a wine
        /// moshava's winery; else null. A kibbutz's b07 dining hall stands on its own dining hall (#820's landmark,
        /// SettlementKits.DiningHall), not on a house.</summary>
        public static TownModel? Once(Settlement s) => WineMoshava(s.Id) ? TownModel.B23Winery : (TownModel?)null;

        /// <summary>One of <paramref name="from"/> for the plot at <paramref name="p"/> of town <paramref name="id"/>: a hash
        /// of the two, so the choice draws nothing from the kit's random streams and moves no plot.</summary>
        public static TownModel Pick(TownModel[] from, string id, Vector2 p, out bool flip)
        {
            ulong h = KitRandom.Fnv1a(id) ^ ((ulong)(uint)Mathf.RoundToInt(p.x) * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)Mathf.RoundToInt(p.y) * 0xC2B2AE3D27D4EB4FUL);
            var r = new KitRandom(h);
            ulong v = r.Next();
            flip = (v & 1UL) != 0;
            return from[(int)((v >> 1) % (ulong)from.Length)];
        }
    }
}
