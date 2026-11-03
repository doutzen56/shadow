<template>
	<el-dialog :model-value="dialogVisible" title="提交审批" width="860px" draggable class="business-workflow-start-dialog" @update:model-value="handleUpdateVisible">
		<template #header>
			<div class="dialog-title">
				<el-icon size="16"><ele-Promotion /></el-icon>
				<span> 提交审批 </span>
			</div>
		</template>

		<el-form label-width="96px">
			<el-form-item label="选择流程" class="flow-form-item">
				<div class="flow-select-container" v-loading="state.loading">
					<el-empty v-if="!state.loading && (!state.definitions || state.definitions.length === 0)" description="当前业务模块暂无可用流程" />
					<div v-else class="flow-scroll">
						<el-radio-group v-model="state.selectedDefinitionId" class="flow-radio-group">
							<el-radio v-for="item in state.definitions" :key="item.id" :value="item.id" class="flow-radio-item">
								<div class="flow-item">
									<div class="flow-item-header">
										<span class="flow-name">{{ getDefinitionName(item) }}</span>
										<el-tag v-if="getDefinitionActive(item)" type="success">启用</el-tag>
									</div>
									<div class="flow-item-meta">
										<span class="meta-item meta-code"
											><span class="meta-label">编码</span><span class="meta-value">{{ getDefinitionCode(item) }}</span></span
										>
										<span class="meta-item"
											><span class="meta-label">分类</span><span class="meta-value">{{ getDefinitionCategory(item) }}</span></span
										>
										<span class="meta-item"
											><span class="meta-label">版本</span><span class="meta-value">{{ getDefinitionVersion(item) }}</span></span
										>
									</div>
								</div>
							</el-radio>
						</el-radio-group>
					</div>
				</div>
			</el-form-item>
			<el-form-item label="备注">
				<el-input v-model="state.remark" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" placeholder="可填写提交审批的说明信息" />
			</el-form-item>
		</el-form>
		<template #footer>
			<el-button @click="handleCancel">取消</el-button>
			<el-button type="primary" :loading="state.submitting" :disabled="!state.selectedDefinitionId" @click="handleConfirm">确定</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts" name="BusinessWorkflowStartDialog">
import { reactive, watch, computed } from 'vue';
import { ElMessage } from 'element-plus';

import { getBusinessModuleConfig } from '../utils/businessModuleConfig';
import { getDefinitions, startWorkflowInstance } from '/@/api/workflow';

interface WorkflowDefinitionItem {
	id: number;
	name?: string;
	flowName?: string;
	code?: string;
	workflowCode?: string;
	flowCode?: string;
	category?: string;
	version?: number;
	businessModule?: string;
	[key: string]: any;
}

const props = defineProps<{
	visible: boolean;
	businessModule: string;
	businessId: number | string;
	defaultWorkflowCode?: string;
}>();

const emit = defineEmits<{
	(e: 'update:visible', value: boolean): void;
	(e: 'success', payload: { instanceCode?: string; definitionId: number; businessId: number | string }): void;
}>();

const state = reactive({
	loading: false,
	submitting: false,
	definitions: [] as WorkflowDefinitionItem[],
	selectedDefinitionId: null as number | null,
	remark: '',
});

const dialogVisible = computed({
	get: () => props.visible,
	set: (val: boolean) => emit('update:visible', val),
});

const handleUpdateVisible = (val: boolean) => {
	dialogVisible.value = val;
};

const parsePagedResult = (res: any) => {
	const data = res?.data?.result ?? res?.data?.data ?? res?.data ?? res;
	const items = Array.isArray(data) ? data : (data?.items ?? data?.list ?? data?.rows ?? []);
	return {
		items: Array.isArray(items) ? items : [],
		total: data?.total ?? data?.totalCount ?? data?.count ?? 0,
	};
};

const getDefinitionName = (item: WorkflowDefinitionItem) => item.name || item.flowName || item.Name || item.FlowName || '-';
const getDefinitionCodeValue = (item: WorkflowDefinitionItem) => item.code || item.workflowCode || item.flowCode || item.Code || item.WorkflowCode || item.FlowCode;
const getDefinitionCode = (item: WorkflowDefinitionItem) => getDefinitionCodeValue(item) || '-';
const getDefinitionCategory = (item: WorkflowDefinitionItem) => item.category || item.Category || '-';
const getDefinitionVersion = (item: WorkflowDefinitionItem) => {
	const version = item.version ?? item.Version;
	return version ? `v${version}` : '未发布';
};
const getDefinitionActive = (item: WorkflowDefinitionItem) => (item.isActive ?? item.IsActive) !== false;

const resetState = () => {
	state.definitions = [];
	state.selectedDefinitionId = null;
	state.remark = '';
};

