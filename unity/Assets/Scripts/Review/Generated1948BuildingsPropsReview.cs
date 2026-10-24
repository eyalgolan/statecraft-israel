#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.Game.Review
{
    /// <summary>An Editor-only Play Mode art display on the real map and camera. Samples have no sim state or deployment.</summary>
    public sealed class Generated1948BuildingsPropsReview : MonoBehaviour
    {
        public const string Flag = "buildingpropsreview", SceneName = "GeneratedBuildingsPropsReview";
        public const string AssetFolder = "Assets/Editor/Generated1948Review";
        public const string ScenePath = AssetFolder + "/GeneratedBuildingsPropsReview.unity";
        public const float Spacing = 1000f, DisplayWidth = 600f;
        [SerializeField] GameObject[] prefabs = Array.Empty<GameObject>();
        [SerializeField] Material[] materials = Array.Empty<Material>();
        [SerializeField] string[] labels = Array.Empty<string>();
        readonly List<Vector3> labelPositions = new List<Vector3>();
        MapScene? map;
        Camera? view;
        GameObject? display;
        public IReadOnlyList<GameObject> Prefabs => prefabs;
        public IReadOnlyList<string> Labels => labels;
        public IReadOnlyList<Material> Materials => materials;
        public int PlacedCount { get; private set; }

        IEnumerator Start()
        {
            // GameBoot builds the map asynchronously. The additive scene never substitutes an editor preview camera.
            while ((map = FindFirstObjectByType<MapScene>()) == null || map.Terrain == null || Camera.main == null)
                yield return null;
            view = Camera.main;
            Place(map, view!);
        }

        public void Place(MapScene target, Camera camera)
        {
            if (prefabs.Length != labels.Length || prefabs.Length != materials.Length || prefabs.Length == 0 || prefabs.Any(p => p == null) || materials.Any(m => m == null))
                throw new InvalidOperationException("Buildings/props review needs a named prefab for every sample.");
            map = target; view = camera;
            if (display != null) Remove(display);
            display = new GameObject("Buildings and props — review samples");
            display.transform.SetParent(transform, false);
            var centre = target.Frame.ToWorld(34.88, 32.01, 0); // art-display area, not a historical placement
            float floor = float.MinValue;
            var platformSize = PlatformSize(prefabs.Length);
            // Cover the full display footprint, including sample margins, so terrain cannot rise through an edge cell.
            for (int z = 0; z <= 8; z++) for (int x = 0; x <= 8; x++)
            {
                var at = centre + new Vector3((x / 8f - .5f) * platformSize.x, 0f, (z / 8f - .5f) * platformSize.z);
                floor = Mathf.Max(floor, target.Terrain.SampleHeight(at) + target.Terrain.transform.position.y);
            }
            // The display is not a deployment: its floor must clear existing map kit geometry too.
            // Bounds are read only; no campaign building or marker is removed or moved.
            foreach (var existing in target.GetComponentsInChildren<Renderer>())
            {
                var b = existing.bounds;
                if (b.max.x >= centre.x - platformSize.x * .5f && b.min.x <= centre.x + platformSize.x * .5f &&
                    b.max.z >= centre.z - platformSize.z * .5f && b.min.z <= centre.z + platformSize.z * .5f)
                    floor = Mathf.Max(floor, b.max.y);
            }
            floor += 25f;
            centre.y = floor;
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Review display surface";
            platform.transform.SetParent(display.transform, false);
            platform.transform.position = centre - Vector3.up * 2f;
            platform.transform.localScale = PlatformSize(prefabs.Length);
            var collider = platform.GetComponent<Collider>(); collider.enabled = false; Remove(collider);
            var renderer = platform.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = target.MarkerMaterial;
            var colour = new MaterialPropertyBlock(); colour.SetColor("_BaseColor", new Color(.42f, .43f, .40f));
            renderer.SetPropertyBlock(colour);
            labelPositions.Clear(); PlacedCount = 0;
            for (int i = 0; i < prefabs.Length; i++)
            {
                var go = Instantiate(prefabs[i], display.transform, false);
                go.name = labels[i];
                foreach (var part in go.GetComponentsInChildren<Renderer>(true)) part.sharedMaterial = materials[i];
                go.transform.position = centre + Offset(i, prefabs.Length);
                var bounds = BoundsOf(go);
                go.transform.localScale *= DisplayWidth / Mathf.Max(bounds.size.x, bounds.size.z);
                bounds = BoundsOf(go);
                go.transform.position += new Vector3(centre.x + Offset(i, prefabs.Length).x - bounds.center.x,
                    floor - bounds.min.y, centre.z + Offset(i, prefabs.Length).z - bounds.center.z);
                labelPositions.Add(centre + Offset(i, prefabs.Length) + new Vector3(0f, 15f, -DisplayWidth * .6f));
                PlacedCount++;
            }
            Statecraft.Game.Map.Units.UnitShowcase.HideTrees(target, Flag,
                new List<(Vector2 centre, float radius)> { (new Vector2(centre.x, centre.z), PlatformSize(prefabs.Length).magnitude * .55f) });
            var rig = camera.GetComponent<CameraRig>();
            if (rig == null) throw new InvalidOperationException("The review uses the actual map CameraRig.");
            rig.LookAt = centre; rig.Distance = Mathf.Max(CameraController.MinDistance, PlatformSize(prefabs.Length).magnitude * 2f); rig.Yaw = 0f; rig.Apply();
        }

        public static Vector3 PlatformSize(int count) => new Vector3(3 * Spacing + 400f, 4f, Mathf.CeilToInt(count / 3f) * Spacing + 400f);
        public static Vector3 Offset(int index, int count) => new Vector3((index % 3 - 1) * Spacing, 0f,
            ((Mathf.CeilToInt(count / 3f) - 1) * .5f - index / 3) * Spacing);
        static void Remove(UnityEngine.Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        public static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(go.name + " has no renderer");
            var result = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }

        void OnGUI()
        {
            if (view == null || PlacedCount == 0) return;
            GUI.Box(new Rect(12f, 108f, 420f, 50f), "Buildings and props review\nVisual samples only — display sizes are not historical scale");
            for (int i = 0; i < labelPositions.Count; i++)
            {
                var point = view.WorldToScreenPoint(labelPositions[i]);
                if (point.z > 0f) GUI.Box(new Rect(point.x - 125f, Screen.height - point.y, 250f, 24f), labels[i]);
            }
        }

        void OnDestroy()
        {
            if (map != null) Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Flag, new List<(Vector2 centre, float radius)>());
        }
    }
}

#endif
