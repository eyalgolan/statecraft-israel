"""tools/translate.py: the Hebrew editor writes only the lines it means to, and refuses what the loader or tests refuse."""
import pytest

import translate
import yaml12

EVENT = """\
# yaml-language-server: $schema=../schema/event.schema.json
# A header comment.
id: sample
title: The vote
text: '{prime_minister} asks the cabinet: vote now?'
anchor: soft
from: 1948-05-15
options:
  - id: yes_vote
    # A comment inside the option.
    text: Vote
    historical: true
    effects:
      - { target: morale, delta: 0.01 }
  - id: wait
    text: Wait for {foreign_minister}
tags: []
status: draft
"""


def changes(**kw):
    base = {"title_he": "ההצבעה", "text_he": "{prime_minister} שואל את הממשלה: להצביע עכשיו?",
            "options": {"yes_vote": "להצביע", "wait": "לחכות ל{foreign_minister}"}, "native_read": False}
    base.update(kw)
    return base


def test_adds_hebrew_beside_the_english_and_keeps_every_other_line():
    out, warnings = translate.set_event(EVENT, changes())
    data = yaml12.load(out)
    assert data["title_he"] == "ההצבעה"
    assert data["text_he"] == "{prime_minister} שואל את הממשלה: להצביע עכשיו?"
    assert data["text_he_review"] == "agent-checked"
    assert [o.get("text_he") for o in data["options"]] == ["להצביע", "לחכות ל{foreign_minister}"]
    lines = out.split("\n")
    assert lines[lines.index("text: '{prime_minister} asks the cabinet: vote now?'") + 1].startswith("title_he: ")
    assert lines[lines.index("    text: Vote") + 1] == "    text_he: להצביע"
    removed = [line for line in EVENT.split("\n") if line not in lines]
    assert removed == []
    assert warnings == []


def test_replaces_in_place_and_native_read_drops_the_flag():
    first, _ = translate.set_event(EVENT, changes())
    out, _ = translate.set_event(first, changes(title_he="ההצבעה בממשלה", native_read=True))
    data = yaml12.load(out)
    assert data["title_he"] == "ההצבעה בממשלה"
    assert "text_he_review" not in data
    assert out.count("title_he:") == 1


def test_quotes_round_trip():
    tricky = 'צה"ל: \'הכול\' # לא הערה'
    out, _ = translate.set_event(EVENT, changes(title_he=tricky))
    assert yaml12.load(out)["title_he"] == tricky


def test_clearing_every_field_removes_the_hebrew_and_its_flag():
    first, _ = translate.set_event(EVENT, changes())
    out, _ = translate.set_event(first, changes(title_he="", text_he="", options={"yes_vote": "", "wait": ""}))
    assert out == EVENT


@pytest.mark.parametrize("bad, message", [
    (changes(text_he="הממשלה שואלת"), "placeholders"),
    (changes(options={"yes_vote": "להצביע", "wait": ""}), "all or none"),
    (changes(title_he="שורה\nשנייה"), "one line"),
    (changes(title_he="\u202bההצבעה"), "direction"),
])
def test_refuses_what_the_loader_or_tests_refuse(bad, message):
    with pytest.raises(translate.Refused, match=message):
        translate.set_event(EVENT, bad)


def test_warns_on_latin_letters():
    _, warnings = translate.set_event(EVENT, changes(title_he="The vote"))
    assert any("Latin" in w for w in warnings)


UI_EN = 'a.one: "One {0}"\na.two: "Two"\nb.three: "Three"\n'
UI_HE = '# Hebrew.\na.one: "אחד {0}"\nb.three: "שלוש"\n'


def test_ui_replaces_and_inserts_in_english_order():
    out = translate.set_ui(UI_HE, ["a.one", "a.two", "b.three"], "a.two", 'שניים "בדיוק"')
    assert out == '# Hebrew.\na.one: "אחד {0}"\na.two: "שניים \\"בדיוק\\""\nb.three: "שלוש"\n'
    out = translate.set_ui(out, ["a.one", "a.two", "b.three"], "a.one", "אחת {0}")
    assert yaml12.load(out)["a.one"] == "אחת {0}"


def test_ui_refuses_lost_tokens_and_empty_values():
    with pytest.raises(translate.Refused, match="placeholders"):
        translate.set_ui(UI_HE, ["a.one"], "a.one", "אחד", english="One {0}")
    with pytest.raises(translate.Refused, match="empty"):
        translate.set_ui(UI_HE, ["a.one"], "a.one", " ", english="One {0}")


@pytest.fixture
def langs(tmp_path, monkeypatch):
    monkeypatch.setattr(translate, "TRANSLATIONS", tmp_path)
    return tmp_path


