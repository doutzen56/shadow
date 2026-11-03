# Evidence: 款项目发放 / 会员详情结构化

日期：2026-11-03

## 变更
1. `money/operate/return|commission|rebate|rescue`：用户/状态筛选、`formatMoney2`、状态中文；返点/佣金通过拒绝；返水拒绝/重发（带 PlanID）。
2. BE：`returnplan/loglist` User/Status/Currency 筛选 + Status/Currency 枚举展示；`rescue` loglist Status/Currency 枚举；`rebate` loglist 回 `PlanID`。
3. `recharge/checkfail` 详情抽屉结构化（对齐充值订单）。
4. `user/agent/usragentinfo`、`user/agent/invite` Type 中文；会员列表详情/余额弹层结构化。
5. Host.Admin 已重编重启 :5005。
6. 充提通道详情抽屉结构化；银行 IsVir 筛分虚拟/电子/超商；银行/证件待审判断兼容 None。
7. 会员列表详情+多币种余额弹层结构化。

## 待拍板（未动）
电销、风控 delete 语义、渠道统计图、BankCountry 全量元数据、代理 RankItems。

8. 意见反馈：User 用户名筛选（BE 解析）+ 详情抽屉。
9. 会员日志/签到补「近7天」快捷；代理审核待审判断兼容 None。
10. 入款审核/加减款审核/比对失败默认今日+快捷；会员列表注册币种 formatCurrency。
10. 入款审核/加减款审核/比对失败默认今日+快捷；会员列表注册币种 formatCurrency。
11. 佣金/返点发放页补「生成」弹层（create/execute）；短信详情抽屉。
12. 救援金页补预览发放（query/post）勾选入账。
13. 风控 user/profit/game 今日·近7天；银行/证件/加减款失败筛选用旧拼写 Faild。
14. 签到列表 BE 补 StartAt/EndAt 过滤（FE 今日/近7天才真正生效）；Host 重编重启。
15. 签到页补手工补录（addsignlog）。
16. 返水发放详情表格化；加减款审核 Action→加款/扣款。
17. 会员日志 Platform 中文（电脑/手机/APP）。
18. 佣金详情 Detail JSON 结构化抽屉；佣金/返点/返水发放补币种筛选。
19. 救援金 loglist Currency 筛选（BE+FE）；Host 重编 :5005。
20. 会员详情锁定位 formatUserLockFlags；返水设定 Groups/Levels 显示名。
21. 充值/提现订单 Currency 筛选。
22. 会员帐变 Currency 筛选。
23. 银行列表 IsVir 类型列；救援金 Date 日筛选。
24. 代理域名今日/近7天；邀请码状态 Tag。
25. 入款审核 Currency 筛选（BE+FE）；层级用户名下钻；Host 重编。
26. 比对失败 Currency + 近7天。
27. 代理返点/返佣启用状态 Tag。
28. 银行/证件审核状态 Tag 色。
29. 返点/佣金/返水/救援金/加减款审核状态 Tag；代理审核 Tag。
30. 返水设定 BetMoney/MaxMoney formatMoney2。
31. 入款审核/提现订单状态 Tag。
32. 返水发放 Type FE 中文兜底。
33. 代理信息空字段显示「-」。
34. 充值通道列表 Sort + Min/Max formatMoney2。
35. 帐变「包含下级」提示文案；会员列表上级空值「-」。
36. 银行审核人/备注空值「-」；充提审核人空值。
37. 帐变上级/备注空值；救援金备注空值；加减款备注空值。
38. 反馈/代理域名空字段「-」。
39. 短信/站内信/IP黑名单空字段「-」。
40. 会员日志/签到空字段「-」。
41. 充提订单上级/分组/网关单号空值「-」。
42. 充提付款渠道空值；返水代理/计划空值。
43. 代理审核联系方式空值「-」+状态Tag；会员列表邀请人/分组/等级/姓名/IP空值；救援金活动空值。
44. 加减款方向 Tag；代理信息上级空值；提现详情补币种。
45. 银行/证件空字段「-」；风控用户 Agent/Level/Type 空值；风控游戏类型空值。
46. 返水设定自动发放 Tag；签到手工补录 Tag；提现银行卡号空值。
47. 加减款 Action 对齐 BE：1=加款/2=扣款；补审核时间+流水号列。
48. 加减款类型 formatMoneyTypeLabel；金额正负色。
49. 返水详情金额/备注格式化；会员分组创建时间 formatDateTime。
50. formatDateTime 过滤 0001/1900 哨兵日期；救援预览空值；加减款审核时间不再显示 MinValue。
51. 加减款 list 解析 Creator/Reviewer 管理员名；FE 展示提交人/审核人；Host 重编 :5005。
52. 余额汇总补「可用」=总余额-锁定；充值订单代客充值 Tag。
53. 银行 IsVir Tag；证件类型 Tag。
54. 救援预览盈亏正负色；邀请码账号类型 Tag。
55. 充值通道 list 回 Sort/Fee/Cost（BE+FE）；Host 重编 :5005。
56. 提现通道 list 回 Sort；FE 展示成本%+排序；Host 重编 :5005。
57. 充值通道列表补开放分组列。
58. 佣金详情游戏盈亏正负色。
59. 充值通道手续费/成本按百分比编辑与展示（库内小数费率 ×100）。
60. 返水发放列表补备注(FaildMsg/ClaimNo)。
61. 加减款审核 Type 筛选（BE 读 Type + FE moneytype 下拉）；Host 重编 :5005。
62. 返水 loglist 补 Currency/Type 真过滤（此前币种仅影响折算）；FE 类型下拉；Host 重编 :5005。
63. 会员加款类型下拉优先走 /admin/config/moneytype（失败回落硬编码）。
64. 会员扣款类型下拉优先 moneytype。
65. 返点发放列表回 PlanID；Host 重编 :5005。
66. 佣金 loglist data.Currency 去掉硬编码 TWD，单币种结果才回枚举名。
67. 返水发放补日期区间+今日/近7天；BE StartAt/EndAt 含截止日（EndAt+1天）。
68. 佣金/返点发放补日期区间+今日/近7天（BE StartAt/EndAt）。
69. 风控盈利/游戏盈亏正负色。
70. 提现订单金额红色；入款审核入款金额绿色。
71. 佣金/返点/返水发放金额绿色。
72. 救援金发放金额绿色。
73. 比对失败金额绿色（列表+详情）。
74. 会员列表总存款绿/总取款红。
75. 余额汇总总余额/可用绿、锁定红。
76. 提现通道编辑/详情补 Sort；BE savechannel/channelinfo 回写 Sort；Host 重编 :5005。
77. 发放页用户空值「-」。
78. 短信：今日/近7天+成功失败筛；IsSuccess Tag；BE 透传 StartAt/EndAt/IsSuccess。
79. 站内信：今日/近7天+已读筛；IsRead Tag；BE StartAt/EndAt；Host 重编 :5005。
80. 余额汇总未结算正负色。
81. 邀请码/代理域名用户名与域名空值「-」。
82. 提现订单手续费/行政费 >0 时红色。
83. 风控用户/盈亏/游戏/同IP同设备/黑名单导入空值「-」。
84. 会员分组名称/代理信息用户名空值「-」。
85. 返佣设定列表补最高比例 MaxRate。
86. 提现银行名称空值「-」。
87. 返佣 list 回 MaxRate；Host 重编 :5005。
88. 充提审/加减款/银行证件/代理审核/帐变/存款一二三/导入 用户名空值「-」；返点/返佣方案名空值。
89. 存款一二三金额绿色。
90. 意见反馈：今日/近7天；BE StartAt/EndAt；用户名改为 Contains；Host 重编 :5005。
91. 返佣设定列表补活动/返水承担列。
92. 返水设定有效流水/最高发放绿色；充值通道名称空值。
93. 代理审核详情抽屉；提现实际到帐红色。
94. 代理域名详情抽屉；充值订单详情实际到帐绿色；返水名称空值。
95. 邀请码详情抽屉；返佣列表去掉无后端字段的「默认方案」列。
96. 帐变锁定红/余额绿；会员分组排序空值。
97. 银行/证件列表补今日/近7天快捷。
98. 充值订单实充绿色；同IP/同设备次数空值兜底。
99. 加减款审核 60s 自动刷新；充值实充字色绿色。
100. 比对失败审核 60s 自动刷新。
101. 返点/佣金/返水/救援金发放页 60s 自动刷新。
102. IP 黑名单详情抽屉。
103. 会员分组人数下钻到会员列表（GroupID query）；列表挂载读 GroupID 并自动查询。
104. 会员日志平台 Tag；返佣代理数量刷新提示。
105. 会员分组默认/跳组布尔列改 Tag。
106. 会员日志 Platform 筛选+用户名 Contains；Host 重编 :5005。
107. 会员日志归属地 IPAddress 筛选；Host 重编 :5005。
108. 会员日志「仅最近」IsLast 筛选；Host 重编 :5005。
109. 会员列表补锁定列（formatUserLockFlags）。
110. 会员列表锁定列危险 Tag（有锁）/「无」。
111. 会员三导入页空值「-」；导入用户额度绿色。
112. 邀请码 IsOpen 筛选；Type 识别 Member/Agent；用户名 Contains；Host 重编 :5005。
113. 加减款审核 SourceID 流水号筛选；用户名 Contains；Host 重编 :5005。
114. 加减款审核补流水号 SourceID 查询输入框（BE 已筛）。
115. 代理层级 Type 筛选+Tag；列表传 Type。
116. 代理信息：Mode/ReturnMode/AgentName/Member/Invite/Domain/BlockGame 补齐；Status/Agent/BelongUser/日期筛；OfPage；Host 重编 :5005。
117. 加减款审核 Action 方向筛选（1加/2扣）；Host 重编 :5005。
118. 加减款审核补 PayName 充值渠道列（PayID→SitePayment）；Host 重编 :5005。
119. 充值/提现订单 User/Agent 改为 Contains 模糊；Host 重编 :5005。
120. 入款审核/比对失败 User Contains；迁移规则补模糊筛/加减款 SourceID·Action·PayName/代理信息补齐。
121. 帐变/银行/证件用户名 Contains；Host 重编 :5005。
122. 返水发放 loglist User/Agent Contains；Host 重编 :5005。
123. 风控用户/盈亏/游戏用户名 Contains；Host 重编 :5005。
124. 会员列表 Agent 上级 Contains（含 BelongUser 树）；Host 重编 :5005。
125. 救援金发放 loglist User Contains；Host 重编 :5005。
126. Host.Admin Database.json：主库/日志库 EnableInitDb/Table/Seed 改 false，避免每次启动 CodeFirst+种子 upsert；空库首次再临时开。
127. WebApi/SuperAdmin/Schedule/Channels Database.json 同步关闭 EnableInitDb/Table/Seed。
128. 风险会员 Type 列改 Tag。
129. 统一请求错误提示：__toastShown + showCaughtError；runAdminVxeQuery/拦截器/资金会员页 catch 去重，避免 502 中英双弹。
130. 提现通道 Cost：详情×100、编辑层百分比、保存÷100（对齐充值 Fee/Cost）。
131. 救援金发放补「今日」快捷筛选。
132. 充值订单补 60s 自动刷新（对齐旧站「自动刷新订单」）。
133. 代理审核：60s 自动刷新；Faild 状态展示「拒绝」。
134. 会员银行/证件审核补 60s 自动刷新。
135. axios-utils 请求拦截去掉重复 ElMessage.error(error)，只保留 showNetworkErrorThrottled。
136. request.ts 网络错误改走 showNetworkErrorThrottled；提现订单列表补前后端备注列。
137. 短信邮件补手机/邮箱 Mobile 筛选（BE 已支持 Contains）。
138. 会员列表补「今日」快捷时间筛选。
139. 风险会员：去掉 Status=0 硬编码；补风险因素筛选；提现银行 Type 空值。
140. 返水发放 FE 补上级代理 Agent 筛选框（对齐 BE Contains）。
141. 帐变币种空值兜底；金额色改 money-in/out。
142. 代理返佣方案 Award/Return/Cost/MaxRate：列表×100、编辑百分比、保存÷100（对齐旧 percent）。
143. 代理返佣列表补 Cost；BE ListItem 回 Cost；Host 重编 :5005 HOST_OK。
144. 代理/会员返水·返佣 Setting 游戏比例：编辑×100、保存÷100（对齐旧 percent）。
145. 存款一二三币种空值兜底。
146. 存款一二三补币种筛选（对齐 BE Currency）。
147. 返水发放 Agent 筛改为直属会员 UserID（非把代理自身当 User）；User/Agent 可同时生效；Host 重编 HOST_OK。
148. 代理层级设代理/锁定 catch 改 showCaughtError。
149. 代理域名/邀请/返佣方案静默 catch 改 showCaughtError。
150. 银行导入暂存/删除/入库 catch 去静默。
151. 会员返水/证件/分组删除 catch 去静默。
152. 手机/证件黑名单筛选项标签改为手机号码/证件号码。
153. 会员返水设定创建人空值兜底。
154. 提现订单补出款币种 FromCurrency 筛（BE+FE）；request2 网络错误改 showNetworkErrorThrottled。
155. 充值订单补入款币种 FromCurrency 筛（BE+FE）。
156. 返水发放补「生成返水」弹层（对齐佣金/返点 create；调 /admin/money/rebate/create）。
157. 充提订单补时间类型 DateType（申请时间 CreateAt / 到帐时间 PayAt，BE+FE）。
158. 比对失败 orderlistfail 接通 RechargeCheckFailService（原 stub「尚未接通」）；补 Currency/StartAt/EndAt 筛。
159. 比对失败币种空值兜底；Host 重编重启 :5005 HOST_OK。
160. 会员银行卡删除 `/admin/user/member/info/bankdelete` 接通 BankAccountService.DeleteAsync（原 stub）。
161. 比对失败补上级代理 Agent 筛（直属会员 AgentID，BE+FE）。
162. 加减款审核补上级代理 Agent 筛（直属会员 AgentID，BE+FE）。
163. 加减款审核补提交人/审核人筛（CreatorAdminName/ReviewerAdminName → SiteAdmin Contains）。
164. 加减款审核补「包含下级」BelongUser（对齐帐变/旧站；直属 AgentID 下级）。
165. 提现接口名称空值兜底。
166. 比对失败列表补到帐状态列 IsPay。
167. 会员列表修复 row_type / row_lock 插槽嵌套错误（#row_lock 误嵌在 #row_type 内）。
168. 会员详情抽屉等级/分组改显示名（LevelName/Group），避免裸 ID。
169. 比对失败补渠道名 PayName（SitePayment）；列表渠道列。
170. 入款审核补上级代理 Agent 筛（直属会员 AgentID，BE+FE）。
171. 代理审核补申请日期筛+今日/近7天；Status 兼容 None/Success/Faild 枚举名解析。
172. 帐变补流水号 SourceID 筛/列；BE 改 Contains 模糊。
173. 提现订单补审核时长/出款时长分钟筛（StartAudit/EndAudit/StartCash/EndCash，对齐旧站）。
174. 银行信息补币种筛+帐号 Contains；充值接口补币种筛+批量启用/停用。
175. 充值接口补名称筛+删除；提现接口补删除（对齐旧站）。
176. 证件号码筛改 Contains 模糊（对齐银行帐号）。
177. 充值接口补分类/状态筛；settinglist 回全字段（原仅 ID/Name）。
178. 提现接口列表补创建时间列。
179. 加扣款类型加载失败改 showCaughtError。
180. 提现接口补币种/启用前端筛。
181. 充提接口 meta 加载失败改 showCaughtError。
182. 返点/佣金发放用户名筛改 Contains（含 UserID）。
183. 充提接口列表启用改 Switch 快捷切换。
184. 挂通 /admin/recharge/updatepaymentstatus（列表 Switch）。
185. 会员返水设定启用/自动改 Switch。
186. 银行/返水/代理佣金 meta 加载 catch 去静默。
187. 发放/返水/代理返点 meta catch 再去静默。
188. 站内信/会员分组 meta catch 去静默。
189. 充提订单/存款一二三 meta catch 去静默。
190. 入款审核/比对失败 meta catch 去静默。
191. 会员列表添加总代/导出 catch 改 showCaughtError。

