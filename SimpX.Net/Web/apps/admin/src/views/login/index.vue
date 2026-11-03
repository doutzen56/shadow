<template>
	<!-- 登录页根容器：背景色随当前轮播图上下边色渐变 -->
	<div class="login-container" :style="{ background: state.currentBg }">
		<!-- 左侧：logo + 轮播图 -->
		<div class="login-left">
			<!-- 左上角 logo + 主/副标题 -->
			<div class="login-left-logo">
				<img :src="getThemeConfig.logoUrl" />
				<div class="login-left-logo-text">
					<span>{{ translateDynamic(getThemeConfig.globalViceTitle) }}</span>
					<span class="login-left-logo-text-msg">{{ translateDynamic(getThemeConfig.globalViceTitleMsg) }}</span>
				</div>
			</div>

			<!-- 轮播：每张图独立取上下边色；切换时同步背景渐变 -->
			<el-carousel height="600px" class="login-carousel" @change="onCarouselChange">
				<el-carousel-item v-for="(item, idx) in displayCarousels" :key="item.key">
					<!-- :key 变化驱动入场动画，与背景过渡同步 -->
					<div class="slide-wrap" :key="state.activeIndex === idx ? 'a' + idx : 'b' + idx">
						<img :src="item.url" class="login-icon-group-icon" />
					</div>
				</el-carousel-item>
			</el-carousel>
		</div>

		<!-- 右侧：登录表单区 -->
		<div class="login-right">
			<div class="login-right-warp">
				<div class="login-right-warp-main">
					<!-- 表单标题 -->
					<div class="login-right-warp-main-title">{{ translateDynamic(getThemeConfig.globalTitle) }}</div>

					<div class="login-right-warp-main-form">
						<!-- 账号 / 手机号 / 注册三种登录方式 -->
						<div v-if="!state.isScan">
							<el-tabs v-model="state.tabsActiveName" :key="getThemeConfig.globalI18n">
								<el-tab-pane label="用户名登录" name="account">
									<Account @openRegistr="showRegistr" />
								</el-tab-pane>
								<el-tab-pane v-if="themeConfig.mobileLogin" label="手机号登录" name="mobile">
									<Mobile @openRegistr="showRegistr" />
								</el-tab-pane>
								<el-tab-pane label="用户注册" name="register" v-if="state.isRegister">
									<Register :isRegister="state.isRegister" @goLogin="goLogin" />
								</el-tab-pane>
							</el-tabs>
						</div>
					</div>
				</div>
			</div>

			<!-- 颜色、主题、多语言切换 -->
			<div v-if="getThemeConfig.i18NSwitch" class="change-language">
				<div class="layout-navbars-breadcrumb-user-icon mr15" @click="themeTransition($event, !themeConfig.isIsDark)" style="cursor: pointer">
					<el-icon :title="'切换主题'" size="16">
						<ele-Sunny v-if="themeConfig.isIsDark" />
						<ele-Moon v-else />
					</el-icon>
				</div>
				<div style="cursor: pointer">
					<el-dropdown @command="onLanguageChange">
						<div class="layout-navbars-breadcrumb-user-icon">
							<!-- <span class="lang-flag" :class="`fi fi-${currentCountryCode}`"></span> -->
							<i class="ri-translate-2"></i>
						</div>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item v-for="(value, key) in languageList" :key="key" :command="key" :disabled="state.disabledI18n === key">
									<div class="flex items-center">
										<div class="mr-2">
											<!-- <span class="lang-flag" :class="`fi fi-${getCountryCode(key)}`"></span> -->
											<span style="display: inline-flex; align-items: center; justify-content: center; width: 16px; height: 16px; border: 1px solid; border-radius: 50%; font-size: 10px">
												{{ value.charAt(0) }}
											</span>
										</div>
										<div style="margin-left: 10px">{{ value }}</div>
									</div>
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</div>
			</div>
		</div>

		<!-- 底部版权信息 -->
		<div class="copyright" :class="[getThemeConfig.icp ? 'mb25' : 'mt5']">{{ getThemeConfig.copyright }}</div>

		<!-- 备案信息 -->
		<div v-if="getThemeConfig.icp" class="icp mt5">
			<el-link :href="getThemeConfig.icpUrl" target="_blank">{{ getThemeConfig.icp }}</el-link>
		</div>
	</div>
