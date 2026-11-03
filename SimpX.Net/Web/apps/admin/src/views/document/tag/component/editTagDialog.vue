<template>
	<div class="doc-tag-dialog">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="500px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="标签名称" prop="name" :rules="[{ required: true, message: '标签名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="标签名称" clearable maxlength="64" show-word-limit />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="标签颜色">
							<div style="display: flex; align-items: center; gap: 10px">
								<el-color-picker v-model="state.ruleForm.color" />
								<el-tag v-if="state.ruleForm.color" :color="state.ruleForm.color" effect="dark">{{ state.ruleForm.name || '预览' }}</el-tag>
								<span v-else style="color: var(--el-color-primary-light-3)'; font-size: 12px">未设置颜色，将使用默认颜色</span>
							</div>
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

<script lang="ts" setup name="editTagDialog">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentTagApi } from '/@/api-services/document/api';
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';


const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {
		id: 0,
		name: '',
		color: '#248067',
		useCount: 0,
	} as any,
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(
		JSON.stringify(
			row || {
				id: 0,
				name: '',
				color: '#248067',
				useCount: 0,
			}
		)
	);
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
				color: state.ruleForm.color ? state.ruleForm.color.trim().substring(0, 16) : '#409EFF',
			};

			if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
				await getAPI(DocumentTagApi).apiDocumentTagUpdatePost(submitData);
			} else {
				await getAPI(DocumentTagApi).apiDocumentTagAddPost(submitData);
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
.doc-tag-dialog {
	.mb20 {
		margin-bottom: 20px;
	}
}
</style>
