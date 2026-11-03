<template>
	<el-dialog
		v-model="visible"
		fullscreen
		append-to-body
		:show-close="false"
		:close-on-click-modal="false"
		destroy-on-close
		class="wf-designer-dialog"
		:before-close="handleBeforeClose"
		@opened="handleOpened"
		@closed="handleClosed"
	>
		<template #header>
			<div class="wf-head">
				<div class="wf-head__brand">
					<span class="wf-head__logo"><i class="ri-flow-chart" /></span>
					<div class="wf-head__text">
						<div class="wf-head__title">工作流设计器</div>
						<div class="wf-head__sub">
							<span class="wf-head__status" :class="statusClass">{{ statusMeta.label }}</span>
							<span class="wf-head__version">v{{ version }}</span>
						</div>
					</div>
				</div>

				<div class="wf-head__actions">
					<el-button class="wf-btn-ghost" :loading="saving" @click="handleSave(false)">
						<i class="ri-save-3-line" />保存草稿
					</el-button>
					<el-button class="wf-btn-light" :loading="saving" @click="handleSave(true)">
						<i class="ri-rocket-2-line" />保存并发布
					</el-button>
					<button class="wf-head__close" title="关闭" @click="requestClose"><i class="ri-close-line" /></button>
				</div>
			</div>
		</template>

		<div class="wf-designer" v-loading="loading" element-loading-text="正在加载工作流…">
			<!-- 文档条：名称与说明做成「标题栏」质感，不加边框、不加字数统计，避免头部再堆一层控件 -->
			<div class="wf-docbar">
				<el-input v-model="name" maxlength="60" placeholder="未命名流程" class="wf-docbar__name" />
				<span class="wf-docbar__sep"></span>
				<el-input v-model="description" maxlength="200" placeholder="添加一句话说明这个流程解决什么问题" class="wf-docbar__desc" />
				<el-tooltip content="拖拽或点击左侧节点加入画布；条件、并行与循环体路由在右侧属性面板配置" placement="bottom-end">
					<i class="ri-question-line wf-docbar__help" />
				</el-tooltip>
			</div>

			<div class="wf-designer__body">
				<!-- 左侧节点面板 -->
				<aside class="wf-palette">
					<div class="wf-palette__head">
						<span>节点</span>
						<span class="wf-palette__hint">点击或拖入画布</span>
					</div>
					<div class="wf-palette__list">
						<div v-for="group in paletteGroups" :key="group.key" class="wf-palette__group">
							<div class="wf-palette__group-title">{{ group.label }}</div>
							<div
								v-for="meta in group.nodes"
								:key="meta.lfType"
								class="wf-palette__item"
								:style="{ '--wf-accent': meta.accent }"
								@click="addNodeByClick(meta)"
								@mousedown.left="startDrag(meta, $event)"
							>
								<span class="wf-palette__icon"><i :class="meta.icon" /></span>
								<div class="wf-palette__text">
									<div class="wf-palette__name">{{ meta.label }}</div>
									<div class="wf-palette__desc">{{ meta.desc }}</div>
								</div>
							</div>
						</div>
					</div>
					<div class="wf-palette__foot">
						<div class="wf-palette__shortcut"><kbd>Ctrl</kbd> + <kbd>Z</kbd> 撤销</div>
						<div class="wf-palette__shortcut"><kbd>Delete</kbd> 删除选中</div>
					</div>
				</aside>

				<!-- 画布 -->
				<div class="wf-canvas-wrap">
					<div ref="canvasRef" class="wf-canvas"></div>

					<!-- 画布工具条：撤销/缩放/校验都属于「画布操作」，从弹窗头部挪出来，头部只留文档级动作 -->
					<div class="wf-canvas-tools">
						<el-tooltip content="撤销（Ctrl+Z）" placement="bottom">
							<button class="wf-tool" :disabled="!canUndo" @click="handleUndo"><i class="ri-arrow-go-back-line" /></button>
						</el-tooltip>
						<el-tooltip content="重做（Ctrl+Y）" placement="bottom">
							<button class="wf-tool" :disabled="!canRedo" @click="handleRedo"><i class="ri-arrow-go-forward-line" /></button>
						</el-tooltip>
						<span class="wf-tool-sep"></span>
						<el-tooltip content="适应画布" placement="bottom">
							<button class="wf-tool" @click="fitCanvas"><i class="ri-aspect-ratio-line" /></button>
						</el-tooltip>
						<el-tooltip content="缩小" placement="bottom">
							<button class="wf-tool" @click="zoomOut"><i class="ri-subtract-line" /></button>
						</el-tooltip>
						<el-tooltip content="重置为 100%" placement="bottom">
							<button class="wf-tool wf-tool--zoom" @click="resetZoom">{{ zoomPercent }}%</button>
						</el-tooltip>
						<el-tooltip content="放大" placement="bottom">
							<button class="wf-tool" @click="zoomIn"><i class="ri-add-line" /></button>
						</el-tooltip>
						<span class="wf-tool-sep"></span>
						<el-tooltip content="校验流程结构" placement="bottom">
							<button class="wf-tool wf-tool--text" @click="handleValidate"><i class="ri-shield-check-line" />校验</button>
						</el-tooltip>
					</div>

					<div class="wf-canvas-stat">{{ graphNodes.length }} 个节点 · {{ edgeCount }} 条连线</div>

					<!-- 结构校验结果：不弹窗打断，浮在画布底部，边改边看 -->
					<transition name="wf-fade">
						<div v-if="showValidateResult" class="wf-validate">
							<div class="wf-validate__head">
								<span class="wf-validate__title">
									<i :class="errors.length ? 'ri-error-warning-line' : 'ri-checkbox-circle-line'" />
									结构校验 · {{ errors.length ? `未通过（${errors.length} 项）` : '通过' }}
								</span>
								<el-button icon="ele-Close" text size="small" @click="showValidateResult = false" />
							</div>
							<ul v-if="errors.length" class="wf-validate__list is-error">
								<li v-for="(item, index) in errors" :key="`e${index}`">{{ item }}</li>
							</ul>
							<ul v-if="warnings.length" class="wf-validate__list is-warning">
								<li v-for="(item, index) in warnings" :key="`w${index}`">{{ item }}</li>
							</ul>
							<div v-if="!errors.length && !warnings.length" class="wf-validate__ok">结构与路由配置均通过校验，可以发布。</div>
						</div>
					</transition>
				</div>

				<!-- 右侧属性面板 -->
				<aside class="wf-props">
					<NodePropertyPanel
						:node="activeNode"
						:revision="revision"
						:agents="agents"
						:nodes="graphNodes"
						:next-node-id="nextNodeId"
						:kms-list="kmsList"
						:skill-tools="skillTools"
						:workflows="workflows"
						:workflow-id="currentId"
						@change="handleNodeChange"
						@set-next="handleSetNext"
						@delete="handleDeleteSelected"
					/>
				</aside>
			</div>
		</div>
	</el-dialog>
