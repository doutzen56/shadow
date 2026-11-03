/**
 * 帐变类型目录（对齐旧 MoneyType 整型 + 中文名；供帐变筛选下拉）。
 * 后端 `/admin/config/moneytype` 若已返回完整分组，优先用接口。
 */

/** 单类型 */
export type MoneyTypeOption = { value: string; label: string };

/** 分组 */
export type MoneyTypeGroup = { label: string; options: MoneyTypeOption[] };

/** 默认分组（覆盖常见帐变；与 OldApiMoneyDisplay.FormatMoneyTypeValue 对齐） */
export const DEFAULT_MONEY_TYPE_GROUPS: MoneyTypeGroup[] = [
	{
		label: '充提',
		options: [
			{ value: '1', label: '充值' },
			{ value: '2', label: '提现' },
			{ value: '3', label: '充值优惠' },
			{ value: '4', label: '提现手续费' },
			{ value: '6', label: '充值手续费' },
			{ value: '9', label: '充值补单' },
			{ value: '10', label: '提现退回' },
			{ value: '11', label: '提现扣款' },
			{ value: '31', label: '提现手续费退回' },
			{ value: '32', label: '充值手续费退回' },
			{ value: '44', label: '充值补单冲正' },
			{ value: '111', label: '代客充值' },
		],
	},
	{
		label: '游戏',
		options: [
			{ value: '5', label: '转出游戏' },
			{ value: '8', label: '转入游戏' },
			{ value: '66', label: '游戏投注' },
			{ value: '99', label: '游戏退还' },
		],
	},
	{
		label: '返水佣金',
		options: [
			{ value: '60', label: '返水' },
			{ value: '61', label: '手动返水' },
			{ value: '62', label: '返水费用' },
			{ value: '130', label: '佣金' },
			{ value: '131', label: '代理返点' },
			{ value: '132', label: '佣金费用' },
			{ value: '133', label: '代理返点费用' },
		],
	},
	{
		label: '转账兑换',
		options: [
			{ value: '120', label: '转入' },
			{ value: '121', label: '转出' },
			{ value: '140', label: '代充入' },
			{ value: '141', label: '代充出' },
			{ value: '150', label: '兑换入' },
			{ value: '151', label: '兑换出' },
		],
	},
	{
		label: '活动后台',
		options: [
			{ value: '200', label: '活动' },
			{ value: '230', label: '活动费用' },
			{ value: '250', label: '后台加款' },
			{ value: '251', label: '后台扣款' },
		],
	},
];

const NAME_BY_VALUE: Record<string, string> = Object.fromEntries(
	DEFAULT_MONEY_TYPE_GROUPS.flatMap((g) => g.options.map((o) => [o.value, o.label]))
);

/** 枚举名 → 中文（列表 TypeValue） */
const NAME_BY_ENUM: Record<string, string> = {
	Recharge: '充值',
	Withdraw: '提现',
	RechargeReward: '充值优惠',
	WithdrawFee: '提现手续费',
	GameTransferOut: '转出游戏',
	RechargeFee: '充值手续费',
	GameTransferIn: '转入游戏',
	RechargeDue: '充值补单',
	WithdrawReturn: '提现退回',
	WithdrawMoney: '提现扣款',
	WithdrawFeeBack: '提现手续费退回',
	RechargeFeeBack: '充值手续费退回',
	RechargeDueReverse: '充值补单冲正',
	Rebate: '返水',
	RebateAdd: '手动返水',
	RebateFee: '返水费用',
	GameBet: '游戏投注',
	GameReturn: '游戏退还',
	ProxyRecharge: '代客充值',
	TransferIn: '转入',
	TransferOut: '转出',
	Commission: '佣金',
	AgentReturn: '代理返点',
	CommissionFee: '佣金费用',
	AgentReturnFee: '代理返点费用',
	ProxyIn: '代充入',
	ProxyOut: '代充出',
	ExchangeIn: '兑换入',
	ExchangeOut: '兑换出',
	Event: '活动',
	EventFee: '活动费用',
	AdminIn: '后台加款',
	AdminOut: '后台扣款',
};

/**
 * 帐变类型展示文案。
 * @param raw Type / TypeValue
 */
export function formatMoneyTypeLabel(raw?: unknown): string {
	if (raw == null || raw === '') return '-';
	const key = String(raw).trim();
	if (NAME_BY_VALUE[key]) return NAME_BY_VALUE[key];
	if (NAME_BY_ENUM[key]) return NAME_BY_ENUM[key];
	return key;
}

/**
 * 把 `/admin/config/moneytype` 的 info 转成分组；失败返回默认。
 * 兼容：旧站 `{ 分类: { "1": "充值", ... } }`；或扁平 `{ "1": "1" }`。
 */
export function normalizeMoneyTypeGroups(info: unknown): MoneyTypeGroup[] {
	if (!info || typeof info !== 'object') return DEFAULT_MONEY_TYPE_GROUPS;
	const obj = info as Record<string, unknown>;
	const groups: MoneyTypeGroup[] = [];
	let nested = false;
	for (const [cate, children] of Object.entries(obj)) {
		if (children && typeof children === 'object' && !Array.isArray(children)) {
			nested = true;
			const options = Object.entries(children as Record<string, unknown>).map(([value, label]) => ({
				value: String(value),
				label: String(label ?? value),
			}));
			if (options.length) groups.push({ label: cate, options });
		}
	}
	if (nested && groups.length) return groups;

	// 扁平 key→label：合成一组
	const flat = Object.entries(obj)
		.filter(([, v]) => typeof v === 'string' || typeof v === 'number')
		.map(([value, label]) => ({
			value: String(value),
			label: NAME_BY_VALUE[String(value)] || NAME_BY_ENUM[String(value)] || String(label),
		}));
	if (flat.length) return [{ label: '帐变类型', options: flat }];
	return DEFAULT_MONEY_TYPE_GROUPS;
}
