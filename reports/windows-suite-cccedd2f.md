# Windows test-suite run on main `cccedd2f`

Run on 22 October 2026 on a Windows 11 Home machine (10.0.26200, build 26200; system locale he-IL, ANSI code page 1255),
on `origin/main` at `cccedd2fb892d46fa05b91029a088cdd22248051` (the v0.2.0 release merge, #1337). It was a clean detached
worktree (`git status` empty before and after). Tooling: .NET SDK 10.0.402, uv 0.12.1, Python 3.12 (`UV_PYTHON=3.12`, as
check.sh sets it), Git Bash 4.4.23, and WSL (Ubuntu) installed. Nothing was fixed or changed; this is a report only.
Unity steps were skipped.

A first pass on `ece85d71` (the head before the release merge) gave the same sim and policy results. Its tools run hung
(failure 1 below) and was stopped, so all tools numbers here come from `cccedd2f`.

## Totals against the Mac reference

| Part | Command (each run separately) | Windows `cccedd2f` | Mac reference (main, today) |
|---|---|---|---|
| Sim | `dotnet test sim/Statecraft.sln -c Release` | **2365 passed, 0 failed** | 2365 passed |
| Policy, `PYTHONUTF8=1` (what check.sh sets since #1326) | `uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests` | **316 passed, 4 failed** | 320 passed |
| Policy, no `PYTHONUTF8` (a direct run) | same | **312 passed, 8 failed** | n/a |
| Tools, exactly as check.sh runs it, `PYTHONUTF8=1` | check.sh's `uv run ... pytest tools/tests -q` | **broken: "Interrupted: 2 errors during collection", 0 tests run** | 1507 passed, 5 skipped |
| Tools, no `PYTHONUTF8` (collection only) | same, `--co` | **broken: 9 collection errors** (1007 collected before it stopped) | n/a |
| Tools, `PYTHONUTF8=1`, the 2 fcntl modules ignored and the 3 hanging tests deselected | plus `--ignore=tools/tests/test_machine_lock.py --ignore=tools/tests/test_unity_postmerge.py`, `--deselect` x3, `pytest-timeout==2.4.0 --timeout=1200` as a backstop | **1417 passed, 14 failed, 23 skipped, 3 deselected** (810 s) | n/a |

Counts reconcile: 1417 + 14 + 23 + 3 = 1457 tests outside the two fcntl modules, and the Mac's 1507 + 5 = 1512, so those
two modules hold 55 tests.

**"0 collected" does happen:** the tools step as check.sh runs it collects 0 runnable tests on Windows and stops (failure
2), so check.sh's tools step cannot pass on Windows today, with or without `PYTHONUTF8`.

## Windows-only failures (with `PYTHONUTF8=1`, the check.sh setting)

### 1. Three tools tests hang forever: `localhost` resolves to IPv6 first, and the server listens on IPv4 only

| Test | Line | First error (isolated run, `--timeout=60`) |
|---|---|---|
| `tools/tests/test_web_check.py::test_precompressed_files_get_content_encoding_and_wasm_its_type` | :59-60 (`base = f"http://localhost:{...}"`; `urllib.request.urlopen(base + "/Build/game.wasm.br")`) | `Timeout` in `socket.py` `create_connection` → `sock.connect(sa)` |
| `tools/tests/test_web_check.py::test_compressed_scripts_and_data_keep_their_types_and_traversal_is_404` | :138-139 (`urlopen(base + "/a.framework.js.gz")`); also `http.client.HTTPConnection("localhost", ...)` at :141 | `Timeout` ... `sock.connect(sa)` |
| `tools/tests/test_webgl_favicon.py::test_favicon_is_served_at_template_relative_url` | :38-39 (`urlopen(base + "/favicon.ico")`) | `Timeout` ... `sock.connect(sa)` |

- **Repro:** `uv run --with pytest==9.1.1 --with pytest-timeout==2.4.0 ... pytest "<test id>" --timeout=60
  --timeout-method=thread`. Each times out at 60 s in `sock.connect`.
- **Diagnosis (Windows, networking):** `tools/web_check.py:355` binds `("127.0.0.1", port)`, IPv4 only. On this machine
  `socket.getaddrinfo("localhost", ...)` returns `('::1', ...)` before `('127.0.0.1', ...)`. That is stock Windows 11:
  `Get-NetPrefixPolicy` ranks `::1/128` at precedence 50, above `::ffff:0:0/96` at 35; IPv6 is enabled
  (`DisabledComponents` 0); and the hosts file's localhost lines are commented out, as shipped. During the hang the
  process uses 0 CPU and holds `::1:<port> -> ::1:<server port>` in **SynSent** against a listener on
  `127.0.0.1:<server port>` (`Get-NetTCPConnection`). The calls pass no timeout, so they wait indefinitely; one sat for
  more than 10 minutes. Why the SYN is not refused at once is observed, not explained here.
- **Not affected:** the other `localhost` strings in `tools/tests` are log-line fixtures (`test_web_check.py:45, :111`,
  `test_web_check_shots.py:143`), a `Host:` header on a socket that connects to `127.0.0.1`
  (`test_web_check.py:86-87`), or a mocked `send` (`test_headed.py:80, :92`).
- **Effect:** the whole tools step stalls with no error. The first full run sat at 78% until it was stopped.

### 2. Two tools test modules cannot be collected: unguarded `import fcntl`

| Module | First error |
|---|---|
| `tools/tests/test_machine_lock.py:1` | `ModuleNotFoundError: No module named 'fcntl'` |
| `tools/tests/test_unity_postmerge.py:1` | `ModuleNotFoundError: No module named 'fcntl'` |

- **Diagnosis (Windows, POSIX-only module):** `fcntl` does not exist on Windows. Every import of it is at module top level
  with no guard: the two test modules above, and the tools they test, `tools/machine_lock.py:29`,
  `tools/unity_postmerge.py:46` and `tools/unity_postmerge_launch.py:13`. These are Mac tooling by design
  (`machine_lock.py`'s docstring opens "One Mac, several worktrees").
- **Effect:** pytest stops the session at collection ("Interrupted: 2 errors during collection"), so no tools test runs.
  `scripts/check.sh:59` runs `pytest tools/tests` without excluding them, so check.sh's tools step cannot pass on Windows
  today.

### 3. Bare `bash` from Python runs WSL's bash, not Git Bash (8 tests)

When a test calls `subprocess.run(["bash", ...])`, Windows' `CreateProcess` searches `C:\Windows\System32` before `PATH`
and finds `bash.exe`, the WSL launcher. `shutil.which("bash")` (a PATH search) returns Git Bash, which is why the
difference doesn't show from Python itself. Proven on this machine: the tag-regex call below returns rc 0 through
`C:\Program Files\Git\usr\bin\bash.exe` and rc 1 through `C:\Windows\System32\bash.exe` and through `"bash"`.

| Test | First error | How WSL breaks it |
|---|---|---|
| `scripts/oss/tests/test_merge_policy.py::test_apply_script_refuses_without_a_terminal_and_calls_nothing` | `assert (127 == 2)`; stderr `/bin/bash: C:scwt-suitescriptsossapply_github_settings.sh: No such file or directory` (:96) | WSL bash gets a Windows path and drops its backslashes |
| `scripts/oss/tests/test_release.py::test_the_gate_zip_carries_every_notice` | `/bin/bash: C:scwt-suitescriptsosspackage_web.sh: No such file or directory`, `assert 127 == 0` (:40) | same |
| `scripts/oss/tests/test_release.py::test_a_refusing_gate_stops_the_package` | `assert (127 != 0 and 'PACKAGE REFUSED' in '')` (:50) | same |
| `scripts/oss/tests/test_release.py::test_a_stale_zip_does_not_survive_a_refusal` | `assert (127 != 0 and not True)` (:71) | same |
| `tools/tests/test_art_compare.py::test_the_checkpoint_completes_then_lists_each_guard_fail_and_exits_3` | `tools/art_checkpoint.sh: line 26: cd: --: invalid option` (:283) | Under WSL, git is 2.25.1 (`wsl -e git --version`); `git rev-parse --path-format=absolute` needs git 2.31+, so line 26's `cd "$(...)"` gets an option-like argument |
| `tools/tests/test_art_compare.py::test_the_checkpoint_exits_3_even_without_a_guard_fail_line` | same (:291) | same |
| `tools/tests/test_art_compare.py::test_the_checkpoint_passes_and_stops_on_genuine_errors_as_before` | `assert (2 == 0)`, same stderr (:296) | same |
| `tools/tests/test_player_golden_check.py::test_the_workflows_shell_tag_check_accepts_exactly_what_the_python_one_does` | `AssertionError: v0.2.0` / `assert (1 == 0)`; WSL stderr `syntax error near unexpected token '('` (:127) | the WSL launcher re-quotes the arguments into `/bin/bash -c "..."`, which breaks the regex's parentheses |

On a machine without WSL, a bare `bash` would not be found at all, or would be whatever PATH gives, so these tests would
fail differently or pass.

### 4. Path separators: tests compare Windows paths with forward-slash strings (6 tests)

| Test | Line | First error |
|---|---|---|
| `tools/tests/test_release_build.py::test_every_unity_build_runs_under_the_shared_lock_and_waits_for_unity` | :387 | `assert (['uv', 'run'] == ['uv', 'run'] ... and False)`: `head[2].endswith("tools/machine_lock.py")` on a backslash path |
| `tools/tests/test_release_build.py::test_the_sim_dlls_are_built_before_unity` | :428 | `StopIteration`: no call where `c[-1].endswith("tools/build_sim_for_unity.py")` |
| `tools/tests/test_release_build.py::test_the_golden_runs_on_the_mac_player_headless_under_the_lock` | :529 | `'C:\...\repo\tools\machine_lock.py'.endswith('tools/machine_lock.py')` is False |
| `tools/tests/test_release_build.py::test_a_missing_notice_file_is_refused_before_any_build[LICENSES/CC-BY-4.0.txt]` | :758 | `assert 'LICENSES/CC-BY-4.0.txt' in 'REFUSED: ... missing from the repository: LICENSES\\CC-BY-4.0.txt ...'` |
| `...[unity/Assets/UI/Fonts/OFL.txt]` | :758 | same, `unity\\Assets\\UI\\Fonts\\OFL.txt` |
| `...[unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt]` | :758 | same |

- **Diagnosis (Windows, paths):** `tools/release_build.py` formats `pathlib` paths with the native separator, and the
  tests expect POSIX strings. Release builds run on the owner's Mac (decision O7), so this affects the tests, not a real
  Windows build.

### 5. No POSIX executable bit on NTFS (2 tests)

| Test | Line | First error |
|---|---|---|
| `tools/tests/test_player_golden_check.py::test_unpack_keeps_the_executable_bit_and_refuses_paths_that_leave_the_folder` | :281 | `assert (33206 & 73)`: `st_mode` 0o100666, no execute bits |
| `tools/tests/test_release_build.py::test_the_linux_archive_keeps_the_executable_bit` | :602 | `assert (438 & 73)`: the tar member's mode is 0o666 |

- **Diagnosis (Windows, permissions):** NTFS has no POSIX mode bits, so `chmod +x` is not kept and `stat` reports 0o666;
  the archive is built from that file's mode.

### 6. CRLF (1 test)

| Test | Line | First error |
|---|---|---|
| `tools/tests/test_release_build.py::test_every_archive_carries_the_notices_and_matches_its_file_list` | :716 | `assert 'LICENSE text\r\n' == 'LICENSE text\n'` |

- **Diagnosis (Windows, line endings):** the `repo` fixture writes the notice files with `write_text` (test_release_build.py
  :33-34), which writes `\r\n` on Windows. The archive keeps those bytes, while the comparison side reads the repo file with
  `read_text()`, which turns `\r\n` back into `\n`, so the two sides differ only on Windows.

### 7. File lock (1 test, seen once)

| Test | Line | First error |
|---|---|---|
| `tools/tests/test_population_hash_cleanup.py::test_population_hash_check_cleans_its_copy_on_success_and_failure[None]` | :22 | `PermissionError: [WinError 5] Access is denied: '...\population-cleanup-...\temporary\VBCSCompiler\AnalyzerAssembly...'` |

- **Diagnosis (Windows, file locking):** the cleanup cannot delete a file under `VBCSCompiler`, the .NET compiler server,
  which keeps running after a build and holds its files open; Windows refuses to delete an open file. The other three
  parameter cases (`load`, `assertion`, `copy`) passed in the same run. Seen in one run only; whether it is
  intermittent was not tested.

## Additional failures without `PYTHONUTF8` (a direct `pytest` run, not check.sh)

check.sh sets `PYTHONUTF8=1` (#1326), so these do not occur under check.sh. They do occur in a direct run on a non-UTF-8
system locale (he-IL, cp1255).

- **Policy, +4 (Windows, encoding):** each test reads or writes a UTF-8 file with `Path.read_text()` / `write_text()` and
  no encoding:
  - `test_event_timeline.py::test_defaults_match_the_sim` (:41): `UnicodeDecodeError: 'charmap' codec can't decode byte 0x9e`
  - `test_pr_content_summary.py::test_lists_are_the_sims_dated_model_records` (:58): same
  - `test_release.py::test_versions_agree` (:62): `'charmap' codec can't decode byte 0x90`
  - `test_gdd_lint.py::test_dates_ordinals_and_scales_as_the_specs_write_them`: the helper at :46 writes `§` in cp1255,
    which `gdd_lint.py` then reads as UTF-8 (`'utf-8' codec can't decode byte 0xa7`).
- **Tools, 7 modules fail to collect (Windows, encoding):** `test_borders.py`, `test_headed_column.py`, `test_hexgrid.py`
  (via `tools/hexgrid.py:171`), `test_localities.py`, `test_locations.py`, `test_settlement_hexes.py` and
  `test_settlements.py` read UTF-8 data with a bare `read_text()` at import: `'charmap' codec can't decode byte ...`.
  Add the 2 fcntl modules and that's 9 collection errors.

## Skips: 23 on Windows against 5 on the Mac

From pytest's `-rs` reasons (which of these also skip on the Mac was not checked):
- `test_player_golden_check.py`, 13: "the fake player is a shell script", "the fake player and xvfb-run are shell scripts",
  "process groups are POSIX" (2), "exec format errors are POSIX", "the macOS zip is unpacked with ditto", "needs ditto,
  lipo and uname".
- `test_encode_event_videos.py`, 5: "ffmpeg not installed" (4); "decodes the clips with ffmpeg, which the CI runner lacks".
- `test_headed.py`, 2: "needs unity/Builds/Web-disabled".
- 3 optional imports that check.sh's `--with` list does not include: `pypdf` (`test_armistice.py:409`), `skimage` and
  `scipy` (`test_routes_1948.py:200, :222`).

## Notes

- **Slowest test:** `tools/tests/test_land_bake.py::test_the_committed_texture_is_the_bake`, 268 s in setup (pytest
  `--durations`); the next slowest is 51 s. It explains the long pause around 24% of the tools run; it is slow, not stuck
  (it used CPU throughout).
- **pytest-timeout:** its thread method ends the whole pytest process when a timeout fires, so the hanging tests were
  deselected for the full run and reproduced one by one instead.
- Logs: sim `sim.log`, policy `policy_utf8.log` / `policy_noutf8.log`, tools `tools_utf8_full.log`, collection
  `tools_collect_noutf8.log` and `tools_utf8_asis.log`, and one log per isolated reproduction, all kept on the Windows
  machine.
