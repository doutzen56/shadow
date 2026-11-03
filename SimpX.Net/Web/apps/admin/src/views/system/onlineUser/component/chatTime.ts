/**
 * 解析聊天时间：后端可能返回 "yyyy-MM-dd HH:mm:ss" 或 ISO 串，后者在 iOS/Safari 上解析不了，
 * 先按原样解析，失败再退回斜杠分隔写法（SignalR 推送与 HTTP 接口两种来源都要覆盖）
 * @param value 时间字符串
 */
export const parseChatDate = (value?: string | Date | null): Date | null => {
	if (!value) return null;
	if (value instanceof Date) return isNaN(value.getTime()) ? null : value;

	// SQLite 存的是 DateTime.Now，小数秒多达 6 位（.905361）；Chrome 只认 3 位毫秒，多则解析为 Invalid Date
	const raw = String(value).replace(/(\.\d{3})\d+/, '$1');
	let date = new Date(raw);
	if (isNaN(date.getTime())) date = new Date(raw.replace(/-/g, '/').replace('T', ' '));
	return isNaN(date.getTime()) ? null : date;
};

const pad = (n: number) => String(n).padStart(2, '0');

/**
 * 聊天时间格式化：今天只显示时分，昨天显示"昨天 时分"，同年显示月日，跨年显示年月日
 * @param value 时间字符串（后端返回 "2026-10-19 20:11:00" 形式，iOS/Safari 只认斜杠分隔）
 */
export const formatChatTime = (value?: string | Date | null): string => {
	const date = parseChatDate(value);
	if (!date) return '';

	const now = new Date();
	const hm = `${pad(date.getHours())}:${pad(date.getMinutes())}`;

	const dayDiff = diffDays(date, now);
	if (dayDiff === 0) return hm;
	if (dayDiff === 1) return `昨天 ${hm}`;

	const md = `${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
	return date.getFullYear() === now.getFullYear() ? `${md} ${hm}` : `${date.getFullYear()}-${md} ${hm}`;
};

/** 日期键：同一天返回同一个串，供消息流按天分组 */
export const chatDayKey = (value?: string | Date | null): string => {
	const date = parseChatDate(value);
	return date ? `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` : '';
};

/** 消息流里的日期分割标签：今天 / 昨天 / M月D日 / YYYY年M月D日 */
export const chatDayLabel = (value?: string | Date | null): string => {
	const date = parseChatDate(value);
	if (!date) return '';

	const dayDiff = diffDays(date, new Date());
	if (dayDiff === 0) return '今天';
	if (dayDiff === 1) return '昨天';

	const md = `${date.getMonth() + 1}月${date.getDate()}日`;
	return date.getFullYear() === new Date().getFullYear() ? md : `${date.getFullYear()}年${md}`;
};

/** 相差天数（按自然日算，不满一天但跨了零点也算 1 天） */
const diffDays = (from: Date, to: Date): number =>
	Math.floor((new Date(to.getFullYear(), to.getMonth(), to.getDate()).getTime() - new Date(from.getFullYear(), from.getMonth(), from.getDate()).getTime()) / 86400000);
