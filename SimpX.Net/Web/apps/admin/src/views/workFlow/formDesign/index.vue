<template>
	<div class="workFlow-formDesign-container">
		<!-- 顶部头图 -->
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
					<h2 class="page-hero__title">流程表单</h2>
					<p class="page-hero__desc">管理可复用的流程表单，支持版本发布与启停；可按名称、分类、发布状态与启停状态筛选。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">表单总数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：条件与查询/重置按钮同一行（全站标准 query-toolbar） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.keyword" class="query-input" placeholder="表单名称 / 编码" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="分类">
					<el-input v-model="state.queryParams.category" class="query-input" placeholder="分类" clearable @keyup.enter.native="handleSearch" />
				</el-form-item>
				<el-form-item label="发布状态">
					<el-select v-model="state.queryParams.isPublished" class="query-select" placeholder="全部状态" clearable>
						<el-option label="已发布" :value="true" />
						<el-option label="未发布" :value="false" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.status" class="query-select" placeholder="全部状态" clearable>
						<el-option label="启用" :value="1" />
						<el-option label="停用" :value="0" />
					</el-select>
				</el-form-item>
			</el-form>
		<div class="query-actions">
			<el-button-group>
				<el-button type="primary" icon="ele-Search" @click="handleSearch" :loading="options.loading"> 查询 </el-button>
				<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
			</el-button-group>
			<!-- 卡片视图没有 vxe 工具栏，新建入口挂在这里（与流程设计页保持同一套布局） -->
			<el-button v-if="state.viewMode === 'card'" type="primary" icon="ele-Plus" @click="openCreate">新建表单</el-button>
			<el-button-group>
				<el-button type="primary" :plain="state.viewMode !== 'card'" icon="ele-Grid" @click="switchView('card')">卡片</el-button>
				<el-button type="primary" :plain="state.viewMode !== 'table'" icon="ele-List" @click="switchView('table')">表格</el-button>
			</el-button-group>
		</div>
		</div>

		<!-- 卡片视图（默认）：全站统一的 .card-grid + .entity-card（与 AI 智能体 · 工具管理同一套） -->
		<template v-if="state.viewMode === 'card'">
			<el-card class="full-table" shadow="hover">
				<div class="card-grid" v-loading="options.loading">
					<el-empty v-if="!options.loading && !state.cardList.length" description="没有找到匹配的表单，点击查询区「新建表单」开始设计" :image-size="120" />
					<el-row v-else :gutter="12" class="card-grid-row">
						<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.cardList" :key="row.id">
							<!-- 不放 entity-card__watermark：本页图标由用户按表单自选，默认 ele-Menu 是方框轮廓，
							     74px 压在右上角正好把状态标签框进去，看着像标签外面套了个框 -->
							<el-card shadow="never" class="entity-card entity-card--accent is-primary">
								<div class="entity-card__head">
									<div class="entity-card__icon">
										<SvgIcon :name="row.icon || 'ele-Menu'" :size="20" />
									</div>
									<div class="entity-card__title-wrap">
										<div class="entity-card__title" :title="row.name">{{ row.name }}</div>
										<div class="entity-card__sub" :title="row.code || ''">{{ row.code || '未设置编码' }}</div>
									</div>
									<el-tooltip :content="Number(row.status) === 1 ? '点击停用该表单' : '点击启用该表单'" placement="top">
										<el-tag size="small" effect="light" class="is-toggle" :type="Number(row.status) === 1 ? 'success' : 'info'" @click.stop="toggleStatus(row)">
											{{ Number(row.status) === 1 ? '启用' : '停用' }}
										</el-tag>
									</el-tooltip>
								</div>

								<div class="entity-card__body">
									<div class="card-desc" :title="row.description || ''">{{ row.description || '暂无描述' }}</div>

									<div class="tag-group">
										<el-tag size="small" effect="plain" type="info" :title="`分类：${row.category || '未分类'}`">{{ row.category || '未分类' }}</el-tag>
										<el-tag size="small" effect="plain" :type="row.isPublished ? 'success' : 'info'">{{ row.isPublished ? '已发布' : '未发布' }}</el-tag>
									</div>

									<div class="metric-group">
										<div class="metric-item">
											<span>版本号</span><b>{{ row.version ? 'v' + row.version : '—' }}</b>
										</div>
										<div class="metric-item">
											<span>发布时间</span><b>{{ row.publishedTime ? formatDateTime(row.publishedTime) : '—' }}</b>
										</div>
									</div>
								</div>

								<div class="entity-card__foot">
									<span class="cell-sub">{{ row.createTime ? formatDateTime(row.createTime) : '—' }}</span>
									<div class="entity-card__actions">
										<el-tooltip content="编辑基础信息" placement="top">
											<el-button icon="ele-Edit" text type="primary" @click.stop="openEdit(row)" />
										</el-tooltip>
										<el-tooltip content="表单设计" placement="top">
											<el-button icon="ele-EditPen" text type="primary" @click.stop="openDesign(row)" />
										</el-tooltip>
										<el-tooltip v-if="row.isPublished" content="取消发布" placement="top">
											<el-button icon="ele-RefreshLeft" text type="warning" @click.stop="handleUnpublish(row)" />
										</el-tooltip>
										<el-tooltip v-else :content="canPublishForm(row) ? '发布表单' : '请先在表单设计中保存设计'" placement="top">
											<el-button icon="ele-Position" text type="success" :disabled="!canPublishForm(row)" @click.stop="openPublish(row)" />
										</el-tooltip>
										<el-tooltip content="删除表单" placement="top">
											<el-button icon="ele-Delete" text type="danger" @click.stop="handleDelete(row)" />
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
		</template>

		<el-card v-else class="full-table" shadow="hover">
			<div class="grid-holder">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="openCreate"> 新建表单 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_category="{ row }">
					{{ row.category || '-' }}
				</template>
				<template #row_icon="{ row }">
					<span v-if="row.icon" class="icon-cell">
						<SvgIcon :name="row.icon" :size="18" />
					</span>
					<span v-else>-</span>
				</template>
				<template #row_version="{ row }">
					<el-tag v-if="row.isPublished" type="success">v{{ row.version }}</el-tag>
					<span v-else>v{{ row.version }}</span>
				</template>
				<template #row_isPublished="{ row }">
					<el-tag v-if="row.isPublished" type="success">已发布</el-tag>
					<el-tag v-else type="info">未发布</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-switch v-model="row.status" :active-value="1" :inactive-value="0" @change="(val: number) => handleStatusChange(row, val)" />
				</template>
				<template #row_createTime="{ row }">
					<span v-if="row.createTime">{{ formatDateTime(row.createTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_publishedTime="{ row }">
					<span v-if="row.publishedTime">{{ formatDateTime(row.publishedTime) }}</span>
					<span v-else>-</span>
				</template>
				<template #row_buttons="{ row }">
					<el-button text type="primary" icon="ele-Edit" @click="openEdit(row)">编辑</el-button>
					<el-button text type="danger" icon="ele-Delete" @click="handleDelete(row)">删除</el-button>
					<el-button text type="primary" icon="ele-EditPen" @click="openDesign(row)">设计</el-button>
					<el-button text type="success" icon="ele-Position" @click="openPublish(row)" :disabled="!row.schemaJson || row.schemaJson.trim() === '' || row.isPublished">发布</el-button>
					<el-button v-if="row.isPublished" text type="warning" icon="ele-RefreshLeft" @click="handleUnpublish(row)">取消发布</el-button>
				</template>
			</vxe-grid>
			</div>
		</el-card>

		<!-- 表单编辑对话框 -->
		<el-dialog v-model="formDialog.visible" draggable width="600px" :close-on-click-modal="false" @close="resetFormDialog">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span> {{ formDialog.title }} </span>
				</div>
			</template>

			<el-form :model="formDialog.form" ref="formDialogRef" :rules="formDialog.rules" label-width="auto">
				<el-form-item label="表单名称" prop="name">
					<el-input v-model="formDialog.form.name" placeholder="请输入表单名称" maxlength="64" show-word-limit />
				</el-form-item>
				<el-form-item label="表单编码" prop="code">
					<el-input v-model="formDialog.form.code" placeholder="留空自动生成" maxlength="64" show-word-limit />
				</el-form-item>
				<el-form-item label="分类">
					<el-input v-model="formDialog.form.category" placeholder="请输入分类" maxlength="32" show-word-limit />
				</el-form-item>
				<el-form-item label="描述">
					<el-input v-model="formDialog.form.description" type="textarea" :rows="3" placeholder="请输入描述" maxlength="256" show-word-limit />
				</el-form-item>
				<el-form-item label="图标">
					<IconSelector v-model="formDialog.form.icon" :size="getGlobalComponentSize" placeholder="图标" type="all" />
				</el-form-item>
				<el-form-item label="状态">
					<el-radio-group v-model="formDialog.form.status">
						<el-radio :label="1" :value="1">启用</el-radio>
						<el-radio :label="0" :value="0">停用</el-radio>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="formDialog.form.remark" type="textarea" :rows="3" placeholder="请输入备注" maxlength="512" show-word-limit />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="formDialog.visible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="handleFormSubmit" :loading="formDialog.loading">确定</el-button>
			</template>
		</el-dialog>

		<!-- 表单设计器对话框 -->
		<el-dialog v-model="designerDialog.visible" class="form-designer-dialog" title="表单设计" :close-on-click-modal="false" @close="handleDesignerClose" fullscreen>
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span> 表单设计 </span>
				</div>
			</template>

			<div class="form-designer-wrapper">
				<FcDesigner v-if="designerDialog.visible" ref="designerRef" :form-json="designerDialog.formJson" />
			</div>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="designerDialog.visible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="handleSaveDesign" :loading="designerDialog.loading">保存设计</el-button>
			</template>
		</el-dialog>

		<!-- 发布对话框 -->
		<el-dialog v-model="publishDialog.visible" title="发布表单" width="500px" draggable :close-on-click-modal="false" @close="resetPublishDialog">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Promotion /></el-icon>
					<span> 发布表单 </span>
				</div>
			</template>

			<el-form :model="publishDialog.form" ref="publishFormRef" label-width="auto">
				<el-form-item label="表单名称">
					<el-input v-model="publishDialog.form.name" disabled />
				</el-form-item>
				<el-form-item label="当前版本">
					<el-input v-model="publishDialog.form.version" disabled />
				</el-form-item>
				<el-form-item label="发布后版本">
					<el-input :value="publishDialog.form.publishedTime ? publishDialog.form.version + 1 : publishDialog.form.version" disabled />
				</el-form-item>
				<el-form-item label="发布备注">
					<el-input v-model="publishDialog.form.remark" type="textarea" :rows="3" placeholder="请输入发布备注（可选）" maxlength="512" show-word-limit />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="publishDialog.visible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="handlePublish" :loading="publishDialog.loading">发布</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="workFlowFormDesign">
import { ref, reactive, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { formatDateTime } from '/@/utils/format';
import IconSelector from '/@/components/iconSelector/index.vue';
import SvgIcon from '/@/components/svgIcon/index.vue';
import FcDesigner from '/@/components/formCreate/FcDesigner.vue';
import { parseFcSchema } from '/@/views/workFlow/utils/flowForm';
import other from '/@/utils/other';

import {
	getWorkflowForms,
	getWorkflowFormDetail,
	createWorkflowForm,
	updateWorkflowForm,
	deleteWorkflowForm,
	saveWorkflowFormDesign,
	publishWorkflowForm,
	unpublishWorkflowForm,
	setWorkflowFormStatus,
} from '/@/api/workflow/index';

interface WorkflowFormItem {
	id: number;
	code: string;
	name: string;
	category?: string;
	description?: string;
	icon?: string;
	version: number;
	isPublished: boolean;
	publishedTime?: string;
	status: number;
	remark?: string;
	schemaJson?: string;
	uiSchemaJson?: string;
	ruleJson?: string;
	createTime?: string;
	updateTime?: string;
}

const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		keyword: '',
		category: '',
		isPublished: undefined as boolean | undefined,
		status: undefined as number | undefined,
	},
	/** 视图模式：card 卡片（默认） / table 表格 */
	viewMode: 'card' as 'card' | 'table',
	/** 卡片视图数据 */
	cardList: [] as WorkflowFormItem[],
});

/** 卡片视图分页 */
const cardPager = reactive({ page: 1, pageSize: 12, total: 0 });

const heroStats = reactive({ total: 0 });

// 表格参数配置
const options = useVxeTable<WorkflowFormItem>(
	{
		id: 'workFlowFormDesign',
		name: '流程表单',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '表单名称', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'code', title: '表单编码', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'category', title: '分类', width: 100, slots: { default: 'row_category' } },
			{ field: 'description', title: '描述', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'icon', title: '图标', width: 80, slots: { default: 'row_icon' } },
			{ field: 'version', title: '版本号', width: 100, slots: { default: 'row_version' } },
			{ field: 'isPublished', title: '发布状态', width: 100, slots: { default: 'row_isPublished' } },
			{ field: 'status', title: '状态', width: 100, slots: { default: 'row_status' } },
			{ field: 'remark', title: '备注', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '创建时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'publishedTime', title: '发布时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_publishedTime' } },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 20 },
		toolbarConfig: { export: true },
	}
);

// 表格事件
const gridEvents: VxeGridListeners<WorkflowFormItem> = {};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const params = Object.assign({}, state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	const res = await getWorkflowForms(params);
	const result = (res.data?.result ?? res.data?.data?.result ?? res.data ?? {}) as any;
	heroStats.total = (result.total ?? 0) as number;
	return res;
};

// 卡片视图取数
const loadCardList = async () => {
	options.loading = true;
	try {
		const params = Object.assign({}, state.queryParams, { page: cardPager.page, pageSize: cardPager.pageSize });
		const res = await getWorkflowForms(params);
		const result = (res.data?.result ?? res.data?.data?.result ?? res.data ?? {}) as any;
		state.cardList = result.items ?? [];
		cardPager.total = Number(result.total ?? 0);
	} finally {
		options.loading = false;
	}
};

// 刷新当前视图（卡片 / 表格共用）
const refresh = async (reset = false) => {
	if (state.viewMode === 'card') {
		if (reset) cardPager.page = 1;
		await loadCardList();
	} else {
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	}
};

// 查询操作（表格视图兼容）
const handleQuery = async (reset = false) => {
	await refresh(reset);
};

// 查询按钮：回到第一页并刷新
const handleSearch = async () => {
	await refresh(true);
};

// 重置操作
const resetQuery = () => {
	state.queryParams = {
		keyword: '',
		category: '',
		isPublished: undefined,
		status: undefined,
	};
	handleSearch();
};

// 切换视图：卡片需要主动取数，表格由 vxe 挂载后 autoLoad 触发
const switchView = (mode: 'card' | 'table') => {
	if (state.viewMode === mode) return;
	state.viewMode = mode;
	if (mode === 'card') loadCardList();
};

// 卡片每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

/** 卡片上的状态胶囊：点击即启用/停用 */
const toggleStatus = async (row: WorkflowFormItem) => {
	const next = Number(row.status) === 1 ? 0 : 1;
	try {
		await setWorkflowFormStatus({ id: row.id, status: next });
		row.status = next;
		ElMessage.success(next === 1 ? '已启用' : '已停用');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.errors || error.message || '状态更新失败');
	}
};

