#nullable enable
using System;
using System.Text.RegularExpressions;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The hex-tile map (docs/hex-tiles/README.md), opt-in while it covers only central Israel and its models
    /// are local: Web ?hex-tiles or ?hex-tiles=1, desktop -hex-tiles; a board id picks another board (Web
    /// ?hex-tiles=tel_aviv_crop, desktop -hex-tiles tel_aviv_crop). Puts the board on the real map (HexRealMap) under one
    /// "HexTiles" root beside the map's own; off with nothing applied touches nothing, so the shipped map is unchanged.
    /// Presentation only: no simulation state is read or written.</summary>
    public static class HexTilesLook
    {
        public const string RootName = "HexTiles", Board = "central_israel";
        /// <summary>The map's own dressing layers (KitLayer's VisualToggles) that the board stands in for.</summary>
        static readonly string[] DressingToggles = { "kits", "trees" };
        static readonly Regex Flag = new Regex(@"(?:^|&)hex-tiles(?:=(?<board>[a-z][a-z0-9_]*|1))?(?=&|$)", RegexOptions.CultureInvariant);

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>Web: only a top-level query parameter counts, bare or "=1" (MapLook's rule). Desktop: the exact argument.</summary>
        public static bool Requested(string? url, string[] args, bool webgl) => RequestedBoard(url, args, webgl) != null;

        /// <summary>The board asked for: null when the switch is off, else the given board id or the default board.</summary>
        public static string? RequestedBoard(string? url, string[] args, bool webgl)
        {
            if (!webgl)
            {
                int i = Array.IndexOf(args, "-hex-tiles");
                if (i < 0) return null;
                return i + 1 < args.Length && Regex.IsMatch(args[i + 1], "^[a-z][a-z0-9_]*$") ? args[i + 1] : Board;
            }
            var address = (url ?? "").Split('#')[0];
            var start = address.IndexOf('?');
            if (start < 0) return null;
            var m = Flag.Match(address.Substring(start + 1));
            if (!m.Success) return null;
            var id = m.Groups["board"].Value;
            return id == "" || id == "1" ? Board : id;
        }

        /// <summary>Removes an earlier board (which restores the real map), then builds it when <paramref name="enabled"/>.</summary>
        public static void Apply(MapScene map, bool enabled)
        {
            if (map == null) return;
            var old = map.transform.Find(RootName);
            if (old != null)
            {
                // Undo the old board now: its Destroy runs only at the frame's end, after a new board may be built.
                old.GetComponentInChildren<HexBoardAssets>()?.Teardown();
                old.SetParent(null, false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject); else UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            if (!enabled || map.Terrain == null) return;
            var holder = new GameObject(RootName).transform;
            holder.SetParent(map.transform, false);
            var id = RequestedBoard(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer) ?? Board;
            if (Resources.Load<TextAsset>(HexBoard.BoardsFolder + "/" + id) == null) { Debug.LogWarning($"hex-tiles: no board '{id}', using {Board}"); id = Board; }
            // The hex tiles replace the map's own dressing (its town kits and planted trees), which would otherwise draw
            // over them; both come back when the board is removed. Off before the board is placed, so what the kits
            // switch shows is already settled when the board hides the map's markers on its land.
            SetDressing(false);
            var (root, _) = HexRealMap.Place(map, HexBoard.Load(id), HexBoard.GroundFromResources, holder);
            root.GetComponent<HexBoardAssets>().OnTeardown(() => SetDressing(true));
            // Civ's warm, saturated diorama light while the board is up.
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            root.GetComponent<HexBoardAssets>().OnTeardown(HexLook.Apply(rig != null && rig.Volume != null ? rig.Volume.profile : null, MapLighting.FindSun()));
        }

        static void SetDressing(bool on)
        {
            foreach (var layer in DressingToggles)
                if (System.Linq.Enumerable.Contains(VisualToggles.Names, layer))   // gone once the map's KitLayer has unloaded
                    VisualToggles.Set(layer, on);
        }
    }
}
