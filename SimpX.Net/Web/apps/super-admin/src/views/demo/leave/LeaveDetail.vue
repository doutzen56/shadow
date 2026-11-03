<template>
	<el-descriptions v-if="detail" :column="2" border>
		<el-descriptions-item label="请假类型">{{ detail.leaveType || detail.LeaveType }}</el-descriptions-item>
		<el-descriptions-item label="请假天数">{{ detail.days || detail.Days }} 天</el-descriptions-item>
		<el-descriptions-item label="开始时间">{{ formatDateTime(detail.startTime || detail.StartTime) }}</el-descriptions-item>
		<el-descriptions-item label="结束时间">{{ formatDateTime(detail.endTime || detail.EndTime) }}</el-descriptions-item>
		<el-descriptions-item label="申请人">{{ detail.applicantName || detail.ApplicantName }}</el-descriptions-item>
		<el-descriptions-item label="部门">{{ detail.deptName || detail.DeptName || '-' }}</el-descriptions-item>
		<el-descriptions-item label="业务状态" :span="2">
			<el-tag v-if="(detail.status ?? detail.Status) === 0" type="info">草稿</el-tag>
			<el-tag v-else-if="(detail.status ?? detail.Status) === 1" type="warning">已提交</el-tag>
			<el-tag v-else-if="(detail.status ?? detail.Status) === 2" type="success">已通过</el-tag>
			<el-tag v-else-if="(detail.status ?? detail.Status) === 3" type="danger">已拒绝</el-tag>
		</el-descriptions-item>
		<el-descriptions-item label="审批状态" :span="2">
			<el-tag v-if="(detail.workflowStatus ?? detail.WorkflowStatus) === 0" type="info">未提交</el-tag>
			<el-tag v-else-if="(detail.workflowStatus ?? detail.WorkflowStatus) === 1" type="warning">审批中</el-tag>
			<el-tag v-else-if="(detail.workflowStatus ?? detail.WorkflowStatus) === 2" type="success">已通过</el-tag>
			<el-tag v-else-if="(detail.workflowStatus ?? detail.WorkflowStatus) === 3" type="danger">已拒绝</el-tag>
			<span v-if="detail.workflowStatusText || detail.WorkflowStatusText" style="margin-left: 10px; color: #909399; font-size: 12px">
				({{ detail.workflowStatusText || detail.WorkflowStatusText }})
			</span>
		</el-descriptions-item>
		<el-descriptions-item label="原始表单" :span="2">
			<el-button text type="primary" size="small" @click="handleViewOriginalForm">
				<el-icon style="margin-right: 4px"><ele-View /></el-icon>
				查看原始表单
			</el-button>
		</el-descriptions-item>
		<el-descriptions-item label="请假原因" :span="2">{{ detail.reason || detail.Reason || '-' }}</el-descriptions-item>
		<el-descriptions-item v-if="detail.workflowComment || detail.WorkflowComment" label="审批意见" :span="2">
			{{ detail.workflowComment || detail.WorkflowComment }}
		</el-descriptions-item>
		<el-descriptions-item v-if="detail.workflowCompleteTime || detail.WorkflowCompleteTime" label="审批完成时间" :span="2">
			{{ formatDateTime(detail.workflowCompleteTime || detail.WorkflowCompleteTime) }}
		</el-descriptions-item>
		<el-descriptions-item v-if="detail.workflowInstanceCode || detail.WorkflowInstanceCode" label="工作流实例编码" :span="2">
			{{ detail.workflowInstanceCode || detail.WorkflowInstanceCode }}
			<el-button text type="primary" size="small" style="margin-left: 10px" @click="handleViewWorkflow(detail.workflowInstanceCode || detail.WorkflowInstanceCode)"> 查看流程 </el-button>
		</el-descriptions-item>
		<el-descriptions-item v-if="detail.remark || detail.Remark" label="备注" :span="2">{{ detail.remark || detail.Remark }}</el-descriptions-item>
	</el-descriptions>
</template>

<script setup lang="ts" name="LeaveDetail">
import { useRouter } from 'vue-router';

const props = defineProps<{
	detail: any;
}>();

const router = useRouter();

const formatDateTime = (date: string | Date) => {
	if (!date) return '';
	const d = new Date(date);
	return d.toLocaleString('zh-CN');
};

const handleViewWorkflow = (instanceCode: string) => {
	router.push({
		path: '/workFlow/myInitiatedTasks',
		query: { instanceCode },
	});
};

/**
 * 查看原始表单预览页
 */
const handleViewOriginalForm = () => {
	// 跳转到原始业务表单的预览页面，并传递业务 ID
	router.push({
		path: '/demo/leave',
		query: {
			id: props.detail?.id || props.detail?.Id, // 传递业务 ID
		},
	});
};
</script>

<style scoped></style>
