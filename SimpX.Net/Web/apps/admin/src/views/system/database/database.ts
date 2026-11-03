export const yesNoSelect = [
	{
		label: '是',
		value: 1,
	},
	{
		label: '否',
		value: 0,
	},
];

/**
 * 字段类型清单
 * value 为数据库类型名；label 为中文说明；
 * hasLength / hasDecimalDigits 决定「长度」「小数位」两个输入框是否可用。
 */
export const dataTypeList = [
	{ value: 'bigint', label: '长整数', hasLength: false, hasDecimalDigits: false },
	{ value: 'int', label: '整数', hasLength: false, hasDecimalDigits: false },
	{ value: 'smallint', label: '短整数', hasLength: false, hasDecimalDigits: false },
	{ value: 'tinyint', label: '微整数', hasLength: false, hasDecimalDigits: false },
	{ value: 'bit', label: '布尔', hasLength: false, hasDecimalDigits: false },
	{ value: 'boolean', label: '布尔', hasLength: false, hasDecimalDigits: false },
	{ value: 'decimal', label: '高精度小数', hasLength: true, hasDecimalDigits: true },
	{ value: 'varchar', label: '变长字符串', hasLength: true, hasDecimalDigits: false },
	{ value: 'nvarchar', label: '变长Unicode字符串', hasLength: true, hasDecimalDigits: false },
	{ value: 'char', label: '定长字符串', hasLength: true, hasDecimalDigits: false },
	{ value: 'nchar', label: '定长Unicode字符串', hasLength: true, hasDecimalDigits: false },
	{ value: 'text', label: '长文本', hasLength: false, hasDecimalDigits: false },
	{ value: 'clob', label: '大文本', hasLength: false, hasDecimalDigits: false },
	{ value: 'blob', label: '二进制', hasLength: false, hasDecimalDigits: false },
	{ value: 'datetime', label: '日期时间', hasLength: false, hasDecimalDigits: false },
	{ value: 'date', label: '日期', hasLength: false, hasDecimalDigits: false },
	{ value: 'time', label: '时间', hasLength: false, hasDecimalDigits: false },
	{ value: 'timestamp', label: '时间戳', hasLength: false, hasDecimalDigits: false },
];

/**
 * 取某个字段类型的元数据（长度 / 小数位是否需要填）
 * 未登记的类型按「都不需要」处理，避免把输入框锁死。
 */
export const typeMetaOf = (dataType?: string | null): { hasLength: boolean; hasDecimalDigits: boolean } => {
	const hit = dataTypeList.find((u) => u.value === (dataType ?? '').toLowerCase());
	return { hasLength: hit?.hasLength ?? false, hasDecimalDigits: hit?.hasDecimalDigits ?? false };
};

/** 基础字段模板项 */
export interface BaseColumnPreset {
	dataType: string;
	name: string;
	desc: string;
	length?: number;
	isNullable?: number;
}

/**
 * 常用字段模板：新增表时可以一键带出一整套基础字段
 * 与 SimpX 实体基类（EntityBase / EntityBaseId 等）保持一致。
 */
export const baseColumnPresets: BaseColumnPreset[] = [
	{ dataType: 'varchar', name: 'Code', desc: '编码', length: 64 },
	{ dataType: 'varchar', name: 'Name', desc: '名称', length: 64 },
	{ dataType: 'varchar', name: 'Description', desc: '描述', length: 500 },
	{ dataType: 'datetime', name: 'CreateTime', desc: '创建时间' },
	{ dataType: 'bigint', name: 'CreateUserId', desc: '创建人Id' },
	{ dataType: 'varchar', name: 'CreateUserName', desc: '创建人', length: 64 },
	{ dataType: 'datetime', name: 'UpdateTime', desc: '修改时间' },
	{ dataType: 'bigint', name: 'UpdateUserId', desc: '修改人Id' },
	{ dataType: 'varchar', name: 'UpdateUserName', desc: '修改人', length: 64 },
	{ dataType: 'bigint', name: 'CreateOrgId', desc: '创建者机构Id' },
	{ dataType: 'varchar', name: 'CreateOrgName', desc: '创建者机构名称', length: 64 },
	{ dataType: 'bit', name: 'IsDelete', desc: '软删除', isNullable: 0 },
	{ dataType: 'datetime', name: 'DeleteTime', desc: '软删除时间' },
];

/** 文件大小格式化：小于 1MB 用 KB，避免备份列表里一堆 0.00 MB */
export const formatFileSize = (size?: number | null): string => {
	if (size == null || isNaN(size as number)) return '—';
	const bytes = size as number;
	if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
	if (bytes >= 1024) return `${(bytes / 1024).toFixed(2)} KB`;
	return `${bytes} B`;
};
