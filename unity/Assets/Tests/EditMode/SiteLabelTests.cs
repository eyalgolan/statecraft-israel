#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>GDD Phase 1 PR 8 (#381): a built site's name is a map pill like a town's (MapLabelsPresenter.SetSites): placed and
/// decluttered by the same rules, clear of every other pill and of the panels, and gone when the site is no longer built.</summary>
public class SiteLabelTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    MapScene map = null!;
    Camera cam = null!;

    [SetUp]
    public void Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        cam = Camera.main;
    }

    [TearDown]
    public void Close() { cam.targetTexture = null; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static Settlement Mefalsim()
    {
        var site = C.Sites.Single(s => s.Id == "mefalsim");
        var hex = C.HexAt(site.Q, site.R)!;
        return new Settlement { Id = SiteLayer.Prefix + site.Id, Name = site.Name, NameHe = site.NameHe, Type = "kibbutz", Lon = hex.Lon, Lat = hex.Lat };
    }

    static bool Shown(VisualElement slot) => slot.style.visibility.value == Visibility.Visible;

    [Test]
    public void ABuiltSitesNameIsAPillClearOfTheOthersAndGoesWithIt()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var site = Mefalsim();
        var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(site.Lon, site.Lat, 0); rig.Distance = 40000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();

        labels.SetSites(new[] { (site, (Bounds[]?)null) });
        labels.Refresh(force: true);
        var slot = ui.Root.Q<VisualElement>("slot-" + site.Id);
        Assert.IsNotNull(slot, "the site has a pill");
        Assert.AreEqual("Mefalsim", slot.Q<Label>().text);
        Assert.IsTrue(Shown(slot), "close over the site, its pill shows");
        var rect = slot.Q<Label>().worldBound;
        foreach (var other in ui.Root.Query<VisualElement>(className: MapLabelsPresenter.SlotClass).ToList().Where(s => s != slot && Shown(s)))
            Assert.IsFalse(rect.Overlaps(other.Q<Label>().worldBound), $"the site's pill {rect} overlaps {other.name} {other.Q<Label>().worldBound}");

        labels.SetSites(System.Array.Empty<(Settlement, Bounds[]?)>());
        Assert.IsNull(ui.Root.Q<VisualElement>("slot-" + site.Id), "no pill once the site is not built");
    }
}
