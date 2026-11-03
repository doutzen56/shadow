<template>
	<div class="sys-dbTable-container">
		<vxe-modal v-model="state.visible" title="增加表" show-footer show-confirm-button show-cancel-button fullscreen show-zoom resize width="100vw" height="100vh" @close="cancel">
			<template #default>
				<!-- 分组标题：不用 el-divider，分隔线会把表单和表格割裂开 -->
				<div class="at-sec">
					<span class="at-sec-ico"><el-icon><ele-Memo /></el-icon></span>
					<span class="at-sec-title">数据表信息</span>
					<span class="at-sec-sub">表名会直接作为数据库表名，描述用于生成实体注释</span>
				</div>
				<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
					<el-row :gutter="10">
						<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item label="表名称" prop="tableName" :rules="[{ required: true, message: '名称不能为空', trigger: 'blur' }]">
								<el-input v-model.lazy.trim="state.ruleForm.tableName" placeholder="表名称" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item label="描述" prop="description" :rules="[{ required: true, message: '描述不能为空', trigger: 'blur' }]">
								<el-input v-model.lazy.trim="state.ruleForm.description" placeholder="描述" clearable />
							</el-form-item>
						</el-col>
					</el-row>
				</el-form>

				<div class="at-sec">
					<span class="at-sec-ico is-green"><el-icon><ele-Menu /></el-icon></span>
					<span class="at-sec-title">数据列信息</span>
					<span class="at-sec-sub">可拖动行首图标调整顺序；至少保留一个字段</span>
					<span class="at-sec-count">{{ state.columnCount }} 个字段</span>
				</div>
				<div class="at-body">
					<vxe-grid ref="xGrid" class="xGrid-table-style" v-bind="options">
						<template #toolbar_buttons>
							<el-button-group>
								<el-button icon="ele-Key" @click="addPrimaryColumn">新增主键字段</el-button>
								<el-button icon="ele-Plus" @click="addColumn">新增普通字段</el-button>
								<el-button icon="ele-OfficeBuilding" @click="addTenantColumn">新增租户字段</el-button>
								<el-button icon="ele-Files" @click="addBaseColumn">新增基础字段</el-button>
							</el-button-group>
							<el-button icon="ele-Delete" type="danger" class="at-tb-gap" @click="deleteSelected">删除选中</el-button>
						</template>
						<template #toolbar_tools> </template>
						<template #empty>
							<div class="at-empty">
								<span class="at-empty-ico"><el-icon><ele-Menu /></el-icon></span>
								<p class="at-empty-text">还没有字段，用上方按钮添加；「新增基础字段」会一次带出编码、名称、创建人等通用列</p>
							</div>
						</template>
						<template #drag_default="{}">
							<span class="drag-btn">
								<i class="fa fa-arrows"></i>
							</span>
						</template>
						<template #isPrimarykey="{ row }">
							<vxe-switch readonly v-model="row.isPrimarykey" open-label="是" close-label="否" :openValue="1" :closeValue="0"></vxe-switch>
						</template>
						<template #isIdentity="{ row }">
							<vxe-switch readonly v-model="row.isIdentity" open-label="是" close-label="否" :openValue="1" :closeValue="0"></vxe-switch>
						</template>
						<template #isNullable="{ row }">
							<vxe-switch readonly v-model="row.isNullable" open-label="是" close-label="否" :openValue="1" :closeValue="0"></vxe-switch>
						</template>
						<template #operate="{ row }">
							<vxe-button mode="text" status="error" icon="vxe-icon-delete" @click="deleteRow(row)" />
						</template>
					</vxe-grid>
				</div>
			</template>
			<template #footer>
				<vxe-button icon="vxe-icon-error-circle-fill" @click="cancel">取消</vxe-button>
				<vxe-button status="primary" icon="vxe-icon-success-circle-fill" @click="submit">确定</vxe-button>
			</template>
		</vxe-modal>
	</div>
</template>

