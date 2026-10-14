"""Issue #437: the WebGL page names its build's hashed Build/ files, and a browser may show a cached copy of the page after a
newer build replaced them. The page therefore asks the server for its current copy and loads the files that copy names.
These check the template's text (no Unity, no browser); the cached-browser run itself is described in the PR."""
import re
from pathlib import Path

import package_release

TEMPLATE = Path(__file__).resolve().parents[2] / "unity/Assets/WebGLTemplates/Statecraft/index.html"
FILES = {"LOADER_FILENAME": "aaaa.loader.js", "DATA_FILENAME": "bbbb.data", "FRAMEWORK_FILENAME": "cccc.framework.js",
         "WORKER_FILENAME": "dddd.worker.js", "CODE_FILENAME": "eeee.wasm", "SYMBOLS_FILENAME": "ffff.symbols.json"}


def page() -> str:
    return TEMPLATE.read_text(encoding="utf-8")


def rendered() -> str:
    """The page as Unity writes it for a single-threaded wasm build: variables filled in, directives resolved."""
    out, keep = [], [True]
    for line in page().splitlines():
        if line.startswith("#if "):
            keep.append(keep[-1] and line.split()[1] in ("USE_WASM",))
        elif line.startswith("#endif"):
            keep.pop()
        elif keep[-1]:
            out.append(re.sub(r"\{\{\{ (\w+) \}\}\}", lambda m: FILES.get(m.group(1), m.group(0)), line))
    return "\n".join(out)


def test_the_loader_is_not_a_static_script_tag():
    """A static <script src="Build/..loader.js"> in a cached page would load the old loader before any check could run."""
    assert not re.search(r"<script[^>]*\ssrc=", page())


def test_the_page_fetches_its_own_current_copy_before_loading_the_build():
    text = page()
    assert re.search(r'fetch\(location\.href[^\n]*\{\s*cache:\s*"no-store"\s*\}\)', text)
    assert re.search(r"\.then\(start\);\s*</script>", text)          # the build starts only after that fetch settles
    start = re.search(r"function start\(\) \{.*?\n      \}\n", text, re.S).group(0)
    assert text.count("createUnityInstance(") == 1 and "createUnityInstance(" in start


def test_every_build_file_the_page_names_is_read_back_from_the_current_copy():
    text = page()
    named = set(re.findall(r'(\w+)\s*[:=]\s*"Build/\{\{\{', text))
    assert named == {"loaderUrl", "dataUrl", "frameworkUrl", "workerUrl", "codeUrl", "symbolsUrl"}
    reread = set(re.findall(r'"(\w+Url)"', re.search(r"FRESH_KEYS = \[([^\]]*)\]", text).group(1)))
    assert named == reread


def test_the_packager_still_finds_every_file_of_a_rendered_page(tmp_path):
    (tmp_path / "index.html").write_text(rendered())
    assert package_release.build_files(tmp_path) == sorted(["aaaa.loader.js", "bbbb.data", "cccc.framework.js", "eeee.wasm"])
