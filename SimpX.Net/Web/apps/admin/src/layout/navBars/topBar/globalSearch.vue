<template>
	<div class="layout-global-search">
		<!-- 触发按钮：大屏为胶囊，小屏缩为裸图标 -->
		<div class="global-search-trigger" @click="openSearch">
			<el-icon class="search-icon">
				<ele-Search />
			</el-icon>
			<span class="search-text">{{ '搜索' }}</span>
			<span class="search-kbd">{{ isMacOs ? '⌘K' : 'Ctrl K' }}</span>
		</div>
		<el-dialog v-model="state.isShowSearch" width="600px" :show-close="false" :lock-scroll="false" modal-class="layout-global-search-modal" append-to-body @close="onCloseSearch">
			<el-input v-model="state.menuQuery" ref="menuSearchInputRef" :placeholder="'搜索菜单，支持名称、路由路径'" :prefix-icon="Search" @input="state.highlightedIndex = 0">
				<template #suffix>
					<i class="global-search-kbd">Enter</i>
				</template>
			</el-input>
			<el-scrollbar max-height="450px" ref="searchResultScrollbarRef" class="global-search-scrollbar" always>
				<!-- 搜索结果 -->
				<template v-if="state.menuQuery">
					<div
						v-for="(item, index) in searchResult"
						:key="item.path"
						class="global-search-item"
						:class="{ 'is-highlight': index === state.highlightedIndex }"
						@click="onHandleSelect(item)"
						@mouseenter="onMouseenter(index)"
					>
						<div class="global-search-item-main">
							<div class="global-search-item-title">
								<SvgIcon :name="item.meta?.icon" class="mr5" />
								<span>{{ $t(item.meta?.title || '') }}</span>
							</div>
							<div class="global-search-item-sub">
								<span v-if="dirOf(item)" class="global-search-item-dir">{{ dirOf(item) }}</span>
								<span class="global-search-item-path">{{ item.path }}</span>
							</div>
						</div>
						<el-icon
							class="global-search-item-fav"
							:class="{ 'is-picked': pickedPaths.has(item.path) }"
							:title="pickedPaths.has(item.path) ? '移出快捷入口' : '加入快捷入口'"
							@click.stop="onToggleQuickEntry(item)"
						>
							<ele-StarFilled v-if="pickedPaths.has(item.path)" />
							<ele-Star v-else />
						</el-icon>
					</div>
					<div v-if="searchResult.length === 0" class="global-search-empty">
						<el-icon><ele-Search /></el-icon>
						<span>{{ '未找到相关菜单' }}</span>
					</div>
				</template>
				<!-- 搜索历史 -->
				<template v-else>
					<div v-if="state.historyList.length > 0" class="global-search-history-header">
						<span class="global-search-history-label">{{ '搜索历史' }}</span>
						<span class="global-search-history-clear" @click="onClearHistory">{{ '清空' }}</span>
					</div>
					<div
						v-for="(item, index) in state.historyList"
						:key="item.path"
						class="global-search-item"
						:class="{ 'is-highlight': index === state.historyIndex }"
						@click="onHandleSelect(item)"
						@mouseenter="onMouseenter(index)"
					>
						<div class="global-search-item-main">
							<div class="global-search-item-title">
								<SvgIcon :name="item.meta?.icon" class="mr5" />
								<span>{{ $t(item.meta?.title || '') }}</span>
							</div>
							<div class="global-search-item-sub">
								<span v-if="dirOf(item)" class="global-search-item-dir">{{ dirOf(item) }}</span>
								<span class="global-search-item-path">{{ item.path }}</span>
							</div>
						</div>
						<el-icon class="global-search-item-delete" @click.stop="onDeleteHistory(index)">
							<ele-Close />
						</el-icon>
					</div>
				</template>
			</el-scrollbar>
			<template #footer>
				<div class="global-search-footer">
					<div class="global-search-footer-item">
						<i class="global-search-kbd">Enter</i>
						<span>{{ '选择' }}</span>
					</div>
					<div class="global-search-footer-item">
						<i class="global-search-kbd">↑</i>
						<i class="global-search-kbd">↓</i>
						<span>{{ '切换' }}</span>
					</div>
					<div class="global-search-footer-item">
						<i class="global-search-kbd">Esc</i>
						<span>{{ '退出' }}</span>
					</div>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="layoutGlobalSearch">
