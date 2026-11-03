<template>
	<div class="notice-container">
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
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> {{ '查询' }} </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> {{ '重置' }} </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-dblclick="({ row }: any) => handleView(row)">
				<template #toolbar_buttons> </template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_title="{ row }">
					<div class="notice-title-cell">
						<span v-if="row.readStatus !== 1" class="unread-dot"></span>
						<span class="notice-title-text" :class="{ 'is-unread': row.readStatus !== 1 }">{{ row.sysNotice.title }}</span>
					</div>
				</template>
				<template #row_type="{ row }">
					<el-tag v-if="row.sysNotice.type === 1" type="success" effect="light" round>{{ '通知' }}</el-tag>
					<el-tag v-else type="warning" effect="light" round>{{ '公告' }}</el-tag>
				</template>
				<template #row_readStatus="{ row }">
					<el-tag v-if="row.readStatus === 1" type="info" effect="plain" round>{{ '已读' }}</el-tag>
					<el-tag v-else type="danger" effect="light" round>{{ '未读' }}</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="'详情'" placement="top">
						<el-button icon="ele-InfoFilled" text type="primary" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<!-- 消息详情弹窗 -->
		<NoticeDetailDialog ref="noticeDetailRef" />
	</div>
</template>

<!-- 通知公告 -->
<script setup lang="ts" name="notice">
import { onMounted, reactive, ref } from 'vue';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import commonFunction from '/@/utils/commonFunction';
import '@wangeditor/editor/dist/css/style.css';

import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/system/api';
import { SysNoticeUser, PageNoticeInput } from '/@/api-services/system/models';
import NoticeDetailDialog from '/@/views/system/notice/component/noticeDetailDialog.vue';

const xGrid = ref<VxeGridInstance>();
const noticeDetailRef = ref();
const { removeHtml } = commonFunction();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysNoticeUser', {
	pageSize: 50 as number,
	defaultSort: { field: 'id', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		title: undefined,
		type: undefined,
	},
	localPageParam,
	title: '',
});

// 表格参数配置
const options = useVxeTable<SysNoticeUser>(
	{
		id: 'sysNoticeUser',
		name: '消息/站内信',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },

			{ field: 'sysNotice.title', title: '标题', minWidth: 220, showOverflow: 'tooltip', align: 'left', headerAlign: 'center', slots: { default: 'row_title' } },
			{
				field: 'sysNotice.content',
				title: '内容',
				minWidth: 180,
				showOverflow: 'tooltip',
				align: 'left',
				headerAlign: 'center',
				slots: { default: (scope: any) => removeHtml(scope.row.sysNotice.content) },
			},

			{ field: 'sysNotice.type', title: '类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_type' } },
			{ field: 'sysNotice.createTime', title: '创建时间', minWidth: 150, showOverflow: 'tooltip' },

			{ field: 'readStatus', title: '阅读状态', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_readStatus' } },
			{ field: 'sysNotice.publicUserName', title: '发布者', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'sysNotice.publicTime', title: '发布时间', minWidth: 150, showOverflow: 'tooltip' },

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
	return getAPI(SysNoticeApi).apiSysNoticePageReceivedPost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.title = undefined;
	state.queryParams.type = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 查看详情
const handleView = async (row: any) => {
	noticeDetailRef.value?.openDialog(row.sysNotice ?? {});
	row.readStatus = 1;
	// mittBus.emit('noticeRead', row.sysNotice.id);
	await getAPI(SysNoticeApi).apiSysNoticeSetReadPost({ id: row.sysNotice.id });
};

// 表格事件
const gridEvents: VxeGridListeners<SysNoticeUser> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};
</script>

<style lang="scss" scoped>
.notice-container {
	/* 列表标题单元格：未读圆点 + 加粗标题 */
	:deep(.notice-title-cell) {
		display: flex;
		align-items: center;
		gap: 6px;
		width: 100%;
		justify-content: center;

		.unread-dot {
			flex: 0 0 auto;
			width: 6px;
			height: 6px;
			border-radius: 50%;
			background: var(--el-color-danger);
			box-shadow: 0 0 0 3px var(--el-color-danger-light-9);
			@supports (background: color-mix(in srgb, red, blue)) {
				box-shadow: 0 0 0 3px color-mix(in srgb, var(--el-color-danger) 16%, transparent);
			}
		}

		.notice-title-text {
			overflow: hidden;
			white-space: nowrap;
			text-overflow: ellipsis;

			&.is-unread {
				font-weight: 600;
				color: var(--el-color-primary);
			}
		}
	}

	/* 列表标签统一去边框 */
	:deep(.el-tag) {
		border: none;
		font-weight: 600;
	}
}
</style>
