<template>
	<el-dialog
		v-model="visible"
		width="640px"
		align-center
		:close-on-click-modal="false"
		class="wf-run-dialog"
		@closed="handleClosed"
	>
		<template #header>
			<div class="dialog-title">
				<el-icon size="16"><ele-VideoPlay /></el-icon>
				<span>运行工作流</span>
			</div>
		</template>

		<div class="wf-run">
			<div class="wf-run__intro">
				<span class="wf-run__icon"><i class="ri-play-circle-line" /></span>
				<div>
					<div class="wf-run__name">{{ workflow?.name || '未命名流程' }}</div>
					<div class="wf-run__desc">{{ workflow?.description || '该流程没有填写描述' }}</div>
				</div>
			</div>

			<el-form label-position="top" class="wf-run__form" @submit.prevent>
				<el-form-item label="运行输入" required>
					<el-input
						v-model="form.input"
						type="textarea"
						:rows="5"
						resize="vertical"
						maxlength="8000"
						show-word-limit
						placeholder="开始节点的输出就是这个输入，可被 {{input}} 引用"
					/>
				</el-form-item>

				<el-form-item label="初始变量（可选）">
					<div class="wf-vars">
						<div v-for="(item, index) in form.variables" :key="index" class="wf-vars__row">
							<el-input v-model="item.key" placeholder="变量名，如 topic" class="wf-vars__key" />
							<el-input v-model="item.value" placeholder="变量值，模板里用 {{var:变量名}} 引用" class="wf-vars__value" />
							<el-button icon="ele-Delete" text type="danger" @click="removeVariable(index)" />
						</div>
						<el-button icon="ele-Plus" size="small" text type="primary" @click="addVariable">添加变量</el-button>
						<div class="wf-field-tip">变量会在整个流程中保持，可被任意节点的输入模板引用。</div>
					</div>
				</el-form-item>
			</el-form>
		</div>

		<template #footer>
			<div class="dialog-footer">
				<el-button @click="visible = false">取消</el-button>
				<el-button type="primary" icon="ele-VideoPlay" :loading="submitting" @click="handleSubmit">开始运行</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="workflowRunDialog">
import { computed, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { AgentWorkflowApi } from '/@/api-services/agent/api';
import { AgentWorkflowDto } from '/@/api-services/agent/models';

const props = defineProps<{
	modelValue: boolean;
	workflow: AgentWorkflowDto | null;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'started', payload: { runId: number; workflowId: number }): void;
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const submitting = ref(false);
const form = reactive({
	input: '',
	variables: [{ key: '', value: '' }] as { key: string; value: string }[],
});

watch(
	() => props.modelValue,
	(value) => {
		if (value) {
			form.input = '';
			form.variables = [{ key: '', value: '' }];
		}
	}
);

const addVariable = () => form.variables.push({ key: '', value: '' });

const removeVariable = (index: number) => {
	form.variables.splice(index, 1);
	if (!form.variables.length) form.variables.push({ key: '', value: '' });
};

const handleSubmit = async () => {
	if (!props.workflow?.id) return;
	if (!form.input.trim()) {
		ElMessage.warning('请填写运行输入');
		return;
	}

	const variables: Record<string, string> = {};
	form.variables.forEach((item) => {
		const key = item.key.trim();
		if (key) variables[key] = item.value;
	});

	submitting.value = true;
	try {
		const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowRunPost({
			workflowId: props.workflow.id,
			input: form.input.trim(),
			variables,
		});
		const runId = Number(res.data.result ?? 0);
		ElMessage.success('已提交运行，正在后台执行');
		visible.value = false;
		emit('started', { runId, workflowId: props.workflow.id });
	} catch (error: any) {
		ElMessage.error(error?.message ?? '发起运行失败');
	} finally {
		submitting.value = false;
	}
};

const handleClosed = () => {
	form.input = '';
	form.variables = [{ key: '', value: '' }];
};
</script>

<style scoped lang="scss">
.wf-run__intro {
	display: flex;
	align-items: flex-start;
	gap: 10px;
	padding: 12px 14px;
	margin-bottom: 14px;
	border-radius: 10px;
	background-color: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
}

.wf-run__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	font-size: 18px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
	flex-shrink: 0;
}

.wf-run__name {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.wf-run__desc {
	margin-top: 2px;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
}

.wf-run__form {
	:deep(.el-form-item__label) {
		padding-bottom: 4px;
		font-size: 12px;
		color: var(--el-text-color-regular);
	}
}

.wf-vars {
	width: 100%;
	display: flex;
	flex-direction: column;
	gap: 8px;

	&__row {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	&__key {
		width: 180px;
		flex-shrink: 0;
	}

	&__value {
		flex: 1;
	}
}

.wf-field-tip {
	margin-top: 2px;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
}
</style>
