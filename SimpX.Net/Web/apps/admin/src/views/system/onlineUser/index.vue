<template>
	<div class="sys-onlineUser-container">
		<!-- append-to-body：顶栏处于 z-index:2 的层叠上下文内，不传送会导致遮罩盖不住页面 z-index 较高的表格边框 -->
		<!-- class：传送后 scoped 的 :deep 选择器（依赖 data-v 祖先）会失效，抽屉自身样式改用独立类名写在非 scoped 块里 -->
		<el-drawer
			v-model="state.isVisible"
			:size="drawerSize"
			class="online-user-drawer"
			append-to-body
			@open="setChatVisible(true)"
			@closed="setChatVisible(false)"
		>
			<template #header>
				<div class="online-user-drawer__head">
					<div class="online-user-drawer__title">
						<el-icon size="16"><ele-UserFilled /></el-icon>
						<span>在线用户</span>
						<!-- 人数与连接数是两个口径：前者按人去重（多端登录只算 1 人），后者是列表行数 -->
						<el-tooltip :content="onlineTip" placement="bottom">
							<el-tag size="small" effect="dark" type="success">在线 {{ onlineUserCount }} 人</el-tag>
						</el-tooltip>
						<el-tooltip :content="connectionTip" placement="bottom">
							<el-tag size="small" effect="dark" class="online-user-drawer__conn">连接 {{ onlineConnectionCount }} 个</el-tag>
						</el-tooltip>
					</div>
					<div class="online-user-drawer__acts">
						<!-- 小屏抽屉本来就铺满，再给全屏键没意义 -->
						<el-tooltip v-if="!isMobile" :content="state.fullscreen ? '退出全屏' : '全屏'" placement="bottom">
							<el-button text @click="state.fullscreen = !state.fullscreen">
								<SvgIcon :name="state.fullscreen ? 'iconfont icon-tuichuquanping' : 'iconfont icon-fullscreen'" />
							</el-button>
						</el-tooltip>
					</div>
				</div>
			</template>

			<!-- 仿聊天软件布局：宽屏左列表 + 右聊天；小屏只显示其中一栏，点人进会话、点返回回列表 -->
			<div ref="chatRef" class="online-chat" :class="{ 'is-mobile': isMobile }">
				<ChatUserList
					v-if="!isMobile || pane === 'list'"
					:style="listStyle"
					:users="mergedUsers"
					:active-key="state.currentKey"
					:loading="state.loading"
					:my-id="myId"
					@select="selectUser"
					@force-offline="forceOffline"
					@force-offline-all="forceOfflineAll"
					@refresh="refresh"
				@delete-conversation="deleteConversation"
				/>
				<!-- 可拖动分隔条：左右拖动调列表宽度，双击复位（小屏是单栏，不需要） -->
				<div
					v-if="!isMobile"
					class="online-chat__resizer"
					:class="{ 'is-active': resizing }"
					title="拖动调整宽度，双击复位"
					@mousedown.prevent="startResize"
					@touchstart.prevent="startResize"
					@dblclick="resetWidth"
				/>
				<ChatPanel
					v-if="!isMobile || pane === 'chat'"
					:user="currentUser"
					:messages="state.messages"
					:sending="state.sending"
					:load-failed="state.loadFailed"
					:my-id="myId"
					:my-name="myRealName"
					:my-avatar="myAvatar"
					:mobile="isMobile"
					@send="sendChat"
					@open-notice="openSendMessage"
					@retry="retryLoad"
					@back="backToList"
				@clear-conversation="clearConversation"
				/>
			</div>
		</el-drawer>

		<SendMessage ref="sendMessageRef" title="发送站内信" />
	</div>
</template>

<!-- 在线用户 -->
<script lang="ts" setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useUserInfo } from '/@/stores/userInfo';
import mittBus from '/@/utils/mitt';
import { Local } from '/@/utils/storage';
import { signalR, sendSignalR } from './signalR';
import { setChatVisible } from './component/chatVisible';

