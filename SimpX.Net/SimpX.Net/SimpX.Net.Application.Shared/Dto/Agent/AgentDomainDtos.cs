using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理域名分页查询（对齐旧版 list 筛选）。
/// </summary>
public sealed class AgentDomainPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string User { get; set; }

    /// <summary>邀请码精确。</summary>
    public string Invite { get; set; }

    /// <summary>域名精确。</summary>
    public string Domain { get; set; }

    /// <summary>创建起（含）；null 则默认今天 0 点（对齐旧版）。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止（含日）；null 则默认现在（对齐旧版）。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// 代理域名列表行。
/// </summary>
public sealed class AgentDomainListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>上级用户名。</summary>
    [JsonPropertyName("UpUserName")]
    public string UpUserName { get; set; }

    /// <summary>域名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>该域名注册会员数。</summary>
    [JsonPropertyName("MemberCount")]
    public int MemberCount { get; set; }

    /// <summary>站点显示名。</summary>
    [JsonPropertyName("SiteName")]
    public string SiteName { get; set; }

    /// <summary>Logo。</summary>
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>邀请码。</summary>
    [JsonPropertyName("InviteID")]
    public string InviteID { get; set; }

    /// <summary>背景（原文）。</summary>
    [JsonPropertyName("DefaultBackground")]
    public string DefaultBackground { get; set; }
}

/// <summary>
/// 代理域名分页结果。
/// </summary>
public sealed class AgentDomainPageOutput
{
    /// <summary>总条数。</summary>
    [JsonPropertyName("RecordCount")]
    public int RecordCount { get; set; }

    /// <summary>页码。</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>页大小。</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>行。</summary>
    [JsonPropertyName("data")]
    public List<AgentDomainListItemDto> Data { get; set; }
}

/// <summary>
/// 代理域名详情（对齐旧版 info）。
/// </summary>
public sealed class AgentDomainDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>域名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>站名。</summary>
    [JsonPropertyName("SiteName")]
    public string SiteName { get; set; }

    /// <summary>Logo。</summary>
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>邀请码。</summary>
    [JsonPropertyName("InviteID")]
    public string InviteID { get; set; }

    /// <summary>背景。</summary>
    [JsonPropertyName("DefaultBackground")]
    public string DefaultBackground { get; set; }

    /// <summary>默认版型。</summary>
    [JsonPropertyName("DefaultTemplate")]
    public string DefaultTemplate { get; set; }
}


/// <summary>
/// 代理域名保存入参（对齐旧版 /admin/user/agent/domain/save）。
/// </summary>
public sealed class AgentDomainSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>域名（不含协议）。</summary>
    public string Name { get; set; }

    /// <summary>绑定邀请码。</summary>
    public string InviteID { get; set; }

    /// <summary>站名；可空。</summary>
    public string SiteName { get; set; }

    /// <summary>Logo；可空。</summary>
    public string Logo { get; set; }

    /// <summary>背景；可空。</summary>
    public string DefaultBackground { get; set; }

    /// <summary>默认版型；可空。</summary>
    public string DefaultTemplate { get; set; }
}
