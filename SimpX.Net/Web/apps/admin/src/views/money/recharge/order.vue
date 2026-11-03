<template>
	<!--
		规格：Web.Admin/src/views/money/recharge/order.html + orderinfo.html + order-create.html
		接口：orderlist / orderinfo / checkorder / deleteorder / money/recharge/create / settinglist
	-->
	<div class="money-recharge-order-page admin-page">
		<div class="page-header-actions">
			<span class="page-title">充值订单</span>
			<div class="header-btns">
				<el-button type="primary" plain v-auth="'site:money:chongzhiguanli:chongzhidingdan'" @click="state.statsTip = true">渠道统计</el-button>
				<el-button type="warning" v-auth="'site:money:chongzhiguanli:chongzhidingdan'" @click="openCreate">代客充值</el-button>
			</div>
		</div>

		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" class="query-form" @submit.prevent>
				<el-form-item label="自动刷新">
					<el-switch v-model="state.autoRefresh" @change="onAutoRefresh" />
				</el-form-item>
				<el-form-item label="关键字">
					<el-input v-model="state.query.KeyWord" clearable placeholder="订单号/网关单号" style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="订单号">
					<el-input v-model="state.query.OrderID" clearable style="width: 160px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="上级代理">
					<el-input v-model="state.query.Agent" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="用户">
					<el-input v-model="state.query.User" clearable style="width: 120px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="充值渠道">
					<el-select v-model="state.query.PayID" clearable filterable placeholder="全部" style="width: 160px">
						<el-option v-for="p in state.payOptions" :key="String(p.ID)" :label="String(p.Name ?? p.ID)" :value="String(p.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="未到账" value="0" />
						<el-option label="已到帐" value="1" />
					</el-select>
				</el-form-item>
				<el-form-item label="币种">
					<el-select v-model="state.query.Currency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="入款币种">
					<el-select v-model="state.query.FromCurrency" clearable filterable placeholder="全部" style="width: 120px">
						<el-option v-for="c in state.currencyOptions" :key="'from-' + String(c.key)" :label="c.value" :value="String(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="代客充值">
					<el-select v-model="state.query.IsProxyRecharge" clearable placeholder="全部" style="width: 100px">
						<el-option label="是" value="1" />
						<el-option label="否" value="0" />
					</el-select>
				</el-form-item>
				<el-form-item label="时间类型">
					<el-select v-model="state.query.DateType" style="width: 120px">
						<el-option label="申请时间" value="CreateAt" />
						<el-option label="到帐时间" value="PayAt" />
					</el-select>
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
				<el-form-item label="金额">
					<el-input v-model="state.query.MoneyFrom" clearable placeholder="起" style="width: 90px" />
					<span style="margin: 0 4px">-</span>
					<el-input v-model="state.query.MoneyTo" clearable placeholder="止" style="width: 90px" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button @click="setRangeDays(30)">近30天</el-button>
				<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:money:chongzhiguanli:chongzhidingdan'" @click="handleQuery(true)">查询</el-button>
				<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				<el-button type="success" plain :loading="state.exporting" v-auth="'site:money:chongzhiguanli:chongzhidingdan'" @click="handleExport">导出(CSV)</el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style vxe-multiline vxe-row-4line" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="140" description="暂无数据（可点「近7天」再查）" />
					</template>
					<template #row_user="{ row }">
						<div class="cell-stack">
							<a href="javascript:;" class="link" @click="openDetail(row)">{{ row.UserName }}</a>
							<span class="muted">#{{ row.UserID }}</span>
						</div>
					</template>
					<template #row_pay="{ row }">
						<div class="cell-stack">
							<span>类型：{{ row.ThirdParty || '-' }}</span>
							<span>渠道：{{ row.Payment || row.PayID || '-' }}</span>
						</div>
					</template>
					<template #row_money="{ row }">
						<div class="cell-stack cell-stack--money">
							<span class="money-in">订单：{{ formatMoney(row.Money) }}</span>
							<span class="fee">手续费：{{ formatMoney(row.Fee) }}</span>
							<span class="fee">行政费：{{ formatMoney(row.Cost) }}</span>
							<span class="real">实充：{{ formatRealPaid(row) }}</span>
						</div>
					</template>
					<template #row_time="{ row }">
						<div class="cell-stack">
							<span class="muted">{{ formatCurrency(row.Currency) }}</span>
							<span>{{ formatDateTime(row.CreateAt) }}</span>
						</div>
					</template>
					<template #row_arrive="{ row }">
						<div class="cell-stack" :title="row.IsPay && row.Count ? '已到帐后的数字为该用户第多少次充值' : ''">
							<span>
								<span :class="row.IsPay ? 'ok' : 'no'">{{ row.IsPay ? '是' : '否' }}</span>
								<span v-if="row.IsPay && formatRechargeCount(row.Count)" class="muted"> / {{ formatRechargeCount(row.Count) }}</span>
							</span>
							<span v-if="row.IsPay && isValidPayAt(row.PayAt)" class="muted">{{ formatDateTime(row.PayAt) }}</span>
						</div>
					</template>
					<template #row_deal="{ row }">
						<span>{{ formatDealTime(row.DealTime) }}</span>
					</template>
					<template #row_proxy="{ row }">
						<el-tag size="small" :type="row.IsProxyRecharge ? 'warning' : 'info'">{{ row.IsProxyRecharge ? '是' : '否' }}</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-button type="primary" link @click="openDetail(row)">详情</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<!-- 详情：对齐 orderinfo.html，未到账可确认入账/删除 -->
		<el-drawer v-model="state.detailVisible" title="充值订单详情" size="640px" destroy-on-close>
			<template v-if="state.detail">
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="订单编号">{{ state.detail.ID || state.detail.OrderID }}</el-descriptions-item>
					<el-descriptions-item label="当前状态">
						<span :class="state.detail.IsPay ? 'ok' : 'no'">{{ state.detail.IsPay ? '已到帐' : '未到账' }}</span>
					</el-descriptions-item>
					<el-descriptions-item label="充值会员">{{ state.detail.UserName }}（{{ state.detail.UserID }}）</el-descriptions-item>
					<el-descriptions-item label="充值渠道">{{ state.detail.Payment || state.detail.PayID }}</el-descriptions-item>
					<el-descriptions-item label="提交时间">{{ formatDateTime(state.detail.CreateAt) }}</el-descriptions-item>
					<el-descriptions-item label="到帐时间">{{ isValidPayAt(state.detail.PayAt) ? formatDateTime(state.detail.PayAt) : '-' }}</el-descriptions-item>
					<el-descriptions-item label="充值金额"><strong class="money-em">{{ formatMoney(state.detail.Money) }}</strong></el-descriptions-item>
					<el-descriptions-item label="手续费">{{ formatMoney(state.detail.Fee) }}</el-descriptions-item>
					<el-descriptions-item label="行政费用">{{ formatMoney(state.detail.Cost) }}</el-descriptions-item>
					<el-descriptions-item label="实际到帐"><span class="money-in">{{ formatMoney(state.detail.Paid) }}</span></el-descriptions-item>
					<el-descriptions-item label="网关编号">{{ state.detail.SystemID || '-' }}</el-descriptions-item>
					<el-descriptions-item label="币种">{{ formatCurrency(state.detail.Currency) }}</el-descriptions-item>
					<el-descriptions-item label="实充折算" v-if="state.detail.RealPaid">{{ formatRealPaidDisplay(state.detail.RealPaid, state.detail.Paid, state.detail.Currency) }}</el-descriptions-item>
					<el-descriptions-item label="备注" :span="state.detail.RealPaid ? 1 : 2">{{ formatRechargeRemark(state.detail.Remark) || '-' }}</el-descriptions-item>
				</el-descriptions>

				<template v-if="!state.detail.IsPay && !state.detail.IsDeleted">
					<el-divider content-position="left">入账操作</el-divider>
					<el-form :model="state.checkForm" label-width="96px" class="check-form">
						<el-form-item label="网关编号">
							<el-input v-model="state.checkForm.SystemID" clearable placeholder="双击可填随机码" @dblclick="fillGuid" />
						</el-form-item>
						<el-form-item label="实际到帐">
							<el-input v-model="state.checkForm.Paid" style="width: 140px" />
						</el-form-item>
						<el-form-item label="手续费">
							<el-input v-model="state.checkForm.Fee" style="width: 140px" />
						</el-form-item>
						<el-form-item label="行政费">
							<el-input v-model="state.checkForm.Cost" style="width: 140px" />
						</el-form-item>
						<el-form-item>
							<el-button type="primary" :loading="state.checking" @click="submitCheck">确认入账</el-button>
							<el-button type="danger" :loading="state.deleting" @click="submitDelete">删除订单</el-button>
						</el-form-item>
					</el-form>
				</template>
			</template>
			<el-empty v-else description="加载中或无数据" />
		</el-drawer>

		<!-- 代客充值：对齐 order-create.html -->
		<el-dialog v-model="state.createVisible" title="代客充值" width="520px" destroy-on-close>
			<p class="tip">提交之后将生成一笔未到账记录，需再次审核后才会到达会员账户。</p>
			<el-form :model="state.createForm" label-width="96px">
				<el-form-item label="用户名" required>
					<el-input v-model="state.createForm.UserName" clearable />
				</el-form-item>
				<el-form-item label="充值渠道" required>
					<el-select v-model="state.createForm.PayID" filterable clearable placeholder="请选择" style="width: 100%">
						<el-option v-for="p in state.payOptions" :key="'c-' + String(p.ID)" :label="String(p.Name ?? p.ID)" :value="String(p.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="充值金额" required>
					<el-input v-model="state.createForm.Money" clearable />
				</el-form-item>
				<el-form-item label="前端备注" required>
					<el-input v-model="state.createForm.Description" clearable />
				</el-form-item>
				<el-form-item label="后端备注" required>
					<el-input v-model="state.createForm.DescriptionA" clearable />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.createVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.creating" @click="submitCreate">确认提交</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.statsTip" title="渠道统计" width="420px">
			<p>旧站弹层为图表页 <code>money/recharge/report-payid</code>，本波先占位；主列表与入账操作已对齐。</p>
			<template #footer>
				<el-button type="primary" @click="state.statsTip = false">知道了</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteMoneyRechargeOrder">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import {
	checkRechargeOrder,
	createProxyRecharge,
	deleteRechargeOrder,
	fetchRechargeOrderInfo,
	fetchRechargeOrderList,
	fetchRechargePayOptions,
} from '/@/api/money/recharge';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { readAdminList, readAdminRecordCount } from '/@/utils/admin-api';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { ensureCurrencyEnum, formatCurrency, formatMoney2, formatRealPaidDisplay, formatRechargeCount, formatRechargeRemark } from '/@/utils/admin-display';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteMoneyRechargeOrder', {
	pageSize: 20 as number,
	defaultSort: { field: 'CreateAt', order: 'desc', descStr: 'desc' },
});

