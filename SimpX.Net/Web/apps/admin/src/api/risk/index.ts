/**
 * 风控 /admin 接口。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';

/** 通用查询 */
export type RiskQuery = Record<string, string | number | undefined | null>;

/**
 * 解析列表 info。
 * @param info 接口 info
 */
export function parseRiskPage(info: unknown) {
	if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in info) {
		const p = info as AdminApiPageInfo<Record<string, unknown>>;
		return { list: p.list ?? [], total: p.RecordCount ?? 0 };
	}
	const list = unwrapAdminList<Record<string, unknown>>(info);
	return { list, total: list.length };
}

/** IP 黑名单 */
export function fetchRiskIpBlacklist(query: RiskQuery) {
	return adminPost('/admin/risk/ip/blacklist', query as Record<string, unknown>);
}

/** 同 IP */
export function fetchRiskIpSame(query: RiskQuery) {
	return adminPost('/admin/risk/ip/same', query as Record<string, unknown>);
}

/** 同设备 */
export function fetchRiskDeviceSame(query: RiskQuery) {
	return adminPost('/admin/risk/device/same', query as Record<string, unknown>);
}

/** 国别黑名单区域 */
export function fetchRiskBlackRegion(query: RiskQuery = {}) {
	return adminPost('/admin/risk/ip/getblackregion', query as Record<string, unknown>);
}

/** 会员盈利 */
export function fetchRiskControlProfit(query: RiskQuery) {
	return adminPost('/admin/risk/control/profit', query as Record<string, unknown>);
}

/** 游戏盈利 */
export function fetchRiskControlGame(query: RiskQuery) {
	return adminPost('/admin/risk/control/game', query as Record<string, unknown>);
}

/** 风险会员 */
export function fetchRiskUserList(query: RiskQuery) {
	return adminPost('/admin/risk/user/list', query as Record<string, unknown>);
}

/** 手机黑名单导入列表。对齐旧 importbalckmobilelist。 */
export function fetchRiskBlackMobileList(query: RiskQuery = {}) {
	return adminPost('/admin/risk/ip/importbalckmobilelist', query as Record<string, unknown>);
}

/** 证件黑名单列表。对齐旧 importbalckkyclist。 */
export function fetchRiskBlackKycList(query: RiskQuery = {}) {
	return adminPost('/admin/risk/ip/importbalckkyclist', query as Record<string, unknown>);
}
