#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Internal authoring window for content/map/overrides.yaml (#1009). It never edits generated inputs,
    /// historical coordinates, identities, sources, localities_1948 or route geometry. Unsaved edits survive a domain
    /// reload (the document is serialized and restored), and closing the window with them asks to save or discard.</summary>
    public sealed class MapOverrideEditorWindow : EditorWindow
    {
        [SerializeField] MapOverrideEditorDocument? document;
        string error = "", message = "";
        Vector2 inspectorScroll, pan;
        float zoom = 1f;
        (int q, int r)? selectedHex;
        string selectedLocation = "";
        int coordinateQ, coordinateR, periodIndex;
        bool draggingLocation, panning;
        (int q, int r)? dragTarget;
        Vector2 lastMouse;

        [MenuItem("Statecraft/Map override editor")]
        public static void Open() => GetWindow<MapOverrideEditorWindow>("Map overrides");

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            saveChangesMessage = "The map override editor has unsaved edits to content/map/overrides.yaml. Save them?";
            if (document == null) Load();
            else if (!document.IsLoaded) Restore();
            else UpdateUnsaved();
        }

        // A domain reload disables the window and keeps the document; only closing the window destroys it.
        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        void OnDestroy()
        {
            if (document != null) DestroyImmediate(document);
        }

        /// <summary>Unity calls this when the window closes with unsaved edits and the author chooses Save. A failed save
        /// keeps hasUnsavedChanges set, so the window stays open with the error.</summary>
        public override void SaveChanges() => Save();

        void OnUndoRedo()
        {
            message = "Undo/redo applied";
            error = "";
            UpdateUnsaved();
            Repaint();
        }

        void UpdateUnsaved() => hasUnsavedChanges = document != null && document.HasUnsavedChanges;

        void Restore()
        {
            try
            {
                document!.Restore();
                SyncCoordinateFields();
                error = "";
                message = "Restored unsaved edits after a script reload";
            }
            catch (Exception exception) { error = exception.Message; }
            UpdateUnsaved();
        }

        void Reload()
        {
            if (document != null && !document.IsLoaded) { Restore(); return; }   // retry, keeping the restored edits
            if (hasUnsavedChanges && !EditorUtility.DisplayDialog("Reload map overrides",
                    "Reloading discards your unsaved edits to content/map/overrides.yaml.", "Discard and reload", "Cancel"))
                return;
            Load();
        }

        void Load()
        {
            try
            {
                if (document != null) DestroyImmediate(document);
                document = MapOverrideEditorDocument.Load(Paths.Content);
                var first = document.Locations.OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                selectedLocation = first?.Id ?? "";
                if (first != null) SetCoordinateFields(document.LocationAt(first.Id));
                error = "";
                message = "Loaded " + document.OverridePath;
            }
            catch (Exception exception)
            {
                document = null;
                error = exception.Message;
            }
            UpdateUnsaved();
        }

        void OnGUI()
        {
            Toolbar();
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (document == null || !document.IsLoaded)
            {
                EditorGUILayout.HelpBox("The map could not be loaded. Fix the named input and choose Reload.", MessageType.Info);
                return;
            }

            var mapRect = GUILayoutUtility.GetRect(240f, 100000f, 240f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var inspectorWidth = Mathf.Min(360f, Mathf.Max(290f, position.width * 0.32f));
            var canvas = new Rect(mapRect.x, mapRect.y, Mathf.Max(100f, mapRect.width - inspectorWidth - 6f), mapRect.height);
            var inspector = new Rect(canvas.xMax + 6f, mapRect.y, inspectorWidth, mapRect.height);
            DrawMap(canvas);
            GUILayout.BeginArea(inspector, EditorStyles.helpBox);
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            DrawInspector();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) Reload();
                using (new EditorGUI.DisabledScope(document == null || !hasUnsavedChanges))
                {
                    if (GUILayout.Button("Discard", EditorStyles.toolbarButton, GUILayout.Width(60))) Discard();
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
                }
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(45))) Undo.PerformUndo();
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(45))) Undo.PerformRedo();
                GUILayout.Space(8);
                GUILayout.Label(hasUnsavedChanges ? "Unsaved changes" : "Saved", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Scroll: zoom · middle drag: pan · drag a marker: move", EditorStyles.miniLabel);
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.LabelField(message, EditorStyles.miniLabel);
        }

        void Discard()
        {
            if (document == null) return;
            try
            {
                Undo.RecordObject(document, "Discard map override edits");
                document.Reload();
                error = "";
                message = "Discarded unsaved edits and reloaded the saved override file";
                SyncCoordinateFields();
            }
            catch (Exception exception) { error = exception.Message; }
            UpdateUnsaved();
        }

        void Save()
        {
            if (document == null || !document.IsLoaded) return;
            if (document.Save(out error))
            {
                AssetDatabase.Refresh();
                message = "Saved " + document.OverridePath;
            }
            UpdateUnsaved();
        }

        void DrawInspector()
        {
            if (document == null) return;
            EditorGUILayout.LabelField("Hex", EditorStyles.boldLabel);
            if (selectedHex is not { } hex) EditorGUILayout.HelpBox("Select a hex on the map.", MessageType.Info);
            else
            {
                EditorGUILayout.LabelField("Coordinates", $"q={hex.q}, r={hex.r}");
                if (!document.IsLand(hex.q, hex.r)) EditorGUILayout.HelpBox("Water hex: land/water flips and terrain edits are not permitted.", MessageType.Info);
                else
                {
                    var terrain = document.TerrainAt(hex.q, hex.r);
                    var terrainIndex = Math.Max(0, document.LandBiomes.ToList().IndexOf(terrain));
                    EditorGUI.BeginChangeCheck();
                    terrainIndex = EditorGUILayout.Popup("Terrain", terrainIndex, document.LandBiomes.ToArray());
                    if (EditorGUI.EndChangeCheck()) Mutate("Change hex terrain", () => document.SetTerrain(hex.q, hex.r, document.LandBiomes[terrainIndex]));
                }
                DrawControl(hex.q, hex.r);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);
            var choices = document.Locations.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
            var selected = Math.Max(0, choices.FindIndex(x => x.Id == selectedLocation));
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup("Location", selected, choices.Select(x => x.Name + " (" + x.Id + ")").ToArray());
            if (EditorGUI.EndChangeCheck() && choices.Count > 0)
            {
                selectedLocation = choices[selected].Id;
                SyncCoordinateFields();
            }
            var location = choices.FirstOrDefault(x => x.Id == selectedLocation);
            if (location != null)
            {
                EditorGUILayout.LabelField("Identity", location.Id);
                EditorGUILayout.LabelField("Kind", location.Kind);
                var at = document.LocationAt(location.Id);
                EditorGUILayout.LabelField("Authored position", $"q={at.q}, r={at.r}");
                coordinateQ = EditorGUILayout.IntField("q", coordinateQ);
                coordinateR = EditorGUILayout.IntField("r", coordinateR);
                if (GUILayout.Button("Move to coordinates"))
                    Mutate("Move map location", () => document.SetLocation(location.Id, coordinateQ, coordinateR));
                DrawDependencies(location.Id);
            }
        }

        void DrawControl(int q, int r)
        {
            if (document == null || document.Periods.Count == 0) return;
            periodIndex = Mathf.Clamp(periodIndex, 0, document.Periods.Count - 1);
            periodIndex = EditorGUILayout.Popup("Control period", periodIndex, document.Periods.Select(x => x.Id).ToArray());
            var period = document.Periods[periodIndex].Id;
            try
            {
                var current = document.ControlAt(q, r, period);
                var index = Math.Max(0, document.ControlValues.ToList().IndexOf(current));
                EditorGUI.BeginChangeCheck();
                index = EditorGUILayout.Popup("Control", index, document.ControlValues.ToArray());
                if (EditorGUI.EndChangeCheck()) Mutate("Change one control period", () => document.SetControl(q, r, period, document.ControlValues[index]));
            }
            catch (ArgumentException)
            {
                EditorGUILayout.HelpBox("This hex is not in the 1948–49 control file, so its control cannot be edited.", MessageType.Info);
            }
        }

        void DrawDependencies(string id)
        {
            if (document == null) return;
            var dependencies = document.Dependencies(id);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Moving a marker changes only its authored gameplay hex. Identity, sources, city/site records, sourced route waypoints and route geometry remain unchanged.", MessageType.Warning);
            DependencyLines("Cities", dependencies.Cities);
            DependencyLines("Sites", dependencies.Sites);
            DependencyLines("Corridors", dependencies.Corridors);
        }

        static void DependencyLines(string title, IReadOnlyList<string> rows)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (rows.Count == 0) EditorGUILayout.LabelField("None", EditorStyles.wordWrappedMiniLabel);
            else foreach (var row in rows) EditorGUILayout.LabelField("• " + row, EditorStyles.wordWrappedMiniLabel);
        }

        void Mutate(string label, Action change)
        {
            if (document == null) return;
            try
            {
                Undo.RecordObject(document, label);
                change();
                EditorUtility.SetDirty(document);
                error = "";
                message = label;
                UpdateUnsaved();
                SyncCoordinateFields();
                Repaint();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is Statecraft.Sim.ContentException)
            {
                error = exception.Message;
            }
        }

        void DrawMap(Rect rect)
        {
            if (document == null) return;
            EditorGUI.DrawRect(rect, new Color(0.10f, 0.11f, 0.12f));
            var hexes = document.Hexes.ToList();
            if (hexes.Count == 0) return;
            var centres = hexes.ToDictionary(x => (x.Q, x.R), x => HexGrid.Centre(x.Q, x.R, 1f));
            var minX = centres.Values.Min(x => x.x); var maxX = centres.Values.Max(x => x.x);
            var minY = centres.Values.Min(x => x.y); var maxY = centres.Values.Max(x => x.y);
            var span = new Vector2(Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
            var mapCentre = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
            var scale = Mathf.Min((rect.width - 20f) / span.x, (rect.height - 20f) / span.y) * zoom;
            Vector2 Screen(Vector2 point) => rect.center + Vector2.Scale(point - mapCentre, new Vector2(scale, scale)) + pan;
            var radius = Mathf.Max(1.2f, scale * .72f);

            Handles.BeginGUI();
            foreach (var hex in hexes)
            {
                var centre = Screen(centres[(hex.Q, hex.R)]);
                if (!rect.Contains(centre)) continue;
                var biome = document.TerrainAt(hex.Q, hex.R);
                var colour = MapBuilder.BiomeColours.TryGetValue(biome, out var found) ? found : Color.gray;
                colour.a = hex.Land ? .92f : .55f;
                Handles.color = colour;
                if (radius < 2.2f) Handles.DrawSolidDisc(centre, Vector3.forward, radius);
                else Handles.DrawAAConvexPolygon(HexPoints(centre, radius));
            }
            if (selectedHex is { } selected && centres.TryGetValue(selected, out var selectedCentre))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(Screen(selectedCentre), Vector3.forward, Mathf.Max(4f, radius + 2f), 2f);
            }
            foreach (var location in document.Locations)
            {
                var at = dragTarget.HasValue && draggingLocation && location.Id == selectedLocation ? dragTarget.Value : document.LocationAt(location.Id);
                if (!centres.TryGetValue(at, out var point)) continue;
                var screen = Screen(point);
                Handles.color = location.Id == selectedLocation ? new Color(1f, .82f, .18f) : Color.white;
                Handles.DrawSolidDisc(screen, Vector3.forward, location.Id == selectedLocation ? 5f : 3f);
                if (location.Id == selectedLocation)
                    Handles.Label(screen + new Vector2(7f, -8f), location.Name, EditorStyles.miniLabel);
            }
            Handles.EndGUI();
            HandleMapInput(rect, centres, Screen);
        }

        static Vector3[] HexPoints(Vector2 centre, float radius)
        {
            var points = new Vector3[6];
            for (int i = 0; i < points.Length; i++)
            {
                var angle = Mathf.Deg2Rad * (60f * i - 30f);
                points[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        void HandleMapInput(Rect rect, Dictionary<(int q, int r), Vector2> centres, Func<Vector2, Vector2> screen)
        {
            if (document == null) return;
            var current = Event.current;
            if (!rect.Contains(current.mousePosition) && !draggingLocation && !panning) return;
            if (current.type == EventType.ScrollWheel)
            {
                zoom = Mathf.Clamp(zoom * (current.delta.y > 0 ? .9f : 1.1f), .6f, 8f);
                current.Use(); Repaint(); return;
            }
            if (current.type == EventType.MouseDown && current.button == 2)
            {
                panning = true; lastMouse = current.mousePosition; current.Use(); return;
            }
            if (current.type == EventType.MouseDrag && panning)
            {
                pan += current.mousePosition - lastMouse; lastMouse = current.mousePosition; current.Use(); Repaint(); return;
            }
            if (current.type == EventType.MouseUp && current.button == 2) { panning = false; current.Use(); return; }
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                var marker = document.Locations.Select(x => (location: x, at: document.LocationAt(x.Id)))
                    .Where(x => centres.ContainsKey(x.at))
                    .OrderBy(x => Vector2.Distance(screen(centres[x.at]), current.mousePosition)).FirstOrDefault();
                if (marker.location != null && Vector2.Distance(screen(centres[marker.at]), current.mousePosition) <= 9f)
                {
                    selectedLocation = marker.location.Id;
                    draggingLocation = true;
                    dragTarget = marker.at;
                    SetCoordinateFields(marker.at);
                }
                else
                {
                    selectedHex = centres.OrderBy(x => Vector2.Distance(screen(x.Value), current.mousePosition)).First().Key;
                    dragTarget = null;
                }
                current.Use(); Repaint(); return;
            }
            if (current.type == EventType.MouseDrag && draggingLocation)
            {
                dragTarget = centres.OrderBy(x => Vector2.Distance(screen(x.Value), current.mousePosition)).First().Key;
                current.Use(); Repaint(); return;
            }
            if (current.type == EventType.MouseUp && current.button == 0 && draggingLocation)
            {
                draggingLocation = false;
                if (dragTarget is { } target) Mutate("Drag map location", () => document.SetLocation(selectedLocation, target.q, target.r));
                dragTarget = null;
                current.Use(); Repaint();
            }
        }

        void SyncCoordinateFields()
        {
            if (document != null && !string.IsNullOrEmpty(selectedLocation)) SetCoordinateFields(document.LocationAt(selectedLocation));
        }

        void SetCoordinateFields((int q, int r) at) { coordinateQ = at.q; coordinateR = at.r; }
    }
}
