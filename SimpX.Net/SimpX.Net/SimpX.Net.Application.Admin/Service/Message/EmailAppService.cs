namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统邮件发送服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "email", Order = 370)]
public class EmailAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysTenant> _sysTenantRep;
    private readonly IEmailSender _emailSender;

    /// <summary>
    /// 注入租户仓储（取邮件标题里的系统名）和邮件发送器。
    /// </summary>
    /// <param name="sysTenantRep">租户仓储</param>
    /// <param name="emailSender">邮件发送器</param>
    public EmailAppService(SqlSugarRepository<SysTenant> sysTenantRep, IEmailSender emailSender)
    {
        _sysTenantRep = sysTenantRep;
        _emailSender = emailSender;
    }

    /// <summary>
    /// 发送邮件 📧。标题为空时用当前租户的系统名拼“系统邮件”。
    /// </summary>
    /// <param name="content">HTML 正文</param>
    /// <param name="title">标题</param>
    /// <param name="toEmail">收件人，空时发给默认收件人</param>
    /// <returns>异步任务</returns>
    [DisplayName("发送邮件")]
    public async Task SendEmail([Required] string content, string title = "", string toEmail = "")
    {
        long.TryParse(App.User?.FindFirst(ClaimConst.TenantId)?.Value ?? SqlSugarConst.DefaultTenantId.ToString(), out var tenantId);
        var webTitle = (await _sysTenantRep.GetFirstAsync(u => u.Id == tenantId))?.Title;
        title = string.IsNullOrWhiteSpace(title) ? $"{webTitle} 系统邮件" : title;
        await _emailSender.SendAsync(title, content, toEmail);
    }
}
