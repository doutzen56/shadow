<template>
	<div class="agents-kms-container">
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
					<h2 class="page-hero__title">知识库管理</h2>
					<p class="page-hero__desc">上传文档后自动解析、分块并写入向量库；智能体在对话中按需检索，命中结果带原文出处。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">知识库总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': heroStats.processing === 0 }">{{ heroStats.processing }}</span>
							<span class="page-hero__stat-k">本页处理中</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.fail }}</span>
							<span class="page-hero__stat-k">本页存在失败</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增按钮放这里，不进表格卡片，避免随内容滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="知识库名称" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'agent/kms/addEdit'"> 新增知识库 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<!-- 新增按钮已移到查询区，这里留空插槽：vxe 在 toolbarConfig 开启时会检查它是否存在，缺了会告警 -->
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="暂无知识库，新增后上传文档即可自动向量化" />
				</template>
				<!-- 展开行：每个知识库下的文档以小卡片铺开（文档字段少，卡片比嵌套子表格更好读） -->
				<template #row_expand="{ row }">
					<div class="doc-cards" v-if="row.aiKmssDetailsList?.length">
						<div class="doc-card" v-for="detail in row.aiKmssDetailsList" :key="detail.id ?? detail.contentName">
							<div class="doc-card__head">
								<span class="doc-card__icon"
									><el-icon :size="13"><ele-Document /></el-icon
								></span>
								<span class="doc-card__name" :title="detail.contentName">{{ detail.contentName || '未命名文档' }}</span>
								<span class="doc-card__type">{{ detail.fileType || 'text' }}</span>
							</div>
							<div class="doc-card__meta">
								<span class="doc-card__stat"
									>向量块 <b>{{ detail.dataCount ?? 0 }}</b></span
								>
								<span class="state-pill" :class="`is-${docStatusTag(detail.status)}`"><i></i>{{ docStatusText(detail.status) }}</span>
							</div>
							<div v-if="detail.status === 2 && detail.errorMessage" class="doc-card__error" :title="detail.errorMessage">{{ detail.errorMessage }}</div>
						</div>
					</div>
					<div v-else class="doc-cards-empty">该知识库暂无文档，编辑后上传即可自动向量化</div>
				</template>
				<template #row_name="{ row }">
					<div class="name-cell">
						<span class="name-cell__icon"
							><el-icon :size="14"><ele-Collection /></el-icon
						></span>
						<span class="name-cell__text">{{ row.name }}</span>
					</div>
				</template>
				<template #row_docCount="{ row }">
					<span class="doc-badge">{{ row.documentCount ?? 0 }}</span>
				</template>
				<template #row_status="{ row }">
					<span class="state-pill" :class="`is-${kmsStatusTag(row.status)}`"><i></i>{{ kmsStatusText(row.status) }}</span>
				</template>
				<!-- 三个切片参数横向排开：竖排三行会把行高撑到别的列两倍，整表看着不齐 -->
				<template #row_slice="{ row }">
					<div class="slice-params">
						<span class="slice-chip"><em>段落</em>{{ row.maxTokensPerParagraph }}</span>
						<span class="slice-chip"><em>行</em>{{ row.maxTokensPerLine }}</span>
						<span class="slice-chip"><em>重叠</em>{{ row.overlappingTokens }}</span>
					</div>
				</template>
				<template #row_createTime="{ row }">
					<div class="time-cell">
						<span class="time-cell__main">{{ splitTime(row.createTime).date }}</span>
						<span class="cell-sub">{{ splitTime(row.createTime).time }}</span>
					</div>
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-Share" text type="primary" @click="handleGraph(row)"> 图谱 </el-button>
					<el-button icon="ele-VideoPlay" text type="success" :loading="state.runningIds.includes(row.id!)" @click="handleRun(row)" v-auth="'agent/kms/run'">
						{{ state.runningIds.includes(row.id!) ? '处理中' : '立即执行' }}
					</el-button>
					<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'agent/kms/addEdit'"> 编辑 </el-button>
					<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'agent/kms/delete'"> 删除 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.editVisible" width="960px" draggable top="5vh">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>{{ state.editTitle }}</span>
				</div>
			</template>
			<KmsEditDialog v-if="state.editVisible" :kms-id="state.currentKmsId" @close="state.editVisible = false" @saved="onSaved" />
		</el-dialog>

		<KmsGraph v-model:visible="state.graphVisible" :kms-id="state.graphKmsId" :kms-title="state.graphKmsTitle" />
	</div>
