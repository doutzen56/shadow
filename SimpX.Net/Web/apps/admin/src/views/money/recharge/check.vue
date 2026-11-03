<template>
	<!-- 规格：Web.Admin/src/views/money/recharge/check.html；审核写操作需人工复核资金后果 -->
	<div class="money-recharge-check-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefreshChange" />
				</el-form-item>
				<el-form-item label="声音提醒">
					<el-switch v-model="state.soundAlert" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="入款渠道">
					<el-select v-model="state.query.PayID" clearable filterable placeholder="全部" style="width: 140px">
						<el-option v-for="p in state.payOptions" :key="String(p.ID)" :label="p.Name || String(p.ID)" :value="String(p.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="提交时间">
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
				<el-form-item label="订单号">
					<el-input v-model="state.query.OrderID" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部状态" style="width: 120px">
						<el-option label="待审" value="None" />
						<el-option label="失败" value="Faild" />
						<el-option label="通过" value="Success" />
						<el-option label="处理中" value="Processing" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:chongzhiguanli:rukuanshenhe'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style vxe-multiline vxe-row-2line" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无待审" /></template>
					<template #row_money="{ row }">
						<div class="cell-stack">
							<span class="money-in">入款：{{ formatMoney(row.Money) }}</span>
							<span class="muted">实到：{{ formatMoney(row.Paid) }}</span>
						</div>
					</template>
					<template #row_bank="{ row }">
						<div class="cell-stack">
							<span>{{ isUsdt(row.Currency) ? '汇款地址' : '姓名' }}：{{ row.Name || '-' }}</span>
							<span class="muted">渠道：{{ row.PayName || row.PayID || '-' }}</span>
						</div>
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
							:type="formatStatus(row.Status) === '通过' ? 'success' : formatStatus(row.Status) === '失败' ? 'danger' : formatStatus(row.Status) === '待审' ? 'warning' : formatStatus(row.Status) === '处理中' ? '' : 'info'"
						>
							{{ formatStatus(row.Status) }}
						</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<template v-if="canOperate(row.Status)">
							<el-button v-if="isPending(row.Status)" type="warning" link @click="onAction(row, 'hang')">挂起</el-button>
							<el-button type="success" link @click="onAction(row, 'agree')">通过</el-button>
							<el-button type="danger" link @click="onAction(row, 'reject')">拒绝</el-button>
						</template>
						<el-button type="primary" link @click="openDetail(row)">{{ canOperate(row.Status) ? '操作' : '明细' }}</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" :title="canOperate(state.detail?.Status) ? '审核操作' : '入款明细'" size="560px" destroy-on-close>
			<template v-if="state.detail">
				<el-descriptions :column="1" border size="small">
					<el-descriptions-item label="编号">{{ state.detail.ID }}</el-descriptions-item>
					<el-descriptions-item label="充值订单号">{{ state.detail.OrderID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="用户">{{ state.detail.UserName }}</el-descriptions-item>
					<el-descriptions-item label="入款金额">{{ formatMoney(state.detail.Money) }}</el-descriptions-item>
					<el-descriptions-item label="实际到帐">{{ formatMoney(state.detail.Paid) }}</el-descriptions-item>
					<el-descriptions-item label="姓名/地址">{{ state.detail.Name || '-' }}</el-descriptions-item>
					<el-descriptions-item label="入款渠道">{{ state.detail.PayName || state.detail.PayID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detail.Currency) }}</el-descriptions-item>
					<el-descriptions-item label="状态">{{ formatStatus(state.detail.Status) }}</el-descriptions-item>
					<el-descriptions-item label="提交时间">{{ formatDateTime(state.detail.CreateAt) }}</el-descriptions-item>
					<el-descriptions-item label="审核时间">{{ formatDateTime(state.detail.CheckAt) }}</el-descriptions-item>
					<el-descriptions-item label="审核人">{{ state.detail.Admin || '-' }}</el-descriptions-item>
					<el-descriptions-item label="备注">{{ formatRechargeRemark(state.detail.Description || state.detail.Remark) || '-' }}</el-descriptions-item>
				</el-descriptions>
				<div v-if="canOperate(state.detail.Status)" class="check-actions">
					<el-button type="success" :loading="state.acting" @click="onAction(state.detail, 'agree')">通过</el-button>
					<el-button type="danger" :loading="state.acting" @click="onAction(state.detail, 'reject')">拒绝</el-button>
					<el-button v-if="isPending(state.detail.Status)" type="warning" :loading="state.acting" @click="onAction(state.detail, 'hang')">挂起</el-button>
				</div>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteMoneyRechargeCheck">
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost, readAdminList } from '/@/utils/admin-api';
import { fetchRechargeCheckInfo, fetchRechargeCheckList, fetchRechargePayOptions } from '/@/api/money/recharge';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency, formatMoneyCheckStatus, formatRechargeRemark } from '/@/utils/admin-display';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyRechargeCheck', {
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
const state = reactive({
	query: { User: '', Agent: '', OrderID: '', PayID: '', Status: 'None', Currency: '' },
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	timeRange: rangeDays(0) as [string, string] | null,
	payOptions: [] as Array<{ ID?: number | string; Name?: string }>,
	detailVisible: false,
	detail: null as Row | null,
	acting: false,
	autoRefresh: false,
	/** 对齐旧站 Transfer 入款待审提示音 */
	soundAlert: true,
	localPageParam,
});
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};

