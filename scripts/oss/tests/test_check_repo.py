import check_repo
import oss_policy

# Fake secrets are assembled at run time so that this file never holds a secret-shaped literal.
FAKE_ELEVENLABS = "sk_" + "ab" * 24
FAKE_GITHUB = "ghp_" + "A1" * 18
ALLOW = 'allow:\n  - check: home-paths\n    glob: "tests/fixtures/*"\n    reason: "redaction fixtures"\n'


def run(root, *args):
    return check_repo.main(["--root", str(root), *args])


def test_fake_key_is_reported_by_type_and_never_by_value(make_repo, capsys):
    root = make_repo({"promo/x.py": f'KEY = "{FAKE_ELEVENLABS}"\nTOKEN = "{FAKE_GITHUB}"\n'})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "secrets: promo/x.py:1: elevenlabs-key" in out
    assert "secrets: promo/x.py:2: github-token" in out
    assert FAKE_ELEVENLABS not in out and FAKE_GITHUB not in out


def test_clean_tree_passes(make_repo, capsys):
    root = make_repo({"sim/a.cs": "class A {}\n"})
    assert run(root, "--only", "secrets,emails,home-paths,sizes,symlinks") == 0
    assert "OK" in capsys.readouterr().out


def test_unity_cloud_ids_empty_pass(make_repo, capsys):
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset": "  cloudProjectId: \n  organizationId: \n"})
    assert run(root, "--only", "secrets") == 0


def test_unity_cloud_ids_filled_fail(make_repo, capsys):
    guid = "1234abcd-" + "12ab-" * 3 + "1234567890ab"
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset":
                      f"  cloudProjectId: {guid}\n  organizationId: some-org\n"})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "unity-cloud-project-id" in out and "unity-organization-id" in out
    assert guid not in out and "some-org" not in out


def test_personal_email_fails_and_noreply_passes(make_repo, capsys):
    personal = "someone" + "@" + "gmail.com"
    root = make_repo({"a.py": f'UA = "x ({personal})"\n',
                      "b.md": "Co-Authored-By: Claude <noreply" + "@" + "anthropic.com>\n",
                      "c.md": "x <5146175+eyalgolan" + "@" + "users.noreply.github.com>\n",
                      "d.py": "    @pytest.mark.parametrize('a', [1])\n"})
    assert run(root, "--only", "emails") == 1
    out = capsys.readouterr().out
    assert "emails: a.py:1: personal-email" in out
    assert "b.md" not in out and "c.md" not in out and "d.py" not in out
    assert personal not in out


def test_package_ids_with_numeric_versions_are_not_emails(make_repo, capsys):
    # Review C1: ProjectSettings.asset and docs carry Unity package ids such as these.
    root = make_repo({"ProjectSettings.asset": "  templatePackageId: com.unity.template.urp-blank" + "@" + "17.0.14\n",
                      "m0.md": "com.unity.ai.assistant" + "@" + "2.20.0-pre.2 and pkg" + "@" + "17.0.14\n"})
    assert run(root, "--only", "emails") == 0


def test_home_path_fails_and_tilde_passes(make_repo, capsys):
    root = make_repo({"a.md": "see /Users/" + "someone/x\n", "b.md": "see ~/Library/Caches\n",
                      "c.md": "see /home/" + "someone/x\n"})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "a.md:1: home-path" in out and "c.md:1: home-path" in out and "b.md" not in out


def test_home_path_inside_a_binary_fails(make_repo, capsys):
    # An FBX string property as Blender writes it: length-prefixed, between NUL bytes (issue #659).
    path = ("/Users/" + "someone/art/tex_0.jpg").encode()
    blob = b"Kaydara FBX Binary  \x00\x1a\x00" + b"\x00" * 16 + b"S" + len(path).to_bytes(4, "little") + path + b"\x00" * 8
    scrubbed = blob.replace(path, b"scrubbed-path".ljust(len(path), b"_"))
    root = make_repo({"m/a.fbx": blob, "m/b.fbx": scrubbed})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "home-paths: m/a.fbx: home-path-in-binary" in out and "m/b.fbx" not in out
    assert "someone" not in out


def _fbx(path: bytes) -> bytes:
    return b"Kaydara FBX Binary  \x00\x1a\x00" + b"\x00" * 16 + b"S" + len(path).to_bytes(4, "little") + path + b"\x00" * 8


