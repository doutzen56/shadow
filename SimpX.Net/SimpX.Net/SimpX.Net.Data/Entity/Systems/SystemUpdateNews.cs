// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统更新通知（表 sys_UpdateNews）。
/// </summary>
[SugarTable("sys_UpdateNews")]
public class SystemUpdateNews
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "NewsID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>标题。</summary>
    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    /// <summary>语种（旧 Language 枚举整型）。</summary>
    [SugarColumn(ColumnName = "Language")]
    public int Language { get; set; }

    /// <summary>正文。</summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>同类型标识。</summary>
    [SugarColumn(ColumnName = "Ident")]
    public string Ident { get; set; }
}
