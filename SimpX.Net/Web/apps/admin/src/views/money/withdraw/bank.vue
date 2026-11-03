<template>
	<!-- 规格：Web.Admin money/withdraw/bank.html → getbanklist + savesitebank / savesitecoin -->
	<div class="money-withdraw-bank-page admin-page">
		<div class="query-toolbar">
			<el-radio-group v-model="state.kind" @change="() => handleQuery(true)">
				<el-radio-button value="bank">提现银行</el-radio-button>
				<el-radio-button value="coin">虚拟币</el-radio-button>
			</el-radio-group>
			<el-input v-model="state.keyword" clearable placeholder="名称筛选" style="width: 180px; margin-left: 12px" @keyup.enter="handleQuery(true)" />
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:tixianguanli:tixianyinhang'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_open="{ row }">
						<el-switch :model-value="!!row.IsOpen" @change="(v) => onToggle(row, !!v)" />
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteMoneyWithdrawBank">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchMemberBankCountryList, unwrapBankTypeOptions } from '/@/api/user/member-bank';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = { Name: string; Type?: number; IsOpen: boolean };
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyWithdrawBank', {
	pageSize: 200 as number,
	defaultSort: { field: 'Name', order: 'asc', descStr: 'desc' },
});
const state = reactive({ kind: 'bank' as 'bank' | 'coin', keyword: '', localPageParam });
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyWithdrawBank',
		name: '提现银行',
		columns: [
			{ field: 'Type', title: '编号', width: 90, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : cellValue) },
			{ field: 'Name', title: '名称', minWidth: 200, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'IsOpen', title: '启用', width: 120, slots: { default: 'row_open' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

const parseSelected = (info: any, field: 'Bank' | 'Coin'): Set<string> => {
	const set = new Set<string>();
	const raw = info?.[field];
	if (!raw) return set;
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : raw;
		if (obj && typeof obj === 'object') {
			for (const k of Object.keys(obj)) set.add(String(obj[k] || k));
		}
	} catch {
		String(raw)
			.split(',')
			.map((s) => s.trim())
			.filter(Boolean)
			.forEach((s) => set.add(s));
	}
	return set;
};

const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const [selectedInfo, catalogInfo] = await Promise.all([
			adminPost('/admin/site/config/getbanklist', {}),
			fetchMemberBankCountryList().catch(() => null),
		]);
		const isCoin = state.kind === 'coin';
		const selected = parseSelected(selectedInfo, isCoin ? 'Coin' : 'Bank');
		const catalog = unwrapBankTypeOptions(catalogInfo).filter((x) => {
			if (!x.Bank) return false;
			return isCoin ? x.Type >= 250 && x.Type <= 299 : x.Type < 200;
		});
		const list: Row[] =
			catalog.length > 0
				? catalog.map((x) => ({ Type: x.Type, Name: x.Bank, IsOpen: selected.has(x.Bank) || selected.has(String(x.Type)) }))
				: [...selected].map((name) => ({ Name: name, IsOpen: true }));
		list.sort((a, b) => Number(b.IsOpen) - Number(a.IsOpen) || a.Name.localeCompare(b.Name, 'zh'));
		const kw = state.keyword.trim().toLowerCase();
		const filtered = kw ? list.filter((r) => String(r.Name || '').toLowerCase().includes(kw) || String(r.Type || '').includes(kw)) : list;
		return { list: filtered, RecordCount: filtered.length };
	});

const resetQuery = () => {
	state.keyword = '';
	handleQuery(true);
};

const onToggle = async (row: Row, value: boolean) => {
	try {
		if (state.kind === 'coin') {
			await adminPost('/admin/site/config/savesitecoin', { Coin: row.Name, Value: value ? 1 : 0 });
		} else {
			await adminPost('/admin/site/config/savesitebank', { Bank: row.Name, Value: value ? 1 : 0 });
		}
		row.IsOpen = value;
		ElMessage.success(value ? '已启用' : '已关闭');
	} catch (e: any) {
		showCaughtError(e);
		handleQuery(false);
	}
};
</script>
