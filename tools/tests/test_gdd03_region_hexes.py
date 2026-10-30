import gdd03_region_hexes as regions


def sample():
    evidence = {"regions": [
        {"id": "north", "reference_subdistricts": ["Acre"], "members": []},
        {"id": "south", "reference_subdistricts": ["Gaza"], "members": []},
    ]}
    subdistricts = {
        "regions": [{"id": 1, "key": "acre"}, {"id": 2, "key": "gaza"}, {"id": 3, "key": "outside"}],
        "hexes": [[3, 1, 1], [1, 2, 1], [0, 0, 2], [9, 9, 3]],
    }
    return evidence, subdistricts


def test_memberships_use_runtime_q_then_r_order_and_report_unassigned():
    evidence, subdistricts = sample()
    by_region, unassigned, doubled = regions.memberships(evidence, subdistricts)
    assert by_region == {"north": [(1, 2), (3, 1)], "south": [(0, 0)]}
    assert unassigned == [(9, 9)]
    assert doubled == []


def test_replacement_changes_only_empty_member_hex_fields():
    evidence, subdistricts = sample()
    by_region, _, _ = regions.memberships(evidence, subdistricts)
    original = """# retained\nplayer_regions:\n- id: north\n  capital_location: a\n  member_hexes: []\n  sources: [x]\n- id: south\n  capital_location: b\n  member_hexes: []\n  sources: [y]\n"""
    generated = regions.replace_member_hex_fields(original, by_region)
    assert generated == """# retained\nplayer_regions:\n- id: north\n  capital_location: a\n  member_hexes:\n  - { q: 1, r: 2 }\n  - { q: 3, r: 1 }\n  sources: [x]\n- id: south\n  capital_location: b\n  member_hexes:\n  - { q: 0, r: 0 }\n  sources: [y]\n"""
    assert regions.replace_member_hex_fields(generated, by_region) == generated


def test_duplicate_subdistrict_claim_is_rejected():
    evidence, subdistricts = sample()
    evidence["regions"][1]["reference_subdistricts"] = ["Acre"]
    try:
        regions.memberships(evidence, subdistricts)
    except ValueError as error:
        assert "claimed by north and south" in str(error)
    else:
        raise AssertionError("duplicate sub-district claim was accepted")


def test_placed_member_mismatch_is_reported_not_reassigned():
    evidence, subdistricts = sample()
    evidence["regions"][0]["members"] = [
        {"id": "border_place", "region_id": "north", "hex": {"q": 0, "r": 0}},
    ]
    by_region, _, _ = regions.memberships(evidence, subdistricts)
    assert regions.placed_member_mismatches(evidence, by_region) == [
        ("north", "border_place", 0, 0, "south"),
    ]
