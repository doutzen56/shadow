<template>
	<div class="wf-detail__head">
		<div class="wf-detail__head-main">
			<span class="wf-detail__icon">
				<el-icon><component :is="icon" /></el-icon>
			</span>
			<div class="wf-detail__head-text">
				<div class="wf-detail__title">{{ title }}</div>
				<div class="wf-detail__sub">
					<span class="wf-detail__sub-name" :title="name">{{ name || '--' }}</span>
					<span v-if="sub" class="wf-detail__sub-extra">{{ sub }}</span>
					<span v-if="code" class="wf-detail__sub-code">{{ code }}</span>
				</div>
			</div>
		</div>
		<div class="wf-detail__head-side">
			<slot name="side" />
		</div>
	</div>
</template>

<script setup lang="ts" name="WorkFlowDetailHead">
import '/@/views/workFlow/styles/workflow.scss'; // 工作流模块公共样式（按模块加载，非全局 theme）
withDefaults(
	defineProps<{
		/** 主标题，如「待办详情」 */
		title: string;
		/** 副行主文案，通常是流程标题 / 节点名称 */
		name?: string;
		/** 副行胶囊，通常是实例编码 / 节点ID */
		code?: string;
		/** 副行补充文案 */
		sub?: string;
		/** 图标组件名（全局注册的 ele-* 图标） */
		icon?: string;
	}>(),
	{ icon: 'ele-InfoFilled' }
);
</script>

<style scoped lang="scss">
.wf-detail__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	padding: 14px 56px 14px 18px;
	background: linear-gradient(118deg, var(--el-color-primary), var(--el-color-primary-dark-2));
	color: #fff;
}

.wf-detail__head-main {
	display: flex;
	align-items: center;
	gap: 12px;
	min-width: 0;
}

.wf-detail__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	background: rgba(255, 255, 255, 0.18);
	backdrop-filter: blur(4px);
	font-size: 17px;
	flex-shrink: 0;
}

/* 全局 .el-dialog__header div{display:flex} 会把标题与副标题压成一行，用双类选择器反压 */
.el-dialog__header .wf-detail__head-text,
.wf-detail__head-text {
	min-width: 0;
	display: block;
}

.wf-detail__title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
	letter-spacing: 0.3px;
}

.wf-detail__sub {
	margin-top: 3px;
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 12px;
	color: rgba(255, 255, 255, 0.82);
	min-width: 0;
}

.wf-detail__sub-name {
	max-width: 260px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.wf-detail__sub-code {
	display: inline-block;
	max-width: 240px;
	padding: 1px 8px;
	border-radius: 20px;
	background: rgba(255, 255, 255, 0.16);
	font-family: var(--el-font-family-mono, monospace);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	flex-shrink: 0;
}

.wf-detail__sub-extra {
	max-width: 220px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.wf-detail__head-side {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-shrink: 0;
}
</style>
