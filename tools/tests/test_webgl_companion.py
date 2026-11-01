"""Read-only companion transport and cached-page regressions. Not a browser/VoiceOver gate."""
import json
import re
import shutil
import subprocess
from html.parser import HTMLParser
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
  constructor(tag) { this.tag = tag; this.children = []; this.value = ''; this.hidden = false; this.attributes = {}; this.sets = 0; }
  set id(value) { this._id = value; elements[value] = this; }
  get id() { return this._id; }
  set textContent(value) { writes++; this.sets++; this.value = value; this.children = []; }
  get textContent() { return this.value; }
  appendChild(child) { this.children.push(child); child.parentNode = this; }
  setAttribute(key, value) { this.attributes[key] = value; if (key === 'hidden') this.hidden = true; }
}
const classes = new Set();
let createLimit = Infinity;
const document = {
  getElementById(id) { return elements[id] || null; },
  createElement(tag) {
    if (createLimit-- <= 0) throw new Error('createElement unavailable');
    assert(['style','aside','h2','p','section','h3','div','ol','li'].includes(tag)); return new Element(tag);
  },
  head: new Element('head'), body: new Element('body')
};
document.body.classList = { toggle(name, on) { assert.equal(name, 'has-companion'); if(on) classes.add(name); else classes.delete(name); } };
document.activeElement = document.body;
const canvas = new Element('canvas'); canvas.id = 'unity-canvas';
const window = {};
const LibraryManager = {library: {}};
function mergeInto(target, values) { Object.assign(target, values); }
let outbound;
function UTF8ToString(pointer) { assert.equal(pointer, 42); return outbound; }
const live = () => Object.values(elements).filter(e => e.attributes['aria-live'] || e.attributes.role === 'status');
const call = () => assert.doesNotThrow(() => LibraryManager.library.StatecraftCompanion(42), 'the bridge must never throw into Unity');
'''


def run(script):
    assert shutil.which('node'), 'node is required: zero executed transport cases is not a pass'
    result = subprocess.run(['node', '-e', script], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    return result.stdout


def payload():
    return dict(lang='he', turn=1, heading='מלווה', help='Read only', focusHeading='Focus', statusHeading='Status',
                choicesHeading='Choices', empty='No decision waiting', unavailable='Unavailable',
                title='<img src=x onerror="bad()"> & שלום', body=['Line one\nLine two', '<script>bad()</script>'],
                choices=[dict(text='First', available=True, note='Returns to the recorded course.'),
                         dict(text='Second', available=False, note='')], focus='מפה', status='Not done: reason',
                reportTitle='', report=[])


class _Skeleton(HTMLParser):
    """The companion elements of the stock page, in document order: (tag, id, parent id, attributes)."""

    def __init__(self):
        super().__init__()
        self.found, self.stack = [], []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        inside = bool(self.stack) or attrs.get('id') == 'companion'
        if inside:
            self.found.append((tag, attrs.get('id'), self.stack[-1] if self.stack else None, attrs))
            self.stack.append(attrs.get('id'))

    def handle_endtag(self, tag):
        if self.stack:
            self.stack.pop()


def skeleton():
    parser = _Skeleton()
    parser.feed(stale.page())
    return parser.found


def bootstrapped(extra=''):
    """The plugin bootstrapped on a cached stock page (no skeleton) with the standard payload."""
    return DRIVER + PLUGIN.read_text() + '\nconst payload = ' + json.dumps(payload()) + ';\n' + r'''
outbound = JSON.stringify(payload); call();
assert.equal(elements['companion-title'].textContent, payload.title);
''' + extra


def test_cached_stock_page_receives_the_bridge_dom_and_literal_payload():
    # Execute the real current stock page's supported cached-HTML URL refresh before loading the new player's plugin.
    page = re.sub(r'\{\{\{ JSON\.stringify\((\w+)\) \}\}\}', r'"\1"', stale.rendered())
    # The page's inline script: from its first <script> to its last </script> (our own template, not a filter).
    script = page[page.index('<script>') + len('<script>'):page.rindex('</script>')].split('fetch(location.href')[0]
    current = page.replace('aaaa', '1111').replace('bbbb', '2222')
    code = DRIVER + script + '\nuseCurrentPage(' + json.dumps(current) + ');\n'
    code += "assert.equal(loaderUrl, 'Build/1111.loader.js'); assert.equal(config.dataUrl, 'Build/2222.data');\n"
    code += "assert.equal(typeof window.statecraftCompanion, 'undefined');\n" + PLUGIN.read_text()
    code += '\nconst payload = ' + json.dumps(payload()) + ';\n'
    code += r'''
