<template>
	<!-- 规格：Web.Admin/src/views/user/member/kyc.html；菜单 /user/member/kyc/index -->
	<div class="user-member-kyc-page admin-page">
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
				<el-form-item label="身份证号码">
					<el-input v-model="state.query.UserNumber" clearable style="width: 180px" @keyup.enter="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.query.Status" clearable placeholder="全部" style="width: 110px">
						<el-option label="待审" value="None" />
						<el-option label="失败" value="Faild" />
						<el-option label="通过" value="Success" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button @click="setRangeDays(0)">今日</el-button>
				<el-button @click="setRangeDays(7)">近7天</el-button>
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:member:id-doc'" @click="handleQuery(true)">查询</el-button>
					<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				</el-button-group>
				<el-button type="success" plain v-auth="'site:user:member:id-doc'" @click="openCreate">添加证件信息</el-button>
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
					<template #row_cardtype="{ row }">
						<el-tag size="small" type="info">{{ CARD_TYPE_LABEL[Number(row.CardType)] || String(row.CardType ?? '-') || '-' }}</el-tag>
					</template>
					<template #row_status="{ row }">
						<el-tag
							size="small"
							:type="formatCheckStatus(row.Status) === '通过' ? 'success' : formatCheckStatus(row.Status) === '失败' ? 'danger' : formatCheckStatus(row.Status) === '待审' ? 'warning' : 'info'"
						>
							{{ formatCheckStatus(row.Status) }}
						</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<template v-if="isKycPending(row)">
							<el-button text type="danger" @click="onDelete(row)">删除</el-button>
							<el-button text type="primary" @click="openAudit(row)">审核</el-button>
						</template>
						<el-button v-else text type="primary" @click="openAudit(row)">详细</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.auditVisible" :title="state.auditPending ? '审核证件信息' : '证件信息'" width="640px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.audit" label-width="100px">
				<el-form-item label="用户"><el-input :model-value="state.audit.UserName" readonly /></el-form-item>
				<el-form-item label="证件名称"><el-input :model-value="state.audit.CardName" readonly /></el-form-item>
				<el-form-item label="证件号码">
					<el-input v-model="state.audit.CardNumber" :readonly="!state.auditPending" />
				</el-form-item>
				<el-form-item label="证件城市"><el-input :model-value="state.audit.CardCity" readonly /></el-form-item>
				<el-form-item label="证件日期"><el-input :model-value="state.audit.CardCreateAt" readonly /></el-form-item>
				<el-form-item label="生日"><el-input :model-value="state.audit.CardBirthday" readonly /></el-form-item>
				<el-form-item label="证件类型"><el-input :model-value="cardTypeText(state.audit.CardType)" readonly /></el-form-item>
				<el-form-item label="性别"><el-input :model-value="genderText(state.audit.Gender)" readonly /></el-form-item>
				<el-form-item label="国家"><el-input :model-value="state.audit.Country" readonly /></el-form-item>
				<el-form-item label="正面">
					<a v-if="state.audit.CardFront" :href="resolveUploadSrc(state.audit.CardFront)" target="_blank" rel="noopener">
						<img class="kyc-preview" :src="resolveUploadSrc(state.audit.CardFront)" alt="正面" />
					</a>
					<span v-else class="muted">无</span>
				</el-form-item>
				<el-form-item label="背面">
					<a v-if="state.audit.CardBack" :href="resolveUploadSrc(state.audit.CardBack)" target="_blank" rel="noopener">
						<img class="kyc-preview" :src="resolveUploadSrc(state.audit.CardBack)" alt="背面" />
					</a>
					<span v-else class="muted">无</span>
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.audit.Remark" :disabled="!state.auditPending" />
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

		<el-dialog v-model="state.createVisible" title="添加证件信息" width="520px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.create" label-width="100px">
				<el-form-item label="用户名称" required>
					<div style="display: flex; gap: 8px; width: 100%">
						<el-input v-model="state.create.UserName" />
						<el-button :loading="state.verifying" @click="onVerifyUser">查询用户</el-button>
					</div>
				</el-form-item>
				<el-form-item label="证件名称" required>
					<el-input v-model="state.create.CardName" />
				</el-form-item>
				<el-form-item label="证件号码" required>
					<el-input v-model="state.create.CardNumber" />
				</el-form-item>
				<el-form-item label="证件城市">
					<el-input v-model="state.create.CardCity" />
				</el-form-item>
				<el-form-item label="证件日期">
					<el-date-picker v-model="state.create.CardCreateAt" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
				</el-form-item>
				<el-form-item label="生日">
					<el-date-picker v-model="state.create.CardBirthday" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
				</el-form-item>
				<el-form-item label="证件类型">
					<el-select v-model="state.create.CardType" style="width: 100%">
						<el-option label="未领" :value="1" />
						<el-option label="领证" :value="2" />
						<el-option label="补证" :value="3" />
						<el-option label="换证" :value="4" />
					</el-select>
				</el-form-item>
				<el-form-item label="性别">
					<el-select v-model="state.create.Gender" style="width: 100%">
						<el-option label="男" :value="1" />
						<el-option label="女" :value="2" />
						<el-option label="其他" :value="0" />
					</el-select>
				</el-form-item>
				<el-form-item label="国家" required>
					<el-select v-model="state.create.Country" style="width: 100%">
						<el-option label="马来西亚(Malaysia)" value="马来西亚(Malaysia)" />
						<el-option label="台湾(Taiwan)" value="台湾(Taiwan)" />
					</el-select>
				</el-form-item>
				<el-form-item label="正面" required>
					<div class="kyc-upload-row">
						<el-input v-model="state.create.CardFront" placeholder="上传后自动填入路径" readonly />
						<el-upload :show-file-list="false" accept="image/*" :http-request="(opt) => onUploadSide('CardFront', opt)">
							<el-button :loading="state.uploadingFront">上传</el-button>
						</el-upload>
					</div>
				</el-form-item>
				<el-form-item label="背面" required>
					<div class="kyc-upload-row">
						<el-input v-model="state.create.CardBack" placeholder="上传后自动填入路径" readonly />
						<el-upload :show-file-list="false" accept="image/*" :http-request="(opt) => onUploadSide('CardBack', opt)">
							<el-button :loading="state.uploadingBack">上传</el-button>
						</el-upload>
					</div>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.createVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" :disabled="!state.create.UserID" @click="onCreateSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberKyc">
import { onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import type { UploadRequestOptions } from 'element-plus';
import {
	createMemberKyc,
	deleteMemberKyc,
	fetchMemberKycInfo,
	fetchMemberKycList,
	updateMemberKycInfo,
	uploadMemberKycImage,
	verifyMemberKycUser,
	type MemberKycRow,
} from '/@/api/user/member-kyc';
import { formatDateTime } from '/@/utils/format';
import { formatMoneyCheckStatus } from '/@/utils/admin-display';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

/** 旧 IDType 文案 */
const CARD_TYPE_LABEL: Record<number, string> = { 1: '未领', 2: '领证', 3: '补证', 4: '换证' };

const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberKyc', {
	pageSize: 20 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const emptyQuery = () => ({
	UserName: '',
	UserNumber: '',
	Status: 'None',
});

const state = reactive({
	query: emptyQuery(),
	timeRange: null as [string, string] | null,
	auditVisible: false,
	auditPending: false,
	createVisible: false,
	saving: false,
	verifying: false,
	uploadingFront: false,
	uploadingBack: false,
	audit: emptyAudit(),
	create: emptyCreate(),
	autoRefresh: false,
	localPageParam,
});

/** 审核/详情默认值 */
function emptyAudit() {
	return {
		ID: 0,
		UserName: '',
		CardName: '',
		CardNumber: '',
		Remark: '',
		Status: 0,
		StatusChoice: 'Faild',
		CardCity: '',
		CardCreateAt: '',
		CardBirthday: '',
		CardType: '',
		Gender: '',
		Country: '',
		CardFront: '',
		CardBack: '',
	};
}

/** 添加证件默认值（对齐旧 IDType / GenderType）。 */
function emptyCreate() {
	return {
		UserID: 0,
		UserName: '',
		CardName: '',
		CardNumber: '',
		CardCity: '',
		CardCreateAt: '',
		CardBirthday: '',
		CardType: 1,
		Gender: 1,
		Country: '马来西亚(Malaysia)',
		CardFront: '',
		CardBack: '',
	};
}


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

const options = useVxeTable<MemberKycRow>(
	{
		id: 'siteUserMemberKyc',
		name: '证件信息',
		columns: [
			{ field: 'ID', title: '序号', width: 80 },
			{ field: 'UserID', title: '会员编号', width: 90 },
			{ field: 'UserName', title: '用户', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardName', title: '证件名称', minWidth: 120, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardNumber', title: '证件号码', minWidth: 160, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardCity', title: '证件城市', width: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CardCreateAt', title: '证件日期', width: 120, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'CardBirthday', title: '生日', width: 120, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'CardType', title: '证件类型', width: 90, slots: { default: 'row_cardtype' } },
			{ field: 'Status', title: '状态', width: 90, slots: { default: 'row_status' } },
			{ field: 'Remark', title: '备注', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'Checker', title: '审核人', width: 90, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CheckDateTime', title: '审核时间', minWidth: 140, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'CreateAt', title: '创建时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
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
const gridEvents: VxeGridListeners<MemberKycRow> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

/**
 * 审核状态文案。
 * @param status 状态
 */
const formatCheckStatus = (status?: number | string) => formatMoneyCheckStatus(status);
const isKycPending = (row: Record<string, any>) => {
	const s = String(row.Status ?? '');
	return s === '0' || s === 'None' || Number(row.Status) === 0 || formatCheckStatus(s) === '待审';
};

/** 证件类型文案 */
const cardTypeText = (v?: string | number) => CARD_TYPE_LABEL[Number(v)] || String(v ?? '') || '-';

/** 性别文案 */
const genderText = (v?: string | number) => {
	const n = Number(v);
	if (n === 1 || v === 'Men' || v === '男') return '男';
	if (n === 2 || v === 'Women' || v === '女') return '女';
	if (v === '' || v == null) return '-';
	return '其他';
};

/** 上传键 → 可预览地址 */
const resolveUploadSrc = (raw?: string) => {
	const s = String(raw || '').trim();
	if (!s) return '';
	if (/^https?:\/\//i.test(s) || s.startsWith('/')) return s;
	return '/' + s.replace(/^\/+/, '');
};

/**
 * 查询。
 * @param page 分页
 */
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(() =>
		fetchMemberKycList({
			UserName: state.query.UserName || '',
			UserNumber: state.query.UserNumber || '',
			Status: state.query.Status || '',
			StartAt: state.timeRange?.[0] || '',
			EndAt: state.timeRange?.[1] || '',
			PageIndex: page.currentPage,
			PageSize: page.pageSize,
		})
	);

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
const onDelete = async (row: MemberKycRow) => {
	try {
		await ElMessageBox.confirm('确认要删除吗？', '提示', { type: 'warning' });
		await deleteMemberKyc(row.ID);
		ElMessage.success('已删除');
		handleQuery(false);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/**
 * 打开审核/详情。
 * @param row 行
 */
const openAudit = async (row: MemberKycRow) => {
	state.auditVisible = true;
	state.auditPending = isKycPending(row);
	Object.assign(state.audit, emptyAudit(), {
		ID: row.ID,
		UserName: row.UserName || '',
		CardName: row.CardName || '',
		CardNumber: row.CardNumber || '',
		Remark: row.Remark || '',
		Status: Number(row.Status ?? 0),
		CardCity: row.CardCity || '',
		CardCreateAt: row.CardCreateAt || '',
		CardBirthday: row.CardBirthday || '',
		CardType: row.CardType != null ? String(row.CardType) : '',
	});
	try {
		const info = (await fetchMemberKycInfo(row.ID)) as Record<string, any> | null;
		Object.assign(state.audit, {
			ID: info?.ID ?? row.ID,
			UserName: info?.UserName ?? row.UserName ?? '',
			CardName: info?.CardName ?? row.CardName ?? '',
			CardNumber: info?.CardNumber ?? row.CardNumber ?? '',
			Remark: info?.Remark ?? row.Remark ?? '',
			Status: Number(info?.Status ?? row.Status ?? 0),
			CardCity: info?.CardCity ?? row.CardCity ?? '',
			CardCreateAt: info?.CardCreateAt ?? row.CardCreateAt ?? '',
			CardBirthday: info?.CardBirthday ?? row.CardBirthday ?? '',
			CardType: info?.CardType != null ? String(info.CardType) : String(row.CardType ?? ''),
			Gender: info?.Gender != null ? String(info.Gender) : '',
			Country: info?.Country ?? '',
			CardFront: info?.CardFrontImg || info?.CardFront || '',
			CardBack: info?.CardBackImg || info?.CardBack || '',
		});
		state.auditPending = isKycPending(state.audit);
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

/** 提交审核 */
const onAuditSave = async () => {
	if (state.audit.StatusChoice === 'Success' && !state.audit.CardNumber?.trim()) {
		ElMessage.warning('通过时证件号码不能为空');
		return;
	}
	state.saving = true;
	try {
		await updateMemberKycInfo({
			ID: state.audit.ID,
			Status: state.audit.StatusChoice,
			Remark: state.audit.Remark || '',
			CardNumber: state.audit.CardNumber || '',
		});
		ElMessage.success('已保存');
		state.auditVisible = false;
		handleQuery(false);
	} catch (e: any) {
		showCaughtError(e, '保存失败');
	} finally {
		state.saving = false;
	}
};

/** 打开添加 */
const openCreate = () => {
	Object.assign(state.create, emptyCreate());
	state.createVisible = true;
};

/**
 * 正反面上传。
 * @param side CardFront / CardBack
 * @param opt 上传选项
 */
const onUploadSide = async (side: 'CardFront' | 'CardBack', opt: UploadRequestOptions) => {
	const file = opt.file as File;
	if (!file) return;
	const loadingKey = side === 'CardFront' ? 'uploadingFront' : 'uploadingBack';
	state[loadingKey] = true;
	try {
		const key = await uploadMemberKycImage(file);
		state.create[side] = key;
		opt.onSuccess?.(key as any);
		ElMessage.success('上传成功');
	} catch (e: any) {
		opt.onError?.(e);
		showCaughtError(e, '上传失败');
	} finally {
		state[loadingKey] = false;
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
		const info = (await verifyMemberKycUser(state.create.UserName.trim())) as { Id?: number; ID?: number } | null;
		state.create.UserID = Number(info?.Id ?? info?.ID ?? 0);
		if (!state.create.UserID) {
			ElMessage.error('用户不存在');
			return;
		}
		ElMessage.success(`已找到用户 #${state.create.UserID}`);
	} catch (e: any) {
		state.create.UserID = 0;
		showCaughtError(e, '查询失败');
	} finally {
		state.verifying = false;
	}
};

/** 创建证件 */
const onCreateSave = async () => {
	if (!state.create.UserID) {
		ElMessage.warning('请先查询用户');
		return;
	}
	if (!state.create.CardName?.trim() || !state.create.CardNumber?.trim()) {
		ElMessage.warning('请填写证件名称和号码');
		return;
	}
	if (!state.create.Country?.trim()) {
		ElMessage.warning('请选择国家');
		return;
	}
	if (!state.create.CardFront?.trim() || !state.create.CardBack?.trim()) {
		ElMessage.warning('请上传证件正反面');
		return;
	}
	state.saving = true;
	try {
		await createMemberKyc({
			UserID: state.create.UserID,
			ID: state.create.UserID,
			UserName: state.create.UserName.trim(),
			CardName: state.create.CardName.trim(),
			CardNumber: state.create.CardNumber.trim(),
			CardCity: state.create.CardCity || '',
			CardCreateAt: state.create.CardCreateAt || '',
			CardBirthday: state.create.CardBirthday || '',
			CardType: state.create.CardType,
			Gender: state.create.Gender,
			Country: state.create.Country,
			CardFront: state.create.CardFront,
			CardBack: state.create.CardBack,
		});
		ElMessage.success('已添加');
		state.createVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e, '添加失败');
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
onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped lang="scss">
.kyc-upload-row {
	display: flex;
	gap: 8px;
	width: 100%;
	align-items: center;
}
.kyc-preview {
	max-width: 220px;
	max-height: 140px;
	object-fit: contain;
	border: 1px solid var(--el-border-color);
	border-radius: 4px;
	cursor: zoom-in;
}
.muted {
	color: var(--el-text-color-secondary);
}
</style>
