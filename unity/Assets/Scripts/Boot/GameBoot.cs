#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The first thing Map.unity runs: loads the game inputs (fetched on WebGL, read from disk elsewhere), runs
    /// the golden check when asked (GoldenCheck), builds the map, then raises Ready for the game (GameRoot) to start.
    /// A failure is logged ("BOOT FAIL ...") and kept in Error.</summary>
    public sealed class GameBoot : MonoBehaviour
    {
        [SerializeField] MapScene map = null!;

        public GameInputs? Inputs { get; private set; }
        public string? Error { get; private set; }
        public event Action<GameInputs>? Ready;
        /// <summary>Raised with the reason when the boot fails (a failed fetch, a content error): GameRoot shows a short
        /// localised reason on the loading line instead (M3 final review I5, #637).</summary>
        public event Action<string>? Failed;

        public void Configure(MapScene mapScene) => map = mapScene;

        /// <summary>Records and logs a boot failure and raises Failed, so the player sees it (not an endless loading line).</summary>
        internal void Fail(string reason)
        {
            Error = reason;
            Debug.LogError("BOOT FAIL " + reason);
            Failed?.Invoke(reason);
        }

        async void Start()
        {
            DiagLog.Init();
            DiagLog.BootPhase("player started");
            await BootAsync(LoadAsync);
        }

        static async Awaitable<GameInputs> LoadAsync() => Application.platform == RuntimePlatform.WebGLPlayer
            ? await GameInputs.FetchAsync(GameInputs.StreamingUrl(), Application.buildGUID)
            : GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

        /// <summary>The boot Start runs, with the loader passed in so a test can drive the real failure path.</summary>
        internal async Awaitable BootAsync(Func<Awaitable<GameInputs>> load)
        {
            try
            {
                Inputs = await load();
                if (GoldenCheck.Requested())
                {
                    var (ok, lines) = GoldenCheck.Run(Inputs.Content, Inputs.Goldens);
                    foreach (var line in lines) Debug.Log(line);
                    // Desktop players run the check headless (-batchmode -nographics) and report through the exit code.
                    if (Application.platform != RuntimePlatform.WebGLPlayer) { Application.Quit(ok ? 0 : 1); return; }
                }
                map.Build(Inputs.Map);
                DiagLog.BootPhase("map built");
                Debug.Log($"BOOT OK content {Inputs.Content.Hash}");
                Ready?.Invoke(Inputs);
            }
            catch (Exception e)
            {
                Fail(e.Message);
                Debug.LogException(e);
                if (GoldenCheck.QuitCodeOnError(GoldenCheck.Requested(), Application.platform == RuntimePlatform.WebGLPlayer) is int code) Application.Quit(code);
            }
        }
    }
}
