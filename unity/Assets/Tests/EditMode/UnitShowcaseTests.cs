using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UnitShowcaseTests
{
    static Mesh[] AllMeshes() => new[]
    {
        UnitModels.Rifleman(UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, "a"),
        UnitModels.Rifleman(UnitModels.Headgear.SteelHelmet, UnitModels.RiflePose.Grounded, true, "b"),
        UnitModels.Rifleman(UnitModels.Headgear.WoolCap, UnitModels.RiflePose.Slung, false, "c"),
        UnitModels.Spitfire("s"), UnitModels.SpitfirePropeller("p"), UnitModels.Corvette("k"),
        UnitModels.AviaS199("v"), UnitModels.S199Propeller("q"),
    };

    [Test]
    public void ShowcaseIsAskedForOnlyByUnits()
    {
        Assert.IsTrue(UnitShowcase.Requested("https://x/index.html?units", new string[0], true));
        Assert.IsTrue(UnitShowcase.Requested("https://x/index.html?golden&shot&units", new string[0], true));
        Assert.IsFalse(UnitShowcase.Requested("https://x/index.html?golden&shot", new string[0], true), "a plain ?shot stays comparable across tracks");
        Assert.IsFalse(UnitShowcase.Requested("https://x/index.html?golden&smoke=5", new string[0], true));
        Assert.IsFalse(UnitShowcase.Requested("https://x/index.html?vprofile", new string[0], true), "profiles price it only with &units");
        Assert.IsFalse(UnitShowcase.Requested("https://x/units/index.html?unitsx", new string[0], true));
        Assert.IsTrue(UnitShowcase.Requested(null, new[] { "player", "-units" }, false));
        Assert.IsFalse(UnitShowcase.Requested(null, new[] { "player", "-shot" }, false));
        Assert.IsTrue(UnitShowcase.StillRequested("https://x/?shot&units", new string[0], true));
        Assert.IsFalse(UnitShowcase.StillRequested("https://x/?units", new string[0], true));
    }

    [Test]
    public void ModelsAreRepeatableAndWithinBudget()
    {
        var a = AllMeshes(); var b = AllMeshes();
        int total = 0;
        for (int i = 0; i < a.Length; i++)
        {
            CollectionAssert.AreEqual(a[i].vertices, b[i].vertices, a[i].name);
            CollectionAssert.AreEqual(a[i].triangles, b[i].triangles, a[i].name);
            CollectionAssert.AreEqual(a[i].uv, b[i].uv, a[i].name);
            total += a[i].triangles.Length / 3;
        }
        // Budget: the whole slice under 25k triangles (a WebGL integrated GPU draws millions; this keeps a full front of
        // trios cheap when #104 instances them).
        Assert.Less(total, 25000, $"slice triangles {total}");
        foreach (var m in a.Concat(b)) Object.DestroyImmediate(m);
    }

    [Test]
    public void EveryTriangleFacesAlongItsNormals()
    {
        foreach (var mesh in AllMeshes())
        {
            var v = mesh.vertices; var n = mesh.normals; var t = mesh.triangles;
            int bad = 0, counted = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                var face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (face.sqrMagnitude < 1e-12f) continue;
                counted++;
                if (Vector3.Dot(face, n[t[i]] + n[t[i + 1]] + n[t[i + 2]]) < 0f) bad++;
            }
            Assert.AreEqual(0, bad, $"{mesh.name}: {bad} of {counted} triangles face against their normals");
            Object.DestroyImmediate(mesh);
        }
    }

    /// <summary>The builders' normals point out of each solid (the winding test above only checks Build against the
    /// normals, so it cannot catch an inside-out builder). Convex parts, placed by rotated, scaled and mirrored matrices.</summary>
    [Test]
    public void BuilderNormalsPointOutwards()
    {
        var at = Matrix4x4.TRS(new Vector3(3, 1, -2), Quaternion.Euler(20, 35, -10), new Vector3(1.5f, 0.6f, 2f));
        var mirrored = Matrix4x4.TRS(new Vector3(-4, 0, 1), Quaternion.Euler(0, 0, 6), new Vector3(-1f, 1f, 1f));
        var parts = new (string name, MiniatureMesh mesh, Vector3 centre)[]
        {
            ("ellipsoid", new MiniatureMesh().Ellipsoid(at, 0), at.MultiplyPoint3x4(Vector3.zero)),
            ("mirrored ellipsoid", new MiniatureMesh().Ellipsoid(mirrored, 0), mirrored.MultiplyPoint3x4(Vector3.zero)),
            ("capped cone", new MiniatureMesh().Lathe(at, new[] { new Vector2(0f, 1f), new Vector2(2f, 0.4f) }, 0), at.MultiplyPoint3x4(new Vector3(0, 1f, 0))),
            ("tapered rod", new MiniatureMesh().Rod(new Vector3(0, 0, 0), new Vector3(1, 2, 0.5f), 0.3f, 0.2f, 0), new Vector3(0.5f, 1f, 0.25f)),
            ("box", new MiniatureMesh().Box(at, 0), at.MultiplyPoint3x4(Vector3.zero)),
        };
        foreach (var (name, builder, centre) in parts)
        {
            var mesh = builder.Build(name);
            var v = mesh.vertices; var n = mesh.normals;
            int inward = 0;
            for (int i = 0; i < v.Length; i++) if (Vector3.Dot(n[i], v[i] - centre) < -1e-4f) inward++;
            Assert.AreEqual(0, inward, $"{name}: {inward} of {v.Length} normals point inwards");
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void RiflemanStandsOnTheOriginAtAMansHeight()
    {
        foreach (UnitModels.Headgear hat in System.Enum.GetValues(typeof(UnitModels.Headgear)))
        {
            var m = UnitModels.Rifleman(hat, UnitModels.RiflePose.Grounded, false, "r");
            Assert.That(m.bounds.min.y, Is.InRange(-0.01f, 0.02f), $"{hat}: feet on the ground");
            Assert.That(m.bounds.max.y, Is.InRange(1.70f, 1.95f), $"{hat}: about a man's height");
            Object.DestroyImmediate(m);
        }
    }

    [Test]
    public void PaletteSlotsSampleTheirOwnColour()
    {
        var tex = UnitPalette.MakeTexture();
        for (int slot = 0; slot < UnitPalette.Count; slot++)
        {
            var c = tex.GetPixelBilinear(UnitPalette.U(slot), 0.5f);
            var want = UnitPalette.Shade(slot, 0.5f);
            Assert.That(Mathf.Abs(c.r - want.r) + Mathf.Abs(c.g - want.g) + Mathf.Abs(c.b - want.b), Is.LessThan(0.03f), $"slot {slot}");
        }
        Object.DestroyImmediate(tex);
    }

    /// <summary>Figures round 1 (refs #197): the uniform reads darker than the sunlit ground and warm brown, as promo 03's
    /// figures do (figure/ground luminance about 0.45 there; 0.68-0.82 with the old khaki, 0.50-0.57 now, on the editor
    /// renders), and the slope base is trodden earth, not a dark disc. Measured on the paint as drawn: the middle shade row
    /// (Shade at 0.5, with its wash), Rec. 709 weights. The old khaki was 0.39-0.52 there and red/green 1.13-1.21.</summary>
    [Test]
    public void TheUniformIsDarkAndWarmAndTheBaseIsEarth()
    {
        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        foreach (var slot in new[] { UnitPalette.Shirt, UnitPalette.ShirtFaded, UnitPalette.Trousers, UnitPalette.SlouchHat })
        {
            var c = UnitPalette.Shade(slot, 0.5f);
            Assert.Less(Luma(c), 0.35f, $"slot {slot}: dark enough to stand out against the ground");
            Assert.Greater(c.r / c.g, 1.24f, $"slot {slot}: warm brown, not olive khaki");
        }
        Assert.Less(Luma(UnitPalette.Shade(UnitPalette.Trousers, 0.5f)), Luma(UnitPalette.Shade(UnitPalette.Shirt, 0.5f)), "the trousers read darker than the shirt");
        Assert.Greater(UnitPalette.Shade(UnitPalette.Earth, 0.5f).r / UnitPalette.Shade(UnitPalette.Earth, 0.5f).g, 1.15f, "warm earth");
        var rows = Enumerable.Range(0, 100).Select(i => UnitPlinth.Mottle(i * 137f, i * 211f)).ToList();
        Assert.That(rows.Min(), Is.GreaterThanOrEqualTo(UnitPlinth.MottleLow)); Assert.That(rows.Max(), Is.LessThanOrEqualTo(UnitPlinth.MottleHigh));
        Assert.Greater(rows.Max() - rows.Min(), 0.3f, "mottled: the earth takes a spread of shade rows across the base");
    }

    [Test]
    public void FlightHoldsItsStillPhaseAndKeepsItsAltitude()
    {
        var go = new GameObject("f"); var prop = new GameObject("p");
        try
        {
            var f = go.AddComponent<ShowcaseFlight>();
            f.Configure(new Vector3(1000, 0, 2000), 500f, 300f, 10f, 20f, prop.transform, 0.25f);
            var first = go.transform.position;
            f.Configure(new Vector3(1000, 0, 2000), 500f, 300f, 10f, 20f, prop.transform, 0.25f);
            Assert.AreEqual(first, go.transform.position);
            Assert.AreEqual(300f, first.y, 1e-3f);
            Assert.AreEqual(500f, Vector2.Distance(new Vector2(first.x, first.z), new Vector2(1000, 2000)), 0.01f);
            // Nose along the path (tangent to the circle).
            var radial = (first - new Vector3(1000, first.y, 2000)).normalized;
            Assert.AreEqual(0f, Vector3.Dot(Vector3.ProjectOnPlane(go.transform.forward, Vector3.up).normalized, radial), 1e-3f);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(prop); }
    }

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    /// <summary>Everything a spawn made that must be freed with it: meshes, the palette material and its texture.</summary>
    static List<Object> SpawnedAssets(GameObject root)
    {
        var list = new List<Object>();
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) list.Add(f.sharedMesh);
        var paint = root.transform.Find("Rifleman0").GetComponent<MeshRenderer>().sharedMaterial;
        list.Add(paint); list.Add(paint.GetTexture("_BaseMap"));
        Assert.IsTrue(list.All(o => o != null));
        return list;
    }

    /// <summary>Issue #249: the showcase trio leaves out the kit trees under its plinth, and its toggle gives them back.</summary>
    [Test]
    public void TheShowcaseTrioStandsClearOfTheTrees()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        try
        {
            var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
            Assert.IsNotNull(veg, "the kits and their trees are on");
            var root = UnitShowcase.Spawn(map, still: true);
            var plinth = root.transform.Find(UnitPlinth.PlinthName);
            var centre = new Vector2(plinth.position.x, plinth.position.z);
            foreach (var i in new[] { 0, 1, 2 })
            {
                var f = root.transform.Find("Rifleman" + i).position;
                Assert.Less((new Vector2(f.x, f.z) - centre).magnitude + 0.2f * UnitShowcase.FigureScale, plinth.localScale.x, $"Rifleman{i}'s boots are on the plinth");
            }
            bool Under(Statecraft.Game.Map.Kits.TreeKind k, Matrix4x4 m)
            {
                var p = m.GetColumn(3); float reach = plinth.localScale.x * UnitPlinth.RimOuter + Statecraft.Game.Map.Kits.VegetationLayer.CrownRadius(k, m);
                return (centre - new Vector2(p.x, p.z)).sqrMagnitude < reach * reach;
            }
            Assert.IsFalse(veg.Drawable().Any(t => Under(t.kind, t.m)), "no drawn tree reaches the trio's plinth");
            Assert.AreEqual(veg.Planted().Count(t => Under(t.kind, t.m)), veg.Hidden, "only the trio's trees are left out");
            VisualToggles.Set(UnitShowcase.ToggleName, false);
            Assert.AreEqual(0, veg.Hidden, "the showcase off gives them back");
        }
        finally { if (VisualToggles.Names.Contains(UnitShowcase.ToggleName)) VisualToggles.Set(UnitShowcase.ToggleName, true); Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void SpawnBuildsTheSliceAtGameplay03AndTheToggleHidesIt()
    {
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            Assert.AreSame(map.Terrain.transform, root.transform.parent, "a map rebuild destroys it");
            Assert.AreEqual(3, root.GetComponentsInChildren<MeshFilter>().Count(f => f.name.StartsWith("Rifleman")));
            Assert.IsNotNull(root.transform.Find("SelectionRing"));
            Assert.IsNotNull(root.transform.Find("Corvette1948"));
            Assert.IsNotNull(root.transform.Find("Spitfire1948/Propeller"));
            var plane = root.GetComponentInChildren<ShowcaseFlight>();
            Assert.AreEqual(UnitShowcase.StillPhase, plane.StillPhase);
            // Each figure stands on the draped base at its own point (the ground there plus the lip).
            var plinth = root.transform.Find(UnitPlinth.PlinthName);
            Assert.IsNotNull(plinth, "the trio stands on a plinth");
            foreach (var f in root.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("Rifleman")))
                Assert.AreEqual(UnitPlinth.FigureY(map, plinth.localScale.x, f.transform.position.x, f.transform.position.z), f.transform.position.y, 1f, f.name);
            // The aircraft clears the highest ground under its loop.
            var loop = map.Frame.ToWorld(UnitShowcase.LoopCentre.lon, UnitShowcase.LoopCentre.lat, 0);
            Assert.Greater(plane.transform.position.y, UnitShowcase.LoopFloor(map, loop, UnitShowcase.LoopRadiusM) + UnitShowcase.AltitudeM * 0.99f);
            // Everything stands inside the gameplay03 view.
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var vp = cam.WorldToViewportPoint(r.bounds.center);
                Assert.That(vp.x, Is.InRange(0.02f, 0.98f), r.name); Assert.That(vp.y, Is.InRange(0.02f, 0.98f), r.name); Assert.Greater(vp.z, 0f, r.name);
            }
            CollectionAssert.Contains(VisualToggles.Names, UnitShowcase.ToggleName);
            VisualToggles.Set(UnitShowcase.ToggleName, false);
            Assert.IsFalse(root.activeSelf);
            VisualToggles.Set(UnitShowcase.ToggleName, true);
            Assert.IsTrue(root.activeSelf);
            // A second spawn replaces the first rather than stacking, and frees the first one's assets.
            var first = SpawnedAssets(root);
            UnitShowcase.Spawn(map, still: true);
            int count = 0; foreach (Transform c in map.Terrain.transform) if (c.name == UnitShowcase.RootName) count++;
            Assert.AreEqual(1, count);
            Assert.IsTrue(first.All(o => o == null), "the replaced spawn's meshes, material and palette are destroyed");
            CollectionAssert.Contains(VisualToggles.Names, UnitShowcase.ToggleName, "the new spawn keeps its switch");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
        CollectionAssert.DoesNotContain(VisualToggles.Names, UnitShowcase.ToggleName, "the map's teardown removes the switch");
    }

    [Test]
    public void TheTrioStandsOnAHexCentreAndTheMapFreesTheSlice()
    {
        var map = BuildMap();
        List<Object> assets;
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var ring = root.transform.Find("SelectionRing").GetComponent<Renderer>().bounds.center;
            float nearest = float.MaxValue;
            foreach (var h in map.Hexes.hexes)
            {
                var c = map.Frame.ToWorld(h.lon, h.lat, 0);
                nearest = Mathf.Min(nearest, new Vector2(c.x - ring.x, c.z - ring.z).magnitude);
            }
            Assert.Less(nearest, 50f, "the ring (and the trio inside it) is centred on a hex");
            // The ring fits inside its hex: its outer edge is short of the hex's inscribed circle.
            float hex = map.Hexes.hex_size_km * 1000f;
            Assert.Less(UnitShowcase.RingRadius * hex * (1f + UnitShowcase.RingWidth), hex * Mathf.Sqrt(3f) / 2f);
            assets = SpawnedAssets(root);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
        Assert.IsTrue(assets.All(o => o == null), "destroying the map destroys every mesh, the material and the palette");
    }

    [Test]
    public void AnAuthoredModelReplacesTheCodeBuiltOneForItsType()
    {
        var map = BuildMap();   // first: a new scene would destroy the stand-in models
        var helmet = new GameObject("authored-helmet"); new GameObject("AuthoredMarker").transform.SetParent(helmet.transform);
        var spitfire = new GameObject("authored-spitfire"); new GameObject("AuthoredMarker").transform.SetParent(spitfire.transform);
        var saved = UnitShowcase.AuthoredModel;
        UnitShowcase.AuthoredModel = type => type == UnitShowcase.RiflemanHelmetType ? helmet : type == UnitShowcase.SpitfireType ? spitfire : null;
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            Assert.IsNotNull(root.transform.Find("Rifleman1/AuthoredMarker"), "the helmet rifleman is the authored model");
            Assert.IsNull(root.transform.Find("Rifleman1").GetComponent<MeshFilter>(), "no code-built mesh for it");
            Assert.IsNotNull(root.transform.Find("Rifleman0").GetComponent<MeshFilter>(), "the others stay code-built");
            Assert.AreEqual(UnitShowcase.FigureScale, root.transform.Find("Rifleman1").localScale.x, 1e-3f, "placed and scaled as the code-built one");
            Assert.IsNotNull(root.transform.Find("Spitfire1948/Airframe/AuthoredMarker"));
            Assert.IsNull(root.transform.Find("Spitfire1948/Propeller"), "an authored aircraft brings its own propeller");
            Assert.IsNotNull(root.transform.Find("Corvette1948").GetComponent<MeshFilter>());
        }
        finally
        {
            UnitShowcase.AuthoredModel = saved;
            Object.DestroyImmediate(map.gameObject); Object.DestroyImmediate(helmet); Object.DestroyImmediate(spitfire);
        }
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
}
