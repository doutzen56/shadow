<template>
	<!-- 单根容器：el-tooltip 渲染的是 fragment（trigger + teleport 弹层），
	     与下面的拖拽校验弹窗并列时本组件整体也是 fragment 根，父级传下来的属性无法自动继承，
	     Vue 开发环境会报 Extraneous non-props attributes 告警（code-inspector 注入的 data-insp-path 就落在属性里） -->
	<div>
		<el-tooltip :visible="state.capsLockVisible" effect="light" content="大写锁定已打开" placement="top">
			<el-form ref="ruleFormRef" :model="state.ruleForm" size="large" :rules="state.rules" class="login-content-form">
				<el-form-item class="login-animation1" prop="account">
					<el-input ref="accountRef" text placeholder="请输入用户名" v-model="state.ruleForm.account" clearable autocomplete="off" @keyup.enter.native="handleSignIn">
						<template #prefix>
							<el-icon>
								<ele-User />
							</el-icon>
						</template>
					</el-input>
				</el-form-item>
				<el-form-item class="login-animation2" prop="password">
					<el-input ref="passwordRef" :type="state.isShowPassword ? 'text' : 'password'" placeholder="请输入密码" v-model="state.ruleForm.password" autocomplete="off" @keyup.enter.native="handleSignIn">
						<template #prefix>
							<el-icon>
								<ele-Unlock />
							</el-icon>
						</template>
						<template #suffix>
							<i class="iconfont el-input__icon login-content-password" :class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'" @click="state.isShowPassword = !state.isShowPassword">
							</i>
						</template>
					</el-input>
				</el-form-item>
				<el-form-item class="login-animation3" prop="captcha" v-if="state.captchaEnabled">
					<el-col :span="15">
						<el-input ref="codeRef" text maxlength="4" placeholder="请输入验证码" v-model="state.ruleForm.code" clearable autocomplete="off" @keyup.enter.native="handleSignIn">
							<template #prefix>
								<el-icon>
									<ele-Position />
								</el-icon>
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
				<el-form-item class="login-animation4 login-content-remember">
					<div class="remember-row">
						<el-checkbox v-model="state.remember" @change="onRememberChange">记住账号</el-checkbox>
						<span class="remember-tip" title="登录密码经算法加密后传输">
							<el-icon><ele-Lock /></el-icon>加密传输
						</span>
					</div>
				</el-form-item>
				<el-form-item class="login-animation4">
					<el-button type="primary" icon="ele-Promotion" class="login-content-submit" round v-waves @click="handleSignIn" :loading="state.loading.signIn"> 登 录 </el-button>
				</el-form-item>
				<div class="font12 mt30 login-animation4 login-msg">* 温馨提示：建议使用谷歌、Microsoft Edge，版本 79.0.1072.62 及以上浏览器，360浏览器请使用极速模式</div>
				<!-- <el-button type="primary" round v-waves @click="weixinSignIn" :loading="state.loading.signIn"></el-button> -->
				<div v-if="themeConfig.signUp" class="font14 mt10 login-animation4" style="display: flex; justify-content: right; align-items: center">
					没有账号？<el-link type="primary" @click="register">点此注册</el-link>
				</div>
			</el-form>
		</el-tooltip>
		<div class="dialog-header">
			<el-dialog v-model="state.rotateVerifyVisible" :show-close="false">
				<DragVerifyImgRotate
					ref="dragRef"
					:imgsrc="state.rotateVerifyImg"
					v-model:isPassing="state.isPassRotate"
					:text="'请按住滑块拖动'"
					:successText="'验证通过'"
					handlerIcon="fa fa-angle-double-right"
					successIcon="fa fa-hand-peace-o"
					@passcallback="passRotateVerify"
				/>
			</el-dialog>
		</div>
	</div>
</template>

