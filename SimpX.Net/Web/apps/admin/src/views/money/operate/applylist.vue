<template>
	<!-- 规格：Web.Admin/src/views/money/operate/applylist.html → /admin/money/moneyreview/list|check -->
	<div class="money-operate-applylist-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
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
				<el-form-item label="会员">
					<el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="包含下级">
					<el-checkbox v-model="state.belongUser">查该用户全部下级</el-checkbox>
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="提交人">
					<el-input v-model="state.query.CreatorAdminName" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="审核人">
					<el-input v-model="state.query.ReviewerAdminName" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="流水号">
					<el-input v-model="state.query.SourceID" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
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
				<el-form-item label="类型">
					<el-select v-model="state.query.Type" clearable filterable placeholder="全部" style="width: 180px">
						<el-option-group v-for="g in state.typeGroups" :key="g.label" :label="g.label">
							<el-option v-for="o in g.options" :key="o.value" :label="o.label" :value="o.value" />
						</el-option-group>
					</el-select>
				</el-form-item>
				<el-form-item label="方向">
					<el-select v-model="state.query.Action" clearable placeholder="全部" style="width: 110px">
						<el-option label="加款" value="1" />
						<el-option label="扣款" value="2" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="state.timeRange = todayRange(); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:kuanxiangguanli:jiajiankuanshenhe'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain :loading="state.batching" @click="onBatch(true)">批量通过</el-button>
				<el-button type="danger" plain :loading="state.batching" @click="onBatch(false)">批量拒绝</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_money="{ row }">
						<span :class="Number(row.Money) < 0 ? 'money-out' : 'money-in'">{{ formatMoney(row.Money) }}</span>
						<span v-if="row.Flow" class="muted" :title="`${row.Flow}倍流水`"> ·{{ row.Flow }}倍</span>
					</template>
					<template #row_action="{ row }">
						<el-tag size="small" :type="actionLabel(row.Action) === '加款' ? 'success' : actionLabel(row.Action) === '扣款' ? 'danger' : 'info'">
							{{ actionLabel(row.Action) }}
						</el-tag>
					</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="
								formatMoneyCheckStatus(row.Status ?? row.StatusValue) === '通过'
									? 'success'
									: formatMoneyCheckStatus(row.Status ?? row.StatusValue) === '失败'
										? 'danger'
										: formatMoneyCheckStatus(row.Status ?? row.StatusValue) === '待审'
											? 'warning'
											: 'info'
							"
						>
							{{ formatMoneyCheckStatus(row.Status ?? row.StatusValue) }}
						</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<template v-if="isPending(row)">
							<el-button text type="success" @click="onCheck(row, true)">通过</el-button>
							<el-button text type="danger" @click="onCheck(row, false)">拒绝</el-button>
						</template>
						<span v-else class="muted">-</span>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateApplylist">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum, formatCurrency, formatMoney2, formatMoneyCheckStatus } from '/@/utils/admin-display';
import {
	DEFAULT_MONEY_TYPE_GROUPS,
	formatMoneyTypeLabel,
	normalizeMoneyTypeGroups,
	type MoneyTypeGroup,
} from '/@/utils/admin-money-type';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyOperateApplylist', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
	return [f(start), f(end)];
};
const todayRange = (): [string, string] => rangeDays(0);
const state = reactive({
	query: { User: '', Agent: '', CreatorAdminName: '', ReviewerAdminName: '', SourceID: '', Status: 'None', Currency: '', Type: '', Action: '' },
	belongUser: false,
	timeRange: todayRange() as [string, string] | null,
	autoRefresh: false,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	typeGroups: DEFAULT_MONEY_TYPE_GROUPS as MoneyTypeGroup[],
	batching: false,
	localPageParam,
});

const formatMoney = formatMoney2;
/** 对齐 BE MoneyReviewService：ActionIn=1 加款 / ActionOut=2 扣款 */
const actionLabel = (v: unknown) => {
	const a = String(v ?? '').trim();
	if (a === '1' || /^add|in|加/i.test(a)) return '加款';
	if (a === '2' || /^sub|out|扣|减/i.test(a)) return '扣款';
	return a || '-';
};
const isPending = (row: Row) => {
	const s = String(row.Status ?? row.StatusValue ?? '');
	return s === 'None' || s === '0' || formatMoneyCheckStatus(s) === '待审';
};

