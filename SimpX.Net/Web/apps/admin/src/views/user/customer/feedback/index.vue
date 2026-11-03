<template>
	<!-- 规格：客户意见反馈；/admin/user/customer/feedback/list|delete -->
	<div class="user-customer-feedback-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="内容">
					<el-input v-model="state.query.Content" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="手机">
					<el-input v-model="state.query.Mobile" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="邮箱">
					<el-input v-model="state.query.Email" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:kehuguanxi:zhanneixinxi'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="反馈详情" size="480px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
				<el-descriptions-item label="用户">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="手机">{{ state.detailRow.Mobile || '-' }}</el-descriptions-item>
				<el-descriptions-item label="邮箱">{{ state.detailRow.Email || '-' }}</el-descriptions-item>
				<el-descriptions-item label="时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
				<el-descriptions-item label="内容"><pre class="detail-pre">{{ state.detailRow.Content || '-' }}</pre></el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserCustomerFeedback">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserCustomerFeedback', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	end.setHours(23, 59, 59, 0);
	const start = new Date();
	start.setHours(0, 0, 0, 0);
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const state = reactive({
	query: { User: '', Content: '', Mobile: '', Email: '' },
	timeRange: null as [string, string] | null,
	detailVisible: false,
	detailRow: null as Row | null,
	localPageParam,
});
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const options = useVxeTable<Row>(
	{
		id: 'siteUserCustomerFeedback',
		name: '意见反馈',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Mobile', title: '手机', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Email', title: '邮箱', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Content', title: '内容', minWidth: 200, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/user/customer/feedback/list', {
			User: state.query.User || '',
			Content: state.query.Content || '',
			Mobile: state.query.Mobile || '',
			Email: state.query.Email || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
		});
		const list = Array.isArray((info as any)?.list) ? (info as any).list : Array.isArray(info) ? (info as any) : [];
		return { list, RecordCount: list.length };
	});
const resetQuery = () => {
	state.query.User = '';
	state.query.Content = '';
	state.query.Mobile = '';
	state.query.Email = '';
	state.timeRange = null;
	handleQuery(true);
};
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除该反馈？', '提示', { type: 'warning' });
		await adminPost('/admin/user/customer/feedback/delete', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
</script>

<style scoped>
.detail-pre { margin: 0; white-space: pre-wrap; font-size: 12px; }
</style>