<script lang="ts" setup name="loginAccount">
import { reactive, computed, ref, onMounted, defineAsyncComponent, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, InputInstance } from 'element-plus';
import { initBackEndControlRoutes } from '/@/router/backEnd';
import { Local, Session } from '/@/utils/storage';
import { formatAxis } from '/@/utils/formatTime';
import { NextLoading } from '/@/utils/loading';
import { loadSysInfo } from '/@/utils/sysInfo';
import { sm2 } from 'sm-crypto-v2';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import { storeToRefs } from 'pinia';
import { staticRoutes } from '/@/router/route';
import { useI18n } from 'vue-i18n';
import { translateDynamic } from '/@/utils/other';

import { accessTokenKey, clearTokens, feature, getAPI } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system/api';

const { t } = useI18n();
// 旋转图片滑块组件
// import verifyImg from '/@/assets/logo-mini.svg';
const DragVerifyImgRotate = defineAsyncComponent(() => import('/@/components/dragVerify/dragVerifyImgRotate.vue'));

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

const route = useRoute();
const router = useRouter();

const ruleFormRef = ref();
const accountRef = ref<InputInstance>();
const passwordRef = ref<InputInstance>();
const codeRef = ref<InputInstance>();

const dragRef: any = ref(null);
const state = reactive({
	isShowPassword: false,
	ruleForm: {
		account: window.__env__.VITE_DEFAULT_USER,
		password: window.__env__.VITE_DEFAULT_USER_PASSWORD,
		code: '',
		codeId: 0 as any,
	},
	rules: {
		account: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
		password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
		code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
	},
	loading: {
		signIn: false,
	},
	captchaImage: '',
	rotateVerifyVisible: false,
	// rotateVerifyImg: verifyImg,
	rotateVerifyImg: themeConfig.value.logoUrl,
	secondVerEnabled: false,
	captchaEnabled: false,
	isPassRotate: false,
	capsLockVisible: false,
	expirySeconds: 60 as any, // 验证码过期时间
	remember: true, // 是否记住账号和密码（默认勾选）
});

// 「记住账号和密码」的缓存键
const REMEMBER_KEY = 'loginRemember';
const REMEMBER_ACCOUNT_KEY = 'loginAccount';
const REMEMBER_PASSWORD_KEY = 'loginPassword';

// 勾选状态变化时立刻落缓存，下次打开登录页保持上次的勾选
const onRememberChange = (val: any) => {
	Local.set(REMEMBER_KEY, !!val);
	if (!val) {
		Local.remove(REMEMBER_ACCOUNT_KEY);
		Local.remove(REMEMBER_PASSWORD_KEY);
	}
};

// 登录成功后按勾选状态写入 / 清除账号密码
const saveRemember = () => {
	if (!state.remember) {
		Local.remove(REMEMBER_ACCOUNT_KEY);
		Local.remove(REMEMBER_PASSWORD_KEY);
		return;
	}
	Local.set(REMEMBER_ACCOUNT_KEY, state.ruleForm.account);
	// 与 token 一样放在 localStorage（明文），内网系统可接受；如需更严格可改为只记账号
	Local.set(REMEMBER_PASSWORD_KEY, state.ruleForm.password);
};

// 回填上次记住的账号密码
const restoreRemember = () => {
	const remembered = Local.get(REMEMBER_KEY);
	state.remember = remembered === null || remembered === undefined ? true : !!remembered;
	if (!state.remember) return;
	const account = Local.get(REMEMBER_ACCOUNT_KEY);
	const password = Local.get(REMEMBER_PASSWORD_KEY);
	if (account) state.ruleForm.account = account;
	if (password) state.ruleForm.password = password;
};

// 验证码过期计时器
let timer: any = null;

