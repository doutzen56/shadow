<template>
	<div class="sys-workFlow-pendingTasks-container">
		<!-- 顶部头图 -->
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
					<h2 class="page-hero__title">待办任务</h2>
					<p class="page-hero__desc">集中处理需要你审批、会签或抄送的工作流任务；支持按标题、状态、类型与优先级快速筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.pending }}</span>
							<span class="page-hero__stat-k">待处理</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.done }}</span>
							<span class="page-hero__stat-k">已处理</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.cc }}</span>
							<span class="page-hero__stat-k">抄送给我</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：条件与查询/重置按钮同一行（全站标准 query-toolbar） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="标题/流程名称/实例编码" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.status" class="query-select" placeholder="全部状态" clearable>
						<el-option label="待处理" :value="0" />
						<el-option label="已处理" :value="1" />
						<el-option label="已拒绝" :value="2" />
					</el-select>
				</el-form-item>
				<el-form-item label="类型">
					<el-select v-model="state.queryParams.todoType" class="query-select" placeholder="全部类型" clearable>
						<el-option label="待办" value="todo" />
						<el-option label="抄送" value="cc" />
						<el-option label="通知" value="notify" />
					</el-select>
				</el-form-item>
				<el-form-item label="优先级">
					<el-select v-model="state.queryParams.priority" class="query-select" placeholder="全部优先级" clearable>
						<el-option label="高" :value="1" />
						<el-option label="中" :value="2" />
						<el-option label="低" :value="3" />
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
				<template #header_priority>
					<span class="wf-priority-header">
						<span>优先级</span>
						<el-tooltip content="优先级定义：普通审批和抄送默认中；驳回重办、驳回到发起人等需要立即处理的任务为高；低为预留值，当前系统不会自动生成；转办继承原任务优先级。" placement="top">
							<el-icon class="wf-priority-help-icon"><ele-InfoFilled /></el-icon>
						</el-tooltip>
					</span>
				</template>
			<template #row_approvalMode="{ row }">
				<span>{{ approvalModeText(row.approvalMode) }}</span>
			</template>
			<template #row_priority="{ row }">
				<WfStatusPill :text="priorityInfo(row.priority).text" :tone="priorityInfo(row.priority).tone" />
			</template>
			<template #row_status="{ row }">
				<WfStatusPill :text="todoRowState(row).text" :tone="todoRowState(row).tone" />
			</template>
				<template #row_createTime="{ row }">
					<span v-if="row.createTime">{{ formatDateTime(row.createTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_buttons="{ row }">
					<el-button v-if="row.status === 0 && getTodoType(row) !== 'cc'" text type="primary" icon="ele-Check" @click="openApprove(row, 'approve')">同意</el-button>
					<el-button v-if="row.status === 0 && getTodoType(row) !== 'cc'" text type="danger" icon="ele-Close" @click="openApprove(row, 'reject')">拒绝</el-button>
					<el-button v-if="row.status === 0 && getTodoType(row) !== 'cc'" text type="warning" icon="ele-Connection" @click="openApprove(row, 'transfer')">转办</el-button>
					<el-button v-if="getTodoType(row) === 'cc'" text type="danger" icon="ele-Delete" @click="handleDelete(row)">删除</el-button>
					<el-button text type="info" icon="ele-InfoFilled" @click="viewDetail(row)">详情</el-button>
				</template>
			</vxe-grid>
			</div>
		</el-card>

		<!-- 审批对话框 -->
		<el-dialog v-model="approveDialog.visible" width="760px" class="wf-detail wf-detail--form" draggable :close-on-click-modal="false" @close="resetApproveDialog">
			<template #header>
				<DetailHead
					:title="approveDialog.title"
					icon="ele-Stamp"
					:name="approveDialog.currentTodo?.title"
					:code="approveDialog.currentTodo?.instanceCode"
					:sub="approveDialog.currentTodo?.flowName"
				>
					<template #side>
						<span class="wf-status" :class="`is-${priorityInfo(approveDialog.currentTodo?.priority).tone}`">
							<i class="wf-status__dot"></i>优先级 {{ priorityInfo(approveDialog.currentTodo?.priority).text }}
						</span>
					</template>
				</DetailHead>
			</template>

			<div class="wf-detail__body">
				<el-form :model="approveDialog.form" ref="approveFormRef" :rules="approveDialog.rules" label-width="auto" label-position="top">
					<el-form-item v-if="approveDialog.action === 'transfer'" label="转办给" prop="nextApproverId">
						<el-select v-model="approveDialog.form.nextApproverId" placeholder="请选择转办目标用户" filterable :loading="loadingUsers" style="width: 100%">
							<el-option v-for="user in userList" :key="user.id" :label="user.realName || user.account" :value="user.id">
								<span>{{ user.realName || user.account }}</span>
								<span class="user-option-account">{{ user.account }}</span>
							</el-option>
						</el-select>
					</el-form-item>
					<el-form-item label="审批意见" prop="comment">
						<el-input v-model="approveDialog.form.comment" type="textarea" :rows="4" placeholder="请输入审批意见" maxlength="512" show-word-limit />
					</el-form-item>
				</el-form>

				<!-- 业务模块详情（仅业务模块表单显示，formId 为 0 表示无表单即业务模块表单） -->
				<BusinessModuleDetail v-if="!approveDialog.formRecord?.formId" :business-module="approveDialog.businessModule" :business-id="approveDialog.businessId" />

				<div class="wf-form-block" v-if="approveDialog.formRecord?.formId">
					<div class="wf-form-block__hd"><i class="wf-form-block__bar"></i>表单数据</div>
					<FormRender
						:schema-json="approveDialog.formRecord?.schemaJson"
						:form-data="approveDialog.formRecord?.formData"
						:form-version="approveDialog.formRecord?.formVersion"
						:submit-time="approveDialog.formRecord?.submitTime"
						:disabled="true"
						:show-meta="true"
					/>
				</div>
			</div>

			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="approveDialog.visible = false">取消</el-button>
				<el-button
					:type="approveDialog.action === 'reject' ? 'danger' : 'primary'"
					:icon="approveDialog.action === 'reject' ? 'ele-CircleCloseFilled' : 'ele-CircleCheckFilled'"
					@click="handleApprove"
					:loading="approveDialog.loading"
				>
					{{ approveActionText }}
				</el-button>
			</template>
		</el-dialog>

		<!-- 详情对话框 -->
		<el-dialog v-model="detailDialog.visible" width="1010px" class="wf-detail" draggable v-if="detailDialog.data">
			<template #header>
				<DetailHead title="待办详情" :name="detailDialog.data.title" :code="detailDialog.data.instanceCode" :sub="detailDialog.data.flowName">
					<template #side>
						<span class="wf-status" :class="`is-${todoStateInfo.tone}`">
							<i class="wf-status__dot"></i>{{ todoStateInfo.text }}
						</span>
						<span class="wf-status" :class="`is-${priorityInfo(detailDialog.data.priority).tone}`">
							<i class="wf-status__dot"></i>优先级 {{ priorityInfo(detailDialog.data.priority).text }}
						</span>
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
										<span class="wf-summary__v" :title="item.text">{{ item.text || '--' }}</span>
									</div>
									<div class="wf-summary__cell is-wide" v-if="detailDialog.data.handleComment">
										<span class="wf-summary__k">处理意见</span>
										<span class="wf-summary__v is-wrap">{{ detailDialog.data.handleComment }}</span>
									</div>
								</div>
							</template>
						</el-skeleton>

						<!-- 业务模块详情（仅业务模块表单显示，formId 为 0 表示无表单即业务模块表单） -->
						<BusinessModuleDetail v-if="!detailDialog.formRecord?.formId" :business-module="detailDialog.businessModule" :business-id="detailDialog.businessId" />

						<div class="wf-form-block" v-if="detailDialog.formRecord?.formId">
							<div class="wf-form-block__hd"><i class="wf-form-block__bar"></i>表单信息</div>
							<FormRender
								:schema-json="detailDialog.formRecord?.schemaJson"
								:form-data="detailDialog.formRecord?.formData"
								:form-version="detailDialog.formRecord?.formVersion"
								:submit-time="detailDialog.formRecord?.submitTime"
								:disabled="true"
								:show-meta="true"
								:loading="detailDialog.loading"
							/>
						</div>
					</el-tab-pane>
					<el-tab-pane label="全部节点" name="allNodes">
						<FlowNodesTab :data="allNodesData" :loading="loadingAllNodes" />
					</el-tab-pane>
				</el-tabs>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowPendingTasks">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { formatDateTime } from '/@/utils/format';

import FormRender from '../components/FormRender.vue';
import BusinessModuleDetail from '../components/BusinessModuleDetail.vue';
import DetailHead from '../components/DetailHead.vue';
import FlowNodesTab from '../components/flowNodesTab.vue';
import WfStatusPill from '../components/wfStatusPill.vue';
import { approvalModeText, priorityInfo } from '../utils/workflowDict';
import { fetchInstanceFormRecord, parsePagedResult, useAllFlowNodes } from '../utils/taskList';
import { getTodos, approveWorkflow, deleteTodo, GetTodoStats } from '/@/api/workflow';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi } from '/@/api-services/system/api';

interface TodoItem {
	id: number;
	title: string;
	flowName: string;
	stepName?: string;
	starterName: string;
	priority?: number;
	status: number;
	todoType?: string;
	deadline?: string;
	instanceStartTime?: string;
	instanceCode: string;
	stepCode: string;
	handleComment?: string;
	isTransferred?: boolean;
}

const approveFormRef = ref();
const xGrid = ref<VxeGridInstance>();
const userList = ref<any[]>([]);
const loadingUsers = ref(false);

const state = reactive({
	queryParams: {
		keyword: '',
		status: 0 as number | undefined, // 默认只显示待处理的任务
		todoType: undefined as string | undefined,
		priority: undefined as number | undefined,
	},
});

// 头图统计：取后端 /todos/stats 的全量真实数字（此前按当前页拼凑，翻页即变，属假指标）
const heroStats = reactive({ pending: 0, done: 0, cc: 0 });

const loadHeroStats = async () => {
	try {
		const res = await GetTodoStats({});
		const stats = res.data?.result;
		if (!stats) return;
		heroStats.pending = stats.pendingCount ?? 0;
		heroStats.done = stats.doneCount ?? 0;
		heroStats.cc = stats.ccCount ?? 0;
	} catch (error) {
		console.error('获取待办统计失败:', error);
	}
};

onMounted(loadHeroStats);

// 表格参数配置
const options = useVxeTable<TodoItem>(
	{
		id: 'workFlowPendingTasks',
		name: '待办任务',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'title', title: '标题', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowName', title: '流程名称', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'flowCategory', title: '流程分类', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'stepName', title: '当前步骤', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'approvalMode', title: '审批模式', width: 110, slots: { default: 'row_approvalMode' } },
			{ field: 'starterName', title: '发起人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'starterDeptName', title: '发起部门', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'priority', title: '优先级', width: 100, slots: { header: 'header_priority', default: 'row_priority' } },
			{ field: 'status', title: '状态', width: 100, slots: { default: 'row_status' } },
			{ field: 'instanceStartTime', title: '创建时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '发起时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		height: 'auto',
		stripe: true,
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign({}, state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await getTodos(params);
	const parsed = parsePagedResult(res);
	return { data: { result: parsed } };
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

const approveDialog = reactive({
	visible: false,
	loading: false,
	action: '' as 'approve' | 'reject' | 'transfer',
	title: '',
	currentTodo: null as TodoItem | null,
	form: {
		comment: '',
		nextApproverId: undefined as number | undefined,
		nextApproverName: '',
	},
	rules: {
		comment: [{ required: true, message: '请输入审批意见', trigger: 'blur' }],
		nextApproverId: [{ required: true, message: '请选择转办目标用户', trigger: 'change' }],
	},
	formRecord: null as any,
	businessModule: undefined as string | undefined,
	businessId: undefined as string | number | undefined,
});

const detailDialog = reactive({
	visible: false,
	loading: false,
	data: null as TodoItem | null,
	formRecord: null as any,
	activeTab: 'info',
	businessModule: undefined as string | undefined,
	businessId: undefined as string | number | undefined,
});

// 「全部节点」页签：懒加载状态由四个任务页共用（见 utils/taskList）
const { loadingAllNodes, allNodesData, resetFlowNodes, handleNodesTabClick } = useAllFlowNodes();

const getTodoType = (row: any) => row?.todoType || 'todo';

/** 待办状态：抄送 / 待处理 / 已转办 / 已处理 / 已拒绝 */
const todoRowState = (row: any) => {
	if (!row) return { text: '-', tone: 'pending' };
	if (getTodoType(row) === 'cc') return { text: '已抄送', tone: 'info' };
	if (row.status === 1 && row.isTransferred) return { text: '已转办', tone: 'info' };
	if (row.status === 1) return { text: '已处理', tone: 'success' };
	if (row.status === 2) return { text: '已拒绝', tone: 'danger' };
	return { text: '待处理', tone: 'warning' };
};

const todoStateInfo = computed(() => todoRowState(detailDialog.data));

/** 审批弹窗主按钮文案随动作变化 */
const approveActionText = computed(() => {
	const map: Record<string, string> = { approve: '同意', reject: '驳回', transfer: '转办' };
	return map[approveDialog.action] ?? '确定';
});

// 基本信息卡片：统一走数组渲染，字段增减不用改模板
const baseInfoItems = computed(() => {
	const d = detailDialog.data;
	if (!d) return [] as Array<{ label: string; text: string }>;
	return [
		{ label: '流程标题', text: d.title },
		{ label: '流程名称', text: d.flowName },
		{ label: '当前步骤', text: d.stepName },
		{ label: '发起人', text: d.starterName },
		{ label: '创建时间', text: d.instanceStartTime ? formatDateTime(d.instanceStartTime) : '--' },
		{ label: '处理期限', text: d.deadline ? formatDateTime(d.deadline) : '--' },
	];
});

const resetQuery = () => {
	state.queryParams.keyword = '';
	state.queryParams.status = 0; // 重置时恢复为待处理状态
	state.queryParams.todoType = undefined;
	state.queryParams.priority = undefined;
	handleQuery(true);
};

const loadUserList = async () => {
	if (userList.value.length > 0) return; // 已加载过，避免重复请求

	try {
		loadingUsers.value = true;
		const res = await getAPI(SysUserApi).apiSysUserPagePost({
			page: 1,
			pageSize: 10000, // 设置一个较大的值以获取所有用户
		});
		userList.value = res.data.result?.items ?? [];
	} catch (error) {
		console.error('获取用户列表失败:', error);
		ElMessage.error('获取用户列表失败');
	} finally {
		loadingUsers.value = false;
	}
};

const openApprove = async (row: TodoItem, action: 'approve' | 'reject' | 'transfer') => {
	approveDialog.action = action;
	approveDialog.currentTodo = row;
	approveDialog.title = action === 'approve' ? '审批通过' : action === 'reject' ? '审批拒绝' : '转办';
	approveDialog.form.comment = '';
	approveDialog.form.nextApproverId = undefined;
	approveDialog.form.nextApproverName = '';
	const result = await fetchInstanceFormRecord(row.instanceCode);
	approveDialog.formRecord = result.formRecord;
	approveDialog.businessModule = result.businessModule;
	approveDialog.businessId = result.businessId;
	approveDialog.visible = true;

	// 如果是转办，需要加载用户列表
	if (action === 'transfer') {
		loadUserList();
	}
};

const resetApproveDialog = () => {
	approveDialog.form.comment = '';
	approveDialog.form.nextApproverId = undefined;
	approveDialog.form.nextApproverName = '';
	approveDialog.currentTodo = null;
	approveDialog.formRecord = null;
	approveDialog.businessModule = undefined;
	approveDialog.businessId = undefined;
	if (approveFormRef.value) {
		approveFormRef.value.resetFields();
	}
};

const handleApprove = async () => {
	if (!approveFormRef.value || !approveDialog.currentTodo) return;

	await approveFormRef.value.validate(async (valid: boolean) => {
		if (!valid || !approveDialog.currentTodo) return;

		approveDialog.loading = true;
		try {
			const params: any = {
				todoId: approveDialog.currentTodo.id,
				action: approveDialog.action,
				comment: approveDialog.form.comment,
			};

			if (approveDialog.action === 'transfer') {
				if (!approveDialog.form.nextApproverId) {
					ElMessage.warning('请选择转办目标用户');
					return;
				}
				params.nextApproverId = approveDialog.form.nextApproverId;
				// 获取用户名
				const selectedUser = userList.value.find((u) => u.id === approveDialog.form.nextApproverId);
				params.nextApproverName = selectedUser ? selectedUser.realName || selectedUser.account : '';
			}

			await approveWorkflow(params);
			ElMessage.success(approveDialog.action === 'approve' ? '审批通过成功' : approveDialog.action === 'reject' ? '审批拒绝成功' : '转办成功');
			approveDialog.visible = false;
			handleQuery();
			loadHeroStats();
		} catch (error: any) {
			ElMessage.error(error.message || '操作失败');
		} finally {
			approveDialog.loading = false;
		}
	});
};

const handleDelete = async (row: TodoItem) => {
	ElMessageBox.confirm('确认删除该抄送记录吗？删除后将不再显示。', '删除确认', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
		dangerouslyUseHTMLString: false,
	})
		.then(async () => {
			try {
				await deleteTodo({ id: row.id });
				ElMessage.success('删除成功');
				if (detailDialog.visible && detailDialog.data?.id === row.id) {
					detailDialog.visible = false;
				}
				handleQuery();
				loadHeroStats();
			} catch (error: any) {
				ElMessage.error(error?.message || '删除失败');
			}
		})
		.catch(() => {
			/* 用户取消 */
		});
};

const viewDetail = async (row: TodoItem) => {
	detailDialog.visible = true;
	detailDialog.loading = true;
	detailDialog.data = row;
	detailDialog.formRecord = null;
	detailDialog.activeTab = 'info';
	resetFlowNodes(); // 清除之前的全部节点数据
	detailDialog.businessModule = undefined;
	detailDialog.businessId = undefined;
	// fetchInstanceFormRecord 内部已做失败提示，这里无需再包 try/catch
	const result = await fetchInstanceFormRecord(row.instanceCode);
	detailDialog.formRecord = result.formRecord;
	detailDialog.businessModule = result.businessModule;
	detailDialog.businessId = result.businessId;
	detailDialog.loading = false;
};

const handleTabClick = (tab: any) => handleNodesTabClick(tab, detailDialog.data?.instanceCode);
</script>

<style scoped>
/* 头图与查询条之间留出呼吸空间（根容器高度由全局 .layout-parent > div:has(.full-table) 提供） */
.sys-workFlow-pendingTasks-container > .query-toolbar {
	margin-top: 12px;
}

.user-option-account {
	margin-left: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
