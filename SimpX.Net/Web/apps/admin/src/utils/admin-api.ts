/**
 * 租户后台业务接口 `/admin/...` 专用请求层。
 * 与 axios-utils（框架 `{ code, result }`）分开：本文件只解 `{ success, msg, info }`，带新 JWT。
 */
import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { ElMessage } from 'element-plus';
import { Local } from '/@/utils/storage';
import {
	accessTokenKey,
	refreshAccessTokenKey,
	clearAccessTokens,
	decryptJWT,
	getJWTDate,
	markToastShown,
	showNetworkErrorThrottled,
} from '/@/utils/axios-utils';

/** `/admin` 接口返回体：success / msg / info */
export interface AdminApiResult<T = unknown> {
	success: number | boolean;
	msg?: string;
	info?: T;
}

/** 鉴权失败时 info 上的错误类型 */
export interface AdminApiErrorInfo {
	ErrorType?: string;
}

/** 分页 info（RecordCount / list） */
export interface AdminApiPageInfo<T = unknown> {
	RecordCount: number;
	PageIndex?: number;
	PageSize?: number;
	list?: T[];
	data?: unknown;
	other?: unknown;
}

/**
 * 转成 useVxeTable 默认 proxyConfig.response 能吃的形态：
 * `data.result.items` / `data.result.total`。
 * 业务页若直接返回 `{ page, result }`，表格会读不到数据（表现为永远空表）。
 */
export function toVxeProxyPage<T>(list: T[] | null | undefined, total?: number | null) {
	const items = Array.isArray(list) ? list : [];
	const n = total == null ? items.length : Number(total);
	return {
		data: {
			result: {
				items,
				total: Number.isFinite(n) ? n : items.length,
			},
		},
	};
}

/** 读分页条数：兼容 RecordCount / recordCount。 */
export function readAdminRecordCount(info: AdminApiPageInfo | Record<string, unknown> | null | undefined, fallback = 0): number {
	if (!info || typeof info !== 'object') return fallback;
	const o = info as Record<string, unknown>;
	const n = Number(o.RecordCount ?? o.recordCount ?? fallback);
	return Number.isFinite(n) ? n : fallback;
}

/** 读分页 list：兼容 list / List / Items。 */
export function readAdminList<T = unknown>(info: AdminApiPageInfo<T> | Record<string, unknown> | null | undefined): T[] {
	if (!info || typeof info !== 'object') return [];
	const o = info as Record<string, unknown>;
	if (Array.isArray(o.list)) return o.list as T[];
	if (Array.isArray(o.List)) return o.List as T[];
	if (Array.isArray(o.Items)) return o.Items as T[];
	if (Array.isArray(o.items)) return o.items as T[];
	return [];
}

/** `/admin` 请求配置 */
export interface AdminApiRequestConfig extends AxiosRequestConfig {
	/** 失败时不弹提示，由调用方处理 */
	silent?: boolean;
	/** 成功时返回完整 { success, msg, info }，默认只返回 info */
	raw?: boolean;
}

const adminAxios: AxiosInstance = axios.create({
	baseURL: import.meta.env.DEV ? '' : (window.__env__.VITE_API_URL as string),
	timeout: 1000 * 60,
});

/**
 * 判断 `/admin` 接口是否成功（success === 1 或 truthy）。
 * @param success 接口 success 字段
 * @returns 是否成功
 */
export function isAdminApiSuccess(success: number | boolean | undefined | null): boolean {
	if (success === true || success === 1) return true;
	if (typeof success === 'number' && success !== 0) return true;
	return false;
}

/**
 * 处理 `/admin` 返回的 msg 文案。
 *
 * 旧站约定：`#操作成功#` 这类是**语言包 key**，不是装饰号。
 * Web.Admin 用 `Utils.getDemo` / `Utils.getField` 查 `scripts/trlang.json`
 * （正则抠出 `#…#` 再查表；缺词条才退回去掉 # 的原文；`~…~` 是另一套页面文案标记）。
 *
 * 本波尚未接入语言包，**原样返回 msg，禁止瞎扒 #**。
 * 接入翻译后在本函数内走查表，不要在拦截器里直接 replace 掉 #。
 *
 * @param msg 接口原始 msg
 * @returns 当前可展示文案（暂为原文）
 */
export function resolveAdminApiMsg(msg?: string | null): string {
	if (!msg) return '';
	return String(msg);
}

/**
 * 把普通对象转成 application/x-www-form-urlencoded（业务页多为 Form 字段）。
 * @param data 表单字段
 * @returns URLSearchParams；空则 undefined
 */
export function toAdminForm(data?: Record<string, unknown> | null): URLSearchParams | undefined {
	if (!data) return undefined;
	const params = new URLSearchParams();
	for (const [key, value] of Object.entries(data)) {
		if (value === undefined || value === null) continue;
		if (Array.isArray(value)) {
			for (const item of value) {
				if (item === undefined || item === null) continue;
				params.append(key, String(item));
			}
			continue;
		}
		params.append(key, String(value));
	}
	return params;
}

/**
 * 从响应里取出 ErrorType（Login / Permission）。
 * @param info info 字段
 * @returns ErrorType 或空串
 */
function readErrorType(info: unknown): string {
	if (!info || typeof info !== 'object') return '';
	const typed = info as AdminApiErrorInfo;
	return typed.ErrorType ? String(typed.ErrorType) : '';
}

