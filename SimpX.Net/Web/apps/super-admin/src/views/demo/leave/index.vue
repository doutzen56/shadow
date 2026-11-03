<template>
	<div class="leave-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="请假类型" prop="leaveType">
							<el-input v-model="state.queryParams.leaveType" placeholder="请输入请假类型" clearable @keyup.enter.native="handleQuery(true)" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="审批状态" prop="workflowStatus">
							<el-select v-model="state.queryParams.workflowStatus" placeholder="请选择状态" clearable>
								<el-option label="未提交" :value="0" />
								<el-option label="审批中" :value="1" />
								<el-option label="已通过" :value="2" />
								<el-option label="已拒绝" :value="3" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="align-self: stretch; height: auto; margin: 0 10px 5px 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="handleAdd"> 新增 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_startTime="{ row }">
					{{ formatDateTime(row.startTime) }}
				</template>
				<template #row_endTime="{ row }">
					{{ formatDateTime(row.endTime) }}
				</template>
				<template #row_workflowStatus="{ row }">
					<el-tag v-if="row.workflowStatus === 0" type="info">未提交</el-tag>
					<el-tag v-else-if="row.workflowStatus === 1" type="warning">审批中</el-tag>
					<el-tag v-else-if="row.workflowStatus === 2" type="success">已通过</el-tag>
					<el-tag v-else-if="row.workflowStatus === 3" type="danger">已拒绝</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button v-if="row.status === 0" text type="primary" icon="ele-Edit" @click="handleEdit(row)">编辑</el-button>
					<el-button text type="danger" icon="ele-Delete" @click="handleDelete(row)">删除</el-button>
					<el-button text type="primary" icon="ele-View" @click="handleView(row)">查看</el-button>
					<el-button v-if="row.status === 0" text type="success" icon="ele-Promotion" @click="handleSubmitWorkflow(row)"> 提交审批 </el-button>
					<el-button v-if="row.workflowInstanceCodes" text type="info" icon="ele-Share" @click="viewWorkflow(row.workflowInstanceCodes?.split(',')[0])"> 查看流程 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<!-- 新增/编辑对话框 -->
		<el-dialog v-model="state.dialogVisible" :title="state.dialogTitle" :close-on-click-modal="false" width="600px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ state.dialogTitle }} </span>
				</div>
			</template>

			<el-form ref="formRef" :model="state.form" :rules="formRules" label-width="auto">
				<el-form-item label="请假类型" prop="leaveType">
					<el-select v-model="state.form.leaveType" placeholder="请选择请假类型" style="width: 100%">
						<el-option label="年假" value="年假" />
						<el-option label="病假" value="病假" />
						<el-option label="事假" value="事假" />
						<el-option label="调休" value="调休" />
					</el-select>
				</el-form-item>
				<el-form-item label="开始时间" prop="startTime">
					<el-date-picker v-model="state.form.startTime" type="datetime" placeholder="选择开始时间" style="width: 100%" />
				</el-form-item>
				<el-form-item label="结束时间" prop="endTime">
					<el-date-picker v-model="state.form.endTime" type="datetime" placeholder="选择结束时间" style="width: 100%" />
				</el-form-item>
				<el-form-item label="请假原因" prop="reason">
					<el-input v-model="state.form.reason" type="textarea" :rows="3" placeholder="请输入请假原因" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.form.remark" type="textarea" :rows="2" placeholder="请输入备注（可选）" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.dialogVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="handleSave">确定</el-button>
			</template>
		</el-dialog>

		<!-- 详情对话框 -->
		<el-dialog v-model="state.detailDialogVisible" title="请假单详情" :close-on-click-modal="false" width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> 请假单详情 </span>
				</div>
			</template>

			<el-descriptions v-if="state.detail" :column="2" border>
				<el-descriptions-item label="请假类型">{{ state.detail.leaveType }}</el-descriptions-item>
				<el-descriptions-item label="请假天数">{{ state.detail.days }} 天</el-descriptions-item>
				<el-descriptions-item label="开始时间">{{ formatDateTime(state.detail.startTime) }}</el-descriptions-item>
				<el-descriptions-item label="结束时间">{{ formatDateTime(state.detail.endTime) }}</el-descriptions-item>
				<el-descriptions-item label="申请人">{{ state.detail.applicantName }}</el-descriptions-item>
				<el-descriptions-item label="部门">{{ state.detail.deptName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="业务状态" :span="2">
					<el-tag v-if="state.detail.status === 0" type="info">草稿</el-tag>
					<el-tag v-else-if="state.detail.status === 1" type="warning">已提交</el-tag>
					<el-tag v-else-if="state.detail.status === 2" type="success">已通过</el-tag>
					<el-tag v-else-if="state.detail.status === 3" type="danger">已拒绝</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="审批状态" :span="2">
					<el-tag v-if="state.detail.workflowStatus === 0" type="info">未提交</el-tag>
					<el-tag v-else-if="state.detail.workflowStatus === 1" type="warning">审批中</el-tag>
					<el-tag v-else-if="state.detail.workflowStatus === 2" type="success">已通过</el-tag>
					<el-tag v-else-if="state.detail.workflowStatus === 3" type="danger">已拒绝</el-tag>
					<span v-if="state.detail.workflowStatusText" style="margin-left: 10px; color: #909399; font-size: 12px"> ({{ state.detail.workflowStatusText }}) </span>
				</el-descriptions-item>
				<el-descriptions-item label="请假原因" :span="2">{{ state.detail.reason || '-' }}</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.workflowComment" label="审批意见" :span="2">
					{{ state.detail.workflowComment }}
				</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.workflowCompleteTime" label="审批完成时间" :span="2">
					{{ formatDateTime(state.detail.workflowCompleteTime) }}
				</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.workflowInstanceCode" label="工作流实例编码" :span="2">
					{{ state.detail.workflowInstanceCode }}
					<el-button text type="primary" style="margin-left: 10px" @click="viewWorkflow(state.detail.workflowInstanceCode)"> 查看流程 </el-button>
				</el-descriptions-item>
				<el-descriptions-item v-if="state.detail.remark" label="备注" :span="2">{{ state.detail.remark }}</el-descriptions-item>
			</el-descriptions>
		</el-dialog>

		<!-- 提交审批：业务流程发起对话框（有选中行再挂载，避免 businessId 为 undefined 触发校验警告） -->
		<BusinessWorkflowStartDialog
			v-if="state.workflowDialogRow"
			v-model:visible="state.workflowDialogVisible"
			business-module="Leave"
			:business-id="state.workflowDialogRow.id"
			@success="handleWorkflowStartSuccess"
		/>
	</div>
</template>

<script setup lang="ts" name="LeaveDemo">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useRouter } from 'vue-router';

import BusinessWorkflowStartDialog from '../../workFlow/components/BusinessWorkflowStartDialog.vue';
import { getLeavePage, addLeave, updateLeave, deleteLeave, getLeaveDetail } from '/@/api/workflow/demo_leave';

const router = useRouter();

const formatDateTime = (date: string | Date) => {
	if (!date) return '';
	const d = new Date(date);
	return d.toLocaleString('zh-CN');
};

const queryForm = ref();
const formRef = ref();
const xGrid = ref<VxeGridInstance>();

const state = reactive({
	dialogVisible: false,
	dialogTitle: '新增请假单',
	detailDialogVisible: false,
	queryParams: {
		leaveType: '',
		workflowStatus: undefined as number | undefined,
	},
	form: {
		id: null as number | null,
		leaveType: '',
		startTime: null as Date | null,
		endTime: null as Date | null,
		reason: '',
		remark: '',
	},
	detail: null as any,
	// 工作流提交对话框
	workflowDialogVisible: false,
	workflowDialogRow: null as any,
});

const formRules = {
	leaveType: [{ required: true, message: '请选择请假类型', trigger: 'change' }],
	startTime: [{ required: true, message: '请选择开始时间', trigger: 'change' }],
	endTime: [{ required: true, message: '请选择结束时间', trigger: 'change' }],
	reason: [{ required: true, message: '请输入请假原因', trigger: 'blur' }],
};

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'leaveDemo',
		name: '请假单',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'leaveType', title: '请假类型', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'startTime', title: '开始时间', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'row_startTime' } },
			{ field: 'endTime', title: '结束时间', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'row_endTime' } },
			{ field: 'days', title: '天数', width: 80 },
			{ field: 'applicantName', title: '申请人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'deptName', title: '部门', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'reason', title: '请假原因', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'workflowStatus', title: '审批状态', width: 110, slots: { default: 'row_workflowStatus' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		rowConfig: { keyField: 'id' },
		proxyConfig: {
			autoLoad: true,
			ajax: {
				query: async ({ page }) => {
					try {
						const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize };
						return await getLeavePage(params);
					} catch (error: any) {
						ElMessage.error(error?.message || '查询失败');
						return { data: { result: { items: [], total: 0 } } };
					}
				},
			},
		},
		pagerConfig: { pageSize: 10 },
		toolbarConfig: { export: true },
	}
);

