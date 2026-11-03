<template>
	<div class="document-permission-editor">
		<el-form :model="form" label-width="auto" class="permission-form">
			<el-form-item label="授权对象">
				<div class="object-picker">
					<el-radio-group v-model="form.objectType">
						<el-radio v-for="item in DOCUMENT_OBJECT_TYPE_OPTIONS" :key="item.value" :label="item.value" :value="item.value">
							{{ item.label }}
						</el-radio>
					</el-radio-group>
					<el-button type="primary" icon="ele-Plus" style="margin-left: 20px" @click="handleOpenSelector">选择对象</el-button>
				</div>
			</el-form-item>
		</el-form>

		<div class="permission-toolbar">
			<el-checkbox
				:model-value="isAllPermissionsChecked()"
				:indeterminate="isAllPermissionsIndeterminate()"
				:disabled="permissionRows.length === 0"
				@change="(checked) => handleAllPermissionsChange(Boolean(checked))"
			>
				全部权限
			</el-checkbox>
		</div>

		<vxe-grid v-bind="permissionGridOptions" :data="permissionRows">
			<template #perm_objectType="{ row }">
				<el-tag :type="getObjectTypeTag(row.objectType)">{{ getObjectTypeLabel(row.objectType) }}</el-tag>
			</template>
			<template v-for="item in DOCUMENT_PERMISSION_OPTIONS" :key="item.value" #[`perm_header_${item.value}`]>
				<el-checkbox
					:model-value="isPermissionColumnAllChecked(item.value)"
					:indeterminate="isPermissionColumnIndeterminate(item.value)"
					:disabled="permissionRows.length === 0"
					@change="(checked) => handlePermissionColumnChange(item.value, Boolean(checked))"
				>
					{{ item.label }}
				</el-checkbox>
			</template>
			<template v-for="item in DOCUMENT_PERMISSION_OPTIONS" :key="item.value" #[`perm_cell_${item.value}`]="{ row }">
				<el-checkbox :model-value="hasRowPermission(row, item.value)" @change="(checked) => handleRowPermissionChange(row, item.value, Boolean(checked))" />
			</template>
			<template #perm_buttons="{ row }">
				<el-button link type="danger" icon="ele-CircleCloseFilled" @click="handleDeletePermission(row)">删除</el-button>
			</template>
		</vxe-grid>

		<UserSelectorDialog v-model:visible="userSelectorVisible" :data="[]" @change="handleUserChange" />
		<OrgSelectorDialog v-model:visible="orgSelectorVisible" :data="[]" @change="handleOrgChange" />
		<RoleSelectorDialog v-model:visible="roleSelectorVisible" :data="[]" @change="handleRoleChange" />
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref, watch } from 'vue';
import { VxeGridProps } from 'vxe-table';
import UserSelectorDialog from '/@/components/selector/userSelectorDialog.vue';
import OrgSelectorDialog from '/@/components/selector/orgSelectorDialog.vue';
import RoleSelectorDialog from '/@/components/selector/roleSelectorDialog.vue';
import { clonePermissionRows, DOCUMENT_OBJECT_TYPE_OPTIONS, DOCUMENT_PERMISSION_OPTIONS, type DocumentPermissionRow } from '../utils/permission';

const props = withDefaults(
	defineProps<{
		modelValue?: DocumentPermissionRow[];
		tableMaxHeight?: number | string;
	}>(),
	{
		modelValue: () => [],
		tableMaxHeight: 320,
	}
);

const emit = defineEmits<{
	'update:modelValue': [rows: DocumentPermissionRow[]];
	change: [rows: DocumentPermissionRow[]];
}>();

const permissionRows = ref<DocumentPermissionRow[]>([]);
const userSelectorVisible = ref(false);
const orgSelectorVisible = ref(false);
const roleSelectorVisible = ref(false);

// 权限矩阵表格：各权限列表头与单元格都是复选框，用动态插槽按权限值命名
const permissionGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	maxHeight: props.tableMaxHeight,
	emptyText: '暂无权限',
	columns: [
		{ field: 'objectType', title: '对象类型', width: 100, slots: { default: 'perm_objectType' } },
		{ field: 'objectName', title: '对象名称', minWidth: 150, showOverflow: 'tooltip' },
		...DOCUMENT_PERMISSION_OPTIONS.map((item) => ({
			field: `perm_${item.value}`,
			title: item.label,
			minWidth: 90,
			slots: { header: `perm_header_${item.value}`, default: `perm_cell_${item.value}` },
		})),
		{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', slots: { default: 'perm_buttons' } },
	],
});

const form = reactive({
	objectType: 1,
});

watch(
	() => props.modelValue,
	(value) => {
		permissionRows.value = clonePermissionRows(value || []);
	},
	{ immediate: true, deep: true }
);

