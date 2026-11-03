<template>
	<!-- 外层"办理任务"抽屉：受控显隐（visible 是 prop，用 :model-value + @update:model-value 而非 v-model，避免直接改 prop）；
	     body-class 直接挂在 .el-drawer__body 上还原 JfDrawer 行为；--fill 让内容铺满不滚动 -->
	<el-drawer body-class="jf-approve-drawer-body jf-approve-drawer-body--fill" :model-value="visible" :title="title" :size="width" @update:model-value="emit('update:visible', $event)">
		<!-- 加载中：拉取任务详情期间显示 loading -->
		<div v-if="loading" class="jf-loading">加载中...</div>
		<!-- 任务已加载：渲染办理内容（task 为 null 时落到最底部"任务不存在"分支） -->
		<template v-else-if="task">
			<!-- 三个页签：详情 / 流程图 / 审批记录 -->
			<el-tabs v-model="activeKey">
				<!-- 详情页签 -->
				<el-tab-pane label="详情" name="detail">
					<!-- 申请信息区：统一用 form-create 渲染（与详情抽屉对称），按 bizReadonly 决定只读 -->
					<!-- 申请信息不冗余才渲染：办理节点复用申请表单且无业务数据时，下方可编辑表单已覆盖，避免重复空表单 -->
					<template v-if="!applyAreaRedundant">
						<h3 class="jf-section-title">申请信息</h3>
						<!-- 申请表单：统一用 form-create 渲染，rule 按首任务节点 formKey 拉取、数据用 f_* 回显；
						     按 bizReadonly 决定只读（只读入口/非进行中/审批节点未启用字段权限 → 只读；首节点重新提交可编） -->
						<form-create v-if="applyDesignedRule.length" ref="bizFormRef" v-model="bizFormData" v-bind="applyFormProps" />
						<div v-else class="jf-muted jf-form-hint">该流程未配置申请表单</div>
						<!-- 发起时附加上下文（抄送/下一处理人/申请理由/附件）：申请维度，只读回显，
						     数据来自实例详情 d.ext（syncApplyExtras），变量与下面办理区可编辑的那组完全隔离 -->
						<JfInitiateExtras
							v-if="showApplyExtras"
							:graph="graph"
							:disabled="true"
							v-model:cc-actors="initCcActors"
							v-model:next-operators="initNextOperators"
							v-model:apply-reason="initApplyReason"
							v-model:attachment="initAttachment"
						/>
					</template>
					<!-- 审核信息：仅非首节点（审批人视角）显示；首节点重新提交时走"申请信息 + 重新提交"，此处无意义 -->
					<template v-if="!isFirstTaskNode">
						<h3 class="jf-section-title">审核信息</h3>
						<!-- 办理节点表单：统一用 form-create 渲染；rule 按当前任务 formKey 拉取，数据用 tf_* 回显；按 taskFormReadonly 决定只读 -->
						<form-create v-if="taskDesignedRule.length" ref="taskFormRef" v-model="formData" v-bind="taskFormProps" />
						<!-- 有 formKey 但未配置表单设计：给可读提示 -->
						<div v-else-if="taskFormKey" class="jf-muted jf-form-hint">未配置办理表单「{{ taskFormKey }}」</div>
					</template>

					<!-- 可操作态（非只读且任务进行中 DOING）：渲染审批操作区 -->
					<template v-if="!readonly && isDoing">
						<!-- 首任务节点：发起人视角，可重填申请信息并"重新提交" -->
						<template v-if="isFirstTaskNode">
							<!-- 发起附加上下文（抄送/下一处理人/申请理由/附件），首节点额外含申请理由与附件 -->
							<JfInitiateExtras :graph="graph" v-model:cc-actors="ccActors" v-model:next-operators="nextOperators" v-model:apply-reason="applyReason" v-model:attachment="attachment" />
							<div class="jf-approve-actions">
								<!-- 首节点唯一操作：重新提交 -->
								<el-button type="primary" :icon="Edit" :loading="submitting" @click="doReApply">重新提交</el-button>
							</div>
						</template>
						<!-- 非首节点（审批人视角）：渲染审批意见与审批操作 -->
						<template v-else>
							<div class="jf-form-item">
								<label class="jf-form-label">审批意见</label>
								<el-input v-model="comment" type="textarea" :rows="3" placeholder="请输入审批意见（可选）" />
							</div>
							<!-- 审批附加上下文（抄送/下一处理人/附件），无申请理由 -->
							<JfInitiateExtras :graph="graph" :task-id="taskId" v-model:cc-actors="ccActors" v-model:next-operators="nextOperators" v-model:attachment="attachment" />
							<div class="jf-approve-actions">
								<!-- 审批动作按钮（按流程配置显隐）：同意/拒绝/退回/会签拒绝/跳转/转办/加签 -->
								<el-button v-if="hasBtn('AGREE')" type="primary" :icon="Select" :disabled="submitting" @click="doExecute(SubmitType.AGREE)">同意</el-button>
								<el-button v-if="hasBtn('REJECT')" type="danger" :icon="CloseBold" :disabled="submitting" @click="doExecute(SubmitType.REJECT)">拒绝</el-button>
								<el-button v-if="hasBtn('ROLLBACK')" plain :icon="RefreshLeft" :disabled="submitting" @click="doExecute(SubmitType.ROLLBACK)">退回上一步</el-button>
								<el-button v-if="hasBtn('ROLLBACK_TO_OPERATOR')" plain :icon="Back" :disabled="submitting" @click="doExecute(SubmitType.ROLLBACK_TO_OPERATOR)">退回发起人</el-button>
								<el-button v-if="hasBtn('COUNTERSIGN_DISAGREE')" type="danger" :icon="CloseBold" :disabled="submitting" @click="doCountersignDisagree">会签拒绝</el-button>
								<el-button v-if="hasBtn('JUMP')" plain :icon="Promotion" :disabled="submitting" @click="openJump">跳转</el-button>
								<el-button plain :icon="Switch" :disabled="submitting" @click="surrogateVisible = true">转办</el-button>
								<el-button v-if="hasBtn('ADD_CANDIDATE')" plain :icon="Plus" :disabled="submitting" @click="addCandidateVisible = true">加签</el-button>
							</div>
						</template>
					</template>
					<!-- 只读/已办态：回显审批意见与本次办理填过的附加上下文（不能只留表单） -->
					<template v-else-if="comment || showDoneExtras">
						<div v-if="comment" class="jf-form-item">
							<label class="jf-form-label">审批意见</label>
							<div class="jf-detail-text">{{ comment }}</div>
						</div>
						<!-- 本次办理时填的附加上下文（抄送/下一处理人/附件）：审批维度，只读回显，与申请维度 init* 隔离 -->
						<JfInitiateExtras v-if="showDoneExtras" :graph="graph" :disabled="true" v-model:cc-actors="doneCcActors" v-model:next-operators="doneNextOperators" v-model:attachment="doneAttachment" />
					</template>
				</el-tab-pane>

				<!-- 流程图页签 -->
				<el-tab-pane label="流程图" name="flow">
					<!-- 有流程定义图：渲染流程图查看器 -->
					<div v-if="graph" class="jf-pane-flow">
						<JfFlowViewer :graph-data="graph" :high-light="highLight" :assignee-text-data="assigneeRows" height="100%" />
					</div>
					<!-- 无流程定义图：空态 -->
					<div v-else class="jf-empty">暂无流程图</div>
				</el-tab-pane>

				<!-- 审批记录页签 -->
				<el-tab-pane label="审批记录" name="record">
					<!-- 审批记录（时间线 + 表格） -->
					<JfApprovalRecord :records="records" />
				</el-tab-pane>
			</el-tabs>
		</template>
		<!-- 任务不存在（加载完成但 task 为 null）：空态 -->
		<div v-else class="jf-empty">任务不存在</div>

		<!-- 跳转节点选择抽屉（嵌套，append-to-body 还原原 Teleport 到 body 的行为） -->
		<el-drawer v-model="jumpVisible" append-to-body body-class="jf-approve-drawer-body" title="跳转到节点" size="420px">
			<!-- 加载中 -->
			<div v-if="jumpLoading" class="jf-loading">加载中...</div>
			<!-- 有可跳转节点：渲染节点列表 -->
			<div v-else-if="jumpNodes.length" class="jf-list">
				<!-- 每个可跳转节点一个按钮 -->
				<el-button v-for="n in jumpNodes" :key="n.value" class="jf-list-item" style="width: 100%; justify-content: flex-start" :icon="Promotion" @click="doJump(n.value)">{{ n.label }}</el-button>
			</div>
			<!-- 无可跳转节点：空态 -->
			<div v-else class="jf-empty">无可跳转节点</div>
		</el-drawer>

		<!-- 转办 / 加签 抽屉（嵌套，append-to-body） -->
		<el-drawer
			append-to-body
			body-class="jf-approve-drawer-body"
			:title="surrogateVisible ? '转办（指定处理人）' : '加签（追加参与人）'"
			:model-value="surrogateVisible || addCandidateVisible"
			size="480px"
			@update:model-value="
				surrogateVisible = $event;
				addCandidateVisible = $event;
			"
		>
			<div class="jf-actor-field">
				<label class="jf-form-label">选择用户</label>
				<!-- 用户选择器：scene 区分转办(surrogate) / 加签(candidate) -->
				<JfUserPicker v-model="actorIds" :task-id="addCandidateVisible ? taskId : null" :scene="addCandidateVisible ? 'candidate' : 'surrogate'" placeholder="搜索姓名/工号" />
			</div>
			<div class="jf-drawer-actions">
				<!-- 取消：同时关闭转办与加签两个抽屉 -->
				<el-button
					plain
					@click="
						surrogateVisible = false;
						addCandidateVisible = false;
					"
					>取消</el-button
				>
				<!-- 确定：执行转办或加签（未选用户或提交中禁用） -->
				<el-button type="primary" :icon="Select" :disabled="actorSaving || !actorIds.length" @click="doActors">确定</el-button>
			</div>
		</el-drawer>
	</el-drawer>
