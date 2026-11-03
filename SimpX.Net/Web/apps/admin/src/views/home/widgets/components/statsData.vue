<template>
	<div>
		<el-card class="stat-container" shadow="hover">
			<template #header>
				<div class="card-header">
					<el-icon><ele-Coin /></el-icon>
					<span>系统数据</span>
				</div>
			</template>
		<div v-for="item in statList" :key="item.key" class="stat-card" :style="{ '--tint': item.color }">
			<!-- 右下角大号水印：给平铺的指标块一点纵深，不干扰读数 -->
			<span class="stat-mark"><el-icon><component :is="item.icon" /></el-icon></span>

			<div class="stat-head">
				<span class="icon-wrapper">
					<el-icon class="decorate-icon"><component :is="item.icon" /></el-icon>
				</span>
				<span class="stat-num">{{ item.value }}</span>
			</div>
			<span class="stat-title">{{ item.title }}</span>
		</div>
		</el-card>
	</div>
</template>

<script lang="ts">
export default {
	title: '系统数据',
	icon: 'ele-DataLine',
	description: '系统数据',
};
</script>

<!-- 系统数据 -->
<script setup lang="ts" name="statsData">
import { computed, onMounted, reactive, markRaw } from 'vue';
import { Briefcase, Collection, Document, Files, FolderOpened, HelpFilled, Menu, OfficeBuilding, Timer, UserFilled } from '@element-plus/icons-vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportStatsApi } from '/@/api-services/system/api';
import type { SysReportStatsOutput } from '/@/api-services/system/models';

/**
 * 静态配置：图标、颜色、标题、数据字段名
 * 用 markRaw 避免图标组件被响应式代理
 */
// 颜色统一走 --next-accent-* 强调色板（定义在 app.scss，明暗主题同值，中调色在浅底/深底上都够清晰）
const CARD_CONFIG = [
	{ title: '账号数量', key: 'userCount', icon: markRaw(UserFilled), color: 'var(--next-accent-1)' },
	{ title: '角色数量', key: 'roleCount', icon: markRaw(HelpFilled), color: 'var(--next-accent-3)' },
	{ title: '组织数量', key: 'orgCount', icon: markRaw(OfficeBuilding), color: 'var(--next-accent-4)' },
	{ title: '职位数量', key: 'posCount', icon: markRaw(Briefcase), color: 'var(--next-accent-6)' },
	{ title: '字典类型', key: 'dictTypeCount', icon: markRaw(Collection), color: 'var(--next-accent-2)' },
	{ title: '字典数据', key: 'dictDataCount', icon: markRaw(Files), color: 'var(--next-accent-5)' },
	{ title: '菜单数量', key: 'menuCount', icon: markRaw(Menu), color: 'var(--el-color-primary)' },
	{ title: '文件数量', key: 'fileCount', icon: markRaw(FolderOpened), color: 'var(--next-accent-8)' },
	{ title: '定时任务', key: 'jobCount', icon: markRaw(Timer), color: 'var(--next-accent-7)' },
	{ title: '操作日志', key: 'logOpCount', icon: markRaw(Document), color: 'var(--next-accent-9)' },
];

/**
 * 只让"数值"保持响应式
 */
const statsValue = reactive<Record<string, string>>(Object.fromEntries(CARD_CONFIG.map((c) => [c.key, '0'])));

/**
 * 模板用的合并列表：静态配置 + 响应式数值
 */
const statList = computed(() =>
	CARD_CONFIG.map((c) => ({
		...c,
		value: statsValue[c.key],
	}))
);

/**
 * 格式化数值：null/undefined -> 0，并加千分位
 */
const formatStatValue = (value: SysReportStatsOutput[keyof SysReportStatsOutput] | undefined) => {
	return (value ?? 0).toLocaleString();
};

onMounted(async () => {
	try {
		const res = await getAPI(SysReportStatsApi).apiSysReportStatsSystemStatsGet();
		const stats = res.data.result;
		if (!stats) return;

		CARD_CONFIG.forEach((c) => {
			statsValue[c.key] = formatStatValue(stats[c.key as keyof SysReportStatsOutput]);
		});
	} catch (error) {
		console.error('获取系统统计失败', error);
	}
});
</script>

<style lang="scss" scoped>
.stat-container {
	width: 100%;

	:deep(.el-card__body) {
		display: grid;
		/* 6/24 的窄列里也要能排两列，否则 10 项会竖着拉成一长条 */
		grid-template-columns: repeat(auto-fill, minmax(126px, 1fr));
		gap: 10px;
	}
}

