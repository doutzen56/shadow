<template>
	<div class="greet-container">
		<el-card class="welcome-card" shadow="never" :body-style="{ padding: '0', height: '100%' }">
			<div class="card-inner">
				<!-- 左侧：欢迎信息区域 -->
				<div class="left-section">
					<!-- 顶部问候语 -->
					<div class="greeting-header">
						<el-icon class="sun-icon" :size="24">
							<component :is="greetingIcon" />
						</el-icon>
						<span class="greeting-text">{{ currentTime }}，{{ userInfos.realName == '' ? userInfos.account : userInfos.realName }}，欢迎回来！</span>
					</div>

					<!-- 主标题 -->
					<h1 class="main-title">{{ welcomeMsg }}</h1>

					<!-- 副标题/提示 -->
					<p class="sub-text">
						您有 <span class="highlight">{{ pendingCount }}</span> 条待处理通知公告和 <span class="highlight">{{ approvalCount }}</span> 个待办或审批任务，
						{{ pendingCount > 0 || approvalCount > 0 ? '快去看看吧' : '先休息一会儿' }}。
					</p>
				</div>

				<!-- 右侧：仅显示插画图 -->
				<div class="right-section">
					<!-- 必须 import 引入：模板里直接写 `/@/assets/xxx` 在 dev 下不会被解析，图片会 404 -->
					<img class="greet-illustration" :src="homeIllustration" alt="" />
				</div>
			</div>
		</el-card>
	</div>
</template>

<script lang="ts">
export default {
	title: '欢迎',
	icon: 'ele-Promotion',
	description: '欢迎',
};
</script>

<!-- 欢迎 -->
<script lang="ts" setup name="welcome">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { storeToRefs } from 'pinia';
import { Sunny, Moon, Sunset } from '@element-plus/icons-vue';
import { useUserInfo } from '/@/stores/userInfo';
import { formatAxis } from '/@/utils/formatTime';

import homeIllustration from '/@/assets/home-01.png';

import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/system/api';
import { PageNoticeInput } from '/@/api-services/system/models';
import { GetTodoStats } from '/@/api/workflow';

const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);
const welcomeMsg = ref<string>();

// ============ 待处理 / 待审批 数量（示例变量，按需替换成接口数据） ============
const pendingCount = ref(0); // 待处理通知数
const approvalCount = ref(0); // 待审批任务数

const loginEncouragements = [
	'新的一天，新的开始，慢慢来，你已经在进步了。',
	'不必事事完美，尽力而为就已经很棒。',
	'你走过的每一步，都算数。',
	'今天也请相信：你比自己想象中更强大。',
	'工作再忙，也别忘了照顾自己的心情。',
	'保持热爱，奔赴山海，生活不会辜负认真的人。',
	'先完成，再完美；先行动，再焦虑。',
	'你不需要一下子改变所有，只需要比昨天好一点。',
	'每一次坚持，都是在为未来铺路。',
	'生活不会一直顺利，但你可以一直勇敢。',
	'愿你今天效率在线，心情也在线。',
	'把大事拆小，把小事做好，你就已经赢了一半。',
	'忙有所值，闲有所趣，愿你今天充实又自在。',
	'别急，好的事情正在慢慢发生。',
	'你已经做得很好了，记得给自己一点掌声。',
	'今天也要认真生活，努力工作，温柔待己。',
	'愿你眼里有光，心中有梦，脚下有路。',
	'所有看似不起波澜的日复一日，都会在某天让你看到坚持的意义。',
	'稳住心态，专注当下，你会离目标越来越近。',
	'欢迎回来，愿你今天诸事顺遂，所愿皆成。',
];

// 挂载后可从接口获取真实数据
onMounted(() => {
	welcomeMsg.value = loginEncouragements[Math.floor(Math.random() * loginEncouragements.length)];
	getNoticeList();
	getTaskList();
});

onUnmounted(() => {});
// 获取时间
const currentTime = computed(() => {
	return formatAxis(new Date());
});