</template>

<script setup lang="ts">
// 依赖引入
import { ref, computed, watch } from 'vue';
import JfFlowViewer from '../ui/JfFlowViewer.vue';
import JfUserPicker from '../ui/JfUserPicker.vue';
import JfApprovalRecord from '../ui/JfApprovalRecord.vue';
import JfInitiateExtras from '../ui/JfInitiateExtras.vue';
import { SubmitType } from '../types';
import type { SysFile } from '/@/api-services/system/models/sys-file';
import type { TaskDetail, InstanceDetail, JumpableTaskRow, HighLightData, ApprovalRecordRow, AssigneeTextRow } from '../types';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowTaskApi, JeeflowInstanceApi, JeeflowFormDesignApi } from '/@/api-services/jeeFlow/api';
import { useUserInfo } from '/@/stores/userInfo';
// jeeflow 工具函数
import { findTaskNode, firstTaskNode, firstTaskFormKey, resolveActionBtns, extractBizFormData, extractTaskFormData, initiateExtraFlags, resolveAssigneeLabels, type ActionBtnKey } from '../helpers';
import { ElMessage } from 'element-plus';
// Element Plus 图标
import { Select, CloseBold, RefreshLeft, Back, Promotion, Switch, Plus, Edit } from '@element-plus/icons-vue';

