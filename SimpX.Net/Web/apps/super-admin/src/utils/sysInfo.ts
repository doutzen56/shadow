import { Local } from '/@/utils/storage';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import logoImg from '/@/assets/logo.png';
import { updateIdleTimeout } from '/@/utils/idleTimeout';

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

/**
 * 加载总后台展示信息。
 * 暂无 Pro SysInfo：恢复本地主题并关掉验证码，公钥用 .env。
 * @param _tenantid 兼容旧调用，总后台不定租
 */
export async function loadSysInfo(_tenantid: number) {
	if (Local.get('themeConfig')) {
		storesThemeConfig.setThemeConfig({ themeConfig: Local.get('themeConfig') });
	}
	if (!themeConfig.value.logoUrl) {
		themeConfig.value.logoUrl = logoImg;
	}
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
