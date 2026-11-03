/**
 * 文档文件 URL 工具。
 * - 本地存储：相对路径，开发环境经 Vite/nginx 代理访问 API 静态目录
 * - MinIO/OSS：完整外链，保持原样；预览二进制优先走下载接口
 */

const getDocumentApiBaseUrl = (preUrl?: string | null): string => {
	let base = (preUrl ?? '').trim();
	if (!base) {
		base = String((window as any).__env__?.VITE_API_URL ?? '').trim();
	}
	return base.replace(/\/+$/, '');
};

/** 是否为 http(s) 绝对地址 */
export function isAbsoluteFileUrl(fileUrl?: string | null): boolean {
	return !!fileUrl && /^https?:\/\//i.test(fileUrl.trim());
}

/** MinIO / OSS / CDN 等第三方存储（与 API 不同源） */
export function isExternalStorageFileUrl(fileUrl?: string | null, preUrl?: string | null): boolean {
	if (!isAbsoluteFileUrl(fileUrl)) return false;

	const apiBase = getDocumentApiBaseUrl(preUrl);
	if (!apiBase) return true;

	try {
		return new URL(fileUrl!.trim()).origin !== new URL(apiBase).origin;
	} catch {
		return true;
	}
}

/** 从绝对/相对地址提取路径部分（含 query） */
const toPathname = (fileUrl: string): string => {
	const trimmed = fileUrl.trim();
	if (!/^https?:\/\//i.test(trimmed)) {
		return trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
	}
	try {
		const url = new URL(trimmed);
		return url.pathname + url.search;
	} catch {
		return trimmed;
	}
};

/**
 * 解析为完整访问地址（展示、外链等场景）。
 */
export function resolveDocumentFileUrl(fileUrl?: string | null, preUrl?: string | null): string {
	if (!fileUrl) return '';

	const trimmed = fileUrl.trim();
	if (isExternalStorageFileUrl(trimmed, preUrl)) return trimmed;
	if (isAbsoluteFileUrl(trimmed)) return trimmed;

	const base = getDocumentApiBaseUrl(preUrl) || window.location.origin;
	const path = toPathname(trimmed);
	return `${base}${path}`;
}

/**
 * 供浏览器直接请求的资源地址（img、video、iframe 等）。
 * 外链存储保持原 URL；本地/API 静态文件在跨域开发时转为相对路径走代理。
 */
export function resolveDocumentBrowserFileUrl(fileUrl?: string | null, preUrl?: string | null): string {
	if (!fileUrl) return '';

	const trimmed = fileUrl.trim();

	// MinIO / OSS：不改写
	if (isExternalStorageFileUrl(trimmed, preUrl)) {
		return trimmed;
	}

	const apiBase = getDocumentApiBaseUrl(preUrl);

	// API 同源绝对地址 -> 相对路径
	if (isAbsoluteFileUrl(trimmed) && apiBase) {
		try {
			if (new URL(trimmed).origin === new URL(apiBase).origin) {
				return toPathname(trimmed);
			}
		} catch {
			// ignore
		}
		return trimmed;
	}

	// 本地相对路径：开发时 API 与页面不同源，走代理
	const path = toPathname(trimmed);
	if (apiBase) {
		try {
			if (new URL(apiBase).origin !== window.location.origin) {
				return path;
			}
		} catch {
			// ignore
		}
	}

	return resolveDocumentFileUrl(trimmed, preUrl);
}

/** 有文档 ID 时，二进制预览应优先走下载接口（兼容本地与 MinIO） */
export function shouldLoadFileViaDownloadApi(documentId?: number, skipLookup?: boolean): boolean {
	return !!documentId && !skipLookup;
}

/** 校验下载结果是否为真实文件流（避免 JSON 错误被当作 docx） */
export const ensureFileBlob = async (blob: Blob): Promise<Blob> => {
	if (!blob || blob.size === 0) {
		throw new Error('文件内容为空');
	}
	const type = blob.type?.toLowerCase() ?? '';
	if (type.includes('json') || type.includes('text/plain')) {
		const snippet = (await blob.slice(0, 256).text()).trim();
		if (snippet.startsWith('{') && (snippet.includes('"code"') || snippet.includes('"message"'))) {
			try {
				const json = JSON.parse(snippet);
				throw new Error(json.message || '文件下载失败，请检查存储配置或权限');
			} catch (e) {
				if (e instanceof Error && e.message !== '文件下载失败，请检查存储配置或权限') throw e;
			}
			throw new Error('文件下载失败，请检查存储配置或权限');
		}
	}
	return blob;
};

/** Blob 转 ArrayBuffer */
export const blobToArrayBuffer = (blob: Blob): Promise<ArrayBuffer> =>
	new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => resolve(reader.result as ArrayBuffer);
		reader.onerror = reject;
		reader.readAsArrayBuffer(blob);
	});

/**
 * @deprecated 使用 resolveDocumentBrowserFileUrl
 */
export function toServiceRequestPath(fileUrl: string): string {
	return resolveDocumentBrowserFileUrl(fileUrl);
}
