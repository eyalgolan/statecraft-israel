using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using Xunit;

/// <summary>#3142 (owner, 11 Nov 2026: "One card, step per front (Recommended)", "Info only, turn 1 only (Recommended)"):
/// the New game invasion card (unity/Assets/Resources/InvasionCard/invasion-card.yaml) centres the camera on each enemy
/// army in turn. Each step's `focus` must be a units_1948.yaml row the game starts with, so its token is on the map on
/// the card's day, and of the side the step names; together the steps cover every enemy side on the map at the start,
/// once each. Lebanon's army has no token on 15 May (lebanese_army's first anchor is from 6 June), so no step points at
/// it. The card itself is presentation only (InvasionCardContent, in the Unity project); this test holds its rows to the
/// sim's own starting forces (StartingForces, R-UNIT-006).</summary>
public class InvasionCardTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());
    static string CardPath => Path.Combine(Path.GetDirectoryName(RealContentTests.ContentRoot())!,
        "unity", "Assets", "Resources", "InvasionCard", "invasion-card.yaml");

    sealed class Step
    {
        public string? Focus { get; set; }
        public string? Side { get; set; }
        public string Text { get; set; } = "";
        public string? TextHe { get; set; }
    }

    sealed class Card { public List<Step> Steps { get; set; } = new List<Step>(); }

    static List<Step> Steps() => new DeserializerBuilder().IgnoreUnmatchedProperties()
        .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
        .Build().Deserialize<Card>(File.ReadAllText(CardPath)).Steps;

    /// <summary>The enemy forces the game starts with: every starting force that is not the player's side and does not
    /// arrive later (R-ENEMY-004 forbids enemy arrivals anyway).</summary>
    static List<StartingForce> EnemyAtStart() =>
        Real.StartingForces.Where(f => f.Side != Movement.PlayerSide && f.Arrives == null).ToList();

    [Fact]
    public void TheIntroHasNoCameraAndEveryOtherStepFocusesOneArmy()
    {
        var steps = Steps();
        Assert.True(steps.Count >= 2, "an intro and at least one front");
        Assert.Null(steps[0].Focus); Assert.Null(steps[0].Side);
        foreach (var step in steps.Skip(1))
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Focus), "a front step names its row: " + step.Text);
            Assert.False(string.IsNullOrWhiteSpace(step.Side), "a front step names its side: " + step.Text);
        }
        Assert.Equal(steps.Count - 1, steps.Skip(1).Select(s => s.Focus).Distinct().Count());
    }

    [Fact]
    public void EachFocusIsAnEnemyForceOnTheMapAtTheStartOfItsStepsSide()
    {
        var enemy = EnemyAtStart().ToDictionary(f => f.Row, f => f);
        foreach (var step in Steps().Skip(1))
        {
            Assert.True(enemy.ContainsKey(step.Focus!), $"{step.Focus} is not an enemy force on the map on {Real.Rules.Start:yyyy-MM-dd}");
            Assert.Equal(step.Side, enemy[step.Focus!].Side);
        }
    }

    [Fact]
    public void TheStepsCoverEveryEnemySideOnTheMapAtTheStart()
    {
        var onMap = EnemyAtStart().Select(f => f.Side).Distinct().OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        var shown = Steps().Skip(1).Select(s => s.Side!).OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        Assert.Equal(onMap, shown);
        // The five the owner's brief names (Egypt, the Arab Legion, Syria, Iraq, the ALA); a change to the roster fails here first.
        Assert.Equal(new[] { "ala", "egypt", "iraq", "jordan", "syria" }, onMap);
    }

    [Fact]
    public void LebanonHasNoStepItHasNoTokenOnTheFirstDay()
    {
        Assert.DoesNotContain(Real.StartingForces, f => f.Row == "lebanese_army");
        Assert.DoesNotContain(Steps(), s => s.Focus == "lebanese_army" || s.Side == "lebanon");
    }

    [Fact]
    public void EveryStepHasHebrew()
    {
        foreach (var step in Steps())
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Text));
            Assert.False(string.IsNullOrWhiteSpace(step.TextHe), "Hebrew for: " + step.Text);
        }
    }
}
