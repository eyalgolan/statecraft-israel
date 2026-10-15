#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The decision card, in the middle of the screen under the bar (decision cards, 2026-10-13): the first pending
    /// event the player has not yet answered, with its 16:9 media header (an archival photograph of the event with its credit
    /// line, as the promo's card; EventImageCatalog) or, when the event has none, the responsible minister's portrait, the
    /// text with today's office holders filled in, and one button per option.
    /// While it shows, the root has OpenClass: a backdrop dims the map and takes the pointer, and ScreenNav.DisableCovered
    /// takes the panels under it out of reach. Hide collapses it to the "Decision waiting" button, which brings it back; a
    /// new event opens it again by itself. A new event puts the keyboard focus on the card itself, not on an
    /// option, so no choice is lit as if recommended; Tab then reaches the options in order.
    /// A choice is an order: it takes effect at End Turn, and an unanswered event is delegated to the minister.</summary>
    public sealed class EventCardPresenter : System.IDisposable
    {
        /// <summary>The portrait thumbnail's size in the card (Statecraft.uss .event-image).</summary>
        public const float PortraitWidth = 96f, PortraitHeight = 128f;
        /// <summary>The class on each option button (Statecraft.uss).</summary>
        public const string OptionClass = "event-option";
        /// <summary>On the "root" element while the card shows (not collapsed): Statecraft.uss shows the backdrop.</summary>
        public const string OpenClass = "event-open";
        /// <summary>On the "root" element while the card shows and no start screen or screen covers it (set by
        /// ScreenNav.DisableCovered): the panels under the backdrop are disabled and the toast moves clear of the card.</summary>
        public const string ModalClass = "event-modal";
        /// <summary>The media header's width over its height (the photographs and the clips are 960x540).</summary>
        public const float MediaAspect = 16f / 9f;

        readonly VisualElement card; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly EventImageCatalog? photos;
        /// <summary>The catalog when the first event shows (EventPhotos.Get: off the boot path, issue #321), when photos is null.</summary>
        readonly System.Func<EventImageCatalog?>? photosLater;
        /// <summary>The card was built under the start screen and it has not closed yet (issue #592): the photograph (and the
        /// portrait in its place) waits for onUncovered, so a Continue with a decision waiting loads no catalog in GameRoot.Begin.</summary>
        bool photoWaits;
        /// <summary>A screenshot run (HiddenInCaptures): the card never shows, so the captures show the map.</summary>
        readonly bool hiddenInCapture;
        /// <summary>Tests: the card is waiting for the start screen to close before it looks up its photograph.</summary>
        internal bool PhotoWaits => photoWaits;
        readonly VisualElement band, head, scroll, options, layer, hudRoot; readonly Label credit;
        readonly Button hide, reopen, illustrationSwitch;
        readonly System.Action onHide, onReopen, onUncovered;
        readonly EventCallback<PointerDownEvent> onHidePointer;
        readonly EventCallback<NavigationSubmitEvent> onHideSubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        /// <summary>The last Hide came from the pointer (PointerBlur's rule: no focus fill left behind for a mouse click).</summary>
        bool hideByPointer;
        /// <summary>The session the card last showed: a new one (New Game, a load) forgets what was collapsed or focused.</summary>
        GameSession? session;
        readonly CardVideo? video; readonly System.Func<string, EventVideoMapping.Row?>? clipFor;
        /// <summary>The shown event's clip URL and its alt text, or null (none, video off, or it failed this session).</summary>
        string? clip, clipAlt;
        /// <summary>The player chose Illustration: kept for the session (not saved), so later cards open on their clip too.</summary>
        bool illustration;
        /// <summary>Events whose clip failed this session: they show the photograph and no switch.</summary>
        readonly System.Collections.Generic.HashSet<string> failed = new System.Collections.Generic.HashSet<string>();
        readonly System.Action onSwitch;
        readonly PressSource presses;
        /// <summary>The event the player collapsed the card on (Hide), or null; a different event opens the card again.</summary>
        string? collapsedFor;
        /// <summary>The event whose first option last took the focus, so an event already open never takes it again.</summary>
        string? focusedFor;
        float appliedWidth = float.NaN;

        /// <param name="video">The illustration loop's player, or null when video is off (CardVideo.ForcedOff): then the
        /// card never offers the Illustration switch.</param>
        /// <param name="clipFor">An event's clip (event-videos.yaml), or null when it has none.</param>
        /// <param name="photosLater">With no <paramref name="photos"/>: gives the catalog, asked only once an event shows.</param>
        /// <param name="underStartScreen">The start screen goes up over the card once it is built (GameRoot.Begin): the card
        /// looks up no photograph until the screen closes, then renders again with it before it is first seen (issue #592).</param>
        /// <param name="hiddenInCapture">A screenshot run (HiddenInCaptures): neither the card, its scrim nor "Decision
        /// waiting" ever shows. Shown still names the event; the sim is untouched.</param>
        public EventCardPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits, EventImageCatalog? photos = null,
            CardVideo? video = null, System.Func<string, EventVideoMapping.Row?>? clipFor = null, System.Func<EventImageCatalog?>? photosLater = null,
            bool underStartScreen = false, bool hiddenInCapture = false)
        {
            photoWaits = underStartScreen;
            this.hiddenInCapture = hiddenInCapture;
            this.video = clipFor == null ? null : video; this.clipFor = clipFor;
            card = root.Q<VisualElement>("event-card"); this.bridge = bridge; this.text = text; this.portraits = portraits; this.photos = photos; this.photosLater = photosLater;
            band = card.Q<VisualElement>("event-photo"); credit = card.Q<Label>("event-photo-credit");
            // The band and its credit settle the drop (FitPhoto); the column, which only the window sizes, brings a dropped
            // photograph back when the window grows. A hidden element keeps its old layout, so a band shown again at the
            // size it was dropped at gets no geometry event of its own; the head below it always moves, so it checks too.
            head = card.Q<VisualElement>(className: "event-head");
            scroll = card.Q<VisualElement>("event-scroll"); options = card.Q<VisualElement>("event-options");
            layer = card.parent; hudRoot = root.Q("root") ?? root;
            hide = card.Q<Button>("event-hide"); reopen = layer.Q<Button>("event-reopen");
            hide.text = text["event.hide"]; reopen.text = text["event.reopen"];
            card.Q<Label>("event-draft").text = text["event.draft"];
            onHide = Collapse; onReopen = Reopen;
            onUncovered = () =>
            {
                // The start screen has closed: the photograph now, in the same frame the card first shows (issue #592).
                if (photoWaits) { photoWaits = false; if (Shown != null) Render(); }
                focusedFor = null; if (!hiddenInCapture && Shown != null && !Collapsed) FocusCard(Shown.Id, force: true);
            };
            onHidePointer = _ => hideByPointer = true;
            onHideSubmit = _ => hideByPointer = false;
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            hide.clicked += onHide; reopen.clicked += onReopen;
            card.focusable = true; card.tabIndex = 0;   // the card takes the focus itself (FocusCard); Statecraft.uss .event-card:focus
            // Hide with the pointer leaves no focus fill on "Decision waiting" (PointerBlur's rule); with the keyboard it moves there.
            hide.RegisterCallback<PointerDownEvent>(onHidePointer, TrickleDown.TrickleDown);
            hide.RegisterCallback<NavigationSubmitEvent>(onHideSubmit, TrickleDown.TrickleDown);
            // Escape on the open card is Hide (GameRoot also polls the key when nothing has the focus: Escape()).
            card.RegisterCallback<NavigationCancelEvent>(onCancel);
            // A start screen or a screen that covered the layer has closed (ScreenNav.DisableCovered): the open card takes
            // the focus it could not take under it.
            ScreenNav.OnUncovered(layer, onUncovered);
            illustrationSwitch = card.Q<Button>("event-illustration");
            onSwitch = () => { illustration = !illustration; ApplyIllustration(); };
            illustrationSwitch.clicked += onSwitch;
            foreach (var e in new[] { band, credit, head, scroll, options }) e.RegisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            layer.RegisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            card.RegisterCallback<GeometryChangedEvent>(OnCardGeometry);
            presses = new PressSource(options);
            bridge.Changed += Render;
            Render();
        }

        /// <summary>True for a screenshot run (?shot, -shot; VisualShots.Requested) that is not also a smoke run: the card is
        /// then never shown, so the captures show the map, as before the GDD cutover opened every new game on a card. The
        /// units are left out of the same runs (UnitLayer.HiddenInCaptures). A smoke run presses the card's options, so it
        /// keeps the card; with no flag (normal play) the card shows.</summary>
        public static bool HiddenInCaptures(string? url, string[] args, bool webgl) =>
            Statecraft.Game.Map.VisualShots.Requested(url, args, webgl) && GameRoot.SmokeTurns(url, args, webgl) == 0;

        /// <summary>Unsubscribes from the bridge, removes the option buttons and hides the card (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
            hide.clicked -= onHide; reopen.clicked -= onReopen;
            hide.UnregisterCallback(onHidePointer, TrickleDown.TrickleDown); hide.UnregisterCallback(onHideSubmit, TrickleDown.TrickleDown);
            card.UnregisterCallback(onCancel);
            ScreenNav.OnUncovered(layer, null);
            illustrationSwitch.clicked -= onSwitch;
            video?.Stop();
            foreach (var e in new[] { band, credit, head, scroll, options }) e.UnregisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            layer.UnregisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            card.UnregisterCallback<GeometryChangedEvent>(OnCardGeometry);
            presses.Dispose();
            card.Q<VisualElement>("event-options")?.Clear(); card.style.display = DisplayStyle.None;
            reopen.style.display = DisplayStyle.None;
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
        }

        /// <summary>True while the card is collapsed to the "Decision waiting" button (Hide).</summary>
        public bool Collapsed => Shown != null && collapsedFor == Shown.Id;

        /// <summary>Hide: the card gives the map back and waits behind the "Decision waiting" button, which takes the focus.</summary>
        public void Collapse()
        {
            if (Shown == null) return;
            collapsedFor = Shown.Id;
            Render();
            if (hideByPointer) { hideByPointer = false; return; }
            reopen.Focus();
            // Tried again once styles resolve: the button was hidden a moment ago and may not take it yet.
            reopen.schedule.Execute(() => { if (Collapsed && reopen.focusController?.focusedElement != reopen) reopen.Focus(); });
        }

        /// <summary>Escape: Hide, when the card is open and in reach. True when it did.</summary>
        public bool Escape()
        {
            if (Shown == null || Collapsed || !layer.enabledInHierarchy) return false;
            hideByPointer = false;
            Collapse();
            return true;
        }

        /// <summary>"Decision waiting": the card opens again and takes the focus.</summary>
        public void Reopen()
        {
            collapsedFor = null; focusedFor = null;
            Render();
        }

        /// <summary>The event the card shows, or null when every pending event has a queued answer.</summary>
        public GameEvent? Shown { get; private set; }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var pending = s.PendingEvents.Select(id => c.Events.First(e => e.Id == id)).ToList();
            var open = pending.Where(e => bridge.QueuedLike(new ChooseEventOption(e.Id, "")) == null).ToList();
            Shown = open.FirstOrDefault();
            if (Shown == null) collapsedFor = null;
            if (bridge.Session != session) { session = bridge.Session; collapsedFor = null; focusedFor = null; }
            var shown = !hiddenInCapture && Shown != null && !Collapsed;
            card.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            card.SetEnabled(shown);   // a child of an undisplayed element can still take the focus (#400): a collapsed card's options must not
            reopen.style.display = !hiddenInCapture && Collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            hudRoot.EnableInClassList(OpenClass, shown);
            ScreenNav.DisableCovered(hudRoot);
            if (Shown == null || Collapsed) video?.Stop();
            if (Shown == null) { clip = null; clipAlt = null; ShowPhoto(null); return; }
            var e = Shown;
            var date = EventSystem.FillDate(e, s.Date);
            var holder = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office, date);
            // The header photograph when the event has one; otherwise the minister's portrait plate (the fallback). Neither
            // while the card waits under the start screen (photoWaits): it is not seen, and the catalog is not loaded yet.
            var photo = photoWaits ? null : (photos ?? photosLater?.Invoke())?.Find(e.Id);
            var row = video != null && photo != null && !failed.Contains(e.Id) ? clipFor!(e.Id) : null;
            clip = row == null ? null : CardVideo.Url(row.File); clipAlt = row?.Alt;
            ShowPhoto(photo);
            var image = card.Q<VisualElement>("event-image");
            var entry = photo == null && !photoWaits ? portraits?.Find(holder?.Id) : null;
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
            if (shown && focusedFor != e.Id) FocusCard(e.Id, force: false);
        }

        /// <summary>A newly shown event puts the keyboard focus on the card (keyboard play, #41): the next Tab reaches its
        /// first option, and no option is lit before the player picks. Not while the layer is out of reach (the start screen
        /// or a screen covers it): then nothing is recorded, and the card takes the focus when the cover closes
        /// (onUncovered, <paramref name="force"/>). It is tried again once styles resolve, as a still-hidden card cannot take
        /// it; that retry takes it from nothing, or, when forced, from whatever the closing cover left focused.</summary>
        void FocusCard(string id, bool force)
        {
            if (!layer.enabledInHierarchy) return;
            focusedFor = id;
            card.Focus();
            card.schedule.Execute(() =>
            {
                if (Shown?.Id != id || Collapsed) return;
                if (!layer.enabledInHierarchy) { if (focusedFor == id) focusedFor = null; return; }   // covered meanwhile: try again when uncovered
                var now = card.focusController?.focusedElement;
                if (card.panel != null && now != card && (force || now == null)) card.Focus();
            });
        }

        /// <summary>Shows the event's photograph with its credit line, its alt text as the picture's tooltip, or hides both.</summary>
        void ShowPhoto(EventImageCatalog.Entry? photo)
        {
            current = photo;
            credit.text = photo?.credit ?? "";
            SetPhotoShown(photo != null);
        }

        /// <summary>Shows the current photograph (band, credit, tooltip, texture) or hides all of it. Idempotent.</summary>
        void SetPhotoShown(bool shown)
        {
            var photo = shown ? current : null;
            var display = photo != null ? DisplayStyle.Flex : DisplayStyle.None;
            band.style.display = display; credit.style.display = display;
            band.style.backgroundImage = photo == null ? (StyleBackground)StyleKeyword.None : new StyleBackground(photo.Value.image);
            band.tooltip = photo?.alt ?? "";
            PhotoAlt = photo?.alt;
            ApplyMediaHeight();
            ApplyIllustration();
        }

        /// <summary>The Illustration switch: shown only when the event has a clip and its photograph shows (the clip is never
        /// the only picture of a real event). On, the header plays the clip, labelled "AI-generated illustration" in place
        /// of the photograph's credit; off, or when the clip fails, the photograph and its credit come back.</summary>
        void ApplyIllustration()
        {
            var offered = clip != null && PhotoAlt != null && !Collapsed;
            illustrationSwitch.style.display = offered ? DisplayStyle.Flex : DisplayStyle.None;
            if (offered && illustration)
            {
                illustrationSwitch.text = text["event.photo"];
                credit.text = text["event.ai_label"];
                band.tooltip = clipAlt ?? "";
                if (video!.Current != clip) { var id = Shown!.Id; video.Play(clip!, band, _ => { failed.Add(id); Render(); }); }
                else video.ShowIn(band);
                return;
            }
            illustrationSwitch.text = text["event.illustration"];
            if (video?.Current != null)
            {
                video.Stop();
                if (current != null && PhotoAlt != null) band.style.backgroundImage = new StyleBackground(current.Value.image);
            }
            if (current != null) { credit.text = current.Value.credit; if (PhotoAlt != null) band.tooltip = current.Value.alt; }
        }

        /// <summary>True while the header plays the event's clip (or is preparing it).</summary>
        public bool IllustrationShown => video?.Current != null;

        /// <summary>The playtest 2 P2-3 line (UiLayoutTests): in a window at least this tall (logically) the body and the
        /// options of every event fit without scrolling. The media header gives way first on every window
        /// (ApplyMediaHeight), so below it the body scrolls only once the photograph is dropped.</summary>
        public const float ScrollFallbackHeight = 990f;

        /// <summary>The media header's height: 16:9 of the card's content width (the stylesheet's 540 px until the card is laid
        /// out). On every window the header gives way first when the card is taller than the layer (Statecraft.uss
        /// flex-shrink 1000; the picture is cropped, background-size cover), so the text and the options show whole; below
        /// PhotoNeeds it is dropped (FitPhoto), and only then may the body scroll. The 200 px band of the old right-hand
        /// column needed the body to scroll first on short windows, or 21:9 never showed a photograph; a 16:9 header has
        /// the room to give.</summary>
        void ApplyMediaHeight()
        {
            var w = card.contentRect.width;
            appliedWidth = w;
            band.style.height = float.IsNaN(w) || w <= 0 ? (StyleLength)StyleKeyword.Null : new StyleLength(UnityEngine.Mathf.Round(w / MediaAspect));
        }

        /// <summary>The card's width changed (it was first laid out, or the window changed): the media header follows it. The
        /// card's height changes with the header, so only a new width counts, or the two would chase each other.</summary>
        void OnCardGeometry(GeometryChangedEvent evt)
        {
            var w = card.contentRect.width;
            if (float.IsNaN(w) || w <= 0 || UnityEngine.Mathf.Abs(w - appliedWidth) < 0.5f) return;
            ApplyMediaHeight(); FitPhoto();
        }

        /// <summary>The smallest height of the picture left visible above its credit strip (Statecraft.uss .event-photo
        /// shrinks first). On a window too short for a long event the band would show a sliver, or cut into the credit that
        /// CC BY 4.0 requires: below credit height + MinPhotoHeight the photograph is dropped (credit, tooltip and all), so
        /// the body and the options keep their room and the card reads as a card without a picture. The minister's
        /// portrait does not come back in its place: it is taller still (docs/event-images.md).</summary>
        public const float MinPhotoHeight = 48f;

        /// <summary>The band height the photograph needs: its whole credit strip plus MinPhotoHeight of picture. The credit is
        /// measured from its text at the band's width, not read from its layout: in a squeezed band the label's layout is
        /// clamped to the room left, which is exactly the clipping this guards against.</summary>
        public float PhotoNeeds => CreditHeight() + MinPhotoHeight;

        float CreditHeight()
        {
            var s = credit.resolvedStyle;
            var bandWidth = band.layout.width > 0 ? band.layout.width : card.contentRect.width;
            var width = bandWidth - band.resolvedStyle.borderLeftWidth - band.resolvedStyle.borderRightWidth - s.paddingLeft - s.paddingRight;
            if (float.IsNaN(width) || width <= 0) return float.NaN;
            var text = credit.MeasureTextSize(credit.text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
            return text.y + s.paddingTop + s.paddingBottom + s.borderTopWidth + s.borderBottomWidth;
        }

        /// <summary>Drops the shown photograph when its band is shorter than <see cref="PhotoNeeds"/>. Only ever hides, so the
        /// layout pass it causes cannot bring the photo back and loop; OnColumnGeometry is what restores it.</summary>
        void FitPhoto()
        {
            if (PhotoAlt == null) return;
            var h = band.layout.height; var needs = PhotoNeeds;
            if (float.IsNaN(h) || float.IsNaN(needs)) return;
            if (h < needs - 0.5f) SetPhotoShown(false);
        }

        void OnPhotoGeometry(GeometryChangedEvent evt) => FitPhoto();

        /// <summary>The decision layer changed size (the window did; the card's content never moves it, as the layer is pinned
        /// to the window on every side). A shown photograph is checked again; a dropped one comes back only when the room now
        /// free in the layer holds it (<see cref="RoomForDroppedPhoto"/>), so it is never shown just to be measured (a hidden
        /// element keeps its old layout, so a re-shown band may get no new geometry event).</summary>
        void OnColumnGeometry(GeometryChangedEvent evt)
        {
            if (current != null && PhotoAlt == null) { if (RoomForDroppedPhoto() >= PhotoNeeds) SetPhotoShown(true); }
            else { ApplyMediaHeight(); FitPhoto(); }
        }

        /// <summary>The band height a dropped photograph would get now (at most; the band itself stops at 16:9): the layer's
        /// height the card leaves free (the card's max-height is the layer's), less the band's margins.</summary>
        float RoomForDroppedPhoto()
        {
            var room = layer.contentRect.height;
            if (float.IsNaN(room) || float.IsNaN(card.layout.height)) return 0;
            var slack = room - card.layout.height;
            return slack - band.resolvedStyle.marginTop - band.resolvedStyle.marginBottom;
        }

        /// <summary>The event's photograph, whether or not the window has room to show it.</summary>
        EventImageCatalog.Entry? current;

        /// <summary>The shown photograph's alt text (what it depicts), or null when the card shows no photograph (none for
        /// this event, or dropped on a short window).</summary>
        public string? PhotoAlt { get; private set; }

        /// <summary>The round sign in an option's left padding (Statecraft.uss .event-option), painted into the button so it
        /// stays a leaf whose wrapped text Yoga measures (UiIcon.Paint). In the Hebrew UI it sits in the right padding and
        /// points left (#193).</summary>
        const float SignRadius = 15f, SignLeft = 9f;

        static void PaintSign(MeshGenerationContext m)
        {
            var e = m.visualElement;
            bool rtl = UiLanguage.Within(e);
            var x = rtl ? e.layout.width - SignLeft - SignRadius : SignLeft + SignRadius;
            UiIcon.PaintDisc(m, rtl ? "chevron-back" : "chevron", new UnityEngine.Vector2(x, e.layout.height / 2f), SignRadius);
        }

        void Choose(GameEvent e, EventOption o)
        {
            // The bridge raises Refused (the toast shows it) or Changed (the toast clears and Render shows the next event,
            // which takes the focus). Answered from the keyboard with no event left, the focus goes to End Turn (#41 M1).
            bridge.Queue(new ChooseEventOption(e.Id, o.Id));
            if (Shown == null && presses.ByKeyboard) FocusReturn.To(hudRoot.Q<Button>("end-turn"));
        }
    }
}
