<template>
	<!-- 规格：/admin/risk/control/game → UserDateGame 按用户×类型汇总 -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="日期">
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:control:game'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_money="{ row }">
						<span :class="Number(row.Money) < 0 ? 'money-out' : 'money-in'">{{ formatMoney(row.Money) }}</span>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteRiskControlGame">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchRiskControlGame } from '/@/api/risk/index';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskControlGame', {
	pageSize: 50 as number,
	defaultSort: { field: 'Money', order: 'desc', descStr: 'desc' },
});
const today = () => {
	const d = new Date();
	const p = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
};
const state = reactive({
	query: { User: '' },
	dateRange: [today(), today()] as [string, string] | null,
	localPageParam,
});
const formatMoney = (v: unknown) => {
	const n = Number(v);
	return Number.isFinite(n) ? n.toFixed(2) : '-';
};
const options = useVxeTable<Row>(
	{
		id: 'siteRiskControlGame',
		name: '游戏盈利',
		columns: [
			{ field: 'UserID', title: '会员编号', width: 100 },
			{ field: 'UserName', title: '用户', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Type', title: '游戏类型', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Money', title: '盈亏', minWidth: 120, slots: { default: 'row_money' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const unwrap = (info: any): Row[] => {
	if (!info) return [];
	if (Array.isArray(info)) return info;
	if (Array.isArray(info.Rows)) return info.Rows;
	if (Array.isArray(info.list)) return info.list;
	return [];
};
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await fetchRiskControlGame({
			User: state.query.User || '',
			StartAt: state.dateRange?.[0] || '',
			EndAt: state.dateRange?.[1] || '',
		});
		const list = unwrap(info);
		return { list, RecordCount: list.length };
	});
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
	state.dateRange = [today(), today()];
	handleQuery(true);
};
</script>
<style scoped>
.money-in { color: #67c23a; }
.money-out { color: #f56c6c; }
</style>
