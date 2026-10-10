"""tools/web_check.py: the console-line parser and the static server's headers (no browser needed)."""
import gzip
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
