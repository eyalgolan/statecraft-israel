import check_repo

RULES = """rules:
  - glob: "img/*.png"
    licence: CC-BY-4.0
    source: ours
    status: verified
  - glob: "future/*"
    licence: Copernicus-DEM
    source: visual-v1 bakes
    status: verified
    allow_empty: true
removed: []
"""


def run(root, *args):
    return check_repo.main(["--root", str(root), "--only", "assets", *args])


def test_covered_binary_passes(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG"})
    assert run(root) == 0


def test_uncovered_binary_is_named(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "art/new.jpg": b"\xff\xd8"})
    assert run(root) == 1
    assert "assets: art/new.jpg: no-provenance" in capsys.readouterr().out


def test_every_listed_asset_extension_needs_provenance(make_repo, capsys):
    exts = [".svg", ".psd", ".exr", ".fbx", ".woff", ".woff2", ".otf", ".tga", ".hdr", ".glb", ".gltf", ".blend",
            ".wav", ".ogg", ".mp3"]
    files = {"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG"}
    files.update({f"art/f{e}": "text-like content\n" for e in exts})
    root = make_repo(files)
    assert run(root) == 1
    out = capsys.readouterr().out
    for e in exts:
        assert f"assets: art/f{e}: no-provenance" in out, e


def test_a_file_git_sees_as_binary_needs_provenance_whatever_its_name(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "data/blob.dat": b"\x00\x01\x02binary"})
    assert run(root) == 1
    assert "assets: data/blob.dat: no-provenance" in capsys.readouterr().out


def test_provenance_globs_cover_map_data_text_files(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES, "img/a.png": b"\x89PNG", "content/map/x.json": "{}"})
    assert run(root) == 1
    assert "content/map/x.json: no-provenance" in capsys.readouterr().out


def test_stale_rule_is_named_unless_allow_empty(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": RULES})
    assert run(root) == 1
    out = capsys.readouterr().out
    assert "assets: ASSETS.yaml: stale-rule img/*.png" in out and "future/*" not in out


def test_rule_missing_a_key_is_a_bad_rule_not_a_crash(make_repo, capsys):
    root = make_repo({"ASSETS.yaml": "rules:\n  - licence: MIT\n    source: x\n    status: verified\n"})
    assert run(root) == 1
    assert "bad-rule missing glob" in capsys.readouterr().out


def test_cc_by_nc_nd_4_0_is_an_allowed_licence(make_repo, capsys):
    rules = RULES.replace("licence: CC-BY-4.0", "licence: CC-BY-NC-ND-4.0")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 0


def test_unknown_licence_and_bad_status(make_repo, capsys):
    bad = RULES.replace("licence: CC-BY-4.0", "licence: WTFPL").replace("status: verified\n  - glob: \"future",
                                                                      "status: maybe\n  - glob: \"future")
    root = make_repo({"ASSETS.yaml": bad, "img/a.png": b"\x89PNG"})
    assert run(root) == 1
    out = capsys.readouterr().out
    assert "unknown-licence WTFPL" in out and "bad-status maybe" in out


def test_release_mode_refuses_owner_verify_rows(make_repo, capsys):
    rules = RULES.replace("source: ours\n    status: verified", "source: ours\n    status: owner-verify")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 1
    assert "assets: ASSETS.yaml: owner-verify img/*.png" in capsys.readouterr().out


def test_release_mode_refuses_unknown_licence(make_repo, capsys):
    rules = RULES.replace("licence: CC-BY-4.0", "licence: unknown")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 1


def test_portraits_file_is_read_too(make_repo, capsys):
    portraits = 'rules:\n  - glob: "p/a.jpg"\n    licence: Public-Domain\n    source: commons\n    status: verified\n'
    root = make_repo({"ASSETS.yaml": RULES, "ASSETS.portraits.yaml": portraits, "img/a.png": b"\x89PNG",
                      "p/a.jpg": b"\xff\xd8"})
    assert run(root) == 0


def test_removed_path_must_not_be_tracked(make_repo, capsys):
    rules = RULES.replace("removed: []", 'removed:\n  - { path: "img/a.png", reason: "x", date: 2026-10-11 }')
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 1
    assert "img/a.png: removed-but-tracked" in capsys.readouterr().out


def test_the_israel_state_archives_terms_are_an_allowed_licence_but_owner_verify_blocks_a_release(make_repo, capsys):
    rules = """rules:
  - glob: "audio/*.wav"
    licence: Israel State Archives terms of use
    source: Israel State Archives item
    status: owner-verify
removed: []
"""
    root = make_repo({"ASSETS.yaml": rules, "audio/a.wav": b"RIFF"})
    assert run(root) == 0
    assert run(root, "--release") == 1


def test_elevenlabs_owner_permission_is_allowed_but_owner_verify_blocks_release(make_repo, capsys):
    rules = RULES.replace("licence: CC-BY-4.0", "licence: LicenseRef-ElevenLabs-Owner-Permission")
    root = make_repo({"ASSETS.yaml": rules, "img/a.png": b"\x89PNG"})
    assert run(root) == 0
    assert run(root, "--release") == 0
    (root / "ASSETS.yaml").write_text(rules.replace("status: verified", "status: owner-verify", 1))
    assert run(root) == 0
    assert run(root, "--release") == 1
    assert "owner-verify img/*.png" in capsys.readouterr().out
