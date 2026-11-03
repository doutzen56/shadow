// 通用函数
import useClipboard from 'vue-clipboard3';
import { ElMessage } from 'element-plus';
import { formatDate } from '/@/utils/formatTime';
import { useThemeConfig } from '/@/stores/themeConfig';
import { EmptyArrayType } from '/@/types/global';

import { getAPI } from '/@/utils/axios-utils';
import { SysCommonApi } from '/@/api-services/system/api';

const themeStore = useThemeConfig();
export default function () {
	const { toClipboard } = useClipboard();

	// 百分比格式化
	const percentFormat = (row: EmptyArrayType, column: number, cellValue: string) => {
		return cellValue ? `${cellValue}%` : '-';
	};
	// 列表日期时间格式化
	const dateFormatYMD = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		return formatDate(new Date(cellValue), 'YYYY-mm-dd');
	};
	// 列表日期时间格式化
	const dateFormatYMDHMS = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		return formatDate(new Date(cellValue), 'YYYY-mm-dd HH:MM:SS');
	};
	// 列表日期时间格式化
	const dateFormatHMS = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		let time = 0;
		if (typeof row === 'number') time = row;
		if (typeof cellValue === 'number') time = cellValue;
		return formatDate(new Date(time * 1000), 'HH:MM:SS');
	};
	// 小数格式化
	const scaleFormat = (value: string = '0', scale: number = 4) => {
		return Number.parseFloat(value).toFixed(scale);
	};
	// 小数格式化
	const scale2Format = (value: string = '0') => {
		return Number.parseFloat(value).toFixed(2);
	};
	// 千分符，默认保留两位小数
	const groupSeparator = (value: number, minimumFractionDigits: number = 2) => {
		return value.toLocaleString('en-US', {
			minimumFractionDigits: minimumFractionDigits,
			maximumFractionDigits: 2,
		});
	};
	// 点击复制文本
	const copyText = (text: string) => {
		return new Promise((resolve, reject) => {
			try {
				//复制
				toClipboard(text);
				//下面可以设置复制成功的提示框等操作
				ElMessage.success('复制成功！');
				resolve(text);
			} catch (e) {
				//复制失败
				ElMessage.error('复制失败！');
				reject(e);
			}
		});
	};
	// 去掉Html标签(取前面5个字符)
	const removeHtmlSub = (value: string) => {
		var str = value.replace(/<[^>]+>/g, '');
		if (str.length > 100) return str.substring(0, 100) + '......';
		else return str;
	};
	// 去掉Html标签
	const removeHtml = (value: string) => {
		return value.replace(/<[^>]+>/g, '');
	};
	// 获取枚举描述
	const getEnumDesc = (key: any, lstEnum: any) => {
		return lstEnum.find((x: any) => x.value == key)?.describe;
	};
	// 追加query参数到url
	const appendQueryParams = (url: string, params: { [key: string]: any }) => {
		if (!params || Object.keys(params).length == 0) return url;
		const queryString = Object.keys(params)
			.map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(params[key])}`)
			.join('&');
		return `${url}${url.includes('?') ? '&' : '?'}${queryString}`;
	};
	// 获取简称
	const getNameAbbr = (text: string, callback?: (abbr: any) => void) => {
		if (!text) return ElMessage.error('获取简称文本不能为空');
		try {
			return getAPI(SysCommonApi)
				.apiSysCommonNameAbbrPost({ text: text })
				.then((res) => {
					if (callback) callback(res.data.result);
					return res.data.result;
				});
		} catch (e) {
			ElMessage.error('获取失败');
		}
	};
	// 处理条件字段清空
	const handleConditionalClear = (condition: boolean, fieldValue: any, clearValue: any = undefined) => {
		if (condition) {
			return clearValue;
		}
		return fieldValue;
	};
	// 获取时间范围选择器快捷选项配置
	// 原来 7 个快捷项逐条复制同一段代码（只有天数 / 月数 / 年数不同），这里抽成三个工厂：
	// sinceDay(n) = 从 n 天前的 00:00 到现在；sinceMonth(n) = 从 n 个月前到现在；tillYearEnd(n) = 从 n 年前的 1 月 1 日到今年 12 月 31 日
	const getTimeRangePickerShortcuts = () => {
		const serverNow = () => new Date(themeStore.themeConfig.serverTime as any);
		// 今天的 23:59:59.999
		const endOfToday = (now: Date) => new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59, 999);

		const sinceDay = (days: number) => () => {
			const now = serverNow();
			return [new Date(now.getFullYear(), now.getMonth(), now.getDate() - days), endOfToday(now)];
		};
		const sinceMonth = (months: number) => () => {
			const now = serverNow();
			return [new Date(now.getFullYear(), now.getMonth() - months, now.getDate()), endOfToday(now)];
		};
		const tillYearEnd = (years: number) => () => {
			const now = serverNow();
			return [new Date(now.getFullYear() - years, 0, 1), new Date(now.getFullYear(), 11, 31, 23, 59, 59, 999)];
		};

		return [
			{ text: '近一周', value: sinceDay(7) },
			{ text: '近一月', value: sinceMonth(1) },
			{ text: '近三月', value: sinceMonth(3) },
			{ text: '近半年', value: sinceMonth(6) },
			{ text: '本年', value: tillYearEnd(0) },
			{ text: '近两年', value: tillYearEnd(1) },
			{ text: '近三年', value: tillYearEnd(2) },
		];
	};
	return {
		percentFormat,
		dateFormatYMD,
		dateFormatYMDHMS,
		dateFormatHMS,
		scaleFormat,
		scale2Format,
		groupSeparator,
		copyText,
		removeHtmlSub,
		removeHtml,
		getEnumDesc,
		appendQueryParams,
		getNameAbbr,
		handleConditionalClear,
		getTimeRangePickerShortcuts,
	};
}
