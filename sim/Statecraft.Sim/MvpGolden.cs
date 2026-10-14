using System;
using Statecraft.Sim.Reference;

namespace Statecraft.Sim
{
    /// <summary>The GDD slice's golden (spec 2026-10-12 section 3; docs/determinism.md, "MVP goldens"): MvpSlice (the real content
    /// with the loop switch on, monthly turns and the slice's placeholder city), the golden seed, Historical mode and the
    /// reference player (D-A7: HistoricalAI's orders, then the jobs filled), until Until.</summary>
    public static class MvpGolden
    {
        /// <summary>Ten monthly turns from 15 May 1948: long enough for the reference player's build at Mefalsim, started on
        /// 15 September 1948, to finish on 15 March 1949 (GDD Phase 1 PR 5). A test choice, not a balance value.</summary>
        public static readonly DateTime Until = new DateTime(1949, 3, 15);

        public static GameState Play(Content c)
        {
            var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < Until && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, ReferencePlayer.Commands(c, s)).State;
            return s;
        }
    }
}