// 页面初始化
onMounted(async () => {
	// 回填上次「记住」的账号密码
	restoreRemember();

	// 若URL带有Token参数（第三方登录）
	const accessToken = route.query.token;
	if (accessToken) await saveTokenAndInitRoutes(accessToken);

	// 若系统配置为空则获取系统配置
	if (themeConfig.value.captcha == undefined) {
		await loadSysInfo(Local.get('tid') ?? 0);
	}

	// 获取登录配置
	state.secondVerEnabled = themeConfig.value.secondVer ?? false;
	state.captchaEnabled = themeConfig.value.captcha ?? false;

	// 获取验证码
	getCaptcha();

	// 注册验证码过期计时器
	if (state.captchaEnabled) {
		timer = setInterval(() => {
			if (state.expirySeconds > 0) state.expirySeconds -= 1;
		}, 1000);
	}

	// 检测大小写按键/CapsLK
	document.addEventListener('keyup', handleKeyPress);
});

// 页面卸载
onUnmounted(() => {
	// 销毁验证码过期计时器
	clearInterval(timer);
	timer = null;

	document.removeEventListener('keyup', handleKeyPress);
});

const emits = defineEmits(['openRegistr']);
// 注册账号
const register = () => {
	emits('openRegistr');
};

// 检测大小写按键
const handleKeyPress = (e: KeyboardEvent) => {
	state.capsLockVisible = e.key === 'CapsLock';
};

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

// 获取时间
const currentTime = computed(() => {
	return formatAxis(new Date());
});

// 登录
const onSignIn = async () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return false;

		try {
			state.loading.signIn = true;

			// SM2加密密码
			// const keys = SM2.generateKeyPair();
			const publicKey = window.__env__.VITE_SM_PUBLIC_KEY;
			const password = sm2.doEncrypt(state.ruleForm.password, publicKey, 1);

			const tenantid = route.query.tid ?? 0;
			const [err, res] = await feature(getAPI(SysAuthApi).apiSysAuthLoginPost({ ...state.ruleForm, password: password, tenantId: Number(tenantid) }));
			if (err) {
				getCaptcha(); // 重新获取验证码
				return;
			}
			if (res.data.code !== 200) {
				// 登录失败的具体原因已由响应拦截器统一弹出，这里只需刷新验证码
				getCaptcha(); // 重新获取验证码
				return;
			}
			if (res.data.result?.accessToken == undefined) {
				getCaptcha(); // 重新获取验证码
				ElMessage.error('登录失败，请检查用户名或密码！');
				return;
			}
			// 登录成功后再记住账号密码，避免记住一串错误口令
			saveRemember();
			await saveTokenAndInitRoutes(res.data.result?.accessToken);
		} finally {
			state.loading.signIn = false;
		}
	});
};

// 保持Token并初始化路由
const saveTokenAndInitRoutes = async (accessToken: string | any) => {
	// 缓存token
	Local.set(accessTokenKey, accessToken);
	// Local.set(refreshAccessTokenKey, refreshAccessToken);
	Session.set('token', accessToken);

	// 添加完动态路由再进行router跳转，否则可能报错 No match found for location with path "/"
	const isNoPower = await initBackEndControlRoutes();
	signInSuccess(isNoPower); // 再执行 signInSuccess
};

// 登录成功后的跳转
const signInSuccess = (isNoPower: boolean | undefined) => {
	if (isNoPower) {
		ElMessage.warning('抱歉，您没有登录权限');
		clearTokens(); // 清空Token缓存
	} else {
		// 初始化登录成功时间问候语
		let currentTimeInfo = currentTime.value;
		// 登录成功，跳到转首页 如果是复制粘贴的路径，非首页/登录页，那么登录成功后重定向到对应的路径中
		if (route.query?.redirect) {
			const stores = useRoutesList();
			const { routesList } = storeToRefs(stores);
			const recursion = (routeList: any[], url: string): boolean | undefined => {
				if (routeList && routeList.length > 0) {
					for (let i = 0; i < routeList.length; i++) {
						if (routeList[i].path === url) return true;
						if (routeList[i]?.children?.length > 0) {
							let result = recursion(routeList[i]?.children, url);
							if (result) return true;
						}
					}
				}
			};
			let exist = recursion(routesList.value, route.query?.redirect as string);
			// 判断是否是静态路由
			if (!exist) exist = recursion(staticRoutes, route.query?.redirect as string);
			if (exist) {
				router.push({
					path: <string>route.query?.redirect,
					query: Object.keys(<string>route.query?.params).length > 0 ? JSON.parse(<string>route.query?.params) : '',
				});
			} else router.push('/');
		} else {
			router.push('/');
		}

		// 登录成功提示
		const signInText = '欢迎回来！';
		ElMessage.success(`${translateDynamic(currentTimeInfo)}，${signInText}`);
		// 添加 loading，防止第一次进入界面时出现短暂空白
		NextLoading.start();
	}
};

