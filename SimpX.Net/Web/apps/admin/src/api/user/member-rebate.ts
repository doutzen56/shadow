/**
 * 返水设定 /admin 接口（对齐 Web.Admin user/member/return → rebate）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';

/** 返水设定行 */
export interface MemberRebateRow {
	ID: number;
	Name?: string;
	IsAuto?: boolean;
	Enabled?: boolean;
	BetMoney?: number;
	MinMoney?: number;
	MaxMoney?: number;
	/** 后端 DTO 字段 */
	Groups?: string;
	Levels?: string;
	/** 旧站表格字段名 */
	Group?: string;
	Level?: string;
	Type?: string;
	CreatorName?: string;
	SettingString?: string;
}

/** 保存入参 */
export interface MemberRebateSaveInput {
	ID: number;
	Name: string;
	Groups?: string;
	Levels?: string;
	BetMoney?: number;
	MinMoney?: number;
	MaxMoney?: number;
	IsAuto?: number | boolean;
	Enabled?: number | boolean;
	Type?: string;
	SettingString?: string;
}

/**
 * 返水设定列表。
 * @param groupId 可选分组筛选（旧站 GroupID）
 */
export async function fetchMemberRebateList(groupId?: string) {
	const info = await adminPost<AdminApiPageInfo<MemberRebateRow> | MemberRebateRow[]>(
		'/admin/user/member/rebate/list',
		groupId ? { GroupID: groupId } : {}
	);
	return unwrapAdminList<MemberRebateRow>(info);
}

/**
 * 返水设定详情。
 * @param id 编号
 */
export function fetchMemberRebateInfo(id: number) {
	return adminPost<MemberRebateRow>('/admin/user/member/rebate/info', { ID: id });
}

/**
 * 保存返水设定。
 * @param input 表单
 */
export function saveMemberRebate(input: MemberRebateSaveInput) {
	return adminPost('/admin/user/member/rebate/save', input as unknown as Record<string, unknown>);
}

/**
 * 删除返水设定。
 * @param id 编号
 */
export function deleteMemberRebate(id: number) {
	return adminPost('/admin/user/member/rebate/delete', { ID: id });
}

/**
 * 切换启用（取反）。
 */
export function toggleMemberRebateEnabled(id: number) {
	return adminPost<boolean>('/admin/user/member/rebate/updateenabled', { ID: id });
}

/**
 * 改自动发放开关。
 */
export function updateMemberRebateAuto(id: number, isAuto: boolean) {
	return adminPost('/admin/user/member/rebate/updateauto', { ID: id, IsAuto: isAuto ? 1 : 0 });
}

/**
 * 返水类型字典。对齐旧 returntype（AA=综合 / A0..=游戏大类）。
 */
export async function fetchMemberRebateReturnTypes(): Promise<Array<{ value: string; label: string }>> {
	const info = await adminPost<Record<string, string> | Array<{ key?: string; value?: string }>>(
		'/admin/user/member/rebate/returntype',
		{}
	);
	if (Array.isArray(info)) {
		return info.map((x) => ({ value: String(x.key ?? ''), label: String(x.value ?? x.key ?? '') })).filter((x) => x.value);
	}
	if (info && typeof info === 'object') {
		return Object.entries(info).map(([value, label]) => ({ value, label: String(label) }));
	}
	return [];
}
