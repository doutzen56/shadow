<template>
	<div class="ca-page">
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
					<h2 class="page-hero__title">缓存管理</h2>
					<p class="page-hero__desc">按分组浏览系统缓存键：左侧选键、中间看值、右侧看结构关系图，支持删除单个键或清空全部缓存。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="分组与键数量按当前已加载数据统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statGroups }}</span>
							<span class="page-hero__stat-k">缓存分组</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statKeys }}</span>
							<span class="page-hero__stat-k">缓存键</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on is-key" :title="state.cacheKey ?? '未选择'">{{ state.cacheKey ?? '未选择' }}</span>
							<span class="page-hero__stat-k">当前键</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 风险提示：删除与清空均不可恢复 -->
		<div class="ca-notice">
			<el-icon class="ca-notice-ico"><ele-WarningFilled /></el-icon>
			<span class="ca-notice-text">缓存数据直接影响系统运行，删除与清空操作不可恢复，请谨慎执行。</span>
		</div>

		<splitpanes class="default-theme ca-panes">
			<pane size="20" min-size="12">
				<el-card shadow="hover" class="ca-pane-card" v-loading="state.loading">
					<template #header>
						<div class="ca-head">
							<span class="ca-head-ico is-blue"
								><el-icon><ele-Menu /></el-icon
							></span>
							<span class="ca-head-title">缓存列表</span>
							<span class="ca-head-count">{{ statKeys }}</span>
							<div class="ca-head-acts">
								<el-tooltip content="刷新缓存列表" placement="bottom">
									<el-button icon="ele-Refresh" size="small" type="success" circle plain @click="handleQuery" v-auth="'sysCache/keyList'" />
								</el-tooltip>
								<el-tooltip content="清空所有缓存" placement="bottom">
									<el-button icon="ele-DeleteFilled" size="small" type="danger" circle plain @click="clearCache" v-auth="'sysCache/clear'" />
								</el-tooltip>
							</div>
						</div>
					</template>
					<el-tree
						ref="treeRef"
						class="ca-tree"
						:data="state.cacheData"
						node-key="id"
						:props="{ children: 'children', label: 'name' }"
						@node-click="nodeClick"
						:default-checked-keys="state.cacheData"
						highlight-current
						check-strictly
						default-expand-all
						accordion
					>
						<template #default="{ data }">
							<span class="ca-node">
								<el-icon class="ca-node-ico" :class="data.id === 0 ? 'is-group' : ''">
									<ele-FolderOpened v-if="data.id === 0" />
									<ele-Key v-else />
								</el-icon>
								<span class="ca-node-name" :title="data.name">{{ data.name }}</span>
								<span v-if="data.children?.length" class="ca-node-count">{{ data.children.length }}</span>
							</span>
						</template>
					</el-tree>
				</el-card>
			</pane>

			<pane size="56" min-size="20">
				<el-card shadow="hover" class="ca-pane-card" v-loading="state.loading1">
					<template #header>
						<div class="ca-head">
							<span class="ca-head-ico is-green"
								><el-icon><ele-Document /></el-icon
							></span>
							<span class="ca-head-title">缓存数据</span>
							<span v-if="state.cacheKey" class="ca-head-key" :title="state.cacheKey">{{ state.cacheKey }}</span>
							<span v-if="valueMeta" class="ca-head-meta">{{ valueMeta }}</span>
							<div class="ca-head-acts">
								<el-button icon="ele-Delete" size="small" type="danger" plain :disabled="!state.cacheKey" @click="delCache" v-auth="'sysCache/delete'"> 删除缓存 </el-button>
							</div>
						</div>
					</template>
					<div class="ca-json">
						<vue-json-pretty v-if="state.cacheValue !== undefined" :data="state.cacheValue" showLength showIcon showLineNumber showSelectController />
						<el-empty v-else description="点击左侧缓存键查看缓存值" :image-size="72" />
					</div>
				</el-card>
			</pane>

			<pane size="24" min-size="12">
				<el-card shadow="hover" class="ca-pane-card">
					<template #header>
						<div class="ca-head">
							<span class="ca-head-ico is-violet"
								><el-icon><ele-Share /></el-icon
							></span>
							<span class="ca-head-title">数据关系图</span>
						</div>
					</template>
					<div class="ca-chart">
						<!-- 空数据不要渲染图表：树图 series 无节点时 ECharts 算出的包围盒是空集，
						     坐标变换矩阵不可逆，setOption 会抛 "can't access property 0, m is null" -->
						<scEcharts v-if="echartsOption.series[0].data?.length" height="100%" :option="echartsOption"></scEcharts>
						<el-empty v-else description="选择结构化数据后展示关系图" :image-size="72" />
					</div>
				</el-card>
			</pane>
		</splitpanes>
	</div>
