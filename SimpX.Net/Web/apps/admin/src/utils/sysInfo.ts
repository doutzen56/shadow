import { Local } from '/@/utils/storage';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import logoImg from '/@/assets/logo.png';
import { updateIdleTimeout } from '/@/utils/idleTimeout';

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

/**
 * 加载系统展示信息（Logo、标题等）。
 * SimpX 暂无 Pro 的租户 SysInfo 接口：恢复本地主题，关掉验证码，Logo 用默认图。
 * 登录密码加密用的公钥走 .env 里的 VITE_SM_PUBLIC_KEY。
 * @param _tenantid 租户 Id（兼容旧调用，当前未使用）
 */
export async function loadSysInfo(_tenantid: number) {
	if (Local.get('themeConfig')) {
		storesThemeConfig.setThemeConfig({ themeConfig: Local.get('themeConfig') });
	}
	if (!themeConfig.value.logoUrl) {
		themeConfig.value.logoUrl = logoImg;
	}
	// 本机后台默认关验证码，避免挡住登录冒烟
	themeConfig.value.captcha = false;
	themeConfig.value.secondVer = false;
	Local.set('themeConfig', themeConfig.value);
	Local.set('oldLayout', themeConfig.value.layout);
	updateIdleTimeout(themeConfig.value.idleTimeout ?? 0);
}

/**
 * 更新浏览器页签图标。
 * @param url 图标地址，空则清掉
 */
export const updateFavicon = (url: string): void => {
	const favicon = document.getElementById('favicon') as HTMLAnchorElement | null;
	if (favicon) favicon.href = url ? url : 'data:;base64,=';
};
