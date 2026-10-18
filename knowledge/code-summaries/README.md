# 代码摘要库

本目录按项目保存业务代码摘要。

## 规则

- 只保存摘要、路径、符号、契约、依赖、风险标签和验证提示。
- 不保存完整业务源码。
- 摘要命中后，行为判断必须回读当前源码。
- 每个项目必须有 `manifest.yaml`。

## 推荐结构

```text
code-summaries/<project-id>/manifest.yaml
code-summaries/<project-id>/by-host/*.md
code-summaries/<project-id>/by-layer/*.md
```