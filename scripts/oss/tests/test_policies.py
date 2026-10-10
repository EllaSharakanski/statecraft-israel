from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def test_decisions_record_d2_decided_and_d10_d11_open_for_the_owner():
    text = read("docs/oss/DECISIONS.md")
    assert "- [x] D2" in text and "inbound = outbound" in text
    assert "- [ ] D10" in text and "d42bd6b" in text and "- [ ] D11" in text and "Published before checked" in text
