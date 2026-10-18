# RAG 质量评估

## 目标

用黄金问题集验证项目隔离、角色路由、代码摘要召回、规则优先级和敏感信息排除。

## 输入

- `config/rag-evaluation.yaml`
- `config/rag-retrieval.yaml`
- `config/rag-role-matrix.yaml`
- 当前 manifest、SQLite 和 Qdrant 状态

## 标准步骤

1. 读取黄金问题集。
2. 对每个问题执行项目识别、角色路由和检索。
3. 比对 expected 和 actual hits。
4. 记录误召回、漏召回、过期命中和敏感信息风险。
5. 生成评估报告。
6. 失败项进入 `knowledge-auto-repair` 或 `agent-governance`。

## 输出

- 评估报告。
- 失败问题清单。
- 召回质量指标。
- 修复建议。

## 收口检查

- 未覆盖项目隔离、代码召回和敏感信息排除时，不算完成。
- 失败项不能被写成通过。