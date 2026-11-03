<template>
	<div class="recycle-container">
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
					<h2 class="page-hero__title">回收站</h2>
					<p class="page-hero__desc">删除的文档先进入回收站，可随时一键恢复；彻底删除后不可找回，请谨慎操作。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': totalCount === 0 }">{{ totalCount }}</span>
							<span class="page-hero__stat-k">待处理文档</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': selectedDocuments.length === 0 }">{{ selectedDocuments.length }}</span>
							<span class="page-hero__stat-k">已选中</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：关键字 + 批量操作同一行（操作按钮不进表格卡片，避免随内容滚动） -->
		<div class="query-toolbar">
			<el-form :model="state" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.searchKeyword" class="query-input" placeholder="文档名称" clearable @keyup.enter="handleSearch" @clear="handleSearch" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleSearch" :loading="options.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="success" icon="ele-RefreshLeft" :disabled="selectedDocuments.length === 0" @click="handleBatchRestore" v-auth="'document/recycle/restore'"> 批量恢复 </el-button>
				<el-button type="danger" icon="ele-Delete" :disabled="selectedDocuments.length === 0" @click="handleBatchDeletePermanently" v-auth="'document/recycle/delete'">
					批量彻底删除
				</el-button>
			</div>
		</div>

		<!-- 规则提示：放在表格卡片外，避免随表格滚动 -->
		<el-alert type="warning" :closable="false" class="recycle-hint">
			<template #title>
				<span>回收站中的文档已被删除，您可以选择恢复或彻底删除。彻底删除后将无法恢复。</span>
			</template>
		</el-alert>

		<!-- 内容区域 -->
		<el-card shadow="hover" class="full-table">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="回收站是空的，删除的文档会先放到这里" />
				</template>
				<template #row_name="{ row }">
					<div class="doc-name-cell">
						<span class="doc-name-cell__icon"><el-icon><component :is="documentIconByName(row.name)" /></el-icon></span>
						<span class="doc-name-cell__text">{{ row.name }}</span>
					</div>
				</template>
				<template #row_deleteTime="{ row }">
					<div class="doc-time-cell">
						<span class="doc-time-cell__main">{{ splitDateTime(row.deleteTime || row.createTime).date }}</span>
						<span class="cell-sub">{{ splitDateTime(row.deleteTime || row.createTime).time }}</span>
					</div>
				</template>
				<template #row_buttons="{ row }">
					<el-button link type="primary" icon="ele-View" @click="handlePreview(row)">预览</el-button>
					<el-button link type="primary" icon="ele-Download" @click="handleDownload(row)">下载</el-button>
					<el-button link type="success" icon="ele-Refresh" @click="handleRestore(row)">恢复</el-button>
					<el-button link type="danger" icon="ele-CircleCloseFilled" @click="handleDeletePermanently(row)">彻底删除</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<!-- 预览对话框 -->
		<DocumentPreviewDialog ref="previewDialogRef" />
	</div>
</template>

<script lang="ts" setup name="documentRecycle">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import '/@/views/document/styles/document.scss';
import DocumentPreviewDialog from '../components/DocumentPreviewDialog.vue';
import { downloadStreamFile } from '/@/utils/download';
import { documentIconByName, splitDateTime } from '../utils/documentMeta';

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	searchKeyword: '',
});

const selectedDocuments = ref<number[]>([]);
const previewDialogRef = ref();
// 回收站待处理总数，供头图统计展示
const totalCount = ref(0);

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentRecycle',
		name: '回收站',
		columns: [
			{ type: 'checkbox', width: 50, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60 },
			{ field: 'name', title: '名称', minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'documentNo', title: '编号', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'sizeInfo', title: '大小', width: 100, showOverflow: 'tooltip' },
			{ field: 'categoryName', title: '分类', width: 100, showOverflow: 'tooltip' },
			{ field: 'createUserName', title: '创建人', width: 100, showOverflow: 'tooltip' },
			{ field: 'deleteTime', title: '删除时间', minWidth: 120, showOverflow: 'tooltip', slots: { default: 'row_deleteTime' } },
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

// 表格事件：单选/全选变化后同步选中集合
const syncCheckboxRecords = () => {
	selectedDocuments.value = (xGrid.value?.getCheckboxRecords() ?? []).map((item: any) => item.id);
};
const gridEvents: VxeGridListeners<any> = {
	checkboxChange: syncCheckboxRecords,
	checkboxAll: syncCheckboxRecords,
	// 代理查询完成后刷新总数（表格自带翻页也会走这里）
	async proxyQuery() {
		totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
	},
};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	try {
		const res = await getAPI(DocumentApi).apiDocumentRecyclePagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			keyword: state.searchKeyword || undefined,
		});
		const result = res.data?.result;
		return { data: { result: { items: result?.items || [], total: result?.total || 0 } } };
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '查询失败');
		return { data: { result: { items: [], total: 0 } } };
	}
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
	totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
	// 数据刷新后清空勾选
	await xGrid.value?.clearCheckboxRow();
	selectedDocuments.value = [];
};

