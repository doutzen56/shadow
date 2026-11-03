/**
 * 工作流定义 ⇄ LogicFlow 图数据映射
 *
 * 后端的路由约束：
 *  - 条件节点的路由写在 Branches[].Next（条件节点不允许普通出边）；
 *  - 并行节点的分支入口写在 ParallelNodeIds（并行节点最多一条「汇合后继续」出边）；
 * 画布把这两类路由投影成「虚线边」用于可视化，保存时以节点属性为准，避免两处数据打架。
 */
import {
	WORKFLOW_NODE_META_LIST,
	WORKFLOW_NODE_TYPE,
	WorkflowConditionBranch,
	WorkflowDefinitionModel,
	WorkflowNodeModel,
	WorkflowNodeType,
	WorkflowVariableAssignment,
	createDefaultDefinition,
	createNodeId,
	createWorkflowNode,
	describeBranch,
	describeNode,
	getNodeMeta,
} from '../types';

/** 变量名规则：与后端 AgentWorkflowDefinition 的校验保持一致 */
const VARIABLE_NAME_REGEX = /^[\w\u4e00-\u9fa5-]+$/;

/** HTTP 节点允许的请求方法（与后端 AgentWorkflowDefinition.HttpMethods 一致） */
const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];

const toNumberOrUndefined = (value: unknown) => {
	const num = Number(value);
	return Number.isFinite(num) ? num : undefined;
};

const isWorkflowNodeType = (value: number): value is WorkflowNodeType => !!WORKFLOW_NODE_META_LIST.find((item) => item.type === value);

const normalizeBranch = (raw: any): WorkflowConditionBranch => ({
	when: String(raw?.when ?? ''),
	value: String(raw?.value ?? ''),
	next: String(raw?.next ?? ''),
});

const normalizeAssignments = (raw: any): WorkflowVariableAssignment[] =>
	Array.isArray(raw) ? raw.map((item: any) => ({ name: String(item?.name ?? ''), valueTemplate: String(item?.valueTemplate ?? '') })) : [];

/** 文本是否为 JSON 对象（工具参数、HTTP 请求头要求对象形态） */
const isJsonObject = (text?: string): boolean => {
	if (!text || !text.trim()) return false;
	try {
		const value = JSON.parse(text);
		return !!value && typeof value === 'object' && !Array.isArray(value);
	} catch {
		return false;
	}
};

