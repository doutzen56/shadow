<template>
	<div class="visMap-container">
		<!-- <el-card class="visMap-container" shadow="hover" :body-style="{ padding: '0' }">
			<template #header>
				<div>
					<el-icon style="display: inline; vertical-align: middle"> <ele-MapLocation /> </el-icon>
					<span> 访问地图 </span>
				</div>
			</template> -->

		<visMap :use-dialog="false" style="height: 300px !important" />
		<!-- </el-card> -->
	</div>
</template>

<script lang="ts">
export default {
	title: '访问地图',
	icon: 'ele-Promotion',
	description: '访问地图',
};
</script>

<!-- 访问地图 -->
<script lang="ts" setup name="vislogMap">
import { defineAsyncComponent } from 'vue';

const VisMap = defineAsyncComponent(() => import('/@/views/system/log/logvis/component/visMap.vue'));
</script>

<style scoped lang="scss">
.visMap-container {
	border-radius: 10px;
	overflow: hidden;
}
</style>
