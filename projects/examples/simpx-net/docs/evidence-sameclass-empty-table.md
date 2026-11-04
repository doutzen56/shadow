# Evidence: 同类空表/英文列扫修（P0）

日期：2026-11-03

## 问题类

1. 列表 `catch` 静默返回空 → 失败看起来像「没数据」
2. 直接返回 `{ page, result }`（可被 hook 双兼容，但非 canonical）
3. 列头英文（如 UserName/UserID/Point）

## 已修页

| 页 | 改动 |
|----|------|
| `report/user/moneylog.vue` | `runAdminVxeQuery`；默认近7天；今日/近7天快捷 |
| `report/user/signin.vue` | `runAdminVxeQuery`；列中文 |
| `report/user/deposit123.vue` | `runAdminVxeQuery`；列中文 |
| `user/member/bank/index.vue` | `runAdminVxeQuery`（不再静默 `{page,result}`） |
| `user/member/kyc/index.vue` | 同上；`UserID`→会员编号 |
| `money/recharge/checkfail.vue` | `runAdminVxeQuery` |
| `money/withdraw/bank.vue` | `runAdminVxeQuery` |
| `money/operate/{rebate,commission,applylist,return,rescue}.vue` | `runAdminVxeQuery` + list 归一 |
| `money/recharge/setting.vue` / `money/withdraw/setting.vue` | `runAdminVxeQuery` |
| `user/member/{group,return}/index.vue` | `runAdminVxeQuery` |
| `user/member/import/{users,idcard,bank}.vue` | `runAdminVxeQuery` |
| `user/agent/{domain,invite,commission,return,check,gradesetting,usragentinfo}` | `runAdminVxeQuery` |
| `site/admin/{importmobile,eventimportusers}.vue` | `runAdminVxeQuery` |

此前已修：`money/recharge/order|check`、`money/withdraw/order`、共享 `vxe-multiline` CSS。

## 刻意未动

- 非 P0 生成壳（game/report/events/exchange/home/risk 等）仍静默 catch → 冲突表「冻结」
- `report/user/{balance,userlog,statistics}` 等壳页暂不动
- 会员列表查询已有弹错，未再改（形态已是 toVxeProxyPage）
- 站点管理员 list/log/group 壳页暂不动
