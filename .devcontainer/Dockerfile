# Base image: Ubuntu 24.04, the same OS as the CI runner (ubuntu-24.04 in .github/workflows/ci.yml).
FROM mcr.microsoft.com/devcontainers/base:ubuntu-24.04

# uv 0.12.5 is the version CI uses (astral-sh/setup-uv, `version:` in .github/workflows/ci.yml).
# Keep this tag equal to that value. Dependabot's docker ecosystem bumps it (see .github/dependabot.yml).
COPY --from=ghcr.io/astral-sh/uv:0.12.5 /uv /uvx /usr/local/bin/
