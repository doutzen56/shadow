<template>
	<el-drawer v-model="visible" size="760px" :destroy-on-close="true" class="wf-run-drawer" @closed="handleClosed">
		<template #header>
			<div class="wf-run-drawer__head">
				<div class="wf-run-drawer__title">
					<span class="wf-run-drawer__icon"><i class="ri-route-line" /></span>
					<div>
						<div class="wf-run-drawer__name">运行详情 <span class="wf-run-drawer__id">#{{ run?.id ?? runId }}</span></div>
						<div class="wf-run-drawer__sub">{{ run?.workflowName || '未知工作流' }} · 定义版本 v{{ run?.workflowVersion ?? '-' }}</div>
					</div>
				</div>
				<el-tag v-if="run" effect="light" :type="runStatusMeta.type">{{ runStatusMeta.label }}</el-tag>
			</div>
		</template>

		<div v-loading="loading" class="wf-run-drawer__body">
			<el-empty v-if="!loading && !run" :image-size="120" description="运行记录不存在或已被删除" />

			<template v-else-if="run">
				<!-- 待审批：把审批动作放在最上方，避免埋在长步骤列表后面 -->
				<div v-if="run.status === WORKFLOW_RUN_STATUS.WaitingApproval" class="wf-approval">
					<div class="wf-approval__head">
						<i class="ri-user-voice-line" />
						<span>流程已暂停，等待人工审批</span>
					</div>
					<div class="wf-approval__desc">审批通过后从审批节点的下一个节点继续执行；拒绝会终止流程并标记为已取消。</div>
					<el-input v-model="approvalComment" type="textarea" :rows="2" maxlength="500" placeholder="审批意见（可选，会写入审批节点记录）" />
					<div class="wf-approval__actions">
						<el-button type="success" icon="ele-Select" :loading="approving" @click="handleApprove(1)" v-auth="'agent/workflowRun/approve'"> 通过 </el-button>
						<el-button type="danger" icon="ele-CloseBold" :loading="approving" @click="handleApprove(2)" v-auth="'agent/workflowRun/approve'"> 拒绝 </el-button>
					</div>
				</div>

				<!-- 概要 -->
				<div class="wf-summary">
					<div class="wf-summary__item">
						<span>触发来源</span><b>{{ run.triggerSource || '-' }}</b>
					</div>
					<div class="wf-summary__item">
						<span>开始时间</span><b>{{ formatTime(run.startTime) }}</b>
					</div>
					<div class="wf-summary__item">
						<span>结束时间</span><b>{{ formatTime(run.endTime) }}</b>
					</div>
					<div class="wf-summary__item">
						<span>总耗时</span><b>{{ formatDuration(run.durationMs) }}</b>
					</div>
					<div class="wf-summary__item">
						<span>节点步骤</span><b>{{ steps.length }} / {{ run.totalSteps ?? 0 }}</b>
					</div>
					<div class="wf-summary__item">
						<span>Token</span><b>{{ formatNumber(run.totalTokenCount) }}</b>
					</div>
				</div>

				<!-- 输入 / 输出 / 错误 -->
				<div class="wf-io">
					<div class="wf-io__block">
						<div class="wf-io__title">运行输入</div>
						<pre class="wf-pre">{{ run.input || '（空）' }}</pre>
					</div>
					<div v-if="run.output" class="wf-io__block">
						<div class="wf-io__title">运行输出</div>
						<pre class="wf-pre">{{ run.output }}</pre>
					</div>
					<div v-if="run.error" class="wf-io__block is-error">
						<div class="wf-io__title">失败原因</div>
						<pre class="wf-pre">{{ run.error }}</pre>
					</div>
				</div>

				<!-- 步骤时间线 -->
				<div class="wf-steps">
					<div class="wf-steps__head">
						<span class="wf-steps__title">执行步骤</span>
						<span class="wf-steps__hint">按执行顺序排列；并行分支的步骤带同样的父步骤</span>
					</div>

					<el-empty v-if="!steps.length" :image-size="90" description="流程尚未产生任何步骤" />

					<div v-for="(step, index) in steps" :key="step.id ?? index" class="wf-step" :class="`is-${stepTone(step.status)}`">
						<div class="wf-step__rail">
							<span class="wf-step__dot"></span>
						</div>
						<div class="wf-step__body">
							<div class="wf-step__head">
								<span class="wf-step__seq">#{{ step.seq ?? index + 1 }}</span>
								<span class="wf-step__name">{{ step.nodeName || step.nodeId }}</span>
								<el-tag size="small" effect="light" :type="stepStatusMeta(step.status).type">{{ stepStatusMeta(step.status).label }}</el-tag>
								<span class="wf-step__duration">{{ formatDuration(step.durationMs) }}</span>
							</div>

							<div class="wf-step__meta">
								<span>{{ nodeTypeLabel(step.nodeType) }}</span>
								<span v-if="step.agentName">{{ step.agentName }}</span>
								<span v-if="step.modelName">{{ step.modelName }}</span>
								<span v-if="step.totalTokenCount">Token {{ formatNumber(step.totalTokenCount) }}</span>
								<span v-if="step.attemptCount && step.attemptCount > 1">第 {{ step.attemptCount }} 次尝试</span>
							</div>

							<div v-if="step.error" class="wf-step__error">{{ step.error }}</div>

							<div v-if="step.inputText" class="wf-step__block">
								<div class="wf-step__block-head" @click="toggleInput(step.id)">
									<span>输入</span>
									<span class="wf-step__toggle">{{ isInputExpanded(step.id) ? '收起' : '展开' }}</span>
								</div>
								<pre v-show="isInputExpanded(step.id)" class="wf-pre">{{ step.inputText }}</pre>
								<div v-if="!isInputExpanded(step.id)" class="wf-step__preview">{{ preview(step.inputText) }}</div>
							</div>

							<div v-if="step.outputText" class="wf-step__block">
								<div class="wf-step__block-head" @click="toggleOutput(step.id)">
									<span>输出</span>
									<span class="wf-step__toggle">{{ isOutputExpanded(step.id) ? '收起' : '展开' }}</span>
								</div>
								<pre v-show="isOutputExpanded(step.id)" class="wf-pre">{{ step.outputText }}</pre>
								<div v-if="!isOutputExpanded(step.id)" class="wf-step__preview">{{ preview(step.outputText) }}</div>
							</div>
						</div>
					</div>
				</div>
			</template>
		</div>

		<template #footer>
			<div class="dialog-footer">
				<el-button v-if="canCancel" icon="ele-VideoPause" @click="handleCancel" v-auth="'agent/workflowRun/cancel'"> 取消运行 </el-button>
				<el-button v-if="canRetry" icon="ele-RefreshRight" @click="handleRetry"> 重试 </el-button>
				<el-button @click="visible = false">关闭</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script lang="ts" setup name="workflowRunDetailDrawer">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { AgentWorkflowRunApi } from '/@/api-services/agent/api';
