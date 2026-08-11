# Project overlay template

Copy this folder to `projects/<your-project-name>/` and fill in.

## Suggested layout

```
projects/<name>/
  overlay.json          # { "markerFile": "Something.sln" } for auto-find under E:\work
  README.md
  AGENTS.md
  .cursor/
    rules/
    skills/
    LEARNINGS.md
  docs/architecture/README.md   # optional short entry
```

## Sync into a business repo

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File E:\shadow\scripts\sync-project-overlay.ps1 -Project <name> -TargetRepo "E:\work\<repo>"
```

If `overlay.json` has `markerFile`, you can omit `-TargetRepo` when the repo lives under `E:\work`.

Do **not** commit overlay files into the business git; the sync script writes local `.git/info/exclude`.
