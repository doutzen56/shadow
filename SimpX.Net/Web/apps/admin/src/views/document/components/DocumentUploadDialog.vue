<template>
	<el-dialog v-model="visible" title="上传文档" width="900px" draggable :close-on-click-modal="false" @close="handleClose">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
				<span> 上传文档 </span>
			</div>
		</template>

		<el-steps :active="activeStep" finish-status="success" simple class="upload-steps">
			<el-step title="选择文件" />
			<el-step title="设置权限" />
		</el-steps>

		<div v-show="activeStep === 0">
			<el-form :model="form" label-width="auto" style="margin-bottom: 20px">
				<el-form-item label="分类">
					<el-cascader v-model="form.categoryId" :options="categoryTree" :props="cascaderProps" placeholder="请选择分类（可选）" clearable filterable class="w100" />
				</el-form-item>
				<el-form-item label="标签">
					<el-select v-model="form.tagIds" placeholder="请选择标签（可选，可多选）" clearable multiple filterable class="w100">
						<el-option v-for="tag in tagList" :key="tag.id" :label="tag.name" :value="tag.id">
							<el-tag :color="tag.color || '#409EFF'" effect="dark" style="margin-right: 5px">{{ tag.name }}</el-tag>
						</el-option>
					</el-select>
				</el-form-item>
				<el-form-item label="描述">
					<el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入文档描述（可选）" maxlength="500" show-word-limit />
				</el-form-item>
			</el-form>

			<el-divider />

			<el-upload ref="uploadRef" v-model:file-list="fileList" class="upload-demo" drag :auto-upload="false" :on-change="handleFileChange" :on-remove="handleFileRemove" multiple>
				<el-icon class="el-icon--upload"><ele-UploadFilled /></el-icon>
				<div class="el-upload__text">将文件拖到此处，或<em>点击选择</em></div>
				<template #tip>
					<div class="el-upload__tip">支持多种格式：Word、Excel、PPT、PDF、图片、压缩包等，单文件最大500MB</div>
				</template>
			</el-upload>
		</div>

		<div v-show="activeStep === 1" class="permission-step">
			<div class="selected-file-summary">
				<el-tag v-for="file in fileList" :key="file.uid" effect="plain">{{ file.name }}</el-tag>
			</div>
			<DocumentPermissionEditor v-model="permissionRows" :table-max-height="260" />
		</div>

		<div v-if="uploadList.length > 0" class="upload-list">
			<div v-for="item in uploadList" :key="item.uid" class="upload-item">
				<div class="upload-info">
					<span>{{ item.name }}</span>
					<el-progress :percentage="item.percentage" :status="item.status === 'error' ? 'exception' : item.status === 'success' ? 'success' : undefined" />
				</div>
				<el-button v-if="item.status === 'success'" type="success" circle>
					<el-icon><ele-Check /></el-icon>
				</el-button>
				<el-button v-else-if="item.status === 'error'" type="danger" circle>
					<el-icon><ele-Close /></el-icon>
				</el-button>
			</div>
		</div>

		<template #footer>
			<el-button icon="ele-CircleCloseFilled" :disabled="uploading" @click="handleClose">取消</el-button>
			<el-button v-if="activeStep === 1" :disabled="uploading" icon="ele-DArrowLeft" @click="activeStep = 0">上一步</el-button>
			<el-button v-if="activeStep === 0" type="primary" icon="ele-DArrowRight" :disabled="fileList.length === 0" @click="handleNext">下一步</el-button>
			<el-button v-else type="primary" icon="ele-Upload" :loading="uploading" :disabled="fileList.length === 0" @click="handleConfirm">开始上传</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentCategoryApi, DocumentTagApi } from '/@/api-services/document/api';
