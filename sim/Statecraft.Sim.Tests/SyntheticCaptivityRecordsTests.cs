using Statecraft.Sim;
using System.Globalization;

// #1710 Slice 1 only. All identifiers and ordering examples below are TEST-ONLY SYNTHETIC.
// R-POW-001: records describe supplied combatant episodes; no surrender producer is implemented.
public class SyntheticCaptivityRecordsTests
{
    static Content Content(string ruleset) => new Content {
        Eras = new List<Era> { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = ruleset } } }
    };
    static SyntheticCaptivityEpisode Episode(string episode = "synthetic-episode", string cohort = "synthetic-cohort",
        string surrender = "synthetic-surrender", string side = "synthetic-side", string captor = "synthetic-captor")
        => new SyntheticCaptivityEpisode(episode, cohort, surrender, side, captor);
    static SyntheticCaptivityRecords Records(params SyntheticCaptivityEpisode[] records)
        => SyntheticCaptivityRecords.CreateFixture(Content(SaveGame.Gdd03Ruleset), records);

    [Theory]
    [InlineData("")] [InlineData("legacy")] [InlineData("gdd-0.1")] [InlineData("gdd-0.3-extra")]
    public void CaptivityFixtureRejectsEveryNonExactIdentity(string ruleset)
    {
        Assert.Throws<InvalidOperationException>(() => SyntheticCaptivityRecords.CreateFixture(Content(ruleset), new[] { Episode() }));
        Assert.Empty(Records().Episodes);
        Assert.Single(Records(Episode()).Episodes);
    }

    [Fact]
    public void CaptivityRequiresEveryIdentityAndLosslessProvenance()
    {
        // Includes malformed UTF-16, whitespace and null for every supplied field.
        foreach (var bad in new string?[] { null, "", " ", "\ud800", "\udc00" })
        {
            Assert.ThrowsAny<ArgumentException>(() => Episode(episode: bad!));
            Assert.ThrowsAny<ArgumentException>(() => Episode(cohort: bad!));
            Assert.ThrowsAny<ArgumentException>(() => Episode(surrender: bad!));
            Assert.ThrowsAny<ArgumentException>(() => Episode(side: bad!));
            Assert.ThrowsAny<ArgumentException>(() => Episode(captor: bad!));
        }
        Assert.Throws<ArgumentNullException>(() => SyntheticCaptivityRecords.CreateFixture(null!, Array.Empty<SyntheticCaptivityEpisode>()));
        Assert.Throws<ArgumentNullException>(() => SyntheticCaptivityRecords.CreateFixture(Content(SaveGame.Gdd03Ruleset), null!));
        Assert.Throws<ArgumentException>(() => Records(new SyntheticCaptivityEpisode[] { null! }));
    }

    [Fact]
    public void CaptivityRejectsDuplicateEpisodeAndDuplicateSurrenderCohort()
    {
        Assert.Throws<ArgumentException>(() => Records(Episode(), Episode(cohort: "synthetic-other")));
        Assert.Throws<ArgumentException>(() => Records(Episode(), Episode(episode: "synthetic-other")));
        // Multiple survivors may share one surrender provenance. Distinct observations of the same
        // cohort under another surrender carry no return/award policy and no current-held count.
        Assert.Equal(2, Records(Episode(), Episode("synthetic-other", "synthetic-other")).Episodes.Count);
        Assert.Equal(2, Records(Episode(), Episode("synthetic-other", surrender: "synthetic-later")).Episodes.Count);
    }

    // R-POW-003: this is a frozen observation contract, with no spendable resource or population mutation.
    [Fact]
    public void CaptivityCopiesInputAndExposesReadOnlyRecords()
    {
        var first = Episode(); var input = new[] { first }; var records = Records(input);
        var before = records.FixtureText(); input[0] = Episode("synthetic-replacement");
        Assert.Same(first, Assert.Single(records.Episodes));
        Assert.Throws<NotSupportedException>(() => ((IList<SyntheticCaptivityEpisode>)records.Episodes).Clear());
        Assert.Equal(before, records.FixtureText());
    }

    [Fact]
    public void CaptivityFixtureTextIsOrdinalInvariantAndCoversEveryField()
    {
        var oldCulture = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            var a = Episode("synthetic-z|:\U0001f6a2"); var b = Episode("synthetic-a", "synthetic-other");
            string? expected = null;
            foreach (var locale in new[] { "he-IL", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                var forward = Records(a, b); var reverse = Records(b, a);
                Assert.Equal("synthetic-a", forward.Episodes[0].EpisodeId);
                Assert.Equal(forward.FixtureText(), reverse.FixtureText());
                expected ??= forward.FixtureText(); Assert.Equal(expected, forward.FixtureText());
                var baseline = Records(Episode()).FixtureText();
                foreach (var changed in new[] { Episode(episode: "synthetic-other"), Episode(cohort: "synthetic-other"),
                    Episode(surrender: "synthetic-other"), Episode(side: "synthetic-other"), Episode(captor: "synthetic-other") })
                    Assert.NotEqual(baseline, Records(changed).FixtureText());
                Assert.NotEqual(Records(Episode(cohort: "a|b", surrender: "c")).FixtureText(),
                    Records(Episode(cohort: "a", surrender: "b|c")).FixtureText());
            }
        }
        finally { CultureInfo.CurrentCulture = oldCulture; CultureInfo.CurrentUICulture = oldUi; }
    }
}