defineOptions({ name: 'JfApproveDrawer' });

const userStore = useUserInfo(); // 当前登录用户（取手写体签名 signature）

// ── Props / Emits ──
// visible：抽屉显隐（受控）；taskId：当前任务 ID；width：抽屉宽度；readonly：只读（已办入口）
const props = withDefaults(
	defineProps<{
		visible: boolean;
		taskId: string | null;
		width?: string;
		readonly?: boolean;
	}>(),
	{ width: '60%', readonly: false }
);

// update:visible 通知父组件关闭；changed 通知父组件列表需刷新
const emit = defineEmits<{
	'update:visible': [v: boolean];
	changed: [];
}>();

// ── 响应式状态 ──
const loading = ref(false); // 任务详情加载态
const task = ref<TaskDetail | null>(null); // 当前任务详情
const highLight = ref<HighLightData | null>(null); // 流程图节点高亮
const records = ref<ApprovalRecordRow[]>([]); // 审批记录
const assigneeRows = ref<AssigneeTextRow[]>([]); // 各节点处理人文本
const formData = ref<Record<string, any>>({}); // 审批表单(tf_)数据
const bizFormData = ref<Record<string, any>>({}); // 业务表单(f_)数据
const comment = ref(''); // 审批意见
const submitting = ref(false); // 审批提交中
const activeKey = ref('detail'); // 当前页签（detail/flow/record）
const bizFormRef = ref<any>(null); // 申请表单组件引用（用于校验）
const taskFormRef = ref<any>(null); // 办理表单组件引用（用于校验）

// 跳转抽屉
const jumpVisible = ref(false); // 跳转抽屉显隐
const jumpLoading = ref(false); // 跳转节点加载态
const jumpNodes = ref<JumpableTaskRow[]>([]); // 可跳转节点列表
// 转办 / 加签 抽屉
const surrogateVisible = ref(false); // 转办抽屉显隐
const addCandidateVisible = ref(false); // 加签抽屉显隐
const actorIds = ref<string[]>([]); // 选中的处理人
const actorSaving = ref(false); // 转办/加签提交中
// 附加上下文（当前办理人视角：可编辑，提交时写 tf_* / f_*）
const ccActors = ref<string[]>([]); // 抄送人
const nextOperators = ref<string[]>([]); // 下一处理人
const applyReason = ref(''); // 申请理由（首节点）
const attachment = ref<SysFile | string>(''); // 附件

