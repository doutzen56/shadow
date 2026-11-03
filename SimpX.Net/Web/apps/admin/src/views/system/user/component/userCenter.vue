<template>
	<div class="uc-page">
		<el-row :gutter="14">
			<!-- 左：身份卡（顶部 banner 即个人头图，页面级 Hero 不套用在本页） -->
			<el-col :xs="24" :sm="24" :md="8" :lg="8" :xl="8">
				<el-card shadow="hover" class="uc-identity">
					<div class="uc-cover" ref="bannerRef">
						<div class="wave wave-1"></div>
					</div>

					<div class="uc-id-head">
						<div class="uc-avatar-zone">
							<el-avatar
								:size="100"
								:src="userInfos.avatar"
								@click="openCropperDialog"
								v-loading="state.avatarLoading"
								element-loading-spinner="el-icon-Upload"
								element-loading-background="rgba(0, 0, 0, 0.2)"
								@mouseenter="mouseEnterAvatar"
								@mouseleave="mouseLeaveAvatar"
								class="uc-avatar"
							/>
							<span class="uc-avatar-hint"><el-icon><ele-Camera /></el-icon></span>
						</div>
						<div class="uc-id-name">{{ userInfos.realName || '未填写姓名' }}</div>
						<div class="uc-id-account">@{{ userInfos.account }}</div>
						<div class="uc-id-roles">
							<el-tag v-for="r in userInfos.roles ?? []" :key="r.id ?? r.name" type="danger" effect="light" round size="small">{{ r.name }}</el-tag>
							<el-tag v-if="!(userInfos.roles ?? []).length" type="info" effect="light" round size="small">未分配角色</el-tag>
						</div>
					</div>

					<!-- 关键档案：图标 + 名称 + 值，悬停轻微高亮 -->
					<div class="uc-facts">
						<div class="uc-fact">
							<span class="uc-fact-ico"><el-icon><ele-School /></el-icon></span>
							<span class="uc-fact-k">所属机构</span>
							<span class="uc-fact-v">{{ userInfos.orgName ?? '—' }}</span>
						</div>
						<div class="uc-fact">
							<span class="uc-fact-ico is-purple"><el-icon><ele-Postcard /></el-icon></span>
							<span class="uc-fact-k">职位</span>
							<span class="uc-fact-v">{{ userInfos.posName ?? '—' }}</span>
						</div>
						<div class="uc-fact">
							<span class="uc-fact-ico is-amber"><el-icon><ele-Medal /></el-icon></span>
							<span class="uc-fact-k">工号</span>
							<span class="uc-fact-v">{{ userInfos.jobNum ?? '—' }}</span>
						</div>
						<div class="uc-fact">
							<span class="uc-fact-ico is-cyan"><el-icon><ele-Message /></el-icon></span>
							<span class="uc-fact-k">邮箱</span>
							<span class="uc-fact-v">{{ userInfos.email ?? '—' }}</span>
						</div>
						<div class="uc-fact">
							<span class="uc-fact-ico is-green"><el-icon><ele-Coin /></el-icon></span>
							<span class="uc-fact-k">积分</span>
							<span class="uc-fact-v is-point">{{ userInfos.points ?? 0 }}</span>
						</div>
						<div class="uc-fact">
							<span class="uc-fact-ico"><el-icon><ele-Calendar /></el-icon></span>
							<span class="uc-fact-k">创建时间</span>
							<span class="uc-fact-v">{{ userInfos.createTime ?? '—' }}</span>
						</div>
					</div>

					<div class="uc-block">
						<div class="uc-block-title">个人标签</div>
						<div class="uc-tags">
							<el-tag round effect="plain" size="small">{{ userInfos.orgName ?? '机构' }}</el-tag>
							<el-tag round effect="plain" size="small">{{ userInfos.posName ?? '职位' }}</el-tag>
							<el-tag round effect="plain" type="warning" size="small">{{ userInfos.jobNum ?? '工号' }}</el-tag>
							<el-tag round effect="plain" size="small">{{ userInfos.email ?? '邮箱' }}</el-tag>
							<el-tag round effect="plain" type="warning" size="small">{{ userInfos.phone ?? '电话' }}</el-tag>
							<el-tag round effect="plain" size="small">{{ userInfos.idCardNum ?? '证件号码' }}</el-tag>
							<el-tag round effect="plain" type="info" size="small">{{ userInfos.address ?? '地址' }}</el-tag>
						</div>
					</div>

					<div class="uc-block uc-block-last">
						<div class="uc-block-title">电子签名</div>
						<div class="uc-sign-box">
							<el-image v-if="userInfos.signature" :src="resolveDocumentBrowserFileUrl(userInfos.signature)" fit="contain" :alt="'电子签名'" loading="lazy" />
							<div v-else class="uc-sign-empty">
								<el-icon><ele-EditPen /></el-icon>
								<span>暂无电子签名</span>
							</div>
						</div>
						<div class="uc-sign-acts">
							<el-button icon="ele-Edit" type="primary" @click="openSignDialog" v-auth="'sysFile/uploadSignature'">手写签名</el-button>
							<el-upload
								ref="uploadSignRef"
								action
								accept=".png"
								:limit="1"
								:show-file-list="false"
								:auto-upload="false"
								:on-change="uploadSignFile"
								:on-exceed="uploadSignFileExceed"
								class="uc-sign-upload"
							>
								<el-button icon="ele-UploadFilled" v-auth="'sysFile/uploadSignature'">上传签名</el-button>
							</el-upload>
						</div>
					</div>
				</el-card>
			</el-col>

			<!-- 右：资料 / 组织 / 密码 / 登录日志 -->
			<el-col :xs="24" :sm="24" :md="16" :lg="16" :xl="16">
				<el-card shadow="hover" class="uc-main">
					<el-tabs class="uc-tabs">
						<el-tab-pane :label="'基础信息'" v-loading="state.loading">
							<el-form :model="state.ruleFormBase" ref="ruleFormBaseRef" label-width="auto" label-position="top" class="uc-form">
								<el-row :gutter="16">
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
										<el-form-item :label="'真实姓名'" prop="realName" :rules="[{ required: true, message: '真实姓名不能为空', trigger: 'blur' }]">
											<el-input v-model="state.ruleFormBase.realName" :placeholder="'真实姓名'" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
										<el-form-item :label="'昵称'">
											<el-input v-model="state.ruleFormBase.nickName" :placeholder="'昵称'" clearable />
										</el-form-item>
									</el-col>

									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
										<el-form-item :label="'手机号码'" prop="phone" :rules="[{ required: true, message: '手机号码不能为空', trigger: 'blur' }]">
											<el-input v-model="state.ruleFormBase.phone" :placeholder="'手机号码'" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
										<el-form-item :label="'邮箱'">
											<el-input v-model="state.ruleFormBase.email" :placeholder="'邮箱'" clearable />
										</el-form-item>
									</el-col>

									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
										<el-form-item :label="'出生日期'" prop="birthday" :rules="[{ required: true, message: '出生日期不能为空', trigger: 'blur' }]">
											<el-date-picker v-model="state.ruleFormBase.birthday" type="date" :placeholder="'出生日期'" format="YYYY-MM-DD" value-format="YYYY-MM-DD" class="w100" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
										<el-form-item :label="'性别'">
											<g-sys-dict v-model="state.ruleFormBase.sex" code="GenderEnum" renderAs="radio" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
										<el-form-item :label="'工号'">
											<el-input v-model="state.ruleFormBase.jobNum" :placeholder="'工号'" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item :label="'地址'">
											<el-input v-model="state.ruleFormBase.address" :placeholder="'地址'" clearable type="textarea" maxlength="256" show-word-limit />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item :label="'备注'">
											<el-input v-model="state.ruleFormBase.remark" :placeholder="'备注'" clearable type="textarea" maxlength="256" show-word-limit />
										</el-form-item>
									</el-col>

									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item class="uc-form-acts">
											<el-button icon="ele-SuccessFilled" type="primary" @click="submitUserBase" v-auth="'sysUser/baseInfo'">保存基本信息</el-button>
										</el-form-item>
									</el-col>
								</el-row>
							</el-form>
						</el-tab-pane>
						<el-tab-pane :label="'组织机构'">
							<OrgTree ref="orgTreeRef" :isAllOrg="true" />
						</el-tab-pane>
						<el-tab-pane :label="'修改密码'">
							<el-alert type="warning" show-icon :closable="false" class="uc-pwd-tip">
								<span>{{ '密码安全策略：必须包含大小写字母、数字和特殊字符的组合，长度在6-16之间。建议定期更新密码。' }}</span>
							</el-alert>
							<el-form ref="ruleFormPasswordRef" :model="state.ruleFormPassword" label-width="auto" label-position="top" class="uc-form uc-form-pwd">
								<el-row :gutter="16">
									<!-- 只有 3 个字段，改上下布局：一行一个，整体限宽不撑满卡片 -->
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item :label="'当前密码'" prop="passwordOld" :rules="[{ required: true, message: '当前密码不能为空', trigger: 'blur' }]">
											<el-input v-model="state.ruleFormPassword.passwordOld" type="password" autocomplete="off" show-password />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item :label="'新密码'" prop="passwordNew" :rules="[{ required: true, message: '新密码不能为空', trigger: 'blur' }]">
											<el-input v-model="state.ruleFormPassword.passwordNew" type="password" autocomplete="off" show-password />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
										<el-form-item :label="'确认密码'" prop="passwordNew2" :rules="[{ validator: validatePassword, required: true, trigger: 'blur' }]">
											<el-input v-model="state.passwordNew2" type="password" autocomplete="off" show-password />
										</el-form-item>
									</el-col>
								</el-row>
								<el-form-item class="uc-form-acts">
									<el-button icon="ele-Refresh" @click="resetPassword">重置</el-button>
									<el-button icon="ele-CircleCheckFilled" type="primary" @click="submitPassword" v-auth="'sysUser/changePwd'">确定</el-button>
								</el-form-item>
							</el-form>
						</el-tab-pane>
						<el-tab-pane :label="'登录日志'">
							<div class="uc-logs">
								<article class="uc-log" v-for="(item, index) in state.logVisData" :key="index">
									<span class="uc-log-ico"><el-icon><ele-Monitor /></el-icon></span>
									<div class="uc-log-main">
										<div class="uc-log-head">
											<h3 class="uc-log-title">{{ item.os }}</h3>
											<el-tag size="small" round effect="light" type="danger">耗时 {{ item.elapsed }}ms</el-tag>
											<span class="uc-log-time">{{ item.logDateTime }}</span>
										</div>
										<div class="uc-log-meta">
											<span><i>浏览器</i>{{ item.browser }}</span>
											<span><i>IP 地址</i>{{ item.remoteIp }}</span>
											<span><i>登录地点</i>{{ item.location }}</span>
											<span><i>经纬度</i>{{ item.longitude }} / {{ item.latitude }}</span>
											<span><i>账号</i>{{ item.account }} / {{ item.realName }}</span>
										</div>
									</div>
								</article>
								<el-empty v-if="!state.logVisData.length" description="暂无登录日志" :image-size="110" />
							</div>
						</el-tab-pane>
					</el-tabs>
				</el-card>
			</el-col>
		</el-row>

		<el-dialog v-model="state.signDialogVisible" draggable :close-on-click-modal="false" width="600px">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"> <ele-EditPen /> </el-icon>
					<span>电子签名</span>
				</div>
			</template>
			<div class="uc-pad">
				<VueSignaturePad ref="signaturePadRef" :options="state.signOptions" />
			</div>
			<div class="uc-pad-acts">
				<div class="uc-pad-field">{{ '画笔粗细' }}：<el-input-number v-model="state.signOptions.minWidth" :min="0.5" :max="2.5" :step="0.1" size="small" /></div>
				<div class="uc-pad-field">{{ '画笔颜色' }}：<el-color-picker v-model="state.signOptions.penColor" color-format="hex" size="default" /></div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-RefreshLeft" @click="unDoSign">{{ '撤销' }}</el-button>
					<el-button icon="ele-CircleClose" @click="clearSign">{{ '清屏' }}</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="saveUploadSign">{{ '保存' }}</el-button>
				</span>
			</template>
		</el-dialog>

		<CropperDialog ref="cropperDialogRef" :title="state.cropperTitle" @uploadCropperImg="uploadCropperImg" />
	</div>
