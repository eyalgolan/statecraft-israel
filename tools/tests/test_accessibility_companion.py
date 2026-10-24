"""#1119 B: execute the shipped renderer in a real browser, separate from actual-game and VoiceOver evidence."""
import re
import os
import shutil
import subprocess
from pathlib import Path

import pytest

TEMPLATE = Path(__file__).resolve().parents[2] / "unity/Assets/WebGLTemplates/Statecraft/index.html"
CHROME = next((Path(p) for p in (os.environ.get("CHROME_PATH"), shutil.which("google-chrome"),
                               shutil.which("chromium"), shutil.which("chromium-browser"),
                               "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
               if p and Path(p).is_file()), None)

DRIVER = r"""
(async function () {
  function check(ok, message) { if (!ok) throw new Error(message); }
  try {
    var status = document.getElementById('companion-status');
    check(status.getAttribute('role') === 'status', 'status exists before mount');
    check(!window.statecraftCompanionPush, 'no renderer before loader resolution');
    mountCompanion();
    var push = window.statecraftCompanionPush;
    var p = {language:'en', turn:0, screen:'', focus:'Choice one', title:'<img src=x onerror=alert(1)>',
      body:'Whole event\nSecond line', context:['Office', 'תפקיד ‎IL 1.2m‎'],
      choices:[{text:'First',enabled:true,notes:['Preview +1']},{text:'Second',enabled:false,notes:[]}],status:'Refused'};
    var canvas = document.getElementById('unity-canvas'); canvas.focus();
    var active = document.activeElement;
    check(push(JSON.stringify(p)), 'valid snapshot');
    var panel = document.getElementById('companion'), reading = document.getElementById('companion-reading');
    check(reading.querySelector('h2').textContent === p.title, 'literal hostile title');
    check(panel.querySelectorAll('img,script,button,a,[tabindex],[contenteditable]').length === 0, 'no execution or focus targets');
    check(reading.textContent.includes(p.body) && reading.textContent.includes(p.context[1]), 'full text and LRM retained');
    var items = reading.querySelectorAll('li');
    check(items.length === 2 && items[0].textContent === 'FirstPreview +1' && items[1].textContent === 'Second', 'choice and preview order');
    check(items[1].getAttribute('aria-disabled') === 'true', 'disabled state');
    check(panel.querySelectorAll('h2,p,li').length > 0 && [...panel.querySelectorAll('h2,p,li')].every(n => n.dir === 'auto'), 'first-strong direction');
    check(document.querySelectorAll('[role=status],[aria-live]').length === 1, 'only status is live');
    var mutations = 0, observer = new MutationObserver(() => mutations++);
    observer.observe(status, {subtree:true, childList:true, characterData:true});
    var card = reading.firstElementChild;
    p.focus = 'Second'; push(JSON.stringify(p)); await Promise.resolve();
    check(mutations === 0 && reading.firstElementChild === card, 'focus changes preserve card and live region');
    p.title = 'Next event'; push(JSON.stringify(p)); await Promise.resolve();
    check(mutations === 0, 'event rebuild is not announced');
    p.turn++; push(JSON.stringify(p)); await Promise.resolve();
    check(mutations === 1, 'same status in a new turn updates once');
    p.status = 'Result'; push(JSON.stringify(p)); await Promise.resolve();
    check(mutations === 2, 'new status updates');
    var last = panel.textContent;
    check(push('{bad') === false && panel.textContent === last, 'bad JSON keeps last good panel');
    var bad = {...p, choices:[{text:'broken',enabled:'true',notes:[]}]};
    check(push(JSON.stringify(bad)) === false && panel.textContent === last, 'bad shape rejected atomically');
    bad = {...p,language:'<img>'};
    check(push(JSON.stringify(bad)) === false && panel.textContent === last, 'language allowlist');
    p.language = 'he'; p.body = 'טקסט עברי'; push(JSON.stringify(p));
    check(panel.lang === 'he' && panel.textContent.includes('טקסט עברי'), 'Hebrew and fallback text');
    check(document.activeElement === active, 'publishing never moves DOM focus');
    push(JSON.stringify({language:'en',turn:0,screen:'',focus:'',title:'',body:'',context:[],choices:[],status:''}));
    check(panel.textContent.trim() === '', 'Dispose snapshot clears panel');
    check(document.getElementById('companion-status') === status, 'live region never replaced');
    observer.disconnect();
    document.getElementById('test-result').textContent = 'COMPANION DOM PASS';
  } catch (e) { document.getElementById('test-result').textContent = 'FAIL: ' + e.message; }
})();
"""


@pytest.mark.skipif(CHROME is None, reason="needs installed Chrome for real DOM execution")
def test_the_product_renderer_is_safe_read_only_and_only_announces_status(tmp_path):
    template = TEMPLATE.read_text(encoding="utf-8")
    match = re.search(r"      function mountCompanion\(\) \{.*?\n      \}", template, re.S)
    assert match, "the product template has no read-only companion renderer"
    html = re.sub(r"<script>.*?</script>", "", template, flags=re.S)
    html = html.replace("</body>", '<pre id="test-result"></pre><script>' + match.group(0) + DRIVER + '</script></body>')
    page = tmp_path / "renderer.html"
    page.write_text(html, encoding="utf-8")
    result = subprocess.run([str(CHROME), "--headless", "--disable-gpu", "--no-first-run",
                             f"--user-data-dir={tmp_path / 'profile'}", "--dump-dom", page.as_uri()],
                            capture_output=True, text=True, timeout=60, check=True)
    assert '<pre id="test-result">COMPANION DOM PASS</pre>' in result.stdout, result.stdout[-3000:]


def test_the_companion_mounts_only_after_the_unity_loader_resolves():
    text = TEMPLATE.read_text(encoding="utf-8")
    assert re.search(r"\}\)\.then\(function \(\) \{\s*mountCompanion\(\);", text)
    assert not re.search(r"\b(innerHTML|outerHTML|insertAdjacentHTML|document\.write)\b", text)
