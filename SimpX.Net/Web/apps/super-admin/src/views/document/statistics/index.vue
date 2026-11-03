<template>
	<div class="document-statistics-container" v-loading="state.loading">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">统计报表</h2>
					<p class="page-hero__desc">从总量、增量、类型与分类四个维度盘点文档资产，帮助判断存储占用与分享活跃度。</p>
				</div>
			</div>
		</div>

		<!-- 统计卡片：数据驱动渲染，避免 6 段几乎相同的 markup -->
		<el-row :gutter="12" class="card-grid-row doc-stat-row">
			<el-col v-for="card in statCards" :key="card.key" :xs="24" :sm="12" :md="8" :lg="4" :xl="4">
				<el-card class="entity-card doc-stat-card" :class="card.accent" shadow="never">
					<div class="entity-card__head">
						<div class="entity-card__icon">
							<el-icon :size="18"><component :is="card.icon" /></el-icon>
						</div>
						<div class="entity-card__title-wrap">
							<div class="entity-card__title">{{ card.label }}</div>
							<div class="entity-card__sub">{{ card.hint }}</div>
						</div>
					</div>
					<div class="entity-card__body">
						<div class="doc-stat-body">
							<span class="doc-stat-value">{{ card.value }}</span>
							<span v-if="card.unit" class="doc-stat-label">{{ card.unit }}</span>
						</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 图表区域 -->
		<el-row :gutter="12" class="card-grid-row">
			<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
				<el-card shadow="hover" class="doc-panel">
					<template #header>
						<div class="doc-panel__header">
							<el-icon><ele-PieChart /></el-icon>
							<span>按类型统计</span>
						</div>
					</template>
					<scEcharts v-if="typeChartOption.series && typeChartOption.series.length > 0" height="360px" :option="typeChartOption" />
					<el-empty v-else :image-size="120" description="暂无类型数据" />
				</el-card>
			</el-col>

			<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
				<el-card shadow="hover" class="doc-panel">
					<template #header>
						<div class="doc-panel__header">
							<el-icon><ele-Histogram /></el-icon>
							<span>按分类统计</span>
						</div>
					</template>
					<scEcharts v-if="categoryChartOption.series && categoryChartOption.series.length > 0" height="360px" :option="categoryChartOption" />
					<el-empty v-else :image-size="120" description="暂无分类数据" />
				</el-card>
			</el-col>
		</el-row>

		<!-- 详细统计表格 -->
		<el-row :gutter="12" class="card-grid-row">
			<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
				<el-card shadow="hover" class="doc-panel">
					<template #header>
						<div class="doc-panel__header">
							<el-icon><ele-Menu /></el-icon>
							<span>类型统计详情</span>
						</div>
					</template>
					<vxe-grid v-bind="typeStatsGridOptions" :data="statistics.typeStatistics || []">
						<template #type_icon="{ row }">
							<div class="doc-thumb">
								<el-icon :size="20"><component :is="documentTypeIcon(row.documentType)" /></el-icon>
							</div>
						</template>
						<template #type_name="{ row }">
							{{ documentTypeText(row.documentType) }}
						</template>
						<template #type_size="{ row }">
							{{ formatFileSize(row.sizeKb) }}
						</template>
					</vxe-grid>
				</el-card>
			</el-col>

			<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
				<el-card shadow="hover" class="doc-panel">
					<template #header>
						<div class="doc-panel__header">
							<el-icon><ele-Files /></el-icon>
							<span>分类统计详情</span>
						</div>
					</template>
					<vxe-grid v-bind="categoryStatsGridOptions" :data="statistics.categoryStatistics || []">
						<template #cat_size="{ row }">
							{{ formatFileSize(row.sizeKb) }}
						</template>
					</vxe-grid>
				</el-card>
			</el-col>
		</el-row>
	</div>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi } from '/@/api-services/document/api';
import { reactive, ref, computed, onMounted, defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import { VxeGridProps } from 'vxe-table';
import '/@/views/document/styles/document.scss';
import { documentTypeText, documentTypeIcon } from '../utils/documentMeta';
import { formatFileSize } from '/@/utils/format';

const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

const state = reactive({
	loading: false,
});

// 类型统计详情表格
const typeStatsGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'icon', title: '类型', width: 80, slots: { default: 'type_icon' } },
		{ field: 'documentType', title: '文档类型', minWidth: 120, slots: { default: 'type_name' } },
		{ field: 'count', title: '数量', width: 100 },
		{ field: 'sizeKb', title: '大小', minWidth: 120, slots: { default: 'type_size' } },
	],
});

// 分类统计详情表格
const categoryStatsGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'categoryName', title: '分类名称', minWidth: 140, showOverflow: 'tooltip' },
		{ field: 'count', title: '数量', width: 100 },
		{ field: 'sizeKb', title: '大小', minWidth: 120, slots: { default: 'cat_size' } },
	],
});

const statistics = ref<any>({
	totalCount: 0,
	totalSizeKb: 0,
	totalSizeInfo: '0 B',
	todayUploadCount: 0,
	todayDownloadCount: 0,
	todayAccessCount: 0,
	totalShareCount: 0,
	recycleCount: 0,
	typeStatistics: [],
	categoryStatistics: [],
});