</template>

<!-- 登录页 -->
<script setup lang="ts" name="loginIndex">
import { defineAsyncComponent, onMounted, reactive, computed, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useThemeConfig } from '/@/stores/themeConfig';
import { NextLoading } from '/@/utils/loading';
import { themeTransition } from '/@/utils/themeTransition';

import { loadSysInfo } from '/@/utils/sysInfo';
import { Local } from '/@/utils/storage';
import { translateDynamic } from '/@/utils/other';
import { languageList, getCountryCode, getTrLang } from '/@/i18n';

// 本地轮播图资源
import login01 from '/@/assets/lg-01.jpg';
import login02 from '/@/assets/lg-02.jpg';
import login03 from '/@/assets/lg-03.jpg';
import login04 from '/@/assets/lg-04.jpg';

// 异步加载登录相关子组件（按需分块，减小首屏体积）
const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));
const Mobile = defineAsyncComponent(() => import('/@/views/login/component/mobile.vue'));
const Register = defineAsyncComponent(() => import('/@/views/login/component/register.vue'));

const { locale } = useI18n();
const route = useRoute();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
// 当前语言对应的国旗 code
const currentCountryCode = ref<string>(getCountryCode(themeConfig.value.globalI18n));

// 取色失败时的兜底色
const FALLBACK_EDGE = '#3a3c50';

/**
 * 单张轮播图数据结构
 * - topColor / bottomColor：图片上下两条边的平均色，用于生成背景渐变
 */
type Slide = { key: string; url: string; topColor: string; bottomColor: string };

// 组件内响应式状态
const state = reactive({
	tabsActiveName: 'account', // 当前激活的登录 tab
	isScan: false, // 是否处于扫码登录
	disabledI18n: 'zh-CN', // 当前语言（在下拉里置灰）
	isRegister: false, // 是否展开注册页
	currentBg: FALLBACK_EDGE, // 容器背景（线性/径向渐变）
	activeIndex: 0, // 当前激活的轮播索引，驱动 slide-wrap 的 :key
});

// 本地图片池（顺序对应 6 张图）
const localUrls = [login01, login02, login03, login04];
// 最终渲染用的数据源：默认用本地图；远端配置存在时被替换
const displayCarousels = ref<Slide[]>(localUrls.map((url, i) => ({ key: `lg-0${i + 1}`, url, topColor: FALLBACK_EDGE, bottomColor: FALLBACK_EDGE })));

/**
 * 提取图片上下两条边的平均色
 * 思路：把图片缩到 60x60 的 canvas，取最上 / 最下 3 行像素求平均
 * 结果用于生成"上=天空、下=沙漠"的竖向渐变背景
 */
const extractEdgeColors = (url: string): Promise<{ top: string; bottom: string }> => {
	return new Promise((resolve) => {
		const fallback = { top: FALLBACK_EDGE, bottom: FALLBACK_EDGE };
		const img = new Image();
		img.crossOrigin = 'anonymous';
		img.src = url;
		img.onload = () => {
			try {
				const canvas = document.createElement('canvas');
				const size = 60; // 采样画布边长
				canvas.width = size;
				canvas.height = size;
				const ctx = canvas.getContext('2d')!;
				ctx.drawImage(img, 0, 0, size, size);
				const { data } = ctx.getImageData(0, 0, size, size);

				const band = 3; // 每条边采样像素行数

				// 计算从 startY 行开始的 band 行像素平均色
				const avgBand = (startY: number): string => {
					let r = 0,
						g = 0,
						b = 0,
						count = 0;
					for (let y = startY; y < startY + band; y++) {
						for (let x = 0; x < size; x++) {
							const i = (y * size + x) * 4;
							r += data[i];
							g += data[i + 1];
							b += data[i + 2];
							count++;
						}
					}
					// ×0.9 略微压暗，避免边缘过亮和背景反差大
					return `rgb(${Math.round((r / count) * 0.9)}, ${Math.round((g / count) * 0.9)}, ${Math.round((b / count) * 0.9)})`;
				};

				resolve({ top: avgBand(0), bottom: avgBand(size - band) });
			} catch {
				resolve(fallback);
			}
		};
		img.onerror = () => resolve(fallback);
	});
};

