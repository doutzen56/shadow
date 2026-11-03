/**
 * 示例配置模板
 *
 * 把已经生成过、编译过、能直接跑的四套配置固化下来。
 * 新手不用从零选库选表猜字段，点一下示例就能拿到一份可直接生成的配置；
 * 表名可以换，树配置与主从联表字段会自动重新推导。
 */

export interface CodeGenSample {
	/** 唯一标识 */
	key: string;
	/** 场景：1000 单表 / 1010 单表树 / 2000 主从表 / 2010 主从表树 */
	scene: number;
	/** 示例名 */
	name: string;
	/** 一句话说明 */
	desc: string;
	/** 能力标签 */
	tags: string[];
	/** 默认业务名 */
	busName: string;
	/** 建议主表实体名 */
	main: string;
	/** 建议从表实体名（主从场景） */
	slave?: string;
	/** 树标题（树场景） */
	treeTitle?: string;
}

export const codeGenSamples: CodeGenSample[] = [
	{
		key: 'single',
		scene: 1000,
		name: '单表 CRUD',
		desc: '一张表生成列表页 + 编辑弹窗，绝大多数业务都是这种',
		tags: ['列表页', '编辑弹窗'],
		busName: '职位管理',
		main: 'SysPos',
	},
	{
		key: 'tree',
		scene: 1010,
		name: '单表 + 左侧树',
		desc: '左侧树按父子层级筛选，右侧列表跟着联动',
		tags: ['左树', '右表联动'],
		busName: '组织机构',
		main: 'SysOrg',
		treeTitle: '组织机构',
	},
	{
		key: 'masterSlave',
		scene: 2000,
		name: '主从表',
		desc: '点主表某一行，下方从表自动过滤出它的明细',
		tags: ['主从联动', '双弹窗'],
		busName: '字典管理',
		main: 'SysDictType',
		slave: 'SysDictData',
	},
	{
		key: 'masterSlaveTree',
		scene: 2010,
		name: '主从表 + 树',
		desc: '树 + 主表 + 从表三级联动，适合机构、台账类业务',
		tags: ['三级联动', '台账'],
		busName: '机构人员',
		main: 'SysOrg',
		slave: 'SysUser',
		treeTitle: '组织机构',
	},
];

/** 是否是树场景 */
export const isTreeScene = (scene: number) => Number(scene) % 100 === 10;

/** 是否是主从场景 */
export const isSlaveScene = (scene: number) => [2000, 2010].includes(Number(scene));

/**
 * 在后端默认字段配置之上再套一层"更像人配的"默认值。
 *
 * - 长文本字段（备注、简介等）不进列表，否则表格会被撑爆
 * - 外键 Id 只用于关联，列表里显示一串数字没有意义
 * - 名称类字段默认开启查询，这是最常用的检索维度
 *
 * @param columns 后端返回的默认字段配置
 * @param linkField 需要保留在增改里、但不进列表的联表字段（如 DictTypeId）
 */
export const applySampleColumnDefaults = (columns: any[], linkField?: string) => {
	const longText = /(Remark|Description|Content|Introduction|ExtData|Signature|StyleSetting|ClassSetting)$/i;
	const nameLike = /(Name|Title|Label|Code|Account|RealName)$/i;
	const foreignKey = /^(?!Id$)\w+Id$/;

	(columns ?? []).forEach((c: any) => {
		const prop = c.propertyName ?? '';
		if (c.isCommon || c.isPrimarykey) return;

		// 联表字段：增改保留（新增时要带主表 Id），列表隐藏
		if (linkField && prop === linkField) {
			c.isTable = false;
			c.isQuery = false;
			return;
		}

		// 长文本：只进增改与导入导出，不进列表、不做查询条件
		if (longText.test(prop) || (c.netType === 'string' && Number(c.columnLength) > 500)) {
			c.isTable = false;
			c.isQuery = false;
			c.isSortable = false;
			return;
		}

		// 外键 Id：列表里显示没有意义
		if (foreignKey.test(prop)) {
			c.isTable = false;
			c.isQuery = false;
			return;
		}

		// 名称类字段默认开启查询
		if (nameLike.test(prop)) {
			c.isQuery = true;
			c.isTable = true;
		}
	});
};

/**
 * 从字段里挑出树配置需要的三个字段：父级 / 显示 / 检索。
 *
 * @param columns 字段列表
 * @returns 命中的属性名，取不到时返回 undefined 由调用方提示用户手选
 */
export const guessTreeFields = (columns: any[]) => {
	const props = (columns ?? []).filter((c: any) => !c.isCommon);
	const pick = (names: string[]) => props.find((c: any) => names.includes(c.propertyName))?.propertyName;
	const pickLike = (re: RegExp) => props.find((c: any) => re.test(c.propertyName))?.propertyName;

	return {
		parent: pick(['Pid', 'ParentId', 'ParentPid']) ?? pickLike(/^(Pid|ParentId)$/i),
		display: pick(['Name', 'Title', 'Label']) ?? pickLike(/Name$/i),
		search: pick(['Name', 'Title', 'Label']) ?? pickLike(/Name$/i),
	};
};

/**
 * 从表里找出指向主表的外键字段。
 *
 * @param slaveColumns 从表字段
 * @param mainEntityName 主表实体名，如 SysOrg -> OrgId
 */
export const guessSlaveLinkField = (slaveColumns: any[], mainEntityName?: string) => {
	const props = (slaveColumns ?? []).filter((c: any) => !c.isCommon);
	if (mainEntityName) {
		const name = mainEntityName.replace(/^Sys/, '') + 'Id';
		const hit = props.find((c: any) => c.propertyName === name);
		if (hit) return hit.propertyName;
	}
	return props.find((c: any) => /^(?!Id$)\w+Id$/.test(c.propertyName))?.propertyName;
};
