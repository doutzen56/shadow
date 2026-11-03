/**
 * 智能体工作流 · 前端数据模型与展示元数据
 *
 * 与后端 AgentWorkflowDefinition 一一对应（JSON 采用 camelCase，后端反序列化不区分大小写）；
 * 额外携带 x / y 画布坐标，后端只读取结构字段，坐标会原样保存在 DefinitionJson 里。
 */

export const WORKFLOW_NODE_TYPE = {
	Start: 1,
	Agent: 2,
	Condition: 3,
	Parallel: 4,
	Approval: 5,
	End: 6,
	Knowledge: 7,
	Tool: 8,
	Http: 9,
	Variable: 10,
	Output: 11,
	Loop: 12,
	Delay: 13,
	SubWorkflow: 14,
} as const;

export type WorkflowNodeType = (typeof WORKFLOW_NODE_TYPE)[keyof typeof WORKFLOW_NODE_TYPE];

/** 左侧节点面板的分组 */
export const WORKFLOW_NODE_GROUP = {
	Ai: 'ai',
	Logic: 'logic',
	Action: 'action',
	Manual: 'manual',
} as const;

export type WorkflowNodeGroup = (typeof WORKFLOW_NODE_GROUP)[keyof typeof WORKFLOW_NODE_GROUP];

export const WORKFLOW_NODE_GROUP_META: Record<WorkflowNodeGroup, { label: string }> = {
	ai: { label: 'AI 能力' },
	logic: { label: '流程控制' },
	action: { label: '数据与集成' },
	manual: { label: '入口与人工' },
};

export const WORKFLOW_STATUS = {
	Draft: 0,
	Published: 1,
	Disabled: 2,
} as const;

export const WORKFLOW_RUN_STATUS = {
	Pending: 0,
	Running: 1,
	Success: 2,
	Fail: 3,
	Cancelled: 4,
	Timeout: 5,
	WaitingApproval: 6,
} as const;

export const WORKFLOW_STEP_STATUS = {
	Pending: 0,
	Running: 1,
	Success: 2,
	Fail: 3,
	Skipped: 4,
	WaitingApproval: 5,
} as const;

export interface WorkflowConditionBranch {
	/** 比较操作符（空 = 默认分支，恒命中） */
	when: string;
	/** 比较期望值（支持 {{var:key}} / {{step:节点名}}） */
	value: string;
	/** 命中后路由到的节点 Id */
	next: string;
}

export interface WorkflowEdgeModel {
	from: string;
	to: string;
}

/** 变量赋值项（变量名只能包含字母、数字、下划线、中文或短横线） */
export interface WorkflowVariableAssignment {
	name: string;
	valueTemplate?: string;
}

/** 单个节点：既是 DefinitionJson 的节点结构，也是画布节点的 properties 载体 */
export interface WorkflowNodeModel {
	id: string;
	type: WorkflowNodeType;
	name: string;
	/** 画布坐标（后端忽略，仅用于还原布局） */
	x?: number;
	y?: number;
	agentId?: number;
	systemPromptAppend?: string;
	inputTemplate?: string;
	timeoutSeconds?: number;
	onError?: string;
	branches?: WorkflowConditionBranch[];
	parallelNodeIds?: string[];
	joinMode?: string;
	approvalTimeoutMinutes?: number;
	approvalTimeoutAction?: string;
	/** 知识库检索节点 */
	kmsId?: number;
	queryTemplate?: string;
	topN?: number;
	scoreThreshold?: number;
	/** 工具节点 */
	skillId?: number;
	toolName?: string;
	argumentsTemplate?: string;
	/** HTTP 请求节点 */
	httpUrl?: string;
	httpMethod?: string;
	httpHeaders?: string;
	httpBody?: string;
	httpContentType?: string;
	httpFailOnError?: boolean;
	/** 变量赋值节点 */
	assignments?: WorkflowVariableAssignment[];
	/** 输出节点 */
	outputTemplate?: string;
	/** 循环节点 */
	loopMode?: string;
	loopItemsTemplate?: string;
	loopSeparator?: string;
	loopCount?: number;
	loopMaxIterations?: number;
	loopBodyNodeId?: string;
	/** 延迟节点 */
	delaySeconds?: number;
	/** 子流程节点 */
	subWorkflowId?: number;
	subWorkflowInputTemplate?: string;
}

