<template>
	<div class="mn-page">
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
					<h2 class="page-hero__title">菜单管理</h2>
					<p class="page-hero__desc">维护平台目录、菜单与按钮权限：菜单树的层级决定侧边栏结构，权限标识决定接口能否访问。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="按当前查询出的菜单树统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.total }}</span>
							<span class="page-hero__stat-k">菜单总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.dir }}</span>
							<span class="page-hero__stat-k">目录</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ state.stats.menu }}</span>
							<span class="page-hero__stat-k">菜单</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.stats.btn }}</span>
							<span class="page-hero__stat-k">按钮</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：按钮紧跟查询条件，不再靠右对齐 -->
		<el-card shadow="hover" class="mn-query">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" class="mn-form">
				<el-form-item label="菜单名称" prop="title">
					<el-input v-model="state.queryParams.title" class="mn-input" placeholder="菜单名称" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="类型" prop="type">
					<el-select v-model="state.queryParams.type" class="mn-select" placeholder="类型" clearable @clear="state.queryParams.type = undefined">
						<el-option label="目录" :value="1" />
						<el-option label="菜单" :value="2" />
						<el-option label="按钮" :value="3" />
					</el-select>
				</el-form-item>
				<div class="mn-query-acts">
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysMenu/list'" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</div>
			</el-form>
		</el-card>

		<el-card class="full-table mn-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style mn-grid" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<div class="mn-bar">
						<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysMenu/add'"> 新增 </el-button>
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
				<template #row_title="{ row }">
					<div class="mn-cell">
						<!-- 图标只保留纯色描边，不再套圆角底色块 -->
						<span class="mn-cell-ico" :class="typeTone(row.type)">
							<SvgIcon v-if="row.icon" :name="row.icon" :size="16" />
							<el-icon v-else :size="16">
								<ele-FolderOpened v-if="row.type == 1" />
								<ele-Menu v-else-if="row.type == 2" />
								<ele-Pointer v-else />
							</el-icon>
						</span>
						<span class="mn-cell-name">{{ row.title }}</span>
					</div>
				</template>
				<template #row_type="{ row }">
					<g-sys-dict v-model="row.type" code="MenuTypeEnum" />
				</template>
				<template #row_status="{ row }">
					<el-switch v-model="row.status" :active-value="1" :inactive-value="2" size="small" @change="changeStatus(row)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-Edit" text type="primary" v-auth="'sysMenu/update'" @click="handleEdit(row)"> 编辑 </el-button>
					<el-button icon="ele-Delete" text type="danger" v-auth="'sysMenu/delete'" @click="handleDelete(row)"> 删除 </el-button>
					<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysMenu/add'" @click="openCopyMenu(row)"> 复制 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditMenu ref="editMenuRef" :title="state.title" :menuData="state.menuData" @handleQuery="handleQuery(false)" />
	</div>
</template>

