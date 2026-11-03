using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台站点列表行（字段名跟旧版一样，首字母大写）。
/// </summary>
public sealed class SiteListItemDto
{
    /// <summary>站点编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>展示名（优先中文）。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>游戏授信。</summary>
    [JsonPropertyName("GameCredit")]
    public decimal GameCredit { get; set; }

    /// <summary>主币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }
}

/// <summary>
/// 总后台站点主机行。
/// </summary>
public sealed class SiteHostItemDto
{
    /// <summary>主机编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>主机名。</summary>
    [JsonPropertyName("Host")]
    public string Host { get; set; }

    /// <summary>站点编号。</summary>
    [JsonPropertyName("SiteID")]
    public int SiteID { get; set; }

    /// <summary>是否启用。</summary>
    [JsonPropertyName("IsEnable")]
    public bool IsEnable { get; set; }
}

/// <summary>
/// 站点后台域名行。
/// </summary>
public sealed class SiteDomainItemDto
{
    /// <summary>域名编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>域名。</summary>
    [JsonPropertyName("Domain")]
    public string Domain { get; set; }

    /// <summary>链接（由是否 SSL 拼出）。</summary>
    [JsonPropertyName("Link")]
    public string Link { get; set; }

    /// <summary>权重。</summary>
    [JsonPropertyName("Weights")]
    public short Weights { get; set; }

    /// <summary>代理名。</summary>
    [JsonPropertyName("AgentName")]
    public string AgentName { get; set; }

    /// <summary>是否可见。</summary>
    [JsonPropertyName("IsVisible")]
    public bool IsVisible { get; set; }

    /// <summary>是否 HTTPS。</summary>
    [JsonPropertyName("IsSSL")]
    public bool IsSSL { get; set; }

    /// <summary>是否 APP。</summary>
    [JsonPropertyName("IsAPP")]
    public bool IsAPP { get; set; }

    /// <summary>测速模板。</summary>
    [JsonPropertyName("Speed")]
    public int Speed { get; set; }
}

/// <summary>
/// 会员列表行摘要（字段跟旧版列表常用列接近；金额、VIP 名等以后再补）。
/// </summary>
public sealed class MemberListItemDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>会员编号别名。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>昵称。</summary>
    [JsonPropertyName("NickName")]
    public string NickName { get; set; }

    /// <summary>提现账户名。</summary>
    [JsonPropertyName("AccountName")]
    public string AccountName { get; set; }

    /// <summary>注册时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>锁定位。</summary>
    [JsonPropertyName("Lock")]
    public int Lock { get; set; }

    /// <summary>上级代理编号。</summary>
    [JsonPropertyName("AgentID")]
    public int AgentID { get; set; }

    /// <summary>邀请人编号。</summary>
    [JsonPropertyName("InviteID")]
    public int InviteID { get; set; }

    /// <summary>分组编号。</summary>
    [JsonPropertyName("GroupID")]
    public int GroupID { get; set; }

    /// <summary>等级。</summary>
    [JsonPropertyName("Level")]
    public byte Level { get; set; }

    /// <summary>上次登录时间。</summary>
    [JsonPropertyName("LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>登录 IP。</summary>
    [JsonPropertyName("LoginIP")]
    public string LoginIP { get; set; }

    /// <summary>活动时间。</summary>
    [JsonPropertyName("ActiveAt")]
    public DateTime ActiveAt { get; set; }

    /// <summary>邮箱（无「查看联系方式」权限时脱敏）。</summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; }

    /// <summary>手机（无「查看联系方式」权限时脱敏）。</summary>
    [JsonPropertyName("Mobile")]
    public string Mobile { get; set; }

    /// <summary>主钱包币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>可用余额。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>用户类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>代理深度。</summary>
    [JsonPropertyName("Depth")]
    public int Depth { get; set; }

    /// <summary>是否在线（活动时间 10 分钟内）。</summary>
    [JsonPropertyName("IsOnline")]
    public bool IsOnline { get; set; }

    /// <summary>上级代理用户名。</summary>
    [JsonPropertyName("AgentName")]
    public string AgentName { get; set; }

    /// <summary>邀请人用户名。</summary>
    [JsonPropertyName("InviterName")]
    public string InviterName { get; set; }

    /// <summary>分组名（旧列 Group）。</summary>
    [JsonPropertyName("Group")]
    public string Group { get; set; }

    /// <summary>等级名。</summary>
    [JsonPropertyName("LevelName")]
    public string LevelName { get; set; }

    /// <summary>总余额（列表主币种钱包）。</summary>
    [JsonPropertyName("TotalMoney")]
    public decimal TotalMoney { get; set; }

    /// <summary>总存款（对齐旧 TotalRechargeMoney：充值/代客充值/充值扣除类汇总）。</summary>
    [JsonPropertyName("TotalRechargeMoney")]
    public decimal TotalRechargeMoney { get; set; }

    /// <summary>总取款（对齐旧 TotalWithdrawMoney：提现/提现扣除/提现退回汇总）。</summary>
    [JsonPropertyName("TotalWithdrawMoney")]
    public decimal TotalWithdrawMoney { get; set; }

    /// <summary>开户方式 RegFlag。</summary>
    [JsonPropertyName("RegFlag")]
    public int RegFlag { get; set; }

    /// <summary>直属下级人数（代理列用）。</summary>
    [JsonPropertyName("Count")]
    public int Count { get; set; }

    /// <summary>类型文案（会员/代理）。</summary>
    [JsonPropertyName("TypeName")]
    public string TypeName { get; set; }
}

/// <summary>
/// 会员列表分页 info。
/// </summary>
public sealed class MemberListInfoDto
{
    /// <summary>总条数（跟旧版一样用 RecordCount，同时保留 total）。</summary>
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

    /// <summary>当前页数据。</summary>
    [JsonPropertyName("data")]
    public List<MemberListItemDto> Data { get; set; }
}
