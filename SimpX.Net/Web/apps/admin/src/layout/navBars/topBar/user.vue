<template>
	<div class="layout-navbars-breadcrumb-user pr15" :style="{ flex: layoutUserFlexNum }">
		<!-- 全局搜索（含触发按钮与弹窗，Ctrl + K） -->
		<Search />
		<el-dropdown :show-timeout="70" :hide-timeout="50" trigger="click" @command="onComponentSizeChange">
			<div class="layout-navbars-breadcrumb-user-icon">
				<i class="iconfont icon-ziti" :title="'组件大小'"></i>
			</div>
			<template #dropdown>
				<el-dropdown-menu>
					<el-dropdown-item command="large" :disabled="state.disabledSize === 'large'">{{ '大型' }}</el-dropdown-item>
					<el-dropdown-item command="default" :disabled="state.disabledSize === 'default'">{{ '默认' }}</el-dropdown-item>
					<el-dropdown-item command="small" :disabled="state.disabledSize === 'small'">{{ '小型' }}</el-dropdown-item>
				</el-dropdown-menu>
			</template>
		</el-dropdown>
		<el-dropdown v-if="themeConfig.i18NSwitch" :show-timeout="70" :hide-timeout="50" trigger="click" @command="onLanguageChange">
			<div class="layout-navbars-breadcrumb-user-icon">
				<!-- <span class="lang-flag" :class="`fi fi-${currentCountryCode}`"></span> -->
				<i class="ri-translate-2"></i>
			</div>
			<template #dropdown>
				<el-dropdown-menu>
					<el-dropdown-item v-for="(value, key) in languageList" :key="key" :command="key" :disabled="state.disabledI18n === key">
						<div class="flex items-center">
							<div class="mr-2">
								<!-- <span class="lang-flag" :class="`fi fi-${getCountryCode(key)}`"></span> -->
								<span style="display: inline-flex; align-items: center; justify-content: center; width: 16px; height: 16px; border: 1px solid; border-radius: 50%; font-size: 10px">
									{{ value.charAt(0) }}
								</span>
							</div>
							<div style="margin-left: 10px">
								{{ value }}
							</div>
						</div>
					</el-dropdown-item>
				</el-dropdown-menu>
			</template>
		</el-dropdown>
		<div class="layout-navbars-breadcrumb-user-icon" @click="onChatClick">
			<el-icon :title="'与AI对话'">
				<ele-ChatDotRound />
			</el-icon>
		</div>
		<div class="layout-navbars-breadcrumb-user-icon" @click="themeTransition($event, !themeConfig.isIsDark)">
			<el-icon :title="'切换主题'">
				<ele-Sunny v-if="themeConfig.isIsDark" />
				<ele-Moon v-else />
			</el-icon>
		</div>
		<div class="layout-navbars-breadcrumb-user-icon" @click="onLayoutSetingClick">
			<i class="icon-skin iconfont" :title="'布局配置'"></i>
		</div>
		<div class="layout-navbars-breadcrumb-user-icon">
			<!-- persistent=true：鼠标移开气泡后不销毁内容，否则内部弹出的「消息详情」弹窗会被一起卸载 -->
			<el-popover placement="bottom" trigger="hover" transition="el-zoom-in-top" :width="400" :persistent="true">
				<template #reference>
					<el-badge :is-dot="hasUnreadNotice">
						<el-icon :title="'消息'">
							<ele-Bell />
						</el-icon>
					</el-badge>
				</template>
				<UserNews :noticeList="state.noticeList" />
			</el-popover>
		</div>
		<div class="layout-navbars-breadcrumb-user-icon" @click="onScreenfullClick">
			<i class="iconfont" :title="state.isScreenfull ? '关全屏' : '开全屏'" :class="!state.isScreenfull ? 'icon-fullscreen' : 'icon-tuichuquanping'"></i>
		</div>
		<div class="layout-navbars-breadcrumb-user-icon mr10" @click="onOnlineUserClick">
			<el-icon :title="'在线用户'">
				<ele-User />
			</el-icon>
		</div>

		<el-dropdown :show-timeout="70" :hide-timeout="50" size="large" @command="onHandleCommandClick">
			<span class="layout-navbars-breadcrumb-user-link">
				<el-tooltip effect="dark" placement="left">
					<template #content>
						{{ '账号' }}：{{ userInfos.account }}<br />
						{{ '姓名' }}：{{ userInfos.realName }}<br />
						{{ '电话' }}：{{ userInfos.phone }}<br />
						{{ '邮箱' }}：{{ userInfos.email }}<br />
						{{ '部门' }}：{{ userInfos.orgName }}<br />
						{{ '职位' }}：{{ userInfos.posName }}<br />
					</template>

					<img :src="userInfos.avatar" class="layout-navbars-breadcrumb-user-link-photo mr5" />
				</el-tooltip>
				<span class="layout-navbars-breadcrumb-user-link-text">
					<span class="layout-navbars-breadcrumb-user-link-name">{{ userDisplayName }}</span>
					<span v-if="userSubTitle" class="layout-navbars-breadcrumb-user-link-sub" :title="userSubTitle">{{ userSubTitle }}</span>
				</span>
				<el-icon class="el-icon--right">
					<ele-ArrowDown />
				</el-icon>
			</span>
			<template #dropdown>
				<el-dropdown-menu>
					<!-- <el-dropdown-item command="/dashboard/home">{{ '首页' }}</el-dropdown-item> -->
					<el-dropdown-item :icon="Avatar" command="/system/userCenter">{{ '个人中心' }}</el-dropdown-item>
					<el-dropdown-item :icon="Guide" command="guide">{{ '新手指引' }}</el-dropdown-item>
					<el-dropdown-item :icon="Loading" command="clearCache">{{ '清理缓存' }}</el-dropdown-item>
					<el-dropdown-item :icon="Lock" divided command="lockScreen">{{ '开启锁屏' }}</el-dropdown-item>
					<el-dropdown-item :icon="CircleCloseFilled" divided command="logOut">{{ '退出登录' }}</el-dropdown-item>
				</el-dropdown-menu>
			</template>
		</el-dropdown>

		<OnlineUser ref="onlineUserRef" />
		<ChangePassword ref="changePasswordRef" />
		<UpgradeInfo ref="upgradeInfoRef" />
	</div>
