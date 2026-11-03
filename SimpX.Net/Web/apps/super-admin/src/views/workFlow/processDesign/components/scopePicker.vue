<template>
	<el-form-item v-if="props.typeOptions.length > 0" :label="props.typeLabel" :error="props.typeError">
		<el-select v-model="innerType" :placeholder="props.typePlaceholder">
			<el-option v-for="item in props.typeOptions" :key="item.value" :label="item.label" :value="item.value" />
		</el-select>
	</el-form-item>

	<slot name="extra" />

	<el-form-item :label="props.scopeLabel" :error="props.error">
		<!-- 提交人上级/下级部门：由流程提交人动态推导，无需配置范围 -->
		<el-alert v-if="isOrgSubject" type="info" :closable="false" show-icon>
			<template #title>
				<div style="font-size: 12px" v-html="props.orgAlertTitle"></div>
			</template>
		</el-alert>

		<template v-else-if="showPresetScope">
			<!-- 部门：组织树多选 + 弹层 -->
			<template v-if="isTreeType">
				<el-popover v-model:visible="deptTreePopoverVisible" placement="bottom-start" :width="300" trigger="click" popper-class="dept-tree-popover">
					<template #reference>
						<el-input :model-value="deptTreeDisplayText" :placeholder="presetPlaceholder" readonly :loading="props.loading" style="cursor: pointer" @click="handleDeptTreeOpen">
							<template #suffix>
								<el-icon v-if="!props.loading"><ele-ArrowDown /></el-icon>
							</template>
						</el-input>
					</template>
					<div style="max-height: 300px; overflow-y: auto">
						<el-tree
							ref="deptTreeRef"
							:data="props.treeData"
							:props="{ children: 'children', label: 'name' }"
							node-key="id"
							show-checkbox
							check-strictly
							:default-expand-all="false"
							:default-expanded-keys="props.treeExpandedKeys"
							:default-checked-keys="deptCheckedKeys"
							:filter-node-method="filterDeptNode"
						/>
					</div>
					<div style="margin-top: 8px">
						<el-input v-model="deptTreeFilterText" placeholder="搜索部门" size="small" clearable @input="(val: string) => deptTreeRef?.filter(val)">
							<template #prefix>
								<el-icon><ele-Search /></el-icon>
							</template>
						</el-input>
					</div>
					<div style="margin-top: 10px; text-align: right; border-top: 1px solid var(--el-border-color-lighter); padding-top: 8px">
						<el-button size="small" @click="deptTreePopoverVisible = false">取消</el-button>
						<el-button type="primary" size="small" @click="confirmDeptSelection">确定</el-button>
					</div>
				</el-popover>
				<div v-if="deptSelected.length > 0" style="margin-top: 8px">
					<el-tag v-for="(dept, index) in deptSelected" :key="dept.id" closable @close="removeDept(index)" type="success" style="margin-right: 6px; margin-bottom: 4px">{{ dept.name }}</el-tag>
				</div>
			</template>

			<!-- 成员 / 角色：可搜索、可自造条目的下拉 -->
			<el-select v-else :model-value="localList" multiple filterable allow-create default-first-option :placeholder="presetPlaceholder" :loading="props.loading" tag-type="success" style="width: 100%" @update:model-value="onListChange">
				<el-option v-for="item in props.presetOptions" :key="item.value" :label="item.label" :value="item.value" />
			</el-select>
		</template>

		<!-- 其余类型（含未选类型）：自由文本 -->
		<el-input v-else :model-value="props.modelValue" type="textarea" :autosize="{ minRows: 2, maxRows: 6 }" :placeholder="props.freeTextPlaceholder" @update:model-value="(val: string) => emit('update:modelValue', val)" />
	</el-form-item>
</template>

<script setup lang="ts" name="ScopePicker">
/**
 * 审批人 / 抄送配置选择器：审批与抄送两块的「类型 + 范围」原本是两份孪生实现（含两套部门树弹层），
 * 这里合并为一个组件；范围串（user:1,role:2）的解析、前缀过滤、组织树勾选都在组件内完成，
 * 名称翻译（approverScopeName）仍由父组件负责。
 */
import { computed, nextTick, ref, watch } from 'vue';
import type { ElTree } from 'element-plus';

interface SelectedDept {
	id: number;
	name: string;
	code?: string;
}

interface ScopeOption {
	label: string;
	value: string;
}

