<template>
	<div class="jf-page jf-workbench">
		<el-card shadow="hover" class="jf-workbench-card">
			<template #header>
				<div>工作台</div>
			</template>
			<!-- 统计卡片（四端门面口径：分页 recordCount，无额外端点依赖） -->
			<div class="wb-cards">
				<div v-for="c in cards" :key="c.key" class="wb-card" @click="emit('goto', c.key)">
					<div class="wb-card__num">{{ c.count == null ? '-' : c.count }}</div>
					<div class="wb-card__label">{{ c.label }}</div>
				</div>
			</div>
		</el-card>

		<!-- 全局概览（issues/103 stats 三 action，全局口径） -->
		<el-card shadow="hover" class="jf-overview-card">
			<template #header>
				<div>全局概览</div>
			</template>
			<div v-if="statsErr" class="jf-empty">全局统计暂不可用：{{ statsErr }}</div>
			<template v-else-if="ov">
				<div class="wb-cards wb-cards--stats">
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ ov.total }}</div>
						<div class="wb-card__label">实例总数</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ ov.inProgress }}</div>
						<div class="wb-card__label">进行中</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ ov.completed }}</div>
						<div class="wb-card__label">已完成</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ ov.todayNew }}</div>
						<div class="wb-card__label">今日新增</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ fmtDuration(ov.avgDurationSeconds) }}</div>
						<div class="wb-card__label">平均办结时长</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ fmtRate(ov.rejectRate) }}</div>
						<div class="wb-card__label">驳回率</div>
					</div>
					<div class="wb-card wb-card--plain">
						<div class="wb-card__num">{{ ov.pendingTaskCount }}</div>
						<div class="wb-card__label">积压任务</div>
					</div>
					<div v-if="ov.overdueTaskCount > 0" class="wb-card wb-card--plain">
						<div class="wb-card__num wb-card__num--warn">{{ ov.overdueTaskCount }}</div>
						<div class="wb-card__label">逾期任务</div>
					</div>
				</div>

				<div class="wb-stats-grid">
					<!-- 近 7 天发起/办结趋势（day 粒度，连续桶补 0） -->
					<div class="wb-panel">
						<h4 class="wb-panel__title">近 7 天趋势</h4>
						<div v-if="trend.length" class="wb-trend">
							<div v-for="t in trend" :key="t.bucket" class="wb-trend__col" :title="`${t.bucket}：发起 ${t.started} / 办结 ${t.finished}`">
								<div class="wb-trend__bars">
									<div class="wb-trend__bar wb-trend__bar--s" :style="{ height: barH(t.started) }"></div>
									<div class="wb-trend__bar wb-trend__bar--f" :style="{ height: barH(t.finished) }"></div>
								</div>
								<div class="wb-trend__label">{{ t.bucket.slice(5) }}</div>
							</div>
						</div>
						<div v-else class="jf-empty">暂无趋势数据</div>
					</div>

					<!-- 流程 Top 5（含平均时长） -->
					<div class="wb-panel">
						<h4 class="wb-panel__title">流程 Top 5</h4>
						<el-table :data="topDefines" row-key="key" size="small" stripe border style="width: 100%" :show-header="false">
							<el-table-column label="流程" min-width="120" align="center">
								<template #default="{ row }">{{ row.label || row.key }}</template>
							</el-table-column>
							<el-table-column label="实例数" min-width="70" align="center">
								<template #default="{ row }"
									><span class="jf-num">{{ row.count }}</span></template
								>
							</el-table-column>
							<el-table-column label="平均时长" min-width="90" align="center">
								<template #default="{ row }"
									><span class="jf-muted">{{ row.avgDurationSeconds != null ? fmtDuration(row.avgDurationSeconds) : '-' }}</span></template
								>
							</el-table-column>
							<template #empty><span class="jf-empty">暂无实例数据</span></template>
						</el-table>
					</div>

					<!-- 状态分布（label 走状态字典映射） -->
					<div class="wb-panel">
						<h4 class="wb-panel__title">状态分布</h4>
						<div v-if="stateDist.length" class="wb-dist">
							<div v-for="g in stateDist" :key="g.key" class="wb-dist__row" :title="`${stateLabel(g.key)}：${g.count}`">
								<span class="wb-dist__label">{{ stateLabel(g.key) }}</span>
								<span class="wb-dist__track"><span class="wb-dist__bar" :style="{ width: distPct(g.count) }"></span></span>
								<span class="wb-dist__num">{{ g.count }}</span>
							</div>
						</div>
						<div v-else class="jf-empty">暂无实例数据</div>
					</div>
				</div>
			</template>
		</el-card>

		<!-- 最近待办 -->
		<el-card shadow="hover" class="jf-recent-card">
			<template #header>
				<div>
					最近待办
					<el-button v-if="todoTotal > recentRows.length" size="small" plain :icon="ArrowRight" @click="emit('goto', 'todo')">查看全部 {{ todoTotal }} 条</el-button>
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<template v-else>
				<el-table :data="recentRows" row-key="id" stripe border style="width: 100%">
					<el-table-column label="流程" min-width="140">
						<template #default="{ row }">{{ row.processDefineDisplayName || '-' }}</template>
					</el-table-column>
					<el-table-column label="任务" min-width="160">
						<template #default="{ row }">{{ row.displayName }}</template>
					</el-table-column>
					<el-table-column label="时间" min-width="160">
						<template #default="{ row }"
							><span class="jf-muted">{{ fmtTime(row.createTime, true) }}</span></template
						>
					</el-table-column>
					<el-table-column label="操作" min-width="180">
						<template #default="{ row }">
							<div class="jf-btn-row">
								<el-button type="primary" size="small" :icon="Edit" @click="openApprove(row.id)">办理</el-button>
								<el-button size="small" plain :icon="View" @click="openDetail(row.processInstanceId)">详情</el-button>
							</div>
						</template>
					</el-table-column>
					<template #empty
						><span class="jf-empty"
							>暂无待办，喝口茶吧 <el-icon :size="14"><Coffee /></el-icon></span
					></template>
				</el-table>
			</template>
		</el-card>

		<!-- 办理/详情抽屉 -->
		<ApproveDrawer v-model:visible="approveVisible" :task-id="approveTaskId" @changed="reload" />
		<InstanceDetailDrawer v-model:visible="detailVisible" :instance-id="detailInstanceId" />
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import ApproveDrawer from '../core/drawers/ApproveDrawer.vue';
import InstanceDetailDrawer from '../core/drawers/InstanceDetailDrawer.vue';
import '../core/style.css';
import { fmtTime } from '../core/helpers';
import type { TaskRow, InstanceRow, PageResult, StatsOverview, StatsTrendRow, StatsGroupRow } from '../core/types';
import { Edit, View, ArrowRight, Coffee } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowStatApi, JeeflowTaskApi, JeeflowInstanceApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfWorkbenchPage' });

