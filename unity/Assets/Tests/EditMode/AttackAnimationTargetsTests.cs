using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using Statecraft.Sim;

public class AttackAnimationTargetsTests
{
    static Cause Line(string key, string count) => new Cause("map", "9,9", 0, "Unrelated localized text",
        key: key, units: new[] { new UnitRef(new Unit(1, "infantry", 0, 0)),
            new UnitRef(new Unit(2, "guns", 0, 0)), new UnitRef(new Unit(3, "defender", 0, 0)) },
        args: new[] { "9,9", "100", "0", "0", count });

    [Test]
    public void ResolvedAttackIncludesSupportingGunAndExcludesDefender()
    {
        CollectionAssert.AreEqual(new[] { 1, 2 }, AttackAnimationTargets.From(Line(WhyKeys.Engagement, "2")));
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("4")]
    [TestCase("invalid")]
    public void InvalidAttackerCountIsIgnored(string count)
    {
        Assert.IsEmpty(AttackAnimationTargets.From(Line(WhyKeys.Engagement, count)));
    }

    [Test]
    public void AnOrderIsNotAResolvedAttack()
    {
        Assert.IsEmpty(AttackAnimationTargets.From(Line(WhyKeys.AttackOrdered, "2")));
    }
}