</template>

<script lang="ts" setup name="sysUserCenter">
import { onMounted, watch, reactive, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { ElForm, ElMessage, ElMessageBox, genFileId } from 'element-plus';
import type { UploadInstance, UploadProps, UploadRawFile } from 'element-plus';
import { useUserInfo } from '/@/stores/userInfo';
import { resolveDocumentBrowserFileUrl } from '/@/utils/documentFileUrl';
import { base64ToFile, blobToFile } from '/@/utils/base64Conver';
import VueGridLayout from 'vue-grid-layout';
import { sm2 } from 'sm-crypto-v2';

import CropperDialog from '/@/components/cropper/index.vue';
import OrgTree from '/@/views/system/user/component/orgTree.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysConfigApi, SysFileApi, SysLogVisApi, SysUserApi } from '/@/api-services/system/api';
import { ChangePwdInput, SysUser, SysLogVis } from '/@/api-services/system/models';

const bannerRef = ref<HTMLDivElement>();
const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);
const uploadSignRef = ref<UploadInstance>();
const signaturePadRef = ref<InstanceType<typeof VueGridLayout>>();
const ruleFormBaseRef = ref<InstanceType<typeof ElForm>>();
const ruleFormPasswordRef = ref<InstanceType<typeof ElForm>>();
const cropperDialogRef = ref<InstanceType<typeof CropperDialog>>();
const state = reactive({
	loading: false,
	avatarLoading: false,
	signDialogVisible: false,
	ruleFormBase: {} as SysUser,
	ruleFormPassword: {} as ChangePwdInput,
	signOptions: {
		penColor: '#000000',
		minWidth: 1.0,
		onBegin: () => {
			signaturePadRef.value.resizeCanvas();
		},
	},
	signFileList: [] as any,
	passwordNew2: '',
	cropperTitle: '',
	logVisData: [] as SysLogVis[],
});

