<template>
	<div class="workflow-form-render">
		<div v-if="loading" class="form-loading">
			<el-skeleton :rows="5" animated />
		</div>
		<template v-else>
			<FcRenderer ref="rendererRef" :schema="schemaJson" :data="normalizedData" :disabled="disabled" />
			<div v-if="showMeta && schemaJson" class="form-meta" style="margin-top: 10px">
				<el-tag v-if="formVersion" type="info">表单版本：v{{ formVersion }}</el-tag>
				<el-tag v-if="submitTime" type="info" style="margin-left: 8px">提交时间：{{ formatDateTimeEmpty(submitTime) }}</el-tag>
			</div>
		</template>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed } from 'vue';
import { formatDateTime } from '/@/utils/format';
import FcRenderer from '/@/components/formCreate/FcRenderer.vue';

interface Props {
	/** 表单设计（form-create 形态 { rule, options }），字符串或已解析对象均可 */
	schemaJson?: string | null | Record<string, any>;
	/** 表单数据 JSON 字符串 或 对象 */
	formData?: string | null | Record<string, any>;
	/** 是否禁用（只读） */
	disabled?: boolean;
	/** 是否显示元信息（版本、提交时间等） */
	showMeta?: boolean;
	/** 表单版本 */
	formVersion?: number | string;
	/** 提交时间 */
	submitTime?: string;
	/** 是否加载中 */
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	schemaJson: null,
	formData: null,
	disabled: true,
	showMeta: true,
	formVersion: undefined,
	submitTime: undefined,
	loading: false,
});

const rendererRef = ref();

// 规范化表单数据：字符串 JSON 或对象统一为对象；form-create 的 <form-create> 直接吃对象
const normalizedData = computed(() => {
	const v = props.formData;
	if (!v) return {};
	if (typeof v === 'string') {
		try {
			const p = JSON.parse(v);
			return p && typeof p === 'object' ? p : {};
		} catch {
			return {};
		}
	}
	return v && typeof v === 'object' ? v : {};
});

// 暴露给父页面：发起流程页通过此接口获取表单数据
const getFormData = (): Record<string, any> => rendererRef.value?.getFormData?.() ?? normalizedData.value;
const setFormData = (data: Record<string, any>) => rendererRef.value?.setFormData?.(data);
const resetForm = () => rendererRef.value?.resetForm?.();

defineExpose({ getFormData, setFormData, resetForm });

const formatDateTimeEmpty = (dateTime?: string) => formatDateTime(dateTime, 'YYYY-mm-dd HH:MM:SS', '');
</script>

<style lang="scss" scoped>
.workflow-form-render {
	.form-loading {
		padding: 20px;
	}

	.form-meta {
		display: flex;
		align-items: center;
	}
}
</style>
