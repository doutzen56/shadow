<template>
	<!-- 规格：Web.Admin/src/views/user/member/return.html；菜单 /user/member/return/index -->
	<div class="user-member-return-page admin-page">
		<div class="query-toolbar">
			<el-form :model="state.query" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="用户组">
					<el-select v-model="state.query.GroupID" clearable placeholder="全部" style="width: 160px">
						<el-option v-for="g in state.groupOptions" :key="g.ID" :label="g.Name || String(g.ID)" :value="String(g.ID)" />
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" :loading="options.loading" v-auth="'site:user:member:rebate'" @click="handleQuery(true)">查询</el-button>
					<el-button icon="ele-Refresh" :loading="options.loading" @click="resetQuery">重置</el-button>
				</el-button-group>
				<el-button type="success" plain v-auth="'site:user:member:rebate'" @click="openEdit()">添加设定</el-button>
				<el-button type="warning" plain v-auth="'site:user:member:rebate'" @click="openReturnRule">自助反水规则</el-button>
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
					<template #row_bet="{ row }">
						<span class="money-in">{{ formatMoney2(row.BetMoney) }}</span>
					</template>
					<template #row_max="{ row }">
						<span class="money-in">{{ formatMoney2(row.MaxMoney) }}</span>
					</template>
					<template #row_enabled="{ row }">
						<el-switch :model-value="!!row.Enabled" @change="() => onToggleEnabled(row)" />
					</template>
					<template #row_auto="{ row }">
						<el-switch :model-value="!!row.IsAuto" @change="() => onToggleAuto(row)" />
					</template>
					<template #row_buttons="{ row }">
						<el-button text type="primary" @click="openEdit(row)">修改</el-button>
						<el-button text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</vxe-grid>
			</div>
		</el-card>

		<el-dialog
			v-model="state.editVisible"
			:title="state.form.ID ? '修改返水设定' : '添加返水设定'"
			width="760px"
			destroy-on-close
			:close-on-click-modal="false"
		>
			<el-form :model="state.form" label-width="110px">
				<el-form-item label="返水名称" required>
					<el-input v-model="state.form.Name" maxlength="80" />
				</el-form-item>
				<el-form-item label="返水类型">
					<el-select v-model="state.form.Type" clearable filterable placeholder="选择类型" style="width: 100%">
						<el-option v-for="t in state.typeOptions" :key="t.value" :label="`${t.label} (${t.value})`" :value="t.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="会员分组">
					<el-checkbox-group v-model="state.form.GroupIds">
						<el-checkbox v-for="g in state.groupOptions" :key="g.ID" :label="String(g.ID)">{{ g.Name || g.ID }}</el-checkbox>
					</el-checkbox-group>
				</el-form-item>
				<el-form-item label="会员等级">
					<el-checkbox-group v-if="state.levelOptions.length" v-model="state.form.LevelIds">
						<el-checkbox v-for="lv in state.levelOptions" :key="String(lv.ID)" :label="String(lv.ID)">{{ lv.Name || lv.ID }}</el-checkbox>
					</el-checkbox-group>
					<el-input v-else v-model="state.form.Levels" placeholder="等级编号，逗号分隔" />
				</el-form-item>
				<el-form-item label="有效投注">
					<el-input-number v-model="state.form.BetMoney" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="上限金额">
					<el-input-number v-model="state.form.MaxMoney" :min="0" :precision="2" controls-position="right" />
				</el-form-item>
				<el-form-item label="自动发放">
					<el-switch v-model="state.form.IsAuto" />
				</el-form-item>
				<el-form-item label="启用">
					<el-switch v-model="state.form.Enabled" />
				</el-form-item>
				<el-form-item label="游戏返水%">
					<div class="rate-editor">
						<template v-if="state.gameGroups.length">
							<fieldset v-for="grp in state.gameGroups" :key="grp.Key || grp.Category" class="game-group">
								<legend>{{ grp.Label || grp.Category }}</legend>
								<div class="game-group-grid">
									<div v-for="g in grp.Games || []" :key="`${grp.Category}.${g.key}`" class="rate-row">
										<label class="rate-label">{{ g.Value || g.key }}</label>
										<el-input-number
											v-model="state.gameRates[rateKey(grp.Category, g.key)]"
											:min="0"
											:max="100"
											:precision="4"
											controls-position="right"
											@change="syncSettingFromGameRates"
										/>
										<span class="rate-unit">%</span>
									</div>
								</div>
							</fieldset>
						</template>
						<div v-else class="rate-empty">暂无开启厂商；可手工维护原文 Setting</div>
						<el-input
							v-model="state.form.SettingString"
							type="textarea"
							:rows="3"
							placeholder="原文 Setting（Category.Code=比例&…）；改厂商网格会回写此处"
							style="margin-top: 8px"
							@change="syncGameRatesFromSetting"
						/>
					</div>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.ruleVisible" title="自助反水规则" width="560px" destroy-on-close :close-on-click-modal="false">
			<el-form label-width="100px">
				<el-form-item label="规则原文">
					<el-input v-model="state.autoReturnRule" type="textarea" :rows="8" placeholder="站点 AutoReturnRule 原文；保存到 /admin/site/vip/savereturnrule" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.ruleVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.ruleSaving" @click="onSaveReturnRule">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteUserMemberReturn">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import type { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, useGridQuery, gridPageEvents } from '/@/hooks/useVxeTableOptionsHook';
