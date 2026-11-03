<template>
	<div class="sys-region-container">
		<splitpanes class="default-theme">
			<pane size="20" style="display: flex">
				<RegionTree ref="regionTreeRef" @node-click="handleNodeChange" />
			</pane>
			<pane size="80" style="display: flex; flex-direction: column">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
								<el-form-item label="行政名称" prop="name">
									<el-input v-model="state.queryParams.name" placeholder="行政名称" clearable @keyup.enter.native="handleQuery(true)" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
								<el-form-item label="行政代码" prop="code">
									<el-input v-model="state.queryParams.code" placeholder="行政代码" clearable @keyup.enter.native="handleQuery(true)" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<el-row>
						<el-col>
							<el-button-group>
								<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysRegion/page'" :loading="options.loading"> 查询 </el-button>
								<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
							</el-button-group>
						</el-col>
					</el-row>
				</el-card>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px; flex: 1">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" :tree-config="{ transform: true, parentField: 'pid' }">
						<template #toolbar_buttons>
							<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysRegion/add'"> 新增 </el-button>
							<el-button-group style="padding: 0 12px 0 12px">
								<el-button icon="ele-Expand" @click="handleExpand"> 全部展开 </el-button>
								<el-button icon="ele-Fold" @click="handleFold"> 全部折叠 </el-button>
							</el-button-group>
							<el-dropdown v-auth="'sysRegion/add'" @command="handleCommand">
								<el-button type="danger" icon="ele-Lightning"> 从云端同步 </el-button>
								<template #dropdown>
									<el-dropdown-menu>
										<el-dropdown-item command="amap" icon="ele-Promotion">同步高德地图</el-dropdown-item>
										<el-dropdown-item command="tdt" divided icon="ele-MostlyCloudy">同步天地图</el-dropdown-item>
										<el-dropdown-item command="mca" divided icon="ele-MapLocation">同步国家地名信息库</el-dropdown-item>
										<el-dropdown-item command="mzb" divided icon="ele-Location">同步民政部行政区划</el-dropdown-item>
									</el-dropdown-menu>
								</template>
							</el-dropdown>
						</template>
						<template #toolbar_tools> </template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip content="编辑" placement="top">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysRegion/update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip content="删除" placement="top">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysRegion/delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
							<el-button icon="ele-OfficeBuilding" text type="primary" v-auth="'sysOrg/add'" @click="genOrg(row)"> 生成组织架构 </el-button>
						</template>
					</vxe-grid>
				</el-card>
			</pane>
		</splitpanes>

		<EditRegion ref="editRegionRef" :title="state.title" @handleQuery="handleQuery" />
		<SyncGdParam ref="syncGdParamRef" :title="state.title" @handleQuery="handleQuery" />
		<SyncMcaParam ref="syncMcaParamRef" :title="state.title" @handleQuery="handleQuery" />
		<SyncTdtParam ref="syncTdtParamRef" :title="state.title" @handleQuery="handleQuery" />
		<GenOrgLevel ref="genOrgLevelRef" :title="state.title" @handleQuery="handleQuery" />
	</div>
</template>

<!-- 行政区划 -->
<script lang="ts" setup name="sysRegion">
import { nextTick, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, ElNotification } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { auth } from '/@/utils/authFunction';

import RegionTree from './component/regionTree.vue';
import EditRegion from './component/editRegion.vue';
import SyncGdParam from './component/syncGdParam.vue';
import SyncMcaParam from './component/syncMcaParam.vue';
import SyncTdtParam from './component/syncTdtParam.vue';
import GenOrgLevel from './component/genOrgLevel.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysRegionApi } from '/@/api-services/system/api';
import { SysRegion, PageRegionInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editRegionRef = ref<InstanceType<typeof EditRegion>>();
const syncGdParamRef = ref<InstanceType<typeof SyncGdParam>>();
const syncMcaParamRef = ref<InstanceType<typeof SyncMcaParam>>();
const syncTdtParamRef = ref<InstanceType<typeof SyncTdtParam>>();
const genOrgLevelRef = ref<InstanceType<typeof GenOrgLevel>>();
const regionTreeRef = ref<InstanceType<typeof RegionTree>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysRegion', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		id: -1,
		pid: undefined,
		name: undefined,
		code: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
});

// 表格参数配置
const options = useVxeTable<SysRegion>(
	{
		id: 'sysRegion',
		name: '区域信息',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 100, fixed: 'left' },
			{ field: 'name', title: '行政名称', minWidth: 280, showOverflow: 'tooltip', treeNode: true },
			{ field: 'code', title: '行政代码', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'type', title: '类型', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'cityCode', title: '区号', minWidth: 100, showOverflow: 'tooltip' },
			// { field: 'createTime', title: '修改时间', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'remark', title: '备注', minWidth: 300, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		stripe: false,
		// 多选配置
		checkboxConfig: { range: false },
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
onMounted(() => {
	// 展开表格所有数据，数据量大时请勿开启
	nextTick(() => {
		setTimeout(() => {
			xGrid.value?.setAllTreeExpand(true);
		}, 1000);
	});
});

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageRegionInput;
	return getAPI(SysRegionApi).apiSysRegionPagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.id = -1;
	state.queryParams.pid = undefined;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加行政区域';
	editRegionRef.value?.openDialog({ orderNo: 100 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑行政区域';
	editRegionRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除行政区域：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysRegionApi).apiSysRegionDeletePost({ id: row.id });
			await handleQuery();
			// 编辑删除后更新机构数据
			regionTreeRef.value?.fetchTreeData();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<SysRegion> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysRegion/update')) await handleEdit(row);
	},
};

// 树组件点击
const handleNodeChange = async (node: any) => {
	state.queryParams.pid = node.id;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	await handleQuery();
};

// 从云端同步点击
const handleCommand = async (command: string) => {
	if (command === 'amap') {
		state.title = '同步高德地图';
		syncGdParamRef.value?.openDialog();
	} else if (command === 'tdt') {
		state.title = '同步天地图行政区划';
		syncTdtParamRef.value?.openDialog();
	} else if (command === 'mca') {
		state.title = '同步国家地名信息库';
		syncMcaParamRef.value?.openDialog();
	} else if (command === 'mzb') {
		// state.title = '同步民政部行政区划';
		// syncTdtParamRef.value?.openDialog();
		await syncMzbRegion();
	} else ElMessage.error(`菜单选择有误`);
};

// 同步民政部行政区划
const syncMzbRegion = async () => {
	ElNotification({
		title: '提示',
		message: '努力同步中...',
		type: 'success',
		position: 'bottom-right',
	});
	options.loading = true;
	await getAPI(SysRegionApi).apiSysRegionSyncRegionMzbPost();
	options.loading = false;
	ElMessage.success('生成成功');
	options.loading = false;
};

// 生成组织架构
const genOrg = (row: any) => {
	state.title = '生成/更新系统组织架构';
	genOrgLevelRef.value?.openDialog(row.id);
};

// 全部展开
const handleExpand = () => {
	xGrid.value?.setAllTreeExpand(true);
};

// 全部折叠
const handleFold = () => {
	xGrid.value?.clearTreeExpand();
};
</script>
