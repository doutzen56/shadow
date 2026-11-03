<template>
	<div class="wf-props-panel">
		<!-- 未选中节点：说明 + 节点图例，避免右侧大片空白 -->
		<div v-if="!node" class="wf-props-empty">
			<div class="wf-props-empty__icon"><i class="ri-cursor-line" /></div>
			<div class="wf-props-empty__title">未选中节点</div>
			<div class="wf-props-empty__desc">点击画布中的节点即可编辑配置；从左侧节点面板拖入新节点。</div>
			<div class="wf-legend">
				<div v-for="meta in WORKFLOW_NODE_META_LIST" :key="meta.lfType" class="wf-legend__item">
					<span class="wf-legend__dot" :style="{ backgroundColor: meta.accent }"></span>
					<span class="wf-legend__name">{{ meta.label }}</span>
					<span class="wf-legend__desc">{{ meta.desc }}</span>
				</div>
			</div>
		</div>

		<template v-else>
			<div class="wf-props-head">
				<div class="wf-props-head__main">
					<span class="wf-props-head__icon" :style="{ color: meta.accent, backgroundColor: `${meta.accent}1f` }">
						<i :class="meta.icon" />
					</span>
					<div class="wf-props-head__text">
						<div class="wf-props-head__title">{{ form.name || meta.defaultName }}</div>
						<div class="wf-props-head__type">{{ meta.label }}<span class="wf-props-head__id" @click="copyNodeId">#{{ node.id }}</span></div>
					</div>
				</div>
				<el-button icon="ele-Delete" text type="danger" size="small" @click="emit('delete')">删除节点</el-button>
			</div>

			<el-scrollbar class="wf-props-scroll">
				<el-form label-position="top" size="small" class="wf-form" @submit.prevent>
					<el-form-item label="节点名称">
						<el-input v-model="form.name" maxlength="60" placeholder="流程内建议唯一" @input="emitChange({ name: form.name })" />
					</el-form-item>

					<!-- 智能体节点 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Agent">
						<el-form-item label="绑定智能体" required>
							<el-select v-model="form.agentId" filterable placeholder="选择要调用的智能体" class="w-full" @change="emitChange({ agentId: Number(form.agentId ?? 0) })">
								<el-option v-for="agent in agents" :key="agent.id" :label="agent.name" :value="agent.id" />
							</el-select>
						</el-form-item>

						<el-form-item label="输入模板">
							<div class="wf-token-bar">
								<el-button v-for="token in templateTokens" :key="token.value" text size="small" @click="appendToken('inputTemplate', token.value)">{{ token.label }}</el-button>
							</div>
							<el-input
								v-model="form.inputTemplate"
								type="textarea"
								:rows="4"
								resize="vertical"
								placeholder="留空则使用运行输入。支持 {{input}} / {{var:key}} / {{step:节点名}}"
								@input="emitChange({ inputTemplate: form.inputTemplate })"
							/>
							<!-- 占位符用 HTML 实体写出，避免 Vue 把 {{ }} 当成插值解析（嵌套花括号会截断表达式） -->
							<div class="wf-field-tip">变量在运行时渲染；引用其它节点输出请使用 <code>&#123;&#123;step:节点名&#125;&#125;</code>。</div>
						</el-form-item>

						<el-form-item label="追加系统提示词">
							<el-input
								v-model="form.systemPromptAppend"
								type="textarea"
								:rows="3"
								resize="vertical"
								placeholder="可空。追加到智能体自身提示词末尾，仅对本节点生效"
								@input="emitChange({ systemPromptAppend: form.systemPromptAppend })"
							/>
						</el-form-item>
					</template>

					<!-- 知识库检索 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Knowledge">
						<el-form-item label="知识库" required>
							<el-select v-model="form.kmsId" filterable placeholder="选择要检索的知识库" class="w-full" @change="emitChange({ kmsId: Number(form.kmsId ?? 0) })">
								<el-option v-for="item in kmsList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>

						<el-form-item label="检索词模板" required>
							<div class="wf-token-bar">
								<el-button v-for="token in templateTokens" :key="token.value" text size="small" @click="appendToken('queryTemplate', token.value)">{{ token.label }}</el-button>
							</div>
							<el-input
								v-model="form.queryTemplate"
								type="textarea"
								:rows="3"
								resize="vertical"
								placeholder="留空则使用上一节点输出。支持 {{input}} / {{var:key}} / {{step:节点名}}"
								@input="emitChange({ queryTemplate: form.queryTemplate })"
							/>
						</el-form-item>

						<div class="wf-form-row">
							<el-form-item label="召回条数">
								<el-input-number
									v-model="form.topN"
									:min="1"
									:max="50"
									controls-position="right"
									class="w-full"
									@change="emitChange({ topN: Number(form.topN ?? 5) })"
								/>
							</el-form-item>
							<el-form-item label="相似度阈值">
								<el-input-number
									v-model="form.scoreThreshold"
									:min="0"
									:max="1"
									:step="0.05"
									:precision="2"
									controls-position="right"
									class="w-full"
									@change="emitChange({ scoreThreshold: Number(form.scoreThreshold ?? 0) })"
								/>
							</el-form-item>
						</div>
						<div class="wf-field-tip">阈值为 0 表示不过滤；节点输出是召回片段拼接后的文本，可直接被后续节点引用。</div>
					</template>

					<!-- 工具调用 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Tool">
						<el-form-item label="工具 / MCP 服务" required>
							<el-select v-model="form.skillId" filterable placeholder="选择已启用的工具或 MCP 服务" class="w-full" @change="onSkillChange">
								<el-option-group v-for="group in skillToolGroups" :key="group.label" :label="group.label">
									<el-option v-for="item in group.options" :key="item.id" :label="item.name" :value="item.id" />
								</el-option-group>
							</el-select>
						</el-form-item>

						<el-form-item label="工具函数" required>
							<el-select
								v-model="form.toolName"
								filterable
								allow-create
								default-first-option
								placeholder="选择要调用的函数名"
								class="w-full"
								@change="emitChange({ toolName: form.toolName })"
							>
								<el-option v-for="name in toolFunctionOptions" :key="name" :label="name" :value="name" />
							</el-select>
							<div class="wf-field-tip">普通工具取方法名末段；MCP 服务取勾选启用的工具清单。</div>
						</el-form-item>

						<el-form-item label="调用参数（JSON 模板）">
							<el-input
								v-model="form.argumentsTemplate"
								type="textarea"
								:rows="4"
								resize="vertical"
								placeholder='可空。例如 {"query": "{{input}}"}'
								@input="emitChange({ argumentsTemplate: form.argumentsTemplate })"
							/>
							<div class="wf-field-tip">参数值支持 <code>&#123;&#123;input&#125;&#125;</code> / <code>&#123;&#123;var:key&#125;&#125;</code> / <code>&#123;&#123;step:节点名&#125;&#125;</code>；渲染后必须是 JSON 对象。</div>
						</el-form-item>
					</template>

					<!-- HTTP 请求 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Http">
						<el-form-item label="请求方法">
							<el-radio-group v-model="form.httpMethod" @change="emitChange({ httpMethod: form.httpMethod })">
								<el-radio-button v-for="method in HTTP_METHOD_OPTIONS" :key="method" :value="method">{{ method }}</el-radio-button>
							</el-radio-group>
						</el-form-item>

						<el-form-item label="请求地址" required>
							<el-input v-model="form.httpUrl" placeholder="https://api.example.com/v1/items" @input="emitChange({ httpUrl: form.httpUrl })" />
							<div class="wf-field-tip">支持模板占位符；目标域名需在智能体的授权域名列表内，否则请求会被拦截。</div>
						</el-form-item>

						<el-form-item label="请求头（JSON 模板）">
							<el-input
								v-model="form.httpHeaders"
								type="textarea"
								:rows="3"
								resize="vertical"
								placeholder='可空。例如 {"Authorization": "Bearer {{var:token}}"}'
								@input="emitChange({ httpHeaders: form.httpHeaders })"
							/>
						</el-form-item>

						<template v-if="httpHasBody">
							<el-form-item label="请求体（模板）">
								<el-input
									v-model="form.httpBody"
									type="textarea"
									:rows="4"
									resize="vertical"
									placeholder='例如 {"q": "{{input}}"}'
									@input="emitChange({ httpBody: form.httpBody })"
								/>
							</el-form-item>
							<el-form-item label="内容类型">
								<el-input v-model="form.httpContentType" placeholder="application/json; charset=utf-8" @input="emitChange({ httpContentType: form.httpContentType })" />
							</el-form-item>
						</template>

						<el-form-item label="响应处理">
							<el-switch
								v-model="form.httpFailOnError"
								active-text="非 2xx 视为失败"
								inactive-text="记录状态码后继续"
								@change="emitChange({ httpFailOnError: form.httpFailOnError })"
							/>
						</el-form-item>
						<div class="wf-field-tip">节点输出为响应体文本，超过 5 万字符会被截断。</div>
					</template>

					<!-- 变量赋值 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Variable">
						<el-form-item label="变量赋值（变量名 → 模板值）" required>
							<div class="wf-assign-list">
								<div v-for="(item, index) in form.assignments" :key="index" class="wf-assign">
									<el-input v-model="item.name" placeholder="变量名" class="wf-assign__name" @input="updateAssignments" />
									<span class="wf-assign__eq">=</span>
									<el-input v-model="item.valueTemplate" placeholder="模板值，可留空" class="wf-assign__value" @input="updateAssignments" />
									<el-button icon="ele-Delete" text type="danger" size="small" :disabled="form.assignments.length <= 1" @click="removeAssignment(index)" />
								</div>
							</div>
							<el-button icon="ele-Plus" size="small" class="wf-add-branch" @click="addAssignment">添加变量</el-button>
							<div class="wf-field-tip">
								后续节点用 <code>&#123;&#123;var:变量名&#125;&#125;</code> 引用；变量名支持字母、数字、下划线、中文与短横线。
							</div>
						</el-form-item>
					</template>

					<!-- 输出 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Output">
						<el-form-item label="输出模板">
							<el-input
								v-model="form.outputTemplate"
								type="textarea"
								:rows="4"
								resize="vertical"
								placeholder="留空则透传上一节点输出。支持 {{input}} / {{var:key}} / {{step:节点名}}"
								@input="emitChange({ outputTemplate: form.outputTemplate })"
							/>
							<div class="wf-field-tip">输出会作为后续节点的默认取值，流程继续向下执行（不会结束流程）。</div>
						</el-form-item>
					</template>

					<!-- 循环迭代 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Loop">
						<el-form-item label="迭代方式">
							<el-radio-group v-model="form.loopMode" @change="emitChange({ loopMode: form.loopMode })">
								<el-radio-button value="Items">按列表</el-radio-button>
								<el-radio-button value="Count">固定次数</el-radio-button>
							</el-radio-group>
						</el-form-item>

						<el-form-item v-if="form.loopMode === 'Count'" label="循环次数">
							<el-input-number
								v-model="form.loopCount"
								:min="1"
								:max="100"
								controls-position="right"
								class="w-full"
								@change="emitChange({ loopCount: Number(form.loopCount ?? 1) })"
							/>
						</el-form-item>

						<template v-else>
							<el-form-item label="迭代数据模板" required>
								<el-input
									v-model="form.loopItemsTemplate"
									type="textarea"
									:rows="3"
									resize="vertical"
									placeholder="JSON 数组，或按分隔符切分的文本"
									@input="emitChange({ loopItemsTemplate: form.loopItemsTemplate })"
								/>
							</el-form-item>
							<el-form-item label="文本分隔符">
								<el-input v-model="loopSeparatorInput" :placeholder="separatorPlaceholder" />
								<div class="wf-field-tip">数据模板不是 JSON 数组时按此分隔符切分；支持 <code>{{ separatorEscapeHint }}</code>、<code>{{ tabEscapeHint }}</code> 写法。</div>
							</el-form-item>
						</template>

						<el-form-item label="最大迭代次数">
							<el-input-number
								v-model="form.loopMaxIterations"
								:min="1"
								:max="100"
								controls-position="right"
								class="w-full"
								@change="emitChange({ loopMaxIterations: Number(form.loopMaxIterations ?? 20) })"
							/>
							<div class="wf-field-tip">超出部分不再执行，防止循环体意外无限放大。</div>
						</el-form-item>

						<el-form-item label="循环体入口" required>
							<el-select
								v-model="form.loopBodyNodeId"
								filterable
								clearable
								placeholder="选择循环内第一个节点"
								class="w-full"
								@change="emitChange({ loopBodyNodeId: form.loopBodyNodeId ?? '' })"
							>
								<el-option v-for="option in loopBodyOptions" :key="option.id" :label="option.name" :value="option.id" />
							</el-select>
							<div class="wf-field-tip">
								循环体内按连线执行，遇到结束节点即完成本次迭代；当次序号与元素用 <code>&#123;&#123;var:index&#125;&#125;</code> /
								<code>&#123;&#123;var:item&#125;&#125;</code> 引用。
							</div>
						</el-form-item>
					</template>

					<!-- 延迟 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Delay">
						<el-form-item label="等待秒数">
							<el-input-number
								v-model="form.delaySeconds"
								:min="0"
								:max="3600"
								controls-position="right"
								class="w-full"
								@change="emitChange({ delaySeconds: Number(form.delaySeconds ?? 0) })"
							/>
							<div class="wf-field-tip">最长 3600 秒；常用于限流、轮询间隔与定时编排的节奏控制。</div>
						</el-form-item>
					</template>

					<!-- 子流程 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.SubWorkflow">
						<el-form-item label="子工作流" required>
							<el-select
								v-model="form.subWorkflowId"
								filterable
								placeholder="选择要调用的工作流"
								class="w-full"
								@change="emitChange({ subWorkflowId: Number(form.subWorkflowId ?? 0) })"
							>
								<el-option v-for="item in availableWorkflows" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
							<div class="wf-field-tip">子流程同步执行，取它的最终输出作为本节点结果；不继承当前流程的变量。</div>
						</el-form-item>

						<el-form-item label="输入模板">
							<el-input
								v-model="form.subWorkflowInputTemplate"
								type="textarea"
								:rows="3"
								resize="vertical"
								placeholder="留空则传入上一节点输出"
								@input="emitChange({ subWorkflowInputTemplate: form.subWorkflowInputTemplate })"
							/>
						</el-form-item>
					</template>

					<!-- 超时与失败策略：仅对真正走节点级超时的类型展示 -->
					<div v-if="supportsTimeout" class="wf-form-row">
						<el-form-item label="单节点超时（秒）">
							<el-input-number
								v-model="form.timeoutSeconds"
								:min="0"
								:max="86400"
								controls-position="right"
								class="w-full"
								@change="emitChange({ timeoutSeconds: Number(form.timeoutSeconds ?? 0) })"
							/>
						</el-form-item>
						<el-form-item label="失败策略">
							<el-select v-model="form.onError" class="w-full" @change="emitChange({ onError: form.onError })">
								<el-option label="终止流程（默认）" value="Stop" />
								<el-option label="记录失败后继续" value="Continue" />
							</el-select>
						</el-form-item>
					</div>
					<el-form-item v-else-if="supportsOnError" label="失败策略">
						<el-select v-model="form.onError" class="w-full" @change="emitChange({ onError: form.onError })">
							<el-option label="终止流程（默认）" value="Stop" />
							<el-option label="记录失败后继续" value="Continue" />
						</el-select>
					</el-form-item>
					<div v-if="supportsTimeout" class="wf-field-tip">超时填 0 表示沿用运行级超时配置。</div>

					<!-- 条件分支 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Condition">
						<el-form-item label="分支条件（按顺序求值，命中即路由）" required>
							<div class="wf-branch-list">
								<div v-for="(branch, index) in form.branches" :key="index" class="wf-branch">
									<div class="wf-branch__head">
										<span class="wf-branch__index">分支 {{ index + 1 }}</span>
										<span v-if="!branch.when" class="wf-branch__badge">默认</span>
										<div class="wf-branch__ops">
											<el-button icon="ele-Top" text size="small" :disabled="index === 0" @click="moveBranch(index, -1)" />
											<el-button icon="ele-Bottom" text size="small" :disabled="index === form.branches.length - 1" @click="moveBranch(index, 1)" />
											<el-button icon="ele-Close" text size="small" type="danger" :disabled="form.branches.length <= 1" @click="removeBranch(index)" />
										</div>
									</div>
									<el-select v-model="branch.when" class="w-full" @change="updateBranches">
										<el-option v-for="operator in WORKFLOW_BRANCH_OPERATORS" :key="operator.value" :label="operator.label" :value="operator.value" />
									</el-select>
									<el-input
										v-if="isValueRequiredOperator(branch.when)"
										v-model="branch.value"
										class="w-full"
										:placeholder="'期望值，支持 {{var:key}} / {{step:节点名}}'"
										@input="updateBranches"
									/>
									<el-select v-model="branch.next" class="w-full" placeholder="命中后前往的节点" filterable @change="updateBranches">
										<el-option v-for="option in branchTargetOptions" :key="option.id" :label="option.name" :value="option.id" />
									</el-select>
								</div>
							</div>
							<el-button icon="ele-Plus" size="small" class="wf-add-branch" @click="addBranch">添加分支</el-button>
							<div class="wf-field-tip">必须保留一个「默认（恒命中）」分支兜底；比较对象是上一节点的输出。</div>
						</el-form-item>
					</template>

					<!-- 并行网关 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Parallel">
						<el-form-item label="并发分支入口（至少两个）" required>
							<el-select v-model="form.parallelNodeIds" multiple filterable collapse-tags class="w-full" placeholder="选择每条分支的入口节点" @change="emitChange({ parallelNodeIds: [...form.parallelNodeIds] })">
								<el-option v-for="option in parallelTargetOptions" :key="option.id" :label="option.name" :value="option.id" />
							</el-select>
							<div class="wf-field-tip">分支内按连线依次执行，到达结束节点即完成该分支。</div>
						</el-form-item>
						<el-form-item label="汇合模式">
							<el-radio-group v-model="form.joinMode" @change="emitChange({ joinMode: form.joinMode })">
								<el-radio-button value="All">全部成功</el-radio-button>
								<el-radio-button value="Any">任一成功</el-radio-button>
							</el-radio-group>
						</el-form-item>
					</template>

					<!-- 人工审批 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.Approval">
						<el-form-item label="审批超时（分钟）">
							<el-input-number
								v-model="form.approvalTimeoutMinutes"
								:min="0"
								:max="43200"
								controls-position="right"
								class="w-full"
								@change="emitChange({ approvalTimeoutMinutes: Number(form.approvalTimeoutMinutes ?? 0) })"
							/>
							<div class="wf-field-tip">0 表示不限制；超时在审批动作触发时判定。</div>
						</el-form-item>
						<el-form-item label="超时处理">
							<el-select v-model="form.approvalTimeoutAction" class="w-full" @change="emitChange({ approvalTimeoutAction: form.approvalTimeoutAction })">
								<el-option label="按拒绝处理（流程取消）" value="Reject" />
								<el-option label="标记运行失败" value="Fail" />
							</el-select>
						</el-form-item>
					</template>

					<!-- 结束节点：自定义最终输出 -->
					<template v-if="node.type === WORKFLOW_NODE_TYPE.End">
						<el-form-item label="最终输出模板">
							<el-input
								v-model="form.inputTemplate"
								type="textarea"
								:rows="3"
								resize="vertical"
								placeholder="留空则输出最后一个智能体节点的结果。支持 {{step:节点名}} / {{var:key}}"
								@input="emitChange({ inputTemplate: form.inputTemplate })"
							/>
						</el-form-item>
					</template>

					<!-- 路由：条件节点由分支决定，结束节点没有后续 -->
					<el-form-item v-if="supportsNext" :label="nextLabel">
						<el-select v-model="form.nextNodeId" clearable filterable placeholder="选择后续节点" class="w-full" @change="emit('set-next', form.nextNodeId)">
							<el-option v-for="option in nextOptions" :key="option.id" :label="option.name" :value="option.id" />
						</el-select>
						<div class="wf-field-tip">也可以直接在画布上从节点右侧锚点拖线连接。</div>
					</el-form-item>
				</el-form>
			</el-scrollbar>
		</template>
	</div>
