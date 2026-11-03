<template>
	<el-drawer v-model="drawerVisible" size="800px" :close-on-click-modal="false" class="flow-drawer">
		<template #header>
			<div class="drawer-header">
				<span class="drawer-header-icon"><el-icon><ele-EditPen /></el-icon></span>
				<div class="drawer-header-text">
					<span class="drawer-header-title">{{ drawerTitle }}</span>
					<span class="drawer-header-sub">流程基础信息与表单绑定</span>
				</div>
			</div>
		</template>

		<el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="drawer-form">
			<!-- 基础信息 -->
			<div class="form-section">
				<div class="section-head">
					<span class="section-bar"></span>
					<span>基础信息</span>
				</div>
				<el-row :gutter="16">
					<el-col :span="12">
						<el-form-item label="流程名称" prop="name">
							<el-input v-model="form.name" placeholder="请输入流程名称" clearable maxlength="64" show-word-limit />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="流程编码" prop="code">
							<el-input v-model="form.code" placeholder="可留空自动生成" clearable maxlength="32" show-word-limit />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="分类">
							<el-input v-model="form.category" placeholder="如：人事、财务、采购" clearable maxlength="32" show-word-limit />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="图标">
							<IconSelector v-model="form.icon" :size="getGlobalComponentSize" placeholder="选择流程图标" type="all" />
						</el-form-item>
					</el-col>
				</el-row>
			</div>

			<!-- 表单配置 -->
			<div class="form-section">
				<div class="section-head">
					<span class="section-bar"></span>
					<span>表单配置</span>
				</div>
				<el-form-item label="表单类型">
					<el-radio-group v-model="form.formType" @change="handleFormTypeChange">
						<el-radio-button :value="0">自定义表单</el-radio-button>
						<el-radio-button :value="1">业务模块表单</el-radio-button>
					</el-radio-group>
				</el-form-item>
				<!-- 自定义表单选择 -->
				<el-form-item v-if="form.formType === 0" label="绑定表单">
					<div class="form-bind-row">
						<el-select
							v-model="form.formId"
							placeholder="请选择已发布表单"
							clearable
							filterable
							:loading="formSelectLoading"
							@visible-change="(visible: boolean) => visible && loadFormOptions()"
							@change="handleFormChange"
						>
							<el-option v-for="item in formOptions" :key="item.id" :label="`${item.name}${item.code ? ` (${item.code})` : ''}`" :value="item.id">
								<div class="form-option">
									<span class="form-option-name">{{ item.name }}</span>
									<el-tag size="small" type="success" effect="plain" v-if="item.isPublished">已发布</el-tag>
									<el-tag size="small" type="info" effect="plain" v-else>未发布</el-tag>
									<span class="form-option-code">{{ item.code }}</span>
								</div>
							</el-option>
						</el-select>
						<el-button v-if="form.formId" text type="danger" @click="clearFormSelection">解绑</el-button>
					</div>
				</el-form-item>
				<!-- 业务模块选择 -->
				<el-form-item v-if="form.formType === 1" label="业务模块">
					<el-select v-model="form.businessModule" placeholder="请选择业务模块" clearable filterable>
						<el-option v-for="item in businessModuleOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<!-- 已绑定表单摘要 -->
				<div v-if="selectedFormInfo && form.formType === 0" class="form-preview">
					<span class="form-preview-icon"><el-icon><ele-Document /></el-icon></span>
					<div class="form-preview-info">
						<span class="form-preview-name">{{ selectedFormInfo?.name || selectedFormInfo?.code || '未命名表单' }}</span>
						<span class="form-preview-meta">
							{{ selectedFormInfo?.code || '—' }}
							<span class="dot">·</span>
							{{ selectedFormInfo?.version ? `v${selectedFormInfo?.version}` : '无版本' }}
							<span class="dot">·</span>
							{{ selectedFormInfo?.isPublished ? '已发布' : '未发布' }}
						</span>
					</div>
				</div>
			</div>

			<!-- 其他信息 -->
			<div class="form-section">
				<div class="section-head">
					<span class="section-bar"></span>
					<span>其他信息</span>
				</div>
				<el-form-item label="业务标题模板">
					<el-input v-model="form.businessTitleTemplate" placeholder="占位符如 {BillNo}、{Warehouse.Name}，取业务数据同名字段值填充；留空则不生成业务标题" clearable maxlength="256" show-word-limit />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="form.remark" type="textarea" :rows="2" maxlength="256" show-word-limit placeholder="请输入备注" />
				</el-form-item>
			</div>

			<!-- 高级：只读 JSON -->
			<el-collapse class="advanced-collapse">
				<el-collapse-item name="advanced">
					<template #title>
						<span class="advanced-title">高级信息（只读 JSON）</span>
					</template>
					<el-form-item label="流程 JSON">
						<el-input v-model="form.flowJson" type="textarea" :autosize="{ minRows: 4, maxRows: 8 }" readonly placeholder="设计器保存后会在此展示 flowJson" />
					</el-form-item>
					<el-form-item label="表单 JSON">
						<el-input v-model="form.formJson" type="textarea" :autosize="{ minRows: 4, maxRows: 8 }" readonly placeholder="如已绑定表单，这里会展示 formJson" />
					</el-form-item>
				</el-collapse-item>
			</el-collapse>
		</el-form>

		<template #footer>
			<el-button icon="ele-CircleClose" @click="drawerVisible = false">取消</el-button>
			<el-button type="primary" icon="ele-CircleCheck" :loading="submitLoading" @click="handleSubmit">保存</el-button>
		</template>
	</el-drawer>
