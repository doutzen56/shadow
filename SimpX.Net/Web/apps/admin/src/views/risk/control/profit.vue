<template>
	<!-- 旧站为图表 ADMIN_USER_PROFIT；本页用 /admin/risk/control/profit 列表过渡 -->
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:control:profit'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<p class="hint">旧站为报表图 ADMIN_USER_PROFIT；当前为资金分类盈亏列表过渡态。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_total="{ row }">
						<span :class="Number(row.Total) < 0 ? 'money-out' : 'money-in'">{{ formatMoney(row.Total) }}</span>
					</template>
					<template #row_cats="{ row }">
						<span class="cats">{{ formatCats(row) }}</span>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteRiskControlProfit">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchRiskControlProfit } from '/@/api/risk/index';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskControlProfit', {
	pageSize: 50 as number,
	defaultSort: { field: 'Total', order: 'desc', descStr: 'desc' },
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
const formatCats = (row: Row) => {
	const cats = row.Categories;
	if (!cats || typeof cats !== 'object') return '-';
	return Object.entries(cats as Record<string, number>)
		.map(([k, v]) => `${k}:${formatMoney(v)}`)
		.join('；');
};
const options = useVxeTable<Row>(
	{
		id: 'siteRiskControlProfit',
		name: '会员盈利',
		columns: [
			{ field: 'UserID', title: '会员编号', width: 100 },
			{ field: 'UserName', title: '用户', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Total', title: '合计', minWidth: 120, slots: { default: 'row_total' } },
			{ field: 'Categories', title: '分类明细', minWidth: 280, slots: { default: 'row_cats' } },
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
	if (Array.isArray(info.Items)) return info.Items;
	return [];
};
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await fetchRiskControlProfit({
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
.hint {
	margin: 0 0 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.cats {
	font-size: 12px;
	word-break: break-all;
}
.money-in { color: #67c23a; }
.money-out { color: #f56c6c; }
</style>
