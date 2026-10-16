#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Tal's soldiers on the army tokens (issue #665, part of #663): The 1948 Collection's figures made by Tal
    /// (@pixelrenderai-hash) for #507, streamed after the first frame (<see cref="CollectionStream"/>), take the place of the
    /// code-built riflemen. Each token is built with the code-built figures as before; each figure then asks for its model, and
    /// when the model arrives it is put under the figure and the code-built mesh's renderer is switched off, in the same
    /// callback, so the swap is one clean change of model and nothing else moves (the figure's transform, which the camera's
    /// yaw turns (#644), its mesh, which the labels' keep-clear boxes read, and the badge and flag stay as they were). It fails
    /// open: until a model arrives, and for ever when its bundle is missing or damaged or the figure has no model in
    /// <see cref="ModelFor"/>, the code-built figure stays. A model that has arrived once is put on new figures at once (a
    /// roster change rebuilds the tokens), so a rebuild never shows the code-built figures for a frame.</summary>
    public static class UnitFigures
    {
        /// <summary>The name of the model's object under its figure.</summary>
        public const string ModelName = "CollectionModel";

        public const string SabraPioneer = "figure-c04-sabra-pioneer-v3", YoungRecruit = "figure-c06-young-recruit-v2",
            VeteranFighter = "figure-c07-veteran-fighter-v2";

        /// <summary>Which of Tal's figures stands for a code-built rifleman: by the rifleman's headgear (the sourced
        /// <see cref="UnitData.Headgear"/>, or the uniform default), as Tal paired them in #507: the bush (slouch) hat is the
        /// sabra pioneer (C04), the steel helmet the young recruit (C06) and the wool cap the veteran fighter (C07). Only the
        /// Israeli side: the Arab Legion (C08) and the irregulars (C09) wait for #226 to place those forces, and C09 for the
        /// maintainer's review; every other side keeps the code-built figures.</summary>
        public static readonly IReadOnlyDictionary<(string side, string type), string> Table = new Dictionary<(string, string), string>
        {
            [(UnitData.Israel, UnitShowcase.RiflemanSlouchHatType)] = SabraPioneer,
            [(UnitData.Israel, UnitShowcase.RiflemanHelmetType)] = YoungRecruit,
            [(UnitData.Israel, UnitShowcase.RiflemanWoolCapType)] = VeteranFighter,
        };

        /// <summary>The Collection model for a rifleman of <paramref name="type"/> on <paramref name="side"/>'s token, or null
        /// (the code-built figure stays).</summary>
        public static string? ModelFor(string side, string type) => Table.TryGetValue((side, type), out var id) ? id : null;

        /// <summary>How a model is asked for: the game's stream in a player or in play mode. In edit mode nothing is asked for
        /// (the stream's coroutine never runs there), unless a caller sets <see cref="Source"/>: the editor's captures load the
        /// prefabs at once, and the tests answer by hand.</summary>
        public static Action<string, CollectionDetail, Action<GameObject?>>? Source { get; set; }

        static Action<string, CollectionDetail, Action<GameObject?>>? Requester =>
            Source ?? (Application.isPlaying ? new Action<string, CollectionDetail, Action<GameObject?>>(CollectionStream.Request) : null);

        /// <summary>The models that have arrived, by id (a destroyed one, after its bundle was unloaded, counts as not arrived).</summary>
        static readonly Dictionary<string, GameObject> arrived = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        /// <summary>Forgets the models that have arrived (tests, and a change of <see cref="Source"/>).</summary>
        public static void Forget() => arrived.Clear();

        /// <summary>Puts model <paramref name="id"/> on <paramref name="figure"/> (a code-built rifleman, real metres scaled by
        /// UnitShowcase.FigureScale): at once when it has arrived before, else when it arrives. Nothing for a null id.</summary>
        public static void Dress(Transform figure, string? id)
        {
            if (id == null || figure == null) return;
            if (arrived.TryGetValue(id, out var have) && have != null) { Swap(figure, have); return; }
            var ask = Requester;
            if (ask == null) return;
            ask(id, CollectionDetail.Far, prefab =>
            {
                if (prefab == null) return;   // fail open: the code-built figure stays (the stream has warned once)
                arrived[id] = prefab;
                if (figure != null) Swap(figure, prefab);   // the token may have been rebuilt or destroyed meanwhile
            });
        }

        /// <summary>The swap: the model under the figure, in its frame (real metres, facing +Z, feet on the origin), and the
        /// code-built mesh's renderer off, together.</summary>
        static void Swap(Transform figure, GameObject prefab)
        {
            if (figure.Find(ModelName) != null) return;
            var model = UnityEngine.Object.Instantiate(prefab, figure, false);
            model.name = ModelName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var code = figure.GetComponent<MeshRenderer>();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (code != null) { r.shadowCastingMode = code.shadowCastingMode; r.receiveShadows = code.receiveShadows; }
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            if (code != null) code.enabled = false;
        }

        /// <summary>True when <paramref name="figure"/> shows its Collection model (and not the code-built mesh).</summary>
        public static bool Dressed(Transform figure)
        {
            var code = figure.GetComponent<MeshRenderer>();
            return figure.Find(ModelName) != null && (code == null || !code.enabled);
        }
    }
}
