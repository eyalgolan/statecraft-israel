#nullable enable
using System;
using System.Collections.Generic;

namespace Statecraft.Game.UI
{
    /// <summary>What the advisors keep for one game in one sitting: the catalogue, the settings lines, the queue and its
    /// history, and the event signals fired this turn. GameRoot keeps it across a language switch (Begin's keep) and makes
    /// a new one for a loaded game; NewGame resets it. Nothing of it is saved (spec 15).</summary>
    public sealed class AdvisorSession
    {
        /// <param name="autoShow">Whether messages show by themselves: only where the start screen shows and a settings file
        /// remembers what was seen (never in automated runs). Kept with the session, so a language switch (whose Begin
        /// shows no start screen) does not silence them.</param>
        public AdvisorSession(AdvisorCatalog catalog, AdvisorSettings settings, bool autoShow)
        {
            Catalog = catalog; Settings = settings; AutoShow = autoShow; Queue = new AdvisorQueue(catalog, settings);
        }

        /// <summary>The opening chain survives a presenter/language rebuild, not a load or a new game (spec9.2).</summary>
        public sealed class ChainState
        {
            public int Step = -1, OpenTurn;
            public bool CardWasOpen, EventShown;
            public string? CardEvent;
            /// <summary>Puts back the selection the chain found, carried over a rebuild (a language switch) to the rebuilt
            /// presenter's first city or unit, so closing the chain later still restores the player's own.</summary>
            public Action? PutBack;
        }
        public ChainState Chain { get; } = new ChainState();

        public bool AutoShow { get; }
        public AdvisorCatalog Catalog { get; }
        public AdvisorSettings Settings { get; }
        public AdvisorQueue Queue { get; }
        /// <summary>The event signals fired on <see cref="FiredTurn"/>; they hold for that turn only.</summary>
        public HashSet<string> Fired { get; } = new HashSet<string>(StringComparer.Ordinal);
        public int FiredTurn { get; set; } = -1;
        /// <summary>Whether Meyerson has been named in full once this profile (her first appearance, spec 3.1), by her seen marks.</summary>
        public bool Introduced(string advisor) =>
            advisor != "meyerson" || System.Linq.Enumerable.Any(Catalog.Messages, m => m.Advisor == advisor && Settings.Seen(m.Id));
    }
}
