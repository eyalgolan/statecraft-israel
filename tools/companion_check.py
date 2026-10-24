# /// script
# requires-python = ">=3.12"
# dependencies = ["websockets==15.0.1", "pyyaml==6.0.3"]
# ///
"""#1119 B headed check: ordinary keyboard play, rendered DOM, and captures. No VoiceOver automation.

uv run tools/companion_check.py --build <gzip-player> --out <evidence-directory>
--baseline takes matching first-card captures without expecting a companion in that player.
Uses headed.py's exact server/browser and isolated profiles; no sim state or game command injection.
"""
import argparse
import asyncio
import json
import re
from pathlib import Path

import yaml12

from headed import Browser

REPO = Path(__file__).resolve().parents[1]
DOM = """JSON.stringify({
  language: document.getElementById('companion')?.lang,
  text: document.getElementById('companion-reading')?.textContent,
  title: document.querySelector('#companion-reading h2')?.textContent,
  choice: document.querySelector('#companion-reading li')?.firstChild?.textContent,
  focus: document.getElementById('companion-focus')?.textContent,
  status: document.getElementById('companion-status')?.textContent,
  active: document.activeElement?.id,
  targets: document.querySelectorAll('#companion button,#companion a,#companion [tabindex]').length,
  canvas: document.getElementById('unity-canvas').getBoundingClientRect().toJSON(),
  panel: document.getElementById('companion')?.getBoundingClientRect().toJSON(),
  regions: document.getElementById('player-regions')?.getBoundingClientRect().toJSON(),
  panelOverflows: (() => {const p=document.getElementById('companion');return p ? p.scrollHeight>p.clientHeight : false;})(),
  padding: document.getElementById('player-layout') ? getComputedStyle(document.getElementById('player-layout')).padding : '',
  overlap: (() => { const c=document.getElementById('unity-canvas').getBoundingClientRect();
    const p=document.getElementById('companion')?.getBoundingClientRect();
    return p ? c.left < p.right && p.left < c.right && c.top < p.bottom && p.top < c.bottom : false; })()
})"""


async def read(b, sid):
    result = await b.send("Runtime.evaluate", {"expression": DOM, "returnByValue": True}, session=sid)
    return json.loads(result["result"]["value"])


async def until(b, sid, predicate, what):
    for _ in range(200):
        state = await read(b, sid)
        if predicate(state):
            return state
        await asyncio.sleep(.1)
    raise AssertionError(f"never observed {what}: {state}")


async def key(b, sid, name):
    code = {"Tab": 9, "Enter": 13, "Escape": 27}[name]
    for kind in ("keyDown", "keyUp"):
        await b.send("Input.dispatchKeyEvent", {"type": kind, "key": name, "code": name,
                     "windowsVirtualKeyCode": code}, session=sid)
        # A zero-duration pulse can begin and end between two WebGL input polls during shader warm-up.
        if kind == "keyDown": await asyncio.sleep(.25)
    await asyncio.sleep(.15)


async def tab_to(b, sid, label):
    seen = []
    for _ in range(90):
        state = await read(b, sid)
        if state["focus"] == label:
            return
        seen.append(state["focus"])
        await key(b, sid, "Tab")
        for _ in range(20):
            if (await read(b, sid))["focus"] != state["focus"]: break
            await asyncio.sleep(.1)
    raise AssertionError(f"Tab did not reach {label}: {seen}")


