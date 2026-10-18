# Agent 治理巡检

## 目标

定期发现规则、角色、工作流和配置的偏差，驱动持续修正。

## 巡检周期

- 建议每周一次。
- 遇到重大规则变更后追加一次。

## 巡检范围

1. 规则冲突（路径、角色、禁令是否一致）。
2. 角色一致性（README、simpx.yaml、agent system 是否一致）。
3. 工作流完整性（输入/步骤/输出/检查点是否齐全）。
4. 环境配置完整性（项目 `envs.yaml` 声明的 `default_envs` 是否都有配置，是否有密钥文件约定；不要求 `dev/test/uat` 齐全）。
5. 安全合规（是否有明文密钥入库风险）。
6. 知识成长性（是否产出高价值候选模式、是否复用已有模式）。
7. 抽象质量（是否以能力/契约/状态机沉淀，而非代码堆砌）。
8. 检索预算（是否遵守 Top-K、轮次、代码引用上限）。
9. RAG 语料一致性（`rag-corpus.yaml`、`rag-role-matrix.yaml`、`rag-manifest.yaml` 是否同步）。
10. 代码摘要健康度（是否存在过期摘要、孤儿符号、缺失风险标签或跨项目误召回）。
11. 自动修复边界（低风险修复是否自动处理，高风险修复是否进入候选队列）。

12. 路由完整性（`workflows/` 下每个工作流都能从 `config/rag-x-routing.yaml` 的 `task_routing`、决策树或 `task_end_hooks` 走到）。
13. 三处排除规则同步（`rag-manifest.yaml` 的 `excluded`、`rag-corpus.yaml` 的 `excluded_paths` / `forbidden_globs`、`rag-importer.yaml` 的 `simpx_never_import` / `security`，以及 `tools/rag_index.py` 的 `DOC_SOURCES` 和敏感文件判断）。
14. 运行时产物一致（`rag-manifest.yaml` 的 run_id、manifest_hash、数量，和 `rag/init-reports/` 最新报告、`rag/index.db` 的 `ingestion_run` 对得上；不一致就重跑 `tools/rag_index.py`）。
15. 引用健康（`templates/`、`workflows/*/examples/`、`knowledge/indexes/` 下每个文件至少被一个工作流、角色或规则引用；没人引用的要么补引用，要么标规划或归档）。
16. `core/` 与 `.cursor/rules/` 同主题内容是否漂移；冲突时以 `.cursor/rules/` 为准，并把 `core/` 对应段落改成引用。

角色一致性巡检必须使用 `role-consistency-check.md`。

### 可以直接跑的检查

```text
python tools/rag_index.py search <关键词>     # 抽查检索能否命中，会写 retrieval_trace
```

```sql
-- 对 rag/index.db 执行
select run_id, run_type, project_id, manifest_hash from ingestion_run order by started_at;
select count(*) from project where repo_root is not null;            -- 应为 0
select knowledge_id from knowledge_item where user_reviewed = 0;     -- 周审要看的条目
select path from source where lifecycle_status = 'stale';            -- 过期的深挖卡片
```

所有工作流巡检前，先按 `workflows/_shared/` 完成任务入口、权限、证据、验证、角色交接和完成检查。

## 输出格式

- 发现项。
- 影响等级（高/中/低）。
- 修正建议。
- 责任角色。
- 预计完成时间。

## 收口步骤

1. `@@X` 汇总巡检范围和执行顺序。
2. `@@TL` 确认优先级。
3. `@@BA` / `@@BE` / `@@DEVOPS` 按职责执行修正。
4. `@@TL` 验收并确认是否沉淀。

## 知识汇总子流程（每周）

1. `@@X` 汇总本周候选模式（使用 `templates/knowledge-weekly-review`）。
2. `@@BA` / `@@BE` / `@@DEVOPS` 先补齐抽象层（能力、契约、状态机、策略点），再补证据引用。
3. `@@X` 汇总本周 RAG 召回质量：误召回、漏召回、候选堆积、过期摘要、断链证据。
4. `@@TL` 决策：入库 / 合并 / 驳回 / 归档 / 触发自动修复；决定入库的直接写正式目录，标 `user_reviewed: false`。
5. 用户周审 `@@TL` 本周入库的条目，可保留、修正或回退。

## 约束规则（强制）

- 禁止整段复制业务代码作为知识主体。
- 代码只允许作为证据引用，优先“摘要 + 路径 + 符号”。
- 若超过检索预算（Top-3、2轮、代码引用上限），停止继续拉取并升级 `@@TL` 决策。
- `candidate` 默认不参与日常实现召回，只在沉淀、复盘、治理和 RAG 运维任务中使用。
- RAG 自动修复不能覆盖正式知识和项目边界，只能生成候选修复。
