#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The "under attack" look (HexUnderAttack): presentation only, shown on one hex and hidden again. It sits on
/// the hex centre and stays inside the hex at every time, shows once however often it is shown, frees every mesh and
/// material it made when hidden, samples the same pulse and smoke for the same time, and ships its material templates.</summary>
public class HexUnderAttackTests
{
    const int Q = -3, R = 19;
    readonly List<GameObject> roots = new List<GameObject>();

    [TearDown] public void DestroyRoots() { foreach (var r in roots) if (r != null) Object.DestroyImmediate(r); roots.Clear(); }

    Transform Root() { var go = new GameObject("HexBoard"); roots.Add(go); return go.transform; }

    /// <summary>Raised ground, so the look must take its height from the surface.</summary>
    sealed class Raised : IHexSurface
    {
        public float Height(Vector3 p) => 80f + p.x * 0.004f;
        public float SlopeDeg(Vector3 p) => 0.2f;
        public bool IsWater(Vector3 p) => false;
    }

    static (MeshFilter[] meshes, Renderer[] renderers) Parts(GameObject go) =>
        (go.GetComponentsInChildren<MeshFilter>(true), go.GetComponentsInChildren<Renderer>(true));

    [Test]
    public void ShowSitsOnTheHexCentreAndStaysInsideIt()
    {
        var root = Root();
        var surface = new Raised();
        var go = HexUnderAttack.Show(root, surface, Q, R);
        var c = HexGeometry.Centre(Q, R, HexScale.HexM);
        Assert.That(go.transform.parent, Is.SameAs(root));
        Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(c.x, surface.Height(c), c.z)), Is.LessThan(0.01f));
        var look = go.GetComponent<HexUnderAttackLook>();
        Assert.That(look, Is.Not.Null, "the look animates through its component");
        foreach (var t in new[] { 0f, 0.7f, 2.9f, 5.3f, 11.1f, 47.5f })
        {
            look.Time(t);
            var (meshes, _) = Parts(go);
            Assert.That(meshes.Count(m => m.sharedMesh != null && m.sharedMesh.vertexCount > 0), Is.GreaterThanOrEqualTo(3), "smoke, glow and border");
            foreach (var mf in meshes)
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = root.InverseTransformPoint(mf.transform.TransformPoint(v));
                    Assert.That(HexGeometry.InHex(p, c, HexScale.HexM), $"{mf.name} vertex {p} at t={t} is outside hex {Q},{R}");
                }
        }
    }

    [Test]
    public void ShowTwiceLeavesOne()
    {
        var root = Root();
        HexUnderAttack.Show(root, new FlatSurface(), Q, R);
        HexUnderAttack.Show(root, new FlatSurface(), Q, R);
        Assert.That(root.Cast<Transform>().Count(t => t.name == HexUnderAttack.Name(Q, R)), Is.EqualTo(1));
    }

    [Test]
    public void HideRemovesItAndReleasesWhatItMade()
    {
        var root = Root();
        var go = HexUnderAttack.Show(root, new FlatSurface(), Q, R);
        go.GetComponent<HexUnderAttackLook>().Time(1.5f);
        var (meshes, renderers) = Parts(go);
        var materials = renderers.SelectMany(r => r.sharedMaterials).ToList();
        var made = meshes.Select(m => (Object)m.sharedMesh).Concat(materials)
            .Concat(materials.Select(m => m.GetTexture("_BaseMap")).Where(t => t != null)).ToList();
        Assert.That(made.Count, Is.GreaterThanOrEqualTo(6), "three meshes and three materials at least");
        Assert.That(made.All(o => o != null));
        Assert.That(HexUnderAttack.Hide(root, Q, R), Is.True);
        Assert.That(made.Where(o => o != null).Select(o => o.name), Is.Empty, "every mesh and material the look made is destroyed");
        Assert.That(HexUnderAttack.Hide(root, Q, R), Is.False);
    }

    [Test]
    public void IsShownFollowsShowAndHide()
    {
        var root = Root();
        Assert.That(HexUnderAttack.IsShown(root, Q, R), Is.False);
        HexUnderAttack.Show(root, new FlatSurface(), Q, R);
        Assert.That(HexUnderAttack.IsShown(root, Q, R), Is.True);
        Assert.That(HexUnderAttack.IsShown(root, Q + 1, R), Is.False);
        HexUnderAttack.Hide(root, Q, R);
        Assert.That(HexUnderAttack.IsShown(root, Q, R), Is.False);
    }

    /// <summary>What a sampled look draws: the border colour and every mesh's vertices and colours.</summary>
    static List<object> Sample(GameObject go)
    {
        var (meshes, renderers) = Parts(go);
        var s = new List<object>();
        s.AddRange(renderers.SelectMany(r => r.sharedMaterials).Select(m => (object)m.GetColor("_BaseColor")));
        foreach (var mf in meshes.OrderBy(m => m.name)) { s.AddRange(mf.sharedMesh.vertices.Cast<object>()); s.AddRange(mf.sharedMesh.colors32.Cast<object>()); }
        return s;
    }

    [Test]
    public void ThePulseAndTheSmokeAreDeterministicForAGivenTime()
    {
        var a = HexUnderAttack.Show(Root(), new FlatSurface(), Q, R).GetComponent<HexUnderAttackLook>();
        var b = HexUnderAttack.Show(Root(), new FlatSurface(), Q, R).GetComponent<HexUnderAttackLook>();
        a.Time(3.25f); b.Time(0.4f); b.Time(3.25f);
        CollectionAssert.AreEqual(Sample(a.gameObject), Sample(b.gameObject));
        Assert.That(HexUnderAttack.Pulse(1.1f), Is.EqualTo(HexUnderAttack.Pulse(1.1f)));
        Assert.That(HexUnderAttack.Pulse(0f), Is.Not.EqualTo(HexUnderAttack.Pulse(HexUnderAttack.PulsePeriodS * 0.5f)).Within(0.2f), "it pulses");
        Assert.That(Enumerable.Range(0, 40).Select(i => HexUnderAttack.Pulse(i * 0.137f)).All(p => p >= 0f && p <= 1f));
        var before = Sample(a.gameObject);
        a.Time(4.1f);
        CollectionAssert.AreNotEqual(before, Sample(a.gameObject), "the smoke and the pulse move with time");
    }

    [Test]
    public void TheSmokeIsSeededByTheHex()
    {
        var a = HexUnderAttack.SmokeBases(Q, R, HexScale.HexM);
        CollectionAssert.AreEqual(a, HexUnderAttack.SmokeBases(Q, R, HexScale.HexM));
        CollectionAssert.AreNotEqual(a, HexUnderAttack.SmokeBases(Q + 1, R, HexScale.HexM));
        Assert.That(a.Length, Is.InRange(2, 4));
        // On every hex: outside the unit spot, and in the north half, so (seen from the south, as the board camera sees
        // it) no column stands in front of the unit or the city's centre.
        for (int q = -6; q <= 6; q++)
            for (int r = -6; r <= 24; r++)
                foreach (var p in HexUnderAttack.SmokeBases(q, r, HexScale.HexM))
                {
                    Assert.That(new Vector2(p.x, p.z).magnitude, Is.GreaterThan(HexScale.UnitSpotShare * HexScale.HexM), $"hex {q},{r}: smoke base {p}");
                    Assert.That(p.z, Is.GreaterThanOrEqualTo(0f), $"hex {q},{r}: smoke base {p} is in the south half");
                }
    }

    [Test]
    public void TheTemplatesShipInResources()
    {
        foreach (var name in HexUnderAttack.Materials)
        {
            var m = Resources.Load<Material>(HexMaterials.Folder + "/" + name);
            Assert.That(m, Is.Not.Null, $"Resources/{HexMaterials.Folder}/{name}.mat (Statecraft/Hex Tiles/Make Materials)");
            Assert.That(m.shader.isSupported, name);
            Assert.That(m.renderQueue, Is.GreaterThanOrEqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent), $"{name} is transparent");
        }
    }
}
