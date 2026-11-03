using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员帐变分页筛选（对齐旧版 /admin/report/money/log）。
/// </summary>
public sealed class MoneyLogAdminPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员编号或用户名。</summary>
    public string User { get; set; }

    /// <summary>会员编号（优先）。</summary>
    public int UserID { get; set; }

    /// <summary>开始时间；空则今天 0 点。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间；空则现在（查询时右开 +1 秒）。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>备注包含。</summary>
    public string Content { get; set; }

    /// <summary>帐变类型，逗号分隔整型。</summary>
    public string Type { get; set; }

    /// <summary>币种整型；空不限。</summary>
    public string Currency { get; set; }

    /// <summary>来源单号。</summary>
    public string SourceID { get; set; }

    /// <summary>包含下级：1=是。</summary>
    public string BelongUser { get; set; }
}

/// <summary>
/// 帐变行。
/// </summary>
public sealed class MoneyLogAdminListItemDto
{
    /// <summary>流水号。</summary>
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

    /// <summary>帐变金额。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>锁定金额。</summary>
    [JsonPropertyName("LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>余额。</summary>
    [JsonPropertyName("Balance")]
    public decimal Balance { get; set; }

    /// <summary>时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>IP。</summary>
    [JsonPropertyName("IP")]
    public string IP { get; set; }

    /// <summary>类型整型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>类型显示名（旧 TypeValue）。</summary>
    [JsonPropertyName("TypeValue")]
    public string TypeValue { get; set; }

    /// <summary>来源。</summary>
    [JsonPropertyName("SourceID")]
    public string SourceID { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>币种代码。</summary>
    [JsonPropertyName("CurrencyCode")]
    public string CurrencyCode { get; set; }
}

/// <summary>
/// 帐变合计。
/// </summary>
public sealed class MoneyLogAdminSummaryDto
{
    /// <summary>金额合计（单币种为原币；多币种按基准汇率折算近似）。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>余额合计（同上）。</summary>
    [JsonPropertyName("Balance")]
    public decimal Balance { get; set; }

    /// <summary>涉及会员数。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }
}

/// <summary>
/// 帐变分页输出。
/// </summary>
public sealed class MoneyLogAdminPageOutput
{
    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>页码。</summary>
    public int Page { get; set; }

    /// <summary>每页。</summary>
    public int PageSize { get; set; }

    /// <summary>行。</summary>
    public List<MoneyLogAdminListItemDto> Data { get; set; }

    /// <summary>合计。</summary>
    public MoneyLogAdminSummaryDto Summary { get; set; }
}
