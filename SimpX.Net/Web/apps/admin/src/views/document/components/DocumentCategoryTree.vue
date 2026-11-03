<template>
	<el-tree
		ref="treeRef"
		:data="categoryTree"
		:props="{ children: 'children', label: 'name' }"
		node-key="id"
		:default-expand-all="true"
		:highlight-current="true"
		:current-node-key="currentCategoryId"
		@node-click="handleNodeClick"
	>
		<template #default="{ node, data }">
			<span class="tree-node">
				<SvgIcon v-if="data.icon" :name="data.icon" />
				<span>{{ node.label }}</span>
			</span>
		</template>
	</el-tree>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentCategoryApi } from '/@/api-services/document/api';
import { ref, onMounted, watch, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import SvgIcon from '/@/components/svgIcon/index.vue';

const props = defineProps<{
	currentCategoryId?: number | null;
}>();

const categoryTree = ref<any[]>([]);
const treeRef = ref();

const emit = defineEmits<{
	select: [categoryId: number | null];
}>();


const loadCategoryTree = async () => {
	try {
		const res = await getAPI(DocumentCategoryApi).apiDocumentCategoryTreeGet();
		categoryTree.value = res.data?.result || [];
		// 加载完成后，设置当前选中的节点
		await nextTick();
		if (props.currentCategoryId && treeRef.value) {
			treeRef.value.setCurrentKey(props.currentCategoryId);
		} else if (!props.currentCategoryId && treeRef.value) {
			treeRef.value.setCurrentKey(null);
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '加载分类树失败');
		categoryTree.value = [];
	}
};

// 监听 currentCategoryId 变化，更新树的高亮节点
watch(
	() => props.currentCategoryId,
	(newVal) => {
		if (treeRef.value) {
			if (newVal) {
				treeRef.value.setCurrentKey(newVal);
			} else {
				treeRef.value.setCurrentKey(null);
			}
		}
	},
	{ immediate: true }
);

const handleNodeClick = (data: any) => {
	emit('select', data.id === 0 ? null : data.id);
};

onMounted(() => {
	loadCategoryTree();
});

defineExpose({
	loadCategoryTree,
});
</script>

<style lang="scss" scoped>
.tree-node {
	display: flex;
	align-items: center;
	gap: 5px;
}
</style>
