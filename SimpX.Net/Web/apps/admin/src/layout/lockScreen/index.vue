<template>
	<div v-show="state.visible">
		<div class="layout-lock-screen-mask"></div>
		<div class="layout-lock-screen-img" :class="{ 'layout-lock-screen-filter': state.showLogin }"></div>
		<div class="layout-lock-screen">
			<!-- 时钟面板：上滑或回车收起，位移/透明度全部由 datePanelStyle 驱动，不再直接改 DOM 样式、也不再 removeChild -->
			<div
				v-show="!state.hideDatePanel"
				class="layout-lock-screen-date"
				ref="layoutLockScreenDateRef"
				:style="datePanelStyle"
				@mousedown="onDownPc"
				@mousemove="onMovePc"
				@mouseup="onEnd"
				@mouseleave="onEnd"
				@touchstart.stop="onDownApp"
				@touchmove.stop="onMoveApp"
				@touchend.stop="onEnd"
			>
				<div class="layout-lock-screen-date-box">
					<div class="layout-lock-screen-date-box-time">
						{{ state.time.hm }}<span class="layout-lock-screen-date-box-minutes">{{ state.time.s }}</span>
					</div>
					<div class="layout-lock-screen-date-box-info">{{ state.time.mdq }}</div>
				</div>
				<div class="layout-lock-screen-date-top">
					<SvgIcon name="ele-Top" />
					<div class="layout-lock-screen-date-top-text">{{ '上滑解锁' }}</div>
				</div>
			</div>
			<transition name="el-zoom-in-center">
				<div v-show="state.showLogin" class="layout-lock-screen-login">
					<div class="layout-lock-screen-login-box">
						<div class="layout-lock-screen-login-box-img">
							<img :src="userInfos.avatar || 'https://img2.baidu.com/it/u=1978192862,2048448374&fm=253&fmt=auto&app=138&f=JPEG?w=504&h=500'" />
						</div>
						<div class="layout-lock-screen-login-box-name">{{ userInfos.realName || userInfos.account || '已锁定' }}</div>
						<!-- 错误提示放在输入框上方常驻显示：原来的 v-if/v-else 会把输入框换成「确认」按钮，
						     焦点丢失后再按回车什么也不会发生，看起来就像「输了密码跳不回去」 -->
						<div v-if="state.message" class="layout-lock-screen-login-box-message">{{ state.message }}</div>
						<div class="layout-lock-screen-login-box-value">
							<el-input
								placeholder="请输入密码"
								:type="state.isShowPassword ? 'text' : 'password'"
								ref="layoutLockScreenInputRef"
								size="default"
								v-model="state.lockScreenPassword"
								:disabled="state.submitting"
							>
								<template #append>
									<el-button :loading="state.submitting" @click="onLockScreenSubmit">
										<el-icon class="el-input__icon">
											<ele-Right />
										</el-icon>
									</el-button>
								</template>
								<template #suffix>
									<i
										class="iconfont el-input__icon login-content-password"
										:class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'"
										@click="state.isShowPassword = !state.isShowPassword"
									>
									</i>
								</template>
							</el-input>
						</div>
						<!-- 登录态失效时后端无法校验密码，给出明确出口，避免只剩一个「密码怎么都不对」的死胡同 -->
						<div v-if="state.showRelogin" class="layout-lock-screen-login-box-relogin">
							<el-link type="primary" :underline="false" @click="goRelogin">登录状态已失效，点此重新登录</el-link>
						</div>
					</div>
					<div class="layout-lock-screen-login-icon">
						<SvgIcon name="ele-Microphone" :size="20" />
						<SvgIcon name="ele-AlarmClock" :size="20" />
						<SvgIcon name="ele-SwitchButton" :size="20" />
					</div>
				</div>
			</transition>
		</div>
	</div>
</template>

<script setup lang="ts" name="layoutLockScreen">
import { computed, nextTick, onMounted, onUnmounted, reactive, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useRoute } from 'vue-router';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useUserInfo } from '/@/stores/userInfo';
import { sm2 } from 'sm-crypto-v2';
import { loadSysInfo } from '/@/utils/sysInfo';
import { formatDate } from '/@/utils/formatTime';
import { Local, Session } from '/@/utils/storage';