adminAxios.interceptors.request.use(
	(conf) => {
		const accessToken = Local.get(accessTokenKey);
		if (accessToken) {
			conf.headers = conf.headers ?? {};
			conf.headers['Authorization'] = `Bearer ${accessToken}`;

			try {
				const jwt: any = decryptJWT(accessToken);
				const exp = getJWTDate(jwt.exp as number);
				if (new Date() >= exp) {
					const refreshAccessToken = Local.get(refreshAccessTokenKey);
					if (refreshAccessToken) {
						conf.headers['X-Authorization'] = `Bearer ${refreshAccessToken}`;
					}
				}
			} catch {
				/* JWT 解析失败时仍带原 Bearer，交给后端拒绝 */
			}
		}

		const globalI18n = Local.get('themeConfig')?.globalI18n;
		if (globalI18n) {
			conf.headers = conf.headers ?? {};
			conf.headers['Accept-Language'] = globalI18n;
		}

		return conf;
	},
	(error) => {
		showNetworkErrorThrottled(error);
		return Promise.reject(error);
	}
);

adminAxios.interceptors.response.use(
	(res: AxiosResponse) => {
		const accessToken = res.headers?.[accessTokenKey];
		const refreshAccessToken = res.headers?.[refreshAccessTokenKey];
		if (accessToken === 'invalid_token') {
			clearAccessTokens();
		} else if (refreshAccessToken && accessToken && accessToken !== 'invalid_token') {
			Local.set(accessTokenKey, accessToken);
			Local.set(refreshAccessTokenKey, refreshAccessToken);
		}

		if (res.status === 401) {
			clearAccessTokens();
			return Promise.reject(new Error('未登录或登录已失效'));
		}
		if (res.status >= 400) {
			return Promise.reject(new Error(res.statusText || `请求失败 (${res.status})`));
		}

		const body = res.data as AdminApiResult;
		const cfg = res.config as AdminApiRequestConfig;

		if (body && typeof body === 'object' && Object.prototype.hasOwnProperty.call(body, 'success')) {
			if (!isAdminApiSuccess(body.success)) {
				const errorType = readErrorType(body.info);
				if (errorType === 'Login') {
					clearAccessTokens();
					return Promise.reject(new Error(resolveAdminApiMsg(body.msg) || '请先登录'));
				}

				const message = resolveAdminApiMsg(body.msg) || '操作失败';
				const err = new Error(message) as Error & { adminApi?: AdminApiResult; errorType?: string };
				err.adminApi = body;
				err.errorType = errorType;
				if (!cfg.silent) {
					ElMessage.error(message);
					markToastShown(err);
				}
				return Promise.reject(err);
			}

			if (cfg.raw) {
				return { ...res, data: body };
			}
			return { ...res, data: body.info };
		}

		// 不是 success/msg/info 形态：原样返回，避免误伤文件流等
		return res;
	},
	(error) => {
		if (error?.response?.status === 401) {
			clearAccessTokens();
		}
		const cfg = error?.config as AdminApiRequestConfig | undefined;
		if (!cfg?.silent) {
			showNetworkErrorThrottled(error);
		}
		return Promise.reject(error);
	}
);

/**
 * 发 `/admin` 业务请求；默认成功时 Promise 兑现 info。
 * @param config 请求配置（url 以 /admin 开头）
 * @returns info，或 raw 时为完整 { success, msg, info }
 */
export function adminRequest<T = unknown>(config: AdminApiRequestConfig): Promise<T> {
	const url = config.url ?? '';
	if (url && !url.startsWith('/admin') && !/^https?:\/\/[^/]+\/admin/i.test(url)) {
		console.warn('[admin-api] 建议只用于 /admin 路径：', url);
	}
	return adminAxios.request(config).then((res) => res.data as T);
}

/**
 * GET `/admin/...`。
 * @param url 路径，如 /admin/user/member/list
 * @param params 查询参数
 * @param config 额外配置
 * @returns info
 */
export function adminGet<T = unknown>(url: string, params?: Record<string, unknown>, config?: AdminApiRequestConfig): Promise<T> {
	return adminRequest<T>({ ...config, method: 'get', url, params });
}

/**
 * POST `/admin/...`，默认 Form 编码（对齐业务页参数习惯）。
 * @param url 路径
 * @param data 表单字段；传 FormData / URLSearchParams / 字符串时原样发送
 * @param config 额外配置；config.json === true 时按 JSON 发
 * @returns info
 */
export function adminPost<T = unknown>(
	url: string,
	data?: Record<string, unknown> | URLSearchParams | FormData | string,
	config?: AdminApiRequestConfig & { json?: boolean }
): Promise<T> {
	const { json, ...rest } = config ?? {};
	let body: unknown = data;
	const headers: Record<string, string> = { ...(rest.headers as Record<string, string>) };

	if (data && !(data instanceof URLSearchParams) && !(data instanceof FormData) && typeof data !== 'string') {
		if (json) {
			headers['Content-Type'] = 'application/json';
			body = data;
		} else {
			headers['Content-Type'] = 'application/x-www-form-urlencoded';
			body = toAdminForm(data);
		}
	}

	return adminRequest<T>({
		...rest,
		method: 'post',
		url,
		data: body,
		headers,
	});
}

/** 导出实例，仅特殊场景（上传进度等）使用 */
export { adminAxios };
