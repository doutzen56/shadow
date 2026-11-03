<template>
	<div class="sys-pos-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="方案名称" prop="name" :rules="[{ required: true, message: '方案名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="默认机构" prop="orgId" :rules="[{ required: true, message: '机构不能为空', trigger: 'blur' }]">
							<el-cascader :options="state.orgData" :props="cascaderProps" v-model="state.ruleForm.orgId" clearable filterable class="w100">
								<template #default="{ node, data }">
									<span>{{ data.name }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="默认角色" prop="roleId" :rules="[{ required: true, message: '角色不能为空', trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.roleId" clearable class="w100">
								<el-option :label="item.name" :value="item.id" v-for="(item, index) in state.roleData" :key="index" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="默认职位" prop="posId" :rules="[{ required: true, message: '职位不能为空', trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.posId" clearable class="w100">
								<el-option :label="item.name" :value="item.id" v-for="(item, index) in state.posData" :key="index" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="排序">
							<el-input-number v-model="state.ruleForm.orderNo" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="备注">
							<el-input v-model="state.ruleForm.remark" clearable type="textarea" />
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

<script lang="ts" setup name="sysEditPos">
import { onMounted, reactive, ref } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysOrgApi, SysPosApi, SysRoleApi, SysUserRegWayApi } from '/@/api-services/system/api';
import { PosOutput, RoleOutput, SysOrg, UpdateUserRegWayInput } from '/@/api-services/system/models';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	loading: false,
	isShowDialog: false,
	ruleForm: {} as UpdateUserRegWayInput,
	orgData: [] as Array<SysOrg>, // 机构数据
	posData: [] as Array<PosOutput>, // 职位数据
	roleData: [] as Array<RoleOutput>, // 角色数据
});

// 级联选择器配置选项
const cascaderProps = { checkStrictly: true, emitPath: false, value: 'id', label: 'name' };

onMounted(async () => {
	state.loading = true;
	state.posData = await getAPI(SysPosApi)
		.apiSysPosListGet()
		.then((res) => res.data.result ?? []);
	state.roleData = await getAPI(SysRoleApi)
		.apiSysRoleListGet()
		.then((res) => res.data.result ?? []);
	state.orgData = await getAPI(SysOrgApi)
		.apiSysOrgListGet(0)
		.then((res) => res.data.result ?? []);
	state.loading = false;
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
		if (!valid) return;
		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(SysUserRegWayApi).apiSysUserRegWayUpdatePost(state.ruleForm);
		} else {
			await getAPI(SysUserRegWayApi).apiSysUserRegWayAddPost(state.ruleForm);
		}
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