</template>

<script setup lang="ts" name="WorkFlowFlowDrawer">
import { ref, reactive, computed } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { createFlow, updateFlow } from '/@/api/workflow';
import { listWorkflowForms, getWorkflowFormDetail } from '/@/api/workflow';
import { getBusinessModuleOptions } from '../../utils/businessModuleConfig';
import IconSelector from '/@/components/iconSelector/index.vue';
import other from '/@/utils/other';

interface WorkflowFlowForm {
	id?: number;
	name: string;
	code?: string;
	category?: string;
	icon?: string;
	remark?: string;
	flowJson?: string;
	formJson?: string;
	formId?: number;
	formCode?: string;
	formName?: string;
	formVersion?: number;
	isFormPublished?: boolean;
	formType?: number;
	businessModule?: string;
	businessTitleTemplate?: string;
}

const emit = defineEmits(['success']);

const drawerVisible = ref(false);
const drawerTitle = ref('新建流程');
const submitLoading = ref(false);
const formRef = ref<FormInstance>();
const form = reactive<WorkflowFlowForm>({
	name: '',
	code: '',
	category: '',
	icon: '',
	remark: '',
	flowJson: '',
	formJson: '',
	formId: undefined,
	formCode: '',
	formName: '',
	formVersion: undefined,
	isFormPublished: undefined,
	formType: 0, // 0-自定义表单, 1-业务模块表单
	businessModule: undefined,
	businessTitleTemplate: '',
});

// 业务模块选项列表
const businessModuleOptions = getBusinessModuleOptions();
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});

const rules = reactive<FormRules<WorkflowFlowForm>>({
	name: [{ required: true, message: '请输入流程名称', trigger: 'blur' }],
});

interface FormOption {
	id: number;
	name: string;
	code?: string;
	isPublished?: boolean;
	version?: number;
}

interface SelectedFormInfo {
	name?: string;
	code?: string;
	version?: number;
	isPublished?: boolean;
}

const formOptions = ref<FormOption[]>([]);
const formSelectLoading = ref(false);
const selectedFormInfo = ref<SelectedFormInfo | null>(null);

const parseListResult = (res: any) => res?.data?.result ?? res?.data?.data ?? res?.data ?? [];

const loadFormOptions = async () => {
	if (formOptions.value.length) return;
	formSelectLoading.value = true;
	try {
		const res = await listWorkflowForms({ isPublished: true });
		formOptions.value = parseListResult(res) ?? [];
	} finally {
		formSelectLoading.value = false;
	}
};

const setSelectedFormInfo = (info?: SelectedFormInfo | null) => {
	selectedFormInfo.value = info ?? null;
};

const handleFormChange = async (formId?: number) => {
	if (!formId) {
		form.formId = undefined;
		form.formCode = '';
		form.formName = '';
		form.formVersion = undefined;
		form.isFormPublished = undefined;
		form.formJson = '';
		setSelectedFormInfo(null);
		return;
	}

	try {
		const res = await getWorkflowFormDetail({ id: formId });
		const detail = res?.data?.result ?? res?.data?.data ?? res?.data;
		form.formId = detail.id;
		form.formCode = detail.code;
		form.formName = detail.name;
		form.formVersion = detail.version;
		form.isFormPublished = detail.isPublished;
		form.formJson = formatJson(detail.schemaJson);
		setSelectedFormInfo({
			name: detail.name,
			code: detail.code,
			version: detail.version,
			isPublished: detail.isPublished,
		});
	} catch (error: any) {
		ElMessage.error(error?.message || '获取表单信息失败');
	}
};

const clearFormSelection = () => {
	handleFormChange(undefined);
};

// 表单类型变更处理
const handleFormTypeChange = (formType: any) => {
	if (Number(formType) === 0) {
		// 切换到自定义表单，清空业务模块
		form.businessModule = undefined;
	} else {
		// 切换到业务模块，清空表单相关信息
		form.formId = undefined;
		form.formCode = '';
		form.formName = '';
		form.formVersion = undefined;
		form.isFormPublished = undefined;
		form.formJson = '';
		setSelectedFormInfo(null);
	}
};