</template>

<script lang="ts" setup name="workflowDesigner">
import { computed, nextTick, onBeforeUnmount, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import LogicFlow from '@logicflow/core';
import { getAPI } from '/@/utils/axios-utils';
import { AgentApi, AgentKmsApi, AgentSkillToolApi, AgentWorkflowApi } from '/@/api-services/agent/api';
import {
	WORKFLOW_NODE_GROUP_META,
	WORKFLOW_NODE_META_LIST,
	WORKFLOW_NODE_TYPE,
	WORKFLOW_STATUS_META,
	WorkflowConditionBranch,
	WorkflowDefinitionModel,
	WorkflowNodeGroup,
	WorkflowNodeMeta,
	WorkflowNodeModel,
	WorkflowNodeType,
	createWorkflowNode,
	describeNode,
} from '../types';
import { buildNodeProperties, definitionToGraph, graphToDefinition, parseDefinition, validateDefinition } from '../logicflow/mapper';
import { registerWorkflowElements } from '../logicflow/registerNodes';
import NodePropertyPanel from './nodePropertyPanel.vue';

const props = defineProps<{
	/** 编辑的工作流 Id；为空表示新建 */
	workflowId?: number;
	/** 新建时预填的名称 */
	initialName?: string;
}>();

const emit = defineEmits<{
	(e: 'close'): void;
	(e: 'saved', id: number): void;
}>();

const visible = ref(true);
const loading = ref(true);
const saving = ref(false);

const currentId = ref<number | undefined>(props.workflowId);
const name = ref('');
const description = ref('');
const status = ref<number>(0);
const version = ref(1);

const agents = ref<{ id: number; name: string }[]>([]);
const kmsList = ref<{ id: number; name: string }[]>([]);
const workflows = ref<{ id: number; name: string }[]>([]);
const skillTools = ref<
	{ id: number; name: string; skillToolType?: number; classMethod?: string | null; mcpTools?: string | null; mcpSelectedTools?: string | null }[]
>([]);
const activeNode = ref<WorkflowNodeModel | null>(null);
const graphNodes = ref<WorkflowNodeModel[]>([]);
const edgeCount = ref(0);
const nextNodeId = ref('');
const revision = ref(0);
const canUndo = ref(false);
const canRedo = ref(false);
const zoomPercent = ref(100);
const dirty = ref(false);

const errors = ref<string[]>([]);
const warnings = ref<string[]>([]);
const showValidateResult = ref(false);

const canvasRef = ref<HTMLDivElement>();
let lf: LogicFlow | null = null;
let observer: ResizeObserver | null = null;
let initializing = false;

const statusMeta = computed(() => WORKFLOW_STATUS_META[status.value] ?? WORKFLOW_STATUS_META[0]);
/** 头部状态胶囊只靠一个小圆点区分状态（头部是主题色实心底，彩色标签会糊在一起） */
const statusClass = computed(() => (status.value === 2 ? 'is-warning' : status.value === 1 ? 'is-success' : 'is-info'));
const agentNameMap = computed<Record<number, string>>(() => Object.fromEntries(agents.value.map((agent) => [agent.id, agent.name])));
/** 左侧面板按能力分组：AI 能力 / 流程控制 / 数据与集成 / 入口与人工 */
const paletteGroups = computed(() =>
	(Object.keys(WORKFLOW_NODE_GROUP_META) as WorkflowNodeGroup[])
		.map((key) => ({
			key,
			label: WORKFLOW_NODE_GROUP_META[key].label,
			nodes: WORKFLOW_NODE_META_LIST.filter((meta) => meta.group === key),
		}))
		.filter((group) => group.nodes.length)
);

/* -------------------------------------------------------------------------- */
/*                                  画布初始化                                  */
/* -------------------------------------------------------------------------- */

const handleOpened = async () => {
	loading.value = true;
	try {
		await loadReferenceData();

		let definition: WorkflowDefinitionModel;
		if (props.workflowId) {
			const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowDetailIdGet(props.workflowId);
			const detail = res.data.result;
			if (!detail) throw new Error('工作流不存在或已被删除');
			currentId.value = detail.id;
			name.value = detail.name ?? '';
			description.value = detail.description ?? '';
			status.value = Number(detail.status ?? 0);
			version.value = Number(detail.version ?? 1);
			definition = parseDefinition(detail.definitionJson);
		} else {
			name.value = props.initialName || '未命名流程';
			description.value = '';
			definition = parseDefinition(null);
		}

		await nextTick();
		initCanvas(definition);
	} catch (error: any) {
		ElMessage.error(error?.message ?? '加载工作流失败');
		visible.value = false;
	} finally {
		loading.value = false;
	}
};

/** 下拉数据（智能体/知识库/工具/MCP/工作流）：任一失败只影响对应的节点配置，不阻断设计器 */
const loadReferenceData = async () => {
	const results = await Promise.allSettled([
		getAPI(AgentApi).apiAgentListGet(),
		getAPI(AgentKmsApi).apiAgentKmsListGet(),
		getAPI(AgentSkillToolApi).apiAgentSkillToolToolListGet(),
		getAPI(AgentSkillToolApi).apiAgentSkillToolMcpListGet(),
		getAPI(AgentWorkflowApi).apiAgentWorkflowSelectListGet(),
	]);
	const pickList = (result: PromiseSettledResult<any>) => (result.status === 'fulfilled' ? result.value?.data?.result ?? [] : []);

	agents.value = (pickList(results[0]) as any[]).filter((agent) => !!agent.id).map((agent) => ({ id: Number(agent.id), name: String(agent.name ?? '') }));
	kmsList.value = (pickList(results[1]) as any[]).filter((item) => !!item.id).map((item) => ({ id: Number(item.id), name: String(item.name ?? '') }));
	const toolList = [...(pickList(results[2]) as any[]), ...(pickList(results[3]) as any[])];
	skillTools.value = toolList
		.filter((item) => !!item.id)
		.map((item) => ({
			id: Number(item.id),
			name: String(item.name ?? ''),
			skillToolType: Number(item.skillToolType ?? 0),
			classMethod: item.classMethod ?? '',
			mcpTools: item.mcpTools ?? '',
			mcpSelectedTools: item.mcpSelectedTools ?? '',
		}));
	workflows.value = (pickList(results[4]) as any[])
		.filter((item) => !!item.id)
		.map((item) => ({ id: Number(item.id), name: String(item.name ?? '') }));
};

const initCanvas = (definition: WorkflowDefinitionModel) => {
	const container = canvasRef.value;
	if (!container) return;

	initializing = true;
	lf = new LogicFlow({
		container,
		width: container.clientWidth || 900,
		height: container.clientHeight || 600,
		// 点阵默认每 5 个点加粗一次（majorBold），画布上会显得很吵，这里关掉；间距放大到 16 让点更稀
		grid: { size: 16, type: 'dot', visible: true, majorBold: false },
		stopScrollGraph: true,
		stopZoomGraph: false,
		metaKeyMultipleSelected: true,
		adjustEdge: true,
		history: true,
		keyboard: { enabled: false },
		edgeType: 'wf-edge',
		guards: { beforeDelete: (data: any) => !data?.properties?.projected },
	});

	registerWorkflowElements(lf);
	lf.render(definitionToGraph(definition, agentNameMap.value));
	bindCanvasEvents();
	lf.fitView(30);

	observer = new ResizeObserver(() => {
		if (!lf) return;
		lf.resize(container.clientWidth, container.clientHeight);
	});
	observer.observe(container);

	refreshGraphState();
	canUndo.value = lf.history.undoAble();
	canRedo.value = lf.history.redoAble();
	nextTick(() => {
		initializing = false;
		dirty.value = false;
	});
};

const bindCanvasEvents = () => {
	if (!lf) return;

	lf.on('node:click', ({ data }: any) => {
		selectedEdgeId.value = '';
		selectNode(data?.id);
	});

	lf.on('edge:click', ({ data }: any) => {
		const properties = data?.properties ?? {};
		// 条件/并行/循环体的虚线边没有独立语义，点击等同选中它的源节点
		if (properties.projected) {
			selectNode(String(properties.conditionNodeId || properties.parallelNodeId || properties.loopNodeId || ''));
			return;
		}
		selectedNodeId.value = '';
		activeNode.value = null;
		nextNodeId.value = '';
		selectedEdgeId.value = String(data?.id ?? '');
		revision.value++;
	});

	lf.on('blank:click', () => clearSelection());

	lf.on('node:delete', ({ data }: any) => {
		cleanupReferences(String(data?.id ?? ''));
		if (selectedNodeId.value === data?.id) clearSelection();
		refreshGraphState();
	});

	lf.on('node:dnd-add', ({ data }: any) => {
		refreshGraphState();
		selectNode(data?.id);
	});

	lf.on('edge:delete', () => {
		selectedEdgeId.value = '';
		refreshGraphState();
	});

	lf.on('history:change', () => {
		if (!lf) return;
		canUndo.value = lf.history.undoAble();
		canRedo.value = lf.history.redoAble();
		if (initializing) return;
		dirty.value = true;
		refreshGraphState();
		refreshActiveNode();
	});

	lf.on('graph:transform', ({ transform }: any) => {
		zoomPercent.value = Math.round(Number(transform?.SCALE_X ?? 1) * 100);
	});
};

/* -------------------------------------------------------------------------- */
/*                              选中与图状态维护                                */
/* -------------------------------------------------------------------------- */

const selectedNodeId = ref('');
const selectedEdgeId = ref('');

const readNode = (id: string): WorkflowNodeModel | null => {
	const model = lf?.getNodeModelById(id);
	if (!model) return null;
	const properties: any = model.properties ?? {};
	return {
		id: model.id,
		type: Number(properties.type) as WorkflowNodeType,
		name: String(properties.name ?? ''),
		x: model.x,
		y: model.y,
		agentId: Number(properties.agentId ?? 0),
		inputTemplate: String(properties.inputTemplate ?? ''),
		systemPromptAppend: String(properties.systemPromptAppend ?? ''),
		timeoutSeconds: Number(properties.timeoutSeconds ?? 0),
		onError: String(properties.onError ?? 'Stop'),
		branches: Array.isArray(properties.branches) ? properties.branches.map((branch: any) => normalizeBranch(branch)) : [],
		parallelNodeIds: Array.isArray(properties.parallelNodeIds) ? properties.parallelNodeIds.map(String) : [],
		joinMode: String(properties.joinMode ?? 'All'),
		approvalTimeoutMinutes: Number(properties.approvalTimeoutMinutes ?? 0),
		approvalTimeoutAction: String(properties.approvalTimeoutAction ?? 'Reject'),
		kmsId: Number(properties.kmsId ?? 0),
		queryTemplate: String(properties.queryTemplate ?? ''),
		topN: Number(properties.topN ?? 5),
		scoreThreshold: Number(properties.scoreThreshold ?? 0),
		skillId: Number(properties.skillId ?? 0),
		toolName: String(properties.toolName ?? ''),
		argumentsTemplate: String(properties.argumentsTemplate ?? ''),
		httpUrl: String(properties.httpUrl ?? ''),
		httpMethod: String(properties.httpMethod ?? 'GET'),
		httpHeaders: String(properties.httpHeaders ?? ''),
		httpBody: String(properties.httpBody ?? ''),
		httpContentType: String(properties.httpContentType ?? 'application/json; charset=utf-8'),
		httpFailOnError: properties.httpFailOnError === undefined ? true : Boolean(properties.httpFailOnError),
		assignments: Array.isArray(properties.assignments) ? properties.assignments.map((item: any) => ({ name: String(item?.name ?? ''), valueTemplate: String(item?.valueTemplate ?? '') })) : [],
		outputTemplate: String(properties.outputTemplate ?? ''),
		loopMode: String(properties.loopMode ?? 'Items'),
		loopItemsTemplate: String(properties.loopItemsTemplate ?? ''),
		loopSeparator: String(properties.loopSeparator ?? '\n'),
		loopCount: Number(properties.loopCount ?? 1),
		loopMaxIterations: Number(properties.loopMaxIterations ?? 20),
		loopBodyNodeId: String(properties.loopBodyNodeId ?? ''),
		delaySeconds: Number(properties.delaySeconds ?? 1),
		subWorkflowId: Number(properties.subWorkflowId ?? 0),
		subWorkflowInputTemplate: String(properties.subWorkflowInputTemplate ?? ''),
	};
};

const normalizeBranch = (branch: any): WorkflowConditionBranch => ({
	when: String(branch?.when ?? ''),
	value: String(branch?.value ?? ''),
	next: String(branch?.next ?? ''),
});

const readNextNodeId = (id: string) => {
	if (!lf) return '';
	const edge = lf.graphModel.edges.find((item) => item.sourceNodeId === id && !item.properties?.projected);
	return edge?.targetNodeId ?? '';
};

const selectNode = (id?: string | null) => {
	if (!id || !lf) {
		clearSelection();
		return;
	}
	selectedEdgeId.value = '';
	selectedNodeId.value = id;
	lf.clearSelectElements();
	lf.selectElementById(id, false);
	activeNode.value = readNode(id);
	nextNodeId.value = readNextNodeId(id);
	revision.value++;
};

const clearSelection = () => {
	selectedNodeId.value = '';
	selectedEdgeId.value = '';
	activeNode.value = null;
	nextNodeId.value = '';
	revision.value++;
	lf?.clearSelectElements();
};

const refreshActiveNode = () => {
	if (!selectedNodeId.value) return;
	const node = readNode(selectedNodeId.value);
	if (!node) {
		clearSelection();
		return;
	}
	activeNode.value = node;
	nextNodeId.value = readNextNodeId(selectedNodeId.value);
	revision.value++;
};

const refreshGraphState = () => {
	if (!lf) return;
	graphNodes.value = lf.graphModel.nodes.map((model) => {
		const properties: any = model.properties ?? {};
		return {
			id: model.id,
			type: Number(properties.type) as WorkflowNodeType,
			name: String(properties.name ?? ''),
			agentId: Number(properties.agentId ?? 0),
		} as WorkflowNodeModel;
	});
	edgeCount.value = lf.graphModel.edges.filter((edge) => !edge.properties?.projected).length;
};

/* -------------------------------------------------------------------------- */
/*                            节点属性变更与路由投影                             */
/* -------------------------------------------------------------------------- */

const handleNodeChange = (patch: Record<string, any>) => {
	if (!lf || !activeNode.value) return;
	const id = activeNode.value.id;
	const merged: WorkflowNodeModel = { ...activeNode.value, ...patch };
	const summary = describeNode(merged, merged.agentId ? agentNameMap.value[merged.agentId] : '');
	lf.setProperties(id, { ...patch, summary });

	activeNode.value = { ...merged };
	revision.value++;
	graphNodes.value = graphNodes.value.map((node) => (node.id === id ? { ...node, name: merged.name } : node));

	// 只有分支/并行/循环体配置会改变投影边，名称等属性无需重建，避免历史记录被高频写入
	if ('branches' in patch || 'parallelNodeIds' in patch || 'loopBodyNodeId' in patch) {
		syncProjectedEdges();
		refreshGraphState();
	}
};

/** 把节点属性里的分支/并行/循环体配置投影成虚线边（这些节点本身不允许普通出边） */
const syncProjectedEdges = () => {
	if (!lf) return;
	const definition = graphToDefinition(lf.getGraphRawData());
	const expected = definitionToGraph(definition, agentNameMap.value).edges.filter((edge: any) => edge?.properties?.projected);
	const existing = lf.graphModel.edges.filter((edge) => edge.properties?.projected === true);

	const sameRoute = (edge: any, target: any) =>
		edge.sourceNodeId === target.sourceNodeId &&
		edge.targetNodeId === target.targetNodeId &&
		edge.properties?.kind === target.properties?.kind &&
		Number(edge.properties?.branchIndex ?? -1) === Number(target.properties?.branchIndex ?? -1);

	existing.forEach((edge) => {
		if (!expected.some((target: any) => sameRoute(edge, target))) lf!.graphModel.deleteEdgeById(edge.id);
	});

	expected.forEach((target: any) => {
		const found = lf!.graphModel.edges.find((edge) => edge.properties?.projected === true && sameRoute(edge, target));
		if (!found) {
			lf!.addEdge(target);
			return;
		}
		lf!.setProperties(found.id, { ...target.properties });
		const label = typeof target.text === 'object' ? target.text?.value : target.text;
		if (label) lf!.updateText(found.id, label);
	});
};

/** 删除节点后清理其它节点里对它的引用（分支目标 / 并行入口 / 循环体） */
const cleanupReferences = (deletedId: string) => {
	if (!lf || !deletedId) return;
	lf.graphModel.nodes.forEach((model) => {
		const properties: any = model.properties ?? {};
		const type = Number(properties.type);
		if (type === WORKFLOW_NODE_TYPE.Condition) {
			const branches: WorkflowConditionBranch[] = Array.isArray(properties.branches) ? properties.branches : [];
			const filtered = branches.filter((branch) => branch.next !== deletedId);
			if (filtered.length !== branches.length) {
				lf!.setProperties(model.id, { branches: filtered.length ? filtered : [{ when: '', value: '', next: '' }] });
			}
		}
		if (type === WORKFLOW_NODE_TYPE.Parallel) {
			const ids: string[] = Array.isArray(properties.parallelNodeIds) ? properties.parallelNodeIds : [];
			const filtered = ids.filter((id) => id !== deletedId);
			if (filtered.length !== ids.length) lf!.setProperties(model.id, { parallelNodeIds: filtered });
		}
		if (type === WORKFLOW_NODE_TYPE.Loop && properties.loopBodyNodeId === deletedId) {
			lf!.setProperties(model.id, { loopBodyNodeId: '' });
		}
	});
	syncProjectedEdges();
	refreshGraphState();
};

const handleSetNext = (targetId: string) => {
	if (!lf || !activeNode.value) return;
	const id = activeNode.value.id;
	lf.graphModel.edges
		.filter((edge) => edge.sourceNodeId === id && !edge.properties?.projected)
		.forEach((edge) => lf!.graphModel.deleteEdgeById(edge.id));
	if (targetId && targetId !== id) {
		lf.addEdge({ type: 'wf-edge', sourceNodeId: id, targetNodeId: targetId, properties: { kind: 'flow' } });
	}
	nextNodeId.value = readNextNodeId(id);
	refreshGraphState();
};

/* -------------------------------------------------------------------------- */
/*                                  节点操作                                    */
/* -------------------------------------------------------------------------- */

const startDrag = (meta: WorkflowNodeMeta, event: MouseEvent) => {
	if (!lf || event.button !== 0) return;
	const node = createWorkflowNode(meta.type);
	lf.dnd.startDrag({ type: meta.lfType, properties: buildNodeProperties(node, agentNameMap.value) });
};

let addOffset = 0;

const addNodeByClick = (meta: WorkflowNodeMeta) => {
	if (!lf || !canvasRef.value) return;
	const rect = canvasRef.value.getBoundingClientRect();
	const point = lf.getPointByClient(rect.left + rect.width / 2, rect.top + rect.height / 2).canvasOverlayPosition;
	// 连点加入画布时按 5 列阶梯错开，避免新节点互相压住
	const slot = addOffset++ % 15;
	const node = createWorkflowNode(meta.type, { x: point.x + (slot % 5) * 30, y: point.y + Math.floor(slot / 5) * 30 });
	lf.addNode({
		id: node.id,
		type: meta.lfType,
		x: node.x ?? point.x,
		y: node.y ?? point.y,
		properties: buildNodeProperties(node, agentNameMap.value),
	});
	selectNode(node.id);
};

const handleDeleteSelected = () => {
	if (!lf) return;
	if (selectedNodeId.value) {
		lf.deleteNode(selectedNodeId.value);
		clearSelection();
		return;
	}
	if (selectedEdgeId.value) {
		const edge = lf.graphModel.getEdgeModelById(selectedEdgeId.value);
		if (edge && !edge.properties?.projected) lf.deleteEdge(selectedEdgeId.value);
		selectedEdgeId.value = '';
	}
};

const handleUndo = () => {
	if (!lf || !lf.history.undoAble()) return;
	lf.undo();
};

const handleRedo = () => {
	if (!lf || !lf.history.redoAble()) return;
	lf.redo();
};

const fitCanvas = () => lf?.fitView(30);

/** LogicFlow 的 zoom(true/false) 按内置刻度步进，zoom(number) 才是绝对缩放 */
const zoomIn = () => lf?.zoom(true);
const zoomOut = () => lf?.zoom(false);
const resetZoom = () => lf?.resetZoom();

/* -------------------------------------------------------------------------- */
/*                              校验 / 保存 / 发布                              */
/* -------------------------------------------------------------------------- */

const buildDefinition = (): WorkflowDefinitionModel => (lf ? graphToDefinition(lf.getGraphRawData()) : parseDefinition(null));

/** 本地校验 + 服务端兜底校验（服务端与引擎使用同一份规则） */
const runValidation = async (): Promise<boolean> => {
	const definition = buildDefinition();
	const local = validateDefinition(definition);
	errors.value = [...local.errors];
	warnings.value = [...local.warnings];
	if (local.errors.length) {
		showValidateResult.value = true;
		return false;
	}

	try {
		const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowValidateDefinitionPost({ definitionJson: JSON.stringify(definition) });
		const serverErrors = res.data.result?.errors ?? [];
		if (serverErrors.length) {
			errors.value = [...serverErrors];
			showValidateResult.value = true;
			return false;
		}
	} catch (error: any) {
		// 服务端校验失败不阻断保存：保存接口本身会再次校验，避免网络抖动时无法落库
		ElMessage.warning(error?.message ?? '服务端校验暂时不可用，已使用本地规则校验');
	}
	return true;
};

const handleValidate = async () => {
	const passed = await runValidation();
	showValidateResult.value = true;
	passed ? ElMessage.success('结构校验通过') : ElMessage.warning('结构校验未通过，请查看画布左下角的提示');
};

const handleSave = async (publish: boolean) => {
	if (!name.value.trim()) {
		ElMessage.warning('请先填写工作流名称');
		return;
	}
	if (!(await runValidation())) {
		ElMessage.warning('结构校验未通过，请先处理校验结果');
		return;
	}

	saving.value = true;
	try {
		const definitionJson = JSON.stringify(buildDefinition());
		if (!currentId.value) {
			const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowPost({ name: name.value.trim(), description: description.value, definitionJson });
			currentId.value = Number(res.data.result);
		} else {
			await getAPI(AgentWorkflowApi).apiAgentWorkflowPut({ id: currentId.value, name: name.value.trim(), description: description.value, definitionJson });
		}

		if (publish && currentId.value) {
			await getAPI(AgentWorkflowApi).apiAgentWorkflowPublishIdPost(currentId.value);
			ElMessage.success('已发布，可按此版本触发运行');
		} else {
			ElMessage.success('已保存为草稿');
		}

		dirty.value = false;
		await refreshMeta();
		// 新建保存后 currentId 才有值，发布成功时也一定存在；这里统一兜一层，避免把 undefined 抛给父组件
		if (currentId.value) emit('saved', currentId.value);
	} catch (error: any) {
		ElMessage.error(error?.message ?? '保存失败');
	} finally {
		saving.value = false;
	}
};

/** 保存后同步状态与版本号（服务端在定义变更时会自增版本并退回草稿） */
const refreshMeta = async () => {
	if (!currentId.value) return;
	const res = await getAPI(AgentWorkflowApi).apiAgentWorkflowDetailIdGet(currentId.value);
	const detail = res.data.result;
	if (!detail) return;
	status.value = Number(detail.status ?? 0);
	version.value = Number(detail.version ?? 1);
};

/* -------------------------------------------------------------------------- */
/*                                  快捷键                                    */
/* -------------------------------------------------------------------------- */

const isTypingTarget = (target: EventTarget | null) => {
	const element = target as HTMLElement | null;
	if (!element) return false;
	return ['INPUT', 'TEXTAREA', 'SELECT'].includes(element.tagName) || element.isContentEditable;
};

const handleKeydown = (event: KeyboardEvent) => {
	if (!visible.value) return;
	const ctrl = event.ctrlKey || event.metaKey;
	const key = event.key.toLowerCase();

	if (ctrl && key === 's') {
		event.preventDefault();
		handleSave(false);
		return;
	}
	if (isTypingTarget(event.target)) return;

	if (ctrl && key === 'z' && !event.shiftKey) {
		event.preventDefault();
		handleUndo();
		return;
	}
	if (ctrl && (key === 'y' || (key === 'z' && event.shiftKey))) {
		event.preventDefault();
		handleRedo();
		return;
	}
	if (event.key === 'Delete' || event.key === 'Backspace') {
		event.preventDefault();
		handleDeleteSelected();
	}
};

window.addEventListener('keydown', handleKeydown);

/* -------------------------------------------------------------------------- */
/*                                  生命周期                                    */
/* -------------------------------------------------------------------------- */

const handleBeforeClose = async (done: () => void) => {
	if (!dirty.value) {
		done();
		return;
	}
	try {
		await ElMessageBox.confirm('当前有未保存的修改，确定关闭吗？', '提示', {
			type: 'warning',
			confirmButtonText: '放弃修改',
			cancelButtonText: '继续编辑',
		});
		done();
	} catch {
		// 用户选择继续编辑
	}
};

/** 顶部「关闭」走同一套未保存确认（before-close 只在 X / ESC / 遮罩关闭时触发） */
const requestClose = () => handleBeforeClose(() => (visible.value = false));

const handleClosed = () => {
	observer?.disconnect();
	observer = null;
	lf?.destroy();
	lf = null;
	emit('close');
};

onBeforeUnmount(() => {
	window.removeEventListener('keydown', handleKeydown);
	observer?.disconnect();
	observer = null;
	lf?.destroy();
	lf = null;
});
</script>

<style scoped lang="scss">
.wf-designer-dialog {
	// 全屏弹窗固定为视口高度：内部 100% 才有可靠基准，否则内容会把弹窗越撑越高
	height: 100vh !important;
	height: 100dvh !important;
	overflow: hidden;

	:deep(.el-dialog__header) {
		margin: 0;
		padding: 0;
		// 头部是主题色实心底（全局规则），再叠一层同色系渐变让整块「有体积」而不是一块平色
		background: linear-gradient(118deg, var(--el-color-primary) 0%, var(--el-color-primary-dark-2) 96%);
	}
}

.wf-designer {
	display: flex;
	flex-direction: column;
	height: 100%;
	min-height: 0;
	overflow: hidden;
}

/* ------------------------------- 头部 ------------------------------- */

.wf-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	// 与系统里默认弹窗（如职位编辑）的标题栏对齐：8px 内边距 + 21.5px 文字行 ≈ 37.5px
	padding: 0 12px;
	min-height: 38px;

	&__brand {
		display: flex;
		align-items: center;
		gap: 9px;
		min-width: 0;
	}

	&__logo {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 24px;
		height: 24px;
		border-radius: 8px;
		font-size: 14px;
		color: var(--el-color-white);
		background-color: rgba(255, 255, 255, 0.16);
		border: 1px solid rgba(255, 255, 255, 0.24);
		flex-shrink: 0;
	}

	&__text {
		// 标题与状态放在同一行，头部只占一行高度
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
	}

	/* 标题必须是白色：头部是主题色实心底，继承正文色会得到「黑字压蓝底」 */
	&__title {
		font-size: 14px;
		font-weight: 600;
		letter-spacing: 0.2px;
		line-height: 20px;
		color: var(--el-color-white);
		flex-shrink: 0;
	}

	&__sub {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
		font-size: 12px;
		color: rgba(255, 255, 255, 0.7);
	}

	&__status {
		display: inline-flex;
		align-items: center;
		gap: 5px;
		height: 18px;
		padding: 0 8px;
		border-radius: 9px;
		font-size: 11px;
		color: var(--el-color-white);
		background-color: rgba(255, 255, 255, 0.14);
		border: 1px solid rgba(255, 255, 255, 0.18);
		flex-shrink: 0;

		&::before {
			content: '';
			width: 5px;
			height: 5px;
			border-radius: 50%;
			background-color: var(--wf-status-dot, rgba(255, 255, 255, 0.75));
		}

		&.is-success {
			--wf-status-dot: #6ee7b7;
		}

		&.is-warning {
			--wf-status-dot: #fcd34d;
		}
	}

	&__version {
		font-family: 'SFMono-Regular', Consolas, monospace;
		font-size: 11px;
		color: rgba(255, 255, 255, 0.55);
		flex-shrink: 0;
	}

	&__actions {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-shrink: 0;
	}

	&__close {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		padding: 0;
		border-radius: 50%;
		border: 1px solid rgba(255, 255, 255, 0.26);
		background-color: transparent;
		color: rgba(255, 255, 255, 0.86);
		font-size: 15px;
		cursor: pointer;
		transition:
			background-color 0.2s ease,
			border-color 0.2s ease,
			color 0.2s ease;

		&:hover {
			color: var(--el-color-white);
			background-color: rgba(255, 255, 255, 0.16);
			border-color: rgba(255, 255, 255, 0.5);
		}
	}
}

