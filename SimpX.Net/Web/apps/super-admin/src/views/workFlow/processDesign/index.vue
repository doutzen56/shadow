<template>
	<div class="workFlow-processDesign-container">
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
					<h2 class="page-hero__title">流程设计</h2>
					<p class="page-hero__desc">可视化编排审批流程，支持版本发布、启停与发布记录；可按名称、分类与状态筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">流程总数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：卡片式工具条，左侧条件 / 右侧动作，与职位管理等列表页保持同一套视觉语言 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="关键字" prop="keyword">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="流程名称 / 编码" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="分类" prop="category">
					<el-select v-model="state.queryParams.category" class="query-select" placeholder="全部分类" clearable filterable allow-create default-first-option>
						<el-option v-for="item in categoryOptions" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态" prop="status">
					<el-select v-model="state.queryParams.status" class="query-select" placeholder="全部状态" clearable>
						<el-option label="启用" :value="1" />
						<el-option label="停用" :value="0" />
					</el-select>
				</el-form-item>
			</el-form>

			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleSearch" :loading="options.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading">重置</el-button>
				</el-button-group>
				<!-- 卡片视图没有 vxe 工具栏，新建入口挂在这里 -->
				<el-button v-if="state.viewMode === 'card'" type="primary" icon="ele-Plus" @click="openCreate">新建流程</el-button>
				<el-button-group>
					<el-button type="primary" :plain="state.viewMode !== 'card'" icon="ele-Grid" @click="switchView('card')">卡片</el-button>
					<el-button type="primary" :plain="state.viewMode !== 'table'" icon="ele-List" @click="switchView('table')">表格</el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 卡片视图（默认）：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<template v-if="state.viewMode === 'card'">
			<el-card class="full-table" shadow="hover">
				<div class="card-grid" v-loading="options.loading">
					<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的流程，点击右上角「新建流程」开始设计" :image-size="120" />
					<el-row v-else :gutter="12" class="card-grid-row">
						<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
							<!-- 不放 entity-card__watermark：图标由用户按流程自选，默认 ele-Menu 是方框轮廓，
							     74px 压在右上角正好把状态标签框进去，看着像标签外面套了个框 -->
							<el-card shadow="never" class="entity-card entity-card--accent is-clickable is-primary" @dblclick="openDesign(row)">
								<div class="entity-card__head">
									<div class="entity-card__icon">
										<SvgIcon :name="row.icon || 'ele-Menu'" :size="20" />
									</div>
									<div class="entity-card__title-wrap">
										<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
										<div class="entity-card__sub" :title="row.code || ''">{{ row.code || '未设置编码' }}</div>
									</div>
									<el-tooltip :content="Number(row.status) === 1 ? '点击停用该流程' : '点击启用该流程'" placement="top">
										<el-tag size="small" effect="light" class="is-toggle" :type="Number(row.status) === 1 ? 'success' : 'info'" @click.stop="toggleStatus(row)">
											{{ Number(row.status) === 1 ? '启用' : '停用' }}
										</el-tag>
									</el-tooltip>
								</div>

								<div class="entity-card__body">
									<div class="card-desc" :title="row.remark || ''">{{ row.remark || '暂无备注' }}</div>

									<div class="tag-group">
										<el-tag size="small" effect="plain" type="info" :title="`分类：${row.category || '未分类'}`">{{ row.category || '未分类' }}</el-tag>
										<g-sys-dict :model-value="row.formType" code="FormTypeEnum" render-as="tag" size="small" />
										<el-tag size="small" effect="plain" type="info" :title="formChipTitle(row)">{{ formChipLabel(row) }}</el-tag>
										<g-sys-dict :model-value="row.publishStatus" code="WorkflowPublishStatusEnum" render-as="tag" size="small" />
									</div>

									<div class="metric-group">
										<div class="metric-item">
											<span>版本号</span><b>{{ row.latestVersion ? 'v' + row.latestVersion : '—' }}</b>
										</div>
										<div class="metric-item">
											<span>发布时间</span><b :title="formatTimeSec(row.latestPublishedTime)">{{ formatTimeSec(row.latestPublishedTime) }}</b>
										</div>
									</div>
								</div>

								<div class="entity-card__foot">
									<span class="cell-sub">{{ formatTimeSec(row.createTime) }}</span>
									<div class="entity-card__actions">
										<el-tooltip content="编辑基础信息" placement="top">
											<el-button icon="ele-Edit" text type="primary" @click.stop="openEdit(row)" />
										</el-tooltip>
										<el-tooltip content="流程设计" placement="top">
											<el-button icon="ele-EditPen" text type="primary" @click.stop="openDesign(row)" />
										</el-tooltip>
										<el-tooltip :content="publishTip(row)" placement="top">
											<span class="wf-op-wrap">
												<el-button icon="ele-Position" text type="success" :disabled="!canPublish(row)" @click.stop="openPublish(row)" />
											</span>
										</el-tooltip>
										<el-tooltip content="发布记录" placement="top">
											<el-button icon="ele-Document" text type="info" @click.stop="viewDefinitions(row)" />
										</el-tooltip>
										<el-tooltip content="删除流程" placement="top">
											<el-button icon="ele-Delete" text type="danger" @click.stop="handleDelete(row)" />
										</el-tooltip>
									</div>
								</div>
							</el-card>
						</el-col>
					</el-row>
				</div>

				<el-pagination
					v-if="cardPager.total > 0"
					class="table-pager"
					v-model:current-page="cardPager.page"
					v-model:page-size="cardPager.pageSize"
					:page-sizes="[12, 24, 48, 96]"
					:total="cardPager.total"
					layout="total, sizes, prev, pager, next, jumper"
					background
					@size-change="onCardSizeChange"
					@current-change="loadCardList"
				/>
			</el-card>
		</template>

		<!-- 表格视图 -->
		<el-card v-else class="full-table" shadow="never">
			<!-- vxe 的 height:'auto' 量的是父级 clientHeight，必须单独包一层 .grid-holder
			     只装剩余空间，否则行高会被压扁、列全部挤在一起 -->
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
					<template #toolbar_buttons>
						<el-button type="primary" icon="ele-Plus" @click="openCreate">新建流程</el-button>
					</template>
					<template #toolbar_tools> </template>
					<template #empty>
						<el-empty :image-size="200" />
					</template>
					<template #row_name="{ row }">
						<span :title="row.name">{{ row.name }}</span>
					</template>
					<template #row_code="{ row }">
						<span :title="row.code || ''">{{ row.code || '—' }}</span>
					</template>
					<template #row_category="{ row }">
						<span>{{ row.category || '—' }}</span>
					</template>
					<template #row_icon="{ row }">
						<SvgIcon :name="row.icon || 'ele-Menu'" :size="16" />
					</template>
					<template #row_formType="{ row }">
						<!-- 后台 FormTypeEnum 枚举，走系统字典组件统一展示 -->
						<g-sys-dict :model-value="row.formType" code="FormTypeEnum" render-as="tag" size="small" />
					</template>
					<!-- 绑定表单：表单名 + 表单版本 -->
					<template #row_formName="{ row }">
						<span class="wf-form-cell" :title="formChipTitle(row)">
							<span class="wf-form-name">{{ formNameText(row) }}</span>
							<em v-if="formVersionOf(row)" class="wf-form-ver">v{{ formVersionOf(row) }}</em>
						</span>
					</template>
					<template #row_version="{ row }">
						<el-tag v-if="row.latestVersion" size="small" effect="plain" class="wf-ver-tag">v{{ row.latestVersion }}</el-tag>
						<span v-else class="wf-muted">—</span>
					</template>
					<template #row_publish="{ row }">
						<!-- 后台 WorkflowPublishStatusEnum 枚举，走系统字典组件统一展示 -->
						<g-sys-dict :model-value="row.publishStatus" code="WorkflowPublishStatusEnum" render-as="tag" size="small" />
					</template>
					<template #row_status="{ row }">
						<el-switch v-model="row.status" :active-value="1" :inactive-value="0" @change="(val: number) => handleStatusChange(row, val)" />
					</template>
					<template #row_remark="{ row }">
						<span :title="row.remark || ''">{{ row.remark || '—' }}</span>
					</template>
					<template #row_createTime="{ row }">
						{{ formatTimeSec(row.createTime) }}
					</template>
					<template #row_publishedTime="{ row }">
						{{ formatTimeSec(row.latestPublishedTime) }}
					</template>
					<template #row_buttons="{ row }">
						<!-- <div class="wf-row-ops"> -->
							<el-button text type="primary" icon="ele-Edit" @click="openEdit(row)">编辑</el-button>
							<el-button text type="danger" icon="ele-Delete" @click="handleDelete(row)">删除</el-button>
							<el-button text type="primary" icon="ele-EditPen" @click="openDesign(row)">设计</el-button>
							<!-- <el-tooltip :content="row._publishTip || '发布流程'" placement="top"> -->
								<!-- <span class="wf-op-wrap"> -->
									<el-button text type="success" icon="ele-Position" :disabled="!row._canPublish" @click="openPublish(row)">发布</el-button>
								<!-- </span> -->
							<!-- </el-tooltip> -->
							<el-button text type="info" icon="ele-Document" @click="viewDefinitions(row)">发布记录</el-button>
						<!-- </div> -->
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<flow-drawer ref="flowDrawerRef" @success="onFlowSaved" />
		<flow-designer ref="flowDesignerRef" @saved="onFlowSaved" />
		<publish-dialog ref="publishDialogRef" @success="onFlowSaved" />

		<!-- 发布记录 -->
		<el-dialog v-model="definitionDialog.visible" width="960px" draggable :close-on-click-modal="false" class="def-dialog">
			<template #header>
				<div class="def-dialog-head">
					<div class="def-dialog-title">
						<el-icon size="16"><ele-Document /></el-icon>
						<span>发布记录</span>
						<el-tag v-if="definitionDialog.currentFlow" size="small" type="info" effect="plain" round>{{ definitionDialog.currentFlow.name }}</el-tag>
					</div>
					<div class="def-dialog-sub">
						共 {{ definitionDialog.list.length }} 个版本
						<template v-if="activeDefinition">
							<span class="def-dialog-dot">·</span>
							当前启用 v{{ activeDefinition.version }}
						</template>
					</div>
				</div>
			</template>

			<vxe-grid v-bind="definitionGridOptions" :data="definitionDialog.list" :loading="definitionDialog.loading">
				<template #def_version="{ row }">
					<div class="def-version">
						<span class="def-version-no">v{{ row.version }}</span>
						<el-tag v-if="row.isLatest" size="small" type="success" effect="plain">最新</el-tag>
					</div>
				</template>
				<template #def_isActive="{ row }">
					<span class="wf-status" :class="row.isActive ? 'is-on' : 'is-off'">
						<i class="wf-status-dot"></i>
						{{ row.isActive ? '启用' : '停用' }}
					</span>
				</template>
				<template #def_publishedTime="{ row }">
					{{ formatTime(row.publishedTime) }}
				</template>
				<template #def_buttons="{ row }">
					<el-button text type="primary" icon="ele-View" @click="viewFlowDefinition(row)">查看</el-button>
					<el-button text type="primary" icon="ele-Switch" @click="toggleDefinitionStatus(row)">{{ row.isActive ? '停用' : '启用' }}</el-button>
					<el-button text type="danger" icon="ele-Delete" :disabled="row.isActive" @click="deleteDefinition(row)">删除</el-button>
				</template>
				<template #empty>
					<el-empty description="该流程还没有发布过版本" :image-size="100" />
				</template>
			</vxe-grid>

			<template #footer>
				<el-button icon="ele-CircleClose" @click="definitionDialog.visible = false">关闭</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowProcessDesign">
