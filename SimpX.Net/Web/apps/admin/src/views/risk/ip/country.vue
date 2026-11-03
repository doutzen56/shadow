<template>
	<!-- 规格：Web.Admin risk/ip/country.html — 国别勾选即时保存 -->
	<div class="risk-page admin-page">
		<div class="query-toolbar">
			<div class="query-actions">
				<el-button type="primary" icon="ele-Refresh" :loading="state.loading" v-auth="'site:risk:ip:country'" @click="load">刷新</el-button>
			</div>
		</div>
		<el-card shadow="hover" v-loading="state.loading">
			<div v-for="g in regionGroups" :key="g.label" class="region-group">
				<h3 class="region-title">{{ g.label }}</h3>
				<div class="region-checks">
					<el-checkbox
						v-for="name in g.items"
						:key="name"
						:model-value="!!state.selected[name]"
						@change="(v: string | number | boolean) => onToggle(name, !!v)"
					>
						{{ name }}
					</el-checkbox>
				</div>
			</div>
			<el-empty v-if="!state.loading && !Object.keys(state.selected).length && false" />
		</el-card>
	</div>
</template>

<script setup lang="ts" name="siteRiskIpCountry">
import { onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { showCaughtError } from '/@/utils/axios-utils';
import { adminPost } from '/@/utils/admin-api';
import { fetchRiskBlackRegion } from '/@/api/risk/index';

/** 对齐旧站 country 分组文案（展示用；实际以接口返回勾选为准）。 */
const regionGroups = [
	{ label: '亚洲', items: ['日本', '韩国', '菲律宾', '泰国', '柬埔寨', '越南', '印度尼西亚', '朝鲜', '新加坡', '香港', '澳门', '台湾'] },
	{ label: '美洲', items: ['美国', '加拿大'] },
	{ label: '欧洲', items: ['英国', '法国', '德国', '瓜德罗普', '马提尼克岛', '法属圭亚那', '留尼汪岛'] },
	{ label: '大洋洲', items: ['澳大利亚', '新西兰'] },
	{
		label: '中国',
		items: [
			'北京', '天津', '上海', '重庆', '河北', '山西', '辽宁', '吉林', '黑龙江', '江苏', '浙江', '安徽', '福建', '江西', '山东',
			'河南', '湖北', '湖南', '广东', '海南', '四川', '贵州', '云南', '陕西', '甘肃', '青海', '台湾', '内蒙古', '广西', '西藏', '宁夏', '新疆',
		],
	},
];

const state = reactive({
	loading: false,
	selected: {} as Record<string, boolean>,
});

const load = async () => {
	state.loading = true;
	try {
		const info = await fetchRiskBlackRegion({});
		const list = Array.isArray(info)
			? info
			: Array.isArray((info as any)?.Regions)
				? (info as any).Regions
				: Array.isArray((info as any)?.list)
					? (info as any).list
					: [];
		const next: Record<string, boolean> = {};
		for (const item of list as any[]) {
			const name = typeof item === 'string' ? item : String(item?.Name ?? item?.Value ?? item ?? '');
			if (name) next[name] = true;
		}
		state.selected = next;
	} catch (e: any) {
		state.selected = {};
		showCaughtError(e);
	} finally {
		state.loading = false;
	}
};

/**
 * 勾选即时保存。对齐 saveblackregion Value + Save。
 * @param name 地区名
 * @param on 是否加入黑名单
 */
const onToggle = async (name: string, on: boolean) => {
	const prev = !!state.selected[name];
	state.selected[name] = on;
	try {
		await adminPost('/admin/risk/ip/saveblackregion', { Value: name, Save: on ? 1 : 0 });
		ElMessage.success(on ? `已拉黑 ${name}` : `已取消 ${name}`);
	} catch (e: any) {
		state.selected[name] = prev;
		showCaughtError(e);
	}
};

onMounted(() => {
	void load();
});
</script>

<style scoped>
.region-group {
	margin-bottom: 20px;
}
.region-title {
	margin: 0 0 10px;
	font-size: 15px;
}
.region-checks {
	display: flex;
	flex-wrap: wrap;
	gap: 8px 16px;
}
</style>