def test_temp_and_session_paths_inside_a_binary_fail_in_utf8_and_utf16(make_repo, capsys):
    # The PR #680 shape: a macOS temp folder holding an agent session named after the account, with no "/Users/" in it.
    leak = "/private/tmp/" + "claude-501/-Users-" + "someone/0f3c/scratchpad/m/m_basecolor.jpg"
    shapes = {
        "m/a.fbx": _fbx(leak.encode()),
        "m/b.fbx": _fbx(leak.encode("utf-16-le")),
        "m/c.fbx": _fbx(b"\x00" + leak.encode("utf-16-be")),
        "m/d.fbx": _fbx(("/var/" + "folders/x1/T/m.jpg").encode()),
        "m/e.fbx": _fbx(("work/" + "claude-501/m.jpg").encode()),
        "m/f.fbx": _fbx(("C:\\x\\-Users-" + "someone\\m.jpg").encode()),
        "m/g.fbx": _fbx(("/home/" + "someone/m.jpg").encode("utf-16-le")),
        "m/ok.fbx": _fbx("m_basecolor.jpg".encode() + "m_basecolor.jpg".encode("utf-16-le")),
    }
    root = make_repo(shapes)
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    for f in shapes:
        if f != "m/ok.fbx":
            assert f"home-paths: {f}: home-path-in-binary" in out, f
    assert "m/ok.fbx" not in out and "someone" not in out


def test_allowlisted_fixture_passes_and_the_same_line_elsewhere_fails(make_repo, capsys):
    line = 'var p = "/Users/' + 'someone/game";\n'
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "tests/fixtures/a.cs": line, "src/b.cs": line})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "src/b.cs:1: home-path" in out and "tests/fixtures/a.cs" not in out


