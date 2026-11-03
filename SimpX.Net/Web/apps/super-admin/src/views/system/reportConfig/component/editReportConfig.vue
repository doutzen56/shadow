<template>
	<div>
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" fullscreen>
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" :rules="rules">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item :label="'名称'" prop="name">
							<el-input v-model="state.ruleForm.name" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item :label="'分组'" prop="groupId">
							<el-select v-model="state.ruleForm.groupId" filterable clearable>
								<el-option v-for="item in state.groupList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item :label="'描述'" prop="description">
							<el-input v-model="state.ruleForm.description" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item :label="'数据源类型'" prop="dsType">
							<!-- TODO：实现 Api 方式-->
							<g-sys-dict v-model="state.ruleForm.dsType" code="ReportConfigDsTypeEnum" render-as="select" disabled />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20" v-if="state.ruleForm.dsType === ReportConfigDsTypeEnum.NUMBER_0">
						<el-form-item :label="'数据源'" prop="dataSource">
							<el-select v-model="state.ruleForm.dataSource" class="w100">
								<el-option v-for="item in state.dataSourceList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" :style="[state.ruleForm.dsType === ReportConfigDsTypeEnum.NUMBER_0 ? '' : 'display:none;']">
						<el-form-item :label="'脚本语句'" prop="sqlScript">
							<div ref="sqlScriptMonacoEditorRef" style="width: 100%; height: 300px; border: 1px solid var(--el-border-color)"></div>
							<el-button ref="parseSqlButtonRef" type="primary" icon="ele-Position" plain style="margin-top: 8px" @click="parseSqlClick">{{ '解析Sql' }}</el-button>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20" v-if="state.ruleForm.dsType === ReportConfigDsTypeEnum.NUMBER_1">
						<el-form-item :label="'接口地址'" prop="apiUrl">
							<el-input v-model="state.ruleForm.apiUrl" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8" class="mb20" v-if="state.ruleForm.dsType === ReportConfigDsTypeEnum.NUMBER_1">
						<el-form-item :label="'接口请求方式'" prop="apiHttpMethod">
							<el-select v-model="state.ruleForm.apiHttpMethod" class="w100">
								<el-option key="Get" label="Get" value="Get" />
								<el-option key="Post" label="Post" value="Post" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" :style="[state.ruleForm.dsType === ReportConfigDsTypeEnum.NUMBER_1 ? '' : 'display:none;']">
						<el-form-item :label="'接口参数'" prop="apiParams">
							<div ref="apiParamsMonacoEditorRef" style="width: 100%; height: 300px; border: 1px solid var(--el-border-color)"></div>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-tabs v-model="state.activeName" class="report-config-tab">
				<el-tab-pane :label="'字段'" name="field">
					<vxe-grid ref="vFieldGrid" v-bind="fieldConfig.gridOptions" :data="state.fieldListData">
						<template #toolbar_buttons>
							<el-space>
								<el-button type="primary" size="small" icon="ele-Plus" @click="() => add(vFieldGrid!, beforeAddField)">{{ '新增' }}</el-button>
								<el-button size="small" icon="ele-Rank" @click="() => insert(vFieldGrid!)">{{ '插入' }}</el-button>
								<el-button type="danger" size="small" icon="ele-Delete" plain @click="() => remove(vFieldGrid!)">{{ '删除' }}</el-button>
							</el-space>
						</template>
						<template #toolbar_tools> </template>
						<template #row_isSummary="{ row }">
							<el-switch v-model="row.isSummary" :active-value="true" :inactive-value="false" size="small" />
						</template>
						<template #row_visible="{ row }">
							<el-switch v-model="row.visible" :active-value="true" :inactive-value="false" size="small" />
						</template>
					</vxe-grid>
				</el-tab-pane>
				<el-tab-pane :label="'参数'" name="param">
					<div style="display: flex; height: 100%">
						<vxe-grid ref="vParamGrid" style="flex: 1" v-bind="paramConfig.gridOptions" :data="state.paramListData">
							<template #toolbar_buttons>
								<el-space>
									<el-button type="primary" size="small" icon="ele-Plus" @click="() => add(vParamGrid!)">{{ '新增' }}</el-button>
									<el-button size="small" icon="ele-Rank" @click="() => insert(vParamGrid!)">{{ '插入' }}</el-button>
									<el-button type="danger" size="small" icon="ele-Delete" plain @click="() => remove(vParamGrid!)">{{ '删除' }}</el-button>
								</el-space>
							</template>
							<template #toolbar_tools> </template>
						</vxe-grid>
						<el-card shadow="hover" style="margin-left: 4px" :header="'内置参数'" headerClass="sys-report-config-param-build-in-card-header">
							<el-row style="width: 240px" v-for="(item, index) in buildInParams" :key="index">
								<el-col :span="11">{{ item.name }}</el-col>
								<el-col :span="13">{{ item.description }}</el-col>
							</el-row>
						</el-card>
					</div>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="sysReportConfigEditForm">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElInput, ElMessage } from 'element-plus';
