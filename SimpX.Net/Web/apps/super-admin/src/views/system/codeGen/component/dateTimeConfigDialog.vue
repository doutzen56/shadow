<template>
	<div class="sys-codeGenDateTime-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="560px" class="dtc-dialog">
			<template #header>
				<div class="dtc-hd">
					<span class="dtc-hd-ico"><el-icon><ele-Clock /></el-icon></span>
					<div class="dtc-hd-txt">
						<span class="dtc-hd-title">时间数据配置</span>
						<span class="dtc-hd-sub">决定这个日期字段在列表、表单和查询区里精确到什么程度</span>
					</div>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" class="dtc-form">
				<el-form-item prop="format" :rules="[{ required: true, message: '时间格式不能为空', trigger: 'blur' }]">
					<template #label>
						<span>时间格式 <Tip text="只影响页面展示与查询，数据库仍按完整时间存储" /></span>
					</template>
					<el-select v-model="state.ruleForm.format" filterable clearable placeholder="请选择时间格式" class="w100">
						<el-option v-for="item in formatOptions" :key="item.value" :label="item.label" :value="item.value">
							<span>{{ item.label }}</span>
							<span class="dtc-opt-eg">{{ item.eg }}</span>
						</el-option>
					</el-select>
				</el-form-item>
			</el-form>
			<div class="dtc-preview">
				<span class="dtc-preview-title">生成效果</span>
				<span class="dtc-preview-txt">列表与表单中展示为 <b>{{ currentEg }}</b></span>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit" v-reclick="1000">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysCodeGenDateTime">
import { computed, reactive, ref, h } from 'vue';
import { ElTooltip } from 'element-plus';

// 表单标签后的问号提示
const Tip = (props: { text: string }) => h(ElTooltip, { content: props.text, placement: 'top' }, { default: () => h('span', { class: 'dtc-q' }, '?') });

const formatOptions = [
	{ value: 'datetime', label: '年-月-日 时:分:秒', eg: '2026-10-23 17:34:00' },
	{ value: 'date', label: '年-月-日', eg: '2026-10-23' },
	{ value: 'time', label: '时:分:秒', eg: '17:34:00' },
];

let rowData = {} as any;
const emits = defineEmits(['submitData']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as any,
});

// 当前格式对应的示例值
const currentEg = computed(() => formatOptions.find((u) => u.value === state.ruleForm.format)?.eg ?? '未选择');

// 打开弹窗
const openDialog = async (row: any) => {
	rowData = row;
	state.isShowDialog = true;
	state.ruleForm = Object.assign({}, row.config);
	state.ruleForm.format ??= 'datetime';
};

// 关闭弹窗
const closeDialog = () => {
	rowData.config = Object.assign({}, state.ruleForm);
	emits('submitData', rowData);
	cancel();
};

// 取消
const cancel = () => {
	ruleFormRef.value?.resetFields();
	state.isShowDialog = false;
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
.dtc-form {
	padding-top: 4px;
}
.dtc-opt-eg {
	float: right;
	margin-left: 16px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.dtc-preview {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
	padding: 10px 14px;
	border-radius: 8px;
	background: color-mix(in srgb, var(--el-color-primary) 7%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 22%, transparent);
}
.dtc-preview-title {
	flex: none;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-color-primary);
}
.dtc-preview-txt {
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
.dtc-dialog .el-dialog__header {
	padding: 0;
}
.dtc-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 14px 18px;
}
.dtc-hd-ico {
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
.dtc-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.dtc-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.dtc-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.dtc-q {
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
