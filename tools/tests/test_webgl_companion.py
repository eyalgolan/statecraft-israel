"""Read-only companion transport and cached-page regressions. Not a browser/VoiceOver gate."""
import json
import re
import shutil
import subprocess
from pathlib import Path

import pytest
import test_webgl_stale_page as stale

ROOT = Path(__file__).resolve().parents[2]
PLUGIN = ROOT / 'unity/Assets/Scripts/UI/WebCompanion.jslib'
CAPTURE = ROOT / 'unity/Assets/Scripts/UI/WebCompanion.cs'

DRIVER = r'''
const assert = require('assert');
let writes = 0;
const elements = {};
class Element {
  constructor(tag) { this.tag = tag; this.children = []; this.value = ''; this.hidden = false; this.attributes = {}; }
  set id(value) { this._id = value; elements[value] = this; }
  get id() { return this._id; }
  set textContent(value) { writes++; this.value = value; this.children = []; }
  get textContent() { return this.value; }
  appendChild(child) { this.children.push(child); }
  setAttribute(key, value) { this.attributes[key] = value; if (key === 'hidden') this.hidden = true; }
}
const classes = new Set();
const document = {
  getElementById(id) { return elements[id] || null; },
  createElement(tag) { assert(['style','aside','h2','p','section','h3','div','ol','li'].includes(tag)); return new Element(tag); },
  head: new Element('head'), body: new Element('body')
};
document.body.classList = { toggle(name, on) { assert.equal(name, 'has-companion'); if(on) classes.add(name); else classes.delete(name); } };
const canvas = new Element('canvas'); canvas.id = 'unity-canvas';
const window = {};
const LibraryManager = {library: {}};
function mergeInto(target, values) { Object.assign(target, values); }
let outbound;
function UTF8ToString(pointer) { assert.equal(pointer, 42); return outbound; }
'''


def run(script):
    assert shutil.which('node'), 'node is required: zero executed transport cases is not a pass'
    subprocess.run(['node', '-e', script], check=True, capture_output=True, text=True)


def payload():
    return dict(lang='he', heading='מלווה', help='Read only', focusHeading='Focus', statusHeading='Status',
                choicesHeading='Choices', empty='No decision waiting', unavailable='Unavailable',
                title='<img src=x onerror="bad()"> & שלום', body=['Line one\nLine two', '<script>bad()</script>'],
                choices=[dict(text='First', available=True, note='Returns to the recorded course.'),
                         dict(text='Second', available=False, note='')], focus='מפה', status='Not done: reason')


def test_cached_stock_page_receives_the_bridge_dom_and_literal_payload():
    # Execute the real current stock page's supported cached-HTML URL refresh before loading the new player's plugin.
    page = re.sub(r'\{\{\{ JSON\.stringify\((\w+)\) \}\}\}', r'"\1"', stale.rendered())
    script = re.search(r'<script>(.*)</script>', page, re.S).group(1).split('fetch(location.href')[0]
    current = page.replace('aaaa', '1111').replace('bbbb', '2222')
    code = DRIVER + script + '\nuseCurrentPage(' + json.dumps(current) + ');\n'
    code += "assert.equal(loaderUrl, 'Build/1111.loader.js'); assert.equal(config.dataUrl, 'Build/2222.data');\n"
    code += "assert.equal(typeof window.statecraftCompanion, 'undefined');\n" + PLUGIN.read_text()
    code += '\nconst payload = ' + json.dumps(payload()) + ';\n'
    code += r'''
outbound = JSON.stringify(payload);
LibraryManager.library.StatecraftCompanion(42);
assert.equal(elements['companion'].attributes['aria-labelledby'], 'companion-heading');
assert.equal(elements['companion-focus'].attributes['aria-live'], 'polite');
assert.equal(elements['companion-status'].attributes.role, 'status');
assert.equal(elements['companion'].lang, 'he');
assert.equal(elements['companion'].hidden, false);
assert.equal(elements['companion-title'].textContent, payload.title);
assert.deepEqual(elements['companion-body'].children.map(x => x.textContent), payload.body);
assert.deepEqual(elements['companion-choices'].children.map(x => x.textContent), ['First\nReturns to the recorded course.', 'Second: Unavailable']);
assert.equal(elements['companion-focus'].textContent, 'מפה');
assert.equal(elements['companion-status'].textContent, 'Not done: reason');
assert.equal(document.head.children.length, 1);
assert(classes.has('has-companion'));
const paragraphs = elements['companion-body'].children.slice();
const choices = elements['companion-choices'].children.slice();
const count = writes;
LibraryManager.library.StatecraftCompanion(42);
assert.equal(writes, count, 'unchanged snapshots must not re-announce');
payload.focus = 'Second'; payload.status = '';
outbound = JSON.stringify(payload); LibraryManager.library.StatecraftCompanion(42);
assert.deepEqual(elements['companion-body'].children, paragraphs);
assert.deepEqual(elements['companion-choices'].children, choices);
assert.equal(elements['companion-status'].textContent, '');
outbound = 'null'; LibraryManager.library.StatecraftCompanion(42);
assert.equal(elements['companion'].hidden, true); assert(!classes.has('has-companion'));
assert.equal(elements['companion-title'].textContent, '');
assert.equal(elements['companion-choices'].children.length, 0);
payload.title = ''; payload.body = []; payload.choices = [];
outbound = JSON.stringify(payload); LibraryManager.library.StatecraftCompanion(42);
assert.equal(elements['companion'].hidden, false);
assert.equal(elements['companion-title'].textContent, payload.empty);
assert.equal(elements['companion-choice-group'].hidden, true);
assert.equal(document.head.children.length, 1, 'no duplicate bootstrap on restart');
'''
    run(code)


def test_custom_callback_is_forwarded_without_bootstrapping_or_input():
    run(DRIVER + PLUGIN.read_text() + r'''
let got = [];
window.statecraftCompanion = value => got.push(value);
outbound = '{"status":"custom"}'; LibraryManager.library.StatecraftCompanion(42);
outbound = 'null'; LibraryManager.library.StatecraftCompanion(42);
assert.deepEqual(got, ['{"status":"custom"}', 'null']);
assert.equal(document.head.children.length, 0);
assert.equal(document.body.children.length, 0);
''')


def test_nonstock_template_without_canvas_keeps_its_own_layout():
    run(DRIVER + PLUGIN.read_text() + r'''
delete elements['unity-canvas']; outbound = '{}';
LibraryManager.library.StatecraftCompanion(42);
assert.equal(typeof window.statecraftCompanion, 'undefined');
assert.equal(document.head.children.length, 0);
''')


def test_csharp_import_js_export_and_production_hook_agree():
    csharp = CAPTURE.read_text()
    plugin = PLUGIN.read_text()
    root = (ROOT / 'unity/Assets/Scripts/UI/GameRoot.cs').read_text()
    assert re.search(r'DllImport\("__Internal"\)\]\s*static extern void StatecraftCompanion\(string json\)', csharp)
    assert 'StatecraftCompanion(json);' in csharp
    assert re.search(r'StatecraftCompanion:\s*function\s*\(json\)', plugin)
    assert 'companion = Own(new WebCompanion(root, text, () => card?.Shown != null, CompanionSend));' in root
    assert 'Application.platform == RuntimePlatform.WebGLPlayer || CompanionSend != null' in root
    assert 'companion = null;' in root
    assert all(word not in plugin for word in ['innerHTML', '.focus(', 'addEventListener', 'SendMessage'])
