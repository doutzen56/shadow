<template>
	<div class="up-page">
		<!-- 顶部头图：全站统一规范（theme/hero.scss） -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">更新日志</h2>
					<p class="page-hero__desc">按时间倒序记录系统每一次迭代的内容，最新一条排在最上方，便于回溯版本变化。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statTotal }}</span>
							<span class="page-hero__stat-k">日志总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statLatest }}</span>
							<span class="page-hero__stat-k">最近更新</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 操作条 -->
		<div class="up-toolbar">
			<div class="up-toolbar-left">
				<span class="up-toolbar-tip">
					<el-icon><ele-Sort /></el-icon>
					共 {{ statTotal }} 条记录，按创建时间倒序
				</span>
			</div>
			<div class="up-toolbar-actions">
				<el-button icon="ele-Refresh" :loading="state.loading" @click="loadList">刷新</el-button>
				<el-button type="primary" icon="ele-Plus" class="up-add-btn" @click="handleAdd">新增日志</el-button>
			</div>
		</div>

		<!-- 发行时间轴 -->
		<div class="up-timeline" v-loading="state.loading">
			<article
				class="up-item"
				v-for="(row, index) in state.list"
				:key="row.id"
				:style="{ animationDelay: Math.min(index * 0.05, 0.5) + 's' }"
			>
				<!-- 左侧：日期 + 轴线 -->
				<div class="up-rail">
					<div class="up-date">
						<b>{{ dateParts(row.createTime).day }}</b>
						<span>{{ dateParts(row.createTime).ym }}</span>
					</div>
					<span class="up-node" :class="{ 'is-latest': index === 0 }"></span>
				</div>

				<!-- 右侧：日志内容卡 -->
				<div class="up-card">
					<header class="up-card-head">
						<span class="up-badge" v-if="index === 0">最新</span>
						<span class="up-stamp">
							<el-icon><ele-Clock /></el-icon>
							{{ formatDateTime(row.createTime, 'YYYY-mm-dd HH:MM') }}
						</span>
						<span class="up-author" v-if="row.createUserName">
							<el-icon><ele-User /></el-icon>
							{{ row.createUserName }}
						</span>
						<div class="up-card-ops">
							<el-tooltip content="编辑" placement="top">
								<el-button icon="ele-Edit" text type="primary" @click.stop="handleEdit(row)" />
							</el-tooltip>
						</div>
					</header>

					<div class="up-rich" :class="{ 'is-clamp': needClamp(row) && !isExpanded(row) }" v-html="row.content || '<p>（暂无内容）</p>'"></div>

					<div class="up-more" v-if="needClamp(row)">
						<el-button link type="primary" @click="toggleExpand(row)">
							{{ isExpanded(row) ? '收起' : '展开全文' }}
							<el-icon><ele-ArrowDown v-if="!isExpanded(row)" /><ele-ArrowUp v-else /></el-icon>
						</el-button>
					</div>
				</div>
			</article>

			<div class="up-empty" v-if="!state.loading && state.list.length === 0">
				<el-empty description="还没有更新日志，点击右上角「新增日志」开始记录" :image-size="140" />
			</div>
		</div>

		<div class="up-pager" v-if="pager.total > 0">
			<el-pagination
				v-model:current-page="pager.page"
				v-model:page-size="pager.pageSize"
				:page-sizes="[10, 20, 50, 100]"
				:total="pager.total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onSizeChange"
				@current-change="loadList"
			/>
		</div>

		<EditUpgrade ref="editUpgradeRef" :title="state.title" @handleQuery="loadList" />
	</div>
</template>

