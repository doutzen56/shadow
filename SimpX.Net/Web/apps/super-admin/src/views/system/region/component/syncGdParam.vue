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
				<span> 此操作会更新行政区划表所有数据，请慎重操作！！！ </span>
			</div>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="top">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="高德地图开发者Key" prop="key" :rules="[{ required: true, message: '高德地图开发者Key不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.key" placeholder="高德地图开发者Key" clearable />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="查询关键字（行政区名称、citycode、adcode）">
							<el-input v-model="state.ruleForm.keywords" placeholder="查询关键字" clearable />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="行政区级数（不支持村级）">
							<el-input-number v-model="state.ruleForm.level" placeholder="行政区级数" class="w100" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<a href="https://lbs.amap.com/" target="_blank" style="float: left">高德开发平台</a>
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
	ruleForm: {
		keywords: '天津市',
		level: 5,
	} as any,
	isShowDialog: false,
});

// 打开弹窗
const openDialog = () => {
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

		if (state.ruleForm.key == null || state.ruleForm.key == '' || state.ruleForm.key.length < 30) {
			ElMessage.error('请正确输入高德地图开发者 Key 值');
			return;
		}

		ElNotification({
			title: '提示',
			message: '努力同步中...',
			type: 'success',
			position: 'bottom-right',
		});
		state.loading = true;
		await getAPI(SysRegionApi).apiSysRegionSyncRegionGDPost(state.ruleForm);
		closeDialog();
		ElMessage.success('生成成功');
		state.loading = false;
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