const emit = defineEmits<{
	/** 卡片点击 → 宿主切换菜单（todo/done/mine/cc） */
	goto: [key: string];
}>();

const loading = ref(false);
const recentRows = ref<TaskRow[]>([]);
const todoTotal = ref(0);

const cards = reactive([
	{ key: 'todo', label: '待办', count: null as number | null },
	{ key: 'done', label: '在办（已处理）', count: null as number | null },
	{ key: 'mine', label: '我发起的', count: null as number | null },
	{ key: 'cc', label: '抄送我的', count: null as number | null },
]);

// ── 全局统计（issues/103；个人视角数字仍走上方 recordCount 卡）──
const ov = ref<StatsOverview | null>(null);
const trend = ref<StatsTrendRow[]>([]);
const topDefines = ref<StatsGroupRow[]>([]);
const stateDist = ref<StatsGroupRow[]>([]);
const statsErr = ref('');
/** 实例状态字典（spec 07 wf_process_instance_state；group/state 的 label 可空，前端兜底） */
const STATE_LABELS: Record<string, string> = {
	'10': '进行中',
	'20': '已完成',
	'30': '已撤回',
	'40': '强行终止',
	'45': '已拒绝',
	'50': '挂起',
};
const stateLabel = (key: string) => STATE_LABELS[key] ?? key;

function fmtDuration(seconds?: number | null): string {
	if (seconds == null) return '-';
	if (seconds < 60) return `${seconds} 秒`;
	if (seconds < 3600) return `${Math.round(seconds / 60)} 分钟`;
	if (seconds < 86400) return `${(seconds / 3600).toFixed(1)} 小时`;
	return `${(seconds / 86400).toFixed(1)} 天`;
}
const fmtRate = (r?: number | null) => (r == null ? '-' : `${(r * 100).toFixed(1)}%`);
const trendMax = () => Math.max(1, ...trend.value.map((t) => Math.max(t.started, t.finished)));
const barH = (n: number) => `${Math.round((n / trendMax()) * 100)}%`;
const distMax = () => Math.max(1, ...stateDist.value.map((g) => g.count));
const distPct = (n: number) => `${Math.round((n / distMax()) * 100)}%`;

