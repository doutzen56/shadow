<template>
	<!-- 外层"流程详情"抽屉：受控显隐（visible 是 prop，用 :model-value + @update:model-value，不能 v-model）；
       body-class 还原 JfDrawer 的 fill 拉伸行为（--fill 让内容铺满不滚动） -->
	<el-drawer body-class="jf-approve-drawer-body jf-approve-drawer-body--fill" :model-value="visible" :title="title" :size="width" @update:model-value="emit('update:visible', $event)">
		<!-- 加载中：拉取实例详情期间显示 -->
		<div v-if="loading" class="jf-loading">加载中...</div>
		<!-- 已加载且有数据：渲染详情（v-else-if 避免空数据闪一下） -->
		<div v-else-if="data" class="jf-detail-body">
			<!-- 详情 / 流程图 / 审批记录 三个页签 -->
			<el-tabs v-model="activeKey">
				<!-- 详情页签 -->
				<el-tab-pane label="详情" name="detail">
					<!-- 头部元信息：标题 / 发起人 / 流水号 / 时间 / 当前处理人 -->
					<div class="jf-detail-meta">
						<!-- 元信息：标签（静态文字）用主文字色醒目，值（{{}} 内容）用 jf-muted 浅色且不加粗 -->
						<span
							>流程标题: <span class="jf-muted">{{ instanceTitle }}</span></span
						>
						<span
							>发起人: <span class="jf-muted">{{ initiatorName }}</span></span
						>
						<!-- 有流水号才显示 -->
						<span v-if="data.businessNo"
							>流水号: <span class="jf-muted">{{ data.businessNo }}</span></span
						>
						<span
							>时间: <span class="jf-muted">{{ fmtTime(data.createTime) }}</span></span
						>
						<!-- 有当前处理人（进行中）才显示 -->
						<span v-if="assigneeText"
							>当前处理人: <span class="jf-muted">{{ assigneeText }}</span></span
						>
					</div>

					<!-- 审批进度面板：有节点进度才渲染 -->
					<div v-if="progressNodes.length" class="jf-progress-panel">
						<div v-for="p in progressNodes" :key="p.node" class="jf-progress-block">
							<div class="jf-progress-title">
								{{ p.displayName }}
								<!-- 会签节点标注串行/并行 -->
								<span v-if="p.type" class="jf-muted">（{{ p.type === 'SEQUENTIAL' ? '串行' : p.type === 'PARALLEL' ? '并行' : p.type }}会签）</span>
							</div>
							<div class="jf-progress-members">
								<span v-for="m in p.members" :key="m.id" class="jf-progress-member" :class="{ 'jf-progress-member--done': m.done, 'jf-progress-member--active': m.active }">
									{{ m.name || m.id }}{{ m.done ? ' ✓' : '' }}
								</span>
							</div>
						</div>
					</div>

					<h3 class="jf-section-title">申请信息</h3>
					<!-- 申请表单：统一用 form-create 渲染，rule 按 ext.formKey 拉取、数据用 d.formData 回显；
					     非重填态时整体 disabled 只读；ref 即 fApi（含 validate），用于重新提交校验 -->
					<form-create v-if="designedRule.length" ref="bizFormRef" v-model="bizFormData" v-bind="formCreateProps" />
					<div v-else class="jf-muted jf-form-hint">该流程未配置申请表单</div>
					<!-- 附加上下文：抄送人 / 下一处理人 / 申请理由 / 附件（详情页只读回显） -->
					<JfInitiateExtras
						:graph="data?.jsonObject"
						:disabled="true"
						v-model:cc-actors="ccActors"
						v-model:next-operators="nextOperators"
						v-model:apply-reason="applyReason"
						v-model:attachment="attachment"
					/>

					<div class="jf-detail-actions">
						<!-- 撤回：有撤回权限 且 流程进行中(state===10) 且 存在活动任务 -->
						<el-button v-auth="'instance/revoke'" v-if="data.state === 10 && activeTaskList.length" plain :icon="RefreshLeft" :disabled="acting" @click="withdraw">撤回</el-button>
						<!-- 抄送：有抄送权限 -->
						<el-button v-auth="'cclist/cc'" plain :icon="Promotion" @click="ccVisible = true">抄送</el-button>
						<!-- 重新提交：首任务节点且可重填（发起人补交入口） -->
						<el-button v-if="reSubmitable" type="primary" :icon="Edit" :disabled="acting" @click="reSubmit">重新提交</el-button>
					</div>
				</el-tab-pane>

				<!-- 流程图页签 -->
				<el-tab-pane label="流程图" name="flow">
					<!-- 有流程定义图才渲染查看器 -->
					<div v-if="data.jsonObject" class="jf-pane-flow">
						<JfFlowViewer :graph-data="data.jsonObject" :high-light="highLight" :assignee-text-data="assigneeRows" height="100%" />
					</div>
					<!-- 无图空态 -->
					<div v-else class="jf-empty">暂无流程图</div>
				</el-tab-pane>

				<!-- 审批记录页签 -->
				<el-tab-pane label="审批记录" name="record">
					<JfApprovalRecord :records="records" />
				</el-tab-pane>
			</el-tabs>
		</div>
		<!-- 无数据空态 -->
		<div v-else class="jf-empty">实例不存在</div>

		<!-- 嵌套"手动抄送"抽屉：append-to-body 避免被外层 overflow:hidden 裁切；
         body-class 复用 ApproveDrawer 嵌套抽屉的 padding:0 16px -->
		<el-drawer v-model="ccVisible" append-to-body body-class="jf-approve-drawer-body" title="手动抄送" size="480px">
			<div class="jf-field-card">
				<label class="jf-form-label">抄送人</label>
				<JfUserPicker v-model="ccIds" scene="cc" placeholder="搜索姓名/工号" />
			</div>
			<div class="jf-drawer-actions">
				<el-button plain :icon="Close" @click="ccVisible = false">取消</el-button>
				<el-button type="primary" :icon="Select" :disabled="ccSaving || !ccIds.length" @click="doCc">确定</el-button>
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
import { fmtTime, stateLabel, pickExt, pickInstanceExt, firstTaskFormKey, resolveAssigneeLabels } from '../helpers';
import { ElMessage } from 'element-plus';
// Element Plus 图标
import { RefreshLeft, Promotion, Edit, Close, Select } from '@element-plus/icons-vue';
import type { InstanceDetail, HighLightData, ApprovalRecordRow, AssigneeTextRow, NodeProgress } from '../types';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowInstanceApi, JeeflowTaskApi, JeeflowFormDesignApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfInstanceDetailDrawer' });

