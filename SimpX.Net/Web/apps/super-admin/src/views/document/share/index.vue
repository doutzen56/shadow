<template>
	<div class="document-share-manage-container">
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
					<h2 class="page-hero__title">分享管理</h2>
					<p class="page-hero__desc">集中查看我发出的分享链接：可复制链接、设置访问密码与有效期，也可随时取消让链接失效。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ totalCount }}</span>
							<span class="page-hero__stat-k">分享总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': enabledCount === 0 }">{{ enabledCount }}</span>
							<span class="page-hero__stat-k">生效中</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行 -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="分享码">
					<el-input v-model="state.queryParams.shareCode" class="query-input" placeholder="请输入分享码" clearable @keyup.enter="handleQuery(true)" />
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
			<!-- 分享列表 -->
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="160" description="还没有分享记录，在文档列表里点「分享」即可生成链接" />
				</template>
				<template #row_documentName="{ row }">
					<div class="doc-name-cell">
						<span class="doc-name-cell__icon"><el-icon><component :is="documentIconByName(row.documentName)" /></el-icon></span>
						<span class="doc-name-cell__text">{{ row.documentName }}</span>
					</div>
				</template>
				<template #row_categoryName="{ row }">
					<span v-if="row.categoryName">{{ row.categoryName }}</span>
					<span v-else class="doc-muted">未分类</span>
				</template>
				<template #row_shareUrl="{ row }">
					<el-input v-model="row.shareUrl" readonly size="small">
						<template #append>
							<el-button @click="handleCopyLink(row.shareUrl)">复制</el-button>
						</template>
					</el-input>
				</template>
				<template #row_hasPassword="{ row }">
					<el-tag v-if="row.hasPassword" type="warning" size="small">有密码</el-tag>
					<el-tag v-else type="success" size="small">无密码</el-tag>
				</template>
				<template #row_expireTime="{ row }">
					<div class="doc-time-cell">
						<span class="doc-time-cell__main">{{ row.expireTime ? splitDateTime(row.expireTime).date : '永久有效' }}</span>
						<span v-if="row.expireTime" class="cell-sub">{{ splitDateTime(row.expireTime).time }}</span>
					</div>
				</template>
				<template #row_sharePermission="{ row }">
					<el-tag v-if="row.sharePermission === 1" type="info" size="small">仅查看</el-tag>
					<el-tag v-else-if="row.sharePermission === 2" type="success" size="small">可下载</el-tag>
				</template>
				<template #row_isEnabled="{ row }">
					<el-tag v-if="row.isEnabled" type="success" size="small">生效中</el-tag>
					<el-tag v-else type="danger" size="small">已取消</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button v-if="row.isEnabled" link type="warning" icon="ele-RefreshLeft" @click="handleCancelShare(row)">取消</el-button>
					<el-button v-else link type="info" icon="ele-RefreshLeft" disabled>已取消</el-button>
					<el-button link type="danger" icon="ele-CircleCloseFilled" @click="handleDeleteShare(row)">删除</el-button>
				</template>
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentShareApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { resolveShareLink } from '/@/utils/documentShareUrl';
import '/@/views/document/styles/document.scss';
import { documentIconByName, splitDateTime } from '../utils/documentMeta';

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		shareCode: '',
	},
});