import { accessTokenKey, clearTokens, feature, getAPI } from '/@/utils/axios-utils';
import { SysAuthApi, SysConfigApi } from '/@/api-services/system/api';

// 定义变量内容
const layoutLockScreenDateRef = ref<HTMLElement>();
const layoutLockScreenInputRef = ref();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const storesUserInfo = useUserInfo();
const { userInfos } = storeToRefs(storesUserInfo);
const route = useRoute();

const state = reactive({
	/** 是否显示锁屏遮罩 */
	visible: false,
	/** 是否显示解锁（密码）面板 */
	showLogin: false,
	/** 时钟面板是否已滑走隐藏 */
	hideDatePanel: false,
	/** 时钟面板正在被拖动 */
	dragging: false,
	/** 松手后回弹（带过渡动画） */
	springBack: false,
	/** 时钟面板滑走动画中 */
	leaving: false,
	/** 本次拖动的纵向位移（px，向上为负） */
	dragOffsetY: 0,
	/** 错误提示 */
	message: '',
	/** 是否展示重新登录入口 */
	showRelogin: false,
	/** 解锁请求进行中 */
	submitting: false,
	time: {
		hm: '',
		s: '',
		mdq: '',
	},
	lockScreenPassword: '',
	isShowPassword: false,
});

/** 时钟面板高度（拖动开始时测量，用于换算滑动比例与解锁阈值） */
const panelHeight = ref(0);
/** 本次拖动的起始纵坐标 */
let downClientY = 0;
/** 时钟刷新定时器 */
let clockTimer: number | undefined;
/** 锁屏倒计时定时器 */
let lockTimer: number | undefined;
/** 面板动画定时器 */
let animTimer: number | undefined;

// 解锁阈值：面板高度的 1/3，并夹在 120~320px 之间。
// 原先写死 400px，窗口较矮时几乎拖不到，等于只能靠回车解锁
const revealDistance = computed(() => Math.min(320, Math.max(120, (panelHeight.value || window.innerHeight) / 3)));

// 时钟面板样式：拖动时无过渡（跟手），松手回弹与滑走时用 0.3s 过渡
const datePanelStyle = computed(() => {
	const animation = 'transform 0.3s ease, opacity 0.3s ease';
	if (state.leaving) return { transform: 'translateY(-100%)', opacity: '0', transition: animation };
	if (state.springBack) return { transform: 'translateY(0)', opacity: '1', transition: animation };
	if (!state.dragging) return {};
	const ratio = Math.min(1, Math.abs(state.dragOffsetY) / revealDistance.value);
	return { transform: `translateY(${state.dragOffsetY}px)`, opacity: `${1 - ratio * 0.9}` };
});

// 时钟（每秒刷新）
const initTime = () => {
	state.time.hm = formatDate(new Date(), 'HH:MM');
	state.time.s = formatDate(new Date(), 'SS');
	state.time.mdq = formatDate(new Date(), 'mm-dd，WWW');
};

/** 停止时钟刷新：解锁面板出现后已经用不上了，省掉每秒的无意义更新 */
const stopClock = () => {
	if (clockTimer !== undefined) {
		window.clearInterval(clockTimer);
		clockTimer = undefined;
	}
};

/** 锁定状态是否已经生效（与 App.vue 的 lockScreenTime > 1 判定保持一致） */
const isLocked = () => themeConfig.value.lockScreenTime <= 1;

// 锁屏倒计时
const initLockScreen = () => {
	if (!themeConfig.value.isLockScreen) return;

	// 锁屏时间为null（配置异常）先补默认值：App.vue 用 lockScreenTime > 1 判断主界面是否可见，
	// null 会让主界面被隐藏而这里又永远等不到倒计时走完，结果就是一片白屏
	if (themeConfig.value.lockScreenTime == null) {
		themeConfig.value.lockScreenTime = 300;
		setLocalThemeConfig();
	}

	// 刷新回来时倒计时已经走完，直接出遮罩，避免空等一秒出现「界面已隐藏、锁屏还没显示」的白屏
	if (isLocked()) showLockScreen();

	lockTimer = window.setInterval(() => {
		// 锁屏时间为null重置为300秒，防止白屏
		if (themeConfig.value.lockScreenTime == null) {
			themeConfig.value.lockScreenTime = 300;
			setLocalThemeConfig();
			return;
		}
		if (themeConfig.value.lockScreenTime <= 1) {
			// 已锁屏就不再重复写本地缓存（原来每秒都会序列化整个 themeConfig 写一次 localStorage）
			if (state.visible) return;
			showLockScreen();
			setLocalThemeConfig();
			return;
		}
		themeConfig.value.lockScreenTime--;
	}, 1000);
};