// 并发给一组 Slide 填充上下边色
const fillEdgeColors = async (list: Slide[]) => {
	const colors = await Promise.all(list.map((s) => extractEdgeColors(s.url)));
	list.forEach((s, i) => {
		s.topColor = colors[i].top;
		s.bottomColor = colors[i].bottom;
	});
};

// 轮播切换：更新当前索引，并按当前图上下边色重算背景渐变
const onCarouselChange = (index: number) => {
	state.activeIndex = index;
	const item = displayCarousels.value[index];
	if (!item) {
		state.currentBg = FALLBACK_EDGE;
		return;
	}
	// 竖向渐变：上边色 → 下边色（中段 30%~70% 之间做过渡）
	state.currentBg = `linear-gradient(180deg, ${item.topColor} 0%, ${item.topColor} 30%, ${item.bottomColor} 70%, ${item.bottomColor} 100%)`;
};

// 打开「用户注册」：注册 tab 默认不渲染，点击后展开并切换过去
const showRegistr = () => {
	state.isRegister = true;
	state.tabsActiveName = 'register';
};

// 注册完成回到登录：切回账号 tab 并收起注册 tab
const goLogin = () => {
	state.isRegister = false;
	state.tabsActiveName = 'account';
};

// 模板里统一通过 getThemeConfig 访问主题配置
const getThemeConfig = computed(() => themeConfig.value);

// 页面加载时
onMounted(async () => {
	// 恢复本地存储的语言选中状态（否则刷新后下拉列表始终置灰默认的简体中文）
	if (Local.get('themeConfig')) initI18nOrSize('globalI18n', 'disabledI18n');
	// 处理租户 id：无则清除缓存；有效则写入 Local
	if (!route.query.tid) Local.remove('tid');
	let tenantid = Number(route.query.tid);
	if (isNaN(tenantid)) tenantid = 0;
	else if (tenantid > 99999) Local.set('tid', tenantid);
	await loadSysInfo(tenantid);

	// 后台配置了轮播图时，优先使用远端资源
	if (themeConfig.value.carouselFiles?.length) {
		displayCarousels.value = themeConfig.value.carouselFiles.map((f: any, i: number) => ({
			key: `remote-${i}`,
			url: f.url,
			topColor: FALLBACK_EDGE,
			bottomColor: FALLBACK_EDGE,
		}));
	}

	// 预取所有图的主色，并初始化第一张的背景
	await fillEdgeColors(displayCarousels.value);
	onCarouselChange(0);

	NextLoading.done(500);
});

// 切换语言：同步主题配置、缓存、当前国旗、i18n、全局翻译
const onLanguageChange = (lang: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalI18n = lang;
	Local.set('themeConfig', themeConfig.value);
	currentCountryCode.value = getCountryCode(lang);
	locale.value = lang;
	initI18nOrSize('globalI18n', 'disabledI18n');
	// 同步全局翻译系统并持久化，刷新后仍然生效
	const trLang = getTrLang(lang);
	window.$changeLang(trLang);
	window.localStorage.setItem('lang', trLang);
};

// 初始化组件大小/i18n
const initI18nOrSize = (value: string, attr: string) => {
	(<any>state)[attr] = Local.get('themeConfig')[value];
};
</script>

<style scoped lang="scss">
/*
 * 图片矩形 mask：
 * 两条 linear-gradient（to right / to bottom），用 mask-composite: intersect 取交集。
 * 效果：矩形四边柔化，但四角仍是直角（不是椭圆）。
 */
$mask-grad:
	linear-gradient(
		to right,
		transparent 0%,
		rgba(0, 0, 0, 0.1) 5%,
		rgba(0, 0, 0, 0.35) 11%,
		rgba(0, 0, 0, 0.7) 18%,
		rgba(0, 0, 0, 0.95) 26%,
		#000 34%,
		#000 66%,
		rgba(0, 0, 0, 0.95) 74%,
		rgba(0, 0, 0, 0.7) 82%,
		rgba(0, 0, 0, 0.35) 89%,
		rgba(0, 0, 0, 0.1) 95%,
		transparent 100%
	),
	linear-gradient(
		to bottom,
		transparent 0%,
		rgba(0, 0, 0, 0.1) 5%,
		rgba(0, 0, 0, 0.35) 11%,
		rgba(0, 0, 0, 0.7) 18%,
		rgba(0, 0, 0, 0.95) 26%,
		#000 34%,
		#000 66%,
		rgba(0, 0, 0, 0.95) 74%,
		rgba(0, 0, 0, 0.7) 82%,
		rgba(0, 0, 0, 0.35) 89%,
		rgba(0, 0, 0, 0.1) 95%,
		transparent 100%
	);