const typeChartOption = ref<any>({});
const categoryChartOption = ref<any>({});

// 从格式化的大小字符串中提取数值部分
const getSizeNumber = (sizeInfo: string): string => {
	if (!sizeInfo) return '0';
	const match = sizeInfo.match(/^([\d.]+)/);
	return match ? match[1] : '0';
};

// 从格式化的大小字符串中提取单位部分
const getSizeUnit = (sizeInfo: string): string => {
	if (!sizeInfo) return 'B';
	const match = sizeInfo.match(/\s*([A-Z]+)$/);
	return match ? match[1] : 'B';
};

// 统计卡片：统一走 .entity-card，配色只用主题语义色阶（跟随明暗主题），不用硬编码渐变
const statCards = computed(() => [
	{ key: 'totalCount', label: '文档总数', hint: '全部在库文档', icon: 'ele-Document', accent: 'is-primary', value: statistics.value.totalCount || 0, unit: '个' },
	{
		key: 'totalSize',
		label: '总大小',
		hint: '累计占用存储',
		icon: 'ele-FolderOpened',
		accent: 'is-success',
		value: getSizeNumber(statistics.value.totalSizeInfo),
		unit: getSizeUnit(statistics.value.totalSizeInfo),
	},
	{ key: 'todayUpload', label: '今日上传', hint: '当日新增', icon: 'ele-Upload', accent: 'is-warning', value: statistics.value.todayUploadCount || 0, unit: '个' },
	{ key: 'todayAccess', label: '今日访问', hint: '当日预览与分享访问', icon: 'ele-View', accent: 'is-info', value: statistics.value.todayAccessCount || 0, unit: '次' },
	{ key: 'totalShare', label: '分享数量', hint: '已生成链接', icon: 'ele-Share', accent: 'is-primary', value: statistics.value.totalShareCount || 0, unit: '个' },
	{ key: 'recycle', label: '回收站', hint: '待处理文档', icon: 'ele-Delete', accent: 'is-danger', value: statistics.value.recycleCount || 0, unit: '个' },
]);

const loadStatistics = async () => {
	state.loading = true;
	try {
		const res = await getAPI(DocumentApi).apiDocumentStatisticsGet();
		// API返回的数据结构：{ code, result, ... }，实际数据在 result 字段中
		statistics.value = res.data?.result || res.data || {};

		// 更新类型统计图表
		updateTypeChart();
		// 更新分类统计图表
		updateCategoryChart();
	} catch (error) {
		console.error('加载统计数据失败:', error);
		ElMessage.error('加载统计数据失败');
	} finally {
		state.loading = false;
	}
};

const updateTypeChart = () => {
	const typeStats = statistics.value.typeStatistics || [];
	if (typeStats.length === 0) {
		typeChartOption.value = {};
		return;
	}

	typeChartOption.value = {
		tooltip: {
			trigger: 'item',
			formatter: '{a} <br/>{b}: {c} ({d}%)',
		},
		legend: {
			orient: 'vertical',
			left: 'left',
			data: typeStats.map((item: any) => documentTypeText(item.documentType)),
		},
		series: [
			{
				name: '文档类型',
				type: 'pie',
				radius: ['40%', '70%'],
				avoidLabelOverlap: false,
				itemStyle: {
					borderRadius: 10,
					borderColor: '#fff',
					borderWidth: 2,
				},
				label: {
					show: true,
					formatter: '{b}: {c}\n({d}%)',
				},
				emphasis: {
					label: {
						show: true,
						fontSize: '16',
						fontWeight: 'bold',
					},
				},
				labelLine: {
					show: true,
				},
				data: typeStats.map((item: any) => ({
					value: item.count,
					name: documentTypeText(item.documentType),
				})),
			},
		],
	};
};

const updateCategoryChart = () => {
	const categoryStats = statistics.value.categoryStatistics || [];
	if (categoryStats.length === 0) {
		categoryChartOption.value = {};
		return;
	}

	// 只显示前10个分类
	const topCategories = categoryStats.slice(0, 10);

	categoryChartOption.value = {
		tooltip: {
			trigger: 'axis',
			axisPointer: {
				type: 'shadow',
			},
		},
		grid: {
			left: '3%',
			right: '4%',
			bottom: '3%',
			containLabel: true,
		},
		xAxis: {
			type: 'category',
			data: topCategories.map((item: any) => item.categoryName),
			axisLabel: {
				rotate: 45,
				interval: 0,
			},
		},
		yAxis: {
			type: 'value',
		},
		series: [
			{
				name: '文档数量',
				type: 'bar',
				data: topCategories.map((item: any) => item.count),
				itemStyle: {
					// 跟着主题主色走，暗色主题下不会是突兀的亮蓝
					color: 'var(--el-color-primary)',
				},
			},
		],
	};
};

onMounted(() => {
	loadStatistics();
});
</script>

<style lang="scss" scoped>
.document-statistics-container {
	/* 统计卡行 / 图表 / 明细面板之间的统一 12px 间距由根容器 gap 接管（见 document.scss） */

	/* 图表 / 明细面板：头部图标与标题基线对齐 */
	.doc-panel__header {
		display: flex;
		align-items: center;
		gap: 6px;
		font-weight: 600;
		font-size: 14px;
	}
}
</style>
