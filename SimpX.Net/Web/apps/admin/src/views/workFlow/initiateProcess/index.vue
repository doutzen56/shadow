<template>
	<div class="workFlow-initiate-container">
		<!-- 顶部头图 -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">发起流程</h2>
					<p class="page-hero__desc">从流程中心选择并一键发起工作流；支持按名称、分类与表单类型快速筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">流程总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.published }}</span>
							<span class="page-hero__stat-k">已发布</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.custom }}</span>
							<span class="page-hero__stat-k">自定义表单</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.business }}</span>
							<span class="page-hero__stat-k">业务表单</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：条件与操作同行（全站标准 query-toolbar） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="流程名称 / 编码" clearable @keyup.enter.native="handleQuery()" />
				</el-form-item>
				<el-form-item label="分类">
					<el-input v-model="state.queryParams.category" class="query-input" placeholder="请输入分类" clearable @keyup.enter.native="handleQuery()" />
				</el-form-item>
				<el-form-item label="流程类型">
					<el-radio-group v-model="state.flowFilterType">
						<el-radio-button label="custom" value="custom">自定义表单</el-radio-button>
						<el-radio-button label="business" value="business">业务表单</el-radio-button>
						<el-radio-button label="all" value="all">全部</el-radio-button>
					</el-radio-group>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery()"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !filteredFlows.length" description="暂无可发起的流程" :image-size="120" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in filteredFlows" :key="item.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable is-primary"
							:class="{ 'is-active': state.selection && state.selection.id === item.id }"
							@click="handleFlowCardClick(item)"
						>
							<!-- 不放 entity-card__watermark：图标可能是方框轮廓（默认 ele-Cherry / 业务表单用 ele-DataAnalysis），
							     74px 压在右上角正好把类型标签框进去，看着像标签外面套了个框 -->
							<div class="entity-card__head">
								<div class="entity-card__icon">
									<SvgIcon :name="getFlowCardIcon(item)" :size="20" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="item.name">{{ item.name }}</div>
									<div class="entity-card__sub" :title="item.code || ''">{{ item.code || '未设置编码' }}</div>
								</div>
								<!-- 自定义表单 / 业务表单是这张卡唯一的分类信息，交给右上角标签表达 -->
								<el-tag size="small" effect="light" :type="getFlowTypeTagType(item)">{{ getFlowTypeText(item) }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="metric-group">
									<div class="metric-item">
										<span>最新版本</span><b>{{ item.latestVersion ? 'v' + item.latestVersion : '未发布' }}</b>
									</div>
									<div class="metric-item">
										<span>所属模块</span><b :title="item.businessModule || '通用流程'">{{ item.businessModule || '通用流程' }}</b>
									</div>
									<div class="metric-item">
										<span>绑定表单</span><b :title="item.formName || '未绑定'">{{ item.formName || '未绑定' }}</b>
									</div>
								</div>

								<div class="entity-card__meta">
									<span><el-icon><ele-Folder /></el-icon>{{ item.category || '未分类' }}</span>
									<span><el-icon><ele-Timer /></el-icon>发布：{{ item.latestPublishedTime || '未知' }}</span>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ item.createTime || '创建时间未知' }}</span>
								<div class="entity-card__actions">
									<el-button type="primary" size="small" :disabled="!canStartFlow(item)" @click.stop="handleFlowPrimaryAction(item)">
										<el-icon><ele-Promotion /></el-icon>
										<span>{{ getFlowPrimaryActionText(item) }}</span>
									</el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				v-if="state.tableParams.total > 0"
				class="table-pager"
				v-model:current-page="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize"
				:page-sizes="[10, 20, 50]"
				:total="state.tableParams.total"
				@current-change="handleCurrentChange"
				@size-change="handleSizeChange"
				layout="total, sizes, prev, pager, next, jumper"
				background
			/>
		</el-card>

		<el-dialog v-model="state.startDialogVisible" width="900px" draggable destroy-on-close :close-on-click-modal="false" @closed="resetFormDataOnly">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Promotion /></el-icon>
					<span> {{ state.selection && (state.selection.name || state.selection.flowName) ? `发起流程 - ${state.selection.name || state.selection.flowName}` : '发起流程（自定义表单）' }} </span>
				</div>
			</template>

			<el-descriptions v-if="state.selection" :column="2" border class="mb10">
				<el-descriptions-item label="流程名称">{{ state.selection.name }}</el-descriptions-item>
				<el-descriptions-item label="流程编码">{{ state.selection.code || '-' }}</el-descriptions-item>
				<el-descriptions-item label="分类">{{ state.selection.category || '-' }}</el-descriptions-item>
				<el-descriptions-item label="版本">
					<span v-if="state.selection.latestVersion">v{{ state.selection.latestVersion }}</span>
					<span v-else>未发布</span>
				</el-descriptions-item>
			</el-descriptions>

			<el-divider border-style="dashed">表单信息</el-divider>

			<el-form ref="startFormRef" :model="state.startForm" :rules="startRules" label-width="auto" label-position="top">
				<el-form-item label="" prop="formData">
			<template v-if="formSchema">
				<FormRender
					:key="`form-${state.startForm.definitionId}-${formMeta.version || '0'}`"
					ref="formRenderRef"
					:schema-json="formSchema"
					:form-data="state.formRenderData"
					:form-version="formMeta.version"
					:disabled="false"
					:show-meta="true"
				/>
			</template>
					<template v-else>
						<el-alert type="info" :closable="false" show-icon title="当前流程未绑定表单，仍可手动填写 JSON 数据" style="margin-bottom: 8px" />
						<el-input v-model="state.startForm.formData" type="textarea" :autosize="{ minRows: 6, maxRows: 10 }" placeholder='{"reason":"请假","days":3}' />
					</template>
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.startForm.remark" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-RefreshLeft" @click="resetStartForm">重置</el-button>
				<el-button type="primary" icon="ele-Promotion" :loading="state.submitLoading" @click="submitStart" :disabled="!state.startForm.definitionId"> 发起流程 </el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowInitiateProcess">
