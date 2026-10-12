"""Shared rules for the open-source checks: what a secret looks like, which emails and paths are personal, what files a
public tree must carry, which files need a provenance row, and how large a file may be. One module, so check_repo.py
(tip of tree) and scan_history.py (every commit) can never disagree about what they look for."""
from __future__ import annotations

import re

SECRET_PATTERNS: dict[str, re.Pattern[str]] = {
    "elevenlabs-key": re.compile(r"\bsk_[0-9a-f]{40,}\b"),
    "anthropic-key": re.compile(r"sk-ant-[A-Za-z0-9_\-]{20,}"),
    "openai-key": re.compile(r"\bsk-(?:proj-)?[A-Za-z0-9]{20,}"),
    "aws-access-key-id": re.compile(r"\b(?:AKIA|ASIA)[0-9A-Z]{16}\b"),
    "github-token": re.compile(r"\b(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}"),
    "google-api-key": re.compile(r"\bAIza[0-9A-Za-z_\-]{35}\b"),
    "slack-token": re.compile(r"xox[baprs]-[A-Za-z0-9-]{10,}"),
    "private-key": re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----"),
    "jwt": re.compile(r"\beyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}"),
    "secret-assignment": re.compile(
        r"(?i)(?:api[_-]?key|secret|token|passw(?:or)?d)['\"]?\s*[:=]\s*['\"][A-Za-z0-9_\-/+=]{16,}['\"]"),
    # The unquoted shell / .env shape: export OPENAI_API_KEY=<value> (review I4).
    "env-assignment": re.compile(
        r"(?:^|[^A-Za-z0-9_])[A-Z0-9_]*(?:API_?KEY|SECRET|TOKEN|PASSWORD)[A-Z0-9_]*[ \t]*[:=][ \t]*[A-Za-z0-9_/+=-]{16,}"),
    # Unity writes these into ProjectSettings.asset when the project is linked to Unity Cloud. Empty is fine. The value
    # shapes are narrow on purpose (a GUID; a slug that ends the line) so that this file and its tests, which mention
    # the keys, do not match themselves.
    "unity-cloud-project-id": re.compile(r"cloudProjectId:[ \t]*[0-9a-fA-F]{8}-"),
    "unity-organization-id": re.compile(r"organizationId:[ \t]*[A-Za-z0-9][\w.-]*[ \t]*$"),
    "unity-licence-secret": re.compile(r"(?i)UNITY_(?:SERIAL|PASSWORD|LICENSE)\s*[:=]\s*\S{6,}"),
}

# The local part must start with a letter or digit (so "@pytest.mark" is not an address), and the last domain label
# must be letters only (so a Unity package id such as "com.unity.template.urp-blank@17.0.14" is not an address).
EMAIL = re.compile(r"(?<![\w.+-])[A-Za-z0-9][A-Za-z0-9._%+\-]*@((?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,})(?![\w.-])")
ALLOWED_EMAIL_DOMAINS = frozenset({"users.noreply.github.com", "example.com", "example.org", "example.net"})
ALLOWED_EMAILS = frozenset({"noreply@anthropic.com", "noreply@github.com"})  # exact addresses, never a whole domain

HOME_PATH = re.compile(
    r"(?:/Users/(?!Shared\b)|/home/)[A-Za-z][\w.\-]*(?:/|\b)|[A-Za-z]:\\Users\\[A-Za-z]|-Users-[a-z][\w.]*-")

MAX_FILE_BYTES = 10 * 1024 * 1024

REQUIRED_FILES: tuple[str, ...] = (
    "LICENSE", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt", "LICENSES/CC0-1.0.txt",
    "LICENSES/YamlDotNet-MIT.txt", "LICENSES/Statecraft-Models.txt", "LICENSING.md", "NOTICE", "THIRD-PARTY-NOTICES.md", "ASSETS.yaml",
    "README.md", "README.he.md", "BUILDING.md", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "SECURITY.md",
    "TRADEMARK.md", "CHANGELOG.md", "docs/CONTENT_POLICY.md",
    "docs/UNTRUSTED_PR_REVIEW.md", "docs/RELEASING.md", "docs/oss/DECISIONS.md", ".github/CODEOWNERS",
    ".github/pull_request_template.md", ".github/workflows/ci.yml",
)

# Files whose provenance ASSETS.yaml must record (check "assets"). Any file git itself treats as binary counts too.
BINARY_EXTENSIONS = frozenset({
    ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff", ".tga", ".psd", ".exr", ".hdr", ".svg",
    ".mp4", ".mov", ".webm", ".r16", ".r16z", ".u16", ".raw",
    ".ttf", ".otf", ".woff", ".woff2",
    ".wav", ".ogg", ".mp3", ".flac", ".aiff", ".m4a",
    ".fbx", ".obj", ".blend", ".glb", ".gltf",
    ".pdf", ".unitypackage",
})
PROVENANCE_GLOBS: tuple[str, ...] = ("content/map/*", "unity/Assets/StreamingAssets/map/*")

ALLOWED_LICENCES = frozenset({"MIT", "CC-BY-4.0", "CC-BY-SA-4.0", "CC0-1.0", "OFL-1.1", "Public-Domain",
                              "Copernicus-DEM", "IMS-data.gov.il", "Unity-Companion-License", "LicenseRef-Statecraft-Models", "unknown"})
STATUSES = frozenset({"verified", "owner-verify"})
RULE_KEYS = ("glob", "licence", "source", "status")


def secret_kinds(line: str) -> list[str]:
    return [kind for kind, rx in SECRET_PATTERNS.items() if rx.search(line)]


def email_kinds(line: str) -> list[str]:
    for m in EMAIL.finditer(line):
        domain = m.group(1).lower()
        if domain in ALLOWED_EMAIL_DOMAINS or m.group(0).lower() in ALLOWED_EMAILS:
            continue
        return ["personal-email"]
    return []


def home_path_kinds(line: str) -> list[str]:
    return ["home-path"] if HOME_PATH.search(line) else []
