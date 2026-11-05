# /// script
# requires-python = ">=3.11"
# dependencies = ["websockets==15.0.1"]
# ///
"""#2634 capture rig: serve a WebGL build, open it on a fresh profile in Chrome for Testing (web_check's flags),
start a new game, force the IndexedDB autosave write to fail, End turn, wait for the save.write_failed toast, screenshot.
Usage: uv run rig.py <repo> <build> <lang en|he> <mode quota|inject> <outdir>"""
import asyncio, base64, json, re, shutil, subprocess, sys, tempfile, time
from pathlib import Path

repo, build, lang, mode, out = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3], sys.argv[4], Path(sys.argv[5])
sys.path.insert(0, str(repo / "tools"))
import web_check as wc  # serving, flags, constants only

out.mkdir(parents=True, exist_ok=True)
INJECT = r"""
(() => {
  window.__statecraftFailPuts = false;
  const put = IDBObjectStore.prototype.put;
  IDBObjectStore.prototype.put = function (...args) {
    if (window.__statecraftFailPuts) {
      console.log("RIG forced IndexedDB put failure on " + this.name + " key " + String(args[1]));
      throw new DOMException("forced by the #2634 capture rig", "QuotaExceededError");
    }
    return put.apply(this, args);
  };
})();
"""

async def main():
    import websockets
    profile = tempfile.mkdtemp(prefix="e21-2634-")
    server = wc.serve(build)
    port = server.server_address[1]
    origin = f"http://localhost:{port}"
    q = "navlog" + ("&lang=he" if lang == "he" else "")
    url = f"{origin}/index.html?{q}"
    proc = subprocess.Popen([str(wc.find_chrome()), *wc.chrome_flags(profile, False), "about:blank"],
                            stderr=subprocess.DEVNULL, stdout=subprocess.DEVNULL)
    log = open(out / f"console-{lang}.txt", "w")
    t0 = time.monotonic()
    lines: list[str] = []
    buttons = wc.StartButtons(wc.VIEWPORT)
    try:
        pf = Path(profile) / "DevToolsActivePort"
        for _ in range(600):
            if pf.exists() and len(pf.read_text().split()) >= 2: break
            await asyncio.sleep(0.1)
        dport, path = pf.read_text().split()[:2]
        async with websockets.connect(f"ws://127.0.0.1:{dport}{path}", max_size=None) as ws:
            ids, pending = iter(range(1, 1 << 30)), {}

            def record(text, raw=None):
                text = text.rstrip()
                stamp = f"{time.monotonic() - t0:8.2f}s"
                lines.append(text); log.write(f"{stamp} {text}\n"); log.flush()
                if raw is not None: buttons.feed(raw.strip())

            async def reader():
                async for raw in ws:
                    r = json.loads(raw)
                    if "id" in r and r["id"] in pending:
                        pending.pop(r["id"]).set_result(r)
                    elif r.get("method") == "Runtime.consoleAPICalled":
                        p = r["params"]
                        text = " ".join(str(a.get("value", a.get("description", ""))) for a in p["args"])
                        record(f"[{p['type']}] {text}", text)
                    elif r.get("method") == "Runtime.exceptionThrown":
                        record("[exception] " + json.dumps(r["params"]["exceptionDetails"].get("exception", {}).get("description", ""))[:400])
            rt = asyncio.create_task(reader())

            async def call(method, params=None, session=None):
                n = next(ids); fut = asyncio.get_running_loop().create_future(); pending[n] = fut
                await ws.send(json.dumps({"id": n, "method": method, "params": params or {}, **({"sessionId": session} if session else {})}))
                r = await asyncio.wait_for(fut, 30)
                if "error" in r: raise RuntimeError(f"{method}: {r['error']}")
                return r.get("result", {})

            async def wait_for(pred, timeout, what):
                end = time.monotonic() + timeout; i = 0
                while time.monotonic() < end:
                    for line in lines[i:]:
                        if pred(line): return line
                    i = len(lines); await asyncio.sleep(0.2)
                raise TimeoutError(what)

            async def click(x, y):
                print(f"click {x},{y}")
                await call("Input.dispatchMouseEvent", {"type": "mouseMoved", "x": x, "y": y}, sid)
                for kind in ("mousePressed", "mouseReleased"):
                    await call("Input.dispatchMouseEvent", {"type": kind, "x": x, "y": y, "button": "left", "clickCount": 1}, sid)

            async def shot(name):
                data = (await call("Page.captureScreenshot", {"format": "png"}, sid))["data"]
                (out / name).write_bytes(base64.b64decode(data)); record(f"RIG screenshot {name}")

            target = (await call("Target.createTarget", {"url": "about:blank"}))["targetId"]
            sid = (await call("Target.attachToTarget", {"targetId": target, "flatten": True}))["sessionId"]
            await call("Runtime.enable", None, sid)
            await call("Page.enable", None, sid)
            await call("Emulation.setDeviceMetricsOverride", wc.viewport_override(1920, 1080, 1.0), sid)
            if mode == "inject":
                await call("Page.addScriptToEvaluateOnNewDocument", {"source": INJECT}, sid)
            record(f"RIG open {url} mode={mode}")
            await call("Page.navigate", {"url": url}, sid)
            game = await wait_for(lambda l: re.search(r"GAME OK \S+ turn 0 ", l), 240, "GAME OK turn 0")
            await asyncio.sleep(4)
            nx, ny = buttons.point(wc.NEW_GAME)
            # Force the failure now: after the boot's IndexedDB populate, before the first save.
            if mode == "quota":
                r = await call("Storage.overrideQuotaForOrigin", {"origin": origin, "quotaSize": 1}, sid)
                record(f"RIG Storage.overrideQuotaForOrigin {origin} quotaSize=1 -> {r}")
            else:
                await call("Runtime.evaluate", {"expression": "window.__statecraftFailPuts = true"}, sid)
                record("RIG IndexedDB put failure armed")
            await click(nx, ny)
            ex, ey = wc.END_TURN_HE if lang == "he" else wc.END_TURN
            await asyncio.sleep(3)
            await shot(f"debug-{lang}-opening.png")
            await click(*((885, 582) if lang == "he" else (1034, 615)))   # "Skip the opening" (measured on the EN capture; mirrored in HE)
            await asyncio.sleep(3)
            await click(ex, ey)          # End turn with units unordered: selects the first unit needing orders
            await asyncio.sleep(3)
            await shot(f"unit-{lang}.png")
            cf = out / f"click-{lang}.txt"
            record(f"RIG waiting for {cf} (x,y of the unit card's Hold button)")
            for _ in range(900):
                if cf.exists() and cf.read_text().strip(): break
                await asyncio.sleep(1)
            hx, hy = (int(v) for v in cf.read_text().split(","))
            await click(hx, hy)          # Hold: a committed order, so GameStart.Autosave writes the save
            try:
                await wait_for(lambda l: "AUTOSAVE FAIL" in l or "SAVE FAILURE SHOWN" in l, 30, "AUTOSAVE FAIL")
            except TimeoutError:
                await shot(f"nofail-{lang}-{mode}.png"); record("RIG no failure within 30 s"); return 2
            await wait_for(lambda l: "SAVE FAILURE SHOWN save.write_failed" in l, 20, "SAVE FAILURE SHOWN")
            for d, name in ((0.7, "a"), (1.5, "b"), (3.0, "c")):
                await asyncio.sleep(d); await shot(f"toast-{lang}-{name}.png")
            await asyncio.sleep(1)
            return 0
    finally:
        log.close()
        proc.terminate()
        try: proc.wait(5)
        except subprocess.TimeoutExpired: proc.kill(); proc.wait()
        server.shutdown(); server.server_close()
        shutil.rmtree(profile, ignore_errors=True)

sys.exit(asyncio.run(main()))
