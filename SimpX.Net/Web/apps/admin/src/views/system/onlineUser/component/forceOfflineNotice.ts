import { ElMessageBox } from 'element-plus';

// 样式随组件一起加载：弹框可能出现在任意页面，不能依赖某个页面的样式块
import './forceOffline.scss';

/** HTML 转义映射：操作者姓名来自服务端，进入 innerHTML 前必须转义 */
const ESC: Record<string, string> = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

/** 转义用于 innerHTML 的文本 */
const escapeHtml = (v: unknown) => String(v ?? '').replace(/[&<>"']/g, (c) => ESC[c] ?? c);

/**
 * 取字段值：SignalR 默认 camelCase，同时兼容历史的大写形式
 * @param o 服务端推送对象
 * @param keys 候选字段名（按顺序取第一个非空值）
 */
const pick = (o: Record<string, any>, ...keys: string[]) => {
	for (const k of keys) {
		const v = o[k];
		if (v != null && v !== '') return String(v);
	}
	return '';
};

/** 时间展示：优先用服务端的 Time（与操作日志对齐），拿不到才退回本地时间 */
const fmtTime = (v: string) => {
	const parsed = v ? new Date(v) : null;
	const d = parsed && !Number.isNaN(parsed.getTime()) ? parsed : new Date();
	const p = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
};

/**
 * 弹出"被强制下线"提示
 * @param payload 服务端下发的 ForceOfflineNotice（operator / reason / time），兼容旧版纯字符串广播
 * @returns 用户点击"重新登录"后 resolve
 */
export const showForceOfflineNotice = (payload?: unknown): Promise<void> => {
	// 旧版广播的是纯字符串（只带原因），这里一并兜住
	const legacy = typeof payload === 'string' ? payload : '';
	const o = typeof payload === 'object' && payload ? (payload as Record<string, any>) : {};

	const who = escapeHtml(pick(o, 'operator', 'Operator') || '系统管理员');
	const why = escapeHtml(pick(o, 'reason', 'Reason') || legacy || '账号安全策略');
	const time = fmtTime(pick(o, 'time', 'Time'));

	const html = `
		<div class="fo-wrap">
			<div class="fo-hero">
				<div class="fo-hero__ring">
					<i class="fo-hero__wave fo-hero__wave--1"></i>
					<i class="fo-hero__wave fo-hero__wave--2"></i>
					<svg class="fo-hero__ico" viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
						<path d="M13 2 4.5 13.5H11L10 22l8.5-11.5H12L13 2z" fill="currentColor"/>
					</svg>
				</div>
				<div class="fo-hero__txt">
					<div class="fo-hero__title">会话已被终止</div>
					<div class="fo-hero__sub">Session Terminated</div>
				</div>
			</div>
			<div class="fo-body">
				<p class="fo-body__main">您已被 <b>${who}</b> 强制下线，请重新登录。</p>
				<div class="fo-meta">
					<div class="fo-meta__i"><span>原因</span><em>${why}</em></div>
					<div class="fo-meta__i"><span>时间</span><em>${time}</em></div>
				</div>
				<p class="fo-tip">若非本人操作，请重新登录后立即修改密码，并联系系统管理员确认。</p>
			</div>
		</div>`;

	// 强制下线是终态：不允许点遮罩、按 Esc 或改 hash 把框关掉后继续使用已失效的会话
	return ElMessageBox.alert(html, '', {
		dangerouslyUseHTMLString: true,
		customClass: 'fo-msgbox',
		confirmButtonText: '重新登录',
		confirmButtonType: 'danger',
		showClose: false,
		closeOnClickModal: false,
		closeOnPressEscape: false,
		closeOnHashChange: false,
	})
		.then(() => undefined)
		.catch(() => undefined);
};
