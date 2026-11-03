<template>
	<div class="form-render-container" v-loading="state.loading" element-loading-text="加载中...">
		<div v-if="!state.loading && !hasForm" class="empty-wrapper">
			<el-empty description="该流程未绑定表单" />
		</div>
		<div v-else-if="!state.loading && hasForm" class="form-wrapper">
			<el-card shadow="hover" class="form-card">
				<template #header>
					<div class="form-header">
						<span class="form-title">{{ state.flowName || '填写表单' }}</span>
						<el-tag v-if="formMeta.version" type="info">表单版本：v{{ formMeta.version }}</el-tag>
					</div>
				</template>
				<FcRenderer
					ref="vFormRef"
					:key="`form-${state.flowId}-${formMeta.version || '0'}`"
					:schema="formSchema"
					:data="state.formRenderData"
					:disabled="state.readonly"
					@change="handleFormChange"
				/>
			</el-card>
			<div v-if="!state.readonly" class="form-actions">
				<el-button @click="handleCancel">取消</el-button>
				<el-button type="primary" :loading="state.submitting" @click="handleSubmit">提交</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="WorkFlowFormRender">
import { reactive, ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import FcRenderer from '/@/components/formCreate/FcRenderer.vue';

import { startWorkflowInstance } from '/@/api/workflow';
import { resolveFlowForm, type FcSchema } from '/@/views/workFlow/utils/flowForm';

const route = useRoute();
const router = useRouter();
const vFormRef = ref();

const state = reactive({
	loading: false,
	submitting: false,
	flowId: '',
	flowName: '',
	definitionId: '',
	workflowCode: '',
	formRenderData: {} as Record<string, any>,
	remark: '', // 备注字段
	readonly: false, // 只读模式
});

// 表单设计 JSON（form-create 形态 { rule, options }）
const formSchema = ref<FcSchema | null>(null);
const formMeta = ref<{ version?: number }>({});

// 是否存在可渲染的表单
const hasForm = computed(() => (formSchema.value?.rule.length ?? 0) > 0);

// 检测是否在 webview 环境中
const isInWebview = () => {
	try {
		if (window.parent !== window) return true;
		if ((window as any).uni || (window as any).plus) return true;
		if ((window as any).webkit?.messageHandlers || (window as any).Android) return true;
		const ua = navigator.userAgent || '';
		if (ua.includes('uni-app') || ua.includes('Html5Plus')) return true;
		return false;
	} catch (e) {
		return true;
	}
};

// 加载流程表单
const fetchFlowForm = async (flowId?: number | null) => {
	formSchema.value = null;
	formMeta.value = {};
	if (!flowId) {
		console.warn('[fetchFlowForm] flowId 为空，无法获取表单');
		return;
	}
	try {
		const info = await resolveFlowForm(flowId);
		formSchema.value = info.schema;
		formMeta.value = { version: info.version };
		state.flowName = info.flowName;
		state.definitionId = info.definitionId;
		state.workflowCode = info.workflowCode;
	} catch (error: any) {
		console.error('[fetchFlowForm] 获取流程表单信息失败', error);
		ElMessage.error(error?.message || '获取流程表单信息失败');
		formSchema.value = null;
	}
};

// 表单数据变化事件处理（v-model 已实时同步，这里仅做快照）
const handleFormChange = (formData: any) => {
	if (formData && typeof formData === 'object' && !Array.isArray(formData)) {
		state.formRenderData = { ...formData };
	}
};

// 提交表单
const handleSubmit = async () => {
	if (!state.definitionId) {
		ElMessage.error('流程定义ID不存在，无法提交');
		return;
	}

	if (!vFormRef.value) {
		ElMessage.warning('表单未初始化');
		return;
	}

	// 校验表单
	try {
		const valid = await vFormRef.value.validate();
		if (valid === false) {
			ElMessage.warning('请完整填写表单');
			return;
		}
	} catch (e) {
		ElMessage.warning('请完整填写表单');
		return;
	}

	state.submitting = true;
	try {
		const payload: any = {
			definitionId: Number(state.definitionId),
			remark: state.remark || undefined,
		};
		if (state.workflowCode) payload.workflowCode = state.workflowCode;

		// 取表单数据（form-create v-model 实时同步）
		const formDataObj = vFormRef.value.getFormData?.() ?? {};
		payload.formData = JSON.stringify(formDataObj ?? {});

		const res = await startWorkflowInstance(payload);
		const instanceCode = res.data?.result?.instanceCode || res.data?.data?.instanceCode;

		vFormRef.value.resetForm?.();
		state.formRenderData = {};

		if (instanceCode) {
			ElMessageBox.alert(
				`<div style="text-align: left; line-height: 1.8;">
					<p style="margin: 0 0 12px 0; font-size: 14px; color: #303133;"><strong>流程已成功发起！</strong></p>
					<p style="margin: 0; font-size: 13px; color: #606266;">流程实例编码：<span style="color: #409eff; font-weight: 600;">${instanceCode}</span></p>
				</div>`,
				'发起成功',
				{
					type: 'success',
					showClose: false,
					dangerouslyUseHTMLString: true,
					confirmButtonText: '确定',
				}
			);
		} else {
			ElMessage.success('流程已发起');
		}
	} catch (error: any) {
		if (error !== 'cancel') {
			const errorMessage = error?.response?.data?.message || error?.data?.message || error?.message || '提交失败';
			ElMessage.error(errorMessage);
		}
	} finally {
		state.submitting = false;
	}
};

// 取消
const handleCancel = () => {
	window.history.back();
};

// 初始化
onMounted(async () => {
	const flowId = route.query.flowId as string;
	const definitionId = route.query.definitionId as string;
	const workflowCode = route.query.workflowCode as string;
	const formDataStr = route.query.formData as string;
	const readonly = route.query.readonly as string;

	const token = route.query._token as string;
	const refreshToken = route.query._refreshToken as string;

	if (token && isInWebview()) {
		try {
			const { Local } = await import('/@/utils/storage');
			Local.set('access-token', token);
			if (refreshToken) Local.set('x-access-token', refreshToken);
		} catch (error) {
			console.error('[onMounted] 存储Token失败:', error);
		}
	}

	state.readonly = readonly === 'true' || readonly === '1';

	if (flowId) {
		state.flowId = flowId;
		state.loading = true;
		await fetchFlowForm(Number(flowId));
		state.loading = false;
	}

	if (definitionId) state.definitionId = String(definitionId);
	if (workflowCode) state.workflowCode = decodeURIComponent(workflowCode);

	if (formDataStr) {
		try {
			const parsedFormData = JSON.parse(decodeURIComponent(formDataStr));
			state.formRenderData = parsedFormData;
		} catch (error) {
			console.error('解析预填充表单数据失败:', error);
		}
	}
});
</script>

<style scoped>
.form-render-container {
	min-height: 100vh;
	background: #f5f7fa;
}

.loading-wrapper,
.empty-wrapper {
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 400px;
}

.form-wrapper {
	max-width: 1200px;
	margin: 0 auto;
}

.form-card {
	margin-bottom: 20px;
}

.form-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.form-title {
	font-size: 16px;
	font-weight: 600;
	color: #303133;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 20px;
	background: #fff;
	border-radius: 4px;
	box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}
</style>
