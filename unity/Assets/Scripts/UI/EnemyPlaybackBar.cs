#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 6): the footer bar while the enemy's
    /// moves play. The owner's answers (8 November 2026): "Flag + name (Recommended)", so it shows the side whose turn it is
    /// as the flag its tokens fly (<see cref="SideFlag"/>, the neutral pennant for the Arab Liberation Army) beside the side's
    /// name in words (playback.turn_&lt;side&gt;), and "One side at a time (Recommended)", so the picture changes once a side
    /// (the beats come side by side, UnitBeats.From). Under the name, "Move n of m" and each beat's action in words, the turn
    /// report's own lines (report.enemy_move, report.enemy_attack through UnitNames.Of), so the motion is never the only
    /// signal (WRITING_UI UI-18). Skip ends the playback (as Esc does, GameRoot.Update); its tooltip holds the spoken text
    /// for the later accessibility layer: no runtime tooltip is read by a screen reader yet.
    /// <para>Never modal: it takes no focus when it opens, and only the Skip button takes the pointer. A clip (Game.uxml
    /// order) is drawn over it. In Hebrew the row is mirrored (the flag at the right), the flag itself is not.</para></summary>
    public sealed class EnemyPlaybackBar : IDisposable
    {
        public const string ElementName = "enemy-playback";
        /// <summary>On the "root" element while the bar shows.</summary>
        public const string OpenClass = "enemy-playback-open";

        /// <summary>The units_1948.yaml rows that name the enemy's units (GameRoot sets them once the map has read them).</summary>
        public IReadOnlyList<UnitEntry>? Units { get; set; }

        readonly VisualElement hudRoot, bar, actions;
        readonly SideFlag flag;
        readonly Label side, step;
        readonly Button skip;
        readonly UiText text; readonly EnemyTurnPlayback playback; readonly SimBridge bridge;
        readonly Action onRender;
        string? shownSide;

        public EnemyPlaybackBar(VisualElement root, UiText text, EnemyTurnPlayback playback, SimBridge bridge)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.playback = playback ?? throw new ArgumentNullException(nameof(playback));
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            hudRoot = root.Q("root") ?? root;
            bar = root.Q<VisualElement>(ElementName);
            var panel = bar.Q<VisualElement>("enemy-playback-panel");
            flag = new SideFlag { name = "enemy-playback-flag" };
            panel.Insert(0, flag);
            side = bar.Q<Label>("enemy-playback-side");
            step = bar.Q<Label>("enemy-playback-step");
            actions = bar.Q<VisualElement>("enemy-playback-actions");
            skip = bar.Q<Button>("enemy-playback-skip");
            skip.text = text["playback.skip"];
            skip.tooltip = text["playback.skip_tip"];
            skip.focusable = true;
            skip.clicked += OnSkip;
            onRender = Render;
            playback.Started += onRender;
            playback.BeatChanged += onRender;
            playback.Finished += onRender;
            Render();
        }

        /// <summary>True while the bar shows.</summary>
        public bool IsOpen => hudRoot.ClassListContains(OpenClass);
        /// <summary>The side the bar names now, or null.</summary>
        public string? Side => shownSide;
        public SideFlag Flag => flag;

        /// <summary>The side's name in words: playback.turn_&lt;side&gt;, or the Arab forces' as a whole for a side with no key.</summary>
        public static string SideName(UiText text, string? side) =>
            !string.IsNullOrEmpty(side) && text.Has("playback.turn_" + side) ? text["playback.turn_" + side] : text["playback.title"];

        /// <summary>The beat's actions in words, one line each, in the sim's order: each move's unit, each engagement's enemy
        /// attackers; a folded beat says so in one line.</summary>
        public static IReadOnlyList<string> Lines(UnitBeat beat, Content content, IReadOnlyList<UnitEntry>? units, UiText text)
        {
            if (beat.Folded) return new[] { text["playback.folded"] };
            var rows = units ?? Array.Empty<UnitEntry>();
            string Name(Cause c, int id)
            {
                var snapshot = c.Units.FirstOrDefault(u => u.Id == id);
                return snapshot != null ? UnitNames.Of(snapshot, content, rows, text) : text.Format("report.unit", id);
            }
            var lines = new List<string>();
            foreach (var c in beat.Causes)
            {
                if (c.Key == WhyKeys.UnitMoved && c.Units.Count > 0) lines.Add(text.Format("report.enemy_move", Name(c, c.Units[0].Id)));
                else if (c.Key == WhyKeys.Engagement)
                    foreach (var id in c.EnemyAttackers) lines.Add(text.Format("report.enemy_attack", Name(c, id)));
            }
            return lines;
        }

        void OnSkip() => playback.Skip();

        void Render()
        {
            var beat = playback.Current;
            bool open = playback.Active && beat != null;
            hudRoot.EnableInClassList(OpenClass, open);
            bar.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open) { shownSide = null; flag.Side = null; actions.Clear(); return; }
            if (!string.Equals(shownSide, beat!.Side, StringComparison.Ordinal))   // the picture changes once a side
            {
                shownSide = beat.Side;
                flag.Side = beat.Side;
                side.text = SideName(text, beat.Side);
            }
            step.text = text.Format("playback.step", playback.Index + 1, playback.Count);
            actions.Clear();
            foreach (var line in Lines(beat, bridge.Content, Units, text))
            {
                var label = new Label(line);   // one label a line, so each takes its own direction (UiDirection)
                label.AddToClassList("enemy-playback-action");
                actions.Add(label);
            }
        }

        public void Dispose()
        {
            skip.clicked -= OnSkip;
            playback.Started -= onRender;
            playback.BeatChanged -= onRender;
            playback.Finished -= onRender;
            hudRoot.RemoveFromClassList(OpenClass);
        }
    }
}