<!-- 更新日志 -->
<script lang="ts" setup name="sysUpgrade">
import { computed, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';

import EditUpgrade from './component/editUpgrade.vue';

import { formatDateTime } from '/@/utils/format';
import { getAPI } from '/@/utils/axios-utils';
import { SysUpgradeApi } from '/@/api-services/system/api';
import { SysUpgrade, PageUpgradeInput } from '/@/api-services/system/models';

const editUpgradeRef = ref<InstanceType<typeof EditUpgrade>>();

const state = reactive({
	loading: false,
	title: '',
	/** 日志列表（按创建时间倒序） */
	list: [] as SysUpgrade[],
	/** 展开状态：id → 是否已展开，默认收起 */
	expanded: {} as Record<string, boolean>,
});

const pager = reactive({ page: 1, pageSize: 10, total: 0 });

// 取数：更新日志没有查询条件，固定按创建时间倒序
const loadList = async () => {
	state.loading = true;
	try {
		const params = {
			page: pager.page,
			pageSize: pager.pageSize,
			field: 'createTime',
			order: 'desc',
			descStr: 'desc',
		} as PageUpgradeInput;
		const res = await getAPI(SysUpgradeApi).apiSysUpgradePagePost(params);
		state.list = (res.data.result?.items ?? []) as SysUpgrade[];
		pager.total = Number(res.data.result?.total ?? 0);
	} catch {
		state.list = [];
		pager.total = 0;
		ElMessage.error('更新日志加载失败');
	} finally {
		state.loading = false;
	}
};

const onSizeChange = () => {
	pager.page = 1;
	loadList();
};

/* ------------------------------ 展示辅助 ------------------------------ */
const statTotal = computed(() => pager.total);
const statLatest = computed(() => (state.list[0]?.createTime ? formatDateTime(state.list[0].createTime, 'YYYY-mm-dd') : '—'));

// 左侧日期块：日 + 年月
const dateParts = (val?: string | Date | null) => {
	const text = formatDateTime(val, 'YYYY-mm-dd', '');
	const [y, m, d] = text.split('-');
	return { day: d || '--', ym: y && m ? `${y}-${m}` : '—' };
};

// 内容较长时才给「展开全文」，避免短日志出现无意义的按钮
const needClamp = (row: SysUpgrade) => (row.content?.length ?? 0) > 420;
const isExpanded = (row: SysUpgrade) => !!state.expanded[String(row.id)];
const toggleExpand = (row: SysUpgrade) => {
	const key = String(row.id);
	state.expanded[key] = !state.expanded[key];
};

/* ------------------------------ 增改 ------------------------------ */
const handleAdd = () => {
	state.title = '添加系统更新日志';
	editUpgradeRef.value?.openDialog({ type: 1 });
};

const handleEdit = (row: SysUpgrade) => {
	state.title = '编辑系统更新日志';
	editUpgradeRef.value?.openDialog(row);
};

// 页面初始化
loadList();
</script>

<style lang="scss" scoped>
.up-page {
	position: relative;
	padding-bottom: 4px;
}

/* 顶部头图统一走 theme/hero.scss 的 .page-hero，本页不再复写 */

/* ===================== 操作条 ===================== */
.up-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	flex-wrap: wrap;
	margin-top: 14px;
	padding: 12px 18px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		border-color 0.25s ease,
		box-shadow 0.25s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 28%, var(--el-border-color-lighter));
		box-shadow: 0 10px 24px -20px color-mix(in srgb, var(--el-color-primary) 80%, transparent);
	}
}

.up-toolbar-tip {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}

.up-toolbar-actions {
	display: flex;
	align-items: center;
	gap: 8px;
}

.up-add-btn {
	position: relative;
	overflow: hidden;
	font-weight: 600;

	&::after {
		content: '';
		position: absolute;
		top: 0;
		bottom: 0;
		width: 40px;
		left: -60px;
		background: linear-gradient(100deg, transparent, rgba(255, 255, 255, 0.45), transparent);
		transform: skewX(-20deg);
		transition: left 0.6s ease;
	}

	&:hover::after {
		left: 120%;
	}
}

/* ===================== 时间轴 ===================== */
.up-timeline {
	position: relative;
	margin-top: 16px;
	min-height: 160px;
}