// ── Props / Emits ──
// visible：抽屉显隐（受控）；instanceId：实例 ID（触发加载）；width：宽度
const props = withDefaults(
	defineProps<{
		visible: boolean;
		instanceId: string | null;
		width?: string;
	}>(),
	{ width: '60%' }
);

// update:visible 通知父组件关闭；changed 通知父组件数据变化（撤回/重填后刷新列表）
const emit = defineEmits<{
	'update:visible': [v: boolean];
	changed: [];
}>();

// ── 响应式状态 ──
const loading = ref(false); // 详情加载态
const acting = ref(false); // 撤回/重新提交 操作中（禁用对应按钮）
const data = ref<InstanceDetail | null>(null); // 实例详情
const records = ref<ApprovalRecordRow[]>([]); // 审批记录
const highLight = ref<HighLightData | null>(null); // 流程图高亮（节点进度）
const assigneeRows = ref<AssigneeTextRow[]>([]); // 当前处理人文本行
const bizFormData = ref<Record<string, any>>({}); // 申请信息业务数据（f_*）
const bizFormRef = ref<any>(null); // 申请表单引用（用于 reSubmit 校验 validate）
const activeKey = ref('detail'); // 当前页签

const ccVisible = ref(false); // 抄送抽屉显隐
const ccIds = ref<string[]>([]); // 抄送人 ID 列表
const ccSaving = ref(false); // 抄送保存中

