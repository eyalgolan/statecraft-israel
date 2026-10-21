#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Cabinet screen, laid out as promo/final/04: the government of the day and the body it answers to; the
    /// Prime Minister's card at the side, listing every office the PM holds (each also a button to select it); the
    /// legislature as a half circle of seats, coalition and opposition, with the majority line, and the coalition's seats,
    /// the opposition's, the majority, the largest party, coalition stability and each party's seats beside it; and a card
    /// for every other office held by someone else (portrait, party, competence and loyalty bars; one card per holder of an
    /// office that has several at once). Every number is the
    /// sim's: legislatures.yaml seats and size, governments.yaml coalition, people.yaml competence and loyalty, GameState
    /// coalition stability. The two seat colours mark coalition and opposition only (content records no party colours).
    /// Selecting an office lists every person with Appoint, or the reason they cannot be appointed (Cabinet.Check).
    /// Appointing queues the Appoint order for End Turn. The screen renders only while it is open (a change while it is
    /// hidden is caught up when ScreenNav opens it), and it updates its cards in place, so keyboard focus survives.</summary>
    public sealed class CabinetPresenter : System.IDisposable
    {
        public const string PrimeMinister = "prime_minister";
        /// <summary>Portrait boxes in logical pixels (Statecraft.uss .seat / .pm-card .card-portrait).</summary>
        public static readonly Vector2 CardPortrait = new Vector2(188f, 150f), PmPortrait = new Vector2(286f, 250f);
        /// <summary>The seat colours: coalition blue and opposition grey, each at least 3:1 against the screen's navy.</summary>
        public static readonly Color CoalitionColour = new Color32(92, 138, 200, 255), OppositionColour = new Color32(186, 180, 168, 255);

        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly Button pmCard; readonly System.Action onPm; readonly EventCallback<GeometryChangedEvent> onPmGeometry;
        string? pmId; VisualElement? pmPortrait;
        readonly ScrollView list, candidates; readonly VisualElement pmOffices, chips; readonly Hemicycle hemicycle;
        readonly VisualElement side, pick, status; readonly EventCallback<GeometryChangedEvent> onSideGeometry;
        /// <summary>The candidate list's floor where the side column has room: its heading and two rows (#41 M7).</summary>
        public const float PickFloor = 220f;
        readonly Dictionary<string, Button> cards = new Dictionary<string, Button>();
        List<string> cardOrder = new List<string>(), pmOrder = new List<string>();
        readonly Dictionary<string, (Button appoint, Label reason)> rows = new Dictionary<string, (Button, Label)>();
        string? pickFor; string chipsFor = "";
        bool dirty;

        public CabinetPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits)
        {
            screen = root.Q<VisualElement>("screen-cabinet"); this.bridge = bridge; this.text = text; this.portraits = portraits;
            pmCard = screen.Q<Button>("seat-" + PrimeMinister);
            list = screen.Q<ScrollView>("cabinet-seats"); candidates = screen.Q<ScrollView>("cabinet-candidates");
            pmOffices = screen.Q<VisualElement>("cabinet-pm-offices"); chips = screen.Q<VisualElement>("cabinet-parties");
            hemicycle = screen.Q<Hemicycle>("cabinet-hemicycle");
            hemicycle.Mirrored = text["ui.lang"] == UiLanguage.Hebrew;   // the coalition from the right in a right-to-left UI
            onPm = () => Select(PrimeMinister);
            pmCard.clicked += onPm;
            onPmGeometry = _ => FramePm();
            screen.Q<Label>("cabinet-house-unit").text = text["cabinet.members_unit"];
            bridge.Changed += OnChanged;
            ScreenNav.OnOpened(screen, () => { if (dirty) Render(); });
            side = screen.Q<VisualElement>("cabinet-side"); pick = screen.Q<VisualElement>("cabinet-pick"); status = screen.Q<VisualElement>("cabinet-status");
            onSideGeometry = _ => FitPick();
            foreach (var e in new[] { side, pmOffices, status }) e.RegisterCallback(onSideGeometry);
            Render();
        }

        /// <summary>Unsubscribes and empties every card, list and the half circle (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= OnChanged;
            pmCard.clicked -= onPm;
            pmPortrait?.UnregisterCallback(onPmGeometry); pmPortrait = null;
            foreach (var e in new[] { side, pmOffices, status }) e.UnregisterCallback(onSideGeometry);
            if (screen.userData is System.Action) screen.userData = null;
            list.Clear(); candidates.Clear(); pmOffices.Clear(); chips.Clear(); PersonCard.Clear(pmCard);
            cards.Clear(); rows.Clear(); cardOrder.Clear(); pmOrder.Clear(); pickFor = null; chipsFor = "";
            hemicycle.Set(new Hemicycle.Group[0], 0);
            screen.Q<VisualElement>("cabinet-pick").style.display = DisplayStyle.None;
        }

        /// <summary>The office whose candidates are listed, or null.</summary>
        public string? Selected { get; private set; }

        /// <summary>How many times the screen was rendered (a hidden screen is not).</summary>
        public int Renders { get; private set; }

        public void Select(string? office) { Selected = office; Render(); }

        void OnChanged() { if (ScreenNav.IsOpen(screen)) Render(); else dirty = true; }

        string Describe(Person p)
        {
            var c = bridge.Content; var party = Offices.PartyOf(c, p, bridge.State.Date);
            var partyName = party == "" ? text["cabinet.no_party"] : ContentNames.Of(text, c.Parties.First(x => x.Id == party));
            return text.Format("cabinet.person", ContentNames.Of(text, p), partyName, p.Competence, p.Loyalty);
        }

        /// <summary>The legislature's party blocks in drawing order: the coalition's parties, then the opposition's, each by
        /// seats (most first, then content order), in the coalition or opposition colour. Parties without a seat are left out.</summary>
        public static IReadOnlyList<Hemicycle.Group> Blocks(Content c, Legislature leg, Government? gov, System.DateTime date)
        {
            var order = c.Parties.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i);
            int Order(string id) => order.TryGetValue(id, out var i) ? i : int.MaxValue;
            var inCoalition = new HashSet<string>(gov?.CoalitionOn(date) ?? new List<string>());
            var blocks = new List<Hemicycle.Group>();
            foreach (var coalition in new[] { true, false })
                foreach (var kv in leg.Seats.Where(kv => kv.Value > 0 && inCoalition.Contains(kv.Key) == coalition)
                             .OrderByDescending(kv => kv.Value).ThenBy(kv => Order(kv.Key)))
                    blocks.Add(new Hemicycle.Group(kv.Key, kv.Value, coalition ? CoalitionColour : OppositionColour, coalition));
            return blocks;
        }

        /// <summary>The unit of a seat count in <paramref name="leg"/>: cabinet.seats for an elected body (legislatures.yaml gives
        /// its election date, Legislature.Elected), cabinet.seats_appointed for one that was not elected (the Provisional
        /// State Council). The same "seats" in English; Hebrew says "מנדטים" for the Knesset and "חברים" for the council (#193).
        /// No legislature (no count is shown then) counts as elected.</summary>
        public static string SeatUnit(UiText text, Legislature? leg) =>
            text[leg == null || leg.Elected.HasValue ? "cabinet.seats" : "cabinet.seats_appointed"];

        public void Render()
        {
            dirty = false; Renders++;
            var c = bridge.Content; var s = bridge.State;
            var gov = Offices.Government(c, s.Date);
            var leg = Offices.Legislature(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("cabinet-government").text = gov == null ? "" : ContentNames.Of(text, gov);
            var advisory = c.SystemsAt(s.Date).Legislature == EraSystems.Assembly;   // eras.yaml systems.legislature (#58)
            screen.Q<Label>("cabinet-legislature").text = leg == null ? "" : text.Format(advisory ? "cabinet.legislature_advisory" : "cabinet.legislature", ContentNames.Of(text, leg), leg.Size);
            RenderHouse(c, s, gov, leg, seats, size);

            var lineup = Cabinet.Lineup(c, s);
            if (Selected != null && lineup.All(x => x.Office.Id != Selected)) Selected = null;   // that office no longer exists (abolished)

            // The Prime Minister's card lists every office the PM holds today (Ben-Gurion was also Minister of Defense);
            // those offices get no second card, but each stays selectable from a button under the PM card.
            var pmSeat = lineup.FirstOrDefault(x => x.Office.Id == PrimeMinister);
            var pm = pmSeat?.Holder;
            bool HeldByPm(Seat x) => pm != null && x.Office.Id != PrimeMinister && x.Holder?.Id == pm.Id;
            pmCard.EnableInClassList("seat-selected", Selected == PrimeMinister);
            if (pmSeat != null)
            {
                var offices = pm == null ? ContentNames.Of(text, pmSeat.Office, null)
                    : string.Join(text["card.offices_separator"], lineup.Where(x => x.Office.Id == PrimeMinister || HeldByPm(x)).Select(x => ContentNames.Of(text, x.Office, pm)));
                PersonCard.Fill(pmCard, "seat-" + PrimeMinister, c, s, pm, offices, pmSeat.Appointed, text, portraits, PmPortrait, partyLabel: true);
                pmId = pm?.Id;
                FramePm();
            }
            var pmHeld = lineup.Where(HeldByPm).ToList();
            if (!pmHeld.Select(x => x.Office.Id).SequenceEqual(pmOrder))
            {
                pmOffices.Clear();
                foreach (var seat in pmHeld)
                {
                    var office = seat.Office.Id;
                    var b = new Button(() => Select(office)) { name = "seat-" + office, text = text.Format("cabinet.select_office", ContentNames.Of(text, seat.Office, pm)) };
                    b.AddToClassList("pm-office");
                    pmOffices.Add(b);
                }
                pmOrder = pmHeld.Select(x => x.Office.Id).ToList();
            }
            foreach (var b in pmOffices.Children()) b.EnableInClassList("seat-selected", b.name == "seat-" + Selected);
            pmOffices.style.display = pmHeld.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var grid = Cards(lineup.Where(x => x.Office.Id != PrimeMinister && !HeldByPm(x)));
            if (!grid.Select(x => x.key).SequenceEqual(cardOrder))
            {
                list.Clear(); cards.Clear();
                foreach (var (key, seat, _) in grid)
                {
                    var office = seat.Office.Id;
                    var card = new Button(() => Select(office)) { name = "seat-" + key };
                    card.AddToClassList("seat");
                    PersonCard.Build(card, "seat-" + key, text);
                    cards[key] = card;
                    list.Add(card);
                }
                cardOrder = grid.Select(x => x.key).ToList();
            }
            foreach (var (key, seat, holder) in grid)
            {
                var card = cards[key];
                card.EnableInClassList("seat-selected", seat.Office.Id == Selected);
                card.EnableInClassList("seat-vacant", holder == null);
                PersonCard.Fill(card, "seat-" + key, c, s, holder, ContentNames.Of(text, seat.Office, holder), seat.Appointed, text, portraits, CardPortrait);
            }

            RenderPick(c, s);
            var queued = bridge.Queued.OfType<Appoint>().Select(a =>
            {
                var person = c.People.First(p => p.Id == a.Person);
                return text.Format("cabinet.queued", ContentNames.Of(text, person), ContentNames.Of(text, c.Offices.First(o => o.Id == a.Office), person));
            });
            screen.Q<Label>("cabinet-status").text = string.Join("\n", queued);
        }

        /// <summary>The candidate list's floor (#41 M7): PickFloor where the side column has room; on a shorter one (32:9 is
        /// 764 logical px), whatever the Prime Minister's card at its smallest (its text, Statecraft.uss .pm-card min-height),
        /// his other offices and the orders line leave, so the orders line is never pushed out of the screen. Above the floor
        /// the portrait gives way first (Statecraft.uss). Setting it moves only the list and the card, never the column, the
        /// offices or the orders line, so it cannot loop.</summary>
        void FitPick()
        {
            var h = side.contentRect.height;
            if (float.IsNaN(h) || h <= 0) return;
            static float Outer(VisualElement e) => e.resolvedStyle.display == DisplayStyle.None || float.IsNaN(e.layout.height) ? 0f
                : e.layout.height + e.resolvedStyle.marginTop + e.resolvedStyle.marginBottom;
            var card = pmCard.resolvedStyle.minHeight.value + pmCard.resolvedStyle.marginTop + pmCard.resolvedStyle.marginBottom;
            var floor = Mathf.Clamp(h - card - Outer(pmOffices) - Outer(status), 0f, PickFloor);
            if (Mathf.Abs(pick.resolvedStyle.minHeight.value - floor) > 0.5f) pick.style.minHeight = floor;
        }

        /// <summary>The Prime Minister's portrait gives way on a short window (Statecraft.uss, #41 M7), so its framing follows
        /// the box it has now and the face stays in view. It listens on the portrait itself: the box also changes when the
        /// card's text gains or loses a line inside a card of the same height.</summary>
        void FramePm()
        {
            var portrait = pmCard.Q("seat-" + PrimeMinister + "-portrait");
            if (portrait != pmPortrait)
            {
                pmPortrait?.UnregisterCallback(onPmGeometry);
                pmPortrait = portrait;
                pmPortrait?.RegisterCallback(onPmGeometry);
            }
            var entry = portraits?.Find(pmId);
            if (portrait == null || entry == null) return;
            var size = portrait.layout.size;
            PersonCard.Frame(portrait, entry.Value, float.IsNaN(size.x) || size.x <= 0 || size.y <= 0 ? PmPortrait : size);
        }

        /// <summary>One card per holder: an office with several holders today (#277: Minister without Portfolio) gets a card for
        /// each, in Seat.Holders order, named office, office-2, office-3...; every other office one card, named by its id.</summary>
        static List<(string key, Seat seat, Person? holder)> Cards(IEnumerable<Seat> seats) =>
            seats.SelectMany(seat => seat.Holders.Count > 1
                ? seat.Holders.Select((p, i) => (i == 0 ? seat.Office.Id : seat.Office.Id + "-" + (i + 1), seat, (Person?)p))
                : new[] { (seat.Office.Id, seat, seat.Holder) }).ToList();

        /// <summary>The candidate list for the selected office: rows are made when the office changes and updated in place.</summary>
        void RenderPick(Content c, GameState s)
        {
            var pick = screen.Q<VisualElement>("cabinet-pick");
            pick.style.display = Selected == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Selected != pickFor)
            {
                candidates.Clear(); rows.Clear();
                pickFor = Selected;
                if (Selected != null)
                    foreach (var person in c.People)
                    {
                        var p = person; var office = Selected;
                        var row = new VisualElement { name = "cand-" + p.Id };
                        row.AddToClassList("candidate");
                        var appoint = new Button(() => Appoint(office, p.Id)) { name = "appoint-" + p.Id };
                        row.Add(appoint);
                        var why = new Label { name = "cand-" + p.Id + "-reason" };
                        why.AddToClassList("reason");
                        row.Add(why);
                        candidates.Add(row);
                        rows[p.Id] = (appoint, why);
                    }
            }
            if (Selected == null) return;
            screen.Q<Label>("cabinet-pick-title").text = text.Format("cabinet.pick", ContentNames.Of(text, c.Offices.First(o => o.Id == Selected), null));
            var list = Cabinet.Candidates(c, s, Selected);
            // A refusal about the office itself (the Prime Minister's or the President's, an abolished one) is the same for
            // everyone: it is said once under the heading, not under every name (#215).
            var shared = list.Count > 0 && list.All(x => x.reason != null && x.reason.Text == list[0].reason!.Text) ? list[0].reason : null;
            var common = screen.Q<Label>("cabinet-pick-reason");
            common.text = shared == null ? "" : ReasonText(c, shared);
            common.style.display = shared == null ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var (person, reason) in list)
            {
                if (!rows.TryGetValue(person.Id, out var r)) continue;
                r.appoint.text = Describe(person);
                r.appoint.SetEnabled(reason == null);
                r.reason.text = shared == null && reason != null ? ReasonText(c, reason) : "";
                r.reason.style.display = reason == null || shared != null ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        string ReasonText(Content c, CabinetReason reason)
        {
            bool he = text["ui.lang"] == UiLanguage.Hebrew;
            string Person(string id) { var p = c.People.First(x => x.Id == id); return he && p.NameHe != "" ? p.NameHe : p.Name; }
            string Office(string id) { var o = c.Offices.First(x => x.Id == id); return he && o.TitleHe != "" ? o.TitleHe : o.Title; }
            string Party(string id) { var p = c.Parties.First(x => x.Id == id); return he && p.NameHe != "" ? p.NameHe : p.Name; }
            string Date(string iso) => Statecraft.Game.Map.Control1948.DateText(System.DateTime.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture), k => text[k]);
            var key = "cabinet.reason." + reason.Kind;
            switch (reason.Kind)
            {
                case "historical_office": case "several_holders": return text.Format(key, Office(reason.Values[0]));
                case "abolished": case "not_established": return text.Format(key, Office(reason.Values[0]), Date(reason.Values[1]));
                case "died": return text.Format(key, Person(reason.Values[0]), Date(reason.Values[1]));
                case "is_president": case "is_chief_of_staff": case "not_career_officer": return text.Format(key, Person(reason.Values[0]));
                case "already_holds": return text.Format(key, Person(reason.Values[0]), ContentNames.Of(text, c.Offices.First(o => o.Id == reason.Values[1]), c.People.First(p => p.Id == reason.Values[0])));
                case "party_not_coalition": return text.Format(key, Person(reason.Values[0]), Party(reason.Values[1]));
                default: return text[key];
            }
        }

        /// <summary>The half circle of seats and its legend; the party list is remade only when the body or government changes.</summary>
        void RenderHouse(Content c, GameState s, Government? gov, Legislature? leg, int seats, int size)
        {
            screen.Q<VisualElement>("cabinet-house").style.display = leg == null ? DisplayStyle.None : DisplayStyle.Flex;
            var unit = SeatUnit(text, leg);
            screen.Q<Label>("cabinet-coalition").text = text.Format("cabinet.coalition", seats, size, PlayerText.Percent(s.CoalitionStability), unit);
            screen.Q<UiBar>("cabinet-stability").Value = (float)s.CoalitionStability;
            if (leg == null) { hemicycle.Set(new Hemicycle.Group[0], 0); chips.Clear(); chipsFor = ""; return; }
            var blocks = Blocks(c, leg, gov, s.Date);
            hemicycle.Set(blocks, leg.Size);
            screen.Q<Label>("cabinet-house-size").text = leg.Size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            screen.Q<Label>("cabinet-opposition").text = text.Format("cabinet.opposition", blocks.Where(b => !b.Coalition).Sum(b => b.Seats), unit);
            screen.Q<Label>("cabinet-majority").text = text.Format("cabinet.majority", hemicycle.Majority, unit);
            var largest = blocks.OrderByDescending(b => b.Seats).FirstOrDefault();
            screen.Q<Label>("cabinet-largest").text = largest.Seats > 0 ? text.Format("cabinet.largest", ContentNames.Of(text, c.Parties.First(p => p.Id == largest.Party)), largest.Seats, unit) : "";
            // The coalition's parties, not the government's id: a government's coalition_changes move parties between the
            // sides mid-term (governments.yaml, 25 May 1953), and the chips must follow the half circle (#41 M5).
            var key = leg.Id + "/" + string.Join(",", blocks.Where(b => b.Coalition).Select(b => b.Party));
            if (key == chipsFor) return;
            chipsFor = key;
            chips.Clear();
            // Two groups, each opened by its side's name: the swatch's blue or grey alone (1.72:1 apart) does not say which
            // side a party is on (#41 M5, WCAG 1.4.1). Two words in all, not one per chip, so the legend barely grows.
            foreach (var coalition in new[] { true, false })
            {
                if (!blocks.Any(b => b.Coalition == coalition)) continue;
                var group = new VisualElement { pickingMode = PickingMode.Ignore };
                group.AddToClassList("party-group");
                var heading = new Label(text[coalition ? "cabinet.coalition_group" : "cabinet.opposition_group"]) { pickingMode = PickingMode.Ignore };
                heading.AddToClassList("party-group-heading");
                group.Add(heading);
                foreach (var b in blocks.Where(b => b.Coalition == coalition))
                    group.Add(Chip("party-" + b.Party, b.Colour, text.Format("cabinet.party_seats", ContentNames.Of(text, c.Parties.First(p => p.Id == b.Party)), b.Seats), false));
                chips.Add(group);
            }
            var unrecorded = leg.Size - blocks.Sum(b => b.Seats);
            if (unrecorded > 0) chips.Add(Chip("party-unrecorded", Color.clear, text.Format("cabinet.party_seats", text["cabinet.unrecorded"], unrecorded), true));
        }

        static VisualElement Chip(string name, Color colour, string caption, bool hollow)
        {
            var chip = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            chip.AddToClassList("party-chip");
            var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            swatch.AddToClassList("party-swatch");
            swatch.EnableInClassList("party-swatch--hollow", hollow);
            if (!hollow) swatch.style.backgroundColor = colour;
            chip.Add(swatch);
            chip.Add(new Label(caption) { name = name + "-label", pickingMode = PickingMode.Ignore });
            return chip;
        }

        void Appoint(string office, string person)
        {
            // The bridge reports a refusal itself (Refused); the sim re-checks legality when the turn runs.
            bridge.Queue(new Appoint(office, person));
        }
    }
}
