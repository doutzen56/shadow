<template>
	<!-- 外层"发起流程"抽屉：受控显隐（visible 是 prop，用 :model-value + @update:model-value 而非 v-model，避免改 prop）；
	     body-class 直接挂在 .el-drawer__body 上还原 JfDrawer 行为；--fill 让内容铺满不滚动 -->
	<el-drawer body-class="jf-approve-drawer-body jf-approve-drawer-body--fill" :model-value="visible" :title="drawerTitle" :size="width" @update:model-value="emit('update:visible', $event)">
		<!-- 加载中：拉取流程定义期间显示 loading -->
		<div v-if="loading" class="jf-loading">加载中...</div>
		<!-- 已加载：渲染内容（loading 完成后才进入） -->
		<template v-else>
			<!-- 两个页签：表单 / 流程图 -->
			<el-tabs v-model="activeKey">
				<!-- 表单页签 -->
				<el-tab-pane label="表单" name="form">
					<h3 class="jf-section-title">申请信息</h3>
					<!-- 设计器生成的表单：按首任务节点 formKey 调 /api/formDesign/content 取 rule（见 designedRule），
					     用 form-create 渲染；隐藏其自带提交/重置按钮（用抽屉底部「发起」）；ref 即 fApi，含 validate() -->
					<form-create ref="formRef" v-model="formData" v-bind="designedBind" />
					<!-- 附加上下文：抄送人 / 下一处理人 / 申请理由 / 附件 -->
					<JfInitiateExtras :graph="graph" v-model:cc-actors="ccActors" v-model:next-operators="nextOperators" v-model:apply-reason="applyReason" v-model:attachment="attachment" />
					<div class="jf-drawer-actions">
						<!-- 取消：直接关闭抽屉 -->
						<el-button plain :icon="Close" @click="emit('update:visible', false)">取消</el-button>
						<!-- 发起：提交；starting 期间 loading 禁用，避免重复提交 -->
						<el-button type="primary" :icon="Promotion" :loading="starting" @click="doStart">发起</el-button>
					</div>
				</el-tab-pane>
				<!-- 流程图页签 -->
				<el-tab-pane label="流程图" name="flow">
					<!-- 有流程定义图：渲染流程图查看器 -->
					<div v-if="graph" class="jf-pane-flow">
						<JfFlowViewer :graph-data="graph" height="100%" />
					</div>
					<!-- 无流程定义图：空态 -->
					<div v-else class="jf-empty">暂无流程图</div>
				</el-tab-pane>
			</el-tabs>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
// 依赖引入
import { ref, computed, watch } from 'vue';
import JfFlowViewer from '../ui/JfFlowViewer.vue';
import JfInitiateExtras from '../ui/JfInitiateExtras.vue';
import { ElMessage } from 'element-plus';
import type { DefineDetail } from '../types';
import { getAPI } from '/@/utils/axios-utils';
import { useUserInfo } from '/@/stores/userInfo';
import { JeeflowDefineApi, JeeflowFormDesignApi } from '/@/api-services/jeeFlow/api';
import { firstTaskFormKey, firstTaskNode } from '../helpers';
// Element Plus 图标
import { Close, Promotion } from '@element-plus/icons-vue';

defineOptions({ name: 'JfStartDrawer' });

// 渲染选项：隐藏 form-create 自带的提交/重置按钮（改用抽屉底部「发起」），去掉它那套默认布局
const designedOption = { submitBtn: false, resetBtn: false };

// ── Props / Emits ──
// visible：抽屉显隐（受控）；define：流程定义（含 jsonObject 流程图）；title/width：标题/宽度
const props = withDefaults(
	defineProps<{
		visible: boolean;
		define?: Record<string, any> | null;
		title?: string;
		width?: string;
	}>(),
	{ title: '发起流程', width: '60%' }
);

// update:visible 通知父组件关闭；started 通知父组件已发起（传实例 ID）
const emit = defineEmits<{
	'update:visible': [v: boolean];
	started: [instanceId: string];
}>();

// ── 响应式状态 ──
const loading = ref(false); // 流程定义加载态
const starting = ref(false); // 发起提交中
const activeKey = ref('form'); // 当前页签（form/flow），默认表单
const formKey = ref(''); // 首任务节点 formKey（即表单设计 id），用于拉取表单 rule
const formData = ref<Record<string, any>>({}); // 表单数据（f_* 业务字段）
const graph = ref<Record<string, any> | null>(null); // 流程定义图（LogicFlow JSON）
const formRef = ref<any>(null); // 表单组件引用（用于校验 validate）
const ccActors = ref<string[]>([]); // 抄送人
const nextOperators = ref<string[]>([]); // 下一处理人
const applyReason = ref(''); // 申请理由
const attachment = ref(''); // 附件
const userStore = useUserInfo(); // 当前登录用户（取手写体签名 signature）

// ── 计算属性 ──
const drawerTitle = computed(() => (props.define?.displayName ? `发起：${props.define.displayName}` : props.title)); // 抽屉标题
// 设计器生成的表单 rule：由流程首任务节点的 formKey（即表单设计 id）调 /api/formDesign/content 获取；
// 该接口返回表单 JSON 字符串，解析后即为 form-create 的 rule 数组。因需异步请求，用 ref 而非 computed。
const designedRule = ref<any[]>([]);
async function loadFormRule(key?: string | number | null): Promise<any[]> {
	// @form-create/designer 在模块加载时会执行 formCreate.component('FcEditor'|'FcTitle'|'TableForm'|'FcTable', …)
	// 注册设计器内置组件（富文本框 fcEditor 就来自它）。这里先触发注册再渲染 rule，
	// 否则报 "Failed to resolve component: fcEditor"。按需动态引入，避免发起页首屏加载设计器整包。
	await import('@form-create/designer');
	const res = await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignContentGet(Number(key));
	const content = res.data?.result; // 字符串形式的表单 rule JSON
	if (!content) return [];
	const parsed = JSON.parse(content);
	console.log('🚀 ~ loadFormRule ~ parsed:', parsed);
	return Array.isArray(parsed) ? parsed : [];
}
// form-create 的 props 类型与常规组件不同（要求整体满足 FormRule[] & Record），这里用 any 收敛后整体 v-bind
const designedBind = computed<any>(() => ({ rule: designedRule.value, option: designedOption }));

