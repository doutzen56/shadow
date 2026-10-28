# 项目索引

## 使用方式

- 从项目维度查看已沉淀模式。
- 用于快速判断“该项目有哪些可直接复用资产”。
- RAG 导入时同时查看 `summary_manifest`，按项目隔离代码摘要。

## 项目列表

- hx-matching:
  - simpx_path: `projects/examples/hx-matching`
  - summary_manifest: `knowledge/code-summaries/hx-matching/manifest.yaml`
  - deep_cards: `knowledge/code-summaries/hx-matching/cards/`
  - profile_docs_extra:
    - business-domains.md
    - flows-order-and-matching.md
    - flows-async-jobs.md
    - flows-multitenant-and-review.md
  - glossary: `knowledge/glossary/hx-matching-domain.md`
  - patterns:
    - pattern-cache-redis-bloom-dual-write-v1
    - pattern-reliability-azure-sql-dba-daily-inspect-v1
    - pattern-reliability-hotpath-admission-gate-v1
    - pattern-reliability-callback-ack-then-async-query-v1
    - pattern-messaging-webhook-mq-local-fallback-v1
    - pattern-data-consistency-redis-shared-matching-pool-v1
    - pattern-reliability-channel-adapter-registry-v1
    - pattern-reliability-saas-tenant-init-steps-v1
  - decisions:
    - knowledge/decisions/2026/ADR-2026-10-07-hx-matching-encoding-comments.md
    - knowledge/decisions/2026/ADR-2026-10-20-hx-matching-host-tenant-resolve.md
    - knowledge/decisions/2026/ADR-2026-10-26-hx-matching-withdrawal-enter-pool-gate.md
  - deep_cards_extra:
    - knowledge/code-summaries/hx-matching/cards/host-tenant-resolve.md
    - knowledge/code-summaries/hx-matching/cards/withdrawal-enter-matching-pool.md
    - knowledge/code-summaries/hx-matching/cards/channel-callback-ack-query.md
    - knowledge/code-summaries/hx-matching/cards/tenant-init-worker.md
    - knowledge/code-summaries/hx-matching/cards/review-owner-boundary.md
- betwin:
  - simpx_path: `projects/examples/betwin`
  - summary_manifest: `knowledge/code-summaries/betwin/manifest.yaml`
  - deep_cards: `knowledge/code-summaries/betwin/cards/`
  - patterns:
    - pattern-money-agent-addmoneylog-v1
    - pattern-payment-callback-idempotency-checklist-v1
  - incidents:
    - INC-2026-10-18-betwin-recharge-callback-swallow
    - INC-2026-10-18-betwin-withdraw-fail-back-to-wait
    - INC-2026-10-18-betwin-api-cert-unauth
  - decisions: []