export interface WorkflowDefinitionModel {
	startNodeId: string;
	nodes: WorkflowNodeModel[];
	edges: WorkflowEdgeModel[];
}

/** 画布节点 properties：结构字段 + 渲染用摘要 */
export interface WorkflowNodeProperties extends WorkflowNodeModel {
	/** 节点卡片副标题（由节点配置推导，避免在视图里读全局状态） */
	summary: string;
}

export interface WorkflowNodeMeta {
	type: WorkflowNodeType;
	/** LogicFlow 注册类型 */
	lfType: string;
	label: string;
	defaultName: string;
	/** remixicon 类名，用于左侧节点面板 */
	icon: string;
	/** 节点主色（画布左侧色条与图标底色） */
	accent: string;
	desc: string;
	/** 左侧面板分组 */
	group: WorkflowNodeGroup;
	/** 胶囊节点（开始/结束）尺寸更小 */
	pill?: boolean;
}

/** 节点主色：比 Element 默认色板再压一档饱和度，画布上更沉稳、也更耐看 */
export const WORKFLOW_NODE_META: Record<WorkflowNodeType, WorkflowNodeMeta> = {
	1: { type: 1, lfType: 'wf-start', label: '开始', defaultName: '开始', icon: 'ri-play-circle-line', accent: '#10B981', desc: '流程唯一入口，接收运行输入', group: 'manual', pill: true },
	2: { type: 2, lfType: 'wf-agent', label: '智能体', defaultName: '智能体节点', icon: 'ri-robot-2-line', accent: '#3B82F6', desc: '调用指定智能体完成一次模型推理', group: 'ai' },
	3: { type: 3, lfType: 'wf-condition', label: '条件分支', defaultName: '条件分支', icon: 'ri-git-branch-line', accent: '#F59E0B', desc: '按上一节点输出择一路由（需配置默认分支）', group: 'logic' },
	4: { type: 4, lfType: 'wf-parallel', label: '并行网关', defaultName: '并行网关', icon: 'ri-git-fork-line', accent: '#8B5CF6', desc: '多条分支并发执行，汇合后继续', group: 'logic' },
	5: { type: 5, lfType: 'wf-approval', label: '人工审批', defaultName: '人工审批', icon: 'ri-user-voice-line', accent: '#F43F5E', desc: '暂停流程等待审批，通过后继续', group: 'manual' },
	6: { type: 6, lfType: 'wf-end', label: '结束', defaultName: '结束', icon: 'ri-stop-circle-line', accent: '#475569', desc: '流程出口，输出最终结果', group: 'manual', pill: true },
	7: { type: 7, lfType: 'wf-knowledge', label: '知识库检索', defaultName: '知识库检索', icon: 'ri-book-read-line', accent: '#0EA5E9', desc: '按检索词在知识库中召回相关片段', group: 'ai' },
	8: { type: 8, lfType: 'wf-tool', label: '工具调用', defaultName: '工具调用', icon: 'ri-tools-line', accent: '#14B8A6', desc: '直接调用指定工具或 MCP 工具', group: 'ai' },
	9: { type: 9, lfType: 'wf-http', label: 'HTTP 请求', defaultName: 'HTTP 请求', icon: 'ri-global-line', accent: '#6366F1', desc: '调用外部接口并把响应交给后续节点', group: 'action' },
	10: { type: 10, lfType: 'wf-variable', label: '变量赋值', defaultName: '变量赋值', icon: 'ri-code-s-slash-line', accent: '#EAB308', desc: '把模板结果写入运行变量', group: 'action' },
	11: { type: 11, lfType: 'wf-output', label: '输出', defaultName: '输出', icon: 'ri-file-text-line', accent: '#EC4899', desc: '渲染一段文本作为当前输出（不结束流程）', group: 'action' },
	12: { type: 12, lfType: 'wf-loop', label: '循环迭代', defaultName: '循环迭代', icon: 'ri-loop-right-line', accent: '#16A34A', desc: '按列表或固定次数重复执行循环体', group: 'logic' },
	13: { type: 13, lfType: 'wf-delay', label: '延迟', defaultName: '延迟', icon: 'ri-timer-line', accent: '#64748B', desc: '等待指定秒数后继续，用于限流与节奏控制', group: 'logic' },
	14: { type: 14, lfType: 'wf-subflow', label: '子流程', defaultName: '子流程', icon: 'ri-flow-chart', accent: '#C026D3', desc: '同步执行另一个已发布工作流并取最终输出', group: 'logic' },
};

