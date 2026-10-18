# 渠道与第三方集成

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `channels-integrations`
- host_or_layer: `external-gateway`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T083109Z-project-hx-matching`
- file_count: 172
- project_count: 1
- risk_tags: `callback`, `funds`, `gateway`, `order-state`, `retry`

## 职责摘要

渠道适配、第三方网关、回调和通道相关扩展能力。

## 扫描根目录

- `HX.Matching.Channels`

## 项目文件

- `HX.Matching.Channels/HX.Matching.Channels.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Channels/ChannelModels/Recharge/BaWangRechargeModels.cs`：符号 `BaWangPayInRechargeOrderResponse, BaWangPayeeInfo, BaWangPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/DfPayRechargeModels.cs`：符号 `DfPayApiResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/DfPayVoucherMeta.cs`：符号 `DfPayVoucherMeta, ToJson, TryParse, ParseLegacy, FirstNonEmpty`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/DingLiuRechargeModels.cs`：符号 `DingLiuApiResponse, DingLiuPayInOrderData, DingLiuPayInQueryData, DingLiuBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/FaCaiRechargeModels.cs`：符号 `FaCaiPayInRechargeOrderResponse, FaCaiPayInRechargeQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/FastRechargeModels.cs`：符号 `FastPayInOrderResponse, FastPayData, FastPayInQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/GaoShengRechargeModels.cs`：符号 `GaoShengApiResponse, GaoShengOrderData, GaoShengQueryData, GaoShengBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/GuanYiRechargeModels.cs`：符号 `GuanYiPayInOrderResponse, GuanYiPayParams, GuanYiPayInQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/GuoWangRechargeModels.cs`：符号 `GuoWangPayInRechargeOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HaiShenRechargeModels.cs`：符号 `HaiShenApiResponse, HaiShenOrderData, HaiShenQueryData, HaiShenBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HaoNanRechargeModels.cs`：符号 `HaoNanApiResponse, HaoNanOrderData, HaoNanQueryData, HaoNanBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HeXingRechargeModels.cs`：符号 `HeXingPayInRechargeOrderResponse, HeXingPayeeInfo, HeXingPayInRechargeQueryOrderResponse, HeXingRechargeResponse, HeXingCollectOrderData, HeXingCollectQueryData, HeXingCollectBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HehuaRechargeModels.cs`：符号 `HeHuaInRechargeOrderResponse, HeHuaInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HongNiuRechargeModels.cs`：符号 `HongNiuPayResponse, HongNiuPayData, HongNiuPayUrlBlock, HongNiuMerchantAccountResponse, HongNiuMerchantAccountData, HongNiuAccountBlock`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/HongShanRechargeModels.cs`：符号 `HongShanApiResponse, HongShanOrderData, HongShanPayExData, HongShanQueryData, HongShanBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JiGuangRechargeModels.cs`：符号 `JiGuangPayResponse, JiGuangPayData, JiGuangPayUrlBlock, JiGuangMerchantAccountResponse, JiGuangMerchantAccountData, JiGuangAccountBlock`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JiXiangPayModels.cs`：符号 `JiXiangPayCallback`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JinHuangRechargeModels.cs`：符号 `JinHuangApiResponse, JinHuangPayInOrderData, JinHuangPayInQueryData, JinHuangBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JinZuanRechargeModels.cs`：符号 `JinZuanPayInRechargeOrderResponse, JinZuanPayeeInfo, JinZuanPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JuXianRechargeModel.cs`：符号 `JuXianPayInRecharegeResponseBase, JuXianPayInRechargeOrderResponse, JuXianPayeeInfo, JuXianPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/JuZhuanRechargeModels.cs`：符号 `JuZhuanPayInRechargeOrderResponse, JuZhuanPayeeInfo, JuZhuanPayInRechargeQueryOrderResponse, JuZhuanBalanceResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/KoiRechargeModels.cs`：符号 `KoiOrderResponse, KoiOrderPayload, KoiQueryResponse, KoiQueryData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/MaYiFuRechargeModels.cs`：符号 `MaYiFuPayInOrderResponse, MaYiFuPayParams, MaYiFuPayInQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/MingXinRechargeModels.cs`：符号 `MingXinResponseBase, MingXinPayInCreateData, MingXinOrderQueryData, MingXinBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/NeoPayRechargeModels.cs`：符号 `NeoPayRechargeModels, NeoPayApiResponse, NeoPayOrderData, NeoPayReceivingCode, NeoPayQueryData, NeoPayBalanceData, NeoPayCashierSession, NeoPayCashierOrder`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/OrionRechargeModels.cs`：符号 `OrionOrderResponse, OrionOrderData, OrionQueryResponse, OrionBalanceResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/RiShengRechargeModels.cs`：符号 `RiShengApiResponse, RiShengOrderData, RiShengQueryData, RiShengBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/SiHaiRechargeModels.cs`：符号 `SiHaiResponse, SiHaiOrderData, SiHaiQueryData, SiHaiPayOrderData, SiHaiPageSetting, SiHaiMoney, SiHaiPayOrderExtend, SiHaiAccountItem`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/TaiXinRechargeModels.cs`：符号 `TaiXinResponse, TaiXinPayInRechargeOrderResponse, TaiXinPayeeInfo, TaiXinPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/TestHehuaRechargeModels.cs`：符号 `TestHehuaPayCreateResponse, TestHehuaPayCreateData, TestHehuaPayUrlBlock`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/UidRechargeModels.cs`：符号 `UidPayInRechargeResponseBase, UidPayInRechargeOrderResponse, UidPayeeInfo, UidPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/WanHeRechargeModels.cs`：符号 `WanHeApiResponse, WanHeOrderData, WanHeQueryData, WanHeBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/WangCaiRechargeModels.cs`：符号 `WangCaiApiResponse, WangCaiOrderData, WangCaiQueryData, WangCaiBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/XiaoMaoRechargeModels.cs`：符号 `XiaoMaoOrderResponse, XiaoMaoQueryResponse, XiaoMaoBalanceResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/XinJuXianRechargeModels.cs`：符号 `XinJuXianResponseBase, XinJuXianPayInCreateData, XinJuXianOrderQueryData, XinJuXianBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/XingShengRechargeModels.cs`：符号 `XingShengPayInRechargeOrderResponse, XingShengeeInfo, XingShengPayInRechargeQueryOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/YiMaRechargeModels.cs`：符号 `YiMaPayInRechargeOrderResponse, YiMaPayInRechargeQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Recharge/YinHeRechargeModels.cs`：符号 `YinHeResponse, YinHePayInRechargeOrderResponse, YinHeCardInfo, YinHePayInRechargeQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/AKWithdrawModels.cs`：符号 `AKWithdrawModels, AKWithdrawRequestBase, AKWithdrawResponseBase, AKWithdrawSubmitRequest, AKWithdrawQueryOrderRequest, AKWithdrawSubmitResponse, AKWithdrawQueryOrderResponse, AKWithdrawQueryBalanceRequest`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/FuYuanWithdrawModels.cs`：符号 `FuYuanWithdrawResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/GuoWangWithdrawModels.cs`：符号 `GuoWangWithdrawOrderResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/HeHuaWithdrawModels.cs`：符号 `HeHuaWithdrawResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/HeXingWithdrawModels.cs`：符号 `HeXingWithdrawResponse, HeXingWithdrawOrderRequest, HeXingWithdrawOrderData, HeXingWithdrawQueryRequest, HeXingWithdrawQueryData, HeXingWithdrawBalanceRequest, HeXingWithdrawBalanceData`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/HongNiuWithdrawModels.cs`：符号 `HongNiuWithdrawApiResponse, HongNiuTransferData, HongNiuTransferQueryData, HongNiuWithdrawAccountBalance`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/JuXianWithdrawModels.cs`：符号 `JuXianWithdrawResponseBase, JuXianWithdrawOrderSubmitResponse, JuXianWithdrawOrderQueryResponse, JuXianWithdrawBalanceQueryResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/ChannelModels/Withdraw/KuaiMaWithdrawModels.cs`：符号 `KuaiMaWithdrawResponse`；风险 `callback, funds, gateway, retry`
- `HX.Matching.Channels/Contracts/Recharge/ChannelRechargeOrderResponse.cs`：符号 `ChannelRechargeOrderResponse`；风险 `callback, funds, gateway, order-state, retry`
- `HX.Matching.Channels/Contracts/RechargeOrderQueryResult.cs`：符号 `RechargeOrderQueryResult`；风险 `callback, funds, gateway, order-state, retry`
- `HX.Matching.Channels/Contracts/Withdraw/ChannelWithdrawOrderResponse.cs`：符号 `ChannelWithdrawOrderResponse`；风险 `callback, funds, gateway, order-state, retry`
- `HX.Matching.Channels/Contracts/WithdrawOrderQueryResult.cs`：符号 `WithdrawOrderQueryResult`；风险 `callback, funds, gateway, order-state, retry`
- 还有 122 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 2 项；明细只写入 `rag/init-reports/20261018T083109Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
