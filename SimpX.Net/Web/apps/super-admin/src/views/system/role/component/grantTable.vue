<template>
	<div class="gt-container">
		<el-drawer v-model="state.isVisible" size="55%" class="gt-drawer">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Grid /></el-icon>
					<span>{{ state.drawerTitle }}</span>
				</div>
			</template>

			<NoticeBar leftIcon="iconfont icon-tongzhi2" text="非超管角色禁止授权系统核心模块字段资源" :scrollable="true" />

			<el-card class="gt-card" shadow="hover">
				<template #header>
					<div class="gt-head">
						<el-input v-model="filterText" prefix-icon="ele-Search" clearable placeholder="表名 / 字段名 / 字段说明" class="gt-search" />
						<div class="gt-head-acts">
							<span class="gt-count">已选 <b>{{ state.checkedCount }}</b> / {{ totalColumnCount }} 个字段</span>
							<el-button link type="primary" @click="checkAll(true)">全选</el-button>
							<el-button link @click="checkAll(false)">清空</el-button>
						</div>
					</div>
				</template>

				<!-- 抽屉里唯一的滚动容器：外层不滚，只在树内上下滚 -->
				<div class="gt-body" v-loading="state.loading">
					<el-tree
						ref="treeRef"
						class="gt-tree"
						:data="state.roleTableData"
						:filter-node-method="filterNode"
						node-key="id"
						:props="{ children: 'columns', label: 'label', class: 'penultimate-node' }"
						show-checkbox
						icon="ele-Menu"
						highlight-current
						default-expand-all
						@check-change="refreshCount"
					>
						<template #default="{ node, data }">
							<span v-if="node.level === 1" class="gt-node gt-node-table">
								<el-icon class="gt-ico"><ele-Grid /></el-icon>
								<span class="gt-name">{{ node.label }}</span>
								<span class="gt-sub">{{ data.name }}</span>
								<el-tag size="small" effect="plain" round>{{ data.columns?.length ?? 0 }} 字段</el-tag>
							</span>
							<span v-else class="gt-node gt-node-col">
								<span class="gt-name">{{ node.label }}</span>
								<span class="gt-sub">{{ data.name }}</span>
							</span>
						</template>
					</el-tree>

					<div v-if="!state.loading && !state.roleTableData.length" class="gt-empty">
						<el-icon><ele-FolderOpened /></el-icon>
						<span>未获取到表字段数据</span>
					</div>
				</div>
			</el-card>

			<template #footer>
				<div class="gt-foot">
					<span class="gt-foot-tip">勾选的字段将对该角色隐藏</span>
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.submitting" @click="submit">确定</el-button>
				</div>
			</template>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup>
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue';
import type { ElTree } from 'element-plus';
import { ElMessage } from 'element-plus';
import NoticeBar from '/@/components/noticeBar/index.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysRoleApi } from '/@/api-services/system/api';
import { RoleTableOutput } from '/@/api-services/system/models';

const filterText = ref('');
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	submitting: false,
	isVisible: false,
	drawerTitle: '',
	tableColumnList: [] as Array<string>,
	roleId: 0,
	roleTableData: [] as Array<RoleTableOutput>, // 表 + 字段数据
	checkedCount: 0, // 已勾选字段（叶子）数量
});

// 全部字段总数，用于「已选 N / M」
const totalColumnCount = computed(() => state.roleTableData.reduce((sum, table) => sum + (table.columns?.length ?? 0), 0));

// 节点过滤
watch(filterText, (val) => {
	treeRef.value?.filter(val);
});

// 页面初始化
onMounted(() => {
	initTreeData();
});

// 初始化树
const initTreeData = async () => {
	state.loading = true;
	try {
		const res = await getAPI(SysRoleApi).apiSysRoleAllTableColumnListGet();
		state.roleTableData = res.data.result ?? [];
	} finally {
		state.loading = false;
	}
};

// 节点过滤：字段命中时保留其所属表，否则父节点被隐藏会连匹配项一起看不见
const filterNode = (value: string, data: any) => {
	if (!value) return true;
	const keyword = value.trim().toLowerCase();
	const hit = (text?: string | null) => (text ?? '').toLowerCase().includes(keyword);

	if (data.columns) return hit(data.label) || hit(data.name) || (data.columns ?? []).some((c: any) => hit(c.label) || hit(c.name));
	return hit(data.label) || hit(data.name);
};

