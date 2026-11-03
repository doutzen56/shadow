import type { ChatUserItem } from './chatTypes';

/**
 * 展示名：优先姓名，其次账号，都没有才落到 Id（左侧列表与右侧面板共用）
 */
export const displayName = (u?: ChatUserItem | null): string => u?.realName || u?.userName || String(u?.userId ?? '');

/** 头像兜底首字：没有可用名称时用 ? 占位 */
export const avatarText = (u?: ChatUserItem | null): string => displayName(u).trim().slice(0, 1) || '?';

/**
 * 列表行唯一键
 *
 * 在线部分**按连接分行**（同一账号多端登录各占一行），所以用连接Id 当键；
 * 离线行没有连接Id，退回用户Id。直接用 userId 当键会让同一个账号的多行互相撞键、渲染错乱。
 */
export const rowKey = (u?: ChatUserItem | null): string => (u?.connectionId ? u.connectionId : `u-${u?.userId ?? 0}`);
