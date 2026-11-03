<template>
	<!-- 规格：Web.Admin/src/views/money/recharge/setting.html；settinglist + settinginfo + savesetting -->
	<div class="money-recharge-setting-page admin-page">
		<div class="query-toolbar">
			<el-form :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="分类">
					<el-select v-model="state.query.CateID" clearable filterable placeholder="全部" style="width: 140px">
						<el-option v-for="c in state.cateOptions" :key="c.ID" :label="c.Name || String(c.ID)" :value="String(c.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="启用" value="1" />
						<el-option label="停用" value="0" />
					</el-select>
				</el-form-item>
				<el-form-item label="名称">
					<el-input v-model="state.query.Name" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:chongzhiguanli:jiekousheding'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:money:chongzhiguanli:jiekousheding'" @click="openEdit()">新增通道</el-button>
				<el-button type="primary" plain :disabled="!state.selectedIds.length" :loading="state.batching" v-auth="'site:money:chongzhiguanli:jiekousheding'" @click="batchStatus(1)">批量启用</el-button>
				<el-button type="danger" plain :disabled="!state.selectedIds.length" :loading="state.batching" v-auth="'site:money:chongzhiguanli:jiekousheding'" @click="batchStatus(0)">批量停用</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_open="{ row }">
						<el-switch
							:model-value="!!(row.IsOpen || row.Status)"
							:loading="state.toggleId === row.ID"
							@change="(v) => onToggle(row, !!v)"
						/>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-drawer v-model="state.detailVisible" title="充值通道" size="520px" destroy-on-close>
			<template v-if="state.detailRow">
				<el-descriptions :column="1" border size="small">
					<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
					<el-descriptions-item label="名称">{{ state.detailRow.Name || '-' }}</el-descriptions-item>
					<el-descriptions-item label="分类">{{ state.detailRow.Category || state.detailRow.Type || state.detailRow.CateID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="系统渠道">{{ state.detailRow.SysName || state.detailRow.Payment || state.detailRow.SysID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detailRow.Currency) || state.detailRow.Currency || '-' }}</el-descriptions-item>
					<el-descriptions-item label="最低/最高">{{ state.detailRow.MinMoney ?? 0 }} / {{ state.detailRow.MaxMoney ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="日停用额">{{ state.detailRow.MaxPay ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="手续费%">{{ state.detailRow.Fee == null ? '-' : (Number(state.detailRow.Fee) * 100).toFixed(4) }}</el-descriptions-item>
					<el-descriptions-item label="成本%">{{ state.detailRow.Cost == null ? '-' : (Number(state.detailRow.Cost) * 100).toFixed(4) }}</el-descriptions-item>
					<el-descriptions-item label="排序">{{ state.detailRow.Sort ?? '-' }}</el-descriptions-item>
					<el-descriptions-item label="启用">{{ state.detailRow.IsOpen || state.detailRow.Status ? '启用' : '关闭' }}</el-descriptions-item>
					<el-descriptions-item label="备注">{{ state.detailRow.Remark || '-' }}</el-descriptions-item>
					<el-descriptions-item label="Setting"><pre class="detail-pre">{{ typeof state.detailRow.Setting === 'string' ? state.detailRow.Setting : JSON.stringify(state.detailRow.Setting ?? '', null, 2) }}</pre></el-descriptions-item>
				</el-descriptions>
			</template>
			<el-empty v-else description="无详情" />
		</el-drawer>
		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改充值通道' : '新增充值通道'" width="560px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="110px">
				<el-form-item label="名称" required><el-input v-model="state.form.Name" /></el-form-item>
				<el-form-item label="分类" required>
					<el-select v-model="state.form.CateID" filterable placeholder="选择分类" style="width: 100%">
						<el-option v-for="c in state.cateOptions" :key="c.ID" :label="c.Name || String(c.ID)" :value="Number(c.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="系统渠道" required>
					<el-select v-model="state.form.SysID" filterable placeholder="选择系统渠道" style="width: 100%">
						<el-option v-for="s in state.sysOptions" :key="s.ID" :label="`${s.Name || s.ID} (#${s.ID})`" :value="Number(s.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.form.Currency" filterable clearable placeholder="选择币种" style="width: 100%">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="Number(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="最低"><el-input-number v-model="state.form.MinMoney" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="最高"><el-input-number v-model="state.form.MaxMoney" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="日停用额"><el-input-number v-model="state.form.MaxPay" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="手续费%"><el-input-number v-model="state.form.Fee" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="成本%"><el-input-number v-model="state.form.Cost" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="排序"><el-input-number v-model="state.form.Sort" :min="0" controls-position="right" /></el-form-item>
				<el-form-item label="启用"><el-switch v-model="state.form.IsOpen" /></el-form-item>
				<el-form-item label="备注"><el-input v-model="state.form.Remark" type="textarea" :rows="2" /></el-form-item>
				<el-form-item label="Setting"><el-input v-model="state.form.Setting" type="textarea" :rows="3" placeholder="通道 Setting 原文/JSON" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteMoneyRechargeSetting">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { unwrapAdminList } from '/@/api/user/member-group';
import { fetchRechargeSettingList } from '/@/api/money/recharge';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum, formatCurrency, formatMoney2 } from '/@/utils/admin-display';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyRechargeSetting', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'asc', descStr: 'desc' },
});
const emptyForm = () => ({
	ID: 0,
	Name: '',
	CateID: 0,
	Currency: 0,
	MinMoney: 0,
	MaxMoney: 0,
	MaxPay: 0,
	Sort: 0,
	IsOpen: true,
	Remark: '',
	Setting: '',
	SysID: 0,
	Fee: 0,
	Cost: 0,
});
const state = reactive({
	query: { CateID: '', Status: '', Name: '', Currency: '' },
	selectedIds: [] as number[],
	batching: false,
	toggleId: 0 as number,
	detailVisible: false,
	detailRow: null as Row | null,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	cateOptions: [] as Array<{ ID: number; Name?: string }>,
	sysOptions: [] as Array<{ ID: number; Name?: string }>,
	currencyOptions: [] as Array<{ key: number | string; value: string }>,
	localPageParam,
});
const loadMeta = async () => {
	try {
		const [cateInfo, sysInfo] = await Promise.all([
			adminPost('/admin/money/recharge/settingcate', {}),
			adminPost('/admin/money/recharge/settingsys', {}),
		]);
		const toOpts = (info: any) => {
			const list = Array.isArray(info?.list) ? info.list : Array.isArray(info) ? info : [];
			return (list as any[]).map((x) => ({ ID: Number(x.ID), Name: String(x.Name ?? x.ID) }));
		};
		state.cateOptions = toOpts(cateInfo);
		state.sysOptions = toOpts(sysInfo);
	} catch (e: any) {
		state.cateOptions = [];
		state.sysOptions = [];
		showCaughtError(e, '加载分类/渠道失败');
	}
};
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyRechargeSetting',
		name: '充值接口设定',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Name', title: '名称', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Category', title: '分类', minWidth: 100, formatter: ({ row, cellValue }) => cellValue || row.Type || '-' },
			{ field: 'GroupNames', title: '开放分组', minWidth: 120, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'SysName', title: '系统渠道', minWidth: 120, formatter: ({ row, cellValue }) => cellValue || row.Payment || '-' },
			{ field: 'Currency', title: '币种', width: 90, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'MinMoney', title: '最低', width: 90, formatter: ({ cellValue }) => formatMoney2(cellValue) },
			{ field: 'MaxMoney', title: '最高', width: 90, formatter: ({ cellValue }) => formatMoney2(cellValue) },
			{ field: 'Fee', title: '手续费%', width: 90, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(4)) },
			{ field: 'Cost', title: '成本%', width: 90, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(4)) },
			{ field: 'Sort', title: '排序', width: 70, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'IsOpen', title: '启用', width: 100, slots: { default: 'row_open' } },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
let gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		state.selectedIds = [];
		const info = await fetchRechargeSettingList({
			CateID: state.query.CateID || '',
			Status: state.query.Status || '',
			Name: state.query.Name || '',
			Currency: state.query.Currency || '',
		});
		if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in info) return info as any;
		const list = unwrapAdminList<Row>(info);
		return { list, RecordCount: list.length };
	});
const resetQuery = () => {
	state.query.CateID = '';
	state.query.Status = '';
	state.query.Name = '';
	state.query.Currency = '';
	handleQuery(true);
};
const syncSelected = () => {
	const rows = (xGrid.value?.getCheckboxRecords?.() as Row[]) || [];
	state.selectedIds = rows.map((r) => Number(r.ID)).filter((n) => n > 0);
};

const onToggle = async (row: Row, value: boolean) => {
	state.toggleId = Number(row.ID) || 0;
	try {
		await adminPost('/admin/recharge/updatepaymentstatus', { ID: row.ID, Status: value ? 1 : 0 });
		row.IsOpen = value;
		row.Status = value ? 1 : 0;
		ElMessage.success(value ? '已启用' : '已停用');
	} catch (e: any) {
		showCaughtError(e);
		handleQuery(false);
	} finally {
		state.toggleId = 0;
	}
};

const batchStatus = async (status: 0 | 1) => {
	syncSelected();
	if (!state.selectedIds.length) {
		ElMessage.warning('请先勾选通道');
		return;
	}
	state.batching = true;
	try {
		await adminPost(status === 1 ? '/admin/recharge/open' : '/admin/recharge/stop', {
			ID: state.selectedIds.join(','),
		});
		ElMessage.success(status === 1 ? '已批量启用' : '已批量停用');
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.batching = false;
	}
};
gridEvents = {
	...gridPageEvents(localPageParamKey, localPageParam),
	checkboxChange: () => syncSelected(),
	checkboxAll: () => syncSelected(),
};
const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detailRow = null;
	try {
		const info = (await adminPost('/admin/money/recharge/settinginfo', { ID: row.ID })) as Row;
		state.detailRow = info && typeof info === 'object' ? { ...row, ...info } : row;
	} catch (e: any) {
		showCaughtError(e, '加载失败');
		state.detailRow = row;
	}
};
const openEdit = async (row?: Row) => {
	Object.assign(state.form, emptyForm());
	state.editVisible = true;
	await loadMeta();
	if (!row?.ID) {
		if (state.cateOptions[0]) state.form.CateID = state.cateOptions[0].ID;
		if (state.sysOptions[0]) state.form.SysID = state.sysOptions[0].ID;
		return;
	}
	try {
		const info = (await adminPost('/admin/money/recharge/settinginfo', { ID: row.ID })) as Row;
		Object.assign(state.form, {
			ID: Number(info?.ID ?? row.ID),
			Name: String(info?.Name ?? row.Name ?? ''),
			CateID: Number(info?.CateID ?? 0),
			Currency: Number(info?.Currency ?? row.Currency ?? 0),
			MinMoney: Number(info?.MinMoney ?? row.MinMoney ?? 0),
			MaxMoney: Number(info?.MaxMoney ?? row.MaxMoney ?? 0),
			MaxPay: Number(info?.MaxPay ?? row.MaxPay ?? 0),
			Sort: Number(info?.Sort ?? 0),
			IsOpen: Boolean(info?.Status ?? info?.IsOpen ?? row.IsOpen ?? row.Status),
			Remark: String(info?.Remark ?? row.Remark ?? ''),
			Setting: typeof info?.Setting === 'string' ? info.Setting : JSON.stringify(info?.Setting ?? ''),
			SysID: Number(info?.SysID ?? 0),
			// 库内为小数费率；编辑层按百分比展示（对齐提现通道 Cost）
			Fee: Number(info?.Fee ?? 0) * 100,
			Cost: Number(info?.Cost ?? 0) * 100,
		});
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};
const onSave = async () => {
	if (!state.form.Name?.trim()) {
		ElMessage.warning('请输入名称');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/money/recharge/savesetting', {
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			CateID: state.form.CateID,
			Currency: state.form.Currency,
			MinMoney: state.form.MinMoney,
			MaxMoney: state.form.MaxMoney,
			MaxPay: state.form.MaxPay,
			Sort: state.form.Sort,
			Status: state.form.IsOpen ? 1 : 0,
			Remark: state.form.Remark || '',
			Setting: state.form.Setting || '',
			SysID: state.form.SysID,
			Fee: Number(state.form.Fee || 0) / 100,
			Cost: Number(state.form.Cost || 0) / 100,
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


const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`确认删除通道「${row.Name || row.ID}」？`, '提示', { type: 'warning' });
		await adminPost('/admin/recharge/deletepayment', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel' && e?.toString?.() !== 'cancel') showCaughtError(e);
	}
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
		showCaughtError(e, '加载币种失败');
	}
};

onMounted(() => {
	void ensureCurrencyEnum();
	void loadMeta();
	void loadCurrencyOptions();
});
</script>

<style scoped>
.detail-pre {
	margin: 0;
	white-space: pre-wrap;
	font-size: 12px;
	max-height: 180px;
	overflow: auto;
}
</style>
