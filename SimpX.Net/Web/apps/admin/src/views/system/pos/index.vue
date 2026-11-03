<template>
	<div class="sys-pos-container">
		<!-- 查询区：全站统一的 .query-toolbar（与 AI 智能体 · 工具管理同一套视觉语言） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true" label-width="auto" label-position="left" class="query-form">
				<el-form-item label="职位名称" prop="name">
					<el-input v-model="state.queryParams.name" class="query-input" placeholder="请输入职位名称" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="职位编码" prop="code">
					<el-input v-model="state.queryParams.code" class="query-input" placeholder="请输入职位编码" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
			</el-form>

			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleSearch" v-auth="'sysPos/page'" :loading="options.loading">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading">重置</el-button>
				</el-button-group>
				<!-- 卡片视图没有 vxe 工具栏，操作按钮统一挂在这里 -->
				<template v-if="state.viewMode === 'card'">
					<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysPos/add'">新增</el-button>
					<el-button-group>
						<el-button plain icon="ele-Upload" @click="importData" v-auth="'sysPos/import'">导入</el-button>
						<el-button plain icon="ele-Download" @click="exportData" v-auth="'sysPos/export'">导出</el-button>
						<el-button plain icon="ele-FolderOpened" @click="downloadTemplate">下载模板</el-button>
					</el-button-group>
				</template>
				<!-- 视图切换：用 element 原生按钮组，尺寸与查询/重置一致 -->
				<el-button-group>
					<el-button type="primary" :plain="state.viewMode !== 'card'" icon="ele-Grid" @click="switchView('card')">卡片</el-button>
					<el-button type="primary" :plain="state.viewMode !== 'table'" icon="ele-List" @click="switchView('table')">表格</el-button>
				</el-button-group>
			</div>
		</div>

		<!-- 卡片视图（默认）：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<el-card v-if="state.viewMode === 'card'" class="full-table" shadow="hover">
			<div class="card-grid" v-loading="options.loading">
				<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的职位" :image-size="120" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
						<el-card
							shadow="never"
							class="entity-card entity-card--accent is-clickable"
							:class="'is-primary'"
							@dblclick="onCardDblclick(row)"
						>
							<!-- 背景水印图标：全局 .entity-card__watermark 统一压在右上角 -->
							<i class="entity-card__watermark"><el-icon><ele-Postcard /></el-icon></i>

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<el-icon><ele-Briefcase /></el-icon>
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
									<div class="entity-card__sub">{{ row.code || '—' }}</div>
								</div>
								<el-tag size="small" effect="light" :type="Number(row.status) === 1 ? 'success' : 'info'">
									{{ Number(row.status) === 1 ? '启用' : '禁用' }}
								</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc" :title="row.remark">{{ row.remark || '暂无备注' }}</div>

								<div class="tag-group">
									<el-tag size="small" effect="light" type="info" :title="row.type"><el-icon><ele-Folder /></el-icon>{{ row.type || '未分类' }}</el-tag>
									<el-popover placement="bottom" width="280" trigger="hover" v-if="row.userList?.length">
										<template #reference>
											<el-tag size="small" effect="light" type="success"><el-icon><ele-User /></el-icon>{{ row.userList.length }} 人在职</el-tag>
										</template>
										<vxe-grid size="mini" stripe max-height="320" :data="row.userList" :columns="userGridColumns" />
									</el-popover>
									<el-tag v-else size="small" effect="plain" type="info"><el-icon><ele-User /></el-icon>暂无在职人员</el-tag>
								</div>

								<div class="metric-group">
									<div class="metric-item">
										<span>排序</span>
										<b>{{ row.orderNo ?? '—' }}</b>
									</div>
									<div class="metric-item">
										<span>租户</span>
										<b :title="row.tenantName">{{ row.tenantName || '—' }}</b>
									</div>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ formatTime(row.updateTime || row.createTime) }}</span>
								<div class="entity-card__actions">
									<el-tooltip content="编辑" placement="top">
										<el-button icon="ele-Edit" text type="primary" v-auth="'sysPos/update'" @click.stop="handleEdit(row)" />
									</el-tooltip>
									<el-tooltip content="复制" placement="top">
										<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysPos/add'" @click.stop="openCopyMenu(row)" />
									</el-tooltip>
									<el-tooltip content="删除" placement="top">
										<el-button icon="ele-Delete" text type="danger" v-auth="'sysPos/delete'" @click.stop="handleDelete(row)" />
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

		<el-card v-else class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysPos/add'"> 新增 </el-button>
					<el-button-group style="padding-left: 12px">
						<el-button plain icon="ele-Upload" @click="importData" v-auth="'sysPos/import'"> 导入 </el-button>
						<el-button plain icon="ele-Download" @click="exportData" v-auth="'sysPos/export'"> 导出 </el-button>
						<el-button plain icon="ele-FolderOpened" @click="downloadTemplate"> 下载模板 </el-button>
					</el-button-group>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="success">启用</el-tag>
					<el-tag v-else type="danger">禁用</el-tag>
				</template>
				<template #row_userCount="{ row }">
					{{ row.userList?.length }}
				</template>
				<template #row_userList="{ row }">
					<el-popover placement="bottom" width="280" trigger="hover" v-if="row.userList?.length">
						<template #reference>
							<el-text type="primary" class="cursor-default">
								<el-icon><ele-InfoFilled /></el-icon>人员明细
							</el-text>
						</template>
						<vxe-grid size="mini" stripe :data="row.userList" :columns="userGridColumns" />
					</el-popover>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-Edit" text type="primary" v-auth="'sysPos/update'" @click="handleEdit(row)"> 编辑 </el-button>
					<el-button icon="ele-Delete" text type="danger" v-auth="'sysPos/delete'" @click="handleDelete(row)"> 删除 </el-button>
					<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysPos/add'" @click="openCopyMenu(row)"> 复制 </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.uploadVisible" :lock-scroll="false" draggable width="400px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
					<span> 导入 </span>
				</div>
			</template>
			<div>
				<el-upload ref="uploadRef" drag :auto-upload="false" :limit="1" :file-list="state.fileList" action="" :on-change="handleChange" accept=".xlsx">
					<el-icon class="el-icon--upload">
						<ele-UploadFilled />
					</el-icon>
					<div>将文件拖到此处，或点击上传</div>
					<!-- <template #tip>
						<div class="el-upload__tip">请上传大小不超过 10MB 的文件</div>
					</template> -->
				</el-upload>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="state.uploadVisible = false">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="uploadFile">确定</el-button>
				</span>
			</template>
		</el-dialog>

		<EditPos ref="editPosRef" :title="state.title" @handleQuery="refresh" />
	</div>
