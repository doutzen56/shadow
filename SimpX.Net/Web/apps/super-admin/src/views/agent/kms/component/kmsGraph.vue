<template>
	<el-dialog v-model="visible" class="kg-dialog" width="100%" top="0" :show-close="false" append-to-body destroy-on-close @opened="handleOpened" @closed="handleClosed">
		<div class="kg-shell">
			<!-- 顶栏：标题 + 核心指标 + 操作 -->
			<header class="kg-head">
				<div class="kg-head__main">
					<span class="kg-head__icon">
						<el-icon :size="17"><ele-Share /></el-icon>
					</span>
					<div class="kg-head__text">
						<h3>{{ title || '知识图谱' }}</h3>
						<p>
							文档与分块的语义关系图
							<template v-if="state.data"> · 生成于 {{ generatedAt }}</template>
						</p>
					</div>
				</div>
				<div class="kg-head__stats">
					<div class="kg-stat">
						<b>{{ state.data?.docCount ?? 0 }}</b>
						<span>文档</span>
					</div>
					<div class="kg-stat">
						<b>{{ chunkStat }}</b>
						<span>分块</span>
					</div>
					<div class="kg-stat">
						<b>{{ state.data?.semanticEdgeCount ?? 0 }}</b>
						<span>语义边</span>
					</div>
					<div class="kg-stat">
						<b>{{ clusterCount }}</b>
						<span>聚类</span>
					</div>
				</div>
				<div class="kg-head__ops">
					<el-button icon="ele-Refresh" :loading="state.loading" @click="reload">重新分析</el-button>
					<el-button icon="ele-Close" @click="visible = false">关闭</el-button>
				</div>
			</header>

			<div class="kg-body">
				<!-- 左栏：视图设置 + 文档导航 -->
				<aside class="kg-side">
					<div class="kg-panel">
						<div class="kg-panel__title">视图</div>
						<div class="kg-field">
							<span>显示分块</span>
							<el-switch v-model="state.includeChunks" size="small" />
						</div>
						<div class="kg-field">
							<span>分块上限</span>
							<el-select v-model="state.maxChunks" size="small" class="kg-select">
								<el-option v-for="count in chunkOptions" :key="count" :label="`${count} 个`" :value="count" />
							</el-select>
						</div>
						<div class="kg-field is-block">
							<div class="kg-field__head">
								<span>相似度阈值</span>
								<b>{{ state.minScore.toFixed(2) }}</b>
							</div>
							<el-slider v-model="state.minScore" :min="0.55" :max="0.95" :step="0.01" size="small" :show-tooltip="false" />
						</div>
						<div class="kg-field is-block">
							<div class="kg-field__head"><span>边类型</span></div>
							<div class="kg-chips">
								<button class="kg-chip" :class="{ 'is-on': state.showContains }" @click="state.showContains = !state.showContains">
									<i class="kg-chip__line"></i>归属
								</button>
								<button class="kg-chip" :class="{ 'is-on': state.showSemantic }" @click="state.showSemantic = !state.showSemantic">
									<i class="kg-chip__line is-semantic"></i>语义
								</button>
							</div>
						</div>
						<div class="kg-field">
							<span>自动布局</span>
							<el-button size="small" text type="primary" icon="ele-RefreshRight" @click="relayout">重新排布</el-button>
						</div>
					</div>

					<div class="kg-panel is-grow">
						<div class="kg-panel__title">
							文档<em>{{ docList.length }}</em>
						</div>
						<el-input v-model="state.keyword" size="small" placeholder="搜索文档或分块内容" clearable prefix-icon="ele-Search" />
						<div class="kg-doclist">
							<button v-for="doc in docList" :key="doc.id" class="kg-doc" :class="{ 'is-active': state.selected === `doc-${doc.id}` }" @click="selectDoc(doc)">
								<i class="kg-doc__dot" :style="{ background: clusterColor(doc.cluster) }"></i>
								<span class="kg-doc__name" :title="doc.name">{{ doc.name }}</span>
								<span class="kg-doc__count">
									{{ doc.sampledCount }}<em v-if="doc.sampledCount !== doc.chunkCount">/{{ doc.chunkCount }}</em>
								</span>
							</button>
							<div v-if="!docList.length" class="kg-inline-empty">没有匹配的文档</div>
						</div>
						<p v-if="state.data?.truncated" class="kg-truncate-tip">
							<el-icon :size="12"><ele-Warning /></el-icon>
							分块较多，当前每个文档只抽取了部分分块参与计算
						</p>
					</div>
				</aside>

				<!-- 画布 -->
				<section class="kg-stage">
					<!-- 极光底：三层缓慢漂移的主题色光斑，Sigma 的 WebGL 画布是透明的，直接透出这一层 -->
					<div class="kg-aurora" aria-hidden="true">
						<i class="kg-aurora__blob is-a"></i>
						<i class="kg-aurora__blob is-b"></i>
						<i class="kg-aurora__blob is-c"></i>
					</div>
					<div ref="canvasRef" class="kg-canvas"></div>
					<div class="kg-veil" aria-hidden="true"></div>

					<!-- 聚类筛选：点一下只看这一簇，再点一下回到全部 -->
					<div v-if="clusters.length" class="kg-clusters">
						<button class="kg-cluster" :class="{ 'is-on': state.activeCluster === null }" @click="focusCluster(null)">全部</button>
						<button
							v-for="item in clusters"
							:key="item.cluster"
							class="kg-cluster"
							:class="{ 'is-on': state.activeCluster === item.cluster }"
							:title="item.tip"
							@click="focusCluster(item.cluster)"
						>
							<i :style="{ background: item.color }"></i>{{ item.label }}<span>{{ item.count }}</span>
						</button>
					</div>

					<div class="kg-legend">
						<span class="kg-legend__item"><i class="is-kms"></i>知识库</span>
						<span class="kg-legend__item"><i class="is-doc"></i>文档</span>
						<span class="kg-legend__item"><i class="is-chunk"></i>分块</span>
						<span class="kg-legend__sep"></span>
						<span class="kg-legend__item"><i class="is-line"></i>归属</span>
						<span class="kg-legend__item"><i class="is-line is-semantic"></i>语义相近</span>
						<span class="kg-legend__sep"></span>
						<span class="kg-legend__note">颜色表示语义簇</span>
					</div>

					<div class="kg-tools">
						<el-button circle text icon="ele-ZoomIn" title="放大" @click="zoom(1.35)" />
						<el-button circle text icon="ele-ZoomOut" title="缩小" @click="zoom(1 / 1.35)" />
						<el-button circle text icon="ele-Aim" title="适应画布" @click="resetCamera" />
					</div>

					<div class="kg-hint">拖拽节点可微调位置 · 滚轮缩放 · 双击节点聚焦 · 单击查看详情</div>

					<div v-if="state.warning" class="kg-banner">
						<el-icon :size="14"><ele-WarningFilled /></el-icon>
						<span>{{ state.warning }}</span>
					</div>

					<div v-if="state.loading" class="kg-mask">
						<div class="kg-loader"></div>
						<p>正在读取向量并计算语义关系…</p>
					</div>
					<div v-else-if="state.error" class="kg-mask">
						<el-icon :size="34" class="kg-mask__icon"><ele-CircleClose /></el-icon>
						<p>{{ state.error }}</p>
						<el-button size="small" icon="ele-Refresh" @click="load">重试</el-button>
					</div>
					<div v-else-if="state.data && !state.data.nodes.length" class="kg-mask">
						<el-icon :size="34" class="kg-mask__icon"><ele-Share /></el-icon>
						<p>还没有可以展示的知识结构，先上传文档并完成向量化</p>
					</div>
				</section>

				<!-- 右栏：节点详情 -->
				<aside v-if="selectedNode" class="kg-detail">
					<div class="kg-detail__head">
						<span class="kg-detail__badge" :class="`is-${selectedNode.type}`">{{ typeText(selectedNode.type) }}</span>
						<span class="kg-detail__cluster" v-if="selectedNode.cluster >= 0">
							<i :style="{ background: clusterColor(selectedNode.cluster) }"></i>簇 {{ selectedNode.cluster + 1 }}
						</span>
						<button class="kg-detail__close" title="关闭详情" @click="state.selected = null">
							<el-icon :size="14"><ele-Close /></el-icon>
						</button>
					</div>
					<h4 class="kg-detail__title">{{ selectedNode.type === 'chunk' ? `第 ${(selectedNode.chunkIndex ?? 0) + 1} 块` : selectedNode.label }}</h4>
					<div class="kg-detail__meta">
						<span v-if="selectedNode.docName" :title="selectedNode.docName">{{ selectedNode.docName }}</span>
						<span>关联 {{ selectedNode.degree }} 条</span>
					</div>
					<p v-if="selectedNode.content" class="kg-detail__content">{{ selectedNode.content }}</p>

					<div v-if="neighborNodes.length" class="kg-detail__section">
						相邻节点
						<div class="kg-neighbors">
							<button v-for="item in neighborNodes" :key="item.id" class="kg-nb" @click="selectNode(item.id)">
								<i :style="{ background: item.color }"></i>
								<span :title="item.label">{{ item.label }}</span>
								<em>{{ item.weight.toFixed(2) }}</em>
							</button>
						</div>
					</div>

					<div class="kg-detail__ops">
						<el-button size="small" icon="ele-Aim" @click="focusNode(selectedNode.id)">定位</el-button>
						<el-button size="small" icon="ele-CopyDocument" :disabled="!selectedNode.content" @click="copyContent">复制正文</el-button>
					</div>
				</aside>
			</div>
		</div>
	</el-dialog>
