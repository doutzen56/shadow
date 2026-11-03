<template>
	<div class="gm-container">
		<el-drawer v-model="state.isVisible" size="56%" class="gm-drawer">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Menu /></el-icon>
					<span>{{ state.drawerTitle }}</span>
				</div>
			</template>

			<NoticeBar leftIcon="iconfont icon-tongzhi2" text="非超管角色禁止授权系统核心模块菜单资源" :scrollable="true" />

			<el-card class="gm-card" shadow="hover">
				<template #header>
					<div class="gm-head">
						<el-input v-model="filterText" prefix-icon="ele-Search" clearable placeholder="菜单名称 / 路由 / 权限标识" class="gm-search" />
						<div class="gm-head-acts">
							<span class="gm-count">已选 <b>{{ state.leafCount }}</b> 项</span>
							<el-button link type="primary" @click="checkAll(true)">全选</el-button>
							<el-button link @click="checkAll(false)">清空</el-button>
						</div>
					</div>
				</template>

				<!-- 抽屉里唯一的滚动容器 -->
				<div class="gm-body" v-loading="state.loading">
					<table v-if="tableGroups.length" class="gm-table">
						<colgroup>
							<col class="col-dir" />
							<col class="col-menu" />
							<col />
						</colgroup>
						<thead>
							<tr>
								<th>一级目录</th>
								<th>菜单</th>
								<th>按钮授权</th>
							</tr>
						</thead>
						<tbody>
							<template v-for="g in tableGroups" :key="g.id">
								<tr v-for="(r, i) in g.rows" :key="r.id" :class="{ 'is-on': state.checkedKeys.has(r.id) }">
									<!-- 一级目录：跨该组所有行 -->
									<td v-if="i === 0" :rowspan="g.rows.length" class="gm-td-dir" :title="g.title">
										<el-checkbox :model-value="state.checkedKeys.has(g.id)" :indeterminate="state.halfKeys.has(g.id)" @change="(v) => toggleNode(g.node, !!v)">
											<span class="gm-dir-name" :class="{ 'is-plain': !g.isDir }">{{ g.title }}</span>
										</el-checkbox>
									</td>

									<!-- 菜单：深度缩进 + 父级路径 -->
									<td class="gm-td-menu">
										<div class="gm-menu" :style="{ paddingLeft: r.depth * 16 + 'px' }">
											<span v-if="r.node.id === g.id" class="gm-menu-name is-plain">本级按钮</span>
											<el-checkbox v-else :model-value="state.checkedKeys.has(r.node.id)" :indeterminate="state.halfKeys.has(r.node.id)" @change="(v) => toggleNode(r.node, !!v)">
												<span class="gm-menu-name">{{ r.node.title }}</span>
												<span v-if="r.trail.length" class="gm-trail">{{ r.trail.join(' / ') }}</span>
											</el-checkbox>
										</div>
									</td>

									<!-- 按钮授权 -->
									<td class="gm-td-btn">
										<div v-if="r.buttons.length" class="gm-btns">
											<el-checkbox
												v-for="b in r.buttons"
												:key="b.id"
												size="small"
												:model-value="state.checkedKeys.has(b.id)"
												@change="(v) => toggleNode(b, !!v)"
											>
												{{ b.title }}
											</el-checkbox>
										</div>
										<span v-else class="gm-nil">—</span>
									</td>
								</tr>
							</template>
						</tbody>
					</table>
					<div v-else class="gm-empty">
						<el-icon><ele-Search /></el-icon>
						<span>{{ filterText ? '没有匹配的菜单' : '暂无菜单数据' }}</span>
					</div>
				</div>
			</el-card>

			<template #footer>
				<div class="gm-foot">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</div>
			</template>

			<!-- 隐藏的树：只为复用 el-tree 的父子级联勾选逻辑，不参与展示 -->
			<div class="gm-tree-proxy" aria-hidden="true">
				<el-tree ref="treeRef" :data="state.menuData" node-key="id" show-checkbox :props="{ children: 'children', label: 'title' }" />
			</div>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup>
