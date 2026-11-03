namespace SimpX.Net.Application.Shared;

/// <summary>
/// 加减款审核列表筛选。对齐旧 moneyreview/list。
/// </summary>
public sealed class MoneyReviewListInput
{
    /// <summary>状态；空=全部；0 待审 / 1 失败 / 2 通过。</summary>
    public string Status { get; set; }

    /// <summary>用户名（Contains；纯数字按 UserID）。</summary>
    public string User { get; set; }

    /// <summary>包含下级（1/true；配合 User）。</summary>
    public string BelongUser { get; set; }

    /// <summary>上级代理用户名（直属会员 AgentID）。</summary>
    public string Agent { get; set; }

    /// <summary>提交人（SiteAdmin.AdminName Contains）。</summary>
    public string CreatorAdminName { get; set; }

    /// <summary>审核人（SiteAdmin.AdminName Contains）。</summary>
    public string ReviewerAdminName { get; set; }

    /// <summary>资金类型；空=全部。</summary>
    public int? Type { get; set; }

    /// <summary>币种；空=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止（含当日需调用方传次日或本服务按日末扩展）。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>流水号模糊。</summary>
    public string SourceID { get; set; }

    /// <summary>方向：1 加款 / 2 扣款；null=全部。</summary>
    public int? Action { get; set; }
}

/// <summary>
/// 加减款审核列表行。
/// </summary>
public sealed class MoneyReviewListItemDto
{
    /// <summary>审核单号。</summary>
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>金额（扣为负）。</summary>
    public decimal Money { get; set; }

    /// <summary>资金类型。</summary>
    public int Type { get; set; }

    /// <summary>加款=1 / 扣款=2。</summary>
    public int Action { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>流水号。</summary>
    public string SourceID { get; set; }

    /// <summary>前端备注。</summary>
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    public string DescriptionA { get; set; }

    /// <summary>流水倍数。</summary>
    public decimal Flow { get; set; }

    /// <summary>最低流水。</summary>
    public decimal MinFlow { get; set; }

    /// <summary>提交人编号。</summary>
    public int CreatorID { get; set; }

    /// <summary>提交人名称。</summary>
    public string CreatorName { get; set; }

    /// <summary>审核人编号。</summary>
    public int ReviewerID { get; set; }

    /// <summary>审核人名称。</summary>
    public string ReviewerName { get; set; }

    /// <summary>审核时间。</summary>
    public DateTime ReviewAt { get; set; }

    /// <summary>充值渠道名（PayID 映射）。</summary>
    public string PayName { get; set; }
}

/// <summary>
/// 审核加减款入参。对齐旧 moneyreview/check。
/// </summary>
public sealed class MoneyReviewCheckInput
{
    /// <summary>审核单号。</summary>
    public int ID { get; set; }

    /// <summary>目标状态：1 失败 / 2 通过。</summary>
    public int Status { get; set; }

    /// <summary>前端备注（单笔审核可改备注）。</summary>
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    public string DescriptionA { get; set; }
}

/// <summary>
/// 批量审核入参。对齐旧 moneyreview/batchsuccess|batchfail。
/// </summary>
public sealed class MoneyReviewBatchInput
{
    /// <summary>审核单号，逗号分隔。</summary>
    public string Ids { get; set; }

    /// <summary>谷歌验证码（本刀不校验，仅保留字段）。</summary>
    public string Code { get; set; }
}