/** 展示遮罩并把解锁入口准备好（回车即可展开，不必先滑走时钟面板） */
const showLockScreen = () => {
	state.visible = true;
};

// 存储布局配置
const setLocalThemeConfig = () => {
	themeConfig.value.isDrawer = false;
	Local.set('themeConfig', themeConfig.value);
};

/** 本地是否还留有访问令牌（没有令牌时后端无法校验密码，也就不能假装解锁成功） */
const hasAccessToken = () => {
	const token = Local.get(accessTokenKey) ?? Session.get('token');
	return !!token && token !== 'invalid_token';
};

/** 去掉后端消息里的错误码前缀，形如「[D0001] 密码错误」 */
const stripErrorCode = (message?: string) => (message ?? '').replace(/^\[[A-Za-z0-9_]+\]\s*/, '').trim();

/** 聚焦密码框：面板隐藏或组件已卸载时 ref 为空，用可选链兜底，别让 focus 抛错打断流程 */
const focusInput = () => {
	nextTick(() => layoutLockScreenInputRef.value?.focus?.());
};

// 鼠标按下 pc
const onDownPc = (down: MouseEvent) => startDrag(down.clientY);
// 鼠标按下 app
const onDownApp = (down: TouchEvent) => {
	if (down.touches[0]) startDrag(down.touches[0].clientY);
};
// 鼠标移动 pc
const onMovePc = (move: MouseEvent) => moveDrag(move.clientY);
// 鼠标移动 app
const onMoveApp = (move: TouchEvent) => {
	if (move.touches[0]) moveDrag(move.touches[0].clientY);
};

/**
 * 开始拖动
 * @param clientY 起点纵坐标
 */
const startDrag = (clientY: number) => {
	if (state.showLogin || state.leaving) return;
	panelHeight.value = layoutLockScreenDateRef.value?.clientHeight ?? window.innerHeight;
	state.dragging = true;
	state.springBack = false;
	downClientY = clientY;
	state.dragOffsetY = 0;
	// 在面板外松手也要结束拖动，否则 dragging 一直是 true，鼠标移动会继续带着面板跑
	window.addEventListener('mouseup', onEnd);
};

/**
 * 拖动过程：只允许向上，且不超过面板高度
 * @param clientY 当前纵坐标
 */
const moveDrag = (clientY: number) => {
	if (!state.dragging) return;
	const maxDistance = panelHeight.value || window.innerHeight;
	state.dragOffsetY = Math.max(-maxDistance, Math.min(0, clientY - downClientY));
};

// 鼠标松开
const onEnd = () => {
	if (!state.dragging) return;
	state.dragging = false;
	window.removeEventListener('mouseup', onEnd);

	if (Math.abs(state.dragOffsetY) >= revealDistance.value) {
		revealLoginPanel();
	} else {
		state.springBack = true;
		state.dragOffsetY = 0;
		clearAnimTimer();
		animTimer = window.setTimeout(() => {
			state.springBack = false;
			animTimer = undefined;
		}, 300);
	}
};

/** 收起时钟面板、展开解锁面板（回车与上滑共用） */
const revealLoginPanel = () => {
	if (state.showLogin) return;
	clearAnimTimer();
	state.dragging = false;
	window.removeEventListener('mouseup', onEnd);
	state.leaving = true;
	stopClock();
	state.showLogin = true;
	focusInput();
	// 滑走动画结束后再隐藏时钟面板：视觉效果与原来的 removeChild 一致，但节点始终由 Vue 管理
	animTimer = window.setTimeout(() => {
		state.hideDatePanel = true;
		state.leaving = false;
		state.dragOffsetY = 0;
		animTimer = undefined;
	}, 300);
};

const clearAnimTimer = () => {
	if (animTimer !== undefined) {
		window.clearTimeout(animTimer);
		animTimer = undefined;
	}
};

