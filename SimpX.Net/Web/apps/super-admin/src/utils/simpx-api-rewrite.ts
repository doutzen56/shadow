/**
 * 把 SimpX 生成客户端里的 /api/sysXxx 路径改成 SimpX 总后台实际路由。
 * 本波只保证登录、用户信息、菜单、常量、通知占位能通。
 */

/** 总后台：登录菜单常量 + 顶栏通知/升级占位 */
const PLATFORM_REWRITES: Array<[RegExp, string]> = [
	[/\/api\/sysAuth(\/|$)/i, '/api/platform/auth$1'],
	[/\/api\/sysMenu(\/|$)/i, '/api/platform/menu$1'],
	[/\/api\/sysConst(\/|$)/i, '/api/platform/const$1'],
	[/\/api\/sysNotice(\/|$)/i, '/api/platform/notice$1'],
	[/\/api\/sysUpgrade(\/|$)/i, '/api/platform/upgrade$1'],
];

/**
 * 按总后台改写接口路径。
 * @param url 原始请求地址
 * @returns 改写后的地址
 */
export function rewriteProApiToSimpX(url: string | undefined): string | undefined {
	if (!url) return url;
	let next = url;
	for (const [pattern, replacement] of PLATFORM_REWRITES) {
		next = next.replace(pattern, replacement);
	}
	return next;
}
