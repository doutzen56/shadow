<template>
	<div class="document-edit-dialog-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="600px" title="编辑文档">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>编辑文档</span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="文档名称" prop="name" :rules="[{ required: true, message: '文档名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="请输入文档名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="文档路径">
							<el-input :model-value="state.ruleForm.fullPath" type="textarea" :rows="2" readonly placeholder="暂无路径信息" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="所属分类">
							<el-cascader :options="categoryTree" :props="cascaderProps" placeholder="请选择分类" clearable filterable class="w100" v-model="state.ruleForm.categoryId" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="标签">
							<el-select v-model="state.ruleForm.tagIds" placeholder="请选择标签（可选，可多选）" clearable multiple filterable class="w100">
								<el-option v-for="tag in tagList" :key="tag.id" :label="tag.name" :value="tag.id">
									<el-tag :color="tag.color || '#409EFF'" effect="dark" style="margin-right: 5px">{{ tag.name }}</el-tag>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="描述">
							<el-input v-model="state.ruleForm.description" placeholder="请输入描述内容" clearable type="textarea" :rows="3" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="排序码">
							<el-input-number v-model="state.ruleForm.sort" placeholder="请输入排序码" :min="0" :max="999999" controls-position="right" class="w100" />
							<div style="font-size: 12px; color: #909399; margin-top: 5px">数字越小越靠前，默认为0</div>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="documentEditDialog">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi, DocumentCategoryApi, DocumentTagApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { resolveDocumentFileUrl } from '/@/utils/documentFileUrl';

const emits = defineEmits<{
	handleQuery: [];
}>();

const ruleFormRef = ref();
const categoryTree = ref<any[]>([]);
const tagList = ref<any[]>([]);
const categoryLoaded = ref(false);
const tagLoaded = ref(false);

const state = reactive({
	isShowDialog: false,
	ruleForm: {
		id: 0,
		name: '',
		fullPath: '',
		categoryId: null as number | null,
		tagIds: [] as number[],
		description: '',
		sort: 0,
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

/** 解析可访问的完整文件路径（优先 fileUrl，其次版本记录中的 fileUrl） */
const resolveFullPath = (doc: any): string => {
	if (!doc) return '';
	const versionUrl = doc.versions?.find((v: any) => v?.fileUrl)?.fileUrl ?? doc.versions?.[0]?.fileUrl;
	const raw = doc.fileUrl || versionUrl || doc.filePath || '';
	return resolveDocumentFileUrl(raw, doc.preUrl);
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

// 加载标签列表
const loadTagList = async () => {
	try {
		const res = await getAPI(DocumentTagApi).apiDocumentTagListGet();
		tagList.value = res.data?.result || [];
		tagLoaded.value = true;
	} catch (error: any) {
		console.error('加载标签列表失败:', error);
		tagList.value = [];
	}
};

// 打开弹窗
const openDialog = async (document: any) => {
	if (!categoryLoaded.value) await loadCategoryTree();
	if (!tagLoaded.value) await loadTagList();

	let docData = document;
	try {
		const detailRes = await getAPI(DocumentApi).apiDocumentDetailGet(document.id);
		if (detailRes.data?.result) {
			docData = { ...document, ...detailRes.data.result };
		}
	} catch (error) {
		console.error('获取文档详情失败:', error);
	}

	// 获取标签ID列表
	const tagIds: number[] = [];
	if (docData.tags && Array.isArray(docData.tags)) {
		docData.tags.forEach((tag: any) => {
			if (tag.id) {
				tagIds.push(tag.id);
			}
		});
	}

	state.ruleForm = {
		id: docData.id,
		name: docData.name || '',
		fullPath: resolveFullPath(docData),
		categoryId: docData.categoryId || null,
		tagIds: tagIds,
		description: docData.description || '',
		sort: docData.sort ?? 0,
	};
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
};

// 关闭弹窗
const closeDialog = () => {
	state.isShowDialog = false;
	// 延迟触发，确保对话框关闭动画完成
	setTimeout(() => {
		emits('handleQuery');
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
		try {
			const categoryId = state.ruleForm.categoryId ?? null;
			await getAPI(DocumentApi).apiDocumentUpdatePost({
				id: state.ruleForm.id,
				name: state.ruleForm.name,
				categoryId,
				clearCategory: categoryId === null,
				tagIds: state.ruleForm.tagIds ?? [],
				description: state.ruleForm.description ?? '',
				sort: state.ruleForm.sort ?? 0,
			});
			ElMessage.success('编辑成功');
			closeDialog();
		} catch (error: any) {
			const errorMessage = error?.response?.data?.message || error?.message || '编辑失败';
			ElMessage.error(errorMessage);
		}
	});
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.document-edit-dialog-container {
	.w100 {
		width: 100%;
	}
	.mb20 {
		margin-bottom: 20px;
	}
}
</style>
