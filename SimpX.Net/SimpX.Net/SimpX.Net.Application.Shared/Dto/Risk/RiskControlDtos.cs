using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>风控列表公共筛选。</summary>
public sealed class RiskControlQueryInput
{
    /// <summary>会员用户名（可空）。</summary>
    [JsonPropertyName("User")]
    public string User { get; set; } = string.Empty;

    /// <summary>开始日。</summary>
    [JsonPropertyName("StartAt")]
    public DateTime? StartAt { get; set; }

    /// <summary>结束日。</summary>
    [JsonPropertyName("EndAt")]
    public DateTime? EndAt { get; set; }

    /// <summary>游戏类型（游戏盈利用；0=不限）。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>风险类型筛选（风险会员用；空=全部）。</summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>会员盈利一行（分类金额展开为键值）。</summary>
public sealed class RiskProfitRowDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>各分类金额：键=分类名，值=金额。</summary>
    [JsonPropertyName("Categories")]
    public Dictionary<string, decimal> Categories { get; set; } = new();

    /// <summary>合计。</summary>
    [JsonPropertyName("Total")]
    public decimal Total { get; set; }

    /// <summary>是否页脚统计行。</summary>
    [JsonPropertyName("IsFooter")]
    public bool IsFooter { get; set; }
}

/// <summary>会员盈利出参。</summary>
public sealed class RiskProfitOutput
{
    /// <summary>行（末行可为合计）。</summary>
    [JsonPropertyName("Rows")]
    public List<RiskProfitRowDto> Rows { get; set; } = [];
}

/// <summary>游戏盈利一行。</summary>
public sealed class RiskGameProfitRowDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>游戏类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>盈亏。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }
}

/// <summary>游戏盈利出参。</summary>
public sealed class RiskGameProfitOutput
{
    /// <summary>行。</summary>
    [JsonPropertyName("Rows")]
    public List<RiskGameProfitRowDto> Rows { get; set; } = [];

    /// <summary>合计盈亏。</summary>
    [JsonPropertyName("TotalMoney")]
    public decimal TotalMoney { get; set; }
}

/// <summary>风险会员一行。</summary>
public sealed class RiskDangerUserRowDto
{
    /// <summary>风险编号。</summary>
    [JsonPropertyName("RiskID")]
    public int RiskID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>代理名。</summary>
    [JsonPropertyName("AgentName")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>摘要。</summary>
    [JsonPropertyName("Context")]
    public string Context { get; set; } = string.Empty;

    /// <summary>记录时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>业务日。</summary>
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    /// <summary>锁定状态。</summary>
    [JsonPropertyName("Lock")]
    public int Lock { get; set; }

    /// <summary>注册时间。</summary>
    [JsonPropertyName("RegCreateAt")]
    public DateTime RegCreateAt { get; set; }

    /// <summary>VIP 等级。</summary>
    [JsonPropertyName("Level")]
    public int Level { get; set; }

    /// <summary>风险类型串。</summary>
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;
}
