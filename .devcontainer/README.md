# Dev container

This container runs the headless checks in a clean Linux environment, for example in a GitHub Codespace
or for a coding agent. It has no Unity. Unity work (`unity/`, builds, edit-mode tests) needs a local machine.

## What it holds

| Tool | Version | Where the version comes from |
| --- | --- | --- |
| .NET SDK | 10.0.401 | `dotnet-version` in `.github/workflows/ci.yml`. The sim targets `net10.0`. There is no `global.json`. |
| uv | 0.12.5 | `version` of `astral-sh/setup-uv` in `.github/workflows/ci.yml` |
| Python | 3.12 | System Python of the `ubuntu-24.04` CI runner. The scripts need `>=3.11`. |
| OS | Ubuntu 24.04 | `runs-on: ubuntu-24.04` in `.github/workflows/ci.yml` |
| git | from the base image | |

When CI changes one of these versions, change it here too. Dependabot bumps the uv image tag and the features.

`postCreateCommand` runs `dotnet restore sim/Statecraft.sln`.

## Run the checks

```bash
dotnet test sim/Statecraft.sln -c Release
uv run scripts/oss/check_repo.py
uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q
```

The data tool tests (`tools/tests`) run in CI too. See `.github/workflows/ci.yml` for the full command.
