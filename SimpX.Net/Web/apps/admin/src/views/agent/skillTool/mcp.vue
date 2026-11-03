<template>
	<SkillToolIndex :fixed-type="3" />
</template>

<script lang="ts" setup name="agentSkillToolMcp">
/** MCP 菜单入口：复用 skillTool/index.vue 并固定 fixedType=3，后端按 input.Parameter=3 过滤类型 */
import SkillToolIndex from './index.vue';
</script>
