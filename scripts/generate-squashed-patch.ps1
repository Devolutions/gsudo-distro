param(
    [string]$RepoPath = "gsudo-src",
    [string]$Branch,
    [string]$BaseTag,
    [string]$ReleaseVersion,
    [string]$OutputDir = "patchsets/squashed"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepoPath)) {
    throw "Repository path not found: $RepoPath"
}

if (-not $Branch) {
    $Branch = (git -C $RepoPath rev-parse --abbrev-ref HEAD).Trim()
}

if (-not $BaseTag) {
    if ($Branch -match '^release/(.+)$') {
        $BaseTag = "upstream/$($Matches[1])"
    } else {
        throw "Could not infer base tag from branch '$Branch'. Pass -BaseTag explicitly."
    }
}

$baseTagExists = (git -C $RepoPath tag -l $BaseTag)
if (-not $baseTagExists) {
    throw "Base tag not found: $BaseTag"
}

if (-not $ReleaseVersion) {
    if ($Branch -match '^release/(\d+\.\d+\.\d+)$') {
        $ReleaseVersion = "$($Matches[1]).0"
    } else {
        throw "Could not infer release version from branch '$Branch'. Pass -ReleaseVersion explicitly."
    }
}

$commitCount = [int](git -C $RepoPath rev-list --count "$BaseTag..$Branch").Trim()
if ($commitCount -eq 0) {
    throw "No commits found in range $BaseTag..$Branch"
}

New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
$outputFile = Join-Path $OutputDir "gsudo-patch-$ReleaseVersion.patch"

$diff = git -C $RepoPath diff --binary "$BaseTag...$Branch"
if ([string]::IsNullOrWhiteSpace($diff)) {
    throw "No diff produced for range $BaseTag...$Branch"
}

$diff | Set-Content -Path $outputFile -Encoding UTF8

Write-Output "Branch=$Branch"
Write-Output "BaseTag=$BaseTag"
Write-Output "ReleaseVersion=$ReleaseVersion"
Write-Output "CommitCount=$commitCount"
Write-Output "OutputFile=$outputFile"