// 页面初始化
onMounted(async () => {
	state.loading = true;
	var res = await getAPI(SysUserApi).apiSysUserBaseInfoGet();
	state.ruleFormBase = res.data.result ?? { account: '' };
	state.loading = false;

	// 获取登录日志
	await getUserLogVisList();

	// 生成气泡
	drawBubble();
});

// 随机生成气泡
const drawBubble = () => {
	const el = bannerRef.value;
	if (!el) return;

	// 容器高度，用于计算气泡上浮的总距离
	const H = el.clientHeight;
	// 气泡数量（越多越热闹，但性能开销也越大）
	const COUNT = 10;
	for (let i = 0; i < COUNT; i++) {
		// 每个气泡是一个 <span class="bubble">
		const b = document.createElement('span');
		b.className = 'bubble';

		// 气泡大小：8 ~ 24px（随机，大小不一更自然）
		const size = 8 + Math.random() * 16;
		// 上浮总距离：容器高度 + 20px，再随机减 0~20px
		// 让每个气泡到达的高度略有差异，避免整齐划一
		const travel = H + 20 - Math.random() * 20;

		// 一次性写入所有内联样式
		//  - left：水平位置随机（0~92%）
		//  - width / height：气泡尺寸
		//  - --travel：CSS 变量，供 @keyframes 里计算每段位移
		//  - animation-duration：6~10s，每个气泡速度不同
		//  - animation-delay：0~6s，错开出场时间
		b.style.cssText = `
    		left: ${Math.random() * 92}%;
    		width: ${size}px;
    		height: ${size}px;
    		--travel: ${travel}px;
    		animation-duration: ${6 + Math.random() * 4}s;
    		animation-delay: ${Math.random() * 6}s;
  		`;
		// 把气泡挂到 banner 容器里
		el.appendChild(b);
	}
};

