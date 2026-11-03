<template>
	<div class="sys-dbColumn-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-CirclePlus /></el-icon>
					<span>增加列</span>
					<span class="db-dlg-sub">给当前表添加一个字段，保存后立即生效</span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="列名" prop="dbColumnName" :rules="[{ required: true, message: '名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.dbColumnName" placeholder="列名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="描述" prop="columnDescription" :rules="[{ required: true, message: '描述不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.columnDescription" placeholder="描述" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="类型">
							<el-select v-model="state.ruleForm.dataType" class="w100" placeholder="请选择类型">
								<el-option v-for="item in dataTypeList" :key="item.value" :label="`${item.value} · ${item.label}`" :value="item.value" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="可空">
							<el-select v-model="state.ruleForm.isNullable" class="w100">
								<el-option v-for="item in yesNoSelect" :key="item.value" :label="item.label" :value="item.value" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="主键">
							<el-select v-model="state.ruleForm.isPrimarykey" class="w100">
								<el-option v-for="item in yesNoSelect" :key="item.value" :label="item.label" :value="item.value" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="自增">
							<el-select v-model="state.ruleForm.isIdentity" class="w100" :disabled="state.ruleForm.isPrimarykey !== 1">
								<el-option v-for="item in yesNoSelect" :key="item.value" :label="item.label" :value="item.value" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="长度">
							<!-- 类型不需要长度时锁掉，避免填了却被后端忽略 -->
							<el-input-number v-model="state.ruleForm.length" class="w100" controls-position="right" :disabled="!typeMeta.hasLength" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="小数位">
							<el-input-number v-model="state.ruleForm.decimalDigits" class="w100" controls-position="right" :disabled="!typeMeta.hasDecimalDigits" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="默认值" prop="defaultValue">
							<el-input v-model="state.ruleForm.defaultValue" placeholder="默认值" clearable />
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

<script lang="ts" setup name="sysAddColumn">
import { computed, nextTick, reactive, ref } from 'vue';

import { dataTypeList, typeMetaOf, yesNoSelect } from '../database';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';
import { DbColumnInput } from '/@/api-services/system/models';

const emits = defineEmits(['handleQueryColumn']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	loading: false,
	ruleForm: {} as DbColumnInput,
});

// 当前类型是否需要长度 / 小数位
const typeMeta = computed(() => typeMetaOf(state.ruleForm.dataType));

// 打开弹窗
const openDialog = (addRow: DbColumnInput) => {
	// 先落地传入值，再补默认值：
	// 原实现先把 length 写到「上一个」ruleForm 上再整体覆盖，默认值等于没设
	state.ruleForm = { ...addRow };
	if (!state.ruleForm.dataType) state.ruleForm.dataType = 'varchar';
	if (!state.ruleForm.length) state.ruleForm.length = 64;
	state.isShowDialog = true;
	nextTick(() => ruleFormRef.value?.resetFields());
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQueryColumn');
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
		await getAPI(SysDatabaseApi).apiSysDatabaseAddColumnPost(state.ruleForm);
		closeDialog();
	} finally {
		state.loading = false;
	}
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
/* 副标题跟在标题后面，弱化处理不抢主标题 */
.db-dlg-sub {
	margin-left: 4px;
	font-size: 12px;
	font-weight: 400;
	opacity: 0.85;
}
</style>
