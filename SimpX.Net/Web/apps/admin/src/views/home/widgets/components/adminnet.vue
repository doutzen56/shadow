<template>
	<el-card shadow="hover">
		<template #header>
			<el-icon><ele-Promotion /></el-icon>
			<span>SimpX</span>
		</template>
		<div class="welcome">
			<div class="logo">
				<h2>欢迎使用 {{ themeConfig.globalTitle }}</h2>
			</div>
			<div class="tips">
				<div class="tips-item">
					<div class="tips-item-icon">
						<el-icon><ele-Menu /></el-icon>
					</div>
					<div class="tips-item-message">这里是项目控制台，你可以点击右上方的“编辑布局”按钮来添加、移除或者移动部件。</div>
				</div>
				<div class="tips-item">
					<div class="tips-item-icon">
						<el-icon><ele-Promotion /></el-icon>
					</div>
					<div class="tips-item-message">在提高前端算力、减少带宽请求和代码执行力上多次优化，并且持续着。</div>
				</div>
				<div class="tips-item">
					<div class="tips-item-icon">
						<el-icon><ele-MilkTea /></el-icon>
					</div>
					<div class="tips-item-message">让开发更简单、更通用、更流行！</div>
				</div>
			</div>
			<div class="actions">
				<el-button type="primary" icon="ele-Guide" @click="godoc">文档教程</el-button>
			</div>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: 'SimpX',
	icon: 'ele-StarFilled',
	description: '项目特色以及文档链接',
};
</script>

<!-- SimpX -->
<script setup lang="ts" name="adminnet">
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const godoc = () => {
	window.open('https://adminnet.top/');
};
</script>

<style scoped lang="scss">
.welcome .logo {
	text-align: center;
}

.welcome .logo h2 {
	margin: 4px 0 0;
	font-size: 22px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.tips {
	margin-top: 18px;
}

.tips-item {
	display: flex;
	align-items: center;
	padding: 8px 0;
}

.tips-item-icon {
	flex: 0 0 auto;
	width: 34px;
	height: 34px;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 10px;
	font-size: 16px;
	margin-right: 12px;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

.tips-item-message {
	flex: 1;
	min-width: 0;
	font-size: 13px;
	color: var(--el-text-color-regular);
	line-height: 1.6;
}

.actions {
	text-align: center;
	margin: 22px 0 4px;
}
</style>
