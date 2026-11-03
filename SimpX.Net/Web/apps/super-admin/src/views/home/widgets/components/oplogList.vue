<template>
	<el-card class="log-card" shadow="hover" :body-style="{ padding: '0' }">
		<template #header>
			<div class="card-header">
				<el-icon><ele-Document /></el-icon>
				<span>操作记录</span>
				<el-tag size="small" type="info" effect="plain" class="header-count"> 共 {{ logList.length }} 条 </el-tag>
			</div>
		</template>

		<!-- 时间线 -->
		<el-timeline class="log-timeline" v-if="logList.length > 0">
			<el-timeline-item v-for="(item, index) in logList" :key="index" :color="getStatusColor(item.status)" size="normal" hide-timestamp>
				<div class="log-row">
					<!-- 时间 -->
					<el-text class="time">{{ formatTime(item.logDateTime) }}</el-text>

					<!-- 操作描述 -->
					<el-text class="desc" truncated>{{ item.displayTitle }}</el-text>

					<!-- 耗时 -->
					<el-text class="elapsed" :class="getElapsedClass(item.elapsed)"> {{ item.elapsed }}ms </el-text>

					<!-- 操作类型 -->
					<el-tag class="action-tag" :type="getMethodType(item.httpMethod)" effect="light" size="small">
						{{ item.httpMethod }}
					</el-tag>
				</div>
			</el-timeline-item>
		</el-timeline>

		<!-- 空数据 -->
		<div v-else class="w-empty">
			<span class="w-empty-ico"><el-icon><ele-Document /></el-icon></span>
			<span class="w-empty-text">暂无操作记录</span>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '操作记录',
	icon: 'ele-Document',
	description: '操作记录',
};
</script>

<!-- 操作记录 -->
<script setup lang="ts" name="oplogList">
import { ref, onMounted } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogOpApi } from '/@/api-services/system/api';
import { getCurrentMonthRange } from '/@/utils/format';

onMounted(() => {
	fetchLogData();
});

const logList = ref<any>([]);

/**
 * 从接口获取操作记录数据
 */
const fetchLogData = async () => {
	const monthRange = getCurrentMonthRange();
	const params = {
		startTime: monthRange.startTime as any,
		endTime: monthRange.endTime as any,
		page: 1,
		pageSize: 50,
	};
	const res: any = await getAPI(SysLogOpApi).apiSysLogOpPagePost(params);
	logList.value = res.data.result.items || [];
};

/**
 * 根据状态返回时间线节点颜色
 */
const getStatusColor = (status: string) => {
	if (status === '200') return 'var(--el-color-primary)';
	if (status === '304') return 'var(--el-color-warning)';
	return 'var(--el-color-danger)';
};

/**
 * 根据 HTTP 方法返回标签类型
 */
const getMethodType = (method: string) => {
	if (method === 'GET') return 'success';
	if (method === 'POST') return 'warning';
	if (method === 'DELETE') return 'danger';
	return 'info';
};

/**
 * 根据耗时返回颜色类名，用于性能预警
 */
const getElapsedClass = (elapsed: number) => {
	if (!elapsed) return '';
	if (elapsed < 100) return 'fast';
	if (elapsed < 500) return 'normal';
	return 'slow';
};

/**
 * 格式化时间，仅显示 MM-DD HH:mm:ss（节省横向空间）
 */
const formatTime = (time: string) => {
	if (!time) return '';
	// 若格式为 'YYYY-MM-DD HH:mm:ss'，截取月-日 时:分:秒
	return time.length >= 19 ? time.substring(5) : time;
};
</script>

<style lang="scss" scoped>
.card-header {
	display: flex;
	align-items: center;
	gap: 8px;

	.header-count {
		margin-left: auto;
		font-weight: normal;
	}
}

.log-timeline {
	padding: 12px 14px 12px 8px;
	margin: 0;
	height: var(--next-widget-height, 300px);
	overflow-y: auto;
	box-sizing: border-box;

	/* 自定义滚动条 */
	&::-webkit-scrollbar {
		width: 6px;
	}

	&::-webkit-scrollbar-thumb {
		background-color: var(--el-border-color);
		border-radius: 3px;

		&:hover {
			background-color: var(--el-border-color-darker);
		}
	}

	:deep(.el-timeline-item) {
		padding-bottom: 14px;

		&:last-child {
			padding-bottom: 0;
		}
	}

	:deep(.el-timeline-item__tail) {
		border-left: 1px solid var(--el-border-color-lighter);
	}

	:deep(.el-timeline-item__node) {
		box-shadow: 0 0 0 3px var(--el-bg-color);
	}
}

/* 单行日志内容布局 */
.log-row {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 4px 6px;
	border-radius: 6px;
	transition: background-color 0.2s;

	&:hover {
		background-color: var(--el-fill-color-light);
	}
}

.time {
	width: 110px;
	flex-shrink: 0;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);
}

.desc {
	flex: 1;
	min-width: 0;
	color: var(--el-text-color-primary);
}

.elapsed {
	width: 60px;
	flex-shrink: 0;
	text-align: right;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-secondary);

	&.fast {
		color: var(--el-color-success);
	}

	&.normal {
		color: var(--el-color-warning);
	}

	&.slow {
		color: var(--el-color-danger);
		font-weight: 600;
	}
}

.action-tag {
	flex-shrink: 0;
	width: 58px;
	text-align: center;
	font-weight: 500;
}
</style>
