<template>
	<div class="st-page">
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
					<h2 class="page-hero__title">接口压测</h2>
					<p class="page-hero__desc">挑选任意接口发起压力测试，实时给出 QPS、成功/失败次数与各分位响应时间，用于定位接口性能瓶颈。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ stStats.apis }}</span>
							<span class="page-hero__stat-k">可压测接口</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ stStats.qps }}</span>
							<span class="page-hero__stat-k">最近 QPS</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ stStats.success }}</span>
							<span class="page-hero__stat-k">成功次数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ stStats.failed }}</span>
							<span class="page-hero__stat-k">失败次数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="st-notice">
			<NoticeBar text="接口压测会占用服务器大量的系统资源（内存、带宽），请慎重操作！！！" />
		</div>

		<div class="st-body">
			<splitpanes class="default-theme">
				<!-- ===== 左：接口列表 ===== -->
				<pane size="22">
					<el-card shadow="hover" class="st-card st-api" v-loading="state.loading">
						<template #header>
							<div class="st-head">
								<span class="st-head-ico"><el-icon><ele-Menu /></el-icon></span>
								<span class="st-head-title">接口列表</span>
								<span class="st-head-count">{{ stStats.apis }}</span>
							</div>
						</template>
						<div class="st-api-body">
							<el-select v-model="state.swaggerUrl" class="st-select" placeholder="接口分组" @change="queryTreeNode()">
								<el-option v-for="(item, index) in state.apiGroupList" :key="index" :label="item.name" :value="item.url" />
							</el-select>
							<el-input v-model="state.keywords" placeholder="接口名称" clearable @keyup.enter="queryTreeNode()">
								<template #append>
									<el-button icon="ele-Search" v-reclick="100" @click="queryTreeNode()" />
								</template>
							</el-input>
							<el-scrollbar class="st-tree">
								<el-tree
									ref="treeRef"
									class="filter-tree"
									:data="state.apiList"
									:props="{ children: 'children', label: 'summary' }"
									:filter-node-method="filterNode"
									node-key="id"
									highlight-current
									check-strictly
								>
									<template #default="{ node }">
										<span class="st-node">
											<el-icon v-if="node.level == 1" size="16" class="st-node-ico"><ele-Menu /></el-icon>
											<el-icon v-else size="16" class="st-node-ico"><ele-Link /></el-icon>
											<span class="st-node-name">{{ node.label }}</span>
											<span class="node-button" v-if="!node.data.children">
												<el-button type="warning" icon="ele-Position" @click="treeNodeTest(node.data)" />
											</span>
										</span>
									</template>
								</el-tree>
							</el-scrollbar>
						</div>
					</el-card>
				</pane>

				<!-- ===== 右：压测参数与结果 ===== -->
				<pane size="78">
					<el-card shadow="hover" class="st-card st-result" v-loading="state.loading">
						<template #header>
							<div class="st-head">
								<span class="st-head-ico is-primary"><el-icon><ele-Odometer /></el-icon></span>
								<span class="st-head-title">压测结果</span>
								<div class="st-head-acts">
									<el-button type="primary" icon="ele-Odometer" @click="showDialog(undefined)">接口压测</el-button>
								</div>
							</div>
						</template>

						<div class="st-block">
							<div class="st-block-title">
								<span class="st-block-ico"><el-icon><ele-Setting /></el-icon></span>
								压测参数
							</div>
							<el-descriptions label-width="150px" :column="2" border class="st-desc">
								<el-descriptions-item label="请求地址" label-align="left" align="left">
									{{ state.ruleForm.requestUri || '—' }}
								</el-descriptions-item>
								<el-descriptions-item label="请求方式" label-align="left" align="left">
									<el-tag v-if="state.ruleForm.requestUri" effect="light" round size="small">{{ state.ruleForm.requestMethod?.toUpperCase() }}</el-tag>
									<span v-else>—</span>
								</el-descriptions-item>
								<el-descriptions-item label="压测轮数" label-align="left" align="left">
									{{ state.ruleForm.numberOfRounds ?? 1 }}
								</el-descriptions-item>
								<el-descriptions-item label="每轮请求数" label-align="left" align="left">
									{{ state.ruleForm.numberOfRequests ?? 1 }}
								</el-descriptions-item>
								<el-descriptions-item label="最大并发量" label-align="left" align="left">
									{{ state.ruleForm.maxDegreeOfParallelism ?? 1 }}
								</el-descriptions-item>
							</el-descriptions>
						</div>

						<div class="st-block">
							<div class="st-block-title">
								<span class="st-block-ico is-green"><el-icon><ele-DataLine /></el-icon></span>
								压测指标
							</div>
							<div v-if="!state.result.totalTimeInSeconds" class="st-empty">
								<el-empty description="还没有压测记录，点击右上角「接口压测」开始" :image-size="140" />
							</div>
							<template v-else>
								<div class="st-metrics">
									<div class="st-metric">
										<span class="st-metric-v">{{ (state.result.totalTimeInSeconds ?? 0).toFixed(2) }}</span>
										<span class="st-metric-k">总用时（秒）</span>
									</div>
									<div class="st-metric is-success">
										<span class="st-metric-v">{{ state.result.successfulRequests ?? 0 }}</span>
										<span class="st-metric-k">成功请求</span>
									</div>
									<div class="st-metric is-danger">
										<span class="st-metric-v">{{ state.result.failedRequests ?? 0 }}</span>
										<span class="st-metric-k">失败请求</span>
									</div>
									<div class="st-metric is-primary">
										<span class="st-metric-v">{{ (state.result.queriesPerSecond ?? 0).toFixed(2) }}</span>
										<span class="st-metric-k">QPS</span>
									</div>
								</div>

								<el-descriptions label-width="180px" :column="3" border class="st-desc">
									<el-descriptions-item label="最小响应时间（毫秒）" label-align="left" align="left">
										{{ (state.result.minResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="最大响应时间（毫秒）" label-align="left" align="left">
										{{ (state.result.maxResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="平均响应时间（毫秒）" label-align="left" align="left">
										{{ (state.result.averageResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P10（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile10ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P25（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile25ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P50（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile50ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P75（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile75ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P90（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile90ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P99（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile99ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
									<el-descriptions-item label="P999（毫秒）" label-align="left" align="left">
										{{ (state.result.percentile9999ResponseTime ?? 0).toFixed(2) }}
									</el-descriptions-item>
								</el-descriptions>
							</template>
						</div>
					</el-card>
				</pane>
			</splitpanes>
		</div>

		<EditStressTest ref="editStressTestRef" @refreshData="refreshData" />
	</div>
</template>
<script lang="ts" setup name="sysStressTest">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElTree } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import 'vue-json-pretty/lib/styles.css';

import EditStressTest from './component/editStressTest.vue';
import NoticeBar from '/@/components/noticeBar/index.vue';

import request, { getToken } from '/@/utils/request';
import { StressTestHarnessResult } from '/@/api-services/system/models';

const editStressTestRef = ref();
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	activeName: '',
	ruleForm: {
		requestUri: '',
		requestMethod: 'GET',
		numberOfRounds: 1,
		numberOfRequests: 10,
		maxDegreeOfParallelism: 10,
		requestParameters: [],
		queryParameters: [],
		pathParameters: [],
		headers: [],
	},
	keywords: undefined,
	swaggerUrl: '/swagger/Default/swagger.json',
	result: {} as StressTestHarnessResult,
	apiList: [] as Array<any>,
	apiGroupList: [] as any,
});

// 头图统计：接口数递归整棵树（只数叶子节点，即真实接口），其余取最近一次压测结果
const stStats = computed(() => {
	let apis = 0;
	const walk = (nodes: any[] | undefined) => {
		for (const n of nodes ?? []) {
			if (n.children) walk(n.children);
			else apis += 1;
		}
	};
	walk(state.apiList);
	return {
		apis,
		qps: (state.result.queriesPerSecond ?? 0).toFixed(2),
		success: state.result.successfulRequests ?? 0,
		failed: state.result.failedRequests ?? 0,
	};
});

// 页面初始化
onMounted(async () => {
	state.apiGroupList = await getApiGroupList();
	state.apiList = await getApiList('');
});

// 获取接口分组列表
const getApiGroupList = async () => {
	try {
		const response = await request('/api/sysCommon/swaggerGroups', { method: 'get' });
		return response.data.result
			.filter((resource: { name: string; url: string }) => !resource.url.toLowerCase().includes('all%20groups') && !resource.url.toLowerCase().includes('all groups'))
			.map((resource: { name: string; url: string }) => {
				const rawUrl = resource.url || '';
				let fixedUrl = rawUrl.startsWith('//') ? rawUrl.substring(1) : rawUrl;
				if (!fixedUrl.startsWith('/') && !fixedUrl.includes('://')) {
					fixedUrl = '/' + fixedUrl;
				}
				return {
					name: decodeURIComponent(resource.name || ''),
					url: fixedUrl,
				};
			});
	} catch {
		return [];
	}
};

// 获取所有接口列表
const getApiList = (keywords: string | undefined) => {
	const emojiPattern =
		/[\u{2139}\u{2B05}-\u{2B07}\u{1F600}-\u{1F64F}\u{1F300}-\u{1F5FF}\u{1F680}-\u{1F6FF}\u{1F700}-\u{1F77F}\u{1F780}-\u{1F7FF}\u{1F800}-\u{1F8FF}\u{1F900}-\u{1F9FF}\u{1FA00}-\u{1FA6F}\u{1FA70}-\u{1FAFF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]/gu;
	return request(state.swaggerUrl, { method: 'get' }).then(({ data }) => {
		const pathMap = data.paths;
		const result = data.tags.map((e: any) => ({ path: e.name, summary: e.description?.replaceAll(emojiPattern, '') || e.name, children: [] }));
		Object.keys(pathMap).map((path) => {
			const method = Object.keys(pathMap[path])[0];
			const apiInfo = pathMap[path][method];
			if (keywords && apiInfo.summary?.indexOf(keywords) === -1) return;
			result
				.find((u: any) => u.path === apiInfo.tags[0])
				.children.push({
					path: path,
					method: method,
					summary: apiInfo.summary?.replaceAll(emojiPattern, '') ?? path,
					parameters: apiInfo.parameters,
					requestBody: apiInfo.requestBody,
					data: apiInfo,
				});
		});
		return result.filter((u: any) => u.children.length > 0);
	});
};

// 刷新数据
const refreshData = (data: StressTestHarnessResult) => {
	state.result = data;
};

// 打开接口参数设置页面
const showDialog = async (row: any) => {
	const newRow = row ?? { ...state.ruleForm };
	const convertToKeyValuePairs = (params: any) => {
		if (Array.isArray(params) && params.every((item) => Array.isArray(item) && item.length === 2)) {
			return params;
		} else if (typeof params === 'object' && params !== null) {
			return Object.entries(params);
		}
		return [];
	};

	state.ruleForm = {
		...newRow,
		requestParameters: convertToKeyValuePairs(newRow.requestParameters),
		queryParameters: convertToKeyValuePairs(newRow.queryParameters),
		pathParameters: convertToKeyValuePairs(newRow.pathParameters),
		headers: convertToKeyValuePairs(newRow.headers),
	};
	editStressTestRef.value.openDialog(state.ruleForm);
};

// 接口树节点按钮事件
const treeNodeTest = async (node: any) => {
	if (node.id == 0) return;
	state.ruleForm = {
		requestUri: location.origin + node.path,
		requestMethod: node.method,
		numberOfRounds: 1,
		numberOfRequests: 10,
		maxDegreeOfParallelism: 10,
		requestParameters: [],
		queryParameters: [],
		pathParameters: [],
		headers: [['Authorization', 'Bearer ' + getToken()]] as any,
	};
	showDialog(state.ruleForm);
};

// 查询树节点
const queryTreeNode = async () => {
	state.apiList = await getApiList(state.keywords);
};

const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.name.includes(value);
};
</script>


<style lang="scss" scoped>
.st-page {
	position: relative;
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;
	padding-bottom: 4px;
	overflow: hidden;
}

.st-notice {
	flex-shrink: 0;
	margin-top: 14px;
}

.st-body {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;

	:deep(.splitpanes) {
		width: 100%;
		height: 100%;
	}
}

.st-card {
	height: 100%;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
}

/* ===================== 卡头 ===================== */
.st-head {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

/* 装饰规范：图标深色、外围浅色同色系 */
.st-head-ico {
	flex: 0 0 auto;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--tint, var(--el-color-warning));
	background: color-mix(in srgb, var(--tint, var(--el-color-warning)) 13%, transparent);

	&.is-primary {
		--tint: var(--el-color-primary);
	}
}

.st-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.st-head-count {
	padding: 0 8px;
	height: 18px;
	line-height: 18px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.st-head-acts {
	margin-left: auto;
	display: flex;
	align-items: center;
	gap: 8px;
}

/* ===================== 左侧接口列表 ===================== */
.st-api {
	:deep(.el-card__body) {
		height: calc(100% - 62px);
		padding: 12px 14px;
		display: flex;
		flex-direction: column;
		gap: 10px;
	}
}

.st-api-body {
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	gap: 10px;
}

.st-select {
	width: 100%;
}

.st-tree {
	flex: 1 1 auto;
	min-height: 0;
}

.st-node {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
}

.st-node-ico {
	color: var(--el-color-primary);
}

.st-node-name {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.node-button {
	position: absolute;
	right: 0;
	scale: 0.7;
}

/* ===================== 右侧结果 ===================== */
.st-result {
	:deep(.el-card__body) {
		height: calc(100% - 62px);
		overflow: auto;
		padding: 14px 16px;
	}
}

.st-block + .st-block {
	margin-top: 18px;
}

.st-block-title {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 10px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.st-block-ico {
	flex: 0 0 auto;
	width: 24px;
	height: 24px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 13px;
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-green {
		--tint: var(--el-color-success);
	}
}

.st-metrics {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 12px;
	margin-bottom: 14px;
}

.st-metric {
	padding: 12px 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.st-metric-v {
	font-size: 20px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.st-metric-k {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.st-metric.is-success .st-metric-v {
	color: var(--el-color-success);
}

.st-metric.is-danger .st-metric-v {
	color: var(--el-color-danger);
}

.st-metric.is-primary .st-metric-v {
	color: var(--el-color-primary);
}

.st-desc {
	width: 100%;
}

@media (max-width: 1200px) {
	.st-metrics {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}
</style>
