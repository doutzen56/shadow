# 变更记录

## 2026-10-18 全局治理修复

- 角色加载：`@@X` 进入任何角色职责阶段、指派 `reviewer` / `researcher` 前都要先读对应 `system.md`；新增“子任务证据回流”格式，交接块要写读过的角色定义。
- 路由：`rag-x-routing.yaml` 改为决策树，`测试验证` 不再直接走 `release`，`只读分析` 默认不套工作流；16 个工作流都有入口。
- RAG：新增 `tools/rag_index.py`（bootstrap / project / search），替换只能跑 BetWin 的旧脚本；两个项目都能复跑，`knowledge_item` 有数据，`search` 会写 `retrieval_trace`。
- RAG 索引分库：`rag/index/core.db`（文档层）+ `rag/index/projects/<id>.db`（项目代码层）+ `rag/traces/<年-月>.db`（轨迹，保留 30 天）；索引可再生不进 git，换电脑靠 bootstrap/project/search 自愈。
- RAG 边界：importer、corpus、manifest 三处排除规则对齐；不再导入 `.config` 和 `appsettings*.json`；业务代码里叫 `Logs` 的目录不再被误排除。
- `rag/`：`init-reports/` 与 `qdrant/` 可进 git；`index/` 与 `traces/` 是可再生本地索引，不进 git。
- 知识元数据：区分 `lifecycle_status`（知识生命周期）和 `maturity`（模式成熟度）；正式知识补 `confirmed_by` / `user_reviewed`。
- 代码摘要：两个项目统一成脚本生成的分组格式；`code-summary-card` 改为高风险文件的手写深挖卡，源码哈希变了自动标 stale。
- 引用清理：`technical-design`、`test-plan`、`review-report`、`adr` 模板和工作流示例都挂到对应工作流；新增 `projects/profiles/README.md` 风格对照表。

## 一阶段初始版

- 建立 SimpX 长期目录蓝图。
- 确定一阶段采用“完整结构保留，核心能力优先完善”的建设方式。
- 明确所有文档内容使用中文。
- 建立角色、流程、项目风格、知识沉淀和配置的基础位置。
