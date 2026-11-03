<template>
	<div class="icon-selector-warp-row">
		<el-scrollbar ref="selectorScrollbarRef" @scroll="onScroll">
			<el-row :gutter="10" v-if="props.list.length > 0">
				<el-col :xs="6" :sm="4" :md="4" :lg="4" :xl="4" v-for="(v, k) in visibleList" :key="k" @click="onColClick(v)">
					<div class="icon-selector-warp-item" :class="{ 'icon-selector-active': prefix === v }">
						<SvgIcon :name="v" />
					</div>
				</el-col>
			</el-row>
			<el-empty :image-size="100" v-if="list.length <= 0" :description="empty"></el-empty>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts" name="iconSelectorList">
import { computed, ref, watch } from 'vue';
// 定义父组件传过来的值
const props = defineProps({
	// 图标列表数据
	list: {
		type: Array,
		default: () => [],
	},
	// 自定义空状态描述文字
	empty: {
		type: String,
		default: () => '无相关图标',
	},
	// 高亮当前选中图标
	prefix: {
		type: String,
		default: () => '',
	},
});

// 定义子组件向父组件传值/事件
const emit = defineEmits(['get-icon']);

// 当前 icon 图标点击时
const onColClick = (v: unknown | string) => {
	emit('get-icon', v);
};

// 增量渲染：remixicon 有 3200+ 个图标，一次性全部创建组件会明显卡顿。
// 这里首屏只渲染 pageSize 个，滚动到列表底部时再追加一批。
const pageSize = 120;
// 距底部多少 px 就开始追加，避免滚到底才加载出现明显停顿
const loadMoreDistance = 150;
const selectorScrollbarRef = ref();
const visibleCount = ref(pageSize);
const visibleList = computed(() => props.list.slice(0, visibleCount.value));
// el-scrollbar 内部真正滚动的元素（组件已 expose wrapRef，取不到时再从 DOM 兜底）
const getScrollWrap = (): HTMLElement | null => {
	const ins: any = selectorScrollbarRef.value;
	if (!ins) return null;
	const wrap = ins.wrapRef ?? ins.wrapElement ?? ins.$el?.querySelector?.('.el-scrollbar__wrap');
	return wrap?.$el ?? wrap ?? null;
};
// 追加下一批
const loadMore = () => {
	if (visibleCount.value >= props.list.length) return;
	visibleCount.value = Math.min(visibleCount.value + pageSize, props.list.length);
};
// 用滚动事件判定，不依赖挂载时机：列表数据是异步到达的，挂载时可能还是空数组
const onScroll = () => {
	const wrap = getScrollWrap();
	if (!wrap) return;
	if (wrap.scrollTop + wrap.clientHeight >= wrap.scrollHeight - loadMoreDistance) loadMore();
};
// 切换 tab / 搜索后列表已变化，回到首屏数量重新增量加载
watch(
	() => props.list,
	() => {
		visibleCount.value = pageSize;
	}
);
</script>

<style scoped lang="scss">
.icon-selector-warp-row {
	height: 230px;
	overflow: hidden;
	.el-row {
		padding: 15px;
	}
	.el-scrollbar__bar.is-horizontal {
		display: none;
	}
	.icon-selector-warp-item {
		display: flex;
		justify-content: center;
		align-items: center;
		border: 1px solid var(--el-border-color);
		border-radius: 5px;
		margin-bottom: 10px;
		height: 30px;
		i {
			font-size: 20px;
			color: var(--el-text-color-regular);
		}
		&:hover {
			cursor: pointer;
			background-color: var(--el-color-primary-light-9);
			border: 1px solid var(--el-color-primary-light-5);
			i {
				color: var(--el-color-primary);
			}
		}
	}
	.icon-selector-active {
		background-color: var(--el-color-primary-light-9);
		border: 1px solid var(--el-color-primary-light-5);
		i {
			color: var(--el-color-primary);
		}
	}
}
</style>
