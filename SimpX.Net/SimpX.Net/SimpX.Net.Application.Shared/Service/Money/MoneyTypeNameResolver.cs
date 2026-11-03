using System.Globalization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 把 Excel/表单里的资金类型（整型或中文名）解析成 MoneyType 数值。
/// 名称对齐旧版 MoneyLog.MoneyType 的 Description（可带或不带 #）。
/// </summary>
public static class MoneyTypeNameResolver
{
    /// <summary>旧版「充值」类型；会员加款禁止走此类型，应走代客充值流程。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>旧版「代客充值」类型。</summary>
    public const int MoneyTypeProxyRecharge = 111;

    private static readonly Dictionary<string, int> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["充值"] = MoneyTypeRecharge,
            ["代客充值"] = MoneyTypeProxyRecharge,
            ["提现"] = 2,
            ["充值奖励"] = 3,
            ["返水"] = 60,
            ["手动返水"] = 61,
            ["返水扣除"] = 62,
            ["代理返佣"] = 130,
            ["代理返点"] = 131,
            ["佣金扣除"] = 132,
            ["代理返点扣除"] = 133,
            ["手工加款"] = 250,
            ["手工减款"] = 251,
            ["手动活动"] = 200,
            ["活动扣除"] = 230,
            ["提现退回"] = 10,
            ["提现扣除"] = 11,
            ["充值扣除"] = 9,
            ["转入"] = 120,
            ["转出"] = 121,
            ["兑入"] = 150,
            ["兑出"] = 151,
            ["提现手续费"] = 4,
            ["游戏转出"] = 5,
            ["充值手续费"] = 6,
            ["转入游戏"] = 8,
            ["管理员加款"] = 250,
            ["管理员减款"] = 251
        };

    /// <summary>
    /// 已登记的资金类型整型集合（含名称对照与常见旧枚举值）。
    /// 加扣款提交前用 <see cref="IsKnownType"/> 校验，避免未知 Type 静默入账。
    /// </summary>
    private static readonly HashSet<int> KnownTypes = BuildKnownTypes();

    /// <summary>
    /// 汇总名称对照与常见旧 MoneyType 数值。
    /// </summary>
    /// <returns>已知类型集合</returns>
    private static HashSet<int> BuildKnownTypes()
    {
        var set = new HashSet<int>(ByName.Values);
        // 旧枚举里常用、名称表未全覆盖的值，补进白名单
        foreach (var t in new[]
                 {
                     1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 31, 32, 44, 60, 61, 62, 66, 99,
                     111, 120, 121, 130, 131, 132, 133, 140, 141, 150, 151, 200, 230, 250, 251
                 })
            set.Add(t);
        return set;
    }

    /// <summary>
    /// 判断整型是否为已登记的资金类型（对齐旧 Enum.IsDefined(MoneyType)）。
    /// </summary>
    /// <param name="type">资金类型</param>
    /// <returns>是否已知</returns>
    public static bool IsKnownType(int type) => type > 0 && KnownTypes.Contains(type);

    /// <summary>
    /// 解析类型列：纯数字直接用；中文名查对照表。
    /// </summary>
    /// <param name="raw">Excel 类型单元格</param>
    /// <param name="type">解析后的整型</param>
    /// <returns>能否解析</returns>
    public static bool TryResolve(string raw, out int type)
    {
        type = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var s = raw.Trim();
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out type))
            return IsKnownType(type);

        s = s.Trim().Trim('#');
        return ByName.TryGetValue(s, out type);
    }

    /// <summary>
    /// 解析币种列：数字或空（空则默认 1）。
    /// </summary>
    /// <param name="raw">Excel 币种单元格</param>
    /// <param name="currency">币种</param>
    /// <returns>能否解析</returns>
    public static bool TryResolveCurrency(string raw, out int currency)
    {
        currency = 1;
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out currency)
               && currency > 0;
    }
}
