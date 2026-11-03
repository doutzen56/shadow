<template>
	<div class="sys-linkTableConfig-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="760px" class="ltc-dialog">
			<template #header>
				<div class="ltc-hd">
					<span class="ltc-hd-ico"><el-icon><ele-Share /></el-icon></span>
					<div class="ltc-hd-txt">
						<span class="ltc-hd-title">{{ state.type == 'tree' ? '树形选择配置' : '外键配置' }}</span>
						<span class="ltc-hd-sub">{{ state.type == 'tree' ? '从一张表里按父子关系生成树形下拉，常用于组织机构、分类目录' : '用另一张表的数据作为本字段的下拉选项，页面显示名称、库里存 Id' }}</span>
					</div>
				</div>
			</template>
			<div class="ltc-tip">
				<el-icon class="ltc-tip-ico"><ele-InfoFilled /></el-icon>
				<span>本字段本身只存「值字段」的值；页面上看到的文字来自「显示字段」，由后台连表查询自动带出。</span>
			</div>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" class="ltc-form">
				<el-form-item prop="configId" :rules="[{ required: true, message: '库定位器不能为空', trigger: 'blur' }]">
					<template #label>
						<span>库定位器 <Tip text="选择实体所在的数据库，与「数据库管理」中的配置一致" /></span>
					</template>
					<el-select clearable v-model="state.ruleForm.configId" placeholder="请选择库名" filterable @change="dbChanged()" class="w100">
						<el-option v-for="item in store.databaseList" :key="item.configId" :label="item.configId" :value="item.configId" />
					</el-select>
				</el-form-item>
				<el-form-item prop="tableName" :rules="[{ required: true, message: '数据库表不能为空', trigger: 'blur' }]">
					<template #label>
						<span>数据来源表 <Tip text="下拉选项的数据来自哪张表，选好后下面三个字段才能选" /></span>
					</template>
					<el-select v-model="state.ruleForm.tableName" @change="tableChanged" filterable clearable placeholder="请选择数据表" class="w100">
						<el-option v-for="item in state.tableData" :key="item.entityName" :label="item.tableName + ' [' + item.tableComment + ']'" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item prop="displayPropertyNames" :rules="[{ required: true, message: '显示字段不能为空', trigger: 'blur' }]">
					<template #label>
						<span>显示字段 <Tip text="下拉里给用户看的文字，如 Name、Title" /></span>
					</template>
					<el-select
						v-model="state.ruleForm.displayPropertyNames"
						@change="(val: any) => changePropertyName(val, 'display')"
						:disabled="!state.columnList?.length"
						filterable
						placeholder="先选数据表"
						class="w100"
					>
						<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
					</el-select>
				</el-form-item>
				<el-form-item prop="linkPropertyName" :rules="[{ required: true, message: '值字段不能为空', trigger: 'blur' }]">
					<template #label>
						<span>值字段 <Tip text="真正存进本字段的值，通常是 Id" /></span>
					</template>
					<el-select v-model="state.ruleForm.linkPropertyName" @change="(val: any) => changePropertyName(val, 'link')" :disabled="!state.columnList?.length" filterable placeholder="先选数据表" class="w100">
						<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item v-if="state.type == 'tree'" prop="parentPropertyName">
					<template #label>
						<span>父级字段 <Tip text="指向自身上级的字段，如 Pid；留空则按无层级处理" /></span>
					</template>
					<el-select v-model="state.ruleForm.parentPropertyName" @change="(val: any) => changePropertyName(val, 'parent')" :disabled="!state.columnList?.length" filterable clearable placeholder="先选数据表" class="w100">
						<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item prop="searchPropertyName">
					<template #label>
						<span>搜索字段 <Tip text="选填：下拉支持输入搜索时，按这个字段模糊匹配" /></span>
					</template>
					<el-select v-model="state.ruleForm.searchPropertyName" @change="(val: any) => changePropertyName(val, 'search')" :disabled="!state.columnList?.length" filterable clearable placeholder="选填" class="w100">
						<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item v-if="rowData.netType?.startsWith('string')" prop="multiple" :rules="[{ required: true, message: '是否多选不能为空', trigger: 'blur' }]">
					<template #label>
						<span>是否多选 <Tip text="仅字符串字段支持：多个值以逗号分隔存储" /></span>
					</template>
					<el-radio-group v-model="state.ruleForm.multiple">
						<el-radio :value="true">是</el-radio>
						<el-radio :value="false">否</el-radio>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="快捷设置">
					<div class="ltc-quick">
						<el-button type="primary" icon="ele-Search" plain @click="handelQuickConfig(state.type == 'tree' ? 'sysOrg' : 'sysUser')">
							{{ state.type == 'tree' ? '套用组织机构选择器' : '套用系统账号选择器' }}
						</el-button>
						<span class="ltc-quick-tip">一键填入系统内置选择器的全部参数，可再自行微调</span>
					</div>
				</el-form-item>
			</el-form>
			<div class="ltc-preview">
				<span class="ltc-preview-title">生成效果</span>
				<span class="ltc-preview-txt">
					页面展示 <b>{{ state.ruleForm.displayPropertyNames || '显示字段' }}</b>
					<template v-if="state.type == 'tree'">，按 <b>{{ state.ruleForm.parentPropertyName || '父级字段' }}</b> 组成树</template>
					，选中后本字段保存 <b>{{ state.ruleForm.linkPropertyName || '值字段' }}</b>
				</span>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<!-- 外键配置 -->
