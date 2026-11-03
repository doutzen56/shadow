<template>
	<!-- 规格：Web.Admin/src/views/user/agent/commission.html -->
	<div class="user-agent-commission-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="options.loading" v-auth="'site:user:agent:fanyongsheding'" @click="handleQuery(true)">刷新</el-button>
				<el-button type="success" plain v-auth="'site:user:agent:fanyongsheding'" @click="openEdit()">添加方案</el-button>
			</div>
		</div>
		<el-card class="full-table" shadow="hover">
			<div class="grid-holder">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty><el-empty :image-size="140" description="暂无数据" /></template>
					<template #row_bool="{ row, column }">
						<el-tag :type="row[column.field] ? 'success' : 'info'" size="small">{{ row[column.field] ? '是' : '否' }}</el-tag>
					</template>
					<template #row_open="{ row }">
						<el-switch
							:model-value="!!row.IsOpen"
							:loading="state.toggleId === String(row.ID)"
							@change="(v) => onToggleOpen(row, !!v)"
						/>
					</template>
					<template #row_count="{ row }">
						<a href="javascript:;" title="点击刷新代理数量" @click="onUpdateCount(row)">{{ row.AgentCount ?? 0 }}</a>
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog v-model="state.editVisible" :title="state.form.ID ? '修改返佣方案' : '添加返佣方案'" width="760px" destroy-on-close :close-on-click-modal="false">
			<el-form :model="state.form" label-width="120px">
				<el-form-item label="名称" required><el-input v-model="state.form.Name" /></el-form-item>
				<el-form-item label="有效会员要求"><el-input-number v-model="state.form.MemberMoney" :min="0" :precision="2" controls-position="right" /></el-form-item>
				<el-form-item label="优惠承担%"><el-input-number v-model="state.form.Award" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="返水承担%"><el-input-number v-model="state.form.Return" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="行政费承担%"><el-input-number v-model="state.form.Cost" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="最高比例%"><el-input-number v-model="state.form.MaxRate" :min="0" :precision="4" controls-position="right" /></el-form-item>
				<el-form-item label="启用"><el-switch v-model="state.form.IsOpen" /></el-form-item>
				<el-form-item label="契约"><el-switch v-model="state.form.IsContract" /></el-form-item>
				<el-form-item label="游戏返佣%">
					<div class="rate-editor">
						<template v-if="state.gameVendors.length">
							<div v-for="v in state.gameVendors" :key="v.Type" class="vendor-block">
								<div class="vendor-title">{{ v.Name || v.Type }}</div>
								<div class="game-group-grid">
									<div v-for="c in v.Categories" :key="`${v.Type}_${c.code}`" class="rate-row">
										<label class="rate-label">{{ c.label }}</label>
										<el-input-number v-model="state.gameRates[rateKey(v.Type, c.code)]" :min="0" :max="100" :precision="4" controls-position="right" @change="syncSettingFromGameRates" />
										<span class="rate-unit">%</span>
									</div>
								</div>
							</div>
						</template>
						<div v-else class="rate-empty">暂无开启厂商；可手工维护原文 Setting</div>
						<el-input v-model="state.form.SettingString" type="textarea" :rows="3" placeholder="原文 Setting（Type_Category=比例）；改厂商网格会回写" style="margin-top: 8px" @change="syncGameRatesFromSetting" />
					</div>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserAgentCommission">
import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { adminPost } from '/@/utils/admin-api';
import { fetchAgentCommissionList, updateAgentCommissionCount } from '/@/api/user/agent';
import { formatDateTime } from '/@/utils/format';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';