import { computed, reactive, ref, nextTick, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import type { ScrollbarInstance } from 'element-plus';
import { ElMessage } from 'element-plus';
import { Search } from '@element-plus/icons-vue';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useQuickEntry } from '/@/stores/quickEntry';
import { Local } from '/@/utils/storage';
import { matchPinyin } from '/@/utils/pinyinMatch';

const historyStorageKey = 'globalSearchHistory';
const historyMaxLength = 10;
const isMacOs = navigator.platform.toUpperCase().includes('MAC');

const storesTagsViewRoutes = useTagsViewRoutes();
const quickEntry = useQuickEntry();
const { tagsViewRoutes } = storeToRefs(storesTagsViewRoutes);
const { t } = useI18n();
const router = useRouter();
const menuSearchInputRef = ref();
const searchResultScrollbarRef = ref<ScrollbarInstance>();
const state = reactive<GlobalSearchState>({
	isShowSearch: false,
	menuQuery: '',
	tagsViewList: [],
	historyList: [],
	highlightedIndex: 0,
	historyIndex: 0,
	isKeyboardNavigating: false,
});

// 搜索结果（匹配路径、标题原文、翻译后标题及其拼音全拼/首字母）
const searchResult = computed(() => {
	const query = state.menuQuery.toLowerCase();
	if (!query) return [];
	return state.tagsViewList.filter((item: RouteItem) => {
		const title = t(item.meta?.title || '');
		return item.path.toLowerCase().includes(query) || item.meta?.title?.toLowerCase().includes(query) || title.toLowerCase().includes(query) || matchPinyin(title, query);
	});
});

// 已加入快捷入口的路由，用于结果行星标的高亮态
const pickedPaths = computed(() => new Set(quickEntry.entries.map((v) => v.path)));

// 结果行的父级菜单链；顶层菜单或菜单不在候选池内时返回空串，该行只显示路由
const dirOf = (item: RouteItem) => {
	const dir = quickEntry.pool.dirById.get(item.id as number);
	if (!dir || dir === '未归类') return '';
	// 候选池的值用 ' / ' 拼接，逐段翻译后改用 › 重接：留 '/' 会和路由的斜杠连成一片
	return dir.split(' / ').map(t).join(' › ');
};

// 点击结果行星标：把该菜单加入 / 移出工作台快捷入口
const onToggleQuickEntry = async (item: RouteItem) => {
	const result = await quickEntry.toggleByPath(item.path);
	if (result === 'notFound') ElMessage({ type: 'warning', message: '该菜单不支持添加为快捷入口' });
	else ElMessage.success(result === 'added' ? '已加入快捷入口' : '已移出快捷入口');
};

// 当前键盘导航作用于哪个列表（有关键词时为搜索结果，否则为历史记录）
const activeList = computed(() => (state.menuQuery ? searchResult.value : state.historyList));
const activeIndex = computed({
	get: () => (state.menuQuery ? state.highlightedIndex : state.historyIndex),
	set: (val: number) => {
		if (state.menuQuery) state.highlightedIndex = val;
		else state.historyIndex = val;
	},
});

// 初始化菜单数据与历史记录
const initTageView = () => {
	if (state.tagsViewList.length > 0) return;
	tagsViewRoutes.value.forEach((v: RouteItem) => {
		if (!v.meta?.isHide && v.type !== 1) state.tagsViewList.push({ ...v });
	});
};

const initHistory = () => {
	const list = Local.get(historyStorageKey);
	state.historyList = Array.isArray(list) ? list : [];
};

// 搜索弹窗打开
const openSearch = () => {
	state.menuQuery = '';
	state.highlightedIndex = 0;
	state.historyIndex = 0;
	initTageView();
	initHistory();
	state.isShowSearch = true;
	nextTick(() => {
		setTimeout(() => menuSearchInputRef.value?.focus(), 100);
	});
};

