#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The units in normal play (docs/units.md, "Units in play"): one token per unit that the sourced unit data
    /// (units_1948.yaml) and the sim's state give now (<see cref="UnitRoster"/>), each on its anchor's spot
    /// (<see cref="UnitPlacer"/>, computed once from the data), with the #139 models (and the authored-prefab hook):
    /// riflemen for a brigade, the squadron's aircraft flying a loop over its base or parked on it, a corvette at sea, and
    /// over each token the promo's badge (a dark plate with the unit's glyph, <see cref="UnitBadge"/>). Selecting a token
    /// draws the promo's white ring round it (selection only). Presentation only: it reads the state and never writes it
    /// (UnitsInPlayTests), so the goldens cannot move. Built under the map's terrain, so a map rebuild destroys it; its
    /// meshes, palette and materials are owned by its own MapRuntimeAssets, which also unregisters its "units" visual
    /// toggle (the frame profile prices it).</summary>
    public sealed class UnitLayer : MonoBehaviour
    {
        public const string RootName = "Units", ToggleName = "units";
        /// <summary>A parked aircraft's centre of gravity above the ground, in real metres (on its undercarriage).</summary>
        public const float ParkedHeightM = 1.4f;
        public const float ParkedHeadingDegrees = 200f;

        MapScene map = null!;
        IReadOnlyList<UnitEntry> units = Array.Empty<UnitEntry>();
        /// <summary>The unit data the layer draws from (read only; MapScene.UnitEntries when it read the same file).</summary>
        public IReadOnlyList<UnitEntry> Units => units;
        UnitGround ground = null!;
        IReadOnlyDictionary<UnitAnchor, AnchorSpot> spots = new Dictionary<UnitAnchor, AnchorSpot>();
        MapRuntimeAssets assets = null!;
        Material paint = null!, ringTemplate = null!, badgePlate = null!, badgeGlyph = null!, rim = null!, flagWhite = null!, flagBlue = null!;
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        /// <summary>The meshes built for the tokens drawn now (each base's draped top, lip and ring follow its own ground):
        /// destroyed when the tokens are rebuilt and with the layer, so no roster change leaks them (review of #559, M1).</summary>
        readonly List<Mesh> tokenMeshes = new List<Mesh>();
        List<UnitToken>? roster;
        /// <summary>What <see cref="Show"/> last drew from, so a Collection model's arrival can draw the tokens that waited for
        /// it (an Arab force's, issue #665).</summary>
        Content? shownContent;
        GameState? shownState;
        List<PlacedToken> placed = new List<PlacedToken>();
        Transform tokensRoot = null!;
        readonly List<Transform> badges = new List<Transform>();
        /// <summary>Each badge's home (over the highest figure's feet, at the figures' height and the badge's lift) and how
        /// far behind the trio it sits, away from the camera (review of #559: the badge keeps off the figures' heads from
        /// any view, the camera's yaw and steepness alike), its flag's pole from the middle (zero for none), the flag (null for
        /// none) and the box of the token's pieces that neither turn nor move (null for a flying aircraft alone). In the order
        /// of <see cref="badges"/> and <see cref="keepClear"/>.</summary>
        readonly List<(Vector3 home, float back, Vector3 pole, MeshFilter? flag, Bounds? standing)> badgeHomes = new List<(Vector3, float, Vector3, MeshFilter?, Bounds?)>();
        /// <summary>Each token's figures as laid out for camera yaw 0 (their place round the base's middle and their heading),
        /// the base's middle and radius, the badge's lift over the highest figure and the figures' meshes (none for an aircraft
        /// or a ship), in the order of <see cref="badges"/>. <see cref="FaceCamera"/> turns the figures and the flag's pole
        /// about the middle with the camera's yaw (issue #644): laid out for yaw 0 alone, at yaw 90 or 270 the trio stood in a
        /// column and from about yaw 180 the pole stood in front of it.</summary>
        readonly List<(Vector3 centre, float radius, float lift, (Transform figure, Vector3 offset, float heading)[] figures, MeshFilter[] meshes)> trios =
            new List<(Vector3, float, float, (Transform, Vector3, float)[], MeshFilter[])>();
        /// <summary>The camera yaw the trios were last turned to: a pan or a zoom keeps the yaw, so it moves no figure.</summary>
        float? turnedFor;
        /// <summary>A badge sits this share of its token's base radius behind the trio, seen from the camera, and
        /// <see cref="BadgeAsideM"/> to the side away from its flag's pole on screen (a compact token's pole cannot stand
        /// further out than its base's edge).</summary>
        public const float BadgeBackShare = 0.6f, BadgeAsideM = 400f, BadgeOverFlagM = 150f;
        readonly List<(Vector2 centre, float radius)> treeHoles = new List<(Vector2, float)>();
        List<Bounds[]> keepClear = new List<Bounds[]>();
        GameObject? ring;
        bool still;
        /// <summary>The camera pose <see cref="FaceCamera"/> last turned the badges and flags to; a call from the same pose
        /// changes nothing and returns at once (GameRoot calls it every frame). Cleared when the tokens are rebuilt.</summary>
        (Vector3 eye, Quaternion rotation)? facedFor;

        /// <summary>The tokens drawn now, in the data's order.</summary>
        public IReadOnlyList<PlacedToken> Tokens => placed;
        /// <summary>Every anchor's spot, from the data alone.</summary>
        public IReadOnlyDictionary<UnitAnchor, AnchorSpot> Spots => spots;
        /// <summary>The ground the anchors were placed on.</summary>
        public UnitGround PlacementGround => ground;
        /// <summary>The world boxes the map's name pills keep clear of (issue #197), one entry per drawn token: its standing
        /// pieces (the figures on their bases with the flag, a parked aircraft, the ship; not a flying aircraft, which moves) and
        /// its badge, which MapLabelsPresenter projects to one screen rectangle, so no pill sits on a token or between its badge
        /// and its figures. The badge's box and the flag's part follow them as <see cref="FaceCamera"/> turns them to a camera
        /// (in place: the boxes hold the token as that camera draws it). A new list whenever the tokens are rebuilt, and empty
        /// while the "units" toggle hides the layer.</summary>
        public IReadOnlyList<Bounds[]> KeepClear => gameObject.activeInHierarchy ? keepClear : (IReadOnlyList<Bounds[]>)Array.Empty<Bounds[]>();
        /// <summary>The selected unit's id, or null.</summary>
        public string? Selected { get; private set; }

        /// <summary>Makes the layer under the map's terrain and places every anchor once; <see cref="Show"/> draws the
        /// tokens. <paramref name="still"/> holds a flying aircraft at <see cref="UnitShowcase.StillPhase"/> (screenshots
        /// repeat).</summary>
        public static UnitLayer Create(MapScene map, IReadOnlyList<UnitEntry> units, IReadOnlyList<MapHex> hexes, bool still)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("UnitLayer needs a built map");
            var terrain = map.Terrain;
            var old = terrain.transform.Find(RootName);
            if (old != null)
            {
                foreach (var a in old.GetComponentsInChildren<MapRuntimeAssets>(true)) a.RunTeardown();
                old.SetParent(null);
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }
            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.MiniatureResource}.mat is missing (run UnitMaterialAssets.Create)");
            var ringTemplate = Resources.Load<Material>(UnitMaterials.RingResource)
                ?? throw new InvalidOperationException($"Resources/{UnitMaterials.RingResource}.mat is missing (run UnitMaterialAssets.Create)");
            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;   // children are placed in world space
            var layer = root.AddComponent<UnitLayer>();
            layer.map = map; layer.units = units; layer.still = still;
            // The same ground and the same placement as MapScene's (which keeps the kits off a token in a town).
            layer.ground = Ground(map, hexes);
            // The map's own spots when it placed these units on these hexes (issue #321: the same ground, so the same spots;
            // UnitSpotsOnceTests), with its skipped anchors' warnings; else placed here.
            if (map.UnitSpotsFor(units, hexes) is { } mapSpots)
            {
                layer.spots = mapSpots;
                foreach (var skip in UnitPlacer.SkipMessages(units, mapSpots)) Debug.LogWarning(skip);
            }
            else layer.spots = layer.ground.Place(units, XZ(map), Debug.LogWarning);
            layer.assets = root.AddComponent<MapRuntimeAssets>();
            var palette = layer.assets.Own(UnitPalette.MakeTexture(readable: false));
            layer.paint = layer.assets.Own(new Material(template) { name = "UnitMiniature (palette)" });
            layer.paint.SetTexture("_BaseMap", palette);
            layer.ringTemplate = ringTemplate;
            layer.badgePlate = layer.assets.Own(new Material(ringTemplate) { name = "UnitBadge (plate)", color = UnitBadge.PlateColour });
            layer.badgeGlyph = layer.assets.Own(new Material(ringTemplate) { name = "UnitBadge (glyph)", color = UnitBadge.GlyphColour });
            layer.rim = layer.assets.Own(new Material(ringTemplate) { name = "UnitPlinthRim", color = UnitPlinth.RimColour });
            layer.flagWhite = layer.assets.Own(new Material(ringTemplate) { name = "UnitFlag (white)", color = UnitFlags.WhiteColour });
            layer.flagBlue = layer.assets.Own(new Material(ringTemplate) { name = "UnitFlag (blue)", color = UnitFlags.BlueColour });
            layer.flagBlue.renderQueue = layer.flagWhite.renderQueue + 1;   // the blue over the white
            layer.tokensRoot = new GameObject("Tokens").transform;
            layer.tokensRoot.SetParent(root.transform, false);
            // The kit trees under the figures are left out while the layer shows (issue #249), and come back with the layer
            // off: a plain screenshot run (HiddenInCaptures) keeps every tree, so its checkpoints stay comparable.
            layer.assets.OnTeardown(() => UnitShowcase.HideTrees(map, ToggleName, Array.Empty<(Vector2, float)>()));
            layer.assets.OnTeardown(layer.FreeTokenMeshes);
            // The Collection's figures (issue #665): an Arab force's token waits for its figure, so ask for each one the map
            // can draw (nothing is fetched before the frame after the request, CollectionStream), and draw again when one arrives.
            UnitFigures.ModelArrived += layer.OnModelArrived;
            layer.assets.OnTeardown(() => UnitFigures.ModelArrived -= layer.OnModelArrived);
            foreach (var u in units)
                if (UnitRoster.OnMap(u) && u.Side != UnitData.Israel) UnitFigures.Prefetch(UnitFigures.SideFigure(u.Side)!);
            layer.assets.OnTeardown(VisualToggles.Register(ToggleName, on =>
            {
                if (root != null) root.SetActive(on);
                if (layer != null) layer.HideTrees();
            }));
            return layer;
        }

        /// <summary>True for a screenshot run (?shot, -shot) that does not also ask for the units (units, -units): the
        /// layer is then switched off, so plain art checkpoints stay comparable (review I5).</summary>
        public static bool HiddenInCaptures(string? url, string[] args, bool webgl) =>
            VisualShots.Requested(url, args, webgl) && !UnitShowcase.Requested(url, args, webgl);

        void OnEnable() => RenderPipelineManager.beginCameraRendering += FaceBadges;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= FaceBadges;

        /// <summary>The kit trees under the drawn figures, left out while the layer shows (issue #249).</summary>
        public IReadOnlyList<(Vector2 centre, float radius)> TreeHoles => treeHoles;

        void HideTrees() => UnitShowcase.HideTrees(map, ToggleName, gameObject.activeSelf ? treeHoles : (IReadOnlyList<(Vector2, float)>)Array.Empty<(Vector2, float)>());

        /// <summary>Turns every badge to face the camera about to render (game, editor capture or scene view alike) and sets
        /// it behind its trio as that camera sees it. No allocation.</summary>
        void FaceBadges(ScriptableRenderContext _, Camera cam) => FaceCamera(cam);

        /// <summary>As the render callback does: for a still render (the editor's promo captures, the tests), and for the map's
        /// names before they are placed (GameRoot), so the keep-clear boxes hold each badge and flag where this camera draws
        /// them: the badge's box is its own cube, not a box round every place the camera's yaw could take it (that box, about
        /// 3.7 km across, pushed Tel Aviv's name off its kit at gameplay03). No allocation.</summary>
        public void FaceCamera(Camera cam)
        {
            if (cam == null) return;
            var eye = cam.transform.position; var rotation = cam.transform.rotation;
            if (facedFor is { } last && last.eye == eye && last.rotation == rotation) return;   // nothing moved since the last turn
            facedFor = (eye, rotation);
            var away = cam.transform.forward; away.y = 0f;
            away = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.forward;
            var right = cam.transform.right; right.y = 0f;
            right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
            // The trio and its pole turn with the camera's yaw, so the camera always sees them as at yaw 0: three figures side
            // by side, the pole behind them (issue #644). Only a new yaw moves them; each is set on the draped top where it lands.
            // Only the game camera turns them: this runs for every camera (the scene view, a preview), and two cameras taking
            // turns would re-drape the figures every frame and leave them turned for whichever rendered last (review of #644).
            float yaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
            bool turn = cam == Camera.main && turnedFor != yaw;
            if (turn) turnedFor = yaw;
            var turning = Quaternion.Euler(0f, turnedFor ?? 0f, 0f);   // as the figures stand now (yaw 0 until first turned)
            for (int i = 0; i < badges.Count; i++)
            {
                var b = badges[i]; if (b == null) continue;
                var (home, back, pole, flag, standing) = badgeHomes[i];
                var (centre, radius, lift, figures, meshes) = trios[i];
                if (turn && figures.Length > 0)
                {
                    float highest = float.MinValue;
                    foreach (var (figure, offset, heading) in figures)
                    {
                        var p = centre + turning * offset;
                        p.y = UnitPlinth.FigureY(map, radius, p.x, p.z);
                        highest = Mathf.Max(highest, p.y);
                        figure.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw + heading, 0f));
                    }
                    home.y = highest + lift;   // the badge clears the highest figure, wherever the turn set it
                    badgeHomes[i] = (home, back, pole, flag, standing);
                    if (flag != null)
                    {
                        var foot = centre + turning * pole;
                        foot.y = UnitPlinth.FigureY(map, radius, foot.x, foot.z);
                        flag.transform.position = foot;
                    }
                }
                if (figures.Length > 0) pole = turning * pole;   // where the pole stands now, for the badge's side
                var aside = pole == Vector3.zero ? Vector3.zero : (Vector3.Dot(pole, right) > 0f ? -right : right) * BadgeAsideM;
                var at = home + away * back + aside;
                b.SetPositionAndRotation(at, rotation);
                var boxes = keepClear[i];
                boxes[0] = new Bounds(at, Vector3.one * UnitBadge.SizeM);
                if ((flag != null || meshes.Length > 0) && standing is Bounds s)
                {
                    if (flag != null)
                    {
                        UnitFlagFacing.Face(flag.transform, eye);   // as it will turn for this camera
                        s.Encapsulate(WorldBox(flag));
                    }
                    foreach (var m in meshes) s.Encapsulate(WorldBox(m));   // the figures as turned
                    boxes[1] = s;
                }
            }
        }

        /// <summary>Draws the units for <paramref name="state"/> (read only). Rebuilds the tokens only when the roster
        /// changed; returns true when it did. A selected unit that is no longer drawn is deselected.</summary>
        public bool Show(Content content, GameState state)
        {
            shownContent = content; shownState = state;
            var now = UnitRoster.Build(units, content, state, UnitFigures.Arrived);
            if (roster != null && roster.SequenceEqual(now)) return false;
            roster = now;
            placed = UnitPlacer.Place(now, spots);
            Rebuild();
            if (Selected != null && !placed.Any(p => p.Token.Unit.Id == Selected)) Select(null); else Select(Selected);
            return true;
        }

        /// <summary>A Collection model arrived: draws the tokens again if it lets a new one show (an Arab force's figure).</summary>
        void OnModelArrived(string id)
        {
            if (this == null || shownContent == null || shownState == null) return;
            Show(shownContent, shownState);
        }

        /// <summary>Where a token's vehicles stand on its base (shares of the base's radius, x east and z north, for camera
        /// yaw 0) and their headings: on the front of the base, before the trio, side-on to the camera, so the figures do
        /// not hide them (behind the trio they were hidden at 30 km; issue #665). The first on the west, a second on the east.</summary>
        internal static readonly (Vector2 at, float heading)[] VehicleSlots = { (new Vector2(-0.42f, -0.55f), 270f), (new Vector2(0.42f, -0.55f), 90f) };

        /// <summary>What a click on hex (q, r) selects: the unit standing there, unless it is already selected (a second
        /// click shows the hex, so a token never hides the hex's map actions) or the "units" toggle hides the layer; null
        /// means show the hex.</summary>
        public string? UnitToSelectAt(int q, int r)
        {
            if (!gameObject.activeInHierarchy) return null;
            var hit = At(q, r);
            return hit != null && hit.Value.Token.Unit.Id != Selected ? hit.Value.Token.Unit.Id : null;
        }

        /// <summary>The token on hex (q, r), or null.</summary>
        public PlacedToken? At(int q, int r)
        {
            foreach (var p in placed) if (p.Q == q && p.R == r) return p;
            return null;
        }

        /// <summary>Selects a drawn unit by id (the ring goes round it), or clears the selection (null).</summary>
        public void Select(string? id)
        {
            if (ring != null) { ring.transform.SetParent(null); if (Application.isPlaying) Destroy(ring); else DestroyImmediate(ring); ring = null; }
            var hit = placed.FirstOrDefault(p => p.Token.Unit.Id == id);
            Selected = id != null && hit.Token != null ? id : null;
            if (Selected == null) return;
            float hex = map.Hexes.hex_size_km * 1000f;
            ring = UnitShowcase.Ring(map, assets, ringTemplate, transform, Centre(hit.Spot), UnitShowcase.RingRadius * hex * Scale(hit.Spot));
        }

        static float Scale(AnchorSpot s) => s.Compact ? UnitGround.CompactScale : 1f;

        Vector3 Centre(AnchorSpot s) => UnitShowcase.Ground(map, new Vector3((float)s.X, 0f, (float)s.Z));

        /// <summary>The ground the units are placed on (footprints, routes, land) for a built map.</summary>
        public static UnitGround Ground(MapScene map, IReadOnlyList<MapHex> hexes) =>
            new UnitGround(map.SettlementFootprints, map.RouteCorridors, hexes, XZ(map), map.Hexes.hex_size_km * 1000f);

        static Func<double, double, Vector2> XZ(MapScene map) => (lon, lat) => { var w = map.Frame.ToWorld(lon, lat, 0); return new Vector2(w.x, w.z); };

        Mesh MeshFor(string key, Func<Mesh> build)
        {
            if (!meshes.TryGetValue(key, out var mesh))
            {
                mesh = UnitPlinth.Upload(assets.Own(build()));   // no CPU copy kept in players
                meshes[key] = mesh;
            }
            return mesh;
        }

        /// <summary>Takes a mesh built for one token: freed with the tokens (<see cref="FreeTokenMeshes"/>).</summary>
        Mesh TokenMesh(Mesh m) { tokenMeshes.Add(UnitPlinth.Upload(m)); return m; }

        void FreeTokenMeshes()
        {
            foreach (var m in tokenMeshes) if (m != null) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
            tokenMeshes.Clear();
        }

        static UnitModels.Headgear Hat(string headgear) => headgear switch
        {
            UnitData.SlouchHat => UnitModels.Headgear.SlouchHat,
            UnitData.SteelHelmet => UnitModels.Headgear.SteelHelmet,
            UnitData.WoolCap => UnitModels.Headgear.WoolCap,
            _ => throw new ArgumentException($"unknown headgear '{headgear}'", nameof(headgear)),
        };

        void Rebuild()
        {
            for (int i = tokensRoot.childCount - 1; i >= 0; i--)
            {
                var old = tokensRoot.GetChild(i).gameObject;
                old.transform.SetParent(null);
                if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }
            FreeTokenMeshes();
            badges.Clear(); badgeHomes.Clear(); facedFor = null;
            trios.Clear(); turnedFor = null;   // the new figures stand as at yaw 0
            treeHoles.Clear();
            keepClear = new List<Bounds[]>();   // a new list, so the labels see the change
            float hex = map.Hexes.hex_size_km * 1000f;
            foreach (var p in placed)
            {
                var t = p.Token;
                var go = new GameObject(t.Unit.Id);
                go.transform.SetParent(tokensRoot, false);
                var centre = Centre(p.Spot);
                var foot = centre;   // where the badge's height is measured from
                float top, plinth = 0f;
                var figures = Array.Empty<(Transform, Vector3, float)>();
                switch (t.Unit.Service)
                {
                    case UnitData.Army:
                        // Demo2 T29's token: the figures stand shoulder to shoulder on one earth base with a bright white
                        // rim, under the side's flag (refs #197). The base fits the full trio, whatever the call-up.
                        float k = UnitShowcase.DrawSpread * hex * Scale(p.Spot);
                        plinth = UnitPlinth.Radius(UnitPlinth.TrioWidth(UnitShowcase.FigureOffsets(3), k));
                        foot.y = UnitPlinth.Build(map, go.transform, centre, plinth, t.Unit.Side, TokenMesh, MeshFor, paint, rim, flagWhite, flagBlue);
                        treeHoles.Add((new Vector2(centre.x, centre.z), plinth * UnitPlinth.RimOuter));
                        var offsets = UnitShowcase.FigureOffsets(t.Figures);
                        float highest = foot.y;   // the badge clears the highest figure (on a slope the uphill one stands higher)
                        bool israel = t.Unit.Side == UnitData.Israel;
                        int vehicles = Mathf.Min(t.Vehicles.Count, VehicleSlots.Length);
                        figures = new (Transform, Vector3, float)[offsets.Length + vehicles];
                        for (int i = 0; i < offsets.Length; i++)
                        {
                            // The pose and heading are the trio position's; the headgear is the unit's (sourced, or the default).
                            var (_, _, pose, faded, heading) = UnitShowcase.Trio[i];
                            var at = centre + new Vector3(offsets[i].x, 0, offsets[i].y) * k;
                            at.y = UnitPlinth.FigureY(map, plinth, at.x, at.z);
                            highest = Mathf.Max(highest, at.y);
                            Transform figure;
                            if (israel)
                            {
                                var hat = Hat(t.Headgear[i]);
                                var type = UnitRoster.RiflemanType(t.Headgear[i]);
                                figure = UnitShowcase.Piece(go.transform, $"Rifleman{i}", type, () => MeshFor($"{type}/{pose}", () => UnitModels.Rifleman(hat, pose, faded, type)),
                                    paint, at, Quaternion.Euler(0, heading, 0), UnitShowcase.FigureScale, out _).transform;
                            }
                            else figure = EmptyPiece(go.transform, $"Figure{i}", at, heading);   // no code-built figure for another side (#740)
                            UnitFigures.Dress(figure, UnitFigures.FigureFor(t.Unit, i, israel ? t.Headgear[i] : null));   // the Collection's figure, once streamed (#665)
                            figures[i] = (figure, new Vector3(offsets[i].x, 0, offsets[i].y) * k, heading);
                        }
                        for (int j = 0; j < vehicles; j++)
                        {
                            var (slot, heading) = VehicleSlots[j];
                            var offset = new Vector3(slot.x, 0f, slot.y) * plinth;
                            var at = centre + offset;
                            at.y = UnitPlinth.FigureY(map, plinth, at.x, at.z);
                            var vehicle = EmptyPiece(go.transform, $"Vehicle{j}", at, heading);
                            UnitFigures.Dress(vehicle, t.Vehicles[j]);   // the gun or the jeep: drawn only once streamed (#665)
                            figures[offsets.Length + j] = (vehicle, offset, heading);
                        }
                        foot.y = highest;
                        top = UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
                        break;
                    case UnitData.Air:
                        BuildAircraft(go.transform, t, centre);
                        top = UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
                        break;
                    default:
                        UnitShowcase.Piece(go.transform, "Corvette", t.ArtType, () => MeshFor(t.ArtType, () => UnitModels.Corvette(t.ArtType)),
                            paint, centre, Quaternion.Euler(0, UnitShowcase.ShipHeadingDegrees, 0), UnitShowcase.ShipScale, out _);
                        top = UnitBadge.MastHeightM * UnitShowcase.ShipScale;
                        break;
                }
                var badge = new GameObject("Badge");
                badge.transform.SetParent(go.transform, false);
                // Above the figures by the badge's lift, and above the flag's top (the cloth turns with the camera, so at some yaws it
                // stands right behind the badge): the badge's lower edge clears the pole's top by BadgeOverFlagM.
                float lift = top + UnitBadge.LiftM;
                if (t.Unit.Service == UnitData.Army && go.transform.Find(UnitPlinth.FlagName) != null)
                    lift = Mathf.Max(lift, UnitFlags.PoleHeight * UnitBadge.FigureHeightM * UnitShowcase.FigureScale + UnitBadge.SizeM / 2f + BadgeOverFlagM);
                var home = foot + Vector3.up * lift;
                float back = t.Unit.Service == UnitData.Army ? BadgeBackShare * UnitPlinth.Radius(UnitPlinth.TrioWidth(UnitShowcase.FigureOffsets(3), UnitShowcase.DrawSpread * hex * Scale(p.Spot))) : 0f;
                badge.transform.position = home + Vector3.forward * back;
                badge.transform.localScale = Vector3.one * UnitBadge.SizeM;
                badge.AddComponent<UnitBadge>();
                var flagT = go.transform.Find(UnitPlinth.FlagName);
                var flag = flagT != null ? flagT.GetComponent<MeshFilter>() : null;
                var pole = flagT != null ? new Vector3(flagT.position.x - centre.x, 0f, flagT.position.z - centre.z) : Vector3.zero;
                var meshes = figures.SelectMany(f => f.Item1.GetComponentsInChildren<MeshFilter>(true)).ToArray();
                var standing = StillBox(go.transform, flag, meshes);
                badges.Add(badge.transform); badgeHomes.Add((home, back, pole, flag, standing));
                trios.Add((centre, plinth, lift, figures, meshes));
                AddRenderer(badge.transform, "Plate", MeshFor("badge-plate", UnitBadge.PlateMesh), badgePlate, 0f);
                AddRenderer(badge.transform, "Glyph", MeshFor("badge-" + t.Unit.Service, () => UnitBadge.GlyphMesh(t.Unit.Service)), badgeGlyph, -UnitBadge.GlyphLift);
                // The badge's cube and the pieces (the flag and the figures as they stand now); FaceCamera moves both with the camera.
                var pieces = standing;
                if (pieces is Bounds s) { if (flag != null) s.Encapsulate(WorldBox(flag)); foreach (var m in meshes) s.Encapsulate(WorldBox(m)); pieces = s; }
                keepClear.Add(pieces is Bounds p2 ? new[] { new Bounds(badge.transform.position, Vector3.one * UnitBadge.SizeM), p2 }
                    : new[] { new Bounds(badge.transform.position, Vector3.one * UnitBadge.SizeM) });
            }
            FaceCamera(Camera.main);   // the badges as the main camera sees them until a render turns them
            HideTrees();
        }

        /// <summary>The world box of a mesh's bounds through its transform. From the mesh and the transform, not
        /// Renderer.bounds, which is empty while the layer is inactive (the "units" toggle off when the tokens are built;
        /// review of #518).</summary>
        static Bounds WorldBox(MeshFilter f)
        {
            var b = f.sharedMesh.bounds; var m = f.transform.localToWorldMatrix; var box = new Bounds(m.MultiplyPoint3x4(b.min), Vector3.zero);
            for (int c = 1; c < 8; c++)
                box.Encapsulate(m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z)));
            return box;
        }

        /// <summary>The box of a token's pieces that neither turn nor move (a flying aircraft, the badge, and the flag and the
        /// figures, which turn with the camera, are left out), or null when it has none.</summary>
        static Bounds? StillBox(Transform token, MeshFilter? flag, MeshFilter[] figures)
        {
            Bounds? box = null;
            foreach (var f in token.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || f == flag || Array.IndexOf(figures, f) >= 0 || f.GetComponentInParent<UnitBadge>(true) != null || f.GetComponentInParent<ShowcaseFlight>(true) != null) continue;
                var w = WorldBox(f);
                if (box is Bounds b) { b.Encapsulate(w); box = b; } else box = w;
            }
            return box;
        }

        /// <summary>A piece with no code-built model (an Arab force's figure, a vehicle): the Collection's model goes under it.</summary>
        static Transform EmptyPiece(Transform parent, string name, Vector3 at, float heading)
        {
            var piece = new GameObject(name).transform;
            piece.SetParent(parent, false);
            piece.SetPositionAndRotation(at, Quaternion.Euler(0, heading, 0));
            piece.localScale = Vector3.one * UnitShowcase.FigureScale;
            return piece;
        }

        static void AddRenderer(Transform parent, string name, Mesh mesh, Material mat, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void BuildAircraft(Transform parent, UnitToken t, Vector3 baseCentre)
        {
            var plane = new GameObject("Aircraft");
            plane.transform.SetParent(parent, false);
            bool spitfire = t.ArtType == UnitShowcase.SpitfireType;
            Func<Mesh> airframe = spitfire ? (Func<Mesh>)(() => UnitModels.Spitfire(t.ArtType))
                : () => UnitModels.AviaS199(t.ArtType);
            UnitShowcase.Piece(plane.transform, "Airframe", t.ArtType, () => MeshFor(t.ArtType, airframe), paint, Vector3.zero, Quaternion.identity, 1f, out bool authored);
            Transform? prop = null;
            if (!authored)
            {
                var propKey = spitfire ? "spitfire-propeller" : "avia-s199-propeller";
                var propMesh = MeshFor(propKey, spitfire ? (Func<Mesh>)(() => UnitModels.SpitfirePropeller(propKey)) : () => UnitModels.S199Propeller(propKey));
                prop = UnitShowcase.Piece(plane.transform, "Propeller", propMesh, paint, Vector3.zero, Quaternion.identity, 1f).transform;
                prop.localPosition = spitfire ? UnitModels.SpitfirePropellerHub : UnitModels.S199PropellerHub;
            }
            plane.transform.localScale = Vector3.one * UnitShowcase.AircraftScale;
            if (t.Flying)
            {
                var flight = plane.AddComponent<ShowcaseFlight>();
                flight.Configure(baseCentre, UnitShowcase.LoopRadiusM, UnitShowcase.LoopFloor(map, baseCentre, UnitShowcase.LoopRadiusM) + UnitShowcase.AltitudeM,
                    UnitShowcase.LoopSeconds, UnitShowcase.BankDegrees, prop, still ? UnitShowcase.StillPhase : (float?)null);
            }
            else
                plane.transform.SetPositionAndRotation(baseCentre + Vector3.up * ParkedHeightM * UnitShowcase.AircraftScale,
                    Quaternion.Euler(0, ParkedHeadingDegrees, 0));
        }
    }
}