const normalizeNode = (raw: any): WorkflowNodeModel => {
	const rawType = Number(raw?.type);
	const type = isWorkflowNodeType(rawType) ? rawType : WORKFLOW_NODE_TYPE.Agent;
	const meta = getNodeMeta(type);
	const node: WorkflowNodeModel = {
		id: String(raw?.id ?? '') || createNodeId(type),
		type,
		name: String(raw?.name ?? '') || meta.defaultName,
		x: toNumberOrUndefined(raw?.x),
		y: toNumberOrUndefined(raw?.y),
	};

	if (type === WORKFLOW_NODE_TYPE.Agent) {
		node.agentId = Number(raw?.agentId ?? 0);
		node.inputTemplate = String(raw?.inputTemplate ?? '');
		node.systemPromptAppend = String(raw?.systemPromptAppend ?? '');
		node.timeoutSeconds = Number(raw?.timeoutSeconds ?? 0);
		node.onError = String(raw?.onError ?? 'Stop');
	}
	if (type === WORKFLOW_NODE_TYPE.Condition) {
		node.branches = Array.isArray(raw?.branches) && raw.branches.length ? raw.branches.map(normalizeBranch) : [{ when: '', value: '', next: '' }];
	}
	if (type === WORKFLOW_NODE_TYPE.Parallel) {
		node.parallelNodeIds = Array.isArray(raw?.parallelNodeIds) ? raw.parallelNodeIds.map(String) : [];
		node.joinMode = String(raw?.joinMode ?? 'All');
	}
	if (type === WORKFLOW_NODE_TYPE.Approval) {
		node.approvalTimeoutMinutes = Number(raw?.approvalTimeoutMinutes ?? 0);
		node.approvalTimeoutAction = String(raw?.approvalTimeoutAction ?? 'Reject');
	}
	if (type === WORKFLOW_NODE_TYPE.End) {
		node.inputTemplate = String(raw?.inputTemplate ?? '');
	}
	if (type === WORKFLOW_NODE_TYPE.Knowledge) {
		node.kmsId = Number(raw?.kmsId ?? 0);
		node.queryTemplate = String(raw?.queryTemplate ?? '');
		node.topN = Number(raw?.topN ?? 5);
		node.scoreThreshold = Number(raw?.scoreThreshold ?? 0);
		node.timeoutSeconds = Number(raw?.timeoutSeconds ?? 0);
		node.onError = String(raw?.onError ?? 'Stop');
	}
	if (type === WORKFLOW_NODE_TYPE.Tool) {
		node.skillId = Number(raw?.skillId ?? 0);
		node.toolName = String(raw?.toolName ?? '');
		node.argumentsTemplate = String(raw?.argumentsTemplate ?? '');
		node.timeoutSeconds = Number(raw?.timeoutSeconds ?? 0);
		node.onError = String(raw?.onError ?? 'Stop');
	}
	if (type === WORKFLOW_NODE_TYPE.Http) {
		node.httpUrl = String(raw?.httpUrl ?? '');
		node.httpMethod = String(raw?.httpMethod ?? 'GET');
		node.httpHeaders = String(raw?.httpHeaders ?? '');
		node.httpBody = String(raw?.httpBody ?? '');
		node.httpContentType = String(raw?.httpContentType ?? 'application/json; charset=utf-8');
		node.httpFailOnError = raw?.httpFailOnError === undefined ? true : Boolean(raw.httpFailOnError);
		node.timeoutSeconds = Number(raw?.timeoutSeconds ?? 0);
		node.onError = String(raw?.onError ?? 'Stop');
	}
	if (type === WORKFLOW_NODE_TYPE.Variable) {
		node.assignments = normalizeAssignments(raw?.assignments);
		if (!node.assignments.length) node.assignments = [{ name: '', valueTemplate: '' }];
	}
	if (type === WORKFLOW_NODE_TYPE.Output) {
		node.outputTemplate = String(raw?.outputTemplate ?? '');
	}
	if (type === WORKFLOW_NODE_TYPE.Loop) {
		node.loopMode = String(raw?.loopMode ?? 'Items');
		node.loopItemsTemplate = String(raw?.loopItemsTemplate ?? '');
		node.loopSeparator = String(raw?.loopSeparator ?? '\n');
		node.loopCount = Number(raw?.loopCount ?? 1);
		node.loopMaxIterations = Number(raw?.loopMaxIterations ?? 20);
		node.loopBodyNodeId = String(raw?.loopBodyNodeId ?? '');
		node.onError = String(raw?.onError ?? 'Stop');
	}
	if (type === WORKFLOW_NODE_TYPE.Delay) {
		node.delaySeconds = Number(raw?.delaySeconds ?? 1);
	}
	if (type === WORKFLOW_NODE_TYPE.SubWorkflow) {
		node.subWorkflowId = Number(raw?.subWorkflowId ?? 0);
		node.subWorkflowInputTemplate = String(raw?.subWorkflowInputTemplate ?? '');
		node.timeoutSeconds = Number(raw?.timeoutSeconds ?? 0);
		node.onError = String(raw?.onError ?? 'Stop');
	}
	return node;
};