// 附加上下文（抄送人 / 下一处理人 / 申请理由 / 附件）：详情页只读回显，数据来自实例 ext / formData
const ccActors = ref<string[]>([]);
const nextOperators = ref<string[]>([]);
const applyReason = ref('');
const attachment = ref<any>(null);

// ── 计算属性 ──
// 抽屉标题：流程名 + 状态标签
const title = computed(() => {
	if (!data.value) return '流程详情';
	return `${data.value.displayName || '流程详情'} · ${stateLabel(data.value.state)}`;
});

const activeTaskList = computed(() => data.value?.activeTaskList ?? []); // 活动任务列表
// 是否可重新提交：存在首任务节点（发起人补交入口）
const reSubmitable = computed(() => Boolean(activeTaskList.value.find((t) => t.ext?.isFirstTaskNode)));
/**
 * 实例变量：ext 优先（引擎按 issues/124 §7 给 detail 补上后即以此为准），variables 只是过渡兜底——
 * 现网八栈 detail 都还没有 ext，而 formData 只切 f_*，标题 autoGenTitle 与发起人 u_realName 只在
 * variables 里，所以此刻直接删兜底会让八栈头部同时变空。引擎落地后删掉最后一项即收口。
 */
const instVars = computed<Record<string, any>>(() => {
	const d = data.value as any;
	for (const cand of [pickExt(d), pickInstanceExt(d), d?.variables]) {
		if (cand && typeof cand === 'object' && Object.keys(cand).length) return cand;
	}
	return {};
});
const instanceTitle = computed(() => String(instVars.value.autoGenTitle || instVars.value.f_title || data.value?.displayName || '-'));
const initiatorName = computed(() => String(instVars.value.u_realName || data.value?.operator || '-'));

// 申请表单：formKey 优先取实例详情 ext.formKey（引擎落地后以此为准），
// 现网八栈 detail 尚未补 ext，回落流程图首个任务节点的 form 属性（与发起时定位一致）
const instFormKey = computed(() => (data.value as any)?.ext?.formKey || firstTaskFormKey(data.value?.jsonObject) || '');
const designedRule = ref<any[]>([]); // 申请表单 rule（form-create 渲染用）
const formCreateOption = { submitBtn: false, resetBtn: false }; // 详情场景不显示自带按钮
// form-create 的 disabled 未在 TS 类型中声明（运行时支持，禁用所有输入），整体以 any 透传避免类型报错
const formCreateProps = computed<any>(() => ({
	rule: designedRule.value,
	option: formCreateOption,
	disabled: !reSubmitable.value,
}));
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

// 从实例详情抽取附加上下文（抄送人 / 下一处理人 / 申请理由 / 附件）。
// 这些数据以 f_ 前缀字段提交（f_ccActors / f_nextNodeOperator / f_applyReason / attachments），
// 后端可能落在 ext（引擎落地后）或 formData / variables 中，这里多源兜底取。
function syncExtras(d: any) {
	const src = d?.ext ?? d?.formData ?? d?.variables ?? {};
	const toArr = (v: any): string[] =>
		Array.isArray(v)
			? v.map(String)
			: typeof v === 'string' && v
				? v
						.split(/[,，]/)
						.map((s) => s.trim())
						.filter(Boolean)
				: [];
	ccActors.value = toArr(src.ccActors ?? src.f_ccActors);
	nextOperators.value = toArr(src.nextNodeOperator ?? src.f_nextNodeOperator);
	applyReason.value = src.applyReason ?? src.f_applyReason ?? '';
	const att = src.attachment ?? src.attachments;
	attachment.value = Array.isArray(att) ? (att[0] ?? null) : (att ?? null);
}

// 当前处理人文本：拼接 assigneeRows 的 label/value
const assigneeText = computed(() => {
	console.log('🚀 ~ assigneeRows.value:', assigneeRows.value);
	return assigneeRows.value
		.map((r) => r.label || r.value)
		.filter(Boolean)
		.join('、');
});

