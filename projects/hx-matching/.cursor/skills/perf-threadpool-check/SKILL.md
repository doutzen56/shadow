---
name: perf-threadpool-check
description: >-
  Investigate OpenApi hangs/timeouts using the thread-pool starvation postmortem
  before blaming Redis/MQ/SQL.
---

# perf-threadpool-check

1. Read `docs/线程池饥饿事故复盘与修复说明.md` first
2. Look for sync-over-async, global locks, long-held DB connections
3. Compare with existing fixes (`ThreadPoolStartupTuning`, split-table cache, order L1/Redis breaker patterns)
4. Minimal fix only; no drive-by refactors