def test_stale_allow_row_is_reported(make_repo, capsys):
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "src/b.cs": "fine\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "stale-allow home-paths tests/fixtures/*" in capsys.readouterr().out


def test_allow_row_without_reason_is_reported(make_repo, capsys):
    rows = 'allow:\n  - check: home-paths\n    glob: "x/*"\n'
    root = make_repo({"scripts/oss/allow.yaml": rows, "x/a.cs": "/home/" + "someone/a\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "allow-row-without-reason x/*" in capsys.readouterr().out


def test_untracked_files_are_not_scanned(make_repo, capsys):
    root = make_repo({"a.txt": "fine\n"}, untracked={"secret.env": f"K={FAKE_ELEVENLABS}\n"})
    assert run(root, "--only", "secrets") == 0


def test_binary_files_are_not_text_scanned(make_repo, capsys):
    root = make_repo({"img.png": b"\x89PNG\x00\x00" + FAKE_GITHUB.encode()})
    assert run(root, "--only", "secrets") == 0


def test_large_file_fails(make_repo, capsys):
    root = make_repo({"big.bin": b"\x00" * (oss_policy.MAX_FILE_BYTES + 1)})
    assert run(root, "--only", "sizes") == 1
    assert "sizes: big.bin: over-size-limit" in capsys.readouterr().out


def test_absolute_symlink_fails(make_repo, capsys):
    root = make_repo({"link": "->/etc/hosts", "ok": "->a.txt", "a.txt": "x\n"})
    assert run(root, "--only", "symlinks") == 1
    out = capsys.readouterr().out
    assert "symlinks: link: symlink-outside-repo" in out and "ok:" not in out


def test_required_files_are_named_when_missing(make_repo, capsys):
    root = make_repo({"README.md": "x\n"})
    assert run(root, "--only", "required") == 1
    out = capsys.readouterr().out
    assert "required: LICENSE: missing" in out and "required: README.md:" not in out


def test_release_mode_refuses_open_decisions(make_repo, capsys):
    root = make_repo({"docs/oss/DECISIONS.md": "- [x] D1 decided\n- [ ] D2 open\n"})
    assert run(root, "--only", "decisions") == 0
    assert run(root, "--only", "decisions", "--release") == 1
    assert "decisions: docs/oss/DECISIONS.md:2: open-decision" in capsys.readouterr().out


def test_unknown_check_name_is_an_error(make_repo, capsys):
    root = make_repo({"a.txt": "x\n"})
    assert run(root, "--only", "nonsense") == 2


# Review I4: scanner gaps.
def test_unquoted_env_assignment_is_a_secret(make_repo, capsys):
    value = "q" * 40
    root = make_repo({"setup.sh": "export OPENAI_" + f"API_KEY={value}\n", "ok.sh": "export OPENAI_API_KEY=$KEY\n"})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "secrets: setup.sh:1: env-assignment" in out and "ok.sh" not in out and value not in out


def test_non_utf8_and_utf16_files_are_still_scanned(make_repo, capsys):
    aws = "AKIA" + "ABCDEFGHIJ" + "123456"
    mail = "someone" + "@" + "gmail.com"
    latin = f"caf\xe9 {aws}\n{mail}\n".encode("latin-1")
    utf16 = f"{aws}\n".encode("utf-16")
    root = make_repo({"latin.txt": latin, "wide.txt": utf16})
    assert run(root, "--only", "secrets,emails") == 1
    out = capsys.readouterr().out
    assert "secrets: latin.txt:1: aws-access-key-id" in out and "emails: latin.txt:2: personal-email" in out
    assert "secrets: wide.txt:1: aws-access-key-id" in out


def test_more_personal_path_shapes_are_home_paths(make_repo, capsys):
    root = make_repo({"a.md": "cd /Users/" + "someone\n", "b.md": "C:\\Users\\" + "someone\\x\n",
                      "c.md": "see -Users-" + "someone-israel-game\n", "d.md": "/Users/Shared and ~/x\n"})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    for f in ("a.md", "b.md", "c.md"):
        assert f"home-paths: {f}:1: home-path" in out
    assert "d.md" not in out


def test_email_allowlist_is_exact_addresses_not_whole_domains(make_repo, capsys):
    root = make_repo({"a.md": "Claude <noreply" + "@" + "anthropic.com>\n", "b.md": "x" + "@" + "anthropic.com\n"})
    assert run(root, "--only", "emails") == 1
    out = capsys.readouterr().out
    assert "b.md:1: personal-email" in out and "a.md" not in out


def test_outside_a_checkout_is_a_readable_error_not_a_traceback(tmp_path, capsys):
    assert run(tmp_path) == 2
    assert "not a git checkout" in capsys.readouterr().err


# Conflict markers are assembled at run time so that this file never holds a line that starts with one.
OURS, BASE, SEP, THEIRS = "<" * 7, "|" * 7, "=" * 7, ">" * 7


def test_committed_conflict_block_is_reported_at_each_marker_line(make_repo, capsys):
    block = f"# Changelog\n{OURS} HEAD\n- ours\n{SEP}\n- theirs\n{THEIRS} origin/main\n"
    root = make_repo({"CHANGELOG.md": block})
    assert run(root, "--only", "conflict-markers") == 1
    out = capsys.readouterr().out
    assert f"conflict-markers: CHANGELOG.md:2: conflict-marker {OURS}" in out
    assert f"conflict-markers: CHANGELOG.md:6: conflict-marker {THEIRS}" in out
    assert "CHANGELOG.md:4:" not in out


def test_diff3_base_marker_is_reported(make_repo, capsys):
    block = f"{OURS} ours\na\n{BASE} base\nb\n{SEP}\nc\n{THEIRS} theirs\n"
    root = make_repo({"docs/oss/DECISIONS.md": block})
    assert run(root, "--only", "conflict-markers") == 1
    assert f"conflict-markers: docs/oss/DECISIONS.md:3: conflict-marker {BASE}" in capsys.readouterr().out


def test_lookalike_lines_are_not_conflict_markers(make_repo, capsys):
    root = make_repo({
        "setext.md": f"Title\n{SEP}\n\ntext\n",
        "rst.rst": f"{SEP}\nTitle\n{SEP}\n",
        "fence.md": f"```\n{SEP}\n{'=' * 4}\n```\n",
        "table.md": "| a | b |\n|---|---|\n| 1 | 2 |\n",
        "art.txt": f"{OURS}\n{THEIRS}\n{BASE}\n",  # seven characters with no label after them
        "long.txt": f"{OURS}< x\n{THEIRS}> x\n",  # eight characters: git writes seven
        "inline.md": f"text {OURS} HEAD\n  {THEIRS} indented\n",  # not at the start of the line
    })
    assert run(root, "--only", "conflict-markers") == 0
    assert "OK: checks conflict-markers" in capsys.readouterr().out


def test_conflict_markers_is_in_the_default_run(make_repo, capsys):
    assert "conflict-markers" in check_repo.CHECKS
    root = make_repo({"a.md": f"{OURS} HEAD\n"})
    assert run(root) == 1
    assert "conflict-markers: a.md:1:" in capsys.readouterr().out


def test_binary_files_are_not_scanned_for_conflict_markers(make_repo, capsys):
    root = make_repo({"img.png": b"\x89PNG\x00\x00\n" + f"{OURS} HEAD\n".encode()})
    assert run(root, "--only", "conflict-markers") == 0
