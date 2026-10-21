#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the short, presentation-only guide to the simulation's existing turn loop (#1041).</summary>
    public sealed class HowToPlayPresenter : System.IDisposable
    {
        readonly VisualElement screen;

        public HowToPlayPresenter(VisualElement root, UiText text)
        {
            screen = root.Q<VisualElement>("screen-how-to-play");
            screen.Q<Label>("how-to-play-title").text = text["how_to_play.title"];
            foreach (var key in new[] { "turn", "attention", "city", "training", "events", "top_bar", "end_turn", "why" })
                screen.Q<Label>("how-to-play-" + key.Replace('_', '-')).text = text["how_to_play." + key];
        }

        public void Dispose() { }
    }
}
