"""#3043 item 2: web_check's expected toggle and pose lists (what `profile` checks a ?vprofile run against) match the C#.

FrameProfile.Run prices "all", "-<name>" for each VisualToggles name registered at run time, then "none", at each
PromoPose.All pose (plus "opening" and PromoPose.CloseUps under vclose). The toggles are registered by the features
themselves, across the map code, some through name constants; this reads every VisualToggles.Register call in
unity/Assets/Scripts and resolves its name, so a toggle added, renamed or removed in C# fails here until the list follows.
"""
import re
from pathlib import Path

import web_check as wc

REPO = Path(__file__).resolve().parents[2]
SCRIPTS = REPO / "unity/Assets/Scripts"
REGISTER = re.compile(r"\bVisualToggles\.Register\(\s*([A-Za-z_][\w.]*|\"[^\"]*\")\s*,")


def code(path: Path) -> str:
    """The file without its // and /// comments (the doc comments quote Register calls as examples)."""
    return "\n".join(line.split("//", 1)[0] for line in path.read_text(encoding="utf-8").splitlines())


def class_file(name: str) -> Path:
    hits = [p for p in SCRIPTS.rglob("*.cs") if re.search(rf"\bclass {re.escape(name)}\b", code(p))]
    assert len(hits) == 1, f"class {name}: {hits}"
    return hits[0]


def constant(path: Path, name: str) -> str:
    """The string value of the const `name` declared in `path` (one declaration may list several: `A = "a", B = "b"`)."""
    m = re.search(rf"\bconst string\b[^;]*?\b{re.escape(name)}\s*=\s*\"([^\"]*)\"", code(path), re.S)
    assert m, f"{path.relative_to(REPO)}: no const string {name}"
    return m.group(1)


def registered() -> dict[str, set[str]]:
    """Every toggle name a VisualToggles.Register call in the game code passes, with the files that register it."""
    names: dict[str, set[str]] = {}
    for path in sorted(SCRIPTS.rglob("*.cs")):
        for arg in REGISTER.findall(code(path)):
            if arg.startswith('"'):
                name = arg.strip('"')
            elif "." in arg:
                cls, const = arg.rsplit(".", 1)
                name = constant(class_file(cls.rsplit(".", 1)[-1]), const)
            else:
                name = constant(path, arg)
            names.setdefault(name, set()).add(path.relative_to(SCRIPTS).as_posix())
    return names


def test_the_expected_toggles_are_the_ones_the_game_registers():
    found = registered()
    assert len(found) >= 15, found       # the parse still sees the map's registrations
    expected = set(wc.VISUAL_TOGGLES) | set(wc.OPTIONAL_VISUAL_TOGGLES)
    assert not set(wc.VISUAL_TOGGLES) & set(wc.OPTIONAL_VISUAL_TOGGLES)
    assert sorted(found) == sorted(expected), (
        f"registered in C# only: {sorted(set(found) - expected)}; listed in web_check only: {sorted(expected - set(found))}")
    assert len(wc.VISUAL_TOGGLES) == len(set(wc.VISUAL_TOGGLES))


def test_name_constants_resolve_and_unrelated_constants_are_not_toggles():
    found = registered()
    assert "Map/MapBorders.cs" in found["borders"]                     # MapBorders.ToggleName
    assert found["grounddetail"] == {"Map/GroundDetail.cs"} and found["groundscrub"] == {"Map/GroundDetail.cs"}
    assert found["train"] == {"Map/Routes/RouteLayer.cs"}              # TrainToggle, registered in two branches
    assert "city-projects-toggle" not in found                         # GddHexPanel.ToggleName is a UI element name


def test_the_optional_toggles_are_the_flag_only_ones():
    """The train is built only for a train mode or from its service date (RouteLayer.Build) and the unit showcase only
    under ?units (UnitShowcase.Requested, GameRoot): a plain ?vprofile run may lack them. Every other toggle is expected."""
    assert set(wc.OPTIONAL_VISUAL_TOGGLES) == {"train", "unitshowcase"}
    route = code(SCRIPTS / "Map/Routes/RouteLayer.cs")
    assert re.search(r"if \(trainRoute != null && train == TrainMode\.Service\)", route)
    root = code(SCRIPTS / "UI/GameRoot.cs")
    assert re.search(r"UnitShowcase\.Requested\(\)\)\s*\n\s*Statecraft\.Game\.Map\.Units\.UnitShowcase\.Spawn\(", root)


def test_the_expected_poses_are_frame_profiles():
    profile, pose = code(SCRIPTS / "Map/FrameProfile.cs"), code(SCRIPTS / "Map/PromoPose.cs")
    assert 'foreach (var name in VisualToggles.Names) configs.Add(("-" + name, name));' in profile
    assert "new List<PromoPose>(PromoPose.All)" in profile and "poses.AddRange(PromoPose.CloseUps);" in profile

    def names(member):
        m = re.search(rf"\b{member} {{ get; }} = new\[\]\s*\{{(.*?)\}};", pose, re.S)
        assert m, member
        body = m.group(1)
        literal = re.findall(r'new PromoPose\("([^"]+)"', body)
        if literal:
            return literal
        return [re.search(rf"\b{field} = new PromoPose\(\"([^\"]+)\"", pose).group(1)
                for field in re.findall(r"[A-Za-z_]\w*", body)]

    assert list(wc.PROFILE_POSES) == names("All")
    assert set(wc.OPTIONAL_PROFILE_POSES) == {*re.findall(r'new PromoPose\("([^"]+)"', profile), *names("CloseUps")}
