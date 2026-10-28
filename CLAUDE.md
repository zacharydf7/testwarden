# TestWarden

## Clean-room rule

The idea for this project came from a closed-source product at the author's employer. Nothing from that product or
that company ever enters this repo:

- No code, and no code or script logic ported or adapted from it.
- None of its names: products, projects, types, files, databases or environment variables.
- None of its test data, scripts, pipeline files, ticket keys or vendor license details.
- No mention of the product, the company or its other projects.

This covers `src/`, `tests/`, `spike/`, the README, this file, commit messages, issues and package metadata. Write
from public sources only: framework source and docs, and format specs. Spike and test fixtures are small sample apps
invented in this repo, with neutral names. Any check against the original product runs outside this repo, and its
results are never recorded here.

## What it is

A `dotnet` tool, plus an optional Microsoft Testing Platform (MTP) extension package, that runs MTP test projects in
CI and returns an exit code you can trust. Quarantined tests are skipped whatever the framework. A crash after a
clean run passes only with proof. A crash during the run always fails, on Linux and Windows.

The project is at the feasibility stage: `spike/` holds throwaway experiments, run by `.github/workflows/spike.yml`.
Product code waits until the feasibility gate is approved.

## Build and test

```
dotnet build
dotnet test -c Release
```

- Tests run on Microsoft Testing Platform (xUnit v3): filter with `--filter-class` and `--filter-method`, not VSTest's
  `--filter`.
- Warnings are errors.
- CI tests every push and pull request on Windows and Linux. Crash behavior is OS-specific, so both matter.
- Pushing a `v*` tag publishes the packages to nuget.org. Only the owner creates tags; sessions may push `main` but
  never tag.

## Tests

- Name tests `Method_Should<Expectation>_When<Condition>`. Separate arrange, act and assert with blank lines, not
  comments.

## Code style

- Write for an average developer: small focused classes, familiar idioms (object initializers, plain loops), and at
  most a one-line comment explaining why.
- Make every option discoverable: closed value sets are listed in `--help`, so users don't need to know MTP filter
  syntax.
- Use American English. Fill lines to about 120 characters instead of putting one parameter per line.
- Keep `README.md` self-contained, and update it when the command line or the exit codes change.
