<template>
	<div class="visMap-container">
		<!-- ===== 弹窗模式 ===== -->
		<el-dialog v-if="props.useDialog" v-model="state.isShowDialog" draggable :close-on-click-modal="false" @opened="handleDialogOpened">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-MapLocation />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<!-- 地图容器：用动态 id，避免同页面多实例冲突 -->
			<div :id="mapId" class="visMap-map"></div>
		</el-dialog>

		<!-- ===== 内嵌模式：只渲染地图，不包 dialog ===== -->
		<div v-else :id="mapId" class="visMap-map"></div>
	</div>
</template>

<!-- 地图组件 -->
<script lang="ts" setup name="visMap">
import { nextTick, onBeforeUnmount, onMounted, reactive } from 'vue';

import 'ol/ol.css';
import Map from 'ol/Map';
import View from 'ol/View';
import OSM from 'ol/source/OSM';
import XYZ from 'ol/source/XYZ';
import TileWMS from 'ol/source/TileWMS';
import { Heatmap as HeatmapLayer, Tile as TileLayer } from 'ol/layer';
import Feature from 'ol/Feature';
import { Point } from 'ol/geom';
import VectorSource from 'ol/source/Vector';
import { transform } from 'ol/proj';

import gcoord from 'gcoord';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogVisApi } from '/@/api-services/system/api';

const emits = defineEmits(['coordinate']);

const props = defineProps({
	/** 弹窗标题（仅弹窗模式使用） */
	title: String,
	/** 拾取坐标模式：不加载热力图，点击地图后触发 coordinate 事件 */
	isPickupCoord: {
		type: Boolean,
		default: false,
	},
	/** 是否使用 el-dialog 包裹。false 时只渲染地图本体，供其他页面内嵌引用 */
	useDialog: {
		type: Boolean,
		default: true,
	},
});

const state = reactive({
	isShowDialog: false,
});

// 每个实例生成独立 id，避免同一页面多实例互相抢 DOM
const uid = Math.random().toString(36).slice(2, 8);
const mapId = `visMap-${uid}`;

// OpenLayers 地图实例（模块级闭包变量，非响应式）
let map: Map | undefined = undefined;

// 初始化地图
const initMap = () => {
	// 幂等：已初始化则直接返回，避免重复创建
	if (map) return;

	// OpenStreetMap 图层
	const osmLayer = new TileLayer({ source: new OSM() });

	// ArcGIS 地图
	const arcGISLayer = new TileLayer({
		source: new XYZ({
			url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}',
			crossOrigin: 'anonymous',
		}),
	});

	// WMS 图层 - Geoserver 服务
	const wmsLayer = new TileLayer({
		source: new TileWMS({
			url: 'https://your-geoserver-url/wms',
			params: {
				LAYERS: 'your-layer-name',
				TILED: true,
			},
		}),
	});

	// --- 高德地图 XYZ 瓦片源 ---
	// style=7：卫星图（带路网标注）
	const amapLayer1 = new TileLayer({
		source: new XYZ({
			url: 'http://webst0{1-4}.is.autonavi.com/appmaptile?style=7&x={x}&y={y}&z={z}&lang=zh_cn&scale=1&size=1',
		}),
	});
	// style=6：卫星影像（纯影像）
	const amapLayer2 = new TileLayer({
		source: new XYZ({
			url: 'http://webst0{1-4}.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={z}&lang=zh_cn&scale=1&size=1',
		}),
	});
	// style=8：道路图（矢量路网）
	const amapLayer3 = new TileLayer({
		source: new XYZ({
			url: 'http://webst0{1-4}.is.autonavi.com/appmaptile?style=8&x={x}&y={y}&z={z}&lang=zh_cn&scale=1&size=1',
		}),
	});

	// 初始中心点：WGS84 经纬度 → Web 墨卡托
	const gd = gcoord.transform(
		[104.88, 30.22], // 经纬度坐标
		gcoord.WGS84, // 当前坐标系
		gcoord.EPSG3857 // 目标坐标系
	);

	// 创建地图实例
	map = new Map({
		target: mapId,
		// 使用卫星影像 + 道路图叠加，视觉效果更好
		layers: [amapLayer2, amapLayer3],
		view: new View({
			// projection: 'EPSG:4326', // WGS84 坐标系
			projection: 'EPSG:3857', // Web 墨卡托坐标系
			center: gd, // 初始中心点
			zoom: 4, // 初始缩放级别
		}),
	});

	// 地图单击：拾取经纬度坐标（EPSG:3857 → EPSG:4326）
	map.on('singleclick', (e) => {
		const coordinate = transform(e.coordinate, 'EPSG:3857', 'EPSG:4326');
		emits('coordinate', coordinate);

		// 拾取坐标模式下，选完坐标自动关闭弹窗（内嵌模式下此赋值无副作用）
		if (props.isPickupCoord) state.isShowDialog = false;
	});

	// 非拾取坐标模式，加载登录日志热力图
	if (!props.isPickupCoord) {
		loadVidLog();
	}
};

