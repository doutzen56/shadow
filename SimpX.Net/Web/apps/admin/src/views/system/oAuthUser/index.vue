<template>
	<div class="ou-page">
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
					<h2 class="page-hero__title">三方账号</h2>
					<p class="page-hero__desc">查看微信公众号 / 小程序 / QQ / Gitee / 支付宝等第三方登录绑定的账号，可修改资料或解除绑定。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ouStats.total }}</span>
							<span class="page-hero__stat-k">账号总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ ouStats.wechat }}</span>
							<span class="page-hero__stat-k">微信系</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ ouStats.other }}</span>
							<span class="page-hero__stat-k">其他平台</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="昵称" prop="nickName">
					<el-input v-model="state.queryParams.nickName" class="query-input" placeholder="三方昵称" clearable @keyup.enter.native="handleQuery" />
				</el-form-item>
				<el-form-item label="手机号码" prop="mobile">
					<el-input v-model="state.queryParams.mobile" class="query-input" placeholder="手机号码" clearable @keyup.enter.native="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'sysOAuthUser/page'" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 卡片列表：全站统一的 .card-grid + .entity-card -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<el-empty v-if="!state.loading && !state.list.length" description="没有匹配的三方账号" :image-size="160" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent" :class="'is-primary'">
							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-avatar :size="36" :src="row.avatar">{{ (row.nickName || '?').slice(0, 1) }}</el-avatar>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.nickName">{{ row.nickName || '未填写昵称' }}</div>
									<div class="entity-card__sub">
										{{ platformOf(row.platformType) }}
										<template v-if="row.sex === 1"> · 男</template>
										<template v-else-if="row.sex === 2"> · 女</template>
									</div>
								</div>
								<!-- 绑定状态是这张卡唯一的语义差别，交给右上角文字标签承载 -->
								<el-tag size="small" effect="light" :type="row.userId ? 'success' : 'info'">{{ row.userId ? '已绑定' : '未绑定' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="metric-group">
									<div class="metric-item">
										<span>手机</span>
										<b>{{ row.mobile || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>地区</span>
										<b>{{ [row.country, row.province, row.city].filter((x) => !!x).join(' ') || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>OpenId</span>
										<b class="cell-mono" :title="row.openId">{{ row.openId || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>绑定时间</span>
										<b>{{ formatTime(row.createTime) }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ platformOf(row.platformType) }}</span>
								<div class="entity-card__actions">
									<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-auth="'sysOAuthUser/update'" :disabled="row.status === 1"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-auth="'sysOAuthUser/delete'" :disabled="row.status === 1"> 解除绑定 </el-button>
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

		<EditOAuthUser ref="editOAuthUserRef" :title="state.title" @handleQuery="handleQuery" />
	</div>
</template>

<!-- OAuth用户 -->
<script lang="ts" setup name="sysOAuthUser">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';

import EditOAuthUser from '/@/views/system/oAuthUser/component/editOAuthUser.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysOAuthUserApi } from '/@/api-services/system/api';
import { SysOAuthUser, OAuthUserInput } from '/@/api-services/system/models';

const editOAuthUserRef = ref<InstanceType<typeof EditOAuthUser>>();
const state = reactive({
	loading: false,
	list: [] as Array<SysOAuthUser>,
	total: 0,
	page: 1,
	pageSize: 12,
	queryParams: {
		nickName: undefined as string | undefined,
		mobile: undefined as string | undefined,
	},
	title: '',
});

// 平台类型 → 文案 / 配色
const platformOf = (type?: number) => {
	const map: Record<number, string> = { 1: '微信公众号', 2: '微信小程序', 3: 'QQ', 4: 'Gitee', 5: 'Alipay' };
	return map[type ?? 0] ?? '未知平台';
};


// 头图统计：总数取接口 total，微信系/其他按当前页统计（不额外请求接口）
const ouStats = computed(() => {
	const list = state.list ?? [];
	const wechat = list.filter((x) => x.platformType === 1 || x.platformType === 2).length;
	return { total: state.total, wechat, other: list.length - wechat };
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
		}) as OAuthUserInput;
		const res = await getAPI(SysOAuthUserApi).apiSysOAuthUserPagePost(params);
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
	state.queryParams.nickName = undefined;
	state.queryParams.mobile = undefined;
	await handleQuery();
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑三方账号';
	editOAuthUserRef.value?.openDialog(row);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除三方账号：${row.nickName}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysOAuthUserApi).apiSysOAuthUserDeletePost({ id: row.id });
			await loadData();
			ElMessage.success('删除成功');
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
.ou-page {
	position: relative;
	/* 页边距由 .layout-parent 的 padding:5px 提供，这里只补底部 */
	padding-bottom: 4px;

	/* 断点下的单列由 .card-grid-row 的 el-col 响应式负责，无需再写媒体查询 */
	> .query-toolbar {
		margin-top: 14px;
	}
}
</style>
