using System.Text.RegularExpressions;

namespace Statecraft.Sim.Tests;

/// <summary>#1682's 68 source producers, including rare branches. The pre-key constructor expressions are pinned to
/// c7851de so Message, Target and Delta cannot drift while metadata is added. This is not runtime branch coverage.</summary>
public class RemainingCauseKeysTests
{
    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "sim", "Statecraft.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repository root not found");
    }

    public static IEnumerable<object[]> Producers()
    {
        yield return new object[] { "Cities.cs", "WorkersAssigned", "new Cause(\"city\", city, count - x.Employed, FormattableString.Invariant($\"{PlayerText.Settlement(c, city)}: {count} groups in work\")" };
        yield return new object[] { "Cities.cs", "WagesPaid", "new Cause(\"city\", x.Id, paid, $\"{PlayerText.Settlement(c, x.Id)}: jobs paid {PlayerText.MoneyChange(c, \"treasury\", paid)}\"" };
        yield return new object[] { "Cities.cs", "ImmigrantsHoused", "new Cause(\"city\", x.Id, absorbed, FormattableString.Invariant($\"{PlayerText.Settlement(c, x.Id)}: {absorbed} groups of immigrants housed\")" };
        yield return new object[] { "Cities.cs", "WorkersAssigned", "new Cause(\"city\", x.Id, work - housed.Employed, FormattableString.Invariant($\"{PlayerText.Settlement(c, x.Id)}: {work} groups in work\")" };
        yield return new object[] { "Cities.cs", "HousingShortage", "new Cause(\"city\", x.Id, over, FormattableString.Invariant($\"{PlayerText.Settlement(c, x.Id)}: {over} groups more than its homes hold\")" };
        yield return new object[] { "Cities.cs", "ImmigrantsWaiting", "new Cause(\"city\", x.Id, added, FormattableString.Invariant($\"{PlayerText.Settlement(c, x.Id)}: {added} groups of immigrants arrived and wait for homes\")" };
        yield return new object[] { "Military.cs", "RecruitsVeteran", "new Cause(\"city\", \"units\", u.Strength, \"Recruits became veteran soldiers\"" };
        yield return new object[] { "Corridors.cs", "CorridorStarted", "new Cause(\"map\", def.Id, 1, $\"{name}: a group of halutzim began building, cost {PlayerText.Money(c, \"treasury\", cost)}\"" };
        yield return new object[] { "Corridors.cs", "CorridorCancelled", "new Cause(\"map\", work.Id, -1, $\"{SegmentName(def, index)}: the halutzim stopped building; the Money spent is not returned\"" };
        yield return new object[] { "Corridors.cs", "CorridorCompleted", "new Cause(\"map\", w.Id, 1, $\"{SegmentName(def, i)} built\"" };
        yield return new object[] { "Corridors.cs", "PioneersVeteran", "new Cause(\"map\", w.Id, 1, \"A group of halutzim became veteran pioneers\"" };
        yield return new object[] { "MapActions.cs", "MapBorrowed", "new Cause(\"economy\", \"debt\", -s.Treasury, $\"borrowed {PlayerText.Money(c, \"debt\", -s.Treasury)} to build {PlayerText.MapAction(c, action)}\"" };
        yield return new object[] { "MapActions.cs", "MapBuilt", "new Cause(\"map\", action, 1, FormattableString.Invariant(\n                $\"Built {PlayerText.MapAction(c, action)}: cost {PlayerText.Money(c, \"treasury\", rule.Cost)}, {PlayerText.Change(c, \"housing\", housing)}, {PlayerText.Change(c, \"morale\", morale)}\")" };
        yield return new object[] { "Projects.cs", "LandWorkStarted", "new Cause(\"map\", site, 1, $\"{place.Name}: a group of halutzim began {(plow ? \"plowing\" : \"draining the swamp\")}, cost {PlayerText.Money(c, \"treasury\", cost)}\"" };
        yield return new object[] { "Projects.cs", "SiteStarted", "new Cause(\"map\", site, 1, $\"{place.Name}: a group of halutzim began building {(camp ? \"a training camp\" : \"a yishuv\")}, cost {PlayerText.Money(c, \"treasury\", cost)}\"" };
        yield return new object[] { "Projects.cs", "SiteCancelled", "new Cause(\"map\", work.Site, -1, $\"{PlayerText.Site(c, work.Site)}: the halutzim stopped building; the Money spent is not returned\"" };
        yield return new object[] { "Projects.cs", "SiteCompleted", "new Cause(\"map\", w.Site, 1, $\"{place.Name} {(IsCamp(place) ? \"built\" : place.Kinds.Contains(FieldKind) ? \"plowed\" : place.Kinds.Contains(SwampKind) ? \"drained\" : \"founded\")}\"" };
        yield return new object[] { "Projects.cs", "PioneersVeteran", "new Cause(\"map\", w.Site, 1, \"A group of halutzim became veteran pioneers\"" };
        yield return new object[] { "Projects.cs", "ProjectStarted", "new Cause(\"city\", city, 1, $\"{name}: {k.Name} began, cost {PlayerText.Money(c, \"treasury\", k.Cost)}\"" };
        yield return new object[] { "Projects.cs", "ProjectCancelled", "new Cause(\"city\", p.City, -1, $\"{PlayerText.Settlement(c, p.City)}: {k.Name} was cancelled; the Money spent is not returned\"" };
        yield return new object[] { "Projects.cs", "ProjectCompleted", "new Cause(\"city\", p.City, 1, $\"{PlayerText.Settlement(c, p.City)}: {k.Name} was finished\"" };
        yield return new object[] { "Pools.cs", "ArmsBought", "new Cause(\"economy\", \"arms\", count, FormattableString.Invariant($\"Bought {count} Arms, cost {PlayerText.Money(c, \"treasury\", cost)}\")" };
        yield return new object[] { "Pools.cs", "FoodHarvest", "new Cause(\"economy\", \"food\", yield, FormattableString.Invariant($\"{f.Name}: the plowed land yielded {yield} units of Food\")" };
        yield return new object[] { "Pools.cs", "FoodShortage", "new Cause(\"economy\", \"food\", -s.FoodUnmet, FormattableString.Invariant($\"Food ran out: {s.FoodUnmet} of {need} units of the month's need went unmet\")" };
        yield return new object[] { "Pools.cs", "FoodWithoutImports", "new Cause(\"economy\", \"food\", -eaten, FormattableString.Invariant($\"No Food imported (foreign currency crisis): the stock fell to {s.Food} units\")" };
        yield return new object[] { "Prisoners.cs", "PrisonerFood", "new Cause(\"economy\", \"food\", -eaten, FormattableString.Invariant($\"The prisoners held ate {eaten} units of Food\")" };
        yield return new object[] { "Prisoners.cs", "PrisonerFoodShortage", "new Cause(\"economy\", \"morale\", change, FormattableString.Invariant($\"The prisoners held went short of {unfed} units of Food: {PlayerText.Change(c, \"morale\", change)}\")" };
        yield return new object[] { "Qualification.cs", "CraftsmenReturned", "new Cause(\"city\", city, count, $\"{name}: {Groups(count)} of craftsmen returned to civilian life and keep their training\"" };
        yield return new object[] { "Qualification.cs", "CraftsmenReassigned", "new Cause(\"city\", city, -count, $\"{name}: {Groups(count)} of qualified craftsmen are craftsmen again\"" };
        yield return new object[] { "Qualification.cs", "PioneerReturned", "new Cause(\"map\", unitId.ToString(CultureInfo.InvariantCulture), 1,\n                $\"Unit {unitId} returned to civilian life in {PlayerText.Settlement(c, city)} and keeps its training\"" };
        yield return new object[] { "Qualification.cs", "PioneersReassigned", "new Cause(\"city\", city, -count, $\"{name}: {Groups(count)} of qualified {(veteran ? \"veteran pioneers\" : \"halutzim\")} are units again\"" };
        yield return new object[] { "Training.cs", "TrainingStarted", "new Cause(\"city\", city, -count, $\"{name}: {Groups(count)} began {Course(type)}{aim}, cost {PlayerText.Money(c, \"treasury\", cost)}\"" };
        yield return new object[] { "Training.cs", "TrainingArms", "new Cause(\"city\", \"arms\", -arms, FormattableString.Invariant($\"{name}: {Course(type)} took {arms} Arms\")" };
        yield return new object[] { "Training.cs", "TrainingCancelled", "new Cause(\"city\", k.City, k.Count,\n                $\"{PlayerText.Settlement(c, k.City)}: {Groups(k.Count)} left {Course(type)}; the Money spent is not returned\"" };
        yield return new object[] { "Training.cs", "TrainingArmsReturned", "new Cause(\"city\", \"arms\", back, FormattableString.Invariant($\"{PlayerText.Settlement(c, k.City)}: {Course(type)} returned {back} Arms\")" };
        yield return new object[] { "Training.cs", "ReplacementsStopped", "new Cause(\"city\", k.City, 0, FormattableString.Invariant($\"{PlayerText.Settlement(c, k.City)}: unit {aim} is no longer in {PlayerText.Settlement(c, k.City)}, so its replacements stop training\")" };
        yield return new object[] { "Training.cs", "ReplacementsJoined", "new Cause(\"city\", k.City, k.Count, FormattableString.Invariant($\"{name}: {Groups(k.Count)} finished {Course(type)} and joined unit {target.Id}\")" };
        yield return new object[] { "Training.cs", "MilitaryTrained", "new Cause(\"city\", k.City, k.Count, $\"{name}: {Groups(k.Count)} finished {Course(type)} and are {Trained(type)}\"" };
        yield return new object[] { "Training.cs", "PioneersTrained", "new Cause(\"city\", k.City, k.Count, $\"{name}: {Groups(k.Count)} finished {Course(type)} and are halutzim\"" };
        yield return new object[] { "Training.cs", "CraftsmenTrained", "new Cause(\"city\", k.City, k.Count, $\"{name}: {Groups(k.Count)} finished {Course(type)} and are craftsmen\"" };
        yield return new object[] { "Cabinet.cs", "Appointed", "new Cause(\"cabinet\", officeId, 0, displaced == null\n                ? $\"{person.Name} appointed {office.Title}\"\n                : $\"{person.Name} appointed {office.Title}, replacing {displaced.Name}\"" };
        yield return new object[] { "Cabinet.cs", "Reshuffle", "new Cause(\"politics\", \"coalition_stability\", s.CoalitionStability - before, (\n                $\"coalition stability {PlayerText.PercentChange(s.CoalitionStability - before)} ({who} {(sameParty ? \"resents the reshuffle\" : \"lost the \" + office.Title)})\")" };
        yield return new object[] { "Cabinet.cs", "AppointmentsEnded", "new Cause(\"cabinet\", \"appointments\", -s.Appointments.Count, \"A new government took office with its own ministers; your appointments ended\"" };
        yield return new object[] { "PoliticsSystem.cs", "ApprovalStability", "new Cause(\"politics\", \"coalition_stability\", s.CoalitionStability - old, $\"coalition stability {PlayerText.PercentChange(s.CoalitionStability - old)} (public approval)\"" };
        yield return new object[] { "PoliticsSystem.cs", "CoalitionCollapsed", "new Cause(\"politics\", \"game_over\", 0, \"The coalition collapsed. Your premiership is over.\"" };
        yield return new object[] { "PoliticsSystem.cs", "LegislatureConvened", "new Cause(\"politics\", l1.Id, 0, FormattableString.Invariant($\"{l1.Body} convened ({l1.Size} seats{elected}); coalition holds {seats}/{size}\")" };
        yield return new object[] { "PoliticsSystem.cs", "GovernmentTookOffice", "new Cause(\"politics\", g1!.Id, 0, $\"{g1.Name} took office ({string.Join(\", \", PartyNames(c, g1.CoalitionOn(g1.From)))})\"" };
        yield return new object[] { "PoliticsSystem.cs", "CoalitionChanged", "new Cause(\"politics\", g.Id, 0, FormattableString.Invariant($\"{string.Join(\"; \", what)} (coalition holds {seats}/{size})\")" };
        yield return new object[] { "Simulation.cs", "EraEnded", "new Cause(\"era\", s.GameOver, 0, PlayerText.GameOver(s.GameOver)" };
        yield return new object[] { "Simulation.cs", "AdvisoryPolicyRejected", "new Cause(\"politics\", p.Policy, 0, $\"{legislature.Body} voted against {PlayerText.Target(c, p.Policy)} at {PlayerText.Percent(p.Level)}, but its vote does not bind\"" };
        yield return new object[] { "Simulation.cs", "PolicyRejected", "new Cause(\"politics\", p.Policy, 0, $\"{legislature.Body} voted down {PlayerText.Target(c, p.Policy)} at {PlayerText.Percent(p.Level)}\"" };
        yield return new object[] { "Simulation.cs", "PolicySet", "new Cause(\"policy\", p.Policy, p.Level, $\"{PlayerText.Target(c, p.Policy)} set to {PlayerText.Percent(p.Level)}\"" };
        yield return new object[] { "Simulation.cs", "AdvisoryBudgetRejected", "new Cause(\"politics\", \"budget\", 0, $\"{Offices.Legislature(c, s.Date)!.Body} voted against the budget, but its vote does not bind\"" };
        yield return new object[] { "Simulation.cs", "BudgetRejected", "new Cause(\"politics\", \"budget\", 0, $\"{Offices.Legislature(c, s.Date)!.Body} voted the budget down\"" };
        yield return new object[] { "TurnResolver.cs", "EraEnded", "new Cause(\"era\", s.GameOver, s.ExhaustedTurns, PlayerText.GameOver(s.GameOver)" };
        yield return new object[] { "TurnResolver.cs", "EraEnded", "new Cause(\"era\", s.GameOver, u.Id, PlayerText.GameOver(s.GameOver)" };
        yield return new object[] { "SettlementControl.cs", "PlaceCaptured", "new Cause(\"map\", t.Id, 0, $\"{c.SettlementNames[t.Id]} is captured: Israeli units hold it and no enemy unit is there. \"\n                    + (fled ? \"Its residents fled. \" : \"\") + \"It awaits a decision\"" };
        yield return new object[] { "SettlementControl.cs", "EnemyRegained", "new Cause(\"map\", t.Id, 0, $\"{c.SettlementNames[t.Id]} is under enemy control again: enemy units hold it and no Israeli unit is there\"\n                    + (record.Fled ? \". \" + FledLine : \"\")" };
        yield return new object[] { "SettlementControl.cs", "PlaceAnnexed", "new Cause(\"map\", id, 0, x.Fled\n                ? $\"{name} is annexed. Its residents fled when it was captured, so no people, homes or jobs are added\"\n                : $\"{name} is annexed. Its residents join the population\"" };
        yield return new object[] { "SettlementControl.cs", "PlaceWithdrawn", "new Cause(\"map\", id, 0, moved.Count > 0\n                ? $\"{name}: Israeli units withdrew and it is under enemy control again. {residents}\"\n                : $\"{name} is under enemy control again. {residents}\"" };
        yield return new object[] { "SettlementControl.cs", "PlaceDeported", "new Cause(\"map\", id, 0, $\"{name}: Deport. Its residents are displaced and the place is depopulated\"" };
        yield return new object[] { "SettlementControl.cs", "DeportEffect", "new Cause(\"map\", indicator, change, $\"{PlayerText.Change(c, indicator, change)} (Deport of {name})\"" };
        yield return new object[] { "SettlementControl.cs", "DeportCost", "new Cause(\"economy\", \"treasury\", -r.DeportMoneyCost!.Value, $\"{PlayerText.Change(c, \"treasury\", -r.DeportMoneyCost.Value)} (Deport of {name})\"" };
        yield return new object[] { "SettlementControl.cs", "OccupationStarted", "new Cause(\"map\", id, turns, FormattableString.Invariant($\"{name} is occupied for {turns} turns: an Israeli unit stays there. \") + Residents(x)" };
        yield return new object[] { "SettlementControl.cs", "PendingBecameRaid", "new Cause(\"map\", id, 0, $\"{c.SettlementNames[id]}: no decision was made before the end, so it counts as a raid\"" };
        yield return new object[] { "SettlementControl.cs", "OccupationLostUnit", "new Cause(\"map\", id, 0, $\"{name}: no Israeli unit holds it any more, so the occupation ended as a raid. It is under enemy control again. {Residents(x)}\"" };
        yield return new object[] { "SettlementControl.cs", "OccupationCountdown", "new Cause(\"map\", id, left, left == 0\n                    ? $\"{name}: the occupation period has ended. It awaits a decision\"\n                    : FormattableString.Invariant($\"{name}: {left} turns of occupation left\")" };
        yield return new object[] { "SettlementControl.cs", "DepopulationSupport", "new Cause(\"map\", SupportIndicator, change, $\"{PlayerText.Change(c, SupportIndicator, change)} (the places depopulated by Deport)\"" };
    }

    [Theory]
    [MemberData(nameof(Producers))]
    public void EveryListedProducerKeepsItsEnglishExpressionsAndAddsARegisteredKey(string file, string key, string original)
    {
        var source = File.ReadAllText(Path.Combine(Root(), "sim", "Statecraft.Sim", file));
        Assert.Contains(original + ", key: CauseKeys." + key, source);
        var field = typeof(CauseKeys).GetField(key);
        Assert.NotNull(field);
        Assert.Contains((string)field!.GetRawConstantValue()!, CauseKeys.All);
    }

    [Fact]
    public void InventoryContainsExactly68ProducerSites()
    {
        Assert.Equal(68, Producers().Count());
    }

    [Fact]
    public void EverySourceProducerIsKeyed()
    {
        var sites = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root(), "sim", "Statecraft.Sim"), "*.cs"))
        {
            var source = File.ReadAllText(file);
            foreach (var keyed in SourceCauseKeys(source))
            {
                Assert.True(keyed, file + ": Cause needs a top-level key argument");
                sites++;
            }
        }
        Assert.Equal(104, sites);
    }

    [Fact]
    public void InventoryIgnoresLiteralCommentAndNestedKeyText()
    {
        // Red-first regression: the old scanner never balances the first literal's '('.
        var source = "new Cause(\"map\", \"town\", 0, \"(\" /* key: fake */);\n"
            + "new Cause(\"map\", \"town\", 0, $\"({Name(\"(\")}\", key: CauseKeys.PlaceCaptured);\n"
            + "new Cause(\"map\", \"town\", 0, @\"(\"\" key: fake\");\n"
            + "new Cause(\"map\", \"town\", 0, $@\"({Name(@\"(\")}\");\n"
            + "new Cause(\"map\", \"town\", 0, @$\"({Name(\"(\")}\");\n"
            + "new Cause(\"map\", \"town\", 0, Label('(', key: fake)); // key: fake\n";
        Assert.Equal(new[] { false, true, false, false, false, false }, SourceCauseKeys(source).ToArray());
    }

    [Fact]
    public void InventoryIgnoresRawLiteralAndInterpolationKeyText()
    {
        // Source snippets are escaped ordinary strings so the fixture itself needs no raw syntax.
        var rows = new[]
        {
            "new Cause(\"map\", \"town\", 0, \"\"\"a\", key: fake, \"b\"\"\");",
            "new Cause(\"map\", \"town\", 0, $\"\"\"a\", key: fake, \"{Name(\"\"\"nested\"\"\")}\"b\"\"\");",
            "new Cause(\"map\", \"town\", 0, $$\"\"\"a\", key: fake, \"{{Name(\"\"\"nested\"\"\")}}\"b\"\"\");",
            "new Cause(\"map\", \"town\", 0, \"\"\"\"\n    a\"\"\", key: fake, \"\"\"b (\n    \"\"\"\");",
            "new Cause(\"map\", \"town\", 0, $$\"\"\"\n    {literal} a\", key: fake, \"{{new { Text = \"\"\"nested\"\"\" }}} b (\n    \"\"\");",
        };
        foreach (var row in rows)
        {
            Assert.Equal(new[] { false }, SourceCauseKeys(row).ToArray());
            // Masking must stop at the delimiter and preserve a real following key argument.
            var keyed = row.Insert(row.LastIndexOf(')'), ", key: CauseKeys.PlaceCaptured");
            Assert.Equal(new[] { true }, SourceCauseKeys(keyed).ToArray());
        }
    }

    [Theory]
    [InlineData("new Cause(\"m\", \"t\", 0, $\"{1:\\\\'}\", key: K);")]
    [InlineData("new Cause(\"m\", \"t\", 0, $$\"\"\"{{1:\\'}}\"\"\", key: K);")]
    [InlineData("new Cause(\"m\", \"t\", 0, $\"{1:\\\\\\\"}\", key: K);")]
    [InlineData("new Cause(\"m\", \"t\", 0, $$\"\"\"{{1:\\\"}}\"\"\", key: K);")]
    [InlineData("new Cause(\"m\", \"t\", 0, $@\"{1:'\"\"}\", key: K);")]
    [InlineData("new Cause(\"m\", \"t\", 0, $\"{(true ? 1 : 2):\\\\'}\", key: K);")]
    public void InventoryKeepsRealKeysAfterInterpolationFormatClauses(string source)
    {
        Assert.Equal(new[] { true }, SourceCauseKeys(source).ToArray());
        Assert.Equal(new[] { false }, SourceCauseKeys(source.Replace(", key: K", "")).ToArray());
    }

    static IEnumerable<bool> SourceCauseKeys(string source)
    {
        // Keep offsets, but erase whole literals (including interpolation holes) and comments.
        var masked = source.ToCharArray();
        for (var i = 0; i < source.Length;)
        {
            var start = i;
            if (!SkipLiteralOrComment(source, ref i)) { i++; continue; }
            for (var j = start; j < i; j++) masked[j] = ' ';
        }
        var code = new string(masked);
        foreach (Match start in Regex.Matches(code, @"\bnew\s+Cause\s*\("))
        {
            var parens = 1; var brackets = 0; var braces = 0;
            var argument = start.Index + start.Length;
            var keyed = false;
            var end = argument;
            for (; end < code.Length; end++)
            {
                var ch = code[end];
                if (parens == 1 && brackets == 0 && braces == 0 && (ch == ',' || ch == ')'))
                {
                    keyed |= Regex.IsMatch(code.Substring(argument, end - argument), @"^\s*key\s*:(?!:)");
                    argument = end + 1;
                }
                if (ch == '(') parens++;
                if (ch == ')') parens--;
                if (ch == '[') brackets++;
                if (ch == ']') brackets--;
                if (ch == '{') braces++;
                if (ch == '}') braces--;
                if (parens == 0) break;
            }
            Assert.Equal(0, parens);
            yield return keyed;
        }
    }

    static bool SkipLiteralOrComment(string source, ref int i)
    {
        var start = i;
        if (source[i] == '/' && i + 1 < source.Length)
        {
            if (source[i + 1] == '/')
            {
                i += 2;
                while (i < source.Length && source[i] != '\n') i++;
                return true;
            }
            if (source[i + 1] == '*')
            {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? source.Length : end + 2;
                return true;
            }
        }
        // Raw literals use a quote-run delimiter; dollar count sets interpolation brace width.
        var quoteStart = i;
        while (quoteStart < source.Length && source[quoteStart] == '$') quoteStart++;
        var quoteEnd = quoteStart;
        while (quoteEnd < source.Length && source[quoteEnd] == '\"') quoteEnd++;
        var quotes = quoteEnd - quoteStart;
        if (quotes >= 3)
        {
            var dollars = quoteStart - i;
            i = quoteEnd;
            while (i < source.Length)
            {
                var end = i;
                while (end < source.Length && source[end] == '\"') end++;
                if (end - i == quotes) { i = end; return true; }
                if (end > i) { i = end; continue; }
                if (dollars > 0 && source[i] == '{')
                {
                    end = i;
                    while (end < source.Length && source[end] == '{') end++;
                    if (end - i >= dollars)
                    {
                        i = end;
                        SkipInterpolation(source, ref i, dollars, raw: true);
                        continue;
                    }
                    i = end;
                    continue;
                }
                i++;
            }
            return true;
        }
        var verbatim = false; var interpolated = false;
        if (source[i] == '$' || source[i] == '@')
        {
            verbatim = source[i] == '@'; interpolated = source[i] == '$'; i++;
            if (i < source.Length && ((verbatim && source[i] == '$') || (interpolated && source[i] == '@')))
            {
                verbatim = interpolated = true; i++;
            }
        }
        if (i >= source.Length || (source[i] != '"' && (i != start || source[i] != '\'')))
        {
            i = start; return false;
        }
        var quote = source[i++];
        while (i < source.Length)
        {
            var ch = source[i++];
            if (!verbatim && ch == '\\') { if (i < source.Length) i++; continue; }
            if (ch == quote)
            {
                if (verbatim && i < source.Length && source[i] == quote) { i++; continue; }
                return true;
            }
            if (interpolated && ch == '{')
            {
                if (i < source.Length && source[i] == '{') { i++; continue; }
                SkipInterpolation(source, ref i, verbatim: verbatim);
            }
        }
        return true;
    }

    static void SkipInterpolation(string source, ref int i, int braceWidth = 1, bool verbatim = false, bool raw = false)
    {
        var depth = 0; var parens = 0; var brackets = 0;
        while (i < source.Length)
        {
            if (SkipLiteralOrComment(source, ref i)) continue;
            // A format component is literal text, not another C# expression. In particular,
            // an apostrophe there must not start a character literal that swallows the real key.
            if (source[i] == ':' && depth == 0 && parens == 0 && brackets == 0)
            {
                if (i + 1 < source.Length && source[i + 1] == ':') { i += 2; continue; }
                i++;
                SkipFormat(source, ref i, braceWidth, verbatim, raw);
                return;
            }
            if (source[i] == '(') parens++;
            if (source[i] == ')') parens--;
            if (source[i] == '[') brackets++;
            if (source[i] == ']') brackets--;
            if (source[i] == '{') depth++;
            if (source[i] == '}')
            {
                if (depth > 0) { depth--; i++; continue; }
                var end = i;
                while (end < source.Length && source[end] == '}') end++;
                if (end - i >= braceWidth) { i += braceWidth; return; }
                i = end;
                continue;
            }
            i++;
        }
    }

    static void SkipFormat(string source, ref int i, int braceWidth, bool verbatim, bool raw)
    {
        while (i < source.Length)
        {
            if (!raw && !verbatim && source[i] == '\\') { i += Math.Min(2, source.Length - i); continue; }
            if (!raw && verbatim && source[i] == '"' && i + 1 < source.Length && source[i + 1] == '"')
            { i += 2; continue; }
            if (source[i] == '}')
            {
                var end = i;
                while (end < source.Length && source[end] == '}') end++;
                if (end - i >= braceWidth) { i += braceWidth; return; }
                i = end; continue;
            }
            i++;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFullReferenceRunKeysEveryProducer(bool legacy)
    {
        var c = legacy ? RealContentTests.Legacy() : ContentLoader.Load(RealContentTests.ContentRoot());
        var state = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var count = 0;
        for (var turn = 0; turn < 1000 && state.GameOver == null; turn++)
        {
            var result = Simulation.Step(c, state, GoldenRun.Orders(c, state));
            foreach (var line in result.Why)
            {
                Assert.True(CauseKeys.All.Contains(line.Key) || WhyKeys.All.Contains(line.Key), line.ToString());
                count++;
            }
            state = result.State;
        }
        Assert.True(count > 0);
        Assert.NotNull(state.GameOver);
    }

    [Fact]
    public void WorkerAndTrainingProducersCaptureCountsAndKeepEnglish()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6, "[military_camp, hakhshara]"),
        })));
        var state = Simulation.NewGame(c, 1, Mode.Historical);
        state.Attention = 10; // Synthetic budget: both fixture commands must be legal.
        var result = Simulation.Step(c, state, new Command[] { new AssignWorkers("town_a", "job", 4), new Train("town_a", "recruit", 3) });
        Assert.Empty(result.Rejected);
        var workers = Assert.Single(result.Why, x => x.Key == "workers_assigned" && x.Args[0] == "4");
        Assert.Equal("Town A: 4 groups in work", workers.Message);
        var training = Assert.Single(result.Why, x => x.Key == "training_started");
        Assert.Equal(new[] { "3", "basic", "", "1.5" }, training.Args);
        Assert.Equal("Town A: 3 groups began basic training, cost IL 1.5m", training.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptureAndWithdrawalSnapshotResidentsBeforeRelease(bool fled)
    {
        var c = fled ? CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets) : CaptureTests.Load(CaptureTests.AllTargets);
        var state = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
        var result = Simulation.Step(c, state, new Command[] { new ResolveCapture("town_a", SettlementControl.Raid) });
        Assert.Empty(result.Rejected);
        var captured = Assert.Single(result.Why, x => x.Key == "place_captured");
        var withdrawn = Assert.Single(result.Why, x => x.Key == "place_withdrawn");
        Assert.Equal(new[] { fled ? "1" : "0" }, captured.Args);
        Assert.Equal(new[] { "1", fled ? "1" : "0" }, withdrawn.Args);
        Assert.Equal("TOWN A is captured: Israeli units hold it and no enemy unit is there. "
            + (fled ? "Its residents fled. " : "") + "It awaits a decision", captured.Message);
        Assert.Equal("TOWN A: Israeli units withdrew and it is under enemy control again. "
            + (fled ? "Its residents fled when it was captured" : "Its residents are unchanged"), withdrawn.Message);
        // Release leaves no stayed record; the stored line must still carry the prior resident state.
        if (!fled) Assert.False(result.State.Settlements.ContainsKey("town_a"));
        result.State.Settlements.Clear();
        Assert.Equal(fled ? "1" : "0", withdrawn.Args[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OccupationAndAnnexationCaptureResidentsFromTheRealProducers(bool fled)
    {
        var c = fled ? CaptureTests.LoadFled(new[] { "town_c" }, CaptureTests.AllTargets) : CaptureTests.Load(CaptureTests.AllTargets);
        var state = CombatTests.With(c, CaptureTests.Soldier(1, 4, 0));
        var why = new List<Cause>();
        SettlementControl.Capture(c, state, state.Date.AddDays(7), why);
        why.Clear();
        Assert.Null(SettlementControl.Resolve(c, state, "town_c", SettlementControl.Occupy, why));
        var occupied = Assert.Single(why, x => x.Key == "occupation_started");
        Assert.Equal(new[] { fled ? "1" : "0" }, occupied.Args);
        var turns = c.RulesAt(state.Date).Mvp!.OccupationTurns!.Value;
        Assert.Equal(FormattableString.Invariant($"TOWN C is occupied for {turns} turns: an Israeli unit stays there. ")
            + (fled ? "Its residents fled when it was captured" : "Its residents are unchanged"), occupied.Message);
        var record = state.Settlements["town_c"];
        state.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, record.Since, fled: fled);
        why.Clear();
        Assert.Null(SettlementControl.End(c, state, "town_c", SettlementControl.Annex, why));
        var annexed = Assert.Single(why, x => x.Key == "place_annexed");
        Assert.Equal(new[] { fled ? "1" : "0" }, annexed.Args);
        Assert.Equal("TOWN C is annexed. " + (fled
            ? "Its residents fled when it was captured, so no people, homes or jobs are added"
            : "Its residents join the population"), annexed.Message);
        state.Settlements.Clear();
        Assert.Equal(fled ? "1" : "0", occupied.Args[0]);
        Assert.Equal(fled ? "1" : "0", annexed.Args[0]);
    }

    [Theory]
    [InlineData("hash1950")]
    [InlineData("hash1952")]
    [InlineData("mvp-end")]
    public void GddIdentityUsesOnlyTheGoldenManifest(string name)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var state = name == "mvp-end" ? GoldenRun.PlayToEnd(c)
            : GoldenRun.Play(c, name == "hash1950" ? GoldenRun.Until1950 : GoldenRun.Until1952);
        Assert.Equal(GoldenManifest.Expected(name), state.Hash());
    }

}
