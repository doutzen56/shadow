<template>
	<!-- 规格：Web.Admin/src/views/money/withdraw/order.html → /admin/money/withdraw/orderlist -->
	<div class="money-withdraw-order-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefreshChange" />
				</el-form-item>
				<el-form-item label="订单号">
					<el-input v-model="state.query.ID" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="网关单号">
					<el-input v-model="state.query.SystemID" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="提现金额">
					<el-input v-model="state.query.MoneyFrom" clearable style="width: 90px" placeholder="起" />
					<span class="range-sep">-</span>
					<el-input v-model="state.query.MoneyTo" clearable style="width: 90px" placeholder="止" />
				</el-form-item>
				<el-form-item label="提现渠道">
					<el-select v-model="state.query.ChannelID" clearable filterable placeholder="全部" style="width: 140px">
						<el-option v-for="c in state.channelOptions" :key="String(c.ID)" :label="c.Name || String(c.ID)" :value="String(c.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 120px">
						<el-option label="待处理" value="None" />
						<el-option label="处理中" value="Wait" />
						<el-option label="已完成" value="Finish" />
						<el-option label="失败" value="Faild" />
						<el-option label="出款中" value="InPayment" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="出款币种">
					<el-select v-model="state.query.FromCurrency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="'from-' + String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="审核时长">
					<el-input v-model="state.query.StartAudit" clearable style="width: 70px" placeholder="起" />
					<span class="range-sep">-</span>
					<el-input v-model="state.query.EndAudit" clearable style="width: 70px" placeholder="止" />
					<span class="muted">分钟</span>
				</el-form-item>
				<el-form-item label="出款时长">
					<el-input v-model="state.query.StartCash" clearable style="width: 70px" placeholder="起" />
					<span class="range-sep">-</span>
					<el-input v-model="state.query.EndCash" clearable style="width: 70px" placeholder="止" />
					<span class="muted">分钟</span>
				</el-form-item>
				<el-form-item label="时间类型">
					<el-select v-model="state.query.DateType" style="width: 120px">
						<el-option label="申请时间" value="CreateAt" />
						<el-option label="到帐时间" value="PayAt" />
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
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button @click="setRangeDays(30)">近30天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:tixianguanli:tixiandingdan'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain :loading="state.exporting" v-auth="'site:money:tixianguanli:tixiandingdan'" @click="handleExport">导出(CSV)</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style vxe-multiline vxe-row-2line" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="140" description="暂无数据（可点「近30天」再查）" />
					</template>
					<template #row_wmoney="{ row }">
						<span class="money-out">{{ formatMoney(row.Money) }}</span>
					</template>
					<template #row_fee="{ row }">
						<span :class="Number(row.Fee) > 0 ? 'money-out' : ''">{{ formatMoney(row.Fee) }}</span>
					</template>
					<template #row_cost="{ row }">
						<span :class="Number(row.Cost) > 0 ? 'money-out' : ''">{{ formatMoney(row.Cost) }}</span>
					</template>
					<template #row_real="{ row }">
						<span class="money-out">{{ formatRealPaidDisplay(row.RealPaid, row.Paid, row.Currency ?? row.CurrencyCode) }}</span>
					</template>
					<template #row_time="{ row }">
						<div class="cell-stack">
							<span class="muted">{{ formatCurrency(row.Currency) }}</span>
							<span>{{ formatDateTime(row.CreateAt) }}</span>
						</div>
					</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="
								statusText(row.StatusValue || row.Status).includes('完成')
									? 'success'
									: statusText(row.StatusValue || row.Status).includes('失败')
										? 'danger'
										: statusText(row.StatusValue || row.Status).includes('出款') || statusText(row.StatusValue || row.Status).includes('处理')
											? 'warning'
											: 'info'
							"
						>
							{{ statusText(row.StatusValue || row.Status) }}
						</el-tag>
					</template>
					<template #row_bank="{ row }">
						<div class="cell-stack">
							<span>{{ row.AccountName || row.Bank || '-' }}</span>
							<span class="muted">{{ row.AccountNumber || '-' }}</span>
						</div>
					</template>
					<template #row_deal="{ row }">
						<div class="cell-stack">
							<span class="muted">审：{{ formatDealTime(row.AuditTime) }}</span>
							<span class="muted">出：{{ formatDealTime(row.DealTime) }}</span>
						</div>
					</template>
					<template #row_buttons="{ row }">
						<el-button type="primary" link @click="openDetail(row)">详情</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="提现订单详情" size="640px" destroy-on-close>
			<template v-if="state.detail">
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="订单号">{{ state.detail.ID || state.detail.OrderID }}</el-descriptions-item>
					<el-descriptions-item label="状态">{{ statusText(state.detail.StatusValue || state.detail.Status) }}</el-descriptions-item>
					<el-descriptions-item label="用户">{{ state.detail.UserName }}（{{ state.detail.UserID }}）</el-descriptions-item>
					<el-descriptions-item label="上级">{{ state.detail.AgentName || state.detail.Agent || '-' }}</el-descriptions-item>
					<el-descriptions-item label="分组">{{ state.detail.Group || '-' }}</el-descriptions-item>
					<el-descriptions-item label="渠道">{{ state.detail.Channel || state.detail.ChannelID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detail.Currency ?? state.detail.CurrencyCode) || '-' }}</el-descriptions-item>
					<el-descriptions-item label="提现金额">{{ formatMoney(state.detail.Money) }}</el-descriptions-item>
					<el-descriptions-item label="手续费">{{ formatMoney(state.detail.Fee) }}</el-descriptions-item>
					<el-descriptions-item label="行政费">{{ formatMoney(state.detail.Cost) }}</el-descriptions-item>
					<el-descriptions-item label="实际到帐">{{ formatRealPaidDisplay(state.detail.RealPaid, state.detail.Paid, state.detail.Currency ?? state.detail.CurrencyCode) }}</el-descriptions-item>
					<el-descriptions-item label="开户名">{{ state.detail.AccountName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="账号">{{ state.detail.AccountNumber || '-' }}</el-descriptions-item>
					<el-descriptions-item label="审核人">{{ state.detail.Admin || '-' }}</el-descriptions-item>
					<el-descriptions-item label="出款人">{{ state.detail.Cashier || '-' }}</el-descriptions-item>
					<el-descriptions-item label="申请时间">{{ formatDateTime(state.detail.CreateAt) }}</el-descriptions-item>
					<el-descriptions-item label="处理时间">{{ formatDateTime(state.detail.PayAt) }}</el-descriptions-item>
					<el-descriptions-item label="审核时长">{{ formatDealTime(state.detail.AuditTime) }}</el-descriptions-item>
					<el-descriptions-item label="出款时长">{{ formatDealTime(state.detail.DealTime) }}</el-descriptions-item>
					<el-descriptions-item label="前端备注" :span="2">{{ state.detail.Description || '-' }}</el-descriptions-item>
					<el-descriptions-item label="后端备注" :span="2">{{ state.detail.DescriptionA || state.detail.Mark || '-' }}</el-descriptions-item>
				</el-descriptions>
				<div v-if="canProcess(state.detail)" class="process-box">
					<el-divider content-position="left">处理提现</el-divider>
					<el-form label-width="90px" @submit.prevent>
						<el-form-item label="前端备注">
							<el-input v-model="state.process.Description" clearable />
						</el-form-item>
						<el-form-item label="后端备注">
							<el-input v-model="state.process.DescriptionA" clearable />
						</el-form-item>
						<template v-if="isWait(state.detail)">
							<el-form-item label="出款渠道">
								<el-select v-model="state.process.ChannelID" clearable filterable placeholder="选择渠道" style="width: 100%">
									<el-option v-for="c in state.channelOptions" :key="String(c.ID)" :label="c.Name || String(c.ID)" :value="String(c.ID)" />
								</el-select>
							</el-form-item>
							<el-form-item label="手续费">
								<el-input v-model="state.process.Fee" clearable />
							</el-form-item>
						</template>
						<el-form-item>
							<el-button type="success" :loading="state.processing" @click="onProcess('Agree')">{{ isWait(state.detail) ? '确认出款' : '通过审核' }}</el-button>
							<el-button type="danger" :loading="state.processing" @click="onProcess('Reject')">{{ isWait(state.detail) ? '拒绝出款' : '拒绝审核' }}</el-button>
						</el-form-item>
					</el-form>
				</div>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteMoneyWithdrawOrder">
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { checkWithdrawOrder, execWithdrawOrder, fetchWithdrawChannelList, fetchWithdrawOrderInfo, fetchWithdrawOrderList } from '/@/api/money/withdraw';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { readAdminList } from '/@/utils/admin-api';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency, formatRealPaidDisplay, formatWithdrawStatus } from '/@/utils/admin-display';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyWithdrawOrder', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const pad = (n: number) => String(n).padStart(2, '0');
const fmt = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	end.setHours(23, 59, 59, 0);
	const start = new Date();
	start.setHours(0, 0, 0, 0);
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	return [fmt(start), fmt(end)];
};