outbound = JSON.stringify(payload);
const focused = document.activeElement;
call();
assert.equal(document.activeElement, focused, 'a push never moves focus');
assert.equal(elements['companion'].attributes['aria-labelledby'], 'companion-heading');
// Condition 4: only the status/result line is live; the card and the focus line are read on demand.
assert.deepEqual(live().map(e => e.id), ['companion-status']);
assert.equal(elements['companion-status'].attributes.role, 'status');
assert.equal(elements['companion-event'].attributes['aria-live'], undefined);
assert.equal(elements['companion-focus'].attributes['aria-live'], undefined);
// The owner's narrow exception: tabindex=-1 on the scroll container only, so it never becomes an implicit Tab stop.
assert.deepEqual(Object.values(elements).filter(e => 'tabindex' in e.attributes).map(e => [e.id, e.attributes.tabindex]), [['companion', '-1']]);
assert.equal(elements['companion'].lang, 'he');
assert(classes.has('has-companion'));
assert.equal(elements['companion-title'].textContent, payload.title);
assert.deepEqual(elements['companion-body'].children.map(x => x.textContent), payload.body);
assert.deepEqual(elements['companion-choices'].children.map(x => x.textContent), ['First\nReturns to the recorded course.', 'Second: Unavailable']);
assert.equal(elements['companion-focus'].textContent, 'מפה');
assert.equal(elements['companion-status'].textContent, 'Not done: reason');
assert.equal(elements['companion-report'].hidden, true);
assert.equal(document.head.children.length, 1);
const paragraphs = elements['companion-body'].children.slice();
const choices = elements['companion-choices'].children.slice();
const count = writes;
call();
assert.equal(writes, count, 'unchanged snapshots must not re-announce');
payload.focus = 'Second'; payload.status = '';
outbound = JSON.stringify(payload); call();
assert.deepEqual(elements['companion-body'].children, paragraphs);
assert.deepEqual(elements['companion-choices'].children, choices);
assert.equal(elements['companion-status'].textContent, '');
outbound = 'null'; call();
assert(!classes.has('has-companion'));
assert.equal(elements['companion-title'].textContent, '');
assert.equal(elements['companion-choices'].children.length, 0);
payload.title = ''; payload.body = []; payload.choices = [];
outbound = JSON.stringify(payload); call();
assert(classes.has('has-companion'));
assert.equal(elements['companion-title'].textContent, payload.empty);
assert.equal(elements['companion-choice-group'].hidden, true);
assert.equal(document.head.children.length, 1, 'no duplicate bootstrap on restart');
'''
    run(code)


def test_the_status_region_is_in_the_stock_page_from_load_and_the_bridge_reuses_it():
    found = skeleton()
    assert found, 'the stock page must carry the companion skeleton'
    status = [f for f in found if f[3].get('role') == 'status' or 'aria-live' in f[3]]
    assert [f[1] for f in status] == ['companion-status'], 'exactly one live region, the status line'
    assert not any('hidden' in f[3] for f in found), 'a hidden ancestor would keep the live region out of the accessibility tree'
    assert 'body:not(.has-companion) #companion' in stale.page(), 'the empty panel is out of sight until the game fills it'
    made = json.loads(run(bootstrapped(r'''
console.log(JSON.stringify(Object.values(elements).filter(e => e.id.startsWith('companion') && e.tag !== 'style')
  .map(e => [e.tag, e.id, e.parentNode.id || null, e.attributes])));
''')))
    assert made == [[tag, id_, parent, {k: v or '' for k, v in attrs.items() if k != 'id'}] for tag, id_, parent, attrs in found], \
        'the stock page skeleton and the cached-page bootstrap must build the same elements'
    build = ''.join(
        f"(() => {{ const e = new Element({json.dumps(tag)}); e.id = {json.dumps(id_)}; "
        f"Object.entries({json.dumps({k: v or '' for k, v in attrs.items() if k != 'id'})}).forEach(([k, v]) => e.setAttribute(k, v)); "
        f"(elements[{json.dumps(parent)}] || document.body).appendChild(e); }})();\n"
        for tag, id_, parent, attrs in found)
    run(DRIVER + build + r'''
const status = elements['companion-status'];
const shape = () => Object.values(elements).filter(e => e.tag !== 'style').map(e => [e.tag, e.id, JSON.stringify(e.attributes)]);
const spec = shape();
const bodyChildren = document.body.children.length;
''' + PLUGIN.read_text() + '\nconst payload = ' + json.dumps(payload()) + ';\n' + r'''
outbound = JSON.stringify(payload); call();
assert.strictEqual(elements['companion-status'], status, 'the page-load region is reused, not replaced');
assert.equal(document.body.children.length, bodyChildren, 'no second panel');
assert.deepEqual(shape(), spec, 'the page-load elements are used as they are');
assert.equal(status.textContent, 'Not done: reason');
''')


def test_malformed_snapshot_keeps_the_last_good_panel_and_a_later_good_one_recovers():
    run(bootstrapped(r'''
