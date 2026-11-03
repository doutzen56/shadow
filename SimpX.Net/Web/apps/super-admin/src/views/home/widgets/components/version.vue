<template>
	<el-card shadow="hover">
		<template #header>
			<el-icon><ele-InfoFilled /></el-icon>
			<span>版本信息</span>
		</template>
		<div class="ver-body">
			<img class="ver-img" :src="verSvg" alt="version" />
			<h2 class="ver-name">SimpX</h2>
			<p class="ver-num">最新版本 {{ version }}</p>
			<div class="ver-acts">
				<el-button type="primary" icon="ele-DocumentCopy" plain round size="small" @click="golog">更新日志</el-button>
				<el-button type="primary" icon="ele-Eleme" plain round size="small" @click="gogit">gitee</el-button>
			</div>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '版本信息',
	icon: 'ele-InfoFilled',
	description: '版本信息',
};
</script>

<!-- 版本信息 -->
<script setup lang="ts" name="version">
import { ref, onMounted } from 'vue';
import verSvg from '/@/assets/version.svg';

const version = ref<string>('loading...');

onMounted(() => {
	version.value = 'v2.0.0';
});

const golog = () => {
	window.open('https://gitee.com/zuohuaijun/SimpX/issues');
};

const gogit = () => {
	window.open('https://gitee.com/zuohuaijun/SimpX.git');
};
</script>

<style scoped lang="scss">
.ver-body {
	display: flex;
	flex-direction: column;
	align-items: center;
	padding: 6px 0 2px;
}

.ver-img {
	height: 130px;
}

.ver-name {
	margin: 12px 0 0;
	font-size: 20px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.ver-num {
	margin: 6px 0 0;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.ver-acts {
	margin-top: 14px;
	display: flex;
	gap: 8px;
}
</style>