// 表格事件
const gridEvents: VxeGridListeners<any> = {};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

const resetQuery = () => {
	state.queryParams.leaveType = '';
	state.queryParams.workflowStatus = undefined;
	handleQuery(true);
};

const handleAdd = () => {
	state.dialogTitle = '新增请假单';
	state.form = {
		id: null,
		leaveType: '',
		startTime: null,
		endTime: null,
		reason: '',
		remark: '',
	};
	state.dialogVisible = true;
};

const handleView = async (row: any) => {
	try {
		const res = await getLeaveDetail({ id: row.id });
		state.detail = res.data?.result || res.data?.data || res.data;
		state.detailDialogVisible = true;
	} catch (error: any) {
		ElMessage.error(error?.message || '获取详情失败');
	}
};

const handleEdit = (row: any) => {
	state.dialogTitle = '编辑请假单';
	state.form = {
		id: row.id,
		leaveType: row.leaveType,
		startTime: new Date(row.startTime),
		endTime: new Date(row.endTime),
		reason: row.reason || '',
		remark: row.remark || '',
	};
	state.dialogVisible = true;
};

const handleDelete = async (row: any) => {
	// 如果正在审批中，提示用户
	if (row.workflowStatus === 1) {
		ElMessage.warning('该请假单正在审批中，无法删除。如需删除，请先终止或取消工作流。');
		return;
	}

	try {
		const confirmMessage = row.workflowStatus === 2 || row.workflowStatus === 3 ? '该请假单已完成审批，确定要删除吗？' : '确定要删除这条请假单吗？';

		await ElMessageBox.confirm(confirmMessage, '提示', {
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			type: 'warning',
		});

		await deleteLeave({ id: row.id });
		ElMessage.success('删除成功');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.message || '删除失败');
		}
	}
};

