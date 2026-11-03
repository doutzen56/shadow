<template>
	<!-- 规格：Web.Admin/src/views/money/recharge/checkfail.html -->
	<div class="money-recharge-checkfail-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="用户名"><el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="上级代理"><el-input v-model="state.query.Agent" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="订单号"><el-input v-model="state.query.OrderID" clearable style="width: 160px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="到帐">
					<el-select v-model="state.query.IsPay" clearable placeholder="全部" style="width: 110px">
						<el-option label="已到帐" value="1" />
						<el-option label="未到账" value="0" />
					</el-select>
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="state.timeRange = todayRange(); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:chongzhiguanli:rukuanshenhe:biduishibaishenhe'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_money="{ row }">
						<span class="money-in">{{ formatMoney2(row.Money) }}</span>
					</template>
					<template #row_pay="{ row }">
						<div class="cell-stack">
							<span :class="row.IsPay === true || row.IsPay === 1 || row.IsPay === '1' ? 'ok' : 'no'">
								{{ row.IsPay === true || row.IsPay === 1 || row.IsPay === '1' ? '已到帐' : '未到账' }}
								<span v-if="(row.IsPay === true || row.IsPay === 1 || row.IsPay === '1') && row.Count" class="muted"> / {{ row.Count }}</span>
							</span>
							<span v-if="(row.IsPay === true || row.IsPay === 1 || row.IsPay === '1') && row.PayAt" class="muted">{{ formatDateTime(row.PayAt) }}</span>
						</div>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="比对失败订单" size="640px" destroy-on-close>
			<template v-if="state.detail">
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="订单编号">{{ state.detail.ID || state.detail.OrderID }}</el-descriptions-item>
					<el-descriptions-item label="当前状态">
						<span :class="state.detail.IsPay ? 'ok' : 'no'">{{ state.detail.IsPay ? '已到帐' : '未到账' }}</span>
					</el-descriptions-item>
					<el-descriptions-item label="充值会员">{{ state.detail.UserName }}（{{ state.detail.UserID }}）</el-descriptions-item>
					<el-descriptions-item label="充值渠道">{{ state.detail.Payment || state.detail.PayID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="提交时间">{{ formatDateTime(state.detail.CreateAt) }}</el-descriptions-item>
					<el-descriptions-item label="到帐时间">{{ state.detail.PayAt ? formatDateTime(state.detail.PayAt) : '-' }}</el-descriptions-item>
					<el-descriptions-item label="充值金额"><strong class="money-in">{{ formatMoney2(state.detail.Money) }}</strong></el-descriptions-item>
					<el-descriptions-item label="手续费">{{ formatMoney2(state.detail.Fee) }}</el-descriptions-item>
					<el-descriptions-item label="行政费用">{{ formatMoney2(state.detail.Cost) }}</el-descriptions-item>
					<el-descriptions-item label="实际到帐">{{ formatMoney2(state.detail.Paid) }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detail.Currency) }}</el-descriptions-item>
					<el-descriptions-item label="实充折算" v-if="state.detail.RealPaid">{{ formatRealPaidDisplay(state.detail.RealPaid, state.detail.Paid, state.detail.Currency) }}</el-descriptions-item>
					<el-descriptions-item label="备注" :span="2">{{ formatRechargeRemark(state.detail.Remark) || '-' }}</el-descriptions-item>
					<el-descriptions-item label="失败原因" :span="2">{{ state.detail.Message || state.detail.ErrorMsg || state.detail.FaildMsg || '-' }}</el-descriptions-item>
				</el-descriptions>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteMoneyRechargeCheckFail">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchRechargeOrderFailList, fetchRechargeOrderInfo } from '/@/api/money/recharge';
import { formatDateTime } from '/@/utils/format';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency, formatMoney2, formatRealPaidDisplay, formatRechargeRemark } from '/@/utils/admin-display';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { ElMessage } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyRechargeCheckFail', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const pad = (n: number) => String(n).padStart(2, '0');
const todayRange = (): [string, string] => {
	const d = new Date();
	const day = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
	return [`${day} 00:00:00`, `${day} 23:59:59`];
};
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
	query: { User: '', Agent: '', OrderID: '', Currency: '', IsPay: '' },
	autoRefresh: false,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	timeRange: todayRange() as [string, string] | null,
	detailVisible: false,
	detail: null as Row | null,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyRechargeCheckFail',
		name: '比对失败审核',
		columns: [
			{ field: 'ID', title: '订单号', minWidth: 150 },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '上级', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Money', title: '金额', minWidth: 110, slots: { default: 'row_money' } },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'Payment', title: '渠道', minWidth: 120, formatter: ({ row, cellValue }) => cellValue || row.PayName || (row.PayID ? `#${row.PayID}` : '-') },
			{ field: 'IsPay', title: '到帐', minWidth: 148, showOverflow: false, slots: { default: 'row_pay' } },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{
				field: 'Remark',
				title: '备注',
				minWidth: 160,
				formatter: ({ row, cellValue }) => formatRechargeRemark(cellValue || row.Description) || '-',
			},
			{ field: 'buttons', title: '操作', width: 100, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
let refreshTimer: ReturnType<typeof setInterval> | null = null;
const onAutoRefresh = (on: boolean) => {
	if (refreshTimer) { clearInterval(refreshTimer); refreshTimer = null; }
	if (on) refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
};
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() =>
		fetchRechargeOrderFailList({
			...state.query,
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
};
const resetQuery = () => {
	state.query.User = '';
	state.query.Agent = '';
	state.query.OrderID = '';
	state.query.Currency = '';
	state.query.IsPay = '';
	state.timeRange = todayRange();
	handleQuery(true);
};
const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = null;
	try {
		const info = await fetchRechargeOrderInfo(row.ID);
		state.detail = (info && typeof info === 'object' ? info : row) as Row;
	} catch (e: any) {
		state.detail = row;
		showCaughtError(e, '加载失败');
	}
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

onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
onMounted(() => {
	void ensureCurrencyEnum();
	void loadCurrencies();
});
</script>

<style scoped>
.ok { color: var(--el-color-success); }
.no { color: var(--el-color-danger); }
.money-in { color: #67c23a; }
.cell-stack { display: flex; flex-direction: column; line-height: 1.35; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
</style>
