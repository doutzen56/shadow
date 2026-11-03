<template>
	<div class="jf-page">
		<el-card shadow="hover" class="jf-donelist-card">
			<template #header>
				<div class="jf-page-title">
					我的已办
					<el-input v-model="keyword" class="jf-page-search" placeholder="搜索流程名..." @keyup.enter="goSearch" />
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<template v-else>
				<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
					<el-table-column label="流程" min-width="120">
						<template #default="{ row }">{{ row.processDefineDisplayName || '-' }}</template>
					</el-table-column>
					<el-table-column label="任务" min-width="120">
						<template #default="{ row }"
							><strong>{{ row.displayName }}</strong></template
						>
					</el-table-column>
					<el-table-column label="标题" min-width="160">
						<template #default="{ row }">{{ rowTitle(row) }}</template>
					</el-table-column>
					<el-table-column label="发起人" min-width="100">
						<template #default="{ row }"
							><span class="jf-muted">{{ rowInitiator(row) }}</span></template
						>
					</el-table-column>
					<el-table-column label="状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="taskStateBadgeType(row.taskState)">{{ taskStateLabel(row.taskState) }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="完成时间" min-width="160">
						<template #default="{ row }"
							><span class="jf-muted">{{ fmtTime(row.finishTime || row.createTime, true) }}</span></template
						>
					</el-table-column>
					<el-table-column label="操作" min-width="160" fixed="right">
						<template #default="{ row }">
							<div class="jf-btn-row">
								<el-button type="primary" size="small" :icon="Edit" @click="openTask(row.id)">我的办理</el-button>
								<el-button size="small" plain :icon="View" @click="openDetail(row.processInstanceId)">详情</el-button>
							</div>
						</template>
					</el-table-column>
				</el-table>

				<!-- 分页 -->
				<div class="jf-pagination">
					<el-pagination layout="prev, pager, next, total" :current-page="pageNum" :page-size="pageSize" :total="recordCount" size="small" background @current-change="go" />
				</div>
			</template>
		</el-card>

		<!-- 详情抽屉（看轨迹） -->
		<InstanceDetailDrawer v-model:visible="detailVisible" :instance-id="detailInstanceId" />
		<!-- 我这次办理填过的表单与审批意见（对齐 vben5-wf：已办点开就是办理抽屉） -->
		<ApproveDrawer v-model:visible="taskVisible" :task-id="taskId" readonly />
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import JfBadge from '../core/ui/JfBadge.vue';
import InstanceDetailDrawer from '../core/drawers/InstanceDetailDrawer.vue';
import ApproveDrawer from '../core/drawers/ApproveDrawer.vue';
import '../core/style.css';
import { fmtTime, taskStateLabel, taskStateBadgeType, rowTitle, rowInitiator } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { TaskRow, PageResult } from '../core/types';
import { Edit, View } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowTaskApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfDonePage' });

const loading = ref(false);
const rows = ref<TaskRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);
const keyword = ref('');

const detailVisible = ref(false);
const detailInstanceId = ref<string | null>(null);
const taskVisible = ref(false);
const taskId = ref<string | null>(null);

function openTask(id: string) {
	taskId.value = id;
	taskVisible.value = true;
}

async function reload() {
	loading.value = true;
	try {
		// 已办列表：改用项目标准接口（api/jeeflowTask/doneList；载荷在 res.data.result）
		const r = await getAPI(JeeflowTaskApi)
			.apiJeeflowTaskDoneListPost({
				pageNum: pageNum.value,
				pageSize,
				...(keyword.value.trim() ? { m_LIKE_processDefineDisplayName: keyword.value.trim() } : {}),
			})
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<TaskRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		ElMessage.error((e as Error).message || '加载已办列表失败');
	} finally {
		loading.value = false;
	}
}

function go(p: number) {
	pageNum.value = p;
	reload();
}

function goSearch() {
	pageNum.value = 1;
	reload();
}

function openDetail(instanceId: string) {
	detailInstanceId.value = instanceId;
	detailVisible.value = true;
}

onMounted(reload);
</script>

<style scoped>
.jf-page-search {
	width: 220px;
	margin-left: auto;
	font-weight: normal;
}
.jf-donelist-card {
	height: 100%;
}
.jf-donelist-card :deep(.el-card__body) {
	height: 100%;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding-bottom: 0;
}
</style>
