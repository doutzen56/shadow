import { createRouter, createWebHashHistory } from 'vue-router';
import NProgress from 'nprogress';
import 'nprogress/nprogress.css';
import pinia from '/@/stores/index';
import { storeToRefs } from 'pinia';
import { useKeepALiveNames } from '/@/stores/keepAliveNames';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Session, Local } from '/@/utils/storage';
import { staticRoutes, notFoundAndNoPower } from '/@/router/route';
import { initFrontEndControlRoutes } from '/@/router/frontEnd';
import { initBackEndControlRoutes } from '/@/router/backEnd';
import { clearTokens } from '/@/utils/axios-utils';
import { showNetworkErrorThrottled, suppressNetworkErrorFor } from '/@/utils/axios-utils';

/**
 * 1、前端控制路由时：isRequestRoutes 为 false，需要写 roles，需要走 setFilterRoute 方法。
 * 2、后端控制路由时：isRequestRoutes 为 true，不需要写 roles，不需要走 setFilterRoute 方法），
 * 相关方法已拆解到对应的 `backEnd.ts` 与 `frontEnd.ts`（他们互不影响，不需要同时改 2 个文件）。
 * 特别说明：
 * 1、前端控制：路由菜单由前端去写（无菜单管理界面，有角色管理界面），角色管理中有 roles 属性，需返回到 userInfo 中。
 * 2、后端控制：路由菜单由后端返回（有菜单管理界面、有角色管理界面）
 */

// 读取 `/src/stores/themeConfig.ts` 是否开启后端控制路由配置
const storesThemeConfig = useThemeConfig(pinia);
const { themeConfig } = storeToRefs(storesThemeConfig);
const { isRequestRoutes } = themeConfig.value;
const routeStores = useRoutesList(pinia);
const { routesList } = storeToRefs(routeStores);

// 有效租户 ID 的最小值（小于该值视为未设置）
const MIN_VALID_TENANT_ID = 100000;

/**
 * 路由权限初始化：并发保护
 * 原实现用 `routesList.length === 0` 作唯一判据，连续快速导航会并发跑多次初始化，
 * 造成重复 addRoute / 重复拉取用户与字典。这里缓存同一个 Promise，保证只初始化一次；
 * 失败时清空缓存，允许下次导航重试。
 */
let initRoutesPromise: Promise<void> | null = null;
const ensureRoutesInit = () => {
	if (!initRoutesPromise) {
		initRoutesPromise = (async () => {
			try {
				if (isRequestRoutes) {
					// 后端控制路由：路由数据初始化，防止刷新时丢失
					const isNoPower = await initBackEndControlRoutes();
					if (isNoPower) clearTokens();
				} else {
					// https://gitee.com/lyt-top/vue-next-admin/issues/I5F1HP
					await initFrontEndControlRoutes();
				}
			} catch (e) {
				initRoutesPromise = null;
				throw e;
			}
		})();
	}
	return initRoutesPromise;
};

/**
 * 创建一个可以被 Vue 应用程序使用的路由实例
 * @method createRouter(options: RouterOptions): Router
 * @link 参考：https://next.router.vuejs.org/zh/api/#createrouter
 */
export const router = createRouter({
	history: createWebHashHistory(),
	/**
	 * 说明：
	 * 1、notFoundAndNoPower 默认添加 404、401 界面，防止一直提示 No match found for location with path 'xxx'
	 * 2、backEnd.ts(后端控制路由)、frontEnd.ts(前端控制路由) 中也需要加 notFoundAndNoPower 404、401 界面。
	 *    防止 404、401 不在 layout 布局中，不设置的话，404、401 界面将全屏显示
	 */
	routes: [...notFoundAndNoPower, ...staticRoutes],
});

/**
 * 路由多级嵌套数组处理成一维数组
 * @param arr 传入路由菜单数据数组
 * @returns 返回处理后的一维路由菜单数组
 */
export function formatFlatteningRoutes(arr: any) {
	if (arr.length <= 0) return false;
	for (let i = 0; i < arr.length; i++) {
		if (arr[i].children) {
			arr = arr.slice(0, i + 1).concat(arr[i].children, arr.slice(i + 1));
		}
	}
	return arr;
}

