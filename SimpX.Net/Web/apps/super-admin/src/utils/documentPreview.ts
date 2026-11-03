import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi } from '/@/api-services/document/api';
import { service } from '/@/utils/request';
import { ensureFileBlob, isExternalStorageFileUrl, resolveDocumentBrowserFileUrl, shouldLoadFileViaDownloadApi } from '/@/utils/documentFileUrl';

export interface DocumentPreviewInput {
	id?: number;
	fileUrl?: string;
	preUrl?: string;
	name?: string;
	fileName?: string;
	suffix?: string;
	fileExt?: string;
	skipPreviewLookup?: boolean;
	previewApiPath?: string;
}

export interface DocumentPreviewResult {
	/** 直链预览（MinIO / 系统文件管理等） */
	url?: string;
	/** 经下载接口获取的二进制，需带扩展名供 file-viewer3 识别格式 */
	file?: File;
	revoke?: () => void;
}

const downloadDocumentBlob = async (documentId: number): Promise<Blob> => {
	const res = await getAPI(DocumentApi).apiDocumentDownloadPost({ id: documentId });
	const blob = res.data instanceof Blob ? res.data : new Blob([res.data]);
	return ensureFileBlob(blob);
};

/** 从文档元数据或原始 URL 解析带扩展名的文件名 */
export function resolvePreviewFileName(doc: DocumentPreviewInput, rawFileUrl?: string): string {
	const baseName = String(doc.name || doc.fileName || '').trim();
	const suffix = String(doc.suffix || doc.fileExt || '')
		.trim()
		.toLowerCase();

	if (baseName) {
		if (/\.\w+$/i.test(baseName)) return baseName;
		if (suffix) return `${baseName}${suffix.startsWith('.') ? suffix : `.${suffix}`}`;
		return baseName;
	}

	if (rawFileUrl) {
		try {
			const segment = rawFileUrl.split('?')[0].split('/').pop() || '';
			if (segment) return decodeURIComponent(segment);
		} catch {
			// ignore
		}
	}

	return 'preview.bin';
}

const blobToPreviewFile = (blob: Blob, fileName: string): File => {
	const type = blob.type && blob.type !== 'application/octet-stream' ? blob.type : undefined;
	return new File([blob], fileName, type ? { type } : undefined);
};

/**
 * 解析文档在线预览资源（详情、预览统计、MinIO 直链、本地下载等）。
 */
export async function resolveDocumentPreviewUrl(doc: DocumentPreviewInput): Promise<DocumentPreviewResult> {
	let working = { ...doc };
	const skipLookup = working.skipPreviewLookup === true;
	const documentId = working.id;

	if (!working.fileUrl && !skipLookup && documentId) {
		const detailRes = await getAPI(DocumentApi).apiDocumentDetailGet(documentId);
		if (detailRes.data?.result) {
			working = { ...working, ...detailRes.data.result };
		}
	}

	if (!skipLookup && documentId) {
		try {
			const previewApiPath = working.previewApiPath || '/api/document/preview/url';
			const previewRes = await service.get(previewApiPath, { params: { id: documentId } });
			if (previewRes.data?.result?.fileUrl) {
				working.fileUrl = previewRes.data.result.fileUrl;
			}
		} catch (error) {
			console.warn('获取预览URL失败，使用原始URL:', error);
		}
	}

	const rawFileUrl = working.fileUrl;
	if (!rawFileUrl) {
		return {};
	}

	const browserUrl = resolveDocumentBrowserFileUrl(rawFileUrl, working.preUrl);
	const isExternal = isExternalStorageFileUrl(rawFileUrl, working.preUrl);
	const previewFileName = resolvePreviewFileName(working, rawFileUrl);

	// MinIO / OSS：与 /platform/file 一致，使用带 .docx 等扩展名的直链 URL
	if (isExternal) {
		return { url: browserUrl };
	}

	if (shouldLoadFileViaDownloadApi(documentId, skipLookup)) {
		try {
			const blob = await downloadDocumentBlob(documentId!);
			return { file: blobToPreviewFile(blob, previewFileName) };
		} catch (error) {
			console.warn('下载接口预览失败，尝试静态文件路径:', error);
			return { url: browserUrl };
		}
	}

	return { url: browserUrl };
}

/** 分享页：仅解析浏览器可访问地址（不调用需登录的预览/详情接口） */
export function resolveShareDocumentPreviewUrl(doc: DocumentPreviewInput): DocumentPreviewResult {
	const url = resolveDocumentBrowserFileUrl(doc.fileUrl, doc.preUrl);
	return { url };
}
