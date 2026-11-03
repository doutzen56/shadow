<template>
	<div class="workFlow-ccTasks-container">
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
					<h2 class="page-hero__title">抄送任务</h2>
					<p class="page-hero__desc">这里汇集所有抄送给你知晓的流程事项；支持按关键字与优先级快速筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">					<span class="page-hero__stat-v is-on">{{ heroStats.cc }}</span>					<span class="page-hero__stat-k">抄送给我</span>			</div>
					</div>
				</div>
			</div>
		</div>

		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
					<el-form-item label="关键字">
						<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="标题 / 流程名称 / 实例编码" clearable @keyup.enter.native="handleQuery(true)" />
					</el-form-item>
					<el-form-item label="优先级">
						<el-select v-model="state.queryParams.priority" class="query-select" placeholder="全部优先级" clearable>
							<el-option label="高" :value="1" />
							<el-option label="中" :value="2" />
							<el-option label="低" :value="3" />
						</el-select>
					</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
			</div>
		</div>
<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #header_priority>
					<span class="wf-priority-header">
						<span>优先级</span>
						<el-tooltip content="优先级定义：普通审批和抄送默认中；驳回重办、驳回到发起人等需要立即处理的任务为高；低为预留值，当前系统不会自动生成；转办继承原任务优先级。" placement="top">
							<el-icon class="wf-priority-help-icon"><ele-InfoFilled /></el-icon>
						</el-tooltip>
					</span>
				</template>
			<template #row_approvalMode="{ row }">
				<span>{{ approvalModeText(row.approvalMode) }}</span>
			</template>
			<template #row_priority="{ row }">
				<span class="wf-status" :class="`is-${priorityInfo(row.priority).tone}`">
					<i class="wf-status__dot"></i>{{ priorityInfo(row.priority).text }}
				</span>
			</template>
				<template #row_createTime="{ row }">
					<span v-if="row.createTime">{{ formatDateTime(row.createTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_buttons="{ row }">
					<el-button text type="info" icon="ele-InfoFilled" @click="viewDetail(row)">详情</el-button>
				</template>
			</vxe-grid>
			</div>
		</el-card>

		<!-- 详情对话框：仅查看，不可操作 -->
		<el-dialog v-model="detailDialog.visible" width="1010px" class="wf-detail" draggable :close-on-click-modal="false" v-if="detailDialog.data">
			<template #header>
				<DetailHead title="抄送详情" :name="detailDialog.data.title" :code="detailDialog.data.instanceCode" :sub="detailDialog.data.flowName">
					<template #side>
						<span class="wf-status is-info"><i class="wf-status__dot"></i>已抄送</span>
						<span class="wf-status" :class="`is-${priorityInfo(detailDialog.data.priority).tone}`">
							<i class="wf-status__dot"></i>优先级 {{ priorityInfo(detailDialog.data.priority).text }}
						</span>
					</template>
				</DetailHead>
			</template>

			<div class="wf-detail__body">
				<el-tabs v-model="detailDialog.activeTab" @tab-click="handleTabClick">
					<el-tab-pane label="基本信息" name="info">
						<div class="wf-summary">
							<div class="wf-summary__cell" v-for="item in baseInfoItems" :key="item.label">
								<span class="wf-summary__k">{{ item.label }}</span>
								<span class="wf-summary__v" :title="item.text">{{ item.text || '--' }}</span>
							</div>
						</div>

						<!-- 业务模块详情 -->
						<BusinessModuleDetail :business-module="detailDialog.businessModule" :business-id="detailDialog.businessId" />

						<div class="wf-form-block">
							<div class="wf-form-block__hd"><i class="wf-form-block__bar"></i>表单信息</div>
							<FormRender
								:schema-json="detailDialog.formRecord?.schemaJson"
								:form-data="detailDialog.formRecord?.formData"
								:form-version="detailDialog.formRecord?.formVersion"
								:submit-time="detailDialog.formRecord?.submitTime"
								:disabled="true"
								:show-meta="true"
								:loading="detailDialog.loading"
							/>
						</div>
					</el-tab-pane>
					<el-tab-pane label="全部节点" name="allNodes">
						<FlowNodesTab :data="allNodesData" :loading="loadingAllNodes" />
					</el-tab-pane>
				</el-tabs>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowCcTasks">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
import { computed, onMounted, reactive, ref } from 'vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { formatDateTime } from '/@/utils/format';

import FormRender from '../components/FormRender.vue';
import BusinessModuleDetail from '../components/BusinessModuleDetail.vue';
import DetailHead from '../components/DetailHead.vue';
import FlowNodesTab from '../components/flowNodesTab.vue';
import { approvalModeText, priorityInfo } from '../utils/workflowDict';
import { fetchInstanceFormRecord, parsePagedResult, useAllFlowNodes } from '../utils/taskList';
import { getTodos, GetTodoStats } from '/@/api/workflow';

interface CcTodo {
	id: number;
	title: string;
	flowName: string;
	stepName?: string;
	starterName: string;
	priority?: number;
	status: number;
	createTime?: string;
	instanceCode: string;
	stepCode: string;
	todoType?: string;
}

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		keyword: '',
		priority: undefined as number | undefined,
	},
});

