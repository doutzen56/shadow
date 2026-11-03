<template>
	<div class="cg-page">
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
					<h2 class="page-hero__title">代码生成</h2>
					<p class="page-hero__desc">按业务配置选择数据库表与模板场景，一键生成前后端代码并打包下载，重启项目即可加载最新代码。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ cgStats.total }}</span>
							<span class="page-hero__stat-k">配置总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ cgStats.entity }}</span>
							<span class="page-hero__stat-k">主表实体</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ cgStats.namespace }}</span>
							<span class="page-hero__stat-k">命名空间</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="业务名" prop="busName">
					<el-input v-model="state.queryParams.busName" class="query-input" placeholder="业务名" clearable @keyup.enter="handleQuery" />
				</el-form-item>
				<el-form-item label="数据库表名" prop="tableName">
					<el-input v-model="state.queryParams.tableName" class="query-input" placeholder="数据库表名" clearable @keyup.enter="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd"> 新增配置 </el-button>
				<el-button icon="ele-MagicStick" @click="handleSample"> 示例模板 </el-button>
			</div>
		</div>

		<!-- 卡片列表：全站统一的 .card-grid + .entity-card -->
		<el-card class="full-table" shadow="hover">
			<div class="card-grid" v-loading="state.loading">
				<!-- 空状态保留自绘引导：这页第一次进来是空的，需要告诉人「下一步做什么」 -->
				<div v-if="!state.loading && state.list.length === 0" class="cg-empty">
					<div class="cg-empty-box">
						<span class="cg-empty-ico"><el-icon><ele-MagicStick /></el-icon></span>
						<p class="cg-empty-title">还没有代码生成配置</p>
						<p class="cg-empty-desc">点「新增配置」→ 选库选表 → 挑一个场景 → 保存并生成，三步就能拿到一整套前后端代码</p>
						<div class="cg-empty-acts">
							<el-button type="primary" icon="ele-Plus" @click="handleAdd">新增配置</el-button>
							<el-button icon="ele-MagicStick" @click="handleSample">用示例模板</el-button>
						</div>
					</div>
				</div>

				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.list" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent is-primary">
							<i class="entity-card__watermark ri-code-box-line"></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Cpu /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.busName">{{ row.busName || '未命名业务' }}</div>
									<div class="entity-card__sub" :title="row.nameSpace">{{ row.nameSpace || '—' }}</div>
								</div>
								<!-- 生成方式：字典组件直接输出标签，外面不再套带底色的 span -->
								<g-sys-dict code="CodeGenMethodEnum" v-model="row.generateMethod" size="small" round />
							</div>

							<div class="entity-card__body">
								<div class="metric-group">
									<div class="metric-item">
										<span>主表实体</span>
										<b class="cell-mono" :title="row.tableList?.[0]?.entityName">{{ row.tableList?.[0]?.entityName || '—' }}</b>
									</div>
									<div class="metric-item">
										<span>模板场景</span>
										<b><g-sys-dict code="CodeGenSceneEnum" v-model="row.scene" /></b>
									</div>
									<div class="metric-item">
										<span>作者</span>
										<b>{{ row.authorName || '—' }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<el-button icon="ele-Position" text type="primary" @click="handleGenerate(row)" v-reclick="1000"> 生成 </el-button>
								<div class="entity-card__actions">
									<el-button icon="ele-Camera" text type="primary" @click="handlePreview(row)" v-reclick="1000"> 预览 </el-button>
									<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" v-reclick="1000"> 编辑 </el-button>
									<el-button icon="ele-CopyDocument" text type="primary" @click="handleCopy(row)" v-reclick="1000"> 复制 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" v-reclick="1000"> 删除 </el-button>
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

		<EditCodeGenDialog ref="EditCodeGenRef" @handleQuery="handleQuery" />
		<SampleDialog ref="sampleRef" @handleQuery="handleQuery" />
		<PreviewDialog :title="state.title" ref="previewRef" />
		<GenerateResultDialog ref="resultRef" />
	</div>
</template>

<!-- 代码生成 -->
<script lang="ts" setup name="sysCodeGen">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage, ElNotification } from 'element-plus';

import EditCodeGenDialog from './component/editCodeGenDialog.vue';
import GenerateResultDialog from './component/generateResultDialog.vue';
import SampleDialog from './component/sampleDialog.vue';
import { useCodeGenStore } from './component/codeGenStore';

import { getAPI } from '/@/utils/axios-utils';
import { SysCodeGenApi } from '/@/api-services/system/api';
import { SysCodeGen, PageCodeGenInput } from '/@/api-services/system/models';
import { downloadByUrl } from '/@/utils/download';

const PreviewDialog = defineAsyncComponent(() => import('./component/previewDialog.vue'));
const store = useCodeGenStore();
const previewRef = ref<InstanceType<typeof PreviewDialog>>();
const resultRef = ref<InstanceType<typeof GenerateResultDialog>>();
const EditCodeGenRef = ref<InstanceType<typeof EditCodeGenDialog>>();
const sampleRef = ref<InstanceType<typeof SampleDialog>>();
const state = reactive({
	loading: false,
	list: [] as Array<SysCodeGen>,
	total: 0,
	page: 1,
	pageSize: 12,
	dbData: [] as any,
	configId: '',
	tableName: '',
	queryParams: {
		busName: undefined as string | undefined,
		tableName: undefined as string | undefined,
	},
	title: '',
	applicationNamespaces: [] as Array<string>,
});

