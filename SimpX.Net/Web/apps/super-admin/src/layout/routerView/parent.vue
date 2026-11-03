<template>
	<div class="layout-parent" ref="layoutParentRef">
		<router-view v-slot="{ Component }">
			<transition :name="setTransitionName" mode="out-in">
				<keep-alive :include="getKeepAliveNames">
					<component :is="Component" :key="state.refreshRouterViewKey" class="w100" v-show="!isIframePage" />
				</keep-alive>
			</transition>
		</router-view>
		<transition :name="setTransitionName" mode="out-in">
			<Iframes class="w100" v-show="isIframePage" :refreshKey="state.iframeRefreshKey" :name="setTransitionName" :list="state.iframeList" />
		</transition>
	</div>
</template>

<script setup lang="ts" name="layoutParentView">
import { defineAsyncComponent, computed, reactive, onBeforeMount, onUnmounted, nextTick, watch, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useKeepALiveNames } from '/@/stores/keepAliveNames';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';

// 引入组件
const Iframes = defineAsyncComponent(() => import('/@/layout/routerView/iframes.vue'));

// 定义变量内容
const route = useRoute();
const router = useRouter();
const storesKeepAliveNames = useKeepALiveNames();
const storesThemeConfig = useThemeConfig();
const { keepAliveNames, cachedViews } = storeToRefs(storesKeepAliveNames);
const { themeConfig } = storeToRefs(storesThemeConfig);
const state = reactive<ParentViewState>({
	refreshRouterViewKey: '', // 非 iframe tagsview 右键菜单刷新时
	iframeRefreshKey: '', // iframe tagsview 右键菜单刷新时
	keepAliveNameList: [],
	iframeList: [],
});

// 设置主界面切换动画
const setTransitionName = computed(() => {
	return themeConfig.value.animation;
});
// 获取组件缓存列表(name值)
const getKeepAliveNames = computed(() => {
	return themeConfig.value.isTagsview ? cachedViews.value : state.keepAliveNameList;
});
// 设置 iframe 显示/隐藏
const isIframePage = computed(() => {
	return route.meta.isIframe;
});

// 兼容不支持 CSS :has() 的旧内核（如奇安信可信浏览器 Chromium 102）：
// 全局样式用 `.layout-parent > div:has(.full-table)` 让表格页撑满高度，旧内核会把含 :has() 的规则整条丢弃，
// 这里给含 .full-table 的路由根节点补 .layout-full-table 标记，由 element.scss 中的等价规则接管。
const layoutParentRef = ref<HTMLElement | null>(null);
let markTimer: ReturnType<typeof setTimeout> | null = null;
const markFullTableRoot = () => {
	const root = layoutParentRef.value;
	if (!root) return;
	Array.from(root.children).forEach((child) => {
		child.classList.toggle('layout-full-table', !!child.querySelector('.full-table'));
	});
};
// 路由组件懒加载 + transition(out-in) 过渡期间 DOM 是陆续插入的，切换后 1s 内多次补标记
// （首次进入某页要现下载 chunk，比缓存页更慢，窗口给到 ~960ms）
const scheduleMarkFullTableRoot = () => {
	markFullTableRoot();
	if (markTimer) clearTimeout(markTimer);
	let tries = 0;
	const tick = () => {
		markFullTableRoot();
		if (++tries < 12) markTimer = setTimeout(tick, 80);
	};
	markTimer = setTimeout(tick, 80);
};
// 懒加载 chunk 何时到不确定，单靠定时重试会漏；用 MutationObserver 兜底，DOM 有变化就重算（防抖 150ms）
let markObserver: MutationObserver | null = null;
let markDebounce: ReturnType<typeof setTimeout> | null = null;
const observeLayoutParent = () => {
	const root = layoutParentRef.value;
	if (!root || markObserver) return;
	markObserver = new MutationObserver(() => {
		if (markDebounce) clearTimeout(markDebounce);
		markDebounce = setTimeout(markFullTableRoot, 150);
	});
	markObserver.observe(root, { childList: true, subtree: true });
};
// 获取 iframe 组件列表(未进行渲染)
const getIframeListRoutes = async () => {
	router.getRoutes().forEach((v) => {
		if (v.meta.isIframe) {
			v.meta.isIframeOpen = false;
			v.meta.loading = true;
			state.iframeList.push({ ...v });
		}
	});
};
// 刷新当前路由视图（具名 handler：mittBus.off 按引用比对，匿名函数解绑不掉会持续叠加监听）
const onTagsViewRefreshRouterView = (fullPath: string) => {
	const cacheList = cachedViews.value;
	if (route.meta.isKeepAlive) cachedViews.value = cachedViews.value?.filter((name: string) => route.name !== name);
	state.keepAliveNameList = keepAliveNames.value.filter((name: string) => route.name !== name);
	state.refreshRouterViewKey = '';
	state.iframeRefreshKey = '';
	nextTick(() => {
		if (route.meta.isKeepAlive) cachedViews.value = cacheList;
		state.keepAliveNameList = keepAliveNames.value;
		state.refreshRouterViewKey = fullPath;
		state.iframeRefreshKey = fullPath;
	});
};

// 页面加载前，处理缓存，页面刷新时路由缓存处理
onBeforeMount(() => {
	state.keepAliveNameList = keepAliveNames.value;
	mittBus.on('onTagsViewRefreshRouterView', onTagsViewRefreshRouterView);
});
// 页面加载时
onMounted(() => {
	getIframeListRoutes();
	scheduleMarkFullTableRoot();
	observeLayoutParent();
	// https://gitee.com/lyt-top/vue-next-admin/issues/I58U75
	// https://gitee.com/lyt-top/vue-next-admin/issues/I59RXK
	// https://gitee.com/lyt-top/vue-next-admin/pulls/40
	nextTick(() => {
		setTimeout(() => {
			if (themeConfig.value.isCacheTagsView) {
				let tagsViewArr: RouteItem[] = Session.get('tagsViewList') || [];
				cachedViews.value = tagsViewArr.filter((item) => item.meta?.isKeepAlive).map((item) => item.name as string);
			}
		}, 0);
	});
});
// 页面卸载时
onUnmounted(() => {
	mittBus.off('onTagsViewRefreshRouterView', onTagsViewRefreshRouterView);
	if (markTimer) clearTimeout(markTimer);
	if (markDebounce) clearTimeout(markDebounce);
	markObserver?.disconnect();
	markObserver = null;
});
// 监听路由变化，防止 tagsView 多标签时，切换动画消失
// https://toscode.gitee.com/lyt-top/vue-next-admin/pulls/38/files
watch(
	() => route.fullPath,
	() => {
		state.refreshRouterViewKey = decodeURI(route.fullPath);
		scheduleMarkFullTableRoot();
	},
	{
		immediate: true,
	}
);
</script>
