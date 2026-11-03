<template>
	<div class="wf-nodes">
		<!-- 图例条：一行说清状态含义，替代原来的大段 el-alert -->
		<div class="wf-legend">
			<span class="wf-legend__title"><el-icon><ele-InfoFilled /></el-icon>节点状态</span>
			<span class="wf-legend__items">
				<span class="wf-status is-success"><i class="wf-status__dot"></i>已完成</span>
				<span class="wf-status is-warning"><i class="wf-status__dot"></i>执行中</span>
				<span class="wf-status is-pending"><i class="wf-status__dot"></i>待执行</span>
				<span class="wf-status is-danger"><i class="wf-status__dot"></i>已拒绝</span>
				<span class="wf-status is-info"><i class="wf-status__dot"></i>已跳过</span>
			</span>
		</div>

		<vxe-grid v-bind="nodeGridOptions" :data="nodes" :loading="loading">
			<template #node_execStatus="{ row }">
				<span class="wf-status" :class="`is-${nodeStatus(row).tone}`">
					<i class="wf-status__dot"></i>{{ nodeStatus(row).text }}
				</span>
			</template>
			<template #node_approvalMode="{ row }">
				<span>{{ approvalModeText(row.approvalMode) }}</span>
			</template>
			<template #node_approverType="{ row }">
				<span>{{ approverTypeText(row.approverType) }}</span>
			</template>
			<template #node_approverNames="{ row }">
				<span v-if="isSpecialNode(row.nodeType)">-</span>
				<el-popover v-else-if="row.approverNames" placement="top" :width="300" trigger="hover">
					<template #reference>
						<span class="approver-names-text">{{ row.approverNames }}</span>
					</template>
					<div style="max-height: 300px; overflow-y: auto">
						<el-tag v-for="(name, index) in row.approverNames.split('、')" :key="index" type="success" style="margin: 2px">
							{{ name }}
						</el-tag>
					</div>
				</el-popover>
				<span v-else>-</span>
			</template>
			<template #node_startTime="{ row }">
				<span v-if="getNodeExecutionStatus(row.nodeId)?.startTime">
					{{ formatDateTime(getNodeExecutionStatus(row.nodeId).startTime) }}
				</span>
				<span v-else>-</span>
			</template>
			<template #node_completeTime="{ row }">
				<span v-if="getNodeExecutionStatus(row.nodeId)?.completeTime">
					{{ formatDateTime(getNodeExecutionStatus(row.nodeId).completeTime) }}
				</span>
				<span v-else>-</span>
			</template>
			<template #node_buttons="{ row }">
				<el-button text type="primary" @click="viewNodeDetail(row)">详情</el-button>
			</template>
		</vxe-grid>

		<!-- 节点详情子弹窗 -->
		<el-dialog v-model="nodeDialog.visible" width="760px" class="wf-node-dialog" draggable>
			<template #header>
				<DetailHead
					title="节点详情"
					:name="nodeDialog.data?.nodeText"
					:code="nodeDialog.data?.nodeId"
					:sub="nodeDialog.data?.nodeType ? `类型：${stepTypeText(nodeDialog.data.nodeType)}` : ''"
				/>
			</template>

			<el-descriptions :column="2" border v-if="nodeDialog.data">
				<el-descriptions-item label="节点ID">{{ nodeDialog.data.nodeId }}</el-descriptions-item>
				<el-descriptions-item label="节点类型">{{ stepTypeText(nodeDialog.data.nodeType) }}</el-descriptions-item>
				<el-descriptions-item label="节点名称" :span="2">{{ nodeDialog.data.nodeText }}</el-descriptions-item>
				<el-descriptions-item label="审批模式">
					<span v-if="isSpecialNode(nodeDialog.data.nodeType)">-</span>
					<span v-else>{{ approvalModeText(nodeDialog.data.approvalMode) }}</span>
				</el-descriptions-item>
				<el-descriptions-item label="审批人类型">
					<span v-if="isSpecialNode(nodeDialog.data.nodeType)">-</span>
					<span v-else>{{ approverTypeText(nodeDialog.data.approverType) }}</span>
				</el-descriptions-item>
				<el-descriptions-item label="审批人范围" :span="2">
					<span v-if="isSpecialNode(nodeDialog.data.nodeType)">-</span>
					<span v-else-if="nodeDialog.data.approverNames">{{ nodeDialog.data.approverNames }}</span>
					<span v-else>-</span>
				</el-descriptions-item>
				<el-descriptions-item label="执行状态" :span="2">
					<span class="wf-status" :class="`is-${nodeStatus(nodeDialog.data).tone}`">
						<i class="wf-status__dot"></i>{{ nodeStatus(nodeDialog.data).text }}
					</span>
				</el-descriptions-item>
				<template v-if="nodeDialog.executionStatus">
					<el-descriptions-item label="开始时间">
						{{ nodeDialog.executionStatus.startTime ? formatDateTime(nodeDialog.executionStatus.startTime) : '-' }}
					</el-descriptions-item>
					<el-descriptions-item label="完成时间">
						{{ nodeDialog.executionStatus.completeTime ? formatDateTime(nodeDialog.executionStatus.completeTime) : '-' }}
					</el-descriptions-item>
					<el-descriptions-item label="审批意见" :span="2">
						{{ nodeDialog.executionStatus.approvalComment || '-' }}
					</el-descriptions-item>
				</template>
			</el-descriptions>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowNodeTable">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
