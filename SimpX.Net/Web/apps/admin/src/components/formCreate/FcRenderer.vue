<template>
	<div class="fc-renderer">
		<template v-if="ready">
			<!-- form-create 已在 main.ts 全局注册（app.use(FcDesigner.formCreate)），渲染组件即 <form-create> -->
			<form-create ref="fApi" v-model="formData" :rule="rule" :option="effectiveOption" @change="onChange" />
		</template>
		<el-empty v-else description="该流程未绑定表单" />
	</div>
</template>

<script lang="ts" setup name="FcRenderer">
import { ref, computed, watch } from 'vue';

/**
 * 表单渲染公共组件：封装 @form-create/element-ui 的 <form-create>。
 * - schema 支持字符串 / 对象；form-create 形态 { rule, options } 或 { form, components }；也兼容 rule 数组。
 * - 通过 v-model 实时持有表单数据，getFormData() 同步返回；resetForm()/validate() 转发渲染器 API。
 * - 默认隐藏 form-create 自带的提交/重置按钮（由业务页自行控制），并可在只读态禁用全部字段。
 */
const props = withDefaults(
	defineProps<{
		/** 表单设计 JSON（form-create 形态：{ rule, options } 或 { form, components }；也接受 rule 数组） */
		schema?: string | null | { rule?: any[]; options?: any; components?: any[]; form?: any };
		/** 预填充的表单数据（字段名 => 值） */
		data?: Record<string, any> | null;
		/** 是否只读（禁用所有字段） */
		disabled?: boolean;
		/** 是否显示 form-create 自带提交/重置按钮（默认隐藏） */
		showActions?: boolean;
	}>(),
	{ schema: null, data: null, disabled: false, showActions: false }
);

const emit = defineEmits<{ (e: 'change', data: any): void }>();

const fApi = ref<any>(null);
const formData = ref<Record<string, any>>({});

// 解析 form-create JSON
const parsed = computed(() => {
	try {
		const raw = typeof props.schema === 'string' ? (props.schema ? JSON.parse(props.schema) : null) : props.schema;
		if (!raw || typeof raw !== 'object') return null;
		if (Array.isArray(raw.rule) || raw.options) return { rule: raw.rule ?? [], options: raw.options ?? {} };
		if (Array.isArray(raw.components)) return { rule: raw.components, options: raw.form ?? {} };
		if (Array.isArray(raw)) return { rule: raw, options: {} };
		return null;
	} catch {
		return null;
	}
});

const rule = computed(() => parsed.value?.rule ?? []);
const ready = computed(() => rule.value.length > 0);

// 隐藏自带按钮 + 按需禁用表单
const effectiveOption = computed(() => {
	const opt: any = { ...(parsed.value?.options ?? {}) };
	if (!props.showActions) {
		opt.submitBtn = false;
		opt.resetBtn = false;
	}
	if (props.disabled) {
		opt.form = { ...(opt.form ?? {}), disabled: true };
	}
	return opt;
});

// 预填充数据：挂载后通过渲染器 API 写入，保证业务数据回填（如发起流程的 loadBusinessData）实时生效
watch(
	() => props.data,
	(val) => {
		const next = val && typeof val === 'object' ? { ...val } : {};
		formData.value = next;
		const api: any = fApi.value;
		if (api && typeof api.setFormData === 'function') {
			try {
				api.setFormData(next);
			} catch {
				/* 忽略预填异常 */
			}
		}
	},
	{ immediate: true }
);

const onChange = (data: any) => {
	if (data && typeof data === 'object') formData.value = { ...data };
	emit('change', formData.value);
};

/** 同步返回当前表单数据（v-model 实时同步，无需等待异步） */
const getFormData = (): Record<string, any> => formData.value;

const setFormData = (data: Record<string, any>) => {
	formData.value = { ...data };
	const api: any = fApi.value;
	if (api && typeof api.setFormData === 'function') api.setFormData(data);
};

const resetForm = () => {
	const api: any = fApi.value;
	if (api && typeof api.resetFields === 'function') api.resetFields();
	else formData.value = {};
};

const validate = (): Promise<boolean> => {
	const api: any = fApi.value;
	if (api && typeof api.validate === 'function') return api.validate();
	return Promise.resolve(true);
};

defineExpose({ getFormData, setFormData, resetForm, validate });
</script>

<style lang="scss" scoped>
.fc-renderer {
	width: 100%;
}
</style>
