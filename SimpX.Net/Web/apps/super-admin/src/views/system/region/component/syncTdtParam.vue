<template>
	<div class="sys-region-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="500px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="top">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="区划名称或编码" prop="keyword" :rules="[{ required: true, message: '区划名称或编码不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.keyword" placeholder="区划名称或编码" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="下级行政区级数">
							<el-select v-model="state.ruleForm.childLevel" placeholder="下级行政区级数" clearable>
								<el-option value="0" label="不返回下级行政区" />
								<el-option value="1" label="返回下一级行政区" />
								<el-option value="2" label="返回下两级行政区" />
								<el-option value="3" label="返回下三级行政区" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="密钥" prop="tk" :rules="[{ required: true, message: '密钥不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.tk" placeholder="输入密钥" clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<a href="http://lbs.tianditu.gov.cn/server/administrative2.html" target="_blank" style="float: left">天地图</a>
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
	ruleForm: {} as any,
	isShowDialog: false,
});

// 打开弹窗
const openDialog = () => {
	ruleFormRef.value?.resetFields();
	state.ruleForm.childLevel = '2';
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
			message: '努力同步中...',
			type: 'success',
			position: 'bottom-right',
		});
		state.loading = true;
		await getAPI(SysRegionApi).apiSysRegionSyncRegionTiandituPost(state.ruleForm);
		closeDialog();
		ElMessage.success('生成成功');
		state.loading = false;
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