const loadDefinitions = async () => {
	if (!props.businessModule) {
		resetState();
		return;
	}
	state.loading = true;
	try {
		const res = await getDefinitions({
			page: 1,
			pageSize: 50,
			isActive: true,
			onlyLatest: true,
			businessModule: props.businessModule,
		});
		const parsed = parsePagedResult(res);
		// 后端已根据 businessModule 做过筛选，这里直接使用返回结果
		const list = parsed.items as any[];
		state.definitions = list;

		// 默认选中逻辑：优先按 workflowCode / flowCode，找不到则在只有一个流程时自动选中
		let selected: WorkflowDefinitionItem | undefined;
		if (props.defaultWorkflowCode) {
			selected = list.find((x) => getDefinitionCodeValue(x) === props.defaultWorkflowCode);
		}
		if (!selected && list.length === 1) {
			selected = list[0];
		}
		state.selectedDefinitionId = selected ? selected.id : null;
	} catch (error: any) {
		ElMessage.error(error?.message || '获取业务流程失败');
	} finally {
		state.loading = false;
	}
};

watch(
	() => [props.visible, props.businessModule, props.defaultWorkflowCode] as const,
	([visible]) => {
		if (visible) {
			resetState();
			loadDefinitions();
		}
	},
	{ immediate: true }
);

const handleCancel = () => {
	dialogVisible.value = false;
};

const handleConfirm = async () => {
	if (!state.selectedDefinitionId) {
		ElMessage.warning('请选择流程');
		return;
	}
	if (!props.businessId && props.businessId !== 0) {
		ElMessage.error('缺少业务ID，无法发起流程');
		return;
	}

	state.submitting = true;
	try {
		const definition = state.definitions.find((x) => x.id === state.selectedDefinitionId);
		const workflowCode = definition ? getDefinitionCodeValue(definition) : undefined;
		const businessIdNum = Number(props.businessId);

		const config = getBusinessModuleConfig(props.businessModule);
		let res: any;
		if (config && config.submitApi) {
			// 优先走业务模块自己的 Submit 接口（推荐方式）
			res = await config.submitApi({
				businessId: businessIdNum,
				definitionId: state.selectedDefinitionId,
				workflowCode: workflowCode || undefined,
				remark: state.remark || undefined,
			});
		} else {
			// 兜底：直接调用工作流启动接口
			res = await startWorkflowInstance({
				definitionId: state.selectedDefinitionId,
				businessModule: props.businessModule,
				businessId: businessIdNum,
				remark: state.remark || undefined,
				workflowCode: workflowCode || undefined,
			});
		}

		if (res?.data?.type && res?.data?.type !== 'success') return;

		const instanceCode = res?.data?.result?.instanceCode || res?.data?.data?.instanceCode;
		if (instanceCode) {
			ElMessage.success(`已提交审批，实例编码：${instanceCode}`);
		} else {
			ElMessage.success('已提交审批');
		}

		emit('success', {
			instanceCode,
			definitionId: state.selectedDefinitionId,
			businessId: props.businessId,
		});
		dialogVisible.value = false;
	} catch (error: any) {
		ElMessage.error(error?.message || '提交审批失败');
	} finally {
		state.submitting = false;
	}
};
</script>

<style scoped>
.flow-select-container {
	width: 100%;
	min-height: 96px;
}

.flow-scroll {
	max-height: 340px;
	overflow-y: auto;
	overflow-x: hidden;
	padding: 2px 6px 2px 0;
	box-sizing: border-box;
}

.flow-radio-group {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.flow-radio-item {
	display: flex;
	align-items: flex-start;
	width: 100%;
	height: auto;
	margin-right: 0;
	padding: 12px 14px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	box-sizing: border-box;
	white-space: normal;
	transition:
		border-color 0.2s,
		background-color 0.2s,
		box-shadow 0.2s;
}

.flow-radio-item:hover {
	border-color: var(--el-color-primary-light-5);
	background: var(--el-color-primary-light-9);
}

.flow-radio-item.is-checked {
	border-color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	box-shadow: 0 0 0 1px var(--el-color-primary-light-7);
}

.flow-item {
	display: flex;
	flex-direction: column;
	width: 100%;
	min-width: 0;
}

.flow-item-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	margin-bottom: 8px;
}

.flow-name {
	min-width: 0;
	color: var(--el-text-color-primary);
	font-weight: 600;
	line-height: 22px;
	word-break: break-word;
}

.flow-item-meta {
	display: grid;
	grid-template-columns: minmax(220px, 1.6fr) minmax(120px, 0.7fr) minmax(100px, 0.5fr);
	gap: 8px 16px;
	font-size: 12px;
	color: #909399;
	line-height: 20px;
}

.meta-item {
	display: flex;
	min-width: 0;
	gap: 6px;
}

.meta-label {
	flex: 0 0 auto;
	color: var(--el-text-color-secondary);
}

.meta-value {
	min-width: 0;
	color: var(--el-text-color-regular);
	word-break: break-word;
}

.meta-code .meta-value {
	font-family: Consolas, Monaco, 'Courier New', monospace;
}

:deep(.flow-form-item .el-form-item__content) {
	min-width: 0;
}

:deep(.flow-radio-item .el-radio__input) {
	margin-top: 3px;
}

:deep(.flow-radio-item .el-radio__label) {
	flex: 1;
	min-width: 0;
	padding-left: 12px;
}

@media (max-width: 768px) {
	.flow-item-meta {
		grid-template-columns: 1fr;
	}
}
</style>