/* 图片入场动画：与容器背景渐变时长保持一致（0.9s ease-in-out） */
@keyframes slideFadeIn {
	from {
		opacity: 0;
	}
	to {
		opacity: 1;
	}
}

.login-container {
	display: flex;
	height: 100%;
	/* 背景色平滑过渡，与图片入场同频，避免"图先出现、背景后到" */
	transition: background 0.9s ease-in-out;

	/* ======================= 左侧区域 ======================= */
	.login-left {
		position: relative;
		width: 60%;
		height: 100%;
		overflow: hidden;

		/* 一层极淡的暗角：把大图边缘压下去，中间视觉焦点更聚拢 */
		&::after {
			content: '';
			position: absolute;
			inset: 0;
			z-index: 2;
			pointer-events: none;
			background: radial-gradient(circle at 34% 42%, transparent 0%, rgb(0 21 41 / 22%) 100%);
		}

		.login-carousel {
			position: relative;
			top: 50%;
			transform: translateY(-50%);
			z-index: 1;

			/* carousel 容器本身透明，避免默认背景形成矩形 */
			:deep(.el-carousel__container) {
				background: transparent;
			}

			/* 指示器提到最上层，避免被 slide-wrap 遮住 */
			:deep(.el-carousel__indicators) {
				z-index: 5;
			}

			/* 关闭 el-carousel 自带切换动画 */
			:deep(.el-carousel__item) {
				transition: none !important;
				animation: none !important;
			}
		}

		/* 每张图片外层容器：入场动画在这里，:key 变化时重播 */
		.slide-wrap {
			width: 100%;
			height: 100%;
			overflow: hidden;
			animation: slideFadeIn 0.9s ease-in-out both;
		}

		/* 图片本体：绝对定位铺满容器，用矩形 mask 柔化四边 */
		.login-icon-group-icon {
			position: absolute;
			inset: 0;
			width: 100%;
			height: 100%;
			object-fit: cover; /* 保持比例并裁剪，不留白边 */
			z-index: 1;

			-webkit-mask-image: $mask-grad;
			mask-image: $mask-grad;
			/* 两 mask 取交集：左右 + 上下同时淡出，四角保持直角 */
			-webkit-mask-composite: source-in;
			mask-composite: intersect;
		}

		/* 左上角 logo + 标题文字 */
		.login-left-logo {
			display: flex;
			align-items: center;
			position: absolute;
			top: 30px;
			left: 30px;
			z-index: 4;
			animation: logoAnimation 0.3s ease;

			img {
				height: 64px;
			}

			.login-left-logo-text {
				display: flex;
				flex-direction: column;

				span {
					margin-left: 20px;
					font-size: 28px;
					font-weight: 700;
					color: var(--next-color-white);
					/* 叠在图上的文字，加一点投影保证任何底图都读得清 */
					text-shadow: 0 2px 14px rgb(0 21 41 / 35%);
				}

				.login-left-logo-text-msg {
					padding-top: 5px;
					font-size: 14px;
					color: var(--next-color-white);
				}
			}
		}
	}

	/* ======================= 右侧区域 ======================= */
	.login-right {
		width: 40%;
		display: flex;
		align-items: center;
		justify-content: center;
		/* 底层白底 + 背景装饰图（bg.svg） */
		background: var(--el-bg-color);
		background-image: url('../../assets/bg.svg');
		background-size: 100% auto;
		background-position: 50% calc(50% - 15px);
		background-repeat: no-repeat;

		/* 表单卡片外框 */
		.login-right-warp {
			height: 600px;
			/* 表单区做成一张有质感的卡片，而不是直接浮在白底上 */
			border-radius: 16px;
			border: 1px solid var(--el-border-color-lighter);
			box-shadow: 0 18px 48px rgb(0 21 41 / 8%);
			background: var(--el-bg-color);
		}

		/* 表单主体：竖向排布，标题固定高度，内容 flex 撑满 */
		.login-right-warp-main {
			display: flex;
			flex-direction: column;
			height: 100%;
			border-radius: inherit;

			/* 表单标题 */
			.login-right-warp-main-title {
				position: relative;
				height: 110px;
				line-height: 110px;
				font-size: 30px;
				font-weight: 700;
				letter-spacing: 1px;
				text-align: center;
				animation: logoAnimation 0.3s ease;
				animation-delay: 0.3s;
				color: var(--el-color-primary);

				/*
				 * 标题下方装饰：中心菱形光点 + 两侧向两端渐隐的细线
				 * 用「点 + 渐隐线」替代原来一根实心粗横线，视觉上更轻、更有层次
				 */
				&::before {
					content: '';
					position: absolute;
					left: 50%;
					bottom: 15.5px;
					width: 6px;
					height: 6px;
					border-radius: 1.5px;
					transform: translateX(-50%) rotate(45deg);
					background: var(--el-color-primary);
					box-shadow: 0 0 8px -2px var(--el-color-primary);
				}

				/* 由中心向两端线性渐隐：线是菱形的光晕延伸，不做成一根独立横杠 */
				&::after {
					content: '';
					position: absolute;
					left: 50%;
					bottom: 17.5px;
					transform: translateX(-50%);
					width: 118px;
					height: 2px;
					background: linear-gradient(90deg, transparent 0%, var(--el-color-primary-light-3) 50%, transparent 100%);
				}
			}

			/* 表单内容区 */
			.login-right-warp-main-form {
				padding: 0 50px 50px;

				/* 登录方式 tab 居中 */
				:deep(.el-tabs__nav) {
					float: none;
					margin: 0 auto;
				}
			}
		}

		// 多语言
		.change-language {
			position: absolute;
			top: 30px;
			right: 30px;
			margin: 0;
			z-index: 1000;
			white-space: nowrap;

			> div {
				display: inline-block;
				vertical-align: middle;
				transition: transform 0.3s ease;
				transform-origin: center;
				&:hover {
					transform: scale(1.2);
				}
			}
		}
	}
}

