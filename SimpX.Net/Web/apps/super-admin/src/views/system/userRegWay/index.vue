<template>
	<div class="rg-page">
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
					<h2 class="page-hero__title">注册方案</h2>
					<p class="page-hero__desc">一套注册方案 = 机构 + 角色 + 职位：新账号注册后按方案自动落到对应的组织与权限位置。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="租户与角色数量按当前页数据统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statTotal }}</span>
							<span class="page-hero__stat-k">方案总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statTenant }}</span>
							<span class="page-hero__stat-k">涉及租户</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statRole }}</span>
							<span class="page-hero__stat-k">默认角色</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询与操作：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :show-message="false" :inline-message="true" label-width="auto" class="query-form">
				<el-form-item label="名称" prop="name">
					<el-input v-model="state.queryParams.name" placeholder="请输入方案名称" clearable class="query-input" @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="租户" prop="tenantId">
					<TenantSelect v-model="state.queryParams.tenantId" clearable class="query-select" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button icon="ele-Search" type="primary" @click="handleSearch" v-auth="'sysUserRegWay/page'" :loading="state.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading">重置</el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" class="rg-add-btn" @click="handleAdd" v-auth="'sysUserRegWay/add'">新增方案</el-button>
			</div>
		</div>

		<!-- 方案卡片：外壳走全局 .entity-card，配置链是本页业务结构，保留自有类 -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !state.list.length" description="没有找到匹配的注册方案" :image-size="140" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent is-clickable is-primary" @dblclick="onCardDblclick(row)">
							<i class="entity-card__watermark ri-user-add-line"></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-OfficeBuilding /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name || '未命名方案' }}</div>
									<div class="entity-card__sub">排序 {{ row.orderNo ?? '—' }} · {{ row.tenantName || '默认租户' }}</div>
								</div>
								<!-- 账号类型是这张卡唯一的语义差别，交给右上角文字标签承载 -->
								<el-tag size="small" effect="light" :type="acctTone(row)">{{ acctLabel(row) }}</el-tag>
							</div>

							<div class="entity-card__body">
								<!-- 配置链：机构 → 角色 → 职位 -->
								<div class="rg-chain">
									<div class="rg-node" :title="row.orgName || ''">
										<em>默认机构</em>
										<b>{{ row.orgName || '—' }}</b>
									</div>
									<el-icon class="rg-arrow"><ele-Right /></el-icon>
									<div class="rg-node" :title="row.roleName || ''">
										<em>默认角色</em>
										<b>{{ row.roleName || '—' }}</b>
									</div>
									<el-icon class="rg-arrow"><ele-Right /></el-icon>
									<div class="rg-node" :title="row.posName || ''">
										<em>默认职位</em>
										<b>{{ row.posName || '—' }}</b>
									</div>
								</div>

								<div class="card-desc" :title="row.remark || ''">{{ row.remark || '暂无备注' }}</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ formatDateTime(row.updateTime || row.createTime, 'YYYY-mm-dd HH:MM') }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysUserRegWay/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysUserRegWay/delete'" @click.stop="handleDelete(row)" />
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

		<EditUserRegWay ref="editUserRegWayRef" :title="state.title" @handleQuery="loadList" />
	</div>
</template>

<!-- 用户注册方案 -->
<script lang="ts" setup name="sysUserRegWay">
import { computed, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';

import { auth } from '/@/utils/authFunction';
import { formatDateTime } from '/@/utils/format';
import TenantSelect from '/@/views/system/tenant/component/tenantSelect.vue';
import EditUserRegWay from './component/editUserRegWay.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserRegWayApi } from '/@/api-services/system/api';
import { PageUserRegWayOutput, PageUserRegWayInput } from '/@/api-services/system/models';

/** 账号类型（与后端 AccountTypeEnum 一致，卡片上只用主题色系区分） */
/** 账号类型：tone 直接给 el-tag 的 type，不另造配色（全站卡片语义色统一走 Element） */
const ACCT_MAP: Record<number, { label: string; tone: 'danger' | 'warning' | 'info' }> = {
	999: { label: '超级管理员', tone: 'danger' },
	888: { label: '系统管理员', tone: 'warning' },
	777: { label: '普通账号', tone: 'info' },
};

/** 生成的 PageUserRegWayOutput 未包含 Name 字段（后端实体有），这里补上，避免类型报错 */
type RegWayRow = PageUserRegWayOutput & { name?: string };

const editUserRegWayRef = ref<InstanceType<typeof EditUserRegWay>>();

const state = reactive({
	loading: false,
	title: '',
	queryParams: {
		name: undefined as string | undefined,
		tenantId: undefined as number | undefined,
	},
	list: [] as RegWayRow[],
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
		} as PageUserRegWayInput;
		const res = await getAPI(SysUserRegWayApi).apiSysUserRegWayPagePost(params);
		state.list = (res.data.result?.items ?? []) as RegWayRow[];
		pager.total = Number(res.data.result?.total ?? 0);
	} catch {
		state.list = [];
		pager.total = 0;
		ElMessage.error('注册方案加载失败');
	} finally {
		state.loading = false;
	}
};

