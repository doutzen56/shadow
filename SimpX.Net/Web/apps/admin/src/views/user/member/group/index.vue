<template>
	<!-- 规格：Web.Admin/src/views/user/member/group.html；菜单 /user/member/group/index -->
	<div class="user-member-group-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" v-auth="'site:user:member:group'" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain v-auth="'site:user:member:group'" @click="openEdit()">添加分组</el-button>
				<el-button type="warning" plain v-auth="'site:user:member:group'" @click="openBatchGroup">批量修改会员分组</el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #toolbar_buttons />
					<template #toolbar_tools />
					<template #empty>
						<el-empty :image-size="160" description="暂无分组" />
					</template>
					<template #row_bool="{ row, column }">
						<el-tag :type="isTruthy(row[column.field]) ? 'success' : 'info'" size="small">{{ isTruthy(row[column.field]) ? '是' : '否' }}</el-tag>
					</template>
					<template #row_member="{ row }">
						<a href="javascript:;" class="link" @click="goMembers(row)">{{ row.Member ?? 0 }}</a>
						<el-button text type="primary" :loading="state.refreshingId === row.ID" @click="onRefreshCount(row)">刷新人数</el-button>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openEdit(row)">{{ isTruthy(row.IsEdit) ? '修改' : '详情' }}</el-button>
						<el-button v-if="isTruthy(row.IsDelete)" text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog
			v-model="state.editVisible"
			:title="state.form.ID ? '修改分组信息' : '添加分组'"
			width="720px"
			destroy-on-close
			:close-on-click-modal="false"
		>
			<el-form :model="state.form" label-width="110px" @submit.prevent>
				<el-form-item label="分组名" required>
					<el-input v-model="state.form.Name" maxlength="50" />
				</el-form-item>
				<el-form-item label="默认分组">
					<el-switch v-model="state.form.IsDefault" />
				</el-form-item>
				<el-form-item label="夜间提款模式">
					<el-switch v-model="state.form.IsNight" />
				</el-form-item>
				<el-form-item label="排序">
					<el-input-number v-model="state.form.Sort" :min="0" :max="32767" controls-position="right" />
				</el-form-item>
				<el-divider content-position="left">自动分组</el-divider>
				<el-form-item label="开启跳组">
					<el-switch v-model="state.form.IsUpdate" />
				</el-form-item>
				<el-form-item label="累计充值">
					<el-input-number v-model="state.condition.Recharge" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="累计打码量">
					<el-input-number v-model="state.condition.BetAmount" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="运算逻辑">
					<el-switch v-model="state.condition.IsOR" active-text="或者" inactive-text="并且" />
				</el-form-item>
				<el-form-item label="充值次数">
					<el-input-number v-model="state.condition.RechargeCount" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="注册天数">
					<el-input-number v-model="state.condition.RegisterDays" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="须完成绑定">
					<el-checkbox v-model="state.condition.IsKYC">KYC</el-checkbox>
					<el-checkbox v-model="state.condition.IsBank">银行卡</el-checkbox>
					<el-checkbox v-model="state.condition.IsMobile">绑定手机</el-checkbox>
					<el-checkbox v-model="state.condition.IsEmail">完整邮箱</el-checkbox>
				</el-form-item>
				<el-divider content-position="left">提现设定</el-divider>
				<el-form-item label="禁止提现">
					<el-switch v-model="state.withdraw.NoWithdraw" />
				</el-form-item>
				<el-form-item label="锁定时间(时)">
					<el-input-number v-model="state.withdraw.WithdrawTime" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="银行卡最多">
					<el-input-number v-model="state.withdraw.MaxCard" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="虚拟卡最多">
					<el-input-number v-model="state.withdraw.MaxCardVir" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="电子钱包最多">
					<el-input-number v-model="state.withdraw.MaxPhone" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="同名数量">
					<el-input-number v-model="state.withdraw.SameCount" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="单笔最低">
					<el-input-number v-model="state.withdraw.MinMoney" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="单笔最高">
					<el-input-number v-model="state.withdraw.MaxMoney" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="免费次数">
					<el-input-number v-model="state.withdraw.FreeCount" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="手续费比例">
					<el-input-number v-model="state.withdraw.Fee" :min="0" :max="1" :step="0.01" :precision="4" controls-position="right" />
				</el-form-item>
				<el-form-item label="USDT手续费">
					<el-input-number v-model="state.withdraw.UsdtFee" :min="0" :max="1" :step="0.01" :precision="4" controls-position="right" />
				</el-form-item>
				<el-form-item label="手续费最低">
					<el-input-number v-model="state.withdraw.MinFee" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="手续费封顶">
					<el-input-number v-model="state.withdraw.MaxFee" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="提现次数/日">
					<el-input-number v-model="state.withdraw.MaxCount" :min="0" controls-position="right" />
				</el-form-item>
				<el-form-item label="允许不同名">
					<el-switch v-model="state.withdraw.IsDifferent" />
				</el-form-item>
				<el-form-item label="流水不足拒提">
					<el-switch v-model="state.withdraw.NoFlowWithdraw" />
				</el-form-item>
				<el-form-item label="存款限制时间">
					<el-switch v-model="state.withdraw.RechargeLimitTime" />
				</el-form-item>
				<el-form-item label="提款限制时间">
					<el-switch v-model="state.withdraw.WithdrawLimitTime" />
				</el-form-item>
				<el-divider content-position="left">充值渠道</el-divider>
				<el-form-item label="充值渠道">
					<div v-if="state.paymentOptions.length" class="payment-checks">
						<el-checkbox-group v-model="state.paymentIds">
							<el-checkbox v-for="p in state.paymentOptions" :key="p.id" :label="p.id">{{ p.name }}</el-checkbox>
						</el-checkbox-group>
					</div>
					<el-input v-else v-model="state.form.Payment" type="textarea" :rows="2" placeholder="通道编号 CSV（未能加载通道列表时手填）" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.batchVisible" title="批量修改会员分组" width="520px" destroy-on-close>
			<el-form label-width="100px">
				<el-form-item label="目标分组" required>
					<el-select v-model="state.batchGroupId" placeholder="选择分组" style="width: 100%">
						<el-option v-for="g in state.batchGroupOptions" :key="g.ID" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
				<el-form-item label="用户名" required>
					<el-input v-model="state.batchUsers" type="textarea" :rows="8" placeholder="每行一个用户名（或逗号分隔）" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.batchVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.batchSaving" @click="onBatchGroupSave">执行</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberGroup">
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';
import {
	deleteMemberGroup,
	fetchMemberGroupInfo,
	fetchMemberGroupList,
	refreshMemberGroupCount,
	saveMemberGroup,
	unwrapAdminList,
	type MemberGroupRow,
} from '/@/api/user/member-group';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { formatDateTime } from '/@/utils/format';

