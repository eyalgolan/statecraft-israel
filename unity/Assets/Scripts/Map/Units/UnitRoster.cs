#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.Map.Units
{
    /// <summary>One unit to draw now: its row, the anchor it stands at, and how it looks.</summary>
    public sealed class UnitToken : IEquatable<UnitToken>
    {
        public UnitEntry Unit { get; }
        public UnitAnchor Anchor { get; }
        /// <summary>Riflemen on an army token (1, 2 or 3); 0 for aircraft and ships.</summary>
        public int Figures { get; }
        /// <summary>The model's art type (a <c>UnitShowcase.*Type</c> constant; the authored-prefab hook's key).</summary>
        public string ArtType { get; }
        /// <summary>An air unit flies over its base (security level 4 or 5) or stands parked on it.</summary>
        public bool Flying { get; }
        /// <summary>An army token's riflemen's headgear, one per figure in the trio's order (<see cref="UnitData.Headgears"/>);
        /// empty for aircraft and ships.</summary>
        public IReadOnlyList<string> Headgear { get; }
        /// <summary>The Collection vehicles the token draws beside its figures (model ids, <see cref="UnitFigures.VehiclesAt"/>;
        /// issue #665); empty for most.</summary>
        public IReadOnlyList<string> Vehicles { get; }

        public UnitToken(UnitEntry unit, UnitAnchor anchor, int figures, string artType, bool flying, IReadOnlyList<string>? headgear = null,
            IReadOnlyList<string>? vehicles = null)
        {
            Unit = unit; Anchor = anchor; Figures = figures; ArtType = artType; Flying = flying;
            Headgear = headgear ?? Array.Empty<string>();
            Vehicles = vehicles ?? Array.Empty<string>();
        }

        public bool Equals(UnitToken? o) => o != null && ReferenceEquals(Unit, o.Unit) && ReferenceEquals(Anchor, o.Anchor)
            && Figures == o.Figures && ArtType == o.ArtType && Flying == o.Flying && Headgear.SequenceEqual(o.Headgear)
            && Vehicles.SequenceEqual(o.Vehicles);
        public override bool Equals(object? o) => Equals(o as UnitToken);
        public override int GetHashCode() => HashCode.Combine(Unit.Id, Anchor.Place, Figures, ArtType, Flying);
        public override string ToString() => $"{Unit.Id}@{Anchor.Place}:{ArtType}x{Figures}{(Headgear.Count > 0 ? "[" + string.Join("/", Headgear) + "]" : "")}{(Vehicles.Count > 0 ? "+" + string.Join("+", Vehicles) : "")}{(Flying ? " flying" : "")}";
    }

    /// <summary>Which units the map draws, from the sim's state and the map-only unit data (docs/units.md, "Units in play").
    /// Pure and read-only: it reads the date, the security level and the reserve call-up, and never writes the state, so
    /// the sim and its goldens cannot move (UnitsInPlayTests). Rules (owner, 2026-10-11):
    /// a unit shows while the date is inside one of its sourced anchor windows, and never when its row is hidden or is another
    /// side's with no Collection figure (<see cref="OnMap"/>); an Arab force's token shows only once its figure has arrived;
    /// an army token has 3 figures at a call-up of at least the security-level-4 floor (rules.yaml rung_min_mobilization,
    /// 0.4), 2 at the level-3 floor (0.15), else 1; an air unit flies at security level 4 or 5 and stands parked below it,
    /// as the aircraft type of its dated window (the Avia S-199, then the Spitfire).</summary>
    public static class UnitRoster
    {
        public const int FlyingFromRung = 4;

        /// <summary>Whether the map draws <paramref name="u"/> at all: not hidden, and Israel's or a side with a Collection
        /// figure (<see cref="UnitFigures.SideFigure"/>: the Arab Legion and the Arab Liberation Army, issue #665). The Arab
        /// forces' rows were data only until their figures landed, after the sensitivity review (owner, 17 October 2026,
        /// #740: "Nothing is drawn with IDF figures, and there is no placeholder art"); Tal Heuberger's unit models count as reviewed
        /// and approved (owner, 17 October 2026, #756). Every other side's rows stay data only. The roster and the placer both
        /// skip a row that is not on the map, so it draws no token, takes no spot and clears no ground for the kits.</summary>
        public static bool OnMap(UnitEntry u) => u.Hidden == null && (u.Side == UnitData.Israel || UnitFigures.SideFigure(u.Side) != null);

        /// <summary>An Arab force's token has three figures: Q-UI-3 gives 3 at two thirds of full strength or more (owner,
        /// 17 October 2026, #226), and the rows give only the sourced 15 May strength, so every force stands at full strength.</summary>
        public const int SideFigures = 3;

        /// <param name="arrived">Whether a Collection model has arrived (<see cref="UnitFigures.Arrived"/>): an Arab force's
        /// token is drawn only once its figure has (it has no code-built figure). Null: none has.</param>
        public static List<UnitToken> Build(IReadOnlyList<UnitEntry> units, Content content, GameState state, Func<string, bool>? arrived = null)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            return Build(units, content.RulesAt(state.Date), state.Date, state.Rung, state.Mobilization, arrived);
        }

        public static List<UnitToken> Build(IReadOnlyList<UnitEntry> units, Rules rules, DateTime date, int rung, double mobilization,
            Func<string, bool>? arrived = null)
        {
            var list = new List<UnitToken>();
            foreach (var u in units)
            {
                if (!OnMap(u)) continue;
                var anchor = AnchorAt(u, date);
                if (anchor == null) continue;
                if (u.Side != UnitData.Israel)
                {
                    var figure = UnitFigures.SideFigure(u.Side)!;
                    if (u.Service == UnitData.Army && arrived != null && arrived(figure))
                        list.Add(new UnitToken(u, anchor, SideFigures, figure, false));
                    continue;
                }
                switch (u.Service)
                {
                    case UnitData.Army:
                        int figures = FiguresFor(rules, mobilization);
                        var hats = (u.Headgear ?? (IReadOnlyList<string>)UnitData.DefaultHeadgear).Take(figures).ToList();
                        list.Add(new UnitToken(u, anchor, figures, RiflemanType(hats[0]), false, hats, UnitFigures.VehiclesAt(u, date)));
                        break;
                    case UnitData.Air:
                        var type = AircraftAt(u, date);
                        if (type != null) list.Add(new UnitToken(u, anchor, 0, type, rung >= FlyingFromRung));
                        break;
                    case UnitData.Navy:
                        list.Add(new UnitToken(u, anchor, 0, UnitShowcase.CorvetteType, false));
                        break;
                }
            }
            return list;
        }

        /// <summary>The GDD loop's tokens (GDD Phase 5 PR 14, #753; impact plan §2.6): one army token per military unit in
        /// <see cref="GameState.Units"/>, grouped per hex ((q, r) order, then id), and the squadron and the ships from their
        /// rows as <see cref="Build(IReadOnlyList{UnitEntry}, Content, GameState, Func{string, bool})"/> draws them (they are
        /// tokens only, Q-UNIT-5). The sim hex is authoritative: a unit on its starting hex stands on its row's starting
        /// anchor (that anchor's spot, UnitPlacer's one-hex offset included); a unit that moved takes
        /// <paramref name="moved"/>'s anchor for its row and hex (the layer caches it with its spot). Its figures follow
        /// Q-UI-3 (<see cref="FiguresForStrength"/>), not the call-up. A unit is drawn only from a row: unit i of a new game is
        /// <c>content.StartingForces[i]</c> (R-UNIT-006), whose row is its card, flag and figures, and a unit with no row (a
        /// halutz; a unit raised in play, which has no art yet) draws no token. An Arab force's token still waits for its
        /// Collection figure (<paramref name="arrived"/>), and a row off the map (<see cref="OnMap"/>) draws nothing.
        /// Pure and read-only like Build.</summary>
        public static List<UnitToken> FromState(IReadOnlyList<UnitEntry> units, Content content, GameState state, Func<string, bool>? arrived,
            Func<UnitEntry, UnitAnchor, int, int, UnitAnchor> moved)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (moved == null) throw new ArgumentNullException(nameof(moved));
            var list = new List<UnitToken>();
            foreach (var unit in state.Units.Values.OrderBy(v => v.Q).ThenBy(v => v.R).ThenBy(v => v.Id))
            {
                // Unit i of a new game is StartingForces[i] (R-UNIT-006); a unit raised in play takes the next id, at or above
                // the count, and has no row yet (UnitsFromStateTests.AUnitRaisedInPlayDrawsNoToken).
                if (unit.Side == "" || unit.Id >= content.StartingForces.Count) continue;
                var force = content.StartingForces[unit.Id];
                var row = units.FirstOrDefault(u => u.Id == force.Row);
                if (row == null || !OnMap(row) || row.Service != UnitData.Army) continue;
                var start = AnchorAt(row, force.Arrives ?? content.Rules.Start);   // an arriving row stands on its arrival day's anchor (R-UNIT-009)
                if (start == null) continue;   // the map's copy of the file disagrees with the sim's; nothing to stand on
                var anchor = unit.Q == force.Q && unit.R == force.R ? start : moved(row, start, unit.Q, unit.R);
                int figures = FiguresForStrength(unit.Strength, force.Strength);
                if (row.Side != UnitData.Israel)
                {
                    var figure = UnitFigures.SideFigure(row.Side)!;
                    if (arrived != null && arrived(figure)) list.Add(new UnitToken(row, anchor, figures, figure, false));
                    continue;
                }
                var hats = (row.Headgear ?? (IReadOnlyList<string>)UnitData.DefaultHeadgear).Take(figures).ToList();
                list.Add(new UnitToken(row, anchor, figures, RiflemanType(hats[0]), false, hats, UnitFigures.VehiclesAt(row, state.Date)));
            }
            list.AddRange(Build(units, content, state, arrived).Where(t => t.Unit.Service != UnitData.Army));
            return list;
        }

        /// <summary>Q-UI-3 (owner, 15 October 2026, "Take all recommendations"; #226, 17 October 2026): "3 figures at two
        /// thirds or more of the type's full strength, 2 at a third or more, 1 below that". A unit's full strength is the
        /// sourced strength it started with (Q-UNIT-4: a starting unit's Strength is its row's), so a new game draws every
        /// unit with three, and losses take figures away. Integers only: 3s &gt;= 2f, then 3s &gt;= f.</summary>
        public static int FiguresForStrength(int strength, int full)
        {
            if (full <= 0) throw new ArgumentOutOfRangeException(nameof(full), full, "a full strength is at least 1");
            if (3 * strength >= 2 * full) return 3;
            if (3 * strength >= full) return 2;
            return 1;
        }

        /// <summary>The art type of a rifleman with <paramref name="headgear"/> (the authored-prefab hook's key).</summary>
        public static string RiflemanType(string headgear) => headgear switch
        {
            UnitData.SlouchHat => UnitShowcase.RiflemanSlouchHatType,
            UnitData.SteelHelmet => UnitShowcase.RiflemanHelmetType,
            UnitData.WoolCap => UnitShowcase.RiflemanWoolCapType,
            _ => throw new ArgumentException($"unknown headgear '{headgear}'", nameof(headgear)),
        };

        /// <summary>The anchor whose window holds <paramref name="date"/>, or null.</summary>
        public static UnitAnchor? AnchorAt(UnitEntry u, DateTime date)
        {
            foreach (var a in u.Anchors)
                if (date >= UnitData.Day(a.From) && date < UnitData.Day(a.To)) return a;
            return null;
        }

        /// <summary>The art type of the aircraft the unit flew on <paramref name="date"/>, or null between types.</summary>
        public static string? AircraftAt(UnitEntry u, DateTime date)
        {
            if (u.Aircraft == null) return null;
            foreach (var t in u.Aircraft)
            {
                if (date < UnitData.Day(t.From) || (t.To != null && date >= UnitData.Day(t.To))) continue;
                return t.Type == UnitData.AviaS199 ? UnitShowcase.S199Type : UnitShowcase.SpitfireType;
            }
            return null;
        }

        /// <summary>1, 2 or 3 riflemen: the call-up against the floors of security levels 3 and 4 (rules.yaml).</summary>
        public static int FiguresFor(Rules rules, double mobilization)
        {
            if (rules.RungMinMobilization.Count != 5) throw new ArgumentException("rules need five rung floors", nameof(rules));
            if (mobilization >= rules.RungMinMobilization[3]) return 3;
            if (mobilization >= rules.RungMinMobilization[2]) return 2;
            return 1;
        }
    }
}
