import * as SignalR from '@microsoft/signalr';
import { ElNotification } from 'element-plus';
import { getToken } from '/@/utils/axios-utils';
import mittBus from '/@/utils/mitt';
import { isChatVisible } from './component/chatVisible';
import { handleForceOffline } from './component/forceOffline';

/** 启动失败 / 断线重连的轮询间隔（毫秒） */
const RETRY_INTERVAL = 5 * 1000;

/** ElNotification 合法类型（后端枚举值不可信，超出范围一律兜底 info） */
type NoticeType = 'success' | 'warning' | 'info' | 'error';

// 初始化SignalR对象
const connection = new SignalR.HubConnectionBuilder()
	.configureLogging(SignalR.LogLevel.Warning)
	// token 用 accessTokenFactory 动态获取：断线重连时会重新求值，拿到当前（可能已刷新的）token。
	// 若把 token 焊死在 url 里（?token=xxx），JWT 过期后（默认20分钟）所有重连都会因 401 永远失败。
	// 不要锁死 WebSockets + skipNegotiation：外网 / 公司代理 / CDN 很常见会把 WS 握手掐掉，
	// 一旦被掐，这类客户端永远连不上 Hub —— 于是「在线用户」里只看得见内网那几个人。
	// 放开后先协商，再按 WebSockets → ServerSentEvents → LongPolling 自动降级，弱网也能连上。
	// 开发态走同源 /hubs（Vite 反代到后台），避免直连 :5005 触发跨域且协商失败后狂刷。
	// 生产打包用网关根；空字符串表示当前站点。
	.withUrl(`${import.meta.env.DEV ? '' : window.__env__.VITE_API_URL || ''}/hubs/onlineUser`, {
		transport: SignalR.HttpTransportType.WebSockets | SignalR.HttpTransportType.ServerSentEvents | SignalR.HttpTransportType.LongPolling,
		accessTokenFactory: () => getToken() || '',
	})
	.withAutomaticReconnect({
		nextRetryDelayInMilliseconds: () => {
			return RETRY_INTERVAL; // 每5秒重连一次
		},
	})
	.build();

// 心跳检测：若15s内没有向服务器发送任何消息，则ping一下服务器端
connection.keepAliveIntervalInMilliseconds = 15 * 1000;
// 超时时间：若30s内没有收到服务器端发过来的信息，则认为服务器端异常
connection.serverTimeoutInMilliseconds = 30 * 1000;

/** 是否已主动关闭（登出 / 强制下线 / 闲置超时）：置位后不再被自愈逻辑重新拉起 */
let stoppedByUser = false;
/** 重试定时器：全局只保留一个，避免"重试 + 页面可见自愈"叠加出多条重连队列 */
let retryTimer: ReturnType<typeof setTimeout> | null = null;

const isDisconnected = () => connection.state === SignalR.HubConnectionState.Disconnected;

/** 弹出连接状态提示 */
const notifyState = (message: string, type: NoticeType) => {
	ElNotification({ title: '提示', message, type, position: 'bottom-right', duration: 3000 });
};

/**
 * 排定下一次启动重试（同一时刻只保留一个定时器）
 * 自动重连（withAutomaticReconnect）只覆盖"连上后断开"，覆盖不了"从未连上"，故这里自行兜底重试。
 */
const scheduleRetry = () => {
	if (stoppedByUser || retryTimer != null) return;
	retryTimer = setTimeout(() => {
		retryTimer = null;
		startInternal();
	}, RETRY_INTERVAL);
};

/**
 * 内部启动：带单飞保护，失败后广播事件并排定重试
 * 失败常见原因是 token 过期（挂机页面无 HTTP 请求、token 不会被动刷新），
 * 广播 signalRStartFailed 让订阅方发一次轻量请求，axios 拦截器便会用 refresh token 换新 token，下一次重试即可成功。
 */
/** 连续启动失败次数：连不上时给一次可见提示，否则用户不知道自己根本没上线 */
let failCount = 0;

const startInternal = async () => {
	if (stoppedByUser || !isDisconnected()) return;
	try {
		await connection.start();
		if (failCount >= 3) notifyState('实时连接已恢复', 'success');
		failCount = 0;
	} catch {
		failCount++;
		mittBus.emit('signalRStartFailed', {});
		// 只提示一次（第 3 次失败），既不至于刷屏，也避免"我明明开着网页，别人却看不到我在线"
		if (failCount === 3) notifyState('实时连接失败，在线状态可能不准确，请检查网络或代理是否放通 WebSocket', 'error');
		scheduleRetry();
	}
};