const options = useVxeTable<Row>(
	{
		id: 'siteMoneyOperateApplylist',
		name: '加减款审核',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '申请时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue, row }) => formatCurrency(cellValue ?? row.CurrencyCode) },
			{ field: 'Money', title: '申请金额', minWidth: 110, slots: { default: 'row_money' } },
			{ field: 'MinFlow', title: '最低流水', width: 90, formatter: ({ cellValue }) => formatMoney(cellValue) },
			{ field: 'Flow', title: '流水要求(倍)', width: 110, formatter: ({ cellValue }) => formatMoney(cellValue) },
			{
				field: 'TypeName',
				title: '类型',
				minWidth: 110,
				formatter: ({ cellValue, row }) => cellValue || formatMoneyTypeLabel(row.Type) || '-',
			},
			{ field: 'Action', title: '方向', width: 90, slots: { default: 'row_action' } },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Creator', title: '提交人', minWidth: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Admin', title: '审核人', minWidth: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'ReviewAt', title: '审核时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'SourceID', title: '流水号', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'PayName', title: '充值渠道', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Description', title: '前端备注', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'DescriptionA', title: '后端备注', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
		checkboxConfig: { highlight: true },
	}
);

const handleQuery = useGridQuery(xGrid, options);
let refreshTimer: ReturnType<typeof setInterval> | null = null;
const onAutoRefresh = (on: boolean) => {
	if (refreshTimer) {
		clearInterval(refreshTimer);
		refreshTimer = null;
	}
	if (on) refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
};

const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() =>
		adminPost('/admin/money/moneyreview/list', {
			User: state.query.User || '',
			BelongUser: state.belongUser ? '1' : '0',
			Agent: state.query.Agent || '',
			CreatorAdminName: state.query.CreatorAdminName || '',
			ReviewerAdminName: state.query.ReviewerAdminName || '',
			SourceID: state.query.SourceID || '',
			Status: state.query.Status || '',
			Currency: state.query.Currency || '',
			Type: state.query.Type || '',
			Action: state.query.Action || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
};

const resetQuery = () => {
	state.query.User = '';
	state.belongUser = false;
	state.query.Agent = '';
	state.query.CreatorAdminName = '';
	state.query.ReviewerAdminName = '';
	state.query.SourceID = '';
	state.query.Action = '';
	state.query.Status = 'None';
	state.query.Currency = '';
	state.query.Type = '';
	state.timeRange = todayRange();
	handleQuery(true);
};

const onCheck = async (row: Row, ok: boolean) => {
	try {
		await ElMessageBox.confirm(ok ? '确认通过该笔加减款？' : '确认拒绝？', '资金审核', { type: 'warning' });
		await adminPost('/admin/money/moneyreview/check', {
			ID: row.ID,
			Status: ok ? 'Success' : 'Faild',
		});
		ElMessage.success('已处理');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

const onBatch = async (ok: boolean) => {
	const rows = ((xGrid.value?.getCheckboxRecords?.() as Row[]) || []).filter(isPending);
	if (!rows.length) {
		ElMessage.warning('请勾选待审记录');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认对 ${rows.length} 笔${ok ? '批量通过' : '批量拒绝'}？`, '资金审核', { type: 'warning' });
		state.batching = true;
		const ids = rows.map((r) => r.ID).join(',');
		await adminPost(ok ? '/admin/money/moneyreview/batchsuccess' : '/admin/money/moneyreview/batchfail', { ID: ids, IDs: ids });
		ElMessage.success('已处理');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.batching = false;
	}
};

onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
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
	try {
		const info = await adminPost('/admin/config/moneytype', {});
		state.typeGroups = normalizeMoneyTypeGroups(info);
	} catch (e: any) {
		state.typeGroups = DEFAULT_MONEY_TYPE_GROUPS;
		showCaughtError(e, '加载帐变类型失败');
	}
});
</script>

<style scoped>
.muted {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}
.money-in {
	color: #67c23a;
}
.money-out {
	color: #f56c6c;
}
</style>
