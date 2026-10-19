#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>Statecraft/Hex Tiles/Hex Painter: loads a board into the open scene (one object per hex), picks a hex by
    /// clicking it in the Scene view, and edits its hex-tile presentation: prototype, ground, unit, facing and region (the
    /// trees of an environment hex). A palette of every registered tile and unit (HexPalette) paints tiles hex by hex as a
    /// brush and stands units with a facing. Each change rebuilds just that hex and is written to the board's
    /// &lt;id&gt;.overrides.json, so it survives regenerating the board (docs/hex-tiles/README.md, "Painting hexes by hand").
    /// The time-of-day and under-attack switches are previews only (HexPainterSwitches): never written, and undone on
    /// unload, reload, close and script reload. Terrain, control and locations are the map editor's (#1009), not the
    /// painter's. Editor only; the board in the scene is never saved.</summary>
    public sealed class HexPainterWindow : EditorWindow
    {
        const string None = "(none)";
        /// <summary>What a Scene-view click does: pick a hex, paint the brush tile on it, or stand the brush unit on it.</summary>
        enum Mode { Select, PaintTile, PlaceUnit }
        static readonly string[] ModeLabels = { "Select", "Paint tile", "Place unit" };
        const float Cell = 72f;

        string[] boards = Array.Empty<string>();
        int boardIndex;
        string boardId = "";
        GameObject? root;
        (int q, int r)? selected;
        bool pick = true;
        Mode mode;
        /// <summary>The tile brush: null until one is picked, "" for no tile.</summary>
        string? brushTile;
        string brushUnit = "", filter = "";
        float brushFacing;
        string status = "";
        Vector2 scroll;
        /// <summary>Hexes painted in the current drag, so a drag paints each hex once.</summary>
        readonly HashSet<(int q, int r)> stroke = new HashSet<(int q, int r)>();
        readonly HashSet<string> folded = new HashSet<string>();
        List<HexPaletteGroup> tiles = new List<HexPaletteGroup>(), units = new List<HexPaletteGroup>();
        readonly Dictionary<string, Texture2D?> thumbs = new Dictionary<string, Texture2D?>();
        /// <summary>The time-of-day and under-attack previews (public so tests can watch what the window undoes).</summary>
        public HexPainterSwitches Switches { get; set; } = new HexPainterSwitches();

        [MenuItem("Statecraft/Hex Tiles/Hex Painter")]
        public static void Open() => GetWindow<HexPainterWindow>("Hex Painter");

        string BoardsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", HexPainter.BoardsFolder));
        HexBoardState? State => root != null ? root.GetComponent<HexBoardState>() : null;

        void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
            boards = Directory.Exists(BoardsDir) ? HexPainter.BoardIds(BoardsDir) : Array.Empty<string>();
            boardIndex = Mathf.Max(0, Array.IndexOf(boards, boardId));
            RefreshPalette();
        }

        /// <summary>Closing the window and reloading scripts both come here: nothing the switches changed is left behind.</summary>
        void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
            Switches.Clear();
            DropThumbs();
        }

        void RefreshPalette()
        {
            try { tiles = HexPalette.Tiles(); units = HexPalette.Units(); }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
            DropThumbs();
        }

        void DropThumbs()
        {
            foreach (var t in thumbs.Values) if (t != null) DestroyImmediate(t);
            thumbs.Clear();
        }

        Texture2D? Thumb(HexPaletteEntry e)
        {
            var key = (e.IsUnit ? "unit-" : "") + e.Id;
            if (!thumbs.TryGetValue(key, out var t)) thumbs[key] = t = HexPalette.LoadThumb(HexPalette.ThumbsDir, e);
            return t;
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                boardIndex = EditorGUILayout.Popup(boardIndex, boards);
                using (new EditorGUI.DisabledScope(boards.Length == 0))
                    if (GUILayout.Button("Load into scene", GUILayout.Width(120))) Load(boards[boardIndex]);
                using (new EditorGUI.DisabledScope(root == null))
                    if (GUILayout.Button("Unload", GUILayout.Width(70))) UnloadAll();
            }
            if (GUILayout.Button("Add the captures' light and grade")) HexPainterStage.FindOrStage();
            TimeOfDay();
            var state = State;
            if (state == null)
            {
                EditorGUILayout.HelpBox("Load a board, then click a hex in the Scene view.", MessageType.Info);
                Palette(null);
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.LabelField($"{boardId}: {state.Board.Hexes.Count} hexes. Edits go to {boardId}{HexBoardOverrides.Suffix}.json");
            pick = EditorGUILayout.ToggleLeft("Pick hexes in the Scene view", pick);
            mode = (Mode)GUILayout.Toolbar((int)mode, ModeLabels);
            if (mode == Mode.PaintTile) EditorGUILayout.HelpBox(brushTile == null ? "Pick a tile in the palette below." : $"Click or drag over hexes to paint {(brushTile == "" ? "no tile" : brushTile)}.", MessageType.None);
            if (mode == Mode.PlaceUnit) EditorGUILayout.HelpBox(brushUnit == "" ? "Pick a unit in the palette below." : $"Click a hex to stand {brushUnit} on it, facing {HexPalette.Degrees(brushFacing)} deg.", MessageType.None);

            EditorGUILayout.Space();
            SelectedHex(state);
            if (status != "") EditorGUILayout.HelpBox(status, MessageType.None);
            Palette(state);
            EditorGUILayout.EndScrollView();
        }

        void TimeOfDay()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Time of day (preview)", GUILayout.Width(140));
                int now = (int)Switches.Time;
                int next = GUILayout.Toolbar(now, new[] { "Day", "Dusk", "Night" });
                if (next != now) SetTime((HexTime)next);
            }
        }

        void SetTime(HexTime time)
        {
            try { HexPainterStage.SetTime(Switches, time, root); status = $"time of day: {HexTimeOfDay.Id(time)} (preview, not saved)"; }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
            SceneView.RepaintAll();
        }

        void SelectedHex(HexBoardState state)
        {
            if (selected == null || !state.Board.Hexes.TryGetValue(selected.Value, out var h))
            {
                EditorGUILayout.HelpBox("No hex selected.", MessageType.None);
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
            if (!Mathf.Approximately(facing, h.facing)) Apply(q, r, () => HexPalette.Face(root!, OverridesPath, q, r, facing), $"facing = {HexPalette.Degrees(facing)}");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(h.unit == ""))
                {
                    foreach (var (label, deg) in HexPalette.Facings)
                        if (GUILayout.Button(label, GUILayout.Width(38))) Apply(q, r, () => HexPalette.Face(root!, OverridesPath, q, r, deg), $"facing = {HexPalette.Degrees(deg)}");
                    if (GUILayout.Button("Clear unit")) Apply(q, r, () => HexPalette.ClearUnit(root!, OverridesPath, q, r), "unit cleared");
                }
            }
            bool attacked = Switches.IsAttacked(q, r);
            bool want = EditorGUILayout.ToggleLeft("Under attack (preview, not saved)", attacked);
            if (want != attacked)
            {
                try { Switches.SetAttack(root!.transform, state.Surface, q, r, want); }
                catch (Exception e) { status = e.Message; Debug.LogException(e); }
                SceneView.RepaintAll();
            }
        }

        /// <summary>The palette: tiles by kind and region, then units by characters and vehicles, each a thumbnail (or its
        /// id when none was made) that becomes the brush. The unit brush has six hex-direction facings and a free angle.</summary>
        void Palette(HexBoardState? state)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                filter = EditorGUILayout.TextField("Filter", filter);
                if (GUILayout.Button("Refresh", GUILayout.Width(70))) RefreshPalette();
                if (GUILayout.Button("Make thumbnails", GUILayout.Width(120))) MakeThumbnails();
            }
            EditorGUILayout.LabelField("Tiles", EditorStyles.miniBoldLabel);
            foreach (var g in tiles) Group(g, brushTile ?? "", id => { brushTile = id; mode = Mode.PaintTile; });
            if (GUILayout.Button("Brush: no tile (clears a hex's prototype)")) { brushTile = ""; mode = Mode.PaintTile; }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Units", EditorStyles.miniBoldLabel);
            foreach (var g in units) Group(g, brushUnit, id => { brushUnit = id; mode = Mode.PlaceUnit; });
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Unit facing", GUILayout.Width(80));
                foreach (var (label, deg) in HexPalette.Facings)
                    if (GUILayout.Toggle(Mathf.Approximately(brushFacing, deg), label, EditorStyles.miniButton, GUILayout.Width(38))) brushFacing = deg;
                brushFacing = EditorGUILayout.FloatField(brushFacing, GUILayout.Width(60));
            }
            using (new EditorGUI.DisabledScope(state == null || selected == null || brushUnit == ""))
                if (GUILayout.Button("Stand the unit on the selected hex") && selected != null) PlaceUnit(selected.Value.q, selected.Value.r);
        }

        void Group(HexPaletteGroup g, string current, Action<string> choose)
        {
            var shown = g.Entries.Where(e => filter == "" || e.Id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || e.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (shown.Count == 0) return;
            bool open = !folded.Contains(g.Name);
            bool next = EditorGUILayout.Foldout(open, $"{g.Name} ({shown.Count})", true);
            if (next != open) { if (next) folded.Remove(g.Name); else folded.Add(g.Name); }
            if (!next) return;
            int columns = Mathf.Max(1, (int)((position.width - 30f) / (Cell + 4f)));
            for (int i = 0; i < shown.Count; i += columns)
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var e in shown.Skip(i).Take(columns))
                    {
                        var tex = Thumb(e);
                        var content = tex != null ? new GUIContent(tex, $"{e.Id}\n{e.Title}") : new GUIContent(e.Id, e.Title);
                        var style = tex != null ? GUI.skin.button : new GUIStyle(GUI.skin.button) { wordWrap = true, fontSize = 9 };
                        bool on = e.Id == current;
                        if (GUILayout.Toggle(on, content, style, GUILayout.Width(Cell), GUILayout.Height(Cell)) && !on) choose(e.Id);
                    }
            var chosen = shown.FirstOrDefault(e => e.Id == current);
            if (chosen != null) EditorGUILayout.LabelField("  " + chosen.Title, EditorStyles.miniLabel);
        }

        string OverridesPath => HexPainter.OverridesPath(BoardsDir, boardId);

        /// <summary>A dropdown of <paramref name="options"/> (with "(none)" for an empty value when allowed); a new pick
        /// is applied at once.</summary>
        void Choice(int q, int r, string field, string current, string[] options, bool allowNone)
        {
            var shown = allowNone ? new[] { None }.Concat(options).ToArray() : options;
            int at = Array.IndexOf(shown, current == "" && allowNone ? None : current);
            if (at < 0) { shown = shown.Append(current).ToArray(); at = shown.Length - 1; }   // a value no longer registered
            int next = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(field), at, shown);
            if (next == at) return;
            var value = shown[next] == None ? "" : shown[next];
            Apply(q, r, () => HexPainter.Change(root!, OverridesPath, q, r, field, value), $"{field} = '{value}'");
        }

        /// <summary>Runs one edit of hex (q, r), re-imports the overrides, and re-shows its under-attack preview.</summary>
        void Apply(int q, int r, Func<HexTileReport?> edit, string what)
        {
            if (root == null) return;
            try
            {
                var report = edit();
                HexPainter.Import(OverridesPath);
                Switches.Rebuilt(q, r);
                status = $"({q},{r}) {what}, written to {Path.GetFileName(OverridesPath)}\n" + (report?.ToString() ?? "no tile on this hex");
            }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
            SceneView.RepaintAll();
        }

        void PaintTile(int q, int r, string id) => Apply(q, r, () =>
        {
            HexPalette.Paint(root!, OverridesPath, new[] { (q, r) }, id);
            return State!.Reports.TryGetValue((q, r), out var report) ? report : null;
        }, $"painted '{id}'");

        void PlaceUnit(int q, int r) =>
            Apply(q, r, () => HexPalette.PlaceUnit(root!, OverridesPath, q, r, brushUnit, brushFacing), $"unit = '{brushUnit}', facing {HexPalette.Degrees(brushFacing)}");

        void Load(string id)
        {
            UnloadAll();
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

        /// <summary>Undoes the previews (while the board still stands) and removes every painter board from the scene.</summary>
        public void UnloadAll()
        {
            Switches.Clear();
            foreach (var old in FindObjectsByType<HexBoardState>(FindObjectsSortMode.None)) Unload(old.gameObject);
            root = null; selected = null;
        }

        static void Unload(GameObject board)
        {
            board.GetComponent<HexBoardAssets>()?.Teardown();
            DestroyImmediate(board);
        }

        /// <summary>Makes every thumbnail in a new empty scene (after asking to save the open one), then reopens the scene
        /// and the board.</summary>
        void MakeThumbnails()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string reload = root != null ? boardId : "";
            UnloadAll();
            var scene = EditorSceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var made = HexPaletteThumbs.MakeAll(HexPalette.ThumbsDir);
                status = $"{made.Count} thumbnails in {HexPalette.ThumbsDir}";
            }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
            finally
            {
                if (scene != "") EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RefreshPalette();
                if (reload != "") Load(reload);
            }
        }

        void OnScene(SceneView view)
        {
            var state = State;
            if (root == null || state == null) return;
            var e = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (pick && e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
            if (pick && e.button == 0 && !e.alt && (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && mode == Mode.PaintTile)))
            {
                if (e.type == EventType.MouseDown) stroke.Clear();
                var hit = HexPainter.Pick(root, HandleUtility.GUIPointToWorldRay(e.mousePosition));
                if (hit != null)
                {
                    var (q, r) = hit.Value;
                    bool changed = selected != hit;
                    selected = hit; if (changed) status = "";
                    if (mode == Mode.PaintTile && brushTile != null && stroke.Add(hit.Value)) PaintTile(q, r, brushTile);
                    else if (mode == Mode.PlaceUnit && e.type == EventType.MouseDown && brushUnit != "") PlaceUnit(q, r);
                    Repaint(); e.Use();
                }
            }
            if (e.type == EventType.MouseUp) stroke.Clear();
            if (selected != null && e.type == EventType.Repaint)
            {
                var (q, r) = selected.Value;
                var corners = HexGeometry.Corners(HexGeometry.Centre(q, r, HexScale.HexM), HexScale.HexM)
                    .Select(c => root.transform.TransformPoint(new Vector3(c.x, Mathf.Max(state.Surface.Height(c), 0f) + 60f, c.z))).ToList();
                corners.Add(corners[0]);
                Handles.color = mode == Mode.PaintTile ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.85f, 0.2f);
                Handles.DrawAAPolyLine(6f, corners.ToArray());
            }
        }
    }
}