// 搜索
const handleSearch = () => {
	handleQuery(true);
};

// 重置
const resetQuery = () => {
	state.searchKeyword = '';
	handleQuery(true);
};

// 预览
const handlePreview = (document: any) => {
	previewDialogRef.value?.open({
		...document,
		skipPreviewLookup: true,
	});
};

// 下载（回收站中的文档也可以下载）
const handleDownload = async (document: any) => {
	try {
		const res = await getAPI(DocumentApi).apiDocumentDownloadPost({ id: document.id }, { responseType: 'blob' });
		if (await downloadStreamFile(res, document.name)) ElMessage.success('下载成功');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '下载失败');
	}
};

// 恢复文档
const handleRestore = async (document: any) => {
	try {
		await ElMessageBox.confirm(`确定要恢复文档"${document.name}"吗？`, '提示', {
			type: 'warning',
		});
		await getAPI(DocumentApi).apiDocumentRecycleRestorePost({ id: document.id });
		ElMessage.success('恢复成功');
		handleQuery();
		// 清除选中项
		selectedDocuments.value = selectedDocuments.value.filter((id) => id !== document.id);
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '恢复失败');
		}
	}
};

// 彻底删除文档
const handleDeletePermanently = async (document: any) => {
	try {
		await ElMessageBox.confirm(`确定要彻底删除文档"${document.name}"吗？此操作不可恢复！`, '危险操作', {
			type: 'error',
			confirmButtonText: '彻底删除',
			cancelButtonText: '取消',
		});
		await getAPI(DocumentApi).apiDocumentRecycleDeletePost({ id: document.id });
		ElMessage.success('彻底删除成功');
		handleQuery();
		// 清除选中项
		selectedDocuments.value = selectedDocuments.value.filter((id) => id !== document.id);
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '彻底删除失败');
		}
	}
};

// 批量恢复
const handleBatchRestore = async () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要恢复的文档');
		return;
	}
	try {
		await ElMessageBox.confirm(`确定要恢复选中的 ${selectedDocuments.value.length} 个文档吗？`, '提示', {
			type: 'warning',
		});
		const restorePromises = selectedDocuments.value.map((id) => getAPI(DocumentApi).apiDocumentRecycleRestorePost({ id }));
		await Promise.all(restorePromises);
		ElMessage.success('批量恢复成功');
		handleQuery();
		selectedDocuments.value = [];
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '批量恢复失败');
		}
	}
};

// 批量彻底删除
const handleBatchDeletePermanently = async () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要彻底删除的文档');
		return;
	}
	try {
		await ElMessageBox.confirm(`确定要彻底删除选中的 ${selectedDocuments.value.length} 个文档吗？此操作不可恢复！`, '危险操作', {
			type: 'error',
			confirmButtonText: '彻底删除',
			cancelButtonText: '取消',
		});
		const deletePromises = selectedDocuments.value.map((id) => getAPI(DocumentApi).apiDocumentRecycleDeletePost({ id }));
		await Promise.all(deletePromises);
		ElMessage.success('批量彻底删除成功');
		handleQuery();
		selectedDocuments.value = [];
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '批量彻底删除失败');
		}
	}
};
</script>

<style lang="scss" scoped>
.recycle-container {
	/* 规则提示条（el-alert）是根容器的直接子元素，上下间距由根容器 gap:12px 接管（见 document.scss） */
	.recycle-hint {
		flex-shrink: 0;
	}
}
</style>
