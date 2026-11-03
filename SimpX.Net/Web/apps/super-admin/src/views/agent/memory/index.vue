<template>
	<div class="agents-memory-container">
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
					<h2 class="page-hero__title">记忆管理</h2>
					<p class="page-hero__desc">AI 在对话中通过 SaveMemory 自动写入值得长期记住的信息，此处可查询、修改与删除，删除后 AI 将无法再检索到。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">记忆总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-off">{{ heroStats.high }}</span>
							<span class="page-hero__stat-k">高重要度</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.types }}</span>
							<span class="page-hero__stat-k">涉及类型</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：记忆只能由 AI 写入，这里没有新增按钮，只放查询与重置 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="记忆内容 / 关键词 / 类型" clearable @keyup.enter="handleQuery(true)" />
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
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="160" description="暂无记忆，AI 在对话中积累到值得长期记住的信息时会自动写入" />
					</template>
					<template #row_content="{ row }">
						<!-- 一条记忆内容通常一到两行，给两行再省略比单行截断更好读 -->
						<div class="memory-text">{{ row.content }}</div>
					</template>
					<template #row_keywords="{ row }">
						<div class="kw-list">
							<template v-if="kwPreview(row).shown.length">
								<span v-for="kw in kwPreview(row).shown" :key="kw" class="kw-chip">{{ kw }}</span>
								<span v-if="kwPreview(row).more" class="kw-chip is-more">+{{ kwPreview(row).more }}</span>
							</template>
							<span v-else class="cell-sub">—</span>
						</div>
					</template>
					<template #row_memoryType="{ row }">
						<span class="type-pill" :class="`is-${getMemoryType(row.memoryType)?.tag ?? 'info'}`"> <i></i>{{ getMemoryType(row.memoryType)?.label ?? row.memoryType }} </span>
					</template>
					<template #row_importance="{ row }">
						<!-- 10 格分段条代替细进度条：0-10 的整数刻度逐格可见，扫一眼就知道比旁边那条多几格 -->
						<div class="imp" :class="{ 'is-high': (row.importance ?? 0) >= 8 }" :title="`重要度 ${row.importance ?? 0}/10`">
							<span class="imp__bars">
								<i v-for="n in 10" :key="n" :class="{ 'is-on': n <= (row.importance ?? 0) }"></i>
							</span>
							<span class="imp__num">{{ row.importance ?? 0 }}</span>
						</div>
					</template>
					<template #row_owner="{ row }">
						<div class="owner-cell">
							<span class="owner-cell__user">{{ row.userName || row.userId }}</span>
							<span class="cell-sub">{{ row.appName || row.aiAppsId || '未关联智能体' }}</span>
						</div>
					</template>
					<template #row_time="{ row }">
						<div class="time-cell">
							<span class="time-cell__main">{{ formatDate(row.createTime) }}</span>
							<span class="cell-sub" :class="`is-${expireState(row.expireTime)}`">
								{{ row.expireTime ? `过期 ${formatDate(row.expireTime).slice(0, 10)}` : '长期有效' }}
							</span>
						</div>
					</template>
					<template #row_buttons="{ row }">
						<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'agent/memory/update'"> 修改 </el-button>
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'agent/memory/delete'"> 删除 </el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<!-- 修改（不提供新增：记忆只能由 AI 写入） -->
		<el-dialog v-model="state.editVisible" width="720px" draggable>
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>修改记忆</span>
				</div>
			</template>
			<el-form :model="state.form" ref="editFormRef" :rules="rules" label-width="90px">
				<el-form-item label="记忆内容" prop="content">
					<el-input v-model="state.form.content" type="textarea" :rows="5" maxlength="2000" show-word-limit placeholder="请输入记忆内容" />
				</el-form-item>
				<el-form-item label="关键词" prop="keywords">
					<el-input v-model="state.form.keywords" placeholder="核心实体词，英文逗号分隔" maxlength="200" show-word-limit clearable />
				</el-form-item>
				<el-form-item label="记忆类型" prop="memoryType">
					<el-select v-model="state.form.memoryType" placeholder="请选择记忆类型" style="width: 100%" @change="onMemoryTypeChange">
						<el-option v-for="item in memoryTypeOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="重要度" prop="importance">
					<el-slider v-model="state.form.importance" :min="0" :max="10" :step="1" show-input />
				</el-form-item>
				<el-form-item v-if="state.form.memoryType === 'task'" label="过期时间" prop="expireTime">
					<el-date-picker v-model="state.form.expireTime" type="datetime" placeholder="短期记忆必须设置过期时间" format="YYYY-MM-DD HH:mm:ss" value-format="YYYY-MM-DD HH:mm:ss" style="width: 100%" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.saving" @click="handleSubmit">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="agentMemory">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance, FormRules } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

