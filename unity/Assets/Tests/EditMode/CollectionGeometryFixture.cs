#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEngine;
internal static class CollectionGeometryFixture
{
    sealed class Lease : IDisposable { public void Dispose() { } }
    static CollectionResidencyPolicy Wide => new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, int.MaxValue, "GEOMETRY TEST ONLY; not production admission");
    static T Layer<T>(MapScene map, string name, Action<T> configure) where T : Component
    {
        var layer = map.Terrain.transform.Find(name)?.GetComponent<T>();
        if (layer != null) return layer;
        var go = new GameObject(name); go.transform.SetParent(map.Terrain.transform, false); layer = go.AddComponent<T>();
        // Private map field supplies the same construction reference as ShowAt, before its first demand.
        typeof(T).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(layer, map);
        configure(layer); return layer;
    }
    static void Observe(Component layer)
    {
        var name = "Geometry fixture camera";
        var go = layer.transform.parent.Find(name)?.gameObject;
        if (go == null) { go = new GameObject(name); go.transform.SetParent(layer.transform.parent, false); go.AddComponent<Camera>(); }
        var camera = go.GetComponent<Camera>(); camera.transform.position = new Vector3(0, 60000, 0);
        camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        // Choose is the real renderer callback, invoked by the deterministic geometry fixture.
        layer.GetType().GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer,
            new object[] { default(UnityEngine.Rendering.ScriptableRenderContext), camera });
    }
    internal static CivilianPeople? People(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content,
        IReadOnlyDictionary<string, SettlementControl>? control = null, IEnumerable<CityProject>? projects = null)
    {
        Layer<CivilianPeople>(map, CivilianPeople.Name, x => x.ConfigureResidency(Wide,
            (id, detail, done) => { CivilianPeople.Request(id, detail, done); return new Lease(); }));
        var result = CivilianPeople.ShowAt(map, date, placements, content, control, projects); Observe(result!); return result;
    }
    internal static RuralLife? Rural(MapScene map)
    {
        Layer<RuralLife>(map, RuralLife.Name, x => x.ConfigureResidency(Wide,
            (id, detail, done) => { RuralLife.Request(id, detail, done); return new Lease(); }));
        var result = RuralLife.Show(map); Observe(result!); return result;
    }
    internal static SeaAirCollection? Craft(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
    {
        Layer<SeaAirCollection>(map, SeaAirCollection.Name, x => x.ConfigureResidency(Wide,
            (id, detail, done) => { SeaAirCollection.Request(id, detail, done); return new Lease(); }));
        var result = SeaAirCollection.ShowAt(map, date, placements, content); Observe(result!); return result;
    }
    internal static SpecialBuildingModels? Special(MapScene map, DateTime date)
    {
        Layer<SpecialBuildingModels>(map, SpecialBuildingModels.Name, x => x.ConfigureResidency(Wide,
            (id, detail, done) => { SpecialBuildingModels.Request(id, detail, done); return new Lease(); }));
        var result = SpecialBuildingModels.ShowAt(map, date); Observe(result!); return result;
    }
}