import { computed, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridProps, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

import { getBusinessModuleConfig } from '../utils/businessModuleConfig';
import FlowDrawer from './components/FlowDrawer.vue';
import FlowDesigner from './components/FlowDesigner.vue';
import PublishDialog from './components/PublishDialog.vue';
import SvgIcon from '/@/components/svgIcon/index.vue';
import { getFlowPage, getFlowList, deleteFlow, setFlowStatus, getDefinitions, setDefinitionStatusV2, deleteDefinition as deleteDefinitionApi } from '/@/api/workflow';

interface WorkflowFlowItem {
	id: number;
	name: string;
	code?: string;
	category?: string;
	icon?: string;
	status: number;
	latestDefinitionId?: number;
	latestVersion?: number;
	isDesignChanged?: boolean;
	flowJson?: string;
	formId?: number;
	formCode?: string;
	formName?: string;
	formVersion?: number;
	isFormPublished?: boolean;
	formJson?: string;
	formType?: number;
	businessModule?: string;
	businessTitleTemplate?: string;
	latestPublishedTime?: string;
	/** 发布状态枚举（后台 WorkflowPublishStatusEnum：0 未发布 / 1 有更新未发布 / 2 已发布） */
	publishStatus?: number;
	createTime: string;
	remark?: string;
	/** 表格行预计算的发布前置条件（避免在 vxe 插槽里调用 setup 函数） */
	_canPublish?: boolean;
	_publishTip?: string;
}

const queryForm = ref();
const xGrid = ref<VxeGridInstance>();
const flowDrawerRef = ref<InstanceType<typeof FlowDrawer>>();
const flowDesignerRef = ref<InstanceType<typeof FlowDesigner>>();
const publishDialogRef = ref<InstanceType<typeof PublishDialog>>();

const state = reactive({
	queryParams: {
		keyword: '',
		category: undefined as string | undefined,
		status: undefined as number | undefined,
	},
	/** 视图模式：card 卡片（默认） / table 表格 */
	viewMode: 'card' as 'card' | 'table',
	/** 卡片视图数据 */
	cardList: [] as WorkflowFlowItem[],
});

/** 分类下拉选项：取自流程列表接口，保证筛选值与库中数据一致 */
const categoryOptions = ref<string[]>([]);

/** 卡片视图分页 */
const cardPager = reactive({ page: 1, pageSize: 12, total: 0 });

/** 顶部头图统计 */
const heroStats = reactive({ total: 0 });

// 表格参数配置
const options = useVxeTable<WorkflowFlowItem>(
	{
		id: 'workFlowProcessDesign',
		name: '流程设计',
		// 列结构与业务口径对齐：序号 / 名称 / 编码 / 分类 / 图标 / 类型 / 表单 / 版本 / 发布状态 / 状态 / 备注 / 创建 / 发布 / 操作
		// 列多必然横向滚动，序号与操作两列固定，中间列 showOverflow 悬浮看全文
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 54, fixed: 'left' },
			{ field: 'name', title: '流程名称', minWidth: 110, align: 'left', showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'code', title: '流程编码', minWidth: 138, align: 'left', showOverflow: 'tooltip', slots: { default: 'row_code' } },
			{ field: 'category', title: '分类', width: 80, showOverflow: 'tooltip', slots: { default: 'row_category' } },
			{ field: 'icon', title: '图标', width: 56, slots: { default: 'row_icon' } },
			{ field: 'formType', title: '流程类型', width: 112, showOverflow: 'tooltip', slots: { default: 'row_formType' } },
			{ field: 'formName', title: '绑定表单', minWidth: 130, align: 'left', showOverflow: 'tooltip', slots: { default: 'row_formName' } },
			{ field: 'latestVersion', title: '版本号', width: 64, slots: { default: 'row_version' } },
			{ field: 'publishStatus', title: '发布状态', width: 116, slots: { default: 'row_publish' } },
			{ field: 'status', title: '状态', width: 66, slots: { default: 'row_status' } },
			{ field: 'remark', title: '备注', minWidth: 92, align: 'left', showOverflow: 'tooltip', slots: { default: 'row_remark' } },
			{ field: 'createTime', title: '创建时间', width: 142, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'latestPublishedTime', title: '发布时间', width: 142, showOverflow: 'tooltip', slots: { default: 'row_publishedTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: false, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 列多（14 列）必然横向滚动：tableLayout fixed 让列宽严格按列定义分配，
		// 否则 auto 布局会把所有列等比压缩进容器，时间列被挤到只剩几个像素
		tableLayout: 'fixed',
		scrollX: { enabled: true, gt: 20 },
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 发布记录弹窗表格
const definitionGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'version', title: '版本号', width: 120, slots: { default: 'def_version' } },
		{ field: 'workflowCode', title: '工作流编码', minWidth: 210, showOverflow: 'tooltip' },
		{ field: 'isActive', title: '状态', width: 100, slots: { default: 'def_isActive' } },
		{ field: 'publishedTime', title: '发布时间', width: 150, slots: { default: 'def_publishedTime' } },
		{ field: 'buttons', title: '操作', width: 220, fixed: 'right', showOverflow: true, slots: { default: 'def_buttons' } },
	],
});

