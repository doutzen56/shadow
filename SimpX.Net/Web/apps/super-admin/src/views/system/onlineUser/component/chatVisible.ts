/**
 * 在线聊天面板（抽屉）可见状态
 *
 * signalR 收到消息时默认会弹右上角全局通知；但若聊天面板正开着，
 * 消息已经直接落在聊天记录里了，再弹一次通知就是重复打扰 —— 故这里
 * 用模块级标志位把"面板是否可见"暴露给 signalR 做抑制判断。
 *
 * 之所以不放 store：signalR.ts 是纯工具模块，只应依赖无副作用的读取。
 */
let visible = false;

/** 聊天面板当前是否可见 */
export const isChatVisible = (): boolean => visible;

/** 由在线用户页在抽屉打开/关闭时维护 */
export const setChatVisible = (value: boolean): void => {
	visible = value;
};