<script lang="tsx" setup name="sysAddTable">
import { nextTick, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridProps } from 'vxe-table';
import Sortable from 'sortablejs';
import { baseColumnPresets, dataTypeList, typeMetaOf } from '../database';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';
import { UpdateDbTableInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const emits = defineEmits(['addTableSubmitted']);
const ruleFormRef = ref();
const state = reactive({
	visible: false,
	ruleForm: {} as UpdateDbTableInput,
	sortable: undefined as any,
	// 字段数不能写成 computed：getTableData() 不在响应式依赖里，增删行不会触发重算
	columnCount: 0,
});

const refreshCount = () => {
	state.columnCount = xGrid.value?.getTableData().fullData?.length ?? 0;
};

// 表格参数配置
const options = reactive<VxeGridProps>({
	id: 'sysAddTable',
	height: 'auto',
	autoResize: true,
	size: 'mini',
	loading: false,
	align: 'center',
	data: [] as Array<any>,
	rowConfig: { useKey: true },
	mouseConfig: {
		selected: true,
	},
	keyboardConfig: {
		isArrow: true,
		isDel: true,
		isEnter: true,
		isTab: true,
		isEdit: true,
	},
	seqConfig: { seqMethod: ({ row }) => row.orderNo },
	columns: [
		{
			field: 'drag',
			width: 80,
			slots: {
				default: 'drag_default',
			},
		},
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'checkbox', type: 'checkbox', width: 40 },
		{
			field: 'dbColumnName',
			title: '字段名',
			minWidth: 160,
			showOverflow: 'tooltip',
			editRender: { name: '$input', props: { clearable: true, placeholder: '请输入字段名' } },
		},
		{
			field: 'columnDescription',
			title: '描述',
			minWidth: 220,
			showOverflow: 'tooltip',
			editRender: { name: '$input', props: { clearable: true, placeholder: '请输入描述' } },
		},
		{
			field: 'isPrimarykey',
			title: '主键',
			minWidth: 100,
			editRender: { name: '$switch', props: { openValue: 1, closeValue: 0, openLabel: '是', closeLabel: '否', immediate: true } },
			slots: {
				default: 'isPrimarykey',
			},
		},
		{
			field: 'isIdentity',
			title: '自增',
			minWidth: 100,
			editRender: { name: '$switch', props: { openValue: 1, closeValue: 0, openLabel: '是', closeLabel: '否' } },
			slots: {
				default: 'isIdentity',
			},
		},
		{
			field: 'dataType',
			title: '类型',
			minWidth: 120,
			editRender: {
				name: '$select',
				options: dataTypeList,
				optionProps: { value: 'value', label: 'value' },
				props: { optionProps: { value: 'value', label: 'value' }, placeholder: '请选择类型', popupClassName: 'zIndex2023' },
			},
		},
		{
			field: 'isNullable',
			title: '可空',
			minWidth: 100,
			editRender: { name: '$switch', props: { openValue: 1, closeValue: 0, openLabel: '是', closeLabel: '否' } },
			slots: {
				default: 'isNullable',
			},
		},
		{
			field: 'length',
			title: '长度',
			minWidth: 100,
			showOverflow: 'tooltip',
			editRender: { name: '$input', props: { type: 'integer', clearable: true, placeholder: '请输入长度' } },
		},
		{
			field: 'decimalDigits',
			title: '小数位',
			minWidth: 100,
			showOverflow: 'tooltip',
			editRender: { name: '$input', props: { type: 'integer', clearable: true, placeholder: '请输入小数位' } },
		},
		{
			field: 'defaultValue',
			title: '默认值',
			minWidth: 100,
			showOverflow: 'tooltip',
			editRender: { name: '$input', props: { clearable: true, placeholder: '请输入默认值' } },
		},
		{
			field: '操作',
			title: '操作',
			width: 'auto',
			showOverflow: true,
			fixed: 'right',
			slots: {
				default: 'operate',
			},
		},
	],
	editRules: {
		dbColumnName: [{ required: true, message: '字段名必填' }],
		dataType: [{ required: true, message: '类型必填' }],
	},
	toolbarConfig: {
		size: 'small',
		slots: { buttons: 'toolbar_buttons', tools: 'toolbar_tools' },
		// 导出/打印未接入服务端方法，按钮点了只会产出空文件，先不开放
		refresh: false,
		export: false,
		print: false,
		zoom: false,
		custom: false,
	},
	checkboxConfig: { range: true },
	sortConfig: {
		trigger: 'cell',
		remote: false,
	},
	editConfig: { trigger: 'click', mode: 'row', showStatus: true },
	proxyConfig: {
		props: {
			list: 'data.result.items', // 不分页时
			total: 'data.result.total',
			message: 'data.message',
		},
		ajax: {
			query: () => Promise.resolve(), // 不加会报错
		},
	},
});

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = row;
	state.visible = true;
	ruleFormRef.value?.resetFields();
	nextTick(() => {
		rowDrop();
		refreshCount();
	});
};

