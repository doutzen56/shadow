# 知识模式卡模板（Candidate）

## 目标

把“可复用经验”转成“可直接编码的实现蓝图”，在不同项目仅替换适配层。

## 基本信息

- pattern_id:
- title:
- domain: search / cache / messaging / data-consistency / reliability / rollout
- owner: @@BA / @@BE / @@DEVOPS
- created_at:

## 抽象问题定义（先回答为什么做）

- abstract_problem: 要解决的抽象问题
- failure_of_old_way: 旧做法为什么失败
- goal_invariant: 这套方案必须长期保持的核心目标

## 能力组件拆解（先组件，再代码）

每个能力组件必须写：

- component_name:
- component_role: 该组件解决什么子问题
- input_contract: 输入约束
- output_contract: 输出约束
- failure_contract: 失败语义
- observable_signal: 如何观察该组件健康

## 核心不变量（必须）

- state_invariants: 状态不变量
- data_invariants: 数据不变量
- timing_invariants: 时序不变量
- security_invariants: 安全不变量

## 关键方法 / 逻辑节点（必须）

- key_methods:
  - method_name:
  - responsibility:
  - pre_condition:
  - post_condition:
  - idempotency_rule:
- key_decision_nodes:
  - node:
  - decision_input:
  - decision_output:

## 核心实现蓝图（平台无关）

- algorithm_blueprint: 用步骤或伪代码描述核心流程，不绑定语言/框架
- adapter_points: 平台适配点（存储、消息、配置、观测）
- non_negotiable_flow: 不可删减的最小闭环步骤

## 参数与取舍

- key_parameters:
- tradeoffs:
- anti_patterns:

## 验收断言（可直接测试）

- verification_checklist: 期望值/实际值/失败判定
- rollback_strategy:
- performance_cost:

## 来源证据（代码仅引用）

- source_projects:
- evidence_refs:
- reusable_assets:

### 代码引用规范

- 主体必须是抽象蓝图，代码只用于证据定位。
- 每个 Pattern 最多 2 段片段，每段不超过 40 行。
- 超过限制用“摘要 + file_path + symbol”。
- 禁止整文件复制。

## 不可直接复用部分（必须）

- coupling_to_project:
- required_replacements:

## 评审结论（由 TL 决策）

- decision: promote / merge / reject
- comments:
