namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户表
/// </summary>
[SugarTable("Users")]
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(ColumnName = "NickName")]
    public string NickName { get; set; }

    /// <summary>
    /// 提现账户名
    /// </summary>
    [SugarColumn(ColumnName = "AccountName")]
    public string AccountName { get; set; }

    /// <summary>
    /// 密码,MD5加密
    /// </summary>
    [SugarColumn(ColumnName = "Password")]
    public string Password { get; set; }

    /// <summary>
    /// 资金密码
    /// </summary>
    [SugarColumn(ColumnName = "PayPassword")]
    public string PayPassword { get; set; }

    /// <summary>
    /// 注册时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(ColumnName = "Face")]
    public string Face { get; set; }

    /// <summary>
    /// 注册IP
    /// </summary>
    [SugarColumn(ColumnName = "RegIP")]
    public string RegIP { get; set; }

    /// <summary>
    /// 锁定状态
    /// </summary>
    [SugarColumn(ColumnName = "Lock")]
    public int Lock { get; set; }

    /// <summary>
    /// 功能开放
    /// </summary>
    [SugarColumn(ColumnName = "Function")]
    public int Function { get; set; }

    /// <summary>
    /// 用户的主钱包货币
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 上次登录时间
    /// </summary>
    [SugarColumn(ColumnName = "LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// 登录IP
    /// </summary>
    [SugarColumn(ColumnName = "LoginIP")]
    public string LoginIP { get; set; }

    /// <summary>
    /// 上级代理编号
    /// </summary>
    [SugarColumn(ColumnName = "AgentID")]
    public int AgentID { get; set; }

    /// <summary>
    /// 所属的层级
    /// </summary>
    [SugarColumn(ColumnName = "Depth")]
    public int Depth { get; set; }

    /// <summary>
    /// 会员所属分组
    /// </summary>
    [SugarColumn(ColumnName = "GroupID")]
    public int GroupID { get; set; }

    /// <summary>
    /// 会员所属旧分组
    /// </summary>
    [SugarColumn(ColumnName = "OldGroupID")]
    public int OldGroupID { get; set; }

    /// <summary>
    /// 用户类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 电子邮件
    /// </summary>
    [SugarColumn(ColumnName = "Email")]
    public string Email { get; set; }

    /// <summary>
    /// 纸飞机
    /// </summary>
    [SugarColumn(ColumnName = "Telegram")]
    public string Telegram { get; set; }

    /// <summary>
    /// LINE
    /// </summary>
    [SugarColumn(ColumnName = "Line")]
    public string Line { get; set; }

    /// <summary>
    /// 谷歌
    /// </summary>
    [SugarColumn(ColumnName = "Google")]
    public string Google { get; set; }

    /// <summary>
    /// 电子邮件是否通过验证
    /// </summary>
    [SugarColumn(ColumnName = "IsEmail")]
    public bool IsEmail { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>
    /// 手机号码是否验证
    /// </summary>
    [SugarColumn(ColumnName = "IsMobile")]
    public bool IsMobile { get; set; }

    /// <summary>
    /// 密保问题
    /// </summary>
    [SugarColumn(ColumnName = "Question")]
    public int Question { get; set; }

    /// <summary>
    /// 密保问题,MD5加密
    /// </summary>
    [SugarColumn(ColumnName = "Answer")]
    public string Answer { get; set; }

    /// <summary>
    /// 谷歌验证码
    /// </summary>
    [SugarColumn(ColumnName = "SecretKey")]
    public Guid SecretKey { get; set; }

    /// <summary>
    /// 可用余额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 锁定资金
    /// </summary>
    [SugarColumn(ColumnName = "LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>
    /// 游戏余额汇总
    /// </summary>
    [SugarColumn(ColumnName = "GameMoney")]
    public decimal GameMoney { get; set; }

    /// <summary>
    /// 未结算的资金
    /// </summary>
    [SugarColumn(ColumnName = "Unbalanced")]
    public decimal Unbalanced { get; set; }

    /// <summary>
    /// 当前是否在线
    /// </summary>
    [SugarColumn(ColumnName = "IsOnline")]
    public bool IsOnline { get; set; }

    /// <summary>
    /// 用户标记
    /// </summary>
    [SugarColumn(ColumnName = "Flag")]
    public int Flag { get; set; }

    /// <summary>
    /// 用户等级
    /// </summary>
    [SugarColumn(ColumnName = "Level")]
    public byte Level { get; set; }

    /// <summary>
    /// 用户初始等级，专用于前面的级别不想打的情况
    /// </summary>
    [SugarColumn(ColumnName = "InitLevel")]
    public byte InitLevel { get; set; }

    /// <summary>
    /// 活动时间
    /// </summary>
    [SugarColumn(ColumnName = "ActiveAt")]
    public DateTime ActiveAt { get; set; }

    /// <summary>
    /// 经验值
    /// </summary>
    [SugarColumn(ColumnName = "Exper")]
    public int Exper { get; set; }

    /// <summary>
    /// 用户开户方式
    /// </summary>
    [SugarColumn(ColumnName = "RegFlag")]
    public int RegFlag { get; set; }

    /// <summary>
    /// 注册来源的域名
    /// </summary>
    [SugarColumn(ColumnName = "RegDomain")]
    public string RegDomain { get; set; }

    /// <summary>
    /// 登录时候所使用的设备
    /// </summary>
    [SugarColumn(ColumnName = "LoginPlatform")]
    public int LoginPlatform { get; set; }

    /// <summary>
    /// 注册所使用的设备
    /// </summary>
    [SugarColumn(ColumnName = "RegPlatform")]
    public int RegPlatform { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 邀请人ID
    /// </summary>
    [SugarColumn(ColumnName = "InviteID")]
    public int InviteID { get; set; }

    /// <summary>
    /// 代理占成
    /// </summary>
    [SugarColumn(ColumnName = "ProxyRate")]
    public decimal ProxyRate { get; set; }

    /// <summary>
    /// 洗码比
    /// </summary>
    [SugarColumn(ColumnName = "WashingRatio")]
    public string WashingRatio { get; set; }

    /// <summary>
    /// 最后返水时间
    /// </summary>
    [SugarColumn(ColumnName = "LastReturn")]
    public DateTime LastReturn { get; set; }

    /// <summary>
    /// 积分
    /// </summary>
    [SugarColumn(ColumnName = "Coin")]
    public decimal Coin { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Notes")]
    public string Notes { get; set; }

    /// <summary>
    /// 禁止货币
    /// </summary>
    [SugarColumn(ColumnName = "DisableCurrency")]
    public string DisableCurrency { get; set; }

    /// <summary>
    /// 用户地址
    /// </summary>
    [SugarColumn(ColumnName = "Address")]
    public string Address { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    [SugarColumn(ColumnName = "Birthday")]
    public DateTime Birthday { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    [SugarColumn(ColumnName = "Info")]
    public string Info { get; set; }

    /// <summary>
    /// 设备号
    /// </summary>
    [SugarColumn(ColumnName = "DeviceId")]
    public string DeviceId { get; set; }

    /// <summary>
    /// 注册来源
    /// </summary>
    [SugarColumn(ColumnName = "RegisteredFrom")]
    public string RegisteredFrom { get; set; }

    /// <summary>
    /// 像素ID
    /// </summary>
    [SugarColumn(ColumnName = "PixelID")]
    public string PixelID { get; set; }

    /// <summary>
    /// 是否初始密码
    /// </summary>
    [SugarColumn(ColumnName = "Is_Temporary_Password")]
    public bool Is_Temporary_Password { get; set; }

    /// <summary>
    /// 锁定的游戏
    /// </summary>
    [SugarColumn(ColumnName = "BlockGame")]
    public string BlockGame { get; set; }
}