// 签名监听
watch(state.signOptions, () => {
	signaturePadRef.value.signaturePad.penColor = state.signOptions.penColor;
	signaturePadRef.value.signaturePad.minWidth = state.signOptions.minWidth;
});

// 上传头像图片
const uploadCropperImg = async (e: any) => {
	var res = await getAPI(SysFileApi).apiSysFileUploadAvatarPostForm(blobToFile(e.img, userInfos.value.account + '.png'));
	userInfos.value.avatar = res.data.result!.url!;
	state.ruleFormBase.avatar = userInfos.value.avatar;
};

// 打开电子签名页面
const openSignDialog = () => {
	state.signDialogVisible = true;
};

// 保存并上传电子签名
const saveUploadSign = async () => {
	const { isEmpty, data } = signaturePadRef.value.saveSignature();
	if (isEmpty) {
		userInfos.value.signature = null;
		state.ruleFormBase.signature = null;
	} else {
		var res = await getAPI(SysFileApi).apiSysFileUploadSignaturePostForm(base64ToFile(data, userInfos.value.account + '.png'));
		userInfos.value.signature = res.data.result!.url!;
		state.ruleFormBase.signature = userInfos.value.signature;
	}
	clearSign();
	state.signDialogVisible = false;
};

// 撤销电子签名
const unDoSign = () => {
	signaturePadRef.value.undoSignature();
};

