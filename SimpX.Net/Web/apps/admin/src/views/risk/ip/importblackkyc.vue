<template>
	<!-- 规格：Web.Admin risk/ip/importblackkyc.html → importbalckkyclist / addbalckkyc / delete / batchdelete -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="证件号码">
					<el-input v-model="state.query.UserName" clearable placeholder="模糊匹配" style="width: 180px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:risk:ip:importblackkyc'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:risk:ip:importblackkyc'" @click="openAdd">添加</el-button>
				<el-button type="warning" plain v-auth="'site:risk:ip:importblackkyc'" @click="triggerImport">导入证件号码(Excel)</el-button>
				<input ref="fileRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onImportChange" />
				<el-button type="danger" plain :loading="state.batching" v-auth="'site:risk:ip:importblackkyc'" @click="onBatchDelete">批量删除</el-button>
			</div>
		</div>
		<p class="hint">Excel/CSV 首列证件号码（可表头）；导入写入黑名单暂存表。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_buttons="{ row }">
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.addVisible" title="添加证件黑名单" width="420px" destroy-on-close>
			<el-form :model="state.form" label-width="100px">
				<el-form-item label="证件号码" required>
					<el-input v-model="state.form.BlackKyc" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.form.Demo" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSaveAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteRiskBlackKyc">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { isUserNameHeader, parseAdminImportTable } from '/@/utils/admin-import-file';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const fileRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteRiskBlackKyc', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const state = reactive({
	query: { UserName: '' },
	addVisible: false,
	saving: false,
	batching: false,
	form: { BlackKyc: '', Demo: '' },
	localPageParam,
});

const options = useVxeTable<Row>(
	{
		id: 'siteRiskBlackKyc',
		name: '证件黑名单',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'BlackKyc', title: '证件号码', minWidth: 180, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Demo', title: '备注', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '创建时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 90, fixed: 'right', slots: { default: 'row_buttons' } },
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
		adminPost('/admin/risk/ip/importbalckkyclist', {
			UserName: state.query.UserName || '',
			BlackKyc: state.query.UserName || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);

const resetQuery = () => {
	state.query.UserName = '';
	handleQuery(true);
};

const openAdd = () => {
	state.form = { BlackKyc: '', Demo: '' };
	state.addVisible = true;
};

const onSaveAdd = async () => {
	if (!state.form.BlackKyc?.trim()) {
		ElMessage.warning('请填写证件号码');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/risk/ip/addbalckkyc', {
			BlackKyc: state.form.BlackKyc.trim(),
			Demo: state.form.Demo || '',
		});
		ElMessage.success('已添加');
		state.addVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};

const triggerImport = () => fileRef.value?.click();

const onImportChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		const rows = await parseAdminImportTable(file, 2);
		let ok = 0;
		for (const p of rows) {
			const kyc = p[0] || '';
			if (!kyc || isUserNameHeader(kyc) || /^证件/.test(kyc) || /^black/i.test(kyc)) continue;
			await adminPost('/admin/risk/ip/addbalckkyc', { BlackKyc: kyc, Demo: p[1] || '' });
			ok++;
		}
		ElMessage.success(ok ? `已导入 ${ok} 行` : '未解析到有效行');
		if (ok) handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	}
};

const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`删除 ${row.BlackKyc || row.ID}？`, '提示', { type: 'warning' });
		await adminPost('/admin/risk/ip/deletebalckkyc', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

const onBatchDelete = async () => {
	const rows = (xGrid.value?.getCheckboxRecords?.() as Row[]) || [];
	if (!rows.length) {
		ElMessage.warning('请先勾选');
		return;
	}
	state.batching = true;
	try {
		await ElMessageBox.confirm(`确认删除选中 ${rows.length} 行？`, '提示', { type: 'warning' });
		const ids = rows.map((r) => r.ID).filter((id) => Number(id) > 0);
		await adminPost('/admin/risk/ip/batchdeletebalckkyc', { ID: ids.join(','), Ids: ids.join(',') });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.batching = false;
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