type Row = Record<string, any>;
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserAgentCommission', { pageSize: 20 as number, defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' } });
const emptyForm = () => ({
	ID: 0,
	Name: '',
	MemberMoney: 0,
	Award: 0,
	Return: 0,
	Cost: 0,
	MaxRate: 0,
	IsOpen: true,
	IsContract: false,
	SettingString: '',
});
const state = reactive({
	toggleId: '' as string,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	gameVendors: [] as Array<{ Type: string; Name: string; Categories: Array<{ code: string; label: string }> }>,
	gameRates: {} as Record<string, number>,
	localPageParam,
});
const stripHash = (s: string) => String(s || '').replace(/^#+|#+$/g, '').trim() || s;
const rateKey = (type: string, cat: string) => `${type}_${cat}`;
const loadGameVendors = async () => {
	try {
		const info = await adminPost('/admin/game/info/get', {});
		const list = Array.isArray((info as any)?.list) ? (info as any).list : Array.isArray(info) ? (info as any) : [];
		state.gameVendors = (list as any[])
			.map((g) => {
				const type = String(g.Type ?? '');
				const catObj = g.Category && typeof g.Category === 'object' ? g.Category : {};
				const Categories = Object.entries(catObj as Record<string, string>).map(([code, label]) => ({
					code,
					label: stripHash(String(label || code)),
				}));
				return { Type: type, Name: String(g.Name || type), Categories };
			})
			.filter((v) => v.Type && v.Categories.length);
	} catch (e: any) {
		state.gameVendors = [];
		showCaughtError(e, '加载游戏厂商失败');
	}
};
const syncGameRatesFromSetting = () => {
	const next: Record<string, number> = {};
	const raw = String(state.form.SettingString || '').trim();
	if (raw) {
		for (const part of raw.split('&')) {
			const i = part.indexOf('=');
			if (i <= 0) continue;
			const key = decodeURIComponent(part.slice(0, i).trim());
			const pct = Number(decodeURIComponent(part.slice(i + 1).trim()));
			if (!key) continue;
			next[key] = Number.isFinite(pct) ? pct : 0;
		}
	}
	// 库内 Setting 为小数费率；编辑层百分比（对齐旧 SettingShow / percent）
	for (const k of Object.keys(next)) next[k] = Number(next[k]) * 100;
	for (const v of state.gameVendors) {
		for (const c of v.Categories) {
			const k = rateKey(v.Type, c.code);
			if (!(k in next)) next[k] = 0;
		}
	}
	state.gameRates = next;
};
const syncSettingFromGameRates = () => {
	const pairs: string[] = [];
	const seen = new Set<string>();
	for (const v of state.gameVendors) {
		for (const c of v.Categories) {
			const k = rateKey(v.Type, c.code);
			seen.add(k);
			const pct = Number(state.gameRates[k] ?? 0);
			if (!pct) continue;
			// 存库用小数费率（百分比÷100），对齐旧 percent 提交
			pairs.push(`${encodeURIComponent(k)}=${encodeURIComponent(String(Number(pct) / 100))}`);
		}
	}
	for (const [k, pct] of Object.entries(state.gameRates)) {
		if (seen.has(k) || !Number(pct)) continue;
		pairs.push(`${encodeURIComponent(k)}=${encodeURIComponent(String(Number(pct) / 100))}`);
	}
	state.form.SettingString = pairs.join('&');
};
const options = useVxeTable<Row>(
	{
		id: 'siteUserAgentCommission',
		name: '返佣设定',
		columns: [
			{ field: 'ID', title: '编号', width: 80 },
			{ field: 'Name', title: '名称', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{ field: 'CreateAt', title: '添加时间', minWidth: 150, formatter: ({ cellValue }) => formatDateTime(cellValue) },
			{ field: 'MemberMoney', title: '有效会员要求', minWidth: 120, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : Number(cellValue).toFixed(2)) },
			{ field: 'MaxRate', title: '最高比例%', width: 100, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(4)) },
			{ field: 'Award', title: '优惠承担%', width: 100, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(2)) },
			{ field: 'Return', title: '返水承担%', width: 100, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(2)) },
			{ field: 'Cost', title: '行政费承担%', width: 110, formatter: ({ cellValue }) => (cellValue == null || cellValue === '' ? '-' : (Number(cellValue) * 100).toFixed(2)) },
			{ field: 'IsOpen', title: '启用', width: 90, slots: { default: 'row_open' } },
			{ field: 'IsContract', title: '契约', width: 80, slots: { default: 'row_bool' } },
			{ field: 'AgentCount', title: '代理数量', width: 100, slots: { default: 'row_count' } },
			{ field: 'buttons', title: '操作', width: 140, fixed: 'right', slots: { default: 'row_buttons' } },
		],
	},
	{ proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } }, pagerConfig: { pageSize: localPageParam.pageSize } }
);
const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<Row> = { ...gridPageEvents(localPageParamKey, localPageParam) };
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const { list, total } = await fetchAgentCommissionList({ PageIndex: page.currentPage, PageSize: page.pageSize });
		return { list, RecordCount: total };
	});
