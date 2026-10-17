#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Cabinet and Defence screens laid out as promo/final/04 and 08: the half circle of seats, the Prime
/// Minister's card, the person cards with competence and loyalty bars, the leadership, the ladder, the call-up bar and the
/// defence budget share. Every figure on them is bound to the sim's data, none is invented, and both screens fit the window
/// shapes the HUD supports, with the HUD's keyboard focus rules.</summary>
public class PromoScreensTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game stepped until <paramref name="date"/> (the opening when it is the start).</summary>
    static GameSession At(DateTime date)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; g.State.Date < date; t++)
        {
            if (t >= 400 || g.State.GameOver != null) Assert.Fail($"the historical game did not reach {date:yyyy-MM-dd}");
            g.Step(HistoricalAI.Commands(C, g.State));
        }
        return g;
    }

    /// <summary>One date in each legislature the game reaches: the Provisional State Council, the First and the Second
    /// Knesset. Since the GDD cutover play ends on 15 January 1952 (R-LOOP-004); the later legislatures stay in the content for
    /// the paused eras (G2), where play does not reach them.</summary>
    static IEnumerable<DateTime> OneDatePerLegislature() => C.Legislatures.Where(l => l.From < C.End.AddDays(-30))
        .Select(l => l.From < C.Rules.Start ? C.Rules.Start : l.From.AddDays(30));

    [Test]
    public void TheHalfCircleSeatsTheRealLegislatureWithTheCoalitionFirst()
    {
        foreach (var date in OneDatePerLegislature())
        {
            var g = At(date);
            using var ui = new UiTestKit();
            var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
            var leg = Offices.Legislature(C, g.State.Date)!;
            var gov = Offices.Government(C, g.State.Date)!;
            var where = $"{g.State.Date:yyyy-MM-dd} {leg.Id}";
            var house = ui.Q<Hemicycle>("cabinet-hemicycle");
            Assert.AreEqual(leg.Size, house.Size, where);
            Assert.AreEqual(leg.Size, Hemicycle.Layout(leg.Size).Count, where + ": one dot per seat");
            Assert.AreEqual(leg.Size / 2 + 1, house.Majority, where);
            foreach (var kv in leg.Seats.Where(kv => kv.Value > 0))
            {
                var block = house.Groups.Single(b => b.Party == kv.Key);
                Assert.AreEqual(kv.Value, block.Seats, where + " " + kv.Key);
                Assert.AreEqual(gov.CoalitionOn(g.State.Date).Contains(kv.Key), block.Coalition, where + " " + kv.Key);
            }
            Assert.AreEqual(leg.Seats.Count(kv => kv.Value > 0), house.Groups.Count, where + ": only parties with seats");
            var (seats, size) = PoliticsSystem.CoalitionSeats(C, g.State.Date);
            Assert.AreEqual(seats, house.Groups.Where(b => b.Coalition).Sum(b => b.Seats), where + ": the coalition's seats");
            // Coalition first: the first <seats> dots from the left are coalition colours, the rest are not.
            var coalitionColours = new HashSet<Color>(house.Groups.Where(b => b.Coalition).Select(b => b.Colour));
            var dots = house.SeatColours();
            for (int i = 0; i < dots.Count; i++)
                Assert.AreEqual(i < seats, dots[i].HasValue && coalitionColours.Contains(dots[i]!.Value), $"{where}: seat {i}");
            Assert.AreEqual(size, dots.Count);
            Assert.AreEqual(leg.Size.ToString(), ui.Q<Label>("cabinet-house-size").text, where);
            Assert.AreEqual($"Coalition: {seats} of {size} seats · stability {PlayerText.Percent(g.State.CoalitionStability)}", ui.Q<Label>("cabinet-coalition").text, where);
            Assert.AreEqual($"Opposition: {leg.Seats.Where(kv => !gov.CoalitionOn(g.State.Date).Contains(kv.Key)).Sum(kv => kv.Value)} seats", ui.Q<Label>("cabinet-opposition").text, where);
            Assert.AreEqual($"Majority: {size / 2 + 1} seats", ui.Q<Label>("cabinet-majority").text, where);
            var top = leg.Seats.OrderByDescending(kv => kv.Value).First();
            Assert.AreEqual($"Largest party: {C.Parties.First(p => p.Id == top.Key).Name}, {top.Value} seats", ui.Q<Label>("cabinet-largest").text, where);
            Assert.AreEqual($"Answerable to the {leg.Body} · {leg.Size} members", ui.Q<Label>("cabinet-legislature").text, where);
            Assert.AreEqual((float)g.State.CoalitionStability, ui.Q<UiBar>("cabinet-stability").Value, 1e-6f, where);
            foreach (var kv in leg.Seats.Where(kv => kv.Value > 0))
                Assert.AreEqual($"{C.Parties.First(p => p.Id == kv.Key).Name} {kv.Value}", ui.Q<Label>("party-" + kv.Key + "-label").text, where);
            cabinet.Dispose();
        }
    }

    /// <summary>#41 M5 (WCAG 1.4.1): whether a party is in the coalition is readable without the swatch's colour. Each
    /// party's chip sits in a group whose first child is a heading naming its side, "Coalition" or "Opposition" (Hebrew
    /// קואליציה, אופוזיציה), the side the half circle seats it on; in each legislature, English and Hebrew.</summary>
    [Test]
    public void EachPartyChipSaysWhetherThePartyIsInTheCoalition()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        foreach (var (text, coalition, opposition) in new[] { (Text, "Coalition", "Opposition"), (he, "קואליציה", "אופוזיציה") })
            foreach (var date in OneDatePerLegislature())
            {
                var g = At(date);
                using var ui = new UiTestKit();
                var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, text), text, null);
                var leg = Offices.Legislature(C, g.State.Date)!;
                var blocks = CabinetPresenter.Blocks(C, leg, Offices.Government(C, g.State.Date), g.State.Date);
                Assert.IsTrue(blocks.Any(b => b.Coalition) && blocks.Any(b => !b.Coalition), $"{date:yyyy-MM-dd}: precondition, both sides seated");
                foreach (var b in blocks)
                {
                    var where = $"{text["ui.lang"]} {g.State.Date:yyyy-MM-dd} {b.Party}";
                    var group = ui.Q<VisualElement>("party-" + b.Party).parent;
                    Assert.IsTrue(group.ClassListContains("party-group"), where + ": the chip sits in a labelled group");
                    var heading = group.Children().First() as Label;
                    Assert.IsNotNull(heading, where + ": the group opens with its heading");
                    Assert.AreEqual(b.Coalition ? coalition : opposition, heading!.text, where);
                }
                cabinet.Dispose();
            }
    }

    /// <summary>#41 M5: the groups follow a coalition change inside one government and one legislature (governments.yaml
    /// coalition_changes, 25 May 1953: the General Zionists, the Sephardim and the Yemenite Association leave) on the same
    /// screen, as the half circle does. The date is set directly: play stops in January 1952, the content goes on. Seats the
    /// content does not record are on neither side, and a side without a party gets no heading. No legislature in the
    /// content has either today, so the second half takes the start's opposition seats away from a copy of the content.</summary>
    [Test]
    public void ThePartyGroupsFollowACoalitionChangeAndLeaveUnrecordedSeatsOut()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var change = new DateTime(1953, 5, 25);
        g.State.Date = change.AddDays(-1);
        using (var ui = new UiTestKit())
        {
            var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
            string Side(string party) => ((Label)ui.Q<VisualElement>("party-" + party).parent.Children().First()).text;
            var leaving = new[] { "general_zionists", "sephardim", "yemenite_association" };
            foreach (var p in leaving) Assert.AreEqual("Coalition", Side(p), "24 May 1953: " + p);
            g.State.Date = change;
            cabinet.Render();
            foreach (var p in leaving) Assert.AreEqual("Opposition", Side(p), "25 May 1953, the same screen: " + p);
            Assert.AreEqual("Coalition", Side("mapai"), "25 May 1953: mapai");
            cabinet.Dispose();
        }

        var c = ContentLoader.Load(Paths.Content);   // a copy: the shared content stays as it is
        var start = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var leg = Offices.Legislature(c, start.State.Date)!;
        var coalition = Offices.Government(c, start.State.Date)!.CoalitionOn(start.State.Date);
        foreach (var party in leg.Seats.Keys.Where(k => !coalition.Contains(k)).ToList()) leg.Seats[party] = 0;
        using (var ui = new UiTestKit())
        {
            var cabinet = new CabinetPresenter(ui.Root, new SimBridge(start, Text), Text, null);
            var parties = ui.Q<VisualElement>("cabinet-parties");
            Assert.AreSame(parties, ui.Q<VisualElement>("party-unrecorded").parent, "the seats not recorded are on neither side");
            CollectionAssert.AreEqual(new[] { "Coalition" }, parties.Query<Label>(className: "party-group-heading").ToList().Select(l => l.text),
                "no heading over an opposition without a party");
            cabinet.Dispose();
        }
    }

    /// <summary>Review I1: the majority line runs, row by row, between the last seat of a majority and the next, clear of
    /// every dot, on the 37-seat Provisional State Council (promo 04) and the 120-seat Knesset; the layout keeps dots apart.</summary>
    [Test]
    public void TheMajorityLineSeparatesAMajorityFromTheRest()
    {
        foreach (var size in new[] { 37, 120, 1, 2, 7, 61 })
        {
            var seats = Hemicycle.Layout(size);
            Assert.AreEqual(size, seats.Count, size + " seats");
            for (int i = 1; i < seats.Count; i++)
                Assert.GreaterOrEqual(Mathf.Atan2(seats[i - 1].y, seats[i - 1].x) + 1e-4f, Mathf.Atan2(seats[i].y, seats[i].x), $"{size}: left to right");
            foreach (var p in seats) Assert.LessOrEqual(p.magnitude, 1.0001f, $"{size}: inside the unit half circle");
            var r = Hemicycle.DotRadius(size);
            for (int i = 0; i < seats.Count; i++)
                for (int j = i + 1; j < seats.Count; j++)
                    Assert.Greater((seats[i] - seats[j]).magnitude, 2 * r, $"{size}: dots {i} and {j} overlap");
            Assert.LessOrEqual(Hemicycle.RowCounts(size).Count(n => n % 2 == 1), 1, $"{size}: at most one seat at the top");
        }
        foreach (var size in new[] { 37, 120, 61, 7 })
        {
            var house = new Hemicycle();
            house.Set(new[] { new Hemicycle.Group("a", size / 2 + 1, Color.blue, true), new Hemicycle.Group("b", size - size / 2 - 1, Color.gray, false) }, size);
            var line = house.MajorityLine;
            var places = Hemicycle.Places(size);
            // The dot plus half the line's 2 px stroke, in unit radii at the shipped box (Statecraft.uss .hemicycle-box, 320 x 168).
            var dot = Hemicycle.DotRadius(size) + Hemicycle.LineWidth / 2f / Hemicycle.ScaleFor(HemicycleBox - new Vector2(1.5f, 1.5f), size);
            Assert.Greater(line.Count, 1, size + ": a line");
            // No dot touches the line: every seat centre is more than a dot's radius from every segment.
            for (int i = 0; i < size; i++)
                for (int k = 1; k < line.Count; k++)
                {
                    var d = SegmentDistance(places[i].Position, line[k - 1], line[k]);
                    Assert.Greater(d, dot, $"{size}: seat {i + 1} is {d:0.000} from the line (dot radius {dot:0.000})");
                }
            // In each row, the first Majority seats from the left lie left of the line's step in that row, the rest right.
            var cuts = house.MajorityCuts;
            for (int i = 0; i < size; i++)
                Assert.AreEqual(i < house.Majority, places[i].Angle > cuts[places[i].Row], $"{size}: seat {i + 1} (row {places[i].Row}) is on the wrong side of the line");
        }
        var h = new Hemicycle();
        Assert.Throws<ArgumentException>(() => h.Set(new[] { new Hemicycle.Group("a", 38, Color.blue, true) }, 37), "never more seats than the body has");
        h.Set(new[] { new Hemicycle.Group("a", 30, Color.blue, true) }, 37);
        Assert.AreEqual(7, h.SeatColours().Count(c => c == null), "unassigned seats stay unassigned, never given to a party");
        var changes = h.Changes;
        h.Set(new[] { new Hemicycle.Group("a", 30, Color.blue, true) }, 37);
        Assert.AreEqual(changes, h.Changes, "the same house again is not redrawn (review I2)");
    }

    /// <summary>The half circle's box in logical pixels (Statecraft.uss .hemicycle-box); BothScreensFitEveryWindowShape checks it.</summary>
    static readonly Vector2 HemicycleBox = new Vector2(320f, 168f);

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a; var t = ab.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + t * ab)).magnitude;
    }

    /// <summary>Each card is its office holder's people.yaml record. The Prime Minister's card lists every office the PM
    /// holds (Ben-Gurion was also Minister of Defense in 1948); those offices get no second card (review M5) but stay
    /// selectable from a button under the PM card, so no real office is hidden.</summary>
    [Test]
    public void EveryCardShowsItsHoldersRealRecord()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        var lineup = Cabinet.Lineup(C, g.State);
        var pm = Cabinet.Holder(C, g.State, "prime_minister")!;
        foreach (var seat in lineup)
        {
            var id = "seat-" + seat.Office.Id;
            Assert.IsNotNull(ui.Root.Q<Button>(id), id + ": every office can be selected");
            if (seat.Office.Id != CabinetPresenter.PrimeMinister && seat.Holder?.Id == pm.Id)
            {
                Assert.AreEqual(seat.Office.Title, ui.Q<Button>(id).text, id + ": the PM's other office, under the PM card");
                Assert.IsNull(ui.Q<ScrollView>("cabinet-seats").Q<Button>(id), id + ": no second card for the PM");
                UiTestKit.Press(ui.Q<Button>(id));
                Assert.AreEqual(seat.Office.Id, cabinet.Selected, id + ": selectable");
                continue;
            }
            if (seat.Office.Id != CabinetPresenter.PrimeMinister)
                Assert.AreEqual(seat.Office.Title, ui.Q<Label>(id + "-office").text, id);
            if (seat.Holder == null)
            {
                Assert.AreEqual("(vacant)", ui.Q<Label>(id + "-holder").text, id);
                Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>(id + "-competence-row").style.display.value, id + ": no bars for a vacancy");
                continue;
            }
            var p = seat.Holder;
            Assert.AreEqual(p.Name, ui.Q<Label>(id + "-holder").text, id);
            var party = Offices.PartyOf(C, p, g.State.Date);
            var partyName = party == "" ? "no party" : C.Parties.First(x => x.Id == party).Name;
            StringAssert.EndsWith(partyName, ui.Q<Label>(id + "-party").text, id);
            Assert.AreEqual(p.Competence / 10f, ui.Q<UiBar>(id + "-competence").Value, 1e-6f, id);
            Assert.AreEqual(p.Loyalty / 10f, ui.Q<UiBar>(id + "-loyalty").Value, 1e-6f, id);
            Assert.AreEqual(p.Competence.ToString(), ui.Q<Label>(id + "-competence-value").text, id);
            Assert.AreEqual(p.Loyalty.ToString(), ui.Q<Label>(id + "-loyalty-value").text, id);
        }
        var held = lineup.Where(x => x.Holder?.Id == pm.Id).Select(x => x.Office.Title).ToList();
        Assert.AreEqual(string.Join(" · ", held), ui.Q<Label>("seat-prime_minister-office").text);
        CollectionAssert.Contains(held, "Minister of Defense", "Ben-Gurion held Defence too");
        Assert.AreEqual("Party: Mapai", ui.Q<Label>("seat-prime_minister-party").text);
        Assert.IsNull(ui.Q<ScrollView>("cabinet-seats").Q<Button>("seat-prime_minister"), "the PM is the side card, not a grid card");
        cabinet.Dispose();
        // Review M3: a disposed screen keeps no card, chip or seat.
        Assert.AreEqual(0, ui.Q<ScrollView>("cabinet-seats").contentContainer.childCount);
        Assert.AreEqual(0, ui.Q<VisualElement>("cabinet-parties").childCount);
        Assert.AreEqual(0, ui.Q<Button>("seat-prime_minister").childCount);
        Assert.AreEqual(0, ui.Q<Hemicycle>("cabinet-hemicycle").Size);
    }

    [Test]
    public void APersonWithoutAPortraitGetsTheirInitialsNeverAnotherFace()
    {
        Assert.AreEqual("DB", PersonCard.Initials("David Ben-Gurion"));
        Assert.AreEqual("HS", PersonCard.Initials("Haim-Moshe Shapira"));
        Assert.AreEqual("M", PersonCard.Initials("Mordechai"));
        Assert.AreEqual("", PersonCard.Initials(""));
        // In Hebrew the plate shows the first name's initial and the surname (Beri, 17 October 2026), not two letters.
        Assert.AreEqual("י. ידין", PersonCard.InitialAndSurname("יגאל ידין"));
        Assert.AreEqual("ש. בן-מאיר", PersonCard.InitialAndSurname("שלמה-ישראל בן-מאיר"));
        Assert.AreEqual("ש. בורג", PersonCard.InitialAndSurname("שלמה יוסף בורג"));
        Assert.AreEqual("", PersonCard.InitialAndSurname(""));
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        var catalog = ScriptableObject.CreateInstance<PortraitCatalog>();
        try
        {
            catalog.entries.Add(new PortraitCatalog.Entry { personId = "kaplan", image = Texture2D.whiteTexture, focusY = 0.3f });
            new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, catalog);
            new DefencePresenter(ui.Root, new SimBridge(g, Text), Text, catalog);
            foreach (var card in ui.Root.Query<VisualElement>(className: "card-plate").ToList())
            {
                var prefix = card.name.Substring(0, card.name.Length - "-plate".Length);
                var portrait = ui.Q<VisualElement>(prefix + "-portrait");
                var initials = ui.Q<Label>(prefix + "-initials");
                var holder = ui.Q<Label>(prefix + "-holder").text;
                bool kaplan = holder.StartsWith("Eliezer Kaplan");
                Assert.AreEqual(kaplan ? DisplayStyle.Flex : DisplayStyle.None, portrait.style.display.value, prefix);
                Assert.AreEqual(kaplan ? DisplayStyle.None : DisplayStyle.Flex, initials.style.display.value, prefix);
                if (!kaplan) Assert.AreEqual(holder == "(vacant)" ? "" : PersonCard.Initials(holder), initials.text, prefix);
            }
        }
        finally { Object.DestroyImmediate(catalog); }
    }

    /// <summary>In Hebrew a card with no portrait shows the first name's initial and the surname ("י. ידין", Beri), and the
    /// longest such plate text of any person fits every plate at a desktop, the short browser, a laptop and a phone size.</summary>
    [UnityTest]
    public IEnumerator InHebrewThePlateShowsTheInitialAndSurnameAndItFits()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var plates = C.People.Where(p => p.NameHe != "").Select(p => PersonCard.InitialAndSurname(p.NameHe)).ToList();
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1568, 752), (1024, 768), (390, 844) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("HebrewInitialsTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var owned = new List<IDisposable>();
            try
            {
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                var bridge = new SimBridge(g, he);
                owned.Add(new HudPresenter(root, bridge, he));
                owned.Add(new CabinetPresenter(root, bridge, he, null));
                owned.Add(new DefencePresenter(root, bridge, he));
                var nav = new ScreenNav(root); owned.Add(nav);
                foreach (var s in new[] { "cabinet", "defence" })
                {
                    nav.Open(s);
                    for (int i = 0; i < 6; i++) yield return null;
                    if (s == "defence")   // no catalog here, so Yadin, the Chief of Staff in February 1950, has no portrait
                        Assert.AreEqual("י. ידין", root.Q<Label>("lead-chief_of_staff-initials").text, "Yadin's plate, no portrait");
                    var shown = root.Q("screen-" + s).Query<Label>(className: PersonCard.InitialsClass).ToList().Where(l => l.contentRect.width > 0).ToList();
                    if (shown.Count == 0) problems.Add($"{w}x{h} {s}: no plate laid out");
                    foreach (var l in shown)
                    {
                        var (widest, px) = plates.Select(t => (t, l.MeasureTextSize(t, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x))
                            .OrderByDescending(x => x.Item2).First();
                        if (px > l.contentRect.width + 0.5f) problems.Add($"{w}x{h} {l.name}: '{widest}' is {px:0} px in {l.contentRect.width:0} px at {l.resolvedStyle.fontSize} px");
                    }
                }
            }
            finally
            {
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems.Distinct().Take(40)));
    }

    [Test]
    public void TheLeadershipIsTheRealCommandAndTheBarsAreTheSims()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        var bridge = new SimBridge(g, Text);
        new ToastPresenter(ui.Root, bridge, Text);
        var defence = new DefencePresenter(ui.Root, bridge, Text);
        // One card per person (review M5): Ben-Gurion's card lists Prime Minister and Minister of Defense; the Chief of Staff has his own.
        var shown = new List<string>();
        foreach (var office in DefencePresenter.Leaders)
        {
            var holder = Cabinet.Holder(C, g.State, office)!;
            var card = ui.Q<VisualElement>("lead-" + office);
            var first = DefencePresenter.Leaders.First(o => Cabinet.Holder(C, g.State, o)?.Id == holder.Id);
            if (first != office)
            {
                Assert.AreEqual(DisplayStyle.None, card.style.display.value, office + ": merged into " + first);
                StringAssert.Contains(C.Offices.First(o => o.Id == office).Title, ui.Q<Label>("lead-" + first + "-office").text, office);
                continue;
            }
            shown.Add(office);
            Assert.AreEqual(holder.Name, ui.Q<Label>("lead-" + office + "-holder").text, office);
            StringAssert.StartsWith(C.Offices.First(o => o.Id == office).Title, ui.Q<Label>("lead-" + office + "-office").text, office);
            Assert.AreEqual(holder.Competence / 10f, ui.Q<UiBar>("lead-" + office + "-competence").Value, 1e-6f, office);
            Assert.AreEqual(holder.Loyalty / 10f, ui.Q<UiBar>("lead-" + office + "-loyalty").Value, 1e-6f, office);
        }
        CollectionAssert.AreEqual(new[] { "prime_minister", "chief_of_staff" }, shown, "1948: Ben-Gurion once, Dori once");
        Assert.AreEqual("Prime Minister\nMinister of Defense", ui.Q<Label>("lead-prime_minister-office").text);
        var bar = ui.Q<UiBar>("mob-bar");
        Assert.AreEqual((float)g.State.Mobilization, bar.Value, 1e-6f);
        Assert.AreEqual((float)SecuritySystem.MinMobilization(C, g.State.Date, g.State.Rung), bar.Mark!.Value, 1e-6f, "the level's minimum is marked");
        Assert.AreEqual(PlayerText.Percent(g.State.Mobilization), ui.Q<Label>("mob-percent").text);
        Assert.AreEqual((float)g.State.Budget[DefencePresenter.BudgetLine], ui.Q<UiBar>("defence-budget-bar").Value, 1e-6f);
        Assert.AreEqual($"{PlayerText.Percent(g.State.Budget[DefencePresenter.BudgetLine])} of budget spending", ui.Q<Label>("defence-budget-share").text);
        Assert.AreEqual("LEADERSHIP", ui.Q<Label>("defence-title-leadership").text, "the caption from strings, in capitals");
        // An order to lower the level: the target rung is marked, and the bar's minimum follows it.
        ui.Press("de-escalate");
        Assert.IsTrue(ui.Q<VisualElement>("rung-4").ClassListContains("rung-target"));
        Assert.IsTrue(ui.Q<VisualElement>("rung-5").ClassListContains("rung-current"));
        Assert.IsFalse(ui.Q<VisualElement>("rung-5").ClassListContains("rung-target"));
        Assert.AreEqual((float)SecuritySystem.MinMobilization(C, g.State.Date, 4), bar.Mark!.Value, 1e-6f);
        ui.Press("mob-down");
        Assert.AreEqual((float)defence.ProposedMobilization, bar.Value, 1e-6f, "the bar shows the ordered call-up");
        ui.Press("escalate");
        Assert.IsFalse(ui.Root.Query<VisualElement>(className: "rung-target").ToList().Any(), "no order, no target");
        // Review M4: a queued budget moves the share as a queued call-up moves the call-up.
        var shares = new Dictionary<string, double>(g.State.Budget);
        var other = shares.Keys.First(k => k != DefencePresenter.BudgetLine);
        shares[DefencePresenter.BudgetLine] -= 0.05; shares[other] += 0.05;
        Assert.IsNull(bridge.Queue(new AllocateBudget(shares)));
        Assert.AreEqual((float)shares[DefencePresenter.BudgetLine], ui.Q<UiBar>("defence-budget-bar").Value, 1e-6f);
        Assert.AreEqual($"{PlayerText.Percent(shares[DefencePresenter.BudgetLine])} of budget spending", ui.Q<Label>("defence-budget-share").text);
        defence.Dispose();
        foreach (var office in DefencePresenter.Leaders) Assert.AreEqual(0, ui.Q<VisualElement>("lead-" + office).childCount, office + ": emptied on Dispose");
        Assert.IsNull(bar.Mark);
    }

    /// <summary>Review I5: each figure on either screen is bound to its own source. Every text with a digit in it, and every
    /// bar, must be one this test names, with its exact expected value from the sim: a new number or bar fails until it is
    /// bound on purpose, so no promo figure (readiness, strength) can slip in.</summary>
    [Test]
    public void NoFigureOnEitherScreenIsInvented()
    {
        foreach (var date in OneDatePerLegislature())
        {
            var g = At(date);
            var s = g.State;
            using var ui = new UiTestKit();
            var bridge = new SimBridge(g, Text);
            var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
            var defence = new DefencePresenter(ui.Root, bridge, Text);
            var where = s.Date.ToString("yyyy-MM-dd");
            var texts = new Dictionary<string, string>();
            var bars = new Dictionary<string, float>();
            var leg = Offices.Legislature(C, s.Date)!;
            var gov = Offices.Government(C, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(C, s.Date);
            var blocks = CabinetPresenter.Blocks(C, leg, gov, s.Date);
            texts["cabinet-coalition"] = $"Coalition: {seats} of {size} seats · stability {PlayerText.Percent(s.CoalitionStability)}";
            texts["cabinet-opposition"] = $"Opposition: {leg.Seats.Where(kv => gov == null || !gov.CoalitionOn(s.Date).Contains(kv.Key)).Sum(kv => kv.Value)} seats";
            texts["cabinet-majority"] = $"Majority: {leg.Size / 2 + 1} seats";
            var top = leg.Seats.OrderByDescending(kv => kv.Value).First();
            texts["cabinet-largest"] = $"Largest party: {C.Parties.First(p => p.Id == top.Key).Name}, {top.Value} seats";
            texts["cabinet-legislature"] = $"Answerable to the {leg.Body} · {leg.Size} members";
            texts["cabinet-house-size"] = leg.Size.ToString();
            foreach (var b in blocks) texts["party-" + b.Party + "-label"] = $"{C.Parties.First(p => p.Id == b.Party).Name} {leg.Seats[b.Party]}";
            bars["cabinet-stability"] = (float)s.CoalitionStability;
            void Person(string prefix, Person? p)
            {
                if (p == null) return;
                texts[prefix + "-competence-value"] = p.Competence.ToString(); texts[prefix + "-loyalty-value"] = p.Loyalty.ToString();
                bars[prefix + "-competence"] = p.Competence / 10f; bars[prefix + "-loyalty"] = p.Loyalty / 10f;
            }
            foreach (var seat in Cabinet.Lineup(C, s)) Person("seat-" + seat.Office.Id, seat.Holder);
            foreach (var office in DefencePresenter.Leaders) Person("lead-" + office, Cabinet.Holder(C, s, office));
            for (int rung = 1; rung <= 5; rung++) texts[$"rung-{rung}-name"] = Text[$"defence.rung{rung}"];
            var floor = SecuritySystem.MinMobilization(C, s.Date, s.Rung);
            var callUp = SecuritySystem.EffectiveMobilization(C, s.Date, s.Rung, s.Mobilization);
            texts["mob-percent"] = PlayerText.Percent(callUp);
            texts["mob-level"] = $"{PlayerText.Percent(callUp)} of reserves called up (level {s.Rung} needs at least {PlayerText.Percent(floor)})";
            texts["mob-cost"] = $"Costs {PlayerText.Money(C, "treasury", EconomySystem.MobilizationCost(C, s.Date, callUp))} a year in pay and lost work";
            texts["mob-down"] = "-5%"; texts["mob-up"] = "+5%";   // the step, DefencePresenter.MobilizationStep
            Assert.AreEqual(0.05, DefencePresenter.MobilizationStep, 1e-12);
            texts["ceasefire"] = s.Rung == 5 ? Text.Format("defence.ceasefire", s.CeasefireWeeksLeft) : "";
            texts["defence-budget-share"] = $"{PlayerText.Percent(s.Budget[DefencePresenter.BudgetLine])} of budget spending";
            bars["mob-bar"] = (float)callUp; bars["defence-budget-bar"] = (float)s.Budget[DefencePresenter.BudgetLine];
            foreach (var name in new[] { "screen-cabinet", "screen-defence" })
            {
                foreach (var t in ui.Q<VisualElement>(name).Query<TextElement>().ToList().Where(t => t.text.Any(char.IsDigit)))
                {
                    Assert.IsTrue(texts.ContainsKey(t.name), $"{where}: '{t.text}' in {t.name} is a figure no test binds to the sim");
                    Assert.AreEqual(texts[t.name], t.text, $"{where}: {t.name}");
                }
                foreach (var bar in ui.Q<VisualElement>(name).Query<UiBar>().ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None && b.parent.style.display != DisplayStyle.None))
                {
                    Assert.IsTrue(bars.ContainsKey(bar.name), $"{where}: bar {bar.name} = {bar.Value} is bound to nothing");
                    Assert.AreEqual(bars[bar.name], bar.Value, 1e-6f, $"{where}: bar {bar.name}");
                }
            }
            // No promo figure the sim lacks.
            var words = string.Join("\n", ui.Q<VisualElement>("screen-defence").Query<TextElement>().ToList().Select(l => l.text));
            foreach (var missing in new[] { "Readiness", "Strength", "Intelligence", "Procurement", "Stance", "Air Force", "Navy" })
                StringAssert.DoesNotContain(missing, words, "the sim has no " + missing);
            cabinet.Dispose(); defence.Dispose();
        }
    }

    static float Lum(Color c, Color under)
    {
        float Ch(float v, float u) { var x = v * c.a + u * (1 - c.a); return x <= 0.04045f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f); }
        return 0.2126f * Ch(c.r, under.r) + 0.7152f * Ch(c.g, under.g) + 0.0722f * Ch(c.b, under.b);
    }

    static float Contrast(Color a, Color b, Color under) { float x = Lum(a, under), y = Lum(b, under); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }

    static readonly Color ScreenNavy = new Color32(11, 18, 32, 255), Deep = new Color32(9, 14, 26, 255);

    /// <summary>Non-text contrast (WCAG 1.4.11): every bar's fill is at least 3:1 against its track, and every party colour
    /// at least 3:1 against the screen behind the half circle.</summary>
    [UnityTest]
    public IEnumerator BarsAndPartyColoursMeetThreeToOne()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        var defence = new DefencePresenter(ui.Root, bridge, Text);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        new ScreenNav(ui.Root).Open("cabinet");
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        for (int i = 0; i < 4; i++) yield return null;
        try
        {
            var seen = new HashSet<string>();
            foreach (var bar in ui.Root.Query<UiBar>().ToList())
            {
                var modifier = bar.GetClasses().FirstOrDefault(c => c.StartsWith("ui-bar--"));
                Assert.IsNotNull(modifier, bar.name + ": a colour modifier");
                seen.Add(modifier!);
                var ratio = Contrast(bar.Fill.resolvedStyle.backgroundColor, bar.resolvedStyle.backgroundColor, ScreenNavy);
                Assert.GreaterOrEqual(ratio, 3f, $"{bar.name} ({modifier}): fill on track {ratio:0.00}:1");
            }
            CollectionAssert.IsSupersetOf(seen, new[] { "ui-bar--competence", "ui-bar--loyalty", "ui-bar--stability", "ui-bar--mobilization", "ui-bar--budget" });
            foreach (var colour in new[] { CabinetPresenter.CoalitionColour, CabinetPresenter.OppositionColour })
                Assert.GreaterOrEqual(Contrast(colour, ScreenNavy, ScreenNavy), 3f, $"seat colour {colour}");
            // Review M2: two flat colours, coalition and opposition; a colour never stands for one party.
            Assert.AreNotEqual(CabinetPresenter.CoalitionColour, CabinetPresenter.OppositionColour);
            var house = ui.Q<Hemicycle>("cabinet-hemicycle");
            CollectionAssert.AreEquivalent(new[] { CabinetPresenter.CoalitionColour, CabinetPresenter.OppositionColour }, house.Groups.Select(b => b.Colour).Distinct());
            foreach (var b in house.Groups) Assert.AreEqual(b.Coalition ? CabinetPresenter.CoalitionColour : CabinetPresenter.OppositionColour, b.Colour, b.Party);
        }
        finally { cabinet.Dispose(); defence.Dispose(); }
    }

    /// <summary>The HUD's focus rules on the cards (reviews I3, N1): keyboard focus changes a card by at least 3:1 and keeps
    /// its text at least 4.5:1, and a pointer click leaves no card lit.</summary>
    [UnityTest]
    public IEnumerator ACardsFocusIsVisibleAndAClickLeavesItUnlit()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        var blur = new PointerBlur(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        new ScreenNav(ui.Root).Open("cabinet");
        try
        {
            for (int i = 0; i < 4; i++) yield return null;
            foreach (var name in new[] { "seat-finance_minister", "seat-prime_minister" })
            {
                var card = ui.Q<Button>(name);
                var before = card.resolvedStyle.backgroundColor;
                card.Focus();
                for (int i = 0; i < 10 && card.focusController.focusedElement != card; i++) yield return null;
                yield return null; yield return null;
                Assert.AreSame(card, card.focusController.focusedElement, name + ": focused");
                var fill = card.resolvedStyle.backgroundColor;
                Assert.GreaterOrEqual(Contrast(before, fill, Deep), 3f, name + ": focus changes the card by 3:1");
                foreach (var label in card.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && l.resolvedStyle.display == DisplayStyle.Flex && l.GetFirstAncestorOfType<VisualElement>()?.ClassListContains("card-plate") != true))
                    Assert.GreaterOrEqual(Contrast(label.resolvedStyle.color, fill, Deep), 4.5f, $"{name}: '{label.text}' on the focus fill");
                using (var e = ClickEvent.GetPooled()) { e.target = card; card.SendEvent(e); }
                yield return null; yield return null;
                Assert.AreNotSame(card, card.focusController.focusedElement, name + ": a click leaves no focus");
            }
        }
        finally { cabinet.Dispose(); blur.Dispose(); }
    }

    /// <summary>#41 M6 (WCAG 1.4.11): the selected Cabinet seat carries a mark that is not a colour change, a tag on its
    /// plate reading "Selected" (Hebrew מסומן) at 4.5:1, which stays when keyboard focus moves to another card; the other
    /// cards show none, and selecting moves nothing on the card. For a grid card and the Prime Minister's, English and
    /// Hebrew.</summary>
    [UnityTest]
    public IEnumerator TheSelectedSeatIsTaggedWithoutMovingItsCard()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        foreach (var (text, hebrew) in new[] { (Text, false), (he, true) })
        {
            using var ui = new UiTestKit();
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, text), text, null);
            ui.Q<VisualElement>("root").RemoveFromClassList("loading");
            ui.Q<VisualElement>("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
            new ScreenNav(ui.Root).Open("cabinet");
            try
            {
                for (int i = 0; i < 4; i++) yield return null;
                foreach (var (office, other) in new[] { ("finance_minister", "seat-foreign_minister"), ("prime_minister", "seat-finance_minister") })
                {
                    var where = $"{text["ui.lang"]} {office}";
                    var card = ui.Q<Button>("seat-" + office);
                    // Another seat first, so the candidate list is open in both states: it opening squeezes the Prime
                    // Minister's portrait (M7), which is not what this measures.
                    cabinet.Select(other.Substring("seat-".Length));
                    for (int i = 0; i < 4; i++) yield return null;
                    var rects = card.Children().Select(e => e.layout).Prepend(card.layout).ToList();
                    cabinet.Select(office);
                    for (int i = 0; i < 4; i++) yield return null;
                    Assert.IsTrue(card.ClassListContains("seat-selected"), where + ": precondition, selected");
                    Label? Tag(VisualElement c) => c.Query<Label>(className: "card-selected").ToList()
                        .FirstOrDefault(l => l.resolvedStyle.display == DisplayStyle.Flex && l.worldBound.width > 0);
                    var tag = Tag(card);
                    Assert.IsNotNull(tag, where + ": the selected card carries a visible tag");
                    Assert.AreEqual(text["card.selected"], tag!.text, where);
                    Assert.IsTrue(card.worldBound.Contains(tag.worldBound.min) && card.worldBound.Contains(tag.worldBound.max - Vector2.one), where + ": the tag is on the card");
                    Assert.GreaterOrEqual(Contrast(tag.resolvedStyle.color, tag.resolvedStyle.backgroundColor, Deep), 4.5f, where + ": the tag's text");
                    foreach (var c in ui.Root.Query<Button>(className: "seat").ToList().Append(ui.Q<Button>("seat-" + CabinetPresenter.PrimeMinister)).Where(c => c != card))
                        Assert.IsNull(Tag(c), where + ": " + c.name + " is not tagged");
                    var after = card.Children().Select(e => e.layout).Prepend(card.layout).ToList();
                    for (int i = 0; i < rects.Count; i++)
                        Assert.IsTrue(Vector2.Distance(rects[i].position, after[i].position) < 0.5f && Vector2.Distance(rects[i].size, after[i].size) < 0.5f,
                            $"{where}: element {i} moved, {rects[i]} -> {after[i]}");

                    var next = ui.Q<Button>(other);
                    next.Focus();
                    for (int i = 0; i < 10 && next.focusController.focusedElement != next; i++) yield return null;
                    yield return null; yield return null;
                    Assert.AreSame(next, next.focusController.focusedElement, where + ": precondition, focus moved on");
                    Assert.IsNotNull(Tag(card), where + ": the tag stays once focus moves on");
                    next.Blur();
                }
            }
            finally { cabinet.Dispose(); }
        }
    }

    /// <summary>#41 M6: an office the Prime Minister holds himself (Defense, at the start) is a plain button under his card,
    /// with no plate for the tag; selected, its rim doubles and the button keeps its size. On a 3840x2160 panel, so a
    /// logical pixel is wider than a screen pixel and 1 px and 2 px do not snap to the same width.</summary>
    [UnityTest]
    public IEnumerator ThePrimeMinistersSelectedOfficeDoublesItsRim()
    {
        var rt = new RenderTexture(3840, 2160, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("PmOfficeTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var cabinet = new CabinetPresenter(root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text), Text, null);
        var nav = new ScreenNav(root);
        try
        {
            root.Q("root").RemoveFromClassList("loading");
            nav.Open("cabinet");
            for (int i = 0; i < 4; i++) yield return null;
            var offices = root.Query<Button>(className: "pm-office").ToList();
            Assert.IsNotEmpty(offices, "precondition: the Prime Minister holds another office");
            var office = offices[0];
            Assert.IsFalse(office.ClassListContains("seat-selected"), "precondition: not selected");
            float thin = office.resolvedStyle.borderTopWidth; var size = office.worldBound.size;
            Assert.Greater(thin, 0f, "precondition: an office not selected has a rim");
            cabinet.Select(office.name.Substring("seat-".Length));
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(office.ClassListContains("seat-selected"), "precondition: the office is selected");
            Assert.AreEqual(2f * thin, office.resolvedStyle.borderTopWidth, 0.25f * thin, "the selected office's rim doubles");
            Assert.Less(Vector2.Distance(size, office.worldBound.size), 0.5f, $"the selected office keeps its size, {size} -> {office.worldBound.size}");
        }
        finally { nav.Dispose(); cabinet.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    /// <summary>Both screens at the window shapes the HUD supports (16:9 at 1920x1080 and 1366x768, 16:10, 4:3, 5:4, the
    /// wide-and-short browser, square and narrower): each opens under the top bar, clear of the minimap column and the
    /// minimap (over the info panel, which it is drawn above), and nothing on it leaves it or its panel sideways; every
    /// one-line text fits its box.</summary>
    [UnityTest]
    public IEnumerator BothScreensFitEveryWindowShape()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1366, 768), (1280, 720), (1440, 900), (1920, 1200), (1024, 768), (1280, 1024), (1531, 784), (1024, 1024), (960, 1080), (800, 1000), (960, 600), (390, 844) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("ScreensTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var owned = new List<IDisposable>();
            try
            {
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var bridge = new SimBridge(g, Text);
                owned.Add(new HudPresenter(root, bridge, Text));
                owned.Add(new CabinetPresenter(root, bridge, Text, null));
                owned.Add(new DefencePresenter(root, bridge, Text));
                var nav = new ScreenNav(root); owned.Add(nav);
                foreach (var s in new[] { "cabinet", "defence" })
                {
                    nav.Open(s);
                    for (int i = 0; i < 6; i++) yield return null;
                    var where = $"{w}x{h} {s}";
                    var screen = root.Q("screen-" + s); var sr = screen.worldBound;
                    if (!(sr.width > 300)) { problems.Add($"{where}: the screen is {sr}"); continue; }
                    if (sr.yMin < root.Q("top-bar").worldBound.yMax - 0.5f) problems.Add($"{where}: starts over the top bar");
                    // Clear of the minimap column, or (two-row HUD, #210) full width with the column hidden.
                    var column = root.Q("right-column");
                    if (column.resolvedStyle.display != DisplayStyle.None && sr.Overlaps(column.worldBound)) problems.Add($"{where}: overlaps the minimap column {column.worldBound}");
                    var parent = root.Q("root");
                    if (parent.IndexOf(screen) < parent.IndexOf(root.Q("info-panel"))) problems.Add($"{where}: drawn under the info panel");
                    // Everything shown stays inside the screen sideways, and inside its panel (a card, a Defence panel, the house).
                    foreach (var e in screen.Query<VisualElement>().ToList().Where(e => e != screen && e.resolvedStyle.display == DisplayStyle.Flex && e.worldBound.width > 0
                                                                                         && !(e is UiFrame) && e.GetFirstAncestorOfType<Scroller>() == null && !(e is Scroller)))
                    {
                        var r = e.worldBound;
                        var clip = e.GetFirstAncestorOfType<ScrollView>();
                        if (clip != null && (r.yMax < clip.contentViewport.worldBound.yMin || r.yMin > clip.contentViewport.worldBound.yMax)) continue;   // scrolled out of view
                        if (r.xMin < sr.xMin - 0.5f || r.xMax > sr.xMax + 0.5f) problems.Add($"{where}: {e.name}/{e.GetClasses().FirstOrDefault()} {r} leaves the screen {sr}");
                        var panel = e.parent == null ? null : Ancestor(e);
                        if (panel != null && (r.xMin < panel.worldBound.xMin - 0.5f || r.xMax > panel.worldBound.xMax + 0.5f))
                            problems.Add($"{where}: {e.name}/{e.GetClasses().FirstOrDefault()} {r} leaves {panel.name}/{panel.GetClasses().FirstOrDefault()} {panel.worldBound}");
                        if (e is Label l && !string.IsNullOrEmpty(l.text) && l.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
                        {
                            var natural = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                            if (natural.x > l.contentRect.width + 1f) problems.Add($"{where}: '{l.text}' ({natural.x:0} px) is cut to {l.contentRect.width:0} px");
                        }
                    }
                    if (s == "cabinet")
                    {
                        var hemi = root.Q("cabinet-hemicycle");
                        if (hemi.layout.width < HemicycleBox.x - 1.5f || hemi.layout.height < HemicycleBox.y - 1.5f)   // pixel snapping at a fractional scale moves it by under 1.5 px
                            problems.Add($"{where}: the half circle is {hemi.layout.size}, not the {HemicycleBox} the line test assumes");
                        var cards = root.Q<ScrollView>("cabinet-seats").Query<Button>(className: "seat").ToList();
                        if (cards.Count < 5) problems.Add($"{where}: {cards.Count} cards");
                        if (!(cards[0].worldBound.yMin < root.Q<ScrollView>("cabinet-seats").contentViewport.worldBound.yMax - 100)) problems.Add($"{where}: no card row in view");
                    }
                }
            }
            finally
            {
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems.Take(40)));
    }

    /// <summary>Review I2: a hidden screen does no work on a change (an order, End Turn) and catches up when it is opened;
    /// an open screen updates its cards in place, so they are the same elements and a focused card or Appoint button keeps
    /// its focus; an unchanged house is not redrawn.</summary>
    [UnityTest]
    public IEnumerator AHiddenScreenDoesNoWorkAndAnOpenOneKeepsFocus()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        var defence = new DefencePresenter(ui.Root, bridge, Text);
        var nav = new ScreenNav(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        try
        {
            int cab = cabinet.Renders, def = defence.Renders;
            bridge.Queue(new SetMobilization(0.9));
            bridge.EndTurn();
            Assert.AreEqual(cab, cabinet.Renders, "hidden Cabinet: no render on an order or End Turn");
            Assert.AreEqual(def, defence.Renders, "hidden Defence: no render");
            nav.Open("defence");
            Assert.AreEqual(def + 1, defence.Renders, "opening catches up, once");
            Assert.AreEqual(PlayerText.Percent(bridge.State.Mobilization), ui.Q<Label>("mob-percent").text, "with today's figures");
            nav.Open("cabinet");
            Assert.AreEqual(cab + 1, cabinet.Renders);
            nav.Open("defence"); nav.Open("cabinet");
            Assert.AreEqual(cab + 1, cabinet.Renders, "nothing changed: no second render on reopening");

            yield return null; yield return null;
            var house = ui.Q<Hemicycle>("cabinet-hemicycle");
            var card = ui.Q<Button>("seat-finance_minister");
            card.Focus();
            for (int i = 0; i < 10 && card.focusController.focusedElement != card; i++) yield return null;
            var changes = house.Changes;
            Assert.IsNull(bridge.Queue(new SetMobilization(0.85)));                      // a change while Cabinet is open
            Assert.AreSame(card, ui.Q<Button>("seat-finance_minister"), "the card is updated in place");
            Assert.AreSame(card, card.focusController.focusedElement, "and keeps the keyboard focus");
            Assert.AreEqual(changes, house.Changes, "an unchanged house is not redrawn");
            bridge.Withdraw(bridge.QueuedLike(new SetMobilization(0))!);                // frees the attention for the appointment
            Assert.AreSame(card, card.focusController.focusedElement, "a withdrawal keeps the focus too");
            cabinet.Select("finance_minister");
            var appoint = ui.Q<ScrollView>("cabinet-candidates").Query<Button>().ToList().First(b => b.enabledSelf);
            appoint.Focus();
            for (int i = 0; i < 10 && appoint.focusController.focusedElement != appoint; i++) yield return null;
            UiTestKit.Press(appoint);                                                    // Enter on Appoint queues the order
            Assert.AreEqual(1, bridge.Queued.OfType<Appoint>().Count());
            Assert.AreSame(appoint, ui.Q<Button>(appoint.name), "the candidate row is updated in place");
            Assert.AreSame(appoint, appoint.focusController.focusedElement, "Appoint keeps the focus");
        }
        finally { nav.Dispose(); cabinet.Dispose(); defence.Dispose(); }
    }

    /// <summary>The review's idle probe as a test: with Cabinet or Defence open (and with neither), 180 idle frames cause no
    /// recurring relayout or repaint anywhere on either screen. Once the editor has delivered its hierarchy-changed
    /// notification for the new UIDocument (which makes it re-add its root, one full relayout) and 5 frames have passed, the
    /// next 60 frames must have none. Frames are Time.frameCount, not test-runner steps (#242).</summary>
    [UnityTest]
    public IEnumerator AnOpenScreenIsStillWhenIdle()
    {
        foreach (var open in new string?[] { null, "cabinet", "defence" })
        {
            var rt = new RenderTexture(1920, 1080, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            // The editor tells UIDocument about the new GameObject some time later, in a batch-mode editor from the next
            // step to tens of frames on. UIDocument then removes and re-adds its root, which resets every layout to zero and
            // lays out the whole tree again (#242). Subscribe first, so that the warm-up below can wait for that.
            int hierarchyFrame = -1;
            Action onHierarchy = () => hierarchyFrame = Time.frameCount;
            UnityEditor.EditorApplication.hierarchyChanged += onHierarchy;
            var go = new GameObject("ScreensIdle");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var owned = new List<IDisposable>();
            try
            {
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
                owned.Add(new CabinetPresenter(root, bridge, Text, null));
                owned.Add(new DefencePresenter(root, bridge, Text));
                var nav = new ScreenNav(root); owned.Add(nav);
                nav.Open(open);
                // `yield return null` is a test-runner step, not a frame. A batch-mode editor runs the player loop (and with it the
                // panel's layout and paint) only now and then while idle, so a count of steps put the first layout of a
                // just-opened screen anywhere in the window. Each step queues a player-loop update, as a player runs one every
                // frame, and frames are counted with Time.frameCount.
                int fcStart = Time.frameCount; var clock = System.Diagnostics.Stopwatch.StartNew();
                var busy = new SortedDictionary<int, List<string>>();
                void Note(string what) { var f = Time.frameCount - fcStart; if (!busy.TryGetValue(f, out var l)) busy[f] = l = new List<string>(); l.Add(what); }
                foreach (var e in new[] { root.Q("screen-cabinet"), root.Q("screen-defence") }.SelectMany(s => s.Query<VisualElement>().ToList()))
                {
                    var name = e.name;
                    e.RegisterCallback<GeometryChangedEvent>(_ => Note("layout " + name));
                    e.generateVisualContent += _ => Note("paint " + name);
                }
                // Warm-up: the screen is laid out and painted when it opens, and once more when the editor's hierarchy
                // notification makes UIDocument re-add its root. That work is a one-off. Wait for the notification and then for
                // 5 more frames; the wait is bounded, so a missing notification fails with a message and does not hang.
                while (hierarchyFrame < 0 || Time.frameCount - Math.Max(hierarchyFrame, fcStart) < 5)
                {
                    Assert.Less(clock.ElapsedMilliseconds, 60000L, "the editor did not deliver the hierarchy change and 5 frames in 60 s");
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
                string Report() => string.Join("; ", busy.Select(kv => $"frame {kv.Key}: {kv.Value.Count} ({string.Join(", ", kv.Value.Distinct().Take(6))})"));
                var warmUp = busy.Count;
                busy.Clear(); int observeFrom = Time.frameCount;
                while (Time.frameCount - observeFrom < 60)
                {
                    Assert.Less(clock.ElapsedMilliseconds, 60000L, "the editor did not run 60 frames in 60 s");
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
                Assert.AreEqual(0, busy.Count, $"open={open ?? "none"}: relayout or repaint in {busy.Count} of 60 idle frames after {warmUp} warm-up frames with work: {Report()}");
            }
            finally
            {
                UnityEditor.EditorApplication.hierarchyChanged -= onHierarchy;
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }

    /// <summary>Escape (the UI Cancel event) closes the open screen, from a focused card or from the panel; with nothing open
    /// it is left alone. GameRoot also calls Close on the Escape key when nothing has the focus.</summary>
    [UnityTest]
    public IEnumerator EscapeClosesTheOpenScreen()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        var nav = new ScreenNav(ui.Root);
        var guard = new NavigationGuard(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        try
        {
            nav.Open("cabinet");
            yield return null; yield return null;
            var card = ui.Q<Button>("seat-finance_minister");
            card.Focus();
            for (int i = 0; i < 10 && card.focusController.focusedElement != card; i++) yield return null;
            using (var e = NavigationCancelEvent.GetPooled()) { e.target = card; card.SendEvent(e); }
            Assert.IsNull(nav.Current, "Escape on a focused card closes the Cabinet");
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-cabinet").style.display.value);
            Assert.IsFalse(ui.Q<Button>("nav-cabinet").ClassListContains(ScreenNav.OpenClass), "its button is unlit");
            Assert.IsFalse(ui.Q<VisualElement>("root").ClassListContains(ScreenNav.WideOpenClass), "the info panel comes back");
            nav.Open("defence");
            using (var e = NavigationCancelEvent.GetPooled()) { e.target = ui.Root; ui.Root.SendEvent(e); }
            Assert.IsNull(nav.Current, "Escape with nothing focused closes Defence");
            nav.Open("budget");
            Assert.IsTrue(nav.Close(), "Close (GameRoot's Escape key) closes Budget");
            Assert.IsFalse(nav.Close(), "and is a no-op with nothing open");
            // The camera's arrow keys still never move UI focus (NavigationGuard), with a screen open.
            nav.Open("cabinet");
            card = ui.Q<Button>("seat-finance_minister");
            card.Focus();
            for (int i = 0; i < 10 && card.focusController.focusedElement != card; i++) yield return null;
            using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right)) { e.target = card; card.SendEvent(e); }
            yield return null;
            Assert.AreSame(card, card.focusController.focusedElement, "an arrow key does not move the focus");
            Assert.AreEqual("cabinet", nav.Current, "and does not close the screen");
        }
        finally { guard.Dispose(); nav.Dispose(); cabinet.Dispose(); }
    }

    /// <summary>#210: at a phone-shaped 390x844 and at 960x600 every screen is usable: at least 500 logical px wide and
    /// 300 tall, under the top bar. In the two-row HUD (390x844) a screen takes the full width, the minimap column and the
    /// info panel under it are hidden and ignore map clicks, and the toast sits below the screen.</summary>
    [UnityTest]
    public IEnumerator EveryScreenIsUsableInANarrowWindow()
    {
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (390, 844), (960, 600) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("NarrowTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var owned = new List<IDisposable>();
            try
            {
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
                owned.Add(new HudPresenter(root, bridge, Text));
                owned.Add(new CabinetPresenter(root, bridge, Text, null));
                owned.Add(new DefencePresenter(root, bridge, Text));
                owned.Add(new BudgetPresenter(root, bridge, Text));
                owned.Add(new WhyPresenter(root, bridge, Text));
                var info = new InfoPanelPresenter(root, bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)); owned.Add(info);
                var nav = new ScreenNav(root); owned.Add(nav);
                var toast = root.Q<Label>("toast");
                toast.text = "Not done: Not enough attention (need 4.0, have 2.0)";
                toast.style.display = DisplayStyle.Flex;
                for (int i = 0; i < 6; i++) yield return null;
                bool twoRow = root.Q("root").ClassListContains(HudPresenter.TwoRowClass);
                if ((w, h) == (390, 844) && !twoRow) problems.Add("390x844 is not the two-row HUD");
                var bar = root.Q("top-bar").worldBound;
                foreach (var s in ScreenNav.Screens)
                {
                    nav.Open(s);
                    for (int i = 0; i < 4; i++) yield return null;
                    var where = $"{w}x{h} {s}";
                    var r = root.Q("screen-" + s).worldBound;
                    if (!(r.width >= 500f && r.height >= 300f)) problems.Add($"{where}: the screen is only {r.width:0} x {r.height:0}");
                    if (r.yMin < bar.yMax - 0.5f) problems.Add($"{where}: starts over the top bar");
                    var t = toast.worldBound;
                    if (t.Overlaps(r)) problems.Add($"{where}: the toast {t} covers the screen {r}");
                    if (twoRow)
                    {
                        var panel = root.panel.visualTree.worldBound;
                        // Full width right of the left icon column (HUD round 2, #398), which stays in view to switch screens.
                        var column = root.Q("left-column").worldBound;
                        if (r.xMin < column.xMax - 0.5f) problems.Add($"{where}: covers the left icon column {column}");
                        if (r.width < panel.width - 40f - 68f) problems.Add($"{where}: not full width ({r.width:0} of {panel.width:0})");
                        foreach (var hidden in new[] { "right-column", "info-panel" })
                            if (root.Q(hidden).resolvedStyle.display != DisplayStyle.None) problems.Add($"{where}: {hidden} shows under the screen");
                        var before = info.Selected;
                        var tlv = C.SettlementHexes.Single(x => x.Id == "tel_aviv");
                        info.Show(tlv.Q, tlv.R);
                        if (info.Selected != before) problems.Add($"{where}: a map click changed the hidden info panel");
                    }
                    else if (r.Overlaps(root.Q("right-column").worldBound)) problems.Add($"{where}: overlaps the minimap column");
                }
                nav.Open(null);
                for (int i = 0; i < 3; i++) yield return null;
                foreach (var shown in new[] { "right-column", "info-panel" })
                    if (root.Q(shown).resolvedStyle.display == DisplayStyle.None) problems.Add($"{w}x{h}: {shown} stays hidden after closing");
            }
            finally
            {
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The nearest box an element belongs in: a card, a Defence panel or the house.</summary>
    static VisualElement? Ancestor(VisualElement e)
    {
        for (var p = e.parent; p != null; p = p.parent)
            if (p.ClassListContains("seat") || p.ClassListContains("leader") || p.ClassListContains("defence-panel") || p.ClassListContains("cabinet-house") || p.ClassListContains("cabinet-side"))
                return p;
        return null;
    }
}
