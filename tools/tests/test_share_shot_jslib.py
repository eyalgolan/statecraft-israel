"""ShareShot.jslib (#1897): the browser half of the Share and Copy buttons, run in node with a stubbed page.

The Unity tests only see the C# side (the jslib is swapped for hooks), so the two robustness rules live here:
a clipboard callback from an older action must not act after a reset, and a share() that throws at once still
downloads the picture."""
import json
import shutil
import subprocess
from pathlib import Path

import pytest

JSLIB = Path(__file__).resolve().parents[2] / "unity/Assets/Scripts/UI/ShareShot.jslib"
pytestmark = pytest.mark.skipif(shutil.which("node") is None, reason="needs node")

# Loads the jslib the way Emscripten does: mergeInto collects the library, and every "$name" entry becomes a global.
DRIVER = r"""
const fs = require("fs"), vm = require("vm");
const scenario = process.argv[3];
const handlers = {}, clicks = [];
const canvas = { addEventListener: (t, f) => { handlers[t] = f; }, removeEventListener: () => {} };
const file = { name: "shot.png" };
let resolveWrite, shareBehaviour = "ok";
const ctx = {
  console, setTimeout, clearTimeout, Promise, URL: { createObjectURL: () => "blob:x", revokeObjectURL() {} },
  HEAPU8: new Uint8Array(4), UTF8ToString: () => "post text",
  File: function (b, name) { return { name }; }, ClipboardItem: function () {},
  navigator: {
    canShare: () => true,
    share: () => {
      if (shareBehaviour === "throw") throw new Error("NotAllowedError");
      return Promise.resolve();
    },
    clipboard: { write: () => new Promise((r) => { resolveWrite = r; }) },
  },
  document: {
    querySelector: () => canvas, addEventListener() {}, removeEventListener() {}, body: { appendChild() {} },
    createElement: () => ({ click() { clicks.push(1); }, remove() {} }),
  },
};
ctx.window = ctx;
ctx.addEventListener = () => {}; ctx.removeEventListener = () => {};
ctx.mergeInto = (lib, entries) => { ctx.lib = entries; };
ctx.LibraryManager = { library: {} };
vm.createContext(ctx);
vm.runInContext(fs.readFileSync(process.argv[2], "utf8"), ctx);
for (const [k, v] of Object.entries(ctx.lib)) if (k[0] === "$") ctx[k.slice(1)] = v;
const L = ctx.lib;
const press = () => handlers.pointerup({ type: "pointerup" });
const tick = () => new Promise((r) => setTimeout(r, 0));
(async () => {
  ctx.statecraftShareFile = file;
  const out = {};
  if (scenario === "late-copy-after-reset" || scenario === "late-copy-after-new-action") {
    L.StatecraftShareCopyOnRelease();
    press();                         // the write is now pending, owned by the first copy action
    if (scenario === "late-copy-after-reset") L.StatecraftShareInvalidate();
    else L.StatecraftShareOnRelease(); // a new main action
    resolveWrite();
    await tick();
    out.copied = L.StatecraftShareCopied();
  } else if (scenario === "copy-still-lands") {
    L.StatecraftShareCopyOnRelease();
    press(); resolveWrite(); await tick();
    out.copied = L.StatecraftShareCopied();
  } else if (scenario === "sync-throw") {
    shareBehaviour = "throw";
    L.StatecraftShareOnRelease();
    press(); await tick();
    out.failed = L.StatecraftShareFailed(); out.downloads = clicks.length;
  }
  console.log(JSON.stringify(out));
})().catch((e) => { console.log(JSON.stringify({ crash: String(e) })); });
"""


@pytest.fixture(scope="module")
def run(tmp_path_factory):
    driver = tmp_path_factory.mktemp("share") / "driver.js"
    driver.write_text(DRIVER)

    def go(scenario):
        done = subprocess.run(["node", str(driver), str(JSLIB), scenario], capture_output=True, text=True, timeout=30)
        assert done.returncode == 0, done.stderr
        return json.loads(done.stdout.strip().splitlines()[-1])
    return go


def test_a_copy_that_lands_in_time_reports_the_picture(run):
    assert run("copy-still-lands") == {"copied": 1}


def test_a_late_clipboard_callback_cannot_act_after_a_lifecycle_reset(run):
    assert run("late-copy-after-reset") == {"copied": 0}


def test_a_late_clipboard_callback_cannot_act_after_a_new_main_action(run):
    assert run("late-copy-after-new-action") == {"copied": 0}


def test_a_synchronous_throw_from_share_still_downloads_the_picture(run):
    assert run("sync-throw") == {"failed": True, "downloads": 1}
