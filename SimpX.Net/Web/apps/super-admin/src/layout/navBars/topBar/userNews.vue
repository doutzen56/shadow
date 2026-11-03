<template>
	<div class="user-news-container">
		<el-tabs stretch class="content-box">
			<el-tab-pane :label="'消息/站内信'">
				<template #label>
					<el-icon><ele-Bell /></el-icon>
					<span style="margin-left: 5px">{{ '消息/站内信' }}</span>
				</template>

				<div class="news-panel">
					<!-- 面板头部：标题、未读数量、一键已读 -->
					<div class="news-head">
						<div class="news-head-left">
							<span class="news-head-title">{{ '消息中心' }}</span>
							<span class="news-head-count" :class="{ 'is-zero': unreadList.length === 0 }">
								{{ unreadList.length > 0 ? `${unreadList.length} 条未读` : '全部已读' }}
							</span>
						</div>
						<el-button v-if="unreadList.length > 0" class="news-head-action" link type="primary" @click.stop="markAllRead">
							<el-icon size="14"><ele-Check /></el-icon>
							<span style="margin-left: 3px">{{ '全部标记已读' }}</span>
						</el-button>
					</div>

					<div class="notice-box">
						<template v-if="unreadList.length > 0">
							<div class="notice-item" v-for="(v, k) in unreadList" :key="v.id ?? k" @click="viewNoticeDetail(v)">
								<div class="notice-icon" :class="v.type == 1 ? 'is-notice' : 'is-announce'">
									<el-icon size="18">
										<ele-Message v-if="v.type == 1" />
										<ele-Notification v-else />
									</el-icon>
								</div>
								<div class="notice-main">
									<div class="notice-main-head">
										<span class="notice-unread-dot" />
										<span class="notice-title" :title="v.title">{{ v.title }}</span>
										<el-tag class="notice-type" size="small" effect="light" round :type="v.type == 1 ? 'primary' : 'warning'">
											{{ v.type == 1 ? '通知' : '公告' }}
										</el-tag>
									</div>
									<div class="notice-content">{{ removeHtmlSub(v.content) }}</div>
									<div class="notice-meta">
										<span class="notice-from">
											<el-icon size="12"><ele-User /></el-icon>
											<span>{{ v.publicUserName || '系统消息' }}</span>
										</span>
										<span class="notice-time" :title="v.publicTime">{{ formatTime(v.publicTime) }}</span>
									</div>
								</div>
							</div>
						</template>
						<el-empty v-else class="news-empty" :image-size="92" :description="'暂无未读消息'"></el-empty>
					</div>

					<div class="notice-foot" @click="goToNotice">
						<span>{{ '前往通知中心' }}</span>
						<el-icon class="notice-foot-arrow" size="14"><ele-Right /></el-icon>
					</div>
				</div>
			</el-tab-pane>

			<el-tab-pane :label="'我的'">
				<template #label>
					<el-icon><ele-Position /></el-icon>
					<span style="margin-left: 5px">{{ '我的' }}</span>
				</template>
				<div class="notice-box" style="height: 435px">
					<el-empty :description="'空'" style="height: 85%"></el-empty>
				</div>
			</el-tab-pane>
		</el-tabs>

		<!-- 消息详情弹窗（与通知中心表格共用同一组件） -->
		<NoticeDetailDialog ref="noticeDetailRef" />
	</div>
</template>

<script setup lang="ts" name="layoutBreadcrumbUserNews">
import { computed, ref } from 'vue';
import { dayjs, ElMessage } from 'element-plus';
import router from '/@/router';
import commonFunction from '/@/utils/commonFunction';

import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/system/api';
import NoticeDetailDialog from '/@/views/system/notice/component/noticeDetailDialog.vue';

const props = defineProps({
	noticeList: Array as any,
});
const { removeHtmlSub } = commonFunction();
const noticeDetailRef = ref();

// 未读消息（已读后即时从列表中消失）
const unreadList = computed(() => (props.noticeList ?? []).filter((v: any) => v.readStatus != 1));

