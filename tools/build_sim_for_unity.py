"""Publish the netstandard2.1 sim and YamlDotNet into Unity's plugin folder.

Run with `uv run tools/build_sim_for_unity.py` (or Python directly).
Requires the .NET SDK on PATH; no Python packages or shell are needed.
"""
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]


def main() -> int:
    output = REPO / "unity/Assets/Plugins/Statecraft"
    try:
        subprocess.run(["dotnet", "publish", str(REPO / "sim/Statecraft.Sim"),
                        "-c", "Release", "-o", str(output)], cwd=REPO, check=True)
    except FileNotFoundError:
        print("Cannot find dotnet. Install the .NET SDK and add it to PATH (see docs/dev-setup.md).",
              file=sys.stderr)
        return 1
    except subprocess.CalledProcessError as error:
        return error.returncode

    dlls = [output / name for name in ("Statecraft.Sim.dll", "YamlDotNet.dll")]
    missing = [str(dll) for dll in dlls if not dll.is_file()]
    if missing:
        print("Publish did not produce the required DLLs: " + ", ".join(missing), file=sys.stderr)
        return 1
    for dll in dlls:
        print(dll)
    return 0


if __name__ == "__main__":
    sys.exit(main())
