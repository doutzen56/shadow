<template>
	<div ref="scEcharts" :style="{ height: height, width: width }"></div>
</template>

<script>
import * as echarts from 'echarts';
import T from './echarts-theme-T.js';
import { resolveCssColor } from '/@/utils/theme';

echarts.registerTheme('T', T);
const unwarp = (obj) => obj && (obj.__v_raw || obj.valueOf() || obj);

export default {
	...echarts,
	name: 'scEcharts',
	props: {
		height: { type: String, default: '100%' },
		width: { type: String, default: '100%' },
		nodata: { type: Boolean, default: false },
		option: {
			type: Object,
			default: () => {},
		},
	},
	data() {
		return {
			isActivat: false,
			myChart: null,
			themeObserver: null,
			sizeObserver: null,
		};
	},
	watch: {
		option: {
			deep: true,
			handler(v) {
				// 组件刚挂载、draw() 还在 nextTick 里排队时 myChart 仍为 null，
				// 此时直接 setOption 会抛 "unwarp(...) is null"（缓存管理页 v-if 首次渲染就会命中）。
				// 未初始化直接跳过即可：draw() 里会用最新的 option 初始化。
				const chart = unwarp(this.myChart);
				if (!chart) return;
				this.$nextTick(() => {
					chart.setOption(v);
				});
			},
		},
	},
	computed: {
		myOptions: function () {
			return this.option || {};
		},
	},
	activated() {
		// 首次挂载时 activated 可能早于 draw()（keep-alive 场景），此时 myChart 还没建出来
		if (!this.isActivat) {
			this.$nextTick(() => {
				this.myChart?.resize();
			});
		}
	},
	deactivated() {
		this.isActivat = false;
	},
	mounted() {
		this.isActivat = true;
		this.themeObserver = new MutationObserver(() => {
			requestAnimationFrame(() => this.refreshTheme());
		});
		this.themeObserver.observe(document.documentElement, {
			attributes: true,
			attributeFilter: ['class'],
		});
		// 容器尺寸变化（分栏拖动、抽屉展开、父级高度塌陷后恢复）时：未初始化就补一次初始化，已初始化则 resize
		if (typeof ResizeObserver !== 'undefined') {
			this.sizeObserver = new ResizeObserver(() => {
				if (this.myChart) this.myChart.resize();
				else this.draw();
			});
			this.sizeObserver.observe(this.$refs.scEcharts);
		}
		this.$nextTick(() => {
			this.draw();
		});
	},
	// ⚠️ Vue 3 的卸载钩子叫 beforeUnmount（beforeDestroy 是 Vue 2 的名字，写了不会执行）
	// 原先写 beforeDestroy 导致：MutationObserver 不断开、resize 监听不移除、图表不 dispose（内存泄漏）。
	beforeUnmount() {
		this.themeObserver?.disconnect();
		this.themeObserver = null;
		this.sizeObserver?.disconnect();
		this.sizeObserver = null;
		window.removeEventListener('resize', this.handleResize);
		this.myChart?.dispose?.();
		this.myChart = null;
	},
	methods: {
		getCssVar(name) {
			return resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue(name).trim());
		},
		getThemeColors() {
			return ['--el-color-primary-light-9', '--el-color-primary-light-7', '--el-color-primary-light-5', '--el-color-primary-light-3', '--el-color-primary-light-1', '--el-color-primary'].map(
				(variable) => this.getCssVar(variable)
			);
		},
		handleResize() {
			this.myChart?.resize();
		},
		refreshTheme() {
			if (!this.myChart) return;

			const option = { ...this.myOptions };
			if (option.visualMap) {
				option.visualMap = {
					...option.visualMap,
					inRange: {
						...option.visualMap.inRange,
						color: this.getThemeColors(),
					},
				};
			}
			if (option.calendar) {
				option.calendar = {
					...option.calendar,
					itemStyle: {
						...option.calendar.itemStyle,
						color: this.getCssVar('--el-fill-color-light'),
						borderColor: this.getCssVar('--el-bg-color'),
					},
				};
			}

			this.myChart.setOption(option, true);
			this.myChart.resize();
		},
		draw() {
			const el = this.$refs.scEcharts;
			// 容器可能因为 v-if / 布局未就绪拿不到，直接放弃（watch 与 ResizeObserver 里有兜底）
			if (!el) return;
			// ⚠️ 容器宽高为 0 时绝不能 init：ECharts 会算出不可逆的坐标变换矩阵，
			// 后续 setOption 在 View 里做矩阵求逆得到 null，抛 "can't access property 0, m is null"
			// （缓存管理页分栏高度塌陷、抽屉/标签页未展开时都会命中）。等 ResizeObserver 拿到尺寸后再画。
			if (!el.clientWidth || !el.clientHeight) return;
			// 避免同一个 DOM 上重复 init 出多个实例
			this.myChart?.dispose?.();
			let myChart = echarts.init(this.$refs.scEcharts, 'T');
			myChart.setOption(this.myOptions);
			this.myChart = myChart;

			myChart.on('click', (evt) => {
				this.$emit('clickData', evt.value);
			});

			// 先移除再添加，避免重复 draw 时叠加多个监听
			window.removeEventListener('resize', this.handleResize);
			window.addEventListener('resize', this.handleResize);
		},
	},
};
</script>
