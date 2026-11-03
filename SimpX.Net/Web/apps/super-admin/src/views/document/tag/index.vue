<template>
	<div class="document-tag-container">
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
					<h2 class="page-hero__title">标签管理</h2>
					<p class="page-hero__desc">给文档打上横向标签，突破分类树的单一归属限制；标签带颜色，在列表与卡片上可直接扫读。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ totalCount }}</span>
							<span class="page-hero__stat-k">标签总数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="标签名称">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="标签名称" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'document/tag/page'" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'document/tag/add'"> 新增标签 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<!-- 新增按钮已移到查询区，这里留空插槽：vxe 在 toolbarConfig 开启时会检查它是否存在，缺了会告警 -->
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="暂无标签，新增后可在文档上横向归类" />
				</template>
				<template #row_name="{ row }">
					<div class="doc-name-cell">
						<span class="doc-name-cell__text">{{ row.name }}</span>
					</div>
				</template>
				<template #row_color="{ row }">
					<div class="doc-color-cell">
						<span class="doc-color-swatch" :style="{ backgroundColor: row.color || '#409EFF' }"></span>
						<span class="cell-sub">{{ row.color || '默认' }}</span>
					</div>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-Edit" text type="primary" v-auth="'document/tag/update'" @click="handleEdit(row)"> 编辑 </el-button>
					<el-button icon="ele-Delete" text type="danger" v-auth="'document/tag/delete'" @click="handleDelete(row)"> 删除 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditTagDialog ref="editTagDialogRef" :title="state.title" @handleQuery="handleQuery(false)" />
	</div>
</template>

<!-- 文档标签管理 -->
<script lang="ts" setup name="documentTag">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentTagApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import '/@/views/document/styles/document.scss';

import ModifyRecord from '/@/components/table/modifyRecord.vue';
import EditTagDialog from './component/editTagDialog.vue';

// 页面初始化
const xGrid = ref<VxeGridInstance>();
const editTagDialogRef = ref<InstanceType<typeof EditTagDialog>>();

const { key: localPageParamKey, param: localPageParam } = useGridPageParam('documentTag', {
	pageSize: 20 as number,
	defaultSort: { field: 'id', order: 'desc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined as string | undefined,
	},
	title: '',
	localPageParam,
});

// 标签总数：来自 vxe 代理分页器的 total，供头图统计展示
const totalCount = ref(0);

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentTag',
		name: '文档标签',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '标签名称', minWidth: 150, showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'color', title: '标签颜色', minWidth: 150, showOverflow: 'tooltip', slots: { default: 'row_color' } },
			{ field: 'useCount', title: '使用次数', width: 100, showOverflow: 'tooltip' },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// vxeGrid配置参数
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { import: false, export: true },
	}
);

// 表格查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	});
	return getAPI(DocumentTagApi).apiDocumentTagPagePost(params);
};

// 查询操作：包装一层，顺带把分页总数同步给头图统计
const baseQuery = useGridQuery(xGrid, options);
const handleQuery = async (reset = false) => {
	await baseQuery(reset);
	totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	await handleQuery(true);
};

// 打开新增页面
const handleAdd = () => {
	state.title = '新增标签';
	editTagDialogRef.value?.openDialog(undefined);
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑标签';
	editTagDialogRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定要删除标签"${row.name}"吗？`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(DocumentTagApi).apiDocumentTagDeletePost({ id: row.id });
			ElMessage.success('删除成功');
			await handleQuery(false);
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<any> = {
	// 代理查询完成后刷新总数（表格自带的翻页 / 排序也会走这里）
	async proxyQuery() {
		totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
	},
	// 双击行事件
	async cellDblclick({ row }) {
		await handleEdit(row);
	},
};
</script>