// 加载热力图 - 登录日志数据
const loadVidLog = async () => {
	const res = await getAPI(SysLogVisApi).apiSysLogVisListGet();
	const pts = res.data.result ?? [];
	if (pts.length === 0) return;

	// 将经纬度点转换为 Web 墨卡托坐标的 Feature
	const features: Feature<Point>[] = [];
	pts.forEach((item: any) => {
		const gd = gcoord.transform([item.longitude, item.latitude], gcoord.WGS84, gcoord.EPSG3857);
		features.push(new Feature({ geometry: new Point(gd) }));
	});

	// 构造热力图层
	const heatmapVectorSource = new VectorSource({ features });
	const heatmapLayer = new HeatmapLayer({
		source: heatmapVectorSource,
		radius: 5, // 半径大小
		blur: 20, // 模糊程度
		// gradient: ['#00f', '#0ff', '#0f0', '#ff0', '#f00'], // 热力图颜色
	});
	map?.addLayer(heatmapLayer);
};

onMounted(() => {
	// 内嵌模式：DOM 已在挂载时存在，等 nextTick 后即可初始化
	// 弹窗模式：等 dialog 打开（@opened）再初始化，见 handleDialogOpened
	if (!props.useDialog) {
		nextTick(() => initMap());
	}
});

onBeforeUnmount(() => {
	// 释放地图实例，避免内存泄漏和残留事件监听
	if (map) {
		map.setTarget(undefined);
		map.dispose?.();
		map = undefined;
	}
});

// ============================================================
// 弹窗相关
// ============================================================
// el-dialog 默认懒渲染，v-model=true 时 body 才插入 DOM，
// 且需要等打开动画结束（@opened）后再 init，否则容器尺寸为 0，地图显示异常。
const handleDialogOpened = () => {
	nextTick(() => initMap());
};

/**
 * 打开弹窗（对外 API）
 * - 弹窗模式：显示 dialog，@opened 后自动初始化地图
 * - 内嵌模式：无弹窗概念，直接确保地图已初始化
 */
const openDialog = () => {
	if (!props.useDialog) {
		nextTick(() => initMap());
		return;
	}
	state.isShowDialog = true;
};

// 导出对象
defineExpose({
	/** 打开弹窗（弹窗模式）/ 确保初始化（内嵌模式） */
	openDialog,
	/** 手动初始化地图（autoInit 场景外的补充入口） */
	init: () => nextTick(() => initMap()),
	/** 获取内部 ol/Map 实例，便于外部自定义图层操作 */
	getMap: () => map,
});
</script>

<style scoped lang="scss">
/* dialog body 去掉内边距与背景，让地图铺满 */
:deep(.el-overlay .el-overlay-dialog .el-dialog__body) {
	height: 800px;
	padding: 0 !important;
	background-color: transparent !important;
}
:deep(.el-dialog) {
	--el-dialog-padding-primary: 0px;
}
:deep(.el-dialog__header) {
	margin: 0 !important;
}

/* 地图容器 */
.visMap-map {
	width: 100%;
	height: 100%;
	background-color: transparent !important;
}
</style>