const definitionDialog = reactive({
	visible: false,
	loading: false,
	list: [] as Array<any>,
	currentFlow: null as WorkflowFlowItem | null,
});

/** 当前启用中的发布版本（用于弹窗头部摘要） */
const activeDefinition = computed(() => definitionDialog.list.find((item) => item.isActive));

/* ------------------------------ 查询 / 刷新 ------------------------------ */

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign({}, state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await getFlowPage(params);
	// vxe 通过 callSlot 渲染操作列，模板中的函数调用拿不到 setup 上下文，
	// 这里把发布前置条件预计算到行数据上，模板只读取字段
	const items = res.data?.result?.items;
	heroStats.total = Number(res.data?.result?.total ?? 0);
	if (Array.isArray(items)) {
		items.forEach((row: WorkflowFlowItem) => {
			row._canPublish = canPublish(row);
			row._publishTip = publishTip(row);
		});
	}
	return res;
};

// 查询操作（表格视图）
const handleQuery = async (reset = false) => {
	options.loading = true;
	try {
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	} finally {
		options.loading = false;
	}
};

// 卡片视图取数
const loadCardList = async () => {
	options.loading = true;
	try {
		const params = Object.assign({}, state.queryParams, { page: cardPager.page, pageSize: cardPager.pageSize });
		const res = await getFlowPage(params);
		state.cardList = res.data?.result?.items ?? [];
		cardPager.total = Number(res.data?.result?.total ?? 0);
		heroStats.total = cardPager.total;
	} finally {
		options.loading = false;
	}
};

