<template>
	<div class="agents-prompt-container">
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
					<h2 class="page-hero__title">提示词管理</h2>
					<p class="page-hero__desc">沉淀可复用的系统提示词：绑定到智能体后作为其系统规则，一条提示词可同时服务多个智能体。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">提示词总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.chars }}</span>
							<span class="page-hero__stat-k">本页总字数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.avgChars }}</span>
							<span class="page-hero__stat-k">本页平均字数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增按钮放这里，不进卡片区，避免随卡片滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="提示词名称 / 描述" clearable @keyup.enter="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'agent/prompt/addEdit'"> 新增提示词 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty :image-size="160" description="暂无提示词，新增后可在智能体里绑定" v-if="!state.tableData.length" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.tableData" :key="item.id">
						<el-card shadow="never" class="entity-card entity-card--accent prompt-card is-clickable" :class="cardAccentClass(item.name)" @click="handleView(item)">
							<!-- 水印与智能体卡片同规格，只把图标换成笔，靠图形区分页面而不是靠更重的装饰 -->
							<i class="ri-quill-pen-fill entity-card__watermark" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i class="ri-quill-pen-fill" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ item.name }}</div>
									<div class="entity-card__sub">{{ item.createUserName || '系统' }} 创建</div>
								</div>
								<el-tag size="small" effect="light" :type="cardAccent(item.name)">{{ formatShortDate(item.createTime) }}</el-tag>
							</div>

							<div class="entity-card__body">
								<!-- 描述单独成块：两行裁切，和标题一起构成「这条提示词做什么」的完整说明 -->
								<div class="card-desc prompt-desc">{{ item.description || '暂无描述' }}</div>

								<!-- 正文预览是这张卡的主角：保留换行，只能扫读前几行，完整内容点开看 -->
								<div class="code-preview prompt-preview">{{ item.prompt }}</div>
							</div>

							<div class="entity-card__foot">
								<!-- 字数/行数改成页脚一行小字：比占满一行的指标块省下半张卡的高度 -->
								<span class="prompt-size">{{ formatCount((item.prompt || '').length) }} 字 · {{ formatCount(lineCount(item.prompt)) }} 行</span>
								<div class="entity-card__actions">
									<el-button icon="ele-Edit" text type="primary" @click.stop="handleEdit(item)" v-auth="'agent/prompt/addEdit'"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click.stop="handleDelete(item)" v-auth="'agent/prompt/delete'"> 删除 </el-button>
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

		<!-- 新增/编辑 -->
		<el-dialog v-model="state.editVisible" width="720px" draggable>
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>{{ state.editTitle }}</span>
				</div>
			</template>
			<el-form :model="state.form" ref="editFormRef" :rules="rules" label-width="auto">
				<el-form-item label="名称" prop="name">
					<el-input v-model="state.form.name" placeholder="请输入提示词名称" maxlength="100" show-word-limit clearable />
				</el-form-item>
				<el-form-item label="描述" prop="description">
					<el-input v-model="state.form.description" placeholder="请输入描述" maxlength="500" show-word-limit clearable />
				</el-form-item>
				<el-form-item label="提示词" prop="prompt">
					<el-input v-model="state.form.prompt" type="textarea" :rows="16" maxlength="5000" show-word-limit placeholder="请输入提示词内容" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.saving" @click="handleSubmit">确定</el-button>
			</template>
		</el-dialog>

		<!-- 查看 -->
		<el-dialog v-model="state.viewVisible" width="720px" draggable>
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-View /></el-icon>
					<span>提示词详情</span>
				</div>
			</template>
			<el-descriptions :column="1" border>
				<el-descriptions-item label="名称">{{ state.form.name }}</el-descriptions-item>
				<el-descriptions-item label="描述">{{ state.form.description || '-' }}</el-descriptions-item>
				<el-descriptions-item label="字数">{{ (state.form.prompt || '').length }}</el-descriptions-item>
			</el-descriptions>
			<el-input style="margin-top: 10px" v-model="state.form.prompt" type="textarea" :rows="16" readonly />
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="agentPrompt">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance } from 'element-plus';

