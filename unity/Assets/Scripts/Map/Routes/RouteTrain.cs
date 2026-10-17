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
    /// promo frames and `?train` / `?units` (RouteLayer.ModeFor, review I1): the run is ambience, not a sourced timetable.
    /// Normal play's train is <see cref="SpawnModel"/>'s (issue #668, <see cref="ServiceTrain"/>): MATKOT's streamed
    /// locomotive and tender leading three of this showcase's coaches, four articulated cars on the stretch and from the
    /// date the content gives, each riding the line (owner, 16 October 2026: "ride the track, cut walls";
    /// <see cref="MaxLiftM"/>).</summary>
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
        // Normal play's model train only (SpawnModel): its body is kept clear of the ground (BodyLift).
        bool clearGround;
        float carHalfWidth;
        TerrainData? terrain;
        Vector3 terrainOrigin;

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
        /// <summary>Each car's middle, metres behind the head along the line (Place puts car k at head - dir * offset).</summary>
        public IReadOnlyList<float> CarOffsets => carOffset;
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
                var axis = fwd.sqrMagnitude > 1e-6f ? fwd.normalized : Vector3.forward;
                if (clearGround) mid.y += Mathf.Min(BodyLift(mid, axis, carHalf[k]), MaxLiftM);
                cars[k].SetPositionAndRotation(mid, Quaternion.LookRotation(axis, Vector3.up));
            }
        }

        /// <summary>The most (m) a service-train car is raised off the line to clear the ground under it: 5 m. The owner's
        /// ruling for #668 (16 October 2026: "ride the track, cut walls"): every car rides the line, and where the Sorek
        /// valley's walls rise under a car's sides on the tightest bends, the sides pass into them, as the rail ribbon's
        /// own edges do in a cutting (RouteLayer.RiseToleranceM), instead of the car floating over them.</summary>
        public const float MaxLiftM = 5f;

        /// <summary>Samples along a model car's body (each side of its middle) and across it (each side of its centre line)
        /// where <see cref="BodyLift"/> reads the ground.</summary>
        public const int LiftSamplesAlong = 20, LiftSamplesAcross = 2;

        /// <summary>How far (m) to raise a car whose middle is at <paramref name="mid"/> (on the line, at the ride height) and
        /// whose straight body runs along <paramref name="dir"/> for <paramref name="half"/> each way, so its base stays
        /// <see cref="RideM"/> above the ground everywhere under it: the line's own points carry that height, but a rigid
        /// car cuts across a bend or a rise of the Sorek valley's grade (issue #668). 0 where the ground stays under it.
        /// Place raises a car by at most <see cref="MaxLiftM"/> of it. 205 terrain reads (41 along by 5 across) per car per
        /// frame.</summary>
        public float BodyLift(Vector3 mid, Vector3 dir, float half)
        {
            if (terrain == null) return 0f;
            var right = Vector3.Cross(Vector3.up, dir); right.y = 0f;
            right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
            float lift = 0f;
            for (int i = -LiftSamplesAlong; i <= LiftSamplesAlong; i++)
                for (int j = -LiftSamplesAcross; j <= LiftSamplesAcross; j++)
                {
                    var p = mid + dir * (half * i / LiftSamplesAlong) + right * (carHalfWidth * j / Mathf.Max(LiftSamplesAcross, 1));
                    float need = RouteLayer.Ground(terrain, terrainOrigin, new Vector2(p.x, p.z)) + RideM - p.y;
                    if (need > lift) lift = need;
                }
            return lift;
        }

        /// <summary>The half-width (m of map) of a model car's base, which <see cref="BodyLift"/> keeps clear.</summary>
        public float CarHalfWidth => carHalfWidth;

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

            var train = NewTrain(parent, line, data, origin);
            var go = train.gameObject;
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

        /// <summary>An empty train under <paramref name="parent"/> with its line's table: the ribbon's own height (its grade,
        /// the same rule, review M4) plus the ride height, every <see cref="TableStepM"/>.</summary>
        static RouteTrain NewTrain(Transform parent, IReadOnlyList<Vector2> line, TerrainData data, Vector3 origin)
        {
            var go = new GameObject("Train");
            go.transform.SetParent(parent, false);
            var train = go.AddComponent<RouteTrain>();
            var (pts, along) = RouteLayer.Resample(line, TableStepM);
            train.tableS = along.ToArray();
            train.tableP = new Vector3[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var dir = (pts[Math.Min(pts.Count - 1, i + 1)] - pts[Math.Max(0, i - 1)]).normalized;
                train.tableP[i] = new Vector3(pts[i].x, RouteLayer.GradeHeight(data, origin, pts[i], dir) + RideM, pts[i].y);
            }
            return train;
        }

        /// <summary>The length (real metres) the streamed locomotive and tender stand for: the showcase's locomotive, the
        /// gap and its tender (TrainModels), so both trains are one size on the map.</summary>
        public const float ModelRealLengthM = TrainModels.LocoLengthM + TrainModels.GapM + TrainModels.TenderLengthM;

        /// <summary>Normal play's train (issue #668): <paramref name="cars"/> articulated cars of equal length, together
        /// <see cref="ModelRealLengthM"/> at <see cref="Scale"/> times <paramref name="sizeShare"/> long and each
        /// <paramref name="carWidthM"/> wide (its height scaled with its width): first an instance of <paramref name="model"/>
        /// (MATKOT's streamed locomotive and tender, <see cref="ServiceTrain"/>), then this showcase's coaches
        /// (<see cref="TrainModels.Coach"/>, their meshes, palette and material owned by <paramref name="assets"/>). Each car is
        /// placed at its own distance along the line, faces along it and rides it (<see cref="Place"/>, <see cref="MaxLiftM"/>);
        /// the train shuttles, the locomotive leading each way, between the line's points nearest <paramref name="startAt"/>
        /// and <paramref name="endAt"/> (world). The model's own frame is turned so its length runs along +Z
        /// (<paramref name="modelYaw"/>, degrees, then to its long horizontal axis) with its base on the car's origin; its
        /// meshes and materials stay the stream's (shared, not owned).</summary>
        public static RouteTrain SpawnModel(Transform parent, MapRuntimeAssets assets, GameObject model, IReadOnlyList<Vector2> line,
            TerrainData data, Vector3 origin, Vector3 startAt, Vector3 endAt, float modelYaw, float sizeShare, int cars, float carWidthM)
        {
            var train = NewTrain(parent, line, data, origin);
            float a = train.Nearest(new Vector2(startAt.x, startAt.z)), b = train.Nearest(new Vector2(endAt.x, endAt.z));
            train.startS = Mathf.Min(a, b); train.endS = Mathf.Max(a, b);
            float total = ModelRealLengthM * Scale * sizeShare, carLength = total / Mathf.Max(cars, 1);

            // The locomotive: the model turned to +Z, its base on the car's origin, sized to the car.
            var loco = new GameObject("Locomotive");
            loco.transform.SetParent(train.transform, false);
            var body = UnityEngine.Object.Instantiate(model, loco.transform, false);
            body.name = model.name;
            body.transform.localRotation = Quaternion.Euler(0f, modelYaw, 0f) * body.transform.localRotation;
            var size = LocalBounds(loco.transform, body);
            if (size.size.x > size.size.z)
            {
                body.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * body.transform.localRotation;
                size = LocalBounds(loco.transform, body);
            }
            body.transform.localPosition -= new Vector3(size.center.x, size.min.y, size.center.z);
            loco.transform.localScale = CarScale(size.size, carLength, carWidthM);
            train.AddCar(loco.transform, carLength);

            // The coaches: the showcase's painted miniatures, one mesh per coach as the showcase's own consist.
            if (cars > 1)
            {
                var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                    ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterialAssets.Create)");
                var palette = assets.Own(UnitPalette.MakeTexture(readable: false));
                var paint = assets.Own(new Material(template) { name = "Service train (palette)" });
                paint.SetTexture("_BaseMap", palette);
                for (int k = 1; k < cars; k++)
                {
                    var mesh = assets.Own(TrainModels.Coach($"ServiceCoach{k}"));
                    var bounds = mesh.bounds;
                    mesh.UploadMeshData(true);
                    var coach = new GameObject($"Coach{k}");
                    coach.transform.SetParent(train.transform, false);
                    var shape = new GameObject("Body");
                    shape.transform.SetParent(coach.transform, false);
                    shape.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    shape.AddComponent<MeshFilter>().sharedMesh = mesh;
                    shape.AddComponent<MeshRenderer>().sharedMaterial = paint;
                    coach.transform.localScale = CarScale(bounds.size, carLength, carWidthM);
                    train.AddCar(coach.transform, carLength);
                }
            }
            train.length = total;
            train.carHalfWidth = carWidthM / 2f;
            train.terrain = data; train.terrainOrigin = origin; train.clearGround = true;
            var (h, d) = train.HeadAt(0f); train.Place(h, d);
            return train;
        }

        /// <summary>The scale that makes a shape of <paramref name="size"/> (its length along Z) <paramref name="length"/> long
        /// and <paramref name="width"/> wide, its height scaled with its width.</summary>
        static Vector3 CarScale(Vector3 size, float length, float width)
        {
            float across = width / Mathf.Max(size.x, 1e-4f);
            return new Vector3(across, across, length / Mathf.Max(size.z, 1e-4f));
        }

        /// <summary>Adds a car <paramref name="length"/> long behind the cars already in the train, nose to tail.</summary>
        void AddCar(Transform car, float length)
        {
            float offset = carOffset.Count == 0 ? 0f : carOffset[carOffset.Count - 1] + carHalf[carHalf.Count - 1];
            cars.Add(car); carOffset.Add(offset + length / 2f); carHalf.Add(length / 2f);
        }

        /// <summary>The bounds of <paramref name="body"/>'s renderers in <paramref name="frame"/>'s local space.</summary>
        static Bounds LocalBounds(Transform frame, GameObject body)
        {
            var bounds = new Bounds(); bool any = false;
            foreach (var f in body.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null) Add(f.transform, f.sharedMesh.bounds);
            foreach (var s in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.sharedMesh != null) Add(s.transform, s.sharedMesh.bounds);
            return bounds;

            void Add(Transform t, Bounds local)
            {
                var m = frame.worldToLocalMatrix * t.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; } else bounds.Encapsulate(p);
                }
            }
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