const emptyQuery = () => ({ ID: '', SystemID: '', User: '', Agent: '', Status: '', ChannelID: '', Currency: '', FromCurrency: '', DateType: 'CreateAt', MoneyFrom: '', MoneyTo: '', StartAudit: '', EndAudit: '', StartCash: '', EndCash: '' });
const state = reactive({
	query: emptyQuery(),
	timeRange: rangeDays(30) as [string, string] | null,
	detailVisible: false,
	detail: null as Row | null,
	exporting: false,
	autoRefresh: false,
	processing: false,
	process: { Description: '', DescriptionA: '', ChannelID: '', Fee: '' },
	channelOptions: [] as Array<{ ID?: number | string; Name?: string }>,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	localPageParam,
});

let refreshTimer: ReturnType<typeof setInterval> | null = null;

const formatMoney = (v: unknown) => {
	const n = Number(v);
	return Number.isFinite(n) ? n.toFixed(2) : '-';
};
const formatDealTime = (v: unknown) => {
	const s = String(v ?? '').trim();
	if (!s || !s.includes(':')) return 'N/A';
	const [h, m, sec] = s.split(':');
	if (h == null || m == null || sec == null) return 'N/A';
	return `${Number(h)}小时${m}分${sec}秒`;
};
const statusText = (s: unknown) => formatWithdrawStatus(s);
const statusClass = (s: unknown) => {
	const t = statusText(s);
	if (t.includes('完成')) return 'ok';
	if (t.includes('失败')) return 'bad';
	if (t.includes('出款') || t.includes('处理')) return 'warn';
	return 'muted';
};
const statusKey = (row: Row | null | undefined) => String(row?.StatusValue ?? row?.Status ?? '');
const isNone = (row: Row | null | undefined) => ['None', '0', '待处理'].includes(statusKey(row)) || statusText(statusKey(row)) === '待处理';
const isWait = (row: Row | null | undefined) => ['Wait', '1', '处理中'].includes(statusKey(row)) || statusText(statusKey(row)) === '处理中';
const canProcess = (row: Row | null | undefined) => !!row && (isNone(row) || isWait(row));

