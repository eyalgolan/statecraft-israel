#nullable enable
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEngine;

/// <summary>Spec 7.1's slot, as plain numbers: the right column's upper part on one and two rows, a sheet under the bar on
/// an upright phone; and Show me's move off a target (spec 7.3).</summary>
public class AdvisorLayoutTests
{
    static Rect R(float x, float y, float w, float h) => new Rect(x, y, w, h);

    [Test]
    public void OneRowAt1920x1080IsSpecSevenOnesSlot()
    {
        var window = R(0, 0, 1920, 1080);
        var column = R(1424, 112, 480, 952);          // right: 16, top: 112, bottom: 16, width: 480
        var minimap = R(1682, 684, 222, 380);         // 380 tall at the column's foot
        var slot = AdvisorLayout.Slot(HudTier.OneRow, window, column, R(16, 112, 56, 600), minimap, null, null, null, null);
        Assert.AreEqual(Rect.MinMaxRect(1504, 166, 1904, 672), slot);
    }

    [Test]
    public void TheTopNeverJumpsWhenDecisionWaitingAppears()
    {
        var window = R(0, 0, 1920, 1080); var column = R(1424, 112, 480, 952); var minimap = R(1682, 684, 222, 380);
        var without = AdvisorLayout.Slot(HudTier.OneRow, window, column, R(16, 112, 56, 600), minimap, null, null, null, null);
        var with = AdvisorLayout.Slot(HudTier.OneRow, window, column, R(16, 112, 56, 600), minimap, R(1700, 112, 204, 44), null, null, null);
        Assert.AreEqual(without, with);
    }

    [Test]
    public void AnUprightPhoneGetsASheetUnderTheBar()
    {
        var window = R(0, 0, 1080, 2340);   // 390x844 in panel px at match 0.5 is about this shape; the numbers are what matter
        var column = R(584, 456, 480, 1868); var left = R(16, 456, 60, 900);
        var minimap = R(842, 1944, 222, 380); var label = R(600, 1900, 464, 30);
        var waiting = R(860, 456, 204, 44); var toast = R(88, 456, 460, 60); var info = R(16, 1700, 800, 400);
        var slot = AdvisorLayout.Slot(HudTier.ThreeRow, window, column, left, minimap, waiting, toast, info, label);
        Assert.AreEqual(88f, slot.xMin, "left: 88px, the left column's edge and 12 px");
        Assert.AreEqual(1064f, slot.xMax, "right: 16px");
        Assert.AreEqual(524f, slot.yMin, "under the toast (the lower of it and Decision waiting) and 8 px");
        Assert.AreEqual(1688f, slot.yMax, "12 px above the higher of the info panel and the minimap's label");
    }

    [Test]
    public void PlaceKeepsThePopupInItsSlotAndOffATarget()
    {
        var slot = Rect.MinMaxRect(1504, 166, 1904, 672);
        Assert.AreEqual(Rect.MinMaxRect(1504, 166, 1904, 366), AdvisorLayout.Place(slot, 200, null), "at the slot's top");
        Assert.AreEqual(Rect.MinMaxRect(1504, 472, 1904, 672), AdvisorLayout.Place(slot, 200, R(1600, 200, 100, 40)), "a target under it: to the slot's foot");
        Assert.AreEqual(Rect.MinMaxRect(1504, 166, 1904, 366), AdvisorLayout.Place(slot, 200, R(1600, 600, 100, 40)), "a target below it: stays");
        Assert.AreEqual(Rect.MinMaxRect(1504, 166, 1904, 672), AdvisorLayout.Place(slot, 900, null), "never taller than its slot (the text scrolls)");
    }

    [Test]
    public void ADegenerateSlotGivesAnEmptyPopupInsideIt()
    {
        var slot = Rect.MinMaxRect(1504, 300, 1904, 200);   // a short window: the foot sits above the top, so the height is negative
        var placed = AdvisorLayout.Place(slot, 200, null);
        Assert.AreEqual(0f, placed.height);
        Assert.AreEqual(slot.yMin, placed.yMin);
    }

    [Test]
    public void WhenTopAndFootBothMeetTheTargetThePopupStaysAtTheTop()
    {
        var slot = Rect.MinMaxRect(1504, 166, 1904, 372);
        Assert.AreEqual(Rect.MinMaxRect(1504, 166, 1904, 366), AdvisorLayout.Place(slot, 200, R(1600, 200, 100, 150)));
    }
}
