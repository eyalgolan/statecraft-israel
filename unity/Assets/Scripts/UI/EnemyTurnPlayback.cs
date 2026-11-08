#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>What the replay needs of the map (GameRoot adapts UnitLayer, the attack presenter and the hexes).</summary>
    public interface IPlaybackMap
    {
        /// <summary>Holds these sim units' tokens where they are drawn (UnitLayer.Hold).</summary>
        void Hold(IEnumerable<int> ids);
        /// <summary>Lets a held token slide to its place (UnitLayer.Release); false when it was not held.</summary>
        bool Release(int id);
        /// <summary>Releases every held token: each slides, or (<paramref name="snap"/>) stands on its place at once.</summary>
        void ReleaseAll(bool snap);
        /// <summary>Plays engagement lines and their Destroyed lines as UnitAttackPresenter plays them at End turn: the
        /// attackers' fire take, the hit on the hex, the defenders' reaction and the dust.</summary>
        void Fight(IReadOnlyList<Cause> lines);
        /// <summary>Whether a token draws this sim unit now (UnitLayer.Draws).</summary>
        bool Draws(int id);
        /// <summary>Whether the units are shown (the "units" toggle, the capture runs' rule).</summary>
        bool UnitsShown { get; }
        /// <summary>The ground point of hex (q, r), or null for a hex the map does not have.</summary>
        Vector3? World(int q, int r);
        /// <summary>A hex's size in metres (the return glide's pacing, CameraGlide.Seconds).</summary>
        float HexMetres { get; }
    }

    /// <summary>What the replay needs of the camera (GameRoot adapts CameraRig and CameraController.Settle).</summary>
    public interface IPlaybackCamera
    {
        Vector3 LookAt { get; set; }
        float Distance { get; }
        /// <summary>Whether the point is well inside the map's view now (CameraGlide.InView).</summary>
        bool InView(Vector3 point);
        /// <summary>Holds the view on the map, as after a pan (CameraController.Settle).</summary>
        void Settle();
    }

    /// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 5): after End turn, the map plays
    /// the enemy's moves and attacks one beat at a time, in the sim's order, and the camera glides to each beat and back to
    /// the player's view. Presentation only: the sim has already resolved the turn, and nothing here changes the state, the
    /// save or a rule. The owner's answers (2 November 2026, "Q1-Q7 recommended, Q8 yes"; 8 November 2026, "All moves, as
    /// approved (Recommended)"): every enemy move and attack plays, on screen or off (Q2 a); the turn report and an event card
    /// wait for it (Q1 a, <see cref="Holding"/>); the player's zoom is kept (Q5 a).
    /// <para>Never modal (#2070: "the game never waits on an animation to accept input"): it only holds tokens and moves the
    /// look-at point. A player's pan or zoom ends its camera work for the turn (CameraGlide.TookOver). A plain class, ticked by
    /// GameRoot.Update, so EditMode tests drive it with <see cref="Tick"/>. At End turn it does one pass over the turn's lines
    /// (UnitBeats.From) and holds the tokens; the rest is spread over frames (#2905).</para>
    /// <para>The seam for #2544 PR 2 (an operation's moves and attacks after the decision): the beats come from a
    /// <see cref="BeatSource"/>, so another reader of a step's lines gets the same holds, pacing, camera and Skip.</para></summary>
    public sealed class EnemyTurnPlayback : IDisposable
    {
        enum Phase { Pan, Act, Return }

        readonly SimBridge bridge;
        readonly IPlaybackMap map;
        readonly IPlaybackCamera camera;
        readonly Func<bool> enabled, waiting;
        readonly BeatSource source;
        readonly Action<StepResult> onStepped;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly Action onChanged;

        IReadOnlyList<UnitBeat> beats = Array.Empty<UnitBeat>();
        GameSession? session;      // the game the beats belong to
        bool pending;              // beats are held and wait for Start
        Phase phase;
        float t, length;
        Vector3 from, to, saved;   // the glide's ends, and the player's view at the start
        bool retreated;            // the beat's retreaters were released
        int acted = -1;            // the last beat whose units were released or fought
        bool wrote, tookOver;      // the playback moved the camera; the player moved it since
        Vector3 lastLookAt;
        float lastDistance;

        /// <param name="enabled">Read at each End turn: the Settings switch, and never in a smoke run, a capture or
        /// EditMode unless a test says so.</param>
        /// <param name="waiting">True while something else holds the screen (an End turn outcome clip): the playback starts
        /// once it is false.</param>
        /// <param name="source">Which lines open a beat (the enemy's moves and attacks by default).</param>
        public EnemyTurnPlayback(SimBridge bridge, IPlaybackMap map, IPlaybackCamera camera, Func<bool> enabled, Func<bool> waiting,
            BeatSource? source = null)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
            this.enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
            this.waiting = waiting ?? throw new ArgumentNullException(nameof(waiting));
            this.source = source ?? BeatSource.Enemy;
            onStepped = Stepped;
            onTurn = _ => Start();
            onChanged = () => { if (session != null && !ReferenceEquals(bridge.Session, session)) Drop(); };
            bridge.Stepped += onStepped;
            bridge.TurnEnded += onTurn;
            bridge.Changed += onChanged;
        }

        /// <summary>True while the beats play (from <see cref="Start"/> to the end, Skip or Finish).</summary>
        public bool Active { get; private set; }
        /// <summary>True from End turn until the playback starts: the tokens are held and the report waits.</summary>
        public bool Pending => pending;
        /// <summary>The beat playing now (0-based), or -1.</summary>
        public int Index { get; private set; } = -1;
        public int Count => beats.Count;
        public IReadOnlyList<UnitBeat> Beats => beats;
        public UnitBeat? Current => Active && Index >= 0 && Index < beats.Count ? beats[Index] : null;
        /// <summary>The player moved the camera during this playback: it no longer steers it, nor returns it.</summary>
        public bool TookOver => tookOver;

        public event Action? Started, BeatChanged, Finished;
        /// <summary>True when a turn's beats are held for the playback, false when it ends (the turn report and the event
        /// card wait while it is true: owner, Q1 a).</summary>
        public event Action<bool>? Holding;

        void Stepped(StepResult r)
        {
            // End turn again while one plays: the old one ends at once, and only the new turn's moves play. When the new turn
            // has moves, the hold goes on from the old playback to the new one (no Holding false in between): the waiting
            // report opens once, for the new turn, when its moves end, never over them (#2967 review, M1).
            var made = enabled() && map.UnitsShown && r.State.GameOver == null   // the end screen is over the map
                ? UnitBeats.From(r.Why, source) : Array.Empty<UnitBeat>();
            End(release: made.Count == 0);
            if (made.Count == 0) return;
            beats = made; session = bridge.Session; pending = true; acted = -1;
            map.Hold(made.SelectMany(b => b.Units).Distinct().ToList());   // before the map is rebuilt (Changed)
            Holding?.Invoke(true);
        }

        /// <summary>A cause in a beat that has not played yet: UnitAttackPresenter leaves it for its beat.</summary>
        public bool Claims(Cause c)
        {
            if (!Active && !pending) return false;
            for (int i = acted + 1; i < beats.Count; i++)
                foreach (var x in beats[i].Causes) if (ReferenceEquals(x, c)) return true;
            return false;
        }

        /// <summary>Begins the held beats, unless something else holds the screen (GameRoot calls it again when the outcome
        /// clips close; <see cref="Tick"/> also tries).</summary>
        public void Start()
        {
            if (!pending || waiting()) return;
            pending = false; Active = true; Index = -1; wrote = tookOver = false;
            saved = camera.LookAt;
            Started?.Invoke();
            Next();
        }

        public void Tick(float dt)
        {
            if (pending) Start();
            if (!Active) return;
            if (dt > 0f) t += dt;
            if (wrote && !tookOver && CameraGlide.TookOver(camera.LookAt, camera.Distance, lastLookAt, lastDistance)) tookOver = true;
            switch (phase)
            {
                case Phase.Pan:
                    if (!tookOver) Steer(CameraGlide.At(from, to, length > 0f ? t / length : 1f));
                    if (tookOver || t >= length) Act();
                    break;
                case Phase.Act:
                    var b = beats[Index];
                    if (!retreated && t >= length * 0.5f) ReleaseRetreats(b);
                    if (t >= length) Next();
                    break;
                case Phase.Return:
                    if (!tookOver) Steer(CameraGlide.At(from, saved, length > 0f ? t / length : 1f));
                    if (tookOver || t >= length) Stop();
                    break;
            }
        }

        /// <summary>Ends the playback now (the Skip button, Esc, the switch turned off): every held token stands on its
        /// place, and the camera is back on the player's view at once unless the player moved it. True when one was on.</summary>
        public bool Skip() => End(release: true);

        /// <param name="release">False when another playback follows at once (End turn again): the report and the card stay
        /// held.</param>
        bool End(bool release)
        {
            if (!Active && !pending) return false;
            if (Active && wrote && !tookOver) { camera.LookAt = saved; camera.Settle(); }
            Stop(release);   // every held token stands on its place
            return true;
        }

        /// <summary>As <see cref="Skip"/>: End turn again, or the UI rebuilt (a language switch).</summary>
        public void Finish() => Skip();

        /// <summary>Another game (New Game, a load): its tokens are drawn where they stand, and its camera is its own.</summary>
        void Drop() => Stop();

        void Next()
        {
            while (++Index < beats.Count)
            {
                var b = beats[Index];
                // A move beat whose units are all gone (destroyed later in the turn) has nothing to show: the next one plays.
                if (b.Engagements == 0 && !b.Moves.Any(m => map.Draws(m.Unit))) { acted = Index; continue; }
                t = 0f; phase = Phase.Pan; length = 0f;
                var focus = b.Folded || tookOver ? null : map.World(b.FocusQ, b.FocusR);
                bool pan = focus is Vector3 p && !camera.InView(p);
                BeatChanged?.Invoke();
                if (pan) { from = camera.LookAt; to = focus!.Value; length = b.Pan; }
                else Act();
                return;
            }
            Index = beats.Count - 1;
            if (!wrote || tookOver) { Stop(); return; }
            phase = Phase.Return; t = 0f; from = camera.LookAt;
            float dx = saved.x - from.x, dz = saved.z - from.z;
            float metres = Mathf.Sqrt(dx * dx + dz * dz);
            if (metres <= CameraGlide.StillM) { Stop(); return; }
            length = CameraGlide.Seconds(metres, map.HexMetres);
        }

        void Act()
        {
            var b = beats[Index];
            acted = Index; phase = Phase.Act; t = 0f;
            length = (b.Slides ? UnitTokenMotion.Duration : 0f) + b.Dwell;
            foreach (var m in b.Moves) map.Release(m.Unit);
            if (b.Engagements > 0)
                map.Fight(b.Causes.Where(c => c.Key == WhyKeys.Engagement || c.Key == WhyKeys.Destroyed).ToList());
            retreated = false;
            // A folded beat plays everything at once; an engagement's retreaters slide off once its hit has landed.
            if (b.Folded || b.Retreats.Count == 0) ReleaseRetreats(b);
        }

        void ReleaseRetreats(UnitBeat b)
        {
            retreated = true;
            foreach (var r in b.Retreats) map.Release(r.Unit);
        }

        void Steer(Vector3 p)
        {
            camera.LookAt = p;
            camera.Settle();
            lastLookAt = camera.LookAt; lastDistance = camera.Distance; wrote = true;
        }

        void Stop(bool release = true)
        {
            bool was = Active || pending;
            Active = false; pending = false; Index = -1; acted = -1; session = null;
            beats = Array.Empty<UnitBeat>();
            if (!was) return;
            map.ReleaseAll(snap: true);   // anything still held (a beat left out) stands on its place
            if (release) Holding?.Invoke(false);
            Finished?.Invoke();
        }

        public void Dispose()
        {
            Finish();
            bridge.Stepped -= onStepped;
            bridge.TurnEnded -= onTurn;
            bridge.Changed -= onChanged;
        }
    }

    /// <summary>The map as the playback sees it in the game: UnitLayer's holds, UnitAttackPresenter's fights and the map's
    /// hexes. Kept here, not in GameRoot, so GameRoot only builds it.</summary>
    public sealed class UnitLayerPlaybackMap : IPlaybackMap
    {
        readonly UnitLayer layer; readonly UnitAttackPresenter attacks; readonly MapScene map; readonly SimBridge bridge;

        public UnitLayerPlaybackMap(UnitLayer layer, UnitAttackPresenter attacks, MapScene map, SimBridge bridge)
        {
            this.layer = layer; this.attacks = attacks; this.map = map; this.bridge = bridge;
        }

        public void Hold(IEnumerable<int> ids) { if (layer != null) layer.Hold(ids); }
        public bool Release(int id) => layer != null && layer.Release(id);
        public void ReleaseAll(bool snap) { if (layer != null) layer.ReleaseAll(snap); }
        public void Fight(IReadOnlyList<Cause> lines) => attacks.Play(lines);
        public bool Draws(int id) => layer != null && layer.Draws(id);
        /// <summary>The "units" toggle switches the layer's object (UnitLayer.ToggleName), and a capture run turns it off.</summary>
        public bool UnitsShown => layer != null && layer.gameObject.activeInHierarchy;
        public Vector3? World(int q, int r)
        {
            var hex = bridge.Content.HexAt(q, r);
            return hex == null || map == null ? (Vector3?)null : map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        }
        public float HexMetres => map != null && map.Hexes != null ? map.Hexes.hex_size_km * 1000f : 0f;
    }

    /// <summary>The camera as the playback sees it in the game: the rig's look-at point and CameraController.Settle, as
    /// GameRoot.FocusUnit moves it.</summary>
    public sealed class RigPlaybackCamera : IPlaybackCamera
    {
        readonly CameraRig rig; readonly CameraController controller; readonly Action? moved;

        /// <param name="moved">After each settle (the map labels follow the camera).</param>
        public RigPlaybackCamera(CameraRig rig, CameraController controller, Action? moved = null)
        {
            this.rig = rig; this.controller = controller; this.moved = moved;
        }

        public Vector3 LookAt { get => rig.LookAt; set => rig.LookAt = value; }
        public float Distance => rig.Distance;
        public bool InView(Vector3 point) => CameraGlide.InView(rig, point);
        public void Settle() { controller.Settle(); moved?.Invoke(); }
    }
}
