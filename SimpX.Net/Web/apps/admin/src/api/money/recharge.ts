/**
 * 充值相关 /admin 接口（对齐 Web.Admin money/recharge/*、/admin/recharge/*）。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';

/** 通用查询 */
export type MoneyQuery = Record<string, string | number | boolean | undefined | null>;

/**
 * 充值订单列表。
 * @param query Form
 */
export function fetchRechargeOrderList(query: MoneyQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/recharge/orderlist', query as Record<string, unknown>);
}

/**
 * 充值订单详情。
 * @param id 订单号
 */
export function fetchRechargeOrderInfo(id: string | number) {
	return adminPost('/admin/recharge/orderinfo', { ID: id, OrderID: id });
}

/**
 * 入款审核列表。
 * @param query Form
 */
export function fetchRechargeCheckList(query: MoneyQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/money/recharge/checklist', query as Record<string, unknown>);
}

/**
 * 入款审核详情。
 * @param id 编号
 */
export function fetchRechargeCheckInfo(id: number) {
	return adminPost('/admin/money/recharge/checkinfo', { ID: id });
}

/**
 * 充值通道设定列表。
 * @param query Form
 */
export function fetchRechargeSettingList(query: MoneyQuery = {}) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/recharge/settinglist', query as Record<string, unknown>);
}

/**
 * 比对失败订单列表。
 * @param query Form
 */
export function fetchRechargeOrderFailList(query: MoneyQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/recharge/orderlistfail', query as Record<string, unknown>);
}

/** 确认入账。对齐旧 checkorder。 */
export function checkRechargeOrder(payload: Record<string, unknown>) {
	return adminPost('/admin/recharge/checkorder', payload);
}

/** 删除未到账订单。对齐旧 deleteorder。 */
export function deleteRechargeOrder(id: string | number) {
	return adminPost('/admin/recharge/deleteorder', { ID: id });
}

/** 代客充值建单。对齐旧 /admin/money/recharge/create。 */
export function createProxyRecharge(payload: Record<string, unknown>) {
	return adminPost('/admin/money/recharge/create', payload);
}

/** 通道下拉（settinglist）。 */
export function fetchRechargePayOptions(query: MoneyQuery = {}) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>> | Record<string, unknown>[]>('/admin/recharge/settinglist', query as Record<string, unknown>);
}