// 附加上下文（发起时/申请维度：只读回显，来自实例详情 d.ext）
// 必须与上面 ccActors/nextOperators/applyReason/attachment 分开：
// 上面那组是"当前办理人要填的"，若共用会被发起人的值污染审批人自己的输入
const initCcActors = ref<string[]>([]); // 发起时抄送人
const initNextOperators = ref<string[]>([]); // 发起时下一处理人
const initApplyReason = ref(''); // 发起时申请理由
const initAttachment = ref<any>(null); // 发起时附件（SysFile 或文件名字符串）
// 附加上下文（本次办理/审批维度：只读回显，来自任务详情的 tf_*）
// 三组互不影响：init* = 发起人填的，done* = 本次审批填的，ccActors/nextOperators/attachment = 当前办理人要填的
const doneCcActors = ref<string[]>([]); // 本次办理填的抄送人
const doneNextOperators = ref<string[]>([]); // 本次办理填的下一处理人
const doneAttachment = ref<SysFile | string>(''); // 本次办理上传的附件（tf_attachments = [SysFile]，取首个）

// 变量值归一化成字符串数组（数组 / 逗号分隔串 / 其它一律空数组）
function toArr(v: any): string[] {
	if (Array.isArray(v)) return v.map(String);
	if (typeof v === 'string' && v)
		return v
			.split(/[,，]/)
			.map((s) => s.trim())
			.filter(Boolean);
	return [];
}

// 从实例详情解析"发起时附加上下文"并写入只读变量（数据主体在 d.ext）
function syncApplyExtras(d: any) {
	const src = d?.ext ?? d?.formData ?? d?.variables ?? {};
	initCcActors.value = toArr(src.ccActors ?? src.f_ccActors);
	initNextOperators.value = toArr(src.nextNodeOperator ?? src.f_nextNodeOperator);
	initApplyReason.value = src.applyReason ?? src.f_applyReason ?? '';
	const att = src.attachment ?? src.attachments;
	initAttachment.value = Array.isArray(att) ? (att[0] ?? null) : (att ?? null);
}

// ── 计算属性：任务与节点 ──
const title = computed(() => (task.value ? `办理：${task.value.displayName}` : '办理任务')); // 抽屉标题
const instanceGraph = ref<Record<string, any> | null>(null); // 实例详情返回的完整流程定义图（task 详情的 jsonObject 常被裁剪）
// 流程定义图：优先实例详情的完整图，回退任务详情的 jsonObject（任务详情可能缺首节点 properties.form / 节点属性）
const graph = computed(() => instanceGraph.value || (task.value?.jsonObject as Record<string, any>) || null);
const isDoing = computed(() => task.value?.taskState === 10); // 任务是否进行中(DOING=10)
const isFirstTaskNode = computed(() => {
	// 当前任务是否首任务节点（决定走"重新提交"分支）
	const t = task.value as any;
	if (t?.ext?.isFirstTaskNode || t?.isFirstTaskNode) return true;
	const first = firstTaskNode(graph.value);
	return Boolean(first && (first.id === t?.taskName || first.properties?.name === t?.taskName));
});
const currentTaskNode = computed(() => {
	// 当前任务对应的流程图节点（取字段权限用）
	const t = task.value;
	if (!t) return null;
	return findTaskNode(graph.value, t.taskName) || { properties: { ...(t as any).taskModel, field: (t as any).taskModel?.ext || (t as any).taskModel?.field } };
});
const actionBtns = computed(() => resolveActionBtns(currentTaskNode.value, task.value?.performType)); // 当前节点可用审批按钮
const bizReadonly = computed(() => {
	// 申请信息是否只读
	if (props.readonly || !isDoing.value) return true; // 只读入口或任务非进行中 → 只读
	if (isFirstTaskNode.value) return false; // 首节点发起视角 → 可编
	return !graph.value?.enableFieldPerm; // 审批节点：启用字段权限时按字段决定，否则只读
});

// 判断某个审批按钮是否显示
function hasBtn(k: ActionBtnKey): boolean {
	return actionBtns.value.includes(k);
}

// ── 计算属性：表单 rule（统一 form-create 渲染） ──
const taskFormKey = computed(() => task.value?.formKey ?? ''); // 办理节点 formKey
// 申请表单 key：属于「实例/发起」范畴，任务详情的 graph 常缺首节点 properties.form，
// 故优先用实例详情解析出的 applyFormKeyResolved（ext.formKey / 流程图首节点 form），
// 首节点场景可直接用当前 task.formKey，最后退回任务 graph 兜底
const applyFormKeyResolved = ref<string>(''); // 由实例详情解析写入
const applyFormKey = computed(() => {
	if (isFirstTaskNode.value && task.value?.formKey) return task.value.formKey; // 首节点：当前表单即申请表单
	return applyFormKeyResolved.value || firstTaskFormKey(graph.value) || '';
});
const applyDesignedRule = ref<any[]>([]); // 申请表单 rule
const taskDesignedRule = ref<any[]>([]); // 办理表单 rule
const formCreateOption = { submitBtn: false, resetBtn: false }; // 审批场景不显示自带按钮
// form-create 的 disabled 未在 TS 类型中声明（运行时支持），整体以 any 透传避免类型报错
const applyFormProps = computed<any>(() => ({
	rule: applyDesignedRule.value,
	option: formCreateOption,
	disabled: bizReadonly.value, // 申请信息：只读入口/非进行中/审批节点(未启用字段权限) 均只读；首节点重新提交可编
}));
const taskFormReadonly = computed(() => props.readonly || !isDoing.value); // 办理表单：仅在可操作时可填
const taskFormProps = computed<any>(() => ({
	rule: taskDesignedRule.value,
	option: formCreateOption,
	disabled: taskFormReadonly.value,
}));