import { cardAccent, cardAccentClass } from '/@/views/agent/cardAccent';
import { getAPI } from '/@/utils/axios-utils';
import { AgentPromptApi } from '/@/api-services/agent/api';
import { AgentPromptDto, AgentPageInput } from '/@/api-services/agent/models';

// 新增/编辑表单实例，提交前用于校验
const editFormRef = ref<FormInstance>();

// 表单校验规则
const rules = {
	name: [{ required: true, message: '请输入提示词名称', trigger: 'blur' }],
	prompt: [{ required: true, message: '请输入提示词内容', trigger: 'blur' }],
};

// 列表、查询参数与编辑表单
const state = reactive({
	loading: false,
	saving: false,
	total: 0,
	tableData: [] as AgentPromptDto[],
	queryParams: { page: 1, pageSize: 24, searchKey: '' } as AgentPageInput,
	editVisible: false,
	viewVisible: false,
	editTitle: '',
	form: {} as AgentPromptDto,
});

/** 卡片上只展示到日期：完整时间戳在卡片里太长，抢了标题的位置 */
// 日期只显示到天
const formatShortDate = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 10) : '-');

/** 非空行数：提示词常按行分节，行数比字数更能反映结构复杂度 */
// 统计提示词非空行数
const lineCount = (prompt?: string | null) => (prompt ? prompt.split('\n').filter((line) => line.trim()).length : 0);

/** 千分位：提示词动辄几千字，四位数连写不好一眼读出来 */
const formatCount = (value: number) => value.toLocaleString();

/** 头图统计：总数取接口返回值，字数只统计当前页（列表接口不下发全量正文） */
const heroStats = computed(() => {
	const chars = state.tableData.reduce((sum, t) => sum + (t.prompt?.length ?? 0), 0);
	return {
		total: state.total,
		chars,
		avgChars: state.tableData.length ? Math.round(chars / state.tableData.length) : 0,
	};
});

// 分页查询提示词
const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await getAPI(AgentPromptApi).apiAgentPromptPagePost(state.queryParams);
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
	state.editTitle = '新增提示词';
	state.form = { name: '', prompt: '', description: '' };
	state.editVisible = true;
};

// 打开编辑弹窗并回填数据
const handleEdit = (row: AgentPromptDto) => {
	state.editTitle = '编辑提示词';
	state.form = { ...row };
	state.editVisible = true;
};

// 查看提示词详情
const handleView = (row: AgentPromptDto) => {
	state.form = { ...row };
	state.viewVisible = true;
};

// 提交新增或编辑
const handleSubmit = async () => {
	await editFormRef.value?.validate();
	state.saving = true;
	try {
		await getAPI(AgentPromptApi).apiAgentPromptAddEditPost(state.form);
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery();
	} finally {
		state.saving = false;
	}
};

// 删除提示词：取消确认框时静默返回
const handleDelete = async (row: AgentPromptDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除提示词「${row.name}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentPromptApi).apiAgentPromptDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* ============================== 提示词卡片 ============================== */
/* 与智能体卡片同一套观感：只在上沿叠一条很淡的顶色带，鼠标移入时才亮起来 */
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

/* 描述固定两行：高度由全局 .card-desc 锁住，同排卡片的预览因此逐行对齐 */
.prompt-desc {
	color: var(--el-text-color-regular);
}

/* 正文只留四行：上方还有两行描述，预览再长整张卡就偏高，卡片墙一屏放不下几列 */
.prompt-preview {
	-webkit-line-clamp: 4;
	height: 88px;
	color: var(--el-text-color-secondary);
}

/* 页脚里的字数/行数：弱化成一行说明文字，与右侧操作同一基线 */
.prompt-size {
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	font-variant-numeric: tabular-nums;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* 页脚上下留白略收：卡片已经压矮，12px 会显得上下各空一截 */
.prompt-card .entity-card__foot {
	padding: 10px 0;
}
</style>