</template>

<script lang="ts" setup name="workflowNodePropertyPanel">
import { computed, reactive, watch } from 'vue';
import { ElMessage } from 'element-plus';
import {
	WORKFLOW_BRANCH_OPERATORS,
	WORKFLOW_NODE_META_LIST,
	WORKFLOW_NODE_TYPE,
	WorkflowConditionBranch,
	WorkflowNodeModel,
	WorkflowNodeType,
	WorkflowVariableAssignment,
	getNodeMeta,
	isValueRequiredOperator,
} from '../types';

/** 技能工具下拉项（只取工作流工具节点需要的字段） */
interface WorkflowSkillToolOption {
	id: number;
	name: string;
	skillToolType?: number;
	classMethod?: string | null;
	mcpTools?: string | null;
	mcpSelectedTools?: string | null;
}

const props = defineProps<{
	node: WorkflowNodeModel | null;
	revision: number;
	agents: { id: number; name: string }[];
	nodes: WorkflowNodeModel[];
	nextNodeId: string;
	kmsList: { id: number; name: string }[];
	skillTools: WorkflowSkillToolOption[];
	workflows: { id: number; name: string }[];
	/** 当前正在编辑的工作流 Id：子流程节点不能调用自己 */
	workflowId?: number;
}>();

const emit = defineEmits<{
	(e: 'change', patch: Record<string, any>): void;
	(e: 'set-next', targetId: string): void;
	(e: 'delete'): void;
}>();

