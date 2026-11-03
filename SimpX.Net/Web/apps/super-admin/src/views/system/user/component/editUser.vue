<template>
	<div class="sys-user-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-tabs v-loading="state.loading" v-model="state.selectedTabName">
				<el-tab-pane :label="'基础信息'" style="height: 650px; overflow-y: auto; overflow-x: hidden">
					<el-form ref="ruleFormRef" :model="state.ruleForm" label-width="auto">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item
									:label="'账号'"
									prop="account"
									:rules="[
										{ required: true, message: '账号名称不能为空', trigger: 'blur' },
										{ min: 3, message: '至少3个字符', trigger: 'blur' },
									]"
								>
									<el-input v-model="state.ruleForm.account" :placeholder="'账号'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item
									:label="'姓名'"
									prop="realName"
									:rules="[
										{ required: true, message: '真实姓名不能为空', trigger: 'blur' },
										{ min: 2, message: '至少2个字符', trigger: 'blur' },
									]"
								>
									<el-input v-model="state.ruleForm.realName" :placeholder="'姓名'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item
									:label="'手机号码'"
									prop="phone"
									:rules="[
										{ required: true, message: '手机号码不能为空', trigger: 'blur' },
										{ pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' },
									]"
								>
									<el-input v-model="state.ruleForm.phone" :placeholder="'手机号码'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'昵称'" prop="nickName" :rules="[{ min: 2, message: '至少2个字符', trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.nickName" :placeholder="'昵称'" clearable />
								</el-form-item>
							</el-col>
							<!-- <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'账号类型'" prop="accountType" :rules="[{ required: true, message: '账号类型不能为空', trigger: 'blur' }]">
									<g-sys-dict v-model="state.ruleForm.accountType" code="AccountTypeEnum" :placeholder="'账号类型'" renderAs="select" clearable />
								</el-form-item>
							</el-col> -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'邮箱'" prop="email" :rules="[{ pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: '邮箱格式不正确', trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.email" :placeholder="'邮箱'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'性别'">
									<g-sys-dict v-model="state.ruleForm.sex" code="GenderEnum" renderAs="radio" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'工号'">
									<el-input v-model="state.ruleForm.jobNum" :placeholder="'工号'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'排序'">
									<el-input-number v-model="state.ruleForm.orderNo" :placeholder="'排序'" class="w100" />
								</el-form-item>
							</el-col>
							<el-divider border-style="dashed" content-position="center">
								<div style="color: #b1b3b8">{{ '机构组织' }}</div>
							</el-divider>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="'所属机构'" prop="orgId" :rules="[{ required: true, message: '所属机构不能为空', trigger: 'blur' }]">
									<el-cascader v-model="state.ruleForm.orgId" :options="props.orgData" :props="cascaderProps" :placeholder="'所属机构'" clearable filterable class="w100">
										<template #default="{ node, data }">
											<span>{{ data.name }}</span>
											<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
										</template>
									</el-cascader>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'职位名称'">
									<el-select v-model="state.ruleForm.posId" :placeholder="'职位名称'" class="w100">
										<el-option v-for="d in state.posData" :key="d.id" :label="d.name" :value="d.id">
											<span style="float: left"> {{ d.name }} </span>
											<span style="float: right; color: var(--el-text-color-secondary)"> {{ d.type }} </span>
										</el-option>
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'域账号'">
									<el-input v-model="state.ruleForm.domainAccount" :placeholder="'域账号'" clearable />
								</el-form-item>
							</el-col>
							<el-divider border-style="dashed" content-position="center">
								<div style="color: #b1b3b8">{{ '附属机构' }}</div>
							</el-divider>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
								<el-button icon="ele-Plus" type="primary" @click="addExtOrgRow"> {{ '增加附属机构' }} </el-button>
								<span style="font-size: 12px; color: gray; padding-left: 5px"> {{ '具有相应组织机构的数据权限' }} </span>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<template v-if="state.ruleForm.extOrgIdList != undefined && state.ruleForm.extOrgIdList.length > 0">
									<el-row :gutter="10" v-for="(v, k) in state.ruleForm.extOrgIdList" :key="k">
										<el-col :xs="11" :sm="11" :md="11" :lg="11" :xl="11" class="mb20">
											<el-form-item :label="'所属机构'" :prop="`extOrgIdList[${k}].orgId`" :rules="[{ required: true, message: '机构不能为空', trigger: 'blur' }]">
												<el-cascader v-model="state.ruleForm.extOrgIdList[k].orgId" :options="props.orgData" :props="cascaderProps" :placeholder="'所属机构'" clearable filterable class="w100">
													<template #default="{ node, data }">
														<span>{{ data.name }}</span>
														<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
													</template>
												</el-cascader>
											</el-form-item>
										</el-col>
										<el-col :xs="11" :sm="11" :md="11" :lg="11" :xl="11" class="mb20">
											<el-form-item :label="'职位名称'">
												<el-select v-model="state.ruleForm.extOrgIdList[k].posId" :placeholder="'职位名称'" class="w100">
													<el-option v-for="d in state.posData" :key="d.id" :label="d.name" :value="d.id">
														<span style="float: left"> {{ d.name }} </span>
														<span style="float: right; color: var(--el-text-color-secondary)"> {{ d.type }} </span>
													</el-option>
												</el-select>
											</el-form-item>
										</el-col>
										<el-col :xs="2" :sm="2" :md="2" :lg="2" :xl="2" class="mb20" style="text-align: right">
											<el-button icon="ele-Delete" type="danger" circle plain @click="deleteExtOrgRow(k)" style="margin-top: 2px" />
										</el-col>
									</el-row>
								</template>
								<el-empty v-else :image-size="50" :description="'暂无数据'"></el-empty>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<el-tab-pane :label="'角色授权'" style="height: 650px; overflow-y: auto; overflow-x: hidden">
					<Transfer :left-title="'未授权'" :right-title="'已授权'" v-model:leftData="state.availableRoles" v-model:rightData="state.grantedRoles" />
				</el-tab-pane>
				<el-tab-pane :label="'档案信息'" style="height: 650px; overflow-y: auto; overflow-x: hidden">
					<el-form :model="state.ruleForm" label-width="auto">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'证件类型'" prop="cardType">
									<el-select v-model="state.ruleForm.cardType" :placeholder="'证件类型'" class="w100">
										<el-option :label="'身份证'" :value="0" />
										<el-option :label="'护照'" :value="1" />
										<el-option :label="'出生证'" :value="2" />
										<el-option :label="'港澳台通行证'" :value="3" />
										<el-option :label="'外国人居留证'" :value="4" />
										<el-option :label="'其他'" :value="99" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'证件号码'" prop="idCardNum" :rules="[useFormRulePresets().idCard(() => state.ruleForm.cardType === 0)]">
									<el-input v-model="state.ruleForm.idCardNum" :placeholder="'证件号码'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'出生日期'" prop="birthday">
									<el-date-picker v-model="state.ruleForm.birthday" type="date" :placeholder="'出生日期'" format="YYYY-MM-DD" value-format="YYYY-MM-DD" class="w100" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'年龄'">
									<el-input-number v-model="state.ruleForm.age" :placeholder="'年龄'" class="w100" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'民族'">
									<!-- <el-input v-model="state.ruleForm.nation" :placeholder="'民族'" clearable /> -->
									<g-sys-dict v-model="state.ruleForm.nation" code="NationEnum" renderAs="select" prop-value="label" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'职称'">
									<el-input v-model="state.ruleForm.posTitle" :placeholder="'职称'" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="'地址'">
									<el-input v-model="state.ruleForm.address" :placeholder="'地址'" clearable>
										<template #suffix>
											<el-icon @click="pickupMapAddress" style="cursor: pointer"><ele-MapLocation /></el-icon>
										</template>
									</el-input>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'入职日期'">
									<el-date-picker v-model="state.ruleForm.joinDate" type="date" :placeholder="'入职日期'" format="YYYY-MM-DD" value-format="YYYY-MM-DD" class="w100" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'职级'">
									<el-input v-model="state.ruleForm.posLevel" :placeholder="'职级'" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'毕业学校'">
									<el-input v-model="state.ruleForm.college" :placeholder="'毕业学校'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'文化程度'">
									<g-sys-dict v-model="state.ruleForm.cultureLevel" code="CultureLevelEnum" renderAs="select" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'政治面貌'">
									<el-input v-model="state.ruleForm.politicalOutlook" :placeholder="'政治面貌'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'办公电话'">
									<el-input v-model="state.ruleForm.officePhone" :placeholder="'办公电话'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'紧急联系人'">
									<el-input v-model="state.ruleForm.emergencyContact" :placeholder="'紧急联系人'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="'联系人电话'">
									<el-input v-model="state.ruleForm.emergencyPhone" :placeholder="'联系人电话'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="'联系人地址'">
									<el-input v-model="state.ruleForm.emergencyAddress" :placeholder="'联系人地址'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="'个人简介'">
									<el-input v-model="state.ruleForm.introduction" :placeholder="'请输入个人简介'" clearable type="textarea" :rows="4" maxlength="512" show-word-limit />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="'备注'">
									<el-input v-model="state.ruleForm.remark" :placeholder="'请输入备注内容'" clearable type="textarea" maxlength="256" show-word-limit />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<el-tab-pane v-if="auth('sysOAuthUser/page')" label="账号绑定" style="height: 650px; overflow-y: auto; overflow-x: hidden">
					<el-form :model="state.ruleForm" label-width="auto">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<template v-if="state.oAuthBindLoading || (state.oAuthBindList != undefined && state.oAuthBindList.length > 0)">
									<vxe-grid v-bind="oAuthBindGridOptions" :data="state.oAuthBindList" :loading="state.oAuthBindLoading">
										<template #bind_platformType="{ row }">
											<g-sys-dict v-model="row.platformType" code="PlatformTypeEnum" />
										</template>
										<template #bind_buttons="{ row }">
											<el-button icon="ele-Delete" type="danger" text v-auth="'sysOAuthUser/delete'" @click="deleteOAuthBind(row)">删除</el-button>
										</template>
									</vxe-grid>
								</template>
								<el-empty v-else description="暂无绑定" style="padding: 5px 0"></el-empty>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</span>
			</template>
		</el-dialog>

		<mapDialog ref="mapDialogRef" title="选取地址" :isPickupCoord="true" @coordinate="coordinate" />
	</div>
</template>

<!-- 账号编辑 -->
<script lang="ts" setup name="sysEditUser">
import { nextTick, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridProps } from 'vxe-table';
import Transfer from '/@/components/transfer/index.vue';
import { auth } from '/@/utils/authFunction';
import { useFormRulePresets } from '/@/utils/formRule';

import { getAPI } from '/@/utils/axios-utils';
import { SysCommonApi, SysOAuthUserApi, SysPosApi, SysRoleApi, SysUserApi } from '/@/api-services/system/api';
import { SysOrg, PagePosOutput, UpdateUserInput } from '/@/api-services/system/models';

import mapDialog from '/@/views/system/log/logvis/component/visMap.vue';
const mapDialogRef = ref<InstanceType<typeof mapDialog>>();

const props = defineProps({
	title: String,
	orgData: Array<SysOrg>,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();

// 账号绑定列表（第三方 OAuth）
const oAuthBindGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	size: 'mini',
	columns: [
		{ field: 'platformType', title: '平台类型', width: 120, slots: { default: 'bind_platformType' } },
		{ field: 'openId', title: 'OpenId', minWidth: 210, showOverflow: 'tooltip' },
		{ field: 'unionId', title: 'UnionId', minWidth: 210, showOverflow: 'tooltip' },
		{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', slots: { default: 'bind_buttons' } },
	],
});

const state = reactive({
	loading: false,
	isShowDialog: false,
	selectedTabName: '0', // 选中的 tab 页
	ruleForm: {} as UpdateUserInput,
	posData: [] as Array<PagePosOutput>, // 职位数据
	availableRoles: [] as any, // 可授权角色
	grantedRoles: [] as any, // 已授权角色
	oAuthBindLoading: false,
	oAuthBindList: [] as any[],
});
// 级联选择器配置选项
const cascaderProps = { checkStrictly: true, emitPath: false, value: 'id', label: 'name', expandTrigger: 'hover' };

// 页面初始化
onMounted(async () => {
	state.loading = true;
	const { data } = await getAPI(SysPosApi).apiSysPosListGet();
	state.posData = data.result ?? [];
	state.loading = false;
});

// 打开弹窗
const openDialog = async (row: any) => {
	ruleFormRef.value?.resetFields();

	state.selectedTabName = '0'; // 重置为第一个 tab 页
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.oAuthBindList = [];
	if (row.id != undefined) {
		const { data } = await getAPI(SysUserApi).apiSysUserOwnRoleListUserIdGet(row.id);
		state.availableRoles = data.result?.availableRoles;
		state.grantedRoles = data.result?.grantedRoles;
		var resExtOrg = await getAPI(SysUserApi).apiSysUserOwnExtOrgListUserIdGet(row.id);
		state.ruleForm.extOrgIdList = resExtOrg.data.result;
		if (auth('sysOAuthUser/page')) await loadOAuthBindList(state.ruleForm.id);
	} else {
		state.ruleForm.accountType = 777; // 默认普通账号类型
		// state.ruleForm.posId = 1300000000106; // 默认职位
		const { data } = await getAPI(SysRoleApi).apiSysRoleListGet();
		state.availableRoles = data.result ?? [];
		state.grantedRoles = [];
		// state.grantedRoles = [{ id: 1300000000104, name: '仅本人数据' }]; // 默认仅本人数据角色
	}
	state.isShowDialog = true;
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
	ruleFormRef.value.validate(async (valid: boolean, invalidFields: Record<string, any[]>) => {
		if (!valid) {
			// 出错的红字只渲染在「基础信息」页签，停在其他页签时不可见，用户会以为按钮失效，故切回出错页签并定位首个出错项
			state.selectedTabName = '0';
			const firstProp = Object.keys(invalidFields)[0];
			ElMessage.error(invalidFields[firstProp][0].message);
			await nextTick();
			ruleFormRef.value.scrollToField(firstProp);
			return;
		}
		if (state.grantedRoles?.length > 0) state.ruleForm.roleIdList = state.grantedRoles.map((e: any) => e.id);
		else {
			ElMessage.error('请分配角色');
			state.selectedTabName = '1'; // 自动跳转到「角色授权」Tab
			return;
		}
		let res;
		try {
			if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
				res = await getAPI(SysUserApi).apiSysUserUpdatePost(state.ruleForm);
			} else {
				res = await getAPI(SysUserApi).apiSysUserAddPost(state.ruleForm);
			}
		} catch {
			// 网络异常等已由拦截器提示，不关闭弹窗，保留用户已填写内容
			return;
		}
		// 业务失败（如手机号已存在）拦截器已提示，不关闭弹窗，保留用户已填写内容
		if (res?.data?.code !== 200) return;
		closeDialog();
	});
};

// 增加附属机构行
const addExtOrgRow = () => {
	if (state.ruleForm.extOrgIdList == undefined) state.ruleForm.extOrgIdList = [];
	state.ruleForm.extOrgIdList?.push({});
};

// 删除附属机构行
const deleteExtOrgRow = (k: number) => {
	state.ruleForm.extOrgIdList?.splice(k, 1);
};

// 加载账号绑定列表
const loadOAuthBindList = async (id?: number) => {
	if (!id) {
		state.oAuthBindList = [];
		return;
	}

	state.oAuthBindLoading = true;
	try {
		const { data } = await getAPI(SysOAuthUserApi).apiSysOAuthUserListUserIdGet(id);
		state.oAuthBindList = (data.result ?? []) as any[];
	} catch {
		state.oAuthBindList = [];
	} finally {
		state.oAuthBindLoading = false;
	}
};

// 删除账号绑定
const deleteOAuthBind = async (row: any) => {
	if (!row?.id) return;

	await ElMessageBox.confirm('确定删除该第三方绑定吗？', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	});
	await getAPI(SysOAuthUserApi).apiSysOAuthUserDeletePost({ id: row.id });
	ElMessage.success('删除成功');
	await loadOAuthBindList(state.ruleForm?.id);
};

// 打开访问热力图
const pickupMapAddress = async () => {
	mapDialogRef.value?.openDialog();
};

// 获取坐标
const coordinate = async (e: any) => {
	var res = await getAPI(SysCommonApi).apiSysCommonTiandituAddressGet(e[0], e[1]);
	state.ruleForm.address = res.data.result ?? e.join(',');
	// state.ruleForm.address = e.join(',');
};

// 导出对象
defineExpose({ openDialog });
</script>
