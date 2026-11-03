<template>
	<div class="tn-page">
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
					<h2 class="page-hero__title">租户管理</h2>
					<p class="page-hero__desc">维护多租户：ID 隔离共用库、库隔离独立建库，可单独授权菜单与接口黑名单，并一键同步租户库表。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="总数取自分页结果，其余按当前页统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.total }}</span>
							<span class="page-hero__stat-k">租户总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ state.stats.enabled }}</span>
							<span class="page-hero__stat-k">启用中</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.db }}</span>
							<span class="page-hero__stat-k">库隔离</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.id }}</span>
							<span class="page-hero__stat-k">ID 隔离</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区 -->
		<el-card shadow="hover" class="tn-query">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" class="tn-form">
				<el-form-item label="租户名称" prop="name">
					<el-input v-model="state.queryParams.name" class="tn-input" placeholder="租户名称" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="联系电话" prop="phone">
					<el-input v-model="state.queryParams.phone" class="tn-input" placeholder="联系电话" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
				<div class="tn-query-acts">
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysTenant/page'" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</div>
			</el-form>
		</el-card>

		<el-card class="full-table tn-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" :loading="state.isLoading">
				<template #toolbar_buttons>
					<div class="tn-bar">
						<el-button icon="ele-Plus" type="primary" @click="handleAdd" v-auth="'sysTenant/add'"> 新增 </el-button>
						<el-button type="danger" plain icon="ele-Refresh" @click="syncTenantDb"> 同步所有租户数据库 </el-button>
					</div>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_content="{ row }">
					<!-- 展开行：租户档案 -->
					<div class="tn-detail">
						<div class="tn-detail-head">
							<el-avatar shape="square" :size="38" :src="row.logo" class="tn-detail-logo">{{ initialOf(row.name) }}</el-avatar>
							<div class="tn-detail-title">
								<span class="tn-detail-name">{{ row.name }}</span>
								<span class="tn-detail-host" :title="row.host">{{ row.host || '未配置独立域名' }}</span>
							</div>
							<span class="tn-chip" :class="row.tenantType === 0 ? 'is-id' : 'is-db'">{{ row.tenantType === 0 ? 'ID 隔离' : '库隔离' }}</span>
							<span class="tn-chip" :class="row.status === 1 ? 'is-on' : 'is-off'">{{ row.status === 1 ? '启用' : '禁用' }}</span>
						</div>

						<el-descriptions :column="3" border size="small" :label-width="92" class="tn-desc">
							<el-descriptions-item label="租户标识">{{ row.id }}</el-descriptions-item>
							<el-descriptions-item label="主标题">{{ row.title || '-' }}</el-descriptions-item>
							<el-descriptions-item label="副标题">{{ row.viceTitle || '-' }}</el-descriptions-item>
							<el-descriptions-item label="描述" :span="3">{{ row.viceDesc || '-' }}</el-descriptions-item>
							<el-descriptions-item label="水印内容">{{ row.watermark || '-' }}</el-descriptions-item>
							<el-descriptions-item label="版权说明">{{ row.copyright || '-' }}</el-descriptions-item>
							<el-descriptions-item label="ICP备案号">{{ row.icp || '-' }}</el-descriptions-item>
							<el-descriptions-item label="数据库连接" :span="3">{{ row.connection || '-' }}</el-descriptions-item>
							<el-descriptions-item label="从库连接" :span="3">{{ row.slaveConnections || '-' }}</el-descriptions-item>
						</el-descriptions>
					</div>
				</template>
				<template #row_name="{ row }">
					<div class="tn-cell">
						<el-avatar shape="square" :size="24" :src="row.logo" class="tn-cell-logo">{{ initialOf(row.name) }}</el-avatar>
						<span class="tn-cell-name" :title="row.name">{{ row.name }}</span>
					</div>
				</template>
				<template #row_tenantType="{ row }">
					<span class="tn-tag" :class="row.tenantType === 0 ? 'is-id' : 'is-db'">{{ row.tenantType === 0 ? 'ID 隔离' : '库隔离' }}</span>
				</template>
				<template #row_status="scope">
					<el-switch v-model="scope.row.status" :active-value="1" :inactive-value="2" size="small" @change="changeStatus(scope)" :disabled="scope.row.id == 1300000000001" />
				</template>
				<template #row_dbType="{ row }">
					<g-sys-dict v-model="row.dbType" code="db_type" render-as="tag" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="编辑" placement="top">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" v-auth="'sysTenant/update'" />
					</el-tooltip>
					<el-tooltip content="删除" placement="top">
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'sysTenant/delete'" :disabled="row.id == 1300000000001"> </el-button>
					</el-tooltip>
					<el-tooltip content="重置密码" placement="top">
						<el-button icon="ele-RefreshLeft" text type="danger" @click="resetTenantPwd(row)" v-auth="'sysTenant/resetPwd'"> </el-button>
					</el-tooltip>

					<el-button icon="ele-Coin" size="small" text type="danger" @click="createTenantData(row)" :v-auth="'sysTenant/createDb'" v-if="row.tenantType === 0"> 创建租户数据 </el-button>
					<el-button icon="ele-Coin" size="small" text type="danger" @click="createTenantDb(row)" :v-auth="'sysTenant/createDb'" v-else> 创建租户库表 </el-button>
					<el-button icon="ele-Menu" size="small" text type="primary" @click="openGrantMenu(row)" v-auth="'sysTenant/grantMenu'"> 授权菜单 </el-button>
					<el-button icon="ele-Link" size="small" text type="primary" @click="openGrantApi(row)"> 接口黑名单 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditTenant ref="editTenantRef" :title="state.title" @handleQuery="handleQuery" />
		<GrantMenu ref="grantMenuRef" />
		<GrantApi ref="grantApiRef" />
	</div>