const pad = (n: number) => String(n).padStart(2, '0');
const fmt = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
const dayStart = (d: Date) => {
	const x = new Date(d);
	x.setHours(0, 0, 0, 0);
	return x;
};
const dayEnd = (d: Date) => {
	const x = new Date(d);
	x.setHours(23, 59, 59, 0);
	return x;
};
const rangeDays = (days: number): [string, string] => {
	const end = dayEnd(new Date());
	const start = dayStart(new Date());
	if (days > 0) start.setDate(start.getDate() - (days - 1));
	return [fmt(start), fmt(end)];
};

const emptyQuery = () => ({
	KeyWord: '',
	OrderID: '',
	Agent: '',
	User: '',
	PayID: '',
	Status: '' as string,
	Currency: '' as string,
	FromCurrency: '' as string,
	DateType: 'CreateAt' as string,
	IsProxyRecharge: '' as string,
	MoneyFrom: '',
	MoneyTo: '',
});

const state = reactive({
	query: emptyQuery(),
	timeRange: rangeDays(7) as [string, string] | null,
	payOptions: [] as Array<{ ID?: number | string; Name?: string }>,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
	detailVisible: false,
	detail: null as Row | null,
	checkForm: { SystemID: '', Paid: '', Fee: '', Cost: '' },
	checking: false,
	deleting: false,
	createVisible: false,
	createForm: { UserName: '', PayID: '', Money: '', Description: '', DescriptionA: '' },
	creating: false,
	exporting: false,
	statsTip: false,
	autoRefresh: false,
	localPageParam,
});