// 根据时间段切换图标（可选）
const greetingIcon = computed(() => {
	const h = new Date().getHours();
	if (h < 6) return Moon; // 凌晨
	if (h < 18) return Sunny; // 白天用太阳
	if (h < 22) return Sunset; // 傍晚
	return Moon; // 深夜
});

const getNoticeList = async () => {
	const params: PageNoticeInput = {
		page: 1,
		pageSize: 50,
	};
	const res: any = await getAPI(SysNoticeApi).apiSysNoticePageReceivedPost(params);
	pendingCount.value = res.data.result.items.filter((item: any) => item.readStatus === 0).length;
};
// 获取统计数据
const getTaskList = async () => {
	const res = await GetTodoStats({});
	approvalCount.value = res.data.result.todoCount;
};
</script>

<style lang="scss" scoped>
/* 样式保持不变，只补充一个夜色图标颜色 */
.greet-container {
	width: 100%;
	height: 100%;
	box-sizing: border-box;
}

.welcome-card {
	width: 100%;
	height: 200px;
	border: none;
	border-radius: 10px;
	/* 深→主→浅三段渐变。原先用 color-mix() 混黑/混白，Chromium 102（可信浏览器）不支持
	   该函数，含它的整条 background 会被丢弃 → 卡片变透明、白字看不见。
	   改走 Element 动态色阶：dark-2 混黑 20%、light-3 混白 30%，随主题色实时变化 */
	background: linear-gradient(135deg, var(--el-color-primary-dark-2) 0%, var(--el-color-primary) 50%, var(--el-color-primary-light-3) 100%);
	overflow: hidden;
	position: relative;
	color: var(--el-color-white);
	&:hover {
		transform: translateY(-2px);
		box-shadow: var(--el-box-shadow-light);
	}
}

/* 暗色下大块主色实底会在深色页面里发亮：主色只取低比例压到页面底色上，
   既保住品牌色相，又让亮色主题色下的白字仍有足够对比度 */
[data-theme='dark'] .welcome-card {
	background:
		linear-gradient(135deg, rgb(0 0 0 / 58%) 0%, rgb(0 0 0 / 44%) 100%),
		linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary) 100%);
	background: linear-gradient(
		135deg,
		color-mix(in srgb, var(--el-color-primary) 34%, var(--el-bg-color)) 0%,
		color-mix(in srgb, var(--el-color-primary) 24%, var(--el-bg-color)) 100%
	);
}

.welcome-card::before,
.welcome-card::after {
	content: '';
	position: absolute;
	border-radius: 50%;
	/* 白色 5% 叠色：等价于 color-mix(white 5%, transparent)，
	   直接写 rgb 是为了避开 Chromium 102（可信浏览器）不支持 color-mix 的问题 */
	background: rgb(255 255 255 / 5%);
	z-index: 0;
}

.welcome-card::before {
	width: 200px;
	height: 200px;
	left: 40%;
	top: -20%;
}

.welcome-card::after {
	width: 120px;
	height: 120px;
	left: 50%;
	bottom: -10%;
	background: rgb(255 255 255 / 4%);
}

.card-inner {
	display: flex;
	height: 100%;
	width: 100%;
	position: relative;
	z-index: 1;
}

.left-section {
	flex: 1;
	display: flex;
	flex-direction: column;
	justify-content: center;
	padding: 0 0 0 24px;
	gap: 8px;
}

.greeting-header {
	display: flex;
	align-items: center;
	gap: 10px;
	margin-bottom: 8px;
}

.sun-icon {
	color: var(--el-color-warning);
}

.greeting-text {
	font-size: 14px;
}

.main-title {
	margin: 0;
	font-size: 25px;
	font-weight: bold;
	line-height: 1.2;
}

.sub-text {
	margin: 0;
	font-size: 14px;
}

.sub-text .highlight {
	font-weight: bold;
	margin: 0 2px;
}

.right-section {
	width: 30%;
	position: relative;
}

.greet-illustration {
	position: absolute;
	right: 10%;
	bottom: 0;
	height: 80%;
	width: auto;
	object-fit: contain;
	pointer-events: none;
	z-index: 2;
}
</style>
