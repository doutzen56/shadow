/**
 * 前后端传输加密（国密 SM2 + SM4）
 *
 * 方案：SM2 协商会话密钥 + SM4-CBC 加密报文。
 * 客户端随机生成 16 字节 SM4 密钥与 16 字节偏移量，用 SM2 公钥封装后随报文上传一次；
 * 服务端解出后登记会话并回传会话标识与过期时间，后续报文只带会话标识、只做 SM4 对称运算。
 *
 * 之所以不让每个请求都走 SM2：椭圆曲线运算在浏览器里是纯 JS 大数计算，
 * 单次数十毫秒且阻塞主线程；协商一次、复用一小时是商业化产品的通行做法（等价于 TLS 会话复用）。
 */

import { sm2, sm4 } from 'sm-crypto-v2';

/** 会话密钥（十六进制字符串） */
export interface CryptoSession {
	key: string;
	iv: string;
}

/** 加密信封 */
interface Envelope {
	v: number;
	s?: string;
	k?: string;
	q?: string;
	b?: string;
	d?: string;
	e?: number;
	t: number;
}

/** 会话过期前多久开始轮换，留出在途请求的余量 */
const RENEW_AHEAD_MS = 30_000;

/** 服务端未下发过期时间时的兜底时长（30 分钟） */
const FALLBACK_TTL_MS = 30 * 60 * 1000;

/** 加密开关与公钥，由服务端握手接口下发 */
let enabled = false;
let publicKey = '';

/** 握手 Promise（全局只发一次） */
let ready: Promise<void> | null = null;

/** 当前会话：sessionId 为空表示服务端尚未确认，本次请求需携带 SM2 封装的密钥 */
let session: CryptoSession | null = null;
let sessionId = '';
let sessionExpireAt = 0;

/** Base64Url 编码 */
const toBase64Url = (bytes: Uint8Array): string => {
	let bin = '';
	const chunk = 0x8000;
	for (let i = 0; i < bytes.length; i += chunk) {
		bin += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
	}
	return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
};

/** Base64Url 解码 */
const fromBase64Url = (text: string): Uint8Array => {
	const b = text.replace(/-/g, '+').replace(/_/g, '/');
	const pad = b.length % 4 === 0 ? '' : '='.repeat(4 - (b.length % 4));
	const bin = atob(b + pad);
	const out = new Uint8Array(bin.length);
	for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
	return out;
};

/** 字节数组转小写十六进制 */
const toHex = (bytes: Uint8Array): string => {
	let s = '';
	for (let i = 0; i < bytes.length; i++) s += bytes[i].toString(16).padStart(2, '0');
	return s;
};

/** 生成随机会话密钥 */
const createSession = (): CryptoSession => {
	const bytes = new Uint8Array(32);
	(globalThis.crypto || window.crypto).getRandomValues(bytes);
	return { key: toHex(bytes.subarray(0, 16)), iv: toHex(bytes.subarray(16)) };
};

/** SM4-CBC 加密 */
const encryptText = (plain: string, session: CryptoSession): string => {
	const out = sm4.encrypt(plain, session.key, { mode: 'cbc', iv: session.iv, padding: 'pkcs#7', output: 'array' });
	return toBase64Url(out as Uint8Array);
};

/**
 * 取当前可用会话，不存在或临近过期则新建（新建后需重新协商，本次请求会携带 SM2 封装的密钥）
 * @returns 会话密钥
 */
const ensureSession = (): CryptoSession => {
	if (!session || Date.now() >= sessionExpireAt - RENEW_AHEAD_MS) {
		session = createSession();
		sessionId = '';
	}
	return session;
};

/**
 * 拉取服务端加密配置（明文接口，仅首次执行一次）
 * @param request 发起请求的函数，由调用方注入以避免与 axios 拦截器循环依赖
 * @returns Promise
 */
export const loadCryptoConfig = (request: () => Promise<any>): Promise<void> => {
	if (ready) return ready;

	ready = (async () => {
		try {
			const res = await request();
			const data = res?.data?.result ?? res?.data ?? {};
			enabled = !!data.enabled;
			publicKey = (data.publicKey || '').trim();
		} catch {
			// 握手失败即按明文通信，避免整个应用不可用
			enabled = false;
			publicKey = '';
		}
	})();

	return ready;
};

/**
 * 当前是否已启用传输加密
 * @returns 已启用返回 true
 */
export const isCryptoEnabled = (): boolean => enabled && !!publicKey;

/**
 * 接收服务端下发的会话标识，之后的请求只带标识、不再做 SM2 运算
 * @param id 会话标识
 * @param expireAt 过期时间（毫秒时间戳）
 * @param used 本次请求实际使用的会话，已轮换则丢弃该结果
 */
export const acceptSession = (id: string, expireAt: number, used?: CryptoSession | null): void => {
	if (!id || !used || used !== session) return;
	sessionId = id;
	sessionExpireAt = expireAt > 0 ? expireAt : Date.now() + FALLBACK_TTL_MS;
};

/**
 * 会话失效（服务端重启 / 已过期）时清空，下次请求自动重新协商
 */
export const resetSession = (): void => {
	session = null;
	sessionId = '';
	sessionExpireAt = 0;
};

/**
 * 构造加密信封
 * @param query 查询串
 * @param body 请求体原文
 * @returns 信封与本次请求使用的会话密钥
 */
export const buildEnvelope = (query: string, body: string): { envelope: Envelope; session: CryptoSession } => {
	const s = ensureSession();
	// 用 SM2 公钥封装密钥材料（C1C3C2，与后端一致）；已有会话标识时无需封装
	const envelope: Envelope = sessionId ? { v: 1, s: sessionId, t: Date.now() } : { v: 1, k: sm2.doEncrypt(s.key + s.iv, publicKey, 1) as string, t: Date.now() };
	if (query) envelope.q = encryptText(query, s);
	if (body) envelope.b = encryptText(body, s);
	return { envelope, session: s };
};

/**
 * 将信封编码为可放在查询参数中的字符串（GET 无请求体时走这条路）
 * @param envelope 信封
 * @returns Base64Url 字符串
 */
export const encodeEnvelope = (envelope: object): string => {
	return toBase64Url(new TextEncoder().encode(JSON.stringify(envelope)));
};

/**
 * SM4-CBC 解密
 * @param cipher Base64Url 密文
 * @param session 本次请求使用的会话密钥
 * @returns 明文，失败返回 null
 */
export const decryptText = (cipher: string, session: CryptoSession): string | null => {
	try {
		const out = sm4.decrypt(fromBase64Url(cipher), session.key, { mode: 'cbc', iv: session.iv, padding: 'pkcs#7', output: 'string' });
		return out as string;
	} catch {
		return null;
	}
};

/**
 * 判断响应体是否为加密信封
 * @param data 响应数据
 * @returns 是返回 true
 */
export const isEncryptedPayload = (data: any): boolean => {
	return !!data && typeof data === 'object' && data.v === 1 && typeof data.d === 'string';
};

/**
 * 判断响应是否为「会话失效，请重新协商」指令
 * @param data 响应数据
 * @returns 是返回 true
 */
export const isRekeySignal = (data: any): boolean => {
	return !!data && typeof data === 'object' && data.v === 1 && data.r === 1;
};
