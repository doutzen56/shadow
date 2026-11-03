import { ElMessage } from 'element-plus';

/**
 * 将含 color-mix() 的 CSS 色值解析为 rgb/rgba
 * canvas 类图表库只接受具体色值，无法解析 CSS 取色函数
 */
export function resolveCssColor(value: string): string {
	if (!value || !value.includes('color-mix(')) return value;
	const canvas = document.createElement('canvas');
	canvas.width = canvas.height = 1;
	const ctx = canvas.getContext('2d');
	if (!ctx) return value;
	ctx.fillStyle = value;
	ctx.fillRect(0, 0, 1, 1);
	const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
	return a === 255 ? `rgb(${r}, ${g}, ${b})` : `rgba(${r}, ${g}, ${b}, ${(a / 255).toFixed(3)})`;
}

/**
 * monaco 编辑器主题：跟随暗色开关，避免写死 vs / vs-dark 后与页面主题不一致
 * 读 html 的 data-theme 而非 store，便于在非组件上下文中调用
 */
export function getMonacoTheme(): string {
	return document.documentElement.getAttribute('data-theme') === 'dark' ? 'vs-dark' : 'vs';
}

/**
 * 颜色转换函数
 * @method hexToRgb hex 颜色转 rgb 颜色
 * @method rgbToHex rgb 颜色转 Hex 颜色
 * @method getDarkColor 加深颜色值
 * @method getLightColor 变浅颜色值
 */
export function useChangeColor() {
	// str 颜色值字符串
	const hexToRgb = (str: string): any => {
		let hexs: any = '';
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(str)) {
			ElMessage.warning('输入错误的hex');
			return '';
		}
		str = str.replace('#', '');
		hexs = str.match(/../g);
		for (let i = 0; i < 3; i++) hexs[i] = parseInt(hexs[i], 16);
		return hexs;
	};
	// r 代表红色 | g 代表绿色 | b 代表蓝色
	const rgbToHex = (r: any, g: any, b: any): string => {
		let reg = /^\d{1,3}$/;
		if (!reg.test(r) || !reg.test(g) || !reg.test(b)) {
			ElMessage.warning('输入错误的rgb颜色值');
			return '';
		}
		let hexs = [r.toString(16), g.toString(16), b.toString(16)];
		for (let i = 0; i < 3; i++) if (hexs[i].length == 1) hexs[i] = `0${hexs[i]}`;
		return `#${hexs.join('')}`;
	};
	// color 颜色值字符串 | level 变浅的程度，限0-1之间
	const getDarkColor = (color: string, level: number): string => {
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(color)) {
			ElMessage.warning('输入错误的hex颜色值');
			return '';
		}
		let rgb = useChangeColor().hexToRgb(color);
		for (let i = 0; i < 3; i++) rgb[i] = Math.floor(rgb[i] * (1 - level));
		return useChangeColor().rgbToHex(rgb[0], rgb[1], rgb[2]);
	};
	// color 颜色值字符串 | level 加深的程度，限0-1之间
	const getLightColor = (color: string, level: number): string => {
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(color)) {
			ElMessage.warning('输入错误的hex颜色值');
			return '';
		}
		let rgb = useChangeColor().hexToRgb(color);
		for (let i = 0; i < 3; i++) rgb[i] = Math.floor((255 - rgb[i]) * level + rgb[i]);
		return useChangeColor().rgbToHex(rgb[0], rgb[1], rgb[2]);
	};
	return {
		hexToRgb,
		rgbToHex,
		getDarkColor,
		getLightColor,
	};
}
