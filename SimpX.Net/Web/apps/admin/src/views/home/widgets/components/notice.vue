<template>
	<el-card class="notice-card" shadow="hover" :body-style="{ padding: '0' }">
		<template #header>
			<div class="card-header">
				<el-icon><ele-Message /></el-icon>
				<span>通知公告</span>
				<el-tag size="small" type="info" effect="plain" class="header-count"> 共 {{ noticeList.length }} 条 </el-tag>
			</div>
		</template>

		<!-- 消息列表 -->
		<div class="notice-list">
			<div v-if="noticeList.length === 0" class="w-empty">
				<span class="w-empty-ico"><el-icon><ele-Bell /></el-icon></span>
				<span class="w-empty-text">暂无通知公告</span>
			</div>
			<div v-for="(item, index) in noticeList" :key="index" class="notice-item">
				<!-- 左侧类型块：通知=橙、公告=主色 -->
				<div class="icon-wrapper" :class="item.sysNotice.type === 1 ? 'is-warning' : 'is-primary'">
					<span>{{ item.sysNotice.type === 1 ? '通知' : '公告' }}</span>
				</div>
				<!-- 中间内容 -->
				<div class="content">
					<div class="row-top">
						<span class="notice-title">{{ item.sysNotice.title }}</span>
						<div class="row-top-acts">
							<el-tag v-if="item.readStatus === 1" type="info" size="small">已读</el-tag>
							<el-tag v-else type="danger" size="small">未读</el-tag>
							<el-button icon="ele-InfoFilled" text type="primary" @click="handleView(item)"> </el-button>
						</div>
					</div>
					<div class="row-bottom">
						<span class="notice-desc">{{ removeHtml(item.sysNotice.content) }}</span>
						<span class="notice-time">{{ item.sysNotice.createTime }}</span>
					</div>
				</div>
			</div>
		</div>

		<!-- 消息详情弹窗 -->
		<NoticeDetailDialog ref="noticeDetailRef" />
	</el-card>
</template>

<script lang="ts">
export default {
	title: '通知公告',
	icon: 'ele-Bell',
	description: '通知公告',
};
</script>

<!-- 通知公告 -->
<script setup lang="ts" name="notice">
import { ref, onMounted } from 'vue';
import commonFunction from '/@/utils/commonFunction';

import NoticeDetailDialog from '/@/views/system/notice/component/noticeDetailDialog.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/system/api';
import { PageNoticeInput } from '/@/api-services/system/models';

const { removeHtml } = commonFunction();
const noticeDetailRef = ref();

onMounted(async () => {
	await getNoticeList();
});

// 消息列表数据
const noticeList = ref<any>([]);

const getNoticeList = async () => {
	const params: PageNoticeInput = {
		page: 1,
		pageSize: 50,
	};
	const res: any = await getAPI(SysNoticeApi).apiSysNoticePageReceivedPost(params);
	noticeList.value = res.data.result.items;
};

// 查看详情
const handleView = async (row: any) => {
	noticeDetailRef.value?.openDialog(row.sysNotice ?? {});
	row.readStatus = 1;
	await getAPI(SysNoticeApi).apiSysNoticeSetReadPost({ id: row.sysNotice.id });
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

/* 消息列表 */
.notice-list {
	display: flex;
	flex-direction: column;
	height: var(--next-widget-height, 300px);
	overflow-y: auto;
}

.notice-item {
	display: flex;
	align-items: center;
	gap: 10px;

	padding: 10px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&:last-child {
		border-bottom: none;
	}
	&:hover {
		background-color: var(--el-fill-color-light);
	}
}

/* 类型块：深色字 + 同色系浅底（全站装饰规范） */
.icon-wrapper {
	flex: 0 0 auto;
	width: 30px;
	height: 30px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	color: var(--tint, var(--el-color-primary));
	background: var(--el-color-primary-light-9);
	@supports (background: color-mix(in srgb, red, blue)) {
		background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);
	}

	&.is-warning {
		--tint: #f59e0b;
	}

	&.is-primary {
		--tint: var(--el-color-primary);
	}
}

.content {
	flex: 1;
	display: flex;
	flex-direction: column;
	gap: 4px;
	min-width: 0;
}

.row-top {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 8px;
}

.row-top-acts {
	flex: 0 0 auto;
	display: flex;
	align-items: center;
	gap: 6px;
}

.notice-title {
	min-width: 0;
	font-size: 13.5px;
	font-weight: 600;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.notice-time {
	font-size: 12px;
	flex-shrink: 0;
	color: var(--el-text-color-secondary);
}

.row-bottom {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 10px;
}

.notice-desc {
	min-width: 0;
	font-size: 12px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	color: var(--el-text-color-secondary);
}
</style>
