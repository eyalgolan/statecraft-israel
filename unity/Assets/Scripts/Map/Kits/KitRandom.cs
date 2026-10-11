#nullable enable

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The kits' own deterministic random stream: SplitMix64 seeded by FNV-1a of a string key (a settlement id, a
    /// grove name) and a salt, so the same map draws the same buildings and trees on every load of the same build. Float
    /// maths (Perlin noise, trigonometry) may differ between the WebGL and native players and move a tree or a house past a
    /// threshold: cosmetic only. Presentation only: it shares no state with the sim's <c>Statecraft.Sim.Rng</c>
    /// (GameState.RngState), so no hash can change.</summary>
    public struct KitRandom
    {
        ulong state;

        public KitRandom(string key, ulong salt = 0) { state = Fnv1a(key) ^ (salt * 0x9E3779B97F4A7C15UL); }
        public KitRandom(ulong seed) { state = seed; }

        public static ulong Fnv1a(string s)
        {
            ulong h = 0xCBF29CE484222325UL;
            foreach (char c in s) { h ^= c; h *= 0x100000001B3UL; }
            return h;
        }

        public ulong Next()
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform in [0, 1), 24 bits.</summary>
        public float Value() => (Next() >> 40) * (1f / 16777216f);
        public float Range(float min, float max) => min + (max - min) * Value();
        /// <summary>Uniform integer in [min, max).</summary>
        public int Range(int min, int max) => min + (int)((Next() >> 33) % (ulong)(max - min));
        public bool Chance(float p) => Value() < p;
        /// <summary>A sum of two uniforms: a cheap bell shape in [min, max].</summary>
        public float Bell(float min, float max) => min + (max - min) * 0.5f * (Value() + Value());
    }
}