// ── 数据加载：监听 visible，打开时重置并拉取流程定义图 ──
watch(
	() => props.visible,
	async (v) => {
		if (!v) return; // 抽屉关闭时不加载
		// 重置所有状态，避免上一流程残留
		formData.value = {};
		formKey.value = '';
		designedRule.value = [];
		graph.value = null;
		activeKey.value = 'form';
		ccActors.value = [];
		nextOperators.value = [];
		applyReason.value = '';
		attachment.value = '';
		if (!props.define) return; // 没有流程定义则无需加载
		loading.value = true;
		try {
			// 优先用传入的 jsonObject；没有则按 defineId 拉详情补全
			let g = props.define.jsonObject;
			const defineId = props.define.processDefineId || props.define.id;
			if (!g && defineId) {
				// 流程定义详情：改用项目标准接口（api/jeeflowDefine/detail；载荷在 res.data.result）
				const d = await getAPI(JeeflowDefineApi)
					.apiJeeflowDefineDetailPost({ id: defineId })
					.then((res) => (res.data?.result ?? {}) as unknown as DefineDetail);
				g = d.jsonObject;
			}
			graph.value = g || null;
			formKey.value = firstTaskFormKey(g); // 首任务节点 formKey = 表单设计 id
			designedRule.value = await loadFormRule(formKey.value); // 拉取表单 rule
		} finally {
			loading.value = false;
		}
	},
	{ deep: true } // 初始即执行（抽屉可能首次就打开）
);

// 表单校验：form-create 的 fApi.validate() 返回 Promise（成功 resolve(true)，失败 reject）
// 注意：组件实例上没有 validate，表单 API 挂在实例的 fapi 上（见 @form-create/core 的 setup 返回值）
async function validateForm(): Promise<string | null> {
	const inst: any = formRef.value;
	if (!inst) return null;
	const fapi = inst.fapi ?? (typeof inst.validate === 'function' ? inst : null);
	if (!fapi) return null;
	try {
		await fapi.validate();
		return null;
	} catch (e) {
		console.warn('[发起流程] 表单校验未通过：', e);
		return '表单校验未通过，请检查必填项';
	}
}

// 发起流程：校验 → 收集 f_* 数据 → 调 startAndExecute
async function doStart() {
	const defineId = props.define?.processDefineId || props.define?.id;
	if (!defineId) {
		ElMessage.error('缺少 processDefineId，无法发起');
		return;
	}
	// 表单校验（两套组件统一入口）
	const err = await validateForm();
	if (err) {
		ElMessage.error(err);
		return;
	}
	starting.value = true;
	try {
		// 收集业务表单数据：统一加 f_ 前缀，附加抄送/下一处理人/申请理由/附件
		const form: Record<string, unknown> = {};
		for (const [k, v] of Object.entries(formData.value)) {
			if (v === '' || v == null) continue;
			form[k.startsWith('f_') ? k : `f_${k}`] = v;
		}
		if (formKey.value) form.formKey = formKey.value; // 同时带表单设计 id，便于后端按 formKey 关联/回查表单
		if (ccActors.value.length) form.f_ccActors = ccActors.value;
		if (nextOperators.value.length) form.f_nextNodeOperator = nextOperators.value;
		if (applyReason.value.trim()) form.f_applyReason = applyReason.value.trim();
		// if (attachment.value) form.f_attachment = attachment.value;
		if (attachment.value) form.attachments = [attachment.value];
		// 发起人手写体签名：取自当前登录用户，随发起入参一并提交（后端按阶段回写各节点 signature）
		if (userStore.userInfos?.signature) form.f_signature = userStore.userInfos.signature;
		// 发起并自动执行首节点：改用项目标准接口（api/jeeflowDefine/startAndExecute；载荷在 res.data.result）
		// 入参沿用 facade 生效字段 processDefineId（生成文档示例为 name/operator，与实际后端不一致，保持原契约）
		console.log(`🚀 ~ doStart ~ { processDefineId: defineId, ...form }:`, { processDefineId: defineId, ...form });
		// return;
		const r = await getAPI(JeeflowDefineApi)
			.apiJeeflowDefineStartAndExecutePost({ processDefineId: defineId, ...form })
			.then((res) => (res.data?.result ?? { processInstanceId: '' }) as unknown as { processInstanceId: string });
		ElMessage.success('发起成功');
		emit('started', r.processInstanceId); // 通知父组件刷新列表
		emit('update:visible', false); // 关闭抽屉
	} catch (e) {
		ElMessage.error((e as Error).message || '发起失败');
	} finally {
		starting.value = false;
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
	padding: 16px; /* 对齐原 JfDrawer .jf-drawer__body 的 16px */
	padding-top: 0;
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
	overflow: auto;
}
.el-drawer__body.jf-approve-drawer-body--fill > .el-tabs > .el-tabs__content > .el-tab-pane {
	height: 100%;
}
</style>
