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
    /// <item>the Egyptian Spitfire in flight near the western board edge (30 km in) toward el-Arish, from 15 May 1948 to 2 January 1949
    /// (<see cref="SpitfireFrom"/>, <see cref="SpitfireTo"/>, #1932), never on a hex or an airfield.</item>
    /// </list>
    /// The boats lie on the open sea past the surf, where SeaCraft's own boats sail (SeaCraft.OffshoreNearM to OffshoreM out
    /// from the same shore points), each hull clear of the harbour works, of SeaCraft's boats and of each other
    /// (SeaCraft.Clear). Every craft is at the one world scale of the map's ambient models (<see cref="SizeOf"/>: 24x real,
    /// set by the steam train; #1030, #1033), except the Spitfire at the unit tokens'
    /// aircraft scale (420x, #2014). The C-54 stands on the nearest free ground by the airport, clear of the kits,
    /// routes, units and the player's placements, with the kit trees under it left out (VegetationLayer.Hide, as under the
    /// unit figures). Every craft draws at every zoom, as SeaCraft's boats do. Each model is the Far mesh, asked of
    /// <see cref="CollectionStream"/> when first wanted, so nothing is fetched before the first frame. It fails open: a model
    /// that does not stream logs one warning and is not drawn; the map plays on.</summary>
    [ExecuteAlways]
    public sealed class SeaAirCollection : MonoBehaviour
    {
        public const string Name = "SeaAirCollection";
        public const string Altalena = "vehicle-altalena-lst-1948-v2wl";
        public const string FishingBoat = "vehicle-fishing-boat-1948-v2wl";
        public const string Steamer = "vehicle-immigrant-steamer-1948-v2wl";
        public const string C54 = "figure-vehicle-c54-skymaster-1948-v2";
        public const string Spitfire = CollectionCatalog.EgyptianSpitfire;

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
            Altalena => 328f * FootM, C54 => 35.81f, Steamer => 80f, FishingBoat => 10f, Spitfire => 11.23f,
            _ => throw new ArgumentException($"'{model}' is not one of #669's models", nameof(model)),
        };

        /// <summary>The model's long side on the map (m): its real size at the one world scale of every Tal Heuberger model that is not
        /// a game piece (SettlementKits.ModelScale, 24x: an 8.5 m house as long as the steam train's 204 m locomotive car; the
        /// owner, 18 October 2026, #1033: "Train scale for all ambient life"). The Altalena is 2.4 km, the C-54 860 m across. The one exception is the
        /// Spitfire, drawn at the unit tokens' aircraft scale (UnitShowcase.AircraftScale, 420x: 4.7 km across), so it reads at strategic zoom (owner ruling, #2014).</summary>
        public static float SizeOf(string model) =>
            RealLengthM(model) * (model == Spitfire ? Statecraft.Game.Map.Units.UnitShowcase.AircraftScale : SettlementKits.ModelScale);

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
        IEnumerable<(string key, Vector3 at, float radius)> Anchors()
        {
            if (map == null) yield break;
            var edge = EdgeSpot(map.Frame);   // the Spitfire's anchor is on the ground under it; only its distance counts
            foreach (var w in wants)
            {
                var c = SeaCraft.Coasts.FirstOrDefault(x => x.place == w.Place);
                yield return (w.Key, w.Model == C54 ? map.Frame.ToWorld(LodLon, LodLat, 0) : w.Model == Spitfire ? new Vector3(edge.x, 0, edge.y)
                    : map.Frame.ToWorld(c.lon, c.lat, 0), SizeOf(w.Model));
            }
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
                    removed.Add(go);
                    if (Application.isPlaying) { if (gameObject.activeInHierarchy) go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
                }
                originals.Remove(key.id); Placed.RemoveAll(p => p.Model == key.id);
            }
            keepAll = originals.Values.SelectMany(list => list).Where(go => go != null)
                .Select(go => new[] { Bounds(go) }).ToList();
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
            Placed.Clear(); Missing.Clear(); keepAll = new List<Bounds[]>();
            originals.Clear();
            if (map != null) Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>());
        }
        bool residencyDirty;
        void ObserveResidency(Camera camera)
        {
            var before = residencyView!.Keys;
            residencyView.Observe(camera, Anchors(), Time.frameCount);
            if (!before.SetEquals(residencyView.Keys))
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

        /// <summary>The craft drawn (key, model, spot at the water's or ground's height; the Spitfire's centre in the air): a test aid.</summary>
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        /// <summary>The craft wanted but not drawn: the model did not stream, or no spot was clear (a test aid).</summary>
        public readonly List<string> Missing = new List<string>();

        /// <summary>Every drawn craft's world box, for the map labels to keep clear of, as they do of the unit tokens
        /// (MapLabelsPresenter's keepClear, issue #197). A new list after every rebuild and the same list otherwise, so the
        /// labels re-read it only when it changes.</summary>
        public IReadOnlyList<Bounds[]> KeepClear => !isActiveAndEnabled ? (IReadOnlyList<Bounds[]>)Array.Empty<Bounds[]>() : keepAll;
        List<Bounds[]> keepAll = new List<Bounds[]>();

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
            var others = next.Any(w => w.Model == C54) ? Taken() : new List<(Vector2, float)>();
            if (next.SequenceEqual(wants) && placed.SequenceEqual(placements) && others.SequenceEqual(taken)) return;
            wants = next; placements = placed; taken = others;
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
                    at = w.Model == C54 ? LandSpot(ground) : w.Model == Spitfire ? EdgeSpot(map.Frame) : SeaSpot(w, ground, works, boats);
                    if (at != null && w.Model != C54 && w.Model != Spitfire) boats.Add(new SeaCraft.Craft(SeaCraft.Kind.Steamer, at.Value, Heading, SizeOf(w.Model), w.Place));
                }
                if (at == null) { Missing.Add(w.Key); continue; }
                var go = Instantiate(prefab!, transform, false); go.name = w.Key;
                if (!originals.TryGetValue(w.Model, out var owned)) originals[w.Model] = owned = new List<GameObject>();
                owned.Add(go);
                if (w.Model == Spitfire) { Fly(go, prefab!, at.Value); continue; }
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
                keepAll.Add(new[] { Bounds(go) });
            }
            // The kit trees under the C-54 are left out at draw time, as under the unit figures (VegetationLayer.Hide, #249).
            var plane = Placed.Where(p => p.Model == C54).Select(p => (new Vector2(p.At.x, p.At.z), 0.5f * SizeOf(C54))).ToList();
            Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, plane);
            Debug.Log($"SEA/AIR: {Placed.Count} collection craft placed, {Missing.Count} not drawn");
        }

        /// <summary>DESIGN VALUE: the yaw (degrees, on top of the prefab's own rotation) that points the Spitfire model's nose
        /// east, read off its top-down capture.</summary>
        internal const float SpitfireNoseEastYaw = 90f;

        /// <summary>The Spitfire in level flight over <paramref name="at"/>: nose east, wingspan at its map size,
        /// UnitShowcase.AltitudeM (4,200 m) above the highest ground under it (UnitShowcase.LoopFloor), where the unit tokens'
        /// aircraft fly (#2014).</summary>
        void Fly(GameObject go, GameObject prefab, Vector2 at)
        {
            go.transform.localRotation = Quaternion.Euler(0f, SpitfireNoseEastYaw, 0f) * prefab.transform.localRotation;
            var b = Bounds(go); float span = b.size.z;
            go.transform.localScale *= span > 1e-3f ? SizeOf(Spitfire) / span : 1f;
            float size = SizeOf(Spitfire);
            var pos = new Vector3(at.x, Statecraft.Game.Map.Units.UnitShowcase.LoopFloor(map!, new Vector3(at.x, 0f, at.y), size) + Statecraft.Game.Map.Units.UnitShowcase.AltitudeM, at.y);
            go.transform.position = pos; b = Bounds(go);
            go.transform.position += pos - b.center;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
            Placed.Add((go.name, Spitfire, pos));
            keepAll.Add(new[] { Bounds(go) });
        }

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
