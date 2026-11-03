<template>
	<!-- 规格：Web.Admin/src/views/user/agent/usragentinfo.html -->
	<div class="user-agent-usragentinfo-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名称"><el-input v-model="state.query.UserName" clearable style="width: 140px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="上级代理"><el-input v-model="state.query.Agent" clearable style="width: 140px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="包含下级"><el-checkbox v-model="state.belongUser" /></el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="待审" value="0" />
						<el-option label="正常" value="1" />
						<el-option label="锁定" value="2" />
					</el-select>
				</el-form-item>
				<el-form-item label="添加时间">
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
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:agent:qiyueguanli:dailixinxi'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_status="{ row }">
						<el-tag size="small" :type="statusType(row.Status)">{{ statusLabel(row.Status) }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="代理信息" size="520px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
				<el-descriptions-item label="用户名称">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="所属上级">{{ state.detailRow.AgentName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="状态">{{ statusLabel(state.detailRow.Status) }}</el-descriptions-item>
				<el-descriptions-item label="下级会员数">{{ state.detailRow.Member ?? 0 }}</el-descriptions-item>
				<el-descriptions-item label="佣金方案">{{ state.detailRow.Mode || '-' }}</el-descriptions-item>
				<el-descriptions-item label="返点方案">{{ state.detailRow.ReturnMode || '-' }}</el-descriptions-item>
				<el-descriptions-item label="邀请码">{{ state.detailRow.Invite || '-' }}</el-descriptions-item>
				<el-descriptions-item label="域名">{{ state.detailRow.Domain || '-' }}</el-descriptions-item>
				<el-descriptions-item label="禁入平台">{{ state.detailRow.BlockGame || '-' }}</el-descriptions-item>
				<el-descriptions-item label="添加时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
			</el-descriptions>
			<el-empty v-else description="无详情" />
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentUsragentinfo">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentUsragentinfo', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	const start = new Date();
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
	return [f(start), f(end)];
};
const todayRange = (): [string, string] => rangeDays(0);
const statusLabel = (v: unknown) => {
	const n = Number(v);
	if (n === 0) return '待审';
	if (n === 1) return '正常';
	if (n === 2) return '锁定';
	return v == null || v === '' ? '-' : String(v);
};
const statusType = (v: unknown) => {
	const n = Number(v);
	if (n === 1) return 'success';
	if (n === 2) return 'danger';
	if (n === 0) return 'warning';
	return 'info';
};
const state = reactive({
	query: { UserName: '', Agent: '', Status: '' },
	belongUser: false,
	timeRange: null as [string, string] | null,
	detailVisible: false,
	detailRow: null as Row | null,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentUsragentinfo',
		name: '代理信息',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名称', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '所属上级', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Member', title: '下级会员数', width: 110, formatter: ({ cellValue }) => cellValue ?? 0 },
			{ field: 'Mode', title: '佣金方案', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'ReturnMode', title: '返点方案', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Invite', title: '邀请码', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Domain', title: '域名', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'BlockGame', title: '禁入平台', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '添加时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 100, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/user/agent/usragent/list', {
			UserName: state.query.UserName || '',
			Agent: state.query.Agent || '',
			BelongUser: state.belongUser ? '1' : '0',
			Status: state.query.Status || '',
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
	state.query.UserName = '';
	state.query.Agent = '';
	state.query.Status = '';
	state.belongUser = false;
	state.timeRange = null;
	handleQuery(true);
};
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
</script>
