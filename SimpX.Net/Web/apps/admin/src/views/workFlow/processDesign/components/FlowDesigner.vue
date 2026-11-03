<template>
	<div class="flow-designer">
		<!-- 流程设计器：全屏弹窗，左侧画布 + 右侧属性面板 -->
		<el-dialog v-model="state.visible" fullscreen append-to-body :show-close="false" :close-on-click-modal="false" class="flow-designer-dialog" @opened="handleOpened" @close="handleDialogClose">
			<!-- 头部：品牌 + 状态 + 文档级动作 -->
			<template #header>
				<div class="fd-head">
					<div class="fd-head__brand">
						<span class="fd-head__logo"
							><el-icon><ele-Share /></el-icon
						></span>
						<div class="fd-head__text">
							<div class="fd-head__title">流程设计器</div>
							<div class="fd-head__sub">
								<span class="fd-head__status" :class="readonlyMode ? 'is-warning' : 'is-success'">{{ readonlyMode ? '只读查看' : '编辑模式' }}</span>
								<span v-if="currentFlow?.latestVersion" class="fd-head__version">v{{ currentFlow.latestVersion }}</span>
							</div>
						</div>
					</div>
					<div class="fd-head__actions">
						<el-button class="fd-btn-ghost" icon="ele-CircleClose" @click="handleCancel">{{ readonlyMode ? '关闭' : '取消编辑' }}</el-button>
						<el-button v-if="!readonlyMode" class="fd-btn-light" icon="ele-CircleCheck" :loading="state.saving" @click="handleSave">保存流程</el-button>
						<button class="fd-head__close" title="关闭" @click="handleCancel">
							<el-icon><ele-Close /></el-icon>
						</button>
					</div>
				</div>
			</template>

			<div class="fd-shell">
				<!-- 文档条：流程名 + 编码/分类 + 操作提示 -->
				<div class="fd-docbar">
					<div class="fd-docbar__name" :title="currentFlow?.name">{{ currentFlow?.name || state.title }}</div>
					<span v-if="currentFlow?.code" class="fd-docbar__chip">{{ currentFlow.code }}</span>
					<span v-if="currentFlow?.category" class="fd-docbar__chip is-soft">{{ currentFlow.category }}</span>
					<span class="fd-docbar__spacer"></span>
					<span class="fd-docbar__hint">
						<el-icon><ele-InfoFilled /></el-icon>
						<span>{{ readonlyMode ? '历史版本快照，仅供查看' : '改属性先「保存属性」，再「保存流程」' }}</span>
					</span>
				</div>

				<div class="fd-body">
					<!-- 左侧节点面板（只读模式隐藏）：不能依赖 lf 才挂载，否则画布会按无左栏的宽度初始化 -->
					<FlowNodePanel v-if="!readonlyMode" :lf="lf" />

					<!-- 画布 -->
					<div class="fd-canvas-wrap">
						<div class="canvas-wrapper" ref="canvasRef" />

						<!-- 悬浮工具条 -->
						<PanelControl v-if="lf" :lf="lf" @catData="previewGraph"></PanelControl>

						<!-- 右下角规模提示 -->
						<div class="fd-canvas-stat">{{ stats.nodes }} 个节点 · {{ stats.edges }} 条连线</div>

						<!-- 左下角状态条：框选开关 + 操作提示 -->
						<div class="fd-statusbar">
							<template v-if="!readonlyMode">
								<span class="fd-status-label">框选</span>
								<el-switch v-model="state.enableSelection" size="small" @change="toggleSelection" />
							</template>
							<span class="fd-status-hint">滚轮缩放 · 拖拽空白处平移画布</span>
						</div>

						<!-- 画布上的只读提示 -->
						<div v-if="readonlyMode" class="fd-readonly-badge">
							<el-icon><ele-Lock /></el-icon>
							<span>只读模式：当前为历史发布版本快照</span>
						</div>

						<!-- 图数据预览对话框 -->
						<el-dialog title="图数据" v-model="preview.visible" width="760px" append-to-body class="preview-data-dialog">
							<template #header>
								<div class="dialog-title">
									<el-icon size="16"><ele-DataLine /></el-icon>
									<span>图数据</span>
								</div>
							</template>
							<PanelDataDialog :graphData="preview.graphData" />
							<template #footer>
								<el-button icon="ele-CircleClose" @click="preview.visible = false">关闭</el-button>
							</template>
						</el-dialog>
					</div>

					<!-- 右侧属性面板 -->
					<aside class="fd-props">
						<PropertyDialog
							:lf="lf"
							:node-data="selectedElement"
							:form-json="formJson"
							:form-type="currentFlow?.formType ?? 0"
							:business-module="currentFlow?.businessModule"
							:readonly="readonlyMode"
						/>
					</aside>
				</div>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="WorkFlowFlowDesigner">
