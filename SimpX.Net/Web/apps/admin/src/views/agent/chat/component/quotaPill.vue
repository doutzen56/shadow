<template>
	<!-- P1-6 Token 用量：后端结算后推来本轮消耗与两个维度的已用/上限；用尽时显示常驻红色提示 -->
	<template v-if="quota.enabled">
		<el-tooltip placement="bottom" effect="light">
			<template #content>
				<div>本轮消耗：{{ quota.lastTotal }} tokens</div>
				<div>本对话累计：{{ quota.sessionUsed }} / {{ quota.sessionLimit || '不限' }}</div>
				<div>租户今日累计：{{ quota.tenantDailyUsed }} / {{ quota.tenantDailyLimit || '不限' }}</div>
			</template>
			<div class="quota-pill" :class="{ 'is-danger': percent >= 80 }">
				<el-icon><ele-Coin /></el-icon>
				<span>本轮 {{ quota.lastTotal }}</span>
				<span v-if="quota.sessionLimit">· 对话 {{ quota.sessionUsed }}/{{ quota.sessionLimit }}</span>
				<span v-if="quota.tenantDailyLimit">· 今日 {{ quota.tenantDailyUsed }}/{{ quota.tenantDailyLimit }}</span>
			</div>
		</el-tooltip>
		<div class="quota-exceeded" v-if="quota.exceeded">
			<el-icon><ele-WarningFilled /></el-icon>
			<span>{{ quota.exceededMessage }}</span>
		</div>
	</template>
</template>

<script lang="ts" setup name="agentQuotaPill">
import type { TokenQuotaView } from '../types';

defineProps<{
	/** 后端 tokenUsage 事件推送的用量快照 */
	quota: TokenQuotaView;
	/** 用量紧张度百分比（取两个维度里更紧张的一个），≥80 转警示色 */
	percent: number;
}>();
</script>

<style scoped>
/* 用量胶囊：默认中性灰，逼近上限（≥80%）转警示色，提醒用户及时新建对话 */
.quota-pill {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 3px 10px;
	border-radius: 12px;
	background-color: var(--el-fill-color-light);
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	transition:
		background-color 0.2s ease,
		color 0.2s ease;
}

.quota-pill.is-danger {
	background-color: var(--el-color-danger-light-9);
	color: var(--el-color-danger);
}

/* 超限提示：红色底让「为什么发不出去」一眼可见，下一轮成功后自动消失 */
.quota-exceeded {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 3px 10px;
	border-radius: 12px;
	background-color: var(--el-color-danger-light-9);
	font-size: 12px;
	line-height: 18px;
	color: var(--el-color-danger);
}
</style>