/** 是否可发布：表单已设计（schemaJson 非空）且尚未发布 */
const canPublishForm = (row: WorkflowFormItem) => !!row.schemaJson && String(row.schemaJson).trim() !== '' && !row.isPublished;

const formDialog = reactive({
	visible: false,
	title: '新建表单',
	loading: false,
	form: {
		id: 0,
		name: '',
		code: '',
		category: '',
		description: '',
		icon: 'ele-Menu',
		status: 1,
		remark: '',
	},
	rules: {
		name: [{ required: true, message: '请输入表单名称', trigger: 'blur' }],
	},
});

const formDialogRef = ref();
const resetFormDialog = () => {
	formDialog.form = {
		id: 0,
		name: '',
		code: '',
		category: '',
		description: '',
		icon: 'ele-Menu',
		status: 1,
		remark: '',
	};
	formDialogRef.value?.resetFields();
};

const designerDialog = reactive({
	visible: false,
	loading: false,
	currentForm: null as WorkflowFormItem | null,
	formJson: null as any,
});

const designerRef = ref();
const handleDesignerClose = () => {
	designerDialog.currentForm = null;
	designerDialog.formJson = null;
};

const publishDialog = reactive({
	visible: false,
	loading: false,
	form: {
		id: 0,
		name: '',
		version: 0,
		publishedTime: undefined as string | undefined,
		remark: '',
	},
});

