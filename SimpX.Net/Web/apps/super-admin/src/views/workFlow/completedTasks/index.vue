<template>
	<div class="workFlow-completed-container">
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
					<h2 class="page-hero__title">已办任务</h2>
					<p class="page-hero__desc">查看你已处理或已拒绝的流程事项；支持按状态、类型与优先级快速筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">					<span class="page-hero__stat-v is-on">{{ heroStats.done }}</span>					<span class="page-hero__stat-k">已处理</span>			</div>
						<div class="page-hero__stat">					<span class="page-hero__stat-v">{{ heroStats.rejected }}</span>					<span class="page-hero__stat-k">已拒绝</span>			</div>
					</div>
				</div>
			</div>
		</div>

		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
					<el-form-item label="关键字">
						<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="标题 / 流程名称 / 实例编码" clearable @keyup.enter.native="handleQuery(true)" />
					</el-form-item>
					<el-form-item label="状态">
						<el-select v-model="state.queryParams.status" class="query-select" placeholder="全部状态" clearable>
							<el-option label="已处理" :value="1" />
							<el-option label="已拒绝" :value="2" />
						</el-select>
					</el-form-item>
					<el-form-item label="类型">
						<el-select v-model="state.queryParams.todoType" class="query-select" placeholder="全部类型" clearable>
							<el-option label="待办" :value="'todo'" />
							<el-option label="抄送" :value="'cc'" />
							<el-option label="通知" :value="'notify'" />
						</el-select>
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
				<WfStatusPill :text="priorityInfo(row.priority).text" :tone="priorityInfo(row.priority).tone" />
			</template>
			<template #row_status="{ row }">
				<WfStatusPill :text="doneStatusInfo(row.status).text" :tone="doneStatusInfo(row.status).tone" />
			</template>
				<template #row_handleTime="{ row }">
					<span v-if="row.handleTime">{{ formatDateTime(row.handleTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_buttons="{ row }">
					<el-button text type="info" icon="ele-InfoFilled" @click="viewDetail(row)">详情</el-button>
				</template>
			</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="detailDialog.visible" width="1010px" class="wf-detail" draggable v-if="detailDialog.data">
			<template #header>
				<DetailHead title="审批详情" :name="detailDialog.data.title" :code="detailDialog.data.instanceCode" :sub="detailDialog.data.flowName">
					<template #side>
						<WfStatusPill :text="doneStatusInfo(detailDialog.data.status).text" :tone="doneStatusInfo(detailDialog.data.status).tone" />
						<WfStatusPill :text="`优先级 ${priorityInfo(detailDialog.data.priority).text}`" :tone="priorityInfo(detailDialog.data.priority).tone" />
					</template>
				</DetailHead>
			</template>

			<div class="wf-detail__body">
				<el-tabs v-model="detailDialog.activeTab" @tab-click="handleTabClick">
					<el-tab-pane label="基本信息" name="info">
						<el-skeleton :loading="detailDialog.loading" animated>
							<template #template>
								<el-skeleton-item variant="h3" style="width: 30%" />
								<el-skeleton-item variant="text" v-for="n in 4" :key="n" />
							</template>
							<template #default>
								<div class="wf-summary">
									<div class="wf-summary__cell" v-for="item in baseInfoItems" :key="item.label">
										<span class="wf-summary__k">{{ item.label }}</span>
										<span class="wf-summary__v" :title="item.text">{{ item.text || '--' }}</span>
									</div>
									<div class="wf-summary__cell is-wide">
										<span class="wf-summary__k">处理意见</span>
										<span class="wf-summary__v is-wrap">{{ detailDialog.data.handleComment || '--' }}</span>
									</div>
								</div>
							</template>
						</el-skeleton>

						<div class="wf-form-block">
							<div class="wf-form-block__hd"><i class="wf-form-block__bar"></i>执行日志</div>
							<div>
								<el-timeline v-if="detailDialog.logs.length" class="wf-timeline">
									<el-timeline-item
										v-for="log in detailDialog.logs"
										:key="log.id"
										:timestamp="formatDateTime(log.createTime)"
										placement="top"
										:type="log.result === 'success' ? 'success' : 'danger'"
									>
										<div class="wf-log">
											<div class="wf-log__hd">
												<span class="wf-log__title">{{ log.actionContent || log.actionType }}</span>
												<el-tag size="small" effect="light" :type="log.result === 'success' ? 'success' : 'danger'">
													{{ log.result === 'success' ? '成功' : '失败' }}
												</el-tag>
											</div>
											<div class="wf-log__meta">
												<span class="wf-log__user" v-if="log.operatorName">
													<el-avatar :size="20" class="wf-log__avatar">{{ log.operatorName?.slice(0, 1) }}</el-avatar>
													{{ log.operatorName }}
												</span>
												<span v-else class="wf-log__user">系统</span>
												<span v-if="log.actionType" class="wf-log__sep">·</span>
												<span v-if="log.actionType">{{ actionTypeText(log.actionType) }}</span>
											</div>
											<div v-if="log.errorMessage" class="wf-log__error">错误：{{ log.errorMessage }}</div>
										</div>
									</el-timeline-item>
								</el-timeline>
								<div v-else class="wf-empty">暂无执行日志</div>
							</div>
						</div>

						<div class="wf-form-block">
							<div class="wf-form-block__hd"><i class="wf-form-block__bar"></i>表单数据</div>
							<FormRender
								:schema-json="detailDialog.formRecord?.schemaJson"
								:form-data="detailDialog.formRecord?.formData"
								:form-version="detailDialog.formRecord?.formVersion"
								:submit-time="detailDialog.formRecord?.submitTime"
								:disabled="true"
								:show-meta="true"
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

<script setup lang="ts" name="WorkFlowCompletedTasks">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { formatDateTime } from '/@/utils/format';

import FormRender from '../components/FormRender.vue';
import DetailHead from '../components/DetailHead.vue';
import FlowNodesTab from '../components/flowNodesTab.vue';
import WfStatusPill from '../components/wfStatusPill.vue';
import { actionTypeText, approvalModeText, doneStatusInfo, priorityInfo } from '../utils/workflowDict';
import { parseData, parsePagedResult, useAllFlowNodes } from '../utils/taskList';
import { getTodos, getWorkflowLogs, getWorkflowInstance, GetTodoStats } from '/@/api/workflow';

interface CompletedTodo {
	id: number;
	title: string;
	flowName: string;
	stepName?: string;
	starterName: string;
	priority?: number;
	status: number;
	handleTime?: string;
	handleComment?: string;
	instanceCode: string;
	stepCode: string;
	todoType?: string;
}

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		keyword: '',
		status: undefined as number | undefined, // 默认显示全部已办任务
		todoType: undefined as string | undefined,
		priority: undefined as number | undefined,
	},
});