/** 近 7 天窗口（本地时区，yyyy-MM-dd HH:mm:ss） */
function last7Days(): { start: string; end: string } {
	const fmtD = (d: Date) => {
		const p = (n: number) => String(n).padStart(2, '0');
		return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
	};
	const end = new Date();
	const start = new Date();
	start.setDate(end.getDate() - 6);
	return { start: `${fmtD(start)} 00:00:00`, end: `${fmtD(end)} 23:59:59` };
}

const approveVisible = ref(false);
const approveTaskId = ref<string | null>(null);
const detailVisible = ref(false);
const detailInstanceId = ref<string | null>(null);

async function reload() {
	loading.value = true;
	try {
		const win = last7Days();
		// 八路并行 + 单路失败不拖垮整体（allSettled）；后四路为 stats 全局口径（issues/103）
		const [todo, done, mine, cc, overview, tr, defines, states] = await Promise.allSettled([
			// 最近待办：改用项目标准接口（api/jeeflowTask/todoList；载荷在 res.data.result）
			getAPI(JeeflowTaskApi)
				.apiJeeflowTaskTodoListPost({ pageNum: 1, pageSize: 5 })
				.then((r) => (r.data?.result ?? { rows: [], recordCount: 0 }) as unknown as PageResult<TaskRow>),
			// 在办（已处理）：改用项目标准接口（api/jeeflowTask/doneList；载荷在 res.data.result）
			getAPI(JeeflowTaskApi)
				.apiJeeflowTaskDoneListPost({ pageNum: 1, pageSize: 1 })
				.then((r) => (r.data?.result ?? { rows: [], recordCount: 0 }) as unknown as PageResult<TaskRow>),
			// 我发起的：改用项目标准接口（api/jeeflowInstance/page；载荷在 res.data.result）
			getAPI(JeeflowInstanceApi)
				.apiJeeflowInstancePagePost({ pageNum: 1, pageSize: 1 })
				.then((r) => (r.data?.result ?? { rows: [], recordCount: 0 }) as unknown as PageResult<InstanceRow>),
			// 抄送我的：改用项目标准接口（api/jeeflowInstance/ccList；载荷在 res.data.result）
			getAPI(JeeflowInstanceApi)
				.apiJeeflowInstanceCcListPost({ pageNum: 1, pageSize: 1 })
				.then((r) => (r.data?.result ?? { rows: [], recordCount: 0 }) as unknown as PageResult<InstanceRow>),
			// 指标卡总览：改用项目标准接口（api/jeeflowStat/overview；getAPI 走系统模块的拦截器，载荷在 res.data.result）
			getAPI(JeeflowStatApi)
				.apiJeeflowStatOverviewPost({})
				.then((r) => (r.data?.result ?? null) as unknown as StatsOverview),
			// 趋势统计：改用项目标准接口（api/jeeflowStat/trend；载荷在 res.data.result）
			getAPI(JeeflowStatApi)
				.apiJeeflowStatTrendPost({ ...win, granularity: 'day' })
				.then((r) => (r.data?.result ?? []) as unknown as StatsTrendRow[]),
			// 维度分组：改用项目标准接口（api/jeeflowStat/group；载荷在 res.data.result）
			getAPI(JeeflowStatApi)
				.apiJeeflowStatGroupPost({ dimension: 'define', limit: 5 })
				.then((r) => (r.data?.result ?? []) as unknown as StatsGroupRow[]),
			getAPI(JeeflowStatApi)
				.apiJeeflowStatGroupPost({ dimension: 'state' })
				.then((r) => (r.data?.result ?? []) as unknown as StatsGroupRow[]),
		]);
		console.log('🚀 ~ reload ~ overview:', overview);
		if (todo.status === 'fulfilled') {
			recentRows.value = todo.value.rows;
			todoTotal.value = todo.value.recordCount;
			cards[0].count = todo.value.recordCount;
		}
		if (done.status === 'fulfilled') cards[1].count = done.value.recordCount;
		if (mine.status === 'fulfilled') cards[2].count = mine.value.recordCount;
		if (cc.status === 'fulfilled') cards[3].count = cc.value.recordCount;
		if (overview.status === 'fulfilled') ov.value = overview.value;
		if (tr.status === 'fulfilled') trend.value = tr.value;
		if (defines.status === 'fulfilled') topDefines.value = defines.value;
		if (states.status === 'fulfilled') stateDist.value = states.value;
		// stats 四路全失败才报错（部分失败静默降级，引擎未实现时不阻塞工作台）
		const statsFailures = [overview, tr, defines, states].filter((r) => r.status === 'rejected');
		if (statsFailures.length === 4) {
			statsErr.value = statsFailures[0].status === 'rejected' ? (statsFailures[0] as PromiseRejectedResult).reason?.message : '未知错误';
		} else {
			statsErr.value = '';
		}
	} finally {
		loading.value = false;
	}
}

