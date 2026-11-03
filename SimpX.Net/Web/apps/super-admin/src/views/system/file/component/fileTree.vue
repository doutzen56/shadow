<template>
	<el-card class="box-card" shadow="hover" body-style="height:100%;overflow:auto;padding:5px;width:100%;">
		<template #header>
			<div class="card-header">
				<div class="tree-h-flex">
					<div class="tree-h-left">
						<el-input :prefix-icon="Search" v-model.lazy="filterText" clearable placeholder="文件路径" />
					</div>
					<div class="tree-h-right">
						<el-dropdown @command="handleCommand">
							<el-button style="width: 34px">
								<el-icon class="el-icon--center">
									<more-filled />
								</el-icon>
							</el-button>
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item command="expandAll">全部展开</el-dropdown-item>
									<el-dropdown-item command="collapseAll">全部折叠</el-dropdown-item>
									<el-dropdown-item command="rootNode">根节点</el-dropdown-item>
									<el-dropdown-item command="refresh">刷新</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>
					</div>
					<el-checkbox v-if="!props.checkStrictly && state.isShowCheckbox" v-model="state.strictly" label="联动" style="margin-left: 8px" border />
				</div>
			</div>
		</template>
		<div style="margin-bottom: 45px" v-loading="state.loading">
			<el-scrollbar>
				<el-tree
					ref="treeRef"
					class="filter-tree"
					:data="state.folderData"
					node-key="id"
					:props="{ children: 'children', label: 'name' }"
					:filter-node-method="filterNode"
					@node-click="nodeClick"
					:show-checkbox="state.isShowCheckbox"
					default-expand-all
					highlight-current
					:expand-on-click-node="false"
					:check-strictly="!state.strictly"
				>
					<template #default="{ node, data }">
						<span class="ft-node" :title="nodeTip(data)">
							<el-icon class="ft-node-ico" size="16">
								<ele-FolderOpened v-if="node.expanded" />
								<ele-Folder v-else />
							</el-icon>
							<span class="ft-node-name">{{ node.label }}</span>
							<!-- 目录统计：直属子项数（子文件夹 + 直属文件）+ 含子目录的占用空间，完整信息见 title -->
							<span v-if="data.totalFileCount" class="ft-node-meta">
								<span v-if="childCount(data)" class="ft-node-count">{{ childCount(data) }}</span>
								<span class="ft-node-size">{{ formatFileSize(data.sizeKb) }}</span>
							</span>
						</span>
					</template>
				</el-tree>
			</el-scrollbar>
		</div>
	</el-card>
</template>

<script lang="ts" setup>
import { onMounted, reactive, ref, watch } from 'vue';
import type { ElTree } from 'element-plus';
import { Search, MoreFilled } from '@element-plus/icons-vue';
import { TreeKey } from 'element-plus/es/components/tree/src/tree.type';

import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system/api';
import { TreeNode } from '/@/api-services/system/models';
import { formatDateTime, formatFileSize } from '/@/utils/format';

const props = defineProps({
	checkStrictly: { type: Boolean, default: true },
});

const filterText = ref('');
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	folderData: [] as Array<TreeNode>,
	isShowCheckbox: false,
	strictly: false,
});

// 页面初始化
onMounted(async () => {
	await fetchTreeData();
});

// 查询监听
watch(filterText, (val) => {
	treeRef.value!.filter(val);
});

// 获取树数据
const fetchTreeData = async (showLoading: boolean = true) => {
	if (showLoading) state.loading = true;
	var res = await getAPI(SysFileApi).apiSysFileFolderGet();
	state.folderData = res.data.result ?? [];
	if (showLoading) state.loading = false;
	return res.data.result ?? [];
};

// 获取已经选择
const getCheckedKeys = () => {
	return treeRef.value!.getCheckedKeys();
};

// 节点过滤
const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.name.includes(value);
};

