<template>
	<div class="og-page">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">机构管理</h2>
					<p class="page-hero__desc">维护组织架构树：机构层级、编码、类型与经纬度，支持整棵复制、逐级展开与快捷查询。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ogStats.total }}</span>
							<span class="page-hero__stat-k">机构总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ ogStats.enabled }}</span>
							<span class="page-hero__stat-k">启用中</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ogStats.deep }}</span>
							<span class="page-hero__stat-k">最大层级</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 左树 + 右查询/表格 -->
		<div class="og-body">
			<splitpanes class="default-theme">
				<pane size="18" style="display: flex">
					<OrgTree ref="orgTreeRef" @node-click="handleNodeChange" />
				</pane>
				<pane size="82" style="display: flex; flex-direction: column">
					<el-card shadow="hover" class="og-query">
						<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" class="og-form">
							<el-form-item label="机构名称" prop="name">
								<el-input v-model="state.queryParams.name" class="og-input" placeholder="机构名称" clearable @keyup.enter.native="handleQuery(true)" />
							</el-form-item>
							<el-form-item label="机构编码" prop="code">
								<el-input v-model="state.queryParams.code" class="og-input" placeholder="机构编码" clearable @keyup.enter.native="handleQuery(true)" />
							</el-form-item>
							<el-form-item label="机构类型" prop="type">
								<el-select v-model="state.queryParams.type" class="og-select" filterable clearable placeholder="全部类型" @clear="state.queryParams.type = undefined">
									<el-option v-for="item in state.orgTypeList" :key="item.id" :label="item.label" :value="item.value" />
								</el-select>
							</el-form-item>
							<div class="og-query-acts">
								<el-button-group>
									<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
									<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
								</el-button-group>
							</div>
						</el-form>
					</el-card>

					<el-card class="full-table og-table" shadow="hover">
						<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
							<template #toolbar_buttons>
								<div class="og-bar">
									<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysOrg/add'"> 新增 </el-button>
									<el-button-group>
										<el-button icon="ele-Expand" @click="handleExpand"> 全部展开 </el-button>
										<el-button icon="ele-Fold" @click="handleFold"> 全部折叠 </el-button>
									</el-button-group>
								</div>
							</template>
							<template #toolbar_tools> </template>
							<template #empty>
								<el-empty :image-size="200" />
							</template>
							<template #row_status="{ row }">
								<el-tag v-if="row.status === 1" type="success" effect="light" round size="small">启用</el-tag>
								<el-tag v-else type="danger" effect="light" round size="small">禁用</el-tag>
							</template>
							<template #row_record="{ row }">
								<ModifyRecord :data="row" />
							</template>
							<template #row_buttons="{ row }">
								<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'sysOrg/update'"> 编辑 </el-button>
								<el-button icon="ele-CopyDocument" text type="primary" @click="openCopyOrg(row)" v-auth="'sysOrg/add'"> 复制 </el-button>
								<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'sysOrg/delete'"> 删除 </el-button>
							</template>
						</vxe-grid>
					</el-card>
				</pane>
			</splitpanes>
		</div>

		<EditOrg ref="editOrgRef" :title="state.title" :orgData="state.treeData" @reload="handleQuery" />
	</div>
</template>

<!-- 组织架构 -->
<script lang="ts" setup name="sysOrg">
import { computed, onMounted, reactive, ref, nextTick } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { auth } from '/@/utils/authFunction';

