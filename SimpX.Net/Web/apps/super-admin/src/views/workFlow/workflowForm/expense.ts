/**
 * 报销模块配置（示例）
 * 工作流系统业务表单配置
 */

import type { WorkflowFormConfig } from './types';

/**
 * 报销模块配置对象
 *
 * 使用说明：
 * 1. 复制此文件并修改为你的业务模块名称
 * 2. 确保 API 文件中包含对应的三个函数导出
 * 3. value 必须与 businessModuleOptions 中的 value 一致
 * 4. enabled: 是否启用该业务模块（默认 true）
 * 5. formPreviewRoute: 原业务表单预览页的路由地址（可选）
 */
export const expenseConfig: WorkflowFormConfig = {
	label: '报销模块',
	value: 'Expense',
	apiPath: '/@/api/demo/expense',
	apis: {
		getListApiName: 'getExpensePage',
		getDetailApiName: 'getExpenseDetail',
		submitApiName: 'submitExpense',
	},
	detailComponentPath: '/@/views/demo/expense/ExpenseDetail.vue',
	enabled: false, // 暂时不启用
	formPreviewRoute: '/demo/expense/index', // 原表单预览页路由地址
	conditionFields: [
		{ name: 'expenseType', label: '报销类型', type: 'string' },
		{ name: 'amount', label: '金额', type: 'number' },
		{ name: 'date', label: '日期', type: 'date' },
		{ name: 'applicantName', label: '申请人', type: 'string' },
		{ name: 'deptName', label: '部门', type: 'string' },
		{ name: 'reason', label: '报销原因', type: 'string' },
	],
};

export default expenseConfig;