// 清空电子签名
const clearSign = () => {
	signaturePadRef.value.clearSignature();
};

// 上传手写电子签名
const uploadSignFile = async (file: any) => {
	var res = await getAPI(SysFileApi).apiSysFileUploadSignaturePostForm(file.raw);
	userInfos.value.signature = res.data.result?.url;
	state.ruleFormBase.signature = userInfos.value.signature;
};

// 获得电子签名文件列表
const handleChangeSignFile = (_file: any, fileList: []) => {
	state.signFileList = fileList;
};

// 修改个人信息
const submitUserBase = () => {
	ruleFormBaseRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		ElMessageBox.confirm('确定修改个人基础信息？', '提示', {
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			type: 'warning',
		}).then(async () => {
			await getAPI(SysUserApi).apiSysUserUpdateBaseInfoPost(state.ruleFormBase);
			ElMessage.success('保存成功');
		});
	});
};

// 密码验证
const validatePassword = (_rule: any, value: any, callback: any) => {
	if (state.passwordNew2 != state.ruleFormPassword.passwordNew) {
		callback(new Error('两次密码不一致！'));
	} else {
		callback();
	}
};

// 密码重置
const resetPassword = () => {
	state.ruleFormPassword.passwordOld = '';
	state.ruleFormPassword.passwordNew = '';
	state.passwordNew2 = '';
};

// 密码提交
const submitPassword = () => {
	ruleFormPasswordRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;

		// SM2加密密码
		var cpwd: ChangePwdInput = { passwordOld: '', passwordNew: '' };
		var publicKey = window.__env__.VITE_SM_PUBLIC_KEY;
		if (publicKey == '') {
			var res = await getAPI(SysConfigApi).apiSysConfigSmPublicKeyGet();
			publicKey = window.__env__.VITE_SM_PUBLIC_KEY = res.data.result ?? '';
		}
		cpwd.passwordOld = sm2.doEncrypt(state.ruleFormPassword.passwordOld, publicKey, 1);
		cpwd.passwordNew = sm2.doEncrypt(state.ruleFormPassword.passwordNew, publicKey, 1);
		await getAPI(SysUserApi).apiSysUserChangePwdPost(cpwd);

		// 修改密码后后端会强制下线，无需弹出确认重新登录提示
		// // 退出系统
		// ElMessageBox.confirm('密码已修改，是否重新登录系统？', '提示', {
		// 	confirmButtonText: '确定',
		// 	cancelButtonText: '取消',
		// 	type: 'warning',
		// }).then(async () => {
		// 	clearAccessTokens();
		// });
	});
};

// 打开裁剪弹窗
const openCropperDialog = () => {
	state.cropperTitle = '更换头像';
	cropperDialogRef.value?.openDialog(userInfos.value.avatar);
};

// 鼠标进入和离开头像时
const mouseEnterAvatar = () => {
	state.avatarLoading = true;
};

const mouseLeaveAvatar = () => {
	state.avatarLoading = false;
};

// 上传签名超出数量限制时执行
const uploadSignFileExceed: UploadProps['onExceed'] = (files) => {
	uploadSignRef.value!.clearFiles();
	const file = files[0] as UploadRawFile;
	file.uid = genFileId();
	uploadSignRef.value!.handleStart(file);
};