// 搜索弹窗关闭后重置
const onCloseSearch = () => {
	state.menuQuery = '';
	state.highlightedIndex = 0;
	state.historyIndex = 0;
	state.isKeyboardNavigating = false;
};

// 选中菜单项：记录历史并跳转（外链新窗口打开）
// 先关弹窗待退出动画结束后再跳转：同步跳转会打断弹窗的退出过渡，导致遮罩残留
const onHandleSelect = (item: RouteItem) => {
	addHistory(item);
	const navigate = () => {
		if (item.meta?.isLink && !item.meta?.isIframe) window.open(item.meta?.isLink);
		else if (item.redirect) router.push(item.redirect);
		else router.push(item.path);
	};
	if (item.meta?.isLink && !item.meta?.isIframe) {
		navigate();
		state.isShowSearch = false;
	} else {
		state.isShowSearch = false;
		setTimeout(navigate, 220);
	}
};

// 历史记录：统一持久化入口
const saveHistory = () => Local.set(historyStorageKey, state.historyList);

const addHistory = (item: RouteItem) => {
	state.historyList = state.historyList.filter((v: RouteItem) => v.path !== item.path);
	if (state.historyList.length >= historyMaxLength) state.historyList.pop();
	state.historyList.unshift({ ...item, children: [] });
	saveHistory();
};

const onDeleteHistory = (index: number) => {
	state.historyList.splice(index, 1);
	saveHistory();
};

const onClearHistory = () => {
	state.historyList = [];
	saveHistory();
};

// 高亮移动（循环）并让滚动条跟随
const moveHighlight = (step: number) => {
	const length = activeList.value.length;
	if (length === 0) return;
	activeIndex.value = (activeIndex.value + step + length) % length;
	scrollToHighlightedItem();
};

const scrollToHighlightedItem = () => {
	nextTick(() => {
		const scrollWrapper = searchResultScrollbarRef.value?.wrapRef;
		if (!scrollWrapper) return;
		const el = scrollWrapper.querySelector('.global-search-item.is-highlight') as HTMLElement | null;
		if (!el) return;
		const itemTop = el.offsetTop;
		const itemBottom = itemTop + el.offsetHeight;
		if (itemTop < scrollWrapper.scrollTop) {
			searchResultScrollbarRef.value?.setScrollTop(itemTop);
		} else if (itemBottom > scrollWrapper.scrollTop + scrollWrapper.clientHeight) {
			searchResultScrollbarRef.value?.setScrollTop(itemBottom - scrollWrapper.clientHeight);
		}
	});
};

// 键盘导航期间抑制 mouseenter 覆盖高亮
const onMouseenter = (index: number) => {
	if (!state.isKeyboardNavigating) activeIndex.value = index;
};

let navTimer: ReturnType<typeof setTimeout>;
const handleKeydown = (event: KeyboardEvent) => {
	if ((isMacOs ? event.metaKey : event.ctrlKey) && event.key.toLowerCase() === 'k') {
		event.preventDefault();
		openSearch();
		return;
	}
	if (!state.isShowSearch) return;
	const actions: Record<string, () => void> = {
		ArrowUp: () => moveHighlight(-1),
		ArrowDown: () => moveHighlight(1),
		Enter: () => {
			const item = activeList.value[activeIndex.value];
			if (item) onHandleSelect(item);
		},
		Escape: () => (state.isShowSearch = false),
	};
	const action = actions[event.key];
	if (action) {
		event.preventDefault();
		if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
			state.isKeyboardNavigating = true;
			clearTimeout(navTimer);
			navTimer = setTimeout(() => (state.isKeyboardNavigating = false), 100);
		}
		action();
	}
};

// 键盘监听必须与卸载配对，避免切换语言重挂载后重复注册
onMounted(() => document.addEventListener('keydown', handleKeydown));
onUnmounted(() => document.removeEventListener('keydown', handleKeydown));

defineExpose({
	openSearch,
});
</script>

