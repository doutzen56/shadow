<template>
	<div class="jf-page">
		<!-- 我的抄送：关键字搜索走 m_LIKE 匹配流程名 -->
		<el-card shadow="hover" class="jf-cclist-card">
			<template #header>
				<div class="jf-page-title">
					我的抄送
					<el-input v-model="keyword" class="jf-page-search" placeholder="搜索流程名..." @keyup.enter="goSearch" />
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<template v-else>
				<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
					<el-table-column label="流程" min-width="160">
						<template #default="{ row }">{{ row.displayName || row.processDefineDisplayName }}</template>
					</el-table-column>
					<el-table-column label="标题" min-width="160">
						<template #default="{ row }">{{ rowTitle(row) }}</template>
					</el-table-column>
					<el-table-column label="实例状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="stateBadgeType(row.state)">{{ stateLabel(row.state) }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="阅读状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="readIds.has(row.id) ? 'done' : 'doing'">{{ readIds.has(row.id) ? '已读' : '未读' }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="发起人" min-width="120">
						<template #default="{ row }">{{ rowInitiator(row) }}</template>
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
								<el-button v-auth="'cclist/cc'" size="small" plain :icon="Promotion" @click="openCc(row.id)">抄送他人</el-button>
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

		<!-- 手动抄送（行级：针对具体实例）；body-class 还原 JfDrawer 抽屉体的 padding -->
		<el-drawer v-model="ccVisible" body-class="jf-approve-drawer-body" title="手动抄送" size="480px">
			<div class="jf-form-item">
				<label class="jf-form-label">抄送人</label>
				<JfUserPicker v-model="ccIds" scene="cc" placeholder="搜索姓名/工号" />
			</div>
			<div class="jf-drawer-actions">
				<el-button plain :icon="Close" @click="ccVisible = false">取消</el-button>
				<el-button type="primary" :icon="Select" :disabled="ccSaving || !ccIds.length" @click="doCc">确定</el-button>
			</div>
		</el-drawer>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import JfBadge from '../core/ui/JfBadge.vue';
import JfUserPicker from '../core/ui/JfUserPicker.vue';
import InstanceDetailDrawer from '../core/drawers/InstanceDetailDrawer.vue';

import '../core/style.css';
import { fmtTime, stateLabel, stateBadgeType, rowTitle, rowInitiator } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { InstanceRow, PageResult } from '../core/types';
import { View, Promotion, Close, Select } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowInstanceApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfCcListPage' });

const loading = ref(false);
const rows = ref<InstanceRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);
const keyword = ref('');
/** 已读实例集合（ccList 出口不带 cc.state：打开详情标已读后本地维护） */
const readIds = reactive(new Set<string>());

const detailVisible = ref(false);
const detailInstanceId = ref<string | null>(null);

const ccVisible = ref(false);
const ccTargetId = ref<string | null>(null);
const ccIds = ref<string[]>([]);
const ccSaving = ref(false);

async function reload() {
	loading.value = true;
	try {
		// 抄送列表：改用项目标准接口（api/jeeflowInstance/ccList；载荷在 res.data.result）
		const r = await getAPI(JeeflowInstanceApi)
			.apiJeeflowInstanceCcListPost({
				pageNum: pageNum.value,
				pageSize,
				...(keyword.value.trim() ? { m_LIKE_processDefineDisplayName: keyword.value.trim() } : {}),
			})
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<InstanceRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		ElMessage.error((e as Error).message || '加载抄送列表失败');
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

/** 打开详情：先 updateCCStatus 标已读（后端落库）再展示 */
async function openDetail(id: string) {
	try {
		// 标已读：改用项目标准接口（api/jeeflowInstance/updateCcStatus；文档入参为 { id }）
		await getAPI(JeeflowInstanceApi).apiJeeflowInstanceUpdateCcStatusPost({ id });
		readIds.add(id);
	} catch {
		/* 标已读失败不阻塞查看 */
	}
	detailInstanceId.value = id;
	detailVisible.value = true;
}

function openCc(instanceId: string) {
	ccTargetId.value = instanceId;
	ccIds.value = [];
	ccVisible.value = true;
}

async function doCc() {
	if (!ccTargetId.value || !ccIds.value.length) return;
	ccSaving.value = true;
	try {
		// 创建抄送：改用项目标准接口（api/jeeflowInstance/createCc；文档入参为 { id, userIds }）
		await getAPI(JeeflowInstanceApi).apiJeeflowInstanceCreateCcPost({ id: ccTargetId.value, userIds: ccIds.value });
		ElMessage.success('抄送成功');
		ccVisible.value = false;
		ccIds.value = [];
	} catch (e) {
		ElMessage.error((e as Error).message || '抄送失败');
	} finally {
		ccSaving.value = false;
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
.jf-cclist-card {
	height: 100%;
}
.jf-cclist-card :deep(.el-card__body) {
	height: 100%;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding-bottom: 0;
}
</style>
