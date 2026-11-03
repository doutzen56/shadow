<template>
	<div class="doc-category-dialog">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="600px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="上级分类">
							<el-cascader :options="props.categoryData" :props="cascaderProps" placeholder="请选择上级分类" clearable filterable class="w100" v-model="state.ruleForm.parentId">
								<template #default="{ node, data }">
									<span>{{ data.name }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="分类名称" prop="name" :rules="[{ required: true, message: '分类名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="分类名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="分类编码">
							<el-input v-model="state.ruleForm.code" placeholder="分类编码" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="图标">
							<IconSelector v-model="state.ruleForm.icon" :size="getGlobalComponentSize" placeholder="图标" type="all" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="排序">
							<el-input-number v-model="state.ruleForm.sort" placeholder="排序" class="w100" :min="0" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="分类颜色">
							<div style="display: flex; align-items: center; gap: 10px">
								<el-color-picker v-model="state.ruleForm.color" />
								<el-tag v-if="state.ruleForm.color" :color="state.ruleForm.color" effect="dark">{{ state.ruleForm.name || '预览' }}</el-tag>
								<span v-else style="color: #909399; font-size: 12px">未设置颜色，将使用默认颜色</span>
							</div>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="描述">
							<el-input v-model="state.ruleForm.description" placeholder="请输入描述内容" clearable type="textarea" :rows="3" />
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

<script lang="ts" setup name="editCategoryDialog">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentCategoryApi } from '/@/api-services/document/api';
import { computed, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import IconSelector from '/@/components/iconSelector/index.vue';
import other from '/@/utils/other';


const props = defineProps({
	title: String,
	categoryData: {
		type: Array,
		default: () => [],
	},
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {
		id: 0,
		parentId: 0,
		name: '',
		code: '',
		icon: '',
		sort: 100,
		color: '#409EFF',
		description: '',
	} as any,
});

// 级联选择器配置选项
const cascaderProps = { checkOnClickNode: true, checkStrictly: true, emitPath: false, value: 'id', label: 'name' };

// 获取全局组件大小
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row || { id: 0, parentId: 0, name: '', code: '', icon: '', sort: 100, color: '#409EFF', description: '' }));
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
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
			// 确保颜色值是有效的十六进制格式，且不超过16个字符
			const submitData = {
				...state.ruleForm,
				color: state.ruleForm.color ? state.ruleForm.color.trim().substring(0, 16) : undefined,
			};

			if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
				await getAPI(DocumentCategoryApi).apiDocumentCategoryUpdatePost(submitData);
			} else {
				await getAPI(DocumentCategoryApi).apiDocumentCategoryAddPost(submitData);
			}
			ElMessage.success('操作成功');
			closeDialog();
		} catch (error: any) {
			const errorMessage = error?.response?.data?.message || error?.message || '操作失败';
			ElMessage.error(errorMessage);
		}
	});
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.doc-category-dialog {
	.w100 {
		width: 100%;
	}
	.mb20 {
		margin-bottom: 20px;
	}
}
</style>
