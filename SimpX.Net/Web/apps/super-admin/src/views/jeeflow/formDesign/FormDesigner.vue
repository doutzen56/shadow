<template>
	<el-dialog v-model="visible" :fullscreen="fullscreen" width="92%" top="4vh" append-to-body :close-on-click-modal="false" @open="onOpen">
		<template #header>
			<span class="jf-designer-title"><el-icon><ele-MagicStick /></el-icon>表单设计器</span>
		</template>
		<div class="jf-fc-designer">
			<!-- FcDesigner 仅在弹窗打开时挂载，避免初始渲染开销；height 控制设计区高度 -->
			<FcDesigner v-if="visible" ref="designerRef" :height="fcHeight" :config="designerConfig" />
		</div>
		<template #footer>
			<el-button @click="visible = false">取消</el-button>
			<el-button type="primary" :icon="Select" @click="handleSave">保存表单</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import { Select } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import FcDesigner from '@form-create/designer';
import type { Rule, Options } from '@form-create/element-ui';

const props = withDefaults(
	defineProps<{
		modelValue: boolean;
		// 是否全屏展示（设计器区域较大，默认全屏）
		fullscreen?: boolean;
		// 已有的表单 JSON，打开时回显到设计器（可为空）
		content?: string | null;
	}>(),
	{
		fullscreen: true,
		content: '',
	}
);

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	// 保存时回传设计结果：渲染规则、全局配置、JSON 字符串
	save: [payload: { rule: Rule[]; option: Options; json: string }];
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (v) => emit('update:modelValue', v),
});

const designerRef = ref<any>(null);
const fcHeight = ref(560);
// 工具栏配置：showSaveBtn 默认 false（不开工具栏保存按钮，保存走弹窗底部「保存表单」）；保留 JSON/录入开关
// fieldReadonly：设计器内部判定是 `config.fieldReadonly !== false`，不传即为 true（「字段 ID」只读），
// 这里显式关掉，允许手工把随机 field（如 F1gnmvt56o4pabc）改成有意义的字段名（如 title）
const designerConfig = { showJsonPreview: true, showInputData: true, fieldReadonly: false };

async function onOpen() {
	// 弹窗高度自适应：留出头部/底部余量，最小 480
	fcHeight.value = Math.max(480, window.innerHeight - 170);
	// 回显已有表单内容：需等 FcDesigner 挂载完成后再写入
	const json = (props.content ?? '').trim();
	if (!json) return;
	await nextTick();
	try {
		const parsed = typeof json === 'string' ? JSON.parse(json) : json;
		const rules = Array.isArray(parsed) ? parsed : parsed?.rule;
		if (rules) designerRef.value?.setRule?.(rules);
		if (parsed && !Array.isArray(parsed) && parsed.option) designerRef.value?.setOption?.(parsed.option);
	} catch {
		ElMessage.warning('已有表单内容不是有效 JSON，已忽略，保存后将覆盖');
	}
}

function handleSave() {
	const designer = designerRef.value;
	if (!designer) return;
	const rule: Rule[] = designer.getRule?.() ?? [];
	const option: Options = designer.getOption?.() ?? {};
	const json: string = designer.getJson?.() ?? '';
	emit('save', { rule, option, json });
	visible.value = false;
}
</script>

<style scoped>
.jf-fc-designer {
	width: 100%;
}

.jf-designer-title {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 16px;
	font-weight: 600;
	color: #fff;
}
</style>

<style>
.el-overlay .el-overlay-dialog .el-dialog._fd-preview-dialog {
	position: relative !important;
}
.el-dialog._fd-preview-dialog > .el-dialog__header {
	position: absolute !important;
	right: 10px;
	top: 6px;
	left: auto;
	background: transparent !important;
	padding: 0 !important;
	border-radius: 0 !important;
}
.el-dialog._fd-preview-dialog .el-dialog__headerbtn .el-dialog__close {
	color: var(--el-text-color-regular, #606266) !important;
}
.el-overlay .el-overlay-dialog .el-dialog._fd-preview-dialog .el-dialog__body {
	padding-top: 0 !important;
}
</style>
