<template>
	<div class="changePassword-container">
		<el-dialog v-model="state.isShowDialog" append-to-body draggable :close-on-click-modal="false" :close-on-press-escape="false" width="700px" :show-close="false">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-View /> </el-icon>
					<span> {{ '修改密码' }} </span>
				</div>
			</template>
			<div style="color: red; padding: 10px 10px; background: #faecd8; margin-bottom: 30px">
				<el-icon style="transform: translateY(2px)"><ele-Bell /></el-icon>
				<span> {{ '密码安全策略：必须包含大小写字母、数字和特殊字符的组合，长度在6-16之间。建议定期更新密码。' }} </span>
			</div>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="'当前密码'" prop="passwordOld" :rules="[{ required: true, message: '当前密码不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.passwordOld" type="password" autocomplete="off" show-password />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="'新密码'" prop="passwordNew" :rules="[{ required: true, message: '新密码不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.passwordNew" type="password" autocomplete="off" show-password />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="'确认密码'" prop="passwordNew2" :rules="[{ validator: validatePassword, required: true, trigger: 'blur' }]">
							<el-input v-model="state.passwordNew2" type="password" autocomplete="off" show-password />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="logout">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { sm2 } from 'sm-crypto-v2';
import { clearAccessTokens, getAPI } from '/@/utils/axios-utils';
import { SysConfigApi, SysUserApi } from '/@/api-services/system/api';
import { ChangePwdInput } from '/@/api-services/system/models';

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as ChangePwdInput,
	passwordNew2: '',
});

// 打开弹窗
const openDialog = () => {
	// 移除 app 元素，即清空主界面
	const appEl = document.getElementById('app')!;
	appEl?.remove();

	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
};

// // 取消
// const cancel = () => {
// 	state.isShowDialog = false;
// };

// 提交
const submit = () => {
	ruleFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;

		// SM2加密密码
		var cpwd: ChangePwdInput = { passwordOld: '', passwordNew: '' };
		var publicKey = window.__env__.VITE_SM_PUBLIC_KEY;
		if (publicKey == '') {
			var res = await getAPI(SysConfigApi).apiSysConfigSmPublicKeyGet();
			publicKey = window.__env__.VITE_SM_PUBLIC_KEY = res.data.result ?? '';
		}
		cpwd.passwordOld = sm2.doEncrypt(state.ruleForm.passwordOld, publicKey, 1);
		cpwd.passwordNew = sm2.doEncrypt(state.ruleForm.passwordNew, publicKey, 1);
		await getAPI(SysUserApi).apiSysUserChangePwdPost(cpwd);

		ElMessage.success('密码已修改，请重新登录系统！');
		state.isShowDialog = false;
		clearAccessTokens();
	});
};

// 密码验证
const validatePassword = (_rule: any, value: any, callback: any) => {
	if (state.passwordNew2 != state.ruleForm.passwordNew) {
		callback(new Error('两次密码不一致！'));
	} else {
		callback();
	}
};

// 退出
const logout = () => {
	clearAccessTokens();
};

// 导出对象
defineExpose({ openDialog });
</script>
