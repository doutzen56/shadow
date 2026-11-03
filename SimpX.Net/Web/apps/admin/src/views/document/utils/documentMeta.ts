/**
 * 文档模块公共展示元数据
 *
 * 把「枚举值 → 文案 / 图标 / 标签色」这类映射集中在一处，避免列表、卡片、统计页各写一份。
 * 常量来自后端 SimpX.Plugin.Document：
 *   DocumentTypeEnum   1 Word  2 Excel  3 PDF  4 PPT  5 TXT  6 视频  7 音频  8 图片  9 ZIP  10 RAR  99 其他
 *   DocumentStatusEnum 1 草稿  2 已发布  3 已归档  4 已删除
 */

/** 文档类型：文案 + 图标（图标统一用 Element Plus 内置图标，不引第三方图标库） */
export const DOCUMENT_TYPE_META: Record<number, { label: string; icon: string }> = {
	1: { label: 'Word', icon: 'ele-Document' },
	2: { label: 'Excel', icon: 'ele-Tickets' },
	3: { label: 'PDF', icon: 'ele-DocumentCopy' },
	4: { label: 'PPT', icon: 'ele-DataBoard' },
	5: { label: 'TXT', icon: 'ele-Document' },
	6: { label: '视频', icon: 'ele-VideoCamera' },
	7: { label: '音频', icon: 'ele-Headset' },
	8: { label: '图片', icon: 'ele-Picture' },
	9: { label: 'ZIP', icon: 'ele-Box' },
	10: { label: 'RAR', icon: 'ele-Box' },
	99: { label: '其他', icon: 'ele-Files' },
};

/** 文档状态：文案 + el-tag 语义色 */
export const DOCUMENT_STATUS_META: Record<number, { label: string; tagType: 'info' | 'success' | 'warning' | 'danger' }> = {
	1: { label: '草稿', tagType: 'info' },
	2: { label: '已发布', tagType: 'success' },
	3: { label: '已归档', tagType: 'warning' },
	4: { label: '已删除', tagType: 'danger' },
};

/** 取类型文案，未知值回落到「其他」 */
export const documentTypeText = (type?: number | null): string => {
	if (type === undefined || type === null) return '其他';
	return DOCUMENT_TYPE_META[type]?.label ?? '其他';
};

/** 取类型图标，未知值回落到默认文件图标 */
export const documentTypeIcon = (type?: number | null): string => {
	if (type === undefined || type === null) return 'ele-Files';
	return DOCUMENT_TYPE_META[type]?.icon ?? 'ele-Files';
};

/** 取状态文案，未知值回落到「草稿」 */
export const documentStatusText = (status?: number | null): string => {
	if (status === undefined || status === null) return '草稿';
	return DOCUMENT_STATUS_META[status]?.label ?? '草稿';
};

/** 取状态标签语义色，未知值回落到 info */
export const documentStatusTag = (status?: number | null): 'info' | 'success' | 'warning' | 'danger' => {
	if (status === undefined || status === null) return 'info';
	return DOCUMENT_STATUS_META[status]?.tagType ?? 'info';
};

/** 按文件名后缀推断类型图标：后端没给 documentType 时（如回收站部分数据）用它兜底 */
export const documentIconByName = (name?: string | null): string => {
	const ext = (name || '').split('.').pop()?.toLowerCase() ?? '';
	if (['doc', 'docx'].includes(ext)) return DOCUMENT_TYPE_META[1].icon;
	if (['xls', 'xlsx', 'csv'].includes(ext)) return DOCUMENT_TYPE_META[2].icon;
	if (['pdf'].includes(ext)) return DOCUMENT_TYPE_META[3].icon;
	if (['ppt', 'pptx'].includes(ext)) return DOCUMENT_TYPE_META[4].icon;
	if (['txt', 'md', 'log'].includes(ext)) return DOCUMENT_TYPE_META[5].icon;
	if (['mp4', 'avi', 'mov', 'mkv', 'webm'].includes(ext)) return DOCUMENT_TYPE_META[6].icon;
	if (['mp3', 'wav', 'flac', 'aac'].includes(ext)) return DOCUMENT_TYPE_META[7].icon;
	if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(ext)) return DOCUMENT_TYPE_META[8].icon;
	if (['zip', '7z', 'tar', 'gz'].includes(ext)) return DOCUMENT_TYPE_META[9].icon;
	if (['rar'].includes(ext)) return DOCUMENT_TYPE_META[10].icon;
	return DOCUMENT_TYPE_META[99].icon;
};

/** 日期格式化：YYYY-MM-DD */
export const formatDate = (date?: string | Date | null): string => {
	if (!date) return '';
	const d = new Date(date);
	if (Number.isNaN(d.getTime())) return '';
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};

/** 日期时间格式化：YYYY-MM-DD HH:mm */
export const formatDateTime = (date?: string | Date | null): string => {
	if (!date) return '';
	const d = new Date(date);
	if (Number.isNaN(d.getTime())) return '';
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${formatDate(d)} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

/** 拆成「日期 + 时间」两行：表格里竖排两行比一长串更好扫读（配合 .time-cell / .cell-sub） */
export const splitDateTime = (date?: string | Date | null): { date: string; time: string } => {
	if (!date) return { date: '', time: '' };
	const d = new Date(date);
	if (Number.isNaN(d.getTime())) return { date: '', time: '' };
	const pad = (n: number) => String(n).padStart(2, '0');
	return {
		date: formatDate(d),
		time: `${pad(d.getHours())}:${pad(d.getMinutes())}`,
	};
};
