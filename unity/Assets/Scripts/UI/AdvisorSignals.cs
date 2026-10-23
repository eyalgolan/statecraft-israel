#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.UI
{
    /// <summary>The signals a message waits for (spec 5). Event signals are raised by a call (AdvisorPresenter.Notice) and
    /// hold for the turn they fired on; state signals hold while the shown state says so and are read on Changed and
    /// TurnEnded only, never each frame (spec 16). A `when` condition measures one number on the state.</summary>
    public static class AdvisorSignals
    {
        public const string NewGame = "new_game", TurnEnded = "turn_ended";
        public static readonly IReadOnlyCollection<string> Events = new[] { NewGame, TurnEnded };
        public static readonly IReadOnlyDictionary<string, Func<AdvisorContext, bool>> States = new Dictionary<string, Func<AdvisorContext, bool>>();
        /// <summary>Each condition's measure; a name ends in _at_least or _at_most, which says how its bound reads.</summary>
        public static readonly IReadOnlyDictionary<string, Func<AdvisorContext, double>> Measures = new Dictionary<string, Func<AdvisorContext, double>>();
        public static IEnumerable<string> Names => Events.Concat(States.Keys);
    }

    /// <summary>What a signal reads: the content, the state the screen shows, the bridge and the decision card.</summary>
    public sealed class AdvisorContext
    {
        public AdvisorContext(SimBridge bridge, EventCardPresenter? card) { Bridge = bridge; Card = card; }
        public SimBridge Bridge { get; }
        public EventCardPresenter? Card { get; }
        public Statecraft.Sim.Content Content => Bridge.Content;
        public Statecraft.Sim.GameState State => Bridge.State;
    }
}