import { VxeGrid } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

import * as monaco from 'monaco-editor';
import { getMonacoTheme } from '/@/utils/theme';
import editorWorker from 'monaco-editor/editor/editor.worker?worker';
import jsonWorker from 'monaco-editor/language/json/json.worker?worker';
import { format } from 'sql-formatter';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportConfigApi, SysReportDataSourceApi, SysReportGroupApi } from '/@/api-services/system/api';
import { ReportConfigDsTypeEnum, ReportDataSourceOutput, SysReportField, SysReportGroup, SysReportParam, UpdateReportConfigInput } from '/@/api-services/system/models';

const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const vFieldGrid = ref<InstanceType<typeof VxeGrid>>();
const vParamGrid = ref<InstanceType<typeof VxeGrid>>();
const sqlScriptMonacoEditorRef = ref();
const apiParamsMonacoEditorRef = ref();
const parseSqlButtonRef = ref<InstanceType<typeof ElInput>>();

// TODO: monaco 封装成组件
// 设置 monaco worker
self.MonacoEnvironment = {
	getWorker: function (workerId: string, label: string) {
		if (label === 'json') {
			return new jsonWorker();
		}
		return new editorWorker();
	},
};

// 注册 monaco sql 格式化
monaco.languages.registerDocumentFormattingEditProvider('sql', {
	provideDocumentFormattingEdits: (model: monaco.editor.ITextModel): monaco.languages.ProviderResult<monaco.languages.TextEdit[]> => {
		return [
			{
				text: formatSql(model.getValue()),
				range: model.getFullModelRange(),
			},
		];
	},
});

/** 格式化 Sql */
const formatSql = (value: string): string => {
	return format(value, {
		tabWidth: 4,
		useTabs: false,
		keywordCase: 'upper',
		dataTypeCase: 'upper',
		functionCase: 'upper',
		logicalOperatorNewline: 'before',
		language: 'transactsql',
	});
};

// 初始化monacoEditor对象
var sqlScriptMonacoEditor: monaco.editor.IStandaloneCodeEditor;
const initSqlScriptMonacoEditor = () => {
	sqlScriptMonacoEditor = monaco.editor.create(sqlScriptMonacoEditorRef.value, {
		theme: getMonacoTheme(), // vs / vs-dark 由主题决定
		value: '', // 默认显示的值
		language: 'sql',
		formatOnPaste: true,
		wordWrap: 'on', //自动换行，注意大小写
		wrappingIndent: 'indent',
		folding: true, // 是否折叠
		foldingHighlight: true, // 折叠等高线
		foldingStrategy: 'indentation', // 折叠方式  auto | indentation
		showFoldingControls: 'always', // 是否一直显示折叠 always | mouSEOver
		disableLayerHinting: true, // 等宽优化
		emptySelectionClipboard: false, // 空选择剪切板
		selectionClipboard: false, // 选择剪切板
		automaticLayout: true, // 自动布局
		codeLens: false, // 代码镜头
		scrollBeyondLastLine: false, // 滚动完最后一行后再滚动一屏幕
		colorDecorators: true, // 颜色装饰器
		accessibilitySupport: 'auto', // 辅助功能支持  "auto" | "off" | "on"
		lineNumbers: 'on', // 行号 取值： "on" | "off" | "relative" | "interval" | function
		lineNumbersMinChars: 5, // 行号最小字符   number
		//enableSplitViewResizing: false,
		readOnly: false, //是否只读  取值 true | false
		renderLineHighlight: 'none', // 当前行突出显示方式  "all" | "line" | "none" | "gutter"
		minimap: {
			// 缩略图
			size: 'fill',
			enabled: false,
		},
	});

	// 添加按钮
	sqlScriptMonacoEditor.addOverlayWidget({
		getId: function () {
			return 'monaco.editor.neuz.parseSqlButton';
		},
		getDomNode: function () {
			return parseSqlButtonRef.value?.$el;
		},
		getPosition: function () {
			return {
				preference: monaco.editor.OverlayWidgetPositionPreference.TOP_RIGHT_CORNER,
			};
		},
	});
};

