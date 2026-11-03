<template>
	<div class="rc-page">
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
					<h2 class="page-hero__title">报表配置</h2>
					<p class="page-hero__desc">按分组组织报表，支持 SQL 脚本与 API 接口两种取数方式，可在线预览、复制并一键发布到系统菜单。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rcStats.total }}</span>
							<span class="page-hero__stat-k">报表总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ rcStats.sql }}</span>
							<span class="page-hero__stat-k">SQL 脚本</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rcStats.api }}</span>
							<span class="page-hero__stat-k">API 接口</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="rc-body">
			<splitpanes class="default-theme">
				<!-- ===== 左：报表分组 ===== -->
				<pane size="16" class="pane-left">
					<ReportGroupPanel @nodeClick="handleGroupNodeClick" />
				</pane>

				<!-- ===== 右：报表卡片 ===== -->
				<pane size="84" class="pane-right">
					<div class="rc-right">
						<!-- 查询区：全站统一的 .query-toolbar -->
						<div class="query-toolbar">
							<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" label-position="left" class="query-form">
								<el-form-item label="名称" prop="name">
									<el-input v-model="state.queryParams.name" class="query-input" placeholder="报表名称" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-form>
							<div class="query-actions">
								<el-button-group>
									<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'sysReportConfig/page'" :loading="state.loading"> 查询 </el-button>
									<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
								</el-button-group>
								<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysReportConfig/add'"> 新增报表 </el-button>
							</div>
						</div>

						<!-- 卡片列表：全站统一的 .card-grid + .entity-card -->
						<el-card class="full-table" shadow="hover">
							<div class="card-grid" v-loading="state.loading">
								<el-empty v-if="!state.loading && !state.list.length" description="没有匹配的报表配置" :image-size="160" />
								<el-row v-else :gutter="12" class="card-grid-row">
									<el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
										<el-card shadow="never" class="entity-card entity-card--accent" :class="'is-primary'">
											<!-- 装饰：右下角大号水印图标 -->
											<i class="entity-card__watermark"><el-icon><ele-DataBoard /></el-icon></i>

											<div class="entity-card__head">
												<div class="entity-card__icon">
													<el-icon><ele-DataBoard /></el-icon>
												</div>
												<div class="entity-card__title-wrap">
													<div class="entity-card__title" :title="row.name">{{ row.name || '未命名报表' }}</div>
													<div class="entity-card__sub">#{{ row.id }}</div>
												</div>
												<g-sys-dict v-model="row.dsType" code="ReportConfigDsTypeEnum" size="small" round />
											</div>

											<div class="entity-card__body">
												<div class="card-desc" :title="row.description">{{ row.description || '暂无描述' }}</div>

												<div class="metric-group">
													<div class="metric-item">
														<span>数据源</span>
														<b :title="dataSourceName(row)">{{ dataSourceName(row) }}</b>
													</div>
													<div class="metric-item">
														<span>报表分组</span>
														<b :title="row.groupName">{{ row.groupName || '—' }}</b>
													</div>
													<div class="metric-item">
														<span>创建人</span>
														<b>{{ row.createUserName || '—' }}</b>
													</div>
													<div class="metric-item">
														<span>状态</span>
														<b>
															<el-tag size="small" round :type="row.status === 1 ? 'success' : 'info'" effect="light">
																{{ row.status === 1 ? '已发布' : '未发布' }}
															</el-tag>
														</b>
													</div>
												</div>
											</div>

											<div class="entity-card__foot">
												<span class="cell-sub" :title="formatTime(row.updateTime || row.createTime)">{{ formatRelative(row.updateTime || row.createTime) }}</span>
												<div class="entity-card__actions">
													<el-button size="small" icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'sysReportConfig/update'" :disabled="row.status === 1"> 编辑 </el-button>
													<el-button size="small" icon="ele-DocumentCopy" text type="primary" @click="copy(row)" v-auth="'sysReportConfig/add'"> 复制 </el-button>
													<el-button size="small" icon="ele-ScaleToOriginal" text type="primary" @click="preview(row)"> 预览 </el-button>
													<el-button size="small" icon="ele-Position" text type="primary" @click="publicToMenu(row)"> 发布 </el-button>
													<el-button size="small" icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'sysReportConfig/delete'" :disabled="row.status === 1"> 删除 </el-button>
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
					</div>
				</pane>
			</splitpanes>
		</div>

		<EditReportConfig ref="editRef" :title="state.title" @handleQuery="handleQuery" />
		<EditMenu ref="editMenuRef" :title="state.title" :menuData="state.menuData" @handleQuery="handleEditMenuQuery" />
	</div>
</template>

<!-- 报表开发配置 -->
<script setup lang="ts" name="sysReportConfig">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useRouter } from 'vue-router';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import ReportGroupPanel from './component/reportGroupPanel/index.vue';
import EditReportConfig from '/@/views/system/reportConfig/component/editReportConfig.vue';
import EditMenu from '/@/views/system/menu/component/editMenu.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi, SysReportConfigApi, SysReportDataSourceApi } from '/@/api-services/system/api';
import { MenuTypeEnum, PageReportConfigInput, ReportConfigDsTypeEnum, ReportDataSourceOutput, StatusEnum, SysMenu, SysReportGroup } from '/@/api-services/system/models';

const router = useRouter();

