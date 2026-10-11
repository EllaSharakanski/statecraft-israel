"""Checks content/map/localities_1948.yaml (the 1948 localities and Bedouin camp areas the map's kits and labels draw;
presentation only) against hexes.json and settlements_1948.yaml."""
import json
import re
from pathlib import Path

import pytest
import yaml

import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}
DOC = yaml.safe_load((MAP / "localities_1948.yaml").read_text())
PLACES = DOC["places"]
CAMPS = DOC["encampments"]
GAME_IDS = {s["id"] for s in yaml.safe_load((MAP / "settlements_1948.yaml").read_text())["settlements"]}
TYPES = {"town", "kibbutz", "moshav", "arab_town", "village"}
KINDS = {"arab_town": "arab_town", "arab_village": "village", "jewish_town": "town", "moshava": "town",
         "kibbutz": "kibbutz", "moshav": "moshav"}
LANDMARKS = {"mosque", "church", "none", "water_tower"}


def sourced(row):
    assert row["status"] == "draft" and row["checked"]
    assert row["sources"] and all(s["url"].startswith("https://") and s["checked"] and s["licence"] in ("CC0", "CC BY-SA 4.0")
                                  for s in row["sources"])


def test_scope_is_the_1000_plus_places_beside_the_game_settlements():
    ids = [p["id"] for p in PLACES]
    assert len(ids) == len(set(ids)) == 272
    assert not set(ids) & GAME_IDS, "a game settlement keeps its settlements_1948.yaml entry"
    assert all(p["population"] >= 1000 for p in PLACES)
    assert [p["population"] for p in PLACES] == sorted((p["population"] for p in PLACES), reverse=True)


@pytest.mark.parametrize("p", PLACES, ids=lambda p: p["id"])
def test_place_is_sourced_named_and_on_land(p):
    sourced(p)
    assert re.fullmatch(r"[a-z][a-z0-9_]*", p["id"])
    assert p["name"].strip() and p["name_today"].strip() and p["name_basis"] in ("vs1945", "hebrew")
    assert p["type"] in TYPES and KINDS[p["kind"]] == p["type"] and p["landmark"] in LANDMARKS
    assert p["owner_verify"] == (p["match"] in ("low", "manual")), "low and manual matches stay owner-verify"
    assert any(s["url"].startswith("https://www.wikidata.org/wiki/") for s in p["sources"])
    assert any("Village_Statistics,_1945" in s["url"] for s in p["sources"])
    b = HEXES["bounds"]
    assert b["lon0"] <= p["lon"] <= b["lon1"] and b["lat0"] <= p["lat"] <= b["lat1"]
    q, r = hg.settlement_hex(p["lon"], p["lat"], HEXES)
    assert BY_QR[(q, r)]["land"], f"{p['id']} on {BY_QR[(q, r)]['biome']} hex ({q},{r})"


def test_names_are_1948_names():
    by = {p["id"]: p for p in PLACES}
    assert (by["beit_shean"]["name"], by["beit_shean"]["name_today"]) == ("Beisan", "Beit She'an")
    assert (by["al_majdal"]["name"], by["al_majdal"]["name_today"]) == ("Majdal", "Ashkelon")
    assert by["kiryat_ata"]["name"] == "Kfar Ata"
    for p in PLACES:   # the VS spellings' marks are gone: plain letters and an apostrophe for ayn
        assert not re.search(r"[‘’āīūḥṭ]|, El$", p["name"]), p["name"]
        assert "(" not in p["name"] or p["id"] == "al_abbasiyya", p["name"]   # VS gives both of its names


def test_start_of_game_only_no_later_fate():
    text = yaml.safe_dump(DOC, allow_unicode=True).lower()   # the rows, not the header that states the rule
    for word in ("depopulat", "destroy", "expel", "fled", "abandon", "massacre", "evacuat", "occupied"):
        assert word not in text, word
    keys = {k for p in PLACES for k in p} | {k for c in CAMPS for k in c}
    assert not keys & {"fate", "end", "ended", "depopulated", "until", "status_after"}


def test_encampments_are_sourced_totals_on_land_hexes():
    assert {c["subdistrict"] for c in CAMPS} == {"beersheba", "jerusalem", "hebron", "ramle", "jaffa", "safad", "gaza", "nablus"}
    assert sum(c["population"] for c in CAMPS) == 65770
    for c in CAMPS:
        sourced(c)
        assert c["population"] > 0 and c["listed_as"] and c["hexes"]
        for q, r in c["hexes"]:
            assert BY_QR[(q, r)]["land"] and BY_QR[(q, r)]["biome"] != "lake", (c["subdistrict"], q, r)
        assert "name" not in c, "camps are never named"
    seen = [tuple(h) for c in CAMPS for h in c["hexes"]]
    assert len(seen) == len(set(seen)), "a hex belongs to one sub-district"