// 初始化monacoEditor对象
var apiParamsMonacoEditor: monaco.editor.IStandaloneCodeEditor;
const initApiParamsMonacoEditor = () => {
	apiParamsMonacoEditor = monaco.editor.create(apiParamsMonacoEditorRef.value, {
		theme: getMonacoTheme(), // vs / vs-dark 由主题决定
		value: '', // 默认显示的值
		language: 'json',
		formatOnPaste: true,
		wordWrap: 'on', //自动换行，注意大小写
		wrappingIndent: 'indent',
		folding: true, // 是否折叠
		foldingHighlight: true, // 折叠等高线
		foldingStrategy: 'indentation', // 折叠方式  auto | indentation
		showFoldingControls: 'always', // 是否一直显示折叠 always | mouSEOver
		disableLayerHinting: true, // 等宽优化
		emptySelectionClipboard: false, // 空选择剪切板
		selectionClipboard: false, // 选择剪切板
		automaticLayout: true, // 自动布局
		codeLens: false, // 代码镜头
		scrollBeyondLastLine: false, // 滚动完最后一行后再滚动一屏幕
		colorDecorators: true, // 颜色装饰器
		accessibilitySupport: 'auto', // 辅助功能支持  "auto" | "off" | "on"
		lineNumbers: 'on', // 行号 取值： "on" | "off" | "relative" | "interval" | function
		lineNumbersMinChars: 5, // 行号最小字符   number
		//enableSplitViewResizing: false,
		readOnly: false, //是否只读  取值 true | false
		renderLineHighlight: 'none', // 当前行突出显示方式  "all" | "line" | "none" | "gutter"
	});
};

/** 后端内置参数 */
const buildInParams = [
	{ name: '@curTenantId', description: '当前租户Id' },
	{ name: '@curUserId', description: '当前用户Id' },
];

const fieldConfig = reactive({
	gridOptions: useVxeTable<SysReportField>(
		{
			id: 'sysReportConfigEditForm_fields',
			columns: [
				{ type: 'checkbox', width: 60, fixed: 'left' },
				{ field: 'fieldName', title: '字段名', editRender: { name: 'ElInput', props: { size: 'small' } }, dragSort: true },
				{ field: 'title', title: '字段标题', editRender: { name: 'ElInput', props: { size: 'small' } } },
				{ field: 'dataType', title: '数据类型' },
				{ field: 'isSummary', title: '是否合计', slots: { default: 'row_isSummary' } },
				{ field: 'visible', title: '是否显示', slots: { default: 'row_visible' } },
				{ field: 'groupTitle', title: '分组标题', editRender: { name: 'ElInput', props: { size: 'small' } } },
				{ field: 'width', title: '列宽', editRender: { name: 'ElInputNumber', props: { size: 'small', min: 0, controlsPosition: 'right' } } },
			],
		},
		{
			align: 'left',
			// columnConfig: {
			// 	width: 120, // 列默认宽度
			// },
			toolbarConfig: {
				refresh: false,
				import: false,
				export: false,
				print: false,
				zoom: false,
				custom: false,
			},
			rowConfig: {
				drag: true,
			},
			editConfig: {
				trigger: 'click',
				mode: 'row',
				// showStatus: true,
				autoClear: false,
			},
			keyboardConfig: {
				isArrow: true,
			},
			editRules: {
				fieldName: [{ required: true, message: '字段名不能为空' }],
			},
		}
	),
});