<script lang="ts" setup name="sysLinkTableConfig">
import { reactive, ref, h } from 'vue';
import { ElTooltip } from 'element-plus';
import { useCodeGenStore } from './codeGenStore';

// 表单标签后的问号提示
const Tip = (props: { text: string }) => h(ElTooltip, { content: props.text, placement: 'top' }, { default: () => h('span', { class: 'ltc-q' }, '?') });

let rowData = {} as any;
const store = useCodeGenStore();
const emits = defineEmits(['submitData']);
const ruleFormRef = ref();
const state = reactive({
	type: 'fk' as 'fk' | 'tree',
	isShowDialog: false,
	ruleForm: {} as any,
	dbData: [] as any,
	tableData: [] as any,
	columnList: [] as any,
});

const changePropertyName = (item: any, type: string) => {
	state.ruleForm[type + 'PropertyName'] = item.propertyName;
	state.ruleForm[type + 'PropertyType'] = item.netType;
};

const dbChanged = async () => {
	state.tableData = await store.tableListBy(state.ruleForm.configId);
};

const tableChanged = async (item: any) => {
	state.ruleForm.displayPropertyNames = undefined;
	state.ruleForm.parentPropertyName = undefined;
	state.ruleForm.searchPropertyName = undefined;
	state.ruleForm.linkPropertyName = undefined;
	state.ruleForm.tableComment = item.tableComment;
	state.ruleForm.entityName = item.entityName;
	state.ruleForm.tableName = item.tableName;
	state.columnList = await store.columnListBy(state.ruleForm.configId, state.ruleForm.tableName);
};

const handelQuickConfig = (type: string) => {
	if (store.quickConfigMap && store.quickConfigMap[type]) {
		state.ruleForm = Object.assign({}, store.quickConfigMap[type]);
	}
};

// 打开弹窗
const openDialog = async (row: any, type: 'fk' | 'tree') => {
	rowData = row;
	state.type = type;
	state.isShowDialog = true;
	state.ruleForm.multiple ??= false;
	state.ruleForm.useTable ??= false;
	state.ruleForm = Object.assign({}, row.config);
	if (state.ruleForm.configId) {
		dbChanged().then(async () => {
			if (state.ruleForm.tableName) {
				state.columnList = await store.columnListBy(state.ruleForm.configId, state.ruleForm.tableName);
			}
		});
	}
};

// 关闭弹窗
const closeDialog = () => {
	if (!rowData.netType.startsWith('string')) state.ruleForm.multiple = false;
	rowData.config = Object.assign({}, state.ruleForm);
	emits('submitData', rowData);
	cancel();
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
	ruleFormRef.value?.resetFields();
	state.ruleForm = {};
};

// 提交
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>

<style scoped>
.ltc-form {
	padding-top: 4px;
}
.ltc-form :deep(.el-form-item) {
	margin-bottom: 18px;
}
.ltc-quick {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
}
.ltc-quick-tip {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.ltc-tip {
	display: flex;
	align-items: flex-start;
	gap: 8px;
	margin-bottom: 14px;
	padding: 9px 12px;
	border-radius: 8px;
	font-size: 12px;
	line-height: 1.7;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
}
.ltc-tip-ico {
	flex: none;
	margin-top: 3px;
	font-size: 14px;
	color: var(--el-color-primary);
}
.ltc-preview {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
	padding: 10px 14px;
	border-radius: 8px;
	background: color-mix(in srgb, var(--el-color-primary) 7%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 22%, transparent);
}
.ltc-preview-title {
	flex: none;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-color-primary);
}
.ltc-preview-txt {
	font-size: 12.5px;
	line-height: 1.7;
	color: var(--el-text-color-regular);
	b {
		color: var(--el-color-primary);
	}
}
</style>

<style lang="scss">
/* 弹窗头部：主题色底 + 白字，这里只接管内边距 */
.ltc-dialog .el-dialog__header {
	padding: 0;
}
.ltc-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 14px 18px;
}
.ltc-hd-ico {
	flex: none;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	background: color-mix(in srgb, currentColor 18%, transparent);
}
.ltc-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.ltc-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.ltc-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.ltc-q {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 14px;
	height: 14px;
	margin-left: 4px;
	border-radius: 50%;
	font-size: 11px;
	line-height: 1;
	color: #fff;
	background: var(--el-text-color-secondary);
	cursor: help;
	vertical-align: middle;
}
</style>
