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
    /// of each unit it destroyed ("Dust when a unit dies"). On the production path the hit, the defenders' reaction and the
    /// dust of the units the engagement destroyed wait for the attackers' first fire take that really starts (or, if none can,
    /// the last one's wait), so nothing lands before the shot; the reaction and the dust then start when the hit lands on the
    /// target (CombatBursts.ImpactS), with the volley's first puff or the shell's burst. Presentation only. A unit whose token cannot play now (destroyed, hidden, far, or no
    /// fire take) is skipped and nothing is kept to play later. GameRoot owns it.</summary>
    public sealed class UnitAttackPresenter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<int, bool> play;
        readonly Func<CombatHit, bool>? hit;
        readonly Func<int, float, bool>? destroyed;
        readonly Func<CombatHit, bool>? defenderHit;
        readonly Func<int, Action, Action, bool>? timedPlay;

        /// <param name="play">Plays a sim unit's attack (UnitLayer.PlayAttack); false when its token cannot.</param>
        /// <param name="hit">Plays an engagement's hit on the hex it attacked (UnitLayer.PlayHit), or null.</param>
        /// <param name="destroyed">Raises a destroyed sim unit's dust (UnitLayer.PlayDestroyed), the given seconds from now
        /// (when the hit that destroyed it lands, CombatBursts.ImpactS), or null.</param>
        /// <param name="defenderHit">Shakes and flashes the drawn defenders after positive aggregate loss, or null.</param>
        /// <param name="timedPlay">Production attack path that reports the actual fire-take start or its missed wait.</param>
        public UnitAttackPresenter(SimBridge bridge, Func<int, bool> play, Func<CombatHit, bool>? hit = null,
            Func<int, float, bool>? destroyed = null, Func<CombatHit, bool>? defenderHit = null,
            Func<int, Action, Action, bool>? timedPlay = null)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.play = play ?? throw new ArgumentNullException(nameof(play));
            this.hit = hit; this.destroyed = destroyed; this.defenderHit = defenderHit; this.timedPlay = timedPlay;
            bridge.CombatResolved += Resolved;
        }

        void Resolved(IReadOnlyList<Cause> lines)
        {
            FireCue? cue = null;   // the timed engagement just before: Combat.Engage adds its Destroyed lines right after it
            float impactS = 0f;    // and when its hit lands after its fire take starts
            foreach (var line in lines)
            {
                if (line.Key == WhyKeys.Destroyed)
                {
                    if (destroyed is Func<int, float, bool> dust && line.Units.Count > 0)
                    {
                        int dead = line.Units[0].Id; float delay = impactS;
                        if (cue != null) cue.Then(() => dust(dead, delay)); else dust(dead, delay);
                    }
                    continue;
                }
                cue = null; impactS = 0f;
                var parsed = HitOf(line, IsGuns);
                if (parsed is CombatHit h && timedPlay != null && Count(line, out int attackerCount))
                {
                    // The production path: the defenders react (on a loss) and the hit lands with the first real fire take.
                    cue = new FireCue(attackerCount, () => Impact(h));
                    impactS = CombatBursts.ImpactS(h);
                    foreach (int id in Attackers(line)) timedPlay(id, cue.Started, cue.Missed);
                }
                else
                {
                    foreach (int id in Attackers(line)) play(id);
                    if (parsed is CombatHit immediate) { Impact(immediate); impactS = CombatBursts.ImpactS(immediate); }
                }
            }
        }

        /// <summary>The defenders' reaction when the engagement records a loss, then the hit on the hex it attacked.</summary>
        void Impact(CombatHit h)
        {
            if (h.DefenderLoss > 0) defenderHit?.Invoke(h);
            hit?.Invoke(h);
        }

        /// <summary>One engagement's group cue: the first actual fire take plays the impact once; if none can show, the last
        /// miss does. UnitLayer's timed PlayAttack always reports one of the two (a hidden or rebuilt token reports a miss).
        /// <see cref="Then"/> adds to the impact (a destroyed unit's dust): it plays with it, or at once if it already has,
        /// since a take that starts at once reports during the engagement's own line.</summary>
        sealed class FireCue
        {
            int left;
            bool shown;
            readonly List<Action> impacts = new List<Action>();

            internal FireCue(int attackers, Action impact) { left = attackers; impacts.Add(impact); }

            internal void Then(Action more) { if (shown) more(); else impacts.Add(more); }
            internal void Started() { if (!shown) Play(); }
            internal void Missed() { if (left > 0) left--; if (!shown && left == 0) Play(); }

            void Play()
            {
                shown = true;
                foreach (var impact in impacts) impact();
                impacts.Clear();
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
            int defenderLoss = int.TryParse(engagement.Args[3], NumberStyles.None, CultureInfo.InvariantCulture, out int loss)
                && loss > 0 ? loss : 0;
            return new CombatHit(q, r, guns, smallArms, defenderLoss, defenders);
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
