<template>
	<div class="pg-page">
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
					<h2 class="page-hero__title">动态插件</h2>
					<p class="page-hero__desc">在线编写 C# 代码并即时编译为动态程序集，无需重启即可扩展平台功能。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="启用 / 禁用数量按当前已加载数据实时统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ cardPager.total }}</span>
							<span class="page-hero__stat-k">插件总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ statEnabled }}</span>
							<span class="page-hero__stat-k">已启用</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-off">{{ statDisabled }}</span>
							<span class="page-hero__stat-k">已禁用</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 搜索与操作：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="功能名称" prop="name">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="请输入功能名称" clearable @keyup.enter.native="handleSearch">
						<template #prefix
							><el-icon class="pg-input-ico"><ele-Search /></el-icon
						></template>
					</el-input>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button icon="ele-Search" type="primary" @click="handleSearch" v-auth="'sysPlugin/page'" :loading="options.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading">重置</el-button>
				</el-button-group>
				<el-button icon="ele-Plus" type="primary" class="pg-add-btn" @click="handleAdd" v-auth="'sysPlugin/add'">新增插件</el-button>
			</div>
		</div>

		<!-- 卡片视图：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="options.loading">
				<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的动态插件" :image-size="140" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable"
							:class="'is-primary'"
							@dblclick="onCardDblclick(row)"
						>
							<i class="entity-card__watermark"><el-icon><ele-Cpu /></el-icon></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-MagicStick /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
									<div class="entity-card__sub">{{ row.assemblyName || '未指定程序集' }}</div>
								</div>
								<el-tag size="small" effect="light" :type="Number(row.status) === 1 ? 'success' : 'info'">
									{{ Number(row.status) === 1 ? '启用' : '禁用' }}
								</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc" :title="row.remark">{{ row.remark || '暂无备注' }}</div>

								<div class="code-preview" v-if="codePreview(row)">{{ codePreview(row) }}</div>

								<div class="tag-group">
									<el-tag size="small" effect="light" type="primary" :title="row.assemblyName">
										<el-icon><ele-Coin /></el-icon>
										{{ row.assemblyName || '—' }}
									</el-tag>
									<el-tag size="small" effect="light" type="info">
										<el-icon><ele-Document /></el-icon>
										代码 {{ codeSize(row) }}
									</el-tag>
									<el-tag size="small" effect="plain" type="info">
										<el-icon><ele-Top /></el-icon>
										排序 #{{ row.orderNo ?? '—' }}
									</el-tag>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub"><ModifyRecord :data="row" /> · {{ formatTime(row.updateTime || row.createTime) }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysPlugin/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysPlugin/delete'" @click.stop="handleDelete(row)" />
									</el-tooltip>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				v-if="cardPager.total > 0"
				class="table-pager"
				v-model:current-page="cardPager.page"
				v-model:page-size="cardPager.pageSize"
				:page-sizes="[12, 24, 48, 96]"
				:total="cardPager.total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onCardSizeChange"
				@current-change="loadCardList"
			/>
		</el-card>

		<EditPlugin ref="editPluginRef" :title="state.title" @handleQuery="refresh" />
	</div>
</template>

<!-- 动态插件 -->
<script lang="ts" setup name="sysPlugin">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, dayjs } from 'element-plus';
import { useVxeTable, useGridPageParam } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditPlugin from '/@/views/system/plugin/component/editPlugin.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysPluginApi } from '/@/api-services/system/api';
import { SysPlugin, PagePluginInput } from '/@/api-services/system/models';

const editPluginRef = ref<InstanceType<typeof EditPlugin>>();
const { param: localPageParam } = useGridPageParam('sysPlugin', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
	/** 卡片视图数据 */
	cardList: [] as SysPlugin[],
});

// 卡片视图分页
const cardPager = reactive({ page: 1, pageSize: 24, total: 0 });

// 表格参数配置（卡片视图仅借用其 loading 状态）
const options = useVxeTable<SysPlugin>(
	{
		id: 'sysPlugin',
		name: '插件信息',
		columns: [],
	},
	{
		// 不自动加载：卡片视图自己取数
		proxyConfig: { enabled: false },
	}
);

// 卡片视图取数
const loadCardList = async () => {
	options.loading = true;
	try {
		const params = {
			...state.queryParams,
			page: cardPager.page,
			pageSize: cardPager.pageSize,
			field: localPageParam.defaultSort.field,
			order: localPageParam.defaultSort.order,
			descStr: 'desc',
		} as PagePluginInput;
		const res = await getAPI(SysPluginApi).apiSysPluginPagePost(params);
		state.cardList = (res.data.result?.items ?? []) as SysPlugin[];
		cardPager.total = Number(res.data.result?.total ?? 0);
	} finally {
		options.loading = false;
	}
};

// 刷新
const refresh = async () => {
	await loadCardList();
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	await loadCardList();
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	cardPager.page = 1;
	await loadCardList();
};

// 每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加动态插件';
	editPluginRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑动态插件';
	editPluginRef.value?.openDialog(row);
};

// 卡片双击进入编辑
const onCardDblclick = (row: any) => {
	if (auth('sysPlugin/update')) handleEdit(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除动态插件：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysPluginApi).apiSysPluginDeletePost({ id: row.id });
			await refresh();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

/* ------------------------------ 展示辅助 ------------------------------ */
const statEnabled = computed(() => state.cardList.filter((r) => Number(r.status) === 1).length);
const statDisabled = computed(() => state.cardList.length - statEnabled.value);

// 代码体积
const codeSize = (row: SysPlugin) => {
	const len = row.csharpCode?.length ?? 0;
	return len >= 1024 ? `${(len / 1024).toFixed(1)} KB` : `${len} 字符`;
};

// 代码预览：取首行非空内容，超出截断
const codePreview = (row: SysPlugin) => {
	const raw = row.csharpCode ?? '';
	if (!raw.trim()) return '';
	const line = raw
		.split(/\r?\n/)
		.map((s) => s.trim())
		.find((s) => s.length > 0);
	if (!line) return '';
	return line.length > 120 ? `${line.slice(0, 120)}…` : line;
};

const formatTime = (val: any) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm') : String(val);
};

onMounted(loadCardList);
</script>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.pg-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		flex-shrink: 0;
		margin-top: 14px;
	}
}

/* 搜索框前置放大镜：弱化到占位色，不与输入文字抢对比度 */
.pg-input-ico {
	color: var(--el-text-color-placeholder);
}

/* 新增插件：主操作，加一道扫光让它在工具条里先被看见 */
.pg-add-btn {
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
</style>
