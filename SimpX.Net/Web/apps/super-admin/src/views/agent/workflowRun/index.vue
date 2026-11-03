<template>
	<div class="agents-workflow-run-container">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
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
					<h2 class="page-hero__title">工作流运行记录</h2>
					<p class="page-hero__desc">每次触发都会生成一条运行记录，包含逐节点的步骤、Token 与耗时；进行中的运行每 4 秒自动刷新，待审批的运行可直接在这里通过或拒绝。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">运行总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.active }}</span>
							<span class="page-hero__stat-k">本页进行中</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': heroStats.fail === 0 }">{{ heroStats.fail }}</span>
							<span class="page-hero__stat-k">本页失败</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：筛选与批量操作同一行 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="工作流">
					<el-select v-model="state.queryParams.workflowId" class="query-select" clearable filterable placeholder="全部">
						<el-option v-for="item in state.workflowList" :key="item.id" :label="item.name" :value="item.id" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.status" class="query-select" clearable placeholder="全部">
						<el-option v-for="(meta, key) in RUN_STATUS_META" :key="key" :label="meta.label" :value="Number(key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="触发时间">
					<el-date-picker v-model="state.queryParams.range" type="datetimerange" range-separator="至" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" @click="handleQuery(true)"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button icon="ele-Delete" type="danger" plain :disabled="!state.selectedIds.length" :loading="state.batchDeleting" @click="handleBatchDelete" v-auth="'agent/workflowRun/delete'">
					批量删除
				</el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="160" description="暂无运行记录，在工作流编排页发布并触发一次运行即可看到" />
					</template>

					<template #row_run="{ row }">
						<div class="run-cell">
							<span class="run-cell__id">#{{ row.id }}</span>
							<span class="cell-sub">{{ row.triggerSource || '手动触发' }}</span>
						</div>
					</template>

					<template #row_workflow="{ row }">
						<div class="run-cell">
							<span class="run-cell__name" :title="row.workflowName || ''">{{ row.workflowName || '未命名工作流' }}</span>
							<span class="cell-sub">定义版本 v{{ row.workflowVersion ?? '-' }}</span>
						</div>
					</template>

					<template #row_status="{ row }">
						<el-tag size="small" effect="light" :type="statusMeta(row.status).type">
							<i v-if="isRunActive(row.status)" class="run-pulse" />
							{{ statusMeta(row.status).label }}
						</el-tag>
					</template>

					<template #row_steps="{ row }">
						<span class="num-text">{{ row.totalSteps ?? 0 }}</span>
					</template>

					<template #row_tokens="{ row }">
						<span v-if="row.totalTokenCount" class="num-text" :title="`输入 ${row.inputTokenCount ?? 0} / 输出 ${row.outputTokenCount ?? 0}`">
							{{ formatNumber(row.totalTokenCount) }}
						</span>
						<span v-else class="cell-sub">—</span>
					</template>

					<template #row_duration="{ row }">
						<span class="num-text">{{ formatDuration(row.durationMs) }}</span>
					</template>

					<template #row_time="{ row }">
						<div class="run-cell">
							<span class="time-text">{{ formatTime(row.startTime || row.createTime) }}</span>
							<span v-if="row.endTime" class="cell-sub">{{ formatTime(row.endTime) }} 结束</span>
						</div>
					</template>

					<template #row_buttons="{ row }">
						<el-button icon="ele-View" text type="primary" @click="openDetail(row)"> 详情 </el-button>
						<el-button
							v-if="row.status === WORKFLOW_RUN_STATUS.WaitingApproval"
							icon="ele-Select"
							text
							type="warning"
							@click="openDetail(row)"
							v-auth="'agent/workflowRun/approve'"
						>
							审批
						</el-button>
						<el-button v-if="isRunActive(row.status)" icon="ele-VideoPause" text type="danger" @click="handleCancel(row)" v-auth="'agent/workflowRun/cancel'"> 取消 </el-button>
						<el-button v-else icon="ele-RefreshRight" text type="primary" @click="handleRetry(row)"> 重试 </el-button>
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'agent/workflowRun/delete'"> 删除 </el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<RunDetailDrawer v-model="state.detailVisible" :run-id="state.detailRunId" @changed="refreshSilently" />
	</div>
</template>

<script lang="ts" setup name="agentWorkflowRun">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { AgentWorkflowApi, AgentWorkflowRunApi } from '/@/api-services/agent/api';
import { AgentWorkflowDto, AgentWorkflowRunDto, AgentWorkflowRunStatusEnum } from '/@/api-services/agent/models';
import { RUN_STATUS_META, WORKFLOW_RUN_STATUS, isRunActive } from '../workflow/types';
import RunDetailDrawer from './component/runDetailDrawer.vue';

