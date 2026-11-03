<template>
	<div class="sys-codeGenUpload-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="620px" class="uc-dialog">
			<template #header>
				<div class="uc-hd">
					<span class="uc-hd-ico"><el-icon><ele-Upload /></el-icon></span>
					<div class="uc-hd-txt">
						<span class="uc-hd-title">上传配置</span>
						<span class="uc-hd-sub">决定生成页面里这个上传字段的展示方式：是显示文件链接，还是直接显示图片缩略图</span>
					</div>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" class="uc-form">
				<el-form-item prop="useDownload" :rules="[{ required: true, message: '链接下载不能为空', trigger: 'blur' }]">
					<template #label>
						<span>链接预览 <Tip text="开启后列表与表单里显示一个可点击的链接，而不是原始文件名" /></span>
					</template>
					<el-radio-group v-model="state.ruleForm.useDownload">
						<el-radio :value="true">是</el-radio>
						<el-radio :value="false">否</el-radio>
					</el-radio-group>
				</el-form-item>
				<el-form-item v-if="state.ruleForm.useDownload" prop="downloadText" :rules="[{ required: true, message: '链接文本不能为空', trigger: 'blur' }]">
					<template #label>
						<span>链接文本 <Tip text="链接上显示的文字，如「查看文件」「下载附件」" /></span>
					</template>
					<el-input v-model="state.ruleForm.downloadText" placeholder="如：查看文件" class="w100" clearable />
				</el-form-item>
				<el-form-item prop="isImage" :rules="[{ required: true, message: '图片文件不能为空', trigger: 'blur' }]">
					<template #label>
						<span>图片文件 <Tip text="开启后页面直接渲染图片缩略图，支持点击放大；关闭则按普通附件处理" /></span>
					</template>
					<el-radio-group v-model="state.ruleForm.isImage">
						<el-radio :value="true">是</el-radio>
						<el-radio :value="false">否</el-radio>
					</el-radio-group>
				</el-form-item>
			</el-form>
			<div class="uc-preview">
				<span class="uc-preview-title">生成效果</span>
				<span class="uc-preview-txt">
					页面将展示
					<b>{{ state.ruleForm.isImage ? '图片缩略图' : '文件名' }}</b>
					<template v-if="state.ruleForm.useDownload">，并附带「<b>{{ state.ruleForm.downloadText || '查看文件' }}</b>」链接</template>
				</span>
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

<script lang="ts" setup name="sysCodeGenUpload">
import { reactive, ref, h } from 'vue';
import { ElTooltip } from 'element-plus';

// 表单标签后的问号提示
const Tip = (props: { text: string }) => h(ElTooltip, { content: props.text, placement: 'top' }, { default: () => h('span', { class: 'uc-q' }, '?') });

let rowData = {} as any;
const emits = defineEmits(['submitData']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as any,
});

// 打开弹窗
const openDialog = async (row: any) => {
	rowData = row;
	state.ruleForm = Object.assign({}, row.config);
	state.ruleForm.downloadText ??= '查看文件';
	state.ruleForm.useDownload ??= false;
	state.ruleForm.isImage ??= false;
	state.isShowDialog = true;
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
.uc-form {
	padding-top: 4px;
}
.uc-preview {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
	padding: 10px 14px;
	border-radius: 8px;
	background: color-mix(in srgb, var(--el-color-primary) 7%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 22%, transparent);
}
.uc-preview-title {
	flex: none;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-color-primary);
}
.uc-preview-txt {
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
.uc-dialog .el-dialog__header {
	padding: 0;
}
.uc-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 14px 18px;
}
.uc-hd-ico {
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
.uc-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.uc-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.uc-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.uc-q {
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
