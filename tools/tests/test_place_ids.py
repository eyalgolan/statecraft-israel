"""Place ids are one namespace: the sim's settlements (content/map/settlements_1948.yaml), the map's 1948 localities
(content/map/localities_1948.yaml and its baked localities_1948.tsv; presentation only) and the designated sites
(content/sites.yaml, GDD Phase 1 PR 5, once it exists). The map keys its kits, footprints and populations by id over
the settlements and localities together (SettlementKits.Build, KitPopulation), and a built site becomes a place on the
same map, so an id may name one place only. settlement_hexes.yaml and settlement_population_1945.yaml key rows of the
settlements themselves: their ids are unique and each is a settlement. Sub-district keys, unit, route and armistice
ids are other namespaces (jaffa is a settlement and a sub-district on purpose).

Research note: localities_1948 already uses `sasa` for the Arab village Sa'sa', so a site `sasa` (kibbutz Sasa, 1949)
must take another id (research_notes/GDD objectives/settlements-1949-51.md)."""
from collections import Counter
from pathlib import Path

import pytest
import yaml12

ROOT = Path(__file__).resolve().parents[2]
SETTLEMENTS = "content/map/settlements_1948.yaml"
LOCALITIES = "content/map/localities_1948.yaml"
LOCALITIES_TSV = "content/map/localities_1948.tsv"
SITES = "content/sites.yaml"
KEYED_BY_SETTLEMENT = ["content/map/settlement_hexes.yaml", "content/map/settlement_population_1945.yaml"]


def yaml_ids(rel, key, root=ROOT):
    return [row["id"] for row in yaml12.load((root / rel).read_text(encoding="utf-8"))[key]]


def tsv_ids(rel, root=ROOT):
    rows = (line.split("\t") for line in (root / rel).read_text(encoding="utf-8").splitlines() if line and not line.startswith("#"))
    return [c[1] for c in rows if c[0] == "place"]


def clashes(lists):
    """Messages for each id repeated inside one file or used by two files; lists: [(file, ids)]. A file and its baked
    form (the localities' YAML and TSV) are passed as one entry, so they do not clash with each other."""
    out = [f"'{i}' appears {n} times in {f}" for f, ids in lists for i, n in sorted(Counter(ids).items()) if n > 1]
    for a, (fa, ia) in enumerate(lists):
        for fb, ib in lists[a + 1:]:
            out += [f"'{i}' is in both {fa} and {fb}" for i in sorted(set(ia) & set(ib))]
    return out


def place_lists(root=ROOT):
    lists = [(SETTLEMENTS, yaml_ids(SETTLEMENTS, "settlements", root)), (LOCALITIES, yaml_ids(LOCALITIES, "places", root))]
    if (root / SITES).exists():
        lists.append((SITES, yaml_ids(SITES, "sites", root)))
    return lists


def test_place_ids_are_unique_across_settlements_localities_and_sites():
    assert not clashes(place_lists()), "\n".join(clashes(place_lists()))


def test_the_localities_tsv_has_the_yaml_ids():
    assert tsv_ids(LOCALITIES_TSV) == yaml_ids(LOCALITIES, "places"), f"{LOCALITIES_TSV} is stale: rerun tools/localities_bake.py"


@pytest.mark.parametrize("rel", KEYED_BY_SETTLEMENT)
def test_rows_keyed_by_settlement_are_unique_settlement_ids(rel):
    ids = yaml_ids(rel, "settlements")
    assert not clashes([(rel, ids)]), "\n".join(clashes([(rel, ids)]))
    assert not set(ids) - set(yaml_ids(SETTLEMENTS, "settlements")), f"{rel} names a place that is not in {SETTLEMENTS}"


def test_sites_file_is_checked_when_present():
    if not (ROOT / SITES).exists():
        pytest.skip(f"{SITES} does not exist yet (GDD Phase 1 PR 5); its ids are checked once it does")
    assert SITES in [f for f, _ in place_lists()]


def test_a_site_reusing_a_locality_id_is_reported_naming_both_files(tmp_path):
    """The guard's failure, on a copy: a site `sasa` beside the locality Sa'sa', and a site listed twice."""
    for rel in (SETTLEMENTS, LOCALITIES):
        (tmp_path / rel).parent.mkdir(parents=True, exist_ok=True)
        (tmp_path / rel).write_text((ROOT / rel).read_text(encoding="utf-8"), encoding="utf-8")
    (tmp_path / SITES).write_text("sites:\n  - id: sasa\n  - id: new_site\n  - id: new_site\n", encoding="utf-8")
    assert clashes(place_lists(tmp_path)) == [f"'new_site' appears 2 times in {SITES}", f"'sasa' is in both {LOCALITIES} and {SITES}"]