// 解锁成功后关闭锁屏：App.vue 里 <LockScreen v-if="themeConfig.isLockScreen" />，置 false 即卸载本组件，主界面随之显示
const unlock = () => {
	themeConfig.value.isLockScreen = false;
	themeConfig.value.lockScreenTime = 300;
	setLocalThemeConfig();
};

// 密码输入点击事件
const onLockScreenSubmit = async () => {
	if (state.submitting) return;
	const password = state.lockScreenPassword;
	if (!password) {
		state.message = '请输入密码';
		focusInput();
		return;
	}

	// 没有令牌（长时间挂机等）时后端校验不了密码，给出重新登录入口，而不是像以前那样直接放行
	if (!hasAccessToken()) {
		state.message = '登录状态已失效，请重新登录';
		state.showRelogin = true;
		state.lockScreenPassword = '';
		return;
	}

	state.submitting = true;
	state.message = '';
	state.showRelogin = false;
	try {
		// SM2加密密码
		let publicKey = window.__env__?.VITE_SM_PUBLIC_KEY ?? '';
		if (!publicKey) {
			const [keyErr, keyRes] = await feature(getAPI(SysConfigApi).apiSysConfigSmPublicKeyGet());
			if (keyErr) {
				state.message = '获取加密公钥失败，请稍后重试';
				return;
			}
			publicKey = window.__env__.VITE_SM_PUBLIC_KEY = keyRes.data.result ?? '';
		}
		if (!publicKey) {
			state.message = '服务端未配置 SM2 公钥，无法校验密码';
			return;
		}

		const [err, res] = await feature(getAPI(SysAuthApi).apiSysAuthUnLockScreenPost(sm2.doEncrypt(password, publicKey, 1)));
		if (err) {
			state.message = stripErrorCode(err.message) || '解锁失败，请稍后重试';
		} else if (res?.data?.result === true) {
			unlock();
			return;
		} else {
			// 业务失败（密码错误等）响应拦截器只弹全局 ElMessage，而它被压在锁屏遮罩下面看不见，
			// 必须在这里把结果判出来并显示到面板上，否则用户看到的就是「输了密码毫无反应」
			state.message = stripErrorCode(res?.data?.message) || '密码错误，请重试';
		}
		state.lockScreenPassword = '';
		focusInput();
	} catch (ex: any) {
		state.message = `出错了:${ex?.message ?? ex}`;
		state.lockScreenPassword = '';
		focusInput();
	} finally {
		state.submitting = false;
	}
};

// 重新登录：清掉锁屏配置和失效令牌后刷新，路由守卫会把用户带到登录页
const goRelogin = () => {
	unlock();
	clearTokens();
	window.location.reload();
};

// 全局回车：只显示时钟面板时按回车直接展开解锁面板，面板展开后按回车提交解锁
// 用 addEventListener 而不是 document.onkeydown，避免覆盖别人的监听，也才能在卸载时解绑
// 解锁提交只挂在这里而不是输入框的 keyup：登录后的「新手指引」(el-tour) 带 focus-trap，
// 锁屏时如果引导还开着会把焦点抢回去，绑在输入框上的回车就彻底失灵
const onGlobalKeydown = (e: KeyboardEvent) => {
	if (e.key !== 'Enter' || !state.visible || e.isComposing) return;
	if (e.repeat) return;
	if (!state.showLogin) {
		revealLoginPanel();
		return;
	}
	e.preventDefault();
	onLockScreenSubmit();
};

// 页面加载时
onMounted(async () => {
	initTime();
	clockTimer = window.setInterval(initTime, 1000);
	initLockScreen();
	document.addEventListener('keydown', onGlobalKeydown);

	// 获取租户Id标识
	var tenantid = Number(route.query.tid);
	if (isNaN(tenantid)) {
		tenantid = 0;
	} else if (tenantid > 99999) {
		Local.set('tid', tenantid);
	}
	// 系统信息只为拿 SM2 公钥，失败不该挡住解锁：解锁提交里还会按需重新获取
	try {
		await loadSysInfo(tenantid);
	} catch (ex: any) {
		console.warn('[lockScreen] 系统信息加载失败，解锁时会重试：', ex);
	}
});

