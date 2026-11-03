// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点奖池
/// </summary>
[SugarTable("site_Jackpot")]
public class SiteJackpot
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 游戏分类
    /// </summary>
    [SugarColumn(ColumnName = "Category")]
    public int Category { get; set; }

    /// <summary>
    /// 游戏平台
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 用户名称
    /// </summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>
    /// 额度
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 平台Icon路径
    /// </summary>
    [SugarColumn(ColumnName = "PlatformIcon")]
    public string PlatformIcon { get; set; }

    /// <summary>
    /// 游戏ID
    /// </summary>
    [SugarColumn(ColumnName = "GameID")]
    public string GameID { get; set; }

    /// <summary>
    /// 游戏名称
    /// </summary>
    [SugarColumn(ColumnName = "GameName")]
    public string GameName { get; set; }

    /// <summary>
    /// 游戏Icon路径
    /// </summary>
    [SugarColumn(ColumnName = "GameIcon")]
    public string GameIcon { get; set; }

    /// <summary>
    /// 支持的币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public string Currency { get; set; }

    /// <summary>
    /// 1：自动 0：手动
    /// </summary>
    [SugarColumn(ColumnName = "IsAuto")]
    public bool IsAuto { get; set; }

    /// <summary>
    /// 游戏币种
    /// </summary>
    [SugarColumn(ColumnName = "GameCurrency")]
    public int GameCurrency { get; set; }

    /// <summary>
    /// 游戏币种对应的金额
    /// </summary>
    [SugarColumn(ColumnName = "GameMoney")]
    public decimal GameMoney { get; set; }

    /// <summary>
    /// 系统获取的排名
    /// </summary>
    [SugarColumn(ColumnName = "GameRank")]
    public int GameRank { get; set; }

    /// <summary>
    /// 记录创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
