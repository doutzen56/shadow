<template>
	<!-- 规格：Web.Admin report/user/deposit123.html → /admin/report/user/deposit -->
	<div class="report-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="包含下级">
					<el-checkbox v-model="state.belongUser" />
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
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
				<el-button @click="state.timeRange = rangeDays(0); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button @click="setRechargeCount(1)">首充</el-button>
				<el-button @click="setRechargeCount(2)">二充</el-button>
				<el-button @click="setRechargeCount(3)">三充</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:report:user:deposit123'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_m1="{ row }"><span class="money-in">{{ formatPair(row.Money1, row.Recharge1) }}</span></template>
					<template #row_m2="{ row }"><span class="money-in">{{ formatPair(row.Money2, row.Recharge2) }}</span></template>
					<template #row_m3="{ row }"><span class="money-in">{{ formatPair(row.Money3, row.Recharge3) }}</span></template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteReportDeposit123">
import { onMounted, reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency } from '/@/utils/admin-display';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { showCaughtError } from '/@/utils/axios-utils';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteReportDeposit123', {
	pageSize: 50 as number,
	defaultSort: { field: 'Date', order: 'desc', descStr: 'desc' },
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
const formatMoney = (v: unknown) => {
	const n = Number(v);
	return Number.isFinite(n) ? n.toFixed(2) : '-';
};
const formatPair = (a: unknown, b: unknown) => `${formatMoney(a)}/${formatMoney(b)}`;

const state = reactive({
	query: { User: '', Currency: '' },
	belongUser: false,
	rechargeCount: 0,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	timeRange: rangeDays(7) as [string, string] | null,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteReportDeposit123',
		name: '存款一二三',
		columns: [
			{ field: 'UserID', title: 'ID', width: 80 },
			{ field: 'UserName', title: '用户名称', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Date', title: '到账日', minWidth: 120, formatter: ({ cellValue }) => formatDateTime(cellValue)?.slice(0, 10) || '-' },
			{ field: 'Money1', title: '首充/到账', minWidth: 140, slots: { default: 'row_m1' } },
			{ field: 'Money2', title: '二充/到账', minWidth: 140, slots: { default: 'row_m2' } },
			{ field: 'Money3', title: '三充/到账', minWidth: 140, slots: { default: 'row_m3' } },
			{ field: 'Currency', title: '币种', width: 90, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/report/user/deposit', {
			User: state.query.User || '',
			BelongUser: state.belongUser ? 1 : 0,
			Currency: state.query.Currency || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			rechargecount: state.rechargeCount || 0,
			RechargeCount: state.rechargeCount || 0,
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		});
		const list = Array.isArray(info) ? info : Array.isArray((info as any)?.list) ? (info as any).list : [];
		return { list, RecordCount: list.length };
	});
};
const setRechargeCount = (n: number) => {
	state.rechargeCount = n;
	handleQuery(true);
};
const resetQuery = () => {
	state.query.User = '';
	state.query.Currency = '';
	state.belongUser = false;
	state.rechargeCount = 0;
	state.timeRange = rangeDays(7);
	handleQuery(true);
};
const loadCurrencies = async () => {
	await ensureCurrencyEnum().catch(() => undefined);
	try {
		const list = await fetchCurrencyOptions();
		state.currencyOptions = (list || []).map((x: any) => ({
			key: x.key ?? x.Key ?? x.value ?? x.Value,
			value: String(x.value ?? x.Value ?? x.key ?? ''),
		}));
	} catch (e: any) {
		state.currencyOptions = [];
		showCaughtError(e, '加载币种失败');
	}
};
onMounted(() => {
	void ensureCurrencyEnum();
	void loadCurrencies();
});
</script>
<style scoped>
.money-in { color: #67c23a; }
</style>