// 相对时间格式化
const formatTime = (time?: any) => {
	if (!time) return '';
	const d = dayjs(time);
	if (!d.isValid()) return String(time);

	const diff = Date.now() - d.valueOf();
	const minute = 60 * 1000;
	const hour = 60 * minute;
	const day = 24 * hour;
	if (diff < 0 || diff < minute) return '刚刚';
	if (diff < hour) return `${Math.floor(diff / minute)} 分钟前`;
	if (diff < day) return `${Math.floor(diff / hour)} 小时前`;
	if (diff < 3 * day) return `${Math.floor(diff / day)} 天前`;
	if (d.year() === dayjs().year()) return d.format('MM-DD HH:mm');
	return d.format('YYYY-MM-DD HH:mm');
};
// 前往通知中心点击
const goToNotice = () => {
	router.push('/dashboard/notice');
};
// 查看消息详情
const viewNoticeDetail = async (notice: any) => {
	noticeDetailRef.value?.openDialog(notice);

	// 设置已读
	notice.readStatus = 1;
	await getAPI(SysNoticeApi).apiSysNoticeSetReadPost({ id: notice.id });
};
// 全部标记已读
const markAllRead = async () => {
	const list = unreadList.value as any[];
	if (list.length === 0) return;
	try {
		await Promise.all(list.map((v) => getAPI(SysNoticeApi).apiSysNoticeSetReadPost({ id: v.id })));
		list.forEach((v) => (v.readStatus = 1));
		ElMessage.success('已全部标记为已读');
	} catch (error) {
		console.error(error);
	}
};
</script>