/** 解析 DefinitionJson：容错处理空串、坏 JSON 与旧版无坐标数据 */
export const parseDefinition = (json?: string | null): WorkflowDefinitionModel => {
	if (!json || !json.trim()) return createDefaultDefinition();
	try {
		const raw = JSON.parse(json);
		const rawNodes = Array.isArray(raw?.nodes) ? raw.nodes : [];
		if (!rawNodes.length) return createDefaultDefinition();

		const nodes = rawNodes.map(normalizeNode);
		const nodeIds = new Set(nodes.map((node) => node.id));
		const edges = (Array.isArray(raw?.edges) ? raw.edges : [])
			.filter((edge: any) => nodeIds.has(String(edge?.from)) && nodeIds.has(String(edge?.to)))
			.map((edge: any) => ({ from: String(edge.from), to: String(edge.to) }));
		const definition: WorkflowDefinitionModel = {
			startNodeId: nodeIds.has(String(raw?.startNodeId)) ? String(raw.startNodeId) : nodes.find((node) => node.type === WORKFLOW_NODE_TYPE.Start)?.id ?? '',
			nodes,
			edges,
		};
		if (nodes.some((node) => node.x === undefined || node.y === undefined)) assignAutoLayout(definition);
		return definition;
	} catch {
		return createDefaultDefinition();
	}
};

/** 路由邻接表：普通边 + 条件分支 + 并行分支 + 循环体 */
const buildNextMap = (definition: WorkflowDefinitionModel) => {
	const map = new Map<string, string[]>();
	definition.nodes.forEach((node) => map.set(node.id, []));
	definition.edges.forEach((edge) => map.get(edge.from)?.push(edge.to));
	definition.nodes.forEach((node) => {
		if (node.type === WORKFLOW_NODE_TYPE.Condition) node.branches?.forEach((branch) => branch.next && map.get(node.id)?.push(branch.next));
		if (node.type === WORKFLOW_NODE_TYPE.Parallel) node.parallelNodeIds?.forEach((id) => map.get(node.id)?.push(id));
		if (node.type === WORKFLOW_NODE_TYPE.Loop && node.loopBodyNodeId) map.get(node.id)?.push(node.loopBodyNodeId);
	});
	return map;
};

/** 分层自动布局：从开始节点做 BFS，同层纵向排列；旧版定义没有坐标时也能直接可用 */
export const assignAutoLayout = (definition: WorkflowDefinitionModel) => {
	const nextMap = buildNextMap(definition);
	const depthMap = new Map<string, number>();
	const queue: string[] = [];
	const start = definition.startNodeId || definition.nodes.find((node) => node.type === WORKFLOW_NODE_TYPE.Start)?.id;
	if (start) {
		depthMap.set(start, 0);
		queue.push(start);
	}
	while (queue.length) {
		const current = queue.shift() as string;
		const depth = depthMap.get(current) ?? 0;
		(nextMap.get(current) ?? []).forEach((next) => {
			if (!depthMap.has(next)) {
				depthMap.set(next, depth + 1);
				queue.push(next);
			}
		});
	}

	let maxDepth = 0;
	depthMap.forEach((depth) => (maxDepth = Math.max(maxDepth, depth)));
	definition.nodes.forEach((node) => {
		if (!depthMap.has(node.id)) depthMap.set(node.id, maxDepth + 1);
	});

	const layerCount = new Map<number, number>();
	definition.nodes.forEach((node) => {
		const depth = depthMap.get(node.id) ?? 0;
		const index = layerCount.get(depth) ?? 0;
		layerCount.set(depth, index + 1);
		if (node.x === undefined || node.y === undefined) {
			node.x = 200 + depth * 280;
			node.y = 200 + index * 130;
		}
	});
};

