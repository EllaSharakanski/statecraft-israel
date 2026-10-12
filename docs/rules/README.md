# Rule IDs

Every rule in the game design gets a stable ID, like `R-ECON-014`: `R-`, a 2 to 6 letter area, a three digit number.
Rewording a rule keeps its ID. A removed rule's ID is not reused.

**Where an ID is written.** A rule is defined in a Markdown file in this folder (`docs/rules/*.md`, not this README), on a line that
starts with the ID:

```
R-ECON-014: Each turn the treasury gains tax per head times the population share of the year.
- **R-ECON-015** Spending per head is paid the same way.
```

**How an ID is cited.** Write the ID in a comment next to the thing that builds or checks the rule: a C# test
(`// R-ECON-014`), a YAML comment in `content/`, or the PR body ("implements R-ECON-014").

**What the check does.** `python3 scripts/oss/rule_ids.py` fails when:

- a rule defined here has no citing test in `sim/Statecraft.Sim.Tests` (untested),
- an ID cited in `sim/` or `content/` is defined nowhere (dead), or
- an ID is defined twice.

It runs in CI and in `scripts/check.sh`. Prose that only mentions an ID mid-line is not a definition.
