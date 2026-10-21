"""Checks content/map/localities_1948.yaml (the 1948 localities and Bedouin camp areas the map's kits and labels draw;
presentation only) against hexes.json and settlements_1948.yaml."""
import json
import re
from pathlib import Path

import pytest
import yaml
import yaml12

import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}
DOC = yaml12.load((MAP / "localities_1948.yaml").read_text())
PLACES = DOC["places"]
CAMPS = DOC["encampments"]
GAME_IDS = {s["id"] for s in yaml12.load((MAP / "settlements_1948.yaml").read_text())["settlements"]}
TYPES = {"town", "kibbutz", "moshav", "arab_town", "village"}
KINDS = {"arab_town": "arab_town", "arab_village": "village", "jewish_town": "town", "moshava": "town",
         "kibbutz": "kibbutz", "moshav": "moshav", "mixed_locality": "town"}
SUBDISTRICTS = yaml12.load((MAP / "subdistrict_hexes_1948.yaml").read_text())
NO_TODAY = {line.strip() for line in (ROOT / "tools" / "tests" / "data" / "localities_no_today_qids.txt").read_text().splitlines()
            if line.strip() and not line.startswith("#")}
LANDMARKS = {"mosque", "church", "none", "water_tower", "mosque_and_water_tower"}


VS_URL = "https://en.wikipedia.org/w/index.php?title=Village_Statistics,_1945&oldid=1359600351"


def sourced(row):
    """A row's sources are URLs read on its checked date, from the hosts the header names (Wikidata CC0, Wikipedia
    CC BY-SA 4.0), the Village Statistics revision first."""
    assert row["status"] == "draft" and str(row["checked"]) == "2026-10-11"
    assert row["sources"] and row["sources"][0] == VS_URL
    assert all(u.startswith(("https://www.wikidata.org/wiki/Q", "https://en.wikipedia.org/")) for u in row["sources"])


def test_scope_is_the_1000_plus_places_beside_the_game_settlements():
    ids = [p["id"] for p in PLACES]
    assert len(ids) == len(set(ids)) == 271   # 293 less the 20 game settlements, the Jahalin tribe row (camps) and Huj
    assert not set(ids) & GAME_IDS, "a game settlement keeps its settlements_1948.yaml entry"
    assert all(p["population"] >= 1000 for p in PLACES)
    assert [p["population"] for p in PLACES] == sorted((p["population"] for p in PLACES), reverse=True)


# The 52 rows that carried owner_verify until the owner cleared them on 2026-10-18 (#200): #488 checked them against
# the Village Statistics 1945 scan and #490 applied the corrections. A new low/manual/multi-place row still needs the flag.
OWNER_CLEARED = {
    "abasan_al_kabira", "al_bireh", "al_majdal", "al_manshiyya_acre", "al_naima", "anabta", "ashdot_yaakov",
    "awarta", "az_zakariyya", "azzoun", "bani_suheila", "bayt_nuba", "beit_shean", "biddya", "binyamina",
    "bnei_brak", "burin", "caesarea", "dayr_al_qassi", "deir_al_ghusun", "fajja", "hunin", "ijzim", "ilar",
    "iraq_al_manshiyya", "jabalia", "kfar_ganim", "khan_yunis", "kiryat_ata", "magdiel", "maghar", "naana", "nesher",
    "ness_ziona", "pardes_hanna_karkur", "qabalan", "qaffin", "rafah", "sair", "saliha", "sepphoris", "shefa_amr",
    "shwiaka_tulkarm", "silat_adh_dhahr", "sur_bahir", "tarbikha", "tarqumiyah", "tubas", "umm_al_fahm", "yabad",
    "yafa_an_naseriyye", "yagur",
}


