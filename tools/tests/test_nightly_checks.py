import zipfile

import pytest

import nightly_checks as nc

PASSED = "Passed!  - Failed:     0, Passed:  3216, Skipped:     0, Total:  3216, Duration: 1 m"   # format seen in real logs


def test_dotnet_totals_sum_every_project():
    assert nc.dotnet_totals(PASSED + "\n" + PASSED.replace("3216", "12")) == (0, 3228)
    assert nc.dotnet_totals("Failed!  - Failed:     5, Passed:  3211, Skipped:     0, Total:  3216") == (5, 3216)
    assert nc.dotnet_totals("Build succeeded.") == (0, 0)


def test_check_sh_must_pass_and_must_have_compiled_unity():
    ok = "== unity compile (dotnet, outside Unity)\n...\nALL CHECKS PASSED\n"
    assert nc.check_sh_verdict(ok, 0) is None
    assert "SKIPPED" in nc.check_sh_verdict("== unity compile: SKIPPED, no unity/Library ...\nALL CHECKS PASSED", 0)
    assert nc.check_sh_verdict(ok, 1) is not None
    assert nc.check_sh_verdict("== unity compile (dotnet, outside Unity)\n...", 0) is not None   # no pass line
    assert nc.check_sh_verdict("ALL CHECKS PASSED\n", 0) is not None   # the compile step never ran


def test_release_gate_needs_its_ok_line_and_exit_0():
    assert nc.release_gate_verdict("...\n\nRELEASE GATE OK\n", 0) is None
    assert nc.release_gate_verdict("...\nRELEASE GATE FAILED\n", 1) is not None
    assert nc.release_gate_verdict("RELEASE GATE OK\n", 1) is not None
    assert nc.release_gate_verdict("", 0) is not None


def test_product_version_reads_the_built_page():
    assert nc.product_version('productName: "S",\n        productVersion: "0.3.1-nightly.20261028",') == "0.3.1-nightly.20261028"
    assert nc.product_version("<html></html>") is None


MANIFEST = "# c\nhash1950 aa\nhash1952 bb\nsmoke5 cc\nsmoke-orders dd\n"


def test_expects_come_from_the_manifest():
    assert nc.golden_expects(MANIFEST) == ["GOLDEN OK hash1950 aa", "GOLDEN OK hash1952 bb", "GOLDEN OK smoke5 cc",
                                           "GOLDEN OK smoke-orders dd"]
    assert nc.smoke_expect(MANIFEST) == "SMOKE OK 5 turns 1948-10-15 cc"
    assert nc.smoke_orders_expect(MANIFEST) == "SMOKE ORDERS OK 1948-06-15 dd"
    with pytest.raises(ValueError):
        nc.smoke_expect("hash1950 aa\n")
    with pytest.raises(ValueError):
        nc.smoke_orders_expect("smoke-orders\n")


def test_the_passes_are_releasing_step_5_plus_smoke_orders(tmp_path):
    p = nc.web_check_passes(tmp_path, MANIFEST)
    assert len(p) == 7 and p[0][1:3] == ["--query", "golden&smoke=5"] and p[1][1:3] == ["--query", "smoke-orders"]
    assert all(x[0] == str(tmp_path) for x in p)
    assert [x[x.index("--startscreen") + 1] for x in p if "--startscreen" in x] == ["new", "recording", "new",
                                                                                      "recording"]
    assert sum("lang=he" in x for x in p) == 2 and "--persistence" in p[6]
    assert all(x[x.index("--stall") + 1] == "90" and x[x.index("--timeout") + 1] == "300" for x in p[2:])
    assert p[4][1:] == ["--startscreen", "new", "--query", "lang=he", "--timeout", "300", "--stall", "90"]
    assert p[0].count("--expect") == 5 and p[0][-1] == "SMOKE OK 5 turns 1948-10-15 cc"


def make_build(root, *, extra=True, data=900, wasm=90, fw=10):
    root.mkdir(parents=True, exist_ok=True)
    (root / "Build").mkdir(exist_ok=True)
    (root / "index.html").write_text(
        'var dataUrl = "Build/h1.data.gz"; var w = "Build/h1.wasm.gz"; src="Build/h1.framework.js.gz"; '
        'loader = "Build/h1.loader.js";')
    (root / "Build/h1.data.gz").write_bytes(b"d" * data)
    (root / "Build/h1.wasm.gz").write_bytes(b"w" * wasm)
    (root / "Build/h1.framework.js.gz").write_bytes(b"f" * fw)
    if extra:
        (root / "Build/old.data.gz").write_bytes(b"x" * 5000)   # a stale file index.html does not name
    return root


