<template>
	<!-- 骨架屏：按「工作台」真实布局占位（工具条 + 欢迎卡 + 两行部件卡），高度撑满视口 -->
	<div v-if="state.pageLoading" class="home-skeleton">
		<div class="home-skeleton-toolbar">
			<el-skeleton animated>
				<template #template>
					<el-skeleton-item variant="text" style="width: 96px; height: 16px" />
				</template>
			</el-skeleton>
			<el-skeleton-item variant="button" style="width: 88px" />
		</div>
		<div class="home-skeleton-body">
			<el-card v-for="i in 7" :key="i" shadow="never" :class="['home-skeleton-card', { 'is-wide': i === 1 }]">
				<el-skeleton animated>
					<template #template>
						<div class="home-skeleton-card-head">
							<el-skeleton-item variant="circle" style="width: 30px; height: 30px" />
							<el-skeleton-item variant="text" style="width: 42%" />
						</div>
						<el-skeleton-item
							v-for="n in (i === 1 ? 3 : 5)"
							:key="n"
							variant="text"
							:style="{ width: n % 3 === 0 ? '64%' : '100%' }"
						/>
					</template>
				</el-skeleton>
			</el-card>
		</div>
	</div>
	<div v-else>
		<widgets></widgets>
	</div>
</template>

<script setup lang="ts" name="homePage">
import { defineAsyncComponent, onMounted, reactive } from 'vue';
// import { useHomepage } from 'src/stores/homepage';

const loadWidgets = () => import('./widgets/index.vue');

const widgets = defineAsyncComponent(loadWidgets);
// const homepageStore = useHomepage();

const state = reactive({
	name: 'dashboard',
	pageLoading: true,
	dashboard: '0',
});

onMounted(async () => {
	// 异步组件要等 v-else 分支渲染才触发 loader，这里先启动模块加载，否则骨架屏会同一个 tick 内消失留出空白
	await loadWidgets();
	state.pageLoading = false;
});
</script>

<style lang="scss" scoped>
/* 骨架屏布局语言与 widgets/index.vue 保持一致：工具条 + 8px 间距的栅格卡片，整体撑满可用高度 */
.home-skeleton {
	height: 100%;
	overflow: hidden;
	display: flex;
	flex-direction: column;
}
.home-skeleton-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 10px 5px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.home-skeleton-body {
	flex: 1;
	min-height: 0;
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	grid-auto-rows: minmax(0, 1fr);
	gap: 10px;
	/* 与 widgets/index.vue 的 .widgets-content 保持一致：左右只留 5px 页边距 */
	padding: 10px 5px;
}
.home-skeleton-card {
	border-radius: 10px; // 与工作台部件卡片圆角一致

	&.is-wide {
		grid-column: 1 / -1;
	}

	:deep(.el-card__body) {
		height: 100%;
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
		justify-content: space-evenly; // 大屏下骨架行随卡片高度均匀铺开
		gap: 10px;
		overflow: hidden;
	}
}
.home-skeleton-card-head {
	display: flex;
	align-items: center;
	gap: 10px;
}
</style>