@pytest.mark.parametrize("p", PLACES, ids=lambda p: p["id"])
def test_place_is_sourced_named_and_on_land(p):
    sourced(p)
    assert re.fullmatch(r"[a-z][a-z0-9_]*", p["id"])
    assert p["name"].strip() and p["name_basis"] in ("vs1945", "hebrew")
    if "today" in p:
        assert p["today"]["name"].strip() and p["today"]["source"].startswith("https://"), "a today name carries its source"
    assert p["type"] in TYPES and KINDS[p["kind"]] == p["type"] and p["landmark"] in LANDMARKS
    multi = any(" and " in x.rsplit(" (", 1)[0] for x in p["listed_as"])
    if (p["match"] in ("low", "manual") or multi) and p["id"] not in OWNER_CLEARED:
        assert p["owner_verify"], "low and manual matches, and VS rows that name more than one place, stay owner-verify"
    assert f"https://www.wikidata.org/wiki/{p['wikidata']}" in p["sources"]
    b = HEXES["bounds"]
    assert b["lon0"] <= p["lon"] <= b["lon1"] and b["lat0"] <= p["lat"] <= b["lat1"]
    q, r = hg.settlement_hex(p["lon"], p["lat"], HEXES)
    assert BY_QR[(q, r)]["land"], f"{p['id']} on {BY_QR[(q, r)]['biome']} hex ({q},{r})"


def test_names_are_1948_names():
    by = {p["id"]: p for p in PLACES}
    assert by["beit_shean"]["name"] == "Beisan"
    assert (by["al_majdal"]["name"], by["al_majdal"]["today"]["name"]) == ("Majdal", "Ashkelon")
    assert by["beit_shean"]["today"]["name"] == "Beit She'an"
    assert by["al_abbasiyya"]["name"] == "'Abbasiyya"
    assert by["kiryat_ata"]["name"] == "Kfar Ata"
    for p in PLACES:   # the VS spellings' marks are gone: plain letters and an apostrophe for ayn
        assert not re.search(r"[‘’āīūḥṭ]|, El$", p["name"]), p["name"]
        assert "(" not in p["name"], p["name"]


def flagged_ids(places):
    return [p["id"] for p in places if "name_he_review" in p]


def test_hebrew_names_are_the_1948_forms_with_their_sources():
    """#558, owner 2026-10-14: name_he is the 1948 form matching the English name. Where it is not the place's Wikidata
    label (a row source), it carries name_he_sources: an https he.wiki page, read on the day given. The places with no
    Hebrew source have no name_he, and the runtime file carries name_he for the map."""
    by = {p["id"]: p for p in PLACES}
    assert {i: by[i]["name_he"] for i in ("beit_shean", "kiryat_ata", "pardes_hanna_karkur", "tarshiha", "al_majdal", "caesarea")} == {
        "beit_shean": "ביסאן", "kiryat_ata": "כפר אתא", "pardes_hanna_karkur": "פרדס חנה", "tarshiha": "תרשיחא",
        "al_majdal": "אל-מג'דל", "caesarea": "קיסריה"}
    with_sources = {p["id"] for p in PLACES if "name_he_sources" in p}
    assert with_sources == {"beit_shean", "kiryat_ata", "pardes_hanna_karkur", "tarshiha", "al_majdal"}
    # The review flag (#564, owner 2026-10-15): agent-checked until a native speaker reads the name, then removed.
    assert all(by[i]["name_he"] and by[i]["name_he_review"] == "agent-checked" for i in flagged_ids(PLACES))
    for i in with_sources:
        assert by[i]["name_he"], i
        for s in by[i]["name_he_sources"]:
            assert set(s) == {"url", "checked"} and s["url"].startswith("https://he.wikipedia.org/") and str(s["checked"]) == "2026-10-14"
    assert {p["id"] for p in PLACES if not p.get("name_he")} == {
        "naana", "al_salihiyya", "karatiyya", "simsim_gaza", "niilya", "al_naima", "al_jiyya", "al_mansi", "az_zakariyya",
        "al_qubayba_hebron", "al_zuq_al_tahtani", "al_sawafir_al_gharbiyya", "al_ghazzawiyya"}
    import localities_bake
    rows = {r.split("\t")[1]: r.split("\t") for r in localities_bake.bake((MAP / "localities_1948.yaml").read_text(encoding="utf-8")).splitlines()
            if r.startswith("place\t")}
    assert rows["beit_shean"][localities_bake.PLACE_COLUMNS.index("name_he")] == "ביסאן"
    assert rows["naana"][localities_bake.PLACE_COLUMNS.index("name_he")] == ""


