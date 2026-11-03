/**
 * 证件信息 /admin 接口（对齐 Web.Admin user/member/kyc → idcard）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 证件列表行 */
export interface MemberKycRow {
	ID: number;
	UserID?: number;
	UserName?: string;
	CardName?: string;
	CardNumber?: string;
	CardCity?: string;
	CardCreateAt?: string;
	CardBirthday?: string;
	CardType?: number;
	Status?: number;
	Remark?: string;
	Checker?: string | number;
	CheckDateTime?: string;
	CreateAt?: string;
}

/** 证件详情 */
export interface MemberKycDetail extends MemberKycRow {}

/** 查询条件 */
export type MemberKycQuery = Record<string, string | number | undefined | null>;

/**
 * 证件列表。
 * @param query Form 条件
 */
export function fetchMemberKycList(query: MemberKycQuery) {
	return adminPost<AdminApiPageInfo<MemberKycRow>>('/admin/user/member/idcard/list', query as Record<string, unknown>);
}

/**
 * 删除待审证件。
 * @param id 证件编号
 */
export function deleteMemberKyc(id: number) {
	return adminPost('/admin/user/member/idcard/delete', { ID: id });
}

/**
 * 证件详情。
 * @param id 编号
 */
export function fetchMemberKycInfo(id: number) {
	return adminPost<MemberKycDetail>('/admin/user/member/idcard/info', { ID: id });
}

/**
 * 按用户名查会员。
 * @param userName 用户名
 */
export function verifyMemberKycUser(userName: string) {
	return adminPost<{ ID?: number; UserName?: string }>('/admin/user/member/idcard/verifyuser', { UserName: userName });
}

/**
 * 新建证件。
 * @param data 表单
 */
export function createMemberKyc(data: Record<string, unknown>) {
	return adminPost('/admin/user/member/idcard/createidcard', data);
}

/**
 * 证件正反面图上传。对齐旧 /admin/system/upload。
 * @param file 本地文件
 * @returns 存储键 Image（写入 CardFront/CardBack）
 */
export async function uploadMemberKycImage(file: File): Promise<string> {
	const fd = new FormData();
	fd.append('file', file);
	const info = await adminPost<{ Image?: string; Url?: string }>('/admin/system/upload', fd);
	const key = String(info?.Image || info?.Url || '').trim();
	if (!key) throw new Error('上传失败');
	return key;
}

/**
 * 审核证件。
 * @param data ID / Status / Remark / CardNumber
 */
export function updateMemberKycInfo(data: {
	ID: number;
	Status: string | number;
	Remark?: string;
	CardNumber?: string;
}) {
	return adminPost('/admin/user/member/idcard/updateinfo', data as Record<string, unknown>);
}
