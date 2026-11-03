/**
 * 当前版本：v1.4.0
 * 使用描述：https://editor.swagger.io 代码生成 typescript-axios 辅组工具库
 * 依赖说明：适配 axios 版本：v0.21.4
 * 视频教程：https://www.bilibili.com/video/BV1EW4y1C71D
 */

import globalAxios, { AxiosInstance } from 'axios';
import { Configuration } from '../api-services/system';
import { BaseAPI, BASE_PATH } from '../api-services/system/base';

import { ElMessage } from 'element-plus';
import { Local, Session } from '../utils/storage';
import { acceptSession, buildEnvelope, decryptText, encodeEnvelope, isCryptoEnabled, isEncryptedPayload, isRekeySignal, loadCryptoConfig, resetSession } from './crypto/transport';
import { rewriteProApiToSimpX } from './simpx-api-rewrite';

/**
 * 请求错误是否已由拦截器弹过提示。下游 catch / runAdminVxeQuery 见此标记则勿再 ElMessage。
 */
export function markToastShown(error: any) {
	if (error && typeof error === 'object') (error as any).__toastShown = true;
}

/** 是否已弹过错误提示（含节流窗口内被吞掉的同批网络错误）。 */
export function isToastShown(error: any): boolean {
	return !!(error && typeof error === 'object' && (error as any).__toastShown);
}

/**
 * 把 Axios / 网络错误收成中文文案（不弹窗）。
 */
export function formatNetworkErrorMessage(error: any): string {
	const status = error?.response?.status;
	const code = error?.code;
	if (status === 502 || status === 503 || status === 504) {
		return '后端服务暂不可用，请稍后重试（' + status + '）';
	}
	if (code === 'ERR_NETWORK' || error?.message === 'Network Error') {
		return '网络连接异常，请检查网络或后端服务';
	}
	if (code === 'ECONNABORTED' || /timeout/i.test(String(error?.message))) {
		return '请求超时，请稍后重试';
	}
	if (typeof error === 'string') return error;
	if (status) return `请求失败 (${status})`;
	const raw = String(error?.message || '请求失败');
	// 吞掉 Axios 默认英文，避免与中文提示叠两条
	if (/^Request failed with status code \d+/i.test(raw) || raw === 'Network Error') {
		return status ? `请求失败 (${status})` : '请求失败';
	}
	return raw;
}

/**
 * 网络错误提示节流器：同一时间窗（默认 2s）内只弹一次。
 * 适用于后端宕机 / 502 / 超时等场景，避免多个并行请求同时失败时弹出多个重复提示。
 * 无论是否实际弹出，都会 markToastShown，避免业务 catch 再用英文 message 再弹一条。
 * 若 AxiosError 已指定 customCatch，调用方应自己处理，不走此函数。
 */
let __lastNetErrorAt = 0;
const NET_ERROR_WINDOW_MS = 2000;
export function showNetworkErrorThrottled(error: any, windowMs: number = NET_ERROR_WINDOW_MS) {
	markToastShown(error);
	const msg = formatNetworkErrorMessage(error);
	// 顺带把 message 换成中文，后续若有人读 e.message 也不会再蹦英文
	if (error && typeof error === 'object' && typeof error.message === 'string') {
		try {
			error.message = msg;
		} catch {
			/* AxiosError.message 偶发只读，忽略 */
		}
	}

	const now = Date.now();
	if (now - __lastNetErrorAt < windowMs) return;
	__lastNetErrorAt = now;
	ElMessage.error({ message: msg, duration: 3000 });
}

/**
 * 手动打开节流窗口：调用后的 windowMs 内再来的错误不会再弹。
 * 适用于路由守卫等场景：已经弹过一个业务级提示，后续批量失败的子请求不再弹。
 */
export function suppressNetworkErrorFor(ms: number = NET_ERROR_WINDOW_MS) {
	__lastNetErrorAt = Date.now() + ms - NET_ERROR_WINDOW_MS;
}

/**
 * 业务 catch 统一入口：拦截器已弹过则跳过；否则弹中文网络/业务文案。
 * 页面请用本函数替代 `ElMessage.error(e?.message)`，避免 502 中英双提示。
 */
export function showCaughtError(error: any, fallback = '操作失败') {
	if (error === 'cancel' || error === 'close') return;
	if (isToastShown(error)) return;
	const msg = formatNetworkErrorMessage(error) || fallback;
	markToastShown(error);
	ElMessage.error({ message: msg || fallback, duration: 3000 });
}

// 接口服务器配置
export const serveConfig = new Configuration({
	basePath: import.meta.env.DEV ? '' : window.__env__.VITE_API_URL,
});

// ===================== 传输加密（国密 SM2 + SM4） =====================

