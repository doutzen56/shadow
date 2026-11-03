namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 总后台客户留言（表 sys_Feedback）。
/// </summary>
[SugarTable("sys_Feedback")]
public class SystemFeedback
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "FeedID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>称呼。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>公司。</summary>
    [SugarColumn(ColumnName = "Company")]
    public string Company { get; set; }

    /// <summary>邮箱。</summary>
    [SugarColumn(ColumnName = "Email")]
    public string Email { get; set; }

    /// <summary>手机。</summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>内容。</summary>
    [SugarColumn(ColumnName = "Content", ColumnDataType = "nvarchar(max)")]
    public string Content { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>IP。</summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>IP 归属。</summary>
    [SugarColumn(ColumnName = "IPAddress")]
    public string IPAddress { get; set; }
}
