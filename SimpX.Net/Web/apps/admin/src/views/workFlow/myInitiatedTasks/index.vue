<template>
	<div class="workFlow-initiated-container">
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
					<h2 class="page-hero__title">我发起的</h2>
					<p class="page-hero__desc">跟踪你发起的全部流程实例；支持按关键字与状态快速筛选，并可直接暂停、恢复、取消或删除。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">					<span class="page-hero__stat-v is-on">{{ heroStats.running }}</span>					<span class="page-hero__stat-k">进行中</span>			</div>
						<div class="page-hero__stat">					<span class="page-hero__stat-v">{{ heroStats.finished }}</span>					<span class="page-hero__stat-k">已结束</span>			</div>
					</div>
				</div>
			</div>
		</div>

		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
					<el-form-item label="关键字">
						<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="流程名称 / 实例编码" clearable @keyup.enter.native="handleQuery(true)" />
					</el-form-item>
					<el-form-item label="状态">
						<el-select v-model="state.queryParams.status" class="query-select" placeholder="全部状态" clearable>
							<el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
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
				<template #row_businessTitle="{ row }">
					{{ row.businessTitle || '--' }}
				</template>
				<template #row_version="{ row }">
					<el-tag type="info">v{{ row.version }}</el-tag>
				</template>
				<template #row_businessModule="{ row }">
					{{ row.businessModule || '通用流程' }}
				</template>
				<template #row_status="{ row }">
					<WfStatusPill :text="instanceStatusInfo(row.status).text" :tone="instanceStatusInfo(row.status).tone" />
				</template>
				<template #row_startTime="{ row }">
					{{ formatDateTime(row.startTime) }}
				</template>
				<template #row_completeTime="{ row }">
					<span v-if="row.completeTime">{{ formatDateTime(row.completeTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_buttons="{ row }">
					<el-button text type="info" icon="ele-InfoFilled" @click="openDetail(row)">详情</el-button>
					<el-button text type="warning" icon="ele-VideoPause" @click="handleControl(row, 'pause')" :disabled="row.status !== WorkflowStatusEnum.Running"> 暂停 </el-button>
					<el-button text type="success" icon="ele-VideoPlay" @click="handleControl(row, 'resume')" :disabled="row.status !== WorkflowStatusEnum.Suspended"> 恢复 </el-button>
					<el-button text type="danger" icon="ele-RefreshLeft" @click="handleControl(row, 'cancel')" :disabled="row.status !== WorkflowStatusEnum.Running"> 取消 </el-button>
					<el-button text type="danger" icon="ele-Delete" @click="handleDelete(row)"> 删除 </el-button>
				</template>
			</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="detailDialog.visible" width="1080px" class="wf-detail" draggable :close-on-click-modal="false" :destroy-on-close="true">
			<template #header>
				<DetailHead title="流程详情" :name="detailDialog.baseInfo?.flowName" :code="detailDialog.baseInfo?.instanceCode">
					<template #side>
						<template v-if="detailDialog.baseInfo">
							<WfStatusPill :text="instanceStatusInfo(detailDialog.baseInfo.status).text" :tone="instanceStatusInfo(detailDialog.baseInfo.status).tone" />
							<el-tag size="small" effect="plain" round>v{{ detailDialog.baseInfo.version }}</el-tag>
						</template>
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
										<span class="wf-summary__v" :title="item.text">{{ item.text }}</span>
									</div>
								</div>

								<!-- 进度概览：让信息页有节奏，也顺手给出「走到哪一步」的判断 -->
								<div class="wf-panel" v-if="stepStats.total">
									<div class="wf-panel__hd"><i class="wf-panel__bar"></i>流程进度</div>
									<div class="wf-progress">
										<div class="wf-progress__line">
											<span class="wf-progress__text">{{ stepStats.done }} / {{ stepStats.total }} 步</span>
											<span class="wf-progress__pct">{{ stepStats.percent }}%</span>
										</div>
										<el-progress
											:percentage="stepStats.percent"
											:stroke-width="8"
											:show-text="false"
											:status="stepStats.percent === 100 ? 'success' : undefined"
										/>
										<div class="wf-metrics">
											<div class="wf-metric">
												<span class="wf-metric__v">{{ stepStats.total }}</span>
												<span class="wf-metric__k">步骤总数</span>
											</div>
											<div class="wf-metric">
												<span class="wf-metric__v is-success">{{ stepStats.done }}</span>
												<span class="wf-metric__k">已完成</span>
											</div>
											<div class="wf-metric">
												<span class="wf-metric__v is-warning">{{ stepStats.running }}</span>
												<span class="wf-metric__k">执行中</span>
											</div>
											<div class="wf-metric">
												<span class="wf-metric__v">{{ detailDialog.logs.length }}</span>
												<span class="wf-metric__k">操作记录</span>
											</div>
										</div>
									</div>
								</div>

								<!-- 业务数据：JSON 代码块，避免只读输入框撑高弹窗 -->
								<div class="wf-panel" v-if="detailDialog.baseInfo?.businessData">
									<div class="wf-panel__hd"><i class="wf-panel__bar"></i>业务数据</div>
									<pre class="wf-json">{{ formatJson(detailDialog.baseInfo.businessData) }}</pre>
								</div>

								<!-- 业务模块详情（仅业务模块表单显示，formId 为 0 表示无表单即业务模块表单） -->
								<BusinessModuleDetail v-if="!detailDialog.formRecord?.formId" :business-module="detailDialog.baseInfo?.businessModule" :business-id="detailDialog.baseInfo?.businessId" />
							</template>
						</el-skeleton>
					</el-tab-pane>

					<el-tab-pane label="表单数据" name="form" v-if="detailDialog.formRecord?.formId">
						<el-skeleton :loading="detailDialog.loading" animated>
							<template #default>
									<FormRender
										:schema-json="detailDialog.formRecord?.schemaJson"
										:form-data="detailDialog.formRecord?.formData"
										:form-version="detailDialog.formRecord?.formVersion"
										:submit-time="detailDialog.formRecord?.submitTime"
										:disabled="true"
										:show-meta="true"
									/>
							</template>
						</el-skeleton>
					</el-tab-pane>

					<el-tab-pane label="步骤进度" name="steps">
						<vxe-grid ref="stepsGridRef" v-bind="stepsGridOptions" :data="detailDialog.steps" :loading="detailDialog.loading">
							<template #step_expand="{ row }">
								<div class="wf-todos">
									<div v-if="row.todos && row.todos.length > 0" class="wf-todos__grid">
										<div class="wf-todo" v-for="todo in row.todos" :key="todo.todoId">
											<div class="wf-todo__hd">
												<el-avatar :size="24" class="wf-todo__avatar">{{ (todo.todoUserName || '-').slice(0, 1) }}</el-avatar>
												<span class="wf-todo__name">{{ todo.todoUserName }}</span>
												<el-tag size="small" :type="getTodoStatusInfo(todo).tagType" effect="light">{{ getTodoStatusInfo(todo).text }}</el-tag>
											</div>
											<div class="wf-todo__time">{{ todo.handleTime ? formatDateTime(todo.handleTime) : '尚未处理' }}</div>
											<div class="wf-todo__comment" v-if="todo.handleComment">审批意见：{{ todo.handleComment }}</div>
										</div>
									</div>
									<el-empty v-else description="该步骤无待办任务" :image-size="60" />
								</div>
							</template>
							<template #step_type="{ row }">
								<span>{{ stepTypeText(row.stepType) }}</span>
							</template>
							<template #step_approvalMode="{ row }">
								<span>{{ approvalModeText(row.approvalMode) }}</span>
							</template>
							<template #step_status="{ row }">
								<WfStatusPill :text="execStatusInfo(row.status).text" :tone="execStatusInfo(row.status).tone" />
							</template>
							<template #step_startTime="{ row }">
								{{ row.startTime ? formatDateTime(row.startTime) : '-' }}
							</template>
							<template #step_completeTime="{ row }">
								{{ row.completeTime ? formatDateTime(row.completeTime) : '-' }}
							</template>
							<template #step_todoCount="{ row }">
								<span v-if="row.todos && getTodoCount(row.todos) > 0" class="wf-count">{{ getTodoCount(row.todos) }} 人</span>
								<span v-else>-</span>
							</template>
						</vxe-grid>
					</el-tab-pane>

					<el-tab-pane label="执行日志" name="logs">
						<el-skeleton :loading="detailDialog.loading" animated>
							<template #default>
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
											</div>
											<div class="wf-log__error" v-if="log.errorMessage">{{ log.errorMessage }}</div>
										</div>
									</el-timeline-item>
								</el-timeline>
								<el-empty v-else description="暂无执行日志" :image-size="80" />
							</template>
						</el-skeleton>
					</el-tab-pane>

					<el-tab-pane label="全部节点" name="allNodes">
						<FlowNodesTab :data="allNodesData" :loading="loadingAllNodes" />
					</el-tab-pane>
				</el-tabs>
			</div>
		</el-dialog>

	</div>
</template>

<script setup lang="ts" name="WorkFlowMyInitiatedTasks">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
import { computed, reactive, ref, nextTick, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridProps, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { myInstances, getWorkflowInstance, getWorkflowSteps, getWorkflowLogs, cancelWorkflow, pauseWorkflow, resumeWorkflow, deleteWorkflowInstance, GetTodoStats } from '/@/api/workflow';
import { formatDateTime } from '/@/utils/format';
import FormRender from '../components/FormRender.vue';
import BusinessModuleDetail from '../components/BusinessModuleDetail.vue';
import DetailHead from '../components/DetailHead.vue';
import FlowNodesTab from '../components/flowNodesTab.vue';
import WfStatusPill from '../components/wfStatusPill.vue';
import { approvalModeText, execStatusInfo, instanceStatusInfo, stepTypeText } from '../utils/workflowDict';
import { parseData, parsePagedResult, useAllFlowNodes } from '../utils/taskList';

enum WorkflowStatusEnum {
	Pending = 0,
	Running = 1,
	Completed = 2,
	Terminated = 3,
	Suspended = 4,
}

interface WorkflowInstanceItem {
	id: number;
	instanceCode: string;
	businessTitle?: string;
	workflowCode: string;
	flowName: string;
	status: number;
	startTime: string;
	completeTime?: string;
	remark?: string;
	version: number;
}

interface WorkflowLog {
	id: number;
	actionContent?: string;
	actionType?: string;
	operatorName?: string;
	result?: string;
	errorMessage?: string;
	createTime: string;
}

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		keyword: '',
		status: undefined as number | undefined,
	},
});

// 头图统计：取后端 /todos/stats 的全量真实数字（此前按当前页拼凑，翻页即变，属假指标）
const heroStats = reactive({ running: 0, finished: 0 });

const loadHeroStats = async () => {
	try {
		const res = await GetTodoStats({});
		const stats = res.data?.result;
		if (!stats) return;
		heroStats.running = stats.runningCount ?? 0;
		heroStats.finished = stats.finishedCount ?? 0;
	} catch (error) {
		console.error('获取流程统计失败:', error);
	}
};

// 表格参数配置
const options = useVxeTable<WorkflowInstanceItem>(
	{
		id: 'workFlowMyInitiatedTasks',
		name: '我发起的流程',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'flowName', title: '流程名称', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'businessTitle', title: '业务标题', minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_businessTitle' } },
			{ field: 'instanceCode', title: '实例编码', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'version', title: '版本', width: 100, slots: { default: 'row_version' } },
			{ field: 'businessModule', title: '所属模块', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_businessModule' } },
			{ field: 'starterName', title: '发起人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'starterDeptName', title: '发起部门', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 100, slots: { default: 'row_status' } },
			{ field: 'startTime', title: '发起时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_startTime' } },
			{ field: 'completeTime', title: '完成时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_completeTime' } },
			{ field: 'remark', title: '备注', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		rowConfig: { keyField: 'instanceCode' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 详情弹窗-步骤进度表格（树形 + 展开行）
const stepsGridRef = ref<VxeGridInstance>();
const stepsGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	// 不能写 height:'auto'：那是「撑满父容器」，展开行会被容器高度裁掉（只显示上半截），
	// 交给内容撑开 + 外层 .el-tabs__content 滚动才是正确语义
	stripe: true,
	rowConfig: { keyField: 'id' },
	treeConfig: { expandAll: true },
	// transform=false 的树形必须显式声明 mode=inside，否则 vxe 会在控制台告警并可能抢展开逻辑
	expandConfig: { expandAll: true, mode: 'inside' },
	columns: [
		// 展开列必须定宽 + 左侧固定，否则会被当作弹性列吃掉剩余空间
		{ field: 'expand', type: 'expand', width: 46, fixed: 'left', slots: { content: 'step_expand' } },
		{ field: 'stepName', title: '步骤名称', minWidth: 220, treeNode: true, showOverflow: 'tooltip' },
		{ field: 'stepType', title: '类型', width: 110, slots: { default: 'step_type' } },
		{ field: 'approvalMode', title: '审批模式', width: 100, slots: { default: 'step_approvalMode' } },
		{ field: 'status', title: '状态', width: 110, slots: { default: 'step_status' } },
		{ field: 'startTime', title: '开始时间', width: 160, showOverflow: 'tooltip', slots: { default: 'step_startTime' } },
		{ field: 'completeTime', title: '完成时间', width: 160, showOverflow: 'tooltip', slots: { default: 'step_completeTime' } },
		{ field: 'todoCount', title: '待办人数', width: 110, slots: { default: 'step_todoCount' } },
	],
});

const statusOptions = [
	{ label: '待执行', value: WorkflowStatusEnum.Pending },
	{ label: '运行中', value: WorkflowStatusEnum.Running },
	{ label: '已完成', value: WorkflowStatusEnum.Completed },
	{ label: '已终止', value: WorkflowStatusEnum.Terminated },
	{ label: '已暂停', value: WorkflowStatusEnum.Suspended },
];

const detailDialog = reactive({
	visible: false,
	loading: false,
	activeTab: 'info',
	baseInfo: null as any,
	steps: [] as any[],
	logs: [] as WorkflowLog[],
	formRecord: null as any,
});

const { loadingAllNodes, allNodesData, resetFlowNodes, handleNodesTabClick } = useAllFlowNodes();

const formatJson = (value?: string | null) => {
	if (!value) return '';
	try {
		return JSON.stringify(JSON.parse(value), null, 2);
	} catch (error) {
		return value;
	}
};

// 基本信息：统一走卡片网格渲染
const baseInfoItems = computed(() => {
	const b = detailDialog.baseInfo;
	if (!b) return [] as Array<{ label: string; text: string }>;
	return [
		{ label: '流程名称', text: b.flowName || '--' },
		{ label: '实例编码', text: b.instanceCode || '--' },
		{ label: '业务标题', text: b.businessTitle || '--' },
		{ label: '所属模块', text: b.businessModule || '通用流程' },
		{ label: '发起人', text: b.starterName || '--' },
		{ label: '发起部门', text: b.starterDeptName || '--' },
		{ label: '发起时间', text: b.startTime ? formatDateTime(b.startTime) : '--' },
		{ label: '完成时间', text: b.completeTime ? formatDateTime(b.completeTime) : '--' },
		{ label: '备注', text: b.remark || '--' },
	];
});

// 步骤是树形结构，进度统计需先摊平
const flattenSteps = (list: any[] = []): any[] => list.reduce((acc: any[], step: any) => acc.concat(step, flattenSteps(step.children || [])), []);

// 基本信息页的流程进度概览（避免信息页大面积留白）
const stepStats = computed(() => {
	const all = flattenSteps(detailDialog.steps);
	const total = all.length;
	const done = all.filter((s) => s.status === 2).length;
	const running = all.filter((s) => s.status === 1).length;
	return { total, done, running, percent: total ? Math.round((done / total) * 100) : 0 };
});

// 待办任务状态与类型展示（审批 / 抄送 / 通知等）
const getTodoStatusInfo = (todo: any) => {
	const todoType = todo.todoType || 'todo';
	const status = typeof todo.status === 'number' ? todo.status : 0;

	// 抄送/通知类：不展示为“待处理”，而是单独的类型文案
	if (todoType === 'cc') {
		return { text: '抄送', tagType: 'info' };
	}
	if (todoType === 'notify') {
		return { text: '通知', tagType: 'info' };
	}

	// 默认视为审批待办
	switch (status) {
		case 1:
			return { text: '已处理', tagType: 'success' };
		case 2:
			return { text: '已拒绝', tagType: 'danger' };
		case 3:
			return { text: '已退回', tagType: 'warning' };
		case 4:
			return { text: '已跳过', tagType: 'info' };
		default:
			return { text: '待处理', tagType: 'warning' };
	}
};

// 统计真实待办人数（不包含抄送/通知等）
const getTodoCount = (todos: any[] = []) => todos.filter((todo) => (todo.todoType || 'todo') === 'todo').length;

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign({}, state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await myInstances(params);
	return { data: { result: parsePagedResult(res) } };
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

const resetQuery = () => {
	state.queryParams.keyword = '';
	state.queryParams.status = undefined;
	handleQuery(true);
};

const openDetail = async (row: WorkflowInstanceItem) => {
	detailDialog.visible = true;
	detailDialog.loading = true;
	detailDialog.activeTab = 'info';
	resetFlowNodes();
	try {
		const [infoRes, stepsRes, logsRes] = await Promise.all([getWorkflowInstance(row.instanceCode), getWorkflowSteps(row.instanceCode), getWorkflowLogs(row.instanceCode)]);
		detailDialog.baseInfo = parseData(infoRes);
		detailDialog.formRecord = detailDialog.baseInfo?.formRecord ?? null;
		detailDialog.steps = parseData(stepsRes) ?? [];
		// 后端已按操作时间倒序排列（最新的在上面）
		detailDialog.logs = parseData(logsRes) ?? [];
	} catch (error: any) {
		ElMessage.error(error.message || '获取详情失败');
	} finally {
		detailDialog.loading = false;
	}
};

const controlApiMap = {
	cancel: cancelWorkflow,
	pause: pauseWorkflow,
	resume: resumeWorkflow,
};

const actionTextMap: Record<string, string> = {
	cancel: '取消流程',
	pause: '暂停流程',
	resume: '恢复流程',
};

const handleControl = (row: WorkflowInstanceItem, action: 'cancel' | 'pause' | 'resume') => {
	const api = controlApiMap[action];
	if (!api) return;

	ElMessageBox.prompt('请输入备注（可选）', actionTextMap[action], {
		// type: 'info',
		inputPlaceholder: '可填写原因或备注',
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		inputType: 'textarea',
		inputValue: '',
		beforeClose: async (closeAction, instance, done) => {
			if (closeAction === 'confirm') {
				instance.confirmButtonLoading = true;
				try {
					await api({
						instanceCode: row.instanceCode,
						remark: (instance as any).inputValue || undefined,
					});
					ElMessage.success(`${actionTextMap[action]}成功`);
					handleQuery();
					loadHeroStats();
					if (detailDialog.visible) {
						openDetail(row);
					}
					done();
				} catch (error: any) {
					ElMessage.error(error.message || `${actionTextMap[action]}失败`);
				} finally {
					instance.confirmButtonLoading = false;
				}
			} else {
				done();
			}
		},
	});
};

const handleDelete = (row: WorkflowInstanceItem) => {
	const currentStatusText = instanceStatusInfo(row.status).text;
	const isRunning = row.status === WorkflowStatusEnum.Running;
	const isSuspended = row.status === WorkflowStatusEnum.Suspended;

	let confirmMessage = `确认删除流程实例「${row.instanceCode}」吗？\n\n当前状态：${currentStatusText}`;
	if (isRunning || isSuspended) {
		confirmMessage += `\n\n⚠️ 该流程正在${isRunning ? '运行中' : '已暂停'}，删除前系统会自动取消该流程。`;
	}
	confirmMessage += `\n\n删除后将无法恢复，请谨慎操作！`;

	ElMessageBox.confirm(confirmMessage, '删除确认', {
		confirmButtonText: '确定删除',
		cancelButtonText: '取消',
		type: 'warning',
		dangerouslyUseHTMLString: false,
	})
		.then(async () => {
			try {
				// 执行删除（后端会自动处理运行中和已暂停的流程）
				await deleteWorkflowInstance({
					instanceCode: row.instanceCode,
					remark: isRunning || isSuspended ? '删除前自动取消' : undefined,
				});
				ElMessage.success('删除成功');
				handleQuery();
				loadHeroStats();
				if (detailDialog.visible && detailDialog.baseInfo?.instanceCode === row.instanceCode) {
					detailDialog.visible = false;
				}
			} catch (error: any) {
				ElMessage.error(error?.message || '删除失败');
			}
		})
		.catch(() => {
			/* 用户取消 */
		});
};

const handleTabClick = async (tab: any) => {
	// 步骤进度页里的表格在隐藏 pane 中初始化（宽度/数据都不完整），
	// 切回来时重算一次，否则展开行高度可能沿用过期的测量值，卡片只露上半截
	if (tab.paneName === 'steps') {
		await nextTick();
		stepsGridRef.value?.recalculate?.();
		return;
	}
	handleNodesTabClick(tab, detailDialog.baseInfo?.instanceCode);
};

const route = useRoute();

onMounted(async () => {
	loadHeroStats();
	// 检查路由参数，如果有 instanceCode，自动打开详情对话框（表格数据由 proxyConfig autoLoad 加载）
	const instanceCode = route.query.instanceCode as string;
	if (instanceCode) {
		await handleQuery();
		// 在表格数据中查找对应的流程实例
		const tableData = (xGrid.value?.getTableData().tableData ?? []) as WorkflowInstanceItem[];
		const instance = tableData.find((item) => item.instanceCode === instanceCode);
		if (instance) {
			// 找到对应的流程实例，打开详情对话框
			await openDetail(instance);
		} else {
			// 如果表格中没有找到，可能是数据还没加载完或者是其他用户的流程
			// 直接通过 instanceCode 打开详情（openDetail 方法只需要 instanceCode）
			const tempInstance: WorkflowInstanceItem = {
				instanceCode: instanceCode,
			} as WorkflowInstanceItem;
			await openDetail(tempInstance);
		}
	}
});
</script>

<style scoped>
/* 头图与查询条之间留出呼吸空间（根容器高度由全局 .layout-parent > div:has(.full-table) 提供） */
.workFlow-initiated-container > .query-toolbar {
	margin-top: 12px;
}

.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
}
</style>

<style>
/* el-dialog 默认 append-to-body，scoped 穿透不生效，本页特有样式放非 scoped 块；
   弹窗外壳/头部/状态胶囊等通用样式已由 views/workFlow/styles/workflow.scss 提供 */

/* ---------- 面板 / JSON ---------- */
.wf-panel {
	margin-top: 14px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	overflow: hidden;
	background: var(--el-bg-color);
}

.wf-panel__hd {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 9px 14px;
	font-size: 13px;
	font-weight: 600;
	background: var(--el-fill-color-lighter);
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.wf-panel__bar {
	width: 3px;
	height: 13px;
	border-radius: 2px;
	background: var(--el-color-primary);
}

.wf-progress {
	padding: 14px;
}

.wf-progress__line {
	display: flex;
	align-items: baseline;
	justify-content: space-between;
	margin-bottom: 8px;
}

.wf-progress__text {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.wf-progress__pct {
	font-size: 20px;
	font-weight: 700;
	line-height: 1;
	color: var(--el-color-primary);
	letter-spacing: 0.5px;
}

.wf-metrics {
	margin-top: 14px;
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 10px;
}

.wf-metric {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 2px;
	padding: 10px 6px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
}

.wf-metric__v {
	font-size: 18px;
	font-weight: 700;
	line-height: 1.2;
	color: var(--el-text-color-primary);
}

.wf-metric__v.is-success {
	color: var(--el-color-success);
}

.wf-metric__v.is-warning {
	color: var(--el-color-warning);
}

.wf-metric__k {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.wf-json {
	margin: 0;
	padding: 12px 14px;
	max-height: 220px;
	overflow: auto;
	font-size: 12px;
	line-height: 1.7;
	font-family: var(--el-font-family-mono, monospace);
	color: var(--el-text-color-regular);
	white-space: pre-wrap;
	word-break: break-all;
}

/* ---------- 步骤进度：展开待办 ---------- */
.wf-todos {
	padding: 8px 14px;
	background: var(--el-fill-color-lighter);
}

.wf-todos__grid {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
	gap: 8px;
}

.wf-todo {
	padding: 9px 12px;
	border-radius: 10px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}

.wf-todo__hd {
	display: flex;
	align-items: center;
	gap: 8px;
}

.wf-todo__avatar {
	flex-shrink: 0;
	font-size: 12px;
	background: var(--el-color-primary);
	color: #fff;
}

.wf-todo__name {
	font-size: 13px;
	font-weight: 600;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.wf-todo__time {
	margin-top: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.wf-todo__comment {
	margin-top: 5px;
	padding: 5px 8px;
	border-radius: 6px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-lighter);
	border-left: 2px solid var(--el-color-primary);
}

/* 执行日志（.wf-timeline / .wf-log 系列）已上移到 views/workFlow/styles/workflow.scss */

/* 步骤进度展开行：高度始终跟随内容，避免表格沿用旧测量值把待办卡片裁掉半边 */
.el-dialog .vxe-body--expanded-cell {
	height: auto !important;
	max-height: none;
	overflow: visible;
}
</style>
