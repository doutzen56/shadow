<template>
	<div class="document-batch-transfer-dialog-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="500px" title="批量转移文档">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-FolderOpened /> </el-icon>
					<span>批量转移文档</span>
				</div>
			</template>
			<div class="transfer-content">
				<div class="selected-info">
					<el-alert type="info" :closable="false" show-icon>
						<template #default>
							<span
								>已选择 <strong>{{ documentIds.length }}</strong> 个文档，请选择要转移至的目标分类</span
							>
						</template>
					</el-alert>
				</div>
				<div class="category-select">
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
						<el-form-item label="目标分类" prop="categoryId" :rules="[{ required: true, message: '请选择目标分类', trigger: 'change' }]">
							<el-cascader :options="categoryTree" :props="cascaderProps" placeholder="请选择目标分类" clearable filterable class="w100" v-model="state.ruleForm.categoryId" />
						</el-form-item>
					</el-form>
				</div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit" :loading="state.loading">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="documentBatchTransferDialog">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi, DocumentCategoryApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';


const emits = defineEmits<{
	success: [];
}>();

const ruleFormRef = ref();
const categoryTree = ref<any[]>([]);
const documentIds = ref<number[]>([]);
const categoryLoaded = ref(false);

const state = reactive({
	isShowDialog: false,
	loading: false,
	ruleForm: {
		categoryId: null as number | null,
	} as any,
});

// 级联选择器配置选项
const cascaderProps = {
	checkOnClickNode: true,
	checkStrictly: true,
	emitPath: false,
	value: 'id',
	label: 'name',
};

// 加载分类树
const loadCategoryTree = async () => {
	try {
		const res = await getAPI(DocumentCategoryApi).apiDocumentCategoryTreeGet();
		categoryTree.value = res.data?.result || [];
		categoryLoaded.value = true;
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '加载分类树失败');
		categoryTree.value = [];
	}
};

// 打开弹窗
const open = async (ids: number[]) => {
	if (!ids || ids.length === 0) {
		ElMessage.warning('请先选择要转移的文档');
		return;
	}
	if (!categoryLoaded.value) await loadCategoryTree();
	documentIds.value = ids;
	state.ruleForm.categoryId = null;
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
};

// 关闭弹窗
const closeDialog = () => {
	state.isShowDialog = false;
	// 延迟触发，确保对话框关闭动画完成
	setTimeout(() => {
		emits('success');
	}, 100);
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 提交
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		if (!state.ruleForm.categoryId) {
			ElMessage.warning('请选择目标分类');
			return;
		}

		state.loading = true;
		try {
			// 批量更新文档分类（后端已支持部分更新，只需传递id和categoryId）
			// 如果categoryId为0，转换为null（根分类用null表示）
			const targetCategoryId = state.ruleForm.categoryId === 0 ? null : state.ruleForm.categoryId;
			const updatePromises = documentIds.value.map((id) =>
				getAPI(DocumentApi).apiDocumentUpdatePost({
					id: id,
					categoryId: targetCategoryId, // 只更新分类ID，其他字段保持不变
				})
			);

			await Promise.all(updatePromises);
			ElMessage.success(`成功转移 ${documentIds.value.length} 个文档`);
			closeDialog();
		} catch (error: any) {
			const errorMessage = error?.response?.data?.message || error?.message || '转移失败';
			ElMessage.error(errorMessage);
		} finally {
			state.loading = false;
		}
	});
};

// 导出对象
defineExpose({ open });
</script>

<style lang="scss" scoped>
.document-batch-transfer-dialog-container {
	.w100 {
		width: 100%;
	}

	.transfer-content {
		.selected-info {
			margin-bottom: 20px;
		}

		.category-select {
			margin-top: 20px;
		}
	}
}
</style>