/** 与后端 AISkillToolTypeEnum 对齐：只有工具与 MCP 能被工作流直接调用 */
const SKILL_TOOL_TYPE = { Tool: 1, Mcp: 3 } as const;
const HTTP_METHOD_OPTIONS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];
/** DELETE 在 HTTP 语义里一般不携带请求体 */
const HTTP_BODY_METHODS = ['POST', 'PUT', 'PATCH'];

const form = reactive({
	name: '',
	agentId: 0 as number | undefined,
	inputTemplate: '',
	systemPromptAppend: '',
	timeoutSeconds: 0,
	onError: 'Stop',
	branches: [] as WorkflowConditionBranch[],
	parallelNodeIds: [] as string[],
	joinMode: 'All',
	approvalTimeoutMinutes: 0,
	approvalTimeoutAction: 'Reject',
	nextNodeId: '',
	kmsId: 0 as number | undefined,
	queryTemplate: '',
	topN: 5,
	scoreThreshold: 0,
	skillId: 0 as number | undefined,
	toolName: '',
	argumentsTemplate: '',
	httpUrl: '',
	httpMethod: 'GET',
	httpHeaders: '',
	httpBody: '',
	httpContentType: 'application/json; charset=utf-8',
	httpFailOnError: true,
	assignments: [] as WorkflowVariableAssignment[],
	outputTemplate: '',
	loopMode: 'Items',
	loopItemsTemplate: '',
	loopSeparator: '\n',
	loopCount: 1,
	loopMaxIterations: 20,
	loopBodyNodeId: '' as string | undefined,
	delaySeconds: 1,
	subWorkflowId: 0 as number | undefined,
	subWorkflowInputTemplate: '',
});

