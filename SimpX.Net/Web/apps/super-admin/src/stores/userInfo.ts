import { defineStore } from 'pinia';
import { Local, Session } from '/@/utils/storage';
import Watermark from '/@/utils/watermark';
import { useThemeConfig } from '/@/stores/themeConfig';
import { i18n } from '/@/i18n';

import { getAPI } from '/@/utils/axios-utils';
import { SysAuthApi, SysConstApi, SysRoleApi } from '/@/api-services/system/api';

const { t } = i18n.global;

/**
 * 用户信息
 * @methods setUserInfos 设置用户信息
 */
export const useUserInfo = defineStore('userInfo', {
	state: (): UserInfosState => ({
		userInfos: {} as any, // 用户信息
		constList: [] as any, // 常量列表
		dictList: {} as any, // 字典列表
		userTableList: [] as any, // 用户表格字段列表
	}),
	getters: {},
	actions: {
		// 存储用户信息到浏览器缓存
		async setUserInfos() {
			this.userInfos = Session.get('userInfo') ?? <UserInfos>await this.getApiUserInfo();
		},

		// 存储常量信息到浏览器缓存
		async setConstList() {
			this.constList = Session.get('constList') ?? <any[]>await this.getSysConstList();
			if (!Session.get('constList')) Session.set('constList', this.constList);
		},

		/**
		 * 装载字典缓存。
		 * 字典维护在总后台，租户后台本波不拉 AllDictList，先放空表，避免白屏挡住登录和菜单。
		 */
		async setDictList() {
			this.dictList = {};
		},

		// 存储用户表格列到浏览器缓存
		async setUserTables() {
			this.userTableList = Session.get('userTable') ?? <any[]>await this.getUserTableList();
			if (!Session.get('userTable')) Session.set('userTable', this.userTableList);
		},

		// 获取当前用户信息
		// 重构为 async/await：确保接口失败时异常能正确往外抛，避免上层 await 永久挂起（历史写法中 new Promise 没 reject/catch，502 或 result==null 时会导致页面刷新后卡死）
		/**
		 * 拉取当前登录用户。
		 * SimpX 返回 buttons / roleIds；Pro 壳用 authApiList 做按钮权限，这里做字段对齐。
		 */
		async getApiUserInfo(): Promise<any> {
			const res: any = await getAPI(SysAuthApi).apiSysAuthUserInfoGet();
			if (res?.data?.result == null) return null;
			const d = res.data.result;
			const userInfos = {
				id: d.id,
				tenantId: d.tenantId,
				currentTenantId: d.currentTenantId,
				account: d.account,
				realName: d.realName,
				phone: d.phone,
				idCardNum: d.idCardNum,
				email: d.email,
				accountType: d.accountType,
				avatar: d.avatar ?? '/upload/logo.png',
				address: d.address,
				signature: d.signature,
				orgId: d.orgId,
				orgName: d.orgName,
				posName: d.posName,
				posId: d.posId,
				createTime: d.createTime,
				jobNum: d.jobNum,
				points: d.points,
				roles: d.roles ?? d.roleIds ?? [],
				// Pro 权限指令读 authApiList；SimpX 用户信息里是 buttons
				authApiList: d.apis ?? d.buttons ?? [],
				authBtnList: d.buttons ?? d.apis ?? [],
				lastChangePasswordTime: d.lastChangePasswordTime,
				langCode: d.langCode,
				time: new Date().getTime(),
			};
			// vue-next-admin 提交Id：225bce7 提交消息：admin-23.03.26:发布v2.4.32版本
			// 增加了下面代码，引起当前会话的用户信息不会刷新，如：重新提交的头像不更新，需要新开一个页面才能正确显示
			// Session.set('userInfo', userInfos);

			// 用户个性化水印
			const storesThemeConfig = useThemeConfig();
			storesThemeConfig.themeConfig.watermarkText = d.watermarkText ?? '';
			if (storesThemeConfig.themeConfig.isWatermark) Watermark.set(storesThemeConfig.themeConfig.watermarkText);
			else Watermark.del();

			Local.remove('themeConfig');
			Local.set('themeConfig', storesThemeConfig.themeConfig);

			return userInfos;
		},

		// 获取当前用户表格列集合
		async getUserTableList(): Promise<any[]> {
			const res: any = await getAPI(SysRoleApi).apiSysRoleUserRoleTableListGet();
			return res?.data?.result ?? [];
		},

		/** 获取常量集合；接口失败时返回空数组，不挡登录。 */
		async getSysConstList(): Promise<any[]> {
			try {
				const res: any = await getAPI(SysConstApi).apiSysConstListGet();
				return res?.data?.result ?? [];
			} catch {
				return [];
			}
		},

		// 根据常量类名获取常量数据
		getConstDataByTypeCode(typeCode: string) {
			return this.constList.find((item: any) => item.code === typeCode)?.data?.result || [];
		},

		// 根据常量类名和编码获取常量值
		getConstItemNameByType(typeCode: string, itemCode: string) {
			const data = this.getConstDataByTypeCode(typeCode);
			return data.find((item: any) => item.code === itemCode)?.name;
		},

		// 常量编码和名称转换（常量未加载 / 编码不存在时返回空串，不能抛异常）
		codeToName(code: any, type: any) {
			return this.constList.find((x: any) => x.code === type)?.data?.result?.find((x: any) => x.code === code)?.name ?? '';
		},

		// 根据字典类型获取字典数据
		getDictDataByCode(dictTypeCode: string) {
			return this.dictList[dictTypeCode] || [];
		},

		// 根据字典类型和值获取取字典项
		getDictItemByValue(dictTypeCode: string, value: string) {
			if (value != undefined && value !== '') {
				const _value = value.toString();
				const ds = this.dictList[dictTypeCode] || [];
				for (const element of ds) {
					if (element.value == _value) {
						return element;
					}
				}
			}
			return {};
		},

		// 根据字典类型和名称获取取字典项
		getDictItemByLabel(dictTypeCode: string, label: string) {
			if (label != undefined && label !== '') {
				const _label = label.toString();
				const ds = this.dictList[dictTypeCode] || [];
				for (const element of ds) {
					if (element.label == _label) {
						return element;
					}
				}
			}
			return {};
		},
	},
});

// 处理字典国际化, 默认显示字典中的label值
const setDictLangMessageAsync = async (dict: any) => {
	dict.langMessage = `message.dictType.${dict.typeCode}_${dict.value}`;
	const text = t(dict.langMessage);
	dict.label = text !== dict.langMessage && !text.endsWith(`${dict.typeCode}_${dict.value}`) ? text : dict.label;
};
