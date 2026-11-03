<template>
	<div class="sys-notice-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="'标题'" prop="title">
							<el-input v-model="state.queryParams.title" :placeholder="'标题'" clearable @keyup.enter="handleQuery(true)" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="'类型'" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="'类型'" clearable @clear="state.queryParams.type = undefined">
								<el-option :label="'通知'" :value="1" />
								<el-option :label="'公告'" :value="2" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysNotice/page'" :loading="options.loading"> {{ '查询' }} </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> {{ '重置' }} </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysNotice/add'"> {{ '新增' }} </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					<el-tag v-if="row.type === 1" type="success">{{ '通知' }}</el-tag>
					<el-tag v-else type="warning">{{ '公告' }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="info">{{ '已发布' }}</el-tag>
					<el-tag v-else type="warning">{{ '未发布' }}</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="'编辑'" placement="top">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" v-auth="'sysNotice/update'" :disabled="row.status === 1" />
					</el-tooltip>
					<el-tooltip :content="'删除'" placement="top">
						<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(row)" v-auth="'sysNotice/delete'" :disabled="row.status === 1" />
					</el-tooltip>
					<el-button icon="ele-Position" size="small" text type="primary" @click="handlePublic(row)" v-auth="'sysNotice/public'" :disabled="row.status === 1">
						{{ '发布' }}
					</el-button>
					<el-button icon="ele-InfoFilled" text type="primary" @click="handleView(row)"> {{ '详情' }} </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<!-- 消息详情弹窗 -->
		<NoticeDetailDialog ref="noticeDetailRef" />
		<EditNotice ref="editNoticeRef" :title="state.title" @handleQuery="handleQuery" />
		<NoticeUser ref="noticeUserRef" v-model:visible="state.noticeUserVisible" :data="state.noticeUsers" @change="closeGrantUser" />
	</div>
</template>

<!-- 通知公告 -->
<script lang="ts" setup name="sysNotice">
import { reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import commonFunction from '/@/utils/commonFunction';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditNotice from '/@/views/system/notice/component/editNotice.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import NoticeUser from '/@/components/selector/userSelectorDialog.vue';
import NoticeDetailDialog from '/@/views/system/notice/component/noticeDetailDialog.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/system/api';
import { SysNotice, PageNoticeInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editNoticeRef = ref<InstanceType<typeof EditNotice>>();
const noticeUserRef = ref<InstanceType<typeof NoticeUser>>();
const noticeDetailRef = ref();
const { removeHtml } = commonFunction();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysNotice', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		title: undefined,
		type: undefined,
	},
	localPageParam,
	title: '',
	currentNoticeRow: {} as any, // 当前选择的通知公告
	noticeUserVisible: false, // 账号选择器
	noticeUsers: [] as any, // 用户集合
});

// 表格参数配置
const options = useVxeTable<SysNotice>(
	{
		id: 'sysNotice',
		name: '通知',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'title', title: '标题', minWidth: 150, showOverflow: 'tooltip', align: 'left', headerAlign: 'center' },
			{ field: 'content', title: '内容', minWidth: 200, showOverflow: 'tooltip', align: 'left', headerAlign: 'center', slots: { default: (scope: any) => removeHtml(scope.row.content) } },
			{ field: 'type', title: '类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_type' } },
			{ field: 'createTime', title: '创建时间', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'publicUserName', title: '发布者', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'publicTime', title: '发布时间', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageNoticeInput;
	return getAPI(SysNoticeApi).apiSysNoticePagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.title = undefined;
	state.queryParams.type = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加通知公告';
	editNoticeRef.value?.openDialog({ type: 1 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑通知公告';
	editNoticeRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除通知公告：${row.title}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysNoticeApi).apiSysNoticeDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<SysNotice> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysNotice/update') && row.status !== 1) await handleEdit(row);
	},
};

// 发布并打开账号选择器
const handlePublic = (row: any) => {
	state.currentNoticeRow = row;
	state.noticeUserVisible = true;
};

// 关闭授权账号页面并授权
const closeGrantUser = (data: any) => {
	ElMessageBox.confirm(`确定发布通知公告：${state.currentNoticeRow.title}，${'不可撤销'}`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			var userIds = data.map((u: any) => u.id);
			await getAPI(SysNoticeApi).apiSysNoticePublicPost({ id: state.currentNoticeRow.id, userIds: userIds });
			handleQuery();
			ElMessage.success('发布成功');
		})
		.catch(() => {});

	state.noticeUserVisible = false;
};

// 查看详情
const handleView = async (row: any) => {
	noticeDetailRef.value?.openDialog(row ?? {});
	await getAPI(SysNoticeApi).apiSysNoticeSetReadPost({ id: row.id });
};
</script>