// 刷新当前视图
const refresh = async () => {
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery();
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	await refresh();
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.keyword = '';
	state.queryParams.category = undefined;
	state.queryParams.status = undefined;
	cardPager.page = 1;
	await refresh();
};

// 分类选项（流程增删改后分类可能变化，保存成功时一并刷新）
const loadCategories = async () => {
	try {
		const res = await getFlowList();
		const list = (res.data?.result ?? []) as Array<{ category?: string }>;
		categoryOptions.value = Array.from(new Set(list.map((item) => item.category).filter((item): item is string => !!item)));
	} catch {
		// 分类选项属于增强信息，接口异常时保留已有选项，不打断主流程
	}
};

// 切换视图：卡片需要主动取数，表格由 vxe 挂载后 autoLoad 触发
const switchView = (mode: 'card' | 'table') => {
	if (state.viewMode === mode) return;
	state.viewMode = mode;
	if (mode === 'card') loadCardList();
};

// 卡片每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 流程保存成功（新建/编辑/设计/发布）：刷新列表与分类选项
const onFlowSaved = async () => {
	await refresh();
	loadCategories();
};

/* ------------------------------ 卡片辅助 ------------------------------ */

// 与发布弹窗的校验保持一致：空串和空图（{}）都视为尚未设计
const hasDesign = (row: WorkflowFlowItem) => {
	const json = row.flowJson?.replace(/\s/g, '');
	return !!json && json !== '{}';
};

