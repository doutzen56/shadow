<template>
	<!-- 规格：Web.Admin/src/views/user/agent/invite.html -->
	<div class="user-agent-invite-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="邀请码"><el-input v-model="state.query.Invite" clearable style="width: 140px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="用户名"><el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="账号类型">
					<el-select v-model="state.query.Type" clearable placeholder="全部" style="width: 110px">
						<el-option label="会员" value="Member" />
						<el-option label="代理" value="Agent" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.IsOpen" clearable placeholder="全部" style="width: 110px">
						<el-option label="开放" value="1" />
						<el-option label="关闭" value="0" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:agent:invite'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:user:agent:invite'" @click="openEdit()">新增邀请码</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_type="{ row }">
						<el-tag size="small" :type="String(row.Type) === '1' || row.Type === 'Agent' || row.Type === '代理' ? 'warning' : 'info'">
							{{
								String(row.Type) === '1' || row.Type === 'Agent' || row.Type === '代理'
									? '代理'
									: String(row.Type) === '0' || row.Type === 'Member' || row.Type === '会员'
										? '会员'
										: row.Type || '-'
							}}
						</el-tag>
					</template>
					<template #row_open="{ row }">
						<el-switch
							:model-value="!!row.IsOpen"
							:loading="state.toggleId === String(row.ID)"
							@change="(v) => onToggleOpen(row, !!v)"
						/>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改邀请码' : '新增邀请码'" width="480px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="100px">
				<el-form-item v-if="!state.form.ID" label="用户名" required>
					<el-input v-model="state.form.User" placeholder="归属用户名" />
				</el-form-item>
				<el-form-item v-if="state.form.ID" label="邀请码">
					<el-input :model-value="state.form.ID" readonly />
				</el-form-item>
				<el-form-item label="是否开放">
					<el-switch v-model="state.form.IsOpen" />
				</el-form-item>
				<el-form-item label="游戏串">
					<el-input v-model="state.form.Game" type="textarea" :rows="3" placeholder="可选，旧站 Game 字段" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
		<el-drawer v-model="state.detailVisible" title="邀请码详情" size="420px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="邀请码">{{ state.detailRow.ID || '-' }}</el-descriptions-item>
				<el-descriptions-item label="用户名">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="账号类型">{{ String(state.detailRow.Type) === '1' || state.detailRow.Type === 'Agent' || state.detailRow.Type === '代理' ? '代理' : String(state.detailRow.Type) === '0' || state.detailRow.Type === 'Member' || state.detailRow.Type === '会员' ? '会员' : (state.detailRow.Type || '-') }}</el-descriptions-item>
				<el-descriptions-item label="状态">{{ state.detailRow.IsOpen ? '开放' : '关闭' }}</el-descriptions-item>
				<el-descriptions-item label="注册会员数">{{ state.detailRow.Member ?? 0 }}</el-descriptions-item>
				<el-descriptions-item label="创建时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
				<el-descriptions-item label="游戏串"><pre class="detail-pre">{{ state.detailRow.Game || '-' }}</pre></el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentInvite">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { deleteAgentInvite, fetchAgentInviteInfo, fetchAgentInviteList, saveAgentInvite } from '/@/api/user/agent';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentInvite', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const emptyQuery = () => ({ Invite: '', User: '', Type: '', IsOpen: '' });
const emptyForm = () => ({ ID: '', User: '', UserID: 0, IsOpen: true, Game: '' });
const state = reactive({
	query: emptyQuery(),
	detailVisible: false,
	detailRow: null as Row | null,
	editVisible: false,
	saving: false,
	toggleId: '' as string,
	form: emptyForm(),
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentInvite',
		name: '邀请管理',
		columns: [
			{ field: 'ID', title: '邀请码', minWidth: 120 },
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Type', title: '账号类型', width: 100, slots: { default: 'row_type' } },
			{ field: 'CreateAt', title: '创建时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'Member', title: '注册会员数', width: 110, formatter: ({ cellValue }) => cellValue ?? 0 },
			{ field: 'IsOpen', title: '当前状态', width: 90, slots: { default: 'row_open' } },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const { list, total } = await fetchAgentInviteList({ ...state.query, PageIndex: page.currentPage, PageSize: page.pageSize });
		return { list, RecordCount: total };
	});
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	handleQuery(true);
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除该邀请码吗？', '提示', { type: 'warning' });
		await deleteAgentInvite(row.ID);
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/** 列表 Switch 启停邀请码。 */
const onToggleOpen = async (row: Row, open: boolean) => {
	const id = String(row.ID || '');
	if (!id) return;
	state.toggleId = id;
	try {
		await saveAgentInvite({
			ID: id,
			UserID: Number(row.UserID || 0),
			User: String(row.UserName || ''),
			IsOpen: open ? 1 : 0,
			Game: String(row.Game || ''),
		});
		row.IsOpen = open;
		ElMessage.success(open ? '已开放' : '已关闭');
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.toggleId = '';
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
		const info = (await fetchAgentInviteInfo(row.ID)) as Row;
		Object.assign(state.form, {
			ID: String(info?.ID ?? row.ID),
			User: String(info?.UserName ?? row.UserName ?? ''),
			UserID: Number(info?.UserID ?? 0),
			IsOpen: info?.IsOpen !== false && row.IsOpen !== false,
			Game: String(info?.Game ?? ''),
		});
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};

/** 保存 */
const onSave = async () => {
	if (!state.form.ID && !state.form.User?.trim()) {
		ElMessage.warning('请填写用户名');
		return;
	}
	state.saving = true;
	try {
		await saveAgentInvite({
			ID: state.form.ID || '',
			User: state.form.User || '',
			UserID: state.form.UserID || 0,
			IsOpen: state.form.IsOpen ? 1 : 0,
			Game: state.form.Game || '',
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
<style scoped>
.detail-pre { margin: 0; white-space: pre-wrap; font-size: 12px; }
</style>