import { reactive, ref, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useRouter, useRoute } from 'vue-router';
import { getFlowPage, startWorkflowInstance } from '/@/api/workflow';
import SvgIcon from '/@/components/svgIcon/index.vue';
import FormRender from '../components/FormRender.vue';
import { resolveFlowForm, type FcSchema } from '/@/views/workFlow/utils/flowForm';

const router = useRouter();
const route = useRoute();

// 兼容旧业务模块逻辑的占位实现
function getBusinessModuleOptions() {
	return [];
}

async function loadBusinessData(_businessModule: string, _businessId: string) {
	// 自定义表单发起页面已不再直接加载业务数据
}

const businessModuleOptions = getBusinessModuleOptions();

const isFromBusinessPage = computed(() => {
	return !!(route.query.businessModule && route.query.businessId);
});

interface WorkflowDefinitionItem {
	id: number;
	flowId?: number;
	flowCode?: string;
	workflowCode?: string;
	name?: string;
	flowName?: string;
	category?: string;
	version?: number;
	latestVersion?: number;
	latestDefinitionId?: number;
	isActive?: boolean;
	isLatest?: boolean;
	publishedTime?: string;
	flowJson?: string;
	businessModule?: string;
	formName?: string;
	createTime?: string;
	[key: string]: any;
}

const queryForm = ref();
const startFormRef = ref();
const formRenderRef = ref();

const state = reactive({
	loading: false,
	submitLoading: false,
	tableData: [] as WorkflowDefinitionItem[],
	formRenderData: {} as Record<string, any>,
	selection: {} as WorkflowDefinitionItem,
	startDialogVisible: false,
	queryParams: {
		keyword: '',
		category: '',
		isActive: true,
	},
	flowFilterType: 'all' as 'custom' | 'business' | 'all',
	tableParams: {
		page: 1,
		pageSize: 10,
		total: 0,
	},
	startForm: {
		definitionId: null as number | null,
		workflowCode: '',
		flowName: '',
		formData: '',
		remark: '',
		businessModule: '' as string | undefined,
		businessId: '' as string | undefined,
	},
});

const startRules = {
	formData: [
		{
			validator: (_: any, value: string, callback: any) => {
				if (formSchema.value) {
					callback();
					return;
				}
				if (!value) {
					callback();
					return;
				}
				try {
					JSON.parse(value);
					callback();
				} catch (error) {
					callback(new Error('请填写合法的 JSON 格式'));
				}
			},
			trigger: 'blur',
		},
	],
};

onMounted(async () => {
	if (route.query.businessModule) {
		state.startForm.businessModule = route.query.businessModule as string;
	}
	if (route.query.businessId) {
		state.startForm.businessId = route.query.businessId as string;
		if (route.query.businessModule && route.query.businessId) {
			await loadBusinessData(route.query.businessModule as string, route.query.businessId as string);
		}
	}
	if (route.query.workflowCode) {
		// 自动选择流程逻辑
	}
	handleQuery();
});

const handleBusinessModuleChange = () => {
	state.startForm.businessId = '';
	state.formRenderData = {};
	if (isFromBusinessPage.value) {
		state.tableParams.page = 1;
		handleQuery();
	}
};

