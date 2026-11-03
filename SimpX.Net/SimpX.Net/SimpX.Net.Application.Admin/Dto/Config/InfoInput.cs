namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统信息保存输入参数
/// </summary>
public class InfoSaveInput
{
    /// <summary>
    /// 系统图标（Data URI scheme base64 编码）
    /// </summary>
    public string LogoBase64 { get; set; }

    /// <summary>
    /// 系统图标文件名
    /// </summary>
    public string LogoFileName { get; set; }

    /// <summary>
    /// 水印内容
    /// </summary>
    public string Watermark { get; set; }

    /// <summary>
    /// 系统主标题
    /// </summary>
    [Required(ErrorMessage = "系统主标题不能为空")]
    public string Title { get; set; }

    /// <summary>
    /// 系统副标题
    /// </summary>
    [Required(ErrorMessage = "系统副标题不能为空")]
    public string ViceTitle { get; set; }

    /// <summary>
    /// 系统描述
    /// </summary>
    [Required(ErrorMessage = "系统描述不能为空")]
    public string ViceDesc { get; set; }

    /// <summary>
    /// 版权说明
    /// </summary>
    [Required(ErrorMessage = "版权说明不能为空")]
    public string Copyright { get; set; }

    /// <summary>
    /// ICP备案号
    /// </summary>
    [Required(ErrorMessage = "ICP备案号不能为空")]
    public string Icp { get; set; }

    /// <summary>
    /// ICP地址
    /// </summary>
    [Required(ErrorMessage = "ICP地址不能为空")]
    public string IcpUrl { get; set; }

    /// <summary>
    /// 启用注册功能
    /// </summary>
    public YesNoEnum EnableReg { get; set; }

    /// <summary>
    /// 登录二次验证
    /// </summary>
    public YesNoEnum SecondVer { get; set; }

    /// <summary>
    /// 图形验证码
    /// </summary>
    public YesNoEnum Captcha { get; set; }

    /// <summary>
    /// 默认注册方案Id
    /// </summary>
    public virtual long RegWayId { get; set; }
}