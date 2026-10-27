using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    // RED-FIRST SKELETON: API surface only, no behaviour yet.
    internal sealed class Gdd03FixtureTrainingCentre
    {
        internal string Id { get; }
        internal string ParentCamp { get; }
        internal bool ParentOperating { get; }
        internal bool UpgradeComplete { get; }
        internal Gdd03FixtureTrainingCentre(string id, string parentCamp, bool parentOperating, bool upgradeComplete)
        { Id = id; ParentCamp = parentCamp; ParentOperating = parentOperating; UpgradeComplete = upgradeComplete; }
    }

    internal sealed class Gdd03TrainingCentreFixture
    {
        internal Gdd03TrainingCentreFixture(Content content, GameState state, IEnumerable<Gdd03FixtureTrainingCentre> centres) { }
        internal void Start(int traineeId, int teacherId, string centre, Gdd03SyntheticMonthContext month) { }
        internal void Advance(int traineeId, Gdd03SyntheticMonthContext month) { }
        internal void Cancel(int traineeId) { }
    }
}