const router = useRouter();
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberGroup', {
	pageSize: 50 as number,
	defaultSort: { field: 'Sort', order: 'asc', descStr: 'desc' },
});

const emptyForm = () => ({
	ID: 0,
	Name: '',
	Sort: 0,
	IsDefault: false,
	IsNight: false,
	IsUpdate: false,
	Payment: '',
	Condition: '',
	SettingInfo: '',
});

const emptyWithdraw = () => ({
	NoWithdraw: false,
	MinMoney: 0,
	MaxMoney: 0,
	Fee: 0,
	UsdtFee: 0,
	MinFee: 0,
	MaxFee: 0,
	WithdrawTime: 0,
	MaxCard: 0,
	MaxCardVir: 0,
	MaxPhone: 0,
	MaxCredit: 0,
	MaxMarket: 0,
	SameCount: 0,
	FreeCount: 0,
	MaxCount: 0,
	IsDifferent: false,
	NoFlowWithdraw: false,
	RechargeLimitTime: false,
	WithdrawLimitTime: false,
});

const emptyCondition = () => ({
	Recharge: 0,
	BetAmount: 0,
	IsOR: false,
	RechargeCount: 0,
	RegisterDays: 0,
	IsKYC: false,
	IsBank: false,
	IsMobile: false,
	IsEmail: false,
});

const state = reactive({
	refreshingId: 0 as number,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	withdraw: emptyWithdraw(),
	condition: emptyCondition(),
	/** 编辑时保留未展示的提现 JSON 字段 */
	withdrawRaw: {} as Record<string, unknown>,
	conditionRaw: {} as Record<string, unknown>,
	paymentOptions: [] as Array<{ id: string; name: string }>,
	paymentIds: [] as string[],
	batchVisible: false,
	batchSaving: false,
	batchGroupId: '' as string,
	batchUsers: '',
	batchGroupOptions: [] as MemberGroupRow[],
	localPageParam,
});