// 头图统计：分享总数来自分页器，生效中数量从当前页数据里数
const totalCount = ref(0);
const enabledCount = ref(0);
const refreshStats = () => {
	totalCount.value = (xGrid.value?.getProxyInfo() as any)?.pager?.total ?? 0;
	enabledCount.value = (xGrid.value?.getTableData().tableData ?? []).filter((row: any) => row.isEnabled).length;
};

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentShare',
		name: '我的分享',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'documentName', title: '文件名称', minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_documentName' } },
			{ field: 'documentNo', title: '文档编号', width: 160, showOverflow: 'tooltip' },
			{ field: 'categoryName', title: '分类', width: 120, showOverflow: 'tooltip', slots: { default: 'row_categoryName' } },
			{ field: 'shareUserName', title: '分享人', width: 100, showOverflow: 'tooltip' },
			{ field: 'shareCode', title: '分享码', width: 150, showOverflow: 'tooltip' },
			{ field: 'shareUrl', title: '分享链接', minWidth: 250, slots: { default: 'row_shareUrl' } },
			{ field: 'hasPassword', title: '密码', width: 100, slots: { default: 'row_hasPassword' } },
			{ field: 'accessCount', title: '访问次数', width: 100 },
			{ field: 'expireTime', title: '过期时间', width: 120, showOverflow: 'tooltip', slots: { default: 'row_expireTime' } },
			{ field: 'sharePermission', title: '权限', width: 100, slots: { default: 'row_sharePermission' } },
			{ field: 'isEnabled', title: '状态', width: 100, slots: { default: 'row_isEnabled' } },
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

// 表格事件：代理查询完成后刷新头图统计
const gridEvents: VxeGridListeners<any> = {
	async proxyQuery() {
		refreshStats();
	},
};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	try {
		const res = await getAPI(DocumentShareApi).apiDocumentShareMyPost({
			page: page.currentPage,
			pageSize: page.pageSize,
		});

		// 处理响应格式：可能是 res.data 或 res.data.result
		const responseData = res.data?.result || res.data;
		let shares = Array.isArray(responseData?.items) ? responseData.items : Array.isArray(responseData) ? responseData : [];

		shares.forEach((share: any) => {
			share.shareUrl = resolveShareLink(share);
		});

		// 如果有分享码筛选条件，进行过滤
		if (state.queryParams.shareCode) {
			shares = shares.filter((s: any) => s.shareCode?.toLowerCase().includes(state.queryParams.shareCode.toLowerCase()));
		}

		return { data: { result: { items: shares, total: responseData?.total || shares.length } } };
	} catch (error: any) {
		console.error('查询分享列表失败:', error);
		ElMessage.error(error?.response?.data?.message || '查询分享列表失败');
		return { data: { result: { items: [], total: 0 } } };
	}
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
	refreshStats();
};

const resetQuery = () => {
	state.queryParams.shareCode = '';
	handleQuery(true);
};

const handleCopyLink = (url: string) => {
	if (!url) {
		ElMessage.warning('分享链接为空');
		return;
	}

	navigator.clipboard
		.writeText(url)
		.then(() => {
			ElMessage.success('链接已复制到剪贴板');
		})
		.catch(() => {
			// 降级方案
			const textarea = document.createElement('textarea');
			textarea.value = url;
			document.body.appendChild(textarea);
			textarea.select();
			document.execCommand('copy');
			document.body.removeChild(textarea);
			ElMessage.success('链接已复制到剪贴板');
		});
};

const handleCancelShare = async (row: any) => {
	try {
		await ElMessageBox.confirm('确定要取消该分享链接吗？取消后链接将失效，但记录仍会保留。', '提示', {
			type: 'warning',
		});

		await getAPI(DocumentShareApi).apiDocumentShareCancelPost({ id: row.id });
		ElMessage.success('已取消分享');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			console.error('取消分享失败:', error);
			ElMessage.error(error?.response?.data?.message || '取消分享失败');
		}
	}
};

const handleDeleteShare = async (row: any) => {
	try {
		await ElMessageBox.confirm('确定要删除该分享记录吗？删除后无法恢复，且分享链接将永久失效。', '删除提示', {
			type: 'warning',
			confirmButtonText: '确定删除',
			cancelButtonText: '取消',
		});

		await getAPI(DocumentShareApi).apiDocumentShareDeletePost({ id: row.id });
		ElMessage.success('删除成功');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			console.error('删除分享失败:', error);
			ElMessage.error(error?.response?.data?.message || '删除分享失败');
		}
	}
};
</script>
