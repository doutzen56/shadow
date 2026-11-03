/**
 * 展示辅助（@mldong/jeeflow-ui）：状态标签/时间格式化（规范 06 §2.9 枚举）
 */

import { hostAdapters } from './adapters';

/** 实例状态 → 展示标签 */
export function stateLabel(s: number | string | null | undefined): string {
  const v = String(s)
  if (v === '10' || v === 'DOING') return '进行中'
  if (v === '20' || v === 'DONE' || v === 'FINISHED') return '已完成'
  if (v === '30' || v === 'WITHDRAW') return '已撤回'
  if (v === '40' || v === 'INTERRUPT') return '已终止'
  if (v === '45' || v === 'REJECT') return '已拒绝'
  if (v === '50' || v === 'PENDING') return '挂起'
  if (v === '99' || v === 'ABANDON') return '已废弃'
  return s != null ? String(s) : '-'
}

/** 实例状态 → JfBadge type */
export function stateBadgeType(s: number | string | null | undefined): 'doing' | 'done' | 'reject' | 'info' {
  const v = String(s)
  if (v === '10' || v === 'DOING') return 'doing'
  if (v === '20' || v === 'DONE' || v === 'FINISHED') return 'done'
  if (v === '45' || v === 'REJECT' || v === '99') return 'reject'
  return 'info'
}

/** 任务状态 → 展示标签 */
export function taskStateLabel(s: number | string | null | undefined): string {
  const v = String(s)
  if (v === '10' || v === 'DOING') return '待办'
  if (v === '20' || v === 'FINISHED') return '已完成'
  if (v === '99' || v === 'ABANDON') return '已废弃'
  if (v === '30' || v === 'WITHDRAW') return '已撤回'
  if (v === '40' || v === 'INTERRUPT') return '已终止'
  return s != null ? String(s) : '-'
}

/** 任务状态 → JfBadge type */
export function taskStateBadgeType(s: number | string | null | undefined): 'doing' | 'done' | 'reject' | 'info' {
  const v = String(s)
  if (v === '10' || v === 'DOING') return 'doing'
  if (v === '20' || v === 'FINISHED') return 'done'
  if (v === '99' || v === 'ABANDON') return 'reject'
  return 'info'
}