// 获取登录日志
const getUserLogVisList = async () => {
	var res = await getAPI(SysLogVisApi).apiSysLogVisUserLogVisListGet();
	state.logVisData = res.data.result ?? ([] as SysLogVis[]);
};

// 导出对象
defineExpose({ handleChangeSignFile });
</script>

<style lang="scss" scoped>
/* 本页身份卡（封面 / 气泡 / 头像压边 / 姓名区 / 档案行 / 标签）的样式**自成一份**，
   与首页「我的资料」部件互不引用：两处卡片尺寸差一档，抽公共文件会导致互相牵制。
   注意：气泡是 JS 运行时 appendChild 的节点，没有 scoped 的 data-v，必须走 :deep()。 */

/* 本页是路由页：上/右/左页边距来自 .layout-parent 的 padding:5px，这里只补底部 */
.uc-page {
	position: relative;
	padding-bottom: 4px;
}

.uc-identity,
.uc-main {
	border-radius: 14px;
	border: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
}

.uc-identity {
	:deep(.el-card__body) {
		padding: 0;
	}
}

.uc-main {
	:deep(.el-card__body) {
		padding: 0 18px 18px;
	}
}

/* ============ 顶部封面（渐变 + 水波 + 气泡） ============ */
.uc-cover {
	position: relative;
	height: 180px;
	overflow: hidden;
	border-radius: 14px 14px 0 0;
	background:
		radial-gradient(ellipse at 50% 0%, var(--el-color-primary-light-7) 0%, transparent 40%),
		linear-gradient(to top, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 40%, var(--el-color-primary-light-5) 75%, var(--el-color-primary-light-7) 100%);
}

/* 气泡本体：玻璃球质感（左上高光 + 透明中心 + 边缘亮环） */
.uc-cover :deep(.bubble) {
	position: absolute;
	bottom: -20px;
	border-radius: 50%;
	background: radial-gradient(
		circle at 30% 30%,
		rgba(255, 255, 255, 0.9) 0%,
		rgba(255, 255, 255, 0.25) 8%,
		rgba(255, 255, 255, 0.05) 35%,
		rgba(255, 255, 255, 0.18) 70%,
		rgba(255, 255, 255, 0.55) 90%,
		rgba(255, 255, 255, 0.9) 100%
	);
	box-shadow:
		0 0 6px rgba(255, 255, 255, 0.35),
		inset 0 0 4px rgba(255, 255, 255, 0.6),
		inset 0 0 2px rgba(0, 0, 0, 0.08);
	border: 1px solid rgba(255, 255, 255, 0.45);
	will-change: transform, opacity;
	transform: translate3d(0, 0, 0);
	backface-visibility: hidden;
	animation: bubbleRise linear infinite;
	pointer-events: none;
}

.uc-cover :deep(.bubble)::before {
	content: '';
	position: absolute;
	top: 15%;
	left: 18%;
	width: 30%;
	height: 30%;
	border-radius: 50%;
	background: radial-gradient(circle at center, rgba(255, 255, 255, 1) 0%, rgba(255, 255, 255, 0.6) 40%, transparent 70%);
	pointer-events: none;
}

.uc-cover :deep(.bubble)::after {
	content: '';
	position: absolute;
	bottom: 18%;
	right: 20%;
	width: 18%;
	height: 18%;
	border-radius: 50%;
	background: radial-gradient(circle at center, rgba(255, 255, 255, 0.8) 0%, transparent 70%);
	pointer-events: none;
}

@keyframes bubbleRise {
	0% {
		transform: translate3d(0, 0, 0) scale(0.7);
		opacity: 0;
	}
	10% {
		opacity: 0.95;
	}
	40% {
		transform: translate3d(10px, calc(var(--travel) * -0.4), 0) scale(0.95);
		opacity: 0.95;
	}
	70% {
		transform: translate3d(-8px, calc(var(--travel) * -0.7), 0) scale(1);
		opacity: 0.9;
	}
	92% {
		transform: translate3d(0, calc(var(--travel) * -0.95), 0) scale(1.5);
		opacity: 0;
	}
	100% {
		transform: translate3d(0, calc(var(--travel) * -1), 0) scale(1.5);
		opacity: 0;
	}
}