</template>

<script setup lang="ts" name="layoutBreadcrumbUser">
import { defineAsyncComponent, ref, computed, reactive, onMounted, onUnmounted } from 'vue';
import { ElMessageBox, ElMessage, ElNotification } from 'element-plus';
import { Avatar, CircleCloseFilled, Loading, Lock, Guide } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import screenfull from 'screenfull';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useRoutesList } from '/@/stores/routesList';
import other from '/@/utils/other';
import mittBus from '/@/utils/mitt';
import { Local, Session } from '/@/utils/storage';
import Push from 'push.js';
import { signalR } from '/@/views/system/onlineUser/signalR';
import { languageList, getCountryCode, getTrLang } from '/@/i18n';
import { themeTransition } from '/@/utils/themeTransition';

import { clearAccessTokens, getAPI } from '/@/utils/axios-utils';
import { SysAuthApi, SysCommonApi, SysNoticeApi, SysUpgradeApi, SysUserApi } from '/@/api-services/system/api';

// 引入组件
const UserNews = defineAsyncComponent(() => import('/@/layout/navBars/topBar/userNews.vue'));
const Search = defineAsyncComponent(() => import('/@/layout/navBars/topBar/globalSearch.vue'));
const OnlineUser = defineAsyncComponent(() => import('/@/views/system/onlineUser/index.vue'));
const ChangePassword = defineAsyncComponent(() => import('/@/views/system/user/component/changePassword.vue'));
const UpgradeInfo = defineAsyncComponent(() => import('/@/views/system/upgrade/component/upgradeInfo.vue'));
const routeStores = useRoutesList();

// 定义变量内容
const { locale, t } = useI18n();
const router = useRouter();
const stores = useUserInfo();
const storesThemeConfig = useThemeConfig();
const { userInfos } = storeToRefs(stores);
const { themeConfig } = storeToRefs(storesThemeConfig);
const onlineUserRef = ref();
const changePasswordRef = ref();
const upgradeInfoRef = ref();
const currentCountryCode = ref<string>(getCountryCode(themeConfig.value.globalI18n));
const state = reactive({
	isScreenfull: false,
	disabledI18n: 'zh-cn',
	disabledSize: 'large',
	noticeList: [] as any, // 站内信列表
	tagsRefsIndex: 0,
	tagsViewList: [],
	tagsViewRoutesList: [],
});

