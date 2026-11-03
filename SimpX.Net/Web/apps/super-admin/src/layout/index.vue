<template>
	<div class="layout-root">
		<component :is="layouts[themeConfig.layout]" />
		<!-- 新手指引：登录完成后自动漫游一次，也可从账号菜单手动唤起 -->
		<GuideTour />
	</div>
</template>

<script setup lang="ts" name="layout">
import { onBeforeMount, onUnmounted, defineAsyncComponent, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { Local } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';

/*
 * ⚠️ 本组件必须保持「单一元素根节点」（外层 .layout-root）。
 * App.vue 在 <router-view> 上用了 v-show（锁屏时隐藏整个应用）；v-show 是运行时指令，
 * 会沿组件链继承到根 vnode，只有根 vnode 是真实元素时才能生效。
 * 若这里渲染成 Fragment（多根节点，或模板首行写了 HTML 注释），指令会挂到 Fragment 锚点
 * 文本节点上而失效，并触发 Vue 警告：
 *   "Runtime directive used on component with non-element root node."
 */
// 引入组件
const layouts: any = {
	defaults: defineAsyncComponent(() => import('/@/layout/main/defaults.vue')),
	classic: defineAsyncComponent(() => import('/@/layout/main/classic.vue')),
	transverse: defineAsyncComponent(() => import('/@/layout/main/transverse.vue')),
	columns: defineAsyncComponent(() => import('/@/layout/main/columns.vue')),
};
// 登录后漫游式引导（内部自行判断是否首次进入）
const GuideTour = defineAsyncComponent(() => import('/@/layout/component/guideTour.vue'));

// 定义变量内容
const storesThemeConfig = useThemeConfig();
const storesTagsViewRoutes = useTagsViewRoutes();
const { themeConfig } = storeToRefs(storesThemeConfig);

// 项目全屏时给 body 挂 class，配合全局 !important 规则强制隐藏布局外壳，
watch(
	() => storesTagsViewRoutes.isTagsViewCurrenFull,
	(val) => {
		document.body.classList.toggle('layout-project-fullscreen', !!val);
	},
	{ immediate: true }
);

// 20240117 最大窗体宽度
let maxClientWidth = document.body.clientWidth;

// 窗口大小改变时(适配移动端)
const onLayoutResize = () => {
	if (!Local.get('oldLayout')) Local.set('oldLayout', themeConfig.value.layout);
	const clientWidth = document.body.clientWidth;

	// 20240117 最大窗体宽度 > 当前宽度，不触发 layoutMobileResize 事件
	if (maxClientWidth > clientWidth) return;
	maxClientWidth = clientWidth;

	if (clientWidth < 1000) {
		themeConfig.value.isCollapse = false;
		mittBus.emit('layoutMobileResize', {
			layout: 'defaults',
			clientWidth,
		});
	} else {
		mittBus.emit('layoutMobileResize', {
			layout: Local.get('oldLayout') ? Local.get('oldLayout') : themeConfig.value.layout,
			clientWidth,
		});
	}
};

// 页面加载前
onBeforeMount(() => {
	onLayoutResize();
	window.addEventListener('resize', onLayoutResize);
});

// 页面卸载时
onUnmounted(() => {
	window.removeEventListener('resize', onLayoutResize);
});
</script>

<style scoped lang="scss">
/* 布局外壳：撑满 #app，内部 .layout-container 的 height:100% 才有参照 */
.layout-root {
	width: 100%;
	height: 100%;
}
</style>
