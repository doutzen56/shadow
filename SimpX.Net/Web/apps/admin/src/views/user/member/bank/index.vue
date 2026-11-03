<template>
	<!-- 规格：Web.Admin/src/views/user/member/bank.html；菜单 /user/member/bank/index -->
	<div class="user-member-bank-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="时间">
					<el-date-picker
						v-model="state.timeRange"
						type="datetimerange"
						value-format="YYYY-MM-DD HH:mm:ss"
						range-separator="-"
						start-placeholder="开始"
						end-placeholder="结束"
						style="width: 340px"
					/>
				</el-form-item>
				<el-form-item label="用户名称">
					<el-input v-model="state.query.UserName" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="银行号码">
					<el-input v-model="state.query.UserNumber" clearable style="width: 180px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="待审" value="None" />
						<el-option label="失败" value="Faild" />
						<el-option label="通过" value="Success" />
					</el-select>
				</el-form-item>
				<el-form-item label="类型">
					<el-radio-group v-model="state.query.IsVir" @change="() => handleQuery(true)">
						<el-radio-button value="">全部</el-radio-button>
						<el-radio-button value="0">银行</el-radio-button>
						<el-radio-button value="1">虚拟币</el-radio-button>
						<el-radio-button value="2">电子钱包</el-radio-button>
						<el-radio-button value="4">超商</el-radio-button>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:member:bank'" @click="handleQuery(true)">查询</el-button>
					<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				</el-button-group>
				<el-button type="success" plain v-auth="'site:user:member:bank'" @click="openCreate">添加银行信息</el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #toolbar_buttons />
					<template #toolbar_tools />
					<template #empty>
						<el-empty :image-size="160" description="暂无数据" />
					</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="formatCheckStatus(row.Status) === '通过' ? 'success' : formatCheckStatus(row.Status) === '失败' ? 'danger' : formatCheckStatus(row.Status) === '待审' ? 'warning' : 'info'"
						>
							{{ formatCheckStatus(row.Status) }}
						</el-tag>
					</template>
					<template #row_bank="{ row }">
						{{ formatBankTypeLabel(row) }}
					</template>
					<template #row_vir="{ row }">
						<el-tag size="small" type="info">{{ formatIsVir(row.IsVir) }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<template v-if="isBankPending(row)">
							<el-button text type="danger" @click="onDelete(row)">删除</el-button>
							<el-button text type="primary" @click="openAudit(row)">审核</el-button>
						</template>
						<template v-else>
							<el-button text type="primary" @click="openAudit(row)">详细</el-button>
							<el-button text type="warning" @click="onReset(row)">撤回审核</el-button>
						</template>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.auditVisible" :title="state.auditPending ? '审核银行信息' : '银行信息'" width="520px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.audit" label-width="100px">
				<el-form-item label="开户名"><el-input :model-value="state.audit.Name" readonly /></el-form-item>
				<el-form-item label="开户银行"><el-input :model-value="state.audit.BankName" readonly /></el-form-item>
				<el-form-item label="银行号码"><el-input :model-value="state.audit.Account" readonly /></el-form-item>
				<el-form-item label="开户地址"><el-input :model-value="state.audit.AccountDetail" readonly /></el-form-item>
				<el-form-item v-if="state.audit.BankInfoImg" label="凭证">
					<el-image :src="state.audit.BankInfoImg" style="max-width: 200px; max-height: 120px" fit="contain" :preview-src-list="[state.audit.BankInfoImg]" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.audit.Remark" :disabled="!state.auditPending && Number(state.audit.Status) !== 0" />
				</el-form-item>
				<el-form-item v-if="state.auditPending" label="审核">
					<el-radio-group v-model="state.audit.StatusChoice">
						<el-radio value="Success">同意</el-radio>
						<el-radio value="Faild">拒绝</el-radio>
					</el-radio-group>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.auditVisible = false">关闭</el-button>
				<el-button v-if="state.auditPending" type="primary" :loading="state.saving" @click="onAuditSave">保存</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.createVisible" title="添加银行信息" width="640px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.create" label-width="100px">
				<el-form-item label="用户名称" required>
					<div style="display: flex; gap: 8px; width: 100%">
						<el-input v-model="state.create.UserName" />
						<el-button :loading="state.verifying" @click="onVerifyUser">查询用户</el-button>
					</div>
				</el-form-item>
				<el-tabs v-model="state.createTab" @tab-change="onCreateTabChange">
					<el-tab-pane label="银行" name="0">
						<el-form-item label="开户名" required>
							<el-input v-model="state.create.Name" />
						</el-form-item>
						<el-form-item label="开户银行" required>
							<el-select v-model="state.create.Type" filterable placeholder="选择银行" style="width: 100%">
								<el-option v-for="b in state.bankTypes" :key="String(b.Type)" :label="b.Bank" :value="Number(b.Type)" />
							</el-select>
						</el-form-item>
						<el-form-item label="银行号码" required>
							<el-input v-model="state.create.Account" />
						</el-form-item>
						<el-form-item label="开户地址">
							<el-input v-model="state.create.BankAddress" />
						</el-form-item>
						<el-form-item label="银行编码">
							<el-input v-model="state.create.BranchCode" />
						</el-form-item>
						<el-form-item label="凭证">
							<div class="bank-upload-row">
								<el-input v-model="state.create.BankInfo" placeholder="上传后自动填入路径" readonly />
								<el-upload :show-file-list="false" accept="image/*,video/*" :http-request="onUploadBankInfo">
									<el-button :loading="state.uploading">上传</el-button>
								</el-upload>
							</div>
							<el-image
								v-if="state.create.BankInfoPreview"
								:src="state.create.BankInfoPreview"
								style="max-width: 160px; max-height: 100px; margin-top: 8px"
								fit="contain"
								:preview-src-list="[state.create.BankInfoPreview]"
							/>
						</el-form-item>
					</el-tab-pane>
					<el-tab-pane label="虚拟钱包" name="1">
						<el-form-item label="币种" required>
							<el-select v-model="state.create.VCurrency" filterable style="width: 100%">
								<el-option v-for="b in state.virTypes" :key="String(b.Type)" :label="b.Bank" :value="Number(b.Type)" />
							</el-select>
						</el-form-item>
						<el-form-item v-if="Number(state.create.VCurrency) === 250" label="协议">
							<el-select v-model="state.create.VAgreement" style="width: 100%">
								<el-option label="TRC20" value="TRC20" />
								<el-option label="ERC20" value="ERC20" />
							</el-select>
						</el-form-item>
						<el-form-item label="地址" required>
							<el-input v-model="state.create.VAddress" />
						</el-form-item>
					</el-tab-pane>
					<el-tab-pane label="电子钱包" name="2">
						<el-form-item label="国家/地区">
							<el-select v-model="state.create.EArea" filterable clearable placeholder="选择国家/地区" style="width: 100%" @change="onEAreaChange">
								<el-option v-for="c in state.eAreas" :key="c" :label="c" :value="c" />
							</el-select>
						</el-form-item>
						<el-form-item label="类型" required>
							<el-select v-model="state.create.EType" filterable placeholder="请选择电子钱包" style="width: 100%">
								<el-option v-for="b in state.eTypes" :key="String(b.Type)" :label="b.Bank" :value="Number(b.Type)" />
							</el-select>
						</el-form-item>
						<el-form-item label="姓名" required>
							<el-input v-model="state.create.EUserName" />
						</el-form-item>
						<el-form-item label="账号" required>
							<el-input v-model="state.create.EAccount" />
						</el-form-item>
					</el-tab-pane>
					<el-tab-pane label="超商" name="4">
						<el-form-item label="超商" required>
							<el-select v-model="state.create.CType" filterable placeholder="请选择" style="width: 100%" @change="onCvsTypeChange">
								<el-option v-for="b in state.cvsTypes" :key="String(b.Type)" :label="b.Bank" :value="Number(b.Type)" />
							</el-select>
						</el-form-item>
						<el-form-item label="县市" required>
							<el-select v-model="state.create.CCity" filterable style="width: 100%" @change="onCvsCityChange">
								<el-option v-for="b in state.cvsCities" :key="b.Bank" :label="b.Bank" :value="b.Bank" />
							</el-select>
						</el-form-item>
						<el-form-item label="地区" required>
							<el-select v-model="state.create.CDistrict" filterable style="width: 100%" @change="onCvsDistrictChange">
								<el-option v-for="b in state.cvsDistricts" :key="b.Bank" :label="b.Bank" :value="b.Bank" />
							</el-select>
						</el-form-item>
						<el-form-item label="门店" required>
							<el-select v-model="state.create.CBankAddress" filterable style="width: 100%">
								<el-option v-for="b in state.cvsStores" :key="String(b.Type)" :label="b.Bank" :value="Number(b.Type)" />
							</el-select>
						</el-form-item>
					</el-tab-pane>
				</el-tabs>
			</el-form>
			<template #footer>
				<el-button @click="state.createVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" :disabled="!state.create.ID" @click="onCreateSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberBank">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { UploadRequestOptions } from 'element-plus';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import {
	createMemberBank,
	deleteMemberBank,
	fetchMemberBankAdressList,
	fetchMemberBankCityList,
	fetchMemberBankCountryInfo,
	fetchMemberBankCountryList,
	fetchMemberBankDistrictList,
	fetchMemberBankInfo,
	fetchMemberBankList,
	resetMemberBank,
	unwrapBankTypeOptions,
	updateMemberBankInfo,
	uploadMemberBankInfo,
	verifyMemberBankUser,
	type MemberBankRow,
} from '/@/api/user/member-bank';
import { formatDateTime } from '/@/utils/format';
import { ensureCurrencyEnum, formatCurrency, formatMoneyCheckStatus } from '/@/utils/admin-display';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberBank', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const emptyQuery = () => ({
	UserName: '',
	UserNumber: '',
	Status: 'None',
	IsVir: '',
	Currency: '',
});

