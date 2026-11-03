using System.Globalization;
using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 解析代理上下分统计 JSON 并调存储过程。
/// </summary>
public sealed class ProxyMoneyStsConsumer : ITransient, IProxyMoneyStsConsumer
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public ProxyMoneyStsConsumer(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <inheritdoc />
    public async Task ConsumeAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            var date = ReadDate(root, "CreateAt");
            var userId = ReadInt(root, "UserId", "UserID");
            var siteId = ReadInt(root, "SiteId", "SiteID");
            var type = ReadMoneyType(root);
            var money = Math.Abs(ReadDecimal(root, "Money"));
            var currency = ReadCurrency(root);

            await _db.Ado.ExecuteCommandAsync(
                "EXEC rpt_ProxyStatistics_UpdateMoney @Date,@UserID,@SiteID,@Type,@Money,@Currency",
                new List<SugarParameter>
                {
                    new("@Date", date),
                    new("@UserID", userId),
                    new("@SiteID", siteId),
                    new("@Type", type),
                    new("@Money", money),
                    new("@Currency", currency)
                });
        }
        catch (Exception ex)
        {
            try
            {
                Log.Warning($"代理上下分统计失败：{ex.Message}");
            }
            catch
            {
                // 单测无 Furion 宿主时忽略日志
            }
        }
    }

    private static DateTime ReadDate(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return DateTime.Now.Date;
        if (p.ValueKind == JsonValueKind.String
            && DateTime.TryParse(p.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt;
        return DateTime.Now.Date;
    }

    private static int ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var p))
                continue;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
                return n;
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s))
                return s;
        }

        return 0;
    }

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return 0;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
            return d;
        if (p.ValueKind == JsonValueKind.String
            && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;
        return 0;
    }

    private static int ReadMoneyType(JsonElement root)
    {
        if (!root.TryGetProperty("Type", out var p))
            return 0;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
            return n;
        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (int.TryParse(s, out var i))
                return i;
            // 旧队列偶发枚举名；常见代理上下分
            if (string.Equals(s, "ProxyIn", StringComparison.OrdinalIgnoreCase))
                return 140;
            if (string.Equals(s, "ProxyOut", StringComparison.OrdinalIgnoreCase))
                return 141;
        }

        return 0;
    }

    private static int ReadCurrency(JsonElement root)
    {
        if (!root.TryGetProperty("Currency", out var p))
            return 0;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
            return n;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s))
            return s;
        return 0;
    }
}
