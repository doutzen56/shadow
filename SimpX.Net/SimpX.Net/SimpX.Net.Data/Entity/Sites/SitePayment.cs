// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点配置的支付参数
/// </summary>
[SugarTable("site_Payment")]
public class SitePayment
{
    /// <summary>
    /// 支付编号
    /// </summary>
    [SugarColumn(ColumnName = "PayID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属分类
    /// </summary>
    [SugarColumn(ColumnName = "CateID")]
    public int CateID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 支付渠道名字
    /// </summary>
    [SugarColumn(ColumnName = "PayName")]
    public string Name { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "PayDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 内部备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 排序值（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 支付状态 0: 停止 1: 正常 2: 维护
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 系统参数设置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>
    /// 所选择的支付渠道类型
    /// </summary>
    [SugarColumn(ColumnName = "SysID")]
    public int SysID { get; set; }

    /// <summary>
    /// 充值最低额度
    /// </summary>
    [SugarColumn(ColumnName = "MinMoney")]
    public decimal MinMoney { get; set; }

    /// <summary>
    /// 充值最大额度
    /// </summary>
    [SugarColumn(ColumnName = "MaxMoney")]
    public decimal MaxMoney { get; set; }

    /// <summary>
    /// 金额选项（如果填写,则不能手动输入金额,只能选择）
    /// </summary>
    [SugarColumn(ColumnName = "Amount")]
    public string Amount { get; set; }

    /// <summary>
    /// 指定代理，指定代理及会员才可以看到这个渠道
    /// </summary>
    [SugarColumn(ColumnName = "Agent")]
    public string Agent { get; set; }

    /// <summary>
    /// 充值手续费比例
    /// </summary>
    [SugarColumn(ColumnName = "Fee")]
    public decimal Fee { get; set; }

    /// <summary>
    /// 保底手续费
    /// </summary>
    [SugarColumn(ColumnName = "MinFee")]
    public decimal MinFee { get; set; }

    /// <summary>
    /// 封顶手续费
    /// </summary>
    [SugarColumn(ColumnName = "MaxFee")]
    public decimal MaxFee { get; set; }

    /// <summary>
    /// 成本费用(百分比）
    /// </summary>
    [SugarColumn(ColumnName = "Cost")]
    public decimal Cost { get; set; }

    /// <summary>
    /// 最低成本
    /// </summary>
    [SugarColumn(ColumnName = "MinCost")]
    public decimal MinCost { get; set; }

    /// <summary>
    /// 最高成本
    /// </summary>
    [SugarColumn(ColumnName = "MaxCost")]
    public decimal MaxCost { get; set; }

    /// <summary>
    /// 日累计停用金额（为0不限制）
    /// </summary>
    [SugarColumn(ColumnName = "MaxPay")]
    public decimal MaxPay { get; set; }

    /// <summary>
    /// 适用平台
    /// </summary>
    [SugarColumn(ColumnName = "Platform")]
    public int Platform { get; set; }

    /// <summary>
    /// 是否已被删除
    /// </summary>
    [SugarColumn(ColumnName = "IsDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 提交充值的时候是否有提示
    /// </summary>
    [SugarColumn(ColumnName = "IsNotify")]
    public bool IsNotify { get; set; }

    /// <summary>
    /// 是否开启ERC
    /// </summary>
    [SugarColumn(ColumnName = "IsERC")]
    public string IsERC { get; set; }

    /// <summary>
    /// 是否开启TRC
    /// </summary>
    [SugarColumn(ColumnName = "IsTRC")]
    public string IsTRC { get; set; }

    /// <summary>
    /// 注册天数
    /// </summary>
    [SugarColumn(ColumnName = "RegisterDays")]
    public short RegisterDays { get; set; }

    /// <summary>
    /// 开启账户
    /// </summary>
    [SugarColumn(ColumnName = "IsOpenAccount")]
    public bool IsOpenAccount { get; set; }

    /// <summary>
    /// 开启账户类型
    /// </summary>
    [SugarColumn(ColumnName = "OpenType")]
    public short OpenType { get; set; }

    /// <summary>
    /// 后带随机数
    /// </summary>
    [SugarColumn(ColumnName = "IsRandom")]
    public bool IsRandom { get; set; }

    /// <summary>
    /// 后两位不带0
    /// </summary>
    [SugarColumn(ColumnName = "IsNotHaveZero")]
    public bool IsNotHaveZero { get; set; }

    /// <summary>
    /// 千百位不带0
    /// </summary>
    [SugarColumn(ColumnName = "IsQBNotHaveZero")]
    public bool IsQBNotHaveZero { get; set; }

    /// <summary>
    /// 是否绑定身份验证资料
    /// </summary>
    [SugarColumn(ColumnName = "IsKYC")]
    public bool IsKYC { get; set; }

    /// <summary>
    /// 是否绑银行卡
    /// </summary>
    [SugarColumn(ColumnName = "IsBank")]
    public bool IsBank { get; set; }

    /// <summary>
    /// 是否绑定绑定手机
    /// </summary>
    [SugarColumn(ColumnName = "IsMobile")]
    public bool IsMobile { get; set; }

    /// <summary>
    /// 是否绑定完整邮箱
    /// </summary>
    [SugarColumn(ColumnName = "IsEmail")]
    public bool IsEmail { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 开启账户
    /// </summary>
    [SugarColumn(ColumnName = "DownloadLink")]
    public string DownloadLink { get; set; }
}
