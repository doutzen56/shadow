using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理申请分页查询（对齐旧版 applylist）。
/// </summary>
public sealed class AgentApplyPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string User { get; set; }

    /// <summary>状态；null 不限。0 待审 / 1 拒绝 / 2 通过。</summary>
    public int? Status { get; set; }

    /// <summary>申请起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>申请止。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// 代理申请列表行。
/// </summary>
public sealed class AgentApplyListItemDto
{
    /// <summary>申请编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>申请时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>状态：0 待审 / 1 拒绝 / 2 通过。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>状态文案。</summary>
    [JsonPropertyName("StatusValue")]
    public string StatusValue { get; set; }

    /// <summary>Skype。</summary>
    [JsonPropertyName("Skype")]
    public string Skype { get; set; }

    /// <summary>QQ。</summary>
    [JsonPropertyName("QQ")]
    public string QQ { get; set; }

    /// <summary>地区。</summary>
    [JsonPropertyName("Area")]
    public string Area { get; set; }

    /// <summary>手机。</summary>
    [JsonPropertyName("Phone")]
    public string Phone { get; set; }

    /// <summary>Telegram。</summary>
    [JsonPropertyName("Telegram")]
    public string Telegram { get; set; }

    /// <summary>Line。</summary>
    [JsonPropertyName("Line")]
    public string Line { get; set; }

    /// <summary>WhatsApp。</summary>
    [JsonPropertyName("WhatsApp")]
    public string WhatsApp { get; set; }

    /// <summary>推广简介。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }
}

/// <summary>
/// 代理申请分页结果。
/// </summary>
public sealed class AgentApplyPageOutput
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
    public List<AgentApplyListItemDto> Data { get; set; }
}

/// <summary>
/// 代理申请审核入参（对齐旧版 execapply）。
/// </summary>
public sealed class AgentApplyCheckInput
{
    /// <summary>申请编号。</summary>
    public int ID { get; set; }

    /// <summary>处理动作：1 拒绝 / 2 通过。</summary>
    public int Status { get; set; }
}
