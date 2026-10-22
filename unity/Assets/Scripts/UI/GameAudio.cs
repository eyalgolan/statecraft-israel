#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>The one place that plays the game's sound effects (#39): a short clip per cue, at the volume the Settings
    /// screen sets. It listens to the hooks itself: End Turn (SimBridge.TurnEnded; a turn with refused orders sounds as a
    /// refusal), an order refused when it is given (SimBridge.Refused) and an event card coming into view
    /// (EventCardPresenter.Opened, which waits for the start screen to close, so on the web the first sound comes after the
    /// player's first click, as browsers require). The clips are AI-generated under Resources/Audio; ASSETS.yaml records their provenance and pending publication rights. A missing clip
    /// plays nothing: sound is never worth a failed start. Presentation only: the sim and the golden hashes never see it.
    /// Ben-Gurion's recording on the start screen keeps its own player (DeclarationRecording).</summary>
    public sealed class GameAudio : IDisposable
    {
        public enum Cue { TurnEnded, EventCard, Refused }

        /// <summary>The clips' folder under Resources, and each cue's file name there.</summary>
        public const string Folder = "Audio";
        public static readonly IReadOnlyDictionary<Cue, string> Files = new Dictionary<Cue, string>
        { { Cue.TurnEnded, "turn-ended" }, { Cue.EventCard, "event-card" }, { Cue.Refused, "refused" } };

        /// <summary>Where a clip plays. The game's is an AudioSource on its own GameObject (SourceOutput).</summary>
        public interface IOutput : IDisposable { void Play(AudioClip clip, float volume); }

        readonly Dictionary<Cue, AudioClip?> clips = new Dictionary<Cue, AudioClip?>();
        readonly SimBridge bridge; readonly EventCardPresenter? card; readonly IOutput output;
        readonly Action<IReadOnlyList<Rejection>> onTurnEnded; readonly Action<string> onRefused, onOpened;
        float volume = 1f;

        /// <summary>0 (silent) to 1 (the clips as recorded); SettingsPresenter sets it from the player's choice.</summary>
        public float Volume { get => volume; set => volume = Mathf.Clamp01(value); }

        /// <param name="card">The event card whose coming into view has its cue; null for none.</param>
        /// <param name="load">Test seam: a clip by its Resources path; null loads it from Resources.</param>
        /// <param name="output">Test seam; null plays through an AudioSource.</param>
        public GameAudio(SimBridge bridge, EventCardPresenter? card, Func<string, AudioClip?>? load = null, IOutput? output = null)
        {
            this.bridge = bridge; this.card = card;
            this.output = output ?? new SourceOutput();
            load ??= path => Resources.Load<AudioClip>(path);
            foreach (var cue in Files)
            {
                var clip = load(Folder + "/" + cue.Value);
                if (clip == null) Debug.LogWarning($"AUDIO no clip at Resources/{Folder}/{cue.Value}: the {cue.Key} cue is silent");
                clips[cue.Key] = clip;
            }
            onTurnEnded = refused => Play(refused.Count > 0 ? Cue.Refused : Cue.TurnEnded);
            onRefused = _ => Play(Cue.Refused);
            onOpened = _ => Play(Cue.EventCard);
            bridge.TurnEnded += onTurnEnded;
            bridge.Refused += onRefused;
            if (card != null) card.Opened += onOpened;
        }

        /// <summary>Plays the cue's clip once, over anything already playing; nothing at volume 0 or with no clip.</summary>
        public void Play(Cue cue)
        {
            if (volume <= 0f || !clips.TryGetValue(cue, out var clip) || clip == null) return;
            output.Play(clip, volume);
        }

        public void Dispose()
        {
            bridge.TurnEnded -= onTurnEnded;
            bridge.Refused -= onRefused;
            if (card != null) card.Opened -= onOpened;
            output.Dispose();
        }

        /// <summary>An AudioSource on its own GameObject, made at the first sound (none in a run that never plays one).</summary>
        sealed class SourceOutput : IOutput
        {
            GameObject? go; AudioSource? source;

            public void Play(AudioClip clip, float volume)
            {
                if (go == null)
                {
                    go = new GameObject("GameAudio");
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.spatialBlend = 0f;
                }
                source!.PlayOneShot(clip, volume);
            }

            public void Dispose()
            {
                if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
                go = null; source = null;
            }
        }
    }
}
