<template>
	<JfUserPicker v-model="ids" scene="cc" placeholder="搜索并选择参与人" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import JfUserPicker from './JfUserPicker.vue';

const props = withDefaults(
	defineProps<{
		/** 流程定义中 assignee 字段：逗号分隔的 userId 串；空串表示未指定 */
		modelValue?: string;
	}>(),
	{ modelValue: '' }
);

const emit = defineEmits<{ 'update:modelValue': [v: string] }>();

// 桥接：组件内部多选数组 <-> 设计器存储的逗号分隔串
const ids = computed<string[]>({
	get: () => (props.modelValue ? props.modelValue.split(',').filter(Boolean) : []),
	set: (v) => emit('update:modelValue', v.join(',')),
});
</script>