/**
 * 工作流流程设计器组件
 * 基于 LogicFlow 实现的流程图编辑器
 * 功能：
 * - 可视化编辑流程图（节点、连线）
 * - 节点/连线属性配置
 * - 流程保存与预览
 */
import { reactive, ref, shallowRef, nextTick, onBeforeUnmount } from 'vue';
import { ElMessage } from 'element-plus';
import LogicFlow from '@logicflow/core';
import { BpmnElement, InsertNodeInPolyline, Menu, MiniMap, SelectionSelect, Snapshot } from '@logicflow/extension';
import '@logicflow/core/dist/index.css';
import '@logicflow/extension/lib/style/index.css';
import XEUtils from 'xe-utils';

// 自定义组件导入
// @ts-ignore
import PanelControl from '/@/views/workFlow/processDesign/components/LogicFlow/Panel/PanelControl.vue';
// @ts-ignore
import PanelDataDialog from '/@/views/workFlow/processDesign/components/LogicFlow/Panel/PanelDataDialog.vue';
// @ts-ignore
import FlowNodePanel from './FlowNodePanel.vue';
import PropertyDialog from './PropertyDialog.vue';
import RegisterNode from '/@/views/workFlow/processDesign/components/LogicFlow/Register/RegisterNode';
import { saveFlowDesign } from '/@/api/workflow';
import { loadFlowSchemaJson } from '/@/views/workFlow/utils/flowForm';

// 组件事件定义
const emit = defineEmits(['saved']);

/**
 * 工作流流程项接口定义
 */
interface WorkflowFlowItem {
	id: number; // 流程ID
	name: string; // 流程名称
	code?: string; // 流程编码
	category?: string; // 分类
	flowJson?: string; // 流程图数据JSON
	formJson?: string; // 表单配置JSON
	formId?: number; // 表单ID
	formCode?: string; // 表单编码
	formName?: string; // 表单名称
	formType?: number; // 表单类型
	businessModule?: string; // 业务模块
	latestVersion?: number; // 最新发布版本号（头部状态区展示）
}

/**
 * 打开流程设计器的选项
 * readonly - 是否只读展示
 * title    - 弹窗标题（默认 "流程设计 - {flow.name}"）
 */
interface OpenDialogOptions {
	readonly?: boolean;
	title?: string;
}

// LogicFlow 实例引用（用 shallowRef：实例是大对象，避免被 Vue 深度响应式代理拖累性能）
const lf = shallowRef<InstanceType<typeof LogicFlow>>();
// 画布容器引用
const canvasRef = ref<HTMLElement>();
// 当前编辑的流程数据
const currentFlow = ref<WorkflowFlowItem>();
// 当前是否只读展示
const readonlyMode = ref(false);

/**
 * 组件状态管理
 */
const state = reactive({
	visible: false, // 对话框显示状态
	title: '流程设计', // 对话框标题
	enableSelection: false, // 是否开启框选功能
	graphData: { nodes: [], edges: [] } as any, // 流程图数据
	saving: false, // 保存中状态
});

/** 画布实时规模（头部展示，随增删节点/连线同步） */
const stats = reactive({ nodes: 0, edges: 0 });

/**
 * 预览对话框状态
 */