const publishFormRef = ref();
const resetPublishDialog = () => {
	publishDialog.form = {
		id: 0,
		name: '',
		version: 0,
		publishedTime: undefined,
		remark: '',
	};
	publishFormRef.value?.resetFields();
};

const openCreate = () => {
	formDialog.title = '新建表单';
	formDialog.visible = true;
};

const openEdit = async (row: WorkflowFormItem) => {
	try {
		const res = await getWorkflowFormDetail({ id: row.id });
		const detail = res.data.result;
		formDialog.form = {
			id: detail.id,
			name: detail.name,
			code: detail.code,
			category: detail.category || '',
			description: detail.description || '',
			icon: detail.icon || '',
			status: detail.status,
			remark: detail.remark || '',
		};
		formDialog.title = '编辑表单';
		formDialog.visible = true;
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.errors || error.message || '获取详情失败');
	}
};

const handleFormSubmit = async () => {
	if (!formDialogRef.value) return;
	await formDialogRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		formDialog.loading = true;
		try {
			if (formDialog.form.id > 0) {
				await updateWorkflowForm(formDialog.form);
				ElMessage.success('更新成功');
			} else {
				await createWorkflowForm(formDialog.form);
				ElMessage.success('创建成功');
			}
			formDialog.visible = false;
			handleQuery();
		} catch (error: any) {
			ElMessage.error(error?.response?.data?.errors || error.message || '操作失败');
		} finally {
			formDialog.loading = false;
		}
	});
};

