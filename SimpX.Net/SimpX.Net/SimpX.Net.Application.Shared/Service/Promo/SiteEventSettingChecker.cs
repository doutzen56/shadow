using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 优惠活动 Setting 与类型唯一性校验（对齐旧版 EventAgent.SaveEvent + IEvent.Check 主约束）。
/// </summary>
internal static class SiteEventSettingChecker
{
    /// <summary>新版每日任务。</summary>
    public const int TypeNewDailyTasks = 2;

    /// <summary>新版每周任务。</summary>
    public const int TypeNewWeekTasks = 3;

    /// <summary>幸运转盘。</summary>
    public const int TypeDelottery = 11;

    /// <summary>邀请有礼。</summary>
    public const int TypeInviteReward = 12;

    /// <summary>
    /// 已知可配 Setting 的活动类型（对齐旧 EventType；手动活动 0 除外）。
    /// </summary>
    private static readonly HashSet<int> SupportedTypes =
    [
        1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25
    ];

    /// <summary>
    /// 校验类型是否支持、Setting 主约束与会员分组。
    /// </summary>
    /// <param name="type">活动类型</param>
    /// <param name="settingJson">Setting JSON</param>
    /// <exception cref="AppFriendlyException">校验失败</exception>
    public static void CheckSetting(int type, string settingJson)
    {
        if (type == 0)
            return;
        if (!SupportedTypes.Contains(type))
            throw Oops.Oh("#当前不支持该活动类型#");

        if (string.IsNullOrWhiteSpace(settingJson))
            throw Oops.Oh("#当前不支持该活动类型#");

        using var doc = JsonDocument.Parse(settingJson);
        var root = doc.RootElement;
        EnsureUserGroup(root);

        switch (type)
        {
            case TypeNewDailyTasks:
            case TypeNewWeekTasks:
                EnsureTaskItems(root);
                break;
            case TypeInviteReward:
                EnsureInviteReward(root);
                break;
            case 9: // Recharge
                EnsureRechargeChoice(root);
                break;
        }
    }

    /// <summary>
    /// 读邀请有礼 ReturnType；解析失败返回 -1。
    /// </summary>
    /// <param name="settingJson">Setting</param>
    /// <returns>ReturnType 整型</returns>
    public static int ReadInviteReturnType(string settingJson)
    {
        if (string.IsNullOrWhiteSpace(settingJson))
            return -1;
        try
        {
            using var doc = JsonDocument.Parse(settingJson);
            if (doc.RootElement.TryGetProperty("ReturnType", out var p) && p.TryGetInt32(out var v))
                return v;
        }
        catch (JsonException)
        {
            return -1;
        }

        return -1;
    }

    /// <summary>
    /// 会员分组必填（对齐多数 IEvent.Check）。
    /// </summary>
    private static void EnsureUserGroup(JsonElement root)
    {
        if (!root.TryGetProperty("UserGroup", out var ug) || ug.ValueKind != JsonValueKind.Array)
            throw Oops.Oh("#请指定参加活动的会员分组#");
        var ids = ug.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
            .Select(e => e.GetInt32())
            .ToArray();
        if (ids.Length <= 0 || (ids.Contains(0) && ids.Length == 1))
            throw Oops.Oh("#请指定参加活动的会员分组#");
    }

    /// <summary>
    /// 每日/每周任务奖品行。
    /// </summary>
    private static void EnsureTaskItems(JsonElement root)
    {
        JsonElement items = default;
        if (root.TryGetProperty("Items", out var direct) && direct.ValueKind == JsonValueKind.Array)
            items = direct;
        else if (root.TryGetProperty("NewDailyTasks", out var nest)
                 && nest.TryGetProperty("Items", out var nested)
                 && nested.ValueKind == JsonValueKind.Array)
            items = nested;
        else if (root.TryGetProperty("NewWeekTasks", out var nest2)
                 && nest2.TryGetProperty("Items", out var nested2)
                 && nested2.ValueKind == JsonValueKind.Array)
            items = nested2;

        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
            throw Oops.Oh("#奖品设置错误#");

        var ids = new List<int>();
        foreach (var it in items.EnumerateArray())
        {
            if (it.TryGetProperty("ID", out var idEl) && idEl.TryGetInt32(out var id))
                ids.Add(id);
        }

        if (ids.GroupBy(x => x).Any(g => g.Count() > 1))
            throw Oops.Oh("#奖品编号重复#");
    }

    /// <summary>
    /// 邀请有礼周期约束。
    /// </summary>
    private static void EnsureInviteReward(JsonElement root)
    {
        var returnType = root.TryGetProperty("ReturnType", out var rt) && rt.TryGetInt32(out var rtv) ? rtv : -1;
        var period = root.TryGetProperty("ReturnPeriod", out var rp) && rp.TryGetInt32(out var rpv) ? rpv : -1;
        // 0=立返 RealTime；首充只支持立返
        if (returnType == 0 && period != 0)
            throw Oops.Oh("#首充只支持立返#");
        // 1=有效投注：日/周/月
        if (returnType == 1 && period is not (1 or 2 or 3))
            throw Oops.Oh("有效投注只支持日返、周返、月返");
        // 2=负盈利：月返
        if (returnType == 2 && period != 3)
            throw Oops.Oh("#负盈利只支持月返#");
    }

    /// <summary>
    /// 充值优惠：比例与固定不能同时有。
    /// </summary>
    private static void EnsureRechargeChoice(JsonElement root)
    {
        var reward = root.TryGetProperty("Reward", out var r) && r.TryGetDecimal(out var rv) ? rv : 0m;
        var hasFixed = false;
        if (root.TryGetProperty("FixedReward", out var fr))
        {
            if (fr.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in fr.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDecimal(out var d) && d != 0)
                        hasFixed = true;
                }
            }
            else if (fr.ValueKind == JsonValueKind.Number && fr.TryGetDecimal(out var d) && d != 0)
                hasFixed = true;
        }

        if (reward > 0m && hasFixed)
            throw Oops.Oh("#比例固定只能二选一#");
    }
}
