/**
 * 会员列表相关 /admin 接口（对齐 Web.Admin user/member/list）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 列表行（字段名跟旧站表格接近） */
export interface MemberListRow {
	ID: number;
	UserID?: number;
	UserName?: string;
	NickName?: string;
	AccountName?: string;
	AgentID?: number;
	AgentName?: string;
	InviterName?: string;
	Type?: number;
	TypeName?: string;
	Group?: string;
	GroupID?: number;
	Level?: number;
	LevelName?: string;
	TotalMoney?: number;
	Money?: number;
	Currency?: number | string;
	LoginAt?: string;
	LoginIP?: string;
	ActiveAt?: string;
	IsOnline?: boolean;
	RegFlag?: number;
	RegCurrency?: string | number;
	Count?: number;
	Depth?: number;
	TotalRechargeMoney?: number;
	TotalWithdrawMoney?: number;
}

/** 下拉选项 */
export interface MemberOption {
	ID?: number | string;
	id?: number | string;
	Level?: number | string;
	Name?: string;
	name?: string;
	key?: number | string;
	value?: string;
}

/** 查询条件：字段名对齐旧站 Form */
export type MemberListQuery = Record<string, string | number | undefined | null>;

/**
 * 归一化下拉列表（兼容 list / 数组 / RecordCount）。
 */
function asOptionList(raw: unknown): MemberOption[] {
	if (Array.isArray(raw)) return raw as MemberOption[];
	if (raw && typeof raw === 'object') {
		const o = raw as AdminApiPageInfo<MemberOption> & { Data?: MemberOption[] };
		if (Array.isArray(o.list)) return o.list;
		if (Array.isArray(o.Data)) return o.Data;
	}
	return [];
}

/**
 * 会员列表（旧路径 info/list）。
 * @param query 查询条件含 PageIndex/PageSize
 */
export function fetchMemberList(query: MemberListQuery) {
	return adminPost<AdminApiPageInfo<MemberListRow>>('/admin/user/member/info/list', query as Record<string, unknown>);
}

/**
 * 上级代理面包屑。
 * @param userName 当前代理用户名
 */
export function fetchMemberParentList(userName: string) {
	return adminPost<Array<{ UserName?: string; ID?: number }>>('/admin/user/member/info/getparentlist', {
		UserName: userName,
	});
}

/**
 * 会员分组下拉。
 */
export function fetchMemberGroupOptions() {
	return adminPost<MemberOption[] | AdminApiPageInfo<MemberOption>>('/admin/user/member/group/list', {}).then(asOptionList);
}

/**
 * 佣金方案下拉（list0）。
 */
export function fetchCommissionOptions() {
	return adminPost<MemberOption[] | AdminApiPageInfo<MemberOption>>('/admin/user/agent/commission/list0', {}).then(asOptionList);
}

/**
 * 返点方案下拉（list0）。
 */
export function fetchReturnPlanOptions() {
	return adminPost<MemberOption[] | AdminApiPageInfo<MemberOption>>('/admin/user/agent/returnplan/list0', {}).then(asOptionList);
}

/**
 * VIP 等级下拉（getlevels：Level / Name）。
 */
export function fetchVipLevelOptions() {
	return adminPost<MemberOption[] | AdminApiPageInfo<MemberOption>>('/admin/site/vip/getlevels', {}).then(asOptionList);
}

/**
 * 显示币种（showreport：key / value）。
 */
export function fetchCurrencyOptions() {
	return adminPost<MemberOption[] | AdminApiPageInfo<MemberOption>>('/admin/site/currency/showreport', {}).then(asOptionList);
}

/**
 * 站点类型（getsitetype；Type==1 时显示注册币种）。
 */
export function fetchSiteType() {
	return adminPost<{ Type?: string | number } | Record<string, unknown>>('/admin/site/info/getsitetype', {});
}

/**
 * 添加总代。
 */
export function addTopAgent(data: { UserName: string; Password: string; Currency?: string | number; Code?: string }) {
	return adminPost('/admin/user/agent/info/add', data as Record<string, unknown>);
}

/**
 * 批量导入备注（Items JSON：UserName + Content）。
 */
export function batchAddMemberMarks(items: Array<{ UserName: string; Content: string }>) {
	return adminPost('/admin/user/member/info/batchaddmark', {
		Items: JSON.stringify(items),
	});
}

/**
 * 多币种余额（余额展开）。
 */
export function fetchMemberDetailMoney(userId: number) {
	return adminPost('/admin/user/member/info/detailmoney', { ID: userId });
}

/**
 * 会员导出（最多 5000 行；前端落 CSV）。
 * @param query 与列表同名筛选；IsLimit=0 可导出全体
 */
export function exportMemberList(query: MemberListQuery) {
	return adminPost<AdminApiPageInfo<MemberListRow>>('/admin/user/member/info/export', query as Record<string, unknown>);
}
