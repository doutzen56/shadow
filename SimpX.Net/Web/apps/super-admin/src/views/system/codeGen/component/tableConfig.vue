<template>
	<el-row :gutter="10" v-if="store.ruleForm.tableList?.[tableIndex]">
		<!-- 这一步做什么：先选表，再配字段 -->
		<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
			<div class="tc-guide">
				<span class="tc-guide-ico"><el-icon><ele-Coin /></el-icon></span>
				<div class="tc-guide-txt">
					<div class="tc-guide-title">
						{{ tableIndex === 0 ? '主表' : '从表 ' + tableIndex }}：
						<b>{{ store.ruleForm.tableList[tableIndex].tableName || '尚未选择数据表' }}</b>
					</div>
					<div class="tc-guide-desc">先选「库定位器 → 实体表名」，系统自动带出全部字段；再逐列决定它在列表、表单、查询、导入导出里的行为。</div>
				</div>
				<span v-if="columnCount" class="tc-guide-stat">{{ columnCount }} 个字段</span>
			</div>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20">
			<el-form-item label="库定位器" :prop="`tableList[${tableIndex}].configId`" :rules="[{ required: true, message: '请选择库定位器', trigger: 'blur' }]">
				<el-select v-model="store.ruleForm.tableList[tableIndex].configId" :disabled="!!store.ruleForm.tableList[tableIndex].id" placeholder="库名" filterable @change="dbChanged()" class="w100">
					<el-option v-for="item in store.databaseList ?? []" :key="item.configId" :label="item.configId" :value="item.configId" />
				</el-select>
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20">
			<el-form-item label="实体表名" :prop="`tableList[${tableIndex}].tableName`" :rules="[{ required: true, message: '请选择实体表名', trigger: 'blur' }]">
				<el-select
					v-model="store.ruleForm.tableList[tableIndex as any].tableName"
					:disabled="!!store.ruleForm.tableList[tableIndex].id || !store.ruleForm.tableList[tableIndex].configId"
					@change="tableChanged"
					value-key="tableName"
					filterable
					clearable
					class="w100"
				>
					<template #prefix>
						<el-tooltip raw-content content="若找不到在前端生成的实体/表，请检查配置文件中实体所在程序集或重启后台服务。" placement="top">
							<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</template>
					<el-option v-for="item in state.tableData" :key="item.entityName" :label="item.entityName + ' ( ' + item.tableName + ' ) [' + item.tableComment + ']'" :value="item" />
				</el-select>
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20" v-if="store.getLastLinkLabel(tableIndex)">
			<el-form-item
				:label="store.getLastLinkLabel(tableIndex)"
				:prop="`tableList[${tableIndex}].lastLinkPropertyName`"
				:rules="[{ required: true, message: '请选择' + store.getLastLinkLabel(tableIndex), trigger: 'blur' }]"
			>
				<el-select
					v-model="store.ruleForm.tableList[tableIndex].lastLinkPropertyName"
					:disabled="!!store.ruleForm.tableList[tableIndex].id || !store.ruleForm.tableList[tableIndex].configId"
					filterable
					clearable
					class="w100"
				>
					<template #prefix>
						<el-tooltip raw-content content="与上级组件连表查询的字段" placement="top">
							<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</template>
					<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
				</el-select>
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20" v-if="store.getNextLinkLabel(tableIndex)">
			<el-form-item
				:label="store.getNextLinkLabel(tableIndex)"
				:prop="`tableList[${tableIndex}].nextLinkPropertyName`"
				:rules="[{ required: true, message: '请选择' + store.getNextLinkLabel(tableIndex), trigger: 'blur' }]"
			>
				<el-select
					v-model="store.ruleForm.tableList[tableIndex].nextLinkPropertyName"
					:disabled="!!store.ruleForm.tableList[tableIndex].id || !store.ruleForm.tableList[tableIndex].configId"
					filterable
					clearable
					class="w100"
				>
					<template #prefix>
						<el-tooltip raw-content content="与下级组件连表查询的字段" placement="top">
							<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</template>
					<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
				</el-select>
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20">
			<el-form-item label="业务名" :prop="`tableList[${tableIndex}].busName`" :rules="[{ required: true, message: '业务名不能为空', trigger: 'blur' }]">
				<el-input v-model="store.ruleForm.tableList[tableIndex].busName" placeholder="请输入" clearable />
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6" class="mb20">
			<el-form-item label="模块名" :prop="`tableList[${tableIndex}].moduleName`" :rules="[{ required: true, message: '模块名不能为空', trigger: 'blur' }]">
				<el-input v-model="store.ruleForm.tableList[tableIndex].moduleName" :disabled="!!store.ruleForm.tableList[tableIndex].id" placeholder="请输入" clearable>
					<template #prefix>
						<el-tooltip raw-content content="对应后端Service接口名称前缀" placement="top">
							<el-icon size="16" style="display: inline; vertical-align: middle"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</template>
				</el-input>
			</el-form-item>
		</el-col>
		<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
			<div class="tc-fields-hd">
				<div class="tc-fields-left">
					<span class="tc-fields-title">表字段配置</span>
					<span class="tc-fields-sub">拖动左侧手柄可调整字段顺序</span>
				</div>
				<div class="tc-batch">
					<el-button size="small" icon="ele-View" @click="setAll('isTable', true)">列表全显</el-button>
					<el-button size="small" icon="ele-Hide" @click="setAll('isTable', false)">列表全隐</el-button>
					<el-button size="small" icon="ele-EditPen" @click="setAll('isAddUpdate', true)">全选增改</el-button>
					<el-button size="small" icon="ele-Sort" @click="setAll('isImport', true)">全选导入导出</el-button>
					<el-button size="small" icon="ele-Search" @click="setAll('isQuery', true)">全选查询</el-button>
					<el-button size="small" type="primary" icon="ele-Refresh" :disabled="store.getSyncColumnListButtonDisabled(tableIndex)" @click="asyncColumnList()" v-reclick="1000">
						同步字段
					</el-button>
				</div>
			</div>
			<div v-if="!store.ruleForm.tableList[tableIndex].tableName" class="tc-empty">
				<el-icon class="tc-empty-ico"><ele-FolderOpened /></el-icon>
				<p class="tc-empty-txt">请先选择上方的「库定位器」与「实体表名」</p>
				<p class="tc-empty-sub">选好表后，这里会自动列出全部字段，默认值按字段名智能填充</p>
			</div>
			<vxe-grid v-else ref="xGridRef" class="xGrid-table-style" v-bind="tableOptions" :data="store.ruleForm.tableList[tableIndex].columnList ?? []">
				<template #drag_default="{}">
					<span class="drag-btn">
						<i class="fa fa-arrows"></i>
					</span>
				</template>
				<template #orderNo="{ row }">
					<vxe-input v-model="row.orderNo" autocomplete="off" @change="handelChangeOrderNo" />
				</template>
				<template #effectType="{ row, rowIndex }">
					<vxe-select v-model="row.effectType" @change="effectTypeChange(row, rowIndex)" :disabled="disabledColumn(row)" :style="{ width: '100%' }" placeholder="Select" transfer filterable>
						<vxe-option v-for="item in userInfo.dictList['CodeGenEffectTypeEnum']" :key="item.value" :label="item.label" :value="item.value" />
					</vxe-select>
					<!-- <vxe-button v-if="showConfigButton(row)" style="width: 30%" icon="vxe-icon-edit" @click="effectTypeChange(row, rowIndex)" v-reclick="1000">修改</vxe-button>-->
					<vxe-checkbox v-if="showCheckBox(row)" v-model="row.config.multiple" style="width: 30%" icon="vxe-icon-edit">多选</vxe-checkbox>
				</template>
				<template #defaultValue="{ row }">
					<vxe-input v-model="row.defaultValue" autocomplete="off" :disabled="disabledColumn(row)" />
				</template>
				<template #columnComment="{ row }">
					<vxe-input v-model="row.columnComment" autocomplete="off" :disabled="disabledColumn(row)" />
				</template>
				<template #config="{ row, rowIndex }">
					<span v-if="!row.config"></span>
					<!-- 枚举、字典、常量 -->
					<vxe-select v-else-if="useDictSelector(row)" v-model="row.config.code" class="w100 m-2" :disabled="!useDictSelector(row)" filterable transfer>
						<vxe-option v-if="row.effectType == 101" v-for="item in store.dictList" :key="item.code" :label="item.name" :value="item.code" />
						<vxe-option v-if="row.effectType == 102" v-for="item in store.constList" :key="item.code" :label="item.name" :value="item.code" />
						<vxe-option v-if="row.effectType == 103" v-for="item in store.enumList" :key="item.code" :label="item.name" :value="item.code" />
					</vxe-select>
					<!-- 时间格式 -->
					<vxe-select v-else-if="row.effectType == 107" v-model="row.config.format" placeholder="时间格式" class="w100">
						<vxe-option label="YYYY-mm-dd HH:MM:SS" value="datetime" />
						<vxe-option label="YYYY-mm-dd" value="date" />
						<vxe-option label="HH:MM:SS" value="time" />
					</vxe-select>
					<!-- 树形选择器 -->
					<vxe-input v-else-if="row.effectType == 104" v-model="row.config.entityName" readonly style="width: 67.5%"></vxe-input>
					<!-- 外键 -->
					<vxe-input v-else-if="row.effectType == 105" v-model="row.config.entityName" readonly style="width: 67.5%"></vxe-input>
					<!-- 上传配置 -->
					<vxe-input v-else-if="row.effectType == 109" readonly placeholder="上传配置" style="width: 67.5%"></vxe-input>
					<vxe-button v-if="[104, 105, 109].includes(row.effectType)" @click="effectTypeChange(row, rowIndex)" style="width: 27.5%" icon="vxe-icon-edit">修改</vxe-button>
				</template>
				<template #isTable="{ row }">
					<vxe-checkbox v-model="row.isTable"></vxe-checkbox>
				</template>
				<template #isCopy="{ row }">
					<vxe-checkbox v-model="row.isCopy" v-if="row.isTable"></vxe-checkbox>
				</template>
				<template #isAddUpdate="{ row }">
					<vxe-checkbox v-model="row.isAddUpdate" v-if="!disabledColumn(row)"></vxe-checkbox>
				</template>
				<template #isImport="{ row }">
					<vxe-checkbox v-model="row.isImport" v-if="!disabledColumn(row)"></vxe-checkbox>
				</template>
				<template #isSortable="{ row }">
					<vxe-checkbox v-model="row.isSortable" v-if="row.isTable"></vxe-checkbox>
				</template>
				<template #isRequired="{ row }">
					<vxe-tag v-if="row.isRequired" status="success">是</vxe-tag>
					<vxe-tag v-else status="info">否</vxe-tag>
				</template>
				<template #isStatistical="{ row }">
					<vxe-switch v-model="row.isStatistical" open-label="是" close-label="否" :openValue="true" :closeValue="false"></vxe-switch>
				</template>
				<template #isQuery="{ row }">
					<vxe-switch v-model="row.isQuery" open-label="是" close-label="否" :openValue="true" :closeValue="false"></vxe-switch>
				</template>
				<template #queryType="{ row }">
					<vxe-select v-model="row.queryType" class="m-2" placeholder="Select" :disabled="!row.isQuery" filterable transfer>
						<vxe-option v-for="item in userInfo.dictList['code_gen_query_type']" :key="item.value" :label="item.label" :value="item.value" />
					</vxe-select>
				</template>
				<template #fromValid="{ row }">
					<vxe-select v-model="row.fromValid" class="m-2" placeholder="Select" :disabled="!row.isAddUpdate" filterable clearable transfer>
						<vxe-option v-for="item in userInfo.dictList['CodeGenFromRuleValidEnum']" :key="item.value" :label="item.label" :value="item.value" />
					</vxe-select>
				</template>
			</vxe-grid>
		</el-col>
	</el-row>
	<EffectLinkTableConfig ref="effectLinkTableDialogRef" @submitData="submitRefreshFk" />
	<UploadConfigDialog ref="uploadConfigDialogRef" @submitData="submitRefreshFk" />
	<DateTimeConfigDialog ref="dateTimeConfigDialogRef" @submitData="submitRefreshFk" />
