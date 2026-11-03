<template>
	<!-- 规格：Web.Admin/src/views/user/agent/domain.html -->
	<div class="user-agent-domain-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名"><el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="邀请码"><el-input v-model="state.query.Invite" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="代理域名"><el-input v-model="state.query.Domain" clearable style="width: 160px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:agent:agent-domain'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:user:agent:agent-domain'" @click="openEdit()">添加域名</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改域名' : '添加域名'" width="520px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="100px">
				<el-form-item label="域名" required>
					<el-input v-model="state.form.Name" placeholder="不含协议，如 a.example.com" />
				</el-form-item>
				<el-form-item label="邀请码" required>
					<el-input v-model="state.form.InviteID" />
				</el-form-item>
				<el-form-item label="站点名称">
					<el-input v-model="state.form.SiteName" />
				</el-form-item>
				<el-form-item label="默认背景">
					<el-input v-model="state.form.DefaultBackground" />
				</el-form-item>
				<el-form-item label="默认版型">
					<el-input v-model="state.form.DefaultTemplate" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
		<el-drawer v-model="state.detailVisible" title="代理域名" size="480px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
				<el-descriptions-item label="上级">{{ state.detailRow.UpUserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="用户">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="域名">{{ state.detailRow.Name || '-' }}</el-descriptions-item>
				<el-descriptions-item label="站点名称">{{ state.detailRow.SiteName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="默认背景">{{ state.detailRow.DefaultBackground || '-' }}</el-descriptions-item>
				<el-descriptions-item label="邀请码">{{ state.detailRow.InviteID || '-' }}</el-descriptions-item>
				<el-descriptions-item label="注册人数">{{ state.detailRow.MemberCount ?? 0 }}</el-descriptions-item>
				<el-descriptions-item label="添加时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentDomain">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { deleteAgentDomain, fetchAgentDomainInfo, fetchAgentDomainList, saveAgentDomain } from '/@/api/user/agent';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentDomain', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	end.setHours(23, 59, 59, 0);
	const start = new Date();
	start.setHours(0, 0, 0, 0);
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) =>
		`${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const emptyQuery = () => ({ User: '', Invite: '', Domain: '' });
const emptyForm = () => ({ ID: 0, Name: '', InviteID: '', SiteName: '', DefaultBackground: '', DefaultTemplate: '', Logo: '' });
const state = reactive({
	query: emptyQuery(),
	detailVisible: false,
	detailRow: null as Row | null,
	timeRange: null as [string, string] | null,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	localPageParam,
});

const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentDomain',
		name: '代理域名',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UpUserName', title: '上级', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'DefaultBackground', title: '默认背景', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'SiteName', title: '站点名称', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Name', title: '域名', minWidth: 160, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'MemberCount', title: '注册人数', width: 100, formatter: ({ cellValue }) => cellValue ?? 0 },
			{ field: 'CreateAt', title: '添加时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'InviteID', title: '邀请码', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

/** 查询 */
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchAgentDomainList({
			...state.query,
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.timeRange = null;
	handleQuery(true);
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除该域名吗？', '提示', { type: 'warning' });
		await deleteAgentDomain(Number(row.ID));
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/**
 * 打开添加/编辑。
 * @param row 行
 */
const openEdit = async (row?: Row) => {
	Object.assign(state.form, emptyForm());
	state.editVisible = true;
	if (!row?.ID) return;
	try {
		const info = (await fetchAgentDomainInfo(Number(row.ID))) as Row;
		Object.assign(state.form, {
			ID: Number(info?.ID ?? row.ID),
			Name: String(info?.Name ?? row.Name ?? ''),
			InviteID: String(info?.InviteID ?? row.InviteID ?? ''),
			SiteName: String(info?.SiteName ?? row.SiteName ?? ''),
			DefaultBackground: String(info?.DefaultBackground ?? row.DefaultBackground ?? ''),
			DefaultTemplate: String(info?.DefaultTemplate ?? ''),
			Logo: String(info?.Logo ?? ''),
		});
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};

/** 保存 */
const onSave = async () => {
	if (!state.form.Name?.trim() || !state.form.InviteID?.trim()) {
		ElMessage.warning('请填写域名和邀请码');
		return;
	}
	state.saving = true;
	try {
		await saveAgentDomain({
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			InviteID: state.form.InviteID.trim(),
			SiteName: state.form.SiteName || '',
			DefaultBackground: state.form.DefaultBackground || '',
			DefaultTemplate: state.form.DefaultTemplate || '',
			Logo: state.form.Logo || '',
		});
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};
</script>