const route = useRoute();
const xGrid = ref<VxeGridInstance>();

const state = reactive({
	workflowList: [] as AgentWorkflowDto[],
	rows: [] as AgentWorkflowRunDto[],
	selectedIds: [] as number[],
	batchDeleting: false,
	detailVisible: false,
	detailRunId: undefined as number | undefined,
	queryParams: {
		workflowId: undefined as number | undefined,
		status: undefined as number | undefined,
		range: null as [Date, Date] | null,
	},
});

/** 头图统计：总数取接口 total，进行中与失败按当前页汇总 */
const heroStats = reactive({ total: 0, active: 0, fail: 0 });

/** 当前页是否有仍在推进的运行：有才轮询，终态页面完全不打扰后端 */
const hasActiveRun = computed(() => state.rows.some((row) => isRunActive(row.status)));

const statusMeta = (status?: number) => RUN_STATUS_META[Number(status ?? 0)] ?? RUN_STATUS_META[0];

const formatTime = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 19) : '-');

const formatNumber = (value?: number | null) => (value === null || value === undefined ? '-' : String(value).replace(/\B(?=(\d{3})+(?!\d))/g, ','));

const formatDuration = (ms?: number | null) => {
	if (!ms) return '-';
	if (ms < 1000) return `${ms} ms`;
	if (ms < 60000) return `${(ms / 1000).toFixed(1)} s`;
	const minutes = Math.floor(ms / 60000);
	const seconds = Math.round((ms % 60000) / 1000);
	return `${minutes} m ${seconds} s`;
};

