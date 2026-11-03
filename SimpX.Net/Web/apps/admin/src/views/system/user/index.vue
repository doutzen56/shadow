<template>
	<div class="sys-user-container">
		<splitpanes class="default-theme">
			<pane size="15" style="display: flex">
				<OrgTree ref="orgTreeRef" @node-click="handleNodeChange" />
			</pane>
			<pane size="85" style="display: flex; flex-direction: column">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item label="账号">
									<el-input v-model="state.queryParams.account" placeholder="账号" clearable @keyup.enter.native="handleQuery(true)" />
								</el-form-item>
							</el-col>

							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item label="姓名">
									<el-input v-model="state.queryParams.realName" placeholder="姓名" clearable @keyup.enter.native="handleQuery(true)" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item label="职位名称">
									<el-input v-model="state.queryParams.posName" placeholder="职位名称" clearable />
								</el-form-item>
							</el-col>

							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item label="手机号码">
									<el-input v-model="state.queryParams.phone" placeholder="手机号码" clearable @keyup.enter.native="handleQuery(true)" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<el-row>
						<el-col>
							<el-button-group>
								<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysUser/page'" :loading="options.loading"> 查询 </el-button>
								<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
							</el-button-group>
						</el-col>
					</el-row>
				</el-card>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px; flex: 1">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
						<template #toolbar_buttons>
							<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysUser/add'"> 新增 </el-button>
						</template>
						<template #toolbar_tools> </template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_avatar="{ row }">
							<el-avatar :src="row.avatar" size="small" :style="{ backgroundColor: row.sex == 1 ? 'var(--el-color-primary)' : 'var(--el-color-danger)' }"> {{ row.nickName?.slice(0, 1) ?? row.realName?.slice(0, 1) }} </el-avatar>
						</template>
						<template #row_realName="{ row }">
							{{ row.realName }}
							<el-icon v-if="row.sex == 1" color="var(--el-color-primary)">
								<ele-Male />
							</el-icon>
							<el-icon v-else-if="row.sex == 2" color="var(--el-color-danger)">
								<ele-Female />
							</el-icon>
							<!-- <g-sys-dict v-model="row.sex" code="GenderEnum" /> -->
						</template>
						<template #row_accountType="{ row }">
							<g-sys-dict v-model="row.accountType" code="AccountTypeEnum" />
						</template>
						<!-- <template #row_points="{ row }">
							<span style="color: var(--el-color-primary); font-weight: 600;">{{ row.points }}</span>
						</template> -->
						<template #row_status="{ row }">
							<el-switch v-model="row.status" :active-value="1" :inactive-value="2" size="small" @change="changeStatus(row)" v-auth="'sysUser/setStatus'" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip content="编辑" placement="top">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysUser/update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip content="删除" placement="top">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysUser/delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
							<el-tooltip content="复制" placement="top">
								<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysUser/add'" @click="openCopyMenu(row)"> </el-button>
							</el-tooltip>

							<el-button icon="ele-RefreshLeft" text type="danger" v-auth="'sysUser/resetPwd'" @click="resetQueryPwd(row)">重置密码</el-button>
							<el-button icon="ele-Unlock" text type="primary" v-auth="'sysUser/unlockLogin'" @click="handleUnlock(row)">解除锁定</el-button>
						</template>
					</vxe-grid>
				</el-card>
			</pane>
		</splitpanes>

		<EditUser ref="editUserRef" :title="state.title" :orgData="state.treeData" @handleQuery="handleQuery" />
	</div>
</template>

<!-- 用户管理 -->
<script lang="ts" setup name="sysUser">
import { onMounted, reactive, ref, onActivated } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditUser from '/@/views/system/user/component/editUser.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi, SysOrgApi } from '/@/api-services/system/api';
import { SysOrg, PageTenantInput, UserOutput, UpdateUserInput } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
// const treeRef = ref<InstanceType<typeof OrgTree>>();
const editUserRef = ref<InstanceType<typeof EditUser>>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysUser', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({
	treeData: [] as Array<SysOrg>,
	queryParams: {
		orgId: -1,
		account: undefined,
		realName: undefined,
		posName: undefined,
		phone: undefined,
	},
	localPageParam,
	visible: false,
	title: '',
});

