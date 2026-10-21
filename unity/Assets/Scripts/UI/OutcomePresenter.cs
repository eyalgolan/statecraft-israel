#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The End Turn outcome clips (decision videos, phase 3; docs/event-videos.md). When a turn has run, each event
    /// the player answered with its historical option, in an order the turn applied (not in the turn's rejections, and in
    /// GameState.ChosenOptions with that option), and that has an outcome clip, plays once: a 3.5 s silent clip in the
    /// middle of the screen, captioned as the order ("Your order: ..."), never as a result. A delegated answer, a
    /// counterfactual option and Deport get none (EventVideoMapping.OutcomeProblems refuses such rows). Several play one
    /// after the other.
    /// While one plays: a screen open at End Turn is closed (closeScreens), the layer is drawn over everything but the
    /// toast (Game.uxml order), and the top bar, the panels and the decision layer are disabled (ScreenNav.DisableCovered),
    /// so no order can be given under it. It has its own CardVideo, so the decision card's loop never touches it.
    /// The layer takes the keyboard focus (retried once styles resolve, as a hidden element cannot take it): Enter, Space
    /// or a gamepad's Submit skips one clip, Esc or Cancel skips them all, and a click skips one. When the last one closes
    /// the focus goes back where it was, unless the decision card takes it (ScreenNav.OnUncovered).
    /// Not made at all when video is off (CardVideo.ForcedOff: every automated run).</summary>
    public sealed class OutcomePresenter : IDisposable
    {
        /// <summary>On the "root" element while an outcome clip shows.</summary>
        public const string OpenClass = "outcome-open";
        /// <summary>How long a clip may take to be ready before it is skipped (the card's loop allows 8 s; an outcome holds
        /// the screen, so it gives up sooner).</summary>
        public const long PrepareTimeoutMs = 3000;
        /// <summary>How long past the clip's own length the overlay waits for its end before moving on (a stalled stream).</summary>
        public const long GraceMs = 2000;

        readonly VisualElement hudRoot, layer, media; readonly Label caption;
        readonly SimBridge bridge; readonly UiText text; readonly CardVideo video;
        readonly Dictionary<(string ev, string option), EventVideoMapping.Row> clips;
        readonly Action? closed, closeScreens;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<PointerDownEvent> onPointer;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<GeometryChangedEvent> onGeometry, onShown;
        readonly Queue<(EventVideoMapping.Row row, string order)> queue = new Queue<(EventVideoMapping.Row, string)>();
        IVisualElementScheduledItem? guard;
        Focusable? focusBefore;

        /// <summary>The clip on screen now, or null.</summary>
        public EventVideoMapping.Row? Showing { get; private set; }

        int closedAt = -100;
        /// <summary>True in the frame the overlay closed: GameRoot's Escape poll then leaves the card alone (the same Escape
        /// key already skipped the clips through the Cancel event).</summary>
        public bool ClosedThisFrame => closedAt == UnityEngine.Time.frameCount;

        /// <summary>True while a clip shows and in the frame after the overlay closed: the map ignores a press then, so the
        /// click that skips a clip never also selects the hex under it.</summary>
        public bool HoldsThePointer => Showing != null || UnityEngine.Time.frameCount - closedAt <= 1;

        /// <summary>The caption of the clip on screen ("Your order: ..."), or "".</summary>
        public string Caption => caption.text;

        /// <param name="video">The outcome clips' own player (not the decision card's).</param>
        /// <param name="closed">Told when the last clip of a turn is done (GameRoot re-renders the decision card).</param>
        /// <param name="closeScreens">Closes any open screen (ScreenNav.Close) when a clip starts.</param>
        public OutcomePresenter(VisualElement root, SimBridge bridge, UiText text, CardVideo video, IEnumerable<EventVideoMapping.Row> rows,
            Action? closed = null, Action? closeScreens = null)
        {
            this.bridge = bridge; this.text = text; this.video = video; this.closed = closed; this.closeScreens = closeScreens;
            clips = rows.ToDictionary(r => (r.Event, r.Option));
            hudRoot = root.Q("root") ?? root;
            layer = root.Q<VisualElement>("outcome-layer");
            media = layer.Q<VisualElement>("outcome-media");
            caption = layer.Q<Label>("outcome-caption");
            layer.Q<Label>("outcome-hint").text = text["outcome.skip"];
            layer.Q<Label>("outcome-label").text = text["event.ai_label"];
            layer.focusable = true; layer.tabIndex = 0;
            onTurn = rejected =>
            {
                // Guarded as a whole: nothing in the outcome code may throw out of TurnEnded, whose later handlers
                // include the turn's autosave (GameStart.Autosave subscribes after this presenter).
                try { Turn(rejected); }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    try { queue.Clear(); video.Stop(); Close(); } catch (Exception again) { UnityEngine.Debug.LogException(again); }
                }
            };
            onPointer = e => { Skip(); e.StopPropagation(); };
            onSubmit = e => { Skip(); e.StopPropagation(); };
            onCancel = e => { SkipAll(); e.StopPropagation(); };
            onGeometry = _ =>   // 16:9 of its width (USS has no aspect ratio)
            {
                var w = media.layout.width;
                if (w > 0 && UnityEngine.Mathf.Abs(media.layout.height - w / EventCardPresenter.MediaAspect) > 0.5f)
                    media.style.height = UnityEngine.Mathf.Round(w / EventCardPresenter.MediaAspect);
            };
            media.RegisterCallback(onGeometry);
            // The layer cannot take the focus while it is still undisplayed: it takes it when it is laid out.
            onShown = _ => { if (Showing != null && layer.focusController?.focusedElement != layer) layer.Focus(); };
            layer.RegisterCallback(onShown);
            bridge.TurnEnded += onTurn;
            layer.RegisterCallback(onPointer);
            layer.RegisterCallback(onSubmit);
            layer.RegisterCallback(onCancel);
        }

        /// <summary>The turn just run: queue the clip of every historical order it applied, then play the first.</summary>
        /// <summary>Test seam: runs at the start of each turn's handling (a test throws from it).</summary>
        internal Action? BeforeTurn;

        void Turn(IReadOnlyList<Rejection> rejected)
        {
            BeforeTurn?.Invoke();
            var c = bridge.Content; var s = bridge.State;
            foreach (var order in bridge.LastBatch.OfType<ChooseEventOption>())
            {
                if (!clips.TryGetValue((order.EventId, order.OptionId), out var row)) continue;
                if (rejected.Any(x => ReferenceEquals(x.Command, order))) continue;                                         // refused
                if (!s.ChosenOptions.TryGetValue(order.EventId, out var applied) || applied != order.OptionId) continue;   // replaced
                var ev = c.Events.FirstOrDefault(e => e.Id == order.EventId);
                var option = ev?.Options.FirstOrDefault(o => o.Id == order.OptionId);
                if (ev == null || option == null || !option.Historical) continue;   // the mapping refuses this too
                string words;
                try
                {
                    var date = EventSystem.FillDate(ev, bridge.LastBatchDate);   // the card's date
                    // The option as the card shows it (#1210): its text_he in Hebrew, the English when it has none.
                    words = ContentNames.Fill(text, c, s, date, Cabinet.Fill(c, s, option.Text, date), option.TextHe);
                }
                catch (Exception e)   // never let a caption break the turn's other TurnEnded work (the autosave)
                {
                    UnityEngine.Debug.LogWarning($"VIDEO outcome caption for {order.EventId}/{order.OptionId}: {e.Message}");
                    continue;
                }
                queue.Enqueue((row, text.Format("outcome.order", words)));
            }
            if (Showing == null && queue.Count > 0) Next();
        }

        void Next()
        {
            guard?.Pause(); guard = null;
            if (queue.Count == 0) { Close(); return; }
            var (row, order) = queue.Dequeue();
            if (Showing == null)
            {
                focusBefore = layer.focusController?.focusedElement;
                closeScreens?.Invoke();
            }
            Showing = row;
            UnityEngine.Debug.Log("OUTCOME showing " + row.Event);
            caption.text = order;
            media.tooltip = row.Alt;
            media.style.backgroundImage = StyleKeyword.None;
            hudRoot.AddToClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
            TakeFocus();
            var shown = row;
            video.Play(CardVideo.Url(row.File), media, _ => { if (Showing == shown) Next(); }, loop: false,
                ended: () => { if (Showing == shown) Next(); }, prepareTimeoutMs: PrepareTimeoutMs);
            guard = layer.schedule.Execute(() => { if (Showing == shown) Next(); }).StartingIn(row.Frames * 1000L / 24 + PrepareTimeoutMs + GraceMs);
        }

        /// <summary>The layer takes the focus now; if it is still undisplayed (the class has not resolved yet), it takes it
        /// when it is laid out (onShown), and a scheduled retry covers a layout that does not change.</summary>
        void TakeFocus()
        {
            layer.Focus();
            layer.schedule.Execute(() => { if (Showing != null && layer.focusController?.focusedElement != layer) layer.Focus(); })
                .Every(50).ForDuration(1000);
        }

        /// <summary>Skips the clip on screen (a click, Enter, Space, Submit).</summary>
        public void Skip() { if (Showing != null) { UnityEngine.Debug.Log("OUTCOME skipped " + Showing.Event); video.Stop(); Next(); } }

        /// <summary>Skips every clip of this turn (Esc, Cancel). True when one was showing.</summary>
        public bool SkipAll()
        {
            if (Showing == null) return false;
            UnityEngine.Debug.Log("OUTCOME skipped all at " + Showing.Event);
            queue.Clear();
            video.Stop();
            Close();
            return true;
        }

        void Close()
        {
            guard?.Pause(); guard = null;
            var was = Showing != null;
            Showing = null; caption.text = "";
            media.tooltip = "";
            media.style.backgroundImage = StyleKeyword.None;
            hudRoot.RemoveFromClassList(OpenClass);
            if (!was) { ScreenNav.DisableCovered(hudRoot); return; }
            closedAt = UnityEngine.Time.frameCount;
            UnityEngine.Debug.Log("OUTCOME closed");
            var back = focusBefore; focusBefore = null;
            layer.Blur();
            ScreenNav.DisableCovered(hudRoot);   // the decision layer comes back: an open card takes the focus (OnUncovered)
            Restore(back);
            layer.schedule.Execute(() => Restore(back));
            closed?.Invoke();
        }

        /// <summary>Puts the focus back where it was before the clips, unless something (the decision card) has it.</summary>
        void Restore(Focusable? back)
        {
            if (Showing != null || back is not VisualElement e || e.panel == null || !e.enabledInHierarchy || !e.canGrabFocus) return;
            var now = layer.focusController?.focusedElement;
            if (now == null || now == layer) e.Focus();
        }

        public void Dispose()
        {
            bridge.TurnEnded -= onTurn;
            layer.UnregisterCallback(onPointer);
            layer.UnregisterCallback(onSubmit);
            layer.UnregisterCallback(onCancel);
            media.UnregisterCallback(onGeometry);
            layer.UnregisterCallback(onShown);
            queue.Clear();
            if (Showing != null) video.Stop();
            focusBefore = null;
            Close();
        }
    }
}
