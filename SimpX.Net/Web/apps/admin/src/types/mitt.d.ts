/**
 * mitt 事件类型定义
 *
 * @method openSettingsDrawer 打开布局设置弹窗
 * @method restoreDefault 分栏布局，鼠标移入、移出数据显示
 * @method setSendColumnsChildren 分栏布局，鼠标移入、移出菜单数据传入到 navMenu 下的菜单中
 * @method setSendClassicChildren 经典布局，开启切割菜单时，菜单数据传入到 navMenu 下的菜单中
 * @method getBreadcrumbIndexSetFilterRoutes 布局设置弹窗，开启切割菜单时，菜单数据传入到 navMenu 下的菜单中
 * @method layoutMobileResize 浏览器窗口改变时，用于适配移动端界面显示
 * @method openOrCloseSortable 布局设置弹窗，开启 TagsView 拖拽
 * @method openShareTagsView 布局设置弹窗，开启 TagsView 共用
 * @method onTagsViewRefreshRouterView tagsview 刷新界面
 * @method onCurrentContextmenuClick tagsview 右键菜单每项点击时
 * @method submitRefreshFk
 * @method signalRReconnected SignalR 断线重连成功，订阅方补拉漏推数据
 * @method signalRStartFailed SignalR 连接失败/重连中，订阅方发请求刷新 token + 补拉数据
 * @method chatMessage 收到站内信/聊天消息（SysLogMsg），在线聊天面板据此追加气泡或标记未读
 * @method openOnlineChat 点击右下角"xxx 发来消息"提醒，打开在线用户面板并定位到该发送者（payload: { userId, userName }）
 */
declare type MittType<T = any> = {
	openSettingsDrawer?: string;
	restoreDefault?: string;
	setSendColumnsChildren: T;
	setSendClassicChildren: T;
	getBreadcrumbIndexSetFilterRoutes?: string;
	layoutMobileResize: T;
	openOrCloseSortable?: string;
	openShareTagsView?: string;
	onTagsViewRefreshRouterView?: T;
	onCurrentContextmenuClick?: T;
	submitRefreshFk?: T;
	signalRReconnected?: T;
	signalRStartFailed?: T;
	chatMessage?: T;
	openOnlineChat?: T;
};

// mitt 参数类型定义
declare type LayoutMobileResize = {
	layout: string;
	clientWidth: number;
};

// mitt 参数菜单类型
declare type MittMenu = {
	children: RouteRecordRaw[];
	item?: RouteItem;
};