<style scoped lang="scss">
// 触发按钮：大屏为胶囊（放大镜 + 搜索 + 快捷键徽标），小屏缩为裸图标（对齐顶栏其他图标风格）
.global-search-trigger {
	display: flex;
	align-items: center;
	// 小屏缩为裸图标时容器定宽 32px，缺少水平居中会让放大镜贴到左边缘
	justify-content: center;
	height: 32px;
	padding: 0 6px;
	margin-right: 10px;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	user-select: none;
	white-space: nowrap;
	flex-shrink: 0;
	background-color: var(--el-fill-color-light);
	border-radius: 16px;
	transition:
		background-color 0.2s,
		color 0.2s;

	&:hover {
		background-color: var(--el-fill-color);
		color: var(--el-text-color-primary);

		.search-kbd {
			color: var(--el-text-color-primary);
		}
	}

	.search-icon {
		font-size: 14px;
	}

	.search-text {
		margin: 0px 10px;
		font-size: 13px;
	}

	.search-kbd {
		padding: 1px 6px;
		font-size: 12px;
		line-height: 18px;
		color: var(--el-text-color-secondary);
		background-color: var(--el-bg-color);
		border-radius: 4px 12px 12px 4px;
		transition: color 0.2s;
	}

	// 小屏（<768px）缩成一个纯图标，样式对齐其他裸图标
	@media (max-width: 767px) {
		height: 32px;
		width: 32px;
		padding: 0;
		margin: 0 1px;
		color: var(--el-text-color-regular);
		background-color: transparent;
		border-radius: 8px;

		&:hover {
			color: var(--el-color-primary);
			background-color: var(--next-color-user-hover);
		}

		.search-text,
		.search-kbd {
			display: none;
		}

		.search-icon {
			margin-right: 0;
			font-size: 16px;
		}
	}
}

// 快捷键徽标（输入框后缀与底部提示条共用）
.global-search-kbd {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-width: 20px;
	height: 20px;
	padding: 0 4px;
	font-size: 11px;
	font-family: inherit;
	line-height: 1;
	color: var(--el-text-color-secondary);
	border: 1px solid var(--el-border-color);
	border-radius: 4px;
	box-shadow: 0 2px 0 var(--el-border-color-lighter);
}

.global-search-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	min-height: 44px;
	padding: 6px 12px;
	margin: 4px 0;
	font-size: 14px;
	color: var(--el-text-color-regular);
	cursor: pointer;
	border-radius: 6px;

	&:hover,
	&.is-highlight {
		background-color: var(--el-color-primary);
		color: #ffffff;

		.global-search-item-dir {
			color: rgba(255, 255, 255, 0.9);
		}

		.global-search-item-path {
			color: rgba(255, 255, 255, 0.75);
		}

		.global-search-item-dir + .global-search-item-path {
			border-left-color: rgba(255, 255, 255, 0.35);
		}
	}

	&-main {
		flex: 1;
		min-width: 0;
	}

	&-title {
		display: flex;
		align-items: center;
		overflow: hidden;
		white-space: nowrap;
		text-overflow: ellipsis;
	}

	&-sub {
		display: flex;
		align-items: center;
		min-width: 0;
		margin-top: 2px;
		font-size: 12px;
		white-space: nowrap;
		overflow: hidden;
	}

	&-dir {
		min-width: 0;
		overflow: hidden;
		color: var(--el-text-color-secondary);
		text-overflow: ellipsis;
	}

	&-path {
		min-width: 0;
		overflow: hidden;
		color: var(--el-text-color-placeholder);
		text-overflow: ellipsis;
	}

	// 竖线挂在相邻选择器上：没有父级链的行不会出现一根孤零零的分隔线
	&-dir + &-path {
		margin-left: 6px;
		padding-left: 6px;
		border-left: 1px solid var(--el-border-color);
	}

	&-fav {
		flex-shrink: 0;
		margin-left: 8px;
		/* el-icon 把自身锁成 1em，padding 撑不开命中区，必须显式给宽高 */
		width: 32px;
		height: 32px;
		font-size: 16px;
		border-radius: 6px;
		color: var(--el-text-color-placeholder);

		&:hover,
		&.is-picked {
			color: var(--el-color-warning);
		}

		&:hover {
			background: var(--el-fill-color);
		}
	}

	&.is-highlight .global-search-item-fav {
		color: rgba(255, 255, 255, 0.65);

		/* 高亮行底色是主题色，通用 fill-color 悬停底会发灰，改用半透明白 */
		&:hover {
			color: var(--el-color-warning);
			background: rgba(255, 255, 255, 0.18);
		}

		&.is-picked {
			color: var(--el-color-warning);
		}
	}

	&-delete {
		flex-shrink: 0;
		margin-left: 8px;
		color: var(--el-text-color-secondary);

		&:hover {
			color: var(--el-color-danger);
		}
	}

	&.is-highlight .global-search-item-delete {
		color: #ffffff;

		&:hover {
			color: var(--el-color-danger);
		}
	}
}