// 按 formKey 从 form-design 服务拉取表单 rule（与发起/详情抽屉同套逻辑）
async function loadFormRule(key?: string | number | null): Promise<any[]> {
	if (key == null || key === '') return [];
	await import('@form-create/designer');
	try {
		const res = await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignContentGet(Number(key));
		const content = res.data?.result; // 字符串形式的表单 rule JSON
		if (!content) return [];
		const parsed = JSON.parse(content);
		return Array.isArray(parsed) ? parsed : [];
	} catch {
		return [];
	}
}
// 把 f_/tf_ 前缀剥掉以对齐 rule.field（form-create 字段 key 不带前缀）
function stripPrefix(obj: Record<string, any>, prefix: string): Record<string, any> {
	const out: Record<string, any> = {};
	for (const [k, v] of Object.entries(obj)) {
		out[k.startsWith(prefix) ? k.slice(prefix.length) : k] = v;
	}
	return out;
}

// 申请表单与办理表单为同一 formKey 且无业务数据：下方可编辑办理表单已覆盖，详情区不再重复渲染空表单
const applyAreaRedundant = computed(() => Boolean(taskFormKey.value) && taskFormKey.value === applyFormKey.value && !Object.keys(bizFormData.value).length);
// 申请信息区是否显示"发起时附加上下文"只读回显：
// 首节点且可操作时，下方已渲染可编辑版本（line ~34），此处不再重复展示同一内容
const showApplyExtras = computed(() => props.readonly || !isDoing.value || !isFirstTaskNode.value);
// 只读/已办态是否回显「本次办理的附加上下文」（抄送/下一处理人/附件）：
// 表单开了对应开关（graph 上的 enableCcActors / selectUserOnInitiate / enableAttachment）
// 且本次审批确实填过才显示，避免渲染出一整块空控件
const showDoneExtras = computed(() => {
	const f = initiateExtraFlags(graph.value);
	return (f.cc && doneCcActors.value.length > 0) || (f.selectUser && doneNextOperators.value.length > 0) || (f.attachment && Boolean(doneAttachment.value));
});

