/**
 * 银行信息 /admin 接口（对齐 Web.Admin user/member/bank）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 银行列表行 */
export interface MemberBankRow {
	ID: number;
	UserID?: number;
	UserName?: string;
	Name?: string;
	Account?: string;
	AccountDetail?: string;
	BankName?: string;
	Type?: number;
	IsVir?: string;
	Status?: number;
	Remark?: string;
	CreateAt?: string;
	CheckDateTime?: string;
	Currency?: number;
	Checker?: string | number;
	IdNumber?: string;
	BankInfo?: string;
	BankInfoImg?: string;
}

/** 银行详情 */
export interface MemberBankDetail extends MemberBankRow {}

/** 查询条件：字段名对齐旧站 Form */
export type MemberBankQuery = Record<string, string | number | undefined | null>;

/**
 * 银行信息列表。
 * @param query Form 条件
 */
export function fetchMemberBankList(query: MemberBankQuery) {
	return adminPost<AdminApiPageInfo<MemberBankRow>>('/admin/user/member/bank/list', query as Record<string, unknown>);
}

/**
 * 删除待审银行（旧站走 info/bankdelete）。
 * @param id 银行编号
 */
export function deleteMemberBank(id: number) {
	return adminPost('/admin/user/member/info/bankdelete', { ID: id });
}

/**
 * 银行详情。
 * @param id 编号
 */
export function fetchMemberBankInfo(id: number) {
	return adminPost<MemberBankDetail>('/admin/user/member/bank/info', { ID: id });
}

/**
 * 按用户名查会员（添加银行前）。
 * @param userName 用户名
 */
export function verifyMemberBankUser(userName: string) {
	return adminPost<{ ID?: number; UserName?: string }>('/admin/user/member/bank/verifyuser', { UserName: userName });
}

/**
 * 创建银行卡（首版先通银行卡 IsVir=0）。
 * @param data 表单
 */
export function createMemberBank(data: Record<string, unknown>) {
	return adminPost('/admin/user/member/bank/createbank', data);
}

/**
 * 审核银行信息。
 * @param data ID / Status(Success|Faild|整型) / Remark
 */
export function updateMemberBankInfo(data: { ID: number; Status: string | number; Remark?: string }) {
	return adminPost('/admin/user/member/bank/updateinfo', data as Record<string, unknown>);
}

/**
 * 撤回审核（回到待审）。
 * @param id 银行编号
 */
export function resetMemberBank(id: number) {
	return adminPost('/admin/user/member/bank/reset', { ID: id });
}

/** 银行类型下拉项（bankcountrylist：Type / Bank） */
export interface MemberBankTypeOption {
	Type?: number | string;
	Bank?: string;
	type?: number | string;
	bank?: string;
}

/**
 * 银行类型全量枚举（添加银行下拉）。
 */
export function fetchMemberBankCountryList() {
	return adminPost<MemberBankTypeOption[] | AdminApiPageInfo<MemberBankTypeOption>>('/admin/user/member/bank/bankcountrylist', {});
}

/** bankcountryinfo 返回：info.list[].bank 为编号→名称 */
export interface MemberBankCountryInfo {
	list?: Array<{ bank?: Record<string, string>; country?: string }>;
}

/**
 * 按 IsVir 取添加弹层选项（虚拟/超商等）。对齐旧 bankcountryinfo。
 * @param isVir 0/1/2/4
 */
export function fetchMemberBankCountryInfo(isVir: string | number) {
	return adminPost<MemberBankCountryInfo>('/admin/user/member/bank/bankcountryinfo', { IsVir: String(isVir), isVir: String(isVir) });
}

/**
 * 超商城市。对齐旧 bankcitylist。
 * @param type 超商类型
 */
export function fetchMemberBankCityList(type: number) {
	return adminPost<MemberBankTypeOption[] | AdminApiPageInfo<MemberBankTypeOption>>('/admin/user/member/bank/bankcitylist', { Type: type });
}

/**
 * 超商区县。对齐旧 bankdistrictlist。
 */
export function fetchMemberBankDistrictList(type: number, city: string) {
	return adminPost<MemberBankTypeOption[] | AdminApiPageInfo<MemberBankTypeOption>>('/admin/user/member/bank/bankdistrictlist', {
		Type: type,
		City: city,
	});
}

/**
 * 超商门店。对齐旧 bankadresslist（Type=门店编号，Bank=地址名）。
 */
export function fetchMemberBankAdressList(type: number, city: string, district: string) {
	return adminPost<MemberBankTypeOption[] | AdminApiPageInfo<MemberBankTypeOption>>('/admin/user/member/bank/bankadresslist', {
		Type: type,
		City: city,
		District: district,
	});
}

/**
 * 银行凭证上传。对齐旧 layupload → /admin/system/upload。
 * @param file 本地文件
 * @returns 存储键 Image
 */
export async function uploadMemberBankInfo(file: File): Promise<string> {
	const fd = new FormData();
	fd.append('file', file);
	const info = await adminPost<{ Image?: string; Url?: string }>('/admin/system/upload', fd);
	const key = String(info?.Image || info?.Url || '').trim();
	if (!key) throw new Error('上传失败');
	return key;
}

/** 归一 Type/Bank 列表 */
export function unwrapBankTypeOptions(info: unknown): Array<{ Type: number; Bank: string }> {
	const raw = Array.isArray(info) ? info : Array.isArray((info as any)?.list) ? (info as any).list : [];
	return (raw as MemberBankTypeOption[])
		.map((x) => ({ Type: Number(x.Type ?? x.type ?? 0), Bank: String(x.Bank ?? x.bank ?? '') }))
		.filter((x) => x.Bank || x.Type);
}