// 头图统计：取后端 /todos/stats 的全量真实数字（此前按当前页拼凑，翻页即变，属假指标）
const heroStats = reactive({ cc: 0 });

const loadHeroStats = async () => {
	try {
		const res = await GetTodoStats({});
		const stats = res.data?.result;
		if (!stats) return;
		heroStats.cc = stats.ccCount ?? 0;
	} catch (error) {
		console.error('获取抄送统计失败:', error);
	}
};

onMounted(loadHeroStats);

// 表格参数配置
const options = useVxeTable<CcTodo>(
	{
		id: 'workFlowCcTasks',
		name: '抄送任务',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'title', title: '标题', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowName', title: '流程名称', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowCategory', title: '流程分类', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'stepName', title: '抄送节点', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'approvalMode', title: '审批模式', width: 110, slots: { default: 'row_approvalMode' } },
			{ field: 'starterName', title: '发起人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'starterDeptName', title: '发起部门', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'priority', title: '优先级', width: 100, slots: { header: 'header_priority', default: 'row_priority' } },
			{ field: 'instanceStartTime', title: '发起时间', width: 180, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '抄送时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		height: 'auto',
		stripe: true,
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params: any = Object.assign({}, state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		// 只查询抄送类型
		todoType: 'cc',
	});
	const res = await getTodos(params);
	const parsed = parsePagedResult(res);
	return { data: { result: parsed } };
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

const resetQuery = () => {
	state.queryParams.keyword = '';
	state.queryParams.priority = undefined;
	handleQuery(true);
};

const detailDialog = reactive({
	visible: false,
	loading: false,
	data: null as CcTodo | null,
	formRecord: null as any,
	activeTab: 'info',
	businessModule: undefined as string | undefined,
	businessId: undefined as string | number | undefined,
});

// 「全部节点」页签：懒加载状态由四个任务页共用（见 utils/taskList）
const { loadingAllNodes, allNodesData, resetFlowNodes, handleNodesTabClick } = useAllFlowNodes();

const viewDetail = async (row: CcTodo) => {
	detailDialog.visible = true;
	detailDialog.loading = true;
	detailDialog.data = row;
	detailDialog.formRecord = null;
	detailDialog.activeTab = 'info';
	resetFlowNodes(); // 清除之前的全部节点数据
	detailDialog.businessModule = undefined;
	detailDialog.businessId = undefined;
	try {
		const result = await fetchInstanceFormRecord(row.instanceCode);
		detailDialog.formRecord = result.formRecord;
		detailDialog.businessModule = result.businessModule;
		detailDialog.businessId = result.businessId;
	} finally {
		detailDialog.loading = false;
	}
};

const handleTabClick = (tab: any) => handleNodesTabClick(tab, detailDialog.data?.instanceCode);

// 基本信息卡片：统一走数组渲染，字段增减不用改模板
const baseInfoItems = computed(() => {
	const d = detailDialog.data;
	if (!d) return [] as Array<{ label: string; text: string }>;
	return [
		{ label: '流程标题', text: d.title },
		{ label: '流程名称', text: d.flowName },
		{ label: '抄送节点', text: d.stepName },
		{ label: '发起人', text: d.starterName },
		{ label: '抄送时间', text: d.createTime ? formatDateTime(d.createTime) : '--' },
		{ label: '实例编码', text: d.instanceCode },
	];
});
</script>

<style scoped>
/* 头图与查询条之间留出呼吸空间（根容器高度由全局 .layout-parent > div:has(.full-table) 提供） */
.workFlow-ccTasks-container > .query-toolbar {
	margin-top: 12px;
}
</style>