const options = useVxeTable<Row>(
	{
		id: 'siteMoneyWithdrawOrder',
		name: '提现订单',
		showOverflow: false,
		columns: [
			{ field: 'ID', title: '订单编号', minWidth: 100, showOverflow: true },
			{ field: 'UserName', title: '用户名', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '上级', minWidth: 90, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Group', title: '所在分组', minWidth: 90, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Money', title: '提现金额', minWidth: 96, slots: { default: 'row_wmoney' } },
			{ field: 'Fee', title: '手续费', minWidth: 80, slots: { default: 'row_fee' } },
			{ field: 'Cost', title: '行政费', minWidth: 80, slots: { default: 'row_cost' } },
			{ field: 'RealPaid', title: '实际到帐', minWidth: 120, slots: { default: 'row_real' } },
			{ field: 'CreateAt', title: '提交时间', minWidth: 140, slots: { default: 'row_time' } },
			{ field: 'PayAt', title: '处理时间', minWidth: 140, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Channel', title: '付款渠道', minWidth: 100, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Bank', title: '提款信息', minWidth: 140, slots: { default: 'row_bank' } },
			{ field: 'Admin', title: '审核人', minWidth: 90, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Cashier', title: '出款人', minWidth: 90, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AuditTime', title: '时长', minWidth: 130, slots: { default: 'row_deal' } },
			{ field: 'Description', title: '前端备注', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'DescriptionA', title: '后端备注', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'buttons', title: '操作', width: 88, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
		rowConfig: { isHover: true },
		cellConfig: { verticalAlign: 'center' as any, height: 'auto' as any },
		scrollY: { enabled: false },
		virtualYConfig: { enabled: false },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

const buildQuery = (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, pageSize?: number) => ({
	ID: state.query.ID || '',
	SystemID: state.query.SystemID || '',
	User: state.query.User || '',
	Agent: state.query.Agent || '',
	Status: state.query.Status || '',
	ChannelID: state.query.ChannelID || '',
	MoneyFrom: state.query.MoneyFrom || '',
	MoneyTo: state.query.MoneyTo || '',
	Currency: state.query.Currency || '',
	FromCurrency: state.query.FromCurrency || '',
	DateType: state.query.DateType || 'CreateAt',
	StartAudit: state.query.StartAudit || '',
	EndAudit: state.query.EndAudit || '',
	StartCash: state.query.StartCash || '',
	EndCash: state.query.EndCash || '',
	StartAt: state.timeRange?.[0] || '',
	EndAt: state.timeRange?.[1] || '',
	PageIndex: page.currentPage,
	PageSize: pageSize ?? page.pageSize,
});

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() => fetchWithdrawOrderList(buildQuery(page)));
};

const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.timeRange = rangeDays(30);
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

const loadChannels = async () => {
	try {
		const info = await fetchWithdrawChannelList({});
		const list = Array.isArray(info) ? info : readAdminList(info as any);
		state.channelOptions = list as any[];
	} catch (e: any) {
		state.channelOptions = [];
		showCaughtError(e, '加载渠道失败');
	}
};

const onAutoRefreshChange = (on: string | number | boolean) => {
	if (refreshTimer) {
		clearInterval(refreshTimer);
		refreshTimer = null;
	}
	if (on) refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
};

const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = { ...row };
	state.process = {
		Description: String(row.Description || ''),
		DescriptionA: String(row.DescriptionA || ''),
		ChannelID: row.ChannelID ? String(row.ChannelID) : '',
		Fee: row.Fee != null ? String(row.Fee) : '',
	};
	try {
		const info = (await fetchWithdrawOrderInfo(row.ID)) as Row;
		if (info && typeof info === 'object') {
			state.detail = { ...row, ...info };
			state.process.Description = String(info.Description ?? state.process.Description);
			state.process.DescriptionA = String(info.DescriptionA ?? state.process.DescriptionA);
			if (info.Fee != null) state.process.Fee = String(info.Fee);
			if (info.ChannelID) state.process.ChannelID = String(info.ChannelID);
		}
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};

const onProcess = async (action: 'Agree' | 'Reject') => {
	const row = state.detail;
	if (!row) return;
	const tip = action === 'Agree' ? (isWait(row) ? '确认出款？' : '确认通过审核？') : isWait(row) ? '确认拒绝出款？' : '确认拒绝审核？';
	try {
		await ElMessageBox.confirm(tip, '资金操作确认', { type: 'warning' });
		state.processing = true;
		const payload = {
			OrderID: row.ID || row.OrderID,
			ID: row.ID || row.OrderID,
			Action: action,
			Description: state.process.Description || '',
			DescriptionA: state.process.DescriptionA || '',
			ChannelID: state.process.ChannelID || '',
			Fee: state.process.Fee || '',
		};
		if (isWait(row)) await execWithdrawOrder(payload);
		else await checkWithdrawOrder(payload);
		ElMessage.success('已提交');
		state.detailVisible = false;
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel' && e?.toString?.() !== 'cancel') showCaughtError(e);
	} finally {
		state.processing = false;
	}
};

const csvCell = (v: unknown) => {
	const s = v == null ? '' : String(v);
	return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
};

const handleExport = async () => {
	state.exporting = true;
	try {
		const info = await fetchWithdrawOrderList(buildQuery({ currentPage: 1, pageSize: 2000 } as any, 2000));
		const rows = readAdminList(info as any);
		if (!rows.length) {
			ElMessage.warning('没有可导出的数据');
			return;
		}
		const headers = ['ID', 'UserName', 'AgentName', 'Group', 'Money', 'Fee', 'Cost', 'RealPaid', 'StatusValue', 'Channel', 'Admin', 'Cashier', 'CreateAt', 'PayAt'];
		const lines = [headers.join(','), ...rows.map((r) => headers.map((h) => csvCell((r as any)[h])).join(','))];
		const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `withdraw-order-${Date.now()}.csv`;
		a.click();
		URL.revokeObjectURL(url);
		ElMessage.success(`已导出 ${rows.length} 行`);
	} catch (e: any) {
		showCaughtError(e, '导出失败');
	} finally {
		state.exporting = false;
	}
};

onMounted(() => {
	void ensureCurrencyEnum();
	loadChannels();
	void loadCurrencies();
});
onBeforeUnmount(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped>
.muted {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}
.ok {
	color: #67c23a;
	font-weight: 600;
}
.bad {
	color: #f56c6c;
	font-weight: 600;
}
.warn {
	color: #e6a23c;
	font-weight: 600;
}
.range-sep {
	margin: 0 4px;
	color: var(--el-text-color-secondary);
}
.process-box {
	margin-top: 8px;
}
.money-out { color: #f56c6c; }
</style>
