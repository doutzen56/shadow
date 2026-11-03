/**
 * 工作流展示层字典：节点/步骤的状态、审批模式、审批人类型等统一文案与配色。
 * 供待办、已办、抄送、我发起的等页面共用，避免同一份映射被复制四遍。
 */

/** 特殊节点（开始/结束/网关/条件等）没有审批人与审批模式 */
export const isSpecialNode = (nodeType?: string): boolean => {
	if (!nodeType) return false;
	return ['start', 'circle', 'end', 'rect', 'condition', 'gateway', 'parallel', 'exclusive'].includes(nodeType.toLowerCase());
};

/** 执行状态：tone 对应工作流模块公共样式 .wf-status.is-* 的配色（views/workFlow/styles/workflow.scss），tagType 兼容仍用 el-tag 的地方 */
export const execStatusInfo = (status?: number) => {
	switch (status) {
		case 0:
			return { text: '待执行', tone: 'pending', tagType: 'info' as const };
		case 1:
			return { text: '执行中', tone: 'warning', tagType: 'warning' as const };
		case 2:
			return { text: '已完成', tone: 'success', tagType: 'success' as const };
		case 3:
			return { text: '已拒绝', tone: 'danger', tagType: 'danger' as const };
		case 4:
			return { text: '已跳过', tone: 'info', tagType: 'info' as const };
		default:
			return { text: '未执行', tone: 'pending', tagType: 'info' as const };
	}
};

/** 操作类型：后端返回英文枚举（approve / complete…），展示层统一中文 */
export const actionTypeText = (type?: string): string => {
	const map: Record<string, string> = {
		start: '发起流程',
		submit: '提交',
		approve: '审批通过',
		agree: '同意',
		reject: '驳回',
		transfer: '转办',
		complete: '流程完成',
		terminate: '终止',
		suspend: '挂起',
		resume: '恢复',
		cc: '抄送',
		notify: '通知',
		revoke: '撤回',
		delete: '删除',
		auto: '自动处理',
	};
	return map[(type ?? '').toLowerCase()] ?? (type || '-');
};

export const approvalModeText = (mode?: string): string => {
	const map: Record<string, string> = {
		single: '单人',
		or: '或签',
		and: '会签',
		sequence: '依次',
	};
	return map[(mode ?? '').toLowerCase()] ?? '-';
};

export const approverTypeText = (type?: string): string => {
	const map: Record<string, string> = {
		user: '指定用户',
		dept: '部门',
		role: '角色',
		starter: '发起人',
	};
	return map[(type ?? '').toLowerCase()] ?? '-';
};

/** 步骤类型：后端返回的是 PascalCase（如 UserTask），统一小写匹配，未知则回退原值 */
export const stepTypeText = (type?: string): string => {
	const map: Record<string, string> = {
		start: '开始',
		end: '结束',
		user: '审批',
		approval: '审批',
		usertask: '审批',
		cc: '抄送',
		notify: '通知',
		condition: '条件',
		gateway: '网关',
		parallel: '并行',
		servicetask: '服务',
		scripttask: '脚本',
		subflow: '子流程',
		timer: '定时',
		task: '任务',
	};
	return map[(type ?? '').toLowerCase()] ?? (type || '-');
};

/** 已办状态：1 已处理 / 2 已拒绝 / 其余待处理（已办列表行与详情头部共用） */
export const doneStatusInfo = (status?: number) => {
	if (status === 1) return { text: '已处理', tone: 'success' };
	if (status === 2) return { text: '已拒绝', tone: 'danger' };
	return { text: '待处理', tone: 'pending' };
};

/** 流程实例状态：0 待执行 / 1 运行中 / 2 已完成 / 3 已终止 / 4 已暂停（我发起的列表、详情头部与删除确认共用） */
export const instanceStatusInfo = (status?: number) => {
	switch (status) {
		case 0:
			return { text: '待执行', tone: 'info' };
		case 1:
			return { text: '运行中', tone: 'warning' };
		case 2:
			return { text: '已完成', tone: 'success' };
		case 3:
			return { text: '已终止', tone: 'danger' };
		case 4:
			return { text: '已暂停', tone: 'warning' };
		default:
			return { text: '未知', tone: 'info' };
	}
};

/** 优先级：1 高 / 2 中 / 3 低 */
export const priorityInfo = (priority?: number) => {
	if (priority === 1) return { text: '高', tone: 'danger', tagType: 'danger' as const };
	if (priority === 2) return { text: '中', tone: 'warning', tagType: 'warning' as const };
	if (priority === 3) return { text: '低', tone: 'info', tagType: 'info' as const };
	return { text: '-', tone: 'pending', tagType: 'info' as const };
};
