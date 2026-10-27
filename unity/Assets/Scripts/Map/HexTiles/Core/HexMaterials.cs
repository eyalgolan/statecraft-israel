#nullable enable
using System;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The board's material templates, shipped in Resources/HexTiles/Materials (made by the editor menu
    /// Statecraft/Hex Tiles/Make Materials) so a player build keeps their shaders and keyword variants: Shader.Find returns
    /// null in players and the URP Terrain/Lit and transparent variants would be stripped. Each call returns a copy the
    /// caller owns. In the editor, a missing template falls back to its shader by name.</summary>
    public static class HexMaterials
    {
        public const string Folder = "HexTiles/Materials";
        public const string Terrain = "HexTerrain", TerrainHoles = "HexTerrainHoles", Lit = "HexLit", Sea = "HexSea", Edges = "HexEdges";
        public const string AttackSmoke = "HexAttackSmoke", AttackGlow = "HexAttackGlow", AttackBorder = "HexAttackBorder";

        public static Material New(string name)
        {
            var template = Resources.Load<Material>(Folder + "/" + name);
            if (template != null) return new Material(template) { name = name };
            if (name is AttackSmoke or AttackGlow or AttackBorder) return FromShader(name);
            var shader = Shader.Find(name switch
            {
                Terrain or TerrainHoles => "Universal Render Pipeline/Terrain/Lit",
                Edges => "Universal Render Pipeline/Unlit",
                _ => "Universal Render Pipeline/Lit",
            }) ?? throw new InvalidOperationException($"no material Resources/{Folder}/{name} and no shader for it");
            var m = new Material(shader) { name = name };
            if (name is Sea or Edges) MakeTransparent(m);
            if (name is Terrain or TerrainHoles) m.EnableKeyword("_NORMALMAP");
            if (name == TerrainHoles) m.EnableKeyword("_ALPHATEST_ON");
            return m;
        }

        /// <summary>The under-attack look's materials (HexUnderAttack) built from their shaders, as Make Materials writes
        /// their templates: URP particle shaders, because they multiply the mesh's vertex colours (soft smoke puffs, a
        /// glow that fades to its rim). Smoke is lit and alpha-blended; the fire glow additive; the border alpha-blended.
        /// Both unlit ones draw both faces.</summary>
        public static Material FromShader(string name)
        {
            var shader = Shader.Find(name switch
            {
                AttackSmoke => "Universal Render Pipeline/Particles/Simple Lit",
                AttackGlow or AttackBorder => "Universal Render Pipeline/Particles/Unlit",
                _ => throw new ArgumentException($"no under-attack material '{name}'"),
            }) ?? throw new InvalidOperationException($"no material Resources/{Folder}/{name} and no shader for it");
            var m = new Material(shader) { name = name };
            MakeTransparent(m);
            if (name == AttackGlow)
            {
                m.SetFloat("_Blend", 2f);   // additive
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            }
            if (name is AttackGlow or AttackBorder) m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        /// <summary>URP Lit/Unlit set to alpha-blended transparency (the settings the shipped templates carry).</summary>
        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
