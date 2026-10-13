#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>The 3D building models (Tripo Studio, Art/Settlements/Prototype; CC0 in ASSETS.yaml) on the map.
    /// GameRoot applies them on every Begin; where none is present nothing changes. A mapped settlement keeps its marker box (object, name, transform, material) and its kit town (the
    /// procedural buildings, when the kits are built) with their renderers off, and a prefab stands on the box's base under
    /// one "SettlementPrototypes" root beside "Settlements", sized to read at the starting view (FootprintM, HeightM, the
    /// ?previewscale knob); PlacementLayer dresses transit camps and housing the same way (DressPlacement) and PreviewProps
    /// adds ships, aircraft, vehicles and figures. Each prefab is optional: one that does not load keeps the kit town. No
    /// simulation state, label, toggle or SettlementPlacer value is touched. A label hangs above its kit's columns
    /// (MapLabelsPresenter, issue #214), which the hidden kit still provides, so a model taller than its town may stand
    /// behind its label. Known gap: the "kits" visual toggle turns every kit back on, the hidden ones too.</summary>
    public static class BuildingPreview
    {
        public const string RootName = "SettlementPrototypes", InstancePrefix = "proto-", ResourcesFolder = "SettlementPrototypes";
        const string Hall = "kibbutz-dining-hall-v1", Compound = "kibbutz-compound-v2", WhiteCity = "tel-aviv-white-city-v1",
            StoneVillage = "stone-village-v1", Jerusalem = "jerusalem-stone-city-v1", Haifa = "haifa-port-city-v1",
            Moshava = "moshava-town-v1", Nahalal = "nahalal-moshav-v1", Maabara = "maabara-camp-v1", Shikun = "shikun-housing-v1";

        // Each mapping is an ordered list of prefab names: the first that loads stands in, so a later name is a fallback (the
        // dining hall while the compound is absent) or another export of the same model (a Tripo density variant, "-7k").
        // By id first, then by type. White City fits only Tel Aviv. The six moshavot share one town model; netanya, tiberias
        // and safed keep their boxes. The stone village on every arab_town is a symbolic stand-in (the content has no village
        // row) pending the owner's decision on where it belongs.
        static readonly Dictionary<string, string[]> ById = new Dictionary<string, string[]>
        {
            ["tel_aviv"] = new[] { WhiteCity },
            ["jerusalem"] = new[] { Jerusalem, Jerusalem + "-7k", Jerusalem + "-3denhance" },
            ["haifa"] = new[] { Haifa },
            ["petah_tikva"] = new[] { Moshava }, ["rishon_lezion"] = new[] { Moshava }, ["zikhron_yaakov"] = new[] { Moshava },
            ["rosh_pinna"] = new[] { Moshava }, ["rehovot"] = new[] { Moshava }, ["hadera"] = new[] { Moshava },
        };
        static readonly Dictionary<string, string[]> ByType = new Dictionary<string, string[]>
        {
            ["kibbutz"] = new[] { Compound, Hall }, ["moshav"] = new[] { Nahalal }, ["arab_town"] = new[] { StoneVillage },
        };
        /// <summary>PlacementLayer's actions: a ma'abara for a transit camp, a shikun block for housing.</summary>
        static readonly Dictionary<string, string[]> ByAction = new Dictionary<string, string[]>
        {
            [MapActions.TransitCamp] = new[] { Maabara }, [MapActions.Housing] = new[] { Shikun },
        };

        /// <summary>Every candidate list a settlement, a placement or a prop (PreviewProps.All) can resolve, once each, in table
        /// order: a row is one mapping, its fallbacks after it (BuildingPreviewCapture reports a row only when none of it
        /// loads). Built on each call, not by the type initialiser, which PlacementLayer's default path runs.</summary>
        public static IReadOnlyList<IReadOnlyList<string>> Rows() => ById.Values.Concat(ByType.Values).Concat(ByAction.Values)
            .Concat<IReadOnlyList<string>>(PreviewProps.All.Select(p => p.Names)).GroupBy(r => string.Join("\n", r)).Select(g => g.First()).ToArray();

        /// <summary>Every prefab name the loader can be asked for (Rows, flattened), in table order. PrototypeImport imports
        /// whatever folders it finds; these are the names the map uses. Built on first use, not by the type initialiser (Rows).</summary>
        public static IReadOnlyList<string> Prototypes => prototypes ??= Rows().SelectMany(r => r).Distinct().ToArray();
        static IReadOnlyList<string>? prototypes;

        // Turn about the vertical (degrees): constant per prototype, so every launch and capture draws the same map (no
        // Random, no GetHashCode). Off the poses' yaw 10 so two facades show. Values are tuned from renders
        // (BuildingPreviewCapture, its turntable gives the value directly): the hall and White City were turned half round
        // after the first editor render showed their backs to the camera, which looks north from the south (veranda,
        // balconies). A name without a row, a density variant among them, takes DefaultYaw.
        static readonly Dictionary<string, float> Yaw = new Dictionary<string, float>
        {
            [Hall] = 210f, [WhiteCity] = 200f, [StoneVillage] = 40f,
            [Compound] = DefaultYaw, [Jerusalem] = DefaultYaw, [Haifa] = DefaultYaw, [Moshava] = DefaultYaw, [Nahalal] = DefaultYaw,
            [Maabara] = DefaultYaw, [Shikun] = DefaultYaw,
        };

        /// <summary>The first batch's fronts faced the camera after about a half turn (White City 200, the hall 210), so a model
        /// without a row of its own starts there until its renders say otherwise.</summary>
        public const float DefaultYaw = 200f;

        /// <summary>A placement's turn steps this far either way of its prototype's yaw, by (q, r), so neighbouring camps differ
        /// (PlacementTurn).</summary>
        public const float PlacementTurnStepDeg = 12f;

        /// <summary>How far a prototype's footprint may spread, in marker edges. A cluster of separate buildings inside the
        /// box's own footprint read smaller than the solid box it replaced at the start view (first WebGL capture, 10 October
        /// 2026), so it may cover twice the box's width; its height stays within the box.</summary>
        public const float Spread = 2f;

        /// <summary>How much larger than its old marker box a prototype stands (Tal, 13 October 2026: big enough to see well at
        /// the 120 km starting view, but not too big): its footprint up to BuildingScale x Spread x the box's edge and its
        /// height up to BuildingScale x the edge, never wider than the kit town it replaces (FootprintM). Placements alike.</summary>
        public const float BuildingScale = 2f;

        /// <summary>The ?previewscale=N knob's range, in percent (clamped, as upstream's ?kitscale is).</summary>
        public const int MinScalePercent = 25, MaxScalePercent = 400;

        /// <summary>The size knob: every prototype and prop is drawn this many times its size. ?previewscale=N (N percent) in the
        /// WebGL player, 1 elsewhere and whenever the preview is off. Set by Apply; DressPlacement and PreviewProps read it.</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>The knob a page URL asks for: ?previewscale=N as a factor (N / 100, clamped), or 1 when absent.</summary>
        public static float RequestedScale(string? url)
        {
            int percent = UrlFlags.Int(url, "previewscale");
            return percent <= 0 ? 1f : Mathf.Clamp(percent, MinScalePercent, MaxScalePercent) / 100f;
        }

        /// <summary>A settlement prototype's largest footprint edge (m): BuildingScale x Spread x its box's edge, but no more than
        /// the square that fits inside the disc of its kit town at any turn (edge at most the square root of 2 times the
        /// disc's radius; MapScene.KitFootprints, when the kits are built), so the model stands where that town stood; times
        /// <paramref name="scale"/>.</summary>
        public static float FootprintM(MapScene map, Settlement s, float scale = 1f)
        {
            float edge = BuildingScale * Spread * SettlementPlacer.MarkerHeightM(s.Type);
            foreach (var (_, radius, id) in map.KitFootprints)
                if (id == s.Id) { edge = Mathf.Min(edge, Mathf.Sqrt(2f) * radius); break; }
            return edge * scale;
        }

        /// <summary>A settlement prototype's greatest height (m): BuildingScale x its box's edge, times <paramref name="scale"/>.</summary>
        public static float HeightM(Settlement s, float scale = 1f) => BuildingScale * SettlementPlacer.MarkerHeightM(s.Type) * scale;

        // The renderers the last enabled Apply turned off (a marker box, a kit town), turned on again by the next Apply. Only a
        // renderer that was on is recorded, so a box something else hid (the kits hide every box) is never shown by mistake.
        static readonly List<Renderer> hidden = new List<Renderer>();

        // The map and loader of the last enabled Apply: PlacementLayer reads them through Active and DressPlacement (its
        // Render has no loader to pass). Apply(false) clears them, on a destroyed map too; a destroyed map no longer matches.
        static MapScene? activeMap;
        static Func<string, GameObject?>? activeLoad;

        /// <summary>Whether the last Apply to <paramref name="map"/> turned the models on (GameRoot applies them before it draws
        /// the placements).</summary>
        public static bool Active(MapScene map) => activeLoad != null && activeMap != null && activeMap == map;

        /// <summary>The names that may stand in for <paramref name="s"/>, preferred first; empty when it keeps its box.</summary>
        public static IReadOnlyList<string> CandidatesFor(Settlement s) =>
            ById.TryGetValue(s.Id, out var byId) ? byId : ByType.TryGetValue(s.Type, out var byType) ? byType : Array.Empty<string>();

        /// <summary>The names that may dress a placement of <paramref name="action"/>, preferred first; empty for none.</summary>
        public static IReadOnlyList<string> CandidatesFor(string action) =>
            ByAction.TryGetValue(action, out var names) ? names : Array.Empty<string>();

        /// <summary>The preferred prototype for <paramref name="s"/>, or null when it keeps its box.</summary>
        public static string? PrototypeFor(Settlement s) { var names = CandidatesFor(s); return names.Count > 0 ? names[0] : null; }

        /// <summary>The first of <paramref name="names"/> that <paramref name="load"/> returns, asked in order (none after it);
        /// (null, null) when none loads.</summary>
        public static (string? name, GameObject? prefab) Resolve(IReadOnlyList<string> names, Func<string, GameObject?> load)
        {
            foreach (var name in names) { var prefab = load(name); if (prefab != null) return (name, prefab); }
            return (null, null);
        }

        /// <summary>The prototype's turn (degrees): its Yaw row, else DefaultYaw.</summary>
        public static float YawOf(string name) => Yaw.TryGetValue(name, out var yaw) ? yaw : DefaultYaw;

        /// <summary>The default loader: Resources/SettlementPrototypes/&lt;name&gt; (PrototypeImport's prefabs), null when absent.</summary>
        public static GameObject? LoadFromResources(string name) => Resources.Load<GameObject>(ResourcesFolder + "/" + name);

        /// <summary>Removes what an earlier Apply placed (turning on again every box and kit town it turned off, and PreviewProps'
        /// props); then, when <paramref name="enabled"/>, places on every mapped settlement the first of its candidates that
        /// <paramref name="load"/> returns, by Stand: at the box's base, turned by its prototype's yaw, at most FootprintM
        /// across and HeightM tall, and turns off its box and its kit town. A mapping none of whose prefabs loads, or one with
        /// nothing to draw, keeps the box and the kit. Also remembers the map and loader for DressPlacement and places the
        /// props (PreviewProps.Apply). Off with nothing placed loads nothing and touches nothing (the default path), and so
        /// does a destroyed map (a teardown may outlive it). Returns the number of settlements placed. MapScene.Build's
        /// Clear destroys both roots with the rest of the map.</summary>
        public static int Apply(MapScene map, bool enabled, Func<string, GameObject?> load) =>
            Apply(map, enabled, load, enabled && Application.platform == RuntimePlatform.WebGLPlayer ? RequestedScale(Application.absoluteURL) : 1f);

        /// <summary>Apply at the size knob <paramref name="scale"/> (Scale; tests pass it, the player reads ?previewscale).</summary>
        public static int Apply(MapScene map, bool enabled, Func<string, GameObject?> load, float scale)
        {
            if (!enabled) { activeMap = null; activeLoad = null; }   // before the guard: a teardown may outlive the map
            Scale = enabled ? scale : 1f;
            foreach (var r in hidden) if (r != null) r.enabled = true;   // a destroyed map's renderers went with it
            hidden.Clear();
            if (map == null) return 0;
            if (enabled) { activeMap = map; activeLoad = load; }
            PreviewProps.Apply(map, enabled, load);
            var markers = map.transform.Find("Settlements");   // SettlementPlacer.Place's root
            var kits = map.transform.Find(Kits.KitLayer.RootName);   // one kit town per settlement, named by its id
            var old = map.transform.Find(RootName);
            if (old != null) Discard(old.gameObject);
            if (!enabled || markers == null) return 0;
            Transform? root = null; int placed = 0;
            foreach (var s in map.Settlements)
            {
                var box = markers.Find(s.Id);
                if (box == null) continue;
                var (name, prefab) = Resolve(CandidatesFor(s), load);
                if (name == null || prefab == null) continue;
                if (root == null) { root = new GameObject(RootName).transform; root.SetParent(map.transform, true); }
                float h = SettlementPlacer.MarkerHeightM(s.Type);
                if (Stand(prefab, box.position - Vector3.up * h / 2, YawOf(name), root, InstancePrefix + s.Id, FootprintM(map, s, scale), HeightM(s, scale)) == null) continue;
                Hide(box.GetComponent<MeshRenderer>());
                if (kits != null && kits.Find(s.Id) is Transform kit) Hide(kit.GetComponent<MeshRenderer>());
                placed++;
            }
            return placed;
        }

        /// <summary>PlacementLayer's hook. While the models are on for <paramref name="map"/> and a prototype for the
        /// placement's action loads, an unscaled holder named as the box would be ("&lt;action&gt;_&lt;q&gt;_&lt;r&gt;") stands at
        /// <paramref name="at"/> under <paramref name="layer"/> and carries it, fitted like a settlement to BuildingScale x Scale
        /// x the box's edge <paramref name="edgeM"/> (Spread x that across, that high at most) and turned by its yaw plus a
        /// step from (q, r);
        /// returns true. Otherwise returns false having loaded and made nothing (always so with the preview off), and
        /// PlacementLayer draws its box as before.</summary>
        public static bool DressPlacement(Transform layer, MapScene map, Placement p, Vector3 at, float edgeM)
        {
            var load = activeLoad;
            if (load == null || !Active(map)) return false;
            var (name, prefab) = Resolve(CandidatesFor(p.Action), load);
            if (name == null || prefab == null) return false;
            var holder = new GameObject($"{p.Action}_{p.Q}_{p.R}").transform;   // PlacementLayer's box name
            holder.SetParent(layer, false);
            holder.position = at;
            float edge = BuildingScale * edgeM * Scale;
            if (Stand(prefab, at, YawOf(name) + PlacementTurn(p.Q, p.R), holder, InstancePrefix + p.Action, Spread * edge, edge) != null) return true;
            Discard(holder.gameObject);
            return false;
        }

        /// <summary>-1, 0 or +1 steps by (q, r): arithmetic, so the same hex always turns the same way. (q - r) mod 3 colours
        /// the axial grid in three, and each of the six neighbour offsets changes q - r by 1 or 2, so every neighbour of a
        /// hex turns differently from it.</summary>
        internal static float PlacementTurn(int q, int r) => PlacementTurnStepDeg * (((q - r) % 3 + 3) % 3 - 1);

        /// <summary>An instance of <paramref name="prefab"/> named <paramref name="name"/> under <paramref name="parent"/>, turned
        /// by <paramref name="yaw"/> about the vertical and scaled uniformly so its measured footprint (after the turn) spans
        /// at most <paramref name="footprintM"/> and its height at most <paramref name="heightM"/>, with its bottom on
        /// <paramref name="at"/> and its footprint centred there; no collider, shadows on, no property block. Null, leaving
        /// nothing behind, when it has nothing to draw. Shared by the settlements, DressPlacement and PreviewProps.</summary>
        internal static GameObject? Stand(GameObject prefab, Vector3 at, float yaw, Transform parent, string name, float footprintM, float heightM)
        {
            var go = UnityEngine.Object.Instantiate(prefab, at, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = name;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Discard(c);
            var size = Measure(go)?.size ?? Vector3.zero;
            float footprint = Mathf.Max(size.x, size.z);
            if (footprint <= 0f || size.y <= 0f) { Discard(go); return null; }
            go.transform.localScale *= Mathf.Min(footprintM / footprint, heightM / size.y);
            var b = Measure(go)!.Value;   // re-measured: centre and bottom move with the scale unless the pivot is there
            go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.SetPropertyBlock(null); }
            return go;
        }

        /// <summary>World bounds of every active renderer under <paramref name="go"/>, or null when there is none.</summary>
        internal static Bounds? Measure(GameObject go)
        {
            Bounds? all = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (all is Bounds b) { b.Encapsulate(r.bounds); all = b; }
                else all = r.bounds;
            }
            return all;
        }

        static void Hide(Renderer? r)
        {
            if (r == null || !r.enabled) return;
            r.enabled = false;
            hidden.Add(r);
        }

        /// <summary>Destroys now in edit mode; in play mode unparents first (as MapScene.Clear does), since Destroy waits for
        /// the frame's end and a second Apply in the same frame must not find the old root.</summary>
        internal static void Discard(UnityEngine.Object o)
        {
            if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(o); return; }
            if (o is GameObject go) go.transform.SetParent(null);
            UnityEngine.Object.Destroy(o);
        }
    }
}
