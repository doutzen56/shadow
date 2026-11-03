import { clearAccessTokens, clearTokens, getAPI, setAuthTerminating } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system/api';
import { showForceOfflineNotice } from './forceOfflineNotice';

/** 已在退出流程中：服务端可能重复推送，弹框与退出都只走一次 */
let handling = false;

/**
 * 强制下线统一处理：先断连、登出并清掉本地令牌，再弹提示告知操作者
 * @param notice 服务端下发的 ForceOfflineNotice
 * @param stopConnection 断开 SignalR 的方法，由调用方注入（避免与 signalR.ts 循环引用）
 */
export const handleForceOffline = async (notice: unknown, stopConnection: () => Promise<void>) => {
	if (handling) return;
	handling = true;

	// 退出期间拦截器不再自行刷新页面，否则登出响应与在途请求的失效响应会把提示弹窗顶掉
	setAuthTerminating(true);
	// 登出要在清令牌之前：登出接口靠请求头里的 token 写服务端黑名单，清完再调就无凭证了
	await stopConnection();
	try {
		await getAPI(SysAuthApi).apiSysAuthLogoutPost();
	} catch {
		/* 忽略：服务端会话已失效时接口本就可能报错 */
	}
	// 先清本地令牌再弹框：弹框期间用户直接刷新页面，也不能带着旧会话回到系统
	clearTokens();

	await showForceOfflineNotice(notice);
	setAuthTerminating(false);
	// clearAccessTokens 内部会刷新页面回到登录页
	clearAccessTokens();
};