</template>

<script lang="ts" setup name="agentKmsGraph">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import Graph from 'graphology';
import Sigma from 'sigma';

import { fetchKmsGraph } from './kmsGraphApi';
import type { KmsGraphData, KmsGraphNode, KmsGraphNodeType } from './kmsGraphApi';

const props = defineProps<{
	/** 知识库 Id */
	kmsId?: string | number;
	/** 知识库名称（顶栏标题） */
	kmsTitle?: string;
}>();

const visible = defineModel<boolean>('visible', { default: false });

/** 后端统一按较低的相似度下限返回候选边，前端滑杆只做本地过滤，拖动时不再请求接口 */
const REQUEST_MIN_SCORE = 0.55;

/** 入场揭示时长（毫秒） */
const REVEAL_MS = 780;

/** 分块数量档位 */
const chunkOptions = [200, 400, 800, 1200];

/** 聚类配色：同一簇内的文档与分块同色，跨簇区分（颜色只用来分类，不表示状态） */
const CLUSTER_PALETTE = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1', '#14b8a6'];

const canvasRef = ref<HTMLDivElement>();

const state = reactive({
	loading: false,
	error: '',
	warning: '',
	data: null as KmsGraphData | null,
	includeChunks: true,
	maxChunks: 400,
	minScore: 0.72,
	showContains: true,
	showSemantic: true,
	keyword: '',
	selected: null as string | null,
	/** 当前只看这一簇（null = 全部） */
	activeCluster: null as number | null,
});

/** 当前悬停或选中的节点及其一阶邻居（其余节点/边淡出，局部结构一眼看清） */
const highlighted = ref<Set<string>>(new Set());

/** 当前真正悬停的节点：分块的文字标签只在悬停时出现，避免一排邻居把画布糊满 */
const hovered = ref<string | null>(null);

let sigma: Sigma | null = null;
let graph: Graph | null = null;
/** 知识库根节点 Id：布局时把它钉在原点，否则它会被两侧的簇拽着来回漂 */
let rootId: string | null = null;
let dragging = false;
let draggedNode: string | null = null;

/* ------------------------------ 动效状态 ------------------------------ */

/** 入场揭示进度（0 → 1）：知识库、文档、分块依次亮起，相机同步推近 */
const reveal = ref(1);
/** 根节点呼吸相位（0 → 1 往复）：尺寸极小幅起伏，画面「活着」但不干扰阅读 */
let breathePhase = 0;
let revealFrame = 0;
let breatheFrame = 0;
let breatheLast = 0;
/** 标签描边色：用底色描边代替白色气泡，深浅主题下文字都清晰 */
let labelHaloColor = 'rgba(255, 255, 255, 0.92)';
/** 本帧已排下的标签占位框，用于避免相邻文档的标签叠在一起 */
const labelBoxes: Array<{ x1: number; y1: number; x2: number; y2: number }> = [];

const title = computed(() => props.kmsTitle || state.data?.name || '知识图谱');

const generatedAt = computed(() => {
	if (!state.data?.generatedAt) return '';
	const text = String(state.data.generatedAt).replace('T', ' ');
	return text.substring(11, 19);
});

/** 分块数：看分块视图时用「参与分析的分块」，关掉后显示文档总块数，避免出现「有文档但分块 0」 */
const chunkStat = computed(() => {
	const data = state.data;
	if (!data) return 0;
	return state.includeChunks ? data.chunkCount : data.totalChunkCount;
});

/** 簇数量：知识库根节点的簇为 -1，不参与统计 */
const clusterCount = computed(() => {
	const nodes = state.data?.nodes ?? [];
	return new Set(nodes.filter((u) => u.type === 'doc').map((u) => u.cluster)).size;
});

const docList = computed(() => {
	const docs = state.data?.docs ?? [];
	const keyword = state.keyword.trim().toLowerCase();
	if (!keyword) return docs;
	return docs.filter((u) => u.name.toLowerCase().includes(keyword));
});

/** 簇清单：用于画布左上角的簇筛选，标签取簇内第一份文档名（去掉扩展名） */
const clusters = computed(() => {
	const docs = state.data?.docs ?? [];
	const grouped = new Map<number, string[]>();
	for (const doc of docs) {
		if (!grouped.has(doc.cluster)) grouped.set(doc.cluster, []);
		grouped.get(doc.cluster)!.push(doc.name);
	}
	return [...grouped.entries()]
		.sort((a, b) => a[0] - b[0])
		.map(([cluster, names]) => ({
			cluster,
			color: clusterColor(cluster),
			label: names[0].replace(/\.[^.]+$/, '').slice(0, 10),
			count: names.length,
			tip: names.join('、'),
		}));
});

const selectedNode = computed(() => {
	if (!state.selected) return null;
	return state.data?.nodes.find((u) => u.id === state.selected) ?? null;
});

/** 相邻节点：按连边权重从高到低取前 8 个，点一下就能顺着关系走 */
const neighborNodes = computed(() => {
	if (!graph || !state.selected || !graph.hasNode(state.selected)) return [];
	const center = state.selected;
	const best = new Map<string, number>();
	graph.forEachEdge(center, (_edge, attributes, source, target) => {
		const other = source === center ? target : source;
		const weight = Number((attributes as Record<string, unknown>).weight ?? 1);
		if (weight > (best.get(other) ?? 0)) best.set(other, weight);
	});
	return [...best.entries()]
		.sort((a, b) => b[1] - a[1])
		.slice(0, 8)
		.map(([id, weight]) => {
			const attributes = graph!.getNodeAttributes(id);
			return {
				id,
				label: (attributes.label as string) || id,
				color: (attributes.color as string) || '#909399',
				weight,
			};
		});
});

/* ------------------------------ 颜色与工具函数 ------------------------------ */

/** 主题主色（知识库根节点用它，和页面的品牌色保持一致） */
const primaryColor = (): string => (getComputedStyle(document.documentElement).getPropertyValue('--el-color-primary') || '#248067').trim() || '#248067';

const isDarkTheme = () => document.documentElement.getAttribute('data-theme') === 'dark';

const clusterColor = (cluster: number) => CLUSTER_PALETTE[((cluster % CLUSTER_PALETTE.length) + CLUSTER_PALETTE.length) % CLUSTER_PALETTE.length];