import { getAPI } from '/@/utils/axios-utils';
import { AgentMemoryApi } from '/@/api-services/agent/api';
import { AgentMemoryDto, AgentPageInput } from '/@/api-services/agent/models';

const xGrid = ref<VxeGridInstance>();
const editFormRef = ref<FormInstance>();

/** 记忆类型：value 与后端 SaveMemory 写入的类型一致，tag 只决定表格里标签的颜色 */
type MemoryTypeTag = 'primary' | 'success' | 'info' | 'warning' | 'danger';

const memoryTypeOptions: { value: string; label: string; tag: MemoryTypeTag }[] = [
	{ value: 'preference', label: '偏好', tag: 'success' },
	{ value: 'fact', label: '事实', tag: 'primary' },
	{ value: 'task', label: '短期', tag: 'warning' },
	{ value: 'decision', label: '决策', tag: 'info' },
	{ value: 'pitfall', label: '踩坑', tag: 'danger' },
	{ value: 'skill', label: '技能', tag: 'success' },
	{ value: 'other', label: '其他', tag: 'info' },
];

/** 未登记的类型（后端新增但前端未同步）返回 undefined，表格里直接显示原值，不显示成空白 */
const getMemoryType = (memoryType?: string | null) => memoryTypeOptions.find((item) => item.value === memoryType);

/** 过期时间只有短期记忆（task）能填，且必须晚于当前时间；非 task 填了要报错，避免静默失效 */
const rules: FormRules = {
	content: [{ required: true, message: '请输入记忆内容', trigger: 'blur' }],
	memoryType: [{ required: true, message: '请选择记忆类型', trigger: 'change' }],
	expireTime: [
		{
			validator: (_rule: unknown, value: string, callback: (error?: Error) => void) => {
				if (state.form.memoryType === 'task' && !value) return callback(new Error('短期记忆（task）必须设置过期时间'));
				if (state.form.memoryType !== 'task' && value) return callback(new Error('只有短期记忆（task）可以设置过期时间'));
				if (value && new Date(String(value).replace(/-/g, '/')) <= new Date()) return callback(new Error('过期时间必须大于当前时间'));
				callback();
			},
			trigger: 'change',
		},
	],
};

const state = reactive({
	saving: false,
	queryParams: { page: 1, pageSize: 20, searchKey: '' } as AgentPageInput,
	editVisible: false,
	form: {} as AgentMemoryDto,
});

/** 表格里只显示到分钟：秒级精度对记忆没有意义，还会把这一列撑宽 */
const formatDate = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 16) : '-');

/** 关键词按逗号拆成胶囊，列窄只放前两个，剩余用 +N 表示，避免把单元格挤成三行 */
const kwPreview = (row: AgentMemoryDto) => {
	const list = (row.keywords || '')
		.split(/[,，]/)
		.map((k) => k.trim())
		.filter(Boolean);
	return { shown: list.slice(0, 2), more: Math.max(list.length - 2, 0) };
};