</template>

<!-- 表配置 -->
<script lang="ts" setup name="tableConfig">
import Sortable from 'sortablejs';
import { ref, reactive, watch, computed } from 'vue';
import { useUserInfo } from '/@/stores/userInfo';
import { VxeGridInstance, VxeGridProps } from 'vxe-table';
import { SysCodeGenColumn, TableOutput } from '/@/api-services/system/models';
import EffectLinkTableConfig from './effectLinkTableConfig.vue';
import DateTimeConfigDialog from './dateTimeConfigDialog.vue';
import UploadConfigDialog from './uploadConfigDialog.vue';
import { useCodeGenStore } from './codeGenStore';
import debounce from 'xe-utils/debounce';

const store = useCodeGenStore();
const uploadConfigDialogRef = ref();
const dateTimeConfigDialogRef = ref();
const effectLinkTableDialogRef = ref();
const xGridRef = ref<VxeGridInstance<SysCodeGenColumn>>();
const props = defineProps<{
	index: number;
}>();
const tableIndex = computed(() => props.index);

const userInfo = useUserInfo();
const state = reactive({
	index: 0,
	tableData: [] as TableOutput[] | any,
	columnList: [] as Array<SysCodeGenColumn>,
});

// 表格参数配置
const tableOptions = reactive<VxeGridProps<SysCodeGenColumn>>({
	id: 'genConfigDialog',
	height: 580,
	keepSource: true,
	autoResize: true,
	loading: false,
	align: 'center',
	rowConfig: { useKey: true },
	tooltipConfig: { enabled: true, theme: 'dark' },
	seqConfig: { seqMethod: ({ row }) => row.orderNo as number },
	columns: [
		{ field: 'id', width: 40, slots: { default: 'drag_default' } },
		{ field: 'orderNo', title: '排序', minWidth: 40, showOverflow: 'tooltip', titlePrefix: { content: '字段在页面上的显示顺序，拖动左侧手柄也能排' }, slots: { default: 'orderNo' } },
		{ field: 'columnName', title: '字段', minWidth: 100, align: 'left', showOverflow: 'tooltip' },
		{ field: 'defaultValue', title: '默认值', minWidth: 80, align: 'left', showOverflow: 'tooltip', titlePrefix: { content: '新增记录时该字段的初始值，留空则由数据库决定' }, slots: { default: 'defaultValue' } },
		{ field: 'columnComment', title: '描述', minWidth: 100, showOverflow: 'tooltip', titlePrefix: { content: '页面上显示的中文名，来自数据库字段注释，可在此改写' }, slots: { default: 'columnComment' } },
		{ field: 'netType', title: '数据类型', minWidth: 90, align: 'left' },
		{ field: 'effectType', title: '控件类型', minWidth: 160, titlePrefix: { content: '页面用什么控件展示这个字段：文本框、下拉、字典、日期、上传等' }, slots: { default: 'effectType' } },
		{ field: 'config', title: '控件配置', minWidth: 180, titlePrefix: { content: '控件的附加参数，例如绑定哪个字典、关联哪张表' }, slots: { default: 'config' } },
		{ field: 'isTable', title: '列表显示', minWidth: 70, titlePrefix: { content: '勾选后该字段作为列表页的一列' }, slots: { default: 'isTable' } },
		{ field: 'isCopy', title: '复制', minWidth: 40, titlePrefix: { content: '列表显示时，勾选后允许一键复制该列内容' }, slots: { default: 'isCopy' } },
		{ field: 'isAddUpdate', title: '增改字段', minWidth: 70, titlePrefix: { content: '勾选后该字段出现在新增 / 编辑弹窗里' }, slots: { default: 'isAddUpdate' } },
		{ field: 'isImport', title: '导入导出', minWidth: 70, titlePrefix: { content: '勾选后该字段参与 Excel 导入与导出' }, slots: { default: 'isImport' } },
		{ field: 'isRequired', title: '必填字段', minWidth: 70, titlePrefix: { content: '数据库非空字段自动为必填，无法修改' }, slots: { default: 'isRequired' } },
		{ field: 'isSortable', title: '排序字段', minWidth: 70, titlePrefix: { content: '列表显示时，该列支持点击表头排序' }, slots: { default: 'isSortable' } },
		{ field: 'isStatistical', title: '统计字段', minWidth: 70, titlePrefix: { content: '列表底部对该列做汇总统计（求和 / 计数）' }, slots: { default: 'isStatistical' } },
		{ field: 'isQuery', title: '查询字段', minWidth: 70, titlePrefix: { content: '勾选后该字段出现在列表页的查询区' }, slots: { default: 'isQuery' } },
		{ field: 'queryType', title: '查询方式', minWidth: 120, titlePrefix: { content: '查询的匹配方式：等于、包含、范围等，开启查询字段后可选' }, slots: { default: 'queryType' } },
		{ field: 'fromValid', title: '校验规则', width: 130, showOverflow: true, titlePrefix: { content: '表单填写时的校验规则：手机号、邮箱、身份证等' }, slots: { default: 'fromValid' } },
	],
	editConfig: { trigger: 'click', mode: 'row', showStatus: true },
	data: [],
});

