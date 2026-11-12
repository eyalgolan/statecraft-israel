#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's ships and the C-54 from The 1948 Collection (issue #669), presentation only:
    /// <list type="bullet">
    /// <item>the immigrant steamer off Haifa's port, and a wooden fishing boat off Jaffa and one off Acre, at every date (the
    /// issue's harbours);</item>
    /// <item>the Altalena off Tel Aviv on the turn the Altalena card plays: the card's own window (content/events/altalena.yaml,
    /// 15-23 June 1948, owner Q-CUT-7), the monthly turn that holds the ship's sourced days off the coast, 20-22 June
    /// (Kfar Vitkin on the 20th, aground off Tel Aviv on the 21st, shelled on the 22nd: the card's sources). One ship on one
    /// turn stands at one place, so it stands off Tel Aviv, where it ran aground and burned;</item>
    /// <item>a C-54 at Lod (Lydda) Airport while Operation Magic Carpet's main airlift ran, from July 1949 to its last flights
    /// into Lydda on 24 September 1950 (<see cref="C54From"/>, <see cref="C54To"/>);</item>
    /// <item>the Egyptian Spitfire flying a slow loop near the western board edge (about 30 km in) toward el-Arish and back (<see cref="SpitfireLoop"/>),
    /// banked into its turns, its propeller turning, its shadow on the ground and a small Egyptian flag over its tail, from 15 May 1948 to 2 January 1949
    /// (<see cref="SpitfireFrom"/>, <see cref="SpitfireTo"/>, #1932), never on a hex or an airfield;</item>
    /// <item>two Egyptian Dakotas, a pair on one loop over Tel Aviv, bombing the city (<see cref="DakotaLoop"/>; owner, 4
    /// November 2026, #2482: "1 + make the visual 2 slightly smaller planes and not 0ne big one"), banked into their turns,
    /// their propellers turning, their shadows on the ground and Egypt's flag over each tail, on the turns of the first
    /// bombing period, 15 May to 11 June 1948 (<see cref="DakotaFrom"/>, <see cref="DakotaTo"/>; SeaAirCollection.Dakota.cs), and
    /// of the second, 8 to 17 July 1948 (<see cref="DakotaSecondFrom"/>), when Tel Aviv's air defence flies against them: two
    /// of 101 Squadron's Avia S-199s attack the lead, hitting it but never bringing it down, and anti-aircraft fire brings
    /// the wing down (<see cref="TelAvivAirDefence"/>; SeaAirCollection.AirDefence.cs).</item>
    /// </list>
    /// The boats lie on the open sea past the surf, where SeaCraft's own boats sail (SeaCraft.OffshoreNearM to OffshoreM out
    /// from the same shore points), each hull clear of the harbour works, of SeaCraft's boats and of each other
    /// (SeaCraft.Clear). Every craft is at the one world scale of the map's ambient models (<see cref="SizeOf"/>: 24x real,
    /// set by the steam train; #1030, #1033), except the Spitfire at the unit tokens'
    /// aircraft scale (420x, #2014) and the Dakotas a little under it (<see cref="DakotaScale"/>, #2482). The C-54 stands on the nearest free ground by the airport, clear of the kits,
    /// routes, units and the player's placements, with the kit trees under it left out (VegetationLayer.Hide, as under the
    /// unit figures). Every craft draws at every zoom, as SeaCraft's boats do. Each model is the Far mesh, asked of
    /// <see cref="CollectionStream"/> when first wanted, so nothing is fetched before the first frame. It fails open: a model
    /// that does not stream logs one warning and is not drawn; the map plays on.</summary>
    [ExecuteAlways]
    public sealed partial class SeaAirCollection : MonoBehaviour
    {
        public const string Name = "SeaAirCollection";
        public const string Altalena = "vehicle-altalena-lst-1948-v2wl";
        public const string FishingBoat = "vehicle-fishing-boat-1948-v2wl";
        public const string Steamer = "vehicle-immigrant-steamer-1948-v2wl";
        public const string C54 = "figure-vehicle-c54-skymaster-1948-v2";
        public const string Spitfire = CollectionCatalog.EgyptianSpitfire;
        public const string Dakota = CollectionCatalog.EgyptianDakota;

        /// <summary>The card whose window dates the Altalena (content/events/altalena.yaml).</summary>
        public const string AltalenaEvent = "altalena";

        /// <summary>The C-54 at Lod from 1 July 1949 to 24 September 1950. Operation Magic Carpet flew Yemenite Jews to Israel
        /// in chartered Douglas DC-4s (the civilian C-54 Skymaster) and Curtiss C-46s; its main wave ran from July to November
        /// 1949 (Meir-Glitzenstein, Israel Studies, 2011), and it ended on 24 September 1950, "when the last two aircraft
        /// reached Lydda Airport" (Schechtman, Jewish Social Studies, 1952): both as cited by en.wiki "Operation Magic Carpet
        /// (Yemen)", checked 18 October 2026. On the game's monthly turns (the 15th) it shows from the 15 July 1949 turn to the
        /// 15 September 1950 turn.</summary>
        public static readonly DateTime C54From = new DateTime(1949, 7, 1), C54To = new DateTime(1950, 9, 24);

        /// <summary>Lod (Lydda) Airport, today Ben Gurion Airport: 32.00944 N, 34.88278 E (en.wiki "Ben Gurion Airport",
        /// checked 18 October 2026). Taken by the IDF on 10 July 1948 (Operation Danny); flights resumed on 24 November 1948.</summary>
        public const double LodLon = 34.88278, LodLat = 32.00944;

        /// <summary>Tal Heuberger's Egyptian Spitfire, only in flight near the board's western edge (30 km in) toward el-Arish (owner ruling,
        /// 30 October 2026, #1932: "(c) In flight / board edge only"; no airfield on the board). el-Arish was the REAF's "only
        /// 'front-line' aerodrome" (research note egyptian-air-bases-and-raids-1948-1872.md, row A1), at 31.132072 N, 33.803376 E
        /// (en.wiki "Arish", checked 30 October 2026), about 0.4 degrees west of the board. The dates come from #1894's
        /// research note, an AI draft not yet checked by a person: 15 May 1948 (rows R2, R4, the first Spitfire raid in the note)
        /// to the last sortie of Spitfire no. 651 in the al-Arish Operational Diary, 2 January 1949 (row A3, which covers that
        /// one aircraft from 9 June 1948): the turns of 15 May to 15 December 1948.</summary>
        public static readonly DateTime SpitfireFrom = new DateTime(1948, 5, 15), SpitfireTo = new DateTime(1949, 1, 2);
        public const double ElArishLon = 33.803376, ElArishLat = 31.132072;

        /// <summary>The real long side (m, seen from above) each model stands for. The Altalena was USS LST-138, an LST-1-class
        /// tank landing ship (en.wiki "Altalena Affair" and "LST-1-class tank landing ship", its LST-100 to LST-199 list),
        /// 328 ft overall (en.wiki "USS LST-1", the class's lead ship: "328 ft (100 m) oa"); the C-54's wingspan is 35.81 m (en.wiki
        /// "Douglas C-54 Skymaster", C-54G); all checked 18 October 2026.
        /// The Spitfire's is its wingspan, 36 ft 10 in (11.23 m), the standard wing of the Mk V and the Mk IX alike (en.wiki
        /// "Supermarine Spitfire", checked 30 October 2026; the research note's two candidate marks, its Disagreements table). The steamer and the fishing boat are generic models,
        /// no one ship, so theirs are DESIGN VALUES, typical of the kind: an 80 m immigrant steamer and a 10 m wooden fishing
        /// boat.</summary>
        const float FootM = 0.3048f;
        public static float RealLengthM(string model) => model switch
        {
            Altalena => 328f * FootM, C54 => 35.81f, Steamer => 80f, FishingBoat => 10f, Spitfire => 11.23f, Dakota => DakotaSpanM,
            _ => throw new ArgumentException($"'{model}' is not one of #669's models", nameof(model)),
        };

        /// <summary>The model's long side on the map (m): its real size at the one world scale of every Tal Heuberger model that is not
        /// a game piece (SettlementKits.ModelScale, 24x: an 8.5 m house as long as the steam train's 204 m locomotive car; the
        /// owner, 18 October 2026, #1033: "Train scale for all ambient life"). The Altalena is 2.4 km, the C-54 860 m across. The one exception is the
        /// Spitfire, drawn at the unit tokens' aircraft scale (UnitShowcase.AircraftScale, 420x: 4.7 km across), so it reads at strategic zoom (owner ruling, #2014),
    /// and the Dakotas, each <see cref="DakotaScale"/> of that (owner, #2482: "2 slightly smaller planes and not 0ne big one").</summary>
        public static float SizeOf(string model) =>
            RealLengthM(model) * (model == Spitfire ? Statecraft.Game.Map.Units.UnitShowcase.AircraftScale
                : model == Dakota ? Statecraft.Game.Map.Units.UnitShowcase.AircraftScale * DakotaScale : SettlementKits.ModelScale);

        /// <summary>DESIGN VALUE: every craft lies with its length east-west (SeaCraft heading 0), side-on to the gameplay
        /// camera looking north, as SeaCraft's boats lie. A model is turned 0 or 90 degrees about the vertical, on top of its
        /// prefab's own rotation, whichever lays its longer side along x.</summary>
        public const float Heading = 0f;

        static readonly Action<string, CollectionDetail, Action<GameObject?>> DefaultRequest = CollectionStream.Request;
        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = DefaultRequest;
        sealed class TestSeamLease : IDisposable { public void Dispose() { } }

        /// <summary>One craft to draw: its key, its model, and the place it belongs to (a SeaCraft coast's place, or "lod").</summary>
        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model, Place;
            public Want(string key, string model, string place) { Key = key; Model = model; Place = place; }
            public bool Equals(Want o) => Key == o.Key && Model == o.Model && Place == o.Place;
            public override bool Equals(object? obj) => obj is Want w && Equals(w);
            public override int GetHashCode() => (Key, Model, Place).GetHashCode();
        }

        /// <summary>The craft wanted as at <paramref name="date"/>, in a fixed order.</summary>
        public static List<Want> Wants(DateTime date, Content content)
        {
            var wants = new List<Want>
            {
                new Want("haifa-steamer", Steamer, "haifa"),
                new Want("jaffa-fishing", FishingBoat, "jaffa"),
                new Want("acre-fishing", FishingBoat, "acre"),
            };
            var card = content.Events.FirstOrDefault(e => e.Id == AltalenaEvent);
            if (card != null && card.To is DateTime to && date >= card.From && date < to)
                wants.Add(new Want("tel-aviv-altalena", Altalena, "tel_aviv"));
            if (date >= C54From && date <= C54To) wants.Add(new Want("lod-c54", C54, "lod"));
            if (date >= SpitfireFrom && date <= SpitfireTo) wants.Add(new Want("el-arish-spitfire", Spitfire, "el_arish"));
            if (DakotaWanted(date)) foreach (var key in DakotaKeys) wants.Add(new Want(key, Dakota, DakotaTown));
            return wants;
        }

        /// <summary>The Spitfire's spot: near the board's western edge (world x = 0 is lon0, GeoFrame) at el-Arish's latitude,
        /// flying in from el-Arish (due east). DESIGN VALUE: 30 km in (owner, #2014: "Move it inward"). Nearer the edge,
        /// CameraController.Step holds the player's view on the map and the aircraft sits at or past the frame's left edge;
        /// from 25 km in the whole aircraft is in frame at 40 and 150 km, and from 30 km in it clears the button rail on the
        /// left (measured, #2014). At the closest zoom (6 km) the camera is below its 4,200 m altitude at every spot measured
        /// along this latitude, so no spot here frames it there.</summary>
        public static Vector2 EdgeSpot(GeoFrame frame) =>
            new Vector2(30000f, frame.ToWorld(ElArishLon, ElArishLat, 0).z);

        /// <summary>DESIGN VALUES: the Spitfire's loop (owner, 2 November 2026: "Fly a slow loop", a short repeating pass in
        /// from el-Arish over the western edge that turns back; v0.3.3 drew it standing still). A long ellipse centred on
        /// <see cref="EdgeSpot"/>, 5 km each way east-west and 3 km each way north-south, flown once every 40 s: in east
        /// along its southern side, a turn north at the east end, back west along its northern side, a turn south at the
        /// west end. About 25.5 km round, so about 640 m/s of map: with the camera's 35 degree field of view (CameraRig.Fov),
        /// about 27 px a second at 40 km and 7 px a second at 150 km on a 1080p screen (the steam train moves about 9 px a
        /// second at the gameplay zoom, RouteTrain.SpeedMps). The west end's centre is 25 km in, where the whole aircraft is
        /// still in frame at 40 and 150 km (measured for #2014); the whole aircraft stays over the board (more than 20 km in).
        /// The nearest unit anchor (content/map/units_1948.yaml: the Egyptian Expeditionary Force at Gaza, to 21 May 1948)
        /// is 42 km from the centre, and Lod, the board's one drawn airfield, about 100 km.</summary>
        public const float SpitfireLoopSeconds = 40f, SpitfireLoopHalfLengthM = 5000f, SpitfireLoopHalfWidthM = 3000f;

        /// <summary>Where the Spitfire is, from its loop's centre (world x, z, m), <paramref name="seconds"/> into its loop:
        /// at 0, due south of the centre, flying east (in from el-Arish); then anticlockwise seen from above.</summary>
        public static Vector2 LoopOffset(float seconds)
        {
            float a = LoopAngle(seconds);
            return new Vector2(SpitfireLoopHalfLengthM * Mathf.Sin(a), -SpitfireLoopHalfWidthM * Mathf.Cos(a));
        }

        /// <summary>The Spitfire's direction of travel there (world x, z; unit length): the loop's tangent.</summary>
        public static Vector2 LoopHeading(float seconds)
        {
            float a = LoopAngle(seconds);
            return new Vector2(SpitfireLoopHalfLengthM * Mathf.Cos(a), SpitfireLoopHalfWidthM * Mathf.Sin(a)).normalized;
        }

        /// <summary>The angle round the ellipse, from the clock taken modulo the period first, so it stays exact however long
        /// the session has run.</summary>
        static float LoopAngle(float seconds) => 2f * Mathf.PI * Mathf.Repeat(seconds, SpitfireLoopSeconds) / SpitfireLoopSeconds;

        /// <summary>The loop's curvature (1/m) <paramref name="seconds"/> into it: for x = a sin t, z = -b cos t,
        /// ab / (a² cos² t + b² sin² t)^(3/2). Least on the passes (b / a², at 0 and half a period) and most at the turns at
        /// the ends (a / b², a quarter and three quarters of a period in).</summary>
        public static float LoopCurvature(float seconds)
        {
            float t = LoopAngle(seconds), a = SpitfireLoopHalfLengthM, b = SpitfireLoopHalfWidthM;
            float c = Mathf.Cos(t), s = Mathf.Sin(t);
            return a * b / Mathf.Pow(a * a * c * c + b * b * s * s, 1.5f);
        }

        /// <summary>DESIGN VALUE: the bank (degrees, into the turn) for a path's <paramref name="curvature"/> (1/m), so the
        /// aircraft reads as flying round its loop (owner's report, 2 November 2026: "it doesnt look fliying"): none on a
        /// straight or on this loop's passes (its least curvature, b / a²), the unit tokens' aircraft bank
        /// (UnitShowcase.BankDegrees, 28 degrees) at its tightest turns (a / b²), and in between in proportion. A real
        /// coordinated turn would bank by v² / (g r), which at the map's speeds and radii is past 80 degrees: a gentle,
        /// readable bank is the aim, not that.</summary>
        public static float BankFor(float curvature)
        {
            float a = SpitfireLoopHalfLengthM, b = SpitfireLoopHalfWidthM, least = b / (a * a), most = a / (b * b);
            return Statecraft.Game.Map.Units.UnitShowcase.BankDegrees * Mathf.Clamp01((curvature - least) / (most - least));
        }

        /// <summary>The Spitfire's bank <paramref name="seconds"/> into its loop (degrees, its inner wing down):
        /// <see cref="BankFor"/> the loop's curvature there.</summary>
        public static float LoopBankDegrees(float seconds) => BankFor(LoopCurvature(seconds));

        /// <summary>The Spitfire's parts beside its airframe: the propeller disc (a child of its mesh), and the object, next
        /// to the aircraft in this layer, that holds its shadow and its flag (they do not bank or turn with it).</summary>
        public const string SpitfirePropellerName = "Propeller", SpitfireMarksSuffix = "-marks", SpitfireShadowName = "Shadow", SpitfireFlagName = "Flag";

        /// <summary>DESIGN VALUES for the propeller disc on the fused mesh, from the model's FBX vertices
        /// (generated-1948-13-egyptian-spitfire.fbx, read 2 November 2026): the spinner's tip is the mesh's front-most point
        /// (-y), on its centre line, 0.70 of the model's height up; the still blades stand 0.03-0.13 of its length behind the
        /// front, reach about 0.11 of the span above and below the hub (one blade tip is the model's highest point) and up to
        /// 0.18 of it sideways. The disc's centre stands <see cref="PropBehindNoseShare"/> of the mesh's length behind its
        /// front, <see cref="PropRadiusShare"/> of its span in radius, its top at the mesh's top (its hub then 0.63 of the
        /// height up, a little under the spinner's tip), so its box stays inside the airframe's (see Propeller). A wider disc
        /// would rise over the model's top; so the still blades' outer parts still show past its rim.</summary>
        public const float PropRadiusShare = 0.118f, PropBehindNoseShare = 0.065f;

        /// <summary>The disc is two blurs, <see cref="PropLayer"/> of its radius in front of and behind its centre, so the blur
        /// lies over the blades' roots whether the camera sees the aircraft from ahead or from behind.</summary>
        public const float PropLayer = 0.3f;

        /// <summary>DESIGN VALUE: the shadow's length and width over the aircraft's seen from above: a little larger, so its
        /// soft rim (fading to nothing) leaves a dark core about the aircraft's size.</summary>
        public const float ShadowShare = 1.1f;

        /// <summary>A boat's spot: out from its coast's shore point (SeaCraft.Coasts) between SeaCraft.OffshoreNearM and
        /// OffshoreM, at a seeded spread along the shore, the first whose hull passes SeaCraft.Clear against the harbour works
        /// and every boat already placed. Null when none is.</summary>
        internal static Vector2? SeaSpot(Want want, KitGround ground, IReadOnlyList<SettlementKits.Placed> works, List<SeaCraft.Craft> boats)
        {
            var coast = SeaCraft.Coasts.FirstOrDefault(c => c.place == want.Place);
            if (coast.place == null) return null;
            var w = ground.Frame.ToWorld(coast.lon, coast.lat, 0); var at = new Vector2(w.x, w.z);
            var shore = ground.Water(at.x, at.y) ? at : SeaCraft.FirstWater(at, coast.seaward, ground, 20000f);
            if (!(shore is Vector2 s)) return null;
            var along = new Vector2(-coast.seaward.y, coast.seaward.x);
            float m = KitScale.Landmarks / 60f, length = SizeOf(want.Model);
            var rnd = new KitRandom(want.Key, 669);
            var workList = works as List<SettlementKits.Placed> ?? works.ToList();
            for (int tries = 0; tries < 60; tries++)
            {
                var c = s + coast.seaward * rnd.Range(SeaCraft.OffshoreNearM, SeaCraft.OffshoreM) * m + along * rnd.Range(-2.2f, 2.2f) * length;
                var craft = new SeaCraft.Craft(SeaCraft.Kind.Steamer, c, Heading, length, want.Place);
                if (SeaCraft.Clear(craft, ground, workList, boats, 0.5f * length)) return c;
            }
            return null;
        }


        CollectionResidency? residency;
        CollectionResidencyView? residencyView;
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? ownedAcquire;
        readonly List<UnityEngine.Object> retired = new List<UnityEngine.Object>();
        internal CollectionResidencyMetrics.Category ResidencySnapshot() => CollectionResidencyMetrics.Row("craft",
            isActiveAndEnabled ? Admitted().Count() : 0, transform.childCount,
            prefabs.Keys.Concat((isActiveAndEnabled ? Admitted().Select(w => w.Model) : Enumerable.Empty<string>())), residency?.OwnerCount ?? 0, residency?.PendingCount ?? 0,
            prefabs.Values.Count(p => p != null), GetComponentsInChildren<Renderer>(true).Length, 0, prefabs.Values.Count(p => p == null), prefabs.Keys.Select(k => k + "|Far"));

        internal void ConfigureResidency(CollectionResidencyPolicy policy,
            Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? acquire)
        {
            if (residencyView != null) throw new InvalidOperationException("Residency already configured.");
            if (asked.Count != 0 && acquire == null) throw new InvalidOperationException("Configure residency before requests; pinned originals remain blockers.");
            ownedAcquire = acquire; residencyView = new CollectionResidencyView(policy);
            // Construction configures before first Show; injected acquisition is a test seam.
            ClearForRetirement();
        }
        IDisposable AcquireOriginal(string id, CollectionDetail detail, Action<GameObject?> answer)
        {
            if (ownedAcquire != null) return ownedAcquire(id, detail, answer);
            // The editor/fake-source seam owns its own assets, as before residency (and as CivilianPeople/RuralLife do).
            if (Request != DefaultRequest) { Request(id, detail, answer); return new TestSeamLease(); }
            return CollectionStream.Acquire(id, detail, answer);
        }
        IEnumerable<Want> Admitted()
        {
            var keys = residencyView?.Keys;
            return keys == null ? wants : wants.Where(w => keys.Contains(w.Key));
        }
        Action<List<(string key, Vector3 at, float radius)>>? fillResidencyAnchors;
        void FillResidencyAnchors(List<(string key, Vector3 at, float radius)> anchors)
        {
            if (map == null) return;
            var edge = EdgeSpot(map.Frame);   // the Spitfire's anchor is on the ground under it; only its distance counts
            for (int i = 0; i < wants.Count; i++)
            {
                var w = wants[i];
                if (w.Model == Dakota)   // its anchor is on the ground under its loop's centre, as far out as it flies (#2482)
                {
                    if (DakotaCentre() is Vector2 d) anchors.Add((w.Key, new Vector3(d.x, 0f, d.y), DakotaSweptHalf.magnitude));
                    continue;
                }
                var c = default((string place, double lon, double lat, Vector2 seaward, int boats));
                for (int j = 0; j < SeaCraft.Coasts.Length; j++)
                    if (SeaCraft.Coasts[j].place == w.Place) { c = SeaCraft.Coasts[j]; break; }
                anchors.Add((w.Key, w.Model == C54 ? map.Frame.ToWorld(LodLon, LodLat, 0) : w.Model == Spitfire ? new Vector3(edge.x, 0, edge.y)
                    : map.Frame.ToWorld(c.lon, c.lat, 0), SizeOf(w.Model)));
            }
        }
        static bool SameKeys(HashSet<string> before, HashSet<string> after)
        {
            if (before.Count != after.Count) return false;
            foreach (string key in before) if (!after.Contains(key)) return false;
            return true;
        }
        void RefreshResidency() => RefreshResidencyCore(true);
        void RefreshResidencyCore(bool redraw)
        {
            if (!isActiveAndEnabled) return;
            residency ??= new CollectionResidency(AcquireOriginal, ClearForRetirement, (id, detail, prefab) => OnModel(id, prefab),
                (instances, owners, keys, complete) => CollectionResidencyMetrics.AfterDestroyed("craft", instances, owners, complete,
                    Application.isPlaying && ownedAcquire == null && Request == DefaultRequest ? () => CollectionStream.Settled(keys) : (Func<bool>?)null, keys), true);
            // v0.3.1 Show requested every wanted craft as Far with no camera or 60 km residency gate.
            residency.Replace(wants.Select(w => (w.Model, CollectionDetail.Far)));
            if (redraw) Rebuild();
        }
        IReadOnlyList<UnityEngine.Object> ClearForRetirement(IReadOnlyCollection<(string id, CollectionDetail detail)> keys)
        {
            var removed = new List<UnityEngine.Object>();
            foreach (var key in keys)
            {
                prefabs.Remove(key.id); asked.Remove(key.id);
                if (!originals.TryGetValue(key.id, out var list)) continue;
                foreach (var go in list) if (go != null)
                {
                    removed.Add(go); keepOf.Remove(go);
                    if (Application.isPlaying) { if (gameObject.activeInHierarchy) go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
                }
                originals.Remove(key.id); Placed.RemoveAll(p => p.Model == key.id);
            }
            // Each drawn craft's own keep-clear boxes (the Spitfire's cover its whole loop, its flag included); the Spitfire's
            // shadow and flag object has none.
            RebuildKeepAll();
            return removed;
        }
        IReadOnlyList<UnityEngine.Object> ClearForRetirement()
        {
            ClearVisuals(); prefabs.Clear(); asked.Clear();

            var pending = retired.ToArray(); retired.Clear(); return pending;
        }
        void ClearVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject; if (residencyView != null) retired.Add(old);
                if (Application.isPlaying) { if (gameObject.activeInHierarchy) old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            Placed.Clear(); Missing.Clear(); keepAll = new List<Bounds[]>(); keepOf.Clear(); dakotaKeeps.Clear(); airDefenceKeeps.Clear(); AirDefence = null;
            originals.Clear();
            if (map != null) Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>());
        }
        bool residencyDirty;
        void ObserveResidency(Camera camera)
        {
            var before = residencyView!.Keys;
            residencyView.Observe(camera, fillResidencyAnchors ??= FillResidencyAnchors, Time.frameCount);
            if (!SameKeys(before, residencyView.Keys))
            { if (Application.isPlaying) residencyDirty = true; else RefreshResidencyCore(false); }
        }
        void LateUpdate()
        {
            if (residencyView != null && residencyView.Prune(Time.frameCount)) residencyDirty = true;
            if (residencyDirty) { residencyDirty = false; RefreshResidencyCore(false); }
        }
        void OnDestroy() { residency?.Dispose(); residency = null; }

        MapScene? map;
        Content? content;
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, List<GameObject>> originals = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
        List<Want> wants = new List<Want>();
        List<(int q, int r)> placements = new List<(int, int)>();
        List<(Vector2 centre, float radius)> taken = new List<(Vector2, float)>();
        List<WildlifeLayer.Want>? wildlife;
        DateTime date;
        bool airDefenceShown;   // AirDefenceWanted at the last rebuild: 15 May and 15 July want the same craft, so the date alone must rebuild (#2649)

        /// <summary>The craft drawn (key, model, spot at the water's or ground's height; the centre of the Spitfire's loop, in the air): a test aid.</summary>
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        /// <summary>The craft wanted but not drawn: the model did not stream, or no spot was clear (a test aid).</summary>
        public readonly List<string> Missing = new List<string>();

        /// <summary>Every drawn craft's world box, for the map labels to keep clear of, as they do of the unit tokens
        /// (MapLabelsPresenter's keepClear, issue #197). A new list after every rebuild and the same list otherwise, so the
        /// labels re-read it only when it changes. The Dakotas' boxes follow them round their loop or their raid (FollowDakotas, #2490, #3140), so
        /// while they fly it is a new list once a step (DakotaKeepStepS).</summary>
        public IReadOnlyList<Bounds[]> KeepClear
        {
            get
            {
                if (!isActiveAndEnabled) return Array.Empty<Bounds[]>();
                FollowDakotas();
                FollowAirDefence();
                return keepAll;
            }
        }
        /// <summary>A new <see cref="KeepClear"/> list from every drawn craft's own boxes, and the air defence's fighters' (keyed
        /// by each fighter's plane, a child of the air defence, which is one of the Dakotas' originals: kept while it is).</summary>
        void RebuildKeepAll()
        {
            var drawn = originals.Values.SelectMany(list => list).Where(go => go != null).ToList();
            keepAll = drawn.Where(keepOf.ContainsKey).Select(go => keepOf[go]).Where(boxes => boxes.Length > 0)   // none for a hidden Dakota (#3140)
                .Concat(airDefenceKeeps.Where(k => k.Go != null && k.Defence != null && keepOf.ContainsKey(k.Go) && drawn.Contains(k.Defence.gameObject))
                    .Select(k => keepOf[k.Go]))
                .ToList();
        }
        List<Bounds[]> keepAll = new List<Bounds[]>();
        readonly Dictionary<GameObject, Bounds[]> keepOf = new Dictionary<GameObject, Bounds[]>();
        void Keep(GameObject go, Bounds box) { var boxes = new[] { box }; keepAll.Add(boxes); keepOf[go] = boxes; }

        /// <summary>Shows the craft wanted as at <paramref name="date"/> (GameRoot calls it on every change, beside
        /// TownLandmarks.ShowAt), redrawing only when the wanted set or the player's placements change.</summary>
        public static SeaAirCollection? ShowAt(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<SeaAirCollection>();
            if (layer == null)
            {
                var go = new GameObject(Name); go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<SeaAirCollection>(); layer.map = map;
            }
            layer.content = content;
            layer.Show(Wants(date, content), placements.Select(p => (p.Q, p.R)).ToList(), date);
            return layer;
        }

        void Show(List<Want> next, List<(int q, int r)> placed, DateTime at)
        {
            date = at;
            // Out of the first bombing period the repeated raid ends, so the next first-period turn starts its raids from the top (#3140).
            if (!DakotaRaidsWanted(at)) DakotaRaidFlight.EndRepeat();
            var others = next.Any(w => w.Model == C54) ? Taken() : new List<(Vector2, float)>();
            bool defence = AirDefenceWanted(at);
            if (next.SequenceEqual(wants) && placed.SequenceEqual(placements) && others.SequenceEqual(taken) && defence == airDefenceShown) return;
            wants = next; placements = placed; taken = others; airDefenceShown = defence;
            OriginalModelRequests.WhenVisible(this, () => RefreshResidency());
        }

        void OnModel(string model, GameObject? prefab)
        {
            if (this == null) return;   // the map was rebuilt before the model came; the new map asks again
            prefabs[model] = prefab;
            if (prefab == null) Debug.LogWarning($"SEA/AIR: the model {model} did not stream; it is not drawn, and the map plays on without it.");
            Rebuild();
        }

        void Rebuild()
        {
            CollectionResidencyMetrics.MarkDirty();
            if (map == null || map.Terrain == null) return;
            ClearVisuals();
            var ground = PlacementLayer.CampGround(map);
            var works = map.KitStats?.HarbourWorks ?? Array.Empty<SettlementKits.Placed>();
            var boats = new List<SeaCraft.Craft>(map.KitStats?.Crafts ?? Array.Empty<SeaCraft.Craft>());
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab)) continue;   // still streaming
                Vector2? at = null;
                if (prefab != null)
                {
                    at = w.Model == C54 ? LandSpot(ground) : w.Model == Spitfire ? EdgeSpot(map.Frame) : w.Model == Dakota ? DakotaCentre()
                        : SeaSpot(w, ground, works, boats);
                    if (at != null && w.Model != C54 && w.Model != Spitfire && w.Model != Dakota) boats.Add(new SeaCraft.Craft(SeaCraft.Kind.Steamer, at.Value, Heading, SizeOf(w.Model), w.Place));
                }
                if (at == null) { Missing.Add(w.Key); continue; }
                var go = Instantiate(prefab!, transform, false); go.name = w.Key;
                if (!originals.TryGetValue(w.Model, out var owned)) originals[w.Model] = owned = new List<GameObject>();
                owned.Add(go);
                if (w.Model == Spitfire) { Fly(go, prefab!, at.Value, ground); continue; }
                if (w.Model == Dakota) { FlyDakota(go, prefab!, at.Value, ground, Array.IndexOf(DakotaKeys, w.Key)); continue; }
                // The yaw goes on top of the prefab's own rotation: every Collection Far prefab's root carries the FBX axis
                // conversion (-90 degrees about X), and setting the yaw alone lays a model on its side (#979).
                go.transform.localRotation = prefab!.transform.localRotation;
                var b = Bounds(go);
                if (b.size.z > b.size.x) { go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * prefab.transform.localRotation; b = Bounds(go); }
                float edge = Mathf.Max(b.size.x, b.size.z);
                go.transform.localScale *= edge > 1e-3f ? SizeOf(w.Model) / edge : 1f;
                // A boat's waterline and the plane's wheels on the surface under the spot (the sea is the heightmap's 0 m).
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
                Placed.Add((w.Key, w.Model, pos));
                Keep(go, Bounds(go));
            }
            FlyAirDefence(ground);   // the S-199s and the anti-aircraft fire beside the Dakotas, on the second period's turns
            // The kit trees under the C-54 are left out at draw time, as under the unit figures (VegetationLayer.Hide, #249).
            var plane = Placed.Where(p => p.Model == C54).Select(p => (new Vector2(p.At.x, p.At.z), 0.5f * SizeOf(C54))).ToList();
            Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, plane);
            Debug.Log($"SEA/AIR: {Placed.Count} collection craft placed, {Missing.Count} not drawn");
        }

        /// <summary>DESIGN VALUE: the yaw (degrees, on top of the prefab's own rotation) that points the Spitfire model's nose
        /// east, read off its top-down capture.</summary>
        internal const float SpitfireNoseEastYaw = 90f;

        /// <summary>The Spitfire in flight round its loop about <paramref name="at"/> (<see cref="SpitfireLoop"/>), wingspan
        /// at its map size, UnitShowcase.AltitudeM (4,200 m) above the highest ground under the loop (UnitShowcase.LoopFloor),
        /// where the unit tokens' aircraft fly (#2014), banked into its turns, with its propeller turning, its shadow on the
        /// ground and a small Egyptian flag over its tail (owner's report, 2 November 2026: "the gyptian plane's propelor isn't moving and it
        /// doesnt look fliying, also it doesnt show its an egyptian unit"). It is drawn at the loop's start, nose east; the
        /// labels keep clear of the whole box it and its flag fly through.</summary>
        void Fly(GameObject go, GameObject prefab, Vector2 at, KitGround ground)
        {
            var level = Quaternion.Euler(0f, SpitfireNoseEastYaw, 0f) * prefab.transform.localRotation;
            go.transform.localRotation = level;
            var b = Bounds(go); float span = b.size.z;
            go.transform.localScale *= span > 1e-3f ? SizeOf(Spitfire) / span : 1f;
            float size = SizeOf(Spitfire);
            var centre = new Vector3(at.x, Statecraft.Game.Map.Units.UnitShowcase.LoopFloor(map!, new Vector3(at.x, 0f, at.y), SpitfireLoopHalfLengthM + size)
                + Statecraft.Game.Map.Units.UnitShowcase.AltitudeM, at.y);
            go.transform.position = centre; b = Bounds(go);
            // The soft disc (Shadow) is its shadow: a cast one would lie about 7.7 km off at the drawn height, a second shadow
            // (e21's AFTER captures of #2728, 5 November 2026).
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; Gloss(r); }
            // The shadow and the flag stand beside the aircraft in this layer, so they neither bank nor turn with it, and go
            // with it (its model's originals) when its dates end.
            var marks = new GameObject(go.name + SpitfireMarksSuffix);
            marks.transform.SetParent(transform, false);
            originals[Spitfire].Add(marks);
            var loop = go.AddComponent<SpitfireLoop>();
            var flag = Flag(marks.transform);
            // The aircraft's half box at level, nose east: half its length, its height and its span.
            var half = 0.5f * new Vector3(b.size.x, b.size.y, b.size.z);
            loop.Attach(Propeller(go), Shadow(marks.transform, spitfire: true), flag, map!.Terrain, 0.5f * ShadowShare * new Vector2(size, b.size.x), half, ground.SeaLevelY);
            // Edit mode and screenshots hold the loop's start, as the train does (RouteLayer.ModeFor).
            loop.Configure(centre, level, b.center - go.transform.position, !Application.isPlaying || VisualShots.Requested());
            var cam = Camera.main;
            if (flag != null && cam != null) Statecraft.Game.Map.Units.UnitFlagFacing.Face(flag, cam.transform.position);
            Placed.Add((go.name, Spitfire, centre));
            // At any heading and bank the aircraft lies within half its box's diagonal of its centre; the flag floats over its
            // tail, at most the steepest bank's box top over the centre (SpitfireLoop.TopOver), its cloth turning about its middle.
            float reach = 0.5f * b.size.magnitude + 1f, up = reach, round = reach;
            if (flag != null)
            {
                var f = flag.GetComponent<MeshFilter>().sharedMesh.bounds; float k = flag.lossyScale.y;
                up = Mathf.Max(up, SpitfireLoop.TopOver(half, Statecraft.Game.Map.Units.UnitShowcase.BankDegrees) + SpitfireLoop.FlagClearM + 2f * f.extents.y * k + 1f);
                round = Mathf.Max(round, half.x + new Vector2(f.extents.x, f.extents.z).magnitude * k + 1f);
            }
            var keep = new Bounds();
            keep.SetMinMax(centre - new Vector3(SpitfireLoopHalfLengthM + round, reach, SpitfireLoopHalfWidthM + round),
                centre + new Vector3(SpitfireLoopHalfLengthM + round, up, SpitfireLoopHalfWidthM + round));
            Keep(go, keep);
        }

        /// <summary>The turning propeller (owner's report: "the gyptian plane's propelor isn't moving"; the model is one fused
        /// mesh, so it cannot turn its own): a translucent disc of three blurred blades at the nose, a child of the aircraft's
        /// mesh (<see cref="PropRadiusShare"/>, <see cref="PropBehindNoseShare"/>), turned by SpitfireLoop about the mesh's
        /// y, the flight axis. The disc's renderer bounds are the square inside its circle, so at every angle its box lies
        /// inside the airframe's (a turning square's box never leaves the circle, and the circle lies inside the mesh's box):
        /// the aircraft's box does not change as it turns. The cost: the renderer's box is between 0.71 and 1 of the disc's reach,
        /// so at a screen edge the disc can be culled a frame before the airframe beside it.</summary>
        Transform Propeller(GameObject go)
        {
            var airframe = go.GetComponentInChildren<MeshFilter>();
            var mesh = airframe.sharedMesh.bounds;
            float radius = PropRadiusShare * mesh.size.x;
            var disc = new GameObject(SpitfirePropellerName);
            disc.transform.SetParent(airframe.transform, false);
            disc.transform.localPosition = new Vector3(mesh.center.x, mesh.min.y + PropBehindNoseShare * mesh.size.y, mesh.max.z - radius);
            disc.transform.localScale = Vector3.one * radius;
            disc.AddComponent<MeshFilter>().sharedMesh = spitfirePropMesh != null ? spitfirePropMesh : spitfirePropMesh = Own(SpitfirePropellerDisc());
            var r = disc.AddComponent<MeshRenderer>();
            r.sharedMaterial = Blur(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.localBounds = new Bounds(Vector3.zero, new Vector3(Mathf.Sqrt(2f), 2f * PropLayer, Mathf.Sqrt(2f)));
            return disc.transform;
        }

        /// <summary>Gives the Spitfire's renderer <paramref name="r"/> glossy copies of its materials (the streamed model's are
        /// fully matte): the same shader, maps and colours, with <see cref="Statecraft.Game.Map.Units.UnitShowcase.AircraftSmoothness"/>.
        /// One copy a source material for this layer, owned by it, so a redraw makes none. A copy whose source is gone (its
        /// model released under residency, then streamed again as a new material) is destroyed, so they do not pile up.</summary>
        void Gloss(Renderer r)
        {
            foreach (var dead in glossy.Keys.Where(k => k == null).ToList()) { DestroyNow(glossy[dead]); glossy.Remove(dead); }
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || !m.HasProperty(Statecraft.Game.Map.Units.UnitShowcase.SmoothnessProperty)) continue;
                if (!glossy.TryGetValue(m, out var g)) glossy[m] = g = Own(Statecraft.Game.Map.Units.UnitShowcase.Glossy(m));
                mats[i] = g;
            }
            r.sharedMaterials = mats;
        }

        /// <summary>The aircraft's soft shadow on the ground (owner's report: "it doesnt look fliying"), placed by SpitfireLoop
        /// where the map's sun casts it.</summary>
        Transform Shadow(Transform parent, bool spitfire = false)
        {
            var go = new GameObject(SpitfireShadowName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = spitfire ? spitfireShadowMesh != null ? spitfireShadowMesh : spitfireShadowMesh = Own(SpitfireShadowDisc())
                : shadowMesh != null ? shadowMesh : shadowMesh = Own(ShadowDisc());
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Blur(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>DESIGN VALUE: the Spitfire's flag over the tokens' flag size (UnitBadge.FigureHeightM x UnitShowcase.FigureScale
        /// a figure-height unit; the cloth 0.72 of it across): smaller, as the owner ruled on #2260 (2 November 2026, "Smaller
        /// flag, no pole"), and still a readable green flag with its white crescent at the opening zoom (picked from captures
        /// of 0.35, 0.5 and 0.65 at 90 and 30 km).</summary>
        public const float SpitfireFlagScale = 0.5f;

        /// <summary>Egypt's flag, as the Egyptian tokens fly it (owner's report: "it doesnt show its an egyptian unit"): the map
        /// marks a unit's side by its side's sourced flag (UnitPlinth.Build, UnitFlags), turned to the camera (UnitFlagFacing).
        /// Here, as the owner ruled on #2260 ("Smaller flag, no pole": "a small Egyptian flag pennant floating just above the
        /// tail (no pole through the plane)"), the cloth alone, centred on its own middle so it turns in place, at
        /// <see cref="SpitfireFlagScale"/> of the tokens' size; SpitfireLoop floats it over the tail. Null, with a warning, when
        /// the units' materials are missing: the aircraft flies on without it.</summary>
        Transform? Flag(Transform parent)
        {
            if (flagMesh == null)
            {
                if (flagMissing) return null;   // warned once for this layer
                var ring = Resources.Load<Material>(Statecraft.Game.Map.Units.UnitMaterials.RingResource);
                if (ring == null)
                {
                    Debug.LogWarning("SEA/AIR: the unit materials are missing (run UnitMaterialAssets.Create); the Spitfire flies without its flag.");
                    flagMissing = true;
                    return null;
                }
                flagCloth = Statecraft.Game.Map.Units.UnitFlags.ClothMaterials(Statecraft.Game.Map.Units.UnitFlags.Egypt, ring, m => Own(m));
                flagMesh = Statecraft.Game.Map.Units.UnitPlinth.Upload(Own(ClothOnly(Statecraft.Game.Map.Units.UnitFlags.Build(Statecraft.Game.Map.Units.UnitFlags.Egypt)!)));
            }
            var flag = Statecraft.Game.Map.Units.UnitShowcase.Piece(parent, SpitfireFlagName, flagMesh, flagCloth![0], Vector3.zero, Quaternion.identity,
                SpitfireFlagScale * Statecraft.Game.Map.Units.UnitBadge.FigureHeightM * Statecraft.Game.Map.Units.UnitShowcase.FigureScale);
            var r = flag.GetComponent<MeshRenderer>();
            r.sharedMaterials = flagCloth; r.shadowCastingMode = ShadowCastingMode.Off;
            flag.AddComponent<Statecraft.Game.Map.Units.UnitFlagFacing>();
            return flag.transform;
        }

        /// <summary>A flag built by UnitFlags (the pole in submesh 0, then one submesh a cloth colour) with the pole left out:
        /// a new mesh of the cloth's submeshes and only the vertices they use (so its bounds are the cloth's), moved so the
        /// cloth's middle is at the origin.</summary>
        static Mesh ClothOnly(Mesh flag)
        {
            var verts = flag.vertices; var uv = flag.uv; var normals = flag.normals;
            var keep = new Dictionary<int, int>();
            var parts = new List<int[]>();
            for (int k = 1; k < flag.subMeshCount; k++)
            {
                var tris = flag.GetTriangles(k);
                for (int i = 0; i < tris.Length; i++)
                {
                    if (!keep.TryGetValue(tris[i], out int to)) keep[tris[i]] = to = keep.Count;
                    tris[i] = to;
                }
                parts.Add(tris);
            }
            var at = new Vector3[keep.Count]; var uvs = new Vector2[keep.Count]; var ns = new Vector3[keep.Count];
            foreach (var (from, to) in keep) { at[to] = verts[from]; if (uv.Length > 0) uvs[to] = uv[from]; if (normals.Length > 0) ns[to] = normals[from]; }
            var cloth = new Bounds(at[0], Vector3.zero);
            foreach (var v in at) cloth.Encapsulate(v);
            for (int i = 0; i < at.Length; i++) at[i] -= cloth.center;
            var mesh = new Mesh { name = flag.name, vertices = at };
            if (uv.Length > 0) mesh.uv = uvs;
            if (normals.Length > 0) mesh.normals = ns;
            mesh.subMeshCount = parts.Count;
            for (int k = 0; k < parts.Count; k++) mesh.SetTriangles(parts[k], k);
            mesh.RecalculateBounds();
            DestroyMesh(flag);
            return mesh;
        }

        static void DestroyMesh(Mesh m) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
        static void DestroyNow(UnityEngine.Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        MapRuntimeAssets? assets;
        bool flagMissing;
        Material? blur; Material[]? flagCloth; Mesh? propMesh, spitfirePropMesh, shadowMesh, spitfireShadowMesh, flagMesh;
        readonly Dictionary<Material, Material> glossy = new Dictionary<Material, Material>();

        /// <summary>Takes <paramref name="o"/>, made once for this layer, to be destroyed with it.</summary>
        T Own<T>(T o) where T : UnityEngine.Object
        {
            if (assets == null) { assets = GetComponent<MapRuntimeAssets>(); if (assets == null) assets = gameObject.AddComponent<MapRuntimeAssets>(); }
            return assets.Own(o);
        }

        /// <summary>The propeller's and the shadow's material: alpha-blended, unlit, both faces, its colour the meshes' vertex
        /// colours (HexMaterials' shipped under-attack border template, so a player build keeps the shader's variant).</summary>
        Material Blur() => blur != null ? blur : blur = Own(Statecraft.HexTiles.HexMaterials.New(Statecraft.HexTiles.HexMaterials.AttackBorder));

        /// <summary>A flat disc of radius 1 in the XZ plane (normal +y), one copy at each of <paramref name="layers"/> (its y): a
        /// centre and rings of <paramref name="segments"/> vertices at <paramref name="rings"/>, coloured <paramref name="colour"/>
        /// with the alpha <paramref name="alpha"/> gives at (radius, angle in degrees from +x towards +z).</summary>
        internal static Mesh Disc(string name, int segments, float[] rings, float[] layers, Color colour, Func<float, float, float> alpha)
        {
            int per = 1 + segments * rings.Length, perTris = 3 * segments + 6 * segments * (rings.Length - 1);
            var verts = new Vector3[per * layers.Length]; var colours = new Color32[verts.Length]; var tris = new int[perTris * layers.Length];
            Color32 Paint(float a) { var c = colour; c.a = Mathf.Clamp01(a); return c; }
            int t = 0;
            for (int l = 0; l < layers.Length; l++)
            {
                int o = l * per;
                verts[o] = new Vector3(0f, layers[l], 0f); colours[o] = Paint(alpha(0f, 0f));
                for (int k = 0; k < rings.Length; k++)
                    for (int i = 0; i < segments; i++)
                    {
                        float deg = 360f * i / segments, rad = deg * Mathf.Deg2Rad;
                        verts[o + 1 + k * segments + i] = new Vector3(Mathf.Cos(rad) * rings[k], layers[l], Mathf.Sin(rad) * rings[k]);
                        colours[o + 1 + k * segments + i] = Paint(alpha(rings[k], deg));
                    }
                for (int i = 0; i < segments; i++) { tris[t++] = o; tris[t++] = o + 1 + (i + 1) % segments; tris[t++] = o + 1 + i; }
                for (int k = 0; k + 1 < rings.Length; k++)
                    for (int i = 0; i < segments; i++)
                    {
                        int a = o + 1 + k * segments + i, b = o + 1 + k * segments + (i + 1) % segments, c = a + segments, d = b + segments;
                        tris[t++] = a; tris[t++] = b; tris[t++] = c; tris[t++] = c; tris[t++] = b; tris[t++] = d;
                    }
            }
            var mesh = new Mesh { name = name, vertices = verts, colors32 = colours, triangles = tris };
            mesh.RecalculateBounds();
            return Statecraft.Game.Map.Units.UnitPlinth.Upload(mesh);
        }

        /// <summary>DESIGN VALUES: the Dakotas' propeller blur, near-black (the units' propeller paint): three blades, each a dark
        /// leading edge whose smear fades behind it (the way the loop turns it), over a half-opaque whole disc, clear at the
        /// spinner and fading out at the rim; two layers (<see cref="PropLayer"/>).</summary>
        static Mesh PropellerDisc() => Disc("SpitfirePropellerBlur", 72, new[] { 0.18f, 0.4f, 0.7f, 0.9f, 1f }, new[] { -PropLayer, PropLayer }, new Color(0.06f, 0.06f, 0.06f), (r, deg) =>
            r >= 1f ? 0f : Mathf.Min(1f, r / 0.4f) * (0.45f + 0.45f * Mathf.Exp(-Mathf.Repeat(-deg, 120f) / 35f)));

        /// <summary>DESIGN VALUES (agent-chosen, coordinator-approved plan, 5 November 2026; checked in captures): the Spitfire's
        /// propeller blur, light (a turning propeller reads as a pale haze against the ground): three blades, each a brighter
        /// streak whose smear fades behind it (the way SpitfireLoop turns it), over a faint whole disc that softens the fused
        /// model's still blades, clear at the spinner and fading out at the rim; two layers (<see cref="PropLayer"/>).</summary>
        static Mesh SpitfirePropellerDisc() => Disc("SpitfirePropellerBlur", 72, new[] { 0.18f, 0.4f, 0.7f, 0.9f, 1f }, new[] { -PropLayer, PropLayer }, PropBlurColour,
            (r, deg) => r >= 1f ? 0f : Mathf.Min(1f, r / 0.4f) * PropBlurAlpha(deg, 120f, 0f));

        /// <summary>DESIGN VALUE: the light propeller blurs' colour, a pale warm grey (the Spitfire's and the 101 Squadron
        /// aircraft's, UnitShowcase.PropellerBlur).</summary>
        internal static readonly Color PropBlurColour = new Color(0.82f, 0.81f, 0.78f);

        /// <summary>DESIGN VALUES: a light blur's opacity at <paramref name="deg"/> round the disc for blades every
        /// <paramref name="spacing"/> degrees whose leading edges are at <paramref name="edge"/>: a faint 0.22 everywhere and up
        /// to 0.55 at a blade, fading behind it (the disc's angle grows as both loops turn their propellers).</summary>
        internal static float PropBlurAlpha(float deg, float spacing, float edge) =>
            0.22f + 0.33f * Mathf.Exp(-Mathf.Repeat(edge - deg, spacing) / (0.25f * spacing));

        /// <summary>DESIGN VALUES: the Dakotas' shadow, a dark warm grey (the map's shadows are warm under its sun), 0.42 opaque at its
        /// middle and fading to nothing at its rim, (1 - r²)^1.5.</summary>
        internal static Mesh ShadowDisc() => Disc("DakotaShadow", 48, new[] { 0.3f, 0.55f, 0.75f, 0.9f, 1f }, new[] { 0f }, new Color(0.10f, 0.08f, 0.06f), (r, _) =>
            0.42f * Mathf.Pow(1f - r * r, 1.5f));

        /// <summary>DESIGN VALUES (agent-chosen, from e21's AFTER captures of #2728, 5 November 2026: the near disc at 0.42 was
        /// too faint over sand and sea, and 0.6 over the brown hills in v2): the Spitfires' shadow, as <see cref="ShadowDisc"/>
        /// (the Dakotas', unchanged) but 0.75 opaque at its middle, still fading to nothing at its rim; it is now their only
        /// shadow (they cast none, Fly).</summary>
        internal static Mesh SpitfireShadowDisc() => Disc("SpitfireShadow", 48, new[] { 0.3f, 0.55f, 0.75f, 0.9f, 1f }, new[] { 0f }, new Color(0.10f, 0.08f, 0.06f), (r, _) =>
            0.75f * Mathf.Pow(1f - r * r, 1.5f));

        /// <summary>The ground the other 1948 Collection layers hold (GameRoot shows them first), for the C-54 to keep off, as
        /// SpecialBuildingModels keeps off the people and the wildlife: the civilians (#666), the wildlife (#670), the farm
        /// scenes drawn (#667) and the fort, barracks and factory drawn (#707). Show compares it, so the plane moves when they
        /// do.</summary>
        List<(Vector2 centre, float radius)> Taken()
        {
            var list = new List<(Vector2, float)>();
            if (map == null || map.Terrain == null) return list;
            var t = map.Terrain.transform;
            var people = t.Find(CivilianPeople.Name)?.GetComponent<CivilianPeople>();
            if (people != null) foreach (var p in people.Wanted) list.Add((new Vector2(p.At.x, p.At.z), CivilianPeople.FigureClearanceM));
            wildlife ??= WildlifeLayer.Wants(map);   // fixed for a map: no date in it
            foreach (var a in wildlife) list.Add((a.At, 0.5f * WildlifeLayer.SizeM));
            var rural = t.Find(RuralLife.Name)?.GetComponent<RuralLife>();
            if (rural != null) foreach (var p in rural.Placed) list.Add((new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)));
            var special = t.Find(SpecialBuildingModels.Name)?.GetComponent<SpecialBuildingModels>();
            if (special != null) foreach (var p in special.Placed) list.Add((new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)));
            return list;
        }

        /// <summary>The C-54's spot: the first free ground by the airport (TownLandmarks.Spot's rings), clear of the water, the
        /// kits' footprints, the other Collection layers (<see cref="Taken"/>), the routes, the units and the player's placed
        /// hexes.</summary>
        Vector2? LandSpot(KitGround ground)
        {
            var w = map!.Frame.ToWorld(LodLon, LodLat, 0); var airport = new Vector2(w.x, w.z);
            float r = 0.5f * SizeOf(C54), hexHalf = 500f * map.Hexes.hex_size_km;
            var discs = map.KitFootprints.Select(f => (f.centre, f.radius)).Concat(taken).ToList();
            foreach (var (q, rr) in placements)
            {
                var hex = content?.HexAt(q, rr);
                if (hex == null) continue;
                var h = map.Frame.ToWorld(hex.Lon, hex.Lat, 0); discs.Add((new Vector2(h.x, h.z), hexHalf));
            }
            bool Free(Vector2 p, float rad)
            {
                if (!TownLandmarks.Clear(p, rad, discs) || !ground.Inside(p.x - rad, p.y - rad) || !ground.Inside(p.x + rad, p.y + rad)) return false;
                for (int k = 0; k < 9; k++)
                {
                    var q = k == 0 ? p : p + new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)) * rad * 0.8f;
                    if (ground.Water(q.x, q.y)) return false;
                }
                return !ground.OnRoute(p, rad) && !ground.OnToken(p, rad) && !ground.OnToken(p, rad, date);
            }
            return Free(airport, r) ? airport : TownLandmarks.Spot(airport, 0f, r, Free);
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Choose;
            if (residencyView != null) { residencyDirty = false; residencyView.Clear(); residency?.Replace(Array.Empty<(string, CollectionDetail)>()); }
        }
        internal void Choose(ScriptableRenderContext context, Camera camera)
        { if (residencyView != null) ObserveResidency(camera); }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }
    }
}
