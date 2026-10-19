#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>Statecraft/Hex Tiles/Hex Painter: loads a board into the open scene (one object per hex), picks a hex by
    /// clicking it in the Scene view, and edits its hex-tile presentation: prototype, ground, unit, facing and region (the
    /// trees of an environment hex). Each change rebuilds just that hex and is written to the board's
    /// &lt;id&gt;.overrides.json, so it survives regenerating the board (docs/hex-tiles/README.md, "Painting hexes by hand").
    /// Terrain, control and locations are the map editor's (#1009), not the painter's. Editor only; the board in the scene
    /// is never saved.</summary>
    public sealed class HexPainterWindow : EditorWindow
    {
        const string None = "(none)";
        string[] boards = Array.Empty<string>();
        int boardIndex;
        string boardId = "";
        GameObject? root;
        (int q, int r)? selected;
        bool pick = true;
        string status = "";
        Vector2 scroll;

        [MenuItem("Statecraft/Hex Tiles/Hex Painter")]
        public static void Open() => GetWindow<HexPainterWindow>("Hex Painter");

        string BoardsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", HexPainter.BoardsFolder));
        HexBoardState? State => root != null ? root.GetComponent<HexBoardState>() : null;

        void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
            boards = Directory.Exists(BoardsDir) ? HexPainter.BoardIds(BoardsDir) : Array.Empty<string>();
            boardIndex = Mathf.Max(0, Array.IndexOf(boards, boardId));
        }

        void OnDisable() => SceneView.duringSceneGui -= OnScene;

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                boardIndex = EditorGUILayout.Popup(boardIndex, boards);
                using (new EditorGUI.DisabledScope(boards.Length == 0))
                    if (GUILayout.Button("Load into scene", GUILayout.Width(120))) Load(boards[boardIndex]);
            }
            if (GUILayout.Button("Add the captures' light and grade")) Capture.HexBoardCapture.Stage();
            var state = State;
            if (state == null)
            {
                EditorGUILayout.HelpBox("Load a board, then click a hex in the Scene view.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.LabelField($"{boardId}: {state.Board.Hexes.Count} hexes. Edits go to {boardId}{HexBoardOverrides.Suffix}.json");
            pick = EditorGUILayout.ToggleLeft("Pick hexes in the Scene view", pick);

            EditorGUILayout.Space();
            if (selected == null || !state.Board.Hexes.TryGetValue(selected.Value, out var h))
            {
                EditorGUILayout.HelpBox("No hex selected.", MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }
            var (q, r) = selected.Value;
            EditorGUILayout.LabelField($"Hex ({q}, {r})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Terrain", h.terrain + (h.env ? ", environment ring" : "") + (h.region != "" ? $", region {h.region}" : ""));
            if (h.note != "") EditorGUILayout.LabelField("Note", h.note);
            Choice(q, r, "prototype", h.prototype, HexPrototypes.All.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(), true);
            Choice(q, r, "ground", h.ground, HexGround.Ids.ToArray(), false);
            Choice(q, r, "unit", h.unit, HexUnitTypes.All.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(), true);
            Choice(q, r, "region", h.region, state.Board.Regions.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(), true);
            float facing = EditorGUILayout.DelayedFloatField("Facing (deg from north)", h.facing);
            if (!Mathf.Approximately(facing, h.facing)) Apply(q, r, "facing", facing.ToString("R", CultureInfo.InvariantCulture));
            if (status != "") EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>A dropdown of <paramref name="options"/> (with "(none)" for an empty value when allowed); a new pick
        /// is applied at once.</summary>
        void Choice(int q, int r, string field, string current, string[] options, bool allowNone)
        {
            var shown = allowNone ? new[] { None }.Concat(options).ToArray() : options;
            int at = Array.IndexOf(shown, current == "" && allowNone ? None : current);
            if (at < 0) { shown = shown.Append(current).ToArray(); at = shown.Length - 1; }   // a value no longer registered
            int next = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(field), at, shown);
            if (next != at) Apply(q, r, field, shown[next] == None ? "" : shown[next]);
        }

        void Apply(int q, int r, string field, string value)
        {
            if (root == null) return;
            var path = HexPainter.OverridesPath(BoardsDir, boardId);
            try
            {
                var report = HexPainter.Change(root, path, q, r, field, value);
                HexPainter.Import(path);
                status = $"({q},{r}) {field} = '{value}', written to {Path.GetFileName(path)}\n" + (report?.ToString() ?? "no tile on this hex");
            }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
            SceneView.RepaintAll();
        }

        void Load(string id)
        {
            foreach (var old in FindObjectsByType<HexBoardState>(FindObjectsSortMode.None)) Unload(old.gameObject);
            boardId = id; selected = null;
            try
            {
                root = HexPainter.Load(id);
                Physics.SyncTransforms();
                var b = HexBoardBuilder.Bounds(State!.Board);
                var centre = root.transform.TransformPoint(new Vector3(b.center.x, 0f, b.center.y));
                SceneView.lastActiveSceneView?.Frame(new Bounds(centre, new Vector3(b.width, 2000f, b.height)), false);
                status = $"loaded {id}";
            }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
        }

        static void Unload(GameObject board)
        {
            board.GetComponent<HexBoardAssets>()?.Teardown();
            DestroyImmediate(board);
        }

        void OnScene(SceneView view)
        {
            var state = State;
            if (root == null || state == null) return;
            var e = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (pick && e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
            if (pick && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                var hit = HexPainter.Pick(root, HandleUtility.GUIPointToWorldRay(e.mousePosition));
                if (hit != null) { selected = hit; status = ""; Repaint(); e.Use(); }
            }
            if (selected != null && e.type == EventType.Repaint)
            {
                var (q, r) = selected.Value;
                var corners = HexGeometry.Corners(HexGeometry.Centre(q, r, HexScale.HexM), HexScale.HexM)
                    .Select(c => root.transform.TransformPoint(new Vector3(c.x, Mathf.Max(state.Surface.Height(c), 0f) + 60f, c.z))).ToList();
                corners.Add(corners[0]);
                Handles.color = new Color(1f, 0.85f, 0.2f);
                Handles.DrawAAPolyLine(6f, corners.ToArray());
            }
        }
    }
}