// ── 数据加载：监听 visible / taskId，拉取任务详情及关联数据 ──
watch(
	() => [props.visible, props.taskId] as const,
	async ([v, id]) => {
		if (!v || !id) return; // 抽屉关闭或没有任务 ID 时不加载
		loading.value = true;
		// 重置所有状态，避免上一任务残留
		task.value = null; // 任务详情
		highLight.value = null; // 流程图高亮数据（已完成/当前节点）
		records.value = []; // 审批记录列表
		assigneeRows.value = []; // 节点处理人文本行
		formData.value = {}; // 办理表单数据（tf_* 剥前缀后）
		bizFormData.value = {}; // 申请表单数据（f_* 剥前缀后）
		applyDesignedRule.value = []; // 申请表单 form-create rule
		taskDesignedRule.value = []; // 办理表单 form-create rule
		applyFormKeyResolved.value = ''; // 实例详情解析出的申请表单 key
		instanceGraph.value = null; // 实例详情返回的完整流程定义图
		comment.value = ''; // 审批意见
		actorIds.value = []; // 转办/加签选中的处理人
		ccActors.value = []; // 抄送人（当前办理人可编辑）
		nextOperators.value = []; // 下一处理人（当前办理人可编辑）
		applyReason.value = ''; // 申请理由（首节点可编辑）
		attachment.value = ''; // 附件（当前办理人可编辑）
		initCcActors.value = []; // 发起时抄送人（申请维度只读回显）
		initNextOperators.value = []; // 发起时下一处理人（申请维度只读回显）
		initApplyReason.value = ''; // 发起时申请理由（申请维度只读回显）
		initAttachment.value = null; // 发起时附件（申请维度只读回显）
		doneCcActors.value = []; // 本次办理抄送人（审批维度只读回显）
		doneNextOperators.value = []; // 本次办理下一处理人（审批维度只读回显）
		doneAttachment.value = ''; // 本次办理附件（审批维度只读回显）
		activeKey.value = 'detail'; // 重置回「详情」页签
		try {
			// 任务详情：改用项目标准接口（api/jeeflowTask/detail；载荷在 res.data.result）
			const td = await getAPI(JeeflowTaskApi)
				.apiJeeflowTaskDetailPost({ id })
				.then((res) => (res.data?.result ?? null) as unknown as TaskDetail);
			task.value = td;
			// 任务级表单回显：取审批人上一轮填过的 tf_*；剥掉 tf_ 前缀对齐 rule.field
			const tf = extractTaskFormData(td);
			// 本次办理的附加上下文（抄送/下一处理人/附件）：collectTf 提交时写成 tf_ccActors / tf_nextNodeOperator / tf_attachments
			// （tf 里还有 tf_approvalComment 等其它字段，这里只按 key 取这三项）
			doneCcActors.value = toArr(tf.tf_ccActors);
			doneNextOperators.value = toArr(tf.tf_nextNodeOperator);
			const att = tf.tf_attachments; // [SysFile]：数组里放一个文件对象
			doneAttachment.value = Array.isArray(att) ? (att[0] ?? '') : (att ?? '');
			formData.value = stripPrefix(tf, 'tf_');
			comment.value = String(tf.tf_approvalComment ?? '');
			// 业务表单 f_* 回显：剥掉 f_ 前缀对齐 rule.field
			bizFormData.value = stripPrefix(extractBizFormData(td), 'f_');
			const instId = td.processInstanceId;
			// 并行拉取：流程图高亮 / 审批记录 / 节点处理人文本
			const jobs: Promise<void>[] = [
				getAPI(JeeflowInstanceApi)
					.apiJeeflowInstanceHighLightPost({ id: instId })
					.then((res) => {
						highLight.value = (res.data?.result ?? null) as unknown as HighLightData;
					})
					.catch(() => {
						highLight.value = null;
					}),
				getAPI(JeeflowInstanceApi)
					.apiJeeflowInstanceApprovalRecordPost({ id: instId })
					.then((res) => {
						records.value = (res.data?.result ?? []) as unknown as ApprovalRecordRow[];
					})
					.catch(() => {
						records.value = [];
					}),
				getAPI(JeeflowInstanceApi)
					.apiJeeflowInstanceGetAssigneeTextDataPost({ id: instId, includeNodeName: true })
					.then(async (res) => {
						// 接口只回「节点名:userId」，这里把 userId 反查成姓名（value 不变，流程图靠它定位文本）
						const rows = (res.data?.result ?? []) as unknown as AssigneeTextRow[];
						assigneeRows.value = (await resolveAssigneeLabels(rows)) as AssigneeTextRow[];
					})
					.catch(() => {
						assigneeRows.value = [];
					}),
			];
			// 实例详情：始终拉取，用于解析申请表单 key（任务详情的 graph 常缺首节点 properties.form）
			// 同时补全完整流程图 / 在任务详情业务数据缺失时补充 f_* 业务数据
			jobs.push(
				getAPI(JeeflowInstanceApi)
					.apiJeeflowInstanceDetailPost({ id: instId })
					.then(async (res) => {
						const d = (res.data?.result ?? null) as unknown as InstanceDetail;
						// 仅当任务详情业务数据缺失时用实例详情补全，避免重复覆盖已有数据
						// 注意：此处只补「申请节点」数据——extractBizFormData 只抽 f_*（申请表单），
						// task 的 tf_* 办理数据已在顶部任务详情(td) 中通过 extractTaskFormData 取得，这里不处理
						if (!Object.keys(bizFormData.value).length) {
							const more = extractBizFormData(d);
							if (Object.keys(more).length) bizFormData.value = stripPrefix(more, 'f_');
						}
						// 完整流程定义图存到独立的 instanceGraph（不污染 task 对象）：任务详情的 jsonObject 常被裁剪，
						// 实例详情的图才含首节点 properties.form 等完整节点信息，供流程图渲染 / 定位当前节点 / 字段权限使用
						if (d.jsonObject) instanceGraph.value = d.jsonObject;
						// 解析"发起时附加上下文"（抄送/下一处理人/申请理由/附件，主体在 d.ext）供申请信息区只读回显
						syncApplyExtras(d);
						// 首节点重新提交：把"发起时附加上下文"回填到可编辑变量，用户可在原值上修改，
						// 避免重新提交时直接提交空值、丢失原抄送/下一处理人/理由/附件（只读 init* 仅供展示，不参与提交）
						if (isFirstTaskNode.value) {
							ccActors.value = initCcActors.value;
							nextOperators.value = initNextOperators.value;
							applyReason.value = initApplyReason.value;
							attachment.value = initAttachment.value;
						}
						// 申请表单 key：优先实例级 ext.formKey，其次流程图首节点 form；解析后直接加载 rule
						applyFormKeyResolved.value = (d as any)?.ext?.formKey || firstTaskFormKey(d.jsonObject) || '';
						applyDesignedRule.value = await loadFormRule(applyFormKey.value);
					})
					.catch(() => {})
			);
			// 办理表单 rule：taskFormKey 已由任务详情就绪，并入 jobs 并行加载（不必等实例详情）
			jobs.push(
				loadFormRule(taskFormKey.value).then((r) => {
					taskDesignedRule.value = r;
				})
			);
			await Promise.all(jobs);
		} finally {
			loading.value = false;
		}
	},
	{ immediate: true } // 初始即执行（抽屉可能首次就打开）
);

