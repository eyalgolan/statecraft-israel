#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The Hex Painter's path without its window, for checking it from the command line: loads a board as the
    /// painter does and runs a list of palette operations on one hex, shooting the hex before and after each. Edits go
    /// through HexPainter / HexPalette and are written to -overrides (by default a file in the capture folder, so the
    /// board's own overrides are untouched); the time-of-day and under-attack switches go through HexPainterSwitches and
    /// are undone after their shots, as the window undoes them.
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Painter.HexPainterCapture.Run -board israel -hex -3,19
    ///   [-field prototype -value kibbutz | -field unit -value palmach_squad -facing 120] [-time dusk|night] [-attack]
    ///   [-ops "prototype=kibbutz;unit=palmach_squad@120;time=dusk;time=night;attack"] [-thumbs]
    ///   [-overrides &lt;path&gt;] [-captureDir asset-staging/ui-local/hex-tiles/painter] [-captureSize 3840x2160]
    /// -ops replaces -field, -time and -attack. -thumbs first makes the palette thumbnails (HexPaletteThumbs) and writes the
    /// contact sheet &lt;dir&gt;/palette.png and palette.txt. Writes &lt;dir&gt;/&lt;n&gt;-&lt;op&gt;/before.png and after.png per operation and
    /// &lt;dir&gt;/painter.txt (each operation, its time, the tile report, the overrides written).</summary>
    public static class HexPainterCapture
    {
        /// <summary>One operation: a field edit (prototype, ground, region, facing), a unit with a facing, a time of day,
        /// or the under-attack look.</summary>
        public sealed class Op
        {
            public string Kind = "", Value = ""; public float Facing;
            public string Folder(int n) => $"{n}-{Kind}" + (Value != "" ? "-" + Value : "") + (Kind == "unit" && Value != "" ? "-" + HexPalette.Degrees(Facing) : "");
            public override string ToString() => Kind == "attack" ? "attack" : Kind == "unit" && Value != "" ? $"unit={Value}@{HexPalette.Degrees(Facing)}" : $"{Kind}={Value}";
        }

        /// <summary>Parses "prototype=kibbutz;unit=palmach_squad@120;time=dusk;attack".</summary>
        public static List<Op> ParseOps(string ops) => ops.Split(';').Select(s => s.Trim()).Where(s => s != "").Select(s =>
        {
            if (s == "attack") return new Op { Kind = "attack" };
            int eq = s.IndexOf('=');
            if (eq < 0) throw new ArgumentException($"operation '{s}' is not field=value, time=..., or attack");
            var op = new Op { Kind = s.Substring(0, eq), Value = s.Substring(eq + 1) };
            int at = op.Value.IndexOf('@');
            if (op.Kind == "unit" && at >= 0) { op.Facing = float.Parse(op.Value.Substring(at + 1), CultureInfo.InvariantCulture); op.Value = op.Value.Substring(0, at); }
            if (op.Kind == "time" && !HexTimeOfDay.TryParse(op.Value, out _)) throw new ArgumentException($"time '{op.Value}' is not one of {string.Join(", ", HexTimeOfDay.Ids)}");
            if (op.Kind != "time") HexPainter.Validate(op.Kind, op.Value);
            return op;
        }).ToList();

        /// <summary>The operations from the command line: -ops, else -field/-value(/-facing), -time and -attack in that order.</summary>
        static List<Op> OpsFromArgs()
        {
            var ops = HexBoardCapture.Arg("-ops");
            if (ops != null) return ParseOps(ops);
            var parts = new List<string>();
            var field = HexBoardCapture.Arg("-field");
            if (field != null)
            {
                var facing = HexBoardCapture.Arg("-facing");
                parts.Add($"{field}={HexBoardCapture.Arg("-value") ?? ""}" + (field == "unit" && facing != null ? "@" + facing : ""));
            }
            if (HexBoardCapture.Arg("-time") is string time) parts.Add("time=" + time);
            if (Environment.GetCommandLineArgs().Contains("-attack")) parts.Add("attack");
            if (parts.Count == 0) throw new ArgumentException("nothing to do: give -field/-value, -time, -attack or -ops");
            return ParseOps(string.Join(";", parts));
        }

        public static void Run()
        {
            try
            {
                var repo = HexPalette.RepoRoot;
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/painter"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "3840x2160").Split('x');
                var hex = (HexBoardCapture.Arg("-hex") ?? throw new ArgumentException("-hex q,r")).Split(',');
                var ops = OpsFromArgs();
                Directory.CreateDirectory(dir);
                if (Environment.GetCommandLineArgs().Contains("-thumbs"))
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var made = HexPaletteThumbs.MakeAll(HexPalette.ThumbsDir);
                    HexPaletteThumbs.Sheet(HexPalette.ThumbsDir, Path.Combine(dir, "palette.png"), Path.Combine(dir, "palette.txt"));
                    Debug.Log($"HEX PAINTER thumbnails: {made.Count} in {HexPalette.ThumbsDir}; contact sheet in {dir}");
                }
                var overrides = HexBoardCapture.Arg("-overrides") ?? Path.Combine(dir, "painter.overrides.json");
                if (HexBoardCapture.Arg("-overrides") == null && File.Exists(overrides)) File.Delete(overrides);
                Shoot(HexBoardCapture.Arg("-board") ?? "israel", int.Parse(hex[0]), int.Parse(hex[1]), ops, overrides, dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX PAINTER CAPTURE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>A unit or the attack look is shot at one hex's distance; the others at about three hexes.</summary>
        static float Distance(Op op) => op.Kind == "unit" || op.Kind == "attack" ? 12500f : 26000f;

        static void Shoot(string boardId, int q, int r, List<Op> ops, string overrides, string dir, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = HexBoardCapture.Stage();
            var clock = Stopwatch.StartNew();
            var root = HexPainter.Load(boardId);
            long loadMs = clock.ElapsedMilliseconds;
            var switches = new HexPainterSwitches();
            var lines = new List<string> { $"board {boardId}: loaded in {loadMs} ms; hex ({q},{r}); {width}x{height}" };
            try
            {
                var state = root.GetComponent<HexBoardState>();
                var c = HexGeometry.Centre(q, r, HexScale.HexM);
                c.y = Mathf.Max(state.Surface.Height(c), 0f);
                var h = state.Board.Hexes[(q, r)];
                for (int i = 0; i < ops.Count; i++)
                {
                    var op = ops[i];
                    var sub = Path.Combine(dir, op.Folder(i + 1));
                    Directory.CreateDirectory(sub);
                    HexBoardCapture.Shoot(cam, c, Distance(op), Path.Combine(sub, "before.png"), width, height);
                    clock.Restart();
                    string line;
                    switch (op.Kind)
                    {
                        case "time":
                            HexTimeOfDay.TryParse(op.Value, out var time);
                            HexPainterStage.SetTime(switches, time, root);
                            line = $"time {op.Value} applied in {clock.ElapsedMilliseconds} ms (preview: undone after the shot)";
                            break;
                        case "attack":
                            switches.SetAttack(root.transform, state.Surface, q, r, true);
                            line = $"under attack shown in {clock.ElapsedMilliseconds} ms, object '{HexUnderAttack.Name(q, r)}' present: {HexUnderAttack.IsShown(root.transform, q, r)} (preview: hidden after the shot)";
                            break;
                        case "unit" when op.Value != "":
                        {
                            string was = $"'{h.unit}' facing {HexPalette.Degrees(h.facing)}";
                            var report = HexPalette.PlaceUnit(root, overrides, q, r, op.Value, op.Facing);
                            line = $"unit {was} -> '{op.Value}' facing {HexPalette.Degrees(op.Facing)}, rebuilt in {clock.ElapsedMilliseconds} ms; tile: {report?.ToString() ?? "none"}";
                            break;
                        }
                        case "prototype":
                        {
                            string was = h.prototype;
                            HexPalette.Paint(root, overrides, new[] { (q, r) }, op.Value);
                            line = $"prototype '{was}' -> '{op.Value}' (brush), rebuilt in {clock.ElapsedMilliseconds} ms; tile: {(state.Reports.TryGetValue((q, r), out var t) ? t.ToString() : "none")}";
                            break;
                        }
                        default:
                        {
                            string was = HexBoardOverrides.Get(h, op.Kind);
                            var report = HexPainter.Change(root, overrides, q, r, op.Kind, op.Value);
                            line = $"{op.Kind} '{was}' -> '{op.Value}', rebuilt in {clock.ElapsedMilliseconds} ms; tile: {report?.ToString() ?? "none"}";
                            break;
                        }
                    }
                    if (op.Kind != "time" && op.Kind != "attack") switches.Rebuilt(q, r);
                    HexBoardCapture.Shoot(cam, c, Distance(op), Path.Combine(sub, "after.png"), width, height);
                    switches.Clear();
                    lines.Add($"{i + 1}. {op} -> {op.Folder(i + 1)}/: {line}");
                }
                lines.Add($"switches after the run: time {HexTimeOfDay.Id(switches.Time)}, under attack on {switches.Attacked.Count} hexes, object present: {HexUnderAttack.IsShown(root.transform, q, r)}");
                lines.Add("overrides written to " + overrides + ":");
                lines.Add(File.Exists(overrides) ? File.ReadAllText(overrides) : "(none: no edit was written)");
                File.WriteAllLines(Path.Combine(dir, "painter.txt"), lines);
                Debug.Log("HEX PAINTER " + string.Join(" | ", lines) + " -> " + dir);
            }
            finally
            {
                switches.Clear();
                TiltShift.Reset();
                Object.DestroyImmediate(root);
            }
        }
    }
}
