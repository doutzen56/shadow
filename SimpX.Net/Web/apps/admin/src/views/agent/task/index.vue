<template>
	<div class="agents-task-container">
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
					<h2 class="page-hero__title">我的AI任务</h2>
					<p class="page-hero__desc">智能体在对话中创建的周期任务会在这里调度执行（如“每天早上9点总结AI资讯”），执行结果以系统消息的形式发送给你。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ taskStats.total }}</span>
							<span class="page-hero__stat-k">任务总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ taskStats.scheduled }}</span>
							<span class="page-hero__stat-k">已调度</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ taskStats.types }}</span>
							<span class="page-hero__stat-k">任务类型</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty :image-size="160" description="暂无自动任务，可在对话中让智能体帮你创建周期任务" v-if="!state.tableData.length" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :lg="8" :xl="6" v-for="item in state.tableData" :key="item.jobId">
						<el-card shadow="never" class="entity-card entity-card--accent">
							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-AlarmClock /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ item.name }}</div>
									<div class="entity-card__sub">{{ item.timeZone }}</div>
								</div>
								<el-tag size="small" type="primary" effect="plain">{{ item.type }}</el-tag>
							</div>

							<div class="entity-card__body">
								<!-- Cron 表达式单独一块、等宽字体：扫一眼就能认出执行节奏，比混在时间列里清楚 -->
								<div class="task-cron">
									<el-icon><ele-Timer /></el-icon>
									<span>{{ item.cron }}</span>
								</div>
							</div>

							<div class="entity-card__meta task-times">
								<span
									>下次执行 <b class="task-time">{{ item.next || '-' }}</b></span
								>
								<span
									>上次执行 <b class="task-time">{{ item.last || '-' }}</b></span
								>
							</div>

							<div class="entity-card__foot">
								<el-tag size="small" type="success" effect="plain" v-if="item.jobId">已调度</el-tag>
								<div class="entity-card__actions">
									<el-button
										icon="ele-VideoPlay"
										text
										type="primary"
										:loading="isPending(item, 'trigger')"
										:disabled="isPending(item, 'remove')"
										@click="handleTrigger(item)"
										v-auth="'agent/task/triggerCronTask'"
									>
										执行
									</el-button>
									<el-button
										icon="ele-Delete"
										text
										type="danger"
										:loading="isPending(item, 'remove')"
										:disabled="isPending(item, 'trigger')"
										@click="handleRemove(item)"
										v-auth="'agent/task/removeCronTask'"
									>
										删除
									</el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="agentTask">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { AgentTaskApi } from '/@/api-services/agent/api';
import { AgentTaskItemDto } from '/@/api-services/agent/models';

/** 任务列表来自调度器（JobId 唯一），页面只做展示与触发/删除，不提供新增 */
// 任务列表与加载状态
const state = reactive({
	loading: false,
	tableData: [] as AgentTaskItemDto[],
	// 行级动作进行中：只锁当前卡片，别让整页 loading 遮住其它任务
	pending: null as { name: string; action: 'trigger' | 'remove' } | null,
});

const isPending = (row: AgentTaskItemDto, action: 'trigger' | 'remove') => state.pending?.name === row.name && state.pending?.action === action;

/** 行级动作统一收口：加锁 → 校验确认 → 调接口 → 刷新，异常时释放锁，避免按钮永久转圈 */
const runRowAction = async (row: AgentTaskItemDto, action: 'trigger' | 'remove', confirmText: string, api: (name: string) => Promise<{ data: { result?: boolean } }>, successText: string, failText: string) => {
	if (state.pending) return;
	try {
		await ElMessageBox.confirm(confirmText, '提示', { type: 'warning' });
	} catch {
		return;
	}
	state.pending = { name: row.name!, action };
	try {
		const res = await api(row.name!);
		ElMessage[res.data.result ? 'success' : 'error'](res.data.result ? successText : failText);
		await handleQuery();
	} finally {
		state.pending = null;
	}
};

// 头图统计：任务无分页接口，直接在列表上汇总
const taskStats = computed(() => {
	const list = state.tableData ?? [];
	return {
		total: list.length,
		scheduled: list.filter((x) => x.jobId).length,
		types: new Set(list.map((x) => x.type).filter(Boolean)).size,
	};
});

// 查询任务列表
const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await getAPI(AgentTaskApi).apiAgentTaskListGet();
		state.tableData = res.data.result ?? [];
	} finally {
		state.loading = false;
	}
};

/** 立即触发一次：任务名即调度器里的唯一键，按名字触发 */
const handleTrigger = (row: AgentTaskItemDto) =>
	runRowAction(row, 'trigger', `确定要立即执行任务「${row.name}」吗？`, (name) => getAPI(AgentTaskApi).apiAgentTaskTriggerPost(name), '任务已触发', '触发失败');

/** 删除任务：同时移除调度器里的 Job，删除后需要让智能体重新创建才会再出现 */
const handleRemove = (row: AgentTaskItemDto) =>
	runRowAction(row, 'remove', `确定要删除任务「${row.name}」吗？`, (name) => getAPI(AgentTaskApi).apiAgentTaskDeletePost(name), '删除成功', '删除失败');

// 首次进入加载任务列表
onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.full-table {
	margin-top: 14px;
}

/* 卡片 hover 顶部色带：卡片本身只上浮 4px 动静偏小，这条色带让密集卡片墙里「当前指向哪张」更明确 */
.entity-card--accent::before {
	content: '';
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	height: 2px;
	background-image: linear-gradient(90deg, var(--card-accent), transparent 72%);
	opacity: 0;
	transition: opacity 0.25s ease;
	pointer-events: none;
}

.entity-card--accent:hover::before {
	opacity: 0.9;
}

/* 图标底色沿用 .entity-card__icon 的主题色，不单独配色 */

.task-cron {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 7px 10px;
	border-radius: 6px;
	background-color: var(--el-fill-color-lighter);
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-regular);

	span {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.task-times {
	flex-direction: column;
	gap: 2px;
}

/* 执行时间是这张卡里最需要"对齐比对"的信息，给等宽字体 */
.task-time {
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
}
</style>