import { reactive, computed, onMounted, ref } from 'vue';
import type { ElTree } from 'element-plus';
import NoticeBar from '/@/components/noticeBar/index.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi, SysRoleApi } from '/@/api-services/system/api';
import { SysMenu } from '/@/api-services/system/models';

/** 表格行：一个末级菜单（或带按钮的中间菜单）+ 它下面的按钮 */
type RowItem = {
	id: number;
	node: SysMenu;
	trail: string[];
	depth: number;
	buttons: SysMenu[];
	keep?: boolean;
};

/** 表格分组：一个顶级节点（通常是一级目录）及其下所有行 */
type GroupItem = {
	id: number;
	node: SysMenu;
	title: string;
	isDir: boolean;
	rows: RowItem[];
};

const filterText = ref('');
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	isVisible: false,
	drawerTitle: '',
	menuIdList: [] as any,
	roleId: 0,
	menuData: [] as Array<SysMenu>, // 菜单数据
	checkedKeys: new Set<number>(), // 全选（含父级）
	halfKeys: new Set<number>(), // 半选（自身未选、但下方有选中）
	leafCount: 0, // 实际勾选项数（叶子）
});

const isBtn = (n: SysMenu) => Number(n.type) === 3;
const isDir = (n: SysMenu) => Number(n.type) === 1;
const hitKeyword = (n: SysMenu, kw: string) =>
	[n.title, n.name, n.path, n.permission].some((f) => String(f ?? '').toLowerCase().includes(kw));

/**
 * 把菜单树拍平成「表格行」：
 * - 每个没有子菜单（或没有子菜单但有按钮）的节点 = 一行；
 * - 中间层级的目录/菜单名收进 trail，作为该行的父级路径；
 * - 深度决定菜单列的缩进，任意层级嵌套都不会丢信息。
 */
const tableGroups = computed<GroupItem[]>(() => {
	const kw = filterText.value.trim().toLowerCase();
	const groups: GroupItem[] = [];

	// isTop：当前节点是分组的第一列（一级目录），不再把自己算进父级路径，避免「工作台 / 工作台」
	const collect = (node: SysMenu, trail: string[], bucket: RowItem[], isTop = false) => {
		const kids = (node.children ?? []) as SysMenu[];
		const subs = kids.filter((k) => !isBtn(k));
		const btns = kids.filter(isBtn);

		// 末级节点：自身成行
		if (!subs.length) {
			bucket.push({ id: node.id, node, trail: [...trail], depth: trail.length, buttons: btns });
			return;
		}
		// 中间节点自身也挂了按钮：单独成行，再把下级递归展开
		if (btns.length) bucket.push({ id: node.id, node, trail: [...trail], depth: trail.length, buttons: btns });

		// 一级目录已占第一列，不重复进路径；其余层级（多级目录/菜单）才记为父级
		const nextTrail = isTop && isDir(node) ? trail : [...trail, String(node.title ?? '')];
		subs.forEach((s) => collect(s, nextTrail, bucket));
	};

	state.menuData.forEach((top) => {
		if (isBtn(top)) return;
		const bucket: RowItem[] = [];
		collect(top, [], bucket, true);
		if (!bucket.length) return;
		groups.push({ id: top.id, node: top, title: String(top.title ?? ''), isDir: isDir(top), rows: bucket });
	});

	if (!kw) return groups;

	// 过滤：目录/菜单命中则整组保留其按钮；只有按钮命中时该行仅显示命中的按钮
	return groups
		.map((g) => {
			const groupHit = hitKeyword(g.node, kw);
			const rows = g.rows
				.map((r) => {
					const rowHit = hitKeyword(r.node, kw) || r.trail.some((t) => t.toLowerCase().includes(kw));
					const buttons = groupHit || rowHit ? r.buttons : r.buttons.filter((b) => hitKeyword(b, kw));
					return { ...r, buttons, keep: groupHit || rowHit || buttons.length > 0 };
				})
				.filter((r) => r.keep);
			return rows.length ? { ...g, rows } : null;
		})
		.filter((g): g is GroupItem => !!g);
});