import ChatUserList from './component/chatUserList.vue';
import ChatPanel from './component/chatPanel.vue';
import SendMessage from './component/sendMessage.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysOnlineUserApi, SysChatApi } from '/@/api-services/system/api';
import type { ChatSessionOutput, OnlineUser } from '/@/api-services/system/models';
import type { ChatMessageItem, ChatUserItem } from './component/chatTypes';
import { parseChatDate } from './component/chatTime';
import { toMessageSummary } from './component/chatContent';
import { displayName, rowKey } from './component/chatUser';

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const storesUserInfo = useUserInfo();
const { userInfos } = storeToRefs(storesUserInfo);
const sendMessageRef = ref<InstanceType<typeof SendMessage>>();

/** 会话已读时间戳缓存：键为对方用户Id，值为最后阅读时间 */
const LAST_READ_KEY = 'onlineChatLastRead';
/** 上次打开的会话：刷新后回到原会话，不必再点一次才看到聊天记录 */
const LAST_CHAT_KEY = 'onlineChatLastUserId';
/** 左侧列表宽度：拖动分隔条调整后记在本机 */
const LIST_WIDTH_KEY = 'onlineChatListWidth';

/** 列表宽度边界：太窄放不下「姓名 + 多端标签 + 时间」，太宽会把聊天区挤没 */
const DEFAULT_LIST_WIDTH = 320;
const MIN_LIST_WIDTH = 260;
const MAX_LIST_WIDTH = 520;
/** 给右侧聊天区留的最小宽度：算上限时用，避免聊天区被拖成一条缝 */
const MIN_CHAT_WIDTH = 360;

const state = reactive({
	isVisible: false,
	loading: false,
	sending: false,
	loadFailed: false,
	fullscreen: false,
	/** 当前会话的对方用户Id（聊天记录是"人对人"的，多端登录的几条连接共用同一份会话） */
	currentId: 0,
	/** 当前选中行的行键（= 连接Id）：同一账号多端登录有多行，靠它才能只高亮其中一条 */
	currentKey: '',
	onlineUsers: [] as OnlineUser[],
	sessions: [] as ChatSessionOutput[],
	messages: [] as ChatMessageItem[],
});

const lastRead = ref<Record<string, string>>(Local.get(LAST_READ_KEY) ?? {});
const viewportWidth = ref(window.innerWidth);

/** 左右分栏容器：算宽度上限要用它的实际宽度 */
const chatRef = ref<HTMLElement | null>(null);
const listWidth = ref(Number(Local.get(LIST_WIDTH_KEY) ?? DEFAULT_LIST_WIDTH) || DEFAULT_LIST_WIDTH);
const resizing = ref(false);

/** 列表宽度内联样式（覆盖组件内 .chat-user-list 的固定宽度） */
const listStyle = computed(() => ({ width: `${listWidth.value}px` }));

/** 宽度上限：既不超过 MAX，也不能吃掉聊天区的最小宽度 */
const maxListWidth = () => {
	const total = chatRef.value?.clientWidth ?? 0;
	return total ? Math.max(MIN_LIST_WIDTH, Math.min(MAX_LIST_WIDTH, total - MIN_CHAT_WIDTH)) : MAX_LIST_WIDTH;
};

/** 夹到合法区间 */
const clampWidth = (w: number) => Math.min(Math.max(Math.round(w), MIN_LIST_WIDTH), maxListWidth());

/** 取事件里的横坐标：鼠标与触屏走不同字段 */
const clientXOf = (e: MouseEvent | TouchEvent) => (e instanceof MouseEvent ? e.clientX : (e.touches[0]?.clientX ?? 0));

/** 开始拖动分隔条 */
const startResize = (e: MouseEvent | TouchEvent) => {
	if (isMobile.value) return;

	const startX = clientXOf(e);
	const startWidth = listWidth.value;
	resizing.value = true;
	document.body.classList.add('is-col-resizing');

	const move = (ev: MouseEvent | TouchEvent) => {
		// 触屏不 preventDefault 会变成页面滚动，拖不动
		if (!(ev instanceof MouseEvent)) ev.preventDefault();
		listWidth.value = clampWidth(startWidth + (clientXOf(ev) - startX));
	};
	const end = () => {
		window.removeEventListener('mousemove', move);
		window.removeEventListener('touchmove', move);
		window.removeEventListener('mouseup', end);
		window.removeEventListener('touchend', end);
		resizing.value = false;
		document.body.classList.remove('is-col-resizing');
		Local.set(LIST_WIDTH_KEY, listWidth.value);
	};

	window.addEventListener('mousemove', move);
	window.addEventListener('touchmove', move, { passive: false });
	window.addEventListener('mouseup', end);
	window.addEventListener('touchend', end);
};