/**
 * 一维数组处理成多级嵌套数组（只保留二级：也就是二级以上全部处理成只有二级，keep-alive 支持二级缓存）
 * @description isKeepAlive 处理 `name` 值，进行缓存。顶级关闭，全部不缓存
 * @link 参考：https://v3.cn.vuejs.org/api/built-in-components.html#keep-alive
 * @param arr 处理后的一维路由菜单数组
 * @returns 返回将一维数组重新处理成 `定义动态路由（dynamicRoutes）` 的格式
 */
export function formatTwoStageRoutes(arr: any) {
	if (arr.length <= 0) return false;
	const newArr: any = [];
	const cacheList: Array<string> = [];
	arr.forEach((v: any) => {
		if (v.path == null || v.path == undefined) return;

		if (v.path === '/') {
			newArr.push({ component: v.component, name: v.name, path: v.path, redirect: v.redirect, meta: v.meta, children: [] });
		} else {
			// 判断是否是动态路由（xx/:id/:name），用于 tagsView 等中使用
			// 修复：https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
			if (v.path.indexOf('/:') > -1) {
				v.meta['isDynamic'] = true;
				v.meta['isDynamicPath'] = v.path;
			}
			newArr[0].children.push({ ...v });
			// 存 name 值，keep-alive 中 include 使用，实现路由的缓存
			// 路径：/@/layout/routerView/parent.vue
			if (newArr[0].meta.isKeepAlive && v.meta.isKeepAlive) {
				cacheList.push(v.name);
			}
		}
	});
	const stores = useKeepALiveNames(pinia);
	stores.setCacheKeepAlive(cacheList);
	return newArr;
}

// 路由加载前
router.beforeEach(async (to, from) => {
	NProgress.configure({ showSpinner: false });
	routeStores.setCurrentRoute(to as unknown as RouteItem); // 记录最新当前路由对象

	if (to.meta.title) NProgress.start();

	// 如果是登录页，且带 token 参数，则跳转登录页做登录操作
	const token = window.location.hash.startsWith('#/login') && to.query.token ? undefined : Session.get('token');

	if (to.meta.isPublic && !token) {
		NProgress.done();
		return;
	} else {
		if (!token) {
			var routeLocation = `/login?redirect=${to.path}&params=${JSON.stringify(to.query ? to.query : to.params)}`;
			// 附加租户Id标识
			var tenantid = Number(Local.get('tid'));
			if (!isNaN(tenantid) && tenantid >= MIN_VALID_TENANT_ID) routeLocation += `&tid=${tenantid}`;
			Session.clear();
			NProgress.done();
			return routeLocation;
		} else if (token && to.path === '/login') {
			NProgress.done();
			return '/home/dashboard';
		} else {
			if (routesList.value.length === 0) {
				// 兼容后端未启动 / 502 / 超时等场景：旧实现下权限初始化一旦失败会造成路由永久挂起，
				// 此处 try/catch 兜底，给用户明确提示并放行，避免页面白屏
				try {
					await ensureRoutesInit();
					// 解决刷新时，一直跳 404 页面问题，关联问题 No match found for location with path 'xxx'
					// to.query 防止页面刷新时，普通路由带参数时，参数丢失。动态路由（xxx/:id/:name"）isDynamic 无需处理
					return { path: to.path, query: to.query };
				} catch (e: any) {
					NProgress.done();
					console.error('[Router] 初始化路由权限失败，可能是后端未启动或网络异常', e);
					// 走同一个节流器：只弹一个提示，后续跳入 login 页后批量失败的子请求在 5s 内不再弹
					showNetworkErrorThrottled(e);
					suppressNetworkErrorFor(5000);
					// 清除可能已落盘的半成品状态，跳登录页等用户手动重试。
					// 带上 redirect 与 params 与上方 token 丢失分支保持一致，避免登录后回跳时 Object.keys(undefined) 报错
					Session.clear();
					const paramsStr = JSON.stringify(to.query ? to.query : to.params);
					return `/login?redirect=${to.path}&params=${paramsStr}`;
				}
			} else {
				return;
			}
		}
	}
});

// 路由加载后
router.afterEach(() => {
	NProgress.done();
});

// 导出路由
export default router;
