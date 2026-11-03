import * as CryptoES from 'crypto-es';

/**
 * 生成 KSort 签名（按 key 排序后拼接再 MD5 加密）
 * @param appId APP_ID
 * @param appKey APP_KEY
 * @param command 命令
 * @param data 数据（对象或字符串）
 * @param timestamp 时间戳（可选，不传则自动生成毫秒级时间戳）
 * @returns 包含签名的完整请求参数对象
 */
export function signatureByKSort(appId: string, appKey: string, command: string, data: any, timestamp: number | null = null) {
	// 数据为字符串则直接使用，否则序列化为 JSON 字符串
	const input = typeof data === 'string' ? data : JSON.stringify(data);

	// 未传入时间戳时，生成当前毫秒级时间戳
	if (!timestamp) {
		timestamp = new Date().getTime() - new Date('1970-01-01T00:00:00Z').getTime();
	}

	// 组装待签名的原始数据对象
	const sData: Record<string, any> = {
		app_id: appId,
		app_key: appKey,
		command,
		data: input,
		timestamp,
	};

	// 获取所有 key 并按字典序升序排序，保证签名结果稳定
	const keys = Object.keys(sData).sort();

	// 按 key=value 形式拼接，key 之间用逗号分隔
	let result = '';
	for (let i = 0; i < keys.length; i++) {
		const key = keys[i];
		result += key + '=' + sData[key];
		if (i < keys.length - 1) result += ',';
	}

	// MD5 加密并转为小写十六进制字符串
	const hexString = CryptoES.MD5(result).toString(CryptoES.Hex);
	const signature = hexString.toLowerCase();

	// 返回原始参数及生成的签名
	return {
		app_id: appId,
		app_key: appKey,
		command,
		data: input,
		timestamp,
		signature,
	};
}

/**
 * 计算数据的 MD5 签名（先规范化再 MD5）
 * @param data 待签名的数据，支持对象、数组、字符串等任意类型
 * @returns 32 位小写 MD5 签名字符串，data 为空或异常时返回空字符串
 */
export function signatureByMD5(data: any): string {
	// 空数据直接返回空字符串，避免无意义计算
	if (!data) return '';

	try {
		// 规范化数据，确保相同语义的数据得到一致的签名
		const normalizedData = normalizeData(data);
		// 序列化后计算 MD5 并转为字符串
		return CryptoES.MD5(JSON.stringify(normalizedData)).toString();
	} catch (e) {
		// 捕获异常避免影响主流程，同时输出错误日志便于排查
		console.error('计算数据签名失败:', e);
		return '';
	}
}

/**
 * 递归规范化数据，保证 JSON 序列化结果稳定
 * @param obj 任意待规范化的数据
 * @returns 规范化后的数据（null/undefined 转为空字符串，基本类型转为字符串）
 */
function normalizeData(obj: any): any {
	// null 和 undefined 统一转为空字符串，避免序列化差异
	if (obj === null || obj === undefined) return '';

	// 日期对象转为 ISO 标准字符串，保证跨时区一致
	if (obj instanceof Date) return obj.toISOString();

	// 数组递归处理每一项
	if (Array.isArray(obj)) return obj.map((item) => normalizeData(item));

	// 对象递归处理，并对 key 排序保证序列化稳定
	if (typeof obj === 'object') {
		const normalized: Record<string, any> = {};
		const keys = Object.keys(obj).sort();
		for (const key of keys) {
			normalized[key] = normalizeData(obj[key]);
		}
		return normalized;
	}

	// 基本类型统一转为字符串，避免数字与字符串比较产生误判
	return String(obj);
}
