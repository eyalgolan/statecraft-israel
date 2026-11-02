#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Tal Heuberger's weapons in the Israeli soldier figures' right hands (#2083; owner, 2 November 2026: "Yes, on the
    /// soldier figures (Recommended)": "Attach a period-correct weapon to the unit-token soldier figures (sourced per army and
    /// date), measured for load"). Presentation only. A figure holds a weapon only where a row below gives its army one, and
    /// only a figure with Tal's 67-bone rig (a <see cref="HandBone"/>) can hold it; every other figure stays as it is (the
    /// [GAP] list in docs/units.md). Each weapon streams in a bundle of its own after the people's, never in the first download.</summary>
    public static class UnitWeapons
    {
        /// <summary>The name of the weapon's object under the figure's hand bone (UnitFigures.BoxIn leaves it out, so a weapon
        /// never changes the figure's height or feet).</summary>
        public const string HeldName = "HeldWeapon";
        /// <summary>The hand bone of Tal Heuberger's 67-bone figures (C04 to C07, C12F; C06's point-fire export binds its K98k to it).</summary>
        public const string HandBone = "Right_Hand";

        /// <summary>One sourced row: which army carried the weapon, from when. The rows are agent-read: an AI agent read the
        /// sources; a person's check is pending (docs/CONTENT_POLICY.md, rule 6).</summary>
        public sealed class Row
        {
            public string Weapon = "", Side = "", From = "", Note = "";
            public string[] Sources = Array.Empty<string>();
            public string Review = "agent-read";
        }

        /// <summary>The sourced rows (checked 2 November 2026). Every token date is on or after 15 May 1948 (content/map/
        /// units_1948.yaml), so after each row's From; UnitWeaponsTests keeps that true.</summary>
        public static readonly IReadOnlyList<Row> Rows = new[]
        {
            new Row
            {
                Weapon = CollectionCatalog.K98k, Side = UnitData.Israel, From = "1948-04-02",
                Note = "Czechoslovak Mauser rifles (the P-18, the Czech name of the K98k) for the Haganah: the first contract (14 January 1948) " +
                    "was for \"200 MG 34 machine guns, 4,500 P 18 rifles\"; the first air shipment landed the night of 31 March to 1 April 1948, " +
                    "and the 4,500 rifles \"arrived at Tel Aviv port aboard the Nora on 2 April\" (Palyam: the Nora reached Tel Aviv on 1 April). " +
                    "By October 1948, 34,500 P-18 rifles had been delivered.",
                Sources = new[]
                {
                    "https://en.wikipedia.org/wiki/Arms_shipments_from_Czechoslovakia_to_Israel",
                    "https://palyam.org/English/ArmsShips/Nora.pdf",
                    "https://en.wikipedia.org/wiki/Karabiner_98k",
                },
            },
            new Row
            {
                Weapon = CollectionCatalog.Sten, Side = UnitData.Israel, From = "1945-01-01",
                Note = "\"The Sten submachine gun, the personal weapon of the Palmach\" (Ayalon Institute); Stens were \"clandestinely " +
                    "manufactured in Tel Aviv and on various kibbutzim in 1945–1948\" (Sten). Held by the Palmach scout (C12F) only.",
                Sources = new[]
                {
                    "https://en.wikipedia.org/wiki/Ayalon_Institute",
                    "https://en.wikipedia.org/wiki/Sten",
                },
            },
        };

        /// <summary>The weapon each Israeli token figure holds: the Palmach scout (C12F, a Palmach woman on Harel's token,
        /// UnitFigures.ThirdFigure) the Palmach's Sten; the other riflemen with Tal's rig the K98k. No Arab force has a row: no
        /// source found places a K98k, an MG34 or a Sten with one in 1948, and Tal made no Lee-Enfield or Bren ([GAP]).</summary>
        public static readonly IReadOnlyDictionary<string, string> ByFigure = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [UnitFigures.SabraPioneer] = CollectionCatalog.K98k, [UnitFigures.PioneerVeteran] = CollectionCatalog.K98k,
            [UnitFigures.YoungRecruit] = CollectionCatalog.K98k, [UnitFigures.VeteranFighter] = CollectionCatalog.K98k,
            [UnitFigures.PalmachScout] = CollectionCatalog.Sten,
        };

        /// <summary>The weapon figure <paramref name="figure"/> holds on a token of <paramref name="side"/>, or null.</summary>
        public static string? WeaponFor(string side, string? figure)
        {
            if (figure == null || !ByFigure.TryGetValue(figure, out var weapon)) return null;
            foreach (var row in Rows) if (row.Weapon == weapon && row.Side == side) return weapon;
            return null;
        }

        /// <summary>The K98k in the hand bone's frame, as Tal Heuberger fixed it to C06's right hand in their point-fire export
        /// (constant over all 97 frames), turned into the C06 rig's frame (UnitFigurePoseSource.Correction). Their other
        /// 67-bone figures share the rig's bone names and axes, so they hold it the same way. UnitWeaponsTests recomputes it
        /// from the two FBX files.</summary>
        public static readonly Vector3 SocketPosition = new Vector3(-0.0261189211f, 0.121834747f, 0.09528886f);
        public static readonly Quaternion SocketRotation = new Quaternion(0.5445083f, 0.2754634f, -0.4476301f, 0.6536498f);
        public const float K98kSocketScale = 0.619152546f;

        /// <summary>The Sten has no fixed pair: it takes the K98k's socket, scaled by the two weapons' sourced lengths
        /// (Sten Mk II 762 mm, https://en.wikipedia.org/wiki/Sten; K98k 1,110 mm, https://en.wikipedia.org/wiki/Karabiner_98k;
        /// Tal's two GLBs are both one unit long).</summary>
        public const float StenLengthMm = 762f, K98kLengthMm = 1110f;

        /// <summary>How a weapon or the pose source is asked for outside a player: edit-mode captures and tests answer by hand
        /// (each id once; setting it forgets the answers). With none, a player or play mode asks the game's stream, which
        /// shares a bundle between its leases; edit mode asks nothing. Apart from UnitFigures.Source, so a figure's requests
        /// stay exactly as they were.</summary>
        public static Action<string, Action<GameObject?>>? Source
        {
            get => source;
            set { source = value; Forget(); }
        }
        static Action<string, Action<GameObject?>>? source;
        static readonly Dictionary<string, GameObject?> answered = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        static readonly Dictionary<string, List<Action<GameObject?>>> waiting = new Dictionary<string, List<Action<GameObject?>>>(StringComparer.Ordinal);

        /// <summary>Asks for weapon or pose source <paramref name="id"/>; <paramref name="done"/> gets its prefab, or null (fail
        /// open). The returned lease (play only) holds its bundle until it is disposed.</summary>
        internal static IDisposable? Request(string id, Action<GameObject?> done)
        {
            if (source == null) return Application.isPlaying ? CollectionStream.Acquire(id, CollectionDetail.Far, done) : null;
            if (answered.TryGetValue(id, out var have)) { done(have); return null; }
            if (waiting.TryGetValue(id, out var list)) { list.Add(done); return null; }
            waiting[id] = new List<Action<GameObject?>> { done };
            var ask = source;
            ask(id, prefab =>
            {
                if (ask != source || !waiting.TryGetValue(id, out var callers)) return;   // the source changed meanwhile
                waiting.Remove(id); answered[id] = prefab;
                foreach (var caller in callers) caller(prefab);
            });
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Source = null;

        /// <summary>Forgets every answer and request (a change of <see cref="Source"/>, and tests).</summary>
        public static void Forget() { answered.Clear(); waiting.Clear(); }

        public static float SocketScale(string weapon) => weapon == CollectionCatalog.Sten ? K98kSocketScale * StenLengthMm / K98kLengthMm : K98kSocketScale;

        /// <summary>Puts <paramref name="weapon"/> in the right hand of figure model <paramref name="instance"/> (its far or near
        /// model), once; nothing when the model has no hand bone. Its renderers draw as the figure's do.</summary>
        internal static GameObject? Arm(Transform instance, string weapon, GameObject prefab)
        {
            var hand = FindDeep(instance, HandBone);
            if (hand == null || hand.Find(HeldName) != null) return null;
            var held = UnityEngine.Object.Instantiate(prefab, hand, false);
            held.name = HeldName;
            held.transform.localPosition = SocketPosition;
            held.transform.localRotation = SocketRotation;
            held.transform.localScale = Vector3.one * SocketScale(weapon);
            var like = instance.GetComponentInChildren<Renderer>(true);
            foreach (var r in held.GetComponentsInChildren<Renderer>(true))
            {
                if (like != null) { r.shadowCastingMode = like.shadowCastingMode; r.receiveShadows = like.receiveShadows; r.forceRenderingOff = like.forceRenderingOff; }
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
            return held;
        }

        /// <summary>True when <paramref name="t"/> is part of a held weapon under <paramref name="model"/>.</summary>
        internal static bool IsHeld(Transform t, Transform model)
        {
            for (var at = t; at != null && at != model; at = at.parent)
                if (at.name == HeldName) return true;
            return false;
        }

        internal static Transform? FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
