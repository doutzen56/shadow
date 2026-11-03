<template>
	<div class="sys-upgrade-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="860px" append-to-body class="upg-info-dialog" :before-close="cancel">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"> <ele-InfoFilled /> </el-icon>
					<span>更新日志</span>
				</div>
			</template>

			<!-- 公文式阅读视图：标题区 + 文武线 + 正文区（与通知公告详情同一套版式，无底部按钮，右上角关闭即已读） -->
			<div class="upg-info" v-if="state.isShowDialog">
				<div class="upg-info-head">
					<h2 class="upg-info-title">{{ state.title }}</h2>
					<div class="upg-info-meta">
						<span class="upg-meta-item is-type">版本更新</span>
						<span class="upg-meta-item">
							<el-icon size="14"><ele-User /></el-icon>
							{{ state.userName || '系统' }}
						</span>
						<span class="upg-meta-item">
							<el-icon size="14"><ele-Clock /></el-icon>
							{{ state.publicTime || '-' }}
						</span>
					</div>
				</div>

				<!-- 文武线：上粗下细两条线，把标题区与正文区切开 -->
				<div class="upg-info-rule"></div>

				<div class="upg-info-body upg-rich" v-html="state.content"></div>
			</div>
		</el-dialog>
	</div>
</template>

<!-- 系统更新日志详情弹窗（登录后在顶栏自动弹出最近一条未读，关闭即标记已读） -->
<script lang="ts" setup name="upgradeInfo">
import { reactive } from 'vue';
import '@wangeditor/editor/dist/css/style.css';

import { getAPI } from '/@/utils/axios-utils';
import { SysUpgradeApi } from '/@/api-services/system/api';
import { formatDateTime } from '/@/utils/format';

const state = reactive({
	isShowDialog: false,
	id: 0,
	title: '',
	userName: '',
	publicTime: '',
	content: '',
});

// 打开弹窗（传入 SysUpgrade）
const openDialog = (row: any) => {
	const item = row ?? {};
	state.id = item.id ?? 0;
	state.title = '系统更新日志';
	state.userName = item.createUserName ?? '';
	state.publicTime = item.createTime ? formatDateTime(item.createTime, 'YYYY-mm-dd HH:MM') : '-';
	state.content = item.content ?? '<p>（暂无更新内容）</p>';
	state.isShowDialog = true;
};

// 关闭弹窗：先关窗口再标记已读
// 标记已读是异步接口，网络异常/令牌失效时会抛错；若放在关闭前 await，异常会让窗口永远关不掉
const cancel = (done?: () => void) => {
	state.isShowDialog = false;
	done?.();

	getAPI(SysUpgradeApi).apiSysUpgradeSetReadPost({ id: state.id }).catch(() => {});
};

// 导出对象
defineExpose({ openDialog });
</script>

<!-- 弹窗用了 append-to-body（顶栏里不传送会被层叠上下文裁掉）：
	 传送到 body 后 scoped 的 :deep() 会断链失效，所以这里的样式一律写在非 scoped 块里，
	 类名统一 .upg- 前缀，避免与页面其它样式撞车 -->
<style lang="scss">
.upg-info-dialog {
	.el-dialog__body {
		padding: 0;
	}
}

.upg-info {
	.upg-info-head {
		padding: 24px 32px 16px;
		background: color-mix(in srgb, var(--el-color-primary) 5%, transparent);
		text-align: center;

		.upg-info-title {
			margin: 0 0 12px;
			font-size: 22px;
			line-height: 1.4;
			font-weight: 700;
			letter-spacing: 0.5px;
			// 与文武线同色（红头文件版式），一处变量同时管标题与线
			color: var(--doc-rule-color, #c62828);
			word-break: break-all;
		}

		.upg-info-meta {
			display: flex;
			align-items: center;
			justify-content: center;
			flex-wrap: wrap;
			gap: 8px 20px;
			font-size: 12px;
			color: var(--el-text-color-secondary);

			.upg-meta-item {
				display: inline-flex;
				align-items: center;
				gap: 4px;
			}

			// 类型并入元信息行：不再单独顶在标题上方，避免"小字压标题"的观感
			.upg-meta-item.is-type {
				font-weight: 600;
				color: var(--el-color-primary);
			}
		}
	}

	/* 文武线：上粗下细两条线（红头文件版式）
	   默认红头红（--doc-rule-color 可覆盖），与标题区背景等宽（背景通栏，线不再内缩） */
	.upg-info-rule {
		margin: 0;
		border-top: 2px solid var(--doc-rule-color, #c62828);
		border-bottom: 1px solid var(--doc-rule-color, #c62828);
		padding-bottom: 3px; // 两线间距
	}

	.upg-info-body {
		max-height: 58vh;
		overflow-y: auto;
		padding: 20px 32px 28px;
		font-size: 14px;
		line-height: 1.9;
		color: var(--el-text-color-regular);
	}
}

/* 富文本排版：与更新日志列表卡（.up-rich）同一套规则，弹窗里放大一档 */
.upg-rich {
	word-break: break-word;

	> :first-child {
		margin-top: 0;
	}

	> :last-child {
		margin-bottom: 0;
	}

	p {
		margin: 0 0 10px;
	}

	h1,
	h2,
	h3,
	h4 {
		margin: 16px 0 10px;
		font-weight: 700;
		line-height: 1.4;
		color: var(--el-text-color-primary);
	}

	h1 {
		font-size: 19px;
	}

	h2 {
		font-size: 17px;
	}

	h3 {
		font-size: 15.5px;
	}

	h4 {
		font-size: 14px;
	}

	ul,
	ol {
		margin: 0 0 10px;
		padding-left: 22px;
	}

	li {
		margin: 3px 0;
	}

	a {
		color: var(--el-color-primary);
		text-decoration: none;

		&:hover {
			text-decoration: underline;
		}
	}

	blockquote {
		margin: 10px 0;
		padding: 8px 14px;
		border-left: 3px solid color-mix(in srgb, var(--el-color-primary) 45%, transparent);
		border-radius: 0 8px 8px 0;
		background: var(--el-fill-color-lighter);
		color: var(--el-text-color-secondary);
	}

	code {
		padding: 1px 5px;
		border-radius: 5px;
		font-family: var(--el-font-family-code, monospace);
		font-size: 13px;
		background: var(--el-fill-color);
		color: var(--el-color-primary);
	}

	pre {
		margin: 10px 0;
		padding: 12px 14px;
		border-radius: 10px;
		overflow: auto;
		background: var(--el-fill-color-light);

		code {
			padding: 0;
			background: transparent;
			color: var(--el-text-color-regular);
		}
	}

	table {
		width: 100%;
		margin: 10px 0;
		border-collapse: collapse;
		font-size: 13px;
	}

	th,
	td {
		padding: 7px 11px;
		border: 1px solid var(--el-border-color-lighter);
	}

	th {
		background: var(--el-fill-color-lighter);
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	img {
		max-width: 100%;
		border-radius: 8px;
	}
}
</style>
