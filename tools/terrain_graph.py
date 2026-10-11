# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Writes unity/Assets/Shaders/StatecraftTerrain.shadergraph (shader "Statecraft/StatecraftTerrain"): Unity's own
"Terrain Simple" Terrain Lit template with its sample nodes, groups, notes and HDRP target removed, and one Custom
Function node (File mode, m_SourceType 0: StatecraftTerrain.hlsl, function StatecraftTerrain; the graph and every added
node at single precision) that takes the terrain UV, the absolute world position and normal and three blackboard
textures (_ScBakeTex, _ScCoastTex, _ScAoTex, _ScLandTex) and feeds every fragment block. All shading lives in the HLSL file;
re-run this only when the node's inputs or outputs change. Slot objects are copied from the template's own slots, so
they match the installed Shader Graph version. Object ids are UUID5s: the output is deterministic.

Usage: uv run tools/terrain_graph.py
Needs unity/Library/PackageCache (open the project in Unity once) and unity/Assets/Shaders/StatecraftTerrain.hlsl.meta."""
import json
import re
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE_GLOB = "unity/Library/PackageCache/com.unity.shadergraph@*/GraphTemplates/Cross Pipeline/Terrain Simple.shadergraph"
HLSL_META = ROOT / "unity" / "Assets" / "Shaders" / "StatecraftTerrain.hlsl.meta"
OUT = ROOT / "unity" / "Assets" / "Shaders" / "StatecraftTerrain.shadergraph"
GRAPH_PATH = "Statecraft"
FUNCTION = "StatecraftTerrain"
NS = uuid.UUID("5b0c1f7e-2f61-4c3a-9a57-7a1d1e0c6f42")
SG = "UnityEditor.ShaderGraph."
SINGLE = 1      # Precision.Single (node precision)
GRAPH_SINGLE = 0   # GraphPrecision.Single (the template's is Half, 2: an inheriting node would run at mediump on GLES3)
URP = "UnityEditor.Rendering.Universal."
FILE_MODE = 0   # HlslSourceType.File (Shader Graph: enum HlslSourceType { File, String })
# display name, reference, function slot; the AO is its own uncompressed texture (Task 6 fix round 1: BC1 banded it); the
# land use (tools/land_bake.py, issue #33) came later, so its slot id follows the outputs' and those stay unchanged
TEXTURES = [("ScBakeTex", "_ScBakeTex", 3), ("ScCoastTex", "_ScCoastTex", 4), ("ScAoTex", "_ScAoTex", 5), ("ScLandTex", "_ScLandTex", 12)]
INPUTS = [(0, "UV", "Vector2"), (1, "PositionWS", "Vector3"), (2, "NormalWS", "Vector3"),
          (3, "Bake", "Texture2DInput"), (4, "Coast", "Texture2DInput"), (5, "Ao", "Texture2DInput"), (12, "Land", "Texture2DInput")]
OUTPUTS = [(6, "BaseColor", "Vector3", "SurfaceDescription.BaseColor"), (7, "NormalOut", "Vector3", "SurfaceDescription.NormalWS"),
           (8, "Occlusion", "Vector1", "SurfaceDescription.Occlusion"), (9, "Smoothness", "Vector1", "SurfaceDescription.Smoothness"),
           (10, "Metallic", "Vector1", "SurfaceDescription.Metallic"), (11, "Emission", "Vector3", "SurfaceDescription.Emission")]
SLOT_KINDS = {"Vector1MaterialSlot", "Vector2MaterialSlot", "Vector3MaterialSlot", "Vector4MaterialSlot",
              "Texture2DMaterialSlot", "Texture2DInputMaterialSlot"}


def oid(name: str) -> str:
    return uuid.uuid5(NS, name).hex


def split(text: str) -> list[dict]:
    return [json.loads(o) for o in re.split(r"\n\n(?=\{)", text.strip())]


def join(objs: list[dict]) -> str:
    return "\n\n".join(json.dumps(o, indent=4) for o in objs) + "\n"


def kind(o: dict) -> str:
    return o["m_Type"].rsplit(".", 1)[-1]


def make_slot(protos: dict, k: str, sid: int, name: str, direction: int, owner: str) -> dict:
    o = json.loads(json.dumps(protos[k + "MaterialSlot"]))
    o.update({"m_ObjectId": oid(f"slot:{owner}:{sid}"), "m_Id": sid, "m_DisplayName": name, "m_SlotType": direction,
              "m_ShaderOutputName": name, "m_Hidden": False})
    for key in ("m_Value", "m_DefaultValue"):
        if isinstance(o.get(key), dict):
            o[key] = {c: 0.0 for c in o[key]}
        elif isinstance(o.get(key), (int, float)):
            o[key] = 0.0
    return o


def make_node(type_name: str, name: str, slots: list[dict], extra: dict, x: float, y: float, version: int = 0, precision: int = 0) -> dict:
    return {"m_SGVersion": version, "m_Type": SG + type_name, "m_ObjectId": oid("node:" + name), "m_Group": {"m_Id": ""},
            "m_Name": name, "m_DrawState": {"m_Expanded": True, "m_Position": {"serializedVersion": "2", "x": x, "y": y,
                                                                                  "width": 220.0, "height": 130.0}},
            "m_Slots": [{"m_Id": s["m_ObjectId"]} for s in slots], "synonyms": [], "m_Precision": precision,
            "m_PreviewExpanded": False, "m_DismissedVersion": 0, "m_PreviewMode": 0,
            "m_CustomColors": {"m_SerializableColors": []}, **extra}


def edge(a: dict, a_slot: int, b: dict, b_slot: int) -> dict:
    return {"m_OutputSlot": {"m_Node": {"m_Id": a["m_ObjectId"]}, "m_SlotId": a_slot},
            "m_InputSlot": {"m_Node": {"m_Id": b["m_ObjectId"]}, "m_SlotId": b_slot}}


def build(template_text: str, hlsl_guid: str) -> str:
    objs = split(template_text)
    by_id = {o["m_ObjectId"]: o for o in objs}
    graph = objs[0]
    if kind(graph) != "GraphData":
        raise ValueError("the template's first object is not its GraphData")
    protos: dict[str, dict] = {}
    for o in objs:
        if kind(o) in SLOT_KINDS:
            protos.setdefault(kind(o), o)
    if SLOT_KINDS - protos.keys():
        raise ValueError(f"the template lacks slot prototypes: {sorted(SLOT_KINDS - protos.keys())}")
    block_ids = [b["m_Id"] for ctx in ("m_VertexContext", "m_FragmentContext") for b in graph[ctx]["m_Blocks"]]
    blocks = {by_id[i]["m_SerializedDescriptor"]: by_id[i] for i in block_ids}

    # Only the Universal target and its sub-target: the template's HDRP target is dead weight in this URP project.
    keep = [graph] + [o for o in objs[1:] if o["m_Type"].startswith(URP) or kind(o) == "CategoryData"]
    for i in block_ids:
        keep.append(by_id[i])
        keep += [by_id[s["m_Id"]] for s in by_id[i]["m_Slots"]]
    for o in keep:
        if kind(o) == "UniversalTarget":
            o["m_CastShadows"] = True       # the relief casts the map-scale shadows (plan Task 2)
            o["m_ReceiveShadows"] = True

    uv_out = make_slot(protos, "Vector4", 0, "Out", 1, "uv")
    uv = make_node("UVNode", "UV", [uv_out], {"m_OutputChannel": 0}, -900, 0, precision=SINGLE)
    pos_out = make_slot(protos, "Vector3", 0, "Out", 1, "position")
    pos = make_node("PositionNode", "Position", [pos_out], {"m_Space": 4, "m_PositionSource": 0}, -900, 150, version=1, precision=SINGLE)
    nrm_out = make_slot(protos, "Vector3", 0, "Out", 1, "normal")
    nrm = make_node("NormalVectorNode", "Normal Vector", [nrm_out], {"m_Space": 2}, -900, 300, precision=SINGLE)
    added = [uv, uv_out, pos, pos_out, nrm, nrm_out]
    props, prop_nodes = [], []
    for display, ref, _ in TEXTURES:
        p = {"m_SGVersion": 0, "m_Type": SG + "Internal.Texture2DShaderProperty", "m_ObjectId": oid("prop:" + ref),
             "m_Guid": {"m_GuidSerialized": str(uuid.uuid5(NS, "guid:" + ref))}, "m_Name": display,
             "m_DefaultRefNameVersion": 1, "m_RefNameGeneratedByDisplayName": display, "m_DefaultReferenceName": ref,
             "m_OverrideReferenceName": "", "m_GeneratePropertyBlock": True, "m_UseCustomSlotLabel": False,
             "m_CustomSlotLabel": "", "m_DismissedVersion": 0, "m_Precision": 0, "overrideHLSLDeclaration": False,
             "hlslDeclarationOverride": 0, "m_Hidden": False, "m_PerRendererData": False, "m_customAttributes": [],
             "m_Value": {"m_SerializedTexture": "{\"texture\":{\"instanceID\":0}}", "m_Guid": ""},
             "isMainTexture": False, "useTilingAndOffset": False, "useTexelSize": False, "m_Modifiable": True, "m_DefaultType": 0}
        out = make_slot(protos, "Texture2D", 0, "Out", 1, "prop:" + ref)
        pn = make_node("PropertyNode", display, [out], {"m_Property": {"m_Id": p["m_ObjectId"]}}, -900, 450 + 120 * len(props),
                       precision=SINGLE)
        props.append(p)
        prop_nodes.append(pn)
        added += [p, pn, out]
    cf_slots = [make_slot(protos, k, sid, name, 0, "cf") for sid, name, k in INPUTS] + \
               [make_slot(protos, k, sid, name, 1, "cf") for sid, name, k, _ in OUTPUTS]
    cf = make_node("CustomFunctionNode", "StatecraftTerrain (Custom Function)", cf_slots,
                   {"m_SourceType": FILE_MODE, "m_FunctionName": FUNCTION, "m_FunctionSource": hlsl_guid,
                    "m_FunctionSourceUsePragmas": True, "m_FunctionBody": ""}, -450, 100, version=1, precision=SINGLE)
    added += [cf, *cf_slots]

    edges = [edge(uv, 0, cf, 0), edge(pos, 0, cf, 1), edge(nrm, 0, cf, 2)]
    edges += [edge(pn, 0, cf, sid) for pn, (_, _, sid) in zip(prop_nodes, TEXTURES)]
    for sid, _, _, descriptor in OUTPUTS:
        if descriptor not in blocks:
            raise ValueError(f"the template has no {descriptor} block")
        block = blocks[descriptor]
        edges.append(edge(cf, sid, block, by_id[block["m_Slots"][0]["m_Id"]]["m_Id"]))

    graph.update(m_Path=GRAPH_PATH, m_GraphPrecision=GRAPH_SINGLE,
                 m_ActiveTargets=[{"m_Id": o["m_ObjectId"]} for o in keep if kind(o) == "UniversalTarget"], m_Properties=[{"m_Id": p["m_ObjectId"]} for p in props], m_Keywords=[], m_Dropdowns=[],
                 m_GroupDatas=[], m_StickyNoteDatas=[], m_Edges=edges,
                 m_Nodes=[{"m_Id": i} for i in block_ids] + [{"m_Id": n["m_ObjectId"]} for n in (uv, pos, nrm, *prop_nodes, cf)])
    for o in keep:
        if kind(o) == "CategoryData":
            o["m_ChildObjectList"] = [{"m_Id": p["m_ObjectId"]} for p in props]
    return join(keep + added)


def main() -> int:
    templates = sorted(ROOT.glob(TEMPLATE_GLOB))
    if not templates:
        print(f"no template at {TEMPLATE_GLOB}: open the project in Unity once so the package cache exists")
        return 1
    m = re.search(r"^guid: ([0-9a-f]{32})$", HLSL_META.read_text(), re.M) if HLSL_META.exists() else None
    if m is None:
        print(f"{HLSL_META} missing: import StatecraftTerrain.hlsl in Unity first")
        return 1
    OUT.write_text(build(templates[-1].read_text(), m.group(1)))
    print(f"wrote {OUT} from {templates[-1].relative_to(ROOT)} (function {FUNCTION} in {m.group(1)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
