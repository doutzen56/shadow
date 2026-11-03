/**
 * 把 SimpX 生成客户端里的 /api/sysXxx 路径改成 SimpX 实际路由。
 * 本波只保证登录、用户信息、菜单、常量能通；其它业务接口以后再逐个接。
 */

/** 租户后台：Pro sys* → SimpX /api/system/* */
const ADMIN_REWRITES: Array<[RegExp, string]> = [
	[/\/api\/sysAuth(\/|$)/i, '/api/system/auth$1'],
	[/\/api\/sysMenu(\/|$)/i, '/api/system/menu$1'],
	[/\/api\/sysConst(\/|$)/i, '/api/system/const$1'],
	[/\/api\/sysUser(\/|$)/i, '/api/system/user$1'],
	[/\/api\/sysRole(\/|$)/i, '/api/system/role$1'],
	[/\/api\/sysOrg(\/|$)/i, '/api/system/org$1'],
	[/\/api\/sysPos(\/|$)/i, '/api/system/pos$1'],
	[/\/api\/sysFile(\/|$)/i, '/api/system/file$1'],
	[/\/api\/sysConfig(\/|$)/i, '/api/system/config$1'],
	[/\/api\/sysTenant(\/|$)/i, '/api/system/tenant$1'],
	[/\/api\/sysNotice(\/|$)/i, '/api/system/notice$1'],
	[/\/api\/sysUpgrade(\/|$)/i, '/api/system/upgrade$1'],
	[/\/api\/sysColumnCustom(\/|$)/i, '/api/system/columnCustom$1'],
];

/** 总后台：登录菜单常量走 /api/platform/* */
const PLATFORM_REWRITES: Array<[RegExp, string]> = [
	[/\/api\/sysAuth(\/|$)/i, '/api/platform/auth$1'],
	[/\/api\/sysMenu(\/|$)/i, '/api/platform/menu$1'],
	[/\/api\/sysConst(\/|$)/i, '/api/platform/const$1'],
];

/**
 * 按当前应用改写接口路径。
 * @param url 原始请求地址
 * @param hostMode admin=租户后台，platform=总后台
 * @returns 改写后的地址
 */
export function rewriteProApiToSimpX(url: string | undefined, hostMode: 'admin' | 'platform' = 'admin'): string | undefined {
	if (!url) return url;
	const table = hostMode === 'platform' ? PLATFORM_REWRITES : ADMIN_REWRITES;
	let next = url;
	for (const [pattern, replacement] of table) {
		next = next.replace(pattern, replacement);
	}
	return next;
}
