// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 短信日志
/// </summary>
[SugarTable("site_SMSLog")]
public class SMSLog
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "SMSID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 触发短信的用户（可能为0）
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 短信号码（支持国际区码）
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>
    /// 短信内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 是否发送成功
    /// </summary>
    [SugarColumn(ColumnName = "IsSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 网关的返回信息
    /// </summary>
    [SugarColumn(ColumnName = "ResultMsg")]
    public string ResultMsg { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
