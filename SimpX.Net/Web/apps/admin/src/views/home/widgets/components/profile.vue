<template>
	<!-- <el-card class="profile-card" shadow="hover" :body-style="{ padding: '5px' }">
		<template #header>
			<el-icon style="display: inline; vertical-align: middle"> <ele-User /> </el-icon>
			<span> 我的资料 </span>
		</template> -->
	<div class="account-center-avatarHolder backDown-enter-active" @dblclick="goToUserCenter">
		<div class="banner" ref="bannerRef">
			<div class="wave wave-1"></div>
		</div>
		<!-- 顶部：头像 + 姓名 + 角色 -->
		<div class="profile-header">
			<!-- 头像：无头像时回落到图标，避免出现裂图 -->
			<el-avatar :size="40" :src="userInfos.avatar" class="avatar">
				<el-icon><ele-User /></el-icon>
			</el-avatar>

			<!-- 姓名与角色 -->
			<div class="profile-info">
				<div class="name-row">
					<span class="name"> {{ userInfos.realName == '' ? userInfos.account : userInfos.realName }}</span>
					<!-- <el-icon class="badge-icon" :size="16"><CircleCheckFilled /></el-icon> -->
				</div>
				<div class="role">{{ userInfos.account }}</div>
			</div>
		</div>

		<!-- 下方：信息列表 -->
		<div class="info-list">
			<!-- <el-tag round> {{ userInfos.orgName ?? '机构' }} </el-tag> -->
			<el-tag round> {{ userInfos.posName ?? '职位' }} </el-tag>
			<el-tag type="warning" round> {{ userInfos.jobNum ?? '工号' }} </el-tag>
			<el-tag round>{{ userInfos.email ?? '邮箱' }}</el-tag>
			<el-tag type="warning" round>{{ userInfos.phone ?? '电话' }} </el-tag>
			<el-tag round>{{ userInfos.idCardNum ?? '证件号码' }} </el-tag>
			<el-tag type="info" round>{{ userInfos.lastChangePasswordTime ?? '密码修改时间' }} </el-tag>
			<!-- <el-tag round>{{ userInfos.points ?? '积分' }}</el-tag> -->
		</div>
	</div>
	<!-- </el-card> -->
</template>

<script lang="ts">
export default {
	title: '我的资料',
	icon: 'ele-User',
	description: '我的资料',
};
</script>

<!-- 我的资料 -->
<script setup lang="ts" name="profile">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';

const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);
const router = useRouter();
const bannerRef = ref<HTMLDivElement>();

onMounted(() => {
	// 生成气泡
	drawBubble();
});

// 随机生成气泡
const drawBubble = () => {
	const el = bannerRef.value;
	if (!el) return;

	// 容器高度，用于计算气泡上浮的总距离
	const H = el.clientHeight;
	// 气泡数量（越多越热闹，但性能开销也越大）
	const COUNT = 10;
	for (let i = 0; i < COUNT; i++) {
		// 每个气泡是一个 <span class="bubble">
		const b = document.createElement('span');
		b.className = 'bubble';

		// 气泡大小：8 ~ 24px（随机，大小不一更自然）
		const size = 8 + Math.random() * 16;
		// 上浮总距离：容器高度 + 20px，再随机减 0~20px
		// 让每个气泡到达的高度略有差异，避免整齐划一
		const travel = H + 20 - Math.random() * 20;

		// 一次性写入所有内联样式
		//  - left：水平位置随机（0~92%）
		//  - width / height：气泡尺寸
		//  - --travel：CSS 变量，供 @keyframes 里计算每段位移
		//  - animation-duration：6~10s，每个气泡速度不同
		//  - animation-delay：0~6s，错开出场时间
		b.style.cssText = `
    		left: ${Math.random() * 92}%;
    		width: ${size}px;
    		height: ${size}px;
    		--travel: ${travel}px;
    		animation-duration: ${6 + Math.random() * 4}s;
    		animation-delay: ${Math.random() * 6}s;
  		`;
		// 把气泡挂到 banner 容器里
		el.appendChild(b);
	}
};

// 双击跳转个人中心
const goToUserCenter = () => {
	router.push('/system/userCenter');
};
</script>

<style lang="scss" scoped>
.profile-card {
	border-radius: 10px;
	background-color: var(--el-bg-color-overlay);
	border: 1px solid var(--el-border-color-lighter);
}

