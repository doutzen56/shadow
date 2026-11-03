# 工作流业务表单集成指南（精简版）

本文只讲一件事：**如何在工作流中接入一个新的业务表单**。

## 1. 接入步骤（2 步）

1. 在 `workflowForm` 目录新增一个配置文件（如 `purchase.ts`）。
2. 确认对应 API 已导出并可调用，页面里能看到新业务模块。

## 2. 新建配置文件

路径示例：`Web/src/views/workFlow/workflowForm/purchase.ts`

```typescript
import type { WorkflowFormConfig } from './types';

export const purchaseConfig: WorkflowFormConfig = {
	// 必填
	label: '采购申请',
	value: 'Purchase',
	apiPath: '/@/api/demo/purchase',
	apis: {
		getListApiName: 'getPurchasePage',
		getDetailApiName: 'getPurchaseDetail',
		submitApiName: 'submitPurchase',
	},

	// 选填
	detailComponentPath: '/@/views/demo/purchase/PurchaseDetail.vue',
	formPreviewRoute: '/demo/purchase/index',
	enabled: true,
	conditionFields: [
		{ name: 'amount', label: '采购金额', type: 'number' },
		{ name: 'deptName', label: '申请部门', type: 'string' },
		{ name: 'applyTime', label: '申请时间', type: 'date' },
	],
};

export default purchaseConfig;
```

> `workflowForm/index.ts` 会自动聚合目录下的配置文件，无需再手动注册。

## 3. 字段最小说明

- `label`：业务模块显示名称（必填）
- `value`：业务模块唯一标识，建议大驼峰（必填）
- `apiPath`：API 文件路径（必填）
- `apis.getListApiName`：列表查询 API（必填）
- `apis.getDetailApiName`：详情查询 API（必填）
- `apis.submitApiName`：提交流程 API（必填）
- `enabled`：是否启用，默认 `true`（选填）
- `conditionFields`：流程连线条件字段，建议只配常用字段（选填）

## 4. 接入检查清单

- [ ] `workflowForm/*.ts` 配置文件已创建
- [ ] `apiPath` 指向的文件存在
- [ ] `apis` 里的 3 个函数都已导出
- [ ] 流程设计器中可选择该业务模块
- [ ] 发起流程与审批详情可正常打开

## 5. 常见问题

### 模块不显示？

优先检查：

1. `enabled` 是否被设为 `false`
2. 配置文件是否正确导出了 `default`
3. `value` 是否与业务模块常量一致

### `conditionFields` 不配可以吗？

可以。不配置时会回退到后端字段获取逻辑（兼容旧方式）。

---

维护建议：新增业务表单时，以 `leave.ts`、`expense.ts` 为参考，按本文模板复制一份再改字段即可。