/** 发布前置条件：流程已设计且处于启用状态（后端发布接口同样限制停用流程） */
const canPublish = (row: WorkflowFlowItem) => hasDesign(row) && Number(row.status) === 1;

const publishTip = (row: WorkflowFlowItem) => {
	if (!hasDesign(row)) return '请先在流程设计中保存流程';
	if (Number(row.status) !== 1) return '流程已停用，请先启用';
	return '发布新版本';
};

/** 卡片上的表单摘要 */
const formChipLabel = (row: WorkflowFlowItem) => {
	if (Number(row.formType) === 1) return getBusinessModuleLabel(row.businessModule) || row.businessModule || '业务模块表单';
	const name = row.formName || row.formCode;
	if (!name) return '未绑定表单';
	return row.formVersion ? `${name} · v${row.formVersion}` : name;
};

const formChipTitle = (row: WorkflowFlowItem) => {
	if (Number(row.formType) === 1) return `业务模块：${getBusinessModuleLabel(row.businessModule) || row.businessModule || '未指定'}`;
	if (!row.formId) return '未绑定表单';
	return `自定义表单：${row.formName || row.formCode}${row.formVersion ? ` · v${row.formVersion}` : ''}${row.isFormPublished ? ' · 已发布' : ' · 未发布'}`;
};

const formatTime = (val?: string) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm') : String(val);
};

/** 表格里的时间精确到秒（创建时间、发布时间两列） */
const formatTimeSec = (val?: string) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm:ss') : String(val);
};

/** 绑定表单的版本：业务模块表单没有表单版本，返回空 */
const formVersionOf = (row: WorkflowFlowItem) => (Number(row.formType) === 1 ? undefined : row.formVersion);

