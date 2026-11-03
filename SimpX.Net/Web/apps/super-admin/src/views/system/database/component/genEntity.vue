<template>
	<div class="sys-dbEntity-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Cpu /></el-icon>
					<span>生成实体</span>
					<span class="db-dlg-sub">按表结构生成 C# 实体类，写到指定程序集</span>
				</div>
			</template>
			<div class="db-gen-notice">
				<el-icon class="db-gen-notice-ico"><ele-WarningFilled /></el-icon>
				<span>实体会写进后端项目目录，<b>生成后必须重启后台服务</b>才会被加载。</span>
			</div>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="表名" prop="tableName" :rules="[{ required: true, message: '表名不能为空', trigger: 'blur' }]">
							<el-input disabled v-model="state.ruleForm.tableName" placeholder="表名" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="实体名称" prop="entityName" :rules="[{ required: true, message: '实体名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.entityName" placeholder="实体名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="基类" prop="baseClassName">
							<g-sys-dict v-model="state.ruleForm.baseClassName" code="code_gen_base_class" render-as="select" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="存放位置" prop="position" :rules="[{ required: true, message: '请选择存放位置', trigger: 'change' }]">
							<el-select v-model="state.ruleForm.position" filterable clearable class="w100" placeholder="存放位置">
								<el-option v-for="(item, index) in props.applicationNamespaces" :key="index" :label="item" :value="item" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel" :disabled="state.loading">取 消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" v-reclick="3000" @click="submit" :loading="state.loading">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysGenEntity">
import { reactive, ref } from 'vue';
import { ElNotification } from 'element-plus';
import { camelCase, upperFirst } from 'lodash-es';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';

const ruleFormRef = ref();
const state = reactive({
	loading: false,
	isShowDialog: false,
	ruleForm: {} as any,
});

const props = defineProps({
	applicationNamespaces: { type: Array },
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm.configId = row.configId;
	state.ruleForm.tableName = row.tableName;
	// 表名转 PascalCase 作为默认实体名，用户可改
	state.ruleForm.entityName = upperFirst(camelCase(row.tableName));
	state.ruleForm.baseClassName = 'EntityBase';
	state.ruleForm.position = row.position;
	state.ruleForm.authorName = 'SimpX';
	state.isShowDialog = true;
};

// 关闭弹窗
const closeDialog = () => {
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
		await getAPI(SysDatabaseApi).apiSysDatabaseCreateEntityPost(state.ruleForm);
		closeDialog();
		ElNotification({
			title: '提示',
			message: '生成成功，请重启服务加载最新代码',
			type: 'success',
			position: 'bottom-right',
		});
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

.db-gen-notice {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 14px;
	padding: 9px 14px;
	border-radius: 12px;
	font-size: 12.5px;
	line-height: 1.6;
	color: var(--el-text-color-regular);
	border: 1px solid color-mix(in srgb, var(--el-color-warning) 35%, transparent);
	background: color-mix(in srgb, var(--el-color-warning) 10%, var(--el-bg-color));
}
.db-gen-notice-ico {
	flex: none;
	font-size: 15px;
	color: var(--el-color-warning);
}
</style>
