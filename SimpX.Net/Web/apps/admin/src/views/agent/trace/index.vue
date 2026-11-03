<template>
	<div class="agents-trace-container">
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
					<h2 class="page-hero__title">调用轨迹</h2>
					<p class="page-hero__desc">每一次模型调用与工具调用都会在此留痕（含 Token、耗时、重试与失败原因），用于问题排查、用量审计与运维监控。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">轨迹总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': heroStats.fail === 0 }">{{ heroStats.fail }}</span>
							<span class="page-hero__stat-k">当前页失败</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ formatToken(heroStats.tokens) }}</span>
							<span class="page-hero__stat-k">当前页 Token</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：轨迹由系统自动写入，没有新增入口，只放筛选与清理 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="类型">
					<el-select v-model="state.queryParams.traceType" class="query-input" style="width: 130px" clearable placeholder="全部">
						<el-option label="模型调用" :value="1" />
						<el-option label="工具调用" :value="2" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.status" class="query-input" style="width: 110px" clearable placeholder="全部">
						<el-option label="成功" :value="0" />
						<el-option label="失败" :value="1" />
						<el-option label="未知" :value="2" />
					</el-select>
				</el-form-item>
				<el-form-item label="智能体">
					<el-select v-model="state.queryParams.appId" class="query-input" style="width: 180px" clearable filterable placeholder="全部">
						<el-option v-for="app in state.agentList" :key="String(app.id)" :label="app.name" :value="Number(app.id)" />
					</el-select>
				</el-form-item>
				<el-form-item label="模型">
					<el-input v-model="state.queryParams.modelName" class="query-input" style="width: 160px" placeholder="模型名称" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker
						v-model="state.queryParams.range"
						type="datetimerange"
						range-separator="至"
						start-placeholder="开始"
						end-placeholder="结束"
						value-format="YYYY-MM-DD HH:mm:ss"
						style="width: 340px"
					/>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					<el-button icon="ele-Download" plain @click="handleExport" :loading="state.exporting" v-auth="'agent/trace/export'"> 导出报表 </el-button>
					<el-button icon="ele-Delete" type="danger" plain @click="handleCleanup" v-auth="'agent/trace/clear'"> 清理 </el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 趋势卡：按天聚合的调用次数 / 失败 / Token，跟随查询条件里的智能体与模型 -->
		<el-card class="trend-card" shadow="hover" :body-style="{ padding: '0' }">
			<div class="trend-head">
				<div class="trend-head__left">
					<span class="trend-head__title">调用趋势</span>
					<span class="trend-head__summary">最近 {{ state.trendDays }} 天共 {{ trendSummary.calls }} 次调用 · {{ trendSummary.fails }} 次失败 · {{ formatToken(trendSummary.tokens) }} Token</span>
				</div>
				<el-radio-group v-model="state.trendDays" size="small" @change="loadTrend">
					<el-radio-button :value="7">近 7 天</el-radio-button>
					<el-radio-button :value="14">近 14 天</el-radio-button>
					<el-radio-button :value="30">近 30 天</el-radio-button>
				</el-radio-group>
			</div>
			<div v-if="trendSummary.calls > 0" class="trend-chart">
				<scEcharts height="260px" :option="trendOption" />
			</div>
			<div v-else class="trend-chart trend-chart--empty">
				<el-empty :image-size="90" description="暂无调用数据，发起一次 AI 对话后即可看到趋势" />
			</div>
		</el-card>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="160" description="暂无调用轨迹，发起一次 AI 对话后这里就会出现记录" />
					</template>
					<template #row_type="{ row }">
						<span class="type-pill" :class="row.traceType === 1 ? 'is-primary' : 'is-warning'"> <i></i>{{ row.traceType === 1 ? '模型调用' : '工具调用' }} </span>
					</template>
					<template #row_status="{ row }">
						<el-tag :type="row.status === 0 ? 'success' : row.status === 1 ? 'danger' : 'info'" size="small" effect="light">
							{{ row.status === 0 ? '成功' : row.status === 1 ? '失败' : '未知' }}
						</el-tag>
					</template>
					<template #row_target="{ row }">
						<!-- 模型调用显示模型名，工具调用显示工具名，主行+次行都齐 -->
						<div class="target-cell">
							<span class="target-cell__main">{{ row.traceType === 2 ? row.toolName || '未知工具' : row.modelName || '-' }}</span>
							<span class="cell-sub">{{ row.appName || '未关联智能体' }}</span>
						</div>
					</template>
					<template #row_source="{ row }">
						<span class="source-text">{{ row.source || '-' }}</span>
					</template>
					<template #row_tokens="{ row }">
						<!-- 工具调用不消耗独立 Token，用占位符保持表格节奏 -->
						<span v-if="row.traceType === 1" class="token-text" :title="`输入 ${row.inputTokenCount ?? 0} / 输出 ${row.outputTokenCount ?? 0}`">
							{{ formatToken(row.totalTokenCount ?? 0) }}
						</span>
						<span v-else class="cell-sub">—</span>
					</template>
					<template #row_duration="{ row }">
						<span class="duration-text" :class="{ 'is-slow': row.durationMs > 30000 }">{{ formatDuration(row.durationMs) }}</span>
					</template>
					<template #row_time="{ row }">
						<span class="time-text">{{ formatDate(row.createTime) }}</span>
					</template>
					<template #row_buttons="{ row }">
						<el-button icon="ele-View" text type="primary" @click="openDetail(row)"> 详情 </el-button>
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'agent/trace/clear'"> 删除 </el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<!-- 详情抽屉：模型调用看 Token 明细与失败原因，工具调用看入参与返回摘要 -->
		<el-drawer v-model="state.detailVisible" size="600px" :with-header="true">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-View /></el-icon>
					<span>{{ state.detail?.traceType === 2 ? '工具调用详情' : '模型调用详情' }}</span>
				</div>
			</template>
			<el-descriptions v-if="state.detail" :column="1" border size="small" label-width="110px" class="detail-desc">
				<el-descriptions-item label="轨迹类型" min-width="120px">{{ state.detail.traceType === 1 ? '模型调用' : '工具调用' }}</el-descriptions-item>
				<el-descriptions-item label="调用状态">
					<el-tag :type="state.detail.status === 0 ? 'success' : state.detail.status === 1 ? 'danger' : 'info'" size="small">{{
						state.detail.status === 0 ? '成功' : state.detail.status === 1 ? '失败' : '未知'
					}}</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="智能体">{{ state.detail.appName || state.detail.appId || '未关联' }}</el-descriptions-item>
				<el-descriptions-item label="调用来源">{{ state.detail.source || '-' }}</el-descriptions-item>
				<el-descriptions-item label="模型">{{ state.detail.modelName || '-' }}</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.traceType === 1" label="端点">{{ state.detail.endpoint || '-' }}</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.traceType === 2" label="工具调用 Id">{{ state.detail.toolCallId || '-' }}</el-descriptions-item>
				<el-descriptions-item label="是否流式">{{ state.detail.isStream ? '流式' : '非流式' }}</el-descriptions-item>
				<el-descriptions-item label="尝试次数">{{ state.detail.attemptCount ?? 1 }}</el-descriptions-item>
				<el-descriptions-item label="耗时">{{ formatDuration(state.detail.durationMs) }}</el-descriptions-item>
				<template v-if="state.detail.traceType === 1">
					<el-descriptions-item label="输入 Token">{{ state.detail.inputTokenCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="输出 Token">{{ state.detail.outputTokenCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="总 Token">{{ state.detail.totalTokenCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="思考 Token">{{ state.detail.reasoningTokenCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="缓存 Token">{{ state.detail.cachedInputTokenCount ?? 0 }}</el-descriptions-item>
				</template>
				<el-descriptions-item label="调用时间">{{ formatDate(state.detail.createTime) }}</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.failReason" label="失败原因">
					<span class="fail-reason">{{ state.detail.failReason }}</span>
				</el-descriptions-item>
			</el-descriptions>
			<template v-if="state.detail?.traceType === 2">
				<div class="detail-block">
					<div class="detail-block__title">工具入参摘要</div>
					<pre class="detail-pre">{{ state.detail.toolArguments || '（无）' }}</pre>
				</div>
				<div class="detail-block">
					<div class="detail-block__title">工具返回摘要</div>
					<pre class="detail-pre">{{ state.detail.toolResult || '（无）' }}</pre>
				</div>
			</template>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup name="agentTrace">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { downloadByData, getFileName } from '/@/utils/download';
import { resolveCssColor } from '/@/utils/theme';

// 通用图表组件：主题切换、容器尺寸变化、初始化/销毁都由它统一处理
const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

import { getAPI } from '/@/utils/axios-utils';
import { AgentApi, AgentTraceApi } from '/@/api-services/agent/api';
import { AgentAppDto, AgentTraceDto, AgentTraceTrendPoint } from '/@/api-services/agent/models';

const xGrid = ref<VxeGridInstance>();
const state = reactive({
	agentList: [] as AgentAppDto[],
	detailVisible: false,
	detail: undefined as AgentTraceDto | undefined,
	trendDays: 14,
	trendPoints: [] as AgentTraceTrendPoint[],
	exporting: false,
	queryParams: {
		traceType: undefined as number | undefined,
		status: undefined as number | undefined,
		appId: undefined as number | undefined,
		modelName: '',
		range: null as [string, string] | null,
	},
});

/** 头图统计：总数取接口 total，失败与 Token 按当前页汇总 */
const heroStats = reactive({ total: 0, fail: 0, tokens: 0 });

/** 趋势卡汇总：随天数切换/重载时重算 */
const trendSummary = computed(() => ({
	calls: state.trendPoints.reduce((sum, p) => sum + (p.callCount ?? 0), 0),
	fails: state.trendPoints.reduce((sum, p) => sum + (p.failCount ?? 0), 0),
	tokens: state.trendPoints.reduce((sum, p) => sum + (p.totalTokens ?? 0), 0),
}));

/** 趋势图配置：柱=调用次数（左轴）、线=失败数（左轴）、面积线=Token（右轴），颜色走 EP 变量随主题 */
const trendOption = computed(() => {
	const primary = resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue('--el-color-primary').trim());
	const danger = resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue('--el-color-danger').trim());
	// Token 用警示色：主题色（绿）柱子和 success（绿）线同色系会分不清
	const warning = resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue('--el-color-warning').trim());
	const textColor = getComputedStyle(document.documentElement).getPropertyValue('--el-text-color-secondary').trim();
	return {
		grid: { left: 44, right: 52, top: 44, bottom: 28 },
		tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
		// 图例靠右放；轴不再画「次数/Token」轴名（会与图例在右上角打架），单位交给 tooltip
		legend: { top: 6, right: 10, itemWidth: 14, itemHeight: 8, textStyle: { fontSize: 12 } },
		xAxis: { type: 'category', data: state.trendPoints.map((p) => p.date?.slice(5) ?? ''), axisTick: { show: false }, axisLine: { lineStyle: { color: textColor } }, axisLabel: { fontSize: 11 } },
		yAxis: [
			{ type: 'value', minInterval: 1, axisLabel: { fontSize: 11 }, splitLine: { lineStyle: { type: 'dashed', opacity: 0.4 } } },
			{ type: 'value', minInterval: 1, axisLabel: { fontSize: 11, formatter: (v: number) => (v >= 10000 ? `${Math.round(v / 1000)}k` : String(v)) }, splitLine: { show: false } },
		],
		series: [
			{
				name: '调用次数',
				type: 'bar',
				barMaxWidth: 22,
				data: state.trendPoints.map((p) => p.callCount ?? 0),
				itemStyle: { color: primary, borderRadius: [4, 4, 0, 0] },
			},
			{
				name: '失败',
				type: 'line',
				data: state.trendPoints.map((p) => p.failCount ?? 0),
				symbol: 'circle',
				symbolSize: 6,
				lineStyle: { width: 2, color: danger },
				itemStyle: { color: danger },
			},
			{
				name: 'Token',
				type: 'line',
				yAxisIndex: 1,
				smooth: true,
				data: state.trendPoints.map((p) => p.totalTokens ?? 0),
				symbol: 'none',
				lineStyle: { width: 2, color: warning },
				itemStyle: { color: warning },
				areaStyle: { opacity: 0.12, color: warning },
			},
		],
	};
});

/** Token 万级缩写：单元格放不下 6 位数字，1.2w 比 123456 更好扫读 */
const formatToken = (value: number) => (value >= 10000 ? `${(value / 10000).toFixed(1)}w` : String(value ?? 0));

/** 耗时：1s 以内显示毫秒，以上折算秒 */
const formatDuration = (ms?: number) => {
	if (!ms) return '-';
	return ms < 1000 ? `${ms}ms` : `${(ms / 1000).toFixed(1)}s`;
};

const formatDate = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 19) : '-');

// 表格参数配置
const options = useVxeTable<AgentTraceDto>(
	{
		id: 'agentsTrace',
		name: 'AI调用轨迹',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left', align: 'center', headerAlign: 'center' },
			{ field: 'type', title: '类型', width: 100, align: 'center', headerAlign: 'center', slots: { default: 'row_type' } },
			{ field: 'status', title: '状态', width: 80, align: 'center', headerAlign: 'center', slots: { default: 'row_status' } },
			{ field: 'target', title: '模型 / 工具', minWidth: 220, slots: { default: 'row_target' } },
			{ field: 'source', title: '来源', width: 110, slots: { default: 'row_source' } },
			{ field: 'tokens', title: 'Token', width: 100, slots: { default: 'row_tokens' } },
			{ field: 'duration', title: '耗时', width: 90, slots: { default: 'row_duration' } },
			{ field: 'attemptCount', title: '尝试', width: 70, align: 'center', headerAlign: 'center' },
			{ field: 'time', title: '调用时间', width: 170, slots: { default: 'row_time' } },
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

// 表格事件
const gridEvents: VxeGridListeners<AgentTraceDto> = {};

// 查询api：后端是 GET + query 参数，逐位传值（高级筛选参数留空）
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const q = state.queryParams;
	const res = await getAPI(AgentTraceApi).apiAgentTracePageDataGet(
		q.traceType,
		q.status,
		q.appId,
		undefined, // chatId
		undefined, // historyId
		q.modelName || undefined,
		undefined, // source
		q.range?.[0],
		q.range?.[1],
		page.currentPage,
		page.pageSize
	);
	const items = res.data.result?.items ?? [];
	heroStats.total = res.data.result?.total ?? 0;
	heroStats.fail = items.filter((r) => r.status === 1).length;
	heroStats.tokens = items.reduce((sum, r) => sum + (r.totalTokenCount ?? 0), 0);
	return res;
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	try {
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	} finally {
		options.loading = false;
	}
	// 趋势跟随查询条件里的智能体与模型，一起刷新
	loadTrend();
};

/** 趋势数据：天数在卡内切换，智能体/模型跟随主查询条件 */
const loadTrend = async () => {
	const q = state.queryParams;
	try {
		const res = await getAPI(AgentTraceApi).apiAgentTraceTrendGet(state.trendDays, q.appId, q.modelName || undefined);
		state.trendPoints = res.data.result ?? [];
	} catch {
		// 趋势是辅助信息：接口失败时清空图表，不打断列表查询（错误提示已由 axios 拦截器统一弹出）
		state.trendPoints = [];
	}
};

/** 导出用量报表：与列表同筛选条件（分页忽略），后端返回 xlsx 流 */
const handleExport = async () => {
	const q = state.queryParams;
	state.exporting = true;
	try {
		// 生成的签名把高级筛选参数排在 options 之前，responseType 必须放在最后一个参数位，
		// 放错位置会让 axios 按文本解析二进制，导出的 xlsx 直接损坏
		const res = await getAPI(AgentTraceApi).apiAgentTraceExportPost(
			q.traceType,
			q.status,
			q.appId,
			undefined, // chatId
			undefined, // historyId
			q.modelName || undefined,
			undefined, // source
			q.range?.[0],
			q.range?.[1],
			undefined, // page（导出全量，分页参数忽略）
			undefined, // pageSize
			undefined, // field
			undefined, // order
			undefined, // descStr
			undefined, // searchFields
			undefined, // searchKeyword
			undefined, // keyword
			undefined, // filterLogic
			undefined, // filterFilters
			undefined, // filterField
			undefined, // filterOperator
			undefined, // filterValue
			{ responseType: 'blob' }
		);
		downloadByData(res.data as any, getFileName(res.headers));
		ElMessage.success('报表已生成并开始下载');
	} finally {
		state.exporting = false;
	}
};

const resetQuery = () => {
	state.queryParams.traceType = undefined;
	state.queryParams.status = undefined;
	state.queryParams.appId = undefined;
	state.queryParams.modelName = '';
	state.queryParams.range = null;
	handleQuery(true);
};

const openDetail = (row: AgentTraceDto) => {
	state.detail = row;
	state.detailVisible = true;
};

/** 单条删除：后端会按 ParentTraceId 级联删掉其下工具调用明细 */
const handleDelete = async (row: AgentTraceDto) => {
	try {
		await ElMessageBox.confirm('确定要删除该条轨迹吗？模型调用的轨迹会连同其下工具调用明细一起删除。', '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentTraceApi).apiAgentTraceDelete({ ids: [row.id!] });
	ElMessage.success('删除成功');
	handleQuery();
};

/** 批量清理：按保留天数清空过期轨迹，控制表体积 */
const handleCleanup = async () => {
	// 取消时 prompt 会 reject，这里吞掉即可（点取消不该弹红条错误）
	const result = await ElMessageBox.prompt('将删除该保留天数之前的全部轨迹（含工具调用明细），请输入保留天数：', '按天数清理', {
		inputValue: '90',
		inputPattern: /^\d+$/,
		inputErrorMessage: '请输入正整数天数',
		type: 'warning',
	}).catch(() => null);
	if (!result) return;
	const keepDays = Math.max(1, parseInt(result.value, 10));
	const count = await getAPI(AgentTraceApi).apiAgentTraceDelete({ ids: [], keepDays });
	ElMessage.success(`已清理 ${count.data.result ?? 0} 条轨迹`);
	handleQuery(true);
};

onMounted(async () => {
	const res = await getAPI(AgentApi).apiAgentListGet();
	state.agentList = res.data.result ?? [];
	loadTrend();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* 类型胶囊：与状态胶囊同族视觉，但不带底色描边 —— 类型是分类信息，
 * 与「成功/失败」这类需要强调的状态区分开，避免整表被一圈圈色块填满 */
.type-pill {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	line-height: 22px;

	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background-color: currentColor;
	}
}

.type-pill.is-primary {
	color: var(--el-color-primary);
}

.type-pill.is-warning {
	color: var(--el-color-warning);
}

/* 趋势卡：固定高度不参与 flex 拉伸，给表格留足空间 */
.trend-card {
	flex-shrink: 0;
}

.trend-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 12px 16px 0;
}

.trend-head__left {
	display: flex;
	align-items: baseline;
	gap: 10px;
	min-width: 0;
}

.trend-head__title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.trend-head__summary {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.trend-chart {
	height: 272px;
	padding: 4px 8px 8px;
}

/* 空态撑同样高度，避免卡片高度跳动 */
.trend-chart--empty {
	display: flex;
	align-items: center;
	justify-content: center;
}

/* 模型/工具 + 智能体两行单元格 */
.target-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.target-cell__main {
	font-size: 13px;
	line-height: 16px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.target-cell .cell-sub {
	font-size: 12px;
	line-height: 16px;
	color: var(--el-text-color-secondary);
}

.source-text {
	font-size: 12px;
	color: var(--el-text-color-regular);
}

/* 数字列统一等宽字体，Token 与耗时的位数变化不跳动 */
.token-text,
.duration-text {
	font-variant-numeric: tabular-nums;
}

/* 超过 30s 的慢调用标警示色，便于发现卡顿 */
.duration-text.is-slow {
	color: var(--el-color-warning);
}

.time-text {
	font-variant-numeric: tabular-nums;
	font-size: 13px;
}

/* 详情抽屉：失败原因整段可读，不截断 */
.detail-desc {
	margin-bottom: 16px;
}

.fail-reason {
	color: var(--el-color-danger);
	word-break: break-all;
}

.detail-block {
	margin-bottom: 14px;
}

.detail-block__title {
	margin-bottom: 6px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

/* 入参/返回摘要用 pre 保留原始 JSON 缩进，可横向滚动不撑破抽屉 */
.detail-pre {
	max-height: 260px;
	margin: 0;
	padding: 10px 12px;
	font-size: 12px;
	line-height: 1.6;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-light);
	border-radius: 6px;
	overflow: auto;
	white-space: pre-wrap;
	word-break: break-all;
}
</style>
