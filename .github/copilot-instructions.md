# Copilot instructions for Devolutions/gsudo-test

## Repository intent

- This repo is a downstream distribution of gsudo.
- The `master` branch is an orphan branch with only downstream files (workflows, packaging).
- Upstream source lives in `upstream` and `release/vX.Y.Z` branches.

## Branching rules

- Do not add upstream gsudo source code to `master`.
- Use `release/vX.Y.Z` branches for code changes on top of upstream tags.
- Keep mirrored upstream tags under `upstream/vX.Y.Z`.
- `upstream` should mirror `gerardog/gsudo` master.

## Versioning

- Downstream releases use `X.Y.Z.R` (4-part) versions.
- `X.Y.Z` matches upstream; `R` increments for downstream revisions.

## Workflows

- Use `build-package.yml` for building, packaging, and releasing.
- Builds target net9.0 only; skip net46.
- Code signing uses AzureSignTool and should be skipped when secrets are missing.
- Do not add NuGet package signing; only sign binaries inside the nupkg.
- Zip packages are plain (not Authenticode-signed).

## Packaging

- NuGet package ID is `Devolutions.gsudo`.
- Packaging files live under `nuget/`.