/**
 * 提交审批 - 通过工作流业务弹窗发起
 */
const handleSubmitWorkflow = (row: any) => {
	state.workflowDialogRow = row;
	state.workflowDialogVisible = true;
};

/**
 * 查看工作流详情
 * 跳转到"我发起的流程"页面，并自动打开详情对话框
 */
const viewWorkflow = (instanceCode: string) => {
	router.push({
		path: '/workFlow/myInitiatedTasks',
		query: { instanceCode },
	});
};

const handleSave = async () => {
	await formRef.value?.validate();

	// 计算天数
	const days = state.form.endTime && state.form.startTime ? Math.ceil((state.form.endTime.getTime() - state.form.startTime.getTime()) / (1000 * 60 * 60 * 24)) : 0;

	if (days <= 0) {
		ElMessage.warning('结束时间必须大于开始时间');
		return;
	}

	try {
		if (state.form.id) {
			// 编辑
			const data = {
				id: state.form.id,
				leaveType: state.form.leaveType,
				startTime: state.form.startTime,
				endTime: state.form.endTime,
				days: days,
				reason: state.form.reason,
				remark: state.form.remark,
			};
			await updateLeave(data);
			ElMessage.success('更新成功');
		} else {
			// 新增
			const data = {
				leaveType: state.form.leaveType,
				startTime: state.form.startTime,
				endTime: state.form.endTime,
				days: days,
				reason: state.form.reason,
			};
			const res = await addLeave(data);
			if (res.data?.result) {
				ElMessage.success('创建成功');
			} else {
				ElMessage.error('创建失败');
				return;
			}
		}
		state.dialogVisible = false;
		handleQuery();
	} catch (error: any) {
		ElMessage.error(error?.message || (state.form.id ? '更新失败' : '创建失败'));
	}
};

/**
 * 工作流发起成功回调
 */
const handleWorkflowStartSuccess = () => {
	state.workflowDialogVisible = false;
	// 刷新列表，展示最新审批状态
	handleQuery();
};
</script>

<style scoped>
.leave-container {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.full-table {
	height: 100%;
	display: flex;
	flex-direction: column;
}

.mb10 {
	margin-bottom: 10px;
}

.mt10 {
	margin-top: 10px;
}
</style>
