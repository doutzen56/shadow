---
name: capture-adr
description: >-
  Write an Architecture Decision Record under KB_ROOT/architecture/adr and
  update INDEX.md. Use when user says 记一条 ADR or capture-adr.
---

# capture-adr

1. Treat `记一条 ADR` as confirmation to write.
2. Copy structure from `$KB_ROOT/templates/adr.md`.
3. Filename: next `NNNN-slug.md` under `$KB_ROOT/architecture/adr/`.
4. Update `$KB_ROOT/architecture/INDEX.md`.
5. Status default `accepted` unless user says proposed.
6. Do not put secrets or production connection strings in ADRs.
