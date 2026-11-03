# Admin 前端迁移：冲突登记

> 配套：`admin-frontend-migration.md`  
> 规则：新旧行为不一致、接口缺口、参数不对 → **先登记，不自行取舍**；需要用户拍板的标 `待拍板`。

| 登记日 | 页/接口 | 现象 | 旧站依据（路径） | 建议 | 状态 |
|--------|---------|------|------------------|------|------|
| 2026-11-03 | OldApiJson 包装 | 每接口手包 `OldApiJson.Result` 重复且易漏；已删，改由 `OldApiAuth`→`OldApiContract` 统一按声明名序列化，方法直接返回 `OldApiResult` | OldApi 契约 | Host.Admin 重载后生效 | 已修 |
| 2026-11-03 | 会员列表空表 | 根因：`useVxeTable` 只认 `data.result.items`，业务页返回 `{page,result}` → 空表；已改 hook 双兼容 + `toVxeProxyPage` | vxe proxy response | 刷新页面后查 `dl111` / 全体会员 | 已修 |
| 2026-11-03 | 会员列表下拉 | 已挂 `commission/list0`、`returnplan/list0`、`vip/getlevels`、`currency/showreport`、`getsitetype`（Type=1 显注册币种） | `user/member/list.html` 下拉 init | — | 已补 |
| 2026-11-03 | 会员列表导出/导入备注/添加总代 | 添加总代 + 备注导入(ExcelJS 解 xlsx/xls + CSV)→`batchaddmark` Items；Form `info/export`→前端 CSV。旧 Url 下载体后端仍可后补；谷歌码非 list 弹层（详情字段） | list.html toolbar | Url 服务端解析可选 | 已修(主路径) |
| 2026-11-03 | 总存款/总取款列 | list 已从 `rpt_UserDateMoney` 汇总 TotalRechargeMoney/TotalWithdrawMoney（本波未做汇率折算） | list.html cols | 汇率折算后补 | 已补 |
| 2026-11-03 | 生产 nginx | 无 `/admin` location，只有 `/admin-api/` | docker/nginx | 上线前补反代 | 待补 |
| 2026-11-03 | 银行卡号/游戏帐号/设备号/KYC 搜索 | SearchType：`Account`→BankAccount、`GameUser`→game_User.PlayerName、`DeviceId`→User.DeviceId、`KYCId`→UserIdCard.CardNumber（各限 2000） | list.html SearchType | 场景验收 | 已修(主路径) |
| 2026-11-03 | 会员分组 | save/delete/主字段/提现全字段/自动跳组 Condition/渠道勾选/批量改分组已接 | `group.html` / `group-info.html` | — | 已修(主路径) |
| 2026-11-03 | 银行信息 | 列表+审核/撤回(reset)/添加已接；IsVir 筛选；bankcountrylist；Status 枚举名；添加弹层：凭证上传(`/admin/system/upload`)、银行/虚拟/电子/超商 Tab；超商 city→district→store 级联；Create 支持 IsVir=4；电子钱包 IsVir=2 国家下拉+类型级联（bankcountryinfo list[].country；元数据未全量前 BE 回「默认」） | `bank.html` / `bank-create.html` | BankCountry 全量元数据后补；场景验收 | 已修(主路径) |
| 2026-11-03 | 证件信息 | 列表+审核+添加已接：IDType/GenderType、国家/城市/日期、正反面上传；审核详情含城市/日期/性别/国家/正反面预览 | `kyc.html` / `kyc-create.html` / `kyc-info.html` | CardFrontImg CDN 映射可选 | 已修(主路径) |
| 2026-11-03 | 返水设定 | 列表 GroupID 筛选已接；编辑：returntype+分组/等级多选；SettingString 键值%网格（与原文互相同步）；自助规则弹层已接 `getrule`/`savereturnrule`→`AutoReturnRule`；厂商 GameGroups 已接（info 回 GameGroups/Games；FE 按大类 fieldset 填 %） | `return.html` / `return-info.html` | 代理侧厂商图后补 | 已修(主路径) |
| 2026-11-03 | 银行/证件 Status | 旧站传枚举名 `None`；已加 `TryParseCheckStatus` 兼容名与整型 | bank/kyc list | Host.Admin 需重载后生效 | 已补代码 |
| 2026-11-03 | 代理弹层 | 域名/邀请/返佣已接；返点方案添加编辑、层级新增/设代理/锁定已接；游戏返点/返佣已接 game/info/get 厂商×分类网格（Type_Category 键） | user/agent/* | 阶梯 RankItems 完整 UI 后补 | 部分 |
| 2026-11-03 | 电销系统 | 菜单有，旧站/后端能力未清 | customer/tel | 核对旧站后再做 | 待拍板 |
| 2026-11-03 | 充提订单列 | 充值：近7天、多行、AdminName、DealTime、详情/CSV/代客建单。提现：上级/分组/渠道/审核出款人/时长/行政费/金额筛选/自动刷新；处理提现 checkorder/execorder 已接。充值渠道统计=报表图 `ADMIN_RECHARGE_PAYMENT` 未迁（弹层诚实占位）。提现银行/虚拟币勾选页已接；充值 settingcate/settingsys/savesetting、提现 channelinfo/savechannel/channelsys SysID 下拉已接 | order.html / bank.html / setting.html | 渠道统计图后补 | 部分 |
| 2026-11-03 | 同类空表/英文列 | 静默 catch→空表、`{page,result}` 直返、英文列头：已扫 P0 资金+会员银行/证件+帐变/签到/存款一二三/余额汇总/会员日志，统一 `runAdminVxeQuery`+中文列；非 P0 图表壳（agent/statistics、chart/report、site/*、user/statistics）已改诚实「未验收」空态 | 对照 recharge/order | 非 P0 解冻时再扫 | 已修(P0) |
| 2026-11-03 | 报表 info 包装 | loginlog/userlog/pointlog/balance 曾直吐 `Data/Items/List` 无 `list/RecordCount`；已改 `OfPage`/`OfList` | report/user/* | Host 重载 | 已修 |
| 2026-11-03 | 展示层枚举/缩写 | 币种缺表项吐整型(17≠USDT / 实充`47.50 2`)；费用「费：a/b」；Count 裸数字。**主档 §11**：`Data.Currency`+`OldApiEnumDisplay`；`FormatMoneyCurrency`/`NormalizeCurrencySuffix`；FE `getcurrencyinfo`+查询前 await；提现 `WithdrawStatus` 枚举；费用分列；次数「第N次」 | order.html tip | 新页强制 §11 | 已修(P0+同类) |
| 2026-11-03 | 充值 Remark 管道 | 单一钱包入账写 `备注\|备注\|实充 币种`，空备注变 `\|\|5975.00 TWD`；详情禁止原样展示。约定：`ParseRechargeRemark`/`formatRechargeRemark` 只出人工备注，实充走 RealPaid | orderinfo Remark | 详情备注走解析 | 已修 |
| 2026-11-03 | 入款审核 | 列表列对齐旧站（金额叠显/银行账户/状态色）；筛选渠道/状态/时间；60s 自动刷新；通过/拒绝/挂起已接；声音提醒：开关 + `/admin/site/admin/sound` 的 Transfer（无配置则蜂鸣）；同批待审去重 | check.html | 场景验收资金后果；全局角标通知后补 | 已修(主路径) |
| 2026-11-03 | 会员导入三页 | 注册/证件/银行：ExcelJS+CSV 暂存+申请入库已接（共用 `admin-import-file`）；证件导入正反面上传+列表列+编辑已接（`/admin/system/upload`→saveidcard CardFront/Back） | import* | — | 已修(主路径) |
| 2026-11-03 | 证件黑名单 | 已改 `importbalckkyclist`；中文列 BlackKyc；添加/Excel 导入→`addbalckkyc`；删除/批量删除 | importblackkyc.html | createbalckkyc 旧「申请入库」仍为占位文案 | 已修(主路径) |
| 2026-11-03 | 加减款/发放 | 加/扣款主表单+审核列表已齐；文本批量/Excel 已接；返点/佣金/返水/救援金 loglist 筛选审核；佣金/返点「生成」；救援金预览发放 query/post | operate/* | 可场景验收 | 已修(主路径) |
| 2026-11-03 | 风控列字段 | IP 黑名单增删改+Excel；国别勾选；同IP/同设备：User|IP|Device+日期（BE 已传 StartAt/EndAt）；控制 user/profit/game 列与筛选对齐；`risk/user/updatestatus` 已挂（按 RiskID 删除移出列表）；list/profit/game 改 OfList；明细抽屉接 info/ip/ipdifuser/userdifip/login/amount，结构化展示（IP对比/改密次数） | risk/* | 处理语义若旧站非删除需拍板 | 部分 |
| 2026-11-03 | 导入/电销占位 | 会员三导入+手机/证件黑名单导入已接；电销仍占位 | customer/tel | 电销后补 | 部分 |
| 2026-11-03 | 批量生成页 API | report/game/events/exchange/site/home/shop/proxy 等猜路径壳；**不算交付**；本波已批量改诚实「非 P0，暂未验收」空态（约 57 页） | 多页 | 纠偏：冻结非 P0，见 reset 档 | 冻结 |
| 2026-11-03 | 迁移策略 | 横向铺壳导致无可演示页 | — | 改 P0 竖切 + 共用壳 | 执行中 |
| 2026-11-03 | 会员帐变/签到/存款一二三 | 帐变：今日默认、包含下级、备注、分组下拉（`/admin/config/moneytype`+本地目录兜底，分类「全部」=多 Type CSV）；TypeValue 中文；签到/存款列中文；`runAdminVxeQuery`；存款一二三已 `OfList`+`BelongUser` 直属下级；`/admin/config/moneytype` 已回完整分类树（MoneyTypeCatalog） | moneylog.html | 场景验收 | 已修(主路径) |

已拍板总表见主档 §7（C1～C6）。