.up-item {
	position: relative;
	display: flex;
	gap: 18px;
	animation: upRise 0.45s ease backwards;

	+ .up-item {
		margin-top: 14px;
	}
}

/* 左侧日期栏：日期块靠左、节点（轴线）单独占右侧一列。
 * 关键点：rail 的 padding-right 必须大于「节点宽度 + 间隙」，
 * 否则右对齐的日期文字会直接压到节点上（就是"日期和轴线重叠"的成因） */
.up-rail {
	position: relative;
	flex: 0 0 96px;
	padding: 12px 22px 0 0;
	display: flex;
	flex-direction: column;
	align-items: flex-end;

	/* 竖轴：与节点同轴（节点/轴线都按 right 定位，中心线对齐），
	 * 起点放在节点上沿，让节点圆点盖住接头，看起来是"线穿过节点" */
	&::before {
		content: '';
		position: absolute;
		right: 5px;
		top: 24px;
		bottom: -14px;
		width: 2px;
		border-radius: 2px;
		background: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 45%, transparent), var(--el-border-color-lighter));
	}

	.up-item:last-child &::before {
		bottom: 4px;
		opacity: 0.5;
	}
}

.up-date {
	text-align: right;
	line-height: 1.1;

	b {
		display: block;
		font-size: 24px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		font-variant-numeric: tabular-nums;
	}

	span {
		display: block;
		margin-top: 3px;
		font-size: 11.5px;
		letter-spacing: 0.3px;
		color: var(--el-text-color-secondary);
	}
}

/* 节点与大号日期居中对齐（日期块高 26.4px，中心约在 25px），
 * 不再落在年月那一行上 —— 那一行正好是文字最右端，最容易被压到 */
.up-node {
	position: absolute;
	right: 0;
	top: 19px;
	width: 12px;
	height: 12px;
	border-radius: 50%;
	background: var(--el-bg-color);
	border: 2px solid color-mix(in srgb, var(--el-color-primary) 45%, var(--el-border-color));
	transition:
		transform 0.3s ease,
		border-color 0.3s ease;

	&.is-latest {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 4px color-mix(in srgb, var(--el-color-primary) 18%, transparent);
		animation: upPulse 2.4s ease-in-out infinite;
	}
}

.up-item:hover .up-node {
	transform: scale(1.15);
	border-color: var(--el-color-primary);
}

/* 右侧内容卡 */
.up-card {
	position: relative;
	flex: 1;
	min-width: 0;
	padding: 14px 18px 12px;
	border-radius: 14px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		transform 0.3s ease,
		box-shadow 0.3s ease,
		border-color 0.3s ease;

	/* 卡片左侧留一条主题色描边，呼应时间轴节点 */
	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 14px;
		bottom: 14px;
		width: 3px;
		border-radius: 0 3px 3px 0;
		background: linear-gradient(180deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary) 20%, transparent));
		opacity: 0.5;
		transition: opacity 0.3s ease;
	}
}

.up-item:hover .up-card {
	transform: translateX(2px);
	border-color: color-mix(in srgb, var(--el-color-primary) 32%, var(--el-border-color-lighter));
	box-shadow: 0 14px 30px -24px color-mix(in srgb, var(--el-color-primary) 90%, transparent);

	&::before {
		opacity: 1;
	}
}

.up-card-head {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
	padding-bottom: 10px;
	margin-bottom: 10px;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}

.up-badge {
	flex-shrink: 0;
	padding: 1px 8px;
	border-radius: 999px;
	font-size: 11px;
	font-weight: 700;
	letter-spacing: 0.5px;
	color: var(--el-color-white);
	background: linear-gradient(135deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary), #ffffff 30%));
	box-shadow: 0 6px 14px -10px var(--el-color-primary);
}

