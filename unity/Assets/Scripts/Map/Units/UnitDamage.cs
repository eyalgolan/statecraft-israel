#nullable enable
using Statecraft.Sim;

namespace Statecraft.Game.Map.Units
{
    /// <summary>#2432: whether a military unit has lost strength, and against what, for the map's damaged look and the unit
    /// card. Presentation only: it reads the sim's state and writes nothing to the sim or a save. A player's unit stores the
    /// strength it had before its first loss as its <see cref="Unit.Full"/> (R-COMBAT-003 with R-TRAIN-004) and a refill to
    /// full clears it, so the look clears when the unit is reinforced; the sim's own <see cref="Training.Room"/> reads the
    /// same "Full, or the strength while it has lost none". An enemy's unit stores no Full (no rule reads one), so its full
    /// strength is its starting force's (unit i of a new game is <c>StartingForces[i]</c>, R-UNIT-006), which is also Q-UI-3's
    /// denominator for its figures (<see cref="UnitRoster.FiguresForStrength"/>).</summary>
    public static class UnitDamage
    {
        /// <summary><paramref name="u"/>'s full strength: its Full once it has lost strength, else for an enemy's unit its
        /// starting force's strength (never below its strength now), else its strength. <paramref name="c"/> may be null (the
        /// legacy loop's callers): an enemy's unit then reads as full.</summary>
        public static int FullStrength(Content? c, Unit u)
        {
            if (u.Full > 0) return u.Full;
            if (c != null && u.Side != "" && u.Side != Movement.PlayerSide && u.Id >= 0 && u.Id < c.StartingForces.Count
                && c.StartingForces[u.Id].Side == u.Side)
                return System.Math.Max(u.Strength, c.StartingForces[u.Id].Strength);
            return u.Strength;
        }

        /// <summary>True for a military unit (a halutz is not one, R-UNIT-001) below its <see cref="FullStrength"/>.</summary>
        public static bool Damaged(Content? c, Unit u) => u.Side != "" && u.Strength < FullStrength(c, u);
    }
}
