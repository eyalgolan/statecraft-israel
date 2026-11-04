#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Plays each fresh engagement's attackers' fire take on the map (#1867; design by Tal Heuberger, #2078): the
    /// attacking units' figures, supporting guns included, never the defenders'. With it, the hit on the hex it attacked
    /// (owner, 4 November 2026: "Hit on target hex": a shell's burst when guns fired, dust when other units did) and the dust
    /// of each unit it destroyed ("Dust when a unit dies"). Presentation only. A unit whose token cannot play now (destroyed,
    /// hidden, far, or no fire take) is skipped and nothing is kept to play later. GameRoot owns it.</summary>
    public sealed class UnitAttackPresenter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<int, bool> play;
        readonly Func<CombatHit, bool>? hit;
        readonly Func<int, bool>? destroyed;

        /// <param name="play">Plays a sim unit's attack (UnitLayer.PlayAttack); false when its token cannot.</param>
        /// <param name="hit">Plays an engagement's hit on the hex it attacked (UnitLayer.PlayHit), or null.</param>
        /// <param name="destroyed">Raises a destroyed sim unit's dust (UnitLayer.PlayDestroyed), or null.</param>
        public UnitAttackPresenter(SimBridge bridge, Func<int, bool> play, Func<CombatHit, bool>? hit = null, Func<int, bool>? destroyed = null)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.play = play ?? throw new ArgumentNullException(nameof(play));
            this.hit = hit; this.destroyed = destroyed;
            bridge.CombatResolved += Resolved;
        }

        void Resolved(IReadOnlyList<Cause> lines)
        {
            foreach (var line in lines)
            {
                foreach (int id in Attackers(line)) play(id);
                if (hit != null && HitOf(line, IsGuns) is CombatHit h) hit(h);
                if (destroyed != null && line.Key == WhyKeys.Destroyed && line.Units.Count > 0) destroyed(line.Units[0].Id);
            }
        }

        /// <summary>True for a unit type of the guns' class (Combat.ArtilleryClass), as the sim's R-COMBAT-007 reads it.</summary>
        bool IsGuns(string type) => bridge.Content.UnitTypes.Any(t => t.Id == type && t.Class == Combat.ArtilleryClass);

        /// <summary>An engagement's attackers, from its structured line (Combat.Engage): the units are the attackers by id,
        /// supporting guns included, then the defenders, and the fifth argument counts the attackers. Never the report's
        /// text. None for another line or a malformed count.</summary>
        internal static IEnumerable<int> Attackers(Cause engagement)
        {
            if (!Count(engagement, out int count)) yield break;
            for (int i = 0; i < count; i++) yield return engagement.Units[i].Id;
        }

        /// <summary>An engagement's hit, from its structured line: the hex it hit (the first argument, "q,r"), whether any
        /// attacker is a gun (<paramref name="isGuns"/> of its type) and whether any is not, and the defenders by id. Null for
        /// another line, a malformed count or a malformed hex.</summary>
        internal static CombatHit? HitOf(Cause engagement, Func<string, bool> isGuns)
        {
            if (!Count(engagement, out int count)) return null;
            var hex = engagement.Args[0].Split(',');
            if (hex.Length != 2 || !int.TryParse(hex[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int q)
                || !int.TryParse(hex[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int r)) return null;
            bool guns = false, smallArms = false;
            for (int i = 0; i < count; i++) { if (isGuns(engagement.Units[i].Type)) guns = true; else smallArms = true; }
            var defenders = new int[engagement.Units.Count - count];
            for (int i = count; i < engagement.Units.Count; i++) defenders[i - count] = engagement.Units[i].Id;
            return new CombatHit(q, r, guns, smallArms, defenders);
        }

        static bool Count(Cause engagement, out int count)
        {
            count = 0;
            return engagement.Key == WhyKeys.Engagement && engagement.Args.Count >= 5
                && int.TryParse(engagement.Args[4], NumberStyles.None, CultureInfo.InvariantCulture, out count)
                && count >= 1 && count <= engagement.Units.Count;
        }

        public void Dispose() => bridge.CombatResolved -= Resolved;
    }
}