const preview = reactive({
	visible: false, // 显示状态
	graphData: {} as Record<string, any>, // 预览数据
});

// 当前选中的元素（节点或连线）
const selectedElement = ref<any>(null);
// 表单配置JSON字符串
const formJson = ref<string>('');

/**
 * 同步画布节点/连线数量
 */
const syncStats = () => {
	// 直接读 graphModel 的长度：getGraphData() 会做一次全图深拷贝，
	// 而这里只需要数量，用 O(1) 取长度避免每次增删/撤销都序列化整张图
	const model = (lf.value as any)?.graphModel;
	stats.nodes = model?.nodes?.length ?? 0;
	stats.edges = model?.edges?.length ?? 0;
};

/**
 * 打开流程设计器对话框
 * @param flow - 要编辑/查看的流程数据
 * @param options - 可选：只读查看、自定义标题
 */
const openDialog = async (flow: WorkflowFlowItem, options?: OpenDialogOptions) => {
	currentFlow.value = flow;
	readonlyMode.value = options?.readonly ?? false;
	state.title = options?.title ?? `流程设计 - ${flow.name}`;
	state.enableSelection = false;
	state.graphData = flow.flowJson ? JSON.parse(flow.flowJson) : { nodes: [], edges: [] };

	// 清空选中的元素，避免显示上一次的属性信息
	selectedElement.value = null;

	// 获取表单配置：优先流程快照，缺失时按 formId 反查表单表
	formJson.value = await loadFlowSchemaJson(flow.formJson, flow.formId);

	state.visible = true;
};

/**
 * 对话框打开完成后的回调
 * 等待 DOM 渲染完成后初始化 LogicFlow
 */
const handleOpened = () => {
	nextTick(() => initLogicFlow());
};

/**
 * 初始化 LogicFlow 实例：配置画布、注册插件与自定义图元、绑定事件
 * 重复打开设计器时先销毁旧实例再重建，避免内存泄漏与事件重复绑定
 */
