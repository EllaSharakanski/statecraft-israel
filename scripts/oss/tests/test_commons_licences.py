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