const meta = computed(() => getNodeMeta((props.node?.type ?? WORKFLOW_NODE_TYPE.Agent) as WorkflowNodeType));

const syncForm = () => {
	const node = props.node;
	if (!node) return;
	form.name = node.name;
	form.agentId = node.agentId || undefined;
	form.inputTemplate = node.inputTemplate ?? '';
	form.systemPromptAppend = node.systemPromptAppend ?? '';
	form.timeoutSeconds = node.timeoutSeconds ?? 0;
	form.onError = node.onError ?? 'Stop';
	form.branches = (node.branches ?? []).map((branch) => ({ ...branch }));
	form.parallelNodeIds = [...(node.parallelNodeIds ?? [])];
	form.joinMode = node.joinMode ?? 'All';
	form.approvalTimeoutMinutes = node.approvalTimeoutMinutes ?? 0;
	form.approvalTimeoutAction = node.approvalTimeoutAction ?? 'Reject';
	form.nextNodeId = props.nextNodeId;
	form.kmsId = node.kmsId || undefined;
	form.queryTemplate = node.queryTemplate ?? '';
	form.topN = node.topN ?? 5;
	form.scoreThreshold = node.scoreThreshold ?? 0;
	form.skillId = node.skillId || undefined;
	form.toolName = node.toolName ?? '';
	form.argumentsTemplate = node.argumentsTemplate ?? '';
	form.httpUrl = node.httpUrl ?? '';
	form.httpMethod = node.httpMethod ?? 'GET';
	form.httpHeaders = node.httpHeaders ?? '';
	form.httpBody = node.httpBody ?? '';
	form.httpContentType = node.httpContentType ?? 'application/json; charset=utf-8';
	form.httpFailOnError = node.httpFailOnError ?? true;
	form.assignments = (node.assignments ?? []).map((assignment) => ({ ...assignment }));
	form.outputTemplate = node.outputTemplate ?? '';
	form.loopMode = node.loopMode ?? 'Items';
	form.loopItemsTemplate = node.loopItemsTemplate ?? '';
	form.loopSeparator = node.loopSeparator ?? '\n';
	form.loopCount = node.loopCount ?? 1;
	form.loopMaxIterations = node.loopMaxIterations ?? 20;
	form.loopBodyNodeId = node.loopBodyNodeId ?? '';
	form.delaySeconds = node.delaySeconds ?? 1;
	form.subWorkflowId = node.subWorkflowId || undefined;
	form.subWorkflowInputTemplate = node.subWorkflowInputTemplate ?? '';
};

