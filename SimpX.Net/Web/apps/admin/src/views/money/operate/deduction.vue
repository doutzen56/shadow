<template>
	<!-- 规格：Web.Admin/src/views/money/operate/deduction.html → /admin/money/operate/redeductmoney -->
	<div class="money-operate-deduction-page admin-page">
		<el-card shadow="hover">
			<template #header>
				<div class="card-hd">
					<span>会员扣款</span>
					<div class="hd-actions">
						<el-button type="danger" plain :loading="state.excelLoading" @click="excelRef?.click()">批量扣款(Excel)</el-button>
						<input ref="excelRef" type="file" accept=".xlsx,.xls" style="display: none" @change="onExcelChange" />
					</div>
				</div>
			</template>
			<el-form :model="state.form" label-width="110px" class="op-form" @submit.prevent>
				<el-form-item label="用户名" required>
					<el-input v-model="state.form.User" clearable placeholder="会员账号" style="width: 320px" />
				</el-form-item>
				<el-form-item label="扣款类型" required>
					<el-select v-model="state.form.Type" filterable style="width: 240px">
						<el-option v-for="t in state.typeOptions" :key="t.value" :label="t.label" :value="t.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="货币类型" required>
					<el-select v-model="state.form.Currency" filterable style="width: 240px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="Number(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="扣款金额" required>
					<el-input v-model="state.form.Money" clearable style="width: 240px" />
					<span class="unit">元</span>
				</el-form-item>
				<el-form-item label="流水号" required>
					<el-input v-model="state.form.SourceID" clearable style="width: 320px" placeholder="流水号+用户+类型全站唯一；双击可随机" @dblclick="fillSourceId" />
				</el-form-item>
				<el-form-item label="前端备注" required>
					<el-input v-model="state.form.Description" clearable style="width: 420px" />
				</el-form-item>
				<el-form-item label="后端备注" required>
					<el-input v-model="state.form.DescriptionA" clearable style="width: 420px" />
				</el-form-item>
				<el-form-item>
					<el-button type="danger" :loading="state.loading" v-auth="'site:money:kuanxiangguanli:huiyuankoukuan'" @click="onSubmit">提交</el-button>
					<el-button @click="resetForm">重置</el-button>
				</el-form-item>
			</el-form>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateDeduction">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum } from '/@/utils/admin-display';
import { normalizeMoneyTypeGroups } from '/@/utils/admin-money-type';

const excelRef = ref<HTMLInputElement | null>(null);

const deductTypes = [
	{ value: 251, label: '手工减款 (251)' },
	{ value: 230, label: '活动扣除 (230)' },
	{ value: 62, label: '返水扣除 (62)' },
	{ value: 132, label: '佣金扣除 (132)' },
	{ value: 133, label: '代理返点扣除 (133)' },
	{ value: 9, label: '充值扣除 (9)' },
	{ value: 11, label: '提现扣除 (11)' },
];

const emptyForm = () => ({
	User: '',
	Type: 251 as number,
	Currency: 0 as number,
	Money: '',
	SourceID: '',
	Description: '',
	DescriptionA: '',
});

const state = reactive({
	loading: false,
	excelLoading: false,
	form: emptyForm(),
	typeOptions: deductTypes,
	currencyOptions: [] as Array<{ key: string | number; value: string }>,
});

const fillSourceId = () => {
	state.form.SourceID = crypto.randomUUID().replace(/-/g, '');
};

const resetForm = () => {
	const cur = state.form.Currency;
	Object.assign(state.form, emptyForm());
	state.form.Currency = cur;
};

const loadCurrency = async () => {
	try {
		await ensureCurrencyEnum();
		const list = await fetchCurrencyOptions();
		state.currencyOptions = (list || []).map((x: any) => ({
			key: x.key ?? x.Key ?? x.value ?? x.Value,
			value: String(x.value ?? x.Value ?? x.key ?? ''),
		}));
		if (state.form.Currency === 0 && state.currencyOptions[0]) state.form.Currency = Number(state.currencyOptions[0].key);
	} catch (e: any) {
		showCaughtError(e);
	}
};

const loadTypes = async () => {
	try {
		const info = await adminPost('/admin/config/moneytype', {});
		const groups = normalizeMoneyTypeGroups(info);
		const flat = groups.flatMap((g) => g.options).map((o) => ({ value: Number(o.value), label: `${o.label} (${o.value})` }));
		if (flat.length) {
			state.typeOptions = flat;
			if (!flat.some((t) => t.value === state.form.Type)) state.form.Type = flat[0]!.value;
		}
	} catch (e: any) {
		state.typeOptions = deductTypes;
		showCaughtError(e, '加载类型失败');
	}
};

const onSubmit = async () => {
	const f = state.form;
	if (!f.User.trim() || !f.Money || !f.SourceID.trim() || !f.Description.trim() || !f.DescriptionA.trim()) {
		ElMessage.warning('请填写用户名、金额、流水号与前后端备注');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认对 ${f.User} 扣款 ${f.Money}？`, '资金操作确认', { type: 'warning' });
		state.loading = true;
		const info = await adminPost('/admin/money/operate/redeductmoney', {
			User: f.User.trim(),
			Type: f.Type,
			Currency: f.Currency,
			Money: f.Money,
			SourceID: f.SourceID.trim(),
			Description: f.Description.trim(),
			DescriptionA: f.DescriptionA.trim(),
		});
		const summary = (info as any)?.Summary || (info as any)?.summary;
		ElMessage.success(typeof summary === 'string' && summary ? summary : '已提交');
		resetForm();
		fillSourceId();
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.loading = false;
	}
};

const onExcelChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		await ElMessageBox.confirm(`确认上传 Excel「${file.name}」批量扣款？`, '资金操作确认', { type: 'warning' });
		state.excelLoading = true;
		const fd = new FormData();
		fd.append('file', file);
		const info = await adminPost('/admin/money/operate/submoneybatchbyexcel', fd);
		const summary = (info as any)?.Summary || (info as any)?.summary;
		ElMessage.success(typeof summary === 'string' && summary ? summary : '已提交');
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.excelLoading = false;
	}
};

onMounted(() => {
	fillSourceId();
	void loadCurrency();
	void loadTypes();
});
</script>

<style scoped>
.card-hd {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	width: 100%;
}
.hd-actions {
	display: flex;
	gap: 8px;
}
.op-form {
	max-width: 720px;
}
.unit {
	margin-left: 8px;
	color: var(--el-text-color-secondary);
	font-size: 13px;
}
</style>
