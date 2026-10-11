namespace Statecraft.Game.Map.Units
{
    /// <summary>Where the unit materials live: in Resources, so the showcase loads them in players without Shader.Find
    /// (M0 correction 1) and without a new serialized field on MapScene. Made by the editor's UnitMaterialAssets.Create.</summary>
    public static class UnitMaterials
    {
        public const string Folder = "Assets/Resources/Units";
        /// <summary>Matte URP Lit; the showcase sets its palette texture on a runtime copy.</summary>
        public const string MiniatureResource = "Units/UnitMiniature";
        /// <summary>The selection ring: the hex selection's own shader (unlit ivory, depth offset towards the camera).</summary>
        public const string RingResource = "Units/UnitRing";
    }
}