/** 颜色加透明度：悬停时把无关节点淡出，比整体改透明度更可控 */
const toRgba = (color: string, alpha: number) => {
	const value = (color || '').trim();
	if (value.startsWith('#')) {
		const hex = value.slice(1);
		const full = hex.length === 3 ? hex.split('').map((c) => c + c).join('') : hex;
		const num = Number.parseInt(full, 16);
		if (Number.isNaN(num)) return value;
		return `rgba(${(num >> 16) & 255}, ${(num >> 8) & 255}, ${num & 255}, ${alpha})`;
	}
	const parts = value.match(/\d+/g);
	return parts && parts.length >= 3 ? `rgba(${parts[0]}, ${parts[1]}, ${parts[2]}, ${alpha})` : value;
};

const typeText = (type: KmsGraphNodeType) => (type === 'kms' ? '知识库' : type === 'doc' ? '文档' : '分块');

const nodeColor = (node: KmsGraphNode) => (node.type === 'kms' ? primaryColor() : clusterColor(node.cluster));

/* ------------------------------ 数据加载 ------------------------------ */

const load = async () => {
	if (!props.kmsId) return;

	destroySigma();
	state.loading = true;
	state.error = '';
	state.selected = null;
	state.activeCluster = null;

	try {
		const data = await fetchKmsGraph({
			kmsId: props.kmsId,
			includeChunks: state.includeChunks,
			minScore: REQUEST_MIN_SCORE,
			maxChunks: state.maxChunks,
		});
		state.data = data;
		state.warning = data.warning || '';
		// 后端的相似度下限可能比滑杆更低（返回值里带的是实际使用的阈值），滑杆不低于它才有边可看
		state.minScore = Math.max(state.minScore, data.minScore);

		await nextTick();
		if (data.nodes.length) buildSigma(data);
	} catch (error) {
		state.error = (error as Error)?.message || '知识图谱加载失败';
	} finally {
		state.loading = false;
	}
};

const reload = () => {
	state.minScore = 0.72;
	load();
};

/* ------------------------------ 图构建与布局 ------------------------------ */

const buildSigma = (data: KmsGraphData) => {
	if (!canvasRef.value) return;

	graph = new Graph({ multi: true });
	rootId = data.nodes.find((u) => u.type === 'kms')?.id ?? null;
	for (const node of data.nodes) {
		graph.addNode(node.id, {
			x: 0,
			y: 0,
			size: node.size,
			label: node.label,
			color: nodeColor(node),
			kind: node.type,
			cluster: node.cluster,
			docId: node.docId ?? null,
			docName: node.docName ?? '',
			chunkIndex: node.chunkIndex ?? null,
			content: node.content ?? '',
			degree: node.degree,
			forceLabel: node.type !== 'chunk',
		});
	}

	const dark = isDarkTheme();
	labelHaloColor = dark ? 'rgba(8, 14, 12, 0.86)' : 'rgba(255, 255, 255, 0.92)';
	// 边按「基础色 + 基础透明度」存：入场动画要按比例调整透明度，
	// 直接在 rgba 字符串上做乘法容易把颜色解析坏
	const containsColor = dark ? '#8ea0b6' : '#8c96aa';
	for (const edge of data.edges) {
		// 防御：边端点缺失时直接丢弃（例如后端在两种视图之间切换时多下发了边），
		// graphology 遇到不存在的端点会抛错并导致整张图渲染失败
		if (!graph.hasNode(edge.source) || !graph.hasNode(edge.target)) continue;
		const sourceColor = (graph.getNodeAttribute(edge.source, 'color') as string) || '#909399';
		// 语义边用所属文档的簇色，深浅随相似度走；归属边用中性色，避免和簇色打架
		const baseColor = edge.type === 'semantic' ? sourceColor : containsColor;
		const baseAlpha = edge.type === 'semantic' ? 0.24 + Math.max(0, edge.weight - 0.5) * 0.8 : dark ? 0.36 : 0.44;
		graph.addEdgeWithKey(edge.id, edge.source, edge.target, {
			edgeType: edge.type,
			weight: edge.weight,
			size: edge.type === 'semantic' ? 1.4 : 1,
			baseColor,
			baseAlpha,
			color: toRgba(baseColor, baseAlpha),
		});
	}


	sigma = new Sigma(graph, canvasRef.value, {
		renderEdgeLabels: false,
		enableEdgeEvents: false,
		minCameraRatio: 0.05,
		maxCameraRatio: 8,
		labelDensity: 0.55,
		labelGridCellSize: 72,
		labelRenderedSizeThreshold: 6,
		labelSize: 12.5,
		labelWeight: '500',
		labelFont: 'system-ui, -apple-system, "PingFang SC", "Microsoft YaHei", sans-serif',
		labelColor: { color: isDarkTheme() ? '#d6deeb' : '#303133' },
		defaultEdgeColor: isDarkTheme() ? 'rgba(170,180,200,0.25)' : 'rgba(130,140,160,0.3)',
		defaultDrawNodeLabel: drawNodeLabel,
		defaultDrawNodeHover: drawNodeHover,
		zIndex: true,
		nodeReducer,
		edgeReducer,
	});

	bindInteractions();
	// 标签逐帧排布：每帧清空占位表，才让「同一帧内先放的标签优先」成立
	sigma.on('afterRender', () => {
		labelBoxes.length = 0;
	});
	runLayout();
	startBreathing();
};

const bindInteractions = () => {
	if (!sigma || !graph || !canvasRef.value) return;
	const container = canvasRef.value;

	sigma.on('enterNode', ({ node }) => setFocus(node));
	sigma.on('leaveNode', () => setFocus(null));
	sigma.on('clickNode', ({ node }) => {
		state.selected = node;
		updateHighlight(node);
	});
	sigma.on('clickStage', () => {
		state.selected = null;
		updateHighlight(null);
	});
	sigma.on('doubleClickNode', ({ node, event }) => {
		event.preventSigmaDefault();
		focusNode(node);
	});

	// 节点拖拽：手动微调位置，点「重新排布」可以回到自动布局
	sigma.on('downNode', ({ node }) => {
		dragging = true;
		draggedNode = node;
	});
	container.addEventListener('mousemove', handleDragMove);
	container.addEventListener('mouseup', handleDragEnd);
	container.addEventListener('mouseleave', handleDragEnd);
};

const handleDragMove = (event: MouseEvent) => {
	if (!dragging || !draggedNode || !graph || !sigma) return;
	const position = sigma.viewportToGraph(event);
	graph.setNodeAttribute(draggedNode, 'x', position.x);
	graph.setNodeAttribute(draggedNode, 'y', position.y);
	event.preventDefault();
	event.stopPropagation();
};

const handleDragEnd = () => {
	dragging = false;
	draggedNode = null;
};

/** 每点一次「重新排布」换一个种子：分块的散开方向略有不同，但整体结构稳定 */
let layoutSeed = 1;

/** 由节点 Id 生成 [0,1) 的稳定随机数：同一个节点每次布局落在同一侧，不会来回跳 */
const stableRandom = (seedText: string) => {
	let hash = 2166136261;
	const text = `${layoutSeed}:${seedText}`;
	for (let i = 0; i < text.length; i++) {
		hash ^= text.charCodeAt(i);
		hash = Math.imul(hash, 16777619);
	}
	return ((hash >>> 0) % 10000) / 10000;
};

/**
 * 自动布局：知识库根节点居中，每个语义簇占据一个扇形，文档排在扇形的圆弧上，
 * 分块以黄金角螺旋散在所属文档周围。
 * 这里刻意不用力导向：结果不确定（每次刷新长得不一样）、簇会被斥力甩成孤岛、
 * 节点一多就退化成一团毛球；放射状的阅读顺序反而更接近「先看有哪几类、再看具体文档」的直觉。
 */