const onUpdateCount = async (row: Row) => {
	try {
		await updateAgentCommissionCount(Number(row.ID));
		ElMessage.success('已更新');
		handleQuery(false);
	} catch (e: any) {
		showCaughtError(e);
	}
};
const onDelete = async (row: Row) => {
	try {
		await ElMessageBox.confirm('确认删除该方案吗？', '提示', { type: 'warning' });
		await adminPost('/admin/user/agent/commission/delete', { ID: row.ID });
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/** 列表 Switch 启停返佣方案（保留原费率字段）。 */
const onToggleOpen = async (row: Row, open: boolean) => {
	const id = Number(row.ID || 0);
	if (!id) return;
	state.toggleId = String(id);
	try {
		const info = (await adminPost('/admin/user/agent/commission/info', { ID: id })) as Row;
		await adminPost('/admin/user/agent/commission/save', {
			ID: id,
			Name: String(info?.Name ?? row.Name ?? ''),
			MemberMoney: Number(info?.MemberMoney ?? row.MemberMoney ?? 0),
			Award: Number(info?.Award ?? row.Award ?? 0),
			Return: Number(info?.Return ?? row.Return ?? 0),
			Cost: Number(info?.Cost ?? row.Cost ?? 0),
			MaxRate: Number(info?.MaxRate ?? row.MaxRate ?? 0),
			IsOpen: open ? 1 : 0,
			IsContract: info?.IsContract || row.IsContract ? 1 : 0,
			SettingString: String(info?.SettingString ?? info?.Setting ?? row.SettingString ?? ''),
		});
		row.IsOpen = open;
		ElMessage.success(open ? '已启用' : '已关闭');
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.toggleId = '';
	}
};
const openEdit = async (row?: Row) => {
	Object.assign(state.form, emptyForm());
	state.gameRates = {};
	state.editVisible = true;
	await loadGameVendors();
	if (!row?.ID) {
		syncGameRatesFromSetting();
		return;
	}
	try {
		const info = (await adminPost('/admin/user/agent/commission/info', { ID: row.ID })) as Row;
		Object.assign(state.form, {
			ID: Number(info?.ID ?? row.ID),
			Name: String(info?.Name ?? row.Name ?? ''),
			MemberMoney: Number(info?.MemberMoney ?? row.MemberMoney ?? 0),
			// 库内小数费率；编辑层按百分比（对齐旧 percent 输入）
			Award: Number(info?.Award ?? 0) * 100,
			Return: Number(info?.Return ?? 0) * 100,
			Cost: Number(info?.Cost ?? 0) * 100,
			MaxRate: Number(info?.MaxRate ?? 0) * 100,
			IsOpen: info?.IsOpen !== false && row.IsOpen !== false,
			IsContract: Boolean(info?.IsContract ?? row.IsContract),
			SettingString: String(info?.SettingString ?? info?.Setting ?? ''),
		});
		syncGameRatesFromSetting();
	} catch (e: any) {
		showCaughtError(e, '加载失败');
	}
};
const onSave = async () => {
	if (!state.form.Name?.trim()) {
		ElMessage.warning('请输入名称');
		return;
	}
	syncSettingFromGameRates();
	state.saving = true;
	try {
		await adminPost('/admin/user/agent/commission/save', {
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			MemberMoney: state.form.MemberMoney,
			Award: Number(state.form.Award || 0) / 100,
			Return: Number(state.form.Return || 0) / 100,
			Cost: Number(state.form.Cost || 0) / 100,
			MaxRate: Number(state.form.MaxRate || 0) / 100,
			IsOpen: state.form.IsOpen ? 1 : 0,
			IsContract: state.form.IsContract ? 1 : 0,
			SettingString: state.form.SettingString || '',
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
</script>

<style scoped>
.rate-editor { width: 100%; }
.vendor-block { border: 1px solid var(--el-border-color); border-radius: 6px; padding: 8px 12px 4px; margin: 0 0 10px; }
.vendor-title { font-size: 13px; margin-bottom: 6px; }
.game-group-grid { display: flex; flex-wrap: wrap; gap: 4px 12px; }
.rate-row { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; min-width: 200px; }
.rate-label { width: 72px; font-size: 12px; }
.rate-unit { color: var(--el-text-color-secondary); }
.rate-empty { color: var(--el-text-color-secondary); font-size: 12px; margin-bottom: 6px; }
</style>
