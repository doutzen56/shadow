<template>
	<div class="agents-workflow-container">
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
					<h2 class="page-hero__title">工作流编排</h2>
					<p class="page-hero__desc">把多个智能体、条件分支、并行网关与人工审批串成可复用的流程；发布后即可按版本触发运行并全程留痕。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.total }}</span>
							<span class="page-hero__stat-k">工作流总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.published }}</span>
							<span class="page-hero__stat-k">本页已发布</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.runs }}</span>
							<span class="page-hero__stat-k">本页累计运行</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：条件与新增按钮同一行 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="名称">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="工作流名称" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.status" class="query-select" clearable placeholder="全部">
						<el-option v-for="(item, key) in WORKFLOW_STATUS_META" :key="key" :label="item.label" :value="Number(key)" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="state.loading" @click="handleQuery(true)"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleCreate" v-auth="'agent/workflow/add'"> 新建工作流 </el-button>
				<el-button icon="ele-Tickets" @click="goRunList()"> 运行记录 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty v-if="!state.tableData.length" :image-size="160" description="暂无工作流，点击「新建工作流」开始编排" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.tableData" :key="item.id">
						<el-card shadow="never" class="entity-card wf-card" :class="statusClass(item.status)">
							<i class="ri-flow-chart entity-card__watermark" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i class="ri-flow-chart" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="item.name || ''">{{ item.name }}</div>
									<div class="entity-card__sub">v{{ item.version ?? 1 }} · 创建于 {{ formatDate(item.createTime) }}</div>
								</div>
								<el-tag size="small" effect="light" :type="statusMeta(item.status).type">{{ statusMeta(item.status).label }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc">{{ item.description || '暂无描述' }}</div>

								<div class="metric-group">
									<div class="metric-item">
										<span>运行次数</span><b>{{ item.runCount ?? 0 }}</b>
									</div>
									<div class="metric-item">
										<span>流程节点</span><b>{{ item.nodeCount }} 个</b>
									</div>
									<div class="metric-item">
										<span>最近运行</span><b>{{ formatDate(item.lastRunTime) }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">更新于 {{ formatDate(item.updateTime) }}</span>
								<div class="entity-card__actions">
									<el-button icon="ele-EditPen" text type="primary" @click="handleDesign(item)" v-auth="'agent/workflow/update'"> 设计 </el-button>
									<el-tooltip :disabled="item.status === WORKFLOW_STATUS.Published" content="发布后才能触发运行" placement="top">
										<span>
											<el-button
												icon="ele-VideoPlay"
												text
												type="primary"
												:disabled="item.status !== WORKFLOW_STATUS.Published"
												@click="handleRun(item)"
												v-auth="'agent/workflow/run'"
											>
												运行
											</el-button>
										</span>
									</el-tooltip>
									<el-dropdown trigger="click" @command="(command: string) => handleCommand(command, item)">
										<el-button icon="ele-MoreFilled" text />
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item command="records" icon="ele-Tickets">运行记录</el-dropdown-item>
												<el-dropdown-item v-if="item.status !== WORKFLOW_STATUS.Published" command="publish" icon="ele-Upload" v-auth="'agent/workflow/publish'">
													发布
												</el-dropdown-item>
												<el-dropdown-item v-else command="disable" icon="ele-VideoPause" v-auth="'agent/workflow/publish'"> 停用 </el-dropdown-item>
												<el-dropdown-item command="copy" icon="ele-CopyDocument" v-auth="'agent/workflow/add'">复制</el-dropdown-item>
												<el-dropdown-item command="delete" icon="ele-Delete" divided v-auth="'agent/workflow/delete'">删除</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				class="table-pager"
				background
				v-model:current-page="state.queryParams.page"
				v-model:page-size="state.queryParams.pageSize"
				:page-sizes="[12, 24, 48, 96]"
				layout="total, sizes, prev, pager, next, jumper"
				:total="state.total"
				@change="handleQuery()"
			/>
		</el-card>

		<!-- 设计器：新建与编辑共用 -->
		<WorkflowDesigner
			v-if="state.designerVisible"
			:workflow-id="state.designerWorkflowId"
			@close="closeDesigner"
			@saved="onDesignerSaved"
		/>

		<RunWorkflowDialog v-model="state.runVisible" :workflow="state.currentWorkflow" @started="onRunStarted" />
	</div>
</template>

<script lang="ts" setup name="agentWorkflow">
import { computed, onMounted, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { AgentWorkflowApi } from '/@/api-services/agent/api';
import { AgentWorkflowStatusEnum, AgentWorkflowDto } from '/@/api-services/agent/models';
import { WORKFLOW_STATUS, WORKFLOW_STATUS_META } from './types';
import { parseDefinition } from './logicflow/mapper';
import WorkflowDesigner from './component/workflowDesigner.vue';
import RunWorkflowDialog from './component/runWorkflowDialog.vue';

type WorkflowCard = AgentWorkflowDto & { nodeCount: number };
type WorkflowQuery = { page: number; pageSize: number; name: string; status?: number };

const router = useRouter();

const state = reactive({
	loading: false,
	total: 0,
	tableData: [] as WorkflowCard[],
	queryParams: { page: 1, pageSize: 12, name: '', status: undefined } as WorkflowQuery,
	designerVisible: false,
	designerWorkflowId: undefined as number | undefined,
	runVisible: false,
	currentWorkflow: null as AgentWorkflowDto | null,
});

const heroStats = computed(() => ({
	published: state.tableData.filter((item) => item.status === WORKFLOW_STATUS.Published).length,
	runs: state.tableData.reduce((sum, item) => sum + (item.runCount ?? 0), 0),
}));

const statusMeta = (status?: number) => WORKFLOW_STATUS_META[Number(status ?? 0)] ?? WORKFLOW_STATUS_META[0];

/** 状态只影响卡片主色，不引入额外视觉噪音：草稿灰、发布绿、停用橙 */
const statusClass = (status?: number) => {
	if (status === WORKFLOW_STATUS.Published) return 'is-success';
	if (status === WORKFLOW_STATUS.Disabled) return 'is-warning';
	return 'is-info';
};

const formatDate = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 10) : '-');

const handleQuery = async (reset = false) => {
	if (reset) state.queryParams.page = 1;
	state.loading = true;
	try {
		const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowPageDataGet(
			state.queryParams.name || undefined,
			state.queryParams.status as AgentWorkflowStatusEnum | undefined,
			state.queryParams.page,
			state.queryParams.pageSize
		);
		const items = res.data.result?.items ?? [];
		state.tableData = items.map((item) => ({ ...item, nodeCount: parseDefinition(item.definitionJson).nodes.length }));
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

const resetQuery = () => {
	state.queryParams.name = '';
	state.queryParams.status = undefined;
	handleQuery(true);
};

const handleCreate = () => {
	state.designerWorkflowId = undefined;
	state.designerVisible = true;
};

const handleDesign = (item: WorkflowCard) => {
	state.designerWorkflowId = item.id;
	state.designerVisible = true;
};

const closeDesigner = () => {
	state.designerVisible = false;
	state.designerWorkflowId = undefined;
	handleQuery();
};

const onDesignerSaved = () => {
	handleQuery();
};

const handleRun = (item: WorkflowCard) => {
	state.currentWorkflow = item;
	state.runVisible = true;
};

const onRunStarted = ({ runId, workflowId }: { runId: number; workflowId: number }) => {
	goRunList({ workflowId, runId });
};

const goRunList = (query?: { workflowId?: number; runId?: number }) => {
	router.push({
		path: '/agent/workflowRun',
		query: {
			...(query?.workflowId ? { workflowId: String(query.workflowId) } : {}),
			...(query?.runId ? { runId: String(query.runId) } : {}),
		},
	});
};

const handleCommand = async (command: string, item: WorkflowCard) => {
	switch (command) {
		case 'records':
			goRunList({ workflowId: item.id });
			break;
		case 'publish':
			await handlePublish(item);
			break;
		case 'disable':
			await handleDisable(item);
			break;
		case 'copy':
			await handleCopy(item);
			break;
		case 'delete':
			await handleDelete(item);
			break;
	}
};

const handlePublish = async (item: WorkflowCard) => {
	try {
		await ElMessageBox.confirm(`发布后即可按当前版本触发运行，确定发布「${item.name}」吗？`, '发布工作流', { type: 'info' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowApi).apiAgentWorkflowPublishIdPost(item.id!);
	ElMessage.success('发布成功');
	handleQuery();
};

const handleDisable = async (item: WorkflowCard) => {
	try {
		await ElMessageBox.confirm(`停用后无法再触发新运行，已在运行或等待审批的实例不受影响。确定停用「${item.name}」吗？`, '停用工作流', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowApi).apiAgentWorkflowDisableIdPost(item.id!);
	ElMessage.success('已停用');
	handleQuery();
};

const handleCopy = async (item: WorkflowCard) => {
	await getAPI(AgentWorkflowApi).apiAgentWorkflowCopyIdPost(item.id!);
	ElMessage.success('已复制为草稿副本');
	handleQuery(true);
};

const handleDelete = async (item: WorkflowCard) => {
	try {
		await ElMessageBox.confirm(`确定删除工作流「${item.name}」吗？历史运行记录会保留，但不可再恢复流程定义。`, '删除工作流', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentWorkflowApi).apiAgentWorkflowIdDelete(item.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped lang="scss">
.query-toolbar {
	margin-top: 14px;
}

.wf-card {
	.entity-card__icon i {
		font-size: 19px;
	}
}
</style>
