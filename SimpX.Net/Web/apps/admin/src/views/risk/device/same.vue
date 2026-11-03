<template>
	<!-- 规格：同设备查询；User 或 Device + 时间段 -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户">
					<el-input v-model="state.query.User" clearable placeholder="用户名" style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="设备号">
					<el-input v-model="state.query.Device" clearable placeholder="设备号" style="width: 180px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="时间段">
					<el-date-picker
						v-model="state.dateRange"
						type="daterange"
						value-format="YYYY-MM-DD"
						range-separator="-"
						start-placeholder="开始"
						end-placeholder="结束"
						style="width: 260px"
					/>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:device:same'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<p class="hint">请输入用户名或设备号。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据（需先填用户或设备号）" /></template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteRiskDeviceSame">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchRiskDeviceSame } from '/@/api/risk/index';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskDeviceSame', {
	pageSize: 50 as number,
	defaultSort: { field: 'Count', order: 'desc', descStr: 'desc' },
});
const state = reactive({
	query: { User: '', Device: '' },
	dateRange: null as [string, string] | null,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteRiskDeviceSame',
		name: '同设备',
		columns: [
			{ field: 'UserID', title: '会员编号', width: 100 },
			{ field: 'UserName', title: '用户', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Count', title: '同设备次数', width: 120, formatter: ({ cellValue }) => cellValue ?? 0 },
		],
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { enabled: false },
	}
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	if (!state.query.User?.trim() && !state.query.Device?.trim()) {
		ElMessage.warning('请输入用户名或设备号');
		return { list: [], RecordCount: 0 };
	}
	return runAdminVxeQuery(async () => {
		const info = await fetchRiskDeviceSame({
			User: state.query.User || '',
			Device: state.query.Device || '',
			StartAt: state.dateRange?.[0] || '',
			EndAt: state.dateRange?.[1] || '',
		});
		const list = Array.isArray(info) ? info : Array.isArray((info as any)?.list) ? (info as any).list : [];
		return { list, RecordCount: list.length };
	});
};
const setRangeDays = (days: number) => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const p = (n: number) => String(n).padStart(2, '0');
	const f = (d: Date) => `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
	state.dateRange = [f(start), f(end)];
	handleQuery(true);
};
const resetQuery = () => {
	state.query.User = '';
	state.query.Device = '';
	state.dateRange = null;
	xGrid.value?.reloadData?.([]);
};
</script>

<style scoped>
.hint {
	margin: 0 0 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
