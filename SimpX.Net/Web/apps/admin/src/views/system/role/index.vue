<template>
	<div class="sys-role-container">
		<!-- 查询区：全站统一的 .query-toolbar（与 AI 智能体 · 工具管理同一套视觉语言） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="角色名称" prop="name">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="请输入角色名称" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="角色编码" prop="code">
					<el-input v-model="state.queryParams.code" class="query-input" placeholder="请输入角色编码" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
			</el-form>

			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleSearch" v-auth="'sysRole/page'" :loading="options.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading">重置</el-button>
				</el-button-group>
				<!-- 卡片视图没有 vxe 工具栏，操作按钮统一挂在这里 -->
				<el-button v-if="state.viewMode === 'card'" type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysRole/add'">新增</el-button>
				<!-- 视图切换：用 element 原生按钮组，尺寸与查询/重置一致 -->
				<el-button-group>
					<el-button type="primary" :plain="state.viewMode !== 'card'" icon="ele-Grid" @click="switchView('card')">卡片</el-button>
					<el-button type="primary" :plain="state.viewMode !== 'table'" icon="ele-List" @click="switchView('table')">表格</el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 卡片视图（默认）：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<el-card v-if="state.viewMode === 'card'" class="full-table" shadow="hover">
			<div class="card-grid" v-loading="options.loading">
				<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的角色" :image-size="120" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable"
							:class="'is-primary'"
							@dblclick="onCardDblclick(row)"
						>
							<!-- 背景水印图标：全局 .entity-card__watermark 统一压在右上角 -->
							<i class="entity-card__watermark"><el-icon><ele-Key /></el-icon></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Key /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
									<div class="entity-card__sub">{{ row.code || '—' }}</div>
								</div>
								<el-tag size="small" effect="light" :type="Number(row.status) === 1 ? 'success' : 'info'">
									{{ Number(row.status) === 1 ? '启用' : '禁用' }}
								</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc" :title="row.remark">{{ row.remark || '暂无备注' }}</div>

								<div class="metric-group">
									<div class="metric-item">
										<span>数据范围</span>
										<b :title="dataScopeText(row.dataScope)">{{ dataScopeText(row.dataScope) }}</b>
									</div>
									<div class="metric-item">
										<span>排序</span>
										<b>{{ row.orderNo ?? '—' }}</b>
									</div>
									<div class="metric-item">
										<span>内置</span>
										<b><g-sys-dict v-model="row.sysFlag" code="YesNoEnum" size="small" /></b>
									</div>
									<div class="metric-item">
										<span>租户</span>
										<b :title="row.tenantName">{{ row.tenantName || '—' }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ formatTime(row.updateTime || row.createTime) }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysRole/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="复制" placement="top">
										<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysRole/add'" @click.stop="handleCopy(row)" />
									</el-tooltip>
									<el-tooltip content="授权菜单" placement="top">
										<el-button icon="ele-Menu" text type="primary" v-auth="'sysRole/grantMenu'" @click.stop="openGrantMenu(row)" />
									</el-tooltip>
									<el-tooltip content="授权数据" placement="top">
										<el-button icon="ele-OfficeBuilding" text type="primary" v-auth="'sysRole/grantDataScope'" @click.stop="openGrantData(row)" />
									</el-tooltip>
									<el-tooltip content="字段黑名单" placement="top">
										<el-button icon="ele-Grid" text type="primary" v-auth="'sysRole/grantTable'" @click.stop="openGrantTable(row)" />
									</el-tooltip>
									<el-tooltip content="接口黑名单" placement="top">
										<el-button icon="ele-Link" text type="primary" v-auth="'sysRole/grantApi'" @click.stop="openGrantApi(row)" />
									</el-tooltip>
									<el-tooltip content="授权账号" placement="top">
										<el-button icon="ele-User" text type="primary" v-auth="'sysRole/grantApi'" @click.stop="openGrantUser(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysRole/delete'" @click.stop="handleDelete(row)" />
									</el-tooltip>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				v-if="cardPager.total > 0"
				class="table-pager"
				v-model:current-page="cardPager.page"
				v-model:page-size="cardPager.pageSize"
				:page-sizes="[12, 24, 48, 96]"
				:total="cardPager.total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onCardSizeChange"
				@current-change="loadCardList"
			/>
		</el-card>

		<el-card v-else class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysRole/add'"> 新增 </el-button>
				</template>
				<template #toolbar_tools></template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_dataScope="{ row }">
					<el-tag v-if="row.dataScope === 1">全部数据</el-tag>
					<el-tag v-else-if="row.dataScope === 2">本部门及以下数据</el-tag>
					<el-tag v-else-if="row.dataScope === 3">本部门数据</el-tag>
					<el-tag v-else-if="row.dataScope === 4">仅本人数据</el-tag>
					<el-tag v-else-if="row.dataScope === 5">自定义数据</el-tag>
				</template>
				<template #row_sysFlag="{ row }">
					<g-sys-dict v-model="row.sysFlag" code="YesNoEnum" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="success">启用</el-tag>
					<el-tag v-else type="danger">禁用</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="编辑" placement="top">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" v-auth="'sysRole/update'"></el-button>
					</el-tooltip>
					<el-tooltip content="删除" placement="top">
						<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(row)" v-auth="'sysRole/delete'"></el-button>
					</el-tooltip>
					<el-tooltip content="复制" placement="top">
						<el-button icon="ele-CopyDocument" text type="primary" @click="handleCopy(row)" v-auth="'sysRole/add'"></el-button>
					</el-tooltip>
					<el-button icon="ele-Menu" size="small" text type="primary" @click="openGrantMenu(row)" v-auth="'sysRole/grantMenu'">授权菜单</el-button>
					<el-button icon="ele-OfficeBuilding" size="small" text type="primary" @click="openGrantData(row)" v-auth="'sysRole/grantDataScope'">授权数据</el-button>
					<el-button icon="ele-Grid" size="small" text type="primary" @click="openGrantTable(row)" v-auth="'sysRole/grantTable'">字段黑名单</el-button>
					<el-button icon="ele-Link" size="small" text type="primary" @click="openGrantApi(row)" v-auth="'sysRole/grantApi'">接口黑名单</el-button>
					<el-button icon="ele-User" size="small" text type="primary" @click="openGrantUser(row)" v-auth="'sysRole/grantApi'">授权账号</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditRole ref="editRoleRef" :title="state.title" @handleQuery="refresh" />
		<GrantMenu ref="grantMenuRef" />
		<GrantTable ref="grantTableRef" />
		<GrantData ref="grantDataRef" @handleQuery="refresh" />
		<GrantApi ref="grantApiRef" />
		<GrantUser ref="grantUserRef" v-model:visible="state.grantUserVisible" :data="state.roleUsers" @change="closeGrantUser" />
	</div>
