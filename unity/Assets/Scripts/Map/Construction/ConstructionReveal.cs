#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Map.Construction
{
    public enum RevealStyle { Perimeter, Staggered, Cloud }
    public enum ConstructionLook { Original, Stones }

    /// <summary>Opt-in presentation only. Bind a mesh-only visual child, never the authoritative pick root.
    /// No callbacks into simulation, global random state, coroutines or per-frame allocations.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ConstructionReveal : MonoBehaviour
    {
        [Range(0.6f, 4f)] public float Duration = 1.8f;
        [Range(0f, 1.5f)] public float Intensity = 1f;
        [Range(0.1f, 3f)] public float Speed = 1f;
        public int Seed = 1948;
        public RevealStyle Style;
        public ConstructionLook Look = ConstructionLook.Stones;
        [Min(0f)] public float GroundOffset;
        public const int DustBudget = 36, DebrisBudget = 8;
        public bool IsPlaying { get; private set; }
        public float Elapsed { get; private set; }
        public float FootprintRadius { get; private set; }
        Transform? visual;
        Vector3 originalPosition, originalScale, localRise;
        GameObject? particles;
        ParticleSystem? dust, debris;
        Material? material, stoneMaterial;
        Mesh? stoneMesh;
        ParticleSystemRenderer? debrisRenderer;
        ConstructionLook configuredLook;
        bool lookConfigured;
        readonly ParticleSystem.Particle[] puffs = new ParticleSystem.Particle[DustBudget];
        readonly ParticleSystem.Particle[] flecks = new ParticleSystem.Particle[DebrisBudget];
        bool bound;

        /// <summary>radius is a conservative circle about the visual base, in world units. Keep at least
        /// 2.25 radius inside the tile. Call before Play; rebinding first restores the old visual.</summary>
        public void Bind(Transform target, float radius)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target == transform || target.parent != transform) throw new ArgumentException("Bind a direct visual child of the reveal host.", nameof(target));
            if (!float.IsFinite(radius) || radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            Cancel(); visual = target; originalPosition = target.localPosition; originalScale = target.localScale;
            FootprintRadius = radius; bound = true;
            float height = radius * 1.3f;
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                height = Mathf.Max(bounds.size.y, radius * 0.1f);
            }
            localRise = transform.InverseTransformVector(Vector3.up * height);
        }

        public void Play()
        {
            if (!isActiveAndEnabled || !bound || visual == null) return;
            Cancel(); EnsureParticles(); IsPlaying = true; Elapsed = 0f; Sample(0f);
        }

        void Update()
        {
            if (Application.isPlaying && IsPlaying) Sample(Elapsed + Time.deltaTime * Mathf.Clamp(Speed, 0.1f, 3f));
        }

        /// <summary>Absolute visual time, for scrubbing and reproducible captures. Does not advance simulation.</summary>
        public void Sample(float seconds)
        {
            if (!isActiveAndEnabled || !bound || visual == null) { Cancel(); return; }
            Elapsed = float.IsFinite(seconds) ? Mathf.Max(0f, seconds) : 0f;
            float duration = float.IsFinite(Duration) ? Mathf.Clamp(Duration, 0.6f, 4f) : 1.8f;
            float t = Mathf.Clamp01(Elapsed / duration);
            if (t >= 1f) { Cancel(); Elapsed = duration; return; }
            EnsureParticles();
            // Anticipation: the intact building starts below the ground. Dust precedes its rise.
            float rise = Smooth(0.12f, Style == RevealStyle.Staggered ? 0.67f : 0.51f, t);
            float settle = Mathf.Sin(Mathf.InverseLerp(0.51f, 0.78f, t) * Mathf.PI) * (t < 0.78f ? 0.012f : 0f);
            if (Look == ConstructionLook.Stones)
            {
                // One restrained overshoot and a smaller return, never a squash of the original model.
                float u = Mathf.InverseLerp(0.51f, 0.82f, t);
                settle = t < 0.82f ? Mathf.Sin(u * Mathf.PI * 2f) * (1f - u) * 0.02f : 0f;
            }
            visual.localScale = originalScale;
            visual.localPosition = originalPosition + localRise * (rise - 1f + settle);
            particles!.transform.position = transform.TransformPoint(originalPosition) + Vector3.up * (Look == ConstructionLook.Stones && float.IsFinite(GroundOffset) ? Mathf.Max(0f, GroundOffset) : 0f);
            ConfigureLook(); DrawDust(t);
            if (Look == ConstructionLook.Original) DrawDebris(t); else DrawStones(t);
        }

        public void Cancel()
        {
            IsPlaying = false;
            if (bound && visual != null) { visual.localPosition = originalPosition; visual.localScale = originalScale; }
            if (dust != null) dust.Clear(); if (debris != null) debris.Clear();
        }

        void OnDisable() => Cancel();
        void OnDestroy()
        {
            Cancel(); Dispose(particles); Dispose(material); Dispose(stoneMaterial); Dispose(stoneMesh);
            particles = null; material = null; stoneMaterial = null; stoneMesh = null; lookConfigured = false;
        }

        void EnsureParticles()
        {
            if (particles != null && dust != null && debris != null) return;
            // A caller may remove a visual-only child while retaining the host for reuse.
            // Release the surviving partial system/material and configure the replacement renderer again.
            if (dust != null) dust.Clear(); if (debris != null) debris.Clear();
            Dispose(particles); Dispose(material);
            particles = null; dust = null; debris = null; material = null;
            debrisRenderer = null; lookConfigured = false;
            var template = Resources.Load<Material>("Construction/Dust");
            if (template == null) throw new InvalidOperationException("Construction/Dust material is missing.");
            material = new Material(template) { name = "Construction dust (owned)", hideFlags = HideFlags.DontSave };
            particles = new GameObject("Construction particles (owned)") { hideFlags = HideFlags.DontSave, layer = 2 };
            particles.transform.SetParent(transform, false);
            // World space keeps the dust at the ground while only the visual child rises.
            dust = MakeSystem("Base puffs", DustBudget); debris = MakeSystem("Earth flecks", DebrisBudget);
        }

        ParticleSystem MakeSystem(string name, int budget)
        {
            var go = new GameObject(name) { layer = 2 }; go.transform.SetParent(particles!.transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false; ps.randomSeed = 1u;
            var main = ps.main; main.simulationSpeed = 0f; main.startSpeed = 0f; main.playOnAwake = false; main.loop = false; main.maxParticles = budget;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission; emission.enabled = false; var shape = ps.shape; shape.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            ps.Play(); ps.Pause();
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }

        void DrawDust(float t)
        {
            int count = 0;
            float intensity = float.IsFinite(Intensity) ? Mathf.Clamp(Intensity, 0f, 1.5f) : 1f;
            int wanted = Mathf.RoundToInt(DustBudget * Mathf.Min(intensity, 1f));
            Vector3 centre = particles!.transform.position;
            float radius = FootprintRadius;
            for (int i = 0; i < wanted; i++)
            {
                float h = Hash(i, 1), h2 = Hash(i, 2), h3 = Hash(i, 3);
                bool completion = Look == ConstructionLook.Stones && i % 3 == 2;
                float start = completion ? 0.49f + h * 0.045f : 0.06f + h * 0.09f + (Style == RevealStyle.Staggered ? (i % 3) * 0.055f : 0f);
                float age = (t - start) / (completion ? 0.32f : 0.48f + h2 * 0.29f);
                if (age <= 0f || age >= 1f) continue;
                float angle = i * 2.399963f + h2 * 0.3f;
                float cloud = Style == RevealStyle.Cloud ? 1.16f : 1f;
                float ring = radius * (0.82f + h2 * 0.19f + age * 0.42f) * cloud;
                float size = radius * (0.35f + h3 * 0.22f) * (0.6f + age * 1.1f) * cloud;
                float opacity = Mathf.Sin(Mathf.PI * age) * (1f - Smooth(0.60f, 1f, age)) * 0.76f * Mathf.Min(intensity, 1.3f);
                var colour = Color.Lerp(new Color(0.57f, 0.41f, 0.26f), new Color(0.88f, 0.76f, 0.51f), h3);
                colour.a = opacity * (completion ? 0.62f : 1f);
                if (completion) size *= 0.68f;
                puffs[count++] = Particle(centre + new Vector3(Mathf.Cos(angle) * ring, radius * (0.07f + age * 0.19f + h3 * 0.06f), Mathf.Sin(angle) * ring), size, colour, h * 360f);
            }
            dust!.SetParticles(puffs, count);
        }

        void DrawDebris(float t)
        {
            int count = 0;
            if (Intensity > 0f)
                for (int i = 0; i < DebrisBudget; i++)
                {
                    float age = (t - 0.14f - Hash(i, 4) * 0.08f) / 0.28f;
                    if (age <= 0f || age >= 1f) continue;
                    float angle = i * 2.399963f, r = FootprintRadius * (0.65f + age * 0.63f);
                    var at = particles!.transform.position + new Vector3(Mathf.Cos(angle) * r, FootprintRadius * (0.025f + Mathf.Sin(age * Mathf.PI) * 0.24f), Mathf.Sin(angle) * r);
                    flecks[count++] = Particle(at, FootprintRadius * (0.025f + Hash(i, 6) * 0.018f), new Color(0.48f, 0.34f, 0.19f, 1f - Smooth(0.6f, 1f, age)), age * 180f);
                }
            debris!.SetParticles(flecks, count);
        }

        void ConfigureLook()
        {
            if (lookConfigured && configuredLook == Look) return;
            debrisRenderer ??= debris!.GetComponent<ParticleSystemRenderer>();
            var main = debris!.main; main.startRotation3D = Look == ConstructionLook.Stones;
            if (Look == ConstructionLook.Stones)
            {
                if (stoneMaterial == null)
                {
                    var template = Resources.Load<Material>("Construction/Stone");
                    if (template == null) throw new InvalidOperationException("Construction/Stone material is missing.");
                    stoneMaterial = new Material(template) { name = "Construction stones (owned)", hideFlags = HideFlags.DontSave };
                    stoneMesh = MakeStone();
                }
                debrisRenderer.renderMode = ParticleSystemRenderMode.Mesh; debrisRenderer.mesh = stoneMesh;
                debrisRenderer.sharedMaterial = stoneMaterial;
            }
            else { debrisRenderer.renderMode = ParticleSystemRenderMode.Billboard; debrisRenderer.sharedMaterial = material; }
            configuredLook = Look; lookConfigured = true;
        }

        void DrawStones(float t)
        {
            int count = 0;
            if (Intensity > 0f)
                for (int i = 0; i < DebrisBudget; i++)
                {
                    float age = (t - 0.10f - (i % 3) * 0.022f - Hash(i, 4) * 0.025f) / (0.55f + Hash(i, 8) * 0.08f);
                    if (age <= 0f || age >= 1f) continue;
                    float angle = i * 2.399963f + Hash(i, 9) * 0.5f;
                    float travel = 1f - (1f - age) * (1f - age);
                    float radius = FootprintRadius * (0.76f + travel * 0.77f);
                    float height;
                    if (age < 0.55f) { float u = age / 0.55f; height = 4f * u * (1f - u) * (0.32f + Hash(i, 10) * 0.1f); }
                    else if (age < 0.80f) { float u = (age - 0.55f) / 0.25f; height = 4f * u * (1f - u) * 0.10f; }
                    else if (age < 0.94f) { float u = (age - 0.80f) / 0.14f; height = 4f * u * (1f - u) * 0.025f; }
                    else height = 0f;
                    float size = FootprintRadius * (0.085f + Hash(i, 6) * 0.035f) * (1f - Smooth(0.82f, 1f, age));
                    var at = particles!.transform.position + new Vector3(Mathf.Cos(angle) * radius, FootprintRadius * (height + 0.065f), Mathf.Sin(angle) * radius);
                    var colour = Color.Lerp(new Color(0.43f, 0.32f, 0.20f), new Color(0.69f, 0.56f, 0.36f), Hash(i, 7));
                    var stone = Particle(at, size, colour, 0f);
                    stone.rotation3D = new Vector3(Hash(i, 11) * 180f + travel * 210f, Hash(i, 12) * 360f, travel * 140f);
                    flecks[count++] = stone;
                }
            debris!.SetParticles(flecks, count);
        }

        static Mesh MakeStone()
        {
            var points = new[] { new Vector3(0.1f, 0.55f, -0.07f), new Vector3(-0.12f, -0.4f, 0.06f),
                new Vector3(-0.52f, 0.05f, 0.04f), new Vector3(0.46f, -0.03f, 0.08f),
                new Vector3(0.02f, 0.03f, -0.43f), new Vector3(0.03f, 0.02f, 0.39f) };
            int[] faces = { 0, 4, 2, 0, 3, 4, 0, 5, 3, 0, 2, 5, 1, 2, 4, 1, 4, 3, 1, 3, 5, 1, 5, 2 };
            var vertices = new Vector3[faces.Length]; var indices = new int[faces.Length];
            for (int i = 0; i < faces.Length; i++) { vertices[i] = points[faces[i]]; indices[i] = i; }
            var mesh = new Mesh { name = "Construction stone (owned)", hideFlags = HideFlags.DontSave, vertices = vertices, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static ParticleSystem.Particle Particle(Vector3 position, float size, Color colour, float rotation) =>
            new ParticleSystem.Particle { position = position, startSize = size, startColor = colour, rotation = rotation, startLifetime = 10f, remainingLifetime = 10f };
        float Hash(int i, int salt)
        {
            uint h = unchecked((uint)(Seed ^ (i + 1) * 73856093 ^ salt * 19349663));
            h ^= h << 13; h ^= h >> 17; h ^= h << 5; return (h & 0xffff) / 65535f;
        }
        static float Smooth(float from, float to, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t));
        static void Dispose(Object? value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
