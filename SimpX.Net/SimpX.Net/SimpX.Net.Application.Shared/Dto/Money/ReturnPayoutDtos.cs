namespace SimpX.Net.Application.Shared;

/// <summary>返点日志行。</summary>
public sealed class ReturnLogListItemDto
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
    public DateTime StartAt { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndAt { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>计划号。</summary>
    public int PlanID { get; set; }
}

/// <summary>返点发放入参。</summary>
public sealed class ReturnPayInput
{
    /// <summary>日志号。</summary>
    public int LogID { get; set; }
}

/// <summary>生成返点区间入参。</summary>
public sealed class ReturnCreateInput
{
    /// <summary>统计起。</summary>
    public DateTime StartDate { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>生成返点结果。</summary>
public sealed class ReturnCreateResultDto
{
    /// <summary>插入成功条数。</summary>
    public int Success { get; set; }

    /// <summary>插入失败条数。</summary>
    public int Failed { get; set; }
}
