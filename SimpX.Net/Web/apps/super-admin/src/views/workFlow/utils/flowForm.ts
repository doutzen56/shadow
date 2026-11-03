// views/workFlow/utils/flowForm.ts
import { getFlowDetail, getWorkflowFormDetail } from '/@/api/workflow';

/** form-create 设计稿形态：{ rule, options } */
export interface FcSchema {
	rule: any[];
	options: Record<string, any>;
}

/** 流程绑定的表单信息 */
export interface FlowFormInfo {
	/** 表单设计稿，未绑定或解析失败为 null */
	schema: FcSchema | null;
	/** 表单版本号 */
	version?: number;
	/** 流程名称 */
	flowName: string;
	/** 发起流程用的定义 Id */
	definitionId: string;
	/** 流程编码 */
	workflowCode: string;
}

const unwrap = (res: any) => res?.data?.result ?? res?.data?.data ?? res?.data;

/**
 * 归一化表单设计稿：字符串 / 对象 / rule 数组统一成 { rule, options }，无法识别返回 null。
 */
export const parseFcSchema = (raw: unknown): FcSchema | null => {
	let value: any = raw;
	if (typeof raw === 'string') {
		if (!raw.trim()) return null;
		try {
			value = JSON.parse(raw);
		} catch {
			return null;
		}
	}
	if (!value || typeof value !== 'object') return null;
	if (Array.isArray(value)) return { rule: value, options: {} };
	if (Array.isArray(value.rule)) return { rule: value.rule, options: value.options ?? {} };
	if (Array.isArray(value.components)) return { rule: value.components, options: value.form ?? {} };
	return null;
};

/**
 * 取流程绑定的表单设计稿：优先流程详情里的 formJson，缺失时按 formId 反查表单表。
 * @returns 设计稿 JSON 字符串，未绑定表单返回空串
 */
export const loadFlowSchemaJson = async (formJson?: string | null, formId?: number | null): Promise<string> => {
	if (formJson && formJson.trim()) return formJson;
	if (!formId) return '';
	try {
		const detail = unwrap(await getWorkflowFormDetail({ id: formId }));
		return detail?.schemaJson ?? detail?.SchemaJson ?? '';
	} catch (error) {
		console.error('[loadFlowSchemaJson] 获取表单详情失败', error);
		return '';
	}
};

/**
 * 按流程 Id 拉取流程详情及其绑定表单，供发起流程 / 填写表单页复用。
 * 接口异常会向上抛出，由调用方决定提示方式。
 */
export const resolveFlowForm = async (flowId: number): Promise<FlowFormInfo> => {
	const detail = unwrap(await getFlowDetail({ id: flowId }));
	const schemaJson = await loadFlowSchemaJson(detail?.formJson ?? detail?.FormJson, detail?.formId ?? detail?.FormId);
	const definitionId = detail?.latestDefinitionId ?? detail?.id;

	return {
		schema: parseFcSchema(schemaJson),
		version: detail?.formVersion ?? detail?.FormVersion,
		flowName: detail?.name ?? detail?.flowName ?? '',
		definitionId: definitionId ? String(definitionId) : '',
		workflowCode: detail?.workflowCode ?? detail?.code ?? '',
	};
};
