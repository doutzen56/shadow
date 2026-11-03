<template>
	<div class="jf-approval">
		<div v-if="!records.length" class="jf-empty">暂无审批记录</div>
		<template v-else>
			<ul class="jf-timeline">
				<li v-for="(r, i) in records" :key="i" class="jf-timeline__item">
					<span class="jf-timeline__dot" :class="`jf-timeline__dot--${taskStateBadgeType(r.taskState)}`"></span>
					<div class="jf-timeline__content">
						<div class="jf-timeline__head">
							<strong>{{ r.displayName || r.taskName }}</strong>
							<span class="jf-muted">{{ fmtTime(r.finishTime, true) }}</span>
						</div>
						<div class="jf-timeline__meta">
							<span>{{ operatorOf(r) }}</span>
							<JfBadge :type="submitBadge(r)">{{ submitTypeLabel(submitOf(r)) }}</JfBadge>
							<span v-if="commentOf(r) !== '-'" class="jf-muted">{{ commentOf(r) }}</span>
						</div>
					</div>
				</li>
			</ul>
			<el-table :data="records" stripe border style="width: 100%; margin-top: 16px">
				<el-table-column label="节点" min-width="140">
					<template #default="{ row }">
						<strong>{{ row.displayName || row.taskName }}</strong>
					</template>
				</el-table-column>
				<el-table-column label="处理人" min-width="120">
					<template #default="{ row }">{{ operatorOf(row) }}</template>
				</el-table-column>
				<el-table-column label="手写体签名" min-width="160">
					<template #default="{ row }">
						<img v-if="signatureOf(row)" :src="signatureOf(row)!" class="jf-sign" alt="签名" />
						<span v-else class="jf-muted">-</span>
					</template>
				</el-table-column>
				<el-table-column label="操作" min-width="100">
					<template #default="{ row }">
						<JfBadge :type="submitBadge(row)">{{ submitTypeLabel(submitOf(row)) }}</JfBadge>
					</template>
				</el-table-column>
				<el-table-column label="意见" min-width="160">
					<template #default="{ row }"
						><span class="jf-muted">{{ commentOf(row) }}</span></template
					>
				</el-table-column>
				<el-table-column label="时间" min-width="160">
					<template #default="{ row }"
						><span class="jf-muted">{{ fmtTime(row.finishTime, true) }}</span></template
					>
				</el-table-column>
			</el-table>
		</template>
	</div>
</template>

<script setup lang="ts">
import JfBadge from './JfBadge.vue';
import { hostAdapters } from '../adapters';
import { fmtTime, submitTypeLabel, taskStateBadgeType } from '../helpers';
import type { ApprovalRecordRow } from '../types';

defineOptions({ name: 'JfApprovalRecord' });

defineProps<{
	records: ApprovalRecordRow[];
}>();

// 后端 execute 不写 u_*（只有发起节点写），缺姓名时用 operator 这个 id 反查姓名；查不到就还是显示 operator
function operatorOf(r: ApprovalRecordRow): string {
	if (r.ext?.u_realName) return String(r.ext.u_realName);
	const op = String(r.operator ?? '');
	if (!op || op === '-') return '-';
	// 走用户缓存反查（首次拉一次，之后零请求）；查到就写回这条记录，响应式刷新成姓名
	hostAdapters.getUsersByIds?.([op]).then(([u]) => {
		if (u?.realName) r.ext = { ...(r.ext ?? {}), u_realName: u.realName };
	});
	return op;
}

function submitOf(r: ApprovalRecordRow): number | string | null | undefined {
	return r.ext?.submitType ?? r.variable?.submitType;
}

// 各阶段手写体签名：后端按节点回写的任务/业务变量；兼容 tf_signature（审批）/ f_signature（发起）/ 裸 signature
function signatureOf(r: ApprovalRecordRow): string {
	const v =
		r.ext?.tf_signature ?? r.variable?.tf_signature ??
		r.ext?.f_signature ?? r.variable?.f_signature ??
		r.ext?.signature ?? r.variable?.signature;
	return v != null && v !== '' ? String(v) : '';
}

function commentOf(r: ApprovalRecordRow): string {
	const v = r.variable?.tf_approvalComment ?? r.ext?.tf_approvalComment;
	return v != null && v !== '' ? String(v) : '-';
}

function submitBadge(r: ApprovalRecordRow): 'doing' | 'done' | 'reject' | 'info' {
	const s = Number(submitOf(r));
	if (s === 2 || s === 20) return 'reject';
	if (s === 1 || s === 0 || s === 5) return 'done';
	return 'info';
}
</script>
