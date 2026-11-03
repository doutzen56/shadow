/**
 * 在线用户聊天：左侧列表项（在线用户与最近会话合并后的统一结构）
 */
export interface ChatUserItem {
	/** 用户Id */
	userId: number;
	/** 账号 */
	userName?: string | null;
	/** 姓名 */
	realName?: string | null;
	/** 头像 */
	avatar?: string | null;
	/** 是否在线 */
	online: boolean;
	/** 登录IP（离线为空） */
	ip?: string | null;
	/** 浏览器（离线为空） */
	browser?: string | null;
	/** SignalR 连接Id（离线为空；列表按连接分行，一行就是一个连接Id，强制下线直接踢它） */
	connectionId?: string | null;
	/** 该账号当前连接数（>1 说明多端登录，列表里会出现多行） */
	deviceCount?: number;
	/** 当前行是该账号的第几个连接（从 1 开始，配合 deviceCount 展示 "2/3 端"） */
	deviceIndex?: number;
	/** 最后一条消息 */
	lastMessage?: string | null;
	/** 最后消息时间 */
	lastTime?: string | null;
	/** 最后一条消息的发送者Id（用于判断是否算未读） */
	lastSendUserId?: number;
	/** 是否有未读 */
	unread?: boolean;
	/** 是不是自己（在线列表里现在也包含自己，这一项用于免掉"给自己发消息 / 踢自己下线"这类无效操作） */
	isMe?: boolean;
}

/**
 * 聊天消息（与后端 ChatMessageOutput 对齐）
 */
export interface ChatMessageItem {
	id?: number;
	sendUserId?: number;
	sendUserName?: string | null;
	receiveUserId?: number;
	message?: string | null;
	// 类型放宽为 string | Date：自动生成层按 swagger 声明成 Date，但 axios 不做日期反序列化，运行时其实是字符串
	sendTime?: string | Date | null;
}
