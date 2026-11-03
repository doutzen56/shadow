<template>
	<div class="pt-page">
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
					<h2 class="page-hero__title">打印模板</h2>
					<p class="page-hero__desc">统一维护业务单据的打印样式，支持浏览器打印与客户端打印两种输出方式。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="启用 / 禁用数量按当前已加载数据实时统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statTotal }}</span>
							<span class="page-hero__stat-k">模板总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ statEnabled }}</span>
							<span class="page-hero__stat-k">已启用</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-off">{{ statDisabled }}</span>
							<span class="page-hero__stat-k">已禁用</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 搜索与操作：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true" label-width="auto" class="query-form">
				<el-form-item label="模板名称" prop="name">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="请输入模板名称" clearable @keyup.enter.native="handleSearch">
						<template #prefix
							><el-icon class="pt-input-ico"><ele-Search /></el-icon
						></template>
					</el-input>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button icon="ele-Search" type="primary" @click="handleSearch" v-auth="'sysPrint/page'" :loading="options.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading">重置</el-button>
				</el-button-group>
				<el-button icon="ele-Plus" type="primary" class="pt-add-btn" @click="handleAdd" v-auth="'sysPrint/add'">新增模板</el-button>
				<!-- 视图切换：用 element 原生按钮组，尺寸与查询/重置一致 -->
				<el-button-group>
					<el-button type="primary" :plain="state.viewMode !== 'card'" icon="ele-Grid" @click="switchView('card')">卡片</el-button>
					<el-button type="primary" :plain="state.viewMode !== 'table'" icon="ele-List" @click="switchView('table')">表格</el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 卡片视图（默认）：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<el-card v-if="state.viewMode === 'card'" class="full-table" shadow="hover">
			<div class="card-grid" v-loading="options.loading">
				<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的打印模板" :image-size="140" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable"
							:class="'is-primary'"
							@dblclick="onCardDblclick(row)"
						>
							<i class="entity-card__watermark"><el-icon><ele-Tickets /></el-icon></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Printer /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
									<div class="entity-card__sub">排序 {{ row.orderNo ?? '—' }}</div>
								</div>
								<el-tag size="small" effect="light" :type="Number(row.status) === 1 ? 'success' : 'info'">
									{{ Number(row.status) === 1 ? '启用' : '禁用' }}
								</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc" :title="row.remark">{{ row.remark || '暂无备注' }}</div>

								<div class="tag-group">
									<el-tag size="small" effect="light" :type="row.printType === 1 ? 'primary' : 'warning'">
										<el-icon><ele-Monitor v-if="row.printType === 1" /><ele-Cpu v-else /></el-icon>
										{{ row.printType === 1 ? '浏览器打印' : '客户端打印' }}
									</el-tag>
									<el-tag size="small" effect="light" type="info">
										<el-icon><ele-Document /></el-icon>
										模板 {{ templateSize(row) }}
									</el-tag>
									<el-tag v-if="row.clientServiceAddress" size="small" effect="plain" type="info" :title="row.clientServiceAddress">
										<el-icon><ele-Link /></el-icon>
										{{ row.clientServiceAddress }}
									</el-tag>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ row.updateUserName || row.createUserName || '—' }} · {{ formatTime(row.updateTime || row.createTime) }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysPrint/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="复制" placement="top">
										<el-button icon="ele-DocumentCopy" text type="primary" v-auth="'sysPrint/add'" @click.stop="handleCopy(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysPrint/delete'" @click.stop="handleDelete(row)" />
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

		<!-- 表格视图 -->
		<el-card v-else class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<!-- <el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysPrint/add'"> 新增 </el-button> -->
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="success">启用</el-tag>
					<el-tag v-else type="danger">禁用</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="编辑" placement="top">
						<el-button icon="ele-Edit" text type="primary" v-auth="'sysPrint/update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip content="复制" placement="top">
						<el-button icon="ele-DocumentCopy" text type="primary" v-auth="'sysPrint/add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip content="删除" placement="top">
						<el-button icon="ele-Delete" text type="danger" v-auth="'sysPrint/delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<EditPrint ref="editPrintRef" :title="state.title" @handleQuery="refresh" />
	</div>
</template>

<!-- 打印模版 -->
<script lang="ts" setup name="sysPrint">
import { computed, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditPrint from '/@/views/system/print/component/editPrint.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysPrintApi } from '/@/api-services/system/api';
import { SysPrint, PagePrintInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editPrintRef = ref<InstanceType<typeof EditPrint>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysPrint', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
	/** 视图模式：card 卡片（默认） / table 表格 */
	viewMode: 'card' as 'card' | 'table',
	/** 卡片视图数据 */
	cardList: [] as SysPrint[],
	/** 表格视图数据（用于顶部统计） */
	tableRows: [] as SysPrint[],
	tableTotal: 0,
});

// 卡片视图分页
const cardPager = reactive({ page: 1, pageSize: 24, total: 0 });

// 表格参数配置
const options = useVxeTable<SysPrint>(
	{
		id: 'sysPrint',
		name: '打印信息',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '名称', minWidth: 250, showOverflow: 'tooltip' },
			// { field: 'template', title: '模板', minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'createTime', title: '修改时间', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'remark', title: '备注', minWidth: 250, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 100, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置：隐藏 vxe 自带工具栏（新增/导出等操作已统一到上方查询区）
		toolbarConfig: { enabled: false },
	}
);

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PagePrintInput;
	const res = await getAPI(SysPrintApi).apiSysPrintPagePost(params);
	state.tableRows = (res.data.result?.items ?? []) as SysPrint[];
	state.tableTotal = Number(res.data.result?.total ?? 0);
	return res;
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 卡片视图取数
const loadCardList = async () => {
	options.loading = true;
	try {
		const params = {
			...state.queryParams,
			page: cardPager.page,
			pageSize: cardPager.pageSize,
			field: localPageParam.defaultSort.field,
			order: localPageParam.defaultSort.order,
			descStr: 'desc',
		} as PagePrintInput;
		const res = await getAPI(SysPrintApi).apiSysPrintPagePost(params);
		state.cardList = (res.data.result?.items ?? []) as SysPrint[];
		cardPager.total = Number(res.data.result?.total ?? 0);
	} finally {
		options.loading = false;
	}
};

