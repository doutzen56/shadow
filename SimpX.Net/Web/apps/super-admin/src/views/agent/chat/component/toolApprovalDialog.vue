<template>
	<!-- 工具执行审批（P0-2）：危险工具执行前弹出人工确认，不可手动关闭，避免误关导致工具调用挂起 -->
	<el-dialog :model-value="!!request" width="520px" :show-close="false" :close-on-click-modal="false" :close-on-press-escape="false" append-to-body>
		<template #header>
			<div class="dialog-title approval-title">
				<el-icon size="16" color="var(--el-color-warning)"><ele-WarningFilled /></el-icon>
				<span>工具执行需审批</span>
			</div>
		</template>
		<div class="approval-body" v-if="request">
			<el-alert type="warning" :closable="false" show-icon class="approval-alert">
				<template #title>智能体请求执行一个潜在危险的操作，请确认后再放行</template>
			</el-alert>
			<div class="approval-tool">
				<el-tag type="danger" effect="dark" size="large">{{ toolLabel }}</el-tag>
				<code class="approval-tool-name">{{ request.toolName }}</code>
			</div>
			<div class="approval-args-label">工具入参</div>
			<pre class="approval-args" v-if="request.arguments">{{ request.arguments }}</pre>
			<pre class="approval-args approval-args-empty" v-else>（无入参）</pre>
			<div class="approval-queue" v-if="(queueCount ?? 0) > 1">当前还有 {{ (queueCount ?? 0) - 1 }} 个待审批请求，将依次处理</div>
		</div>
		<template #footer>
			<el-button icon="ele-CircleCloseFilled" :disabled="!request" @click="emit('resolve', false)"> 拒绝 </el-button>
			<el-button type="primary" icon="ele-CircleCheckFilled" :disabled="!request" @click="emit('resolve', true)"> 批准执行 </el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="agentToolApprovalDialog">
import { computed } from 'vue';
import type { ToolApprovalRequest } from '../types';

// 危险工具名 → 中文标签（须与后端 ToolApprovalPolicy 的注册名一致）
const TOOL_LABELS: Record<string, string> = {
	RunShell: '执行 Shell 命令',
	RunPythonCode: '执行 Python 代码',
	WriteTextToDesktop: '写入桌面文件',
	SaveFileContent: '保存文件内容',
	SaveMemory: '写入长期记忆',
	UpdateMemory: '更新记忆',
	DeleteMemory: '删除记忆',
	GetAsync: '发起 HTTP GET 请求',
	PostAsync: '发起 HTTP POST 请求',
	PutAsync: '发起 HTTP PUT 请求',
	DeleteAsync: '发起 HTTP DELETE 请求',
	AddOrUpdateCronTask: '创建/更新周期任务',
	AddOnceTask: '创建一次性任务',
	RemoveCronTask: '移除任务',
	TriggerCronTask: '立即触发任务',
	SendDDToMyMsg: '发送钉钉消息（给自己）',
	SendDDToUserMsg: '发送钉钉消息（给他人）',
	GenerateImage: '文生图',
	AddJson: '保存 Json 数据',
};

const props = defineProps<{
	/** 当前待审批项（队列中的第一个）；为 null 时弹窗隐藏 */
	request?: ToolApprovalRequest | null;
	/** 待审批总数，>1 时提示后面还有几个 */
	queueCount?: number;
}>();

const emit = defineEmits<{ resolve: [approved: boolean] }>();

/** 中文标签：未收录的工具直接回退为原始工具名，保证永远有可读文案 */
const toolLabel = computed(() => (props.request ? TOOL_LABELS[props.request.toolName] ?? props.request.toolName : ''));
</script>

<style scoped>
.dialog-title {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.approval-title {
	color: var(--el-color-warning);
}

.approval-body {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.approval-alert {
	border-radius: 8px;
}

.approval-tool {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
}

.approval-tool-name {
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	word-break: break-all;
}

.approval-args-label {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.approval-args {
	margin: 0;
	padding: 10px 12px;
	max-height: 220px;
	overflow: auto;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background-color: var(--el-fill-color-light);
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 18px;
	white-space: pre-wrap;
	word-break: break-all;
	color: var(--el-text-color-primary);
}

.approval-args-empty {
	color: var(--el-text-color-placeholder);
}

.approval-queue {
	font-size: 12px;
	color: var(--el-color-warning);
}
</style>
