/**
 * 会员分组 /admin 接口（对齐 Web.Admin user/member/group）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 分组列表行 */
export interface MemberGroupRow {
	ID: number;
	Name?: string;
	CreateAt?: string;
	Member?: number;
	Sort?: number;
	IsUpdate?: number | string | boolean;
	IsDefault?: number | string | boolean;
	IsNight?: number | string | boolean;
	Wallet?: string;
	IsEdit?: string;
	IsDelete?: string;
}

/** 分组详情 */
export interface MemberGroupDetail {
	ID?: number;
	Name?: string;
	Sort?: number;
	IsUpdate?: boolean;
	IsDefault?: boolean;
	IsNight?: boolean;
	Member?: number;
	Payment?: string;
	Withdraw?: string;
	Condition?: string;
	SettingInfo?: string;
}

/** 保存入参 */
export interface MemberGroupSaveInput {
	ID: number;
	Name: string;
	Sort: number;
	IsUpdate: number | boolean;
	IsDefault: number | boolean;
	IsNight: number | boolean;
	Payment?: string;
	WithdrawString?: string;
	ConditionString?: string;
	SettingInfo?: string;
}

/**
 * 从 info 解出数组（兼容 OfList / 纯数组）。
 * @param info 接口 info
 */
export function unwrapAdminList<T>(info: unknown): T[] {
	if (Array.isArray(info)) return info as T[];
	if (info && typeof info === 'object' && Array.isArray((info as AdminApiPageInfo<T>).list)) {
		return (info as AdminApiPageInfo<T>).list as T[];
	}
	return [];
}

/**
 * 分组列表。
 */
export function fetchMemberGroupList() {
	return adminPost<AdminApiPageInfo<MemberGroupRow> | MemberGroupRow[]>('/admin/user/member/group/list', {});
}

/**
 * 分组详情。
 * @param id 分组编号
 */
export function fetchMemberGroupInfo(id: number) {
	return adminPost<MemberGroupDetail>('/admin/user/member/group/info', { ID: id });
}

/**
 * 保存分组（0=新增）。
 * @param input 表单
 */
export function saveMemberGroup(input: MemberGroupSaveInput) {
	return adminPost('/admin/user/member/group/save', input as unknown as Record<string, unknown>);
}

/**
 * 删除分组。
 * @param id 分组编号
 */
export function deleteMemberGroup(id: number) {
	return adminPost('/admin/user/member/group/delete', { ID: id });
}

/**
 * 刷新组内人数。
 * @param id 分组编号
 */
export function refreshMemberGroupCount(id: number) {
	return adminPost<{ Member?: number }>('/admin/user/member/group/updatemember', { ID: id });
}

/**
 * 设为默认分组。
 * @param id 分组编号
 */
export function setMemberGroupDefault(id: number) {
	return adminPost('/admin/user/member/group/updategroupisdefault', { ID: id });
}