import { AgentWorkflowApprovalActionEnum, AgentWorkflowRunDto, AgentWorkflowStepDto } from '/@/api-services/agent/models';
import {
	RUN_STATUS_META,
	STEP_STATUS_META,
	WORKFLOW_NODE_META,
	WORKFLOW_RUN_STATUS,
	WorkflowNodeType,
	isRunActive,
} from '../../workflow/types';

const props = defineProps<{
	modelValue: boolean;
	runId?: number;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'changed'): void;
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const loading = ref(false);
const approving = ref(false);
const run = ref<AgentWorkflowRunDto | null>(null);
const steps = ref<AgentWorkflowStepDto[]>([]);
const approvalComment = ref('');
const expandedInputs = ref(new Set<number>());
const expandedOutputs = ref(new Set<number>());

let timer: ReturnType<typeof setInterval> | null = null;

const runStatusMeta = computed(() => RUN_STATUS_META[Number(run.value?.status ?? 0)] ?? RUN_STATUS_META[0]);
const canCancel = computed(() => isRunActive(run.value?.status));
const canRetry = computed(() => !!run.value && !isRunActive(run.value.status));

const stepStatusMeta = (status?: number) => STEP_STATUS_META[Number(status ?? 0)] ?? STEP_STATUS_META[0];
const stepTone = (status?: number) => {
	const type = stepStatusMeta(status).type;
	return type === 'primary' ? 'running' : type;
};

const nodeTypeLabel = (nodeType?: number) => WORKFLOW_NODE_META[(nodeType ?? 0) as WorkflowNodeType]?.label ?? '节点';

const formatTime = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 19) : '-');

const formatNumber = (value?: number | null) => (value === null || value === undefined ? '-' : String(value).replace(/\B(?=(\d{3})+(?!\d))/g, ','));

const formatDuration = (ms?: number | null) => {
	if (ms === null || ms === undefined) return '-';
	if (ms < 1000) return `${ms} ms`;
	if (ms < 60000) return `${(ms / 1000).toFixed(1)} s`;
	const minutes = Math.floor(ms / 60000);
	const seconds = Math.round((ms % 60000) / 1000);
	return `${minutes} m ${seconds} s`;
};

const preview = (text?: string | null) => {
	const value = String(text ?? '').replace(/\s+/g, ' ').trim();
	return value.length > 160 ? `${value.substring(0, 160)}…` : value;
};

