namespace SimpX.Net.Application.Shared;

/// <summary>返水计划行。</summary>
public sealed class RebatePlanListItemDto
{
    /// <summary>计划编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>计划金额。</summary>
    public decimal Money { get; set; }

    /// <summary>已发放。</summary>
    public decimal Paid { get; set; }

    /// <summary>总笔数。</summary>
    public int Total { get; set; }

    /// <summary>成功笔数。</summary>
    public int Success { get; set; }

    /// <summary>失败笔数。</summary>
    public int Faild { get; set; }

    /// <summary>流水倍数。</summary>
    public decimal FlowRate { get; set; }

    /// <summary>统计起。</summary>
    public DateTime StartAt { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndAt { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>操作员。</summary>
    public int AdminID { get; set; }

    /// <summary>操作员名（对齐旧 Admin）。</summary>
    public string AdminName { get; set; }

    /// <summary>发放币种。</summary>
    public int Currency { get; set; }

    /// <summary>来源币种。</summary>
    public int FromCurrency { get; set; }

    /// <summary>折基础币金额。</summary>
    public decimal MoneyT { get; set; }

    /// <summary>折基础币已发放。</summary>
    public decimal PaidT { get; set; }
}

/// <summary>返水计划分页（旧 planlist）。</summary>
public sealed class RebatePlanPageOutput
{
    /// <summary>总条数。</summary>
    public int RecordCount { get; set; }

    /// <summary>汇总 Money（折基础币）。</summary>
    public decimal DataMoney { get; set; }

    /// <summary>汇总 Paid（折基础币）。</summary>
    public decimal DataPaid { get; set; }

    /// <summary>当前页。</summary>
    public List<RebatePlanListItemDto> Items { get; set; } = [];
}

/// <summary>发放入参。</summary>
public sealed class RebatePayInput
{
    /// <summary>计划编号。</summary>
    public int PlanID { get; set; }
}

/// <summary>生成返水计划入参。</summary>
public class RebateGeneratePlanInput
{
    /// <summary>计划名称。</summary>
    public string Name { get; set; }

    /// <summary>统计起。</summary>
    public DateTime StartAt { get; set; }

    /// <summary>统计止。</summary>
    public DateTime EndAt { get; set; }

    /// <summary>打码倍数。</summary>
    public decimal FlowRate { get; set; }

    /// <summary>币种；空=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>日志类型；空按会员返水。</summary>
    public string Type { get; set; }

    /// <summary>指定用户名，逗号分隔（旧 User）。空=不限。</summary>
    public string User { get; set; }

    /// <summary>会员分组（旧 GroupID）。空=不限。</summary>
    public int[] GroupID { get; set; }
}

/// <summary>执行返水（建计划并发放）入参。对齐旧 execute。</summary>
public sealed class RebateExecuteInput : RebateGeneratePlanInput
{
    /// <summary>计划标题（旧 Title；优先于 Name）。</summary>
    public string Title { get; set; }

    /// <summary>勾选的待发日志编号（旧 checked）；空则收时间段内全部待发。</summary>
    public int[] Checked { get; set; }

    /// <summary>日志编号列表（旧 ID）；与 Checked 合并去重。</summary>
    public int[] ID { get; set; }
}

/// <summary>执行返水结果。</summary>
public sealed class RebateExecuteResultDto
{
    /// <summary>计划编号。</summary>
    public int PlanID { get; set; }

    /// <summary>发放成功笔数。</summary>
    public int Success { get; set; }

    /// <summary>失败笔数（本刀简化恒 0；发放失败已写日志 Status=1）。</summary>
    public int Faild { get; set; }
}

/// <summary>拒绝/重发返水入参。对齐旧 reject|reextcute。</summary>
public sealed class RebateLogActionInput
{
    /// <summary>计划编号。</summary>
    public int PlanID { get; set; }

    /// <summary>日志编号；0=整计划（仅重发用）。</summary>
    public int LogID { get; set; }
}