/**
 * 半选：自身没选中、但下方有节点被选中。
 * 直接从已选集合推导，不依赖 el-tree 的 indeterminate 同步时机（它有异步重算，容易读到旧值）。
 */
const computeHalf = (checked: Set<number>) => {
	const set = new Set<number>();
	const walk = (n: SysMenu): boolean => {
		const kids = (n.children ?? []) as SysMenu[];
		let any = checked.has(n.id);
		kids.forEach((k) => {
			if (walk(k)) any = true;
		});
		if (!checked.has(n.id) && any) set.add(n.id);
		return any;
	};
	state.menuData.forEach((n) => walk(n));
	return set;
};

// 同步 el-tree 的勾选状态到表格
const syncChecked = () => {
	const tree = treeRef.value;
	if (!tree) return;
	const checked = new Set(tree.getCheckedKeys() as number[]);
	state.checkedKeys = checked;
	state.halfKeys = computeHalf(checked);
	state.leafCount = (tree.getCheckedKeys(true) as number[]).length;
};

// 勾选/取消：走树，父子级联由 el-tree 保证
const toggleNode = (node: SysMenu, val: boolean) => {
	treeRef.value?.setChecked(node.id, val, true);
	syncChecked();
	setTimeout(syncChecked, 0);
};

// 全选 / 清空
const checkAll = (val: boolean) => {
	const tree = treeRef.value;
	if (!tree) return;
	tree.setCheckedKeys(val ? collectAllKeys(state.menuData) : []);
	syncChecked();
	setTimeout(syncChecked, 0);
};

const collectAllKeys = (nodes: SysMenu[]): number[] => {
	const keys: number[] = [];
	const walk = (list: SysMenu[]) => {
		list.forEach((n) => {
			keys.push(n.id);
			const kids = (n.children ?? []) as SysMenu[];
			if (kids.length) walk(kids);
		});
	};
	walk(nodes);
	return keys;
};

// 页面初始化
onMounted(async () => {
	state.loading = true;
	var res = await getAPI(SysMenuApi).apiSysMenuListGet();
	state.menuData = res.data.result ?? [];
	state.loading = false;
});

// 打开页面
const openDrawer = async (row: any) => {
	state.roleId = row.id;
	state.drawerTitle = `授权角色菜单 - ${row.name}`;

	filterText.value = '';
	treeRef.value?.setCheckedKeys([]);
	syncChecked();

	state.isVisible = true;
	state.loading = true;
	if (row.id != undefined) {
		var res = await getAPI(SysRoleApi).apiSysRoleOwnMenuListGet(row.id);
		const keys = res.data.result ?? [];
		// 抽屉内容首次渲染后树才存在，稍等再回填
		setTimeout(() => {
			treeRef.value?.setCheckedKeys(keys);
			syncChecked();
		}, 100);
	}
	state.loading = false;
};

// 取消
const cancel = () => {
	state.isVisible = false;
};

// 提交
const submit = async () => {
	state.menuIdList = treeRef.value?.getCheckedKeys(true) as Array<number>;
	await getAPI(SysRoleApi).apiSysRoleGrantMenuPost({ id: state.roleId, menuIdList: state.menuIdList });
	cancel();
};

// 导出对象
defineExpose({ openDrawer });
</script>

<!-- 抽屉内容由 el-drawer 挂到 body 上，scoped 的 data-v 会断链，这里必须写全局样式 -->
<style lang="scss">
.gm-drawer {
	/* 外层整列 flex 且禁止滚动：滚动条只留给表格 */
	.el-drawer__body {
		display: flex;
		flex-direction: column;
		gap: 10px;
		min-height: 0;
		padding: 12px 16px;
		overflow: hidden;
	}

	.el-drawer__footer {
		padding: 10px 16px;
	}
}

/* 卡片吃掉抽屉剩余高度，再把剩余高度交给表格 */
.gm-card {
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;

	.el-card__header {
		flex: 0 0 auto;
		padding: 10px 12px;
	}

	.el-card__body {
		flex: 1 1 auto;
		min-height: 0;
		overflow: hidden;
		padding: 0;
	}
}

