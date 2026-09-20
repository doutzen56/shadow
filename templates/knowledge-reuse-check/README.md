# 知识复用检查模板（执行前）

## 任务信息

- task_id:
- project_id:
- owner:

## 检索结果

- matched_patterns:
- selected_pattern:
- reason_for_selection:
- selected_abstractions:
  - capability_pattern:
  - state_machine:
  - contracts:
  - strategy_points:

## 若未复用

- non_reuse_reason:
- gap_summary:
- should_create_candidate: yes/no

## 代码读取预算（防巨量请求）

- max_patterns_read: <=3
- max_rounds: <=2
- max_code_refs: <=5
- max_snippets: <=2
- snippet_max_lines: <=40
- exceeded_budget_action: 升级 `@@TL` 决策（停止继续拉取）

## 执行后回填

- what_worked:
- what_failed:
- candidate_to_create:
- abstraction_reused:
- adapter_needed:
