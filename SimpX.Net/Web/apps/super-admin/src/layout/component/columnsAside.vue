<template>
	<div class="layout-columns-aside" v-show="!isTagsViewCurrenFull">
		<div class="layout-logo">
			<img :src="themeConfig.logoUrl" class="layout-logo-medium-img" />
		</div>
		<el-scrollbar>
			<ul @mouseleave="onColumnsAsideMenuMouseleave()">
				<li
					v-for="(v, k) in state.columnsAsideList"
					:key="k"
					@click="onColumnsAsideMenuClick(v)"
					@mouseenter="onColumnsAsideMenuMouseenter(v, k)"
					:ref="
						(el) => {
							if (el) columnsAsideOffsetTopRefs[k] = el;
						}
					"
					:class="{ 'layout-columns-active': state.liIndex === k, 'layout-columns-hover': state.liHoverIndex === k }"
				>
					<div :class="themeConfig.columnsAsideLayout" v-if="!v.meta.isLink || (v.meta.isLink && v.meta.isIframe)">
						<el-tooltip :content="$t(v.meta.title)" placement="right" v-if="!isCollapse">
							<SvgIcon :name="v.meta.icon" style="width: 20px; height: 20px" />
						</el-tooltip>
						<SvgIcon v-else :name="v.meta.icon" />
						<div class="columns-vertical-title font12" v-if="isCollapse">
							{{ $t(v.meta.title) }}
						</div>
					</div>
					<div :class="themeConfig.columnsAsideLayout" v-else>
						<a :href="v.meta.isLink" target="_blank">
							<SvgIcon :name="v.meta.icon" />
							<div class="columns-vertical-title font12">
								{{ $t(v.meta.title) }}
							</div>
						</a>
					</div>
				</li>
				<div ref="columnsAsideActiveRef" :class="themeConfig.columnsAsideStyle"></div>
			</ul>
		</el-scrollbar>
		<div class="layout-logo" style="cursor: pointer; transform: translateY(-12px)" @click="onThemeConfigChange">
			<SvgIcon class="layout-navbars-breadcrumb-icon" :name="isCollapse ? 'ele-Expand' : 'ele-Fold'" :size="16" />
		</div>
	</div>
</template>

<script setup lang="ts" name="layoutColumnsAside">
import { reactive, ref, onMounted, nextTick, watch, onUnmounted } from 'vue';
import { useRoute, useRouter, onBeforeRouteUpdate, RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import mittBus from '/@/utils/mitt';
// import logoMini from '/@/assets/logo-mini.svg';

// 定义变量内容
const columnsAsideOffsetTopRefs = ref<RefType>([]);
const columnsAsideActiveRef = ref();
const stores = useRoutesList();
const storesThemeConfig = useThemeConfig();
const storesTagsViewRoutes = useTagsViewRoutes();
const { routesList, isColumnsMenuHover, isColumnsNavHover } = storeToRefs(stores);
const { themeConfig } = storeToRefs(storesThemeConfig);
const { isTagsViewCurrenFull } = storeToRefs(storesTagsViewRoutes);
const route = useRoute();
const router = useRouter();
const isCollapse = ref(true);
const state = reactive<ColumnsAsideState>({
	columnsAsideList: [],
	liIndex: 0,
	liOldIndex: null,
	liHoverIndex: null,
	liOldPath: null,
	difference: 0,
	routeSplit: [],
});

const onThemeConfigChange = () => {
	isCollapse.value = !isCollapse.value;
	document.documentElement.style.setProperty('--el-columnsMenuWidth', `${isCollapse.value ? 80 : 50}px`);
	document.documentElement.style.setProperty('--el-columnsMenuHeight', `${isCollapse.value ? 60 : 40}px`);
	document.documentElement.style.setProperty('--el-columnsMenuRoundWidth', `${isCollapse.value ? 80 : 50 - 5}px`);
	document.documentElement.style.setProperty('--el-columnsMenuRoundHeight', `${isCollapse.value ? 60 : 40 - 5}px`);

	columnsAsideActiveRef.value.style.top = `${columnsAsideOffsetTopRefs.value[state.liIndex].offsetTop + state.difference}px`;
};

// 设置菜单高亮位置移动
const setColumnsAsideMove = (k: number) => {
	if (k === undefined) return false;
	state.liIndex = k;
	columnsAsideActiveRef.value.style.top = `${columnsAsideOffsetTopRefs.value[k].offsetTop + state.difference}px`;
};
// 菜单高亮点击事件
const onColumnsAsideMenuClick = async (v: RouteItem) => {
	let { path, redirect } = v;
	if (redirect) {
		onColumnsAsideDown(v.k);
		if (route.path.startsWith(redirect)) mittBus.emit('setSendColumnsChildren', setSendChildren(redirect));
		else router.push(redirect);
	} else {
		if (!v.children || v.children?.length === 0) {
			router.push(path);
			onColumnsAsideDown(v.k);
		} else {
			// 显示子级菜单
			const resData: MittMenu = setSendChildren(path);
			if (Object.keys(resData).length <= 0) return false;
			onColumnsAsideDown(resData.item?.k);
			mittBus.emit('setSendColumnsChildren', resData);
		}
	}

	// 一个路由设置自动收起菜单
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6HW7H
	if (!v.children || v.children?.length === 0) themeConfig.value.isCollapse = true;
	else if (v.children.length > 1) themeConfig.value.isCollapse = false;
};
// 鼠标移入时，显示当前的子级菜单
const onColumnsAsideMenuMouseenter = (v: RouteRecordRaw, k: number) => {
	if (!themeConfig.value.isColumnsMenuHoverPreload) return false;
	let { path } = v;
	state.liOldPath = path;
	state.liOldIndex = k;
	state.liHoverIndex = k;
	mittBus.emit('setSendColumnsChildren', setSendChildren(path));
	stores.setColumnsMenuHover(false);
	stores.setColumnsNavHover(true);
};
// 鼠标移走时，显示原来的子级菜单
const onColumnsAsideMenuMouseleave = async () => {
	if (!themeConfig.value.isColumnsMenuHoverPreload) return false;
	await stores.setColumnsNavHover(false);
	// 添加延时器，防止拿到的 store.state.routesList 值不是最新的
	setTimeout(() => {
		if (!isColumnsMenuHover && !isColumnsNavHover) mittBus.emit('restoreDefault');
	}, 100);
};
// 设置高亮动态位置
const onColumnsAsideDown = (k: number) => {
	nextTick(() => {
		setColumnsAsideMove(k);
	});
};
// 设置只有一个路由时设置自动收起菜单
// https://gitee.com/lyt-top/vue-next-admin/issues/I6UW2I
const setMenuAutoCollaps = (path: string) => {
	const resData: MittMenu = setSendChildren(path);
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6HW7H
	(!resData.item?.redirect && resData.children.length == 0) || (resData.item?.redirect && resData.children.length <= 1)
		? (themeConfig.value.isCollapse = true)
		: (themeConfig.value.isCollapse = false);
	return resData;
};
// 设置/过滤路由（非静态路由/是否显示在菜单中）
const setFilterRoutes = () => {
	state.columnsAsideList = filterRoutesFun(routesList.value);
	const resData: MittMenu = setMenuAutoCollaps(route.path);
	onColumnsAsideDown(resData.item?.k);
	// 延迟 500 毫秒更新，防止 aside.vue 组件 setSendColumnsChildren 还没有注册
	setTimeout(() => {
		mittBus.emit('setSendColumnsChildren', resData);
	}, 500);
};
// 传送当前子级数据到菜单中
const setSendChildren = (path: string) => {
	const currentPathSplit = path.split('/');
	let currentData: MittMenu = { children: [] };
	state.columnsAsideList.map((v: RouteItem, k: number) => {
		if (v.path === `/${currentPathSplit[1]}`) {
			v['k'] = k;
			currentData['item'] = { ...v };
			currentData['children'] = [{ ...v }];
			if (v.children) currentData['children'] = v.children;
		}
	});
	return currentData;
};
// 路由过滤递归函数
const filterRoutesFun = <T extends RouteItem>(arr: T[]): T[] => {
	return arr
		.filter((item: T) => !item.meta?.isHide)
		.map((item: T) => {
			item = Object.assign({}, item);
			if (item.children) item.children = filterRoutesFun(item.children);
			return item;
		});
};
// tagsView 点击时，根据路由查找下标 columnsAsideList，实现左侧菜单高亮
const setColumnsMenuHighlight = (path: string) => {
	state.routeSplit = path.split('/');
	state.routeSplit.shift();
	const routeFirst = `/${state.routeSplit[0]}`;
	const currentSplitRoute = state.columnsAsideList.find((v: RouteItem) => v.path === routeFirst);
	if (!currentSplitRoute) return false;
	// 延迟拿值，防止取不到
	setTimeout(() => {
		onColumnsAsideDown(currentSplitRoute.k);
	}, 0);
};
// 销毁变量，防止鼠标再次移入时，保留了上次的记录（具名 handler，保证能真正解绑）
const onRestoreDefault = () => {
	state.liOldIndex = null;
	state.liOldPath = null;
};

// 页面加载时
onMounted(() => {
	setFilterRoutes();
	mittBus.on('restoreDefault', onRestoreDefault);
});
// 页面卸载时
onUnmounted(() => {
	mittBus.off('restoreDefault', onRestoreDefault);
});
// 路由更新时
onBeforeRouteUpdate((to) => {
	const resData = setMenuAutoCollaps(to.path);
	setColumnsMenuHighlight(to.path);
	mittBus.emit('setSendColumnsChildren', resData);
});
// 监听布局配置信息的变化，动态增加菜单高亮位置移动像素
watch(
	[() => themeConfig.value.columnsAsideStyle, isColumnsMenuHover, isColumnsNavHover],
	() => {
		themeConfig.value.columnsAsideStyle === 'columnsRound' ? (state.difference = 3) : (state.difference = 0);
		if (!isColumnsMenuHover.value && !isColumnsNavHover.value) {
			state.liHoverIndex = null;
			mittBus.emit('setSendColumnsChildren', setSendChildren(route.path));
		} else {
			state.liHoverIndex = state.liOldIndex;
			if (!state.liOldPath) return false;
			mittBus.emit('setSendColumnsChildren', setSendChildren(state.liOldPath));
		}
	},
	{
		deep: true,
	}
);
</script>

<style scoped lang="scss">
.layout-columns-aside {
	width: var(--el-columnsMenuWidth);
	height: 100%;
	background: var(--next-bg-columnsMenuBar);
	border-right: 1px solid var(--el-border-color);
	transition: width 0.3s ease-in-out;
	ul {
		position: relative;
		.layout-columns-active,
		.layout-columns-active a {
			// color: var(--next-bg-columnsMenuBarColor) !important;
			color: var(--el-color-primary);
			transition: 0.3s ease-in-out;
			font-weight: 600;
		}
		.layout-columns-hover {
			color: var(--el-color-primary);
			a {
				color: var(--el-color-primary);
			}
		}
		li {
			border-radius: 5px;
			margin: 2px 6px;
			color: var(--next-bg-columnsMenuBarColor);
			width: calc(100% - 12px);
			height: var(--el-columnsMenuHeight);
			text-align: center;
			display: flex;
			cursor: pointer;
			position: relative;
			z-index: 2;
			span,
			.el-icon,
			.iconfont,
			.fa,
			svg,
			[class^='ri-'] {
				transition: transform 0.25s ease;
				display: inline-block;
			}
			&:not(.layout-columns-active):hover {
				//@extend .layout-columns-hover;
				background-color: var(--next-color-hover-bg);
				color: var(--el-color-primary);
				span,
				.el-icon,
				.iconfont,
				.fa,
				svg,
				[class^='ri-'] {
					transform: scale(1.1);
				}
			}
			.columns-vertical {
				margin: auto;
				.columns-vertical-title {
					padding-top: 1px;
				}
			}
			.columns-horizontal {
				display: flex;
				height: var(--el-columnsMenuHeight);
				width: 100%;
				align-items: center;
				padding: 0 5px;
				i {
					margin-right: 3px;
				}
				a {
					display: flex;
					.columns-horizontal-title {
						padding-top: 1px;
					}
				}
			}
			a {
				text-decoration: none;
				color: var(--next-bg-columnsMenuBarColor);
			}
		}
		.columns-round {
			background: var(--next-bg-menuBarActiveColor);
			// color: var(--el-color-primary);
			position: absolute;
			left: 50%;
			height: var(--el-columnsMenuRoundHeight);
			width: calc(var(--el-columnsMenuRoundWidth) - 10px);
			transform: translateX(-50%);
			z-index: 1;
			transition: 0.5s ease-in-out;
			border-radius: 5px;
		}
		.columns-card {
			@extend .columns-round;
			top: 0;
			height: var(--el-columnsMenuHeight);
			width: 100%;
			border-radius: 0;
		}
	}
}

.layout-logo {
	height: var(--el-columnsLogoHeight);
	display: flex;
	align-items: center;
	justify-content: center;
	margin-bottom: 5px;
	margin-top: 5px;
	animation: logoAnimation 0.3s ease-in-out;
	&-medium-img {
		width: 60%;
		height: auto;
	}
	&:hover {
		img {
			animation: logoAnimation 0.3s ease-in-out;
		}
	}
}

:deep(.el-scrollbar) {
	height: calc(100% - 120px);
}
</style>
