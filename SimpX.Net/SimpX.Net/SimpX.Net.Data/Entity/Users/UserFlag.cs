namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的标记
/// </summary>
[SugarTable("usr_Flag")]
public class UserFlag
{
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>
    /// 是否已经绑定过银行卡
    /// </summary>
    [SugarColumn(ColumnName = "IsCard")]
    public bool IsCard { get; set; }

    /// <summary>
    /// 已绑定过手机号
    /// </summary>
    [SugarColumn(ColumnName = "IsMobile")]
    public bool IsMobile { get; set; }

    /// <summary>
    /// 已绑定过电子邮箱
    /// </summary>
    [SugarColumn(ColumnName = "IsEmail")]
    public bool IsEmail { get; set; }

    /// <summary>
    /// 是否已经绑定过Kyc
    /// </summary>
    [SugarColumn(ColumnName = "IsKYCCard")]
    public bool IsKYCCard { get; set; }

    /// <summary>
    /// // KYC 页面流程 //
    /// </summary>
    [SugarColumn(ColumnName = "KYCProcess")]
    public string KYCProcessString { get; set; }
}
