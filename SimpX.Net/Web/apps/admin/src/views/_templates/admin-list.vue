<template>
	<!--
		业务列表页拷贝模板（W0）。
		用法：复制到菜单 Component 对应路径（如 views/user/member/list.vue），再改查询字段、列、接口。
		请求必须走 adminPost / adminGet，不要用 axios-utils 的 getAPI。
	-->
	<div class="admin-list-template">
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="按业务页字段改名" clearable @keyup.enter="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" @click="handleQuery(true)">查询</el-button>
					<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				</el-button-group>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
					<template #toolbar_buttons />
					<template #toolbar_tools />
					<template #empty>
						<el-empty :image-size="160" description="暂无数据" />
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="onRowAction(row)">操作</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="adminListTemplate">
import { reactive, ref } from 'vue';
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 行类型：按业务列表字段改 */
interface RowItem {
	id?: number | string;
	[key: string]: unknown;
}

const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('adminListTemplate', {
	pageSize: 20 as number,
	defaultSort: { field: 'id', order: 'desc', descStr: 'desc' },
});

const state = reactive({
	/** 查询条件：字段名对齐业务页 Form 参数 */
	queryParams: {
		keyword: '' as string,
	},
	localPageParam,
});

const options = useVxeTable<RowItem>(
	{
		id: 'adminListTemplate',
		name: '业务列表模板',
		columns: [
			{ field: 'id', title: '编号', minWidth: 100 },
			{ field: 'name', title: '名称', minWidth: 160 },
			{ field: 'status', title: '状态', minWidth: 100 },
			{ field: 'buttons', title: '操作', width: 120, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: {
			// 模板页默认不自动请求；复制到真实页后改为 true，并换成真实 /admin 路径
			autoLoad: false,
			ajax: {
				query: ({ page }) => handleQueryApi(page),
			},
		},
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents = {
	...gridPageEvents(localPageParamKey, localPageParam),
};
void gridEvents;

/**
 * 调 /admin 列表；返回 vxe 需要的结构。
 * @param page vxe 分页
 */
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	// TODO: 换成真实路径，例如 /admin/user/member/list；参数名按业务 html
	const info = await adminPost<AdminApiPageInfo<RowItem>>('/admin/_template/list', {
		...state.queryParams,
		PageIndex: page.currentPage,
		PageSize: page.pageSize,
	}).catch(() => null);

	const list = info?.list ?? [];
	const total = info?.RecordCount ?? 0;
	return {
		page: { total },
		result: list,
	};
};

/** 重置查询并刷新 */
const resetQuery = () => {
	state.queryParams.keyword = '';
	handleQuery(true);
};

/**
 * 行操作占位。
 * @param row 当前行
 */
const onRowAction = (row: RowItem) => {
	console.info('admin-list template row action', row);
};
</script>