.up-stamp,
.up-author {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.up-card-ops {
	margin-left: auto;
	flex-shrink: 0;
	margin-right: -8px;

	:deep(.el-button.is-text) {
		padding: 5px;
		font-size: 15px;
	}
}

/* 富文本内容：内容来自后台编辑器，统一收敛标题/段落/列表/表格/代码样式 */
.up-rich {
	font-size: 13px;
	line-height: 1.75;
	color: var(--el-text-color-regular);
	word-break: break-word;

	&.is-clamp {
		max-height: 232px;
		overflow: hidden;
		mask-image: linear-gradient(180deg, #000 68%, transparent);
	}

	:deep(> :first-child) {
		margin-top: 0;
	}

	:deep(> :last-child) {
		margin-bottom: 0;
	}

	:deep(p) {
		margin: 0 0 8px;
	}

	:deep(h1),
	:deep(h2),
	:deep(h3),
	:deep(h4) {
		margin: 14px 0 8px;
		font-weight: 700;
		line-height: 1.4;
		color: var(--el-text-color-primary);
	}

	:deep(h1) {
		font-size: 17px;
	}

	:deep(h2) {
		font-size: 16px;
	}

	:deep(h3) {
		font-size: 14.5px;
	}

	:deep(h4) {
		font-size: 13.5px;
	}

	:deep(ul),
	:deep(ol) {
		margin: 0 0 8px;
		padding-left: 20px;
	}

	:deep(li) {
		margin: 2px 0;
	}

	:deep(a) {
		color: var(--el-color-primary);
		text-decoration: none;

		&:hover {
			text-decoration: underline;
		}
	}

	:deep(blockquote) {
		margin: 8px 0;
		padding: 6px 12px;
		border-left: 3px solid color-mix(in srgb, var(--el-color-primary) 45%, transparent);
		border-radius: 0 8px 8px 0;
		background: var(--el-fill-color-lighter);
		color: var(--el-text-color-secondary);
	}

	:deep(code) {
		padding: 1px 5px;
		border-radius: 5px;
		font-family: var(--el-font-family-code, monospace);
		font-size: 12px;
		background: var(--el-fill-color);
		color: var(--el-color-primary);
	}

	:deep(pre) {
		margin: 8px 0;
		padding: 10px 12px;
		border-radius: 10px;
		overflow: auto;
		background: var(--el-fill-color-light);

		code {
			padding: 0;
			background: transparent;
			color: var(--el-text-color-regular);
		}
	}

	:deep(table) {
		width: 100%;
		margin: 8px 0;
		border-collapse: collapse;
		font-size: 12.5px;
	}

	:deep(th),
	:deep(td) {
		padding: 6px 10px;
		border: 1px solid var(--el-border-color-lighter);
	}

	:deep(th) {
		background: var(--el-fill-color-lighter);
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	:deep(img) {
		max-width: 100%;
		border-radius: 8px;
	}
}

.up-more {
	display: flex;
	justify-content: center;
	margin-top: 4px;

	:deep(.el-button.is-link) {
		display: inline-flex;
		align-items: center;
		gap: 3px;
		font-size: 12.5px;
	}
}

.up-empty {
	margin-top: 4px;
	padding: 34px 0;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px dashed var(--el-border-color-lighter);
}

.up-pager {
	display: flex;
	justify-content: flex-end;
	margin-top: 16px;
	padding: 10px 12px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}

/* ===================== 动画 ===================== */
@keyframes upRise {
	from {
		opacity: 0;
		transform: translateY(12px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@keyframes upPulse {
	0%,
	100% {
		box-shadow: 0 0 0 4px color-mix(in srgb, var(--el-color-primary) 18%, transparent);
	}
	50% {
		box-shadow: 0 0 0 7px color-mix(in srgb, var(--el-color-primary) 6%, transparent);
	}
}

@media (max-width: 900px) {
	.up-rail {
		flex-basis: 76px;
		padding-right: 18px;

		.up-date b {
			font-size: 20px;
		}
	}

	.up-node {
		top: 16px;
	}
}
</style>