/* 头部按钮：白底实心 button 在主题色上最干净，主操作反白、次操作描边。
   挂到 .wf-head__actions 下提高权重，避免和 .el-button 的 32px 高度打平手时被组件库样式盖掉 */
.wf-head__actions .wf-btn-ghost,
.wf-head__actions .wf-btn-light {
	height: 28px;
	padding: 0 10px;
	border-radius: 7px;
	font-size: 13px;
	font-weight: 500;

	i {
		margin-right: 4px;
		font-size: 13px;
		vertical-align: -2px;
	}
}

.wf-btn-ghost {
	/* 暗色全局规则用 --next-border-color 顶掉所有按钮的 hover 边框，这里固定成白色描边 */
	--next-border-color: rgba(255, 255, 255, 0.66);
	--el-button-bg-color: rgba(255, 255, 255, 0.12);
	--el-button-border-color: rgba(255, 255, 255, 0.4);
	--el-button-text-color: var(--el-color-white);
	--el-button-hover-bg-color: rgba(255, 255, 255, 0.24);
	--el-button-hover-border-color: rgba(255, 255, 255, 0.66);
	--el-button-hover-text-color: var(--el-color-white);
	--el-button-active-bg-color: rgba(255, 255, 255, 0.28);
	--el-button-active-border-color: var(--el-color-white);
	--el-button-active-text-color: var(--el-color-white);
	--el-button-disabled-bg-color: rgba(255, 255, 255, 0.08);
	--el-button-disabled-border-color: rgba(255, 255, 255, 0.2);
	--el-button-disabled-text-color: rgba(255, 255, 255, 0.5);
}

