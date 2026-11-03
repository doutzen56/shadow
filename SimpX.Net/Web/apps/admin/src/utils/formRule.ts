import type { FormItemRule } from 'element-plus';
import { ElMessage } from 'element-plus';
import { nextTick, Ref, watch } from 'vue';

/** 输入时触发（change） */
const TRIGGER_INPUT: FormItemRule['trigger'] = 'change';
/** 失焦时触发（blur） */
const TRIGGER_BLUR: FormItemRule['trigger'] = 'blur';

/**
 * 表单校验规则预设 hooks
 * 返回一组工厂函数，每个函数调用后返回 Element Plus 可用的 FormItemRule
 * 用法：`:rules="[required('姓名'), idCard()]"` 或 `:rules="[idCard(() => cond)]"`
 */
export function useFormRulePresets() {
	/**
	 * 必填规则
	 * @param label 字段名（用于错误提示）
	 * @param trigger 触发时机，默认 blur
	 */
	const required = (label = '此项', trigger: FormItemRule['trigger'] = TRIGGER_BLUR): FormItemRule => ({
		required: true,
		message: `${label}为必填项`,
		trigger,
	});

	// TODO 如果数据发生变化，自动触发验证取消错误标记
	/**
	 * 条件必填规则（根据传入的布尔值决定是否必填）
	 * 注意：这里是「静态」必填，不会随数据变化自动重新校验
	 * @param required 是否必填
	 * @param label 字段名
	 * @param trigger 触发时机
	 */
	const requiredIf = (required?: boolean, label = '此项', trigger: FormItemRule['trigger'] = TRIGGER_BLUR): FormItemRule => ({
		required: required ?? false,
		message: `${label}为必填项`,
		trigger,
	});

	/**
	 * 动态条件校验：当 shouldError() 返回 true 时报错
	 * 内部通过 watch 监听 shouldError 的变化，自动重新校验对应字段
	 * @param shouldError 返回 true 表示当前字段应报错
	 * @param formRef 表单 ref，用于主动触发 validateField
	 * @param label 字段名
	 */
	const validatorIf = (shouldError: () => boolean, formRef?: Ref<any>, label: string = '此项') => {
		let field = '';
		// 监听条件变化，变化后自动重新校验当前字段
		watch(
			() => shouldError(),
			() => nextTick(() => formRef?.value?.validateField(field).catch(() => {})),
			{ immediate: false, deep: true }
		);

		return {
			validator: (_: any, __: any, callback: (error?: Error) => void) => {
				// _ 是 rule 对象，从中取出当前字段名，供 watch 使用
				field = _.field;
				if (shouldError()) {
					callback(new Error(`${label}为必填`));
				} else {
					callback();
				}
			},
			trigger: ['blur', 'change'] as const,
		};
	};

	/**
	 * 最大长度校验
	 * @param len 最大字符数
	 * @param label 字段名
	 * @param trigger 触发时机，默认 change
	 */
	const maxLen = (len: number, label = '此项', trigger = TRIGGER_INPUT): FormItemRule => ({
		max: len,
		message: `${label}长度不能超过 ${len} 个字符`,
		trigger,
	});

	/**
	 * 最小长度校验
	 * @param len 最小字符数
	 * @param label 字段名
	 * @param trigger 触发时机，默认 change
	 */
	const minLen = (len: number, label = '此项', trigger = TRIGGER_INPUT): FormItemRule => ({
		min: len,
		message: `${label}长度不能少于 ${len} 个字符`,
		trigger,
	});

	/**
	 * 正则匹配校验
	 * @param re 正则表达式
	 * @param msg 错误提示
	 * @param trigger 触发时机，默认 blur
	 */
	const pattern = (re: RegExp, msg: string, trigger = TRIGGER_BLUR): FormItemRule => ({
		pattern: re,
		message: msg,
		trigger,
	});

	/**
	 * 通用自定义校验（返回 FormItemRule 对象形式）
	 * @param fn 返回 true 表示通过；返回 string 表示错误提示；支持 Promise
	 * @param trigger 触发时机
	 * 用法：`:rules="[validator(v => v > 0 ? true : '必须大于0')]"`
	 */
	const validator = (fn: (value: any) => true | string | Promise<true | string>, trigger = TRIGGER_BLUR): FormItemRule => ({
		trigger,
		validator: (_rule, value, callback) => {
			Promise.resolve(fn(value))
				.then((res) => {
					if (res === true) return callback();
					return callback(new Error(res));
				})
				.catch((err) => callback(err instanceof Error ? err : new Error(String(err))));
		},
	});

	/** 手机号 / 固定电话校验（对象形式） */
	const phone = () => pattern(/^(1[3-9]\d{9})$|^(0\d{2,3}[-]?\d{7,8})$|^(\(\d{3,4}\)\d{7,8})$|^(\d{7,})$/, '请输入有效的手机号或电话号');

	/**
	 * 身份证校验（对象形式，直接放进 :rules 数组）
	 * @param shouldValidate 可选，返回 false 时跳过校验（用于「只有身份证才校验」）
	 *
	 * 校验策略：
	 * - 空值放行，交给 required 规则处理
	 * - 15 位：老身份证，仅结构校验
	 * - 18 位：新身份证，含校验码（MOD 11-2）
	 *
	 * 用法：
	 *   `:rules="[idCard()]"`
	 *   `:rules="[idCard(() => state.ruleForm.cardType === 0)]"`
	 */
	const idCard = (shouldValidate?: () => boolean) =>
		validator((value) => {
			// 条件不满足 -> 跳过校验
			if (shouldValidate && !shouldValidate()) return true;
			if (!value) return true; // 如果为空，让required规则处理

			const idCard = String(value).trim();

			// 15位身份证号验证（老身份证）
			if (idCard.length === 15) {
				const reg15 = /^[1-9]\d{5}\d{2}((0[1-9])|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}$/;
				if (!reg15.test(idCard)) {
					return '请输入有效的15位身份证号';
				}
				return true;
			}

			// 18位身份证号验证（新身份证）
			if (idCard.length === 18) {
				const reg18 = /^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}(\d|X|x)$/;
				if (!reg18.test(idCard)) {
					return '请输入有效的18位身份证号';
				}

				// 验证校验码
				const factor = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
				const parity = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];
				let sum = 0;
				let ai = 0;
				let wi = 0;

				for (let i = 0; i < 17; i++) {
					ai = parseInt(idCard[i]);
					wi = factor[i];
					sum += ai * wi;
				}

				const last = parity[sum % 11];
				if (last !== idCard[17].toUpperCase()) {
					return '身份证号校验码错误';
				}

				return true;
			}

			return '身份证号必须是15位或18位';
		}, TRIGGER_BLUR);

	/** 邮箱校验 */
	const email = () => pattern(/^[^\s@]+@[^\s@]+\.[^\s@]+$/, '请输入有效的邮箱地址');

	/**
	 * 纯数字校验
	 * @param label 字段名
	 * @param trigger 触发时机，默认 change
	 */
	const digits = (label = '此项', trigger = TRIGGER_INPUT) => pattern(/^\d+$/, `${label}需为纯数字`, trigger);

	/**
	 * 整数范围校验（必填语义：空值会提示"请输入xxx"）
	 * @param min 最小值
	 * @param max 最大值
	 * @param label 字段名
	 */
	const intRange = (min: number, max: number, label = '数值') =>
		validator((v) => {
			if (v === '' || v === undefined || v === null) return `请输入${label}`;
			if (!Number.isInteger(Number(v))) return `${label}需为整数`;
			const n = Number(v);
			if (n < min || n > max) return `${label}需在 ${min}~${max} 之间`;
			return true;
		}, TRIGGER_INPUT);

	/**
	 * 两次输入是否一致
	 * @param getOther 返回被比较值（通常是另一个字段的值）
	 * @param label 字段名
	 */
	const confirmSame = (getOther: () => any, label = '两次输入') => validator((v) => (v === getOther() ? true : `${label}不一致`), TRIGGER_INPUT);

	/** 将任意值转为 Date（失败返回 null），内部工具函数 */
	const toDate = (v: any): Date | null => {
		if (v instanceof Date && !isNaN(v.getTime())) return v;
		if (typeof v === 'number') {
			const d = new Date(v);
			return isNaN(d.getTime()) ? null : d;
		}
		if (typeof v === 'string' && v) {
			const d = new Date(v);
			return isNaN(d.getTime()) ? null : d;
		}
		return null;
	};

	/**
	 * 日期必须晚于今天
	 * @param label 字段名
	 * @param trigger 触发时机，默认 change
	 */
	const dateAfterToday = (label = '日期', trigger = TRIGGER_INPUT) =>
		validator((v) => {
			if (v === '' || v === undefined || v === null) return true;
			const d = toDate(v);
			if (!d) return `请选择有效的${label}`;
			const today = new Date();
			today.setHours(0, 0, 0, 0);
			return d.getTime() > today.getTime() ? true : `${label}必须在今天之后`;
		}, trigger);

	/**
	 * 日期必须早于今天
	 * @param label 字段名
	 * @param trigger 触发时机，默认 change
	 */
	const dateBeforeToday = (label = '日期', trigger = TRIGGER_INPUT) =>
		validator((v) => {
			if (v === '' || v === undefined || v === null) return true;
			const d = toDate(v);
			if (!d) return `请选择有效的${label}`;
			const today = new Date();
			today.setHours(0, 0, 0, 0);
			return d.getTime() < today.getTime() ? true : `${label}必须在今天之前`;
		}, trigger);

	/**
	 * 数值精度校验（整数位 + 小数位）
	 * @param maxIntDigits 最大整数位数，null 表示不限
	 * @param maxFracDigits 最大小数位数，null 表示不限；0 表示必须为整数
	 * @param label 字段名
	 */
	const numberPrecision = (maxIntDigits: number | null, maxFracDigits: number | null, label = '此项') =>
		validator((v) => {
			if (v === '' || v === undefined || v === null) return true;

			const str = String(v).trim();

			// 允许 0、整数或小数（正数），不允许负数与多余小数点
			if (!/^\d+(?:\.\d+)?$/.test(str)) {
				return `${label}必须为数字`;
			}

			const [intPart, fracPart = ''] = str.split('.');
			if (maxIntDigits !== null) {
				if (intPart.length > maxIntDigits) {
					return `${label}整数位不能超过${maxIntDigits}位`;
				}
			}

			if (fracPart && maxFracDigits !== null && fracPart.length > maxFracDigits) {
				if (maxFracDigits === 0) {
					return `${label}必须为整数`;
				}
				return `${label}小数位不能超过${maxFracDigits}位`;
			}

			return true;
		}, TRIGGER_BLUR);

	// ========== 直接显示错误的验证函数（直接用于元素）==========
	// 说明：以下函数在验证失败时直接调用 ElMessage.error 弹提示，
	//       并返回 boolean，适用于非表单场景（如按钮点击前手动校验）。

	/** 长度验证（超过 maxLength 时弹 ElMessage 并返回 false） */
	const validateLength = (value: any, maxLength: number, fieldName = '字段') => {
		if (!value) return true;
		if (String(value).length > maxLength) {
			ElMessage.error(`${fieldName}长度不能超过${maxLength}个字符`);
			return false;
		}
		return true;
	};

	/** 必填验证（空值弹 ElMessage 并返回 false） */
	const validateRequired = (value: any, fieldName = '字段') => {
		if (!value || String(value).trim() === '') {
			ElMessage.error(`${fieldName}不能为空`);
			return false;
		}
		return true;
	};

	/** 纯数字验证 */
	const validateNumber = (value: any, fieldName = '字段') => {
		if (!value) return true;
		if (!/^\d+$/.test(String(value))) {
			ElMessage.error(`${fieldName}必须为纯数字`);
			return false;
		}
		return true;
	};

	/** 整数范围验证 */
	const validateIntRange = (value: any, min: number, max: number, fieldName = '字段') => {
		if (!value) return true;
		const num = Number(value);
		if (!Number.isInteger(num)) {
			ElMessage.error(`${fieldName}必须为整数`);
			return false;
		}
		if (num < min || num > max) {
			ElMessage.error(`${fieldName}必须在${min}~${max}之间`);
			return false;
		}
		return true;
	};

	/** 中文验证 */
	const validateChinese = (value: any, fieldName = '字段') => {
		if (!value) return true;
		if (!/^[\u4e00-\u9fa5]+$/.test(String(value))) {
			ElMessage.error(`${fieldName}必须为中文`);
			return false;
		}
		return true;
	};

	/** 邮箱验证 */
	const validateEmail = (value: any, fieldName = '字段') => {
		if (!value) return true;
		if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value))) {
			ElMessage.error(`${fieldName}格式不正确`);
			return false;
		}
		return true;
	};

	/** 手机号验证 */
	const validatePhone = (value: any, fieldName = '字段') => {
		if (!value) return true;
		if (!/^1[3-9]\d{9}$/.test(String(value))) {
			ElMessage.error(`${fieldName}格式不正确`);
			return false;
		}
		return true;
	};

	/** 身份证验证（直接弹 ElMessage 版，与上方 idCard 逻辑一致） */
	const validateIdCard = (value: any, fieldName = '字段') => {
		if (!value) return true;
		const idCard = String(value).trim();

		if (idCard.length === 15) {
			const reg15 = /^[1-9]\d{5}\d{2}((0[1-9])|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}$/;
			if (!reg15.test(idCard)) {
				ElMessage.error(`${fieldName}格式不正确`);
				return false;
			}
			return true;
		}

		if (idCard.length === 18) {
			const reg18 = /^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}(\d|X|x)$/;
			if (!reg18.test(idCard)) {
				ElMessage.error(`${fieldName}格式不正确`);
				return false;
			}

			// 验证校验码
			const factor = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
			const parity = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];
			let sum = 0;

			for (let i = 0; i < 17; i++) {
				sum += parseInt(idCard[i]) * factor[i];
			}

			const last = parity[sum % 11];
			if (last !== idCard[17].toUpperCase()) {
				ElMessage.error(`${fieldName}校验码错误`);
				return false;
			}
			return true;
		}

		ElMessage.error(`${fieldName}必须是15位或18位`);
		return false;
	};

	/** 自定义正则验证 */
	const validatePattern = (value: any, pattern: RegExp, message: string) => {
		if (!value) return true;
		if (!pattern.test(String(value))) {
			ElMessage.error(message);
			return false;
		}
		return true;
	};

	/** 组合验证：所有 validator 都返回 true 才算通过 */
	const validateMultiple = (value: any, validators: Array<(value: any) => boolean>) => {
		for (const validator of validators) {
			if (!validator(value)) {
				return false;
			}
		}
		return true;
	};

	/** 小数精度验证（直接弹 ElMessage 版） */
	const validateDecimalPrecision = (value: any, maxIntDigits: number, maxFracDigits: number, fieldName = '字段') => {
		if (!value) return true;

		const str = String(value).trim();

		// 允许 0、整数或小数（正数），不允许负数与多余小数点
		if (!/^\d+(?:\.\d+)?$/.test(str)) {
			ElMessage.error(`${fieldName}必须为数字`);
			return false;
		}

		const [intPart, fracPart = ''] = str.split('.');

		// 验证整数位数
		if (intPart.length > maxIntDigits) {
			ElMessage.error(`${fieldName}整数位不能超过${maxIntDigits}位`);
			return false;
		}

		// 验证小数位数
		if (fracPart && fracPart.length > maxFracDigits) {
			if (maxFracDigits === 0) {
				ElMessage.error(`${fieldName}必须为整数`);
				return false;
			}
			ElMessage.error(`${fieldName}小数位不能超过${maxFracDigits}位`);
			return false;
		}

		return true;
	};

	/** 通用验证：根据 type 自动分发到对应验证函数 */
	const validateField = (value: any, type: string, fieldName: string, options?: any) => {
		switch (type) {
			case 'required':
				return validateRequired(value, fieldName);
			case 'length':
				return validateLength(value, options.maxLength, fieldName);
			case 'number':
				return validateNumber(value, fieldName);
			case 'range':
				return validateIntRange(value, options.min, options.max, fieldName);
			case 'chinese':
				return validateChinese(value, fieldName);
			case 'email':
				return validateEmail(value, fieldName);
			case 'phone':
				return validatePhone(value, fieldName);
			case 'idcard':
				return validateIdCard(value, fieldName);
			case 'pattern':
				return validatePattern(value, options.pattern, options.message);
			case 'decimal':
				return validateDecimalPrecision(value, options.maxIntDigits, options.maxFracDigits, fieldName);
			default:
				return true;
		}
	};

	// 统一导出：对象形式规则 + 直接弹错误函数
	return {
		required,
		requiredIf,
		validatorIf,
		maxLen,
		minLen,
		pattern,
		validator,
		phone,
		idCard,
		email,
		digits,
		intRange,
		confirmSame,
		dateAfterToday,
		dateBeforeToday,
		numberPrecision,
		validateRequired,
		validateNumber,
		validateIntRange,
		validateChinese,
		validateLength,
		validateEmail,
		validatePhone,
		validateIdCard,
		validatePattern,
		validateMultiple,
		validateField,
		validateDecimalPrecision,
	};
}