/** 节点属性（画布渲染 + 属性面板共用） */
export const buildNodeProperties = (node: WorkflowNodeModel, agentMap: Record<number, string>) => ({
	type: node.type,
	name: node.name,
	summary: describeNode(node, node.agentId ? agentMap[node.agentId] : ''),
	agentId: node.agentId ?? 0,
	agentName: node.agentId ? agentMap[node.agentId] ?? '' : '',
	inputTemplate: node.inputTemplate ?? '',
	systemPromptAppend: node.systemPromptAppend ?? '',
	timeoutSeconds: node.timeoutSeconds ?? 0,
	onError: node.onError ?? 'Stop',
	branches: node.branches?.map((branch) => ({ ...branch })) ?? [],
	parallelNodeIds: [...(node.parallelNodeIds ?? [])],
	joinMode: node.joinMode ?? 'All',
	approvalTimeoutMinutes: node.approvalTimeoutMinutes ?? 0,
	approvalTimeoutAction: node.approvalTimeoutAction ?? 'Reject',
	kmsId: node.kmsId ?? 0,
	queryTemplate: node.queryTemplate ?? '',
	topN: node.topN ?? 5,
	scoreThreshold: node.scoreThreshold ?? 0,
	skillId: node.skillId ?? 0,
	toolName: node.toolName ?? '',
	argumentsTemplate: node.argumentsTemplate ?? '',
	httpUrl: node.httpUrl ?? '',
	httpMethod: node.httpMethod ?? 'GET',
	httpHeaders: node.httpHeaders ?? '',
	httpBody: node.httpBody ?? '',
	httpContentType: node.httpContentType ?? 'application/json; charset=utf-8',
	httpFailOnError: node.httpFailOnError ?? true,
	assignments: node.assignments?.map((assignment) => ({ ...assignment })) ?? [],
	outputTemplate: node.outputTemplate ?? '',
	loopMode: node.loopMode ?? 'Items',
	loopItemsTemplate: node.loopItemsTemplate ?? '',
	loopSeparator: node.loopSeparator ?? '\n',
	loopCount: node.loopCount ?? 1,
	loopMaxIterations: node.loopMaxIterations ?? 20,
	loopBodyNodeId: node.loopBodyNodeId ?? '',
	delaySeconds: node.delaySeconds ?? 1,
	subWorkflowId: node.subWorkflowId ?? 0,
	subWorkflowInputTemplate: node.subWorkflowInputTemplate ?? '',
});

/** 定义 → LogicFlow 图数据 */
export const definitionToGraph = (definition: WorkflowDefinitionModel, agentMap: Record<number, string> = {}) => {
	const nodes = definition.nodes.map((node) => ({
		id: node.id,
		type: getNodeMeta(node.type).lfType,
		x: node.x ?? 200,
		y: node.y ?? 200,
		properties: buildNodeProperties(node, agentMap),
	}));

	const edges: any[] = definition.edges.map((edge) => ({
		type: 'wf-edge',
		sourceNodeId: edge.from,
		targetNodeId: edge.to,
		properties: { kind: 'flow' },
	}));

	definition.nodes.forEach((node) => {
		const meta = getNodeMeta(node.type);
		if (node.type === WORKFLOW_NODE_TYPE.Condition) {
			node.branches?.forEach((branch, index) => {
				if (!branch.next) return;
				edges.push({
					type: 'wf-edge',
					sourceNodeId: node.id,
					targetNodeId: branch.next,
					text: { value: describeBranch(branch) },
					properties: { kind: 'branch', projected: true, accent: meta.accent, conditionNodeId: node.id, branchIndex: index },
				});
			});
		}
		if (node.type === WORKFLOW_NODE_TYPE.Parallel) {
			node.parallelNodeIds?.forEach((targetId, index) => {
				edges.push({
					type: 'wf-edge',
					sourceNodeId: node.id,
					targetNodeId: targetId,
					text: { value: `并行 ${index + 1}` },
					properties: { kind: 'parallel', projected: true, accent: meta.accent, parallelNodeId: node.id, branchIndex: index },
				});
			});
		}
		if (node.type === WORKFLOW_NODE_TYPE.Loop && node.loopBodyNodeId) {
			edges.push({
				type: 'wf-edge',
				sourceNodeId: node.id,
				targetNodeId: node.loopBodyNodeId,
				text: { value: '循环体' },
				properties: { kind: 'loop', projected: true, accent: meta.accent, loopNodeId: node.id },
			});
		}
	});

	return { nodes, edges };
};

