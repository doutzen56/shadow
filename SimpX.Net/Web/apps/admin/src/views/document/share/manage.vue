<template>
	<div class="document-share-manage-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="分享码" prop="shareCode">
							<el-input v-model="state.queryParams.shareCode" placeholder="请输入分享码" clearable @keyup.enter.native="handleQuery(true)" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="align-self: stretch; height: auto; margin: 0 10px 5px 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">

			<!-- 分享列表 -->
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_categoryName="{ row }">
					<span>{{ row.categoryName || '未分类' }}</span>
				</template>
				<template #row_shareUrl="{ row }">
					<el-input v-model="row.shareUrl" readonly>
						<template #append>
							<el-button @click="handleCopyLink(row.shareUrl)">复制</el-button>
						</template>
					</el-input>
				</template>
				<template #row_hasPassword="{ row }">
					<el-tag v-if="row.hasPassword" type="warning">有密码</el-tag>
					<el-tag v-else type="success">无密码</el-tag>
				</template>
				<template #row_expireTime="{ row }">
					<span v-if="row.expireTime">{{ formatDateTime(row.expireTime) }}</span>
					<span v-else>永久有效</span>
				</template>
				<template #row_sharePermission="{ row }">
					<el-tag v-if="row.sharePermission === 1" type="info">仅查看</el-tag>
					<el-tag v-else-if="row.sharePermission === 2" type="success">可下载</el-tag>
				</template>
				<template #row_isEnabled="{ row }">
					<el-tag v-if="row.isEnabled" type="success">启用</el-tag>
					<el-tag v-else type="danger">已取消</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button v-if="row.isEnabled" link type="warning" @click="handleCancelShare(row)">取消</el-button>
					<el-button v-else link type="info" disabled>已取消</el-button>
					<el-button link type="danger" @click="handleDeleteShare(row)">删除</el-button>
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
import { formatDateTime } from '/@/utils/format';
import { resolveShareLink } from '/@/utils/documentShareUrl';


const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		shareCode: '',
	},
});

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: 'documentShareManage',
		name: '我的分享管理',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'documentName', title: '文件名称', minWidth: 180, showOverflow: 'tooltip' },
			{ field: 'documentNo', title: '文档编号', width: 160, showOverflow: 'tooltip' },
			{ field: 'categoryName', title: '分类', width: 120, showOverflow: 'tooltip', slots: { default: 'row_categoryName' } },
			{ field: 'shareUserName', title: '分享人', width: 100, showOverflow: 'tooltip' },
			{ field: 'shareCode', title: '分享码', width: 150, showOverflow: 'tooltip' },
			{ field: 'shareUrl', title: '分享链接', minWidth: 250, slots: { default: 'row_shareUrl' } },
			{ field: 'hasPassword', title: '是否有密码', width: 120, slots: { default: 'row_hasPassword' } },
			{ field: 'accessCount', title: '访问次数', width: 100 },
			{ field: 'expireTime', title: '过期时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_expireTime' } },
			{ field: 'sharePermission', title: '分享权限', width: 100, slots: { default: 'row_sharePermission' } },
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

// 表格事件
const gridEvents: VxeGridListeners<any> = {};

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

		// 将相对路径转换为完整URL
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

<style lang="scss" scoped>
.document-share-manage-container {
	.full-table {
		border-radius: 12px;
		border: 1px solid var(--el-border-color-lighter);
	}
}
</style>