/** 表格「绑定表单」列的主行文本 */
const formNameText = (row: WorkflowFlowItem) => {
	if (Number(row.formType) === 1) return getBusinessModuleLabel(row.businessModule) || row.businessModule || '未指定';
	return row.formName || row.formCode || '未绑定表单';
};

/** 统一提取接口错误文案 */
const errMsg = (error: any, fallback: string) => {
	const data = error?.response?.data;
	if (typeof data === 'string' && data) return data;
	if (data?.errors) return data.errors;
	return error?.message || fallback;
};

/* ------------------------------ 业务操作 ------------------------------ */

const openCreate = () => flowDrawerRef.value?.openDrawer();
const openEdit = (row: WorkflowFlowItem) => flowDrawerRef.value?.openDrawer(row);
const openDesign = (row: WorkflowFlowItem) => flowDesignerRef.value?.openDialog(row);
const openPublish = (row: WorkflowFlowItem) => publishDialogRef.value?.openDialog(row);

// 卡片上的状态胶囊：点击即启用/停用
const toggleStatus = async (row: WorkflowFlowItem) => {
	const next = Number(row.status) === 1 ? 0 : 1;
	try {
		await setFlowStatus({ id: row.id, status: next, remark: row.remark });
		row.status = next;
		ElMessage.success(next === 1 ? '已启用' : '已停用');
	} catch (error: any) {
		ElMessage.error(errMsg(error, '状态更新失败'));
	}
};

// 表格里的状态开关
const handleStatusChange = async (row: WorkflowFlowItem, value: number) => {
	try {
		await setFlowStatus({ id: row.id, status: value, remark: row.remark });
		ElMessage.success(value === 1 ? '已启用' : '已停用');
	} catch (error: any) {
		row.status = value === 1 ? 0 : 1;
		ElMessage.error(errMsg(error, '状态更新失败'));
	}
};

// 删除流程：有发布版本时提示会一并删除，确认后走强制删除
const handleDelete = async (row: WorkflowFlowItem) => {
	let versionCount = 0;
	try {
		const res = await getDefinitions({ flowId: row.id, page: 1, pageSize: 100 });
		versionCount = res.data?.result?.items?.length ?? 0;
	} catch {
		versionCount = 0;
	}

	const tip =
		versionCount > 0
			? `流程「${row.name}」下还有 ${versionCount} 个发布版本，删除流程会一并删除全部版本且不可恢复，确认继续？`
			: `确认删除流程「${row.name}」吗？`;
	try {
		await ElMessageBox.confirm(tip, '删除流程', {
			type: 'warning',
			confirmButtonText: '确认删除',
			cancelButtonText: '取消',
			confirmButtonClass: 'el-button--danger',
		});
	} catch {
		return;
	}

	try {
		await deleteFlow({ id: row.id, forceDelete: true });
		ElMessage.success('删除成功');
		await onFlowSaved();
	} catch (error: any) {
		ElMessage.error(errMsg(error, '删除失败'));
	}
};

/* ------------------------------ 发布记录 ------------------------------ */

const viewDefinitions = async (row: WorkflowFlowItem) => {
	definitionDialog.visible = true;
	definitionDialog.loading = true;
	definitionDialog.currentFlow = row;
	definitionDialog.list = [];
	try {
		const res = await getDefinitions({ flowId: row.id, page: 1, pageSize: 100 });
		definitionDialog.list = res.data?.result?.items ?? [];
	} catch (error: any) {
		ElMessage.error(errMsg(error, '获取发布记录失败'));
	} finally {
		definitionDialog.loading = false;
	}
};

/** 只读查看指定发布版本的流程设计 */
const viewFlowDefinition = (row: any) => {
	if (!definitionDialog.currentFlow) return;
	if (!row.logicFlowJson || row.logicFlowJson.trim() === '') {
		ElMessage.warning('该版本没有可查看的流程设计');
		return;
	}
	// 以当前流程为基底，覆盖为该版本的流程设计，只读查看
	const flow = { ...definitionDialog.currentFlow, flowJson: row.logicFlowJson };
	flowDesignerRef.value?.openDialog(flow, {
		readonly: true,
		title: `查看流程设计 - ${flow.name}（v${row.version}）`,
	});
};