/* 底部版权 / 备案：绝对定位底部居中 */
.copyright,
.icp {
	position: absolute;
	bottom: 2%;
	transform: translateX(-50%);
	white-space: nowrap;
}

/* ------------------------------ 响应式 ------------------------------*/
.login-container {
	/* 大屏（≥1200px）：卡片定宽，版权/备案偏右与表单列对齐 */
	@media screen and (min-width: 1200px) {
		.login-right-warp {
			width: 500px;
		}

		.copyright,
		.icp {
			left: 80%;
			color: var(--el-text-color-secondary);

			.el-link {
				color: var(--el-text-color-secondary);
			}
		}
	}

	/* 中屏（≤1200px）：左侧大图整块隐藏，只留登录卡片 */
	@media screen and (max-width: 1200px) {
		align-items: flex-start;
		overflow-y: auto;

		.login-left {
			display: none;
		}

		.login-right {
			width: 100%;
			min-height: 100%;
			background: transparent;
			padding: 40px 16px 96px;

			.login-right-warp {
				width: 100%;
				max-width: 500px;
				min-width: 0;
				height: auto;
			}
		}

		/* 深色渐变的底上，版权/备案用浅色，gray 次级色读不清 */
		.copyright,
		.icp {
			left: 50%;
			color: rgb(255 255 255 / 82%);
			z-index: 10;

			.el-link {
				color: rgb(255 255 255 / 82%);
			}
		}
	}

	/* 小屏（≤580px）：手机上不摆"浮起的卡片"，整屏就是表单本身 */
	@media screen and (max-width: 580px) {
		.login-right {
			align-items: flex-start;
			background: var(--el-bg-color);
			padding: 24px 12px 88px;

			.login-right-warp {
				max-width: 100%;
				border: none;
				box-shadow: none;
			}

			.login-right-warp-main {
				.login-right-warp-main-title {
					height: 88px;
					line-height: 88px;
					font-size: 22px;
				}

				.login-right-warp-main-form {
					padding: 0 18px 28px;
				}
			}
		}

		/* 白底回归，版权/备案回到次级文字色 */
		.copyright,
		.icp {
			color: var(--el-text-color-secondary);

			.el-link {
				color: var(--el-text-color-secondary);
			}
		}
	}
}
</style>