def rows():
    return {r["key"]: r for r in translate.catalog()}


def test_the_catalog_holds_every_kind_of_user_facing_string():
    r = rows()
    assert r["event:altalena:title"]["en"] == "The Altalena" and r["event:altalena:title"]["unit"] == "event:altalena"
    assert r["ui:hud.turn"]["en"] == "Turn {0}"
    assert r["content:people.yaml#people.ben_gurion.name"]["en"] == "David Ben-Gurion"
    assert r["content:offices.yaml#offices.prime_minister.title_f"]["en"] == "Prime Minister"
    assert r["content:map/units_1948.yaml#units.golani.sector"]["en"].startswith("Much of the Lower Galilee")
    # Every Hebrew value the content holds has its row: nothing user-facing is left out.
    texts, _ = translate.load("he")
    assert set(texts) <= set(r)


def test_a_new_language_is_written_to_its_own_files_and_reads_back(langs):
    edits = {"event:altalena:title": "L'Altalena", "event:altalena:text": "{defense_minister} décide.",
             "event:altalena:option:enforce": "Imposer", "event:altalena:option:compromise": "Céder",
             "ui:hud.turn": "Tour {0}", "content:people.yaml#people.ben_gurion.name": "David Ben Gourion"}
    assert translate.save("fr", edits, {}, dry=True)["errors"] == {}
    assert not (langs / "fr").exists()
    result = translate.save("fr", edits, {"content:people.yaml#people.ben_gurion.name": True})
    assert result["errors"] == {}
    events = yaml12.load((langs / "fr/events.yaml").read_text(encoding="utf-8"))
    assert events["altalena"] == {"title": "L'Altalena", "text": "{defense_minister} décide.",
                                  "options": {"enforce": "Imposer", "compromise": "Céder"}, "review": "agent-checked"}
    assert yaml12.load((langs / "fr/ui.yaml").read_text(encoding="utf-8")) == {"hud.turn": "Tour {0}"}
    assert yaml12.load((langs / "fr/content.yaml").read_text(encoding="utf-8")) == {
        "people.yaml#people.ben_gurion.name": {"text": "David Ben Gourion"}}
    texts, flagged = translate.load("fr")
    assert texts["event:altalena:option:compromise"] == "Céder" and "event:altalena" in flagged
    assert "fr" in translate.languages()
    # Clearing the whole event removes it.
    translate.save("fr", {k: "" for k in edits if k.startswith("event:")}, {})
    assert "altalena" not in (yaml12.load((langs / "fr/events.yaml").read_text(encoding="utf-8")) or {})


def test_a_new_language_keeps_the_hebrew_rules(langs):
    errors = translate.save("fr", {"event:altalena:title": "L'Altalena", "ui:hud.turn": "Tour"}, {})["errors"]
    assert "all or none" in errors["event:altalena"]
    assert "placeholders" in errors["ui:hud.turn"]
    assert not (langs / "fr").exists()


def test_hebrew_content_names_stay_with_their_sources(langs):
    errors = translate.save("he", {"content:people.yaml#people.ben_gurion.name": "דוד"}, {}, dry=True)["errors"]
    assert "by hand" in errors["content:people.yaml#people.ben_gurion.name"]


@pytest.mark.parametrize("code", ["en", "FR", "../x", ""])
def test_refuses_a_bad_language(code, langs):
    with pytest.raises(translate.Refused):
        translate.save(code, {}, {})


def test_hebrew_saves_into_the_event_and_the_ui_file(tmp_path, monkeypatch):
    events = tmp_path / "events"
    events.mkdir()
    (events / "altalena.yaml").write_bytes((translate.EVENTS / "altalena.yaml").read_bytes())
    (tmp_path / "strings.he.yaml").write_bytes(translate.UI_HE.read_bytes())
    monkeypatch.setattr(translate, "EVENTS", events)
    monkeypatch.setattr(translate, "UI_HE", tmp_path / "strings.he.yaml")
    before = (events / "altalena.yaml").read_text(encoding="utf-8")
    result = translate.save("he", {"event:altalena:title": "האלטלנה", "ui:hud.turn": "סבב {0}"},
                            {"event:altalena": True})
    assert result["errors"] == {}
    data = yaml12.load((events / "altalena.yaml").read_text(encoding="utf-8"))
    assert data["title_he"] == "האלטלנה" and "text_he_review" not in data
    assert yaml12.load((tmp_path / "strings.he.yaml").read_text(encoding="utf-8"))["hud.turn"] == "סבב {0}"
    after = (events / "altalena.yaml").read_text(encoding="utf-8").split("\n")
    assert [line for line in before.split("\n") if line not in after] == \
        [line for line in before.split("\n") if line.startswith(("title_he:", "text_he_review:"))]
