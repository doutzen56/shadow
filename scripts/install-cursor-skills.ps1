# Install personal Cursor skills from this repo into ~/.cursor/skills
# Does not delete other existing skills.

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Src = Join-Path $RepoRoot "cursor-skills"
$Dst = Join-Path $env:USERPROFILE ".cursor\skills"

if (-not (Test-Path $Src)) {
    throw "cursor-skills not found at $Src"
}

New-Item -ItemType Directory -Force -Path $Dst | Out-Null

Get-ChildItem -Directory $Src | ForEach-Object {
    $target = Join-Path $Dst $_.Name
    if (Test-Path $target) {
        Remove-Item -Recurse -Force $target
    }
    Copy-Item -Recurse -Force $_.FullName $target
    Write-Host "Installed skill: $($_.Name) -> $target"
}

Write-Host "Done. KB_ROOT default: E:\shadow (set in Cursor User Rules)."