const toggleDefinitionStatus = async (row: any) => {
	const action = row.isActive ? '停用' : '启用';
	try {
		await ElMessageBox.confirm(`确认${action}版本 v${row.version} 吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	try {
		await setDefinitionStatusV2({ id: row.id, isActive: !row.isActive });
		row.isActive = !row.isActive;
		ElMessage.success(`${action}成功`);
	} catch (error: any) {
		ElMessage.error(errMsg(error, `${action}失败`));
	}
};

const deleteDefinition = async (row: any) => {
	if (row.isActive) {
		ElMessage.warning('请先停用该版本后再删除');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认删除版本 v${row.version} 吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	try {
		await deleteDefinitionApi({ id: row.id });
		ElMessage.success('删除成功');
		if (definitionDialog.currentFlow) await viewDefinitions(definitionDialog.currentFlow);
		await refresh();
	} catch (error: any) {
		ElMessage.error(errMsg(error, '删除失败'));
	}
};

/** 业务模块的显示名称 */
const getBusinessModuleLabel = (businessModule?: string): string => {
	if (!businessModule) return '';
	const config = getBusinessModuleConfig(businessModule);
	return config?.label || businessModule;
};

// 页面初始化：默认卡片视图 + 分类选项
loadCardList();
loadCategories();
</script>

<style lang="scss" scoped>
.workFlow-processDesign-container {
	position: relative;

	/* 表格模式下根节点是 flex 列，查询区不许被压缩 */
	> .query-toolbar {
		flex-shrink: 0;
		margin-top: 14px;
	}
}

.full-table {
	flex: 1;
}

/* ===================== 卡片视图 ===================== */
/* 卡片视图与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */

/* 头部状态标签承担"点击切换启停"，给个手型提示（全局 el-tag 默认不是可点的） */
.entity-card__head > .el-tag.is-toggle {
	cursor: pointer;
}

/** 版本号标签：等宽数字 + 浅灰底，表格与卡片统一 */
.wf-ver-tag {
	font-weight: 600;
	letter-spacing: 0.2px;
}

.wf-muted {
	color: var(--el-text-color-placeholder);
}

/* 禁用按钮的 tooltip 需要一层可悬停的包裹元素 */
.wf-op-wrap {
	display: inline-flex;
}

/* 发布记录弹窗里的启用/停用胶囊（表格插槽仍在用）。
 * 底色不用 color-mix：老内核（Chromium 102）会在计算值阶段整条作废、退回透明，
 * 这里直接按 Element 的浅色档位取色，两内核等价 */
.wf-status {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	flex-shrink: 0;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	font-weight: 600;
	line-height: 1.5;
	white-space: nowrap;
	border: 1px solid transparent;

	&.is-on {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
		border-color: var(--el-color-success-light-7);

		.wf-status-dot {
			background: var(--el-color-success);
		}
	}

	&.is-off {
		color: var(--el-color-info);
		background: var(--el-color-info-light-9);
		border-color: var(--el-color-info-light-7);

		.wf-status-dot {
			background: var(--el-color-info);
		}
	}
}

.wf-status-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
}

/* 操作列：文字按钮等距平铺（按钮尺寸与间距沿用表格默认） */
.wf-row-ops {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 6px;
	white-space: nowrap;

	/* 列被挤压时按钮不换行、不缩字 */
	:deep(.el-button) {
		flex-shrink: 0;
		white-space: nowrap;
	}
}

/* 绑定表单列：表单名（可省略）+ 表单版本（灰色小字） */
.wf-form-cell {
	display: inline-flex;
	align-items: center;
	max-width: 100%;
	min-width: 0;
}

.wf-form-name {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.wf-form-ver {
	flex-shrink: 0;
	margin-left: 4px;
	font-size: 11px;
	font-style: normal;
	color: var(--el-text-color-placeholder);
}

/* ===================== 发布记录弹窗 ===================== */
.def-dialog-head {
	display: flex;
	flex-direction: column;
	align-items: flex-start;
	gap: 2px;
}

.def-dialog-title {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 15px;
	font-weight: 600;
	/* 弹窗头部是主题色实心底，标题必须显式给白 */
	color: var(--el-color-white);

	.el-tag {
		font-weight: 400;
		color: var(--el-color-white);
		background: rgb(255 255 255 / 22%);
		border-color: rgb(255 255 255 / 38%);
	}
}

.def-dialog-sub {
	font-size: 12px;
	color: rgb(255 255 255 / 78%);
}

.def-dialog-dot {
	margin: 0 4px;
	color: rgb(255 255 255 / 55%);
}

.def-version {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}

.def-version-no {
	font-weight: 600;
	color: var(--el-text-color-primary);
}
</style>