const isInputExpanded = (id?: number) => !!id && expandedInputs.value.has(id);
const isOutputExpanded = (id?: number) => !!id && expandedOutputs.value.has(id);

const toggleInput = (id?: number) => {
	if (!id) return;
	const next = new Set(expandedInputs.value);
	next.has(id) ? next.delete(id) : next.add(id);
	expandedInputs.value = next;
};

const toggleOutput = (id?: number) => {
	if (!id) return;
	const next = new Set(expandedOutputs.value);
	next.has(id) ? next.delete(id) : next.add(id);
	expandedOutputs.value = next;
};

const loadDetail = async (silent = false) => {
	if (!props.runId) return;
	if (!silent) loading.value = true;
	try {
		const res = await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunDetailRunIdGet(props.runId);
		const detail = res.data.result;
		run.value = detail?.run ?? null;
		steps.value = [...(detail?.steps ?? [])].sort((a, b) => (a.seq ?? 0) - (b.seq ?? 0));
		syncPolling();
	} finally {
		if (!silent) loading.value = false;
	}
};

/** 运行中/待审批时轻量轮询，终态自动停止 */
const syncPolling = () => {
	const active = visible.value && isRunActive(run.value?.status);
	if (active && !timer) {
		timer = setInterval(() => loadDetail(true), 3500);
	}
	if (!active && timer) {
		clearInterval(timer);
		timer = null;
	}
};

const stopPolling = () => {
	if (timer) {
		clearInterval(timer);
		timer = null;
	}
};

const handleApprove = async (action: number) => {
	if (!run.value?.id) return;
	approving.value = true;
	try {
		await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunApprovePost({
			runId: run.value.id,
			action: action as AgentWorkflowApprovalActionEnum,
			comment: approvalComment.value || undefined,
		});
		ElMessage.success(action === 1 ? '已通过，流程继续执行' : '已拒绝，流程终止');
		approvalComment.value = '';
		emit('changed');
		await loadDetail(true);
	} catch (error: any) {
		ElMessage.error(error?.message ?? '审批操作失败');
	} finally {
		approving.value = false;
	}
};

