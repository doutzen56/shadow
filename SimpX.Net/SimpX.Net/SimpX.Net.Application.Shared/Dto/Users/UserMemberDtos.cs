using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台会员分页查询。筛选键对齐旧站会员列表 Form（SearchType/User/Agent…）。
/// </summary>
public sealed class UserMemberPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小，1～100。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊（旧版 UserName / User；无 SearchType 时用）。</summary>
    public string UserName { get; set; }

    /// <summary>搜索类型，对齐旧版 SearchType（UserName/UserID/NickName/Mobile/Email 等）。</summary>
    public string SearchType { get; set; }

    /// <summary>搜索关键字，对齐旧版 User。</summary>
    public string User { get; set; }

    /// <summary>上级代理用户名；填 0 表示只查总代（AgentID=0）。</summary>
    public string Agent { get; set; }

    /// <summary>包含下级：1=代理树下全体，0=仅直属。对齐 BelongUser。</summary>
    public int BelongUser { get; set; }

    /// <summary>用户类型：数字或 Member/Agent。空=全部。</summary>
    public string Type { get; set; }

    /// <summary>会员分组。</summary>
    public int? GroupID { get; set; }

    /// <summary>会员等级。</summary>
    public int? Level { get; set; }

    /// <summary>开户方式 RegFlag。</summary>
    public int? RegMethod { get; set; }

    /// <summary>时间类型：Reg / Login / Active。</summary>
    public string TimeType { get; set; }

    /// <summary>时间起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>时间止。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>余额下限。</summary>
    public decimal? MinMoney { get; set; }

    /// <summary>余额上限。</summary>
    public decimal? MaxMoney { get; set; }

    /// <summary>用户层级 Depth。</summary>
    public int? Depth { get; set; }

    /// <summary>在线分钟内（按 ActiveAt）。</summary>
    public int? Active { get; set; }

    /// <summary>注册域名。</summary>
    public string Domain { get; set; }

    /// <summary>邀请人用户名。</summary>
    public string InviterName { get; set; }

    /// <summary>显示币种（列表展示用；筛余额时可作参考）。</summary>
    public int? Currency { get; set; }

    /// <summary>1=必须有查询条件（默认）；0=允许全体（查看全体会员）。</summary>
    public int IsLimit { get; set; } = 1;

    /// <summary>佣金方案（有字段后再筛；暂收参）。</summary>
    public int? CommissionID { get; set; }

    /// <summary>返点方案（有字段后再筛；暂收参）。</summary>
    public int? ReturnID { get; set; }

    /// <summary>充值次数档：1首充/2二充/3三充（依赖统计表，暂收参）。</summary>
    public int? RechargeCount { get; set; }
}

/// <summary>
/// 总后台跨站会员分页（必须显式 SiteID）。
/// </summary>
public sealed class PlatformUserMemberPageInput
{
    /// <summary>站点编号，必须大于 0。</summary>
    public int SiteID { get; set; }

    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小，1～100。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }
}

/// <summary>
/// 租户后台会员分页结果（业务字段与旧版 list info 对齐）。
/// </summary>
public sealed class UserMemberPageOutput
{
    /// <summary>总条数（旧版 RecordCount）。</summary>
    [JsonPropertyName("RecordCount")]
    public int RecordCount { get; set; }

    /// <summary>总条数别名。</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>页码。</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>页大小。</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>当前页行。</summary>
    [JsonPropertyName("data")]
    public List<MemberListItemDto> Data { get; set; }
}