const initLogicFlow = () => {
	const container = canvasRef.value;
	if (!container) return;

	// 复用父容器尺寸，避免画布宽高为 0
	const parent = container.parentElement;
	if (parent) {
		container.style.width = parent.clientWidth + 'px';
		container.style.height = parent.clientHeight + 'px';
	}

	// 已有实例 -> 先销毁再重建，避免内存泄漏与事件重复绑定
	// 注意：不要手动调 graphModel.eventCenter.off()，LogicFlow 2.x 的 off(evts) 内部会执行
	// evts.split(',')，不传参会抛 "can't access property split, evts is undefined"；
	// destroy() 本身已完整清理事件中心与 DOM，无需额外处理
	if (lf.value) {
		try {
			lf.value.destroy();
		} catch (error) {
			console.warn('[FlowDesigner] 销毁 LogicFlow 实例警告:', error);
		}
		lf.value = undefined;
		selectedElement.value = null;
		container.innerHTML = '';
	}

	// 节点类型 -> ID 后缀映射
	const idSuffix: Record<string, string> = {
		'start-node': '_snode',
		'end-node': '_enode',
		'user-node': '_unode',
		'gateway-node': '_gnode',
		'parallel-node': '_pnode',
	};

	lf.value = new LogicFlow({
		stopScrollGraph: true, // 禁止滚轮平移画布（保留缩放）
		stopZoomGraph: false, // 允许滚轮缩放
		metaKeyMultipleSelected: true, // 支持 Ctrl/Cmd 多选
		snapline: true, // 对齐线
		isSilentMode: readonlyMode.value, // 只读模式禁止编辑
		keyboard: {
			enabled: !readonlyMode.value, // 只读模式禁用键盘编辑（删除等）
			shortcuts: [
				{
					// Delete 键删除选中节点/连线
					keys: ['delete'],
					callback: () => {
						const elements = lf.value!.getSelectElements(true);
						lf.value!.clearSelectElements();
						elements.edges.forEach((edge) => lf.value!.deleteEdge(edge.id));
						elements.nodes.forEach((node) => lf.value!.deleteNode(node.id));
					},
				},
			],
		},
		container,
		width: container.clientWidth,
		height: container.clientHeight,
		// 网格只保留点阵：默认 mesh 是交叉实线，视觉上像草稿纸方格，商业感差
		grid: { visible: true, type: 'dot', size: 20, config: { color: 'rgba(100, 116, 139, 0.22)', thickness: 3 }, majorBold: false },
		plugins: [BpmnElement, InsertNodeInPolyline, Menu, MiniMap, SelectionSelect, Snapshot],
		idGenerator: (type) => `${XEUtils.toDateString(new Date(), 'yyyyMMddHHmmssSSS')}${idSuffix[type] ?? '_unknown'}`,
	});

	// 注册自定义节点
	RegisterNode.Register(lf.value);

	lf.value.setTheme({
		snapline: { stroke: '#FF0000', strokeWidth: 1 }, // 红色对齐线
		// 注意：网格只在构造器里配置（Grid 是 overlay，建实例时定型），这里不要再传 grid 会回落成 mesh 实线
		// 全局文本：节点名 / 连线条件，统一排版层级
		nodeText: { color: '#344054', fontSize: 12, fontWeight: 500 },
		edgeText: { color: '#475467', fontSize: 11, background: { fill: '#FFFFFF' } },
		// 箭头与连线同色，避免"线灰箭头黑"的割裂感
		arrow: { stroke: '#98a2b3', fill: '#98a2b3', strokeWidth: 1.6 },
		polyline: {
			stroke: 'var(--el-color-primary)', // 跟随 Element Plus 主题色
			strokeWidth: 1.5,
			strokeDasharray: '10, 5', // 长虚线：实线 10px / 空白 5px
		},
		edgeAnimation: {
			stroke: '#FF6B6B', // 流动光点颜色
			strokeDasharray: '20 10',
			animationDuration: '8s',
			animationIterationCount: 'infinite',
			animationTimingFunction: 'linear',
		},
	});

	// ========== 事件监听 ==========

	// 节点点击：选中并展示属性
	lf.value.on('node:click', ({ data }) => {
		selectedElement.value = { ...data, elementType: 'node' };
	});

	// 连线点击：事件回调的 data 未必含最新 properties，需回查图数据合并
	lf.value.on('edge:click', ({ data }) => {
		if (!lf.value) return;
		const edgeData = (lf.value.getGraphData() as any).edges?.find((e: any) => e.id === data.id);

		if (edgeData) {
			// 图数据中的 properties 优先级更高（含最新保存的属性）
			selectedElement.value = {
				...data,
				...edgeData,
				properties: { ...(data.properties || {}), ...(edgeData.properties || {}) },
				elementType: 'edge',
			};
		} else {
			// 新建的边可能处于临时状态，直接用事件数据
			selectedElement.value = { ...data, elementType: 'edge' };
		}
	});

	// 连线新增：统一改写为 edge_ 前缀 ID
	lf.value.on('edge:add', (args) => {
		const timestamp = XEUtils.toDateString(new Date(), 'yyyyMMddHHmmssSSS');
		lf.value?.changeEdgeId(args.data.id, `edge_${timestamp}`);
	});

	// 空白区域点击：取消选中
	lf.value.on('blank:click', () => {
		selectedElement.value = null;
	});

	// 节点/连线增删、撤销恢复后同步头部规模统计
	(['node:add', 'node:delete', 'edge:add', 'edge:delete', 'history:change'] as string[]).forEach((name) => lf.value!.on(name as any, syncStats));

	// 渲染流程图并聚焦画布中心
	const graphData = state.graphData && (state.graphData.nodes || state.graphData.edges) ? state.graphData : { nodes: [], edges: [] };

	lf.value.render(graphData);
	syncStats();
	lf.value.focusOn({ coordinate: { x: 300, y: 300 } });
};

