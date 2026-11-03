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
						<el-form-item label="指定区划代码 (0或不输入，导入全部数据)" prop="code">
							<el-input-number v-model="state.ruleForm.code" placeholder="指定区划代码" class="w100" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="行政区级数（最大查询深度，最多支持2级深度，不支持村级）">
							<el-input-number v-model="state.ruleForm.level" placeholder="行政区级数" class="w100" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<div>
						<a href="https://dmfw.mca.gov.cn/9095/xzqh/getList?code=&maxLevel=4" target="_blank" style="float: left">国家地名信息库数据</a>
						<a href="https://dmfw.mca.gov.cn/interface.html" target="_blank" style="float: left; margin-left: 15px">接口说明</a>
					</div>
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
		code: 120000000000,
		level: 2,
	} as any,
	isShowDialog: false,
});

// 打开弹窗
const openDialog = () => {
	state.ruleForm.code = 120000000000;
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
			message: '努力同步中...',
			type: 'success',
			position: 'bottom-right',
		});
		state.loading = true;
		await getAPI(SysRegionApi).apiSysRegionSyncRegionMcaPost(state.ruleForm);
		closeDialog();
		ElMessage.success('生成成功');
		state.loading = false;
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
