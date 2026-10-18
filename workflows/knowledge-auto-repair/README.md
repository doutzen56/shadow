# 知识库自动修复

## 目标

自动修复低风险索引和摘要问题；对业务语义、项目边界和正式知识内容只生成候选修复。

## 输入

- `config/rag-repair.yaml`
- `knowledge/indexes/rag-manifest.yaml`
- SQLite catalog
- Qdrant collections
- 代码摘要 manifest

## 标准步骤

1. 执行健康检查。
2. 按 L0-L4 分类风险。
3. L1 自动修复索引、FTS、孤儿向量和缺失 metadata。
4. L2 自动重建过期代码摘要，但不改变业务语义。
5. L3 生成候选修复到 `knowledge/candidates/`。
6. L4 阻塞并请求用户或 `@@TL` 决策。
7. 写入 repair_log。

## 输出

- 健康检查结果。
- 自动修复清单。
- 候选修复清单。
- 阻塞项。
- 回滚建议。

## 收口检查

- 没有修复前后状态，不得宣称修复完成。
- 自动修复不能覆盖正式知识和项目边界。