const rowDrop = () => {
	const el = document.querySelector('.xGrid-table-style .vxe-table--body tbody') as HTMLElement;
	if (!el) return;
	state.sortable = Sortable.create(el, {
		animation: 300,
		handle: '.drag-btn',
		onEnd: (sortableEvent: any) => {
			const fullData = xGrid.value?.getTableData().fullData;
			const newIndex = sortableEvent.newIndex as number;
			const oldIndex = sortableEvent.oldIndex as number;
			// 往前移动
			if (oldIndex > newIndex) {
				const moveRow = fullData?.find((e) => e.orderNo == oldIndex + 1);
				for (let i = oldIndex; i > newIndex; i--) {
					const row = fullData?.find((e) => e.orderNo == i);
					row.orderNo += 1;
				}
				moveRow.orderNo = newIndex + 1;
			} else {
				// 往后移动
				const moveRow = fullData?.find((e) => e.orderNo == oldIndex + 1);
				for (let i = oldIndex; i < newIndex; i++) {
					const row = fullData?.find((e) => e.orderNo == i + 2);
					row.orderNo -= 1;
				}
				moveRow.orderNo = newIndex + 1;
			}
			xGrid.value?.updateData();
		},
	});
};

// 关闭弹窗
const closeDialog = () => {
	emits('addTableSubmitted', state.ruleForm.tableName ?? '');
	cancel();
};

// 取消
const cancel = () => {
	xGrid.value?.loadData([]);
	refreshCount();
	state.visible = false;
	if (state.sortable) {
		state.sortable.destroy();
		state.sortable = undefined;
	}
};

// 提交
const submit = async () => {
	try {
		await ruleFormRef.value.validate();
	} catch {
		return;
	}
	const tableData = xGrid.value?.getTableData().fullData ?? [];
	if (tableData.length === 0) {
		ElMessage.error('请至少添加一个列');
		return;
	}
	const rowValid = await xGrid.value?.validate(true);
	if (rowValid) {
		ElMessage.error('列信息校验不通过，请检查标红的单元格');
		return;
	}
	const params: any = {
		dbColumnInfoList: tableData,
		...state.ruleForm,
	};
	await getAPI(SysDatabaseApi).apiSysDatabaseAddTablePost(params);
	closeDialog();
};

/** 统一的新增动作：插入一行并把光标落到字段名上 */
const insertRow = async (row: any) => {
	const fullData = xGrid.value?.getTableData().fullData;
	const colIndex = (fullData?.length ?? 0) + 1;
	const temp: any = await xGrid.value?.insertAt({ ...row, orderNo: colIndex, editable: true, isNew: true }, -1);
	refreshCount();
	if (temp && temp.row) await xGrid.value?.setEditCell(temp.row, 'dbColumnName');
	return temp;
};