// 设置分割样式
const layoutUserFlexNum = computed(() => {
	let num: string | number = '';
	const { layout, isClassicSplitMenu } = themeConfig.value;
	const layoutArr: string[] = ['defaults', 'columns'];
	if (layoutArr.includes(layout) || (layout === 'classic' && !isClassicSplitMenu)) num = '1';
	else num = '';
	return num;
});
// 是否有未读消息
const hasUnreadNotice = computed(() => {
	return state.noticeList.some((r: any) => r.readStatus == undefined || r.readStatus == 0);
});
// 顶栏显示名：未维护真实姓名时回退为账号
const userDisplayName = computed(() => {
	const { realName, account } = userInfos.value;
	return realName ? realName : account;
});
// 顶栏姓名下方的辅助信息：系统账号 · 部门名称（部门为空时只显示账号，避免残留分隔符）
const userSubTitle = computed(() => {
	const account = (userInfos.value.account ?? '').trim();
	const orgName = (userInfos.value.orgName ?? '').trim();
	return [account, orgName].filter((r) => r).join('·');
});
// 全屏点击时
const onScreenfullClick = () => {
	if (!screenfull.isEnabled) {
		ElMessage.warning('暂不不支持全屏');
		return false;
	}
	screenfull.toggle();
	screenfull.on('change', () => {
		if (screenfull.isFullscreen) state.isScreenfull = true;
		else state.isScreenfull = false;
	});
};
// 布局配置 icon 点击时
const onLayoutSetingClick = () => {
	mittBus.emit('openSettingsDrawer');
};
// 下拉菜单点击时
const onHandleCommandClick = (path: string) => {
	if (path === 'clearCache') {
		Session.clear();
		Local.clear();
		window.location.reload();
	} else if (path === 'guide') {
		// 手动唤起新手指引（走 mittBus，避免与布局组件强耦合）
		mittBus.emit('openGuide');
	} else if (path === 'lockScreen') {
		Local.remove('themeConfig');
		themeConfig.value.isLockScreen = true;
		themeConfig.value.lockScreenTime = 1;
		Local.set('themeConfig', themeConfig.value);
	} else if (path === 'logOut') {
		ElMessageBox({
			closeOnClickModal: false,
			closeOnPressEscape: false,
			title: '提示',
			message: '此操作将退出登录, 是否继续?',
			type: 'warning',
			showCancelButton: true,
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			// buttonSize: 'default',
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = '退出中';
					try {
						// 退出登录不重置「新手指引已看」标记：只有主动点「清理缓存」才需要重看一次
						const guideViewed = Local.get('guideVersion');
						Session.clear();
						Local.clear();
						if (guideViewed != null) Local.set('guideVersion', guideViewed);
						await getAPI(SysAuthApi).apiSysAuthLogoutPost();
					} catch (error) {
						console.error(error);
					}
					instance.confirmButtonLoading = false;
					done();
				} else {
					done();
				}
			},
		})
			.then(async () => {
				clearAccessTokens();
			})
			.catch(() => {});
	} else {
		router.push(path);
	}
};
// 在线用户列表
const onOnlineUserClick = () => {
	onlineUserRef.value?.openDrawer?.();
};
// 组件大小改变
const onComponentSizeChange = (size: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalComponentSize = size;
	Local.set('themeConfig', themeConfig.value);
	initI18nOrSize('globalComponentSize', 'disabledSize');
};

// 语言切换
const onLanguageChange = async (lang: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalI18n = lang;
	Local.set('themeConfig', themeConfig.value);
	currentCountryCode.value = getCountryCode(lang);
	locale.value = lang;
	other.useTitle();
	initI18nOrSize('globalI18n', 'disabledI18n');
	refreshCurrentTabpage();

	// 同步全局翻译系统并持久化，刷新后仍然生效
	const trLang = getTrLang(lang);
	window.$changeLang(trLang);
	window.localStorage.setItem('lang', trLang);

	await getAPI(SysCommonApi).apiSysCommonSetCulturePost(lang);
};

// 聊天点击
const onChatClick = () => {
	router.push({
		path: '/agent/chat',
	});
};

// 刷新当前标签页
const refreshCurrentTabpage = () => {
	mittBus.emit('onCurrentContextmenuClick', { ...routeStores.currentRoute, contextMenuClickId: 0 });
};

// 初始化组件大小/i18n
const initI18nOrSize = (value: string, attr: string) => {
	(<any>state)[attr] = Local.get('themeConfig')[value];
};

// 监听浏览器 当前系统是否在当前页（具名函数，方便卸载时移除，避免切语言重挂载后重复监听）
const onVisibilityChange = () => {
	if (!document.hidden) {
		// 清空关闭消息通知，
		Push.clear();
	}
};

