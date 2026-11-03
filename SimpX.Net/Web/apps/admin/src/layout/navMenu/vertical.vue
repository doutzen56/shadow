<template>
	<el-menu router :default-active="state.defaultActive" background-color="transparent" :collapse="state.isCollapse" :unique-opened="themeConfig.isUniqueOpened" :collapse-transition="false">
		<template v-for="val in menuList" :key="val.path">
			<el-sub-menu :index="val.path" v-if="val.children && val.children.length > 0">
				<template #title>
					<SvgIcon :name="val.meta.icon" />
					<span>{{ $t(val.meta.title) }}</span>
				</template>
				<SubItem :chil="val.children" />
			</el-sub-menu>
			<el-menu-item :index="val.path" v-else>
				<SvgIcon :name="val.meta.icon" />
				<template #title v-if="!val.meta.isLink || (val.meta.isLink && val.meta.isIframe)">
					<span>{{ $t(val.meta.title) }}</span>
					<span v-if="val.meta.showTextBadge" class="menu-text-badge">{{ val.meta.showTextBadge }}</span>
				</template>
				<template #title v-else>
					<a class="w100" @click.prevent="onALinkClick(val)">{{ $t(val.meta.title) }}</a>
				</template>
			</el-menu-item>
		</template>
	</el-menu>
</template>

<script setup lang="ts" name="navMenuVertical">
import { defineAsyncComponent, reactive, onMounted, watch } from 'vue';
import { useRoute, onBeforeRouteUpdate, RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';

// 引入组件
const SubItem = defineAsyncComponent(() => import('/@/layout/navMenu/subItem.vue'));

// 定义父组件传过来的值
defineProps({
	// 菜单列表
	menuList: {
		type: Array<RouteRecordRaw>,
		default: () => [],
	},
});

// 定义变量内容
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const route = useRoute();
const state = reactive({
	// 修复：https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
	defaultActive: route.meta.isDynamic ? route.meta.isDynamicPath : route.path,
	isCollapse: false,
});

// 菜单高亮（详情时，父级高亮）
const setParentHighlight = (currentRoute: RouteToFrom) => {
	const { path, meta } = currentRoute;
	const pathSplit = meta?.isDynamic ? meta.isDynamicPath!.split('/') : path!.split('/');
	if (pathSplit.length >= 4 && meta?.isHide) return pathSplit.splice(0, 3).join('/');
	else return path;
};
// 打开外部链接
const onALinkClick = (val: RouteItem) => {
	other.handleOpenLink(val);
};
// 页面加载时
onMounted(() => {
	state.defaultActive = setParentHighlight(route);
});
// 路由更新时
onBeforeRouteUpdate((to) => {
	// 修复：https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
	state.defaultActive = setParentHighlight(to);
	const clientWidth = document.body.clientWidth;
	if (clientWidth < 1000) themeConfig.value.isCollapse = false;
});
// 设置菜单的收起/展开
watch(
	() => themeConfig.value.isCollapse,
	(isCollapse) => {
		document.body.clientWidth <= 1000 ? (state.isCollapse = false) : (state.isCollapse = isCollapse);
	},
	{
		immediate: true,
	}
);
</script>
