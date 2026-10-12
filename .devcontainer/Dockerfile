# Base image: Ubuntu 24.04, the same OS as the CI runner (ubuntu-24.04 in .github/workflows/ci.yml). Pinned by version
# and digest; bump it by hand.
FROM mcr.microsoft.com/devcontainers/base:3.0.8-ubuntu24.04@sha256:d7c468679f45a52ad3673d06656b5bf16e488990b17216a1fa295d9e1f89d724

# uv 0.12.5 is the version CI uses (astral-sh/setup-uv, `version:` in .github/workflows/ci.yml).
# Keep this tag equal to that value, and change both together.
COPY --from=ghcr.io/astral-sh/uv:0.12.5@sha256:e85be844203885286c60ffad8a858d48afb6c5a5c237ca0e67f12e74b8f174b1 /uv /uvx /usr/local/bin/