</template>

<!-- 角色管理 -->
<script lang="ts" setup name="sysRole">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditRole from '/@/views/system/role/component/editRole.vue';
import GrantMenu from '/@/views/system/role/component/grantMenu.vue';
import GrantTable from '/@/views/system/role/component/grantTable.vue';
import GrantData from '/@/views/system/role/component/grantData.vue';
import GrantApi from '/@/views/system/role/component/grantApi.vue';
import GrantUser from '/@/components/selector/userSelectorDialog.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysRoleApi } from '/@/api-services/system/api';
import { PageRoleInput, PageRoleOutput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editRoleRef = ref<InstanceType<typeof EditRole>>();
const grantMenuRef = ref<InstanceType<typeof GrantMenu>>();
const grantTableRef = ref<InstanceType<typeof GrantTable>>();
const grantDataRef = ref<InstanceType<typeof GrantData>>();
const grantApiRef = ref<InstanceType<typeof GrantApi>>();
const grantUserRef = ref<InstanceType<typeof GrantUser>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysRole', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
	currentRoleRow: {} as any, // 当前选择的角色
	grantUserVisible: false, // 账号选择器
	roleUsers: [] as any, // 当前角色绑定用户集合
	/** 视图模式：card 卡片（默认） / table 表格 */
	viewMode: 'card' as 'card' | 'table',
	/** 卡片视图数据 */
	cardList: [] as PageRoleOutput[],
});

// 卡片视图分页
const cardPager = reactive({ page: 1, pageSize: 12, total: 0 });