</template>

<!-- 租户管理 -->
<script lang="ts" setup name="sysTenant">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditTenant from '/@/views/system/tenant/component/editTenant.vue';
import GrantMenu from '/@/views/system/tenant/component/grantMenu.vue';
import GrantApi from '/@/views/system/role/component/grantApi.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysTenantApi } from '/@/api-services/system/api';
import { PageTenantInput, TenantOutput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editTenantRef = ref<InstanceType<typeof EditTenant>>();
const grantMenuRef = ref<InstanceType<typeof GrantMenu>>();
const grantApiRef = ref<InstanceType<typeof GrantApi>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysTenant', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined,
		phone: undefined,
	},
	localPageParam,
	title: '',
	/** 是否加载中 */
	isLoading: false,
	/** 顶部统计：总数取分页结果，其余按当前页统计 */
	stats: { total: 0, enabled: 0, db: 0, id: 0 },
});

/** 无 logo 时用名称首字做占位 */
const initialOf = (name?: string | null) => (name ? String(name).trim().slice(0, 1) : '租');

// 表格参数配置
const options = useVxeTable<TenantOutput>(
	{
		id: 'sysTenant',
		name: '租户信息',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'expand', type: 'expand', width: 40, slots: { content: 'row_content' } },
			{ field: 'name', title: '租户名称', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'row_name' } },
			{ field: 'adminAccount', title: '租管账号', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'phone', title: '电话', minWidth: 120, showOverflow: 'tooltip' },
			// { field: 'host', title: '域名', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'email', title: '邮箱', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'expirationTime', title: '过期时间', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'userCount', title: '用户数量', showOverflow: 'tooltip' },
			{ field: 'tenantType', title: '租户类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_tenantType' } },
			{ field: 'status', title: '状态', minWidth: 100, slots: { default: 'row_status' } },
			// { field: 'configId', title: '数据库标识', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'dbType', title: '数据库类型', minWidth: 120, showOverflow: 'tooltip', slots: { default: 'row_dbType' } },
			// { field: 'connection', title: '数据库连接', minWidth: 300, showOverflow: 'tooltip' },
			// { field: 'slaveConnections', title: '从库连接', minWidth: 300, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
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
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageTenantInput;
	const res = await getAPI(SysTenantApi).apiSysTenantPagePost(params);
	// 顺带统计：总数用分页 total，其余按当前页汇总
	const items = res.data.result?.items ?? [];
	state.stats.total = res.data.result?.total ?? 0;
	state.stats.enabled = items.filter((t) => Number(t.status) === 1).length;
	state.stats.db = items.filter((t) => Number(t.tenantType) !== 0).length;
	state.stats.id = items.filter((t) => Number(t.tenantType) === 0).length;
	return res;
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	state.queryParams.phone = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加租户';
	editTenantRef.value?.openDialog({ tenantType: 0, orderNo: 100, host: '' });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑租户';
	editTenantRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除租户：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi).apiSysTenantDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<TenantOutput> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysTenant/update')) await handleEdit(row);
	},
};

// 打开授权菜单页面
const openGrantMenu = async (row: any) => {
	grantMenuRef.value?.openDialog(row);
};

// 打开授权接口页面
const openGrantApi = async (row: any) => {
	grantApiRef.value?.openDrawer(row);
};

// 创建租户库
const createTenantDb = (row: any) => {
	ElMessageBox.confirm(`确定创建/更新租户数据库：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			try {
				state.isLoading = true;
				await getAPI(SysTenantApi).apiSysTenantInitTenantDbPost({ id: row.id });
				ElMessage.success('创建/更新租户数据库成功');
			} finally {
				state.isLoading = false;
			}
		})
		.catch(() => {});
};

// 创建租户数据
const createTenantData = (row: any) => {
	ElMessageBox.confirm(`确定创建/更新租户数据：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi).apiSysTenantInitTenantDataPost({ id: row.id });
			ElMessage.success('创建/更新租户数据成功');
		})
		.catch(() => {});
};

