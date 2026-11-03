<template>
	<div class="fc-designer-wrapper">
		<!-- 显式绑定设计器核心组件，不用 <fc-designer> 标签：
		     模板里写 <fc-designer> 时 Vue 的 resolveComponent 会先拿当前组件的 name 做「隐式自引用」判断，
		     本组件 name 与设计器全局名 FcDesigner 归一化后相同，会被解析成自己造成无限自挂载（Maximum call stack size exceeded） -->
		<FcDesignerCore ref="designerRef" />
	</div>
</template>

<script lang="ts" setup name="FcDesignerWrap">
import { ref, markRaw, onMounted, watch } from 'vue';
import FcDesignerCoreComponent from '@form-create/designer';

/**
 * 表单设计器公共组件：封装 @form-create/designer。
 * 对外只暴露与引擎无关的三个方法（getFormJson / setFormJson / clear），
 * 持久化格式统一为 form-create 原生形态 { rule, options }。
 */
const FcDesignerCore = markRaw(FcDesignerCoreComponent);

/** 表单 JSON：form-create 原生形态，rule 为组件规则数组，options 为表单配置 */
export interface FcFormJson {
	rule: any[];
	options: Record<string, any>;
}

const props = defineProps<{
	/** 待载入的表单 JSON（对象或 { rule, options } 的 JSON 字符串），为空则保持空白设计器 */
	formJson?: FcFormJson | string | null;
}>();

const designerRef = ref<any>(null);

// 设计器内部会把 rule 挂到自己的响应式状态上，这里一律先转成纯数据，
// 既切断父组件 reactive() 的代理，也避免设计器改动反向污染父组件状态
const toPlain = (value: any): any => {
	if (value === null || value === undefined) return value;
	if (typeof value === 'string') return value;
	try {
		return JSON.parse(JSON.stringify(value));
	} catch {
		return value;
	}
};

// 兼容 { rule, options } / rule 数组 / JSON 字符串三种入参
const normalize = (json: FcFormJson | string | null | undefined): FcFormJson => {
	let raw: any = json;
	if (typeof raw === 'string') {
		try {
			raw = JSON.parse(raw);
		} catch {
			raw = null;
		}
	}
	if (Array.isArray(raw)) return { rule: raw, options: {} };
	return { rule: raw?.rule ?? [], options: raw?.options ?? {} };
};

const applyFormJson = (json: FcFormJson | string | null | undefined) => {
	// 未传设计稿（如系统表单设计页直接使用空白设计器）时不动设计器自带的默认配置
	if (json === null || json === undefined) return;
	const designer = designerRef.value;
	if (!designer) return;
	const { rule, options } = normalize(json);
	designer.setRule(toPlain(rule) ?? []);
	designer.setOptions(toPlain(options) ?? {});
};

/** 读取当前设计稿，返回可直接入库的 { rule, options } */
const getFormJson = (): FcFormJson => {
	const designer = designerRef.value;
	if (!designer) return { rule: [], options: {} };
	return {
		rule: JSON.parse(designer.getJson()),
		options: JSON.parse(designer.getOptionsJson()),
	};
};

/** 清空设计稿 */
const clear = () => applyFormJson({ rule: [], options: {} });

onMounted(() => applyFormJson(props.formJson));
watch(() => props.formJson, applyFormJson);

defineExpose({ getFormJson, setFormJson: applyFormJson, clear });
</script>

<style lang="scss" scoped>
.fc-designer-wrapper {
	width: 100%;
	height: 100%;
	min-height: 420px;
	background-color: var(--el-bg-color);
}
</style>
