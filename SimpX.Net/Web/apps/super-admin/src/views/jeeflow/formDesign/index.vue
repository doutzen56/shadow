<template>
	<div class="jf-form-design-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="表单名称">
							<el-input v-model="state.queryParams.name" placeholder="表单名称" clearable @keyup.enter.native="handleQuery(true)" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label-width="0">
							<el-button-group>
								<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'formDesign/page'" :loading="options.loading"> 查询 </el-button>
								<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
							</el-button-group>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px; flex: 1">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'formDesign/add'"> 新增 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="编辑" placement="top">
						<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip content="删除" placement="top">
						<el-button icon="ele-Delete" text type="danger" v-auth="'formDesign/delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
			</vxe-grid>
		</el-card>

		<!-- 新增/编辑表单 -->
		<el-dialog v-model="state.editVisible" width="640px" destroy-on-close @close="closeEdit">
			<template #header>
				<span class="jf-edit-title"
					><el-icon><ele-Edit /></el-icon>{{ state.title }}</span
				>
			</template>
			<el-form ref="editFormRef" :model="state.editForm" :rules="editRules" label-width="90px">
				<el-form-item label="表单名称" prop="name">
					<el-input v-model="state.editForm.name" placeholder="请输入表单名称" clearable maxlength="100" />
				</el-form-item>
				<el-form-item label="表单内容" prop="content">
					<div class="jf-content-box">
						<div class="jf-content-bar">
							<el-button type="primary" size="small" icon="ele-MagicStick" @click="openDesigner"> 设计表单 </el-button>
							<el-button v-if="state.editForm.content" size="small" icon="ele-Delete" @click="state.editForm.content = ''"> 清空内容 </el-button>
							<span class="jf-content-tip">{{ state.editForm.content ? '已由设计器生成，可直接保存' : '设计器导出的表单 JSON（可为空）' }}</span>
						</div>
						<el-input v-model="state.editForm.content" placeholder="点击「设计表单」可视化拖拽生成，或直接填写 JSON" type="textarea" :rows="6" clearable />
					</div>
				</el-form-item>
				<el-form-item label="版本号" prop="version">
					<el-input v-model="state.editForm.version" placeholder="默认 1" clearable />
				</el-form-item>
				<el-form-item label="排序" prop="sortOrder">
					<el-input-number v-model="state.editForm.sortOrder" :min="0" :max="9999" controls-position="right" style="width: 100%" />
				</el-form-item>
				<el-form-item label="备注" prop="remark">
					<el-input v-model="state.editForm.remark" placeholder="请输入备注" type="textarea" :rows="2" clearable maxlength="200" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false"> 取 消 </el-button>
				<el-button type="primary" :loading="editLoading" @click="handleSubmit"> 确 定 </el-button>
			</template>
		</el-dialog>

		<!-- 表单设计器：全屏拖拽设计，保存后回填到「表单内容」 -->
		<FormDesigner v-model="designerVisible" :content="state.editForm.content" @save="onDesignerSave" />
	</div>
</template>

<!-- 表单设计管理 -->
<script lang="ts" setup name="jfFormDesign">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance, FormRules } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { JeeflowFormDesignApi } from '/@/api-services/jeeFlow/api';
import { WfFormDesignOutput, WfFormDesignAddInput, WfFormDesignUpdateInput, WfFormDesignPageInput } from '/@/api-services/jeeFlow/models';
import FormDesigner from './FormDesigner.vue';

const xGrid = ref<VxeGridInstance>();
const editFormRef = ref<FormInstance>();
const editLoading = ref(false);
// 全屏表单设计器弹窗
const designerVisible = ref(false);

const { key: localPageParamKey, param: localPageParam } = useGridPageParam('jfFormDesign', {
	pageSize: 50 as number,
	defaultSort: { field: 'id', order: 'desc', descStr: 'desc' },
});

const state = reactive({
	queryParams: {
		name: undefined as string | undefined,
	},
	localPageParam,
	title: '',
	editVisible: false,
	editForm: {
		id: undefined as number | undefined,
		name: '',
		remark: '',
		version: 1,
		sortOrder: 100,
		content: '',
	} as WfFormDesignUpdateInput,
});

const editRules = reactive<FormRules>({
	name: [{ required: true, message: '请输入表单名称', trigger: 'blur' }],
});

// 表格参数配置
const options = useVxeTable<WfFormDesignOutput>(
	{
		id: 'jfFormDesign',
		name: '表单设计',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'name', title: '表单名称', minWidth: 140, showOverflow: 'tooltip' },
			{ field: 'remark', title: '备注', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'version', title: '版本号', width: 80, showOverflow: 'tooltip' },
			{ field: 'sortOrder', title: '排序', width: 80, showOverflow: 'tooltip' },
			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: localPageParam.defaultSort },
		pagerConfig: { pageSize: localPageParam.pageSize },
		toolbarConfig: { export: true },
	}
);

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as WfFormDesignPageInput;
	return getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignPagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.name = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 打开新增页面
const handleAdd = () => {
	state.title = '新增表单设计';
	state.editForm = { id: undefined, name: '', remark: '', version: 1, sortOrder: 100, content: '' };
	state.editVisible = true;
};

// 打开编辑页面
const handleEdit = async (row: WfFormDesignOutput) => {
	state.title = '编辑表单设计';
	let content = '';
	if (row.id) {
		const res = await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignDetailGet(row.id);
		content = (res.data.result as any)?.content ?? '';
	}
	state.editForm = {
		id: row.id,
		name: row.name ?? '',
		remark: row.remark ?? '',
		version: row.version ?? 1,
		sortOrder: row.sortOrder ?? 100,
		content,
	};
	state.editVisible = true;
};

const closeEdit = () => {
	editFormRef.value?.clearValidate();
};

// 打开全屏表单设计器
const openDesigner = () => {
	designerVisible.value = true;
};

// 设计器保存：把生成的 JSON 回填到「表单内容」
const onDesignerSave = (payload: { rule: any[]; option: any; json: string }) => {
	state.editForm.content = payload?.json ?? '';
	ElMessage.success('表单设计已回填');
};

// 新增/编辑提交
const handleSubmit = async () => {
	if (!editFormRef.value) return;
	await editFormRef.value.validate();
	try {
		editLoading.value = true;
		// 版本号为输入框，回传的是字符串，此处归一化为数字
		const version = Number(state.editForm.version ?? 1);
		const body = { ...state.editForm, version: Number.isFinite(version) ? version : 1 };
		if (body.id) {
			await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignUpdatePost(body as WfFormDesignUpdateInput);
			ElMessage.success('更新成功');
		} else {
			const { id, ...addBody } = body;
			await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignAddPost(addBody as WfFormDesignAddInput);
			ElMessage.success('新增成功');
		}
		state.editVisible = false;
		await handleQuery();
	} finally {
		editLoading.value = false;
	}
};

// 删除
const handleDelete = (row: WfFormDesignOutput) => {
	ElMessageBox.confirm(`确定删除表单设计：${row.name}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			if (!row.id) return;
			await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<WfFormDesignOutput> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};
</script>

<style scoped lang="scss">
.jf-form-design-container {
	display: flex;
	flex-direction: column;
	height: 100%;
}

// 「表单内容」上方的操作条：设计表单按钮 + 状态提示
.jf-content-box {
	display: flex;
	flex-direction: column;
	gap: 6px;
	width: 100%;
}

.jf-content-bar {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

.jf-content-tip {
	margin-left: auto;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.jf-edit-title {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 16px;
	font-weight: 600;
	color: #fff;
}
</style>
