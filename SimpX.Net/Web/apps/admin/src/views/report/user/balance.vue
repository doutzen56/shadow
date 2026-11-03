<template>
	<!-- 规格：Web.Admin report/user/balance.html → /admin/report/user/balance（按币种汇总） -->
	<div class="report-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:report:user:balance'" @click="handleQuery(true)">刷新</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_total="{ row }">
						<span class="money-in">{{ formatMoney2(row.TotalMoney) }}</span>
					</template>
					<template #row_lock="{ row }">
						<span class="money-out">{{ formatMoney2(row.LockMoney) }}</span>
					</template>
					<template #row_avail="{ row }">
						<span class="money-in">{{ formatMoney2(Number(row.TotalMoney || 0) - Number(row.LockMoney || 0)) }}</span>
					</template>
					<template #row_unbal="{ row }">
						<span :class="Number(row.Unbalanced) < 0 ? 'money-out' : 'money-in'">{{ formatMoney2(row.Unbalanced) }}</span>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteReportUserBalance">
import { onMounted, reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum, formatCurrency, formatMoney2 } from '/@/utils/admin-display';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteReportUserBalance', {
	pageSize: 50 as number,
	defaultSort: { field: 'Currency', order: 'asc', descStr: 'desc' },
});
const state = reactive({ localPageParam });
const options = useVxeTable<Row>(
	{
		id: 'siteReportUserBalance',
		name: '余额汇总',
		columns: [
			{ field: 'Currency', title: '币种', minWidth: 100, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'TotalMoney', title: '总余额', minWidth: 140, slots: { default: 'row_total' } },
			{ field: 'LockMoney', title: '锁定', minWidth: 120, slots: { default: 'row_lock' } },
			{ field: 'Available', title: '可用', minWidth: 120, slots: { default: 'row_avail' } },
			{ field: 'Unbalanced', title: '未结算', minWidth: 120, slots: { default: 'row_unbal' } },
			{ field: 'UserCount', title: '人数', width: 90, formatter: ({ cellValue }) => cellValue ?? '-' },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() => adminPost('/admin/report/user/balance', {}));
};
onMounted(() => {
	void ensureCurrencyEnum();
});
</script>
<style scoped>
.money-in { color: #67c23a; }
.money-out { color: #f56c6c; }
</style>