// 组件卸载兜底：弹窗关闭不销毁 DOM，切页时若不释放，LogicFlow 实例与其事件监听会整体泄漏
onBeforeUnmount(() => {
	if (!lf.value) return;
	try {
		lf.value.destroy();
	} catch (error) {
		console.warn('[FlowDesigner] 卸载销毁 LogicFlow 实例警告:', error);
	}
	lf.value = undefined;
});

/**
 * 切换框选功能
 * @param enable - 是否开启框选
 */
const toggleSelection = (enable: boolean) => {
	if (!lf.value) return;
	const selection = (lf.value as any)?.extension?.selectionSelect;
	if (!selection) return;

	enable ? selection.openSelectionSelect() : selection.closeSelectionSelect();
};

/**
 * 预览流程图数据
 */
const previewGraph = () => {
	if (!lf.value) return;
	preview.graphData = lf.value.getGraphData() as any;
	preview.visible = true;
};

/**
 * 对话框关闭时的回调
 * 清空选中状态
 */
const handleDialogClose = () => {
	selectedElement.value = null;
};

/**
 * 取消编辑，关闭对话框
 */
const handleCancel = () => {
	selectedElement.value = null;
	state.visible = false;
};

/**
 * 保存流程
 * 获取当前画布数据并提交到后端
 */
const handleSave = async () => {
	if (readonlyMode.value) return;
	if (!lf.value || !currentFlow.value) return;

	// 获取当前画布的图数据
	const graph = lf.value.getGraphData();

	state.saving = true;
	try {
		await saveFlowDesign({
			id: currentFlow.value.id,
			flowJson: JSON.stringify(graph),
		});
		ElMessage.success('流程设计已保存');

		// 关闭对话框并清理状态
		selectedElement.value = null;
		state.visible = false;

		// 通知父组件刷新列表
		emit('saved');
	} finally {
		state.saving = false;
	}
};

// 暴露给父组件的方法
defineExpose({
	openDialog,
});
</script>

<style scoped lang="scss">
/* ===================== 头部 ===================== */
.fd-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	padding: 0 4px;
	min-height: 34px;
}

.fd-head__brand {
	display: flex;
	align-items: center;
	gap: 9px;
	min-width: 0;
}

.fd-head__logo {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 8px;
	font-size: 14px;
	color: #fff;
	background: rgba(255, 255, 255, 0.16);
	border: 1px solid rgba(255, 255, 255, 0.24);
	flex-shrink: 0;
}