const openDesign = async (row: WorkflowFormItem) => {
	try {
		const res = await getWorkflowFormDetail({ id: row.id });
		const detail = res.data.result;

		designerDialog.currentForm = row;

		// 设计稿就是保存时 getFormJson() 写回的 { rule, options }，解析失败按空表单处理
		designerDialog.formJson = parseFcSchema(detail.schemaJson) ?? { rule: [], options: {} };

		// 设计稿通过 :form-json 传入，FcDesigner 挂载时自行载入
		designerDialog.visible = true;
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.errors || error.message || '获取详情失败');
	}
};

const handleSaveDesign = async () => {
	if (!designerRef.value || !designerDialog.currentForm) return;
	try {
		// 读取设计器当前表单 JSON（form-create 原生形态 { rule, options }）
		const formJson = designerRef.value.getFormJson();
		if (!Array.isArray(formJson.rule) || formJson.rule.length === 0) {
			ElMessage.warning('表单设计稿为空，请先设计表单');
			return;
		}

		designerDialog.loading = true;
		await saveWorkflowFormDesign({
			id: designerDialog.currentForm.id,
			schemaJson: JSON.stringify(formJson),
		});
		ElMessage.success('保存成功');
		designerDialog.visible = false;
		handleQuery();
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.errors || error.message || '保存失败');
	} finally {
		designerDialog.loading = false;
	}
};

