using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 视讯打赏日志（只读；表 game_RewardLog）。旧版报表主路径走 ES，本实体供 SQL 只读聚合。
/// </summary>
[SugarTable("game_RewardLog")]
public class RewardLog
{
    /// <summary>日志编号。</summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>游戏类型。</summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>玩家名。</summary>
    [SugarColumn(ColumnName = "PlayerName")]
    public string PlayerName { get; set; }

    /// <summary>流水号。</summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>交易类型。</summary>
    [SugarColumn(ColumnName = "TradeType")]
    public int TradeType { get; set; }

    /// <summary>交易额度。</summary>
    [SugarColumn(ColumnName = "TradeAmount")]
    public decimal TradeAmount { get; set; }

    /// <summary>打赏时间。</summary>
    [SugarColumn(ColumnName = "RewardAt")]
    public DateTime RewardAt { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