const props = withDefaults(
	defineProps<{
		/** 已保存的范围串，如 user:1001,role:manager */
		modelValue: string;
		/** 当前范围类型：user / role / dept / orgparent / orgchild / '' */
		scopeType: string;
		typeLabel: string;
		typePlaceholder?: string;
		typeError?: string;
		scopeLabel: string;
		/** 动态推导类型的提示富文本 */
		orgAlertTitle: string;
		/** 自由文本模式占位 */
		freeTextPlaceholder: string;
		typeOptions: ScopeOption[];
		presetOptions: ScopeOption[];
		treeData: any[];
		treeExpandedKeys: number[];
		loading?: boolean;
		/** 部门树为空时由父组件触发加载（父组件持有缓存并负责请求） */
		loadTree: () => Promise<void>;
		error?: string;
	}>(),
	{ loading: false, error: '', typeError: '', typePlaceholder: '' }
);

const emit = defineEmits<{
	(e: 'update:modelValue', value: string): void;
	(e: 'update:scopeType', value: string): void;
}>();

const deptTreeRef = ref<InstanceType<typeof ElTree>>();
const deptTreePopoverVisible = ref(false);
const deptTreeFilterText = ref('');
const deptSelected = ref<SelectedDept[]>([]);
const localList = ref<string[]>([]);

const innerType = computed({
	get: () => props.scopeType,
	set: (value: string) => emit('update:scopeType', value),
});

const isTreeType = computed(() => props.scopeType === 'dept');
const showPresetScope = computed(() => ['user', 'role', 'dept'].includes(props.scopeType));
const isOrgSubject = computed(() => ['orgparent', 'orgchild'].includes(props.scopeType));
const presetPlaceholder = computed(() => {
	switch (props.scopeType) {
		case 'user':
			return '输入或选择成员（user:1001），可自定义';
		case 'role':
			return '输入或选择角色编码（role:manager）';
		default:
			return '输入或选择部门编码（dept:100）';
	}
});

const scopeItems = (scope?: string) =>
	(scope || '')
		.split(',')
		.map((item) => item.trim())
		.filter((item) => !!item);

const deptCheckedKeys = computed(() => deptSelected.value.map((dept) => dept.id));
const deptTreeDisplayText = computed(() => {
	if (deptSelected.value.length === 1) return deptSelected.value[0].name;
	if (deptSelected.value.length > 1) return `已选择 ${deptSelected.value.length} 个部门`;
	return '';
});

const findDeptInTree = (tree: any[], value: string): any => {
	for (const node of tree) {
		if ((node.code && node.code.toString() === value) || node.id?.toString() === value) return node;
		if (node.children?.length) {
			const found = findDeptInTree(node.children, value);
			if (found) return found;
		}
	}
	return null;
};

/** 部门勾选态：范围串、类型或树数据任一变化都要重解析（首次打开时树数据可能还没到） */
watch(
	[() => props.modelValue, () => props.scopeType, () => props.treeData],
	() => {
		if (!isTreeType.value) {
			deptSelected.value = [];
			return;
		}
		deptSelected.value = scopeItems(props.modelValue)
			.filter((item) => item.startsWith('dept:'))
			.map((item) => {
				const value = item.substring(5);
				const dept = findDeptInTree(props.treeData, value);
				return dept ? { id: dept.id, name: dept.name, code: dept.code } : { id: Number(value) || 0, name: `部门 ${value}`, code: value };
			});
		nextTick(() => deptTreeRef.value?.setCheckedKeys(deptCheckedKeys.value));
	},
	{ immediate: true }
);

/** 外部改动（切节点、切类型）时同步下拉选中项 */
const syncList = () => {
	if (isTreeType.value || showPresetScope.value) {
		localList.value = scopeItems(props.modelValue).filter((item) => item.startsWith(`${props.scopeType}:`));
	}
};
watch([() => props.modelValue, () => props.scopeType], syncList, { immediate: true });

/** 用户在下拉里的增删直接写回范围串 */
const onListChange = (items: string[]) => {
	localList.value = items;
	emit('update:modelValue', items.join(','));
};

const handleDeptTreeOpen = async () => {
	if (props.treeData.length === 0) await props.loadTree();
	await nextTick();
	deptTreeRef.value?.setCheckedKeys(deptCheckedKeys.value);
};

const confirmDeptSelection = () => {
	const checked = (deptTreeRef.value?.getCheckedNodes() ?? []) as any[];
	deptSelected.value = checked.map((node) => ({ id: node.id, name: node.name, code: node.code }));
	emit('update:modelValue', deptSelected.value.map((dept) => `dept:${dept.code || dept.id}`).join(','));
	deptTreePopoverVisible.value = false;
};

const removeDept = (index: number) => {
	deptSelected.value.splice(index, 1);
	emit('update:modelValue', deptSelected.value.map((dept) => `dept:${dept.code || dept.id}`).join(','));
};

const filterDeptNode = (value: string, data: any) => {
	if (!value) return true;
	return String(data.name ?? '').includes(value);
};
</script>
