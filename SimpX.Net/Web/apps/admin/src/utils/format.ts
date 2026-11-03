/**
 * 通用格式化工具
 *
 * 收敛散落在各页面里逐字重复的格式化函数（日期时间 / 文件大小），
 * 页面统一从这里引入，避免同一份逻辑在十几个文件里各写一遍。
 */
import { formatDate } from '/@/utils/formatTime';

/**
 * 日期时间 → 字符串（空值统一显示占位符，避免出现 `NaN-NaN-NaN`）
 * @param value 日期对象或可被 `new Date()` 解析的值
 * @param format 目标格式，见 `formatDate` 支持的占位符
 * @param placeholder 空值时的显示文案
 */
export function formatDateTime(value?: string | Date | null, format: string = 'YYYY-mm-dd HH:MM:SS', placeholder: string = '-'): string {
	if (!value) return placeholder;
	const s = String(value);
	// CLR DateTime.MinValue / 旧库哨兵日期不当作有效时间
	if (s.startsWith('0001') || s.startsWith('1900-01-01') || s.startsWith('1900/01/01')) return placeholder;
	const d = value instanceof Date ? value : new Date(value);
	if (Number.isNaN(d.getTime())) return placeholder;
	const y = d.getFullYear();
	if (y <= 1900) return placeholder;
	return formatDate(d, format);
}

/**
 * 把日期格式化成当天的起点 / 终点时刻
 * @param date 日期对象
 * @param isEnd true 取 `23:59:59`，false 取 `00:00:00`
 */
export function formatDateWithDayEdge(date: Date, isEnd = false): string {
	const y = date.getFullYear();
	const m = String(date.getMonth() + 1).padStart(2, '0');
	const d = String(date.getDate()).padStart(2, '0');
	return `${y}-${m}-${d} ${isEnd ? '23:59:59' : '00:00:00'}`;
}

/**
 * 获取当月的起止时间（列表查询常用的 startTime / endTime）
 */
export function getCurrentMonthRange(): { startTime: string; endTime: string } {
	const now = new Date();
	const start = new Date(now.getFullYear(), now.getMonth(), 1);
	const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
	return { startTime: formatDateWithDayEdge(start, false), endTime: formatDateWithDayEdge(end, true) };
}

/**
 * 字节数 → 可读字符串（1024 进制）
 * @param bytes 字节数
 * @param options.trimZeros 是否去掉小数末尾的 0（true：`1.5 KB`；false：`1.50 KB`）
 */
export function formatBytes(bytes: number, options: { trimZeros?: boolean } = {}): string {
	const { trimZeros = true } = options;
	const value = Number(bytes || 0);
	if (!value) return '0 B';
	const units = ['B', 'KB', 'MB', 'GB', 'TB'];
	// 钳位：超过 TB 时下标会越界，单位会变成 undefined
	const i = Math.min(Math.floor(Math.log(value) / Math.log(1024)), units.length - 1);
	const num = (value / Math.pow(1024, i)).toFixed(2);
	return `${trimZeros ? parseFloat(num) : num} ${units[i]}`;
}

/**
 * 文件大小（后端统一存 KB）→ 可读字符串
 */
export function formatFileSize(kb: number): string {
	if (kb >= 1024 * 1024) return (kb / (1024 * 1024)).toFixed(2) + ' GB';
	if (kb >= 1024) return (kb / 1024).toFixed(2) + ' MB';
	return kb + ' KB';
}

/**
 * 文件大小 → Element Plus 标签类型（>=1GB 红、>=1MB 橙、其余绿）
 */
export function getFileTagType(kb: number): string {
	if (kb >= 1024 * 1024) return 'danger';
	if (kb >= 1024) return 'warning';
	return 'success';
}