const handleQuery = async () => {
	state.loading = true;
	try {
		const params: any = {
			keyword: state.queryParams.keyword || undefined,
			category: state.queryParams.category || undefined,
			status: 1,
			page: state.tableParams.page,
			pageSize: state.tableParams.pageSize,
		};

		const res = await getFlowPage(params);
		const items = res.data.result?.items ?? res.data?.data?.items ?? res.data?.items ?? [];
		const total = res.data.result?.total ?? res.data?.data?.total ?? res.data?.total ?? 0;

		state.tableData = items.filter((x: any) => x.latestDefinitionId);
		state.tableParams.total = total;
	} finally {
		state.loading = false;
	}
};

const resetQuery = () => {
	state.queryParams.keyword = '';
	state.queryParams.category = '';
	state.tableParams.page = 1;
	handleQuery();
};

const handleSizeChange = (val: number) => {
	state.tableParams.pageSize = val;
	handleQuery();
};

const handleCurrentChange = (val: number) => {
	state.tableParams.page = val;
	handleQuery();
};

const formSchema = ref<FcSchema | null>(null);
const formMeta = ref<{ version?: number }>({});

const isBusinessFormFlow = (item: WorkflowDefinitionItem) => {
	return !!item.businessModule;
};

const isCustomFormFlow = (item: WorkflowDefinitionItem) => {
	return !isBusinessFormFlow(item);
};

const filteredFlows = computed<WorkflowDefinitionItem[]>(() => {
	let list = state.tableData || [];

	if (state.flowFilterType === 'custom') {
		list = list.filter((x) => isCustomFormFlow(x));
	} else if (state.flowFilterType === 'business') {
		list = list.filter((x) => isBusinessFormFlow(x));
	}

	return list;
});

const heroStats = computed(() => {
	const list = state.tableData || [];
	const published = list.filter((x: any) => x.latestVersion || x.version).length;
	const custom = list.filter((x: any) => !x.businessModule).length;
	const business = list.filter((x: any) => !!x.businessModule).length;
	return { total: state.tableParams.total, published, custom, business };
});

const getFlowTypeText = (item: WorkflowDefinitionItem) => {
	return isBusinessFormFlow(item) ? '业务表单' : '自定义表单';
};

const getFlowTypeTagType = (item: WorkflowDefinitionItem) => {
	return isBusinessFormFlow(item) ? 'warning' : 'primary';
};

const getFlowPrimaryActionText = (item: WorkflowDefinitionItem) => {
	return isBusinessFormFlow(item) ? '在业务页面发起' : '发起流程';
};

const canStartFlow = (item: WorkflowDefinitionItem) => {
	if (isBusinessFormFlow(item)) return false;
	return !!(item.latestVersion || item.version || item.latestDefinitionId);
};

// 新增辅助方法
const getFlowTypeIcon = (item: WorkflowDefinitionItem) => {
	return isBusinessFormFlow(item) ? 'ele-DataAnalysis' : 'ele-Cherry';
};

const getFlowCardIcon = (item: WorkflowDefinitionItem) => {
	return item.icon || getFlowTypeIcon(item);
};

const handleFlowCardClick = (item: WorkflowDefinitionItem) => {
	openStartDialogForFlow(item);
};

const handleFlowPrimaryAction = (item: WorkflowDefinitionItem) => {
	openStartDialogForFlow(item);
};

const openStartDialogForFlow = async (item: WorkflowDefinitionItem) => {
	if (isBusinessFormFlow(item)) {
		ElMessage.info('业务表单流程请在对应业务页面中发起，本页面仅支持自定义表单流程。');
		return;
	}
	await selectDefinition(item);
	if (state.selection && state.selection.id === item.id) {
		state.startDialogVisible = true;
	}
};



const fetchFlowForm = async (flowId?: number | null) => {
	formSchema.value = null;
	formMeta.value = {};
	if (!flowId) {
		console.warn('[fetchFlowForm] flowId 为空');
		return;
	}
	try {
		const info = await resolveFlowForm(flowId);
		formSchema.value = info.schema;
		formMeta.value = { version: info.version };
	} catch (error: any) {
		console.error('[fetchFlowForm] 获取流程表单信息失败', error);
		ElMessage.error(error?.message || '获取流程表单信息失败');
		formSchema.value = null;
	}
};

const selectDefinition = async (row: WorkflowDefinitionItem) => {
	const isPublished = !!(row.version || row.latestVersion || row.latestDefinitionId);
	if (!isPublished) {
		ElMessage.warning('该流程尚未发布，无法发起');
		return;
	}
	if (isBusinessFormFlow(row)) {
		ElMessage.info('业务表单流程请在对应业务页面中发起，本页面仅支持自定义表单流程。');
		return;
	}
	state.selection = row;
	const definitionId = (row as any).latestDefinitionId ?? row.id;
	state.startForm.definitionId = definitionId;
	state.startForm.workflowCode = row.workflowCode ?? row.flowCode ?? '';
	state.startForm.flowName = row.name || row.flowName || '';
	state.startForm.formData = '';
	await fetchFlowForm(row.id);

	if (state.startForm.businessModule && state.startForm.businessId) {
		await loadBusinessData(state.startForm.businessModule, state.startForm.businessId);
	}
};