const syncRows = () => {
	const rows = clonePermissionRows(permissionRows.value);
	permissionRows.value = rows;
	emit('update:modelValue', rows);
	emit('change', rows);
};

const getObjectTypeLabel = (objectType: number) => DOCUMENT_OBJECT_TYPE_OPTIONS.find((item) => item.value === objectType)?.label || '未知';
const getObjectTypeTag = (objectType: number) => DOCUMENT_OBJECT_TYPE_OPTIONS.find((item) => item.value === objectType)?.tagType || 'info';
const permissionValues = DOCUMENT_PERMISSION_OPTIONS.map((item) => item.value);

const handleOpenSelector = () => {
	if (form.objectType === 1) {
		userSelectorVisible.value = true;
	} else if (form.objectType === 2) {
		orgSelectorVisible.value = true;
	} else if (form.objectType === 3) {
		roleSelectorVisible.value = true;
	}
};

const handleUserChange = (users: any[]) => {
	handleSelectedObjects(
		users.map((user: any) => ({
			id: user.id,
			name: user.realName || user.account,
		}))
	);
};

const handleOrgChange = (orgs: any[]) => {
	handleSelectedObjects(
		orgs.map((org: any) => ({
			id: org.targetId || org.id,
			name: org.name,
		}))
	);
};

const handleRoleChange = (roles: any[]) => {
	handleSelectedObjects(
		roles.map((role: any) => ({
			id: role.targetId || role.id,
			name: role.name,
		}))
	);
};

const handleSelectedObjects = (objects: Array<{ id: number; name: string }>) => {
	const validObjects = objects.filter((item) => item.id);
	if (validObjects.length === 0) return;

	validObjects.forEach((object) => {
		const objectId = Number(object.id);
		const exists = permissionRows.value.find((row) => row.objectType === form.objectType && row.objectId === objectId);

		if (exists) {
			exists.objectName = object.name || exists.objectName;
		} else {
			permissionRows.value.push({
				objectType: form.objectType,
				objectId,
				objectName: object.name,
				permissionTypes: [1],
			});
		}
	});

	syncRows();
};

const hasRowPermission = (row: DocumentPermissionRow, permissionType: number) => row.permissionTypes.includes(permissionType);

const setRowPermission = (row: DocumentPermissionRow, permissionType: number, checked: boolean) => {
	if (checked) {
		row.permissionTypes = Array.from(new Set([...row.permissionTypes, permissionType])).sort((a, b) => a - b);
		return;
	}

	row.permissionTypes = row.permissionTypes.filter((item) => item !== permissionType);
};

const handleRowPermissionChange = (row: DocumentPermissionRow, permissionType: number, checked: boolean) => {
	setRowPermission(row, permissionType, checked);
	syncRows();
};

const isPermissionColumnAllChecked = (permissionType: number) => permissionRows.value.length > 0 && permissionRows.value.every((row) => hasRowPermission(row, permissionType));

const isPermissionColumnIndeterminate = (permissionType: number) => {
	const checkedCount = permissionRows.value.filter((row) => hasRowPermission(row, permissionType)).length;
	return checkedCount > 0 && checkedCount < permissionRows.value.length;
};

const handlePermissionColumnChange = (permissionType: number, checked: boolean) => {
	permissionRows.value.forEach((row) => setRowPermission(row, permissionType, checked));
	syncRows();
};

const isAllPermissionsChecked = () => permissionRows.value.length > 0 && permissionRows.value.every((row) => permissionValues.every((permissionType) => hasRowPermission(row, permissionType)));

const isAllPermissionsIndeterminate = () => {
	const totalCount = permissionRows.value.length * permissionValues.length;
	if (totalCount === 0) return false;

	const checkedCount = permissionRows.value.reduce((count, row) => count + row.permissionTypes.filter((permissionType) => permissionValues.includes(permissionType)).length, 0);
	return checkedCount > 0 && checkedCount < totalCount;
};

const handleAllPermissionsChange = (checked: boolean) => {
	permissionRows.value.forEach((row) => {
		row.permissionTypes = checked ? [...permissionValues] : [];
	});
	syncRows();
};

const handleDeletePermission = (permission: DocumentPermissionRow) => {
	const index = permissionRows.value.indexOf(permission);
	if (index > -1) {
		permissionRows.value.splice(index, 1);
		syncRows();
	}
};
</script>

<style lang="scss" scoped>
.document-permission-editor {
	width: 100%;

	.permission-form {
		margin-bottom: 12px;

		:deep(.el-form-item:last-child) {
			margin-bottom: 0;
		}
	}

	.object-picker {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 12px;
	}

	.permission-toolbar {
		display: flex;
		justify-content: flex-end;
		margin-bottom: 8px;
	}

	:deep(.el-checkbox-group) {
		display: flex;
		flex-wrap: wrap;
		gap: 8px 16px;
	}
}
</style>
