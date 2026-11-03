namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统短信服务 🧩：生成并校验手机验证码。具体怎么发由注册的 <see cref="ISmsSender"/> 决定。
/// </summary>
[AllowAnonymous]
[ApiDescriptionSettings(Module = "system", Name = "sms", Order = 150)]
public class SmsAppService : IDynamicApiController, ITransient, ISmsCodeVerifier
{
    private readonly IOptionsMonitor<SmsOptions> _smsOptions;
    private readonly ICacheStateStore _state;
    private readonly IEnumerable<ISmsSender> _senders;

    /// <summary>
    /// 注入短信配置、状态缓存和已注册的发送通道。
    /// </summary>
    /// <param name="smsOptions">短信配置</param>
    /// <param name="state">状态缓存，验证码按手机号摘要存放</param>
    /// <param name="senders">已注册的短信发送通道</param>
    public SmsAppService(IOptionsMonitor<SmsOptions> smsOptions, ICacheStateStore state, IEnumerable<ISmsSender> senders)
    {
        _smsOptions = smsOptions;
        _state = state;
        _senders = senders;
    }

    /// <summary>
    /// 发送短信验证码。验证码按手机号摘要放进缓存，过期时间看缓存定义；演示通道用固定验证码。
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="templateId">短信模板编号</param>
    /// <returns>异步任务</returns>
    /// <exception cref="AppFriendlyException">手机号不合法或没有可用的短信通道</exception>
    // 手机号在请求地址里，记请求日志会把它明文写进去
    [SuppressMonitor]
    [AllowAnonymous]
    [DisplayName("发送短信")]
    public async Task SendSms([Required] string phoneNumber, string templateId = "0")
    {
        if (!phoneNumber.TryValidate(ValidationTypes.PhoneNumber).IsValid) throw Oops.Oh("请正确填写手机号码");

        var sender = _senders.LastOrDefault() ?? throw Oops.Oh("未配置短信发送通道");
        var options = _smsOptions.CurrentValue;
        var verifyCode = options.Demo.Enabled ? options.Demo.FixedVerifyCode : Random.Shared.Next(100000, 999999).ToString();

        await sender.SendAsync(new SmsMessage
        {
            PhoneNumber = phoneNumber,
            TemplateId = templateId,
            Parameters = new Dictionary<string, string> { ["code"] = verifyCode }
        });

        // 过期时间用缓存定义，不把手机号原文放进键里
        _state.Add(AuthCacheKeys.PhoneCode, CacheIdHasher.Sha256(phoneNumber), verifyCode, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 校验短信验证码。
    /// </summary>
    /// <param name="input">手机号和验证码</param>
    /// <returns>校验通过返回 true</returns>
    /// <exception cref="AppFriendlyException">验证码不存在、已失效或不正确</exception>
    // 入参带手机号和验证码，记请求日志会把它们明文写进去
    [SuppressMonitor]
    [AllowAnonymous]
    [DisplayName("校验短信验证码")]
    public bool VerifyCode(SmsVerifyCodeInput input)
    {
        VerifyPhoneCode(input.Phone, input.Code);
        return true;
    }

    /// <inheritdoc />
    public void VerifyPhoneCode(string phone, string code)
    {
        var verifyCode = _state.Get<string>(AuthCacheKeys.PhoneCode, CacheIdHasher.Sha256(phone));

        if (string.IsNullOrWhiteSpace(verifyCode)) throw Oops.Oh("验证码不存在或已失效，请重新获取！");

        if (verifyCode != code) throw Oops.Oh("验证码错误！");
    }
}
