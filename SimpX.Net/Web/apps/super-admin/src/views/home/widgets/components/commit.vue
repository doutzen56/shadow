<template>
	<el-card class="commit-card" shadow="hover">
		<template #header>
			<el-icon><ele-DocumentCopy /></el-icon>
			<span>更新记录</span>
			<el-button type="primary" icon="ele-Refresh" round plain size="small" :loading="state.loading" @click="refresh">刷新</el-button>
		</template>
		<div class="commit" v-loading="state.loading">
			<el-timeline v-if="state.list.length > 0">
				<el-timeline-item v-for="(item, index) in state.list" :key="index" :timestamp="formatDate(new Date(item.commit.committer.date), 'YYYY-mm-dd HH:MM:SS')">
					<el-link class="commit-msg" :href="item.html_url" target="_blank"> {{ item.commit.message }}</el-link>
				</el-timeline-item>
			</el-timeline>
			<div v-else-if="state.error" class="w-empty">
				<span class="w-empty-ico"
					><el-icon><ele-Warning /></el-icon
				></span>
				<span class="w-empty-text">更新记录加载失败，请稍后重试</span>
			</div>
			<div v-else class="w-empty">
				<span class="w-empty-ico"
					><el-icon><ele-Refresh /></el-icon
				></span>
				<span class="w-empty-text">暂无更新记录</span>
			</div>
		</div>
	</el-card>
</template>

<script lang="ts">
export default {
	title: '更新记录',
	icon: 'ele-DocumentCopy',
	description: '当前项目更新记录',
};
</script>

<!-- 更新记录 -->
<script setup lang="ts" name="commit">
import { reactive, onMounted } from 'vue';
import { formatDate } from '/@/utils/formatTime';
import axios from 'axios';

// 独立实例：外部 Gitee 请求不应走项目全局 axios 拦截器（避免触发登录/鉴权逻辑造成页面跳转）
const http = axios.create({ timeout: 10000 });

// Gitee 公开 commits 接口（按需替换为你自己的仓库路径）
const COMMIT_API = 'https://gitee.com/api/v5/repos/zuohuaijun/SimpX/commits';

const state = reactive({
	loading: false,
	list: [] as any[],
	error: false,
});

onMounted(() => {
	getList();
});

// 获取更新记录数据
const getList = async () => {
	if (state.loading) return; // 防止重复并发请求
	state.loading = true;
	state.error = false;
	try {
		const res = await http.get(COMMIT_API, {
			params: { page: 1, per_page: 10 },
		});
		// 仅当返回为数组时才赋值，避免错误响应（对象/HTML）导致渲染异常
		state.list = Array.isArray(res.data) ? res.data : [];
	} catch (e) {
		state.list = [];
		state.error = true;
		console.warn('[更新记录] 获取 Gitee 提交记录失败：', e);
	} finally {
		state.loading = false; // 不论成败都结束加载，杜绝永久转圈
	}
};

// 刷新
const refresh = () => {
	getList();
};
</script>

<style scoped lang="scss">
.commit {
	height: var(--next-widget-height, 300px);
	overflow: auto;
	padding-left: 2px;
}

.commit-msg {
	white-space: pre-line;
	word-break: break-all;
}
</style>