const editRef = ref<InstanceType<typeof EditReportConfig>>();
const editMenuRef = ref<InstanceType<typeof EditMenu>>();
const state = reactive({
	loading: false,
	list: [] as Array<any>,
	total: 0,
	page: 1,
	pageSize: 12,
	queryParams: {
		name: undefined as string | undefined,
		groupId: 0,
	} as EmptyObjectType,
	title: '',
	/** 已选择的分组Id */
	selectedGroupId: 0,
	/** 数据源列表数据 */
	dataSourceList: [] as ReportDataSourceOutput[],
	/** 菜单树数据 */
	menuData: [] as Array<SysMenu>,
});

// 头图统计：总数取接口 total，SQL / API 两种取数方式按当前页统计
const rcStats = computed(() => {
	const list = state.list ?? [];
	const sql = list.filter((x) => x.dsType === ReportConfigDsTypeEnum.NUMBER_0).length;
	return { total: state.total, sql, api: list.length - sql };
});

// 数据源名称：列表里存的是数据源 Id，换成名称显示
const dataSourceName = (row: any) => {
	return state.dataSourceList.find((r) => r.id == row.dataSource)?.name || '—';
};

// 时间格式化：接口可能返回字符串，这里兼容 Date 与字符串
const formatTime = (value?: Date | string | null) => {
	if (!value) return '—';
	const d = new Date(value);
	if (Number.isNaN(d.getTime())) return String(value);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

/**
 * 卡片底部用的相对时间：底部要平铺 5 个按钮，
 * 完整的 "YYYY-MM-DD HH:mm" 会被挤到几乎看不见，故只显示「3 天前」这类短串，
 * 完整时间放进 title，鼠标悬停可见。
 */
const formatRelative = (value?: Date | string | null) => {
	if (!value) return '—';
	const d = new Date(value);
	if (Number.isNaN(d.getTime())) return String(value);
	const diff = Date.now() - d.getTime();
	const min = Math.floor(diff / 60000);
	if (min < 1) return '刚刚';
	if (min < 60) return `${min} 分钟前`;
	const hour = Math.floor(min / 60);
	if (hour < 24) return `${hour} 小时前`;
	const day = Math.floor(hour / 24);
	if (day < 30) return `${day} 天前`;
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};

// 加载数据
const loadData = async () => {
	state.loading = true;
	try {
		state.queryParams.groupId = state.selectedGroupId;
		const params = Object.assign({}, state.queryParams, {
			page: state.page,
			pageSize: state.pageSize,
			field: 'id',
			order: 'desc',
			descStr: 'desc',
		}) as PageReportConfigInput;
		const res = await getAPI(SysReportConfigApi).apiSysReportConfigPagePost(params);
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

// 页面初始化
onMounted(() => {
	getAPI(SysReportDataSourceApi)
		.apiSysReportDataSourceDataSourceListGet()
		.then((result) => {
			if (result.data.type !== 'success') {
				return;
			}
			state.dataSourceList = result.data.result!;
		});

	loadData();
});

// 打开新增页面
const handleAdd = () => {
	state.title = '添加报表配置';
	editRef.value?.openDialog({ dsType: ReportConfigDsTypeEnum.NUMBER_0 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑报表配置';
	editRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm('确定删除？', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysReportConfigApi).apiSysReportConfigDeletePost({ id: row.id });
		await loadData();
		ElMessage.success('删除成功');
	});
};

const handleGroupNodeClick = (g: SysReportGroup) => {
	state.selectedGroupId = g.id!;
	handleQuery();
};

/** 复制 */
const copy = (row: any) => {
	ElMessageBox.confirm('确定复制？', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysReportConfigApi).apiSysReportConfigCopyPost({ id: row.id });
		await loadData();
		ElMessage.success('复制成功');
	});
};

/** 预览 */
const preview = (row: any) => {
	router.push(`/report/view/${row.id}/${encodeURIComponent(row.name)}`);
};

/** 发布到菜单 */
const publicToMenu = async (row: any) => {
	if (state.menuData.length == 0) {
		await loadMenuList();
	}
	state.title = '发布到菜单';
	editMenuRef.value?.openDialog({
		id: 0,
		type: MenuTypeEnum.NUMBER_2,
		name: '',
		title: row.name,
		icon: 'ele-Tickets',
		path: `/report/view/${row.id}/${row.name}`,
		orderNo: 100,
		isHide: false,
		isKeepAlive: true,
		isAffix: false,
		isIframe: false,
		status: StatusEnum.NUMBER_1,
	} as SysMenu);
};

/** 处理发布菜单后查询 */
const handleEditMenuQuery = () => {
	loadMenuList();
};

/** 获取菜单列表 */
const loadMenuList = () => {
	return getAPI(SysMenuApi)
		.apiSysMenuListGet()
		.then((res) => {
			state.menuData = res.data.result!;
			return res;
		});
};
</script>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.rc-page {
	/* 页边距由 .layout-parent 的 padding:5px 提供，这里只补底部；
	   flex 列 + min-height:0 才能把剩余高度交给下面的 splitpanes */
	position: relative;
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;
	padding-bottom: 4px;
	overflow: hidden;
}

.rc-body {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;

	:deep(.splitpanes) {
		width: 100%;
		height: 100%;
	}
}

.pane-left {
	display: flex;
	flex-direction: column;

	/* 分组面板卡片撑满左栏（splitpanes 的 pane 自带 height:100%） */
	:deep(.el-card) {
		height: 100%;
		border-radius: 12px;
	}
}

.pane-right {
	display: flex;
	flex-direction: column;
}

.rc-right {
	height: 100%;
	display: flex;
	flex-direction: column;
	min-height: 0;
}

/* 右栏：查询区固定高度，剩下的全给卡片墙 */
.rc-right > .query-toolbar {
	flex-shrink: 0;
	// margin-top: 14px;
}

.rc-right > .full-table {
	// margin-top: 12px;
}
</style>