// 表格参数配置
const options = useVxeTable<AgentWorkflowRunDto>(
	{
		id: 'agentsWorkflowRun',
		name: '工作流运行记录',
		columns: [
			{ type: 'checkbox', width: 46, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left', align: 'center', headerAlign: 'center' },
			{ field: 'run', title: '运行', width: 150, slots: { default: 'row_run' } },
			{ field: 'workflowName', title: '工作流', minWidth: 200, slots: { default: 'row_workflow' } },
			{ field: 'status', title: '状态', width: 110, align: 'center', headerAlign: 'center', slots: { default: 'row_status' } },
			{ field: 'totalSteps', title: '步骤', width: 80, align: 'center', headerAlign: 'center', slots: { default: 'row_steps' } },
			{ field: 'totalTokenCount', title: 'Token', width: 100, align: 'right', headerAlign: 'right', slots: { default: 'row_tokens' } },
			{ field: 'durationMs', title: '耗时', width: 110, align: 'right', headerAlign: 'right', slots: { default: 'row_duration' } },
			{ field: 'startTime', title: '触发时间', width: 190, slots: { default: 'row_time' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', align: 'center', headerAlign: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		align: 'left',
		headerAlign: 'left',
		cellConfig: { height: 46 },
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { enabled: false },
	}
);

const syncCheckboxRecords = () => {
	state.selectedIds = (xGrid.value?.getCheckboxRecords() ?? []).map((item: AgentWorkflowRunDto) => item.id!);
};

// 表格事件：双击行直接打开详情，批量删除依赖勾选同步
const gridEvents: VxeGridListeners<AgentWorkflowRunDto> = {
	checkboxChange: syncCheckboxRecords,
	checkboxAll: syncCheckboxRecords,
	cellDblclick({ row }) {
		openDetail(row);
	},
};

// 查询api：后端是 GET + query 参数，时间区间直接传 Date（生成代码按 ISO 序列化）
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const q = state.queryParams;
	const res = await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunPageDataGet(
		q.workflowId,
		q.status as AgentWorkflowRunStatusEnum | undefined,
		undefined, // userId：后台按数据权限过滤，不在这里指定
		q.range?.[0],
		q.range?.[1],
		page.currentPage,
		page.pageSize
	);
	const items = res.data.result?.items ?? [];
	state.rows = items;
	heroStats.total = res.data.result?.total ?? 0;
	heroStats.active = items.filter((row) => isRunActive(row.status)).length;
	heroStats.fail = items.filter((row) => row.status === WORKFLOW_RUN_STATUS.Fail).length;
	return res;
};

// 查询操作：主动查询后旧勾选已失效，一并清空
const handleQuery = async (reset = false) => {
	options.loading = true;
	try {
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	} finally {
		options.loading = false;
	}
	await xGrid.value?.clearCheckboxRow();
	state.selectedIds = [];
};

/** 轮询/回写用的静默刷新：不动 loading，也不清空勾选，避免用户正在挑选时被打断 */
const refreshSilently = async () => {
	await xGrid.value?.commitProxy('query');
};

const resetQuery = () => {
	// 通过路由带入的工作流筛选也一并清掉，否则「重置」看起来没生效
	state.queryParams.workflowId = undefined;
	state.queryParams.status = undefined;
	state.queryParams.range = null;
	handleQuery(true);
};

const openDetail = (row: AgentWorkflowRunDto) => {
	state.detailRunId = row.id;
	state.detailVisible = true;
};

const handleCancel = async (row: AgentWorkflowRunDto) => {
	try {
		await ElMessageBox.confirm('确定取消这次运行吗？正在执行的节点会在当前调用返回后停止。', '取消运行', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunCancelRunIdPost(row.id!);
	ElMessage.success('已提交取消');
	refreshSilently();
};

const handleRetry = async (row: AgentWorkflowRunDto) => {
	try {
		await ElMessageBox.confirm('将以相同的输入与初始变量重新发起一次运行，确定重试吗？', '重试运行', { type: 'info' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunRetryRunIdPost(row.id!);
	ElMessage.success('已发起重试，请稍后刷新查看结果');
	refreshSilently();
};

const handleDelete = async (row: AgentWorkflowRunDto) => {
	try {
		await ElMessageBox.confirm(`确定删除运行记录 #${row.id} 吗？步骤明细会一并删除，正在运行或等待审批的记录不可删除。`, '删除运行记录', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunDelete({ ids: [row.id!] });
	ElMessage.success('删除成功');
	handleQuery();
};

const handleBatchDelete = async () => {
	if (!state.selectedIds.length) return;
	try {
		await ElMessageBox.confirm(`确定删除选中的 ${state.selectedIds.length} 条运行记录吗？步骤明细会一并删除。`, '批量删除', { type: 'warning' });
	} catch {
		return;
	}
	state.batchDeleting = true;
	try {
		await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunDelete({ ids: [...state.selectedIds] });
		ElMessage.success('删除成功');
		handleQuery();
	} finally {
		state.batchDeleting = false;
	}
};

// 工作流下拉：运行记录需要跨工作流查看，一次取 200 条足够覆盖常规规模
const loadWorkflows = async () => {
	try {
		const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowPageDataGet(undefined, undefined, 1, 200);
		state.workflowList = res.data.result?.items ?? [];
	} catch {
		// 下拉加载失败不影响记录列表，错误提示已由拦截器统一弹出
		state.workflowList = [];
	}
};

/** 轮询：只在本页存在进行中的运行时启动，切到后台标签页时跳过 */
let timer: ReturnType<typeof setInterval> | null = null;
const syncPolling = () => {
	const active = hasActiveRun.value;
	if (active && !timer) {
		timer = setInterval(() => {
			if (document.visibilityState === 'visible') refreshSilently();
		}, 4000);
	}
	if (!active && timer) {
		clearInterval(timer);
		timer = null;
	}
};

watch(hasActiveRun, syncPolling);

/** 从工作流列表页跳转过来时带上 workflowId；触发运行后跳转还带 runId，直接开详情 */
const applyRouteQuery = () => {
	const workflowId = Number(route.query.workflowId ?? 0);
	const runId = Number(route.query.runId ?? 0);
	if (workflowId > 0) state.queryParams.workflowId = workflowId;
	if (runId > 0) {
		state.detailRunId = runId;
		state.detailVisible = true;
	}
};

onMounted(() => {
	applyRouteQuery();
	loadWorkflows();
});

onBeforeUnmount(() => {
	if (timer) {
		clearInterval(timer);
		timer = null;
	}
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* 运行记录里每一行都是「主值 + 次行」，统一收进一个两行单元格 */
.run-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
}

.run-cell__id {
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 13px;
	color: var(--el-text-color-primary);
}

.run-cell__name {
	font-size: 13px;
	line-height: 18px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.run-cell .cell-sub {
	font-size: 12px;
	line-height: 16px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 数字列等宽，翻页时位数变化不跳动 */
.num-text {
	font-variant-numeric: tabular-nums;
}

.time-text {
	font-size: 13px;
	font-variant-numeric: tabular-nums;
}

/* 进行中的状态点：整表只有这里会动，用来提示「这行还在跑」 */
.run-pulse {
	display: inline-block;
	width: 6px;
	height: 6px;
	margin-right: 4px;
	border-radius: 50%;
	background-color: currentColor;
	vertical-align: middle;
	animation: run-blink 1.4s ease-in-out infinite;
}

@keyframes run-blink {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.25;
	}
}
</style>