export const WORKFLOW_NODE_META_LIST = Object.values(WORKFLOW_NODE_META);

export const getNodeMeta = (type: WorkflowNodeType): WorkflowNodeMeta => WORKFLOW_NODE_META[type] ?? WORKFLOW_NODE_META[2];

/** 条件操作符：与后端 AgentWorkflowTemplate.Evaluate 一一对应 */
export const WORKFLOW_BRANCH_OPERATORS = [
	{ value: '', label: '默认（恒命中）' },
	{ value: 'contains', label: '包含' },
	{ value: 'not_contains', label: '不包含' },
	{ value: 'equals', label: '等于' },
	{ value: 'not_equals', label: '不等于' },
	{ value: 'regex', label: '正则匹配' },
	{ value: 'not_empty', label: '不为空' },
	{ value: 'empty', label: '为空' },
	{ value: 'gt', label: '大于（数值）' },
	{ value: 'lt', label: '小于（数值）' },
];

/** 无需期望值的操作符 */
export const isValueRequiredOperator = (operator: string) => !['', 'not_empty', 'empty'].includes(operator);

export type WorkflowStatus = (typeof WORKFLOW_STATUS)[keyof typeof WORKFLOW_STATUS];
export type WorkflowRunStatus = (typeof WORKFLOW_RUN_STATUS)[keyof typeof WORKFLOW_RUN_STATUS];
export type WorkflowStepStatus = (typeof WORKFLOW_STEP_STATUS)[keyof typeof WORKFLOW_STEP_STATUS];

interface StatusMeta {
	label: string;
	type: 'primary' | 'success' | 'info' | 'warning' | 'danger';
}

export const WORKFLOW_STATUS_META: Record<number, StatusMeta> = {
	0: { label: '草稿', type: 'info' },
	1: { label: '已发布', type: 'success' },
	2: { label: '已停用', type: 'warning' },
};

export const RUN_STATUS_META: Record<number, StatusMeta> = {
	0: { label: '排队中', type: 'info' },
	1: { label: '运行中', type: 'primary' },
	2: { label: '成功', type: 'success' },
	3: { label: '失败', type: 'danger' },
	4: { label: '已取消', type: 'info' },
	5: { label: '已超时', type: 'warning' },
	6: { label: '待审批', type: 'warning' },
};

export const STEP_STATUS_META: Record<number, StatusMeta> = {
	0: { label: '待执行', type: 'info' },
	1: { label: '执行中', type: 'primary' },
	2: { label: '成功', type: 'success' },
	3: { label: '失败', type: 'danger' },
	4: { label: '已跳过', type: 'info' },
	5: { label: '待审批', type: 'warning' },
};

/** 运行是否仍在推进（用于详情轮询与操作按钮可用性判断） */
export const isRunActive = (status?: number | null) =>
	status === WORKFLOW_RUN_STATUS.Pending || status === WORKFLOW_RUN_STATUS.Running || status === WORKFLOW_RUN_STATUS.WaitingApproval;

let nodeSequence = 0;

export const createNodeId = (type: WorkflowNodeType) => `wf_${type}_${Date.now().toString(36)}_${(++nodeSequence).toString(36)}`;

