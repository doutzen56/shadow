/**
 * 文档分享外链（Hash 路由）
 */
export function buildDocumentShareUrl(shareCode: string): string {
	const code = shareCode?.trim();
	if (!code) return '';

	const pageBase = window.location.href.split('#')[0].replace(/\/$/, '');
	return `${pageBase}/#/document/share/${code}`;
}

/**
 * 将接口返回的 shareUrl / shareCode 规范为可访问的完整链接
 */
export function resolveShareLink(share: { shareCode?: string; shareUrl?: string }): string {
	const code = share.shareCode?.trim();
	if (code) return buildDocumentShareUrl(code);

	const raw = share.shareUrl?.trim() ?? '';
	if (!raw) return '';
	if (/^https?:\/\//i.test(raw)) return raw;

	const pageBase = window.location.href.split('#')[0].replace(/\/$/, '');
	if (raw.startsWith('/#/')) return `${pageBase}${raw}`;
	if (raw.startsWith('#/')) return `${pageBase}/${raw}`;
	if (raw.startsWith('/document/share/')) return `${pageBase}/#${raw}`;
	if (raw.startsWith('/')) return `${pageBase}/#${raw}`;

	return buildDocumentShareUrl(raw);
}