// ── 跳转 ──
async function openJump() {
	jumpVisible.value = true;
	jumpNodes.value = [];
	if (!task.value) return;
	jumpLoading.value = true;
	try {
		// 可跳转节点：改用项目标准接口（api/jeeflowTask/jumpAbleTaskNameList；沿用 facade 字段 processInstanceId）
		jumpNodes.value = await getAPI(JeeflowTaskApi)
			.apiJeeflowTaskJumpAbleTaskNameListPost({ processInstanceId: task.value.processInstanceId })
			.then((res) => (res.data?.result ?? []) as unknown as JumpableTaskRow[]);
	} catch {
		jumpNodes.value = [];
	} finally {
		jumpLoading.value = false;
	}
}

// 收集审批表单数据：统一加 tf_ 前缀，附加意见/抄送/下一处理人
function collectTf(): Record<string, unknown> {
	const tf: Record<string, unknown> = {};
	for (const [k, v] of Object.entries(formData.value)) {
		if (v === '' || v == null) continue;
		tf[k.startsWith('tf_') ? k : `tf_${k}`] = v;
	}
	if (comment.value.trim() && !('tf_approvalComment' in tf)) {
		tf.tf_approvalComment = comment.value.trim(); // 审批意见
	}
	if (ccActors.value.length) tf.tf_ccActors = ccActors.value; // 抄送
	if (nextOperators.value.length) tf.tf_nextNodeOperator = nextOperators.value; // 下一处理人
	if (attachment.value) tf.tf_attachments = [attachment.value]; // 附件（审批人上传；必须用 tf_ 前缀，避免与实例级 attachments 撞车覆盖申请附件）
	// 办理人手写体签名：取自当前登录用户，作为任务变量 tf_signature 提交（与 tf_approvalComment 同口径，后端按节点回写）
	if (userStore.userInfos?.signature) tf.tf_signature = userStore.userInfos.signature;
	return tf;
}

// 收集业务表单数据：统一加 f_ 前缀，附加抄送/下一处理人/申请理由/附件
function collectBiz(): Record<string, unknown> {
	const form: Record<string, unknown> = {};
	for (const [k, v] of Object.entries(bizFormData.value)) {
		if (v === '' || v == null) continue;
		form[k.startsWith('f_') ? k : `f_${k}`] = v;
	}
	if (ccActors.value.length) form.f_ccActors = ccActors.value;
	if (nextOperators.value.length) form.f_nextNodeOperator = nextOperators.value;
	if (applyReason.value.trim()) form.f_applyReason = applyReason.value.trim(); // 申请理由（首节点）
	// 发起人手写体签名（首节点重新提交等业务表单）：作为业务变量 f_signature 提交
	if (userStore.userInfos?.signature) form.f_signature = userStore.userInfos.signature;
	// if (attachment.value) form.f_attachment = attachment.value; // 附件
	if (attachment.value) form.attachments = [attachment.value]; // 附件
	return form;
}

// 执行审批动作：提交到引擎，成功关闭抽屉并通知父组件刷新
async function doExecute(submitType: number, extra: Record<string, unknown> = {}) {
	if (!props.taskId) return;
	// 校验办理表单（仅当表单已渲染且可编辑：非首节点 + 有 rule + 非只读）
	if (taskDesignedRule.value.length && !isFirstTaskNode.value && !taskFormReadonly.value) {
		const inst: any = taskFormRef.value;
		const fapi = inst?.fapi ?? (typeof inst?.validate === 'function' ? inst : null);
		if (fapi) {
			try {
				await fapi.validate();
			} catch (e) {
				console.warn('[办理] 表单校验未通过：', e);
				ElMessage.error('表单校验未通过，请检查必填项');
				return;
			}
		}
	}
	submitting.value = true;
	try {
		// 启用字段权限时，把业务表单(f_)一并收集提交
		const extraForm = !isFirstTaskNode.value && graph.value?.enableFieldPerm ? collectBiz() : {};
		// 首节点重新提交：只提交申请数据（collectBiz 经 extra 传入），无办理表单/审批意见，故不收集 tf_*
		// 否则会把同一组 cc/next/attachment 又写成 tf_* 并带入上一轮 tf_approvalComment，污染重新提交
		const tfData = isFirstTaskNode.value ? {} : collectTf();
		const payload = {
			processTaskId: props.taskId,
			submitType: submitType as any,
			...tfData,
			...extraForm,
			...extra,
		};

		// 调试：提交前打印完整入参（申请表单/办理表单/意见/抄送/下一处理人等）
		console.log('[办理提交] apiJeeflowTaskExecutePost 入参：', payload);
		// return;
		// 办理：改用项目标准接口（api/jeeflowTask/execute；沿用 facade 字段 processTaskId/submitType）
		await getAPI(JeeflowTaskApi).apiJeeflowTaskExecutePost(payload);
		ElMessage.success('办理成功');
		emit('changed');
		emit('update:visible', false);
	} catch (e) {
		ElMessage.error((e as Error).message || '办理失败');
	} finally {
		submitting.value = false;
	}
}

