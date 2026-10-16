#nullable enable
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>#690 option C: the dune coasts' one definition (Resources/Map/DuneCoasts.json) and its hand-over to the water
/// pass.</summary>
public class DuneCoastsTests
{
    [TearDown]
    public void Reset() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void TheFileGivesJaffaSouthAndHaifaBay()
    {
        var bands = DuneCoasts.Bands;
        Assert.AreEqual(2, bands.Length);
        Assert.AreEqual(32.07, bands[0].lat1, 1e-9, "Jaffa south: full south of 32.03 N, gone by 32.07 N");
        Assert.AreEqual(0.04, bands[0].ramp, 1e-9);
        Assert.AreEqual(1f, DuneCoasts.WeightAt(34.70, 31.95), 1e-6f, "south of Jaffa");
        Assert.AreEqual(0f, DuneCoasts.WeightAt(34.78, 32.10), 1e-6f, "the kurkar coast north of Tel Aviv");
        Assert.AreEqual(0.5f, DuneCoasts.WeightAt(34.70, 32.05), 1e-6f, "half way across the ramp");
        Assert.AreEqual(1f, DuneCoasts.WeightAt(35.08, 32.85), 1e-6f, "Haifa Bay");
    }

    [Test]
    public void AMissingOrBrokenFileGivesNoBandsAndAnError()
    {
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("DuneCoasts: .* missing"));
        Assert.IsEmpty(DuneCoasts.Parse(""));
        Assert.IsEmpty(DuneCoasts.Parse("{\"bands\": []}"));
    }

    [Test]
    public void TheWaterPassGetsTheBandsInMapMetres()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        Assert.AreEqual(2f, Shader.GetGlobalFloat("_ScDuneCount"));
        var rects = Shader.GetGlobalVectorArray("_ScDuneRect"); var ramps = Shader.GetGlobalVectorArray("_ScDuneRamp");
        var b = DuneCoasts.Bands[0];
        Vector3 sw = map.Frame.ToWorld(b.lon0, b.lat0, 0f), ne = map.Frame.ToWorld(b.lon1, b.lat1, 0f);
        Assert.AreEqual(new Vector4(sw.x, ne.x, sw.z, ne.z), rects[0]);
        float rampZ = (float)(b.ramp / (map.Frame.Lat1 - map.Frame.Lat0) * map.Frame.LengthM);
        Assert.AreEqual(rampZ, ramps[0].y, 0.5f, "the ramp north-south, in metres");
    }
}
