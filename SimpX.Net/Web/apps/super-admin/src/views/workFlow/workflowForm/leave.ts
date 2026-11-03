/**
 * 请假模块配置
 * 工作流系统业务表单配置示例
 */

import type { WorkflowFormConfig } from './types';

/**
 * 请假模块配置对象
 *
 * 说明：
 * 1. value: 必须与 businessModuleOptions 中的 value 一致
 * 2. apiPath: API 文件路径，确保该文件包含 getListApiName、getDetailApiName、submitApiName 对应的导出函数
 * 3. detailComponentPath: 详情组件路径（可选），用于在工作流详情页展示业务数据
 * 4. enabled: 是否启用该业务模块（默认 true），为 false 时不会出现在业务模块选项中
 * 5. formPreviewRoute: 原业务表单预览页的路由地址（可选），用于在工作流详情页中跳转查看原表单
 * 6. conditionFields: 连线条件可用字段（可选），用于工作流条件判断时提供字段选项
 */
export const leaveConfig: WorkflowFormConfig = {
	label: '请假模块',
	value: 'Leave',
	apiPath: '/@/api/workflow/demo_leave',
	apis: {
		getListApiName: 'getLeavePage',
		getDetailApiName: 'getLeaveDetail',
		submitApiName: 'submitLeave',
	},
	detailComponentPath: '/@/views/demo/leave/LeaveDetail.vue',
	enabled: true, // 是否启用
	formPreviewRoute: '/demo/leave/index', // 原表单预览页路由地址
	// 连线条件可用字段（只暴露常用的字段）
	conditionFields: [
		{ name: 'leaveType', label: '请假类型', type: 'string' },
		{ name: 'days', label: '请假天数', type: 'number' },
		{ name: 'startTime', label: '开始时间', type: 'date' },
		{ name: 'endTime', label: '结束时间', type: 'date' },
		{ name: 'applicantName', label: '申请人', type: 'string' },
		{ name: 'deptName', label: '部门', type: 'string' },
	],
};

export default leaveConfig;
