<template>
	<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" size="large" class="login-content-form">
		<el-form-item prop="tenantId" class="mb15 login-animation1">
			<TenantSelect v-model="state.ruleForm.tenantId" ref="tenantSelectRef" clearable />
		</el-form-item>
		<el-form-item prop="realName" :rules="[{ required: true, message: '请输入真实姓名', trigger: 'blur' }]" class="mb15 login-animation2">
			<el-input v-model="state.ruleForm.realName" placeholder="请输入真实姓名" clearable prefix-icon="ele-User" />
		</el-form-item>
		<el-form-item prop="phone" :rules="[{ required: true, message: '请输入手机号码', trigger: 'blur' }]" class="mb15 login-animation3">
			<el-input v-model="state.ruleForm.phone" placeholder="请输入手机号码" clearable prefix-icon="ele-Iphone" />
		</el-form-item>
		<!-- <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb15 login-animation2">
				<el-form-item prop="account" :rules="[{ required: true, message: '请输入登录账号', trigger: 'blur' }]">
					<el-input v-model="state.ruleForm.account" placeholder="请输入登录账号" clearable />
				</el-form-item>
			</el-col> -->
		<el-form-item prop="password" :rules="[{ required: true, message: '请输入登录密码', trigger: 'blur' }]" class="mb15 login-animation4">
			<el-input v-model="state.ruleForm.password" :type="state.isShowPassword ? 'text' : 'password'" placeholder="请输入登录密码" prefix-icon="ele-Unlock">
				<template #suffix>
					<i class="iconfont el-input__icon login-content-password" :class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'" @click="state.isShowPassword = !state.isShowPassword" />
				</template>
			</el-input>
		</el-form-item>
		<el-form-item class="mb15 login-animation5">
			<el-col :span="15">
				<el-input text maxlength="6" placeholder="请输入验证码" v-model="state.ruleForm.code" clearable autocomplete="off">
					<template #prefix>
						<el-icon class="el-input__icon"><ele-Position /></el-icon>
					</template>
				</el-input>
			</el-col>
			<el-col :span="1"></el-col>
			<el-col :span="8">
				<div :class="[state.expirySeconds > 0 ? 'login-content-code' : 'login-content-code-expired']" @click="getCaptcha">
					<div style="width: 130px; height: 38px; text-align: center; cursor: pointer" v-if="state.captchaImage == ''">
						<el-icon class="is-loading">
							<ele-Loading />
						</el-icon>
					</div>
					<img class="login-content-code-img" width="130px" height="38px" :src="state.captchaImage" style="cursor: pointer" v-else />
				</div>
			</el-col>
		</el-form-item>

		<el-form-item class="login-animation4">
			<el-button type="primary" icon="ele-Edit" round v-waves class="login-content-submit" @click="onSignUp"> 注 册 </el-button>
		</el-form-item>
	</el-form>
</template>

<!-- 用户注册 -->
<script setup lang="ts" name="loginMobile">
import { computed, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
// import { verifyPhone } from '/@/utils/toolsValidate';
import { useI18n } from 'vue-i18n';

import TenantSelect from '/@/views/system/tenant/component/tenantSelect.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system/api';
import { UserRegistrationInput } from '/@/api-services/system/models';

const { t } = useI18n();
const ruleFormRef = ref();
const emits = defineEmits(['goLogin']);

const state = reactive({
	ruleForm: {} as UserRegistrationInput,
	isShowPassword: false,
	captchaEnabled: false,
	expirySeconds: 60 as any, // 验证码过期时间
	captchaImage: '',
});

const props = defineProps({
	isRegister: Boolean,
});
// 验证码过期计时器
var timer: any = null;

// 页面初始化
watch(
	() => props.isRegister,
	(val) => {
		state.captchaEnabled = true;
		if (val)
			setTimeout(() => {
				// 调用验证码
				getCaptcha();
				// 注册验证码过期计时器
				if (state.captchaEnabled) {
					timer = setInterval(() => {
						if (state.expirySeconds > 0) state.expirySeconds -= 1;
					}, 1000);
				}
			}, 500);
	},
	{ immediate: true }
);

// 获取验证码
const getCaptcha = async () => {
	if (!state.captchaEnabled) return;

	state.ruleForm.code = '';
	const res = await getAPI(SysAuthApi)
		.apiSysAuthCaptchaGet()
		.then((res) => res.data.result);
	state.captchaImage = 'data:text/html;base64,' + res?.img;
	state.expirySeconds = res?.expirySeconds;
	state.ruleForm.codeId = res?.id;
};

// 注册
const onSignUp = async () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		await getAPI(SysAuthApi)
			.apiSysAuthUserRegistrationPost(state.ruleForm)
			.then((res) => {
				if (res.data?.code == 200) {
					emits('goLogin');
					// state.ruleForm = {} as UserRegistrationInput;
					ElMessage.success('注册成功');
				} else getCaptcha();
			});
	});
};

// 验证码过期显示文本
const captchaExpired = computed(() => `"${'验证码已过期'}"`);
</script>

<style lang="scss" scoped>
.login-content-form {
	margin-top: 20px;

	@for $i from 1 through 6 {
		.login-animation#{$i} {
			opacity: 0;
			animation-name: error-num;
			animation-duration: 0.5s;
			animation-fill-mode: forwards;
			animation-delay: calc($i/10) + s;
		}
	}

	.login-content-password {
		display: inline-block;
		width: 20px;
		cursor: pointer;

		&:hover {
			color: var(--el-color-info);
		}
	}
	.login-content-code {
		display: flex;
		align-items: center;
		justify-content: space-around;
		position: relative;

		.login-content-code-img {
			width: 100%;
			height: 40px;
			line-height: 40px;
			background-color: #ffffff;
			border: 1px solid rgb(220, 223, 230);
			cursor: pointer;
			transition: all ease 0.2s;
			border-radius: 4px;
			user-select: none;

			&:hover {
				border-color: #c0c4cc;
				transition: all ease 0.2s;
			}
		}
	}

	.login-content-code-expired {
		@extend .login-content-code;

		&::before {
			content: v-bind(captchaExpired);
			position: absolute;
			top: 0;
			left: 0;
			right: 0;
			bottom: 0;
			border-radius: 4px;
			background-color: rgba(0, 0, 0, 0.5);
			color: #ffffff;
			text-align: center;
		}
	}
	.login-content-submit {
		width: 100%;
		letter-spacing: 2px;
		font-weight: 500;
		margin-top: 15px;
	}
}
</style>
