<template>
	<!-- 规格：旧站证件导入；Excel/CSV 暂存→createidcard；正反面可上传 -->
	<div class="user-member-import-idcard-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" v-auth="'site:user:member:list'" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain v-auth="'site:user:member:list'" @click="openAdd">添加一行</el-button>
				<el-button type="warning" plain v-auth="'site:user:member:list'" @click="triggerCsv">Excel/CSV 导入暂存</el-button>
				<input ref="csvRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onCsvChange" />
				<el-button type="danger" plain :loading="state.applying" v-auth="'site:user:member:list'" @click="onApplySelected">申请入库</el-button>
			</div>
		</div>
		<p class="hint">格式：用户名,证件号,证件类型,城市（首行可表头）。添加时可上传正反面图（/admin/system/upload）。</p>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无导入行" /></template>
					<template #row_front="{ row }">
						<span class="path-cell">{{ row.CardFront || '-' }}</span>
					</template>
					<template #row_back="{ row }">
						<span class="path-cell">{{ row.CardBack || '-' }}</span>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openEdit(row)">编辑</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.addVisible" :title="state.form.ID ? '编辑证件导入行' : '添加证件导入行'" width="560px" destroy-on-close>
			<el-form :model="state.form" label-width="100px">
				<el-form-item label="用户名" required><el-input v-model="state.form.UserName" /></el-form-item>
				<el-form-item label="证件号" required><el-input v-model="state.form.CardNumber" /></el-form-item>
				<el-form-item label="证件类型"><el-input-number v-model="state.form.CardType" :min="0" controls-position="right" /></el-form-item>
				<el-form-item label="城市"><el-input v-model="state.form.CardCity" /></el-form-item>
				<el-form-item label="正面">
					<div class="upload-row">
						<el-input v-model="state.form.CardFront" placeholder="上传后自动填入" readonly />
						<el-upload :show-file-list="false" accept="image/*" :http-request="(opt) => onUpload('CardFront', opt)">
							<el-button :loading="state.uploadingFront">上传</el-button>
						</el-upload>
					</div>
				</el-form-item>
				<el-form-item label="背面">
					<div class="upload-row">
						<el-input v-model="state.form.CardBack" placeholder="上传后自动填入" readonly />
						<el-upload :show-file-list="false" accept="image/*" :http-request="(opt) => onUpload('CardBack', opt)">
							<el-button :loading="state.uploadingBack">上传</el-button>
						</el-upload>
					</div>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSaveAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberImportIdcard">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { UploadRequestOptions } from 'element-plus';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { uploadMemberKycImage } from '/@/api/user/member-kyc';
import { isUserNameHeader, parseAdminImportTable } from '/@/utils/admin-import-file';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const csvRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberImportIdcard', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const emptyForm = () => ({
	ID: 0,
	UserName: '',
	CardNumber: '',
	CardType: 0,
	CardCity: '',
	CardFront: '',
	CardBack: '',
});
const state = reactive({
	addVisible: false,
	saving: false,
	applying: false,
	uploadingFront: false,
	uploadingBack: false,
	form: emptyForm(),
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteUserMemberImportIdcard',
		name: '证件导入',
		columns: [
			{ type: 'checkbox', width: 48 },
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardNumber', title: '证件号', minWidth: 160, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardType', title: '类型', width: 80, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardCity', title: '城市', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardFront', title: '正面', minWidth: 120, slots: { default: 'row_front' } },
			{ field: 'CardBack', title: '背面', minWidth: 120, slots: { default: 'row_back' } },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
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
		const info = await adminPost('/admin/site/admin/importidcardlist', { PageIndex: page.currentPage, PageSize: page.pageSize });
		const { list, total } = unwrapRows(info);
		return { list, RecordCount: total };
	});
const openAdd = () => {
	Object.assign(state.form, emptyForm());
	state.addVisible = true;
};
const openEdit = (row: Row) => {
	Object.assign(state.form, {
		ID: Number(row.ID || 0),
		UserName: row.UserName || '',
		CardNumber: row.CardNumber || '',
		CardType: Number(row.CardType || 0),
		CardCity: row.CardCity || '',
		CardFront: row.CardFront || '',
		CardBack: row.CardBack || '',
	});
	state.addVisible = true;
};
const onUpload = async (side: 'CardFront' | 'CardBack', opt: UploadRequestOptions) => {
	const loadingKey = side === 'CardFront' ? 'uploadingFront' : 'uploadingBack';
	state[loadingKey] = true;
	try {
		const key = await uploadMemberKycImage(opt.file as File);
		state.form[side] = key;
		opt.onSuccess?.(key as any);
		ElMessage.success('已上传');
	} catch (e: any) {
		opt.onError?.(e as any);
		showCaughtError(e);
	} finally {
		state[loadingKey] = false;
	}
};
const onSaveAdd = async () => {
	if (!state.form.UserName?.trim() || !state.form.CardNumber?.trim()) {
		ElMessage.warning('请填写用户名和证件号');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/site/admin/saveidcard', {
			ID: state.form.ID || 0,
			UserName: state.form.UserName.trim(),
			CardNumber: state.form.CardNumber.trim(),
			CardType: state.form.CardType,
			CardCity: state.form.CardCity || '',
			CardFront: state.form.CardFront || '',
			CardBack: state.form.CardBack || '',
		});
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
		const lines = await parseAdminImportTable(file, 4);
		let ok = 0;
		for (const p of lines) {
			if (isUserNameHeader(p[0])) continue;
			if (p.length < 2 || !p[0] || !p[1]) continue;
			await adminPost('/admin/site/admin/saveidcard', {
				ID: 0,
				UserName: p[0],
				CardNumber: p[1],
				CardType: Number(p[2] || 0) || 0,
				CardCity: p[3] || '',
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
		await adminPost('/admin/site/admin/deleteidcard', { ID: row.ID });
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
		await ElMessageBox.confirm(`确认将 ${rows.length} 行申请写入证件？`, '提示', { type: 'warning' });
		const ids = rows
			.map((r) => r.ID)
			.filter((id) => id > 0)
			.join(',');
		await adminPost('/admin/site/admin/createidcard', { Ids: ids, ID: ids });
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
.upload-row {
	display: flex;
	gap: 8px;
	width: 100%;
}
.path-cell {
	display: inline-block;
	max-width: 140px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 12px;
}
</style>