const runLayout = () => {
	if (!graph || !sigma) return;
	const docsByCluster = new Map<number, string[]>();
	const chunksByDoc = new Map<number, string[]>();
	let docCount = 0;

	graph.forEachNode((id, attributes) => {
		const kind = attributes.kind as KmsGraphNodeType;
		if (kind === 'kms') return;
		if (kind === 'doc') {
			docCount++;
			const cluster = Number(attributes.cluster ?? 0);
			if (!docsByCluster.has(cluster)) docsByCluster.set(cluster, []);
			docsByCluster.get(cluster)!.push(id);
			return;
		}
		const docId = Number(attributes.docId ?? 0);
		if (!chunksByDoc.has(docId)) chunksByDoc.set(docId, []);
		chunksByDoc.get(docId)!.push(id);
	});

	const clusterKeys = [...docsByCluster.keys()].sort((a, b) => a - b);
	const sectors = Math.max(1, clusterKeys.length);
	// 文档所在的圆弧半径随文档数增长：簇多、文档多时整体外扩，保证每个簇都有地方放
	// 不显示分块时没有外围的块云，圆弧要收得更紧，否则几个文档会散在画布四个角上
	const hasChunks = graph.order > docCount + 1;
	const docRadius = (hasChunks ? 150 : 92) + Math.min(120, Math.sqrt(docCount) * 22);
	const sectorWidth = (Math.PI * 2) / sectors;

	clusterKeys.forEach((cluster, index) => {
		const docs = docsByCluster.get(cluster)!;
		const sectorCenter = index * sectorWidth - Math.PI / 2;
		docs.forEach((docNode, docIndex) => {
			// 同簇文档保持固定角距而不是铺满整个扇形：它们之间的分块是互相连边的，
			// 拉得越开，跨文档的语义边就越长、越像「乱线」，紧一点才看得出是一团
			const offset = (docIndex - (docs.length - 1) / 2) * 0.68;
			const angle = sectorCenter + offset;
			// 同一簇里的文档错开半径：都在同一圈上时，扇形边缘的两篇会挤在一起
			const radius = docRadius + (docIndex % 2 === 0 ? 0 : 46);
			const x = Math.cos(angle) * radius;
			const y = Math.sin(angle) * radius;
			// 标签沿簇扇区方向朝外排：同簇文档的标签像扇骨一样分开，不会互相压字
			graph!.mergeNodeAttributes(docNode, { x, y, labelDir: { x: Math.cos(angle), y: Math.sin(angle) } });

			const docId = Number(graph!.getNodeAttribute(docNode, 'docId') ?? 0);
			const chunks = chunksByDoc.get(docId) ?? [];
			chunks.forEach((chunkNode, chunkIndex) => {
				const spread = 16 + Math.min(72, 10 * Math.sqrt(chunkIndex + 1));
				const a = chunkIndex * 2.399963 + stableRandom(chunkNode) * Math.PI * 2;
				const scale = 0.75 + stableRandom(`${chunkNode}-r`) * 0.5;
				graph!.mergeNodeAttributes(chunkNode, {
					x: x + Math.cos(a) * spread * scale,
					y: y + Math.sin(a) * spread * scale,
				});
			});
		});
	});

	// 知识库根节点放在正中心：所有归属边从它出发，居中后整张图才稳
	graph.forEachNode((id, attributes) => {
		if (attributes.kind !== 'kms') return;
		graph!.mergeNodeAttributes(id, { x: 0, y: 0, labelDir: { x: 0, y: 1 } });
	});

	enterScene();
};

/** 重新排布：换一个种子再摆一次，整体结构不变、分块的散开方式略有不同 */
const relayout = () => {
	layoutSeed += 1;
	runLayout();
};

/* ------------------------------ 交互 ------------------------------ */

const setFocus = (node: string | null) => {
	if (!sigma) return;
	hovered.value = node;
	updateHighlight(node ?? state.selected);
};

const updateHighlight = (node: string | null) => {
	if (!graph) return;
	const next = new Set<string>();
	if (node && graph.hasNode(node)) {
		next.add(node);
		for (const neighbor of graph.neighbors(node)) next.add(neighbor);
	}
	highlighted.value = next;
	sigma?.refresh();
};

const selectNode = (id: string) => {
	state.selected = id;
	updateHighlight(id);
	focusNode(id);
};

/**
 * 只看某一簇：点已经选中的簇则取消筛选。切换后把相机对准该簇，
 * 否则画布停在原处、被淡出的节点占据大片空白，看起来像「筛选没生效」
 */
const focusCluster = (cluster: number | null) => {
	state.activeCluster = state.activeCluster === cluster ? null : cluster;
	sigma?.refresh();
	if (state.activeCluster === null || !graph || !sigma) {
		sigma?.getCamera().animatedReset({ duration: 380 });
		return;
	}

	const positions: Array<{ x: number; y: number }> = [];
	graph.forEachNode((id, attributes) => {
		if (attributes.kind === 'kms' || Number(attributes.cluster) !== state.activeCluster) return;
		const position = sigma!.getNodeDisplayData(id);
		if (position) positions.push(position);
	});
	if (!positions.length) return;

	const minX = Math.min(...positions.map((u) => u.x));
	const maxX = Math.max(...positions.map((u) => u.x));
	const minY = Math.min(...positions.map((u) => u.y));
	const maxY = Math.max(...positions.map((u) => u.y));
	const span = Math.max(maxX - minX, maxY - minY);
	// ratio 是「可见范围 / 全图范围」：簇越小缩放越深，但留 0.32 的上限防止糊脸
	const ratio = Math.max(0.32, Math.min(0.8, span / 520));
	sigma.getCamera().animate({ x: (minX + maxX) / 2, y: (minY + maxY) / 2, ratio }, { duration: 420 });
};

const selectDoc = (doc: { id: string | number }) => selectNode(`doc-${doc.id}`);

/** 相机聚焦到节点，并保留一点上下文（ratio 太小会看不到邻居） */
const focusNode = (id: string) => {
	if (!sigma || !graph || !graph.hasNode(id)) return;
	const position = sigma.getNodeDisplayData(id);
	if (!position) return;
	// 0.4 大约能看到「节点 + 同簇邻居」，再深一点就把上下文裁没了，画面会显得局促
	sigma.getCamera().animate({ x: position.x, y: position.y, ratio: 0.4 }, { duration: 420 });
};

const zoom = (factor: number) => {
	if (!sigma) return;
	const camera = sigma.getCamera();
	camera.animatedZoom({ factor, duration: 260 });
};

/**
 * 相机适配到整张图：布局以知识库根节点为原点，所以直接回到原点即可。
 * ratio 取 1.12 而不是 1 —— 1 是「边界刚好贴住画布」，边缘节点的标签会被裁掉一截。
 */
const fitCamera = (duration = 420) => {
	// Sigma 的相机坐标是「相对整图包围盒的归一化坐标」：0.5/0.5 就是包围盒中心，
	// ratio 1 = 刚好放下整张图，放大到 1.12 就是四周留出约 6% 的边距
	sigma?.getCamera().animate({ x: 0.5, y: 0.5, ratio: 1.12, angle: 0 }, { duration });
};

const resetCamera = () => fitCamera(380);

/* ------------------------------ 动效与画布绘制 ------------------------------ */

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

/** 单个节点的入场可见度：知识库先亮、文档次之、分块最后，整体错峰但不拖长总时长 */
const revealAlpha = (attributes: Record<string, any>) => {
	if (reveal.value >= 1) return 1;
	const elapsed = reveal.value * REVEAL_MS;
	const delay = attributes.kind === 'kms' ? 0 : attributes.kind === 'doc' ? 90 : 220;
	return easeOutCubic(Math.min(1, Math.max(0, (elapsed - delay) / 440)));
};

/** 入场：相机从稍远处推近，配合节点逐层亮起，打开时像镜头推进而不是一张静态图 */
const enterScene = () => {
	if (!sigma) return;
	if (prefersReducedMotion()) {
		reveal.value = 1;
		fitCamera(0);
		return;
	}
	const camera = sigma.getCamera();
	camera.setState({ x: 0.5, y: 0.5, ratio: 1.62, angle: 0 });
	camera.animate({ x: 0.5, y: 0.5, ratio: 1.12, angle: 0 }, { duration: 1100 });
	playReveal();
};

const playReveal = () => {
	cancelAnimationFrame(revealFrame);
	const start = performance.now();
	reveal.value = 0;
	const tick = (now: number) => {
		reveal.value = Math.min(1, (now - start) / REVEAL_MS);
		sigma?.refresh();
		if (reveal.value < 1) revealFrame = requestAnimationFrame(tick);
	};
	revealFrame = requestAnimationFrame(tick);
};