/* 主操作：白底 + 主题色字（头部底色固定为主题色渐变，hover 不跟随暗色主题反转的色阶） */
.wf-btn-light {
	--next-border-color: color-mix(in srgb, var(--el-color-primary) 10%, #fff);
	--el-button-bg-color: var(--el-color-white);
	--el-button-border-color: var(--el-color-white);
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

/* ------------------------------ 文档条 ------------------------------ */

.wf-docbar {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 7px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);

	:deep(.el-input__wrapper) {
		padding: 0 8px;
		border-radius: 6px;
		background-color: transparent;
		box-shadow: none;
		transition: background-color 0.2s ease;

		&:hover {
			background-color: var(--el-fill-color-light);
		}

		&.is-focus {
			background-color: var(--el-fill-color-light);
			box-shadow: inset 0 0 0 1px var(--el-color-primary-light-5);
		}
	}

	&__name {
		width: 240px;
		flex-shrink: 0;

		:deep(.el-input__inner) {
			height: 26px;
			font-size: 14px;
			font-weight: 600;
			color: var(--el-text-color-primary);
		}
	}

	&__sep {
		width: 1px;
		height: 15px;
		background-color: var(--el-border-color);
		flex-shrink: 0;
	}

	&__desc {
		flex: 1;
		min-width: 0;

		:deep(.el-input__inner) {
			height: 26px;
			font-size: 12px;
		}
	}

	&__help {
		font-size: 15px;
		color: var(--el-text-color-placeholder);
		cursor: help;
		flex-shrink: 0;
		transition: color 0.2s ease;

		&:hover {
			color: var(--el-color-primary);
		}
	}
}

.wf-designer__body {
	display: flex;
	flex: 1;
	min-height: 0;
	overflow: hidden;
}

.wf-palette {
	display: flex;
	flex-direction: column;
	width: 200px;
	flex-shrink: 0;
	min-height: 0;
	border-right: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);

	&__head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		// 左右与右侧属性面板、画布工具条统一 12px，三栏的视觉基线才对得齐
		padding: 14px 12px 8px;
		font-size: 12.5px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	&__hint {
		font-size: 11px;
		font-weight: 400;
		color: var(--el-text-color-placeholder);
	}

	&__list {
		display: flex;
		flex-direction: column;
		gap: 6px;
		padding: 0 12px 10px;
		flex: 1;
		min-height: 0;
		overflow-y: auto;
	}

	&__group {
		display: flex;
		flex-direction: column;
		gap: 6px;

		& + & {
			margin-top: 6px;
		}
	}

	&__group-title {
		padding: 2px 2px 0;
		font-size: 11px;
		font-weight: 600;
		letter-spacing: 0.3px;
		color: var(--el-text-color-placeholder);
	}

	&__item {
		display: flex;
		align-items: center;
		gap: 9px;
		padding: 8px 9px;
		border-radius: 9px;
		border: 1px solid var(--el-border-color-lighter);
		background-color: var(--el-fill-color-blank);
		cursor: grab;
		transition:
			border-color 0.2s ease,
			background-color 0.2s ease,
			box-shadow 0.2s ease,
			transform 0.2s ease;
		user-select: none;

		&:hover {
			border-color: color-mix(in srgb, var(--wf-accent) 55%, transparent);
			background-color: color-mix(in srgb, var(--wf-accent) 5%, var(--el-fill-color-blank));
			box-shadow: 0 4px 12px -6px color-mix(in srgb, var(--wf-accent) 60%, transparent);
			transform: translateY(-1px);

			.wf-palette__icon {
				background-color: color-mix(in srgb, var(--wf-accent) 14%, transparent);
			}
		}

		&:active {
			cursor: grabbing;
			transform: translateY(0);
		}
	}

	&__icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 26px;
		height: 26px;
		border-radius: 8px;
		font-size: 15px;
		color: var(--wf-accent);
		background-color: color-mix(in srgb, var(--wf-accent) 9%, transparent);
		flex-shrink: 0;
		transition: background-color 0.2s ease;
	}

	&__text {
		min-width: 0;
	}

	&__name {
		font-size: 12.5px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		line-height: 17px;
	}

	&__desc {
		margin-top: 1px;
		font-size: 11px;
		line-height: 15px;
		color: var(--el-text-color-secondary);
		display: -webkit-box;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	&__foot {
		margin-top: auto;
		padding: 10px 12px 12px;
		border-top: 1px dashed var(--el-border-color-lighter);
		display: flex;
		flex-direction: column;
		gap: 4px;
	}

	&__shortcut {
		font-size: 11px;
		color: var(--el-text-color-placeholder);

		kbd {
			padding: 0 4px;
			border-radius: 3px;
			border: 1px solid var(--el-border-color);
			background-color: var(--el-fill-color-light);
			font-family: monospace;
			font-size: 10px;
		}
	}
}

