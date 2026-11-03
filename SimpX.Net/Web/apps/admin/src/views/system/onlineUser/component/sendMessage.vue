<template>
	<div class="sendMessage-container">
		<!-- 小屏按视口比例收窄：写死 900px 在手机上会横向溢出屏幕 -->
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" :width="dialogWidth" append-to-body>
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="'标题'" prop="title" :rules="[{ required: true, message: '标题不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.title" :placeholder="'标题'" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="'类型'">
							<g-sys-dict v-model="state.ruleForm.messageType" code="MessageTypeEnum" render-as="radio" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="'收件人'">
							<el-input v-model="state.ruleForm.receiveUserName" :placeholder="'收件人'" disabled />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="'IP地址'">
							<el-input v-model="state.ruleForm.receiveUserId" :placeholder="'IP地址'" disabled />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="'内容'" prop="message" :rules="[{ required: true, message: '内容不能为空', trigger: 'blur' }]">
							<Editor v-model:get-html="state.ruleForm.message" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button type="primary" icon="ele-Position" @click="submit"> {{ '发送' }} </el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sendMessage">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useUserInfo } from '/@/stores/userInfo';
import { storeToRefs } from 'pinia';
import Editor from '/@/components/editor/index.vue';
import { sendSignalR } from '../signalR';

const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);
const props = defineProps({
	title: String,
});
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as any,
});

/** 弹窗宽度：手机按 94vw，桌面保持 900px */
const viewportWidth = ref(window.innerWidth);
const onResize = () => (viewportWidth.value = window.innerWidth);
onMounted(() => window.addEventListener('resize', onResize));
onUnmounted(() => window.removeEventListener('resize', onResize));
const dialogWidth = computed(() => (viewportWidth.value < 768 ? '94vw' : '900px'));

// 打开弹窗
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();

	var receiveUser = JSON.parse(JSON.stringify(row));
	state.ruleForm.receiveUserIds = [receiveUser.userId];
	state.ruleForm.receiveUserName = receiveUser.realName + '/' + receiveUser.userName;
	state.ruleForm.receiveUserId = receiveUser.ip;
	state.ruleForm.connectionId = receiveUser.connectionId;
	state.ruleForm.sendUserId = userInfos.value.id;
	state.isShowDialog = true;
};

// 发送
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		// 发送消息给某人（对应后台Hub的方法名称）
		try {
			await sendSignalR('SendMessageToUser', state.ruleForm);
			ElMessage.success('操作成功');
		} catch (err: any) {
			ElMessage.error(err?.message ?? '发送失败');
		}
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
