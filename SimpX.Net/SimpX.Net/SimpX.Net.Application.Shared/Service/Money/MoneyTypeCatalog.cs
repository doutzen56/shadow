namespace SimpX.Net.Application.Shared;

/// <summary>帐变类型分类树（对齐旧站 moneytype 抽检）。</summary>
public static class MoneyTypeCatalog
{
    /// <summary>分类 →（枚举名 → 中文）。</summary>
    public static Dictionary<string, Dictionary<string, string>> BuildTree() => new(StringComparer.Ordinal)
    {
        ["手动加减款"] = new()
        {
            ["RebateAdd"] = "手动返水",
            ["Event"] = "手动活动",
            ["AdminIn"] = "手工加款",
            ["AdminOut"] = "手工减款"
        },
        ["充值"] = new()
        {
            ["Recharge"] = "充值",
            ["RechargeDue"] = "充值扣除",
            ["RechargeDueReverse"] = "充值扣点补回",
            ["ProxyRecharge"] = "代客充值"
        },
        ["提现"] = new()
        {
            ["Withdraw"] = "提现",
            ["WithdrawReturn"] = "提现退回",
            ["WithdrawMoney"] = "提现扣除"
        },
        ["手续费"] = new()
        {
            ["WithdrawFee"] = "提现手续费",
            ["RechargeFee"] = "充值手续费",
            ["WithdrawFeeBack"] = "提现手续费补回",
            ["RechargeFeeBack"] = "储值手续费补回"
        },
        ["游戏"] = new()
        {
            ["GameTransferOut"] = "游戏转出",
            ["GameTransferIn"] = "转入游戏",
            ["AVIABet"] = "投注",
            ["AVIAReward"] = "奖金",
            ["AVIARevoke"] = "电竞退单",
            ["GameReturn"] = "退回投注",
            ["GameBet"] = "投注扣除",
            ["GameExcetionIn"] = "游戏异常加点",
            ["GameExcetion"] = "游戏异常扣点"
        },
        ["投注"] = new() { ["AVIABet"] = "投注" },
        ["奖金"] = new() { ["AVIAReward"] = "奖金" },
        ["佣金"] = new()
        {
            ["Commission"] = "代理返佣",
            ["CommissionFee"] = "佣金扣除"
        },
        ["返水"] = new()
        {
            ["Rebate"] = "返水",
            ["RebateFee"] = "返水扣除"
        },
        ["转账"] = new()
        {
            ["TransferIn"] = "转入",
            ["TransferOut"] = "转出",
            ["ProxyIn"] = "转账上分",
            ["ProxyOut"] = "转账下分"
        },
        ["退单"] = new() { ["AVIARevoke"] = "电竞退单" },
        ["活动"] = new()
        {
            ["RechargeReward"] = "充值奖励",
            ["VipEventIn"] = "VIP活动加点",
            ["PlatEventIn"] = "平台赠品上分",
            ["VipEventOut"] = "VIP活动扣点",
            ["PointIn"] = "积分兑换",
            ["VoucherIn"] = "兑换码兑换",
            ["EventRegister"] = "注册礼金",
            ["NewDailyTasks"] = "新版每日任务",
            ["NewWeekTasks"] = "新版每周任务",
            ["EventMobile"] = "手机验证",
            ["EventRescue"] = "救援金",
            ["EventCard"] = "绑定银行卡",
            ["EventPlatform"] = "擂台奖",
            ["EventReward"] = "奖上奖",
            ["EventRecharge"] = "充值优惠",
            ["EventUserInvite"] = "全民代理",
            ["EventDelottery"] = "幸运转盘",
            ["EventInviteReward"] = "邀请有礼",
            ["EventLongCheckIn"] = "长期签到",
            ["EventDailyTasks"] = "每日任务",
            ["EventWeekTasks"] = "每周任务",
            ["EventWithdraw"] = "提款优惠",
            ["EventWashing"] = "洗码优惠",
            ["EventFirstRecharge"] = "首充优惠",
            ["RechargeByLevel"] = "存款优惠(阶梯)",
            ["EventBindData"] = "绑定资料",
            ["EventRedEnvelope"] = "红包活动",
            ["Survery"] = "问卷调查",
            ["InviteTask"] = "邀请任务",
            ["PlatEventOut"] = "平台赠品扣回",
            ["EventFee"] = "活动扣除",
            ["EventQA"] = "调查问卷",
            ["AgentIn"] = "代理自辦加点",
            ["AgentOut"] = "代理自辦扣点"
        },
        ["返点"] = new()
        {
            ["AgentReturn"] = "代理返点",
            ["AgentReturnFee"] = "代理返点扣除"
        },
        ["兑换"] = new()
        {
            ["ExchangeIn"] = "兑入",
            ["ExchangeOut"] = "兑出"
        },
        ["特权"] = new()
        {
            ["Deposit"] = "存款优惠",
            ["Upgrade"] = "晋级礼金",
            ["EveryMonth"] = "每月礼金",
            ["Birthday"] = "生日礼金",
            ["PriRebate"] = "返水优惠",
            ["EveryWeek"] = "每周礼金"
        },
        ["客服打赏"] = new()
        {
            ["RewardIn"] = "客服打赏补回",
            ["RewardOut"] = "客服打赏扣除"
        },
        ["套利"] = new()
        {
            ["ArbitrageIn"] = "套利补回",
            ["ArbitrageOut"] = "套利扣除"
        }
    };

}
