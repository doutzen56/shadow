<template>
	<el-dialog v-model="state.isShowDialog" draggable width="860px" class="notice-detail-dialog">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Bell /> </el-icon>
				<span> {{ '消息详情' }} </span>
			</div>
		</template>

		<!-- 公文式阅读视图：顶部标题区 + 正文区（无底部按钮，右上角关闭即可） -->
		<div class="notice-detail" v-if="state.title">
			<div class="notice-detail-head">
				<h2 class="notice-detail-title">{{ state.title }}</h2>
				<div class="notice-detail-meta">
					<span class="meta-item meta-type">{{ state.type === 1 ? '通知' : '公告' }}</span>
					<span class="meta-item">
						<el-icon size="14"><ele-User /></el-icon>
						{{ state.userName || '系统消息' }}
					</span>
					<span class="meta-item" v-if="state.orgName">
						<el-icon size="14"><ele-OfficeBuilding /></el-icon>
						{{ state.orgName }}
					</span>
					<span class="meta-item">
						<el-icon size="14"><ele-Clock /></el-icon>
						{{ state.publicTime || '-' }}
					</span>
				</div>
			</div>

			<!-- 文武线：上粗下细两条线，把标题区与正文区切开 -->
			<div class="notice-detail-rule"></div>

			<div class="notice-detail-body" v-html="state.content" data-slate-editor></div>
		</div>
	</el-dialog>
</template>

<!-- 消息详情弹窗（顶栏消息卡片 / 通知中心表格共用） -->
<script lang="ts" setup name="noticeDetailDialog">
import { reactive } from 'vue';
import '@wangeditor/editor/dist/css/style.css';

const state = reactive({
	isShowDialog: false,
	title: '',
	type: 1 as any,
	userName: '',
	orgName: '',
	publicTime: '',
	content: '',
});

// 打开详情（传入 SysNotice 对象）
const openDialog = (notice?: any) => {
	const item = notice ?? {};
	state.title = item.title ?? '';
	state.type = item.type ?? 1;
	state.userName = item.publicUserName ?? '';
	state.orgName = item.publicOrgName ?? '';
	state.publicTime = item.publicTime ?? '';
	state.content = item.content ?? '';
	state.isShowDialog = true;
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.notice-detail-dialog {
	:deep(.el-dialog__body) {
		padding: 0;
	}
}
.notice-detail {
	.notice-detail-head {
		position: relative;
		padding: 24px 32px 16px 32px;
		background: color-mix(in srgb, var(--el-color-primary) 5%, transparent);
		text-align: center;

		.notice-detail-title {
			margin: 0 0 12px;
			font-size: 22px;
			line-height: 1.4;
			font-weight: 700;
			letter-spacing: 0.5px;
			// 与文武线同色（红头文件版式），一处变量同时管标题与线
			color: var(--doc-rule-color, #c62828);
			word-break: break-all;
		}

		.notice-detail-meta {
			display: flex;
			align-items: center;
			justify-content: center;
			flex-wrap: wrap;
			gap: 8px 20px;
			font-size: 12px;
			color: var(--el-text-color-secondary);

			.meta-item {
				display: inline-flex;
				align-items: center;
				gap: 4px;
			}

			// 类型（通知 / 公告）并入元信息行：不再单独顶在标题上方，避免"小字压标题"的观感
			.meta-type {
				font-weight: 600;
				color: var(--el-color-primary);
			}
		}
	}

	/* 文武线：上粗下细两条线（红头文件版式）
	   默认红头红（--doc-rule-color 可覆盖），与标题区背景等宽（背景通栏，线不再内缩） */
	.notice-detail-rule {
		margin: 0;
		border-top: 2px solid var(--doc-rule-color, #c62828);
		border-bottom: 1px solid var(--doc-rule-color, #c62828);
		padding-bottom: 3px; // 两线间距
	}

	.notice-detail-body {
		max-height: 58vh;
		overflow-y: auto;
		padding: 20px 32px 28px;
		font-size: 14px;
		line-height: 1.9;
		color: var(--el-text-color-regular);
	}

	:deep(.el-tag) {
		border: none;
	}
}
</style>