</template>

<!-- 缓存管理 -->
<script lang="ts" setup name="sysCache">
import { computed, defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, ElTree } from 'element-plus';
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import { getAPI } from '/@/utils/axios-utils';
import { SysCacheApi } from '/@/api-services/system/api';

const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

const treeRef = ref<InstanceType<typeof ElTree>>();
const currentNode = ref<any>({});
const state = reactive({
	loading: false,
	loading1: false,
	cacheData: [] as any,
	cacheValue: undefined as any,
	cacheKey: undefined,
});

// 顶部统计：分组数 = 一级节点数；键数 = 叶子节点数（无 children 的一级节点本身就是一个键）
const statGroups = computed(() => state.cacheData?.length ?? 0);
const statKeys = computed(() => (state.cacheData ?? []).reduce((sum: number, item: any) => sum + (item.children?.length ? item.children.length : 1), 0));

// 缓存值规模：卡片头上一句话说明当前值是对象 / 数组 / 文本，避免打开大对象后毫无体感
const valueMeta = computed(() => {
	const v = state.cacheValue;
	if (v === undefined || v === null) return '';
	if (Array.isArray(v)) return `数组 · ${v.length} 项`;
	if (typeof v === 'object') return `对象 · ${Object.keys(v).length} 个字段`;
	if (typeof v === 'string') return `文本 · ${v.length} 字符`;
	return `${typeof v} · ${String(v)}`;
});

const echartsOption = ref({
	// darkMode: true,
	// backgroundColor: '#100C2A',
	tooltip: {
		trigger: 'item',
		triggerOn: 'mousemove',
	},
	series: [
		{
			type: 'tree',
			data: [],
			// top: '1%',
			// left: '7%',
			// bottom: '1%',
			// right: '20%',
			symbolSize: 10,
			// symbol: 'rect',
			label: {
				position: 'left',
				verticalAlign: 'middle',
				align: 'right',
				fontSize: 9,
			},
			leaves: {
				label: {
					position: 'right',
					verticalAlign: 'middle',
					align: 'left',
				},
			},
			emphasis: {
				focus: 'descendant',
			},
			itemStyle: {},
			expandAndCollapse: true,
			animationDuration: 550,
			animationDurationUpdate: 750,
		},
	],
});

// 页面初始化
onMounted(async () => {
	await handleQuery();
});

// 查询操作
const handleQuery = async () => {
	state.cacheData = [];
	state.cacheValue = undefined;
	state.cacheKey = undefined;

	state.loading = true;
	var res = await getAPI(SysCacheApi).apiSysCacheKeyListGet();
	let keyList: any = res.data.result;

	// 构造树（以分号分割）
	for (let i = 0; i < keyList.length; i++) {
		let keyNames = keyList[i].split(':');
		let pName = keyNames[0];
		if (state.cacheData.filter((x: any) => x.name == pName).length === 0) {
			state.cacheData.push({
				id: keyNames.length > 1 ? 0 : keyList[i],
				name: pName,
				children: [],
			});
		}
		if (keyNames.length > 1) {
			let pNode = state.cacheData.filter((x: any) => x.name == pName)[0] || {};
			pNode.children.push({
				id: keyList[i],
				name: keyList[i].substr(pName.length + 1),
			});
		}
	}
	state.loading = false;
};