/** 节点卡片副标题 */
export const describeNode = (node: WorkflowNodeModel, agentName?: string | null): string => {
	switch (node.type) {
		case WORKFLOW_NODE_TYPE.Start:
			return '流程入口 · 接收运行输入';
		case WORKFLOW_NODE_TYPE.Agent:
			return agentName ? `智能体 · ${agentName}` : '未绑定智能体';
		case WORKFLOW_NODE_TYPE.Condition:
			return `${node.branches?.length ?? 0} 个分支 · 按顺序求值`;
		case WORKFLOW_NODE_TYPE.Parallel:
			return `${node.parallelNodeIds?.length ?? 0} 条并发分支 · ${node.joinMode === 'Any' ? '任一成功' : '全部成功'}`;
		case WORKFLOW_NODE_TYPE.Approval:
			return node.approvalTimeoutMinutes && node.approvalTimeoutMinutes > 0 ? `人工审批 · ${node.approvalTimeoutMinutes} 分钟超时` : '人工审批 · 不限制超时';
		case WORKFLOW_NODE_TYPE.End:
			return node.inputTemplate ? '流程出口 · 自定义输出' : '流程出口 · 输出上一步结果';
		case WORKFLOW_NODE_TYPE.Knowledge:
			return `知识库 · 召回 ${node.topN ?? 5} 条${node.queryTemplate ? '' : ' · 使用上一节点输出'}`;
		case WORKFLOW_NODE_TYPE.Tool:
			return node.toolName ? `工具 · ${node.toolName}` : '未选择要调用的工具';
		case WORKFLOW_NODE_TYPE.Http: {
			const method = (node.httpMethod || 'GET').toUpperCase();
			if (!node.httpUrl) return `${method} · 未填写请求地址`;
			return `${method} ${node.httpUrl.length > 28 ? `${node.httpUrl.slice(0, 28)}…` : node.httpUrl}`;
		}
		case WORKFLOW_NODE_TYPE.Variable:
			return `${node.assignments?.length ?? 0} 个变量赋值`;
		case WORKFLOW_NODE_TYPE.Output:
			return node.outputTemplate ? '输出 · 自定义模板' : '输出 · 沿用上一节点结果';
		case WORKFLOW_NODE_TYPE.Loop:
			return `${node.loopMode === 'Count' ? `固定 ${node.loopCount ?? 1} 次` : '按列表迭代'} · 最多 ${node.loopMaxIterations ?? 20} 次`;
		case WORKFLOW_NODE_TYPE.Delay:
			return `等待 ${node.delaySeconds ?? 1} 秒`;
		case WORKFLOW_NODE_TYPE.SubWorkflow:
			return node.subWorkflowId ? '调用子流程 · 取最终输出' : '未选择子流程';
		default:
			return '';
	}
};