// 审批进度：过滤出"还有活动成员或未完成"的节点，映射为展示结构（跳过已全完成的结束节点）
const progressNodes = computed(() => {
	const np = highLight.value?.nodeProgress;
	if (!np || !data.value) return [];
	const out: Array<{ node: string; displayName: string; type?: string; members: NodeProgress['members'] }> = [];
	for (const [node, p] of Object.entries(np)) {
		if (!p?.members?.length) continue;
		if (!p.members.some((m) => m.active) && p.members.every((m) => m.done)) continue;
		const task = data.value.jsonObject?.nodes?.find?.((n: any) => n.id === node);
		out.push({
			node,
			displayName: task?.text?.value || node,
			type: p.type,
			members: p.members,
		});
	}
	return out;
});

// ── 数据加载：visible 或 instanceId 变化即加载（immediate 首屏也执行） ──
watch(
	() => [props.visible, props.instanceId] as const,
	async ([v, id]) => {
		if (!v || !id) return; // 抽屉关闭或无 ID 不加载
		loading.value = true;
		// 重置状态，避免上一实例残留
		data.value = null;
		records.value = [];
		highLight.value = null;
		assigneeRows.value = [];
		bizFormData.value = {};
		designedRule.value = [];
		ccActors.value = [];
		nextOperators.value = [];
		applyReason.value = '';
		attachment.value = null;
		activeKey.value = 'detail';
		try {
			await reload();
		} finally {
			loading.value = false;
		}
	},
	{ deep: true }
);

// 并行拉取：实例详情 / 审批记录 / 流程图高亮；附加重处理人文本（失败不阻塞主流程）
async function reload() {
	if (!props.instanceId) return;
	const [d, rec, hl] = await Promise.all([
		// 实例详情 / 审批记录 / 流程图高亮：改用项目标准接口（载荷在 res.data.result）
		getAPI(JeeflowInstanceApi)
			.apiJeeflowInstanceDetailPost({ id: props.instanceId })
			.then((res) => (res.data?.result ?? null) as unknown as InstanceDetail),
		getAPI(JeeflowInstanceApi)
			.apiJeeflowInstanceApprovalRecordPost({ id: props.instanceId })
			.then((res) => (res.data?.result ?? []) as unknown as ApprovalRecordRow[]),
		getAPI(JeeflowInstanceApi)
			.apiJeeflowInstanceHighLightPost({ id: props.instanceId })
			.then((res) => (res.data?.result ?? null) as unknown as HighLightData),
	]);
	console.log('🚀 ~ reload ~ d:', d);
	data.value = d;
	records.value = rec;
	highLight.value = hl;
	// 申请信息业务数据：直接使用实例详情里的 formData；提交时 key 带 f_ 前缀，这里去掉以对齐 rule.field
	const rawForm = (d as any).formData ?? {};
	const mapped: Record<string, any> = {};
	for (const [k, v] of Object.entries(rawForm)) {
		const key = k.startsWith('f_') ? k.slice(2) : k;
		mapped[key] = v;
	}
	bizFormData.value = mapped;
	syncExtras(d); // 回显抄送人 / 下一处理人 / 申请理由 / 附件
	designedRule.value = await loadFormRule(instFormKey.value); // 按 ext.formKey 拉取申请表单 rule
	try {
		// 处理人文本：改用项目标准接口（api/jeeflowInstance/getAssigneeTextData；载荷在 res.data.result）
		const rows = await getAPI(JeeflowInstanceApi)
			.apiJeeflowInstanceGetAssigneeTextDataPost({ id: props.instanceId, includeNodeName: true })
			.then((res) => (res.data?.result ?? []) as unknown as AssigneeTextRow[]);
		// 接口只回「节点名:userId」，这里把 userId 反查成姓名（value 不变，流程图靠它定位文本）
		assigneeRows.value = (await resolveAssigneeLabels(rows)) as AssigneeTextRow[];
		console.log('🚀 ~ reload ~ assigneeRows.value:', assigneeRows.value);
	} catch {
		assigneeRows.value = [];
	}
}