/**
 * 呼吸重绘：约 25fps，肉眼依然连续，功耗只有 60fps 的一半。
 * 页面切到后台时跳过重绘，回到前台自然接上，不会积压。
 */
const startBreathing = () => {
	cancelAnimationFrame(breatheFrame);
	if (prefersReducedMotion()) return;
	breatheLast = 0;
	const tick = (now: number) => {
		breatheFrame = requestAnimationFrame(tick);
		if (!sigma || document.hidden) return;
		if (now - breatheLast < 40) return;
		breatheLast = now;
		breathePhase = (Math.sin(now / 1400) + 1) / 2;
		sigma.refresh();
	};
	breatheFrame = requestAnimationFrame(tick);
};

const stopMotion = () => {
	cancelAnimationFrame(revealFrame);
	cancelAnimationFrame(breatheFrame);
	revealFrame = 0;
	breatheFrame = 0;
	reveal.value = 1;
	breathePhase = 0;
};

/**
 * 标签：Obsidian 风格的纯文字标签，默认压在节点下方；文档节点按布局方向朝簇外排
 * （同簇文档的标签像扇骨一样分开）。用底色描边代替 Sigma 默认的白色气泡，
 * 深色主题下不会出现「白底贴片盖住节点」。
 */
const drawNodeLabel = (context: CanvasRenderingContext2D, data: Record<string, any>, settings: Record<string, any>) => {
	const label = data.label;
	if (!label) return;
	// 节点长到大约一半时标签才开始淡入，避免文字比节点先出现
	const alpha = reveal.value < 1 ? Math.max(0, (reveal.value - 0.45) / 0.55) : 1;
	if (alpha <= 0) return;
	const size = Number(settings.labelSize) || 12;
	const dir = data.labelDir ?? { x: 0, y: 1 };
	const gap = data.size + size * 0.75;

	context.save();
	context.font = `${settings.labelWeight} ${size}px ${settings.labelFont}`;
	const width = context.measureText(label).width;
	const height = size * 1.25;
	const place = (ux: number, uy: number, dy = 0) => {
		const x = data.x + ux * gap;
		const y = data.y + uy * gap + dy;
		const align = Math.abs(ux) > 0.45 ? (ux > 0 ? 'left' : 'right') : 'center';
		const left = align === 'left' ? x : align === 'right' ? x - width : x - width / 2;
		return { x, y, align, box: { x1: left - 3, y1: y - height / 2, x2: left + width + 3, y2: y + height / 2 } };
	};
	// 同簇文档常常挨得很近，标签先按扇骨方向放；撞了就对侧、再不行上下让一格，
	// 都放不下才放弃（分块标签放弃得最干脆，文档尽量留着）
	const candidates = [place(dir.x, dir.y), place(-dir.x, -dir.y), place(dir.x, dir.y, -height * 1.5), place(dir.x, dir.y, height * 1.5)];
	const chosen = candidates.find((c) => !labelBoxes.some((box) => c.box.x1 < box.x2 && c.box.x2 > box.x1 && c.box.y1 < box.y2 && c.box.y2 > box.y1));
	if (!chosen && data.kind === 'chunk') {
		context.restore();
		return;
	}
	const spot = chosen ?? candidates[0];
	labelBoxes.push(spot.box);

	context.globalAlpha = alpha;
	context.textAlign = spot.align as CanvasTextAlign;
	context.textBaseline = 'middle';
	context.lineJoin = 'round';
	context.lineWidth = 3.5;
	context.strokeStyle = labelHaloColor;
	context.strokeText(label, spot.x, spot.y);
	context.fillStyle = settings.labelColor?.color || '#303133';
	context.fillText(label, spot.x, spot.y);
	context.restore();
};

/** 悬停：一圈柔光 + 细描边，像放大镜落在节点上；颜色跟随节点，簇色一眼能对上 */
const drawNodeHover = (context: CanvasRenderingContext2D, data: Record<string, any>) => {
	const color = String(data.color || '#909399');
	const radius = Number(data.size) || 6;
	const glowRadius = radius * 3.6;
	const glow = context.createRadialGradient(data.x, data.y, radius * 0.4, data.x, data.y, glowRadius);
	glow.addColorStop(0, toRgba(color, 0.34));
	glow.addColorStop(0.45, toRgba(color, 0.12));
	glow.addColorStop(1, toRgba(color, 0));
	context.beginPath();
	context.arc(data.x, data.y, glowRadius, 0, Math.PI * 2);
	context.fillStyle = glow;
	context.fill();

	context.beginPath();
	context.arc(data.x, data.y, radius + 5, 0, Math.PI * 2);
	context.lineWidth = 1.2;
	context.strokeStyle = toRgba(color, 0.5);
	context.stroke();
};

const copyContent = async () => {
	const content = selectedNode.value?.content;
	if (!content) return;
	try {
		await navigator.clipboard.writeText(content);
		ElMessage.success('正文已复制');
	} catch {
		ElMessage.warning('当前环境不支持自动复制，请手动选择文本');
	}
};

/* ------------------------------ reducer ------------------------------ */

/**
 * 节点样式：簇筛选 / 搜索 / 悬停三者取交集，不满足的节点统一淡出并隐藏标签。
 * 命中悬停或搜索的节点强制显示标签，分块节点平时不显示文字，靠这一步在交互时补上。
 */
const nodeReducer = (node: string, data: Record<string, any>) => {
	const result: Record<string, any> = { ...data };
	const keyword = state.keyword.trim();
	const baseColor = String(result.color ?? '#909399');

	// 入场揭示：按层级错峰放大 + 淡入
	const appear = revealAlpha(result);
	if (appear < 1) {
		result.size = result.size * (0.4 + 0.6 * appear);
		result.color = toRgba(baseColor, appear);
	}
	// 呼吸：根节点与选中节点轻微起伏，画面保持「活着」的感觉
	if (result.kind === 'kms') result.size = result.size * (1 + breathePhase * 0.09);
	if (node === state.selected) result.size = result.size * (1 + breathePhase * 0.06);

	const inCluster = state.activeCluster === null || result.kind === 'kms' || Number(result.cluster) === state.activeCluster;
	const inSearch = !keyword || searchMatches.value.has(node);
	const inFocus = highlighted.value.size === 0 || highlighted.value.has(node);

	if (!inCluster || !inSearch || !inFocus) {
		result.color = toRgba(baseColor, 0.05);
		result.label = '';
		result.zIndex = 0;
		return result;
	}

	if (highlighted.value.has(node)) {
		// 分块只在悬停或选中自身时挂文字：一个文档的邻居分块动辄十几个，标签全开会糊成一片。
		// 放大后节点尺寸会越过 labelRenderedSizeThreshold 自动出标签，所以要连 label 一起清掉
		const chunkQuiet = result.kind === 'chunk' && node !== hovered.value && node !== state.selected;
		if (chunkQuiet) result.label = '';
		result.forceLabel = !chunkQuiet;
		result.zIndex = node === state.selected ? 6 : 4;
	}
	if (keyword && searchMatches.value.has(node)) {
		// 分块命中时只提亮不挂标签：一个关键词往往命中几十个块，标签会糊成一片，
		// 想看清是哪一块，悬停即可（悬停分支会强制显示标签）
		if (result.kind === 'chunk') result.label = '';
		result.forceLabel = result.kind !== 'chunk';
		result.zIndex = 5;
	}
	return result;
};