/** 双击分隔条复位 */
const resetWidth = () => {
	listWidth.value = clampWidth(DEFAULT_LIST_WIDTH);
	Local.set(LIST_WIDTH_KEY, listWidth.value);
};

/** 当前登录用户 */
const myId = computed(() => Number(userInfos.value?.id ?? 0));
const myRealName = computed(() => userInfos.value?.realName || userInfos.value?.account || String(myId.value));
const myAvatar = computed(() => userInfos.value?.avatar ?? '');

/** 取时间戳用于排序：解析不了的时间统一按 0 处理，排在最末 */
const timeValue = (v?: string | Date | null) => parseChatDate(v)?.getTime() ?? 0;

/** 当前时间格式化为后端同款字符串，便于本地消息与历史消息一起排序展示 */
const nowText = () => {
	const d = new Date();
	const p = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
};

/** 未读：最后一条是对方发的，且晚于本地记录的已读时间 */
const isUnread = (u: ChatUserItem) => {
	if (!u.lastTime || u.lastSendUserId === myId.value) return false;
	const read = lastRead.value[String(u.userId)];
	if (!read) return true;
	return timeValue(u.lastTime) > timeValue(read);
};

/**
 * 左侧列表 = 在线连接 + 最近会话（会话里可能包含已离线的联系人）
 *
 * 在线部分**按连接分行、不做 userId 去重**：同一个账号开多个标签页 / 多端登录就是多行，
 * 这样既能一眼看出"这个号开了几端、分别在哪"，也能单独踢掉其中某一条连接。
 */
const mergedUsers = computed<ChatUserItem[]>(() => {
	const list = (state.onlineUsers ?? []).filter((u) => Number(u.userId ?? 0) > 0);

	// 先统计每个账号有几条连接，用于显示 "2/3 端" 与排序时的分组
	const countOf = new Map<number, number>();
	list.forEach((u) => {
		const uid = Number(u.userId ?? 0);
		countOf.set(uid, (countOf.get(uid) ?? 0) + 1);
	});

	const rows: ChatUserItem[] = [];
	const indexOf = new Map<number, number>();

	// 自己也在列表里：能看到自己在线、IP 是多少、开了几端
	list.forEach((u) => {
		const uid = Number(u.userId ?? 0);
		const index = (indexOf.get(uid) ?? 0) + 1;
		indexOf.set(uid, index);

		rows.push({
			userId: uid,
			userName: u.userName,
			realName: u.realName,
			online: true,
			ip: u.ip,
			browser: u.browser,
			connectionId: u.connectionId,
			deviceCount: countOf.get(uid) ?? 1,
			deviceIndex: index,
			isMe: uid === myId.value,
		});
	});

	(state.sessions ?? []).forEach((s) => {
		if (!s.userId || s.userId === myId.value) return;

		const rowsOfUser = rows.filter((r) => r.userId === s.userId);
		if (rowsOfUser.length) {
			// 已在线：会话摘要只挂到第一条上，其余行保留各自的 IP / 浏览器，方便分辨是哪一端
			const first = rowsOfUser[0];
			first.avatar = s.avatar ?? first.avatar;
			first.userName = s.userName ?? first.userName;
			first.realName = s.realName ?? first.realName;
			first.lastMessage = s.lastMessage;
			first.lastTime = s.lastTime ? String(s.lastTime) : null;
			first.lastSendUserId = s.lastSendUserId;
			return;
		}

		rows.push({
			userId: s.userId,
			userName: s.userName,
			realName: s.realName,
			avatar: s.avatar,
			online: s.online ?? false,
			ip: s.ip,
			browser: s.browser,
			lastMessage: s.lastMessage,
			lastTime: s.lastTime ? String(s.lastTime) : null,
			lastSendUserId: s.lastSendUserId,
		});
	});

	// 排序用的"最近消息时间"按账号取：否则同一账号只有第一行带 lastTime，
	// 排序时会被别人的消息插进来，同一个账号的几端就散在列表各处了
	const sortTime = new Map<number, number>();
	rows.forEach((r) => sortTime.set(r.userId, Math.max(sortTime.get(r.userId) ?? 0, timeValue(r.lastTime))));

	return rows
		.map((u) => ({ ...u, unread: isUnread(u) }))
		// 排序：自己置顶 → 在线优先 → 最近有消息的靠前 → 同一账号的连接挨在一起并按端序排
		.sort(
			(a, b) =>
				Number(!!b.isMe) - Number(!!a.isMe) ||
				Number(b.online) - Number(a.online) ||
				sortTime.get(b.userId)! - sortTime.get(a.userId)! ||
				a.userId - b.userId ||
				(a.deviceIndex ?? 0) - (b.deviceIndex ?? 0)
		);
});

