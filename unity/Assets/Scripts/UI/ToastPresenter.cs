#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A short message under the top bar: why orders were refused (spec section 7: the UI shows the stated
    /// reason), or a note such as a save warning. Hidden when there is nothing to say; cleared by the next order, except a
    /// failed save, which stays under the other lines until a save succeeds (#2707).</summary>
    public sealed class ToastPresenter : System.IDisposable
    {
        readonly Label label; readonly UiText text; readonly SimBridge bridge;
        readonly Action<IReadOnlyList<Rejection>> onTurnEnded, onRouteStep;
        readonly Action<IReadOnlyList<Cause>> onCommitted;
        IReadOnlyList<UnitEntry> units = Array.Empty<UnitEntry>();
        string shown = "";              // the lines Show and Append set; Clear and the next order replace them
        string? saveFailure;            // SaveFailed's line, kept until SaveSucceeded
        string? recordingFailure;       // RecordingFailed's line, kept until the retry or the next order

        /// <summary>The units_1948.yaml rows that name starting forces in the committed orders' Why lines (GameRoot sets
        /// them once the map has read them, as for WhyPresenter); without them a starting force is named by its type.</summary>
        public IReadOnlyList<UnitEntry> Units { get => units; set => units = value ?? Array.Empty<UnitEntry>(); }
        /// <summary>Each settlement's Hebrew name, by id (the map's parse), for a raised unit's home city in Hebrew.</summary>
        public IReadOnlyDictionary<string, string>? TownNamesHe { get; set; }

        public ToastPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            label = root.Q<Label>("toast"); this.text = text; this.bridge = bridge;
            onTurnEnded = r => Show(r.Select(x => text.Format("toast.refused", SimBridge.ReasonText(text, x.Reason, bridge.Content, TownNamesHe))));
            onCommitted = why => Show(why.Where(x => WhyKeys.All.Contains(x.Key))   // unit lines only: a call-up order's line (B1 key) is no toast
                .Select(x => UnitNames.Line(x, bridge.Content, units, text, TownNamesHe)));
            onRouteStep = _ =>
            {
                var lines = bridge.Session.Now.Why
                    .Where(x => x.Key == WhyKeys.Arrived || x.Key == WhyKeys.StoppedNoRoute || x.Key == WhyKeys.StoppedFull || x.Key == WhyKeys.CardOrderEnded)
                    .Select(x => UnitNames.Line(x, bridge.Content, units, text, TownNamesHe)).ToList();
                if (lines.Count > 0) Append(string.Join("\n", lines));
            };
            bridge.TurnEnded += onTurnEnded;
            bridge.TurnEnded += onRouteStep;
            bridge.Refused += Refuse;
            bridge.Changed += Clear;
            bridge.Committed += onCommitted;
            Clear();
        }

        public void Dispose() { bridge.TurnEnded -= onTurnEnded; bridge.TurnEnded -= onRouteStep; bridge.Refused -= Refuse; bridge.Changed -= Clear; bridge.Committed -= onCommitted; }

        /// <summary>Shows the lines as they are, a failed recording's line and a failed save's line under them (none of either = hide).</summary>
        public void Show(IEnumerable<string> lines)
        {
            shown = string.Join("\n", lines);
            var all = new[] { shown, recordingFailure ?? "", saveFailure ?? "" }.Where(x => x.Length > 0).ToList();
            label.text = string.Join("\n", all);
            label.style.display = all.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(string line) => Show(new[] { line });

        /// <summary>Adds a line under what is shown (alone when nothing is), e.g. a unit's route step after the turn's
        /// refusals.</summary>
        public void Append(string line) => Show(shown.Length > 0 ? shown + "\n" + line : line);

        /// <summary>A save failed: its line stays under whatever else is shown, through the next order, until
        /// <see cref="SaveSucceeded"/> (autosave and New Game, and on WebGL the IndexedDB result).</summary>
        public void SaveFailed(string line) { saveFailure = line; Show(shown); }

        /// <summary>The start screen's recording could not be played: its line goes under the lines shown (the start-up
        /// notes), not in place of them, until the next press of its button or the next change, such as an order (#2707).</summary>
        public void RecordingFailed(string line) { recordingFailure = line; Show(shown); }

        /// <summary>The recording's button was pressed again: its failure line goes; the other lines stay.</summary>
        public void RecordingRetried()
        {
            if (recordingFailure == null) return;
            recordingFailure = null;
            Show(shown);
        }

        /// <summary>A save succeeded: a failed save's line goes; the other lines stay.</summary>
        public void SaveSucceeded()
        {
            if (saveFailure == null) return;
            saveFailure = null;
            Show(shown);
        }

        /// <summary>Shows why an order was refused ("Not done: ...").</summary>
        public void Refuse(string reason) => Show(text.Format("toast.refused", reason));

        public void Clear() { recordingFailure = null; Show(new string[0]); }
    }
}
