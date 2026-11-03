<template>
	<div class="sys-jobCluster-container">
		<el-drawer v-model="state.isVisible" title="作业集群" size="65%">
			<vxe-grid v-bind="gridOptions" :data="state.jobClusterList" :loading="state.loading">
			<template #status="{ row }">
				<el-tag v-if="row.status == 0"> 宕机 </el-tag>
				<el-tag v-if="row.status == 1"> 工作中 </el-tag>
				<el-tag v-if="row.status == 2"> 等待被唤醒 </el-tag>
			</template>
		</vxe-grid>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup name="sysJobCluster">
import { onMounted, reactive } from 'vue';
import { VxeGridProps } from 'vxe-table';

import { getAPI } from '/@/utils/axios-utils';
import { SysJobApi } from '/@/api-services/system/api';
import { SysJobCluster } from '/@/api-services/system/models';

// 表格参数配置
const gridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 65 },
		{ field: 'clusterId', title: '集群编号', minWidth: 200, showOverflow: 'tooltip' },
		{
			field: 'status',
			title: '状态',
			width: 110,
			slots: { default: 'status' },
		},
		{ field: 'description', title: '描述', minWidth: 150, showOverflow: 'tooltip' },
		{ field: 'updatedTime', title: '更新时间', width: 160, showOverflow: 'tooltip' },
	],
});

const state = reactive({
	loading: false,
	isVisible: false,
	jobClusterList: [] as Array<SysJobCluster>,
});

// 页面初始化
onMounted(async () => {
	handleQuery();
});

// 查询操作
const handleQuery = async () => {
	state.loading = true;
	var res = await getAPI(SysJobApi).apiSysJobJobClusterListGet();
	state.jobClusterList = res.data.result ?? [];
	state.loading = false;
};

// 打开页面
const openDrawer = () => {
	state.isVisible = true;
};

// 导出对象
defineExpose({ openDrawer });
</script>
