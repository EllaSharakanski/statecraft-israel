"""Text files check out with LF on every platform (#405). Git for Windows defaults to core.autocrlf=true, which checked
PORTRAITS.txt and PortraitCatalog.asset out with CRLF. PortraitImport and Unity then wrote them back with LF, so
PortraitImportTests.RunningTheImportAgainChangesNothing failed and the files showed as modified. .gitattributes pins LF;
these tests read it through git, so they fail wherever the rule is missing, not only on Windows."""
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PORTRAITS = "unity/Assets/UI/Portraits"


def git(*args: str) -> str:
    return subprocess.run(["git", "-C", str(ROOT), *args], capture_output=True, check=True, text=True).stdout


def test_text_files_check_out_with_lf_whatever_core_autocrlf_says():
    files = [f"{PORTRAITS}/PORTRAITS.txt", f"{PORTRAITS}/PortraitCatalog.asset", f"{PORTRAITS}/ben_gurion.jpg.meta",
             "unity/Assets/Materials/Terrain.mat", "content/people.yaml", "sim/Statecraft.sln"]
    out = git("check-attr", "eol", "--", *files)
    assert out.splitlines() == [f"{f}: eol: lf" for f in files]


def test_no_tracked_file_is_stored_with_crlf():
    # With `text=auto` git would convert a CRLF blob on the next commit that touches it; none should be waiting.
    crlf = [line.split("\t", 1)[1] for line in git("ls-files", "--eol").splitlines() if line.startswith("i/crlf")]
    assert crlf == []


def test_images_stay_binary():
    eol = git("ls-files", "--eol", "--", f"{PORTRAITS}/ben_gurion.jpg").split()
    assert eol[0] == "i/-text"
