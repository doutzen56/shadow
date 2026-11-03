<template>
	<!-- 规格：Web.Admin/src/views/user/agent/gradesetting.html -->
	<div class="user-agent-grade-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="用户名称"><el-input v-model="state.query.UserName" clearable style="width: 140px" @keyup.enter="handleQuery(true)" /></el-form-item>
				<el-form-item label="账号类型">
					<el-select v-model="state.query.Type" clearable placeholder="全部" style="width: 110px">
						<el-option label="会员" value="Member" />
						<el-option label="代理" value="Agent" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:agent:cengjishezhi'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain v-auth="'site:user:agent:cengjishezhi'" @click="openAdd">新增层级</el-button>
			</div>
		</div>
		<div v-if="state.parentTrail.length" class="member-parent-trail">
			<span>代理层级：</span>
			<template v-for="(p, idx) in state.parentTrail" :key="idx">
				<span v-if="idx"> / </span>
				<a href="javascript:;" @click="drill(p.UserName || '')">{{ p.UserName }}</a>
			</template>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="请输入用户名称查询" /></template>
					<template #row_user="{ row }">
						<a href="javascript:;" @click="drill(row.UserName || '')">{{ row.UserName || '-' }}</a>
					</template>
					<template #row_agent="{ row }">
						<a v-if="row.AgentName" href="javascript:;" @click="drill(row.AgentName)">{{ row.AgentName }}</a>
						<span v-else>-</span>
					</template>
					<template #row_type="{ row }">
						<el-tag size="small" :type="Number(row.Type) === 1 ? 'warning' : 'info'">{{ Number(row.Type) === 1 ? '代理' : '会员' }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button v-if="Number(row.Type) !== 1" text type="primary" @click="setAsAgent(row)">设为代理</el-button>
						<el-button v-else text type="warning" @click="lockAgent(row)">锁定代理</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.addVisible" title="新增层级（设为代理）" width="480px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="110px">
				<el-form-item label="用户名" required>
					<el-input v-model="state.form.UserName" clearable placeholder="要设为代理的会员用户名" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.form.ParentName" clearable placeholder="空=顶层；默认当前查询用户" />
				</el-form-item>
				<el-form-item label="佣金方案">
					<el-select v-model="state.form.CommissionID" clearable placeholder="不改/默认" style="width: 100%">
						<el-option v-for="g in state.commissionOptions" :key="String(g.ID)" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="返点方案">
					<el-select v-model="state.form.ReturnID" clearable placeholder="不改/默认" style="width: 100%">
						<el-option v-for="g in state.returnOptions" :key="String(g.ID)" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSaveAdd">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentGrade">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';
import { fetchAgentGradeList } from '/@/api/user/agent';
import { fetchCommissionOptions, fetchMemberParentList, fetchReturnPlanOptions, type MemberOption } from '/@/api/user/member';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentGrade', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'asc', descStr: 'desc' },
});
const emptyForm = () => ({ UserName: '', ParentName: '', CommissionID: '', ReturnID: '' });
const state = reactive({
	query: { UserName: '', Type: '' as string },
	parentTrail: [] as Array<{ UserName?: string }>,
	addVisible: false,
	saving: false,
	form: emptyForm(),
	commissionOptions: [] as MemberOption[],
	returnOptions: [] as MemberOption[],
	localPageParam,
});
const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentGrade',
		name: '层级设置',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'UserName', title: '用户名称', minWidth: 140, slots: { default: 'row_user' } },
			{
				field: 'AgentName',
				title: '所属上级',
				minWidth: 140,
				slots: { default: 'row_agent' },
			},
			{ field: 'Type', title: '账号类型', width: 100, slots: { default: 'row_type' } },
			{ field: 'buttons', title: '操作', width: 120, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: false, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { enabled: false } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await fetchAgentGradeList({
			UserName: state.query.UserName || '',
			...(state.query.Type !== '' && state.query.Type != null ? { Type: state.query.Type } : {}),
		});
		const list = unwrapAdminList<Row>(info);
		if (state.query.UserName) {
			try {
				const parents = await fetchMemberParentList(state.query.UserName);
				state.parentTrail = Array.isArray(parents) ? parents : [];
			} catch {
				state.parentTrail = [];
			}
		} else {
			state.parentTrail = [];
		}
		return { list, RecordCount: list.length };
	});
const resetQuery = () => {
	state.query.UserName = '';
	state.query.Type = '';
	state.parentTrail = [];
	handleQuery(true);
};
const drill = (userName: string) => {
	state.query.UserName = userName;
	handleQuery(true);
};
const openAdd = () => {
	Object.assign(state.form, emptyForm());
	state.form.ParentName = state.query.UserName || '';
	state.addVisible = true;
};
const onSaveAdd = async () => {
	if (!state.form.UserName?.trim()) {
		ElMessage.warning('请填写用户名');
		return;
	}
	state.saving = true;
	try {
		await adminPost('/admin/user/agent/grade/save', {
			UserName: state.form.UserName.trim(),
			ParentName: state.form.ParentName?.trim() || '',
			AsAgent: 1,
			CommissionID: state.form.CommissionID || 0,
			ReturnID: state.form.ReturnID || 0,
		});
		ElMessage.success('已设为代理');
		state.addVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};
const setAsAgent = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`将 ${row.UserName} 设为代理？`, '提示', { type: 'warning' });
		await adminPost('/admin/user/agent/grade/save', {
			UserID: row.ID,
			UserName: row.UserName,
			ParentName: state.query.UserName || row.AgentName || '',
			AsAgent: 1,
		});
		ElMessage.success('已设为代理');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
const lockAgent = async (row: Row) => {
	try {
		await ElMessageBox.confirm(`锁定代理 ${row.UserName}？`, '提示', { type: 'warning' });
		await adminPost('/admin/user/agent/grade/save', {
			UserID: row.ID,
			AsAgent: 0,
		});
		ElMessage.success('已锁定');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
onMounted(async () => {
	const [commissions, returns] = await Promise.all([
		fetchCommissionOptions().catch(() => []),
		fetchReturnPlanOptions().catch(() => []),
	]);
	state.commissionOptions = commissions;
	state.returnOptions = returns;
});
</script>

<style scoped>
.member-parent-trail {
	margin: 0 0 10px;
	font-size: 13px;
}
.member-parent-trail a {
	color: var(--el-color-primary);
}
</style>
