---
id: adr-2026-10-21-simpx-utf8-all-io
title: Agent 读写一律 UTF-8
doc_type: decision
projects: []
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
---

# ADR-2026-10-21：Agent 写文件 / 写数据 / 调外部工具，读写编码统一 UTF-8（记住）

- 日期：2026-10-21
- 来源：用户 `@@X`「规范一下 AI，写文件写数据写任何东西，调用任何脚本任何语言等外部工具写，读写编码统一 utf-8。记住这点」
- 范围：SimpX 全局（所有项目、所有 Agent）
- 状态：生效

## 决定

1. Agent 凡是写文件、写数据、写种子或 SQL、写测试辅助产物，以及调用 Python / PowerShell / .NET / Node / shell 等任何外部工具做读写，编码统一 **UTF-8（不带 BOM）**。
2. 不得依赖操作系统默认编码；Windows 上禁止用易写成 ANSI / UTF-16 的默认重定向与 `Set-Content` / `Out-File`。
3. 往库里写中文时，脚本源文件本身要是 UTF-8；易被终端弄坏时用 `NCHAR` 码点或参数化，避免落成 `?`。
4. 细则与示例以 `.cursor/rules/encoding.mdc` 为准（已按本决定扩到「任何读写」，不限文档）。

## 后果

- 再出现 UTF-16 空字节、中文变 `????`、脚本默认编码写坏文件，视为未按规则完成，要先修编码再继续业务改动。
- 与 2026-10-03 / 2026-10-07 编码约定一致，本条把适用范围从「文档」明确扩到「一切 Agent IO」。