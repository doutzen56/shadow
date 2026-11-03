<template>
	<!-- 规格：Web.Admin report/user/signin.html → /admin/report/user/signinlog -->
	<div class="report-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="关键字"><el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="state.timeRange = todayRange(); handleQuery(true)">今日</el-button>
				<el-button @click="state.timeRange = rangeDays(7); handleQuery(true)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:report:user:signin'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:report:user:signin'" @click="openAdd">手工补录</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_add="{ row }">
						<el-tag size="small" :type="row.IsAdd ? 'warning' : 'info'">{{ row.IsAdd ? '是' : '否' }}</el-tag>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-dialog v-model="state.addVisible" title="手工补录签到" width="420px" destroy-on-close>
			<p class="tip">仅记库不发奖；请填写会员编号。</p>
			<el-form label-width="90px">
				<el-form-item label="会员编号" required>
					<el-input-number v-model="state.addUserId" :min="1" controls-position="right" style="width: 100%" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.adding" @click="onAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteReportUserSignin">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteReportUserSignin', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
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
const todayRange = (): [string, string] => rangeDays(0);
const state = reactive({
	query: { User: '' },
	timeRange: todayRange() as [string, string] | null,
	addVisible: false,
	addUserId: 0,
	adding: false,
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteReportUserSignin',
		name: '签到记录',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'UserName', title: '用户', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{
				field: 'IsAdd',
				title: '手工补录',
				width: 100,
				slots: { default: 'row_add' },
			},
			{ field: 'CreateAt', title: '签到时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		adminPost('/admin/report/user/signinlog', {
			User: state.query.User || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);
const resetQuery = () => {
	state.query.User = '';
	state.timeRange = todayRange();
	handleQuery(true);
};
const openAdd = () => {
	state.addUserId = 0;
	state.addVisible = true;
};
const onAdd = async () => {
	if (!state.addUserId || state.addUserId <= 0) {
		ElMessage.warning('请填写会员编号');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认给会员 #${state.addUserId} 手工补录签到？（仅记库不发奖）`, '手工补录', { type: 'warning' });
		state.adding = true;
		await adminPost('/admin/report/user/addsignlog', { UserID: state.addUserId });
		ElMessage.success('已补录');
		state.addVisible = false;
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.adding = false;
	}
};
</script>

<style scoped>
.tip {
	margin: 0 0 12px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
