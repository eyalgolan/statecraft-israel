"""#1119: real CSS geometry at desktop and safe-area phone sizes, without loading Unity."""
import os
import re
import shutil
import subprocess
from pathlib import Path

import pytest

REPO = Path(__file__).resolve().parents[2]
TEMPLATE = REPO / "unity/Assets/WebGLTemplates/Statecraft/index.html"
CHROME = next((Path(p) for p in (os.environ.get("CHROME_PATH"), shutil.which("google-chrome"),
                               shutil.which("chromium"), shutil.which("chromium-browser"),
                               "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
               if p and Path(p).is_file()), None)

DRIVER = r"""
(async () => {
  const check = (ok, why) => { if (!ok) throw new Error(why); };
  const tick = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
  try {
    const frame=document.getElementById('player-layout'), regions=document.getElementById('player-regions');
    const phone=innerWidth<=700;
    if(phone) { frame.style.setProperty('--safe-top','47px'); frame.style.setProperty('--safe-bottom','34px'); }
    mountCompanion();
    window.statecraftCompanionPush(JSON.stringify({language:'en',turn:0,screen:'',focus:'',title:'One Army',
      body:'Event text '.repeat(100),context:[],choices:[],status:''}));
    await tick();
    const canvas=document.getElementById('unity-canvas'), panel=document.getElementById('companion');
    const c=canvas.getBoundingClientRect(),p=panel.getBoundingClientRect();
    check(!(c.left<p.right && p.left<c.right && c.top<p.bottom && p.top<c.bottom),'overlap');
    if(phone) {
      check(c.height>=Math.min(innerWidth*1.5,innerHeight-47-34-48)-1,'game lost its minimum height');
      const r=regions.getBoundingClientRect();
      check(c.top===47 && r.bottom===innerHeight-34,'fixed hardware clipping');
      check(Math.min(p.bottom,r.bottom)-Math.max(p.top,r.top)>=47,'no visible companion scroll area');
      const active=document.activeElement;
      regions.scrollTop=regions.scrollHeight;
      await tick();
      const after=panel.getBoundingClientRect();
      check(after.top>=r.top && after.bottom<=r.bottom+1,'panel not reachable within safe region');
      check(document.activeElement===active,'scroll moved focus');
    } else {
      check(c.width>=innerWidth*.7 && c.height===innerHeight,'desktop game region shrank');
      check(p.left===c.right && p.right===innerWidth,'desktop panel beside game');
    }
    console.log('LAYOUT PASS '+innerWidth+'x'+innerHeight);
  } catch(e) { console.log('LAYOUT FAIL '+e.message); }
})();
"""


@pytest.mark.skipif(CHROME is None or shutil.which("uv") is None, reason="needs Chrome and uv for the existing CDP checker")
@pytest.mark.parametrize("viewport", ["1920x1080", "390x844", "375x667", "667x375"])
def test_the_panel_keeps_game_height_and_can_be_read_inside_phone_safe_edges(tmp_path, viewport):
    template = TEMPLATE.read_text(encoding="utf-8")
    renderer = re.search(r"      function mountCompanion\(\) \{.*?\n      \}", template, re.S)
    assert renderer
    html = re.sub(r"<script>.*?</script>", "", template, flags=re.S)
    html = html.replace("</body>", "<script>" + renderer.group(0) + DRIVER + "</script></body>")
    (tmp_path / "index.html").write_text(html, encoding="utf-8")
    result = subprocess.run(["uv", "run", str(REPO / "tools/web_check.py"), str(tmp_path),
                             "--viewport", viewport, "--expect", "LAYOUT PASS " + viewport,
                             "--fail", "LAYOUT FAIL", "--timeout", "30"],
                            env={**os.environ, "CHROME_PATH": str(CHROME)},
                            text=True, capture_output=True, timeout=60)
    assert result.returncode == 0, result.stdout + result.stderr
