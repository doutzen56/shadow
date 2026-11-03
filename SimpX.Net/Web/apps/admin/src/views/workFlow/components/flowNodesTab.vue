<template>
	<el-skeleton :loading="loading" animated>
		<template #default>
			<FlowNodeTable v-if="data" :data="data" :loading="loading" />
			<el-empty v-else description="暂无节点数据" />
		</template>
	</el-skeleton>
</template>

<script setup lang="ts" name="FlowNodesTab">
import FlowNodeTable from './FlowNodeTable.vue';

defineProps<{ data?: any; loading?: boolean }>();
</script>