</template>

<script lang="ts" setup name="agentKms">
import { onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

import KmsEditDialog from './component/kmsEditDialog.vue';
import KmsGraph from './component/kmsGraph.vue';

import { getAPI } from '/@/utils/axios-utils';
import { AgentKmsApi } from '/@/api-services/agent/api';
import { AgentKmsDto, AgentPageInput } from '/@/api-services/agent/models';

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: { page: 1, pageSize: 20, searchKey: '' } as AgentPageInput,
	editVisible: false,
	editTitle: '',
	currentKmsId: undefined as number | undefined,
	/** 知识图谱弹窗（与编辑弹窗的 currentKmsId 分开，避免关掉图谱后编辑弹窗拿到旧的 Id） */
	graphVisible: false,
	graphKmsId: undefined as number | undefined,
	graphKmsTitle: '',
	/** 正在后台处理的知识库 Id：按钮转圈 + 文案切「处理中」，同时防重复点击 */
	runningIds: [] as number[],
});

/** 状态：3=存在失败文档、1=处理中、其余视为已完成（与后端 KmsStatus 一致） */
const kmsStatusText = (status?: number) => (status === 3 ? '存在失败' : status === 1 ? '处理中' : '已完成');
const kmsStatusTag = (status?: number) => (status === 3 ? 'danger' : status === 1 ? 'warning' : 'success');

/** 文档向量化状态：1=已完成、2=失败、其余待处理（与主表 KmsStatus 的枚举不同） */
const docStatusText = (status?: number) => (status === 1 ? '已完成' : status === 2 ? '失败' : '待处理');
const docStatusTag = (status?: number) => (status === 1 ? 'success' : status === 2 ? 'danger' : 'warning');

/** 创建时间拆成日期 + 时刻两行：一列放完整时间要 165px，拆开后 120px 就够，且日期更醒目 */
const splitTime = (value?: string | Date | null) => {
	const text = value ? String(value).replace('T', ' ').substring(0, 19) : '';
	return { date: text ? text.slice(0, 10) : '-', time: text ? text.slice(11, 16) : '' };
};

