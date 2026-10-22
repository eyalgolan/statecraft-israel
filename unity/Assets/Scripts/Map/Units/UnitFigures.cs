#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The 1948 Collection on the army tokens (issue #665, part of #663; models by Tal Heuberger, #507): the figures and the
    /// vehicles each token draws, streamed after the first frame (<see cref="CollectionStream"/>). Which model goes where is
    /// in the tables below (docs/units.md, "The 1948 Collection on the tokens").
    /// An Israeli token is built with its code-built riflemen as before; each figure then asks for its model, and when the
    /// model arrives it goes under the figure and the code-built renderer is switched off, in the same callback, so nothing
    /// else moves (the figure's transform, which the camera's yaw turns (#644), its mesh, which the labels' keep-clear boxes
    /// read, and the badge and flag). It fails open: until a model arrives, and for ever when its bundle is missing or
    /// damaged, the code-built figure stays. An Arab force's token has no code-built figure at all (owner, 17 October 2026,
    /// #740: "Nothing is drawn with IDF figures, and there is no placeholder art"): the roster draws it only once its model
    /// has arrived (<see cref="Arrived"/>). A vehicle has no code-built model either: it appears when it arrives. A model
    /// that has arrived once is put on new tokens at once (a roster change rebuilds them), so a rebuild never shows the
    /// code-built figures for a frame. Up close (<see cref="UnitFigureDetail"/>), each figure whose model has an idle clip
    /// shows its skinned near model playing it instead.</summary>
    public static class UnitFigures
    {
        /// <summary>The name of the model's object under its figure or vehicle.</summary>
        public const string ModelName = "CollectionModel";

        public const string SabraPioneer = "figure-c04-sabra-pioneer-v3", PioneerVeteran = "figure-c05-pioneer-veteran-v2",
            YoungRecruit = "figure-c06-young-recruit-v2", VeteranFighter = "figure-c07-veteran-fighter-v2",
            PalmachScout = "figure-c12f-palmach-scout-v2", Soldier1948 = "figure-soldier-1948-v1", ArabLegion = "figure-c08-arab-legion-v2", Irregular = "figure-c09-irregular-v2",
            SyrianInfantry = "figure-syrian-infantry-1948-v1",
            AlaInfantry = "figure-ala-infantry-1948-v1", IraqiInfantry = "figure-iraqi-infantry-1948-v1",
            Napoleonchik = "figure-vehicle-napoleonchik-65mm-1948-v2", WillysJeep = "figure-vehicle-willys-jeep-1948-v2";

        /// <summary>An Israeli rifleman's figure by his headgear (the sourced <see cref="UnitEntry.Headgear"/>, or the uniform
        /// default), as Tal Heuberger's figures wear them and #507 paired them: the bush (slouch) hat is the sabra pioneer (C04), the
        /// Mk II steel helmet the young recruit (C06), the Palmach's knitted stocking cap the veteran fighter (C07).</summary>
        public static readonly IReadOnlyDictionary<string, string> ByHeadgear = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [UnitData.SlouchHat] = SabraPioneer, [UnitData.SteelHelmet] = YoungRecruit, [UnitData.WoolCap] = VeteranFighter,
        };

        /// <summary>The third figure of an Israeli token whose headgear is the uniform default (no source ties headgear to
        /// it), in place of the default's second bush hat: the pioneer veteran (C05), bareheaded, except where a row is named
        /// here. Harel's is the Palmach scout (C12F): Harel was a Palmach brigade (its row's number, palmach.org.il) whose
        /// battalions escorted the convoys between Sha'ar HaGai, where its token stands, and Saris (its anchor, Izkor);
        /// a Palmach woman's duties included "escorting convoys" (https://en.wikipedia.org/wiki/Netiva_Ben-Yehuda, checked
        /// 2026-10-18), and the Palmach memorial site records 34 of its women killed, seventeen in action
        /// (https://en.wikipedia.org/wiki/Palmach, checked 2026-10-18).
        /// Yiftach's and Negev's is #336's 1948 soldier (owner, 21 October 2026, through the coordinator: "place it as a third
        /// figure on some brigade unit tokens, next to C06 and C07"): a rifleman in khaki wearing the knitted stocking cap, the
        /// Palmach's (as C07 wears it), so he stands third on the other two Palmach brigades, the 11th and 12th (their rows'
        /// numbers, palmach.org.il). On Negev's token he takes the third figure's sourced cap from C07
        /// (<see cref="ThirdOverHeadgear"/>), so it shows C07, C06 and the soldier.</summary>
        public static readonly IReadOnlyDictionary<string, string> ThirdFigure = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["harel"] = PalmachScout, ["yiftach"] = Soldier1948, ["negev"] = Soldier1948,
        };

        /// <summary>The headgear a sourced third figure must wear for a <see cref="ThirdFigure"/> row to stand in its place: the
        /// soldier's knitted stocking cap, so a row never changes what its headgear source shows.</summary>
        public static readonly IReadOnlyDictionary<string, string> ThirdOverHeadgear = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Soldier1948] = UnitData.WoolCap,
        };

        /// <summary>An Arab force's own figure, by side. The generated 1948 infantry have separate Syrian, ALA and
        /// Iraqi identities; C08 stays with Jordan and C09 with local irregulars. A mapping supplies art only: no local-force
        /// row is authored here. Egypt awaits an approved representation of its simultaneous bases, and Lebanon's hidden
        /// row remains off the map (<see cref="UnitRoster.OnMap"/>).
        /// Unit models by Tal Heuberger count as reviewed and approved (owner, 17 October 2026, #756, Q-ART-3).</summary>
        public static readonly IReadOnlyDictionary<string, string> BySide = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["jordan"] = ArabLegion, ["local_arab_forces"] = Irregular,
            ["syria"] = SyrianInfantry, ["ala"] = AlaInfantry, ["iraq"] = IraqiInfantry,
        };

        /// <summary>A vehicle's model by its type (<see cref="UnitVehicle"/>): the 65 mm gun is H02, the "Napoleonchik"
        /// (owner, 17 October 2026, #756: Tal Heuberger's H02 is the gun), the jeep H03, the Willys.</summary>
        public static readonly IReadOnlyDictionary<string, string> ByVehicle = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [UnitData.Gun65mm] = Napoleonchik, [UnitData.Jeep] = WillysJeep,
        };

        /// <summary>The figure of an Arab force's side, or null (that side's rows stay off the map).</summary>
        public static string? SideFigure(string side) => BySide.TryGetValue(side, out var id) ? id : null;

        /// <summary>The model for figure <paramref name="index"/> (0 to 2) of <paramref name="u"/>'s token, wearing
        /// <paramref name="headgear"/> on an Israeli token (ignored on another side's).</summary>
        public static string? FigureFor(UnitEntry u, int index, string? headgear)
        {
            if (u.Side != UnitData.Israel) return SideFigure(u.Side);
            if (index == 2 && ThirdFigure.TryGetValue(u.Id, out var third)
                && (u.Headgear == null || ThirdOverHeadgear.TryGetValue(third, out var cap) && cap == headgear)) return third;
            if (index == 2 && u.Headgear == null) return PioneerVeteran;
            return headgear != null && ByHeadgear.TryGetValue(headgear, out var id) ? id : null;
        }

        /// <summary>The vehicles <paramref name="u"/>'s token draws on <paramref name="date"/>: each sourced one from its day,
        /// in the data's order, as model ids.</summary>
        public static IReadOnlyList<string> VehiclesAt(UnitEntry u, DateTime date)
        {
            if (u.Vehicles == null) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var v in u.Vehicles)
                if (date >= UnitData.Day(v.From)) list.Add(ByVehicle[v.Type]);
            return list;
        }

        /// <summary>How a model is asked for: the game's stream in a player or in play mode. In edit mode nothing is asked for
        /// (the stream's coroutine never runs there), unless a caller sets <see cref="Source"/>: the editor's captures load the
        /// prefabs at once, and the tests answer by hand.</summary>
        public static Action<string, CollectionDetail, Action<GameObject?>>? Source { get; set; }

        static Action<string, CollectionDetail, Action<GameObject?>>? Requester =>
            Source ?? (Application.isPlaying ? new Action<string, CollectionDetail, Action<GameObject?>>(CollectionStream.Request) : null);

        /// <summary>The models that have arrived, by id, and the ids asked for and not yet answered.</summary>
        static readonly Dictionary<string, GameObject> arrived = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        static readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>The figures and vehicles waiting for each asked-for model.</summary>
        static readonly Dictionary<string, List<Transform>> waiting = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
        /// <summary>The near prefabs answered (null: missing or damaged, so the far model stays), and the callers waiting for
        /// each near prefab asked for.</summary>
        static readonly Dictionary<string, GameObject?> near = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        static readonly Dictionary<string, List<Action>> nearWaiting = new Dictionary<string, List<Action>>(StringComparer.Ordinal);

        /// <summary>Raised once for each model when it first arrives (the unit layer then draws the tokens that waited for it).</summary>
        public static event Action<string>? ModelArrived;

        /// <summary>Forgets every model and request (tests, and a change of <see cref="Source"/>).</summary>
        public static void Forget() { arrived.Clear(); asked.Clear(); waiting.Clear(); near.Clear(); nearWaiting.Clear(); }

        /// <summary>True when model <paramref name="id"/> has arrived (and its prefab still exists).</summary>
        public static bool Arrived(string id) => arrived.TryGetValue(id, out var p) && p != null;

        /// <summary>Asks for model <paramref name="id"/> once, without a figure to put it on (an Arab force's token waits for it).</summary>
        public static void Prefetch(string id) => Ask(id);

        /// <summary>Puts model <paramref name="id"/> on <paramref name="piece"/> (a figure or a vehicle's holder, in real metres
        /// scaled by <see cref="UnitShowcase.FigureScale"/>): at once when it has arrived, else when it arrives. Nothing for a
        /// null id.</summary>
        public static void Dress(Transform piece, string? id)
        {
            if (id == null || piece == null) return;
            if (arrived.TryGetValue(id, out var have) && have != null) { Swap(piece, id, have); return; }
            if (!waiting.TryGetValue(id, out var list)) waiting[id] = list = new List<Transform>();
            list.Add(piece);
            Ask(id);
        }

        /// <summary>Asks once for model <paramref name="id"/>'s near (close-zoom) prefab and calls <paramref name="ready"/> when
        /// it is answered (at once if it already was), found or not: <see cref="Near"/> then gives it, or null.</summary>
        internal static void RequestNear(string id, Action ready)
        {
            if (near.ContainsKey(id)) { ready(); return; }
            var ask = Requester;
            if (ask == null) return;
            if (!nearWaiting.TryGetValue(id, out var list)) nearWaiting[id] = list = new List<Action>();
            list.Add(ready);
            if (list.Count > 1) return;   // asked already
            ask(id, CollectionDetail.Near, prefab =>
            {
                near[id] = prefab;   // null: fail open, the far model stays (the stream has warned once)
                if (!nearWaiting.TryGetValue(id, out var done)) return;
                nearWaiting.Remove(id);
                foreach (var callback in done) callback();
            });
        }

        /// <summary>Model <paramref name="id"/>'s near prefab once it has arrived, else null.</summary>
        internal static GameObject? Near(string id) => near.TryGetValue(id, out var prefab) ? prefab : null;

        static void Ask(string id)
        {
            if (Arrived(id) || asked.Contains(id)) return;
            var ask = Requester;
            if (ask == null) return;
            asked.Add(id);
            ask(id, CollectionDetail.Far, prefab =>
            {
                asked.Remove(id);
                var pieces = waiting.TryGetValue(id, out var l) ? l : null;
                waiting.Remove(id);
                if (prefab == null) return;   // fail open: the code-built figure stays (the stream has warned once)
                if (arrived.Count == 0 && Application.isPlaying) Debug.Log("UNITS figures: the Collection's figures streamed in");
                arrived[id] = prefab;
                if (pieces != null) foreach (var p in pieces) if (p != null) Swap(p, id, prefab);   // a token rebuilt meanwhile is gone
                ModelArrived?.Invoke(id);
            });
        }

        /// <summary>A vehicle's length (its longest side, seen from above) as a share of a drawn figure's height: the models
        /// are sized against the figures as Civ draws them, not to scale (as the aircraft and the ship, docs/units.md), so a
        /// vehicle fits on the token's base behind the trio.</summary>
        public const float VehicleLengthShare = 0.7f;

        /// <summary>The swap: the model under the piece, in its frame (facing +Z, feet or wheels on the origin), and the
        /// code-built mesh's renderer off, together. A figure takes the code-built figure's height (or, with none, a man's
        /// height); a vehicle takes <see cref="VehicleLengthShare"/>.</summary>
        static void Swap(Transform piece, string id, GameObject prefab)
        {
            if (piece.Find(ModelName) != null) return;
            // The model under a holder in the piece's frame: the prefab keeps its own root transform (every Collection far
            // prefab's root carries the FBX's axis turn, -90 degrees about X; resetting it would lay the model on its back).
            // The piece's yaw (the camera's turn, #644) is its parent's, so it composes on top of that rotation.
            var holder = new GameObject(ModelName).transform;
            holder.SetParent(piece, false);
            var model = UnityEngine.Object.Instantiate(prefab, holder, false);
            var code = piece.TryGetComponent<MeshRenderer>(out var mr) ? mr : null;
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                if (BoxIn(holder, model.transform) is Bounds have && have.size.y > 1e-6f)
                {
                    float k;
                    bool vehicle = ByVehicleId(id);
                    if (vehicle) k = VehicleLengthShare * UnitBadge.FigureHeightM / Mathf.Max(have.size.x, have.size.z, 1e-6f);
                    else
                    {
                        // An Arab force's figure has no code-built mesh (no MeshFilter: TryGetComponent, as ?. skips Unity's null).
                        var codeMesh = piece.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                        k = (codeMesh != null ? codeMesh.bounds.size.y : UnitBadge.FigureHeightM) / have.size.y;
                    }
                    model.transform.localPosition -= new Vector3(have.center.x, have.min.y, have.center.z);   // centred, feet on the origin
                    holder.localScale = Vector3.one * k;
                }
            }
            foreach (var r in renderers)
            {
                if (code != null) { r.shadowCastingMode = code.shadowCastingMode; r.receiveShadows = code.receiveShadows; }
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            if (code != null) code.enabled = false;
            if (CollectionCatalog.Animated.Contains(id)) UnitFigureDetail.Of(piece).Add(holder, id);   // C05 has no idle: he stays far
        }

        /// <summary>The box of <paramref name="model"/>'s meshes in <paramref name="frame"/>'s space, from each mesh's own
        /// bounds (Renderer.bounds is empty while the layer is off). A skinned mesh is drawn through its bones, so its box is
        /// the renderer's local bounds in its root bone's space (as Unity bounds it), not its mesh's. Null with no mesh.</summary>
        internal static Bounds? BoxIn(Transform frame, Transform model)
        {
            var inv = frame.worldToLocalMatrix;
            Bounds? box = null;
            void Add(Bounds b, Transform at)
            {
                var m = inv * at.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var p = m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
                    if (box is Bounds bb) { bb.Encapsulate(p); box = bb; } else box = new Bounds(p, Vector3.zero);
                }
            }
            foreach (var f in model.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh != null) Add(f.sharedMesh.bounds, f.transform);
            foreach (var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.sharedMesh != null) Add(s.localBounds, s.rootBone != null ? s.rootBone : s.transform);
            return box;
        }

        static bool ByVehicleId(string id) => id == Napoleonchik || id == WillysJeep;

        /// <summary>True when <paramref name="piece"/> shows its Collection model (and not a code-built mesh).</summary>
        public static bool Dressed(Transform piece)
        {
            var code = piece.TryGetComponent<MeshRenderer>(out var mr) ? mr : null;
            return piece.Find(ModelName) != null && (code == null || !code.enabled);
        }
    }
}
