# 项目风格

`projects/examples/<project-id>/profile.yaml` 里的 `primary_style`、`secondary_style`、`extra_style` 用中文风格名，对应关系如下。接入或开工时按 `profile.yaml` 读对应风格说明，用来调整方案深度、交付节奏、测试重点和沉淀方式。

| 风格名 | 目录 | 状态 | 使用的项目 |
| --- | --- | --- | --- |
| 企业稳定型 | `projects/profiles/enterprise/` | 已启用 | hx-matching、betwin |
| 遗留系统改造型 | `projects/profiles/legacy-modernization/` | 已启用 | betwin |
| 性能优先型 | `projects/profiles/performance-first/` | 已启用 | hx-matching |
| 业务交付型 | 未建目录 | 缺口 | hx-matching、betwin（次风格） |
| 创业快速型 | `projects/profiles/startup/` | 规划 | 无 |
| 人工智能原生型 | `projects/profiles/ai-native/` | 规划 | 无 |

- “规划”状态的风格还没有项目用过，不能当已验证的打法引用。
- “业务交付型”被两个项目当次风格用，但还没有风格说明；补上之前，按项目档案 README 的“项目风格”一节理解。
- 风格说明不进 RAG 检索，按 `profile.yaml` 确定性读取。
