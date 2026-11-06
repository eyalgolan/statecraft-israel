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

    /// <summary>Everything a spawn made that must be freed with it: meshes, the palette material and its texture, and the
    /// aircraft's glossy paint and its blur and shadow material.</summary>
    static List<Object> SpawnedAssets(GameObject root)
    {
        var list = new List<Object>();
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) list.Add(f.sharedMesh);
        var paint = root.transform.Find("Rifleman0").GetComponent<MeshRenderer>().sharedMaterial;
        list.Add(paint); list.Add(paint.GetTexture("_BaseMap"));
        list.Add(root.transform.Find("Spitfire1948/Airframe").GetComponent<MeshRenderer>().sharedMaterial);
        list.Add(root.transform.Find("Spitfire1948/" + UnitShowcase.ShadowName).GetComponent<MeshRenderer>().sharedMaterial);
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

    /// <summary>#2734 (coordinator's option 1): no unit model comes from a Resources folder any more. The art hook that loaded
    /// one there (UnitShowcase.AuthoredModel, from Resources/Units/Authored/&lt;unit type&gt;) is gone, the figures and the ship
    /// are always the code-built ones, and no unit type has a model left under Resources (the Blender-built Spitfire streams
    /// instead, StreamedAircraft). Replaces AnAuthoredModelReplacesTheCodeBuiltOneForItsType, which stubbed that hook.</summary>
    [Test]
    public void NoUnitModelComesFromResourcesAndTheFiguresAndTheShipStayCodeBuilt()
    {
        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        Assert.IsNull(typeof(UnitShowcase).GetField("AuthoredModel", flags), "the Resources art hook is gone");
        Assert.IsNull(typeof(UnitShowcase).GetField("AuthoredFolder", flags), "and its folder");
        foreach (var type in new[] { UnitShowcase.RiflemanSlouchHatType, UnitShowcase.RiflemanHelmetType, UnitShowcase.RiflemanWoolCapType,
            UnitShowcase.SpitfireType, UnitShowcase.CorvetteType, UnitShowcase.S199Type })
            Assert.IsNull(Resources.Load<GameObject>("Units/Authored/" + type), type + ": nothing under Resources/Units/Authored");
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            foreach (var name in new[] { "Rifleman0", "Rifleman1", "Rifleman2", "Corvette1948", "Spitfire1948/Airframe", "Spitfire1948/Propeller" })
            {
                var piece = root.transform.Find(name);
                Assert.IsNotNull(piece, name);
                Assert.IsNotNull(piece.GetComponent<MeshFilter>().sharedMesh, name + " is code-built");
            }
            Assert.AreEqual(UnitShowcase.FigureScale, root.transform.Find("Rifleman1").localScale.x, 1e-3f, "placed and scaled as before");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A stand-in for the stream's lease, counting its releases.</summary>
    sealed class CountedLease : System.IDisposable { public int Disposed; public void Dispose() => Disposed++; }

    /// <summary>A stand-in for the streamed Spitfire's prefab, as the FBX's two top-level nodes: a body and, when asked, a
    /// <see cref="StreamedAircraft.PropellerName"/> at the hub.</summary>
    static GameObject StandInSpitfire(bool propeller)
    {
        var root = new GameObject("stand-in streamed spitfire");
        void Part(string name, PrimitiveType shape, Vector3 at)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false); go.transform.localPosition = at;
        }
        Part("Body", PrimitiveType.Cube, Vector3.zero);
        if (propeller) Part(StreamedAircraft.PropellerName, PrimitiveType.Sphere, UnitModels.SpitfirePropellerHub);
        return root;
    }

    /// <summary>Asks for the streamed Spitfire through a deferred stand-in for the stream: each request is kept, with its own
    /// lease, until the test answers it. The caller restores <see cref="StreamedAircraft.Source"/> to null.</summary>
    static List<(string id, Statecraft.Game.Map.Collection.CollectionDetail detail, System.Action<GameObject> done, CountedLease lease)> DeferredStream()
    {
        var asked = new List<(string, Statecraft.Game.Map.Collection.CollectionDetail, System.Action<GameObject>, CountedLease)>();
        StreamedAircraft.Source = (id, detail, done) => { var lease = new CountedLease(); asked.Add((id, detail, done, lease)); return lease; };
        return asked;
    }

    /// <summary>The code-built airframe's and propeller's own renderers under an aircraft holder (not the propeller's blur,
    /// UnitShowcase.PropellerBlurName, a look of the flight rather than of the model).</summary>
    static Renderer[] CodeBuilt(Transform plane) =>
        new[] { "Airframe", "Propeller" }.SelectMany(n => plane.Find(n).GetComponentsInChildren<Renderer>(true))
            .Where(r => r.name != UnitShowcase.PropellerBlurName).ToArray();

    /// <summary>#2734: the showcase's Spitfire is drawn at once, code-built (its airframe and propeller), and the Blender-built
    /// model is asked for from the Collection stream. When the stream answers, the model stands under the aircraft in the
    /// code-built model's frame, the code-built airframe and propeller stop drawing, the aircraft keeps its place, heading and
    /// symbolic scale, and the flight turns the model's own Propeller, not the hidden code-built one.</summary>
    [Test]
    public void TheShowcaseSpitfireStartsCodeBuiltAndSwapsToTheStreamedModelWhenItArrives()
    {
        var map = BuildMap();   // first: a new scene would destroy the stand-in model
        var model = StandInSpitfire(propeller: true);
        var asked = DeferredStream();
        try
        {
            var root = UnitShowcase.Spawn(map, still: false);
            var plane = root.transform.Find("Spitfire1948");
            Assert.AreEqual(1, asked.Count, "one request, for the 101 Squadron Spitfire");
            Assert.AreEqual(Statecraft.Game.Map.Collection.CollectionCatalog.IsraeliSpitfire, asked[0].id);
            Assert.AreEqual(UnitShowcase.SpitfireType, asked[0].id, "the unit type is the catalog id");
            Assert.AreEqual(Statecraft.Game.Map.Collection.CollectionDetail.Far, asked[0].detail);
            var code = CodeBuilt(plane);
            Assert.AreEqual(2, code.Length);
            Assert.IsTrue(code.All(r => r.enabled), "until the model arrives the code-built aircraft is drawn");
            Assert.IsNull(plane.Find(StreamedAircraft.ModelName));
            var flight = plane.GetComponent<ShowcaseFlight>();
            var codeProp = plane.Find("Propeller");
            Assert.AreSame(codeProp, flight.Propeller, "meanwhile the flight turns the code-built propeller");
            var (position, rotation, scale) = (plane.position, plane.rotation, plane.localScale);

            asked[0].done(model);

            var streamed = plane.Find(StreamedAircraft.ModelName);
            Assert.IsNotNull(streamed, "the streamed model stands under the aircraft");
            Assert.AreEqual(Vector3.zero, streamed.localPosition, "in the code-built model's frame: its origin");
            Assert.Less(Quaternion.Angle(Quaternion.identity, streamed.localRotation), 0.01f, "nose along the aircraft's");
            Assert.AreEqual(Vector3.one, streamed.localScale, "real metres, as the code-built model");
            Assert.IsTrue(streamed.gameObject.activeSelf);
            var drawn = streamed.GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(2, drawn.Length); Assert.IsTrue(drawn.All(r => r.enabled), "the streamed model draws");
            Assert.IsTrue(code.All(r => !r.enabled), "the code-built airframe and propeller no longer draw");
            Assert.AreEqual(position, plane.position, "the aircraft keeps its place");
            Assert.Less(Quaternion.Angle(rotation, plane.rotation), 0.01f, "and its heading");
            Assert.AreEqual(scale, plane.localScale, "and its symbolic scale");
            Assert.AreEqual(Vector3.one * UnitShowcase.AircraftScale, plane.localScale);
            Assert.AreEqual(0, asked[0].lease.Disposed, "the lease is held while the aircraft lives");

            // The flight now turns the streamed propeller, and leaves the hidden code-built one alone.
            var streamedProp = streamed.Find(StreamedAircraft.PropellerName);
            Assert.AreSame(streamedProp, flight.Propeller, "the flight turns the streamed model's Propeller");
            var hidden = Quaternion.Euler(0f, 0f, 33f);
            codeProp.localRotation = hidden; streamedProp.localRotation = Quaternion.Euler(0f, 0f, 77f);
            typeof(ShowcaseFlight).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(flight, null);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 0f, -Mathf.Repeat(Time.time * 1500f, 360f)), streamedProp.localRotation), 0.01f,
                "a flight step sets the streamed propeller's angle");
            Assert.Less(Quaternion.Angle(hidden, codeProp.localRotation), 0.01f, "and not the hidden code-built one's");
        }
        finally
        {
            StreamedAircraft.Source = null;
            Object.DestroyImmediate(map.gameObject); Object.DestroyImmediate(model);
        }
    }

    /// <summary>#2734: the stream failing (a missing or damaged bundle answers null) keeps the whole code-built aircraft, and
    /// so does a streamed model without a Propeller child (a model the flight could not turn): nothing of either is added.</summary>
    [Test]
    public void AFailedOrPropellerlessStreamedSpitfireKeepsTheWholeCodeBuiltAircraft()
    {
        var map = BuildMap();
        var noPropeller = StandInSpitfire(propeller: false);
        var asked = DeferredStream();
        try
        {
            void KeepsTheCodeBuiltAircraft(GameObject root, string why)
            {
                var plane = root.transform.Find("Spitfire1948");
                Assert.IsNull(plane.Find(StreamedAircraft.ModelName), why + ": no streamed model");
                Assert.AreEqual(plane.GetComponentsInChildren<Renderer>(true).Count(r => r.name != UnitShowcase.PropellerBlurName && r.name != UnitShowcase.ShadowName),
                    CodeBuilt(plane).Length, why + ": nothing else under the aircraft but its blur and its shadow");
                Assert.IsTrue(CodeBuilt(plane).All(r => r.enabled), why + ": the code-built airframe and propeller still draw");
                Assert.AreSame(plane.Find("Propeller"), plane.GetComponent<ShowcaseFlight>().Propeller, why + ": the flight turns the code-built propeller");
            }
            var first = UnitShowcase.Spawn(map, still: true);
            asked[0].done(null);
            KeepsTheCodeBuiltAircraft(first, "no model");
            var second = UnitShowcase.Spawn(map, still: true);
            Assert.AreEqual(1, asked[0].lease.Disposed, "the replaced showcase released its lease");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no Propeller"));
            asked[1].done(noPropeller);
            KeepsTheCodeBuiltAircraft(second, "a model without a Propeller");
        }
        finally
        {
            StreamedAircraft.Source = null;
            Object.DestroyImmediate(map.gameObject); Object.DestroyImmediate(noPropeller);
        }
    }

    /// <summary>#2734: destroying the showcase (here the root; a map rebuild destroys it the same way) releases the aircraft's
    /// lease exactly once, and the stream's answer coming after that instantiates nothing.</summary>
    [Test]
    public void DestroyingTheShowcaseReleasesTheSpitfireLeaseOnceAndALateAnswerDrawsNothing()
    {
        var map = BuildMap();
        var model = StandInSpitfire(propeller: true);
        var asked = DeferredStream();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            Assert.AreEqual(1, asked.Count);
            Assert.AreEqual(0, asked[0].lease.Disposed);
            Object.DestroyImmediate(root);
            Assert.AreEqual(1, asked[0].lease.Disposed, "destroying the aircraft releases its lease");
            asked[0].done(model);   // the stream answers after all
            Assert.AreEqual(1, asked[0].lease.Disposed, "once");
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.name == StreamedAircraft.ModelName),
                "a late answer instantiates nothing");
            UnitShowcase.Spawn(map, still: true);
            Object.DestroyImmediate(map.gameObject);
            Assert.AreEqual(1, asked[1].lease.Disposed, "destroying the map releases the showcase's lease too");
        }
        finally
        {
            StreamedAircraft.Source = null;
            if (map != null) Object.DestroyImmediate(map.gameObject);
            Object.DestroyImmediate(model);
        }
    }

    /// <summary>#2734: the game asks for the streamed Spitfire with the Collection stream's owned lease (CollectionStream.Acquire,
    /// not Request, so the bundle is not pinned for good). In edit mode, with no stand-in, nothing is asked for (the stream's
    /// coroutine never runs there, as UnitFigures has it), so the code-built aircraft stays.</summary>
    [Test]
    public void TheGameAsksForTheStreamedSpitfireWithCollectionStreamAcquireAndEditModeAsksNothing()
    {
        Assert.AreEqual(typeof(Statecraft.Game.Map.Collection.CollectionStream).GetMethod(nameof(Statecraft.Game.Map.Collection.CollectionStream.Acquire),
                new[] { typeof(string), typeof(Statecraft.Game.Map.Collection.CollectionDetail), typeof(System.Action<GameObject>) }),
            StreamedAircraft.ProductionAcquire.Method, "the production acquisition is CollectionStream.Acquire");
        Assert.IsNull(StreamedAircraft.Source, "no stand-in is left set");
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var streamed = root.transform.Find("Spitfire1948").GetComponent<StreamedAircraft>();
            Assert.IsNotNull(streamed, "the showcase's Spitfire carries the swap");
            Assert.IsFalse(streamed.Asked, "edit mode asks nothing of the stream");
            Assert.IsTrue(CodeBuilt(streamed.transform).All(r => r.enabled));
            Assert.IsNull(root.transform.Find("Corvette1948").GetComponent<StreamedAircraft>(), "only the aircraft");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The coordinator-approved Spitfire plan, step 1 (5 November 2026): the code-built 101 Squadron Spitfire's
    /// propeller carries a light, see-through blur that turns with it: pale, clear at the rim, at most about half opaque,
    /// brighter at its four blades than between them, casting and taking no shadow.</summary>
    [Test]
    public void TheCodeBuiltSpitfiresPropellerCarriesALightBlur()
    {
        bool readable = UnitPlinth.KeepMeshesReadable; UnitPlinth.KeepMeshesReadable = true;   // the blur's colours are read below
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var prop = root.transform.Find("Spitfire1948/Propeller");
            var blur = prop.Find(UnitShowcase.PropellerBlurName);
            Assert.IsNotNull(blur, "a blur, a child of the turning propeller, so it turns with it");
            var r = blur.GetComponent<MeshRenderer>();
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode); Assert.IsFalse(r.receiveShadows);
            Assert.AreNotSame(root.transform.Find("Rifleman0").GetComponent<MeshRenderer>().sharedMaterial, r.sharedMaterial, "a see-through material, not the paint");
            Assert.Greater(Mathf.Abs(Vector3.Dot(blur.up, prop.forward)), 0.999f, "the disc faces along the propeller's axis (+z)");
            var colours = blur.GetComponent<MeshFilter>().sharedMesh.colors32;
            Assert.IsTrue(colours.All(c => c.r > 180 && c.g > 180 && c.b > 180), "light, not the dark rods' colour");
            float max = colours.Max(c => c.a) / 255f;
            Assert.That(max, Is.InRange(0.3f, 0.6f), "see-through");
            // e21's AFTER captures of #2728: two layers read as a near-opaque plate under the dark rods. One layer, lighter
            // than the Egyptian Spitfire's haze at the same angle.
            var positions = blur.GetComponent<MeshFilter>().sharedMesh.vertices;
            Assert.IsTrue(positions.All(v => Mathf.Abs(v.y - positions[0].y) < 1e-5f), "one layer");
            Assert.Less(max, Statecraft.Game.Map.Collection.SeaAirCollection.PropBlurAlpha(0f, 90f, 0f) - 0.1f, "lighter than the Egyptian haze at a blade");
            Assert.AreEqual(0, colours.Min(c => c.a), "clear at its rim");
            Assert.Greater(max - colours.Where(c => c.a > 0).Min(c => c.a) / 255f, 0.15f, "brighter at the blades: streaked, not a flat disc");
            // The streaks sit at the four rods (UnitModels.SpitfirePropeller: up turned 90 b + 20 degrees about +z, so at 110 +
            // 90 b from +x) and trail them: the propeller turns clockwise seen from behind (ShowcaseFlight, -z), so just behind a
            // rod is a little more angle. Read in the propeller's own frame.
            var verts = blur.GetComponent<MeshFilter>().sharedMesh.vertices;
            float near = 0f, far = 0f; int nearN = 0, farN = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                var p = prop.InverseTransformPoint(blur.TransformPoint(verts[i]));
                if (new Vector2(p.x, p.y).magnitude < 0.5f * UnitShowcase.PropellerBlurRadius || colours[i].a == 0) continue;   // the outer rings
                float behind = Mathf.Repeat(Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg - 110f, 90f);
                if (behind < 15f) { near += colours[i].a; nearN++; } else if (behind > 45f && behind < 75f) { far += colours[i].a; farN++; }
            }
            Assert.Greater(nearN, 0); Assert.Greater(farN, 0);
            Assert.Greater(near / nearN - far / farN, 0.1f * 255f, "brighter just behind each of the four rods than between them");
        }
        finally { UnitPlinth.KeepMeshesReadable = readable; Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The coordinator-approved Spitfire plan, step 2: the code-built Spitfire wears a glossy copy of the units' paint
    /// (the same palette, smoothness UnitShowcase.AircraftSmoothness), its own: the riflemen and the corvette keep theirs.</summary>
    [Test]
    public void TheCodeBuiltSpitfireWearsAGlossyCopyOfTheUnitsPaint()
    {
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var paint = root.transform.Find("Rifleman0").GetComponent<MeshRenderer>().sharedMaterial;
            var gloss = root.transform.Find("Spitfire1948/Airframe").GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreNotSame(paint, gloss, "its own material");
            Assert.AreSame(gloss, root.transform.Find("Spitfire1948/Propeller").GetComponent<MeshRenderer>().sharedMaterial, "the propeller too");
            Assert.AreSame(paint, root.transform.Find("Corvette1948").GetComponent<MeshRenderer>().sharedMaterial, "the corvette keeps the units' paint");
            Assert.AreEqual(UnitShowcase.AircraftSmoothness, gloss.GetFloat("_Smoothness"), 1e-6f);
            Assert.Greater(gloss.GetFloat("_Smoothness"), paint.GetFloat("_Smoothness"), "glossier than the units' paint");
            Assert.AreSame(paint.shader, gloss.shader); Assert.AreSame(paint.GetTexture("_BaseMap"), gloss.GetTexture("_BaseMap"), "the same palette");
            Assert.AreEqual(paint.GetFloat("_Metallic"), gloss.GetFloat("_Metallic"), "painted, not bare metal");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The coordinator-approved Spitfire plan, step 3: the flying code-built Spitfire casts a soft shadow on the ground,
    /// as the Egyptian Spitfire does: on the map sun's ray from SpitfireLoop.ShadowHeightShare of its height, a little over the
    /// ground there, turned with its heading, about as long and as wide as the aircraft seen from above, going with it.</summary>
    [Test]
    public void TheFlyingCodeBuiltSpitfireCastsASoftShadowOnTheGround()
    {
        var map = BuildMap();
        var sun = new GameObject("TEST ONLY sun").AddComponent<Light>();
        var sunWas = RenderSettings.sun;
        try
        {
            sun.type = LightType.Directional; MapLighting.Apply(sun);
            var root = UnitShowcase.Spawn(map, still: true);
            var flight = root.GetComponentInChildren<ShowcaseFlight>();
            var shadow = flight.transform.Find(UnitShowcase.ShadowName);
            Assert.IsNotNull(shadow, "a shadow, a child of the aircraft, so it goes with it");
            Assert.IsTrue(flight.GetComponentsInChildren<Renderer>(true).All(x => x.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off),
                "its only shadow: the aircraft casts none (one would lie about 7 km off at its drawn height)");
            var r = shadow.GetComponent<MeshRenderer>();
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode); Assert.IsFalse(r.receiveShadows);
            var model = UnitModels.Spitfire("span"); float span = model.bounds.size.x * UnitShowcase.AircraftScale; Object.DestroyImmediate(model);
            float y0 = map.Terrain.transform.position.y, sea = UnitShowcase.SeaLevelY(map);
            float Surface(Vector3 p) => Mathf.Max(map.Terrain.SampleHeight(p) + y0, sea);
            var ray = sun.transform.forward;
            for (int i = 0; i < 12; i++)
            {
                flight.Place(i / 12f);
                var drawn = flight.transform.position; var s = shadow.position;
                Assert.That(s.y - Surface(s), Is.InRange(0f, Statecraft.Game.Map.Collection.SpitfireLoop.ShadowLiftM + 400f), $"on the ground at phase {i}/12");
                float under = Surface(drawn);
                var low = new Vector3(drawn.x, under + Statecraft.Game.Map.Collection.SpitfireLoop.ShadowHeightShare * (drawn.y - under), drawn.z);
                var cast = low + ray * ((low.y - Surface(s)) / -ray.y);
                Assert.Less(new Vector2(s.x - cast.x, s.z - cast.z).magnitude, 50f + 0.03f * (low.y - Surface(s)), $"on the sun's ray from its lowered height at phase {i}/12");
                Assert.Greater(Vector3.Dot(shadow.forward, Vector3.ProjectOnPlane(flight.transform.forward, Vector3.up).normalized), 0.999f, "turned with its heading");
                Assert.That(shadow.lossyScale.x / span, Is.InRange(0.4f, 0.6f), "about half the span across (a little narrowed in the bank)");
                Assert.That(shadow.lossyScale.z / span, Is.InRange(0.3f, 0.6f), "about half its length along");
            }
        }
        finally { RenderSettings.sun = sunWas; Object.DestroyImmediate(sun.gameObject); Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The 101 Squadron Spitfire's Blender-built model (#2734), once CollectionImport.MakeAuthoredUnitPrefabs has made
    /// its prefab: the catalog prefab (CollectionCatalog.PrefabPath at Far, loaded from the AssetDatabase, as MapCapture answers
    /// for the stream; nothing of it is under Resources) has a DIRECT child named StreamedAircraft.PropellerName (the child
    /// StreamedAircraft hands the flight), with a mesh, its pivot on the centre line ahead of the airframe, at rest (identity
    /// rotation, as ShowcaseFlight overwrites it), so the flight turns it. Ignored until the prefab is made.</summary>
    [Test]
    public void TheAuthoredSpitfireHasADirectPropellerChildTheFlightCanTurn()
    {
        var path = Statecraft.Game.Map.Collection.CollectionCatalog.PrefabPath(Statecraft.Game.Map.Collection.CollectionCatalog.IsraeliSpitfire,
            Statecraft.Game.Map.Collection.CollectionDetail.Far);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Assert.Ignore($"no {path} yet (run CollectionImport.MakeAuthoredUnitPrefabs, #2734)");
        var prop = prefab.transform.Find(StreamedAircraft.PropellerName);
        Assert.IsNotNull(prop, $"a direct child named {StreamedAircraft.PropellerName}");
        Assert.IsNotNull(prop.GetComponentInChildren<MeshFilter>()?.sharedMesh, "its blades are a mesh");
        Assert.AreEqual(Quaternion.identity, prop.localRotation, "at rest: ShowcaseFlight sets its turn about local +z");
        Assert.AreEqual(Vector3.one, prop.localScale);
        Assert.AreEqual(0f, prop.localPosition.x, 0.01f, "the hub on the centre line");
        var others = prefab.GetComponentsInChildren<MeshFilter>(true).Where(f => !f.transform.IsChildOf(prop)).Select(f => f.sharedMesh.bounds).ToList();
        Assert.IsNotEmpty(others, "an airframe besides the propeller");
        Assert.Greater(prop.localPosition.z, others.Max(b => b.center.z), "the propeller is at the nose (+z)");

        // And through the real showcase, the stream answered from the AssetDatabase at once: the flight turns that propeller.
        var map = BuildMap();
        StreamedAircraft.Source = (id, detail, done) =>
        {
            done(AssetDatabase.LoadAssetAtPath<GameObject>(Statecraft.Game.Map.Collection.CollectionCatalog.PrefabPath(id, detail)));
            return new CountedLease();
        };
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var spawned = root.transform.Find($"Spitfire1948/{StreamedAircraft.ModelName}/{StreamedAircraft.PropellerName}");
            Assert.IsNotNull(spawned, "the streamed model's Propeller under the showcase's Spitfire");
            var flight = root.GetComponentInChildren<ShowcaseFlight>();
            Assert.AreSame(spawned, flight.Propeller);
            Assert.IsNotNull(flight.transform.Find(UnitShowcase.ShadowName), "the soft shadow");
            // And the flight really turns it about its local +z, 1500 degrees a second, once it flies (not held still).
            typeof(ShowcaseFlight).GetField("still", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(flight, null);
            var update = typeof(ShowcaseFlight).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var clock = ShowcaseFlight.Clock;
            try
            {
                ShowcaseFlight.Clock = () => 0f; update.Invoke(flight, null); var q0 = spawned.localRotation;
                ShowcaseFlight.Clock = () => 0.01f; update.Invoke(flight, null);
                Assert.AreEqual(15f, Quaternion.Angle(q0, spawned.localRotation), 0.1f, "15 degrees in a hundredth of a second");
                Assert.Greater(Mathf.Abs(Vector3.Dot(spawned.localRotation * Vector3.forward, Vector3.forward)), 0.999f, "about its local +z");
            }
            finally { ShowcaseFlight.Clock = clock; }
        }
        finally { StreamedAircraft.Source = null; Object.DestroyImmediate(map.gameObject); }
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
}
