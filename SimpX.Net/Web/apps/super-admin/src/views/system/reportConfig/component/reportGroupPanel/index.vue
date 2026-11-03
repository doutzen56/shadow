<template>
	<el-card class="box-card" shadow="hover" v-loading="state.isLoading">
		<template #header>
			<div class="card-header">
				<div class="tree-h-flex">
					<div class="tree-h-left">
						<el-input :prefix-icon="Search" v-model.lazy="filterText" clearable placeholder="查询分组" />
					</div>
					<div class="tree-h-right">
						<el-dropdown @command="handleCommand">
							<el-button class="tree-h-more" title="更多操作">
								<el-icon class="el-icon--center">
									<more-filled />
								</el-icon>
							</el-button>
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item command="expandAll">{{ '全部展开' }}</el-dropdown-item>
									<el-dropdown-item command="collapseAll">{{ '全部折叠' }}</el-dropdown-item>
									<el-dropdown-item command="create">{{ '新增分组' }}</el-dropdown-item>
									<el-dropdown-item command="edit">{{ '编辑分组' }}</el-dropdown-item>
									<el-dropdown-item command="delete">{{ '删除分组' }}</el-dropdown-item>
									<el-dropdown-item command="refresh">{{ '刷新' }}</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>
					</div>
				</div>
			</div>
		</template>
		<div class="tree-scroll">
			<el-scrollbar>
				<el-tree
					v-if="state.treeData"
					ref="treeRef"
					class="filter-tree"
					:data="state.treeData"
					node-key="id"
					:props="{ children: 'children', label: 'label' }"
					:filter-node-method="filterNode"
					@node-click="(group: SysReportGroup) => emit('nodeClick', group)"
					:default-expanded-keys="state.treeData[0] && [state.treeData[0].id]"
					highlight-current
				>
					<template #default="{ node }">
						<el-icon v-if="node.level == 1" size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-School /></el-icon>
						<el-icon v-else-if="node.level == 2" size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Refrigerator /></el-icon>
						<el-icon v-else size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-CollectionTag /></el-icon>
						<highLightKeyword v-if="filterText" :text="node.label" :keyword="filterText" />
						<span v-else>{{ node.label }}</span>
					</template>
				</el-tree>
				<el-empty v-else :image-size="100" />
			</el-scrollbar>
		</div>
	</el-card>
	<EditReportGroup ref="editReportGroup" :title="state.title" @handleQuery="fetchTreeData"></EditReportGroup>
</template>

<script setup lang="ts" name="sysReportGroup">
import { onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox, ElTree } from 'element-plus';
import { Search, MoreFilled } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';

import EditReportGroup from './editReportGroup.vue';
import highLightKeyword from '/@/components/highLightKeyword/index.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportGroupApi } from '/@/api-services/system/api';
import { SysReportGroup } from '/@/api-services/system/models';

const { t } = useI18n();

// 定义子组件向父组件传值/事件
const emit = defineEmits(['nodeClick']);

const treeRef = ref<InstanceType<typeof ElTree>>();
const editReportGroup = ref<InstanceType<typeof EditReportGroup>>();

const filterText = ref('');
const state = reactive({
	isLoading: false, // 是否加载中
	title: '',
	treeData: [] as any,
});

// 页面初始化
onMounted(() => {
	fetchTreeData();
});

// 获取树数据
const fetchTreeData = () => {
	state.isLoading = true;
	getAPI(SysReportGroupApi)
		.apiSysReportGroupListGet()
		.then((res) => {
			getTree(res.data.result ?? []);
			let rootGroup = [{ id: 0, label: t('全部'), value: 0, children: [] }];
			rootGroup[0].children = res.data.result as [];
			state.treeData = rootGroup as [];
		})
		.finally(() => {
			state.isLoading = false;
		});
};

// 处理树数据
const getTree = (tree: SysReportGroup[]): [] => {
	tree.forEach((r: any) => {
		r.label = `${r.number}[${r.name}]`;
		r.value = r.id;
	});
	return [];
};

// 查询监听
watch(filterText, (val) => {
	treeRef.value!.filter(val);
});

// 节点过滤
const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.label.includes(value);
};

// 节点操作
const handleCommand = async (command: string | number | object) => {
	if ('expandAll' == command) {
		// 展开分组
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = true;
		}
	} else if ('collapseAll' == command) {
		// 折叠分组
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = false;
		}
	} else if ('create' == command) {
		// 新增分组
		state.title = t('新增分组');
		editReportGroup.value?.openDialog({});
	} else if ('edit' == command) {
		// 编辑分组
		const currData = treeRef.value!.getCurrentNode();
		if (!currData) {
			ElMessage.error(t('请选择分组后编辑'));
			return;
		}
		if (currData.id == 0) {
			ElMessage.error(t('请选择有效的分组节点'));
			return;
		}
		state.title = t('编辑分组');
		editReportGroup.value?.openDialog(currData);
	} else if ('delete' == command) {
		// 删除分组
		const currData = treeRef.value!.getCurrentNode();
		if (currData?.id == 0) {
			ElMessage.error(t('请选择有效的分组节点'));
			return;
		}
		ElMessageBox.confirm(t('确认删除？', { label: currData?.label }), t('提示'), {
			confirmButtonText: t('确定'),
			cancelButtonText: t('取消'),
			type: 'warning',
		}).then(() => {
			getAPI(SysReportGroupApi)
				.apiSysReportGroupDeletePost({ id: currData?.id })
				.then((res) => {
					ElMessage.success(t('删除成功'));
					fetchTreeData();
				});
		});
	} else if ('refresh' == command) {
		fetchTreeData(); // 刷新
	}
};
</script>

<style scoped lang="scss">
.box-card {
	flex: 1;
	height: 100%;
	display: flex;
	flex-direction: column;

	> :deep(.el-card__header) {
		padding: 10px 12px;
	}

	> :deep(.el-card__body) {
		flex: 1 1 auto;
		min-height: 0;
		padding: 4px 8px 8px;
	}
}

/* 查询框 + 下拉按钮左右占满一行：左侧吃掉全部剩余宽度，右侧固定 */
.tree-h-flex {
	display: flex;
	flex: 1 1 auto;
	min-width: 0;
	align-items: center;
	gap: 8px;
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
}

.tree-h-more {
	width: 32px;
	padding: 0;
}

/* 树区域撑满卡片剩余高度 */
.tree-scroll {
	height: 100%;
}
</style>