.wave {
	position: absolute;
	bottom: 0;
	left: 0;
	width: 200%;
	height: 200px;
	background-repeat: repeat-x;
	background-size: 50% 100%;
	opacity: 0.8;
}

.wave-1 {
	background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1440 320' preserveAspectRatio='none'%3E%3Cpath fill='%23ffffff' fill-opacity='0.15' d='M0,192 C240,256 480,128 720,192 C960,256 1200,128 1440,192 L1440,320 L0,320 Z'%3E%3C/path%3E%3C/svg%3E");
	background-size: cover;
	animation: waveMove 5s linear infinite;
	height: 220px;
}

@keyframes waveMove {
	0% {
		transform: translate(0);
	}
	to {
		transform: translate(-50%);
	}
}

/* ============ 身份区（头像压在封面下沿） ============ */
.uc-id-head {
	margin-top: -52px;
	padding: 0 18px;
	text-align: center;
}

.uc-avatar-zone {
	position: relative;
	display: inline-block;
}

.uc-avatar {
	border: 4px solid #fff;
	box-sizing: content-box;
	background: #fff;
	border-radius: 50%;
	box-shadow: 0 6px 16px -6px rgba(0, 0, 0, 0.3);
	cursor: pointer;
}

/* 悬停才出现的「可更换」提示：默认不占位、不抢视觉 */
.uc-avatar-hint {
	position: absolute;
	right: 2px;
	bottom: 2px;
	width: 28px;
	height: 28px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--el-color-white);
	background: var(--el-color-primary);
	border: 2px solid var(--el-color-white);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
	opacity: 0;
	transform: translateY(4px);
	transition:
		opacity 0.25s ease,
		transform 0.25s ease;
	pointer-events: none;
}

.uc-avatar-zone:hover .uc-avatar-hint {
	opacity: 1;
	transform: translateY(0);
}

.uc-id-name {
	margin-top: 12px;
	font-size: 20px;
	line-height: 1.3;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.uc-id-account {
	margin-top: 4px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}

.uc-id-roles {
	margin-top: 10px;
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
	justify-content: center;
}

/* ============ 关键档案：图标 + 名称 + 值 ============ */
.uc-facts {
	padding: 14px 12px 4px;
}

.uc-fact {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 8px 10px;
	border-radius: 10px;
	transition: background 0.22s ease;

	+ .uc-fact {
		margin-top: 2px;
	}

	&:hover {
		background: var(--el-fill-color-light);
	}
}

.uc-fact-ico {
	flex: 0 0 auto;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
	color: var(--el-color-primary);

	&.is-purple {
		background: color-mix(in srgb, var(--el-color-danger) 12%, transparent);
		color: var(--el-color-danger);
	}

	&.is-amber {
		background: color-mix(in srgb, var(--el-color-warning) 16%, transparent);
		color: var(--el-color-warning);
	}

	&.is-cyan {
		background: color-mix(in srgb, var(--el-color-info) 14%, transparent);
		color: var(--el-color-info);
	}

	&.is-green {
		background: color-mix(in srgb, var(--el-color-success) 14%, transparent);
		color: var(--el-color-success);
	}
}

/* 字段名定宽，让值左对齐成一条竖线 */
.uc-fact-k {
	flex: 0 0 auto;
	width: 60px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}

.uc-fact-v {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;

	&.is-point {
		color: var(--el-color-primary);
	}
}

/* ============ 标签 / 电子签名 ============ */
.uc-block {
	margin-top: 14px;
	padding: 0 18px;
}

.uc-block-last {
	padding-bottom: 18px;
}

.uc-block-title {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 10px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);

	&::before {
		content: '';
		width: 3px;
		height: 12px;
		border-radius: 2px;
		background: var(--el-color-primary);
	}
}

.uc-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.uc-sign-box {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 140px;
	padding: 10px;
	overflow: hidden;
	border: 1px dashed var(--el-border-color);
	border-radius: 12px;
	background: var(--el-fill-color-lighter);

	/* 签名图必须给定容器尺寸 + contain：
	   原来 el-image 没有宽高，按图片原始像素撑开（常见 600~1200px 宽），
	   超出 140px 的框后被 overflow: hidden 裁掉，看起来就是"签名没显示全" */
	:deep(.el-image) {
		width: 100%;
		height: 100%;
	}

	:deep(.el-image__inner) {
		width: 100%;
		height: 100%;
		object-fit: contain;
	}
}

