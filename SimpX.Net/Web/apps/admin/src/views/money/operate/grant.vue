<template>
	<!-- 规格：Web.Admin/src/views/money/operate/grant.html → /admin/money/operate/addmoney -->
	<div class="money-operate-grant-page admin-page">
		<el-card shadow="hover">
			<template #header>
				<div class="card-hd">
					<span>会员加款</span>
					<div class="hd-actions">
						<el-button type="primary" plain @click="state.batchVisible = true">批量加款(文本)</el-button>
						<el-button type="success" plain :loading="state.excelLoading" @click="excelRef?.click()">批量加款(Excel)</el-button>
						<input ref="excelRef" type="file" accept=".xlsx,.xls" style="display: none" @change="onExcelChange" />
					</div>
				</div>
			</template>
			<el-form :model="state.form" label-width="110px" class="op-form" @submit.prevent>
				<el-form-item label="用户名" required>
					<el-input v-model="state.form.User" type="textarea" :rows="2" clearable placeholder="多个用户用英文逗号(,)分隔，一次最多1000个" />
				</el-form-item>
				<el-form-item label="加款类型" required>
					<el-select v-model="state.form.Type" filterable style="width: 240px">
						<el-option v-for="t in state.typeOptions" :key="t.value" :label="t.label" :value="t.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="货币类型" required>
					<el-select v-model="state.form.Currency" filterable style="width: 240px">
						<el-option v-for="c in state.currencyOptions" :key="String(c.key)" :label="c.value" :value="Number(c.key)" />
					</el-select>
				</el-form-item>
				<el-form-item label="加款金额" required>
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
				<el-form-item label="流水要求">
					<el-input v-model="state.form.Flow" clearable style="width: 120px" />
					<span class="unit">倍（0=可直接提现）</span>
				</el-form-item>
				<el-form-item label="最低流水">
					<el-input v-model="state.form.MinFlow" clearable style="width: 120px" />
					<span class="unit">元</span>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" :loading="state.loading" v-auth="'site:money:kuanxiangguanli:huiyuanjiakuan'" @click="onSubmit">提交</el-button>
					<el-button @click="resetForm">重置</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-dialog v-model="state.batchVisible" title="批量加款(文本)" width="720px" destroy-on-close>
			<p class="batch-tip">每行制表符分隔：用户ID、类型、流水号、金额、前端备注、流水倍、最低流水、币种（对齐旧 addmoneybatch）</p>
			<el-input v-model="state.batchText" type="textarea" :rows="14" placeholder="粘贴批量行…" />
			<template #footer>
				<el-button @click="state.batchVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.batchLoading" @click="onBatchSubmit">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="siteMoneyOperateGrant">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import { fetchCurrencyOptions } from '/@/api/user/member';
import { adminPost } from '/@/utils/admin-api';
import { ensureCurrencyEnum } from '/@/utils/admin-display';
import { normalizeMoneyTypeGroups } from '/@/utils/admin-money-type';

const excelRef = ref<HTMLInputElement | null>(null);

const grantTypes = [
	{ value: 250, label: '手工加款 (250)' },
	{ value: 200, label: '手动活动 (200)' },
	{ value: 3, label: '充值奖励 (3)' },
	{ value: 111, label: '代客充值 (111)' },
	{ value: 60, label: '返水 (60)' },
	{ value: 61, label: '手动返水 (61)' },
	{ value: 130, label: '代理返佣 (130)' },
	{ value: 131, label: '代理返点 (131)' },
];

const emptyForm = () => ({
	User: '',
	Type: 250 as number,
	Currency: 0 as number,
	Money: '',
	SourceID: '',
	Description: '',
	DescriptionA: '',
	Flow: '1',
	MinFlow: '0',
});

const state = reactive({
	loading: false,
	batchVisible: false,
	batchLoading: false,
	batchText: '',
	excelLoading: false,
	form: emptyForm(),
	typeOptions: grantTypes,
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
		state.typeOptions = grantTypes;
		showCaughtError(e, '加载类型失败');
	}
};

const onSubmit = async () => {
	const f = state.form;
	if (!f.User.trim() || !f.Money || !f.SourceID.trim() || !f.Description.trim() || !f.DescriptionA.trim()) {
		ElMessage.warning('请填写用户名、金额、流水号与前后端备注');
		return;
	}
	if (!f.Type || f.Currency == null) {
		ElMessage.warning('请选择加款类型与货币');
		return;
	}
	try {
		await ElMessageBox.confirm(`确认对用户加款 ${f.Money}？`, '资金操作确认', { type: 'warning' });
		state.loading = true;
		const info = await adminPost('/admin/money/operate/addmoney', {
			User: f.User.trim(),
			Type: f.Type,
			Currency: f.Currency,
			Money: f.Money,
			SourceID: f.SourceID.trim(),
			Description: f.Description.trim(),
			DescriptionA: f.DescriptionA.trim(),
			Flow: f.Flow || '1',
			MinFlow: f.MinFlow || '0',
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

const onBatchSubmit = async () => {
	if (!state.batchText.trim()) {
		ElMessage.warning('请粘贴批量内容');
		return;
	}
	try {
		await ElMessageBox.confirm('确认提交批量加款？', '资金操作确认', { type: 'warning' });
		state.batchLoading = true;
		const info = await adminPost('/admin/money/operate/addmoneybatch', { Batch: state.batchText });
		const summary = (info as any)?.Summary || (info as any)?.summary;
		ElMessage.success(typeof summary === 'string' && summary ? summary : '已提交');
		state.batchVisible = false;
		state.batchText = '';
	} catch (e: any) {
		if (e !== 'cancel') showCaughtError(e);
	} finally {
		state.batchLoading = false;
	}
};

const onExcelChange = async (ev: Event) => {
	const input = ev.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = '';
	if (!file) return;
	try {
		await ElMessageBox.confirm(`确认上传 Excel「${file.name}」批量加款？`, '资金操作确认', { type: 'warning' });
		state.excelLoading = true;
		const fd = new FormData();
		fd.append('file', file);
		const info = await adminPost('/admin/money/operate/addmoneybatchbyexcel', fd);
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
.batch-tip {
	margin: 0 0 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
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
