using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public sealed partial class SettlementControl
    {
        // R-CTRL-012: owner ruling, 27 October 2026. Read the existing catalogue without rewriting its provenance.
        internal static int OccupationDuration(Content c, GameState s) => c.Ruleset == SaveGame.Gdd03Ruleset
            ? CatalogueTurns(c) : c.RulesAt(s.Date).Mvp!.OccupationTurns!.Value;

        static int CatalogueTurns(Content c)
        {
            const string key = "occupation.turns";
            var table = c.Gdd03Balance;
            if (table == null || table.Ruleset != SaveGame.Gdd03Ruleset || table.Values == null
                || !table.Values.TryGetValue(key, out var row) || row == null
                || (row.Basis != "Agreed" && row.Basis != "Default")
                || row.Values == null || row.Values.Count != 1 || row.Parts == null || row.Parts.Count != 0
                || !string.IsNullOrEmpty(row.Expression) || row.Values[0] < 1 || row.Values[0] > int.MaxValue
                || row.Values[0] != decimal.Truncate(row.Values[0]))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key,
                    "requires one positive exact Agreed/Default integer; no Deferred or old-rules fallback");
            return decimal.ToInt32(row.Values[0]);
        }

        internal static void ValidateCatalogue(Content c) { CatalogueTurns(c); }

        // R-CTRL-023: preview and command share this guard; a preview never authorizes a later stale command.
        static CaptureRefusal? CanAcquire(Content c, GameState s, string id)
        {
            if (c.Ruleset != SaveGame.Gdd03Ruleset)
                return new CaptureRefusal(CaptureRefusal.NotInPlay, "That order is not available in this game");
            if (s.GameOver != null)
                return new CaptureRefusal(CaptureRefusal.NotInPlay, "The game is over");
            var t = c.SettlementTargets.FirstOrDefault(x => x.Id == id);
            if (t == null || t.Status != "sensitivity-reviewed"
                || !c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities.Contains(id)
                || !(c.HexAt(t.Q, t.R) is MapHex hex) || !hex.Land)
                return new CaptureRefusal(CaptureRefusal.NotInPlay, "That locality is not an available reviewed capture target");
            if (s.Settlements.TryGetValue(id, out var old) && !(old.Owner == "" && old.Fled && !old.Depopulated))
                return new CaptureRefusal(CaptureRefusal.NotAwaiting, "That locality cannot be captured again in its current state");
            if (s.Units.Values.Any(u => u.Q == t.Q && u.R == t.R && IsEnemy(u))
                || !s.Units.Values.Any(u => u.Q == t.Q && u.R == t.R && u.Side == Movement.PlayerSide
                    && c.UnitTypes.Any(type => type.Id == u.Type && (type.Class == "infantry" || type.Class == "armour"))))
                return new CaptureRefusal(CaptureRefusal.NeedsUnit, "Capture needs a friendly ground unit and no defending enemy unit");
            return null;
        }

        internal static CaptureRefusal? Acquire(Content c, GameState s, string id, List<Cause> why)
        {
            if (CanAcquire(c, s, id) is CaptureRefusal no) return no;
            var t = c.SettlementTargets.First(x => x.Id == id);
            var fled = t.Residents == ContentLoader.Fled;
            s.Settlements[id] = new SettlementControl(Movement.PlayerSide, AwaitingChoice, fled: fled);
            why.Add(new Cause("map", id, 0, $"{c.SettlementNames[id]} is captured: Israeli units hold it and no enemy unit is there. "
                + (fled ? "Its residents fled. " : "") + "It awaits a decision", key: CauseKeys.PlaceCaptured, args: new[] { fled ? "1" : "0" }));
            return null;
        }

        /// <summary>Eligibility, selected control transition and existing outcome previews before confirmation.
        /// No outcome is selected; recurring occupation upkeep is separate from immediate resource changes.</summary>
        public static LocalityCapturePreview PreviewCapture(Content c, GameState s, string id)
        {
            var after = s.Clone();
            s.Settlements.TryGetValue(id, out var before);
            var refusal = Acquire(c, after, id, new List<Cause>());
            return refusal != null
                ? new LocalityCapturePreview(id, refusal, before, null, Array.Empty<CaptureOption>(), null, null)
                : new LocalityCapturePreview(id, null, before, after.Settlements[id], CaptureOptions(c, after, id),
                    OccupationDuration(c, after), c.RulesAt(s.Date).Mvp!.OccupationUpkeepPerTurn!.Value);
        }
    }

    public sealed class LocalityCapturePreview
    {
        public string Settlement { get; }
        public CaptureRefusal? Refused { get; }
        public SettlementControl? Before { get; }
        public SettlementControl? After { get; }
        public IReadOnlyList<CaptureOption> Options { get; }
        public int? OccupationTurns { get; }
        public double? UpkeepPerTurn { get; }
        internal LocalityCapturePreview(string id, CaptureRefusal? refused, SettlementControl? before,
            SettlementControl? after, IReadOnlyList<CaptureOption> options, int? turns, double? upkeep)
        { Settlement = id; Refused = refused; Before = before; After = after; Options = options; OccupationTurns = turns; UpkeepPerTurn = upkeep; }
    }
}