// 打开旋转验证
const openRotateVerify = () => {
	state.rotateVerifyVisible = true;
	state.isPassRotate = false;
	dragRef.value?.reset();
};

// 通过旋转验证
const passRotateVerify = () => {
	state.rotateVerifyVisible = false;
	state.isPassRotate = true;
	onSignIn();
};

// 登录处理
const handleSignIn = () => {
	if (!state.ruleForm.account) {
		accountRef.value?.focus();
	} else if (!state.ruleForm.password) {
		passwordRef.value?.focus();
	} else if (state.captchaEnabled && !state.ruleForm.code) {
		codeRef.value?.focus();
	} else {
		state.secondVerEnabled ? openRotateVerify() : onSignIn();
	}
};

// // 微信登录
// const weixinSignIn = () => {
// 	window.open('http://localhost:5005/api/sysoauth/signin?provider=Gitee&redirectUrl=http://localhost:8888');
// };

// 验证码过期显示文本
const captchaExpired = computed(() => `"${'验证码已过期'}"`);

// 导出对象
defineExpose({ saveTokenAndInitRoutes });
</script>

<style lang="scss" scoped>
.dialog-header {
	:deep(.el-dialog) {
		width: unset !important;

		.el-dialog__header {
			display: none;
		}

		.el-dialog__wrapper {
			position: absolute !important;
		}

		.v-modal {
			position: absolute !important;
		}
	}
}

.login-content-form {
	margin-top: 20px;

	@for $i from 0 through 4 {
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
		color: var(--el-text-color-placeholder);
		transition: color 0.2s ease;

		&:hover {
			color: var(--el-text-color-secondary);
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
			background-color: var(--el-bg-color);
			border: 1px solid var(--el-border-color);
			cursor: pointer;
			border-radius: var(--el-border-radius-base);
			user-select: none;
			transition: border-color 0.2s ease;

			&:hover {
				border-color: var(--el-border-color-hover);
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
			border-radius: var(--el-border-radius-base);
			background-color: var(--el-overlay-color-lighter);
			color: var(--el-color-white);
			text-align: center;
		}
	}

	/* 「记住账号和密码」：紧贴登录按钮上方 */
	.login-content-remember {
		margin-bottom: 0;

		:deep(.el-form-item__content) {
			line-height: 20px;
		}
	}

	/* 记住账号居左，国密提示居右 */
	.remember-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		width: 100%;
	}

	.remember-tip {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		font-size: 12px;
		color: var(--el-text-color-placeholder);
		user-select: none;
		cursor: default;
	}

	.login-content-submit {
		width: 100%;
		height: 44px;
		letter-spacing: 4px;
		font-size: 16px;
		font-weight: 600;
		margin-top: 12px;
	}

	.login-msg {
		color: var(--el-text-color-placeholder);
	}
}

/* 手机屏：控件本身已是 40px 高，22px 行距。
 * 「记住账号」那行紧贴登录按钮，下边距保持 0。 */
@media screen and (max-width: 580px) {
	.login-content-form {
		.el-form-item:not(.login-content-remember) {
			margin-bottom: 22px;
		}
	}
}
</style>
