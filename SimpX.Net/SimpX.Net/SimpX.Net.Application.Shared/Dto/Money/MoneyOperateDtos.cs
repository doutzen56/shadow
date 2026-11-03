using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员加款/扣款入参（对齐旧 admin/money/operate/addmoney|redeductmoney 主字段）。
/// </summary>
public sealed class MoneyOperateSubmitInput
{
    /// <summary>用户名，多个用英文逗号分隔，最多 1000 个。</summary>
    [JsonPropertyName("User")]
    public string User { get; set; }

    /// <summary>
    /// 批量文件正文（CSV/TSV：每行「用户名」或「用户名,金额」）。有值时优先于 User。
    /// </summary>
    [JsonPropertyName("CsvText")]
    public string CsvText { get; set; }

    /// <summary>资金类型（MoneyType 整型）。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>金额（正数）。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>流水号；与用户、类型全站唯一。</summary>
    [JsonPropertyName("SourceID")]
    public string SourceID { get; set; }

    /// <summary>前端备注。</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    [JsonPropertyName("DescriptionA")]
    public string DescriptionA { get; set; }

    /// <summary>流水倍率；加款用，扣款可忽略。</summary>
    [JsonPropertyName("Flow")]
    public decimal Flow { get; set; } = 1;

    /// <summary>最低流水；加款用。</summary>
    [JsonPropertyName("MinFlow")]
    public decimal MinFlow { get; set; }

    /// <summary>充值渠道；扣款可选。</summary>
    [JsonPropertyName("PayID")]
    public int PayID { get; set; }
}

/// <summary>
/// 加扣款逐用户结果。
/// </summary>
public sealed class MoneyOperateUserResult
{
    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>是否成功。</summary>
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    /// <summary>说明。</summary>
    [JsonPropertyName("Message")]
    public string Message { get; set; }
}

/// <summary>
/// 加扣款提交汇总。
/// </summary>
public sealed class MoneyOperateSubmitResult
{
    /// <summary>逐用户结果。</summary>
    [JsonPropertyName("Items")]
    public List<MoneyOperateUserResult> Items { get; set; } = [];

    /// <summary>汇总文案（对齐旧版拼消息）。</summary>
    [JsonPropertyName("Summary")]
    public string Summary { get; set; }
}

/// <summary>
/// 加款页游戏过滤选项。对齐旧 operate/filtergame。
/// </summary>
public sealed class MoneyOperateFilterGameDto
{
    /// <summary>开放厂商选项 JSON 数组原文。</summary>
    [JsonPropertyName("openPlatforms")]
    public string OpenPlatforms { get; set; }

    /// <summary>开放转入选项 JSON 数组原文。</summary>
    [JsonPropertyName("openTransfer")]
    public string OpenTransfer { get; set; }

    /// <summary>初始勾选 JSON 数组原文。</summary>
    [JsonPropertyName("initPlatform")]
    public string InitPlatform { get; set; }
}

/// <summary>
/// 活动已选游戏过滤回显。对齐旧 operate/initplatform|inittransfer。
/// </summary>
public sealed class MoneyOperateInitPlatformDto
{
    /// <summary>已选游戏 JSON 数组原文。</summary>
    [JsonPropertyName("initPlatform")]
    public string InitPlatform { get; set; }
}

/// <summary>
/// 手工活动下拉行。对齐旧 operate/show0|showtransfer0。
/// </summary>
public sealed class MoneyOperateEventOptionDto
{
    /// <summary>活动编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>标题原文（多语言 JSON 原样返回）。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }
}

/// <summary>
/// 批量加款（制表符行）入参。对齐旧 operate/addmoneybatch。
/// </summary>
public sealed class MoneyOperateBatchTabInput
{
    /// <summary>多行文本：用户编号\t类型\t流水号\t金额\t备注\t流水倍\t最低流水\t币种。</summary>
    [JsonPropertyName("Batch")]
    public string Batch { get; set; }

    /// <summary>缺流水号时的前缀。</summary>
    [JsonPropertyName("SourceID")]
    public string SourceID { get; set; }
}
