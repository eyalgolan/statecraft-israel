#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>The steam train in normal play (issue #668; maintainer decision on #35, 16 October 2026: "Yes, normal
    /// play", on the Jaffa–Jerusalem line, only from the sourced date regular service resumed). The stretch and the date
    /// come from the content: the route's service (routes_1948.yaml, through the runtime file; RouteService), from 2 March
    /// 1950 on the stretches that run: Lod to Jerusalem (Israel Railways, 1998), and Tel Aviv South to Lod only once a
    /// person signs that stretch off (it rests on Wikipedia alone). Before that date there is no train and nothing is
    /// fetched. The model is MATKOT's steam locomotive and tender, H09 of The 1948 Collection (#507,
    /// #680), streamed through
    /// <see cref="CollectionStream"/>: asked for only when the game date first reaches the service, so never for the first
    /// frame (the game starts in May 1948, and the stream serves nothing before the frame after a request). The strategic
    /// zoom's mesh (CollectionDetail.Far) is the one drawn at every zoom: a vehicle has no idle clip, and one model of at
    /// most 4,500 triangles costs nothing worth a swap.
    /// It fails open: when the model does not stream (no bundle, a damaged one, no prefab), one warning, no train, and the
    /// map and the game run on. The train runs on the real clock, as the showcase does, whatever the turn: it is ambience,
    /// not a timetable, and the game has no pause that stops the clock. Unity's culling drops it off screen.</summary>
    public sealed class ServiceTrain : MonoBehaviour
    {
        public const string Name = "ServiceTrain";
        /// <summary>H09 in The 1948 Collection (CollectionCatalog.Vehicles).</summary>
        public const string ModelId = "vehicle-steam-locomotive-1948-v2";
        /// <summary>The yaw (degrees) that turns the model's smokebox to its car's +Z, the way it runs (measured on the far
        /// prefab: ServiceTrainTests.TheLocomotiveLeadsWithItsSmokebox).</summary>
        public const float ModelYaw = 0f;
        /// <summary>The train's length as a share of the showcase's locomotive and tender (RouteTrain.Scale): 0.4, about 816 m
        /// on the map (coordinator, 16 October 2026). At the showcase's own size (about 2 km by 745 m) one rigid body cannot
        /// follow the Sorek valley's bends (issue #668, measured along Lod–Jerusalem).</summary>
        public const float ModelSize = 0.4f;
        /// <summary>The train's articulated cars, MATKOT's locomotive and three coaches, each a quarter of its length (about
        /// 204 m): the owner's ruling for #668 (16 October 2026, "ride the track, cut walls"), measured against one rigid car.</summary>
        public const int Cars = 4;
        /// <summary>Each car's width: 0.9 of the rail ribbon's narrowest drawn width (RouteLayer.MinWidthShare of
        /// RouteLayer.RailWidthM, 170 m on the Sorek's gorge floor), about 153 m, so a car fits the track it rides.</summary>
        public const float CarWidthM = 0.9f * RouteLayer.MinWidthShare * RouteLayer.RailWidthM;

        /// <summary>How the train asks for its model: the game's stream. A test seam (internal; the tests see it through
        /// InternalsVisibleTo), so the tests can answer with a prefab or with nothing.</summary>
        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        List<Vector2> line = new List<Vector2>();
        MapRuntimeAssets? assets;
        TerrainData? data;
        Vector3 origin, startAt, endAt;
        DateTime shownAt;
        bool requested, failed;

        /// <summary>The first day the train runs.</summary>
        public DateTime From { get; private set; }
        /// <summary>The train, once its model has streamed in; null before, and for good when it did not.</summary>
        public RouteTrain? Train { get; private set; }
        public bool Requested => requested;
        public bool Failed => failed;

        /// <summary>Adds the (empty) service train under <paramref name="parent"/> for <paramref name="route"/>'s service,
        /// shown as at <paramref name="date"/>.</summary>
        public static ServiceTrain Create(Transform parent, MapRuntimeAssets assets, GeoFrame frame, RoutePath route, TerrainData data,
            Vector3 origin, DateTime date)
        {
            var s = route.Service ?? throw new ArgumentException($"'{route.Id}' has no service", nameof(route));
            var go = new GameObject(Name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<ServiceTrain>();
            t.line = RouteLayer.World(frame, route);
            t.assets = assets;
            t.data = data; t.origin = origin;
            t.startAt = frame.ToWorld(s.StartLon, s.StartLat, 0);
            t.endAt = frame.ToWorld(s.EndLon, s.EndLat, 0);
            t.From = RoutePaths.Bound(s.From);
            t.ShowAt(date);
            return t;
        }

        /// <summary>Runs the train from its date on and hides it before (a loaded earlier save). The first call at or after
        /// the date asks the stream for the model, once; the rest only switch the train on or off.</summary>
        public void ShowAt(DateTime date)
        {
            shownAt = date;
            bool runs = date >= From;
            if (Train != null) Train.gameObject.SetActive(runs);
            if (!runs || requested) return;
            requested = true;
            Request(ModelId, CollectionDetail.Far, OnModel);
        }

        void OnModel(GameObject? prefab)
        {
            if (this == null) return;   // the map was rebuilt before the model came; the new map asks again
            if (prefab == null || data == null || assets == null)
            {
                failed = true;
                Debug.LogWarning($"TRAIN: the locomotive ({ModelId}) did not stream; no train runs on {RouteLayer.TrainRoute}, and the map plays on without it.");
                return;
            }
            Train = RouteTrain.SpawnModel(transform, assets, prefab, line, data, origin, startAt, endAt, ModelYaw, ModelSize, Cars, CarWidthM);
            Train.gameObject.SetActive(shownAt >= From);
            Debug.Log($"TRAIN: the steam train runs on {RouteLayer.TrainRoute} from {From:yyyy-MM-dd} ({ModelId}, {Train.Length:0} m)");
        }
    }
}
