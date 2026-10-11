import json

import pytest

import terrain_graph as tg

SG = "UnityEditor.ShaderGraph."


def obj(t, oid, **kw):
    return {"m_SGVersion": 0, "m_Type": t, "m_ObjectId": oid, **kw}


def slot(kind, oid, sid, name, direction):
    o = obj(SG + kind + "MaterialSlot", oid, m_Id=sid, m_DisplayName=name, m_SlotType=direction, m_Hidden=False,
            m_ShaderOutputName=name, m_StageCapability=3)
    if kind.startswith("Vector"):
        n = int(kind[6]) if kind != "Vector1" else 1
        o["m_Value"] = 0.0 if n == 1 else {c: 0.0 for c in "xyzw"[:n]}
        o["m_DefaultValue"] = o["m_Value"]
    return o


def fixture() -> str:
    """A small stand-in for Terrain Simple.shadergraph: the same object kinds and links, fewer of them."""
    descriptors = ["VertexDescription.Position", "SurfaceDescription.BaseColor", "SurfaceDescription.NormalWS",
                   "SurfaceDescription.Metallic", "SurfaceDescription.Smoothness", "SurfaceDescription.Occlusion",
                   "SurfaceDescription.Emission"]
    objs, blocks = [], []
    for i, d in enumerate(descriptors):
        s = slot("Vector3" if i < 3 or i == 6 else "Vector1", f"bs{i}", i, d, 0)
        b = obj(SG + "BlockNode", f"b{i}", m_Name=d, m_Slots=[{"m_Id": s["m_ObjectId"]}], m_SerializedDescriptor=d)
        objs += [b, s]; blocks.append(b)
    extra_slots = [slot("Vector2", "x1", 0, "In", 0), slot("Vector4", "x2", 1, "Out", 1),
                   slot("Texture2D", "x3", 2, "Tex", 1), slot("Texture2DInput", "x4", 3, "TexIn", 0)]
    add = obj(SG + "AddNode", "add", m_Name="Add", m_Slots=[{"m_Id": s["m_ObjectId"]} for s in extra_slots])
    sticky = obj(SG + "StickyNoteData", "note", m_Title="sample")
    target = obj("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget", "urp", m_ActiveSubTarget={"m_Id": "terrain"},
                 m_CastShadows=False, m_ReceiveShadows=True)
    sub = obj("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTerrainLitSubTarget", "terrain")
    cat = obj(SG + "CategoryData", "cat", m_Name="", m_ChildObjectList=[])
    graph = obj(SG + "GraphData", "graph", m_Properties=[], m_Keywords=[], m_Dropdowns=[], m_CategoryData=[{"m_Id": "cat"}],
                m_Nodes=[{"m_Id": b["m_ObjectId"]} for b in blocks] + [{"m_Id": "add"}], m_GroupDatas=[],
                m_StickyNoteDatas=[{"m_Id": "note"}],
                m_Edges=[{"m_OutputSlot": {"m_Node": {"m_Id": "add"}, "m_SlotId": 1}, "m_InputSlot": {"m_Node": {"m_Id": "b1"}, "m_SlotId": 1}}],
                m_VertexContext={"m_Position": {"x": 0, "y": 0}, "m_Blocks": [{"m_Id": "b0"}]},
                m_FragmentContext={"m_Position": {"x": 0, "y": 100}, "m_Blocks": [{"m_Id": f"b{i}"} for i in range(1, 7)]},
                m_Path="Terrain", m_ActiveTargets=[{"m_Id": "urp"}])
    return "\n\n".join(json.dumps(o, indent=4) for o in [graph, cat, target, sub, add, *extra_slots, sticky, *objs]) + "\n"


GUID = "0123456789abcdef0123456789abcdef"


def built():
    objs = tg.split(tg.build(fixture(), GUID))
    return objs, {o["m_ObjectId"]: o for o in objs}


def test_sample_nodes_and_notes_go_and_the_blocks_stay():
    objs, by_id = built()
    graph = objs[0]
    kinds = [tg.kind(by_id[n["m_Id"]]) for n in graph["m_Nodes"]]
    assert kinds.count("BlockNode") == 7 and "AddNode" not in kinds
    assert sorted(set(kinds) - {"BlockNode"}) == ["CustomFunctionNode", "NormalVectorNode", "PositionNode", "PropertyNode", "UVNode"]
    assert graph["m_StickyNoteDatas"] == [] and "note" not in by_id and "add" not in by_id
    assert graph["m_Path"] == "Statecraft"


