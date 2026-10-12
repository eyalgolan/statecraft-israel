#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Units;
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>The steam train of the promo (promo/final/03 and the video's 0:21 close-up): a tank-and-tender locomotive and
    /// four coaches, code-built as painted miniatures (MiniatureMesh, the units' palette and material), at the settlements'
    /// symbolic scale (<see cref="Scale"/>), running on the railway ribbon. It shuttles along its route between the line's
    /// start and the turn point (RouteLayer: Jaffa to Lydda), the locomotive leading each way, with a stop at each end.
    /// A still train (screenshots, editor renders) stands at <see cref="StillAt"/>, heading inland, every run alike. Cheap:
    /// six transforms set per frame from a precomputed table; no physics, no allocation after Spawn. A showcase for the
    /// promo frames and `?train` / `?units`, never normal play (RouteLayer.ModeFor, review I1): the run is ambience, not a
    /// sourced timetable.</summary>
    public sealed class RouteTrain : MonoBehaviour
    {
        /// <summary>Times real size along the line, measured on the promo's gameplay frame: its six-car train is about 250 px
        /// long at 1920 px (about 35 m/px there), about 8.8 km, so about 80 times this 110 m consist (the settlements' blocks
        /// are about 60x, owner ruling 2026-10-11). Across and up it is <see cref="Chunk"/> times more, as the promo's
        /// miniature train is stockier than a real one (a first checkpoint at a uniform 65x drew it a thin dark line).</summary>
        public const float Scale = 80f, Chunk = 1.5f;
        /// <summary>Symbolic speed along the line (m/s of map), so the train moves visibly at the gameplay zoom (about 9 px a
        /// second); and the stop at each end (s).</summary>
        public const float SpeedMps = 300f, DwellSeconds = 6f;
        /// <summary>Where a still train's middle stands: on the line by Mikve Israel, east of Jaffa and Tel Aviv, inside the
        /// gameplay03 frame, as in the promo.</summary>
        public static readonly (double lon, double lat) StillAt = (34.83, 32.01);
        /// <summary>Height of the wheels above the ribbon's line, so they sit on the ballast, not in it.</summary>
        public const float RideM = 4f;
        const float TableStepM = 50f;

        readonly List<Transform> cars = new List<Transform>();
        readonly List<float> carOffset = new List<float>(), carHalf = new List<float>();   // from the head, metres of map
        float[] tableS = Array.Empty<float>();
        Vector3[] tableP = Array.Empty<Vector3>();
        float startS, endS, length;
        float? still;

        /// <summary>Distance along the line of the head (front of the locomotive) at time <paramref name="t"/>, and the
        /// direction (+1 towards the turn point, -1 back).</summary>
        public (float head, int dir) HeadAt(float t)
        {
            float run = Mathf.Max(endS - startS - length, 0f);
            float leg = run / SpeedMps + DwellSeconds, period = 2f * leg;
            float x = Mathf.Repeat(t, period);
            if (x < leg)
            {
                float moved = Mathf.Min(x * SpeedMps, run);
                return (startS + length + moved, +1);
            }
            float back = Mathf.Min((x - leg) * SpeedMps, run);
            return (endS - length - back, -1);
        }

        public float? StillHead => still;
        public IReadOnlyList<Transform> Cars => cars;
        public float Length => length;
        public (float start, float end) Stretch => (startS, endS);

        void Update()
        {
            if (still.HasValue) return;
            var (head, dir) = HeadAt(Time.time);
            Place(head, dir);
        }

        /// <summary>Places every car with the head at <paramref name="head"/> metres along the line, running in direction
        /// <paramref name="dir"/>: each car's centre on the line, its nose and tail on the line too, so it follows bends and
        /// grades.</summary>
        public void Place(float head, int dir)
        {
            for (int k = 0; k < cars.Count; k++)
            {
                float c = head - dir * carOffset[k];
                var front = At(c + dir * carHalf[k]); var back = At(c - dir * carHalf[k]);
                var mid = At(c);
                var fwd = front - back;
                cars[k].SetPositionAndRotation(mid, Quaternion.LookRotation(fwd.sqrMagnitude > 1e-6f ? fwd : Vector3.forward, Vector3.up));
            }
        }

        /// <summary>The point of the line (on the ribbon, plus the ride height) at distance s, clamped to the line.</summary>
        public Vector3 At(float s)
        {
            if (tableS.Length == 0) return Vector3.zero;
            if (s <= 0f) return tableP[0];
            if (s >= tableS[tableS.Length - 1]) return tableP[tableP.Length - 1];
            int i = Mathf.Min((int)(s / TableStepM), tableS.Length - 2);
            while (i > 0 && tableS[i] > s) i--;
            while (i + 1 < tableS.Length - 1 && tableS[i + 1] < s) i++;
            float f = (s - tableS[i]) / Mathf.Max(tableS[i + 1] - tableS[i], 1e-3f);
            return Vector3.Lerp(tableP[i], tableP[i + 1], f);
        }

        /// <summary>Builds the train under <paramref name="parent"/> on <paramref name="line"/> (world x, z), its meshes,
        /// palette and material owned by <paramref name="assets"/>. It runs from the line's start to the point nearest
        /// <paramref name="turnAt"/>; a still train's middle stands at the point nearest <paramref name="stillAt"/> (both
        /// world points). Returns the train's root.</summary>
        public static GameObject Spawn(Transform parent, MapRuntimeAssets assets, IReadOnlyList<Vector2> line, TerrainData data,
            Vector3 origin, Vector3 turnAt, Vector3 stillAt, bool stillTrain)
        {
            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterialAssets.Create)");
            var palette = assets.Own(UnitPalette.MakeTexture(readable: false));
            var paint = assets.Own(new Material(template) { name = "Train (palette)" });
            paint.SetTexture("_BaseMap", palette);

            var go = new GameObject("Train");
            go.transform.SetParent(parent, false);
            var train = go.AddComponent<RouteTrain>();
            var (pts, along) = RouteLayer.Resample(line, TableStepM);
            train.tableS = along.ToArray();
            train.tableP = new Vector3[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                // The ribbon's own height (its grade, the same rule, review M4) plus the ride height.
                var dir = (pts[Math.Min(pts.Count - 1, i + 1)] - pts[Math.Max(0, i - 1)]).normalized;
                train.tableP[i] = new Vector3(pts[i].x, RouteLayer.GradeHeight(data, origin, pts[i], dir) + RideM, pts[i].y);
            }
            train.startS = 0f;
            train.endS = train.Nearest(new Vector2(turnAt.x, turnAt.z));

            float offset = 0f;
            foreach (var (name, mesh, lengthM) in TrainModels.Consist())
            {
                assets.Own(mesh); mesh.UploadMeshData(true);
                var car = new GameObject(name);
                car.transform.SetParent(go.transform, false);
                car.transform.localScale = new Vector3(Scale * Chunk, Scale * Chunk, Scale);
                car.AddComponent<MeshFilter>().sharedMesh = mesh;
                car.AddComponent<MeshRenderer>().sharedMaterial = paint;
                float half = lengthM * Scale / 2f;
                train.cars.Add(car.transform); train.carOffset.Add(offset + half); train.carHalf.Add(half);
                offset += 2f * half + TrainModels.GapM * Scale;
            }
            train.length = offset - TrainModels.GapM * Scale;

            float stillMiddle = train.Nearest(new Vector2(stillAt.x, stillAt.z));
            float stillHead = Mathf.Clamp(stillMiddle + train.length / 2f, train.startS + train.length, train.endS);
            train.still = stillTrain ? stillHead : (float?)null;
            if (stillTrain) train.Place(stillHead, +1); else { var (h, d) = train.HeadAt(0f); train.Place(h, d); }
            return go;
        }

        /// <summary>Distance along the line of its point nearest <paramref name="p"/> (to the table's resolution).</summary>
        public float Nearest(Vector2 p)
        {
            float best = float.MaxValue, s = 0f;
            for (int i = 0; i < tableP.Length; i++)
            {
                float d = (new Vector2(tableP[i].x, tableP[i].z) - p).sqrMagnitude;
                if (d < best) { best = d; s = tableS[i]; }
            }
            return s;
        }
    }
}