/** 边样式：按边类型 / 相似度阈值过滤，聚焦与搜索时只保留相关边 */
const edgeReducer = (edge: string, data: Record<string, any>) => {
	const result: Record<string, any> = { ...data };
	if (data.edgeType === 'contains' && !state.showContains) result.hidden = true;
	if (data.edgeType === 'semantic' && (!state.showSemantic || (data.weight ?? 1) < state.minScore)) result.hidden = true;
	if (result.hidden || !graph) return result;

	// 入场：边比节点晚一点亮起，先有节点再连线，视觉顺序更像「关系被推导出来」
	if (reveal.value < 1) {
		result.color = toRgba(String(data.baseColor ?? '#909399'), Number(data.baseAlpha ?? 0.3) * easeOutCubic(reveal.value));
	}

	const source = graph.source(edge);
	const target = graph.target(edge);
	const keyword = state.keyword.trim();

	if (highlighted.value.size > 0 && (!highlighted.value.has(source) || !highlighted.value.has(target))) {
		result.hidden = true;
		return result;
	}

	// 簇筛选：只要有一端不属于当前簇就隐藏（知识库根节点是公共端点，不参与判断），
	// 否则边会连向已被淡出的节点，画布上留下一堆悬空的线头
	if (state.activeCluster !== null) {
		const inActive = (id: string): boolean => {
			const attributes = graph!.getNodeAttributes(id);
			return attributes.kind === 'kms' || Number(attributes.cluster) === state.activeCluster;
		};
		if (!inActive(source) || !inActive(target)) {
			result.hidden = true;
			return result;
		}
	}

	// 搜索：无关的边淡出而不是隐藏，保留一点上下文结构
	if (keyword && (!searchMatches.value.has(source) || !searchMatches.value.has(target))) {
		result.color = toRgba(String(data.color ?? '#909399'), 0.05);
	}
	return result;
};

/** 搜索命中的节点：文档名、分块正文都参与匹配 */
const searchMatches = computed(() => {
	const keyword = state.keyword.trim().toLowerCase();
	const matches = new Set<string>();
	if (!keyword || !graph) return matches;
	graph.forEachNode((id, attributes) => {
		const text = `${attributes.label ?? ''} ${attributes.docName ?? ''} ${attributes.content ?? ''}`.toLowerCase();
		if (text.includes(keyword)) matches.add(id);
	});
	return matches;
});

/* ------------------------------ 生命周期 ------------------------------ */

const destroySigma = () => {
	stopMotion();
	if (sigma) {
		if (canvasRef.value) {
			canvasRef.value.removeEventListener('mousemove', handleDragMove);
			canvasRef.value.removeEventListener('mouseup', handleDragEnd);
			canvasRef.value.removeEventListener('mouseleave', handleDragEnd);
		}
		sigma.kill();
		sigma = null;
	}
	graph = null;
	rootId = null;
	highlighted.value = new Set();
	hovered.value = null;
	dragging = false;
	draggedNode = null;
};

const handleOpened = async () => {
	await nextTick();
	await load();
};

const handleClosed = () => {
	destroySigma();
	state.data = null;
	state.error = '';
	state.warning = '';
	state.selected = null;
	state.keyword = '';
	state.activeCluster = null;
};

watch(
	() => [state.includeChunks, state.maxChunks],
	() => {
		if (visible.value) load();
	}
);

watch(
	() => [state.showContains, state.showSemantic, state.minScore],
	() => sigma?.refresh()
);

watch(searchMatches, () => sigma?.refresh());

// 搜索与簇筛选是两套聚光灯，同时开着只会把结果取空：开始搜索就自动退出簇筛选
watch(
	() => state.keyword,
	(value) => {
		if (value.trim() && state.activeCluster !== null) {
			state.activeCluster = null;
			sigma?.refresh();
		}
	}
);

watch(
	() => state.selected,
	(value) => updateHighlight(value)
);

onBeforeUnmount(destroySigma);
</script>

<style scoped>
/* 整页三栏：左设置 / 中画布 / 右详情，各自滚动，文档级不出现滚动条 */
.kg-shell {
	display: flex;
	flex-direction: column;
	height: 100vh;
	overflow: hidden;
	background: var(--el-bg-color);
}

/* ------------------------------ 顶栏 ------------------------------ */
.kg-head {
	flex-shrink: 0;
	display: flex;
	align-items: center;
	gap: 20px;
	height: 60px;
	padding: 0 16px;
	background: linear-gradient(120deg, color-mix(in srgb, var(--el-color-primary) 6%, var(--el-bg-color)) 0%, var(--el-bg-color) 40%);
	border-bottom: 1px solid color-mix(in srgb, var(--el-color-primary) 14%, var(--el-border-color-lighter));
}

.kg-head__main {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
}

.kg-head__icon {
	flex-shrink: 0;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	color: #fff;
	background: linear-gradient(140deg, color-mix(in srgb, var(--el-color-primary) 82%, #fff) 0%, var(--el-color-primary) 100%);
	box-shadow: 0 8px 18px -12px var(--el-color-primary);
}

.kg-head__text {
	min-width: 0;
}

.kg-head__text h3 {
	margin: 0;
	font-size: 15px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.kg-head__text p {
	margin: 2px 0 0;
	font-size: 12px;
	line-height: 16px;
	color: var(--el-text-color-secondary);
}

.kg-head__stats {
	display: flex;
	align-items: center;
	gap: 16px;
	margin-left: auto;
}

/* 指标用竖线分组，不再用浅灰胶囊：白底上更干净，也更像一块仪表盘 */
.kg-stat {
	position: relative;
	display: flex;
	align-items: baseline;
	gap: 5px;
	height: 30px;
}

.kg-stat + .kg-stat::before {
	content: '';
	position: absolute;
	left: -9px;
	top: 7px;
	width: 1px;
	height: 16px;
	background: var(--el-border-color-lighter);
}

.kg-stat b {
	font-size: 16px;
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-primary);
}

.kg-stat span {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.kg-head__ops {
	display: flex;
	align-items: center;
	gap: 8px;
}

/* ------------------------------ 主体 ------------------------------ */
.kg-body {
	flex: 1 1 auto;
	display: flex;
	min-height: 0;
}

/* 左右两栏统一 12px 内边距；底色带一点主题色晕染，白色面板才有层次 */
.kg-side {
	flex-shrink: 0;
	display: flex;
	flex-direction: column;
	gap: 10px;
	width: 252px;
	padding: 12px;
	overflow: hidden;
	background: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 4%, var(--el-bg-color)) 0%, var(--el-bg-color) 46%);
	border-right: 1px solid var(--el-border-color-lighter);
}

.kg-panel {
	display: flex;
	flex-direction: column;
	gap: 10px;
	flex-shrink: 0;
	padding: 12px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
	box-shadow: 0 1px 2px color-mix(in srgb, var(--el-text-color-primary) 5%, transparent);
}

.kg-panel.is-grow {
	flex: 1 1 auto;
	min-height: 0;
}

.kg-panel__title {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.kg-panel__title::before {
	content: '';
	width: 3px;
	height: 12px;
	border-radius: 2px;
	background: linear-gradient(180deg, var(--el-color-primary) 0%, color-mix(in srgb, var(--el-color-primary) 45%, transparent) 100%);
}

.kg-panel__title em {
	font-style: normal;
	font-weight: 500;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);
}

.kg-field {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	font-size: 12.5px;
	color: var(--el-text-color-regular);
}

.kg-field.is-block {
	display: block;
}

.kg-field__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 2px;
}

.kg-field__head b {
	font-size: 12.5px;
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	color: var(--el-color-primary);
}

.kg-select {
	width: 96px;
}

.kg-chips {
	display: flex;
	gap: 8px;
}

.kg-chip {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	height: 26px;
	padding: 0 10px;
	border-radius: 7px;
	font-size: 12px;
	cursor: pointer;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
	border: 1px solid transparent;
	transition:
		color 0.2s ease,
		border-color 0.2s ease,
		background-color 0.2s ease;
}

.kg-chip:hover {
	color: var(--el-text-color-primary);
}

.kg-chip.is-on {
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
	border-color: color-mix(in srgb, var(--el-color-primary) 26%, transparent);
}

.kg-chip__line {
	width: 14px;
	height: 2px;
	border-radius: 1px;
	background: var(--el-border-color-darker);
}

.kg-chip__line.is-semantic {
	background: var(--el-color-primary);
}

/* 文档导航 */
.kg-doclist {
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
	overflow-y: auto;
	margin: 0 -4px;
	padding: 0 4px;
}

.kg-doc {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
	width: 100%;
	height: 32px;
	padding: 0 8px;
	border: 1px solid transparent;
	border-radius: 8px;
	background: transparent;
	cursor: pointer;
	text-align: left;
	transition:
		background-color 0.18s ease,
		border-color 0.18s ease;
}

.kg-doc:hover {
	background: var(--el-fill-color-light);
}

