<template>
	<div class="sys-region-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="500px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<div style="color: red; padding: 10px 10px; background: #faecd8; margin-bottom: 10px">
				<el-icon style="transform: translateY(2px)"><ele-Bell /></el-icon>
				<span> 只会更新和新增组织架构，不会删除已有的组织架构数据！ </span>
			</div>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="top">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="选择层级" prop="level" :rules="[{ required: true, message: '请选择层级', trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.level" filterable clearable class="w100">
								<el-option :label="1" :value="1" />
								<el-option :label="2" :value="2" />
								<el-option :label="3" :value="3" />
								<el-option :label="4" :value="4" />
								<el-option :label="5" :value="5" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.loading" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { ElMessage, ElNotification } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { SysRegionApi } from '/@/api-services/system/api';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	loading: false,
	ruleForm: { level: 3 } as any,
	isShowDialog: false,
});

// 打开弹窗
const openDialog = (id: any) => {
	if (!id) ElMessage.error('行政区域数据错误！');
	state.ruleForm.id = id;
	ruleFormRef.value?.resetFields();
	state.isShowDialog = true;
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

		ElNotification({
			title: '提示',
			message: '努力生成中...',
			type: 'success',
			position: 'bottom-right',
		});
		state.loading = true;
		await getAPI(SysRegionApi).apiSysRegionGenOrgPost(state.ruleForm);
		closeDialog();

		ElMessage.success('生成成功');
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
