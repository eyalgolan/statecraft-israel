using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class ReleaseGateTests
{
    /// <summary>R-GATE-002: the Unity build gate (BuildGateTests) accepts the settlement-control strings review only with
    /// the sim's one canonical owner-review status, so the manifest's word must not drift from it.</summary>
    [Fact]
    public void SettlementControlStringsUseTheCanonicalSensitivityReviewStatus()
    {
        Assert.Equal("sensitivity-reviewed", ReleaseGate.ReviewedStatus);
    }

    /// <summary>R-GATE-001: an event tagged displacement must be sensitivity-reviewed before a shareable build.</summary>
    [Fact]
    public void AnUnreviewedDisplacementEventIsRefused()
    {
        var files = Fixture.Write(events: new Dictionary<string, string>
        {
            ["displacement.yaml"] = "id: displacement\ntitle: A decision\ntext: A decision is required.\nfrom: 1948-06-01\ntags: [displacement]\noptions:\n  - id: continue\n    text: Continue\nstatus: fact-checked\n",
        });

        Assert.Equal(new[] { "displacement" }, ReleaseGate.SensitiveUnreviewed(ContentLoader.Load(files)));
    }

    [Fact]
    public void TheRealContentHasNoDisplacementEventYet()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.DoesNotContain(content.Events, e => e.Tags.Contains("displacement", System.StringComparer.OrdinalIgnoreCase));
    }
}
