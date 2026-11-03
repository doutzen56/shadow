// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 第三方游戏转账订单（表 game_TransferOrder）。
/// Action 对齐旧 DirectionType：0=None，1=IN 转入游戏（中心→厂商），2=OUT 游戏转出（厂商→中心）。
/// Status 对齐旧 CheckStatus：0=待处理，1=失败，2=成功，3=处理中，4=已解锁。
/// </summary>
[SugarTable("game_TransferOrder")]
public class GameTransferOrder
{
    /// <summary>转账订单号（列 OrderID）。</summary>
    [SugarColumn(ColumnName = "OrderID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>游戏类型（旧 GameType 整型）。</summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>下单时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>厂商返回结果时间。</summary>
    [SugarColumn(ColumnName = "CheckAt")]
    public DateTime CheckAt { get; set; }

    /// <summary>转账金额（正数）。</summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>订单状态（int，对齐旧 CheckStatus）。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>方向（int，对齐旧 DirectionType）。</summary>
    [SugarColumn(ColumnName = "Action")]
    public int Action { get; set; }

    /// <summary>备注（厂商返回或失败原因）。</summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>远程订单号。</summary>
    [SugarColumn(ColumnName = "ResultID")]
    public string ResultID { get; set; }

    /// <summary>币种（旧 Currency 整型）。</summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
