import pin_actions

SHA = "a" * 40


def test_tags_are_replaced_by_sha_with_the_tag_kept_as_a_comment():
    text = "steps:\n  - uses: actions/checkout@v4\n  - uses: astral-sh/setup-uv@v6\n    with:\n      version: x\n"
    out = pin_actions.pin_text(text, lambda repo, ref: SHA)
    assert f"  - uses: actions/checkout@{SHA} # v4\n" in out
    assert f"  - uses: astral-sh/setup-uv@{SHA} # v6\n" in out


def test_already_pinned_lines_are_left_alone():
    text = f"  - uses: actions/checkout@{SHA} # v4\n"
    assert pin_actions.pin_text(text, lambda repo, ref: (_ for _ in ()).throw(AssertionError)) == text


def test_a_comment_already_on_the_line_is_kept_with_the_ref():
    # #1463: pin_text rebuilt the line from the lead, action, SHA and ref, so a note already on the line was lost.
    # The ref stays where it has always been, right after the SHA, and the existing comment follows it unchanged.
    text = "  - uses: actions/checkout@v4 # keep: checkout needs credentials\n"
    out = pin_actions.pin_text(text, lambda repo, ref: SHA)
    assert out == f"  - uses: actions/checkout@{SHA} # v4 # keep: checkout needs credentials\n"


def test_trailing_whitespace_after_the_ref_does_not_become_a_comment():
    # The no-comment line must still come out exactly as before, with no trailing space.
    text = "  - uses: actions/checkout@v4   \n"
    out = pin_actions.pin_text(text, lambda repo, ref: SHA)
    assert out == f"  - uses: actions/checkout@{SHA} # v4\n"
