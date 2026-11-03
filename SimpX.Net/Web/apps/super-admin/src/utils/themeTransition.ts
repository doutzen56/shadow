import { Local } from './storage';
import { getMonacoTheme } from './theme';
import Watermark from './watermark';
import { useThemeConfig } from '/@/stores/themeConfig';
import { storeToRefs } from 'pinia';

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// html 根元素，后续主题属性 / CSS 变量 / class 都写在它身上
const root = document.documentElement;

/** 是否支持 View Transitions API（不支持则降级为直接切换，无扩散动画） */
const supportsViewTransition = typeof document !== 'undefined' && 'startViewTransition' in document;

/** 禁用全局过渡时挂载的 class 名（与全局 CSS 中的 .disable-transitions 对应） */
const TRANSITION_BLOCK_CLASS = 'disable-transitions';

/** 禁用全局过渡：给 html 挂上 class，配合 CSS 中的 `transition: none !important` 生效 */
const disableTransitions = () => root.classList.add(TRANSITION_BLOCK_CLASS);

/** 恢复全局过渡：移除 class，让后续交互恢复正常过渡效果 */
const enableTransitions = () => root.classList.remove(TRANSITION_BLOCK_CLASS);

/**
 * 将主题状态写入 html 根元素
 * - html class（dark / light）供样式与监听 class 变化的逻辑识别主题
 * - data-theme / data-vxe-ui-theme 属性供样式或第三方组件（如 VXE UI）识别主题
 * 两者必须同时落地，只写其中一种会让依赖另一种的消费者停留在旧主题
 * @param isDark 是否暗色
 */
export const applyThemeAttributes = (isDark: boolean) => {
	// 互斥切换，保留其他类名
	root.classList.toggle('dark', isDark);
	root.classList.toggle('light', !isDark);

	const themeName = isDark ? 'dark' : 'light';
	root.setAttribute('data-theme', themeName);
	root.setAttribute('data-vxe-ui-theme', themeName);

	// index.html 的首屏脚本在应用启动前执行，读不到带项目前缀的缓存键，这里另存一个固定键名的标记
	try {
		localStorage.setItem('app-theme', themeName);
	} catch (e) {
		// 隐私模式下 localStorage 可能不可写，首屏标记缺失只影响一次刷新前的底色
	}

	// 水印的明暗两档颜色是绘制时写进画布的，主题切换后必须重绘，否则暗色下会沿用亮色的深灰水印
	const { isWatermark, watermarkText } = themeConfig.value;
	if (isWatermark && watermarkText) Watermark.set(watermarkText);

	// 页面上已创建的 monaco 编辑器实时跟随主题（无编辑器时跳过，避免为空页面拉起 monaco chunk）
	if (document.querySelector('.monaco-editor')) {
		import('monaco-editor')
			.then(({ editor }) => editor.setTheme(getMonacoTheme()))
			.catch(() => {});
	}
};

/**
 * 执行主题切换（写入 DOM 属性）
 * - 切换过程中临时禁用全局过渡，避免其他元素出现不必要的补间
 */
const applyTheme = () => {
	// 切换期间禁用全局过渡，防止其他元素被"误伤"产生过渡动画
	disableTransitions();

	applyThemeAttributes(themeConfig.value.isIsDark);

	// 双 rAF 确保 DOM 更新与样式计算完成后再恢复过渡，
	// 避免在切换的同一帧内恢复导致过渡被触发
	requestAnimationFrame(() => {
		requestAnimationFrame(enableTransitions);
	});
};

/**
 * 主题切换动画入口：以鼠标位置为圆心做圆形扩散过渡
 * - 支持 View Transitions API：走圆形扩散动画
 * - 不支持：直接调用 applyTheme 完成切换
 * @param e 鼠标事件（用于获取点击坐标，作为扩散圆心）
 * @param isDark 是否切换为暗色
 */
export const themeTransition = (e: MouseEvent, isDark: boolean) => {
	// 先更新 store 中的主题状态，applyTheme 内部会读取它
	themeConfig.value.isIsDark = isDark;

	Local.remove('themeConfig');
	Local.set('themeConfig', storesThemeConfig.themeConfig);

	// 环境不支持 View Transitions API，直接切换，无动画
	if (!supportsViewTransition) {
		applyTheme();
		return;
	}

	// 鼠标点击位置，作为扩散圆心
	const { clientX: x, clientY: y } = e;

	// 鼠标位置到视窗最远角的距离，作为扩散圆最大半径
	// 取 x 方向、y 方向中较远的一侧，再求对角线长度
	const endRadius = Math.hypot(Math.max(x, window.innerWidth - x), Math.max(y, window.innerHeight - y));

	// 将圆心坐标与半径写入 CSS 变量，供 @keyframes clip 使用
	root.style.setProperty('--x', `${x}px`);
	root.style.setProperty('--y', `${y}px`);
	root.style.setProperty('--r', `${endRadius}px`);

	// 启动视图过渡，回调中执行实际主题切换
	document.startViewTransition(() => applyTheme());
};
