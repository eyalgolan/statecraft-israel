"""tools/regions_draft_reference.py (#1691): the approved draft's per-identity tuples, and nothing else, round-trip."""
import hashlib

import yaml

import regions_draft_reference as ref

DRAFT = """\
draft: test-draft
reception_site:
  name: Camp
  opening_control: { date: '1948-05-15', status: PARTIAL-EVIDENCE, value: null }
regions:
  - id: north
    name: North
    capital_place_id: town_a
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
    members:
      - id: town_a
        name: Town A
        region_id: north
        binding_status: SOURCED
        opening_control: { date: '1948-05-15', status: SOURCED, value: 'Held: "quoted", עברית, a: colon' }
        sources: [{ file: x }]
      - id: town_b
        region_id: north
        binding_status: NEEDS-RESEARCH
        opening_control: { status: PARTIAL-EVIDENCE, value: '' }
    hexes:
      - { q: 3, r: 4, opening_control: { status: NEEDS-RESEARCH, value: null } }
    facilities:
      - { status: SOURCED, opening_control: { status: SOURCED, value: held } }
"""


def test_reference_keeps_membership_and_control_and_drops_everything_else():
    raw = DRAFT.encode("utf-8")
    text = ref.reference(raw)
    out = yaml.safe_load(text)
    assert out["source"] == {"file": "docs/drafts/regions-1691.draft.yaml", "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
    assert out["regions"] == [{
        "id": "north", "capital_place_id": "town_a", "binding_status": "NEEDS-RESEARCH",
        "control_status": "NEEDS-RESEARCH", "control_value": None,
        "members": [
            {"id": "town_a", "region_id": "north", "binding_status": "SOURCED", "control_status": "SOURCED",
             "control_value": 'Held: "quoted", עברית, a: colon', "control_date": "1948-05-15"},
            {"id": "town_b", "region_id": "north", "binding_status": "NEEDS-RESEARCH", "control_status": "PARTIAL-EVIDENCE",
             "control_value": "", "control_date": None},
        ],
    }]
    assert out["ancillary_controls"] == [
        {"path": "reception_site", "status": "PARTIAL-EVIDENCE", "value": None, "date": "1948-05-15"},
        {"path": "regions/north/hexes/3,4", "status": "NEEDS-RESEARCH", "value": None, "date": None},
        {"path": "regions/north/facilities/0", "status": "SOURCED", "value": "held", "date": None},
    ]
    assert "Town A" not in text and "sources" not in text


def test_committed_reference_names_the_approved_draft():
    out = yaml.safe_load(ref.OUT.read_text(encoding="utf-8"))
    assert out["source"]["file"] == "docs/drafts/regions-1691.draft.yaml"
    assert out["source"]["sha256"] == "69e0586f6805bf97b49b64c4ee3fd6732afd128594939a9f92e8f5aa3113f125"
    assert sum(len(r["members"]) for r in out["regions"]) == 326
    assert len(out["ancillary_controls"]) == 50
