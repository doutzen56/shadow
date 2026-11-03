<template>
	<div class="jf-page">
		<el-card shadow="hover" class="jf-define-card">
			<template #header>
				<div class="jf-page-title">
					流程定义
					<el-input v-model="keyword" class="jf-page-search" placeholder="搜索编码/显示名..." @keyup.enter="goSearch" />
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<template v-else>
				<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
					<el-table-column label="编码" min-width="120">
						<template #default="{ row }"
							><span class="jf-muted">{{ row.name }}</span></template
						>
					</el-table-column>
					<el-table-column label="显示名" min-width="160">
						<template #default="{ row }"
							><strong>{{ row.displayName }}</strong></template
						>
					</el-table-column>
					<el-table-column label="类型" min-width="100">
						<template #default="{ row }"
							><span class="jf-muted">{{ row.type }}</span></template
						>
					</el-table-column>
					<el-table-column label="版本" min-width="80">
						<template #default="{ row }"
							><span class="jf-muted">v{{ row.version }}</span></template
						>
					</el-table-column>
					<el-table-column label="状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="row.state === 1 ? 'done' : 'info'">{{ row.state === 1 ? '启用' : '停用' }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="更新时间" min-width="160">
						<template #default="{ row }"
							><span class="jf-muted">{{ fmtTime(row.updateTime || row.createTime, true) }}</span></template
						>
					</el-table-column>
					<el-table-column label="操作" min-width="200" fixed="right">
						<template #default="{ row }">
							<div class="jf-btn-row">
								<el-button size="small" type="primary" :icon="View" @click="openDetail(row)">详情</el-button>
								<el-button v-auth="'define/stop'" size="small" :icon="Switch" @click="toggle(row)">{{ row.state === 1 ? '停用' : '启用' }}</el-button>
								<el-button v-auth="'define/delete'" size="small" type="danger" :icon="Delete" @click="remove(row)">删除</el-button>
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

		<!-- 定义详情：基本信息 + 流程图弹性铺满（--fill 还原 JfDrawer 的 fill：内容铺满不滚动） -->
		<el-drawer v-model="detailVisible" body-class="jf-approve-drawer-body jf-approve-drawer-body--fill" :title="detail?.displayName || '流程详情'" size="820px">
			<div v-if="detail" class="jf-detail-body">
				<div class="jf-define-meta">
					<div class="jf-define-meta-item">
						<span class="jf-muted">编码</span><strong>{{ detail.name }}</strong>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">显示名</span><strong>{{ detail.displayName }}</strong>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">类型</span><span>{{ detail.type || '-' }}</span>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">版本</span><span>v{{ detail.version }}</span>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">状态</span>
						<JfBadge :type="detail.state === 1 ? 'done' : 'info'">{{ detail.state === 1 ? '启用' : '停用' }}</JfBadge>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">创建</span><span>{{ fmtTime(detail.createTime, true) }}{{ detail.createUser ? `（${detail.createUser}）` : '' }}</span>
					</div>
					<div class="jf-define-meta-item">
						<span class="jf-muted">更新</span><span>{{ fmtTime(detail.updateTime || detail.createTime, true) }}</span>
					</div>
				</div>
				<div v-if="detail.jsonObject" class="jf-detail-graph">
					<JfFlowViewer :graph-data="detail.jsonObject" />
				</div>
				<div v-else class="jf-empty">该定义无流程图（content 缺失）</div>
			</div>
		</el-drawer>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import JfBadge from '../core/ui/JfBadge.vue';
import JfFlowViewer from '../core/ui/JfFlowViewer.vue';

import '../core/style.css';
import { fmtTime } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { DefineRow, DefineDetail, PageResult } from '../core/types';
import { View, Switch, Delete } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowDefineApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfProcessDefinePage' });

const loading = ref(false);
const rows = ref<DefineRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);
const keyword = ref('');

const detailVisible = ref(false);
const detail = ref<DefineDetail | null>(null);

async function reload() {
	loading.value = true;
	try {
		// 流程定义列表：改用项目标准接口（api/jeeflowDefine/page；载荷在 res.data.result）
		const r = await getAPI(JeeflowDefineApi)
			.apiJeeflowDefinePagePost({
				pageNum: pageNum.value,
				pageSize,
				orderBy: 't.update_time desc',
				...(keyword.value.trim() ? { m_LIKE_displayName: keyword.value.trim() } : {}),
			})
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<DefineRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		ElMessage.error(`加载失败：${(e as Error).message}`);
	} finally {
		loading.value = false;
	}
}

function goSearch() {
	pageNum.value = 1;
	reload();
}

function go(p: number) {
	pageNum.value = p;
	reload();
}

async function openDetail(d: DefineRow) {
	try {
		// 流程定义详情：改用项目标准接口（api/jeeflowDefine/detail；载荷在 res.data.result）
		detail.value = await getAPI(JeeflowDefineApi)
			.apiJeeflowDefineDetailPost({ id: d.id })
			.then((res) => (res.data?.result ?? null) as unknown as DefineDetail);
		detailVisible.value = true;
	} catch (e) {
		ElMessage.error(`打开详情失败：${(e as Error).message}`);
	}
}

async function toggle(d: DefineRow) {
	try {
		// 启用/停用：改用项目标准接口（api/jeeflowDefine/upAndDown；文档入参 { id, state }）
		await getAPI(JeeflowDefineApi).apiJeeflowDefineUpAndDownPost({ id: d.id, state: d.state === 1 ? 0 : 1 });
		ElMessage.success(d.state === 1 ? '已停用' : '已启用');
		reload();
	} catch (e) {
		ElMessage.error(`操作失败：${(e as Error).message}`);
	}
}

async function remove(d: DefineRow) {
	if (!window.confirm(`确认删除流程定义「${d.displayName}」？`)) return;
	try {
		// 删除：改用项目标准接口（api/jeeflowDefine/remove；文档入参 { ids }）
		await getAPI(JeeflowDefineApi).apiJeeflowDefineRemovePost({ ids: [d.id] });
		ElMessage.success('已删除');
		reload();
	} catch (e) {
		ElMessage.error(`删除失败：${(e as Error).message}`);
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
.jf-define-card {
	height: 100%;
}
.jf-define-card :deep(.el-card__body) {
	height: 100%;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding-bottom: 0;
}
.jf-define-meta {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
	gap: 10px 20px;
	padding: 14px 16px;
	background: #fafbfc;
	border: 1px solid var(--jf-border, #f0f0f0);
	border-radius: 8px;
}
.jf-define-meta-item {
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 13px;
}
.jf-define-meta-item > .jf-muted {
	min-width: 42px;
}
</style>
