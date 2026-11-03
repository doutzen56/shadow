/**
 * 业务表单连线条件字段配置工具
 * 从 workflowForm 目录的业务配置里读取预设条件字段
 */

import type { ConditionField } from '/@/views/workFlow/workflowForm/index';
import { workflowFormConfigs } from '/@/views/workFlow/workflowForm/index';

/**
 * 获取业务模块的预设连线条件字段
 *
 * @param businessModule - 业务模块名称（如 'Leave', 'Expense'）
 * @returns 条件字段数组，如果未配置则返回空数组
 */
export const getConditionFields = (businessModule: string): ConditionField[] => {
	const config = workflowFormConfigs[businessModule];
	return config?.conditionFields || [];
};
