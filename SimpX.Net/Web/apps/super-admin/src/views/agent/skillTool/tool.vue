<template>
	<SkillToolIndex :fixed-type="1" />
</template>

<script lang="ts" setup name="agentSkillToolTool">
/** 工具（Tool）菜单入口：复用 skillTool/index.vue 并固定 fixedType=1，后端按 input.Parameter=1 过滤类型 */
import SkillToolIndex from './index.vue';
</script>
