// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 会员在第三方游戏中的帐号（表 game_User）。Game/Currency 存旧枚举整型。
/// </summary>
[SugarTable("game_User")]
public class GameUser
{
    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员编号。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>游戏类型。</summary>
    [SugarColumn(ColumnName = "Game", IsPrimaryKey = true)]
    public int Game { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    /// <summary>第三方帐号。</summary>
    [SugarColumn(ColumnName = "PlayerName")]
    public string PlayerName { get; set; }

    /// <summary>第三方密码。</summary>
    [SugarColumn(ColumnName = "PlayerPass")]
    public string PlayerPass { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>余额。</summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    /// <summary>可转出额度。</summary>
    [SugarColumn(ColumnName = "Withdraw")]
    public decimal Withdraw { get; set; }

    /// <summary>余额更新时间。</summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>游戏锁。</summary>
    [SugarColumn(ColumnName = "Lock")]
    public int Lock { get; set; }

    /// <summary>是否测试号。</summary>
    [SugarColumn(ColumnName = "IsTest")]
    public bool IsTest { get; set; }

    /// <summary>限红。</summary>
    [SugarColumn(ColumnName = "Limits")]
    public string Limits { get; set; }

    /// <summary>限红类型。</summary>
    [SugarColumn(ColumnName = "LimitType")]
    public int LimitType { get; set; }
}
