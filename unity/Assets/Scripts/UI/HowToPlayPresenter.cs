#nullable enable
using UnityEngine.UIElements;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the short, presentation-only guide to the simulation's existing turn loop (#1041).</summary>
    public sealed class HowToPlayPresenter : System.IDisposable
    {
        readonly VisualElement screen;

        public HowToPlayPresenter(VisualElement root, UiText text, string ruleset = SaveGame.GddRuleset)
        {
            screen = root.Q<VisualElement>("screen-how-to-play");
            screen.Q<Label>("how-to-play-title").text = text["how_to_play.title"];
            bool gdd03 = ruleset == SaveGame.Gdd03Ruleset;
            foreach (var key in new[] { "turn", "attention", "city", "training", "events", "top_bar", "end_turn", "why" })
            {
                var line = screen.Q<Label>("how-to-play-" + key.Replace('_', '-'));
                if (key == "attention")
                {
                    line.text = gdd03 ? "" : text["how_to_play.attention"];
                    line.style.display = gdd03 ? DisplayStyle.None : DisplayStyle.Flex;
                    continue;
                }
                bool variant = gdd03 && (key == "events" || key == "top_bar" || key == "end_turn");
                line.text = text["how_to_play." + (variant ? "gdd03." : "") + key];
            }
        }

        public void Dispose() { }
    }
}