/** 信封查询参数名，与后端 TransportCryptoMiddleware.QueryField 一致 */
const CRYPTO_QUERY_FIELD = '__c';

/** 不参与加密的路径前缀，与后端内置排除项保持一致 */
const CRYPTO_EXCLUDE = ['/swagger', '/mcp', '/hubs', '/api/sysCrypto', '/api/sysCommon/smKeyPair'];

/** 独立的明文 axios 实例：不挂任何拦截器，专用于加密握手，避免与加密逻辑形成递归 */
const plainAxios: AxiosInstance = globalAxios.create({ timeout: 1000 * 10 });

/** 握手 Promise：全局只执行一次，失败即按明文通信，不阻塞应用 */
let cryptoHandshake: Promise<void> | null = null;

/**
 * 完成一次加密握手（拉取开关与公钥）
 * @returns Promise
 */
/**
 * SimpX 暂未迁 Pro 传输加密中间件：跳过握手，避免 /api/sysCrypto/transportConfig 404 刷屏。
 * 以后接上 TransportCrypto 后再改回请求 transportConfig。
 */
export const ensureCryptoHandshake = (): Promise<void> => {
	if (!cryptoHandshake) {
		cryptoHandshake = Promise.resolve();
	}
	return cryptoHandshake;
};

/**
 * 拆出 URL 的路径部分（兼容绝对地址）
 * @param url 请求地址
 * @returns 以 / 开头的路径
 */
const urlPath = (url: string): string => {
	if (/^https?:\/\//i.test(url)) {
		try {
			return new URL(url).pathname;
		} catch {
			return url.split('?')[0];
		}
	}
	return url.split('?')[0];
};

/**
 * 拆出 URL 中位于路径之前的前缀（协议+主机+basePath），重组 URL 时必须保留
 * @param url 请求地址
 * @returns 前缀，相对地址时为空串
 */
const urlPrefix = (url: string): string => {
	const main = url.split('?')[0];
	return main.slice(0, main.length - urlPath(url).length);
};

/** 当前请求是否适合加密（排除文件上传、下载流、文档等） */
const canEncrypt = (conf: any): boolean => {
	if (conf.__cryptoSkip) return false;
	const path = urlPath(conf.url ?? '');
	if (!path.startsWith('/api')) return false;
	for (const item of CRYPTO_EXCLUDE) {
		if (path.startsWith(item)) return false;
	}

	const data = conf.data;
	if (data instanceof FormData || data instanceof Blob || data instanceof URLSearchParams || data instanceof ArrayBuffer) return false;
	// 非 JSON 载荷（表单串等）保持原样，后端对非 JSON 请求体同样不解密
	if (typeof data === 'string' && data.length > 0 && data[0] !== '{' && data[0] !== '[') return false;
	return true;
};

// token 键定义
export const accessTokenKey = 'access-token';
export const refreshAccessTokenKey = `x-${accessTokenKey}`;

// 获取 token
export const getToken = () => {
	return Local.get(accessTokenKey);
};

// 获取请求头 token
export const getHeader = () => {
	return { authorization: 'Bearer ' + getToken() };
};

/** 退出流程进行中：拦截器不再自行刷新页面，跳转时机交给调用方 */
let authTerminating = false;

/** 设置退出流程状态：强制下线在弹提示前置位，弹框确认后复位并自行跳转 */
export const setAuthTerminating = (value: boolean) => {
	authTerminating = value;
};

// 清除 token
export const clearAccessTokens = () => {
	clearTokens();

	// 退出流程中由调用方统一控制跳转时机，避免拦截器提前刷新把提示弹窗顶掉
	if (authTerminating) return;

	// 清除hash中的token值
	if (window.location.hash.startsWith('#/login')) {
		window.location.hash = window.location.hash.replace(/([?&])token=[^&]*&?/, '$1').replace(/&$/, '');
	} else {
		// 刷新浏览器
		window.location.reload();
	}
};

// 清除 token
export const clearTokens = () => {
	Local.remove(accessTokenKey);
	Local.remove(refreshAccessTokenKey);
	Session.clear();
};

// axios 默认实例
export const axiosInstance: AxiosInstance = globalAxios;

// 这里可以配置 axios 更多选项 =========================================
axiosInstance.defaults.timeout = 1000 * 60 * 10; // 设置超时，默认 10 分钟

