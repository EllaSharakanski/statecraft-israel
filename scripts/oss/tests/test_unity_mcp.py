"""Unity-MCP (com.ivanmurzak.unity.mcp) is the project's standard AI-agent bridge for the Unity editor. These tests pin
how it is installed, so a fresh clone resolves it with no manual steps (a missing OpenUPM scope gave contributors
`error CS0246 'PlayerPrefsBool'`)."""
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PKG, VERSION = "com.ivanmurzak.unity.mcp", "0.93.3"
OPENUPM = "https://package.openupm.com"


def manifest() -> dict:
    return json.loads((ROOT / "unity/Packages/manifest.json").read_text(encoding="utf-8"))


def lock() -> dict:
    return json.loads((ROOT / "unity/Packages/packages-lock.json").read_text(encoding="utf-8"))["dependencies"]


def test_manifest_pins_the_plugin_version():
    assert manifest()["dependencies"][PKG] == VERSION


def test_openupm_registry_scopes_cover_every_openupm_package_in_the_lock():
    regs = [r for r in manifest().get("scopedRegistries", []) if r["url"] == OPENUPM]
    assert len(regs) == 1
    scopes = regs[0]["scopes"]
    assert "com.ivanmurzak" in scopes and "extensions.unity" in scopes
    from_openupm = [name for name, d in lock().items() if d.get("url") == OPENUPM]
    assert PKG in from_openupm and "extensions.unity.playerprefsex" in from_openupm
    for name in from_openupm:
        assert any(name == s or name.startswith(s + ".") for s in scopes), name


def test_lock_resolves_the_pinned_plugin_from_openupm():
    entry = lock()[PKG]
    assert entry["version"] == VERSION and entry["source"] == "registry" and entry["url"] == OPENUPM


def test_project_settings_do_not_commit_the_plugin_gate_defines():
    # The plugin's resolver downloads its NuGet DLLs, then adds UNITY_MCP_READY and UNITY_MCP_DEPS_<n> on each
    # machine. Committed, the defines reach a fresh clone before the DLLs do, and the plugin fails to compile
    # (error CS0234 'System.Text.Json', ...), which stops batch mode. Leave that local change uncommitted: this reads
    # the committed file, so a contributor's own working copy (which has the defines) still passes.
    text = subprocess.run(["git", "-C", str(ROOT), "show", "HEAD:unity/ProjectSettings/ProjectSettings.asset"],
                          capture_output=True, check=True, text=True).stdout
    assert "UNITY_MCP_" not in text


def test_downloads_and_generated_agent_files_are_ignored():
    # The NuGet DLLs are third-party binaries fetched on first open; .mcp.json can carry the local server's bearer
    # token; the generated skills change with every plugin version. UserSettings/ (the plugin's per-user config and
    # local token) was already ignored.
    ignore = (ROOT / ".gitignore").read_text(encoding="utf-8").splitlines()
    for rule in ("unity/Assets/Plugins/NuGet/", "unity/Assets/Plugins/NuGet.meta", ".mcp.json", "unity/.claude/",
                 "unity/UserSettings/", "unity/Library/"):
        assert rule in ignore, rule


def test_notices_and_licensing_name_the_plugin_and_its_dependencies():
    notices = (ROOT / "THIRD-PARTY-NOTICES.md").read_text(encoding="utf-8")
    for fact in (PKG, "Apache-2.0", "extensions.unity.playerprefsex", "ReflectorNet", "GameDev-MCP-Server", "nuget.org",
                 "UnityMcpGuard"):
        assert fact in notices, fact
    row = next(l for l in (ROOT / "LICENSING.md").read_text(encoding="utf-8").splitlines() if "`unity/Packages/`" in l)
    assert "OpenUPM" in row and PKG in row and "Apache-2.0" in row
