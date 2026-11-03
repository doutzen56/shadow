<template>
	<div class="ld-page">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
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
					<h2 class="page-hero__title">AD 域配置</h2>
					<p class="page-hero__desc">维护 AD / LDAP 目录连接：搜索基准、绑定 DN 与用户过滤规则，配置好可一键把域账户同步为系统用户。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ldStats.total }}</span>
							<span class="page-hero__stat-k">配置总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ ldStats.enabled }}</span>
							<span class="page-hero__stat-k">启用中</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ldStats.disabled }}</span>
							<span class="page-hero__stat-k">已停用</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="关键字" prop="keyword">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="主机 / 基准 / 绑定 DN" clearable @keyup.enter.native="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'sysLdap/page'" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysLdap/add'"> 新增配置 </el-button>
			</div>
		</div>

		<!-- 卡片列表：全站统一的 .card-grid + .entity-card -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !state.list.length" description="没有匹配的域配置" :image-size="160" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent" :class="'is-primary'">
							<i class="entity-card__watermark ri-links-line"></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Connection /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.host">{{ row.host }}</div>
									<div class="entity-card__sub">端口 {{ row.port ?? 389 }} · Ldap v{{ row.version ?? 3 }}</div>
								</div>
								<el-tag size="small" effect="light" :type="row.status === 1 ? 'success' : 'info'">{{ row.status === 1 ? '启用' : '禁用' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="metric-group">
									<div class="metric-item">
										<span>搜索基准</span>
										<b :title="row.baseDn">{{ row.baseDn || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>绑定 DN</span>
										<b :title="row.bindDn">{{ row.bindDn || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>过滤规则</span>
										<b :title="row.authFilter">{{ row.authFilter || '—' }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<el-button icon="ele-Refresh" text type="primary" @click="handleSync(row)" v-auth="'sysLdap/syncUser'"> 同步域账户 </el-button>
								<div class="entity-card__actions">
									<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'sysLdap/update'"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'sysLdap/delete'"> 删除 </el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				v-if="state.total > 0"
				class="table-pager"
				v-model:current-page="state.page"
				v-model:page-size="state.pageSize"
				:total="state.total"
				:page-sizes="[8, 12, 24, 48]"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="handlePageSizeChange"
				@current-change="loadData"
			/>
		</el-card>

		<EditDialog ref="editRef" :title="state.title" @handleQuery="handleQuery" />
	</div>
</template>

<!-- 域用户管理 -->
<script lang="ts" setup name="sysLdap">
import { computed, defineAsyncComponent, ref, reactive, onMounted } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { SysLdapApi } from '/@/api-services/system/api';
import { SysLdap, PageLdapInput } from '/@/api-services/system/models';

// 异步引用组件
const EditDialog = defineAsyncComponent(() => import('/@/views/system/ldap/component/editLdap.vue'));

const editRef = ref<InstanceType<typeof EditDialog>>();
const state = reactive({
	loading: false,
	list: [] as Array<SysLdap>,
	total: 0,
	page: 1,
	pageSize: 12,
	queryParams: {
		keyword: '',
	},
	title: '',
});

// 头图统计：总数取接口 total，启用/停用按当前页统计（不额外请求接口）
const ldStats = computed(() => {
	const list = state.list ?? [];
	const enabled = list.filter((x) => x.status === 1).length;
	return { total: state.total, enabled, disabled: list.length - enabled };
});

// 加载数据
const loadData = async () => {
	state.loading = true;
	try {
		const params = Object.assign({}, state.queryParams, {
			page: state.page,
			pageSize: state.pageSize,
			field: 'id',
			order: 'asc',
			descStr: 'desc',
		}) as PageLdapInput;
		const res = await getAPI(SysLdapApi).apiSysLdapPagePost(params);
		state.list = res.data.result?.items ?? [];
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

// 查询操作
const handleQuery = async () => {
	state.page = 1;
	await loadData();
};

// 每页条数变化：回到第一页
const handlePageSizeChange = async () => {
	state.page = 1;
	await loadData();
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.keyword = '';
	await handleQuery();
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加系统域登录信息配置';
	editRef.value?.showDialog({ status: 1 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑系统域登录信息配置';
	editRef.value?.showDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定要删除吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysLdapApi).apiSysLdapDeletePost(row);
			await loadData();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 同步域账户
const handleSync = (row: any) => {
	ElMessageBox.confirm(`确定要同步域登录信息配置：${row.host}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysLdapApi).apiSysLdapSyncUserPost({ id: row.id });
			await loadData();
			ElMessage.success('同步成功');
		})
		.catch(() => {});
};

onMounted(() => {
	loadData();
});
</script>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.ld-page {
	position: relative;
	/* 页边距由 .layout-parent 的 padding:5px 提供，这里只补底部 */
	padding-bottom: 4px;

	> .query-toolbar {
		margin-top: 14px;
	}
}
</style>