async def run(args):
    args.out.mkdir(parents=True, exist_ok=True)
    receipts = []
    for language in ("en", "he"):
        text = yaml12.load((REPO / f"unity/Assets/UI/strings.{language}.yaml").read_text())
        for size in (((1920, 1080), (390, 844)) if args.baseline else ((1920, 1080), (390, 844), (375, 667))):
            name = f"{language}-{size[0]}x{size[1]}"
            async with Browser(args.build, width=size[0], height=size[1], viewport=size, keep_awake=True) as b:
                sid = await b.open(f"firstcard&lang={language}")
                try:
                    await b.send("Page.bringToFront", session=sid)
                    await b.send("Emulation.setFocusEmulationEnabled", {"enabled":True}, session=sid)
                    if size[0] <= 390:
                        await b.send("Emulation.setSafeAreaInsetsOverride", {"insets": {"top":47,"right":0,"bottom":34,"left":0}}, session=sid)
                    await b.wait_for(sid, "^FIRST CARD OK", stall=120)
                    if args.baseline:
                        await b.shot(sid, args.out / f"{name}-event.png")
                        continue
                    state = await until(b, sid, lambda p: p.get("choice"), "current event and choices")
                    assert state["language"] == language and not state["overlap"] and state["targets"] == 0, state
                    if size[0] <= 390:
                        assert state["canvas"]["top"] == 47 and state["padding"] == "47px 0px 34px", state
                    await b.shot(sid, args.out / f"{name}-event.png")
                    receipts.append({"step": name + " event", **state})
                    if size[0] <= 390:
                        assert state["canvas"]["height"] >= min(size[0] * 1.5, size[1] - 47 - 34 - 48) - 1, state
                        assert state["panelOverflows"], "the real phone event must exercise a scrolling panel"
                        # Scroll only page layout, keeping the real game state and ordinary keyboard path untouched.
                        before = state["active"]
                        await b.send("Runtime.evaluate", {"expression": "document.getElementById('player-regions').scrollTop=1e6"}, session=sid)
                        state = await read(b, sid)
                        assert state["active"] == before and state["panel"]["top"] >= 47 and state["panel"]["bottom"] <= size[1]-34+1, state
                        await b.shot(sid, args.out / f"{name}-scrolled.png")
                        p = state["panel"]
                        await b.click(sid, p["left"] + 20, p["top"] + 12)
                        clicked_state = await read(b, sid)
                        clicked = clicked_state["active"]
                        await key(b, sid, "Tab")
                        after_tab = await read(b, sid)
                        receipts.append({"step": name + " panel click then Tab", "clicked_active": clicked, **after_tab})
                        assert after_tab["focus"] != clicked_state["focus"] or after_tab["active"] == "unity-canvas", "the next key after clicking the panel did not reach the game or return canvas focus"
                        # The panel contributes no action; Tab/Enter must still operate the existing game after clicking it.
                        await tab_to(b, sid, state["choice"])
                        await key(b, sid, "Enter")
                        await until(b, sid, lambda p: not p.get("choice"), "phone event choice queued after panel click")
                        await tab_to(b, sid, text["hud.end_turn"])
                        await key(b, sid, "Enter")
                        state = await until(b, sid, lambda p: text["report.title"] in p.get("status", ""), "phone End Turn after panel click")
                        receipts.append({"step": name + " keyboard result after panel click", **state})
                        continue
                    # Focus the canvas through a harmless pointer press on the card's backdrop, then use only keys.
                    await b.click(sid, 5, 100)
                    await tab_to(b, sid, state["choice"])
                    before = (await read(b, sid))["active"]
                    await key(b, sid, "Enter")
                    await until(b, sid, lambda p: not p.get("choice"), "event choice queued")
                    await tab_to(b, sid, text["hud.nav.budget"])
                    await key(b, sid, "Enter")
                    await tab_to(b, sid, "+")
                    await key(b, sid, "Enter")
                    await tab_to(b, sid, text["budget.submit"])
                    await key(b, sid, "Enter")
                    state = await until(b, sid, lambda p: bool(p.get("status")), "real budget refusal")
                    assert state["status"].startswith(text["toast.refused"].split("{0}")[0]), state
                    assert state["active"] == before, "publication moved document focus"
                    receipts.append({"step": name + " refusal", **state})
                    await b.shot(sid, args.out / f"{name}-refusal.png")
                    await key(b, sid, "Escape")
                    await tab_to(b, sid, text["hud.end_turn"])
                    await key(b, sid, "Enter")
                    state = await until(b, sid, lambda p: text["report.title"] in p.get("status", ""), "End Turn result")
                    receipts.append({"step": name + " result", **state})
                    await b.shot(sid, args.out / f"{name}-result.png")
                    assert not state["overlap"] and state["targets"] == 0, state
                except Exception:
                    await b.shot(sid, args.out / f"{name}-failure.png")
                    raise
                finally:
                    lines = b.lines(sid)
                    (args.out / f"{name}-console.txt").write_text("\n".join(lines))
                    (args.out / "observations.json").write_text(json.dumps(receipts, ensure_ascii=False, indent=2))
                    failures = [line for line in lines if re.search(r"BOOT FAIL|FIRST CARD FAIL|SMOKE FAIL|Exception|RuntimeError|out of memory", line)]
                    assert not failures, failures
    print("COMPANION HEADED PASS" if not args.baseline else "BASELINE CAPTURES PASS")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--baseline", action="store_true")
    asyncio.run(run(parser.parse_args()))