.kg-doc.is-active {
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
	border-color: color-mix(in srgb, var(--el-color-primary) 24%, transparent);
}

.kg-doc__dot {
	flex-shrink: 0;
	width: 8px;
	height: 8px;
	border-radius: 50%;
}

.kg-doc__name {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12.5px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.kg-doc__count {
	flex-shrink: 0;
	font-size: 11.5px;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);
}

.kg-doc__count em {
	font-style: normal;
	opacity: 0.6;
}

.kg-inline-empty {
	padding: 12px 4px;
	font-size: 12px;
	text-align: center;
	color: var(--el-text-color-secondary);
}

.kg-truncate-tip {
	display: flex;
	align-items: flex-start;
	gap: 5px;
	flex-shrink: 0;
	margin: 0;
	font-size: 11.5px;
	line-height: 16px;
	color: var(--el-text-color-secondary);
}

/* ------------------------------ 画布 ------------------------------ */
/* 底色不再用灰：主题色极光 + 近白渐变，Sigma 的 WebGL 画布是透明的，直接透出这一层 */
.kg-stage {
	position: relative;
	flex: 1 1 auto;
	min-width: 0;
	overflow: hidden;
	isolation: isolate;
	background:
		radial-gradient(1100px 720px at 14% 4%, color-mix(in srgb, var(--el-color-primary) 18%, transparent), transparent 62%),
		radial-gradient(920px 640px at 88% 94%, color-mix(in srgb, var(--el-color-primary) 12%, transparent), transparent 60%),
		radial-gradient(760px 560px at 74% 8%, color-mix(in srgb, #38bdf8 10%, transparent), transparent 64%),
		linear-gradient(158deg, #feffff 0%, #f6fbf8 48%, #eff8f4 100%);
}

/* 点阵：只在中部可见、向四周淡出，像一层纸的纹理而不是网格 */
.kg-stage::before {
	content: '';
	position: absolute;
	inset: 0;
	z-index: 0;
	background-image: radial-gradient(circle, color-mix(in srgb, var(--el-color-primary) 30%, transparent) 1px, transparent 1px);
	background-size: 22px 22px;
	opacity: 0.45;
	-webkit-mask-image: radial-gradient(ellipse 76% 70% at 50% 46%, #000 28%, transparent 82%);
	mask-image: radial-gradient(ellipse 76% 70% at 50% 46%, #000 28%, transparent 82%);
	pointer-events: none;
}

/* 极光：三团缓慢漂移的色光，给静止的图谱一点呼吸感 */
.kg-aurora {
	position: absolute;
	inset: 0;
	z-index: 0;
	overflow: hidden;
	pointer-events: none;
}

.kg-aurora__blob {
	position: absolute;
	display: block;
	border-radius: 50%;
	filter: blur(28px);
	will-change: transform;
	animation: kg-drift 24s ease-in-out infinite alternate;
}

.kg-aurora__blob.is-a {
	left: -6%;
	top: -10%;
	width: 46%;
	height: 54%;
	background: radial-gradient(circle, color-mix(in srgb, var(--el-color-primary) 30%, transparent) 0%, transparent 68%);
}

.kg-aurora__blob.is-b {
	right: -8%;
	bottom: -14%;
	width: 44%;
	height: 56%;
	background: radial-gradient(circle, color-mix(in srgb, var(--el-color-primary) 22%, transparent) 0%, transparent 70%);
	animation-duration: 30s;
	animation-delay: -6s;
}

.kg-aurora__blob.is-c {
	left: 42%;
	top: 0;
	width: 38%;
	height: 42%;
	background: radial-gradient(circle, color-mix(in srgb, #38bdf8 20%, transparent) 0%, transparent 70%);
	animation-duration: 36s;
	animation-delay: -14s;
}

@keyframes kg-drift {
	0% {
		transform: translate3d(0, 0, 0) scale(1);
	}
	50% {
		transform: translate3d(3%, 2%, 0) scale(1.08);
	}
	100% {
		transform: translate3d(-2%, 4%, 0) scale(1.02);
	}
}

.kg-canvas {
	position: absolute;
	inset: 0;
	z-index: 1;
}

/* 暗角：把视线收拢到中心，边缘留一点主题色阴影，画面更有纵深 */
.kg-veil {
	position: absolute;
	inset: 0;
	z-index: 2;
	pointer-events: none;
	background: radial-gradient(120% 96% at 50% 46%, transparent 56%, color-mix(in srgb, #103a2e 8%, transparent) 100%);
}

/* 画布浮层统一玻璃质感：半透明 + 模糊 + 主题色描边，压在极光上依然是「一块浮起来的玻璃」 */
.kg-legend {
	position: absolute;
	left: 14px;
	bottom: 14px;
	z-index: 3;
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 7px 12px;
	border-radius: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	background: color-mix(in srgb, var(--el-bg-color) 76%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 16%, var(--el-border-color-lighter));
	backdrop-filter: blur(14px) saturate(1.15);
	box-shadow: 0 14px 34px -28px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
	pointer-events: none;
	animation: kg-rise 0.5s cubic-bezier(0.22, 1, 0.36, 1) 0.06s both;
}

.kg-legend__item {
	display: inline-flex;
	align-items: center;
	gap: 5px;
}

.kg-legend__item i {
	width: 8px;
	height: 8px;
	border-radius: 50%;
}

/* 图例里的圆点只表达「节点大小」，颜色一律交给簇色，避免图例和画布各说一套 */
.kg-legend__item i.is-kms {
	width: 10px;
	height: 10px;
	background: var(--el-text-color-primary);
}

.kg-legend__item i.is-doc {
	width: 8px;
	height: 8px;
	background: var(--el-text-color-regular);
}

.kg-legend__item i.is-chunk {
	width: 5px;
	height: 5px;
	background: var(--el-text-color-placeholder);
}

.kg-legend__note {
	font-size: 11.5px;
	color: var(--el-text-color-placeholder);
}

/* 聚类筛选：贴在画布左上角，横向滚动的胶囊组，簇多时不占画布高度 */
.kg-clusters {
	position: absolute;
	top: 14px;
	left: 14px;
	z-index: 3;
	display: flex;
	align-items: center;
	gap: 6px;
	max-width: calc(100% - 120px);
	padding: 4px;
	border-radius: 10px;
	overflow-x: auto;
	background: color-mix(in srgb, var(--el-bg-color) 76%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 16%, var(--el-border-color-lighter));
	backdrop-filter: blur(14px) saturate(1.15);
	box-shadow: 0 14px 34px -28px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
	scrollbar-width: none;
	animation: kg-rise 0.42s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.kg-clusters::-webkit-scrollbar {
	display: none;
}

.kg-cluster {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	flex-shrink: 0;
	height: 26px;
	padding: 0 9px;
	border: 1px solid transparent;
	border-radius: 7px;
	font-size: 12px;
	white-space: nowrap;
	cursor: pointer;
	color: var(--el-text-color-secondary);
	background: transparent;
	transition:
		color 0.18s ease,
		background-color 0.18s ease,
		border-color 0.18s ease;
}

.kg-cluster:hover {
	color: var(--el-text-color-primary);
	background: var(--el-fill-color-light);
}

.kg-cluster.is-on {
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
	border-color: color-mix(in srgb, var(--el-color-primary) 26%, transparent);
}

.kg-cluster i {
	width: 7px;
	height: 7px;
	border-radius: 50%;
}

.kg-cluster span {
	font-size: 11px;
	font-variant-numeric: tabular-nums;
	opacity: 0.65;
}

.kg-legend__item i.is-line {
	width: 14px;
	height: 2px;
	border-radius: 1px;
	background: var(--el-border-color-darker);
}

.kg-legend__item i.is-line.is-semantic {
	background: var(--el-color-primary);
}

.kg-legend__sep {
	width: 1px;
	height: 12px;
	background: var(--el-border-color-lighter);
}

.kg-tools {
	position: absolute;
	right: 14px;
	top: 14px;
	z-index: 3;
	display: flex;
	flex-direction: column;
	gap: 2px;
	padding: 3px;
	border-radius: 10px;
	background: color-mix(in srgb, var(--el-bg-color) 76%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 16%, var(--el-border-color-lighter));
	backdrop-filter: blur(14px) saturate(1.15);
	box-shadow: 0 14px 34px -28px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
	animation: kg-rise 0.5s cubic-bezier(0.22, 1, 0.36, 1) 0.1s both;
}

.kg-hint {
	position: absolute;
	right: 14px;
	bottom: 14px;
	z-index: 3;
	padding: 5px 10px;
	border-radius: 8px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
	background: color-mix(in srgb, var(--el-bg-color) 72%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 14%, var(--el-border-color-lighter));
	backdrop-filter: blur(12px);
	pointer-events: none;
	animation: kg-rise 0.5s cubic-bezier(0.22, 1, 0.36, 1) 0.14s both;
}

@keyframes kg-rise {
	from {
		opacity: 0;
		transform: translateY(8px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.kg-banner {
	position: absolute;
	top: 14px;
	left: 50%;
	z-index: 3;
	transform: translateX(-50%);
	display: flex;
	align-items: center;
	gap: 6px;
	max-width: min(560px, 70%);
	padding: 6px 12px;
	border-radius: 8px;
	font-size: 12px;
	color: var(--el-color-warning);
	background: color-mix(in srgb, var(--el-color-warning-light-9) 88%, transparent);
	border: 1px solid var(--el-color-warning-light-7);
	backdrop-filter: blur(10px);
}

/* 加载/错误遮罩：同样走玻璃 + 极光底，不再是一块灰板 */
.kg-mask {
	position: absolute;
	inset: 0;
	z-index: 5;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 10px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
	background: color-mix(in srgb, var(--el-bg-color) 78%, transparent);
	backdrop-filter: blur(10px) saturate(1.1);
}

.kg-mask__icon {
	color: var(--el-text-color-placeholder);
}

.kg-loader {
	width: 30px;
	height: 30px;
	border-radius: 50%;
	border: 2px solid color-mix(in srgb, var(--el-color-primary) 16%, transparent);
	border-top-color: var(--el-color-primary);
	border-right-color: color-mix(in srgb, var(--el-color-primary) 55%, transparent);
	box-shadow: 0 0 20px -8px var(--el-color-primary);
	animation: kg-spin 0.85s linear infinite;
}

@keyframes kg-spin {
	to {
		transform: rotate(360deg);
	}
}

/* ------------------------------ 详情 ------------------------------ */
.kg-detail {
	flex-shrink: 0;
	display: flex;
	flex-direction: column;
	gap: 8px;
	width: 316px;
	padding: 12px;
	overflow-y: auto;
	background: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 3%, var(--el-bg-color)) 0%, var(--el-bg-color) 42%);
	border-left: 1px solid var(--el-border-color-lighter);
}

.kg-detail__head {
	display: flex;
	align-items: center;
	gap: 8px;
}

.kg-detail__badge {
	height: 20px;
	line-height: 20px;
	padding: 0 8px;
	border-radius: 6px;
	font-size: 11.5px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.kg-detail__badge.is-doc {
	color: var(--el-color-success);
	background: color-mix(in srgb, var(--el-color-success) 12%, transparent);
}

.kg-detail__badge.is-chunk {
	color: var(--el-color-info);
	background: color-mix(in srgb, var(--el-color-info) 14%, transparent);
}

.kg-detail__cluster {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
}

.kg-detail__cluster i {
	width: 8px;
	height: 8px;
	border-radius: 50%;
}

.kg-detail__close {
	margin-left: auto;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 22px;
	height: 22px;
	border: none;
	border-radius: 6px;
	background: transparent;
	color: var(--el-text-color-secondary);
	cursor: pointer;
}

.kg-detail__close:hover {
	background: var(--el-fill-color-light);
	color: var(--el-text-color-primary);
}

.kg-detail__title {
	margin: 0;
	font-size: 14px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
	word-break: break-all;
}

.kg-detail__meta {
	display: flex;
	align-items: center;
	gap: 10px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
}

.kg-detail__meta span:first-child {
	flex: 1 1 auto;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.kg-detail__content {
	margin: 2px 0 0;
	padding: 10px;
	border-radius: 8px;
	font-size: 12.5px;
	line-height: 1.7;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-lighter);
	white-space: pre-wrap;
	word-break: break-word;
}

.kg-detail__section {
	margin-top: 2px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.kg-neighbors {
	display: flex;
	flex-direction: column;
	gap: 2px;
	margin-top: 6px;
}

.kg-nb {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
	height: 28px;
	padding: 0 8px;
	border: none;
	border-radius: 7px;
	background: transparent;
	cursor: pointer;
	text-align: left;
}

.kg-nb:hover {
	background: var(--el-fill-color-light);
}

.kg-nb i {
	flex-shrink: 0;
	width: 7px;
	height: 7px;
	border-radius: 50%;
}

.kg-nb span {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.kg-nb em {
	flex-shrink: 0;
	font-style: normal;
	font-size: 11px;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);
}

.kg-detail__ops {
	display: flex;
	gap: 8px;
	margin-top: auto;
	padding-top: 10px;
	border-top: 1px solid var(--el-border-color-lighter);
}

/* ------------------------------ 暗色主题 ------------------------------ */
/* 亮色是「近白 + 主题色极光」，暗色就是「深墨 + 同一束光」，不会退回灰色 */
[data-theme='dark'] .kg-stage {
	background:
		radial-gradient(1100px 720px at 14% 4%, color-mix(in srgb, var(--el-color-primary) 30%, transparent), transparent 62%),
		radial-gradient(920px 640px at 88% 94%, color-mix(in srgb, var(--el-color-primary) 18%, transparent), transparent 60%),
		radial-gradient(760px 560px at 74% 8%, color-mix(in srgb, #38bdf8 14%, transparent), transparent 64%),
		linear-gradient(158deg, #0a1411 0%, #070d0b 55%, #050a08 100%);
}

[data-theme='dark'] .kg-stage::before {
	background-image: radial-gradient(circle, color-mix(in srgb, var(--el-color-primary) 52%, transparent) 1px, transparent 1px);
	opacity: 0.34;
}

[data-theme='dark'] .kg-veil {
	background: radial-gradient(120% 96% at 50% 46%, transparent 52%, rgba(0, 0, 0, 0.5) 100%);
}

[data-theme='dark'] .kg-head {
	background: linear-gradient(120deg, color-mix(in srgb, var(--el-color-primary) 14%, var(--el-bg-color)) 0%, var(--el-bg-color) 46%);
}

[data-theme='dark'] .kg-side {
	background: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 6%, var(--el-bg-color)) 0%, var(--el-bg-color) 46%);
}

[data-theme='dark'] .kg-panel {
	box-shadow: 0 1px 2px rgba(0, 0, 0, 0.4);
}

[data-theme='dark'] .kg-legend,
[data-theme='dark'] .kg-clusters,
[data-theme='dark'] .kg-tools,
[data-theme='dark'] .kg-hint {
	background: color-mix(in srgb, var(--el-bg-color) 62%, transparent);
	border-color: color-mix(in srgb, var(--el-color-primary) 26%, transparent);
}

/* 关掉动效时保留最终状态，图层不得残留半透明或位移 */
@media (prefers-reduced-motion: reduce) {
	.kg-aurora__blob,
	.kg-clusters,
	.kg-legend,
	.kg-tools,
	.kg-hint,
	.kg-loader {
		animation: none !important;
	}
}
</style>

<style>
/* 全屏弹窗：全局样式里 .el-dialog__body 带 18px 内边距 + 限高滚动（!important），
 * 必须在同一链路上再加一层类名才能顶掉，否则画布四周会多出空白 */
.el-overlay .el-overlay-dialog .el-dialog.kg-dialog {
	height: 100vh;
	margin: 0;
	padding: 0;
	border-radius: 0;
}

.el-overlay .el-overlay-dialog .el-dialog.kg-dialog .el-dialog__header {
	display: none;
}

.el-overlay .el-overlay-dialog .el-dialog.kg-dialog .el-dialog__body {
	height: 100vh;
	max-height: none !important;
	padding: 0 !important;
	overflow: hidden;
}
</style>