/* 头部：搜索框占满 + 右侧计数与快捷操作 */
.gm-head {
	display: flex;
	align-items: center;
	gap: 12px;
	min-width: 0;

	.gm-search {
		flex: 1 1 auto;
		min-width: 0;
	}
}

.gm-head-acts {
	display: flex;
	align-items: center;
	flex: 0 0 auto;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);

	.gm-count {
		margin-right: 10px;

		b {
			color: var(--el-color-primary);
			font-weight: 600;
			font-variant-numeric: tabular-nums;
		}
	}

	.el-button + .el-button {
		margin-left: 0;
	}
}

/* 唯一滚动容器 */
.gm-body {
	position: relative;
	height: 100%;
	min-height: 0;
	overflow: auto;
}

.gm-table {
	width: 100%;
	min-width: 640px;
	table-layout: fixed;
	border-collapse: separate;
	border-spacing: 0;
	font-size: 13px;

	/* 列宽用百分比：抽屉宽窄变化时按钮列不会被挤爆（按钮多时靠换行兜住） */
	.col-dir {
		width: 19%;
	}

	.col-menu {
		width: 29%;
	}

	thead th {
		position: sticky;
		top: 0;
		z-index: 2;
		padding: 10px 14px;
		text-align: left;
		font-size: 12.5px;
		font-weight: 600;
		color: var(--el-text-color-regular);
		background: var(--el-fill-color-light);
		border-bottom: 1px solid var(--el-border-color);
		white-space: nowrap;
	}

	tbody td {
		padding: 8px 14px;
		vertical-align: top;
		border-bottom: 1px solid var(--el-border-color-lighter);
	}

	tbody tr:last-child td {
		border-bottom: none;
	}

	/* 目录列：竖向居中 + 极浅底色，作为行的“分组条” */
	.gm-td-dir {
		vertical-align: middle;
		background: color-mix(in srgb, var(--el-color-primary) 5%, transparent);
		border-right: 1px solid var(--el-border-color);
	}

	tbody tr:hover td:not(.gm-td-dir) {
		background: color-mix(in srgb, var(--el-color-primary) 4%, transparent);
	}

	.el-checkbox {
		margin-right: 0;
		height: auto;
	}

	.el-checkbox__label {
		padding-left: 6px;
		line-height: 1.5;
	}
}

.gm-dir-name {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);

	&.is-plain {
		font-weight: 500;
		color: var(--el-text-color-secondary);
	}
}

/* 菜单列：多行文本（名称 + 父级路径）时复选框与首行对齐 */
.gm-menu {
	display: flex;
	min-width: 0;

	.el-checkbox {
		align-items: flex-start;
	}

	.el-checkbox__input {
		margin-top: 2px;
	}

	.el-checkbox__label {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}
}

.gm-menu-name {
	font-size: 13px;
	color: var(--el-text-color-primary);
	word-break: break-all;

	&.is-plain {
		color: var(--el-text-color-secondary);
		font-size: 12.5px;
	}
}

.gm-trail {
	font-size: 11px;
	line-height: 1.4;
	color: var(--el-text-color-placeholder);
	word-break: break-all;
}

/* 按钮列：横排铺开、自动换行 */
.gm-btns {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 2px 16px;

	.el-checkbox {
		height: 22px;
	}

	.el-checkbox__label {
		padding-left: 5px;
		font-size: 12.5px;
		color: var(--el-text-color-regular);
	}
}

.gm-nil {
	color: var(--el-text-color-disabled);
}

.gm-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	height: 100%;
	min-height: 200px;
	font-size: 13px;
	color: var(--el-text-color-placeholder);

	.el-icon {
		font-size: 26px;
		opacity: 0.6;
	}
}

/*
 * 隐藏的代理树：只负责父子级联的勾选计算。
 * 不能用 display:none（el-tree 需要真实尺寸做节点计算），移出可视区即可。
 */
.gm-tree-proxy {
	position: absolute;
	left: -9999px;
	top: 0;
	width: 1px;
	height: 1px;
	overflow: hidden;
	opacity: 0;
	pointer-events: none;
}

.gm-foot {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
}
</style>