import { computed, reactive } from 'vue';
import { VxeGridProps } from 'vxe-table';
import { formatDateTime } from '/@/utils/format';
import { approvalModeText, approverTypeText, execStatusInfo, isSpecialNode, stepTypeText } from '../utils/workflowDict';
import DetailHead from './DetailHead.vue';

const props = withDefaults(
	defineProps<{
		/** getAllFlowNodes 返回的原始数据（含 allNodes、executedSteps） */
		data?: any;
		loading?: boolean;
		/** 是否显示操作列的「详情」按钮 */
		showDetail?: boolean;
	}>(),
	{ showDetail: true }
);

const nodes = computed(() => props.data?.allNodes ?? []);

const getNodeExecutionStatus = (nodeId: string) => {
	if (!props.data?.executedSteps) return null;
	return props.data.executedSteps.find((step: any) => step.stepCode === nodeId || step.stepId === nodeId);
};

/** 节点状态：有执行记录取记录；开始/结束/网关等结构节点天然已完成（后端 nodeType 大小写不统一，用正则宽松判断） */
const nodeStatus = (row: any) => {
	const exec = getNodeExecutionStatus(row?.nodeId);
	if (exec) return execStatusInfo(exec.status);
	const type = String(row?.nodeType ?? '').toLowerCase();
	if (/start|circle|begin|end/.test(type)) return { text: '已完成', tone: 'success' };
	return { text: '未执行', tone: 'pending' };
};

// 操作列开关：只读场景（如抄送页）不给「详情」按钮，列宽合计控制在 1000 以内避免横向滚动
// 列宽合计约 942px，控制在 1010px 弹窗内容区之内，避免固定右列盖住「完成时间」
const allColumns = [
	{ field: 'seq', type: 'seq', title: '序号', width: 56 },
	{ field: 'nodeText', title: '节点名称', minWidth: 130, showOverflow: 'tooltip' },
	{ field: 'execStatus', title: '执行状态', width: 100, slots: { default: 'node_execStatus' } },
	{ field: 'approvalMode', title: '审批模式', width: 80, slots: { default: 'node_approvalMode' } },
	{ field: 'approverType', title: '审批人类型', width: 90, slots: { default: 'node_approverType' } },
	{ field: 'approverNames', title: '审批人范围', minWidth: 130, slots: { default: 'node_approverNames' } },
	{ field: 'startTime', title: '开始时间', width: 140, slots: { default: 'node_startTime' } },
	{ field: 'completeTime', title: '完成时间', width: 140, slots: { default: 'node_completeTime' } },
	{ field: 'buttons', title: '操作', width: 76, fixed: 'right', slots: { default: 'node_buttons' } },
];

const nodeGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	stripe: true,
	rowConfig: { keyField: 'nodeId' },
	columns: props.showDetail ? allColumns : allColumns.filter((c: any) => c.field !== 'buttons'),
});

const nodeDialog = reactive({
	visible: false,
	data: null as any,
	executionStatus: null as any,
});

const viewNodeDetail = (row: any) => {
	nodeDialog.data = row;
	nodeDialog.executionStatus = getNodeExecutionStatus(row.nodeId);
	nodeDialog.visible = true;
};
</script>

<style scoped lang="scss">
.wf-nodes {
	display: flex;
	flex-direction: column;
}

.wf-legend :deep(.el-icon) {
	font-size: 14px;
}
</style>

<style>
/* 弹窗默认 append-to-body，scoped 穿透不生效，需非 scoped 块 */
.wf-node-dialog .el-dialog__header {
	padding: 0;
	margin: 0;
}

.wf-node-dialog .el-dialog__body {
	padding: 16px 18px 18px;
}

.wf-node-dialog .approver-names-text {
	cursor: pointer;
	color: var(--el-color-primary);
	text-decoration: underline dotted;
}
</style>
