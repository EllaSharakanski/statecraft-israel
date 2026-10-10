import check_repo
import oss_policy

# Fake secrets are assembled at run time so that this file never holds a secret-shaped literal.
FAKE_ELEVENLABS = "sk_" + "ab" * 24
FAKE_GITHUB = "ghp_" + "A1" * 18
ALLOW = 'allow:\n  - check: home-paths\n    glob: "tests/fixtures/*"\n    reason: "redaction fixtures"\n'


def run(root, *args):
    return check_repo.main(["--root", str(root), *args])


def test_fake_key_is_reported_by_type_and_never_by_value(make_repo, capsys):
    root = make_repo({"promo/x.py": f'KEY = "{FAKE_ELEVENLABS}"\nTOKEN = "{FAKE_GITHUB}"\n'})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "secrets: promo/x.py:1: elevenlabs-key" in out
    assert "secrets: promo/x.py:2: github-token" in out
    assert FAKE_ELEVENLABS not in out and FAKE_GITHUB not in out


def test_clean_tree_passes(make_repo, capsys):
    root = make_repo({"sim/a.cs": "class A {}\n"})
    assert run(root, "--only", "secrets,emails,home-paths,sizes,symlinks") == 0
    assert "OK" in capsys.readouterr().out


def test_unity_cloud_ids_empty_pass(make_repo, capsys):
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset": "  cloudProjectId: \n  organizationId: \n"})
    assert run(root, "--only", "secrets") == 0


def test_unity_cloud_ids_filled_fail(make_repo, capsys):
    guid = "1234abcd-" + "12ab-" * 3 + "1234567890ab"
    root = make_repo({"unity/ProjectSettings/ProjectSettings.asset":
                      f"  cloudProjectId: {guid}\n  organizationId: some-org\n"})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "unity-cloud-project-id" in out and "unity-organization-id" in out
    assert guid not in out and "some-org" not in out


def test_personal_email_fails_and_noreply_passes(make_repo, capsys):
    personal = "someone" + "@" + "gmail.com"
    root = make_repo({"a.py": f'UA = "x ({personal})"\n',
                      "b.md": "Co-Authored-By: Claude <noreply" + "@" + "anthropic.com>\n",
                      "c.md": "x <5146175+eyalgolan" + "@" + "users.noreply.github.com>\n",
                      "d.py": "    @pytest.mark.parametrize('a', [1])\n"})
    assert run(root, "--only", "emails") == 1
    out = capsys.readouterr().out
    assert "emails: a.py:1: personal-email" in out
    assert "b.md" not in out and "c.md" not in out and "d.py" not in out
    assert personal not in out


def test_package_ids_with_numeric_versions_are_not_emails(make_repo, capsys):
    # Review C1: ProjectSettings.asset and docs carry Unity package ids such as these.
    root = make_repo({"ProjectSettings.asset": "  templatePackageId: com.unity.template.urp-blank" + "@" + "17.0.14\n",
                      "m0.md": "com.unity.ai.assistant" + "@" + "2.20.0-pre.2 and pkg" + "@" + "17.0.14\n"})
    assert run(root, "--only", "emails") == 0


def test_home_path_fails_and_tilde_passes(make_repo, capsys):
    root = make_repo({"a.md": "see /Users/" + "someone/x\n", "b.md": "see ~/Library/Caches\n",
                      "c.md": "see /home/" + "someone/x\n"})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "a.md:1: home-path" in out and "c.md:1: home-path" in out and "b.md" not in out


def test_allowlisted_fixture_passes_and_the_same_line_elsewhere_fails(make_repo, capsys):
    line = 'var p = "/Users/' + 'someone/game";\n'
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "tests/fixtures/a.cs": line, "src/b.cs": line})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    assert "src/b.cs:1: home-path" in out and "tests/fixtures/a.cs" not in out


