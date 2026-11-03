/**
 * 代理管理相关 /admin 接口（对齐 Web.Admin user/agent/*）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';
import { unwrapAdminList } from '/@/api/user/member-group';

/** 通用查询 */
export type AgentQuery = Record<string, string | number | undefined | null>;

/**
 * 代理域名列表。
 * @param query Form
 */
export function fetchAgentDomainList(query: AgentQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/user/agent/domain/list', query as Record<string, unknown>);
}

/**
 * 删除代理域名。
 * @param id 编号
 */
export function deleteAgentDomain(id: number) {
	return adminPost('/admin/user/agent/domain/delete', { ID: id });
}

/**
 * 代理域名详情。
 * @param id 编号
 */
export function fetchAgentDomainInfo(id: number) {
	return adminPost('/admin/user/agent/domain/info', { ID: id });
}

/**
 * 保存代理域名。
 * @param data 表单
 */
export function saveAgentDomain(data: Record<string, unknown>) {
	return adminPost('/admin/user/agent/domain/save', data);
}

/**
 * 邀请码列表。
 * @param query Form
 */
export async function fetchAgentInviteList(query: AgentQuery) {
	const info = await adminPost<AdminApiPageInfo<Record<string, unknown>> | Record<string, unknown>[]>(
		'/admin/user/agent/invite/list',
		query as Record<string, unknown>
	);
	if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in info) {
		return {
			list: (info as AdminApiPageInfo<Record<string, unknown>>).list ?? [],
			total: (info as AdminApiPageInfo<Record<string, unknown>>).RecordCount ?? 0,
		};
	}
	const list = unwrapAdminList<Record<string, unknown>>(info);
	return { list, total: list.length };
}

/**
 * 删除邀请码。
 * @param id 邀请码
 */
export function deleteAgentInvite(id: string | number) {
	return adminPost('/admin/user/agent/invite/delete', { ID: id });
}

/**
 * 邀请码详情。
 * @param id 邀请码
 */
export function fetchAgentInviteInfo(id: string | number) {
	return adminPost('/admin/user/agent/invite/info', { ID: id });
}

/**
 * 保存邀请码。
 * @param data 表单
 */
export function saveAgentInvite(data: Record<string, unknown>) {
	return adminPost('/admin/user/agent/invite/save', data);
}

/**
 * 代理申请列表。
 * @param query Form
 */
export function fetchAgentApplyList(query: AgentQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/user/agent/info/applylist', query as Record<string, unknown>);
}

/**
 * 审核代理申请。
 * @param id 申请编号
 * @param status 旧站 event 值（通过/拒绝等）
 */
export function execAgentApply(id: number, status: string) {
	return adminPost('/admin/user/agent/info/execapply', { ID: id, Status: status });
}

/**
 * 返佣方案列表。
 */
export async function fetchAgentCommissionList(query: AgentQuery = {}) {
	const info = await adminPost<AdminApiPageInfo<Record<string, unknown>> | Record<string, unknown>[]>(
		'/admin/user/agent/commission/list',
		query as Record<string, unknown>
	);
	if (info && typeof info === 'object' && !Array.isArray(info) && 'list' in info) {
		return {
			list: (info as AdminApiPageInfo<Record<string, unknown>>).list ?? [],
			total: (info as AdminApiPageInfo<Record<string, unknown>>).RecordCount ?? 0,
		};
	}
	const list = unwrapAdminList<Record<string, unknown>>(info);
	return { list, total: list.length };
}

/**
 * 刷新返佣方案代理数。
 * @param id 方案编号
 */
export function updateAgentCommissionCount(id: number) {
	return adminPost('/admin/user/agent/commission/updateagentcount', { ID: id });
}

/**
 * 返点方案列表。
 */
export async function fetchAgentReturnPlanList() {
	const info = await adminPost<AdminApiPageInfo<Record<string, unknown>> | Record<string, unknown>[]>(
		'/admin/user/agent/returnplan/list',
		{}
	);
	return unwrapAdminList<Record<string, unknown>>(info);
}

/**
 * 返点方案详情。
 */
export function fetchAgentReturnPlanInfo(id: number) {
	return adminPost('/admin/user/agent/returnplan/info', { ID: id });
}

/**
 * 保存返点方案。
 */
export function saveAgentReturnPlan(data: Record<string, unknown>) {
	return adminPost('/admin/user/agent/returnplan/save', data);
}

/**
 * 层级设置列表。
 * @param query Form
 */
export function fetchAgentGradeList(query: AgentQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>> | Record<string, unknown>[]>(
		'/admin/user/agent/grade/list',
		query as Record<string, unknown>
	);
}

/**
 * 保存层级（设代理/锁定）。
 */
export function saveAgentGrade(data: Record<string, unknown>) {
	return adminPost('/admin/user/agent/grade/save', data);
}
