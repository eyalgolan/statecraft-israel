#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>The advisors' lines in the settings file (spec 15; SettingsStore: presentation only, never the sim or the
    /// save): the help level, the messages seen (one line, advisor_seen=a,b,c) and the opening chain's mark. The marks
    /// are also kept in memory, so a failed write (logged) or no file at all (automated runs) only costs persistence.</summary>
    public sealed class AdvisorSettings
    {
        public const string LevelKey = "advisor_level", SeenKey = "advisor_seen", OpeningKey = "advisor_opening";
        /// <summary>The first-turn guide's line (spec 9.3: honoured, so a player who saw the guide gets no chain by itself).</summary>
        public const string GuideSeenKey = "first_turn_guide";
        /// <summary>Spec 6, from the chattiest; a message shows when its level's index (AdvisorCatalog.Levels) is at least this one's.</summary>
        public static readonly string[] Levels = { "new", "experienced", "warnings", "off" };

        readonly SettingsStore? store;
        readonly SortedSet<string> seen = new SortedSet<string>(StringComparer.Ordinal);
        bool openingSeen;

        public AdvisorSettings(SettingsStore? store)
        {
            this.store = store;
            var stored = store?.Read(LevelKey);
            Level = Array.IndexOf(Levels, stored) >= 0 ? stored! : Levels[0];
            foreach (var id in (store?.Read(SeenKey) ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) seen.Add(id);
            openingSeen = store?.Read(OpeningKey) == "seen" || store?.Read(GuideSeenKey) == "seen";
        }

        /// <summary>A settings file exists to remember the marks in (none in automated runs and EditMode tests).</summary>
        public bool HasFile => store != null;
        public string Level { get; private set; }
        public int LevelRank => Array.IndexOf(Levels, Level);

        public void SetLevel(string level)
        {
            if (Array.IndexOf(Levels, level) < 0) throw new ArgumentException("unknown help level: " + level, nameof(level));
            Level = level;
            Write(LevelKey, level);
        }

        public bool Seen(string id) => seen.Contains(id);

        public void MarkSeen(string id)
        {
            if (!seen.Add(id)) return;
            Write(SeenKey, string.Join(",", seen));
        }

        public bool OpeningSeen => openingSeen;

        public void MarkOpeningSeen()
        {
            openingSeen = true;
            Write(OpeningKey, "seen");
        }

        void Write(string key, string value)
        {
            if (store == null) return;
            try { store.Write(key, value); }
            catch (Exception e) { Debug.LogWarning($"The advisors could not save {key}: {e.Message}"); }
        }
    }
}
