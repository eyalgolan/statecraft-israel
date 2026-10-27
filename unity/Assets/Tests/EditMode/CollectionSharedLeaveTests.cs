#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEngine;
public class CollectionSharedLeaveTests
{
    sealed class Source : CollectionStream.ISource
    {
        public int Unloads;
        public GameObject Prefab = null!;
        public IEnumerator Open(string bundle, Action<string?> done) { done(null); yield break; }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done) { done(Prefab); yield break; }
        public void Unload(string bundle) { Unloads++; }
    }
    [Test] public void RoadNearPinBlocksUnloadWhenTheUnitFarLeaseLeaves()
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var source = new Source { Prefab = prefab }; var stream = new CollectionStream(source);
        string id = UnitFigures.SandwichArmouredCar;
        stream.Enqueue(id, CollectionDetail.Near, _ => { });
        var unit = stream.Retain(id, CollectionDetail.Far, _ => { });
        var stack = new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(stream.Run());
        for (int frame = 0; frame < 20 && stack.Count > 0;)
        { var top = stack.Peek(); if (!top.MoveNext()) stack.Pop(); else if (top.Current is IEnumerator nested) stack.Push(nested); else frame++; }
        unit.Dispose(); UnityEngine.Object.DestroyImmediate(prefab); Assert.AreEqual(0, source.Unloads, "retained road consumer owns the original bundle pin");
    }
}