let refreshTimer: ReturnType<typeof setInterval> | null = null;
let lastSoundPendingIds = '';
let soundCache: Record<string, string> | null = null;
let audioEl: HTMLAudioElement | null = null;

const formatMoney = (v: unknown) => {
	const n = Number(v);
	return Number.isFinite(n) ? n.toFixed(2) : v == null || v === '' ? '-' : String(v);
};
const formatStatus = (s: unknown) => formatMoneyCheckStatus(s);
const statusClass = (s: unknown) => {
	const t = formatStatus(s);
	if (t === '待审') return 'st-none';
	if (t === '处理中') return 'st-proc';
	if (t === '通过') return 'st-ok';
	if (t === '失败') return 'st-fail';
	return '';
};
const isPending = (s: unknown) => {
	const key = String(s ?? '');
	return key === 'None' || key === '0' || formatMoneyCheckStatus(key) === '待审';
};
const canOperate = (s: unknown) => {
	const t = formatStatus(s);
	return t === '待审' || t === '处理中';
};
const isUsdt = (c: unknown) => formatCurrency(c).toUpperCase() === 'USDT';

const options = useVxeTable<Row>(
	{
		id: 'siteMoneyRechargeCheck',
		name: '入款审核',
		showOverflow: false,
		columns: [
			{ field: 'ID', title: '编号', width: 70 },
			{ field: 'OrderID', title: '充值订单号', minWidth: 140, showOverflow: true },
			{ field: 'UserName', title: '用户名', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Money', title: '存款金额', minWidth: 130, showOverflow: false, slots: { default: 'row_money' } },
			{ field: 'PayName', title: '银行账户', minWidth: 150, showOverflow: false, slots: { default: 'row_bank' } },
			{ field: 'CreateAt', title: '提交时间', minWidth: 140, showOverflow: false, slots: { default: 'row_time' } },
			{ field: 'CheckAt', title: '审核时间', minWidth: 140, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Admin', title: '审核人', minWidth: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{
				field: 'Description',
				title: '备注',
				minWidth: 120,
				showOverflow: true,
				formatter: ({ cellValue }) => formatRechargeRemark(cellValue) || '-',
			},
			{ field: 'buttons', title: '操作', width: 220, fixed: 'right', slots: { default: 'row_buttons' } },
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

/** 拉取管理员提示音配置（Transfer=入款审核）。 */
const loadSoundMap = async () => {
	if (soundCache) return soundCache;
	try {
		const info = (await adminPost('/admin/site/admin/sound', {})) as Record<string, string>;
		soundCache = info && typeof info === 'object' ? info : {};
	} catch {
		soundCache = {};
	}
	return soundCache;
};

/**
 * 待审有单时播放 Transfer 音（挂起/处理中不再提醒，对齐旧站文案）。
 * @param rows 当前页行
 */
const maybePlayPendingSound = async (rows: Row[]) => {
	if (!state.soundAlert) return;
	const pending = rows.filter((r) => isPending(r.Status));
	if (!pending.length) {
		lastSoundPendingIds = '';
		return;
	}
	const sig = pending
		.map((r) => String(r.ID))
		.sort()
		.join(',');
	if (sig === lastSoundPendingIds) return;
	lastSoundPendingIds = sig;
	const map = await loadSoundMap();
	const url = String(map?.Transfer || map?.transfer || '').trim();
	try {
		if (!audioEl) audioEl = new Audio();
		if (url) {
			audioEl.src = url;
			await audioEl.play();
		} else {
			// 未配置自定义音时用短蜂鸣兜底，避免静默。
			const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
			const o = ctx.createOscillator();
			const g = ctx.createGain();
			o.connect(g);
			g.connect(ctx.destination);
			o.frequency.value = 880;
			g.gain.value = 0.05;
			o.start();
			setTimeout(() => {
				o.stop();
				void ctx.close();
			}, 220);
		}
	} catch {
		/* 浏览器可能拦截未手势播放，忽略 */
	}
};

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	const result = await runAdminVxeQuery(() =>
		fetchRechargeCheckList({
			User: state.query.User || '',
			Agent: state.query.Agent || '',
			OrderID: state.query.OrderID || '',
			PayID: state.query.PayID || '',
			Status: state.query.Status || '',
			Currency: state.query.Currency || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
	const rows = ((result as any)?.result as Row[]) || [];
	void maybePlayPendingSound(rows);
	return result;
};

const resetQuery = () => {
	state.query.User = '';
	state.query.Agent = '';
	state.query.OrderID = '';
	state.query.PayID = '';
	state.query.Status = 'None';
	state.query.Currency = '';
	state.timeRange = rangeDays(0);
	handleQuery(true);
};

const loadPayOptions = async () => {
	try {
		const info = await fetchRechargePayOptions({});
		const list = Array.isArray(info) ? info : readAdminList(info as any);
		state.payOptions = list as any[];
	} catch (e: any) {
		state.payOptions = [];
		showCaughtError(e, '加载渠道失败');
	}
};

const onAutoRefreshChange = (on: string | number | boolean) => {
	if (refreshTimer) {
		clearInterval(refreshTimer);
		refreshTimer = null;
	}
	if (on) {
		refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
	}
};

const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = { ...row };
	try {
		const info = (await fetchRechargeCheckInfo(Number(row.ID))) as Row;
		if (info && typeof info === 'object') state.detail = { ...row, ...info };
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};

const onAction = async (row: Row, kind: 'agree' | 'reject' | 'hang') => {
	const map = {
		agree: { url: '/admin/money/recharge/checkagree', tip: '确认要通过该笔充值订单吗？' },
		reject: { url: '/admin/money/recharge/checkreject', tip: '确认要拒绝该笔充值订单吗？' },
		hang: { url: '/admin/money/recharge/checkprocessing', tip: '设为处理中，不再声音提醒，确认吗？' },
	} as const;
	const conf = map[kind];
	try {
		await ElMessageBox.confirm(conf.tip, '资金操作确认', { type: 'warning' });
		state.acting = true;
		await adminPost(conf.url, { ID: row.ID });
		ElMessage.success('已提交');
		state.detailVisible = false;
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel' && e?.toString?.() !== 'cancel') showCaughtError(e);
	} finally {
		state.acting = false;
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

onMounted(() => {
	void ensureCurrencyEnum();
	void loadCurrencies();
	loadPayOptions();
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
.st-none {
	color: #e6a23c;
}
.st-proc {
	color: #409eff;
}
.st-ok {
	color: #67c23a;
	font-weight: 600;
}
.st-fail {
	color: #f56c6c;
}
.check-actions {
	display: flex;
	gap: 8px;
	margin-top: 16px;
}
.money-in { color: #67c23a; }
</style>
