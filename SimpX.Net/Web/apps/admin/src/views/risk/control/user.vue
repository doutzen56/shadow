<template>
	<!-- 规格：Web.Admin risk/control/user.html；批量处理 → /admin/risk/user/updatestatus；明细抽屉 -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="风险因素">
					<el-input v-model="state.query.Status" clearable placeholder="类型关键字" style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="日期">
					<el-date-picker
						v-model="state.dateRange"
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:control:user'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="warning" plain v-auth="'site:risk:control:user'" @click="onBatchHandle">批量处理</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_type="{ row }">
						<el-tag size="small" type="warning">{{ row.Type || '-' }}</el-tag>
					</template>
					<template #row_factors="{ row }">
						<span class="factors">{{ row.Context || row.Type || '-' }}</span>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">明细</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="风险会员明细" size="560px" destroy-on-close>
			<el-tabs v-model="state.detailTab" @tab-change="onDetailTabChange">
				<el-tab-pane label="绑定信息" name="info" />
				<el-tab-pane label="IP对比" name="ip" />
				<el-tab-pane label="同IP用户" name="ipdifuser" />
				<el-tab-pane label="同用户IP" name="userdifip" />
				<el-tab-pane label="登录" name="login" />
				<el-tab-pane label="资金" name="amount" />
			</el-tabs>
			<div v-loading="state.detailLoading" class="detail-body">
				<template v-if="state.detailTab === 'info'">
					<p class="detail-block">{{ state.detail.BindInfo || state.detailText || '暂无绑定异常说明' }}</p>
				</template>
				<template v-else-if="state.detailTab === 'ip'">
					<el-descriptions :column="1" border size="small">
						<el-descriptions-item label="前次登录">
							{{ state.detail.PreCreateAt || '-' }} {{ state.detail.PreAddress || '' }}
							<span v-if="state.detail.PreIP"> IP({{ state.detail.PreIP }})</span>
						</el-descriptions-item>
						<el-descriptions-item label="今日登录">
							{{ state.detail.CurCreateAt || '-' }} {{ state.detail.CurAddress || '' }}
							<span v-if="state.detail.CurIP"> IP({{ state.detail.CurIP }})</span>
						</el-descriptions-item>
					</el-descriptions>
				</template>
				<template v-else-if="state.detailTab === 'ipdifuser' || state.detailTab === 'userdifip'">
					<p class="detail-block">{{ state.detail.Context || state.detailText || '暂无' }}</p>
				</template>
				<template v-else-if="state.detailTab === 'login' || state.detailTab === 'amount'">
					<p class="detail-center">
						该会员于当天修改{{ state.detailTab === 'login' ? '登录' : '资金' }}密码
						<span class="danger">{{ state.detailTab === 'login' ? state.detail.LoginChangeCount : state.detail.AmountChangeCount }}</span>
						次
					</p>
					<ul v-if="(state.detail.ContextList || []).length" class="detail-list">
						<li v-for="(item, idx) in state.detail.ContextList" :key="idx">● {{ item }} 修改成功</li>
					</ul>
					<p class="detail-hint">修改{{ state.detailTab === 'login' ? '登录' : '资金' }}密码 &gt;= {{ state.detail.DefaultCount ?? '-' }} 次</p>
				</template>
				<pre v-else class="detail-pre">{{ state.detailText }}</pre>
			</div>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteRiskControlUser">
import { onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchRiskUserList } from '/@/api/risk/index';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskControlUser', {
	pageSize: 20 as number,
	defaultSort: { field: 'CreateAt', order: 'desc', descStr: 'desc' },
});
const today = () => {
	const d = new Date();
	const p = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
};
const emptyDetail = () => ({
	BindInfo: '',
	Context: '',
	PreCreateAt: '',
	PreAddress: '',
	PreIP: '',
	CurCreateAt: '',
	CurAddress: '',
	CurIP: '',
	LoginChangeCount: 0,
	AmountChangeCount: 0,
	DefaultCount: 0,
	ContextList: [] as string[],
});
const state = reactive({
	query: { User: '', Status: '' },
	dateRange: [today(), today()] as [string, string] | null,
	autoRefresh: false,
	detailVisible: false,
	detailLoading: false,
	detailTab: 'info',
	detailRiskId: 0,
	detailText: '',
	detail: emptyDetail(),
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteRiskControlUser',
		name: '风险会员',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'Date', title: '日期', width: 120, formatter: ({ cellValue }) => formatDateTime(cellValue)?.slice(0, 10) || '-' },
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '上级代理', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Level', title: 'VIP', width: 80, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'Type', title: '状态/类型', minWidth: 100, slots: { default: 'row_type' } },
			{ field: 'Context', title: '风险因素', minWidth: 200, slots: { default: 'row_factors' } },
			{ field: 'CreateAt', title: '最近触发时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'RegCreateAt', title: '会员注册时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 90, fixed: 'right', slots: { default: 'row_buttons' } },
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
onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchRiskUserList({
			User: state.query.User || '',
			StartAt: state.dateRange?.[0] || '',
			EndAt: state.dateRange?.[1] || '',
			// 空=全部；勿硬编码 '0'（会 Type.Contains('0') 误伤）
			Status: state.query.Status || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
const loadDetailTab = async () => {
	if (!state.detailRiskId) return;
	const pathMap: Record<string, string> = {
		info: '/admin/risk/user/info',
		ip: '/admin/risk/user/ip',
		ipdifuser: '/admin/risk/user/ipdifuser',
		userdifip: '/admin/risk/user/userdifip',
		login: '/admin/risk/user/login',
		amount: '/admin/risk/user/amount',
	};
	const url = pathMap[state.detailTab] || pathMap.info;
	state.detailLoading = true;
	state.detailText = '';
	Object.assign(state.detail, emptyDetail());
	try {
		const info = (await adminPost(url, { RiskID: state.detailRiskId, ID: state.detailRiskId })) as Record<string, any>;
		const ctx = info?.Context;
		const ctxList = Array.isArray(ctx) ? ctx.map((x: any) => String(x)) : [];
		Object.assign(state.detail, {
			BindInfo: String(info?.BindInfo ?? ''),
			Context: typeof ctx === 'string' ? ctx : '',
			PreCreateAt: String(info?.PreCreateAt ?? ''),
			PreAddress: String(info?.PreAddress ?? ''),
			PreIP: String(info?.PreIP ?? ''),
			CurCreateAt: String(info?.CurCreateAt ?? ''),
			CurAddress: String(info?.CurAddress ?? ''),
			CurIP: String(info?.CurIP ?? ''),
			LoginChangeCount: Number(info?.LoginChangeCount ?? 0),
			AmountChangeCount: Number(info?.AmountChangeCount ?? 0),
			DefaultCount: Number(info?.DefaultCount ?? 0),
			ContextList: ctxList,
		});
		if (info?.BindInfo) state.detailText = String(info.BindInfo);
		else if (typeof ctx === 'string') state.detailText = ctx;
		else state.detailText = JSON.stringify(info ?? {}, null, 2);
	} catch (e: any) {
		showCaughtError(e, '加载失败');
		state.detailText = '加载失败';
	} finally {
		state.detailLoading = false;
	}
};
const openDetail = async (row: Row) => {
	state.detailRiskId = Number(row.RiskID || row.ID || 0);
	state.detailTab = 'info';
	state.detailVisible = true;
	await loadDetailTab();
};
const onDetailTabChange = () => {
	void loadDetailTab();
};
const onBatchHandle = async () => {
	const rows = (xGrid.value?.getCheckboxRecords?.() || []) as Row[];
	if (!rows.length) {
		ElMessage.warning('请先勾选要处理的记录');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认处理选中的 ${rows.length} 条风险记录？（将移出列表）`, '提示', { type: 'warning' });
		const ids = rows.map((r) => Number(r.RiskID || r.ID)).filter((n) => n > 0);
		await adminPost('/admin/risk/user/updatestatus', { IDs: ids.join(',') });
		ElMessage.success('已处理');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
const setRangeDays = (days: number) => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const p = (n: number) => String(n).padStart(2, '0');
	const f = (d: Date) => `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
	state.dateRange = [f(start), f(end)];
	handleQuery(true);
};
const resetQuery = () => {
	state.query.User = '';
	state.query.Status = '';
	state.dateRange = [today(), today()];
	handleQuery(true);
};
</script>

<style scoped>
.factors {
	font-size: 12px;
	word-break: break-all;
}
.detail-body {
	min-height: 160px;
	padding: 8px 4px 16px;
}
.detail-block {
	white-space: pre-wrap;
	word-break: break-word;
	line-height: 1.6;
	font-size: 13px;
}
.detail-center {
	text-align: center;
	font-size: 13px;
	margin-bottom: 8px;
}
.detail-list {
	list-style: none;
	padding: 0;
	margin: 0 0 12px;
	font-size: 12px;
	line-height: 1.7;
}
.detail-hint {
	text-align: center;
	color: var(--el-color-warning);
	font-size: 12px;
}
.danger {
	color: var(--el-color-danger);
	font-weight: 600;
	margin: 0 4px;
}
.detail-pre {
	white-space: pre-wrap;
	font-size: 12px;
}
</style>
