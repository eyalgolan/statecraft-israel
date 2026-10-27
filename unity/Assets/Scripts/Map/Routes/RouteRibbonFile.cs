#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>One route's share of a ribbon mesh: its first vertex and vertex count, and its first index and index count
    /// (whole triangles). A route whose line is shorter than two points has none. A route a corridor follows (GDD Phase 4,
    /// #792) also has its segments' index ranges: <see cref="Segments"/> holds the index each segment starts at, then the
    /// route's end (FirstIndex + IndexCount), so segment k is [Segments[k], Segments[k + 1]); empty for any other route.</summary>
    public readonly struct RouteSpan : IEquatable<RouteSpan>
    {
        public readonly string Id;
        public readonly int FirstVertex, VertexCount, FirstIndex, IndexCount;
        public readonly int[] Segments;
        public RouteSpan(string id, int firstVertex, int vertexCount, int firstIndex, int indexCount, int[]? segments = null)
        {
            Id = id; FirstVertex = firstVertex; VertexCount = vertexCount; FirstIndex = firstIndex; IndexCount = indexCount;
            Segments = segments ?? Array.Empty<int>();
        }
        public bool Equals(RouteSpan o) => Id == o.Id && FirstVertex == o.FirstVertex && VertexCount == o.VertexCount
            && FirstIndex == o.FirstIndex && IndexCount == o.IndexCount && (Segments ?? Array.Empty<int>()).AsSpan().SequenceEqual(o.Segments ?? Array.Empty<int>());
        public override bool Equals(object? o) => o is RouteSpan s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Id, FirstVertex, VertexCount, FirstIndex, IndexCount);
        public override string ToString() => $"{Id} v{FirstVertex}+{VertexCount} i{FirstIndex}+{IndexCount}" + (Segments is { Length: > 0 } s ? $" segments {string.Join(",", s)}" : "");
    }

    /// <summary>A ribbon mesh's arrays (RouteLayer.Geometry), and which routes it holds where (<see cref="Routes"/>), so a
    /// later change can draw one route on its own (GDD Phase 4 corridors, issue #384).</summary>
    public sealed class RibbonGeometry
    {
        public readonly Vector3[] Vertices, Normals;
        public readonly Vector2[] Uvs;
        public readonly int[] Indices;
        public readonly RouteSpan[] Routes;

        public RibbonGeometry(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] indices, RouteSpan[] routes)
        { Vertices = vertices; Normals = normals; Uvs = uvs; Indices = indices; Routes = routes; }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (Vertices.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(Vertices); mesh.SetNormals(Normals); mesh.SetUVs(0, Uvs); mesh.SetTriangles(Indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Segment <paramref name="segment"/> of route <paramref name="route"/> (<see cref="RouteSpan.Segments"/>) as
        /// a mesh of its own: its triangles and only the vertices they use, so the GDD loop can show each built segment by
        /// itself (RouteLayer.ShowAt).</summary>
        public Mesh SegmentMesh(int route, int segment, string name)
        {
            var s = Routes[route].Segments;
            int from = s[segment], count = s[segment + 1] - from;
            if (count == 0) return new Mesh { name = name };   // two cuts on one cross-section: nothing to draw
            int low = int.MaxValue, high = -1;
            for (int i = from; i < from + count; i++) { low = Math.Min(low, Indices[i]); high = Math.Max(high, Indices[i]); }
            var indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = Indices[from + i] - low;
            int n = high - low + 1;
            return new RibbonGeometry(Vertices.AsSpan(low, n).ToArray(), Normals.AsSpan(low, n).ToArray(), Uvs.AsSpan(low, n).ToArray(),
                indices, Array.Empty<RouteSpan>()).ToMesh(name);
        }
    }

    /// <summary>The route ribbons, baked (Editor RouteRibbonBake, issue #321), so a player places them instead of deriving
    /// them at boot (about 100 ms at the boot budget's CPU throttling). Little-endian throughout:
    /// <list type="bullet">
    /// <item>header: "SCR1", <see cref="Version"/>, the heightmap's resolution and HeightsCheck, checks of heightmap.json
    /// and route_paths_1948.yaml (<see cref="Check"/>), the terrain's size and origin (6 floats), the geometry constants
    /// (<see cref="Floats"/>, then <see cref="Ints"/>), the mesh count;</item>
    /// <item>each mesh, in RouteLayer.Plan's order: its name, its vertex, index and route counts, each route's id and
    /// <see cref="RouteSpan"/> (its four ints, then the count of its segment starts and the starts), then the vertices, normals and uvs (floats) and the indices (int32).</item>
    /// </list>
    /// A string is its UTF-8 byte count (int32) and the bytes. tools/tests/test_route_ribbon_bake.py checks the header
    /// against the shipped files without Unity.</summary>
    public static class RouteRibbonFile
    {
        public const string Resource = "Routes/RouteRibbons";
        public const string AssetPath = RouteLayer.Folder + "/RouteRibbons.bytes";
        /// <summary>Bump it when RouteLayer.Geometry's rule changes (the constants are checked on their own), so CI fails a
        /// bake made by the old rule. tools/tests/test_route_ribbon_bake.py pins it with a fingerprint of that code
        /// (RouteLayer.Plan, Derive and what they call, and Bake below) and fails on an edit until both are re-pinned.</summary>
        public const int Version = 4;   // 2: roads cut at the Old City's walls (RouteLayer.Drawn, #319); 3: the railway too; 4: segments (#792)
        const int Magic = 0x31524353;   // "SCR1"

        /// <summary>What the ribbons were made from, as the map read it.</summary>
        public readonly struct Source
        {
            public readonly int Resolution, HeightsCheck, MapCheck, RoutesCheck;
            public Source(int resolution, int heightsCheck, int mapCheck, int routesCheck)
            { Resolution = resolution; HeightsCheck = heightsCheck; MapCheck = mapCheck; RoutesCheck = routesCheck; }

            public static Source Of(float[,] heights, string heightmapJson, string routesYaml) =>
                new Source(heights.GetLength(1), MapBuilder.HeightsCheck(heights), Check(heightmapJson), Check(routesYaml));
        }

        /// <summary>FNV-1a over the text's UTF-8 bytes (the file's bytes: no BOM, LF by .gitattributes).</summary>
        public static int Check(string text)
        {
            uint h = 2166136261;
            foreach (var b in Encoding.UTF8.GetBytes(text ?? "")) { h ^= b; h *= 16777619; }
            return (int)h;
        }

        /// <summary>The constants the ribbons depend on, in the header's order (the Python test reads the same names).</summary>
        static float[] Floats => new[]
        {
            RouteLayer.RailWidthM, RouteLayer.RoadWidthM, RouteLayer.TrackWidthM, RouteLayer.StepM, RouteLayer.ClearanceM,
            RouteLayer.FallToleranceM, RouteLayer.RiseToleranceM, RouteLayer.MinWidthShare, RouteLayer.HalfWidthStepShare,
            RouteLayer.RailSpanShare, RouteLayer.RailClearanceM, MapBuilder.VerticalExaggeration,
        };
        static int[] Ints => new[] { RouteLayer.Across, RouteLayer.NarrowSamples, RouteLayer.LiftSamples };

        /// <summary>The bake of the map's inputs: the terrain as MapBuilder.Build makes it (at the origin, as MapScene's), the
        /// routes as MapScene reads them, every plan derived.</summary>
        public static byte[] Bake(MapInputs inputs)
        {
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16z);
            var data = new TerrainData { heightmapResolution = meta.width };
            try
            {
                data.size = MapBuilder.TerrainSize(meta);
                data.SetHeights(0, 0, heights);
                var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, data.size.x, data.size.z);
                // As the map reads them (MapScene): the runtime file carries the corridors' segment ends (#792).
                var routes = RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), Debug.LogWarning);
                var plans = RouteLayer.Plan(routes);
                var ribbons = plans.ConvertAll(p => RouteLayer.Derive(p, frame, data, Vector3.zero));
                return Bytes(Source.Of(heights, inputs.HeightmapJson, inputs.RoutesYaml), data.size, Vector3.zero, plans, ribbons);
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        public static byte[] Bytes(Source source, Vector3 size, Vector3 origin, IReadOnlyList<RouteLayer.RibbonPlan> plans,
            IReadOnlyList<RibbonGeometry> ribbons)
        {
            if (plans.Count != ribbons.Count) throw new ArgumentException("one ribbon a plan", nameof(ribbons));
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            foreach (var v in new[] { Magic, Version, source.Resolution, source.HeightsCheck, source.MapCheck, source.RoutesCheck }) w.Write(v);
            foreach (var f in new[] { size.x, size.y, size.z, origin.x, origin.y, origin.z }) w.Write(f);
            foreach (var f in Floats) w.Write(f);
            foreach (var i in Ints) w.Write(i);
            w.Write(plans.Count);
            for (int m = 0; m < plans.Count; m++)
            {
                var g = ribbons[m];
                WriteString(w, plans[m].MeshName);
                w.Write(g.Vertices.Length); w.Write(g.Indices.Length); w.Write(g.Routes.Length);
                foreach (var s in g.Routes)
                {
                    WriteString(w, s.Id);
                    w.Write(s.FirstVertex); w.Write(s.VertexCount); w.Write(s.FirstIndex); w.Write(s.IndexCount);
                    w.Write(s.Segments.Length);
                    foreach (var start in s.Segments) w.Write(start);
                }
                var floats = new float[g.Vertices.Length * 8];
                for (int i = 0, n = g.Vertices.Length; i < n; i++)
                {
                    floats[3 * i] = g.Vertices[i].x; floats[3 * i + 1] = g.Vertices[i].y; floats[3 * i + 2] = g.Vertices[i].z;
                    floats[3 * n + 3 * i] = g.Normals[i].x; floats[3 * n + 3 * i + 1] = g.Normals[i].y; floats[3 * n + 3 * i + 2] = g.Normals[i].z;
                    floats[6 * n + 2 * i] = g.Uvs[i].x; floats[6 * n + 2 * i + 1] = g.Uvs[i].y;
                }
                var block = new byte[4 * (floats.Length + g.Indices.Length)];
                Buffer.BlockCopy(floats, 0, block, 0, 4 * floats.Length);
                Buffer.BlockCopy(g.Indices, 0, block, 4 * floats.Length, 4 * g.Indices.Length);
                w.Write(block);
            }
            w.Flush();
            return stream.ToArray();
        }

        static void WriteString(BinaryWriter w, string s)
        {
            var b = Encoding.UTF8.GetBytes(s);
            w.Write(b.Length); w.Write(b);
        }

        /// <summary>Reads the bake; false (and the reason) when it was not made from <paramref name="source"/>, for this
        /// terrain, with these constants and for these <paramref name="plans"/> (the same meshes, routes and order).</summary>
        public static bool TryRead(byte[]? bytes, Source source, Vector3 size, Vector3 origin, IReadOnlyList<RouteLayer.RibbonPlan> plans,
            out List<RibbonGeometry> ribbons, out string reason)
        {
            ribbons = new List<RibbonGeometry>();
            if (bytes == null || bytes.Length == 0) { reason = "missing"; return false; }
            var r = new Reader(bytes);
            try
            {
                if (r.Int() != Magic) { reason = "not a route ribbon bake"; return false; }
                int version = r.Int();
                if (version != Version) { reason = $"made by version {version}, this is {Version} (re-bake: RouteRibbonBake.Bake)"; return false; }
                int res = r.Int();
                if (res != source.Resolution) { reason = $"made for a {res} heightmap, this one is {source.Resolution}"; return false; }
                if (r.Int() != source.HeightsCheck) { reason = "made for other heights (re-bake: RouteRibbonBake.Bake)"; return false; }
                if (r.Int() != source.MapCheck) { reason = "made for another heightmap.json (re-bake: RouteRibbonBake.Bake)"; return false; }
                if (r.Int() != source.RoutesCheck) { reason = $"made for another {RoutePaths.DefaultFileName} (re-bake: RouteRibbonBake.Bake)"; return false; }
                var at = new Vector3(r.Float(), r.Float(), r.Float());
                var o = new Vector3(r.Float(), r.Float(), r.Float());
                if (!Same(at, size) || !Same(o, origin)) { reason = $"made for a terrain of size {at} at {o}, this one is {size} at {origin}"; return false; }
                foreach (var f in Floats)
                    if (BitConverter.SingleToInt32Bits(r.Float()) != BitConverter.SingleToInt32Bits(f)) { reason = "made with other widths or spacing (re-bake: RouteRibbonBake.Bake)"; return false; }
                foreach (var i in Ints)
                    if (r.Int() != i) { reason = "made with other sample counts (re-bake: RouteRibbonBake.Bake)"; return false; }
                int count = r.Int();
                if (count != plans.Count) { reason = $"holds {count} ribbons, the routes make {plans.Count}"; return false; }
                foreach (var plan in plans)
                {
                    var name = r.Text();
                    if (name != plan.MeshName) { reason = $"holds '{name}' where the routes make '{plan.MeshName}'"; return false; }
                    int nv = r.Count(), ni = r.Count(), nr = r.Count();
                    if (nr != plan.Routes.Count) { reason = $"'{name}' holds {nr} routes, the routes make {plan.Routes.Count}"; return false; }
                    var spans = new RouteSpan[nr];
                    for (int k = 0; k < nr; k++)
                    {
                        var id = r.Text();
                        if (id != plan.Routes[k].Id) { reason = $"'{name}' holds '{id}' where the routes have '{plan.Routes[k].Id}'"; return false; }
                        int fv = r.Int(), cv = r.Int(), fi = r.Int(), ci = r.Int();
                        var segments = r.Ints(r.Count());
                        // One start per segment and the route's end: the ends the routes give (RoutePath.SegmentEnds), each
                        // inside the route's range and in order, so a stale bake cannot cut a segment out of another route.
                        int want = plan.Routes[k].SegmentEnds?.Count ?? 0;
                        if (segments.Length != want) { reason = $"'{name}' cuts '{id}' at {segments.Length} points, the routes give {want} (re-bake: RouteRibbonBake.Bake)"; return false; }
                        for (int j = 0; j < segments.Length; j++)
                            if (segments[j] < fi || segments[j] > fi + ci || j > 0 && segments[j] < segments[j - 1]
                                || (j == 0 && segments[j] != fi) || (j == segments.Length - 1 && segments[j] != fi + ci))
                            { reason = $"'{name}' cuts '{id}' outside its range"; return false; }
                        spans[k] = new RouteSpan(id, fv, cv, fi, ci, segments);
                    }
                    var floats = r.Floats(8 * nv);
                    var verts = new Vector3[nv]; var normals = new Vector3[nv]; var uvs = new Vector2[nv];
                    for (int i = 0; i < nv; i++)
                    {
                        verts[i] = new Vector3(floats[3 * i], floats[3 * i + 1], floats[3 * i + 2]);
                        normals[i] = new Vector3(floats[3 * nv + 3 * i], floats[3 * nv + 3 * i + 1], floats[3 * nv + 3 * i + 2]);
                        uvs[i] = new Vector2(floats[6 * nv + 2 * i], floats[6 * nv + 2 * i + 1]);
                    }
                    var indices = r.Ints(ni);
                    foreach (var i in indices)
                        if ((uint)i >= (uint)nv) { reason = $"'{name}' indexes a vertex it does not hold"; return false; }
                    ribbons.Add(new RibbonGeometry(verts, normals, uvs, indices, spans));
                }
                if (!r.AtEnd) { reason = "longer than its meshes"; return false; }
            }
            catch (EndOfStreamException) { ribbons.Clear(); reason = "truncated"; return false; }
            reason = "";
            return true;
        }

        /// <summary>Equal, bit for bit (-0 and 0 apart: the bake writes what it was given).</summary>
        static bool Same(Vector3 a, Vector3 b) =>
            BitConverter.SingleToInt32Bits(a.x) == BitConverter.SingleToInt32Bits(b.x) && BitConverter.SingleToInt32Bits(a.y) == BitConverter.SingleToInt32Bits(b.y)
            && BitConverter.SingleToInt32Bits(a.z) == BitConverter.SingleToInt32Bits(b.z);

        /// <summary>Reads the file front to back; past its end it throws EndOfStreamException.</summary>
        sealed class Reader
        {
            readonly byte[] bytes; int at;
            public Reader(byte[] bytes) { this.bytes = bytes; }
            public bool AtEnd => at == bytes.Length;

            void Need(long n) { if (n < 0 || at + n > bytes.Length) throw new EndOfStreamException(); }
            public int Int() { Need(4); int v = BitConverter.ToInt32(bytes, at); at += 4; return v; }
            public float Float() { Need(4); float v = BitConverter.ToSingle(bytes, at); at += 4; return v; }
            /// <summary>A count: never negative.</summary>
            public int Count() { int n = Int(); if (n < 0) throw new EndOfStreamException(); return n; }
            public string Text() { int n = Count(); Need(n); var s = Encoding.UTF8.GetString(bytes, at, n); at += n; return s; }
            public float[] Floats(int n) { Need(4L * n); var a = new float[n]; Buffer.BlockCopy(bytes, at, a, 0, 4 * n); at += 4 * n; return a; }
            public int[] Ints(int n) { Need(4L * n); var a = new int[n]; Buffer.BlockCopy(bytes, at, a, 0, 4 * n); at += 4 * n; return a; }
        }
    }
}
