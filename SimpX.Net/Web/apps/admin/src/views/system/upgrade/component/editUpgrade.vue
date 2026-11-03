<template>
	<div class="sys-upgrade-container">
		<vxe-modal v-model="state.isShowDialog" width="60vw" height="600px" resize show-footer show-zoom @close="cancel">
			<template #title>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<template #default>
				<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
					<el-form-item label="" prop="content" :rules="[{ required: true, message: '系统更新日志不能为空', trigger: 'blur' }]">
						<Editor v-model:get-html="state.ruleForm.content" style="width: 100%; height: 100%" />
					</el-form-item>
				</el-form>
			</template>
			<template #footer>
				<!-- <vxe-button icon="vxe-icon-error-circle-fill" @click="cancel">取消</vxe-button> -->
				<!-- <vxe-button status="primary" icon="vxe-icon-success-circle-fill" @click="submit">确定</vxe-button> -->
				<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
			</template>
		</vxe-modal>
	</div>
</template>

<script lang="ts" setup name="sysUpgradeEdit">
import { reactive, ref } from 'vue';
import Editor from '/@/components/editor/index.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysUpgradeApi } from '/@/api-services/system/api';
import { SysUpgrade } from '/@/api-services/system/models';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as SysUpgrade,
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
		if (!valid || state.ruleForm.content == undefined || state.ruleForm.content.length < 20) return;
		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(SysUpgradeApi).apiSysUpgradeUpdatePost(state.ruleForm);
		} else {
			await getAPI(SysUpgradeApi).apiSysUpgradeAddPost(state.ruleForm);
		}
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
// :deep(.vxe-modal--header) {
// 	background: var(--el-color-primary) !important;
// 	color: #fff;
// }
</style>
