#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Playtest 2, P2-4: map clicks under same-frame input.</summary>
public class MapPressTests
{
    /// <summary>MapPointer evaluates every press at its own position, in order, even when several land in one frame.</summary>
    [Test]
    public void EveryMapPressIsCheckedAtItsOwnPosition()
    {
        var go = new GameObject("P24Pointer");
        try
        {
            var pointer = go.AddComponent<MapPointer>();
            var asked = new List<Vector2>();
            pointer.IsOverUi = p => { asked.Add(p); return p.y > 1000; };   // the top of the screen is UI
            pointer.OnPress(new Vector2(1908, 1151));
            pointer.OnPress(new Vector2(1630, 317));
            pointer.ProcessPresses();
            CollectionAssert.AreEqual(new[] { new Vector2(1908, 1151), new Vector2(1630, 317) }, asked, "both presses, each where it happened");
            asked.Clear();
            pointer.ProcessPresses();
            Assert.IsEmpty(asked, "a press is handled once");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Review of the touchscreen tap rule: a press released inside the slop is a tap, at where the finger landed.</summary>
    [Test]
    public void ATapSelectsWhereTheFingerLanded()
    {
        var tap = new TapPress();
        Assert.IsNull(tap.Step(true, false, new Vector2(1150, 501), false), "nothing while the finger is down");
        Assert.AreEqual((Vector2?)new Vector2(1150, 501), tap.Step(false, true, new Vector2(1152, 500), false),
            "a 2-pixel wiggle is still a tap, and it selects where the finger landed");
    }

    /// <summary>A finger that wanders past the slop drags the camera and never selects - even if it comes back.</summary>
    [Test]
    public void ADragIsNeverATapEvenOneThatComesBack()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(400, 400), false);
        tap.Step(false, false, new Vector2(600, 400), false);   // far past the slop
        Assert.IsNull(tap.Step(false, true, new Vector2(401, 399), false), "back at the start is still a drag");
    }

    /// <summary>A pinch is never a tap: with the thumb held still, lifting it after the other finger spread must not select.</summary>
    [Test]
    public void APinchIsNeverATapEvenWithOneFingerHeldStill()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(1150, 501), false);    // the thumb lands
        tap.Step(false, false, new Vector2(1150, 501), true);    // a second finger goes down; the thumb stays put
        Assert.IsNull(tap.Step(false, true, new Vector2(1150, 501), true), "lifting the thumb after a pinch is not a tap");
    }

    /// <summary>A finger that goes down only after the watched one lifted changes nothing.</summary>
    [Test]
    public void ASecondFingerAfterTheTapChangesNothing()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(300, 300), false);
        Assert.AreEqual((Vector2?)new Vector2(300, 300), tap.Step(false, true, new Vector2(300, 300), false));
    }
}
