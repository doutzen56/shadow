/**
 * 聊天消息内容编解码
 *
 * 后端 SysLogMsg.Message 只有一个字符串字段，图片/文件消息用内嵌标记表达：
 *   [[img:{json}]] / [[file:{json}]]
 * 收发双方都按同一规则解析后渲染成对应气泡；不认识该标记的地方（会话摘要、旧数据）按 "[图片]" "[文件]" 文本兜底。
 */

/** 图片消息前缀 */
const IMG_PREFIX = '[[img:';

/** 文件消息前缀 */
const FILE_PREFIX = '[[file:';

/** 消息标记后缀 */
const MARK_SUFFIX = ']]';

/** 图片消息负载 */
export interface ChatImagePayload {
	/** 图片访问地址 */
	url: string;
	/** 原始文件名（可选，用于另存/悬浮提示） */
	name?: string;
}

/** 文件消息负载 */
export interface ChatFilePayload {
	/** 文件访问地址 */
	url: string;
	/** 原始文件名 */
	name?: string;
	/** 文件大小（字节） */
	size?: number;
	/** 文件记录Id（后续走下载接口时可用） */
	id?: number;
}

/** 解析后的消息内容 */
export interface ChatContent {
	/** 文本、图片或文件 */
	kind: 'text' | 'image' | 'file';
	/** 文本内容（图片/文件消息为兜底文案） */
	text: string;
	/** 图片信息（kind 为 image 时存在） */
	image?: ChatImagePayload;
	/** 文件信息（kind 为 file 时存在） */
	file?: ChatFilePayload;
}

/** 生成图片消息内容 */
export const encodeImageMessage = (payload: ChatImagePayload): string => `${IMG_PREFIX}${JSON.stringify(payload)}${MARK_SUFFIX}`;

/** 生成文件消息内容 */
export const encodeFileMessage = (payload: ChatFilePayload): string => `${FILE_PREFIX}${JSON.stringify(payload)}${MARK_SUFFIX}`;

/** 消息标记内的 JSON 负载：url 必有，其余字段按需携带 */
type MarkPayload = {
	url?: string;
	name?: string;
	size?: number;
	id?: number;
};

/** 解析内嵌标记：只认自家前缀，解析失败按普通文本处理，不至于让整条消息消失 */
const parseMark = (text: string, prefix: string): MarkPayload | null => {
	if (!text.startsWith(prefix) || !text.endsWith(MARK_SUFFIX)) return null;
	try {
		return JSON.parse(text.slice(prefix.length, text.length - MARK_SUFFIX.length));
	} catch {
		return null;
	}
};

/** 解析消息内容：非图片/文件消息原样返回文本 */
export const parseChatContent = (raw?: string | null): ChatContent => {
	const text = raw ?? '';

	if (text.startsWith(IMG_PREFIX)) {
		const payload = parseMark(text, IMG_PREFIX);
		if (payload?.url) return { kind: 'image', text: '[图片]', image: { url: String(payload.url), name: payload.name ? String(payload.name) : '' } };
	}

	if (text.startsWith(FILE_PREFIX)) {
		const payload = parseMark(text, FILE_PREFIX);
		if (payload?.url) {
			return {
				kind: 'file',
				text: '[文件]',
				file: {
					url: String(payload.url),
					name: payload.name ? String(payload.name) : '文件',
					size: Number(payload.size ?? 0) || 0,
					id: Number(payload.id ?? 0) || undefined,
				},
			};
		}
	}

	return { kind: 'text', text };
};

/** 会话摘要：图片/文件消息在左侧列表里只显示 "[图片]" "[文件]" */
export const toMessageSummary = (raw?: string | null): string => parseChatContent(raw).text;

/** 文件大小格式化：小于 1MB 用 KB，否则用 MB */
export const formatFileSize = (bytes?: number): string => {
	const size = Number(bytes ?? 0);
	if (!size) return '';
	if (size < 1024) return `${size} B`;
	if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
	return `${(size / 1024 / 1024).toFixed(1)} MB`;
};

/** 取文件扩展名（不含点、转大写，用于文件卡片上的角标） */
export const fileExt = (name?: string): string => {
	const fileName = name ?? '';
	const dot = fileName.lastIndexOf('.');
	if (dot < 0 || dot === fileName.length - 1) return 'FILE';
	return fileName.slice(dot + 1).toUpperCase().slice(0, 4);
};

/** 常见图片后缀 */
const IMAGE_EXT = ['JPG', 'JPEG', 'PNG', 'BMP', 'GIF', 'WEBP', 'SVG', 'ICO'];

/**
 * 是否图片文件：文件卡片里图片走内置大图预览，其余交给框架的 OnlineFilePreview。
 * 判断依据是文件名而非消息类型 —— 以图片形式发出的消息也可能是 png 之外的格式。
 */
export const isImageName = (name?: string): boolean => IMAGE_EXT.includes(fileExt(name));

/** 文件类型分组：文件卡片按分组配色（文档蓝、表格绿、演示橙、PDF 红、压缩紫、媒体青、图片粉、其余灰） */
export type FileColorGroup = 'doc' | 'sheet' | 'slide' | 'pdf' | 'archive' | 'media' | 'image' | 'file';

/** 按扩展名归入配色分组：未知类型兜底为中性灰，不至于误染成文档色 */
export const fileColorGroup = (name?: string): FileColorGroup => {
	switch (fileExt(name)) {
		case 'DOC':
		case 'DOCX':
		case 'TXT':
		case 'MD':
			return 'doc';
		case 'XLS':
		case 'XLSX':
		case 'CSV':
			return 'sheet';
		case 'PPT':
		case 'PPTX':
			return 'slide';
		case 'PDF':
			return 'pdf';
		case 'ZIP':
		case 'RAR':
		case '7Z':
			return 'archive';
		case 'MP4':
		case 'MKV':
		case 'MP3':
		case 'WAV':
			return 'media';
		default:
			return isImageName(name) ? 'image' : 'file';
	}
};
