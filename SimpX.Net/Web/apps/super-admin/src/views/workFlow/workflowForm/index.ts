/**
 * 业务表单配置索引：自动聚合 workflowForm 目录下的所有业务模块配置
 * 新增业务模块时，在本目录建一个 .ts 文件、导出 default 配置对象即可被自动发现
 */

import type { WorkflowFormConfig } from './types';
export type { ConditionField, WorkflowFormConfig } from './types';

const workflowFormModules = import.meta.glob<{ default: WorkflowFormConfig }>(['./*.ts', '!./index.ts', '!./types.ts'], { eager: true });

/**
 * 业务表单配置集合
 * key: 业务模块的 value 值（如 'Leave', 'Expense'）
 * value: 业务模块配置对象
 */
export const workflowFormConfigs: Record<string, WorkflowFormConfig> = Object.values(workflowFormModules).reduce(
	(acc, moduleConfig) => {
		const config = moduleConfig?.default;
		if (!config?.value) return acc;

		acc[config.value] = config;
		return acc;
	},
	{} as Record<string, WorkflowFormConfig>
);
