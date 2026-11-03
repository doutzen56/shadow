// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的参数配置
/// </summary>
[SugarTable("site_Config")]
public class SiteConfig
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 支持提现的银行
    /// </summary>
    [SugarColumn(ColumnName = "Bank")]
    public string Bank { get; set; }

    /// <summary>
    /// 注册配置
    /// </summary>
    [SugarColumn(ColumnName = "Register")]
    public string RegisterString { get; set; }

    /// <summary>
    /// 邮箱配置
    /// </summary>
    [SugarColumn(ColumnName = "Mail")]
    public string MailString { get; set; }

    /// <summary>
    /// 禁止访问的地区
    /// </summary>
    [SugarColumn(ColumnName = "BlackRegion")]
    public string BlackRegion { get; set; }

    /// <summary>
    /// 允许申请代理的条件
    /// </summary>
    [SugarColumn(ColumnName = "AgentJoin")]
    public string AgentJoinString { get; set; }

    /// <summary>
    /// 短信网关
    /// </summary>
    [SugarColumn(ColumnName = "SMS")]
    public string SMSString { get; set; }

    /// <summary>
    /// 配置可直接提现的资金类型
    /// </summary>
    [SugarColumn(ColumnName = "MoneyType")]
    public string MoneyType { get; set; }

    /// <summary>
    /// 游戏设置
    /// </summary>
    [SugarColumn(ColumnName = "GameSetting")]
    public string GameSetting { get; set; }

    /// <summary>
    /// 后台允许访问的IP
    /// </summary>
    [SugarColumn(ColumnName = "AdminIP")]
    public string AdminIP { get; set; }

    /// <summary>
    /// PC端的自定义样式
    /// </summary>
    [SugarColumn(ColumnName = "PCStyle")]
    public string PCStyle { get; set; }

    /// <summary>
    /// 移动端的自定义样式
    /// </summary>
    [SugarColumn(ColumnName = "MobileStyle")]
    public string MobileStyle { get; set; }

    /// <summary>
    /// 系统提示声音配置
    /// </summary>
    [SugarColumn(ColumnName = "WarningSoundString")]
    public string WarningSoundString { get; set; }

    /// <summary>
    /// VIP系统设置开始时间
    /// </summary>
    [SugarColumn(ColumnName = "VIPString")]
    public string VIPSettingString { get; set; }

    /// <summary>
    /// 支持提现的虚拟币
    /// </summary>
    [SugarColumn(ColumnName = "VirtualCoin")]
    public string VirtualCoin { get; set; }

    /// <summary>
    /// 登录配置,支持用户登录和手机密码登录,和手机短信登录
    /// </summary>
    [SugarColumn(ColumnName = "Login")]
    public string LoginString { get; set; }

    /// <summary>
    /// 站点支持的背景类型
    /// </summary>
    [SugarColumn(ColumnName = "BackgroundType")]
    public string BackgroundType { get; set; }

    /// <summary>
    /// 站点支持的背景类型
    /// </summary>
    [SugarColumn(ColumnName = "Festival")]
    public string Festival { get; set; }

    /// <summary>
    /// 站点是否开放节日背景
    /// </summary>
    [SugarColumn(ColumnName = "FestivalStatus")]
    public string FestivalStatus { get; set; }

    /// <summary>
    /// 手机区号设置
    /// </summary>
    [SugarColumn(ColumnName = "AreaCode")]
    public string AreaCode { get; set; }

    /// <summary>
    /// Telegram推送设置
    /// </summary>
    [SugarColumn(ColumnName = "Telegram")]
    public string TelegramString { get; set; }

    /// <summary>
    /// 注册站内信标题
    /// </summary>
    [SugarColumn(ColumnName = "RegisterTitle")]
    public string RegisterTitle { get; set; }

    /// <summary>
    /// 注册站内信内容
    /// </summary>
    [SugarColumn(ColumnName = "RegisterContent")]
    public string RegisterContent { get; set; }

    /// <summary>
    /// 手机验证码提示
    /// </summary>
    [SugarColumn(ColumnName = "PhoneCodeMessage")]
    public string PhoneCodeMessage { get; set; }

    /// <summary>
    /// Telegram推送设置
    /// </summary>
    [SugarColumn(ColumnName = "Risk")]
    public string RiskSettingString { get; set; }

    /// <summary>
    /// Jackpot爆奖设置
    /// </summary>
    [SugarColumn(ColumnName = "Jackpot")]
    public string JackpotSettingString { get; set; }
}