def test_every_fragment_block_is_fed_by_the_custom_function():
    objs, by_id = built()
    graph = objs[0]
    cf = next(o for o in objs if tg.kind(o) == "CustomFunctionNode")
    fed = {e["m_InputSlot"]["m_Node"]["m_Id"]: e for e in graph["m_Edges"] if e["m_OutputSlot"]["m_Node"]["m_Id"] == cf["m_ObjectId"]}
    for sid, _, _, descriptor in tg.OUTPUTS:
        block = next(o for o in objs if o.get("m_SerializedDescriptor") == descriptor)
        e = fed[block["m_ObjectId"]]
        assert e["m_OutputSlot"]["m_SlotId"] == sid
        assert e["m_InputSlot"]["m_SlotId"] == by_id[block["m_Slots"][0]["m_Id"]]["m_Id"]


def test_the_function_is_the_hlsl_file_by_guid_and_name_with_single_precision():
    objs, _ = built()
    cf = next(o for o in objs if tg.kind(o) == "CustomFunctionNode")
    # m_SourceType 0 is File (Shader Graph's HlslSourceType is { File, String }); String with an empty body would compile
    # to zero outputs without an error (review C1).
    assert (cf["m_SourceType"], cf["m_FunctionName"], cf["m_FunctionSource"], cf["m_Precision"]) == (0, "StatecraftTerrain", GUID, 1)
    assert tg.FILE_MODE == 0
    assert next(o for o in objs if tg.kind(o) == "PositionNode")["m_Precision"] == 1


def test_the_textures_are_blackboard_properties_with_the_shaders_names():
    objs, by_id = built()
    graph = objs[0]
    props = [by_id[p["m_Id"]] for p in graph["m_Properties"]]
    assert [p["m_DefaultReferenceName"] for p in props] == ["_ScBakeTex", "_ScCoastTex", "_ScAoTex", "_ScLandTex"]
    cat = next(o for o in objs if tg.kind(o) == "CategoryData")
    assert [c["m_Id"] for c in cat["m_ChildObjectList"]] == [p["m_ObjectId"] for p in props]
    assert next(o for o in objs if tg.kind(o) == "UniversalTarget")["m_CastShadows"] is True


def test_every_edge_is_wired_to_existing_slots_and_the_output_is_deterministic():
    text = tg.build(fixture(), GUID)
    assert text == tg.build(fixture(), GUID)
    objs = tg.split(text)
    by_id = {o["m_ObjectId"]: o for o in objs}
    assert len(by_id) == len(objs), "object ids are unique"
    for e in objs[0]["m_Edges"]:
        for end in ("m_OutputSlot", "m_InputSlot"):
            node = by_id[e[end]["m_Node"]["m_Id"]]
            assert e[end]["m_SlotId"] in [by_id[s["m_Id"]]["m_Id"] for s in node["m_Slots"]]
    with pytest.raises(ValueError, match="slot prototypes"):
        tg.build(fixture().replace("Texture2DInputMaterialSlot", "SomethingElse"), GUID)


def test_every_added_node_and_the_graph_run_at_single_precision():
    # Review I2: the template's graph precision is Half (2); a node that inherits it (the UV node has no inputs) would
    # compute the terrain UV as mediump on GLES3 phones, steps of about 210 m north-south.
    objs, _ = built()
    assert objs[0]["m_GraphPrecision"] == tg.GRAPH_SINGLE == 0
    for k in ("UVNode", "PositionNode", "NormalVectorNode", "PropertyNode", "CustomFunctionNode"):
        assert all(o["m_Precision"] == 1 for o in objs if tg.kind(o) == k), k


def test_only_the_universal_target_is_kept():
    # Review M3: the template also carries an HDRP target and its data; this project is URP-only.
    text = fixture_with_hdrp()
    objs = tg.split(tg.build(text, GUID))
    assert not [o for o in objs if "HighDefinition" in o["m_Type"]]
    targets = [o["m_Id"] for o in objs[0]["m_ActiveTargets"]]
    by_id = {o["m_ObjectId"]: o for o in objs}
    assert [tg.kind(by_id[t]) for t in targets] == ["UniversalTarget"]


def fixture_with_hdrp() -> str:
    objs = tg.split(fixture())
    hd_sub = obj("UnityEditor.Rendering.HighDefinition.ShaderGraph.TerrainLitSubTarget", "hdsub")
    hd = obj("UnityEditor.Rendering.HighDefinition.ShaderGraph.HDTarget", "hd", m_ActiveSubTarget={"m_Id": "hdsub"})
    objs[0]["m_ActiveTargets"].append({"m_Id": "hd"})
    objs[0]["m_GraphPrecision"] = 2
    return tg.join(objs + [hd, hd_sub])