// 撤回：二次确认后调用 withdraw，成功通知父组件刷新
async function withdraw() {
	if (!props.instanceId) return;
	if (!window.confirm('确认撤回该流程？')) return;
	acting.value = true;
	try {
		// 撤回：改用项目标准接口（api/jeeflowInstance/withdraw）
		await getAPI(JeeflowInstanceApi).apiJeeflowInstanceWithdrawPost({ id: props.instanceId });
		ElMessage.success('已撤回');
		emit('changed');
		await reload();
	} catch (e) {
		ElMessage.error(`撤回失败：${(e as Error).message}`);
	} finally {
		acting.value = false;
	}
}

// 重新提交：校验申请表单 → 收集 f_* → 以"提交类型 5"执行首任务节点（发起人补交）
async function reSubmit() {
	const task = activeTaskList.value.find((t) => t.ext?.isFirstTaskNode);
	if (!task) return;
	// form-create 的校验 API 挂在 fapi 上（组件实例本身无 validate），校验失败 reject
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
	acting.value = true;
	try {
		const form: Record<string, unknown> = {};
		for (const [k, v] of Object.entries(bizFormData.value)) {
			if (v === '' || v == null) continue;
			form[k.startsWith('f_') ? k : `f_${k}`] = v;
		}
		// 重新提交（提交类型 5）：改用项目标准接口（api/jeeflowTask/execute；沿用 facade 字段 processTaskId/submitType）
		await getAPI(JeeflowTaskApi).apiJeeflowTaskExecutePost({ processTaskId: task.id, submitType: 5, ...form });
		ElMessage.success('已重新提交');
		emit('changed');
		await reload();
	} catch (e) {
		ElMessage.error(`重新提交失败：${(e as Error).message}`);
	} finally {
		acting.value = false;
	}
}

// 抄送：创建抄送实例，成功关闭抽屉并清空选择
async function doCc() {
	if (!props.instanceId || !ccIds.value.length) return;
	ccSaving.value = true;
	try {
		// 创建抄送：改用项目标准接口（api/jeeflowInstance/createCc；文档入参 { id, userIds }）
		await getAPI(JeeflowInstanceApi).apiJeeflowInstanceCreateCcPost({ id: props.instanceId, userIds: ccIds.value });
		ElMessage.success('抄送成功');
		ccVisible.value = false;
		ccIds.value = [];
	} catch (e) {
		ElMessage.error((e as Error).message || '抄送失败');
	} finally {
		ccSaving.value = false;
	}
}
</script>

<style scoped>
.jf-progress-panel {
	border: 1px solid var(--jf-border, #f0f0f0);
	border-radius: 8px;
	padding: 10px 12px;
	background: var(--jf-hover-bg, #fafbfc);
	display: flex;
	flex-direction: column;
	gap: 8px;
	margin-bottom: 12px;
}
.jf-progress-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--jf-text, #1f1f1f);
}
.jf-progress-members {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
	margin-top: 6px;
}
.jf-progress-member {
	padding: 2px 10px;
	border-radius: 12px;
	font-size: 12px;
	background: #fff;
	border: 1px solid var(--jf-border, #e5e5e5);
	color: #666;
}
.jf-progress-member--active {
	border-color: var(--jf-primary, #1677ff);
	color: var(--jf-primary, #1677ff);
}
.jf-progress-member--done {
	background: var(--jf-done-soft, #f6ffed);
	border-color: #b7eb8f;
	color: #389e0d;
}
/* 详情抽屉：el-tabs 嵌套在 .jf-detail-body 内（不是抽屉 body 直接子级），fill 的 > * 只命中 body 直接子级，
   这里单独把 el-tabs 撑高，保证流程图 Tab 的 JfFlowViewer height=100% 生效 */
.jf-detail-body {
	display: flex;
	flex-direction: column;
	min-height: 0;
}
.jf-detail-body > .el-tabs {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}
.jf-detail-body > .el-tabs > .el-tabs__header {
	flex-shrink: 0;
}
.jf-detail-body > .el-tabs > .el-tabs__content {
	flex: 1;
	min-height: 0;
	overflow: hidden;
}
.jf-detail-body > .el-tabs > .el-tabs__content > .el-tab-pane {
	height: 100%;
	overflow: auto;
}
</style>
