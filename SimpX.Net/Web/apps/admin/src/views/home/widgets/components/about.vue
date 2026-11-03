<template>
	<el-card shadow="hover">
		<template #header>
			<el-icon><ele-QuestionFilled /></el-icon>
			<span>关于项目</span>
		</template>
		<p class="about-text">
			基于 .NET9 (Furion/SqlSugar) 实现的通用权限开发框架，前端采用 Vue3+Element-plus+Vite5，整合众多优秀技术和框架，模块插件式开发。集成多租户、缓存、数据校验、鉴权、事件总线、动态API、通讯、远程请求、任务调度、打印等众多黑科技。代码结构简单清晰，注释详尽，易于上手与二次开发，即便是复杂业务逻辑也能迅速实现，真正实现开箱即用。
		</p>
		<div class="about-badges">
			<a href="https://gitee.com/zuohuaijun/SimpX.git" target="_blank">
				<img src="https://gitee.com/zuohuaijun/SimpX/badge/star.svg?theme=dark" alt="star" />
				<img src="https://gitee.com/zuohuaijun/SimpX/badge/fork.svg?theme=dark" alt="fork" />
				<img src="https://img.shields.io/badge/license-MIT-yellow" alt="license" />
			</a>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '关于',
	icon: 'ele-QuestionFilled',
	description: '点个星星支持一下',
};
</script>

<!-- 关于项目 -->
<script setup lang="ts" name="about"></script>

<style scoped lang="scss">
.about-text {
	margin: 0;
	color: var(--el-text-color-secondary);
	line-height: 1.8;
	font-size: 13px;
}

.about-badges {
	margin-top: 12px;

	img {
		vertical-align: middle;
		margin-right: 6px;
	}
}
</style>
