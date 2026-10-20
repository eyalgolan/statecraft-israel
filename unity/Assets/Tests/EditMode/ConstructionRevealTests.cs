#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class ConstructionRevealTests
{
    // Reflection keeps the red test executable before the opt-in component exists.
    static Type RevealType => typeof(Statecraft.Game.Map.PlacementLayer).Assembly.GetType("Statecraft.Game.Map.Construction.ConstructionReveal")!;

    [Test]
    public void RevealIsAvailableWithoutLoadingTheGame()
    {
        Assert.That(RevealType, Is.Not.Null, "a standalone construction reveal must exist");
    }

    [Test]
    public void SeededParticlesAreRepeatableBoundedAndDoNotConsumeGlobalRandom()
    {
        Assert.That(RevealType, Is.Not.Null, "a standalone construction reveal must exist");
        var root = new GameObject("test root"); var visual = new GameObject("visual");
        try
        {
            visual.transform.SetParent(root.transform, false); root.transform.position = new Vector3(30, 4, 20);
            var fx = root.AddComponent(RevealType);
            void Call(string name, params object[] args) => RevealType.GetMethod(name)!.Invoke(fx, args);
            Call("Bind", visual.transform, 2f);
            var random = UnityEngine.Random.state;
            Call("Play"); Call("Sample", 0.65f);
            Assert.That(UnityEngine.Random.state, Is.EqualTo(random), "visual seed never consumes the sim/global RNG");
            var systems = root.GetComponentsInChildren<ParticleSystem>(); Assert.That(systems.Length, Is.EqualTo(2));
            var before = new ParticleSystem.Particle[44]; int count = systems[0].GetParticles(before);
            Assert.That(count, Is.InRange(1, 36));
            foreach (var ps in systems)
            {
                var particles = new ParticleSystem.Particle[44]; int n = ps.GetParticles(particles);
                Assert.That(n, Is.LessThanOrEqualTo(ps.main.maxParticles));
                for (int i = 0; i < n; i++)
                {
                    var d = particles[i].position - root.transform.position;
                    Assert.That(new Vector2(d.x, d.z).magnitude + particles[i].startSize * 0.5f, Is.LessThan(4.5f));
                    Assert.That(d.y, Is.InRange(0f, 1f), "dust remains near ground");
                }
            }
            Call("Play"); Call("Sample", 0.65f);
            var after = new ParticleSystem.Particle[44]; Assert.That(systems[0].GetParticles(after), Is.EqualTo(count));
            for (int i = 0; i < count; i++) Assert.That(after[i].position, Is.EqualTo(before[i].position));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void DestroyAndRebindRestoreTheOldTargetAndReleaseOwnedMaterial()
    {
        Assert.That(RevealType, Is.Not.Null, "a standalone construction reveal must exist");
        var root = new GameObject("root"); var a = new GameObject("a"); var b = new GameObject("b");
        a.transform.SetParent(root.transform, false); b.transform.SetParent(root.transform, false);
        try
        {
            var fx = root.AddComponent(RevealType);
            void Call(string name, params object[] args) => RevealType.GetMethod(name)!.Invoke(fx, args);
            Call("Bind", a.transform, 2f); Call("Play"); Call("Sample", 0.2f);
            var material = root.GetComponentInChildren<ParticleSystemRenderer>()!.sharedMaterial;
            Call("Bind", b.transform, 1f); Assert.That(a.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(a.transform.localPosition, Is.EqualTo(Vector3.zero));
            Call("Play"); Call("Sample", 0.2f); Object.DestroyImmediate(fx);
            Assert.That(b.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(b.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(material == null, Is.True, "owned material is released");
            Assert.That(root.GetComponentsInChildren<ParticleSystem>().Length, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void ReplayCancelAndDisableRestoreVisualAndKeepPickingStable()
    {
        Assert.That(RevealType, Is.Not.Null, "a standalone construction reveal must exist");
        var root = new GameObject("pick root");
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            visual.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var pick = root.AddComponent<BoxCollider>();
            visual.transform.localPosition = new Vector3(2, 0, 1);
            visual.transform.localScale = new Vector3(2, 3, 2);
            var position = visual.transform.localPosition; var scale = visual.transform.localScale;
            var fx = root.AddComponent(RevealType);
            void Call(string name, params object[] args) => RevealType.GetMethod(name)!.Invoke(fx, args);
            Call("Bind", visual.transform, 4f);
            for (int i = 0; i < 12; i++)
            {
                Call("Play"); Call("Sample", 0.6f);
                Assert.That(pick.enabled, Is.True);
                Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                Call("Cancel");
                Assert.That(visual.transform.localPosition, Is.EqualTo(position));
                Assert.That(visual.transform.localScale, Is.EqualTo(scale));
            }
            Assert.That(root.GetComponentsInChildren<ParticleSystem>(true).Length, Is.EqualTo(2), "replay reuses its two systems");
            Call("Play"); Call("Sample", 0.5f); ((Behaviour)fx).enabled = false;
            Assert.That(visual.transform.localPosition, Is.EqualTo(position));
            Assert.That(visual.transform.localScale, Is.EqualTo(scale));
            ((Behaviour)fx).enabled = true;
            Call("Play"); Call("Sample", 100f);
            Assert.That(visual.transform.localScale, Is.EqualTo(scale));
            Assert.That(root.GetComponentsInChildren<ParticleSystem>(true)[0].particleCount, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