const handleSearch = () => {
	pager.page = 1;
	loadList();
};

const resetQuery = () => {
	state.queryParams.name = undefined;
	state.queryParams.tenantId = undefined;
	pager.page = 1;
	loadList();
};

const onSizeChange = () => {
	pager.page = 1;
	loadList();
};

/* ------------------------------ 展示辅助 ------------------------------ */
const statTotal = computed(() => pager.total);
// 租户/角色数按当前页数据统计，避免为统计再打一次接口
const statTenant = computed(() => new Set(state.list.map((r) => r.tenantName || '__default__')).size);
const statRole = computed(() => new Set(state.list.map((r) => r.roleName || '__none__')).size);

const acctLabel = (row: RegWayRow) => ACCT_MAP[Number(row.accountType)]?.label ?? '未指定';
const acctTone = (row: RegWayRow) => ACCT_MAP[Number(row.accountType)]?.tone ?? 'info';

/* ------------------------------ 增改删 ------------------------------ */
const handleAdd = () => {
	state.title = '增加注册方案';
	editUserRegWayRef.value?.openDialog({ status: 1, orderNo: 100 });
};

const handleEdit = (row: RegWayRow) => {
	state.title = '编辑注册方案';
	editUserRegWayRef.value?.openDialog(row);
};

const onCardDblclick = (row: RegWayRow) => {
	if (auth('sysUserRegWay/update')) handleEdit(row);
};

const handleDelete = (row: RegWayRow) => {
	ElMessageBox.confirm(`确定删除注册方案「${row.name || ''}」?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysUserRegWayApi).apiSysUserRegWayDeletePost({ id: row.id });
			await loadList();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 页面初始化
loadList();
</script>

<style lang="scss" scoped>

/* 卡片外壳与查询区走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉。下面只保留本页特有的业务结构：机构 → 角色 → 职位 配置链。 */

.rg-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		flex-shrink: 0;
		margin-top: 14px;
	}
}



/* 新增方案：主操作，加一道扫光让它在工具条里先被看见 */

.rg-add-btn {
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

.rg-chain {
	display: grid;
	grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr) auto minmax(0, 1fr);
	align-items: center;
	gap: 6px;
	padding: 10px 12px;
	border-radius: 12px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-extra-light, var(--el-border-color-lighter));
}

.rg-node {
	min-width: 0;
	text-align: center;

	em {
		display: block;
		font-style: normal;
		font-size: 10.5px;
		letter-spacing: 0.3px;
		color: var(--el-text-color-placeholder);
	}

	b {
		display: block;
		margin-top: 2px;
		font-size: 12.5px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.rg-arrow {
	flex-shrink: 0;
	font-size: 13px;
	color: color-mix(in srgb, var(--el-color-primary) 55%, var(--el-text-color-placeholder));
}

.rg-remark {
	position: relative;
	display: flex;
	align-items: flex-start;
	gap: 5px;
	margin: 0;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-text-color-secondary);
	min-height: 19px;
}

.rg-remark :deep(.el-icon) {
	flex-shrink: 0;
	margin-top: 2px;
	opacity: 0.7;
}

.rg-foot {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	padding-top: 10px;
	border-top: 1px dashed var(--el-border-color-lighter);
}

.rg-meta {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	font-variant-numeric: tabular-nums;
}

.rg-ops {
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

.rg-empty {
	margin-top: 14px;
	padding: 32px 0;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px dashed var(--el-border-color-lighter);
}

.rg-pager {
	display: flex;
	justify-content: flex-end;
	margin-top: 16px;
	padding: 10px 12px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}



/* ===================== 动画 ===================== */

@keyframes rgRise {
	from {
		opacity: 0;
		transform: translateY(12px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@media (max-width: 900px) {
	.rg-toolbar-actions {
		width: 100%;
	}

	.rg-chain {
		grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
		row-gap: 10px;

		/* 窄屏时第三个节点换行，箭头仍留在第二行首 */
		.rg-node:nth-of-type(3) {
			grid-column: 1 / -1;
		}

		.rg-arrow:nth-of-type(2) {
			display: none;
		}
	}
}

</style>