.global-search-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 40px 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);

	.el-icon {
		font-size: 28px;
		margin-bottom: 8px;
	}
}

.global-search-history-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0 12px;
}

.global-search-history-label {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.global-search-history-clear {
	font-size: 12px;
	color: var(--el-color-primary);
	cursor: pointer;

	&:hover {
		opacity: 0.8;
	}
}

.global-search-footer {
	display: flex;
	align-items: center;
	justify-content: center;

	&-item {
		display: flex;
		align-items: center;
		margin: 0 8px;
		font-size: 12px;
		color: var(--el-text-color-regular);

		.global-search-kbd {
			margin-right: 4px;
		}

		.global-search-kbd + .global-search-kbd {
			margin-left: 4px;
		}
	}
}
</style>

<style lang="scss">
// 弹窗动画与布局。非 scoped：弹层会被 Teleport 到 body 下，scoped 选择器与 @keyframes 重命名都无法命中。
@keyframes global-search-dialog-in {
	from {
		opacity: 0;
		transform: translateY(-30px) scale(0.95);
	}
	to {
		opacity: 1;
		transform: translateY(0) scale(1);
	}
}

@keyframes global-search-dialog-out {
	from {
		opacity: 1;
		transform: translateY(0) scale(1);
	}
	to {
		opacity: 0;
		transform: translateY(-30px) scale(0.95);
	}
}

// 根选择器为 modal-class（挂在 body 下的 .el-overlay 上），样式只作用于本弹窗。
// 全局 transition.scss/element.scss 对所有 dialog 有覆盖（bounce 动画、flex 居中、margin/padding !important），
// 因此以下部分属性需以同等或更高特异性 + !important 反制。
.el-overlay.layout-global-search-modal {
	background-color: rgba(0, 0, 0, 0.3);
	overflow: hidden;

	.el-overlay-dialog {
		align-items: flex-start !important;
		animation-duration: 0s !important;
	}

	.el-overlay-dialog .el-dialog {
		// 永久禁用全局 bounce 动画：过渡类移除后 animation-name 变化会再次触发动画，表现为"先出框、后动画"
		animation: none !important;
		display: flex;
		flex-direction: column;
		position: relative;
		margin: 10vh auto 50px !important;
		max-height: calc(100vh - 20px); // 旧内核兜底（不支持 dvh，如 Chormium 102）
		max-height: calc(100dvh - 20px);
		border-radius: 8px;
		padding: 16px 16px 12px !important;

		.el-dialog__header {
			padding: 0;
			display: none;
		}

		.el-dialog__body {
			padding: 0 !important;
		}

		.el-dialog__footer {
			padding: 12px 0 0;
			border-top: 1px solid var(--el-border-color-lighter);
		}
	}

	.el-input__wrapper {
		border-radius: 6px;
	}

	.el-input__inner {
		height: 36px;
	}

	// 内容区最小高度与"未找到相关菜单"空状态一致，避免结果少/无历史时弹窗高度跳变
	.global-search-scrollbar {
		min-height: 186px;
	}

	// 进场：遮罩淡入 + 内容 fade/zoom/滑入
	&.dialog-fade-enter-active {
		animation-duration: 0.2s;

		.el-overlay-dialog {
			animation: global-search-dialog-in 0.3s cubic-bezier(0.16, 1, 0.3, 1) !important;
		}
	}

	// 出场：内容 fade/zoom/滑出
	&.dialog-fade-leave-active {
		animation-duration: 0.2s;

		.el-overlay-dialog {
			animation: global-search-dialog-out 0.2s cubic-bezier(0.4, 0, 1, 1) !important;
		}
	}
}
</style>