watch(() => [props.node?.id, props.revision], syncForm, { immediate: true });
watch(
	() => props.nextNodeId,
	(value) => {
		if (value !== form.nextNodeId) form.nextNodeId = value;
	}
);

const emitChange = (patch: Record<string, any>) => emit('change', patch);

const templateTokens = [
	{ label: '{{input}}', value: '{{input}}' },
	{ label: '{{var:key}}', value: '{{var:key}}' },
	{ label: '{{step:节点名}}', value: '{{step:节点名}}' },
];

/** 模板占位符按字段追加：不同节点的模板字段不同，不能共用一份状态 */
const appendToken = (field: 'inputTemplate' | 'queryTemplate', token: string) => {
	form[field] = `${form[field] ?? ''}${token}`;
	emitChange({ [field]: form[field] });
};

/** 技能工具分组：普通工具与 MCP 服务混在一起难以区分 */
const skillToolGroups = computed(() => {
	const groups = [
		{ label: '工具', type: SKILL_TOOL_TYPE.Tool, options: [] as WorkflowSkillToolOption[] },
		{ label: 'MCP 服务', type: SKILL_TOOL_TYPE.Mcp, options: [] as WorkflowSkillToolOption[] },
	];
	props.skillTools.forEach((item) => {
		groups.find((group) => group.type === Number(item.skillToolType))?.options.push(item);
	});
	return groups.filter((group) => group.options.length);
});