const resetStartForm = () => {
	resetFormDataOnly();
};

const submitStart = async () => {
	if (!state.startForm.definitionId) {
		ElMessage.warning('请先选择流程');
		return;
	}
	await startFormRef.value?.validate();
	state.submitLoading = true;
	try {
		const payload: any = {
			definitionId: state.startForm.definitionId,
			remark: state.startForm.remark || undefined,
		};
		if (state.startForm.workflowCode) payload.workflowCode = state.startForm.workflowCode;

		if (formSchema.value && formRenderRef.value) {
			try {
				let formDataObj: any = {};

				if (typeof formRenderRef.value.getFormData === 'function') {
					const result = formRenderRef.value.getFormData();
					if (result && typeof result.then === 'function') {
						formDataObj = await result;
					} else {
						formDataObj = result || {};
					}
				} else if (formRenderRef.value.formData) {
					formDataObj = formRenderRef.value.formData;
				} else if (state.formRenderData && Object.keys(state.formRenderData).length > 0) {
					formDataObj = state.formRenderData;
				}

				if (formDataObj && typeof formDataObj.then === 'function') {
					formDataObj = await formDataObj;
				}

				if (Array.isArray(formDataObj)) {
					if (state.formRenderData && typeof state.formRenderData === 'object' && !Array.isArray(state.formRenderData)) {
						formDataObj = state.formRenderData;
					} else {
						formDataObj = {};
					}
				}

				if (!formDataObj || typeof formDataObj !== 'object' || Array.isArray(formDataObj)) {
					formDataObj = {};
				}

				payload.formData = JSON.stringify(formDataObj);
			} catch (error) {
				console.error('[发起流程] 获取表单数据失败:', error);
				payload.formData = '{}';
			}
		} else if (state.startForm.formData) {
			payload.formData = state.startForm.formData;
		} else if (formSchema.value) {
			payload.formData = '{}';
		}

		const res = await startWorkflowInstance(payload);
		const instanceCode = res.data?.result?.instanceCode || res.data?.data?.instanceCode;

		if (instanceCode) {
			state.startDialogVisible = false;
			ElMessageBox.confirm(
				`<div style="text-align: left; line-height: 1.8;">
					<p style="margin: 0 0 12px 0; font-size: 14px; color: #303133;"><strong>流程已成功发起！</strong></p>
					<p style="margin: 0 0 8px 0; font-size: 13px; color: #606266;">流程实例编码：<span style="color: #409eff; font-weight: 600;">${instanceCode}</span></p>
					<p style="margin: 8px 0 0 0; font-size: 12px; color: #909399;">请选择下一步操作：</p>
				</div>`,
				'发起成功',
				{
					confirmButtonText: '查看我发起的流程',
					cancelButtonText: '继续发起',
					distinguishCancelAndClose: true,
					type: 'success',
					showClose: false,
					dangerouslyUseHTMLString: true,
				}
			)
				.then(() => {
					router.push('/workFlow/myInitiatedTasks');
				})
				.catch((action) => {
					if (action === 'cancel') {
						resetFormDataOnly();
						ElMessage.info('表单已清空，您可以继续填写信息发起新流程');
					}
				});
		} else {
			state.startDialogVisible = false;
			ElMessage.success('流程已发起');
			resetFormDataOnly();
		}
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.message || '发起流程失败');
		}
	} finally {
		state.submitLoading = false;
	}
};

const resetFormDataOnly = () => {
	state.startForm.formData = '';
	state.startForm.remark = '';
	state.formRenderData = {};
	startFormRef.value?.clearValidate();
};
</script>

<style scoped>
.workFlow-initiate-container > .query-toolbar {
	margin-top: 12px;
}

/* 卡片区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */

/* 选中态：发起流程是"先选卡、再填单"的两步操作，选中的那张必须留得住高亮。
 * 取 Element 的浅色档位而不是 color-mix —— 老内核（Chromium 102）读不了 color-mix，
 * 会在计算值阶段整条作废退回透明，选中态会彻底看不见 */
.entity-card.is-active {
	border-color: var(--el-color-primary-light-5);
	box-shadow: 0 0 0 1px var(--el-color-primary-light-5);
}

.mb10 {
	margin-bottom: 10px;
}

.mt10 {
	margin-top: 10px;
}

:deep(.el-input),
:deep(.el-select) {
	width: 100%;
}

.form-meta {
	margin-left: 20px;
}
</style>
