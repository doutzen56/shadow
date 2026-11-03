<template>
	<div class="sys-dbTable-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="600px">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>表编辑</span>
					<span class="db-dlg-sub">改名会同步改动数据库表名，请确认无引用后再改</span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="表名称" prop="tableName" :rules="[{ required: true, message: '名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.tableName" placeholder="表名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="描述" prop="description" :rules="[{ required: true, message: '描述不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.description" placeholder="描述" clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取 消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit" :loading="state.loading">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditTable">
import { nextTick, reactive, ref } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';
import { UpdateDbTableInput } from '/@/api-services/system/models';

const emits = defineEmits(['handleQueryTable']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	loading: false,
	ruleForm: {} as UpdateDbTableInput,
});

// 打开弹窗
const openDialog = (row: any) => {
	// 深拷贝：直接引用行对象会让「取消」也把表格里的值改掉
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
	nextTick(() => ruleFormRef.value?.resetFields());
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQueryTable');
	state.isShowDialog = false;
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 提交
const submit = async () => {
	try {
		await ruleFormRef.value.validate();
	} catch {
		return;
	}
	state.loading = true;
	try {
		await getAPI(SysDatabaseApi).apiSysDatabaseUpdateTablePost(state.ruleForm);
		closeDialog();
	} finally {
		state.loading = false;
	}
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.db-dlg-sub {
	margin-left: 4px;
	font-size: 12px;
	font-weight: 400;
	opacity: 0.85;
}
</style>
