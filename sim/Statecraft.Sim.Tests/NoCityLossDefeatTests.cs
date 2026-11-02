using Statecraft.Sim;

/// <summary>#1711 adopted slice only. Occupation is injected into fixture state: this does not authorize enemy capture.</summary>
public class NoCityLossDefeatTests
{
    static Content Load(string ruleset = SaveGame.Gdd03Ruleset)
    {
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Fixture.GddUnitTypes +
                "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
            ["corridors.yaml"] = "corridors: []\n",
        });
        var loop = ruleset == SaveGame.LegacyRuleset ? "legacy" : "gdd";
        files["eras.yaml"] = Fixture.Files["eras.yaml"] + $"    systems: {{ loop: {loop}, ruleset: {ruleset} }}\n";
        files["cities.yaml"] += Fixture.City("town_b", 12, 8, 10, 6);
        files["map/settlement_hexes.yaml"] = Fixture.Files["map/settlement_hexes.yaml"] + "  - { id: town_b, q: 3, r: 1 }\n";
        files["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"] +
            "  - { id: town_b, name: Town B, lon: 34.51, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n";
        files["map/locations_1948.yaml"] = Fixture.Files["map/locations_1948.yaml"] +
            Fixture.Files["map/locations_1948.yaml"].Replace("locations:\n", "").Replace("town_a", "town_b")
                .Replace("Town A", "Town B").Replace("עיר א", "עיר ב").Replace("34.29", "34.51")
                .Replace("33.35", "33.28").Replace("q: 1", "q: 3").Replace("r: 0", "r: 1");
        if (ruleset == SaveGame.Gdd03Ruleset)
            files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return ContentLoader.Load(Fixture.Write(files));
    }

    static void Occupy(GameState s, int q = 1, int r = 0, int id = 1) =>
        s.Units.Add(id, new Unit(id, "enemy", q, r, side: "egypt", strength: 3));

    static void Exhaust(GameState s)
    {
        s.Debt = 10; s.Food = 0; s.FxReserves = -1e9;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
    }

    // R-END-003: every fixture city, multiple cities, ordinary dates and truce dates; no war-loss cause either.
    [Theory]
    [InlineData(1, 0, false, false)]
    [InlineData(3, 1, false, false)]
    [InlineData(1, 0, true, false)]
    [InlineData(1, 0, false, true)]
    public void Gdd03KeepsPlayingWhileEnemyUnitsHoldCities(int q, int r, bool both, bool truce)
    {
        var c = Load();
        var session = GameSession.New(c, 1, Mode.Historical);
        if (truce) session.State.Date = new DateTime(1948, 6, 15);
        Occupy(session.State, q, r);
        if (both) Occupy(session.State, 3, 1, 2);
        session.State.Units.Add(3, new Unit(3, "veteran_soldier", 4, 0, side: "israel", strength: 3));
        var cityIds = session.State.Cities.Keys.ToArray();
        var units = session.State.Units.Keys.ToArray();
        // An already-awarded objective is not revoked by occupation.
        session.State.Objectives["already_awarded"] = session.State.Date;
        session.State.Score = 100;
        for (int i = 0; i < 3; i++)
        {
            var before = session.State;
            var result = session.EndTurn(Array.Empty<Command>());
            Assert.Null(result.State.GameOver);
            Assert.DoesNotContain(result.Why, w => w.System == "era" && w.Target == GameOverCodes.DefeatWar);
            Assert.True(result.State.Date > before.Date);
            Assert.Equal(before.Turn + 1, result.State.Turn);
            Assert.Equal(units, result.State.Units.Keys);
            Assert.Equal(cityIds, result.State.Cities.Keys);
            Assert.Equal(before.Objectives["already_awarded"], result.State.Objectives["already_awarded"]);
            Assert.Equal(100, result.State.Score);
            Assert.Equal(q, result.State.Units[1].Q);
            Assert.Equal(r, result.State.Units[1].R);
            // An instant order is still accepted after the occupied-city turn.
            Assert.Null(session.Commit(new UnitOrder(3, i % 2 == 0 ? "hold" : "fortify")));
        }
    }

    // R-END-001, R-END-002, R-END-003: occupation neither suppresses nor accelerates economic defeat.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Gdd03StillEndsAtTheEconomicCount(bool occupied)
    {
        var c = Load(); var s = Simulation.NewGame(c, 1, Mode.Historical);
        if (occupied) Occupy(s);
        Exhaust(s);
        for (int i = 1; i <= 3; i++)
        {
            var result = Simulation.Step(c, s, Array.Empty<Command>()); s = result.State;
            Assert.Equal(i, s.ExhaustedTurns);
            Assert.Equal(i == 3 ? GameOverCodes.DefeatEconomy : null, s.GameOver);
            Assert.DoesNotContain(result.Why, w => w.Target == GameOverCodes.DefeatWar);
        }
    }

    // R-END-001: either resource alone is insufficient, even with an occupied city.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Gdd03OneExhaustedResourceDoesNotEndTheGame(bool debtOnly)
    {
        var c = Load(); var s = Simulation.NewGame(c, 1, Mode.Historical); Occupy(s);
        if (debtOnly) s.Debt = 1000;
        else { Exhaust(s); s.Debt = 0; s.Treasury = 1e9; }
        for (int i = 0; i < 4; i++) s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Null(s.GameOver); Assert.Equal(0, s.ExhaustedTurns);
    }

    // R-END-002, R-END-003: the retained economic end precedes the dated end; otherwise the dated end still fires.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Gdd03PreservesDatedEndAndEconomicPrecedence(bool occupied, bool economic)
    {
        var c = Load(); var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Date = c.End.AddDays(-1);
        if (occupied) Occupy(s);
        if (economic) { Exhaust(s); s.ExhaustedTurns = 2; }
        var result = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal(c.End, result.State.Date);
        Assert.Equal(economic ? GameOverCodes.DefeatEconomy : GameOverCodes.EraEnded, result.State.GameOver);
        Assert.Single(result.Why, w => w.System == "era" && w.Target == result.State.GameOver);
        Assert.DoesNotContain(result.Why, w => w.Target == GameOverCodes.DefeatWar);
    }

    // R-END-003: the old rules keep city defeat and its priority before the dated end, but after economic defeat.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Gdd01KeepsItsOriginalWarLossPrecedence(bool lastDay, bool economic)
    {
        var c = Load(SaveGame.GddRuleset); var s = Simulation.NewGame(c, 1, Mode.Historical); Occupy(s);
        if (lastDay) s.Date = c.End.AddDays(-1);
        if (economic) { Exhaust(s); s.ExhaustedTurns = 2; }
        var result = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal(economic ? GameOverCodes.DefeatEconomy : GameOverCodes.DefeatWar, result.State.GameOver);
        Assert.Contains(result.Why, w => w.System == "era" && w.Target == result.State.GameOver);
    }

    // R-END-003: legacy never entered the GDD war/economic checks and must still not do so.
    [Fact]
    public void LegacyKeepsItsNonGddEndRules()
    {
        var c = Load(SaveGame.LegacyRuleset); var s = Simulation.NewGame(c, 1, Mode.Historical);
        Occupy(s); Exhaust(s);
        for (int i = 0; i < 4; i++) s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Null(s.GameOver); Assert.Equal(0, s.ExhaustedTurns);
    }

    // R-END-003: don't revive already-ended saves or allow another command to change their state.
    [Theory]
    [InlineData(GameOverCodes.DefeatWar)]
    [InlineData(GameOverCodes.DefeatEconomy)]
    [InlineData(GameOverCodes.EraEnded)]
    public void Gdd03PreservesAnExistingEnd(string code)
    {
        var c = Load(); var s = Simulation.NewGame(c, 1, Mode.Historical); Occupy(s); s.GameOver = code;
        var before = s.Describe();
        var result = Simulation.Step(c, s, new Command[] { new UnitOrder(1, "hold") });
        Assert.Equal(before, result.State.Describe()); Assert.Single(result.Rejected); Assert.Empty(result.Why);
    }

    static bool Paid(StepResult result, string city) => result.Why.Any(w => w.Key == CauseKeys.WagesPaid && w.Target == city);

    // R-CITY-010 (D6): under gdd-0.3 a city with an enemy military unit on its hex pays no job income and its projects
    // make no progress; the other city is unchanged. Retaken, both resume from where they stopped, with the same ids.
    [Fact]
    public void Gdd03HeldCityPausesItsJobsAndProjectsUntilRetaken()
    {
        var c = Load(); var s = Simulation.NewGame(c, 1, Mode.Historical); Occupy(s);
        s.CityProjects.Add(10, new CityProject(10, "housing", "town_a", 1, 1, 3));
        s.CityProjects.Add(11, new CityProject(11, "housing", "town_b", 3, 2, 3));
        s.Objectives["already_awarded"] = s.Date;
        var cities = s.Cities.Keys.ToArray();
        var held = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.False(Paid(held, "town_a"));
        Assert.True(Paid(held, "town_b"));
        Assert.Equal(3, held.State.CityProjects[10].TurnsLeft);
        Assert.Equal(0.0, held.State.CityProjects[10].Progress);
        Assert.Equal(2, held.State.CityProjects[11].TurnsLeft);
        Assert.Equal(cities, held.State.Cities.Keys);
        Assert.True(held.State.Objectives.ContainsKey("already_awarded"));

        var s2 = held.State; s2.Units.Remove(1);   // retaken: no enemy unit on the hex at the turn's end
        var retaken = Simulation.Step(c, s2, Array.Empty<Command>());
        Assert.True(Paid(retaken, "town_a"));
        Assert.Equal(2, retaken.State.CityProjects[10].TurnsLeft);
        Assert.Equal(new[] { 10, 11 }, retaken.State.CityProjects.Keys.OrderBy(x => x));
        Assert.Equal(cities, retaken.State.Cities.Keys);
        Assert.True(retaken.State.Objectives.ContainsKey("already_awarded"));
    }

    // R-CITY-010, R-UNIT-001: a halutz holds nothing, and gdd-0.1 keeps paying the turn its R-END-003 defeat fires.
    [Theory]
    [InlineData(SaveGame.Gdd03Ruleset, true)]
    [InlineData(SaveGame.GddRuleset, false)]
    public void OnlyAnEnemyUnitUnderGdd03PausesACity(string ruleset, bool halutz)
    {
        var c = Load(ruleset); var s = Simulation.NewGame(c, 1, Mode.Historical);
        if (halutz) s.Units.Add(1, new Unit(1, "halutz", 1, 0)); else Occupy(s);
        s.CityProjects.Add(10, new CityProject(10, "housing", "town_a", 1, 1, 3));
        var result = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.True(Paid(result, "town_a"));
        Assert.Equal(2, result.State.CityProjects[10].TurnsLeft);
    }
}
