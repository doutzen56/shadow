<template>
	<!-- 规格来源：Web.Admin/src/views/user/member/list.html；菜单 Component=/user/member/list -->
	<div class="user-member-list-page admin-page">
		<div class="page-header-actions">
			<el-button type="success" plain :loading="state.exporting" v-auth="'site:user:member:list:export'" @click="handleExport">导出(CSV)</el-button>
			<el-button type="warning" plain v-auth="'site:user:member:list'" @click="triggerMarkImport">导入备注(Excel)</el-button>
			<input ref="markFileRef" type="file" accept=".xlsx,.xls,.csv,text/csv,text/plain" style="display: none" @change="onMarkFileChange" />
			<el-button type="primary" v-auth="'site:user:member:list'" @click="openAddAgent">添加总代</el-button>
		</div>

		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="搜索类型">
					<el-select v-model="state.query.SearchType" class="query-select" style="width: 130px">
						<el-option v-for="o in searchTypeOptions" :key="o.value" :label="o.label" :value="o.value" />
					</el-select>
					<el-input v-model="state.query.User" class="query-input" clearable style="width: 140px; margin-left: 6px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" class="query-input" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
					<el-checkbox v-model="state.belongUserChecked" style="margin-left: 8px">包含下级</el-checkbox>
					<el-button style="margin-left: 6px" @click="searchTopAgent">总代</el-button>
				</el-form-item>
				<el-form-item label="用户类型">
					<el-select v-model="state.query.Type" class="query-select" clearable placeholder="全部" style="width: 110px">
						<el-option label="会员" value="Member" />
						<el-option label="代理" value="Agent" />
					</el-select>
				</el-form-item>
				<el-form-item label="佣金方案">
					<el-select v-model="state.query.CommissionID" class="query-select" clearable placeholder="全部" style="width: 140px">
						<el-option v-for="g in state.commissionOptions" :key="String(g.ID)" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="返点方案">
					<el-select v-model="state.query.ReturnID" class="query-select" clearable placeholder="全部" style="width: 140px">
						<el-option v-for="g in state.returnOptions" :key="String(g.ID)" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="用户分组">
					<el-select v-model="state.query.GroupID" class="query-select" clearable placeholder="全部" style="width: 140px">
						<el-option v-for="g in state.groupOptions" :key="String(g.ID)" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="用户等级">
					<el-select v-model="state.query.Level" class="query-select" clearable placeholder="全部" style="width: 120px">
						<el-option v-for="g in state.levelOptions" :key="String(g.Level)" :label="g.Name || String(g.Level)" :value="String(g.Level)" />
					</el-select>
				</el-form-item>
				<el-form-item label="开户方式">
					<el-select v-model="state.query.RegMethod" class="query-select" clearable placeholder="全部" style="width: 130px">
						<el-option v-for="o in regMethodOptions" :key="o.value" :label="o.label" :value="o.value" />
					</el-select>
				</el-form-item>
				<el-form-item>
					<el-select v-model="state.query.TimeType" class="query-select" style="width: 120px">
						<el-option label="注册时间" value="Reg" />
						<el-option label="登录时间" value="Login" />
						<el-option label="活动时间" value="Active" />
					</el-select>
					<el-date-picker
						v-model="state.timeRange"
						type="datetimerange"
						value-format="YYYY-MM-DD HH:mm:ss"
						range-separator="-"
						start-placeholder="开始"
						end-placeholder="结束"
						style="margin-left: 6px; width: 340px"
					/>
				</el-form-item>
				<el-form-item label="账户余额">
					<el-input v-model="state.query.MinMoney" class="query-input" clearable style="width: 90px" />
					<span style="margin: 0 4px">-</span>
					<el-input v-model="state.query.MaxMoney" class="query-input" clearable style="width: 90px" />
				</el-form-item>
				<el-form-item label="用户层级">
					<el-input v-model="state.query.Depth" class="query-input" clearable style="width: 80px" />
				</el-form-item>
				<el-form-item label="在线时间">
					<el-input v-model="state.query.Active" class="query-input" clearable style="width: 80px" />
					<span style="margin-left: 4px">分钟内</span>
				</el-form-item>
				<el-form-item label="注册域名">
					<el-input v-model="state.query.Domain" class="query-input" clearable style="width: 140px" />
				</el-form-item>
				<el-form-item v-if="state.showRegCurrency" label="注册币种">
					<el-select v-model="state.query.RegCurrency" class="query-select" clearable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="String(c.value ?? c.key)" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="邀请人">
					<el-input v-model="state.query.InviterName" class="query-input" clearable style="width: 120px" />
				</el-form-item>
				<el-form-item label="显示币种">
					<el-select v-model="state.query.Currency" class="query-select" clearable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="'c-' + String(c.key)" :label="String(c.value ?? c.key)" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="充值次数">
					<el-select v-model="state.query.RechargeCount" class="query-select" clearable placeholder="全部" style="width: 100px">
						<el-option label="首充" value="1" />
						<el-option label="二充" value="2" />
						<el-option label="三充" value="3" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:member:list'" @click="handleQuery(true)">搜索</el-button>
					<el-button :loading="options.loading" @click="searchToday">今日</el-button>
					<el-button :loading="options.loading" @click="searchYesterday">昨天</el-button>
					<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				</el-button-group>
				<el-button type="warning" plain :loading="options.loading" @click="searchAllMembers">查看全体会员</el-button>
			</div>
		</div>

		<div v-if="state.parentTrail.length" class="member-parent-trail">
			<span>代理层级：</span>
			<a href="javascript:;" @click="drillAgent('')">全部</a>
			<template v-for="(p, idx) in state.parentTrail" :key="idx">
				<span> / </span>
				<a href="javascript:;" @click="drillAgent(p.UserName || '')">{{ p.UserName }}</a>
			</template>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #toolbar_buttons />
					<template #toolbar_tools />
					<template #empty>
						<el-empty :image-size="160" description="暂无数据（请先输入查询条件）" />
					</template>
					<template #row_user="{ row }">
						<span :title="row.ActiveAt ? `在线时间:${row.ActiveAt}` : ''">
							<span :style="{ color: row.IsOnline ? '#67c23a' : '#c0c4cc', marginRight: '4px' }">●</span>
							<a href="javascript:;" @click="openDetail(row)">{{ row.UserName }}</a>
						</span>
					</template>
					<template #row_type="{ row }">
						<span>{{ row.TypeName || (row.Type === 0 ? '会员' : '代理') }}</span>
						<template v-if="row.Type !== 0 && row.Count">
							/
							<a href="javascript:;" title="查看下级" @click="drillAgent(row.UserName || '')">{{ row.Count }}</a>
						</template>
					</template>
					<template #row_lock="{ row }">
						<el-tag v-if="Number(row.Lock) > 0" type="danger" size="small" effect="plain">{{ formatUserLockFlags(row.Lock) }}</el-tag>
						<span v-else class="muted">无</span>
					</template>
					<template #row_money="{ row }">
						<span>{{ row.Currency != null && row.Currency !== '' ? `${formatCurrency(row.Currency) || row.Currency}:` : '' }}{{ formatMoney(row.TotalMoney ?? row.Money) }}</span>
						<a href="javascript:;" style="margin-left: 6px" title="多币种余额" @click="openMoney(row)">▾</a>
					</template>
					<template #row_recharge="{ row }">
						<span class="money-in">{{ formatMoney(row.TotalRechargeMoney) }}</span>
					</template>
					<template #row_withdraw="{ row }">
						<span class="money-out">{{ formatMoney(row.TotalWithdrawMoney) }}</span>
					</template>
					<template #row_loginAt="{ row }">
						{{ formatDateTime(row.LoginAt) }}
					</template>
					<template #row_reg="{ row }">
						{{ formatRegFlag(row.RegFlag) }}{{ state.showRegCurrency && row.RegCurrency != null ? '/' + (formatCurrency(row.RegCurrency) || row.RegCurrency) : '' }}
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-drawer v-model="state.detailVisible" title="会员详情" size="560px" destroy-on-close>
			<template v-if="state.detailRow">
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="编号">{{ state.detailRow.ID }}</el-descriptions-item>
					<el-descriptions-item label="用户名">{{ state.detailRow.UserName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="昵称">{{ state.detailRow.NickName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="开户名">{{ state.detailRow.AccountName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="分组">{{ state.detailRow.Group || state.detailRow.GroupName || state.detailRow.GroupID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="等级">{{ state.detailRow.LevelName || state.detailRow.Level || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detailRow.Currency) || state.detailRow.Currency || '-' }}</el-descriptions-item>
					<el-descriptions-item label="余额">{{ formatMoney(state.detailRow.Money) }}</el-descriptions-item>
					<el-descriptions-item label="手机">{{ state.detailRow.Mobile || '-' }}</el-descriptions-item>
					<el-descriptions-item label="邮箱">{{ state.detailRow.Email || '-' }}</el-descriptions-item>
					<el-descriptions-item label="注册IP">{{ state.detailRow.RegIP || '-' }}</el-descriptions-item>
					<el-descriptions-item label="注册域名">{{ state.detailRow.RegDomain || '-' }}</el-descriptions-item>
					<el-descriptions-item label="注册时间">{{ formatDateTime(state.detailRow.CreateAt) }}</el-descriptions-item>
					<el-descriptions-item label="登录时间">{{ formatDateTime(state.detailRow.LoginAt) }}</el-descriptions-item>
					<el-descriptions-item label="登录IP">{{ state.detailRow.LoginIP || '-' }}</el-descriptions-item>
					<el-descriptions-item label="活跃时间">{{ formatDateTime(state.detailRow.ActiveAt) }}</el-descriptions-item>
					<el-descriptions-item label="上级ID">{{ state.detailRow.AgentID ?? '-' }}</el-descriptions-item>
					<el-descriptions-item label="邀请码">{{ state.detailRow.InviteID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="类型">{{ state.detailRow.Type ?? '-' }}</el-descriptions-item>
					<el-descriptions-item label="深度">{{ state.detailRow.Depth ?? '-' }}</el-descriptions-item>
					<el-descriptions-item label="锁定位">{{ formatUserLockFlags(state.detailRow.Lock) }}</el-descriptions-item>
					<el-descriptions-item label="功能位">{{ state.detailRow.Function ?? '-' }}</el-descriptions-item>
				</el-descriptions>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-drawer>

		<el-dialog v-model="state.moneyVisible" title="余额" width="520px" destroy-on-close>
			<template v-if="state.moneyRows.length">
				<p v-if="state.moneyMaster" class="money-master">主钱包：{{ state.moneyMaster }}</p>
				<el-table :data="state.moneyRows" size="small" border>
					<el-table-column prop="currency" label="币种" min-width="120" />
					<el-table-column prop="money" label="可用余额" min-width="140" />
				</el-table>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-dialog>

		<el-dialog v-model="state.agentVisible" title="添加总代" width="420px" destroy-on-close @closed="resetAgentForm">
			<el-form :model="state.agentForm" label-width="80px">
				<el-form-item label="用户名" required>
					<el-input v-model="state.agentForm.UserName" clearable />
				</el-form-item>
				<el-form-item label="密码" required>
					<el-input v-model="state.agentForm.Password" clearable show-password />
				</el-form-item>
				<el-form-item label="货币">
					<el-select v-model="state.agentForm.Currency" clearable placeholder="全部" style="width: 100%">
						<el-option label="全部" value="-1" />
						<el-option v-for="c in state.currencyOptions" :key="'a-' + String(c.key)" :label="String(c.value ?? c.key)" :value="String(c.key)" />
					</el-select>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.agentVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.agentSaving" @click="submitAddAgent">添加总代账户</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberList">
import { onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost, readAdminList, readAdminRecordCount, toVxeProxyPage } from '/@/utils/admin-api';
import { isUserNameHeader, parseAdminImportTable } from '/@/utils/admin-import-file';
import {
	addTopAgent,
	batchAddMemberMarks,
	exportMemberList,
	fetchCommissionOptions,
	fetchCurrencyOptions,
	fetchMemberDetailMoney,
	fetchMemberGroupOptions,
	fetchMemberList,
	fetchMemberParentList,
	fetchReturnPlanOptions,
	fetchSiteType,
	fetchVipLevelOptions,
	type MemberListRow,
	type MemberOption,
} from '/@/api/user/member';
import { formatDateTime } from '/@/utils/format';
import { ensureCurrencyEnum, formatCurrency, formatUserLockFlags } from '/@/utils/admin-display';

const searchTypeOptions = [
	{ value: 'UserName', label: '用户名' },
	{ value: 'UserID', label: '用户ID' },
	{ value: 'AccountName', label: '提现开户名' },
	{ value: 'NickName', label: '昵称' },
	{ value: 'Account', label: '银行卡号' },
	{ value: 'Mobile', label: '手机号码' },
	{ value: 'Email', label: '电子邮箱' },
	{ value: 'GameUser', label: '游戏帐号' },
	{ value: 'RegIP', label: '注册IP' },
	{ value: 'LoginIP', label: '登录IP' },
	{ value: 'DeviceId', label: '最近登录设备号' },
	{ value: 'KYCId', label: '证件号码' },
];

/** 开户方式：对齐旧 RegMethod 常见项 */
const regMethodOptions = [
	{ value: '0', label: '前台注册' },
	{ value: '1', label: '后台开户' },
	{ value: '2', label: '管理员创建' },
	{ value: '3', label: '导入' },
];

const route = useRoute();
const xGrid = ref<VxeGridInstance>();
const markFileRef = ref<HTMLInputElement | null>(null);
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberList', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const emptyQuery = () => ({
	SearchType: 'UserName',
	User: '',
	Agent: '',
	Type: '' as string,
	GroupID: '' as string,
	Level: '',
	RegMethod: '' as string,
	TimeType: 'Reg',
	MinMoney: '',
	MaxMoney: '',
	Depth: '',
	Active: '',
	Domain: '',
	InviterName: '',
	RechargeCount: '' as string,
	CommissionID: '',
	ReturnID: '',
	Currency: '',
	RegCurrency: '',
});

const state = reactive({
	query: emptyQuery(),
	belongUserChecked: false,
	timeRange: null as [string, string] | null,
	isLimit: 1,
	groupOptions: [] as MemberOption[],
	commissionOptions: [] as MemberOption[],
	returnOptions: [] as MemberOption[],
	levelOptions: [] as MemberOption[],
	currencyOptions: [] as MemberOption[],
	showRegCurrency: false,
	parentTrail: [] as Array<{ UserName?: string }>,
	detailVisible: false,
	detailRow: null as Record<string, any> | null,
	moneyVisible: false,
	moneyMaster: '' as string,
	moneyRows: [] as Array<{ currency: string; money: string }>,
	agentVisible: false,
	agentSaving: false,
	agentForm: { UserName: '', Password: '', Currency: '-1' },
	exporting: false,
	localPageParam,
});

const options = useVxeTable<MemberListRow>(
	{
		id: 'siteUserMemberList',
		name: '会员列表',
		columns: [
			{ field: 'ID', title: '编号', width: 90 },
			{ field: 'UserName', title: '用户名', minWidth: 140, slots: { default: 'row_user' } },
			{ field: 'AgentName', title: '所属上级', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'InviterName', title: '邀请人', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'TypeName', title: '账号类型', minWidth: 110, slots: { default: 'row_type' } },
			{ field: 'Lock', title: '锁定', minWidth: 140, showOverflow: true, slots: { default: 'row_lock' } },
			{ field: 'Group', title: '所在分组', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'LevelName', title: '会员等级', width: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'AccountName', title: '取款姓名', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'TotalMoney', title: '总余额', minWidth: 130, slots: { default: 'row_money' } },
			{ field: 'TotalRechargeMoney', title: '总存款', minWidth: 110, slots: { default: 'row_recharge' } },
			{ field: 'TotalWithdrawMoney', title: '总取款', minWidth: 110, slots: { default: 'row_withdraw' } },
			{ field: 'LoginAt', title: '最后登录时间', minWidth: 160, slots: { default: 'row_loginAt' } },
			{ field: 'LoginIP', title: '最后登录IP', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'RegFlag', title: '开户方式', minWidth: 120, slots: { default: 'row_reg' } },
		],
	},
	{
		proxyConfig: {
			autoLoad: false,
			ajax: {
				query: ({ page }) => handleQueryApi(page),
			},
		},
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<MemberListRow> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

/** 组装旧站同名 Form 参数。 */
const buildForm = (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	const q = state.query;
	return {
		SearchType: q.SearchType || 'UserName',
		User: q.User || '',
		Agent: q.Agent || '',
		BelongUser: state.belongUserChecked ? 1 : 0,
		Type: q.Type || '',
		GroupID: q.GroupID || '',
		Level: q.Level || '',
		RegMethod: q.RegMethod || '',
		TimeType: q.TimeType || 'Reg',
		StartAt: state.timeRange?.[0] || '',
		EndAt: state.timeRange?.[1] || '',
		MinMoney: q.MinMoney || '',
		MaxMoney: q.MaxMoney || '',
		Depth: q.Depth || '',
		Active: q.Active || '',
		Domain: q.Domain || '',
		InviterName: q.InviterName || '',
		RechargeCount: q.RechargeCount || '',
		CommissionID: q.CommissionID || '',
		ReturnID: q.ReturnID || '',
		Currency: q.Currency || '',
		RegCurrency: q.RegCurrency || '',
		IsLimit: state.isLimit,
		PageIndex: page.currentPage,
		PageSize: page.pageSize,
	};
};

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	try {
		const info = await fetchMemberList(buildForm(page));
		await refreshParentTrail();
		// toVxeProxyPage → data.result.items/total；hook 也已兼容 {page,result}
		return toVxeProxyPage(readAdminList<MemberListRow>(info as any), readAdminRecordCount(info as any));
	} catch (e: any) {
		showCaughtError(e, '查询失败');
		return toVxeProxyPage<MemberListRow>([], 0);
	} finally {
		state.isLimit = 1;
	}
};

const refreshParentTrail = async () => {
	const agent = (state.query.Agent || '').trim();
	if (!agent || agent === '0') {
		state.parentTrail = [];
		return;
	}
	try {
		const list = await fetchMemberParentList(agent);
		state.parentTrail = Array.isArray(list) ? list : [];
	} catch {
		state.parentTrail = [];
	}
};

const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.belongUserChecked = false;
	state.timeRange = null;
	state.isLimit = 1;
	state.parentTrail = [];
	handleQuery(true);
};

const searchTopAgent = () => {
	state.query.Agent = '0';
	state.query.Type = 'Agent';
	state.isLimit = 1;
	handleQuery(true);
};

const searchToday = () => {
	const d = new Date();
	const y = d.getFullYear();
	const m = String(d.getMonth() + 1).padStart(2, '0');
	const day = String(d.getDate()).padStart(2, '0');
	state.timeRange = [`${y}-${m}-${day} 00:00:00`, `${y}-${m}-${day} 23:59:59`];
	state.isLimit = 1;
	handleQuery(true);
};

const searchYesterday = () => {
	const d = new Date();
	d.setDate(d.getDate() - 1);
	const y = d.getFullYear();
	const m = String(d.getMonth() + 1).padStart(2, '0');
	const day = String(d.getDate()).padStart(2, '0');
	state.timeRange = [`${y}-${m}-${day} 00:00:00`, `${y}-${m}-${day} 23:59:59`];
	state.isLimit = 1;
	handleQuery(true);
};

const searchAllMembers = () => {
	state.isLimit = 0;
	handleQuery(true);
};

const drillAgent = (userName: string) => {
	Object.assign(state.query, emptyQuery());
	state.belongUserChecked = false;
	state.timeRange = null;
	state.query.Agent = userName || '';
	state.isLimit = 1;
	handleQuery(true);
};

const openDetail = async (row: MemberListRow) => {
	state.detailVisible = true;
	state.detailRow = null;
	try {
		await ensureCurrencyEnum().catch(() => undefined);
		const info = (await adminPost('/admin/user/member/info/detail', { UserID: row.ID })) as Record<string, any>;
		state.detailRow = info && typeof info === 'object' ? info : row;
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

const openMoney = async (row: MemberListRow) => {
	state.moneyVisible = true;
	state.moneyMaster = '';
	state.moneyRows = [];
	try {
		await ensureCurrencyEnum().catch(() => undefined);
		const info = (await fetchMemberDetailMoney(row.ID)) as any;
		const masterRaw = String(info?.Master ?? '');
		if (masterRaw.includes(':')) {
			const [code, amt] = masterRaw.split(':');
			const cur = formatCurrency(code) || code;
			state.moneyMaster = `${cur} ${formatMoney(amt)}`;
			state.moneyRows.push({ currency: `${cur}（主）`, money: formatMoney(amt) });
		} else if (masterRaw) {
			state.moneyMaster = masterRaw;
		}
		let detailObj: Record<string, any> = {};
		const detailRaw = info?.Detail;
		if (typeof detailRaw === 'string' && detailRaw.trim()) {
			try {
				detailObj = JSON.parse(detailRaw);
			} catch {
				detailObj = {};
			}
		} else if (detailRaw && typeof detailRaw === 'object') {
			detailObj = detailRaw;
		}
		for (const [k, v] of Object.entries(detailObj)) {
			state.moneyRows.push({ currency: formatCurrency(k) || String(k), money: formatMoney(v as any) });
		}
		if (!state.moneyRows.length && info?.All && typeof info.All === 'object') {
			for (const [k, v] of Object.entries(info.All as Record<string, any>)) {
				state.moneyRows.push({ currency: formatCurrency(k) || String(k), money: formatMoney(v) });
			}
		}
	} catch (e: any) {
		showCaughtError(e, '加载余额失败');
	}
};

const formatMoney = (n?: number) => {
	if (n == null || Number.isNaN(Number(n))) return '0.00';
	return Number(n).toFixed(2);
};

const formatRegFlag = (flag?: number) => {
	if (flag == null || flag === ('' as any)) return '-';
	const hit = regMethodOptions.find((o) => o.value === String(flag));
	return hit?.label ?? String(flag) || '-';
};

const openAddAgent = () => {
	state.agentVisible = true;
};

const resetAgentForm = () => {
	state.agentForm = { UserName: '', Password: '', Currency: '-1' };
	state.agentSaving = false;
};

const submitAddAgent = async () => {
	if (!state.agentForm.UserName?.trim() || !state.agentForm.Password?.trim()) {
		ElMessage.warning('请填写用户名和密码');
		return;
	}
	state.agentSaving = true;
	try {
		await addTopAgent({
			UserName: state.agentForm.UserName.trim(),
			Password: state.agentForm.Password,
			Currency: state.agentForm.Currency || '-1',
		});
		ElMessage.success('添加成功');
		state.agentVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.agentSaving = false;
	}
};

/** CSV 单元格转义（RFC4180）。 */
const csvCell = (v: unknown) => {
	const s = v == null ? '' : String(v);
	if (/[",\r\n]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
	return s;
};

const handleExport = async () => {
	state.exporting = true;
	try {
		const form = buildForm({ currentPage: 1, pageSize: 5000 } as VxeGridPropTypes.ProxyAjaxQueryPageParams);
		const info = await exportMemberList(form);
		const rows = readAdminList<MemberListRow>(info as any);
		const total = readAdminRecordCount(info as any, rows.length);
		if (!rows.length) {
			ElMessage.warning('没有可导出的数据');
			return;
		}
		const headers = [
			'ID',
			'UserName',
			'AgentName',
			'InviterName',
			'TypeName',
			'Group',
			'LevelName',
			'AccountName',
			'TotalMoney',
			'TotalRechargeMoney',
			'TotalWithdrawMoney',
			'LoginAt',
			'LoginIP',
			'RegFlag',
		];
		const lines = [
			headers.join(','),
			...rows.map((r) =>
				[
					r.ID,
					r.UserName,
					r.AgentName,
					r.InviterName,
					r.TypeName ?? (r.Type === 0 ? '会员' : '代理'),
					r.Group,
					r.LevelName ?? r.Level,
					r.AccountName,
					r.TotalMoney ?? r.Money,
					r.TotalRechargeMoney,
					r.TotalWithdrawMoney,
					r.LoginAt,
					r.LoginIP,
					r.RegFlag,
				]
					.map(csvCell)
					.join(',')
			),
		];
		const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `member-export-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.csv`;
		a.click();
		URL.revokeObjectURL(url);
		ElMessage.success(`已导出 ${rows.length} 行` + (total > rows.length ? `（筛选共 ${total}，上限 5000）` : ''));
	} catch (e: any) {
		showCaughtError(e, '导出失败');
	} finally {
		state.exporting = false;
	}
};

const triggerMarkImport = () => markFileRef.value?.click();

const onMarkFileChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		const rows = await parseAdminImportTable(file, 2);
		const items: Array<{ UserName: string; Content: string }> = [];
		for (const parts of rows) {
			if (isUserNameHeader(parts[0])) continue;
			const userName = parts[0] || '';
			const content = parts[1] || '';
			if (userName && content) items.push({ UserName: userName, Content: content });
		}
		if (!items.length) {
			ElMessage.warning('未解析到有效行（Excel/CSV：用户名,备注）');
			return;
		}
		const msg = await batchAddMemberMarks(items);
		ElMessage.success(typeof msg === 'string' && msg ? msg : `已导入 ${items.length} 行`);
	} catch (e: any) {
		showCaughtError(e);
	}
};

const loadLookups = async () => {
	const [groups, commissions, returns, levels, currencies, siteType] = await Promise.all([
		fetchMemberGroupOptions().catch(() => []),
		fetchCommissionOptions().catch(() => []),
		fetchReturnPlanOptions().catch(() => []),
		fetchVipLevelOptions().catch(() => []),
		fetchCurrencyOptions().catch(() => []),
		fetchSiteType().catch(() => ({})),
	]);
	state.groupOptions = groups;
	state.commissionOptions = commissions;
	state.returnOptions = returns;
	state.levelOptions = levels;
	state.currencyOptions = currencies;
	const typeVal = (siteType as any)?.Type ?? (siteType as any)?.type;
	state.showRegCurrency = String(typeVal) === '1';
};

onMounted(async () => {
	const gid = String(route.query.GroupID ?? route.query.groupID ?? '').trim();
	if (gid) state.query.GroupID = gid;

	void ensureCurrencyEnum().catch(() => undefined);
	await loadLookups();
	if (gid) handleQuery(true);
});
</script>

<style scoped>
.page-header-actions {
	display: flex;
	justify-content: flex-end;
	gap: 8px;
	margin-bottom: 10px;
}
.member-parent-trail {
	margin: 0 0 10px;
	font-size: 13px;
}
.member-parent-trail a {
	color: var(--el-color-primary);
}
.money-master {
	margin: 0 0 10px;
	font-size: 13px;
	color: var(--el-text-color-regular);
}
.money-in { color: #67c23a; }
.money-out { color: #f56c6c; }
.muted { color: var(--el-text-color-secondary); }
</style>
