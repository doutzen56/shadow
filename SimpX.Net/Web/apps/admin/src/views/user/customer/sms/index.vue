<template>
	<!-- 规格：短信邮件；接口 /admin/user/customer/sms/list -->
	<div class="user-customer-sms-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="手机/邮箱">
					<el-input v-model="state.query.Mobile" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.IsSuccess" clearable placeholder="全部" style="width: 110px">
						<el-option label="成功" value="1" />
						<el-option label="失败" value="0" />
					</el-select>
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:kehuguanxi:duanxinyoujian'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_ok="{ row }">
						<el-tag :type="row.IsSuccess ? 'success' : 'danger'" size="small">{{ row.IsSuccess ? '成功' : '失败' }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="短信/邮件详情" size="480px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
				<el-descriptions-item label="用户">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="手机/邮箱">{{ state.detailRow.Mobile || '-' }}</el-descriptions-item>
				<el-descriptions-item label="状态">{{ state.detailRow.IsSuccess ? '成功' : '失败' }}</el-descriptions-item>
				<el-descriptions-item label="时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
				<el-descriptions-item label="内容"><pre class="detail-pre">{{ state.detailRow.Content || '-' }}</pre></el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserCustomerSms">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserCustomerSms', {
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
const state = reactive({ query: { User: '', Mobile: '', IsSuccess: '' }, timeRange: rangeDays(0) as [string, string] | null, detailVisible: false, detailRow: null as Row | null, localPageParam });
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const options = useVxeTable<Row>(
	{
		id: 'siteUserCustomerSms',
		name: '短信邮件',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Mobile', title: '手机/邮箱', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'IsSuccess', title: '状态', width: 90, slots: { default: 'row_ok' } },
			{ field: 'Content', title: '内容', minWidth: 200, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 90, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		adminPost('/admin/user/customer/sms/list', {
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
			User: state.query.User || '',
			Mobile: state.query.Mobile || '',
			IsSuccess: state.query.IsSuccess || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
		})
	);
const resetQuery = () => {
	state.query.User = '';
	state.query.Mobile = '';
	state.query.IsSuccess = '';
	state.timeRange = rangeDays(0);
	handleQuery(true);
};
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
</script>

<style scoped>
.detail-pre { margin: 0; white-space: pre-wrap; font-size: 12px; }
</style>
