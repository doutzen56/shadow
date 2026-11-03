<template>
	<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="600px">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
			<el-row :gutter="10">
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="'名称'" prop="name" :rules="[{ required: true, message: '名称不能为空', trigger: 'blur' }]">
						<el-input v-model="state.ruleForm.name" clearable />
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="'数据库类型'" prop="dbType" :rules="[{ required: true, message: '数据库类型不能为空', trigger: 'blur' }]">
						<g-sys-dict v-model="state.ruleForm.dbType" code="db_type" render-as="select" />
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="'连接字符串'" prop="connectionString" :rules="[{ required: !state.ruleForm.id, message: '连接字符串不能为空', trigger: 'blur' }]">
						<el-input v-model="state.ruleForm.connectionString" type="textarea" :autosize="{ minRows: 6 }" :placeholder="'编辑时置空则不修改原连接字符串'" />
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="'备注'">
						<el-input v-model="state.ruleForm.remark" clearable />
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
</template>

<script setup lang="ts" name="sysReportDataSourceEditForm">
import { reactive, ref } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportDataSourceApi } from '/@/api-services/system/api';
import { UpdateReportDataSourceInput } from '/@/api-services/system/models';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateReportDataSourceInput,
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
	// 连接字符串不回显，编辑时置空则不修改原连接字符串
	state.ruleForm.connectionString = '';
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
			await getAPI(SysReportDataSourceApi).apiSysReportDataSourceUpdatePost(state.ruleForm);
		} else {
			await getAPI(SysReportDataSourceApi).apiSysReportDataSourceAddPost(state.ruleForm);
		}
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
