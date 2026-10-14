import subprocess
import unittest
from contextlib import redirect_stderr, redirect_stdout
from io import StringIO
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch

import build_sim_for_unity as build


class BuildSimForUnityTests(unittest.TestCase):
    def test_publish_uses_repo_paths_even_from_another_directory(self):
        with TemporaryDirectory(prefix="sim build ") as directory:
            repo = Path(directory)
            output = repo / "unity/Assets/Plugins/Statecraft"

            def publish(command, **kwargs):
                self.assertEqual(command, ["dotnet", "publish", str(repo / "sim/Statecraft.Sim"),
                                           "-c", "Release", "-o", str(output)])
                self.assertEqual(kwargs, {"cwd": repo, "check": True})
                output.mkdir(parents=True)
                for name in ("Statecraft.Sim.dll", "YamlDotNet.dll"):
                    (output / name).write_bytes(b"dll")

            with patch.object(build, "REPO", repo), patch.object(build.subprocess, "run", side_effect=publish), \
                    redirect_stdout(StringIO()) as stdout:
                self.assertEqual(build.main(), 0)
            self.assertIn(str(output / "Statecraft.Sim.dll"), stdout.getvalue())
            self.assertIn(str(output / "YamlDotNet.dll"), stdout.getvalue())

    def test_publish_failure_keeps_its_exit_code_and_does_not_report_success(self):
        with patch.object(build.subprocess, "run", side_effect=subprocess.CalledProcessError(7, ["dotnet"])), \
                redirect_stdout(StringIO()) as stdout:
            self.assertEqual(build.main(), 7)
        self.assertEqual(stdout.getvalue(), "")

    def test_missing_dotnet_has_an_actionable_error(self):
        with patch.object(build.subprocess, "run", side_effect=FileNotFoundError), \
                redirect_stderr(StringIO()) as stderr:
            self.assertNotEqual(build.main(), 0)
        self.assertIn(".NET SDK", stderr.getvalue())

    def test_successful_publish_must_produce_both_dlls(self):
        for missing in ("Statecraft.Sim.dll", "YamlDotNet.dll"):
            with self.subTest(missing=missing), TemporaryDirectory() as directory:
                repo = Path(directory)
                output = repo / "unity/Assets/Plugins/Statecraft"
                output.mkdir(parents=True)
                for name in ("Statecraft.Sim.dll", "YamlDotNet.dll"):
                    if name != missing:
                        (output / name).write_bytes(b"dll")
                with patch.object(build, "REPO", repo), patch.object(build.subprocess, "run"), \
                        redirect_stderr(StringIO()) as stderr:
                    self.assertNotEqual(build.main(), 0)
                self.assertIn(missing, stderr.getvalue())
