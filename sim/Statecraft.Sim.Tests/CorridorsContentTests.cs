using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/corridors.yaml (GDD Phase 4 PR 2, #786): each corridor's kind, names and segments, each an ordered list
/// of land hexes on the map, one step at a time, and each segment starting where the last one ends. The hexes are derived
/// from the traced route by tools/corridor_hexes.py; tools/tests/test_corridor_hexes.py checks they match it (Review focus
/// 3). The Money and turns are design values in rules.yaml mvp: (Q-COR-6). Only the GDD loop reads the file.</summary>
public class CorridorsContentTests
{
    static Content Load(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

    [Fact]
    public void TheGddFixtureLoadsItsCorridorsAndRules()
    {
        var c = Load();
        Assert.Equal(new[] { "road_a", "rail_a" }, c.Corridors.Select(x => x.Id));
        var road = c.Corridors[0];
        Assert.Equal(("road", "Road A", "כביש א", 2), (road.Kind, road.Name, road.NameHe, road.Segments.Count));
        Assert.Equal(new[] { new[] { 2, 0 }, new[] { 3, 0 } }, road.Segments[0].Hexes.Select(h => h.ToArray()));
        var m = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((3, 1.5, 2, 3.0), (m.RoadSegmentTurns, m.RoadSegmentCost, m.RailSegmentTurns, m.RailSegmentCost));
    }

    [Fact]
    public void TheLegacyLoopReadsNoCorridors()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["corridors.yaml"] = Fixture.GddCorridors }));
        Assert.Empty(c.Corridors);
    }

    /// <summary>The owner's answers of 17 October 2026 (#782): the Burma Road on its traced line, one segment between each pair
    /// of its five waypoints (Q-COR-1, Q-COR-2), and the 1949 rail link as the Yarkon link alone (Q-RAIL-1); and of 18 October
    /// 2026 (#786, Q-COR-3 (b)): the Tel Aviv–Netanya road as straight draft lines between its five sourced places, four
    /// segments.</summary>
    [Fact]
    public void TheMvpHasTheBurmaRoadTheRailLinkAndTheNetanyaRoad()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(new[] { ("burma_road", "road", 4), ("tlv_haifa_rail", "rail", 1), ("tlv_netanya_road", "road", 4) },
                     c.Corridors.Select(x => (x.Id, x.Kind, x.Segments.Count)));
        var m = c.Rules.Mvp!;
        Assert.Equal((6, 2.0, 6, 4.0), (m.RoadSegmentTurns, m.RoadSegmentCost, m.RailSegmentTurns, m.RailSegmentCost));   // Q-COR-6
    }

    [Fact]
    public void SegmentsAreContiguousLandHexes()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        foreach (var x in c.Corridors)
        {
            int[]? prev = null;
            foreach (var seg in x.Segments)
            {
                Assert.NotEmpty(seg.Hexes);
                foreach (var h in seg.Hexes) Assert.True(c.HexAt(h[0], h[1])?.Land, $"{x.Id}: ({h[0]}, {h[1]})");
                foreach (var h in seg.Hexes)
                {
                    if (prev != null) Assert.True(MapActions.Distance(prev[0], prev[1], h[0], h[1]) <= 1, $"{x.Id}: a gap before ({h[0]}, {h[1]})");
                    prev = h.ToArray();
                }
            }
        }
    }

    [Fact]
    public void TheCorridorsFileHoldsNoBalanceValue()
    {
        var text = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "corridors.yaml"));
        Assert.DoesNotMatch(@"(?m)^[^#\n]*\b\w*(turns|cost)\w*\s*:", text);
    }

    static string Cor(string replace, string with) => Fixture.GddCorridors.Replace(replace, with);
    static string Rules(string mvp) => Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts) + mvp;

    public static IEnumerable<object[]> BadRows() => new List<object[]>
    {
        new object[] { "corridors.yaml", Cor("id: rail_a", "id: road_a"), "corridors[road_a].id", "duplicate id" },
        new object[] { "corridors.yaml", Cor("kind: rail", "kind: canal"), "corridors[rail_a].kind", "unknown kind 'canal' (road, rail)" },
        new object[] { "corridors.yaml", Cor("name: Road A", "name: ' '"), "corridors[road_a].name", "required" },
        new object[] { "corridors.yaml", Cor("name_he: כביש א", "name_he: ''"), "corridors[road_a].name_he", "required" },
        new object[] { "corridors.yaml", Cor("route: road_a", "route: ''"), "corridors[road_a].route", "required" },
        new object[] { "corridors.yaml", Cor("segments: [{ to: Y, hexes: [[2, 1], [3, 1]] }]", "segments: []"), "corridors[rail_a].segments", "at least one segment" },
        new object[] { "corridors.yaml", Cor("hexes: [[2, 1], [3, 1]]", "hexes: []"), "corridors[rail_a].segments[1].hexes", "at least one hex" },
        new object[] { "corridors.yaml", Cor("hexes: [[2, 1], [3, 1]]", "hexes: [[2, 1], [3]]"), "corridors[rail_a].segments[1].hexes", "[q, r] pairs" },
        new object[] { "corridors.yaml", Cor("hexes: [[2, 1], [3, 1]]", "hexes: [[0, 0], [1, 0]]"), "corridors[rail_a].segments[1].hexes", "hex q=0 r=0 is not a land hex" },
        new object[] { "corridors.yaml", Cor("hexes: [[2, 1], [3, 1]]", "hexes: [[1, 1], [3, 1]]"), "corridors[rail_a].segments[1].hexes", "hex q=3 r=1 is not next to q=1 r=1" },
        new object[] { "corridors.yaml", Cor("hexes: [[3, 0], [4, 0]]", "hexes: [[1, 1]]"), "corridors[road_a].segments[2].hexes", "hex q=1 r=1 is not next to q=3 r=0" },
        new object[] { "corridors.yaml", Cor("    status: draft\n", "    status: done\n"), "corridors[road_a].status", "unknown status 'done'" },
        new object[] { "corridors.yaml", Cor(", checked: 2026-10-15", ""), "corridors[road_a].sources[0]", "url and checked date" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("road_segment_turns: 3", "road_segment_turns: 0")), "mvp.road_segment_turns", "must be >= 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("rail_segment_turns: 2", "rail_segment_turns: 0")), "mvp.rail_segment_turns", "must be >= 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("road_segment_cost: 1.5", "road_segment_cost: -1")), "mvp.road_segment_cost", "must be a finite number >= 0" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("rail_segment_cost: 3", "rail_segment_cost: .nan")), "mvp.rail_segment_cost", "must be a finite number >= 0" },
    };

    [Theory]
    [MemberData(nameof(BadRows))]
    public void ABadValueFailsNamingTheFileAndField(string file, string text, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { [file] = text }));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void TheGddLoopNeedsTheCorridorsFile()
    {
        var o = Fixture.Gdd();
        o.Remove("corridors.yaml");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(o)));
        Assert.Equal("corridors.yaml", ex.File);
        Assert.Contains("missing file", ex.Message);
    }
}