/**
 * 当前会话对象
 * 同一账号的多行共用一份聊天记录，所以先按行键找（选中哪条连接就展示哪条的信息），
 * 那条连接掉了再退回同账号的其它行，避免面板头部突然变空。
 */
const currentUser = computed<ChatUserItem | null>(
	() => mergedUsers.value.find((u) => rowKey(u) === state.currentKey) ?? mergedUsers.value.find((u) => u.userId === state.currentId) ?? null
);

/** 在线人数：按「人」统计（含自己，同一账号多端登录只算 1 人） */
const onlineUserCount = computed(() => new Set(mergedUsers.value.filter((u) => u.online).map((u) => u.userId)).size);

/** 在线连接数：按「连接」统计，也就是左侧列表的行数（多端登录会大于人数） */
const onlineConnectionCount = computed(() => (state.onlineUsers ?? []).length);

/** 人数口径的说明：多端登录只算 1 人，所以这里的人数可能小于连接数 */
const onlineTip = computed(() =>
	onlineConnectionCount.value > onlineUserCount.value
		? `${onlineUserCount.value} 位用户在线（含自己），同一账号多端登录只算 1 人`
		: `${onlineUserCount.value} 位用户在线（含自己）`
);

/** 连接数口径的说明：列表按连接分行，多端登录各占一行 */
const connectionTip = computed(() =>
	onlineConnectionCount.value > onlineUserCount.value
		? `共 ${onlineConnectionCount.value} 个连接：同一账号多开标签页 / 多端登录会各占一行，可逐条断开`
		: `共 ${onlineConnectionCount.value} 个连接，一人一个`
);

/**
 * 小屏判定：窄到放不下「320px 列表 + 聊天区」时切成单栏（列表 / 会话二选一），
 * 否则聊天区被挤成一条缝，底部的发送按钮会被顶出可视区（手机上就发不出消息了）。
 */
const MOBILE_WIDTH = 768;
const isMobile = computed(() => viewportWidth.value < MOBILE_WIDTH);

/** 小屏当前显示哪一栏：list=用户列表，chat=会话 */
const pane = ref<'list' | 'chat'>('list');

/** 小屏从会话返回列表 */
const backToList = () => (pane.value = 'list');

/**
 * 抽屉宽度：列表加宽到 320px 后，60% 的抽屉在 1280 屏上只剩 ~450 给聊天区，偏挤 →
 * 大屏提到 68%；1100 以下直接铺满（再按比例切，聊天区会被压成一条缝）
 */
const drawerSize = computed(() => (viewportWidth.value < 1100 || state.fullscreen ? '100%' : '68%'));

