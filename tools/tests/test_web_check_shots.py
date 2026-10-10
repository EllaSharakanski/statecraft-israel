import base64
import hashlib

import pytest

import web_check as wc


def shot_lines(name: str, png: bytes, chunk: int = 10) -> list[str]:
    b64 = base64.b64encode(png).decode()
    parts = [b64[i:i + chunk] for i in range(0, len(b64), chunk)]
    return [f"SHOT {name} begin {len(parts)} {hashlib.sha256(png).hexdigest()}"] + \
           [f"SHOT {name} {i + 1}/{len(parts)} {p}" for i, p in enumerate(parts)]


def test_a_shot_is_reassembled_and_written(tmp_path):
    png = bytes(range(256)) * 3
    c = wc.ShotCollector(tmp_path)
    assert all(c.feed(line) for line in shot_lines("gameplay03", png))
    assert (tmp_path / "gameplay03.png").read_bytes() == png
    assert c.written == [tmp_path / "gameplay03.png"]
    assert c.feed("SHOT done") is False          # printed and matched by --expect, not consumed
    assert c.feed("GAME OK 1948-05-15") is False


def test_parts_may_arrive_out_of_order(tmp_path):
    png = b"\x89PNG" + bytes(200)
    lines = shot_lines("diorama02", png)
    c = wc.ShotCollector(tmp_path)
    for line in [lines[0]] + list(reversed(lines[1:])):
        c.feed(line)
    assert (tmp_path / "diorama02.png").read_bytes() == png


def test_a_cut_line_fails_the_hash(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    lines[2] = lines[2][:-3]
    c = wc.ShotCollector(tmp_path)
    with pytest.raises(ValueError, match="SHA-256"):
        for line in lines:
            c.feed(line)


def test_a_part_before_its_header_is_an_error(tmp_path):
    with pytest.raises(ValueError, match="before its begin line"):
        wc.ShotCollector(tmp_path).feed("SHOT gameplay03 1/2 AAAA")


def test_a_lost_part_fails_loudly_at_shot_done(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    c = wc.ShotCollector(tmp_path)
    for line in lines[:2] + lines[3:]:           # one part line never arrives
        c.feed(line)
    with pytest.raises(ValueError, match=r"gameplay03: \d+ of \d+ parts"):
        c.feed("SHOT done")


def test_headless_chrome_runs_uncapped_at_dpr_1():
    flags = wc.chrome_flags("/tmp/p", swiftshader=False)
    for flag in ("--headless=new", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080",
                 "--user-data-dir=/tmp/p", "--force-device-scale-factor=1"):
        assert flag in flags
    assert "--use-angle=swiftshader" in wc.chrome_flags("/tmp/p", swiftshader=True)
