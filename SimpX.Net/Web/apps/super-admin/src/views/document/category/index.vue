<template>
	<div class="document-category-container">
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
					<h2 class="page-hero__title">分类管理</h2>
					<p class="page-hero__desc">按业务维度建立文档分类树，支持无限层级；分类可自带颜色，在文档列表与卡片上以标签色呈现。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ totalCount }}</span>
							<span class="page-hero__stat-k">分类总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.categoryData.length }}</span>
							<span class="page-hero__stat-k">一级分类</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增/展开/折叠放这里，不随表格滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="分类名称">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="分类名称" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'document/category/tree'" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'document/category/add'"> 新增分类 </el-button>
				<el-button-group>
					<el-button icon="ele-Expand" @click="handleExpand"> 全部展开 </el-button>
					<el-button icon="ele-Fold" @click="handleFold"> 全部折叠 </el-button>
				</el-button-group>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<!-- 操作按钮已移到查询区，这里留空插槽：vxe 在 toolbarConfig 开启时会检查它是否存在，缺了会告警 -->
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="暂无分类，新增后可按业务维度组织文档" />
				</template>
				<template #row_name="{ row }">
					<div class="doc-name-cell">
						<SvgIcon v-if="row.icon" :name="row.icon" />
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
					<el-button icon="ele-Edit" text type="primary" v-auth="'document/category/update'" @click="handleEdit(row)"> 编辑 </el-button>
					<el-button icon="ele-Delete" text type="danger" v-auth="'document/category/delete'" @click="handleDelete(row)"> 删除 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditCategoryDialog ref="editCategoryRef" :title="state.title" :categoryData="state.categoryData" @handleQuery="handleQuery(false)" />
	</div>
</template>

<!-- 文档分类管理 -->
<script lang="ts" setup name="documentCategory">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentCategoryApi } from '/@/api-services/document/api';
import { reactive, ref, computed } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import '/@/views/document/styles/document.scss';
import EditCategoryDialog from './component/editCategoryDialog.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import SvgIcon from '/@/components/svgIcon/index.vue';

const xGrid = ref<VxeGridInstance>();
const editCategoryRef = ref();
const state = reactive({
	categoryData: [] as any[],
	queryParams: {
		name: undefined as string | undefined,
	},
	title: '',
});

// 分类总数：树形数据是嵌套结构，递归累加才能反映真实规模
const totalCount = computed(() => {
	const count = (list: any[]): number => list.reduce((sum, item) => sum + 1 + (item.children?.length ? count(item.children) : 0), 0);
	return count(state.categoryData);
});

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentCategory',
		name: '文档分类',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 80, fixed: 'left' },
			{ field: 'name', title: '分类名称', minWidth: 180, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center', slots: { default: 'row_name' } },
			{ field: 'code', title: '分类编码', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'color', title: '分类颜色', minWidth: 150, showOverflow: 'tooltip', slots: { default: 'row_color' } },
			{ field: 'sort', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'description', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		stripe: false,
		// 多选配置
		checkboxConfig: { range: false },
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
		// 树形配置
		treeConfig: { expandAll: true, reserve: true },
		// 行配置信息
		rowConfig: {
			keyField: 'id',
		},
	}
);

// 查询api
const handleQueryApi = async () => {
	const params = Object.assign(state.queryParams);
	return getAPI(DocumentCategoryApi).apiDocumentCategoryTreeGet(params.name);
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	state.categoryData = xGrid.value?.getTableData().tableData ?? [];
	options.loading = false;
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	// 调用vxe-grid的commitProxy(reload)方法，触发表格重新加载数据
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '新增分类';
	editCategoryRef.value?.openDialog({ parentId: 0, sort: 100 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑分类';
	editCategoryRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定要删除分类"${row.name}"吗？`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(DocumentCategoryApi).apiDocumentCategoryDeletePost({ id: row.id });
			ElMessage.success('删除成功');
			await handleQuery(false);
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<any> = {
	// 只对 proxy-config.ajax.query 配置时有效，当手动点击查询时会触发该事件
	async proxyQuery() {
		state.categoryData = xGrid.value?.getTableData().tableData ?? [];
	},
	// 双击行事件
	async cellDblclick({ row }) {
		await handleEdit(row);
	},
};

// 全部展开
const handleExpand = () => {
	xGrid.value?.setAllTreeExpand(true);
};

// 全部折叠
const handleFold = () => {
	xGrid.value?.clearTreeExpand();
};
</script>