def test_stale_allow_row_is_reported(make_repo, capsys):
    root = make_repo({"scripts/oss/allow.yaml": ALLOW, "src/b.cs": "fine\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "stale-allow home-paths tests/fixtures/*" in capsys.readouterr().out


def test_allow_row_without_reason_is_reported(make_repo, capsys):
    rows = 'allow:\n  - check: home-paths\n    glob: "x/*"\n'
    root = make_repo({"scripts/oss/allow.yaml": rows, "x/a.cs": "/home/" + "someone/a\n"})
    assert run(root, "--only", "home-paths") == 1
    assert "allow-row-without-reason x/*" in capsys.readouterr().out


def test_untracked_files_are_not_scanned(make_repo, capsys):
    root = make_repo({"a.txt": "fine\n"}, untracked={"secret.env": f"K={FAKE_ELEVENLABS}\n"})
    assert run(root, "--only", "secrets") == 0


def test_binary_files_are_not_text_scanned(make_repo, capsys):
    root = make_repo({"img.png": b"\x89PNG\x00\x00" + FAKE_GITHUB.encode()})
    assert run(root, "--only", "secrets") == 0


def test_large_file_fails(make_repo, capsys):
    root = make_repo({"big.bin": b"\x00" * (oss_policy.MAX_FILE_BYTES + 1)})
    assert run(root, "--only", "sizes") == 1
    assert "sizes: big.bin: over-size-limit" in capsys.readouterr().out


def test_absolute_symlink_fails(make_repo, capsys):
    root = make_repo({"link": "->/etc/hosts", "ok": "->a.txt", "a.txt": "x\n"})
    assert run(root, "--only", "symlinks") == 1
    out = capsys.readouterr().out
    assert "symlinks: link: symlink-outside-repo" in out and "ok:" not in out


def test_required_files_are_named_when_missing(make_repo, capsys):
    root = make_repo({"README.md": "x\n"})
    assert run(root, "--only", "required") == 1
    out = capsys.readouterr().out
    assert "required: LICENSE: missing" in out and "required: README.md:" not in out


def test_release_mode_refuses_open_decisions(make_repo, capsys):
    root = make_repo({"docs/oss/DECISIONS.md": "- [x] D1 decided\n- [ ] D2 open\n"})
    assert run(root, "--only", "decisions") == 0
    assert run(root, "--only", "decisions", "--release") == 1
    assert "decisions: docs/oss/DECISIONS.md:2: open-decision" in capsys.readouterr().out


def test_unknown_check_name_is_an_error(make_repo, capsys):
    root = make_repo({"a.txt": "x\n"})
    assert run(root, "--only", "nonsense") == 2


# Review I4: scanner gaps.
def test_unquoted_env_assignment_is_a_secret(make_repo, capsys):
    value = "q" * 40
    root = make_repo({"setup.sh": "export OPENAI_" + f"API_KEY={value}\n", "ok.sh": "export OPENAI_API_KEY=$KEY\n"})
    assert run(root, "--only", "secrets") == 1
    out = capsys.readouterr().out
    assert "secrets: setup.sh:1: env-assignment" in out and "ok.sh" not in out and value not in out


def test_non_utf8_and_utf16_files_are_still_scanned(make_repo, capsys):
    aws = "AKIA" + "ABCDEFGHIJ" + "123456"
    mail = "someone" + "@" + "gmail.com"
    latin = f"caf\xe9 {aws}\n{mail}\n".encode("latin-1")
    utf16 = f"{aws}\n".encode("utf-16")
    root = make_repo({"latin.txt": latin, "wide.txt": utf16})
    assert run(root, "--only", "secrets,emails") == 1
    out = capsys.readouterr().out
    assert "secrets: latin.txt:1: aws-access-key-id" in out and "emails: latin.txt:2: personal-email" in out
    assert "secrets: wide.txt:1: aws-access-key-id" in out


def test_more_personal_path_shapes_are_home_paths(make_repo, capsys):
    root = make_repo({"a.md": "cd /Users/" + "someone\n", "b.md": "C:\\Users\\" + "someone\\x\n",
                      "c.md": "see -Users-" + "someone-israel-game\n", "d.md": "/Users/Shared and ~/x\n"})
    assert run(root, "--only", "home-paths") == 1
    out = capsys.readouterr().out
    for f in ("a.md", "b.md", "c.md"):
        assert f"home-paths: {f}:1: home-path" in out
    assert "d.md" not in out


def test_email_allowlist_is_exact_addresses_not_whole_domains(make_repo, capsys):
    root = make_repo({"a.md": "Claude <noreply" + "@" + "anthropic.com>\n", "b.md": "x" + "@" + "anthropic.com\n"})
    assert run(root, "--only", "emails") == 1
    out = capsys.readouterr().out
    assert "b.md:1: personal-email" in out and "a.md" not in out


def test_outside_a_checkout_is_a_readable_error_not_a_traceback(tmp_path, capsys):
    assert run(tmp_path) == 2
    assert "not a git checkout" in capsys.readouterr().err
