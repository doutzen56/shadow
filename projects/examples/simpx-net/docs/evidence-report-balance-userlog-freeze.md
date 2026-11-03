# Evidence: 余额汇总 / 会员日志 / 非 P0 报表冻结

日期：2026-11-03

## 做了什么

1. **余额汇总** `report/user/balance.vue`
   - UTF-8 重写；列对齐 `WalletBalanceItemDto`：币种 / 总余额 / 锁定 / 未结算 / 人数
   - `runAdminVxeQuery` + `ensureCurrencyEnum`
   - BE：`Balance()` 改为 `OldApiShowResult.OfList`

2. **会员日志** `report/user/userlog.vue`
   - 中文列（登录时间/IP/归属地/设备/域名/平台）+ `runAdminVxeQuery`
   - BE：`Loginlog()`（userlog/loginlog 同入口）改为 `OfPage(Data, Total, …)`

3. **积分日志包装**：`Pointlog()` → `OfPage(Items, Total, page, size)`

4. **非 P0 图表壳**改为诚实空态「非 P0，暂未验收」：
   - `agent/statistics`、`chart/report`
   - `site/{deposit,profit,rechargeandwithdraw,rechargeusers}`
   - `user/statistics`

5. **代理返佣** `user/agent/commission`：补 SettingString 键值%网格（与返点/返水同模式）

6. **银行电子钱包** IsVir=2：`BankCountryInfo` 返回 200/7007/8061；FE 类型下拉

7. **风控** `risk/user/updatestatus`：按 RiskID 删除移出列表；list/profit/game `OfList`

8. **自助反水** `savereturnrule`→`AutoReturnRule`；返水页弹层可读写

9. **非 P0 生成壳**（game/events/exchange/home/shop/site/proxy 等约 57 页）批量诚实空态

10. **存款一二三** `BelongUser` 直属下级；客服站内信发送弹层

11. **提现银行** getbanklist + savesitebank/savesitecoin（银行+虚拟币 Tab）；**充值/提现通道** settinginfo/savesetting、channelinfo/savechannel 编辑弹层

12. **风控国别** 回读兼容 `Regions`；返水自助规则函数补齐

13. **充值通道** settingcate / settingsys 下拉 + savesetting

14. **提现通道** `/admin/money/withdraw/channelsys`（SystemWithdraw IsOpen）+ setting.vue SysID 下拉

15. **返水设定 GameGroups**：BE `RebateSettingDetailDto.GameGroups/Games`；FE 按厂商大类填比例

## 验收

- Host.Admin 已重编重启（:5005）
- 打开余额汇总应见按币种汇总行（需登录态）
- 打开冻结图表页应见空态文案，不再静默空表冒充列表

16. **风控明细抽屉** 结构化展示 BindInfo/IP对比/改密次数（对齐旧站 tab 文案）

17. **代理返点/返佣** 编辑弹层挂 `/admin/game/info/get` 厂商×分类比例网格（键 Type_Category）

18. **帐变 moneytype**：`SiteConfigService.MoneyTypeAsync` 改为回 `MoneyTypeCatalog` 完整分类树（不再吐站点勾选扁平键）

19. **充提通道币种**：setting 弹层 Currency 改为 fetchCurrencyOptions 下拉（禁裸数字）

20. **银行电子钱包**：国家/地区下拉 + 类型级联；BE IsVir=2 回 country=默认

21. **站内信发送**：User 用户名 CSV + Groups 分组；详情抽屉结构化

22. **站内信列表**：回填 UserName；支持 User/Title 筛选

23. **充值通道列表** enrich SysName（SystemPayment）；FE 系统渠道列

24. **返水列表**：Groups CSV→分组名；类型列中文（returntype）

25. **签到/会员日志**：默认今日时间段 + 今日快捷按钮；提现银行名称筛选

26. **意见反馈** `user/customer/feedback/index.vue` 列表+删除（Content/Mobile/Email 筛）
