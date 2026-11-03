import { AgentAppDto } from '/@/api-services/agent/models';
import { CardAccent } from '/@/views/agent/cardAccent';

export type AgentCapabilityTag = { label: string; type: CardAccent };

/**
 * 已开启的能力标签：每类固定一个语义色，卡片上不用逐个读字就能分清配了哪几类。
 * 「智能体管理」与「我的智能体」展示的是同一批开关，颜色必须出自这一张表，否则两个页面会各说各话。
 */
export const agentCapabilityTags = (row: AgentAppDto): AgentCapabilityTag[] => {
	const list: AgentCapabilityTag[] = [];
	if (row.isAITools) list.push({ label: '工具', type: 'primary' });
	if (row.isSkill) list.push({ label: '技能', type: 'success' });
	if (row.isMcp) list.push({ label: 'MCP', type: 'warning' });
	if (row.isMemory) list.push({ label: '记忆', type: 'info' });
	if (row.isAIMessageCompaction) list.push({ label: '压缩', type: 'danger' });
	return list;
};
