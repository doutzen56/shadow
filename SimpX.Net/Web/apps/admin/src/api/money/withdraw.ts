/**
 * 提现相关 /admin 接口。
 */
import { adminPost, type AdminApiPageInfo } from '/@/utils/admin-api';
import type { MoneyQuery } from '/@/api/money/recharge';

/**
 * 提现订单列表。
 * @param query Form
 */
export function fetchWithdrawOrderList(query: MoneyQuery) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/money/withdraw/orderlist', query as Record<string, unknown>);
}

/**
 * 提现订单详情。
 * @param id 订单号
 */
export function fetchWithdrawOrderInfo(id: string | number) {
	return adminPost('/admin/money/withdraw/orderinfo', { ID: id });
}

/**
 * 提现通道列表。
 */
export function fetchWithdrawChannelList(query: MoneyQuery = {}) {
	return adminPost<AdminApiPageInfo<Record<string, unknown>>>('/admin/money/withdraw/channelist', query as Record<string, unknown>);
}

/** 审核提现（Status=None）。Action=Agree|Reject */
export function checkWithdrawOrder(payload: Record<string, unknown>) {
	return adminPost('/admin/money/withdraw/checkorder', payload);
}

/** 出款/拒绝（Status=Wait）。Action=Agree|Reject */
export function execWithdrawOrder(payload: Record<string, unknown>) {
	return adminPost('/admin/money/withdraw/execorder', payload);
}
