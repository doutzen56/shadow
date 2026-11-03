import { AxiosResponseHeaders, RawAxiosResponseHeaders } from 'axios';
import { ElMessage } from 'element-plus';
import { dataURLtoBlob, urlToBase64 } from './base64Conver';

/**
 * Download online pictures
 * @param url
 * @param filename
 * @param mime
 * @param bom
 */
export function downloadByOnlineUrl(url: string, filename: string, mime?: string, bom?: BlobPart) {
	urlToBase64(url).then((base64) => {
		downloadByBase64(base64, filename, mime, bom);
	});
}

/**
 * Download pictures based on base64
 * @param buf
 * @param filename
 * @param mime
 * @param bom
 */
export function downloadByBase64(buf: string, filename: string, mime?: string, bom?: BlobPart) {
	const base64Buf = dataURLtoBlob(buf);
	downloadByData(base64Buf, filename, mime, bom);
}

/**
 * Download according to the background interface file stream
 * @param {*} data
 * @param {*} filename
 * @param {*} mime
 * @param {*} bom
 */
export function downloadByData(data: BlobPart, filename: string, mime?: string, bom?: BlobPart) {
	const blobData = typeof bom !== 'undefined' ? [bom, data] : [data];
	const blob = new Blob(blobData, { type: mime || 'application/octet-stream' });

	const blobURL = window.URL.createObjectURL(blob);
	const tempLink = document.createElement('a');
	tempLink.style.display = 'none';
	tempLink.href = blobURL;
	tempLink.setAttribute('download', filename);
	if (typeof tempLink.download === 'undefined') {
		tempLink.setAttribute('target', '_blank');
	}
	document.body.appendChild(tempLink);
	tempLink.click();
	document.body.removeChild(tempLink);
	window.URL.revokeObjectURL(blobURL);
}

/**
 * Download file according to file address
 * @param {*} sUrl
 */
export async function downloadByUrl({ url, fileName, target = '_blank' }: { url: string; fileName?: string; target?: string }): Promise<boolean> {
	// 如果没有提供 fileName，则从 URL 中解析
	const finalFileName = fileName || url.substring(url.lastIndexOf('/') + 1).split('?')[0] || 'download';

	// 同源或 data: blob: 协议可直接使用 download 属性
	const isSameOrigin = (url: string): boolean => {
		try {
			const link = new URL(url, window.location.href);
			return link.origin === window.location.origin;
		} catch {
			return false;
		}
	};
	const isBlobOrData = url.startsWith('blob:') || url.startsWith('data:');

	if (isSameOrigin(url) || isBlobOrData) {
		const link = document.createElement('a');
		link.href = url;
		link.download = finalFileName;
		link.target = target;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		return true;
	}

	// 跨域：尝试通过 fetch 下载（需要 CORS）
	try {
		const response = await fetch(url);
		if (!response.ok) throw new Error(`HTTP ${response.status}`);
		const blob = await response.blob();
		const blobUrl = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = blobUrl;
		link.download = finalFileName;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		URL.revokeObjectURL(blobUrl); // 清理内存
		return true;
	} catch (error) {
		console.error('Download failed:', error);
		// 降级：直接打开（无法指定文件名）
		openWindow(url, { target });
		return false;
	}
}

export function openWindow(url: string, opt?: { target?: string | string; noopener?: boolean; noreferrer?: boolean }) {
	const { target = '__blank', noopener = true, noreferrer = true } = opt || {};
	const feature: string[] = [];

	noopener && feature.push('noopener=yes');
	noreferrer && feature.push('noreferrer=yes');

	window.open(url, target, feature.join(','));
}

export function getFileName(headers: RawAxiosResponseHeaders | AxiosResponseHeaders) {
	var contentDisposition = headers['content-disposition'];
	if (contentDisposition == undefined) return;
	var fileName = contentDisposition.split(';')[1].split('filename=')[1];
	var fileNameUnicode = contentDisposition.split('filename*=')[1];
	if (fileName?.includes('%')) fileName = decodeURIComponent(fileName);
	if (fileNameUnicode) {
		//当存在 filename* 时，取filename* 并进行解码（为了解决中文乱码问题）
		fileName = decodeURIComponent(fileNameUnicode.split("''")[1]);
	}
	return fileName;
}

/**
 * 文件流下载
 * @param res
 * @param fileName 文件名
 * @returns 是否已开始下载
 */
export async function downloadStreamFile(res: any, fileName: string | undefined = undefined): Promise<boolean> {
	const contentType = res.headers?.['content-type'] ?? '';
	// 后端业务异常（如无权限、文件不存在）会返回 JSON，此时提示错误而不是保存成文件
	if (contentType.includes('application/json')) {
		let message = '下载失败';
		try {
			const text = res.data instanceof Blob ? await res.data.text() : typeof res.data === 'string' ? res.data : JSON.stringify(res.data);
			message = JSON.parse(text)?.message || message;
		} catch {
			/* 保留默认提示 */
		}
		ElMessage.error(message);
		return false;
	}

	fileName = fileName || getFileName(res.headers);
	const blob = res.data instanceof Blob ? res.data : new Blob([res.data], { type: contentType });
	downloadByUrl({ url: window.URL.createObjectURL(blob), fileName });
	return true;
}
