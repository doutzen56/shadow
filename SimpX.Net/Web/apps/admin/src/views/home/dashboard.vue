<template>
	<div class="home-dashboard-placeholder">
		<el-card shadow="hover">
			<template #header>
				<span>主页</span>
			</template>
			<p>SimpX 租户后台已用 SimpX 壳重建。</p>
			<p>本波只保证登录和菜单；业务页后续按接口重做。</p>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="siteHomeDashboard">
/**
 * SimpX 菜单组件路径 /home/dashboard 的占位页。
 * Pro 原首页在 /dashboard/home，与种子菜单不一致，先用本页承接。
 */
</script>

<style scoped>
.home-dashboard-placeholder {
	padding: 12px;
}
</style>
