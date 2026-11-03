<template>
	<!-- 佣金发放：/admin/money/commission/loglist -->
	<div class="money-operate-commission-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="待审" value="None" />
						<el-option label="失败" value="Faild" />
						<el-option label="通过" value="Success" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="日期">
					<el-date-picker
						v-model="state.timeRange"
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:kuanxiangguanli:yongjinfafang'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:money:kuanxiangguanli:yongjinfafang'" @click="openCreate">生成佣金</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_pmoney="{ row }">
						<span class="money-in">{{ formatMoney2(row.Money) }}</span>
					</template>

					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="formatMoneyCheckStatus(row.Status) === '通过' ? 'success' : formatMoneyCheckStatus(row.Status) === '失败' ? 'danger' : formatMoneyCheckStatus(row.Status) === '待审' ? 'warning' : 'info'"
						>
							{{ formatMoneyCheckStatus(row.Status) }}
						</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<template v-if="isPending(row)">
							<el-button text type="success" @click="onAction(row, true)">通过</el-button>
							<el-button text type="danger" @click="onAction(row, false)">拒绝</el-button>
						</template>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-dialog v-model="state.createVisible" title="生成待发佣金" width="420px" destroy-on-close>
			<el-form label-width="90px">
				<el-form-item label="起止日期" required>
					<el-date-picker v-model="state.createRange" type="daterange" value-format="YYYY-MM-DD" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 100%" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.createVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.creating" @click="onCreate">生成</el-button>
			</template>
		</el-dialog>
		<el-drawer v-model="state.detailVisible" title="佣金详情" size="520px" destroy-on-close>
			<el-descriptions v-if="state.detailObj" :column="1" border size="small">
				<el-descriptions-item label="游戏盈亏">
					<span :class="Number(state.detailObj.GameMoney) < 0 ? 'money-out' : 'money-in'">{{ formatMoney2(state.detailObj.GameMoney) }}</span>
				</el-descriptions-item>
				<el-descriptions-item label="活动金额">{{ formatMoney2(state.detailObj.MoneyEvent) }}</el-descriptions-item>
				<el-descriptions-item label="返水金额">{{ formatMoney2(state.detailObj.MoneyRebate) }}</el-descriptions-item>
				<el-descriptions-item label="充提手续费">{{ formatMoney2(state.detailObj.Cost) }}</el-descriptions-item>
				<el-descriptions-item label="活动系数">{{ state.detailObj.Award ?? '-' }}</el-descriptions-item>
				<el-descriptions-item label="返水系数">{{ state.detailObj.Return ?? '-' }}</el-descriptions-item>
				<el-descriptions-item label="佣金比例">{{ state.detailObj.Rate ?? '-' }}</el-descriptions-item>
				<el-descriptions-item label="佣金金额">{{ formatMoney2(state.detailObj.Money) }}</el-descriptions-item>
				<el-descriptions-item label="币种">{{ formatCurrency(state.detailObj.Currency) || String(state.detailObj.Currency ?? '-') }}</el-descriptions-item>
			</el-descriptions>
			<pre v-else-if="state.detail" class="detail-pre">{{ state.detail }}</pre>
			<el-empty v-else description="无明细" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateCommission">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { formatDateTime } from '/@/utils/format';
import { ensureCurrencyEnum, formatCurrency, formatMoney2, formatMoneyCheckStatus } from '/@/utils/admin-display';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyOperateCommission', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const pad = (n: number) => String(n).padStart(2, '0');
const dayStr = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	return [dayStr(start), dayStr(end)];
};
const state = reactive({
	query: { User: '', Status: '', Currency: '' },
	autoRefresh: false,
	timeRange: rangeDays(0) as [string, string] | null,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	detailVisible: false,
	detail: '',
	detailObj: null as Record<string, any> | null,
	createVisible: false,
	createRange: null as [string, string] | null,
	creating: false,
	localPageParam,
});
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const isPending = (row: Row) => {
	const s = String(row.Status ?? '');
	return s === 'None' || s === '0' || formatMoneyCheckStatus(s) === '待审';
};
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyOperateCommission',
		name: '佣金发放',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'Money', title: '金额', minWidth: 110, slots: { default: 'row_pmoney' } },
			{ field: 'StartDate', title: '起', minWidth: 110, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'EndDate', title: '止', minWidth: 110, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 200, fixed: 'right', slots: { default: 'row_buttons' } },
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
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/money/commission/loglist', {
			User: state.query.User || '',
			Status: state.query.Status || '',
			Currency: state.query.Currency || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		});
		if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in (info as object)) return info as any;
		const list = unwrapAdminList<Row>(info);
		return { list, RecordCount: list.length };
	});
const resetQuery = () => {
	state.query.User = '';
	state.query.Status = '';
	state.query.Currency = '';
	state.timeRange = rangeDays(0);
	handleQuery(true);
};
const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = '';
	state.detailObj = null;
	try {
		const info = await adminPost('/admin/money/commission/logdetail', { ID: row.ID });
		const detail = (info as any)?.Detail ?? (info as any)?.detail ?? info;
		if (detail && typeof detail === 'object' && !Array.isArray(detail)) {
			state.detailObj = detail as Record<string, any>;
			return;
		}
		if (typeof detail === 'string') {
			const raw = detail.trim();
			if (raw.startsWith('{')) {
				try {
					state.detailObj = JSON.parse(raw);
					return;
				} catch {
					/* fallthrough */
				}
			}
			state.detail = raw;
			return;
		}
		state.detail = detail != null ? JSON.stringify(detail, null, 2) : '';
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};
const openCreate = () => {
	const d = new Date();
	const p = (n: number) => String(n).padStart(2, '0');
	const day = `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
	state.createRange = [day, day];
	state.createVisible = true;
};
const onCreate = async () => {
	if (!state.createRange?.[0] || !state.createRange?.[1]) {
		ElMessage.warning('请选择起止日期');
		return;
	}
	try {
		await ElMessageBox.confirm('确认生成该区间待发佣金？', '生成佣金', { type: 'warning' });
		state.creating = true;
		const body = (await adminPost(
			'/admin/money/commission/create',
			{
				StartDate: state.createRange[0],
				EndDate: state.createRange[1],
				StartAt: state.createRange[0],
				EndAt: state.createRange[1],
			},
			{ raw: true }
		)) as any;
		ElMessage.success(body?.msg || '已提交');
		state.createVisible = false;
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.creating = false;
	}
};
const onAction = async (row: Row, ok: boolean) => {
	try {
		await ElMessageBox.confirm(ok ? '确认通过该笔佣金？' : '确认拒绝该笔佣金？', '佣金发放', { type: 'warning' });
		await adminPost(ok ? '/admin/money/commission/logagree' : '/admin/money/commission/logreject', { ID: row.ID });
		ElMessage.success('已处理');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};


onUnmounted(() => { if (refreshTimer) clearInterval(refreshTimer); });
onMounted(async () => {
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
});
</script>

<style scoped>
.detail-pre { white-space: pre-wrap; font-size: 12px; margin: 0; }
.money-in { color: #67c23a; }
.money-out { color: #f56c6c; }
</style>