// 当前表的字段数
const columnCount = computed(() => store.ruleForm.tableList?.[tableIndex.value]?.columnList?.length ?? 0);

// 批量设置某一列的开关，省得逐行点
const setAll = (key: string, val: boolean) => {
	const list = (store.ruleForm.tableList?.[tableIndex.value]?.columnList ?? []) as any[];
	list.forEach((c: any) => {
		// 公共字段与主键不参与增改 / 导入导出
		if ((key === 'isAddUpdate' || key === 'isImport') && disabledColumn(c)) return;
		c[key] = val;
	});
};

const reset = (type: string) => {
	if (type === 'db') {
		store.ruleForm.tableList[tableIndex.value]!.busName = undefined as any;
		store.ruleForm.tableList[tableIndex.value]!.moduleName = undefined as any;
		store.ruleForm.tableList[tableIndex.value]!.tableName = undefined as any;
		store.ruleForm.tableList[tableIndex.value]!.entityName = undefined as any;
	}
	store.ruleForm.tableList[tableIndex.value].lastLinkPropertyName = undefined;
	store.ruleForm.tableList[tableIndex.value].nextLinkPropertyName = undefined;
};

// 库改变事件
watch(
	() => store.ruleForm.tableList?.[tableIndex.value]?.configId,
	() => reset('db')
);

