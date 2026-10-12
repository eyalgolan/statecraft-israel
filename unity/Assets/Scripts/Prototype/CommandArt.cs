using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Prototype
{
    /// <summary>Presentation-only bindings to the imported model collection. Mission orders and selection live elsewhere.</summary>
    public static class CommandArt
    {
        public const string ResourceFolder = "SettlementPrototypes/";
        public static readonly IReadOnlyList<string> Catalog = Array.AsReadOnly(new[] {
            "airliner-c54-v1",
            "altalena-lst-v1",
            "figure-arab-villager-v1",
            "figure-builder-1950-v1",
            "figure-elder-immigrant-v1",
            "figure-immigrant-child-v1",
            "figure-immigrant-woman-v1",
            "figure-kibbutz-woman-v1",
            "figure-nurse-1948-v1",
            "figure-palmach-scout-v1",
            "figure-pioneer-farmer-v1-tpose",
            "figure-soldier-1948-v1",
            "figure-yemenite-immigrant-v1",
            "haifa-port-city-v1",
            "immigrant-steamer-v1",
            "jerusalem-stone-city-v1",
            "jerusalem-stone-city-v1-3denhance",
            "kibbutz-compound-v2",
            "kibbutz-dining-hall-v1",
            "maabara-camp-v1",
            "moshava-town-v1",
            "nahalal-moshav-v1",
            "sandwich-armoured-truck-v1",
            "shikun-housing-v1",
            "stone-village-v1",
            "tegart-fort-v1",
            "tel-aviv-white-city-v1",
            "vehicle-citrus-truck-v1",
            "vehicle-farm-tractor-v1",
            "vehicle-fishing-boat-v1",
            "vehicle-horse-wagon-v1",
            "vehicle-intercity-bus-v1",
            "vehicle-steam-locomotive-v1",
            "vehicle-willys-jeep-v1"
        });
        public static string[] ModelsFor(CommandKind kind)
        {
            switch (kind) {
                case CommandKind.Brigade: return new[] { "figure-soldier-1948-v1", "figure-soldier-1948-v1", "figure-soldier-1948-v1" };
                case CommandKind.Scout: return new[] { "figure-palmach-scout-v1", "figure-palmach-scout-v1" };
                case CommandKind.Worker: return new[] { "figure-builder-1950-v1", "figure-pioneer-farmer-v1-tpose" };
                case CommandKind.Immigrants: return new[] { "figure-immigrant-woman-v1", "figure-immigrant-child-v1", "figure-elder-immigrant-v1" };
                case CommandKind.Convoy: return new[] { "sandwich-armoured-truck-v1" };
                case CommandKind.Depot: return new[] { "tel-aviv-white-city-v1" };
                case CommandKind.Camp: return new[] { "maabara-camp-v1" };
                case CommandKind.Settlement: return new[] { "kibbutz-compound-v2" };
                default: return Array.Empty<string>(); // Threats stay neutral symbols, never a civilian model.
            }
        }
        public static string Title(string name) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            name.Replace("figure-", "").Replace("vehicle-", "").Replace("-v1", "").Replace("-v2", "").Replace('-', ' '));
        public static GameObject Load(string name) => Resources.Load<GameObject>(ResourceFolder + name);
        public static bool TryAttach(CommandKind kind, Transform parent, out CommandArtMotion motion)
        {
            motion = null; var names = ModelsFor(kind);
            if (names.Length == 0) return false;
            var prefabs = names.Select(Load).ToArray();
            if (prefabs.Any(p => p == null)) return false;
            bool figures = kind == CommandKind.Brigade || kind == CommandKind.Scout || kind == CommandKind.Worker || kind == CommandKind.Immigrants;
            var animators = new List<Animator>();
            for (int i = 0; i < prefabs.Length; i++) {
                var instance = Object.Instantiate(prefabs[i], parent, false); instance.name = names[i];
                Fit(instance, figures ? 520 : kind == CommandKind.Convoy ? 1150 : 1850, figures);
                if (figures) instance.transform.localPosition += new Vector3((i - (prefabs.Length - 1) / 2f) * 310, 0, (i % 2) * 180);
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animators.Add(animator); }
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            motion = new CommandArtMotion(animators.ToArray()); return true;
        }
        public static Bounds BoundsOf(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.one);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        public static void Fit(GameObject instance, float extent, bool byHeight = false)
        {
            var bounds = BoundsOf(instance);
            float dimension = byHeight ? bounds.size.y : Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            instance.transform.localScale *= extent / Mathf.Max(.0001f, dimension);
            bounds = BoundsOf(instance);
            var delta = instance.transform.parent.InverseTransformVector(new Vector3(-bounds.center.x + instance.transform.parent.position.x,
                -bounds.min.y + instance.transform.parent.position.y, -bounds.center.z + instance.transform.parent.position.z));
            instance.transform.localPosition += delta;
        }
    }
    public sealed class CommandArtMotion
    {
        readonly Animator[] animators; bool? wasMoving;
        public CommandArtMotion(Animator[] animators) { this.animators = animators; }
        public void Update(bool moving, bool paused)
        {
            if (!Application.isPlaying) return;
            foreach (var animator in animators) {
                if (animator == null || animator.runtimeAnimatorController == null) continue;
                animator.speed = paused ? 0 : 1;
                if (wasMoving == moving) continue;
                string[] candidates = moving ? new[] { "walk_001", "walk", "run_001" } : new[] { "idle_001", "idle", "look_around_001" };
                foreach (string state in candidates) {
                    int hash = Animator.StringToHash("Base Layer." + state);
                    if (!animator.HasState(0, hash)) continue;
                    animator.CrossFade(hash, .12f); break;
                }
            }
            wasMoving = moving;
        }
    }

    /// <summary>A single-model inspection camera, rendered to a texture. Does not move or reconfigure the mission camera.</summary>
    internal sealed class CommandGallery : IDisposable
    {
        readonly CommandModel model; readonly VisualElement overlay; readonly Label title;
        readonly List<(VisualElement element, bool enabled)> disabledSiblings = new List<(VisualElement, bool)>();
        readonly Button nextButton; VisualElement previousFocus;
        readonly GameObject stage; readonly Camera camera; readonly RenderTexture texture;
        GameObject preview; int index; bool resume; bool disposed;
        public bool IsOpen { get; private set; }
        public CommandGallery(VisualElement parent, CommandModel model)
        {
            this.model = model;
            overlay = new VisualElement { name = "command-model-gallery" };
            overlay.style.position = Position.Absolute; overlay.style.left = overlay.style.right = overlay.style.top = overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(.025f,.04f,.045f,.97f); overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            var card = new VisualElement(); card.style.width = 740; card.style.height = 580; card.style.maxWidth = Length.Percent(94); card.style.maxHeight = Length.Percent(92);
            title = new Label(); title.style.color = new Color(.88f,.75f,.46f); title.style.fontSize = 20; title.style.unityTextAlign = TextAnchor.MiddleCenter; card.Add(title);
            texture = new RenderTexture(768, 512, 24) { name = "Command model inspection" }; texture.Create();
            var picture = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit }; picture.style.flexGrow = 1; picture.style.flexShrink = 1; picture.style.minHeight = 120; card.Add(picture);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center;
            Button AddButton(string text, Action clicked) { var b = new Button(clicked) { text = text }; b.style.paddingLeft = b.style.paddingRight = 20; b.style.paddingTop = b.style.paddingBottom = 10; row.Add(b); return b; }
            AddButton("← Previous", () => Show(index - 1)); nextButton = AddButton("Next →", () => Show(index + 1)); AddButton("Close · Escape", Close); card.Add(row);
            var note = new Label("MODEL COLLECTION  ·  Mission paused  ·  Models may represent different periods"); note.style.color = new Color(.8f,.81f,.75f); note.style.unityTextAlign = TextAnchor.MiddleCenter; card.Add(note);
            overlay.Add(card); parent.Add(overlay); overlay.style.display = DisplayStyle.None;
            stage = new GameObject("Command model inspection stage"); stage.transform.position = new Vector3(0,-20000,0);
            var cameraObject = new GameObject("Command model inspection camera"); cameraObject.transform.SetParent(stage.transform,false);
            camera = cameraObject.AddComponent<Camera>(); camera.targetTexture = texture; camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.105f,.11f);
            camera.nearClipPlane = .05f; camera.farClipPlane = 40; camera.fieldOfView = 35; camera.enabled = false;
            camera.transform.localPosition = new Vector3(4,3.3f,-6); camera.transform.LookAt(stage.transform.position + Vector3.up * 1.2f);
            var lamp = new GameObject("Inspection light"); lamp.transform.SetParent(stage.transform,false); lamp.transform.localRotation = Quaternion.Euler(40,-30,0);
            var light = lamp.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; light.cullingMask = 1 << 30; light.shadows = LightShadows.None;
            stage.SetActive(false);
        }
        public void Open()
        {
            if (IsOpen || disposed) return;
            previousFocus = overlay.panel?.focusController?.focusedElement as VisualElement;
            previousFocus?.Blur();
            foreach (var sibling in overlay.parent.Children().Where(e => e != overlay)) {
                disabledSiblings.Add((sibling, sibling.enabledSelf)); sibling.SetEnabled(false);
            }
            resume = !model.Paused; model.Paused = true; IsOpen = true;
            overlay.style.display = DisplayStyle.Flex; stage.SetActive(true); camera.enabled = true; Show(index);
            nextButton.Focus();
            // Wait for the newly visible panel's style pass if focus could not be acquired immediately.
            nextButton.schedule.Execute(() => { if (IsOpen) nextButton.Focus(); });
        }
        public void Close()
        {
            if (!IsOpen) return;
            var focused = overlay.panel?.focusController?.focusedElement as VisualElement;
            if (focused != null && overlay.Contains(focused)) focused.Blur();
            IsOpen = false; camera.enabled = false; stage.SetActive(false); overlay.style.display = DisplayStyle.None;
            foreach (var entry in disabledSiblings) entry.element.SetEnabled(entry.enabled);
            disabledSiblings.Clear();
            if (previousFocus != null && previousFocus.panel != null && previousFocus.enabledInHierarchy) previousFocus.Focus();
            previousFocus = null;
            if (resume && model.Outcome == CommandOutcome.Playing) model.Paused = false;
        }
        void Show(int next)
        {
            index = (next + CommandArt.Catalog.Count) % CommandArt.Catalog.Count;
            if (preview != null) { preview.SetActive(false); Destroy(preview); }
            string name = CommandArt.Catalog[index]; var prefab = CommandArt.Load(name);
            title.text = (index + 1) + " / " + CommandArt.Catalog.Count + "   " + CommandArt.Title(name);
            if (prefab == null) { title.text += " · unavailable"; return; }
            preview = new GameObject("Inspection pivot"); preview.transform.SetParent(stage.transform, false);
            var instance = Object.Instantiate(prefab, preview.transform, false); CommandArt.Fit(instance, 3.3f);
            foreach (var t in preview.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var animator in preview.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animator.speed = 0; }
        }
        public void Tick(float dt) { if (IsOpen && preview != null) preview.transform.Rotate(0, dt * 22, 0, Space.World); }
        public void Dispose() { if (disposed) return; disposed = true; Close(); overlay.RemoveFromHierarchy(); if (camera != null) camera.targetTexture = null; Destroy(stage); texture.Release(); Destroy(texture); }
        static void Destroy(Object obj) { if (obj == null) return; if (Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj); }
    }
}
