<template>
	<el-dialog v-model="visible" title="权限设置" width="900px" draggable :close-on-click-modal="false" @close="handleClose">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Unlock /> </el-icon>
				<span> 权限设置 </span>
			</div>
		</template>
		<DocumentPermissionEditor v-model="permissionRows" :table-max-height="360" />

		<template #footer>
			<el-button icon="ele-CircleCloseFilled" @click="handleClose">取消</el-button>
			<el-button type="primary" icon="ele-CircleCheckFilled" @click="handleSave" :loading="saving">保存</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentPermissionApi } from '/@/api-services/document/api';
import { ref } from 'vue';
import { ElMessage } from 'element-plus';

import DocumentPermissionEditor from './DocumentPermissionEditor.vue';
import { buildPermissionPayload, normalizePermissionRows, type DocumentPermissionRow } from '../utils/permission';

const emit = defineEmits(['success']);

const visible = ref(false);
const saving = ref(false);
const documentId = ref<number | null>(null);
const permissionRows = ref<DocumentPermissionRow[]>([]);

const open = async (docId: number) => {
	documentId.value = docId;
	visible.value = true;
	await loadPermissionList();
};

const loadPermissionList = async () => {
	if (!documentId.value) return;

	try {
		const res = await getAPI(DocumentPermissionApi).apiDocumentPermissionListGet(documentId.value);
		// 确保返回的数据是数组
		const permissions = Array.isArray(res.data) ? res.data : Array.isArray(res.data?.result) ? res.data.result : [];
		permissionRows.value = normalizePermissionRows(permissions);
	} catch (error) {
		console.error('加载权限列表失败:', error);
		ElMessage.error('加载权限列表失败');
		permissionRows.value = [];
	}
};

const handleClose = () => {
	visible.value = false;
	documentId.value = null;
	permissionRows.value = [];
};

const handleSave = async () => {
	if (!documentId.value) {
		ElMessage.warning('文档ID不能为空');
		return;
	}

	try {
		saving.value = true;

		const permissions = buildPermissionPayload(permissionRows.value);

		await getAPI(DocumentPermissionApi).apiDocumentPermissionSetPost({
			documentId: documentId.value,
			permissions,
		});

		ElMessage.success('权限设置成功');
		emit('success');
		handleClose();
	} catch (error) {
		console.error('保存权限失败:', error);
		ElMessage.error('权限设置失败');
	} finally {
		saving.value = false;
	}
};

defineExpose({
	open,
});
</script>

<style lang="scss" scoped>
:deep(.el-dialog__body) {
	padding: 20px;
}
</style>