const selectedSkill = computed(() => props.skillTools.find((item) => item.id === form.skillId));

/** MCP 工具名来自测试连接时的 JSON 数组；勾选清单优先，为空时回落到全部工具 */
const parseToolNames = (json?: string | null): string[] => {
	if (!json) return [];
	try {
		const value = JSON.parse(json);
		return Array.isArray(value) ? value.map((item) => String(item)).filter(Boolean) : [];
	} catch {
		return [];
	}
};

const toolFunctionOptions = computed<string[]>(() => {
	const skill = selectedSkill.value;
	if (!skill) return [];
	if (Number(skill.skillToolType) === SKILL_TOOL_TYPE.Mcp) {
		const selected = parseToolNames(skill.mcpSelectedTools);
		return selected.length ? selected : parseToolNames(skill.mcpTools);
	}
	const method = String(skill.classMethod ?? '').trim();
	if (!method) return [];
	return [method.includes('.') ? method.slice(method.lastIndexOf('.') + 1) : method];
});

/** 切换工具后清空不再有效的函数名，避免残留一个按名字匹配不到的工具 */
const onSkillChange = () => {
	emitChange({ skillId: Number(form.skillId ?? 0) });
	if (form.toolName && !toolFunctionOptions.value.includes(form.toolName)) {
		form.toolName = toolFunctionOptions.value[0] ?? '';
		emitChange({ toolName: form.toolName });
	}
};