</template>

<!-- 职位管理 -->
<script lang="ts" setup name="sysPos">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';
import { downloadByData, getFileName } from '/@/utils/download';

import EditPos from '/@/views/system/pos/component/editPos.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysPosApi } from '/@/api-services/system/api';
import { PagePosOutput, PagePosInput, UpdatePosInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const editPosRef = ref<InstanceType<typeof EditPos>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysPos', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
	uploadVisible: false,
	fileList: [] as any,
	/** 视图模式：card 卡片（默认） / table 表格 */
	viewMode: 'card' as 'card' | 'table',
	/** 卡片视图数据 */
	cardList: [] as PagePosOutput[],
});

// 卡片视图分页
const cardPager = reactive({ page: 1, pageSize: 12, total: 0 });

// 人员明细弹层小表（卡片视图与表格视图共用）；列宽合计须小于弹层 280px 的内容区，否则出横向滚动条
const userGridColumns = [
	{ field: 'seq', type: 'seq', title: '序号', width: 50 },
	{ field: 'account', title: '账号', width: 100, showOverflow: 'tooltip' },
	{ field: 'realName', title: '姓名', width: 100, showOverflow: 'tooltip' },
];

// 表格参数配置
const options = useVxeTable<PagePosOutput>(
	{
		id: 'sysPos',
		name: '职位名称',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '职位名称', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'code', title: '职位编码', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'type', title: '职位类别', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'userCount', title: '在职人数', showOverflow: 'tooltip', slots: { default: 'row_userCount' } },
			{ field: 'userList', title: '人员明细', showOverflow: 'tooltip', slots: { default: 'row_userList' } },
			{ field: 'orderNo', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 80, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort), queryAll: () => handleQueryAllApi() } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化

