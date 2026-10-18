# 代码摘要深挖卡模板

## 什么时候用

分组摘要（`knowledge/code-summaries/<project-id>/<group>.md`）由 `tools/rag_index.py` 自动生成，只有路径、符号和风险标签。

碰到资金、订单状态、权限、登录、回调、幂等这类高风险文件，分组摘要不够用时，才手写一张深挖卡，放在 `knowledge/code-summaries/<project-id>/cards/<summary_id>.md`。

- 导入脚本只登记卡片，不覆盖。
- 卡片里的 `file_hash` 和当前源码对不上时，下次导入会把卡片标成 stale，要回读源码后更新卡片和哈希。
- `file_hash` 是源码文件内容（UTF-8 读出）的 sha256，可以从 `rag/index.db` 的 `source` 表按 `path` 查 `content_hash`。

## 文件开头（frontmatter，原样写在文件最前面）

```yaml
---
summary_id:
project_id:
file_path:
file_hash:
symbol:
symbol_kind:
role_scope: []
task_scope: []
risk_tags: []
last_seen:
---
```

## 职责

说明这个文件或符号负责什么业务能力。

## 契约

- 输入：
- 输出：
- 失败契约：

## 依赖

- DB：
- Redis：
- ES：
- MQ：
- 第三方：

## 状态与副作用

说明会改哪些状态、资金、订单、缓存或外部系统。

## 验证提示

列出 `@@BE` 和 `@@DEVOPS` 可以复用的断言、命令或场景。

## 风险说明

说明资金、权限、幂等、兼容、回调、生产配置等风险。

## 源码回读要求

涉及行为判断时，必须回读当前源码；本卡片只做定位和初筛。
