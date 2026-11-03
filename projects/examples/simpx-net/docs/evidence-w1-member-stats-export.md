# Evidence: W1 会员列表充提合计 + Form 导出

日期：2026-11-03

## 做了什么

1. **TotalRechargeMoney / TotalWithdrawMoney**
   - DTO：`MemberListItemDto` 增加两字段（JSON 名对齐旧站）。
   - `UserMemberService.PageAsync`：对本页 `UserID` 查 `rpt_UserDateMoney`，按类型汇总：
     - 充：`1, 111, 9, 44`
     - 提：`2, 11, 10`
   - 本波**不做汇率折算**（旧站有 FX）；冲突表已记。

2. **Form 导出**
   - `GET/POST /admin/user/member/info/export` 与 `/admin/user/member/export`
   - `ExportForOldApiAsync`：与 list 同筛选校验；`PageSize=5000`；`info.list` 为行。
   - JWT `Export` 仍走 Shared `ExportAsync`（不变）。

3. **前端**
   - `exportMemberList` → Form export
   - 列表页「导出(CSV)」：`site:user:member:list:export`；BOM CSV 本地下载。

## 构建

- `SimpX.Net.Application.Shared`：0 error
- `SimpX.Net.Application.Admin`：见同波 build 结果

## 同波续作（已完成）

- 代理返点弹层、层级 save（UserName/ParentName）
- `bank/reset` 撤回审核 + 前端按钮
- 分组充值渠道勾选（settinglist→Payment CSV）
- 返水 `updateenabled` / `updateauto` + 列表点击切换
- 会员三导入 + 号码/活动导入（CSV/文本暂存与申请）

## 仍 deferred

- 导出谷歌码弹层
- `batchaddmark` Excel Url 解析
- 充提合计汇率折算
- 返水游戏列/自助规则；批量改分组；电销页
