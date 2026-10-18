# 项目索引

## 使用方式

- 从项目维度查看已沉淀模式。
- 用于快速判断“该项目有哪些可直接复用资产”。
- RAG 导入时同时查看 `summary_manifest`，按项目隔离代码摘要。

## 项目列表

- hx-matching:
  - simpx_path: `projects/examples/hx-matching`
  - summary_manifest: `knowledge/code-summaries/hx-matching/manifest.yaml`
  - patterns:
    - pattern-cache-redis-bloom-dual-write-v1
    - pattern-reliability-azure-sql-dba-daily-inspect-v1
  - decisions:
    - knowledge/decisions/2026/ADR-2026-10-07-hx-matching-encoding-comments.md
- betwin:
  - simpx_path: `projects/examples/betwin`
  - summary_manifest: `knowledge/code-summaries/betwin/manifest.yaml`
  - patterns: []
  - decisions: []