/**
 * 聊天页专用的前端视图类型。
 * 说明：这里只放「后端没有对应 DTO」的运行时结构（SignalR 事件 payload、本地临时消息）；
 * 凡后台有 DTO 的一律直接用真实 DTO（如 AgentChatHistoryDto、SysFile、AgentChatHistoryBindLogDto）。
 */

import type { AgentChatHistoryDto } from '/@/api-services/agent/models';

/**
 * 聊天消息：服务端 DTO + 前端本地附加字段。
 * recommendQuestions 不落库（由绑定日志派生 / 实时事件下发），AgentChatHistoryDto 本身没有该字段；
 * durationMs 同样不落库（提问行与回复行的落库时间差 / 本轮实时计时），因此用交叉类型扩展，
 * 避免直接给 DTO 加属性触发 TS2339。
 */
export type ChatHistoryItem = AgentChatHistoryDto & { recommendQuestions?: string[]; durationMs?: number };

/** 工具审批请求：后端 toolApprovalRequest 事件推送的 payload，前端回传时用 requestId 唤醒等待 */
export interface ToolApprovalRequest {
	requestId: string;
	toolName: string;
	arguments: string;
}

/** Token 用量快照：后端 tokenUsage 事件推送的 payload（对应后端 TokenQuotaSnapshot） */
export interface TokenQuotaView {
	enabled: boolean;
	exceeded: boolean;
	exceededMessage: string;
	sessionUsed: number;
	sessionLimit: number;
	tenantDailyUsed: number;
	tenantDailyLimit: number;
	lastTotal: number;
}