// 重新提交（首节点）：先校验申请表单，再提交业务数据
async function doReApply() {
	const inst: any = bizFormRef.value;
	const fapi = inst?.fapi ?? (typeof inst?.validate === 'function' ? inst : null);
	if (fapi) {
		try {
			await fapi.validate();
		} catch (e) {
			console.warn('[重新提交] 表单校验未通过：', e);
			ElMessage.error('表单校验未通过，请检查必填项');
			return;
		}
	}
	await doExecute(SubmitType.RE_APPLY, collectBiz());
}

// 会签拒绝
function doCountersignDisagree() {
	return doExecute(SubmitType.COUNTERSIGN_DISAGREE, { countersignDisagreeFlag: true });
}

// 执行跳转：提交后关闭跳转抽屉
async function doJump(taskName: string) {
	await doExecute(SubmitType.JUMP, { taskName });
	jumpVisible.value = false;
}

// 执行转办或加签
async function doActors() {
	if (!props.taskId || !actorIds.value.length) return;
	actorSaving.value = true;
	try {
		// 转办 / 加签：改用项目标准接口（api/jeeflowTask/surrogate、addCandidate；沿用 facade 字段 processTaskId/actorIds）
		if (surrogateVisible.value)
			await getAPI(JeeflowTaskApi).apiJeeflowTaskSurrogatePost({ processTaskId: props.taskId, actorIds: actorIds.value }); // 转办
		else await getAPI(JeeflowTaskApi).apiJeeflowTaskAddCandidatePost({ processTaskId: props.taskId, actorIds: actorIds.value }); // 加签
		surrogateVisible.value = false;
		addCandidateVisible.value = false;
		actorIds.value = [];
		emit('changed');
	} finally {
		actorSaving.value = false;
	}
}
</script>

<style lang="scss">
/* JfDrawer → el-drawer 行为还原：body-class 直接加在 .el-drawer__body 上，非 scoped 命中更稳 */
/* 复合 .el-drawer__body.xxx 特异性(0,2,0) 压过 Element 默认 .el-drawer__body(0,1,0) */
.el-drawer__body.jf-approve-drawer-body {
	display: flex;
	flex-direction: column;
	overflow-y: auto;
	padding: 0 16px; /* 对齐原 JfDrawer .jf-drawer__body 的 16px */
}
/* fill：内容铺满不滚动（流程图 Tab 的 JfFlowViewer height=100% 依赖这层拉伸） */
.el-drawer__body.jf-approve-drawer-body--fill {
	overflow: hidden;
}
.el-drawer__body.jf-approve-drawer-body--fill > * {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}
/* el-tabs 作为 fill 抽屉直接子元素：header 固定、content 撑满，保证流程图 height=100% 生效 */
.el-drawer__body.jf-approve-drawer-body--fill > .el-tabs > .el-tabs__header {
	flex-shrink: 0;
}
.el-drawer__body.jf-approve-drawer-body--fill > .el-tabs > .el-tabs__content {
	flex: 1;
	min-height: 0;
	overflow: hidden;
}
/* 让每个页签自身成为滚动容器：内容超出时纵向滚动条出现在页签内（流程图页签仍可 height:100% 铺满） */
.el-drawer__body.jf-approve-drawer-body--fill > .el-tabs > .el-tabs__content > .el-tab-pane {
	height: calc(100% - 10px);
	overflow: auto;
}
/* 选择用户类字段卡片化分组（替代原 label+输入框裸排，提升可读性）；
   .jf-actor-field 为 ApproveDrawer 转办/加签专用，.jf-field-card 为通用别名供其它抽屉复用 */
.jf-actor-field,
.jf-field-card {
	margin-top: 16px;
	padding-bottom: 16px;
	border-radius: 8px;
}
.jf-actor-field .jf-form-label,
.jf-field-card .jf-form-label {
	margin-bottom: 8px;
}
</style>
