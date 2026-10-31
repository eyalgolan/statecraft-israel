"""scripts/unity_compile/compile.sh finds the editor's managed DLLs in both layouts (#1970): macOS keeps them under
<Contents>/Resources/Scripting, the Linux/Windows editor under Editor/Data directly. The script runs in a scratch copy
of the repo with no unity/Library, so it stops at the first missing file and the "missing:" line shows which layout it
looked in; no Unity, no dotnet."""
import shutil
import subprocess
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]


def _missing_line(tmp_path, scripting_dir):
    """Run a scratch copy of compile.sh against an editor root whose Managed/NetStandard DLLs sit in scripting_dir."""
    shutil.copytree(REPO / "scripts" / "unity_compile", tmp_path / "scripts" / "unity_compile")
    version = "6000.0.0f1"
    (tmp_path / "unity" / "ProjectSettings").mkdir(parents=True)
    (tmp_path / "unity" / "ProjectSettings" / "ProjectVersion.txt").write_text(f"m_EditorVersion: {version}\n")
    common = tmp_path / "scripts" / "unity_compile" / "Common.props"
    common.write_text(common.read_text().replace("6000.3.25f1", version))
    root = tmp_path / "editor"
    scripting = root / scripting_dir if scripting_dir else root
    for rel in ("Managed/UnityEngine/UnityEngine.CoreModule.dll", "NetStandard/ref/2.1.0/netstandard.dll"):
        (scripting / rel).parent.mkdir(parents=True, exist_ok=True)
        (scripting / rel).write_bytes(b"")
    run = subprocess.run(["bash", str(tmp_path / "scripts" / "unity_compile" / "compile.sh"), str(root)],
                         capture_output=True, text=True)
    assert run.returncode == 1, run.stdout + run.stderr
    return run.stdout.strip()


def test_macos_layout_is_found_under_resources_scripting(tmp_path):
    # Both editor DLLs are found, so the first missing file is the Library's, not the editor's.
    assert "ScriptAssemblies/Unity.RenderPipelines.Core.Runtime.dll" in _missing_line(tmp_path, "Resources/Scripting")


def test_linux_layout_is_found_directly_under_editor_data(tmp_path):
    assert "ScriptAssemblies/Unity.RenderPipelines.Core.Runtime.dll" in _missing_line(tmp_path, "")
