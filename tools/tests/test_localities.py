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
         "kibbutz": "kibbutz", "moshav": "moshav", "mixed_locality": "town"}
SUBDISTRICTS = yaml.safe_load((MAP / "subdistrict_hexes_1948.yaml").read_text())
NO_TODAY = {line.strip() for line in (ROOT / "tools" / "tests" / "data" / "localities_no_today_qids.txt").read_text().splitlines()
            if line.strip() and not line.startswith("#")}
LANDMARKS = {"mosque", "church", "none", "water_tower", "mosque_and_water_tower"}


def sourced(row):
    assert row["status"] == "draft" and row["checked"]
    assert row["sources"] and all(s["url"].startswith("https://") and s["checked"] and s["licence"] in ("CC0", "CC BY-SA 4.0")
                                  for s in row["sources"])


def test_scope_is_the_1000_plus_places_beside_the_game_settlements():
    ids = [p["id"] for p in PLACES]
    assert len(ids) == len(set(ids)) == 271   # 293 less the 20 game settlements, the Jahalin tribe row (camps) and Huj
    assert not set(ids) & GAME_IDS, "a game settlement keeps its settlements_1948.yaml entry"
    assert all(p["population"] >= 1000 for p in PLACES)
    assert [p["population"] for p in PLACES] == sorted((p["population"] for p in PLACES), reverse=True)


@pytest.mark.parametrize("p", PLACES, ids=lambda p: p["id"])
def test_place_is_sourced_named_and_on_land(p):
    sourced(p)
    assert re.fullmatch(r"[a-z][a-z0-9_]*", p["id"])
    assert p["name"].strip() and p["name_basis"] in ("vs1945", "hebrew")
    if "today" in p:
        assert p["today"]["name"].strip() and p["today"]["source"].startswith("https://"), "a today name carries its source"
    assert p["type"] in TYPES and KINDS[p["kind"]] == p["type"] and p["landmark"] in LANDMARKS
    multi = any(" and " in x.rsplit(" (", 1)[0] for x in p["listed_as"])
    if p["match"] in ("low", "manual") or multi:
        assert p["owner_verify"], "low and manual matches, and VS rows that name more than one place, stay owner-verify"
    assert any(s["url"].startswith("https://www.wikidata.org/wiki/") for s in p["sources"])
    assert any("Village_Statistics,_1945" in s["url"] for s in p["sources"])
    b = HEXES["bounds"]
    assert b["lon0"] <= p["lon"] <= b["lon1"] and b["lat0"] <= p["lat"] <= b["lat1"]
    q, r = hg.settlement_hex(p["lon"], p["lat"], HEXES)
    assert BY_QR[(q, r)]["land"], f"{p['id']} on {BY_QR[(q, r)]['biome']} hex ({q},{r})"


def test_names_are_1948_names():
    by = {p["id"]: p for p in PLACES}
    assert by["beit_shean"]["name"] == "Beisan"
    assert (by["al_majdal"]["name"], by["al_majdal"]["today"]["name"]) == ("Majdal", "Ashkelon")
    assert by["beit_shean"]["today"]["name"] == "Beit She'an"
    assert by["al_abbasiyya"]["name"] == "'Abbasiyya"
    assert by["kiryat_ata"]["name"] == "Kfar Ata"
    for p in PLACES:   # the VS spellings' marks are gone: plain letters and an apostrophe for ayn
        assert not re.search(r"[‘’āīūḥṭ]|, El$", p["name"]), p["name"]
        assert "(" not in p["name"], p["name"]


def test_start_of_game_only_no_later_fate():
    text = yaml.safe_dump(DOC, allow_unicode=True).lower()   # the rows, not the header that states the rule
    for word in ("depopulat", "destroy", "expel", "fled", "abandon", "massacre", "evacuat", "occupied"):
        assert word not in text, word
    keys = {k for p in PLACES for k in p} | {k for c in CAMPS for k in c}
    assert not keys & {"fate", "end", "ended", "depopulated", "until", "status_after"}


def test_no_place_that_did_not_carry_on_has_a_today_name():
    """Owner ruling 2026-10-12: "Today:" only where a living place carries on. A place whose Wikidata item is classed a
    depopulated village or a former settlement (tools/tests/data/localities_no_today_qids.txt) has no today name; the
    one exception is the owner's own example, Majdal, whose item is today's city of Ashkelon."""
    exceptions = {"al_majdal"}
    offending = [p["id"] for p in PLACES if p["wikidata"] in NO_TODAY and "today" in p and p["id"] not in exceptions]
    assert not offending
    assert sum(1 for p in PLACES if p["wikidata"] in NO_TODAY) >= 100


def test_shafa_amr_deir_el_qasi_nesher_review_fixes():
    by = {p["id"]: p for p in PLACES}
    assert (by["shefa_amr"]["population"], by["shefa_amr"]["landmark"]) == (3640, "church")   # the Urban row only (review I1)
    assert by["dayr_al_qassi"]["landmark"] == "mosque" and by["dayr_al_qassi"]["owner_verify"]   # review I2
    assert (by["nesher"]["kind"], by["nesher"]["landmark"]) == ("mixed_locality", "mosque_and_water_tower")   # owner ruling 2026-10-12
    assert "huj" not in by and "dorot" not in by   # the village alone is under 1,000 (review M2)


def test_encampments_are_sourced_totals_of_real_subdistricts():
    """The camps' areas are the sub-districts as content/map/subdistrict_hexes_1948.yaml draws them, read at load (review
    I4: no copy of the hexes here). Jerusalem counts only its "Tribal Units" row (7,070, review M3)."""
    keys = {r["key"] for r in SUBDISTRICTS["regions"]}
    assert {c["subdistrict"] for c in CAMPS} == {"beersheba", "jerusalem", "hebron", "ramle", "jaffa", "safad", "gaza", "nablus"}
    assert {c["subdistrict"] for c in CAMPS} <= keys
    by = {c["subdistrict"]: c["population"] for c in CAMPS}
    assert by["jerusalem"] == 7070 and sum(by.values()) == 64670
    for c in CAMPS:
        sourced(c)
        assert c["population"] > 0 and c["listed_as"] and "hexes" not in c
        assert "name" not in c, "camps are never named"
