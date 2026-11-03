<template>
	<!-- 规格：Web.Admin/src/views/user/agent/check.html -->
	<div class="user-agent-check-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="用户名"><el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 120px">
						<el-option label="待审" value="None" />
						<el-option label="通过" value="Success" />
						<el-option label="拒绝" value="Faild" />
					</el-select>
				</el-form-item>
				<el-form-item label="申请时间">
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
				<el-button @click="state.timeRange = todayRange(); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:agent:dailishenhe'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_phone="{ row }">{{ row.Phone ? `${row.Area || ''}${row.Phone}` : '-' }}</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="formatCheckStatus(row.Status) === '通过' ? 'success' : formatCheckStatus(row.Status) === '失败' || formatCheckStatus(row.Status) === '拒绝' ? 'danger' : formatCheckStatus(row.Status) === '待审' ? 'warning' : 'info'"
						>
							{{ formatCheckStatus(row.Status) === '失败' ? '拒绝' : formatCheckStatus(row.Status) }}
						</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<template v-if="isApplyPending(row)">
							<el-button text type="success" @click="onExec(row, 'Success')">通过</el-button>
							<el-button text type="danger" @click="onExec(row, 'Faild')">拒绝</el-button>
						</template>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="代理申请详情" size="480px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="用户名">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="状态">{{ formatCheckStatus(state.detailRow.Status) === '失败' ? '拒绝' : formatCheckStatus(state.detailRow.Status) }}</el-descriptions-item>
				<el-descriptions-item label="手机">{{ state.detailRow.Phone ? `${state.detailRow.Area || ''}${state.detailRow.Phone}` : '-' }}</el-descriptions-item>
				<el-descriptions-item label="Line">{{ state.detailRow.Line || '-' }}</el-descriptions-item>
				<el-descriptions-item label="QQ">{{ state.detailRow.QQ || '-' }}</el-descriptions-item>
				<el-descriptions-item label="WhatsApp">{{ state.detailRow.WhatsApp || '-' }}</el-descriptions-item>
				<el-descriptions-item label="Skype">{{ state.detailRow.Skype || '-' }}</el-descriptions-item>
				<el-descriptions-item label="Telegram">{{ state.detailRow.Telegram || '-' }}</el-descriptions-item>
				<el-descriptions-item label="申请时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
				<el-descriptions-item label="推广简介"><pre class="detail-pre">{{ state.detailRow.Content || '-' }}</pre></el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentCheck">
import { onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { execAgentApply, fetchAgentApplyList } from '/@/api/user/agent';
import { formatDateTime } from '/@/utils/format';
import { formatMoneyCheckStatus } from '/@/utils/admin-display';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

const formatCheckStatus = (s?: unknown) => formatMoneyCheckStatus(s as any);
const isApplyPending = (row: Record<string, any>) => {
	const s = String(row.Status ?? '');
	return s === 'None' || s === '0' || Number(row.Status) === 0 || formatCheckStatus(s) === '待审';
};

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentCheck', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const pad = (n: number) => String(n).padStart(2, '0');
const dayStr = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	return [dayStr(start), dayStr(end)];
};
const todayRange = (): [string, string] => rangeDays(0);
const emptyQuery = () => ({ User: '', Status: 'None' });
const state = reactive({
	query: emptyQuery(),
	timeRange: todayRange() as [string, string] | null,
	detailVisible: false,
	detailRow: null as Row | null,
	autoRefresh: false,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentCheck',
		name: '代理审核',
		columns: [
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Line', title: 'Line', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'QQ', title: 'QQ', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'WhatsApp', title: 'WhatsApp', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Phone', title: '手机号码', minWidth: 130, slots: { default: 'row_phone' } },
			{ field: 'Skype', title: 'Skype', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Telegram', title: 'Telegram', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Content', title: '推广简介', minWidth: 160, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '申请时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchAgentApplyList({
			...state.query,
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
let refreshTimer: ReturnType<typeof setInterval> | null = null;
const onAutoRefresh = (on: boolean) => {
	if (refreshTimer) {
		clearInterval(refreshTimer);
		refreshTimer = null;
	}
	if (on) refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
};
onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.timeRange = todayRange();
	handleQuery(true);
};
const onExec = async (row: Row, status: string) => {
	try {
		await ElMessageBox.confirm('确认要进行该操作吗？', '提示', { type: 'warning' });
		await execAgentApply(Number(row.ID), status);
		ElMessage.success('已处理');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
</script>

<style scoped>
.muted {
	color: var(--el-text-color-secondary);
}
</style>
<style scoped>
.detail-pre { margin: 0; white-space: pre-wrap; font-size: 12px; }
.muted { color: var(--el-text-color-secondary); }
</style>