// 页面初始化
onMounted(() => {
	// 监听用户在线列表
	signalR.on('OnlineUserList', async (data: any) => {
		state.onlineUsers = data.userList ?? [];

		// 上下线通知
		if (!themeConfig.value.onlineNotice) return;

		const uid = Number(data?.userId ?? 0);
		const connId = String(data?.connectionId ?? '');

		// 静默判定**必须看连接Id，不能只看用户Id**：
		// 广播是按租户分组发的，当事人自己也在组里；若按 UserId 判"这是我"，
		// 别人拿着我的账号在别处登录时（UserId 同样是"我"）就会被误判成我自己、静默掉 ——
		// 只有"事件连接Id == 我自己的连接Id"才是我这次上下线，才不提示。
		// 老后端没下发 ConnectionId 时退回按 UserId 判断（宁可少弹，也不打扰）。
		const silent = connId ? connId === (signalR.connectionId ?? '') : myId.value > 0 && uid === myId.value;
		if (silent) return;

		// 同一个账号的其它连接：别人登了我的号 / 我又开了一个端 —— 这是安全相关事件，单独用警示样式提示
		if (myId.value > 0 && uid === myId.value) {
			ElNotification({
				title: '账号提醒',
				message: `你的账号在另一处${data.online ? '上线' : '断开'}（${data.ip || '未知 IP'}）`,
				type: 'warning',
				position: 'bottom-right',
				duration: 6000,
				progress: true,
			});
			return;
		}

		ElNotification({
			title: '提示',
			message: `${data.realName} | ${data.ip} - ${data.online ? '上线了' : '离开了'}`,
			type: `${data.online ? 'primary' : 'error'}`,
			position: 'bottom-right',
			progress: true,
		});
	});

	// 收到聊天消息：当前会话直接追加气泡，其他会话更新摘要（列表出现未读红点）
	mittBus.on('chatMessage', onChatMessage);
	// 面板关着时收到消息会弹右下角提醒，点击提醒即打开面板并定位到发消息的人
	mittBus.on('openOnlineChat', onOpenFromNotify);
	window.addEventListener('resize', onResize);
});

onUnmounted(() => {
	mittBus.off('chatMessage', onChatMessage);
	mittBus.off('openOnlineChat', onOpenFromNotify);
	window.removeEventListener('resize', onResize);
	// 组件卸载后抽屉不复存在，复位可见标志，避免全局通知被永久抑制
	setChatVisible(false);
});

const onResize = () => {
	viewportWidth.value = window.innerWidth;
	// 窗口变窄后，上次拖宽了的列表可能已经吃掉了聊天区，这里重新夹一次
	listWidth.value = clampWidth(listWidth.value);
	// 缩到小屏但没选中任何人时，会话栏只会是一片空白，不如直接落在列表上
	if (isMobile.value && pane.value === 'chat' && !state.currentId) pane.value = 'list';
};

/** 收到聊天消息（由 signalR.ts 广播，避免重复注册 ReceiveMessage 回调互相覆盖） */
const onChatMessage = (msg: any) => {
	const sendUserId = Number(msg?.sendUserId ?? 0);
	if (!sendUserId) return;

	if (sendUserId === state.currentId) {
		state.messages.push({
			id: msg?.id,
			sendUserId,
			sendUserName: msg?.sendUserName,
			receiveUserId: msg?.receiveUserId,
			message: msg?.message,
			sendTime: msg?.sendTime,
		});
		// 面板正开着且正好看着这个会话，视为已读
		if (state.isVisible) markRead(sendUserId);
	}

	// 摘要统一由 upsertSession 归口（图片/文件消息在列表里只显示 "[图片]" "[文件]"）
	upsertSession({
		userId: sendUserId,
		realName: msg?.sendUserName,
		lastMessage: msg?.message,
		lastTime: msg?.sendTime,
		lastSendUserId: sendUserId,
	});
};

/**
 * 会话摘要补丁
 * 生成层把 LastTime 声明成 Date，但 axios 不做日期反序列化、运行时其实是字符串，故放宽类型（见 parseChatDate）。
 */
type SessionPatch = { userId: number } & Partial<Record<keyof ChatSessionOutput, any>>;

/** 更新/插入会话摘要，并把它顶到列表最前（图片消息在列表里只显示 "[图片]"） */
const upsertSession = (patch: SessionPatch) => {
	const idx = state.sessions.findIndex((s) => s.userId === patch.userId);
	const old = idx >= 0 ? state.sessions[idx] : undefined;
	const merged = {
		...old,
		...patch,
		userId: patch.userId,
		online: patch.online ?? old?.online ?? false,
		lastMessage: toMessageSummary(patch.lastMessage ?? old?.lastMessage),
	} as ChatSessionOutput;

	if (idx >= 0) state.sessions.splice(idx, 1);
	state.sessions.unshift(merged);
};