// 表格参数配置
const options = useVxeTable<AgentKmsDto>(
	{
		id: 'agentsKms',
		name: '知识库',
		columns: [
			{ field: 'expand', type: 'expand', width: 40, fixed: 'left', align: 'center', headerAlign: 'center', slots: { content: 'row_expand' } },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left', align: 'center', headerAlign: 'center' },
			// 列宽分配：只有 minWidth 的列会分摊剩余宽度，所以让名称/切片/创建人三列一起分摊，
			// 否则剩余空间全被名称吃光（实测名称独占 752px，切片与创建时间被挤到放不下内容）
			{ field: 'name', title: '名称', minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'documentCount', title: '文档数', width: 90, align: 'center', headerAlign: 'center', slots: { default: 'row_docCount' } },
			{ field: 'sliceParams', title: '切片参数（段落 / 行 / 重叠）', minWidth: 240, slots: { default: 'row_slice' } },
			{ field: 'status', title: '状态', width: 100, align: 'center', headerAlign: 'center', slots: { default: 'row_status' } },
			{ field: 'createUserName', title: '创建人', minWidth: 110, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '创建时间', width: 130, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', align: 'center', headerAlign: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 文字列一律左对齐（hook 默认是 center，标题居中而内容左对齐会看着对不上），
		// 数值/状态/操作三列在列上单独改回居中
		align: 'left',
		headerAlign: 'left',
		// 48px：创建时间拆成两行后需要 36px，再留一点上下留白，行不再显得压扁
		// vxe 4.23 起行高改由 cellConfig.height 控制（rowConfig.height 已废弃）
		cellConfig: { height: 48 },
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 表格事件
const gridEvents: VxeGridListeners<AgentKmsDto> = {};

/** 头图统计：总数取接口 total，处理中/存在失败按当前页聚合（与表格状态列同源） */
const heroStats = reactive({ total: 0, processing: 0, fail: 0 });

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await getAPI(AgentKmsApi).apiAgentKmsPagePost(params);
	const items = res.data.result?.items ?? [];
	heroStats.total = res.data.result?.total ?? 0;
	heroStats.processing = items.filter((t) => t.status === 1).length;
	heroStats.fail = items.filter((t) => t.status === 3).length;
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

const handleAdd = () => {
	state.editTitle = '新增知识库';
	state.currentKmsId = undefined;
	state.editVisible = true;
};

const handleEdit = (row: AgentKmsDto) => {
	state.editTitle = '编辑知识库';
	state.currentKmsId = row.id;
	state.editVisible = true;
};

const onSaved = () => {
	state.editVisible = false;
	handleQuery();
};

/** 知识图谱：只读展示，不校验按钮权限（看图谱不改变任何数据） */
const handleGraph = (row: AgentKmsDto) => {
	state.graphKmsId = row.id;
	state.graphKmsTitle = row.name ?? '';
	state.graphVisible = true;
};

const handleDelete = async (row: AgentKmsDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除知识库「${row.name}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentKmsApi).apiAgentKmsDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

/* ------------------------------ 立即执行向量化 ------------------------------ */

/** 文档状态：与后端 ImportKmsStatusEnum 一致（0 待处理 / 1 已完成 / 2 失败） */
const DOC_STATUS_FAIL = 2;

/** 轮询上限与间隔：向量化是后台任务，按这个节奏刷新 3 次左右就能看到状态变化 */
const POLL_INTERVAL_MS = 5000;
const POLL_MAX_TIMES = 12;

let pollTimer: ReturnType<typeof setTimeout> | null = null;

const stopPolling = () => {
	if (pollTimer) {
		clearTimeout(pollTimer);
		pollTimer = null;
	}
};

/** 提交后轮询表格：只要该知识库仍在「处理中」就继续刷新，直到完成或到达上限（Promise 在处理结束后才 resolve） */
const startPolling = (kmsId: number) => {
	stopPolling();
	return new Promise<void>((resolve) => {
		let times = 0;
		const tick = async () => {
			await handleQuery();
			times++;
			const row = (xGrid.value?.getData() ?? []).find((t: AgentKmsDto) => t.id === kmsId);
			// 主表状态由明细聚合：1=仍存在待处理文档（即处理中）
			if (times < POLL_MAX_TIMES && row?.status === 1) {
				pollTimer = setTimeout(tick, POLL_INTERVAL_MS);
			} else {
				pollTimer = null;
				resolve();
			}
		};
		pollTimer = setTimeout(tick, POLL_INTERVAL_MS);
	});
};

/** 立即执行：解析、分块与向量化交给后端后台任务，这里只负责提交与跟进进度 */
const handleRun = async (row: AgentKmsDto) => {
	const kmsId = row.id!;
	if (state.runningIds.includes(kmsId)) return;

	// 存在失败文档时让用户选：只跑待处理的，还是连失败的一起重跑
	let force = false;
	const hasFail = (row.aiKmssDetailsList ?? []).some((t) => t.status === DOC_STATUS_FAIL);
	if (hasFail) {
		try {
			await ElMessageBox.confirm('存在向量化失败的文档，请选择本次处理方式：', '立即执行', {
				confirmButtonText: '连失败文档一起重跑',
				cancelButtonText: '仅处理待处理文档',
				type: 'warning',
				distinguishCancelAndClose: true,
			});
			force = true;
		} catch (action) {
			// cancel=选「仅处理待处理」，close=直接关闭弹窗（放弃本次操作）
			if (action === 'close') return;
		}
	}

	state.runningIds.push(kmsId);
	try {
		const res = await getAPI(AgentKmsApi).apiAgentKmsRunPost({ kmsId, force });
		const count = res.data.result ?? 0;
		ElMessage.success(`已提交处理 ${count} 个文档，正在后台解析与向量化`);
		// 等到轮询结束（处理完成或超时）才放开按钮，避免用户在处理中重复提交
		await startPolling(kmsId);
	} catch (e: unknown) {
		const err = e as { message?: string };
		ElMessage.error(err?.message ?? '提交失败，请稍后重试');
	} finally {
		const index = state.runningIds.indexOf(kmsId);
		if (index >= 0) state.runningIds.splice(index, 1);
	}
};

onUnmounted(stopPolling);
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* 状态胶囊：圆点 + 文字，比 el-tag 更轻，也避免整表被色块切碎；
 * 颜色只由 --pill 一个变量驱动（底色/描边/圆点同源，不会出现深浅不一） */
.state-pill {
	--pill: var(--el-color-info);

	display: inline-flex;
	align-items: center;
	gap: 6px;
	height: 22px;
	padding: 0 9px;
	border-radius: 11px;
	font-size: 12px;
	color: var(--pill);
	background-color: color-mix(in srgb, var(--pill) 10%, transparent);
	border: 1px solid color-mix(in srgb, var(--pill) 24%, transparent);

	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background-color: currentColor;
	}
}

.state-pill.is-primary {
	--pill: var(--el-color-primary);
}

.state-pill.is-success {
	--pill: var(--el-color-success);
}

.state-pill.is-warning {
	--pill: var(--el-color-warning);
}

.state-pill.is-danger {
	--pill: var(--el-color-danger);
}

.state-pill.is-info {
	--pill: var(--el-color-info);
}

/* 两行时间单元格：日期在上、时刻在下。一列放完整时间要 165px，拆开后 130px 就够 */
.time-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
	font-variant-numeric: tabular-nums;

	.cell-sub {
		margin-top: 0;
		line-height: 16px;
	}
}

.time-cell__main {
	font-size: 13px;
	line-height: 16px;
	color: var(--el-text-color-primary);
}

/* 展开行：文档小卡片。文档只有名称/类型/向量块/状态四个字段，靠表头才能读懂的
 * 子表格信息密度太低，改成卡片横向自动换行，一张卡一眼读完 */
.doc-cards {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
	gap: 10px;
	margin: 4px 8px 10px 48px;
}

.doc-card {
	min-width: 0;
	padding: 10px 12px;
	border-radius: 10px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		border-color 0.2s ease,
		box-shadow 0.2s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 30%, var(--el-border-color-lighter));
		box-shadow: 0 8px 18px -14px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
	}
}