const httpHasBody = computed(() => HTTP_BODY_METHODS.includes(String(form.httpMethod ?? '').toUpperCase()));

/** 单行输入里换行符不可见：编辑时显示为 \n 字面量，落库时再还原 */
const escapeSeparator = (value: string) => value.replace(/\n/g, '\\n').replace(/\t/g, '\\t');
const unescapeSeparator = (value: string) => value.replace(/\\n/g, '\n').replace(/\\t/g, '\t');

const loopSeparatorInput = computed({
	get: () => escapeSeparator(form.loopSeparator ?? ''),
	set: (value: string) => {
		form.loopSeparator = unescapeSeparator(value) || '\n';
		emitChange({ loopSeparator: form.loopSeparator });
	},
});

// 模板里的静态文本会被 i18n 转译层解转义，反斜杠字面量只能从脚本里给
const separatorEscapeHint = String.raw`\n`;
const tabEscapeHint = String.raw`\t`;
const separatorPlaceholder = `${separatorEscapeHint}（默认换行）`;

const supportsTimeout = computed(() => {
	const type = props.node?.type;
	return (
		type === WORKFLOW_NODE_TYPE.Agent ||
		type === WORKFLOW_NODE_TYPE.Knowledge ||
		type === WORKFLOW_NODE_TYPE.Tool ||
		type === WORKFLOW_NODE_TYPE.Http ||
		type === WORKFLOW_NODE_TYPE.SubWorkflow
	);
});

const supportsOnError = computed(() => supportsTimeout.value || props.node?.type === WORKFLOW_NODE_TYPE.Loop);

/** 分支目标：不能指向自身；同一个节点可被多个分支命中（由求值顺序决定） */
const branchTargetOptions = computed(() => props.nodes.filter((item) => item.id !== props.node?.id));

/** 并行分支入口：开始/人工审批节点由执行器直接拒绝，其余节点都可以 */
const parallelTargetOptions = computed(() =>
	props.nodes.filter(
		(item) => item.id !== props.node?.id && item.type !== WORKFLOW_NODE_TYPE.Start && item.type !== WORKFLOW_NODE_TYPE.Approval
	)
);

/** 循环体入口：同上，且不能指向自身 */
const loopBodyOptions = computed(() => parallelTargetOptions.value);

/** 子流程不能调用自己 */
const availableWorkflows = computed(() => props.workflows.filter((item) => item.id !== props.workflowId));

const nextOptions = computed(() => props.nodes.filter((item) => item.id !== props.node?.id));

const supportsNext = computed(() => {
	const type = props.node?.type;
	return type !== WORKFLOW_NODE_TYPE.Condition && type !== WORKFLOW_NODE_TYPE.End && !!type;
});

const nextLabel = computed(() => {
	const type = props.node?.type;
	if (type === WORKFLOW_NODE_TYPE.Parallel) return '汇合后继续';
	if (type === WORKFLOW_NODE_TYPE.Loop) return '循环结束后继续';
	return '下一步';
});

const updateBranches = () => emitChange({ branches: form.branches.map((branch) => ({ ...branch })) });

const addBranch = () => {
	form.branches.push({ when: '', value: '', next: '' });
	updateBranches();
};

const removeBranch = (index: number) => {
	if (form.branches.length <= 1) return;
	form.branches.splice(index, 1);
	updateBranches();
};

const moveBranch = (index: number, offset: number) => {
	const target = index + offset;
	if (target < 0 || target >= form.branches.length) return;
	const [branch] = form.branches.splice(index, 1);
	form.branches.splice(target, 0, branch);
	updateBranches();
};

const updateAssignments = () => emitChange({ assignments: form.assignments.map((assignment) => ({ ...assignment })) });

const addAssignment = () => {
	form.assignments.push({ name: '', valueTemplate: '' });
	updateAssignments();
};