## 2026-11-03 cont137
- Withdraw Status filter: FE enum names + BE Enum.TryParse + Failed/Fail→Faild
- Invite IsOpen list Switch → save
- Risk detail catch → showCaughtError
- Return/Commission Status filter: TryParseCheckStatus
- Host.Admin rebuilt :5005 HOST_OK

## 2026-11-03 cont138
- Rebate/Return/Commission/Rescue Status FE → enum names
- Rescue Loglist Status TryParseCheckStatus
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont139
- MoneyLog Currency 支持枚举名
- Agent grade Type 筛 Member/Agent（BE+FE）
- Host.Admin rebuild :5005

## 2026-11-03 cont140
- 风险会员列表补 60s 自动刷新

## 2026-11-03 cont141
- checkfail IsPay filter BE+FE
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont142
- agent return/commission IsOpen list Switch
- invite Switch cast fix

## 2026-11-03 cont143
- checkfail AgentName/Count/PayAt + IsPay color slot
- agent return/commission IsOpen Switch
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont144
- rescue Date default today
- checkfail AgentName build fix (userAgentMap)

## 2026-11-03 cont145
- rebate/return/commission timeRange default today

## 2026-11-03 cont146
- fix userlog.vue adminPost object syntax (Platform/IP/IsLast were outside payload)

## 2026-11-03 cont147
- agent check timeRange default today

## 2026-11-03 cont148
- moneylog meta catch → showCaughtError

## 2026-11-03 cont149
- AgentDomain Invite/Domain Contains
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont150
- invite code search Contains
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont151
- member list InviterName Contains (+ int InviteID)
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont152
- IP black list keyword User/Name/Remark/IP filter BE
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont153
- customer message/sms timeRange default today

## 2026-11-03 cont154
- recharge order KeyWord OrderID/SystemID Contains BE+FE
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont155
- withdraw order SystemID Contains BE+FE
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont156
- bank recharge check OrderID Contains
- Host.Admin :5005 HOST_OK

## 2026-11-03 cont157
- checkfail OrderID Contains
- Host.Admin :5005 HOST_OK