const handleCancel = async () => {
	if (!run.value?.id) return;
	try {
		await ElMessageBox.confirm('确定取消这次运行吗？正在执行的节点会在当前调用返回后停止。', '取消运行', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunCancelRunIdPost(run.value.id);
	ElMessage.success('已提交取消');
	emit('changed');
	await loadDetail(true);
};

const handleRetry = async () => {
	if (!run.value?.id) return;
	try {
		await ElMessageBox.confirm('将以相同的输入与初始变量重新发起一次运行，确定重试吗？', '重试运行', { type: 'info' });
	} catch {
		return;
	}
	const res = await getAPI(AgentWorkflowRunApi).apiAgentWorkflowRunRetryRunIdPost(run.value.id);
	const newRunId = Number(res.data.result ?? 0);
	ElMessage.success('已发起重试');
	emit('changed');
	if (newRunId) {
		emit('update:modelValue', false);
	}
};

watch(
	() => [props.modelValue, props.runId] as const,
	([open]) => {
		if (open) {
			approvalComment.value = '';
			expandedInputs.value = new Set();
			expandedOutputs.value = new Set();
			loadDetail();
		} else {
			stopPolling();
		}
	},
	{ immediate: true }
);

const handleClosed = () => {
	stopPolling();
	run.value = null;
	steps.value = [];
};

onBeforeUnmount(stopPolling);
</script>

<style scoped lang="scss">
.wf-run-drawer__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
}

.wf-run-drawer__title {
	display: flex;
	align-items: center;
	gap: 10px;
}

.wf-run-drawer__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	font-size: 18px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.wf-run-drawer__name {
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.wf-run-drawer__id {
	font-family: monospace;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.wf-run-drawer__sub {
	margin-top: 2px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.wf-run-drawer__body {
	display: flex;
	flex-direction: column;
	gap: 14px;
}

.wf-approval {
	padding: 14px;
	border-radius: 10px;
	border: 1px solid var(--el-color-warning-light-5);
	background-color: var(--el-color-warning-light-9);

	&__head {
		display: flex;
		align-items: center;
		gap: 6px;
		font-size: 14px;
		font-weight: 600;
		color: var(--el-color-warning);
	}

	&__desc {
		margin: 6px 0 10px;
		font-size: 12px;
		line-height: 18px;
		color: var(--el-text-color-regular);
	}

	&__actions {
		display: flex;
		gap: 8px;
		margin-top: 10px;
	}
}

.wf-summary {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 8px;

	&__item {
		padding: 8px 10px;
		border-radius: 8px;
		background-color: var(--el-fill-color-lighter);

		span {
			display: block;
			font-size: 11px;
			color: var(--el-text-color-secondary);
		}

		b {
			display: block;
			margin-top: 2px;
			font-size: 13px;
			font-weight: 600;
			color: var(--el-text-color-primary);
			font-variant-numeric: tabular-nums;
			word-break: break-all;
		}
	}
}

.wf-io {
	display: flex;
	flex-direction: column;
	gap: 10px;

	&__block {
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		overflow: hidden;

		&.is-error {
			border-color: var(--el-color-danger-light-5);
		}
	}

	&__title {
		padding: 6px 10px;
		font-size: 12px;
		font-weight: 600;
		color: var(--el-text-color-regular);
		background-color: var(--el-fill-color-lighter);
	}
}

.wf-pre {
	margin: 0;
	padding: 10px;
	max-height: 260px;
	overflow: auto;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 19px;
	color: var(--el-text-color-regular);
	white-space: pre-wrap;
	word-break: break-word;
	background-color: var(--el-bg-color);
}

.wf-steps {
	display: flex;
	flex-direction: column;
	gap: 10px;

	&__head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 10px;
	}

	&__title {
		font-size: 14px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	&__hint {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

.wf-step {
	display: flex;
	gap: 10px;

	&__rail {
		position: relative;
		width: 16px;
		display: flex;
		justify-content: center;
		flex-shrink: 0;

		&::before {
			content: '';
			position: absolute;
			top: 14px;
			bottom: -10px;
			width: 2px;
			background-color: var(--el-border-color-lighter);
		}
	}

	&:last-child &__rail::before {
		display: none;
	}

	&__dot {
		position: relative;
		z-index: 1;
		margin-top: 5px;
		width: 10px;
		height: 10px;
		border-radius: 50%;
		background-color: var(--el-color-info);
		box-shadow: 0 0 0 3px var(--el-color-info-light-9);
	}

	&.is-success &__dot {
		background-color: var(--el-color-success);
		box-shadow: 0 0 0 3px var(--el-color-success-light-9);
	}

	&.is-danger &__dot {
		background-color: var(--el-color-danger);
		box-shadow: 0 0 0 3px var(--el-color-danger-light-9);
	}

	&.is-warning &__dot {
		background-color: var(--el-color-warning);
		box-shadow: 0 0 0 3px var(--el-color-warning-light-9);
	}

	&.is-running &__dot {
		background-color: var(--el-color-primary);
		box-shadow: 0 0 0 3px var(--el-color-primary-light-9);
		animation: wf-pulse 1.6s ease-in-out infinite;
	}

	&__body {
		flex: 1;
		min-width: 0;
		padding: 10px 12px;
		border-radius: 10px;
		border: 1px solid var(--el-border-color-lighter);
		background-color: var(--el-bg-color);
	}

	&__head {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
	}

	&__seq {
		font-family: monospace;
		font-size: 12px;
		color: var(--el-text-color-placeholder);
	}

	&__name {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	&__duration {
		margin-left: auto;
		font-size: 12px;
		font-variant-numeric: tabular-nums;
		color: var(--el-text-color-secondary);
	}

	&__meta {
		display: flex;
		flex-wrap: wrap;
		gap: 2px 12px;
		margin-top: 4px;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	&__error {
		margin-top: 8px;
		padding: 8px 10px;
		border-radius: 6px;
		font-size: 12px;
		line-height: 18px;
		color: var(--el-color-danger);
		background-color: var(--el-color-danger-light-9);
		word-break: break-word;
	}

	&__block {
		margin-top: 8px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 6px;
		overflow: hidden;
	}

	&__block-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 5px 10px;
		font-size: 12px;
		font-weight: 600;
		color: var(--el-text-color-regular);
		background-color: var(--el-fill-color-lighter);
		cursor: pointer;
		user-select: none;
	}

	&__toggle {
		font-size: 11px;
		font-weight: 400;
		color: var(--el-color-primary);
	}

	&__preview {
		padding: 8px 10px;
		font-size: 12px;
		line-height: 18px;
		color: var(--el-text-color-regular);
		word-break: break-word;
		display: -webkit-box;
		-webkit-box-orient: vertical;
		-webkit-line-clamp: 3;
		overflow: hidden;
	}

	.wf-pre {
		max-height: 220px;
	}
}

@keyframes wf-pulse {
	0%,
	100% {
		box-shadow: 0 0 0 3px var(--el-color-primary-light-9);
	}
	50% {
		box-shadow: 0 0 0 6px var(--el-color-primary-light-8);
	}
}
</style>
