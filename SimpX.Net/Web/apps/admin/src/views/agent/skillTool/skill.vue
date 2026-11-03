<template>
	<SkillToolIndex :fixed-type="2" />
</template>

<script lang="ts" setup name="agentSkillToolSkill">
/** 技能（Skill）菜单入口：复用 skillTool/index.vue 并固定 fixedType=2，后端按 input.Parameter=2 过滤类型 */
import SkillToolIndex from './index.vue';
</script>