// axios 请求拦截（async：传输加密握手需在发出前完成）
axiosInstance.interceptors.request.use(
	async (conf) => {
		// Pro 客户端路径 → SimpX 租户后台
		conf.url = rewriteProApiToSimpX(conf.url, 'admin');

		// 获取本地的 token或session中的token
		const accessToken = Local.get(accessTokenKey) ?? Session.get('token');
		if (accessToken) {
			// 将 token 添加到请求报文头中
			conf.headers!['Authorization'] = `Bearer ${accessToken}`;

			// 判断 accessToken 是否过期
			const jwt: any = decryptJWT(accessToken);
			const exp = getJWTDate(jwt.exp as number);

			// token 已经过期
			if (new Date() >= exp) {
				// 获取刷新 token
				const refreshAccessToken = Local.get(refreshAccessTokenKey);

				// 携带刷新 token
				if (refreshAccessToken) {
					conf.headers!['X-Authorization'] = `Bearer ${refreshAccessToken}`;
				}
			}
		}

		// 这里编写请求拦截代码 =========================================

		// 获取前端设置的语言
		const globalI18n = Local.get('themeConfig')?.globalI18n;
		if (globalI18n) {
			// 添加到请求报文头中
			conf.headers!['Accept-Language'] = globalI18n;
		}

		// 传输加密：握手 + 封装信封（握手失败自动降级为明文）
		await ensureCryptoHandshake();
		if (isCryptoEnabled() && canEncrypt(conf)) {
			try {
				const raw = conf.url ?? '';
				const qIndex = raw.indexOf('?');
				const path = urlPath(raw);
				const prefix = urlPrefix(raw);
				const query = qIndex >= 0 ? raw.slice(qIndex + 1) : '';
				const body = typeof conf.data === 'string' ? conf.data : conf.data == null ? '' : JSON.stringify(conf.data);
				const { envelope, session } = buildEnvelope(query, body);
				conf.__cryptoSession = session;
				// 留存原始地址与报文，供会话失效重试时复原
				conf.__cryptoRawUrl = conf.url;
				conf.__cryptoRawData = conf.data;

				const method = (conf.method ?? 'get').toLowerCase();
				// GET/HEAD 或无请求体时只能通过查询参数携带信封
				if (method === 'get' || method === 'head' || !body) {
					conf.url = `${prefix}${path}?${CRYPTO_QUERY_FIELD}=${encodeURIComponent(encodeEnvelope(envelope))}`;
					conf.data = undefined;
				} else {
					conf.url = `${prefix}${path}`;
					conf.data = JSON.stringify(envelope);
				}
			} catch {
				// 加密异常时回退明文，优先保证业务可用
				delete conf.__cryptoSession;
			}
		}

		return conf;
	},
	(error) => {
		// 请求阶段失败：只走节流中文提示，禁止再 ElMessage.error(error)（会叠 Axios 英文第二条）
		showNetworkErrorThrottled(error);
		return Promise.reject(error);
	}
);

// axios 响应拦截
axiosInstance.interceptors.response.use(
	(res) => {
		// 响应体解密：仅在本次请求发过信封且响应确实是信封时进行
		const conf: any = res.config;
		const session = conf?.__cryptoSession;
		if (isEncryptedPayload(res.data)) {
			const plain = session ? decryptText(res.data.d, session) : null;
			if (plain == null) throw new Error('响应解密失败，请检查服务端加密配置');
			// 记下服务端下发的会话标识，后续请求不再做 SM2 协商
			acceptSession(res.data.s, res.data.e, session);
			res.data = JSON.parse(plain);
		} else if (isRekeySignal(res.data) && !conf?.__cryptoRetried) {
			// 会话已失效（服务端重启 / 会话过期）：重新协商一次后重发，业务层无感知
			resetSession();
			conf.__cryptoRetried = true;
			conf.url = conf.__cryptoRawUrl ?? conf.url;
			conf.data = conf.__cryptoRawData;
			return axiosInstance.request(conf);
		}

		// 处理 401
		if (res.status === 401) {
			clearAccessTokens();
		}
		// 处理未进行规范化处理的
		else if (res.status >= 400) {
			throw new Error(res.statusText || 'Request Error.');
		}

		// 处理规范化结果错误
		var serve = res.data;
		if (serve && serve.hasOwnProperty('errors') && serve.errors) {
			throw new Error(JSON.stringify(serve.errors || 'Request Error.'));
		}

		// 读取响应报文头 token 信息
		var accessToken = res.headers[accessTokenKey];
		var refreshAccessToken = res.headers[refreshAccessTokenKey];

		// 判断是否是无效 token
		if (accessToken === 'invalid_token') {
			clearAccessTokens();
		}
		// 判断是否存在刷新 token，如果存在则存储在本地
		else if (refreshAccessToken && accessToken && accessToken !== 'invalid_token') {
			Local.set(accessTokenKey, accessToken);
			Local.set(refreshAccessTokenKey, refreshAccessToken);
		}

		// 响应拦截及自定义处理
		if (serve.code === 401) {
			clearAccessTokens();
		} else if (serve.code === undefined) {
			return Promise.resolve(res);
		} else if (serve.code !== 200) {
			let message = handleResponse(serve.message, 5);
			ElMessage.error({ dangerouslyUseHTMLString: true, message: message });
			// 用户自定义处理异常：抛出已打标错误，避免 catch 再叠一条
			if (res.config?.customCatch) {
				const bizErr = new Error(typeof serve.message === 'string' ? serve.message : message) as Error & { __toastShown?: boolean };
				markToastShown(bizErr);
				throw bizErr;
			}
		}

		return res;
	},
	(error) => {
		// 处理响应错误
		if (error.response) {
			// 错误响应同样可能带信封，解密后再交给业务层，避免提示内容是一串密文
			const session = (error.config as any)?.__cryptoSession;
			if (session && isEncryptedPayload(error.response.data)) {
				const plain = decryptText(error.response.data.d, session);
				if (plain != null) {
					try {
						error.response.data = JSON.parse(plain);
					} catch {
						/* 保留原始响应体 */
					}
				}
			}

			if (error.response.status === 401) {
				clearAccessTokens();
			}
		}

		// 用户自定义处理异常
		if (!error.config?.customCatch) {
			// 响应错误代码及自定义处理（节流，同窗口内不重复弹）
			showNetworkErrorThrottled(error);
		}

		return Promise.reject(error);
	}
);

