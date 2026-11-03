<template>
	<!-- 规格：旧站活动会员导入；文本/CSV → createevent -->
	<div class="site-admin-event-import-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain @click="state.batchVisible = true">批量追加</el-button>
			</div>
		</div>
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
		<el-dialog v-model="state.batchVisible" title="批量追加活动会员" width="520px" destroy-on-close>
			<el-input v-model="state.batchText" type="textarea" :rows="10" placeholder="每行一条内容（对齐旧 Text）" />
			<template #footer>
				<el-button @click="state.batchVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onBatchSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteAdminEventImportUsers">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { formatDateTime } from '/@/utils/format';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteAdminEventImportUsers', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const state = reactive({ batchVisible: false, batchText: '', saving: false, localPageParam });
const options = useVxeTable<Row>(
	{
		id: 'siteAdminEventImportUsers',
		name: '活动会员导入',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Value', title: '内容', minWidth: 200 },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
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
const unwrap = (info: any) => {
	if (!info) return { list: [] as Row[], total: 0 };
	if (Array.isArray(info.Items)) return { list: info.Items as Row[], total: info.Total ?? info.Items.length };
	if (Array.isArray(info.list)) return { list: info.list as Row[], total: info.RecordCount ?? info.list.length };
	return { list: [], total: 0 };
};
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await adminPost('/admin/site/admin/importeventlist', { PageIndex: page.currentPage, PageSize: page.pageSize });
		const { list, total } = unwrap(info);
		return { list, RecordCount: total };
	});
const onBatchSave = async () => {
	if (!state.batchText.trim()) {
		ElMessage.warning('请输入内容');
		return;
	}
	state.saving = true;
	try {
		const n = await adminPost('/admin/site/admin/createevent', { Text: state.batchText });
		ElMessage.success(typeof n === 'number' ? `已追加 ${n} 条` : '已追加');
		state.batchVisible = false;
		state.batchText = '';
		handleQuery(true);
	} catch {
		/* */
	} finally {
		state.saving = false;
	}
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除？', '提示', { type: 'warning' });
		await adminPost('/admin/site/admin/deleteevent', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(false);
	} catch {
		/* */
	}
};
</script>
