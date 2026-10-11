#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The event card (right of the screen): the first pending event the player has not yet answered, with the
    /// responsible minister's portrait, the text with today's office holders filled in, and one button per option.
    /// A choice is an order: it takes effect at End Turn, and an unanswered event is delegated to the minister.</summary>
    public sealed class EventCardPresenter : System.IDisposable
    {
        /// <summary>The portrait thumbnail's size in the card (Statecraft.uss .event-image).</summary>
        public const float PortraitWidth = 96f, PortraitHeight = 128f;
        /// <summary>The class on each option button (Statecraft.uss).</summary>
        public const string OptionClass = "event-option";

        readonly VisualElement card; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        
        public EventCardPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits)
        {
            card = root.Q<VisualElement>("event-card"); this.bridge = bridge; this.text = text; this.portraits = portraits;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge, removes the option buttons and hides the card (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; card.Q<VisualElement>("event-options")?.Clear(); card.style.display = DisplayStyle.None; }

        /// <summary>The event the card shows, or null when every pending event has a queued answer.</summary>
        public GameEvent? Shown { get; private set; }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var pending = s.PendingEvents.Select(id => c.Events.First(e => e.Id == id)).ToList();
            var open = pending.Where(e => bridge.QueuedLike(new ChooseEventOption(e.Id, "")) == null).ToList();
            Shown = open.FirstOrDefault();
            card.style.display = Shown == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Shown == null) return;
            var e = Shown;
            var date = EventSystem.FillDate(e, s.Date);
            var holder = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office, date);
            var image = card.Q<VisualElement>("event-image");
            var entry = portraits?.Find(holder?.Id);
            var portrait = entry?.image;
            image.style.backgroundImage = portrait == null ? (StyleBackground)StyleKeyword.None : new StyleBackground(portrait);
            image.style.display = portrait == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (portrait != null)   // keep the face in view (playtest 2, P2-5)
                image.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top,
                    new Length(PortraitFraming.PositionYPercent(portrait.width, portrait.height, PortraitWidth, PortraitHeight, entry!.Value.focusY), LengthUnit.Percent));
            card.Q<Label>("event-draft").style.display = e.Status == "draft" ? DisplayStyle.Flex : DisplayStyle.None;
            card.Q<Label>("event-title").text = Cabinet.Fill(c, s, e.Title, date);
            card.Q<Label>("event-office").text = holder == null ? "" : text.Format("event.office", c.Offices.First(o => o.Id == e.Office).Title, holder.Name);
            card.Q<Label>("event-text").text = EventSystem.Render(c, s, e);
            var options = card.Q<VisualElement>("event-options");
            options.Clear();
            foreach (var o in e.Options)
            {
                var option = o;
                var b = new Button(() => Choose(e, option)) { name = "option-" + o.Id, text = Cabinet.Fill(c, s, o.Text, date) };
                b.AddToClassList(OptionClass);
                b.generateVisualContent += PaintSign;                      // the promo's round sign at the option's left
                options.Add(b);
            }
            card.Q<Label>("event-count").text = open.Count > 1 ? text.Format("event.more", open.Count - 1) : "";
        }

        /// <summary>The round sign in an option's left padding (Statecraft.uss .event-option), painted into the button so it
        /// stays a leaf whose wrapped text Yoga measures (UiIcon.Paint).</summary>
        const float SignRadius = 15f, SignLeft = 9f;

        static void PaintSign(MeshGenerationContext m) =>
            UiIcon.PaintDisc(m, "chevron", new UnityEngine.Vector2(SignLeft + SignRadius, m.visualElement.layout.height / 2f), SignRadius);

        void Choose(GameEvent e, EventOption o)
        {
            // The bridge raises Refused (the toast shows it) or Changed (the toast clears); nothing more to do here.
            bridge.Queue(new ChooseEventOption(e.Id, o.Id));
        }
    }
}
