<template>
	<div class="task-container">
		<!-- 上：待办任务主卡片（左文案 + 右环形） -->
		<div class="task-main-card" @click="goToPendingTasks">
			<div class="main-card-info">
				<div class="main-card-header">
					<el-icon><ele-Edit /></el-icon>
					<span class="title">待办任务</span>
				</div>
				<div class="main-card-num">{{ counts.todoCount }}</div>
				<div class="main-card-hint">点击进入待办列表</div>
			</div>
			<div class="progress-ring">
				<div ref="pieChartRef" class="pie-chart"></div>
			</div>
		</div>

		<!-- 下：三项统计并排（与「系统数据」同一套指标块形态） -->
		<div class="task-stats-container">
			<div class="stat-card" :style="{ '--tint': 'var(--el-color-success)' }" @click="goToDoneTasks">
				<div class="stat-head">
					<span class="icon-wrapper"
						><el-icon><ele-CircleCheck /></el-icon
					></span>
					<span class="stat-num">{{ counts.doneCount }}</span>
				</div>
				<span class="stat-label">已完成</span>
			</div>

			<div class="stat-card" :style="{ '--tint': 'var(--el-color-warning)' }" @click="goToCcTasks">
				<div class="stat-head">
					<span class="icon-wrapper"
						><el-icon><ele-DocumentCopy /></el-icon
					></span>
					<span class="stat-num">{{ counts.ccCount }}</span>
				</div>
				<span class="stat-label">抄送我的</span>
			</div>

			<div class="stat-card" :style="{ '--tint': 'var(--el-color-primary)' }" @click="goToStartedTasks">
				<div class="stat-head">
					<span class="icon-wrapper"
						><el-icon><ele-Position /></el-icon
					></span>
					<span class="stat-num">{{ counts.startedCount }}</span>
				</div>
				<span class="stat-label">我已发起</span>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
export default {
	title: '我的任务',
	icon: 'ele-Promotion',
	description: '我的任务',
};
</script>

<!-- 我的任务 -->
<script setup lang="ts" name="task">
import { onMounted, onBeforeUnmount, ref, nextTick } from 'vue';
import * as echarts from 'echarts';
import { useRouter } from 'vue-router';
import { GetTodoStats } from '/@/api/workflow';

const router = useRouter();

// 各 Tab 的数量统计
const counts = ref({
	todoCount: 0, // 待办
	doneCount: 0, // 已完成
	ccCount: 0, // 抄送
	startedCount: 0, // 已发起
});

// 饼图实例
const pieChartRef = ref<HTMLElement | null>(null);
let pieChart: echarts.ECharts | null = null;

// 读取 CSS 变量
const getCssVar = (name: string) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

// 绘制环形饼图（颜色跟随主题）
const drawPieChart = () => {
	if (!pieChartRef.value) return;
	if (!pieChart) {
		pieChart = echarts.init(pieChartRef.value);
	}

	const todo = counts.value.todoCount || 0;
	const done = counts.value.doneCount || 0;

	pieChart.setOption({
		tooltip: {
			trigger: 'item',
		},
		// 环形只有 96px，图例会盖住图形，占比信息由左侧数字承担
		legend: { show: false },
		series: [
			{
				type: 'pie',
				radius: ['56%', '88%'],
				center: ['50%', '50%'],
				avoidLabelOverlap: false,
				itemStyle: {
					borderRadius: 5,
					borderWidth: 2,
					borderColor: getCssVar('--el-color-primary'),
				},
				label: {
					show: false,
					position: 'center',
				},
				emphasis: {
					label: {
						show: false,
						fontSize: 40,
						fontWeight: 'var(--el-color-primary)',
					},
				},
				labelLine: {
					show: false,
				},
				data: [
					// 主卡是主色底，环形图用白色系两档，避免再引入一个和主题无关的撞色
					{ value: todo, name: '待办', itemStyle: { color: '#ffffff' } },
					{ value: done, name: '已完成', itemStyle: { color: 'rgba(255, 255, 255, 0.35)' } },
				],
			},
		],
	});
};

// 主题切换时重绘
const handleThemeChange = () => {
	nextTick(() => drawPieChart());
};

// 获取统计数据
const getTaskList = async () => {
	const res = await GetTodoStats({});
	counts.value.todoCount = res.data.result.todoCount;
	counts.value.doneCount = res.data.result.doneCount;
	counts.value.ccCount = res.data.result.ccCount;
	counts.value.startedCount = res.data.result.startedCount;

	// 数据拿到后重绘
	nextTick(() => drawPieChart());
};

// 跳转到待办任务页面
const goToPendingTasks = () => {
	router.push('/workFlow/pendingTasks');
};

// 跳转到已完成任务页面
const goToDoneTasks = () => {
	router.push('/workFlow/completedTasks');
};

// 跳转到抄送我的页面
const goToCcTasks = () => {
	router.push('/workFlow/ccTasks');
};

// 跳转到已发起任务页面
const goToStartedTasks = () => {
	router.push('/workFlow/myInitiatedTasks');
};

// 窗口尺寸变化
const handleResize = () => {
	pieChart?.resize();
};

// 监听 <html> class 变化，用于主题切换时重绘
let observer: MutationObserver | null = null;