// 处理响应数据
const handleResponse = (input: any, col: number = 5) => {
	const messageList = [] as string[];

	// 正则：匹配 [XXX] 开头的错误码前缀（例如 [D0010]、[E123]）
	const errorCodeRegex = /^\[[A-Za-z0-9_]+\]\s*/;

	function process(value: any) {
		if (value == null) return;

		if (typeof value === 'string') {
			// 去掉错误码前缀，保留提示文本
			const clean = value.replace(errorCodeRegex, '');
			if (clean.trim()) messageList.push(clean.trim());
		} else if (Array.isArray(value)) {
			// 递归处理数组
			value.forEach((item) => process(item));
		} else if (typeof value === 'object') {
			// 递归处理对象的每个值
			Object.values(value).forEach((val) => process(val));
		}
	}

	try {
		// 尝试将输入解析为 JSON（如果是字符串）
		const parsed = typeof input === 'string' ? JSON.parse(input) : input;
		process(parsed);
	} catch (e) {
		// 如果不是合法 JSON，则当作普通字符串处理
		process(input);
	}

	col = col < 4 ? 4 : col;
	return messageList
		.reduce((rows: any[], msg, i) => {
			if (i % col === 0) rows.push([]);
			rows[rows.length - 1].push(msg);
			return rows;
		}, [])
		.map((row) => row.join('; '))
		.join('<br/>');
};

/**
 * 包装 Promise 并返回 [Error, any]
 * @param promise Promise 方法
 * @param errorExt 自定义错误信息（拓展）
 * @returns [Error, any]
 */
export function feature<T, U = Error>(promise: Promise<T>, errorExt?: object): Promise<[U, undefined] | [null, T]> {
	return promise
		.then<[null, T]>((data: T) => [null, data])
		.catch<[U, undefined]>((err: U) => {
			if (errorExt) {
				const parsedError = Object.assign({}, err, errorExt);
				return [parsedError, undefined];
			}

			return [err, undefined];
		});
}

/**
 * 获取/创建服务 API 实例
 * @param apiType BaseAPI 派生类型
 * @param configuration 服务器配置对象
 * @param basePath 服务器地址
 * @param axiosObject axios 实例
 * @returns 服务API 实例
 */
export function getAPI<T>(
	// eslint-disable-next-line no-unused-vars
	apiType: new (configuration?: Configuration, basePath?: string, axiosInstance?: AxiosInstance) => T,
	configuration: Configuration = serveConfig,
	basePath: string = BASE_PATH,
	axiosObject: AxiosInstance = axiosInstance
) {
	return new apiType(configuration, basePath, axiosObject);
}

/**
 * 解密 JWT token 的信息
 * @param token jwt token 字符串
 * @returns <any>object
 */
export function decryptJWT(token: string): any {
	token = token.replace(/_/g, '/').replace(/-/g, '+');
	var json = decodeURIComponent(escape(window.atob(token.split('.')[1])));
	return JSON.parse(json);
}

/**
 * 将 JWT 时间戳转换成 Date
 * @description 主要针对 `exp`，`iat`，`nbf`
 * @param timestamp 时间戳
 * @returns Date 对象
 */
export function getJWTDate(timestamp: number): Date {
	return new Date(timestamp * 1000);
}

/**
 * 实现异步延迟
 * @param delay 延迟时间（毫秒）
 * @returns
 */
export function sleep(delay: number) {
	return new Promise((resolve) => setTimeout(resolve, delay));
}
