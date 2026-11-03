import { nextTick } from 'vue';
import * as svg from '@element-plus/icons-vue';
// import 本地样式类名数组
import { iconfonntClassList } from '/@/theme/iconfont/font_2298093_rnp72ifj3ba';
import { fontAwesomeClassList } from '/@/theme/font-awesome/font-awesome';

// 获取阿里字体图标
const getAlicdnIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const styles: any = document.styleSheets;
			let sheetsList = [];
			let sheetsIconList = [];
			for (let i = 0; i < styles.length; i++) {
				if (styles[i].href && styles[i].href.indexOf('at.alicdn.com') > -1) {
					sheetsList.push(styles[i]);
				}
			}
			for (let i = 0; i < sheetsList.length; i++) {
				for (let j = 0; j < sheetsList[i].cssRules.length; j++) {
					if (sheetsList[i].cssRules[j].selectorText && sheetsList[i].cssRules[j].selectorText.indexOf('.icon-') > -1) {
						sheetsIconList.push(`${sheetsList[i].cssRules[j].selectorText.substring(1, sheetsList[i].cssRules[j].selectorText.length).replace(/\:\:before/gi, '')}`);
					}
				}
			}
			// 从本地读取阿里字体
			if (sheetsIconList.length == 0) {
				for (let i = 0; i < iconfonntClassList.length; i++) {
					sheetsIconList.push(iconfonntClassList[i]);
				}
			}

			if (sheetsIconList.length > 0) resolve(sheetsIconList);
			else reject('未获取到值，请刷新重试');
		});
	});
};

// 初始化获取 css 样式，获取 element plus 自带 svg 图标，增加了 ele- 前缀，使用时：ele-Aim
const getElementPlusIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const icons = svg as any;
			const sheetsIconList = [];
			for (const i in icons) {
				sheetsIconList.push(`ele-${icons[i].name}`);
			}
			if (sheetsIconList.length > 0) resolve(sheetsIconList);
			else reject('未获取到值，请刷新重试');
		});
	});
};

// 初始化获取 css 样式，这里使用 fontawesome 的图标
const getAwesomeIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const styles: any = document.styleSheets;
			let sheetsList = [];
			let sheetsIconList = [];
			for (let i = 0; i < styles.length; i++) {
				if (styles[i].href && styles[i].href.indexOf('netdna.bootstrapcdn.com') > -1) {
					sheetsList.push(styles[i]);
				}
			}
			for (let i = 0; i < sheetsList.length; i++) {
				for (let j = 0; j < sheetsList[i].cssRules.length; j++) {
					if (sheetsList[i].cssRules[j].selectorText && sheetsList[i].cssRules[j].selectorText.indexOf('.fa-') === 0 && sheetsList[i].cssRules[j].selectorText.indexOf(',') === -1) {
						if (/::before/.test(sheetsList[i].cssRules[j].selectorText)) {
							sheetsIconList.push(`${sheetsList[i].cssRules[j].selectorText.substring(1, sheetsList[i].cssRules[j].selectorText.length).replace(/\:\:before/gi, '')}`);
						}
					}
				}
			}
			// 从本地读取阿里字体
			if (sheetsIconList.length == 0) {
				for (let i = 0; i < fontAwesomeClassList.length; i++) {
					sheetsIconList.push(fontAwesomeClassList[i]);
				}
			}

			if (sheetsIconList.length > 0) resolve(sheetsIconList.reverse());
			else reject('未获取到值，请刷新重试');
		});
	});
};

// 获取 remixicon 全部图标
// 运行时从已加载的 remixicon.css 中读取，无需再维护本地类名列表（换版改 css 即可）
const getRemixIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const styles: any = document.styleSheets;
			const sheetsIconList: string[] = [];
			const sheetsIconMap: any = {};
			// 兼容单/双冒号（不同浏览器 CSSOM 序列化结果不一致），只匹配带伪元素的图标规则，
			// 天然排除 .ri-lg / .ri-1x 这类无伪元素的尺寸类
			const iconReg = /\.ri-([a-z0-9-]+)::?(?:before|after)/g;
			for (let i = 0; i < styles.length; i++) {
				// 跨域样式表读取 cssRules 会抛错，跳过
				let rules: any;
				try {
					rules = styles[i].cssRules;
				} catch (e) {
					continue;
				}
				if (!rules) continue;
				for (let j = 0; j < rules.length; j++) {
					const selectorText = rules[j].selectorText;
					if (!selectorText || selectorText.indexOf('.ri-') === -1) continue;
					iconReg.lastIndex = 0;
					let matched: RegExpExecArray | null;
					while ((matched = iconReg.exec(selectorText))) {
						const icon = `ri-${matched[1]}`;
						if (sheetsIconMap[icon]) continue;
						sheetsIconMap[icon] = true;
						sheetsIconList.push(icon);
					}
				}
			}
			if (sheetsIconList.length > 0) resolve(sheetsIconList);
			else reject('未获取到值，请刷新重试');
		});
	});
};

/**
 * 获取字体图标 `document.styleSheets`
 * @method ali 获取阿里字体图标 `<i class="iconfont 图标类名"></i>`
 * @method ele 获取 element plus 自带图标 `<i class="图标类名"></i>`
 * @method ali 获取 fontawesome 的图标 `<i class="fa 图标类名"></i>`
 * @method remix 获取 remixicon 全部图标 `<SvgIcon name="ri-图标名" />`
 */
const initIconfont = {
	// iconfont
	ali: () => {
		return getAlicdnIconfont();
	},
	// element plus
	ele: () => {
		return getElementPlusIconfont();
	},
	// fontawesome
	awe: () => {
		return getAwesomeIconfont();
	},
	// remixicon
	remix: () => {
		return getRemixIconfont();
	},
};

// 导出方法
export default initIconfont;
