# Sync a project agent overlay from shadow into a business repo (local only).
# Does not commit/push the business repo. Updates .git/info/exclude so git status stays clean.
#
# Examples:
#   .\sync-project-overlay.ps1 -Project hx-matching -TargetRepo "E:\work\撮合项目"
#   .\sync-project-overlay.ps1 -Project my-api -TargetRepo "E:\work\my-api"
#   .\sync-project-overlay.ps1 -Project hx-matching
#     (auto-finds repo under E:\work that contains marker file from projects/<name>/overlay.json)

param(
    [Parameter(Mandatory = $true)]
    [string]$Project,
    [string]$TargetRepo = ""
)

$ErrorActionPreference = "Stop"
$ShadowRoot = Split-Path -Parent $PSScriptRoot
$Overlay = Join-Path $ShadowRoot "projects\$Project"

if (-not (Test-Path $Overlay)) {
    throw "Overlay not found: $Overlay  (create projects\$Project from projects\_template)"
}

$metaPath = Join-Path $Overlay "overlay.json"
$markerFile = $null
if (Test-Path $metaPath) {
    $meta = Get-Content $metaPath -Raw | ConvertFrom-Json
    if ($meta.markerFile) { $markerFile = [string]$meta.markerFile }
}

if (-not $TargetRepo) {
    if (-not $markerFile) {
        throw "Pass -TargetRepo, or add overlay.json with markerFile (e.g. HX.Matching.sln)"
    }
    $found = Get-ChildItem "E:\work" -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName $markerFile) } |
        Select-Object -First 1
    if (-not $found) { throw "Could not find repo with $markerFile under E:\work; pass -TargetRepo" }
    $TargetRepo = $found.FullName
}

if (-not (Test-Path $TargetRepo)) { throw "Target repo not found: $TargetRepo" }
if (-not (Test-Path (Join-Path $TargetRepo ".git"))) { throw "Not a git repo: $TargetRepo" }
Write-Host "Project=$Project"
Write-Host "Overlay=$Overlay"
Write-Host "TargetRepo=$TargetRepo"

$pairs = @()
if (Test-Path (Join-Path $Overlay "AGENTS.md")) {
    $pairs += @{ Src = "AGENTS.md"; Dst = "AGENTS.md" }
}
if (Test-Path (Join-Path $Overlay ".cursor")) {
    $pairs += @{ Src = ".cursor"; Dst = ".cursor" }
}
$archReadme = Join-Path $Overlay "docs\architecture\README.md"
if (Test-Path $archReadme) {
    $pairs += @{ Src = "docs\architecture\README.md"; Dst = "docs\architecture\README.md" }
}
if ($pairs.Count -eq 0) { throw "Overlay has nothing to sync (need AGENTS.md and/or .cursor)" }

foreach ($p in $pairs) {
    $from = Join-Path $Overlay $p.Src
    $to = Join-Path $TargetRepo $p.Dst
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
$marker = "# shadow-overlay:$Project"
$block = @(
    $marker,
    "AGENTS.md",
    ".cursor/",
    "docs/architecture/"
)
$existing = @()
if (Test-Path $exclude) { $existing = Get-Content $exclude }
if ($existing -notcontains $marker) {
    Add-Content -Path $exclude -Value ""
    Add-Content -Path $exclude -Value $block
    Write-Host "Updated local exclude: $exclude"
} else {
    Write-Host "Local exclude already has $marker"
}

Write-Host "Done. Re-run after editing projects\$Project in shadow."