// 修改状态
const changeStatus = async (scope: any) => {
	getAPI(SysTenantApi)
		.apiSysTenantSetStatusPost({ id: scope.row.id, status: scope.row.status })
		.then(() => {
			ElMessage.success('租户状态设置成功');
		})
		.catch(async () => {
			scope.row.status = scope.row.status === 1 ? 2 : 1;
			await xGrid.value?.updateStatus(scope);
		});
};

// 重置密码
const resetTenantPwd = async (row: any) => {
	ElMessageBox.confirm(`确定重置密码：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi)
				.apiSysTenantResetPwdPost({ userId: row.userId })
				.then((res) => {
					ElMessage.success(`密码重置成功为：${res.data.result}`);
				});
		})
		.catch(() => {});
};

// 同步所有租户数据库
const syncTenantDb = () => {
	ElMessageBox.confirm(`确定同步所有租户数据库?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			options.loading = true;
			await getAPI(SysTenantApi).apiSysTenantSyncTenantDbPost();
			ElMessage.success('同步成功');
			options.loading = false;
		})
		.catch(() => {});
};
</script>

<style lang="scss" scoped>
.tn-page {
	/* 页边距统一由 .layout-parent 的 padding:5px 提供（与打印模板页一致），
	   这里不能再叠加 5px，否则上/右/左都会变成 10px；底部补 4px 与打印页对齐 */
	position: relative;
	padding-bottom: 4px;

	/* 表格页根节点是 flex 列（layout-parent > div:has(.full-table)），头图与查询区不许被压缩 */
	.page-hero,
	.tn-query {
		flex-shrink: 0;
	}
}

/* ===================== 查询区 ===================== */
.tn-query {
	margin-top: 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);

	:deep(.el-card__body) {
		padding: 14px 18px 4px;
	}
}

.tn-form {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 12px;
	}
}

.tn-input {
	width: 200px;
}

/* 查询 / 重置 固定为按钮组，紧跟查询条件 */
.tn-query-acts {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 12px;

	:deep(.el-button-group) {
		display: inline-flex;
	}
}

/* ===================== 表格卡 ===================== */
.tn-table {
	margin-top: 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
}

.tn-bar {
	display: flex;
	align-items: center;
	gap: 9px;
	flex-wrap: wrap;
	width: 100%;
	min-width: 0;
}

.tn-bar-ico {
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	font-size: 14px;
	color: var(--el-color-white);
	background: linear-gradient(135deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary), #ffffff 30%));
}

.tn-bar-title {
	margin-right: 4px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

/* ===================== 单元格 ===================== */
.tn-cell {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.tn-cell-logo {
	flex-shrink: 0;
	border-radius: 7px;
	font-size: 11px;
	font-weight: 700;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.tn-cell-name {
	min-width: 0;
	font-weight: 500;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 类型 / 状态小胶囊：表格内与展开行共用 */
.tn-tag,
.tn-chip {
	display: inline-flex;
	align-items: center;
	flex-shrink: 0;
	padding: 0 8px;
	line-height: 20px;
	border-radius: 999px;
	font-size: 11.5px;
	font-weight: 600;
	white-space: nowrap;

	&.is-id {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}

	&.is-db {
		color: var(--el-color-primary);
		background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
	}

	&.is-on {
		color: var(--el-color-success);
		background: color-mix(in srgb, var(--el-color-success) 12%, transparent);
	}

	&.is-off {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}
}

/* ===================== 展开行：租户档案 ===================== */
.tn-detail {
	margin: 8px;
	padding: 12px 14px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}

.tn-detail-head {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
	padding-bottom: 10px;
	margin-bottom: 10px;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}

.tn-detail-logo {
	flex-shrink: 0;
	border-radius: 10px;
	font-size: 16px;
	font-weight: 700;
	color: var(--el-color-white);
	background: linear-gradient(135deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary), #ffffff 32%));
}

.tn-detail-title {
	display: flex;
	flex-direction: column;
	gap: 2px;
	flex: 1 1 auto;
	min-width: 0;
}

.tn-detail-name {
	min-width: 0;
	font-size: 14px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.tn-detail-host {
	min-width: 0;
	font-size: 11.5px;
	font-family: Consolas, Monaco, monospace;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.tn-desc {
	:deep(.el-descriptions__label) {
		font-weight: 500;
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color-lighter);
	}

	:deep(.el-descriptions__content) {
		font-size: 12.5px;
		word-break: break-all;
	}
}
</style>