/** 解析 Payment CSV / JSON 数组为勾选 id。 */
const parsePaymentIds = (raw?: string) => {
	const text = String(raw ?? '').trim();
	if (!text) return [] as string[];
	if (text.startsWith('[')) {
		try {
			const arr = JSON.parse(text) as unknown[];
			return arr.map((x) => String(x).trim()).filter(Boolean);
		} catch {
			/* fallthrough */
		}
	}
	return text.split(/[,，\s]+/).map((x) => x.trim()).filter(Boolean);
};

const loadPaymentOptions = async () => {
	try {
		const info = await adminPost<AdminApiPageInfo<{ ID?: number; Name?: string; Category?: string }>>(
			'/admin/recharge/settinglist',
			{ PageIndex: 1, PageSize: 500 }
		);
		const list = info?.list ?? [];
		state.paymentOptions = list.map((p) => ({
			id: String(p.ID ?? ''),
			name: `${p.Category ? p.Category + ' / ' : ''}${p.Name || p.ID}`,
		})).filter((p) => p.id);
	} catch (e: any) {
		state.paymentOptions = [];
		showCaughtError(e, '加载充值通道失败');
	}
};

const options = useVxeTable<MemberGroupRow>(
	{
		id: 'siteUserMemberGroup',
		name: '会员分组',
		columns: [
			{ field: 'ID', title: '分组编号', width: 100 },
			{ field: 'Name', title: '分组名称', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '创建时间', minWidth: 160, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'IsDefault', title: '默认分组', width: 100, slots: { default: 'row_bool' } },
			{ field: 'IsUpdate', title: '自动跳组', width: 100, slots: { default: 'row_bool' } },
			{ field: 'Sort', title: '排序值', width: 90, formatter: ({ cellValue }) => cellValue ?? '-' },
			{ field: 'Member', title: '分组会员数', minWidth: 140, slots: { default: 'row_member' } },
			{ field: 'buttons', title: '操作', width: 160, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: {
			autoLoad: true,
			ajax: {
				query: ({ page }) => handleQueryApi(page),
			},
		},
		pagerConfig: { enabled: false },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<MemberGroupRow> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

/**
 * 是否为旧站 1 / true。
 * @param v 字段值
 */
const isTruthy = (v: unknown) => v === 1 || v === '1' || v === true;

/**
 * 拉分组列表（旧站不分页）。
 * @param _page vxe 分页（忽略）
 */
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const info = await fetchMemberGroupList();
		const list = unwrapAdminList<MemberGroupRow>(info);
		return { list, RecordCount: list.length };
	});

/**
 * 刷新组内人数。
 * @param row 行
 */
const goMembers = (row: MemberGroupRow) => {
	router.push({ path: '/user/member/list', query: { GroupID: String(row.ID) } });
};
const onRefreshCount = async (row: MemberGroupRow) => {
	state.refreshingId = row.ID;
	try {
		const info = await refreshMemberGroupCount(row.ID);
		if (info && typeof info.Member === 'number') {
			row.Member = info.Member;
			ElMessage.success('已刷新人数');
		}
	} catch {
		/* admin-api 已提示 */
	} finally {
		state.refreshingId = 0;
	}
};

/**
 * 解析提现 JSON。
 * @param raw 接口 Withdraw 字符串
 */
const parseWithdraw = (raw?: string) => {
	const base = emptyWithdraw();
	state.withdrawRaw = {};
	if (!raw || !String(raw).trim()) {
		Object.assign(state.withdraw, base);
		return;
	}
	try {
		const obj = JSON.parse(String(raw)) as Record<string, unknown>;
		state.withdrawRaw = { ...obj };
		Object.assign(state.withdraw, base, {
			NoWithdraw: Boolean(obj.NoWithdraw),
			MinMoney: Number(obj.MinMoney ?? 0),
			MaxMoney: Number(obj.MaxMoney ?? 0),
			Fee: Number(obj.Fee ?? 0),
			UsdtFee: Number(obj.UsdtFee ?? 0),
			MinFee: Number(obj.MinFee ?? 0),
			MaxFee: Number(obj.MaxFee ?? 0),
			WithdrawTime: Number(obj.WithdrawTime ?? 0),
			MaxCard: Number(obj.MaxCard ?? 0),
			MaxCardVir: Number(obj.MaxCardVir ?? 0),
			MaxPhone: Number(obj.MaxPhone ?? 0),
			MaxCredit: Number(obj.MaxCredit ?? 0),
			MaxMarket: Number(obj.MaxMarket ?? 0),
			SameCount: Number(obj.SameCount ?? 0),
			FreeCount: Number(obj.FreeCount ?? 0),
			MaxCount: Number(obj.MaxCount ?? 0),
			IsDifferent: Boolean(obj.IsDifferent),
			NoFlowWithdraw: Boolean(obj.NoFlowWithdraw),
			RechargeLimitTime: Boolean(obj.RechargeLimitTime),
			WithdrawLimitTime: Boolean(obj.WithdrawLimitTime),
		});
	} catch {
		Object.assign(state.withdraw, base);
	}
};

/**
 * 解析自动跳组条件 JSON。
 * @param raw Condition 字符串
 */
const parseCondition = (raw?: string) => {
	const base = emptyCondition();
	state.conditionRaw = {};
	if (!raw || !String(raw).trim()) {
		Object.assign(state.condition, base);
		return;
	}
	try {
		const obj = JSON.parse(String(raw)) as Record<string, unknown>;
		state.conditionRaw = { ...obj };
		Object.assign(state.condition, base, {
			Recharge: Number(obj.Recharge ?? 0),
			BetAmount: Number(obj.BetAmount ?? 0),
			IsOR: Boolean(obj.IsOR),
			RechargeCount: Number(obj.RechargeCount ?? 0),
			RegisterDays: Number(obj.RegisterDays ?? 0),
			IsKYC: Boolean(obj.IsKYC),
			IsBank: Boolean(obj.IsBank),
			IsMobile: Boolean(obj.IsMobile),
			IsEmail: Boolean(obj.IsEmail),
		});
	} catch {
		Object.assign(state.condition, base);
	}
};

/**
 * 打开添加/编辑。
 * @param row 行；空为新增
 */
const openEdit = async (row?: MemberGroupRow) => {
	Object.assign(state.form, emptyForm());
	parseWithdraw();
	parseCondition();
	state.paymentIds = [];
	state.editVisible = true;
	if (!state.paymentOptions.length) await loadPaymentOptions();
	if (!row?.ID) return;
	try {
		const info = await fetchMemberGroupInfo(row.ID);
		Object.assign(state.form, {
			ID: info?.ID ?? row.ID,
			Name: info?.Name ?? row.Name ?? '',
			Sort: Number(info?.Sort ?? row.Sort ?? 0),
			IsDefault: Boolean(info?.IsDefault ?? isTruthy(row.IsDefault)),
			IsNight: Boolean(info?.IsNight),
			IsUpdate: Boolean(info?.IsUpdate ?? isTruthy(row.IsUpdate)),
			Payment: info?.Payment ?? '',
			Condition: info?.Condition ?? '',
			SettingInfo: info?.SettingInfo ?? '',
		});
		state.paymentIds = parsePaymentIds(info?.Payment ?? state.form.Payment);
		parseWithdraw(info?.Withdraw);
		parseCondition(info?.Condition);
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

/**
 * 保存分组。
 */
const onSave = async () => {
	if (!state.form.Name?.trim()) {
		ElMessage.warning('请输入分组名称');
		return;
	}
	state.saving = true;
	try {
		const withdrawPayload = {
			...state.withdrawRaw,
			NoWithdraw: state.withdraw.NoWithdraw,
			MinMoney: state.withdraw.MinMoney,
			MaxMoney: state.withdraw.MaxMoney,
			Fee: state.withdraw.Fee,
			UsdtFee: state.withdraw.UsdtFee,
			MinFee: state.withdraw.MinFee,
			MaxFee: state.withdraw.MaxFee,
			WithdrawTime: state.withdraw.WithdrawTime,
			MaxCard: state.withdraw.MaxCard,
			MaxCardVir: state.withdraw.MaxCardVir,
			MaxPhone: state.withdraw.MaxPhone,
			MaxCredit: state.withdraw.MaxCredit,
			MaxMarket: state.withdraw.MaxMarket,
			SameCount: state.withdraw.SameCount,
			FreeCount: state.withdraw.FreeCount,
			MaxCount: state.withdraw.MaxCount,
			IsDifferent: state.withdraw.IsDifferent,
			NoFlowWithdraw: state.withdraw.NoFlowWithdraw,
			RechargeLimitTime: state.withdraw.RechargeLimitTime,
			WithdrawLimitTime: state.withdraw.WithdrawLimitTime,
		};
		const conditionPayload = {
			...state.conditionRaw,
			Recharge: state.condition.Recharge,
			BetAmount: state.condition.BetAmount,
			IsOR: state.condition.IsOR,
			RechargeCount: state.condition.RechargeCount,
			RegisterDays: state.condition.RegisterDays,
			IsKYC: state.condition.IsKYC,
			IsBank: state.condition.IsBank,
			IsMobile: state.condition.IsMobile,
			IsEmail: state.condition.IsEmail,
		};
		const paymentCsv = state.paymentOptions.length
			? state.paymentIds.filter(Boolean).join(',')
			: state.form.Payment || '';
		await saveMemberGroup({
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			Sort: state.form.Sort,
			IsDefault: state.form.IsDefault ? 1 : 0,
			IsNight: state.form.IsNight ? 1 : 0,
			IsUpdate: state.form.IsUpdate ? 1 : 0,
			Payment: paymentCsv,
			WithdrawString: JSON.stringify(withdrawPayload),
			ConditionString: JSON.stringify(conditionPayload),
			SettingInfo: state.form.SettingInfo || '',
		});
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.saving = false;
	}
};

/** 打开批量改分组。 */
const openBatchGroup = async () => {
	state.batchUsers = '';
	state.batchGroupId = '';
	state.batchVisible = true;
	try {
		const info = await fetchMemberGroupList();
		state.batchGroupOptions = unwrapAdminList<MemberGroupRow>(info);
	} catch (e: any) {
		state.batchGroupOptions = [];
		showCaughtError(e, '加载分组失败');
	}
};

/** 按用户名解析后逐个 updatestatus GroupID。 */
const onBatchGroupSave = async () => {
	if (!state.batchGroupId) {
		ElMessage.warning('请选择目标分组');
		return;
	}
	const names = state.batchUsers
		.split(/[\r\n,，\s]+/)
		.map((s) => s.trim())
		.filter(Boolean);
	if (!names.length) {
		ElMessage.warning('请填写用户名');
		return;
	}
	state.batchSaving = true;
	let ok = 0;
	let fail = 0;
	try {
		for (const userName of names) {
			try {
				const page = await adminPost<{ list?: Array<{ ID?: number; UserName?: string }> }>('/admin/user/member/info/list', {
					SearchType: 'UserName',
					User: userName,
					IsLimit: 1,
					PageIndex: 1,
					PageSize: 5,
				});
				const hit =
					(page?.list ?? []).find((r) => String(r.UserName ?? '').toLowerCase() === userName.toLowerCase()) ??
					page?.list?.[0];
				const userId = Number(hit?.ID ?? 0);
				if (!userId) {
					fail++;
					continue;
				}
				await adminPost('/admin/user/member/info/updatestatus', {
					UserID: userId,
					Field: 'GroupID',
					Value: state.batchGroupId,
				});
				ok++;
			} catch {
				fail++;
			}
		}
		ElMessage.success(`完成：成功 ${ok}，失败 ${fail}`);
		if (ok) {
			state.batchVisible = false;
			handleQuery(true);
		}
	} finally {
		state.batchSaving = false;
	}
};

onMounted(() => {
	loadPaymentOptions();
});

/**
 * 删除分组。
 * @param row 行
 */
const onDelete = async (row: MemberGroupRow) => {
	try {
		await ElMessageBox.confirm(`确认删除分组「${row.Name || row.ID}」吗？`, '提示', { type: 'warning' });
		await deleteMemberGroup(row.ID);
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};
</script>

<style scoped>
.payment-checks {
	max-height: 180px;
	overflow: auto;
	width: 100%;
}
.payment-checks :deep(.el-checkbox) {
	display: flex;
	margin-right: 0;
	margin-bottom: 4px;
}
.link { color: var(--el-color-primary); cursor: pointer; margin-right: 8px; }
</style>
