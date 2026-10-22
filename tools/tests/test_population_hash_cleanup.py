"""Run the real population-hash test in isolation; its content copy must never survive.

Faults are injected only into a private source copy. This keeps the tested path real without
adding failure hooks to the C# test or touching another test process's temporary files.
"""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

import pytest


REPO = Path(__file__).resolve().parents[2]
TEST = "SaveGameTests.TheKitPopulationFileIsOutsideTheContentHash"


@pytest.mark.parametrize("fault", [None, "copy", "load", "assertion"])
def test_population_hash_check_cleans_its_copy_on_success_and_failure(tmp_path, fault):
    with tempfile.TemporaryDirectory(prefix="population-cleanup-", dir=tmp_path) as directory:
        root = Path(directory)
        repo = root / "repo"
        shutil.copytree(REPO / "sim", repo / "sim", ignore=shutil.ignore_patterns("bin", "obj", "TestResults"))
        shutil.copytree(REPO / "content", repo / "content")

        source = repo / "sim/Statecraft.Sim.Tests/SaveGameTests.cs"
        text = source.read_text(encoding="utf-8")
        start = text.index("    public void TheKitPopulationFileIsOutsideTheContentHash()")
        end = text.index("\n    /// <summary>", start)
        body = text[start:end]
        if fault == "copy":
            target = "File.Copy(f, Path.Combine(copy, rel));"
            replacement = target + '\n            if (File.Exists(Path.Combine(copy, rel))) throw new IOException("population cleanup copy probe");'
        else:
            target = "Assert.Equal(ContentLoader.Load(real).Hash, ContentLoader.Load(copy).Hash);"
            if fault == "load":
                replacement = 'File.WriteAllText(Path.Combine(copy, "rules.yaml"), "invalid: [");\n        ' + target
            elif fault == "assertion":
                replacement = 'File.AppendAllText(Path.Combine(copy, "rules.yaml"), "\\n# population cleanup assertion probe\\n");\n        ' + target
            else:
                replacement = target
        assert body.count(target) == 1, "the real population-hash test path must be selected exactly once"
        source.write_text(text[:start] + body.replace(target, replacement) + text[end:], encoding="utf-8")

        temporary = root / "temporary"
        temporary.mkdir()
        sentinel = temporary / "statecraft-nopop-another-run"
        sentinel.mkdir()
        (sentinel / "keep.txt").write_text("another run's data", encoding="utf-8")
        results = root / "results"
        run = subprocess.run(
            ["dotnet", "test", str(repo / "sim/Statecraft.sln"), "-c", "Release", "--nologo",
             "--filter", f"FullyQualifiedName={TEST}", "--logger", "trx;LogFileName=population.trx",
             "--results-directory", str(results)],
            cwd=repo, env={**os.environ, "TMPDIR": str(temporary), "TMP": str(temporary), "TEMP": str(temporary)},
            capture_output=True, text=True, timeout=180,
        )
        output = run.stdout + run.stderr
        report = results / "population.trx"
        assert report.is_file(), output
        counters = ET.parse(report).find(".//{*}Counters")
        assert counters is not None and counters.get("executed") == "1", output
        assert counters.get("total") == "1", output
        assert "warning CS" not in output, output
        assert run.returncode == (0 if fault is None else 1), output
        assert counters.get("passed" if fault is None else "failed") == "1", output
        if fault is not None:
            expected = {"copy": "population cleanup copy probe", "load": "rules.yaml", "assertion": "Assert.Equal() Failure"}[fault]
            assert expected in output + report.read_text(encoding="utf-8"), output
        assert (sentinel / "keep.txt").read_text(encoding="utf-8") == "another run's data"
        survivors = sorted(p.name for p in temporary.glob("statecraft-nopop-*") if p != sentinel)
        assert survivors == [], f"population-hash test left its own content copy behind: {survivors}"
