<template>
	<!-- 返水发放：/admin/money/rebate/loglist -->
	<div class="money-operate-rebate-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
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
					<el-select v-model="state.query.Type" clearable placeholder="全部" style="width: 110px">
						<el-option label="自助" value="V2" />
						<el-option label="综合" value="AA" />
						<el-option label="分类1" value="A1" />
						<el-option label="分类2" value="A2" />
						<el-option label="分类3" value="A3" />
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:kuanxiangguanli:fanshuifafang'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:money:kuanxiangguanli:fanshuifafang'" @click="openCreate">生成返水</el-button>
			</div>
		</div>
		<el-dialog v-model="state.createVisible" title="生成待发返水" width="460px" destroy-on-close>
			<el-form label-width="90px">
				<el-form-item label="起止日期" required>
					<el-date-picker v-model="state.createRange" type="daterange" value-format="YYYY-MM-DD" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 100%" />
				</el-form-item>
				<el-form-item label="标题">
					<el-input v-model="state.createForm.Title" clearable placeholder="可选" />
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.createForm.Currency" clearable filterable placeholder="全部" style="width: 100%">
						<el-option v-for="c in state.currencyOptions" :key="'c-' + String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="流水倍数">
					<el-input v-model="state.createForm.FlowRate" clearable placeholder="0" style="width: 160px" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.createVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.creating" @click="onCreate">生成</el-button>
			</template>
		</el-dialog>
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
							<el-button text type="danger" @click="onReject(row)">拒绝</el-button>
						</template>
						<template v-else-if="isFailed(row)">
							<el-button text type="warning" @click="onReexecute(row)">重发</el-button>
						</template>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="返水详情" size="560px" destroy-on-close>
			<template v-if="state.detailRows.length">
				<el-table :data="state.detailRows" size="small" border max-height="480">
					<el-table-column prop="ID" label="编号" width="80" />
					<el-table-column prop="UserName" label="用户" min-width="100" :formatter="(_, __, v) => v || '-'" />
					<el-table-column prop="Money" label="金额" width="100" :formatter="(_, __, v) => formatMoney2(v)" />
					<el-table-column prop="Currency" label="币种" width="80" :formatter="(_, __, v) => formatCurrency(v) || String(v ?? '-')" />
					<el-table-column prop="Status" label="状态" width="80" :formatter="(_, __, v) => formatMoneyCheckStatus(v)" />
					<el-table-column prop="FaildMsg" label="备注" min-width="120" show-overflow-tooltip :formatter="(_, __, v) => v || '-'" />
				</el-table>
			</template>
			<pre v-else-if="state.detail" class="detail-pre">{{ state.detail }}</pre>
			<el-empty v-else description="无明细" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateRebate">
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
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyOperateRebate', {
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
	query: { User: '', Agent: '', Status: '', Currency: '', Type: '' },
	autoRefresh: false,
	timeRange: rangeDays(0) as [string, string] | null,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	detailVisible: false,
	detail: '',
	detailRows: [] as Row[],
	createVisible: false,
	creating: false,
	createRange: null as [string, string] | null,
	createForm: { Title: '', Currency: '', FlowRate: '0' },
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
const isFailed = (row: Row) => {
	const s = String(row.Status ?? '');
	return s === 'Failed' || s === 'Faild' || s === '1' || formatMoneyCheckStatus(s) === '失败';
};
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyOperateRebate',
		name: '返水发放',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'PlanID', title: '计划', width: 80, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '代理', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{
				field: 'Type',
				title: '类型',
				width: 90,
				formatter: ({ cellValue }) => {
					const t = String(cellValue ?? '');
					if (!t) return '-';
					if (/^自助|综合|分类/.test(t)) return t;
					if (/^v2$/i.test(t)) return '自助';
					if (/^aa$/i.test(t)) return '综合';
					const m = /^a(\d+)$/i.exec(t);
					if (m) return `分类${m[1]}`;
					return t;
				},
			},
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'Money', title: '金额', minWidth: 110, slots: { default: 'row_pmoney' } },
			{ field: 'StartAt', title: '起', minWidth: 110, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'EndAt', title: '止', minWidth: 110, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{
				field: 'FaildMsg',
				title: '备注',
				minWidth: 120,
				showOverflow: true,
				formatter: ({ row, cellValue }) => cellValue || row.ClaimNo || '-',
			},
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 160, fixed: 'right', slots: { default: 'row_buttons' } },
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
		const info = await adminPost('/admin/money/rebate/loglist', {
			User: state.query.User || '',
			Agent: state.query.Agent || '',
			Status: state.query.Status || '',
			Currency: state.query.Currency || '',
			Type: state.query.Type || '',
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
	state.query.Agent = '';
	state.query.Status = '';
	state.query.Currency = '';
	state.query.Type = '';
	state.timeRange = rangeDays(0);
	handleQuery(true);
};
const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = '';
	state.detailRows = [];
	try {
		const info = await adminPost('/admin/money/rebate/logdetail', { ID: row.PlanID || row.ID, PlanID: row.PlanID || 0, LogID: row.ID });
		const list = Array.isArray(info) ? info : Array.isArray((info as any)?.list) ? (info as any).list : null;
		if (list) state.detailRows = list as Row[];
		else state.detail = JSON.stringify(info, null, 2);
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};
const openCreate = () => {
	const day = dayStr(new Date());
	state.createRange = [day, day];
	state.createForm = { Title: '', Currency: state.query.Currency || '', FlowRate: '0' };
	state.createVisible = true;
};
const onCreate = async () => {
	if (!state.createRange?.[0] || !state.createRange?.[1]) {
		ElMessage.warning('请选择起止日期');
		return;
	}
	try {
		await ElMessageBox.confirm('确认生成该区间待发返水？', '生成返水', { type: 'warning' });
		state.creating = true;
		const body = (await adminPost(
			'/admin/money/rebate/create',
			{
				Title: state.createForm.Title || '',
				Name: state.createForm.Title || '',
				StartAt: state.createRange[0],
				EndAt: state.createRange[1],
				Currency: state.createForm.Currency || '',
				FlowRate: state.createForm.FlowRate || '0',
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
const onReject = async (row: Row) => {
	if (!row.PlanID) {
		ElMessage.warning('缺少计划编号，无法拒绝');
		return;
	}
	try {
		await ElMessageBox.confirm('确认拒绝该笔返水？', '返水发放', { type: 'warning' });
		await adminPost('/admin/money/rebate/reject', { PlanID: row.PlanID, LogID: row.ID });
		ElMessage.success('已拒绝');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
const onReexecute = async (row: Row) => {
	if (!row.PlanID) {
		ElMessage.warning('缺少计划编号，无法重发');
		return;
	}
	try {
		await ElMessageBox.confirm('确认重发该笔返水？', '返水发放', { type: 'warning' });
		await adminPost('/admin/money/rebate/reextcute', { PlanID: row.PlanID, LogID: row.ID });
		ElMessage.success('已提交重发');
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
</style>