// 表格参数配置
const options = useVxeTable<PageRoleOutput>(
	{
		id: 'sysRole',
		name: '角色',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '角色名称', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'code', title: '角色编码', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'dataScope', title: '数据范围', minWidth: 150, showOverflow: 'tooltip', slots: { default: 'row_dataScope' } },
			{ field: 'sysFlag', title: '是否内置', width: 80, showOverflow: 'tooltip', slots: { default: 'row_sysFlag' } },
			{ field: 'tenantName', title: '租户名称', minWidth: 180, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 80, showOverflow: 'tooltip', slots: { default: 'row_status' } },
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
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageRoleInput;
	return getAPI(SysRoleApi).apiSysRolePagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 卡片视图取数
const loadCardList = async () => {
	options.loading = true;
	try {
		const params = {
			...state.queryParams,
			page: cardPager.page,
			pageSize: cardPager.pageSize,
			field: localPageParam.defaultSort.field,
			order: localPageParam.defaultSort.order,
			descStr: 'desc',
		} as PageRoleInput;
		const res = await getAPI(SysRoleApi).apiSysRolePagePost(params);
		state.cardList = (res.data.result?.items ?? []) as PageRoleOutput[];
		cardPager.total = Number(res.data.result?.total ?? 0);
	} finally {
		options.loading = false;
	}
};

// 刷新当前视图
const refresh = async () => {
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery();
};

// 切换视图：卡片需要主动取数，表格由 vxe 挂载后 autoLoad 触发
const switchView = (mode: 'card' | 'table') => {
	if (state.viewMode === mode) return;
	state.viewMode = mode;
	if (mode === 'card') loadCardList();
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery(true);
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await xGrid.value?.commitProxy('reload');
};

// 卡片每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 卡片双击进入编辑
const onCardDblclick = (row: any) => {
	if (auth('sysRole/update')) handleEdit(row);
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加角色';
	editRoleRef.value?.openDialog({ id: undefined, status: 1, orderNo: 100 });
};

// 打开编辑页面
const handleEdit = async (row: any) => {
	state.title = '编辑角色';
	editRoleRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除角色：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysRoleApi).apiSysRoleDeletePost({ id: row.id });
			await refresh();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 复制
const handleCopy = (row: any) => {
	ElMessageBox.confirm(`确定复制角色：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysRoleApi).apiSysRoleCopyPost({ id: row.id });
			await refresh();
			ElMessage.success('复制成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<PageRoleOutput> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysRole/update')) await handleEdit(row);
	},
};

// 打开授权菜单页面
const openGrantMenu = (row: any) => {
	grantMenuRef.value?.openDrawer(row);
};

// 打开授权表格页面
const openGrantTable = (row: any) => {
	grantTableRef.value?.openDrawer(row);
};

// 打开授权数据范围页面
const openGrantData = (row: any) => {
	grantDataRef.value?.openDialog(row);
};

// 打开授权接口资源页面
const openGrantApi = (row: any) => {
	grantApiRef.value?.openDrawer(row);
};

// 打开授权账号页面
const openGrantUser = async (row: any) => {
	var res = await getAPI(SysRoleApi).apiSysRoleOwnUserListGet(row.id);
	state.roleUsers = res.data.result ?? [];
	state.currentRoleRow = row;
	state.grantUserVisible = true;
};

// 关闭授权账号页面并授权
const closeGrantUser = (data: any) => {
	var userIds = data.map((u: any) => u.id);
	getAPI(SysRoleApi).apiSysRoleGrantUserPost({ id: state.currentRoleRow.id, userIdList: userIds });
	state.grantUserVisible = false;
};

/* ------------------------------ 展示辅助 ------------------------------ */
const dataScopeText = (scope: any) => {
	const map: Record<number, string> = {
		1: '全部数据',
		2: '本部门及以下数据',
		3: '本部门数据',
		4: '仅本人数据',
		5: '自定义数据',
	};
	return map[Number(scope)] ?? '—';
};

const formatTime = (val: any) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm') : String(val);
};

// 页面初始化：默认卡片视图
loadCardList();
</script>

<style lang="scss" scoped>
/* 卡片视图与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.sys-role-container {
	position: relative;

	/* 表格模式下根节点是 flex 列（.layout-full-table），查询区不许被压缩 */
	> .query-toolbar {
		flex-shrink: 0;
		// margin-top: 14px;
	}
}
</style>