<style scoped lang="scss">
.user-news-container {
	.content-box {
		font-size: 12px;

		.news-panel {
			display: flex;
			flex-direction: column;
		}

		/* 面板头部 */
		.news-head {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 8px;
			padding: 0 2px 10px;
			border-bottom: 1px solid var(--el-border-color-lighter);

			&-left {
				display: flex;
				align-items: center;
				gap: 8px;
				min-width: 0;
			}

			.news-head-title {
				position: relative;
				padding-left: 10px;
				font-size: 15px;
				font-weight: 700;
				letter-spacing: 0.5px;
				color: var(--el-text-color-primary);
				white-space: nowrap;

				&::before {
					content: '';
					position: absolute;
					left: 0;
					top: 50%;
					transform: translateY(-50%);
					width: 3px;
					height: 14px;
					border-radius: 2px;
					background: var(--el-color-primary);
				}
			}

			.news-head-count {
				flex: 0 0 auto;
				padding: 2px 8px;
				border-radius: 999px;
				font-size: 12px;
				font-weight: 600;
				line-height: 18px;
				background: var(--el-color-danger-light-9);
				color: var(--el-color-danger);

				&.is-zero {
					background: var(--el-color-success-light-9);
					color: var(--el-color-success);
				}
			}

			.news-head-action {
				flex: 0 0 auto;
				font-size: 12px;
			}
		}

		.notice-box {
			height: 352px;
			padding: 10px 2px 0;
			overflow-y: auto;
			overflow-x: hidden;
		}

		/* 空状态 */
		.news-empty {
			height: 100%;
			display: flex;
			flex-direction: column;
			align-items: center;
			justify-content: center;

			:deep(.el-empty__description) {
				color: var(--el-text-color-placeholder);
				font-size: 13px;
			}
		}

		/* 消息卡片 */
		.notice-item {
			position: relative;
			display: flex;
			align-items: flex-start;
			gap: 10px;
			padding: 12px;
			margin-bottom: 8px;
			border: 1px solid var(--el-border-color-lighter);
			border-radius: 10px;
			cursor: pointer;
			overflow: hidden;
			transition: border-color 0.25s ease, background-color 0.25s ease, box-shadow 0.25s ease, transform 0.25s ease;

			&::before {
				content: '';
				position: absolute;
				left: 0;
				top: 14px;
				bottom: 14px;
				width: 3px;
				border-radius: 0 3px 3px 0;
				background: var(--el-color-primary);
				opacity: 0;
				transition: opacity 0.25s ease;
			}

			.notice-icon {
				flex: 0 0 auto;
				width: 36px;
				height: 36px;
				border-radius: 10px;
				display: inline-flex;
				align-items: center;
				justify-content: center;
				background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
				color: var(--el-color-primary);
				transition: transform 0.3s ease;

				&.is-announce {
					background: color-mix(in srgb, var(--el-color-warning) 14%, transparent);
					color: var(--el-color-warning);
				}
			}

			.notice-main {
				flex: 1 1 auto;
				min-width: 0;
			}

			.notice-main-head {
				display: flex;
				align-items: center;
				gap: 6px;
				height: 22px;
			}

			.notice-unread-dot {
				flex: 0 0 auto;
				width: 6px;
				height: 6px;
				border-radius: 50%;
				background: var(--el-color-danger);
				box-shadow: 0 0 0 3px color-mix(in srgb, var(--el-color-danger) 16%, transparent);
			}

			.notice-title {
				flex: 1 1 auto;
				min-width: 0;
				font-size: 14px;
				font-weight: 600;
				color: var(--el-text-color-primary);
				white-space: nowrap;
				overflow: hidden;
				text-overflow: ellipsis;
			}

			.notice-type {
				flex: 0 0 auto;
				border: none;
				padding: 0 7px;
				height: 20px;
				line-height: 20px;
			}

			.notice-content {
				margin-top: 6px;
				width: 100%;
				font-size: 12px;
				line-height: 1.6;
				color: var(--el-text-color-secondary);
				display: -webkit-box;
				-webkit-box-orient: vertical;
				-webkit-line-clamp: 2;
				line-clamp: 2;
				overflow: hidden;
			}

			.notice-meta {
				display: flex;
				align-items: center;
				justify-content: space-between;
				gap: 8px;
				margin-top: 8px;
				font-size: 12px;
				color: var(--el-text-color-secondary);
			}

			.notice-from {
				display: inline-flex;
				align-items: center;
				gap: 4px;
				min-width: 0;
				max-width: 60%;
				overflow: hidden;
				white-space: nowrap;
				text-overflow: ellipsis;
			}

			.notice-time {
				flex: 0 0 auto;
				color: var(--el-text-color-placeholder);
				font-variant-numeric: tabular-nums;
			}

			&:hover {
				border-color: var(--el-color-primary-light-7);
				background: color-mix(in srgb, var(--el-color-primary) 5%, transparent);
				box-shadow: 0 6px 16px color-mix(in srgb, var(--el-color-primary) 14%, transparent);
				transform: translateY(-1px);

				&::before {
					opacity: 1;
				}

				.notice-icon {
					transform: scale(1.06) rotate(-4deg);
				}
			}
		}
	}
	.notice-foot {
		height: 42px;
		width: 100%;
		color: var(--el-color-primary);
		font-size: 13px;
		font-weight: 600;
		cursor: pointer;
		margin-top: 2px;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 4px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 10px;
		transition: background-color 0.25s ease, border-color 0.25s ease;

		.notice-foot-arrow {
			transition: transform 0.25s ease;
		}

		&:hover {
			background: color-mix(in srgb, var(--el-color-primary) 8%, transparent);
			border-color: var(--el-color-primary-light-7);

			.notice-foot-arrow {
				transform: translateX(4px);
			}
		}
	}
}
.el-tabs {
	:deep(.el-tabs__header) {
		margin: 0 0 10px;
	}

	:deep(.el-tabs__nav-wrap)::after {
		height: 1px;
		background-color: var(--el-border-color-lighter);
	}

	:deep(.el-tabs__item) {
		height: 42px;
		font-size: 13px;
	}

	.el-tab-pane {
		display: flex;
		flex-direction: column;
	}
}
</style>