function openApprove(taskId: string) {
	approveTaskId.value = taskId;
	approveVisible.value = true;
}

function openDetail(instanceId: string) {
	detailInstanceId.value = instanceId;
	detailVisible.value = true;
}

onMounted(reload);
</script>

<style scoped>
.wb-cards {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
	gap: 12px;
}
.wb-card {
	background: var(--jf-bg, #fff);
	border: 1px solid var(--jf-border, #f0f0f0);
	border-radius: 10px;
	padding: 16px;
	cursor: pointer;
	transition: all 0.15s;
}
.wb-card:hover {
	box-shadow: 0 4px 14px rgba(0, 0, 0, 0.08);
	border-color: var(--jf-primary, #1677ff);
}
.wb-card__num {
	font-size: 26px;
	font-weight: 700;
	color: var(--jf-primary, #1677ff);
}
.wb-card__label {
	font-size: 13px;
	color: #666;
	margin-top: 4px;
}
.jf-workbench-card {
	margin-bottom: 5px;
}
.jf-overview-card {
	margin-bottom: 5px;
}
.jf-recent-card {
	margin-bottom: 5px;
}

.wb-cards--stats {
	margin-bottom: 12px;
}
.wb-card--plain {
	cursor: default;
}
.wb-card--plain:hover {
	border-color: var(--jf-border, #f0f0f0);
	box-shadow: none;
}
.wb-card__num--warn {
	color: #fa8c16;
}
.wb-stats-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
	gap: 12px;
}
.wb-panel {
	background: var(--jf-bg, #fff);
	border: 1px solid var(--jf-border, #f0f0f0);
	border-radius: 10px;
	padding: 12px 14px;
}
.wb-panel__title {
	margin: 0 0 10px;
	font-size: 13px;
	font-weight: 600;
}
.jf-num {
	text-align: right;
	font-variant-numeric: tabular-nums;
}
/* 趋势双序列柱状（纯 CSS） */
.wb-trend {
	display: flex;
	align-items: flex-end;
	gap: 6px;
	height: 120px;
}
.wb-trend__col {
	flex: 1;
	display: flex;
	flex-direction: column;
	align-items: center;
	height: 100%;
}
.wb-trend__bars {
	flex: 1;
	display: flex;
	align-items: flex-end;
	gap: 2px;
	width: 100%;
	justify-content: center;
}
.wb-trend__bar {
	width: 9px;
	border-radius: 2px 2px 0 0;
	min-height: 2px;
}
.wb-trend__bar--s {
	background: var(--jf-primary, #1677ff);
}
.wb-trend__bar--f {
	background: #52c41a;
}
.wb-trend__label {
	font-size: 11px;
	color: #999;
	margin-top: 4px;
}
/* 状态分布横向条 */
.wb-dist {
	display: flex;
	flex-direction: column;
	gap: 8px;
}
.wb-dist__row {
	display: grid;
	grid-template-columns: 70px 1fr 40px;
	align-items: center;
	gap: 8px;
	font-size: 13px;
}
.wb-dist__label {
	color: #555;
}
.wb-dist__track {
	background: #f5f5f5;
	border-radius: 4px;
	height: 12px;
	overflow: hidden;
}
.wb-dist__bar {
	display: block;
	height: 100%;
	background: var(--jf-primary, #1677ff);
	border-radius: 4px;
}
.wb-dist__num {
	text-align: right;
	font-variant-numeric: tabular-nums;
}
</style>
