<template>
	<div class="sysEditOAuthUser-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="昵称" prop="nickName" :rules="[{ required: true, message: '昵称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.nickName" placeholder="昵称" clearable />
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

<script lang="ts" setup name="sysEditOAuthUser">
import { reactive, ref } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysOAuthUserApi } from '/@/api-services/system/api';
import { SysOAuthUser } from '/@/api-services/system/models';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as SysOAuthUser,
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
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
		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(SysOAuthUserApi).apiSysOAuthUserUpdatePost(state.ruleForm);
		} else {
			await getAPI(SysOAuthUserApi).apiSysOAuthUserAddPost(state.ruleForm);
		}
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
