<template>
	<div class="sys-dictType-container">
		<el-dialog v-model="state.visible" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="formRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="字典名称" prop="name" :rules="[{ required: true, message: '字典名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="字典名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="字典编码" prop="code" :rules="[{ required: true, message: '字典编码不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" placeholder="字典编码" clearable />
						</el-form-item>
					</el-col>
					<!-- <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20" v-if="!state.ruleForm.id && userInfo.accountType === AccountTypeEnum.NUMBER_999">
						<el-form-item label="租户字典" prop="isTenant" :rules="[{ required: true, message: '租户字典不能为空', trigger: 'blur' }]">
							<g-sys-dict v-model="state.ruleForm.isTenant" code="YesNoEnum" render-as="radio" />
						</el-form-item>
					</el-col> -->
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="状态">
							<g-sys-dict v-model="state.ruleForm.status" code="StatusEnum" render-as="radio" />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="排序">
							<el-input-number v-model="state.ruleForm.orderNo" placeholder="排序" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="备注">
							<el-input v-model="state.ruleForm.remark" placeholder="请输入备注内容" clearable type="textarea" />
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

<script lang="ts" setup name="sysEditDictType">
import { reactive, ref } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysDictTypeApi } from '/@/api-services/system/api';
import { AccountTypeEnum, UpdateDictTypeInput } from '/@/api-services/system/models';
import { useUserInfo } from '/@/stores/userInfo';

const props = defineProps({
	title: String,
});
const userInfo = useUserInfo().userInfos;
const emits = defineEmits(['handleQuery', 'handleUpdate']);
const formRef = ref();
const state = reactive({
	visible: false,
	ruleForm: {} as UpdateDictTypeInput,
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.visible = true;
	formRef.value?.resetFields();
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQuery');
	state.visible = false;
};

// 取消
const cancel = () => {
	state.visible = false;
};

// 提交
const submit = () => {
	formRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(SysDictTypeApi).apiSysDictTypeUpdatePost(state.ruleForm);
		} else {
			await getAPI(SysDictTypeApi).apiSysDictTypeAddPost(state.ruleForm);
		}
		emits('handleUpdate');
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