// 头图统计：取后端 /todos/stats 的全量真实数字（此前按当前页拼凑，翻页即变，属假指标）
const heroStats = reactive({ done: 0, rejected: 0 });

const loadHeroStats = async () => {
	try {
		const res = await GetTodoStats({});
		const stats = res.data?.result;
		if (!stats) return;
		heroStats.done = stats.processedCount ?? 0;
		heroStats.rejected = stats.rejectedCount ?? 0;
	} catch (error) {
		console.error('获取已办统计失败:', error);
	}
};

onMounted(loadHeroStats);

// 表格参数配置
const options = useVxeTable<CompletedTodo>(
	{
		id: 'workFlowCompletedTasks',
		name: '已办任务',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'title', title: '标题', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowName', title: '流程名称', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowCategory', title: '流程分类', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'stepName', title: '处理步骤', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'approvalMode', title: '审批模式', width: 110, slots: { default: 'row_approvalMode' } },
			{ field: 'starterName', title: '发起人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'starterDeptName', title: '发起部门', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'priority', title: '优先级', width: 100, slots: { header: 'header_priority', default: 'row_priority' } },
			{ field: 'status', title: '状态', width: 100, slots: { default: 'row_status' } },
			{ field: 'instanceStartTime', title: '发起时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'handleTime', title: '处理时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_handleTime' } },
			{ field: 'handleComment', title: '处理意见', minWidth: 120, showOverflow: 'tooltip' },
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
	const params = Object.assign({}, state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		isCompleted: true, // 后端按此标识只返回已办（status != 0）；同时传 status 时再按 1-已处理 / 2-已拒绝 细分
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
	state.queryParams.todoType = undefined;
	state.queryParams.priority = undefined;
	state.queryParams.status = undefined; // 重置为全部
	handleQuery(true);
};

const detailDialog = reactive({
	visible: false,
	loading: false,
	data: null as CompletedTodo | null,
	logs: [] as any[],
	formRecord: null as any,
	activeTab: 'info',
});

const { loadingAllNodes, allNodesData, resetFlowNodes, handleNodesTabClick } = useAllFlowNodes();

// 基本信息卡片：统一走数组渲染，字段增减不用改模板
const baseInfoItems = computed(() => {
	const d = detailDialog.data;
	if (!d) return [] as Array<{ label: string; text: string }>;
	return [
		{ label: '流程标题', text: d.title },
		{ label: '流程名称', text: d.flowName },
		{ label: '处理步骤', text: d.stepName },
		{ label: '发起人', text: d.starterName },
		{ label: '处理时间', text: d.handleTime ? formatDateTime(d.handleTime) : '--' },
		{ label: '实例编码', text: d.instanceCode },
	];
});

const viewDetail = async (row: CompletedTodo) => {
	detailDialog.data = row;
	detailDialog.visible = true;
	detailDialog.loading = true;
	detailDialog.logs = [];
	detailDialog.formRecord = null;
	detailDialog.activeTab = 'info';
	resetFlowNodes();
	try {
		const [logsRes, instanceRes] = await Promise.all([getWorkflowLogs(row.instanceCode), getWorkflowInstance(row.instanceCode)]);
		// 后端已按操作时间倒序（最新的在上面）
		detailDialog.logs = parseData(logsRes) ?? [];
		detailDialog.formRecord = parseData(instanceRes)?.formRecord ?? null;
	} catch (error: any) {
		ElMessage.error(error.message || '获取详情失败');
	} finally {
		detailDialog.loading = false;
	}
};

const handleTabClick = (tab: any) => handleNodesTabClick(tab, detailDialog.data?.instanceCode);

</script>

<style scoped>
/* 头图与查询条之间留出呼吸空间（根容器高度由全局 .layout-parent > div:has(.full-table) 提供） */
.workFlow-completed-container > .query-toolbar {
	margin-top: 12px;
}
</style>