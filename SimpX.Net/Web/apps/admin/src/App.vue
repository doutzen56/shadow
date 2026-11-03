<template>
	<el-config-provider :size="getGlobalComponentSize" :locale="getGlobalI18n">
		<router-view v-show="setLockScreen" />
		<LockScreen v-if="themeConfig.isLockScreen" />
		<Settings ref="settingsRef" v-show="setLockScreen" />
		<CloseFull v-if="!themeConfig.isLockScreen" />
	</el-config-provider>
</template>

<script setup lang="ts" name="app">
import { defineAsyncComponent, computed, ref, onBeforeMount, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';
import { Local, Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';
import setIntroduction from '/@/utils/setIconfont';
// import Watermark from '/@/utils/watermark';
import { initIdleTimeout } from '/@/utils/idleTimeout';
import { updateFavicon } from '/@/utils/sysInfo';

// 引入组件
const LockScreen = defineAsyncComponent(() => import('/@/layout/lockScreen/index.vue'));
const Settings = defineAsyncComponent(() => import('/@/layout/navBars/topBar/settings.vue'));
const CloseFull = defineAsyncComponent(() => import('/@/layout/navBars/topBar/closeFull.vue'));

// 定义变量内容
const { messages, locale } = useI18n();
const settingsRef = ref();
const route = useRoute();
const stores = useTagsViewRoutes();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// 设置锁屏时组件显示隐藏
const setLockScreen = computed(() => {
	// 防止锁屏后，刷新出现不相关界面
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6AF8P
	return themeConfig.value.isLockScreen ? themeConfig.value.lockScreenTime > 1 : themeConfig.value.lockScreenTime >= 0;
});

// 获取全局组件大小
const getGlobalComponentSize = computed(() => {
	return themeConfig.value.globalComponentSize;
});

// 获取全局 i18n
const getGlobalI18n = computed(() => {
	return messages.value[locale.value];
});

// 设置初始化，防止刷新时恢复默认
onBeforeMount(() => {
	toggleTheme(true);
	// 设置批量第三方 icon 图标
	setIntroduction.cssCdn();
	// 设置批量第三方 js
	setIntroduction.jsCdn();
});

// 打开布局配置抽屉（具名函数：mittBus.off 按引用比对，匿名函数解绑不掉）
const onOpenSettingsDrawer = () => {
	settingsRef.value?.openDrawer();
};

// 页面加载时
onMounted(() => {
	toggleTheme(false);
	nextTick(() => {
		// 监听布局配置弹窗点击打开
		mittBus.on('openSettingsDrawer', onOpenSettingsDrawer);
		// 获取缓存中的布局配置
		if (Local.get('themeConfig')) {
			storesThemeConfig.setThemeConfig({ themeConfig: Local.get('themeConfig') });
			// 同步当前多语言状态
			if (themeConfig.value.globalI18n) {
				locale.value = themeConfig.value.globalI18n;
			}
			document.documentElement.style.cssText = Local.get('themeConfigStyle');

			// 从本地存储恢复favicon
			const storedConfig = Local.get('themeConfig');
			if (storedConfig && storedConfig.logoUrl) {
				updateFavicon(storedConfig.logoUrl);
			}
		}
		// 获取缓存中的全屏配置
		if (Session.get('isTagsViewCurrenFull')) {
			stores.setCurrenFullscreen(Session.get('isTagsViewCurrenFull'));
		}
	});
});

// 切换主题动画
const toggleTheme = (enable: boolean) => {
	const body = document.body;
	if (enable) {
		body.classList.add('theme-change');
	} else {
		setTimeout(() => {
			body.classList.remove('theme-change');
		}, 300);
	}
};

// 页面销毁时，关闭监听布局配置/i18n监听
onUnmounted(() => {
	mittBus.off('openSettingsDrawer', onOpenSettingsDrawer);
	document.body.removeEventListener('drop', onBodyDrop);
});

// 监听路由的变化，设置网站标题
watch(
	() => route.path,
	() => {
		other.useTitle();
	},
	{
		deep: true,
	}
);

// 阻止火狐浏览器在拖动时打开新窗口（用 addEventListener，避免覆盖其它监听器，卸载时可清理）
const onBodyDrop = (event: DragEvent) => {
	event.preventDefault();
	event.stopPropagation();
};
document.body.addEventListener('drop', onBodyDrop);

// 初始化全局空闲超时
initIdleTimeout({
	timeout: themeConfig.value.idleTimeout,
});
</script>

<style lang="scss">
/* 查询区（inline 表单）控件统一宽度：写死 171px 各页面无法按需覆盖，改为读取全局变量 */
.el-form--inline {
	.el-form-item {
		.el-select {
			width: var(--next-search-control-width) !important;
		}
		.el-select__wrapper {
			line-height: 22px !important;
		}
		.el-date-editor {
			--el-date-editor-width: var(--next-search-control-width) !important;
		}
		.el-input {
			width: var(--next-search-control-width) !important;
		}
		.el-form-item__label {
			color: var(--el-text-color-regular);
			font-weight: 400;
		}
	}
}
</style>
