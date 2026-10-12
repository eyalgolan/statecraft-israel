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

        public UnitToken(UnitEntry unit, UnitAnchor anchor, int figures, string artType, bool flying, IReadOnlyList<string>? headgear = null)
        {
            Unit = unit; Anchor = anchor; Figures = figures; ArtType = artType; Flying = flying;
            Headgear = headgear ?? Array.Empty<string>();
        }

        public bool Equals(UnitToken? o) => o != null && ReferenceEquals(Unit, o.Unit) && ReferenceEquals(Anchor, o.Anchor)
            && Figures == o.Figures && ArtType == o.ArtType && Flying == o.Flying && Headgear.SequenceEqual(o.Headgear);
        public override bool Equals(object? o) => Equals(o as UnitToken);
        public override int GetHashCode() => HashCode.Combine(Unit.Id, Anchor.Place, Figures, ArtType, Flying);
        public override string ToString() => $"{Unit.Id}@{Anchor.Place}:{ArtType}x{Figures}{(Headgear.Count > 0 ? "[" + string.Join("/", Headgear) + "]" : "")}{(Flying ? " flying" : "")}";
    }

    /// <summary>Which units the map draws, from the sim's state and the map-only unit data (docs/units.md, "Units in play").
    /// Pure and read-only: it reads the date, the security level and the reserve call-up, and never writes the state, so
    /// the sim and its goldens cannot move (UnitsInPlayTests). Rules (owner, 2026-10-11):
    /// a unit shows while the date is inside one of its sourced anchor windows, and never when its row is hidden;
    /// an army token has 3 figures at a call-up of at least the security-level-4 floor (rules.yaml rung_min_mobilization,
    /// 0.4), 2 at the level-3 floor (0.15), else 1; an air unit flies at security level 4 or 5 and stands parked below it,
    /// as the aircraft type of its dated window (the Avia S-199, then the Spitfire).</summary>
    public static class UnitRoster
    {
        public const int FlyingFromRung = 4;

        public static List<UnitToken> Build(IReadOnlyList<UnitEntry> units, Content content, GameState state)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            return Build(units, content.RulesAt(state.Date), state.Date, state.Rung, state.Mobilization);
        }

        public static List<UnitToken> Build(IReadOnlyList<UnitEntry> units, Rules rules, DateTime date, int rung, double mobilization)
        {
            var list = new List<UnitToken>();
            foreach (var u in units)
            {
                if (u.Hidden != null) continue;
                var anchor = AnchorAt(u, date);
                if (anchor == null) continue;
                switch (u.Service)
                {
                    case UnitData.Army:
                        int figures = FiguresFor(rules, mobilization);
                        var hats = (u.Headgear ?? (IReadOnlyList<string>)UnitData.DefaultHeadgear).Take(figures).ToList();
                        list.Add(new UnitToken(u, anchor, figures, RiflemanType(hats[0]), false, hats));
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