.wf-canvas-wrap {
	position: relative;
	flex: 1;
	min-width: 0;
	// 画布保持中性底色，不加任何色晕，只靠点阵区分画布与两侧面板
	background-color: var(--el-bg-color-overlay, #ffffff);
}

.wf-canvas {
	position: absolute;
	inset: 0;

	// LogicFlow 自带 #fff 底色，统一交给外层容器决定
	:deep(.lf-graph) {
		background: transparent;
	}
}

/* 主流程连线上的流动光点：叠加一层虚线沿同一路径位移，表达「数据在往前走」。
   只作用于主流程边（投影的配置边是虚线语义，选中态由整条高亮表达），不会互相打架 */
:deep(.wf-edge-flow) {
	pointer-events: none;
	animation: wf-edge-flow 1.1s linear infinite;
}

@keyframes wf-edge-flow {
	from {
		stroke-dashoffset: 0;
	}
	to {
		stroke-dashoffset: -14;
	}
}

/* 画布工具条：磨砂玻璃小胶囊，浮在画布左上角 */
.wf-canvas-tools {
	position: absolute;
	top: 12px;
	left: 12px;
	z-index: 5;
	display: flex;
	align-items: center;
	gap: 2px;
	padding: 3px;
	max-width: calc(100% - 24px);
	flex-wrap: wrap;
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	background-color: color-mix(in srgb, var(--el-bg-color-overlay) 86%, transparent);
	backdrop-filter: blur(8px);
	box-shadow: 0 4px 16px -8px rgba(15, 23, 42, 0.28);
}

.wf-tool {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	padding: 0;
	border: none;
	border-radius: 7px;
	background-color: transparent;
	color: var(--el-text-color-regular);
	font-size: 15px;
	cursor: pointer;
	transition:
		background-color 0.18s ease,
		color 0.18s ease;

	&:hover:not(:disabled) {
		color: var(--el-color-primary);
		background-color: var(--el-fill-color);
	}

	&:disabled {
		color: var(--el-text-color-placeholder);
		cursor: not-allowed;
		opacity: 0.45;
	}

	&--zoom {
		min-width: 44px;
		font-size: 11.5px;
		font-family: 'SFMono-Regular', Consolas, monospace;
		color: var(--el-text-color-secondary);
	}

	&--text {
		width: auto;
		padding: 0 8px;
		gap: 4px;
		font-size: 12px;

		i {
			font-size: 14px;
		}
	}
}

.wf-tool-sep {
	width: 1px;
	height: 14px;
	margin: 0 3px;
	background-color: var(--el-border-color-lighter);
}

/* 画布右下角的规模提示：只报事实，不抢视线 */
.wf-canvas-stat {
	position: absolute;
	right: 12px;
	bottom: 12px;
	padding: 2px 9px;
	border-radius: 10px;
	font-size: 11px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	background-color: color-mix(in srgb, var(--el-bg-color-overlay) 80%, transparent);
	border: 1px solid var(--el-border-color-lighter);
	pointer-events: none;
	z-index: 4;
}

.wf-props {
	width: 320px;
	flex-shrink: 0;
	border-left: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);
	overflow: hidden;
}