const openPublish = async (row: WorkflowFormItem) => {
	publishDialog.form = {
		id: row.id,
		name: row.name,
		version: row.version,
		publishedTime: row.publishedTime,
		remark: '',
	};
	publishDialog.visible = true;
};

const handlePublish = async () => {
	if (!publishFormRef.value) return;
	publishDialog.loading = true;
	try {
		await publishWorkflowForm({
			id: publishDialog.form.id,
			remark: publishDialog.form.remark,
		});
		ElMessage.success('发布成功');
		publishDialog.visible = false;
		handleQuery();
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.errors || error.message || '发布失败');
	} finally {
		publishDialog.loading = false;
	}
};

const handleUnpublish = async (row: WorkflowFormItem) => {
	try {
		await ElMessageBox.confirm(`确认取消发布表单「${row.name}」吗？`, '取消发布确认', {
			type: 'warning',
		});
		await unpublishWorkflowForm({ id: row.id });
		ElMessage.success('取消发布成功');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.errors || error.message || '取消发布失败');
		}
	}
};

const handleDelete = async (row: WorkflowFormItem) => {
	try {
		await ElMessageBox.confirm(`确认删除表单「${row.name}」吗？`, '删除确认', {
			type: 'warning',
		});
		await deleteWorkflowForm({ id: row.id });
		ElMessage.success('删除成功');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.errors || error.message || '删除失败');
		}
	}
};

