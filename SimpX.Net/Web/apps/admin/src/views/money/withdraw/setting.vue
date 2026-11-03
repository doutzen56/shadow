<template>
	<!-- 规格：提现接口；channelist + channelinfo + savechannel + channelsys -->
	<div class="money-withdraw-setting-page admin-page">
		<div class="query-toolbar">
			<el-form :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="启用">
					<el-select v-model="state.query.IsOpen" clearable placeholder="全部" style="width: 110px">
						<el-option label="启用" value="1" />
						<el-option label="关闭" value="0" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:tixianguanli:tixianjiekou'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:money:tixianguanli:tixianjiekou'" @click="openEdit()">新增通道</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_open="{ row }">
						<el-switch
							:model-value="!!row.IsOpen"
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
		<el-drawer v-model="state.detailVisible" title="提现通道" size="520px" destroy-on-close>
			<template v-if="state.detailRow">
				<el-descriptions :column="1" border size="small">
					<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
					<el-descriptions-item label="名称">{{ state.detailRow.Name || '-' }}</el-descriptions-item>
					<el-descriptions-item label="系统渠道">{{ state.detailRow.SysName || state.detailRow.SysID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detailRow.Currency) || state.detailRow.Currency || '-' }}</el-descriptions-item>
					<el-descriptions-item label="成本%">{{ state.detailRow.Cost == null || state.detailRow.Cost === '' ? '-' : (Number(state.detailRow.Cost) * 100).toFixed(4) }}</el-descriptions-item>
					<el-descriptions-item label="排序">{{ state.detailRow.Sort ?? '-' }}</el-descriptions-item>
					<el-descriptions-item label="最低/最高成本">{{ state.detailRow.MinCost ?? 0 }} / {{ state.detailRow.MaxCost ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="启用">{{ state.detailRow.IsOpen ? '启用' : '关闭' }}</el-descriptions-item>
					<el-descriptions-item label="Setting"><pre class="detail-pre">{{ state.detailRow.Setting || '-' }}</pre></el-descriptions-item>
					<el-descriptions-item label="QuerySetting"><pre class="detail-pre">{{ state.detailRow.QuerySetting || '-' }}</pre></el-descriptions-item>
				</el-descriptions>
			</template>
			<el-empty v-else description="无详情" />
		</el-drawer>
		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改提现通道' : '新增提现通道'" width="560px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="110px">
				<el-form-item label="名称" required><el-input v-model="state.form.Name" /></el-form-item>
				<el-form-item label="系统渠道" required>
					<el-select v-model="state.form.SysID" filterable placeholder="选择系统代付渠道" style="width: 100%">
						<el-option v-for="s in state.sysOptions" :key="s.ID" :label="`${s.Name || s.ID} (#${s.ID})`" :value="Number(s.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.form.Currency" filterable clearable placeholder="选择币种" style="width: 100%">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="Number(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="成本%"><el-input-number v-model="state.form.Cost" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="排序"><el-input-number v-model="state.form.Sort" :min="0" controls-position="right" /></el-form-item>
				<el-form-item label="最低成本"><el-input-number v-model="state.form.MinCost" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="最高成本"><el-input-number v-model="state.form.MaxCost" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="启用"><el-switch v-model="state.form.IsOpen" /></el-form-item>
				<el-form-item label="Setting"><el-input v-model="state.form.Setting" type="textarea" :rows="3" /></el-form-item>
				<el-form-item label="QuerySetting"><el-input v-model="state.form.QuerySetting" type="textarea" :rows="2" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteMoneyWithdrawSetting">
import { onMounted, reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { unwrapAdminList } from '/@/api/user/member-group';
import { fetchWithdrawChannelList } from '/@/api/money/withdraw';
import { adminPost } from '/@/utils/admin-api';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { ensureCurrencyEnum, formatCurrency } from '/@/utils/admin-display';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyWithdrawSetting', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'asc', descStr: 'desc' },
});
const emptyForm = () => ({
	ID: 0,
	Name: '',
	SysID: 0,
	Currency: 0,
	Cost: 0,
	Sort: 0,
	MinCost: 0,
	MaxCost: 0,
	IsOpen: true,
	Setting: '',
	QuerySetting: '',
});
const state = reactive({
	query: { Currency: '', IsOpen: '' },
	toggleId: 0 as number,
	detailVisible: false,
	detailRow: null as Row | null,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	sysOptions: [] as Array<{ ID: number; Name?: string }>,
	currencyOptions: [] as Array<{ key: number | string; value: string }>,
	localPageParam,
});
const loadMeta = async () => {
	try {
		const sysInfo = await adminPost('/admin/money/withdraw/channelsys', {});
		const list = Array.isArray((sysInfo as any)?.list) ? (sysInfo as any).list : Array.isArray(sysInfo) ? (sysInfo as any) : [];
		state.sysOptions = (list as any[]).map((x) => ({ ID: Number(x.ID), Name: String(x.Name ?? x.ID) }));
	} catch (e: any) {
		state.sysOptions = [];
		showCaughtError(e, '加载系统渠道失败');
	}
};
const options = useVxeTable<Row>(
	{
		id: 'siteMoneyWithdrawSetting',
		name: '提现接口',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Name', title: '名称', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'SysName', title: '系统渠道', minWidth: 140, formatter: ({ row, cellValue }) => cellValue || (row.SysID ? `#${row.SysID}` : '-') },
			{ field: 'Currency', title: '币种', width: 90, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{
				field: 'Cost',
				title: '成本%',
				width: 90,
				formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(2)),
			},
			{ field: 'Sort', title: '排序', width: 80, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'CreateAt', title: '创建时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'IsOpen', title: '启用', width: 90, slots: { default: 'row_open' } },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await fetchWithdrawChannelList({});
		let list: Row[] = [];
		if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in info) list = (info as any).list || [];
		else list = unwrapAdminList<Row>(info);
		const cur = String(state.query.Currency || '');
		const open = String(state.query.IsOpen || '');
		if (cur) list = list.filter((r) => String(r.Currency ?? '') === cur);
		if (open === '1') list = list.filter((r) => !!r.IsOpen);
		if (open === '0') list = list.filter((r) => !r.IsOpen);
		return { list, RecordCount: list.length };
	});
const resetQuery = () => {
	state.query.Currency = '';
	state.query.IsOpen = '';
	handleQuery(true);
};
const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detailRow = null;
	try {
		const info = (await adminPost('/admin/money/withdraw/channelinfo', { ID: row.ID })) as Row;
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
		if (state.sysOptions[0]) state.form.SysID = state.sysOptions[0].ID;
		return;
	}
	try {
		const info = (await adminPost('/admin/money/withdraw/channelinfo', { ID: row.ID })) as Row;
		Object.assign(state.form, {
			ID: Number(info?.ID ?? row.ID),
			Name: String(info?.Name ?? row.Name ?? ''),
			SysID: Number(info?.SysID ?? 0),
			Currency: Number(info?.Currency ?? 0),
			// 库内为小数费率；编辑层按百分比展示（对齐充值通道 Fee/Cost）
			Cost: Number(info?.Cost ?? 0) * 100,
			Sort: Number(info?.Sort ?? row.Sort ?? 0),
			MinCost: Number(info?.MinCost || 0),
			MaxCost: Number(info?.MaxCost || 0),
			IsOpen: info?.IsOpen !== false && row.IsOpen !== false,
			Setting: String(info?.Setting ?? ''),
			QuerySetting: String(info?.QuerySetting ?? ''),
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
	if (!state.form.SysID) {
		ElMessage.warning('请选择系统代付渠道');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/money/withdraw/savechannel', {
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			SysID: state.form.SysID,
			Currency: state.form.Currency,
			Cost: Number(state.form.Cost || 0) / 100,
			Sort: state.form.Sort,
			MinCost: state.form.MinCost,
			MaxCost: state.form.MaxCost,
			IsOpen: state.form.IsOpen ? 1 : 0,
			Setting: state.form.Setting || '',
			QuerySetting: state.form.QuerySetting || '',
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

const onToggle = async (row: Row, value: boolean) => {
	state.toggleId = Number(row.ID) || 0;
	try {
		// 走 savechannel 最小字段切换启用
		await adminPost('/admin/money/withdraw/savechannel', {
			ID: row.ID,
			Name: row.Name || '',
			SysID: row.SysID || 0,
			Currency: row.Currency || 0,
			// savechannel 会再 ÷100，这里按编辑层百分比传
			Cost: Number(row.Cost ?? 0) * 100,
			Sort: row.Sort ?? 0,
			IsOpen: value ? 1 : 0,
			MinCost: row.MinCost ?? 0,
			MaxCost: row.MaxCost ?? 0,
			Setting: row.Setting || '',
			QuerySetting: row.QuerySetting || '',
		});
		row.IsOpen = value;
		ElMessage.success(value ? '已启用' : '已关闭');
	} catch (e: any) {
		showCaughtError(e);
		handleQuery(false);
	} finally {
		state.toggleId = 0;
	}
};

const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`确认删除通道「${row.Name || row.ID}」？`, '提示', { type: 'warning' });
		await adminPost('/admin/money/withdraw/deletechannel', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel' && e?.toString?.() !== 'cancel') showCaughtError(e);
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