/** 时间格式化：yyyy-MM-dd HH:mm:ss 或宽松（非法时间原样返回） */
export function fmtTime(t: string | null | undefined, short = false): string {
  if (!t) return ''
  const d = new Date(t)
  if (isNaN(d.getTime())) return t
  if (short) {
    return d.toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
  }
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`
}

/** 是否任务节点（snaker:task / task） */
function isTaskType(type: unknown): boolean {
  return type === 'snaker:task' || type === 'task'
}

/** 流程 JSON 图中 start 的下一个任务节点（对齐 vben5 getFirstTaskNode） */
export function firstTaskNode(graph: any): any | null {
  const nodes: any[] = graph?.nodes || []
  const edges: any[] = graph?.edges || []
  const start = nodes.find((n) => n?.type === 'snaker:start' || n?.type === 'start')
  if (start) {
    const edge = edges.find((e) => e.sourceNodeId === start.id)
    if (edge) {
      const target = nodes.find((n) => n.id === edge.targetNodeId && isTaskType(n?.type))
      if (target) return target
    }
  }
  return nodes.find((n) => isTaskType(n?.type)) || null
}

/** 按 taskName / 节点 id 定位任务节点 */
export function findTaskNode(graph: any, taskName?: string | null): any | null {
  if (!taskName) return null
  const nodes: any[] = graph?.nodes || []
  return nodes.find((n) =>
    isTaskType(n?.type) && (n.id === taskName || n.properties?.name === taskName || n.text?.value === taskName),
  ) || null
}

/** 流程 JSON 图中首个任务节点的 formKey（发起表单定位） */
export function firstTaskFormKey(graph: any): string {
  const n = firstTaskNode(graph)
  if (n?.properties?.form) return n.properties.form
  for (const node of graph?.nodes || []) {
    if (isTaskType(node?.type) && node?.properties?.form) return node.properties.form
  }
  const url = graph?.instanceUrl
  if (typeof url === 'string' && url && !url.includes('/')) return url
  return ''
}

function truthyFlag(v: unknown): boolean {
  return v === 1 || v === true || v === '1'
}

export type ActionBtnKey =
  | 'AGREE'
  | 'REJECT'
  | 'ROLLBACK'
  | 'ROLLBACK_TO_OPERATOR'
  | 'JUMP'
  | 'COUNTERSIGN_DISAGREE'
  | 'ADD_CANDIDATE'
  | 'SURROGATE'

export function isCountersign(performType: unknown): boolean {
  return performType === 1 || performType === '1' || performType === 'COUNTERSIGN' || performType === 'ALL'
}

/** 有 actionBtns 用配置；否则按会签/普通给默认集 */
export function resolveActionBtns(taskNode: any, performType?: unknown): ActionBtnKey[] {
  const configured = taskNode?.properties?.actionBtns
  if (Array.isArray(configured) && configured.length) {
    return configured.filter((k: unknown) => typeof k === 'string') as ActionBtnKey[]
  }
  if (isCountersign(performType ?? taskNode?.properties?.performType)) {
    return ['AGREE', 'COUNTERSIGN_DISAGREE', 'ADD_CANDIDATE']
  }
  return ['AGREE', 'REJECT', 'ROLLBACK', 'ROLLBACK_TO_OPERATOR', 'JUMP']
}

/** submitType → 中文（无字典服务，写死对照表） */
export function submitTypeLabel(n: number | string | null | undefined): string {
  const map: Record<string, string> = {
    '0': '发起申请',
    '1': '同意',
    '2': '拒绝',
    '3': '退回上一步',
    '4': '跳转',
    '5': '重新提交',
    '6': '退回发起人',
    '20': '会签拒绝',
  }
  return map[String(n)] ?? (n != null ? String(n) : '-')
}

/** jsonObject 上的发起附加项开关 */
export function initiateExtraFlags(graph: any): {
  selectUser: boolean
  cc: boolean
  reason: boolean
  attachment: boolean
  any: boolean
} {
  const selectUser = truthyFlag(graph?.selectUserOnInitiate)
  const cc = truthyFlag(graph?.enableCcActors)
  const reason = truthyFlag(graph?.enableApplyReason)
  const attachment = truthyFlag(graph?.enableAttachment)
  return { selectUser, cc, reason, attachment, any: selectUser || cc || reason || attachment }
}

/** 变量对象里的 f_* / tf_* 切片 */
function onlyPrefixed(vars: Record<string, any> | null | undefined, prefix: string): Record<string, any> {
  const out: Record<string, any> = {}
  for (const [k, v] of Object.entries(vars || {})) {
    if (k.startsWith(prefix)) out[k] = v
  }
  return out
}

function objOrEmpty(v: unknown): Record<string, any> {
  return v && typeof v === 'object' ? (v as Record<string, any>) : {}
}

/** 实例级表单源：processInstance/detail 的 formData（引擎已切好的 f_* 集） */
export function pickInstanceFormData(source: any): Record<string, any> {
  return objOrEmpty(source?.formData)
}

/** 任务级表单源：任务行与 processTask/detail 的 taskFormData（tf_* 集） */
export function pickTaskFormData(source: any): Record<string, any> {
  return objOrEmpty(source?.taskFormData)
}

/** 任务变量对象：服务端把 variable JSON 解析成的 ext（任务级为空时引擎回退实例级） */
export function pickExt(source: any): Record<string, any> {
  return objOrEmpty(source?.ext)
}

/** 实例变量对象：任务行上的 instanceExt */
export function pickInstanceExt(source: any): Record<string, any> {
  return objOrEmpty(source?.instanceExt)
}

/**
 * 从实例/任务响应里抽 f_* 业务表单数据。取数顺序对齐 vben5-wf 消费口径：
 * formData（实例 detail 的 f_* 切片）→ instanceExt（任务行上的实例变量）。
 *
 * 刻意不读 variable / instanceVariable 原串，也不读 variables 全集：前者八栈出口类型不统一
 * （java/rust/moon/csharp 出 JSON 串，go/python/node/php 出对象），后者在 moon 栈键名被 camel 化；
 * 服务端已统一解析为 ext，前端再解析等于把类型漂移引进来。issues/124 §7 已裁定门面出口只留
 * ext/instanceExt，variables 将随引擎侧收口一并下线。
 */
export function extractBizFormData(source: any): Record<string, any> {
  if (!source) return {}
  for (const cand of [pickInstanceFormData(source), pickInstanceExt(source)]) {
    const picked = onlyPrefixed(cand, 'f_')
    if (Object.keys(picked).length) return picked
  }
  return {}
}

/** 从任务响应抽审批人填过的 tf_*（办理表单回显源） */
export function extractTaskFormData(source: any): Record<string, any> {
  if (!source) return {}
  for (const cand of [pickTaskFormData(source), pickExt(source)]) {
    const picked = onlyPrefixed(cand, 'tf_')
    if (Object.keys(picked).length) return picked
  }
  return {}
}

/** 列表行取变量：任务行是 instanceExt→ext，实例行是 ext（detail 场景不走本函数，见 issues/124 §7） */
function rowVarSources(row: any): Record<string, any>[] {
  return [pickInstanceExt(row), pickExt(row)]
}

/**
 * 列表行流程标题（vben5-wf 口径：列读 ext.autoGenTitle / ext.f_title，
 * 任务行上的实例变量叫 instanceExt）。取不到再回落流程定义显示名。
 */
export function rowTitle(row: any): string {
  for (const src of rowVarSources(row)) {
    const t = src.autoGenTitle || src.f_title
    if (t) return String(t)
  }
  return String(row?.displayName || row?.processDefineDisplayName || '-')
}

/** 列表行发起人姓名（instanceExt.u_realName / ext.u_realName），取不到回落工号 */
export function rowInitiator(row: any): string {
  for (const src of rowVarSources(row)) {
    if (src.u_realName) return String(src.u_realName)
  }
  return String(row?.operator || '-')
}

// ── 办理人文本（processInstance/getAssigneeTextData）──
// 该接口只回「节点名:userId」（如 `审批人:1300000000111`），不解析姓名，故前端把其中的 userId 反查成真实姓名。
// 注意：value 是流程图定位文本用的 id（JfFlowViewer → lf.updateText(value, label)），必须原样保留，只改 label。

/** 取 label 里「节点名:」之后的那段 id（多个用顿号/逗号分隔） */
function assigneeIdTokens(label: string): string[] {
  const s = String(label ?? '')
  const i = Math.max(s.lastIndexOf(':'), s.lastIndexOf('：'))
  const tail = i >= 0 ? s.slice(i + 1) : s
  return tail.split(/[,，、\s]+/).map((t) => t.trim()).filter(Boolean)
}

/**
 * 把办理人文本里的 userId 换成真实姓名（查不到则保持原样，不会误显示 id 以外的东西）。
 * 无宿主 getUsersByIds 或请求失败时原样返回，由调用方降级显示。
 */
export async function resolveAssigneeLabels(rows: { value: string; label: string }[] | null | undefined): Promise<{ value: string; label: string }[]> {
  const list = (Array.isArray(rows) ? rows : []).filter((r) => r && r.value != null && String(r.value))
  if (!list.length) return (Array.isArray(rows) ? rows : []) as { value: string; label: string }[]
  const need = new Set<string>()
  for (const r of list) {
    need.add(String(r.value))
    for (const t of assigneeIdTokens(r.label)) need.add(t)
  }
  let users: { userId?: string; realName?: string }[] = []
  try {
    users = (await hostAdapters.getUsersByIds?.([...need])) ?? []
  } catch {
    users = []
  }
  const map = new Map<string, string>()
  for (const u of users) if (u?.userId && u?.realName) map.set(String(u.userId), u.realName)
  if (!map.size) return list
  return list.map((r) => {
    let label = String(r.label ?? '')
    for (const [id, name] of map) if (label.includes(id)) label = label.split(id).join(name)
    return { ...r, label }
  })
}