const handleStatusChange = async (row: WorkflowFormItem, value: number) => {
	try {
		await setWorkflowFormStatus({ id: row.id, status: value });
		ElMessage.success('状态更新成功');
	} catch (error: any) {
		row.status = value === 1 ? 0 : 1;
		ElMessage.error(error?.response?.data?.errors || error.message || '状态更新失败');
	}
};

// 页面初始化：默认卡片视图取数
if (state.viewMode === 'card') loadCardList();
</script>

<style lang="scss" scoped>
.workFlow-formDesign-container {
	// display: flex;
	// flex-direction: column;
	// gap: 10px;
	// height: 100%;
}

.workFlow-formDesign-container > .query-toolbar {
	margin-top: 12px;
}

.workFlow-formDesign-container > .full-table {
	// margin-top: 12px;
}

/* 弹窗内的表单控件撑满整行。
 * 必须限定在 .el-dialog 里：裸写 :deep(.el-input) 会顺带命中查询区的 .query-input，
 * 且它的特异性（.el-input + 属性选择器）高于全局的 .query-input{width:220px}，
 * 把查询框撑成 100% → 一行塞不下 4 个条件 → 查询区被挤成两行（88px，其他页 54px） */
.el-dialog :deep(.el-input),
.el-dialog :deep(.el-select) {
	width: 100%;
}

.full-table {
	flex: 1;
}

.mb10 {
	margin-bottom: 10px;
}

.form-designer-wrapper {
	width: 100%;
	/* 撑满弹窗 body（body 是 flex 列），高度交给容器而不是写死 100vh，否则底部按钮栏被顶出视口 */
	flex: 1;
	min-height: 0;
	overflow: auto;
}

.icon-cell {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	gap: 6px;
	white-space: nowrap;
}

/* 卡片视图与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .card-grid / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */

/* 头部状态标签承担"点击切换启停"，给个手型提示（全局 el-tag 默认不是可点的） */
.entity-card__head > .el-tag.is-toggle {
	cursor: pointer;
}
</style>

<!-- 全屏表单设计器：清零弹窗留白，左右上下贴边。
     与 flow-designer-dialog 同理，全局 element.scss 的同名规则带 !important，必须用非 scoped 块反压 -->
<style lang="scss">
.el-overlay .el-overlay-dialog .el-dialog.form-designer-dialog {
	padding: 0 !important;
	margin: 0 !important;
	height: 100vh;
	max-height: 100vh;
	overflow: hidden;
	display: flex;
	flex-direction: column;

	.el-dialog__body {
		flex: 1;
		min-height: 0;
		padding: 0 !important;
		max-height: none !important;
		overflow: hidden !important;
		display: flex;
		flex-direction: column;
	}
}
</style>