const paramConfig = reactive({
	gridOptions: useVxeTable<SysReportField>(
		{
			id: 'sysReportConfigEditForm_param',
			columns: [
				{ type: 'checkbox', width: 60, fixed: 'left' },
				{ field: 'paramName', title: '参数名', editRender: { name: 'ElInput', props: { size: 'small' } } },
				{ field: 'title', title: '参数标题', editRender: { name: 'ElInput', props: { size: 'small' } } },
				{
					field: 'inputCtrl',
					title: '输入控件类型',
					editRender: {
						name: 'ElSelect',
						options: [
							{ label: '输入框', value: 'Input' },
							{ label: '日期选择器', value: 'Date' },
							{ label: '选择器', value: 'Select' },
						],
						props: { size: 'small', filterable: true },
					},
				},
				{ field: 'defaultValue', title: '默认值', editRender: { name: 'ElInput', props: { size: 'small' } } },
			],
		},
		{
			align: 'left',
			// columnConfig: {
			// 	width: 130, // 列默认宽度
			// },
			toolbarConfig: {
				refresh: false,
				import: false,
				export: false,
				print: false,
				zoom: false,
				custom: false,
			},
			editConfig: {
				trigger: 'click',
				mode: 'row',
				// showStatus: true,
				autoClear: false,
			},
			keyboardConfig: {
				isArrow: true,
			},
			editRules: {
				paramName: [{ required: true, message: '参数名不能为空' }],
				title: [{ required: true, message: '参数标题不能为空' }],
				inputCtrl: [{ required: true, message: '输入控件类型不能为空' }],
			},
		}
	),
});

const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateReportConfigInput,
	activeName: 'field',
	/** 字段列表数据 */
	fieldListData: [] as SysReportField[],
	/** 参数列表数据 */
	paramListData: [] as SysReportParam[],
	/** 数据源列表数据 */
	dataSourceList: [] as ReportDataSourceOutput[],
	/** 分组列表数据 */
	groupList: [] as SysReportGroup[],
});

const rules = computed(() => {
	const data = {
		name: [{ required: true, message: '名称不能为空' }],
		dsType: [{ required: true, message: '数据源类型不能为空' }],
	} as any;

	if (state.ruleForm.dsType == ReportConfigDsTypeEnum.NUMBER_0) {
		data.dataSource = [{ required: true, message: '数据源不能为空' }];
	} else {
		data.apiUrl = [{ required: true, message: '接口地址不能为空' }];
		data.apiHttpMethod = [{ required: true, message: '接口请求方式不能为空' }];
	}

	return data;
});

onMounted(() => {
	getAPI(SysReportDataSourceApi)
		.apiSysReportDataSourceDataSourceListGet()
		.then((result) => {
			if (result.data.type !== 'success') {
				return;
			}

			state.dataSourceList = result.data.result!;
		});
	getAPI(SysReportGroupApi)
		.apiSysReportGroupListGet()
		.then((res) => {
			state.groupList = res.data.result!;
		});
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));

	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();

	const defaultSqlValue = `-- SELECT * FROM xxx WHERE number = @number AND createTime >= @time AND tenantId=@curTenantId
-- sp_name`;

	// 延迟拿值防止取不到
	setTimeout(() => {
		if (sqlScriptMonacoEditor == null || sqlScriptMonacoEditor == undefined) initSqlScriptMonacoEditor();
		sqlScriptMonacoEditor!.setValue(state.ruleForm.sqlScript == undefined || state.ruleForm.sqlScript == null || state.ruleForm.sqlScript == '' ? defaultSqlValue : state.ruleForm.sqlScript);

		if (apiParamsMonacoEditor == null || apiParamsMonacoEditor == undefined) initApiParamsMonacoEditor();
		apiParamsMonacoEditor!.setValue(state.ruleForm.apiParams ?? '');
	}, 0);

	setTimeout(() => {
		state.fieldListData = state.ruleForm.fields ? JSON.parse(state.ruleForm.fields) : [];
		state.paramListData = state.ruleForm.params ? JSON.parse(state.ruleForm.params) : [];
	}, 0);
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 提交
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		// 构造提交数据
		const fieldFullData = vFieldGrid.value!.getFullData();
		const paramFullData = vParamGrid.value!.getFullData();
		const record = state.ruleForm;
		record.sqlScript = sqlScriptMonacoEditor.getValue();
		record.apiParams = apiParamsMonacoEditor.getValue();
		record.fields = JSON.stringify(fieldFullData);
		record.params = JSON.stringify(paramFullData);

		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(SysReportConfigApi).apiSysReportConfigUpdatePost(record);
		} else {
			await getAPI(SysReportConfigApi).apiSysReportConfigAddPost(record);
		}
		closeDialog();
	});
};

