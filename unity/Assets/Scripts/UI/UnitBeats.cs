#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    public enum BeatKind { Move, Attack }

    /// <summary>One unit's move as a cause records it: the hex it stood on (UnitRef.Q, R, #2230) and the hex it reached.</summary>
    public readonly struct UnitStep
    {
        public readonly int Unit, FromQ, FromR, ToQ, ToR;
        public UnitStep(int unit, int fromQ, int fromR, int toQ, int toR) { Unit = unit; FromQ = fromQ; FromR = fromR; ToQ = toQ; ToR = toR; }
        public override string ToString() => FormattableString.Invariant($"{Unit}: ({FromQ},{FromR}) to ({ToQ},{ToR})");
    }

    /// <summary>Which of a step's Why lines open a beat: a move line (UnitMoved, the destination in Args[0]) or an
    /// engagement line (Engagement, as Combat.Engage writes it). <see cref="Enemy"/> reads the enemy's orders (resolver steps 1
    /// and 2); another reader of the same lines (#2544 PR 2, an operation's moves and attacks) brings its own two tests and
    /// gets the same grouping, focus and pacing.</summary>
    public sealed class BeatSource
    {
        readonly Func<Cause, bool> isMove, isAttack;

        public BeatSource(Func<Cause, bool> isMove, Func<Cause, bool> isAttack)
        {
            this.isMove = isMove ?? throw new ArgumentNullException(nameof(isMove));
            this.isAttack = isAttack ?? throw new ArgumentNullException(nameof(isAttack));
        }

        /// <summary>The enemy's moves (Movement.Move with enemy: true) and attacks (an engagement whose attackers are the
        /// enemy's). The player's own lines carry no enemy order, so a turn's earlier player lines are never read.</summary>
        public static readonly BeatSource Enemy = new BeatSource(
            c => c.Key == WhyKeys.UnitMoved && c.EnemyOrder == Movement.Kind,
            c => c.Key == WhyKeys.Engagement && c.EnemyOrder == Combat.Attack);

        internal bool IsMove(Cause c) => c.Key == WhyKeys.UnitMoved && isMove(c);
        internal bool IsAttack(Cause c) => c.Key == WhyKeys.Engagement && isAttack(c);
    }

    /// <summary>One beat of a replay: up to <see cref="UnitBeats.Timing.MaxUnitsPerBeat"/> nearby moves, or one engagement
    /// with its retreats and losses, or (<see cref="Folded"/>) every action past the last full beat at once.</summary>
    public sealed class UnitBeat
    {
        internal readonly List<UnitStep> moves = new List<UnitStep>(), retreats = new List<UnitStep>();
        internal readonly List<int> attackers = new List<int>();
        internal readonly List<Cause> causes = new List<Cause>();

        public BeatKind Kind { get; internal set; }
        /// <summary>The side whose beat it is (UnitRef.Side): a move's unit's, an engagement's first-named attacker's (Combat.Fight
        /// names the attackers first, by id). A beat never holds two sides' actions (owner, 8 November 2026: "One side at a time
        /// (Recommended)").</summary>
        public string Side { get; internal set; } = "";
        public IReadOnlyList<UnitStep> Moves => moves;
        /// <summary>The engagements' attackers (UnitAttackPresenter.Attackers), whose fire take plays on this beat.</summary>
        public IReadOnlyList<int> Attackers => attackers;
        /// <summary>The defenders an engagement pushed back (Retreated lines), from the hex they held to the one they reached.</summary>
        public IReadOnlyList<UnitStep> Retreats => retreats;
        /// <summary>The units whose tokens wait for this beat (UnitLayer.Hold): the movers, then the retreaters.</summary>
        public IReadOnlyList<int> Units => moves.Select(m => m.Unit).Concat(retreats.Select(m => m.Unit)).ToList();
        /// <summary>The hex the camera goes to: the cube-rounded middle of a move beat's destinations, an engagement's hex.</summary>
        public int FocusQ { get; internal set; }
        public int FocusR { get; internal set; }
        /// <summary>One side's actions past the beats a long turn keeps (at most <see cref="UnitBeats.Timing.MaxBeats"/> - 1),
        /// played together, with no pan.</summary>
        public bool Folded { get; internal set; }
        /// <summary>The Why lines the beat plays, in the sim's order: its moves, or its engagements and their results.</summary>
        public IReadOnlyList<Cause> Causes => causes;
        /// <summary>How many engagements the beat plays (1 on an attack beat, 0 on a move beat, any number when folded).</summary>
        public int Engagements { get; internal set; }
        /// <summary>Seconds the camera takes to reach the focus (0 when folded).</summary>
        public float Pan { get; internal set; }
        /// <summary>Seconds the beat holds after its slide (a move) or for its fire take (an attack).</summary>
        public float Dwell { get; internal set; }
        /// <summary>Whether the beat's tokens slide (UnitTokenMotion.Duration) before its dwell: any beat with moves.</summary>
        public bool Slides => moves.Count > 0;
        public float Length => Pan + (Slides ? UnitTokenMotion.Duration : 0f) + Dwell;
    }

    /// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 2): the beats of a replay, from a
    /// step's recorded unit actions (StepResult.Why) in the sim's own order. Presentation only: it groups and paces what the
    /// sim already resolved and decides nothing. One pass over the lines with no allocation per frame (#2905: a caller builds
    /// them on the replay's first frame, not inside End turn). The owner's answers (2 November 2026, "Q1-Q7 recommended, Q8
    /// yes"): every move plays, on screen or off (Q2 a); the pacing below (Q3); nearby moves share a beat, beats past the
    /// eighth fold into one (Q4). The owner's answer of 8 November 2026, "One side at a time (Recommended)": the beats play side
    /// by side, the sides in the order they first act in the step's lines, each side's beats in the sim's order; beats group
    /// and fold within a side only. Only the playback's order changes: the sim, its lines and every number stay as they are.</summary>
    public static class UnitBeats
    {
        /// <summary>The pacing (Q3) and grouping (Q4), in seconds and hexes. The slide itself is #2176's
        /// UnitTokenMotion.Duration (0.65 s), unchanged.</summary>
        public static class Timing
        {
            public const float PanMin = 0.3f, PanMax = 0.8f, PanPerHex = 0.05f;
            public const float MoveDwell = 0.35f, AttackMin = 0.8f, AttackMax = 1.5f;
            /// <summary>The whole replay's longest (Q3: "a 12 s cap on the whole playback").</summary>
            public const float Cap = 12f;
            /// <summary>How far the cap may shrink a pan and a move's dwell; an attack's hold never drops below AttackMin.</summary>
            public const float MinPan = 0.25f, MinDwell = 0.1f;
            public const int BatchRadiusHexes = 3, MaxUnitsPerBeat = 4, MaxBeats = 8;
        }

        /// <summary>The pan to a point <paramref name="hexes"/> away: PanPerHex a hex, between PanMin and PanMax.</summary>
        public static float PanSeconds(float hexes) => Math.Clamp(Timing.PanPerHex * hexes, Timing.PanMin, Timing.PanMax);

        /// <summary>The beats of <paramref name="why"/> that <paramref name="source"/> reads, paced and capped. A move joins
        /// the move beat before it while that beat has room and the move's destination lies within BatchRadiusHexes of the
        /// beat's first destination. An engagement opens its own beat, which takes the Destroyed, Retreated and Surrendered
        /// lines after it, up to the next engagement (anyone's: a player's strike's retreats are never an enemy beat's).</summary>
        public static IReadOnlyList<UnitBeat> From(IReadOnlyList<Cause> why, BeatSource source)
        {
            if (why == null) throw new ArgumentNullException(nameof(why));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var beats = new List<UnitBeat>();
            var moving = new Dictionary<string, UnitBeat>(StringComparer.Ordinal);   // per side, the beat its next move may join
            UnitBeat? attack = null;   // the engagement whose results follow
            foreach (var c in why)
            {
                if (source.IsMove(c))
                {
                    attack = null;
                    if (c.Units.Count == 0 || c.Args.Count == 0 || !TryHex(c.Args[0], out int q, out int r)) continue;
                    var step = new UnitStep(c.Units[0].Id, c.Units[0].Q, c.Units[0].R, q, r);
                    var side = c.Units[0].Side ?? "";
                    if (!moving.TryGetValue(side, out var beat) || beat.moves.Count >= Timing.MaxUnitsPerBeat
                        || MapActions.Distance(beat.moves[0].ToQ, beat.moves[0].ToR, q, r) > Timing.BatchRadiusHexes)
                    {
                        moving[side] = beat = new UnitBeat { Kind = BeatKind.Move, Side = side };
                        beats.Add(beat);
                    }
                    beat.moves.Add(step);
                    beat.causes.Add(c);
                }
                else if (c.Key == WhyKeys.Engagement)
                {
                    moving.Clear(); attack = null;
                    if (!source.IsAttack(c) || c.Args.Count == 0 || !TryHex(c.Args[0], out int q, out int r)) continue;
                    attack = new UnitBeat { Kind = BeatKind.Attack, FocusQ = q, FocusR = r, Engagements = 1,
                        Side = c.Units.Count > 0 ? c.Units[0].Side ?? "" : "" };
                    attack.attackers.AddRange(UnitAttackPresenter.Attackers(c));
                    attack.causes.Add(c);
                    beats.Add(attack);
                }
                else if (attack != null && Result(c.Key))
                {
                    attack.causes.Add(c);
                    if (c.Key == WhyKeys.Retreated && c.Units.Count > 0 && c.Args.Count > 0 && TryHex(c.Args[0], out int q, out int r))
                        attack.retreats.Add(new UnitStep(c.Units[0].Id, c.Units[0].Q, c.Units[0].R, q, r));
                }
            }
            foreach (var b in beats) if (b.Kind == BeatKind.Move) Focus(b);
            var sides = new List<string>();
            foreach (var b in beats) if (!sides.Contains(b.Side)) sides.Add(b.Side);
            var ordered = beats.OrderBy(b => sides.IndexOf(b.Side)).ToList();   // OrderBy is stable: the sim's order within a side
            ordered = Fold(ordered);
            Pace(ordered);
            return ordered;
        }

        /// <summary>The replay's length: every beat's pan, slide and dwell.</summary>
        public static float Total(IReadOnlyList<UnitBeat> beats)
        {
            float total = 0f;
            foreach (var b in beats) total += b.Length;
            return total;
        }

        /// <summary>An engagement's own results (Combat.Fight writes them right after its line).</summary>
        static bool Result(string key) => key == WhyKeys.Destroyed || key == WhyKeys.Retreated
            || key == WhyKeys.SurrenderedPrisoners || key == WhyKeys.SurrenderedHeld;

        static bool TryHex(string hex, out int q, out int r)
        {
            q = r = 0;
            var parts = hex.Split(',');
            return parts.Length == 2
                && int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out q)
                && int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out r);
        }

        /// <summary>A move beat looks at the middle of its destinations, rounded to a hex in cube coordinates.</summary>
        static void Focus(UnitBeat b)
        {
            double q = b.moves.Average(m => m.ToQ), r = b.moves.Average(m => m.ToR), s = -q - r;
            double rq = Math.Round(q, MidpointRounding.AwayFromZero), rr = Math.Round(r, MidpointRounding.AwayFromZero);
            double rs = Math.Round(s, MidpointRounding.AwayFromZero);
            double dq = Math.Abs(rq - q), dr = Math.Abs(rr - r), ds = Math.Abs(rs - s);
            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;
            b.FocusQ = (int)rq; b.FocusR = (int)rr;
        }

        /// <summary>Past MaxBeats, the first beats stay and every later one is played at once, where the camera stays (Q4):
        /// one folded beat per side among them, so a fold never mixes sides. As many beats stay as leave room for those folded
        /// ones within MaxBeats (MaxBeats - 1 on a one-side turn), so the cap below always holds. A folded beat's focus is the
        /// beat before it, so a later beat's pan is measured from where the camera is.</summary>
        static List<UnitBeat> Fold(List<UnitBeat> beats)
        {
            if (beats.Count <= Timing.MaxBeats) return beats;
            int keep = Timing.MaxBeats - 1;
            while (keep > 1 && keep + SidesFrom(beats, keep) > Timing.MaxBeats) keep--;
            var kept = beats.Take(keep).ToList();
            UnitBeat? folded = null;
            foreach (var b in beats.Skip(keep))
            {
                if (folded == null || folded.Side != b.Side)
                {
                    var before = kept[kept.Count - 1];
                    folded = new UnitBeat { Folded = true, Side = b.Side, FocusQ = before.FocusQ, FocusR = before.FocusR };
                    kept.Add(folded);
                }
                folded.moves.AddRange(b.moves); folded.retreats.AddRange(b.retreats);
                folded.attackers.AddRange(b.attackers); folded.causes.AddRange(b.causes);
                folded.Engagements += b.Engagements;
            }
            foreach (var f in kept) if (f.Folded) f.Kind = f.moves.Count > 0 ? BeatKind.Move : BeatKind.Attack;
            return kept;
        }

        /// <summary>How many sides the beats from <paramref name="from"/> on belong to (they come side by side).</summary>
        static int SidesFrom(List<UnitBeat> beats, int from)
        {
            int n = 0;
            for (int i = from; i < beats.Count; i++) if (i == from || beats[i].Side != beats[i - 1].Side) n++;
            return n;
        }

        /// <summary>The pans and dwells at full pace (Q3), then, if the whole passes Cap less the return glide, each pan and
        /// dwell shrunk by the same share of the way to its minimum, so the beats are exactly that long; the slides never
        /// shrink. The camera's glide back to the player's view after the last beat (EnemyTurnPlayback.Next) is at most PanMax
        /// (CameraGlide.Seconds), so the whole playback, return included, stays within Cap (#3004). With at most MaxBeats
        /// beats the minimums with their slides sum to at most Cap - PanMax (one kept attack beat, 1.05 s, and seven folded
        /// beats that slide and fight, 1.45 s each), so the cap is always met.</summary>
        static void Pace(List<UnitBeat> beats)
        {
            float slides = 0f, full = 0f, least = 0f;
            for (int i = 0; i < beats.Count; i++)
            {
                var b = beats[i];
                b.Pan = b.Folded ? 0f
                    : i == 0 ? Timing.PanMax   // where the camera starts is not known here: the longest
                    : PanSeconds(MapActions.Distance(beats[i - 1].FocusQ, beats[i - 1].FocusR, b.FocusQ, b.FocusR));
                b.Dwell = b.Engagements > 0 ? (b.Folded ? Timing.AttackMin : Timing.AttackMax) : Timing.MoveDwell;
                if (b.Slides) slides += UnitTokenMotion.Duration;
                full += b.Pan + b.Dwell;
                least += MinPan(b) + MinDwell(b);
            }
            const float beatsCap = Timing.Cap - Timing.PanMax;   // the return glide's longest is left for it
            if (slides + full <= beatsCap || full <= least) return;
            float share = Math.Max(0f, (beatsCap - slides - least) / (full - least));
            foreach (var b in beats)
            {
                b.Pan = MinPan(b) + share * (b.Pan - MinPan(b));
                b.Dwell = MinDwell(b) + share * (b.Dwell - MinDwell(b));
            }
        }

        static float MinPan(UnitBeat b) => b.Folded ? 0f : Timing.MinPan;
        static float MinDwell(UnitBeat b) => b.Engagements > 0 ? Timing.AttackMin : Timing.MinDwell;
    }
}