// 表改变事件
watch(
	() => store.ruleForm.tableList?.[tableIndex.value]?.tableName,
	() => reset('table')
);

// 库改变
const dbChanged = async () => {
	state.tableData = (await store.tableListBy(store.ruleForm.tableList[tableIndex.value].configId)) ?? [];
};

// table改变
const tableChanged = async (item: any) => {
	tableOptions.loading = true;
	try {
		store.ruleForm.tableList[tableIndex.value].tableName = item.tableName;
		store.ruleForm.tableList[tableIndex.value].entityName = item.entityName;
		store.ruleForm.tableList[tableIndex.value].moduleName = item.entityName;
		store.ruleForm.tableList[tableIndex.value].busName = item.tableComment?.replace(/表$/, '');

		const data = store.ruleForm.tableList[tableIndex.value];
		state.columnList = ((await store.columnListBy(data.configId, data.tableName)) ?? []) as any[];
		await asyncColumnList();
	} finally {
		tableOptions.loading = false;
	}
};

// 同步字段列表
const asyncColumnList = async () => {
	tableOptions.loading = true;
	try {
		await store.getDefaultColumnConfigList(tableIndex.value);
		rowDrop();
	} finally {
		tableOptions.loading = false;
	}
};

// 判断是否（用于是否能选择或输入等）
const disabledColumn = (row: any) => row.isCommon || row.isPrimarykey;
//const showConfigButton = (row: any) => useConfigButton(row) && !row.isCommon;
const showCheckBox = (row: any) => useDictSelector(row) && row.netType.startsWith('string') && !row.isCommon;
const useDictSelector = (row: any) => [101, 102, 103].includes(Number(row.effectType) ?? 0);
//const useConfigButton = (row: any) => [104, 105, 107, 109].includes(Number(row.effectType));