.doc-card__head {
	display: flex;
	align-items: center;
	gap: 7px;
	min-width: 0;
}

.doc-card__icon {
	flex-shrink: 0;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 22px;
	height: 22px;
	border-radius: 6px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.doc-card__name {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.doc-card__type {
	flex-shrink: 0;
	height: 18px;
	line-height: 18px;
	padding: 0 6px;
	border-radius: 5px;
	font-size: 11px;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	color: var(--el-text-color-secondary);
	background-color: var(--el-fill-color-light);
}

.doc-card__meta {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	margin-top: 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.doc-card__stat b {
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-primary);
}

/* 向量化失败时展示异常信息：只留一行，完整内容看 title 悬浮提示 */
.doc-card__error {
	margin-top: 7px;
	padding-top: 7px;
	border-top: 1px dashed var(--el-border-color-lighter);
	font-size: 11.5px;
	color: var(--el-color-danger);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.doc-cards-empty {
	padding: 4px 2px 6px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}

/* 名称列：图标色块 + 名称。整表其它列都是纯文字，首列给一个形状锚点，
 * 视线扫下来先落在名称上，而不是从一片同质文字里找起点 */
.name-cell {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.name-cell__icon {
	flex-shrink: 0;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.name-cell__text {
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 文档数：整行唯一的量化指标，用浅色胶囊承载，比裸数字更容易定位 */
.doc-badge {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-width: 26px;
	height: 20px;
	padding: 0 7px;
	border-radius: 10px;
	font-size: 12px;
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

/* 切片参数：三项各是一个「标签 + 数值」小胶囊，横向比对三个数值时不用先找分隔 */
.slice-params {
	display: flex;
	align-items: center;
	gap: 6px;
	font-variant-numeric: tabular-nums;
}

.slice-chip {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	height: 20px;
	padding: 0 7px;
	border-radius: 6px;
	font-size: 12px;
	color: var(--el-text-color-primary);
	background-color: var(--el-fill-color-light);
}

.slice-chip em {
	font-style: normal;
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

</style>
