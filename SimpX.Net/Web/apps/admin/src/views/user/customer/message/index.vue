<template>
	<!-- 规格：Web.Admin 站内信息；list/info/send/delete；发送对齐 User CSV + 分组 -->
	<div class="user-customer-message-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名">
					<el-input v-model="state.query.User" clearable style="width: 140px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="标题">
					<el-input v-model="state.query.Title" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="已读">
					<el-select v-model="state.query.IsRead" clearable placeholder="全部" style="width: 110px">
						<el-option label="未读" value="0" />
						<el-option label="已读" value="1" />
					</el-select>
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker v-model="state.timeRange" type="datetimerange" value-format="YYYY-MM-DD HH:mm:ss" range-separator="-" start-placeholder="开始" end-placeholder="结束" style="width: 340px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:kehuguanxi:zhanneixinxi'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:user:kehuguanxi:zhanneixinxi'" @click="openSend">发送站内信</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_read="{ row }">
						<el-tag :type="row.IsRead ? 'success' : 'warning'" size="small">{{ row.IsRead ? '已读' : '未读' }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openDetail(row)">详情</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
		<el-dialog v-model="state.sendVisible" title="发送站内信" width="560px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.send" label-width="100px">
				<el-form-item label="接收者" required>
					<el-input v-model="state.send.User" type="textarea" :rows="2" placeholder="多个用户名用逗号隔开" />
				</el-form-item>
				<el-form-item label="会员分组">
					<el-checkbox-group v-model="state.send.GroupIds">
						<el-checkbox v-for="g in state.groupOptions" :key="g.ID" :label="String(g.ID)">{{ g.Name || g.ID }}</el-checkbox>
					</el-checkbox-group>
				</el-form-item>
				<el-form-item label="标题" required><el-input v-model="state.send.Title" maxlength="120" /></el-form-item>
				<el-form-item label="内容" required><el-input v-model="state.send.Content" type="textarea" :rows="6" /></el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.sendVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.sending" @click="onSend">发送</el-button>
			</template>
		</el-dialog>
		<el-drawer v-model="state.detailVisible" title="站内信息" size="480px" destroy-on-close>
			<el-descriptions v-if="state.detailObj" :column="1" border size="small">
				<el-descriptions-item label="编号">{{ state.detailObj.ID }}</el-descriptions-item>
				<el-descriptions-item label="用户">{{ state.detailObj.UserName || state.detailObj.UserID || '-' }}</el-descriptions-item>
				<el-descriptions-item label="标题">{{ state.detailObj.Title || '-' }}</el-descriptions-item>
				<el-descriptions-item label="已读">{{ state.detailObj.IsRead ? '已读' : '未读' }}</el-descriptions-item>
				<el-descriptions-item label="时间">{{ formatDateTime(state.detailObj.CreateAt) }}</el-descriptions-item>
				<el-descriptions-item label="内容">
					<pre class="detail-content">{{ state.detailObj.Content }}</pre>
				</el-descriptions-item>
			</el-descriptions>
			<pre v-else class="detail-content">{{ state.detail }}</pre>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="siteUserCustomerMessage">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchMemberGroupList, unwrapAdminList } from '/@/api/user/member-group';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserCustomerMessage', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});
const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	end.setHours(23, 59, 59, 0);
	const start = new Date();
	start.setHours(0, 0, 0, 0);
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const state = reactive({
	query: { User: '', Title: '', IsRead: '' },
	timeRange: rangeDays(0) as [string, string] | null,
	detailVisible: false,
	detail: '',
	detailObj: null as Record<string, any> | null,
	sendVisible: false,
	sending: false,
	send: { User: '', Title: '', Content: '', GroupIds: [] as string[] },
	groupOptions: [] as Array<{ ID: number; Name?: string }>,
	localPageParam,
});
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const options = useVxeTable<Row>(
	{
		id: 'siteUserCustomerMessage',
		name: '站内信息',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Title', title: '标题', minWidth: 160, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'UserName', title: '用户', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'IsRead', title: '已读', width: 90, slots: { default: 'row_read' } },
			{ field: 'CreateAt', title: '时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		adminPost('/admin/user/customer/message/list', {
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
			User: state.query.User || '',
			Title: state.query.Title || '',
			IsRead: state.query.IsRead || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
		})
	);
const resetQuery = () => {
	state.query.User = '';
	state.query.Title = '';
	state.query.IsRead = '';
	state.timeRange = rangeDays(0);
	handleQuery(true);
};

const loadGroups = async () => {
	try {
		state.groupOptions = unwrapAdminList(await fetchMemberGroupList());
	} catch (e: any) {
		state.groupOptions = [];
		showCaughtError(e, '加载分组失败');
	}
};

const openSend = async () => {
	state.send = { User: '', Title: '', Content: '', GroupIds: [] };
	state.sendVisible = true;
	if (!state.groupOptions.length) await loadGroups();
};
const onSend = async () => {
	if ((!state.send.User?.trim() && !state.send.GroupIds.length) || !state.send.Title?.trim() || !state.send.Content?.trim()) {
		ElMessage.warning('请填写接收者或勾选分组，并填写标题与内容');
		return;
	}
	state.sending = true;
	try {
		await adminPost('/admin/user/customer/message/send', {
			User: state.send.User.trim(),
			Groups: state.send.GroupIds.join(','),
			Title: state.send.Title.trim(),
			Content: state.send.Content.trim(),
		});
		ElMessage.success('已发送');
		state.sendVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.sending = false;
	}
};

const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = '';
	state.detailObj = null;
	try {
		const info = (await adminPost('/admin/user/customer/message/info', { ID: row.ID })) as Record<string, any>;
		state.detailObj = {
			ID: info?.ID ?? row.ID,
			UserName: info?.UserName ?? row.UserName,
			UserID: info?.UserID ?? row.UserID,
			Title: info?.Title ?? row.Title,
			Content: info?.Content ?? '',
			CreateAt: formatDateTime(info?.CreateAt ?? row.CreateAt),
		};
	} catch (e: any) {
		state.detail = e?.message || JSON.stringify(row, null, 2);
	}
};

const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除该站内信吗？', '提示', { type: 'warning' });
		await adminPost('/admin/user/customer/message/delete', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

onMounted(() => {
	void loadGroups();
});
</script>

<style scoped>
.detail-content {
	white-space: pre-wrap;
	font-size: 12px;
	margin: 0;
	font-family: inherit;
}
</style>