/** 标记会话已读（本机维度，记录在 localStorage；时间与后端同格式，便于一并解析比较） */
const markRead = (userId: number) => {
	lastRead.value = { ...lastRead.value, [String(userId)]: nowText() };
	Local.set(LAST_READ_KEY, lastRead.value);
};

/** 点击右下角消息提醒：打开面板并直接选中发消息的人、拉出聊天记录 */
const onOpenFromNotify = (payload: any) => openDrawer(Number(payload?.userId ?? 0), payload?.userName);

// 打开页面（传入 userId 时选中该会话；未传则回到上次打开的会话）
const openDrawer = async (userId?: number, userName?: string) => {
	state.isVisible = true;
	// 刷新失败（接口异常/未发布）不能阻断后续选中，否则面板开了却停在空白引导态
	await refresh().catch(() => {});

	const targetId = userId && userId > 0 ? userId : Number(Local.get(LAST_CHAT_KEY) ?? 0);
	if (!targetId) return;

	// 刷新过列表后再找，离线但有聊天记录的人同样在 mergedUsers 里
	let target = mergedUsers.value.find((u) => u.userId === targetId);
	if (!target) {
		// 兜底：对方已离线且会话接口未返回时，用提醒里带的发送者信息补一条，保证能选中并拉到历史
		upsertSession({ userId: targetId, realName: userName, lastTime: nowText(), lastSendUserId: targetId });
		target = mergedUsers.value.find((u) => u.userId === targetId);
	}
	if (!target) return;

	// 强制走一次完整选中流程，避免"同 Id 但有内容"的分支被跳过
	state.currentId = 0;
	state.currentKey = '';
	await selectUser(target);
};

// 刷新在线用户与会话列表（异常不外抛：列表只是辅助信息，失败不应影响面板打开与聊天记录加载）
const refresh = async () => {
	state.loading = true;
	try {
		const [onlineRes, sessionRes] = await Promise.all([
			getAPI(SysOnlineUserApi).apiSysOnlineUserOnlineUserListGet(),
			// 会话取 50：与左侧列表首屏分页一致，避免"最近会话"比列表还短
			getAPI(SysChatApi).apiSysChatChatSessionsTopGet(50),
		]);
		state.onlineUsers = onlineRes.data.result ?? [];
		state.sessions = sessionRes.data.result ?? [];
	} catch {
		// 保留上一次的数据，静默失败
	} finally {
		state.loading = false;
	}
};

// 选中会话：拉取聊天记录并标记已读
const selectUser = async (u: ChatUserItem) => {
	// 小屏：选中即进入会话栏（列表随之隐藏，靠会话头部的返回键回退）
	if (isMobile.value) pane.value = 'chat';

	// 先定位行：同一账号多端登录有多行，点哪行就高亮哪行（聊天记录按人共用，所以下面还要判 userId）
	state.currentKey = rowKey(u);

	// 同一个人只是换了一行（另一条连接）：会话内容是一样的，不用重新拉一次记录
	if (state.currentId === u.userId && state.messages.length) {
		markRead(u.userId);
		return;
	}
	state.currentId = u.userId;
	state.messages = [];
	state.loadFailed = false;
	markRead(u.userId);
	Local.set(LAST_CHAT_KEY, u.userId); // 记住会话，刷新后直接回到这里

	try {
		const res = await getAPI(SysChatApi).apiSysChatChatMessagesTargetUserIdTopGet(u.userId, 50);
		state.messages = res.data.result ?? [];
	} catch (err: any) {
		// 不再静默：面板里给出"加载失败，点击重试"的可见出口，避免"刷新后一片空白"却查不到原因
		state.loadFailed = true;
		ElMessage.error(err?.message ?? '聊天记录加载失败');
	}
};

/** 重试加载当前会话的聊天记录 */
const retryLoad = () => {
	const u = currentUser.value;
	if (u) selectUser(u);
};

/**
 * 清空与指定用户的全部聊天记录：走后端 DeleteChatConversation，后端按当前登录用户过滤，
 * 只删"我自己参与"的那部分，不会误删别人的记录（满足"只针对当前用户"的要求）。
 */