import { fetchMemberGroupList, unwrapAdminList } from '/@/api/user/member-group';
import { fetchVipLevelOptions } from '/@/api/user/member';
import {
	deleteMemberRebate,
	fetchMemberRebateInfo,
	fetchMemberRebateList,
	fetchMemberRebateReturnTypes,
	saveMemberRebate,
	toggleMemberRebateEnabled,
	updateMemberRebateAuto,
	type MemberRebateRow,
} from '/@/api/user/member-rebate';
import { runAdminVxeQuery } from '/@/utils/admin-vxe';
import { adminPost, readAdminList } from '/@/utils/admin-api';
import { formatMoney2 } from '/@/utils/admin-display';

const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('siteUserMemberReturn', {
	pageSize: 50 as number,
	defaultSort: { field: 'ID', order: 'desc', descStr: 'desc' },
});

const emptyForm = () => ({
	ID: 0,
	Name: '',
	GroupIds: [] as string[],
	LevelIds: [] as string[],
	Levels: '',
	BetMoney: 0,
	MinMoney: 0,
	MaxMoney: 0,
	IsAuto: false,
	Enabled: true,
	Type: '',
	SettingString: '',
});

const state = reactive({
	query: { GroupID: '' as string },
	groupOptions: [] as Array<{ ID: number; Name?: string }>,
	levelOptions: [] as Array<{ ID: number | string; Name?: string }>,
	typeOptions: [] as Array<{ value: string; label: string }>,
	editVisible: false,
	saving: false,
	form: emptyForm(),
	gameGroups: [] as Array<{ Key?: string; Category: string; Label?: string; Games?: Array<{ key: string; Value?: string }> }>,
	gameRates: {} as Record<string, number>,
	ruleVisible: false,
	ruleSaving: false,
	autoReturnRule: '',
	localPageParam,
});

/** 逗号串 → 勾选 id */
const splitIds = (raw?: string | null) =>
	String(raw || '')
		.split(/[,|;]/)
		.map((s) => s.trim())
		.filter(Boolean);

/** 比例键：Category.Code（对齐旧 Game.Lottery.CB）。 */
const rateKey = (category: string, code: string) => `${category}.${code}`;

/** SettingString → gameRates（库内小数 → 编辑百分比）。 */
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
			next[key] = Number.isFinite(pct) ? pct * 100 : 0;
		}
	}
	for (const grp of state.gameGroups) {
		for (const g of grp.Games || []) {
			const k = rateKey(grp.Category, g.key);
			if (!(k in next)) next[k] = 0;
		}
	}
	state.gameRates = next;
};

/** gameRates → SettingString（百分比 → 库内小数）。 */
const syncSettingFromGameRates = () => {
	const pairs: string[] = [];
	const seen = new Set<string>();
	for (const grp of state.gameGroups) {
		for (const g of grp.Games || []) {
			const k = rateKey(grp.Category, g.key);
			seen.add(k);
			const pct = Number(state.gameRates[k] ?? 0);
			if (!pct) continue;
			pairs.push(`${encodeURIComponent(k)}=${encodeURIComponent(String(Number(pct) / 100))}`);
		}
	}
	for (const [k, pct] of Object.entries(state.gameRates)) {
		if (seen.has(k) || !Number(pct)) continue;
		pairs.push(`${encodeURIComponent(k)}=${encodeURIComponent(String(Number(pct) / 100))}`);
	}
	state.form.SettingString = pairs.join('&');
};

