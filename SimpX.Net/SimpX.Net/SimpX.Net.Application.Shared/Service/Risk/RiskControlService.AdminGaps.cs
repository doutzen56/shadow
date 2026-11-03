using System.Text.Json;
using SimpX.Net.Data.Entity.Risk;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 风险会员明细弹窗。对齐旧 /admin/risk/user/*。
/// </summary>
public sealed partial class RiskControlService
{
    /// <summary>
    /// 风险会员列表别名。对齐旧 list。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public Task<List<RiskDangerUserRowDto>> ListAsync(
        RiskControlQueryInput input,
        CancellationToken cancellationToken = default) =>
        DangerUsersAsync(input ?? new RiskControlQueryInput(), cancellationToken);

    /// <summary>
    /// 批量处理：按 RiskID 删除风险记录（移出待处理列表）。对齐旧 updatestatus。
    /// </summary>
    /// <param name="riskIds">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除条数</returns>
    public async Task<int> UpdateStatusAsync(IReadOnlyList<int> riskIds, CancellationToken cancellationToken = default)
    {
        if (riskIds == null || riskIds.Count == 0)
            throw Oops.Oh("#请选择要处理的记录#");
        var siteId = _site.RequireSiteId();
        var ids = riskIds.Where(x => x > 0).Distinct().ToList();
        if (ids.Count == 0)
            throw Oops.Oh("#请选择要处理的记录#");
        return await _db.Deleteable<RiskUserDanger>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.RiskID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 异地登录 IP 对比。对齐旧 ip。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>对比</returns>
    public async Task<RiskUserIpCompareDto> IpAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [0, 1, 2]);
        if (detail == null)
            return new RiskUserIpCompareDto();

        var curId = detail.Value.TryGetProperty("CurrentLogID", out var c) ? c.GetInt32() : 0;
        var preId = detail.Value.TryGetProperty("PrevLogID", out var p) ? p.GetInt32() : 0;
        var logs = await _db.Queryable<UserLog>()
            .Where(t => t.ID == curId || t.ID == preId)
            .ToListAsync(cancellationToken);
        var cur = logs.FirstOrDefault(t => t.ID == curId);
        var pre = logs.FirstOrDefault(t => t.ID == preId);
        return new RiskUserIpCompareDto
        {
            CurCreateAt = cur?.CreateAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
            CurIP = cur?.IP ?? string.Empty,
            CurAddress = string.Empty,
            PreCreateAt = pre?.CreateAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
            PreIP = pre?.IP ?? string.Empty,
            PreAddress = string.Empty
        };
    }

    /// <summary>
    /// 手机邮箱未绑定说明。对齐旧 info。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>说明</returns>
    public async Task<RiskUserBindInfoDto> InfoAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [7]);
        var regRaw = detail != null && detail.Value.TryGetProperty("注册时间", out var reg)
            ? reg.GetString()
            : null;
        if (!DateTime.TryParse(regRaw, out var createAt))
            createAt = risk.CreateAt;
        var days = (risk.CreateAt.Date - createAt.Date).Days;
        return new RiskUserBindInfoDto
        {
            BindInfo = $"注册已{days}天仍未完成手机或邮箱绑定"
        };
    }

    /// <summary>
    /// 同 IP 多用户。对齐旧 ipdifuser。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>说明</returns>
    public async Task<RiskUserContextDto> IpDifUserAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [4]);
        var ip = detail != null && detail.Value.TryGetProperty("IP", out var ipEl) ? ipEl.GetString() ?? string.Empty : string.Empty;
        var usersRaw = detail != null && detail.Value.TryGetProperty("Users", out var u) ? u.GetString() ?? string.Empty : string.Empty;
        var userIds = usersRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .ToList();
        var names = await LoadUserNamesAsync(risk.SiteID, userIds, cancellationToken);
        var text = $"{string.Join(",", names.Values)} 在{risk.Date:yyyy-MM-dd}当天全部使用IP {ip}登录";
        return new RiskUserContextDto { Context = text };
    }

    /// <summary>
    /// 同用户多 IP。对齐旧 userdifip。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>说明</returns>
    public async Task<RiskUserContextDto> UserDifIpAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [3]);
        var ips = detail != null && detail.Value.TryGetProperty("IPs", out var el) ? el.GetString() ?? string.Empty : string.Empty;
        var names = await LoadUserNamesAsync(risk.SiteID, [risk.UserID], cancellationToken);
        var userName = names.GetValueOrDefault(risk.UserID, string.Empty);
        return new RiskUserContextDto
        {
            Context = $"{userName} 在{risk.Date:yyyy-MM-dd}当天使用多个IP {ips}登录"
        };
    }

    /// <summary>
    /// 登录密码多次变更。对齐旧 login。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>次数</returns>
    public async Task<RiskUserPasswordDto> LoginAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [5]);
        var times = SplitTimes(detail);
        return new RiskUserPasswordDto
        {
            LoginChangeCount = times.Length,
            DefaultCount = 0,
            Context = times
        };
    }

    /// <summary>
    /// 资金密码多次变更。对齐旧 amount。
    /// </summary>
    /// <param name="riskId">风险编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>次数</returns>
    public async Task<RiskUserPasswordDto> AmountAsync(int riskId, CancellationToken cancellationToken = default)
    {
        var risk = await LoadRiskAsync(riskId, cancellationToken);
        var detail = PickDetail(risk.Detail, preferredTypes: [6]);
        var times = SplitTimes(detail);
        return new RiskUserPasswordDto
        {
            AmountChangeCount = times.Length,
            DefaultCount = 0,
            Context = times
        };
    }

    private async Task<RiskUserDanger> LoadRiskAsync(int riskId, CancellationToken cancellationToken)
    {
        if (riskId <= 0)
            throw Oops.Oh("#错误数据#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RiskUserDanger>()
            .Where(t => t.SiteID == siteId && t.RiskID == riskId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault() ?? throw Oops.Oh("#错误数据#");
    }

    private static string[] SplitTimes(JsonElement? detail)
    {
        if (detail == null || !detail.Value.TryGetProperty("Times", out var times))
            return [];
        return (times.GetString() ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Detail 可能是单个 JSON，或逗号拼成的多个对象；按 Type 取第一条。
    /// </summary>
    private static JsonElement? PickDetail(string? detail, int[] preferredTypes)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;
        try
        {
            using var doc = JsonDocument.Parse($"[{detail}]");
            foreach (var type in preferredTypes)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("Type", out var t) && t.TryGetInt32(out var tv) && tv == type)
                        return el.Clone();
                }
            }

            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined ? null : first.Clone();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>异地 IP 对比。</summary>
public sealed class RiskUserIpCompareDto
{
    /// <summary>当前时间。</summary>
    public string CurCreateAt { get; set; }

    /// <summary>当前地址。</summary>
    public string CurAddress { get; set; }

    /// <summary>当前 IP。</summary>
    public string CurIP { get; set; }

    /// <summary>上次时间。</summary>
    public string PreCreateAt { get; set; }

    /// <summary>上次地址。</summary>
    public string PreAddress { get; set; }

    /// <summary>上次 IP。</summary>
    public string PreIP { get; set; }
}

/// <summary>绑定说明。</summary>
public sealed class RiskUserBindInfoDto
{
    /// <summary>说明。</summary>
    public string BindInfo { get; set; }
}

/// <summary>文本上下文。</summary>
public sealed class RiskUserContextDto
{
    /// <summary>说明。</summary>
    public string Context { get; set; }
}

/// <summary>密码变更次数。</summary>
public sealed class RiskUserPasswordDto
{
    /// <summary>登录密码变更次数。</summary>
    public int LoginChangeCount { get; set; }

    /// <summary>资金密码变更次数。</summary>
    public int AmountChangeCount { get; set; }

    /// <summary>阈值。</summary>
    public int DefaultCount { get; set; }

    /// <summary>时间点。</summary>
    public string[] Context { get; set; }
}
