# Thin wrapper — prefer sync-project-overlay.ps1 for new projects.
param([string]$TargetRepo = "")
& "$PSScriptRoot\sync-project-overlay.ps1" -Project hx-matching -TargetRepo $TargetRepo