.wf-validate {
	position: absolute;
	left: 12px;
	bottom: 12px;
	width: 380px;
	max-height: 230px;
	display: flex;
	flex-direction: column;
	padding: 10px 12px 8px;
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	background-color: color-mix(in srgb, var(--el-bg-color-overlay) 92%, transparent);
	backdrop-filter: blur(8px);
	box-shadow: 0 8px 24px -12px rgba(15, 23, 42, 0.35);
	z-index: 5;

	&__head {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	&__title {
		display: flex;
		align-items: center;
		gap: 6px;
		font-size: 12.5px;
		font-weight: 600;
		color: var(--el-text-color-primary);

		i {
			font-size: 14px;
			color: var(--el-color-primary);
		}
	}

	&__list {
		margin: 6px 0 0;
		padding: 0;
		list-style: none;
		overflow-y: auto;
		font-size: 12px;
		line-height: 19px;

		li {
			position: relative;
			padding-left: 12px;

			&::before {
				content: '';
				position: absolute;
				left: 2px;
				top: 8px;
				width: 4px;
				height: 4px;
				border-radius: 50%;
				background-color: currentColor;
			}
		}

		&.is-error {
			color: var(--el-color-danger);
		}

		&.is-warning {
			color: var(--el-color-warning);
		}
	}

	&__ok {
		margin-top: 6px;
		font-size: 12px;
		color: var(--el-color-success);
	}
}

.wf-fade-enter-active,
.wf-fade-leave-active {
	transition:
		opacity 0.18s ease,
		transform 0.18s ease;
}

.wf-fade-enter-from,
.wf-fade-leave-to {
	opacity: 0;
	transform: translateY(6px);
}

@media (max-width: 1200px) {
	.wf-palette {
		width: 172px;
	}

	.wf-props {
		width: 300px;
	}

	.wf-docbar__name {
		width: 200px;
	}
}
</style>

<style lang="scss">
/* 网格点色走变量：LogicFlow 的点阵颜色是写死的主题色，只能靠 CSS 覆盖（CSS 声明优先于 SVG 表现属性）。
 * 亮色下压到几乎看不见，只留一层「纸的纹理」；暗色单独提亮一点，避免在白底上刺眼。 */
.wf-designer-dialog {
	--wf-grid-dot: rgba(100, 116, 139, 0.13);
}

[data-theme='dark'] .wf-designer-dialog {
	--wf-grid-dot: rgba(148, 163, 184, 0.16);
}

.wf-designer-dialog .lf-grid pattern g {
	fill: var(--wf-grid-dot);
}

/* 边标签底在主题里写死成白色（edgeText.background.fill），暗色下会变成「白底浅字」的贴片 */
.wf-designer-dialog .lf-line-text rect {
	fill: var(--el-bg-color-overlay, #ffffff);
	rx: 4px;
}

/* 全局给弹窗内容区设了 `.el-overlay .el-overlay-dialog .el-dialog .el-dialog__body`
 * 的 padding: 18px !important 与 overflow-y: auto（四层类选择器 + !important），
 * 设计器要的是「内容铺满、各栏自己滚动」，必须用更高权重的同一条链路覆盖掉，
 * 否则四周会多出 18px 内边距，并把弹窗整体撑出滚动条。 */
.el-overlay .el-overlay-dialog .el-dialog.wf-designer-dialog .el-dialog__body {
	flex: 1;
	min-height: 0;
	padding: 0 !important;
	overflow: hidden !important;
}
</style>
