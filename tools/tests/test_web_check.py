"""tools/web_check.py: the console-line parser and the static server's headers (no browser needed)."""
import gzip
import http.client
import re
import urllib.request

import web_check as wc


def test_console_lines_are_read_from_their_first_line():
    one = '[1010/025003.375619:INFO:CONSOLE:2] "GOLDEN OK 1950 abc", source: http://localhost:1/index.html (2)'
    assert wc.CONSOLE.search(one).group(1) == "GOLDEN OK 1950 abc"
    # Unity's Debug.Log adds a stack trace, so the message continues on the next stderr lines.
    first = '[1010/030000.1:INFO:CONSOLE:9] "SMOKE OK 5 turns 1949-01-01'
    assert wc.CONSOLE.search(first).group(1) == "SMOKE OK 5 turns 1949-01-01"


def test_precompressed_files_get_content_encoding_and_wasm_its_type(tmp_path):
    (tmp_path / "Build").mkdir()
    (tmp_path / "Build" / "game.wasm.br").write_bytes(b"\x00")
    (tmp_path / "Build" / "game.data.gz").write_bytes(gzip.compress(b"x"))
    (tmp_path / "index.html").write_text("<html></html>")
    server = wc.serve(tmp_path)
    try:
        base = f"http://localhost:{server.server_address[1]}"
        r = urllib.request.urlopen(base + "/Build/game.wasm.br")
        assert (r.headers["Content-Encoding"], r.headers["Content-Type"]) == ("br", "application/wasm")
        r = urllib.request.Request(base + "/Build/game.data.gz", headers={"Accept-Encoding": "identity"})
        assert urllib.request.urlopen(r).headers["Content-Encoding"] == "gzip"
        assert urllib.request.urlopen(base + "/index.html").headers["Content-Encoding"] is None
    finally:
        server.shutdown()


FAIL = re.compile(r"GOLDEN FAIL|BOOT FAIL")


def line(text):
    return f'[1010/025003.375619:INFO:CONSOLE:2] "{text}", source: http://localhost:1/index.html (2)'


def test_scan_passes_when_every_expected_text_was_seen():
    status, pending, texts = wc.scan([line("GOLDEN OK 1950 a"), line("GOLDEN OK 1952 b")], ["GOLDEN OK 1950", "GOLDEN OK 1952"], FAIL)
    assert (status, pending) == ("pass", []) and texts == ["GOLDEN OK 1950 a", "GOLDEN OK 1952 b"]


def test_scan_fails_on_a_failure_line_and_waits_for_missing_texts():
    status, pending, texts = wc.scan([line("GOLDEN FAIL 1950 x expected y")], ["GOLDEN OK 1950"], FAIL)
    assert status == "fail" and texts == ["GOLDEN FAIL 1950 x expected y"]
    status, pending, _ = wc.scan([line("GOLDEN OK 1950 a")], ["GOLDEN OK 1950", "GOLDEN OK 1952"], FAIL)
    assert (status, pending) == ("pending", ["GOLDEN OK 1952"])


def test_scan_ignores_lines_that_are_not_console_messages():
    status, pending, texts = wc.scan(["[0101/000000:ERROR:gpu] something", "  at Foo.Bar()"], ["X"], FAIL)
    assert (status, pending, texts) == ("pending", ["X"], [])


def test_compressed_scripts_and_data_keep_their_types_and_traversal_is_404(tmp_path):
    (tmp_path / "site").mkdir()
    (tmp_path / "site" / "a.framework.js.gz").write_bytes(gzip.compress(b"x"))
    (tmp_path / "site" / "a.data.br").write_bytes(b"\x00")
    (tmp_path / "secret.txt").write_text("secret")
    server = wc.serve(tmp_path / "site")
    try:
        base = f"http://localhost:{server.server_address[1]}"
        assert urllib.request.urlopen(base + "/a.framework.js.gz").headers["Content-Type"] == "application/javascript"
        assert urllib.request.urlopen(base + "/a.data.br").headers["Content-Type"] == "application/octet-stream"
        conn = http.client.HTTPConnection("localhost", server.server_address[1])
        for path in ("/../secret.txt", "/%2e%2e/secret.txt"):
            conn.request("GET", path)
            r = conn.getresponse(); r.read()
            assert r.status == 404, path
    finally:
        server.shutdown()
        server.server_close()
