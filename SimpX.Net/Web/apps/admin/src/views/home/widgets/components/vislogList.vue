<template>
	<div>
		<el-card shadow="hover" :body-style="{ padding: '0' }">
			<template #header>
				<el-icon><ele-Monitor /></el-icon>
				<span>登录日志</span>
			</template>

			<!-- 列表容器：支持 loading -->
			<div v-loading="loading" class="log-list">
				<!-- 每条日志卡片 -->
				<div v-for="(item, index) in tableData" :key="index" class="log-item">
					<!-- 左侧：系统图标 -->
					<div class="log-icon">
						<el-icon :size="20"><ele-Monitor /></el-icon>
					</div>

					<!-- 右侧：内容区 -->
					<div class="log-content">
						<!-- 第一行：系统名 + 耗时标签 -->
						<div class="log-title">
							<span class="os-name">{{ item.os || '未知系统' }}</span>
							<el-tag v-if="item.elapsed" type="danger" effect="light" size="small"> 耗时：{{ item.elapsed }}ms </el-tag>
						</div>

						<!-- 第二行：详细信息 -->
						<div class="log-meta">
							<span>{{ item.browser || '未知' }}</span>
							<span class="divider">|</span>
							<span>{{ item.remoteIp || '0.0.0.0' }}</span>
							<span class="divider">|</span>
							<span>{{ item.location || '未知' }}</span>
							<span class="divider">|</span>
							<span>{{ item.account || '-' }}</span>
							<span class="divider">|</span>
							<span>{{ item.logDateTime }}</span>
						</div>
					</div>
				</div>

				<!-- 空数据提示 -->
				<div v-if="!loading && tableData.length === 0" class="w-empty">
					<span class="w-empty-ico"><el-icon><ele-Monitor /></el-icon></span>
					<span class="w-empty-text">暂无登录日志</span>
				</div>
			</div>
		</el-card>
	</div>
</template>

<script lang="ts">
export default {
	title: '登录日志',
	icon: 'ele-Promotion',
	description: '登录日志',
};
</script>

<script setup lang="ts" name="vislogList">
import { reactive, ref, onMounted } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysLogVisApi } from '/@/api-services/system/api';
import { getCurrentMonthRange } from '/@/utils/format';

const loading = ref(false);
const tableData = ref<any[]>([]);

const monthRange = getCurrentMonthRange();
const state = reactive({
	queryParams: {
		startTime: monthRange.startTime as any,
		endTime: monthRange.endTime as any,
		page: 1,
		pageSize: 50,
	},
});

// 拉取登录日志数据
const fetchLoginLogs = async () => {
	loading.value = true;
	try {
		const res = await getAPI(SysLogVisApi).apiSysLogVisPagePost(state.queryParams);
		tableData.value = res?.data?.result?.items ?? [];
	} finally {
		loading.value = false;
	}
};

onMounted(() => {
	fetchLoginLogs();
});
</script>

<style lang="scss" scoped>
/* 列表容器 */
.log-list {
	height: var(--next-widget-height, 300px);
	overflow-y: auto;
}

/* 单条日志卡片 */
.log-item {
	display: flex;
	align-items: flex-start;
	padding: 10px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	transition: background-color 0.2s;
}

.log-item:hover {
	background-color: var(--el-fill-color-light);
}

.log-item:last-child {
	border-bottom: none;
}

/* 左侧图标 */
.log-icon {
	flex: 0 0 auto;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 36px;
	height: 36px;
	margin-right: 10px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
	border-radius: 10px;
}

/* 右侧内容 */
.log-content {
	flex: 1;
	min-width: 0;
}

/* 第一行：系统名 + 耗时 */
.log-title {
	display: flex;
	align-items: center;
	margin-bottom: 6px;
}

.os-name {
	font-size: 14px;
	font-weight: 600;
	margin-right: 10px;
}

/* 第二行：详细信息 */
.log-meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	line-height: 1.6;
	word-break: break-all;
}

.log-meta .divider {
	margin: 0 6px;
	color: var(--el-text-color-secondary);
}
</style>
