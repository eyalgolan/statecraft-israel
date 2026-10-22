#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>#1009 step 2: the Unity map editor round-trips the one override file through the sim's parser/serializer, does not
/// touch an unchanged file, rejects invalid moves before save, and records edits with Unity Undo.</summary>
public sealed class MapOverrideEditorTests
{
    string temporary = "";

    [SetUp]
    public void SetUp()
    {
        temporary = Path.Combine(Path.GetTempPath(), "statecraft-map-editor-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(Paths.Content, temporary);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
    }

    [Test]
    public void LoadEditSaveAndReloadHaveTheSameData()
    {
        var document = MapOverrideEditorDocument.Load(temporary);
        MapOverrideEditorDocument? reloaded = null;
        try
        {
            var hex = document.Hexes.First(x => x.Land);
            var terrain = document.LandBiomes.First(x => x != hex.Biome);
            var location = document.Locations.OrderBy(x => x.Id, StringComparer.Ordinal).First();
            var target = document.Hexes.First(x => x.Land && (x.Q != location.BaseQ || x.R != location.BaseR));
            var controlHex = Control1948.Parse(File.ReadAllText(Path.Combine(temporary, "map", Control1948.FileName))).EditorHexes.First();
            var period = document.Periods[0].Id;
            var control = document.ControlValues.First(x => x != document.ControlAt(controlHex.q, controlHex.r, period));

            document.SetTerrain(hex.Q, hex.R, terrain);
            document.SetControl(controlHex.q, controlHex.r, period, control);
            document.SetLocation(location.Id, target.Q, target.R);
            Assert.IsTrue(document.Save(out var error), error);

            reloaded = MapOverrideEditorDocument.Load(temporary);
            Assert.AreEqual(terrain, reloaded.TerrainAt(hex.Q, hex.R));
            Assert.AreEqual(control, reloaded.ControlAt(controlHex.q, controlHex.r, period));
            Assert.AreEqual((target.Q, target.R), reloaded.LocationAt(location.Id));
            Assert.AreEqual(MapOverrides.Serialize(document.ToOverrides()), MapOverrides.Serialize(reloaded.ToOverrides()));
        }
        finally
        {
            if (reloaded != null) UnityEngine.Object.DestroyImmediate(reloaded);
            UnityEngine.Object.DestroyImmediate(document);
        }
    }

    [Test]
    public void SavingUnchangedDataDoesNotRewriteTheFile()
    {
        var path = Path.Combine(temporary, MapOverrides.FileName);
        var before = File.ReadAllBytes(path);
        var beforeWrite = File.GetLastWriteTimeUtc(path);
        var document = MapOverrideEditorDocument.Load(temporary);
        try
        {
            Assert.IsFalse(document.HasUnsavedChanges);
            Assert.IsTrue(document.Save(out var error), error);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            Assert.AreEqual(beforeWrite, File.GetLastWriteTimeUtc(path), "an unchanged save does not touch the file");
        }
        finally { UnityEngine.Object.DestroyImmediate(document); }
    }

    [Test]
    public void UnityUndoAndRedoRestoreEveryEditKind()
    {
        var document = MapOverrideEditorDocument.Load(temporary);
        try
        {
            var hex = document.Hexes.First(x => x.Land);
            var terrainBefore = document.TerrainAt(hex.Q, hex.R);
            var terrainAfter = document.LandBiomes.First(x => x != terrainBefore);
            var location = document.Locations.OrderBy(x => x.Id, StringComparer.Ordinal).First();
            var before = document.LocationAt(location.Id);
            var target = document.Hexes.First(x => x.Land && (x.Q != before.q || x.R != before.r));
            var controlHex = Control1948.Parse(File.ReadAllText(Path.Combine(temporary, "map", Control1948.FileName))).EditorHexes.First();
            var period = document.Periods[0].Id;
            var controlBefore = document.ControlAt(controlHex.q, controlHex.r, period);
            var controlAfter = document.ControlValues.First(x => x != controlBefore);
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(document, "Edit map overrides");
            document.SetTerrain(hex.Q, hex.R, terrainAfter);
            document.SetControl(controlHex.q, controlHex.r, period, controlAfter);
            document.SetLocation(location.Id, target.Q, target.R);
            Undo.FlushUndoRecordObjects();
            Assert.AreEqual(terrainAfter, document.TerrainAt(hex.Q, hex.R));
            Assert.AreEqual(controlAfter, document.ControlAt(controlHex.q, controlHex.r, period));
            Assert.AreEqual((target.Q, target.R), document.LocationAt(location.Id));
            Undo.PerformUndo();
            Assert.AreEqual(terrainBefore, document.TerrainAt(hex.Q, hex.R));
            Assert.AreEqual(controlBefore, document.ControlAt(controlHex.q, controlHex.r, period));
            Assert.AreEqual(before, document.LocationAt(location.Id));
            Undo.PerformRedo();
            Assert.AreEqual(terrainAfter, document.TerrainAt(hex.Q, hex.R));
            Assert.AreEqual(controlAfter, document.ControlAt(controlHex.q, controlHex.r, period));
            Assert.AreEqual((target.Q, target.R), document.LocationAt(location.Id));
        }
        finally
        {
            Undo.ClearUndo(document);
            UnityEngine.Object.DestroyImmediate(document);
        }
    }

    [Test]
    public void InvalidInputIsClearAndLeavesTheSavedFileIntact()
    {
        var path = Path.Combine(temporary, MapOverrides.FileName);
        var before = File.ReadAllBytes(path);
        var document = MapOverrideEditorDocument.Load(temporary);
        try
        {
            var location = document.Locations.First();
            var water = document.Hexes.First(x => !x.Land);
            var error = Assert.Throws<ArgumentException>(() => document.SetLocation(location.Id, water.Q, water.R));
            StringAssert.Contains("not a land hex", error!.Message);
            error = Assert.Throws<ArgumentException>(() => document.SetLocation("unknown_location", water.Q, water.R));
            StringAssert.Contains("unknown location id", error!.Message);
            Assert.IsFalse(document.HasUnsavedChanges);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
        }
        finally { UnityEngine.Object.DestroyImmediate(document); }
    }

    [Test]
    public void ALocationMoveThatBreaksASiteCannotReplaceTheSavedMap()
    {
        var path = Path.Combine(temporary, MapOverrides.FileName);
        var before = File.ReadAllBytes(path);
        var content = LoadSavedContent();
        var site = content.Sites.First();
        var location = content.SettlementHexes.First();
        var document = MapOverrideEditorDocument.Load(temporary);
        try
        {
            document.SetLocation(location.Id, site.Q, site.R);
            Assert.IsFalse(document.Save(out var error), "a settlement cannot occupy a designated site's hex");
            StringAssert.Contains(site.Id, error, "the failure identifies the affected dependent record");
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path), "the last valid saved map stays intact");
            Assert.DoesNotThrow(() => { LoadSavedContent(); }, "the saved map still loads in the game");
        }
        finally { UnityEngine.Object.DestroyImmediate(document); }
    }

    [Test]
    public void MovingACorridorEndpointReportsTheCorridorAndKeepsItsTracedHexes()
    {
        // A corridor is bound to hexes, not to a location id (corridors.yaml; ContentLoader.ValidateCorridors), so moving the
        // location at its end cannot break loading: the move saves, the dependency is reported and no hex is rewritten.
        var path = Path.Combine(temporary, MapOverrides.FileName);
        var content = LoadSavedContent();
        var corridor = content.Corridors.Single(x => x.Id == "tlv_netanya_road");
        var tracedBefore = corridor.Segments.SelectMany(x => x.Hexes).Select(h => (h[0], h[1])).ToList();
        var end = corridor.Segments.Last().Hexes.Last();
        var occupied = content.SettlementHexes.Select(x => (x.Q, x.R)).Concat(content.MapLocations.Select(x => (x.Q, x.R)))
            .Concat(content.Sites.Select(x => (x.Q, x.R))).ToHashSet();
        var document = MapOverrideEditorDocument.Load(temporary);
        try
        {
            var location = document.Locations.OrderBy(x => x.Id, StringComparer.Ordinal).First(x => x.BaseQ == end[0] && x.BaseR == end[1]);
            var target = document.Hexes.First(x => x.Land && !occupied.Contains((x.Q, x.R)));
            StringAssert.Contains("tlv_netanya_road", string.Join("\n", document.Dependencies(location.Id).Corridors),
                "the corridor that ends on the location is reported before saving");
            document.SetLocation(location.Id, target.Q, target.R);
            Assert.IsTrue(document.Save(out var error), error);
            var saved = LoadSavedContent();
            CollectionAssert.AreEqual(tracedBefore, saved.Corridors.Single(x => x.Id == corridor.Id).Segments
                .SelectMany(x => x.Hexes).Select(h => (h[0], h[1])).ToList(), "the corridor keeps its traced hexes");
            var moved = saved.SettlementHexes.Select(x => (x.Id, x.Q, x.R)).Concat(saved.MapLocations.Select(x => (x.Id, x.Q, x.R)))
                .Single(x => x.Id == location.Id);
            Assert.AreEqual((target.Q, target.R), (moved.Q, moved.R), "the game reads the moved location");
            StringAssert.Contains(location.Id, File.ReadAllText(path));
        }
        finally { UnityEngine.Object.DestroyImmediate(document); }
    }

    [Test]
    public void UnsavedEditsSurviveADomainReload()
    {
        // A domain reload keeps a document's serialized fields and drops the rest; EditorJsonUtility does the same round trip.
        var document = MapOverrideEditorDocument.Load(temporary);
        var restored = ScriptableObject.CreateInstance<MapOverrideEditorDocument>();
        try
        {
            var hex = document.Hexes.First(x => x.Land);
            var terrain = document.LandBiomes.First(x => x != hex.Biome);
            document.SetTerrain(hex.Q, hex.R, terrain);
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(document), restored);
            Assert.IsFalse(restored.IsLoaded, "the map read from disk is not serialized");
            restored.Restore();
            Assert.IsTrue(restored.IsLoaded);
            Assert.AreEqual(terrain, restored.TerrainAt(hex.Q, hex.R), "the unsaved edit survives");
            Assert.IsTrue(restored.HasUnsavedChanges, "and still counts as unsaved");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(restored);
            UnityEngine.Object.DestroyImmediate(document);
        }
    }

    Content LoadSavedContent() => ContentLoader.Load(relative =>
    {
        var file = Path.Combine(temporary, relative);
        return File.Exists(file) ? File.ReadAllText(file) : null;
    }, Directory.GetFiles(Path.Combine(temporary, "events"), "*.yaml").Select(x => "events/" + Path.GetFileName(x)).ToList());

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