import { computed, ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';

import { service } from '/@/utils/request';
import DocumentPermissionEditor from './DocumentPermissionEditor.vue';
import { appendPermissionPayloadToFormData, buildPermissionPayload, type DocumentPermissionRow } from '../utils/permission';

const visible = ref(false);
const uploadRef = ref();
const activeStep = ref(0);
const uploading = ref(false);
const fileList = ref<any[]>([]);
const uploadList = reactive<any[]>([]);
const categoryId = ref<number | null>(null);
const categoryTree = ref<any[]>([]);
const tagList = ref<any[]>([]);
const categoryLoaded = ref(false);
const tagLoaded = ref(false);
const permissionRows = ref<DocumentPermissionRow[]>([]);

const form = reactive({
	categoryId: null as number | null,
	tagIds: [] as number[],
	description: '',
});

const cascaderProps = {
	value: 'id',
	label: 'name',
	children: 'children',
	checkStrictly: true,
	emitPath: false,
};

const successCount = computed(() => uploadList.filter((item) => item.status === 'success').length);

const loadCategoryTree = async () => {
	try {
		const res = await getAPI(DocumentCategoryApi).apiDocumentCategoryTreeGet();
		categoryTree.value = res.data?.result || [];
		categoryLoaded.value = true;
	} catch (error: any) {
		console.error('加载分类树失败:', error);
	}
};

const loadTagList = async () => {
	try {
		const res = await getAPI(DocumentTagApi).apiDocumentTagListGet();
		tagList.value = res.data?.result || [];
		tagLoaded.value = true;
	} catch (error: any) {
		console.error('加载标签列表失败:', error);
	}
};

const open = async (catId?: number | null) => {
	if (!categoryLoaded.value) await loadCategoryTree();
	if (!tagLoaded.value) await loadTagList();
	categoryId.value = catId || null;
	form.categoryId = catId || null;
	form.tagIds = [];
	form.description = '';
	activeStep.value = 0;
	visible.value = true;
	resetUploadState();
};

const resetUploadState = () => {
	uploading.value = false;
	fileList.value = [];
	permissionRows.value = [];
	uploadList.length = 0;
	uploadRef.value?.clearFiles?.();
};

const handleClose = () => {
	if (uploading.value) {
		ElMessage.warning('文件正在上传，请稍后');
		return;
	}

	visible.value = false;
	activeStep.value = 0;
	resetUploadState();
};

const validateFile = (file: File) => {
	const maxSize = 500 * 1024 * 1024;
	if (file.size > maxSize) {
		ElMessage.error(`${file.name} 大小不能超过500MB`);
		return false;
	}
	return true;
};

const handleFileChange = (file: any, files: any[]) => {
	if (file.raw && !validateFile(file.raw)) {
		fileList.value = files.filter((item) => item.uid !== file.uid);
		return;
	}

	fileList.value = files.map((item) => ({
		...item,
		status: item.status || 'ready',
		percentage: item.percentage || 0,
	}));
};

const handleFileRemove = (_file: any, files: any[]) => {
	fileList.value = files;
};

const handleNext = () => {
	if (fileList.value.length === 0) {
		ElMessage.warning('请先选择文件');
		return;
	}

	activeStep.value = 1;
};

const handleConfirm = async () => {
	if (fileList.value.length === 0) {
		ElMessage.warning('请先选择文件');
		return;
	}

	try {
		uploading.value = true;
		uploadList.length = 0;
		const failedFiles: string[] = [];

		fileList.value.forEach((file) => {
			uploadList.push({
				uid: file.uid,
				name: file.name,
				percentage: 0,
				status: 'uploading',
			});
		});

		for (const file of fileList.value) {
			try {
				await uploadSingleFile(file);
			} catch (error) {
				failedFiles.push(file.name);
			}
		}

		uploading.value = false;
		if (failedFiles.length > 0) {
			if (successCount.value > 0) emit('success');
			ElMessage.error(`上传完成，成功 ${successCount.value} 个，失败 ${failedFiles.length} 个`);
			return;
		}

		ElMessage.success(`上传完成，共 ${successCount.value} 个文件`);
		emit('success');
		handleClose();
	} catch (error: any) {
		ElMessage.error(error?.message || '上传失败');
	} finally {
		uploading.value = false;
	}
};

const uploadSingleFile = async (fileItem: any) => {
	const rawFile = fileItem.raw as File;
	const progressItem = uploadList.find((item) => item.uid === fileItem.uid);
	if (!rawFile || !progressItem) return;

	try {
		const formData = new FormData();
		formData.append('File', rawFile);

		const finalCategoryId = form.categoryId || categoryId.value;
		if (finalCategoryId) {
			formData.append('CategoryId', finalCategoryId.toString());
		}

		if (form.tagIds.length > 0) {
			form.tagIds.forEach((tagId, index) => {
				formData.append(`TagIds[${index}]`, tagId.toString());
			});
		}

		if (form.description && form.description.trim()) {
			formData.append('Description', form.description.trim());
		}

		appendPermissionPayloadToFormData(formData, buildPermissionPayload(permissionRows.value));

		const response = await service.post('/api/document/upload', formData, {
			headers: {
				'Content-Type': 'multipart/form-data',
			},
			onUploadProgress: (progressEvent) => {
				if (progressEvent.total) {
					progressItem.percentage = Math.round((progressEvent.loaded * 100) / progressEvent.total);
				}
			},
		});

		const responseData = response.data;
		if (responseData?.code === 200 || responseData?.result) {
			progressItem.status = 'success';
			progressItem.percentage = 100;
			return;
		}

		progressItem.status = 'error';
		throw new Error(responseData?.message || `${fileItem.name} 上传失败`);
	} catch (error: any) {
		progressItem.status = 'error';
		throw new Error(error?.response?.data?.message || error?.message || `${fileItem.name} 上传失败`);
	}
};

const emit = defineEmits<{
	success: [];
}>();

defineExpose({
	open,
});
</script>

<style lang="scss" scoped>
.upload-steps {
	margin-bottom: 20px;
}

.upload-demo {
	:deep(.el-upload-dragger) {
		width: 100%;
	}
}

.permission-step {
	.selected-file-summary {
		display: flex;
		flex-wrap: wrap;
		gap: 8px;
		margin-bottom: 16px;
	}
}

.upload-list {
	margin-top: 20px;
	max-height: 220px;
	overflow-y: auto;

	.upload-item {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 10px;
		border-bottom: 1px solid var(--el-border-color);

		.upload-info {
			flex: 1;
			margin-right: 10px;

			span {
				display: block;
				margin-bottom: 5px;
			}
		}
	}
}

.w100 {
	width: 100%;
}
</style>