const deleteConversation = async (userId: number) => {
	if (!userId || userId === myId.value) return;

	const u = mergedUsers.value.find((x) => x.userId === userId);
	const who = u ? displayName(u) : `用户 ${userId}`;

	ElMessageBox.confirm(`确定清空与「${who}」的全部聊天记录吗？此操作仅删除你自己参与的记录，不可恢复。`, '清空会话', {
		confirmButtonText: '清空',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(SysChatApi).apiSysChatDeleteChatConversationPost({ TargetUserId: userId });
				// 若正在看这个会话，立即清掉本地消息，给出即时反馈
				if (state.currentId === userId) state.messages = [];
				// 从会话列表里摘掉这一条摘要
				const idx = state.sessions.findIndex((s) => s.userId === userId);
				if (idx >= 0) state.sessions.splice(idx, 1);
				ElMessage.success('已清空与该用户的聊天记录');
			} catch (err: any) {
				ElMessage.error(err?.message ?? '清空失败');
			}
		})
		.catch(() => {});
};

/** 从会话面板头部点"清空"：清空当前正在看的会话 */
const clearConversation = () => {
	const u = currentUser.value;
	if (!u) return;
	deleteConversation(u.userId);
};

// 发送聊天消息
const sendChat = async (text: string) => {
	const targetId = state.currentId;
	// 自己也在列表里了，给自己发消息没有意义（面板上也没给输入框，这里再拦一道）
	if (!targetId || !text || targetId === myId.value) return;

	state.sending = true;
	try {
		await sendSignalR('SendMessageToUser', {
			messageType: 0, // MessageTypeEnum.Info
			title: `来自 ${myRealName.value} 的聊天消息`,
			message: text,
			sendUserId: myId.value,
			receiveUserIds: [targetId],
		});

		// 服务端只推给接收方，自己这条要本地补上
		const sendTime = nowText();
		state.messages.push({ id: -Date.now(), sendUserId: myId.value, sendUserName: myRealName.value, receiveUserId: targetId, message: text, sendTime });
		upsertSession({ userId: targetId, lastMessage: text, lastTime: sendTime, lastSendUserId: myId.value });
		markRead(targetId);
	} catch (err: any) {
		ElMessage.error(err?.message ?? '发送失败');
	} finally {
		state.sending = false;
	}
};

// 发送正式站内信（带标题、类型、富文本）
const openSendMessage = () => {
	const u = currentUser.value;
	if (!u) return;
	sendMessageRef.value?.openDialog({
		userId: u.userId,
		realName: u.realName,
		userName: u.userName,
		ip: u.ip,
		connectionId: u.connectionId,
	});
};

/**
 * 某账号当前的连接Id 列表（列表按连接分行，同一账号的多行各带一个连接Id）
 * exceptKey：传了就跳过这一行，用来实现"保留这一条、踢掉其它端"
 */
const connectionsOf = (userId: number, exceptKey = ''): string[] => {
	const all = mergedUsers.value.filter((r) => r.userId === userId && r.online && r.connectionId);
	const hit = exceptKey ? all.filter((r) => rowKey(r) !== exceptKey) : all;
	return hit.map((r) => r.connectionId as string);
};

/** 逐条发强制下线（对应后台 Hub 的 ForceOffline） */
const kickConnections = async (ids: string[]) => {
	for (const connectionId of ids) await sendSignalR('ForceOffline', { connectionId });
};

/** 断开"这一条"连接：一行就是一个连接，想留哪端留哪端 */
const forceOffline = async (u: ChatUserItem) => {
	if (!u.connectionId) return ElMessage.warning('没有拿到该连接的 Id，请刷新后重试');

	const where = `${u.ip ?? ''} ${u.browser ?? ''}`.trim();
	const who = displayName(u);
	const text = u.isMe ? `确定断开「${who}」的这条连接${where ? `（${where}）` : ''}？` : `确定强制「${who}」下线${where ? `（${where}）` : ''}？`;

	ElMessageBox.confirm(text, '提示', { confirmButtonText: '确认', cancelButtonText: '取消', type: 'warning' })
		.then(async () => {
			try {
				await kickConnections([u.connectionId as string]);
			} catch (err: any) {
				ElMessage.error(err?.message ?? err);
			}
		})
		.catch(() => {});
};