/** LogicFlow 节点 → 定义节点（仅保留该类型相关的字段，避免脏数据入库） */
const nodeFromGraph = (graphNode: any): WorkflowNodeModel => {
	const properties = graphNode?.properties ?? {};
	const type = Number(properties.type) as WorkflowNodeType;
	const base = createWorkflowNode(isWorkflowNodeType(type) ? type : WORKFLOW_NODE_TYPE.Agent, {
		id: String(graphNode?.id ?? ''),
		name: String(properties.name ?? ''),
		x: toNumberOrUndefined(graphNode?.x),
		y: toNumberOrUndefined(graphNode?.y),
	});
	return { ...base, ...(isWorkflowNodeType(type) ? {} : { type: WORKFLOW_NODE_TYPE.Agent }) };
};

/** 把画布当前属性回填到定义节点（属性面板的编辑都落在 properties 上） */
const mergeProperties = (node: WorkflowNodeModel, properties: any): WorkflowNodeModel => {
	const merged: WorkflowNodeModel = {
		id: node.id,
		type: node.type,
		name: String(properties?.name ?? node.name),
		x: node.x,
		y: node.y,
	};
	if (node.type === WORKFLOW_NODE_TYPE.Agent) {
		merged.agentId = Number(properties?.agentId ?? 0);
		merged.inputTemplate = String(properties?.inputTemplate ?? '');
		merged.systemPromptAppend = String(properties?.systemPromptAppend ?? '');
		merged.timeoutSeconds = Number(properties?.timeoutSeconds ?? 0);
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Condition) {
		merged.branches = Array.isArray(properties?.branches) ? properties.branches.map(normalizeBranch) : [];
	}
	if (node.type === WORKFLOW_NODE_TYPE.Parallel) {
		merged.parallelNodeIds = Array.isArray(properties?.parallelNodeIds) ? properties.parallelNodeIds.map(String) : [];
		merged.joinMode = String(properties?.joinMode ?? 'All');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Approval) {
		merged.approvalTimeoutMinutes = Number(properties?.approvalTimeoutMinutes ?? 0);
		merged.approvalTimeoutAction = String(properties?.approvalTimeoutAction ?? 'Reject');
	}
	if (node.type === WORKFLOW_NODE_TYPE.End) {
		merged.inputTemplate = String(properties?.inputTemplate ?? '');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Knowledge) {
		merged.kmsId = Number(properties?.kmsId ?? 0);
		merged.queryTemplate = String(properties?.queryTemplate ?? '');
		merged.topN = Number(properties?.topN ?? 5);
		merged.scoreThreshold = Number(properties?.scoreThreshold ?? 0);
		merged.timeoutSeconds = Number(properties?.timeoutSeconds ?? 0);
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Tool) {
		merged.skillId = Number(properties?.skillId ?? 0);
		merged.toolName = String(properties?.toolName ?? '');
		merged.argumentsTemplate = String(properties?.argumentsTemplate ?? '');
		merged.timeoutSeconds = Number(properties?.timeoutSeconds ?? 0);
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Http) {
		merged.httpUrl = String(properties?.httpUrl ?? '');
		merged.httpMethod = String(properties?.httpMethod ?? 'GET');
		merged.httpHeaders = String(properties?.httpHeaders ?? '');
		merged.httpBody = String(properties?.httpBody ?? '');
		merged.httpContentType = String(properties?.httpContentType ?? 'application/json; charset=utf-8');
		merged.httpFailOnError = properties?.httpFailOnError === undefined ? true : Boolean(properties.httpFailOnError);
		merged.timeoutSeconds = Number(properties?.timeoutSeconds ?? 0);
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Variable) {
		merged.assignments = normalizeAssignments(properties?.assignments);
	}
	if (node.type === WORKFLOW_NODE_TYPE.Output) {
		merged.outputTemplate = String(properties?.outputTemplate ?? '');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Loop) {
		merged.loopMode = String(properties?.loopMode ?? 'Items');
		merged.loopItemsTemplate = String(properties?.loopItemsTemplate ?? '');
		merged.loopSeparator = String(properties?.loopSeparator ?? '\n');
		merged.loopCount = Number(properties?.loopCount ?? 1);
		merged.loopMaxIterations = Number(properties?.loopMaxIterations ?? 20);
		merged.loopBodyNodeId = String(properties?.loopBodyNodeId ?? '');
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	if (node.type === WORKFLOW_NODE_TYPE.Delay) {
		merged.delaySeconds = Number(properties?.delaySeconds ?? 1);
	}
	if (node.type === WORKFLOW_NODE_TYPE.SubWorkflow) {
		merged.subWorkflowId = Number(properties?.subWorkflowId ?? 0);
		merged.subWorkflowInputTemplate = String(properties?.subWorkflowInputTemplate ?? '');
		merged.timeoutSeconds = Number(properties?.timeoutSeconds ?? 0);
		merged.onError = String(properties?.onError ?? 'Stop');
	}
	return merged;
};

/** LogicFlow 图数据 → 定义（分支/并行虚线边不落库） */
export const graphToDefinition = (graph: any): WorkflowDefinitionModel => {
	const nodes = (graph?.nodes ?? []).map((graphNode: any) => mergeProperties(nodeFromGraph(graphNode), graphNode.properties));
	const nodeIds = new Set(nodes.map((node) => node.id));
	const edges = (graph?.edges ?? [])
		.filter((edge: any) => !edge.properties?.projected)
		.filter((edge: any) => nodeIds.has(String(edge.sourceNodeId)) && nodeIds.has(String(edge.targetNodeId)))
		.map((edge: any) => ({ from: String(edge.sourceNodeId), to: String(edge.targetNodeId) }));
	return {
		startNodeId: nodes.find((node) => node.type === WORKFLOW_NODE_TYPE.Start && nodeIds.has(node.id))?.id ?? '',
		nodes,
		edges,
	};
};

/**
 * 本地结构校验（镜像后端 AgentWorkflowDefinition.Validate，错误口径一致）
 * errors 阻断保存/发布，warnings 仅提示（例如节点不可达、名称重复）
 */
export const validateDefinition = (definition: WorkflowDefinitionModel): { errors: string[]; warnings: string[] } => {
	const errors: string[] = [];
	const warnings: string[] = [];
	if (!definition.nodes.length) return { errors: ['流程没有任何节点'], warnings };

	const nodeIds = new Set<string>();
	const duplicatedIds = new Set<string>();
	definition.nodes.forEach((node) => {
		if (nodeIds.has(node.id)) duplicatedIds.add(node.id);
		nodeIds.add(node.id);
	});
	if (duplicatedIds.size) errors.push(`节点 Id 重复：${[...duplicatedIds].join(', ')}`);
	if (definition.nodes.some((node) => !node.name?.trim())) errors.push('存在未设置名称的节点');

	const findNode = (id: string) => definition.nodes.find((node) => node.id === id);
	const startNode = findNode(definition.startNodeId);
	if (!startNode) errors.push('流程缺少开始节点（请添加「开始」节点）');
	else if (startNode.type !== WORKFLOW_NODE_TYPE.Start) errors.push('开始节点指向的节点类型必须为「开始」');
	if (definition.nodes.filter((node) => node.type === WORKFLOW_NODE_TYPE.Start).length > 1) errors.push('开始节点只能有一个');
	if (!definition.nodes.some((node) => node.type === WORKFLOW_NODE_TYPE.End)) errors.push('流程缺少「结束」节点');

	definition.nodes.forEach((node) => {
		const outEdges = definition.edges.filter((edge) => edge.from === node.id);
		const targets: string[] = [];
		switch (node.type) {
			case WORKFLOW_NODE_TYPE.Agent:
				if (!node.agentId) errors.push(`智能体节点「${node.name}」未绑定智能体`);
				break;
			case WORKFLOW_NODE_TYPE.Condition:
				if (!node.branches?.length) errors.push(`条件节点「${node.name}」至少需要一个分支`);
				else {
					if (!node.branches.some((branch) => !branch.when)) errors.push(`条件节点「${node.name}」缺少默认分支（When 留空即默认）`);
					node.branches.forEach((branch) => branch.next && targets.push(branch.next));
				}
				break;
			case WORKFLOW_NODE_TYPE.Parallel:
				if (!node.parallelNodeIds || node.parallelNodeIds.length < 2) errors.push(`并行节点「${node.name}」至少需要两条分支`);
				else {
					targets.push(...node.parallelNodeIds);
					node.parallelNodeIds.forEach((id) => {
						const target = findNode(id);
						if (target && (target.type === WORKFLOW_NODE_TYPE.Start || target.type === WORKFLOW_NODE_TYPE.Approval)) {
							errors.push(`并行节点「${node.name}」的分支入口「${target.name}」不能是开始或人工审批节点`);
						}
					});
				}
				break;
			case WORKFLOW_NODE_TYPE.Approval:
				if ((node.approvalTimeoutMinutes ?? 0) < 0) errors.push(`审批节点「${node.name}」超时分钟数不能为负`);
				break;
			case WORKFLOW_NODE_TYPE.Knowledge:
				if (!node.kmsId) errors.push(`知识库节点「${node.name}」未选择知识库`);
				if (!node.queryTemplate?.trim()) errors.push(`知识库节点「${node.name}」未设置检索词模板`);
				if ((node.topN ?? 5) < 1 || (node.topN ?? 5) > 50) errors.push(`知识库节点「${node.name}」召回条数必须在 1-50 之间`);
				if ((node.scoreThreshold ?? 0) < 0 || (node.scoreThreshold ?? 0) > 1) errors.push(`知识库节点「${node.name}」相似度阈值必须在 0-1 之间`);
				break;
			case WORKFLOW_NODE_TYPE.Tool:
				if (!node.skillId) errors.push(`工具节点「${node.name}」未选择工具或 MCP 服务`);
				if (!node.toolName?.trim()) errors.push(`工具节点「${node.name}」未指定要调用的工具名称`);
				if (node.argumentsTemplate?.trim() && !isJsonObject(node.argumentsTemplate)) errors.push(`工具节点「${node.name}」的调用参数必须是 JSON 对象`);
				break;
			case WORKFLOW_NODE_TYPE.Http:
				if (!node.httpUrl?.trim()) errors.push(`HTTP 节点「${node.name}」未填写请求地址`);
				if (!HTTP_METHODS.includes((node.httpMethod ?? '').trim().toUpperCase())) errors.push(`HTTP 节点「${node.name}」的请求方法只能是 GET / POST / PUT / PATCH / DELETE`);
				if (node.httpHeaders?.trim() && !isJsonObject(node.httpHeaders)) errors.push(`HTTP 节点「${node.name}」的请求头必须是 JSON 对象`);
				break;
			case WORKFLOW_NODE_TYPE.Variable: {
				const assignments = node.assignments ?? [];
				if (!assignments.length) {
					errors.push(`变量节点「${node.name}」至少需要一项赋值`);
					break;
				}
				const names = new Set<string>();
				assignments.forEach((assignment) => {
					const variableName = assignment.name?.trim() ?? '';
					if (!variableName) errors.push(`变量节点「${node.name}」存在未命名的变量`);
					else if (!VARIABLE_NAME_REGEX.test(variableName)) errors.push(`变量节点「${node.name}」的变量名「${variableName}」只能包含字母、数字、下划线、中文或短横线`);
					else if (names.has(variableName.toLowerCase())) errors.push(`变量节点「${node.name}」的变量名「${variableName}」重复`);
					else names.add(variableName.toLowerCase());
				});
				break;
			}
			case WORKFLOW_NODE_TYPE.Loop: {
				const maxIterations = node.loopMaxIterations ?? 20;
				if (maxIterations < 1 || maxIterations > 100) errors.push(`循环节点「${node.name}」的最大迭代次数必须在 1-100 之间`);
				if ((node.loopMode ?? 'Items').toUpperCase() === 'COUNT') {
					if ((node.loopCount ?? 1) < 1 || (node.loopCount ?? 1) > 100) errors.push(`循环节点「${node.name}」的循环次数必须在 1-100 之间`);
				} else if (!node.loopItemsTemplate?.trim()) {
					errors.push(`循环节点「${node.name}」未设置迭代数据模板`);
				}
				const bodyNode = node.loopBodyNodeId ? findNode(node.loopBodyNodeId) : undefined;
				if (!bodyNode) errors.push(`循环节点「${node.name}」未选择循环体入口节点`);
				else if (bodyNode.id === node.id) errors.push(`循环节点「${node.name}」的循环体不能指向自身`);
				else if (bodyNode.type === WORKFLOW_NODE_TYPE.Start || bodyNode.type === WORKFLOW_NODE_TYPE.Approval) errors.push(`循环节点「${node.name}」的循环体入口不能是开始或人工审批节点`);
				if (bodyNode) targets.push(bodyNode.id);
				break;
			}
			case WORKFLOW_NODE_TYPE.Delay:
				if ((node.delaySeconds ?? 1) < 0 || (node.delaySeconds ?? 1) > 3600) errors.push(`延迟节点「${node.name}」的等待秒数必须在 0-3600 之间`);
				break;
			case WORKFLOW_NODE_TYPE.SubWorkflow:
				if (!node.subWorkflowId) errors.push(`子流程节点「${node.name}」未选择要调用的工作流`);
				break;
		}

		// 出边约束：条件节点路由完全由分支表达；结束节点没有出边；其余节点至多一条
		if (node.type === WORKFLOW_NODE_TYPE.Condition && outEdges.length) errors.push(`条件节点「${node.name}」的路由由分支配置表达，不能直接连线`);
		if (node.type === WORKFLOW_NODE_TYPE.End && outEdges.length) errors.push(`结束节点「${node.name}」不能有出边`);
		if (node.type === WORKFLOW_NODE_TYPE.Parallel && outEdges.length > 1) errors.push(`并行节点「${node.name}」汇合后最多只能有一条出边（指向继续执行的下一节点）`);
		if (node.type !== WORKFLOW_NODE_TYPE.Condition && node.type !== WORKFLOW_NODE_TYPE.End && outEdges.length > 1) {
			errors.push(`节点「${node.name}」最多只能有一条出边（多路路由请使用条件/并行节点）`);
		}

		targets.push(...outEdges.map((edge) => edge.to));
		// 会继续往下走的节点没有出边时流程会提前结束，给出提示而非阻断
		if (node.type !== WORKFLOW_NODE_TYPE.End && node.type !== WORKFLOW_NODE_TYPE.Condition && !outEdges.length) {
			warnings.push(`节点「${node.name}」没有后续节点，流程会在此之前结束`);
		}
		targets.forEach((target) => {
			if (!findNode(target)) errors.push(`节点「${node.name}」引用了不存在的目标节点 ${target}`);
		});
	});

	definition.edges.forEach((edge) => {
		if (!findNode(edge.from)) errors.push(`存在起点不存在的边：${edge.from} → ${edge.to}`);
		if (!findNode(edge.to)) errors.push(`存在终点不存在的边：${edge.from} → ${edge.to}`);
	});

	// 名称重复会让 {{step:节点名}} 指向不确定的输出（后写覆盖先写）
	const nameCount = new Map<string, number>();
	definition.nodes.forEach((node) => nameCount.set(node.name, (nameCount.get(node.name) ?? 0) + 1));
	nameCount.forEach((count, name) => {
		if (count > 1) warnings.push(`节点名称「${name}」重复，模板引用 {{step:${name}}} 时结果不确定`);
	});

	// 不可达节点：只做提示，允许先搭结构再连线
	if (definition.nodes.length) {
		const reachable = new Set<string>();
		const nextMap = buildNextMap(definition);
		const queue = definition.startNodeId ? [definition.startNodeId] : [];
		while (queue.length) {
			const current = queue.shift() as string;
			if (reachable.has(current)) continue;
			reachable.add(current);
			(nextMap.get(current) ?? []).forEach((next) => !reachable.has(next) && queue.push(next));
		}
		definition.nodes.forEach((node) => {
			if (!reachable.has(node.id)) warnings.push(`节点「${node.name}」从开始节点不可达`);
		});
	}

	return { errors, warnings };
};
