import commons_licences as cl


def test_public_domain_with_us_status_is_verified():
    r = cl.rule_for("p/a.jpg", "https://commons.wikimedia.org/wiki/File:A.jpg", "Public domain",
                    ["Template:PD-Israel", "Template:PD-US-expired"], "<a href='x'>GPO</a>")
    assert r == {"glob": "p/a.jpg", "licence": "Public-Domain", "source": "https://commons.wikimedia.org/wiki/File:A.jpg",
                 "status": "verified", "commons": "Public domain; PD-Israel, PD-US-expired", "artist": "GPO"}


def test_public_domain_without_us_status_needs_the_owner():
    r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:PD-Israel"], "")
    assert r["status"] == "owner-verify" and "no explicit US status" in r["note"]


def test_cc0_is_verified():
    r = cl.rule_for("p/a.jpg", "s", "CC0", ["Template:Cc-zero"], "")
    assert r["licence"] == "CC0-1.0" and r["status"] == "verified"


def test_share_alike_is_recorded_as_unknown_licence():
    r = cl.rule_for("p/a.jpg", "s", "CC BY-SA 4.0", ["Template:Cc-by-sa-4.0"], "")
    assert r["licence"] == "unknown" and r["status"] == "owner-verify"
    assert "not public domain or CC0" in r["note"]


def test_explicit_us_restoration_warning_cannot_be_verified_by_nested_pd_text():
    r = cl.rule_for("p/a.jpg", "s", "Public domain",
                    ["Template:PD-Israel", "Template:PD-US-expired", "Template:Not-PD-US-URAA",
                     "Template:PD-1996-text/country", "Template:PD-1996-text/date"], "")
    assert r["status"] == "owner-verify"
    assert "Not-PD-US-URAA" in r["note"]


def test_commons_keeps_the_us_restoration_warning_from_the_api():
    class Response:
        def json(self):
            return {"query": {"pages": {"1": {
                "imageinfo": [{"extmetadata": {"LicenseShortName": {"value": "Public domain"}}}],
                "templates": [{"title": name} for name in
                              ["Template:PD-Israel", "Template:Not-PD-US-URAA",
                               "Template:PD-1996-text/date"]]}}}}

    class Session:
        def get(self, *args, **kwargs):
            return Response()

    short, templates, artist = cl._commons("File:A.jpg", Session())
    assert "Template:Not-PD-US-URAA" in templates
    assert cl.rule_for("p/a.jpg", "s", short, templates, artist)["status"] == "owner-verify"


def test_pd_1996_parameter_subtemplates_are_not_us_status_on_their_own():
    r = cl.rule_for("p/a.jpg", "s", "Public domain",
                    ["Template:PD-Israel", "Template:PD-1996-text/country", "Template:PD-1996-text/date"], "")
    assert r["status"] == "owner-verify"


def test_actual_pd_1996_template_remains_verified():
    r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:PD-1996", "Template:PD-1996-text"], "")
    assert r["status"] == "verified"


def test_pd_1996_text_helper_alone_is_not_us_status():
    # {{PD-India-URAA}} (and PD-Poland, PD-HU-unknown and others) transclude {{PD-1996-text}} without {{PD-1996}}.
    r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:PD-India-URAA", "Template:PD-1996-text"], "")
    assert r["status"] == "owner-verify"


def test_us_text_helpers_are_not_status_tags():
    for helper in ("PD-US-expired-text", "PD-US-expired-text/en", "PD-US-no-notice-text"):
        r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:PD-Israel", "Template:" + helper], "")
        assert r["status"] == "owner-verify", helper


def test_weizmann_expired_composite_tag_is_independent_us_status():
    for tag in ("PD-old-70-expired", "PD-old-70-1923"):
        r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:" + tag, "Template:PD-US-expired-text"], "")
        assert r["status"] == "verified", tag
        assert cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:" + tag], "")["status"] == "verified"


def test_plain_old_tag_and_composite_helpers_are_not_us_status():
    for tag in ("PD-old-70", "PD-old-text", "PD-old-70-expired/en", "PD-old-X-expired"):
        r = cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:" + tag], "")
        assert r["status"] == "owner-verify", tag


def test_primary_us_tags_still_verify():
    for tag in ("PD-US-expired", "PD-USGov", "PD-USGov-NARA", "PD-US-no notice"):
        assert cl.rule_for("p/a.jpg", "s", "Public domain", ["Template:" + tag], "")["status"] == "verified"