/** 断开连接（对外）：登出 / 强制下线 / 闲置超时调用，停止后续一切自动重连
 *  （重新登录必然伴随页面刷新，模块会重新初始化，故不需要配对的 start 出口） */
const stopSignalR = async () => {
	stoppedByUser = true;
	if (retryTimer != null) {
		clearTimeout(retryTimer);
		retryTimer = null;
	}
	if (!isDisconnected()) await connection.stop();
};

/**
 * 发送消息（对外）：未连接时先启动再发送，避免"还没连上就发"这类必失败的调用
 * @param method 服务端 Hub 方法名
 * @param arg 方法入参
 */
const sendSignalR = async (method: string, arg?: any) => {
	if (isDisconnected()) await startInternal();
	await connection.send(method, arg);
};

// 页面恢复可见时自愈：浏览器冻结后台标签页期间（省电/内存优化）JS 定时器全部暂停，
// SignalR 的心跳与重连重试都会停摆，断线可能永远不被重试。visibilitychange 在解冻时必然派发（不受冻结影响），
// 故以此为契机检查连接状态：已断开则立即重连（配合启动失败重试兜底），冻结期漏推由订阅方补拉
document.addEventListener('visibilitychange', () => {
	if (document.visibilityState !== 'visible') return;
	// Disconnected：start 未成功或 onclose 后的状态；重连彻底失败（超最大次数）也会进入该状态
	if (isDisconnected()) {
		mittBus.emit('signalRStartFailed', {});
		startInternal();
	}
});
// 连接关闭：非主动关闭的异常中断（如服务端重启）兜底重试，防止连接一去不回
connection.onclose(() => {
	if (!stoppedByUser) scheduleRetry();
});
// 重连中：同样广播事件让订阅方发请求刷新 token（挂机页面 token 已过期，不刷新则重连永远 401）
connection.onreconnecting(() => {
	notifyState('连接已断开，正在重连...', 'error');
	mittBus.emit('signalRStartFailed', {});
});
// 重连成功：断线期间服务端推送的消息已丢失，广播事件让订阅方（如角标 store）补拉数据
connection.onreconnected(() => {
	notifyState('连接已恢复', 'success');
	mittBus.emit('signalRReconnected', {});
});

/**
 * 同一发送者连续发消息时的通知节流窗口（毫秒）
 * 对方连发好几条时只弹一次，否则右下角会被同一人的通知刷满
 */
const NOTIFY_GAP = 4 * 1000;
/** 发送者Id → 上次弹通知的时间戳 */
const lastNotifyAt = new Map<number, number>();

// 接收消息
connection.on('ReceiveMessage', (message: any) => {
	// 广播给业务页面（在线聊天面板）：SignalR 同一方法名重复 on 会互相覆盖，
	// 全局通知留在这一处，面板只订阅事件、不重复注册回调
	mittBus.emit('chatMessage', message);

	// 聊天面板正开着：消息已直接落到聊天记录里，不再叠一层通知
	if (isChatVisible()) return;

	const sendUserId = Number(message?.sendUserId ?? 0);
	const now = Date.now();
	if (sendUserId > 0) {
		if (now - (lastNotifyAt.get(sendUserId) ?? 0) < NOTIFY_GAP) return;
		lastNotifyAt.set(sendUserId, now);
	}

	// 隐私：只提示"谁发来消息"，不带消息正文（通知挂在页面右下角，路过的人都能看见）
	const from = String(message?.sendUserName || message?.title || '有人');
	ElNotification({
		title: `${from} 发来消息`,
		message: '点击查看并回复',
		type: 'info',
		position: 'bottom-right',
		duration: 4500,
		// 带上发送者姓名：面板打开后若接口没返回该会话，用它兜底把人选中
		onClick: () => mittBus.emit('openOnlineChat', { userId: sendUserId, userName: from }),
	});
});

// 服务端会向登录用户所在分组广播 OnlineUserList（在线用户列表变更）。
// 业务页面未打开时无人注册该回调，SignalR 客户端会刷
// "No client method with the name 'onlineuserlist' found" 警告 —— 这里挂一个
// 兜底空实现消音；打开在线用户页时页面自会再注册真正处理的回调。
connection.on('OnlineUserList', () => {});

// 强制下线：挂在连接层而不是某个页面 —— 无论当前停在哪一页被踢，都要先提示再退出
connection.on('ForceOffline', (notice: unknown) => handleForceOffline(notice, stopSignalR));

// 首次进入应用即建立连接；用户列表、强制下线等业务监听由各页面自行 on/off 注册
startInternal();

export { connection as signalR, stopSignalR, sendSignalR };
