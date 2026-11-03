<template>
	<div class="agents-app-container">
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
					<h2 class="page-hero__title">智能体管理</h2>
					<p class="page-hero__desc">配置智能体的模型、提示词、工具与知识库，保存后即可在「我的智能体」里直接对话。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">智能体总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.kms }}</span>
							<span class="page-hero__stat-k">本页绑知识库</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.stream }}</span>
							<span class="page-hero__stat-k">本页流式输出</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增按钮放这里，不进卡片区，避免随卡片滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="名称/描述" clearable @keyup.enter="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'agent/app/addEdit'"> 新增智能体 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty v-if="!state.tableData.length" :image-size="160" description="暂无智能体，点击上方按钮创建第一个" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.tableData" :key="item.id">
						<el-card shadow="never" class="entity-card entity-card--accent app-card" :class="cardAccentClass(item.name)">
							<!-- 水印只做视觉记忆点，pointer-events 已在全局关掉，不会挡住卡片交互 -->
							<i class="ri-robot-2-fill entity-card__watermark" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i class="ri-robot-2-fill" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ item.name }}</div>
									<div class="entity-card__sub">{{ item.type || '通用助手' }} · {{ formatTime(item.createTime) }}</div>
								</div>
								<el-tag size="small" effect="light" :type="cardAccent(item.name)">{{ item.aiPromptName || '未绑定提示词' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc">{{ item.describe || '暂无描述' }}</div>

								<!-- 运行参数放进指标块：散字很难对齐，装进方块后同排卡片的数值落在同一条基线上 -->
								<div class="metric-group">
									<div class="metric-item">
										<span>温度</span><b>{{ ((item.temperature ?? 0) / 100).toFixed(2) }}</b>
									</div>
									<div class="metric-item">
										<span>输出</span><b>{{ item.msgType === 2 ? '流式' : '非流式' }}</b>
									</div>
									<div class="metric-item">
										<span>能力</span><b>{{ agentCapabilityTags(item).length }} 项</b>
									</div>
								</div>

								<div class="tag-group">
									<el-tag v-for="cap in agentCapabilityTags(item)" :key="cap.label" size="small" effect="light" :type="cap.type">{{ cap.label }}</el-tag>
									<span v-if="!agentCapabilityTags(item).length" class="cell-sub">未开启任何能力</span>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ item.kmsId ? '已绑定知识库' : '未绑定知识库' }}</span>
								<div class="entity-card__actions">
									<el-button icon="ele-Edit" text type="primary" @click="handleEdit(item)" v-auth="'agent/app/addEdit'"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click="handleDelete(item)" v-auth="'agent/app/delete'"> 删除 </el-button>
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
				@change="handleQuery"
			/>
		</el-card>

		<!-- 弹窗高度随「智能体信息」这个 tab 的内容走，其余 tab 至少留同样高度，切 tab 时窗口不再上下跳 -->
		<el-dialog v-model="state.editVisible" width="960px" draggable top="5vh" class="app-edit-dialog">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>{{ state.editTitle }}</span>
				</div>
			</template>
			<AppEditDialog v-if="state.editVisible" :app-id="state.currentAppId" @close="state.editVisible = false" @saved="onSaved" />
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="agentApp">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { AgentApi } from '/@/api-services/agent/api';
import { AgentAppDto, AgentPageInput } from '/@/api-services/agent/models';
import { cardAccent, cardAccentClass } from '/@/views/agent/cardAccent';
import { agentCapabilityTags } from '/@/views/agent/capabilityTags';
import AppEditDialog from './component/appEditDialog.vue';

/** currentAppId 为空即新增：编辑与新增共用同一个弹窗，由它区分 */
// 列表、查询参数与弹窗状态
const state = reactive({
	loading: false,
	total: 0,
	tableData: [] as AgentAppDto[],
	queryParams: { page: 1, pageSize: 12, searchKey: '' } as AgentPageInput,
	editVisible: false,
	editTitle: '',
	currentAppId: undefined as number | undefined,
});

/** 卡片上只用到日期：完整时间戳会把标题挤成省略号 */
// 日期只显示到天
const formatTime = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 10) : '-');

/** 头图统计：总数取接口返回值，知识库与流式按当前页汇总（列表接口不返回全量明细） */
const heroStats = computed(() => ({
	total: state.total,
	kms: state.tableData.filter((t) => !!t.kmsId).length,
	stream: state.tableData.filter((t) => t.msgType === 2).length,
}));

// 分页查询智能体
const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await getAPI(AgentApi).apiAgentPagePost(state.queryParams);
		state.tableData = res.data.result?.items ?? [];
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

// 重置查询条件并重新查询
const resetQuery = () => {
	state.queryParams.searchKey = '';
	state.queryParams.page = 1;
	handleQuery();
};

// 打开新增弹窗
const handleAdd = () => {
	state.editTitle = '新增智能体';
	state.currentAppId = undefined;
	state.editVisible = true;
};

// 打开编辑弹窗并回填数据
const handleEdit = (row: AgentAppDto) => {
	state.editTitle = '编辑智能体';
	state.currentAppId = row.id;
	state.editVisible = true;
};

// 弹窗保存成功后刷新列表
const onSaved = () => {
	state.editVisible = false;
	handleQuery();
};

// 删除智能体：取消确认框时静默返回，避免未捕获的 rejection 打断后续逻辑
const handleDelete = async (row: AgentAppDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除智能体「${row.name}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentApi).apiAgentDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

// 首次进入加载列表
onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
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
</style>
