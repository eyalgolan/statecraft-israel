#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#1661 item 1 only: a city-pill hit carries its intent through HandleClick and the real GameRoot
/// subscription. This fixture deliberately does not drive Update, input devices or scheduling (contributor items 4–6).</summary>
public class OccupiedCityPillTests
{
    static readonly GameInputs Inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static void Send(object instance, string method) => instance.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, null);

    sealed class Rig : IDisposable
    {
        public readonly MapScene Map;
        public readonly GameRoot Root;
        public readonly MapPointer Pointer;
        public readonly CameraController Controller;
        public readonly CameraRig View;
        readonly Camera camera;
        readonly PanelSettings panel;
        readonly RenderTexture panelTarget, cameraTarget;
        readonly string saves = Path.Combine(Path.GetTempPath(), "statecraft-city-pill-" + Guid.NewGuid().ToString("N"));

        public Rig()
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var doc = Object.FindFirstObjectByType<UIDocument>();
            camera = Camera.main!;
            panelTarget = new RenderTexture(1920, 1080, 0);
            cameraTarget = new RenderTexture(1920, 1080, 24);
            panel = Object.Instantiate(doc.panelSettings);
            panel.targetTexture = panelTarget; doc.panelSettings = panel; camera.targetTexture = cameraTarget;
            Map = Object.FindFirstObjectByType<MapScene>(); Map.Build(Inputs.Map);
            Root = Object.FindFirstObjectByType<GameRoot>(); Root.Store = new SaveStore(saves); Root.Begin(Inputs, 0);
            GameSceneTests.HideTheOpeningCard(doc.rootVisualElement);
            Pointer = Object.FindFirstObjectByType<MapPointer>();
            Controller = camera.GetComponent<CameraController>(); View = camera.GetComponent<CameraRig>();
            Send(Controller, "Awake");   // initialise projection for the badge hit-test, no Update or Tick calls
            Pointer.IsOverUi = _ => false;   // isolate routing from layout; no fake click/command/event subscriber
        }

        public void Select(string row)
        {
            Root.Units!.Select(row); Root.Info!.ShowUnit();   // initial state, not the gesture under test
            Assert.IsTrue(Root.Info.MovePicking, "precondition: a movable unit is selected");
        }

        public Vector2 Centre(int q, int r)
        {
            var h = Inputs.Content.HexAt(q, r)!;
            var ground = Map.Frame.ToWorld(h.Lon, h.Lat, 0);
            View.LookAt = ground; View.Distance = 60000f; View.Yaw = 0f; Controller.Settle();
            ground.y = Map.Terrain.SampleHeight(ground);
            var screen = camera.WorldToScreenPoint(ground);
            return new Vector2(screen.x, screen.y);
        }

        public void Dispose()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // As EndTurnCycleSceneTests' TearDown: the scene change can already have unloaded these, and Unity's == is
            // false for a destroyed object, so each is released only while it still exists.
            if (panel != null) Object.DestroyImmediate(panel);
            if (panelTarget != null) { panelTarget.Release(); Object.DestroyImmediate(panelTarget); }
            if (cameraTarget != null) { cameraTarget.Release(); Object.DestroyImmediate(cameraTarget); }
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
        }
    }

    [UnityTest]
    public IEnumerator AnOccupiedCityPillShowsTheCityAndTheBadgeStillSelectsItsUnit()
    {
        using var rig = new Rig();
        yield return null;
        var bridge = rig.Root.Bridge!; var c = bridge.Content; var layer = rig.Root.Units!;
        var a = layer.Tokens.Single(t => t.Token.Unit.Id == "golani");
        var aId = UnitCard.SimUnit(c, bridge.State, a.Token.Unit)!.Value;
        var b = layer.Tokens.First(t => t.Token.Unit.Side == "israel"
            && UnitCard.SimUnit(c, bridge.State, t.Token.Unit) is int id && id != aId);
        var bId = UnitCard.SimUnit(c, bridge.State, b.Token.Unit)!.Value;
        var city = c.SettlementHexes.First(h => bridge.State.Cities.ContainsKey(h.Id) && layer.At(h.Q, h.R) == null);
        // Valid occupied-city fixture, before any commit. Rebuild tokens from that state, as the live bridge does.
        Assert.IsEmpty(bridge.Session.Committed, "precondition: no preview commands before fixture placement");
        Assert.IsFalse(bridge.Session.State.Units.Values.Any(u => u.Order == Movement.GoTo));
        var u = bridge.Session.State.Units[bId];   // Unit is immutable: put B on the city as UnitOrdersTests does
        bridge.Session.State.Units[bId] = new Unit(u.Id, u.Type, city.Q, city.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
        layer.Show(c, bridge.State);
        rig.Select("golani");
        var at = rig.Centre(city.Q, city.R);
        // Controlled hit-test result for the pill; MapPointer must carry that origin through its event and GameRoot.
        // The separate rendered city-pill test checks MapLabelsPresenter.PillAt itself.
        rig.Pointer.PillHex = _ => (city.Q, city.R);
        var before = bridge.Session.Committed.Count;
        Assert.IsTrue(rig.Pointer.HandleClick(at), "the pill hit enters the actual scene subscription");
        Assert.IsFalse(rig.Root.Info!.ShowsUnit, "the occupied city's pill must not select B");
        Assert.AreEqual((city.Q, city.R), rig.Root.Info.Selected);
        Assert.IsNull(layer.Selected);
        Assert.AreEqual(before, bridge.Session.Committed.Count);

        rig.Select("golani");
        rig.Pointer.PillHex = null;
        // Pick an actual projected point on B's badge/figures, not the city pill.
        var placed = layer.Tokens.Single(t => t.Token.Unit.Id == b.Token.Unit.Id);
        var cam = Camera.main!;
        var badge = (Vector2?)null;
        for (float y = at.y - 80; y <= at.y + 80 && badge == null; y += 4)
            for (float x = at.x - 80; x <= at.x + 80; x += 4)
                if (layer.TokenAtScreen(cam, new Vector2(x, y)) is { } hit && hit.Token.Unit.Id == placed.Token.Unit.Id)
                { badge = new Vector2(x, y); break; }
        Assert.IsNotNull(badge, "precondition: the occupant has a projected badge/figure hit point");
        Assert.IsTrue(rig.Pointer.HandleClick(badge!.Value), "a badge hit enters the same scene subscription");
        Assert.AreEqual(b.Token.Unit.Id, layer.Selected, "a badge still selects B");
        Assert.IsTrue(rig.Root.Info.ShowsUnit);
        Assert.AreEqual(before, bridge.Session.Committed.Count);
    }

}
