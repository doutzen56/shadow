<template>
	<!-- 规格：Web.Admin report/user/userlog.html → /admin/report/user/userlog（与 loginlog 同服务） -->
	<div class="report-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="平台">
					<el-select v-model="state.query.Platform" clearable placeholder="全部" style="width: 110px">
						<el-option label="电脑" :value="0" />
						<el-option label="手机" :value="1" />
						<el-option label="APP" :value="2" />
					</el-select>
				</el-form-item>
				<el-form-item label="归属地">
					<el-input v-model="state.query.IPAddress" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="仅最近">
					<el-switch v-model="state.query.IsLast" />
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker
						v-model="state.timeRange"
						type="datetimerange"
						value-format="YYYY-MM-DD HH:mm:ss"
						range-separator="-"
						start-placeholder="开始"
						end-placeholder="结束"
						style="width: 340px"
					/>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="state.timeRange = todayRange(); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:report:user:userlog'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_platform="{ row }">
						<el-tag size="small" type="info">{{ formatPlatform(row.Platform) }}</el-tag>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteReportUserUserlog">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteReportUserUserlog', {
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
	const f = (d: Date) =>
		`${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const todayRange = (): [string, string] => rangeDays(0);
const state = reactive({ query: { User: '', Platform: '' as string | number, IPAddress: '', IsLast: false }, timeRange: todayRange() as [string, string] | null, localPageParam });
const formatPlatform = (cellValue: unknown) => {
	const p = String(cellValue ?? '');
	if (p === '0' || /pc|web/i.test(p)) return '电脑';
	if (p === '1' || /h5|mobile|wap/i.test(p)) return '手机';
	if (p === '2' || /app|ios|android/i.test(p)) return 'APP';
	return p || '-';
};
const options = useVxeTable<Row>(
	{
		id: 'siteReportUserUserlog',
		name: '会员日志',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'UserName', title: '用户', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'LoginAt', title: '登录时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'IP', title: 'IP', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'IPAddress', title: '归属地', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'DeviceID', title: '设备号', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'LoginDomain', title: '登录域名', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Platform', title: '平台', width: 90, slots: { default: 'row_platform' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		adminPost('/admin/report/user/userlog', {
			User: state.query.User || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
			Platform: state.query.Platform === '' || state.query.Platform === null || state.query.Platform === undefined ? '' : state.query.Platform,
			IPAddress: state.query.IPAddress || '',
			IsLast: state.query.IsLast ? '1' : '',
		})
	);
const resetQuery = () => {
	state.query.User = '';
	state.query.Platform = '';
	state.query.IPAddress = '';
	state.query.IsLast = false;
	state.timeRange = todayRange();
	handleQuery(true);
};
</script>