const before = writes;
for (const bad of ['{not json', '', '[]', '42', '"text"',
                   JSON.stringify(Object.assign({}, payload, {body: 'not a list'})),
                   JSON.stringify(Object.assign({}, payload, {choices: [{text: 1}]})),
                   JSON.stringify(Object.assign({}, payload, {lang: 'fr'})),
                   JSON.stringify(Object.assign({}, payload, {title: null})),
                   JSON.stringify({lang: 'en'})]) {
  outbound = bad; call();
}
assert.equal(writes, before, 'a bad payload writes nothing');
assert.equal(elements['companion-title'].textContent, payload.title);
assert(classes.has('has-companion'));
payload.title = 'Recovered'; outbound = JSON.stringify(payload); call();
assert.equal(elements['companion-title'].textContent, 'Recovered');
'''))


def test_missing_child_node_writes_nothing_and_recovers_when_it_returns():
    run(bootstrapped(r'''
const focusNode = elements['companion-focus'];
delete elements['companion-focus'];
const before = writes;
payload.title = 'Next'; payload.focus = 'Elsewhere'; outbound = JSON.stringify(payload); call();
assert.equal(writes, before, 'nodes are resolved before any write');
assert.equal(elements['companion-title'].textContent, '<img src=x onerror="bad()"> & שלום');
outbound = 'null'; call();
assert.equal(writes, before, 'clearing also needs every node');
elements['companion-focus'] = focusNode;
outbound = JSON.stringify(payload); call();
assert.equal(elements['companion-title'].textContent, 'Next');
assert.equal(focusNode.textContent, 'Elsewhere');
'''))


def test_missing_page_apis_never_throw_and_bootstrap_is_retried():
    run(DRIVER + PLUGIN.read_text() + '\nconst payload = ' + json.dumps(payload()) + ';\n' + r'''
outbound = JSON.stringify(payload);
const head = document.head; document.head = undefined; call();
assert.equal(typeof window.statecraftCompanion, 'undefined', 'no callback without a usable page');
document.head = head;
createLimit = 3; call();
assert.equal(typeof window.statecraftCompanion, 'undefined', 'a failed bootstrap stays retryable');
createLimit = Infinity; call();
assert.equal(typeof window.statecraftCompanion, 'function');
assert.equal(elements['companion-title'].textContent, payload.title);
assert.equal(document.head.children.length, 1, 'the retry reuses what the failed attempt made');
assert.equal(document.body.children.filter(e => e.id === 'companion').length, 1);
''')


def test_utf8_conversion_failure_never_throws():
    run(DRIVER + PLUGIN.read_text() + r'''
UTF8ToString = () => { throw new Error('bad pointer'); };
call();
''')


def test_custom_callback_is_forwarded_without_bootstrapping_or_input():
    run(DRIVER + PLUGIN.read_text() + r'''
let got = [];
window.statecraftCompanion = value => got.push(value);
outbound = '{"status":"custom"}'; call();
outbound = 'null'; call();
assert.deepEqual(got, ['{"status":"custom"}', 'null']);
assert.equal(document.head.children.length, 0);
assert.equal(document.body.children.length, 0);
''')


def test_a_throwing_custom_callback_never_reaches_unity():
    run(DRIVER + PLUGIN.read_text() + r'''
window.statecraftCompanion = () => { throw new Error('page script bug'); };
outbound = '{}'; call();
''')


def test_status_is_rewritten_for_a_new_turn_even_with_the_same_text():
    run(bootstrapped(r'''
const status = elements['companion-status'];
let sets = status.sets;
outbound = JSON.stringify(payload); call();
assert.equal(status.sets, sets, 'same text, same turn: no write');
payload.turn = 2; outbound = JSON.stringify(payload); call();
assert(status.sets > sets, 'same text on a new turn is announced again');
assert.equal(status.textContent, 'Not done: reason');
sets = status.sets;
payload.focus = 'Moved'; outbound = JSON.stringify(payload); call();
assert.equal(status.sets, sets, 'a focus move alone does not touch the status line');
'''))


def test_turn_report_is_a_readable_section_that_is_not_live():
    run(bootstrapped(r'''
payload.turn = 2; payload.status = 'What happened this turn';
payload.reportTitle = 'What happened this turn';
payload.report = ['What changed in the top bar:', 'Money: -5\nWages paid'];
outbound = JSON.stringify(payload); call();
assert.equal(elements['companion-report'].hidden, false);
assert.equal(elements['companion-report-title'].textContent, 'What happened this turn');
assert.deepEqual(elements['companion-report-rows'].children.map(x => x.textContent), payload.report);
assert.deepEqual(live().map(e => e.id), ['companion-status']);
payload.reportTitle = ''; payload.report = []; outbound = JSON.stringify(payload); call();
assert.equal(elements['companion-report'].hidden, true);
assert.equal(elements['companion-report-rows'].children.length, 0);
'''))


def test_nonstock_template_without_canvas_keeps_its_own_layout():
    run(DRIVER + PLUGIN.read_text() + r'''
delete elements['unity-canvas']; outbound = '{}';
call();
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
    assert 'companion = Own(new WebCompanion(root, text, bridge, () => card?.Shown != null, CompanionSend));' in root
    assert 'Application.platform == RuntimePlatform.WebGLPlayer || CompanionSend != null' in root
    assert 'companion = null;' in root
    assert all(word not in plugin for word in ['innerHTML', '.focus(', 'addEventListener', 'SendMessage'])