.account-center-avatarHolder {
	position: relative;
	border-radius: 10px;
	// text-align: center;

	transition:
		transform 0.3s ease,
		box-shadow 0.3s ease;
	&:hover {
		transform: translateY(-2px);
		box-shadow: var(--el-box-shadow-light);
	}
	.banner {
		position: relative;
		height: 180px;
		overflow: hidden;
		border-radius: 10px;
		background:
			radial-gradient(ellipse at 50% 0%, var(--el-color-primary-light-7) 0%, transparent 40%),
			linear-gradient(to top, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 40%, var(--el-color-primary-light-5) 75%, var(--el-color-primary-light-7) 100%);
	}

	/* 暗色下主色实底会在深色页面里发亮：主色只取低比例压到页面底色上 */
	[data-theme='dark'] & .banner {
		background:
			radial-gradient(ellipse at 50% 0%, color-mix(in srgb, var(--el-color-primary) 18%, var(--el-bg-color)) 0%, transparent 40%),
			linear-gradient(
				to top,
				color-mix(in srgb, var(--el-color-primary) 34%, var(--el-bg-color)) 0%,
				color-mix(in srgb, var(--el-color-primary) 20%, var(--el-bg-color)) 100%
			);
	}

	// 气泡本体
	.banner :deep(.bubble) {
		position: absolute;
		bottom: -20px; /* 从容器底部外面开始，向上飘 */
		border-radius: 50%; /* 圆形 */

		/* 逼真球体：左上高光 + 透明中心 + 边缘亮环
	   用径向渐变模拟"玻璃球"的光学效果 */
		background: radial-gradient(
			circle at 30% 30%,
			rgba(255, 255, 255, 0.9) 0%,
			/* 光源在左上角 */ rgba(255, 255, 255, 0.25) 8%,
			/* 高光芯 */ rgba(255, 255, 255, 0.05) 35%,
			/* 高光外圈 */ rgba(255, 255, 255, 0.18) 70%,
			/* 内部几乎透明 */ rgba(255, 255, 255, 0.55) 90%,
			/* 球壁内侧 */ rgba(255, 255, 255, 0.9) 100% /* 最外圈一条细亮边 */
		);

		/* 立体感：外圈柔光 + 内壁亮反光 + 内壁暗边 */
		box-shadow:
			0 0 6px rgba(255, 255, 255, 0.35),
			/* 外层光晕 */ inset 0 0 4px rgba(255, 255, 255, 0.6),
			/* 内壁高光 */ inset 0 0 2px rgba(0, 0, 0, 0.08); /* 内壁暗边（模拟厚度） */

		border: 1px solid rgba(255, 255, 255, 0.45); /* 极细球壁线，增强轮廓 */

		/* 性能优化：强制 GPU 合成，动画只走 transform/opacity */
		will-change: transform, opacity;
		transform: translate3d(0, 0, 0);
		backface-visibility: hidden;

		animation: bubbleRise linear infinite; /* 应用上浮动画 */
		pointer-events: none; /* 不挡鼠标事件 */
	}

	// 左上高光点：模拟光源反射
	.banner :deep(.bubble)::before {
		content: '';
		position: absolute;
		top: 15%;
		left: 18%;
		width: 30%;
		height: 30%;
		border-radius: 50%;
		background: radial-gradient(circle at center, rgba(255, 255, 255, 1) 0%, /* 高光最亮点 */ rgba(255, 255, 255, 0.6) 40%, transparent 70% /* 向外自然淡出 */);
		pointer-events: none;
	}

	// 右下反光点：模拟球体背面的环境反光
	.banner :deep(.bubble)::after {
		content: '';
		position: absolute;
		bottom: 18%;
		right: 20%;
		width: 18%;
		height: 18%;
		border-radius: 50%;
		background: radial-gradient(circle at center, rgba(255, 255, 255, 0.8) 0%, transparent 70%);
		pointer-events: none;
	}

	// 上浮 + 左右摆动 + 破掉动画
	@keyframes bubbleRise {
		0% {
			/* 起点：从底部出发，缩小 + 完全透明 */
			transform: translate3d(0, 0, 0) scale(0.7);
			opacity: 0;
		}
		10% {
			/* 10% 处完全出现 */
			opacity: 0.95;
		}
		40% {
			/* 40% 行程：往右飘 + 略微缩小 */
			transform: translate3d(10px, calc(var(--travel) * -0.4), 0) scale(0.95);
			opacity: 0.95;
		}
		70% {
			/* 70% 行程：往左飘 + 恢复大小，形成左右摆动 */
			transform: translate3d(-8px, calc(var(--travel) * -0.7), 0) scale(1);
			opacity: 0.9;
		}
		92% {
			/* 破掉前一瞬间：突然放大 + 快速变淡 */
			transform: translate3d(0, calc(var(--travel) * -0.95), 0) scale(1.5);
			opacity: 0;
		}
		100% {
			/* 完全破掉：更大 + 完全透明，停在顶部分界线上 */
			transform: translate3d(0, calc(var(--travel) * -1), 0) scale(1.5);
			opacity: 0;
		}
	}

	// 水波纹
	.wave {
		position: absolute;
		bottom: 0;
		left: 0;
		width: 200%;
		height: 200px;
		background-repeat: repeat-x;
		background-size: 50% 100%;
		opacity: 0.8;
	}
	.wave-1 {
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1440 320' preserveAspectRatio='none'%3E%3Cpath fill='%23ffffff' fill-opacity='0.15' d='M0,192 C240,256 480,128 720,192 C960,256 1200,128 1440,192 L1440,320 L0,320 Z'%3E%3C/path%3E%3C/svg%3E");
		background-size: cover;
		animation: waveMove 5s linear infinite;
		height: 220px;
	}
	@keyframes waveMove {
		0% {
			transform: translate(0);
		}
		to {
			transform: translate(-50%);
		}
	}
}
/* 顶部：头像与姓名 */
.profile-header {
	display: flex;
	align-items: center;
	gap: 16px;
	margin-bottom: 20px;
	position: absolute;
	// bottom: 30px;
	top: 10px;
	left: 10px;
}

.avatar {
	width: 40px;
	height: 40px;
	border-radius: 50%;
	object-fit: cover;
	// border: 4px solid #fff;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.name {
	font-size: 18px;
	font-weight: bold;
	color: var(--el-text-color-primary);
}

.profile-info {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.name-row {
	display: flex;
	align-items: center;
	gap: 6px;
}

.name {
	font-size: 14px;
	font-weight: bold;
	color: var(--el-color-white);
	// text-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
}

.badge-icon {
	color: var(--el-color-primary);
}

.role {
	font-size: 14px;
	color: var(--el-color-white);
}

/* 信息列表：个人标签叠在 banner 背景上，右侧留边让标签在卡内换行而不溢出 */
.info-list {
	position: absolute;
	bottom: 8px;
	left: 12px;
	right: 12px;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	:deep(.el-tag) {
		font-size: 11px;
		max-width: 100%;
		margin-right: 6px;
		margin-bottom: 6px;
		/* 溢出时用省略号收尾，长字段（证件号/密码修改时间）不会撑破 banner */
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}
</style>
