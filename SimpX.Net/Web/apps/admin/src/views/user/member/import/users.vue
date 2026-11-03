<template>
	<!-- 规格：旧站注册会员导入；Excel/CSV 暂存→createuser 落库 -->
	<div class="user-member-import-users-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" v-auth="'site:user:member:list'" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain v-auth="'site:user:member:list'" @click="openAdd">添加一行</el-button>
				<el-button type="warning" plain v-auth="'site:user:member:list'" @click="triggerCsv">Excel/CSV 导入暂存</el-button>
				<input ref="csvRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onCsvChange" />
				<el-button type="danger" plain :loading="state.applying" v-auth="'site:user:member:list'" @click="onApplySelected">申请入库</el-button>
			</div>
		</div>
		<p class="hint">格式：用户名,密码,上级代理,手机,币种,额度,开户名（首行可表头；Excel 取前 7 列）。先暂存再「申请入库」。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无导入行" /></template>
					<template #row_bal="{ row }">
						<span class="money-in">{{ formatMoney2(row.Balance) }}</span>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.addVisible" title="添加导入行" width="480px" destroy-on-close>
			<el-form :model="state.form" label-width="100px">
				<el-form-item label="用户名" required><el-input v-model="state.form.UserName" /></el-form-item>
				<el-form-item label="密码" required><el-input v-model="state.form.Password" show-password /></el-form-item>
				<el-form-item label="上级代理"><el-input v-model="state.form.AgentName" /></el-form-item>
				<el-form-item label="手机"><el-input v-model="state.form.Mobile" /></el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.form.Currency" filterable style="width: 100%">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="Number(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="额度"><el-input-number v-model="state.form.Balance" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="开户名"><el-input v-model="state.form.AccountName" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSaveAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberImportUsers">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum, formatCurrency, formatMoney2 } from '/@/utils/admin-display';
import { isUserNameHeader, parseAdminImportTable } from '/@/utils/admin-import-file';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const csvRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberImportUsers', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const emptyForm = () => ({
	UserName: '',
	Password: '',
	AgentName: '',
	Mobile: '',
	Currency: 0,
	Balance: 0,
	AccountName: '',
});
const state = reactive({
	addVisible: false,
	saving: false,
	applying: false,
	form: emptyForm(),
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	localPageParam,
});

const options = useVxeTable<Row>(
	{
		id: 'siteUserMemberImportUsers',
		name: '注册会员导入',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AgentName', title: '上级代理', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Mobile', title: '手机', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Currency', title: '币种', width: 90, formatter: ({ cellValue }) => formatCurrency(cellValue) },
			{ field: 'Balance', title: '额度', width: 100, slots: { default: 'row_bal' } },
			{ field: 'AccountName', title: '开户名', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
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
	if (Array.isArray(info.items)) return { list: info.items, total: info.total ?? info.items.length };
	return { list: [], total: 0 };
};

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/site/admin/importuserlist', {
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		});
		const { list, total } = unwrapRows(info);
		return { list, RecordCount: total };
	});
};

const loadCurrencyOptions = async () => {
	try {
		const list = await fetchCurrencyOptions();
		state.currencyOptions = (list || []).map((x: any) => ({
			key: x.key ?? x.Key ?? x.value ?? x.Value,
			value: String(x.value ?? x.Value ?? x.key ?? x.Key ?? ''),
		}));
	} catch (e: any) {
		state.currencyOptions = [];
		showCaughtError(e);
	}
};

onMounted(() => {
	void ensureCurrencyEnum();
	void loadCurrencyOptions();
});

const openAdd = () => {
	Object.assign(state.form, emptyForm());
	state.addVisible = true;
};

const onSaveAdd = async () => {
	if (!state.form.UserName?.trim() || !state.form.Password?.trim()) {
		ElMessage.warning('请填写用户名和密码');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/site/admin/saveuser', {
			ID: 0,
			UserName: state.form.UserName.trim(),
			Password: state.form.Password,
			AgentName: state.form.AgentName || '',
			Mobile: state.form.Mobile || '',
			Currency: state.form.Currency,
			Balance: state.form.Balance,
			AccountName: state.form.AccountName || '',
		});
		ElMessage.success('已暂存');
		state.addVisible = false;
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
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
		for (const parts of lines) {
			if (!parts.length) continue;
			if (isUserNameHeader(parts[0])) continue;
			if (parts.length < 2 || !parts[0] || !parts[1]) continue;
			await adminPost('/admin/site/admin/saveuser', {
				ID: 0,
				UserName: parts[0],
				Password: parts[1],
				AgentName: parts[2] || '',
				Mobile: parts[3] || '',
				Currency: Number(parts[4] || 0) || 0,
				Balance: Number(parts[5] || 0) || 0,
				AccountName: parts[6] || '',
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
		await adminPost('/admin/site/admin/deleteuser', { ID: row.ID });
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
		await ElMessageBox.confirm(`确认将 ${rows.length} 行申请写入正式会员？`, '提示', { type: 'warning' });
		const ids = rows.map((r) => r.ID).filter((id) => id > 0);
		const result = await adminPost('/admin/site/admin/createuser', { Ids: ids.join(','), ID: ids.join(',') });
		ElMessage.success(typeof result === 'string' && result ? result : `已提交 ${ids.length} 行`);
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
<style scoped>
.money-in { color: #67c23a; }
</style>
