<template>
	<!-- 规格：Web.Admin report/user/moneylog.html → /admin/report/money/log -->
	<div class="report-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable placeholder="用户ID/账号" style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="包含下级">
					<el-checkbox v-model="state.belongUser">查该用户全部下级帐变</el-checkbox>
				</el-form-item>
				<el-form-item label="帐变类型">
					<el-select v-model="state.query.Type" clearable filterable placeholder="全部类型" style="width: 220px">
						<el-option-group v-for="g in state.typeGroups" :key="g.label" :label="g.label">
							<el-option
								:value="g.options.map((o) => o.value).join(',')"
								:label="`${g.label}（全部）`"
							/>
							<el-option v-for="o in g.options" :key="o.value" :label="o.label" :value="o.value" />
						</el-option-group>
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="帐变时间">
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
				<el-form-item label="备注">
					<el-input v-model="state.query.Content" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="流水号">
					<el-input v-model="state.query.SourceID" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="state.timeRange = rangeDays(0); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:report:user:moneylog'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据（默认可查今日）" /></template>
					<template #row_money="{ row }">
						<span :class="Number(row.Money) < 0 ? 'money-out' : 'money-in'">{{ formatMoney(row.Money) }}</span>
					</template>
					<template #row_lock="{ row }">
						<span class="money-out">{{ formatMoney(row.LockMoney) }}</span>
					</template>
					<template #row_bal="{ row }">
						<span class="money-in">{{ formatMoney(row.Balance) }}</span>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteReportUserMoneylog">
import { onMounted, reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency, formatMoney2 } from '/@/utils/admin-display';
import { showCaughtError } from '/@/utils/axios-utils';
import {
	DEFAULT_MONEY_TYPE_GROUPS,
	formatMoneyTypeLabel,
	normalizeMoneyTypeGroups,
	type MoneyTypeGroup,
} from '/@/utils/admin-money-type';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteReportUserMoneylog', {
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
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const formatMoney = formatMoney2;

const state = reactive({
	query: { User: '', Type: '', Content: '', SourceID: '', Currency: '' },
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	belongUser: false,
	timeRange: rangeDays(0) as [string, string] | null,
	typeGroups: DEFAULT_MONEY_TYPE_GROUPS as MoneyTypeGroup[],
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteReportUserMoneylog',
		name: '会员帐变',
		columns: [
			{ field: 'UserName', title: '用户', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'UpUserName', title: '上级', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{
				field: 'Type',
				title: '帐变类型',
				minWidth: 120,
				formatter: ({ row, cellValue }) => formatMoneyTypeLabel(row.TypeValue ?? cellValue),
			},
			{ field: 'CreateAt', title: '帐变时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Money', title: '帐变金额', minWidth: 110, slots: { default: 'row_money' } },
			{ field: 'LockMoney', title: '当前锁定', minWidth: 110, slots: { default: 'row_lock' } },
			{ field: 'Balance', title: '账户余额', minWidth: 110, slots: { default: 'row_bal' } },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'SourceID', title: '流水号', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Description', title: '备注', minWidth: 160, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() =>
		adminPost('/admin/report/money/log', {
			User: state.query.User || '',
			BelongUser: state.belongUser ? '1' : '0',
			Type: state.query.Type || '',
			Content: state.query.Content || '',
			SourceID: state.query.SourceID || '',
			Currency: state.query.Currency || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
};

const resetQuery = () => {
	state.query.User = '';
	state.query.Type = '';
	state.query.Content = '';
	state.query.SourceID = '';
	state.query.Currency = '';
	state.belongUser = false;
	state.timeRange = rangeDays(0);
	handleQuery(true);
};

/** 加载帐变类型下拉（旧 /admin/config/moneytype） */
const loadMoneyTypes = async () => {
	try {
		const info = await adminPost('/admin/config/moneytype', {});
		state.typeGroups = normalizeMoneyTypeGroups(info);
	} catch (e: any) {
		state.typeGroups = DEFAULT_MONEY_TYPE_GROUPS;
		showCaughtError(e, '加载帐变类型失败');
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
	void loadMoneyTypes();
});
</script>

<style scoped>
.money-in {
	color: #67c23a;
}
.money-out {
	color: #f56c6c;
}
</style>
