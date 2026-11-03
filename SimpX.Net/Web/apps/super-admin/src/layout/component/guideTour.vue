<template>
	<el-tour v-model="state.visible" type="primary" :mask="true" :show-arrow="true" @close="state.visible = false" @finish="state.visible = false">
		<el-tour-step v-for="step in state.steps" :key="step.key" :target="step.target" :title="step.title" :description="step.description" :placement="step.placement" />
	</el-tour>
</template>

<!-- 登录后漫游式引导 -->
<script setup lang="ts" name="layoutGuideTour">
import { computed, onMounted, onUnmounted, reactive, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { Local } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';
import { useThemeConfig } from '/@/stores/themeConfig';

// 已看标记写在本地缓存里：清理缓存后引导会再出现一次（符合预期，不做额外持久化）
// 改版本号即可让所有人的引导重新出现（如：功能入口有调整时）
const GUIDE_KEY = 'guideVersion';
const GUIDE_VERSION = 1;

/** 单个引导步骤 */
type GuideStep = {
	/** 稳定标识，用于 v-for 的 key */
	key: string;
	/** 目标元素（函数形式：每次进入该步时现取，避免布局切换后拿到旧节点） */
	target: () => HTMLElement | null;
	title: string;
	description: string;
	placement: 'bottom' | 'bottom-start' | 'bottom-end' | 'right-start' | 'top' | 'left';
};

// 按视觉动线编排：左侧菜单 → 顶栏快捷区 → 标签页 → 账号菜单
const allSteps: GuideStep[] = [
	{
		key: 'aside',
		target: () => document.querySelector('.layout-aside'),
		title: '功能导航',
		description: '系统所有功能按业务域分组在这里。左上角按钮可折叠收起，鼠标悬停折叠态图标可临时展开。',
		placement: 'right-start',
	},
	{
		key: 'search',
		target: () => document.querySelector('.global-search-trigger'),
		title: '全局搜索',
		description: '按住 Ctrl + K（Mac 为 ⌘K）或直接点击，可模糊搜索菜单名称与路由路径，回车直达。',
		placement: 'bottom-end',
	},
	{
		key: 'breadcrumb',
		target: () => document.querySelector('.layout-navbars-breadcrumb'),
		title: '面包屑',
		description: '显示当前页面在菜单中的位置，可逐级返回；左侧图标用于折叠 / 展开导航。',
		placement: 'bottom-start',
	},
	{
		key: 'tagsView',
		target: () => document.querySelector('.layout-navbars-tagsview'),
		title: '多页签',
		description: '打开的页面以标签形式保留，右键标签可刷新、关闭其它或全部关闭，切换不丢数据。',
		placement: 'bottom',
	},
	{
		key: 'toolBar',
		target: () => document.querySelector('.layout-navbars-breadcrumb-user'),
		title: '顶栏快捷区',
		description: '依次为：AI 对话、组件大小、语言、切换主题、布局配置、消息通知、全屏、在线用户。布局配置里可切换五种布局与主题配色。',
		placement: 'bottom-end',
	},
	{
		key: 'user',
		target: () => document.querySelector('.layout-navbars-breadcrumb-user-link'),
		title: '账号菜单',
		description: '这里可进入个人中心修改资料与密码、清理缓存、开启锁屏以及退出登录。',
		placement: 'bottom-end',
	},
];

const state = reactive({
	visible: false,
	steps: [] as GuideStep[],
});

const { themeConfig } = storeToRefs(useThemeConfig());

// 锁屏遮罩是否已经盖住界面（判定与 App.vue 的 setLockScreen 一致）
// el-tour 带 focus-trap，会在锁屏输入密码时把焦点从密码框抢回去，导致回车失灵，
// 所以锁屏期间既不自动播放也要收起已经开着的引导
const isScreenLocked = computed(() => themeConfig.value.isLockScreen && (themeConfig.value.lockScreenTime ?? 0) <= 1);

// 标记已看（走完 / 关闭 / 中途跳过都算）
const markViewed = () => {
	Local.set(GUIDE_KEY, GUIDE_VERSION);
};

// 打开引导：过滤掉当前布局下不存在的锚点，保证遮罩一定能定位成功
const openGuide = () => {
	if (isScreenLocked.value) return;
	state.steps = allSteps.filter((item) => !!item.target());
	if (state.steps.length === 0) return;
	state.visible = true;
};

// 是否与当前版本一致（不一致说明有新引导版本，需要重新播放）
const isViewed = () => {
	return Number(Local.get(GUIDE_KEY)) === GUIDE_VERSION;
};

/** 本次收起是否由锁屏打断（这种情况不算「已看」，解锁后还应再看一次） */
let closedByLock = false;

// 只要引导从显示变为隐藏，就落标记：覆盖关闭按钮 / ESC / 走完最后一步 / 程序关闭等所有路径
watch(
	() => state.visible,
	(val, oldVal) => {
		if (!oldVal || val) return;
		if (closedByLock) {
			closedByLock = false;
			return;
		}
		markViewed();
	}
);

// 登录后自动触发一次，延迟等待布局与异步组件挂载完成
const autoStart = () => {
	if (isViewed()) return;
	setTimeout(openGuide, 1200);
};

// 引导进行中途被锁屏（空闲倒计时走完）：立刻收起，把键盘焦点还给锁屏密码框
watch(isScreenLocked, (locked) => {
	if (!locked || !state.visible) return;
	closedByLock = true;
	state.visible = false;
});

onMounted(() => {
	autoStart();
	mittBus.on('openGuide', openGuide);
});

onUnmounted(() => {
	mittBus.off('openGuide', openGuide);
	// 引导开着时组件被卸载（如整页刷新/布局切换），同样视为已看，避免下次再弹
	if (state.visible) markViewed();
});

defineExpose({ openGuide });
</script>
