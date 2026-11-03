namespace SimpX.Net.Caching;

/// <summary>
/// 会员钱包缓存键。对齐旧 MoneyCaching 钱包快照用途（总余额/锁定）。
/// </summary>
public static class MoneyCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "money";

    /// <summary>会员钱包快照：标识为「用户编号:币种」。</summary>
    public const string UserWallet = "Money:UserWallet";

    /// <summary>会员洗码剩余：标识为「用户编号」（多币种以 CNY 口径对齐旧版）。</summary>
    public const string UserWashFlow = "Money:UserWashFlow";
}
