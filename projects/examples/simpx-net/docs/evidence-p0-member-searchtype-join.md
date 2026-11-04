# Evidence: 会员列表 SearchType 关联查询

日期：2026-11-03

## 对照旧站 `list.html` SearchType

| 值 | 含义 | 实现 |
|----|------|------|
| Account | 银行卡号 | `usr_BankAccount.Account` Contains → UserID |
| GameUser | 游戏帐号 | `game_User.PlayerName` Contains → UserID |
| DeviceId | 最近登录设备号 | `User.DeviceId` Contains |
| KYCId | 证件号码 | `usr_IdCard.CardNumber` Contains → UserID |

关联查询 Distinct + Take(2000) 防爆。

## 文件

- `UserMemberService.cs` Page 筛选 switch