const formatMoney = formatMoney2;
const formatRealPaid = (row: Row) => formatRealPaidDisplay(row.RealPaid, row.Paid, row.Currency);
const isValidPayAt = (v: unknown) => {
	if (!v) return false;
	const s = String(v);
	return !s.startsWith('1900') && !s.startsWith('0001');
};
/** 对齐旧 htmlFunction.DealTime：H:mm:ss → x小时y分z秒 */
const formatDealTime = (v: unknown) => {
	const s = String(v ?? '').trim();
	if (!s || !s.includes(':')) return 'N/A';
	const [h, m, sec] = s.split(':');
	if (h == null || m == null || sec == null) return 'N/A';
	return `${Number(h)}小时${m}分${sec}秒`;
};

const options = useVxeTable<Row>(
	{
		id: 'siteMoneyRechargeOrder',
		name: '充值订单',
		// 多行单元格：关 ellipsis；行高交给 CSS min-height + overflow:visible（见 admin-page.scss）
		showOverflow: false,
		columns: [
			{ field: 'ID', title: '订单号', minWidth: 148, showOverflow: true },
			{ field: 'SystemID', title: '网关单号', minWidth: 130, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'UserName', title: '用户', minWidth: 110, showOverflow: false, slots: { default: 'row_user' } },
			{ field: 'AgentName', title: '上级', minWidth: 100, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Pay', title: '支付', minWidth: 160, showOverflow: false, slots: { default: 'row_pay' } },
			{ field: 'Money', title: '存入金额', minWidth: 168, showOverflow: false, slots: { default: 'row_money' } },
			{ field: 'CreateAt', title: '充值时间', minWidth: 148, showOverflow: false, slots: { default: 'row_time' } },
			{ field: 'IsPay', title: '到帐', minWidth: 148, showOverflow: false, slots: { default: 'row_arrive' } },
			{ field: 'DealTime', title: '处理时长', minWidth: 120, showOverflow: false, slots: { default: 'row_deal' } },
			{ field: 'IsProxyRecharge', title: '代客充值', width: 88, slots: { default: 'row_proxy' } },
			{ field: 'Admin', title: '审核人', minWidth: 100, showOverflow: true, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'buttons', title: '明细', width: 88, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 不写死 height：固定 78 会被 vxe 打成 col--rs-height + overflow:hidden，第三行「实充」必裁
		rowConfig: { isHover: true },
		cellConfig: { verticalAlign: 'center' as any, height: 'auto' as any },
		scrollY: { enabled: false },
		virtualYConfig: { enabled: false },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };

const buildQuery = (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, pageSize?: number) => ({
	KeyWord: state.query.KeyWord || '',
	OrderID: state.query.OrderID || '',
	Agent: state.query.Agent || '',
	User: state.query.User || '',
	PayID: state.query.PayID || '',
	IsPay: state.query.Status || '',
	Status: state.query.Status || '',
	Currency: state.query.Currency || '',
	FromCurrency: state.query.FromCurrency || '',
	DateType: state.query.DateType || 'CreateAt',
	IsProxyRecharge: state.query.IsProxyRecharge || '',
	MoneyFrom: state.query.MoneyFrom || '',
	MoneyTo: state.query.MoneyTo || '',
	StartAt: state.timeRange?.[0] || '',
	EndAt: state.timeRange?.[1] || '',
	PageIndex: page.currentPage,
	PageSize: pageSize ?? page.pageSize,
});

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	await ensureCurrencyEnum().catch(() => undefined);
	return runAdminVxeQuery(() => fetchRechargeOrderList(buildQuery(page)));
};

const setRangeDays = (days: number) => {
	state.timeRange = rangeDays(days);
	handleQuery(true);
};
const resetQuery = () => {
	Object.assign(state.query, emptyQuery());
	state.timeRange = rangeDays(7);
	handleQuery(true);
};

const loadPayOptions = async () => {
	try {
		const info = await fetchRechargePayOptions({});
		const list = Array.isArray(info) ? info : readAdminList(info as any);
		state.payOptions = list as any[];
	} catch (e: any) {
		state.payOptions = [];
		showCaughtError(e, '加载渠道失败');
	}
};

const openDetail = async (row: Row) => {
	state.detailVisible = true;
	state.detail = { ...row };
	state.checkForm = {
		SystemID: String(row.SystemID || ''),
		Paid: formatMoney(row.Money),
		Fee: formatMoney(row.Fee),
		Cost: formatMoney(row.Cost),
	};
	try {
		const info = (await fetchRechargeOrderInfo(String(row.ID ?? row.OrderID).trim())) as Row;
		if (info && typeof info === 'object') {
			state.detail = { ...row, ...info };
			state.checkForm.Paid = formatMoney(info.Money ?? row.Money);
			state.checkForm.Fee = formatMoney(info.Fee ?? row.Fee);
			state.checkForm.Cost = formatMoney(info.Cost ?? row.Cost);
			state.checkForm.SystemID = String(info.SystemID ?? row.SystemID ?? '');
		}
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

const fillGuid = () => {
	state.checkForm.SystemID = crypto.randomUUID().replace(/-/g, '');
};

const submitCheck = async () => {
	if (!state.detail) return;
	if (!state.checkForm.SystemID.trim()) {
		ElMessage.warning('请填写网关编号');
		return;
	}
	state.checking = true;
	try {
		await checkRechargeOrder({
			ID: state.detail.ID || state.detail.OrderID,
			SystemID: state.checkForm.SystemID.trim(),
			Paid: state.checkForm.Paid,
			Fee: state.checkForm.Fee,
			Cost: state.checkForm.Cost,
		});
		ElMessage.success('已确认入账');
		state.detailVisible = false;
		handleQuery(false);
	} catch (e: any) {
		showCaughtError(e, '入账失败');
	} finally {
		state.checking = false;
	}
};

const submitDelete = async () => {
	if (!state.detail) return;
	await ElMessageBox.confirm('确认删除该未到账订单？', '删除订单', { type: 'warning' });
	state.deleting = true;
	try {
		await deleteRechargeOrder(state.detail.ID || state.detail.OrderID);
		ElMessage.success('已删除');
		state.detailVisible = false;
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e, '删除失败');
	} finally {
		state.deleting = false;
	}
};

const openCreate = async () => {
	if (!state.payOptions.length) await loadPayOptions();
	state.createForm = { UserName: '', PayID: '', Money: '', Description: '', DescriptionA: '' };
	state.createVisible = true;
};

const submitCreate = async () => {
	const f = state.createForm;
	if (!f.UserName.trim() || !f.PayID || !f.Money || !f.Description.trim() || !f.DescriptionA.trim()) {
		ElMessage.warning('请填写完整');
		return;
	}
	state.creating = true;
	try {
		await createProxyRecharge({ ...f });
		ElMessage.success('已提交，请审核入账');
		state.createVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e, '提交失败');
	} finally {
		state.creating = false;
	}
};

const csvCell = (v: unknown) => {
	const s = v == null ? '' : String(v);
	return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
};

const handleExport = async () => {
	state.exporting = true;
	try {
		const info = await fetchRechargeOrderList(buildQuery({ currentPage: 1, pageSize: 2000 } as any, 2000));
		const rows = readAdminList(info as any);
		const total = readAdminRecordCount(info as any, rows.length);
		if (!rows.length) {
			ElMessage.warning('没有可导出的数据');
			return;
		}
		const headers = ['ID', 'SystemID', 'UserName', 'AgentName', 'ThirdParty', 'Payment', 'Money', 'Fee', 'Cost', 'Paid', 'Currency', 'CreateAt', 'IsPay', 'PayAt', 'Count', 'IsProxyRecharge', 'Admin'];
		const lines = [headers.join(','), ...rows.map((r) => headers.map((h) => csvCell((r as any)[h])).join(','))];
		const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `recharge-order-${Date.now()}.csv`;
		a.click();
		URL.revokeObjectURL(url);
		ElMessage.success(`已导出 ${rows.length} 行` + (total > rows.length ? `（共 ${total}）` : ''));
	} catch (e: any) {
		showCaughtError(e, '导出失败');
	} finally {
		state.exporting = false;
	}
};


const loadCurrencies = async () => {
	await ensureCurrencyEnum().catch(() => undefined);
	try {
		const list = await fetchCurrencyOptions();
		state.currencyOptions = (list || []).map((x: any) => ({
			key: x.key ?? x.Key ?? x.value ?? x.Value,
			value: String(x.value ?? x.Value ?? x.key ?? ''),
		}));
	} catch (e: any) {
		state.currencyOptions = [];
		showCaughtError(e, '加载币种失败');
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

onMounted(() => {
	void ensureCurrencyEnum();
	void loadCurrencies();
	loadPayOptions();
});
onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped>
.page-title {
	font-size: 16px;
	font-weight: 600;
}
.header-btns {
	display: flex;
	gap: 8px;
	margin-left: auto;
}
.page-header-actions {
	justify-content: flex-start;
}
.muted {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}
.fee {
	color: #67c23a;
}
.real {
	color: #67c23a;
	word-break: break-all;
}
.cell-stack--money {
	min-width: 0;
}
.ok {
	color: #67c23a;
	font-weight: 600;
}
.no {
	color: #909399;
}
.link {
	color: var(--el-color-primary);
}
.money-em {
	color: #f56c6c;
	font-size: 16px;
}
.tip {
	color: var(--el-text-color-secondary);
	margin: 0 0 12px;
	font-size: 13px;
}
.check-form {
	margin-top: 8px;
}
.money-in { color: #67c23a; }
</style>