// 头图统计：总数取接口 total，主表实体 / 命名空间按当前页统计（不额外请求接口）
const cgStats = computed(() => {
	const list = state.list ?? [];
	const entity = list.filter((x) => !!x.tableList?.[0]?.entityName).length;
	const namespace = new Set(list.map((x) => x.nameSpace).filter((x) => !!x)).size;
	return { total: state.total, entity, namespace };
});

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
		}) as PageCodeGenInput;
		const res = await getAPI(SysCodeGenApi).apiSysCodeGenPagePost(params);
		state.list = res.data.result?.items ?? [];
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

// 查询操作
// opt.generate：从「保存并生成」按钮回来时，保存完直接接着生成
// opt.openBusName：从「示例模板」创建回来时，按业务名找到新配置并直接打开，省一次查找
const handleQuery = async (opt?: any) => {
	state.page = 1;
	await loadData();
	if (opt?.generate && state.list.length > 0) await handleGenerate(state.list[0]);
	if (opt?.openBusName) {
		const row = state.list.find((x) => x.busName === opt.openBusName);
		if (row) handleEdit(row);
	}
};

// 每页条数变化：回到第一页
const handlePageSizeChange = async () => {
	state.page = 1;
	await loadData();
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.busName = undefined;
	state.queryParams.tableName = undefined;
	await handleQuery();
};

// 打开新增页面
const handleAdd = () => {
	EditCodeGenRef.value?.openDialog({
		authorName: 'SimpX',
		generateMethod: 200,
		printType: 1,
		menuIcon: 'ele-Menu',
		pagePath: 'main',
		configObj: {},
		nameSpace: state.applicationNamespaces[0],
		generateMenu: false,
		isApiService: false,
		scene: 1000,
	});
};

// 打开示例模板弹窗
const handleSample = () => {
	sampleRef.value?.openDialog();
};

// 打开编辑页面
const handleEdit = (row: any) => {
	EditCodeGenRef.value?.openDialog(row);
};

// 打开复制页面
const handleCopy = async (row: any) => {
	const data = await store.getDetail(row.id);
	if (data) {
		data.moduleName = undefined;
		data.tableList?.forEach((e) => {
			e.columnList?.forEach((c) => delete c.id);
			delete e.id;
		});
		delete data.id;
	}
	EditCodeGenRef.value?.openDialog(data);
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysCodeGenApi).apiSysCodeGenDeletePost({ id: row.id });
			await loadData();
			ElMessage.success('操作成功');
		})
		.catch(() => {});
};

// 开始生成代码
const handleGenerate = async (row: any) => {
	try {
		await ElMessageBox.confirm(`确定要生成：${row.busName}?`, '提示', {
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			type: 'warning',
		});
	} catch {
		return; // 取消生成
	}

	const res = await getAPI(SysCodeGenApi).apiSysCodeGenGeneratePost(row);
	const result: any = res.data.result ?? {};
	if (result.url) downloadByUrl({ url: result.url }); // 下载模式：直接下载压缩包
	await loadData();

	// 生成结果：概览 + 文件清单 + 风险提醒 + 下一步
	resultRef.value?.openDialog(result, row.busName);
	ElMessage.success('生成成功');
};

// 预览代码
const handlePreview = (row: any) => {
	state.title = '预览代码';
	previewRef.value?.openDialog(row);
};

// 页面初始化
onMounted(async () => {
	store.init();
	await loadData();
});
</script>

<style lang="scss" scoped>
/* 卡片外壳与查询区走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉。
   下面只保留本页特有的空状态引导（这页第一次进来是空的，需要告诉人下一步做什么）。 */
.cg-page {
	position: relative;
	padding-bottom: 4px;

	> .query-toolbar {
		margin-top: 14px;
	}
}

.cg-empty {
	grid-column: 1 / -1;
}

.cg-empty-box {
	display: flex;
	flex-direction: column;
	align-items: center;
	padding: 46px 20px;
	border: 1px dashed var(--el-border-color);
	border-radius: 14px;
	background: var(--el-bg-color);
}

.cg-empty-ico {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 52px;
	height: 52px;
	border-radius: 14px;
	font-size: 25px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.cg-empty-title {
	margin-top: 14px;
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.cg-empty-desc {
	margin: 6px 0 16px;
	max-width: 520px;
	text-align: center;
	font-size: 12.5px;
	line-height: 1.8;
	color: var(--el-text-color-secondary);
}

.cg-empty-acts {
	display: flex;
	gap: 10px;

	/* EP 相邻按钮自带 margin-left:12px，会和 gap 叠出不等距 */
	:deep(.el-button + .el-button) {
		margin-left: 0;
	}
}
</style>