const removeAssignment = (index: number) => {
	if (form.assignments.length <= 1) return;
	form.assignments.splice(index, 1);
	updateAssignments();
};

const copyNodeId = async () => {
	if (!props.node) return;
	try {
		await navigator.clipboard.writeText(props.node.id);
		ElMessage.success('节点 Id 已复制');
	} catch {
		ElMessage.warning('浏览器不允许访问剪贴板');
	}
};
</script>

<style scoped lang="scss">
.wf-props-panel {
	display: flex;
	flex-direction: column;
	height: 100%;
	min-height: 0;
}

.w-full {
	width: 100%;
}

.wf-props-head {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 8px;
	// 与左侧节点面板、画布工具条统一 12px 边距，左右两栏的起始基线才一致
	padding: 14px 12px 10px;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&__main {
		display: flex;
		align-items: center;
		gap: 9px;
		min-width: 0;
	}

	&__icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 30px;
		height: 30px;
		border-radius: 9px;
		font-size: 16px;
		flex-shrink: 0;
	}

	&__text {
		min-width: 0;
	}

	&__title {
		font-size: 13.5px;
		font-weight: 600;
		line-height: 19px;
		color: var(--el-text-color-primary);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	&__type {
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 11.5px;
		color: var(--el-text-color-secondary);
	}

	&__id {
		font-family: monospace;
		font-size: 11px;
		cursor: pointer;
		color: var(--el-text-color-placeholder);

		&:hover {
			color: var(--el-color-primary);
		}
	}
}

.wf-props-scroll {
	flex: 1;
	min-height: 0;
}

.wf-form {
	padding: 12px 12px 20px;

	:deep(.el-form-item) {
		margin-bottom: 14px;
	}

	:deep(.el-form-item__label) {
		padding-bottom: 4px;
		font-size: 12px;
		color: var(--el-text-color-regular);
	}
}

.wf-form-row {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 10px;
}

.wf-field-tip {
	margin-top: 4px;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);

	code {
		padding: 0 3px;
		font-family: monospace;
		font-size: 11px;
		border-radius: 3px;
		color: var(--el-color-primary);
		background-color: var(--el-color-primary-light-9);
	}
}

.wf-token-bar {
	display: flex;
	flex-wrap: wrap;
	gap: 2px;
	margin-bottom: 4px;

	.el-button {
		padding: 0 6px;
		font-family: monospace;
		font-size: 11px;
	}
}

.wf-branch-list {
	display: flex;
	flex-direction: column;
	gap: 8px;
	width: 100%;
}

.wf-branch {
	padding: 10px;
	border-radius: 10px;
	background-color: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);

	&__head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 8px;
	}

	&__index {
		font-size: 12px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	&__badge {
		margin-left: 6px;
		padding: 0 6px;
		font-size: 11px;
		line-height: 18px;
		border-radius: 4px;
		color: var(--el-color-warning);
		background-color: var(--el-color-warning-light-9);
	}

	&__ops {
		display: flex;
		align-items: center;
		gap: 2px;
	}

	:deep(.el-select),
	:deep(.el-input) {
		margin-bottom: 6px;
	}
}

.wf-add-branch {
	margin-top: 8px;
}

.wf-assign-list {
	display: flex;
	flex-direction: column;
	gap: 6px;
	width: 100%;
}

.wf-assign {
	display: flex;
	align-items: center;
	gap: 6px;

	&__name {
		flex: 0 0 34%;
	}

	&__eq {
		flex-shrink: 0;
		font-size: 12px;
		color: var(--el-text-color-placeholder);
	}

	&__value {
		flex: 1;
		min-width: 0;
	}
}

.wf-props-empty {
	padding: 28px 20px;
	text-align: center;

	&__icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 52px;
		height: 52px;
		border-radius: 16px;
		font-size: 24px;
		color: var(--el-color-primary);
		background-color: var(--el-color-primary-light-9);
	}

	&__title {
		margin-top: 12px;
		font-size: 14px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	&__desc {
		margin-top: 6px;
		font-size: 12px;
		line-height: 19px;
		color: var(--el-text-color-secondary);
	}
}

.wf-legend {
	margin-top: 20px;
	padding-top: 14px;
	border-top: 1px dashed var(--el-border-color);
	display: flex;
	flex-direction: column;
	gap: 10px;
	text-align: left;

	&__item {
		display: grid;
		grid-template-columns: 8px 54px 1fr;
		align-items: center;
		gap: 8px;
	}

	&__dot {
		width: 8px;
		height: 8px;
		border-radius: 50%;
	}

	&__name {
		font-size: 12px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	&__desc {
		font-size: 12px;
		line-height: 17px;
		color: var(--el-text-color-secondary);
	}
}
</style>