export const createWorkflowNode = (type: WorkflowNodeType, patch: Partial<WorkflowNodeModel> = {}): WorkflowNodeModel => {
	const meta = getNodeMeta(type);
	const node: WorkflowNodeModel = {
		id: patch.id ?? createNodeId(type),
		type,
		name: patch.name ?? meta.defaultName,
		x: patch.x,
		y: patch.y,
	};
	if (type === WORKFLOW_NODE_TYPE.Agent) {
		node.agentId = patch.agentId ?? 0;
		node.inputTemplate = patch.inputTemplate ?? '';
		node.systemPromptAppend = patch.systemPromptAppend ?? '';
		node.timeoutSeconds = patch.timeoutSeconds ?? 0;
		node.onError = patch.onError ?? 'Stop';
	}
	if (type === WORKFLOW_NODE_TYPE.Condition) {
		node.branches = patch.branches?.length ? patch.branches : [{ when: '', value: '', next: '' }];
	}
	if (type === WORKFLOW_NODE_TYPE.Parallel) {
		node.parallelNodeIds = patch.parallelNodeIds ?? [];
		node.joinMode = patch.joinMode ?? 'All';
	}
	if (type === WORKFLOW_NODE_TYPE.Approval) {
		node.approvalTimeoutMinutes = patch.approvalTimeoutMinutes ?? 0;
		node.approvalTimeoutAction = patch.approvalTimeoutAction ?? 'Reject';
	}
	if (type === WORKFLOW_NODE_TYPE.End) {
		node.inputTemplate = patch.inputTemplate ?? '';
	}
	if (type === WORKFLOW_NODE_TYPE.Knowledge) {
		node.kmsId = patch.kmsId ?? 0;
		node.queryTemplate = patch.queryTemplate ?? '{{input}}';
		node.topN = patch.topN ?? 5;
		node.scoreThreshold = patch.scoreThreshold ?? 0;
		node.timeoutSeconds = patch.timeoutSeconds ?? 0;
		node.onError = patch.onError ?? 'Stop';
	}
	if (type === WORKFLOW_NODE_TYPE.Tool) {
		node.skillId = patch.skillId ?? 0;
		node.toolName = patch.toolName ?? '';
		node.argumentsTemplate = patch.argumentsTemplate ?? '';
		node.timeoutSeconds = patch.timeoutSeconds ?? 0;
		node.onError = patch.onError ?? 'Stop';
	}
	if (type === WORKFLOW_NODE_TYPE.Http) {
		node.httpUrl = patch.httpUrl ?? '';
		node.httpMethod = patch.httpMethod ?? 'GET';
		node.httpHeaders = patch.httpHeaders ?? '';
		node.httpBody = patch.httpBody ?? '';
		node.httpContentType = patch.httpContentType ?? 'application/json; charset=utf-8';
		node.httpFailOnError = patch.httpFailOnError ?? true;
		node.timeoutSeconds = patch.timeoutSeconds ?? 30;
		node.onError = patch.onError ?? 'Stop';
	}
	if (type === WORKFLOW_NODE_TYPE.Variable) {
		node.assignments = patch.assignments?.length ? patch.assignments.map((item) => ({ ...item })) : [{ name: '', valueTemplate: '' }];
	}
	if (type === WORKFLOW_NODE_TYPE.Output) {
		node.outputTemplate = patch.outputTemplate ?? '';
	}
	if (type === WORKFLOW_NODE_TYPE.Loop) {
		node.loopMode = patch.loopMode ?? 'Items';
		node.loopItemsTemplate = patch.loopItemsTemplate ?? '';
		node.loopSeparator = patch.loopSeparator ?? '\n';
		node.loopCount = patch.loopCount ?? 1;
		node.loopMaxIterations = patch.loopMaxIterations ?? 20;
		node.loopBodyNodeId = patch.loopBodyNodeId ?? '';
		node.onError = patch.onError ?? 'Stop';
	}
	if (type === WORKFLOW_NODE_TYPE.Delay) {
		node.delaySeconds = patch.delaySeconds ?? 1;
	}
	if (type === WORKFLOW_NODE_TYPE.SubWorkflow) {
		node.subWorkflowId = patch.subWorkflowId ?? 0;
		node.subWorkflowInputTemplate = patch.subWorkflowInputTemplate ?? '';
		node.timeoutSeconds = patch.timeoutSeconds ?? 0;
		node.onError = patch.onError ?? 'Stop';
	}
	return node;
};

/** 新建工作流的初始结构：开始 → 智能体 → 结束 */
export const createDefaultDefinition = (): WorkflowDefinitionModel => {
	const start = createWorkflowNode(WORKFLOW_NODE_TYPE.Start, { name: '开始', x: 200, y: 240 });
	const agent = createWorkflowNode(WORKFLOW_NODE_TYPE.Agent, { name: '智能体处理', x: 490, y: 240 });
	const end = createWorkflowNode(WORKFLOW_NODE_TYPE.End, { name: '结束', x: 800, y: 240 });
	return {
		startNodeId: start.id,
		nodes: [start, agent, end],
		edges: [
			{ from: start.id, to: agent.id },
			{ from: agent.id, to: end.id },
		],
	};
};

/** 分支条件的中文描述，用于画布线标签与属性面板回显 */
export const describeBranch = (branch: WorkflowConditionBranch): string => {
	const operator = WORKFLOW_BRANCH_OPERATORS.find((item) => item.value === branch.when);
	if (!branch.when) return '默认分支';
	if (!isValueRequiredOperator(branch.when)) return operator?.label ?? branch.when;
	return `${operator?.label ?? branch.when}「${branch.value || '空值'}」`;
};
