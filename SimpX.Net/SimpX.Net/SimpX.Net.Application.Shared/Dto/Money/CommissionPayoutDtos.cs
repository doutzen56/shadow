namespace SimpX.Net.Application.Shared;

/// <summary>佣金日志行。</summary>
public sealed class CommissionLogListItemDto
{
    /// <summary>日志号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>统计起。</summary>
    public DateTime StartDate { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndDate { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>佣金发放入参。</summary>
public sealed class CommissionPayInput
{
    /// <summary>日志号。</summary>
    public int LogID { get; set; }
}

/// <summary>生成佣金区间入参。</summary>
public sealed class CommissionCreateInput
{
    /// <summary>统计起。</summary>
    public DateTime StartDate { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>生成佣金结果。</summary>
public sealed class CommissionCreateResultDto
{
    /// <summary>插入成功条数。</summary>
    public int Success { get; set; }

    /// <summary>插入失败条数。</summary>
    public int Failed { get; set; }
}

/// <summary>佣金日志号入参（旧 ID）。</summary>
public sealed class CommissionLogIdInput
{
    /// <summary>日志号。</summary>
    public int ID { get; set; }
}

/// <summary>佣金详情。</summary>
public sealed class CommissionLogDetailDto
{
    /// <summary>日志号。</summary>
    public int ID { get; set; }

    /// <summary>明细 JSON 原文。</summary>
    public string Detail { get; set; }
}