def test_sizes_sum_data_wasm_framework_named_by_index_html(tmp_path):
    build = make_build(tmp_path / "b")
    assert nc.web_sizes(build) == {"data": 900, "wasm": 90, "framework": 10}


def test_sizes_refuse_a_missing_or_ambiguous_file(tmp_path):
    build = make_build(tmp_path / "b")
    (build / "Build/h1.wasm.gz").unlink()
    with pytest.raises(ValueError):
        nc.web_sizes(build)
    build = make_build(tmp_path / "c")
    (build / "index.html").write_text('"Build/a.data.gz" "Build/b.data.gz" "Build/h.wasm.gz" "Build/h.framework.js.gz"')
    with pytest.raises(ValueError):
        nc.web_sizes(build)


def test_zip_and_folder_sizes_agree(tmp_path):
    build = make_build(tmp_path / "b")
    z = tmp_path / "w.zip"
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(build / "index.html", "index.html")
        for f in (build / "Build").iterdir():
            zf.write(f, f"Build/{f.name}")
    assert nc.web_sizes_from_zip(z) == nc.web_sizes(build)
    with zipfile.ZipFile(z, "w") as zf:
        zf.writestr("other.txt", "x")
    with pytest.raises(ValueError):
        nc.web_sizes_from_zip(z)


def test_size_growth_within_2_percent_passes_and_beyond_refuses():
    old = {"data": 900, "wasm": 90, "framework": 10}
    assert nc.size_verdict(old, {"data": 920, "wasm": 90, "framework": 10}, 0) is None          # +20 = 2.0%
    assert "1021" in nc.size_verdict(old, {"data": 921, "wasm": 90, "framework": 10}, 0)        # +21 > 2%
    assert nc.size_verdict(old, {"data": 921, "wasm": 90, "framework": 10}, 1) is None          # budgeted
    assert nc.size_verdict(old, {"data": 100, "wasm": 90, "framework": 10}, 0) is None          # shrinking is fine


def test_size_math_stays_exact_for_huge_totals():
    big = 10**30
    old = {"data": big, "wasm": 0, "framework": 0}
    assert nc.size_verdict(old, {"data": big + big // 50, "wasm": 0, "framework": 0}, 0) is None
    assert nc.size_verdict(old, {"data": big + big // 50 + 1, "wasm": 0, "framework": 0}, 0) is not None


def test_the_budget_line_is_exact_and_summed():
    bodies = ["x\nNightly size budget: +1.5 MB\n", "Nightly size budget: +2 MB", "nightly size budget +9 MB",
              "Nightly size budget: -3 MB"]
    assert nc.size_budget_bytes(bodies) == 3_500_000
    assert nc.size_budget_bytes([]) == 0


REAL_BOOT = "console: BOOT phase=screens ready at=8123 ms dt=40 ms\nconsole: GAME OK 1948-05-15 turn 0 hash ab\nPASS: saw GAME OK\n"


def test_boot_reads_screens_ready_and_needs_a_pass():
    assert nc.boot_ms(REAL_BOOT) == 8123
    assert nc.boot_ms(REAL_BOOT.replace("console: ", "")) == 8123
    for bad in (REAL_BOOT.replace("PASS: saw GAME OK", "TIMEOUT: never saw GAME OK"),
                REAL_BOOT.replace("PASS: saw GAME OK", "FAIL: BOOT FAIL"),
                REAL_BOOT.replace("screens ready", "map ready"), "", "PASS: saw GAME OK\n"):
        with pytest.raises(ValueError):
            nc.boot_ms(bad)


def test_the_boot_bound_is_the_baseline_spread():
    assert nc.boot_verdict(9000, [8000, 8100]) == ("collecting", None)
    base = [8000, 8100, 8050, 8200, 8150]                     # max 8200, spread 200 -> limit 8400
    assert nc.boot_verdict(8400, base) == ("checked", None)
    assert "8401" in nc.boot_verdict(8401, base)[1]


def test_notices_missing_from_a_zip(tmp_path):
    z = tmp_path / "w.zip"
    with zipfile.ZipFile(z, "w") as zf:
        for n in ("LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt"):
            zf.writestr(n, "x")
    assert nc.zip_missing_notices(z) == []
    with zipfile.ZipFile(z, "w") as zf:
        zf.writestr("LICENSE", "x")
    assert nc.zip_missing_notices(z) == ["NOTICE", "THIRD-PARTY-NOTICES.md", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt"]
    assert len(nc.zip_missing_notices(tmp_path / "absent.zip")) == 5