// 直属子项数：子文件夹 + 直属文件（upload=2 个文件夹、10=3 个子目录、叶子目录=文件数）
const childCount = (data: any) => (data?.children?.length ?? 0) + (data?.fileCount ?? 0);

// 节点悬浮提示：直属子项构成、含子目录文件数、占用空间、最后修改时间
const nodeTip = (data: any) => {
	if (!data?.totalFileCount) return data?.name ?? '';
	const dirs = data?.children?.length ?? 0;
	const files = data?.fileCount ?? 0;
	const time = formatDateTime(data.lastModified, 'YYYY-mm-dd HH:MM');
	return `${data.name}：${dirs} 个文件夹 + ${files} 个文件 · 合计 ${data.totalFileCount} 个文件 / ${formatFileSize(data.sizeKb ?? 0)} · 最近修改 ${time}`;
};

// 节点操作
const handleCommand = async (command: string | number | object) => {
	if ('expandAll' == command) {
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = true;
		}
	} else if ('collapseAll' == command) {
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = false;
		}
	} else if ('refresh' == command) {
		fetchTreeData();
	} else if ('rootNode' == command) {
		treeRef.value?.setCurrentKey();
		emits('node-click', { id: 0, name: '' });
	}
};

// 与父组件的交互逻辑
const emits = defineEmits(['node-click']);
const nodeClick = (node: any, data: any) => {
	var path = node.name;
	if (data.level == 4) {
		// 当点击日节点时，获取对应的月和年
		var monthNode = treeRef.value!.getNode(node.pid);
		var yearNode = treeRef.value!.getNode(monthNode.data.pid);
		path = `${yearNode.data.name}/${monthNode.data.name}/${node.name}`;
	} else if (data.level == 3) {
		// 当点击月节点时，获取对应的年
		var yearNode = treeRef.value!.getNode(node.pid);
		path = `${yearNode.data.name}/${node.name}`;
	}
	emits('node-click', { id: node.id, name: node.name, path: path });
};

// 设置当前选中节点
const setCurrentKey = (key?: TreeKey | undefined, shouldAutoExpandParent?: boolean | undefined) => {
	treeRef.value?.setCurrentKey(key, shouldAutoExpandParent);
};

// 导出对象
defineExpose({ fetchTreeData, getCheckedKeys, setCurrentKey });
</script>

<style lang="scss" scoped>
.box-card {
	flex: 1;
	> :deep(.el-card__header) {
		padding: 5px;
	}
}
.tree-h-flex {
	/* 全局 .card-header 是 space-between 的 flex，这里必须 flex:1 撑满整行，
	   否则头部只按内容收缩，输入框右侧留一大截空白 */
	display: flex;
	align-items: center;
	gap: 8px;
	flex: 1 1 auto;
	min-width: 0;
}

.tree-h-left {
	flex: 1 1 auto;
	min-width: 0;

	:deep(.el-input) {
		width: 100%;
	}
}

.tree-h-right {
	flex: 0 0 auto;
	width: 42px;
	min-width: 42px;
}

/* 目录节点：图标 + 名称（可省略）+ 右侧统计 */
.ft-node {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 1 1 auto;
	min-width: 0;

	.ft-node-ico {
		flex: 0 0 auto;
		color: var(--el-color-primary);
	}

	.ft-node-name {
		flex: 1 1 auto;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.ft-node-meta {
		flex: 0 0 auto;
		display: inline-flex;
		align-items: center;
		gap: 5px;
		font-size: 11px;
		line-height: 1;
		color: var(--el-text-color-secondary);
		margin-right: 3px;
	}

	.ft-node-count {
		min-width: 18px;
		height: 16px;
		padding: 0 5px;
		border-radius: 8px;
		font-size: 11px;
		line-height: 16px;
		text-align: center;
		background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
		color: var(--el-color-primary);
	}
}

/* 选中节点提亮统计，未选中时弱化，避免抢目录名的视觉重心 */
:deep(.el-tree-node.is-current) .ft-node-meta {
	color: var(--el-color-primary);
}
</style>