// 控件类型改变
const effectTypeChange = (row: any, index: number) => {
	row.effectType = Number(row.effectType);
	if ([101, 102, 103].includes(row.effectType)) {
		row.config = {};
	} else if (row.effectType === 104) {
		openTreeDialog(row, index);
	} else if (row.effectType === 105) {
		openFkDialog(row, index);
	} else if (row.effectType === 107) {
		openDateTimeDialog(row, index);
	} else if (row.effectType === 109) {
		openUploadDialog(row, index);
	} else {
		row.config = undefined;
	}
	// 选择器类型，默认使用 “等于” 查询方式
	if (row.effectType <= 105) row.queryType = '==';
};

// 打开外键弹窗
const openFkDialog = (row: any, index: number) => {
	row.index = index;
	effectLinkTableDialogRef.value.openDialog(row, 'fk');
};

// 打开树形弹窗
const openTreeDialog = (row: any, index: number) => {
	row.index = index;
	effectLinkTableDialogRef.value.openDialog(row, 'tree');
};

// 打开上传弹窗
const openUploadDialog = (row: any, index: number) => {
	row.index = index;
	uploadConfigDialogRef.value.openDialog(row);
};

// 打开时间弹窗
const openDateTimeDialog = (row: any, index: number) => {
	row.index = index;
	dateTimeConfigDialogRef.value.openDialog(row);
};