type BankOpt = { Type: number; Bank: string };

const state = reactive({
	query: emptyQuery(),
	timeRange: null as [string, string] | null,
	bankTypes: [] as BankOpt[],
	virTypes: [] as BankOpt[],
	eTypes: [] as BankOpt[],
	eAreas: [] as string[],
	eAreaBankMap: {} as Record<string, BankOpt[]>,
	cvsTypes: [] as BankOpt[],
	cvsCities: [] as BankOpt[],
	cvsDistricts: [] as BankOpt[],
	cvsStores: [] as BankOpt[],
	auditVisible: false,
	auditPending: false,
	createVisible: false,
	createTab: '0',
	saving: false,
	verifying: false,
	uploading: false,
	audit: {
		ID: 0,
		Name: '',
		BankName: '',
		Account: '',
		AccountDetail: '',
		BankInfoImg: '',
		Remark: '',
		Status: 0 as number,
		StatusChoice: 'Faild' as string,
	},
	create: {
		ID: 0,
		UserName: '',
		Name: '',
		Account: '',
		Type: 0,
		BankAddress: '',
		BranchCode: '',
		BankInfo: '',
		BankInfoPreview: '',
		IsVir: '0',
		VCurrency: 250,
		VAgreement: 'TRC20',
		VAddress: '',
		EArea: '',
		EType: 0,
		EUserName: '',
		EAccount: '',
		CType: 0,
		CCity: '',
		CDistrict: '',
		CBankAddress: 0,
	},
	autoRefresh: false,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	localPageParam,
});


