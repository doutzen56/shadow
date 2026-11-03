<template>
	<el-card class="visit-card" shadow="hover" :body-style="{ padding: '0' }">
		<template #header>
			<el-icon><ele-DataAnalysis /></el-icon>
			<span>访问趋势</span>
		</template>

		<div ref="chartRef" class="chart-container"></div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '访问趋势',
	icon: 'ele-TrendCharts',
	description: '访问趋势',
};
</script>

<!-- 访问趋势 -->
<script setup lang="ts" name="vislogChart">
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue';
import * as echarts from 'echarts';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogVisApi } from '/@/api-services/system/api';

const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;

// 原始接口数据（按天）
const rawData = ref<{ date: string; count: number }[]>([]);

// 主题色
const getCssVar = (name: string) => {
	if (typeof window === 'undefined') return '';
	return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
};

/**
 * 生成从当前月往前的 N 个月份列表（含当前月）
 * @param size 月数，默认 12
 * @returns ['2025-10', '2025-11', ..., '2026-09']
 */
const generateMonthList = (size = 12, baseDate = new Date()): string[] => {
	const list: string[] = [];
	const y = baseDate.getFullYear();
	const m = baseDate.getMonth();

	for (let i = 0; i < size; i++) {
		const d = new Date(y, m - (size - 1 - i), 1); // JS 自动处理跨年
		const yy = d.getFullYear();
		const mm = String(d.getMonth() + 1).padStart(2, '0');
		list.push(`${yy}-${mm}`);
	}
	return list;
};

/**
 * 把按天的数据聚合成按月的数据
 */
const aggregateByMonth = (data: { date: string; count: number }[], monthList: string[]): number[] => {
	const sumMap = new Map<string, number>();
	data.forEach((item) => {
		if (!item?.date) return;
		const key = item.date.slice(0, 7); // '2025-09-22' -> '2025-09'
		sumMap.set(key, (sumMap.get(key) || 0) + (item.count || 0));
	});
	return monthList.map((m) => sumMap.get(m) ?? 0);
};

/**
 * 把 'YYYY-MM' 格式化为 X 轴展示文本
 */
const formatMonthLabel = (ym: string, short = true): string => {
	const [y, m] = ym.split('-');
	if (short) return `${y.slice(2)}-${m}`;
	return `${y}年${parseInt(m)}月`;
};

/**
 * 根据原始数据刷新图表
 */
const updateChartData = () => {
	if (!chartInstance) return;

	const monthList = generateMonthList(12);
	const values = aggregateByMonth(rawData.value, monthList);
	const labels = monthList.map((m) => formatMonthLabel(m, true));

	chartInstance.setOption({
		xAxis: { data: labels },
		series: [{ data: values }],
	});
};
let themeObserver: MutationObserver | null = null;
const refreshTheme = () => {
	chartInstance?.dispose();
	nextTick(() => {
		initChart();
		updateChartData();
	});
};
/**
 * 初始化图表
 */
const initChart = () => {
	if (!chartRef.value) return;
	chartInstance = echarts.init(chartRef.value);

	// 主题切换时刷新 ECharts 配色
	const primaryColor = getCssVar('--el-color-primary');
	const borderLighter = getCssVar('--el-border-color-lighter');
	const option: echarts.EChartsOption = {
		grid: {
			left: '20',
			right: '20',
			top: '40',
			bottom: '0',
			containLabel: true,
		},
		tooltip: {
			trigger: 'axis',
			axisPointer: { type: 'line' },
			formatter: (params: any) => {
				const p = params[0];
				const [yy, mm] = p.name.split('-');
				const full = `20${yy}年${parseInt(mm)}月`;
				return `${full}<br/>访问量：${p.value}`;
			},
		},
		xAxis: {
			type: 'category',
			boundaryGap: false,
			data: [],
			axisTick: { show: false },
			axisLabel: { fontSize: 12, margin: 12 },
		},
		yAxis: {
			type: 'value',
			name: '访问量',
			nameTextStyle: {
				fontSize: 12,
				padding: [0, 0, 6, -50],
			},
			splitLine: {
				show: true,
				lineStyle: { color: borderLighter, type: 'dashed' },
			},
			axisLine: { show: false },
			axisTick: { show: false },
			axisLabel: { fontSize: 12, margin: 12 },
			min: 0,
			max: (value: { max: number }) => Math.max(10, Math.ceil((value.max * 1.2) / 5) * 5),
		},
		series: [
			{
				name: '访问量',
				type: 'line',
				smooth: true,
				symbol: 'circle',
				symbolSize: 8,
				showSymbol: true,
				itemStyle: {
					color: primaryColor,
					// borderColor: '#ffffff',
					borderWidth: 2,
				},
				lineStyle: {
					color: primaryColor,
					width: 2,
				},
				areaStyle: {
					// 渐变落到卡片底色，避免写死 rgba 导致暗色主题下发灰
					color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
						{ offset: 0, color: primaryColor },
						{ offset: 1, color: getCssVar('--el-bg-color') },
					]),
				},
				label: {
					show: true,
					position: 'top',
					fontSize: 12,
					formatter: (params: any) => params.value.toLocaleString(),
				},
				data: [],
			},
		],
	};

	chartInstance.setOption(option);
};

/**
 * 获取统计数据
 */
const getYearDayStatsData = async () => {
	try {
		const res: any = await getAPI(SysLogVisApi).apiSysLogVisYearDayStatsGet();

		const list = Array.isArray(res.data.result) ? res.data.result : (res.data.result?.items ?? []);
		rawData.value = list;

		updateChartData();
	} catch (err) {
		console.error('获取统计数据失败:', err);
		rawData.value = [];
		updateChartData();
	}
};

// 自适应
const resizeHandler = () => chartInstance?.resize();

onMounted(() => {
	// 1. 监听 html class 变化（Element Plus 暗黑模式 = html.dark）
	themeObserver = new MutationObserver(() => {
		// class 变化后等一帧，确保 CSS 变量已更新
		requestAnimationFrame(() => refreshTheme());
	});
	themeObserver.observe(document.documentElement, {
		attributes: true,
		attributeFilter: ['class'],
	});
	nextTick(async () => {
		initChart();
		await getYearDayStatsData();
		window.addEventListener('resize', resizeHandler);
	});
});

onBeforeUnmount(() => {
	themeObserver?.disconnect();
	themeObserver = null;
	window.removeEventListener('resize', resizeHandler);
	chartInstance?.dispose();
});
</script>

<style lang="scss" scoped>
.chart-container {
	width: 100%;
	height: var(--next-widget-height, 300px);
	padding: 6px;
	box-sizing: border-box;
}
</style>