/** 应用 info 的 GameGroups / Games。 */
const applyGameMeta = (info: Record<string, any> | null | undefined) => {
	state.gameGroups = Array.isArray(info?.GameGroups) ? (info!.GameGroups as any[]) : [];
	const games = info?.Games && typeof info.Games === 'object' && !Array.isArray(info.Games) ? info.Games : {};
	const rates: Record<string, number> = {};
	for (const [k, v] of Object.entries(games as Record<string, any>)) {
		const n = Number(v);
		// Games 来自 SettingString 小数费率 → 编辑百分比
		rates[k] = Number.isFinite(n) ? n * 100 : 0;
	}
	for (const grp of state.gameGroups) {
		for (const g of grp.Games || []) {
			const k = rateKey(grp.Category, g.key);
			if (!(k in rates)) rates[k] = 0;
		}
	}
	state.gameRates = rates;
	if (!state.form.SettingString && Object.keys(rates).some((k) => rates[k])) {
		state.form.SettingString = Object.entries(rates)
			.filter(([, pct]) => Number(pct))
			.map(([k, pct]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(Number(pct) / 100))}`)
			.join('&');
	}
};

const options = useVxeTable<MemberRebateRow>(
	{
		id: 'siteUserMemberReturn',
		name: '返水设定',
		columns: [
			{ field: 'Name', title: '返水名称', minWidth: 140, formatter: ({ cellValue }) => cellValue || '-' },
			{
				field: 'Type',
				title: '类型',
				width: 100,
				formatter: ({ cellValue }) => {
					const hit = state.typeOptions.find((t) => t.value === String(cellValue));
					return hit?.label || cellValue || '-';
				},
			},
			{ field: 'BetMoney', title: '有效流水', minWidth: 110, slots: { default: 'row_bet' } },
			{
				field: 'Groups',
				title: '适用分组',
				minWidth: 140,
				formatter: ({ cellValue }) => {
					const raw = String(cellValue ?? '').trim();
					if (!raw) return '-';
					return raw
						.split(/[,，]/)
						.map((id) => {
							const hit = state.groupOptions.find((g) => String(g.ID) === String(id).trim());
							return hit?.Name || String(id).trim();
						})
						.filter(Boolean)
						.join('、');
				},
			},
			{
				field: 'Levels',
				title: '会员等级',
				minWidth: 120,
				formatter: ({ cellValue }) => {
					const raw = String(cellValue ?? '').trim();
					if (!raw) return '-';
					return raw
						.split(/[,，]/)
						.map((id) => {
							const hit = state.levelOptions.find((lv) => String(lv.ID) === String(id).trim());
							return hit?.Name || String(id).trim();
						})
						.filter(Boolean)
						.join('、');
				},
			},
			{ field: 'MaxMoney', title: '最高发放金额', minWidth: 120, slots: { default: 'row_max' } },
			{ field: 'IsAuto', title: '自动发放', width: 90, slots: { default: 'row_auto' } },
			{ field: 'Enabled', title: '启用', width: 90, slots: { default: 'row_enabled' } },
			{ field: 'CreatorName', title: '创建人', minWidth: 100, formatter: ({ cellValue }) => cellValue || '-' },
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
		pagerConfig: { enabled: false },
	}
);

const handleQuery = useGridQuery(xGrid, options);
const gridEvents: VxeGridListeners<MemberRebateRow> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

/**
 * 列表。
 * @param _page 分页
 */
const handleQueryApi = async (_page: VxeGridPropTypes.ProxyAjaxQueryPageParams) =>
	runAdminVxeQuery(async () => {
		const list = await fetchMemberRebateList(state.query.GroupID || undefined);
		const mapped = list.map((r) => ({
			...r,
			Groups: r.Groups ?? r.Group,
			Levels: r.Levels ?? r.Level,
		}));
		return { list: mapped, RecordCount: mapped.length };
	});

/** 重置 */
const resetQuery = () => {
	state.query.GroupID = '';
	handleQuery(true);
};

/**
 * 删除。
 * @param row 行
 */
const onDelete = async (row: MemberRebateRow) => {
	try {
		await ElMessageBox.confirm('确认删除该返水设定吗？', '提示', { type: 'warning' });
		await deleteMemberRebate(row.ID);
		ElMessage.success('已删除');
		handleQuery(true);
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	}
};

/** 列表点启用标签切换。 */
const onToggleEnabled = async (row: MemberRebateRow) => {
	try {
		const next = await toggleMemberRebateEnabled(row.ID);
		row.Enabled = typeof next === 'boolean' ? next : !row.Enabled;
		ElMessage.success(row.Enabled ? '已启用' : '已关闭');
	} catch (e: any) {
		showCaughtError(e, '切换失败');
	}
};

/** 列表点自动发放切换。 */
const onToggleAuto = async (row: MemberRebateRow) => {
	try {
		const next = !row.IsAuto;
		await updateMemberRebateAuto(row.ID, next);
		row.IsAuto = next;
		ElMessage.success(next ? '已开自动发放' : '已关自动发放');
	} catch (e: any) {
		showCaughtError(e, '切换失败');
	}
};

/**
 * 打开添加/编辑。
 * @param row 行
 */
const openEdit = async (row?: MemberRebateRow) => {
	Object.assign(state.form, emptyForm());
	state.gameGroups = [];
	state.gameRates = {};
	state.editVisible = true;
	try {
		const info = (await fetchMemberRebateInfo(row?.ID || 0)) as Record<string, any>;
		if (row?.ID) {
			const groupsRaw = info?.Groups ?? row.Groups ?? row.Group ?? '';
			const levelsRaw = info?.Levels ?? row.Levels ?? row.Level ?? '';
			Object.assign(state.form, {
				ID: info?.ID ?? row.ID,
				Name: info?.Name ?? row.Name ?? '',
				GroupIds: splitIds(groupsRaw),
				LevelIds: splitIds(levelsRaw),
				Levels: levelsRaw,
				BetMoney: Number(info?.BetMoney ?? row.BetMoney ?? 0),
				MinMoney: Number(info?.MinMoney ?? row.MinMoney ?? 0),
				MaxMoney: Number(info?.MaxMoney ?? row.MaxMoney ?? 0),
				IsAuto: Boolean(info?.IsAuto ?? row.IsAuto),
				Enabled: info?.Enabled !== false && row.Enabled !== false,
				Type: info?.Type ?? row.Type ?? '',
				SettingString: info?.SettingString ?? '',
			});
		}
		applyGameMeta(info);
	} catch (e: any) {
		showCaughtError(e, '加载详情失败');
	}
};

/** 保存 */
const onSave = async () => {
	if (!state.form.Name?.trim()) {
		ElMessage.warning('请输入返水名称');
		return;
	}
	syncSettingFromGameRates();
	state.saving = true;
	try {
		const levels =
			state.levelOptions.length > 0 ? state.form.LevelIds.join(',') : state.form.Levels || '';
		await saveMemberRebate({
			ID: state.form.ID,
			Name: state.form.Name.trim(),
			Groups: state.form.GroupIds.join(','),
			Levels: levels,
			BetMoney: state.form.BetMoney,
			MinMoney: state.form.MinMoney,
			MaxMoney: state.form.MaxMoney,
			IsAuto: state.form.IsAuto ? 1 : 0,
			Enabled: state.form.Enabled ? 1 : 0,
			Type: state.form.Type || '',
			SettingString: state.form.SettingString || '',
		});
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery(true);
	} catch (e: any) {
		showCaughtError(e, '保存失败');
	} finally {
		state.saving = false;
	}
};

/** 分组下拉 */
const loadGroups = async () => {
	try {
		const info = await fetchMemberGroupList();
		state.groupOptions = unwrapAdminList(info);
	} catch (e: any) {
		state.groupOptions = [];
		showCaughtError(e, '加载分组失败');
	}
};

/** 返水类型 */
const loadTypes = async () => {
	try {
		state.typeOptions = await fetchMemberRebateReturnTypes();
	} catch (e: any) {
		state.typeOptions = [];
		showCaughtError(e, '加载返水类型失败');
	}
};

/** VIP 等级 */
const loadLevels = async () => {
	try {
		const info = await fetchVipLevelOptions();
		const list = Array.isArray(info) ? info : readAdminList(info as any);
		state.levelOptions = (list as any[]).map((x) => ({
			ID: x.Level ?? x.ID ?? x.id ?? x.Key,
			Name: x.Name ?? x.name ?? String(x.Level ?? x.ID ?? ''),
		}));
	} catch (e: any) {
		state.levelOptions = [];
		showCaughtError(e, '加载等级失败');
	}
};

const openReturnRule = async () => {
	state.autoReturnRule = '';
	state.ruleVisible = true;
	try {
		const info = (await adminPost('/admin/site/vip/getrule', {})) as Record<string, any>;
		state.autoReturnRule = String(info?.AutoReturnRule ?? info?.ReturnRule ?? '');
	} catch (e: any) {
		showCaughtError(e);
	}
};
const onSaveReturnRule = async () => {
	state.ruleSaving = true;
	try {
		await adminPost('/admin/site/vip/savereturnrule', { AutoReturnRule: state.autoReturnRule || '' });
		ElMessage.success('保存成功');
		state.ruleVisible = false;
	} catch (e: any) {
		showCaughtError(e);
	} finally {
		state.ruleSaving = false;
	}
};

onMounted(() => {
	loadGroups();
	loadTypes();
	loadLevels();
});
</script>

<style scoped>
.rate-editor {
	width: 100%;
}
.game-group {
	border: 1px solid var(--el-border-color);
	border-radius: 6px;
	padding: 8px 12px 4px;
	margin: 0 0 10px;
}
.game-group legend {
	padding: 0 6px;
	font-size: 13px;
	color: var(--el-text-color-regular);
}
.game-group-grid {
	display: flex;
	flex-wrap: wrap;
	gap: 4px 12px;
}
.rate-row {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 6px;
	min-width: 220px;
}
.rate-label {
	width: 88px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.rate-unit {
	color: var(--el-text-color-secondary);
}
.rate-empty {
	color: var(--el-text-color-secondary);
	font-size: 12px;
	margin-bottom: 6px;
}
.money-in { color: #67c23a; }
</style>