const resetForm = () => {
	form.id = undefined;
	form.name = '';
	form.code = '';
	form.category = '';
	form.icon = 'ele-Menu';
	form.remark = '';
	form.flowJson = '';
	form.formJson = '';
	form.formId = undefined;
	form.formCode = '';
	form.formName = '';
	form.formVersion = undefined;
	form.isFormPublished = undefined;
	form.formType = 0;
	form.businessModule = undefined;
	form.businessTitleTemplate = '';
	setSelectedFormInfo(null);
};

const formatJson = (value?: string) => {
	if (!value) return '';
	try {
		return JSON.stringify(JSON.parse(value), null, 2);
	} catch {
		return value;
	}
};

const openDrawer = (row?: WorkflowFlowForm) => {
	resetForm();
	loadFormOptions().catch(() => undefined);
	if (row) {
		Object.assign(form, row);
		form.flowJson = formatJson(row.flowJson);
		form.formJson = formatJson(row.formJson);
		setSelectedFormInfo(
			row.formId
				? {
						name: row.formName || row.formCode,
						code: row.formCode,
						version: row.formVersion,
						isPublished: row.isFormPublished,
					}
				: null
		);
		drawerTitle.value = '编辑流程';
	} else {
		drawerTitle.value = '新建流程';
	}
	drawerVisible.value = true;
};

const handleSubmit = async () => {
	await formRef.value?.validate();
	submitLoading.value = true;
	try {
		if (form.id) {
			await updateFlow(form);
			ElMessage.success('流程更新成功');
		} else {
			await createFlow(form);
			ElMessage.success('流程创建成功');
		}
		drawerVisible.value = false;
		emit('success');
	} finally {
		submitLoading.value = false;
	}
};

defineExpose({
	openDrawer,
});
</script>

<style lang="scss" scoped>
.drawer-header {
	display: flex;
	align-items: center;
	gap: 10px;
}

.drawer-header-icon {
	display: grid;
	place-items: center;
	flex-shrink: 0;
	width: 30px;
	height: 30px;
	border-radius: 9px;
	font-size: 15px;
	color: #fff;
	background: rgba(255, 255, 255, 0.18);
}

.drawer-header-text {
	display: flex;
	flex-direction: column;
	line-height: 1.35;
}

.drawer-header-title {
	font-size: 15px;
	font-weight: 600;
}

.drawer-header-sub {
	font-size: 12px;
	color: color-mix(in srgb, #fff 72%, transparent);
}

.drawer-form {
	padding: 4px 4px 0;
}

.form-section {
	margin-bottom: 6px;
}

.section-head {
	display: flex;
	align-items: center;
	gap: 8px;
	margin: 4px 0 14px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.section-bar {
	width: 3px;
	height: 13px;
	border-radius: 2px;
	background: var(--el-color-primary);
}

.drawer-form :deep(.el-col) {
	margin-bottom: 2px;
}

.drawer-form :deep(.el-form-item) {
	margin-bottom: 16px;
}

.drawer-form :deep(.el-input),
.drawer-form :deep(.el-select) {
	width: 100%;
}

.form-bind-row {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;

	.el-select {
		flex: 1;
		min-width: 0;
	}

	.el-button {
		flex-shrink: 0;
	}
}

.form-option {
	display: flex;
	align-items: center;
	gap: 6px;
}

.form-option-name {
	flex: 1;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
}

.form-option-code {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}

.form-preview {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px 12px;
	margin: -4px 0 18px;
	border-radius: 10px;
	background: color-mix(in srgb, var(--el-color-primary) 6%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 16%, transparent);
}

.form-preview-icon {
	display: grid;
	place-items: center;
	flex-shrink: 0;
	width: 32px;
	height: 32px;
	border-radius: 8px;
	font-size: 16px;
	color: var(--el-color-primary);
	background: var(--el-bg-color);
}

.form-preview-info {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
}

.form-preview-name {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.form-preview-meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);

	.dot {
		margin: 0 4px;
		color: var(--el-text-color-placeholder);
	}
}

.advanced-collapse {
	margin-top: 4px;
	border-top: 1px solid var(--el-border-color-lighter);
	border-bottom: none;

	:deep(.el-collapse-item__header) {
		height: 42px;
		border-bottom: none;
		font-size: 13px;
		color: var(--el-text-color-secondary);
	}

	:deep(.el-collapse-item__wrap) {
		border-bottom: none;
	}
}

.advanced-title {
	font-weight: 500;
	color: var(--el-text-color-secondary);
}
</style>
