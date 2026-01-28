# gsudo with UniElevate Build Variant

This branch demonstrates a minimal-diff approach to maintaining both vanilla gsudo and a customized "UniElevate" variant from a single codebase.

## Key Differences from Upstream

This branch adds **only** the following changes to vanilla gsudo:

### New Files Added
- `src/gsudo/IntegrityHelpers.cs` - Security verification system (compiled only for UniElevate)
- `src/gsudo/Tokens/NativeMethods.cs` - Additional P/Invoke declarations
- `src/gsudo/icon/unielevate.ico` - Custom icon for UniElevate
- `build-variants.ps1` - Build script for both variants

### Modified Files
- `src/gsudo/gsudo.csproj` - Added conditional build properties for UniElevate variant
- `src/gsudo/AppSettings/RegistrySetting.cs` - Conditional registry key path
- `src/gsudo/Rpc/NamedPipeServer.cs` - Conditional client verification
- `src/gsudo/Program.cs` - Conditional caller verification

### What's Preserved
✅ **All** build scripts, tests, documentation, and infrastructure from upstream
✅ Clean merge path from upstream gsudo
✅ Single codebase for both variants

## Building

### Build vanilla gsudo:
```powershell
dotnet build src/gsudo/gsudo.csproj -c Release
# Output: gsudo.exe
```

### Build UniElevate variant:
```powershell
dotnet build src/gsudo/gsudo.csproj -c Release -p:BuildVariant=UniElevate
# Output: UniGetUI Elevator.exe
```

### Build both variants:
```powershell
.\build-variants.ps1 -Variant All
```

## UniElevate Features

When built with `-p:BuildVariant=UniElevate`:
- Assembly name: "UniGetUI Elevator"
- Registry key: `SOFTWARE\unielevate-gsudo`
- Custom icon in UAC prompts
- Security verification:
  - Process name validation
  - Parent process whitelist
  - Digital signature verification
  - Helper DLL integrity check

## Advantages Over Separate Branches

- **Minimal divergence**: Only ~200 lines of conditional code
- **Easy merging**: Can pull upstream changes cleanly
- **Single codebase**: No duplication
- **Preserved infrastructure**: All tests, docs, build scripts remain