// 查询分页api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PagePosInput;
	return getAPI(SysPosApi).apiSysPosPagePost(params);
};

// 查询所有api
const handleQueryAllApi = async () => {
	return getAPI(SysPosApi).apiSysPosListGet();
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

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
		} as PagePosInput;
		const res = await getAPI(SysPosApi).apiSysPosPagePost(params);
		state.cardList = (res.data.result?.items ?? []) as PagePosOutput[];
		cardPager.total = Number(res.data.result?.total ?? 0);
	} finally {
		options.loading = false;
	}
};

// 刷新当前视图
const refresh = async () => {
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery();
};

// 切换视图：卡片需要主动取数，表格由 vxe 挂载后 autoLoad 触发
const switchView = (mode: 'card' | 'table') => {
	if (state.viewMode === mode) return;
	state.viewMode = mode;
	if (mode === 'card') loadCardList();
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await handleQuery(true);
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	cardPager.page = 1;
	if (state.viewMode === 'card') await loadCardList();
	else await xGrid.value?.commitProxy('reload');
};

// 卡片每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 卡片双击进入编辑
const onCardDblclick = (row: any) => {
	if (auth('sysPos/update')) handleEdit(row);
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加职位';
	editPosRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// 打开编辑页面
const handleEdit = (row: any) => {
	state.title = '编辑职位';
	editPosRef.value?.openDialog(row);
};

// 打开复制页面
const openCopyMenu = (row: any) => {
	state.title = '复制职位';
	var copyRow = JSON.parse(JSON.stringify(row)) as UpdatePosInput;
	copyRow.id = 0;
	copyRow.name = '';
	editPosRef.value?.openDialog(copyRow);
};

// 删除操作
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除职位：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysPosApi).apiSysPosDeletePost({ id: row.id });
			await refresh();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<PagePosOutput> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysPos/update')) await handleEdit(row);
	},
};

// 通过onChanne方法获得文件列表
const handleChange = (file: any, fileList: []) => {
	state.fileList = fileList;
};

// 上传
const uploadFile = async () => {
	if (state.fileList.length < 1) return;

	state.uploadVisible = false;
	options.loading = true;
	await getAPI(SysPosApi).apiSysPosImportPostForm(state.fileList[0].raw);
	await refresh();
	ElMessage.success('上传成功');
	state.fileList = [];
	options.loading = false;
};

// 导入
const importData = () => {
	state.uploadVisible = true;
};

// 导出
const exportData = async () => {
	options.loading = true;
	var res = await getAPI(SysPosApi).apiSysPosExportPost(state.queryParams, { responseType: 'blob' });
	options.loading = false;

	var fileName = getFileName(res.headers);
	downloadByData(res.data as any, fileName);
};

// 下载模板
const downloadTemplate = async () => {
	var res = await getAPI(SysPosApi).apiSysPosDownloadTemplatePost({ responseType: 'blob' });

	var fileName = getFileName(res.headers);
	downloadByData(res.data as any, fileName);
};

/* ------------------------------ 展示辅助 ------------------------------ */
const formatTime = (val: any) => {
	if (!val) return '—';
	const d = dayjs(val);
	return d.isValid() ? d.format('YYYY-MM-DD HH:mm') : String(val);
};

// 页面初始化：默认卡片视图
loadCardList();
</script>

<style lang="scss" scoped>
/* 卡片视图与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.sys-pos-container {
	position: relative;

	/* 表格模式下根节点是 flex 列（.layout-full-table），查询区不许被压缩 */
	> .query-toolbar {
		flex-shrink: 0;
		// margin-top: 14px;
	}
}
</style>
