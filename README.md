# Devolutions gsudo

A downstream distribution of [gsudo](https://github.com/gerardog/gsudo) — a sudo for Windows — with additional patches for the UniElevate build variant and enhanced security verification.

## Repository Structure

This repository uses an **orphan master branch** strategy to cleanly separate downstream maintenance files from the upstream gsudo source code.

### Branches

| Branch | Purpose |
|--------|---------|
| `master` | Default branch. Contains only downstream files: README, GitHub Actions workflows, and NuGet packaging. **No upstream gsudo source code.** Has its own independent git history. |
| `upstream` | Tracks the upstream `gerardog/gsudo` master branch. Updated periodically via `git fetch`. |
| `release/vX.Y.Z` | Per-release patch branches. Contains the full gsudo source tree at upstream version `vX.Y.Z` with downstream patches applied on top. Example: `release/v2.6.1`. |

Upstream release tags are mirrored with a namespace: `upstream/vX.Y.Z`.

### Versioning

Downstream releases use a **4-part version**: `vX.Y.Z.R`

- `X.Y.Z` matches the upstream gsudo version (e.g. `2.6.1`)
- `R` is the downstream revision, starting at `0` and incremented for additional releases based on the same upstream version

Examples: `v2.6.1.0`, `v2.6.1.1`, `v2.6.1.2` for multiple releases based on upstream `v2.6.1`.

## Applied Patches

The downstream patches add a **UniElevate build variant** using `#if UNIELEVATE` conditional compilation:

### New Files
- `src/gsudo/IntegrityHelpers.cs` — Security verification system (compiled only for UniElevate)
- `src/gsudo/Tokens/NativeMethods.cs` — Additional P/Invoke declarations
- `src/gsudo/icon/unielevate.ico` — Custom icon for UniElevate
- `build-variants.ps1` — Build script for both variants

### Modified Files
- `src/gsudo/gsudo.csproj` — Conditional build properties for UniElevate variant
- `src/gsudo/AppSettings/RegistrySetting.cs` — Conditional registry key path
- `src/gsudo/Rpc/NamedPipeServer.cs` — Conditional client verification
- `src/gsudo/Program.cs` — Conditional caller verification

### Build Variants

| Variant | Output | Description |
|---------|--------|-------------|
| gsudo (vanilla) | `gsudo.exe` | Standard gsudo with no modifications to behavior |
| UniElevate | `UniGetUI Elevator.exe` | Adds process name validation, parent process whitelist, digital signature verification |

Both variants are built for **x64** and **arm64** architectures.

### NuGet Packages

| Package | Contents |
|---------|----------|
| `Devolutions.gsudo` | Vanilla `gsudo.exe` for x64 and arm64 |
| `Devolutions.UniGetUI.Elevator` | `UniGetUI Elevator.exe` for x64 and arm64 |

### Certificates

The `certs/` directory contains Devolutions code signing certificates (public `.crt` files) whose subjects are allowlisted in `IntegrityHelpers.cs` for UniElevate caller verification:

- `Devolutions_CodeSign_2023-2026.crt` — valid 2023–2026
- `Devolutions_CodeSign_2025-2028.crt` — valid 2025–2028

## Rebasing to a New Upstream Version

When upstream releases a new version (e.g. `v2.7.0`):

```bash
# 1. Fetch the latest upstream
git fetch upstream

# 2. Create a new release branch from the namespaced upstream tag
git checkout -b release/v2.7.0 upstream/v2.7.0

# 3. Cherry-pick the downstream patches from the previous release branch
#    (find the commits that are on top of the upstream base)
git log --oneline upstream/v2.6.1..release/v2.6.1  # list the patch commits
git cherry-pick <commit-hash>              # apply each patch

# 4. Resolve any conflicts, test, and push
git push origin release/v2.7.0
```

Then trigger the build workflow with `branch=release/v2.7.0` and `version=2.7.0.0`.

## Building

The GitHub Actions workflow (`build-package.yml`) handles building, code signing, NuGet packaging, and releasing. Trigger it manually via `workflow_dispatch` with:

- `version`: Release version in `X.Y.Z.R` format (e.g. `2.6.1.0`)
- `branch`: The release branch to build from (e.g. `release/v2.6.1`)
- `skip-publish`: Skip NuGet and GitHub Release publishing
- `dry-run`: Simulate publishing without actually releasing

## Local Worktree for Source

If you want to keep a local source checkout inside this repository without committing it on `master`, use a linked worktree:

```bash
# from repository root
git worktree add gsudo-src release/v2.6.1

# keep it local-only (not committed)
echo gsudo-src/ >> .git/info/exclude
```

This keeps `gsudo-src/` attached to the `release/vX.Y.Z` branch while `master` remains clean and downstream-only.

## Solution File

The downstream solution file is `gsudo-distro.sln`.

## License

gsudo is licensed under the [MIT License](https://github.com/gerardog/gsudo/blob/master/LICENSE.txt).