/** 在新增字段前执行 */
const beforeAddField = (row: SysReportField): Promise<void> => {
	row.isSummary = false;
	row.visible = true;
	row.width = 0;

	return Promise.resolve();
};

/** 解析SQL */
const parseSqlClick = () => {
	const paramFullData = vParamGrid.value!.getFullData();
	const execParams: Record<string, string> = {};
	for (const item of paramFullData) {
		if (item.paramName) {
			execParams[item.paramName] = '';
		}
	}

	return getAPI(SysReportConfigApi)
		.apiSysReportConfigParseSqlPost({ dataSource: state.ruleForm.dataSource, sqlScript: sqlScriptMonacoEditor.getValue(), execParams: execParams })
		.then((res) => {
			// 解析失败时拦截器已弹出错误提示，这里直接中断，不再提示解析成功
			if (res.data.type !== 'success') return;

			ElMessage.success('解析成功');

			const fieldList = vFieldGrid.value!.getFullData();
			const reportFieldList = res.data.result ?? [];

			reportFieldList.forEach((reportField: SysReportField) => {
				if (fieldList.some((field) => field.fieldName === reportField.fieldName)) return;

				const field: SysReportField = { fieldName: reportField.fieldName, title: reportField.title, dataType: reportField.dataType, isSummary: false, visible: reportField.visible, width: 0 };
				vFieldGrid.value!.insertAt(field, -1);
			});
		});
};

/** 新增行 */
const add = async (grid: InstanceType<typeof VxeGrid>, beforeAdd: ((row: any) => Promise<void>) | undefined = undefined): Promise<void> => {
	try {
		const newRow = {};
		beforeAdd && (await beforeAdd(newRow));

		const { row } = await grid.insertAt(newRow, -1);
		await grid.setEditRow(row, true);
	} catch (err: any) {
		ElMessage.error(err.message || err);
	}
};

/** 插入行 */
const insert = async (grid: InstanceType<typeof VxeGrid>): Promise<void> => {
	try {
		const newRow = {};

		const curRow = grid.getCurrentRecord();
		const { row } = await grid.insertAt(newRow, curRow);
		await grid.setEditRow(row, true);
	} catch (err: any) {
		ElMessage.error(err.message || err);
	}
};

/** 删除行 */
const remove = async (grid: InstanceType<typeof VxeGrid>): Promise<void> => {
	try {
		let checkedRows = grid.getCheckboxRecords();
		if (checkedRows === null || checkedRows.length === 0) {
			if (checkedRows === null) {
				checkedRows = [];
			}
			const curRow = grid.getCurrentRecord();
			if (curRow !== null) checkedRows.push(curRow);
		}

		if (!checkedRows || checkedRows.length === 0) return;

		await Promise.all(
			checkedRows.map(async (row: any): Promise<void> => {
				await grid.remove(row);
			})
		);

		await grid.clearCheckboxRow();
		await grid.clearCurrentRow();
	} catch (err: any) {
		ElMessage.error(err.message || err);
	}
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
:deep(.el-dialog__body) {
	height: calc(100vh - 18px) !important;
	max-height: calc(100vh - 116px) !important;
	display: flex;
	flex-direction: column;
}

:deep(.el-tabs) {
	height: 100%;
	display: flex;

	.el-tabs__content {
		flex: 1;

		.el-tab-pane {
			height: 100%;
			display: flex;
			flex-direction: column;
		}
	}
}

.report-config-tab {
	flex: 1;
}
</style>

<style lang="scss">
.sys-report-config-param-build-in-card-header {
	padding: 8px;
}
</style>