// 表格参数配置
const options = useVxeTable<UserOutput>(
	{
		id: 'sysUser',
		name: '账号',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'avatar', title: '头像', showOverflow: 'tooltip', slots: { default: 'row_avatar' } },
			{ field: 'account', title: '账号', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'realName', title: '姓名', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_realName' } },
			{ field: 'nickName', title: '昵称', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'phone', title: '手机号码', minWidth: 100, showOverflow: 'tooltip' },
			// { field: 'birthday', title: '出生日期', minWidth: 100, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => XEUtils.toDateString(cellValue, 'yyyy-MM-dd') },
			// { field: 'sex', title: '性别', showOverflow: 'tooltip', slots: { default: 'row_sex' } },
			{ field: 'roleName', title: '角色集合', minWidth: 130, showOverflow: 'tooltip' },
			{ field: 'accountType', title: '账号类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_accountType' } },
			{ field: 'orgName', title: '所属机构', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'posName', title: '职位名称', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'jobNum', title: '工号', minWidth: 100, showOverflow: 'tooltip' },
			// { field: 'points', title: '积分', minWidth: 80, showOverflow: 'tooltip', slots: { default: 'row_points' } },
			{ field: 'email', title: '邮箱', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', width: 80, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'orderNo', title: '排序', showOverflow: 'tooltip' },

			{ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// height:'400',
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {
	await fetchOrgData();
});

// 组织架构节点切换
onActivated(async () => {
	// await fetchOrgData();
});

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageTenantInput;
	return getAPI(SysUserApi).apiSysUserPagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.orgId = -1;
	state.queryParams.account = undefined;
	state.queryParams.realName = undefined;
	state.queryParams.posName = undefined;
	state.queryParams.phone = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 查询机构数据
const fetchOrgData = async () => {
	var res = await getAPI(SysOrgApi).apiSysOrgListGet(0);
	state.treeData = res.data.result ?? [];
};

// 打开新增页面
const handleAdd = () => {
	state.title = '添加账号';
	editUserRef.value?.openDialog({ id: undefined, birthday: '2000-01-01', sex: 1, orderNo: 100, cardType: 0, nation: '汉族', cultureLevel: 10 });
};

// 打开编辑页面
const handleEdit = async (row: any) => {
	var newRow = await getDetail(row);

	state.title = '编辑账号';
	editUserRef.value?.openDialog(newRow);
};

// 打开复制页面
const openCopyMenu = async (row: any) => {
	// var newRow = await getDetail(row);

	state.title = '复制账号';
	var copyRow = { ...row, id: 0, account: '', phone: '', idCardNum: '', email: '' } as UpdateUserInput;
	editUserRef.value?.openDialog(copyRow);
};

// 重新获取用户详情（电话和身份证脱敏）
const getDetail = async (row: any) => {
	var res = await getAPI(SysUserApi).apiSysUserBaseInfoGet(row.id);
	// ({ phone: row.phone, idCardNum: row.idCardNum, email: row.email } = res.data.result ?? {});
	return {
		...row,
		phone: res.data.result?.phone,
		idCardNum: res.data.result?.idCardNum,
		email: res.data.result?.email,
	};
};

// 删除
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定删除账号：${row.account}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysUserApi).apiSysUserDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<UserOutput> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysUser/update')) await handleEdit(row);
	},
};

// 修改状态
const changeStatus = (row: any) => {
	getAPI(SysUserApi)
		.apiSysUserSetStatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success('账号状态设置成功');
		})
		.catch(() => {
			row.status = row.status == 1 ? 2 : 1;
		});
};

// 重置密码
const resetQueryPwd = async (row: any) => {
	ElMessageBox.prompt(`请输入新密码，如果不填将使用默认密码`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async ({ value }) => {
			await getAPI(SysUserApi)
				.apiSysUserResetPwdPost({ id: row.id, newPassword: value })
				.then((res) => {
					ElMessage.success(`密码重置成功：${res.data.result}`);
				});
		})
		.catch(() => {});
};

// 解除登录锁定
const handleUnlock = async (row: any) => {
	ElMessageBox.confirm(`确定解除登录锁定：${row.account}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysUserApi)
				.apiSysUserUnlockLoginPost({ id: row.id })
				.then(() => {
					ElMessage.success('解除登录锁定成功');
				});
		})
		.catch(() => {});
};

// 树组件点击
const handleNodeChange = async (node: any) => {
	state.queryParams.orgId = node.id;
	state.queryParams.account = undefined;
	state.queryParams.realName = undefined;
	state.queryParams.phone = undefined;
	await handleQuery();
};
</script>
