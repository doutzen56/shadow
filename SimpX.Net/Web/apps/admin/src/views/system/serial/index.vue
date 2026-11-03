<template>
	<div class="sn-page">
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
					<h2 class="page-hero__title">流水序号</h2>
					<p class="page-hero__desc">按业务分类维护单据编号的生成规则：序列号在上限内递增，到期后按重置间隔归零。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="启用 / 停用数量按当前页数据统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statTotal }}</span>
							<span class="page-hero__stat-k">规则总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ statEnabled }}</span>
							<span class="page-hero__stat-k">已启用</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-off">{{ statDisabled }}</span>
							<span class="page-hero__stat-k">已停用</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询与操作：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :show-message="false" :inline-message="true" label-width="auto" class="query-form">
				<el-form-item label="分类" prop="type">
					<el-select v-model="state.queryParams.type" placeholder="全部分类" clearable class="query-select" @keyup.enter.native="handleSearch">
						<el-option v-for="item in state.typeData" :key="item.value" :label="item.text" :value="item.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态" prop="status">
					<g-sys-dict v-model="state.queryParams.status" :code="'StatusEnum'" render-as="select" clearable placeholder="全部状态" @keyup.enter.native="handleSearch" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button icon="ele-Search" type="primary" @click="handleSearch" v-auth="'sysSerial/page'" :loading="state.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading">重置</el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" class="sn-add-btn" @click="handleAdd" v-auth="'sysSerial/add'">新增流水号</el-button>
			</div>
		</div>

		<!-- 号码牌卡片：外壳走全局 .entity-card，序列号大字与用量刻度是本页业务结构，保留自有类 -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !state.list.length" description="没有找到匹配的流水号规则" :image-size="140" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable"
							:class="'is-primary'"
							@dblclick="onCardDblclick(row)"
						>
							<span class="entity-card__watermark">#</span>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-CollectionTag /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ typeLabel(row) }}</div>
									<div class="entity-card__sub">排序 {{ row.orderNo ?? '—' }}</div>
								</div>
								<el-tag size="small" effect="light" :type="isEnabled(row) ? 'success' : 'info'">{{ isEnabled(row) ? '启用' : '停用' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<!-- 主体：当前序列号用等宽大字，像票据编号 -->
								<div class="sn-figure">
									<span class="sn-figure-k">当前序列号</span>
									<div class="sn-figure-row">
										<strong class="sn-figure-v">{{ formatSeq(row.seq) }}</strong>
										<span class="sn-figure-max">/ {{ formatSeq(row.max) }}</span>
									</div>
								</div>

								<!-- 用量刻度：序列号在最小值~最大值之间的位置 -->
								<div class="sn-meter" :title="`已用 ${usagePct(row)}%（${row.min ?? 0} ~ ${row.max ?? 0}）`">
									<div class="sn-meter-track">
										<i :style="{ width: usagePct(row) + '%' }"></i>
									</div>
									<div class="sn-meter-meta">
										<span>{{ formatSeq(row.min) }}</span>
										<span class="sn-meter-pct">{{ usagePct(row) }}%</span>
										<span>{{ formatSeq(row.max) }}</span>
									</div>
								</div>

								<!-- 表达式（等宽，触边省略） -->
								<div class="code-preview" :title="row.formater || '未设置格式化表达式'">{{ row.formater || '未设置格式化表达式' }}</div>

								<div class="tag-group">
									<el-tag size="small" effect="light" type="info">
										<el-icon><ele-RefreshRight /></el-icon>
										{{ resetLabel(row) }}
									</el-tag>
									<el-tag size="small" effect="light" type="warning" :title="`有效期至 ${formatDateTime(row.expy, 'YYYY-mm-dd HH:MM')}`">
										<el-icon><ele-Calendar /></el-icon>
										{{ formatDateTime(row.expy, 'YYYY-mm-dd') }}
									</el-tag>
								</div>

								<div class="card-desc" :title="row.remark || ''">{{ row.remark || '暂无备注' }}</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ row.updateUserName || row.createUserName || '—' }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysSerial/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysSerial/delete'" @click.stop="handleDelete(row)" />
									</el-tooltip>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				v-if="pager.total > 0"
				class="table-pager"
				v-model:current-page="pager.page"
				v-model:page-size="pager.pageSize"
				:page-sizes="[12, 24, 48, 96]"
				:total="pager.total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onSizeChange"
				@current-change="loadList"
			/>
		</el-card>

		<EditSerial ref="editSerialRef" :title="state.title" @handleQuery="loadList" />
	</div>
</template>

<!-- 流水号 -->
<script lang="ts" setup name="sysSerial">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';

import { auth } from '/@/utils/authFunction';
import { formatDateTime } from '/@/utils/format';
import EditSerial from './component/editSerial.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysSerialApi } from '/@/api-services/system/api';
import { PageSerialInput, SysSerial } from '/@/api-services/system/models';

/** 重置间隔枚举（与后端 ResetIntervalEnum 一致） */
const RESET_LABELS: Record<number, string> = {
	1: '不重置',
	2: '按日重置',
	3: '按月重置',
	4: '按年重置',
};

const editSerialRef = ref<InstanceType<typeof EditSerial>>();

const state = reactive({
	loading: false,
	title: '',
	queryParams: {
		type: undefined as string | undefined,
		status: undefined as number | undefined,
	},
	list: [] as SysSerial[],
	typeData: [] as any[],
});

const pager = reactive({ page: 1, pageSize: 12, total: 0 });

// 取数
const loadList = async () => {
	state.loading = true;
	try {
		const params = {
			...state.queryParams,
			page: pager.page,
			pageSize: pager.pageSize,
			field: 'orderNo',
			order: 'asc',
			descStr: 'desc',
		} as PageSerialInput;
		const res = await getAPI(SysSerialApi).apiSysSerialPagePost(params);
		state.list = (res.data.result?.items ?? []) as SysSerial[];
		pager.total = Number(res.data.result?.total ?? 0);
	} catch {
		state.list = [];
		pager.total = 0;
		ElMessage.error('流水号加载失败');
	} finally {
		state.loading = false;
	}
};

const handleSearch = () => {
	pager.page = 1;
	loadList();
};

const resetQuery = () => {
	state.queryParams.type = undefined;
	state.queryParams.status = undefined;
	pager.page = 1;
	loadList();
};

const onSizeChange = () => {
	pager.page = 1;
	loadList();
};

/* ------------------------------ 展示辅助 ------------------------------ */
const statTotal = computed(() => pager.total);
const statEnabled = computed(() => state.list.filter((r) => isEnabled(r)).length);
const statDisabled = computed(() => state.list.length - statEnabled.value);

const isEnabled = (row: SysSerial) => Number(row.status) === 1;

const typeLabel = (row: SysSerial) => state.typeData.find((x) => String(x.value) === String(row.type))?.text ?? row.type ?? '未分类';

const resetLabel = (row: SysSerial) => RESET_LABELS[Number(row.resetInterval)] ?? '—';

// 序列号统一千分位展示，位数变化时列宽不跳
const formatSeq = (val?: number | null) => (val === null || val === undefined ? '—' : Number(val).toLocaleString('en-US'));

// 已用比例：当前序列号在 最小值~最大值 之间的位置
const usagePct = (row: SysSerial) => {
	const min = Number(row.min ?? 0);
	const max = Number(row.max ?? 0);
	const seq = Number(row.seq ?? 0);
	if (!(max > min)) return 0;
	const pct = ((seq - min) / (max - min)) * 100;
	return Math.min(100, Math.max(0, Math.round(pct)));
};

/* ------------------------------ 增改删 ------------------------------ */
const handleAdd = () => {
	state.title = '添加流水号';
	editSerialRef.value?.openDialog({ resetInterval: 1, min: 1, max: 99999999, status: 1, orderNo: 100 }, state.typeData);
};

const handleEdit = (row: SysSerial) => {
	state.title = '编辑流水号';
	editSerialRef.value?.openDialog(row, state.typeData);
};

const onCardDblclick = (row: SysSerial) => {
	if (auth('sysSerial/update')) handleEdit(row);
};

const handleDelete = (row: SysSerial) => {
	ElMessageBox.confirm(`确定删除流水号规则「${typeLabel(row)}」?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysSerialApi).apiSysSerialDeletePost({ id: row.id });
			await loadList();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 页面初始化：先取分类字典，再取列表
onMounted(async () => {
	state.typeData = await getAPI(SysSerialApi)
		.apiSysSerialTypeListGet()
		.then((res) => res.data.result ?? [])
		.catch(() => []);
	loadList();
});
</script>

<style lang="scss" scoped>

/* 卡片外壳与查询区走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉。
   下面只保留本页特有的业务结构：序列号大字块 + 用量刻度。 */

.sn-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		flex-shrink: 0;
		margin-top: 14px;
	}
}



/* 新增流水号：主操作，加一道扫光让它在工具条里先被看见 */

.sn-add-btn {
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

.sn-figure {
	position: relative;
}

.sn-figure-k {
	display: block;
	font-size: 11.5px;
	letter-spacing: 0.4px;
	color: var(--el-text-color-secondary);
}

.sn-figure-row {
	display: flex;
	align-items: baseline;
	gap: 6px;
	margin-top: 1px;
	min-width: 0;
}

.sn-figure-v {
	font-family: var(--el-font-family-code, monospace);
	font-size: 30px;
	font-weight: 700;
	line-height: 1.15;
	letter-spacing: 0.5px;
	color: var(--el-text-color-primary);
	font-variant-numeric: tabular-nums;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.sn-figure-max {
	flex-shrink: 0;
	font-family: var(--el-font-family-code, monospace);
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}

.sn-meter {
	position: relative;
}

.sn-meter-track {
	height: 6px;
	border-radius: 999px;
	background: var(--el-fill-color);
	overflow: hidden;

	i {
		display: block;
		height: 100%;
		min-width: 3px;
		border-radius: 999px;
		background: linear-gradient(90deg, color-mix(in srgb, var(--c1) 60%, transparent), var(--c1));
		transition: width 0.6s ease;
	}
}

.sn-meter-meta {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-top: 4px;
	font-size: 11px;
	color: var(--el-text-color-placeholder);
	font-variant-numeric: tabular-nums;
}

.sn-meter-pct {
	font-weight: 700;
	color: color-mix(in srgb, var(--c1) 80%, var(--el-text-color-regular));
}



/* 表达式：等宽代码块，触边才省略 */

.sn-code {
	position: relative;
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 6px 9px;
	border-radius: 9px;
	background: var(--el-fill-color-lighter);
	border: 1px dashed color-mix(in srgb, var(--c1) 26%, var(--el-border-color-lighter));
	color: var(--el-text-color-secondary);
	font-size: 11.5px;
	min-width: 0;

	code {
		flex: 1 1 auto;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-family: var(--el-font-family-code, monospace);
		color: color-mix(in srgb, var(--c1) 72%, var(--el-text-color-primary));
	}
}

.sn-tags {
	position: relative;
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
}

.sn-chip {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	padding: 3px 8px;
	border-radius: 8px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
	border: 1px solid transparent;
	font-variant-numeric: tabular-nums;
}

.sn-remark {
	position: relative;
	margin: 0;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-text-color-secondary);
	display: -webkit-box;
	-webkit-line-clamp: 1;
	line-clamp: 1;
	-webkit-box-orient: vertical;
	overflow: hidden;
}

.sn-foot {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	padding-top: 10px;
	border-top: 1px dashed var(--el-border-color-lighter);
}

.sn-foot-meta {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	span {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.sn-ops {
	display: inline-flex;
	align-items: center;
	gap: 0;
	margin-right: -8px;
	flex-shrink: 0;

	:deep(.el-button.is-text) {
		padding: 5px;
		font-size: 15px;
	}
}

.sn-empty {
	margin-top: 14px;
	padding: 32px 0;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px dashed var(--el-border-color-lighter);
}

.sn-pager {
	display: flex;
	justify-content: flex-end;
	margin-top: 16px;
	padding: 10px 12px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}



/* ===================== 动画（只动 transform / opacity） ===================== */

@keyframes snRise {
	from {
		opacity: 0;
		transform: translateY(12px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@keyframes snPulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.5;
		transform: scale(1.3);
	}
}

@media (max-width: 900px) {
	.sn-toolbar-actions {
		width: 100%;
	}
}

</style>
