# AGENTS.md for Devolutions/gsudo-distro

## Purpose

This repository is a downstream distribution of gsudo. Keep downstream automation, packaging, and release logic here without importing upstream source into `master`.

## Repository model

- `master` is an orphan downstream branch (workflows, packaging, and distro metadata only).
- Upstream source of truth is mirrored in `upstream`.
- Product code changes belong on `release/vX.Y.Z` branches (based on upstream tags).

## Branch and source rules

- Do not add upstream gsudo source code to `master`.
- Keep `upstream` aligned with `gerardog/gsudo` `master`.
- Keep mirrored upstream tags namespaced as `upstream/vX.Y.Z`.
- Use `release/vX.Y.Z` for downstream code changes on top of upstream versions.

## Versioning

- Downstream version format is `X.Y.Z.R`.
- `X.Y.Z` tracks upstream.
- `R` increments only for downstream revisions.

## Workflows

- `build-package.yml` is the workflow for build, packaging, and release.
- Build target is `net9.0` only (do not reintroduce `net46`).
- Binary signing uses AzureSignTool and must be skipped when signing secrets are unavailable.
- Do not add NuGet package signing; sign binaries inside the `.nupkg` only.
- Zip packages are plain archives (not Authenticode-signed).
- `sync-upstream.yml` is intended to be manually triggered (`workflow_dispatch`) unless explicitly changed.

## Packaging

- Primary NuGet package ID is `Devolutions.gsudo`.
- Packaging assets live under `nuget/`.

## Change guardrails for agents

- Prefer minimal, targeted edits aligned with downstream distribution needs.
- Do not add unrelated tooling, frameworks, or workflow complexity.
- Preserve existing naming and branch conventions.