// 删除
const delCache = () => {
	if (currentNode.value.id == 0) {
		ElMessage.warning('禁止删除顶层缓存');
		return;
	}
	ElMessageBox.confirm(`确定删除缓存：${currentNode.value.id}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysCacheApi).apiSysCacheDeleteKeyPost(currentNode.value.id);
			await handleQuery();
			state.cacheValue = undefined;
			state.cacheKey = undefined;
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 清空
const clearCache = () => {
	ElMessageBox.confirm(`确认清空所有缓存?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysCacheApi).apiSysCacheClearPost();
			await handleQuery();
			state.cacheValue = undefined;
			state.cacheKey = undefined;
			ElMessage.success('清空成功');
		})
		.catch(() => {});
};

// 树点击
const nodeClick = async (node: any) => {
	if (node.id == 0) return;

	currentNode.value = node;
	state.loading1 = true;
	var res = await getAPI(SysCacheApi).apiSysCacheValueKeyGet(node.id);
	// state.cacheValue = JSON.parse(res.data.result);
	var result = res.data.result;
	if (typeof result == 'string') {
		try {
			var obj = JSON.parse(result);
			if (typeof obj == 'object') {
				state.cacheValue = obj;
			} else {
				state.cacheValue = result;
			}
		} catch (e) {
			state.cacheValue = result;
		}
		echartsOption.value.series[0].data = []; // 更新图表数据
	} else {
		state.cacheValue = result;
		// 树图 series.data 必须是非空数组：裸对象包一层，null / 非法值一律置空（空数据不渲染图表）
		echartsOption.value.series[0].data = result && typeof result === 'object' ? (Array.isArray(result) ? result : [result]) : []; // 更新图表数据
	}

	state.cacheKey = node.id;
	state.loading1 = false;
};
</script>

<style lang="scss" scoped>
/*
 * 页面整体：头图 + 提示条 + 分栏三段式。
 * 根容器用绝对定位铺满 .layout-parent（height:0 + overflow:auto 撑不出百分比高度），
 * 分栏再按剩余空间自适应，ECharts 就不会在 0 尺寸容器上 init
 * （坐标变换矩阵不可逆 → setOption 时报 "m is null"）。
 */
.ca-page {
	/* 不要再用 position:absolute + inset:5px：
	   路由组件根节点会被 parent.vue 加 .w100，而 theme/app.scss 生成的
	   .w100 { width: 100% !important } 会盖掉 width:auto —— 绝对定位下 100% 按
	   「padding box」算（比内容区宽 10px），right 失效，页面右侧永远多出一条横向滚动条。
	   改成普通 flex 项：.layout-parent 是 flex 列容器，flex:1 直接拿到剩余高度，
	   宽度由 align-items:stretch 得到内容区宽度（1110 = 1120 - 两侧 5px），与打印模板页一致。 */
	position: relative;
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	/* 页边距全部由 .layout-parent 的 padding:5px 提供，这里不能再叠 padding；
	   只补底部 4px，与打印模板页（.pt-page）的底部合计 9px 对齐 */
	padding-bottom: 4px;
	overflow: hidden;

	.page-hero {
		flex-shrink: 0;
	}
}