onMounted(() => {
	getTaskList();

	window.addEventListener('resize', handleResize);

	observer = new MutationObserver(() => {
		handleThemeChange();
	});
	observer.observe(document.documentElement, {
		attributes: true,
		attributeFilter: ['class'],
	});
});

onBeforeUnmount(() => {
	window.removeEventListener('resize', handleResize);
	observer?.disconnect();
	observer = null;
	pieChart?.dispose();
	pieChart = null;
});
</script>

<style lang="scss" scoped>
.task-container {
	display: flex;
	flex-direction: column;
	gap: 10px;
	width: 100%;
}

/* 主卡片：待办任务（与欢迎卡同一套主色渐变语言） */
.task-main-card {
	width: 100%;
	min-width: 0;
	padding: 14px 16px;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	/* 同 welcome-card：color-mix() 在 Chromium 102 下会让整条声明失效，改走 Element 色阶 */
	background: linear-gradient(135deg, var(--el-color-primary-dark-2) 0%, var(--el-color-primary) 50%, var(--el-color-primary-light-3) 100%);
	border-radius: 10px;
	color: var(--el-color-white);
	cursor: pointer;
	transition:
		transform 0.3s ease,
		box-shadow 0.3s ease;
	position: relative;
	overflow: hidden;

	&:hover {
		transform: translateY(-2px);
		box-shadow: var(--el-box-shadow-light);
	}

	.main-card-info {
		position: relative;
		z-index: 1;
		display: flex;
		flex-direction: column;
		gap: 4px;
		min-width: 0;
	}

	.main-card-header {
		display: flex;
		align-items: center;
		gap: 6px;

		.title {
			font-size: 13px;
			font-weight: 600;
			color: rgb(255 255 255 / 88%);
		}
	}

	.main-card-num {
		font-size: 30px;
		font-weight: 700;
		line-height: 1.1;
		color: #fff;
		font-variant-numeric: tabular-nums;
	}

	.main-card-hint {
		font-size: 12px;
		color: rgb(255 255 255 / 70%);
	}

	.progress-ring {
		flex: 0 0 auto;
		position: relative;
		z-index: 1;

		.pie-chart {
			width: 96px;
			height: 96px;
		}
	}
}

/* 暗色下大块主色实底会在深色页面里发亮：主色只取低比例压到页面底色上，
   既保住品牌色相，又让亮色主题色下的白字仍有足够对比度 */
[data-theme='dark'] .task-main-card {
	background:
		linear-gradient(135deg, rgb(0 0 0 / 58%) 0%, rgb(0 0 0 / 44%) 100%),
		linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary) 100%);
	background: linear-gradient(
		135deg,
		color-mix(in srgb, var(--el-color-primary) 34%, var(--el-bg-color)) 0%,
		color-mix(in srgb, var(--el-color-primary) 24%, var(--el-bg-color)) 100%
	);
}

.task-main-card::before,
.task-main-card::after {
	content: '';
	position: absolute;
	border-radius: 50%;
	background: rgb(255 255 255 / 5%);
	z-index: 0;
}

.task-main-card::before {
	width: 200px;
	height: 200px;
	left: 20%;
	top: -40%;
}

.task-main-card::after {
	width: 80px;
	height: 80px;
	left: 65%;
	bottom: 5%;
	background: rgb(255 255 255 / 4%);
}
/* 三项统计并排 */
.task-stats-container {
	display: flex;
	gap: 10px;

	> * {
		flex: 1 1 0;
		min-width: 0;
	}
}

.stat-card {
	position: relative;
	border-radius: 10px;
	padding: 10px 10px 9px 11px;
	display: flex;
	flex-direction: column;
	min-width: 0;
	cursor: pointer;
	background: var(--el-fill-color-light);
	transition:
		transform 0.2s,
		box-shadow 0.2s,
		background-color 0.2s;

	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 0;
		bottom: 0;
		width: 3px;
		background: var(--tint);
	}

	&:hover {
		transform: translateY(-2px);
		box-shadow: var(--el-box-shadow-light);
		background-color: var(--el-fill-color);
	}
}

.stat-head {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.stat-num {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 17px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	line-height: 1.15;
	font-variant-numeric: tabular-nums;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stat-label {
	display: block;
	margin-top: 6px;
	font-size: 12px;
	line-height: 1.3;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* 图标底衬：深色图标 + 浅色同色系圆角块 */
.icon-wrapper {
	flex: 0 0 auto;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
	color: var(--tint);
	background-color: var(--el-fill-color-light);
	@supports (background: color-mix(in srgb, red, blue)) {
		background-color: color-mix(in srgb, var(--tint) 12%, transparent);
	}
	transition: background-color 0.2s;
}

.stat-card:hover .icon-wrapper {
	background-color: var(--el-fill-color);
	@supports (background: color-mix(in srgb, red, blue)) {
		background-color: color-mix(in srgb, var(--tint) 19%, transparent);
	}
}

/* 老内核兜底：同 statsData，用伪元素还原 tint 的浅色底 */
@supports not (background: color-mix(in srgb, red, blue)) {
	.stat-card .icon-wrapper {
		position: relative;
		background-color: transparent;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			border-radius: inherit;
			background: var(--tint);
			opacity: 0.12;
		}

		> * {
			position: relative;
		}
	}

	.stat-card:hover .icon-wrapper::before {
		opacity: 0.19;
	}
}
</style>
