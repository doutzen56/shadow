<template>
	<!-- 规格：Web.Admin risk/ip/black.html + blackinfo.html -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.query.User" clearable placeholder="名称/IP/备注" style="width: 180px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:ip:black'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:risk:ip:black'" @click="openEdit()">添加</el-button>
				<el-button type="warning" plain v-auth="'site:risk:ip:black'" @click="triggerImport">Excel/CSV 导入</el-button>
				<input ref="fileRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onImportChange" />
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_ip="{ row }">{{ row.StartIP || row.IP || '-' }}{{ row.EndIP && row.EndIP !== row.StartIP ? ` - ${row.EndIP}` : '' }}</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改 IP 黑名单' : '添加 IP 黑名单'" width="480px" destroy-on-close>
			<el-form :model="state.form" label-width="90px">
				<el-form-item label="名称"><el-input v-model="state.form.Name" /></el-form-item>
				<el-form-item label="开始 IP" required><el-input v-model="state.form.StartIP" placeholder="单 IP 或段起始" /></el-form-item>
				<el-form-item label="结束 IP"><el-input v-model="state.form.EndIP" placeholder="可空=同开始" /></el-form-item>
				<el-form-item label="备注"><el-input v-model="state.form.Remark" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
		<el-drawer v-model="state.detailVisible" title="IP 黑名单" size="420px" destroy-on-close>
			<el-descriptions v-if="state.detailRow" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
				<el-descriptions-item label="名称">{{ state.detailRow.Name || '-' }}</el-descriptions-item>
				<el-descriptions-item label="IP">{{ state.detailRow.StartIP || state.detailRow.IP || '-' }}{{ state.detailRow.EndIP && state.detailRow.EndIP !== state.detailRow.StartIP ? ` - ${state.detailRow.EndIP}` : '' }}</el-descriptions-item>
				<el-descriptions-item label="备注">{{ state.detailRow.Remark || '-' }}</el-descriptions-item>
				<el-descriptions-item label="添加时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
			</el-descriptions>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteRiskIpBlack">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchRiskIpBlacklist } from '/@/api/risk/index';
import { parseAdminImportTable } from '/@/utils/admin-import-file';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const fileRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskIpBlack', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const emptyForm = () => ({ ID: 0, Name: '', StartIP: '', EndIP: '', Remark: '' });
const state = reactive({
	query: { User: '' },
	detailVisible: false,
	detailRow: null as Row | null,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteRiskIpBlack',
		name: 'IP黑名单',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Name', title: '名称', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'StartIP', title: 'IP', minWidth: 180, slots: { default: 'row_ip' } },
			{ field: 'Remark', title: '备注', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '添加时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 180, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchRiskIpBlacklist({ User: state.query.User || '', PageIndex: page.currentPage, PageSize: page.pageSize })
	);
const openDetail = (row: Row) => {
	state.detailVisible = true;
	state.detailRow = row;
};
const resetQuery = () => {
	state.query.User = '';
	handleQuery(true);
};
const openEdit = async (row?: Row) => {
	Object.assign(state.form, emptyForm());
	state.editVisible = true;
	if (!row?.ID) return;
	try {
		const info = (await adminPost('/admin/risk/ip/blackinfo', { ID: row.ID })) as Row;
		Object.assign(state.form, {
			ID: Number(info?.ID ?? row.ID),
			Name: String(info?.Name ?? row.Name ?? ''),
			StartIP: String(info?.StartIP ?? info?.IP ?? row.StartIP ?? row.IP ?? ''),
			EndIP: String(info?.EndIP ?? row.EndIP ?? ''),
			Remark: String(info?.Remark ?? row.Remark ?? ''),
		});
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};
const onSave = async () => {
	if (!state.form.StartIP?.trim()) {
		ElMessage.warning('请填写开始 IP');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/risk/ip/saveblackip', {
			ID: state.form.ID || 0,
			Name: state.form.Name || state.form.StartIP.trim(),
			StartIP: state.form.StartIP.trim(),
			EndIP: state.form.EndIP.trim() || state.form.StartIP.trim(),
			Remark: state.form.Remark || '',
		});
		ElMessage.success('已保存');
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
		await ElMessageBox.confirm(`确认删除 ${row.Name || row.StartIP || row.ID}？`, '提示', { type: 'warning' });
		await adminPost('/admin/risk/ip/deleteblackip', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
const triggerImport = () => fileRef.value?.click();
const onImportChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		const lines = await parseAdminImportTable(file, 2);
		const ips: string[] = [];
		for (const p of lines) {
			const ip = (p[0] || '').trim();
			if (!ip || /^ip$/i.test(ip) || /^名称$/i.test(ip)) continue;
			ips.push(ip);
		}
		if (!ips.length) {
			ElMessage.warning('未解析到 IP（每行一个 IP，或首列 IP）');
			return;
		}
		await adminPost('/admin/risk/ip/batchsaveblackip', { IP: ips.join(','), Ips: ips.join(',') });
		ElMessage.success(`已导入 ${ips.length} 条`);
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	}
};
</script>
