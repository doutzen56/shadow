/**
 * 租户后台统一枚举解析：整型/字符串 → 展示名。
 * 禁止页面散落 CurrencyNames / switch。
 * 币种唯一源：后端 Data.Currency → `/admin/exchange/rate/getcurrencyinfo`（全量），
 * 失败再回退 `/admin/site/currency/report`。
 */
import { adminPost } from '/@/utils/admin-api';

export type AdminEnumMap = Record<string, string>;

const caches = new Map<string, AdminEnumMap>();
const inflight = new Map<string, Promise<AdminEnumMap>>();

/** 币种枚举缓存键 */
export const CURRENCY_ENUM_KEY = 'currency';

/**
 * 通用：用已加载的 map 解析枚举值。
 * 已是名称则原样；未加载/未知保留原字符串。
 */
export function enumLabel(mapKey: string, value: unknown, empty = '-'): string {
	if (value == null || value === '') return empty;
	const s = String(value).trim();
	const map = caches.get(mapKey);
	if (!map) return s;
	if (map[s] != null) return map[s]!;
	return s;
}

/** 是否已缓存 */
export function hasEnumMap(mapKey: string): boolean {
	return caches.has(mapKey);
}

/** 写入/覆盖缓存 */
export function setEnumMap(mapKey: string, map: AdminEnumMap): void {
	caches.set(mapKey, map);
}

/** 懒加载枚举表；同 key 并发只打一次。 */
export async function loadEnumMap(mapKey: string, loader: () => Promise<AdminEnumMap>): Promise<AdminEnumMap> {
	const hit = caches.get(mapKey);
	if (hit) return hit;
	let p = inflight.get(mapKey);
	if (!p) {
		p = loader()
			.then((m) => {
				caches.set(mapKey, m);
				inflight.delete(mapKey);
				return m;
			})
			.catch((e) => {
				inflight.delete(mapKey);
				throw e;
			});
		inflight.set(mapKey, p);
	}
	return p;
}

type CurrencyInfoRow = { Type?: number | string; Name?: string; type?: number | string; name?: string };
type CurrencyReportRow = { key?: number | string; value?: string; Key?: number | string; Value?: string };

function toCurrencyMapFromInfo(rows: CurrencyInfoRow[]): AdminEnumMap {
	const map: AdminEnumMap = {};
	for (const row of rows) {
		const key = row.Type ?? row.type;
		const value = String(row.Name ?? row.name ?? '').trim();
		if (key == null || !value) continue;
		map[String(key)] = value;
		map[value] = value;
	}
	return map;
}

function toCurrencyMapFromReport(rows: CurrencyReportRow[]): AdminEnumMap {
	const map: AdminEnumMap = {};
	for (const row of rows) {
		const key = row.key ?? row.Key;
		const value = String(row.value ?? row.Value ?? '').trim();
		if (key == null || !value) continue;
		map[String(key)] = value;
		map[value] = value;
	}
	return map;
}

/**
 * 拉取全量币种枚举（Data.Currency）。
 * 优先 getcurrencyinfo（全枚举），失败再 report。
 */
export async function ensureCurrencyEnum(): Promise<AdminEnumMap> {
	return loadEnumMap(CURRENCY_ENUM_KEY, async () => {
		try {
			const info = await adminPost<CurrencyInfoRow[] | { list?: CurrencyInfoRow[] }>('/admin/exchange/rate/getcurrencyinfo', {});
			const list = Array.isArray(info) ? info : Array.isArray((info as any)?.list) ? (info as any).list : [];
			const map = toCurrencyMapFromInfo(list as CurrencyInfoRow[]);
			if (Object.keys(map).length > 0) return map;
		} catch {
			/* fall through */
		}
		const rows = await adminPost<CurrencyReportRow[] | { list?: CurrencyReportRow[] }>('/admin/site/currency/report', {});
		const list = Array.isArray(rows) ? rows : Array.isArray((rows as any)?.list) ? (rows as any).list : [];
		return toCurrencyMapFromReport(list as CurrencyReportRow[]);
	});
}

/** 币种展示。 */
export function formatCurrencyEnum(value: unknown): string {
	return enumLabel(CURRENCY_ENUM_KEY, value);
}

/**
 * 规范化「金额 币种」后缀：`47.50 2` → `47.50 TWD`。
 * 末段是数字则走币种枚举；已是代码则原样。
 */
export function normalizeCurrencySuffix(text: unknown, fallbackCurrency?: unknown): string {
	const raw = String(text ?? '').trim();
	if (!raw) {
		return fallbackCurrency == null || fallbackCurrency === '' ? '' : formatCurrencyEnum(fallbackCurrency);
	}
	const bits = raw.split(/\s+/).filter(Boolean);
	if (bits.length === 1) {
		return fallbackCurrency == null || fallbackCurrency === ''
			? bits[0]!
			: `${bits[0]} ${formatCurrencyEnum(fallbackCurrency)}`;
	}
	const last = bits[bits.length - 1]!;
	if (/^\d+$/.test(last)) bits[bits.length - 1] = formatCurrencyEnum(Number(last));
	else bits[bits.length - 1] = formatCurrencyEnum(last);
	return bits.join(' ');
}
