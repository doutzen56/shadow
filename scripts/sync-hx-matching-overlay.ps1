# Sync HX.Matching agent overlay from shadow into the business repo (local only).
# Does not commit/push the business repo. Updates .git/info/exclude so git status stays clean.

param(
    [string]$TargetRepo = "",
    [string]$Overlay = ""
)

$ErrorActionPreference = "Stop"

if (-not $Overlay) {
    $Overlay = Join-Path (Split-Path -Parent $PSScriptRoot) "projects\hx-matching"
}

if (-not $TargetRepo) {
    $found = Get-ChildItem "E:\work" -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName "HX.Matching.sln") } |
        Select-Object -First 1
    if (-not $found) { throw "Could not find HX.Matching.sln under E:\work; pass -TargetRepo" }
    $TargetRepo = $found.FullName
}

if (-not (Test-Path $Overlay)) { throw "Overlay not found: $Overlay" }
if (-not (Test-Path $TargetRepo)) { throw "Target repo not found: $TargetRepo" }
if (-not (Test-Path (Join-Path $TargetRepo ".git"))) { throw "Not a git repo: $TargetRepo" }
Write-Host "TargetRepo=$TargetRepo"

$pairs = @(
    @{ Src = "AGENTS.md"; Dst = "AGENTS.md" },
    @{ Src = ".cursor"; Dst = ".cursor" },
    @{ Src = "docs\architecture\README.md"; Dst = "docs\architecture\README.md" }
)

foreach ($p in $pairs) {
    $from = Join-Path $Overlay $p.Src
    $to = Join-Path $TargetRepo $p.Dst
    if (-not (Test-Path $from)) { throw "Missing overlay path: $from" }
    $parent = Split-Path -Parent $to
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    if (Test-Path $from -PathType Container) {
        if (Test-Path $to) { Remove-Item -Recurse -Force $to }
        Copy-Item -Recurse -Force $from $to
    } else {
        Copy-Item -Force $from $to
    }
    Write-Host "Synced $($p.Src) -> $to"
}

$exclude = Join-Path $TargetRepo ".git\info\exclude"
$markers = @(
    "# shadow-overlay:hx-matching",
    "AGENTS.md",
    ".cursor/",
    "docs/architecture/"
)
$existing = @()
if (Test-Path $exclude) {
    $existing = Get-Content $exclude
}
if ($existing -notcontains "# shadow-overlay:hx-matching") {
    Add-Content -Path $exclude -Value ""
    Add-Content -Path $exclude -Value $markers
    Write-Host "Updated local exclude: $exclude"
} else {
    Write-Host "Local exclude already has shadow-overlay marker"
}

Write-Host "Done. Business repo git status should ignore overlay files."
