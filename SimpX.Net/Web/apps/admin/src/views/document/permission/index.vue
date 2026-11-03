<template>
	<div class="document-permission-container">
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
					<h2 class="page-hero__title">权限管理</h2>
					<p class="page-hero__desc">按文档单独授予查看 / 下载 / 编辑 / 删除 / 分享权限，粒度可到用户、角色与组织，未授权者不可见。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ totalCount }}</span>
							<span class="page-hero__stat-k">可授权文档</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="文档名称">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="文档名称或编号" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 文档列表 -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="没有可授权的文档，先在文档列表上传吧" />
				</template>
				<template #row_name="{ row }">
					<div class="doc-name-cell">
						<span class="doc-name-cell__icon"
							><el-icon><component :is="documentIconByName(row.name)" /></el-icon
						></span>
						<span class="doc-name-cell__text">{{ row.name }}</span>
					</div>
				</template>
				<template #row_categoryName="{ row }">
					<span v-if="row.categoryName">{{ row.categoryName }}</span>
					<span v-else class="doc-muted">未分类</span>
				</template>
				<template #row_createTime="{ row }">
					<div class="doc-time-cell">
						<span>{{ row.createTime }}</span>
					</div>
				</template>
				<template #row_buttons="{ row }">
					<el-button link type="primary" icon="ele-Unlock" @click="handleSetPermission(row)">设置权限</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<!-- 权限设置对话框 -->
		<DocumentPermissionDialog ref="permissionDialogRef" @success="handleQuery(false)" />
	</div>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentPermissionApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import '/@/views/document/styles/document.scss';
import { documentIconByName, splitDateTime } from '../utils/documentMeta';
import DocumentPermissionDialog from '../components/DocumentPermissionDialog.vue';

const xGrid = ref<VxeGridInstance>();
const permissionDialogRef = ref();

const state = reactive({
	queryParams: {
		keyword: '',
	},
});

// 可授权文档总数，供头图统计展示
const totalCount = ref(0);

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentPermission',
		name: '文档权限',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '文档名称', minWidth: 200, showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'documentNo', title: '文档编号', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'categoryName', title: '分类', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_categoryName' } },
			{ field: 'createUserName', title: '创建人', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '创建时间', minWidth: 120, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 表格事件：代理查询完成后刷新总数
const gridEvents: VxeGridListeners<any> = {
	async proxyQuery() {
		totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
	},
};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	try {
		const res = await getAPI(DocumentPermissionApi).apiDocumentPermissionPagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			keyword: state.queryParams.keyword,
		});

		// 处理响应格式：可能是 res.data 或 res.data.result
		const responseData = res.data?.result || res.data;
		return { data: { result: { items: responseData?.items || [], total: responseData?.total || 0 } } };
	} catch (error: any) {
		console.error('查询文档列表失败:', error);
		ElMessage.error(error?.response?.data?.message || '查询文档列表失败');
		return { data: { result: { items: [], total: 0 } } };
	}
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
	totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
};

const resetQuery = () => {
	state.queryParams.keyword = '';
	handleQuery(true);
};

const handleSetPermission = (row: any) => {
	permissionDialogRef.value?.open(row.id);
};
</script>
