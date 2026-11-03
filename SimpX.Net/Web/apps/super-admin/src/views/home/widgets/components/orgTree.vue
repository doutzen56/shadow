<template>
	<el-card shadow="hover">
		<template #header>
			<div class="card-header">
				<el-icon><ele-OfficeBuilding /></el-icon>
				<span>组织架构</span>
			</div>
		</template>
		<div class="org-wrap">
			<OrgTree ref="orgTreeRef" :isAllOrg="true" />
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '组织架构',
	icon: 'ele-OfficeBuilding',
	description: '组织架构',
};
</script>

<!-- 组织架构 -->
<script setup lang="ts" name="orgTree">
import OrgTree from '/@/views/system/user/component/orgTree.vue';
</script>

<style lang="scss" scoped>
.card-header {
	display: flex;
	align-items: center;
	gap: 8px;
}

.org-wrap {
	height: var(--next-widget-height, 300px);
	overflow: auto;
}
</style>
