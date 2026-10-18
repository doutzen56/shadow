# 渠道与第三方集成

project_id: hx-matching
summary_group: channels-integrations
host_or_layer: external-gateway
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 171
project_count: 1
risk_tags: [gateway, funds, callback, retry]

## 职责摘要

渠道适配、第三方网关、回调和通道相关扩展能力。

## 扫描根目录

- HX.Matching.Channels

## 项目文件

- HX.Matching.Channels/HX.Matching.Channels.csproj

## 定位样例

- HX.Matching.Channels/ChannelCoreFactory.cs
- HX.Matching.Channels/ChannelCoreServiceCollectionExtensions.cs
- HX.Matching.Channels/ChannelModels/Common/BaWangCommonModels.cs
- HX.Matching.Channels/ChannelModels/Common/FaCaiCommonModels.cs
- HX.Matching.Channels/ChannelModels/Common/GuoWangCommonModels.cs
- HX.Matching.Channels/ChannelModels/Common/HeXingCommonModels.cs
- HX.Matching.Channels/ChannelModels/Common/JinZuanCommonResponse.cs
- HX.Matching.Channels/ChannelModels/Common/JuZhuanCommonResponse.cs
- HX.Matching.Channels/ChannelModels/Common/XingShengCommonModels.cs
- HX.Matching.Channels/ChannelModels/Common/YiMaCommonModels.cs
- HX.Matching.Channels/ChannelModels/Recharge/BaWangRechargeModels.cs
- HX.Matching.Channels/ChannelModels/Recharge/DfPayRechargeModels.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。
