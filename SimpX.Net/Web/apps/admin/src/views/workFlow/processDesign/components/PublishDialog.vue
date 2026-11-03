<template>
	<el-dialog v-model="dialogVisible" width="520px" draggable :close-on-click-modal="false" class="publish-dialog" @closed="resetForm">
		<template #header>
			<div class="pub-header">
				<el-icon size="16"><ele-Promotion /></el-icon>
				<span>{{ dialogTitle }}</span>
			</div>
		</template>

		<!-- 流程摘要 -->
		<div class="pub-flow">
			<span class="pub-flow-icon"><el-icon><ele-Share /></el-icon></span>
			<div class="pub-flow-info">
				<span class="pub-flow-name" :title="form.flowName">{{ form.flowName }}</span>
				<span class="pub-flow-code">{{ form.flowCode || '未设置流程编码' }}</span>
			</div>
			<span class="pub-flow-version">本次生成 v{{ nextVersion }}</span>
		</div>

		<el-alert class="pub-tip" type="info" :closable="false" show-icon>
			<template #title>发布会基于当前流程设计生成新版本</template>
			<div class="pub-tip-desc">启用后注册到工作流引擎，新发起的流程将使用该版本。</div>
		</el-alert>

		<el-form ref="formRef" :model="form" :rules="rules" label-width="auto" label-position="top" class="pub-form">
			<el-form-item label="工作流编码" prop="workflowCode">
				<el-input v-model="form.workflowCode" placeholder="留空自动生成：WF_流程编码_时间戳" clearable maxlength="64" show-word-limit />
			</el-form-item>
			<el-form-item label="是否立即启用" prop="isActive">
				<div class="pub-switch-row">
					<el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" />
					<span class="pub-switch-hint">停用状态下只生成版本，不注册到工作流引擎</span>
				</div>
			</el-form-item>
			<el-form-item label="备注">
				<el-input v-model="form.remark" type="textarea" :rows="3" maxlength="256" show-word-limit placeholder="填写本次发布的变更说明（可选）" />
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button icon="ele-CircleClose" @click="dialogVisible = false">取消</el-button>
			<el-button type="primary" icon="ele-Promotion" :loading="submitLoading" @click="handleSubmit">发布</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts" name="WorkFlowPublishDialog">
import { computed, ref, reactive } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { registerWorkflow } from '/@/api/workflow';

interface PublishForm {
	flowId: number;
	flowName: string;
	flowCode?: string;
	workflowCode?: string;
	isActive: boolean;
	remark?: string;
}

/** 打开弹窗时传入的流程信息 */
interface PublishFlow {
	id: number;
	name: string;
	code?: string;
	flowJson?: string;
	latestVersion?: number;
}

const emit = defineEmits(['success']);

const dialogVisible = ref(false);
const dialogTitle = ref('发布流程');
const submitLoading = ref(false);
const formRef = ref<FormInstance>();
const latestVersion = ref(0);
const form = reactive<PublishForm>({
	flowId: 0,
	flowName: '',
	flowCode: '',
	workflowCode: '',
	isActive: true,
	remark: '',
});

/** 发布后生成的版本号：发布接口按当前最大版本 +1 */
const nextVersion = computed(() => latestVersion.value + 1);

const rules = reactive<FormRules<PublishForm>>({});

const resetForm = () => {
	form.flowId = 0;
	form.flowName = '';
	form.flowCode = '';
	form.workflowCode = '';
	form.isActive = true;
	form.remark = '';
	latestVersion.value = 0;
};

const openDialog = (flow: PublishFlow) => {
	if (!flow.flowJson || flow.flowJson.trim() === '' || flow.flowJson === '{}') {
		ElMessage.warning('请先在设计器中保存流程设计');
		return;
	}
	resetForm();
	form.flowId = flow.id;
	form.flowName = flow.name;
	form.flowCode = flow.code;
	latestVersion.value = Number(flow.latestVersion ?? 0);
	dialogVisible.value = true;
};

const handleSubmit = async () => {
	await formRef.value?.validate();
	submitLoading.value = true;
	try {
		const res = await registerWorkflow({
			flowId: form.flowId,
			workflowCode: form.workflowCode || undefined,
			isActive: form.isActive,
			remark: form.remark || undefined,
		});
		const workflowCode = res.data?.result || res.data?.data || '未知';
		ElMessage.success(`发布成功，工作流编码：${workflowCode}`);
		dialogVisible.value = false;
		emit('success');
	} catch (error: any) {
		// 错误已在拦截器中处理
	} finally {
		submitLoading.value = false;
	}
};

defineExpose({
	openDialog,
});
</script>

<style lang="scss" scoped>
.pub-header {
	display: flex;
	align-items: center;
	gap: 6px;
}

/* 流程摘要卡 */
.pub-flow {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 14px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
}

.pub-flow-icon {
	display: grid;
	place-items: center;
	flex-shrink: 0;
	width: 38px;
	height: 38px;
	border-radius: 10px;
	font-size: 18px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 18%, transparent);
}

.pub-flow-info {
	display: flex;
	flex-direction: column;
	gap: 2px;
	flex: 1;
	min-width: 0;
}

.pub-flow-name {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.pub-flow-code {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.pub-flow-version {
	flex-shrink: 0;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

.pub-tip {
	margin: 12px 0 4px;

	.pub-tip-desc {
		font-size: 12px;
		line-height: 1.6;
		color: var(--el-text-color-secondary);
	}
}

.pub-form {
	margin-top: 12px;

	:deep(.el-input),
	:deep(.el-select) {
		width: 100%;
	}
}

.pub-switch-row {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;
}

.pub-switch-hint {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}
</style>