// 刷新当前视图
const refresh = async () => {
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery();
};

// 切换视图：卡片需要主动取数，表格由 vxe 挂载后 autoLoad 触发
const switchView = (mode: 'card' | 'table') => {
	if (state.viewMode === mode) return;
	state.viewMode = mode;
	if (mode === 'card') loadCardList();
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery(true);
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery(true);
};

// 卡片每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加打印模板';
	editPrintRef.value?.openDialog({});
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑打印模板';
	editPrintRef.value?.openDialog(row);
};

// 卡片双击进入编辑
const onCardDblclick = (row: any) => {
	if (auth('sysPrint/update')) handleEdit(row);
};

// 复制
const handleCopy = async (row: any) => {
	await getAPI(SysPrintApi).apiSysPrintCopyPost({ id: row.id });
	await refresh();
	ElMessage.success('复制成功');
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除打印模板：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysPrintApi).apiSysPrintDeletePost({ id: row.id });
			await refresh();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<SysPrint> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysPrint/update')) await handleEdit(row);
	},
};

/* ------------------------------ 展示辅助 ------------------------------ */
const statRows = computed<SysPrint[]>(() => (state.viewMode === 'card' ? state.cardList : state.tableRows));
const statTotal = computed(() => (state.viewMode === 'card' ? cardPager.total : state.tableTotal));
const statEnabled = computed(() => statRows.value.filter((r) => Number(r.status) === 1).length);
const statDisabled = computed(() => statRows.value.length - statEnabled.value);

const templateSize = (row: SysPrint) => {
	const len = row.template?.length ?? 0;
	return len >= 1024 ? `${(len / 1024).toFixed(1)} KB` : `${len} 字符`;
};

const formatTime = (val: any) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm') : String(val);
};

// 页面初始化：默认卡片视图
loadCardList();
</script>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.pt-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		flex-shrink: 0;
		margin-top: 14px;
	}
}

/* 搜索框前置放大镜：弱化到占位色，不与输入文字抢对比度 */
.pt-input-ico {
	color: var(--el-text-color-placeholder);
}

/* 新增模板：主操作，加一道扫光让它在工具条里先被看见 */
.pt-add-btn {
	position: relative;
	overflow: hidden;
	font-weight: 600;

	&::after {
		content: '';
		position: absolute;
		top: 0;
		bottom: 0;
		width: 40px;
		left: -60px;
		background: linear-gradient(100deg, transparent, rgba(255, 255, 255, 0.45), transparent);
		transform: skewX(-20deg);
		transition: left 0.6s ease;
	}

	&:hover::after {
		left: 120%;
	}
}
</style>
