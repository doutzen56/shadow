import { defineStore } from 'pinia';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserMenuApi } from '/@/api-services/system/api';
import { MenuOutput } from '/@/api-services/system/models';

import { useRequestOldRoutes } from './requestOldRoutes';

// 候选分组：一个目录（含其面包屑链）下的全部叶子菜单
export type QuickEntryGroup = { id: number; title: string; items: MenuOutput[] };

export type QuickEntryPool = { groups: QuickEntryGroup[]; dirById: Map<number, string> };

// 把菜单树拍平成「目录 -> 叶子菜单」的候选分组，全程只读，不改写 requestOldRoutes 里的源数据
const buildGroups = (nodes: MenuOutput[], trail: string[] = [], groups: QuickEntryGroup[] = [], groupIndex = new Map<number, QuickEntryGroup>()) => {
	nodes.forEach((node) => {
		if (node.meta?.isHide || node.type == 3 || node.status != 1) return;
		if (node.children?.length) {
			buildGroups(node.children, [...trail, node.meta?.title ?? node.name ?? ''], groups, groupIndex);
			return;
		}
		// 快捷入口卡片用 router-link 跳转，纯外链菜单进不了路由，不入候选池
		if (node.meta?.isLink && !node.meta?.isIframe) return;
		const key = node.pid ?? 0;
		let group = groupIndex.get(key);
		if (!group) {
			group = { id: key, title: trail.length ? trail.join(' / ') : '未归类', items: [] };
			groupIndex.set(key, group);
			groups.push(group);
		}
		group.items.push(node);
	});
	return groups;
};

/**
 * 工作台快捷入口（我的常用）
 * 集中持有已选列表与候选池，供快捷入口卡片、添加应用抽屉、标签页右键菜单、全局搜索共用
 */
export const useQuickEntry = defineStore('quickEntry', {
	state: (): { entries: MenuOutput[]; loaded: boolean } => ({
		entries: [],
		loaded: false,
	}),
	getters: {
		// 候选池来自登录菜单树，已按当前用户权限过滤
		pool(): QuickEntryPool {
			const groups = buildGroups((useRequestOldRoutes().requestOldRoutes as MenuOutput[]) || []);
			const dirById = new Map<number, string>();
			groups.forEach((group) => group.items.forEach((item) => dirById.set(item.id as number, group.title)));
			return { groups, dirById };
		},
		entryIds(state): Set<number> {
			return new Set(state.entries.map((v) => v.id));
		},
	},
	actions: {
		// 拉取已收藏菜单
		async load() {
			try {
				const res = await getAPI(SysUserMenuApi).apiSysUserMenuUserMenuListGet();
				this.entries = res.data.result || [];
			} catch (error) {
				this.entries = [];
			}
			this.loaded = true;
		},
		// 全量覆盖保存：提交顺序即卡片展示顺序
		async save(list: MenuOutput[]) {
			await getAPI(SysUserMenuApi).apiSysUserMenuAddPost({ menuIdList: list.map((v) => v.id) as any });
			this.entries = list;
		},
		// 按路由查候选池里的菜单（不在池内说明无权限、已停用或纯外链）
		findPath(path: string): MenuOutput | undefined {
			return this.pool.groups.flatMap((group) => group.items).find((item) => item.path === path);
		},
		// 该路由是否已加入快捷入口
		isPickedPath(path: string): boolean {
			const target = this.findPath(path);
			return !!target && this.entryIds.has(target.id as number);
		},
		// 按路由切换收藏状态，返回结果供调用方提示；不在候选池内的路由（如未授权菜单）返回 notFound
		async toggleByPath(path: string): Promise<'added' | 'removed' | 'notFound'> {
			// 保存接口是按用户全删全插，切换前必须拿到最新列表，否则会把别处新增的入口覆盖掉
			await this.load();
			const target = this.findPath(path);
			if (!target) return 'notFound';
			const picked = this.entryIds.has(target.id as number);
			await this.save(picked ? this.entries.filter((v) => v.id !== target.id) : [...this.entries, target]);
			return picked ? 'removed' : 'added';
		},
	},
});
