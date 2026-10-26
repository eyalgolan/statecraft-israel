"""Read-only companion regressions; no real screen reader or browser evidence."""
import json
import re
import shutil
import subprocess
from pathlib import Path

import pytest

TEMPLATE = Path(__file__).resolve().parents[2] / "unity/Assets/WebGLTemplates/Statecraft/index.html"


def test_companion_is_a_named_read_only_region_with_separate_live_outputs():
    page = TEMPLATE.read_text(encoding="utf-8")
    assert 'id="companion"' in page
    assert 'aria-labelledby="companion-heading"' in page
    assert 'id="companion-focus" dir="auto" aria-live="polite"' in page
    assert 'id="companion-status" dir="auto" role="status"' in page
    assert 'has-companion #unity-canvas' in page


@pytest.mark.skipif(shutil.which("node") is None, reason="needs node to execute the companion renderer")
def test_payload_is_text_only_deduplicated_ordered_and_resettable():
    page = TEMPLATE.read_text(encoding="utf-8")
    script = re.search(r"// Companion begin(.*?)// Companion end", page, re.S)
    assert script, "template must provide the one-way companion renderer"
    # Deliberately no input, focus, innerHTML or event APIs: any use of them fails.
    driver = r'''
const assert = require('assert');
let writes = 0;
class Element {
  constructor() { this.children = []; this.value = ''; this.hidden = false; }
  set textContent(value) { writes++; this.value = value; this.children = []; }
  get textContent() { return this.value; }
  appendChild(child) { this.children.push(child); }
}
const elements = {};
const document = {
  getElementById(id) { return elements[id] || (elements[id] = new Element()); },
  createElement(tag) { assert(['li', 'p'].includes(tag)); return new Element(); },
  body: { classList: { toggle(name, on) { assert.equal(name, 'has-companion'); } } }
};
'''
    payload = {
        "lang": "he", "heading": "מלווה", "help": "Read only", "focusHeading": "Focus",
        "statusHeading": "Status", "choicesHeading": "Choices", "empty": "No decision waiting",
        "unavailable": "Unavailable", "title": '<img src=x onerror="bad()"> & שלום',
        "body": ["Line one\nLine two", "<script>bad()</script>"],
        "choices": [{"text": "First", "available": True}, {"text": "Second", "available": False}],
        "focus": "First", "status": "Not done: reason",
    }
    driver += script.group(1) + "\nconst payload = " + json.dumps(payload) + ";\n"
    driver += r'''
statecraftCompanion(JSON.stringify(payload));
assert.equal(elements['companion'].lang, 'he');
assert.equal(elements['companion-title'].textContent, payload.title);
assert.deepEqual(elements['companion-body'].children.map(x => x.textContent), payload.body);
assert.deepEqual(elements['companion-choices'].children.map(x => x.textContent), ['First', 'Second — Unavailable']);
assert.equal(elements['companion-focus'].textContent, 'First');
assert.equal(elements['companion-status'].textContent, 'Not done: reason');
let count = writes;
statecraftCompanion(JSON.stringify(payload));
assert.equal(writes, count);
payload.focus = 'Second'; payload.status = '';
const title = elements['companion-title'];
const paragraphs = elements['companion-body'].children.slice();
const choices = elements['companion-choices'].children.slice();
statecraftCompanion(JSON.stringify(payload));
assert.strictEqual(elements['companion-title'], title);
assert.deepEqual(elements['companion-body'].children, paragraphs);
assert.deepEqual(elements['companion-choices'].children, choices);
assert.equal(elements['companion-status'].textContent, '');
statecraftCompanion('null');
assert.equal(elements['companion'].hidden, true);
assert.equal(elements['companion-title'].textContent, '');
assert.equal(elements['companion-choices'].children.length, 0);
payload.title = ''; payload.body = []; payload.choices = [];
statecraftCompanion(JSON.stringify(payload));
assert.equal(elements['companion'].hidden, false);
assert.equal(elements['companion-title'].textContent, payload.empty);
assert.equal(elements['companion-body'].children.length, 0);
assert.equal(elements['companion-choices'].children.length, 0);
'''
    subprocess.run(["node", "-e", driver], check=True, capture_output=True, text=True)