/* 当前键可能很长：限制宽度并省略，否则统计面板会被撑到卡片右边缘，右侧留白就没了 */
.page-hero__stat-v.is-key {
	max-width: 170px;
	margin: 0 auto;
	font-size: 13px;
	font-family: Consolas, Monaco, monospace;
	text-align: center;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* ===================== 风险提示条 ===================== */
.ca-notice {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
	margin-top: 14px;
	padding: 9px 14px;
	border-radius: 12px;
	border: 1px solid color-mix(in srgb, var(--el-color-warning) 35%, transparent);
	background: color-mix(in srgb, var(--el-color-warning) 10%, var(--el-bg-color));
}

/* 页面内相邻块的垂直间距统一 14px（与打印模板页一致） */

.ca-notice-ico {
	flex-shrink: 0;
	font-size: 16px;
	color: var(--el-color-warning);
}

.ca-notice-text {
	font-size: 12.5px;
	line-height: 1.5;
	color: var(--el-text-color-regular);
}

/* ===================== 分栏 ===================== */
.ca-panes {
	flex: 1 1 auto;
	min-height: 280px;
	margin-top: 14px;

	:deep(.splitpanes__pane) {
		min-width: 0;
		background: transparent;
	}

	/* 分隔条默认自带一条灰底，这里改成透明 + 悬停泛主题色，视觉上更像一条细缝 */
	:deep(.splitpanes__splitter) {
		background: transparent;
		border-radius: 4px;
		transition: background-color 0.2s ease;

		&:hover,
		&:active {
			background: color-mix(in srgb, var(--el-color-primary) 22%, transparent);
		}
	}
}

.ca-pane-card {
	height: 100%;
	display: flex;
	flex-direction: column;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);

	:deep(.el-card__header) {
		flex-shrink: 0;
		padding: 10px 14px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		background: linear-gradient(180deg, color-mix(in srgb, var(--el-fill-color-light) 75%, transparent), transparent);
	}

	:deep(.el-card__body) {
		flex: 1 1 auto;
		min-height: 0;
		overflow: auto;
		padding: 12px;
	}
}

/* ===================== 卡片头 ===================== */
.ca-head {
	display: flex;
	align-items: center;
	gap: 9px;
	min-width: 0;
}

.ca-head-ico {
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	font-size: 14px;
	/* 装饰规范：浅色同色系底 + 深色图标（见个人中心 .uc-fact-ico） */
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-blue {
		--tint: #3b82f6;
	}

	&.is-green {
		--tint: #10b981;
	}

	&.is-violet {
		--tint: #8b5cf6;
	}
}

.ca-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.ca-head-count {
	padding: 0 8px;
	line-height: 18px;
	border-radius: 999px;
	font-size: 11px;
	font-weight: 700;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.ca-head-key {
	max-width: 260px;
	padding: 0 8px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 11px;
	font-family: Consolas, Monaco, monospace;
	color: var(--el-color-success);
	background: color-mix(in srgb, var(--el-color-success) 14%, transparent);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.ca-head-meta {
	flex-shrink: 0;
	padding: 0 7px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 11px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color);
	white-space: nowrap;
}

.ca-head-acts {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-left: auto;
	flex-shrink: 0;
}

/* ===================== 缓存树 ===================== */
.ca-tree {
	:deep(.el-tree-node__content) {
		height: 32px;
		border-radius: 8px;
		transition: background-color 0.2s ease;
	}

	:deep(.el-tree-node__content:hover) {
		background: var(--el-fill-color-light);
	}

	/* 选中项：主题色底 + 名称加粗，比默认的浅灰底更容易定位 */
	:deep(.el-tree-node.is-current > .el-tree-node__content) {
		background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
	}

	:deep(.el-tree-node.is-current > .el-tree-node__content .ca-node-name) {
		font-weight: 600;
		color: var(--el-color-primary);
	}

	:deep(.el-tree-node__expand-icon) {
		color: var(--el-text-color-placeholder);
	}
}

.ca-node {
	display: inline-flex;
	align-items: center;
	gap: 7px;
	min-width: 0;
	font-size: 13px;
}

.ca-node-ico {
	flex-shrink: 0;
	font-size: 14px;
	color: var(--el-color-primary);

	&.is-group {
		color: var(--el-color-warning);
	}
}

.ca-node-name {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.ca-node-count {
	flex-shrink: 0;
	padding: 0 6px;
	line-height: 17px;
	border-radius: 999px;
	font-size: 10.5px;
	font-weight: 700;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color);
}

/* ===================== 缓存值 / 关系图 ===================== */
.ca-json {
	min-height: 200px;
	padding: 2px 4px;
	font-size: 13px;

	/* JSON 结构用等宽字体，层级缩进才对得齐 */
	:deep(.vjs-tree) {
		font-family: Consolas, Monaco, monospace;
		font-size: 12.5px;
		line-height: 1.75;
	}

	:deep(.el-empty) {
		padding: 40px 0;
	}
}

.ca-chart {
	height: 100%;
	min-height: 260px;

	:deep(.el-empty) {
		height: 100%;
		justify-content: center;
	}
}
</style>