import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditOrg from '/@/views/system/org/component/editOrg.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysDictDataApi, SysOrgApi } from '/@/api-services/system/api';
import { SysOrg, UpdateOrgInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editOrgRef = ref<InstanceType<typeof EditOrg>>();
const orgTreeRef = ref<InstanceType<typeof OrgTree>>();
const state = reactive({
	treeData: [] as Array<SysOrg>, // 机构树所有数据
	queryParams: {
		id: 0,
		name: undefined,
		code: undefined,
		type: undefined,
	},
	title: '',
	orgTypeList: [] as any,
});

// 头图统计：递归整棵树（父节点+所有子节点），不额外请求接口
const ogStats = computed(() => {
	let total = 0;
	let enabled = 0;
	let deep = 0;
	const walk = (nodes: Array<SysOrg> | undefined, level: number) => {
		for (const n of nodes ?? []) {
			total += 1;
			if (n.status === 1) enabled += 1;
			deep = Math.max(deep, level);
			walk(n.children, level + 1);
		}
	};
	walk(state.treeData, 1);
	return { total, enabled, deep };
});

// 表格参数配置
const options = useVxeTable<SysOrg>(
	{
		id: 'sysOrg',
		name: '机构组织',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '机构名称', minWidth: 200, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center' },
			{ field: 'code', title: '机构编码', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'level', title: '级别', minWidth: 70, showOverflow: 'tooltip' },
			{
				field: 'type',
				title: '机构类型',
				minWidth: 80,
				formatter: ({ cellValue }: any) => state.orgTypeList.find((u: any) => u.value == cellValue)?.label,
				showOverflow: 'tooltip',
			},
			{ field: 'longitude', title: '经度', showOverflow: 'tooltip' },
			{ field: 'latitude', title: '纬度', showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 80, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		stripe: false,
		// 多选配置
		checkboxConfig: { range: false },
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
		// 树形配置
		treeConfig: { expandAll: false },
	}
);

// 页面初始化
onMounted(async () => {
	let resDicData = await getAPI(SysDictDataApi).apiSysDictDataDataListCodeGet('org_type');
	state.orgTypeList = resDicData.data.result;
	// 展开表格所有数据，数据量大时请勿开启
	nextTick(async () => {
		if (state.treeData?.length < 100) await xGrid.value?.setAllTreeExpand(true);
	});
});

// 查询api
const handleQueryApi = async () => {
	const params = Object.assign(state.queryParams);
	return getAPI(SysOrgApi).apiSysOrgListGet(params.id, params.name, params.code, params.type);
};

// 查询操作
const handleQuery = async (updateTree: boolean = false) => {
	options.loading = true;
	var res = await handleQueryApi();
	xGrid.value?.loadData(res.data.result ?? []);
	options.loading = false;
	// 是否更新左侧机构列表树
	if (updateTree == true) {
		const data = await orgTreeRef.value?.fetchTreeData();
		state.treeData = data ?? [];
	}
	// 若无选择节点并且查询条件为空时，更新编辑页面机构列表树
	if (state.queryParams.id == 0 && state.queryParams.name == undefined && state.queryParams.code == undefined && state.queryParams.type == undefined && updateTree == false)
		state.treeData = res.data.result ?? [];
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.id = 0;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.type = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加机构';
	editOrgRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑机构';
	editOrgRef.value?.openDialog(row);
};

// 打开复制页面
const openCopyOrg = (row: any) => {
	state.title = '复制机构';
	var copyRow = JSON.parse(JSON.stringify(row)) as UpdateOrgInput;
	copyRow.id = 0;
	copyRow.name = '';
	editOrgRef.value?.openDialog(copyRow);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除机构：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			getAPI(SysOrgApi)
				.apiSysOrgDeletePost({ id: row.id })
				.then(async (res: any) => {
					if (res.data.code == 200) {
						ElMessage.success('删除成功');
						await handleQuery(true);
					}
				});
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<SysOrg> = {
	// 只对 proxy-config.ajax.query 配置时有效，当手动点击查询时会触发该事件
	async proxyQuery() {
		state.treeData = xGrid.value?.getTableData().tableData ?? [];
	},
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysOrg/update')) await handleEdit(row);
	},
};

// 树组件点击
const handleNodeChange = async (node: any) => {
	state.queryParams.id = node.id;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.type = undefined;
	orgTreeRef?.value?.setCurrentKey();
	await handleQuery();
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

<style lang="scss" scoped>
.og-page {
	/* 页边距由 .layout-parent 的 padding:5px 提供，这里只补底部 */
	position: relative;
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;
	padding-bottom: 4px;
	overflow: hidden;
}

/* ===================== 左侧树 + 右侧内容 ===================== */
.og-body {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;
	display: flex;

	:deep(.splitpanes) {
		width: 100%;
		height: 100%;
	}
}

/* ===================== 查询区 ===================== */
.og-query {
	flex-shrink: 0;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);

	:deep(.el-card__body) {
		padding: 14px 18px 4px;
	}
}

.og-form {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 12px;
	}
}

.og-input {
	width: 200px;
}

.og-select {
	width: 160px;
}

/* 查询 / 重置 固定为按钮组，紧跟最后一个条件 */
.og-query-acts {
	display: flex;
	align-items: center;
	margin-bottom: 12px;

	:deep(.el-button-group) {
		display: inline-flex;
	}
}

/* ===================== 表格卡 ===================== */
.og-table {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
}

.og-bar {
	display: flex;
	align-items: center;
	gap: 9px;
	flex-wrap: wrap;
}
</style>
