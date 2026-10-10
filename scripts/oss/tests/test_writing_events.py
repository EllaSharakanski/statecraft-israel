"""docs/WRITING_EVENTS.md must stay true to the loader: every field its tables list is a property the C# types
accept, every property is listed, and every error message it quotes exists in the sim source."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
GUIDE = (ROOT / "docs/WRITING_EVENTS.md").read_text(encoding="utf-8")
SIM = ROOT / "sim/Statecraft.Sim"


def snake(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).lower()


def yaml_keys(cls: str) -> set[str]:
    """The keys the YAML loader accepts for a class: its public settable properties, minus [YamlIgnore] ones."""
    src = (SIM / "Content.cs").read_text(encoding="utf-8")
    body = re.search(rf"class {cls}\b.*?\n    }}", src, re.S) or re.search(rf"class {cls}\b[^\n]*\{{.*?\}}\s*(?:\n|$)", src, re.S)
    assert body, cls
    keys = set()
    for line in body.group(0).splitlines():
        m = re.search(r"public [\w<>?, ]+ (\w+) \{ get; set; \}", line)
        if m and "[YamlIgnore]" not in line:
            keys.add(snake(m.group(1)))
    return keys


def table_keys(heading: str) -> set[str]:
    """First-column `names` of the table that follows the heading, up to the next heading."""
    section = GUIDE.split(heading, 1)[1]
    section = re.split(r"\n#{2,3} ", section, maxsplit=1)[0]
    return set(re.findall(r"^\| `(\w+)` \|", section, re.M))


def test_event_field_table_matches_the_loader():
    assert table_keys("\n## Fields") == yaml_keys("GameEvent")


def test_option_field_table_matches_the_loader():
    assert table_keys("\n### options") == yaml_keys("EventOption")


def test_every_quoted_error_exists_in_the_sim_source():
    section = GUIDE.split("\n## What the loader rejects", 1)[1].split("\n## ", 1)[0]
    fragments = re.findall(r"^\| (.+?) \| .+ \|$", section, re.M)[1:]
    fragments = [f for f in fragments if not f.startswith("---")]
    assert len(fragments) > 15
    source = "\n".join(p.read_text(encoding="utf-8") for p in SIM.glob("*.cs"))
    for f in fragments:
        # `Property 'x' not found...` comes from YamlDotNet, not from this repo's source
        if f.startswith("`Property"):
            continue
        text = f.strip("`")
        assert text in source, f"guide quotes an error the code does not contain: {text!r}"


def test_status_values_match_the_loader():
    src = (SIM / "ContentLoader.cs").read_text(encoding="utf-8")
    values = re.search(r"Statuses = new HashSet<string> \{(.*?)\}", src, re.S).group(1)
    for v in re.findall(r'"([\w-]+)"', values):
        assert f"`{v}`" in GUIDE, v


def test_sensitive_tags_match_the_release_gate():
    src = (SIM / "EventRules.cs").read_text(encoding="utf-8")
    tags = re.search(r"SensitiveTags = .*?\{(.*?)\};", src, re.S).group(1)
    for t in re.findall(r'"([\w-]+)"', tags):
        assert f"`{t}`" in GUIDE, t