// 更新主键
const submitRefreshFk = (row: any) => {
	let tableData = xGridRef.value?.getFullData() || [];
	tableData[row.index] = row;
	xGridRef.value?.loadData(tableData);
};

// 序号改变事件
const handelChangeOrderNo = debounce(() => {
	let tableData = xGridRef.value?.getFullData() || [];
	tableData = tableData.sort((a: any, b: any) => a.orderNo - b.orderNo);
	xGridRef.value?.loadData(tableData);
}, 1000);

const rowDrop = () => {
	const el = document.querySelector('.xGrid-table-style .vxe-table--body tbody') as HTMLElement;
	Sortable.create(el, {
		animation: 300,
		handle: '.drag-btn',
		onEnd: (sortableEvent: any) => {
			const fullData = xGridRef.value?.getTableData().fullData || [];
			const newIndex = sortableEvent.newIndex as number;
			const oldIndex = sortableEvent.oldIndex as number;

			if (newIndex === undefined || oldIndex === undefined || newIndex === oldIndex) return;

			const currentRow = fullData.splice(oldIndex, 1)[0];
			fullData.splice(newIndex, 0, currentRow);
			fullData.forEach((u, i) => (u.orderNo = 5 + i * 5));

			// 重新分配 orderNo 以保持原有差距
			let prevOrderNo = newIndex > oldIndex ? fullData[newIndex - 1]?.orderNo : fullData[newIndex + 1]?.orderNo;
			let nextOrderNo = newIndex > oldIndex ? fullData[newIndex + 1]?.orderNo : fullData[newIndex - 1]?.orderNo;

			if (prevOrderNo !== undefined && nextOrderNo !== undefined) {
				currentRow.orderNo = (prevOrderNo + nextOrderNo) / 2;
			} else if (prevOrderNo !== undefined) {
				currentRow.orderNo = prevOrderNo + 5; // 假设最小差距为 5
				if (newIndex == 0) currentRow.orderNo = 5;
			} else if (nextOrderNo !== undefined) {
				currentRow.orderNo = nextOrderNo - 5; // 假设最小差距为 5
			}

			// 更新表格数据
			xGridRef.value?.loadData(fullData);
		},
	});
};
</script>

<style lang="scss" scoped>
.tc-guide {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 16px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
}
.tc-guide-ico {
	flex: none;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}
.tc-guide-txt {
	flex: 1 1 auto;
	min-width: 0;
}
.tc-guide-title {
	font-size: 13.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	b {
		color: var(--el-color-primary);
	}
}
.tc-guide-desc {
	margin-top: 3px;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-text-color-secondary);
}
.tc-guide-stat {
	flex: none;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

.tc-fields-hd {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px 12px;
	flex-wrap: wrap;
	margin-bottom: 10px;
}
.tc-fields-left {
	display: flex;
	align-items: baseline;
	gap: 8px;
	min-width: 0;
}
.tc-fields-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.tc-fields-sub {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.tc-batch {
	display: flex;
	gap: 8px;
	flex-wrap: wrap;
}

.tc-empty {
	padding: 44px 20px;
	text-align: center;
	border: 1px dashed var(--el-border-color);
	border-radius: 12px;
	background: var(--el-fill-color-blank);
}
.tc-empty-ico {
	font-size: 38px;
	color: color-mix(in srgb, var(--el-color-primary) 45%, transparent);
}
.tc-empty-txt {
	margin-top: 10px;
	font-size: 13.5px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}
.tc-empty-sub {
	margin-top: 5px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.xGrid-table-style .drag-btn {
	cursor: move;
	font-size: 20px;
}
:deep(.vxe-cell) {
	height: auto !important;
}
</style>
