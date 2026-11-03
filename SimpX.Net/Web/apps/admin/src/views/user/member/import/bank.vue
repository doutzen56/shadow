<template>
	<!-- 规格：旧站银行导入；Excel/CSV 暂存→createbank -->
	<div class="user-member-import-bank-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" v-auth="'site:user:member:list'" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain v-auth="'site:user:member:list'" @click="openAdd">添加一行</el-button>
				<el-button type="warning" plain v-auth="'site:user:member:list'" @click="triggerCsv">Excel/CSV 导入暂存</el-button>
				<input ref="csvRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onCsvChange" />
				<el-button type="danger" plain :loading="state.applying" v-auth="'site:user:member:list'" @click="onApplySelected">申请入库</el-button>
			</div>
		</div>
		<p class="hint">格式：用户名,开户名,银行名,账号,开户地址,银行类型,IsVir（首行可表头）。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无导入行" /></template>
					<template #row_buttons="{ row }">
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.addVisible" title="添加银行导入行" width="480px" destroy-on-close>
			<el-form :model="state.form" label-width="100px">
				<el-form-item label="用户名" required><el-input v-model="state.form.UserName" /></el-form-item>
				<el-form-item label="开户名" required><el-input v-model="state.form.Name" /></el-form-item>
				<el-form-item label="银行名"><el-input v-model="state.form.BankName" /></el-form-item>
				<el-form-item label="账号" required><el-input v-model="state.form.Account" /></el-form-item>
				<el-form-item label="开户地址"><el-input v-model="state.form.AccountDetail" /></el-form-item>
				<el-form-item label="类型"><el-input-number v-model="state.form.Type" :min="0" controls-position="right" /></el-form-item>
				<el-form-item label="IsVir"><el-input v-model="state.form.IsVir" placeholder="0 银行卡" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSaveAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberImportBank">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { isUserNameHeader, parseAdminImportTable } from '/@/utils/admin-import-file';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const csvRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberImportBank', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const emptyForm = () => ({
	UserName: '',
	Name: '',
	BankName: '',
	Account: '',
	AccountDetail: '',
	Type: 0,
	IsVir: '0',
});
const state = reactive({ addVisible: false, saving: false, applying: false, form: emptyForm(), localPageParam });
const options = useVxeTable<Row>(
	{
		id: 'siteUserMemberImportBank',
		name: '银行导入',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Name', title: '开户名', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'BankName', title: '银行', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Account', title: '账号', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
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
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const unwrapRows = (info: any): { list: Row[]; total: number } => {
	if (!info) return { list: [], total: 0 };
	if (Array.isArray(info)) return { list: info, total: info.length };
	if (Array.isArray(info.list)) return { list: info.list, total: info.RecordCount ?? info.list.length };
	if (Array.isArray(info.Items)) return { list: info.Items, total: info.Total ?? info.Items.length };
	return { list: [], total: 0 };
};
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/site/admin/importbanklist', { PageIndex: page.currentPage, PageSize: page.pageSize });
		const { list, total } = unwrapRows(info);
		return { list, RecordCount: total };
	});
const openAdd = () => {
	Object.assign(state.form, emptyForm());
	state.addVisible = true;
};
const onSaveAdd = async () => {
	if (!state.form.UserName?.trim() || !state.form.Account?.trim()) {
		ElMessage.warning('请填写用户名和账号');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/site/admin/savebank', { ID: 0, ...state.form, UserName: state.form.UserName.trim() });
		ElMessage.success('已暂存');
		state.addVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};
const triggerCsv = () => csvRef.value?.click();
const onCsvChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		const lines = await parseAdminImportTable(file, 7);
		let ok = 0;
		for (const p of lines) {
			if (isUserNameHeader(p[0])) continue;
			if (p.length < 4 || !p[0] || !p[3]) continue;
			await adminPost('/admin/site/admin/savebank', {
				ID: 0,
				UserName: p[0],
				Name: p[1] || '',
				BankName: p[2] || '',
				Account: p[3],
				AccountDetail: p[4] || '',
				Type: Number(p[5] || 0) || 0,
				IsVir: p[6] || '0',
			});
			ok++;
		}
		ElMessage.success(ok ? `已暂存 ${ok} 行` : '未解析到有效行');
		if (ok) handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	}
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`删除暂存 ${row.UserName || row.ID}？`, '提示', { type: 'warning' });
		await adminPost('/admin/site/admin/deletebank', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
const onApplySelected = async () => {
	const rows = (xGrid.value?.getCheckboxRecords?.() as Row[]) || [];
	if (!rows.length) {
		ElMessage.warning('请先勾选要入库的行');
		return;
	}
	state.applying = true;
	try {
		await ElMessageBox.confirm(`确认将 ${rows.length} 行申请写入银行信息？`, '提示', { type: 'warning' });
		const ids = rows.map((r) => r.ID).filter((id) => id > 0).join(',');
		await adminPost('/admin/site/admin/createbank', { Ids: ids, ID: ids });
		ElMessage.success('已提交');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.applying = false;
	}
};
</script>

<style scoped>
.hint {
	margin: 0 0 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
