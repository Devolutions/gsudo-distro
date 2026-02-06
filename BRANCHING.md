# Branching and Tagging Strategy

## Goals

- Keep upstream history and tags clearly separated from downstream work.
- Track downstream releases with an explicit downstream revision number.

## Branches

- `upstream`
  - Mirror of `gerardog/gsudo` upstream `master`.
- `master`
  - Downstream distribution-only branch (workflows, packaging, release plumbing).
  - Do not add upstream source here.
- `release/vX.Y.Z`
  - Downstream patch branch based on upstream release `vX.Y.Z`.
  - Example: `release/v2.6.1` is created from tag `upstream/v2.6.1`.

## Tags

- Upstream mirrored tags:
  - `upstream/vX.Y.Z`
  - Example: `upstream/v2.6.1`
- Downstream release tags:
  - `vX.Y.Z.B` (4-part version)
  - `X.Y.Z` = upstream version, `B` = downstream build/revision increment.
  - Example sequence on branch `release/v2.6.1`: `v2.6.1.1`, `v2.6.1.2`, ...

## Release Flow

1. Sync upstream branch and upstream tags under `upstream/*`.
2. Create/update patch branch `release/vX.Y.Z` from `upstream/vX.Y.Z`.
3. Apply downstream patches on `release/vX.Y.Z`.
4. Build/package from `release/vX.Y.Z`.
5. Tag downstream release as `vX.Y.Z.B`.

## Notes

- Prefer annotated tags for downstream releases.
- Keep upstream and downstream tags distinct by namespace and version shape.
- Do not reuse plain `vX.Y.Z` names for branches to avoid branch/tag ambiguity.
