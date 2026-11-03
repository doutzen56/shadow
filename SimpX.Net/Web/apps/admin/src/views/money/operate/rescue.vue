<template>
	<!-- 救援金发放：loglist + query/post -->
	<div class="money-operate-rescue-page admin-page">
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
					<el-date-picker v-model="state.query.Date" type="date" value-format="YYYY-MM-DD" clearable style="width: 150px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:kuanxiangguanli:jiuyuanjinfafang'" @click="handleQuery(true)">查询</el-button>
				<el-button @click="state.query.Date = today(); handleQuery(true)">今日</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:money:kuanxiangguanli:jiuyuanjinfafang'" @click="openPreview">预览发放</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_reward="{ row }">
						<span class="money-in">{{ formatMoney2(row.Reward) }}</span>
					</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="formatMoneyCheckStatus(row.Status) === '通过' ? 'success' : formatMoneyCheckStatus(row.Status) === '失败' ? 'danger' : formatMoneyCheckStatus(row.Status) === '待审' ? 'warning' : 'info'"
						>
							{{ formatMoneyCheckStatus(row.Status) }}
						</el-tag>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.previewVisible" title="救援金预览发放" width="860px" destroy-on-close>
			<el-form :inline="true" class="query-form" @submit.prevent>
				<el-form-item label="日期">
					<el-date-picker v-model="state.previewDate" type="date" value-format="YYYY-MM-DD" style="width: 160px" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.previewUser" clearable style="width: 140px" placeholder="可空=全体亏损" />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" :loading="state.previewLoading" @click="loadPreview">预览</el-button>
				</el-form-item>
			</el-form>
			<el-table :data="state.previewRows" size="small" border max-height="360" @selection-change="onPreviewSelect">
				<el-table-column type="selection" width="48" />
				<el-table-column prop="UserName" label="用户" min-width="110" :formatter="(_, __, v) => v || '-'" />
				<el-table-column prop="EventName" label="活动" min-width="120" :formatter="(_, __, v) => v || '-'" />
				<el-table-column prop="Currency" label="币种" width="80" :formatter="(_, __, v) => formatCurrency(v) || String(v ?? '-')" />
				<el-table-column prop="GameMoney" label="游戏盈亏" width="100">
					<template #default="{ row }">
						<span :class="Number(row.GameMoney) < 0 ? 'money-out' : 'money-in'">{{ formatMoney2(row.GameMoney) }}</span>
					</template>
				</el-table-column>
				<el-table-column prop="Money" label="救援金" width="100" :formatter="(_, __, v) => formatMoney2(v)" />
				<el-table-column prop="Flow" label="流水" width="90" :formatter="(_, __, v) => formatMoney2(v)" />
			</el-table>
			<template #footer>
				<el-button @click="state.previewVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.posting" :disabled="!state.previewSelected.length" @click="onPost">发放选中</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateRescue">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';
import { formatDateTime } from '/@/utils/format';
import { ensureCurrencyEnum, formatCurrency, formatMoney2, formatMoneyCheckStatus } from '/@/utils/admin-display';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyOperateRescue', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const pad = (n: number) => String(n).padStart(2, '0');
const today = () => {
	const d = new Date();
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};
const state = reactive({
	query: { User: '', Status: '', Currency: '', Date: today() },
	autoRefresh: false,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	previewVisible: false,
	previewDate: today(),
	previewUser: '',
	previewLoading: false,
	previewRows: [] as Row[],
	previewSelected: [] as Row[],
	posting: false,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyOperateRescue',
		name: '救援金发放',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'EventID', title: '活动', width: 90, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'Reward', title: '金额', minWidth: 110, slots: { default: 'row_reward' } },
			{ field: 'Flow', title: '流水倍', width: 90, formatter: ({ cellValue }) => formatMoney2(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'ErrorMsg', title: '备注', minWidth: 140, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
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
		const info = await adminPost('/admin/money/rescue/loglist', {
			User: state.query.User || '',
			Status: state.query.Status || '',
			Currency: state.query.Currency || '',
			Date: state.query.Date || '',
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
	state.query.Date = today();
	handleQuery(true);
};
const openPreview = () => {
	state.previewDate = today();
	state.previewUser = state.query.User || '';
	state.previewRows = [];
	state.previewSelected = [];
	state.previewVisible = true;
};
const loadPreview = async () => {
	if (!state.previewDate) {
		ElMessage.warning('请选择日期');
		return;
	}
	state.previewLoading = true;
	try {
		const info = await adminPost('/admin/money/rescue/query', {
			Date: state.previewDate,
			User: state.previewUser || '',
		});
		const list = Array.isArray((info as any)?.list) ? (info as any).list : Array.isArray(info) ? (info as any) : [];
		state.previewRows = list as Row[];
		state.previewSelected = [];
		if (!list.length) ElMessage.info('无可发放记录');
	} catch (e: any) {
		state.previewRows = [];
		showCaughtError(e);
	} finally {
		state.previewLoading = false;
	}
};
const onPreviewSelect = (rows: Row[]) => {
	state.previewSelected = rows || [];
};
const onPost = async () => {
	if (!state.previewSelected.length) {
		ElMessage.warning('请勾选要发放的记录');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认发放选中的 ${state.previewSelected.length} 笔救援金？`, '救援金发放', { type: 'warning' });
		state.posting = true;
		const payload: Record<string, string> = {};
		state.previewSelected.forEach((r, i) => {
			payload[String(i)] = JSON.stringify({
				UserID: r.UserID,
				EventID: r.EventID,
				Money: r.Money,
				Flow: r.Flow,
				Currency: r.Currency,
				Date: r.Date || state.previewDate,
			});
		});
		const body = (await adminPost('/admin/money/rescue/post', payload, { raw: true })) as any;
		ElMessage.success(body?.msg || '已提交发放');
		state.previewVisible = false;
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.posting = false;
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
.money-in {
	color: #67c23a;
}
.money-out {
	color: #f56c6c;
}
</style>
