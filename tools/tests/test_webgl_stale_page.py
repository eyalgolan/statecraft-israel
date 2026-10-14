"""Issue #437: the WebGL page names its build's hashed Build/ files, and a browser may show a cached copy of the page after a
newer build replaced them. The page therefore asks the server for its current copy and loads the files that copy names.
These check the template's text (no Unity, no browser); the cached-browser run itself is described in the PR."""
import json
import re
import shutil
import subprocess
from pathlib import Path

import pytest

import package_release

TEMPLATE = Path(__file__).resolve().parents[2] / "unity/Assets/WebGLTemplates/Statecraft/index.html"
FILES = {"LOADER_FILENAME": "aaaa.loader.js", "DATA_FILENAME": "bbbb.data", "FRAMEWORK_FILENAME": "cccc.framework.js",
         "WORKER_FILENAME": "dddd.worker.js", "CODE_FILENAME": "eeee.wasm", "SYMBOLS_FILENAME": "ffff.symbols.json"}


def page() -> str:
    return TEMPLATE.read_text(encoding="utf-8")


def rendered() -> str:
    """The page as Unity writes it for a single-threaded wasm build: variables filled in, directives resolved."""
    out, keep = [], [True]
    for line in page().splitlines():
        if line.startswith("#if "):
            keep.append(keep[-1] and line.split()[1] in ("USE_WASM",))
        elif line.startswith("#endif"):
            keep.pop()
        elif keep[-1]:
            out.append(re.sub(r"\{\{\{ (\w+) \}\}\}", lambda m: FILES.get(m.group(1), m.group(0)), line))
    return "\n".join(out)


def test_the_loader_is_not_a_static_script_tag():
    """A static <script src="Build/..loader.js"> in a cached page would load the old loader before any check could run."""
    assert not re.search(r"<script[^>]*\ssrc=", page())


def test_the_page_fetches_its_own_current_copy_before_loading_the_build():
    text = page()
    assert re.search(r'fetch\(location\.href[^\n]*\{\s*cache:\s*"reload"\s*\}\)', text)
    assert re.search(r"\.then\(start\);\s*</script>", text)          # the build starts only after that fetch settles
    start = re.search(r"function start\(\) \{.*?\n      \}\n", text, re.S).group(0)
    assert text.count("createUnityInstance(") == 1 and "createUnityInstance(" in start


def test_every_build_file_the_page_names_is_read_back_from_the_current_copy():
    text = page()
    named = set(re.findall(r'(\w+)\s*[:=]\s*"Build/\{\{\{', text))
    assert named == {"loaderUrl", "dataUrl", "frameworkUrl", "workerUrl", "codeUrl", "symbolsUrl"}
    reread = set(re.findall(r'"(\w+Url)"', re.search(r"FRESH_KEYS = \[([^\]]*)\]", text).group(1)))
    assert named == reread


def test_the_packager_still_finds_every_file_of_a_rendered_page(tmp_path):
    (tmp_path / "index.html").write_text(rendered())
    assert package_release.build_files(tmp_path) == sorted(["aaaa.loader.js", "bbbb.data", "cccc.framework.js", "eeee.wasm"])


def run_use_current_page(page_html: str, current_html: str) -> dict:
    """Runs the page's own script (up to its fetch) in node, feeds useCurrentPage the current copy, and returns the
    names the page would then load."""
    script = re.search(r"<script>(.*)</script>", page_html, re.S).group(1).split("fetch(location.href")[0]
    driver = (script + "\nuseCurrentPage(" + json.dumps(current_html) + ");\n"
              "console.log(JSON.stringify({loaderUrl: loaderUrl, config: config}));\n")
    out = subprocess.run(["node", "-e", driver], capture_output=True, text=True, check=True).stdout
    return json.loads(out)


@pytest.mark.skipif(shutil.which("node") is None, reason="needs node to run the page's script")
def test_a_stale_page_loads_the_files_the_current_copy_names():
    """Review of #501: the text checks above pass with useCurrentPage's assignments removed; this runs it."""
    stale = re.sub(r"\{\{\{ JSON\.stringify\((\w+)\) \}\}\}", r'"\1"', rendered())
    current = stale.replace("aaaa", "1111").replace("bbbb", "2222").replace("cccc", "3333").replace("eeee", "5555")
    got = run_use_current_page(stale, current)
    assert got["loaderUrl"] == "Build/1111.loader.js"
    assert {k: got["config"].get(k) for k in ("dataUrl", "frameworkUrl", "codeUrl", "workerUrl", "symbolsUrl")} == {
        "dataUrl": "Build/2222.data", "frameworkUrl": "Build/3333.framework.js", "codeUrl": "Build/5555.wasm",
        "workerUrl": None, "symbolsUrl": None}
    assert got["config"]["streamingAssetsUrl"] == "StreamingAssets" and got["config"]["autoSyncPersistentDataPath"] is True
    # A copy that is not this template's (an error page, say) leaves the page's own names.
    kept = run_use_current_page(stale, "<html><body>Not Found</body></html>")
    assert kept["loaderUrl"] == "Build/aaaa.loader.js" and kept["config"]["dataUrl"] == "Build/bbbb.data"
