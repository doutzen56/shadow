<template>
	<el-card class="heatmap-card" shadow="hover" :body-style="{ padding: '0' }">
		<template #header>
			<el-icon><ele-ScaleToOriginal /></el-icon>
			<span>操作日志</span>
		</template>

		<div v-if="hasData" class="chart-container">
			<scEcharts :option="echartsOption" />
		</div>
		<div v-else class="w-empty">
			<span class="w-empty-ico"><el-icon><ele-DataLine /></el-icon></span>
			<span class="w-empty-text">暂无操作日志</span>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '操作日志',
	icon: 'ele-Grid',
	description: '操作日志',
};
</script>

<!-- 操作日志 -->
<script setup lang="ts" name="oplogChart">
import { ref, reactive, onMounted, defineAsyncComponent } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogOpApi } from '/@/api-services/system/api';
import { resolveCssColor } from '/@/utils/theme';

// 复用通用图表组件：主题切换、容器尺寸变化、初始化/销毁都由它统一处理
const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

// 读取 CSS 变量
const cssVar = (name: string) => resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue(name).trim());
// 获取主题颜色变量：必须在切换主题后重新读取，缓存成常量会一直停留在首次加载时的配色
const getThemeColors = () =>
	['--el-color-primary-light-9', '--el-color-primary-light-7', '--el-color-primary-light-5', '--el-color-primary-light-3', '--el-color-primary-light-1', '--el-color-primary'].map(
		(variable) => cssVar(variable)
	);
const echartsOption = ref({
	title: {
		top: 30,
		left: 'center',
		text: '日志统计',
		show: false,
	},
	tooltip: {
		formatter: function (p: any) {
			return p.data[0] + ' 数据量：' + p.data[1];
		},
	},
	visualMap: {
		show: true,
		inRange: {
			color: getThemeColors(),
		},
		outOfRange: {
			color: ['transparent'],
		},
		min: 0.01,
		max: 1000,
		maxOpen: {
			type: 'piecewise',
		},
		type: 'piecewise',
		orient: 'horizontal',
		left: 'right',
	},
	calendar: {
		top: 30,
		left: 30,
		right: 30,
		bottom: 30,
		cellSize: ['auto', 20],
		range: ['', ''],
		splitLine: true,
		dayLabel: {
			firstDay: 1,
			nameMap: 'ZH',
		},
		itemStyle: {
			// 空白格子的底色：浅色→浅灰，深色→深灰
			color: cssVar('--el-fill-color-light'),
			borderWidth: 2,
			// 格子之间的"缝"：用卡片背景色，浅色是白、深色是黑
			borderColor: cssVar('--el-bg-color'),
		},
		monthLabel: {
			nameMap: ['一月', '二月', '三月', '四月', '五月', '六月', '七月', '八月', '九月', '十月', '十一月', '十二月'],
		},
		yearLabel: {
			show: false,
		},
	},
	series: {
		type: 'heatmap',
		coordinateSystem: 'calendar',
		data: [],
	},
});

// 有没有日历热力数据：没有就不挂图表，改成空态（避免 data[0] 取空数组报错留下白板）
const hasData = ref(false);
const state = reactive({
	logMaxValue: 1,
});

onMounted(async () => {
	let data = [] as any;
	var res = await getAPI(SysLogOpApi).apiSysLogOpYearDayStatsGet();
	res.data.result?.forEach((item: any) => {
		data.push([item.date, item.count]);

		if (item.count > state.logMaxValue) state.logMaxValue = item.count; // 计算最大值
	});

	if (data.length === 0) return;

	echartsOption.value.visualMap.max = state.logMaxValue;
	echartsOption.value.series.data = data;
	echartsOption.value.calendar.range = [data[0][0], data[data.length - 1][0]];
	hasData.value = true;
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
