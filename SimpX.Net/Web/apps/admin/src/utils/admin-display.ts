/**
 * 租户后台展示层统一格式：币种/费用/次数等。
 * 币种走 admin-enum（接口缓存），禁止硬编码 CurrencyNames。
 */
import { ensureCurrencyEnum, formatCurrencyEnum, normalizeCurrencySuffix } from '/@/utils/admin-enum';

export { ensureCurrencyEnum, normalizeCurrencySuffix };

/**
 * 币种展示：已是代码则原样；数字查枚举缓存；未知保留原值。
 * 页面 onMounted / 查询前请先 `await ensureCurrencyEnum()`。
 */
export function formatCurrency(value: unknown): string {
	return formatCurrencyEnum(value);
}

/** 金额两位小数 */
export function formatMoney2(value: unknown): string {
	const n = Number(value);
	return Number.isFinite(n) ? n.toFixed(2) : value == null || value === '' ? '-' : String(value);
}

/** 到账次数：第 N 次（旧站 Count，旁注「该用户第多少次充值」） */
export function formatRechargeCount(count: unknown): string {
	const n = Number(count);
	if (!Number.isFinite(n) || n <= 0) return '';
	return `第${n}次`;
}

/**
 * 充值 Remark：库内常为 `备注A|备注B|实充 币种`，单一钱包空备注会变成 `||5975.00 TWD`。
 * 展示只留人工备注；纯编码实充时返回空（页面显示 `-`）。
 */
export function formatRechargeRemark(value: unknown): string {
	if (value == null || value === '') return '';
	const raw = String(value).trim();
	if (!raw) return '';
	const parts = raw.split('|');
	if (parts.length >= 3) {
		return [parts[0], parts[1]]
			.map((s) => s.trim())
			.filter(Boolean)
			.join(' / ');
	}
	return raw.replace(/\|+/g, ' ').trim();
}

/**
 * 实充展示：`47.50 2` → `47.50 TWD`（末段币种走统一枚举）。
 */
export function formatRealPaidDisplay(raw: unknown, fallbackPaid?: unknown, fallbackCurrency?: unknown): string {
	const s = String(raw ?? '').trim();
	if (!s) {
		const money = formatMoney2(fallbackPaid);
		if (fallbackCurrency == null || fallbackCurrency === '') return money;
		return `${money} ${formatCurrency(fallbackCurrency)}`;
	}
	return normalizeCurrencySuffix(s, fallbackCurrency);
}

/** 资金审核状态（MoneyCheckStatus）中文 */
export function formatMoneyCheckStatus(value: unknown): string {
	const key = String(value ?? '').trim();
	const map: Record<string, string> = {
		'0': '待审',
		None: '待审',
		'1': '失败',
		Failed: '失败',
		Faild: '失败',
		'2': '通过',
		Success: '通过',
		'3': '处理中',
		Processing: '处理中',
	};
	return map[key] || (key ? key : '-');
}

/** 提现状态（WithdrawStatus）中文；保留旧拼写 Faild */
export function formatWithdrawStatus(value: unknown): string {
	const key = String(value ?? '').trim();
	const map: Record<string, string> = {
		'0': '待处理',
		None: '待处理',
		'1': '处理中',
		Wait: '处理中',
		'2': '已完成',
		Finish: '已完成',
		'3': '失败',
		Faild: '失败',
		Failed: '失败',
		'4': '出款中',
		InPayment: '出款中',
	};
	return map[key] || (key ? key : '-');
}

/**
 * 会员锁定位中文（旧 LockStatus 位标志）。
 * 已知：Login=1 Recharge=2 Game=32 Point=64 Activity=128 Upgrade=256。
 */
export function formatUserLockFlags(value: unknown): string {
	const n = Number(value);
	if (!Number.isFinite(n) || n <= 0) return '无';
	const bits: Array<[number, string]> = [
		[1, '禁登录'],
		[2, '禁充值'],
		[32, '禁游戏'],
		[64, '禁积分'],
		[128, '禁活动'],
		[256, '禁升级'],
	];
	const labels = bits.filter(([bit]) => (n & bit) !== 0).map(([, label]) => label);
	const known = bits.reduce((acc, [bit]) => acc | bit, 0);
	const rest = n & ~known;
	if (rest) labels.push(`其他(${rest})`);
	return labels.length ? labels.join('、') : String(n);
}
