using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Supplied synthetic enemy combatant observation, not a surrender command or own-person record.
    /// SurrenderId is opaque provenance shared by survivors; EpisodeId identifies this cohort's observation.
    /// No geography, formation-strength conversion, equipment, return state or award policy is inferred.</summary>
    internal sealed class SyntheticCaptivityEpisode
    {
        internal string EpisodeId { get; }
        internal string EnemyCohortId { get; }
        internal string SurrenderId { get; }
        internal string OriginalSideId { get; }
        internal string CaptorId { get; }

        // R-POW-001: producer must eventually establish surrender/combatant eligibility. This only validates references.
        internal SyntheticCaptivityEpisode(string episodeId, string enemyCohortId, string surrenderId,
            string originalSideId, string captorId)
        {
            foreach (var value in new[] { episodeId, enemyCohortId, surrenderId, originalSideId, captorId })
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Explicit synthetic captivity references required.");
                PopulationText.ValidateText(value);
            }
            EpisodeId = episodeId; EnemyCohortId = enemyCohortId; SurrenderId = surrenderId;
            OriginalSideId = originalSideId; CaptorId = captorId;
        }
        internal string FixtureText() => PopulationText.Fields(EpisodeId, EnemyCohortId, SurrenderId, OriginalSideId, CaptorId);
    }

    /// <summary>Frozen synthetic observations only; no GameState owner or campaign persistence.
    /// Several episodes for a cohort are observations, not multiple currently held captives or score entitlements.
    /// Own captured units remain exclusively in PopulationLedger. No production initialization hook exists.</summary>
    internal sealed class SyntheticCaptivityRecords
    {
        internal IReadOnlyList<SyntheticCaptivityEpisode> Episodes { get; }
        SyntheticCaptivityRecords(IEnumerable<SyntheticCaptivityEpisode> input)
        {
            var episodes = new SortedDictionary<string, SyntheticCaptivityEpisode>(StringComparer.Ordinal);
            var provenance = new HashSet<string>(StringComparer.Ordinal);
            foreach (var episode in input)
            {
                if (episode == null) throw new ArgumentException("Null captivity observation.", nameof(input));
                if (episodes.ContainsKey(episode.EpisodeId)
                    || !provenance.Add(PopulationText.Fields(episode.SurrenderId, episode.EnemyCohortId)))
                    throw new ArgumentException("Duplicate episode identity or surrender/cohort provenance.", nameof(input));
                episodes.Add(episode.EpisodeId, episode);
            }
            Episodes = new ReadOnlyCollection<SyntheticCaptivityEpisode>(episodes.Values.ToList());
        }
        internal static SyntheticCaptivityRecords CreateFixture(Content content, IEnumerable<SyntheticCaptivityEpisode> episodes)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.Ruleset != SaveGame.Gdd03Ruleset)
                throw new InvalidOperationException("Synthetic captivity records require exact gdd-0.3 identity.");
            if (episodes == null) throw new ArgumentNullException(nameof(episodes));
            return new SyntheticCaptivityRecords(episodes);
        }
        // Fixture comparison only, never appended to GameState.Describe/Hash or the save replay envelope.
        // PopulationText.Fields length-prefixes all strings with InvariantCulture and rejects malformed UTF-16.
        internal string FixtureText() => PopulationText.Fields(Episodes.Select(x => (object)x.FixtureText()).ToArray());
    }
}