/* 单张指标块：白底细描边 + 顶部主题色细线 + 右下角水印，商务报表的质感。
   内容仍是「图标 + 数值」一行、标题独占一行，窄列下标题也不会被省略号截掉 */
.stat-card {
	position: relative;
	overflow: hidden;
	min-width: 0;
	padding: 11px 12px 10px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	color: var(--el-text-color-primary);
	/* 左上角的极淡同色系渐层，比整块灰底更干净。
	   注意：color-mix() 在 Chromium 102（可信浏览器）里会让整条声明在「计算值阶段」失效，
	   属性直接回落到初始值（透明），写在前面的 fallback 不会生效 —— 必须用 @supports 隔离 */
	background: var(--el-bg-color);
	@supports (background: color-mix(in srgb, red, blue)) {
		background:
			linear-gradient(155deg, color-mix(in srgb, var(--tint) 10%, transparent) 0%, transparent 62%),
			var(--el-bg-color);
	}
	transition:
		transform 0.25s ease,
		box-shadow 0.25s ease,
		border-color 0.25s ease;

	/* 顶部 2px 主题色线：静默时半透明，hover 点亮 */
	&::before {
		content: '';
		position: absolute;
		inset: 0 0 auto;
		height: 2px;
		background: var(--tint);
		@supports (background: color-mix(in srgb, red, blue)) {
			background: linear-gradient(90deg, var(--tint) 0%, color-mix(in srgb, var(--tint) 20%, transparent) 100%);
		}
		opacity: 0.45;
		transition: opacity 0.25s ease;
	}

	&:hover {
		transform: translateY(-2px);
		border-color: var(--tint);
		box-shadow: 0 12px 24px -18px rgb(0 0 0 / 22%);
		@supports (background: color-mix(in srgb, red, blue)) {
			border-color: color-mix(in srgb, var(--tint) 38%, var(--el-border-color-lighter));
			box-shadow: 0 12px 24px -18px color-mix(in srgb, var(--tint) 90%, transparent);
		}

		&::before {
			opacity: 1;
		}

		.stat-mark {
			transform: rotate(-8deg) scale(1.1);
			opacity: 0.1;
		}

		.icon-wrapper {
			background-color: var(--el-fill-color);
			@supports (background: color-mix(in srgb, red, blue)) {
				background-color: color-mix(in srgb, var(--tint) 20%, transparent);
			}
		}
	}
}

/* 右下角水印图标 */
.stat-mark {
	position: absolute;
	right: -8px;
	bottom: -10px;
	font-size: 46px;
	line-height: 1;
	color: var(--tint);
	opacity: 0.055;
	pointer-events: none;
	transition:
		transform 0.4s ease,
		opacity 0.4s ease;
}

.stat-head {
	position: relative;
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.stat-num {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 18px;
	font-weight: 700;
	line-height: 1.15;
	letter-spacing: -0.2px;
	color: var(--el-text-color-primary);
	font-variant-numeric: tabular-nums;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stat-title {
	position: relative;
	display: block;
	margin-top: 7px;
	font-size: 12px;
	line-height: 1.3;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* 图标底衬：深色图标 + 浅色同色系圆角块 */
.icon-wrapper {
	flex: 0 0 auto;
	width: 28px;
	height: 28px;
	border-radius: 9px;
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--tint);
	background-color: var(--el-fill-color-light);
	@supports (background: color-mix(in srgb, red, blue)) {
		background-color: color-mix(in srgb, var(--tint) 12%, transparent);
	}
	transition: background-color 0.25s ease;
}

.decorate-icon {
	font-size: 16px;
}

/* ---- 老内核兜底（Chromium 102 / 可信浏览器）----
   color-mix() 在此内核下会让整条声明回落到初始值，图标块会变成无底色。
   改用伪元素叠一层同色 + opacity，等价于 color-mix(tint 12%, transparent)。
   放在 @supports not 里，新内核完全不受影响。 */
@supports not (background: color-mix(in srgb, red, blue)) {
	.icon-wrapper {
		position: relative;
		background-color: transparent;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			border-radius: inherit;
			background: var(--tint);
			opacity: 0.12;
		}

		.decorate-icon {
			position: relative;
		}
	}

	.stat-card:hover .icon-wrapper::before {
		opacity: 0.2;
	}
}
</style>