const pad = (n: number) => String(n).padStart(2, '0');
const rangeDays = (days: number): [string, string] => {
	const end = new Date();
	end.setHours(23, 59, 59, 0);
	const start = new Date();
	start.setHours(0, 0, 0, 0);
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	const f = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
	return [f(start), f(end)];
};
const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};

const options = useVxeTable<MemberBankRow>(
	{
		id: 'siteUserMemberBank',
		name: '银行信息',
		columns: [
			{ field: 'ID', title: '序号', width: 80 },
			{ field: 'UserName', title: '用户名称', minWidth: 110, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Name', title: '开户名', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Currency', title: '币种', width: 80, formatter: ({ cellValue }) => formatCurrency(cellValue) || String(cellValue ?? '-') },
			{ field: 'AccountDetail', title: '开户地址', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Account', title: '帐号', minWidth: 150, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Checker', title: '审核人', width: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CheckDateTime', title: '审核时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'CreateAt', title: '添加时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'BankName', title: '银行/虚拟/电子', minWidth: 130, slots: { default: 'row_bank' } },
			{ field: 'IsVir', title: '类型', width: 100, slots: { default: 'row_vir' } },
			{ field: 'Remark', title: '备注', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: {
			autoLoad: true,
			ajax: {
				query: ({ page }) => handleQueryApi(page),
			},
		},
		pagerConfig: { pageSize: localPageParam.pageSize },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<MemberBankRow> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

/**
 * 审核状态文案。
 * @param status 状态
 */
const formatCheckStatus = (status?: number | string) => formatMoneyCheckStatus(status);
const isBankPending = (row: Record<string, any>) => {
	const s = String(row.Status ?? '');
	return s === '0' || s === 'None' || Number(row.Status) === 0 || formatCheckStatus(s) === '待审';
};
const formatIsVir = (v: unknown) => {
	const s = String(v ?? '');
	if (s === '0') return '银行';
	if (s === '1') return '虚拟币';
	if (s === '2') return '电子钱包';
	if (s === '4') return '超商';
	return s || '-';
};
const formatBankTypeLabel = (row: Record<string, any>) => {
	const v = String(row.IsVir ?? '');
	if (v === '0') return row.BankName || '银行';
	if (v === '1') return row.Type || row.BankName || '虚拟币';
	if (v === '2') return row.Type || row.BankName || '电子钱包';
	if (v === '4') return row.Type || row.BankName || '超商';
	return row.BankName || row.Type || '-';
};

/**
 * 查询。
 * @param page 分页
 */
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchMemberBankList({
			UserName: state.query.UserName || '',
			UserNumber: state.query.UserNumber || '',
			Status: state.query.Status || '',
			IsVir: state.query.IsVir || '',
			Currency: state.query.Currency || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);

/** 归一 bankcountryinfo.list[].bank 字典为下拉。 */
const mapBankDict = (info: unknown): BankOpt[] => {
	const list = (info as any)?.list;
	const first = Array.isArray(list) ? list[0] : null;
	const bank = first?.bank;
	if (!bank || typeof bank !== 'object') return [];
	return Object.entries(bank as Record<string, string>).map(([k, v]) => ({ Type: Number(k), Bank: String(v) }));
};

const loadBankTypes = async () => {
	try {
		const info = await fetchMemberBankCountryList();
		state.bankTypes = unwrapBankTypeOptions(info).filter((x) => x.Bank);
	} catch (e: any) {
		state.bankTypes = [];
		showCaughtError(e);
	}
};

/** 虚拟币种 / 电子钱包 / 超商类型。 */
const loadVirOrCvsTypes = async (isVir: string) => {
	try {
		const info = await fetchMemberBankCountryInfo(isVir);
		const opts = mapBankDict(info);
		if (isVir === '1') {
			state.virTypes = opts.length ? opts : state.bankTypes.filter((x) => x.Type >= 250 && x.Type <= 299);
			if (!state.create.VCurrency && state.virTypes[0]) state.create.VCurrency = state.virTypes[0].Type;
		} else if (isVir === '2') {
			const rows = Array.isArray((info as any)?.list) ? (info as any).list : [];
			const areaMap: Record<string, BankOpt[]> = {};
			for (const row of rows) {
				const country = String(row?.country || '默认').trim() || '默认';
				const bank = row?.bank && typeof row.bank === 'object' ? row.bank : {};
				areaMap[country] = Object.entries(bank as Record<string, string>).map(([k, v]) => ({
					Type: Number(k),
					Bank: String(v),
				}));
			}
			if (!Object.keys(areaMap).length) {
				const fallback = opts.length
					? opts
					: state.bankTypes.filter((x) => x.Type === 200 || x.Type === 7007 || x.Type === 8061);
				areaMap['默认'] = fallback;
			}
			state.eAreaBankMap = areaMap;
			state.eAreas = Object.keys(areaMap);
			if (!state.create.EArea || !areaMap[state.create.EArea]) state.create.EArea = state.eAreas[0] || '默认';
			state.eTypes = areaMap[state.create.EArea] || [];
			if (!state.create.EType && state.eTypes[0]) state.create.EType = state.eTypes[0].Type;
		} else if (isVir === '4') {
			state.cvsTypes = opts;
			if (!state.create.CType && state.cvsTypes[0]) state.create.CType = state.cvsTypes[0].Type;
		}
	} catch (e: any) {
		showCaughtError(e);
	}
};

/** 重置 */
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.timeRange = null;
	handleQuery(true);
};

/**
 * 删除待审。
 * @param row 行
 */
const onDelete = async (row: MemberBankRow) => {
	try {
		await ElMessageBox.confirm('确认要删除吗？', '提示', { type: 'warning' });
		await deleteMemberBank(row.ID);
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/**
 * 撤回审核（回到待审）。
 * @param row 行
 */
const onReset = async (row: MemberBankRow) => {
	try {
		await ElMessageBox.confirm('确认撤回审核，恢复为待审？', '提示', { type: 'warning' });
		await resetMemberBank(row.ID);
		ElMessage.success('已撤回');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/**
 * 打开审核/详情。
 * @param row 行
 */
const openAudit = async (row: MemberBankRow) => {
	state.auditVisible = true;
	state.auditPending = isBankPending(row);
	Object.assign(state.audit, {
		ID: row.ID,
		Name: row.Name || '',
		BankName: row.BankName || '',
		Account: row.Account || '',
		AccountDetail: row.AccountDetail || '',
		BankInfoImg: '',
		Remark: row.Remark || '',
		Status: Number(row.Status ?? 0),
		StatusChoice: 'Faild',
	});
	try {
		const info = await fetchMemberBankInfo(row.ID);
		Object.assign(state.audit, {
			ID: info?.ID ?? row.ID,
			Name: info?.Name ?? row.Name ?? '',
			BankName: info?.BankName ?? row.BankName ?? '',
			Account: info?.Account ?? row.Account ?? '',
			AccountDetail: info?.AccountDetail ?? row.AccountDetail ?? '',
			BankInfoImg: (info as any)?.BankInfoImg || '',
			Remark: info?.Remark ?? row.Remark ?? '',
			Status: Number(info?.Status ?? row.Status ?? 0),
		});
		state.auditPending = isBankPending(state.audit);
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

/** 提交审核 */
const onAuditSave = async () => {
	state.saving = true;
	try {
		await updateMemberBankInfo({
			ID: state.audit.ID,
			Status: state.audit.StatusChoice,
			Remark: state.audit.Remark || '',
		});
		ElMessage.success('已保存');
		state.auditVisible = false;
		handleQuery(false);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};

/** 打开添加 */
const openCreate = () => {
	Object.assign(state.create, {
		ID: 0,
		UserName: '',
		Name: '',
		Account: '',
		Type: state.bankTypes[0]?.Type ?? 0,
		BankAddress: '',
		BranchCode: '',
		BankInfo: '',
		BankInfoPreview: '',
		IsVir: '0',
		VCurrency: 250,
		VAgreement: 'TRC20',
		VAddress: '',
		EArea: '',
		EType: 0,
		EUserName: '',
		EAccount: '',
		CType: 0,
		CCity: '',
		CDistrict: '',
		CBankAddress: 0,
	});
	state.createTab = '0';
	state.cvsCities = [];
	state.cvsDistricts = [];
	state.cvsStores = [];
	state.createVisible = true;
	if (!state.bankTypes.length) void loadBankTypes();
};

/**
 * 添加弹层 Tab → IsVir（超商旧站 index3→4）。
 * @param name tab name
 */
const onCreateTabChange = (name: string | number) => {
	const tab = String(name);
	state.createTab = tab;
	state.create.IsVir = tab;
	if (tab === '1') void loadVirOrCvsTypes('1');
	if (tab === '2') void loadVirOrCvsTypes('2');
	if (tab === '4') void loadVirOrCvsTypes('4');
};

/** 电子钱包国家切换 → 类型下拉。 */
const onEAreaChange = () => {
	const area = state.create.EArea || state.eAreas[0] || '默认';
	state.eTypes = state.eAreaBankMap[area] || [];
	state.create.EType = state.eTypes[0]?.Type ?? 0;
};

/** 凭证上传 */
const onUploadBankInfo = async (opt: UploadRequestOptions) => {
	const file = opt.file as File;
	state.uploading = true;
	try {
		const key = await uploadMemberBankInfo(file);
		state.create.BankInfo = key;
		state.create.BankInfoPreview = URL.createObjectURL(file);
		opt.onSuccess?.(key as any);
		ElMessage.success('已上传');
	} catch (e: any) {
		opt.onError?.(e as any);
		showCaughtError(e);
	} finally {
		state.uploading = false;
	}
};

/** 超商类型 → 城市 */
const onCvsTypeChange = async () => {
	state.create.CCity = '';
	state.create.CDistrict = '';
	state.create.CBankAddress = 0;
	state.cvsCities = [];
	state.cvsDistricts = [];
	state.cvsStores = [];
	if (!state.create.CType) return;
	try {
		state.cvsCities = unwrapBankTypeOptions(await fetchMemberBankCityList(state.create.CType));
	} catch (e: any) {
		showCaughtError(e);
	}
};

/** 城市 → 区县 */
const onCvsCityChange = async () => {
	state.create.CDistrict = '';
	state.create.CBankAddress = 0;
	state.cvsDistricts = [];
	state.cvsStores = [];
	if (!state.create.CType || !state.create.CCity) return;
	try {
		state.cvsDistricts = unwrapBankTypeOptions(await fetchMemberBankDistrictList(state.create.CType, state.create.CCity));
	} catch (e: any) {
		showCaughtError(e);
	}
};

/** 区县 → 门店 */
const onCvsDistrictChange = async () => {
	state.create.CBankAddress = 0;
	state.cvsStores = [];
	if (!state.create.CType || !state.create.CCity || !state.create.CDistrict) return;
	try {
		state.cvsStores = unwrapBankTypeOptions(
			await fetchMemberBankAdressList(state.create.CType, state.create.CCity, state.create.CDistrict)
		);
	} catch (e: any) {
		showCaughtError(e);
	}
};

/** 查询会员 */
const onVerifyUser = async () => {
	if (!state.create.UserName?.trim()) {
		ElMessage.warning('请输入用户名称');
		return;
	}
	state.verifying = true;
	try {
		const info = await verifyMemberBankUser(state.create.UserName.trim());
		state.create.ID = Number((info as any)?.ID ?? (info as any)?.Id ?? 0);
		if (!state.create.ID) {
			ElMessage.error('用户不存在');
			return;
		}
		ElMessage.success(`已找到用户 #${state.create.ID}`);
	} catch (e: any) {
		state.create.ID = 0;
		showCaughtError(e);
	} finally {
		state.verifying = false;
	}
};

/** 创建（按 Tab/IsVir 组包，对齐旧 createbank）。 */
const onCreateSave = async () => {
	if (!state.create.ID) {
		ElMessage.warning('请先查询用户');
		return;
	}
	const isVir = state.createTab || '0';
	if (isVir === '0') {
		if (!state.create.Name?.trim() || !state.create.Account?.trim()) {
			ElMessage.warning('请填写开户名和银行号码');
			return;
		}
	} else if (isVir === '1') {
		if (!state.create.VCurrency || !state.create.VAddress?.trim()) {
			ElMessage.warning('请填写虚拟币种与地址');
			return;
		}
	} else if (isVir === '2') {
		if (!state.create.EType || !state.create.EUserName?.trim() || !state.create.EAccount?.trim()) {
			ElMessage.warning('请填写电子钱包类型、姓名与账号');
			return;
		}
	} else if (isVir === '4') {
		if (!state.create.CType || !state.create.CCity || !state.create.CDistrict || !state.create.CBankAddress) {
			ElMessage.warning('请选择超商、县市、地区与门店');
			return;
		}
	}
	const storeLabel = state.cvsStores.find((x) => x.Type === state.create.CBankAddress)?.Bank || '';
	state.saving = true;
	try {
		await createMemberBank({
			ID: state.create.ID,
			UserName: state.create.UserName.trim(),
			IsVir: isVir,
			Type: state.create.Type,
			Name: state.create.Name.trim(),
			Account: state.create.Account.trim(),
			BankAddress: isVir === '4' ? storeLabel : state.create.BankAddress || '',
			BranchCode: state.create.BranchCode || '',
			BankInfo: state.create.BankInfo || '',
			VCurrency: state.create.VCurrency,
			VAgreement: state.create.VAgreement || '',
			VAddress: state.create.VAddress || '',
			EType: state.create.EType,
			EUserName: state.create.EUserName || '',
			EAccount: state.create.EAccount || '',
			EArea: state.create.EArea || '',
			CType: state.create.CType,
			CCity: state.create.CCity || '',
			CDistrict: state.create.CDistrict || '',
			CBankAddress: state.create.CBankAddress,
		});
		ElMessage.success('已添加');
		state.createVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};

let refreshTimer: ReturnType<typeof setInterval> | null = null;
const onAutoRefresh = (on: boolean) => {
	if (refreshTimer) {
		clearInterval(refreshTimer);
		refreshTimer = null;
	}
	if (on) refreshTimer = setInterval(() => handleQuery(false), 60 * 1000);
};

const loadCurrencyOptions = async () => {
	try {
		await ensureCurrencyEnum().catch(() => undefined);
		const list = await fetchCurrencyOptions();
		state.currencyOptions = (list || []).map((x: any) => ({
			key: x.key ?? x.Key ?? x.value ?? x.Value,
			value: String(x.value ?? x.Value ?? x.key ?? x.Key ?? ''),
		}));
	} catch (e: any) {
		state.currencyOptions = [];
		showCaughtError(e, '加载币种失败');
	}
};

onMounted(() => {
	void loadCurrencyOptions();
	void loadBankTypes();
});
onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped>
.bank-upload-row {
	display: flex;
	gap: 8px;
	width: 100%;
}
</style>