// 页面加载时
onMounted(async () => {
	if (Local.get('themeConfig')) {
		initI18nOrSize('globalComponentSize', 'disabledSize');
		initI18nOrSize('globalI18n', 'disabledI18n');
	}
	// 手动获取用户桌面通知权限
	if (Push.Permission.GRANTED) {
		// 判断当前是否有权限，没有则手动获取
		Push.Permission.request(undefined, undefined);
	}
	// 监听浏览器 当前系统是否在当前页
	document.addEventListener('visibilitychange', onVisibilityChange);
	// 加载未读站内信；SimpX 暂无 Pro 同名接口时跳过，不挡壳子
	try {
		const res = await getAPI(SysNoticeApi).apiSysNoticeUnReadListGet();
		state.noticeList = res.data.result ?? [];
	} catch {
		state.noticeList = [];
	}

	// 接收站内信
	signalR.on('PublicNotice', receiveNotice);

	// 加载系统更新日志；接口不存在时忽略
	try {
		const res1 = await getAPI(SysUpgradeApi).apiSysUpgradeLastUnReadGet();
		if (res1.data.result != null) {
			upgradeInfoRef.value?.openDialog(res1.data.result);
		}
	} catch {
		/* SimpX 暂无 upgrade 未读接口 */
	}

	// 是否修改密码
	await changePassword();
});
// 页面卸载时（切换语言会静默重挂载本组件，必须清理副作用，否则监听与站内信处理器会重复注册）
onUnmounted(() => {
	document.removeEventListener('visibilitychange', onVisibilityChange);
	signalR.off('PublicNotice', receiveNotice);
});

const receiveNotice = (msg: any) => {
	state.noticeList.unshift(msg);

	ElNotification({
		title: '提示',
		message: '您有一条新消息...',
		type: 'info',
		position: 'bottom-right',
	});
	Push.create('提示', {
		body: '你有一条新的消息',
		icon: 'logo.png', // public目录下的
		timeout: 4500, // 通知显示时间，单位为毫秒
	});
};

// 修改密码
const changePassword = async () => {
	// 若是超管则不进行强制修改密码和有效期验证
	if (userInfos.value.accountType == 999) return;

	// 等待配置加载完成
	if (themeConfig.value.forceChangePassword == undefined) {
		while (themeConfig.value.forceChangePassword != undefined) {
			await new Promise((resolve) => setTimeout(resolve, 100));
		}
	}
	// 开启强制修改密码
	const enabledForceChangePassword = themeConfig.value.forceChangePassword ?? false;
	if (enabledForceChangePassword && !userInfos.value.lastChangePasswordTime) {
		changePasswordRef.value?.openDialog();
		return;
	}

	// 验证密码有效期
	const passwordExpirationTime = themeConfig.value.passwordExpirationTime ?? 0;
	if (passwordExpirationTime > 0) {
		const res = await getAPI(SysUserApi).apiSysUserVerifyPwdExpirationTimePost();
		if (!res.data.result) {
			changePasswordRef.value?.openDialog();
		}
	}
};
</script>

<style scoped lang="scss">
.layout-navbars-breadcrumb-user {
	display: flex;
	align-items: center;
	justify-content: flex-end;

	&-link {
		height: 100%;
		display: flex;
		align-items: center;
		white-space: nowrap;

		&-photo {
			width: 25px;
			height: 25px;
			border-radius: 100%;
			flex-shrink: 0;
		}

		// 姓名 + 账号·部门 的纵向两行块
		&-text {
			display: flex;
			flex-direction: column;
			align-items: flex-start;
			justify-content: center;
			margin-right: 2px;
			line-height: 1.2;
		}

		&-name,
		&-sub {
			max-width: 150px;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		&-name {
			font-size: 14px;
			line-height: 18px;
		}

		// 辅助信息弱化：字号更小 + 降低不透明度，深浅主题下都能自然退到背景层
		&-sub {
			font-size: 11px;
			line-height: 15px;
			opacity: 0.55;
		}
	}

	// 顶栏图标按钮：32px 圆角方块悬浮态，替代原来撑满 50px 通高的直角色块
	&-icon {
		width: 32px;
		padding: 0;
		margin: 0 1px;
		cursor: pointer;
		color: var(--next-bg-topBarColor);
		height: 32px;
		border-radius: 8px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		transition:
			background-color 0.2s ease,
			color 0.2s ease,
			transform 0.2s ease;

		&:hover {
			background: var(--next-color-user-hover);
			color: var(--el-color-primary);

			i {
				display: inline-block;
				animation: logoAnimation 0.3s ease-in-out;
			}
		}
	}

	:deep(.el-dropdown) {
		color: var(--next-bg-topBarColor);
	}

	:deep(.el-badge) {
		height: 36px;
		display: inline-flex;
		align-items: center;
	}

	:deep(.el-badge__content.is-fixed) {
		top: 8px;
		right: 6px;
	}
}
</style>
