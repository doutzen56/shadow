<template>
	<div class="sys-print-container">
		<wormPrint ref="wormPrintRef" :template="editTemplate" :title="props.title" @save="onWormSave" />

		<el-dialog v-model="state.showDialog2" draggable overflow destroy-on-close width="600px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="模板名称" prop="name" :rules="[{ required: true, message: '模板名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="模板名称" clearable />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="排序">
							<el-input-number v-model="state.ruleForm.orderNo" placeholder="排序" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="状态">
							<el-radio-group v-model="state.ruleForm.status">
								<el-radio :value="1">启用</el-radio>
								<el-radio :value="2">禁用</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="打印类型">
							<el-radio-group v-model="state.ruleForm.printType">
								<el-radio :value="1">浏览器打印</el-radio>
								<el-radio :value="2">客户端打印</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="客户端服务地址">
							<el-input v-model="state.ruleForm.clientServiceAddress" placeholder="客户端服务地址" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="打印参数">
							<el-input v-model="state.ruleForm.printParam" placeholder="请输入打印参数" clearable type="textarea" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="备注">
							<el-input v-model="state.ruleForm.remark" placeholder="请输入备注内容" clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="templateCancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="templateSubmit">确定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditPrint">
import { onMounted, reactive, ref } from 'vue';
import wormPrint from './wormPrint.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysPrintApi } from '/@/api-services/system/api';
import { UpdatePrintInput } from '/@/api-services/system/models';


const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const wormPrintRef = ref<InstanceType<typeof wormPrint>>();

const state = reactive({
	ruleForm: {} as UpdatePrintInput,
	showDialog2: false,
});

// 页面初始化
onMounted(async () => {});

// 编辑时需要传给 wormPrint 加载的已有模板（编辑场景；新增时为空）
const editTemplate = ref<string>('')

// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	editTemplate.value = (state.ruleForm.template as string) ?? '';
	// 兜底：把已有模板也同步进保存缓存，避免「未点设计器保存、直接点确定」时把模板清空
	wormTemplateJson.value = editTemplate.value;

	// 打开 wormPrint 自身弹窗（设计器），直接把当前模板传给它，避免 props 刷新时序问题
	wormPrintRef.value?.open({ template: editTemplate.value });
	ruleFormRef.value?.resetFields();

};


// 取消：关闭 wormPrint 设计器弹窗
const cancel = () => {
	wormPrintRef.value?.close();
};

// ============ wormPrint 设计器相关 ============
// 缓存设计器「保存」时带回的模板 JSON 字符串
const wormTemplateJson = ref<string>('')

// 提交：打开第二层弹窗，填写模板名称 / 排序 / 状态等元信息
const submit = async () => {
	state.showDialog2 = true;
	if (state.ruleForm.orderNo == undefined) state.ruleForm.orderNo = 100;
	if (state.ruleForm.status == undefined) state.ruleForm.status = 1;
	if (state.ruleForm.printType == undefined) state.ruleForm.printType = 1;
};

// wormPrint 设计器内点「保存」时触发：缓存模板，并进入「填写模板元信息」步骤
const onWormSave = (payload?: { template?: string }) => {
	if (payload?.template) wormTemplateJson.value = payload.template;
	submit();
};

// 模板设置取消
const templateCancel = () => {
	state.showDialog2 = false;
};

// 模板设置提交
const templateSubmit = async () => {
	state.ruleForm.template = wormTemplateJson.value;
	// state.ruleForm.printDataDemo = JSON.stringify(mockPrintData);
	if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
		await getAPI(SysPrintApi).apiSysPrintUpdatePost(state.ruleForm);
	} else {
		await getAPI(SysPrintApi).apiSysPrintAddPost(state.ruleForm);
	}
	cancel();
	templateCancel();
	emits('handleQuery');
};

// 父组件 ref 调用：关闭设计器弹窗与元信息弹窗
const close = () => {
	wormPrintRef.value?.close();
	templateCancel();
};

// 导出对象：父组件通过 ref 控制显示与隐藏
defineExpose({ openDialog, close });
</script>

<style lang="scss" scoped></style>
