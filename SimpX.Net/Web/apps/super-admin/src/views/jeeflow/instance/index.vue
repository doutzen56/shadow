<template>
	<div class="jf-page">
		<el-card shadow="hover" class="jf-instance-card">
			<template #header>
				<div class="jf-page-title">
					我发起的流程
					<el-input v-model="keyword" class="jf-page-search" placeholder="搜索流程名..." @keyup.enter="goSearch" />
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<template v-else>
				<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
					<el-table-column label="ID" min-width="160">
						<template #default="{ row }"
							><span class="jf-muted">{{ row.id }}</span></template
						>
					</el-table-column>
					<el-table-column label="流程" min-width="160">
						<template #default="{ row }">{{ row.displayName || row.processDefineDisplayName }}</template>
					</el-table-column>
					<el-table-column label="标题" min-width="160">
						<template #default="{ row }">{{ rowTitle(row) }}</template>
					</el-table-column>
					<el-table-column label="状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="stateBadgeType(row.state)">{{ stateLabel(row.state) }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="时间" min-width="160">
						<template #default="{ row }"
							><span class="jf-muted">{{ fmtTime(row.createTime, true) }}</span></template
						>
					</el-table-column>
					<el-table-column label="操作" min-width="180" fixed="right">
						<template #default="{ row }">
							<div class="jf-btn-row">
								<el-button size="small" plain :icon="View" @click="openDetail(row.id)">详情</el-button>
								<el-button v-auth="'instance/revoke'" v-if="row.state === 10" size="small" plain :icon="RefreshLeft" @click="withdraw(row)">撤回</el-button>
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

		<InstanceDetailDrawer v-model:visible="detailVisible" :instance-id="detailInstanceId" @changed="reload" />
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import JfBadge from '../core/ui/JfBadge.vue';
import InstanceDetailDrawer from '../core/drawers/InstanceDetailDrawer.vue';

import '../core/style.css';
import { fmtTime, stateLabel, stateBadgeType, rowTitle } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { InstanceRow, PageResult } from '../core/types';
import { View, RefreshLeft } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowInstanceApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfMyInstancePage' });

const loading = ref(false);
const rows = ref<InstanceRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);
const keyword = ref('');

const detailVisible = ref(false);
const detailInstanceId = ref<string | null>(null);

async function reload() {
	loading.value = true;
	try {
		// 实例列表：改用项目标准接口（api/jeeflowInstance/page；载荷在 res.data.result）
		const r = await getAPI(JeeflowInstanceApi)
			.apiJeeflowInstancePagePost({
				pageNum: pageNum.value,
				pageSize,
				...(keyword.value.trim() ? { m_LIKE_processDefineDisplayName: keyword.value.trim() } : {}),
			})
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<InstanceRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		ElMessage.error((e as Error).message || '加载实例列表失败');
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

function openDetail(id: string) {
	detailInstanceId.value = id;
	detailVisible.value = true;
}

async function withdraw(row: InstanceRow) {
	if (!window.confirm('确认撤回该流程？')) return;
	try {
		// 撤回实例：改用项目标准接口（api/jeeflowInstance/withdraw）
		await getAPI(JeeflowInstanceApi).apiJeeflowInstanceWithdrawPost({ id: row.id });
		ElMessage.success('已撤回');
		reload();
	} catch (e) {
		// 撤回可能因实例已办结/已被办理而失败（负向路径），提示后端原因
		ElMessage.error(`撤回失败：${(e as Error).message}`);
	}
}

onMounted(reload);
</script>

<style scoped>
.jf-page-search {
	width: 220px;
	margin-left: auto;
	font-weight: normal;
}
.jf-instance-card {
	height: 100%;
}
.jf-instance-card :deep(.el-card__body) {
	height: 100%;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding-bottom: 0;
}
</style>
