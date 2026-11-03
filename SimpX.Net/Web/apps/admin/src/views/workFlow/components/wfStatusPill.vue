<template>
	<span class="wf-status" :class="`is-${tone}`">
		<i class="wf-status__dot"></i>{{ text }}
	</span>
</template>

<script setup lang="ts" name="WfStatusPill">
// 工作流状态胶囊：列表插槽与弹窗头部共用，配色见 styles/workflow.scss 的 .wf-status.is-*
defineProps<{ text: string; tone: string }>();
</script>