.fd-head__text {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.fd-head__title {
	font-size: 14px;
	font-weight: 600;
	letter-spacing: 0.2px;
	line-height: 20px;
	color: #fff;
	flex-shrink: 0;
}

.fd-head__sub {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.fd-head__status {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	height: 18px;
	padding: 0 8px;
	border-radius: 9px;
	font-size: 11px;
	color: #fff;
	background: rgba(255, 255, 255, 0.14);
	border: 1px solid rgba(255, 255, 255, 0.18);

	&::before {
		content: '';
		width: 5px;
		height: 5px;
		border-radius: 50%;
		background: rgba(255, 255, 255, 0.75);
	}

	&.is-success::before {
		background: #6ee7b7;
	}

	&.is-warning::before {
		background: #fcd34d;
	}
}

.fd-head__version {
	font-family: 'SFMono-Regular', Consolas, monospace;
	font-size: 11px;
	color: rgba(255, 255, 255, 0.55);
}

.fd-head__actions {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
}

/* 头部按钮：主题色底上主操作反白、次操作描边 */
.fd-head__actions .fd-btn-ghost,
.fd-head__actions .fd-btn-light {
	height: 28px;
	padding: 0 12px;
	border-radius: 7px;
	font-size: 13px;
	font-weight: 500;
	transition: all 0.18s ease;
}

/* 次操作：白色描边透明底 */
.fd-btn-ghost {
	/* 暗色全局规则用 --next-border-color 顶掉所有按钮的 hover 边框，这里固定成白色描边 */
	--next-border-color: rgba(255, 255, 255, 0.66);
	--el-button-bg-color: rgba(255, 255, 255, 0.12);
	--el-button-border-color: rgba(255, 255, 255, 0.4);
	--el-button-text-color: #fff;
	--el-button-hover-bg-color: rgba(255, 255, 255, 0.24);
	--el-button-hover-border-color: rgba(255, 255, 255, 0.66);
	--el-button-hover-text-color: #fff;
	--el-button-active-bg-color: rgba(255, 255, 255, 0.28);
	--el-button-active-border-color: #fff;
	--el-button-active-text-color: #fff;
	--el-button-disabled-bg-color: rgba(255, 255, 255, 0.08);
	--el-button-disabled-border-color: rgba(255, 255, 255, 0.2);
	--el-button-disabled-text-color: rgba(255, 255, 255, 0.5);
}

/* 主操作：白底 + 主题色字（头部底色固定为主题色渐变，hover 不跟随暗色主题反转的色阶） */
.fd-btn-light {
	--next-border-color: color-mix(in srgb, var(--el-color-primary) 10%, #fff);
	--el-button-bg-color: #fff;
	--el-button-border-color: #fff;
	--el-button-text-color: var(--el-color-primary);
	--el-button-hover-bg-color: color-mix(in srgb, var(--el-color-primary) 10%, #fff);
	--el-button-hover-border-color: color-mix(in srgb, var(--el-color-primary) 10%, #fff);
	--el-button-hover-text-color: color-mix(in srgb, var(--el-color-primary) 90%, #000);
	--el-button-active-bg-color: color-mix(in srgb, var(--el-color-primary) 20%, #fff);
	--el-button-active-border-color: color-mix(in srgb, var(--el-color-primary) 20%, #fff);
	--el-button-active-text-color: color-mix(in srgb, var(--el-color-primary) 85%, #000);
	--el-button-disabled-bg-color: rgba(255, 255, 255, 0.55);
	--el-button-disabled-border-color: transparent;
	--el-button-disabled-text-color: rgba(255, 255, 255, 0.85);
}

.fd-head__close {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	padding: 0;
	border-radius: 50%;
	border: 1px solid rgba(255, 255, 255, 0.26);
	background: transparent;
	color: rgba(255, 255, 255, 0.86);
	font-size: 15px;
	cursor: pointer;
	transition: all 0.2s ease;

	&:hover {
		color: #fff;
		background: rgba(255, 255, 255, 0.16);
		border-color: rgba(255, 255, 255, 0.5);
	}
}

/* ===================== 主体：文档条 + 三栏 ===================== */
.fd-shell {
	display: flex;
	flex-direction: column;
	height: 100%;
	min-height: 0;
	overflow: hidden;
}

/* 文档条：流程名 + 编码/分类 chip + 提示 */
.fd-docbar {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-shrink: 0;
	padding: 8px 14px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
}

.fd-docbar__name {
	max-width: 46%;
	font-size: 15px;
	font-weight: 600;
	line-height: 22px;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.fd-docbar__chip {
	flex-shrink: 0;
	padding: 1px 8px;
	border-radius: 6px;
	font-size: 11px;
	line-height: 18px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 22%, transparent);
	font-family: 'SFMono-Regular', Consolas, monospace;

	&.is-soft {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color-light);
		border-color: var(--el-border-color-lighter);
		font-family: inherit;
	}
}

.fd-docbar__spacer {
	flex: 1;
	min-width: 0;
}

.fd-docbar__hint {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	flex-shrink: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	.el-icon {
		color: var(--el-color-warning);
	}
}

.fd-body {
	display: flex;
	flex: 1;
	min-height: 0;
	overflow: hidden;
}

.fd-props {
	display: flex;
	width: 340px;
	flex-shrink: 0;
	min-height: 0;
	border-left: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
	overflow: hidden;
}

/* ===================== 画布 ===================== */
.fd-canvas-wrap {
	position: relative;
	flex: 1;
	min-width: 0;
	overflow: hidden;
	background: var(--el-bg-color-overlay, #fff);

	/* LogicFlow 自带 #fff 底色，统一交给外层容器决定（否则暗色下露白） */
	:deep(.lf-graph) {
		background: transparent;
	}
}

.canvas-wrapper {
	position: absolute;
	inset: 0;
}

/* 右下角规模提示：只报事实，不抢视线 */
.fd-canvas-stat {
	position: absolute;
	right: 12px;
	bottom: 12px;
	z-index: 4;
	padding: 2px 9px;
	border-radius: 10px;
	font-size: 11px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	background: color-mix(in srgb, var(--el-bg-color-overlay) 80%, transparent);
	border: 1px solid var(--el-border-color-lighter);
	pointer-events: none;
}

/* 左下角状态条 */
.fd-statusbar {
	position: absolute;
	left: 16px;
	bottom: 16px;
	z-index: 3;
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 5px 12px;
	border-radius: 999px;
	background: color-mix(in srgb, var(--el-bg-color) 92%, transparent);
	backdrop-filter: blur(8px);
	border: 1px solid var(--el-border-color-lighter);
	font-size: 12px;
	color: var(--el-text-color-secondary);
	box-shadow: 0 8px 20px -18px rgba(0, 0, 0, 0.4);
}

.fd-status-label {
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.fd-status-hint {
	padding-left: 10px;
	border-left: 1px solid var(--el-border-color-lighter);
}

/* 只读提示胶囊 */
.fd-readonly-badge {
	position: absolute;
	left: 16px;
	bottom: 16px;
	z-index: 3;
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 5px 12px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	background: color-mix(in srgb, var(--el-bg-color) 92%, transparent);
	border: 1px solid var(--el-border-color-lighter);
	backdrop-filter: blur(8px);
}

/* ===================== 图数据弹窗 ===================== */
:deep(.preview-data-dialog) {
	.el-dialog__body {
		max-height: calc(100vh - 220px) !important;
	}
}
</style>

<!-- 全屏设计器需要清零弹窗留白：全局 element.scss 的同名规则带 !important，必须用非 scoped 块反压 -->
<style lang="scss">
/* 全屏设计器：清零弹窗留白，内部各栏自己滚动 */
.el-overlay .el-overlay-dialog .el-dialog.flow-designer-dialog {
	padding: 0 !important;
	margin: 0 !important;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	max-height: 100vh;
	height: 100vh;

	/* 头部：主题色渐变实心底（全局规则是纯色，这里叠一层同色系渐变让整块「有体积」） */
	.el-dialog__header {
		margin: 0;
		padding: 6px 14px;
		background: linear-gradient(118deg, var(--el-color-primary) 0%, var(--el-color-primary-dark-2) 96%);
	}

	.el-dialog__body {
		flex: 1;
		min-height: 0;
		padding: 0 !important;
		overflow: hidden !important;
		max-height: none !important;
	}
}

/* 网格点色走变量：LogicFlow 点阵颜色写死在 SVG 属性里，只能用 CSS 覆盖（CSS 声明优先于表现属性） */
.flow-designer-dialog {
	--fd-grid-dot: rgba(100, 116, 139, 0.22);
}

[data-theme='dark'] .flow-designer-dialog {
	--fd-grid-dot: rgba(148, 163, 184, 0.26);
}

.flow-designer-dialog .lf-grid pattern g {
	fill: var(--fd-grid-dot);
}

/* 边标签底在主题里写死成白色，暗色下会变成「白底浅字」贴片 */
.flow-designer-dialog .lf-line-text rect {
	fill: var(--el-bg-color-overlay, #ffffff);
	rx: 4px;
}

/* 缩略图卡片与视窗框同样是插件写死的亮色，暗色下跟随画布 */
[data-theme='dark'] .flow-designer-dialog .lf-mini-map {
	background: var(--el-bg-color);
	box-shadow:
		0 0 0 1px var(--el-border-color-lighter),
		0 12px 30px -18px rgba(0, 0, 0, 0.8);
}

[data-theme='dark'] .flow-designer-dialog .lf-minimap-viewport {
	background-color: rgba(255, 255, 255, 0.14);
}

[data-theme='dark'] .flow-designer-dialog .lf-mini-map text {
	fill: var(--el-text-color-regular);
}
</style>