<!-- 菜单管理 -->
<script lang="ts" setup name="sysMenu">
import { reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import SvgIcon from '/@/components/svgIcon/index.vue';
import { auth } from '/@/utils/authFunction';

import EditMenu from '/@/views/system/menu/component/editMenu.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi } from '/@/api-services/system/api';
import { SysMenu, UpdateMenuInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editMenuRef = ref<InstanceType<typeof EditMenu>>();
const state = reactive({
	menuData: [] as Array<SysMenu>,
	queryParams: {
		title: undefined,
		type: undefined,
	},
	title: '',
	/** 顶部统计：按整棵菜单树递归统计（1 目录 / 2 菜单 / 3 按钮） */
	stats: { total: 0, dir: 0, menu: 0, btn: 0 },
});

// 递归统计菜单树
const countMenus = (list: Array<SysMenu> | null | undefined) => {
	const r = { total: 0, dir: 0, menu: 0, btn: 0 };
	const walk = (items: Array<SysMenu> | null | undefined) => {
		(items ?? []).forEach((m) => {
			r.total += 1;
			const t = Number(m.type);
			if (t === 1) r.dir += 1;
			else if (t === 2) r.menu += 1;
			else if (t === 3) r.btn += 1;
			walk(m.children);
		});
	};
	walk(list);
	return r;
};

// 菜单类型对应的图标底色（目录 / 菜单 / 按钮）
const typeTone = (type?: number | null) => {
	switch (Number(type)) {
		case 1:
			return 'is-dir';
		case 3:
			return 'is-btn';
		default:
			return 'is-menu';
	}
};

// 表格参数配置
const options = useVxeTable<SysMenu>(
	{
		id: 'sysMenu',
		name: '菜单信息',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 80, fixed: 'left' },
			{ field: 'title', title: '菜单名称', minWidth: 180, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center', slots: { default: 'row_title' } },
			{ field: 'type', title: '菜单类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_type' } },
			{ field: 'path', title: '路由路径', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'component', title: '组件路径', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'permission', title: '权限标识', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 100, showOverflow: 'tooltip', slots: { default: 'row_status' } },
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
		treeConfig: { expandAll: false, reserve: true },
		// 行配置信息
		rowConfig: {
			keyField: 'id',
		},
	}
);

// 查询api
const handleQueryApi = async () => {
	const params = Object.assign(state.queryParams);
	const res = await getAPI(SysMenuApi).apiSysMenuListGet(params.title, params.type);
	state.stats = countMenus(res.data.result ?? []);
	return res;
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

// 上级菜单候选树：列表带筛选条件时后端返回扁平列表且无层级，故单独取全量树
const loadMenuTree = async () => {
	const res = await getAPI(SysMenuApi).apiSysMenuListGet();
	state.menuData = res.data.result ?? [];
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.title = undefined;
	state.queryParams.type = undefined;
	// 调用vxe-grid的commitProxy(reload)方法，触发表格重新加载数据
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = async () => {
	state.title = '添加菜单';
	editMenuRef.value?.openDialog({ type: 2, isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, status: 1, orderNo: 100 });
	await loadMenuTree();
};

// 打开编辑页面
const handleEdit = async (row: any) => {
	state.title = '编辑菜单';
	editMenuRef.value?.openDialog(row);
	await loadMenuTree();
};

// 打开复制页面
const openCopyMenu = async (row: any) => {
	state.title = '复制菜单';
	var copyRow = JSON.parse(JSON.stringify(row)) as UpdateMenuInput;
	copyRow.id = 0;
	copyRow.title = '';
	editMenuRef.value?.openDialog(copyRow);
	await loadMenuTree();
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除菜单：${row.title}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysMenuApi).apiSysMenuDeletePost({ id: row.id });
			ElMessage.success('删除成功');
			await handleQuery(false);
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<SysMenu> = {
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysMenu/update')) await handleEdit(row);
	},
};

// 全部展开
const handleExpand = () => {
	xGrid.value?.setAllTreeExpand(true);
};

// 全部折叠
const handleFold = () => {
	xGrid.value?.clearTreeExpand();
};

// 修改状态
const changeStatus = async (row: any) => {
	await getAPI(SysMenuApi)
		.apiSysMenuSetStatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success('操作成功');
		})
		.catch(() => {
			row.status = row.status == 1 ? 2 : 1;
		});
};
</script>

<style lang="scss" scoped>
.mn-page {
	/* 页边距统一由 .layout-parent 的 padding:5px 提供（与打印模板页一致），这里不再叠加 */
	position: relative;
	padding-bottom: 4px;

	.page-hero {
		flex-shrink: 0;
	}
}

/* ===================== 查询区 ===================== */
.mn-query {
	flex-shrink: 0;
	margin-top: 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);

	:deep(.el-card__body) {
		padding: 14px 18px 4px;
	}
}

.mn-form {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 12px;
	}
}

/* 查询控件给足宽度：默认在 flex 行里会被压窄，下拉框文字显示不全 */
.mn-input {
	width: 220px;
}

.mn-select {
	width: 160px;
}

/* 查询 / 重置 固定为按钮组，紧跟最后一个查询条件（全站统一约定） */
.mn-query-acts {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 12px;

	:deep(.el-button-group) {
		display: inline-flex;
	}
}

/* ===================== 表格卡 ===================== */
.mn-table {
	margin-top: 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
}

/* 工具栏：左侧固定为操作区（标题 + 按钮），统计只在头图右上角出现 */
.mn-bar {
	display: flex;
	align-items: center;
	gap: 9px;
	width: 100%;
	min-width: 0;
}

.mn-bar-ico {
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

.mn-bar-title {
	margin-right: 4px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

/* ===================== 单元格 ===================== */
.mn-cell {
	display: flex;
	align-items: center;
	gap: 7px;
	min-width: 0;
}

/* 图标不再有圆角底块，只靠颜色区分目录 / 菜单 / 按钮 */
.mn-cell-ico {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 16px;
	height: 16px;
	font-size: 16px;
	color: var(--el-color-primary);

	&.is-dir {
		color: var(--el-color-warning);
	}

	&.is-menu {
		color: var(--el-color-primary);
	}

	&.is-btn {
		color: var(--el-color-info);
	}
}

.mn-cell-name {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
</style>