.uc-sign-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	color: var(--el-text-color-placeholder);

	.el-icon {
		font-size: 20px;
	}
}

.uc-sign-acts {
	display: flex;
	align-items: center;
	gap: 10px;
	margin-top: 10px;
}

.uc-sign-upload {
	display: inline-flex;
}

/* ============ 右侧标签页 ============ */
.uc-tabs {
	:deep(.el-tabs__header) {
		margin: 0;
	}

	:deep(.el-tabs__nav-wrap)::after {
		height: 1px;
		background-color: var(--el-border-color-lighter);
	}

	:deep(.el-tabs__item) {
		height: 50px;
		font-size: 14px;
	}

	:deep(.el-tabs__content) {
		padding-top: 18px;
	}
}

.uc-form {
	/* 坑：theme/element.scss 有 `.el-form .el-form-item:last-of-type { margin-bottom: 0 !important }`。
	 * 本页表单项是"每个 el-col 里放一个"，对 el-col 来说它就是唯一的 form-item，
	 * 也就是每一项都算 last-of-type —— 行间距被这条 !important 全部清零，
	 * 只剩 label 的 padding 撑着，所以行距忽大忽小（和输入框高度对不上）。
	 * 这里用同特异性 + !important（scoped 样式在 element.scss 之后加载）反压回来。 */
	:deep(.el-form-item.el-form-item) {
		margin-bottom: 24px !important;
	}

	:deep(.el-form-item__label) {
		/* label 默认带 32px 高度（那是给左侧布局垂直居中用的），
		 * 标签在上时要按文字行高自适应，否则标签与输入框之间凭空多出 20 多 px */
		height: auto !important;
		line-height: 1.4;
		padding: 0 0 8px;
		font-size: 13px;
		font-weight: 500;
	}

	/* 最后一行是按钮，交给 .uc-form-acts 自己管 */
	:deep(.uc-form-acts.el-form-item) {
		margin-bottom: 0 !important;
	}
}

/* 按钮行：与上方最后一个表单项拉开距离，且本身不占位 */
.uc-form-acts {
	margin-top: 10px;
	padding-top: 6px;
	margin-bottom: 0;
	border-top: 1px dashed var(--el-border-color-lighter);
}

/* 修改密码：字段少，单列排布并限宽，不要撑满整张卡 */
.uc-form-pwd {
	max-width: 460px;
}

.uc-pwd-tip {
	margin-bottom: 24px;
	border-radius: 10px;
}

/* ============ 登录日志 ============ */
.uc-logs {
	display: flex;
	flex-direction: column;
	gap: 10px;
	max-height: calc(100vh - 300px);
	overflow-y: auto;
	padding-right: 4px;
}

.uc-log {
	display: flex;
	gap: 12px;
	padding: 14px 16px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 12px;
	background: var(--el-bg-color);
	transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;

	&:hover {
		transform: translateY(-2px);
		border-color: color-mix(in srgb, var(--el-color-primary) 40%, transparent);
		box-shadow: 0 10px 22px -14px rgba(0, 0, 0, 0.35);
	}
}

.uc-log-ico {
	flex: 0 0 auto;
	width: 38px;
	height: 38px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 18px;
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
	color: var(--el-color-primary);
}

.uc-log-main {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 6px;
}

.uc-log-head {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;
}

.uc-log-title {
	margin: 0;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.uc-log-time {
	margin-left: auto;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}

.uc-log-meta {
	display: flex;
	flex-wrap: wrap;
	gap: 4px 18px;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	i {
		font-style: normal;
		margin-right: 4px;
		opacity: 0.65;
	}
}

/* ============ 签名画板 ============ */
.uc-pad {
	border: 1px dashed var(--el-border-color);
	border-radius: 12px;
	width: 100%;
	height: 250px;
}

.uc-pad-acts {
	display: flex;
	align-items: center;
	gap: 24px;
	margin-top: 12px;
}

.uc-pad-field {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 13px;
	color: var(--el-text-color-regular);
}
</style>