/**
 * 断开同一账号的"其它"连接：**点哪一行就保留哪一行**，其余同账号连接全部断开
 * 典型场景：自己在别处还挂着几个端，只想把其它端清掉（"我想删掉同一个账号其他连接"）。
 */
const forceOfflineAll = async (u: ChatUserItem) => {
	// 保留点中的这一条，其余全踢；只有一条时退化为踢它自己
	const others = connectionsOf(u.userId, rowKey(u));
	const ids = others.length ? others : connectionsOf(u.userId);
	if (!ids.length) return ElMessage.warning('没有拿到该账号的连接信息，请刷新后重试');

	const who = displayName(u);
	const extra = others.length ? '（保留这一条）' : '';
	ElMessageBox.confirm(`确定断开「${who}」的其它 ${ids.length} 个连接${extra}？`, '提示', {
		confirmButtonText: '确认',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			try {
				await kickConnections(ids);
			} catch (err: any) {
				ElMessage.error(err?.message ?? err);
			}
		})
		.catch(() => {});
};

// 导出对象
defineExpose({ openDrawer });
</script>

<!-- 抽屉 append-to-body 后会被传送到 body 下，scoped 的 :deep 需要 data-v 祖先才能命中，
     传送后祖先链断了选择器即失效 —— 抽屉体样式因此改用"独立类名 + 非 scoped 块"承载 -->
<style lang="scss">
// 头像兜底与在线状态点：列表与面板共用，避免两边各写一份
@import './component/chatBase.scss';
// 强制下线提示框同样被传送到 body，必须非 scoped 承载

.online-user-drawer .el-drawer__body {
	display: flex;
	flex-direction: column;
	height: 100%;
	padding: 0;
	overflow: hidden;
}

.online-user-drawer__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	width: 100%;
}

.online-user-drawer__title {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 15px;
	font-weight: 700;

	// 连接数标签：取头部文字色的半透明膜，比"人数"标签弱一档
	.el-tag.online-user-drawer__conn {
		color: var(--el-color-white);
		background-color: color-mix(in srgb, currentColor 22%, transparent);
		border-color: color-mix(in srgb, currentColor 32%, transparent);
	}
}

.online-user-drawer__acts {
	display: flex;
	gap: 2px;
}

.online-user-drawer__acts .el-button {
	padding: 6px;
	// 头部操作键改变量而不是写 color：el-button 自带 color，也不吃祖先色
	--el-button-text-color: var(--el-color-white);
	--el-button-hover-text-color: var(--el-color-white);
}

/* 亮色下 is-text 的 hover/active 底是近白实色，压在主题色上就是一块白框；这里换成头部文字色的半透明膜。
 * 暗色那一档本就是抬亮膜、压在主题色上仍分得开，所以只覆盖亮色 */
html:not([data-theme='dark']) .online-user-drawer__acts .el-button {
	--el-fill-color-light: color-mix(in srgb, var(--el-color-white) 15%, transparent);
	--el-fill-color: color-mix(in srgb, var(--el-color-white) 25%, transparent);
}

.online-chat {
	display: flex;
	flex: 1;
	min-height: 0;
	height: 100%;

	// 可拖动分隔条：本身只有 2px（更细），::before 把命中热区向左右各扩 5px，否则很难精准抓到
	&__resizer {
		position: relative;
		flex: none;
		width: 2px;
		cursor: col-resize;
		background-color: var(--el-border-color-lighter);
		transition: background-color 0.2s;

		&::before {
			content: '';
			position: absolute;
			top: 0;
			bottom: 0;
			left: -5px;
			right: -5px;
		}

		&:hover,
		&.is-active {
			background-color: var(--el-color-primary);
		}
	}
}

// 拖动期间：整页锁定 col-resize 光标并禁掉文本选中（否则拖的时候会一路选中列表文字）
body.is-col-resizing {
	cursor: col-resize !important;
	user-select: none;
}
/* 小屏：单栏，选中谁就整屏铺谁（列表 / 会话互斥） */
.online-chat.is-mobile {
	> * {
		flex: 1 1 auto;
		width: 100%;
	}
}
</style>
