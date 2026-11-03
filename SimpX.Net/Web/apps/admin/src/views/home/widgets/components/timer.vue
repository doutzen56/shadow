<template>
	<el-card shadow="hover">
		<template #header>
			<el-icon><ele-Clock /></el-icon>
			<span>当前时钟</span>
		</template>
		<div class="time">
			<div class="time-hm">{{ time }}</div>
			<div class="time-day">{{ day }}</div>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '当前时钟',
	icon: 'ele-Timer',
	description: '当前时钟',
};
</script>

<!-- 当前时钟 -->
<script setup lang="ts" name="timer">
import { ref, onMounted, onUnmounted } from 'vue';
import { formatDate } from '/@/utils/formatTime';

const time = ref<string>('');
const day = ref<string>('');
const timer = ref<any>(null);

onMounted(() => {
	showTime();
	timer.value = setInterval(() => {
		showTime();
	}, 1000);
});

onUnmounted(() => {
	clearInterval(timer.value);
});

const showTime = () => {
	time.value = formatDate(new Date(), 'HH:MM:SS');
	day.value = formatDate(new Date(), 'YYYY-mm-dd');
};
</script>

<style scoped lang="scss">
.time {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	padding: 18px 0;
}

.time-hm {
	font-size: 38px;
	font-weight: 700;
	line-height: 1;
	letter-spacing: 1px;
	color: var(--el-color-primary);
	font-variant-numeric: tabular-nums;
}

.time-day {
	font-size: 13px;
	color: var(--el-text-color-secondary);
	font-variant-numeric: tabular-nums;
}
</style>