// 刷新已选数量（勾选变化与 setCheckedKeys 之后都要调）
const refreshCount = () => {
	state.checkedCount = (treeRef.value?.getCheckedKeys(true) ?? []).length;
};

// 勾选态要在树渲染完成后才能写入，首次打开抽屉时 treeRef 可能尚未挂载
const applyCheckedKeys = async (keys: Array<string>) => {
	await nextTick();
	if (!treeRef.value) await new Promise((resolve) => setTimeout(resolve, 50));
	treeRef.value?.setCheckedKeys(keys);
	refreshCount();
};

// 打开页面
const openDrawer = async (row: any) => {
	state.roleId = row.id;
	state.drawerTitle = `设置角色字段黑名单 - ${row.name}`;
	state.isVisible = true;
	state.loading = true;
	try {
		// 树数据尚未回来时先补拉一次，避免白屏
		if (!state.roleTableData.length) await initTreeData();

		await applyCheckedKeys([]); // 清空上一次角色的选中值
		if (row.id != undefined) {
			const res = await getAPI(SysRoleApi).apiSysRoleRoleTableRoleIdGet(row.id);
			await applyCheckedKeys((res.data.result ?? []) as Array<string>);
		}
	} finally {
		state.loading = false;
	}
};

// 全选 / 清空：只提交叶子节点，因此这里也只写叶子，父级由 el-tree 自动推导半选
const checkAll = (checked: boolean) => {
	const keys = checked ? state.roleTableData.flatMap((table) => (table.columns ?? []).map((c) => c.id ?? '')).filter((id) => !!id) : [];
	applyCheckedKeys(keys);
};

// 取消
const cancel = () => {
	state.isVisible = false;
};

// 提交
const submit = async () => {
	state.submitting = true;
	try {
		state.tableColumnList = (treeRef.value?.getCheckedKeys(true) ?? []) as Array<string>;
		await getAPI(SysRoleApi).apiSysRoleGrantRoleTablePost({ id: state.roleId, tableColumnList: state.tableColumnList });
		ElMessage.success('保存成功');
		cancel();
	} catch (error: any) {
		ElMessage.error(error?.message ?? '保存失败，请稍后重试');
	} finally {
		state.submitting = false;
	}
};

// 导出对象
defineExpose({ openDrawer });
</script>

<!-- 抽屉内容被挂到 body 上，scoped 的 data-v 会断链，这里必须写全局样式（.gt- 前缀限定作用域） -->
<style lang="scss">
.gt-drawer {
	/* 外层整列 flex 且禁止滚动：滚动条只留给里面的树 */
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

/* 卡片吃掉抽屉剩余高度，再把剩余高度交给树 */
.gt-card {
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
		padding: 6px 8px 8px;
	}
}

.gt-head {
	display: flex;
	align-items: center;
	gap: 12px;
	min-width: 0;

	.gt-search {
		flex: 1 1 auto;
		min-width: 0;
	}
}

.gt-head-acts {
	display: flex;
	align-items: center;
	flex: 0 0 auto;
	gap: 4px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);

	.gt-count {
		margin-right: 6px;
		white-space: nowrap;

		b {
			color: var(--el-color-primary);
			font-weight: 600;
		}
	}
}

.gt-body {
	height: 100%;
	min-height: 0;
	overflow: auto;
	padding-right: 2px;
}

.gt-tree {
	/* 字段节点平铺：一张表的字段横向排布，避免竖排把列表拉得过长 */
	.penultimate-node > .el-tree-node__children {
		padding-left: 35px;
		white-space: pre-wrap;
		line-height: 100%;

		.el-tree-node {
			display: inline-block;
			min-width: 160px;
			max-width: 100%;
		}

		.el-tree-node__content {
			padding-left: 5px !important;
			padding-right: 5px;
		}
	}
}

/* 表名用主文案 + 物理表名副文案，字段同理（说明 + 列名） */
.gt-node {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
	max-width: 100%;
}

.gt-node-table .gt-name {
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.gt-ico {
	font-size: 15px;
	color: var(--el-color-primary);
}

.gt-name {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.gt-sub {
	font-size: 11.5px;
	color: var(--el-text-color-placeholder);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.gt-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	height: 100%;
	min-height: 160px;
	color: var(--el-text-color-placeholder);
	font-size: 13px;

	.el-icon {
		font-size: 30px;
	}
}

.gt-foot {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 10px;

	.gt-foot-tip {
		margin-right: auto;
		font-size: 12.5px;
		color: var(--el-text-color-placeholder);
	}
}
</style>