def test_start_of_game_only_no_later_fate():
    text = yaml.safe_dump(DOC, allow_unicode=True).lower()   # the rows, not the header that states the rule
    for word in ("depopulat", "destroy", "expel", "fled", "abandon", "massacre", "evacuat", "occupied"):
        assert word not in text, word
    keys = {k for p in PLACES for k in p} | {k for c in CAMPS for k in c}
    assert not keys & {"fate", "end", "ended", "depopulated", "until", "status_after"}


def test_no_place_that_did_not_carry_on_has_a_today_name():
    """Owner ruling 2026-10-12: "Today:" only where a living place carries on. A place whose Wikidata item is classed a
    depopulated village or a former settlement (tools/tests/data/localities_no_today_qids.txt) has no today name; the
    exceptions are the owner's own example, Majdal, whose item is today's city of Ashkelon, and Sha'b, whose item is also
    the living local council of Sha'ab (Q2655669 is classed both; coordinator ruling 2026-10-12 under the owner's rule).
    Walaja, Samakh and Lubiya, classed both as well, have no living successor and stay without one."""
    exceptions = {"al_majdal", "shaab"}
    offending = [p["id"] for p in PLACES if p["wikidata"] in NO_TODAY and "today" in p and p["id"] not in exceptions]
    assert not offending
    assert sum(1 for p in PLACES if p["wikidata"] in NO_TODAY) >= 100


def test_shafa_amr_deir_el_qasi_nesher_review_fixes():
    by = {p["id"]: p for p in PLACES}
    assert (by["shefa_amr"]["population"], by["shefa_amr"]["landmark"]) == (3640, "church")   # the Urban row only (review I1)
    assert by["dayr_al_qassi"]["landmark"] == "mosque"   # review I2
    assert (by["nesher"]["kind"], by["nesher"]["landmark"]) == ("mixed_locality", "mosque_and_water_tower")   # owner ruling 2026-10-12
    assert "huj" not in by and "dorot" not in by   # the village alone is under 1,000 (review M2)


def test_encampments_are_sourced_totals_of_real_subdistricts():
    """The camps' areas are the sub-districts as content/map/subdistrict_hexes_1948.yaml draws them, read at load (review
    I4: no copy of the hexes here). Jerusalem counts only its "Tribal Units" row (7,070, review M3)."""
    keys = {r["key"] for r in SUBDISTRICTS["regions"]}
    assert {c["subdistrict"] for c in CAMPS} == {"beersheba", "jerusalem", "hebron", "ramle", "jaffa", "safad", "gaza", "nablus"}
    assert {c["subdistrict"] for c in CAMPS} <= keys
    by = {c["subdistrict"]: c["population"] for c in CAMPS}
    assert by["jerusalem"] == 7070 and sum(by.values()) == 64670
    for c in CAMPS:
        sourced(c)
        assert c["population"] > 0 and c["listed_as"] and "hexes" not in c
        assert "name" not in c, "camps are never named"


def test_the_runtime_file_is_a_fresh_bake():
    """The player reads content/map/localities_1948.tsv, baked from the YAML (boot budget). A stale bake fails here: rerun
    uv run tools/localities_bake.py after editing the YAML."""
    import localities_bake
    assert (MAP / "localities_1948.tsv").read_text(encoding="utf-8") == localities_bake.bake((MAP / "localities_1948.yaml").read_text(encoding="utf-8"))


def test_the_bake_refuses_a_field_with_a_tab_and_keeps_every_row():
    import localities_bake
    text = (MAP / "localities_1948.yaml").read_text(encoding="utf-8")
    rows = [line for line in localities_bake.bake(text).splitlines() if not line.startswith("#")]
    assert len([r for r in rows if r.startswith("place\t")]) == len(PLACES)
    assert len([r for r in rows if r.startswith("camp\t")]) == len(CAMPS)
    assert all(len(r.split("\t")) == (15 if r.startswith("place\t") else 6) for r in rows)
    with pytest.raises(ValueError, match="place 'hebron'"):
        localities_bake.bake(text.replace("  name: Hebron\n", "  name: \"Heb\\tron\"\n", 1))
