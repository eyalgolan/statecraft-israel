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
    /// reason), or a note such as a save warning. Hidden when there is nothing to say; cleared by the next order.</summary>
    public sealed class ToastPresenter : System.IDisposable
    {
        readonly Label label; readonly UiText text; readonly SimBridge bridge;
        readonly Action<IReadOnlyList<Rejection>> onTurnEnded, onRouteStep;
        readonly Action<IReadOnlyList<Cause>> onCommitted;
        IReadOnlyList<UnitEntry> units = Array.Empty<UnitEntry>();

        /// <summary>The units_1948.yaml rows that name starting forces in the committed orders' Why lines (GameRoot sets
        /// them once the map has read them, as for WhyPresenter); without them a starting force is named by its type.</summary>
        public IReadOnlyList<UnitEntry> Units { get => units; set => units = value ?? Array.Empty<UnitEntry>(); }
        /// <summary>Each settlement's Hebrew name, by id (the map's parse), for a raised unit's home city in Hebrew.</summary>
        public IReadOnlyDictionary<string, string>? TownNamesHe { get; set; }

        public ToastPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            label = root.Q<Label>("toast"); this.text = text; this.bridge = bridge;
            onTurnEnded = r => Show(r.Select(x => text.Format("toast.refused", x.Reason)));
            onCommitted = why => Show(why.Where(x => !string.IsNullOrEmpty(x.Key))
                .Select(x => UnitNames.Line(x, bridge.Content, units, text, TownNamesHe)));
            onRouteStep = _ =>
            {
                var lines = bridge.Session.Now.Why
                    .Where(x => x.Key == WhyKeys.Arrived || x.Key == WhyKeys.StoppedNoRoute || x.Key == WhyKeys.StoppedFull)
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

        /// <summary>Shows the lines as they are (none = hide).</summary>
        public void Show(IEnumerable<string> lines)
        {
            var list = lines.ToList();
            label.text = string.Join("\n", list);
            label.style.display = list.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(string line) => Show(new[] { line });

        /// <summary>Adds a line under what is shown (alone when nothing is), e.g. a failed autosave after the turn's
        /// refusals.</summary>
        public void Append(string line) =>
            Show(label.style.display.value == DisplayStyle.Flex && label.text.Length > 0 ? label.text + "\n" + line : line);

        /// <summary>Shows why an order was refused ("Not done: ...").</summary>
        public void Refuse(string reason) => Show(text.Format("toast.refused", reason));

        public void Clear() => Show(new string[0]);
    }
}
