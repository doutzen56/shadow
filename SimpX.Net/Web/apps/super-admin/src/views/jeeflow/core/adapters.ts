/**
 * 宿主适配器接口（原 @mldong/jeeflow-ui 的 adapters，已去除 Vue 上下文依赖）
 *
 * 流程中心不绑宿主 REST（无 /sys/user/select）。选人 / 角色 / 字典 / 上传
 * 一律由宿主注入函数；缺哪个 adapter，对应控件降级或隐藏。
 *
 * 当前已接入附件上传（走系统文件接口 /api/sysFile/uploadFile）、选人列表 listUsers
 * （走系统用户分页接口 /api/sysUser/page）；其余 adapter
 * （getDict / getUsersByIds / listRoles）仍为空 → 对应控件走降级分支，
 * 需要启用时在此 hostAdapters 上补上实现即可。
 *
 * 选人优先级（JfUserPicker）：
 *  1. scene=candidate 且有 taskId → 项目标准接口 api/jeeflowTask/candidatePage
 *  2. hostAdapters.listUsers(keyword, ctx)
 *  3. 提示未注入 listUsers
 *
 * 流程 JSON 里的 selectUserApi 仅作为 ctx.apiHint 传给宿主，ui-kit 不直接 fetch。
 */

import { getAPI } from '/@/utils/axios-utils'
import { SysFileApi, SysUserApi } from '/@/api-services/system/api'
import type { SysFile } from '/@/api-services/system/models/sys-file'
import type { UserOutput } from '/@/api-services/system/models/user-output'

/** 选人场景：办理候选走引擎；其余走宿主 listUsers */
export type JeeflowUserPickerScene =
  | 'candidate'
  | 'nextOperator'
  | 'cc'
  | 'surrogate'
  | 'assignee'

export interface JeeflowUserRow {
  userId: string
  realName: string
  deptName?: string
  postName?: string
  [key: string]: unknown
}

export interface JeeflowRoleRow {
  roleId: string
  roleName: string
  [key: string]: unknown
}

export interface JeeflowDictItem {
  value: string
  label: string
  [key: string]: unknown
}

export interface JeeflowListUsersContext {
  scene: JeeflowUserPickerScene
  taskId?: string | null
  /** 流程 JSON selectUserApi，仅 hint */
  apiHint?: string
}

export interface JeeflowHostAdapters {
  /** 无任务候选池时的选人（抄送/转办/委托/指定下一处理人/设计器指派） */
  listUsers?: (keyword: string, ctx: JeeflowListUsersContext) => Promise<JeeflowUserRow[]>
  /** 已选 id → 姓名（chips 回显；不传则显示 userId） */
  getUsersByIds?: (ids: string[]) => Promise<JeeflowUserRow[]>
  /** 设计器按角色指派；不传则角色指派隐藏 */
  listRoles?: (keyword: string) => Promise<JeeflowRoleRow[]>
  /** SchemaForm ApiDict / 设计器字典；不传则 ApiDict 退化为空下拉 */
  getDict?: (code: string) => Promise<JeeflowDictItem[]>
  /** 附件上传，返回系统文件对象（res.data.result）；不传则只存文件名 */
  upload?: (file: File) => Promise<SysFile>
}

// 用户姓名缓存：userId → JeeflowUserRow。一次拉全量后复用（与项目其它选人场景一致用大 pageSize），
// 避免每次按 id 反查都去翻页（原实现最多 20 次串行请求）。JfUserPicker 的 chips 回显与办理人文本共用。
let userNameCache: Map<string, JeeflowUserRow> | null = null
let userNameCacheTask: Promise<Map<string, JeeflowUserRow>> | null = null

function loadUserNames(): Promise<Map<string, JeeflowUserRow>> {
  if (userNameCache) return Promise.resolve(userNameCache)
  if (!userNameCacheTask) {
    userNameCacheTask = (async () => {
      const m = new Map<string, JeeflowUserRow>()
      try {
        const res = await getAPI(SysUserApi).apiSysUserPagePost({ page: 1, pageSize: 10000 })
        for (const u of (res.data?.result?.items ?? []) as UserOutput[]) {
          m.set(String(u.id), { userId: String(u.id), realName: u.realName ?? '', deptName: u.orgName ?? '', postName: u.posName ?? '' })
        }
      } catch {
        // 拉不到就留空表，调用方降级显示 userId
      }
      userNameCache = m
      userNameCacheTask = null
      return m
    })()
  }
  return userNameCacheTask
}

/** 宿主注入的能力集合 */
export const hostAdapters: JeeflowHostAdapters = {
  // 选人（抄送/转办/委托/指定下一处理人/设计器指派）：走系统用户分页接口 /api/sysUser/page
  listUsers: async (keyword: string, _ctx: JeeflowListUsersContext): Promise<JeeflowUserRow[]> => {
    const res = await getAPI(SysUserApi).apiSysUserPagePost({
      keyword: keyword || undefined,
      page: 1,
      pageSize: 500,
    })
    const items: UserOutput[] = res.data?.result?.items ?? []
    return items.map((u) => ({
      userId: String(u.id),
      realName: u.realName ?? '',
      deptName: u.orgName ?? '',
      postName: u.posName ?? '',
    }))
  },
  // 按 userId 列表反查用户：走上面那份全量缓存（首次拉一次，之后零请求）
  getUsersByIds: async (ids: string[]): Promise<JeeflowUserRow[]> => {
    if (!ids.length) return []
    const m = await loadUserNames()
    return ids
      .map((id) => m.get(String(id)))
      .filter((u): u is JeeflowUserRow => Boolean(u?.realName))
  },
  // 附件上传：走系统文件上传接口 /api/sysFile/uploadFile
  // 暂返回整个 SysFile 对象（res.data.result）；JfInitiateExtras 会 emit 成 attachment → form.f_attachment
  upload: async (file: File): Promise<SysFile> => {
    const res = await getAPI(SysFileApi).apiSysFileUploadFilePostForm(
      file, // file: Blob
      'jeeflow', // fileType 文件类别
      file.name, // fileAlias 文件别名
      undefined, // allowSuffix
      undefined, // dataId
      undefined, // userId
      true, // allowRepeat 允许重复上传同一文件
    )
    const result = res.data?.result
    if (!result) throw new Error('附件上传失败：未返回文件信息')
    return result
  },
}
