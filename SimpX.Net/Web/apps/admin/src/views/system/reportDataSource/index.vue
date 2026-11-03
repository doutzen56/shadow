<template>
	<div class="rds-page">
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
					<h2 class="page-hero__title">报表数据源</h2>
					<p class="page-hero__desc">集中维护报表可连接的数据库：登记名称、数据库类型与连接串，报表配置直接引用这里的数据源取数。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rdsStats.total }}</span>
							<span class="page-hero__stat-k">数据源总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ rdsStats.types }}</span>
							<span class="page-hero__stat-k">数据库类型</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rdsStats.linked }}</span>
							<span class="page-hero__stat-k">已配连接串</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="名称" prop="name">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="数据源名称" clearable @keyup.enter.native="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'sysReportDataSource/page'" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysReportDataSource/add'"> 新增数据源 </el-button>
			</div>
		</div>

		<!-- 卡片列表：全站统一的 .card-grid + .entity-card -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !state.list.length" description="没有匹配的报表数据源" :image-size="160" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent is-primary">
							<!-- 装饰：右下角大号水印图标 -->
							<i class="entity-card__watermark"><el-icon><ele-Coin /></el-icon></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Coin /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name || '未命名数据源' }}</div>
									<div class="entity-card__sub">#{{ row.id }}</div>
								</div>
								<g-sys-dict v-model="row.dbType" code="db_type" size="small" round />
							</div>

							<div class="entity-card__body">
								<div class="card-desc" :title="row.remark">{{ row.remark || '暂无备注' }}</div>

								<!-- 连接串：等宽预览块，点击整块可复制 -->
								<div class="code-preview" :title="row.connectionString" @click="copyConn(row)">{{ row.connectionString || '—' }}</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ formatTime(row.updateTime || row.createTime) }}</span>
								<div class="entity-card__actions">
									<el-button size="small" icon="ele-Edit" text type="primary" @click.stop="handleEdit(row)" v-auth="'sysReportDataSource/update'" :disabled="row.status === 1"> 编辑 </el-button>
									<el-button size="small" icon="ele-DocumentCopy" text type="primary" @click.stop="copyConn(row)"> 复制 </el-button>
									<el-button size="small" icon="ele-Delete" text type="danger" @click.stop="handleDelete(row)" v-auth="'sysReportDataSource/delete'" :disabled="row.status === 1"> 删除 </el-button>
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
				:page-sizes="[12, 24, 48, 96]"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="handlePageSizeChange"
				@current-change="loadData"
			/>
		</el-card>

		<EditReportDataSource ref="editRef" :title="state.title" @handleQuery="handleQuery" />
	</div>
</template>

<!-- 报表开发数据源 -->
<script setup lang="ts" name="sysReportDataSource">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';

import EditReportDataSource from '/@/views/system/reportDataSource/component/editReportDataSource.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportDataSourceApi } from '/@/api-services/system/api';
import { PageReportDataSourceInput } from '/@/api-services/system/models';

const editRef = ref<InstanceType<typeof EditReportDataSource>>();
const state = reactive({
	loading: false,
	list: [] as Array<any>,
	total: 0,
	page: 1,
	pageSize: 12,
	queryParams: {
		name: undefined as string | undefined,
	},
	title: '',
});

// 头图统计：总数取接口 total，类型数 / 已配连接串按当前页统计（不额外请求接口）
const rdsStats = computed(() => {
	const list = state.list ?? [];
	const types = new Set(list.map((x) => x.dbType).filter((x) => x !== undefined && x !== null)).size;
	const linked = list.filter((x) => !!x.connectionString).length;
	return { total: state.total, types, linked };
});

// 时间格式化：接口可能返回字符串，这里兼容 Date 与字符串
const formatTime = (value?: Date | string | null) => {
	if (!value) return '—';
	const d = new Date(value);
	if (Number.isNaN(d.getTime())) return String(value);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

// 加载数据
const loadData = async () => {
	state.loading = true;
	try {
		const params = Object.assign({}, state.queryParams, {
			page: state.page,
			pageSize: state.pageSize,
			field: 'id',
			order: 'desc',
			descStr: 'desc',
		}) as PageReportDataSourceInput;
		const res = await getAPI(SysReportDataSourceApi).apiSysReportDataSourcePagePost(params);
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
	state.queryParams.name = undefined;
	await handleQuery();
};

// 复制连接字符串
const copyConn = async (row: any) => {
	if (!row.connectionString) return;
	try {
		await navigator.clipboard.writeText(row.connectionString);
		ElMessage.success('连接字符串已复制');
	} catch {
		ElMessage.warning('当前浏览器不支持自动复制，请手动选中');
	}
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加报表数据源';
	editRef.value?.openDialog({});
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑报表数据源';
	editRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm('确定删除？', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysReportDataSourceApi).apiSysReportDataSourceDeletePost({ id: row.id });
			await loadData();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 页面初始化
onMounted(async () => {
	await loadData();
});
</script>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.rds-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		margin-top: 14px;
	}
}
</style>