/** 过期状态：已过期=红、7 天内到期=橙、其余灰；只给第二行上色，不影响主信息 */
const expireState = (value?: string | Date | null) => {
	if (!value) return 'long';
	const time = new Date(String(value).replace('T', ' ').replace(/-/g, '/')).getTime();
	if (time <= Date.now()) return 'expired';
	if (time - Date.now() < 7 * 24 * 3600 * 1000) return 'soon';
	return 'idle';
};

// 表格参数配置
const options = useVxeTable<AgentMemoryDto>(
	{
		id: 'agentsMemory',
		name: 'AI记忆',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left', align: 'center', headerAlign: 'center' },
			// 列宽分配：只有 minWidth 的列会分摊剩余宽度，关键词与归属一起分摊，
			// 否则剩余空间全被记忆内容吃光（实测独占 632px，其它列被挤窄）
			{ field: 'content', title: '记忆内容', minWidth: 280, slots: { default: 'row_content' } },
			{ field: 'keywords', title: '关键词', minWidth: 170, showOverflow: 'tooltip', slots: { default: 'row_keywords' } },
			{ field: 'memoryType', title: '类型', width: 100, align: 'center', headerAlign: 'center', slots: { default: 'row_memoryType' } },
			{ field: 'importance', title: '重要度', width: 150, slots: { default: 'row_importance' } },
			{ field: 'owner', title: '归属', minWidth: 170, slots: { default: 'row_owner' } },
			{ field: 'time', title: '时间', width: 140, slots: { default: 'row_time' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', align: 'center', headerAlign: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 文字列一律左对齐（hook 默认是 center，标题居中而内容左对齐会看着对不上），
		// 序号/类型/操作三列在列上单独改回居中
		align: 'left',
		headerAlign: 'left',
		// 50px：记忆内容是两行 40px、归属/时间是两行 34px，默认 36px 会把第二行裁掉 12px（看着像两行叠在一起）
		// vxe 4.23 起行高改由 cellConfig.height 控制（rowConfig.height 已废弃）
		cellConfig: { height: 50 },
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		// 记忆页面不放任何工具按钮，表格上方的导出/刷新/缩放工具栏一并隐藏
		toolbarConfig: { enabled: false },
	}
);

// 表格事件
const gridEvents: VxeGridListeners<AgentMemoryDto> = {};

// 头图统计：总数取接口 total，其余按当前页数据汇总，不额外请求接口
const heroStats = reactive({ total: 0, high: 0, types: 0 });

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await getAPI(AgentMemoryApi).apiAgentMemoryPagePost(params);
	const items = res.data.result?.items ?? [];
	heroStats.total = res.data.result?.total ?? 0;
	heroStats.high = items.filter((r) => (r.importance ?? 0) >= 8).length;
	heroStats.types = new Set(items.map((r) => r.memoryType).filter(Boolean)).size;
	return res;
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	try {
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	} finally {
		options.loading = false;
	}
};

const resetQuery = () => {
	state.queryParams.searchKey = '';
	handleQuery(true);
};

const handleEdit = (row: AgentMemoryDto) => {
	state.form = {
		...row,
		// el-date-picker 只认 yyyy-MM-dd HH:mm:ss 字符串，后端返回的 ISO 带 T 和毫秒，这里统一转一次
		// expireTime 字段类型是 Date | null，回填时把后端返回的时间串解析成 Date（replace(/-/g,'/') 兼容 Safari 解析）
		expireTime: row.expireTime ? new Date(String(row.expireTime).replace('T', ' ').replace(/-/g, '/')) : null,
	};
	state.editVisible = true;
};

const onMemoryTypeChange = () => {
	if (state.form.memoryType !== 'task') state.form.expireTime = null;
};

const handleSubmit = async () => {
	await editFormRef.value?.validate();
	state.saving = true;
	try {
		await getAPI(AgentMemoryApi).apiAgentMemoryUpdatePost(state.form);
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery();
	} finally {
		state.saving = false;
	}
};

const handleDelete = async (row: AgentMemoryDto) => {
	try {
		await ElMessageBox.confirm('确定要删除该条记忆吗？删除后 AI 将无法再检索到它。', '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentMemoryApi).apiAgentMemoryDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* 类型胶囊：与状态胶囊同族视觉，但不带底色描边 —— 类型是分类信息，
 * 与「成功/失败」这类需要强调的状态区分开，避免整表被一圈圈色块填满 */
.type-pill {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	line-height: 22px;

	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background-color: currentColor;
	}
}

.type-pill.is-primary {
	color: var(--el-color-primary);
}

.type-pill.is-success {
	color: var(--el-color-success);
}

.type-pill.is-warning {
	color: var(--el-color-warning);
}

.type-pill.is-danger {
	color: var(--el-color-danger);
}

.type-pill.is-info {
	color: var(--el-color-info);
}

/* 两行时间单元格：日期/摘要在上，过期状态在下 */
.time-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
	font-variant-numeric: tabular-nums;

	.cell-sub {
		margin-top: 0;
		line-height: 16px;
	}

	.cell-sub.is-expired {
		color: var(--el-color-danger);
	}

	.cell-sub.is-soon {
		color: var(--el-color-warning);
	}

	.cell-sub.is-long {
		color: var(--el-color-success);
	}
}

.time-cell__main {
	font-size: 13px;
	line-height: 16px;
	color: var(--el-text-color-primary);
}

/* 记忆正文：两行以内完整显示，超出再省略——单行截断会把记住了什么切掉一半。
 * white-space 必须显式写 normal：vxe 单元格默认 nowrap，不覆盖的话文字不换行，
 * -webkit-line-clamp 就只会把一行硬切掉，两行省略形同失效 */
.memory-text {
	display: -webkit-box;
	-webkit-box-orient: vertical;
	-webkit-line-clamp: 2;
	overflow: hidden;
	white-space: normal;
	word-break: break-word;
	font-size: 13px;
	line-height: 20px;
	color: var(--el-text-color-primary);
}

/* 关键词：小胶囊排开，比一段逗号分隔的字符串更容易逐词扫读 */
.kw-list {
	display: flex;
	align-items: center;
	flex-wrap: nowrap;
	gap: 4px;
	overflow: hidden;
}

.kw-chip {
	flex-shrink: 0;
	display: inline-flex;
	align-items: center;
	height: 20px;
	max-width: 78px;
	padding: 0 7px;
	border-radius: 6px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-light);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.kw-chip.is-more {
	max-width: none;
	color: var(--el-text-color-secondary);
	background-color: transparent;
	padding: 0 2px;
}

/* 重要度：10 格分段条。0-10 是离散刻度，分格比连续进度条更容易一眼比对；
 * ≥8 的高重要度转成警示色，让"这条要优先看"在整表里跳出来 */
.imp {
	display: flex;
	align-items: center;
	gap: 8px;
}

.imp__bars {
	display: flex;
	align-items: center;
	gap: 3px;

	i {
		width: 5px;
		height: 12px;
		border-radius: 2px;
		background-color: var(--el-fill-color);
	}

	i.is-on {
		background-color: var(--el-color-primary);
	}
}

.imp.is-high .imp__bars i.is-on {
	background-color: var(--el-color-warning);
}

.imp__num {
	flex-shrink: 0;
	width: 16px;
	font-size: 12px;
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);
	text-align: right;
}

.imp.is-high .imp__num {
	color: var(--el-color-warning);
}

/* 归属：用户名在上（主信息），所属智能体在下（补充）。
 * 两行用独立类而不是两个 .cell-sub —— 后者字重色阶相同，主次分不开 */
.owner-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
}

.owner-cell__user {
	font-size: 13px;
	line-height: 16px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.owner-cell .cell-sub {
	margin-top: 0;
	line-height: 16px;
}
</style>