// 增加主键列
const addPrimaryColumn = () =>
	insertRow({
		columnDescription: '主键ID',
		dataType: 'bigint',
		dbColumnName: 'Id',
		decimalDigits: 0,
		isIdentity: 0,
		isNullable: 0,
		isPrimarykey: 1,
		length: 0,
	});

// 增加普通列
const addColumn = () =>
	insertRow({
		columnDescription: '',
		dataType: 'varchar',
		dbColumnName: '',
		decimalDigits: 0,
		isIdentity: 0,
		isNullable: 1,
		isPrimarykey: 0,
		length: 64,
	});

// 增加租户列
const addTenantColumn = () =>
	insertRow({
		columnDescription: '租户Id',
		dataType: 'bigint',
		dbColumnName: 'TenantId',
		decimalDigits: 0,
		isIdentity: 0,
		isNullable: 1,
		isPrimarykey: 0,
		length: 0,
	});

// 增加通用基础列
const addBaseColumn = async () => {
	const fullData = xGrid.value?.getTableData().fullData;
	let colIndex = (fullData?.length ?? 0) + 1;
	let temp: any = {};
	for (const field of baseColumnPresets) {
		const meta = typeMetaOf(field.dataType);
		temp = await xGrid.value?.insertAt(
			{
				columnDescription: field.desc,
				dataType: field.dataType,
				dbColumnName: field.name,
				decimalDigits: 0,
				isIdentity: 0,
				isNullable: field.isNullable === 0 ? 0 : 1,
				isPrimarykey: 0,
				length: meta.hasLength ? ((field as any).length ?? 0) : 0,
				orderNo: colIndex,
				editable: true,
				isNew: true,
			},
			-1
		);
		colIndex++;
	}
	if (temp && temp.row) await xGrid.value?.setEditCell(temp.row, 'dbColumnName');
	refreshCount();
};

// 删除行
const deleteRow = (row: any) => {
	const fullData = xGrid.value?.getTableData().fullData;
	fullData?.filter((e: any) => e.orderNo > row.orderNo)?.forEach((e: any) => (e.orderNo = e.orderNo - 1));
	xGrid.value?.remove(row);
	refreshCount();
};

// 删除选中行
const deleteSelected = () => {
	const selected = xGrid.value?.getCheckboxRecords() ?? [];
	const fullData = xGrid.value?.getTableData().fullData;
	// 更新序号
	for (const item of selected) {
		fullData?.filter((e: any) => e.orderNo > item.orderNo)?.forEach((e: any) => (e.orderNo = e.orderNo - 1));
	}
	// 移除选中
	xGrid.value?.removeCheckboxRow();
	refreshCount();
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
/* 分组标题：替代 el-divider */
.at-sec {
	display: flex;
	align-items: center;
	gap: 8px;
	margin: 4px 0 12px;
	padding-bottom: 8px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.at-sec-ico {
	flex: none;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 13px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);

	&.is-green {
		color: #10b981;
		background: color-mix(in srgb, #10b981 12%, transparent);
	}
}
.at-sec-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.at-sec-sub {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.at-sec-count {
	flex: none;
	padding: 2px 9px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

.at-body {
	height: calc(100vh - 320px);
	min-height: 260px;
}
.at-tb-gap {
	margin-left: 12px;
}

.xGrid-table-style .drag-btn {
	cursor: move;
	font-size: 20px;
}

/* 拖拽中的行：原来用纯红 #e40000 太刺眼，改成主题色浅底 + 虚线框 */
.xGrid-table-style .vxe-body--row.sortable-ghost,
.xGrid-table-style .vxe-body--row.sortable-chosen {
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent) !important;
	outline: 1px dashed var(--el-color-primary);
}

.at-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 10px;
	padding: 26px 0;
}
.at-empty-ico {
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
}
.at-empty-text {
	margin: 0;
	max-width: 460px;
	text-align: center;
	font-size: 12.5px;
	line-height: 1.7;
	color: var(--el-text-color-secondary);
}
</style>