// 页面卸载时
onUnmounted(() => {
	stopClock();
	if (lockTimer !== undefined) {
		window.clearInterval(lockTimer);
		lockTimer = undefined;
	}
	clearAnimTimer();
	window.removeEventListener('mouseup', onEnd);
	document.removeEventListener('keydown', onGlobalKeydown);
});
</script>

<style scoped lang="scss">
.layout-lock-screen-fixed {
	position: fixed;
	top: 0;
	left: 0;
	width: 100%;
	height: 100%;
}
.layout-lock-screen-filter {
	filter: blur(1px);
}
.layout-lock-screen-mask {
	background: var(--el-bg-color);
	@extend .layout-lock-screen-fixed;
	z-index: 9999990;
}
.layout-lock-screen-img {
	@extend .layout-lock-screen-fixed;
	background-image: url('/@/assets/lockscreen.img');
	background-size: 100% 100%;
	z-index: 9999991;
}
.layout-lock-screen {
	@extend .layout-lock-screen-fixed;
	z-index: 9999992;
	&-date {
		position: absolute;
		left: 0;
		top: 0;
		width: 100%;
		height: 100%;
		color: var(--el-color-white);
		z-index: 9999993;
		user-select: none;
		&-box {
			position: absolute;
			left: 30px;
			bottom: 50px;
			&-time {
				font-size: 100px;
				color: var(--el-color-white);
			}
			&-info {
				font-size: 40px;
				color: var(--el-color-white);
			}
			&-minutes {
				font-size: 16px;
			}
		}
		&-top {
			width: 40px;
			height: 40px;
			line-height: 40px;
			border-radius: 100%;
			border: 1px solid var(--el-border-color-light, #ebeef5);
			background: rgba(255, 255, 255, 0.1);
			color: var(--el-color-white);
			opacity: 0.8;
			position: absolute;
			right: 30px;
			bottom: 50px;
			text-align: center;
			overflow: hidden;
			transition: all 0.3s ease;
			i {
				transition: all 0.3s ease;
			}
			&-text {
				opacity: 0;
				position: absolute;
				top: 150%;
				font-size: 12px;
				color: var(--el-color-white);
				left: 50%;
				line-height: 1.2;
				transform: translate(-50%, -50%);
				transition: all 0.3s ease;
				width: 35px;
			}
			&:hover {
				border: 1px solid rgba(255, 255, 255, 0.5);
				background: rgba(255, 255, 255, 0.2);
				box-shadow: 0 0 12px 0 rgba(255, 255, 255, 0.5);
				color: var(--el-color-white);
				opacity: 1;
				transition: all 0.3s ease;
				i {
					transform: translateY(-40px);
					transition: all 0.3s ease;
				}
				.layout-lock-screen-date-top-text {
					opacity: 1;
					top: 50%;
					transition: all 0.3s ease;
				}
			}
		}
	}
	&-login {
		position: relative;
		z-index: 9999994;
		width: 100%;
		height: 100%;
		left: 0;
		top: 0;
		display: flex;
		flex-direction: column;
		justify-content: center;
		color: var(--el-color-white);
		&-box {
			text-align: center;
			margin: auto;
			&-img {
				width: 180px;
				height: 180px;
				margin: auto;
				img {
					width: 100%;
					height: 100%;
					border-radius: 100%;
				}
			}
			&-name {
				font-size: 26px;
				margin: 15px 0 30px;
			}
			&-message {
				font-size: 14px;
				color: var(--el-color-danger);
				margin-bottom: 10px;
			}
			&-value {
				min-height: 73px;
			}
			&-relogin {
				margin-top: 4px;
				font-size: 14px;
			}
		}
		&-icon {
			position: absolute;
			right: 30px;
			bottom: 30px;
			i {
				font-size: 20px;
				margin-left: 15px;
				cursor: pointer;
				opacity: 0.8;
				&:hover {
					opacity: 1;
				}
			}
		}
	}
}
:deep(.el-input-group__append) {
	background: var(--el-bg-color);
	padding: 0px 15px;
}
:deep(.el-input__wrapper.is-focus) {
	box-shadow: 0 0 0 1px var(--el-input-border-color, var(--el-border-color)) inset !important;
}
:deep(.el-input__inner) {
	border-right-color: var(--el-border-color-extra-light);
	&:hover {
		border-color: var(--el-border-color-extra-light);
	}
}
</style>
