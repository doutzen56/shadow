import type { BusinessModuleCreateOptions } from '../utils/businessModuleConfig';

/**
 * 连线条件字段配置
 * 定义哪些字段可以在工作流连线条件中使用
 */
export interface ConditionField {
	/** 字段名（英文） */
	name: string;
	/** 字段显示名称 */
	label: string;
	/** 字段类型：string/number/boolean/date/datetime */
	type?: string;
}

/**
 * 业务表单配置
 */
export type WorkflowFormConfig = BusinessModuleCreateOptions & {
	conditionFields?: ConditionField[];
};
